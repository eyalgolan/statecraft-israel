import plistlib
import subprocess
from datetime import datetime
from pathlib import Path

import pytest

# nightly_launch locks with flock (through nightly_state); fcntl does not exist on Windows.
pytest.importorskip("fcntl", reason="fcntl is POSIX-only (nightly_state uses flock); not on Windows")

import nightly_launch as nl  # noqa: E402
import nightly_state as ns  # noqa: E402
import unity_postmerge as up  # noqa: E402

TOOLS = Path(__file__).resolve().parents[1]
NOW = datetime(2026, 10, 28, 3, 0, tzinfo=ns.TZ)


class Recorder:
    """Stands in for git and cp: records (cwd, args) and never starts a process."""

    def __init__(self, fail_on: str | None = None):
        self.calls: list[tuple[Path, tuple]] = []
        self.fail_on = fail_on

    def git(self, cwd, *args):
        self.calls.append((cwd, args))
        if self.fail_on and args[0] == self.fail_on:
            raise subprocess.CalledProcessError(128, ["git", *args], stderr="fatal: boom")
        return ""

    def copy(self, cwd, src, dest):
        self.calls.append((cwd, ("cp", "-c", "-R", str(src), str(dest))))


def cfg_for(tmp_path, *, clone=True, library=False):
    main, clone_dir = tmp_path / "main", tmp_path / "clone"
    (main / "unity/Library").mkdir(parents=True) if library else main.mkdir(parents=True)
    if clone:
        (clone_dir / ".git").mkdir(parents=True)
    return ns.Config(main_repo=main, clone=clone_dir, handoff=tmp_path / "HANDOFF.md")


def write_config(home, cfg):
    home.mkdir(parents=True, exist_ok=True)
    (home / "config.json").write_text(f'{{"clone": "{cfg.clone}", "main_repo": "{cfg.main_repo}", '
                                      f'"handoff": "{cfg.handoff}"}}')


def test_the_plist_runs_at_0300_and_never_on_an_interval():
    filled = up.fill_plist((TOOLS / "nightly.plist.in").read_text(), uv="/opt/uv", repo="/r", home="/h", path="/usr/bin:/bin")
    p = plistlib.loads(filled.encode())
    assert p["Label"] == "com.statecraft.nightly" and p["StartCalendarInterval"] == {"Hour": 3, "Minute": 0}
    assert "StartInterval" not in p and not p.get("RunAtLoad") and "@" not in filled
    assert p["ProgramArguments"][:3] == ["/opt/uv", "run", "/h/nightly_launch.py"]
    assert p["StandardOutPath"] == p["StandardErrorPath"] == "/h/launchd.log"


def test_the_launcher_keeps_what_the_postmerge_job_keeps():
    assert nl.KEEP == up.KEEP


def test_prepare_clone_resets_only_the_clones_own_main_and_keeps_the_library(tmp_path):
    cfg, rec = cfg_for(tmp_path, library=True), Recorder()
    nl.prepare_clone(cfg, rec.git, rec.copy)
    clone = cfg.clone
    assert rec.calls == [
        (clone, ("fetch", "--quiet", "--tags", "origin", "+refs/heads/main:refs/remotes/origin/main")),
        (clone, ("checkout", "--quiet", "main")),
        (clone, ("reset", "--hard", "refs/remotes/origin/main")),
        (clone, ("clean", "-ffdx", "-e", "/unity/Library/", "-e", "/unity/Assets/Plugins/NuGet/",
                 "-e", "/unity/Assets/Plugins/NuGet.meta")),
        (cfg.main_repo, ("cp", "-c", "-R", str(cfg.main_repo / "unity/Library"), str(clone / "unity/Library"))),
    ]
    assert all(cwd != cfg.main_repo for cwd, args in rec.calls if args[0] != "cp")


