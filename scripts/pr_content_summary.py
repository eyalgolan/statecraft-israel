#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Markdown summary of the game records a branch adds, removes or re-dates (#325):
`uv run scripts/pr_content_summary.py [BASE]` (BASE defaults to origin/main; the branch is HEAD).

Compares committed content only, against the merge base (BASE...HEAD), like the changelog check. Covers
content/events/*.yaml (by id) and the dated record lists in content/model.yaml (indicators, policies, effects,
situations, voter_groups, actors). A record is "re-dated" when its `from` or `to` changed. Prints nothing but "No content records changed." when there is no difference."""
import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))  # tools/yaml12.py
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
# The dated model records: the sim's ModelView (sim/Statecraft.Sim/Content.cs; a test keeps them equal).
LISTS = ["indicators", "policies", "effects", "situations", "voter_groups", "actors"]


def git(root: Path, *args: str) -> str:
    r = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True, encoding="utf-8")
    return r.stdout if r.returncode == 0 else ""


def records(root: Path, ref: str) -> dict[str, dict]:
    """{ "event:id" or "policy:id": {"from": .., "to": ..} } as committed at ref."""
    out: dict[str, dict] = {}
    for path in git(root, "ls-tree", "-r", "--name-only", ref, "content/events/").split():
        if path.endswith(".yaml"):
            e = yaml12.load(git(root, "show", f"{ref}:{path}")) or {}
            if "id" in e:
                out[f"event:{e['id']}"] = {"from": str(e.get("from", "")), "to": str(e.get("to", ""))}
    model = yaml12.load(git(root, "show", f"{ref}:content/model.yaml")) or {}
    for key in LISTS:
        for r in model.get(key) or []:
            if isinstance(r, dict) and "id" in r:
                out[f"{key.rstrip('s') if key != 'policies' else 'policy'}:{r['id']}"] = {
                    "from": str(r.get("from", "")), "to": str(r.get("to", ""))}
    return out


def summarize(old: dict[str, dict], new: dict[str, dict]) -> str:
    added = sorted(set(new) - set(old))
    removed = sorted(set(old) - set(new))
    redated = sorted(k for k in set(old) & set(new) if old[k] != new[k])
    if not (added or removed or redated):
        return "No content records changed."

    def win(r: dict) -> str:
        return f" ({r['from'] or '-'} to {r['to'] or 'open'})" if r["from"] or r["to"] else ""

    lines = []
    for title, keys, src in (("Added", added, new), ("Removed", removed, old)):
        if keys:
            lines += [f"**{title}** ({len(keys)})"] + [f"- `{k}`{win(src[k])}" for k in keys] + [""]
    if redated:
        lines += [f"**Re-dated** ({len(redated)})"] + [
            f"- `{k}`: {old[k]['from'] or '-'} to {old[k]['to'] or 'open'} -> {new[k]['from'] or '-'} to {new[k]['to'] or 'open'}"
            for k in redated]
    return "\n".join(lines).rstrip()


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    p.add_argument("base", nargs="?", default="origin/main")
    p.add_argument("--root", type=Path, default=ROOT)
    a = p.parse_args(argv)
    if not git(a.root, "rev-parse", "--verify", "--quiet", f"{a.base}^{{commit}}").strip():
        print(f"base ref not found: {a.base} (try: git fetch origin)", file=sys.stderr)
        return 2
    # Against the merge base, as `git diff BASE...HEAD` and the changelog check do: what main gained since the branch
    # started is not this branch's change.
    fork = git(a.root, "merge-base", a.base, "HEAD").strip()
    if not fork:
        print(f"no merge base between {a.base} and HEAD", file=sys.stderr)
        return 2
    print(summarize(records(a.root, fork), records(a.root, "HEAD")))
    return 0


if __name__ == "__main__":
    sys.exit(main())
