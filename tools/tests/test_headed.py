"""tools/headed.py: the pure part always; the real headed browser run only with `pytest --headed` (it opens a window)."""
import asyncio
import argparse
from pathlib import Path

import pytest

import headed

BUILD = Path(headed.REPO) / "unity/Builds/Web-disabled"


def test_a_burst_is_move_press_release_per_click_in_order():
    events = headed.burst_events([(10, 20), (30, 40)])
    assert [(e["type"], e["x"], e["y"]) for e in events] == [
        ("mouseMoved", 10, 20), ("mousePressed", 10, 20), ("mouseReleased", 10, 20),
        ("mouseMoved", 30, 40), ("mousePressed", 30, 40), ("mouseReleased", 30, 40)]
    assert all(e["button"] == "left" and e["clickCount"] == 1 for e in events)


@pytest.mark.headed
@pytest.mark.skipif(not BUILD.exists(), reason="needs unity/Builds/Web-disabled")
def test_every_click_in_one_frame_counts_once():
    """Playtest 2, P2-4, in a real browser: two clicks in one frame each toggle Why exactly once."""
    toggles = asyncio.run(headed.navpairs(argparse.Namespace(build=str(BUILD), cpu=1.0)))
    assert toggles == {"Why+Why": 2, "Why+map-under-panel": 1, "Why+option": 1}


def test_boot_phases_and_the_map_build_span():
    lines = ["BOOT phase=player started at=900 ms dt=900 ms", "BOOT phase=heightmap decoded at=5200 ms dt=1300 ms",
             "GAME OK 1948-05-15 turn 0 hash 00", "BOOT phase=map built at=9400 ms dt=300 ms"]
    phases = headed.boot_phases(lines)
    assert phases[0] == ("player started", 900, 900)
    assert headed.map_build_ms(phases) == 9400 - 5200 + 1300
    with pytest.raises(ValueError, match="map built"):
        headed.map_build_ms(phases[:2])


def test_the_boot_budget_compares_the_median_with_the_baseline():
    assert headed.over_budget([5500, 5700, 5900], 4800, 1000) == (5700, False)   # 5700 <= 5800
    assert headed.over_budget([5500, 5700, 5900], 4600, 1000) == (5700, True)    # 5700 > 5600
    assert headed.over_budget([5500], None, 1000) == (5500, False)


def test_profile_windows_are_uncapped_at_the_requested_dpr():
    plain = headed.browser_flags("/tmp/p", 1568, 752)
    assert "--use-angle=metal" in plain and "--window-size=1568,752" in plain and plain[-1] == "about:blank"
    assert not any("scale-factor" in f or "frame-rate" in f for f in plain), "navpairs and boot keep their old flags"
    prof = headed.browser_flags("/tmp/p", 1920, 1080, dpr=2, uncapped=True)
    for flag in ("--force-device-scale-factor=2", "--disable-frame-rate-limit", "--disable-gpu-vsync", "--window-size=1920,1080"):
        assert flag in prof
    assert prof[-1] == "about:blank"


def test_the_default_boot_budget_is_300_ms_or_half_the_baseline():
    """Ruling 3: baseline + max(300 ms, 50%) unless --budget is given."""
    assert headed.over_budget([740], 450, None) == (740, False)       # 450 + 300
    assert headed.over_budget([760], 450, None) == (760, True)
    assert headed.over_budget([1490], 1000, None) == (1490, False)    # 1000 + 500
    assert headed.over_budget([1510], 1000, None) == (1510, True)


def test_measurement_windows_are_kept_awake_and_navpairs_keeps_its_flags():
    awake = headed.browser_flags("/tmp/p", 1920, 1080, keep_awake=True)
    for flag in ("--disable-backgrounding-occluded-windows", "--disable-renderer-backgrounding",
                 "--disable-background-timer-throttling"):
        assert flag in awake
        assert flag not in headed.browser_flags("/tmp/p", 1568, 752)
    assert awake[-1] == "about:blank"


