import base64
import hashlib

import pytest

import web_check as wc


def shot_lines(name: str, png: bytes, chunk: int = 10) -> list[str]:
    b64 = base64.b64encode(png).decode()
    parts = [b64[i:i + chunk] for i in range(0, len(b64), chunk)]
    return [f"SHOT {name} begin {len(parts)} {hashlib.sha256(png).hexdigest()}"] + \
           [f"SHOT {name} {i + 1}/{len(parts)} {p}" for i, p in enumerate(parts)]


def test_a_shot_is_reassembled_and_written(tmp_path):
    png = png_of_size(32, 18, varied=True)
    c = wc.ShotCollector(tmp_path)
    assert all(c.feed(line) for line in shot_lines("gameplay03", png))
    assert (tmp_path / "gameplay03.png").read_bytes() == png
    assert c.written == [tmp_path / "gameplay03.png"]
    assert c.feed("SHOT done") is False          # printed and matched by --expect, not consumed
    assert c.feed("GAME OK 1948-05-15") is False


def test_parts_may_arrive_out_of_order(tmp_path):
    png = png_of_size(32, 18, varied=True)
    lines = shot_lines("diorama02", png)
    c = wc.ShotCollector(tmp_path)
    for line in [lines[0]] + list(reversed(lines[1:])):
        c.feed(line)
    assert (tmp_path / "diorama02.png").read_bytes() == png


def test_a_cut_line_fails_the_hash(tmp_path):
    lines = shot_lines("gameplay03", bytes(300))
    lines[2] = lines[2][:-3]
    c = wc.ShotCollector(tmp_path)
    with pytest.raises(ValueError, match="SHA-256"):
        for line in lines:
            c.feed(line)


def test_a_part_before_its_header_is_an_error(tmp_path):
    with pytest.raises(ValueError, match="before its begin line"):
        wc.ShotCollector(tmp_path).feed("SHOT gameplay03 1/2 AAAA")


def test_a_lost_part_fails_loudly_at_shot_done(tmp_path):
    lines = shot_lines("gameplay03", bytes(300))
    c = wc.ShotCollector(tmp_path)
    for line in lines[:2] + lines[3:]:           # one part line never arrives
        c.feed(line)
    with pytest.raises(ValueError, match=r"gameplay03: \d+ of \d+ parts"):
        c.feed("SHOT done")


def test_headless_chrome_runs_uncapped_at_dpr_1():
    flags = wc.chrome_flags("/tmp/p", swiftshader=False)
    for flag in ("--headless=new", "--disable-frame-rate-limit", "--disable-gpu-vsync", "--window-size=1920,1080",
                 "--user-data-dir=/tmp/p", "--force-device-scale-factor=1"):
        assert flag in flags
    assert "--use-angle=swiftshader" in wc.chrome_flags("/tmp/p", swiftshader=True)


