import dataclasses
import json
import subprocess
import sys
import time
import zipfile
from datetime import date, datetime
from pathlib import Path

import pytest

pytest.importorskip("fcntl")   # nightly imports unity_postmerge, which locks with flock

import nightly
import nightly_state as ns
from conftest import FakeRunner

SHA = "a" * 40
OLD = "b" * 40
NOW = datetime(2026, 10, 28, 3, 0, tzinfo=ns.TZ)
TAG = "v0.3.2"


class FakeGH:
    """The GitHub client as the night uses it. `release` is what release_state answers (None: no release); `fail`
    maps a method name to an exception raised by it (a list raises one per call, then succeeds)."""

    def __init__(self, release=None, *, fail=None, previous_tag="v0.3.1"):
        self.release = release
        self.fail = fail or {}
        self.previous_tag = previous_tag
        self.latest = "v0.3.1"   # deliberately different from the previous nightly asset tag
        self.calls = []
        self.baseline = None   # a zip the download hands out

    def _maybe_fail(self, name):
        err = self.fail.get(name)
        if isinstance(err, list):
            err = err.pop(0) if err else None
        if err is not None:
            raise err

    def release_state(self, tag):
        self.calls.append(("release_state", tag))
        return self.release

    def next_version(self):
        self.calls.append(("next_version",))
        return "0.3.2"

    def ensure_tag_available(self, tag):
        self.calls.append(("ensure_tag_available", tag))
        if self.release is not None:
            raise ns.Refused("tag-exists", f"a {self.release} release already has the tag {tag}")

    def delete_draft(self, tag):
        self.calls.append(("delete_draft", tag))
        self.release = None

    def dispatch_site(self, deadline):
        self.calls.append(("dispatch_site", deadline))
        self._maybe_fail("dispatch_site")

    def previous_build_tag(self, state_tag):
        self.calls.append(("previous_build_tag", state_tag))
        return state_tag or self.previous_tag

    def download(self, tag, pattern, dest):
        self.calls.append(("download", tag, pattern, dest))
        self._maybe_fail("download")
        return self.baseline

    def create_draft(self, tag, sha, notes, assets):
        self.calls.append(("create_draft", tag, sha, notes, list(assets)))
        self._maybe_fail("create_draft")
        self.release = "draft"

    def verify_draft(self, tag, local, dest):
        self.calls.append(("verify_draft", tag, list(local), dest))
        self._maybe_fail("verify_draft")

    def publish(self, tag, sha):
        self.calls.append(("publish", tag, sha))
        self.latest = tag   # the edit may succeed before verification fails
        self._maybe_fail("publish")
        self.release = "published"

    def latest_tag(self):
        self.calls.append(("latest_tag",))
        self._maybe_fail("latest_tag")
        return self.latest

    def restore_latest(self, previous, failed):
        self.calls.append(("restore_latest", previous, failed))
        self._maybe_fail("restore_latest")
        if self.latest != failed:
            return self.latest
        self.latest = previous
        return None

    def merged_pr_bodies(self, shas):
        self.calls.append(("merged_pr_bodies", set(shas)))
        return []

    def names(self):
        return [c[0] for c in self.calls]


class Env:
    """A tmp job folder, a tmp clone with the files preflight reads, and a FakeRunner answering the clone checks."""

    def __init__(self, tmp_path, monkeypatch):
        self.job = tmp_path / "job"
        self.job.mkdir()
        clone = tmp_path / "clone"
        (clone / "sim/Statecraft.Sim").mkdir(parents=True)
        (clone / "sim/Statecraft.Sim/SimInfo.cs").write_text('public const string Version = "0.3.1";\n')
        (clone / "sim/Statecraft.Sim.Tests/golden").mkdir(parents=True)
        (clone / "sim/Statecraft.Sim.Tests/golden/manifest.txt").write_text("golden-manifest\n")
        (clone / "unity/ProjectSettings").mkdir(parents=True)
        (clone / "unity/ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.3.25f1\n")
        self.licence = tmp_path / "lic.xml"
        self.licence.write_text("x")
        self.editor = tmp_path / "Unity"
        self.editor.write_text("x")
        self.slots = tmp_path / "unity_slots.py"
        self.slots.write_text("x")
        self.marker = tmp_path / "EXCLUSIVE-WINDOW"
        monkeypatch.setattr(nightly.unity_postmerge, "unity_path", lambda version: str(self.editor))
        self.cfg = ns.Config(clone=clone, licence=self.licence, unity_slots=self.slots, exclusive_marker=self.marker,
                             handoff=tmp_path / "HANDOFF.md")
        self.runner = FakeRunner()
        self.branch, self.head, self.origin, self.dirty = "main", SHA, SHA, ""
        self._rules()
        self.state_path = self.job / "state.json"

    def _rules(self):
        clone = str(self.cfg.clone)
        # FakeRunner answers fixed text, so rules are rebuilt by set() when a field changes
        self.runner.rules.clear()
        self.runner.add(["git", "-C", clone, "rev-parse", "--abbrev-ref", "HEAD"], out=self.branch + "\n")
        self.runner.add(["git", "-C", clone, "rev-parse", "HEAD"], out=self.head + "\n")
        self.runner.add(["git", "-C", clone, "rev-parse", "refs/remotes/origin/main"], out=self.origin + "\n")
        self.runner.add(["git", "-C", clone, "status", "--porcelain", "--untracked-files=no"], out=self.dirty)

    def set(self, **kw):
        for k, v in kw.items():
            setattr(self, k, v)
        self._rules()

    def pre(self, state=None, gh=None, *, now=NOW, manual=False, free=100.0, no_publish=False):
        return nightly.preflight(self.cfg, state or ns.State(), self.runner, gh or FakeGH(), job=self.job, now=now,
                                 manual=manual, free_gb=lambda p: free, no_publish=no_publish)

    def night(self, state=None, gh=None, *, now=NOW, manual=False, free=100.0, no_publish=False):
        if state is not None:
            ns.save_state(self.state_path, state)
        return nightly.run_night(self.cfg, self.state_path, self.job, self.runner, gh or FakeGH(), None, now=now,
                                 manual=manual, no_publish=no_publish, free_gb=lambda p: free)

    def log(self):
        p = self.job / "LOG.md"
        return p.read_text() if p.exists() else ""


@pytest.fixture
def env(tmp_path, monkeypatch, no_processes):
    monkeypatch.setattr(nightly, "notify", lambda *a, **k: env_notes.append(a))
    env_notes.clear()
    return Env(tmp_path, monkeypatch)


env_notes: list = []


def gate_of(env, **kw):
    with pytest.raises(ns.Stop) as e:
        env.pre(**kw)
    return e.value


def test_a_clean_preflight_builds_the_night(env):
    n = env.pre()
    assert (n.sha, n.version, n.tag, n.day) == (SHA, "0.3.2", TAG, date(2026, 10, 28))
    assert n.manifest.strip() == "golden-manifest"
    assert n.run_dir == env.job / "runs" / "20261028-030000" and n.job == env.job
    assert n.deadline > time.monotonic()


def test_preflight_selects_the_numeric_max_across_scripted_release_pages(env):
    from nightly_publish import GitHub
    cmd = ["gh", "api", f"repos/{env.cfg.repo_slug}/releases?per_page=100", "--paginate", "--slurp"]
    env.runner.add(cmd, out=json.dumps([
        [{"tag_name": "v0.9.9", "draft": False}, {"tag_name": "v99.0.0", "draft": True}],
        [{"tag_name": "v0.10.0", "draft": False}, {"tag_name": "v99.0.0-nightly.20261028", "draft": False}],
    ]))
    env.runner.add(["gh", "release", "view"], code=1,
                   effect=lambda cmd: (_ for _ in ()).throw(ns.Refused("command", "release not found")))
    env.runner.add(["git", "-C", str(env.cfg.clone), "tag", "--list", "v0.10.1"])
    env.runner.add(["git", "-C", str(env.cfg.clone), "ls-remote", "origin", "refs/tags/v0.10.1",
                    "refs/tags/v0.10.1^{}"])
    gh = GitHub(env.runner, env.cfg.repo_slug, env.cfg.clone)
    n = env.pre(gh=gh)
    assert (n.version, n.tag) == ("0.10.1", "v0.10.1") and cmd in env.runner.calls


def test_an_unchanged_main_is_skipped(env):
    st = ns.State(last_published=ns.Published("v0.3.0-nightly.20261027", SHA, "0.3.0", {}, None))
    assert env.night(st) == "SKIPPED"
    assert "SKIPPED" in env.log()
    assert all(c[0] == "git" for c in env.runner.calls)   # no Unity, no gh write
    assert env.state_path.exists() and ns.load_state(env.state_path).last_run_day == "2026-10-28"


@pytest.mark.parametrize("tag", ["v0.3.1-nightly.20261027", "v0.3.2", "unrelated"])
def test_skip_uses_the_saved_sha_before_looking_up_a_version(env, tag):
    gh = FakeGH()
    st = ns.State(last_published=ns.Published(tag, SHA, "0.3.2", {}, None))
    ns.save_state(env.state_path, st)
    with pytest.raises(ns.Skipped):
        env.pre(ns.load_state(env.state_path), gh)
    assert gh.calls == []


def test_changed_sha_builds_even_when_the_saved_tag_matches(env):
    gh = FakeGH()
    st = ns.State(last_published=ns.Published(TAG, OLD, "0.3.2", {}, None))
    assert env.pre(st, gh).sha == SHA
    assert gh.calls == [("next_version",), ("release_state", TAG), ("ensure_tag_available", TAG)]


def test_a_late_start_is_missed_and_earlier_gaps_are_reported(env):
    late = datetime(2026, 10, 28, 9, 10, tzinfo=ns.TZ)
    assert env.night(ns.State(last_run_day="2026-10-25"), now=late) == "MISSED"
    missed = [ln for ln in env.log().splitlines() if "MISSED" in ln]
    assert [ln[2:12] for ln in missed] == ["2026-10-26", "2026-10-27", "2026-10-28"]
    assert env.runner.calls == []   # missed before any clone check


def test_a_manual_run_ignores_the_window(env):
    late = datetime(2026, 10, 28, 9, 10, tzinfo=ns.TZ)
    assert env.pre(now=late, manual=True).tag == TAG


def test_low_disk_refuses_before_anything_runs(env):
    e = gate_of(env, free=39.9)
    assert isinstance(e, ns.Refused) and e.gate == "disk"
    assert all(c[0] == "git" for c in env.runner.calls)


def test_the_exclusive_window_marker_refuses(env):
    env.marker.write_text("x")
    assert gate_of(env).gate == "exclusive-window"


@pytest.mark.parametrize("what,gate", [("licence", "unity-licence"), ("editor", "unity-editor"),
                                       ("slots", "unity-slots")])
def test_a_missing_licence_unity_editor_or_unity_slots_refuses(env, what, gate):
    getattr(env, what).unlink()
    assert gate_of(env).gate == gate


@pytest.mark.parametrize("change", [{"branch": "feat/x"}, {"head": OLD}, {"dirty": " M a.cs\n"}])
def test_the_clone_must_be_main_at_origin_main_and_clean(env, change):
    env.set(**change)
    e = gate_of(env)
    assert isinstance(e, ns.Refused) and e.gate == "clone"


def test_same_day_rerun_cleans_its_draft_and_refuses_a_published_tag(env):
    gh = FakeGH("draft")
    st = ns.State(open_draft=TAG)
    env.pre(st, gh)
    assert ("delete_draft", TAG) in gh.calls
    assert st.open_draft is None
    gh = FakeGH("published")
    with pytest.raises(ns.Refused) as e:
        env.pre(ns.State(open_draft=TAG), gh)
    assert e.value.gate == "tag-exists" and ("delete_draft", TAG) not in gh.calls
    gh = FakeGH("draft")   # a draft this job did not leave
    with pytest.raises(ns.Refused) as e:
        env.pre(ns.State(), gh)
    assert e.value.gate == "tag-exists" and ("delete_draft", TAG) not in gh.calls


