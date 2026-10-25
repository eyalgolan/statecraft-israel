"""tools/web_check.py: the console-line parser and the static server's headers (no browser needed)."""
import gzip
import http.client
import queue
import re
import socket
import threading
import time
import urllib.request

import pytest

import web_check as wc

# A client connects to the address serve() bound (127.0.0.1), not "localhost": Windows resolves localhost to ::1 first,
# where nothing listens, and with no timeout the connect waits with no end. The timeout bounds every request.
TIMEOUT = 10


def base_url(server) -> str:
    host, port = server.server_address[:2]
    return f"http://{host}:{port}"


@pytest.mark.parametrize("error", [BrokenPipeError, ConnectionResetError])
def test_server_client_disconnects_do_not_print_a_false_failure(error, capsys):
    server = wc.Server(("127.0.0.1", 0), wc.Handler)
    try:
        try:
            raise error("browser closed an outstanding response")
        except error:
            server.handle_error(None, server.server_address)
    finally:
        server.server_close()
    assert capsys.readouterr().err == ""


@pytest.mark.parametrize("error", [ValueError, OSError])
def test_server_unexpected_request_errors_are_still_reported(error, capsys):
    server = wc.Server(("127.0.0.1", 0), wc.Handler)
    try:
        try:
            raise error("unexpected request failure")
        except error:
            server.handle_error(None, server.server_address)
    finally:
        server.server_close()
    report = capsys.readouterr().err
    assert "Traceback" in report
    assert f"{error.__name__}: unexpected request failure" in report


def test_console_lines_are_read_from_their_first_line():
    one = '[1010/025003.375619:INFO:CONSOLE:2] "GOLDEN OK 1950 abc", source: http://localhost:1/index.html (2)'
    assert wc.CONSOLE.search(one).group(1) == "GOLDEN OK 1950 abc"
    # Unity's Debug.Log adds a stack trace, so the message continues on the next stderr lines.
    first = '[1010/030000.1:INFO:CONSOLE:9] "SMOKE OK 5 turns 1949-01-01'
    assert wc.CONSOLE.search(first).group(1) == "SMOKE OK 5 turns 1949-01-01"


def test_precompressed_files_get_content_encoding_and_wasm_its_type(tmp_path):
    (tmp_path / "Build").mkdir()
    (tmp_path / "Build" / "game.wasm.br").write_bytes(b"\x00")
    (tmp_path / "Build" / "game.data.gz").write_bytes(gzip.compress(b"x"))
    (tmp_path / "index.html").write_text("<html></html>")
    server = wc.serve(tmp_path)
    try:
        base = base_url(server)
        r = urllib.request.urlopen(base + "/Build/game.wasm.br", timeout=TIMEOUT)
        assert (r.headers["Content-Encoding"], r.headers["Content-Type"]) == ("br", "application/wasm")
        r = urllib.request.Request(base + "/Build/game.data.gz", headers={"Accept-Encoding": "identity"})
        assert urllib.request.urlopen(r, timeout=TIMEOUT).headers["Content-Encoding"] == "gzip"
        assert urllib.request.urlopen(base + "/index.html", timeout=TIMEOUT).headers["Content-Encoding"] is None
    finally:
        server.shutdown()
        server.server_close()


def test_a_burst_of_connections_while_the_accept_thread_is_starved_is_served(tmp_path):
    """The boot fetches every StreamingAssets file at once over HTTP/1.0 (a new connection each), up to 6 at a time per
    host in Chrome. On a loaded machine the accept thread can lag; macOS answers a connection that overflows the listen
    backlog with a reset, which the player sees as a failed fetch (BOOT FAIL <file>: Unknown Error). Holding the accept
    thread while a burst arrives must not lose any connection."""
    (tmp_path / "a.yaml").write_text("a: 1\n")
    server = wc.serve(tmp_path)
    gate, accept = threading.Event(), server.get_request
    server.get_request = lambda: (gate.wait(10), accept())[1]    # the starved accept thread
    socks, errors = [], {}
    try:
        for i in range(16):
            s = socket.socket()
            socks.append(s)
            s.settimeout(10)
            try:
                s.connect(("127.0.0.1", server.server_address[1]))
                s.sendall(b"GET /a.yaml HTTP/1.0\r\nHost: localhost\r\n\r\n")
            except OSError as e:
                errors[i] = type(e).__name__
        time.sleep(0.2)
        gate.set()
        statuses = []
        for i, s in enumerate(socks):
            try:
                statuses.append(errors.get(i) or s.recv(64).split(b"\r\n")[0].decode())
            except OSError as e:
                statuses.append(type(e).__name__)
        assert statuses == ["HTTP/1.0 200 OK"] * 16
    finally:
        gate.set()
        for s in socks:
            s.close()
        server.shutdown()
        server.server_close()


