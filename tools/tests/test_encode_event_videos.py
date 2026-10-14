"""tools/encode_event_videos.py: the decision-card clips are silent 960x540 H.264 with faststart, made only from the
promo shot each row names (by SHA-256), with the burned-in label; Deport and other content rules live in the Unity-side
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
        assert r["file"] == r["event"] + ".mp4" and r["ai_generated"] is True
        assert (ev.OUT / r["file"]).is_file()


def test_the_encode_is_pinned_silent_and_labelled():
    cmd = ev.encode_command(ev.BUG.parent / "in.mp4", ev.BUG.parent / "out.mp4")
    assert "-an" in cmd and "+faststart" in cmd and "libx264" in cmd and "28" in cmd
    graph = ev.filter_graph()
    assert "crop=960:540" in graph and f"overlay={ev.SAFE_X}:{ev.SAFE_Y}" in graph
    assert ev.BUG.is_file()


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
        assert ev.problems(ev.OUT / r["file"]) == [], r["file"]


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
