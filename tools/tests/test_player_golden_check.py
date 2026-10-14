import hashlib
import io
import json
import re
import shutil
import subprocess
import sys
import tarfile
import zipfile
from pathlib import Path

import pytest

import player_golden_check as pg

REPO = Path(__file__).resolve().parents[2]
H50, H52 = "a" * 64, "b" * 64
MANIFEST = f"# comment\nhash1950 {H50}\nhash1952 {H52}\nsmoke5 {'c' * 64}\n"
EXPECTED = [f"GOLDEN OK 1950 {H50}", f"GOLDEN OK 1952 {H52}"]


def digest(name):
    return "sha256:" + hashlib.sha256(name.encode()).hexdigest()


def release(tag, *names, draft=False):
    return {"tagName": tag, "isDraft": draft, "assets": [{"name": n, "digest": digest(n)} for n in names]}


def players(tag):
    return [pg.asset_name(tag, p) for p in ("windows", "linux", "macos")]


def test_asset_names_are_the_release_script_contract():
    assert pg.asset_name("v0.2.0", "windows") == "statecraft-israel-v0.2.0-windows-x64.zip"
    assert pg.asset_name("v0.2.0", "linux") == "statecraft-israel-v0.2.0-linux-x64.tar.gz"
    assert pg.asset_name("v0.2.0-rc.1", "macos") == "statecraft-israel-v0.2.0-rc.1-macos-universal.zip"


TAGS = {"v0.2.0": True, "v10.20.30": True, "v0.2.0-rc.1": True, "v1.0.0-beta.2.x9": True,
        "0.2.0": False, "v0.2": False, "v0.2.0 ": False, "v0.2.0;id": False, "v0.2.0/../x": False, "main": False, "": False,
        "v1.2.3-": False, "v1.2.3-rc..1": False, "v1.0.0\nmatrix=x": False, "v1.0.0\n": False, "v\u0661.0.0": False,
        "v1.0.0-r\u00e9": False, "V1.0.0": False}


def test_the_workflows_shell_tag_check_accepts_exactly_what_the_python_one_does():
    text = (REPO / ".github/workflows/player-golden.yml").read_text(encoding="utf-8")
    shell_re = re.search(r"^\s*re='([^']+)'$", text, re.M).group(1)
    for tag, want in TAGS.items():
        assert bool(pg.TAG.fullmatch(tag)) is want, tag
        r = subprocess.run(["bash", "-c", 're=$1; [[ $2 =~ $re ]]', "_", shell_re, tag], env={"LC_ALL": "C"})
        assert (r.returncode == 0) is want, tag


@pytest.mark.parametrize("tag", [t for t, ok in TAGS.items() if not ok])
def test_anything_but_a_release_tag_is_refused(tag):
    with pytest.raises(ValueError):
        pg.asset_name(tag, "linux")
    include, msgs, ok = pg.plan(tag, release(tag), "workflow_dispatch")
    assert not ok and include == [] and msgs[0].startswith("::error::")


def test_plan_lists_all_three_players_with_githubs_digests():
    r = release("v0.2.0", "statecraft-israel-v0.2.0-web.zip", *players("v0.2.0"))
    include, msgs, ok = pg.plan("v0.2.0", r, "release")
    assert ok and msgs == []
    assert include == [
        {"platform": "windows", "runner": "windows-2025", "asset": "statecraft-israel-v0.2.0-windows-x64.zip",
         "sha256": digest("statecraft-israel-v0.2.0-windows-x64.zip")[7:]},
        {"platform": "linux", "runner": "ubuntu-24.04", "asset": "statecraft-israel-v0.2.0-linux-x64.tar.gz",
         "sha256": digest("statecraft-israel-v0.2.0-linux-x64.tar.gz")[7:]},
        {"platform": "macos", "runner": "macos-15", "asset": "statecraft-israel-v0.2.0-macos-universal.zip",
         "sha256": digest("statecraft-israel-v0.2.0-macos-universal.zip")[7:]},
    ]


