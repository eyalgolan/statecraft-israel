"""Who held which areas, 1948-49: the true polygons (content/map/control_1948_49.geojson, tools/control_digitise.py) and
their per-hex, per-period layer (content/map/control_hexes_1948_49.yaml, tools/control_bake.py). Owner rulings of
2026-10-14: sides only from sourced data; grey only for the days an operation ran there; the 11 June snapshot may use the
line the archive map dates 9 July; the localities stay as of 1948 (no per-village dates)."""
import json
import re
from pathlib import Path

import pytest
import yaml

import borders_bake as bb
import control_bake as cb
import control_digitise as cd
import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
SOURCE = json.loads((MAP / "control_1948_49.geojson").read_text(encoding="utf-8"))
BAKED = yaml.safe_load((MAP / "control_hexes_1948_49.yaml").read_text(encoding="utf-8"))
HEXES = json.loads((MAP / "hexes.json").read_text())
META = SOURCE["statecraft"]
PERIODS = [p["id"] for p in BAKED["periods"]]
CODES = {(q, r): c for q, r, c in BAKED["hexes"]}


def code(lon: float, lat: float, period: str) -> str:
    return CODES[hg.settlement_hex(lon, lat, HEXES)][PERIODS.index(period)]


def test_the_bake_is_up_to_date_and_deterministic():
    text = cb.build()
    assert text == (MAP / "control_hexes_1948_49.yaml").read_text(encoding="utf-8")
    assert cb.build() == text


def test_periods_run_on_from_11_june_1948_without_gaps():
    ps = BAKED["periods"]
    assert str(ps[0]["from"]) == "1948-06-11", "no sourced line for 15 May 1948: the sub-districts until the first truce"
    for a, b in zip(ps, ps[1:]):
        assert str(a["until"]) == str(b["from"]), (a["id"], b["id"])
    assert ps[-1]["until"] is None
    assert str(BAKED["shown_from"]) == "1948-06-11"


def test_the_legend_turns_to_the_armistice_wording_front_by_front():
    by = {p["id"]: p for p in BAKED["periods"]}
    assert all(by[p]["legend"] == "held" and by[p]["fronts"] == [] for p in PERIODS if not p.startswith("arm_"))
    assert by["arm_1949-02-24"]["fronts"] == ["egypt"] and by["arm_1949-02-24"]["legend"] == "mixed"
    assert by["arm_1949-03-23"]["fronts"] == ["egypt", "lebanon"]
    assert by["arm_1949-04-03"]["fronts"] == ["egypt", "jordan", "lebanon"]
    assert by["arm_1949-07-20"]["fronts"] == ["egypt", "jordan", "lebanon", "syria"] and by["arm_1949-07-20"]["legend"] == "armistice"


def test_every_land_hex_of_mandatory_palestine_has_one_valid_code_per_period():
    assert set(CODES) == set(cb.mandate_hexes())
    valid = set(cb.HOLDER_CODE.values()) | set(cb.KIND_CODE.values())
    for qr, c in CODES.items():
        assert len(c) == len(PERIODS) and set(c) <= valid, qr


@pytest.mark.parametrize("place, lon, lat", [("Tel Aviv", 34.78, 32.08), ("Haifa", 34.9992, 32.8192), ("Rehovot", 34.8167, 31.8969)])
def test_the_coastal_plain_is_israeli_throughout(place, lon, lat):
    assert set(CODES[hg.settlement_hex(lon, lat, HEXES)]) == {"I"}, place


@pytest.mark.parametrize("place, lon, lat", [("Nablus", 35.2608, 32.2211), ("Hebron", 35.0986, 31.535), ("Gaza", 34.4597, 31.5075)])
def test_the_other_sides_heartlands_are_never_israeli(place, lon, lat):
    c = CODES[hg.settlement_hex(lon, lat, HEXES)]
    assert "I" not in c, (place, c)


def test_dated_changes_follow_the_sources():
    # Nazareth: the other side's on the 9 July line, contested in the Ten Days, Israeli from 18 July (Operation Dekel)
    assert code(35.3033, 32.7019, "truce1") in "VAM" and code(35.3033, 32.7019, "ten_days") == "c"
    assert code(35.3033, 32.7019, "truce2") == "I"
    # Lydda: the other side's, then Israeli (Operation Danny)
    assert code(34.8881, 31.9519, "truce1") != "I" and code(34.8881, 31.9519, "truce2") == "I"
    # Beersheba: the other side's until Yoav, Israeli on the October line
    assert code(34.7867, 31.2522, "truce2b") != "I" and code(34.7867, 31.2522, "october") == "I"
    # Tarshiha, in the Galilee pocket: contested only during Hiram, Israeli after
    assert code(35.2683, 33.009, "yoav") != "I" and code(35.2683, 33.009, "yoav_hiram") == "c" and code(35.2683, 33.009, "october") == "I"


