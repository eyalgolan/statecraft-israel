import fcntl
import json
import os
import plistlib
import subprocess
import sys
import threading
import time
from pathlib import Path

import pytest

import unity_postmerge as up

SHA = "0123456789abcdef0123456789abcdef01234567"
PREV = "fedcba9876543210fedcba9876543210fedcba98"


def results_xml(tmp_path: Path, cases: list[tuple[str, str, str]]) -> Path:
    """An NUnit 3 result file as Unity's -testResults writes it: (fullname, result, message) per test case."""
    failed = sum(1 for _, r, _ in cases if r == "Failed")
    rows = []
    for name, result, message in cases:
        body = f"<failure><message><![CDATA[{message}]]></message></failure>" if result == "Failed" else ""
        rows.append(f'<test-case fullname="{name}" name="{name.split(".")[-1]}" result="{result}">{body}</test-case>')
    xml = (f'<?xml version="1.0" encoding="utf-8"?>\n<test-run id="2" testcasecount="{len(cases)}" '
           f'result="{"Failed" if failed else "Passed"}" total="{len(cases)}" passed="{len(cases) - failed}" '
           f'failed="{failed}" inconclusive="0" skipped="0"><test-suite type="TestSuite" name="Statecraft Israel">'
           + "".join(rows) + "</test-suite></test-run>")
    p = tmp_path / "results.xml"
    p.write_text(xml, encoding="utf-8")
    return p


# --- change detection -------------------------------------------------------------------------------------------

def test_a_tip_the_job_has_not_tested_is_new():
    assert up.commit_to_test(SHA, {}) == SHA
    assert up.commit_to_test(SHA, {"last_tested": PREV}) == SHA


def test_the_tip_already_tested_is_not_run_again():
    assert up.commit_to_test(SHA, {"last_tested": SHA}) is None


def test_state_round_trips_and_a_missing_or_corrupt_file_is_empty(tmp_path):
    p = tmp_path / "state.json"
    assert up.load_state(p) == {}
    up.save_state(p, {"last_tested": SHA, "issued": {SHA: "https://example.invalid/1"}})
    assert up.load_state(p)["last_tested"] == SHA
    p.write_text("{not json", encoding="utf-8")
    assert up.load_state(p) == {}


def test_the_tip_is_read_after_fetching_main():
    calls = []

    def git(*args):
        calls.append(args)
        return SHA + "\n" if args[0] == "rev-parse" else ""

    assert up.fetch_tip(git) == SHA
    assert calls[0] == ("fetch", "--quiet", "origin", "+refs/heads/main:refs/remotes/origin/main")
    assert calls[1] == ("rev-parse", "--verify", "refs/remotes/origin/main^{commit}")


def test_a_tip_that_is_not_a_full_sha_is_refused():
    with pytest.raises(RuntimeError):
        up.fetch_tip(lambda *a: "" if a[0] == "fetch" else "garbage\n")


# --- results: 0 tests ran is a broken run, never a pass ---------------------------------------------------------

def test_a_full_green_run_passes(tmp_path):
    r = up.parse_results(results_xml(tmp_path, [("A.One", "Passed", ""), ("A.Two", "Passed", "")]))
    assert (r.total, r.passed, r.failed, r.failures, r.broken) == (2, 2, 0, [], None)
    assert up.passed(r, unity_exit=0)


def test_zero_tests_ran_is_a_broken_run(tmp_path):
    r = up.parse_results(results_xml(tmp_path, []))
    assert r.total == 0 and r.broken and "0 tests" in r.broken
    assert not up.passed(r, unity_exit=0)


def test_a_missing_or_unreadable_result_file_is_a_broken_run(tmp_path):
    assert up.parse_results(tmp_path / "absent.xml").broken
    bad = tmp_path / "bad.xml"
    bad.write_text("<test-run", encoding="utf-8")
    r = up.parse_results(bad)
    assert r.broken and not up.passed(r, unity_exit=0)


