# /// script
# requires-python = ">=3.12"
# dependencies = ["websockets==15.0.1"]
# ///
"""Headed browser checks over the Chrome DevTools Protocol (CDP): real GPU (ANGLE Metal), real mouse events.

Launches Chrome for Testing (Playwright's cache, as web_check.py) HEADED with a temporary profile that is deleted on exit,
serves a WebGL build with web_check's handler, and drives it over one browser-level websocket. Subcommands:

  boot      boot-phase timings from ?bootlog (optionally under CPU throttling): where the start-up time goes
  navpairs  pairs of clicks on a screen button and a second target, d ms apart or in one frame, under ?navlog
  loadshot  screenshots during the load and once ready (the loading line, or the boot-failure line)

Run from the repo root, e.g.:
  uv run tools/headed.py boot --build unity/Builds/Web-disabled --cpu 8
  uv run tools/headed.py navpairs --build unity/Builds/Web-disabled --cpu 20
Coordinates are CSS pixels for a 1568x752 window (the playtest window). Used for playtest 2 (P2-1, P2-4) and the final
review evidence; the EditMode tests are the regression guard, this is the in-browser check.
"""
import argparse
import asyncio
import base64
import functools
import http.server
import json
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import time
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "tools"))
import web_check  # noqa: E402  (stdlib only)

WINDOW = (1568, 752)
END_TURN, WHY, OPTION, MAP_UNDER_WHY = (1516, 42), (1317, 33), (1390, 283), (815, 450)


def burst_events(points):
    """Mouse events for clicks at `points`, in order: move, press, release each. Sent at once, they land in one frame."""
    return [{"type": t, "x": x, "y": y, "button": "left", "clickCount": 1}
            for (x, y) in points for t in ("mouseMoved", "mousePressed", "mouseReleased")]