def test_the_viewport_is_set_before_the_page_loads(tmp_path):
    """Ruling 1: Emulation.setDeviceMetricsOverride (1920x1080 at the DPR, not mobile) precedes Page.navigate."""
    b = headed.Browser(tmp_path, viewport=(1920, 1080), dpr=2)
    sent = []

    async def send(method, params=None, session=None, timeout=60):
        sent.append((method, params))
        return {"targetId": "t", "sessionId": "s"}
    b.send, b.base = send, "http://localhost/"
    asyncio.run(b.open("vprofile", init_script="window.x = 1"))
    methods = [m for m, _ in sent]
    assert methods.index("Emulation.setDeviceMetricsOverride") < methods.index("Page.navigate")
    assert methods.index("Page.addScriptToEvaluateOnNewDocument") < methods.index("Page.navigate")
    assert dict(sent)["Emulation.setDeviceMetricsOverride"] == {"width": 1920, "height": 1080, "deviceScaleFactor": 2, "mobile": False}
    plain = headed.Browser(tmp_path)
    plain_sent = []

    async def send2(method, params=None, session=None, timeout=60):
        plain_sent.append(method)
        return {"targetId": "t", "sessionId": "s"}
    plain.send, plain.base = send2, "http://localhost/"
    asyncio.run(plain.open("navlog"))
    assert "Emulation.setDeviceMetricsOverride" not in plain_sent       # navpairs and loadshot keep the window's size
    import shutil
    for browser in (b, plain):
        shutil.rmtree(browser.profile, ignore_errors=True)


def test_the_pad_swipe_is_many_small_deltas():
    swipe = headed.pad_swipe()
    assert len(swipe) == 31 and all(-8 <= d <= -0.5 for d in swipe)
    assert headed.ideal_notches("pad") == pytest.approx(1.082, abs=1e-3)
    assert headed.ideal_notches("lines3") == 1 and headed.ideal_notches("out_px100") == -1
    assert headed.ideal_notches("px200") == 1 and headed.ideal_notches("px150") == 1 and headed.ideal_notches("cdp100") == 1
    assert headed.ideal_notches("px49") == pytest.approx(0.49)


def test_gesture_notches_reads_the_camera_log():
    assert headed.gesture_notches(120000, [108000]) == pytest.approx((1.0, 1.0))
    total, largest = headed.gesture_notches(120000, [120000 * 0.9 ** 0.5, 120000 * 0.9 ** 0.7])
    assert total == pytest.approx(0.7) and largest == pytest.approx(0.5)
    assert headed.gesture_notches(120000, []) == (0.0, 0.0)


@pytest.mark.headed
@pytest.mark.skipif(not BUILD.exists(), reason="needs unity/Builds/Web-disabled")
def test_wheel_zoom_follows_a_swipe_and_keeps_a_notch_at_one_step():
    """#43 in a real browser (WheelInput.jslib and CameraController.WheelOr): a touchpad swipe zooms by its length with
    no frame near the clamp, before or after a mouse notch; a notch in pixels or lines is one step; nothing over a panel."""
    r = {label: {g: (total, largest) for g, total, largest, _ in steps} for label, steps in
         asyncio.run(headed.wheel(argparse.Namespace(build=str(BUILD)))).items()}
    pad = headed.ideal_notches("pad")
    for label in ("pad", "px100,pad", "lines3,pad", "pad,out_px100", "pad,out_lines3"):
        total, largest = r[label]["pad"]
        assert total == pytest.approx(pad, abs=0.05), label
        assert largest < 0.25, label
    assert r["px100,pad"]["px100"][0] == pytest.approx(1, abs=0.02)
    assert r["lines3,pad"]["lines3"][0] == pytest.approx(1, abs=0.02)
    assert r["pad,out_px100"]["out_px100"][0] == pytest.approx(-1, abs=0.02)
    assert r["pad,out_lines3"]["out_lines3"][0] == pytest.approx(-1, abs=0.02)
    assert r["pad,px100@panel"] == {"pad": (0.0, 0.0), "px100": (0.0, 0.0)}
    for label in ("cdp100", "pad,cdp100"):   # Chrome's own 100 px notch, which arrives as 200 px: one step, not two
        assert r[label]["cdp100"][0] == pytest.approx(1, abs=0.02), label
    assert r["px150,px200,px49"]["px150"][0] == pytest.approx(1, abs=0.02)
    assert r["px150,px200,px49"]["px200"][0] == pytest.approx(1, abs=0.02)
    assert r["px150,px200,px49"]["px49"][0] == pytest.approx(0.49, abs=0.02)


def png(path, varied=True, size=(8, 8)):
    """A real PNG at `path` of `size` pixels: a gradient, or one colour (a blank capture)."""
    from PIL import Image
    w, h = size
    im = Image.new("RGB", (w, h), (40, 60, 80))
    if varied:
        im.putdata([(x * 30 % 256, y * 30 % 256, 90) for y in range(h) for x in range(w)])
    im.save(path)