@pytest.mark.parametrize("event", ["release", "workflow_dispatch"])
def test_a_partial_set_of_players_fails_and_runs_none(event):
    # Review M1: a release with only the Mac zip (already checked on the owner's Mac) must not pass green.
    r = release("v0.2.0", "statecraft-israel-v0.2.0-web.zip", players("v0.2.0")[2])
    include, msgs, ok = pg.plan("v0.2.0", r, event)
    assert not ok and include == []
    assert msgs == ["::error::release v0.2.0 has 1 of the 3 desktop players; missing: windows "
                    "(statecraft-israel-v0.2.0-windows-x64.zip), linux (statecraft-israel-v0.2.0-linux-x64.tar.gz). "
                    "Attach all three in the same gh release create (docs/RELEASING.md, step 6)"]


@pytest.mark.parametrize("bad", [None, "", "sha1:" + "a" * 40, "sha256:" + "A" * 64, "sha256:" + "a" * 63])
def test_an_asset_without_a_sha256_digest_fails(bad):
    r = release("v0.2.0", *players("v0.2.0"))
    r["assets"][1]["digest"] = bad
    include, msgs, ok = pg.plan("v0.2.0", r, "release")
    assert not ok and include == [] and "has no SHA-256 digest" in msgs[0]


def test_no_player_fails_a_manual_run_but_a_web_only_release_passes_unchecked(tmp_path, monkeypatch):
    r = release("v0.2.0", "statecraft-israel-v0.2.0-web.zip")
    include, msgs, ok = pg.plan("v0.2.0", r, "release")
    assert ok and include == [] and msgs[0].startswith("::warning::UNCHECKED: release v0.2.0 has no desktop player")
    assert "gh workflow run player-golden.yml -f tag=v0.2.0" in msgs[0]
    assert pg.plan("v0.2.0", r, "workflow_dispatch")[2] is False
    monkeypatch.setattr(sys, "stdin", io.StringIO(json.dumps(r)))
    out, summary = tmp_path / "out", tmp_path / "summary"
    assert pg.main(["plan", "--tag", "v0.2.0", "--event", "release", "--output", str(out), "--summary", str(summary)]) == 0
    assert out.read_text().splitlines()[1] == "count=0"
    assert summary.read_text().startswith("## Player golden v0.2.0: UNCHECKED")


def test_drafts_and_mismatched_releases_are_refused():
    assert pg.plan("v0.2.0", release("v0.2.0", draft=True), "release")[2] is False
    assert pg.plan("v0.2.0", release("v0.1.0"), "release")[2] is False