def test_a_stranded_own_draft_is_cleaned_and_the_night_proceeds(env):
    class Releases(FakeGH):
        def next_version(self):
            self.calls.append(("next_version",))
            return ns.next_version([{"tag_name": "v0.3.1", "draft": False},
                                    {"tag_name": TAG, "draft": True}])
    gh, st = Releases("draft"), ns.State(open_draft=TAG)
    assert env.pre(st, gh).tag == TAG
    assert gh.calls == [("next_version",), ("release_state", TAG), ("delete_draft", TAG),
                        ("ensure_tag_available", TAG)]
    assert st.open_draft is None


def test_a_foreign_draft_with_the_next_tag_refuses_without_deletion(env):
    gh, st = FakeGH("draft"), ns.State(open_draft="v0.3.0")
    with pytest.raises(ns.Refused) as e:
        env.pre(st, gh)
    assert e.value.gate == "tag-exists" and "delete_draft" not in gh.names()
    assert st.open_draft == "v0.3.0"


def test_a_missing_tool_is_refused_and_recorded(env):
    def gone(cmd):
        raise FileNotFoundError("gh")
    env.runner.rules.clear()
    env.runner.add(["git"], effect=gone)
    assert env.night() == "REFUSED"
    assert "REFUSED" in env.log() and "gate=preflight" in env.log()
    assert len(env_notes) == 1


def test_no_publish_writes_no_record(env):
    st = ns.State(last_published=ns.Published("v0.3.0", SHA, "0.3.0", {}, None))
    assert env.night(st, no_publish=True) == "SKIPPED"
    assert env.log() == "" and ns.load_state(env.state_path).last_run_day is None


def test_a_corrupt_state_refuses_without_overwriting_it(env):
    env.state_path.write_text("{nope")
    assert env.night() == "REFUSED"
    assert env.state_path.read_text() == "{nope" and "gate=state" in env.log()


def pending_state():
    return ns.State(last_published=ns.Published("v0.3.0", SHA, "0.3.0", {}, None), site_pending="0.3.0-nightly.20261027")


def test_site_pending_is_retried_before_skipping(env, monkeypatch):
    served = []
    monkeypatch.setattr(nightly, "demo_serves", lambda url, version, *, deadline: served.append((url, version, deadline)))
    gh = FakeGH()
    assert env.night(pending_state(), gh) == "SKIPPED"
    assert gh.names() == ["dispatch_site"] and gh.calls[0][1] > time.monotonic()
    assert [s[:2] for s in served] == [(env.cfg.site_demo_url, "0.3.0-nightly.20261027")]
    assert ns.load_state(env.state_path).site_pending is None   # the retry worked, so nothing is pending
    assert "SKIPPED" in env.log() and "now serves" in env.log()


@pytest.mark.parametrize("where", ["dispatch", "demo"])
def test_a_failing_site_retry_stays_pending_and_the_night_stays_skipped(env, monkeypatch, where):
    def demo(url, version, *, deadline):
        if where == "demo":
            raise ns.Refused("site", "still serves 0.3.0")
    monkeypatch.setattr(nightly, "demo_serves", demo)
    gh = FakeGH(fail={"dispatch_site": ns.Refused("site", "run failed")} if where == "dispatch" else None)
    assert env.night(pending_state(), gh) == "SKIPPED"
    assert ns.load_state(env.state_path).site_pending == "0.3.0-nightly.20261027"
    assert "SKIPPED" in env.log() and "still failed" in env.log() and env_notes == []


@pytest.mark.parametrize("where", ["dispatch", "demo"])
def test_a_non_refusal_error_in_the_site_retry_keeps_the_night_skipped_and_pending(env, monkeypatch, where):
    def demo(url, version, *, deadline):
        if where == "demo":
            raise KeyError("databaseId")
    monkeypatch.setattr(nightly, "demo_serves", demo)
    gh = FakeGH(fail={"dispatch_site": KeyError("databaseId")} if where == "dispatch" else None)
    assert env.night(pending_state(), gh) == "SKIPPED"
    assert ns.load_state(env.state_path).site_pending == "0.3.0-nightly.20261027"
    assert "KeyError" in env.log() and "still failed" in env.log()


def test_no_publish_never_retries_the_site(env, monkeypatch):
    monkeypatch.setattr(nightly, "demo_serves", lambda *a, **k: pytest.fail("a store call under --no-publish"))
    gh = FakeGH()
    assert env.night(pending_state(), gh, no_publish=True) == "SKIPPED"
    assert gh.calls == []


def test_no_publish_leaves_its_own_draft_alone(env):
    gh = FakeGH("draft")
    st = ns.State(open_draft=TAG)
    with pytest.raises(ns.Refused) as e:
        env.pre(st, gh, no_publish=True)
    assert e.value.gate == "tag-exists"
    assert ("delete_draft", TAG) not in gh.calls and st.open_draft == TAG


def test_no_release_means_the_tag_is_free_and_any_release_refuses(env):
    gh = FakeGH(None)
    assert env.pre(gh=gh).tag == TAG
    for state in ("draft", "published"):
        with pytest.raises(ns.Refused) as e:
            env.pre(gh=FakeGH(state))
        assert e.value.gate == "tag-exists"


def test_run_bounded_terminates_at_the_deadline(tmp_path):
    t = time.monotonic()
    code = nightly.ProcRunner(grace=1).run([sys.executable, "-c", "import time; time.sleep(60)"],
                                           cwd=tmp_path, log=tmp_path / "l", deadline=time.monotonic() + 1)
    assert code == 124 and time.monotonic() - t < 10


def test_capture_refuses_on_a_non_zero_exit(tmp_path):
    r = nightly.ProcRunner()
    assert r.capture([sys.executable, "-c", "print('hi')"], cwd=tmp_path).strip() == "hi"
    with pytest.raises(ns.Refused) as e:
        r.capture([sys.executable, "-c", "raise SystemExit(3)"], cwd=tmp_path)
    assert e.value.gate == "command"


def test_the_deadline_kills_a_grandchild_that_ignores_sigterm(tmp_path):
    import threading
    pidfile = tmp_path / "pid"
    grand = ("import signal,time,pathlib;signal.signal(signal.SIGTERM,signal.SIG_IGN);"
             f"pathlib.Path({str(pidfile)!r}).write_text(str(__import__('os').getpid()));time.sleep(60)")
    top = ("import subprocess,sys,time;subprocess.Popen([sys.executable,'-c',sys.argv[1]]);time.sleep(60)")
    code = nightly.ProcRunner(grace=1).run([sys.executable, "-c", top, grand], cwd=tmp_path, log=tmp_path / "l",
                                           deadline=time.monotonic() + 8)   # long enough for a busy machine to start both
    assert code == 124
    end = time.monotonic() + 30
    while not pidfile.exists() and time.monotonic() < end:   # the grandchild may only just have written it
        time.sleep(0.1)
    assert pidfile.exists(), "the grandchild never started, so the test proves nothing"
    pid = int(pidfile.read_text())
    time.sleep(0.5)
    with pytest.raises(ProcessLookupError):
        import os
        os.kill(pid, 0)


def test_a_failed_state_save_still_writes_the_record(env, monkeypatch):
    def boom(*a, **k):
        raise OSError("disk full")
    monkeypatch.setattr(nightly, "save_state", boom)
    st = ns.State(last_published=ns.Published("v0.3.0", SHA, "0.3.0", {}, None))
    assert env.night(st) == "SKIPPED"
    assert "SKIPPED" in env.log()


# --- gates and the full EditMode run (Task 3) -------------------------------------------------------------------

import os
from nightly_state import Refused

OK_CHECK = "== unity compile (dotnet, outside Unity)\nALL CHECKS PASSED\n"
DOTNET_OK = "Passed!  - Failed:     0, Passed:  10, Skipped:     0, Total:  10, Duration: 1 s"


class EnvRunner(FakeRunner):
    """Also records the env each command ran with."""

    def __init__(self):
        super().__init__()
        self.envs = []

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        self.envs.append((list(cmd), env, holder))
        return super().run(cmd, cwd=cwd, log=log, deadline=deadline, env=env, holder=holder)


@pytest.fixture
def night(env):
    env.runner = EnvRunner()
    env._rules()
    n = env.pre()
    n.run_dir.mkdir(parents=True)
    return n


def test_unity_goes_through_slots_and_the_shared_lock(night):
    argv = nightly.unity_argv(night, ["-runTests"], webgl=False)
    assert argv[:3] == ["python3", "-I", str(night.cfg.unity_slots)] and "--webgl" not in argv
    i = argv.index("--")
    assert argv[i + 1:i + 6] == ["uv", "run", str(night.cfg.clone / "tools/machine_lock.py"), "--shared", "--"]
    assert "--wait-unity" not in argv and "-nographics" not in argv
    assert argv[-1] == "-runTests" and str(night.cfg.clone / "unity") in argv
    assert nightly.unity_argv(night, ["-quit"], webgl=True)[3] == "--webgl"
    env = nightly.unity_env(night)
    assert env["UNITY_SLOTS"] == "3" and Path(env["UNITY_MCP_SERVER_PATH"]).is_file()
    assert Path(env["UNITY_MCP_SERVER_PATH"]).read_text() == ""


class LocaleRunner(EnvRunner):
    """Answers `dotnet test` by the LANG it runs with."""

    def __init__(self, he, en):
        super().__init__()
        self.by_lang = {"he_IL.UTF-8": he, "en_US.UTF-8": en}

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        if cmd[:2] == ["dotnet", "test"]:
            self.calls.append(list(cmd))
            self.envs.append((list(cmd), env, holder))
            code, out = self.by_lang[env["LANG"]]
            Path(log).parent.mkdir(parents=True, exist_ok=True)
            Path(log).write_text(out)
            return code
        return super().run(cmd, cwd=cwd, log=log, deadline=deadline, env=env, holder=holder)


def gates_runner(night, env, *, check=(0, OK_CHECK), rg=(0, "RELEASE GATE OK\n"), he=(0, DOTNET_OK),
                 en=(0, DOTNET_OK)):
    r = LocaleRunner(he, en)
    clone = night.cfg.clone
    r.add(["uv", "run", f"{clone}/tools/build_sim_for_unity.py"], 0, "")
    r.add(["bash", "scripts/check.sh", "origin/main"], *check)
    r.add(["bash", "scripts/oss/release_gate.sh"], *rg)
    return r


def test_gates_run_in_order_with_both_locales(night, env):
    r = gates_runner(night, env)
    nightly.run_gates(night, r)
    assert r.calls == [
        ["uv", "run", f"{night.cfg.clone}/tools/build_sim_for_unity.py"],
        ["bash", "scripts/check.sh", "origin/main"],
        ["bash", "scripts/oss/release_gate.sh"],
        ["dotnet", "test", "sim/Statecraft.sln", "-c", "Release", "--nologo"],
        ["dotnet", "test", "sim/Statecraft.sln", "-c", "Release", "--nologo"],
    ]
    langs = [(e["LANG"], e["LC_ALL"]) for c, e, _ in r.envs if c[0] == "dotnet"]
    assert langs == [("he_IL.UTF-8", "he_IL.UTF-8"), ("en_US.UTF-8", "en_US.UTF-8")]


@pytest.mark.parametrize("which,gate,kw", [
    ("build", "build-sim", None),
    ("check", "check", dict(check=(1, OK_CHECK))),
    ("check-skip", "check", dict(check=(0, "== unity compile: SKIPPED, no unity/Library\nALL CHECKS PASSED\n"))),
    ("rg", "release-gate", dict(rg=(0, "RELEASE GATE FAILED\n"))),
    ("he", "sim-he_IL", dict(he=(0, "Build succeeded."))),
    ("he-fail", "sim-he_IL", dict(he=(1, "Failed!  - Failed:     2, Passed:  8, Skipped:     0, Total:  10"))),
    ("en", "sim-en_US", dict(en=(0, "Build succeeded."))),
    ("en-fail", "sim-en_US", dict(en=(1, "Failed!  - Failed:     1, Passed:  9, Skipped:     0, Total:  10"))),
    ("timeout", "check", dict(check=(124, OK_CHECK))),
])
def test_a_failing_gate_refuses_by_name(night, env, which, gate, kw):
    r = gates_runner(night, env, **(kw or {}))
    if which == "build":
        r.rules.insert(0, (("uv", "run", f"{night.cfg.clone}/tools/build_sim_for_unity.py"), 2, "", None))
    with pytest.raises(Refused) as e:
        nightly.run_gates(night, r)
    assert e.value.gate == gate


