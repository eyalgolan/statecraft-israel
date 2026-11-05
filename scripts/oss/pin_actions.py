# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Pin every `uses: owner/repo@ref` in .github/workflows/*.yml and *.yaml to the ref's full commit SHA, keeping the
ref as a trailing comment. Anything already after the ref on the line, such as a note of why that action is used,
is kept after the ref. Resolves with a read-only `gh api` GET. A tag can be moved by its owner; a SHA cannot
(no_human 5f99b7af3: unpinned third-party downloads in the release build).

Usage: uv run scripts/oss/pin_actions.py"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path
from typing import Callable

import yaml

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))  # tools/yaml12.py
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
USES = re.compile(
    r"""^(?P<lead>\s*-?\s*(?P<kq>["']?)uses(?P=kq)\s*:\s*)(?P<quote>["']?)(?P<action>[\w.-]+/[\w./-]+)"""
    r"""@(?P<ref>[^\s#'"]+)(?P=quote)(?P<rest>.*)$"""
)


def gh_resolve(action: str, ref: str) -> str:
    repo = "/".join(action.split("/")[:2])
    out = subprocess.run(["gh", "api", f"repos/{repo}/commits/{ref}", "--jq", ".sha"], capture_output=True,
                         text=True, check=True).stdout.strip()
    if not re.fullmatch(r"[0-9a-f]{40}", out):
        raise SystemExit(f"could not resolve {action}@{ref}: {out!r}")
    return out


def workflow_paths(root: Path) -> list[Path]:
    folder = root / ".github/workflows"
    return sorted((*folder.glob("*.yml"), *folder.glob("*.yaml")))


def _uses_by_line(text: str) -> dict[int, str]:
    """Return parsed scalar `uses` values keyed by their zero-based source line."""
    found: dict[int, str] = {}
    seen: set[int] = set()

    def walk(node: yaml.nodes.Node) -> None:
        # An alias can make the composed node graph share nodes (or even contain a cycle).
        if id(node) in seen:
            return
        seen.add(id(node))
        if isinstance(node, yaml.nodes.MappingNode):
            for key, value in node.value:
                if (isinstance(key, yaml.nodes.ScalarNode) and key.value == "uses"
                        and isinstance(value, yaml.nodes.ScalarNode)
                        and value.tag == "tag:yaml.org,2002:str"):
                    found[value.start_mark.line] = value.value
                walk(value)
        elif isinstance(node, yaml.nodes.SequenceNode):
            for value in node.value:
                walk(value)

    document = yaml.compose(text, Loader=yaml12.Core12Loader)   # YAML 1.2 tags, as tools/yaml12.py (#601)
    if document is not None:
        walk(document)
    return found


def pin_text(text: str, resolve: Callable[[str, str], str]) -> str:
    parsed_uses = _uses_by_line(text)
    lines = []
    for line_number, line in enumerate(text.splitlines(keepends=True)):
        m = USES.match(line.rstrip("\n"))
        value = f"{m['action']}@{m['ref']}" if m else None
        if (m and parsed_uses.get(line_number) == value
                and not re.fullmatch(r"[0-9a-f]{40}", m["ref"])):
            sha = resolve(m["action"], m["ref"])
            # Whatever already followed the ref is kept after it, so a note on the line survives the pin (#1463).
            kept = m["rest"].strip()
            quote = m["quote"]
            line = (f"{m['lead']}{quote}{m['action']}@{sha}{quote} # {m['ref']}"
                    f"{' ' + kept if kept else ''}\n")
        lines.append(line)
    return "".join(lines)


def main() -> int:
    for wf in workflow_paths(ROOT):
        before = wf.read_text(encoding="utf-8")
        after = pin_text(before, gh_resolve)
        if after != before:
            wf.write_text(after, encoding="utf-8")
            print(f"pinned {wf.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