class Browser:
    def __init__(self, build: Path, port=8766, width=WINDOW[0], height=WINDOW[1]):
        self.build, self.port, self.size = Path(build), port, (width, height)
        self.profile = tempfile.mkdtemp(prefix="statecraft-headed-")
        self.next_id, self.pending, self.console = 0, {}, []

    async def __aenter__(self):
        import websockets  # lazy: the normal tools test run does not install it
        self.server = http.server.ThreadingHTTPServer(("127.0.0.1", self.port), functools.partial(web_check.Handler, directory=str(self.build)))
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        self.base = f"http://localhost:{self.port}/"
        w, h = self.size
        flags = ["--use-angle=metal", "--remote-debugging-port=0", f"--user-data-dir={self.profile}", "--no-first-run",
                 "--no-default-browser-check", f"--window-size={w},{h}", "about:blank"]
        self.proc = subprocess.Popen([str(web_check.find_chrome()), *flags], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        port_file = Path(self.profile) / "DevToolsActivePort"
        for _ in range(200):
            if port_file.exists() and len(port_file.read_text().split()) >= 2:
                break
            await asyncio.sleep(0.1)
        port, path = port_file.read_text().split()[:2]
        self.ws = await websockets.connect(f"ws://127.0.0.1:{port}{path}", max_size=None)
        self.reader = asyncio.create_task(self._read())
        return self

    async def __aexit__(self, *exc):
        try:
            await self.send("Browser.close")
        except Exception:
            pass
        self.reader.cancel()
        try:
            self.proc.wait(10)
        except subprocess.TimeoutExpired:
            self.proc.kill()
        self.server.shutdown(); self.server.server_close()
        shutil.rmtree(self.profile, ignore_errors=True)   # the temporary profile never outlives the run

    async def _read(self):
        async for raw in self.ws:
            m = json.loads(raw)
            if "id" in m and m["id"] in self.pending:
                self.pending.pop(m["id"]).set_result(m)
            elif m.get("method") == "Runtime.consoleAPICalled":
                p = m["params"]
                text = " ".join(str(a.get("value", a.get("description", ""))) for a in p.get("args", []))
                self.console.append((m.get("sessionId"), text.split("\n")[0], p.get("timestamp")))

    async def send(self, method, params=None, session=None, timeout=60):
        self.next_id += 1
        msg = {"id": self.next_id, "method": method, "params": params or {}}
        if session:
            msg["sessionId"] = session
        fut = asyncio.get_event_loop().create_future()
        self.pending[self.next_id] = fut
        await self.ws.send(json.dumps(msg))
        r = await asyncio.wait_for(fut, timeout)
        if "error" in r:
            raise RuntimeError(f"{method}: {r['error']}")
        return r.get("result", {})

    async def open(self, query="", cpu=1.0):
        target = (await self.send("Target.createTarget", {"url": "about:blank"}))["targetId"]
        sid = (await self.send("Target.attachToTarget", {"targetId": target, "flatten": True}))["sessionId"]
        for m in ("Runtime.enable", "Page.enable"):
            await self.send(m, session=sid)
        if cpu != 1:
            await self.send("Emulation.setCPUThrottlingRate", {"rate": cpu}, session=sid)
        await self.send("Page.navigate", {"url": self.base + "index.html" + (f"?{query}" if query else "")}, session=sid)
        return sid

    def lines(self, sid):
        return [t for s, t, _ in self.console if s == sid]

    async def wait_for(self, sid, pattern, timeout=300, after=0):
        rx, end = re.compile(pattern), time.time() + timeout
        while time.time() < end:
            for t in self.lines(sid)[after:]:
                if rx.search(t):
                    return t
            await asyncio.sleep(0.1)
        raise TimeoutError(f"never saw /{pattern}/; last lines: {self.lines(sid)[-8:]}")

    async def shot(self, sid, path):
        data = (await self.send("Page.captureScreenshot", {"format": "png"}, session=sid))["data"]
        Path(path).write_bytes(base64.b64decode(data))
        return path

    async def click(self, sid, x, y):
        for e in burst_events([(x, y)]):
            await self.send("Input.dispatchMouseEvent", e, session=sid)

    async def burst(self, sid, points):
        await asyncio.gather(*[self.send("Input.dispatchMouseEvent", e, session=sid) for e in burst_events(points)])


async def boot(args):
    async with Browser(args.build) as b:
        sid = await b.open("bootlog", args.cpu)
        await b.wait_for(sid, r"^GAME OK")
        origin = (await b.send("Runtime.evaluate", {"expression": "performance.timeOrigin", "returnByValue": True}, session=sid))["result"]["value"]
        print(f"CPU {args.cpu}x")
        for s, text, ts in b.console:
            if s == sid and (text.startswith("BOOT") or text.startswith("GAME OK")):
                print(f"{ts - origin:8.0f} ms  {text[:140]}")


async def navpairs(args):
    """Returns the number of screen toggles per pair, printing the ?navlog lines."""
    results = {}
    async with Browser(args.build) as b:
        sid = await b.open("navlog", 1)
        await b.wait_for(sid, r"^GAME OK")
        n = len(b.lines(sid)); await b.click(sid, *END_TURN); await b.wait_for(sid, r"^TURN ", after=n)
        await asyncio.sleep(1)
        if args.cpu != 1:
            await b.send("Emulation.setCPUThrottlingRate", {"rate": args.cpu}, session=sid)
        await b.click(sid, *WHY); await asyncio.sleep(3)                      # Why open
        for label, second in (("Why+Why", WHY), ("Why+map-under-panel", MAP_UNDER_WHY), ("Why+option", OPTION)):
            n = len(b.lines(sid))
            await b.burst(sid, [WHY, second])                                  # both clicks in one frame
            await asyncio.sleep(3)
            lines = [t for t in b.lines(sid)[n:] if t.startswith("NAV") and "raw mouse" not in t]
            results[label] = sum(" toggle " in t for t in lines)
            print(f"--- {label} (one frame): toggles={results[label]}")
            for t in lines:
                print("   ", t)
            if label != "Why+Why":                                             # leave Why open again for the next pair
                await b.click(sid, *WHY); await asyncio.sleep(3)
    return results


async def loadshot(args):
    async with Browser(args.build) as b:
        sid = await b.open("bootlog", args.cpu)
        await b.wait_for(sid, r"BOOT phase=player started|BOOT FAIL")
        print(await b.shot(sid, args.out + "-loading.png"))
        line = await b.wait_for(sid, r"^GAME OK|BOOT FAIL")
        await asyncio.sleep(2)
        print(line)
        print(await b.shot(sid, args.out + "-after.png"))


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("command", choices=["boot", "navpairs", "loadshot"])
    p.add_argument("--build", default=str(REPO / "unity/Builds/Web-disabled"))
    p.add_argument("--cpu", type=float, default=1.0, help="CPU throttling rate (1 = none)")
    p.add_argument("--out", default="headed", help="screenshot path prefix (loadshot)")
    args = p.parse_args(argv)
    asyncio.run({"boot": boot, "navpairs": navpairs, "loadshot": loadshot}[args.command](args))


if __name__ == "__main__":
    main()
