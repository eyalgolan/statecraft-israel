"""Real subprocess lifetime tests; every lock is a tmp_path throwaway."""
import os
import signal
import subprocess
import sys
import time
from pathlib import Path

import pytest

pytest.importorskip('fcntl')
import machine_lock as ml


def wait_for(predicate, seconds=6):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        if predicate():
            return
        time.sleep(.02)
    assert predicate(), 'process/receipt did not reach expected state'


def live(pid):
    p = subprocess.run(['ps', '-p', str(pid), '-o', 'stat='], capture_output=True, text=True)
    return p.returncode == 0 and bool(p.stdout.strip()) and not p.stdout.strip().startswith('Z')


def cleanup(procs, pids):
    # Test-owned receipts only; old-main red must not leave orphan sleepers.
    for pid in pids:
        if live(pid):
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
    for p in procs:
        if p.poll() is None:
            p.kill()
        p.wait(timeout=8)


@pytest.mark.parametrize('sig', [signal.SIGTERM, signal.SIGHUP, signal.SIGINT])
def test_holder_signal_retires_whole_group_before_shared_waiter(tmp_path, sig):
    record = tmp_path / 'holder'
    child_pid = tmp_path / 'child'
    grand_pid = tmp_path / 'grand'
    ready = tmp_path / 'ready'
    child = tmp_path / 'child.py'
    # Descendants ignore catchable signals: escalation must retire both.
    grand_code = ('import os,signal,time; from pathlib import Path; '
                  'signal.signal(signal.SIGTERM,signal.SIG_IGN); '
                  'signal.signal(signal.SIGHUP,signal.SIG_IGN); '
                  'signal.signal(signal.SIGINT,signal.SIG_IGN); '
                  f'Path({str(grand_pid)!r}).write_text(str(os.getpid())); time.sleep(30)')
    child.write_text('import os,signal,subprocess,sys,time\nfrom pathlib import Path\n'
                     'for sig in (signal.SIGTERM,signal.SIGHUP,signal.SIGINT): signal.signal(sig,signal.SIG_IGN)\n'
                     f'Path({str(child_pid)!r}).write_text(str(os.getpid()))\n'
                     f'p=subprocess.Popen([sys.executable,"-c",{grand_code!r}])\n'
                     f'while not Path({str(grand_pid)!r}).exists(): time.sleep(.01)\n'
                     f'Path({str(ready)!r}).touch()\n'
                     'p.wait()\n')
    env = {**os.environ, 'STATECRAFT_MACHINE_LOCK': str(tmp_path / 'machine.lock'), ml.HOLDER_ENV: str(record)}
    error_log = (tmp_path / 'holder-stderr.log').open('w')
    p = subprocess.Popen([sys.executable, ml.__file__, '--exclusive', '--', sys.executable, str(child)],
                         env=env, stdout=subprocess.DEVNULL, stderr=error_log)
    procs = [p]
    pids = []
    try:
        wait_for(lambda: ready.exists() and record.exists())
        pids = [int(child_pid.read_text()), int(grand_pid.read_text())]
        assert list(map(int, record.read_text().split())) == [p.pid, pids[0]]
        probe = ('import subprocess,sys; '
                 f'pids={pids!r}; '
                 'states=[subprocess.run(["ps","-p",str(p),"-o","stat="],capture_output=True,text=True).stdout.strip() for p in pids]; '
                 'print(repr(states),flush=True); '
                 'sys.exit(1 if any(s and not s.startswith("Z") for s in states) else 0)')
        wenv = {k: v for k, v in env.items() if k != ml.HOLDER_ENV}
        waiter = subprocess.Popen([sys.executable, ml.__file__, '--shared', '--', sys.executable, '-c', probe],
                                  env=wenv, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        procs.append(waiter)
        os.kill(p.pid, sig)
        stdout, stderr = waiter.communicate(timeout=10)
        assert waiter.returncode == 0, (stdout, stderr, 'shared acquired before workloads retired')
        assert p.wait(timeout=8) == 128 + sig, (tmp_path / 'holder-stderr.log').read_text()
        assert not any(live(pid) for pid in pids)
    finally:
        if not pids:
            pids = [int(f.read_text()) for f in (child_pid, grand_pid) if f.exists()]
        cleanup(procs, pids)
        error_log.close()


def test_holder_record_failure_cleans_child_and_grandchild(tmp_path):
    ready = tmp_path / 'ready'
    pids_file = tmp_path / 'pids'
    runner = tmp_path / 'runner.py'
    runner.write_text('import time\nfrom pathlib import Path\nimport machine_lock as ml\n'
                      'original=Path.write_text\n'
                      'def fail(self,*args,**kwargs):\n'
                      '    if self.name == "holder.tmp":\n'
                      f'        deadline=time.monotonic()+5\n        while not Path({str(ready)!r}).exists() and time.monotonic()<deadline: time.sleep(.01)\n'
                      '        raise OSError("test-only holder receipt failure")\n'
                      '    return original(self,*args,**kwargs)\n'
                      'Path.write_text=fail\nraise SystemExit(ml.main())\n')
    grand = 'import signal,time; signal.signal(signal.SIGTERM,signal.SIG_IGN); time.sleep(30)'
    child = ('import os,subprocess,sys,time; from pathlib import Path; '
             f'p=subprocess.Popen([sys.executable,"-c",{grand!r}]); '
             f'Path({str(pids_file)!r}).write_text(str(os.getpid())+" "+str(p.pid)); '
             f'Path({str(ready)!r}).touch(); p.wait()')
    env = {**os.environ, 'PYTHONPATH': str(Path(ml.__file__).parent),
           'STATECRAFT_MACHINE_LOCK': str(tmp_path / 'machine.lock'), ml.HOLDER_ENV: str(tmp_path / 'holder')}
    p = subprocess.Popen([sys.executable, str(runner), '--exclusive', '--', sys.executable, '-c', child],
                         env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    pids = []
    try:
        wait_for(lambda: pids_file.exists())
        pids = list(map(int, pids_file.read_text().split()))
        assert p.wait(timeout=10) != 0
        assert not any(live(pid) for pid in pids), 'post-spawn exception leaked descendants'
    finally:
        cleanup([p], pids)


@pytest.mark.parametrize('code', [0, 7])
def test_group_ownership_preserves_normal_exit_and_holder_pair(tmp_path, code):
    record = tmp_path / 'holder'
    evidence = tmp_path / 'identity'
    env = {**os.environ, 'STATECRAFT_MACHINE_LOCK': str(tmp_path / 'machine.lock'), ml.HOLDER_ENV: str(record)}
    command = ('import os; from pathlib import Path; '
               f'Path({str(evidence)!r}).write_text(f"{{os.getpid()}} {{os.getppid()}} {{os.getpgid(0)}}"); '
               f'raise SystemExit({code})')
    p = subprocess.run([sys.executable, ml.__file__, '--shared', '--', sys.executable, '-c', command],
                       env=env, capture_output=True, text=True, timeout=10)
    assert p.returncode == code, (p.stdout, p.stderr)
    child, parent, group = map(int, evidence.read_text().split())
    assert record.read_text().split() == [str(parent), str(child)]
    assert group == child, 'workload needs its own process group'


def test_dead_orphan_group_probe_does_not_replace_the_signal_exit(monkeypatch):
    class Child:
        pid = 4242
        def wait(self):
            return 0
        def poll(self):
            return 0
    sent = []
    def killpg(pid, sig):
        assert pid == 4242
        if sig == 0:
            raise PermissionError("Darwin orphan-zombie probe")
        sent.append(sig)
    monkeypatch.setattr(ml.os, "killpg", killpg)
    monkeypatch.setattr(ml.subprocess, "check_output", lambda *a, **k: "4242 Z\\n")
    ml.stop_command_group(Child(), grace=0)
    assert sent == [signal.SIGTERM, signal.SIGKILL]


def test_real_cleanup_permission_errors_are_not_suppressed(monkeypatch):
    class Child:
        pid = 4242
    def denied(pid, sig):
        raise PermissionError("actual signal denied")
    monkeypatch.setattr(ml.os, "killpg", denied)
    monkeypatch.setattr(ml.subprocess, "check_output", lambda *a, **k: "4242 S\n")  # a live member refused it
    with pytest.raises(PermissionError, match="actual signal denied"):
        ml.stop_command_group(Child(), grace=0)


def test_zombie_only_group_eperm_on_cleanup_signal_counts_as_retired(monkeypatch):
    class Child:
        pid = 4242
        waited = False
        def wait(self):
            self.waited = True
            return -15
    def denied(pid, sig):
        raise PermissionError("Darwin: group holds only our unreaped zombie")
    monkeypatch.setattr(ml.os, "killpg", denied)
    monkeypatch.setattr(ml.subprocess, "check_output", lambda *a, **k: "4242 Z\n")
    child = Child()
    ml.stop_command_group(child, grace=0)
    assert child.waited


@pytest.mark.parametrize('sig', [signal.SIGTERM, signal.SIGHUP, signal.SIGINT])
def test_holder_signal_with_default_action_child_exits_128_plus_sig(tmp_path, sig):
    # A plain child dies from the forwarded signal before cleanup runs; on Darwin its unreaped zombie made the
    # cleanup killpg raise EPERM, so the holder crashed with exit 1 (review of the C210 draft, 12 of 12 runs).
    ready = tmp_path / 'ready'
    env = {**os.environ, 'STATECRAFT_MACHINE_LOCK': str(tmp_path / 'machine.lock'), ml.HOLDER_ENV: str(ready)}
    p = subprocess.Popen([sys.executable, ml.__file__, '--exclusive', '--', 'sleep', '30'],
                         env=env, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    child = None
    try:
        wait_for(lambda: ready.exists())
        child = int(ready.read_text().split()[1])
        os.kill(p.pid, sig)
        _, stderr = p.communicate(timeout=8)
        assert p.returncode == 128 + sig, stderr
        assert not live(child)
    finally:
        cleanup([p], [child] if child else [])