class FakeBrowser:
    """headed.Browser without Chrome: wait_for returns the queued lines; shot writes the PNG the test queued (a gradient by
    default, 8x8); page_size answers as Runtime.evaluate would, with the queued (innerWidth, innerHeight, devicePixelRatio)
    (8, 8, 1 by default) or raises a queued exception."""
    waits: list[str] = []
    console: list[str] = []
    shots: list = []
    contents: list = []       # per shot, in order: "varied", "blank" or "garbage"
    png_sizes: list = []      # per shot, in order: the PNG's (width, height)
    pages: list = []          # per page_size call, in order: (width, height, dpr) or an exception
    calls: list = []          # "page_size" and "shot", in the order loadshot made them

    def __init__(self, *args, **kwargs):
        pass

    async def __aenter__(self):
        return self

    async def __aexit__(self, *args):
        return None

    async def open(self, *args):
        return "sid"

    def lines(self, sid):
        return list(FakeBrowser.console)

    async def wait_for(self, *args, **kwargs):
        line = FakeBrowser.waits.pop(0)
        FakeBrowser.console.append(line)
        return line

    async def shot(self, sid, path):
        FakeBrowser.shots.append(path)
        FakeBrowser.calls.append("shot")
        kind = FakeBrowser.contents.pop(0) if FakeBrowser.contents else "varied"
        size = FakeBrowser.png_sizes.pop(0) if FakeBrowser.png_sizes else (8, 8)
        if kind == "garbage":
            Path(path).write_bytes(b"not a png")
        else:
            png(path, varied=kind == "varied", size=size)
        return path

    async def page_size(self, sid):
        FakeBrowser.calls.append("page_size")
        page = FakeBrowser.pages.pop(0) if FakeBrowser.pages else (8, 8, 1)
        if isinstance(page, Exception):
            raise page
        return page


def test_loadshot_exits_1_on_a_boot_failure_before_or_after_loading(monkeypatch, tmp_path, capsys):
    """#2697: loadshot used to take its screenshots and exit 0 after BOOT FAIL. A BOOT FAIL already in the console when
    the first wait returns "player started" (wait_for gives the first match) takes no loading screenshot either."""
    shots = FakeBrowser.shots

    monkeypatch.setattr(headed, "Browser", FakeBrowser)
    for waits, buffered, expected_shots in ((["BOOT FAIL content missing"], [], 0),
                                            (["BOOT phase=player started", "BOOT FAIL scene"], [], 1),
                                            (["BOOT phase=player started"], ["BOOT FAIL late"], 0)):
        shots.clear()
        FakeBrowser.waits, FakeBrowser.console, FakeBrowser.contents = list(waits), list(buffered), []
        assert headed.main(["loadshot", "--build", str(tmp_path), "--out", str(tmp_path / "shot")]) == 1
        assert len(shots) == expected_shots
        assert "FAIL: BOOT FAIL" in capsys.readouterr().out


def run_loadshot(monkeypatch, tmp_path, contents, png_sizes=(), pages=()):
    """loadshot through headed.main on a healthy boot whose two screenshots hold `contents` at `png_sizes`, on a page
    that reports `pages` (8x8 PNGs of an 8x8 page at DPR 1 by default)."""
    monkeypatch.setattr(headed, "Browser", FakeBrowser)
    FakeBrowser.waits = ["BOOT phase=player started", "GAME OK 1948-05-15 turn 0 hash 00"]
    FakeBrowser.console, FakeBrowser.contents, FakeBrowser.shots = [], list(contents), []
    FakeBrowser.png_sizes, FakeBrowser.pages, FakeBrowser.calls = list(png_sizes), list(pages), []
    return headed.main(["loadshot", "--build", str(tmp_path), "--out", str(tmp_path / "shot")])


def test_loadshot_passes_when_both_captures_are_real_and_a_plain_loading_screen_is_fine(monkeypatch, tmp_path):
    assert run_loadshot(monkeypatch, tmp_path, ["varied", "varied"]) == 0
    assert run_loadshot(monkeypatch, tmp_path, ["blank", "varied"]) == 0      # the owner's #2700 caution: a plain loading screen


