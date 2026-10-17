# /// script
# requires-python = ">=3.12"
# dependencies = []
# ///
"""The licence-free golden check of a released desktop player (.github/workflows/player-golden.yml; plan
docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md, sections 2.2 and 6.3; decision O7).

  uv run tools/player_golden_check.py plan --tag vX.Y.Z --event release|workflow_dispatch --output FILE [--summary FILE] < release.json
  uv run tools/player_golden_check.py run --platform windows|linux|macos [--arch arm64|x86_64 (macos)] --archive FILE --sha256 HEX --manifest manifest.txt
      --work DIR

`plan` reads `gh release view --json tagName,isDraft,assets` on stdin, checks the tag's form and that the release is
published, and appends to FILE (the job's $GITHUB_OUTPUT) `matrix=` the GitHub Actions matrix of the players and
`count=` their number. Each entry carries the SHA-256 GitHub records for the asset (its `digest`). A release with desktop
players must carry all three: one or two fails, and so does an asset without a SHA-256 digest. No player at all fails a
manual run; on a release event it passes (a web-only release) but writes an "UNCHECKED" warning to the run summary.

`run` checks the archive's SHA-256 against that digest (so a re-uploaded asset fails until it is re-planned), unpacks it
into an empty folder, finds the player, checks the layout (the notices of REQUIRED_NOTICES at the top level beside the
player or the .app), runs it with
`-batchmode -nographics -golden -logFile <absolute path>` (docs/determinism.md), the Linux player under `xvfb-run -a`
(DISPLAY_WRAPPER: it crashes with no display at all), and passes only when it exits 0 and its log
holds `GOLDEN OK 1950 <hash1950>` and `GOLDEN OK 1952 <hash1952>` from the golden manifest, and no FAIL line. On failure
it prints the whole player log. With --arch (the Mac app, run once per slice) the player is started as
`arch -<arch>` (the x86_64 slice under Rosetta on an arm64 runner), `uname -m` behind the same prefix must be that
architecture and `lipo -archs` must list it. Exit 0 when the check passes, 1 when it fails, 2 on bad input.

The release asset names are a contract with the release script (tools/release_build.py): see ASSETS.
Stdlib only.
"""
import argparse
import hashlib
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

# A release tag: vMAJOR.MINOR.PATCH, optionally with a pre-release suffix (v0.2.0-rc.1). Nothing else reaches a path,
# a download or a checkout.
# re.ASCII: \d must not accept other scripts' digits. The workflow's shell check is the same pattern (a test pins them).
TAG = re.compile(r"v\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:\.[0-9A-Za-z]+)*)?", re.ASCII)
DIGEST = re.compile(r"sha256:([0-9a-f]{64})", re.ASCII)

# The asset names the release script must attach to the GitHub Release, by platform (the web zip already follows this
# form: statecraft-israel-vX.Y.Z-web.zip, docs/RELEASING.md). {tag} is the release tag, v included.
ASSETS = {
    "windows": "statecraft-israel-{tag}-windows-x64.zip",
    "linux": "statecraft-israel-{tag}-linux-x64.tar.gz",
    "macos": "statecraft-israel-{tag}-macos-universal.zip",
}
# The GitHub-hosted runners each player is checked on (standard runners, free and unlimited for a public repository,
# plan 6.1): (runner label, the CPU architecture it must report). The Universal Mac app runs twice, so each of its two
# native IL2CPP slices is checked on its own: both on macos-15 (Apple silicon), the x86_64 one under Rosetta (MAC_SLICE).
# GitHub retires macos-15-intel, its last x86_64 image, in August 2027 (runner-images#13045; issue #548). An empty
# architecture is not checked.
RUNNERS = {
    "windows": [("windows-2025", "")],
    "linux": [("ubuntu-24.04", "")],
    "macos": [("macos-15", "arm64"), ("macos-15", "x86_64")],
}
# How the Mac player and the CPU probe are started for a slice. A Universal app started bare runs the machine's native
# slice; `arch -x86_64` runs the x86_64 one under Rosetta (installed on GitHub's arm64 macOS images), and `arch -arm64`
# fails on an Intel Mac. `uname -m` behind the same prefix reports the CPU the player will see (issue #548).
MAC_SLICE = ["arch", "-{arch}"]

