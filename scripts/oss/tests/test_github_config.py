import json
import os
import re
import shutil
import subprocess
from pathlib import Path

import pytest
import yaml

import pin_actions

ROOT = Path(__file__).resolve().parents[3]
CI = ROOT / ".github/workflows/ci.yml"
RULESET = ROOT / "scripts/oss/github/ruleset-main-review.json"
EXPECTED_JOBS = {"Sim (dotnet test)", "Tools (pytest)", "Repo policy", "Changelog"}


def ci() -> dict:
    return yaml.safe_load(CI.read_text(encoding="utf-8"))


def triggers(wf: dict) -> dict:
    return wf.get("on", wf.get(True))  # PyYAML reads the bare key `on` as True


def steps(wf: dict):
    return [s for j in wf["jobs"].values() for s in j["steps"]]


def test_job_names_are_the_required_checks():
    assert {j["name"] for j in ci()["jobs"].values()} == EXPECTED_JOBS
    if RULESET.exists():
        rules = json.loads(RULESET.read_text())["rules"]
        checks = next(r for r in rules if r["type"] == "required_status_checks")["parameters"]
        assert {c["context"] for c in checks["required_status_checks"]} == EXPECTED_JOBS


def test_default_token_is_read_only_and_nothing_targets_the_base_repo():
    wf = ci()
    assert wf["permissions"] == {"contents": "read"}
    t = triggers(wf)
    assert "pull_request_target" not in t and "schedule" not in t and "workflow_run" not in t


def test_checkouts_keep_no_credentials():
    for s in steps(ci()):
        if s.get("uses", "").startswith("actions/checkout@"):
            assert s.get("with", {}).get("persist-credentials") is False, s


def test_pull_request_types_rerun_on_label_changes():
    types = set(triggers(ci())["pull_request"]["types"])
    assert {"opened", "synchronize", "reopened", "labeled", "unlabeled"} <= types


def test_ci_uses_no_secret_but_the_read_only_token():
    text = CI.read_text(encoding="utf-8")
    assert "secrets." not in text
    assert set(re.findall(r"\$\{\{\s*github\.token\s*\}\}", text)) <= {"${{ github.token }}"}


# The one named exception to "no secrets in workflows": the Steam upload (plan 2026-11-01-steamworks-integration Task 8,
# owner answer Q8 "GitHub Actions"). Its two secrets live in the `steam` environment, which only `main` may deploy.
SECRET_WORKFLOWS = {"steam-upload.yml"}


def test_the_secret_exception_names_only_steam_upload():
    users = {wf.name for wf in pin_actions.workflow_paths(ROOT) if "secrets." in wf.read_text(encoding="utf-8")}
    assert users <= SECRET_WORKFLOWS, users


def _uses_values(node):
    # Every value of a `uses` key in the parsed workflow (steps and reusable-workflow jobs), however the YAML is quoted;
    # text in comments or run: blocks is not a key and is not read.
    if isinstance(node, dict):
        for key, value in node.items():
            if key == "uses" and isinstance(value, str):
                yield value
            yield from _uses_values(value)
    elif isinstance(node, list):
        for item in node:
            yield from _uses_values(item)


def _assert_every_action_is_pinned(root: Path) -> None:
    for wf in pin_actions.workflow_paths(root):
        for uses in _uses_values(yaml.safe_load(wf.read_text(encoding="utf-8"))):
            if uses.startswith(("./", "docker://")):
                continue   # local and docker actions are not pinned (pin_actions.USES needs owner/repo@ref)
            ref = uses.partition("@")[2]
            assert re.fullmatch(r"[0-9a-f]{40}", ref), f"{wf.name}: {ref}"


def test_every_action_is_pinned_to_a_full_sha():
    _assert_every_action_is_pinned(ROOT)


def test_action_assertion_checks_quoted_uses_in_yaml(tmp_path):
    workflow = tmp_path / ".github/workflows/check.yaml"
    workflow.parent.mkdir(parents=True)
    workflow.write_text('steps:\n  - uses: "actions/checkout@v4"\n', encoding="utf-8")

    with pytest.raises(AssertionError, match=r"check\.yaml: v4"):
        _assert_every_action_is_pinned(tmp_path)

    workflow.write_text(f'steps:\n  - uses: "actions/checkout@{"a" * 40}"\n', encoding="utf-8")
    _assert_every_action_is_pinned(tmp_path)


