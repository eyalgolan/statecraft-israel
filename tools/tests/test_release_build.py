"""tools/release_build.py: the local desktop release script (#482 Phases 0-1). Every subprocess (git, Unity, the player,
the lock) is faked; nothing here starts Unity or touches the network."""
import hashlib
import json
from pathlib import Path

import pytest

import release_build as rb

H1950 = "71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07"
H1952 = "62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8"
SHA = "1b7a36cb" + "0" * 32


@pytest.fixture
def repo(tmp_path):
    """A minimal repository tree with the files the script reads."""
    root = tmp_path / "repo"
    (root / "unity/ProjectSettings").mkdir(parents=True)
    (root / "unity/ProjectSettings/ProjectVersion.txt").write_text(
        "m_EditorVersion: 6000.3.25f1\nm_EditorVersionWithRevision: 6000.3.25f1 (e1dba0a9aba4)\n")
    (root / "unity/ProjectSettings/ProjectSettings.asset").write_text("PlayerSettings:\n  bundleVersion: 0.1.0\n")
    (root / "sim/Statecraft.Sim").mkdir(parents=True)
    (root / "sim/Statecraft.Sim/SimInfo.cs").write_text('        public const string Version = "0.1.0";\n')
    (root / "sim/Statecraft.Sim.Tests/golden").mkdir(parents=True)
    (root / "sim/Statecraft.Sim.Tests/golden/manifest.txt").write_text(f"# pinned\nhash1950 {H1950}\nhash1952 {H1952}\nsmoke5 abc\n")
    return root


@pytest.fixture
def editor(tmp_path):
    """A fake Unity install with every module of the three platforms; tests remove one to make it missing."""
    root = tmp_path / "Editor" / "6000.3.25f1"
    exe = root / "Unity.app/Contents/MacOS/Unity"
    exe.parent.mkdir(parents=True)
    exe.write_text("")
    for module in rb.PLATFORMS.values():
        (root / module.module_dir).mkdir(parents=True, exist_ok=True)
    return exe


class Fake:
    """Records every command and plays git, Unity and the player."""

    def __init__(self, dirty="", golden_ok=True, build_ok=True):
        self.calls: list[list[str]] = []
        self.dirty, self.golden_ok, self.build_ok = dirty, golden_ok, build_ok

    def __call__(self, cmd, **kw):
        cmd = [str(c) for c in cmd]
        self.calls.append(cmd)
        out, code = "", 0
        if cmd[:2] == ["git", "status"]:
            out = self.dirty
        elif cmd[:2] == ["git", "rev-parse"]:
            out = "main\n" if "--abbrev-ref" in cmd else SHA + "\n"
        elif cmd[:2] == ["git", "describe"]:
            out = "v0.1.0-5-g1b7a36cb\n"
        elif cmd[:2] == ["git", "check-ignore"]:
            code = 1
        elif "-executeMethod" in cmd:
            target = Path(cmd[cmd.index("-out") + 1])
            log = Path(cmd[cmd.index("-logFile") + 1])
            if self.build_ok:
                if target.suffix == ".app":
                    (target / "Contents/MacOS").mkdir(parents=True)
                    (target / "Contents/Info.plist").write_bytes(
                        b'<?xml version="1.0" encoding="UTF-8"?><plist version="1.0"><dict>'
                        b"<key>CFBundleExecutable</key><string>Statecraft Israel</string></dict></plist>")
                    (target / "Contents/MacOS/Statecraft Israel").write_bytes(b"player")
                else:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_bytes(b"player " + target.name.encode())
                (target.parent / "event-photos.json").write_text("{}\n")
                log.write_text("BUILD Succeeded StandaloneOSX -> x: 1.0 MB on disk, 0 errors, 321s\n")
            else:
                log.write_text("BUILD Failed StandaloneOSX -> x: 0.0 MB on disk, 3 errors, 12s\n")
                code = 1
        elif "-golden" in cmd:
            log = Path(cmd[cmd.index("-logFile") + 1])
            second = f"GOLDEN OK 1952 {H1952}" if self.golden_ok else f"GOLDEN FAIL 1952 {'f' * 64} expected {H1952}"
            log.write_text(f"boot\nGOLDEN OK 1950 {H1950}\n{second}\n")
            code = 0 if self.golden_ok else 1
        elif cmd[0] == "ditto":
            Path(cmd[-1]).write_bytes(b"PK mac zip of " + Path(cmd[-2]).name.encode())
        elif cmd[0] == "lipo":
            out = "x86_64 arm64\n"
        return rb.Result(code, out)


