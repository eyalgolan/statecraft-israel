import hashlib
import io
import json
import zipfile
from pathlib import Path

import pytest

import nightly_publish as gp
from conftest import FakeRunner
from nightly_state import Refused

SLUG = "eyalgolan/statecraft-israel"
SHA = "a" * 40
OTHER = "c" * 40
TAG = "v0.3.1-nightly.20261028"
VERSION = "0.3.1-nightly.20261028"
DEMO = "https://example.invalid/demo/"
NOTICES = ("LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt")


class Script(FakeRunner):
    """Answers `capture` from a script: `on(prefix, *answers)` pops one answer per matching call (the last one
    repeats). An answer is the stdout text, a Refused to raise, or a callable(cmd) returning the text."""

    def __init__(self):
        super().__init__()
        self.scripts = []

    def on(self, prefix, *answers):
        self.scripts.append((tuple(prefix), list(answers)))

    def capture(self, cmd, *, cwd, timeout=300):
        self.calls.append(list(cmd))
        for prefix, answers in self.scripts:
            if tuple(cmd[:len(prefix)]) == prefix:
                answer = answers.pop(0) if len(answers) > 1 else answers[0]
                if isinstance(answer, Exception):
                    raise answer
                return answer(cmd) if callable(answer) else answer
        raise AssertionError(f"unexpected command {cmd}")


@pytest.fixture
def runner():
    return Script()


@pytest.fixture
def gh(runner, tmp_path):
    return gp.GitHub(runner, SLUG, tmp_path, sleep=lambda s: None, now=iter(range(0, 10_000, 5)).__next__)


def make_zip(path: Path, *, notices=NOTICES, extra=b"x") -> Path:
    with zipfile.ZipFile(path, "w") as z:
        for n in notices:
            z.writestr(n, "text")
        z.writestr("index.html", extra)
    return path


@pytest.fixture
def zips(tmp_path):
    return [make_zip(tmp_path / f"statecraft-israel-v{VERSION}-web.zip"),
            make_zip(tmp_path / f"statecraft-israel-v{VERSION}-web-pages.zip", extra=b"pages")]


def view_json(*, draft=True, pre=True, names=()):
    return json.dumps({"isDraft": draft, "isPrerelease": pre, "assets": [{"name": n} for n in names]})


def not_found():
    return Refused("command", "gh release view exited 1: release not found")


# --- release state ---------------------------------------------------------------------------------------------------

def test_release_state_is_draft_published_or_none(gh, runner):
    runner.on(["gh", "release", "view", TAG, "--repo", SLUG, "--json", "isDraft"], '{"isDraft": true}')
    assert gh.release_state(TAG) == "draft"
    runner.scripts.clear()
    runner.on(["gh", "release", "view", TAG], '{"isDraft": false}')
    assert gh.release_state(TAG) == "published"
    runner.scripts.clear()
    runner.on(["gh", "release", "view", TAG], not_found())
    assert gh.release_state(TAG) is None


def test_release_state_refuses_on_any_other_error_or_garbage(gh, runner):
    runner.on(["gh", "release", "view", TAG], Refused("command", "gh release view exited 4: auth required"))
    with pytest.raises(Refused) as e:
        gh.release_state(TAG)
    assert e.value.gate == "github-state"
    runner.scripts.clear()
    runner.on(["gh", "release", "view", TAG], "not json")
    with pytest.raises(Refused):
        gh.release_state(TAG)


# --- create, verify, publish, delete --------------------------------------------------------------------------------

def test_the_draft_is_a_prerelease_never_latest_targeting_the_commit(gh, runner, tmp_path, zips):
    runner.on(["gh", "release", "create"], "")
    gh.create_draft(TAG, SHA, tmp_path / "notes.md", zips)
    assert runner.calls[-1] == ["gh", "release", "create", TAG, "--repo", SLUG, "--draft", "--prerelease",
                                "--latest=false", "--target", SHA, "--title", f"{TAG} (nightly)",
                                "--notes-file", str(tmp_path / "notes.md"), str(zips[0]), str(zips[1])]