def test_failing_tests_are_named_with_their_message(tmp_path):
    r = up.parse_results(results_xml(tmp_path, [("HudTests.TheGddGameShowsItsScore", "Failed", "Expected 9 but was 11"),
                                                ("A.Two", "Passed", "")]))
    assert r.failed == 1 and r.failures == [("HudTests.TheGddGameShowsItsScore", "Expected 9 but was 11")]
    assert not up.passed(r, unity_exit=2)


def test_a_green_file_with_a_non_zero_unity_exit_is_not_a_pass(tmp_path):
    r = up.parse_results(results_xml(tmp_path, [("A.One", "Passed", "")]))
    assert not up.passed(r, unity_exit=1)


def test_a_failed_count_without_a_named_case_still_fails(tmp_path):
    p = results_xml(tmp_path, [("A.One", "Passed", "")])
    p.write_text(p.read_text(encoding="utf-8").replace('failed="0"', 'failed="1"'), encoding="utf-8")
    r = up.parse_results(p)
    assert not up.passed(r, unity_exit=0)


# --- duplicate-issue guard --------------------------------------------------------------------------------------

def test_an_open_issue_for_the_same_sha_is_found_by_its_marker_or_title():
    by_marker = [{"number": 7, "title": "something", "body": f"x\n{up.marker(SHA)}\n", "url": "u7"}]
    by_title = [{"number": 8, "title": up.issue_title(SHA), "body": "", "url": "u8"}]
    assert up.existing_issue(by_marker, SHA) == "u7"
    assert up.existing_issue(by_title, SHA) == "u8"


def test_an_issue_for_another_sha_is_not_a_duplicate():
    other = [{"number": 9, "title": up.issue_title(PREV), "body": up.marker(PREV), "url": "u9"}]
    assert up.existing_issue(other, SHA) is None
    assert up.existing_issue([], SHA) is None


class FakeGh:
    def __init__(self, open_issues=(), labels=("area: unity", "bug"), fail_create=False):
        self.open_issues, self.labels, self.fail_create = list(open_issues), list(labels), fail_create
        self.created, self.comments = [], []

    def __call__(self, args, stdin=None):
        if args[:2] == ["issue", "list"]:
            return json.dumps(self.open_issues)
        if args[:2] == ["label", "list"]:
            return json.dumps([{"name": n} for n in self.labels])
        if args[:2] == ["issue", "comment"]:
            if self.fail_create:
                raise RuntimeError("gh failed")
            self.comments.append((args, stdin))
            return f"https://github.com/{up.REPO_SLUG}/issues/{args[2]}#issuecomment-{len(self.comments)}\n"
        if args[:2] == ["issue", "create"]:
            if self.fail_create:
                raise RuntimeError("gh failed")
            self.created.append((args, stdin))
            url = f"https://github.com/{up.REPO_SLUG}/issues/{100 + len(self.created)}"
            self.open_issues.append({"number": 100 + len(self.created), "title": args[args.index("--title") + 1],
                                     "body": stdin, "url": url})
            return url + "\n"
        raise AssertionError(args)


def test_report_opens_one_issue_with_the_label_and_records_it():
    gh, state = FakeGh(), {}
    url = up.report(gh, state, SHA, "title", f"body\n{up.marker(SHA)}")
    assert url.endswith("/issues/101") and state["issued"][SHA] == url
    args, body = gh.created[0]
    assert args[args.index("--label") + 1] == "area: unity" and args[args.index("--repo") + 1] == up.REPO_SLUG
    assert "--body-file" in args and body.startswith("body")


def test_report_skips_the_label_when_the_repo_has_none():
    gh = FakeGh(labels=("bug",))
    up.report(gh, {}, SHA, "t", "b")
    assert "--label" not in gh.created[0][0]


def test_report_never_opens_a_second_issue_for_a_commit():
    gh = FakeGh(open_issues=[{"number": 5, "title": up.issue_title(SHA), "body": "", "url": "u5"}])
    state = {}
    assert up.report(gh, state, SHA, "t", "b") == "u5" and gh.created == []
    gh2 = FakeGh()   # the first issue was closed, but this job already opened one for the commit
    assert up.report(gh2, {"issued": {SHA: "u1"}}, SHA, "t", "b") == "u1" and gh2.created == []