def run_main(argv, repo, editor, fake, monkeypatch):
    monkeypatch.setattr(rb, "REPO", repo)
    monkeypatch.setattr(rb, "run", fake)
    return rb.main(["--unity", str(editor), *argv])


def unity_calls(fake):
    return [c for c in fake.calls if "-executeMethod" in c]


# --- arguments ---------------------------------------------------------------------------------------------------------

def test_the_platform_flag_selects_one_or_all():
    assert rb.selected(rb.parse(["--platform", "mac"])) == ["mac"]
    assert rb.selected(rb.parse(["--platform", "all"])) == ["mac", "linux", "windows"]
    assert rb.selected(rb.parse([])) == ["mac", "linux", "windows"], "a release builds every platform by default"
    with pytest.raises(SystemExit):
        rb.parse(["--platform", "ios"])


def test_out_dry_run_and_dev_are_flags():
    a = rb.parse(["--out", "/tmp/x", "--dry-run", "--dev"])
    assert a.out == Path("/tmp/x") and a.dry_run and a.dev
    a = rb.parse([])
    assert a.out is None and not a.dry_run and not a.dev


def test_the_default_output_folder_is_outside_the_repository(repo):
    out = rb.default_out(SHA, "0.1.0", "20261014T120000Z")
    assert out == Path.home() / "statecraft-release" / "v0.1.0-1b7a36cb-20261014T120000Z"
    assert not out.is_relative_to(rb.REPO)