def draft_script(runner, zips, tmp_path, *, view=None, downloaded=None):
    names = [z.name for z in zips]
    runner.on(["gh", "release", "view", TAG, "--repo", SLUG, "--json", "isDraft,isPrerelease,assets"],
              view or view_json(names=names))

    def download(cmd):
        dest = Path(cmd[cmd.index("--dir") + 1])
        dest.mkdir(parents=True, exist_ok=True)
        for z in (downloaded or zips):
            (dest / z.name).write_bytes(z.read_bytes())
        return ""
    runner.on(["gh", "release", "download"], download)


def test_verify_draft_passes_when_everything_matches(gh, runner, zips, tmp_path):
    draft_script(runner, zips, tmp_path)
    gh.verify_draft(TAG, zips, tmp_path / "dl")
    assert runner.calls[-1] == ["gh", "release", "download", TAG, "--repo", SLUG, "--dir", str(tmp_path / "dl"),
                                "--pattern", "*.zip"]


def test_verify_draft_refuses_different_bytes(gh, runner, zips, tmp_path):
    other = make_zip(tmp_path / "other.zip", extra=b"tampered")
    swapped = [type("F", (), {"name": zips[0].name, "read_bytes": other.read_bytes}), zips[1]]
    draft_script(runner, zips, tmp_path, downloaded=swapped)
    with pytest.raises(Refused) as e:
        gh.verify_draft(TAG, zips, tmp_path / "dl")
    assert e.value.gate == "github-draft" and zips[0].name in e.value.detail


def test_verify_draft_refuses_a_zip_without_the_licence(gh, runner, zips, tmp_path):
    bare = make_zip(tmp_path / "bare.zip", notices=[n for n in NOTICES if n != "LICENSE"])
    bare_local = [type("F", (), {"name": zips[0].name, "read_bytes": bare.read_bytes}), zips[1]]
    # the local file is the same bare zip, so the hashes agree and only the notices check can fail
    zips[0].write_bytes(bare.read_bytes())
    draft_script(runner, zips, tmp_path, downloaded=bare_local)
    with pytest.raises(Refused) as e:
        gh.verify_draft(TAG, zips, tmp_path / "dl")
    assert e.value.gate == "github-draft" and "LICENSE" in e.value.detail


@pytest.mark.parametrize("view", [view_json(draft=False, names=["a"]), view_json(pre=False, names=["a"]),
                                  view_json(names=["only-one.zip"]), "{}", "[]"])
def test_verify_draft_refuses_a_wrong_state_or_asset_list(gh, runner, zips, tmp_path, view):
    names = [z.name for z in zips]
    if "a" in view:
        view = view.replace('"a"', json.dumps(names[0])).replace("]}", f", {json.dumps(names[1])}]}}")
    draft_script(runner, zips, tmp_path, view=view)
    with pytest.raises(Refused) as e:
        gh.verify_draft(TAG, zips, tmp_path / "dl")
    assert e.value.gate == "github-draft"


def publish_script(runner, *, draft=False, pre=True, latest="v0.3.1", remote=SHA):
    runner.on(["gh", "release", "edit"], "")
    runner.on(["gh", "release", "view"], json.dumps({"isDraft": draft, "isPrerelease": pre}))
    runner.on(["gh", "api", f"repos/{SLUG}/releases/latest"], latest + "\n")
    runner.on(["git", "-C"], f"{remote}\trefs/tags/{TAG}\n" if remote else "")


def test_publish_checks_draft_off_prerelease_on_latest_unchanged_and_tag_at_sha(gh, runner, tmp_path):
    publish_script(runner)
    gh.publish(TAG, SHA)
    assert runner.calls[0] == ["gh", "release", "edit", TAG, "--repo", SLUG, "--draft=false", "--latest=false"]
    assert ["gh", "api", f"repos/{SLUG}/releases/latest", "--jq", ".tag_name"] in runner.calls
    assert ["git", "-C", str(tmp_path), "ls-remote", "origin", f"refs/tags/{TAG}"] in runner.calls