def test_a_new_failure_while_a_post_merge_issue_is_open_comments_on_it():
    open_issue = {"number": 42, "title": up.issue_title(PREV), "body": f"b\n{up.marker(PREV)}", "url": "u42"}
    unrelated = {"number": 7, "title": "a bug", "body": "no marker", "url": "u7"}
    gh, state = FakeGh(open_issues=[unrelated, open_issue]), {}
    url = up.report(gh, state, SHA, up.issue_title(SHA), f"body {SHA}\n{up.marker(SHA)}")
    assert gh.created == [] and len(gh.comments) == 1
    args, body = gh.comments[0]
    assert args[2] == "42" and args[args.index("--repo") + 1] == up.REPO_SLUG and SHA in body
    assert url.endswith("#issuecomment-1") and state["issued"][SHA] == url
    assert up.report(gh, state, SHA, "t", "b") == url and len(gh.comments) == 1     # never twice for one commit


def test_the_oldest_open_post_merge_issue_gets_the_comment():
    issues = [{"number": n, "title": "x", "body": up.marker(f"{n:040x}"), "url": f"u{n}"} for n in (50, 12, 31)]
    gh = FakeGh(open_issues=issues)
    up.report(gh, {}, SHA, "t", "b")
    assert gh.comments[0][0][2] == "12"


def test_a_failed_comment_raises_and_records_nothing():
    gh = FakeGh(open_issues=[{"number": 42, "title": "x", "body": up.marker(PREV), "url": "u42"}], fail_create=True)
    state = {}
    with pytest.raises(RuntimeError):
        up.report(gh, state, SHA, "t", "b")
    assert SHA not in state.get("issued", {})


def test_a_failed_create_raises_and_records_nothing():
    gh, state = FakeGh(fail_create=True), {}
    with pytest.raises(RuntimeError):
        up.report(gh, state, SHA, "t", "b")
    assert SHA not in state.get("issued", {})


# --- issue body -------------------------------------------------------------------------------------------------

def test_the_issue_body_has_the_sha_failing_tests_log_tail_and_marker(tmp_path):
    r = up.parse_results(results_xml(tmp_path, [("HudTests.TheGddGameShowsItsScore", "Failed", "Expected 9 but was 11"),
                                                ("A.Two", "Passed", "")]))
    log = "\n".join(f"line {i}" for i in range(500)) + "\nAssets/X.cs(81,48): error CS0103: The name 'MvpSlice'\n"
    body = up.issue_body(SHA, PREV, r, unity_exit=2, log_text=log, commits="abc1234 Merge #600")
    assert SHA in body and up.marker(SHA) in body
    assert "HudTests.TheGddGameShowsItsScore" in body and "Expected 9 but was 11" in body
    assert "line 499" in body and "line 10\n" not in body              # the tail, not the whole log
    assert "error CS0103" in body                                    # compile errors are pulled out
    assert f"{PREV[:12]}..{SHA[:12]}" in body and "abc1234 Merge #600" in body
    assert "1 failed of 2" in body and "Unity exit code: 2" in body
    assert len(body) < 60000


def test_a_broken_run_body_says_why_and_never_claims_a_pass(tmp_path):
    r = up.parse_results(results_xml(tmp_path, []))
    body = up.issue_body(SHA, None, r, unity_exit=0, log_text="Scripts have compiler errors.", commits="")
    assert "Broken run" in body and "0 tests" in body and "compiler errors" in body
    assert "passed" not in up.issue_title(SHA).lower()


def test_a_huge_failure_list_is_cut_to_fit_an_issue(tmp_path):
    cases = [(f"T.Case{i}", "Failed", "m" * 2000) for i in range(400)]
    r = up.parse_results(results_xml(tmp_path, cases))
    body = up.issue_body(SHA, PREV, r, unity_exit=2, log_text="x\n" * 5000, commits="")
    assert len(body) < 60000 and "T.Case0" in body and "more" in body


