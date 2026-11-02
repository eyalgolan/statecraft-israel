# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pyyaml==6.0.3"]
# ///
"""Who held which areas, 1948-49 (content/map/control_1948_49.geojson, the true polygons: tools/control_digitise.py) ->
the per-hex, per-period layer the map draws (MapRegions; docs/visual-v1.md "Region borders").

Usage: uv run tools/control_bake.py   (reads the GeoJSON, content/map/hexes.json, content/map/subdistrict_hexes_1948.yaml,
content/map/settlements_1948.yaml and the heightmap; writes content/map/control_hexes_1948_49.yaml)

The GeoJSON keeps the true polygons; the game shows them snapped to hex edges, as the promo does (owner ruling
2026-10-11). For each period of the GeoJSON's `periods` (a snapshot and the overlays in force), every land hex of
Mandatory Palestine (the sub-district layer's land: its regions but 'outside') takes one code at its probe: the hex's
centre, or the settlement's own point where a mapped settlement sits in it (rule 0 below)
(deterministic: float64 arithmetic in a fixed order, features in file order; the even-odd ray cast of tools/borders_bake.py):
  0 a hex holding a mapped settlement (content/map/settlements_1948.yaml) is probed at the settlement's own point, not
    at the hex's centre, which can fall the other side of the traced line, and which says nothing about a shore town
    placed on a substitute hex (#1659; owner's ruling of 25 October 2026, option (a)). Rules 1 and 2 then read that
    point, and 4 to 6 still apply on top. Two settlements in one hex must agree about its code or the bake fails;
  1 an overlay containing the probe wins, the later in the period's list first: contested (c), demilitarized zone (z),
    no sourced line (-);
  2 else the snapshot's area containing it: Israel (I), or the other side by its holder (info panel only): Egypt (E),
    Jordan from its agreement (J), Syria (S), the Arab Liberation Army (V), several (M), not named (A);
  3 a shore hex whose centre is in water (tools/borders_bake.py rule 3) takes the code at the nearest land within one hex
    size; a centre outside every polygon takes the commonest code over the hex's area (37 sample points: a frontier hex
    such as Metulla's, whose centre the outline leaves out), and none there means no colour (-: the 60-km northing's
    south before 3 April 1949);
  4 Jerusalem (S/1025: an agreed line with a no-man's land between the two lines; from 1949 map 2, not available): a hex
    within 6 km of the city whose own area holds both sides, the smaller at least a fifth of 37 sample points, is
    no-man's land (m). A period whose `jerusalem` is no_mans_land (15 May - 10 June 1948: the city fought over, owner
    decision of 16 October 2026, #674) makes every hex within 6 km no-man's land (m), unless it has no colour (-);
  5 a contested place (the GeoJSON's contested_places: a place whose change of hands a source dates inside a period
    that no traced line shows, owner ruling 2 of 16 October 2026 on #686) makes its hex (placed as the game places
    settlements) contested (c) for every period inside its window, unless the hex has no colour (-) or is no-man's land (m);
  6 a pinned town (the GeoJSON's pinned_towns: a town the sources place on one side that the traced lines, within their
    accuracy, put on the other) gives its hex (placed as the game places settlements) the sourced holder for every period
    inside the pin's window, unless the hex is grey;
  7 a settlement exempt from an overlay (OVERLAY_EXEMPT: a place a source shows held before the game's start whose point
    the overlay's traced polygon takes in) is probed without that overlay, so its hex takes the snapshot's code there;
    the overlay still greys every other hex it covers. An exemption that exempts nothing (an unknown settlement or
    overlay, or a point outside it) fails the bake;
Each hex row is [q, r, "codes"]: one character per period, in the periods' order. The sim never reads the output (display
only: the content hash and the goldens do not move). Status draft, owner-verify (the GeoJSON's).
"""
import json
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import borders_bake as bb  # noqa: E402
import hexgrid  # noqa: E402
from geo import KM_PER_DEG_LAT, KM_PER_DEG_LON_EQ  # noqa: E402
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "content" / "map"
SOURCE = MAP / "control_1948_49.geojson"
SUBDISTRICTS = MAP / "subdistrict_hexes_1948.yaml"
SETTLEMENTS = MAP / "settlements_1948.yaml"
OUT = MAP / "control_hexes_1948_49.yaml"
HOLDER_CODE = {"israel": "I", "egypt": "E", "jordan": "J", "syria": "S", "ala": "V", "mixed": "M",
               "unspecified": "A"}
