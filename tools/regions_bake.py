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

A value is "s:" and the scalar's own text (backslash, tab, CR and line feed escaped as \\\\, \\t, \\r, \\n), "n:" for a
plain null (empty, ~, null, Null or NULL, as YamlDotNet reads one), or "l:" and a comma-separated list of tokens. The
bake works on the YAML's nodes (yaml.compose), not on PyYAML's resolved values, so every scalar keeps the text
YamlDotNet sees: YAML 1.1's re-typing (yes, Off, 1_0, 1:30, 0x0F, a datetime) never reaches the runtime file (review of
#601). The classes then read integers and booleans from that text as their YAML path does. A token holds no space,
comma, semicolon, tab or line break; the bake refuses one that does.

Usage: uv run tools/regions_bake.py [--check]
--check exits 1 when a committed runtime file differs from a fresh bake (pytest runs the same comparison:
tools/tests/test_regions_bake.py).
"""
import argparse
import struct
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "content" / "map"
FILES = ["subdistrict_hexes_1948", "armistice_hexes_1949", "control_hexes_1948_49"]
VERSION = 2   # RegionsRuntime.Version (2: every scalar as its own text, review of #601)


def fnv1a(data: bytes) -> int:
    """RouteRibbonFile.Check: FNV-1a over the bytes, as a signed int32."""
    h = 2166136261
    for b in data:
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def _escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace("\t", "\\t").replace("\r", "\\r").replace("\n", "\\n")


NULLS = {"", "~", "null", "Null", "NULL"}


def _scalar(node, where: str):
    if not isinstance(node, yaml.ScalarNode):
        raise ValueError(f"{where}: not a scalar")
    return None if node.style is None and node.value in NULLS else node.value


def _token(node, where: str) -> str:
    text = _scalar(node, where)
    if not text or any(c in text for c in " ,;\t\r\n"):
        raise ValueError(f"{where}: {text!r} is not a plain token")
    return text


def _value(node, where: str) -> str:
    if isinstance(node, yaml.SequenceNode):
        return "l:" + ",".join(_token(v, where) for v in node.value)
    text = _scalar(node, where)
    return "n:" if text is None else "s:" + _escape(text)


def bake(name: str, data: bytes) -> str:
    """The runtime file for a region YAML's bytes: deterministic, so a fresh bake equals the committed file."""
    root = yaml.compose(data.decode("utf-8"))
    if not isinstance(root, yaml.MappingNode):
        raise ValueError(f"{name}.yaml: not a mapping")
    rows = []
    for key_node, value in root.value:
        key = _token(key_node, f"{name}.yaml")
        where = f"{name}.yaml: {key}"
        if isinstance(value, yaml.SequenceNode) and not value.value:
            rows.append(f"empty\t{key}")
        elif isinstance(value, yaml.SequenceNode) and all(isinstance(v, yaml.MappingNode) for v in value.value):
            for i, item in enumerate(value.value):
                fields = []
                for k, v in item.value:
                    field = _token(k, f"{where}[{i}]")
                    if "=" in field:
                        raise ValueError(f"{where}[{i}]: field name {field!r} holds '='")
                    fields.append(f"{field}={_value(v, f'{where}[{i}].{field}')}")
                rows.append("\t".join(["item", key] + fields))
        elif isinstance(value, yaml.SequenceNode) and all(isinstance(v, yaml.SequenceNode) for v in value.value):
            body = ";".join(" ".join(_token(t, f"{where}[{i}]") for t in v.value) for i, v in enumerate(value.value))
            rows.append(f"tuples\t{key}\t{len(value.value)}\t{body}")
        elif isinstance(value, (yaml.ScalarNode, yaml.SequenceNode)):
            rows.append(f"scalar\t{key}\t{_value(value, where)}")
        else:
            raise ValueError(f"{where}: no runtime form for a {type(value).__name__}")
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