def test_action_assertion_reads_a_quoted_key_but_not_comments_or_run_text(tmp_path):
    workflow = tmp_path / ".github/workflows/check.yml"
    workflow.parent.mkdir(parents=True)
    workflow.write_text('steps:\n  - "uses": "actions/checkout@v4"\n', encoding="utf-8")
    with pytest.raises(AssertionError, match=r"check\.yml: v4"):
        _assert_every_action_is_pinned(tmp_path)

    workflow.write_text('jobs:\n  a:\n    steps:\n      # uses: actions/checkout@v4\n'
                        '      - run: |\n          echo "uses: actions/checkout@v4"\n      - uses: ./local-action\n',
                        encoding="utf-8")
    _assert_every_action_is_pinned(tmp_path)


def test_tools_job_installs_dotnet():
    # Review C2: tools/tests/test_package_release.py runs the sim's release gate through dotnet.
    tools = next(j for j in ci()["jobs"].values() if j["name"] == "Tools (pytest)")
    assert any(s.get("uses", "").startswith("actions/setup-dotnet@") for s in tools["steps"])
    assert "--deselect" not in CI.read_text(encoding="utf-8")


def test_changelog_diffs_the_pr_head_not_the_merge_ref():
    text = CI.read_text(encoding="utf-8")
    assert "github.event.pull_request.head.sha" in text and '--head "$HEAD_SHA"' in text


def test_no_cla_or_dco_machinery():
    # Decision D2 (owner ruling): inbound = outbound under the repository licences; no CLA, no DCO.
    text = CI.read_text(encoding="utf-8")
    assert "CLA" not in text.replace("no CLA or DCO", "").replace("There is no CLA", "") and "Signed-off-by" not in text
    assert not (ROOT / "CLA.md").exists() and not (ROOT / "contributors").exists()
    assert not (ROOT / ".github/workflows/cla-nudge.yml").exists()


def test_ci_commands_match_contributing():
    contributing = (ROOT / "CONTRIBUTING.md").read_text(encoding="utf-8")
    runs = [s["run"].strip() for s in steps(ci()) if "run" in s]
    for cmd in ("dotnet test sim/Statecraft.sln -c Release", "pytest tools/tests -q", "pytest scripts/oss/tests -q",
                "uv run scripts/oss/check_repo.py"):
        assert any(cmd in r for r in runs), cmd
        assert cmd.replace(" -c Release", "") in contributing, cmd


def test_codeowners_makes_the_maintainer_owner_of_everything():
    lines = [l for l in (ROOT / ".github/CODEOWNERS").read_text().splitlines() if l and not l.startswith("#")]
    assert lines == ["* @eyalgolan"]


# Review fix round 1 (I5, M1, M2, M4).
def test_pins_carry_exact_version_comments_and_dependabot_keeps_them_current():
    for ref_line in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@[0-9a-f]{40}.*", CI.read_text(encoding="utf-8")):
        assert re.search(r"# v\d+\.\d+\.\d+$", ref_line), ref_line
    dep = yaml.safe_load((ROOT / ".github/dependabot.yml").read_text(encoding="utf-8"))
    assert dep["version"] == 2 and any(u["package-ecosystem"] == "github-actions" for u in dep["updates"])


def test_toolchain_is_pinned_and_main_pushes_are_not_cancelled():
    text = CI.read_text(encoding="utf-8")
    assert "ubuntu-latest" not in text and "ubuntu-24.04" in text
    assert 'dotnet-version: "10.0.401"' in text and "10.0.x" not in text
    assert 'UV_PYTHON: "3.12"' in text
    assert "cancel-in-progress: ${{ github.event_name == 'pull_request' }}" in text
    assert "ruleset-protect-main.json" not in text and "ruleset-main-review.json" in text


def test_setup_uv_caches_on_the_files_that_carry_the_dependencies():
    # No uv.lock exists (dependencies are inline), so the default glob matched nothing and CI warned.
    uv_steps = [s for s in steps(ci()) if s.get("uses", "").startswith("astral-sh/setup-uv@")]
    assert uv_steps
    for s in uv_steps:
        globs = s["with"]["cache-dependency-glob"].split()
        assert globs == ["tools/**/*.py", "scripts/oss/**/*.py"], globs