@pytest.mark.parametrize("kw", [dict(draft=True), dict(pre=False), dict(latest=TAG), dict(remote=OTHER),
                                dict(remote="")])
def test_publish_refuses_any_mismatch(gh, runner, kw):
    publish_script(runner, **kw)
    with pytest.raises(Refused) as e:
        gh.publish(TAG, SHA)
    assert e.value.gate == "github-publish"


def test_a_latest_nightly_is_named_as_such_and_needs_a_manual_fix(gh, runner):
    publish_script(runner, latest=TAG)
    with pytest.raises(Refused) as e:
        gh.publish(TAG, SHA)
    assert "now the Latest release" in e.value.detail and "manual fix" in e.value.detail


def test_demo_serves_survives_a_broken_http_response():
    import http.client
    calls = []

    def fetch(u, timeout):
        calls.append(u)
        if len(calls) == 1:
            raise http.client.IncompleteRead(b"x")
        return io.BytesIO(b'productVersion: "0.3.1-nightly.20261028"')
    gp.demo_serves(DEMO, VERSION, deadline=100, fetch=fetch, sleep=lambda s: None,
                   now=iter(range(0, 1000, 30)).__next__)
    assert len(calls) == 2


def test_publish_prefers_the_peeled_tag_commit(gh, runner):
    publish_script(runner, remote=None)
    runner.scripts = [s for s in runner.scripts if s[0] != ("git", "-C")]
    runner.on(["git", "-C"], f"{OTHER}\trefs/tags/{TAG}\n{SHA}\trefs/tags/{TAG}^{{}}\n")
    gh.publish(TAG, SHA)


def test_a_failing_gh_edit_is_a_publish_refusal(gh, runner):
    runner.on(["gh", "release", "edit"], Refused("command", "gh release edit exited 1: boom"))
    with pytest.raises(Refused) as e:
        gh.publish(TAG, SHA)
    assert e.value.gate == "github-publish"


def test_delete_draft_removes_a_draft_and_never_cleans_tags(gh, runner):
    runner.on(["gh", "release", "view"], '{"isDraft": true}')
    runner.on(["gh", "release", "delete"], "")
    gh.delete_draft(TAG)
    assert runner.calls[-1] == ["gh", "release", "delete", TAG, "--repo", SLUG, "--yes"]
    assert all("--cleanup-tag" not in c for c in runner.calls)


def test_delete_draft_refuses_a_published_release_and_never_cleans_tags(gh, runner):
    runner.on(["gh", "release", "view"], '{"isDraft": false}')
    with pytest.raises(Refused):
        gh.delete_draft(TAG)
    assert not any(c[:3] == ["gh", "release", "delete"] for c in runner.calls)
    assert all("--cleanup-tag" not in c for c in runner.calls)


def test_delete_draft_refuses_when_the_state_is_unknown(gh, runner):
    runner.on(["gh", "release", "view"], "garbage")
    with pytest.raises(Refused):
        gh.delete_draft(TAG)
    assert not any(c[:3] == ["gh", "release", "delete"] for c in runner.calls)


# --- previous build, download -----------------------------------------------------------------------------------------

def test_previous_build_is_state_tag_else_newest_release_with_a_web_zip(gh, runner):
    assert gh.previous_build_tag("v0.3.0-nightly.20261027") == "v0.3.0-nightly.20261027"
    assert runner.calls == []
    runner.on(["gh", "release", "list", "--repo", SLUG, "--exclude-drafts", "--limit", "30", "--json", "tagName"],
              json.dumps([{"tagName": "v0.3.2"}, {"tagName": "v0.3.1"}]))
    runner.on(["gh", "release", "view", "v0.3.2"], view_json(draft=False, names=["notes.txt"]))
    runner.on(["gh", "release", "view", "v0.3.1"],
              view_json(draft=False, names=["statecraft-israel-v0.3.1-web-pages.zip", "statecraft-israel-v0.3.1-web.zip"]))
    assert gh.previous_build_tag(None) == "v0.3.1"