KIND_CODE = {"contested": "c", "dmz": "z", "not_sourced": "-", "no_mans_land": "m"}
JERUSALEM = (35.2256, 31.7789)
JERUSALEM_KM = 6.0
JERUSALEM_MIN_SHARE = 0.2
# Rule 7, keyed to the settlement (not its hex, so a regrid keeps it). Owner's rulings of 1 November 2026 on #1659,
# "Israeli from 15 May (Recommended)" (Safed) and "Yes, Rosh Pinna too (Recommended)": both were in Jewish hands before
# the game's start, so the CIA's 18 May tenuously-held loop (contested_opening_hula) greys the rest of the Hula area but
# not their hexes.
OVERLAY_EXEMPT = {
    "safed": {"overlay": "contested_opening_hula", "source": "https://en.wikipedia.org/w/index.php?oldid=1382135431",
              "why": "en.wiki Safed, revision 1382135431, read 2026-10-27, 1948 war section: 'The city was fully under the "
                     "control of Jewish paramilitary forces by May 11, 1948.' (research_notes/Map/"
                     "regions-1948-opening-control-p2a.md, gdd03-0016)"},
    "rosh_pinna": {"overlay": "contested_opening_hula", "source": "https://en.wikipedia.org/w/index.php?oldid=1369980534",
                   "why": "en.wiki Operation Yiftach, revision 1369980534, read 2026-10-27: 'The army camp at Rosh Pinna was "
                          "handed over to the Haganah/Palmach by its British commander on 28 April.' (research_notes/Map/"
                          "regions-1948-opening-control-p2a.md, gdd03-0017)"},
}
HEADER = ("# Generated by tools/control_bake.py from control_1948_49.geojson (the true polygons), hexes.json and the sub-district\n"
          "# layer. Do not edit by hand: rerun the tool. Display only (MapBorders): the sim never reads this file. Each land hex of\n"
          "# Mandatory Palestine takes, per period, the code at its centre, or at the settlement's own point where a mapped\n"
          "# settlement sits in it (#1659; tools/control_bake.py). Status draft, owner-verify.\n")


def mandate_hexes() -> dict:
    """{(q, r): sub-district id} for the land hexes in Mandatory Palestine (the sub-district layer's regions but 'outside')."""
    doc = yaml12.load(SUBDISTRICTS.read_text(encoding="utf-8"))
    outside = {r["id"] for r in doc["regions"] if r["key"] == bb.OUTSIDE}
    return {(q, r): rid for q, r, rid in doc["hexes"] if rid not in outside}


def load(source: dict) -> tuple[dict, dict, list]:
    """(snapshot id -> [(code, rings)], overlay id -> (code, rings), periods)."""
    snaps, overlays = {}, {}
    for f in source["features"]:
        p = f["properties"]
        if p["kind"] != "area":
            continue
        rings = bb.rings(f["geometry"])
        if p.get("snapshot"):
            snaps.setdefault(p["snapshot"], []).append((HOLDER_CODE[p["holder"]], rings))
        else:
            overlays[p["id"]] = (KIND_CODE[p["neutral_kind"]], rings)
    return snaps, overlays, source["statecraft"]["periods"]


def code_at(lon: float, lat: float, snap: list, overlays: list) -> str | None:
    for code, rings in reversed(overlays):
        if bb.contains(rings, lon, lat):
            return code
    for code, rings in snap:
        if bb.contains(rings, lon, lat):
            return code
    return None


def period_overlays(period: dict, overlays: dict, settlement: str | None = None) -> list:
    """The period's overlays in force at a probe, less the one the probed settlement is exempt from (rule 7)."""
    skip = OVERLAY_EXEMPT.get(settlement, {}).get("overlay")
    return [overlays[o] for o in period["overlays"] if o in overlays and o != skip]


