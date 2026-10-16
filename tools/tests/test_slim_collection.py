"""tools/slim_collection.py (issue #664) slims the same 31 models, in the same categories, that the game's catalog
(unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs) streams; the caps match the Unity import tests."""
import re
from pathlib import Path

import pytest

import slim_collection as slim

REPO = Path(__file__).resolve().parents[2]
CATALOG = REPO / "unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs"
IMPORT_TESTS = REPO / "unity/Assets/Tests/EditMode/CollectionImportTests.cs"


def catalog_bundles() -> dict[str, list[str]]:
    text = CATALOG.read_text(encoding="utf-8")
    bundles = {}
    for name, body in re.findall(r"\((People|Vehicles|Animals), new\[\]\s*\{(.*?)\}\)", text, re.S):
        bundles[name] = re.findall(r'"([a-z0-9-]+)"', body)
    return bundles


def test_the_tool_slims_the_31_models_the_catalog_streams():
    bundles = catalog_bundles()
    assert bundles == {"People": slim.PEOPLE, "Vehicles": slim.VEHICLES, "Animals": slim.ANIMALS}
    assert len(slim.IDS) == len(set(slim.IDS)) == 31
    assert all(not i.startswith("b") for i in slim.IDS), "the buildings are #659's"


def test_the_caps_match_the_unity_import_tests():
    text = IMPORT_TESTS.read_text(encoding="utf-8")
    assert f"FarTriangles = {slim.FAR_TRIS}," in text
    assert f"FarTrianglesVehicle = {slim.FAR_TRIS_VEHICLE}," in text
    assert f"NearTriangles = {slim.NEAR_TRIS}" in text
    assert f"MaxTextureSize = {slim.TEX_PX}" in text


def _fbx(*strings: bytes) -> bytes:
    body = b"".join(b"S" + len(s).to_bytes(4, "little") + s + b"\x00" * 4 for s in strings)
    return b"Kaydara FBX Binary  \x00\x1a\x00" + b"\x00" * 16 + body


def test_scrub_blanks_the_export_and_source_paths_in_utf8_and_utf16(tmp_path):
    # Blender writes the export path as the document URL; PR #680 also carried the texture's absolute path.
    src, out = tmp_path / "src", tmp_path / "out" / "m"
    src.mkdir()
    out.mkdir(parents=True)
    fbx = out / "m-far.fbx"
    blob = _fbx(str(fbx).encode(), str(fbx).encode("utf-16-le"), (str(src) + "/m/m_basecolor.jpg").encode(), b"mesh")
    fbx.write_bytes(blob)
    assert slim.scrub(fbx, src) == 3
    data = fbx.read_bytes()
    assert len(data) == len(blob), "same-length scrub keeps the FBX valid"
    assert str(tmp_path).encode() not in data and str(tmp_path).encode("utf-16-le") not in data
    assert b"m_basecolor.jpg" in data and b"mesh" in data


def test_scrub_refuses_and_deletes_a_file_with_any_other_temp_or_session_path(tmp_path):
    fbx = tmp_path / "m-far.fbx"
    fbx.write_bytes(_fbx(("/private/" + "tmp/claude-501/elsewhere/m.jpg").encode("utf-16-le")))
    with pytest.raises(SystemExit, match="local path"):
        slim.scrub(fbx, tmp_path)
    assert not fbx.exists()