FAIL = re.compile(r"GOLDEN FAIL|BOOT FAIL")


def line(text):
    return f'[1010/025003.375619:INFO:CONSOLE:2] "{text}", source: http://localhost:1/index.html (2)'


def test_scan_passes_when_every_expected_text_was_seen():
    status, pending, texts = wc.scan([line("GOLDEN OK 1950 a"), line("GOLDEN OK 1952 b")], ["GOLDEN OK 1950", "GOLDEN OK 1952"], FAIL)
    assert (status, pending) == ("pass", []) and texts == ["GOLDEN OK 1950 a", "GOLDEN OK 1952 b"]


def test_scan_fails_on_a_failure_line_and_waits_for_missing_texts():
    status, pending, texts = wc.scan([line("GOLDEN FAIL 1950 x expected y")], ["GOLDEN OK 1950"], FAIL)
    assert status == "fail" and texts == ["GOLDEN FAIL 1950 x expected y"]
    status, pending, _ = wc.scan([line("GOLDEN OK 1950 a")], ["GOLDEN OK 1950", "GOLDEN OK 1952"], FAIL)
    assert (status, pending) == ("pending", ["GOLDEN OK 1952"])


def test_scan_ignores_lines_that_are_not_console_messages():
    status, pending, texts = wc.scan(["[0101/000000:ERROR:gpu] something", "  at Foo.Bar()"], ["X"], FAIL)
    assert (status, pending, texts) == ("pending", ["X"], [])


def test_compressed_scripts_and_data_keep_their_types_and_traversal_is_404(tmp_path):
    (tmp_path / "site").mkdir()
    (tmp_path / "site" / "a.framework.js.gz").write_bytes(gzip.compress(b"x"))
    (tmp_path / "site" / "a.data.br").write_bytes(b"\x00")
    (tmp_path / "secret.txt").write_text("secret")
    server = wc.serve(tmp_path / "site")
    try:
        base = base_url(server)
        js = urllib.request.urlopen(base + "/a.framework.js.gz", timeout=TIMEOUT)
        assert js.headers["Content-Type"] == "application/javascript"
        data = urllib.request.urlopen(base + "/a.data.br", timeout=TIMEOUT)
        assert data.headers["Content-Type"] == "application/octet-stream"
        conn = http.client.HTTPConnection(*server.server_address[:2], timeout=TIMEOUT)
        for path in ("/../secret.txt", "/%2e%2e/secret.txt"):
            conn.request("GET", path)
            r = conn.getresponse(); r.read()
            assert r.status == 404, path
    finally:
        server.shutdown()
        server.server_close()


def run_watch(lines, exit_code=None, expect=("GOLDEN OK",), timeout=5.0, late=()):
    """Drives wc.watch with a fake browser: exit_code is what poll() returns (None = still running); late lines
    arrive only after the reader thread is joined (output written just before the exit)."""
    inbox = queue.Queue()
    for l in lines:
        inbox.put(l)
    clock = [0.0]

    def join_reader():
        for l in late:
            inbox.put(l)

    return wc.watch(inbox, lambda: exit_code, join_reader, list(expect), FAIL, timeout,
                    now=lambda: clock[0], sleep=lambda d: clock.__setitem__(0, clock[0] + d))


def test_a_pass_line_written_just_before_the_exit_is_a_pass():
    assert run_watch([], exit_code=0, late=[line("GOLDEN OK 1950 a")]) == 0
    assert run_watch([line("GOLDEN OK 1950 a")], exit_code=0) == 0


def test_an_exit_without_the_expected_lines_is_browser_exited(capsys):
    assert run_watch([line("something else")], exit_code=3) == 1
    assert "BROWSER EXITED code 3" in capsys.readouterr().out


def test_a_failure_line_fails_and_a_silent_running_browser_times_out(capsys):
    assert run_watch([line("GOLDEN FAIL 1950 a expected b")]) == 1
    assert "FAIL: GOLDEN FAIL" in capsys.readouterr().out
    assert run_watch([], exit_code=None) == 1
    assert "TIMEOUT" in capsys.readouterr().out


def test_find_chrome_takes_chrome_path_when_set(tmp_path, monkeypatch):
    exe = tmp_path / "chrome.exe"
    exe.write_bytes(b"")
    monkeypatch.setenv("CHROME_PATH", str(exe))
    assert wc.find_chrome() == exe