def test_plan_command_writes_the_matrix_to_the_github_output(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(sys, "stdin", io.StringIO(json.dumps(release("v1.0.0", *players("v1.0.0")))))
    out = tmp_path / "github_output"
    assert pg.main(["plan", "--tag", "v1.0.0", "--event", "release", "--output", str(out)]) == 0
    lines = out.read_text().splitlines()
    matrix = json.loads(lines[0].removeprefix("matrix="))
    assert [e["platform"] for e in matrix["include"]] == ["windows", "linux", "macos"]
    assert lines[1] == "count=3"


def test_expected_lines_come_from_the_manifest_and_the_real_manifest_has_them():
    assert pg.expected_lines(pg.read_manifest(MANIFEST)) == EXPECTED
    with pytest.raises(ValueError, match="hash1952"):
        pg.expected_lines(pg.read_manifest(f"hash1950 {H50}\n"))
    real = pg.read_manifest((REPO / "sim/Statecraft.Sim.Tests/golden/manifest.txt").read_text(encoding="utf-8"))
    assert [l.split()[2] for l in pg.expected_lines(real)] == ["1950", "1952"]


def test_a_passing_log_needs_both_lines_and_exit_zero():
    log = "Initialize engine\r\n" + "\r\n".join(EXPECTED) + "\r\n"
    assert pg.check_log(log, EXPECTED, 0) == []
    assert pg.check_log(log, EXPECTED, 1) == ["the player exited 1"]
    assert pg.check_log(log, EXPECTED, None) == ["the player did not exit before the timeout"]
    assert pg.check_log(EXPECTED[0], EXPECTED, 0) == [f"missing line: {EXPECTED[1]}"]


def test_a_wrong_hash_or_a_fail_line_fails():
    wrong = f"GOLDEN OK 1950 {H50}\nGOLDEN FAIL 1952 {'d' * 64} expected {H52}\n"
    assert pg.check_log(wrong, EXPECTED, 1) == ["the player exited 1", f"missing line: {EXPECTED[1]}",
                                                 f"failure line: GOLDEN FAIL 1952 {'d' * 64} expected {H52}"]
    assert pg.check_log("\n".join(EXPECTED) + "\nBOOT FAIL content missing", EXPECTED, 0) == [
        "failure line: BOOT FAIL content missing"]
    # A hash prefix or a line with more after it is not the line.
    assert pg.check_log(f"GOLDEN OK 1950 {H50[:8]}\nGOLDEN OK 1952 {H52}x", EXPECTED, 0) == [
        f"missing line: {EXPECTED[0]}", f"missing line: {EXPECTED[1]}"]


def test_find_player_finds_exactly_one(tmp_path):
    (tmp_path / "w" / "Statecraft_Data").mkdir(parents=True)
    (tmp_path / "w" / "Statecraft Israel.exe").write_bytes(b"")
    (tmp_path / "w" / "UnityCrashHandler64.exe").write_bytes(b"")
    assert pg.find_player(tmp_path / "w", "windows").name == "Statecraft Israel.exe"
    (tmp_path / "w" / "Other.exe").write_bytes(b"")
    with pytest.raises(ValueError, match="found 2"):
        pg.find_player(tmp_path / "w", "windows")
    (tmp_path / "l" / "statecraft").mkdir(parents=True)
    (tmp_path / "l" / "statecraft" / "Statecraft.x86_64").write_bytes(b"")
    assert pg.find_player(tmp_path / "l", "linux").name == "Statecraft.x86_64"
    macos = tmp_path / "m" / "Statecraft.app" / "Contents" / "MacOS"
    macos.mkdir(parents=True)
    (macos / "Statecraft Israel").write_bytes(b"")
    assert pg.find_player(tmp_path / "m", "macos").name == "Statecraft Israel"
    with pytest.raises(ValueError, match="found 0"):
        pg.find_player(tmp_path / "w", "linux")


def test_unpack_keeps_the_executable_bit_and_refuses_paths_that_leave_the_folder(tmp_path):
    exe = tmp_path / "Statecraft.x86_64"
    exe.write_bytes(b"#!/bin/sh\n")
    exe.chmod(0o755)
    good = tmp_path / "good.tar.gz"
    with tarfile.open(good, "w:gz") as t:
        t.add(exe, arcname="statecraft/Statecraft.x86_64")
    pg.unpack(good, tmp_path / "out")
    assert (tmp_path / "out/statecraft/Statecraft.x86_64").stat().st_mode & 0o111
    with pytest.raises(ValueError, match="not empty"):
        pg.unpack(good, tmp_path / "out")
    bad = tmp_path / "bad.tar.gz"
    with tarfile.open(bad, "w:gz") as t:
        t.add(exe, arcname="../escaped")
    with pytest.raises(tarfile.TarError):
        pg.unpack(bad, tmp_path / "out2")
    assert not (tmp_path / "escaped").exists()
    z = tmp_path / "p.zip"
    with zipfile.ZipFile(z, "w") as zf:
        zf.writestr("Statecraft/Statecraft Israel.exe", b"")
    pg.unpack(z, tmp_path / "out3")
    assert pg.find_player(tmp_path / "out3", "windows").name == "Statecraft Israel.exe"
    with pytest.raises(ValueError, match="not a .zip or .tar.gz"):
        pg.unpack(tmp_path / "good.7z", tmp_path / "out4")


@pytest.mark.skipif(sys.platform == "win32", reason="the fake player is a shell script")
def test_run_passes_a_player_that_prints_the_goldens_and_prints_the_log_when_it_does_not(tmp_path, capsys):
    (tmp_path / "manifest.txt").write_text(MANIFEST)

    def archive(name, script):
        exe = tmp_path / "Statecraft.x86_64"
        exe.write_text("#!/bin/sh\n" + script)
        exe.chmod(0o755)
        path = tmp_path / name
        with tarfile.open(path, "w:gz") as t:
            t.add(exe, arcname="statecraft/Statecraft.x86_64")
        return path

    # The fake player writes its log where -logFile says (the last argument), as the Unity player does.
    ok = archive("ok.tar.gz", "for a; do log=$a; done\n" + "".join(f"echo '{l}' >> \"$log\"\n" for l in EXPECTED))
    args = ["run", "--platform", "linux", "--manifest", str(tmp_path / "manifest.txt")]

    def sha(p):
        return ["--sha256", pg.sha256(p)]

    # Review S2: an archive that is not the one GitHub's digest names is refused before it is unpacked or run.
    assert pg.main(args + ["--archive", str(ok), "--sha256", "0" * 64, "--work", str(tmp_path / "w0")]) == 2
    out = capsys.readouterr().out
    assert "but GitHub's digest for the asset is" in out and not (tmp_path / "w0").exists()
    assert pg.main(args + ["--archive", str(ok), *sha(ok), "--work", str(tmp_path / "w1")]) == 0
    out = capsys.readouterr().out
    assert "PLAYER GOLDEN OK linux" in out and "ARCHIVE ok.tar.gz sha256 " in out and "-batchmode -nographics -golden -logFile" in out

    bad = archive("bad.tar.gz", "for a; do log=$a; done\necho 'GOLDEN FAIL 1950 x expected y' >> \"$log\"\nexit 1\n")
    assert pg.main(args + ["--archive", str(bad), *sha(bad), "--work", str(tmp_path / "w2")]) == 1
    out = capsys.readouterr().out
    assert "----- player log" in out and "GOLDEN FAIL 1950 x expected y" in out and "PLAYER GOLDEN FAIL linux" in out

    slow = archive("slow.tar.gz", "sleep 30\n")
    assert pg.main(args + ["--archive", str(slow), *sha(slow), "--work", str(tmp_path / "w3"), "--timeout", "1"]) == 1
    assert "did not exit before the timeout" in capsys.readouterr().out


@pytest.mark.skipif(sys.platform != "darwin" or not shutil.which("ditto"), reason="the macOS zip is unpacked with ditto")
def test_a_mac_app_zipped_with_ditto_comes_back_runnable(tmp_path):
    # The release script zips the app with ditto -c -k --norsrc --keepParent; unpack() uses ditto -x -k on macOS.
    macos = tmp_path / "build" / "Statecraft.app" / "Contents" / "MacOS"
    macos.mkdir(parents=True)
    exe = macos / "Statecraft Israel"
    exe.write_text("#!/bin/sh\n")
    exe.chmod(0o755)
    z = tmp_path / "statecraft-israel-v0.2.0-macos-universal.zip"
    subprocess.run(["ditto", "-c", "-k", "--norsrc", "--keepParent", str(tmp_path / "build" / "Statecraft.app"), str(z)],
                   check=True)
    pg.unpack(z, tmp_path / "out")
    found = pg.find_player(tmp_path / "out", "macos")
    assert found == tmp_path / "out" / "Statecraft.app" / "Contents" / "MacOS" / "Statecraft Israel"
    assert found.stat().st_mode & 0o111