MERGE = ROOT / ".github/workflows/merge-approved.yml"


def test_merge_workflow_never_touches_pull_request_code():
    # Owner ruling 2026-10-11: approved pull requests merge without giving contributors write access. The job holds a
    # write token, so it must never check out, build or run pull-request code, and never read PR text into the shell.
    wf = yaml.safe_load(MERGE.read_text(encoding="utf-8"))
    # Owner decision on #3026 (10 Nov): no pull_request_review trigger. A review run would use the file from the pull
    # request's merge ref; these three triggers all run main's copy.
    assert set(triggers(wf)) == {"workflow_run", "schedule", "workflow_dispatch"}
    assert triggers(wf)["workflow_run"]["workflows"] == ["CI"]
    assert "pull_request_target" not in triggers(wf)
    assert wf["permissions"] == {"contents": "write", "pull-requests": "write"}
    script = "\n".join(s.get("run", "") for s in steps(wf))
    assert not any("uses" in s for s in steps(wf)) and "checkout" not in script
    for field in ("title", "headRefName"):
        assert field not in script, field
    # The body is read only by jq for the licence tick; jq prints a SHA or "no", so no body text reaches the shell.
    assert script.count("body") == 2 and ',body,' in script and '($p.body // "" | test(' in script
    assert "--match-head-commit" in script and "$r.commit.oid == $p.headRefOid" in script


def merge_filter(pr: dict) -> str:
    # Runs the workflow's own jq program on one `gh pr view --json` object, as gh --jq does.
    wf = yaml.safe_load(MERGE.read_text(encoding="utf-8"))
    script = "\n".join(s.get("run", "") for s in steps(wf))
    program = script.split("statusCheckRollup --jq '", 1)[1].split("')", 1)[0]
    env = {**os.environ, **{k: wf["jobs"]["merge"]["env"][k] for k in ("MAINTAINER", "REQUIRED_CHECKS")}}
    return subprocess.run(["jq", "-r", program], input=json.dumps(pr), env=env, capture_output=True, text=True,
                          check=True).stdout.strip()


@pytest.mark.parametrize("author, body, merges", [
    ("contributor", "- [ ] I contribute this under the terms in `CONTRIBUTING.md`", False),
    ("contributor", "- [x] I contribute this under the terms in `CONTRIBUTING.md`", True),
    ("contributor", "- [X] I contribute this under the terms in `CONTRIBUTING.md`", True),
    ("contributor", "- [x] I contribute this under the repository's licences", False),   # the pre-#1371 line
    ("contributor", None, False),
    ("eyalgolan", "- [ ] I contribute this under the terms in `CONTRIBUTING.md`", True),   # agents never tick it
])
def test_merge_workflow_needs_the_licence_tick_from_outside_authors(author, body, merges):
    if shutil.which("jq") is None:
        assert not os.environ.get("CI"), "jq is on GitHub's ubuntu runners"
        pytest.skip("jq not installed")
    pr = {"author": {"login": author}, "body": body, "isDraft": False, "mergeable": "MERGEABLE", "headRefOid": "abc",
          "reviews": [{"author": {"login": "eyalgolan"}, "state": "APPROVED", "commit": {"oid": "abc"}}],
          "statusCheckRollup": [{"name": n, "conclusion": "SUCCESS"} for n in sorted(EXPECTED_JOBS)]}
    assert merge_filter(pr) == ("abc" if merges else "no")


def test_merge_workflow_waits_for_every_required_check():
    wf = yaml.safe_load(MERGE.read_text(encoding="utf-8"))
    need = set(wf["jobs"]["merge"]["env"]["REQUIRED_CHECKS"].split("|"))
    assert need == EXPECTED_JOBS