def test_a_later_gate_does_not_run_after_a_refusal(night, env):
    r = gates_runner(night, env, check=(1, OK_CHECK))
    with pytest.raises(Refused):
        nightly.run_gates(night, r)
    assert not any(c[0] == "dotnet" or c[:2] == ["bash", "scripts/oss/release_gate.sh"] for c in r.calls)


WARM_OK = "UnityMcpGuard warm-up: gate defines in place\n"


class EditRunner(EnvRunner):
    """Unity steps (python3 -I ...) answer by kind: the warm-up has -executeMethod, the test run -runTests."""

    def __init__(self, night, xml, code, warm_code, warm_log, restore_code):
        super().__init__()
        self.night, self.xml, self.code, self.warm_code = night, xml, code, warm_code
        self.warm_log, self.restore_code = warm_log, restore_code

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        self.envs.append((list(cmd), env, holder))
        self.calls.append(list(cmd))
        d = self.night.run_dir
        if cmd[0] == "git":
            return self.restore_code
        if "-executeMethod" in cmd:
            (d / "warmup.log").write_text(self.warm_log)
            return self.warm_code
        if self.xml is not None:
            (d / "editmode.xml").write_text(self.xml)
        return self.code


def editmode_runner(night, *, xml='<test-run total="5" passed="5" failed="0"/>', code=0, warm_code=0,
                    warm_log=WARM_OK, restore_code=0):
    return EditRunner(night, xml, code, warm_code, warm_log, restore_code)


def test_editmode_needs_tests_and_no_failures_and_never_quits(night, env):
    r = editmode_runner(night)
    nightly.run_editmode(night, r)
    warm, tests, restore = r.envs
    assert "-executeMethod" in warm[0] and nightly.unity_postmerge.WARM_UP in warm[0] and "-quit" not in warm[0]
    assert "-runTests" in tests[0] and "-quit" not in tests[0] and "-testPlatform" in tests[0]
    assert "--webgl" not in tests[0] and warm[1]["UNITY_MCP_SERVER_PATH"] == tests[1]["UNITY_MCP_SERVER_PATH"]
    assert tests[2] == night.run_dir / "lock-holder"
    assert restore[0] == nightly.RESTORE_ARGV


def test_editmode_with_zero_tests_refuses_and_still_restores_settings(night, env):
    r = editmode_runner(night, xml='<test-run total="0" passed="0" failed="0"/>')
    with pytest.raises(Refused) as e:
        nightly.run_editmode(night, r)
    assert e.value.gate == "editmode"
    assert r.envs[-1][0][0] == "git"


def test_editmode_failure_or_missing_results_or_timeout_refuses(night, env):
    for kw in (dict(xml='<test-run total="5" passed="4" failed="1"/>'), dict(xml=None), dict(code=124)):
        r = editmode_runner(night, **kw)
        with pytest.raises(Refused) as e:
            nightly.run_editmode(night, r)
        assert e.value.gate == "editmode"


def test_settings_are_restored_when_the_runner_raises(night, env):
    r = editmode_runner(night)
    real = r.run

    def boom(cmd, **k):
        if "-runTests" in cmd:
            raise OSError("disk gone")
        return real(cmd, **k)
    r.run = boom
    with pytest.raises(OSError):
        nightly.run_editmode(night, r)
    assert r.envs[-1][0][0] == "git"


def test_the_exclusive_marker_appearing_before_a_unity_step_refuses(night, env):
    env.marker.write_text("x")
    r = editmode_runner(night)
    with pytest.raises(Refused) as e:
        nightly.run_editmode(night, r)
    assert e.value.gate == "exclusive-window" and not r.envs


def test_a_warm_up_timeout_refuses_and_restores(night, env):
    r = editmode_runner(night, warm_code=124)
    with pytest.raises(Refused) as e:
        nightly.run_editmode(night, r)
    assert e.value.gate == "editmode" and "timed out" in e.value.detail
    assert [c[0][0] for c in r.envs] == ["python3", "git"]


@pytest.mark.parametrize("kw", [dict(warm_code=1), dict(warm_log=""), dict(warm_code=1, warm_log=WARM_OK)])
def test_a_failed_or_unconfirmed_warm_up_refuses_before_any_test_run(night, env, kw):
    r = editmode_runner(night, **kw)
    with pytest.raises(Refused) as e:
        nightly.run_editmode(night, r)
    assert e.value.gate == "editmode" and "warm-up" in e.value.detail
    assert not any("-runTests" in c for c in r.calls)
    assert r.envs[-1][0][0] == "git"


def test_the_warm_up_and_the_test_run_share_the_lock_holder(night, env):
    r = editmode_runner(night)
    nightly.run_editmode(night, r)
    assert r.envs[0][2] == night.run_dir / "lock-holder" == r.envs[1][2]


def test_a_failed_restore_does_not_replace_the_original_refusal(night, env, capsys):
    r = editmode_runner(night, xml='<test-run total="5" passed="4" failed="1"/>', restore_code=1)
    with pytest.raises(Refused) as e:
        nightly.run_editmode(night, r)
    assert e.value.gate == "editmode"
    assert "restore-settings" in (night.run_dir / "restore-settings.note").read_text() + capsys.readouterr().err


def test_a_failed_restore_alone_refuses(night, env):
    r = editmode_runner(night, restore_code=1)
    with pytest.raises(Refused) as e:
        nightly.run_editmode(night, r)
    assert e.value.gate == "restore-settings"


@pytest.mark.parametrize("how", ["warm-up refusal", "runner raises"])
def test_a_pending_error_and_a_failed_restore_keep_the_original(night, env, how):
    if how == "warm-up refusal":
        r = editmode_runner(night, warm_code=1, restore_code=1)
        expect, exc = "editmode", Refused
    else:
        r = editmode_runner(night, restore_code=1)
        real = r.run

        def boom(cmd, **k):
            if "-runTests" in cmd:
                raise OSError("disk gone")
            return real(cmd, **k)
        r.run = boom
        expect, exc = None, OSError
    with pytest.raises(exc) as e:
        nightly.run_editmode(night, r)
    if expect:
        assert e.value.gate == expect
    assert "restore-settings" in (night.run_dir / "restore-settings.note").read_text()


def test_a_restore_only_failure_is_not_swallowed_inside_a_caller_except_block(night, env):
    r = editmode_runner(night, restore_code=1)
    try:
        raise ValueError("caller is handling something")
    except ValueError:
        with pytest.raises(Refused) as e:
            nightly.run_editmode(night, r)
    assert e.value.gate == "restore-settings"


# --- the stamp, the two web builds and packaging ----------------------------------------------------------------

STAMPED = "0.3.2"
RESTORE_CMD = nightly.RESTORE_ARGV


def test_the_stamp_replaces_exactly_the_one_bundle_version(tmp_path):
    s = tmp_path / "ProjectSettings.asset"
    s.write_text("  productName: X\n  bundleVersion: 0.3.1\n  other: 1\n")
    nightly.stamp_bundle_version(s, STAMPED)
    assert s.read_text() == f"  productName: X\n  bundleVersion: {STAMPED}\n  other: 1\n"
    s.write_text("no version here\n")
    with pytest.raises(ValueError):
        nightly.stamp_bundle_version(s, STAMPED)
    s.write_text("  bundleVersion: 1\n  bundleVersion: 2\n")
    with pytest.raises(ValueError):
        nightly.stamp_bundle_version(s, STAMPED)


class WebRunner(EnvRunner):
    """Answers the Build.Web runs (index.html carries `page_version`), the settings restore and the packaging."""

    def __init__(self, night, *, page_version=STAMPED, build_code=0, package_out="PACKAGED x\nOK: notices inside x\n",
                 package_code=0):
        super().__init__()
        self.night, self.page_version, self.build_code = night, page_version, build_code
        self.package_out, self.package_code = package_out, package_code
        self.settings = night.cfg.clone / "unity/ProjectSettings/ProjectSettings.asset"
        self.settings.parent.mkdir(parents=True, exist_ok=True)
        self.settings.write_text("  bundleVersion: 0.3.1\n")
        self.stamped_at_build = []
        self.restores = 0

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        self.envs.append((list(cmd), env, holder))
        self.calls.append(list(cmd))
        if cmd[0] == "git" and cmd[1:3] == ["checkout", "--"]:
            self.restores += 1
            self.settings.write_text("  bundleVersion: 0.3.1\n")
            return 0
        if "-executeMethod" in cmd:
            self.stamped_at_build.append(self.settings.read_text())
            pages = "-decompressionFallback" in cmd
            out = self.night.cfg.clone / "unity/Builds" / ("Web-pages" if pages else "Web-gzip")
            out.mkdir(parents=True, exist_ok=True)
            (out / "index.html").write_text(f'productVersion: "{self.page_version}",\n')
            return self.build_code
        if cmd[0] == "bash":
            Path(log).parent.mkdir(parents=True, exist_ok=True)
            Path(log).write_text(self.package_out)
            return self.package_code
        raise AssertionError(f"unexpected command {cmd}")


def builds(night, runner, *, free=100.0):
    return (nightly.build_web(night, runner, pages=False, free_gb=lambda p: free),
            nightly.build_web(night, runner, pages=True, free_gb=lambda p: free))


def test_builds_follow_releasing_step_5(night, env):
    r = WebRunner(night)
    gz, pg = builds(night, r)
    clone = night.cfg.clone
    assert gz == clone / "unity/Builds/Web-gzip" and pg == clone / "unity/Builds/Web-pages"
    gz_cmd = next(c for c in r.calls if "-executeMethod" in c and "-decompressionFallback" not in c)
    pg_cmd = next(c for c in r.calls if "-decompressionFallback" in c)
    gz_log = str(night.run_dir / "unity-build-web-gz.log")
    pg_log = str(night.run_dir / "unity-build-web-pages.log")
    method = ["-quit", "-buildTarget", "WebGL", "-executeMethod", "Statecraft.Game.EditorTools.Build.Web"]
    i = gz_cmd.index("-quit")
    assert gz_cmd[i:] == [*method, "-compression", "gzip", "-shareable", "-logFile", gz_log]
    j = pg_cmd.index("-quit")
    assert pg_cmd[j:] == [*method, "-compression", "gzip", "-decompressionFallback", "-shareable", "-out",
                          "Builds/Web-pages", "-logFile", pg_log]
    for c in (gz_cmd, pg_cmd):
        assert c[3] == "--webgl" and c[0] == "python3" and "--wait-unity" not in c
    envs = [e for cmd, e, h in r.envs if "-executeMethod" in cmd]
    assert all(Path(e["UNITY_MCP_SERVER_PATH"]).is_file() for e in envs)


def test_the_stamp_is_applied_before_and_removed_after_each_build(night, env):
    r = WebRunner(night)
    builds(night, r)
    assert r.stamped_at_build == [f"  bundleVersion: {STAMPED}\n"] * 2
    assert r.restores == 2 and r.settings.read_text() == "  bundleVersion: 0.3.1\n"
    kinds = ["restore" if c[0] == "git" else "build" for c in r.calls]
    assert kinds == ["build", "restore", "build", "restore"]
    assert r.calls[1] == RESTORE_CMD


def test_a_build_page_without_tonights_version_refuses(night, env):
    for wrong in ("0.3.1", "0.3.1-nightly.20261027"):
        r = WebRunner(night, page_version=wrong)
        with pytest.raises(Refused) as e:
            nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
        assert e.value.gate == "version-stamp"
        assert r.restores == 1 and r.settings.read_text() == "  bundleVersion: 0.3.1\n"
    r = WebRunner(night)
    (night.cfg.clone / "unity/Builds/Web-gzip").mkdir(parents=True, exist_ok=True)
    r.build_code = 0
    r.page_version = "none"
    with pytest.raises(Refused):
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)