def test_loadshot_exits_1_on_a_blank_capture_after_the_game_is_ready(monkeypatch, tmp_path, capsys):
    """#2697 item 4: a blank screenshot after GAME OK is no capture."""
    assert run_loadshot(monkeypatch, tmp_path, ["varied", "blank"]) == 1
    out = capsys.readouterr().out
    assert "FAIL" in out and "blank" in out


def test_loadshot_exits_1_on_a_capture_that_does_not_decode(monkeypatch, tmp_path, capsys):
    for contents in (["garbage", "varied"], ["varied", "garbage"]):
        assert run_loadshot(monkeypatch, tmp_path, contents) == 1, contents
        assert "FAIL" in capsys.readouterr().out


def test_loadshot_exits_1_when_a_capture_is_not_the_page_size_times_its_pixel_ratio(monkeypatch, tmp_path, capsys):
    """#3043 item 1: a capture of the wrong size used to pass. The expected size is the page's own innerWidth x
    innerHeight times its devicePixelRatio, read just before each capture, so the window and viewport stay as they are
    (the #2700 caution); more than 1 px off in either dimension is a failure."""
    for pngs, pages, wrong in (([(8, 8), (16, 8)], [(8, 8, 1), (8, 8, 1)], "16x8"),          # the capture after GAME OK
                               ([(8, 4), (16, 8)], [(8, 4, 2), (8, 4, 2)], "8x4"),           # CSS size on a DPR 2 page
                               ([(14, 8), (16, 7)], [(9, 5, 1.5), (9, 5, 1.5)], "16x7"),     # 2 px wide of 14x8
                               ([(8, 8), (8, 6)], [(8, 8, 1), (8, 8, 1)], "8x6")):           # 2 px short
        assert run_loadshot(monkeypatch, tmp_path, ["varied", "varied"], pngs, pages) == 1, (pngs, pages)
        out = capsys.readouterr().out
        assert "FAIL:" in out and wrong in out, out


def test_loadshot_passes_a_capture_the_page_size_times_its_pixel_ratio_and_records_both(monkeypatch, tmp_path, capsys):
    """#3043 item 1: DPR 2 (a Retina screen: device pixels, twice the CSS size), DPR 1, and 1 px of rounding for a
    fractional ratio (9 x 1.5 = 13.5) all pass; each capture line records the PNG and page sizes."""
    for pngs, pages, recorded in (([(16, 8), (16, 8)], [(8, 4, 2), (8, 4, 2)], "16x8, page 8x4 CSS px at DPR 2"),
                                  ([(8, 8), (8, 8)], [(8, 8, 1), (8, 8, 1)], "8x8, page 8x8 CSS px at DPR 1"),
                                  ([(13, 7), (14, 8)], [(9, 5, 1.5), (9, 5, 1.5)], "13x7, page 9x5 CSS px at DPR 1.5")):
        assert run_loadshot(monkeypatch, tmp_path, ["varied", "varied"], pngs, pages) == 0, (pngs, pages)
        out = capsys.readouterr().out
        assert "FAIL" not in out and recorded in out, out
        assert FakeBrowser.calls == ["page_size", "shot", "page_size", "shot"]      # read just before each capture


def test_loadshot_exits_1_when_the_page_size_cannot_be_read(monkeypatch, tmp_path, capsys):
    """#3043 item 1: a page mid-reload (Runtime.evaluate fails, or answers with no value) is a failure, not a pass."""
    for pages, shots in (([RuntimeError("Runtime.evaluate: Execution context was destroyed")], 0),
                         ([(8, 8, 1), ValueError("no page size (exceptionDetails)")], 1)):
        assert run_loadshot(monkeypatch, tmp_path, ["varied", "varied"], pages=pages) == 1
        out = capsys.readouterr().out
        assert "FAIL:" in out and "page size" in out, out
        assert len(FakeBrowser.shots) == shots


def test_page_size_reads_the_window_and_ratio_over_runtime_evaluate(tmp_path):
    """The real Browser.page_size: one Runtime.evaluate on the page's session, by value; an exception in the page or a
    missing value raises, so loadshot fails instead of guessing."""
    import shutil
    b = headed.Browser(tmp_path)
    answers, sent = [{"result": {"type": "object", "value": [1520, 640, 2]}},
                     {"result": {"type": "object"}, "exceptionDetails": {"text": "Uncaught"}}], []

    async def send(method, params=None, session=None, timeout=60):
        sent.append((method, params, session))
        return answers.pop(0)
    b.send = send
    assert asyncio.run(b.page_size("s1")) == (1520, 640, 2)
    method, params, session = sent[0]
    assert method == "Runtime.evaluate" and session == "s1" and params["returnByValue"] is True
    for name in ("innerWidth", "innerHeight", "devicePixelRatio"):
        assert name in params["expression"]
    with pytest.raises(ValueError, match="page size"):
        asyncio.run(b.page_size("s1"))
    shutil.rmtree(b.profile, ignore_errors=True)


