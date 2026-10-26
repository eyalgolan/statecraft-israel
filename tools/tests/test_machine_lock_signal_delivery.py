"""Catchable signals must reach a new session without an inherited blocked mask."""
import os
import signal
import subprocess
import sys
import time
from pathlib import Path

import pytest

pytest.importorskip('fcntl')
import machine_lock as ml


@pytest.mark.parametrize('sig', [signal.SIGTERM, signal.SIGHUP, signal.SIGINT])
def test_forwarded_signal_reaches_cooperative_child(tmp_path, sig):
    ready, heard = tmp_path / 'ready', tmp_path / 'heard'
    child_file = tmp_path / 'child'
    script = tmp_path / 'cooperative.py'
    script.write_text('import os,signal,time\nfrom pathlib import Path\n'
                      'def finish(sig,frame):\n'
                      f'    Path({str(heard)!r}).write_text(str(sig))\n'
                      '    raise SystemExit(0)\n'
                      'for sig in (signal.SIGTERM,signal.SIGHUP,signal.SIGINT): signal.signal(sig,finish)\n'
                      f'Path({str(child_file)!r}).write_text(str(os.getpid()))\n'
                      f'Path({str(ready)!r}).touch()\n'
                      'time.sleep(30)\n')
    env = {**os.environ, 'STATECRAFT_MACHINE_LOCK': str(tmp_path / 'lock')}
    env.pop(ml.HOLDER_ENV, None)
    p = subprocess.Popen([sys.executable, ml.__file__, '--exclusive', '--', sys.executable, str(script)],
                         env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        deadline = time.monotonic() + 6
        while not ready.exists() and time.monotonic() < deadline:
            time.sleep(.02)
        assert ready.exists()
        os.kill(p.pid, sig)
        assert p.wait(timeout=6) == 128 + sig
        assert heard.exists(), 'catchable signals must not be inherited as blocked'
        # Cleanup TERM may follow the requested signal; at least a catchable signal must reach the child.
        assert int(heard.read_text()) in (sig, signal.SIGTERM)
    finally:
        if p.poll() is None:
            p.kill()
        p.wait(timeout=6)
        if child_file.exists():
            try:
                os.kill(int(child_file.read_text()), signal.SIGKILL)
            except ProcessLookupError:
                pass
