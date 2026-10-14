# /// script
# requires-python = ">=3.12"
# dependencies = []
# ///
"""The licence-free golden check of a released desktop player (.github/workflows/player-golden.yml; plan
docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md, sections 2.2 and 6.3; decision O7).

  uv run tools/player_golden_check.py plan --tag vX.Y.Z --event release|workflow_dispatch --output FILE < release.json
  uv run tools/player_golden_check.py run --platform windows|linux|macos --archive FILE --manifest manifest.txt --work DIR

`plan` reads `gh release view --json tagName,isDraft,assets` on stdin, checks the tag's form and that the release is
published, and appends to FILE (the job's $GITHUB_OUTPUT) `matrix=` the GitHub Actions matrix of the platforms whose
player is attached and `count=` their number. A missing
player is a warning; no player at all fails a manual run, and is only a notice on a release (a web-only release).

`run` prints the archive's SHA-256, unpacks it into an empty folder, finds the player, runs it with
`-batchmode -nographics -golden -logFile <absolute path>` (docs/determinism.md) and passes only when it exits 0 and its log
holds `GOLDEN OK 1950 <hash1950>` and `GOLDEN OK 1952 <hash1952>` from the golden manifest, and no FAIL line. On failure
it prints the whole player log. Exit 0 when the check passes, 1 when it fails, 2 on bad input.

The release asset names are a contract with the release script (tools/release_build.py): see ASSETS.
Stdlib only.
"""
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import tarfile
import zipfile
from pathlib import Path

# A release tag: vMAJOR.MINOR.PATCH, optionally with a pre-release suffix (v0.2.0-rc.1). Nothing else reaches a path,
# a download or a checkout.
TAG = re.compile(r"v\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:\.[0-9A-Za-z]+)*)?")

# The asset names the release script must attach to the GitHub Release, by platform (the web zip already follows this
# form: statecraft-israel-vX.Y.Z-web.zip, docs/RELEASING.md). {tag} is the release tag, v included.
ASSETS = {
    "windows": "statecraft-israel-{tag}-windows-x64.zip",
    "linux": "statecraft-israel-{tag}-linux-x64.tar.gz",
    "macos": "statecraft-israel-{tag}-macos-universal.zip",
}
# The GitHub-hosted runner each player is checked on (free and unlimited for a public repository, plan 6.1).
RUNNERS = {"windows": "windows-2025", "linux": "ubuntu-24.04", "macos": "macos-15"}

GOLDENS = (("1950", "hash1950"), ("1952", "hash1952"))
FAIL = re.compile(r"\b(GOLDEN FAIL|BOOT FAIL)\b")


def asset_name(tag: str, platform: str) -> str:
    if not TAG.fullmatch(tag):
        raise ValueError(f"not a release tag: {tag!r} (expected vX.Y.Z)")
    return ASSETS[platform].format(tag=tag)


def plan(tag: str, release: dict, event: str) -> tuple[list[dict], list[str], bool]:
    """(matrix include list, messages as workflow commands, ok) for a release's JSON from gh."""
    if not TAG.fullmatch(tag):
        return [], [f"::error::not a release tag: {tag!r} (expected vX.Y.Z)"], False
    if release.get("tagName") != tag:
        return [], [f"::error::the release is for {release.get('tagName')!r}, not {tag!r}"], False
    if release.get("isDraft"):
        return [], [f"::error::release {tag} is a draft; only published releases are checked"], False
    names = {a.get("name") for a in release.get("assets", [])}
    include, msgs = [], []
    for platform, runner in RUNNERS.items():
        name = asset_name(tag, platform)
        if name in names:
            include.append({"platform": platform, "runner": runner, "asset": name})
        else:
            msgs.append(f"::warning::release {tag} has no {platform} player ({name}); it is not checked")
    if not include:
        if event == "workflow_dispatch":
            return [], msgs + [f"::error::release {tag} has none of the player assets"], False
        msgs.append(f"::notice::release {tag} has no desktop player; nothing to check")
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
    """The problems with a player run: empty when it passed."""
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


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def run_player(exe: Path, log: Path, timeout: int) -> int | None:
    """Runs the golden check; the exit code, or None on timeout. subprocess waits for a Windows GUI-subsystem player
    too, which a plain PowerShell call would not."""
    if os.name != "nt":
        exe.chmod(exe.stat().st_mode | 0o111)
    cmd = [str(exe), "-batchmode", "-nographics", "-golden", "-logFile", str(log.resolve())]
    print("RUN", " ".join(cmd), flush=True)
    try:
        return subprocess.run(cmd, cwd=exe.parent, timeout=timeout).returncode
    except subprocess.TimeoutExpired:
        return None


def cmd_plan(a) -> int:
    try:
        release = json.load(sys.stdin)
    except json.JSONDecodeError as e:
        print(f"::error::the release JSON is unreadable: {e}")
        return 2
    include, msgs, ok = plan(a.tag, release, a.event)
    for m in msgs:
        print(m)
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
        print(f"ARCHIVE {archive.name} sha256 {sha256(archive)}", flush=True)
        work = Path(a.work)
        unpack(archive, work / "player")
        exe = find_player(work / "player", a.platform)
    except (OSError, ValueError, subprocess.CalledProcessError, tarfile.TarError, zipfile.BadZipFile) as e:
        print(f"PLAYER GOLDEN ERROR {e}")
        return 2
    log = work / "player.log"
    code = run_player(exe, log, a.timeout)
    text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
    problems = check_log(text, expected, code)
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
    r = sub.add_parser("run")
    r.add_argument("--platform", required=True, choices=sorted(ASSETS))
    r.add_argument("--archive", required=True)
    r.add_argument("--manifest", required=True)
    r.add_argument("--work", required=True)
    r.add_argument("--timeout", type=int, default=900)
    a = ap.parse_args(argv)
    return cmd_plan(a) if a.cmd == "plan" else cmd_run(a)


if __name__ == "__main__":
    sys.exit(main())
