"""tools/steam_upload.py: the VDF scripts and the draft-release hand-over to steam-upload.yml (plan
docs/superpowers/plans/2026-11-01-steamworks-integration.md Task 8, #2155). `gh` is faked and no process starts:
nothing here logs in to Steam, runs steamcmd or touches the network."""
import hashlib
import json
import re
import stat
import subprocess
import zipfile
from pathlib import Path

import pytest

import steam_upload as su

REPO = Path(__file__).resolve().parents[2]
DATA = Path(__file__).resolve().parent / "data/steam"
SHA = "1b7a36cb" + "0" * 32
IDS = {"app_id": 1000, "depots": {"windows": 1001, "mac": 1002, "linux": 1003}, "branches": ["beta", "nightly"]}


@pytest.fixture(autouse=True)
def no_steamcmd(monkeypatch):
    """Any process naming steamcmd fails the test; every other process start does too, since nothing here needs one."""
    def boom(*a, **k):
        cmd = a[0] if a else k.get("args")
        if "steamcmd" in " ".join(map(str, cmd if isinstance(cmd, (list, tuple)) else [cmd])):
            pytest.fail("steamcmd was run")
        pytest.fail(f"a test started a process: {cmd}")
    for name in ("run", "Popen", "call", "check_call", "check_output"):
        monkeypatch.setattr(subprocess, name, boom)


def record(**over):
    r = {"schema": 1, "commit": SHA, "tag": "v0.3.2", "release": True, "tree_clean": True,
         "tree_clean_after_builds": True, "steam": True, "channel": "nightly", "dev": False,
         "artifacts": [{"platform": p, "steam_folder": f"steam/{p}", "archive": None} for p in su.PLATFORMS]}
    r.update(over)
    return r


def release_folder(tmp_path, **over):
    out = tmp_path / "release"
    for p, files in {"windows": {"Statecraft.exe": 0o644, "Statecraft_Data/data.unity3d": 0o644},
                     "linux": {"Statecraft.x86_64": 0o755, "Statecraft_Data/data.unity3d": 0o644},
                     "mac": {"Statecraft.app/Contents/MacOS/Statecraft Israel": 0o755,
                             "Statecraft.app/Contents/Info.plist": 0o644}}.items():
        for rel, mode in files.items():
            f = out / "steam" / p / rel
            f.parent.mkdir(parents=True, exist_ok=True)
            f.write_bytes(f"{p} {rel}".encode())
            f.chmod(mode)
        (out / "steam" / p / "LICENSE").write_text("licence\n")
    (out / "build-record.json").write_text(json.dumps(record(**over)))
    return out


class FakeGh:
    """Plays `gh`: no release of that name exists unless `existing` says it is a draft (True) or published (False)."""

    def __init__(self, existing=None):
        self.calls, self.existing = [], existing

    def __call__(self, args):
        args = [str(a) for a in args]
        self.calls.append(args)
        if args[:2] == ["release", "view"]:
            if self.existing is None:
                return su.Result(1, "", "release not found")
            return su.Result(0, json.dumps({"isDraft": self.existing}), "")
        return su.Result(0, "", "")


def config_file(tmp_path, ids=IDS):
    path = tmp_path / "steam.json"
    path.write_text(json.dumps(ids))
    return path


# --- the IDs and the record --------------------------------------------------------------------------------------------

def test_the_committed_config_has_every_id_null_until_the_owner_has_them():
    config = json.loads((REPO / "steam/steam.json").read_text())
    assert set(config) == {"app_id", "depots", "branches"} and set(config["depots"]) == set(su.PLATFORMS)
    assert config["branches"] == ["beta", "nightly"]
    for value in [config["app_id"], *config["depots"].values()]:
        assert value is None or (isinstance(value, int) and value > 0)


def test_null_ids_refuse_by_name():
    nulls = {"app_id": None, "depots": {"windows": 1001, "mac": None, "linux": None}, "branches": ["beta", "nightly"]}
    problem = su.refuse(nulls)
    assert problem is not None
    for name in ("app_id", "depots.mac", "depots.linux"):
        assert name in problem
    assert "depots.windows" not in problem
    assert su.refuse(IDS) is None


def test_the_game_app_id_matches_steam_json():
    config = json.loads((REPO / "steam/steam.json").read_text())
    source = REPO / "unity/Assets/Steam/SteamAppId.cs"
    if not source.exists():
        # The Steam assembly lands with #2154 (plan Task 6); until then nothing may give the app an ID only here.
        assert config["app_id"] is None, "steam/steam.json has an app ID but unity/Assets/Steam/SteamAppId.cs is missing"
        return
    m = re.search(r"\bValue\s*=\s*(\d+)u?\s*;", source.read_text())
    assert m, f"no `Value = <number>;` in {source}"
    assert int(m.group(1)) == (config["app_id"] or 0)