def test_no_release_with_a_web_zip_refuses(gh, runner):
    runner.on(["gh", "release", "list"], json.dumps([{"tagName": "v0.3.2"}]))
    runner.on(["gh", "release", "view"], view_json(names=["statecraft-israel-v0.3.2-web-pages.zip"]))
    with pytest.raises(Refused) as e:
        gh.previous_build_tag(None)
    assert e.value.gate == "sizes"


def test_download_returns_the_one_matching_file(gh, runner, tmp_path):
    def fetch(cmd):
        d = Path(cmd[cmd.index("--dir") + 1])
        d.mkdir(parents=True, exist_ok=True)
        (d / "statecraft-israel-v0.3.1-web.zip").write_bytes(b"z")
        return ""
    runner.on(["gh", "release", "download"], fetch)
    got = gh.download("v0.3.1", "*-web.zip", tmp_path / "prev")
    assert got == tmp_path / "prev/statecraft-israel-v0.3.1-web.zip"
    assert runner.calls[-1] == ["gh", "release", "download", "v0.3.1", "--repo", SLUG, "--dir",
                                str(tmp_path / "prev"), "--pattern", "*-web.zip"]


def test_download_refuses_zero_matches(gh, runner, tmp_path):
    runner.on(["gh", "release", "download"], "")
    with pytest.raises(Refused) as e:
        gh.download("v0.3.1", "*-web.zip", tmp_path / "prev")
    assert e.value.gate == "github-download"


# --- merged PR bodies -------------------------------------------------------------------------------------------------

def pr_rows(*pairs):
    return json.dumps([{"body": b, "mergeCommit": ({"oid": o} if o else None)} for b, o in pairs])


def test_merged_pr_bodies_filter_by_merge_commit_squash_and_merge_commit_alike(gh, runner, tmp_path):
    squash, merge = "1" * 40, "2" * 40
    runner.on(["git", "-C", str(tmp_path), "log", "--no-walk", "--format=%cs"], "2026-10-27\n2026-10-26\n")
    runner.on(["gh", "pr", "list"], pr_rows(("squashed body", squash), ("merged body", merge),
                                            ("older PR", "9" * 40), ("never merged", None)))
    assert gh.merged_pr_bodies({squash, merge}) == ["squashed body", "merged body"]
    cmd = [c for c in runner.calls if c[:3] == ["gh", "pr", "list"]][0]
    assert cmd == ["gh", "pr", "list", "--repo", SLUG, "--state", "merged", "--limit", "500",
                   "--search", "merged:>=2026-10-25", "--json", "body,mergeCommit"]


def test_merged_pr_bodies_refuse_a_possibly_truncated_list(gh, runner, tmp_path):
    runner.on(["git", "-C"], "2026-10-27\n")
    runner.on(["gh", "pr", "list"], pr_rows(*[("b", None)] * 500))
    with pytest.raises(Refused) as e:
        gh.merged_pr_bodies({"1" * 40})
    assert e.value.gate == "sizes"


def test_merged_pr_bodies_of_no_commits_ask_nothing(gh, runner):
    assert gh.merged_pr_bodies(set()) == []
    assert runner.calls == []


def test_merged_pr_bodies_refuse_garbage_and_an_unknown_commit_date(gh, runner):
    runner.on(["git", "-C"], "2026-10-27\n")
    runner.on(["gh", "pr", "list"], "{}")
    with pytest.raises(Refused) as e:
        gh.merged_pr_bodies({"1" * 40})
    assert e.value.gate == "sizes"
    runner.scripts.clear()
    runner.on(["git", "-C"], "\n")
    with pytest.raises(Refused):
        gh.merged_pr_bodies({"1" * 40})


# --- the site -------------------------------------------------------------------------------------------------------------

RUN_LIST = ["gh", "run", "list", "--repo", SLUG, "--workflow", "site.yml", "--event", "workflow_dispatch",
            "--limit", "5", "--json", "databaseId,createdAt"]


