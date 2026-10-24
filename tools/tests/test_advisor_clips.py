"""tools/advisor_clips.py: the advisors' clips are a silent picture held to the decision cards' encode and the exact
16-bit PCM WAV the lip-sync used, each named by a media row and recorded in ASSETS.yaml (advisors spec 11.4, 12.1;
plan Task 26, Step 7)."""
import shutil
import subprocess
import wave

import pytest

import advisor_clips as ac
import encode_event_videos as ev

needs_ffmpeg = pytest.mark.skipif(shutil.which("ffmpeg") is None or shutil.which("ffprobe") is None, reason="ffmpeg not installed")


@needs_ffmpeg
def test_every_shipped_picture_passes_the_encoders_checks():
    pictures = sorted(ac.OUT.glob("*.mp4"))
    assert pictures, "no advisor clip found: the glob is broken"
    for p in pictures:
        assert ev.problems(p) == [], p.name


@needs_ffmpeg
def test_the_shipped_clips_match_their_rows_and_records():
    assert ac.media_rows(), "no media row found"
    assert ac.check() == []


def _wav(path, width=2):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(width); w.setframerate(48000); w.writeframes(b"\x00" * width * 4800)
    return path


def test_only_a_16_bit_pcm_voice_is_accepted(tmp_path):
    assert ac.voice_problems(_wav(tmp_path / "ok.wav")) == []
    assert ac.voice_problems(_wav(tmp_path / "u8.wav", width=1))
    (tmp_path / "x.wav").write_bytes(b"not a wav")
    assert ac.voice_problems(tmp_path / "x.wav")


@needs_ffmpeg
def test_ingest_encodes_a_voiced_1080p_take_and_copies_a_conforming_picture(tmp_path):
    take = tmp_path / "take.mp4"
    subprocess.run(["ffmpeg", "-v", "error", "-f", "lavfi", "-i", "testsrc=size=1920x1080:rate=24:duration=1", "-f", "lavfi",
                    "-i", "sine=duration=1", "-c:v", "libx264", "-c:a", "aac", "-shortest", str(take)], check=True)
    assert ev.problems(take), "precondition: the take has sound and is 1080p"
    out = tmp_path / "advisors"
    pic, wav = ac.ingest("x.msg", take, _wav(tmp_path / "v.wav"), out=out)
    assert ev.problems(pic) == [] and wav.read_bytes() == (tmp_path / "v.wav").read_bytes()
    assert (out / "x.msg.mp4.meta").is_file() and (out / "x.msg.en.wav.meta").is_file()
    again, _ = ac.ingest("y.msg", pic, _wav(tmp_path / "v.wav"), out=out)
    assert again.read_bytes() == pic.read_bytes(), "a conforming picture is copied byte for byte"


@needs_ffmpeg
def test_ingest_refuses_a_voice_that_is_not_16_bit_pcm(tmp_path):
    with pytest.raises(ValueError):
        ac.ingest("x.msg", tmp_path / "unused.mp4", _wav(tmp_path / "u8.wav", width=1), out=tmp_path / "advisors")
    assert not (tmp_path / "advisors").exists()


@needs_ffmpeg
def test_check_names_an_orphan_and_a_missing_record(tmp_path):
    out = tmp_path / "advisors"; out.mkdir()
    for f in ac.OUT.iterdir():
        shutil.copyfile(f, out / f.name)
    (out / "stray.mp4").write_bytes(b"")
    found = ac.check(out=out)
    assert any("stray.mp4: no media row uses it" in f for f in found)
    assert any("no ASSETS.yaml row" in f for f in found), "files outside the repository have no ASSETS.yaml rows"
