# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Serve a Unity WebGL build on http://localhost and open it in headless Chrome; pass when the console shows every
expected line, fail on a failure line or a timeout. Uses the Playwright-cached chrome-headless-shell (no npm needed):
console messages reach stderr with --enable-logging=stderr --v=1 as  INFO:CONSOLE... "text", source: ...

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
import threading
import time
from pathlib import Path

# A console message's first line; Unity's Debug.Log adds a stack trace on later lines, so the closing quote and
# ", source:" may be far below. Only the first line is matched.
CONSOLE = re.compile(r'INFO:CONSOLE[^\]]*\] "(.*?)(?:", source: .*)?$')
CHROME_GLOB = "ms-playwright/chromium-*/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"


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
    found = sorted(Path.home().joinpath("Library", "Caches").glob(CHROME_GLOB))
    if not found:
        sys.exit("Chrome for Testing not found under ~/Library/Caches/ms-playwright (npx playwright install chromium)")
    return found[-1]


def serve(root: Path) -> http.server.ThreadingHTTPServer:
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(Handler, directory=str(root)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


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
    server = serve(a.build)
    url = f"http://localhost:{server.server_address[1]}/index.html" + (f"?{a.query}" if a.query else "")
    flags = ["--headless=new", "--enable-logging=stderr", "--v=1", "--no-first-run", "--no-default-browser-check",
             "--autoplay-policy=no-user-gesture-required", "--window-size=1920,1080",
             "--disable-frame-rate-limit", "--disable-gpu-vsync",
             f"--user-data-dir={a.build.resolve()}/../.web-check-profile"]
    if a.swiftshader:
        flags += ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]
    proc = subprocess.Popen([str(find_chrome()), *flags, url], stderr=subprocess.PIPE, stdout=subprocess.DEVNULL, text=True, errors="replace")
    pending, fail, deadline = list(a.expect), re.compile(a.fail), time.time() + a.timeout
    lines: list[str] = []
    reader = threading.Thread(target=lambda: lines.extend(iter(proc.stderr.readline, "")), daemon=True)
    reader.start()
    seen = 0
    try:
        while time.time() < deadline:
            while seen < len(lines):
                m = CONSOLE.search(lines[seen]); seen += 1
                if not m:
                    continue
                text = m.group(1)
                print("console:", text)
                if fail.search(text):
                    print(f"FAIL: {text}")
                    return 1
                pending = [p for p in pending if p not in text]
            if not pending:
                print("PASS: saw " + ", ".join(a.expect))
                return 0
            time.sleep(0.2)
        print("TIMEOUT: never saw " + ", ".join(pending))
        return 1
    finally:
        proc.terminate()
        server.shutdown()


if __name__ == "__main__":
    sys.exit(main())