def test_site_dispatch_finds_the_new_run_and_watches_it(gh, runner):
    old = json.dumps([{"databaseId": 7, "createdAt": "2026-10-27T01:00:00Z"}])
    new = json.dumps([{"databaseId": 9, "createdAt": "2026-10-28T01:00:00Z"},
                      {"databaseId": 7, "createdAt": "2026-10-27T01:00:00Z"}])
    runner.on(RUN_LIST, old, old, new)
    runner.on(["gh", "workflow", "run"], "")
    runner.on(["gh", "run", "watch"], "")
    gh.dispatch_site(1000)
    assert ["gh", "workflow", "run", "site.yml", "--repo", SLUG, "--ref", "main"] in runner.calls
    assert runner.calls[-1] == ["gh", "run", "watch", "9", "--repo", SLUG, "--exit-status"]
    assert runner.calls.index(RUN_LIST) < runner.calls.index(["gh", "workflow", "run", "site.yml", "--repo", SLUG,
                                                              "--ref", "main"])


def test_a_failed_site_run_is_a_site_refusal(gh, runner):
    runner.on(RUN_LIST, "[]", json.dumps([{"databaseId": 9, "createdAt": "x"}]))
    runner.on(["gh", "workflow", "run"], "")
    runner.on(["gh", "run", "watch"], Refused("command", "gh run watch exited 1: failed"))
    with pytest.raises(Refused) as e:
        gh.dispatch_site(1000)
    assert e.value.gate == "site"


def test_a_dispatch_that_never_shows_a_new_run_refuses_at_the_deadline(gh, runner):
    runner.on(RUN_LIST, "[]")
    runner.on(["gh", "workflow", "run"], "")
    with pytest.raises(Refused) as e:
        gh.dispatch_site(30)
    assert e.value.gate == "site" and "did not appear" in e.value.detail
    assert not any(c[:3] == ["gh", "run", "watch"] for c in runner.calls)


def test_a_failed_dispatch_is_a_site_refusal(gh, runner):
    runner.on(RUN_LIST, "[]")
    runner.on(["gh", "workflow", "run"], Refused("command", "gh workflow run exited 1: 403"))
    with pytest.raises(Refused) as e:
        gh.dispatch_site(1000)
    assert e.value.gate == "site"


def test_demo_serves_polls_until_the_version_appears():
    pages = iter([b'productVersion: "0.3.1"', b'productVersion: "0.3.1-nightly.20261028"'])
    urls = []

    def fetch(u, timeout):
        urls.append(u)
        return io.BytesIO(next(pages))
    gp.demo_serves(DEMO, VERSION, deadline=100, fetch=fetch, sleep=lambda s: None,
                   now=iter(range(0, 1000, 30)).__next__)
    assert len(urls) == 2 and all(u.startswith(DEMO) for u in urls)


def test_demo_serves_refuses_at_the_deadline_and_survives_fetch_errors():
    def broken(u, timeout):
        raise OSError("down")
    with pytest.raises(Refused) as e:
        gp.demo_serves(DEMO, VERSION, deadline=100, fetch=broken, sleep=lambda s: None,
                       now=iter(range(0, 1000, 30)).__next__)
    assert e.value.gate == "site"
    with pytest.raises(Refused):
        gp.demo_serves(DEMO, VERSION, deadline=100, fetch=lambda u, timeout: io.BytesIO(b"no version here"),
                       sleep=lambda s: None, now=iter(range(0, 1000, 30)).__next__)


# --- notes and unzip ------------------------------------------------------------------------------------------------------

def test_release_notes_list_commits_and_hashes_and_say_nightly():
    text = gp.release_notes(version=VERSION, sha=SHA, since="b" * 40, subjects=["Fix a thing", "Add a thing"],
                            sha256_lines=["abc  one.zip", "def  two.zip"])
    assert f"Automated nightly build of `main` at {SHA[:12]}. A pre-release, not a versioned release." in text
    assert "- Fix a thing\n- Add a thing" in text and "abc  one.zip\ndef  two.zip" in text
    assert VERSION in text and ("b" * 12) in text and ("b" * 40) not in text
    assert "\u2014" not in text


def test_release_notes_with_no_commits_say_so():
    text = gp.release_notes(version=VERSION, sha=SHA, since="v0.3.1", subjects=[], sha256_lines=[])
    assert "v0.3.1" in text and "no commits" in text.lower()


