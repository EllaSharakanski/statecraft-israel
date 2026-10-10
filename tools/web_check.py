# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Serve a Unity WebGL build on http://localhost and open it in Chrome; pass when the console shows every expected line,
fail on a failure line or a timeout. Uses Chrome for Testing from the Playwright cache (macOS only, no npm needed) in
--headless=new mode (the cached chrome-headless-shell has no WebGL for Unity): console messages reach stderr with
--enable-logging=stderr --v=1 as INFO:CONSOLE... "text", source: ...

Usage (from the repo root):
  uv run tools/web_check.py unity/Builds/Web-disabled --query "golden" --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" --expect "GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767"
Options: --query Q (page URL query, without '?'), --expect TEXT (repeatable), --fail REGEX (default: GOLDEN FAIL|SMOKE FAIL|BOOT FAIL),
--timeout SECONDS (default 300), --swiftshader (software WebGL when there is no GPU), --shots DIR (reassemble the player's
"SHOT" console lines, from ?shot, into DIR/<pose>.png; a "SHOT done" with an unfinished shot fails the run). Chrome runs at
device pixel ratio 1. Headed runs (a visible window, another DPR) go through tools/headed.py.
Pre-compressed builds: *.br and *.gz files are served with Content-Encoding, *.wasm(.br/.gz) as application/wasm.
Brotli needs a secure context; http://localhost is one."""
import argparse
import base64
import functools
import hashlib
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

# A console message's first line; warnings and errors carry a stack trace on later lines (ProjectSetup sets none for
# Debug.Log), so the closing quote and ", source:" may be far below. Only the first line is matched.
CONSOLE = re.compile(r'INFO:CONSOLE[^\]]*\] "(.*?)(?:", source: .*)?$')
CHROME_GLOB = "ms-playwright/chromium-*/chrome-mac*/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"
SHOT_BEGIN = re.compile(r"^SHOT (\S+) begin (\d+) ([0-9a-f]{64})$")
SHOT_PART = re.compile(r"^SHOT (\S+) (\d+)/(\d+) (\S+)$")


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


class ShotCollector:
    """Reassembles the PNGs Statecraft.Game.Map.VisualShots logs: "SHOT <pose> begin <n> <sha256>" then n lines
    "SHOT <pose> <i>/<n> <base64>" (any order). A finished shot is checked against its SHA-256 and written to
    <out_dir>/<pose>.png (out_dir None: checked, not written)."""

    def __init__(self, out_dir: Path | None):
        self.out_dir = out_dir
        self.expect: dict[str, tuple[int, str]] = {}
        self.parts: dict[str, dict[int, str]] = {}
        self.written: list[Path] = []

    def feed(self, text: str) -> bool:
        """True when text was a begin or part line (consumed: too long to print); False for anything else. "SHOT done"
        while a shot is unfinished (a part line was lost) raises ValueError naming it."""
        if text == "SHOT done" and self.expect:
            unfinished = ", ".join(f"{n}: {len(self.parts.get(n, {}))} of {k} parts" for n, (k, _) in self.expect.items())
            raise ValueError(f"SHOT done with unfinished shots ({unfinished})")
        m = SHOT_BEGIN.match(text)
        if m:
            self.expect[m.group(1)] = (int(m.group(2)), m.group(3))
            self.parts[m.group(1)] = {}
            return True
        m = SHOT_PART.match(text)
        if not m:
            return False
        name, i = m.group(1), int(m.group(2))
        if name not in self.expect:
            raise ValueError(f"SHOT {name}: part {i}/{m.group(3)} before its begin line")
        self.parts[name][i] = m.group(4)
        if len(self.parts[name]) == self.expect[name][0]:
            self._finish(name)
        return True

    def _finish(self, name: str) -> None:
        n, digest = self.expect.pop(name)
        parts = self.parts.pop(name)
        try:
            png = base64.b64decode("".join(parts[i] for i in range(1, n + 1)), validate=True)
        except ValueError:                       # binascii.Error: a cut line leaves invalid base64
            png = b""
        if hashlib.sha256(png).hexdigest() != digest:
            raise ValueError(f"SHOT {name}: SHA-256 mismatch (a console line was cut or lost)")
        if self.out_dir is not None:
            self.out_dir.mkdir(parents=True, exist_ok=True)
            path = self.out_dir / f"{name}.png"
            path.write_bytes(png)
            self.written.append(path)
            print(f"shot: wrote {path}")


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


