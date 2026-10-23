"""The 1948 sub-districts: the true lines (content/map/subdistricts_1948.geojson, tools/borders_digitise.py) and their
hex-snapped display layer (content/map/subdistrict_hexes_1948.yaml, tools/borders_bake.py)."""
import json
import re
from pathlib import Path

import numpy as np
import pytest
import yaml12

import borders_bake as bb
import borders_digitise as bd
import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
SOURCE = json.loads((MAP / "subdistricts_1948.geojson").read_text(encoding="utf-8"))
HEXES = json.loads((MAP / "hexes.json").read_text())
BAKED = yaml12.load((MAP / "subdistrict_hexes_1948.yaml").read_text(encoding="utf-8"))
TOWNS = {t["id"]: t for t in yaml12.load((MAP / "settlements_1948.yaml").read_text())["settlements"]}
# The 1947 Schedule (Palestine Gazette No. 1552, Supplement No. 2): six districts, sixteen sub-districts.
GAZETTE_1947 = {
    "Galilee District": ["Acre Sub-District", "Beisan Sub-District", "Nazareth Sub-District", "Safad Sub-District", "Tiberias Sub-District"],
    "Haifa District": ["Haifa Sub-District"],
    "Samaria District": ["Jenin Sub-District", "Nablus Sub-District", "Tulkarm Sub-District"],
    "Jerusalem District": ["Hebron Sub-District", "Jerusalem Sub-District", "Ramallah Sub-District"],
    "Lydda District": ["Jaffa Sub-District", "Ramle Sub-District"],
    "Gaza District": ["Beersheba Sub-District", "Gaza Sub-District"],
}

GAZETTE_HE_1945 = {
    "acre": "נפת עכו", "beisan": "נפת בית־שאן", "nazareth": "נפת נצרת", "safad": "נפת צפת",
    "tiberias": "נפת טבריה", "haifa": "נפת חיפה", "jenin": "נפת ג'נין", "nablus": "נפת שכם",
    "tulkarm": "נפת טול כרם", "hebron": "נפת חברון", "jerusalem": "נפת ירושלים",
    "ramallah": "נפת ראם־אללה", "jaffa": "נפת יפו", "ramle": "נפת רמלה", "gaza": "נפת עזה",
    "beersheba": "נפת באר־שבע",
}


def region_of(q: int, r: int) -> str:
    keys = {x["id"]: x["key"] for x in BAKED["regions"]}
    cells = {(q_, r_): rid for q_, r_, rid in BAKED["hexes"]}
    return keys.get(cells.get((q, r), 0), "none")


# --- the true lines ---

def test_every_sub_district_of_the_1947_schedule_once_with_its_district():
    got = {}
    for f in SOURCE["features"]:
        got.setdefault(f["properties"]["district"], []).append(f["properties"]["name"])
    assert {k: sorted(v) for k, v in got.items()} == {k: sorted(v) for k, v in GAZETTE_1947.items()}


def test_each_feature_carries_name_sources_checked_date_and_reviewed_status():
    assert {f["properties"]["id"]: f["properties"]["name_he"] for f in SOURCE["features"]} == GAZETTE_HE_1945
    for f in SOURCE["features"]:
        p = f["properties"]
        assert p["name"] and p["name_he"] and p["name_ar"], p["id"]
        assert p["sources"] and all(s.startswith("https://") for s in p["sources"]), p["id"]
        assert p["checked"] == "2026-10-11" and p["status"] == "sensitivity-reviewed", p["id"]   # owner-delegated, 2026-10-11
        assert p["names_source"].startswith("name_he / district_he: The Palestine Gazette (Hebrew edition) No. 1415"), p["id"]
        assert "name_ar / district_ar: Wikipedia" in p["names_source"], p["id"]
        assert p["in_force_from"] == "1947-01-01", p["id"]
    meta = SOURCE["statecraft"]
    assert meta["status"] == "sensitivity-reviewed" and meta["sources"] and meta["licence_note"] and meta["uncertain_segments"]
    assert not any("amendment" in q for q in meta["open_questions"]), "the owner closed the 1947-48 amendment check"
    assert "section 3.3" in meta["provenance"] and "#196 comment 6359825082" in meta["provenance"]
    assert all(gap in meta["provenance"] for gap in ("29 April to 14 May 1948", "typescript", "Hebrew schedule", "village lists"))
    assert all("district_ar_note" in f["properties"] for f in SOURCE["features"] if f["properties"]["district"] == "Samaria District")