def samples(lon: float, lat: float, km_lon: float, size_km: float) -> list:
    """37 points over a hex's area: the centre and rings of 6, 12 and 18 points out to 0.8 of its inradius."""
    pts = [(lon, lat)]
    inr = size_km * math.sqrt(3) / 2 * 0.8
    for k, n in ((1, 6), (2, 12), (3, 18)):
        rad = inr * k / 3
        for i in range(n):
            a = 2 * math.pi * i / n
            pts.append((lon + rad * math.cos(a) / km_lon, lat + rad * math.sin(a) / KM_PER_DEG_LAT))
    return pts


def inside(period: dict, window: dict) -> bool:
    """The whole period lies inside the window (from, until; an until of None is open-ended)."""
    return period["from"] >= window["from"] and (window["until"] is None or (period["until"] is not None and period["until"] <= window["until"]))


def check_exemptions(source: dict) -> None:
    """Fail closed (rule 7): every exemption names a mapped settlement whose point lies in the named overlay, and that
    overlay is in force in a drawn period; otherwise a renamed id or a moved point would silently grey the place again."""
    _, overlays, periods = load(source)
    by_id = {s["id"]: s for s in yaml12.load_path(SETTLEMENTS)["settlements"]}
    for sid, ex in sorted(OVERLAY_EXEMPT.items()):
        s, ov = by_id.get(sid), overlays.get(ex["overlay"])
        why = ("no such settlement" if s is None else "no such overlay" if ov is None
               else "the overlay is in no drawn period" if not any(p["snapshot"] and ex["overlay"] in p["overlays"] for p in periods)
               else "point outside the overlay" if not bb.contains(ov[1], s["lon"], s["lat"]) else None)
        if why:
            raise SystemExit(f"overlay exemption exempts nothing: {sid} from {ex['overlay']}: {why}")


def bake(source: dict, hexes: dict, water: np.ndarray, meta: dict) -> tuple[list, dict]:
    snaps, overlays, periods = load(source)
    pins = [(hexgrid.settlement_hex(p["lon"], p["lat"], hexes), p) for p in source["statecraft"].get("pinned_towns", [])]
    greys = [(hexgrid.settlement_hex(p["lon"], p["lat"], hexes), p) for p in source["statecraft"].get("contested_places", [])]
    land = mandate_hexes()
    # A mapped settlement's hex takes the code at the settlement's own point, not at the hex's centre (#1659; owner's
    # ruling of 25 October 2026, option (a)). These centres can sit the other side of the traced line, and a shore town
    # such as Tiberias is placed on a substitute hex whose own hex is lake, so its centre says nothing about the town.
    # The contested, pin and Jerusalem rules below still apply on top of the result.
    towns: dict = {}
    for s in yaml12.load_path(SETTLEMENTS)["settlements"]:
        towns.setdefault(hexgrid.settlement_hex(s["lon"], s["lat"], hexes), []).append(s)
    b = hexes["bounds"]
    km_lon = KM_PER_DEG_LON_EQ * math.cos(math.radians((b["lat0"] + b["lat1"]) / 2))
    size = hexes["hex_size_km"]
    drawn = [p for p in periods if p["snapshot"]]
    # Two settlements in one hex must not have one of them silently win: the hex has a single colour, so their points
    # have to agree about it. Fail closed instead of picking (review of #1659).
    for qr, ss in sorted(towns.items()):
        if len(ss) < 2:
            continue
        seen: dict = {}
        for s in ss:
            key = tuple(code_at(s["lon"], s["lat"], snaps[p["snapshot"]], period_overlays(p, overlays, s["id"]))
                        for p in drawn)
            seen.setdefault(key, []).append(s["id"])
        if len(seen) > 1:
            # A settlement outside every snapshot and overlay codes as None; sorting must not compare it with a code.
            order = sorted(seen.items(), key=lambda kv: tuple(c or "-" for c in kv[0]))
            parts = "; ".join(f"{', '.join(ids)}: {''.join(c or '-' for c in k)}" for k, ids in order)
            raise SystemExit(f"settlements in hex {qr} disagree about its control ({parts})")
    cells = {}
    applied = set()
    for h in hexes["hexes"]:
        qr = (h["q"], h["r"])
        if qr not in land:
            continue
        lon, lat = h["lon"], h["lat"]
        probe = (lon, lat)
        if bb.water_at(lon, lat, water, meta):
            near = bb.nearest_land(lon, lat, water, meta, km_lon, size)
            if near is not None:
                probe = near
        town = None
        if qr in towns:   # the settlement's own point (#1659), checked above to be the only answer for this hex
            town = towns[qr][0]["id"]
            probe = (towns[qr][0]["lon"], towns[qr][0]["lat"])
        jer = math.hypot((lon - JERUSALEM[0]) * km_lon, (lat - JERUSALEM[1]) * KM_PER_DEG_LAT) <= JERUSALEM_KM
        codes = []
        for p in drawn:
            ovs = period_overlays(p, overlays, town)
            c = code_at(*probe, snaps[p["snapshot"]], ovs)
            if c is None:   # a frontier hex whose centre the outline leaves out (Metulla): the most common code over its area
                got = [g for g in (code_at(x, y, snaps[p["snapshot"]], ovs) for x, y in samples(lon, lat, km_lon, size)) if g]
                c = max(sorted(set(got)), key=got.count) if got else "-"
            if jer and p.get("jerusalem") == "no_mans_land" and c != "-":
                c = "m"
            elif jer and c not in "cz-":
                got = [code_at(x, y, snaps[p["snapshot"]], []) for x, y in samples(lon, lat, km_lon, size)]
                n_i = sum(1 for g in got if g == "I"); n_a = sum(1 for g in got if g and g != "I")
                if min(n_i, n_a) >= JERUSALEM_MIN_SHARE * len(got):
                    c = "m"
            for gqr, g in greys:
                if gqr == qr and c not in "-m" and inside(p, g):
                    c = "c"
                    applied.add(g["place"])
            for pqr, pin in pins:
                if pqr == qr and c not in "czm" and inside(p, pin):
                    c = HOLDER_CODE[pin["holder"]]
            codes.append(c)
        cells[qr] = "".join(codes)
    missed = sorted({g["place"] for _, g in greys} - applied)
    if missed:   # fail closed: a contested place off the land, or with no period inside its window, would grey nothing
        raise SystemExit(f"contested_places greyed no hex: {', '.join(missed)}")
    return drawn, cells