def chrome_flags(profile: str, swiftshader: bool) -> list[str]:
    """Headless Chrome at device pixel ratio 1, frame rate uncapped (so frame times are not the display's)."""
    flags = ["--headless=new", "--enable-logging=stderr", "--v=1", "--no-first-run", "--no-default-browser-check",
             "--autoplay-policy=no-user-gesture-required", "--window-size=1920,1080", "--force-device-scale-factor=1",
             "--disable-frame-rate-limit", "--disable-gpu-vsync", f"--user-data-dir={profile}"]
    if swiftshader:
        flags += ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]
    return flags


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


def drain(inbox: queue.Queue) -> list:
    batch = []
    while True:       # lines are dropped once scanned: only the batch is held
        try:
            batch.append(inbox.get_nowait())
        except queue.Empty:
            return batch


def watch(inbox, poll, join_reader, expect, fail, timeout, now=time.time, sleep=time.sleep, on_text=None) -> int:
    """The check loop: 0 on PASS, 1 on a failure line, on BROWSER EXITED or on TIMEOUT. poll() is the browser's
    returncode (None while running); join_reader() waits briefly for the stderr reader thread to hit EOF, so output
    written just before the exit is still scanned. on_text(text) -> True consumes a message (it is not printed)."""
    pending, deadline = list(expect), now() + timeout

    def look():
        nonlocal pending
        status, pending, texts = scan(drain(inbox), pending, fail)
        for t in texts:
            if on_text is not None and on_text(t):
                continue
            print("console:", t)
        if status == "fail":
            print(f"FAIL: {texts[-1]}")
            return 1
        if status == "pass":
            print("PASS: saw " + ", ".join(expect))
            return 0
        return None

    while now() < deadline:
        result = look()
        if result is not None:
            return result
        code = poll()
        if code is not None:
            join_reader()
            result = look()
            if result is not None:
                return result
            print(f"BROWSER EXITED code {code}; never saw " + ", ".join(pending))
            return 1
        sleep(0.2)
    print("TIMEOUT: never saw " + ", ".join(pending))
    return 1


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("build", type=Path)
    ap.add_argument("--query", default="")
    ap.add_argument("--expect", action="append", default=[])
    ap.add_argument("--fail", default=r"GOLDEN FAIL|SMOKE FAIL|BOOT FAIL")
    ap.add_argument("--timeout", type=float, default=300)
    ap.add_argument("--swiftshader", action="store_true")
    ap.add_argument("--shots", type=Path)
    a = ap.parse_args()
    if not (a.build / "index.html").exists():
        sys.exit(f"{a.build}/index.html not found")
    server, proc, profile = None, None, tempfile.mkdtemp(prefix="web-check-")
    shots = ShotCollector(a.shots)
    try:
        chrome = find_chrome()
        server = serve(a.build)
        url = f"http://localhost:{server.server_address[1]}/index.html" + (f"?{a.query}" if a.query else "")
        proc = subprocess.Popen([str(chrome), *chrome_flags(profile, a.swiftshader), url],
                                stderr=subprocess.PIPE, stdout=subprocess.DEVNULL, text=True, errors="replace")
        inbox: queue.Queue = queue.Queue()
        reader = threading.Thread(target=lambda: [inbox.put(l) for l in iter(proc.stderr.readline, "")], daemon=True)
        reader.start()
        try:
            return watch(inbox, proc.poll, lambda: reader.join(2), list(a.expect), re.compile(a.fail), a.timeout, on_text=shots.feed)
        except ValueError as e:
            print(f"FAIL: {e}")
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
