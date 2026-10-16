"""tools/event_images.py: the event-card photographs are cropped to 16:9, resized, graded (luminance, levels, the
navy-sepia tone) and nothing else; downloads are checked by SHA-1 before they are read."""
import hashlib
import io
from pathlib import Path

import pytest
from PIL import Image

import event_images as ei


def test_the_crop_box_is_sixteen_by_nine_and_inside_the_picture():
    for size, crop in [((2000, 1334), [0.5, 0.5, 1.0]), ((686, 1200), [0.5, 0.52, 0.94]), ((1476, 932), [0.38, 0.6, 0.7]),
                       ((1000, 300), [0.5, 0.5, 1.0]), ((1000, 1000), [0.0, 1.0, 0.5])]:
        left, top, right, bottom = ei.crop_box(size, crop)
        assert 0 <= left < right <= size[0] and 0 <= top < bottom <= size[1]
        assert abs((right - left) / (bottom - top) - 16 / 9) < 0.02
    assert ei.crop_box((1000, 300), [0.5, 0.5, 1.0])[3] - ei.crop_box((1000, 300), [0.5, 0.5, 1.0])[1] == 300   # too short: full height
    with pytest.raises(ValueError):
        ei.crop_box((100, 100), [0.5, 0.5, 1.5])


def test_the_grade_is_a_tone_of_the_luminance_only():
    src = Image.new("RGB", (1800, 1200))
    src.putdata([((x * 7) % 256, (y * 3) % 256, 128) for y in range(1200) for x in range(1800)])
    out = ei.grade(src, [0.5, 0.5, 1.0])
    assert out.size == (960, 540) and out.size[0] % 4 == 0 and out.size[1] % 4 == 0
    lut = ei.tone_lut()
    tones = {(lut[v], lut[256 + v], lut[512 + v]) for v in range(256)}
    assert set(out.get_flattened_data()) <= tones   # every pixel is a tone of one grey level: no colour added
    assert (lut[0], lut[256], lut[512]) == ei.SHADOW and (lut[255], lut[511], lut[767]) == ei.HIGHLIGHT


def test_a_small_original_is_not_enlarged_and_stays_block_aligned():
    out = ei.grade(Image.new("L", (686, 1200), 90), [0.5, 0.52, 0.94])
    assert out.size == (640, 360)                          # 645 px wide crop -> 640 (a multiple of 64), never upscaled


def test_encode_stays_under_the_size_cap():
    noisy = Image.effect_noise((960, 540), 120).convert("RGB")
    data = ei.encode(noisy)
    assert len(data) <= ei.MAX_BYTES
    assert Image.open(io.BytesIO(data)).info.get("exif") is None


@pytest.fixture
def as_isolated(monkeypatch):
    monkeypatch.setattr(ei, "isolated", lambda: True)


def test_it_refuses_to_run_without_python_dash_i(tmp_path, capsys, monkeypatch):
    monkeypatch.setattr(ei, "isolated", lambda: False)
    assert ei.main(["--originals", str(tmp_path), "--out", str(tmp_path)]) == 2
    assert "python -I" in capsys.readouterr().out


def original_for(row, folder):
    """A stand-in original with the row's size; the row's SHA-1 is set to match it (rows are dicts, edited in memory)."""
    path = folder / f"{row['event']}.jpg"
    Image.new("RGB", tuple(row["original_size"]), (90, 90, 90)).save(path)
    row["sha1"] = hashlib.sha1(path.read_bytes()).hexdigest()
    return path


def test_a_size_mismatch_is_refused_and_nothing_is_written(tmp_path, capsys, monkeypatch, as_isolated):
    rows = [dict(r) for r in ei.load_rows()[:3]]
    originals, out = tmp_path / "originals", tmp_path / "out"
    originals.mkdir(); out.mkdir()
    for r in rows:
        original_for(r, originals)
    rows[2]["original_size"] = [rows[2]["original_size"][0] + 1, rows[2]["original_size"][1]]   # the last row is wrong
    monkeypatch.setattr(ei, "load_rows", lambda mapping=ei.MAPPING: rows)
    assert ei.main(["--originals", str(originals), "--out", str(out)]) == 1
    assert "SIZE MISMATCH" in capsys.readouterr().out
    assert list(out.iterdir()) == []                                   # rows 1 and 2 were good, but nothing was written


def test_good_originals_are_all_written(tmp_path, capsys, monkeypatch, as_isolated):
    rows = [dict(r) for r in ei.load_rows()[:2]]
    originals, out = tmp_path / "originals", tmp_path / "out"
    originals.mkdir(); out.mkdir()
    for r in rows:
        original_for(r, originals)
    monkeypatch.setattr(ei, "load_rows", lambda mapping=ei.MAPPING: rows)
    assert ei.main(["--originals", str(originals), "--out", str(out)]) == 0
    assert sorted(p.name for p in out.iterdir()) == sorted(r["file"] for r in rows)


def test_a_download_with_the_wrong_sha1_is_refused(tmp_path, capsys, as_isolated):
    row = ei.load_rows()[0]
    bad = tmp_path / f"{row['event']}.jpg"
    Image.new("RGB", (10, 10)).save(bad)
    assert hashlib.sha1(bad.read_bytes()).hexdigest() != row["sha1"]
    assert ei.main(["--originals", str(tmp_path), "--out", str(tmp_path), "--only", row["event"]]) == 1
    assert "SHA-1 MISMATCH" in capsys.readouterr().out


def test_every_row_has_a_shipped_file_and_a_crop():
    rows = ei.load_rows()
    assert len(rows) == 25   # first_new_yishuv, the objective card (#380)
    for r in rows:
        assert (ei.FOLDER / r["file"]).is_file()
        assert len(r["crop"]) == 3 and len(r["sha1"]) == 40
        with Image.open(ei.FOLDER / r["file"]) as im:
            assert im.size[0] * 9 == im.size[1] * 16



ISOLATED_IMPORT = ("import importlib.util, sys; s = importlib.util.spec_from_file_location('m', sys.argv[1]); "
                   "s.loader.exec_module(importlib.util.module_from_spec(s))")


# routes_accuracy.py has the same fix but needs scipy, which the tools tests do not install
@pytest.mark.parametrize("tool", ["event_images.py", "encode_event_videos.py", "localities_bake.py", "land_bake.py",
                                  "routes_1948.py"])
def test_imports_under_python_isolated(tool):
    """`python -I` (the documented way to run event_images.py, which reads untrusted downloads) leaves the script's own
    folder off sys.path, so a tool must add it itself to import its siblings (yaml12, hexgrid). Imported, not run."""
    import subprocess
    import sys
    path = Path(ei.__file__).with_name(tool)
    r = subprocess.run([sys.executable, "-I", "-c", ISOLATED_IMPORT, str(path)], capture_output=True, text=True, cwd="/")
    assert r.returncode == 0, r.stderr[-400:]


def test_documented_isolated_command_starts():
    import subprocess
    import sys
    r = subprocess.run([sys.executable, "-I", str(Path(ei.__file__)), "--help"], capture_output=True, text=True, cwd="/")
    assert r.returncode == 0 and "--originals" in r.stdout, r.stderr[-400:]