def test_grey_contested_is_only_for_the_days_an_operation_ran():
    contested = {p for p in PERIODS for c in CODES.values() if c[PERIODS.index(p)] == "c"}
    assert contested <= {"ten_days", "shoter", "yoav", "yoav_hiram", "horev"}, contested
    ops = META["operations"]
    for f in SOURCE["features"]:
        p = f["properties"]
        if p.get("neutral_kind") == "contested":
            op = {"contested_ten_days": "ten_days", "contested_shoter": "shoter", "contested_yoav": "yoav",
                  "contested_hiram": "hiram", "contested_horev": "horev"}[p["id"]]
            assert (p["shown_from"], p["shown_until"]) == (ops[op]["from"], ops[op]["until"]), p["id"]


def test_jerusalem_is_split_between_its_two_hexes_or_no_mans_land():
    """The city's line (S/1025; Art. V.1(b)) falls between the hex of the Old City and the one west of it at this scale: the
    first is never Israeli, the second always is; a hex holding both sides, the smaller a fifth of it, would be grey (m)."""
    q, r = hg.settlement_hex(35.2256, 31.7789, HEXES)
    east, west = CODES[(q, r)], CODES[(q - 1, r)]
    assert "I" not in east and set(east) <= set("MJm"), east
    assert set(west) <= {"I", "m"}, west


def test_the_south_is_uncoloured_until_the_jordan_agreement_closes_it():
    eilat = CODES[hg.settlement_hex(34.95, 29.56, HEXES)]
    i = PERIODS.index("arm_1949-04-03")
    assert set(eilat[:i]) == {"-"}, "no 1948 line is sourced south of the scan's 60-km northing"
    assert set(eilat[i:]) == {"I"}


def test_demilitarized_zones_are_never_the_other_side():
    zones = [f for f in SOURCE["features"] if f["properties"].get("neutral_kind") == "dmz"]
    assert {f["properties"]["id"] for f in zones} >= {"dmz_egypt_el_auja_dmz", "dmz_syria_dmz_central"}
    assert all(f["properties"]["side"] == "neutral" for f in zones)


def test_every_feature_is_sourced_draft_and_awaits_the_owners_review():
    for f in SOURCE["features"]:
        p = f["properties"]
        assert p["status"] == "draft" and p["review"] == "owner-verify", p["id"]
        assert p["sources"] and p["checked"] == cd.CHECKED, p["id"]
        if p["kind"] == "area":
            assert p["source_quality"] == "low_res_scan" and "1-2 km" in p["accuracy"], p["id"]
            assert p["side"] in ("israel", "arab", "neutral", "unsided"), p["id"]


def test_only_the_three_front_lines_are_traced_never_the_partition_or_the_demanded_line():
    lines = [f["properties"] for f in SOURCE["features"] if f["properties"]["kind"] == "line"]
    assert {l["colour"] for l in lines} == {"red", "blue", "green"}
    assert {k for k, (c, _) in cd.STROKES.items()} == {l["id"] for l in lines}
    assert all(l["on_ink"] >= 0.5 for l in lines), [(l["id"], l["on_ink"]) for l in lines]
    assert any("partition" in t for t in META["not_traced"])


def test_the_closure_check_passed_for_every_snapshot():
    assert set(META["anchors"]) >= {"truce1", "truce2", "truce2b", "october", "arm_1949-04-03", "arm_1949-07-20"}
    for sid, rows in META["anchors"].items():
        assert rows and all(r["ok"] for r in rows), sid
        assert all(r["got"] == r["want"] or r["line_km"] < 3.0 for r in rows), sid


def test_the_scan_and_its_georeference_are_recorded():
    assert META["sources"][0]["sha256"] == cd.ISA_MAP["sha256"]
    assert max(META["georeference"]["rms_m"]) < 150, "the printed 10-km grid fits to about 0.1 km"
    assert META["searched_for_a_better_copy"]


def test_a_hex_takes_the_code_at_its_centre():
    snaps, overlays, periods = cb.load(SOURCE)
    drawn = [p for p in periods if p["snapshot"]]
    land = set(cb.mandate_hexes())
    checked = 0
    for h in HEXES["hexes"]:
        qr = (h["q"], h["r"])
        if qr not in land or abs(h["lon"] - 35.2256) < 0.1:
            continue
        p = drawn[PERIODS.index("truce2")]
        got = cb.code_at(h["lon"], h["lat"], snaps[p["snapshot"]], [overlays[o] for o in p["overlays"]])
        if got is not None:
            assert CODES[qr][PERIODS.index("truce2")] == got, qr
            checked += 1
    assert checked > 150


def test_the_sim_never_reads_the_control_files():
    """Display only: the content hash covers only files the sim reads (ContentLoader)."""
    for cs in (ROOT / "sim" / "Statecraft.Sim").rglob("*.cs"):
        assert not re.search(r"control_(1948|hexes)", cs.read_text(encoding="utf-8"), re.IGNORECASE), cs


def test_the_localities_stay_as_of_1948():
    """Owner ruling (b), 2026-10-14: the colour claims military control only; no locality gets a date or a fate."""
    loc = (MAP / "localities_1948.yaml").read_text(encoding="utf-8")
    assert "control_1948" not in loc and "held_by" not in loc