def test_the_library_is_cloned_once_and_only_when_the_main_checkout_has_one(tmp_path):
    cfg, rec = cfg_for(tmp_path, library=True), Recorder()
    (cfg.clone / "unity/Library").mkdir(parents=True)
    nl.prepare_clone(cfg, rec.git, rec.copy)
    assert not any(args[0] == "cp" for _, args in rec.calls)
    cfg2, rec2 = cfg_for(tmp_path / "b"), Recorder()
    nl.prepare_clone(cfg2, rec2.git, rec2.copy)
    assert not any(args[0] == "cp" for _, args in rec2.calls)


def test_a_git_failure_is_a_refused_clone_gate(tmp_path):
    cfg, rec = cfg_for(tmp_path), Recorder(fail_on="reset")
    with pytest.raises(ns.Refused) as e:
        nl.prepare_clone(cfg, rec.git, rec.copy)
    assert e.value.gate == "clone" and "fatal: boom" in e.value.detail
    assert not any(args[0] == "clean" for _, args in rec.calls)   # nothing is cleaned after a failed reset


def test_a_missing_clone_is_refused_and_recorded_not_cloned(tmp_path, monkeypatch):
    home = tmp_path / "job"
    cfg = cfg_for(tmp_path, clone=False)
    write_config(home, cfg)
    monkeypatch.setattr(nl, "notify", lambda *a, **k: None)
    rec, ran = Recorder(), []
    code = nl.main(["--uv", "/opt/uv", "--home", str(home)], git=rec.git, copy=rec.copy, call=ran.append, now=NOW)
    assert code == 1 and rec.calls == [] and ran == []
    log = (home / "LOG.md").read_text()
    assert "REFUSED" in log and "gate=clone" in log and "2026-10-28 03:00" in log
    assert "gate=clone" in cfg.handoff.read_text()
    assert not cfg.clone.exists()


def test_a_corrupt_config_is_refused_not_a_night_on_defaults(tmp_path, monkeypatch):
    home = tmp_path / "job"
    home.mkdir()
    (home / "config.json").write_text("{not json")
    monkeypatch.setattr(nl, "notify", lambda *a, **k: None)
    monkeypatch.setattr(nl, "Config", lambda: ns.Config(handoff=tmp_path / "HANDOFF.md"))
    rec, ran = Recorder(), []
    assert nl.main(["--uv", "/opt/uv", "--home", str(home)], git=rec.git, copy=rec.copy, call=ran.append, now=NOW) == 1
    assert rec.calls == [] and ran == [] and "gate=config" in (home / "LOG.md").read_text()


def test_a_run_in_progress_exits_0_and_touches_nothing(tmp_path, capsys):
    home = tmp_path / "job"
    rec, ran = Recorder(), []
    with ns.run_lock(home) as held:
        assert held
        code = nl.main(["--uv", "/opt/uv", "--home", str(home)], git=rec.git, copy=rec.copy, call=ran.append, now=NOW)
    assert code == 0 and rec.calls == [] and ran == []
    assert "a run is in progress" in capsys.readouterr().out
    assert not (home / "LOG.md").exists()


def test_a_prepared_clone_runs_the_clones_own_nightly_script_with_the_lock_free(tmp_path):
    home = tmp_path / "job"
    cfg = cfg_for(tmp_path)
    write_config(home, cfg)
    rec, seen = Recorder(), []

    def call(cmd):
        with ns.run_lock(home) as held:   # nightly.py takes this lock itself, so the launcher must have let go
            seen.append((cmd, held))
        return 7

    assert nl.main(["--uv", "/opt/uv", "--home", str(home)], git=rec.git, copy=rec.copy, call=call, now=NOW) == 7
    assert seen == [(["/opt/uv", "run", str(cfg.clone / "tools" / "nightly.py"), "run"], True)]


def test_no_nightly_module_pushes_merges_or_deletes_tags():
    src = "".join((TOOLS / n).read_text() for n in ("nightly.py", "nightly_publish.py", "nightly_state.py", "nightly_checks.py", "nightly_launch.py"))
    for banned in ("git push", "\"push\", \"--force\"", "--cleanup-tag", "\"tag\", \"-d\"", "pr merge", "push --force"):
        assert banned not in src