@pytest.mark.parametrize("over, word", [({"dev": True}, "dev"), ({"tree_clean_after_builds": False}, "dirty"),
                                        ({"tree_clean": False}, "dirty"), ({"channel": "default"}, "channel"),
                                        ({"channel": None}, "channel"), ({"steam": False}, "Steam")])
def test_a_dev_or_dirty_record_is_refused(over, word):
    assert su.refuse(IDS, record()) is None
    problem = su.refuse(IDS, record(**over))
    assert problem is not None and word in problem


# --- render ------------------------------------------------------------------------------------------------------------

def test_render_matches_a_golden_vdf(tmp_path):
    root = tmp_path / "content"
    root.mkdir()
    paths = su.render(IDS, root, "v0.3.2", SHA, "nightly")
    names = sorted(p.name for p in paths)
    assert names == ["app_build_1000.vdf", "depot_build_1001.vdf", "depot_build_1002.vdf", "depot_build_1003.vdf"]
    by_name = {p.name: p for p in paths}
    app = by_name["app_build_1000.vdf"].read_text().replace(str(root.resolve()), "<root>")
    assert app == (DATA / "app_build_1000.vdf").read_text()
    assert by_name["depot_build_1002.vdf"].read_text() == (DATA / "depot_build_1002.vdf").read_text()
    for p in paths:
        assert p.parent == by_name["app_build_1000.vdf"].parent, "the depot scripts sit beside the app script"
    assert by_name["app_build_1000.vdf"].parent != root.resolve(), "the scripts are never uploaded as content"


@pytest.mark.parametrize("branch", ["default", "Default", "", "public", "beta2"])
def test_default_branch_is_never_rendered(tmp_path, branch):
    with pytest.raises(ValueError):
        su.render(IDS, tmp_path, "v0.3.2", SHA, branch)
    with pytest.raises(ValueError):   # not even when someone adds it to steam.json
        su.render({**IDS, "branches": ["beta", "nightly", "default"]}, tmp_path, "v0.3.2", SHA, "default")
    assert not list(tmp_path.glob("**/*.vdf"))


def test_render_refuses_null_ids_and_a_bad_tag_or_commit(tmp_path):
    with pytest.raises(ValueError, match="app_id"):
        su.render({**IDS, "app_id": None}, tmp_path, "v0.3.2", SHA, "beta")
    with pytest.raises(ValueError):
        su.render(IDS, tmp_path, 'v0.3.2" "SetLive" "default', SHA, "beta")
    with pytest.raises(ValueError):
        su.render(IDS, tmp_path, "v0.3.2", "HEAD", "beta")


# --- stage -------------------------------------------------------------------------------------------------------------

def stage(tmp_path, gh, config=IDS, upload_tag="v0.3.2", **over):
    out = release_folder(tmp_path, **over)
    su.stage(out, upload_tag, gh, config_path=config_file(tmp_path, config))
    return out


def test_stage_writes_hashes_and_only_a_draft(tmp_path):
    gh = FakeGh()
    out = stage(tmp_path, gh)
    rec = json.loads((out / "steam-record.json").read_text())
    assert rec["tag"] == "v0.3.2" and rec["commit"] == SHA and rec["branch"] == "nightly"
    assert set(rec["assets"]) == {f"steam-v0.3.2-{p}.zip" for p in su.PLATFORMS}
    for name, meta in rec["assets"].items():
        data = (out / name).read_bytes()
        assert meta == {"sha256": hashlib.sha256(data).hexdigest(), "size": len(data)}
    releases = [c for c in gh.calls if c[0] == "release"]
    create = [c for c in releases if c[1] == "create"]
    assert len(create) == 1 and "--draft" in create[0] and create[0][2] == "steam-v0.3.2"
    assert create[0][create[0].index("--target") + 1] == SHA
    uploaded = {Path(a).name for a in create[0] if a.endswith((".zip", ".json"))}
    assert uploaded == {*rec["assets"], "steam-record.json"}
    for c in releases:
        assert c[1] in ("view", "create"), c
        assert not any(x.startswith("--draft=") or x in ("--latest", "--prerelease") for x in c), c
    assert not [c for c in gh.calls if c[:2] == ["release", "edit"]], "a draft is never published"


def test_stage_replaces_only_a_draft_of_that_name(tmp_path):
    gh = FakeGh(existing=True)
    stage(tmp_path, gh)
    assert ["release", "delete", "steam-v0.3.2", "--yes"] in gh.calls
    gh = FakeGh(existing=False)
    with pytest.raises(SystemExit, match="published"):
        stage(tmp_path / "again", gh)
    assert not [c for c in gh.calls if c[:2] in (["release", "delete"], ["release", "create"], ["workflow", "run"])]


