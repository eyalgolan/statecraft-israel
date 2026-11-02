"""tools/generated_towns.py --verify: Tal Heuberger's four #2085 whole towns ship in the Unity Collection as his delivered files.

Each town's GLB under asset-sources/ matches the hash its Unity folder's provenance.json records, the Unity colour map is
his delivered JPG byte for byte (the image the GLB embeds), and the compatibility FBX is the one the conversion recorded.
"""
import importlib.util
import json
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]


def towns():
    spec = importlib.util.spec_from_file_location("generated_towns", ROOT / "tools/generated_towns.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def test_verify_passes_on_the_shipped_files():
    towns().verify(ROOT)


def test_the_four_towns_are_tals_2085_exports_with_their_recorded_triangles():
    mod = towns()
    assert list(mod.TOWNS) == ["tiberias-town-v1", "safed-town-v1", "netanya-town-v1", "ramla-town-v1"]
    expected = {"tiberias-town-v1": 5002, "safed-town-v1": 4485, "netanya-town-v1": 3428, "ramla-town-v1": 4426}
    for mid, triangles in expected.items():
        record = json.loads((ROOT / mod.COLLECTION / mid / "provenance.json").read_text(encoding="utf-8"))
        assert record["triangles"] == triangles and record["triangle_connectivity"] == "identical"
        assert record["decimation"] is False and record["welding"] is False
        assert record["geometry_max_vertex_error"] < 1e-6 and record["uv_max_error"] < 1e-6
        assert record["creator_credit"] == "Tal Heuberger" and record["licence"] == "CC-BY-NC-ND-4.0"


def test_verify_rejects_a_changed_colour_map(tmp_path):
    mod = towns()
    for mid, rel in mod.TOWNS.items():
        for src in (mod.SOURCES / rel, (mod.SOURCES / rel).with_name(Path(rel).stem + "_basecolor.jpg")):
            (tmp_path / src).parent.mkdir(parents=True, exist_ok=True)
            (tmp_path / src).write_bytes((ROOT / src).read_bytes())
        dest = tmp_path / mod.COLLECTION / mid
        dest.mkdir(parents=True)
        for name in (f"{mid}.fbx", f"{mid}_basecolor.jpg", "provenance.json"):
            (dest / name).write_bytes((ROOT / mod.COLLECTION / mid / name).read_bytes())
    mod.verify(tmp_path)
    jpg = tmp_path / mod.COLLECTION / "safed-town-v1" / "safed-town-v1_basecolor.jpg"
    jpg.write_bytes(jpg.read_bytes() + b"\0")
    with pytest.raises(AssertionError, match="byte for byte"):
        mod.verify(tmp_path)
