import hashlib
import io
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import tarfile
import time
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


def add_notices_to_tar(t):
    """The notices at the archive's top level, as tools/release_build.py adds them (#552)."""
    for n in pg.REQUIRED_NOTICES:
        info = tarfile.TarInfo(n)
        data = f"{n}\n".encode()
        info.size = len(data)
        t.addfile(info, io.BytesIO(data))


@pytest.fixture
def fake_xvfb_run(tmp_path, monkeypatch):
    """A stand-in for xvfb-run on PATH: it records its arguments, then runs the command as its child, as xvfb-run does
    (so a timeout must stop a grandchild). The file it records to is returned."""
    calls = tmp_path / "xvfb-run-calls.txt"
    bin_dir = tmp_path / "fakebin"
    bin_dir.mkdir()
    fake = bin_dir / "xvfb-run"
    fake.write_text(f'#!/bin/sh\necho "$@" >> "{calls}"\n[ "$1" = -a ] || exit 99\nshift\n"$@"\n')
    fake.chmod(0o755)
    monkeypatch.setenv("PATH", f"{bin_dir}{os.pathsep}{os.environ['PATH']}")
    return calls


def gone_soon(pid, seconds=3.0):
    """Whether pid is gone within `seconds`. A killed orphan lingers for some milliseconds until init (launchd on
    macOS) reaps it, so a check made at once can still find it."""
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        try:
            os.kill(pid, 0)
        except ProcessLookupError:
            return True
        time.sleep(0.01)
    return False


@pytest.fixture
def stop_once_ready(monkeypatch, tmp_path):
    """Returns arm(ready): it holds a timed-out run's stop until the fake player has written `ready`, which it does once
    its TERM trap is set. Under heavy load (#704) a 1-2 s timeout could fire before the shell got that far: the stop
    then killed a player that had not started yet, and the test found no file to read. If `ready` never appears the run
    is still stopped, and the test then fails, so a fake player that never starts cannot hang the test or let it pass.
    At teardown, after the test's own checks, it SIGKILLs whatever is left of each stopped run's process group, so a
    failing test (a stop that no longer sends SIGKILL) does not leave its fake player looping. It signals a group only
    while one of its processes runs from this test's tmp_path, so a reused group id is never touched."""
    groups, killpg = [], os.killpg

    def arm(ready, seconds=30.0):
        stop = pg.stop_group

        def stop_when_ready(proc, grace):
            groups.append(proc.pid)   # start_new_session: the group id is the run's pid
            deadline = time.monotonic() + seconds
            while not (started := ready.is_file() and ready.read_text().endswith("\n")) and time.monotonic() < deadline:
                time.sleep(0.01)
            stop(proc, grace)
            assert started, (f"the fake player did not write {ready.name} within {seconds} s, so it never started: "
                             "the machine is too loaded to run it (#704), and the stop itself was not tested")

        monkeypatch.setattr(pg, "stop_group", stop_when_ready)

    yield arm
    for pgid in groups:
        ps = subprocess.run(["ps", "-A", "-o", "pgid=,command="], capture_output=True, text=True, check=True).stdout
        ours = [line for line in ps.splitlines() if line.split()[:1] == [str(pgid)] and str(tmp_path) in line]
        if ours:
            try:
                killpg(pgid, signal.SIGKILL)
            except (ProcessLookupError, PermissionError):
                pass


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
        "v1.0.0-r\u00e9": False, "V1.0.0": False, "xv1.0.0": False, "refs/tags/v1.0.0": False}


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
    win, lin, mac = players("v0.2.0")
    assert include == [
        {"platform": "windows", "label": "windows", "runner": "windows-2025", "arch": "", "asset": win,
         "sha256": digest(win)[7:]},
        {"platform": "linux", "label": "linux", "runner": "ubuntu-24.04", "arch": "", "asset": lin,
         "sha256": digest(lin)[7:]},
        {"platform": "macos", "label": "macos arm64", "runner": "macos-15", "arch": "arm64", "asset": mac,
         "sha256": digest(mac)[7:]},
        {"platform": "macos", "label": "macos x86_64", "runner": "macos-15", "arch": "x86_64", "asset": mac,
         "sha256": digest(mac)[7:]},
    ]


