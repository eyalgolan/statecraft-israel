# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Bakes the map's region files into compact runtime forms the player reads at boot (issue #321):

  content/map/subdistrict_hexes_1948.yaml  -> subdistrict_hexes_1948.tsv   (Subdistricts1948)
  content/map/armistice_hexes_1949.yaml    -> armistice_hexes_1949.tsv     (Armistice1949)
  content/map/control_hexes_1948_49.yaml   -> control_hexes_1948_49.tsv    (Control1948)

The boot budget is the reason (docs/visual-v1.md, "Issue #321"): YamlDotNet over the three files cost about 135 ms at
cpu 4 between the map and the start screen, and the map's own read of the sub-districts 24 ms more. The YAML files stay
the source of truth (tools/borders_bake.py, tools/armistice_bake.py and tools/control_bake.py write them) and still ship; each runtime file
carries its YAML's FNV-1a check (RouteRibbonFile.Check) and this layout's version, and the player reads the YAML instead,
with a warning, when either does not match, when the file is cut short or when a row is malformed (RegionsRuntime.Read).

The layout is the YAML's own shape, generically, so each class keeps its checks (it builds the same rows from either
source). One row per line, tab-separated:

  meta    version     <VERSION>
  meta    yaml_check  <signed int32>
  meta    rows        <number of data rows>
  scalar  <key>       <value>                       a top-level scalar, or a list of plain scalars
  item    <table>     <field>=<value> ...           one mapping of a top-level list of mappings
  tuples  <table>     <count>  <a b c;a b c;...>    a top-level list of lists (each element a plain token)
  empty   <key>                                     an empty top-level list
  meta    body_check  <signed int32>                the last row: FNV-1a over the data rows' UTF-8, each with its '\\n'

A value is typed: "s:" and the text (backslash, tab, CR and line feed escaped as \\\\, \\t, \\r, \\n), "n:" null,
"b:true" / "b:false", "i:" and an integer, or "l:" and a comma-separated list of plain tokens. A date is written as
its yyyy-MM-dd text, as YamlDotNet reads it into the classes' string fields. A plain token holds no space, comma,
semicolon, tab or line break; the bake refuses one that does.

Usage: uv run tools/regions_bake.py [--check]
--check exits 1 when a committed runtime file differs from a fresh bake (pytest runs the same comparison:
tools/tests/test_regions_bake.py).
"""
import argparse
import datetime
import struct
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "content" / "map"
FILES = ["subdistrict_hexes_1948", "armistice_hexes_1949", "control_hexes_1948_49"]
VERSION = 1   # RegionsRuntime.Version


def fnv1a(data: bytes) -> int:
    """RouteRibbonFile.Check: FNV-1a over the bytes, as a signed int32."""
    h = 2166136261
    for b in data:
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def _escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace("\t", "\\t").replace("\r", "\\r").replace("\n", "\\n")


def _token(value, where: str) -> str:
    if isinstance(value, bool) or not isinstance(value, (int, str, datetime.date)):
        raise ValueError(f"{where}: {value!r} is not a plain token")
    text = value.isoformat() if isinstance(value, datetime.date) else str(value)
    if text == "" or any(c in text for c in " ,;\t\r\n"):
        raise ValueError(f"{where}: {text!r} is not a plain token")
    return text


def _value(value, where: str) -> str:
    if value is None:
        return "n:"
    if isinstance(value, bool):
        return "b:true" if value else "b:false"
    if isinstance(value, int):
        return f"i:{value}"
    if isinstance(value, datetime.date):
        return "s:" + value.isoformat()
    if isinstance(value, str):
        return "s:" + _escape(value)
    if isinstance(value, list):
        return "l:" + ",".join(_token(v, where) for v in value)
    raise ValueError(f"{where}: {value!r} has no runtime form")


def bake(name: str, data: bytes) -> str:
    """The runtime file for a region YAML's bytes: deterministic, so a fresh bake equals the committed file."""
    doc = yaml.safe_load(data.decode("utf-8"))
    rows = []
    for key, value in doc.items():
        where = f"{name}.yaml: {key}"
        if isinstance(value, list) and not value:
            rows.append(f"empty\t{key}")
        elif isinstance(value, list) and all(isinstance(v, dict) for v in value):
            for i, item in enumerate(value):
                fields = [f"{k}={_value(v, f'{where}[{i}].{k}')}" for k, v in item.items()]
                if any("=" in k or "\t" in k for k in item):
                    raise ValueError(f"{where}[{i}]: a field name holds '=' or a tab")
                rows.append("\t".join(["item", key] + fields))
        elif isinstance(value, list) and all(isinstance(v, list) for v in value):
            body = ";".join(" ".join(_token(t, f"{where}[{i}]") for t in v) for i, v in enumerate(value))
            rows.append(f"tuples\t{key}\t{len(value)}\t{body}")
        else:
            rows.append(f"scalar\t{key}\t{_value(value, where)}")
    out = [
        f"# Generated by tools/regions_bake.py from {name}.yaml. Do not edit: edit the YAML (or rerun its own tool) and",
        "# rerun this one. The runtime form the map reads at boot (display only, outside the content hash).",
        f"meta\tversion\t{VERSION}",
        f"meta\tyaml_check\t{fnv1a(data)}",
        f"meta\trows\t{len(rows)}",
    ]
    out += rows
    out.append(f"meta\tbody_check\t{fnv1a(''.join(r + chr(10) for r in rows).encode('utf-8'))}")
    return "\n".join(out) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--check", action="store_true", help="fail when a committed runtime file is stale")
    args = ap.parse_args()
    stale = 0
    for name in FILES:
        fresh = bake(name, (MAP / f"{name}.yaml").read_bytes())
        runtime = MAP / f"{name}.tsv"
        if args.check:
            current = runtime.read_text(encoding="utf-8") if runtime.exists() else ""
            if current != fresh:
                print(f"{runtime.relative_to(ROOT)} is stale: run uv run tools/regions_bake.py", file=sys.stderr)
                stale = 1
            continue
        runtime.write_text(fresh, encoding="utf-8", newline="\n")
        print(f"wrote {runtime.relative_to(ROOT)} ({len(fresh)} characters)")
    if args.check and not stale:
        print("the region runtime files are up to date")
    return stale


if __name__ == "__main__":
    sys.exit(main())
