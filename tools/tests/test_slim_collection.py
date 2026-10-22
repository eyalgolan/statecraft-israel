"""The original #507/#336 models retain their canonical slimming inputs and runtime categories.
The catalog also streams independently generated infantry, outside that converter; all use the import caps.
"""
import re
from pathlib import Path

import pytest

import slim_collection as slim

REPO = Path(__file__).resolve().parents[2]
CATALOG = REPO / "unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs"
IMPORT_TESTS = REPO / "unity/Assets/Tests/EditMode/CollectionImportTests.cs"

# These characters have their own generated sources and rigging/export pipeline, not #507/#336 FBX inputs.
GENERATED_PEOPLE = [
    "figure-syrian-infantry-1948-v1",
    "figure-ala-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
]


def catalog_bundles() -> dict[str, list[str]]:
    text = CATALOG.read_text(encoding="utf-8")
    bundles = {}
    for name, body in re.findall(r"\((People|Vehicles|Animals|Towns), new\[\]\s*\{(.*?)\}\)", text, re.S):
        bundles[name] = re.findall(r'"([a-z0-9-]+)"', body)
    return bundles


def test_the_original_50_slim_inputs_and_generated_infantry_partition_the_catalog():
    bundles = catalog_bundles()
    assert bundles == {
        "People": slim.PEOPLE + GENERATED_PEOPLE,
        "Vehicles": slim.VEHICLES,
        "Animals": slim.ANIMALS,
        "Towns": slim.TOWNS,
    }
    assert len(slim.IDS) == len(set(slim.IDS)) == 50
    assert not set(GENERATED_PEOPLE) & set(slim.IDS), "new generated sources must not enter the original FBX converter"
    assert "figure-egyptian-infantry-1948-v1" not in sum(bundles.values(), []), "Egyptian art stays in review"
    assert "figure-egyptian-infantry-1948-v1" not in slim.IDS
    assert all(not i.startswith("b") for i in slim.IDS), "the buildings are #659's"


def test_the_towns_are_the_nine_of_507_and_the_jerusalem_variant_of_336():
    # Owner ruling, 17 October 2026 (#663): "The nine whole-town models ship". Each is #507's folder name. The Jerusalem
    # 3denhance variant is #336's only, and comes in beside them, not in place of #507's Jerusalem (owner, 19 October
    # 2026, on #708: "Yes, both", under the terms of #708's eight figures).
    assert slim.TOWNS == [
        "tel-aviv-white-city-v1", "jerusalem-stone-city-v1", "haifa-port-city-v1", "moshava-town-v1", "nahalal-moshav-v1",
        "kibbutz-compound-v2", "stone-village-v1", "maabara-camp-v1", "shikun-housing-v1",
        "jerusalem-stone-city-v1-3denhance",
    ]


# Issue #708: the eight figures offered only in #336 (owner, 18 Oct 2026, #708: "the 8 figures in #708 can be merged
# without his license line"), and #886's pioneer farmer (owner, 19 Oct 2026: "Yes, both"), people like C01-C13, so they
# stream in the people bundle after the twelve of #507.
FIGURES_336 = [
    "figure-arab-villager-v1", "figure-builder-1950-v1", "figure-elder-immigrant-v1", "figure-immigrant-child-v1",
    "figure-immigrant-woman-v1", "figure-kibbutz-woman-v1", "figure-soldier-1948-v1", "figure-yemenite-immigrant-v1",
    "figure-pioneer-farmer-v1-tpose",
]


def test_the_nine_figures_of_336_are_people_after_the_twelve_of_507():
    assert slim.PEOPLE[12:] == FIGURES_336
    assert not set(FIGURES_336) & set(slim.POSE_CLIP)


class _Actions:
    def __init__(self, *names):
        self.data = type("data", (), {"actions": [type("action", (), {"name": n})() for n in names]})()


def test_the_idle_clip_is_found_with_or_without_blenders_numbered_suffix():
    # #507 names it "Armature|Armature|idle"; #336's FBX files carry "Armature|idle.001" (docs/command-models.json there),
    # and without the suffix match the eight rigged figures of #336 would ship static (the farmer in his T-pose).
    found = slim.find_idle(_Actions("Armature|Armature|run", "Armature|Armature|idle"), ["rig"])
    assert found.name == "Armature|Armature|idle"
    assert slim.find_idle(_Actions("Armature|walk.001", "Armature|idle.001"), ["rig"]).name == "Armature|idle.001"
    assert slim.find_idle(_Actions("Armature|Armature|idle_walk", "Armature|idle.01", "Armature|idle.0012"), ["rig"]) is None
    assert slim.find_idle(_Actions("Armature|idle"), []) is None, "no rig, no idle"


def test_a_pose_clip_poses_only_a_person_without_an_idle_clip():
    # Issue #665: C05 has no idle clip and a T-pose rest, so his static meshes stand at his walk's first frame.
    assert slim.POSE_CLIP == {"figure-c05-pioneer-veteran-v2": "idle_walk"}
    animated = re.search(r"Animated = new HashSet<string>.*?\{(.*?)\};", CATALOG.read_text(encoding="utf-8"), re.S).group(1)
    for mid in slim.POSE_CLIP:
        assert mid in slim.PEOPLE and f'"{mid}"' not in animated, mid


def test_the_caps_match_the_unity_import_tests():
    text = IMPORT_TESTS.read_text(encoding="utf-8")
    assert f"FarTriangles = {slim.FAR_TRIS}," in text
    assert f"FarTrianglesVehicle = {slim.FAR_TRIS_VEHICLE}," in text
    assert f"FarTrianglesTown = {slim.FAR_TRIS_TOWN}," in text
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