def test_a_failed_build_refuses_and_restores(night, env):
    r = WebRunner(night, build_code=1)
    with pytest.raises(Refused) as e:
        nightly.build_web(night, r, pages=True, free_gb=lambda p: 100.0)
    assert e.value.gate == "build-web" and r.restores == 1


def test_low_disk_before_the_second_build_refuses(night, env):
    r = WebRunner(night)
    seen = iter([100.0, 39.0])
    nightly.build_web(night, r, pages=False, free_gb=lambda p: next(seen))
    with pytest.raises(Refused) as e:
        nightly.build_web(night, r, pages=True, free_gb=lambda p: next(seen))
    assert e.value.gate == "disk"
    assert sum("-executeMethod" in c for c in r.calls) == 1
    assert r.settings.read_text() == "  bundleVersion: 0.3.1\n"


def test_the_exclusive_marker_refuses_a_web_build(night, env):
    env.marker.write_text("x")
    r = WebRunner(night)
    with pytest.raises(Refused) as e:
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "exclusive-window" and r.calls == []


def test_a_build_timeout_restores_settings_and_refuses(night, env):
    r = WebRunner(night, build_code=124)
    with pytest.raises(Refused) as e:
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "build-web" and "timed out" in e.value.detail
    assert r.restores == 1 and r.settings.read_text() == "  bundleVersion: 0.3.1\n"   # no stamp left behind


def test_a_failed_restore_does_not_mask_the_build_error(night, env, capsys):
    r = WebRunner(night, build_code=124)
    real = r.run

    def run(cmd, **k):
        return 1 if cmd[0] == "git" else real(cmd, **k)
    r.run = run
    with pytest.raises(Refused) as e:
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "build-web"
    assert (night.run_dir / "restore-settings.note").is_file()


def test_packaging_needs_the_notices_line(night, env):
    r = WebRunner(night)
    build = night.cfg.clone / "unity/Builds/Web-gzip"
    z = nightly.package_zip(night, r, build, False)
    assert z == night.run_dir / f"statecraft-israel-v{STAMPED}-web.zip"
    assert r.calls[-1] == ["bash", "scripts/oss/package_web.sh", str(build), str(z)]
    zp = nightly.package_zip(night, r, night.cfg.clone / "unity/Builds/Web-pages", True)
    assert zp == night.run_dir / f"statecraft-israel-v{STAMPED}-web-pages.zip"
    for kw in (dict(package_out="PACKAGED x\n"), dict(package_out="OK: notices inside x\n"),
               dict(package_code=1), dict(package_code=124)):
        with pytest.raises(Refused) as e:
            nightly.package_zip(night, WebRunner(night, **kw), build, False)
        assert e.value.gate == "package"


def test_the_tree_must_be_clean_after_the_builds(night, env):
    nightly.assert_clean_tree(night, env.runner)
    env.set(dirty=" M unity/ProjectSettings/ProjectSettings.asset\n")
    with pytest.raises(Refused) as e:
        nightly.assert_clean_tree(night, env.runner)
    assert e.value.gate == "dirty-tree"


def test_a_malformed_bundle_version_surfaces_as_a_version_stamp_refusal(night, env):
    r = WebRunner(night)
    r.settings.write_text("  bundleVersion: 0.3.1\n  bundleVersion: 0.3.2\n")
    with pytest.raises(Refused) as e:
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "version-stamp" and "bundleVersion" in e.value.detail
    assert r.restores == 1 and r.settings.read_text() == "  bundleVersion: 0.3.1\n"
    assert not any("-executeMethod" in c for c in r.calls)


# --- Task 5: web checks, sizes, boot -----------------------------------------------------------------------------

class CheckRunner(EnvRunner):
    """Answers the web_check and boot runs: `answers` maps a marker in the argv to (code, text), else (0, `ok`)."""

    def __init__(self, ok="PASS: saw x\n", answers=None):
        super().__init__()
        self.ok, self.answers, self.cwds = ok, answers or {}, []

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        self.envs.append((list(cmd), env, holder))
        self.calls.append(list(cmd))
        self.cwds.append(cwd)
        code, text = 0, self.ok
        for marker, answer in self.answers.items():
            if marker in cmd:
                code, text = answer(len(self.calls)) if callable(answer) else answer
        Path(log).parent.mkdir(parents=True, exist_ok=True)
        Path(log).write_text(text)
        return code


MANIFEST = "# c\nhash1950 aa\nsmoke5 cc\nsmoke-orders dd\n"


def test_a_manifest_without_the_smoke_goldens_refuses(night, env):
    r = CheckRunner()
    with pytest.raises(Refused) as e:
        nightly.run_web_checks(night, r, night.cfg.clone / "unity/Builds/Web-gzip")   # the fixture manifest is a stub
    assert e.value.gate == "web-check" and r.calls == []


def test_web_checks_run_under_the_shared_lock_and_all_must_pass(night, env):
    night.manifest = MANIFEST
    build = night.cfg.clone / "unity/Builds/Web-gzip"
    r = CheckRunner()
    nightly.run_web_checks(night, r, build)
    clone = night.cfg.clone
    assert len(r.calls) == 7
    for c in r.calls:
        assert c[:8] == ["uv", "run", str(clone / "tools/machine_lock.py"), "--shared", "--", "uv", "run",
                         str(clone / "tools/web_check.py")]
        assert c[8] == str(build) and "--wait-unity" not in c
    assert set(r.cwds) == {clone}
    assert (night.run_dir / "web-check-Web-gzip-7.log").is_file()


@pytest.mark.parametrize("answer", [(1, "PASS: saw x\n"), (0, "console: nothing\n"), (0, "TIMEOUT: never saw x\n"),
                                    (124, "PASS: saw x\n")])
def test_a_failing_web_check_pass_refuses_and_stops(night, env, answer):
    night.manifest = MANIFEST
    r = CheckRunner(answers={"--persistence": answer, "--startscreen": answer})
    with pytest.raises(Refused) as e:
        nightly.run_web_checks(night, r, night.cfg.clone / "unity/Builds/Web-pages")
    assert e.value.gate == "web-check" and "Web-pages" in e.value.detail
    assert len(r.calls) == 3   # golden, smoke-orders, then the first startscreen pass fails


class FakeGhSizes:
    def __init__(self, bodies=(), error=None):
        self.bodies, self.error, self.calls = list(bodies), error, []

    def merged_pr_bodies(self, shas):
        self.calls.append(set(shas))
        if self.error:
            raise self.error
        return self.bodies


@pytest.fixture
def fake_gh():
    return FakeGhSizes


def sized_build(root, data):
    from test_nightly_checks import make_build
    return make_build(root, extra=False, data=data)


def baseline_zip(path, data=900):
    with zipfile.ZipFile(path, "w") as z:
        z.writestr("index.html", 'x "Build/h1.data.gz" "Build/h1.wasm.gz" "Build/h1.framework.js.gz"')
        z.writestr("Build/h1.data.gz", b"d" * data)
        z.writestr("Build/h1.wasm.gz", b"w" * 90)
        z.writestr("Build/h1.framework.js.gz", b"f" * 10)
    return path


def test_sizes_refuse_without_a_baseline_zip(night, env, fake_gh, tmp_path):
    build = sized_build(tmp_path / "b", 900)
    with pytest.raises(Refused) as e:
        nightly.check_sizes(night, fake_gh(), build, tmp_path / "absent.zip", OLD, runner=env.runner)
    assert e.value.gate == "sizes" and "baseline" in e.value.detail
    bad = tmp_path / "bad.zip"
    bad.write_text("not a zip")
    with pytest.raises(Refused) as e:
        nightly.check_sizes(night, fake_gh(), build, bad, OLD, runner=env.runner)
    assert e.value.gate == "sizes"


def test_a_budget_is_looked_up_only_when_growth_exceeds_2_percent(night, env, fake_gh, tmp_path):
    clone = str(night.cfg.clone)
    env.runner.add(["git", "-C", clone, "rev-list", f"{OLD}..{SHA}"], out="c1\nc2\n")
    z = baseline_zip(tmp_path / "prev.zip")
    gh = fake_gh()
    assert nightly.check_sizes(night, gh, sized_build(tmp_path / "ok", 920), z, OLD, runner=env.runner) == \
        {"data": 920, "wasm": 90, "framework": 10}
    assert gh.calls == []                                                   # +20 of 1000 needs no lookup
    with pytest.raises(Refused) as e:
        nightly.check_sizes(night, gh, sized_build(tmp_path / "big", 921), z, OLD, runner=env.runner)
    assert e.value.gate == "sizes" and "1021" in e.value.detail and gh.calls == [{"c1", "c2"}]
    gh = fake_gh(["Nightly size budget: +0.001 MB"])
    assert nightly.check_sizes(night, gh, sized_build(tmp_path / "b2", 921), z, OLD, runner=env.runner)["data"] == 921
    with pytest.raises(Refused):   # the budget is too small for +500
        nightly.check_sizes(night, fake_gh(["Nightly size budget: +0.0001 MB"]),
                            sized_build(tmp_path / "b3", 1400), z, OLD, runner=env.runner)


def test_growth_with_an_unknown_previous_commit_refuses(night, env, fake_gh, tmp_path):
    z = baseline_zip(tmp_path / "prev.zip")
    with pytest.raises(Refused) as e:
        nightly.check_sizes(night, fake_gh(), sized_build(tmp_path / "big", 1400), z, None, runner=env.runner)
    assert e.value.gate == "sizes"


def test_a_truncated_pr_list_refuses(night, env, fake_gh, tmp_path):
    env.runner.add(["git", "-C", str(night.cfg.clone), "rev-list"], out="c1\n")
    gh = fake_gh(error=Refused("sizes", "500 merged PRs, the list may be truncated"))
    with pytest.raises(Refused) as e:
        nightly.check_sizes(night, gh, sized_build(tmp_path / "big", 1400), baseline_zip(tmp_path / "p.zip"), OLD,
                            runner=env.runner)
    assert e.value.gate == "sizes" and "truncated" in e.value.detail


def boot_text(ms):
    return f"console: BOOT phase=screens ready at={ms} ms dt=40 ms\nconsole: GAME OK 1948 turn 0 hash ab\nPASS: saw GAME OK\n"


def test_boot_takes_the_exclusive_lock_from_the_main_checkout_three_times(night, env):
    times = iter([8100, 8300, 8200])

    class BootRunner(CheckRunner):
        def run(self, cmd, **k):
            self.ok = boot_text(next(times))
            return super().run(cmd, **k)

    r = BootRunner()
    build = night.cfg.clone / "unity/Builds/Web-gzip"
    assert nightly.measure_boot(night, r, build) == 8200.0
    clone = night.cfg.clone
    assert len(r.calls) == 3 and set(r.cwds) == {night.cfg.main_repo}
    assert r.calls[0] == ["uv", "run", str(clone / "tools/machine_lock.py"), "--exclusive", "--wait-unity",
                          "--timeout", "2700", "--", "uv", "run", str(clone / "tools/web_check.py"), str(build),
                          "--query", "bootlog", "--expect", "GAME OK", "--timeout", "300", "--stall", "90"]


@pytest.mark.parametrize("answer,word", [((3, ""), "Unity"), ((124, ""), "timed out"), ((1, boot_text(1)), "exited 1"),
                                         ((0, "PASS: saw GAME OK\n"), "BOOT"), ((0, "TIMEOUT\n"), "pass")])
def test_a_bad_boot_run_refuses_with_gate_boot(night, env, answer, word):
    r = CheckRunner(answers={"--exclusive": answer})
    with pytest.raises(Refused) as e:
        nightly.measure_boot(night, r, night.cfg.clone / "unity/Builds/Web-gzip")
    assert e.value.gate == "boot" and word in e.value.detail
    assert len(r.calls) == 1


# --- Task 6: publish_night, the stage wiring, the clients ---------------------------------------------------------

BOOT_MS = 8200.0
SIZES = {"data": 1000, "wasm": 90, "framework": 10}


@pytest.fixture
def pub(night, env, tmp_path):
    """A night plus two zips, a git log answer and a fake demo check."""
    clone = str(night.cfg.clone)
    env.runner.add(["git", "-C", clone, "log", "--format=%s"], out="Fix a thing\nAdd a thing\n")
    zips = (tmp_path / "web.zip", tmp_path / "pages.zip")
    for z in zips:
        z.write_bytes(b"zip")
    return zips


