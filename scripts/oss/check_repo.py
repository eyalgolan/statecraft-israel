# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Policy checks over the TRACKED files at the tip of the tree, the tree a clone receives. Prints one line per finding
(check, path, line, kind) and never the matched text, so a found secret is not copied into a CI log.

Known, reviewed exceptions live in scripts/oss/allow.yaml ({check, glob, reason}); a row that suppresses nothing is
itself reported (stale-allow), so the list cannot silently outlive its reason.

Usage: uv run scripts/oss/check_repo.py [--root DIR] [--only a,b,...] [--release]
--release also refuses open owner decisions (docs/oss/DECISIONS.md) and ASSETS.yaml rows still marked owner-verify
or with licence unknown; CI runs without it, the release gate with it. Exit 0 clean, 1 findings, 2 usage error."""
from __future__ import annotations

import argparse
import fnmatch
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

sys.path.insert(0, str(Path(__file__).resolve().parent))
import oss_policy as P  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))  # tools/yaml12.py
import yaml12  # noqa: E402

ALLOW_FILE = "scripts/oss/allow.yaml"
ASSET_FILES = ("ASSETS.yaml", "ASSETS.portraits.yaml")


@dataclass(frozen=True)
class Finding:
    check: str
    path: str
    line: int  # 0 when the finding is about the whole file
    kind: str

    def __str__(self) -> str:
        where = f"{self.path}:{self.line}" if self.line else self.path
        return f"{self.check}: {where}: {self.kind}"


def tracked_files(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return sorted(p for p in out.decode("utf-8").split("\0") if p)


def git_binary_files(root: Path) -> set[str]:
    """Files git's own attribute logic treats as binary in the index (`i/-text`)."""
    out = subprocess.run(["git", "-C", str(root), "ls-files", "--eol", "-z"], capture_output=True, check=True).stdout
    found = set()
    for rec in out.decode("utf-8").split("\0"):
        if rec.startswith("i/-text"):
            found.add(rec.split("\t", 1)[1])
    return found


def _text(root: Path, rel: str) -> str | None:
    p = root / rel
    if p.is_symlink() or not p.is_file():
        return None
    data = p.read_bytes()
    if data[:2] in (b"\xff\xfe", b"\xfe\xff"):
        return data.decode("utf-16", errors="replace")  # UTF-16 text has NULs; the BOM says it is text
    if b"\0" in data[:8192]:
        return None
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError:
        return data.decode("latin-1")  # never skip a text file whole


def _line_check(name: str, fn: Callable[[str], list[str]]):
    def check(root: Path, files: list[str], release: bool) -> list[Finding]:
        found = []
        for rel in files:
            text = _text(root, rel)
            if text is None:
                continue
            for n, line in enumerate(text.splitlines(), 1):
                found += [Finding(name, rel, n, k) for k in fn(line)]
        return found
    return check


def check_home_paths(root: Path, files: list[str], release: bool) -> list[Finding]:
    """Home paths in text files, by line, and in binary files too (a model exported on someone's machine keeps the
    absolute paths of its textures, issue #659), where temp and agent-session paths count too (PR #680) and UTF-16
    forms are matched as well (`oss_policy.binary_home_path_kinds`)."""
    found = _line_check("home-paths", P.home_path_kinds)(root, files, release)
    for rel in files:
        p = root / rel
        if p.is_symlink() or not p.is_file() or _text(root, rel) is not None:
            continue
        found += [Finding("home-paths", rel, 0, k) for k in P.binary_home_path_kinds(p.read_bytes())]
    return found


def check_sizes(root: Path, files: list[str], release: bool) -> list[Finding]:
    return [Finding("sizes", rel, 0, "over-size-limit") for rel in files
            if (root / rel).is_file() and not (root / rel).is_symlink()
            and (root / rel).stat().st_size > P.MAX_FILE_BYTES]


def check_symlinks(root: Path, files: list[str], release: bool) -> list[Finding]:
    found = []
    for rel in files:
        p = root / rel
        if not p.is_symlink():
            continue
        target = os.readlink(p)
        inside = (p.parent / target).resolve().is_relative_to(root.resolve())
        if os.path.isabs(target) or not inside:
            found.append(Finding("symlinks", rel, 0, "symlink-outside-repo"))
    return found


def check_required(root: Path, files: list[str], release: bool) -> list[Finding]:
    tracked = set(files)
    return [Finding("required", rel, 0, "missing") for rel in P.REQUIRED_FILES if rel not in tracked]


def check_decisions(root: Path, files: list[str], release: bool) -> list[Finding]:
    rel = "docs/oss/DECISIONS.md"
    if not release or rel not in files:
        return []
    text = _text(root, rel) or ""
    return [Finding("decisions", rel, n, "open-decision")
            for n, line in enumerate(text.splitlines(), 1) if line.lstrip().startswith("- [ ]")]


