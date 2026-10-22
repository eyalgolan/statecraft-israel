import os
import subprocess
import sys
import time
from pathlib import Path

import pytest

# machine_lock is the Mac's flock-based lock; fcntl does not exist on Windows, so the module skips there.
fcntl = pytest.importorskip("fcntl", reason="fcntl is POSIX-only (machine_lock uses flock); not available on Windows")

import machine_lock as ml  # noqa: E402


def test_the_command_follows_the_double_dash_and_a_mode_is_required():
    a = ml.parse(["--exclusive", "--wait-unity", "--", "uv", "run", "tools/web_check.py", "--query", "vprofile"])
    assert a.exclusive and a.wait_unity and a.command == ["uv", "run", "tools/web_check.py", "--query", "vprofile"]
    assert ml.parse(["--shared", "--", "echo", "x"]).command == ["echo", "x"]
    with pytest.raises(SystemExit):
        ml.parse(["--", "echo"])
    with pytest.raises(SystemExit):
        ml.parse(["--shared"])


def test_waiting_ends_when_no_unity_process_is_left():
    seen = iter([[101, 102], [102], []])
    clock = iter(range(100))
    assert ml.wait_for_unity(600, pids=lambda: next(seen), sleep=lambda s: None, now=lambda: next(clock)) is True


def test_waiting_gives_up_at_the_timeout():
    clock = iter(range(0, 1000, 100))
    assert ml.wait_for_unity(250, pids=lambda: [7], sleep=lambda s: None, now=lambda: next(clock)) is False


def test_a_shared_holder_really_blocks_an_exclusive_lock(tmp_path):
    lock = tmp_path / "machine.lock"
    env = {**os.environ, "STATECRAFT_MACHINE_LOCK": str(lock)}
    child = subprocess.Popen([sys.executable, ml.__file__, "--shared", "--", sys.executable, "-c", "import time; time.sleep(5)"],
                             env=env, stdout=subprocess.PIPE, text=True)
    try:
        assert "taking the machine lock (shared)" in child.stdout.readline()
        blocked, deadline = False, time.time() + 4
        while not blocked and time.time() < deadline:
            with open(lock, "a+") as f:
                try:
                    fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
                    fcntl.flock(f, fcntl.LOCK_UN)
                    time.sleep(0.05)
                except BlockingIOError:
                    blocked = True
        assert blocked, "an exclusive lock was granted while the shared holder ran"
    finally:
        child.terminate()
        child.wait(10)
        child.stdout.close()


def test_an_editor_holding_a_worktree_lockfile_counts_as_unity(tmp_path):
    temp = tmp_path / "a" / "unity" / "Temp"
    temp.mkdir(parents=True)
    (temp / "UnityLockfile").write_text("")
    (tmp_path / "b").mkdir()
    asked = []
    pids = ml.lockfile_holders([tmp_path / "a", tmp_path / "b"], lsof=lambda files: (asked.extend(files), "4242\n4242\n")[1])
    assert pids == [4242]
    assert asked == [str(temp / "UnityLockfile")]
    assert ml.lockfile_holders([tmp_path / "b"], lsof=lambda files: "1\n") == []


UNITY_EXE = "/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"


def test_only_the_unity_executable_counts_never_a_wrapper_naming_it():
    """Review C1: a queued `machine_lock.py --shared -- .../Unity -batchmode` wrapper and its `uv run` parent name Unity in
    their arguments; selecting by argument text made an exclusive profile wait on them (a deadlock)."""
    comm = "\n".join([
        f"  101 {UNITY_EXE}",
        "  102 /opt/statecraft-test/uv/python/cpython-3.13/bin/python3.13",
        "  103 /opt/homebrew/bin/uv",
        f"  104 {UNITY_EXE}",
        "  105 /Applications/Unity Hub.app/Contents/MacOS/Unity Hub",
    ])
    args = "\n".join([
        f"  101 {UNITY_EXE} -batchmode -projectPath unity -runTests",
        f"  102 python3 tools/machine_lock.py --shared -- {UNITY_EXE} -batchmode -projectPath unity",
        f"  103 uv run tools/machine_lock.py --shared -- {UNITY_EXE} -batchmode -projectPath unity",
        f"  104 {UNITY_EXE} -projectPath unity",
        "  105 /Applications/Unity Hub.app/Contents/MacOS/Unity Hub -batchmode",
    ])
    assert ml.batch_unity_pids(comm, args) == [101]
    assert ml.batch_unity_pids(comm, args, exclude={101}) == []


def test_a_waiting_exclusive_request_runs_before_a_later_shared_one(tmp_path):
    # Issue #207: while a shared job runs and an exclusive request waits, a newer shared request must not get in first.
    env = {**os.environ, "STATECRAFT_MACHINE_LOCK": str(tmp_path / "machine.lock")}
    stamp = [sys.executable, "-c", "import time; print(time.time(), flush=True); time.sleep(0.3)"]

    def start(mode, command):
        return subprocess.Popen([sys.executable, ml.__file__, mode, "--", *command], env=env, stdout=subprocess.PIPE,
                                text=True)

    holder = start("--shared", [sys.executable, "-c", "import time; time.sleep(2)"])
    procs = [holder]
    try:
        assert "taking the machine lock (shared)" in holder.stdout.readline()
        exclusive = start("--exclusive", stamp)
        procs.append(exclusive)
        assert "taking the machine lock (exclusive)" in exclusive.stdout.readline()
        time.sleep(0.5)  # the exclusive request is now waiting behind the holder
        later = start("--shared", stamp)
        procs.append(later)
        assert "taking the machine lock (shared)" in later.stdout.readline()
        ran_exclusive, ran_later = float(exclusive.stdout.readline()), float(later.stdout.readline())
        assert ran_exclusive < ran_later, "a later shared request got the lock before a waiting exclusive one"
    finally:
        for p in procs:
            p.terminate()
            p.wait(10)
            p.stdout.close()


