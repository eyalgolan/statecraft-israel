"""tools/c07_nokeffiyeh.py: C07's keffiyeh-free copy (owner, 3 November 2026: "no israeli soldir should wear a khafia on their
neck") ships as its record says, and the code draws it in the delivered C07's place.

The shipped FBX and colour map match provenance.json, which names Tal Heuberger's unchanged C07 files as the inputs; the record
has C07's 67 bones, all nine takes, no unweighted vertex, no scarf-sized patch of the scarf's texels in the neck box and the
holes closed; the catalog streams and animates the copy and keeps the original; the tokens and the hex tiles draw the copy.
The texel classifier is checked on plain colours, without Blender.
"""
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs"
FIGURES = ROOT / "unity/Assets/Scripts/Map/Units/UnitFigures.cs"
HEX_MODELS = ROOT / "unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs"


def tool():
    spec = importlib.util.spec_from_file_location("c07_nokeffiyeh", ROOT / "tools/c07_nokeffiyeh.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def test_verify_passes_on_the_shipped_files():
    mod = tool()
    record = mod.verify(ROOT)
    assert record["bones"] == 67 and sorted(record["takes"]) == sorted(mod.TAKES)
    assert record["take_names"] == sorted(f"Armature|Armature|{t}" for t in mod.TAKES)
    assert record["unweighted_vertices"] == 0 and record["max_influences"] <= 4
    assert record["scarf_texels_in_box"]["scarf_patches"] == 0
    assert record["open_edges_in_box"]["new_in_copy"] <= mod.OPEN_EDGES_ALLOWED
    assert record["scarf_faces_deleted"] > 4000 and record["new_faces"] > 1000
    assert record["creator_credit"] == "Tal Heuberger" and record["licence"] == "CC-BY-NC-ND-4.0"


def test_the_originals_are_the_recorded_inputs_and_untouched_by_the_tool():
    mod = tool()
    record = json.loads((ROOT / mod.COLLECTION / mod.OUT / "provenance.json").read_text(encoding="utf-8"))
    manifest = json.loads((ROOT / "docs/original-models.json").read_text(encoding="utf-8"))
    pinned = {Path(f["path"]).name: f["shipped_sha256"] for f in manifest["files"] if f["source"] in ("507", "336")}
    for name, digest in record["inputs"].items():
        assert pinned[name] == digest, f"{name}: the tool's input is the original docs/original-models.json pins"
    assert record["files"][f"{mod.OUT}_basecolor.jpg"] == pinned[f"{mod.SOURCE}_basecolor.jpg"], "the map is Tal's byte for byte"


def test_the_code_draws_the_copy_and_keeps_the_delivered_c07_catalogued():
    mod = tool()
    text = CATALOG.read_text(encoding="utf-8")
    assert f'VeteranFighterNoKeffiyeh = "{mod.OUT}"' in text
    people = re.search(r"\(People, new\[\]\s*\{(.*?)\}", text, re.S).group(1)
    assert f'"{mod.SOURCE}"' in people and "VeteranFighterNoKeffiyeh" in people
    animated = re.search(r"Animated = new HashSet<string>\(StringComparer\.Ordinal\)\s*\{(.*?)\};", text, re.S).group(1)
    assert "VeteranFighterNoKeffiyeh" in animated and f'"{mod.SOURCE}"' in animated, "the delivered C07 keeps its idle (catalogued, unused)"
    assert "VeteranFighter = CollectionCatalog.VeteranFighterNoKeffiyeh" in FIGURES.read_text(encoding="utf-8")
    assert '("proto:figure-veteran-fighter-c07-1948-v2", CollectionCatalog.VeteranFighterNoKeffiyeh, "near")' in HEX_MODELS.read_text(encoding="utf-8")


def test_the_texel_classifier_tells_the_scarf_from_the_sweater_and_the_skin():
    mod = tool()
    assert mod.kind((0.55, 0.53, 0.50)) == "white", "the scarf's off-white"
    assert mod.kind((0.27, 0.26, 0.21)) == "cloth", "the sweater's olive"
    assert mod.kind((0.10, 0.09, 0.09)) == "cloth", "the scarf's black pattern is not white (found by enclosure, not by tone)"
    assert mod.kind((0.68, 0.47, 0.42)) == "skin"
    assert mod.kind((0.62, 0.48, 0.40)) == "skin"
