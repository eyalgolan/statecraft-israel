# /// script
# requires-python = ">=3.11"
# dependencies = ["websockets==15.0.1"]
# ///
"""Serve a Unity WebGL build on http://localhost and open it in Chrome; pass when the console shows every expected line,
fail on a failure line or a timeout. Uses Chrome for Testing from the Playwright cache (macOS only, no npm needed) in
--headless=new mode (the cached chrome-headless-shell has no WebGL for Unity): console messages reach stderr with
--enable-logging=stderr --v=1 as INFO:CONSOLE... "text", source: ...

Usage (from the repo root):
  uv run tools/web_check.py unity/Builds/Web-disabled --query "golden" --expect "GOLDEN OK hash1950 8578966e171c213fbc314ab87c8292817ff9a94790e372b4e4792c00e1708f61" --expect "GOLDEN OK hash1952 385ba3b58a15d15f88bb27c9cc0620a7e7af011e51037f00fea70177644b3e83"
  uv run tools/web_check.py unity/Builds/Web-disabled --query "smoke-orders" --expect "SMOKE ORDERS OK 1948-06-15 bf0b41cce7efc8ad9f1f518b0f2c26736b5c9a25d477afc075a7f69dfbcad42b"
  (the smoke-orders golden: one Move given through the unit card, docs/determinism.md)
Options: --query Q (page URL query, without '?'), --expect TEXT (repeatable), --fail REGEX (default: GOLDEN FAIL|SMOKE FAIL|BOOT FAIL),
--timeout SECONDS (default 300), --stall SECONDS (fail when no console message arrives for that long; 0 = off),
--swiftshader (software WebGL when there is no GPU), --dpr N (default 1), --shots DIR (reassemble the player's
"SHOT" console lines, from ?shot, into DIR/<pose>.png; a "SHOT done" with an unfinished shot fails the run),
--persistence (play one normal turn, wait for its matching IndexedDB commit, reload and Continue, then play another
turn and reload as soon as its matching commit arrives; both reloads must report the saved date, turn and hash), --startscreen new|recording (#908: on a fresh
profile, the real start screen every player sees first: "new" presses New game, "recording" first presses Play the
recording and waits for RECORDING playing; then End turn must log TURN 1, first pressing "Skip the opening" when the
opening (#530) takes End turn's click, then "Skip this turn" on each unit End
turn selects while units still need orders (EndTurn; --persistence ends its turns so too); the map must draw Israeli and
enemy unit tokens on turn 1 (the new game) and turn 2 (after End turn), else UNITS FAIL (#2056, UnitsOnMap); any console
Exception, RECORDING failed or BOOT FAIL fails the run; add --query lang=he for the Hebrew layout). Both runs add ?navlog and click the start screen's
buttons where its "start buttons" line says they are (StartButtons).
--url URL opens that page (the itch.io game frame) instead of serving a build; --profile DIR keeps that Chrome profile
between runs; --first-save drives --persistence only to its first durable autosave and prints PERSISTENCE saved <date> turn <n> hash <h>.
The page is opened over the DevTools protocol with its viewport fixed to 1920x1080 at the DPR before it loads
(Emulation.setDeviceMetricsOverride: --window-size alone gives a 1920x937 viewport in --headless=new), and the run
fails on any "res=" line, or any screenshot, of another size. --viewport WxH checks another window - 390x844 is the
phone the HUD's three-row bar is tuned for - and --mobile adds the phone emulation (meta viewport, touch events).
Headed runs go through tools/headed.py.
Pre-compressed builds: *.br and *.gz files are served with Content-Encoding, *.wasm(.br/.gz) as application/wasm.
Brotli needs a secure context; http://localhost is one."""
import argparse
import asyncio
import base64
import functools
import hashlib
import http.server
import json
import os
import re
import subprocess
import sys
import queue
import shutil
import tempfile
import threading
import time
import urllib.parse
from pathlib import Path

# A console message's first line; warnings and errors carry a stack trace on later lines (ProjectSetup sets none for
# Debug.Log), so the closing quote and ", source:" may be far below. Only the first line is matched.
CONSOLE = re.compile(r'INFO:CONSOLE[^\]]*\] "(.*?)(?:", source: .*)?$')
CHROME_GLOB = "ms-playwright/chromium-*/chrome-mac*/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"
SHOT_BEGIN = re.compile(r"^SHOT (\S+) begin (\d+) ([0-9a-f]{64})$")
SHOT_PART = re.compile(r"^SHOT (\S+) (\d+)/(\d+) (\S+)$")
VIEWPORT = (1920, 1080)
# A background or occluded window is throttled (no frames, slow timers): measurement windows stay awake (review ruling 2).
KEEP_AWAKE = ["--disable-backgrounding-occluded-windows", "--disable-renderer-backgrounding",
              "--disable-background-timer-throttling"]
RES = re.compile(r"\bres=(\d+x\d+)\b")


def viewport_override(width: int, height: int, dpr: float, mobile: bool = False) -> dict:
    """Emulation.setDeviceMetricsOverride parameters: exactly width x height CSS pixels at dpr, desktop (review ruling 1)
    unless --mobile asks for the phone emulation (the meta viewport and touch events a phone sends)."""
    return {"width": width, "height": height, "deviceScaleFactor": dpr, "mobile": mobile}


def parse_viewport(text: str) -> tuple[int, int]:
    """A --viewport value, WIDTHxHEIGHT in CSS pixels ("390x844"); the default VIEWPORT keeps the desktop ruling."""
    m = re.fullmatch(r"(\d{3,4})x(\d{3,4})", text.strip().lower())
    if not m:
        raise ValueError(f"--viewport wants WIDTHxHEIGHT in CSS pixels, e.g. 390x844, got {text!r}")
    return int(m.group(1)), int(m.group(2))


def expected_res(width: int, height: int, dpr: float) -> str:
    """The player's Screen size for a width x height viewport at dpr (Unity renders at the browser's DPR)."""
    return f"{round(width * dpr)}x{round(height * dpr)}"


def res_error(text: str, expected: str) -> str | None:
    """An error when a console line states a res= other than expected; None otherwise."""
    m = RES.search(text)
    return None if m is None or m.group(1) == expected else f"res={m.group(1)}, expected res={expected}: {text}"