def test_rings_are_closed_counter_clockwise_and_in_the_map():
    b = HEXES["bounds"]
    for f in SOURCE["features"]:
        for poly in bb.rings(f["geometry"]):
            for i, ring in enumerate(poly):
                assert len(ring) >= 4 and (ring[0] == ring[-1]).all(), f["properties"]["id"]
                area2 = np.sum(ring[:-1, 0] * ring[1:, 1] - ring[1:, 0] * ring[:-1, 1])
                assert (area2 > 0) == (i == 0), f"{f['properties']['id']}: RFC 7946 winding"
                assert (ring[:, 0] >= b["lon0"]).all() and (ring[:, 0] <= b["lon1"]).all()
                assert (ring[:, 1] >= b["lat0"]).all() and (ring[:, 1] <= b["lat1"]).all()


# Village Statistics 1945 follows the 1945 Schedule, so 1947 adjustments count as error; Beersheba's figure is a rounded
# estimate (nominal check) and Gaza against Beersheba is their shared line (the Bureir connector): 5% for those two.
AREA_TOLERANCE = {"gaza": 0.05, "beersheba": 0.05}


def test_areas_agree_with_village_statistics_1945():
    """The independent check: each digitised area within 4% of the Government's own land area for it (5% for Gaza and
    Beersheba, see AREA_TOLERANCE)."""
    for f in SOURCE["features"]:
        p = f["properties"]
        assert abs(p["area_km2"] / p["vs1945_area_km2"] - 1) < AREA_TOLERANCE.get(p["id"], 0.04), p["id"]


def test_two_prints_of_the_map_agree():
    for f in SOURCE["features"]:
        c = f["properties"]["cross_check"]
        assert c["iou"] > 0.94 and c["mean_offset_km"] < 0.3 and c["max_offset_km"] < 1.5, f["properties"]["id"]


def test_digitiser_constants_match_the_file():
    assert [f["properties"]["id"] for f in SOURCE["features"]] == [s[0] for s in bd.SUBDISTRICTS]
    assert sorted(bd.VS1945_DUNAMS) == sorted(s[0] for s in bd.SUBDISTRICTS)
    # every seed lies inside its own polygon
    for f in SOURCE["features"]:
        lon, lat = f["properties"]["seed"]
        assert bb.contains(bb.rings(f["geometry"]), lon, lat), f["properties"]["id"]


def test_affine_georeference_round_trips():
    pts = [(lo, la, 100 + 2900 * (lo - 35) + 3 * (la - 30), 12300 - 3490 * (la - 30) + 2 * (lo - 35))
           for lo in (35, 36) for la in (30, 31, 32, 33)]
    cx, cy, rms = bd.fit_affine(pts)
    assert rms < 1e-6
    x, y = bd.to_px(cx, cy, 35.3, 31.7)
    lon, lat = bd.to_lonlat(cx, cy, x, y)
    assert abs(lon - 35.3) < 1e-9 and abs(lat - 31.7) < 1e-9


# --- the snap ---

def test_point_in_polygon_respects_holes_and_edges():
    sq = [np.array([[0, 0], [4, 0], [4, 4], [0, 4], [0, 0]], float), np.array([[1, 1], [1, 3], [3, 3], [3, 1], [1, 1]], float)]
    assert bb.contains([sq], 0.5, 0.5) and not bb.contains([sq], 2, 2) and not bb.contains([sq], 5, 2)
    assert bb.ring_contains(sq[0], 0.0, 2.0) != bb.ring_contains(sq[0], 4.0, 2.0)   # half-open: one edge in, its twin out


def test_committed_layer_is_up_to_date_and_deterministic():
    text = bb.build()
    assert (MAP / "subdistrict_hexes_1948.yaml").read_text(encoding="utf-8") == text, "rerun: uv run tools/borders_bake.py"
    assert bb.build() == text


def test_every_land_hex_and_only_land_has_a_region():
    land = {(h["q"], h["r"]) for h in HEXES["hexes"] if h["land"]}
    assert {(q, r) for q, r, _ in BAKED["hexes"]} == land


