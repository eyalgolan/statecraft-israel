# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Bakes content/map/route_paths_1948.yaml (written by tools/routes_1948.py) into content/map/route_paths_1948.tsv, the
compact runtime form the player reads at boot (RoutePaths.ParseRuntime in Unity).

The boot budget is the reason (issue #321, docs/visual-v1.md): parsing the YAML's 1,500 points with YamlDotNet cost about
125 ms of the map build at cpu 4, as the localities' YAML did before tools/localities_bake.py (#291). The YAML still
ships and stays the file the tools and the route ribbons' bake read; the runtime file carries the YAML's check
(RouteRibbonFile.Check: FNV-1a over its UTF-8 bytes) and this layout's version, and the player reads the YAML instead,
with a warning, when either does not match. Rows, tab-separated:

  meta   version     <VERSION>
  meta   yaml_check  <signed int32>
  meta   routes      <number of route rows>
  route  id kind status from to length_km on_symbol max_gap_m point_count points
  ...
  service  route_id from start end    (one per route with a `service` in routes_1948.yaml, issue #668)
  ...
  meta   body_check  <signed int32>   (the last row)

A service row is a railway's train service: the date regular service resumed (`from`) and the stretch's two ends as
"lon lat" (start, end), read from routes_1948.yaml (the sourced, hand-written file the map never reads), so the train's
date and stretch come from the content. A service changes neither route_paths_1948.yaml nor the ribbon bake.

`to` is empty when the route has no end; `points` is "lon lat" pairs joined by ';', each number as Python's shortest
round-trip form of the YAML's value. `body_check` is FNV-1a over the UTF-8 bytes of every route and service row, each with its
'\n', in order. The reader requires it as the last row and recomputes it, so a file cut anywhere (even inside a
number, review of #547) or edited by hand is refused; the counts give the clearer message for a lost row or point. Lines starting with '#' are comments. No field may hold a tab or a line break.

Usage: uv run tools/route_paths_bake.py [--check]
--check exits 1 when the committed runtime file differs from a fresh bake (pytest runs the same comparison:
tools/tests/test_route_paths_bake.py).
"""
import argparse
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))  # under python -I the script's folder is not on sys.path
import yaml12  # noqa: E402
import review_record  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "content" / "map" / "route_paths_1948.yaml"
ROUTES = ROOT / "content" / "map" / "routes_1948.yaml"   # the services (issue #668)
RUNTIME = ROOT / "content" / "map" / "route_paths_1948.tsv"
VERSION = 3   # RoutePaths.RuntimeVersion (2: the body_check row, review of #547; 3: the service rows, #668)
COLUMNS = ["route", "id", "kind", "status", "from", "to", "length_km", "on_symbol", "max_gap_m", "point_count", "points"]


def yaml_check(data: bytes) -> int:
    """RouteRibbonFile.Check: FNV-1a over the bytes, as a signed int32."""
    h = 2166136261
    for b in data:
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def body_check(rows: list[str]) -> int:
    """RoutePaths.BodyCheck: FNV-1a over the route and service rows' UTF-8 bytes, each followed by '\n', as a signed int32."""
    return yaml_check("".join(r + "\n" for r in rows).encode("utf-8"))


def _number(value, where: str) -> str:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{SOURCE.name}: {where}: {value!r} is not a number")
    return repr(float(value)) if isinstance(value, float) else str(value)


def _field(value, where: str) -> str:
    text = "" if value is None else str(value)
    if any(c in text for c in "\t\r\n;"):
        raise ValueError(f"{SOURCE.name}: {where}: a field holds a tab, a ';' or a line break: {text!r}")
    return text


def running_span(service: dict) -> tuple[dict, dict] | None:
    """The (start, end) stations of the service's stretches that run: every stretch but a gated one a person has not
    signed off. They must be one unbroken run along the line; None when none runs."""
    runs = [not x.get("gated", False) or review_record.signed(x) for x in service["stretches"]]
    if not any(runs):
        return None
    first, last = runs.index(True), len(runs) - 1 - runs[::-1].index(True)
    if not all(runs[first:last + 1]):
        raise ValueError("a gated stretch that does not run lies between two that do: the train's span would jump it")
    return service["stretches"][first]["start"], service["stretches"][last]["end"]


def bake(data: bytes, routes: bytes) -> str:
    """The runtime file for the YAML file's bytes and routes_1948.yaml's (the services): deterministic, so a fresh bake
    equals the committed file."""
    doc = yaml12.load(data.decode("utf-8"))
    sourced = yaml12.load(routes.decode("utf-8"))["routes"]
    out = [
        "# Generated by tools/route_paths_bake.py from route_paths_1948.yaml and the services of routes_1948.yaml.",
        "# Do not edit: edit the YAMLs (tools/routes_1948.py writes the first)",
        "# and rerun the tool. The runtime form the map reads at boot (map-only, outside the content hash).",
        "#" + "\t".join(COLUMNS),
        f"meta\tversion\t{VERSION}",
        f"meta\tyaml_check\t{yaml_check(data)}",
        f"meta\troutes\t{len(doc['routes'])}",
    ]
    rows = []
    for r in doc["routes"]:
        where = f"route '{r.get('id')}'"
        points = ";".join(f"{_number(p[0], where)} {_number(p[1], where)}" for p in r["points"])
        row = ["route", r["id"], r["kind"], r["status"], r["from"], r.get("to") or "", _number(r["length_km"], where),
               _number(r["on_symbol"], where), _number(r["max_gap_m"], where), str(len(r["points"]))]
        rows.append("\t".join(_field(v, where) for v in row) + "\t" + points)
    traced = {r["id"] for r in doc["routes"]}
    for r in sourced:
        s = r.get("service")
        if s is None:
            continue
        where = f"route '{r.get('id')}' service"
        if r["id"] not in traced:
            raise ValueError(f"{ROUTES.name}: {where}: the route is not in {SOURCE.name}")
        span = running_span(s)
        if span is None:
            continue   # every stretch is gated and none is signed off: no train
        ends = [f"{_number(w['lon'], where)} {_number(w['lat'], where)}" for w in span]
        rows.append("\t".join(_field(v, where) for v in ["service", r["id"], s["from"], *ends]))
    out += rows
    out.append(f"meta\tbody_check\t{body_check(rows)}")
    return "\n".join(out) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--check", action="store_true", help="fail when the committed runtime file is stale")
    args = ap.parse_args()
    fresh = bake(SOURCE.read_bytes(), ROUTES.read_bytes())
    if args.check:
        current = RUNTIME.read_text(encoding="utf-8") if RUNTIME.exists() else ""
        if current != fresh:
            print(f"{RUNTIME.relative_to(ROOT)} is stale: run uv run tools/route_paths_bake.py", file=sys.stderr)
            return 1
        print(f"{RUNTIME.relative_to(ROOT)} is up to date")
        return 0
    RUNTIME.write_text(fresh, encoding="utf-8", newline="\n")
    print(f"wrote {RUNTIME.relative_to(ROOT)} ({len(fresh)} characters)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