def test_unzip_extracts_into_the_folder(tmp_path):
    z = make_zip(tmp_path / "a.zip")
    out = gp.unzip_to(z, tmp_path / "out")
    assert out == tmp_path / "out" and (out / "LICENSE").read_text() == "text" and (out / "index.html").is_file()


def test_unzip_refuses_a_path_that_leaves_the_folder(tmp_path):
    z = tmp_path / "evil.zip"
    with zipfile.ZipFile(z, "w") as zf:
        zf.writestr("../escape.txt", "x")
    with pytest.raises(Refused):
        gp.unzip_to(z, tmp_path / "out")
    assert not (tmp_path / "escape.txt").exists()


def test_sha256_lines_match_shasum_format(zips):
    lines = gp.sha256_lines(zips)
    assert lines[0] == f"{hashlib.sha256(zips[0].read_bytes()).hexdigest()}  {zips[0].name}"


# --- itch.io (butler) ------------------------------------------------------------------------------------------------

DATA = Path(__file__).resolve().parent / "data"
TOOLS = Path(__file__).resolve().parents[1]
BUTLER = Path("/fake/butler")
ITCH_TARGET = "eyalgolan/statecraft-israel:html5"
ITCH_PROJECT = "eyalgolan/statecraft-israel"


def live_text() -> str:
    return (DATA / "butler_status_live.txt").read_text(encoding="utf-8")


def status_with(build="✓ #2217587 (from #2211125)", version="0.3.1", channel="html5") -> str:
    return ("+---------+-----------+----------------------------+---------+\n"
            "| CHANNEL |  UPLOAD   |           BUILD            | VERSION |\n"
            "+---------+-----------+----------------------------+---------+\n"
            f"| {channel} | #19840947 | {build} | {version} |\n"
            "+---------+-----------+----------------------------+---------+\n")


def test_live_version_reads_the_channel_row():
    assert gp.Itch.live_version(live_text()) == "0.3.1"
    assert gp.Itch.build_id(live_text()) == "2217587"


def test_a_build_cell_without_the_live_mark_is_not_live():
    unmarked = live_text().replace("✓ ", "")
    assert "#2217587 (from #2211125)" in unmarked and "✓" not in unmarked
    assert gp.Itch.live_version(unmarked) is None
    assert gp.Itch.build_id(unmarked) is None


@pytest.mark.xfail(strict=True, reason="pending fixture not captured")
def test_a_processing_build_is_not_live():
    # butler's text for a build that is still processing has not been captured yet (it needs a real push), so
    # there is no fixture to read; until then the fail-closed rule above is what keeps it from counting as live
    text = (DATA / "butler_status_pending.txt").read_text(encoding="utf-8")
    assert gp.Itch.live_version(text) is None


@pytest.mark.parametrize("text", ["", "garbage", status_with(channel="webgl"), status_with(version=""),
                                  status_with(build="")])
def test_a_missing_empty_or_other_channel_row_is_not_live(text):
    assert gp.Itch.live_version(text) is None


def test_two_rows_for_the_channel_are_not_live():
    row = status_with().splitlines()[3]
    assert gp.Itch.live_version(status_with() + row + "\n") is None


def test_live_version_reads_the_given_channel():
    assert gp.Itch.live_version(status_with(channel="html5-beta", version="9"), "html5-beta") == "9"


def test_a_target_must_be_user_project_channel(runner):
    for bad in ("", "eyalgolan/statecraft-israel", "statecraft:html5", "a/b:", ":c"):
        with pytest.raises(Refused) as e:
            gp.Itch(runner, BUTLER, bad)
        assert e.value.gate == "itch"


def test_push_uses_the_folder_target_and_userversion(runner, tmp_path):
    runner.on([str(BUTLER), "push"], "")
    runner.on([str(BUTLER), "status", ITCH_PROJECT], live(100, "0.3.1"))
    gp.Itch(runner, BUTLER, ITCH_TARGET).push(tmp_path, "0.3.1-nightly.20261028")
    assert runner.calls[-1] == [str(BUTLER), "push", str(tmp_path), ITCH_TARGET, "--userversion",
                                "0.3.1-nightly.20261028"]


