"""The 1949 armistice lines: the Palestine-grid conversion, the true lines (content/map/armistice_lines_1949.geojson,
tools/armistice_digitise.py) and their hex-snapped display layer (content/map/armistice_hexes_1949.yaml,
tools/armistice_bake.py), and the per-front ends they give the 1948 sub-district layer (tools/borders_bake.py)."""
import json
import math
import re
from pathlib import Path

import numpy as np
import pytest
import yaml

import armistice_bake as ab
import armistice_digitise as ad
import borders_bake as bb

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
SOURCE = json.loads((MAP / "armistice_lines_1949.geojson").read_text(encoding="utf-8"))
BAKED = yaml.safe_load((MAP / "armistice_hexes_1949.yaml").read_text(encoding="utf-8"))
SUBD = yaml.safe_load((MAP / "subdistrict_hexes_1948.yaml").read_text(encoding="utf-8"))
HEXES = json.loads((MAP / "hexes.json").read_text())
FEATS = {f["properties"]["id"]: f for f in SOURCE["features"]}
DATES = {"egypt": "1949-02-24", "lebanon": "1949-03-23", "jordan": "1949-04-03", "syria": "1949-07-20"}


def km(lon_a, lat_a, lon_b, lat_b) -> float:
    return math.hypot((lon_a - lon_b) * 111.32 * math.cos(math.radians(lat_a)), (lat_a - lat_b) * 110.57)


# --- the conversion -------------------------------------------------------------------------------------------------

def test_the_pinned_pipeline_is_pyprojs_epsg_28191_to_wgs84():
    pyproj = pytest.importorskip("pyproj")
    best = pyproj.transformer.TransformerGroup("EPSG:28191", "EPSG:4326", always_xy=True).transformers[0]
    assert "Helmert" in best.description or best.accuracy == 2.0, best.description
    for _, e, n in ad.SYRIA_POINTS + [(None, 87.5, 46.8), (None, 192.5, 95.8)]:
        lon, lat = ad.mr_to_lonlat(e, n)
        lon2, lat2 = best.transform(e * 1000, n * 1000)
        assert abs(lon - lon2) < 1e-9 and abs(lat - lat2) < 1e-9


@pytest.mark.parametrize("e, n, lon, lat, what", [
    (208.7, 294.2, 35.6255, 33.2426, "Syria Annex I.1: the Lebanon-Syria-Palestine tripoint"),
    (87.5, 46.8, 34.3462, 31.0094, "El Auja DMZ, Art. VIII.2: the frontier point NW of the Rafah-El Auja road"),
    (192.5, 95.8, 35.4469, 31.4540, "Jordan Art. V.1(d): MR 1925-0958 on the Dead Sea's west shore"),
])
def test_known_mr_points_convert_to_the_research_notes_values(e, n, lon, lat, what):
    pytest.importorskip("pyproj")
    got = ad.mr_to_lonlat(e, n)
    assert abs(got[0] - lon) < 5e-5 and abs(got[1] - lat) < 5e-5, what


def test_the_conversion_round_trips():
    pytest.importorskip("pyproj")
    lon, lat = ad.mr_to_lonlat(209.6, 239.0)
    e, n = ad.lonlat_to_mr(lon, lat)
    assert abs(e - 209.6) < 1e-4 and abs(n - 239.0) < 1e-4   # 10 cm: the Helmert step inverts approximately (about 1 cm)


def test_mr_values_are_km_with_no_false_northing_offset():
    """EPSG:28193 (Israeli CS Grid) would add 1,000 km to the northings; the agreements' MR values are EPSG:28191."""
    pytest.importorskip("pyproj")
    lon, lat = ad.mr_to_lonlat(208.7, 294.2)
    assert 33.0 < lat < 33.5 and 35.4 < lon < 35.8


# --- the sources typed in -------------------------------------------------------------------------------------------