@pytest.fixture
def served(monkeypatch):
    calls = []
    monkeypatch.setattr(nightly, "demo_serves", lambda url, version, *, deadline: calls.append((url, version)))
    return calls


def last_published():
    return ns.Published("v0.3.0-nightly.20261027", OLD, "0.3.0-nightly.20261027", {}, None)


PREVIOUS = ("v0.3.0-nightly.20261027", OLD)


def publish(night, state, gh, zips, env, itch=None, save=lambda: None, previous=PREVIOUS):
    nightly.publish_night(night, state, gh, itch, zips, SIZES, BOOT_MS, previous=previous, save=save,
                          runner=env.runner)


def test_release_targets_the_preflight_commit(night, env, pub, served):
    clone = str(night.cfg.clone)
    gh, st = FakeGH(), ns.State(last_published=last_published())
    publish(night, st, gh, pub, env)   # origin/main may move on after preflight: nothing may follow it
    create = next(c for c in gh.calls if c[0] == "create_draft")
    assert create[1:3] == (TAG, night.sha) and create[4] == list(pub)
    assert ["git", "-C", clone, "log", "--format=%s", f"{OLD}..{night.sha}"] in env.runner.calls
    assert not any("ls-remote" in c and "refs/heads/main" in c for c in env.runner.calls)
    notes = (night.run_dir / "release-notes.md").read_text()
    assert "- Fix a thing" in notes and "- Add a thing" in notes and night.sha[:12] in notes
    assert gh.names() == ["ensure_tag_available", "create_draft", "verify_draft", "latest_tag", "publish", "dispatch_site"]
    assert gh.latest == TAG and "restore_latest" not in gh.names()
    assert (night.run_dir / "latest-before-publish.txt").read_text() == "v0.3.1\n"


def test_the_first_night_fetches_the_release_tag_before_resolving_it(night, env):
    clone = str(night.cfg.clone)
    env.runner.add(["git", "-C", clone, "fetch"])
    env.runner.add(["git", "-C", clone, "rev-list", "-n", "1", "refs/tags/v0.3.1"], out=OLD + "\n")
    gh = FakeGH()
    assert nightly.previous_build(night, ns.State(), gh, env.runner) == ("v0.3.1", OLD)
    fetch = ["git", "-C", clone, "fetch", "--no-tags", "origin", "refs/tags/v0.3.1:refs/tags/v0.3.1"]
    assert env.runner.calls.index(fetch) < [c[3] for c in env.runner.calls].index("rev-list")


def test_a_failed_tag_fetch_on_the_first_night_refuses(night, env):
    env.runner.add(["git", "-C", str(night.cfg.clone), "fetch"], code=128)
    with pytest.raises(ns.Refused):
        nightly.previous_build(night, ns.State(), FakeGH(), env.runner)


def test_later_nights_use_the_published_commit_and_fetch_nothing(night, env):
    assert nightly.previous_build(night, ns.State(last_published=last_published()), FakeGH(), env.runner) == \
        ("v0.3.0-nightly.20261027", OLD)
    assert not any("fetch" in c for c in env.runner.calls)


def test_the_notes_log_runs_from_the_previous_commit_or_tag(night, env, pub, served):
    clone = str(night.cfg.clone)
    publish(night, ns.State(), FakeGH(), pub, env, previous=("v0.3.1", None))
    assert ["git", "-C", clone, "log", "--format=%s", f"v0.3.1..{night.sha}"] in env.runner.calls


def test_a_failed_draft_check_deletes_the_draft_and_refuses(night, env, pub, served):
    gh = FakeGH(fail={"verify_draft": ns.Refused("github-draft", "hash differs")})
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "github-draft" and "hash differs" in e.value.detail
    assert ("delete_draft", TAG) in gh.calls and "publish" not in gh.names() and "dispatch_site" not in gh.names()
    assert st.open_draft is None and st.last_published.sha == OLD and served == []


@pytest.mark.parametrize("open_draft", [None, TAG, "v0.3.0"])
def test_a_release_made_after_preflight_refuses_before_create_and_is_never_deleted(night, env, pub, served, open_draft):
    gh = FakeGH("draft")
    st = ns.State(last_published=last_published(), open_draft=open_draft)
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "tag-exists"
    assert gh.names() == ["ensure_tag_available"] and gh.release == "draft"
    assert st.open_draft == open_draft and st.last_published.sha == OLD


def test_a_create_that_fails_after_leaving_a_draft_cleans_that_draft(night, env, pub, served):
    class HalfMade(FakeGH):
        def create_draft(self, tag, sha, notes, assets):
            self.calls.append(("create_draft", tag, sha, notes, list(assets)))
            self.release = "draft"   # gh made the release, then an asset upload failed
            raise ns.Refused("github-draft", "upload timed out")
    gh = HalfMade()
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused, match="upload timed out"):
        publish(night, st, gh, pub, env)
    assert [c for c in gh.calls if c[0] == "delete_draft"] == [("delete_draft", TAG)]
    assert gh.release is None and st.open_draft is None and st.last_published.sha == OLD


def test_rollback_after_create_deletes_only_the_created_tag(night, env, pub, served):
    gh = FakeGH(fail={"verify_draft": ns.Refused("github-draft", "hash differs")})
    st = ns.State(open_draft="v0.3.0", last_published=last_published())
    with pytest.raises(ns.Refused):
        publish(night, st, gh, pub, env)
    assert [c for c in gh.calls if c[0] == "delete_draft"] == [("delete_draft", TAG)]
    assert st.open_draft is None


def test_a_non_refusal_error_in_the_draft_check_still_deletes_the_draft(night, env, pub, served):
    gh = FakeGH(fail={"verify_draft": OSError("disk")})
    with pytest.raises(ns.Refused) as e:
        publish(night, ns.State(last_published=last_published()), gh, pub, env)
    assert e.value.gate == "github-draft" and "OSError" in e.value.detail and ("delete_draft", TAG) in gh.calls


def test_a_failed_cleanup_keeps_the_original_refusal_and_the_open_draft(night, env, pub, served, capsys):
    gh = FakeGH(fail={"verify_draft": ns.Refused("github-draft", "hash differs")})
    gh.delete_draft = lambda tag: (_ for _ in ()).throw(ns.Refused("github-delete", "offline"))
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "github-draft" and st.open_draft == TAG and "offline" in capsys.readouterr().err


def test_a_failed_publish_deletes_a_draft_that_is_still_one(night, env, pub, served):
    gh = FakeGH(fail={"publish": ns.Refused("github-publish", "tag at the wrong commit")})
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "github-publish" and ("delete_draft", TAG) in gh.calls
    assert st.last_published.sha == OLD and served == []


def test_a_failed_publish_check_on_a_public_release_is_never_deleted(night, env, pub, served):
    class Public(FakeGH):
        def publish(self, tag, sha):
            self.release = "published"
            self.latest = tag
            raise ns.Refused("github-publish", f"{tag} points at the wrong commit")
    gh = Public()
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "github-publish" and "by hand" in e.value.detail
    assert "delete_draft" not in gh.names() and st.open_draft == TAG
    assert gh.latest == "v0.3.1" and ("restore_latest", "v0.3.1", TAG) in gh.calls
    assert "restored previous Latest v0.3.1" in e.value.detail
    assert "v0.3.1" in (night.run_dir / "latest-rollback.log").read_text()


def test_a_newer_latest_published_meanwhile_is_never_overwritten_by_the_recovery(night, env, pub, served):
    err = ns.Refused("site", "run failed")
    gh = FakeGH(fail={"dispatch_site": [err, err]})
    real_dispatch = gh.dispatch_site

    def dispatch(deadline):
        gh.latest = "v0.4.0"   # the owner publishes a release while the site deploy runs
        real_dispatch(deadline)
    gh.dispatch_site = dispatch
    with pytest.raises(ns.Refused) as e:
        publish(night, ns.State(last_published=last_published()), gh, pub, env)
    assert e.value.gate == "site" and gh.latest == "v0.4.0"
    assert "Latest is now v0.4.0, not " + TAG + ": left alone" in e.value.detail


def test_a_site_failure_after_publish_retries_once_then_refuses_and_marks_site_pending(night, env, pub, served):
    err = ns.Refused("site", "run failed")
    gh = FakeGH(fail={"dispatch_site": [err, err]})
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "site" and "public" in e.value.detail and "unchanged" in e.value.detail
    assert gh.names().count("dispatch_site") == 2 and "delete_draft" not in gh.names()
    assert st.last_published.tag == TAG and st.last_published.sha == night.sha   # the release is public
    assert st.site_pending == night.version and st.open_draft is None
    assert gh.latest == "v0.3.1" and ("restore_latest", "v0.3.1", TAG) in gh.calls
    assert "restored previous Latest v0.3.1" in e.value.detail


def test_a_site_refusal_records_the_restored_latest_in_the_nightly_log(env, pub, served, monkeypatch):
    err = ns.Refused("site", "run failed")
    gh = FakeGH(fail={"dispatch_site": [err, err]})
    def stages(night, state, runner, client, itch, **kw):
        night.run_dir.mkdir(parents=True, exist_ok=True)
        publish(night, state, client, pub, env, save=kw["save"])
    monkeypatch.setattr(nightly, "run_stages", stages)
    assert env.night(ns.State(last_published=last_published()), gh) == "REFUSED"
    assert "REFUSED" in env.log() and "restored previous Latest v0.3.1" in env.log()


def test_no_previous_latest_is_explicit_in_the_refusal_and_log(night, env, pub, served):
    gh = FakeGH(fail={"publish": ns.Refused("github-publish", "wrong commit")})
    gh.latest = None
    with pytest.raises(ns.Refused) as e:
        publish(night, ns.State(), gh, pub, env)
    assert ("restore_latest", None, TAG) in gh.calls and gh.latest is None
    assert "no previous Latest" in e.value.detail
    assert "no previous Latest" in (night.run_dir / "latest-rollback.log").read_text()


def test_a_latest_recovery_failure_names_the_previous_latest(night, env, pub, served):
    gh = FakeGH(fail={"publish": ns.Refused("github-publish", "wrong commit"),
                      "restore_latest": OSError("offline")})
    with pytest.raises(ns.Refused) as e:
        publish(night, ns.State(), gh, pub, env)
    assert "wrong commit" in e.value.detail and "Latest recovery failed (previous Latest v0.3.1)" in e.value.detail
    assert "offline" in (night.run_dir / "latest-rollback.log").read_text()


def test_an_interrupt_after_publish_restores_latest_and_records_the_result(night, env, pub, served):
    gh = FakeGH(fail={"dispatch_site": KeyboardInterrupt()})
    with pytest.raises(KeyboardInterrupt):
        publish(night, ns.State(), gh, pub, env)
    assert gh.latest == "v0.3.1" and "delete_draft" not in gh.names()
    assert any("restored previous Latest v0.3.1" in note for note in night.notes)


def test_a_failed_latest_lookup_refuses_before_the_public_edit(night, env, pub, served):
    gh = FakeGH(fail={"latest_tag": ns.Refused("github-latest", "offline")})
    with pytest.raises(ns.Refused, match="offline"):
        publish(night, ns.State(), gh, pub, env)
    assert "publish" not in gh.names() and "restore_latest" not in gh.names()
    assert ("delete_draft", TAG) in gh.calls


def test_a_failed_save_after_publish_restores_latest(night, env, pub, served):
    gh = FakeGH()
    saves = []
    def save():
        saves.append(True)
        if len(saves) == 2:   # the save right after publish; _try_save swallows only OSError, so this one escapes
            raise TypeError("Object of type Path is not JSON serializable")
    with pytest.raises(ns.Refused, match="restored previous Latest v0.3.1"):
        publish(night, ns.State(), gh, pub, env, save=save)
    assert gh.latest == "v0.3.1" and "delete_draft" not in gh.names()