def test_find_chrome_refuses_a_chrome_path_that_is_not_a_file(tmp_path, monkeypatch):
    monkeypatch.setenv("CHROME_PATH", str(tmp_path / "missing"))
    try:
        wc.find_chrome()
    except SystemExit as e:
        assert "CHROME_PATH is not a file" in str(e)
    else:
        raise AssertionError("expected SystemExit")


def test_find_chrome_without_chrome_path_picks_the_newest_revision(tmp_path, monkeypatch):
    # PR #289 review: the override must not change the default lookup, which returns the highest chromium-<rev>.
    monkeypatch.delenv("CHROME_PATH", raising=False)
    monkeypatch.setattr(wc.Path, "home", classmethod(lambda cls: tmp_path))
    found = []
    for rev in ("999", "1223", "1100"):
        exe = tmp_path / "Library" / "Caches" / wc.CHROME_GLOB.replace("chromium-*", f"chromium-{rev}").replace("*", "x")
        exe.parent.mkdir(parents=True, exist_ok=True)
        exe.write_bytes(b"")
        found.append(exe)
    assert wc.find_chrome() == found[1]


# A start buttons line as StartScreenPresenter logs it under ?navlog (a save: Continue shows), and the CSS pixels each
# centre maps to in the 1920x1080 viewport.
LAID_OUT = ("NAV f=88 t=9625 start buttons start-continue=0.3750,0.9278 start-new-game=0.4479,0.9278 "
            "start-star=0.5521,0.9278 start-listen=0.4229,0.8704")
AT = {"start-continue": (720, 1002), "start-new-game": (860, 1002), "start-star": (1060, 1002), "start-listen": (812, 940)}


def _buttons(text=LAID_OUT, wait=0.0):
    buttons = wc.StartButtons((1920, 1080), wait=wait, sleep=lambda s: None)
    buttons.feed(text)
    return buttons


def test_start_buttons_maps_the_logged_centres_onto_the_viewport():
    buttons = _buttons()
    assert {name: buttons.point(name) for name in AT} == AT
    phone = wc.StartButtons((390, 844))
    phone.feed("NAV f=1 t=2 start buttons start-new-game=0.5,0.75")
    assert phone.point("start-new-game") == (195, 633)


def test_start_buttons_keeps_only_the_latest_layout_and_forgets_it_on_clear():
    buttons = _buttons()
    buttons.feed("NAV f=90 t=9700 start buttons start-new-game=0.5000,0.9000")    # the row moved: Continue is gone
    assert buttons.point("start-new-game") == (960, 972)
    buttons.feed("console: start buttons start-new-game=0.1,0.1")                  # not a NAV line: ignored
    buttons.feed("RECORDING playing audio/declaration.wav (9.29 s, at 0.20 s)")
    assert buttons.point("start-new-game") == (960, 972)
    with pytest.raises(ValueError, match="START SCREEN FAIL no centre for start-continue .*logged: start-new-game"):
        buttons.point("start-continue")
    buttons.clear()
    with pytest.raises(ValueError, match="no start buttons line; the page needs [?]navlog"):
        buttons.point("start-new-game")


def test_start_buttons_waits_for_a_line_the_reader_thread_brings():
    t = [0.0]
    buttons = wc.StartButtons((1920, 1080), wait=10, clock=lambda: t[0])

    def sleep(seconds):
        t[0] += seconds
        if t[0] >= 3:
            buttons.feed(LAID_OUT)
    buttons.sleep = sleep
    assert buttons.point("start-new-game") == AT["start-new-game"] and 3 <= t[0] < 3.2


def test_start_buttons_fails_a_centre_outside_the_window():
    with pytest.raises(ValueError, match="start-star is outside the window"):
        _buttons("NAV f=1 t=2 start buttons start-star=1.0200,0.9000").point("start-star")


def test_page_query_adds_navlog_only_to_the_start_screen_runs():
    assert wc.page_query(_args("--startscreen", "new")) == "navlog"
    assert wc.page_query(_args("--startscreen", "new", "--query", "lang=he")) == "lang=he&navlog"
    assert wc.page_query(_args("--persistence")) == "navlog"
    assert wc.page_query(_args("--startscreen", "recording", "--query", "navlog&lang=he")) == "navlog&lang=he"
    assert wc.page_query(_args("--query", "golden")) == "golden"