def profile_errors(lines, expected: str) -> list[str]:
    """What is wrong with a ?vprofile run's console lines: no PROFILE run lines, a run at another size, no done line."""
    runs = [l for l in lines if l.startswith("PROFILE visual=")]
    errors = [e for e in (res_error(l, expected) for l in runs) if e]
    if not runs:
        errors.append("no PROFILE visual= lines")
    if "PROFILE visual done" not in lines:
        errors.append("no 'PROFILE visual done' line")
    return errors


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
        if base.endswith((".data", ".r16z")):
            return "application/octet-stream"
        return super().guess_type(base)

    def log_message(self, fmt, *args):
        if os.environ.get("WEB_CHECK_VERBOSE"):
            print("http:", fmt % args)


# A Chrome log line ("[pid:tid:MMDD/HHMMSS.micro:LEVEL:file.cc:line] text") ending a raw stderr line.
SPLICED = re.compile(r"\[\d+:\d+:\d{4}/\d{6}\.\d+:[A-Z]+\d*:[^\]\s]+:\d+\] [^\n]*\n?$")


class Rejoin:
    """Repairs console lines another thread's log line was spliced into. Chrome writes a long console message (a SHOT
    part is 8000 characters) to stderr in pieces, and any module's --v=1 line can land between two pieces: the console
    line then ends with that log line, and the message's tail (with its '", source:' end) starts the next stderr line.
    feed(raw) cuts the log line out and joins the tail back on; it returns the lines ready to scan (none while a line is
    open). A line that ends in its source, or that holds no spliced log line (a multi-line message's first line), passes
    through unchanged."""

    def __init__(self):
        self.head: str | None = None

    def feed(self, raw: str) -> list[str]:
        if self.head is not None:
            raw, self.head = self.head + raw, None
        m = CONSOLE.search(raw)
        if m and '", source: ' not in raw[m.start(1):]:
            s = SPLICED.search(raw, m.start(1))
            if s:
                self.head = raw[:s.start()]
                return []
        return [raw]

    def flush(self) -> list[str]:
        head, self.head = self.head, None
        return [head] if head is not None else []


class ShotCollector:
    """Reassembles the PNGs Statecraft.Game.Map.VisualShots logs: "SHOT <pose> begin <n> <sha256>" then n lines
    "SHOT <pose> <i>/<n> <base64>" (any order). A finished shot is checked against its SHA-256 and written to
    <out_dir>/<pose>.png (out_dir None: checked, not written)."""

    def __init__(self, out_dir: Path | None, size: tuple[int, int] | None = None):
        self.out_dir, self.size = out_dir, size
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
            if int(m.group(2)) < 1:
                raise ValueError(f"SHOT {m.group(1)}: a begin line with no parts")
            self.expect[m.group(1)] = (int(m.group(2)), m.group(3))
            self.parts[m.group(1)] = {}
            return True
        m = SHOT_PART.match(text)
        if not m:
            return False
        name, i = m.group(1), int(m.group(2))
        if name not in self.expect:
            raise ValueError(f"SHOT {name}: part {i}/{m.group(3)} before its begin line")
        n = self.expect[name][0]
        if int(m.group(3)) != n or not 1 <= i <= n:
            raise ValueError(f"SHOT {name}: part {i}/{m.group(3)} does not fit its begin line ({n} parts)")
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
        if self.size is not None:
            w, h = int.from_bytes(png[16:20], "big"), int.from_bytes(png[20:24], "big")
            if (w, h) != self.size:
                raise ValueError(f"SHOT {name}: {w}x{h}, expected {self.size[0]}x{self.size[1]}")
        if self.out_dir is not None:
            self.out_dir.mkdir(parents=True, exist_ok=True)
            path = self.out_dir / f"{name}.png"
            path.write_bytes(png)
            self.written.append(path)
            print(f"shot: wrote {path}")


def find_chrome() -> Path:
    """The newest Chrome for Testing in the Playwright cache (macOS, arm64 or x64), by numeric revision.
    CHROME_PATH, when set, overrides the lookup and works on any platform."""
    override = os.environ.get("CHROME_PATH")
    if override:
        path = Path(override)
        if not path.is_file():
            sys.exit(f"CHROME_PATH is not a file: {path}")
        return path

    found = list(Path.home().joinpath("Library", "Caches").glob(CHROME_GLOB))
    if not found:
        sys.exit("Chrome for Testing not found under ~/Library/Caches/ms-playwright/chromium-<rev>/chrome-mac*"
                 " (macOS only; install with: npx playwright install chromium,"
                 " or set CHROME_PATH to a Chrome for Testing executable)")

    def revision(path: Path) -> int:
        m = re.search(r"chromium-(\d+)", str(path))
        return int(m.group(1)) if m else -1
    return max(found, key=revision)


def chrome_flags(profile: str, swiftshader: bool, dpr: float = 1.0) -> list[str]:
    """Headless Chrome at the device pixel ratio (1 unless asked), frame rate uncapped (so frame times are not the
    display's), never throttled, with a DevTools port for the viewport override."""
    # --vmodule: Chrome's page content agent logs at --v=1 from another thread, often enough to split a long console
    # line (a SHOT part) on stderr every run; Rejoin repairs such a split from any module.
    flags = ["--headless=new", "--enable-logging=stderr", "--v=1", "--vmodule=ai_page_content_agent=0", "--no-first-run", "--no-default-browser-check",
             "--autoplay-policy=no-user-gesture-required", "--window-size=1920,1080", f"--force-device-scale-factor={dpr:g}",
             "--disable-frame-rate-limit", "--disable-gpu-vsync", *KEEP_AWAKE, "--remote-debugging-port=0",
             f"--user-data-dir={profile}"]
    if swiftshader:
        flags += ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]
    return flags


