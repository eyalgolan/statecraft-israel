# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Read-only scan of every ADDED line in every commit reachable from the given ref (merge commits included, diffed
against each parent), plus every commit message and author/committer email, for secret shapes, personal emails and
home paths (the patterns in oss_policy.py). Prints commit, file and pattern type, NEVER the matched text; OPEN rows
that are not secret-shaped print as one count per pattern type.

Findings are information only (owner ruling 2026-10-11: no history rewriting, no history enforcement) and do not change
the exit code. A command failure is non-zero because the scan is incomplete.
Rows already handled are listed in docs/oss/history-accepted.tsv (commit, path, kind, action, date) and are shown as
"accepted", so a rotated key or a reviewed false positive never blocks anything twice. A real secret is fixed by
revoking or rotating it at its provider; history is never rewritten.

Usage: uv run scripts/oss/scan_history.py [--repo DIR] [--ref main | --all]   (default --all)"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import oss_policy as P  # noqa: E402

ACCEPTED = "docs/oss/history-accepted.tsv"
SECRET_KINDS = frozenset(P.SECRET_PATTERNS)


def _kinds(line: str) -> list[str]:
    return P.secret_kinds(line) + P.email_kinds(line) + P.home_path_kinds(line)


def scan(repo: Path, refs: list[str]) -> list[tuple[str, str, str]]:
    hits: set[tuple[str, str, str]] = set()
    # -m: merge commits are diffed against each parent, so content added while resolving a conflict is read too.
    # --no-ext-diff/--no-textconv: a repo's own diff drivers must not run while we read it.
    cmd = ["git", "-C", str(repo), "log", *refs, "-m", "-p", "--no-color", "--no-ext-diff", "--no-textconv",
           "--format=COMMIT %H"]
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE)
    commit = path = ""
    assert proc.stdout is not None
    for raw in proc.stdout:
        line = raw.decode("utf-8", "replace").rstrip("\n")
        if line.startswith("COMMIT "):
            commit, path = line[7:17], ""
        elif line.startswith("+++ "):
            path = line[6:] if line.startswith("+++ b/") else ""
        elif line.startswith("+") and path:
            hits.update((commit, path, k) for k in _kinds(line[1:]))
    code = proc.wait()
    if code != 0:
        raise subprocess.CalledProcessError(code, cmd)
    meta = subprocess.run(["git", "-C", str(repo), "log", *refs, "--format=%H%x00%ae%x00%ce%x00%B%x1e"],
                          capture_output=True, check=True).stdout.decode("utf-8", "replace")
    for rec in meta.split("\x1e"):
        parts = rec.strip("\n").split("\x00", 3)
        if len(parts) < 4:
            continue
        sha, ae, ce, body = parts
        for who in (ae, ce):
            hits.update((sha[:10], "(identity)", k) for k in P.email_kinds(who))
        for line in body.splitlines():
            hits.update((sha[:10], "(message)", k) for k in _kinds(line))
    return sorted(hits)


def accepted(repo: Path) -> set[tuple[str, str, str]]:
    p = repo / ACCEPTED
    rows = set()
    if p.is_file():
        for line in p.read_text(encoding="utf-8").splitlines():
            if line and not line.startswith("#"):
                commit, path, kind = line.split("\t")[:3]
                rows.add((commit[:10], path, kind))
    return rows


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--repo", type=Path, default=Path.cwd())
    ap.add_argument("--ref", default=None)
    ap.add_argument("--all", action="store_true")
    args = ap.parse_args(argv)
    refs = [args.ref] if args.ref and not args.all else ["--all"]
    try:
        hits = scan(args.repo, refs)
    except subprocess.CalledProcessError as e:
        print(f"HISTORY SCAN FAILED: git log exited {e.returncode}; the history scan is incomplete", file=sys.stderr)
        return 1
    done = accepted(args.repo)
    open_secrets = 0
    # OPEN rows that are not secret-shaped (personal emails, home paths) print as one count per kind: on main they are
    # over a thousand commit identities, and listed one by one they buried the summary line (#2655 item 7).
    other_open: dict[str, int] = {}
    for commit, path, kind in hits:
        state = "accepted" if (commit, path, kind) in done else "OPEN"
        open_secrets += state == "OPEN" and kind in SECRET_KINDS
        if state == "OPEN" and kind not in SECRET_KINDS:
            other_open[kind] = other_open.get(kind, 0) + 1
            continue
        print(f"{commit}  {path}  {kind}  {state}")
    for kind, n in sorted(other_open.items()):
        print(f"{kind}  {n} OPEN rows (not secret-shaped, not listed)")
    count = len(subprocess.run(["git", "-C", str(args.repo), "rev-list", *refs], capture_output=True, text=True,
                               check=True).stdout.split())
    print(f"scanned {count} commits ({' '.join(refs)}): {len(hits) or 'no'} findings, "
          f"{open_secrets} open secret-shape findings")
    print("NOTE: information only. An OPEN secret-shape row must be revoked or rotated at its provider (owner step), "
          f"then recorded in {ACCEPTED}; history is never rewritten.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