def test_a_disk_error_saving_state_after_publish_keeps_the_good_release_latest(night, env, pub, served, tmp_path,
                                                                              monkeypatch):
    monkeypatch.setattr(nightly, "notify", lambda *a: None)
    blocked = tmp_path / "state.json"
    blocked.mkdir()   # writing the state file fails with an OSError, which production's _try_save reports and absorbs
    gh, st = FakeGH(), ns.State(last_published=last_published())
    publish(night, st, gh, pub, env, save=lambda: nightly._try_save(blocked, st))
    assert gh.latest == TAG and "restore_latest" not in gh.names() and st.last_published.tag == TAG


@pytest.mark.parametrize("how", ["incomplete-read", "key-error"])
def test_an_unexpected_error_in_the_site_step_still_gets_the_second_attempt(night, env, pub, monkeypatch, how):
    import http.client
    import nightly_publish as gp
    def broken(url, timeout):
        raise http.client.IncompleteRead(b"x")
    monkeypatch.setattr(nightly, "demo_serves", lambda url, version, *, deadline: gp.demo_serves(
        url, version, deadline=deadline, fetch=broken, sleep=lambda s: None, now=iter(range(0, 10**9, 10**8)).__next__))
    fail = {"dispatch_site": [KeyError("databaseId"), KeyError("databaseId")]} if how == "key-error" else None
    gh, st = FakeGH(fail=fail), ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        publish(night, st, gh, pub, env)
    assert e.value.gate == "site" and gh.names().count("dispatch_site") == 2
    assert st.site_pending == night.version and st.last_published.tag == TAG


def test_site_pending_is_marked_before_the_site_step_and_saved_with_the_publish(night, env, pub, monkeypatch):
    seen = []
    monkeypatch.setattr(nightly, "demo_serves", lambda *a, **k: None)
    gh, st = FakeGH(), ns.State(last_published=last_published())
    gh.dispatch_site = lambda deadline: seen.append(st.site_pending)
    publish(night, st, gh, pub, env)
    assert seen == [night.version] and st.site_pending is None


class Crash(BaseException):
    pass


def test_state_is_saved_right_after_create_draft_and_right_after_publish(night, env, pub, served, tmp_path):
    path = tmp_path / "state.json"
    saves = []

    def save(crash_on):
        def f():
            ns.save_state(path, st)
            saves.append((st.open_draft, st.last_published.tag))
            if len(saves) == crash_on:
                raise Crash()
        return f
    st = ns.State(last_published=last_published())
    with pytest.raises(Crash):
        publish(night, st, FakeGH(), pub, env, save=save(1))   # killed right after create_draft
    assert ns.load_state(path).open_draft == TAG and saves[0] == (TAG, last_published().tag)
    saves.clear()
    st = ns.State(last_published=last_published())
    with pytest.raises(Crash):
        publish(night, st, FakeGH(), pub, env, save=save(2))   # killed right after publish
    disk = ns.load_state(path)
    assert disk.last_published.tag == TAG and disk.open_draft is None and disk.last_published.sha == night.sha


def test_a_failing_checkpoint_save_does_not_stop_the_night(env, monkeypatch, capsys):
    monkeypatch.setattr(nightly, "save_state", lambda p, st: (_ for _ in ()).throw(OSError("full")))
    seen = {}
    def stages(night, state, runner, gh, itch, *, no_publish, free_gb, save):
        save()
        seen["ok"] = True
        return True
    monkeypatch.setattr(nightly, "run_stages", stages)
    assert env.night() == "PUBLISHED" and seen and "could not save state" in capsys.readouterr().err


def test_a_failed_checkpoint_save_notifies_so_it_is_never_silent(env, monkeypatch):
    monkeypatch.setattr(nightly, "save_state", lambda p, st: (_ for _ in ()).throw(OSError("full")))
    monkeypatch.setattr(nightly, "run_stages", lambda night, state, runner, gh, itch, *, no_publish, free_gb, save:
                        save() or True)
    assert env.night() == "PUBLISHED"
    assert any("not saved" in a[0].lower() and "full" in a[1] for a in env_notes)


def test_a_failing_demo_check_is_retried_once_and_a_second_success_clears_pending(night, env, pub, monkeypatch):
    results = [ns.Refused("site", "old page"), None]

    def demo(url, version, *, deadline):
        r = results.pop(0)
        if r:
            raise r
    monkeypatch.setattr(nightly, "demo_serves", demo)
    gh, st = FakeGH(), ns.State(last_published=last_published(), site_pending="0.2.9")
    publish(night, st, gh, pub, env)
    assert gh.names().count("dispatch_site") == 2 and st.site_pending is None


def test_a_published_night_updates_state_and_boot_baseline(night, env, pub, served):
    gh, st = FakeGH(), ns.State(last_published=last_published(), boot_baseline=[8000.0, 8100.0], open_draft="x")
    publish(night, st, gh, pub, env)
    assert st.last_published == ns.Published(TAG, night.sha, night.version, SIZES, BOOT_MS)
    assert st.boot_baseline == [8000.0, 8100.0, BOOT_MS] and st.open_draft is None and st.site_pending is None
    assert served == [(night.cfg.site_demo_url, night.version)]
    full = ns.State(last_published=last_published(), boot_baseline=[1.0, 2.0, 3.0, 4.0, 5.0])
    publish(night, full, FakeGH(), pub, env)
    assert full.boot_baseline == [1.0, 2.0, 3.0, 4.0, 5.0]


def test_itch_is_skipped_with_a_logged_reason_when_there_is_no_client(night, env, pub, served, capsys):
    publish(night, ns.State(last_published=last_published()), FakeGH(), pub, env)
    assert "itch: skipped (save test not passed)" in capsys.readouterr().out


# --- Task 7: itch.io, behind itch_enabled ---------------------------------------------------------------------------------

PREV_TAG = "v0.3.0-nightly.20261027"
PREV_VERSION = "0.3.0-nightly.20261027"


class FakeItch:
    """The itch client as the night uses it; every call lands in `log`, which tests share with FakeGH.calls."""

    def __init__(self, log, *, fail=None):
        self.log, self.fail = log, fail or {}

    def _call(self, name, *args):
        self.log.append((f"itch.{name}", *args))
        err = self.fail.get(name)
        if isinstance(err, list):
            err = err.pop(0) if err else None
        if err is not None:
            raise err

    def push(self, folder, version):
        self._call("push", Path(folder).name, version)

    def wait_live(self, version, deadline):
        self._call("wait_live", version)

    def matches(self, folder, dest):
        self._call("matches", Path(folder).name)


def zip_of(path, name):
    with zipfile.ZipFile(path, "w") as z:
        z.writestr("index.html", name)
    return path


@pytest.fixture
def itch_night(night, env, tmp_path, served):
    """An itch-enabled night with real zips for tonight and the previous build."""
    night.cfg = dataclasses.replace(night.cfg, itch_enabled=True)
    clone = str(night.cfg.clone)
    env.runner.add(["git", "-C", clone, "log", "--format=%s"], out="Fix a thing\n")
    zips = (zip_of(tmp_path / "web.zip", "tonight"), zip_of(tmp_path / "pages.zip", "pages"))
    gh = FakeGH()
    gh.baseline = zip_of(tmp_path / "previous-web.zip", "previous")
    return night, zips, gh, FakeItch(gh.calls)


def run_itch(itch_night, env, itch="fake", st=None):
    night, zips, gh, fake = itch_night
    st = st or ns.State(last_published=last_published())
    publish(night, st, gh, zips, env, itch=fake if itch == "fake" else itch)
    return gh, st


def refused_itch(itch_night, env, itch="fake"):
    st = ns.State(last_published=last_published())
    with pytest.raises(ns.Refused) as e:
        run_itch(itch_night, env, itch, st)
    return e.value, itch_night[2], st


def test_itch_disabled_is_logged_and_never_calls_the_client(night, env, pub, served, capsys):
    gh = FakeGH()
    publish(night, ns.State(last_published=last_published()), gh, pub, env, itch=FakeItch(gh.calls))
    assert "itch: skipped (save test not passed)" in capsys.readouterr().out
    assert gh.names() == ["ensure_tag_available", "create_draft", "verify_draft", "latest_tag", "publish", "dispatch_site"]


def test_itch_enabled_without_a_client_refuses_before_any_store_change(itch_night, env):
    err, gh, st = refused_itch(itch_night, env, itch=None)
    assert err.gate == "itch" and "create_draft" not in gh.names() and st.open_draft is None


def test_the_itch_push_sits_between_verify_and_publish_and_the_rollback_is_ready_first(itch_night, env):
    gh, st = run_itch(itch_night, env)
    version = itch_night[0].version
    assert gh.calls[3][:3] == ("download", PREV_TAG, "*-web.zip")   # after the tag check, create_draft, verify_draft, before the push
    assert [c[0] for c in gh.calls if c[0] not in ("release_state",)] == [
        "ensure_tag_available", "create_draft", "verify_draft", "download", "itch.push", "itch.wait_live", "itch.matches", "latest_tag", "publish",
        "dispatch_site"]
    assert ("itch.push", "itch-push", version) in gh.calls and st.last_published.tag == TAG
    night = itch_night[0]
    assert (night.run_dir / "itch-push/index.html").read_text() == "tonight"
    assert (night.run_dir / "itch-rollback/index.html").read_text() == "previous"


@pytest.mark.parametrize("how", ["download", "bad-zip"])
def test_an_unusable_rollback_target_refuses_before_any_push(itch_night, env, how):
    night, zips, gh, fake = itch_night
    if how == "download":
        gh.fail["download"] = ns.Refused("github-download", "0 files matching")
    else:
        gh.baseline.write_bytes(b"not a zip")
    err, gh, st = refused_itch(itch_night, env)
    assert err.gate == "itch-rollback"
    assert not [c for c in gh.calls if c[0].startswith("itch.")] and "publish" not in gh.names()
    assert ("delete_draft", TAG) in gh.calls and st.open_draft is None


@pytest.mark.parametrize("step", ["push", "wait_live", "matches"])
def test_an_itch_failure_rolls_back_and_deletes_the_draft(itch_night, env, step):
    night, zips, gh, fake = itch_night
    fake.fail[step] = [ns.Refused("itch", f"{step} failed")]
    err, gh, st = refused_itch(itch_night, env)
    assert err.gate == "itch" and f"{step} failed" in err.detail
    itch_calls = [c for c in gh.calls if c[0].startswith("itch.")]
    assert ("itch.push", "itch-rollback", PREV_VERSION) in itch_calls and ("itch.wait_live", PREV_VERSION) in itch_calls
    names = [n for n in gh.names() if n != "release_state"]
    assert names[-2:] == ["itch.wait_live", "delete_draft"]
    assert "publish" not in names and "dispatch_site" not in names
    assert st.open_draft is None and st.last_published.sha == OLD


def test_a_failed_rollback_is_named_in_the_refusal_and_notified(itch_night, env):
    night, zips, gh, fake = itch_night
    fake.fail["wait_live"] = [ns.Refused("itch", "never live"), None]
    fake.fail["push"] = [None, ns.Refused("itch", "butler push exited 1")]
    err, gh, st = refused_itch(itch_night, env)
    assert err.gate == "itch" and "never live" in err.detail
    assert "rollback" in err.detail and "butler push exited 1" in err.detail and "by hand" in err.detail
    assert ("delete_draft", TAG) in gh.calls   # the draft is still cleaned up
    assert any("itch" in a[0].lower() and "rollback" in a[1].lower() for a in env_notes)


def test_a_github_publish_failure_after_itch_rolls_itch_back(itch_night, env):
    night, zips, gh, fake = itch_night
    gh.fail["publish"] = ns.Refused("github-publish", "tag at the wrong commit")
    err, gh, st = refused_itch(itch_night, env)
    assert err.gate == "github-publish"
    assert ("itch.push", "itch-rollback", PREV_VERSION) in gh.calls and ("itch.wait_live", PREV_VERSION) in gh.calls
    assert ("delete_draft", TAG) in gh.calls