def load_asset_rules(root: Path) -> tuple[list[dict], list[dict]]:
    rules: list[dict] = []
    removed: list[dict] = []
    for name in ASSET_FILES:
        p = root / name
        if p.is_file():
            doc = yaml12.load(p.read_text(encoding="utf-8")) or {}
            rules += [dict(r, _file=name) for r in doc.get("rules") or []]
            removed += doc.get("removed") or []
    return rules, removed


def _needs_provenance(rel: str, binary: set[str]) -> bool:
    return (rel in binary or Path(rel).suffix.lower() in P.BINARY_EXTENSIONS
            or any(fnmatch.fnmatch(rel, g) for g in P.PROVENANCE_GLOBS))


def check_assets(root: Path, files: list[str], release: bool) -> list[Finding]:
    """Note: fnmatch's '*' also matches '/', so "promo/ref/*-src.jpg" covers subfolders too. That is intended."""
    rules, removed = load_asset_rules(root)
    found: list[Finding] = []
    good: list[dict] = []
    for r in rules:
        missing = [k for k in P.RULE_KEYS if not r.get(k)]
        if missing:
            found.append(Finding("assets", r["_file"], 0, f"bad-rule missing {','.join(missing)}"))
            continue
        good.append(r)
        if r["licence"] not in P.ALLOWED_LICENCES:
            found.append(Finding("assets", r["_file"], 0, f"unknown-licence {r['licence']}"))
        if r["status"] not in P.STATUSES:
            found.append(Finding("assets", r["_file"], 0, f"bad-status {r['status']}"))
        elif release and (r["status"] == "owner-verify" or r["licence"] == "unknown"):
            found.append(Finding("assets", r["_file"], 0, f"owner-verify {r['glob']}"))
        if not r.get("allow_empty") and not any(fnmatch.fnmatch(f, r["glob"]) for f in files):
            found.append(Finding("assets", r["_file"], 0, f"stale-rule {r['glob']}"))
    binary = git_binary_files(root)
    for rel in files:
        if rel in ASSET_FILES or not _needs_provenance(rel, binary):
            continue
        if not any(fnmatch.fnmatch(rel, r["glob"]) for r in good):
            found.append(Finding("assets", rel, 0, "no-provenance"))
    tracked = set(files)
    found += [Finding("assets", d["path"], 0, "removed-but-tracked") for d in removed if d["path"] in tracked]
    return found


CHECKS: dict[str, Callable[[Path, list[str], bool], list[Finding]]] = {
    "secrets": _line_check("secrets", P.secret_kinds),
    "emails": _line_check("emails", P.email_kinds),
    "home-paths": check_home_paths,
    "conflict-markers": _line_check("conflict-markers", P.conflict_marker_kinds),
    "sizes": check_sizes,
    "symlinks": check_symlinks,
    "required": check_required,
    "decisions": check_decisions,
    "assets": check_assets,
}


def apply_allowlist(root: Path, findings: list[Finding], checks: list[str]) -> list[Finding]:
    p = root / ALLOW_FILE
    rows: list[dict] = []
    if p.is_file():
        rows = (yaml12.load(p.read_text(encoding="utf-8")) or {}).get("allow") or []
    kept, used = [], set()
    for f in findings:
        hit = next((i for i, r in enumerate(rows)
                    if r.get("check") == f.check and fnmatch.fnmatch(f.path, r.get("glob", ""))), None)
        if hit is None:
            kept.append(f)
        else:
            used.add(hit)
    for i, r in enumerate(rows):
        if not r.get("reason"):
            kept.append(Finding("allow", ALLOW_FILE, 0, f"allow-row-without-reason {r.get('glob')}"))
        elif r.get("check") in checks and i not in used:
            kept.append(Finding("allow", ALLOW_FILE, 0, f"stale-allow {r.get('check')} {r.get('glob')}"))
    return kept


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--root", type=Path, default=Path.cwd())
    ap.add_argument("--only", default="")
    ap.add_argument("--release", action="store_true")
    args = ap.parse_args(argv)
    names = [n for n in args.only.split(",") if n] or list(CHECKS)
    unknown = [n for n in names if n not in CHECKS]
    if unknown:
        print(f"unknown check(s): {', '.join(unknown)}; known: {', '.join(CHECKS)}")
        return 2
    root = args.root.resolve()
    try:
        files = tracked_files(root)
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("not a git checkout (or git refused it): run from a clone", file=sys.stderr)
        return 2
    findings = [f for n in names for f in CHECKS[n](root, files, args.release)]
    findings = apply_allowlist(root, findings, names)
    for f in findings:
        print(f)
    if findings:
        print(f"FAIL: {len(findings)} finding(s) in checks {', '.join(names)}")
        return 1
    print(f"OK: checks {', '.join(names)} over {len(files)} tracked files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
