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
        base = f"http://localhost:{server.server_address[1]}"
        r = urllib.request.urlopen(base + "/Build/game.wasm.br")
        assert (r.headers["Content-Encoding"], r.headers["Content-Type"]) == ("br", "application/wasm")
        r = urllib.request.Request(base + "/Build/game.data.gz", headers={"Accept-Encoding": "identity"})
        assert urllib.request.urlopen(r).headers["Content-Encoding"] == "gzip"
        assert urllib.request.urlopen(base + "/index.html").headers["Content-Encoding"] is None
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
        base = f"http://localhost:{server.server_address[1]}"
        assert urllib.request.urlopen(base + "/a.framework.js.gz").headers["Content-Type"] == "application/javascript"
        assert urllib.request.urlopen(base + "/a.data.br").headers["Content-Type"] == "application/octet-stream"
        conn = http.client.HTTPConnection("localhost", server.server_address[1])
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


def test_persistence_check_reloads_after_a_settled_and_an_immediate_autosave(capsys):
    actions, sleeps = [], []
    check = wc.PersistenceCheck(actions.append, sleeps.append)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa00")
    check.feed("TURN 1 1948-06-01 turn 1 hash one")       # unrelated malformed diagnostic is ignored
    check.feed("TURN 1 1948-06-01 hash aaa1")
    check.feed("GAME OK 1948-06-01 turn 1 hash aaa1")
    check.feed("TURN 2 1948-07-01 hash aaa2")
    check.feed("GAME OK 1948-07-01 turn 2 hash aaa2")
    new_game, cont, end_turn = (f"click:{x},{y}" for x, y in (wc.NEW_GAME, wc.CONTINUE, wc.END_TURN))
    assert actions == [new_game, end_turn, "reload", cont, end_turn, "reload"]
    assert sleeps == [3, 2, 2, 3, 2]     # no wait between the second TURN and its reload
    assert check.done
    assert "settled reload OK" in capsys.readouterr().out


def test_persistence_check_fails_when_the_immediate_reload_did_not_load_the_saved_turn():
    check = wc.PersistenceCheck(lambda action: None, lambda seconds: None)
    for text in ("GAME OK 1948-05-15 turn 0 hash aa", "TURN 1 1948-06-01 hash bb",
                 "GAME OK 1948-06-01 turn 1 hash bb", "TURN 2 1948-07-01 hash cc"):
        check.feed(text)
    try:
        check.feed("GAME OK 1948-06-01 turn 1 hash bb")
    except ValueError as e:
        assert "PERSISTENCE FAIL immediate reload" in str(e) and "file a follow-up" in str(e)
    else:
        raise AssertionError("a lost immediate autosave must fail")


def _played_to_the_settled_reload(check):
    for text in ("GAME OK 1948-05-15 turn 0 hash aa", "TURN 1 1948-06-15 hash bb"):
        check.feed(text)


def test_persistence_check_fails_when_the_settled_reload_did_not_load_the_saved_turn():
    check = wc.PersistenceCheck(lambda action: None, lambda seconds: None)
    _played_to_the_settled_reload(check)
    with pytest.raises(ValueError, match="PERSISTENCE FAIL settled reload got 1948-05-15 turn 0"):
        check.feed("GAME OK 1948-05-15 turn 0 hash aa")      # the reload came up with no save: a fresh game
    assert not check.done


def test_persistence_check_fails_when_continue_started_a_new_game():
    """A click that misses Continue and starts a new game must not pass: the turn after Continue is the reloaded
    game's next turn (2), not a new game's turn 1."""
    check = wc.PersistenceCheck(lambda action: None, lambda seconds: None)
    _played_to_the_settled_reload(check)
    check.feed("GAME OK 1948-06-15 turn 1 hash bb")
    with pytest.raises(ValueError, match="PERSISTENCE FAIL End turn after Continue: turn 1 .*expected turn 2"):
        check.feed("TURN 1 1948-06-15 hash cc")
    assert not check.done


def test_persistence_check_fails_when_the_first_boot_is_not_a_fresh_game():
    check = wc.PersistenceCheck(lambda action: None, lambda seconds: None)
    with pytest.raises(ValueError, match="PERSISTENCE FAIL first boot"):
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
    actions = []
    check = wc.StartScreenCheck(actions.append, recording=False, hebrew=False, settle=lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    assert actions == _clicks(wc.NEW_GAME, wc.END_TURN)
    assert not check.done
    check.feed("TURN 1 1948-06-15 hash bb")
    assert check.done
    assert "START SCREEN OK New game reached the map: TURN 1" in capsys.readouterr().out


def test_start_screen_check_plays_the_recording_first_and_waits_for_it_hebrew_layout():
    actions = []
    check = wc.StartScreenCheck(actions.append, recording=True, hebrew=True, settle=lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    assert actions == _clicks(wc.PLAY_RECORDING_HE)
    check.feed("TURN 1 1948-06-15 hash bb")            # no New game before the recording plays
    assert actions == _clicks(wc.PLAY_RECORDING_HE) and not check.done
    check.feed("RECORDING playing audio/declaration.wav (9.29 s, at 0.20 s)")
    assert actions == _clicks(wc.PLAY_RECORDING_HE, wc.NEW_GAME, wc.END_TURN_HE)
    check.feed("TURN 1 1948-06-15 hash bb")
    assert check.done


def test_start_screen_check_fails_on_a_saved_game_or_a_wrong_turn():
    with pytest.raises(ValueError, match="START SCREEN FAIL the first boot is turn 3"):
        wc.StartScreenCheck(lambda a: None, False, False, lambda s: None).feed("GAME OK 1948-08-15 turn 3 hash aa")
    check = wc.StartScreenCheck(lambda a: None, False, False, lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    with pytest.raises(ValueError, match="START SCREEN FAIL End turn after New game logged turn 2"):
        check.feed("TURN 2 1948-07-15 hash cc")
    assert not check.done


def test_query_value():
    assert wc.query_value("lang=he&smoke", "lang") == "he"
    assert wc.query_value("smoke", "lang") is None
    assert wc.query_value("", "lang") is None


def test_start_screen_check_english_recording_uses_the_english_layout():
    actions = []
    check = wc.StartScreenCheck(actions.append, recording=True, hebrew=False, settle=lambda s: None)
    check.feed("GAME OK 1948-05-15 turn 0 hash aa")
    check.feed("RECORDING playing audio/declaration.wav (9.29 s, at 0.20 s)")
    assert actions == _clicks(wc.PLAY_RECORDING, wc.NEW_GAME, wc.END_TURN)


def _args(*argv):
    return wc.parser().parse_args(["build", *argv])


def _plan(*argv):
    driver, fail, plan = wc.run_plan(_args(*argv), lambda action: None)
    if driver is not None:
        driver.settle = lambda seconds: None
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
    assert (he.play, he.end_turn) == (wc.PLAY_RECORDING_HE, wc.END_TURN_HE)
    assert (en.play, en.end_turn) == (wc.PLAY_RECORDING, wc.END_TURN)


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
