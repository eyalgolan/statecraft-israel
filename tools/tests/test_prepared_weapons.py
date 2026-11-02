"""tools/prepared_weapons.py --verify: Tal Heuberger's #2083 K98k and Sten ship in the Unity Collection as their delivered files.

Each weapon's GLB under asset-sources/ matches the hash #2083's provenance.json and its Unity folder's provenance.json record,
the Unity colour map is their delivered JPG byte for byte (the image the GLB embeds), and the compatibility FBX is the one the
conversion recorded. The C06 point-fire export in the Unity Collection is Tal's file byte for byte.
"""
import hashlib
import importlib.util
import json
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]


def weapons():
    spec = importlib.util.spec_from_file_location("prepared_weapons", ROOT / "tools/prepared_weapons.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def test_verify_passes_on_the_shipped_files():
    weapons().verify(ROOT)


def test_the_k98k_and_sten_are_tals_2083_exports_with_their_recorded_triangles():
    mod = weapons()
    assert list(mod.WEAPONS) == ["weapon-k98k-v1", "weapon-sten-v1"]
    for mid, triangles in {"weapon-k98k-v1": 5485, "weapon-sten-v1": 4851}.items():
        record = json.loads((ROOT / mod.COLLECTION / mid / "provenance.json").read_text(encoding="utf-8"))
        assert record["triangles"] == triangles and record["triangle_connectivity"] == "identical"
        assert record["decimation"] is False and record["welding"] is False
        assert record["geometry_max_vertex_error"] < 1e-6 and record["uv_max_error"] < 1e-6
        assert record["creator_credit"] == "Tal Heuberger" and record["licence"] == "CC-BY-NC-ND-4.0"


def test_the_point_fire_export_in_unity_is_tals_file_byte_for_byte():
    tal = ROOT / "asset-sources/c06-point-fire-reference-20261031"
    record = json.loads((tal / "provenance.json").read_text(encoding="utf-8"))
    want = next(f["sha256"] for f in record["files"] if f["path"] == "C06-POINT-FIRE-PROVISIONAL-PUBLIC.fbx")
    ours = ROOT / "unity/Assets/Art/Collection/c06-point-fire-v1/C06-POINT-FIRE-PROVISIONAL-PUBLIC.fbx"
    assert hashlib.sha256(ours.read_bytes()).hexdigest() == want


def test_verify_rejects_a_changed_colour_map(tmp_path):
    mod = weapons()
    (tmp_path / mod.SOURCES).mkdir(parents=True)
    (tmp_path / mod.SOURCES / "provenance.json").write_bytes((ROOT / mod.SOURCES / "provenance.json").read_bytes())
    for mid, rel in mod.WEAPONS.items():
        for src in (mod.SOURCES / rel, (mod.SOURCES / rel).with_name(Path(rel).stem + "_basecolor.jpg")):
            (tmp_path / src).parent.mkdir(parents=True, exist_ok=True)
            (tmp_path / src).write_bytes((ROOT / src).read_bytes())
        dest = tmp_path / mod.COLLECTION / mid
        dest.mkdir(parents=True)
        for name in (f"{mid}.fbx", f"{mid}_basecolor.jpg", "provenance.json"):
            (dest / name).write_bytes((ROOT / mod.COLLECTION / mid / name).read_bytes())
    mod.verify(tmp_path)
    jpg = tmp_path / mod.COLLECTION / "weapon-sten-v1" / "weapon-sten-v1_basecolor.jpg"
    jpg.write_bytes(jpg.read_bytes() + b"\0")
    with pytest.raises(AssertionError, match="byte for byte"):
        mod.verify(tmp_path)