def test_the_syria_annex_points_are_the_24_stops_of_annex_i_in_order():
    labels = [p[0] for p in ad.SYRIA_POINTS]
    assert len(labels) == 24 and labels[0] == "1" and labels[-1] == "24"
    assert ("BP61", 210.6, 246.3) in ad.SYRIA_POINTS and ("BP66", 211.7, 240.1) in ad.SYRIA_POINTS
    assert set(ad.SYRIA_STRETCHES) == set(zip(labels, labels[1:])), "every stretch between consecutive points is defined"
    for (u, v), (how, why) in ad.SYRIA_STRETCHES.items():
        assert how in ("line", "map") and why.startswith("Annex I."), (u, v)
        if "along" in why or "Marabout" in why:
            assert how == "map", f"{u}-{v}: the text follows a feature, so it is traced on the map"


def test_the_el_auja_points_are_art_viii_2s():
    assert [(e, n) for _, e, n in ad.EL_AUJA] == [(87.5, 46.8), (96.5, 41.4), (107.8, 28.5), (99.5, 14.5)]


# --- the true lines -------------------------------------------------------------------------------------------------

def test_fronts_are_the_four_agreements_and_jordan_is_low_accuracy():
    fronts = {f["key"]: f for f in SOURCE["statecraft"]["fronts"]}
    assert list(fronts) == ["egypt", "lebanon", "jordan", "syria"]
    for k, f in fronts.items():
        assert f["signed"] == DATES[k] and f["agreement"] and f["document"]
        assert f["data"] is True and f["shown_from"] == DATES[k]
    j = fronts["jordan"]
    assert j["source_quality"] == "low_res_scan" and "full-resolution" in j["low_accuracy"]
    assert "Jerusalem" in j["gap"] and "Mount Scopus" in j["gap"] and len(j["searched"]) >= 5
    for stage in ("1949-05-08", "1949-05-22", "1949-07-17"):
        assert stage in j["dating"]
    assert SOURCE["statecraft"]["partition"]["closed"] is False


def test_every_jordan_feature_is_marked_low_accuracy_and_staged():
    jo = {k: f["properties"] for k, f in FEATS.items() if f["properties"]["front"] == "jordan"}
    assert set(jo) == {"jordan_adl_iraqi_a_triangle", "jordan_adl_iraqi_b_wadi_ara", "jordan_adl_iraqi_c_east",
                       "jordan_adl_iraqi_c_south", "jordan_adl_a_latrun", "jordan_adl_bc_south", "jordan_adl_d_arava"}
    for k, p in jo.items():
        assert p["source_quality"] == "low_res_scan" and "replaced" in p["source_quality_note"], k
        assert p["accuracy"].startswith("LOW ACCURACY"), k
        assert ad.JORDAN_MAP["url"] in p["sources"], k
    assert jo["jordan_adl_iraqi_a_triangle"]["shown_from"] == "1949-05-08"
    assert jo["jordan_adl_iraqi_b_wadi_ara"]["shown_from"] == "1949-05-22"
    assert jo["jordan_adl_iraqi_c_east"]["shown_from"] == jo["jordan_adl_iraqi_c_south"]["shown_from"] == "1949-07-17"
    for k in ("jordan_adl_a_latrun", "jordan_adl_bc_south", "jordan_adl_d_arava"):
        assert jo[k]["shown_from"] == "1949-04-03"


def test_the_jordan_georeference_is_measured_not_assumed():
    """Control points on the map's town symbols: rms and leave-one-out errors recorded, about 1-2.5 km."""
    for k in ("jordan_adl_a_latrun", "jordan_adl_d_arava"):
        acc = FEATS[k]["properties"]["accuracy"]
        assert "leave-one-out" in acc and "51 m a pixel" in acc
    assert len(ad.JORDAN_CONTROL) >= 25


def test_the_commons_copy_without_provenance_is_never_a_source():
    for f in SOURCE["features"]:
        assert not any("commons.wikimedia.org" in s and "Green_Line" in s for s in f["properties"]["sources"])
    assert "not used" in " ".join(ad.JORDAN_SEARCH)