# A stand-in for gh, enough for the merge step: `pr list` prints the numbers in prs.txt, `pr view N --json ... --jq P`
# runs P on N.json (N.after.json once a merge of N was tried), and `pr merge N` prints N.merge.out to stderr and exits
# with N.merge.rc (0 when the file is missing). Every call is appended to calls.txt.
FAKE_GH = r"""#!/usr/bin/env bash
set -euo pipefail
d="$FAKE_GH_DIR"
printf '%s\n' "$*" >> "$d/calls.txt"
case "$1 $2" in
  "pr list") cat "$d/prs.txt" ;;
  "pr view")
    n="$3"; prog="${@: -1}"; f="$d/$n.json"
    [ -e "$d/$n.tried" ] && [ -e "$d/$n.after.json" ] && f="$d/$n.after.json"
    jq -r "$prog" "$f" ;;
  "pr merge")
    n="$3"; touch "$d/$n.tried"
    [ -e "$d/$n.merge.out" ] && cat "$d/$n.merge.out" >&2
    exit "$(cat "$d/$n.merge.rc" 2>/dev/null || echo 0)" ;;
  *) echo "fake gh: unexpected call: $*" >&2; exit 99 ;;
esac
"""


def ready_pr(head: str = "abc", **extra) -> dict:
    return {"author": {"login": "eyalgolan"}, "body": "", "isDraft": False, "mergeable": "MERGEABLE",
            "mergeStateStatus": "CLEAN", "headRefOid": head,
            "reviews": [{"author": {"login": "eyalgolan"}, "state": "APPROVED", "commit": {"oid": head}}],
            "statusCheckRollup": [{"name": n, "conclusion": "SUCCESS"} for n in sorted(EXPECTED_JOBS)], **extra}


def run_merge_step(tmp_path: Path, event: str, prs: dict) -> tuple[subprocess.CompletedProcess, list[str]]:
    """Runs the workflow's own merge script against the fake gh. prs maps a number to (pr, merge_rc, merge_out, after)."""
    if shutil.which("jq") is None:
        assert not os.environ.get("CI"), "jq is on GitHub's ubuntu runners"
        pytest.skip("jq not installed")
    wf = yaml.safe_load(MERGE.read_text(encoding="utf-8"))
    (script,) = [s["run"] for s in steps(wf) if "gh pr merge" in s.get("run", "")]
    bin_dir, data = tmp_path / "bin", tmp_path / "gh"
    bin_dir.mkdir()
    data.mkdir()
    (bin_dir / "gh").write_text(FAKE_GH, encoding="utf-8")
    (bin_dir / "gh").chmod(0o755)
    (data / "calls.txt").write_text("", encoding="utf-8")
    (data / "prs.txt").write_text("".join(f"{n}\n" for n in prs), encoding="utf-8")
    for n, (pr, rc, out, after) in prs.items():
        (data / f"{n}.json").write_text(json.dumps(pr), encoding="utf-8")
        (data / f"{n}.merge.rc").write_text(str(rc), encoding="utf-8")
        (data / f"{n}.merge.out").write_text(out, encoding="utf-8")
        if after is not None:
            (data / f"{n}.after.json").write_text(json.dumps(after), encoding="utf-8")
    env = {**os.environ, **{k: str(v) for k, v in wf["jobs"]["merge"]["env"].items() if "${{" not in str(v)},
           "PATH": f"{bin_dir}{os.pathsep}{os.environ['PATH']}", "FAKE_GH_DIR": str(data),
           "GITHUB_EVENT_NAME": event, "GH_TOKEN": "fake", "GH_REPO": "owner/repo"}
    done = subprocess.run(["bash", "-e", "-c", script], env=env, capture_output=True, text=True, timeout=60)
    calls = (data / "calls.txt").read_text(encoding="utf-8").splitlines()
    return done, calls


READ_ONLY = "GraphQL: Resource not accessible by integration (mergePullRequest)\n"


def test_a_merge_that_succeeds_passes(tmp_path):
    done, calls = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr(), 0, "", None)})
    assert done.returncode == 0, done.stdout + done.stderr
    assert "pr merge 7 --squash --match-head-commit abc" in calls


def test_the_merge_step_does_not_depend_on_which_trigger_started_it():
    # #3026: every trigger left runs main's copy with the write token, so no refusal is tolerated per event.
    wf = yaml.safe_load(MERGE.read_text(encoding="utf-8"))
    script = "\n".join(s.get("run", "") for s in steps(wf))
    assert "GITHUB_EVENT_NAME" not in script and "Resource not accessible" not in script
    assert "pull_request_review" not in MERGE.read_text(encoding="utf-8")