class FakeGame:
    """Answers a driver's clicks as ?navlog's game does, through a real ConsoleTail: End turn (either layout's) selects a
    unit while `units` still need orders, else ends the turn: a TURN line in the console EndTurn reads (a driver's own
    TURN lines are fed by the test). The unit card is EN_CARD, its rows mirrored in Hebrew: a probe names the element under
    it (a "map" one is taken by the map), and a click on Skip skips a unit."""

    def __init__(self, units=0, card=None, hebrew=False, answers=True):
        self.tail, self.units, self.answers = wc.ConsoleTail(), units, answers
        self.card, self.hebrew, self.actions, self.turn = card or EN_CARD, hebrew, [], 1

    def nav(self, what):
        self.tail.feed(f"NAV f=1 t=1 {what}")

    def at(self, x, y):
        if self.hebrew:
            x = wc.INFO_PANEL_LEFT + wc.INFO_PANEL_RIGHT - x
        inside = wc.INFO_PANEL_LEFT <= x <= wc.INFO_PANEL_RIGHT and y >= 1080 - wc.INFO_PANEL_BOTTOM - wc.INFO_PANEL_MAX_HEIGHT
        return next((name for (x0, x1, y0, y1), name in self.card if x0 <= x <= x1 and y0 <= y <= y1),
                    "info-region" if inside else "top-bar")

    def __call__(self, action):
        self.actions.append(action)
        kind, _, at = action.partition(":")
        if not self.answers or not at:
            return
        x, y = (int(v) for v in at.split(","))
        if kind == "click" and (x, y) in (wc.END_TURN, wc.END_TURN_HE):
            self.nav("ui click count=1 target=end-turn")
            if self.units == 0:
                self.tail.feed(f"TURN {self.turn} 1948-06-15 hash bb")
            self.turn += self.units == 0
        elif kind == "probe":
            target = self.at(x, y)
            if target == "map":                      # a press the UI does not take: the map's hex click
                self.nav(f"map click at ({x}, {1080 - y}) overUi=False")
            else:
                self.nav(f"ui pointerdown id=0 at ({x}, {y}) target={target}")
        elif kind == "click":
            target = self.at(x, y)
            self.nav(f"ui click count=1 target={target}")
            self.units -= target == "info-skip"


# The unit card of the RC build at 1920x1080 (English): ((x0, x1, y0, y1), the element there); elsewhere the description.
EN_CARD = [((84, 536, 1010, 1064), "info-reason"), ((96, 200, 976, 1002), "info-skip"), ((206, 290, 976, 1002), "info-attack"),
           ((96, 300, 930, 956), "info-defence"), ((306, 380, 930, 956), "info-move"), ((84, 536, 920, 1009), "VisualElement")]
# Skip second in its line: found along the line, not at its start.
SECOND_CARD = [((84, 536, 1010, 1064), "info-reason"), ((96, 180, 976, 1002), "info-move"), ((186, 300, 976, 1002), "info-skip"),
               ((84, 536, 960, 1009), "VisualElement")]
NO_SKIP_CARD = [((84, 536, 1010, 1064), "info-reason"), ((96, 180, 976, 1002), "info-move"), ((84, 536, 960, 1009), "VisualElement")]


def _quiet():
    """A PersistenceCheck's game, buttons and console."""
    game = FakeGame()
    return game, _buttons(), game.tail


def _quiet_start():
    """An English New game StartScreenCheck's game, recording, hebrew, buttons and console."""
    game = FakeGame()
    return game, False, False, _buttons(), game.tail


def _end_turn(game, **kw):
    return wc.EndTurn(game, game.tail, game.hebrew, settle=lambda s: None, answer=0.05, turn_wait=0.05, **kw)


def test_end_turn_skips_each_unit_it_selects_then_ends_the_turn(capsys):
    game = FakeGame(units=3)
    assert _end_turn(game)() == 3 and game.units == 0
    clicks = [a for a in game.actions if a.startswith("click:")]
    assert clicks == _clicks(wc.END_TURN, (110, 998)) * 3 + _clicks(wc.END_TURN), "only End turn and Skip are left-clicked"
    probes = [a for a in game.actions if a.startswith("probe:")]
    assert probes.count("probe:110,998") == 3 and len(probes) == 7, "Skip is looked for once, then probed where it was"
    assert "END TURN the turn ended after 3 Skip(s)" in capsys.readouterr().out


def test_end_turn_with_no_unit_needing_orders_is_one_click():
    game = FakeGame()
    assert _end_turn(game)() == 0 and game.actions == _clicks(wc.END_TURN)


@pytest.mark.parametrize("hebrew", [False, True])
def test_end_turn_finds_skip_along_its_line_in_either_layout(hebrew):
    game = FakeGame(units=1, card=SECOND_CARD, hebrew=hebrew)
    assert _end_turn(game)() == 1
    x = int([a for a in game.actions if a.startswith("click:")][1].split(":")[1].split(",")[0])
    assert (wc.INFO_PANEL_LEFT + wc.INFO_PANEL_RIGHT - x if hebrew else x) in range(186, 301)
    assert game.actions[-1] == "click:" + ",".join(map(str, wc.END_TURN_HE if hebrew else wc.END_TURN))


