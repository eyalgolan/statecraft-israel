# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pyyaml==6.0.3", "scipy==1.18.1"]
# ///
"""How far the traced 1948 routes (content/map/route_paths_1948.yaml) lie from independent references: the accuracy
table in docs/routes.md.

Usage: uv run tools/routes_accuracy.py [--fetch]
References, downloaded with --fetch into tools/.cache/routes-accuracy/ (never committed, never copied into content):
  OpenHistoricalMap (CC0): railway ways starting by 1948, and primary to tertiary roads valid on 14 May 1948;
  OpenStreetMap (ODbL): today's railways and the abandoned, disused and razed ones. ODbL is outside this repository's
      licence policy, so its geometry is only measured against here, as the route files never contain any of it.
For every route, every 50 m of its line: the distance to the nearest reference point (references densified every
20 m); prints the median, the 90th percentile and the share within 300 m. A reference with no line near a route (OSM
in the Gaza Strip, OHM for most 1948 roads) reads as a large distance: read the table with docs/routes.md's notes.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
import urllib.parse
import urllib.request
from pathlib import Path

import numpy as np
import yaml
from scipy.spatial import cKDTree

ROOT = Path(__file__).resolve().parents[1]
PATHS = ROOT / "content" / "map" / "route_paths_1948.yaml"
CACHE = ROOT / "tools" / ".cache" / "routes-accuracy"
BBOX = "29.45,34.2,33.35,35.9"   # south, west, north, east: the map's bounds (content/map/hexes.json)
QUERIES = {
    "ohm_rail.json": ("https://overpass-api.openhistoricalmap.org/api/interpreter",
                      f'[out:json][timeout:170];way["railway"]({BBOX});out tags geom;'),
    "ohm_roads.json": ("https://overpass-api.openhistoricalmap.org/api/interpreter",
                       f'[out:json][timeout:170];way["highway"]({BBOX});out tags geom;'),
    "osm_rail.json": ("https://overpass-api.de/api/interpreter https://z.overpass-api.de/api/interpreter https://lz4.overpass-api.de/api/interpreter",
                      f'[out:json][timeout:170];(way["railway"~"^(rail|abandoned|disused|razed|dismantled)$"]({BBOX});'
                      f'way["abandoned:railway"]({BBOX});way["disused:railway"]({BBOX}););out tags geom;'),
}
USER_AGENT = "statecraft-israel-routes/1.0 (+https://github.com/eyalgolan/statecraft-israel)"
LAT0 = 31.8   # the equirectangular frame's standard parallel (the map's middle)


def fetch() -> None:
    CACHE.mkdir(parents=True, exist_ok=True)
    for name, (urls, query) in QUERIES.items():
        for url in urls.split():   # the main Overpass server is often busy: try its mirrors in turn
            req = urllib.request.Request(url, data=urllib.parse.urlencode({"data": query}).encode(),
                                         headers={"User-Agent": USER_AGENT})
            try:
                with urllib.request.urlopen(req, timeout=200) as resp:
                    body = resp.read()
                json.loads(body)   # an Overpass error page is HTML or XML: try the next server
            except (OSError, ValueError) as e:
                print(f"{name}: {url} failed ({e})", file=sys.stderr)
                continue
            (CACHE / name).write_bytes(body)
            print(f"fetched {name} from {url} ({len(body)} bytes)")
            break
        else:
            raise SystemExit(f"{name}: every Overpass server failed")


def xy(lon: float, lat: float) -> np.ndarray:
    return np.array([lon * 111320 * math.cos(math.radians(LAT0)), lat * 110570])


def densify(points: list[tuple[float, float]], step: float) -> np.ndarray:
    out = []
    for a, b in zip(points, points[1:]):
        pa, pb = xy(*a), xy(*b)
        n = max(1, int(np.linalg.norm(pb - pa) / step))
        out += [pa + (pb - pa) * t / n for t in range(n)]
    out.append(xy(*points[-1]))
    return np.array(out)


def reference(files: list[str], keep) -> cKDTree:
    pts = []
    for name in files:
        for e in json.loads((CACHE / name).read_text())["elements"]:
            if e["type"] == "way" and keep(e.get("tags", {})):
                pts.append(densify([(p["lon"], p["lat"]) for p in e["geometry"]], 20))
    return cKDTree(np.concatenate(pts))


def valid_1948(t: dict) -> bool:
    return not (t.get("start_date", "") > "1948-05-14") and not (t.get("end_date", "9999") < "1948-05-14")


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--fetch", action="store_true", help="download the references into the cache first")
    args = ap.parse_args(argv)
    if args.fetch:
        fetch()
    missing = [n for n in QUERIES if not (CACHE / n).exists()]
    if missing:
        print(f"missing references {missing}: run with --fetch", file=sys.stderr)
        return 2
    refs = {
        "OHM rail by 1948": reference(["ohm_rail.json"], lambda t: t.get("start_date", "9")[:4] <= "1948"),
        "OSM rail, current and abandoned": reference(["osm_rail.json"], lambda t: True),
        "OHM roads valid in 1948": reference(["ohm_roads.json"], lambda t: t.get("highway") in (
            "primary", "secondary", "tertiary", "trunk") and valid_1948(t)),
    }
    print("| Route | Reference | Median distance | 90th percentile | Within 300 m |")
    print("|---|---|---|---|---|")
    for r in yaml.safe_load(PATHS.read_text(encoding="utf-8"))["routes"]:
        line = densify([tuple(p) for p in r["points"]], 50)
        names = ["OHM rail by 1948", "OSM rail, current and abandoned"] if r["kind"] == "rail" else ["OHM roads valid in 1948"]
        for name in names:
            d, _ = refs[name].query(line)
            print(f"| {r['id']} | {name} | {np.median(d):.0f} m | {np.percentile(d, 90):.0f} m | {np.mean(d < 300):.0%} |")
    return 0


if __name__ == "__main__":
    sys.exit(main())