def test_the_1950_modus_vivendi_is_its_own_dated_feature():
    for k in ("egypt_mv1950_demarcation", "egypt_mv1950_zone_lines"):
        p = FEATS[k]["properties"]
        assert p["shown_from"] == p["signed"] == "1950-02-22" and "modus vivendi" in p["agreement"].lower()
        assert p["document"] == "S/1471" and p["label_note"] == "1950 modus vivendi"
    assert "not drawn" in FEATS["egypt_mv1950_demarcation"]["properties"]["gaps"]
    assert FEATS["egypt_adl_gaza"]["properties"]["shown_from"] == "1949-02-24"


def test_each_feature_is_dated_sourced_and_draft():
    assert {"lebanon_adl", "syria_adl", "syria_dmz_north", "syria_dmz_central", "syria_dmz_south",
            "egypt_adl_gaza", "egypt_el_auja_dmz", "egypt_mv1950_demarcation"} <= set(FEATS)
    for fid, f in FEATS.items():
        p = f["properties"]
        assert p["shown_from"] >= p["signed"], fid
        if "mv1950" not in fid:
            assert p["signed"] == DATES[p["front"]], fid
        assert p["status"] == "draft" and p["checked"] == ad.CHECKED, fid
        assert p["sources"] and all(s.startswith("https://") for s in p["sources"]), fid
        assert p["accuracy"] and p["method"] and p["agreement"] and p["document"], fid
        assert p["kind"] == ("zone" if f["geometry"]["type"] in ("Polygon", "MultiPolygon") else "line"), fid
    meta = SOURCE["statecraft"]
    assert meta["status"] == "draft" and meta["licence_note"] and "sensitivity" in meta


def test_no_later_green_line_drawing_is_a_source():
    """Owner ruling: a later drawing may only be a named cross-check (IBS 46 of 1965), unless it is its own dated feature
    (the signed 1950 modus vivendi map, owner ruling 2026-10-11)."""
    for s in SOURCE["statecraft"]["sources"]:
        if "1965" in s["cite"]:
            assert "cross-check" in s["cite"] + s["licence"], s["cite"]
    for f in SOURCE["features"]:
        if f["properties"]["signed"] != "1950-02-22":
            assert not any("s1471" in u.lower() or "khan_yunis" in u for u in f["properties"]["sources"]) or f["properties"]["id"] == "egypt_adl_gaza"
    for f in SOURCE["features"]:
        for url in f["properties"]["sources"]:
            assert "Green_Line" not in url and "Talknic" not in url, url


def test_the_syria_line_passes_through_every_annex_point_in_order():
    pytest.importorskip("pyproj")
    coords = FEATS["syria_adl"]["geometry"]["coordinates"]
    i = 0
    for label, e, n in ad.SYRIA_POINTS:
        lon, lat = ad.mr_to_lonlat(e, n)
        while i < len(coords) and km(*coords[i], lon, lat) > 0.002:
            i += 1
        assert i < len(coords), f"Annex I point {label} not on the line (in order)"


def test_the_syria_line_and_zones_agree_where_the_zone_edge_is_the_line():
    """Two scans, two georeferences: where Art. V.5 makes the line the zone's edge, they agree to about 0.1 km."""
    agree = SOURCE["statecraft"]["diagnostics"]["syria"]["line_to_zone_edge_median_m"]
    for stretch in ("1b-5a", "11-12", "12-13", "13-14", "14-15", "15-16", "16-17", "BP61-BP66", "21-22"):
        assert agree[stretch] <= 100, (stretch, agree[stretch])
    pts = FEATS["syria_adl"]["properties"]["diagnostics"]["annex_points_to_green_m"]
    assert np.median(list(pts.values())) < 60 and max(pts.values()) < 150


def test_the_demilitarized_zone_is_three_parts_of_about_65_km2():
    areas = {k: FEATS[k]["properties"]["area_km2"] for k in ("syria_dmz_north", "syria_dmz_central", "syria_dmz_south")}
    assert 60 <= sum(areas.values()) <= 70, areas
    assert areas["syria_dmz_north"] < areas["syria_dmz_central"] < areas["syria_dmz_south"] + 10