class Page:
    """Opens url in a new tab of the Chrome using `profile`, over the DevTools protocol, with the viewport fixed before
    the first load; the connection (and with it the override) is held on a background thread until close()."""

    def __init__(self, profile: str, url: str, width: int, height: int, dpr: float, mobile: bool = False):
        self.profile, self.url, self.metrics = profile, url, viewport_override(width, height, dpr, mobile)
        self.ready, self.stop = threading.Event(), threading.Event()
        self.commands: queue.Queue = queue.Queue()
        self.error: BaseException | None = None
        self.thread = threading.Thread(target=lambda: asyncio.run(self._run()), daemon=True)

    def open(self, timeout: float = 60) -> None:
        self.thread.start()
        if not self.ready.wait(timeout):
            raise ValueError(f"could not open the page over the DevTools protocol within {timeout:.0f} s")
        if self.error is not None:
            raise ValueError(f"could not open the page over the DevTools protocol: {self.error}")

    def close(self) -> None:
        self.stop.set()
        self.thread.join(5)

    def command(self, name: str) -> None:
        """Runs one action on the page's CDP thread and waits for it: "reload", "click:X,Y" (a left click at CSS
        pixels X, Y of the viewport), or "probe:X,Y" (a middle-button click there: EndTurn's probe, which no button, map
        or camera acts on, while ?navlog names the element under it)."""
        done, result = threading.Event(), []
        self.commands.put((name, done, result))
        if not done.wait(10):
            raise ValueError(f"browser action {name} did not finish")
        if result:
            raise ValueError(f"browser action {name} failed: {result[0]}")

    async def _run(self) -> None:
        try:
            import websockets  # lazy: the unit tests do not install it
            port_file = Path(self.profile) / "DevToolsActivePort"
            for _ in range(600):
                if port_file.exists() and len(port_file.read_text().split()) >= 2:
                    break
                await asyncio.sleep(0.1)
            port, path = port_file.read_text().split()[:2]
            async with websockets.connect(f"ws://127.0.0.1:{port}{path}", max_size=None) as ws:
                ids = iter(range(1, 1 << 30))

                async def call(method: str, params: dict | None = None, session: str | None = None) -> dict:
                    n = next(ids)
                    await ws.send(json.dumps({"id": n, "method": method, "params": params or {},
                                              **({"sessionId": session} if session else {})}))
                    while True:
                        r = json.loads(await ws.recv())
                        if r.get("id") == n:
                            if "error" in r:
                                raise RuntimeError(f"{method}: {r['error']}")
                            return r.get("result", {})
                target = (await call("Target.createTarget", {"url": "about:blank"}))["targetId"]
                sid = (await call("Target.attachToTarget", {"targetId": target, "flatten": True}))["sessionId"]
                await call("Emulation.setDeviceMetricsOverride", self.metrics, sid)
                await call("Page.navigate", {"url": self.url}, sid)
                self.ready.set()
                while not self.stop.is_set():
                    try:
                        name, done, result = self.commands.get_nowait()
                    except queue.Empty:
                        name = None
                    if name is not None:
                        try:
                            if name.startswith("click:"):
                                x, y = (float(v) for v in name[len("click:"):].split(","))
                                await call("Input.dispatchMouseEvent", {"type": "mouseMoved", "x": x, "y": y}, sid)
                                for kind in ("mousePressed", "mouseReleased"):
                                    await call("Input.dispatchMouseEvent", {"type": kind, "x": x, "y": y, "button": "left",
                                                                            "clickCount": 1}, sid)
                            elif name.startswith("probe:"):
                                x, y = (float(v) for v in name[len("probe:"):].split(","))
                                await call("Input.dispatchMouseEvent", {"type": "mouseMoved", "x": x, "y": y}, sid)
                                for kind in ("mousePressed", "mouseReleased"):
                                    await call("Input.dispatchMouseEvent", {"type": kind, "x": x, "y": y, "button": "middle",
                                                                            "clickCount": 1}, sid)
                            elif name == "reload":
                                await call("Page.reload", {"ignoreCache": True}, sid)
                            else:
                                raise ValueError(f"unknown command {name}")
                        except Exception as e:
                            result.append(e)
                        finally:
                            done.set()
                    try:
                        await asyncio.wait_for(ws.recv(), 0.1)
                    except asyncio.TimeoutError:
                        pass
        except Exception as e:  # reported by open(); after it, the browser closing ends the thread
            if not self.ready.is_set():
                self.error = e
                self.ready.set()


class Server(http.server.ThreadingHTTPServer):
    """The static server. socketserver listens with a backlog of 5, and macOS resets a connection that overflows it (no
    SYN retry). The boot fetches every StreamingAssets file at once, each over a new HTTP/1.0 connection, up to 6 at a
    time per host in Chrome; on a loaded machine the accept thread lags, a connection is reset and the boot fails
    (BOOT FAIL <file>: Unknown Error). 128 is macOS's kern.ipc.somaxconn."""
    request_queue_size = 128

    def handle_error(self, request, client_address):
        # Closing the browser cancels outstanding responses. Keep other server failures visible.
        if isinstance(sys.exc_info()[1], (BrokenPipeError, ConnectionResetError)):
            return
        super().handle_error(request, client_address)


def serve(root: Path, port: int = 0) -> Server:
    server = Server(("127.0.0.1", port), functools.partial(Handler, directory=str(root)))
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


# The top bar's End turn (clickable while the first event card is open) in CSS pixels of the fixed 1920x1080 viewport,
# measured on headless captures, and in the Hebrew layout (?lang=he: the top bar is mirrored, so End turn is at the left).
# Keys do not work here: Enter sent over CDP before any click reaches no Unity button, and after New game the first event
# card, not End turn, takes the focus.
END_TURN, END_TURN_HE = (1840, 45), (80, 45)
# The start screen's buttons are not measured: their row changes as buttons are added (#1178's Star on GitHub moved New
# game off the viewport's centre), so the game says where they are. Under ?navlog, StartScreenPresenter.ButtonCentres logs
# each shown button's centre as a fraction of the window, again whenever the layout moves it:
#   NAV f=4 t=4094 start buttons start-new-game=0.4167,0.9278 start-star=0.5833,0.9278 start-listen=0.4229,0.8704
NEW_GAME, CONTINUE, PLAY_RECORDING = "start-new-game", "start-continue", "start-listen"
START_BUTTONS = re.compile(r"^NAV f=\d+ t=\d+ start buttons (.+)$")
START_BUTTON = re.compile(r"(\S+)=(\d+(?:\.\d+)?),(\d+(?:\.\d+)?)")
NAVLOG = "navlog"
GAME_STATE = re.compile(r"^GAME OK (\d{4}-\d{2}-\d{2}) turn (\d+) hash ([0-9a-f]+)$")
TURN_STATE = re.compile(r"^TURN (\d+) (\d{4}-\d{2}-\d{2}) hash ([0-9a-f]+)$")
SAVE_STATE = re.compile(r"^AUTOSAVE (PENDING|OK|FAIL) ([1-9]\d*) (\d{4}-\d{2}-\d{2}) turn (\d+) hash ([0-9a-f]+)$")