# pull_request_review is no longer a trigger (#3026); it stays in the list to show its old read-only exception is gone.
@pytest.mark.parametrize("event", ["workflow_run", "schedule", "workflow_dispatch", "pull_request_review"])
def test_a_read_only_refusal_fails_the_run(tmp_path, event):
    # Every run holds the write token, so "not accessible" means the permissions are wrong.
    done, _ = run_merge_step(tmp_path, event, {7: (ready_pr(), 1, READ_ONLY, None)})
    assert done.returncode != 0, done.stdout + done.stderr


def test_a_head_that_moved_is_an_expected_refusal(tmp_path):
    out = "GraphQL: Head branch was modified. Review and try the merge again. (mergePullRequest)\n"
    done, calls = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr("abc"), 1, out, ready_pr("def"))})
    assert done.returncode == 0, done.stdout + done.stderr
    assert "head moved" in done.stdout and "pr merge 7 --squash --match-head-commit abc" in calls


def test_a_head_moved_message_without_a_new_head_fails_the_run(tmp_path):
    # The tolerance rests on the head GitHub reports after the refusal, not on the wording.
    out = "GraphQL: Head branch was modified. Review and try the merge again. (mergePullRequest)\n"
    done, _ = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr("abc"), 1, out, ready_pr("abc"))})
    assert done.returncode != 0, done.stdout + done.stderr


def test_a_pull_request_merged_by_hand_mid_run_passes(tmp_path):
    # Owner decision on #3027 (10 Nov): "Log and skip if MERGED (Recommended)". Someone merged it between gh's read
    # and the mutation, so gh refused; GitHub now reports it MERGED on the same head.
    out = "GraphQL: Pull request is not mergeable (mergePullRequest)\n"
    done, _ = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr("abc"), 1, out, ready_pr("abc", state="MERGED"))})
    assert done.returncode == 0, done.stdout + done.stderr
    assert "#7: merge refused, GitHub reports it already merged" in done.stdout


def test_a_failed_re_read_after_a_refusal_fails_the_run(tmp_path):
    # If gh cannot re-read the pull request, the run must not read the empty answer as a moved head.
    out = "HTTP 502: Bad Gateway\n"
    done, _ = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr("abc"), 1, out, "not json")})
    assert done.returncode != 0, done.stdout + done.stderr
    assert "head moved" not in done.stdout


@pytest.mark.parametrize("state", ["OPEN", "CLOSED"])
def test_a_refusal_on_a_pull_request_that_did_not_merge_fails_the_run(tmp_path, state):
    # Only MERGED is tolerated; a pull request closed mid-run, or still open on the same head, fails as before.
    out = "GraphQL: Pull request is not mergeable (mergePullRequest)\n"
    done, _ = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr("abc"), 1, out, ready_pr("abc", state=state))})
    assert done.returncode != 0, done.stdout + done.stderr
    assert "::error::#7" in done.stdout


@pytest.mark.parametrize("out", [
    "HTTP 401: Bad credentials (https://api.github.com/graphql)\n",
    "GraphQL: Repository rule violations found\n\nChanges must be made through a pull request. (mergePullRequest)\n",
    "X Pull request owner/repo#7 is not mergeable: the base branch policy prohibits the merge.\n",
    "GraphQL: API rate limit exceeded for installation ID 1. (mergePullRequest)\n",
    "unknown flag: --match-head-commit\n",
    "",
])
def test_any_other_merge_failure_fails_the_run(tmp_path, out):
    done, _ = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr(), 1, out, None)})
    assert done.returncode != 0, done.stdout + done.stderr


def test_one_failed_merge_does_not_stop_the_next_pull_request(tmp_path):
    done, calls = run_merge_step(tmp_path, "workflow_run", {
        7: (ready_pr("abc"), 1, "HTTP 502: Bad Gateway\n", None),
        8: (ready_pr("def"), 0, "", None)})
    assert done.returncode != 0, done.stdout + done.stderr
    assert "pr merge 8 --squash --match-head-commit def" in calls


