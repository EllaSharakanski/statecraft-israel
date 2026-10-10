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
  profile   visual v1's ?vprofile in a headed 1920x1080 window at --dpr (frame rate uncapped)

Run from the repo root, e.g.:
  uv run tools/headed.py boot --build unity/Builds/Web-disabled --cpu 8
  uv run tools/headed.py navpairs --build unity/Builds/Web-disabled --cpu 20
  uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --baseline 5400
Coordinates are CSS pixels for a 1568x752 window (the playtest window). Used for playtest 2 (P2-1, P2-4) and the final
review evidence; the EditMode tests are the regression guard, this is the in-browser check.
"""
import argparse
import asyncio
import base64
import statistics
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


BOOT_PHASE = re.compile(r"BOOT phase=(.+?) at=(\d+) ms dt=(\d+) ms")


def browser_flags(profile: str, width: int, height: int, dpr: float | None = None, uncapped: bool = False) -> list[str]:
    """Chrome flags for a headed run. dpr forces the device pixel ratio (1: the same pixels as a headless run; 2: the
    visual v1 fill-rate stress proxy); uncapped lifts the display's frame-rate cap for frame-time profiles."""
    flags = ["--use-angle=metal", "--remote-debugging-port=0", f"--user-data-dir={profile}", "--no-first-run",
             "--no-default-browser-check", f"--window-size={width},{height}"]
    if dpr is not None:
        flags.append(f"--force-device-scale-factor={dpr:g}")
    if uncapped:
        flags += ["--disable-frame-rate-limit", "--disable-gpu-vsync"]
    return flags + ["about:blank"]


def boot_phases(lines) -> list[tuple[str, int, int]]:
    """(name, at ms, dt ms) of every ?bootlog phase among console lines."""
    return [(m.group(1), int(m.group(2)), int(m.group(3))) for m in map(BOOT_PHASE.search, lines) if m]


def map_build_ms(phases) -> int:
    """The map-build span (visual v1 boot budget): at(map built) - at(heightmap decoded) + dt(heightmap decoded), from the
    end of the content fetch to the map's last phase."""
    first: dict[str, tuple[int, int]] = {}
    for name, at, dt in phases:
        first.setdefault(name, (at, dt))
    for need in ("heightmap decoded", "map built"):
        if need not in first:
            raise ValueError(f"no '{need}' phase (was ?bootlog in the query?)")
    return first["map built"][0] - first["heightmap decoded"][0] + first["heightmap decoded"][1]


def over_budget(spans, baseline, budget) -> tuple[float, bool]:
    """(median span, whether it exceeds baseline + budget); never over without a baseline."""
    median = statistics.median(spans)
    return median, baseline is not None and median > baseline + budget


class Browser:
    def __init__(self, build: Path, port=8766, width=WINDOW[0], height=WINDOW[1], dpr=None, uncapped=False):
        self.build, self.port, self.size = Path(build), port, (width, height)
        self.dpr, self.uncapped = dpr, uncapped
        self.profile = tempfile.mkdtemp(prefix="statecraft-headed-")
        self.next_id, self.pending, self.console = 0, {}, []

    async def __aenter__(self):
        import websockets  # lazy: the normal tools test run does not install it
        self.server = http.server.ThreadingHTTPServer(("127.0.0.1", self.port), functools.partial(web_check.Handler, directory=str(self.build)))
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        self.base = f"http://localhost:{self.port}/"
        w, h = self.size
        flags = browser_flags(self.profile, w, h, self.dpr, self.uncapped)
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
    """?bootlog timings, args.runs times (each in a fresh profile: a cold load), then the median map-build span; 1 when it
    exceeds --baseline + --budget."""
    spans = []
    for run in range(args.runs):
        async with Browser(args.build) as b:
            sid = await b.open("bootlog", args.cpu)
            await b.wait_for(sid, r"^GAME OK")
            origin = (await b.send("Runtime.evaluate", {"expression": "performance.timeOrigin", "returnByValue": True}, session=sid))["result"]["value"]
            print(f"CPU {args.cpu}x, run {run + 1} of {args.runs}")
            for s, text, ts in b.console:
                if s == sid and (text.startswith("BOOT") or text.startswith("GAME OK")):
                    print(f"{ts - origin:8.0f} ms  {text[:140]}")
            span = map_build_ms(boot_phases(b.lines(sid)))
            print(f"BOOT map build {span} ms")
            spans.append(span)
    median, over = over_budget(spans, args.baseline, args.budget)
    print(f"BOOT map_build_ms median={median:.0f} runs={len(spans)}")
    if over:
        print(f"OVER BUDGET: {median:.0f} ms > baseline {args.baseline:.0f} + {args.budget:.0f} ms")
    return 1 if over else 0


async def profile(args):
    """?vprofile (visual v1) in a headed 1920x1080 window at --dpr, frame rate uncapped; prints the PROFILE lines."""
    async with Browser(args.build, width=1920, height=1080, dpr=args.dpr, uncapped=True) as b:
        sid = await b.open("vprofile", args.cpu)
        await b.wait_for(sid, r"^PROFILE visual done", timeout=args.timeout)
        for line in b.lines(sid):
            if line.startswith("PROFILE"):
                print(line)
    return 0


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
    p.add_argument("command", choices=["boot", "navpairs", "loadshot", "profile"])
    p.add_argument("--build", default=str(REPO / "unity/Builds/Web-disabled"))
    p.add_argument("--cpu", type=float, default=1.0, help="CPU throttling rate (1 = none)")
    p.add_argument("--out", default="headed", help="screenshot path prefix (loadshot)")
    p.add_argument("--runs", type=int, default=1, help="boot: cold loads to take the median of")
    p.add_argument("--baseline", type=float, help="boot: exit 1 when the median map-build span exceeds this + --budget (ms)")
    p.add_argument("--budget", type=float, default=1000, help="boot: allowed growth over --baseline (ms)")
    p.add_argument("--dpr", type=float, default=1.0, help="profile: device pixel ratio (2 = the fill-rate stress proxy)")
    p.add_argument("--timeout", type=float, default=1800, help="profile: seconds to wait for PROFILE visual done")
    args = p.parse_args(argv)
    result = asyncio.run({"boot": boot, "navpairs": navpairs, "loadshot": loadshot, "profile": profile}[args.command](args))
    return result if isinstance(result, int) else 0


if __name__ == "__main__":
    sys.exit(main())