class StartButtons:
    """Where the start screen's buttons are: the latest "start buttons" line (START_BUTTONS), fed from the stderr reader
    thread, so a click uses the layout logged while its driver was waiting. `point` maps a button's centre onto the
    viewport in CSS pixels, waiting up to `wait` seconds for it, and fails at once (not a stall) when it never comes."""

    def __init__(self, viewport: tuple[int, int], wait: float = 10.0, clock=time.monotonic, sleep=time.sleep):
        self.viewport, self.wait, self.clock, self.sleep = viewport, wait, clock, sleep
        self.lock, self.centres = threading.Lock(), {}

    def feed(self, text: str) -> None:
        m = START_BUTTONS.match(text)
        if m:
            centres = {name: (float(x), float(y)) for name, x, y in START_BUTTON.findall(m.group(1))}
            with self.lock:
                self.centres = centres

    def clear(self) -> None:
        """Forgets the layout: before a reload, so the new page's line is waited for."""
        with self.lock:
            self.centres = {}

    def point(self, name: str) -> tuple[int, int]:
        deadline = self.clock() + self.wait
        while True:
            with self.lock:
                centre, known = self.centres.get(name), sorted(self.centres)
            if centre is not None:
                break
            if self.clock() >= deadline:
                raise ValueError(f"START SCREEN FAIL no centre for {name} within {self.wait:.0f} s (logged: "
                                 f"{', '.join(known) or 'no start buttons line'}; the page needs ?{NAVLOG})")
            self.sleep(0.1)
        fx, fy = centre
        if not (0 < fx < 1 and 0 < fy < 1):
            raise ValueError(f"START SCREEN FAIL {name} is outside the window: centre at {fx:g}, {fy:g} of it")
        return round(fx * self.viewport[0]), round(fy * self.viewport[1])


class ConsoleTail:
    """The console messages so far, numbered, fed from the stderr reader thread (like StartButtons), so a driver that is
    waiting for the game's answer to its click sees it come in. `wait` returns the first match after mark `since`."""

    KEEP = 5000

    def __init__(self):
        self.cond, self.lines, self.count = threading.Condition(), [], 0

    def feed(self, text: str) -> None:
        with self.cond:
            self.count += 1
            self.lines.append((self.count, text))
            del self.lines[:-self.KEEP]
            self.cond.notify_all()

    def mark(self) -> int:
        with self.cond:
            return self.count

    def wait(self, pattern: re.Pattern, since: int, timeout: float, group: str | None = None) -> re.Match | None:
        """The first message numbered after `since` that `pattern` matches (with group(1) == `group` when given), waiting
        up to `timeout` seconds for it; None when none comes."""
        deadline = time.monotonic() + timeout
        with self.cond:
            while True:
                for n, text in self.lines:
                    m = pattern.match(text) if n > since else None
                    if m and (group is None or m.group(1) == group):
                        return m
                left = deadline - time.monotonic()
                if left <= 0:
                    return None
                self.cond.wait(left)


# The tokens the map draws, as Statecraft.Game.Map.Units.UnitLayer logs them under ?navlog (#2056) whenever the drawn
# tokens change and on each new turn: the state's turn, the Israeli and the enemy (every other side) totals, then each enemy
# side's count:
#   NAV f=412 t=20311 units drawn turn=0 israel=10 enemy=2 egypt=1 jordan=1
UNITS_DRAWN = re.compile(r"^NAV f=\d+ t=\d+ units drawn turn=(\d+) israel=(\d+) enemy=(\d+)(?: .*)?$")
# Seconds the check waits for both sides' tokens: an enemy token is drawn only once its side's figure has streamed in (#665).
UNITS_WAIT = 60.0


class UnitsOnMap:
    """Owner ruling on #2056 (1 November 2026): a run fails unless the map draws Israeli and enemy unit tokens on turns 1
    and 2 (the 31 October nightly drew no enemy token: its enemy tokens waited for a figure never requested). Keeps the latest
    UNITS_DRAWN line, fed from the stderr reader thread (like StartButtons). `require` waits up to `wait` seconds for the
    latest line to be for the given sim turn with both totals above zero. No such line (a build from before #2056, a page
    without ?navlog, a line in another format) fails: the check never passes without its signal."""

    def __init__(self, wait: float = UNITS_WAIT):
        self.wait, self.cond = wait, threading.Condition()
        self.latest: tuple[int, int, int, str] | None = None   # (turn, israel, enemy, the line)

    def feed(self, text: str) -> None:
        m = UNITS_DRAWN.match(text)
        if m:
            with self.cond:
                self.latest = (int(m.group(1)), int(m.group(2)), int(m.group(3)), text)
                self.cond.notify_all()

    def require(self, turn: int, what: str) -> tuple[int, int]:
        """(Israeli, enemy) tokens drawn on sim turn `turn` (the player's `what`); a ValueError naming what is missing."""
        deadline = time.monotonic() + self.wait
        with self.cond:
            while True:
                latest = self.latest
                if latest is not None and latest[0] == turn and latest[1] > 0 and latest[2] > 0:
                    print(f"UNITS OK {what}: israel={latest[1]} enemy={latest[2]}")
                    return latest[1], latest[2]
                left = deadline - time.monotonic()
                if left <= 0:
                    break
                self.cond.wait(left)
        within = f"within {self.wait:g} s"
        if latest is None:
            raise ValueError(f"UNITS FAIL {what}: no 'units drawn' line {within} (the page needs ?{NAVLOG}, and a build "
                             f"whose UnitLayer logs it, #2056)")
        if latest[0] != turn:
            raise ValueError(f"UNITS FAIL {what}: no 'units drawn' line for turn {turn} {within}; the last was {latest[3]}")
        missing = " and ".join(name for name, n in (("Israeli", latest[1]), ("enemy", latest[2])) if n == 0)
        raise ValueError(f"UNITS FAIL {what}: no {missing} unit tokens drawn {within}: {latest[3]}")