def test_an_approved_pull_request_behind_main_waits_without_a_merge_attempt(tmp_path):
    # The main ruleset needs branches up to date, so gh refuses a BEHIND pull request before asking GitHub; the run
    # says so instead of trying (and failing) every ten minutes.
    done, calls = run_merge_step(tmp_path, "schedule", {7: (ready_pr(mergeStateStatus="BEHIND"), 1, "", None)})
    assert done.returncode == 0, done.stdout + done.stderr
    assert not any(c.startswith("pr merge") for c in calls) and "behind main" in done.stdout


def test_a_pull_request_that_is_not_ready_is_not_merged(tmp_path):
    done, calls = run_merge_step(tmp_path, "workflow_run", {7: (ready_pr(isDraft=True), 0, "", None)})
    assert done.returncode == 0, done.stdout + done.stderr
    assert not any(c.startswith("pr merge") for c in calls) and "#7: not ready" in done.stdout


PLAYER =ROOT / ".github/workflows/player-golden.yml"


def test_player_golden_runs_only_the_repositorys_own_released_players():
    # Plan 2026-10-14 section 6.3 and decision O7: GitHub runners only check the players the owner built and released.
    # The workflow runs binaries, so nothing a pull request, a URL or an Actions artifact supplies may reach it.
    wf = yaml.safe_load(PLAYER.read_text(encoding="utf-8"))
    text = PLAYER.read_text(encoding="utf-8")
    assert triggers(wf) == {"release": {"types": ["published"]},
                            "workflow_dispatch": {"inputs": {"tag": triggers(wf)["workflow_dispatch"]["inputs"]["tag"]}}}
    assert wf["permissions"] == {"contents": "read"}
    assert "secrets." not in text and "UNITY_" not in text and "game-ci/" not in text   # no Unity licence or editor
    for s in steps(wf):
        if s.get("uses", "").startswith("actions/checkout@"):
            assert s.get("with", {}).get("persist-credentials") is False, s
        assert "${{" not in s.get("run", ""), s   # inputs and event fields reach the shell only through env:
    # An empty matrix is a workflow error, so the player jobs need the guard; one platform's failure must not cancel the
    # others' runs (review N2).
    golden = wf["jobs"]["golden"]
    assert golden["needs"] == "plan" and golden["if"] == "needs.plan.outputs.count != '0'"
    assert golden["strategy"]["fail-fast"] is False
    assert golden["strategy"]["matrix"] == "${{ fromJSON(needs.plan.outputs.matrix) }}"
    run_step = next(s for s in golden["steps"] if "player_golden_check.py run" in s.get("run", ""))
    assert '--sha256 "$SHA256"' in run_step["run"] and run_step["env"]["SHA256"] == "${{ matrix.sha256 }}"
    assert '${ARCH:+--arch "$ARCH"}' in run_step["run"] and run_step["env"]["ARCH"] == "${{ matrix.arch }}"
    assert golden["name"] == "Player golden (${{ matrix.label }})" and golden["runs-on"] == "${{ matrix.runner }}"
    # #533 (P62): the Linux player needs an X display even with -nographics; player_golden_check.py runs it under
    # xvfb-run, so the Linux job installs xvfb before that step, and only the Linux job.
    names = [s.get("name", "") for s in golden["steps"]]
    xvfb = next(s for s in golden["steps"] if "xvfb" in s.get("run", ""))
    assert xvfb["if"] == "matrix.platform == 'linux'" and "apt-get install" in xvfb["run"]
    assert names.index(xvfb["name"]) < names.index(run_step["name"])
    downloads = [s["run"] for s in steps(wf) if "gh release download" in s.get("run", "")]
    assert downloads and all('--repo "$GITHUB_REPOSITORY"' in r for r in downloads)
    assert "curl" not in text and "wget" not in text and "download-artifact" not in text
    for ref_line in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@[0-9a-f]{40}.*", text):
        assert re.search(r"# v\d+\.\d+\.\d+$", ref_line), ref_line