def test_rings_are_closed_and_lines_are_inside_the_map():
    b = HEXES["bounds"]
    for fid, f in FEATS.items():
        g = f["geometry"]
        rings = [g["coordinates"]] if g["type"] == "LineString" else (g["coordinates"] if g["type"] in ("Polygon", "MultiLineString") else [r for p in g["coordinates"] for r in p])
        for r in rings:
            if g["type"] in ("Polygon", "MultiPolygon"):
                assert r[0] == r[-1] and len(r) >= 4, fid
            for lon, lat in r:
                assert b["lon0"] - 0.1 <= lon <= b["lon1"] + 0.1 and b["lat0"] - 0.1 <= lat <= b["lat1"] + 0.1, fid


def test_the_el_auja_zone_has_the_four_art_viii_points_as_corners():
    pytest.importorskip("pyproj")
    ring = FEATS["egypt_el_auja_dmz"]["geometry"]["coordinates"][0]
    for name, e, n in ad.EL_AUJA:
        lon, lat = ad.mr_to_lonlat(e, n)
        assert min(km(x, y, lon, lat) for x, y in ring) < 0.002, name


def test_the_lebanon_line_is_the_frontier_from_the_coast_to_the_tripoint():
    pytest.importorskip("pyproj")
    c = FEATS["lebanon_adl"]["geometry"]["coordinates"]
    tri = ad.mr_to_lonlat(208.7, 294.2)
    ends = (c[0], c[-1])
    assert min(km(*e, *ad.NAQURA) for e in ends) < 2.0, "west end at Ras en Naqura"
    assert min(km(*e, *tri) for e in ends) < 1.0, "east end at the tripoint of the Syria agreement's Annex I"
    assert 70 < sum(km(*a, *b) for a, b in zip(c, c[1:])) < 130


def test_the_1949_egypt_line_runs_from_the_wadi_hasi_to_gaza_and_the_rest_is_a_stated_gap():
    pytest.importorskip("pyproj")
    p = FEATS["egypt_adl_gaza"]["properties"]
    c = FEATS["egypt_adl_gaza"]["geometry"]["coordinates"]
    e0, n0 = ad.lonlat_to_mr(*c[0])
    assert abs(e0 - 102.5) < 0.3 and abs(n0 - 112.8) < 0.3, "starts at the Wadi Hasi mouth (sheet 9 'Gaza')"
    ds = ad.mr_to_lonlat(107.5, 109.0)
    assert min(km(x, y, *ds) for x, y in c) < 1.0, "through Deir Suneid (the Art. VI.3 outpost 'on the north side of the Wadi')"
    assert min(ad.lonlat_to_mr(x, y)[1] for x, y in c) > 99.5, "not drawn south of Gaza: no source for that stretch"
    assert "Cemetery" in p["gap"] and "not drawn" in p["gap"]
    assert not any(f["properties"]["front"] == "egypt" and f["properties"]["kind"] == "zone" and "Gaza" in f["properties"]["name"]
                   for f in SOURCE["features"]), "the Egyptian-held area needs the whole line"


def test_the_digitised_file_is_written_one_vertex_a_line_and_round_trips():
    text = (MAP / "armistice_lines_1949.geojson").read_text(encoding="utf-8")
    assert json.loads(text) == SOURCE
    assert re.search(r'\n    \[\d+\.\d+,\d+\.\d+\]', text), "one vertex per line, for reviewable diffs"


# --- the display layer ----------------------------------------------------------------------------------------------

def test_the_bake_is_reproducible():
    assert ab.build() == (MAP / "armistice_hexes_1949.yaml").read_text(encoding="utf-8")


def test_the_baked_layer_lists_the_fronts_zones_and_lines_with_their_dates():
    assert BAKED["layer"] == "armistice_1949" and BAKED["label"] and "nearest hex edge" in BAKED["line"]
    fronts = {f["key"]: f for f in BAKED["fronts"]}
    assert {k: str(f["shown_from"]) for k, f in fronts.items()} == DATES
    assert fronts["jordan"]["low_accuracy"] is True and fronts["egypt"]["low_accuracy"] is False
    for z in BAKED["zones"]:
        assert z["style"] == "secondary" and str(z["shown_from"]) == DATES[z["front"]]
    lines = {x["key"]: x for x in BAKED["lines"]}
    assert str(lines["egypt_mv1950_demarcation"]["signed"]) == "1950-02-22" and "modus vivendi" in lines["egypt_mv1950_demarcation"]["agreement"].lower()
    assert all(x["low_accuracy"] for x in BAKED["lines"] if x["front"] == "jordan")
    keys = {z["key"] for z in BAKED["zones"]} | {ln["key"] for ln in BAKED["lines"]}
    assert keys == set(FEATS)