# What ?navlog's NavEventLog says of a press and a click: the deepest UI element under the pointer, by name.
NAV_DOWN = re.compile(r"^NAV f=\d+ t=\d+ ui pointerdown id=\d+ at \([^)]*\) target=(\S+)(?: .*)?$")   # NavEventLog adds path= and modals= after target
NAV_CLICK = re.compile(r"^NAV f=\d+ t=\d+ ui click count=\d+ target=(\S+)$")
# A left press the map takes: not over the UI, or (#1617) on the unit card's text over a lit hex of its armed Move pick,
# which moves the unit. So EndTurn finds Skip with the middle button, which only the UI's pointer events see.
NAV_MAP = re.compile(r"^NAV f=\d+ t=\d+ map click at \([^)]*\) overUi=False$")
# The unit card is the info panel (Statecraft.uss .info-panel: left 84 px, bottom 16 px, width 452 px, in both languages;
# in Hebrew only its rows mirror). Its order buttons wrap in one row container (Game.uxml), whose lines each start at
# the body's start edge (left, or right in Hebrew), under the unit's description lines and over the reason and notes.
# Probes keep PROBE_INSET px inside the panel's frame.
INFO_PANEL_LEFT, INFO_PANEL_RIGHT, INFO_PANEL_BOTTOM, INFO_PANEL_MAX_HEIGHT, PROBE_INSET = 84, 536, 16, 640, 26
ORDER_ROW = {"build-camp", "build-housing", "info-defence", "info-primary-orders", "info-move", "info-fortify", "info-hold",
             "info-skip", "info-attack", "info-send-home", "info-reinforce"}
ABOVE_ORDERS = {"info-title", "info-subtitle", "info-region", "info-settlements", "info-localities", "info-placement"}
SKIP = "info-skip"
# The opening (#530): after New game, once opening.yaml is reviewed, three stills cover the whole window, top bar included
# (Statecraft.uss .opening-open), so End turn's click reaches the opening until the player presses "Skip the opening" (or
# Next through it). Its Back, Next and Skip row (Game.uxml .opening-actions) is centred under prose whose height changes
# with the panel and the language, so Skip is found where the game draws it, like the unit card's: down two columns 30 px
# apart (a button is wider than that, the row's gaps narrower) to the row, then along it.
OPENING, OPENING_SKIP = "opening-", "opening-skip"
OPENING_ROW = {"opening-back", "opening-next", OPENING_SKIP}
# Well over the units a turn has needing orders (8 on a new game, 9 on turn 2, 0.3.0 RC): a turn that never ends fails.
MAX_SKIPS = 40


class EndTurn:
    """Ends the turn as a player does since #1527 U4: End turn selects a unit that still needs orders (HudPresenter) instead
    of ending the turn, so press that unit's "Skip this turn" and End turn again, until the game logs the next TURN line, at
    most `max_skips` times (GameRoot.Smoke's loop, through the browser). The unit card's buttons move with its text, so
    Skip is found where the game draws it: a middle-button probe reports the element under it (?navlog NAV_DOWN) and acts
    on nothing (a left press on the card's text can move the unit, NAV_MAP), then a left click presses Skip. When the
    opening (#530) takes the first End turn click, its own Skip is pressed the same way and End turn clicked again. A
    missed End turn, a probe the map takes, a card or an opening with no Skip, or a turn that never ends fails at once.
    Returns the number of units Skipped."""

    def __init__(self, action, tail: ConsoleTail, hebrew: bool, settle=time.sleep, max_skips=MAX_SKIPS, answer=5.0,
                 turn_wait=3.0, viewport=VIEWPORT):
        self.action, self.tail, self.hebrew, self.settle = action, tail, hebrew, settle
        self.max_skips, self.answer, self.turn_wait = max_skips, answer, turn_wait
        self.point = END_TURN_HE if hebrew else END_TURN
        self.viewport, self.bottom = viewport, viewport[1] - INFO_PANEL_BOTTOM
        self.skip_at: tuple[int, int] | None = None  # where Skip was last: cards of one kind keep it there

    def __call__(self) -> int:
        for skips in range(self.max_skips + 1):
            since = self.click_end_turn(opening=skips == 0)   # the opening shows only before a new game's first turn
            if self.tail.wait(TURN_STATE, since, self.turn_wait) is not None:
                print(f"END TURN the turn ended after {skips} Skip(s)")
                return skips
            if skips == self.max_skips:
                break
            self.settle(1)                           # the camera goes to the unit and its card lays out
            if not self.skip(since):
                print(f"END TURN the turn ended after {skips} Skip(s)")
                return skips
        raise ValueError(f"END TURN FAIL End turn still selected a unit needing orders after {self.max_skips} Skips")

    def click_end_turn(self, opening: bool) -> int:
        """Clicks End turn; the console mark before the click that pressed it. With `opening`, a click the opening takes is
        answered as a player does: its Skip (skip_opening), then End turn again."""
        x, y = self.point
        since = self.tail.mark()
        print(f"click end-turn at ({x}, {y})")
        self.action(f"click:{x},{y}")
        if self.tail.wait(NAV_CLICK, since, self.answer, "end-turn") is not None:
            return since
        other = self.tail.wait(NAV_CLICK, since, 0)
        reached = f" (the click reached {other.group(1)})" if other else ""
        if opening and other is not None and other.group(1).startswith(OPENING):
            print(f"END TURN the click reached {other.group(1)}: the opening covers End turn, so press its Skip")
            self.skip_opening()
            self.settle(1)                           # the top bar shows again
            return self.click_end_turn(opening=False)
        raise ValueError(f"END TURN FAIL the click at ({x}, {y}) did not press end-turn within {self.answer:g} s{reached}")

    def skip_opening(self) -> None:
        """Presses the opening's Skip where the game draws it (OPENING_ROW), found with middle-button probes."""
        cx, seen, row = self.viewport[0] // 2, [], None
        for y in range(PROBE_INSET, self.viewport[1] - PROBE_INSET, 10):
            for x in (cx, cx - 30):
                target = self.press(x, y)
                seen.append(target)
                if target in OPENING_ROW:
                    row = (x, y)
                    break
            if row is not None:
                break
        points = [] if row is None else [row] + [(x, row[1]) for dx in range(10, cx, 10) for x in (cx + dx, cx - dx)]
        for x, y in points:
            if (target := self.press(x, y)) == OPENING_SKIP:
                since = self.tail.mark()
                print(f"click {OPENING_SKIP} at ({x}, {y})")
                self.action(f"click:{x},{y}")
                if self.tail.wait(NAV_CLICK, since, self.answer, OPENING_SKIP) is None:
                    raise ValueError(f"OPENING FAIL the click at ({x}, {y}) did not press {OPENING_SKIP} within "
                                     f"{self.answer:g} s")
                return
            seen.append(target)
        raise ValueError(f"OPENING FAIL the opening covers End turn, but no {OPENING_SKIP} was found (its row at y "
                         f"{row[1] if row else 'none'}; pressed {', '.join(dict.fromkeys(seen))})")

    def skip(self, since: int) -> bool:
        """Clicks the shown unit card's Skip; False when the turn ended after all (a late TURN line), so nothing was pressed."""
        if self.skip_at is not None and self.probe(*self.skip_at):
            return True
        x0 = INFO_PANEL_RIGHT - PROBE_INSET if self.hebrew else INFO_PANEL_LEFT + PROBE_INSET
        rows, seen = [], []
        for y in range(self.bottom - PROBE_INSET, self.bottom - INFO_PANEL_MAX_HEIGHT, -10):
            if self.tail.wait(TURN_STATE, since, 0) is not None:
                return False
            target = self.press(x0, y)
            seen.append(target)
            if target == SKIP:
                self.click_skip(x0, y)
                return True
            if target in ORDER_ROW and (not rows or rows[-1] - y > 20):
                rows.append(y)
            elif target in ABOVE_ORDERS and rows:
                break
        step = -20 if self.hebrew else 20
        for y in rows:
            for x in range(x0 + step, INFO_PANEL_LEFT + PROBE_INSET if self.hebrew else INFO_PANEL_RIGHT - PROBE_INSET, step):
                if self.probe(x, y):
                    return True
        raise ValueError(f"END TURN FAIL End turn selected a unit, but its card shows no {SKIP} (order rows at y "
                         f"{rows or 'none'}; pressed {', '.join(dict.fromkeys(seen))})")

    def probe(self, x: int, y: int) -> bool:
        """Clicks Skip when it is at x, y (True)."""
        if self.press(x, y) != SKIP:
            return False
        self.click_skip(x, y)
        return True

    def press(self, x: int, y: int) -> str:
        """The UI element a middle-button probe at x, y reaches."""
        since = self.tail.mark()
        self.action(f"probe:{x},{y}")
        down = self.tail.wait(NAV_DOWN, since, self.answer)
        if self.tail.wait(NAV_MAP, since, 0) is not None:
            raise ValueError(f"END TURN FAIL a press at ({x}, {y}) reached the map, not the unit card")
        if down is None:
            raise ValueError(f"END TURN FAIL a press at ({x}, {y}) reached no UI element within {self.answer:g} s "
                             f"(the page needs ?{NAVLOG})")
        print(f"probe ({x}, {y}): {down.group(1)}")
        return down.group(1)

    def click_skip(self, x: int, y: int) -> None:
        since = self.tail.mark()
        print(f"click {SKIP} at ({x}, {y})")
        self.action(f"click:{x},{y}")
        if self.tail.wait(NAV_CLICK, since, self.answer, SKIP) is None:
            raise ValueError(f"END TURN FAIL the click at ({x}, {y}) did not press {SKIP} within {self.answer:g} s")
        self.skip_at = (x, y)


