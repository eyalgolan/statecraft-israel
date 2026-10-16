# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""One Mac, several worktrees: frame-time runs must not share the machine with Unity imports, builds, test runs or an open
Unity editor.

Usage: uv run tools/machine_lock.py (--shared | --exclusive) [--wait-unity] [--timeout SECONDS] -- <command> [args...]
--shared takes the machine lock shared (Unity builds, captures and edit-mode runs); --exclusive takes it alone (frame-time
profiles and boot budgets) and, with --wait-unity, then waits until no Unity is left running: no batch-mode Unity process,
and no process (an interactive editor included) holding any worktree's unity/Temp/UnityLockfile. The lock file is
~/.cache/statecraft/machine.lock (fcntl.flock; STATECRAFT_MACHINE_LOCK overrides it), shared by every worktree of this
user. Exit code: the command's, or 3 when --wait-unity times out.

Unity processes are recognised by their executable (`ps -o comm=`), never by argument text, so queued wrappers that
name Unity in their command line do not count (review C1). macOS flock does not queue fairly, so every request first
passes a gate (the same path plus ".gate"): it takes the gate exclusively, then the machine lock, then releases the gate.
A waiting exclusive request holds the gate until it has the lock, so no new shared holder gets in ahead of it and the
running ones drain (issue #207; before, a stream of shared jobs starved a profile, review M9).

When STATECRAFT_LOCK_HOLDER names a file, it writes "<this pid> <command pid>" there once the command has started:
the holder record tools/unity_postmerge.py uses to stop a hung Unity by its PID (T72)."""
import argparse
import fcntl
import os
import subprocess
import sys
import time
from pathlib import Path

LOCK = Path(os.environ.get("STATECRAFT_MACHINE_LOCK", str(Path.home() / ".cache" / "statecraft" / "machine.lock")))
GATE = LOCK.with_name(LOCK.name + ".gate")
UNITY_EXE = "/Unity.app/Contents/MacOS/Unity"
HOLDER_ENV = "STATECRAFT_LOCK_HOLDER"


def parse(argv: list[str]) -> argparse.Namespace:
    ap = argparse.ArgumentParser()
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--shared", action="store_true")
    mode.add_argument("--exclusive", action="store_true")
    ap.add_argument("--wait-unity", action="store_true")
    ap.add_argument("--timeout", type=float, default=3600)
    ap.add_argument("command", nargs=argparse.REMAINDER)
    a = ap.parse_args(argv)
    if a.command and a.command[0] == "--":
        a.command = a.command[1:]
    if not a.command:
        ap.error("no command after --")
    return a


def _rows(ps_text: str) -> dict[int, str]:
    rows = {}
    for line in ps_text.splitlines():
        pid, _, rest = line.strip().partition(" ")
        if pid.isdigit():
            rows[int(pid)] = rest.strip()
    return rows


def batch_unity_pids(comm_text: str, args_text: str, exclude: set[int] | frozenset[int] = frozenset()) -> list[int]:
    """Pids whose executable (`ps -o comm=`, the binary's path) is Unity.app's Unity and whose arguments include
    -batchmode. Selected by executable, never by argument text: a queued `machine_lock.py --shared -- .../Unity
    -batchmode` wrapper and its `uv run` parent name Unity in their arguments and must not count (review C1)."""
    args = _rows(args_text)
    return sorted(pid for pid, exe in _rows(comm_text).items()
                  if exe.endswith(UNITY_EXE) and pid not in exclude and " -batchmode" in f" {args.get(pid, '')} ")


def unity_batch_pids() -> list[int]:
    def ps(field: str) -> str:
        return subprocess.run(["ps", "-axo", f"pid=,{field}="], capture_output=True, text=True).stdout
    return batch_unity_pids(ps("comm"), ps("args"), exclude={os.getpid()})


def worktrees() -> list[Path]:
    """Every worktree of this repository, so an editor open in any of them is seen."""
    r = subprocess.run(["git", "worktree", "list", "--porcelain"], capture_output=True, text=True)
    return [Path(line[len("worktree "):]) for line in r.stdout.splitlines() if line.startswith("worktree ")]


def lockfile_holders(roots: list[Path], lsof=None) -> list[int]:
    """Pids holding any root's unity/Temp/UnityLockfile: every Unity editor with that project open, interactive or batch."""
    files = [str(p / "unity" / "Temp" / "UnityLockfile") for p in roots if (p / "unity" / "Temp" / "UnityLockfile").exists()]
    if not files:
        return []
    run = lsof or (lambda fs: subprocess.run(["lsof", "-t", *fs], capture_output=True, text=True).stdout)
    return sorted({int(p) for p in run(files).split() if p.isdigit()})


def unity_pids() -> list[int]:
    return sorted(set(unity_batch_pids()) | set(lockfile_holders(worktrees())))


def wait_for_unity(timeout: float, pids=unity_pids, sleep=time.sleep, now=time.monotonic) -> bool:
    deadline = now() + timeout
    while True:
        busy = pids()
        if not busy:
            return True
        if now() >= deadline:
            return False
        print(f"machine_lock: waiting for Unity processes {busy} (batch runs, or an editor with a worktree open)", flush=True)
        sleep(10)


def main(argv: list[str] | None = None) -> int:
    a = parse(sys.argv[1:] if argv is None else argv)
    LOCK.parent.mkdir(parents=True, exist_ok=True)
    with open(GATE, "a+") as gate, open(LOCK, "a+") as f:
        print(f"machine_lock: taking the machine lock ({'exclusive' if a.exclusive else 'shared'})", flush=True)
        fcntl.flock(gate, fcntl.LOCK_EX)
        fcntl.flock(f, fcntl.LOCK_EX if a.exclusive else fcntl.LOCK_SH)
        fcntl.flock(gate, fcntl.LOCK_UN)
        if a.wait_unity and not wait_for_unity(a.timeout):
            print("machine_lock: timed out waiting for Unity")
            return 3
        child = subprocess.Popen(a.command)
        if record := os.environ.get(HOLDER_ENV):
            tmp = Path(record + ".tmp")
            tmp.write_text(f"{os.getpid()} {child.pid}\n")
            os.replace(tmp, record)
        try:
            return child.wait()
        except BaseException:
            child.kill()
            child.wait()
            raise


if __name__ == "__main__":
    sys.exit(main())