def test_zone_hexes_are_land_hexes_of_the_subdistrict_layer_and_lines_are_on_the_map():
    land = {(q, r) for q, r, _ in SUBD["hexes"]}
    on_map = {(h["q"], h["r"]) for h in HEXES["hexes"]}
    zone_ids = {z["id"] for z in BAKED["zones"]}
    line_ids = {ln["id"] for ln in BAKED["lines"]}
    cells = [(q, r) for q, r, _ in BAKED["zone_hexes"]]
    assert len(cells) == len(set(cells))
    for q, r, z in BAKED["zone_hexes"]:
        assert (q, r) in land and z in zone_ids
    for q, r, ln in BAKED["line_hexes"]:
        assert (q, r) in on_map and ln in line_ids
    for ln in line_ids:
        assert any(x == ln for _, _, x in BAKED["line_hexes"]), f"line {ln} names some hex"


def test_zones_too_narrow_for_a_hex_are_listed_not_widened():
    """Snapping by centre: a zone with no hex centre inside stays in the true lines only, and the bake says so."""
    with_hex = {z["key"] for z in BAKED["zones"] if any(x == z["id"] for _, _, x in BAKED["zone_hexes"])}
    assert set(BAKED["no_hex"]) == {z["key"] for z in BAKED["zones"]} - with_hex


def test_line_hexes_follow_the_line():
    grid = HEXES
    hexes = ab.line_hexes([[35.0, 32.0], [35.05, 32.0]], grid)
    assert len(hexes) >= 1 and len(hexes) == len(set(hexes))


# --- the sub-district layer's per-front ends ------------------------------------------------------------------------

def test_the_subdistrict_layer_ends_per_front_on_each_agreement_with_a_line():
    fronts = {f["key"]: str(f["until"]) for f in SUBD["fronts"]}
    assert fronts == DATES
    assert SUBD["shown_until"] is None, "the lines do not enclose each side's area yet, so the layer stays as the base"


def test_the_layer_ends_as_a_whole_only_when_every_front_has_a_line_and_they_close():
    fake = {"statecraft": {"fronts": [{"key": "a", "data": True, "shown_from": "1949-02-24"},
                                      {"key": "b", "data": True, "shown_from": "1949-07-20"}], "partition": {"closed": True}}}
    source = json.loads((MAP / "subdistricts_1948.geojson").read_text(encoding="utf-8"))
    text = bb.render(source, [], {}, fake)
    assert "shown_until: 1949-07-20" in text
    fake["statecraft"]["partition"]["closed"] = False
    assert "shown_until: null" in bb.render(source, [], {}, fake), "not while the lines leave gaps"
    fake["statecraft"]["partition"]["closed"] = True
    fake["statecraft"]["fronts"].append({"key": "c", "data": False, "shown_from": None})
    assert "shown_until: null" in bb.render(source, [], {}, fake)


def test_the_sim_never_reads_the_armistice_files():
    """Display only: the content hash covers only files the sim reads (ContentLoader)."""
    for cs in (ROOT / "sim" / "Statecraft.Sim").rglob("*.cs"):
        assert not re.search(r"armistice_(lines|hexes)", cs.read_text(encoding="utf-8"), re.IGNORECASE), cs