def test_profile_exits_1_on_a_run_missing_a_configuration_from_every_pose(monkeypatch, tmp_path, capsys):
    """#3043 item 2 through headed.main: every pose has the same configurations, so the per-pose check passed this run;
    the toggles the game registers (web_check.VISUAL_TOGGLES) are missing from all of them."""
    from test_web_check_shots import profile_lines
    monkeypatch.setattr(headed, "Browser", FakeBrowser)
    FakeBrowser.waits, FakeBrowser.console = ["PROFILE visual done"], profile_lines(labels=("all", "none"))
    assert headed.main(["profile", "--build", str(tmp_path)]) == 1
    out = capsys.readouterr().out
    assert "FAIL" in out and "-labels" in out, out


def test_profile_exits_1_on_a_partial_profile_and_0_on_a_whole_one(monkeypatch, tmp_path, capsys):
    """#2697 item 5 through headed.main: the done line alone does not make a profile."""
    from test_web_check_shots import profile_lines
    monkeypatch.setattr(headed, "Browser", FakeBrowser)
    for lines, code in ((profile_lines(), 0), ([profile_lines()[0], "PROFILE visual done"], 1)):
        FakeBrowser.waits, FakeBrowser.console = ["PROFILE visual done"], list(lines)
        assert headed.main(["profile", "--build", str(tmp_path)]) == code
        assert ("FAIL" in capsys.readouterr().out) == (code == 1)


def test_new_game_is_clicked_where_the_start_screen_puts_it_in_each_language():
    """The start screen's New game in the 1568x752 playtest window, derived from Statecraft.uss (refs #3003). A headed
    capture on 10 Nov 2026 at 0be35a517 (main 59fdd6924 plus #3003) found it at (541, 657) in English and (1026, 657) in
    Hebrew (?lang=he, the action row mirrored); the old (784, 693) missed it. The derivation agrees within 2 px."""
    for lang, measured in (("en", (541, 657)), ("he", (1026, 657))):
        x, y = headed.start_new_game_button(lang)
        assert abs(x - measured[0]) <= 3 and abs(y - measured[1]) <= 3, (lang, (x, y))
    assert headed.start_new_game_button("en") == (543, 657)
    assert headed.start_new_game_button("he") == (1025, 657)
    assert headed.START_NEW == headed.start_new_game_button("en")
    assert headed.query_lang("camlog") == "en" and headed.query_lang("camlog&lang=he") == "he"
    with pytest.raises(ValueError):
        headed.start_new_game_button("ro")


def test_the_new_game_derivation_reads_the_uss_numbers():
    """Fails when Statecraft.uss moves the start panel's bottom stack or resizes its buttons and headed.py does not follow."""
    import re
    uss = (Path(headed.REPO) / "unity/Assets/UI/Statecraft.uss").read_text()

    def rule(selector):
        m = re.search(r"(?m)^" + re.escape(selector) + r"\s*\{([^}]*)\}", uss)
        assert m, selector
        return m.group(1)

    assert re.search(r"padding:\s*0 16px 28px 16px", rule(".start-body"))
    assert re.search(r"padding:\s*18px 40px 20px 40px", rule(".start-panel"))
    assert re.search(r"border-width:\s*1px", rule(".panel"))
    community = rule(".start-community")
    assert re.search(r"height:\s*40px", community) and re.search(r"margin-top:\s*8px", community)
    button = rule(".start-button")
    assert re.search(r"min-width:\s*300px", button) and re.search(r"height:\s*50px", button)
    assert re.search(r"margin:\s*4px 10px", button)
    assert re.search(r"\.start-actions, \.lang-he \.start-community \{ flex-direction: row-reverse", uss)
    assert headed.START_PANEL_BOTTOM == 28 + 1 + 20 + 40 + 8 and headed.START_BUTTON == (300 + 2 * 10, 50 + 2 * 4)