def render(source: dict, periods: list, cells: dict) -> str:
    meta = source["statecraft"]
    q = lambda s: json.dumps(s, ensure_ascii=False)
    out = [HEADER,
           "layer: control_1948_49\n",
           f"title: {q(meta['title'])}\n",
           f"meaning: {q(meta['meaning'])}\n",
           f"line: {q('The true polygons are content/map/control_1948_49.geojson (status ' + meta['status'] + ', ' + meta['review'] + '); shown here to the nearest hex edge, up to half a hex (about 4 km) from the true line.')}\n",
           f"shown_from: {periods[0]['from']}   # the game's start (15 May 1948): the 9 July 1948 lines, adjusted (#674); nothing before it\n",
           "codes: \"I Israel; E Egypt, J Jordan, S Syria, V the Arab Liberation Army, M several, A not named "
           "(the other side, by holder); c contested; m no-man's land; z demilitarized zone; - no colour (no sourced line)\"\n",
           "periods:   # legend: held (1948 front lines), armistice (every front's agreement), mixed (some); line_date: the date the source line shows\n"]
    for p in periods:
        fronts = "[" + ", ".join(p.get("fronts", [])) + "]"
        out.append(f"  - {{ id: {p['id']}, from: {p['from']}, until: {p['until'] or 'null'}, legend: {p['legend']}, "
                   f"line_date: {q(p['line_date'])}, fronts: {fronts} }}\n")
    out.append("hexes:   # [q, r, codes: one per period], ordered by r then q\n")
    for (hq, hr), codes in sorted(cells.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        out.append(f"  - [{hq}, {hr}, \"{codes}\"]\n")
    return "".join(out)


def build() -> str:
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    check_exemptions(source)
    hexes = json.loads((MAP / "hexes.json").read_text())
    elev, meta = bb.load_elevation()
    periods, cells = bake(source, hexes, bb.water_mask(elev, meta), meta)
    return render(source, periods, cells)


def main() -> None:
    text = build()
    OUT.write_text(text, encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({text.count(chr(10) + '  - [')} hexes)")


if __name__ == "__main__":
    main()