def test_the_1950_line_follows_the_drawn_dash_dot_not_the_sheet_frame_or_grid():
    """Review C-1: piece 1 overlies the 1949 line (the dash-dot lies on it); no run of the trace sits on an integer-km grid
    line or the N 100 neatline."""
    pytest.importorskip("pyproj")
    def km_pts(coords):
        e, n = ad.lonlat_to_mr([c[0] for c in coords], [c[1] for c in coords])
        return np.c_[e, n]
    def dist_to_polyline(p, poly):
        a, b = poly[:-1], poly[1:]; d = b - a
        tt = np.clip(np.einsum("ij,ij->i", p - a, d) / np.maximum(np.einsum("ij,ij->i", d, d), 1e-12), 0, 1)
        return float(np.min(np.hypot(*(a + tt[:, None] * d - p).T)))
    l49 = km_pts(FEATS["egypt_adl_gaza"]["geometry"]["coordinates"])
    pieces = FEATS["egypt_mv1950_demarcation"]["geometry"]["coordinates"]
    p1 = km_pts(pieces[0])
    samples = np.concatenate([np.linspace(a, b, 20, endpoint=False) for a, b in zip(p1[:-1], p1[1:])] + [p1[-1:]])
    ds = [dist_to_polyline(s, l49) for s in samples]
    assert max(ds) < 0.2 and float(np.median(ds)) < 0.06, (max(ds), np.median(ds))
    for piece in pieces:
        cs = [tuple(v) for v in km_pts(piece)]
        for (e0, n0), (e1, n1) in zip(cs, cs[1:]):
            on_grid = lambda v: abs(v - round(v)) < 0.015
            assert not ((on_grid(e0) and on_grid(e1) and round(e0) == round(e1)) or (on_grid(n0) and on_grid(n1) and round(n0) == round(n1))), \
                f"a stretch along a grid line: {(e0, n0)} -> {(e1, n1)}"
    assert min(FEATS["egypt_mv1950_demarcation"]["properties"]["pieces_on_ink"]) > 0.45


def test_jordan_sectors_are_cut_at_the_texts_own_points():
    """Review I-1: line A ends at Kh Deir Arab (MR 1510-1574, Art. V.1(a)); sector (d) starts at MR 1925-0958."""
    pytest.importorskip("pyproj")
    def ends(fid):
        c = FEATS[fid]["geometry"]["coordinates"]
        return [tuple(float(v) for v in ad.lonlat_to_mr(*c[0])), tuple(float(v) for v in ad.lonlat_to_mr(*c[-1]))]
    cuts = FEATS["jordan_adl_iraqi_c_south"]["properties"]["sector_cuts_km"]
    kda, ds = ad.JORDAN_KH_DEIR_ARAB, ad.JORDAN_DEAD_SEA_POINT
    near = lambda p, q, tol: math.hypot(p[0] - q[0], p[1] - q[1]) <= tol
    assert near(ends("jordan_adl_iraqi_c_south")[1], kda, cuts["kh_deir_arab_km"] + 0.05)
    assert near(ends("jordan_adl_a_latrun")[0], kda, cuts["kh_deir_arab_km"] + 0.05)
    assert near(ends("jordan_adl_bc_south")[1], ds, cuts["dead_sea_point_km"] + 0.05)
    assert near(ends("jordan_adl_d_arava")[0], ds, cuts["dead_sea_point_km"] + 0.05)
    assert cuts["kh_deir_arab_km"] < 1.5 and cuts["dead_sea_point_km"] < 2.5


def test_stage_dates_are_worded_as_deadlines_and_quote_the_article():
    for k in ("a_triangle", "b_wadi_ara", "c_east", "c_south"):
        p = FEATS[f"jordan_adl_iraqi_{k}"]["properties"]
        assert ", by " in p["name"] and "Zububa" not in p["name"], p["name"]
    assert "thence to the east of Kafr Qasim" in ad.JORDAN_STAGES and "deadlines" in ad.JORDAN_STAGES


def test_no_stale_statements_about_the_1950_line():
    """Review I-3."""
    egypt = next(f for f in SOURCE["statecraft"]["fronts"] if f["key"] == "egypt")
    assert "is later and not drawn" not in egypt["gap"]
    assert "Art. V (the signed map decides)" not in ad.MV_TEXT["cite"] and "used only as a named cross-check" not in ad.S1471["cite"]
    urls = [s["url"] for s in SOURCE["statecraft"]["sources"]]
    assert len(urls) == len(set(urls)), "each source listed once"
