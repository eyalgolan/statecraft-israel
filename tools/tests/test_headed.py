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