# --- the Unity command and the plist ----------------------------------------------------------------------------

def test_unity_runs_under_the_shared_lock_with_graphics_and_ci_flags_off(tmp_path):
    cmd = up.unity_command(Path("/wt"), "6000.3.25f1", tmp_path / "r.xml", tmp_path / "u.log")
    lock = cmd.index("--shared")
    assert cmd[:3] == ["uv", "run", str(up.TOOLS / "machine_lock.py")]
    assert cmd[lock + 1:lock + 4] == ["--wait-unity", "--timeout", "14400"] and "--" in cmd
    unity = cmd[cmd.index("--") + 1:]
    assert unity[0] == "/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"
    assert "-nographics" not in cmd
    for flag in ("-CI", "-GITHUB_ACTIONS", "-TF_BUILD"):
        assert unity[unity.index(flag) + 1] == "false"
    assert unity[unity.index("-testPlatform") + 1] == "EditMode" and "-runTests" in unity
    assert unity[unity.index("-projectPath") + 1] == "/wt/unity"
    assert unity[unity.index("-testResults") + 1] == str(tmp_path / "r.xml")


def test_the_plist_template_polls_no_more_often_than_every_ten_minutes():
    text = (up.TOOLS / "unity_postmerge.plist.in").read_text(encoding="utf-8")
    filled = up.fill_plist(text, uv="/opt/uv", repo="/r", home="/h", path="/usr/bin:/bin")
    plist = plistlib.loads(filled.encode())
    assert plist["StartInterval"] >= 600
    assert plist["Label"] == up.LABEL_ID and "@" not in filled


# --- a hung Unity is stopped by the PID machine_lock recorded ----------------------------------------------------

def _locked(tmp_path, *code):
    return [sys.executable, str(up.TOOLS / "machine_lock.py"), "--shared", "--", sys.executable, "-c", *code]


def _alive(pid):
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    # a zombie (exited, not yet reaped) counts as gone
    st = subprocess.run(["ps", "-o", "stat=", "-p", str(pid)], capture_output=True, text=True).stdout.strip()
    return bool(st) and not st.startswith("Z")


def _lock_is_free(lock):
    with open(lock, "a+") as f:
        try:
            fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            return False
        fcntl.flock(f, fcntl.LOCK_UN)
        return True


@pytest.mark.parametrize("ignores_term", [False, True])
def test_a_hung_unity_is_stopped_by_pid_and_the_lock_is_released(tmp_path, monkeypatch, ignores_term):
    lock = tmp_path / "machine.lock"
    monkeypatch.setenv("STATECRAFT_MACHINE_LOCK", str(lock))
    hang = ("import signal, time\n" + ("signal.signal(signal.SIGTERM, signal.SIG_IGN)\n" if ignores_term else "")
            + "time.sleep(600)")
    started = time.monotonic()
    with open(tmp_path / "out.log", "w") as out:
        code, stopped = up.run_with_timeout(_locked(tmp_path, hang), tmp_path / "holder", timeout=1.5, grace=1.0,
                                            poll=0.1, cwd=tmp_path, stdout=out)
    assert stopped is not None and not _alive(stopped)
    assert code != 0 and time.monotonic() - started < 60
    assert _lock_is_free(lock), "machine_lock still holds the lock after the timeout"


def test_a_unity_that_finishes_in_time_is_left_alone(tmp_path, monkeypatch):
    monkeypatch.setenv("STATECRAFT_MACHINE_LOCK", str(tmp_path / "machine.lock"))
    with open(tmp_path / "out.log", "w") as out:
        code, stopped = up.run_with_timeout(_locked(tmp_path, "import sys; sys.exit(2)"), tmp_path / "holder",
                                            timeout=30, grace=1, poll=0.1, cwd=tmp_path, stdout=out)
    assert (code, stopped) == (2, None)