def test_a_failed_push_is_an_itch_refusal(runner, tmp_path):
    runner.on([str(BUTLER), "push"], Refused("command", "butler push exited 1"))
    runner.on([str(BUTLER), "status", ITCH_PROJECT], live(100, "0.3.1"))
    with pytest.raises(Refused) as e:
        gp.Itch(runner, BUTLER, ITCH_TARGET).push(tmp_path, "1")
    assert e.value.gate == "itch" and "exited 1" in e.value.detail


def live(build_id, version):
    return status_with(build=f"\u2713 #{build_id} (from #1)", version=version)


def itch_after_push(runner, tmp_path, *answers, before=None):
    """An Itch that has pushed once: the status before the push is `before`, then `answers` follow."""
    runner.on([str(BUTLER), "push"], "")
    runner.on([str(BUTLER), "status", ITCH_PROJECT], before or live(100, "0.3.1"), *answers)
    itch = gp.Itch(runner, BUTLER, ITCH_TARGET)
    itch.push(tmp_path, "0.3.2")
    return itch


def clock():
    return iter(range(0, 10_000, 10)).__next__


def test_push_reads_the_build_id_first(runner, tmp_path):
    itch_after_push(runner, tmp_path)
    assert runner.calls[0][:3] == [str(BUTLER), "status", ITCH_PROJECT] and runner.calls[1][1] == "push"


def test_push_without_a_readable_build_id_fails_closed_and_pushes_nothing(runner, tmp_path):
    runner.on([str(BUTLER), "push"], "")
    runner.on([str(BUTLER), "status", ITCH_PROJECT], status_with(build="#1 (processing)"))
    with pytest.raises(Refused) as e:
        gp.Itch(runner, BUTLER, ITCH_TARGET).push(tmp_path, "0.3.2")
    assert e.value.gate == "itch" and not any(c[1] == "push" for c in runner.calls)


def test_wait_live_without_a_push_first_fails_closed(runner):
    with pytest.raises(Refused) as e:
        gp.Itch(runner, BUTLER, ITCH_TARGET).wait_live("0.3.2", deadline=1000)
    assert e.value.gate == "itch" and runner.calls == []


def test_wait_live_polls_until_the_version_is_live_with_a_newer_build(runner, tmp_path):
    itch = itch_after_push(runner, tmp_path, status_with(build="#101 (processing)", version="0.3.1"),
                           live(100, "0.3.1"), live(101, "0.3.2"))
    sleeps = []
    itch.wait_live("0.3.2", deadline=1000, sleep=sleeps.append, now=clock())
    assert sleeps == [30, 30]


def test_the_expected_version_on_the_old_build_id_is_not_live(runner, tmp_path):
    itch = itch_after_push(runner, tmp_path, live(100, "0.3.2"))   # a stale row that already shows the version
    sleeps = []
    with pytest.raises(Refused) as e:
        itch.wait_live("0.3.2", deadline=100, sleep=sleeps.append, now=iter(range(0, 1000, 60)).__next__)
    assert e.value.gate == "itch" and "0.3.2" in e.value.detail and sleeps


def test_a_rollback_is_live_only_with_a_build_newer_than_the_failed_push(runner, tmp_path):
    itch = itch_after_push(runner, tmp_path, live(100, "0.3.1"))   # the forward build never goes live
    with pytest.raises(Refused):
        itch.wait_live("0.3.2", deadline=0, sleep=lambda s: None, now=clock())
    # the rollback push reads the status while the forward build is still processing: no live id, floor kept
    runner.scripts[1][1][:] = [status_with(build="#101 (processing)", version="0.3.1")]
    itch.push(tmp_path, "0.3.1")
    runner.scripts[1][1][:] = [live(100, "0.3.1"), live(100, "0.3.1"), live(102, "0.3.1")]
    sleeps = []
    itch.wait_live("0.3.1", deadline=1000, sleep=sleeps.append, now=clock())
    assert len(sleeps) == 2   # the old row (id 100, the very version being restored) did not count