# The notices every archive carries at its top level, beside the player (tools/release_build.py adds them and takes
# this list; the web packager, scripts/oss/package_web.sh, copies the same files: test_release_build pins that).
REQUIRED_NOTICES = ["LICENSE", "LICENSES/CC-BY-4.0.txt", "LICENSES/CC-BY-SA-4.0.txt", "LICENSES/CC0-1.0.txt",
                    "LICENSES/YamlDotNet-MIT.txt", "NOTICE", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt",
                    "THIRD-PARTY-NOTICES.md"]
NOTICE_TOPS = {n.split("/")[0] for n in REQUIRED_NOTICES}

# What the player is started behind, by platform. Linux: a virtual X display. Unity 6's Linux player picks its window
# backend (x11 or wayland) at start-up, and with neither DISPLAY nor WAYLAND_DISPLAY set it picks none ("Selected window
# backend: (null)") and segfaults in PlayerMain after loading the first scene, with -batchmode -nographics or without them
# (issue #533: the v0.1.0-rc.0 run on ubuntu-24.04, reproduced in an ubuntu:24.04 x86_64 container). Under xvfb-run it
# selects x11 and passes; -nographics still keeps the Null graphics device, so the runner needs no GPU.
# A test pins this command.
DISPLAY_WRAPPER = {"linux": ["xvfb-run", "-a"]}

# Seconds a timed-out run gets between SIGTERM and SIGKILL (stop_group).
TERM_GRACE = 5

GOLDENS = (("1950", "hash1950"), ("1952", "hash1952"))
FAIL = re.compile(r"\b(GOLDEN FAIL|BOOT FAIL)\b")


def asset_name(tag: str, platform: str) -> str:
    if not TAG.fullmatch(tag):
        raise ValueError(f"not a release tag: {tag!r} (expected vX.Y.Z)")
    return ASSETS[platform].format(tag=tag)


def plan(tag: str, release: dict, event: str) -> tuple[list[dict], list[str], bool]:
    """(matrix include list, messages as workflow commands, ok) for a release's JSON from gh
    (`gh release view --json tagName,isDraft,assets`; each asset has `name` and `digest`)."""
    if not TAG.fullmatch(tag):
        return [], [f"::error::not a release tag: {tag!r} (expected vX.Y.Z)"], False
    if release.get("tagName") != tag:
        return [], [f"::error::the release is for {release.get('tagName')!r}, not {tag!r}"], False
    if release.get("isDraft"):
        return [], [f"::error::release {tag} is a draft; only published releases are checked"], False
    digests = {a.get("name"): a.get("digest") for a in release.get("assets", [])}
    include, missing, msgs = [], [], []
    found = 0
    for platform, runners in RUNNERS.items():
        name = asset_name(tag, platform)
        if name not in digests:
            missing.append(f"{platform} ({name})")
            continue
        found += 1
        m = DIGEST.fullmatch(digests[name] or "")
        if not m:
            msgs.append(f"::error::asset {name} has no SHA-256 digest from GitHub ({digests[name]!r})")
        for runner, arch in runners:
            include.append({"platform": platform, "label": f"{platform} {arch}".strip(), "runner": runner, "arch": arch,
                            "asset": name, "sha256": m.group(1) if m else ""})
    if not include:
        if event == "workflow_dispatch":
            return [], [f"::error::release {tag} has none of the player assets"], False
        # A web-only release is valid; failing it would make red runs routine. The summary says it loudly instead.
        return [], [f"::warning::UNCHECKED: release {tag} has no desktop player. If players are uploaded later, run "
                    f"gh workflow run player-golden.yml -f tag={tag} (docs/RELEASING.md, step 6)"], True
    if missing:
        msgs.append(f"::error::release {tag} has {found} of the 3 desktop players; missing: {', '.join(missing)}. "
                    "Attach all three in the same gh release create (docs/RELEASING.md, step 6)")
    if msgs:
        return [], msgs, False
    return include, msgs, True


def read_manifest(text: str) -> dict[str, str]:
    out = {}
    for line in text.splitlines():
        parts = line.split()
        if len(parts) == 2 and not line.startswith("#"):
            out[parts[0]] = parts[1]
    return out