def test_a_land_hex_inside_a_sub_district_takes_it_by_its_centre():
    """Rule 2, except the hexes of Gazette-listed places (rule 5)."""
    shapes = {f["properties"]["id"]: bb.rings(f["geometry"]) for f in SOURCE["features"]}
    listed = set(bb.place_hexes(bb.listed_places(list(TOWNS.values())), HEXES))
    for h in HEXES["hexes"]:
        if not h["land"] or (h["q"], h["r"]) in listed:
            continue
        inside = [k for k, s in shapes.items() if bb.contains(s, h["lon"], h["lat"])]
        if inside:
            assert region_of(h["q"], h["r"]) == inside[0], (h["q"], h["r"])


def test_every_sub_district_shows_and_its_hex_count_matches_its_area():
    hex_km2 = 1.5 * 3 ** 0.5 * HEXES["hex_size_km"] ** 2
    counts = {}
    for q, r, rid in BAKED["hexes"]:
        counts[rid] = counts.get(rid, 0) + 1
    for i, f in enumerate(SOURCE["features"], start=1):
        n, expected = counts.get(i, 0), f["properties"]["area_km2"] / hex_km2
        assert n >= 1 and abs(n - expected) <= max(3, 0.25 * expected), (f["properties"]["id"], n, expected)


def test_a_town_a_hex_deep_inside_its_sub_district_has_its_hex_there():
    """The snap moves a line by up to about half a hex, so a town at least one hex size (5 km) from every line of its
    sub-district must be drawn in it (settlements_1948.yaml and the places below)."""
    shapes = {f["properties"]["id"]: bb.rings(f["geometry"]) for f in SOURCE["features"]}
    km_lon = hg.km_per_deg_lon(HEXES["bounds"])
    checked = 0
    for name, (lon, lat) in [(t, (TOWNS[t]["lon"], TOWNS[t]["lat"])) for t in TOWNS] + [(k, v[1:3]) for k, v in PLACES.items()]:
        inside = [k for k, sh in shapes.items() if bb.contains(sh, lon, lat)]
        if len(inside) != 1 or bb.distance_km(shapes[inside[0]], lon, lat, km_lon) < HEXES["hex_size_km"]:
            continue
        q, r = hg.settlement_hex(lon, lat, HEXES)
        assert region_of(q, r) == inside[0], name
        checked += 1
    assert checked >= 12   # 14 on 2026-10-11


@pytest.mark.parametrize("lon, lat", [(35.25, 33.30), (35.85, 32.55), (34.35, 30.40), (35.75, 30.50)])
def test_land_beyond_the_frontier_is_outside(lon, lat):
    (q,), (r,) = hg.point_to_hex(np.array([(lon - HEXES["bounds"]["lon0"]) * hg.km_per_deg_lon(HEXES["bounds"])]),
                                 np.array([(HEXES["bounds"]["lat1"] - lat) * hg.KM_PER_DEG_LAT]), HEXES["hex_size_km"])
    assert region_of(q, r) == bb.OUTSIDE


@pytest.mark.parametrize("hexqr, key", [((-21, 57), bb.OUTSIDE), ((-20, 56), "beersheba"), ((-15, 30), bb.OUTSIDE)])
def test_shore_hexes_follow_their_own_coast(hexqr, key):
    """Review M7: a shore hex takes the side of its nearest land. Off the Sinai coast at Taba the hex is outside, Umm
    Rashrash (Eilat) is Beersheba; the shore hex west of Rafah's frontier is outside. (Rafah town's own hex is (-15, 31):
    see test_a_listed_places_hex_takes_its_sub_district.)"""
    assert region_of(*hexqr) == key


def test_a_listed_places_hex_takes_its_sub_district():
    """Rule 5 (controller ruling, owner-delegated, 2026-10-11): the hex the game puts a Gazette-listed place on takes its
    sub-district, never outside. Pinned: Rafah town (Gaza, p. 133) on (-15, 31), whose centre is 1.7 km outside Gaza in
    Sinai, and Metulla (Safad, p. 127) on (15, 1), whose centre is about 2.9 km outside Safad."""
    places = bb.listed_places(list(TOWNS.values()))
    for name, sub, lon, lat in places:
        assert region_of(*hg.settlement_hex(lon, lat, HEXES)) == sub, name
    assert hg.settlement_hex(34.25861, 31.27250, HEXES) == (-15, 31) and region_of(-15, 31) == "gaza"
    assert hg.settlement_hex(35.57444, 33.27889, HEXES) == (15, 1) and region_of(15, 1) == "safad"


