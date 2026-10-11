#!/usr/bin/env python3
"""List event files that look incomplete for source-backed content work.

This is a lightweight helper for the skill bundle. It is intentionally small and
project-specific: it looks for event YAML files with no usable `sources:` or a
`status:` still at `draft`.
"""

from __future__ import annotations

import sys
from pathlib import Path

try:
    import yaml
except Exception as exc:  # pragma: no cover
    print(f"PyYAML is required to run this script: {exc}")
    raise SystemExit(1)

ROOT = Path(__file__).resolve().parents[4]
EVENTS = ROOT / "content" / "events"


def main() -> int:
    if not EVENTS.exists():
        print(f"Events directory not found: {EVENTS}")
        return 1

    problems: list[str] = []
    for path in sorted(EVENTS.glob("*.yaml")):
        with path.open("r", encoding="utf-8") as fh:
            try:
                data = yaml.safe_load(fh) or {}
            except Exception as exc:  # pragma: no cover
                problems.append(f"{path.name}: YAML parse error ({exc})")
                continue

        if not isinstance(data, dict):
            problems.append(f"{path.name}: not a mapping")
            continue

        missing = []
        sources = data.get("sources")
        if not isinstance(sources, list) or not sources:
            missing.append("sources")
        else:
            for src in sources:
                url = str(src.get("url", "")) if isinstance(src, dict) else ""
                if not url.startswith("https://") or not src.get("checked"):
                    missing.append("a source with an https url and a checked date")
                    break
        if data.get("status", "draft") == "draft":
            missing.append("a status above draft")
        if missing:
            problems.append(f"{path.name}: needs {', '.join(missing)}")

    if problems:
        for item in problems:
            print(item)
        return 1

    print(f"All {len(list(EVENTS.glob('*.yaml')))} event files have sources and a reviewed status.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
