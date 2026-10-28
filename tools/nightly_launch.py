# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""launchd's entry point for the nightly release (tools/nightly.py). Installed as a copy in the job folder, next to a
copy of nightly_state.py, so it depends on no checkout's working tree. It takes the job's run lock, moves the
dedicated clone's own `main` to `origin/main` (resetting and cleaning only that clone, keeping its Unity Library) and
runs tools/nightly.py from the clone, so a merged change to the script takes effect on the next night. The clone is
created by hand at install time (tools/README.md, "Nightly release"): a missing clone is a recorded REFUSED night,
never a clone made here. When a run is in progress the launcher exits 0 and touches nothing.

The lock is held only while the clone is prepared: nightly.py takes the same lock itself and would find it taken.

Usage: uv run nightly_launch.py --uv PATH --home JOB_FOLDER"""
from __future__ import annotations

import argparse
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Callable

from nightly_state import TZ, Config, Refused, load_config, log_line, notify, record, run_lock

# What `git clean -x` keeps in the clone: Unity's import cache and the Unity-MCP plugin's NuGet download. The same
# list as unity_postmerge.KEEP (a test keeps them equal; this file runs without the checkout, so it cannot import it).
KEEP = ("/unity/Library/", "/unity/Assets/Plugins/NuGet/", "/unity/Assets/Plugins/NuGet.meta")
FETCH = ("fetch", "--quiet", "--tags", "origin", "+refs/heads/main:refs/remotes/origin/main")


def _git(cwd: Path, *args: str) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True).stdout.strip()


def _copy_library(cwd: Path, src: Path, dest: Path) -> None:
    subprocess.run(["cp", "-c", "-R", str(src), str(dest)], cwd=cwd, check=True, capture_output=True, text=True)


def prepare_clone(cfg: Config, git: Callable[..., str] = _git,
                  copy: Callable[[Path, Path, Path], None] = _copy_library) -> None:
    """Leaves the clone on `main`, at origin/main, with no change the last night left behind. `reset --hard` restores
    every tracked file Unity touched (ProjectSettings.asset, Assets/Settings, the prickly_pear material) and `clean`
    removes what it added; only the KEEP paths survive. Raises Refused("clone", ...) when it cannot."""
    clone = cfg.clone
    if not (clone / ".git").exists():
        raise Refused("clone", f"{clone} is not a git clone (create it as the install steps say)")
    try:
        git(clone, *FETCH)
        git(clone, "checkout", "--quiet", "main")
        git(clone, "reset", "--hard", "refs/remotes/origin/main")
        git(clone, "clean", "-ffdx", *[x for k in KEEP for x in ("-e", k)])
        lib, src = clone / "unity" / "Library", cfg.main_repo / "unity" / "Library"
        if not lib.exists() and src.is_dir():   # the first night only: the main checkout's warm import cache
            copy(cfg.main_repo, src, lib)
    except subprocess.CalledProcessError as e:
        tail = (e.stderr or "").strip()[-300:]
        raise Refused("clone", f"{' '.join(str(a) for a in e.cmd[:3])} failed: {tail}") from e


def _refuse(cfg: Config, home: Path, e: Refused, now: datetime) -> None:
    line = log_line(now, "REFUSED", tag=None, sha=None, gate=e.gate, log=None)
    record(home / "LOG.md", cfg.handoff, f"{line} {e.detail}")
    notify("Nightly refused", str(e)[:200])
    print(e, file=sys.stderr)


def main(argv: list[str] | None = None, *, git: Callable[..., str] = _git,
         copy: Callable[[Path, Path, Path], None] = _copy_library,
         call: Callable[[list[str]], int] = subprocess.call, now: datetime | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--uv", required=True)
    ap.add_argument("--home", type=Path, required=True)
    a = ap.parse_args(argv)
    now = now or datetime.now(TZ)
    a.home.mkdir(parents=True, exist_ok=True)
    try:
        cfg = load_config(a.home / "config.json")
    except Refused as e:   # a corrupt config is a recorded REFUSED night, not a night on defaults
        _refuse(Config(), a.home, e, now)
        return 1
    with run_lock(a.home) as held:
        if not held:
            print("nightly_launch: a run is in progress")
            return 0
        try:
            prepare_clone(cfg, git, copy)
        except Refused as e:
            _refuse(cfg, a.home, e, now)
            return 1
    return call([a.uv, "run", str(cfg.clone / "tools" / "nightly.py"), "run"])


if __name__ == "__main__":
    sys.exit(main())
