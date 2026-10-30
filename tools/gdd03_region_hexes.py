# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Fill the GDD 0.3 schema-input member_hexes from its approved Mandate sub-districts.

Only empty ``member_hexes: []`` fields are replaced. All other bytes are retained.
The report includes unclaimed and multiply claimed map hexes, per-region counts, and
place rows whose source material cannot supply one unambiguous sub-district.

Usage: uv run tools/gdd03_region_hexes.py [--check]
"""
from __future__ import annotations

import argparse
import re
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable

import yaml12

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "research_notes" / "gdd03-regions-capitals-fields.yaml"
SUBDISTRICTS = ROOT / "content" / "map" / "subdistrict_hexes_1948.yaml"
TARGET = ROOT / "research_notes" / "gdd03-regions.schema-input.yaml"


def memberships(evidence: dict[str, Any], subdistricts: dict[str, Any]):
    number_to_key = {int(row["id"]): row["key"].casefold() for row in subdistricts["regions"]}
    owner: dict[str, str] = {}
    region_order: list[str] = []
    for region in evidence["regions"]:
        region_id = region["id"]
        region_order.append(region_id)
        for name in region["reference_subdistricts"]:
            key = name.casefold()
            if key in owner:
                raise ValueError(f"sub-district {name!r} is claimed by {owner[key]} and {region_id}")
            owner[key] = region_id

    by_region: dict[str, list[tuple[int, int]]] = {region_id: [] for region_id in region_order}
    claims: dict[tuple[int, int], list[str]] = defaultdict(list)
    for q, r, number in subdistricts["hexes"]:
        key = number_to_key[int(number)]
        if key in owner:
            region_id = owner[key]
            by_region[region_id].append((int(q), int(r)))
            claims[(int(q), int(r))].append(region_id)
    for values in by_region.values():
        values.sort()  # Runtime canonical order: q, then r (PlayerRegionGeography).

    all_hexes = {(int(q), int(r)) for q, r, _ in subdistricts["hexes"]}
    unassigned = sorted(all_hexes - set(claims))
    doubled = sorted((hex_, regions) for hex_, regions in claims.items() if len(regions) > 1)
    return by_region, unassigned, doubled


def _strings(value: Any) -> Iterable[str]:
    if isinstance(value, str):
        yield value
    elif isinstance(value, dict):
        for child in value.values():
            yield from _strings(child)
    elif isinstance(value, list):
        for child in value:
            yield from _strings(child)


def place_exceptions(evidence: dict[str, Any], subdistricts: dict[str, Any]):
    names = [row["key"] for row in subdistricts["regions"] if row["key"] != "outside"]
    patterns = {name: re.compile(rf"\b{re.escape(name)} sub-?district\b", re.IGNORECASE) for name in names}
    missing: list[tuple[str, str]] = []
    multiple: list[tuple[str, str, tuple[str, ...]]] = []
    for region in evidence["regions"]:
        for member in region["members"]:
            text = list(_strings(member.get("sources", []))) + list(_strings(member.get("source_records", [])))
            found = {name for name, pattern in patterns.items() if any(pattern.search(value) for value in text)}
            if member.get("mandate_subdistrict"):
                found.add(member["mandate_subdistrict"].casefold())
            if not found:
                missing.append((region["id"], member["id"]))
            elif len(found) > 1:
                multiple.append((region["id"], member["id"], tuple(sorted(found))))
    return missing, multiple


def placed_member_mismatches(evidence: dict[str, Any], by_region: dict[str, list[tuple[int, int]]]):
    hex_owner = {hex_: region for region, hexes in by_region.items() for hex_ in hexes}
    mismatches: list[tuple[str, str, int, int, str | None]] = []
    for region in evidence["regions"]:
        for member in region["members"]:
            q, r = member.get("hex", {}).get("q"), member.get("hex", {}).get("r")
            if isinstance(q, int) and isinstance(r, int) and hex_owner.get((q, r)) != member["region_id"]:
                mismatches.append((member["region_id"], member["id"], q, r, hex_owner.get((q, r))))
    return mismatches


def replace_member_hex_fields(text: str, by_region: dict[str, list[tuple[int, int]]]) -> str:
    lines = text.splitlines(keepends=True)
    current: str | None = None
    seen: set[str] = set()
    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        match = re.fullmatch(r"- id: ([a-z0-9_]+)\r?\n?", line)
        if match:
            current = match.group(1)
        if re.fullmatch(r"  member_hexes:(?: \[\])?\r?\n?", line):
            if current not in by_region:
                raise ValueError(f"member_hexes has no approved region at {current!r}")
            newline = "\r\n" if line.endswith("\r\n") else "\n"
            out.append("  member_hexes:" + newline)
            out.extend(f"  - {{ q: {q}, r: {r} }}{newline}" for q, r in by_region[current])
            seen.add(current)
            i += 1
            while i < len(lines) and re.fullmatch(r"  - \{ q: -?\d+, r: -?\d+ \}\r?\n?", lines[i]):
                i += 1
        else:
            out.append(line)
            i += 1
    missing = set(by_region) - seen
    if missing:
        raise ValueError("no member_hexes field for: " + ", ".join(sorted(missing)))
    return "".join(out)


def report(by_region, unassigned, doubled, missing, multiple, mismatches) -> str:
    lines = [*(f"{region}: {len(hexes)}" for region, hexes in by_region.items()),
             f"unassigned: {len(unassigned)}", f"double: {len(doubled)}",
             f"places_without_subdistrict: {len(missing)}", f"places_with_multiple_subdistricts: {len(multiple)}"]
    for region, place, names in multiple:
        lines.append(f"NEEDS-HUMAN-CHECK multiple: {region}/{place}: {', '.join(names)}")
    for region, place, q, r, derived in mismatches:
        lines.append(f"NEEDS-HUMAN-CHECK hex mismatch: {region}/{place} at {q},{r}: {derived or 'no region'}")
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args(argv)
    evidence = yaml12.load(EVIDENCE.read_text(encoding="utf-8"))
    subdistricts = yaml12.load(SUBDISTRICTS.read_text(encoding="utf-8"))
    by_region, unassigned, doubled = memberships(evidence, subdistricts)
    missing, multiple = place_exceptions(evidence, subdistricts)
    mismatches = placed_member_mismatches(evidence, by_region)
    old = TARGET.read_text(encoding="utf-8")
    new = replace_member_hex_fields(old, by_region)
    if new != old:
        if args.check:
            print(f"{TARGET.relative_to(ROOT)} needs regeneration", file=sys.stderr)
            return 1
        TARGET.write_text(new, encoding="utf-8")
    print(report(by_region, unassigned, doubled, missing, multiple, mismatches))
    return 1 if doubled else 0


if __name__ == "__main__":
    raise SystemExit(main())