def expected_lines(manifest: dict[str, str]) -> list[str]:
    missing = [key for _, key in GOLDENS if not re.fullmatch(r"[0-9a-f]{64}", manifest.get(key, ""))]
    if missing:
        raise ValueError(f"the golden manifest has no SHA-256 for {', '.join(missing)}")
    return [f"GOLDEN OK {year} {manifest[key]}" for year, key in GOLDENS]


def check_log(log: str, expected: list[str], exit_code: int | None) -> list[str]:
    """The problems with a player run: empty when it passed. The player prints each GOLDEN line bare on its own line
    (GameBoot.cs Debug.Log; the Mac player log of the #542 release-script run shows exactly that), so a line must equal
    the expected text after stripping: no prefix, and no extra character (one more hex digit is a different hash)."""
    lines = [l.strip() for l in log.splitlines()]
    problems = []
    if exit_code is None:
        problems.append("the player did not exit before the timeout")
    elif exit_code != 0:
        problems.append(f"the player exited {exit_code}")
    for want in expected:
        if want not in lines:
            problems.append(f"missing line: {want}")
    problems += [f"failure line: {l}" for l in lines if FAIL.search(l)]
    return problems


def unpack(archive: Path, dest: Path) -> None:
    """Unpacks into dest, which must be new or empty. tar keeps the Linux player's executable bit; ditto keeps the Mac
    app's bundle layout and modes; the Windows zip needs neither."""
    dest.mkdir(parents=True, exist_ok=True)
    if any(dest.iterdir()):
        raise ValueError(f"{dest} is not empty")
    name = archive.name
    if name.endswith(".tar.gz"):
        with tarfile.open(archive) as t:
            t.extractall(dest, filter="data")   # refuses absolute paths, .. and links that leave dest
    elif name.endswith(".zip") and sys.platform == "darwin":
        subprocess.run(["ditto", "-x", "-k", str(archive), str(dest)], check=True)
    elif name.endswith(".zip"):
        with zipfile.ZipFile(archive) as z:
            z.extractall(dest)   # zipfile drops absolute paths and .. components
    else:
        raise ValueError(f"not a .zip or .tar.gz: {name}")


def find_player(root: Path, platform: str) -> Path:
    """The one player executable under root."""
    if platform == "windows":
        found = [p for p in root.rglob("*.exe") if not p.name.lower().startswith("unitycrashhandler")]
    elif platform == "linux":
        found = [p for p in root.rglob("*.x86_64") if p.is_file()]
    elif platform == "macos":
        found = [p for app in root.rglob("*.app") if app.is_dir()
                 for p in (app / "Contents" / "MacOS").glob("*") if p.is_file()]
    else:
        raise ValueError(f"unknown platform {platform!r}")
    if len(found) != 1:
        raise ValueError(f"expected one {platform} player under {root}, found {len(found)}: "
                         + ", ".join(str(p.relative_to(root)) for p in sorted(found)))
    return found[0]


def layout_problems(root: Path, platform: str, exe: Path) -> list[str]:
    """How an unpacked archive breaks the release layout (tools/release_build.py, make_archive): the notices at the top
    level; Windows and Linux: the player there too; macOS: the .app there, and nothing else beside it but the notices."""
    problems = [f"missing notice: {n}" for n in REQUIRED_NOTICES if not (root / n).is_file()]
    if platform == "macos":
        app = exe.parents[2]   # <app>/Contents/MacOS/<exe>
        if app.parent != root:
            problems.append(f"the app is not at the archive's top level: {app.relative_to(root)}")
        extra = sorted(p.name for p in root.iterdir() if p != app and p.name not in NOTICE_TOPS)
        if extra:
            problems.append(f"unexpected beside the app: {', '.join(extra)}")
    elif exe.parent != root:
        problems.append(f"the player is not at the archive's top level: {exe.relative_to(root)}")
    return problems


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def check_arch(exe: Path, arch: str, machine: str, archs: str) -> list[str]:
    """Problems with running exe's `arch` slice here: this machine must be that CPU (`uname -m`), and the executable
    must carry the slice (`lipo -archs`), so a wrong runner label cannot quietly check the other slice."""
    problems = []
    if machine != arch:
        problems.append(f"the runner is {machine}, not {arch}: check the runner label")
    if arch not in archs.split():
        problems.append(f"{exe.name} has no {arch} slice (lipo -archs: {archs.strip() or 'nothing'})")
    return problems