def test_two_sub_districts_listed_on_one_hex_fail_the_bake_even_if_one_matches_the_centre():
    """Review 3 of rule 5: a hex holding listed places of two sub-districts is a conflict to resolve by hand, even when
    the centre rule already gives one of them; the bake must not pass it quietly."""
    places = [("town_a", "gaza", 34.25861, 31.27250), ("town_b", "beersheba", 34.25861, 31.27250)]
    ids = {"gaza": 1, "beersheba": 2}
    with pytest.raises(ValueError, match="different sub-districts"):
        bb.apply_places({(-15, 31): ids["gaza"]}, places, HEXES, ids)


def test_the_layer_shows_from_the_start_until_replaced_with_its_label():
    """Owner ruling 2026-10-11 (I4): from the 1948 start until the armistice lines replace it, labelled."""
    assert BAKED["label"] == "Mandate sub-districts, 1947"
    assert str(BAKED["shown_from"]) == "1948-05-15" and BAKED["shown_until"] is None


def test_regions_are_the_sub_districts_then_outside_and_drawn_primary():
    rows = BAKED["regions"]
    assert [r["key"] for r in rows] == [f["properties"]["id"] for f in SOURCE["features"]] + [bb.OUTSIDE]
    assert [r["id"] for r in rows] == list(range(1, len(rows) + 1))
    assert all(r["style"] == "primary" for r in rows[:-1]) and rows[-1]["style"] == "none"


def test_the_sim_never_reads_the_border_files():
    """Display only: the content hash covers only files the sim reads (ContentLoader), so these must stay out of it."""
    for cs in (ROOT / "sim" / "Statecraft.Sim").rglob("*.cs"):
        text = cs.read_text(encoding="utf-8")
        assert not re.search(r"subdistrict", text, re.IGNORECASE), cs


# Places -> sub-district as listed in the 1947 Schedule: borders_bake.GAZETTE_SETTLEMENTS (settlements_1948.yaml rows) and
# borders_bake.GAZETTE_PLACES (Wikipedia coordinates), each with its Gazette page; they also drive the bake's override
# (rule 5). None is a digitiser seed (review I3): the seeds are the chief towns, so those would pass by construction.
# Exception: Metulla passes the true-line test by the digitiser's village patch (PATCHES["villages"]): the 1:750,000
# map draws it on the frontier's tip, so its row checks the patch, not an independent line.
GAZETTE_MEMBERS = bb.GAZETTE_SETTLEMENTS
PLACES = bb.GAZETTE_PLACES
# A place within this of its own sub-district passes outside it: the stated accuracy is about +-1 km, and these are known
# near-line cases (also in the GeoJSON's open_questions).
NEAR_LINE_KM = 0.3
NEAR_LINE = {"Ra'anana"}


def check_member(name: str, sub: str, lon: float, lat: float) -> None:
    shapes = {f["properties"]["id"]: bb.rings(f["geometry"]) for f in SOURCE["features"]}
    inside = [k for k, sh in shapes.items() if bb.contains(sh, lon, lat)]
    if inside == [sub]:
        return
    km = bb.distance_km(shapes[sub], lon, lat, hg.km_per_deg_lon(HEXES["bounds"]))
    assert name in NEAR_LINE and km <= NEAR_LINE_KM, (name, inside, f"{km:.2f} km from {sub}")


@pytest.mark.parametrize("town", sorted(GAZETTE_MEMBERS))
def test_the_true_lines_put_each_listed_town_in_its_gazette_sub_district(town):
    check_member(town, GAZETTE_MEMBERS[town][0], TOWNS[town]["lon"], TOWNS[town]["lat"])


@pytest.mark.parametrize("place", sorted(PLACES))
def test_the_true_lines_put_each_listed_place_in_its_gazette_sub_district(place):
    sub, lon, lat, _page = PLACES[place]
    check_member(place, sub, lon, lat)