def test_an_output_folder_inside_the_repository_is_refused_unless_git_ignores_it(repo, editor, monkeypatch):
    fake = Fake()
    assert run_main(["--platform", "mac", "--out", str(repo / "builds")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []


# --- what the script reads ------------------------------------------------------------------------------------------------

def test_unity_version_and_goldens_come_from_the_repository(repo, monkeypatch):
    monkeypatch.setattr(rb, "REPO", repo)
    assert rb.unity_version() == ("6000.3.25f1", "e1dba0a9aba4")
    assert rb.expected_goldens() == {"1950": H1950, "1952": H1952}
    assert rb.versions() == {"bundle_version": "0.1.0", "sim_version": "0.1.0"}


# --- refusals ---------------------------------------------------------------------------------------------------------

def test_a_dirty_tree_is_refused_before_any_build(repo, editor, monkeypatch, capsys, tmp_path):
    fake = Fake(dirty=" M unity/ProjectSettings/ProjectSettings.asset\n")
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []
    err = capsys.readouterr().err
    assert "REFUSED" in err and "ProjectSettings.asset" in err
    assert not (tmp_path / "out").exists()


def test_untracked_files_count_as_dirty(repo, editor, monkeypatch, tmp_path):
    fake = Fake(dirty="?? notes.txt\n")
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 1
    status = next(c for c in fake.calls if c[:2] == ["git", "status"])
    assert "--porcelain" in status and "--untracked-files=normal" in status


def test_missing_modules_fail_clearly_and_name_what_to_install(repo, editor, monkeypatch, capsys, tmp_path):
    root = editor.parents[3]
    for name in ("linux", "windows"):
        (root / rb.PLATFORMS[name].module_dir).rmdir()
    fake = Fake()
    assert run_main(["--platform", "all", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 2
    assert unity_calls(fake) == [], "no platform is built when any selected module is missing"
    err = capsys.readouterr().err
    assert "linux-mono" in err and "windows-mono" in err and "mac-il2cpp" not in err
    assert "Unity Hub" in err and "install-modules" in err and "-m linux-mono -m windows-mono" in err


def test_only_the_selected_platforms_modules_are_required(repo, editor, monkeypatch, tmp_path):
    root = editor.parents[3]
    (root / rb.PLATFORMS["windows"].module_dir).rmdir()
    fake = Fake()
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 0


def test_a_missing_editor_fails_clearly(repo, tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(rb, "REPO", repo)
    monkeypatch.setattr(rb, "run", Fake())
    assert rb.main(["--unity", str(tmp_path / "nope/Unity"), "--platform", "mac"]) == 2
    assert "6000.3.25f1" in capsys.readouterr().err


# --- the lock and the commands ----------------------------------------------------------------------------------------------

def test_every_unity_build_runs_under_the_shared_lock_and_waits_for_unity(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    assert run_main(["--platform", "all", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 0
    builds = unity_calls(fake)
    assert len(builds) == 3
    for c in builds:
        lock = c.index("--")
        head = c[:lock]
        assert head[:2] == ["uv", "run"] and head[2].endswith("tools/machine_lock.py")
        assert "--shared" in head and "--wait-unity" in head
        assert c[lock + 1] == str(editor), "Unity is the locked command itself"
        assert "-nographics" not in c, "never -nographics for the editor"
        for flag in ("-batchmode", "-quit", "-CI", "-GITHUB_ACTIONS", "-TF_BUILD"):
            assert flag in c
    methods = [c[c.index("-executeMethod") + 1] for c in builds]
    assert methods == ["Statecraft.Game.EditorTools.Build.Mac", "Statecraft.Game.EditorTools.Build.LinuxMono",
                       "Statecraft.Game.EditorTools.Build.WindowsMono"]
    assert [c[c.index("-buildTarget") + 1] for c in builds] == ["OSXUniversal", "Linux64", "Win64"]


def test_the_lock_never_times_out_on_its_own_hour(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    run_main(["--platform", "mac", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch)
    for c in unity_calls(fake):
        assert int(c[c.index("--timeout") + 1]) >= 7 * 24 * 3600, "a long wait is reported, never cancelled"


def test_release_builds_are_shareable_and_dev_builds_are_not(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    run_main(["--platform", "mac", "--out", str(tmp_path / "a")], repo, editor, fake, monkeypatch)
    assert "-shareable" in unity_calls(fake)[0]
    fake = Fake()
    run_main(["--platform", "mac", "--dev", "--out", str(tmp_path / "b")], repo, editor, fake, monkeypatch)
    assert "-shareable" not in unity_calls(fake)[0]
    assert json.loads((tmp_path / "b/build-record.json").read_text())["shareable"] is False


def test_settings_churn_is_dropped_after_each_build(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    run_main(["--platform", "all", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch)
    checkouts = [i for i, c in enumerate(fake.calls) if c[:3] == ["git", "checkout", "--"]]
    builds = [i for i, c in enumerate(fake.calls) if "-executeMethod" in c]
    assert len(checkouts) == 3 and all(b < k for b, k in zip(builds, checkouts))
    assert fake.calls[checkouts[0]][3:] == ["unity/ProjectSettings", "unity/Assets/Settings"]


def test_the_sim_dlls_are_built_before_unity(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    run_main(["--platform", "mac", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch)
    sim = next(i for i, c in enumerate(fake.calls) if c[-1].endswith("tools/build_sim_for_unity.py"))
    assert sim < next(i for i, c in enumerate(fake.calls) if "-executeMethod" in c)


def test_no_push_upload_commit_or_network(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    run_main(["--platform", "all", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch)
    for c in fake.calls:
        if c[0] == "git":
            assert c[1] in {"status", "rev-parse", "describe", "checkout", "check-ignore"}, c
        assert c[0] not in {"gh", "curl", "wget", "scp", "rsync", "steamcmd", "xcrun"}, c
        assert "push" not in c and "commit" not in c
    source = Path(rb.__file__).read_text()
    for module in ("urllib", "http.client", "socket", "requests", "ftplib", "smtplib"):
        assert f"import {module}" not in source and f"from {module}" not in source


def test_a_dry_run_builds_nothing_and_writes_nothing(repo, editor, monkeypatch, tmp_path, capsys):
    fake = Fake()
    assert run_main(["--platform", "all", "--dry-run", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 0
    assert unity_calls(fake) == [] and not [c for c in fake.calls if "-golden" in c or c[:2] == ["git", "checkout"]]
    assert not (tmp_path / "out").exists()
    out = capsys.readouterr().out
    assert "Build.Mac" in out and "Build.WindowsMono" in out and "machine_lock.py" in out


def test_a_failed_build_stops_with_no_record(repo, editor, monkeypatch, tmp_path):
    fake = Fake(build_ok=False)
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch) == 1
    assert not (tmp_path / "out/build-record.json").exists()
    assert [c for c in fake.calls if c[:2] == ["git", "checkout"]], "the settings are restored even after a failure"


# --- the record ----------------------------------------------------------------------------------------------------------

def test_the_record_holds_commit_unity_date_hashes_sizes_and_goldens(repo, editor, monkeypatch, tmp_path, capsys):
    monkeypatch.setattr(rb, "utc_now", lambda: rb.datetime(2026, 10, 14, 12, 0, 0, tzinfo=rb.timezone.utc))
    fake = Fake()
    out = tmp_path / "out"
    assert run_main(["--platform", "all", "--out", str(out)], repo, editor, fake, monkeypatch) == 0
    record = json.loads((out / "build-record.json").read_text())
    assert record["commit"] == SHA and record["tree_clean"] is True
    assert record["unity"] == {"version": "6000.3.25f1", "revision": "e1dba0a9aba4"}
    assert record["date_utc"] == "2026-10-14T12:00:00Z"
    assert record["version"] == {"bundle_version": "0.1.0", "sim_version": "0.1.0"}
    assert record["shareable"] is True and record["signing"] == "none" and record["tag"] == "v0.1.0"
    arts = {a["platform"]: a for a in record["artifacts"]}
    assert set(arts) == {"mac", "linux", "windows"}
    assert arts["mac"]["backend"] == "IL2CPP" and arts["windows"]["backend"] == "Mono" and arts["linux"]["backend"] == "Mono"
    assert arts["mac"]["architecture"] == "x86_64 arm64"
    assert arts["mac"]["unity_build_seconds"] == 321

    mac = arts["mac"]
    manifest = out / mac["file_list"]
    lines = manifest.read_text().splitlines()
    exe = out / "players/mac/Statecraft.app/Contents/MacOS/Statecraft Israel"
    assert f"{hashlib.sha256(b'player').hexdigest()}  Statecraft.app/Contents/MacOS/Statecraft Israel" in lines
    assert any(line.endswith("  event-photos.json") for line in lines)
    assert lines == sorted(lines, key=lambda l: l.split("  ", 1)[1])
    assert mac["file_list_sha256"] == hashlib.sha256(manifest.read_bytes()).hexdigest()
    for name, asset in (("mac", "statecraft-israel-v0.1.0-macos-universal.zip"),
                        ("linux", "statecraft-israel-v0.1.0-linux-x64.tar.gz"),
                        ("windows", "statecraft-israel-v0.1.0-windows-x64.zip")):
        archive = out / asset
        assert arts[name]["archive"] == {"name": asset, "bytes": archive.stat().st_size,
                                         "sha256": hashlib.sha256(archive.read_bytes()).hexdigest()}
    assert mac["files"] == len(lines)
    assert mac["bytes"] == sum(p.stat().st_size for p in (out / "players/mac").rglob("*") if p.is_file())
    assert exe.is_file()

    assert mac["golden"]["status"] == "pass"
    assert mac["golden"]["lines"] == [f"GOLDEN OK 1950 {H1950}", f"GOLDEN OK 1952 {H1952}"]
    assert mac["golden"]["exit_code"] == 0
    for name in ("linux", "windows"):
        assert arts[name]["golden"]["status"] == "unchecked"
        assert "CI" in arts[name]["golden"]["note"]

    md = (out / "build-record.md").read_text()
    assert SHA in md and "6000.3.25f1" in md and mac["archive"]["sha256"] in md and mac["archive"]["name"] in md and "unchecked" in md and "pass" in md
    printed = capsys.readouterr().out
    assert str(out / "build-record.json") in printed


def test_the_golden_runs_on_the_mac_player_headless_under_the_lock(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    run_main(["--platform", "all", "--out", str(tmp_path / "out")], repo, editor, fake, monkeypatch)
    goldens = [c for c in fake.calls if "-golden" in c]
    assert len(goldens) == 1, "only the macOS player can run on the Mac"
    c = goldens[0]
    head = c[:c.index("--")]
    assert head[2].endswith("tools/machine_lock.py") and "--shared" in head
    player = c[c.index("--") + 1]
    assert player.endswith("Statecraft.app/Contents/MacOS/Statecraft Israel")
    assert {"-batchmode", "-nographics", "-golden"} <= set(c), "a player may run -nographics (docs/determinism.md)"
    assert Path(c[c.index("-logFile") + 1]).is_absolute()


def test_a_failed_golden_is_recorded_and_fails_the_run(repo, editor, monkeypatch, tmp_path):
    fake = Fake(golden_ok=False)
    out = tmp_path / "out"
    assert run_main(["--platform", "mac", "--out", str(out)], repo, editor, fake, monkeypatch) == 1
    mac = json.loads((out / "build-record.json").read_text())["artifacts"][0]
    assert mac["golden"]["status"] == "fail" and mac["golden"]["exit_code"] == 1
    assert any(line.startswith("GOLDEN FAIL 1952") for line in mac["golden"]["lines"])


def test_golden_lines_must_match_the_pinned_hashes_not_just_say_ok():
    expected = {"1950": H1950, "1952": H1952}
    assert rb.golden_status(f"GOLDEN OK 1950 {H1950}\nGOLDEN OK 1952 {H1952}\n", 0, expected) == "pass"
    assert rb.golden_status(f"GOLDEN OK 1950 {H1950}\nGOLDEN OK 1952 {'0' * 64}\n", 0, expected) == "fail"
    assert rb.golden_status(f"GOLDEN OK 1950 {H1950}\n", 0, expected) == "fail", "both years are required"
    assert rb.golden_status(f"GOLDEN OK 1950 {H1950}\nGOLDEN OK 1952 {H1952}\n", 1, expected) == "fail"


def test_symlinks_are_listed_by_target_not_followed(tmp_path):
    folder = tmp_path / "p"
    (folder / "A.app/Contents").mkdir(parents=True)
    (folder / "A.app/Contents/real").write_bytes(b"x")
    (folder / "A.app/Contents/link").symlink_to("real")
    text, files, size = rb.tree_manifest(folder)
    assert f"{hashlib.sha256(b'x').hexdigest()}  A.app/Contents/real" in text.splitlines()
    assert any(line.endswith("A.app/Contents/link -> real") for line in text.splitlines())
    assert files == 2 and size == 1


# --- the archives (the asset-name contract with the CI player check, PR #533) ----------------------------------------------

def test_archive_names_follow_the_ci_contract():
    assert rb.archive_name("v0.2.0", "windows") == "statecraft-israel-v0.2.0-windows-x64.zip"
    assert rb.archive_name("v0.2.0", "linux") == "statecraft-israel-v0.2.0-linux-x64.tar.gz"
    assert rb.archive_name("v0.2.0-rc.1", "mac") == "statecraft-israel-v0.2.0-rc.1-macos-universal.zip"


def test_the_mac_archive_is_made_with_ditto_keeping_the_bundle(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    out = tmp_path / "out"
    run_main(["--platform", "mac", "--tag", "v0.2.0", "--out", str(out)], repo, editor, fake, monkeypatch)
    ditto = next(c for c in fake.calls if c[0] == "ditto")
    assert ditto[1:4] == ["-c", "-k", "--keepParent"]
    assert ditto[-2] == str(out / "players/mac/Statecraft.app")
    assert ditto[-1] == str(out / "statecraft-israel-v0.2.0-macos-universal.zip")


def test_the_linux_and_windows_archives_hold_the_player_folder(repo, editor, monkeypatch, tmp_path):
    import tarfile
    import zipfile
    out = tmp_path / "out"
    assert run_main(["--platform", "all", "--out", str(out)], repo, editor, Fake(), monkeypatch) == 0
    with tarfile.open(out / "statecraft-israel-v0.1.0-linux-x64.tar.gz") as t:
        assert sorted(t.getnames()) == ["Statecraft.x86_64", "event-photos.json"]
    with zipfile.ZipFile(out / "statecraft-israel-v0.1.0-windows-x64.zip") as z:
        assert sorted(z.namelist()) == ["Statecraft.exe", "event-photos.json"]


def test_the_linux_archive_keeps_the_executable_bit(tmp_path):
    import tarfile
    folder = tmp_path / "linux"
    folder.mkdir()
    exe = folder / "Statecraft.x86_64"
    exe.write_bytes(b"elf")
    exe.chmod(0o755)
    rb.make_archive(rb.PLATFORMS["linux"], folder, exe, tmp_path / "a.tar.gz")
    with tarfile.open(tmp_path / "a.tar.gz") as t:
        assert t.getmember("Statecraft.x86_64").mode & 0o111


def test_a_bad_tag_is_refused(repo, editor, monkeypatch, tmp_path):
    fake = Fake()
    assert run_main(["--platform", "mac", "--tag", "0.1.0", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []


def test_a_folder_with_two_players_breaks_the_contract(tmp_path):
    folder = tmp_path / "w"
    folder.mkdir()
    for name in ("Statecraft.exe", "Other.exe", "UnityCrashHandler64.exe"):
        (folder / name).write_bytes(b"x")
    assert "found 2" in rb.player_problem(rb.PLATFORMS["windows"], folder)
    (folder / "Other.exe").unlink()
    assert rb.player_problem(rb.PLATFORMS["windows"], folder) is None, "the crash handler does not count"


def test_a_boot_fail_line_fails_the_golden_even_with_both_ok_lines():
    expected = {"1950": H1950, "1952": H1952}
    log = f"GOLDEN OK 1950 {H1950}\nGOLDEN OK 1952 {H1952}\nBOOT FAIL could not read content\n"
    assert rb.golden_status(log, 0, expected) == "fail"