def png_of_size(w: int, h: int, varied: bool = False) -> bytes:
    """A PNG; varied=True gives it a gradient, since a one-colour (blank) shot is now rejected."""
    import io
    from PIL import Image
    img = Image.new("RGB", (w, h), (10, 20, 30))
    if varied:
        img.putdata([(x * 255 // max(w - 1, 1), y * 255 // max(h - 1, 1), 99) for y in range(h) for x in range(w)])
    buf = io.BytesIO()
    img.save(buf, "PNG")
    return buf.getvalue()


def test_a_shot_of_the_wrong_size_fails(tmp_path):
    """Ruling 1: screenshots are 1920x1080; the 1920x937 headless viewport must fail, not pass silently."""
    c = wc.ShotCollector(tmp_path, size=(1920, 1080))
    with pytest.raises(ValueError, match="1920x937.*1920x1080"):
        for line in shot_lines("gameplay03", png_of_size(1920, 937), chunk=4000):
            c.feed(line)
    ok = wc.ShotCollector(tmp_path, size=(64, 36))
    assert all(ok.feed(line) for line in shot_lines("diorama02", png_of_size(64, 36, varied=True), chunk=4000))


@pytest.mark.parametrize("bad", ["SHOT gameplay03 0/3 AAAA", "SHOT gameplay03 4/3 AAAA", "SHOT gameplay03 1/2 AAAA"])
def test_a_part_that_does_not_fit_its_begin_line_is_a_value_error(tmp_path, bad):
    """Review M1: out-of-range or mismatched part numbers fail as FAIL (ValueError), never as a KeyError traceback."""
    c = wc.ShotCollector(tmp_path)
    c.feed("SHOT gameplay03 begin 3 " + "0" * 64)
    with pytest.raises(ValueError, match="does not fit"):
        c.feed(bad)


def test_the_viewport_is_fixed_over_cdp_at_the_dpr():
    assert wc.viewport_override(1920, 1080, 2) == {"width": 1920, "height": 1080, "deviceScaleFactor": 2, "mobile": False}
    assert wc.expected_res(1920, 1080, 1) == "1920x1080" and wc.expected_res(1920, 1080, 2) == "3840x2160"


def test_a_profile_line_at_another_resolution_is_an_error():
    good = "PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=0 frames=300 avg_ms=1.73 p95_ms=2.00"
    assert wc.res_error(good, "1920x1080") is None
    assert wc.res_error("GAME OK 1948-05-15 turn 0 hash 00", "1920x1080") is None
    assert "1920x937" in wc.res_error(good.replace("1920x1080", "1920x937"), "1920x1080")


def profile_lines(poses=("gameplay03", "diorama02"), labels=("all", "-labels", "none"), reps=3, res="1920x1080", done=True):
    """The console lines of a whole ?vprofile run as FrameProfile.Run logs them: per pose and configuration `reps` run
    lines, then a summary line; then the done line."""
    lines = []
    for pose in poses:
        for label in labels:
            lines += [f"PROFILE visual={label} pose={pose} res={res} scale=0.80 rep={r} frames=300 avg_ms=1.73 p95_ms=2.00" for r in range(reps)]
            lines.append(f"PROFILE summary visual={label} pose={pose} runs={reps} median_avg_ms=1.73 spread_ms=0.10 median_p95_ms=2.00")
    return lines + (["PROFILE visual done"] if done else [])


def test_profile_errors_need_run_lines_the_right_size_and_the_done_line():
    assert wc.profile_errors(profile_lines(), "1920x1080") == []
    assert wc.profile_errors(["GAME OK"], "1920x1080")                      # no PROFILE lines at all
    assert wc.profile_errors(profile_lines(done=False), "1920x1080")        # never finished
    assert wc.profile_errors(profile_lines(res="1728x860"), "1920x1080")


def test_a_partial_profile_is_an_error_even_with_the_done_line():
    """#2697 item 5: one run line and the done line used to pass."""
    full = profile_lines()
    one_run = [l for l in full if l.startswith("PROFILE visual=all pose=gameplay03 ")][:1] + ["PROFILE visual done"]
    assert wc.profile_errors(one_run, "1920x1080")
    no_summary = [l for l in full if not l.startswith("PROFILE summary visual=-labels pose=diorama02")]
    assert any("diorama02 visual -labels" in e for e in wc.profile_errors(no_summary, "1920x1080"))
    short_pose = [l for l in full if not (l.startswith("PROFILE") and "visual=none pose=diorama02" in l)]
    assert any("diorama02 has no run of none" in e for e in wc.profile_errors(short_pose, "1920x1080"))
    assert any("'none'" in e for e in wc.profile_errors(profile_lines(labels=("all", "-labels")), "1920x1080"))
    dropped = full.copy(); dropped.remove(next(l for l in full if l.startswith("PROFILE visual=all pose=gameplay03 ")))
    assert any("2 run lines, summary says 3" in e for e in wc.profile_errors(dropped, "1920x1080"))


def test_a_configuration_missing_from_every_pose_is_an_error():
    """#3043 item 2: the per-pose check compares poses with each other, so a run that drops one toggle's configuration
    (or a pose) everywhere passed. The expected toggles and poses are web_check's lists, checked against the C# Register
    calls by test_visual_toggles.py; the flag-only toggles and the vclose poses may be there or not."""
    full = ("all", *(f"-{t}" for t in wc.VISUAL_TOGGLES), "none")
    assert wc.profile_errors(profile_lines(labels=full, poses=wc.PROFILE_POSES), "1920x1080") == []
    for gone in ("-borders", "-labels", f"-{wc.VISUAL_TOGGLES[0]}"):
        errors = wc.profile_errors(profile_lines(labels=[l for l in full if l != gone], poses=wc.PROFILE_POSES), "1920x1080")
        assert any(f"'{gone}'" in e for e in errors), (gone, errors)
    errors = wc.profile_errors(profile_lines(labels=full, poses=wc.PROFILE_POSES[1:]), "1920x1080")
    assert any(f"pose {wc.PROFILE_POSES[0]}" in e for e in errors), errors
    extra = (*full[:-1], *(f"-{t}" for t in wc.OPTIONAL_VISUAL_TOGGLES), "none")
    assert wc.profile_errors(profile_lines(labels=extra, poses=(*wc.PROFILE_POSES, *wc.OPTIONAL_PROFILE_POSES)), "1920x1080") == []
    moving = tuple(f"{p}-moving" for p in wc.PROFILE_POSES)       # a ?vmoving run's poses (FrameProfile.cs)
    assert wc.profile_errors(profile_lines(labels=full, poses=moving), "1920x1080") == []


def test_measurement_windows_are_kept_awake():
    flags = wc.chrome_flags("/tmp/p", swiftshader=False)
    for flag in ("--disable-backgrounding-occluded-windows", "--disable-renderer-backgrounding",
                 "--disable-background-timer-throttling", "--remote-debugging-port=0"):
        assert flag in flags
    assert "--force-device-scale-factor=2" in wc.chrome_flags("/tmp/p", swiftshader=False, dpr=2)


def test_a_silent_browser_stalls_out_before_the_timeout(capsys):
    import queue
    clock = iter(range(0, 10000, 10))
    code = wc.watch(queue.Queue(), lambda: None, lambda: None, ["PROFILE visual done"], wc.re.compile("FAIL"), 3600,
                    now=lambda: next(clock), sleep=lambda s: None, stall=120)
    assert code == 1 and "STALLED" in capsys.readouterr().out


def test_chrome_keeps_its_page_content_agent_quiet_so_it_cannot_split_a_console_line():
    # Chrome 153's --v=1 log of ai_page_content_agent.cc ("GetAIPageContent SYNC MainFrame") was written into the middle of
    # an 8000-character SHOT line on stderr, so that part failed to parse (Task 10: hero06judea 165-166 of 167, three runs).
    assert "--vmodule=ai_page_content_agent=0" in wc.chrome_flags("/tmp/p", swiftshader=False)


SPLICE = "[8471:92424315:1010/172052.176598:VERBOSE1:gpu/command_buffer/service/gles2_cmd_decoder.cc:77] Some GPU note\n"


def test_a_log_line_from_another_thread_spliced_into_a_shot_line_is_cut_out_and_the_line_rejoined():
    # Chrome writes a long console line to stderr in pieces; another thread's --v=1 line (any module) can land between
    # them, ending the console line early and pushing its tail onto the next stderr line (Task 10 fix round 1, m6).
    text = "SHOT hero06judea 11/167 " + "AbCd+/" * 40
    head, tail = text[:100], text[100:]
    raw = [f'[1010/172052.1:INFO:CONSOLE:2] "{head}{SPLICE}', f'{tail}", source: http://localhost:1/index.html (2)\n']
    rejoin = wc.Rejoin()
    out = [l for r in raw for l in rejoin.feed(r)]
    assert len(out) == 1 and wc.CONSOLE.search(out[0]).group(1) == text


def test_rejoin_holds_an_open_line_across_batches_and_leaves_ordinary_lines_alone():
    rejoin = wc.Rejoin()
    plain = '[1010/1.1:INFO:CONSOLE:2] "GOLDEN OK 1950 a", source: x (2)\n'
    multi = '[1010/1.1:INFO:CONSOLE:9] "SMOKE OK 5 turns 1949-01-01\n'   # a multi-line message: first line only, as before
    other = "[8471:1:1010/1.1:VERBOSE1:foo.cc:1] unrelated\n"
    assert rejoin.feed(plain) == [plain] and rejoin.feed(multi) == [multi] and rejoin.feed(other) == [other]
    assert rejoin.feed(f'[1010/1.1:INFO:CONSOLE:2] "SHOT a 1/2 AAAA{SPLICE}') == []
    joined = rejoin.feed('BBBB", source: x (2)\n')
    assert [wc.CONSOLE.search(l).group(1) for l in joined] == ["SHOT a 1/2 AAAABBBB"]
    assert rejoin.flush() == []


# --- #2697 items 2 and 6: a --shots run that captured nothing usable must fail ---------------------------------------

def run_main(monkeypatch, capsys, tmp_path, console: list[str], *argv: str) -> tuple[int, str]:
    """web_check.main against a fake browser whose stderr is `console` as Chrome prints it; no Chrome, no network."""
    import io
    import sys

    class FakeBrowser:
        stderr = io.StringIO("".join(f'[1105/1.1:INFO:CONSOLE:1] "{t}", source: x (1)\n' for t in console))
        def poll(self): return 0
        def terminate(self): pass
        def wait(self, timeout=None): return 0

    class FakePage:
        def __init__(self, *a): pass
        def open(self): pass
        def close(self): pass
        def command(self, name): pass

    monkeypatch.setattr(wc, "find_chrome", lambda: tmp_path / "chrome")
    monkeypatch.setattr(wc.subprocess, "Popen", lambda *a, **k: FakeBrowser())
    monkeypatch.setattr(wc, "Page", FakePage)
    monkeypatch.setattr(sys, "argv", ["web_check.py", "--url", "http://x.invalid/index.html", "--viewport", "100x100",
                                      "--shots", str(tmp_path / "shots"), *argv])
    code = wc.main()
    return code, capsys.readouterr().out


def test_shots_run_with_an_unrelated_expect_and_no_shot_at_all_fails(monkeypatch, capsys, tmp_path):
    code, out = run_main(monkeypatch, capsys, tmp_path, ["GAME OK 1948-05-15"], "--expect", "GAME OK")
    assert code == 1 and "PASS" not in out and not list((tmp_path / "shots").glob("*.png"))


def test_shots_run_that_never_logs_shot_done_fails_even_with_a_shot(monkeypatch, capsys, tmp_path):
    console = ["GAME OK 1948-05-15"] + shot_lines("gameplay03", png_of_size(100, 100, varied=True), chunk=4000)
    code, out = run_main(monkeypatch, capsys, tmp_path, console, "--expect", "GAME OK")
    assert code == 1 and "PASS" not in out


def test_shots_run_with_shot_done_but_no_shot_fails(monkeypatch, capsys, tmp_path):
    code, out = run_main(monkeypatch, capsys, tmp_path, ["SHOT done"], "--expect", "SHOT done")
    assert code == 1 and "PASS" not in out


def test_shots_run_that_finishes_one_good_shot_passes(monkeypatch, capsys, tmp_path):
    console = shot_lines("gameplay03", png_of_size(100, 100, varied=True), chunk=4000) + ["SHOT done"]
    code, out = run_main(monkeypatch, capsys, tmp_path, console, "--expect", "SHOT done")
    assert code == 0 and (tmp_path / "shots" / "gameplay03.png").exists()


def test_a_blank_shot_is_rejected(tmp_path):
    c = wc.ShotCollector(tmp_path, size=(64, 36))
    with pytest.raises(ValueError, match="blank"):
        for line in shot_lines("gameplay03", png_of_size(64, 36), chunk=4000):
            c.feed(line)
    assert not list(tmp_path.glob("*.png"))


def test_a_shot_that_is_not_a_png_is_rejected_even_without_a_size(tmp_path):
    c = wc.ShotCollector(tmp_path)
    with pytest.raises(ValueError, match="not a PNG"):
        for line in shot_lines("gameplay03", bytes(range(256)) * 3):
            c.feed(line)
    assert not list(tmp_path.glob("*.png"))


def test_shots_run_with_a_blank_shot_then_shot_done_fails_and_writes_nothing(monkeypatch, capsys, tmp_path):
    """#2697 item 6 through web_check.main: a one-colour PNG is no capture."""
    console = shot_lines("gameplay03", png_of_size(100, 100), chunk=4000) + ["SHOT done"]
    code, out = run_main(monkeypatch, capsys, tmp_path, console, "--expect", "SHOT done")
    assert code == 1 and "PASS" not in out and "blank" in out and not list((tmp_path / "shots").glob("*"))


def test_shots_run_with_non_png_bytes_then_shot_done_fails_and_writes_nothing(monkeypatch, capsys, tmp_path):
    console = shot_lines("gameplay03", bytes(range(256)) * 3, chunk=4000) + ["SHOT done"]
    code, out = run_main(monkeypatch, capsys, tmp_path, console, "--expect", "SHOT done")
    assert code == 1 and "PASS" not in out and "not a PNG" in out and not list((tmp_path / "shots").glob("*"))


def test_a_png_with_a_good_header_and_a_damaged_body_is_rejected(tmp_path):
    """The SHA-256 is of the damaged bytes, so only the decode catches it."""
    png = bytearray(png_of_size(64, 36, varied=True))
    at = png.index(b"IDAT") + 12
    for i in range(at, at + 6):
        png[i] ^= 0xFF
    c = wc.ShotCollector(tmp_path, size=(64, 36))
    with pytest.raises(ValueError, match="not a PNG that decodes"):
        for line in shot_lines("gameplay03", bytes(png), chunk=4000):
            c.feed(line)
    assert not list(tmp_path.glob("*.png"))