def test_wait_live_gives_up_at_the_deadline(runner, tmp_path):
    itch = itch_after_push(runner, tmp_path, live(100, "0.3.1"))
    with pytest.raises(Refused) as e:
        itch.wait_live("0.3.2", deadline=100, sleep=lambda s: None, now=iter(range(0, 1000, 60)).__next__)
    assert e.value.gate == "itch" and "0.3.2" in e.value.detail and "0.3.1" in e.value.detail


def test_wait_live_treats_a_status_error_as_not_live_until_the_deadline(runner, tmp_path):
    itch = itch_after_push(runner, tmp_path, Refused("command", "butler status exited 1"))
    with pytest.raises(Refused) as e:
        itch.wait_live("0.3.2", deadline=100, sleep=lambda s: None, now=iter(range(0, 1000, 60)).__next__)
    assert e.value.gate == "itch"


def fetch_writes(files: dict[str, bytes]):
    def effect(cmd):
        dest = Path(cmd[3])
        for name, data in files.items():
            (dest / name).parent.mkdir(parents=True, exist_ok=True)
            (dest / name).write_bytes(data)
        return ""
    return effect


def pushed_folder(tmp_path) -> Path:
    folder = tmp_path / "pushed"
    (folder / "Build").mkdir(parents=True)
    (folder / "index.html").write_bytes(b"page")
    (folder / "Build" / "a.data.gz").write_bytes(b"data")
    return folder


def test_fetch_compare_accepts_identical_content(runner, tmp_path):
    runner.on([str(BUTLER), "fetch"], fetch_writes({"index.html": b"page", "Build/a.data.gz": b"data"}))
    gp.Itch(runner, BUTLER, ITCH_TARGET).matches(pushed_folder(tmp_path), tmp_path / "fetched")
    assert runner.calls[-1] == [str(BUTLER), "fetch", ITCH_TARGET, str(tmp_path / "fetched")]


@pytest.mark.parametrize("files", [{"index.html": b"page"},                                         # a file is missing
                                   {"index.html": b"page", "Build/a.data.gz": b"DATA"},             # changed bytes
                                   {"index.html": b"page", "Build/a.data.gz": b"data", "x": b"1"}])  # an extra file
def test_fetch_compare_refuses_any_difference(runner, tmp_path, files):
    runner.on([str(BUTLER), "fetch"], fetch_writes(files))
    with pytest.raises(Refused) as e:
        gp.Itch(runner, BUTLER, ITCH_TARGET).matches(pushed_folder(tmp_path), tmp_path / "fetched")
    assert e.value.gate == "itch"


def test_a_failed_fetch_refuses(runner, tmp_path):
    runner.on([str(BUTLER), "fetch"], Refused("command", "butler fetch exited 1"))
    with pytest.raises(Refused) as e:
        gp.Itch(runner, BUTLER, ITCH_TARGET).matches(pushed_folder(tmp_path), tmp_path / "fetched")
    assert e.value.gate == "itch"


def test_no_butler_command_names_the_credentials_file():
    src = "".join((TOOLS / n).read_text() for n in ("nightly_publish.py", "nightly.py", "nightly_state.py"))
    assert "butler_creds" not in src and "--identity" not in src and "auth token" not in src
    assert "GH_TOKEN" not in src
    assert not any(w in src for w in ('"login"', '"logout"'))


# --- Task 8: the itch page's game frame -----------------------------------------------------------------------------

def test_the_frame_url_is_read_from_the_page():
    page = (DATA / "itch_page.html").read_text()
    assert "html-classic.itch.zone/html/19840947-2217587/index.html?v=" in page.replace("&quot;", "")   # as captured
    assert gp.itch_frame_url(page) == "https://html-classic.itch.zone/html/19840947-2217587/index.html"   # no ?v= query
    assert gp.itch_frame_url("<html>no game</html>") is None


def test_a_frame_of_another_host_is_not_a_game_frame():
    assert gp.itch_frame_url('data-iframe="&lt;iframe src=&quot;https://evil.example/html/1-2/index.html&quot;') is None
