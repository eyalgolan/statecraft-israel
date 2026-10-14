"""tools/encode_event_videos.py: the decision-card clips are silent 960x540 H.264 with faststart, made only from the
promo shot each row names (by SHA-256), with no burned-in caption (owner ruling of 14 October 2026: "no need to say
there were created by ai"; the rows keep ai_generated: true); Deport and other content rules live in the Unity-side
EventVideoMapping tests."""
import shutil
import subprocess

import pytest

import encode_event_videos as ev

needs_ffmpeg = pytest.mark.skipif(shutil.which("ffmpeg") is None or shutil.which("ffprobe") is None, reason="ffmpeg not installed")


def test_every_row_names_a_promo_shot_by_sha256_and_is_marked_ai():
    rows = ev.load_rows()
    assert len(rows) >= 10
    for r in rows:
        assert r["source"].startswith("promo/") and len(r["source_sha256"]) == 64
        assert r["file"] == r["event"] + ("-outcome.mp4" if "option" in r else ".mp4") and r["ai_generated"] is True
        if "option" in r:   # an outcome clip keeps a recorded 3.5 s window of its shot
            assert r["frames"] == 84 and r["start_frame"] >= 0
        assert (ev.OUT / r["file"]).is_file()


def test_the_encode_is_pinned_silent_and_has_no_burned_in_caption():
    """Owner ruling of 14 October 2026 (relayed by the coordinator): "the 3 ilustration clips are great but no need to say
    there were created by ai". The encode lays nothing over the shot: one input, no overlay, no drawtext, no label."""
    cmd = ev.encode_command(ev.REPO / "in.mp4", ev.REPO / "out.mp4")
    assert "-an" in cmd and "+faststart" in cmd and "libx264" in cmd and "28" in cmd
    assert cmd.count("-i") == 1, "the shot is the only input (no label image)"
    for row in (None, {"start_frame": 5, "frames": 84, "decimate": True}):
        graph = ev.filter_graph(row)
        assert "crop=960:540" in graph and graph.endswith("[out]")
        assert "overlay" not in graph and "drawtext" not in graph and "[1:v]" not in graph, graph
    assert not hasattr(ev, "BUG") and not hasattr(ev, "LABEL_EN"), "no caption is kept in the encoder"
    assert not (ev.REPO / "tools/event_videos/ai-bug.png").exists(), "the caption image is gone"


def test_provenance_stays_ai_generated_on_every_row():
    """The caption goes; the provenance stays: every row of the mapping, and every clip's ASSETS.yaml row, says
    ai_generated: true."""
    import yaml
    rows = ev.load_rows()
    assert rows and all(r["ai_generated"] is True for r in rows)
    assets = {r["glob"]: r for r in yaml.safe_load((ev.REPO / "ASSETS.yaml").read_text(encoding="utf-8"))["rules"]}
    for r in rows:
        assert assets["unity/Assets/StreamingAssets/video/" + r["file"]]["ai_generated"] is True, r["file"]


def test_the_owner_reviewed_exactly_the_three_sensitive_clips():
    """Owner ruling of 14 October 2026: the three sensitive clips (operation_yoav, ten_days, the operation_horev outcome)
    are reviewed; no other row changes."""
    rows = ev.load_rows()
    assert sorted(r["file"] for r in rows if r["sensitive"]) == ["operation_horev-outcome.mp4", "operation_yoav.mp4", "ten_days.mp4"]
    assert all(r["sensitivity_reviewed"] is r["sensitive"] for r in rows)


def make(path, audio: bool, size="960x540"):
    cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", f"testsrc=size={size}:rate=24:duration=1"]
    if audio:
        cmd += ["-f", "lavfi", "-i", "sine=duration=1", "-c:a", "aac"]
    subprocess.run(cmd + ["-c:v", "libx264", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(path)], check=True)