def click_button(action, buttons: StartButtons, name: str) -> None:
    """A left click on a start-screen button where the game says it is; printed, so a stalled run shows where it clicked."""
    x, y = buttons.point(name)
    print(f"click {name} at ({x}, {y})")
    action(f"click:{x},{y}")


class PersistenceCheck:
    """Drives and checks the two reloads of issue #910. `action` is Page.command (injected, so the unit tests need no
    browser); a reload keeps the page's profile and origin, so its IndexedDB autosave. Each turn ends through EndTurn."""

    def __init__(self, action, buttons: StartButtons, tail: ConsoleTail, settle=time.sleep, now=time.monotonic,
                 save_timeout=15.0, first_save_only=False):
        self.action, self.buttons, self.settle, self.now, self.save_timeout = action, buttons, settle, now, save_timeout
        self.first_save_only = first_save_only   # stop, with the saved line printed, at the first durable commit
        self.end_turn = EndTurn(action, tail, hebrew=False, settle=settle)
        self.stage, self.saved, self._done = "first_boot", None, False
        self.pending, self.acknowledged, self.deadline = None, None, None

    @property
    def done(self) -> bool:
        # watch polls this even when no more console lines arrive.
        if self.deadline is not None and self.now() >= self.deadline:
            raise ValueError(f"PERSISTENCE FAIL no matching IndexedDB commit within {self.save_timeout:g}s: "
                             f"{self.saved}; pending receipt {self.pending}")
        return self._done

    def _reload_when_durable(self) -> None:
        if self.stage not in ("first_commit", "second_commit"):
            return
        if self.pending is None or self.pending[1:] != self.saved or self.acknowledged != self.pending:
            return
        first = self.stage == "first_commit"
        self.deadline = None
        if first and self.first_save_only:
            date, turn, digest = self.saved
            print(f"PERSISTENCE saved {date} turn {turn} hash {digest}")
            self.stage, self._done = "saved", True
            return
        self.stage = "settled_reload" if first else "immediate_reload"
        self.buttons.clear()
        self.action("reload")

    def feed(self, text: str) -> None:
        _ = self.done  # fail a missing acknowledgement before processing later diagnostics
        receipt = SAVE_STATE.match(text)
        if receipt:
            kind, generation, date, turn, digest = receipt.groups()
            value = (int(generation), date, int(turn), digest)
            if kind == "FAIL":
                raise ValueError(f"PERSISTENCE FAIL IndexedDB autosave failed: {value}")
            if kind == "PENDING":
                if self.pending is None or value[0] > self.pending[0]:
                    self.pending, self.acknowledged = value, None
            elif value == self.pending:
                self.acknowledged = value
            self._reload_when_durable()
            return
        game, turn = GAME_STATE.match(text), TURN_STATE.match(text)
        if game:
            state, kind = (game.group(1), int(game.group(2)), game.group(3)), "GAME"
        elif turn:
            state, kind = (turn.group(2), int(turn.group(1)), turn.group(3)), "TURN"
        else:
            return
        if self.stage == "first_boot" and kind == "GAME":
            self._turn(state, 0, "first boot (a fresh profile has no save)")
            self.settle(3)                       # the start screen lays out and takes input
            self.click(NEW_GAME)
            self.settle(2)
            self.stage = "first_turn"
            self.end_turn()
        elif self.stage == "first_turn" and kind == "TURN":
            self._turn(state, 1, "first End turn after New game")
            self.saved, self.stage = state, "first_commit"
            self.deadline = self.now() + self.save_timeout
            self._reload_when_durable()
        elif self.stage == "settled_reload" and kind == "GAME":
            self._same(state, "settled")
            print(f"PERSISTENCE settled reload OK {state[0]} turn {state[1]} hash {state[2]}")
            # A new page starts its generation counter again; old-page receipts cannot acknowledge its save.
            self.pending, self.acknowledged = None, None
            self.settle(3)
            self.click(CONTINUE)
            self.settle(2)
            self.stage = "second_turn"
            self.end_turn()
        elif self.stage == "second_turn" and kind == "TURN":
            self._turn(state, self.saved[1] + 1, "End turn after Continue")
            self.saved, self.stage = state, "second_commit"
            self.deadline = self.now() + self.save_timeout
            self._reload_when_durable()
        elif self.stage == "immediate_reload" and kind == "GAME":
            self._same(state, "immediate")
            print(f"PERSISTENCE immediate reload OK {state[0]} turn {state[1]} hash {state[2]}")
            self._done = True

    def click(self, target: str | tuple[int, int]) -> None:
        if isinstance(target, str):
            click_button(self.action, self.buttons, target)
        else:
            self.action(f"click:{target[0]},{target[1]}")

    @staticmethod
    def _turn(state, expected: int, what: str) -> None:
        if state[1] != expected:
            raise ValueError(f"PERSISTENCE FAIL {what}: turn {state[1]} ({state[0]}), expected turn {expected}")

    def _same(self, actual, pass_name: str) -> None:
        if actual != self.saved:
            date, turn, digest = self.saved
            raise ValueError(f"PERSISTENCE FAIL {pass_name} reload got {actual[0]} turn {actual[1]} hash {actual[2]}; "
                             f"saved {date} turn {turn} hash {digest}; file a follow-up save-persistence bug")


