"""tools/rig_soldier_1948.py: the 1948 soldier's rigged copy (#2384) ships as its record says, and the cross-section rules
that place its joints read a figure the way they should.

The shipped FBX and colour map match provenance.json, which names Tal Heuberger's two unchanged originals as the inputs; the
record has C07's 67 bones, the three takes the token plays and no unweighted vertex; the catalog streams and animates the copy.
The ring and landmark rules are checked on a box figure built here (two legs, a torso, two hanging arms, a neck and a head)
without Blender.
"""
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs"


def tool():
    spec = importlib.util.spec_from_file_location("rig_soldier_1948", ROOT / "tools/rig_soldier_1948.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def box(verts, polys, x0, x1, y0, y1, z0, z1):
    """A closed box's six quads, appended to verts and polys."""
    base = len(verts)
    verts += [(x, y, z) for z in (z0, z1) for y in (y0, y1) for x in (x0, x1)]
    for a, b, c, d in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        polys.append((base + a, base + b, base + c, base + d))


def box_figure():
    """A metre-tall figure: legs 0 to 0.45, a torso 0.45 to 0.80 half a width of 0.15, arms beside it from 0.42 to 0.70 (x 0.18 to 0.26),
    a neck 0.80 to 0.86 half a width of 0.04 and a head 0.86 to 1.0 half a width of 0.08."""
    verts, polys = [], []
    for sign in (-1, 1):
        box(verts, polys, min(sign * 0.02, sign * 0.13), max(sign * 0.02, sign * 0.13), -0.06, 0.06, 0.0, 0.45)      # legs
        box(verts, polys, min(sign * 0.18, sign * 0.26), max(sign * 0.18, sign * 0.26), -0.04, 0.04, 0.42, 0.70)     # arms
    box(verts, polys, -0.15, 0.15, -0.08, 0.08, 0.45, 0.80)   # torso
    box(verts, polys, -0.04, 0.04, -0.04, 0.04, 0.80, 0.86)   # neck
    box(verts, polys, -0.08, 0.08, -0.08, 0.08, 0.86, 1.0)    # head
    return verts, polys


def test_verify_passes_on_the_shipped_files():
    record = tool().verify(ROOT)
    assert record["bones"] == 67 and sorted(record["takes"]) == ["fire", "idle", "walk"]
    assert record["take_names"] == ["Armature|Armature|fire", "Armature|Armature|idle", "Armature|Armature|walk"]
    assert record["unweighted_vertices"] == 0 and record["max_influences"] <= 4
    assert record["creator_credit"] == "Tal Heuberger" and record["licence"] == "CC-BY-NC-ND-4.0"


def test_the_originals_are_the_recorded_inputs_and_untouched_by_the_tool():
    mod = tool()
    record = json.loads((ROOT / mod.COLLECTION / mod.OUT / "provenance.json").read_text(encoding="utf-8"))
    manifest = json.loads((ROOT / "docs/original-models.json").read_text(encoding="utf-8"))
    pinned = {Path(f["path"]).name: f["shipped_sha256"] for f in manifest["files"] if f["source"] in ("507", "336")}
    for name, digest in record["inputs"].items():
        assert pinned[name] == digest, f"{name}: the tool's input is the original docs/original-models.json pins"


def test_the_catalog_streams_and_animates_the_rigged_copy_and_keeps_the_original():
    mod = tool()
    text = CATALOG.read_text(encoding="utf-8")
    assert f'Soldier1948Rigged = "{mod.OUT}"' in text
    people = re.search(r"\(People, new\[\]\s*\{(.*?)\}", text, re.S).group(1)
    assert f'"{mod.SOURCE}"' in people and "Soldier1948Rigged" in people
    animated = re.search(r"Animated = new HashSet<string>\(StringComparer\.Ordinal\)\s*\{(.*?)\};", text, re.S).group(1)
    assert "Soldier1948Rigged" in animated and f'"{mod.SOURCE}"' not in animated


def test_rings_follow_the_mesh_across_uv_seams():
    mod = tool()
    verts, polys = [], []
    box(verts, polys, -0.1, 0.1, -0.1, 0.1, 0.0, 0.5)
    rings = mod.rings_at(verts, mod.welded(verts, polys), 0.25)
    assert len(rings) == 1 and rings[0].n == 4 and rings[0].spans_centre, "the four vertical edges, one ring"
    # The same box with every face on its own vertices (as Tripo's seam-split meshes are) chains into one ring once welded.
    split_verts, split_polys = [], []
    for poly in polys:
        base = len(split_verts)
        split_verts += [verts[i] for i in poly]
        split_polys.append(tuple(range(base, base + 4)))
    assert len(mod.rings_at(split_verts, split_polys, 0.25)) == 4, "unwelded, each side's crossings stay apart"
    assert len(mod.rings_at(split_verts, mod.welded(split_verts, split_polys), 0.25)) == 1


def test_landmarks_read_a_box_figure():
    mod = tool()
    sections = mod.Sections(*box_figure())
    assert abs(sections.top - 1.0) < 1e-9
    assert abs(sections.crotch - 0.455) < 1e-6, "the first ring across the centre line, just above the legs"
    assert abs(sections.fingertip - 0.425) < 1e-6, "the arms' lowest ring"
    assert abs(sections.armpit - 0.695) < 1e-6, "the arms' highest ring"
    assert abs(sections.collar - 0.805) < 1e-6, "the first ring above the arms narrower than the collar width"
    assert 0.80 < sections.neck < 0.86, "the narrowest ring between the collar and the head"
    assert abs(sections.arm(0.6).xc - 0.22) < 1e-6 and abs(sections.leg(0.2).xc - 0.075) < 1e-6
    assert sections.arm(0.2) is None and sections.leg(0.6) is None and sections.torso(0.2) is None
    assert abs(sections.foot_length() - 0.12) < 1e-6