@needs_ffmpeg
def test_problems_refuses_audio_and_the_wrong_size(tmp_path):
    good, loud, small = tmp_path / "good.mp4", tmp_path / "loud.mp4", tmp_path / "small.mp4"
    make(good, audio=False); make(loud, audio=True); make(small, audio=False, size="640x360")
    assert ev.problems(good) == []
    assert any("audio" in p for p in ev.problems(loud))
    assert any("960x540" in p for p in ev.problems(small))


@needs_ffmpeg
def test_every_shipped_clip_passes():
    for r in ev.load_rows():
        assert ev.problems(ev.OUT / r["file"], r.get("frames")) == [], r["file"]


@needs_ffmpeg
def test_problems_refuses_an_outcome_clip_of_the_wrong_length(tmp_path):
    clip = tmp_path / "c.mp4"
    make(clip, audio=False)   # 24 frames
    assert ev.problems(clip, 24) == []
    assert any("24 frames, not 84" in p for p in ev.problems(clip, 84))


@needs_ffmpeg
def test_steadiest_skips_a_morph_at_the_start(tmp_path):
    """A shot whose first second churns (a morph) and then settles: the steadiest 1 s window starts after the churn."""
    import numpy as np
    w, h, rng = 192, 108, np.random.default_rng(7)
    frames = [rng.integers(0, 255, (h, w), dtype=np.uint8) for _ in range(30)]
    frames += [np.full((h, w), 100 + i // 8, np.uint8) for i in range(66)]   # still, with a slow drift
    src = tmp_path / "shot.mp4"
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo", "-pix_fmt", "gray",
                    "-s", f"{w}x{h}", "-r", "24", "-i", "-", "-c:v", "libx264", "-qp", "0", "-pix_fmt", "yuv420p", str(src)],
                   input=b"".join(f.tobytes() for f in frames), check=True)
    start, numbers = ev.steadiest(src, seconds=1.0)
    assert start >= 30, (start, numbers)


def test_only_selects_a_loop_or_an_outcome_by_its_file(monkeypatch, tmp_path, capsys):
    rows = [{"event": "e", "file": "e.mp4", "source": "promo/x.mp4", "source_sha256": "0" * 64},
            {"event": "e", "option": "o", "file": "e-outcome.mp4", "source": "promo/y.mp4", "source_sha256": "0" * 64}]
    monkeypatch.setattr(ev, "load_rows", lambda mapping=ev.MAPPING: rows)
    monkeypatch.setattr(ev.shutil, "which", lambda name: "/usr/bin/" + name)
    assert ev.main(["--root", str(tmp_path), "--only", "e-outcome"]) == 1
    assert "y.mp4" in capsys.readouterr().out, "only the outcome row is looked at"


def test_a_source_with_the_wrong_sha256_is_refused(tmp_path, capsys, monkeypatch):
    row = dict(ev.load_rows()[0])
    (tmp_path / "promo").mkdir()
    src = tmp_path / row["source"]
    src.parent.mkdir(parents=True, exist_ok=True)
    src.write_bytes(b"not the shot")
    monkeypatch.setattr(ev, "load_rows", lambda mapping=ev.MAPPING: [row])
    monkeypatch.setattr(ev.shutil, "which", lambda name: "/usr/bin/" + name)
    assert ev.main(["--root", str(tmp_path), "--out", str(tmp_path / "out")]) == 1
    assert "SHA-256 MISMATCH" in capsys.readouterr().out
    assert not (tmp_path / "out").exists() or not any((tmp_path / "out").iterdir())


def test_an_outcome_row_cuts_its_recorded_window_and_decimates_a_dreamina_shot():
    graph = ev.filter_graph({"start_frame": 5, "frames": 84, "decimate": True})
    assert graph.startswith("[0:v]mpdecimate") and "trim=start_frame=5:end_frame=89" in graph
    assert "trim" not in ev.filter_graph() and "mpdecimate" not in ev.filter_graph(), "the card loops are cut as before"