def test_stage_dispatches_with_the_records_branch(tmp_path):
    for channel in ("beta", "nightly"):
        gh = FakeGh()
        stage(tmp_path / channel, gh, channel=channel)
        assert gh.calls[-1] == ["workflow", "run", "steam-upload.yml", "--ref", "main", "-f", "tag=v0.3.2",
                                "-f", f"branch={channel}"]


@pytest.mark.parametrize("over, tag", [({"dev": True}, "v0.3.2"), ({"channel": "default"}, "v0.3.2"),
                                       ({}, "v0.3.3"), ({"tag": "v0.3.2-rc1"}, "v0.3.2-rc1"),
                                       ({"artifacts": [{"platform": "linux", "steam_folder": "steam/linux"}]}, "v0.3.2")])
def test_stage_refuses_before_any_gh_call(tmp_path, over, tag):
    gh = FakeGh()
    with pytest.raises(SystemExit):
        stage(tmp_path, gh, upload_tag=tag, **over)
    assert gh.calls == []


def test_stage_refuses_null_ids_before_any_gh_call(tmp_path):
    gh = FakeGh()
    with pytest.raises(SystemExit, match="app_id"):
        stage(tmp_path, gh, config={**IDS, "app_id": None})
    assert gh.calls == []


def test_stage_keeps_the_executable_bits(tmp_path):
    out = stage(tmp_path, FakeGh())
    with zipfile.ZipFile(out / "steam-v0.3.2-linux.zip") as z:
        modes = {i.filename: i.external_attr >> 16 for i in z.infolist()}
        assert z.getinfo("Statecraft.x86_64").create_system == 3   # Unix, so unzip honours the mode
    assert stat.S_IMODE(modes["Statecraft.x86_64"]) == 0o755 and stat.S_IMODE(modes["LICENSE"]) == 0o644
    with zipfile.ZipFile(out / "steam-v0.3.2-mac.zip") as z:
        modes = {i.filename: i.external_attr >> 16 for i in z.infolist()}
    assert stat.S_IMODE(modes["Statecraft.app/Contents/MacOS/Statecraft Israel"]) == 0o755


def test_stage_keeps_symlinks_as_links(tmp_path):
    out = release_folder(tmp_path)
    fw = out / "steam/mac/Statecraft.app/Contents/Frameworks/X.framework"
    (fw / "Versions/A").mkdir(parents=True)
    (fw / "Versions/A/X").write_bytes(b"lib")
    (fw / "Versions/Current").symlink_to("A")
    su.stage(out, "v0.3.2", FakeGh(), config_path=config_file(tmp_path))
    with zipfile.ZipFile(out / "steam-v0.3.2-mac.zip") as z:
        info = z.getinfo("Statecraft.app/Contents/Frameworks/X.framework/Versions/Current")
        assert stat.S_ISLNK(info.external_attr >> 16) and z.read(info) == b"A"


# --- the upload job's own check ----------------------------------------------------------------------------------------

def downloaded(tmp_path):
    out = stage(tmp_path, FakeGh())
    d = tmp_path / "download"
    d.mkdir()
    for f in [*out.glob("steam-*.zip"), out / "steam-record.json"]:
        (d / f.name).write_bytes(f.read_bytes())
    return d


def test_verify_checks_hashes_tag_and_branch(tmp_path, capsys):
    d = downloaded(tmp_path)
    assert su.verify(d, "v0.3.2", "nightly") == SHA
    with pytest.raises(SystemExit, match="branch"):
        su.verify(d, "v0.3.2", "beta")
    with pytest.raises(SystemExit, match="tag"):
        su.verify(d, "v0.3.3", "nightly")
    (d / "steam-v0.3.2-linux.zip").write_bytes(b"replaced")
    with pytest.raises(SystemExit, match="sha256"):
        su.verify(d, "v0.3.2", "nightly")


# --- no credential, ever -----------------------------------------------------------------------------------------------

def test_no_option_takes_a_credential():
    parser = su.parser()
    options = [o for action in parser._actions for o in action.option_strings]
    for sub in parser._subparsers._group_actions[0].choices.values():
        options += [o for action in sub._actions for o in action.option_strings]
    assert options
    for o in options:
        assert not re.search(r"password|token|secret|vdf|user|login|key", o, re.I), o
    source = (REPO / "tools/steam_upload.py").read_text()
    assert "STEAM_CONFIG_VDF" not in source and "STEAM_BUILDER_USERNAME" not in source and "getpass" not in source


def test_steamcmd_is_never_run_here(tmp_path):
    # The autouse fixture fails any process start; the whole hand-over runs with gh faked and starts none.
    stage(tmp_path, FakeGh())
    source = (REPO / "tools/steam_upload.py").read_text()
    assert not re.search(r"""["']steamcmd""", source), "steam_upload.py never names steamcmd as a command"