def query_value(query: str, key: str) -> str | None:
    """The value of `key` in a page query without its '?' ("lang=he&smoke" -> "he"); None when absent."""
    values = urllib.parse.parse_qs(query, keep_blank_values=True).get(key)
    return values[0] if values else None


class StartScreenCheck:
    """Issue #908: the start screen of a fresh profile, as a player meets it (no automation flag skips it). New game
    (after Play the recording when `recording`) and End turn (EndTurn: Skip each unit it selects) must reach the map's
    first turn, with Israeli and enemy tokens drawn on the new game (the player's turn 1) and after End turn (turn 2):
    `units` (#2056). `action` is Page.command; `buttons` says where the start screen's buttons are; `tail` is the console."""

    def __init__(self, action, recording: bool, hebrew: bool, buttons: StartButtons, tail: ConsoleTail, units: UnitsOnMap,
                 settle=time.sleep):
        self.action, self.recording, self.buttons, self.settle, self.units = action, recording, buttons, settle, units
        self.end_turn = EndTurn(action, tail, hebrew, settle=settle)
        self.stage, self.done = "boot", False

    def feed(self, text: str) -> None:
        game, turn = GAME_STATE.match(text), TURN_STATE.match(text)
        if self.stage == "boot" and game:
            if int(game.group(2)) != 0:
                raise ValueError(f"START SCREEN FAIL the first boot is turn {game.group(2)}, not a fresh game")
            self.settle(3)                       # the start screen lays out and takes input
            if self.recording:
                self.click(PLAY_RECORDING)
                self.stage = "recording"
            else:
                self.new_game()
        elif self.stage == "recording" and text.startswith("RECORDING playing "):
            print("START SCREEN recording OK")
            self.settle(2)
            self.new_game()
        elif self.stage == "turn" and turn:
            if int(turn.group(1)) != 1:
                raise ValueError(f"START SCREEN FAIL End turn after New game logged turn {turn.group(1)}, not turn 1")
            self.units.require(1, "turn 2 (after End turn)")
            print(f"START SCREEN OK New game reached the map: TURN 1 {turn.group(2)} hash {turn.group(3)}")
            self.done = True

    def new_game(self) -> None:
        self.click(NEW_GAME)
        self.settle(2)
        self.units.require(0, "turn 1 (New game)")
        self.stage = "turn"
        self.end_turn()

    def click(self, target: str | tuple[int, int]) -> None:
        if isinstance(target, str):
            click_button(self.action, self.buttons, target)
        else:
            self.action(f"click:{target[0]},{target[1]}")


def watch(inbox, poll, join_reader, expect, fail, timeout, now=time.time, sleep=time.sleep, on_text=None, stall=0,
          complete=None, complete_what="the end of the --persistence run", grace=0.0) -> int:
    """The check loop: 0 on PASS, 1 on a failure line, on BROWSER EXITED, on TIMEOUT, or (stall > 0) when no console
    message arrives for stall seconds (STALLED). poll() is the browser's returncode (None while running); join_reader()
    waits briefly for the stderr reader thread to hit EOF, so output written just before the exit is still scanned.
    on_text(text) -> True consumes a message (it is not printed). With grace > 0, PASS waits that many seconds after
    the run is complete, still reading, so a failure line that follows the last expected one (an exception logged
    after TURN 1) fails the run instead of being lost when the browser closes."""
    pending, deadline = list(expect), now() + timeout
    last = now()
    passes_at = None
    end = [complete_what] if complete is not None else []

    def missing() -> str:
        return ", ".join(pending + [e for e in end if not complete()]) or "nothing"

    def look():
        nonlocal pending, last, passes_at
        status, pending, texts = scan(drain(inbox), pending, fail)
        if texts:
            last = now()
        for t in texts:
            if on_text is not None and on_text(t):
                continue
            print("console:", t)
        if texts:
            last = now()      # again: a driver (EndTurn) may have waited in on_text while the page kept logging
        if status == "fail":
            print(f"FAIL: {texts[-1]}")
            return 1
        finished = complete is None or complete()  # poll the deadline while other expectations are pending too
        if status == "pass" and finished:
            if passes_at is None:
                passes_at = now() + grace
            if now() >= passes_at:
                print("PASS: saw " + ", ".join(list(expect) + end))
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
            print(f"BROWSER EXITED code {code}; never saw " + missing())
            return 1
        if stall and now() - last > stall:
            print(f"STALLED: no console message for {stall:.0f} s; never saw " + missing())
            return 1
        sleep(0.2)
    print("TIMEOUT: never saw " + missing())
    return 1


# Failure lines every --startscreen run adds to --fail, whatever --fail says (#908).
START_SCREEN_FAILS = r"BOOT FAIL|Exception|RECORDING failed"
# Seconds a scripted run keeps reading after it is complete, before PASS (watch's grace).
DRIVER_GRACE = 3.0


def page_query(a) -> str:
    """The page's query: --query, plus ?navlog for the runs that click the start screen (StartButtons). navlog only logs:
    it skips nothing, so the start screen is still the one a player meets."""
    if not (a.startscreen or a.persistence or a.first_save) or query_value(a.query, NAVLOG) is not None:
        return a.query
    return f"{a.query}&{NAVLOG}" if a.query else NAVLOG