def test_a_failed_rollback_after_a_publish_failure_is_named_and_notified(itch_night, env):
    night, zips, gh, fake = itch_night
    gh.fail["publish"] = ns.Refused("github-publish", "tag at the wrong commit")
    fake.fail["push"] = [None, ns.Refused("itch", "butler push exited 1")]
    err, gh, st = refused_itch(itch_night, env)
    assert err.gate == "github-publish" and "tag at the wrong commit" in err.detail
    assert "rollback" in err.detail and "butler push exited 1" in err.detail
    assert any("rollback" in a[1].lower() for a in env_notes)


def test_an_interrupt_during_publish_rolls_itch_back_and_cleans_the_draft(itch_night, env):
    night, zips, gh, fake = itch_night
    gh.fail["publish"] = KeyboardInterrupt()
    st = ns.State(last_published=last_published())
    with pytest.raises(KeyboardInterrupt):
        run_itch(itch_night, env, st=st)
    assert ("itch.push", "itch-rollback", PREV_VERSION) in gh.calls and ("itch.wait_live", PREV_VERSION) in gh.calls
    assert ("delete_draft", TAG) in gh.calls and st.open_draft is None


def test_the_itch_skip_reason_is_noted_on_the_night(night, env, pub, served):
    publish(night, ns.State(last_published=last_published()), FakeGH(), pub, env)
    assert night.notes == ["itch: skipped (save test not passed)"]


def test_the_night_notes_reach_the_log_line(env, monkeypatch):
    def stages(night, state, runner, gh, itch, *, no_publish, free_gb, save):
        night.notes.append("itch: skipped (save test not passed)")
        return True
    monkeypatch.setattr(nightly, "run_stages", stages)
    assert env.night() == "PUBLISHED"
    assert "PUBLISHED" in env.log() and "itch: skipped (save test not passed)" in env.log()


def test_a_site_failure_after_publish_does_not_roll_itch_back(itch_night, env):
    night, zips, gh, fake = itch_night
    err_site = ns.Refused("site", "run failed")
    gh.fail["dispatch_site"] = [err_site, err_site]
    err, gh, st = refused_itch(itch_night, env)
    assert err.gate == "site"
    assert [c[0] for c in gh.calls if c[0].startswith("itch.")] == ["itch.push", "itch.wait_live", "itch.matches"]
    assert "delete_draft" not in gh.names()


# --- the stage wiring ---------------------------------------------------------------------------------------------------

@pytest.fixture
def stages(night, env, tmp_path, monkeypatch):
    """Every stage replaced by a recorder, so the order and the arguments are what is tested."""
    order = []
    gz, pages = tmp_path / "Web-gzip", tmp_path / "Web-pages"
    zips = {False: tmp_path / "web.zip", True: tmp_path / "pages.zip"}
    prev = tmp_path / "prev.zip"

    def rec(name, result=None):
        def f(*a, **k):
            order.append((name, a, k))
            return result(a, k) if callable(result) else result
        monkeypatch.setattr(nightly, name, f)

    rec("run_gates")
    rec("run_editmode")
    rec("build_web", lambda a, k: pages if k["pages"] else gz)
    rec("assert_clean_tree")
    rec("package_zip", lambda a, k: zips[a[3]])
    rec("run_web_checks")
    rec("check_sizes", SIZES)
    rec("measure_boot", BOOT_MS)
    rec("publish_night")
    gh = FakeGH()
    gh.baseline = prev
    return type("S", (), dict(order=order, gz=gz, pages=pages, zips=zips, prev=prev, gh=gh,
                              names=lambda self: [o[0] for o in order]))()


def test_the_stages_run_in_the_brief_order(night, env, stages, capsys):
    st = ns.State(last_published=last_published())
    assert nightly.run_stages(night, st, env.runner, stages.gh, None, no_publish=False, free_gb=lambda p: 100.0)
    assert stages.names() == ["run_gates", "run_editmode", "build_web", "build_web", "assert_clean_tree",
                              "package_zip", "package_zip", "run_web_checks", "run_web_checks", "check_sizes",
                              "measure_boot", "publish_night"]
    builds = [o for o in stages.order if o[0] == "build_web"]
    assert [b[2]["pages"] for b in builds] == [False, True]
    checks = [o for o in stages.order if o[0] == "run_web_checks"]
    assert [c[1][2] for c in checks] == [stages.gz, stages.pages]   # web checks on BOTH builds
    sizes = next(o for o in stages.order if o[0] == "check_sizes")
    assert sizes[1][2:5] == (stages.gz, stages.prev, OLD) and sizes[2]["runner"] is env.runner
    assert stages.gh.calls[-1][:3] == ("download", "v0.3.0-nightly.20261027", "*-web.zip")
    pub_call = stages.order[-1]
    assert pub_call[1][4] == (stages.zips[False], stages.zips[True]) and pub_call[1][5:7] == (SIZES, BOOT_MS)
    assert pub_call[2]["previous"] == ("v0.3.0-nightly.20261027", OLD) and callable(pub_call[2]["save"])
    assert stages.gh.names().count("previous_build_tag") == 1   # once a night


def test_no_publish_stops_before_any_store_call(night, env, stages, capsys):
    st = ns.State(last_published=last_published())
    assert nightly.run_stages(night, st, env.runner, stages.gh, None, no_publish=True,
                              free_gb=lambda p: 100.0) is False
    assert "publish_night" not in stages.names() and f"NO-PUBLISH: all gates passed for {TAG}\n" in \
        capsys.readouterr().out
    assert stages.gh.names() == ["previous_build_tag", "download"]   # reads only


def test_a_no_publish_night_touches_no_record_and_no_state(env, monkeypatch, capsys):
    monkeypatch.setattr(nightly, "run_stages", lambda *a, **k: (print(f"NO-PUBLISH: all gates passed for {TAG}")
                                                                or False))
    assert env.night(no_publish=True) == "NO-PUBLISH"
    assert env.log() == "" and not env.state_path.exists() and not env.cfg.handoff.exists()
    assert f"NO-PUBLISH: all gates passed for {TAG}" in capsys.readouterr().out and env_notes == []


def test_a_published_night_is_recorded_as_published(env, monkeypatch):
    monkeypatch.setattr(nightly, "run_stages", lambda *a, **k: True)
    assert env.night() == "PUBLISHED"
    assert "PUBLISHED" in env.log() and f"tag={TAG}" in env.log()


def test_a_boot_slowdown_refuses_and_a_collecting_baseline_passes(night, env, stages):
    st = ns.State(last_published=last_published(), boot_baseline=[8000.0, 8100.0, 8050.0, 8100.0, 8000.0])
    nightly.run_stages(night, st, env.runner, stages.gh, None, no_publish=True, free_gb=lambda p: 100.0)   # 8200 <= limit
    st.boot_baseline = [1000.0, 1000.0, 1000.0, 1000.0, 1000.0]
    with pytest.raises(ns.Refused) as e:
        nightly.run_stages(night, st, env.runner, stages.gh, None, no_publish=True, free_gb=lambda p: 100.0)
    assert e.value.gate == "boot" and "publish_night" not in stages.names()