def test_the_reviewed_by_guard_runs_on_the_pr_head_with_full_history():
    """#375: the guard needs every commit of the pull request (fetch-depth 0) and diffs the PR head, not the merge ref."""
    job = next(j for j in ci()["jobs"].values() if j["name"] == "Changelog")
    checkout = next(s for s in job["steps"] if s.get("uses", "").startswith("actions/checkout@"))
    assert checkout["with"]["fetch-depth"] == 0
    step = next(s for s in job["steps"] if "reviewed_by_guard.py" in s.get("run", ""))
    assert '--base "$BASE_SHA"' in step["run"] and '--head "$HEAD_SHA"' in step["run"]
    assert step["env"]["HEAD_SHA"] == "${{ github.event.pull_request.head.sha }}"
    assert step["env"]["BASE_SHA"] == "${{ github.event.pull_request.base.sha }}"


def test_the_reviewed_by_guard_reads_the_description_from_the_event_file_only():
    """Owner, 4 November 2026 ("Yes, it counts"): the guard reads the PR's "AI assistance" section from the runner's event
    file. The description is attacker-controlled text, so it never goes into a run line or an env value."""
    job = next(j for j in ci()["jobs"].values() if j["name"] == "Changelog")
    step = next(s for s in job["steps"] if "reviewed_by_guard.py" in s.get("run", ""))
    assert '--pr-event "$GITHUB_EVENT_PATH"' in step["run"]
    assert "github.event.pull_request.body" not in (ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")


STEAM = ROOT / ".github/workflows/steam-upload.yml"
STEAM_SECRETS = {"STEAM_BUILDER_USERNAME", "STEAM_CONFIG_VDF"}
STEAM_LOGIN_STEPS = {"Restore the builder login (owner's secret)", "Upload with steamcmd (owner's builder account)"}


def steam():
    return yaml.safe_load(STEAM.read_text(encoding="utf-8"))


def test_steam_upload_runs_only_on_dispatch():
    # Plan Task 8: a pull request, a fork, a release event or a push never starts an upload; only someone with write
    # access dispatches it (tools/steam_upload.py stage does, on the owner's Mac).
    t = triggers(steam())
    assert set(t) == {"workflow_dispatch"}
    for name in ("push", "pull_request", "pull_request_target", "release", "schedule", "workflow_run"):
        assert name not in t


def test_steam_upload_uses_the_steam_environment_and_minimal_permissions():
    wf = steam()
    assert wf["permissions"] == {}
    assert set(wf["jobs"]) == {"upload", "refused"}
    job = wf["jobs"]["upload"]
    assert job["environment"] == "steam" and job["permissions"] == {"contents": "write"}
    assert job["runs-on"] == "ubuntu-24.04" and job["timeout-minutes"] <= 120
    for s in job["steps"]:
        if s.get("uses", "").startswith("actions/checkout@"):
            assert s["with"]["persist-credentials"] is False and s["with"]["ref"] == "main", s
        assert "${{" not in s.get("run", ""), s   # inputs, outputs and secrets reach the shell only through env:
        assert "set -x" not in s.get("run", ""), s
    for ref_line in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@[0-9a-f]{40}.*", STEAM.read_text(encoding="utf-8")):
        assert re.search(r"# v\d+\.\d+\.\d+$", ref_line), ref_line


def test_steam_upload_branch_input_is_beta_or_nightly():
    inputs = triggers(steam())["workflow_dispatch"]["inputs"]
    assert set(inputs) == {"tag", "branch", "record_sha256"}
    assert inputs["branch"]["type"] == "choice" and inputs["branch"]["options"] == ["beta", "nightly"]
    assert inputs["tag"]["required"] is True and inputs["branch"]["required"] is True
    first = steam()["jobs"]["upload"]["steps"][0]
    assert "^v[0-9]+\\.[0-9]+\\.[0-9]+$" in first["run"] and "^(beta|nightly)$" in first["run"]


def test_steam_secrets_appear_only_in_the_login_steps():
    job = steam()["jobs"]["upload"]
    using = {}
    for s in job["steps"]:
        text = yaml.safe_dump(s)
        found = set(re.findall(r"secrets\.(\w+)", text))
        if found:
            using[s.get("name")] = found
            assert all(f"${{{{ secrets.{n} }}}}" in s.get("env", {}).values() for n in found), s   # only through env:
    assert set(using) == STEAM_LOGIN_STEPS and set().union(*using.values()) == STEAM_SECRETS
    text = STEAM.read_text(encoding="utf-8")
    assert "secrets: inherit" not in text and "toJSON(secrets)" not in text
    # The login config is always deleted, the upload fails without SteamPipe's success line, and the draft goes only
    # after a successful upload.
    names = [s.get("name", s.get("uses")) for s in job["steps"]]
    cleanup = next(s for s in job["steps"] if "config.vdf" in s.get("run", "") and s.get("if") == "always()")
    assert names.index(cleanup["name"]) > names.index("Upload with steamcmd (owner's builder account)")
    upload = next(s for s in job["steps"] if s.get("name") == "Upload with steamcmd (owner's builder account)")
    assert "+@NoPromptForPassword 1" in upload["run"] and '+login "$STEAM_BUILDER_USERNAME"' in upload["run"]
    assert "Successfully finished AppID" in upload["run"]
    delete = next(s for s in job["steps"] if "gh release delete" in s.get("run", ""))
    assert delete.get("if", "success()") == "success()" and names.index(delete["name"]) > names.index(cleanup["name"])


def test_steam_upload_checks_the_draft_and_the_commit_before_any_login():
    job = steam()["jobs"]["upload"]
    names = [s.get("name", s.get("uses")) for s in job["steps"]]
    verify = next(s for s in job["steps"] if "steam_upload.py verify" in s.get("run", ""))
    on_main = next(s for s in job["steps"] if "merge-base --is-ancestor" in s.get("run", ""))
    render = next(s for s in job["steps"] if "steam_upload.py render" in s.get("run", ""))
    login = names.index("Restore the builder login (owner's secret)")
    assert names.index(verify["name"]) < names.index(on_main["name"]) < names.index(render["name"]) < login
    assert "refs/remotes/origin/main" in on_main["run"]


def test_steam_upload_runs_only_for_the_owner_on_main():
    # Review I1 (#2155): the environment's branch policy is a repository setting the workflow cannot see; the job also
    # refuses itself any run not started by the repository owner (stage and the nightly dispatch as the owner) on main.
    guard = steam()["jobs"]["upload"]["if"]
    assert "github.ref == 'refs/heads/main'" in guard
    assert "github.triggering_actor == github.repository_owner" in guard and "&&" in guard


def test_steam_upload_record_hash_comes_from_the_dispatch():
    # Review (a review agent, k2 C1): the draft can be edited after stage; the dispatch input cannot.
    inputs = triggers(steam())["workflow_dispatch"]["inputs"]
    assert inputs["record_sha256"]["required"] is True
    first = steam()["jobs"]["upload"]["steps"][0]
    assert "^[0-9a-f]{64}$" in first["run"] and first["env"]["RECORD_SHA256"] == "${{ inputs.record_sha256 }}"
    verify = next(s for s in steam()["jobs"]["upload"]["steps"] if "steam_upload.py verify" in s.get("run", ""))
    assert '--record-sha256 "$RECORD_SHA256"' in verify["run"]


def test_a_refused_steam_upload_fails_loudly():
    # Review (k2 C4): a run the guard refuses must fail, not show a green skipped job.
    jobs = steam()["jobs"]
    refused = jobs["refused"]
    assert refused["if"] == "!(" + jobs["upload"]["if"] + ")"
    assert "environment" not in refused and refused["permissions"] == {}
    assert any("exit 1" in s.get("run", "") for s in refused["steps"])
    assert "secrets." not in yaml.safe_dump(refused)


def test_the_login_cache_is_written_where_either_steamcmd_layout_reads_it():
    # Review (k2 H1): where the tarball's steamcmd keeps config/ is unconfirmed; both candidates get it and both go.
    job = steam()["jobs"]["upload"]
    restore = next(s for s in job["steps"] if s.get("name") == "Restore the builder login (owner's secret)")
    for path in ('"$HOME/Steam/config/config.vdf"', '"$RUNNER_TEMP/steamcmd/config/config.vdf"'):
        assert path in restore["run"]
    cleanup = next(s for s in job["steps"] if s.get("if") == "always()")
    assert '"$HOME/Steam"' in cleanup["run"] and '"$RUNNER_TEMP/steamcmd"' in cleanup["run"]
