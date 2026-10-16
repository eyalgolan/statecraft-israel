# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""launchd's entry point for the post-merge Unity run (T72). Installed as a copy in the job folder, so it depends on no
checkout's working tree. It takes the job's run lock, fetches origin/main in the main checkout, moves the job's
worktree to that tip (creating it on the first run) and runs tools/unity_postmerge.py from the worktree, so a merged
change to the script takes effect on the next run. The script, told by UNITY_POSTMERGE_LOCKED that the lock is held,
does the rest. When a run is in progress the launcher exits 0 and touches nothing.

Usage: uv run unity_postmerge_launch.py --uv PATH --repo MAIN_CHECKOUT --home JOB_FOLDER"""
import argparse
import fcntl
import os
import subprocess
import sys
from pathlib import Path


def git(cwd: Path, *args: str) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True).stdout.strip()


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--uv", required=True)
    ap.add_argument("--repo", type=Path, required=True)
    ap.add_argument("--home", type=Path, required=True)
    a = ap.parse_args(argv)
    a.home.mkdir(parents=True, exist_ok=True)
    wt = a.home / "worktree"
    with open(a.home / "run.lock", "a+") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            print("unity_postmerge_launch: a run is in progress")
            return 0
        try:
            git(a.repo, "fetch", "--quiet", "origin", "+refs/heads/main:refs/remotes/origin/main")
            tip = git(a.repo, "rev-parse", "--verify", "refs/remotes/origin/main^{commit}")
            if not (wt / ".git").exists():
                git(a.repo, "worktree", "prune")
                git(a.repo, "worktree", "add", "--detach", "--force", str(wt), tip)
            else:
                git(wt, "checkout", "--quiet", "--force", "--detach", tip)
        except subprocess.CalledProcessError as e:
            print(f"unity_postmerge_launch: git {e.cmd[1]} failed: {e.stderr.strip()[-500:]}", file=sys.stderr)
            return 1
        return subprocess.call([a.uv, "run", str(wt / "tools" / "unity_postmerge.py"), "--repo", str(a.repo),
                                "--home", str(a.home)], env={**os.environ, "UNITY_POSTMERGE_LOCKED": "1"})


if __name__ == "__main__":
    sys.exit(main())
