# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Serve a Unity WebGL build on http://localhost and open it in headless Chrome; pass when the console shows every
expected line, fail on a failure line or a timeout. Uses Chrome for Testing from the Playwright cache (macOS only, no npm needed) in --headless=new mode (the cached
chrome-headless-shell has no WebGL for Unity): console messages reach stderr with --enable-logging=stderr --v=1 as
INFO:CONSOLE... "text", source: ...

Usage (from the repo root):
  uv run tools/web_check.py unity/Builds/Web-disabled --query "golden" --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952"
Options: --query Q (page URL query, without '?'), --expect TEXT (repeatable), --fail REGEX (default: GOLDEN FAIL|SMOKE FAIL|BOOT FAIL),
--timeout SECONDS (default 300), --swiftshader (software WebGL when there is no GPU).
Pre-compressed builds: *.br and *.gz files are served with Content-Encoding, *.wasm(.br/.gz) as application/wasm.
Brotli needs a secure context; http://localhost is one."""
import argparse
import functools
import http.server
import os
import re
import subprocess
import sys
import queue
import shutil
import tempfile
import threading
import time
from pathlib import Path

# A console message's first line; Unity's Debug.Log adds a stack trace on later lines, so the closing quote and
# ", source:" may be far below. Only the first line is matched.
CONSOLE = re.compile(r'INFO:CONSOLE[^\]]*\] "(.*?)(?:", source: .*)?$')
CHROME_GLOB = "ms-playwright/chromium-*/chrome-mac*/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        path = self.path.split("?")[0]
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        base = re.sub(r"\.(br|gz)$", "", str(path))
        if base.endswith(".wasm"):
            return "application/wasm"
        if base.endswith(".js"):
            return "application/javascript"
        if base.endswith((".data", ".r16")):
            return "application/octet-stream"
        return super().guess_type(base)

    def log_message(self, fmt, *args):
        if os.environ.get("WEB_CHECK_VERBOSE"):
            print("http:", fmt % args)


def find_chrome() -> Path:
    """The newest Chrome for Testing in the Playwright cache (macOS, arm64 or x64), by numeric revision."""
    found = list(Path.home().joinpath("Library", "Caches").glob(CHROME_GLOB))
    if not found:
        sys.exit("Chrome for Testing not found under ~/Library/Caches/ms-playwright/chromium-<rev>/chrome-mac*"
                 " (macOS only; install with: npx playwright install chromium)")

    def revision(path: Path) -> int:
        m = re.search(r"chromium-(\d+)", str(path))
        return int(m.group(1)) if m else -1
    return max(found, key=revision)


def serve(root: Path) -> http.server.ThreadingHTTPServer:
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(Handler, directory=str(root)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def scan(lines, expect, fail):
    """Reads stderr lines: returns (status, pending, texts). status is "fail" at the first failure message, "pass" when
    every expected text has been seen, else "pending". texts are the console messages found (first lines only)."""
    pending, texts = list(expect), []
    for raw in lines:
        m = CONSOLE.search(raw)
        if not m:
            continue
        text = m.group(1)
        texts.append(text)
        if fail.search(text):
            return "fail", pending, texts
        pending = [p for p in pending if p not in text]
    return ("pass" if not pending else "pending"), pending, texts


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("build", type=Path)
    ap.add_argument("--query", default="")
    ap.add_argument("--expect", action="append", default=[])
    ap.add_argument("--fail", default=r"GOLDEN FAIL|SMOKE FAIL|BOOT FAIL")
    ap.add_argument("--timeout", type=float, default=300)
    ap.add_argument("--swiftshader", action="store_true")
    a = ap.parse_args()
    if not (a.build / "index.html").exists():
        sys.exit(f"{a.build}/index.html not found")
    server, proc, profile = None, None, tempfile.mkdtemp(prefix="web-check-")
    try:
        chrome = find_chrome()
        server = serve(a.build)
        url = f"http://localhost:{server.server_address[1]}/index.html" + (f"?{a.query}" if a.query else "")
        flags = ["--headless=new", "--enable-logging=stderr", "--v=1", "--no-first-run", "--no-default-browser-check",
                 "--autoplay-policy=no-user-gesture-required", "--window-size=1920,1080",
                 "--disable-frame-rate-limit", "--disable-gpu-vsync", f"--user-data-dir={profile}"]
        if a.swiftshader:
            flags += ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]
        proc = subprocess.Popen([str(chrome), *flags, url], stderr=subprocess.PIPE, stdout=subprocess.DEVNULL, text=True, errors="replace")
        fail, pending, deadline = re.compile(a.fail), list(a.expect), time.time() + a.timeout
        inbox: queue.Queue = queue.Queue()
        threading.Thread(target=lambda: [inbox.put(l) for l in iter(proc.stderr.readline, "")], daemon=True).start()
        idle = 0
        while time.time() < deadline:
            batch = []
            while True:       # lines are dropped once scanned: only the batch is held
                try:
                    batch.append(inbox.get_nowait())
                except queue.Empty:
                    break
            status, pending, texts = scan(batch, pending, fail)
            for t in texts:
                print("console:", t)
            if status == "fail":
                print(f"FAIL: {texts[-1]}")
                return 1
            if status == "pass":
                print("PASS: saw " + ", ".join(a.expect))
                return 0
            if proc.poll() is not None and not batch:
                idle += 1      # two idle polls after exit: the reader thread has drained stderr
            else:
                idle = 0
            if idle >= 2:
                print(f"BROWSER EXITED code {proc.returncode}; never saw " + ", ".join(pending))
                return 1
            time.sleep(0.2)
        print("TIMEOUT: never saw " + ", ".join(pending))
        return 1
    finally:
        if proc is not None:
            proc.terminate()
            try:
                proc.wait(5)
            except subprocess.TimeoutExpired:
                proc.kill(); proc.wait()
        if server is not None:
            server.shutdown(); server.server_close()
        shutil.rmtree(profile, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