def slice_prefix(arch: str) -> list[str]:
    """The MAC_SLICE prefix for a Mac slice; nothing when no architecture is checked."""
    return [p.format(arch=arch) for p in MAC_SLICE] if arch else []


def player_command(exe: Path, log: Path, platform: str, arch: str = "") -> list[str]:
    """The golden run's command line: the player with -batchmode -nographics -golden and an absolute -logFile, behind
    the platform's DISPLAY_WRAPPER if it has one, and behind MAC_SLICE for a Mac slice."""
    return [*DISPLAY_WRAPPER.get(platform, []), *slice_prefix(arch), str(exe), "-batchmode", "-nographics", "-golden",
            "-logFile", str(log.resolve())]


def run_player(exe: Path, log: Path, timeout: int, platform: str, arch: str = "") -> int | None:
    """Runs the golden check; the exit code, or None on timeout. subprocess waits for a Windows GUI-subsystem player
    too, which a plain PowerShell call would not. On POSIX the run gets its own process group and a timeout stops the
    whole group: xvfb-run starts the player and Xvfb as its children, and killing only xvfb-run would leave them."""
    if os.name != "nt":
        exe.chmod(exe.stat().st_mode | 0o111)
    cmd = player_command(exe, log, platform, arch)
    print("RUN", " ".join(cmd), flush=True)
    with subprocess.Popen(cmd, cwd=exe.parent, start_new_session=os.name != "nt") as proc:
        try:
            return proc.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            stop_group(proc, TERM_GRACE)
            return None


def stop_group(proc: subprocess.Popen, grace: float) -> None:
    """Stops a timed-out run. POSIX: SIGTERM to its process group, so the player and Xvfb can shut down, then SIGKILL to
    whatever is left after `grace` seconds. The run may exit just as the timeout fires: signalling a group that is gone
    raises ProcessLookupError, which ends the stop. macOS can also answer PermissionError, for a gone group or for a
    group that still holds a zombie, so that answer never escapes but also never cuts the grace short or skips the
    SIGKILL."""
    if os.name == "nt":
        proc.kill()
        proc.wait()
        return
    try:
        os.killpg(proc.pid, signal.SIGTERM)
    except ProcessLookupError:
        proc.wait()
        return
    except PermissionError:
        pass   # macOS: a zombie in the group answers EPERM, and the rest may still be running
    deadline = time.monotonic() + grace
    while time.monotonic() < deadline:
        proc.poll()   # reaps the group leader, so a zombie leader does not keep the group "alive"
        try:
            os.killpg(proc.pid, 0)
        except ProcessLookupError:
            break
        except PermissionError:
            pass   # the same macOS answer: not proof that the group is gone, so the grace runs on
        time.sleep(0.1)
    else:
        try:
            os.killpg(proc.pid, signal.SIGKILL)
        except (ProcessLookupError, PermissionError):
            pass
    proc.wait()


def summary(tag: str, include: list[dict], ok: bool) -> str:
    if not ok:
        return f"## Player golden {tag}: REFUSED\n\nNo player was run; see the plan job's errors.\n"
    if not include:
        return (f"## Player golden {tag}: UNCHECKED\n\nThe release has no desktop player, so none was run. If players "
                f"are uploaded later, run `gh workflow run player-golden.yml -f tag={tag}` and wait for it to pass.\n")
    rows = "".join(f"| {e['label']} | `{e['runner']}` | `{e['asset']}` | `{e['sha256']}` |\n" for e in include)
    return (f"## Player golden {tag}\n\n| Player | Runner | Asset | SHA-256 (GitHub digest) |\n| --- | --- | --- | --- |\n"
            f"{rows}")