def test_end_turn_fails_when_the_turn_never_ends():
    game = FakeGame(units=100)
    with pytest.raises(ValueError, match="END TURN FAIL End turn still selected a unit needing orders after 2 Skips"):
        _end_turn(game, max_skips=2)()
    assert game.units == 98


def test_end_turn_fails_on_a_card_without_skip():
    with pytest.raises(ValueError, match="card shows no info-skip .*order rows at y \\[998\\]"):
        _end_turn(FakeGame(units=1, card=NO_SKIP_CARD))()


def test_end_turn_fails_when_its_click_misses_end_turn():
    with pytest.raises(ValueError, match="END TURN FAIL the click at \\(1840, 45\\) did not press end-turn"):
        _end_turn(FakeGame(answers=False))()


def test_a_click_that_does_not_press_skip_fails():
    game = FakeGame(units=1)
    probe_at = game.at
    game.at = lambda x, y: probe_at(x, y) if game.actions[-1].startswith("probe:") else "info-hold"   # the card moved
    with pytest.raises(ValueError, match="END TURN FAIL the click at \\(110, 998\\) did not press info-skip"):
        _end_turn(game)()


def test_a_probe_that_reaches_the_map_fails_at_once():
    game = FakeGame(units=1, card=[((0, 2000, 0, 2000), "map")])
    with pytest.raises(ValueError, match="END TURN FAIL a press at \\(110, 1038\\) reached the map"):
        _end_turn(game)()
    assert sum(a.startswith("probe:") for a in game.actions) == 1


def test_console_tail_waits_for_a_match_after_the_mark_from_another_thread():
    tail = wc.ConsoleTail()
    tail.feed("NAV f=1 t=1 ui click count=1 target=end-turn")
    since = tail.mark()
    assert tail.wait(wc.NAV_CLICK, since, 0, "end-turn") is None, "lines before the mark do not count"
    threading.Timer(0.05, tail.feed, ["NAV f=2 t=2 ui click count=1 target=info-skip"]).start()
    assert tail.wait(wc.NAV_CLICK, since, 5, "info-skip").group(1) == "info-skip"
    assert tail.wait(wc.NAV_DOWN, since, 0) is None


def test_watch_does_not_stall_while_a_driver_waits_on_lines_that_keep_coming(capsys):
    inbox, t = queue.Queue(), [0.0]
    inbox.put(line("GAME OK 1948-05-15 turn 0 hash aa"))

    def on_text(text):
        if text.startswith("GAME OK"):
            t[0] += 30                      # EndTurn's Skips took 30 s, the page logging all along
            inbox.put(line("TURN 1 1948-06-15 hash bb"))
        return False
    rc = wc.watch(inbox, lambda: None, lambda: None, ["TURN 1"], re.compile("BOOT FAIL"), 300, now=lambda: t[0],
                  sleep=lambda s: t.__setitem__(0, t[0] + s), on_text=on_text, stall=20)
    assert rc == 0, capsys.readouterr().out


def _committed_turn(check, generation, date, turn, digest):
    receipt = f"{generation} {date} turn {turn} hash {digest}"
    check.feed("AUTOSAVE PENDING " + receipt)
    check.feed(f"TURN {turn} {date} hash {digest}")
    check.feed("AUTOSAVE OK " + receipt)


def test_persistence_check_reloads_after_a_settled_and_an_immediate_autosave(capsys):
    sleeps, game = [], FakeGame()
    actions = game.actions
    check = wc.PersistenceCheck(game, _buttons(), game.tail, sleeps.append)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa00")
    check.feed("TURN 1 1948-06-01 turn 1 hash one")       # malformed diagnostic is ignored
    _committed_turn(check, 1, "1948-06-01", 1, "aaa1")
    assert check.buttons.centres == {}, "a reload forgets the old page's layout"
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-06-01 turn 1 hash aaa1")
    _committed_turn(check, 1, "1948-07-01", 2, "aaa2")    # a reload resets the page's counter
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-07-01 turn 2 hash aaa2")
    new_game, cont, end_turn = _clicks(AT["start-new-game"], AT["start-continue"], wc.END_TURN)
    assert actions == [new_game, end_turn, "reload", cont, end_turn, "reload"]
    assert sleeps == [3, 2, 3, 2]     # UI layout only; neither commit uses a settling delay
    assert check.done
    assert "settled reload OK" in capsys.readouterr().out