def test_the_mac_player_runs_both_slices_on_apple_silicon():
    # Review S3: the Universal app has two native IL2CPP slices and each is checked on its own. #548: GitHub retires
    # macos-15-intel, its last x86_64 image, in August 2027, so the x86_64 slice runs under Rosetta on the arm64 runner
    # (`arch -x86_64`, MAC_SLICE) and no runner is an Intel one.
    assert pg.RUNNERS["macos"] == [("macos-15", "arm64"), ("macos-15", "x86_64")]
    include, _, ok = pg.plan("v0.2.0", release("v0.2.0", *players("v0.2.0")), "workflow_dispatch")
    assert ok and [(e["runner"], e["arch"]) for e in include if e["platform"] == "macos"] == [
        ("macos-15", "arm64"), ("macos-15", "x86_64")]
    assert not any("intel" in e["runner"] for e in include)
    assert len({e["label"] for e in include}) == len(include)   # the job names differ


def test_the_arch_check_catches_a_wrong_runner_or_a_missing_slice():
    exe = Path("Statecraft Israel")
    assert pg.check_arch(exe, "x86_64", "x86_64", "x86_64 arm64\n") == []
    assert pg.check_arch(exe, "arm64", "arm64", "x86_64 arm64\n") == []
    assert pg.check_arch(exe, "x86_64", "arm64", "x86_64 arm64") == ["the runner is arm64, not x86_64: check the runner label"]
    assert pg.check_arch(exe, "x86_64", "x86_64", "arm64") == ["Statecraft Israel has no x86_64 slice (lipo -archs: arm64)"]


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


def test_drafts_and_mismatched_releases_are_refused(tmp_path, monkeypatch):
    assert pg.plan("v0.2.0", release("v0.2.0", draft=True), "release")[2] is False
    assert pg.plan("v0.2.0", release("v0.1.0"), "release")[2] is False
    # The command exits 1 on a refusal and writes no matrix, so the player jobs are skipped behind a failed job.
    monkeypatch.setattr(sys, "stdin", io.StringIO(json.dumps(release("v0.2.0", *players("v0.2.0"), draft=True))))
    out = tmp_path / "out"
    assert pg.main(["plan", "--tag", "v0.2.0", "--event", "release", "--output", str(out)]) == 1
    assert not out.exists()