def test_a_failed_build_still_checks_the_tree_before_the_refusal(night, env, stages, monkeypatch):
    def boom(n, r, *, pages, free_gb):
        if pages:
            raise ns.Refused("build-web", "the Unity build exited 1")
        return stages.gz
    monkeypatch.setattr(nightly, "build_web", boom)
    with pytest.raises(ns.Refused) as e:
        nightly.run_stages(night, ns.State(last_published=last_published()), env.runner, stages.gh, None,
                           no_publish=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "build-web" and "assert_clean_tree" in stages.names()
    assert "package_zip" not in stages.names() and "publish_night" not in stages.names()


def test_a_dirty_tree_after_a_failed_build_is_noted_not_substituted(night, env, stages, monkeypatch, capsys):
    monkeypatch.setattr(nightly, "build_web", lambda *a, **k: (_ for _ in ()).throw(ns.Refused("build-web", "x")))
    def dirty(n, r):
        raise ns.Refused("dirty-tree", "settings still changed")
    monkeypatch.setattr(nightly, "assert_clean_tree", dirty)
    with pytest.raises(ns.Refused) as e:
        nightly.run_stages(night, ns.State(last_published=last_published()), env.runner, stages.gh, None,
                           no_publish=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "build-web"
    assert "settings still changed" in (night.run_dir / "dirty-tree.note").read_text()
    assert "not clean" in capsys.readouterr().err


def test_a_dirty_tree_after_good_builds_refuses(night, env, stages, monkeypatch):
    def dirty(n, r):
        raise ns.Refused("dirty-tree", "changed")
    monkeypatch.setattr(nightly, "assert_clean_tree", dirty)
    with pytest.raises(ns.Refused) as e:
        nightly.run_stages(night, ns.State(last_published=last_published()), env.runner, stages.gh, None,
                           no_publish=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "dirty-tree" and "package_zip" not in stages.names()


class LoggingWebRunner(WebRunner):
    def run(self, cmd, **k):
        if "-executeMethod" in cmd and "-logFile" in cmd:
            Path(cmd[cmd.index("-logFile") + 1]).write_text(
                "boring\nAssets/X.cs(3,4): error CS0103: The name 'y' does not exist\nmore\nBuildFailedException: nope\n")
        return super().run(cmd, **k)


def test_a_failed_build_names_the_unity_errors_in_its_refusal(night, env):
    r = LoggingWebRunner(night, build_code=1)
    with pytest.raises(ns.Refused) as e:
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
    assert e.value.gate == "build-web" and "error CS0103" in e.value.detail and "BuildFailedException" in e.value.detail
    assert "boring" not in e.value.detail


def test_a_failed_build_without_a_unity_log_says_so(night, env):
    r = WebRunner(night, build_code=1)
    with pytest.raises(ns.Refused) as e:
        nightly.build_web(night, r, pages=False, free_gb=lambda p: 100.0)
    assert "no Unity log" in e.value.detail


# --- the clients ----------------------------------------------------------------------------------------------------------

def test_clients_are_a_github_for_the_configured_repo_and_no_itch(env, monkeypatch):
    monkeypatch.setattr(nightly.shutil, "which", lambda name: "/usr/bin/gh")
    gh, itch = nightly._clients(env.cfg)
    assert isinstance(gh, nightly.GitHub) and gh.slug == env.cfg.repo_slug and gh.cwd == env.cfg.clone and itch is None


def test_an_itch_client_exists_only_when_itch_is_enabled(env, monkeypatch, tmp_path):
    monkeypatch.setattr(nightly.shutil, "which", lambda name: "/usr/bin/gh")
    butler = tmp_path / "butler"
    butler.write_text("x")
    cfg = dataclasses.replace(env.cfg, itch_enabled=True, butler=butler)
    gh, itch = nightly._clients(cfg)
    assert isinstance(itch, nightly.Itch) and itch.target == cfg.itch_target and itch.butler == butler


def test_enabled_itch_without_butler_is_a_recorded_refusal(env, monkeypatch, tmp_path):
    monkeypatch.setattr(nightly.shutil, "which", lambda name: "/usr/bin/gh")
    with pytest.raises(ns.Refused) as e:
        nightly._clients(dataclasses.replace(env.cfg, itch_enabled=True, butler=tmp_path / "missing"))
    assert e.value.gate == "itch-client"


def test_a_missing_gh_is_a_github_client_refusal(env, monkeypatch):
    monkeypatch.setattr(nightly.shutil, "which", lambda name: None)
    with pytest.raises(ns.Refused) as e:
        nightly._clients(env.cfg)
    assert e.value.gate == "github-client"


def test_an_unusable_client_is_a_recorded_github_client_refusal_not_internal(env):
    err = ns.Refused("github-client", "gh is not on PATH")
    out = nightly.run_night(env.cfg, env.state_path, env.job, env.runner, None, None, now=NOW, manual=False,
                            no_publish=False, client_error=err, free_gb=lambda p: 100.0)
    assert out == "REFUSED" and "gate=github-client" in env.log() and "gh is not on PATH" in env.log()
    assert env.runner.calls == [] and len(env_notes) == 1


# --- Task 8: the two-upload itch save test ----------------------------------------------------------------------------

PAGE_URL = "https://example.invalid/statecraft-israel"
ITCH_HOST = "https://html-classic.itch.zone"
SAVED = "PERSISTENCE saved 1948-05-16 turn 1 hash ab12\n"


class SaveItch:
    """The itch client of the save test: each push moves the live build id on by one."""

    def __init__(self, calls, *, fail=None):
        self.calls, self.fail, self.pushes, self.build = calls, fail or {}, 0, 100

    def push(self, folder, version):
        self.calls.append(("itch.push", Path(folder).name, version))
        if "push" in self.fail:
            raise self.fail["push"]
        self.pushes += 1
        self.build += 1

    def wait_live(self, version, deadline):
        self.calls.append(("itch.wait_live", version))
        if "wait_live" in self.fail:
            raise self.fail["wait_live"]

    def live_build_id(self):
        return self.build


class SaveRunner(FakeRunner):
    """Answers the web_check runs: the first-save run prints `saved`, a --expect run answers `loads` (one per call)."""

    def __init__(self, *, saved=(0, "PASS: saw x\n" + SAVED), loads=((0, "PASS: saw GAME OK\n"),) * 2):
        super().__init__()
        self.saved, self.loads, self.runs = saved, list(loads), []

    def run(self, cmd, *, cwd, log, deadline, env=None, holder=None):
        self.runs.append(list(cmd))
        code, text = self.saved if "--first-save" in cmd else self.loads.pop(0)
        Path(log).parent.mkdir(parents=True, exist_ok=True)
        Path(log).write_text(text)
        return code


class Page:
    def __init__(self, text):
        self.text = text

    def __enter__(self):
        return self

    def __exit__(self, *a):
        return False

    def read(self):
        return self.text.encode()


def page_for(itch, *, same=False, frame=True):
    """A fetch answering the itch page: its frame follows the itch client's build, or never changes with `same`."""
    def fetch(url, timeout=30):
        assert url == PAGE_URL
        build = 101 if same else itch.build
        inner = f"{ITCH_HOST}/html/19840947-{build}/index.html?v=1" if frame else "none"
        return Page(f'<div data-iframe="&lt;iframe src=&quot;{inner}&quot;&gt;">')
    return fetch


class Saved:
    """A save test: a config file, a published release and the fakes."""

    def __init__(self, tmp_path, monkeypatch, *, release="published", page_url=PAGE_URL, **runner_kw):
        self.job = tmp_path / "job"
        self.job.mkdir()
        self.config = self.job / "config.json"
        self.config.write_text('{"itch_page_url": "%s"}\n' % page_url)
        self.cfg = ns.load_config(self.config)
        self.cfg = dataclasses.replace(self.cfg, handoff=tmp_path / "HANDOFF.md")
        self.gh = FakeGH(release=release)
        self.gh.baseline = zip_of(tmp_path / "tonight-web.zip", "tonight")
        self.itch = SaveItch(self.gh.calls)
        self.runner = SaveRunner(**runner_kw)
        monkeypatch.setattr(nightly, "find_chrome", lambda: Path("/fake/chrome"))

    def run(self, fetch=None, tag=TAG):
        return nightly.savetest(self.cfg, self.config, self.job, self.runner, self.gh, self.itch, tag=tag,
                                fetch=fetch or page_for(self.itch), sleep=lambda s: None)

    def evidence(self):
        return json.loads(next((self.job / "savetest").glob("*/evidence.json")).read_text())

    def enabled(self):
        return json.loads(self.config.read_text()).get("itch_enabled", False)

    def log(self):
        return (self.job / "LOG.md").read_text()


@pytest.fixture
def saved(tmp_path, monkeypatch, no_processes):
    return lambda **kw: Saved(tmp_path, monkeypatch, **kw)


def test_savetest_passes_only_with_matching_tuple_new_url_same_origin(saved):
    t = saved()
    assert t.run() == "PASS"
    version = TAG[1:]
    assert [c for c in t.gh.calls if c[0].startswith("itch.")] == [
        ("itch.push", "folder", f"{version}-savetest-a"), ("itch.wait_live", f"{version}-savetest-a"),
        ("itch.push", "folder", f"{version}-savetest-b"), ("itch.wait_live", f"{version}-savetest-b")]
    first, b1, b2 = t.runner.runs
    profile = first[first.index("--profile") + 1]
    assert first[first.index("--url") + 1] == f"{ITCH_HOST}/html/19840947-101/index.html" and "--first-save" in first
    for run in (b1, b2):   # B twice, same frame URL, same kept profile, the full tuple
        assert run[run.index("--url") + 1] == f"{ITCH_HOST}/html/19840947-102/index.html"
        assert run[run.index("--profile") + 1] == profile
        assert run[run.index("--expect") + 1] == "GAME OK 1948-05-16 turn 1 hash ab12"
        assert run[:5] == ["uv", "run", str(nightly.TOOLS / "machine_lock.py"), "--shared", "--"]
    ev = t.evidence()
    assert ev["upload_a"] == {"build_id": 101, "frame_url": f"{ITCH_HOST}/html/19840947-101/index.html"}
    assert ev["upload_b"]["build_id"] == 102 and ev["tuple"] == {"date": "1948-05-16", "turn": 1, "hash": "ab12"}
    assert ev["chrome"] == "/fake/chrome" and len(ev["zip_sha256"]) == 64 and ev["result"] == "PASS"
    assert t.enabled() and json.loads(t.config.read_text())["itch_evidence"].endswith("evidence.json")
    assert "SAVETEST PASS" in t.log() and TAG in t.log()


def test_savetest_same_frame_url_is_inconclusive_and_keeps_itch_off(saved):
    t = saved()
    assert t.run(page_for(t.itch, same=True)) == "INCONCLUSIVE"
    assert not t.enabled() and len(t.runner.runs) == 1   # B is never loaded
    assert "same frame URL" in t.evidence()["detail"] and "SAVETEST INCONCLUSIVE" in t.log()


def test_savetest_another_frame_origin_is_inconclusive(saved, monkeypatch):
    t = saved()   # itch_frame_url only matches the one host today; the origin check guards a change of that
    hosts = iter(["html-classic.itch.zone", "html-classic.example.zone"])
    monkeypatch.setattr(nightly, "itch_frame_url",
                        lambda page: f"https://{next(hosts)}/html/1-{t.itch.build}/index.html")
    assert t.run() == "INCONCLUSIVE" and not t.enabled() and "origin changed" in t.evidence()["detail"]


def test_savetest_mismatch_fails_and_keeps_itch_off(saved):
    t = saved(loads=((1, "TIMEOUT: never saw GAME OK\n"),))
    assert t.run() == "FAIL" and not t.enabled() and "SAVETEST FAIL" in t.log()
    assert len(t.runner.runs) == 2   # A, then the first load of B: no second load after a failure


def test_savetest_second_load_failing_is_a_fail(saved):
    t = saved(loads=((0, "PASS: saw x\n"), (1, "TIMEOUT\n")))
    assert t.run() == "FAIL" and not t.enabled()


def test_savetest_a_timed_out_load_is_inconclusive(saved):
    t = saved(loads=((124, ""),))
    assert t.run() == "INCONCLUSIVE" and not t.enabled()


@pytest.mark.parametrize("saved_run", [(1, "FAIL: x\n"), (0, "PASS: saw x\n"), (0, "no pass\n" + SAVED)])
def test_savetest_without_a_durable_first_save_is_inconclusive(saved, saved_run):
    t = saved(saved=saved_run)
    assert t.run() == "INCONCLUSIVE" and not t.enabled() and len(t.runner.runs) == 1
    assert [c[0] for c in t.gh.calls].count("itch.push") == 1   # B is never pushed


@pytest.mark.parametrize("kw", [{"release": "draft"}, {"release": None}, {"page_url": ""}])
def test_savetest_needs_a_published_nightly_and_a_page_url(saved, kw):
    t = saved(**kw)
    assert t.run() == "INCONCLUSIVE" and not t.enabled()
    assert not any(c[0].startswith("itch.") or c[0] == "download" for c in t.gh.calls) and t.runner.runs == []
    assert "SAVETEST INCONCLUSIVE" in t.log()


def test_savetest_waits_while_itch_is_still_unpacking_the_upload(saved):
    t = saved()   # 28 Oct 2026: the page showed only a load_url for about 17 minutes before it named the frame
    framed, calls = page_for(t.itch), {"n": 0}

    def fetch(url, timeout=30):
        calls["n"] += 1
        if calls["n"] <= 30:   # 30 polls x 30 s = 15 minutes of {"status":"extracting"}
            return Page('<div class="html_embed_widget">Embed({"load_url":"https:\\/\\/x.itch.io\\/g\\/html_url\\/1"})')
        return framed(url, timeout)
    assert t.run(fetch) == "PASS" and t.enabled()


def test_savetest_a_page_without_a_frame_is_inconclusive(saved):
    t = saved()
    assert t.run(page_for(t.itch, frame=False)) == "INCONCLUSIVE" and t.runner.runs == [] and not t.enabled()


def test_savetest_waits_for_the_page_to_name_the_new_frame(saved):
    t = saved()
    seen = []
    base = page_for(t.itch)
    def fetch(url, timeout=30):   # the page lags: after upload B its first two answers still name A's frame
        seen.append(t.itch.pushes)
        stale = t.itch.pushes == 2 and seen.count(2) <= 2
        return base(url) if not stale else Page('&quot;%s/html/19840947-101/index.html&quot;' % ITCH_HOST)
    assert t.run(fetch) == "PASS" and seen.count(2) == 3


@pytest.mark.parametrize("step", ["push", "wait_live"])
def test_savetest_an_itch_failure_is_inconclusive_and_recorded(saved, step):
    t = saved()
    t.itch.fail[step] = ns.Refused("itch", f"{step} failed")
    assert t.run() == "INCONCLUSIVE" and not t.enabled() and f"itch: {step} failed" in t.log()


def test_savetest_an_unreadable_page_is_inconclusive(saved):
    t = saved()
    def fetch(url, timeout=30):
        raise OSError("offline")
    assert t.run(fetch) == "INCONCLUSIVE" and not t.enabled() and "offline" in t.log()


def test_savetest_without_chrome_pushes_nothing(saved, monkeypatch):
    t = saved()
    monkeypatch.setattr(nightly, "find_chrome", lambda: sys.exit("Chrome for Testing not found"))
    assert t.run() == "INCONCLUSIVE" and not any(c[0].startswith("itch.") for c in t.gh.calls)


def test_the_savetest_command_takes_a_tag_and_exits_zero_only_on_a_pass(tmp_path, monkeypatch, no_processes):
    monkeypatch.setattr(nightly, "JOB", tmp_path / "job")
    got = []
    for result, code in (("PASS", 0), ("FAIL", 1), ("INCONCLUSIVE", 1)):
        monkeypatch.setattr(nightly, "run_savetest", lambda cfg, job, tag, r=result: got.append(tag) or (0 if r == "PASS" else 1))
        assert nightly.main(["savetest", "--tag", TAG]) == code
    assert got == [TAG] * 3
    with pytest.raises(SystemExit):
        nightly.main(["savetest"])


def test_the_restore_also_resets_the_prickly_pear_material(night):
    r = FakeRunner()
    r.add(("git", "checkout", "--"))
    nightly.restore_settings(night, r)
    assert r.calls == [["git", "checkout", "--", "unity/ProjectSettings", "unity/Assets/Settings",
                        "unity/Assets/Art/HexTiles/Props/arab_village_prickly_pear/prickly_pear-rock.mat"]]
    assert (Path(__file__).resolve().parents[2] / r.calls[0][-1]).is_file()   # the path is a real tracked file


def test_a_corrupt_config_on_the_savetest_path_is_recorded_not_an_attribute_error(tmp_path, monkeypatch):
    job = tmp_path / "job"
    job.mkdir()
    (job / "config.json").write_text("{not json")
    monkeypatch.setattr(nightly, "JOB", job)
    monkeypatch.setattr(nightly, "Config", lambda: ns.Config(handoff=tmp_path / "HANDOFF.md"))
    monkeypatch.setattr(nightly, "notify", lambda *a, **k: None)
    assert nightly.main(["savetest", "--tag", "v0.3.1-nightly.20261028"]) == 1
    assert "gate=config" in (job / "LOG.md").read_text()