def _played_to_the_settled_reload(check):
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    _committed_turn(check, 1, "1948-06-15", 1, "bb")


def test_persistence_check_fails_when_the_immediate_reload_did_not_load_the_saved_turn():
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None)
    _played_to_the_settled_reload(check)
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-06-15 turn 1 hash bb")
    _committed_turn(check, 1, "1948-07-15", 2, "cc")
    with pytest.raises(ValueError, match="PERSISTENCE FAIL immediate reload"):
        check.feed("GAME OK 1948-06-15 turn 1 hash bb")


def test_persistence_check_fails_when_the_settled_reload_did_not_load_the_saved_turn():
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None)
    _played_to_the_settled_reload(check)
    with pytest.raises(ValueError, match="PERSISTENCE FAIL settled reload got 1948-05-15 turn 0"):
        check.feed("GAME OK 1948-05-15 turn 0 hash aa")


def test_persistence_check_fails_when_continue_started_a_new_game():
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None)
    _played_to_the_settled_reload(check)
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-06-15 turn 1 hash bb")
    with pytest.raises(ValueError, match="expected turn 2"):
        check.feed("TURN 1 1948-06-15 hash bb")


def test_persistence_check_fails_when_the_first_boot_is_not_a_fresh_game():
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None)
    with pytest.raises(ValueError, match="expected turn 0"):
        check.feed("GAME OK 1948-06-15 turn 1 hash bb")


def test_watch_names_the_unfinished_persistence_run(capsys):
    inbox = queue.Queue()
    inbox.put("GAME OK 1948-05-15 turn 0 hash aa")
    t = [0.0]
    rc = wc.watch(inbox, lambda: None, lambda: None, [], re.compile("BOOT FAIL"), 5, now=lambda: t[0],
                  sleep=lambda s: t.__setitem__(0, t[0] + 1), complete=lambda: False)
    assert rc == 1
    assert "TIMEOUT: never saw the end of the --persistence run" in capsys.readouterr().out


def _clicks(*points):
    return [f"click:{x},{y}" for x, y in points]


