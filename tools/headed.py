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
  wheel     wheel zoom under ?camlog (#43): a touchpad-like swipe, mouse notches in pixels and lines, over the map and a panel

Run from the repo root, e.g.:
  uv run tools/headed.py boot --build unity/Builds/Web-disabled --cpu 8
  uv run tools/headed.py navpairs --build unity/Builds/Web-disabled --cpu 20
  uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --baseline 450 --baseline-first-frame 9000
  uv run tools/headed.py wheel --build unity/Builds/Web-disabled
Coordinates are CSS pixels for a 1568x752 window (the playtest window). Used for playtest 2 (P2-1, P2-4) and the final
review evidence; the EditMode tests are the regression guard, this is the in-browser check.
"""
import argparse
import asyncio
import base64
import math
import statistics
import json
import re
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "tools"))
import web_check  # noqa: E402  (stdlib only)

WINDOW = (1568, 752)
# OPTION: the first option of the centred decision card after the first End Turn (One Army); MAP_UNDER_WHY: a map point under
# the open Why screen, clear of the card (the card hides under a screen and its backdrop blocks the map; #434). All four
# measured on a WebGL capture of the playtest window, this branch at 1a0c3a7; WHY is now derived (left_column_button).
END_TURN, OPTION, MAP_UNDER_WHY = (1516, 42), (608, 520), (1180, 300)


def left_column_button(index: int, window: tuple[int, int] = WINDOW) -> tuple[int, int]:
    """The centre of the left icon column's button `index` (0 Cabinet, 1 Budget, 2 Defence, 3 Why?) in CSS pixels, from
    Statecraft.uss (HUD round 2, #398): the column at left 16 px and top 112 px (the one-row bar), 56 px discs 68 px apart,
    in panel units the PanelSettings scale to the window (1920x1080 reference, match 0.5: the geometric mean of the two
    ratios). Derived, so a moved column is one change in the USS and here, not a re-measured capture."""
    w, h = window
    scale = ((w / 1920) * (h / 1080)) ** 0.5
    return round((16 + 28) * scale), round((112 + 68 * index + 28) * scale)


WHY = left_column_button(3)   # Why? moved from the top bar to the left icon column (#602)


# For `wheel`, in a fixed 1568x752 CSS viewport (Playwright's, from the #43 browser runs): START_NEW, the start screen's
# New Game button; MAP, open map clear of the HUD; PANEL, the End Turn button in the top bar.
WHEEL_VIEWPORT, START_NEW, MAP, PANEL = (1568, 752), (784, 693), (300, 400), (1500, 44)


def pad_swipe() -> list[float]:
    """A touchpad swipe in wheel pixels (zoom in: negative deltaY): a ramp up, a hold, then momentum decaying 12% an
    event down to half a pixel; 31 events, 108.2 px."""
    ramp = [0.5, 1, 2, 4, 6, 8, 8, 8, 8, 8]
    decay = [round(8 * 0.88 ** k, 2) for k in range(1, 40) if 8 * 0.88 ** k >= 0.5]
    return [-d for d in ramp + decay]


# Each gesture: (deltaY list, deltaMode, ms between events). out_ = the same zooming out.
GESTURES = {"pad": (pad_swipe(), 0, 16), "px100": ([-100.0], 0, 0), "lines3": ([-3.0], 1, 0),
            "out_px100": ([100.0], 0, 0), "out_lines3": ([3.0], 1, 0), "px150": ([-150.0], 0, 0), "px200": ([-200.0], 0, 0),
            "px49": ([-49.0], 0, 0)}
# (label, [(gesture, where)]): each case runs on a fresh page.
WHEEL_CASES = [("pad", [("pad", "map")]), ("px100,pad", [("px100", "map"), ("pad", "map")]),
               ("lines3,pad", [("lines3", "map"), ("pad", "map")]),
               ("pad,out_px100", [("pad", "map"), ("out_px100", "map")]), ("pad,out_lines3", [("pad", "map"), ("out_lines3", "map")]),
               ("pad,px100@panel", [("pad", "panel"), ("px100", "panel")]), ("cdp100", [("cdp100", "map")]),
               ("pad,cdp100", [("pad", "map"), ("cdp100", "map")]),
               ("px150,px200,px49", [("px150", "map"), ("px200", "map"), ("px49", "map")])]
WHEEL_STREAM = """async ([x, y, deltas, mode, gap]) => { const c = document.querySelector('canvas');
    for (const d of deltas) { c.dispatchEvent(new WheelEvent('wheel', {deltaY: d, deltaMode: mode, clientX: x, clientY: y,
        bubbles: true, cancelable: true})); if (gap) await new Promise(r => setTimeout(r, gap)); } }"""
# Records what the page sees of every wheel event (deltaMode read first, as WheelInput.jslib does).
WHEEL_SEEN_SCRIPT = """window.__wheelSeen = []; window.addEventListener('wheel', e => { const m = e.deltaMode;
    window.__wheelSeen.push([e.deltaY, m, e.isTrusted]); }, {capture: true, passive: true});"""
CAMERA = re.compile(r"^CAMERA distance=(\d+)")


MOUSE_NOTCH_MIN_PX = 50   # CameraController.MouseNotchMinPixels


def ideal_notches(gesture: str) -> float:
    """What the gesture should zoom, in notches (positive = in): lines / 3; a pixel event of 50 px or more is one notch;
    smaller pixels count 100 to the notch. (Chrome's trusted 100 px notch, cdp100, arrives as 200 px: one notch.)"""
    if gesture == "cdp100":
        return 1.0
    deltas, mode, _ = GESTURES[gesture]
    if mode == 1:
        return -sum(deltas) / 3
    return sum(-math.copysign(1, d) if abs(d) >= MOUSE_NOTCH_MIN_PX else -d / 100 for d in deltas)


def gesture_notches(d0: float, distances: list[float]) -> tuple[float, float]:
    """(total, largest single frame) in notches (one notch is 10%: d1 = d0 * 0.9^n) from the start distance and the
    distance logged in each frame that changed it."""
    total, largest, prev = 0.0, 0.0, d0
    for d in distances:
        step = math.log(d / prev) / math.log(0.9)
        largest, prev = max(largest, abs(step)), d
    if distances:
        total = math.log(distances[-1] / d0) / math.log(0.9)
    return total, largest


def burst_events(points):
    """Mouse events for clicks at `points`, in order: move, press, release each. Sent at once, they land in one frame."""
    return [{"type": t, "x": x, "y": y, "button": "left", "clickCount": 1}
            for (x, y) in points for t in ("mouseMoved", "mousePressed", "mouseReleased")]


BOOT_PHASE = re.compile(r"BOOT phase=(.+?) at=(\d+) ms dt=(\d+) ms")


def browser_flags(profile: str, width: int, height: int, dpr: float | None = None, uncapped: bool = False,
                  keep_awake: bool = False) -> list[str]:
    """Chrome flags for a headed run. dpr forces the device pixel ratio (1: the same pixels as a headless run; 2: the
    visual v1 fill-rate stress proxy); uncapped lifts the display's frame-rate cap for frame-time profiles; keep_awake
    stops Chrome throttling the window when it is in the background or covered (profiles and boot timings)."""
    flags = ["--use-angle=metal", "--remote-debugging-port=0", f"--user-data-dir={profile}", "--no-first-run",
             "--no-default-browser-check", f"--window-size={width},{height}"]
    if keep_awake:
        flags += web_check.KEEP_AWAKE
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


def allowed_growth(baseline, budget=None) -> float:
    """The boot budget's allowed growth over the baseline: budget when given, else max(300 ms, 50% of the baseline)."""
    return budget if budget is not None else max(300.0, 0.5 * baseline)


def over_budget(spans, baseline, budget=None) -> tuple[float, bool]:
    """(median span, whether it exceeds baseline + allowed_growth); never over without a baseline."""
    median = statistics.median(spans)
    return median, baseline is not None and median > baseline + allowed_growth(baseline, budget)


# Installed before the page's own scripts: on the player's "GAME OK" line it waits two animation frames (the frame that
# draws the new map, shader compiles included, and the next) and records the time since navigation start.
FIRST_FRAME_SCRIPT = """(() => {
  const log = console.log;
  console.log = function (...args) {
    if (window.__scFirstFrame === undefined && typeof args[0] === "string" && args[0].startsWith("GAME OK")) {
      window.__scFirstFrame = null;
      requestAnimationFrame(() => requestAnimationFrame(() => { window.__scFirstFrame = performance.now(); }));
    }
    return log.apply(this, args);
  };
})();"""


class Browser:
    def __init__(self, build: Path, port=8766, width=WINDOW[0], height=WINDOW[1], dpr=None, uncapped=False,
                 viewport=None, keep_awake=False):
        """viewport (w, h) fixes the page's CSS viewport over CDP before it loads (deviceScaleFactor = dpr or 1), so the
        player renders exactly that size even when the screen clamps the window (review ruling 1)."""
        self.build, self.port, self.size = Path(build), port, (width, height)
        self.dpr, self.uncapped, self.viewport, self.keep_awake = dpr, uncapped, viewport, keep_awake
        self.profile = tempfile.mkdtemp(prefix="statecraft-headed-")
        self.next_id, self.pending, self.console = 0, {}, []

    async def __aenter__(self):
        import websockets  # lazy: the normal tools test run does not install it
        self.server = web_check.serve(self.build, self.port)
        self.base = f"http://localhost:{self.port}/"
        w, h = self.size
        flags = browser_flags(self.profile, w, h, self.dpr, self.uncapped, self.keep_awake)
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

    async def open(self, query="", cpu=1.0, init_script=None):
        target = (await self.send("Target.createTarget", {"url": "about:blank"}))["targetId"]
        sid = (await self.send("Target.attachToTarget", {"targetId": target, "flatten": True}))["sessionId"]
        for m in ("Runtime.enable", "Page.enable"):
            await self.send(m, session=sid)
        if cpu != 1:
            await self.send("Emulation.setCPUThrottlingRate", {"rate": cpu}, session=sid)
        if self.viewport is not None:
            await self.send("Emulation.setDeviceMetricsOverride", web_check.viewport_override(*self.viewport, self.dpr or 1), session=sid)
        if init_script is not None:
            await self.send("Page.addScriptToEvaluateOnNewDocument", {"source": init_script}, session=sid)
        await self.send("Page.navigate", {"url": self.base + "index.html" + (f"?{query}" if query else "")}, session=sid)
        return sid

    def lines(self, sid):
        return [t for s, t, _ in self.console if s == sid]

    async def wait_for(self, sid, pattern, timeout=300, after=0, stall=None):
        """The first console line matching pattern; TimeoutError after timeout seconds, or (stall) after stall seconds
        without any new console line, so a throttled or hung page fails loudly instead of waiting out the timeout."""
        rx, end = re.compile(pattern), time.time() + timeout
        seen, last = len(self.lines(sid)), time.time()
        while time.time() < end:
            lines = self.lines(sid)
            for t in lines[after:]:
                if rx.search(t):
                    return t
            if len(lines) != seen:
                seen, last = len(lines), time.time()
            elif stall is not None and time.time() - last > stall:
                raise TimeoutError(f"STALLED: no console line for {stall:.0f} s waiting for /{pattern}/; last lines: {lines[-8:]}")
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
    """?bootlog timings, args.runs times (each in a fresh profile: a cold load) in a kept-awake window with a 1920x1080
    viewport, then two medians: the map-build span (heightmap decoded to map built) and the first frame (navigation
    start to the second animation frame after GAME OK: download, scene load, map build and the first draw with its
    shader compiles). 1 when either exceeds its baseline + the allowed growth (--budget, default max(300 ms, 50%))."""
    builds, firsts = [], []
    for run in range(args.runs):
        async with Browser(args.build, width=web_check.VIEWPORT[0], height=web_check.VIEWPORT[1],
                           viewport=web_check.VIEWPORT, keep_awake=True) as b:
            sid = await b.open("bootlog", args.cpu, init_script=FIRST_FRAME_SCRIPT)
            await b.wait_for(sid, r"^GAME OK", stall=args.stall)
            origin = (await b.send("Runtime.evaluate", {"expression": "performance.timeOrigin", "returnByValue": True}, session=sid))["result"]["value"]
            first = None
            for _ in range(300):
                first = (await b.send("Runtime.evaluate", {"expression": "window.__scFirstFrame", "returnByValue": True}, session=sid))["result"].get("value")
                if first:
                    break
                await asyncio.sleep(0.1)
            if not first:
                raise RuntimeError("no first frame after GAME OK (the console hook did not fire)")
            print(f"CPU {args.cpu}x, run {run + 1} of {args.runs}")
            for s, text, ts in b.console:
                if s == sid and (text.startswith("BOOT") or text.startswith("GAME OK")):
                    print(f"{ts - origin:8.0f} ms  {text[:140]}")
            span = map_build_ms(boot_phases(b.lines(sid)))
            print(f"BOOT map build {span} ms, first frame {first:.0f} ms")
            builds.append(span)
            firsts.append(round(first))
    code = 0
    for name, spans, baseline in (("map_build_ms", builds, args.baseline), ("first_frame_ms", firsts, args.baseline_first_frame)):
        median, over = over_budget(spans, baseline, args.budget)
        print(f"BOOT {name} median={median:.0f} runs={len(spans)} cpu={args.cpu:g}")
        if over:
            print(f"OVER BUDGET: {name} {median:.0f} ms > baseline {baseline:.0f} + {allowed_growth(baseline, args.budget):.0f} ms")
            code = 1
    return code


async def profile(args):
    """?vprofile (visual v1) in a headed, kept-awake window with a 1920x1080 viewport at --dpr, frame rate uncapped;
    prints the PROFILE lines. 1 when a line is missing, a run is at another res= (ruling 1), or the page stalls."""
    async with Browser(args.build, width=1920, height=1080, dpr=args.dpr, uncapped=True,
                       viewport=web_check.VIEWPORT, keep_awake=True) as b:
        sid = await b.open("vprofile", args.cpu)
        try:
            await b.wait_for(sid, r"^PROFILE visual done", timeout=args.timeout, stall=args.stall)
        except TimeoutError as e:
            print(f"FAIL: {e}")
        lines = b.lines(sid)
        for line in lines:
            if line.startswith("PROFILE"):
                print(line)
        errors = web_check.profile_errors(lines, web_check.expected_res(*web_check.VIEWPORT, args.dpr))
        for e in errors:
            print(f"FAIL: {e}")
    return 1 if errors else 0


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


async def wheel(args):
    """Each WHEEL_CASES case on a fresh page under ?camlog: returns {label: [(gesture, total, largest frame, seen)]},
    printing a line per gesture. Gestures are sent to the canvas by dispatchEvent (cdp100: one trusted 100 px notch
    through CDP's mouseWheel); "seen" is what the page saw of them ([deltaY, deltaMode, trusted] per event)."""
    results = {}
    for label, steps in WHEEL_CASES:
        async with Browser(args.build, viewport=WHEEL_VIEWPORT) as b:
            sid = await b.open("camlog", init_script=WHEEL_SEEN_SCRIPT)
            await b.wait_for(sid, r"^GAME OK")
            await asyncio.sleep(3)
            await b.click(sid, *START_NEW)
            await b.wait_for(sid, r"^CAMERA ")
            await asyncio.sleep(2)
            out = []
            for gesture, where in steps:
                x, y = MAP if where == "map" else PANEL
                await b.send("Input.dispatchMouseEvent", {"type": "mouseMoved", "x": x, "y": y}, session=sid)
                await asyncio.sleep(1)
                await b.send("Runtime.evaluate", {"expression": "window.__wheelSeen.length = 0"}, session=sid)
                cams = [CAMERA.match(t) for t in b.lines(sid)]
                d0 = float([m for m in cams if m][-1].group(1))
                n = len(b.lines(sid))
                if gesture == "cdp100":
                    await b.send("Input.dispatchMouseEvent", {"type": "mouseWheel", "x": x, "y": y, "deltaX": 0, "deltaY": -100}, session=sid)
                else:
                    deltas, mode, gap = GESTURES[gesture]
                    expr = f"({WHEEL_STREAM})({json.dumps([x, y, deltas, mode, gap])})"
                    await b.send("Runtime.evaluate", {"expression": expr, "awaitPromise": True}, session=sid)
                await asyncio.sleep(1.5)
                seen = (await b.send("Runtime.evaluate", {"expression": "window.__wheelSeen", "returnByValue": True}, session=sid))["result"]["value"]
                frames = [float(m.group(1)) for m in (CAMERA.match(t) for t in b.lines(sid)[n:]) if m]
                total, largest = gesture_notches(d0, frames)
                out.append((gesture, total, largest, seen))
                print(f"WHEEL {label} | {gesture}@{where}: {total:+.2f} notches over {len(frames)} frames, largest frame {largest:.2f}"
                      + f" (ideal {ideal_notches(gesture):+.2f})" + (f" (seen {seen})" if gesture == "cdp100" else ""))
            results[label] = out
    return results


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("command", choices=["boot", "navpairs", "loadshot", "profile", "wheel"])
    p.add_argument("--build", default=str(REPO / "unity/Builds/Web-disabled"))
    p.add_argument("--cpu", type=float, default=1.0, help="CPU throttling rate (1 = none)")
    p.add_argument("--out", default="headed", help="screenshot path prefix (loadshot)")
    p.add_argument("--runs", type=int, default=1, help="boot: cold loads to take the median of")
    p.add_argument("--baseline", type=float, help="boot: exit 1 when the median map-build span exceeds this + --budget (ms)")
    p.add_argument("--baseline-first-frame", type=float, help="boot: the same for the first-frame span (ms)")
    p.add_argument("--budget", type=float, help="boot: allowed growth over each baseline (ms; default max(300, 50%%))")
    p.add_argument("--stall", type=float, default=120, help="boot, profile: fail after this many seconds with no console line")
    p.add_argument("--dpr", type=float, default=1.0, help="profile: device pixel ratio (2 = the fill-rate stress proxy)")
    p.add_argument("--timeout", type=float, default=1800, help="profile: seconds to wait for PROFILE visual done")
    args = p.parse_args(argv)
    result = asyncio.run({"boot": boot, "navpairs": navpairs, "loadshot": loadshot, "profile": profile, "wheel": wheel}[args.command](args))
    return result if isinstance(result, int) else 0


if __name__ == "__main__":
    sys.exit(main())
