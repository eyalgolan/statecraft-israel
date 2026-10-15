import json
import re
from pathlib import Path

import yaml

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


def test_every_action_is_pinned_to_a_full_sha():
    for wf in (ROOT / ".github/workflows").glob("*.yml"):
        for ref in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@(\S+)", wf.read_text(encoding="utf-8")):
            assert re.fullmatch(r"[0-9a-f]{40}", ref), f"{wf.name}: {ref}"


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
    assert set(triggers(wf)) == {"workflow_run", "pull_request_review", "schedule", "workflow_dispatch"}
    assert triggers(wf)["workflow_run"]["workflows"] == ["CI"]
    assert "pull_request_target" not in triggers(wf)
    assert wf["permissions"] == {"contents": "write", "pull-requests": "write"}
    script = "\n".join(s.get("run", "") for s in steps(wf))
    assert not any("uses" in s for s in steps(wf)) and "checkout" not in script
    for field in ("title", "body", "headRefName"):
        assert field not in script, field
    assert "--match-head-commit" in script and "$r.commit.oid == $p.headRefOid" in script


def test_merge_workflow_waits_for_every_required_check():
    wf = yaml.safe_load(MERGE.read_text(encoding="utf-8"))
    need = set(wf["jobs"]["merge"]["env"]["REQUIRED_CHECKS"].split("|"))
    assert need == EXPECTED_JOBS


PLAYER = ROOT / ".github/workflows/player-golden.yml"


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