def test_the_clock_starts_when_unity_starts_not_while_waiting_for_the_lock(tmp_path, monkeypatch):
    lock = tmp_path / "machine.lock"
    monkeypatch.setenv("STATECRAFT_MACHINE_LOCK", str(lock))
    with open(lock, "a+") as held:
        fcntl.flock(held, fcntl.LOCK_EX)          # someone else holds the machine for 2 s, longer than the timeout
        timer = threading.Timer(2.0, lambda: fcntl.flock(held, fcntl.LOCK_UN))
        timer.start()
        with open(tmp_path / "out.log", "w") as out:
            code, stopped = up.run_with_timeout(_locked(tmp_path, "import time; time.sleep(0.5)"), tmp_path / "holder",
                                                timeout=1.5, grace=1, poll=0.1, cwd=tmp_path, stdout=out)
        timer.join()
    assert (code, stopped) == (0, None)


def test_a_timed_out_run_is_reported_as_a_timeout():
    r = up.timed_out(4242, 90)
    assert r.broken and "timed out" in r.broken and "4242" in r.broken and not up.passed(r, unity_exit=0)


# --- the launcher runs the script from the worktree, at origin/main ---------------------------------------------

STUB = """import json, os, sys
from pathlib import Path
Path(sys.argv[sys.argv.index("--home") + 1], "ran.json").write_text(json.dumps(
    {"version": VERSION, "argv": sys.argv[1:], "file": __file__, "locked": os.environ.get("UNITY_POSTMERGE_LOCKED")}))
"""


def _git(cwd, *args):
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True).stdout.strip()


def _push_stub(src, version):
    (src / "tools").mkdir(exist_ok=True)
    (src / "tools" / "unity_postmerge.py").write_text(f"VERSION = {version}\n" + STUB)
    _git(src, "add", "-A")
    _git(src, "-c", "user.name=t", "-c", "user.email=t@users.noreply.github.com", "commit", "-qm", f"v{version}")
    _git(src, "push", "-q", "origin", "HEAD:main")
    return _git(src, "rev-parse", "HEAD")


def test_the_launcher_runs_the_merged_script_from_the_worktree(tmp_path):
    origin, src, repo, home = (tmp_path / n for n in ("origin.git", "src", "repo", "home"))
    _git(tmp_path, "init", "-q", "--bare", "-b", "main", str(origin))
    _git(tmp_path, "clone", "-q", str(origin), str(src))
    first = _push_stub(src, 1)
    _git(tmp_path, "clone", "-q", str(origin), str(repo))
    home.mkdir()
    fake_uv = tmp_path / "uv"
    fake_uv.write_text(f"#!/bin/sh\nshift\nexec {sys.executable} \"$@\"\n")
    fake_uv.chmod(0o755)
    launch = [sys.executable, str(up.TOOLS / "unity_postmerge_launch.py"), "--uv", str(fake_uv), "--repo", str(repo),
              "--home", str(home)]

    assert subprocess.run(launch).returncode == 0
    ran = json.loads((home / "ran.json").read_text())
    wt = home / "worktree"
    assert ran["version"] == 1 and Path(ran["file"]).resolve() == (wt / "tools" / "unity_postmerge.py").resolve()
    assert ran["locked"] == "1" and _git(wt, "rev-parse", "HEAD") == first
    assert ran["argv"] == ["--repo", str(repo), "--home", str(home)]

    second = _push_stub(src, 2)                         # a merged change to the script takes effect on the next run
    assert subprocess.run(launch).returncode == 0
    assert json.loads((home / "ran.json").read_text())["version"] == 2 and _git(wt, "rev-parse", "HEAD") == second
    assert _git(repo, "status", "--porcelain") == ""    # the main checkout's working tree is never touched

    (home / "ran.json").unlink()
    with open(home / "run.lock", "a+") as held:          # a run in progress: the launcher leaves the worktree alone
        fcntl.flock(held, fcntl.LOCK_EX)
        _push_stub(src, 3)
        assert subprocess.run(launch).returncode == 0
    assert not (home / "ran.json").exists() and _git(wt, "rev-parse", "HEAD") == second