def test_start_screen_check_presses_new_game_then_end_turn_and_needs_turn_1(capsys):
    game = FakeGame()
    actions = game.actions
    check = wc.StartScreenCheck(game, recording=False, hebrew=False, buttons=_buttons(), tail=game.tail, settle=lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    assert actions == _clicks(AT["start-new-game"], wc.END_TURN)
    assert not check.done
    check.feed("TURN 1 1948-06-15 hash bb")
    assert check.done
    out = capsys.readouterr().out
    assert "click start-new-game at (860, 1002)" in out and "START SCREEN OK New game reached the map: TURN 1" in out


def test_start_screen_check_fails_at_once_when_the_game_never_says_where_new_game_is():
    check = wc.StartScreenCheck(lambda a: None, False, False, buttons=_buttons(""), tail=wc.ConsoleTail(), settle=lambda s: None)
    with pytest.raises(ValueError, match="START SCREEN FAIL no centre for start-new-game"):
        check.feed("GAME OK 1948-05-15 turn 0 hash aa")


def test_start_screen_check_plays_the_recording_first_and_waits_for_it_hebrew_layout():
    game = FakeGame()
    actions, buttons = game.actions, _buttons()
    check = wc.StartScreenCheck(game, recording=True, hebrew=True, buttons=buttons, tail=game.tail, settle=lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    assert actions == _clicks(AT["start-listen"])
    check.feed("TURN 1 1948-06-15 hash bb")            # no New game before the recording plays
    assert actions == _clicks(AT["start-listen"]) and not check.done
    buttons.feed("NAV f=300 t=19000 start buttons start-new-game=0.5521,0.8000")   # the caption moved the row up
    check.feed("RECORDING playing audio/declaration.wav (9.29 s, at 0.20 s)")
    assert actions == _clicks(AT["start-listen"], (1060, 864), wc.END_TURN_HE)
    check.feed("TURN 1 1948-06-15 hash bb")
    assert check.done


def test_start_screen_check_fails_on_a_saved_game_or_a_wrong_turn():
    with pytest.raises(ValueError, match="START SCREEN FAIL the first boot is turn 3"):
        wc.StartScreenCheck(*_quiet_start(), lambda s: None).feed("GAME OK 1948-08-15 turn 3 hash aa")
    check = wc.StartScreenCheck(*_quiet_start(), lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    with pytest.raises(ValueError, match="START SCREEN FAIL End turn after New game logged turn 2"):
        check.feed("TURN 2 1948-07-15 hash cc")
    assert not check.done


def test_query_value():
    assert wc.query_value("lang=he&smoke", "lang") == "he"
    assert wc.query_value("smoke", "lang") is None
    assert wc.query_value("", "lang") is None


def test_start_screen_check_english_recording_uses_the_english_layout():
    game = FakeGame()
    actions = game.actions
    check = wc.StartScreenCheck(game, recording=True, hebrew=False, buttons=_buttons(), tail=game.tail, settle=lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    check.feed("RECORDING playing audio/declaration.wav (9.29 s, at 0.20 s)")
    assert actions == _clicks(AT["start-listen"], AT["start-new-game"], wc.END_TURN)


def _args(*argv):
    return wc.parser().parse_args(["build", *argv])


def _plan(*argv):
    game = FakeGame()
    driver, fail, plan = wc.run_plan(_args(*argv), game, _buttons(), game.tail)
    if driver is not None:
        driver.settle = driver.end_turn.settle = lambda seconds: None
    return driver, fail, plan


def test_every_run_fails_on_boot_fail_by_default():
    _, fail, plan = _plan()
    assert fail.search("BOOT FAIL manifest.json: timeout") and plan == {}


@pytest.mark.parametrize("mode", ["new", "recording"])
def test_a_start_screen_run_fails_on_boot_fail_exceptions_and_the_recording_whatever_fail_says(mode):
    for argv in ((), ("--fail", "SOMETHING ELSE")):
        _, fail, _ = _plan("--startscreen", mode, *argv)
        for text in ("BOOT FAIL manifest.json: timeout", "NullReferenceException: Object reference not set",
                     "RECORDING failed audio/declaration.wav: no audio"):
            assert fail.search(text), (argv, text)
    assert _plan("--startscreen", mode, "--fail", "SOMETHING ELSE")[1].search("SOMETHING ELSE")


def test_the_start_screen_run_takes_the_hebrew_layout_from_the_query():
    he, _, _ = _plan("--startscreen", "recording", "--query", "lang=he&smoke=0")
    en, _, _ = _plan("--startscreen", "recording")
    assert (he.end_turn.point, en.end_turn.point) == (wc.END_TURN_HE, wc.END_TURN)


def test_persistence_and_start_screen_are_separate_runs():
    with pytest.raises(ValueError, match="separate runs"):
        _plan("--persistence", "--startscreen", "new")


def _run(plan_args, lines, late=()):
    """Feeds `lines` to watch through run_plan's driver; `late` arrive at the first sleep, after them."""
    driver, fail, plan = _plan(*plan_args)
    inbox = queue.Queue()
    for text in lines:
        inbox.put(line(text))
    t, late = [0.0], list(late)

    def sleep(seconds):
        while late:
            inbox.put(line(late.pop(0)))
        t[0] += seconds

    return wc.watch(inbox, lambda: None, lambda: None, [], fail, 60, now=lambda: t[0], sleep=sleep,
                    on_text=lambda text: driver.feed(text) and False, **plan)


BOOTED_AND_TURNED = ["GAME OK 1948-05-15 turn 0 hash aa", "TURN 1 1948-06-15 hash bb"]


def test_a_start_screen_run_passes_after_turn_1_and_names_itself(capsys):
    assert _run(("--startscreen", "new"), BOOTED_AND_TURNED) == 0
    assert "PASS: saw the end of the --startscreen new run" in capsys.readouterr().out


def test_an_exception_logged_just_after_turn_1_still_fails_the_run(capsys):
    assert _run(("--startscreen", "new"), BOOTED_AND_TURNED, late=["NullReferenceException: late presenter"]) == 1
    assert "FAIL: NullReferenceException: late presenter" in capsys.readouterr().out


def test_a_start_screen_run_that_never_reaches_turn_1_fails_naming_itself(capsys):
    assert _run(("--startscreen", "recording"), ["GAME OK 1948-05-15 turn 0 hash aa"]) == 1
    assert "TIMEOUT: never saw the end of the --startscreen recording run" in capsys.readouterr().out


@pytest.mark.parametrize("bad", ["BOOT FAIL manifest.json: timeout", "RECORDING failed audio/declaration.wav: no audio",
                                 "InvalidOperationException: presenter"])
def test_a_failure_line_during_a_start_screen_run_fails_it(bad, capsys):
    assert _run(("--startscreen", "recording"), ["GAME OK 1948-05-15 turn 0 hash aa", bad]) == 1
    assert f"FAIL: {bad}" in capsys.readouterr().out


def test_viewport_parses_width_x_height_css_pixels():
    assert wc.parse_viewport("390x844") == (390, 844)   # the phone the HUD's three-row bar is tuned for
    assert wc.parse_viewport("1920x1080") == (1920, 1080)
    assert wc.parse_viewport(" 390X844 ") == (390, 844)
    for bad in ("", "abc", "39x84", "390x", "x844", "390-844"):
        with pytest.raises(ValueError):
            wc.parse_viewport(bad)


def test_mobile_only_flips_the_emulation_flag():
    assert wc.viewport_override(1920, 1080, 1.0)["mobile"] is False          # the desktop default (review ruling 1)
    phone = wc.viewport_override(390, 844, 3.0, True)
    assert phone == {"width": 390, "height": 844, "deviceScaleFactor": 3.0, "mobile": True}


def test_persistence_check_does_not_reload_turn_two_before_its_commit_ack():
    game = FakeGame()
    actions = game.actions
    check = wc.PersistenceCheck(game, _buttons(), game.tail, lambda seconds: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    check.feed("AUTOSAVE PENDING 1 1948-06-15 turn 1 hash bb")
    check.feed("TURN 1 1948-06-15 hash bb")
    check.feed("AUTOSAVE OK 1 1948-06-15 turn 1 hash bb")
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-06-15 turn 1 hash bb")
    assert actions.count("reload") == 1
    check.feed("AUTOSAVE PENDING 2 1948-07-15 turn 2 hash cc")
    check.feed("TURN 2 1948-07-15 hash cc")
    assert actions.count("reload") == 1, "TURN is not an IndexedDB commit acknowledgement"
    check.feed("AUTOSAVE OK 2 1948-07-15 turn 2 hash cc")
    assert actions.count("reload") == 2
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-07-15 turn 2 hash cc")
    assert check.done


@pytest.mark.parametrize("ack", [
    "AUTOSAVE OK 1 1948-07-15 turn 2 hash cc",  # old generation, identical state
    "AUTOSAVE OK 2 1948-07-15 turn 2 hash dd",  # wrong hash
    "AUTOSAVE OK 2 1948-06-15 turn 1 hash bb",  # old turn/date
])
def test_a_stale_or_mismatched_save_ack_cannot_authorize_reload(ack):
    game = FakeGame()
    actions = game.actions
    check = wc.PersistenceCheck(game, _buttons(), game.tail, lambda seconds: None)
    _played_to_the_settled_reload(check)
    check.buttons.feed(LAID_OUT)  # the reloaded page reports its own layout
    check.feed("GAME OK 1948-06-15 turn 1 hash bb")
    check.feed("AUTOSAVE PENDING 2 1948-07-15 turn 2 hash cc")
    check.feed("TURN 2 1948-07-15 hash cc")
    check.feed(ack)
    assert actions.count("reload") == 1
    assert not check.done
    check.feed("AUTOSAVE OK 2 1948-07-15 turn 2 hash cc")
    assert actions.count("reload") == 2


def test_a_commit_timeout_fails_even_without_any_further_console_line():
    clock = [0.0]
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None, now=lambda: clock[0], save_timeout=5)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    check.feed("TURN 1 1948-06-15 hash bb")
    clock[0] = 5
    with pytest.raises(ValueError, match="no matching IndexedDB commit within 5s"):
        _ = check.done


def test_an_indexeddb_failure_fails_the_driver_loudly():
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None)
    with pytest.raises(ValueError, match="IndexedDB autosave failed"):
        check.feed("AUTOSAVE FAIL 1 1948-06-15 turn 1 hash bb")


def test_commit_ack_before_turn_diagnostic_is_not_lost():
    game = FakeGame()
    actions = game.actions
    check = wc.PersistenceCheck(game, _buttons(), game.tail, lambda seconds: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    check.feed("AUTOSAVE PENDING 1 1948-06-15 turn 1 hash bb")
    check.feed("AUTOSAVE OK 1 1948-06-15 turn 1 hash bb")
    assert "reload" not in actions
    check.feed("TURN 1 1948-06-15 hash bb")
    assert actions.count("reload") == 1


def test_commit_timeout_is_polled_while_an_unrelated_expectation_is_still_missing():
    clock = [0.0]
    check = wc.PersistenceCheck(*_quiet(), lambda seconds: None, now=lambda: clock[0], save_timeout=5)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    check.feed("TURN 1 1948-06-15 hash bb")
    with pytest.raises(ValueError, match="no matching IndexedDB commit within 5s"):
        wc.watch(queue.Queue(), lambda: None, lambda: None, ["UNRELATED"], re.compile("BOOT FAIL"), 20,
                 now=lambda: clock[0], sleep=lambda seconds: clock.__setitem__(0, clock[0] + 1),
                 complete=lambda: check.done)