def cmd_plan(a) -> int:
    try:
        release = json.load(sys.stdin)
    except json.JSONDecodeError as e:
        print(f"::error::the release JSON is unreadable: {e}")
        return 2
    include, msgs, ok = plan(a.tag, release, a.event)
    for m in msgs:
        print(m)
    if a.summary:
        with open(a.summary, "a", encoding="utf-8") as f:
            f.write(summary(a.tag, include, ok))
    matrix = json.dumps({"include": include}, separators=(",", ":"))
    print(f"MATRIX {matrix}")
    if ok:
        with open(a.output, "a", encoding="utf-8") as f:
            f.write(f"matrix={matrix}\ncount={len(include)}\n")
    return 0 if ok else 1


def cmd_run(a) -> int:
    try:
        expected = expected_lines(read_manifest(Path(a.manifest).read_text(encoding="utf-8")))
        archive = Path(a.archive)
        got = sha256(archive)
        print(f"ARCHIVE {archive.name} sha256 {got}", flush=True)
        if got != a.sha256:
            raise ValueError(f"{archive.name} has SHA-256 {got}, but GitHub's digest for the asset is {a.sha256!r}; "
                             "it was replaced after the plan job: run the workflow again")
        if (a.platform == "macos") != bool(a.arch):
            raise ValueError("--arch (arm64 or x86_64) is required for macos and only for macos")
        work = Path(a.work).resolve()   # the player runs with cwd at its folder, so every path must be absolute
        unpack(archive, work / "player")
        exe = find_player(work / "player", a.platform)
        if problems := layout_problems(work / "player", a.platform, exe):
            raise ValueError("; ".join(problems))
        if a.arch:
            machine = subprocess.run([*slice_prefix(a.arch), "uname", "-m"], capture_output=True, text=True,
                                     check=True).stdout.strip()
            archs = subprocess.run(["lipo", "-archs", str(exe)], capture_output=True, text=True, check=True).stdout
            print(f"ARCH runner {machine}, player slices {archs.strip()}", flush=True)
            if problems := check_arch(exe, a.arch, machine, archs):
                raise ValueError("; ".join(problems))
        if (wrapper := DISPLAY_WRAPPER.get(a.platform)) and not shutil.which(wrapper[0]):
            raise ValueError(f"{wrapper[0]} is not installed: the {a.platform} player needs a virtual display "
                             "(apt-get install xvfb; the workflow installs it)")
    except (OSError, ValueError, subprocess.CalledProcessError, tarfile.TarError, zipfile.BadZipFile) as e:
        print(f"::error::{a.platform}: {e}")
        print(f"PLAYER GOLDEN ERROR {a.platform}")
        return 2
    log = work / "player.log"
    try:
        code = run_player(exe, log, a.timeout, a.platform, a.arch or "")
    except OSError as e:   # the player could not be started at all (for example: Exec format error); it has no exit code
        code, start_error = None, f"the player could not be started: {e}"
    else:
        start_error = ""
    text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
    problems = [start_error] if start_error else check_log(text, expected, code)
    if not problems:
        for line in expected:
            print(line)
        print(f"PLAYER GOLDEN OK {a.platform}")
        return 0
    print(f"----- player log ({log}) -----")
    print(text if text else "(no log was written)")
    print("----- end of player log -----")
    for p in problems:
        print(f"::error::{a.platform}: {p}")
    print(f"PLAYER GOLDEN FAIL {a.platform}")
    return 1


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("plan")
    p.add_argument("--tag", required=True)
    p.add_argument("--event", required=True, choices=["release", "workflow_dispatch"])
    p.add_argument("--output", required=True, help="the file to append matrix= and count= to ($GITHUB_OUTPUT)")
    p.add_argument("--summary", help="the file to append a Markdown summary to ($GITHUB_STEP_SUMMARY)")
    r = sub.add_parser("run")
    r.add_argument("--platform", required=True, choices=sorted(ASSETS))
    r.add_argument("--archive", required=True)
    r.add_argument("--arch", choices=["arm64", "x86_64"],
                   help="macos only, and required there: the slice to check; the runner must be that CPU and the app "
                        "must carry it")
    r.add_argument("--sha256", required=True, help="the asset's SHA-256 from GitHub (hex), from plan")
    r.add_argument("--manifest", required=True)
    r.add_argument("--work", required=True)
    r.add_argument("--timeout", type=int, default=900)
    a = ap.parse_args(argv)
    return cmd_plan(a) if a.cmd == "plan" else cmd_run(a)


if __name__ == "__main__":
    sys.exit(main())