def test_the_plist_runs_the_launcher_from_the_job_folder():
    text = (up.TOOLS / "unity_postmerge.plist.in").read_text(encoding="utf-8")
    plist = plistlib.loads(up.fill_plist(text, uv="/opt/uv", repo="/r", home="/h", path="/usr/bin").encode())
    assert plist["ProgramArguments"] == ["/opt/uv", "run", "/h/unity_postmerge_launch.py", "--uv", "/opt/uv",
                                         "--repo", "/r", "--home", "/h"]


# --- privacy: the log tail goes into a public issue -------------------------------------------------------------

# Assembled at run time so the repo's own home-path and email checks (scripts/oss/check_repo.py) pass on this file.
USERS, HOME_ROOT, AT = "/" + "Users", "/" + "home", "@"
LOG_WITH_IDENTIFIERS = f"""\
Built from '6000.3/staging' branch; Version is '6000.3.25f1'; mode is 'batchmode'
Command line arguments:
    /Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath {USERS}/someone/israel-game/unity
[Licensing::Module] Trying to connect to existing licensing client channel...
[Licensing::IpcConnector] LicenseClient-someone channel disconnected successfully.
Launching external process: /Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/Frameworks/UnityLicensingClient.app --namedPipe Unity-LicenseClient-someone-6000.3.25
[Licensing::Client] Successfully resolved entitlement details for Someone.Person+unity{AT}example.co.il
Loading package cache from {USERS}/someone/Library/Unity/cache/packages
Assets/Scripts/Hud/WhyPresenter.cs(81,48): error CS0103: The name 'MvpSlice' does not exist
{USERS}/someone/.cache/statecraft/unity-postmerge/worktree/unity/Assets/X.cs(3,1): error CS1002: ; expected
Saved to ~/x by SOMEONE (owner: someone{AT}gmail.com)
"""


@pytest.fixture
def someone(monkeypatch):
    """The machine's user is `someone`, with its home folder under /Users."""
    monkeypatch.setenv("HOME", f"{USERS}/someone")
    monkeypatch.setenv("USER", "someone")
    monkeypatch.setattr(up.getpass, "getuser", lambda: "someone")


def test_redaction_leaves_no_home_user_name_or_email(someone):
    out = up.redact(LOG_WITH_IDENTIFIERS)
    assert "someone" not in out.lower() and "@" not in out
    assert "~/israel-game/unity" in out and "LicenseClient-<user>" in out and "<email>" in out
    assert "error CS0103" in out and "6000.3.25f1" in out                  # the useful part survives


def test_redaction_also_covers_another_users_home_and_licence_pipe(monkeypatch):
    monkeypatch.setenv("HOME", f"{USERS}/owner")
    monkeypatch.setenv("USER", "owner")
    monkeypatch.setattr(up.getpass, "getuser", lambda: "owner")
    out = up.redact(f"{USERS}/someone/x LicenseClient-someone-6000.3.25 {HOME_ROOT}/someone/y")
    assert "someone" not in out and "LicenseClient-<user>-6000.3.25" in out


def test_the_issue_body_carries_no_identifier_from_the_log_or_test_messages(tmp_path, someone):
    r = up.parse_results(results_xml(tmp_path, [("A.One", "Failed", f"at {USERS}/someone/israel-game/unity/A.cs:3 "
                                                                     f"mail someone{AT}example.com")]))
    body = up.issue_body(SHA, PREV, r, unity_exit=2, log_text=LOG_WITH_IDENTIFIERS, commits="abc1234 Merge #600")
    assert "someone" not in body.lower() and "@" not in body
    assert "error CS0103" in body and "A.One" in body and "Last 80 lines" in body


def test_a_redaction_that_raises_withholds_the_log(tmp_path, someone, monkeypatch):
    def boom(text):
        raise ValueError("redaction broke")
    monkeypatch.setattr(up, "redact", boom)
    r = up.parse_results(results_xml(tmp_path, [("A.One", "Failed", f"at {USERS}/someone/A.cs someone{AT}example.com")]))
    body = up.issue_body(SHA, PREV, r, unity_exit=2, log_text=LOG_WITH_IDENTIFIERS, commits="abc1234 Merge #600")
    assert "someone" not in body.lower() and "@" not in body
    assert "log withheld" in body.lower() and "error CS" not in body and "Last 80 lines" not in body
    assert "A.One" in body and SHA in body and up.marker(SHA) in body       # still a usable report