def test_plan_command_writes_the_matrix_to_the_github_output(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(sys, "stdin", io.StringIO(json.dumps(release("v1.0.0", *players("v1.0.0")))))
    out = tmp_path / "github_output"
    assert pg.main(["plan", "--tag", "v1.0.0", "--event", "release", "--output", str(out)]) == 0
    lines = out.read_text().splitlines()
    matrix = json.loads(lines[0].removeprefix("matrix="))
    assert [e["label"] for e in matrix["include"]] == ["windows", "linux", "macos arm64", "macos x86_64"]
    assert lines[1] == "count=4"


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


def test_the_linux_player_runs_under_a_virtual_x_display_and_the_others_run_bare(tmp_path):
    # #533 (P62): with no X or Wayland display, Unity 6's Linux player selects no window backend ("Selected window
    # backend: (null)") and segfaults in PlayerMain, -batchmode -nographics or not. xvfb-run gives it one; -nographics
    # still keeps the Null graphics device, so the runner needs no GPU.
    exe, log = tmp_path / "Statecraft.x86_64", tmp_path / "player.log"
    args = [str(exe), "-batchmode", "-nographics", "-golden", "-logFile", str(log.resolve())]
    assert pg.player_command(exe, log, "linux") == ["xvfb-run", "-a", *args]
    assert pg.player_command(exe, log, "windows") == args


def test_the_mac_player_runs_the_slice_it_checks(tmp_path):
    # #548: a Universal app started bare runs the runner's native slice, so each slice is started through `arch`: the
    # x86_64 one under Rosetta on an arm64 runner.
    exe, log = tmp_path / "Statecraft Israel", tmp_path / "player.log"
    args = [str(exe), "-batchmode", "-nographics", "-golden", "-logFile", str(log.resolve())]
    assert pg.player_command(exe, log, "macos", "x86_64") == ["arch", "-x86_64", *args]
    assert pg.player_command(exe, log, "macos", "arm64") == ["arch", "-arm64", *args]


@pytest.mark.skipif(sys.platform == "win32", reason="the fake player is a shell script")
def test_a_linux_run_without_xvfb_run_is_refused_before_the_player_runs(tmp_path, capsys, monkeypatch):
    (tmp_path / "manifest.txt").write_text(MANIFEST)
    arc = fake_linux_player(tmp_path, b"#!/bin/sh\n")
    monkeypatch.setenv("PATH", str(tmp_path / "nothing-here"))
    code = pg.main(["run", "--platform", "linux", "--manifest", str(tmp_path / "manifest.txt"), "--archive", str(arc),
                    "--sha256", pg.sha256(arc), "--work", str(tmp_path / "w")])
    out = capsys.readouterr().out
    assert code == 2 and "xvfb-run is not installed" in out and "RUN " not in out


@pytest.mark.skipif(sys.platform == "win32", reason="the fake player and xvfb-run are shell scripts")
def test_a_timeout_also_stops_what_xvfb_run_started(tmp_path, fake_xvfb_run, stop_once_ready):
    # xvfb-run runs the player as its child; killing only xvfb-run would leave the player running.
    exe = tmp_path / "Statecraft.x86_64"
    pid_file = tmp_path / "player.pid"
    stop_once_ready(pid_file)
    exe.write_text(f'#!/bin/sh\necho $$ > "{pid_file}"\nexec sleep 30\n')
    exe.chmod(0o755)
    assert pg.run_player(exe, tmp_path / "player.log", 2, "linux") is None
    assert gone_soon(int(pid_file.read_text())), "xvfb-run's child, the player, outlived the stop"


@pytest.mark.skipif(sys.platform == "win32", reason="the fake player and xvfb-run are shell scripts")
def test_a_timeout_asks_the_player_to_stop_before_killing_it(tmp_path, fake_xvfb_run, stop_once_ready):
    # Review #581 nit 1: SIGTERM first, so the player (and Xvfb) can shut down cleanly; SIGKILL only after the grace.
    exe = tmp_path / "Statecraft.x86_64"
    termed, ready = tmp_path / "termed", tmp_path / "ready"
    stop_once_ready(ready)
    exe.write_text(f"#!/bin/sh\ntrap 'echo yes > \"{termed}\"; exit 0' TERM\necho $$ > \"{ready}\"\nsleep 30 &\nwait\n")
    exe.chmod(0o755)
    assert pg.run_player(exe, tmp_path / "player.log", 1, "linux") is None
    # The player was ready, its trap set (stop_once_ready), so a missing `termed` is not #704's start-up race.
    assert termed.is_file(), ("the ready player never handled a SIGTERM: the stop sent none, or killed it before the "
                              "grace let its trap run")
    assert termed.read_text().strip() == "yes"


@pytest.mark.skipif(sys.platform == "win32", reason="the fake player and xvfb-run are shell scripts")
def test_a_player_that_ignores_sigterm_is_killed_after_the_grace(tmp_path, fake_xvfb_run, monkeypatch,
                                                                  stop_once_ready):
    monkeypatch.setattr(pg, "TERM_GRACE", 1)
    exe = tmp_path / "Statecraft.x86_64"
    pid_file = tmp_path / "player.pid"
    stop_once_ready(pid_file)
    sent, killpg = [], os.killpg

    def record(pid, sig):
        if sig:
            sent.append((sig, time.monotonic()))
        killpg(pid, sig)

    monkeypatch.setattr(pg.os, "killpg", record)
    exe.write_text(f"#!/bin/sh\ntrap '' TERM\necho $$ > \"{pid_file}\"\nwhile :; do sleep 1; done\n")
    exe.chmod(0o755)
    assert pg.run_player(exe, tmp_path / "player.log", 1, "linux") is None
    assert gone_soon(int(pid_file.read_text())), "a player that ignores SIGTERM outlived the stop: no SIGKILL"
    # SIGTERM, then SIGKILL no sooner than the grace after it. Load can only lengthen the gap, never shorten it.
    assert [sig for sig, _ in sent] == [signal.SIGTERM, signal.SIGKILL], "the stop must send SIGTERM, then SIGKILL"
    gap = sent[1][1] - sent[0][1]
    assert gap >= pg.TERM_GRACE, f"SIGKILL came {gap:.2f} s after SIGTERM, before the {pg.TERM_GRACE} s grace"


@pytest.mark.skipif(sys.platform == "win32", reason="process groups are POSIX")
@pytest.mark.parametrize("error", [ProcessLookupError, PermissionError])
def test_stopping_a_run_that_already_exited_is_not_an_error(monkeypatch, error):
    # Review #581 nit 2: the player can exit just as the timeout fires. Signalling its gone group raises
    # ProcessLookupError, or PermissionError on macOS; neither may escape.
    proc = subprocess.Popen(["true"], start_new_session=True)
    proc.wait()
    pg.stop_group(proc, 1)   # the real race: the group is gone

    def gone(pid, sig):
        raise error()

    monkeypatch.setattr(pg.os, "killpg", gone)
    pg.stop_group(proc, 1)


@pytest.mark.skipif(sys.platform == "win32", reason="the fake player is a shell script")
def test_run_passes_a_player_that_prints_the_goldens_and_prints_the_log_when_it_does_not(tmp_path, capsys,
                                                                                          fake_xvfb_run):
    (tmp_path / "manifest.txt").write_text(MANIFEST)

    def archive(name, script):
        exe = tmp_path / "Statecraft.x86_64"
        exe.write_text("#!/bin/sh\n" + script)
        exe.chmod(0o755)
        path = tmp_path / name
        with tarfile.open(path, "w:gz") as t:
            t.add(exe, arcname="Statecraft.x86_64")
            add_notices_to_tar(t)
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
    assert "PLAYER GOLDEN OK linux" in out and "ARCHIVE ok.tar.gz sha256 " in out
    assert "RUN xvfb-run -a " in out and "-batchmode -nographics -golden -logFile" in out
    assert fake_xvfb_run.read_text().startswith("-a ") and "Statecraft.x86_64 -batchmode -nographics" in fake_xvfb_run.read_text()

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


def test_the_pass_rule_matches_the_real_player_log_format():
    # Review T53 S-B: the #542 run's Mac player log carries the GOLDEN lines bare. A prefixed line, or a hash with one
    # more hex digit, is not the expected line.
    assert pg.check_log("Loading content\n" + "\n".join(EXPECTED) + "\n", EXPECTED, 0) == []
    assert pg.check_log("\n".join("12:00:01 " + l for l in EXPECTED), EXPECTED, 0) == [f"missing line: {l}" for l in EXPECTED]
    assert pg.check_log(EXPECTED[0] + "a\n" + EXPECTED[1], EXPECTED, 0) == [f"missing line: {EXPECTED[0]}"]


def test_the_release_script_shares_the_asset_names_the_tag_rule_and_the_pass_rule():
    # Issue #546: tools/release_build.py takes all three from this module, so the build record and CI cannot disagree.
    import release_build as rb
    assert rb.ARCHIVES == {"mac": pg.ASSETS["macos"], "linux": pg.ASSETS["linux"], "windows": pg.ASSETS["windows"]}
    assert rb.TAG is pg.TAG
    expected = {"1950": H50, "1952": H52}
    logs = ["\n".join(EXPECTED), "\n".join("12:00:01 " + l for l in EXPECTED), EXPECTED[0] + "a\n" + EXPECTED[1],
            "\n".join(EXPECTED) + "\nBOOT FAIL x", EXPECTED[0], ""]
    for log in logs:
        for code in (0, 1, None):
            assert (rb.golden_status(log, code, expected) == "pass") is (pg.check_log(log, EXPECTED, code) == []), (log, code)


def fake_linux_player(tmp_path, body: bytes, name="p.tar.gz"):
    exe = tmp_path / "Statecraft.x86_64"
    exe.write_bytes(body)
    exe.chmod(0o755)
    path = tmp_path / name
    with tarfile.open(path, "w:gz") as t:
        t.add(exe, arcname="Statecraft.x86_64")
        add_notices_to_tar(t)
    return path


@pytest.mark.skipif(sys.platform == "win32", reason="exec format errors are POSIX")
def test_a_player_that_cannot_start_fails_cleanly(tmp_path, capsys, monkeypatch):
    # The player started directly (as on Windows and macOS): under xvfb-run an unstartable Linux player is xvfb-run's
    # exit code 126 instead, which the exit-code rule already fails.
    monkeypatch.setattr(pg, "DISPLAY_WRAPPER", {})
    (tmp_path / "manifest.txt").write_text(MANIFEST)
    arc = fake_linux_player(tmp_path, b"\x00\x01 not an executable")
    code = pg.main(["run", "--platform", "linux", "--manifest", str(tmp_path / "manifest.txt"), "--archive", str(arc),
                    "--sha256", pg.sha256(arc), "--work", str(tmp_path / "w")])
    out = capsys.readouterr().out
    assert code == 1 and "::error::linux: the player could not be started" in out and "PLAYER GOLDEN FAIL linux" in out
    assert "exited" not in out and "missing line" not in out, "no made-up exit code (review T53)"


def test_arch_is_required_for_macos_and_refused_elsewhere(tmp_path, capsys):
    (tmp_path / "manifest.txt").write_text(MANIFEST)
    arc = fake_linux_player(tmp_path, b"#!/bin/sh\n")
    base = ["run", "--manifest", str(tmp_path / "manifest.txt"), "--archive", str(arc), "--sha256", pg.sha256(arc)]
    assert pg.main(base + ["--platform", "linux", "--arch", "x86_64", "--work", str(tmp_path / "w1")]) == 2
    assert pg.main(base + ["--platform", "macos", "--work", str(tmp_path / "w2")]) == 2
    assert capsys.readouterr().out.count("--arch (arm64 or x86_64) is required for macos and only for macos") == 2
    with pytest.raises(SystemExit):
        pg.main(base + ["--platform", "macos", "--arch", "", "--work", str(tmp_path / "w3")])


def fake_mac_release(tmp_path):
    (tmp_path / "manifest.txt").write_text(MANIFEST)
    macos = tmp_path / "build" / "Statecraft.app" / "Contents" / "MacOS"
    macos.mkdir(parents=True)
    shutil.copy("/usr/bin/true", macos / "Statecraft Israel")
    z = mac_release_zip(tmp_path)
    return ["run", "--platform", "macos", "--manifest", str(tmp_path / "manifest.txt"), "--archive", str(z),
            "--sha256", pg.sha256(z), "--work", str(tmp_path / "w")]


@pytest.mark.skipif(sys.platform != "darwin" or not shutil.which("ditto"), reason="needs ditto, lipo and uname")
def test_run_refuses_a_mac_slice_the_runner_cannot_run_before_running(tmp_path, capsys, monkeypatch):
    # #548: the CPU is read through `arch -<slice>`. On an arm64 runner without Rosetta (or an Intel one asked for
    # arm64) `arch` cannot start uname at all ("Bad CPU type in executable"), so the run stops before the player.
    real_run = subprocess.run
    seen = []

    def fake_run(cmd, *a, **k):
        seen.append(cmd)
        if cmd == ["arch", "-x86_64", "uname", "-m"]:
            raise subprocess.CalledProcessError(1, cmd, "", "arch: posix_spawnp: uname: Bad CPU type in executable\n")
        return real_run(cmd, *a, **k)
    monkeypatch.setattr(pg.subprocess, "run", fake_run)
    code = pg.main(fake_mac_release(tmp_path) + ["--arch", "x86_64"])
    out = capsys.readouterr().out
    assert code == 2 and "PLAYER GOLDEN ERROR macos" in out and "RUN " not in out
    assert "['arch', '-x86_64', 'uname', '-m']" in out
    assert ["arch", "-x86_64", "uname", "-m"] in seen and ["uname", "-m"] not in seen


@pytest.mark.skipif(sys.platform != "darwin" or not shutil.which("ditto"), reason="needs ditto, lipo and uname")
def test_run_refuses_a_mac_player_without_the_slice_before_running(tmp_path, capsys, monkeypatch):
    # Review T53 nit: pins the call into check_arch (the `if a.arch:` wiring), not only check_arch itself.
    real_run = subprocess.run

    def fake_run(cmd, *a, **k):
        if cmd == ["arch", "-x86_64", "uname", "-m"]:
            return subprocess.CompletedProcess(cmd, 0, "x86_64\n", "")
        if cmd[:2] == ["lipo", "-archs"]:
            return subprocess.CompletedProcess(cmd, 0, "arm64\n", "")
        return real_run(cmd, *a, **k)
    monkeypatch.setattr(pg.subprocess, "run", fake_run)
    code = pg.main(fake_mac_release(tmp_path) + ["--arch", "x86_64"])
    out = capsys.readouterr().out
    assert code == 2 and "Statecraft Israel has no x86_64 slice (lipo -archs: arm64)" in out and "RUN " not in out


@pytest.mark.skipif(sys.platform != "darwin" or not shutil.which("ditto"), reason="needs ditto, lipo and uname")
def test_run_starts_the_mac_player_through_arch_for_its_slice(tmp_path, capsys, monkeypatch):
    # Review of #643: a bare start would run the runner's native arm64 slice and could pass green for x86_64.
    real_run = subprocess.run
    started = []

    def fake_run(cmd, *a, **k):
        if cmd == ["arch", "-x86_64", "uname", "-m"]:
            return subprocess.CompletedProcess(cmd, 0, "x86_64\n", "")   # Rosetta on an arm64 runner
        if cmd[:2] == ["lipo", "-archs"]:
            return subprocess.CompletedProcess(cmd, 0, "x86_64 arm64\n", "")
        return real_run(cmd, *a, **k)
    monkeypatch.setattr(pg.subprocess, "run", fake_run)
    monkeypatch.setattr(pg, "run_player", lambda *args: started.append(args) or 0)
    pg.main(fake_mac_release(tmp_path) + ["--arch", "x86_64"])
    assert len(started) == 1 and started[0][4:] == ("x86_64",)
    exe, log, _, platform, arch = started[0]
    assert pg.player_command(exe, log, platform, arch)[:2] == ["arch", "-x86_64"]


def mac_release_zip(tmp_path, notices=pg.REQUIRED_NOTICES, extra=()):
    """A zip shaped like tools/release_build.py's (#552): ditto zips the .app, then the notices go in at the top level."""
    z = tmp_path / "statecraft-israel-v0.2.0-macos-universal.zip"
    subprocess.run(["ditto", "-c", "-k", "--norsrc", "--keepParent", str(tmp_path / "build" / "Statecraft.app"), str(z)],
                   check=True)
    with zipfile.ZipFile(z, "a") as zf:
        for n in [*notices, *extra]:
            zf.writestr(n, f"{n}\n")
    return z


def unpacked(tmp_path, platform, notices=pg.REQUIRED_NOTICES, extra=(), nested=False):
    """An unpacked archive shaped like #552's: the player (Windows, Linux) or the .app (macOS) plus the notices."""
    root = tmp_path / f"u-{platform}"
    base = root / "statecraft" if nested else root
    if platform == "windows":
        (base / "Statecraft_Data").mkdir(parents=True)
        (base / "UnityCrashHandler64.exe").write_bytes(b"")
        exe = base / "Statecraft Israel.exe"
    elif platform == "linux":
        (base / "Statecraft_Data").mkdir(parents=True)
        exe = base / "Statecraft.x86_64"
    else:
        (base / "Statecraft.app" / "Contents" / "MacOS").mkdir(parents=True)
        exe = base / "Statecraft.app" / "Contents" / "MacOS" / "Statecraft Israel"
    exe.write_bytes(b"")
    for n in [*notices, *extra]:
        (root / n).parent.mkdir(parents=True, exist_ok=True)
        (root / n).write_text(n)
    return root


@pytest.mark.parametrize("platform", ["windows", "linux", "macos"])
def test_the_player_plus_the_notices_is_the_accepted_layout(tmp_path, platform):
    root = unpacked(tmp_path, platform)
    exe = pg.find_player(root, platform)   # the notices are not players: LICENSES/ holds only .txt
    assert pg.layout_problems(root, platform, exe) == []


@pytest.mark.parametrize("platform", ["windows", "linux", "macos"])
def test_a_missing_notice_or_a_nested_player_breaks_the_layout(tmp_path, platform):
    root = unpacked(tmp_path, platform, notices=[n for n in pg.REQUIRED_NOTICES if n != "LICENSES/CC0-1.0.txt"])
    assert pg.layout_problems(root, platform, pg.find_player(root, platform)) == ["missing notice: LICENSES/CC0-1.0.txt"]
    nested = unpacked(tmp_path / "n", platform, nested=True)
    problems = pg.layout_problems(nested, platform, pg.find_player(nested, platform))
    assert "not at the archive's top level" in problems[0]


def test_nothing_but_the_notices_may_sit_beside_the_mac_app(tmp_path):
    root = unpacked(tmp_path, "macos", extra=["event-photos.json"])
    assert pg.layout_problems(root, "macos", pg.find_player(root, "macos")) == ["unexpected beside the app: event-photos.json"]


def test_the_release_script_shares_the_notice_list_and_the_one_player_rule():
    import release_build as rb
    assert rb.REQUIRED_NOTICES is pg.REQUIRED_NOTICES and rb.NOTICE_TOPS == pg.NOTICE_TOPS
    assert set(rb.FONT_LICENCES) <= rb.NOTICE_TOPS


@pytest.mark.skipif(sys.platform != "darwin" or not shutil.which("ditto"), reason="the macOS zip is unpacked with ditto")
def test_a_mac_release_zip_with_its_notices_unpacks_to_an_accepted_layout(tmp_path):
    macos = tmp_path / "build" / "Statecraft.app" / "Contents" / "MacOS"
    macos.mkdir(parents=True)
    (macos / "Statecraft Israel").write_bytes(b"")
    pg.unpack(mac_release_zip(tmp_path), tmp_path / "out")
    exe = pg.find_player(tmp_path / "out", "macos")
    assert pg.layout_problems(tmp_path / "out", "macos", exe) == []
    assert (tmp_path / "out" / "LICENSES" / "CC0-1.0.txt").is_file()


def test_a_release_archive_without_its_notices_is_refused_before_the_player_runs(tmp_path, capsys):
    (tmp_path / "manifest.txt").write_text(MANIFEST)
    exe = tmp_path / "Statecraft.x86_64"
    exe.write_text("#!/bin/sh\n")
    exe.chmod(0o755)
    arc = tmp_path / "bare.tar.gz"
    with tarfile.open(arc, "w:gz") as t:
        t.add(exe, arcname="Statecraft.x86_64")
    code = pg.main(["run", "--platform", "linux", "--manifest", str(tmp_path / "manifest.txt"), "--archive", str(arc),
                    "--sha256", pg.sha256(arc), "--work", str(tmp_path / "w")])
    out = capsys.readouterr().out
    assert code == 2 and "missing notice: LICENSE" in out and "RUN " not in out
