"""scripts/check.sh keeps a failing step's log: a flaky test's message was lost when the temp log was deleted."""
import os
import shutil
import stat
import subprocess
from pathlib import Path

import pytest
from conftest import commit_all

ROOT = Path(__file__).resolve().parents[3]

pytestmark = pytest.mark.skipif(shutil.which("bash") is None, reason="check.sh needs bash")


def _run_check(make_repo, tmp_path, uv_exit: int):
    # A copy of check.sh in a throwaway repo, with stand-in uv and dotnet on PATH so no real step runs.
    repo = make_repo({
        "scripts/check.sh": (ROOT / "scripts/check.sh").read_text(encoding="utf-8"),
        "unity/ProjectSettings/ProjectVersion.txt": "m_EditorVersion: 0.0.0f1\n",
    })
    commit_all(repo, "base")
    fake = tmp_path / "bin"
    fake.mkdir()
    for tool, code in (("uv", uv_exit), ("dotnet", 0)):
        p = fake / tool
        p.write_text(f"#!/bin/sh\necho \"{tool} stand-in: the detail that used to be lost\"\nexit {code}\n")
        p.chmod(p.stat().st_mode | stat.S_IXUSR)
    env = {**os.environ, "PATH": f"{fake}{os.pathsep}{os.environ['PATH']}",
           "CHECK_UNITY_CONTENTS": str(tmp_path / "no-unity")}
    r = subprocess.run(["bash", "scripts/check.sh", "HEAD"], cwd=repo, env=env, capture_output=True, text=True)
    return repo, r


def test_a_failing_step_keeps_its_log_and_prints_the_path(make_repo, tmp_path):
    repo, r = _run_check(make_repo, tmp_path, uv_exit=1)
    assert r.returncode == 1, r.stdout + r.stderr
    assert "FAILED: changelog (against HEAD)" in r.stdout
    kept = list((repo / ".superpowers/check-logs").glob("changelog-against-HEAD-*.log"))
    assert len(kept) == 1, kept
    assert "uv stand-in: the detail that used to be lost" in kept[0].read_text(encoding="utf-8")
    last = r.stdout.rstrip().splitlines()[-1]
    prefix = "kept the failing step's log: "
    assert last.startswith(prefix), r.stdout
    assert Path(last[len(prefix):]).resolve() == kept[0].resolve()


def test_a_passing_run_keeps_no_log(make_repo, tmp_path):
    repo, r = _run_check(make_repo, tmp_path, uv_exit=0)
    assert r.returncode == 0, r.stdout + r.stderr
    assert r.stdout.rstrip().splitlines()[-1] == "ALL CHECKS PASSED"
    assert "kept the failing step's log" not in r.stdout
    assert not (repo / ".superpowers/check-logs").exists()