def run_plan(a, command, buttons: StartButtons, tail: ConsoleTail, units: UnitsOnMap):
    """What a run drives and fails on: (driver or None, the failure regex, watch's keyword arguments). `command` is
    Page.command; `buttons`, `tail` and `units` are fed the console by the caller."""
    if (a.persistence or a.first_save) and a.startscreen:
        raise ValueError("--persistence and --startscreen are separate runs")
    if a.startscreen:
        driver = StartScreenCheck(command, a.startscreen == "recording", query_value(a.query, "lang") == "he", buttons, tail,
                                  units)
        fail, what = f"(?:{a.fail})|{START_SCREEN_FAILS}", f"the end of the --startscreen {a.startscreen} run"
    elif a.persistence or a.first_save:
        driver = PersistenceCheck(command, buttons, tail, first_save_only=a.first_save)
        fail, what = a.fail, "the end of the --first-save run" if a.first_save else "the end of the --persistence run"
    else:
        return None, re.compile(a.fail), {}
    return driver, re.compile(fail), {"complete": lambda: driver.done, "complete_what": what, "grace": DRIVER_GRACE}


def parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser()
    ap.add_argument("build", type=Path, nargs="?", help="a Unity WebGL build folder, served on localhost (or use --url)")
    ap.add_argument("--url", help="open this page instead of serving a build (the itch.io game frame); no server starts")
    ap.add_argument("--profile", type=Path,
                    help="use and keep this Chrome profile folder (default: a new temporary one, deleted at the end)")
    ap.add_argument("--first-save", action="store_true",
                    help="drive the persistence run to the first durable autosave, print PERSISTENCE saved <date> turn <n> "
                         "hash <h> and pass, without reloading")
    ap.add_argument("--query", default="")
    ap.add_argument("--expect", action="append", default=[])
    ap.add_argument("--fail", default=r"GOLDEN FAIL|SMOKE FAIL|BOOT FAIL")
    ap.add_argument("--timeout", type=float, default=300)
    ap.add_argument("--swiftshader", action="store_true")
    ap.add_argument("--shots", type=Path)
    ap.add_argument("--dpr", type=float, default=1.0)
    ap.add_argument("--viewport", default=f"{VIEWPORT[0]}x{VIEWPORT[1]}",
                    help="viewport in CSS pixels, WIDTHxHEIGHT (default %(default)s, the desktop ruling); a phone's is 390x844")
    ap.add_argument("--mobile", action="store_true", help="emulate a phone: the meta viewport applies and touch events are sent")
    ap.add_argument("--stall", type=float, default=0)
    ap.add_argument("--persistence", action="store_true")
    ap.add_argument("--startscreen", choices=["new", "recording"])
    return ap


def check_source(ap: argparse.ArgumentParser, a) -> None:
    """Exactly one of the build folder and --url names the page."""
    if (a.build is None) == (a.url is None):
        ap.error("give exactly one of build and --url")


def with_query(url: str, query: str) -> str:
    """`url` with `query` added: after ? or, when the URL already has a query, after &."""
    if not query:
        return url
    return f"{url}{'&' if '?' in url else '?'}{query}"


def pick_profile(a) -> tuple[str, bool]:
    """(the Chrome profile folder, whether the run owns it). --profile is made if missing and is never deleted."""
    if a.profile is not None:
        Path(a.profile).mkdir(parents=True, exist_ok=True)
        return str(a.profile), False
    return tempfile.mkdtemp(prefix="web-check-"), True


def main() -> int:
    ap = parser()
    a = ap.parse_args()
    check_source(ap, a)
    if a.build is not None and not (a.build / "index.html").exists():
        sys.exit(f"{a.build}/index.html not found")
    server, proc, page = None, None, None
    try:
        w, h = parse_viewport(a.viewport)
    except ValueError as e:
        sys.exit(str(e))
    profile, own_profile = pick_profile(a)
    res = expected_res(w, h, a.dpr)
    shots = ShotCollector(a.shots, size=(round(w * a.dpr), round(h * a.dpr)))
    buttons, tail, units = StartButtons((w, h)), ConsoleTail(), UnitsOnMap()

    def on_text(text: str) -> bool:
        err = res_error(text, res)
        if err:
            raise ValueError(err)
        return shots.feed(text)
    try:
        chrome = find_chrome()
        query = page_query(a)
        if a.url:
            url = with_query(a.url, query)
        else:
            server = serve(a.build)
            url = with_query(f"http://localhost:{server.server_address[1]}/index.html", query)
        proc = subprocess.Popen([str(chrome), *chrome_flags(profile, a.swiftshader, a.dpr), "about:blank"],
                                stderr=subprocess.PIPE, stdout=subprocess.DEVNULL, text=True, errors="replace")
        inbox: queue.Queue = queue.Queue()
        rejoin = Rejoin()

        def read():
            for raw in iter(proc.stderr.readline, ""):
                for line in rejoin.feed(raw):
                    console = CONSOLE.search(line)
                    if console:
                        buttons.feed(console.group(1))     # here, not in watch: a driver waiting in settle() sees it
                        tail.feed(console.group(1))        # and one waiting for the game's answer to its click
                        units.feed(console.group(1))       # and one waiting for the map's tokens
                    inbox.put(line)
            for line in rejoin.flush():
                inbox.put(line)
        reader = threading.Thread(target=read, daemon=True)
        reader.start()
        try:
            page = Page(profile, url, w, h, a.dpr, a.mobile)
            page.open()
            print(f"viewport {w}x{h} at DPR {a.dpr:g}{' mobile' if a.mobile else ''} (expect res={res})")
            driver, fail, plan = run_plan(a, page.command, buttons, tail, units)

            def handle_text(text: str) -> bool:
                consumed = on_text(text)
                if driver is not None:
                    driver.feed(text)
                return consumed
            return watch(inbox, proc.poll, lambda: reader.join(2), list(a.expect), fail, a.timeout,
                         on_text=handle_text, stall=a.stall, **plan)
        except ValueError as e:
            print(f"FAIL: {e}")
            return 1
    finally:
        if page is not None:
            page.close()
        if proc is not None:
            proc.terminate()
            try:
                proc.wait(5)
            except subprocess.TimeoutExpired:
                proc.kill(); proc.wait()
        if server is not None:
            server.shutdown(); server.server_close()
        if own_profile:
            shutil.rmtree(profile, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
