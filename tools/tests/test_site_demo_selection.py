import os, stat, subprocess, sys, textwrap
from pathlib import Path
import pytest

WORKFLOW = Path(__file__).resolve().parents[2] / ".github/workflows/site.yml"
STEP = "- name: Unpack the Latest release's Pages build at /demo/"

def step_script() -> str:
    lines = WORKFLOW.read_text(encoding="utf-8").splitlines()
    i = next(n for n, l in enumerate(lines) if STEP in l)
    j = next(n for n in range(i, len(lines)) if lines[n].strip() == "run: |")
    indent = len(lines[j]) - len(lines[j].lstrip())
    body = []
    for l in lines[j + 1:]:
        if l.strip() and len(l) - len(l.lstrip()) <= indent:
            break
        body.append(l)
    return textwrap.dedent("\n".join(body))

FAKE_GH = r'''#!/usr/bin/env python3
import json, os, sys, subprocess
a = sys.argv[1:]
open(os.environ["GH_LOG"], "a").write(" ".join(a) + "\n")
latest = os.environ["LATEST"]             # a tag, "404", or "403"
rows = json.loads(os.environ["RELEASES"])  # newest first: [{"tagName":..,"isPrerelease":..}]
if a[:1] == ["api"]:
    assert a[1] == "repos/maintainer/statecraft-israel/releases/latest"
    if latest in ("404", "403", "500"):
        sys.stderr.write(f"gh: HTTP {latest}\n"); sys.exit(1)
    data = {"tag_name": latest}
    result = subprocess.run(["jq", "-r", a[a.index("--jq") + 1]], input=json.dumps(data), text=True)
    sys.exit(result.returncode)
if a[:2] == ["release", "list"]:
    if os.environ.get("LIST_FAIL") == "1": sys.stderr.write("gh: offline\n"); sys.exit(1)
    result = subprocess.run(["jq", "-r", a[a.index("--jq") + 1]], input=json.dumps(rows), text=True)
    sys.exit(result.returncode)
if a[:2] == ["release", "view"]:
    print(""); sys.exit(0)                 # no pages zip: the step stops (exit 1) after naming the tag
sys.exit(99)
'''

def run(tmp_path, latest, releases, *, list_fail=False):
    bin_ = tmp_path / "bin"; bin_.mkdir()
    gh = bin_ / "gh"; gh.write_text(FAKE_GH); gh.chmod(gh.stat().st_mode | stat.S_IEXEC)
    log = tmp_path / "gh.log"; log.write_text("")
    env = {**os.environ, "PATH": f"{bin_}:{os.environ['PATH']}", "GH_LOG": str(log),
           "LATEST": latest, "RELEASES": __import__("json").dumps(releases),
           "GITHUB_REPOSITORY": "maintainer/statecraft-israel", "LIST_FAIL": str(int(list_fail))}
    p = subprocess.run(["bash", "-c", step_script()], cwd=tmp_path, env=env, capture_output=True, text=True)
    return p, log.read_text()

FAILED = {"tagName": "v0.3.2", "isPrerelease": False}   # failed nightly, still published, no longer Latest
GOOD = {"tagName": "v0.3.1", "isPrerelease": False}
HIST = {"tagName": "v0.1.0-pre", "isPrerelease": True}

def test_demo_follows_restored_latest_not_newest_published(tmp_path):
    p, log = run(tmp_path, "v0.3.1", [FAILED, GOOD, HIST])
    assert "release view v0.3.1" in log and "v0.3.2" not in log
    assert p.returncode == 1 and "No *-web-pages.zip in the Latest release (v0.3.1)" in p.stdout

def test_no_latest_falls_back_to_newest_prerelease_only(tmp_path):
    p, log = run(tmp_path, "404", [FAILED, HIST, {"tagName": "v0.0.1-pre", "isPrerelease": True}])
    assert "release view v0.1.0-pre" in log and "release view v0.3.2" not in log
    assert p.returncode == 1 and "newest prerelease (no Latest) release (v0.1.0-pre)" in p.stdout

@pytest.mark.parametrize("latest", ["403", "500"])
def test_a_non_404_lookup_error_fails_instead_of_deploying(tmp_path, latest):
    p, log = run(tmp_path, latest, [FAILED, GOOD])
    assert p.returncode != 0 and "not a 404" in p.stdout and "release view" not in log

def test_no_latest_and_no_prerelease_deploys_nothing(tmp_path):
    p, log = run(tmp_path, "404", [FAILED])
    assert p.returncode == 1 and "nothing to deploy" in p.stdout and "release view" not in log

@pytest.mark.parametrize("latest", ["", "null"])
def test_a_latest_response_without_a_tag_refuses(tmp_path, latest):
    p, log = run(tmp_path, latest, [FAILED, HIST])
    assert p.returncode != 0 and "release view" not in log and "release list" not in log

def test_a_fallback_lookup_failure_refuses(tmp_path):
    p, log = run(tmp_path, "404", [HIST], list_fail=True)
    assert p.returncode != 0 and "release view" not in log