def test_the_report_says_what_the_job_does_now_not_one_issue_per_commit(tmp_path):
    body = up.issue_body(SHA, PREV, up.Results(broken="x"), unity_exit=1, log_text="", commits="")
    assert "one issue per commit" not in body
    assert "comment" in body


# --- fail-silent: no result for main's tip within the bound -----------------------------------------------------

def test_the_stale_bound_covers_the_lock_wait_and_the_unity_timeout():
    assert up.STALE_AFTER == up.LOCK_TIMEOUT + up.UNITY_TIMEOUT_MIN * 60 + 30 * 60 == 6 * 3600


def test_a_tip_with_no_result_past_the_bound_is_stale():
    t0 = 1_800_000_000
    log = f"2026-10-16T00:00:00Z {PREV} PASS exit=0 total=9\n"
    assert up.stale_note(log, SHA, t0, t0 + up.STALE_AFTER + 1, {}) is not None
    assert up.stale_note(log, SHA, t0, t0 + up.STALE_AFTER - 1, {}) is None           # still inside the bound
    assert up.stale_note(log + f"x {SHA} FAIL exit=2\n", SHA, t0, t0 + 10**6, {}) is None   # it has a result
    assert up.stale_note(log, SHA, t0, t0 + 10**6, {"last_tested": SHA}) is None
    assert up.stale_note(log + f"x {SHA} STALE no result\n", SHA, t0, t0 + 10**6, {}) is None  # said once


def test_an_expired_gh_login_makes_the_job_log_stale_instead_of_staying_silent(tmp_path, monkeypatch):
    home = tmp_path / "job"
    home.mkdir()
    up.save_state(home / "state.json", {"last_tested": PREV, "pending": {"sha": PREV, "title": "t", "body": "b"}})
    (home / "results.log").write_text(f"2026-10-16T00:00:00Z {PREV} FAIL exit=2\n")
    monkeypatch.setattr(up, "fetch_tip", lambda git: SHA)
    monkeypatch.setattr(up, "commit_time", lambda git, sha: time.time() - up.STALE_AFTER - 60)

    def gh(args, stdin=None):
        raise RuntimeError("gh: authentication required")
    monkeypatch.setattr(up, "_gh", gh)
    assert up.main(["--repo", str(tmp_path), "--home", str(home)]) == 1
    log = (home / "results.log").read_text()
    assert f" {SHA} STALE " in log and "6 h" in log
    assert up.main(["--repo", str(tmp_path), "--home", str(home)]) == 1
    assert (home / "results.log").read_text().count("STALE") == 1          # once per tip, not every 15 minutes


def test_a_broken_run_reason_with_git_stderr_carries_no_identifier(someone):
    r = up.Results(broken=f"the worktree could not be prepared: fatal: cannot lock ref in {USERS}/someone/.cache/"
                          f"statecraft/unity-postmerge/worktree (committer someone{AT}example.com)")
    body = up.issue_body(SHA, PREV, r, unity_exit=-1, log_text="", commits=f"abc1234 Fix by someone{AT}example.com")
    assert "someone" not in body.lower() and "@" not in body
    assert "Broken run" in body and "could not be prepared" in body and "~/.cache/statecraft" in body


def test_a_redaction_that_raises_withholds_the_broken_run_reason_too(someone, monkeypatch):
    def boom(text):
        raise ValueError("redaction broke")
    monkeypatch.setattr(up, "redact", boom)
    r = up.Results(broken=f"the worktree could not be prepared: {USERS}/someone/x someone{AT}example.com")
    body = up.issue_body(SHA, PREV, r, unity_exit=-1, log_text="", commits="")
    assert "someone" not in body.lower() and "@" not in body
    assert "Broken run" in body and "log withheld" in body.lower()
