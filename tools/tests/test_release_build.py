"""tools/release_build.py: the local desktop release script (#482 Phases 0-1). Every subprocess (git, Unity, the player,
the lock) is faked; nothing here starts Unity or touches the network."""
import hashlib
import json
from pathlib import Path

import pytest

import release_build as rb

H1950 = "ca953c983faa2a85216efdf264bf1da9167c9787a06d21a73c76717dfab0f8b4"
H1952 = "b42380ab7935e4daabd0577fa3df231c474f3ad92f6006af0a05918a46743915"
SHA = "1b7a36cb" + "0" * 32
LICENCE_TEXTS = ("CC-BY-4.0.txt", "CC-BY-SA-4.0.txt", "CC0-1.0.txt", "YamlDotNet-MIT.txt")
NOTICES = ["LICENSE", "LICENSES/CC-BY-4.0.txt", "LICENSES/CC-BY-SA-4.0.txt", "LICENSES/CC0-1.0.txt",
           "LICENSES/YamlDotNet-MIT.txt", "NOTICE", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt", "THIRD-PARTY-NOTICES.md"]
FONT_SOURCES = {"OFL-EBGaramond.txt": "unity/Assets/UI/Fonts/OFL.txt",
                "OFL-FrankRuhlLibre.txt": "unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt"}


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
    (root / "sim/Statecraft.Sim.Tests/golden/manifest.txt").write_text(f"# pinned\nhash1950 {H1950}\nhash1952 {H1952}\nsmoke5 {'c' * 64}\n")
    for name in ("LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md"):
        (root / name).write_text(f"{name} text\n")
    (root / "LICENSES").mkdir()
    for name in LICENCE_TEXTS:
        (root / "LICENSES" / name).write_text(f"{name} text\n")
    for name, src in FONT_SOURCES.items():
        (root / src).parent.mkdir(parents=True, exist_ok=True)
        (root / src).write_text(f"{name}: Copyright line of this font\n")
    return root


@pytest.fixture
def editor(tmp_path):
    """A fake Unity install with every module of the three platforms; tests remove one to make it missing."""
    root = tmp_path / "Editor" / "6000.3.25f1"
    exe = root / "Unity.app/Contents/MacOS/Unity"
    exe.parent.mkdir(parents=True)
    exe.write_text("")
    write_editor_version(exe, "6000.3.25f1 (e1dba0a9aba4)")
    for module in rb.PLATFORMS.values():
        (root / module.module_dir).mkdir(parents=True, exist_ok=True)
    return exe


def write_editor_version(exe, version):
    (exe.parents[1] / "Info.plist").write_bytes(
        b'<?xml version="1.0" encoding="UTF-8"?><plist version="1.0"><dict><key>CFBundleGetInfoString</key>'
        + f"<string>Unity version {version}. (c) 2005-2026 Unity Technologies.</string></dict></plist>".encode())


class Fake:
    """Records every command and plays git, Unity and the player."""

    def __init__(self, dirty="", golden_ok=True, build_ok=True, branch="main", tags=("v0.1.0",), worktrees=None,
                 build_raises=None, dirty_after="", on_origin_main=True):
        self.calls: list[list[str]] = []
        self.dirty, self.golden_ok, self.build_ok = dirty, golden_ok, build_ok
        self.branch, self.tags, self.worktrees = branch, tags, worktrees
        self.build_raises, self.dirty_after, self.built = build_raises, dirty_after, False
        self.on_origin_main = on_origin_main

    def __call__(self, cmd, **kw):
        cmd = [str(c) for c in cmd]
        self.calls.append(cmd)
        out, code = "", 0
        if cmd[:2] == ["git", "status"]:
            out = self.dirty_after if self.built else self.dirty
        elif cmd[:2] == ["git", "rev-parse"]:
            out = self.branch + "\n" if "--abbrev-ref" in cmd else SHA + "\n"
        elif cmd[:2] == ["git", "merge-base"]:
            code = 0 if self.on_origin_main else 1
        elif cmd[:3] == ["git", "tag", "--points-at"]:
            out = "".join(t + "\n" for t in self.tags)
        elif cmd[:3] == ["git", "worktree", "list"]:
            out = "".join(f"worktree {w}\nHEAD {SHA}\nbranch refs/heads/x\n\n" for w in self.worktrees or [])
        elif cmd[:2] == ["git", "describe"]:
            out = "v0.1.0-5-g1b7a36cb\n"
        elif cmd[:2] == ["git", "check-ignore"]:
            code = 0   # as if git ignored every path: being ignored must not make a folder in a checkout acceptable
        elif "-executeMethod" in cmd:
            self.built = True
            if self.build_raises:
                raise self.build_raises
            target = Path(cmd[cmd.index("-out") + 1])
            log = Path(cmd[cmd.index("-logFile") + 1])
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
            if not self.build_ok:   # Unity failed, but a (partial or older) player is still there
                log.write_text("BUILD Failed StandaloneOSX -> x: 0.0 MB on disk, 3 errors, 12s\n")
                code = 1
        elif "-golden" in cmd:
            log = Path(cmd[cmd.index("-logFile") + 1])
            second = f"GOLDEN OK hash1952 {H1952}" if self.golden_ok else f"GOLDEN FAIL hash1952 {'f' * 64} expected {H1952}"
            log.write_text(f"boot\nGOLDEN OK hash1950 {H1950}\n{second}\nGOLDEN OK smoke5 {'c' * 64}\n")
            code = 0 if self.golden_ok else 1
        elif cmd[0] == "ditto":
            # Plays `ditto -c -k --norsrc --keepParent SRC ZIP`: a real zip, SRC's name as the top folder.
            import zipfile
            src, dest = Path(cmd[-2]), Path(cmd[-1])
            with zipfile.ZipFile(dest, "w") as z:
                for p in sorted(src.rglob("*")):
                    if p.is_file():
                        z.write(p, (Path(src.name) / p.relative_to(src)).as_posix())
        elif cmd[0] == "lipo":
            out = "x86_64 arm64\n"
        return rb.Result(code, out)


def run_main(argv, repo, editor, fake, monkeypatch):
    monkeypatch.setattr(rb, "REPO", repo)
    monkeypatch.setattr(rb, "run", fake)
    if fake.worktrees is None:
        fake.worktrees = [repo]
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


def test_an_output_folder_inside_the_repository_is_refused(repo, editor, monkeypatch):
    fake = Fake()
    assert run_main(["--platform", "mac", "--out", str(repo / "builds")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []


@pytest.mark.parametrize("inside", ["unity/Assets/StreamingAssets/content/r", "unity/Library/out", "unity/Builds/r",
                                    ".worktrees/x/out"])
def test_an_ignored_folder_inside_the_repository_is_refused_too(repo, editor, monkeypatch, inside):
    """Ignored is not safe: ContentSync deletes StreamingAssets/content on every build (ContentManifest.cs), which would
    take an earlier platform's player with it (review of #542, item 2 and mutation M4)."""
    fake = Fake()
    assert run_main(["--platform", "all", "--out", str(repo / inside)], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []


def test_the_contentsync_wipe_cannot_reach_the_output(repo, editor, monkeypatch):
    """--platform all into StreamingAssets/content: refused before the first build, so no later build can wipe it."""
    fake = Fake()
    out = repo / "unity/Assets/StreamingAssets/content/r"
    assert run_main(["--platform", "all", "--out", str(out)], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == [] and not out.exists()


def test_a_case_variant_of_the_repository_path_is_refused(repo, editor, monkeypatch):
    """APFS is case-insensitive: /…/REPO/unity/Assets/out is the repository's own unity/Assets."""
    fake = Fake()
    variant = Path(str(repo).upper()) / "unity/Assets/out"
    assert run_main(["--platform", "mac", "--out", str(variant)], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []


def test_a_dotdot_or_symlink_into_the_repository_is_refused(repo, editor, monkeypatch, tmp_path):
    link = tmp_path / "link"
    link.symlink_to(repo / "unity")
    for out in (link / "Assets/out", tmp_path / "elsewhere/../repo/unity/x"):
        fake = Fake()
        assert run_main(["--platform", "mac", "--out", str(out)], repo, editor, fake, monkeypatch) == 1, out
        assert unity_calls(fake) == []


def test_every_checkout_of_the_repository_is_refused(repo, editor, monkeypatch, tmp_path):
    """The main checkout and every other worktree, from `git worktree list --porcelain`, not only the running one."""
    main, other = tmp_path / "main-checkout", tmp_path / "wt" / "other"
    for root in (main, other):
        (root / "unity/Assets").mkdir(parents=True)
    for out in (main / "unity/Assets/rel", other / "unity/Assets/rel", main / "x"):
        fake = Fake(worktrees=[main, repo, other])
        assert run_main(["--platform", "mac", "--out", str(out)], repo, editor, fake, monkeypatch) == 1, out
        assert unity_calls(fake) == []


def test_a_non_empty_output_folder_is_refused(repo, editor, monkeypatch, tmp_path):
    out = tmp_path / "out"
    (out / "players/linux").mkdir(parents=True)
    (out / "players/linux/stale.so").write_bytes(b"old")
    fake = Fake()
    assert run_main(["--platform", "linux", "--out", str(out)], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []
    empty = tmp_path / "empty"
    empty.mkdir()
    assert run_main(["--platform", "mac", "--out", str(empty)], repo, editor, Fake(), monkeypatch) == 0


# --- a release is main at its tag (spec 6.4 step 1) -------------------------------------------------------------------

def test_a_release_off_main_is_refused(repo, editor, monkeypatch, tmp_path, capsys):
    fake = Fake(branch="tools/feature", tags=("v0.1.0",))
    assert run_main(["--platform", "mac", "--tag", "v0.1.0", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == [] and "main" in capsys.readouterr().err


def test_a_release_whose_tag_is_not_at_head_is_refused(repo, editor, monkeypatch, tmp_path, capsys):
    fake = Fake(branch="main", tags=("v0.0.9",))
    assert run_main(["--platform", "mac", "--tag", "v0.1.0", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == [] and "v0.1.0" in capsys.readouterr().err
    fake = Fake(branch="main", tags=())
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "p")], repo, editor, fake, monkeypatch) == 1, \
        "the default tag v<bundleVersion> must be at HEAD too"


def test_a_release_on_main_at_its_tag_runs(repo, editor, monkeypatch, tmp_path):
    fake = Fake(branch="main", tags=("v0.1.0-rc.2", "other"))
    out = tmp_path / "o"
    assert run_main(["--platform", "mac", "--tag", "v0.1.0-rc.2", "--out", str(out)], repo, editor, fake, monkeypatch) == 0
    assert (out / "statecraft-israel-v0.1.0-rc.2-macos-universal.zip").exists()


def test_a_dev_build_runs_anywhere_under_a_dev_marked_name(repo, editor, monkeypatch, tmp_path):
    """--dev: any branch, no tag needed, never a release name: v<bundleVersion>-dev.<commit8>, and no -shareable."""
    fake = Fake(branch="tools/feature", tags=())
    out = tmp_path / "o"
    assert run_main(["--platform", "mac", "--dev", "--out", str(out)], repo, editor, fake, monkeypatch) == 0
    assert (out / "statecraft-israel-v0.1.0-dev.1b7a36cb-macos-universal.zip").exists()
    record = json.loads((out / "build-record.json").read_text())
    assert record["tag"] == "v0.1.0-dev.1b7a36cb" and record["release"] is False and record["branch"] == "tools/feature"


def test_a_dev_build_takes_no_release_tag(repo, editor, monkeypatch, tmp_path):
    fake = Fake(branch="main", tags=("v1.0.0",))
    assert run_main(["--platform", "mac", "--dev", "--tag", "v1.0.0", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []


# --- the editor must be the project's version -------------------------------------------------------------------------

def test_an_editor_of_another_version_is_refused(repo, editor, monkeypatch, tmp_path, capsys):
    write_editor_version(editor, "6000.4.1f1 (0123456789ab)")
    for extra in ([], ["--dev"]):
        fake = Fake()
        assert run_main(["--platform", "mac", *extra, "--out", str(tmp_path / f"o{len(extra)}")], repo, editor, fake, monkeypatch) == 1
        assert unity_calls(fake) == []
    err = capsys.readouterr().err
    assert "6000.4.1f1" in err and "6000.3.25f1" in err


def test_the_editor_version_is_read_from_its_info_plist(editor):
    assert rb.editor_version(editor) == ("6000.3.25f1", "e1dba0a9aba4")


# --- the mutants that survived the review ------------------------------------------------------------------------------

def test_the_settings_are_restored_when_the_build_call_raises(repo, editor, monkeypatch, tmp_path):
    """Mutation M2: the try/finally around the build. A Ctrl-C while Unity runs still drops the settings churn."""
    fake = Fake(build_raises=KeyboardInterrupt())
    with pytest.raises(KeyboardInterrupt):
        run_main(["--platform", "mac", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch)
    build = next(i for i, c in enumerate(fake.calls) if "-executeMethod" in c)
    assert [c for c in fake.calls[build + 1:] if c[:3] == ["git", "checkout", "--"]]


def test_a_failed_unity_exit_fails_even_when_a_player_is_left_behind(repo, editor, monkeypatch, tmp_path):
    """Mutation M1: Unity's exit code counts, not only whether the target exists."""
    fake = Fake(build_ok=False)
    out = tmp_path / "o"
    assert run_main(["--platform", "mac", "--out", str(out)], repo, editor, fake, monkeypatch) == 1
    assert (out / "players/mac/Statecraft.app").exists() and not (out / "build-record.json").exists()
    assert not [c for c in fake.calls if "-golden" in c]


def test_an_empty_or_malformed_manifest_never_passes():
    assert rb.golden_status("GOLDEN OK one nope\n", 0, {"one": "nope"}) == "fail"
    assert rb.golden_status("", 0, {}) == "fail"


# --- the record's tree state ------------------------------------------------------------------------------------------

def test_the_record_says_whether_the_tree_stayed_clean(repo, editor, monkeypatch, tmp_path):
    fake = Fake(dirty_after=" M unity/Packages/packages-lock.json\n")
    out = tmp_path / "o"
    assert run_main(["--platform", "mac", "--out", str(out)], repo, editor, fake, monkeypatch) == 0
    record = json.loads((out / "build-record.json").read_text())
    assert record["tree_clean"] is True, "checked before the builds"
    assert record["tree_clean_after_builds"] is False
    assert record["dirty_after_builds"] == ["M unity/Packages/packages-lock.json"]
    describe = next(i for i, c in enumerate(fake.calls) if c[:2] == ["git", "describe"])
    assert describe < next(i for i, c in enumerate(fake.calls) if "-executeMethod" in c), "describe is taken before"


# --- what the script reads ------------------------------------------------------------------------------------------------

def test_unity_version_and_goldens_come_from_the_repository(repo, monkeypatch):
    monkeypatch.setattr(rb, "REPO", repo)
    assert rb.unity_version() == ("6000.3.25f1", "e1dba0a9aba4")
    assert rb.expected_goldens() == {"hash1950": H1950, "hash1952": H1952, "smoke5": "c" * 64}
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
            assert c[1] in {"status", "rev-parse", "describe", "checkout", "tag", "worktree", "merge-base"}, c
        assert c[0] not in {"gh", "curl", "wget", "scp", "rsync", "steamcmd", "xcrun"}, c
        assert c[:2] not in (["git", "fetch"], ["git", "pull"], ["git", "ls-remote"]), "no network, not even a fetch"
        assert "push" not in c and "commit" not in c
        if c[:2] == ["git", "tag"]:
            assert c[2] == "--points-at", "tags are only read"
        if c[:2] == ["git", "worktree"]:
            assert c[2] == "list", "worktrees are only read"
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
    assert not any(line.endswith("event-photos.json") for line in lines), "the list is exactly what the zip holds"
    assert all(line.split("  ", 1)[1].startswith("Statecraft.app/") or line.split("  ", 1)[1] in NOTICES for line in lines)
    assert [l.split("  ", 1)[1] for l in lines if not l.split("  ", 1)[1].startswith("Statecraft.app/")] == NOTICES
    photos = out / mac["photo_manifest"]["file"]
    assert photos.name == "event-photos-mac.json" and photos.read_text() == "{}\n"
    assert mac["photo_manifest"]["sha256"] == hashlib.sha256(photos.read_bytes()).hexdigest()
    assert lines == sorted(lines, key=lambda l: l.split("  ", 1)[1])
    assert mac["file_list_sha256"] == hashlib.sha256(manifest.read_bytes()).hexdigest()
    for name, asset in (("mac", "statecraft-israel-v0.1.0-macos-universal.zip"),
                        ("linux", "statecraft-israel-v0.1.0-linux-x64.tar.gz"),
                        ("windows", "statecraft-israel-v0.1.0-windows-x64.zip")):
        archive = out / asset
        assert arts[name]["archive"] == {"name": asset, "bytes": archive.stat().st_size,
                                         "sha256": hashlib.sha256(archive.read_bytes()).hexdigest()}
    assert mac["files"] == len(lines)
    assert mac["bytes"] == sum(p.stat().st_size for p in (out / "players/mac/Statecraft.app").rglob("*") if p.is_file()) \
        + sum((out / "players/mac" / n).stat().st_size for n in NOTICES)
    assert exe.is_file()

    assert mac["golden"]["status"] == "pass"
    assert mac["golden"]["lines"] == [f"GOLDEN OK hash1950 {H1950}", f"GOLDEN OK hash1952 {H1952}", f"GOLDEN OK smoke5 {'c' * 64}"]
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
    assert any(line.startswith("GOLDEN FAIL hash1952") for line in mac["golden"]["lines"])


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
    fake = Fake(tags=("v0.1.0",))
    out = tmp_path / "out"
    run_main(["--platform", "mac", "--tag", "v0.1.0", "--out", str(out)], repo, editor, fake, monkeypatch)
    ditto = next(c for c in fake.calls if c[0] == "ditto")
    assert ditto[1:5] == ["-c", "-k", "--norsrc", "--keepParent"], "no AppleDouble ._* entries the file list lacks"
    assert ditto[-2] == str(out / "players/mac/Statecraft.app")
    assert ditto[-1] == str(out / "statecraft-israel-v0.1.0-macos-universal.zip")


def test_the_linux_and_windows_archives_hold_the_player_folder(repo, editor, monkeypatch, tmp_path):
    import tarfile
    import zipfile
    out = tmp_path / "out"
    assert run_main(["--platform", "all", "--out", str(out)], repo, editor, Fake(), monkeypatch) == 0
    with tarfile.open(out / "statecraft-israel-v0.1.0-linux-x64.tar.gz") as t:
        assert [m.name for m in t.getmembers() if m.isfile()] == sorted(NOTICES + ["Statecraft.x86_64"])
    with zipfile.ZipFile(out / "statecraft-israel-v0.1.0-windows-x64.zip") as z:
        assert [n for n in z.namelist() if not n.endswith("/")] == sorted(NOTICES + ["Statecraft.exe"])


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


def test_unitys_do_not_ship_folders_are_left_out_of_the_list_and_the_archives(tmp_path):
    """IL2CPP and Burst leave Statecraft_BackUpThisFolder_ButDontShipItWithYourGame (2 GB in the first real run) and
    <product>_BurstDebugInformation_DoNotShip beside the player; neither is part of the release."""
    import zipfile
    folder = tmp_path / "w"
    (folder / "Statecraft_BackUpThisFolder_ButDontShipItWithYourGame").mkdir(parents=True)
    (folder / "Statecraft_BackUpThisFolder_ButDontShipItWithYourGame/big.cpp").write_bytes(b"x" * 10)
    (folder / "Statecraft Israel_BurstDebugInformation_DoNotShip").mkdir()
    (folder / "Statecraft Israel_BurstDebugInformation_DoNotShip/d.pdb").write_bytes(b"y")
    (folder / "Statecraft.exe").write_bytes(b"exe")
    text, files, size = rb.tree_manifest(folder)
    assert text.splitlines() == [f"{hashlib.sha256(b'exe').hexdigest()}  Statecraft.exe"] and (files, size) == (1, 3)
    rb.make_archive(rb.PLATFORMS["windows"], folder, folder / "Statecraft.exe", tmp_path / "w.zip")
    assert zipfile.ZipFile(tmp_path / "w.zip").namelist() == ["Statecraft.exe"]


# --- review of #542, round 3 ----------------------------------------------------------------------------------------------

def test_a_release_tag_must_be_the_bundle_version(repo, editor, monkeypatch, tmp_path, capsys):
    """A v2.0.0 tag at HEAD with bundleVersion 0.1.0 would name archives after a version the player does not have."""
    fake = Fake(tags=("v2.0.0",))
    assert run_main(["--platform", "mac", "--tag", "v2.0.0", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == [] and "0.1.0" in capsys.readouterr().err
    fake = Fake(tags=("v0.1.0-rc.1",))
    assert run_main(["--platform", "mac", "--tag", "v0.1.0-rc.1", "--out", str(tmp_path / "p")], repo, editor, fake, monkeypatch) == 0, \
        "a pre-release suffix on the bundle version is a release of that version"


def test_a_malformed_tag_at_head_is_refused(repo, editor, monkeypatch, tmp_path, capsys):
    """Pins the tag-format check itself: this tag is at HEAD and starts with v<bundleVersion>-."""
    fake = Fake(tags=("v0.1.0-rc_1",))
    assert run_main(["--platform", "mac", "--tag", "v0.1.0-rc_1", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == [] and "not a release tag" in capsys.readouterr().err


def test_a_release_must_be_on_origin_main(repo, editor, monkeypatch, tmp_path, capsys):
    """A local main with commits GitHub's main lacks is not a release; the script reads origin/main and never fetches."""
    fake = Fake(on_origin_main=False)
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    err = capsys.readouterr().err
    assert unity_calls(fake) == [] and "origin/main" in err and "git fetch" in err
    check = next(c for c in fake.calls if c[:2] == ["git", "merge-base"])
    assert check == ["git", "merge-base", "--is-ancestor", "HEAD", "refs/remotes/origin/main"]
    fake = Fake(on_origin_main=False, branch="tools/x", tags=())
    assert run_main(["--platform", "mac", "--dev", "--out", str(tmp_path / "d")], repo, editor, fake, monkeypatch) == 0, \
        "a dev build may be anywhere"


def test_a_detached_head_says_so(repo, editor, monkeypatch, tmp_path, capsys):
    fake = Fake(branch="HEAD")
    assert run_main(["--platform", "mac", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert "HEAD is detached" in capsys.readouterr().err


def test_a_dangling_symlink_into_the_repository_is_refused(repo, editor, monkeypatch, tmp_path):
    link = tmp_path / "dangling"
    link.symlink_to(repo / "unity/Assets/NotYetThere")
    for extra in ([], ["--dry-run"]):
        fake = Fake()
        assert run_main(["--platform", "mac", "--dev", *extra, "--out", str(link / "x")], repo, editor, fake, monkeypatch) == 1
        assert unity_calls(fake) == []


@pytest.mark.parametrize("tag", ["v٠.١.٠", "v０.１.０", "v0.1.0-rc.١"])
def test_a_tag_with_non_ascii_digits_is_not_a_release_tag(tag):
    """\\d without re.ASCII matches Arabic-Indic and full-width digits; a tag reaches file names and the CI check."""
    assert rb.TAG.fullmatch(tag) is None


# --- the notices in every desktop archive (RELEASING.md step 7; P47) -------------------------------------------------------

def archive_files(path):
    import tarfile
    import zipfile
    if path.name.endswith(".tar.gz"):
        with tarfile.open(path) as t:
            return sorted(m.name for m in t.getmembers() if m.isfile() or m.issym())
    with zipfile.ZipFile(path) as z:
        return sorted(n for n in z.namelist() if not n.endswith("/"))


def test_every_archive_carries_the_notices_and_matches_its_file_list(repo, editor, monkeypatch, tmp_path):
    out = tmp_path / "out"
    assert run_main(["--platform", "all", "--out", str(out)], repo, editor, Fake(), monkeypatch) == 0
    record = json.loads((out / "build-record.json").read_text())
    for a in record["artifacts"]:
        names = archive_files(out / a["archive"]["name"])
        for notice in NOTICES:
            assert notice in names, (a["platform"], notice)
            assert (zip_text(out / a["archive"]["name"], notice)) == (repo / FONT_SOURCES.get(notice, notice)).read_text()
        listed = sorted(l.split("  ", 1)[1].split(" -> ")[0] for l in (out / a["file_list"]).read_text().splitlines())
        assert listed == names, f"{a['platform']}: the file list is exactly the archive"


def zip_text(path, name):
    import tarfile
    import zipfile
    if path.name.endswith(".tar.gz"):
        with tarfile.open(path) as t:
            return t.extractfile(name).read().decode()
    with zipfile.ZipFile(path) as z:
        return z.read(name).decode()


def test_the_mac_zip_keeps_the_app_whole_with_the_notices_beside_it_and_no_appledouble(repo, editor, monkeypatch, tmp_path):
    out = tmp_path / "out"
    fake = Fake()
    assert run_main(["--platform", "mac", "--out", str(out)], repo, editor, fake, monkeypatch) == 0
    names = archive_files(out / "statecraft-israel-v0.1.0-macos-universal.zip")
    assert not [n for n in names if Path(n).name.startswith("._")]
    tops = sorted({n.split("/")[0] for n in names})
    assert tops == sorted({"Statecraft.app", "LICENSE", "LICENSES", "NOTICE", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt",
                           "THIRD-PARTY-NOTICES.md"})
    ditto = next(c for c in fake.calls if c[0] == "ditto")
    assert "--norsrc" in ditto and ditto[-2].endswith("Statecraft.app"), "the bundle is still zipped by ditto"


def test_the_one_player_rule_still_holds_with_the_notices(repo, editor, monkeypatch, tmp_path):
    out = tmp_path / "out"
    assert run_main(["--platform", "all", "--out", str(out)], repo, editor, Fake(), monkeypatch) == 0
    for name in ("mac", "linux", "windows"):
        assert rb.player_problem(rb.PLATFORMS[name], out / "players" / name) is None


@pytest.mark.parametrize("missing", ["NOTICE", "LICENSES/CC-BY-4.0.txt", "unity/Assets/UI/Fonts/OFL.txt",
                                     "unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt"])
def test_a_missing_notice_file_is_refused_before_any_build(repo, editor, monkeypatch, tmp_path, capsys, missing):
    (repo / missing).unlink()
    fake = Fake()
    assert run_main(["--platform", "linux", "--out", str(tmp_path / "o")], repo, editor, fake, monkeypatch) == 1
    assert unity_calls(fake) == []
    assert missing in capsys.readouterr().err


def test_an_archive_without_its_notices_is_caught(tmp_path):
    import zipfile
    z = tmp_path / "a.zip"
    with zipfile.ZipFile(z, "w") as f:
        f.writestr("Statecraft.exe", "x")
        f.writestr("LICENSE", "x")
    assert "NOTICE" in rb.missing_notices(z)
    assert rb.missing_notices(z) == sorted(set(NOTICES) - {"LICENSE"})


def test_the_notice_list_is_the_web_packagers():
    """One list: the files scripts/oss/package_web.sh copies and checks in the web zip (index.html aside)."""
    import re
    script = (Path(rb.__file__).resolve().parents[1] / "scripts/oss/package_web.sh").read_text()
    loop = re.search(r"for f in (.*?); do", script, re.S).group(1).replace("\\\n", " ")
    web = sorted(set(loop.split()) - {"index.html"})
    assert sorted(rb.REQUIRED_NOTICES) == web == NOTICES


def test_every_shipped_fonts_licence_is_a_required_notice():
    """The OFL asks that each font's copyright notice and licence travel with it: every OFL.txt under
    unity/Assets/UI/Fonts (one per font folder) is shipped under its own name, so the next font cannot be missed."""
    root = Path(rb.__file__).resolve().parents[1]
    found = sorted(p.relative_to(root).as_posix() for p in (root / "unity/Assets/UI/Fonts").rglob("OFL.txt"))
    assert found == sorted(FONT_SOURCES.values())
    assert sorted(rb.FONT_LICENCES.values()) == found
    for name in rb.FONT_LICENCES:
        assert name in rb.REQUIRED_NOTICES
    assert rb.FONT_LICENCES == FONT_SOURCES, "the names site/public/fonts already uses"


def test_the_golden_rule_wants_the_exact_bare_lines_the_player_prints():
    """Review T53 S-B: the real Mac player log (the #542 run) has the GOLDEN lines bare, one per line. A prefixed line is
    not that format, and a hash with one more hex character is a different hash; both fail, as in CI (#533)."""
    expected = {"1950": H1950, "1952": H1952}
    assert rb.golden_status(f"Initialize engine\nGOLDEN OK 1950 {H1950}\r\nGOLDEN OK 1952 {H1952}\n", 0, expected) == "pass"
    assert rb.golden_status(f"12:00:01 GOLDEN OK 1950 {H1950}\n12:00:02 GOLDEN OK 1952 {H1952}\n", 0, expected) == "fail"
    assert rb.golden_status(f"GOLDEN OK 1950 {H1950}a\nGOLDEN OK 1952 {H1952}\n", 0, expected) == "fail"
    assert rb.golden_lines(f"GOLDEN OK 1950 {H1950}a\n") == [f"GOLDEN OK 1950 {H1950}a"], "the record shows what was printed"
