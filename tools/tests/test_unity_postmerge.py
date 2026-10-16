import json
import plistlib
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
        self.created = []

    def __call__(self, args, stdin=None):
        if args[:2] == ["issue", "list"]:
            return json.dumps(self.open_issues)
        if args[:2] == ["label", "list"]:
            return json.dumps([{"name": n} for n in self.labels])
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
    assert plist["ProgramArguments"][:3] == ["/opt/uv", "run", "/r/tools/unity_postmerge.py"]
    assert plist["Label"] == up.LABEL_ID and "@" not in filled