def test_the_holder_record_names_this_lock_and_the_command_it_started(tmp_path):
    """T72: unity_postmerge.py stops a hung Unity by the PID this record gives, never by name."""
    record = tmp_path / "holder"
    env = {**os.environ, "STATECRAFT_MACHINE_LOCK": str(tmp_path / "machine.lock"), ml.HOLDER_ENV: str(record)}
    p = subprocess.run([sys.executable, ml.__file__, "--shared", "--", sys.executable, "-c",
                        "import os; print(os.getpid(), os.getppid())"], env=env, capture_output=True, text=True)
    assert p.returncode == 0
    child, parent = p.stdout.split()[-2:]
    assert record.read_text().split() == [parent, child]


def test_no_holder_record_is_written_unless_asked(tmp_path):
    env = {k: v for k, v in os.environ.items() if k != ml.HOLDER_ENV}
    env["STATECRAFT_MACHINE_LOCK"] = str(tmp_path / "machine.lock")
    assert subprocess.run([sys.executable, ml.__file__, "--shared", "--", sys.executable, "-c", "pass"],
                          env=env).returncode == 0
    assert sorted(p.name for p in tmp_path.iterdir()) == ["machine.lock", "machine.lock.gate"]


@pytest.mark.parametrize("wait_flags,serial", [((True, True), True), ((False, False), False), ((True, False), False)])
def test_shared_waiters_serialize_commands_but_ordinary_shared_jobs_still_overlap(tmp_path, wait_flags, serial):
    """#1211: two idle checks may both succeed before either launched Unity becomes observable.

    Use the real wrapper, OS locks and children, replacing only Unity detection with an idle result. Queue both
    wrappers behind the existing gate, then release them together. Children record entry/exit and wait briefly for
    the peer's entry, widening the launch gap without depending on a particular elapsed execution time.
    """
    runner = tmp_path / "runner.py"
    runner.write_text("import machine_lock as ml\nml.wait_for_unity = lambda timeout: True\nraise SystemExit(ml.main())\n")
    command = tmp_path / "command.py"
    command.write_text("""import fcntl, sys, time
from pathlib import Path
root, who = Path(sys.argv[1]), sys.argv[2]
def record(event):
    with (root / 'events').open('a') as stream:
        fcntl.flock(stream, fcntl.LOCK_EX)
        stream.write(f'{event} {who}\\n')
        stream.flush()
record('start')
(root / f'started-{who}').touch()
peer = root / f'started-{1 - int(who)}'
deadline = time.monotonic() + 2
while not peer.exists() and time.monotonic() < deadline:
    time.sleep(0.01)
record('end')
""")
    lock = tmp_path / "machine.lock"
    env = {k: v for k, v in os.environ.items() if k != ml.HOLDER_ENV}
    env.update(STATECRAFT_MACHINE_LOCK=str(lock), PYTHONPATH=str(Path(ml.__file__).parent))
    procs = []
    with lock.with_name(lock.name + ".gate").open("a+") as gate:
        fcntl.flock(gate, fcntl.LOCK_EX)
        try:
            for who, wait in enumerate(wait_flags):
                flags = ["--wait-unity"] if wait else []
                process = subprocess.Popen([sys.executable, str(runner), "--shared", *flags, "--", sys.executable,
                                            str(command), str(tmp_path), str(who)], env=env, stdout=subprocess.PIPE,
                                           stderr=subprocess.PIPE, text=True)
                procs.append(process)
                assert "taking the machine lock (shared)" in process.stdout.readline()
            fcntl.flock(gate, fcntl.LOCK_UN)
            for process in procs:
                stdout, stderr = process.communicate(timeout=15)
                assert process.returncode == 0, (stdout, stderr)
        finally:
            for process in procs:
                if process.poll() is None:
                    process.kill()
                process.wait(10)
                process.stdout.close()
                process.stderr.close()
    events = [line.split() for line in (tmp_path / "events").read_text().splitlines()]
    assert len(events) == 4 and sorted(events) == sorted([[event, str(who)] for event in ["start", "end"] for who in [0, 1]])
    if serial:
        assert [event for event, who in events] == ["start", "end", "start", "end"], events
        assert events[0][1] == events[1][1] and events[2][1] == events[3][1], events
    else:
        assert [event for event, who in events[:2]] == ["start", "start"], events


def test_a_shared_wait_timeout_does_not_launch_and_releases_its_locks(tmp_path):
    runner = tmp_path / "runner.py"
    runner.write_text("import sys, machine_lock as ml\nstate = sys.argv.pop(1)\nml.wait_for_unity = lambda timeout: state == 'idle'\n"
                      "raise SystemExit(ml.main())\n")
    marker = tmp_path / "launched"
    env = {k: v for k, v in os.environ.items() if k != ml.HOLDER_ENV}
    env.update(STATECRAFT_MACHINE_LOCK=str(tmp_path / "machine.lock"), PYTHONPATH=str(Path(ml.__file__).parent))

    def run(state, command):
        return subprocess.run([sys.executable, str(runner), state, "--shared", "--wait-unity", "--", sys.executable,
                               "-c", command], env=env, capture_output=True, text=True, timeout=10)

    command = f"from pathlib import Path; Path({str(marker)!r}).touch()"
    busy = run("busy", command)
    assert busy.returncode == 3 and "timed out waiting for Unity" in busy.stdout
    assert not marker.exists(), "a command started after the Unity wait timed out"
    assert run("idle", "raise SystemExit(7)").returncode == 7
    assert run("idle", command).returncode == 0 and marker.exists()
