# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Build the desktop players on the owner's Mac and write a build record (refs #54; Phases 0-1 of the
plan in PR #482, spec
docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md, sections 6.4 and 6.5).

Usage: uv run tools/release_build.py [--platform mac|linux|windows|all] [--out DIR] [--tag vX.Y.Z] [--dev] [--dry-run]
                                    [--unity PATH]

- Refuses a dirty working tree (tracked changes or untracked files), before anything is built.
- Checks the Unity modules each selected platform needs and fails (exit 2) naming the ones to add in Unity Hub.
- Builds the sim DLLs (tools/build_sim_for_unity.py), then each player: macOS IL2CPP (Build.Mac), Linux and Windows on
  Mono (Build.LinuxMono, Build.WindowsMono; decisions O7 and O8). Every Unity run goes through
  `uv run tools/machine_lock.py --shared --wait-unity`, never with -nographics, and is followed by
  `git checkout -- unity/ProjectSettings unity/Assets/Settings` to drop the settings churn. Builds are -shareable (the
  sensitivity gate) unless --dev is given; the record says which.
- Runs the player golden check (docs/determinism.md) on the macOS player, the only one the Mac can run:
  `-batchmode -nographics -golden -logFile <absolute>`, under the shared lock. It passes only on exit 0 with both
  `GOLDEN OK` lines carrying the hashes pinned in sim/Statecraft.Sim.Tests/golden/manifest.txt. The Linux and Windows
  players are recorded as unchecked, for the CI player check (spec 6.3).
- Archives each player under the asset name the CI player check expects (PR #533's contract; ARCHIVES below):
  statecraft-israel-<tag>-macos-universal.zip (ditto, the .app alone), statecraft-israel-<tag>-linux-x64.tar.gz and
  statecraft-israel-<tag>-windows-x64.zip, <tag> being --tag or v<bundleVersion>. It checks first that the folder holds
  exactly one player executable.
- Writes build-record.json, build-record.md and files-<platform>.sha256 (one `shasum -a 256` line per file of the
  player folder; `shasum -a 256 -c` verifies it from inside players/<platform>/), with the SHA-256 and size of each
  archive and of each file list.

The output folder defaults to ~/statecraft-release/v<version>-<commit>-<UTC stamp>/: outside every worktree, so removing
a worktree or cleaning unity/Builds cannot take a release with it, and outside git, so nothing in it can be committed
by accident. An --out inside the repository is refused unless git ignores it. Build records live in the owner's
separate records repository (decision O12): this script never commits, pushes or uploads anything; it prints where the
record is, and the owner copies it.

Exit 0 when every build succeeded and every check that ran passed; 1 on a refusal, a failed build or a failed golden
(the record is still written for a failed golden); 2 when the editor or a module is missing.
"""
import argparse
import hashlib
import json
import os
import plistlib
import re
import subprocess
import sys
import tarfile
import time
import zipfile
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
LOCK_TIMEOUT = 30 * 24 * 3600   # machine_lock's own default gives up after an hour; a release waits for the machine
HUB = "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
METHOD = "Statecraft.Game.EditorTools.Build."
# The release asset names: a contract with the CI player golden check (tools/player_golden_check.py, PR #533), in the
# form of the web zip (statecraft-israel-vX.Y.Z-web.zip, docs/RELEASING.md). {tag} carries the v.
ARCHIVES = {
    "mac": "statecraft-israel-{tag}-macos-universal.zip",
    "linux": "statecraft-israel-{tag}-linux-x64.tar.gz",
    "windows": "statecraft-israel-{tag}-windows-x64.zip",
}
TAG = re.compile(r"v\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:\.[0-9A-Za-z]+)*)?")


@dataclass(frozen=True)
class Platform:
    name: str
    method: str          # Build.<method>
    build_target: str    # Unity's -buildTarget
    backend: str
    module_id: str       # Unity Hub's module id
    module_name: str
    module_dir: str      # where the module lives, relative to the editor folder (modules.json's destination)
    output: str          # the player's path inside players/<name>/
    runs_here: bool      # whether the Mac can run this player's golden check


PLATFORMS = {
    "mac": Platform("mac", "Mac", "OSXUniversal", "IL2CPP", "mac-il2cpp", "Mac Build Support (IL2CPP)",
                    "Unity.app/Contents/PlaybackEngines/MacStandaloneSupport", "Statecraft.app", True),
    "linux": Platform("linux", "LinuxMono", "Linux64", "Mono", "linux-mono", "Linux Build Support (Mono)",
                      "PlaybackEngines/LinuxStandaloneSupport", "Statecraft.x86_64", False),
    "windows": Platform("windows", "WindowsMono", "Win64", "Mono", "windows-mono", "Windows Build Support (Mono)",
                        "PlaybackEngines/WindowsStandaloneSupport", "Statecraft.exe", False),
}


@dataclass
class Result:
    returncode: int
    stdout: str = ""


def run(cmd: list, **kw) -> Result:
    """The one place a process is started (tests replace it). Output is captured only when asked for."""
    capture = kw.pop("capture", False)
    p = subprocess.run([str(c) for c in cmd], text=True, capture_output=capture, **kw)
    return Result(p.returncode, p.stdout if capture else "")


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def parse(argv: list[str]) -> argparse.Namespace:
    ap = argparse.ArgumentParser(description="Build the desktop players and write a build record (never pushes).")
    ap.add_argument("--platform", choices=["mac", "linux", "windows", "all"], default="all")
    ap.add_argument("--out", type=Path, default=None, help="output folder (default ~/statecraft-release/<run>)")
    ap.add_argument("--dev", action="store_true", help="a development build: no -shareable, so not for sharing")
    ap.add_argument("--dry-run", action="store_true", help="check, then print the commands without running them")
    ap.add_argument("--tag", default=None, help="the release tag in the archive names, v included (default "
                                                "v<bundleVersion>)")
    ap.add_argument("--unity", type=Path, default=None, help="the Unity executable (default: the Hub path of the "
                                                             "version in ProjectVersion.txt)")
    return ap.parse_args(argv)


def selected(a: argparse.Namespace) -> list[str]:
    return list(PLATFORMS) if a.platform == "all" else [a.platform]


# --- what the repository says ------------------------------------------------------------------------------------------

def unity_version() -> tuple[str, str]:
    text = (REPO / "unity/ProjectSettings/ProjectVersion.txt").read_text()
    m = re.search(r"m_EditorVersionWithRevision:\s*(\S+)\s*\((\w+)\)", text)
    if not m:
        raise SystemExit("cannot read the Unity version from unity/ProjectSettings/ProjectVersion.txt")
    return m.group(1), m.group(2)


def versions() -> dict:
    bundle = re.search(r"^\s*bundleVersion:\s*(\S+)", (REPO / "unity/ProjectSettings/ProjectSettings.asset").read_text(), re.M)
    sim = re.search(r'Version\s*=\s*"([^"]+)"', (REPO / "sim/Statecraft.Sim/SimInfo.cs").read_text())
    return {"bundle_version": bundle.group(1) if bundle else None, "sim_version": sim.group(1) if sim else None}


def expected_goldens() -> dict[str, str]:
    pins = {}
    for line in (REPO / "sim/Statecraft.Sim.Tests/golden/manifest.txt").read_text().splitlines():
        name, _, value = line.partition(" ")
        if name in ("hash1950", "hash1952"):
            pins[name[4:]] = value.strip()
    return pins


def default_out(commit: str, version: str, stamp: str) -> Path:
    return Path.home() / "statecraft-release" / f"v{version}-{commit[:8]}-{stamp}"


# --- checks --------------------------------------------------------------------------------------------------------------

def git(*args: str) -> Result:
    return run(["git", *args], cwd=REPO, capture=True)


def dirty_lines() -> list[str]:
    return [l for l in git("status", "--porcelain", "--untracked-files=normal").stdout.splitlines() if l.strip()]


def missing_modules(names: list[str], editor_root: Path) -> list[Platform]:
    return [PLATFORMS[n] for n in names if not (editor_root / PLATFORMS[n].module_dir).is_dir()]


def modules_message(missing: list[Platform], version: str) -> str:
    rows = "\n".join(f"  - {p.module_name} (Unity Hub module id {p.module_id}), for --platform {p.name}" for p in missing)
    ids = " ".join(f"-m {p.module_id}" for p in missing)
    return (f"MISSING UNITY MODULES for Unity {version}:\n{rows}\n"
            f"Add them in Unity Hub (Installs > {version} > Manage > Add modules), or from the command line:\n"
            f'  "{HUB}" -- --headless install-modules -v {version} {ids}\n'
            "Nothing was built.")


def inside_repo_and_tracked(out: Path) -> bool:
    try:
        out.resolve().relative_to(REPO.resolve())
    except ValueError:
        return False
    return git("check-ignore", "-q", str(out.resolve())).returncode != 0


# --- commands ------------------------------------------------------------------------------------------------------------

def locked(cmd: list, wait_unity: bool) -> list[str]:
    head = ["uv", "run", str(REPO / "tools/machine_lock.py"), "--shared"]
    if wait_unity:
        head.append("--wait-unity")
    return [*head, "--timeout", str(LOCK_TIMEOUT), "--", *[str(c) for c in cmd]]


def build_command(unity: Path, p: Platform, target: Path, log: Path, shareable: bool) -> list[str]:
    cmd = [str(unity), "-batchmode", "-CI", "false", "-GITHUB_ACTIONS", "false", "-TF_BUILD", "false", "-quit",
           "-projectPath", str(REPO / "unity"), "-buildTarget", p.build_target, "-executeMethod", METHOD + p.method,
           "-out", str(target), "-logFile", str(log)]
    if shareable:
        cmd.append("-shareable")
    return locked(cmd, wait_unity=True)


def mac_executable(app: Path) -> Path:
    with open(app / "Contents/Info.plist", "rb") as f:
        return app / "Contents/MacOS" / plistlib.load(f)["CFBundleExecutable"]


def golden_command(player: Path, log: Path) -> list[str]:
    # A player, not the editor: docs/determinism.md runs it -batchmode -nographics, with an absolute -logFile.
    return locked([str(player), "-batchmode", "-nographics", "-golden", "-logFile", str(log.resolve())], wait_unity=False)


# --- results -------------------------------------------------------------------------------------------------------------

def golden_lines(log_text: str) -> list[str]:
    return [m.group(0) for m in re.finditer(r"GOLDEN (?:OK|FAIL) \d{4} [0-9a-f]{64}(?: expected [0-9a-f]{64})?", log_text)]


def golden_status(log_text: str, exit_code: int, expected: dict[str, str]) -> str:
    """pass only on exit 0, both `GOLDEN OK <year> <pinned hash>` lines, and no GOLDEN FAIL or BOOT FAIL line (the same
    rule as the CI player check of PR #533)."""
    lines = set(golden_lines(log_text))
    ok = exit_code == 0 and all(f"GOLDEN OK {year} {h}" in lines for year, h in expected.items())
    failed = re.search(r"\b(GOLDEN FAIL|BOOT FAIL)\b", log_text) is not None
    return "pass" if ok and len(expected) == 2 and not failed else "fail"


def archive_name(tag: str, platform: str) -> str:
    return ARCHIVES[platform].format(tag=tag)


def make_archive(p: Platform, folder: Path, target: Path, dest: Path) -> None:
    """The release archive of one player folder. macOS: ditto keeps the bundle (modes, links), and the zip holds the
    .app alone. Linux: tar keeps the executable bit. Windows: a zip of the folder."""
    if p.name == "mac":
        if run(["ditto", "-c", "-k", "--keepParent", str(target), str(dest)]).returncode != 0:
            raise SystemExit(f"ditto failed for {target}")
    elif p.name == "linux":
        with tarfile.open(dest, "w:gz") as t:
            for path in sorted(folder.rglob("*")):
                t.add(path, arcname=path.relative_to(folder).as_posix(), recursive=False)
    else:
        with zipfile.ZipFile(dest, "w", zipfile.ZIP_DEFLATED) as z:
            for path in sorted(folder.rglob("*")):
                z.write(path, path.relative_to(folder).as_posix())


def player_problem(p: Platform, folder: Path) -> str | None:
    """Why the player folder breaks the archive contract of PR #533, or None: one player .exe (crash handler aside), one
    *.x86_64, or one .app with one file in Contents/MacOS."""
    if p.name == "windows":
        found = [f for f in folder.rglob("*.exe") if not f.name.lower().startswith("unitycrashhandler")]
    elif p.name == "linux":
        found = [f for f in folder.rglob("*.x86_64") if f.is_file()]
    else:
        found = [f for app in folder.rglob("*.app") if app.is_dir() for f in (app / "Contents/MacOS").glob("*") if f.is_file()]
    return None if len(found) == 1 else f"expected one {p.name} player in {folder}, found {len(found)}"


def file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def unity_seconds(log_text: str) -> int | None:
    m = re.search(r"BUILD Succeeded \S+ -> .*?, (\d+)s", log_text)
    return int(m.group(1)) if m else None


def tree_manifest(folder: Path) -> tuple[str, int, int]:
    """`shasum -a 256` lines for every file under folder, sorted by path; a symlink is listed by its target, not
    followed. Returns the text, the number of entries and the bytes of the regular files."""
    rows, size = [], 0
    for path in folder.rglob("*"):
        rel = path.relative_to(folder).as_posix()
        if path.is_symlink():
            rows.append((rel, f"symlink  {rel} -> {os.readlink(path)}"))
        elif path.is_file():
            size += path.stat().st_size
            rows.append((rel, f"{file_sha256(path)}  {rel}"))
    rows.sort(key=lambda r: r[0])
    return "".join(line + "\n" for _, line in rows), len(rows), size


def markdown(r: dict) -> str:
    out = [f"# Build record: Statecraft: Israel {r['version']['bundle_version']} ({r['commit'][:8]})", "",
           f"- Commit: `{r['commit']}` ({r['describe']}, branch {r['branch']}); tree clean: {r['tree_clean']}",
           f"- Date (UTC): {r['date_utc']}",
           f"- Unity: {r['unity']['version']} ({r['unity']['revision']})",
           f"- Version: bundleVersion {r['version']['bundle_version']}, SimInfo.Version {r['version']['sim_version']}",
           f"- Shareable build: {r['shareable']}" + ("" if r["shareable"] else " (a --dev build: not for sharing)"),
           f"- Signing: {r['signing']}", "",
           "| Platform | Backend | Architecture | Archive | Archive bytes | Archive SHA-256 | Player files | Player bytes "
           "| File list SHA-256 | Golden |",
           "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"]
    for a in r["artifacts"]:
        ar = a["archive"]
        out.append(f"| {a['platform']} | {a['backend']} | {a['architecture'] or '-'} | `{ar['name']}` | {ar['bytes']} | "
                   f"`{ar['sha256']}` | {a['files']} | {a['bytes']} | `{a['file_list_sha256']}` | {a['golden']['status']} |")
    out.append("")
    for a in r["artifacts"]:
        g = a["golden"]
        out.append(f"- {a['platform']}: {g['status']}" + (f", exit {g['exit_code']}" if g["exit_code"] is not None else "")
                   + (f"; {g['note']}" if g.get("note") else ""))
        out.extend(f"  - `{line}`" for line in g["lines"])
    out += ["", "The archive is the release asset. The file list (`" + "`, `".join(a["file_list"] for a in r["artifacts"])
            + "`) names every file of the player folder; `shasum -a 256 -c` on a list, run inside the unpacked folder, "
            "checks every file.",
            "Steam build ID: (added by the owner after an upload).", ""]
    return "\n".join(out)


# --- the run -------------------------------------------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    a = parse(sys.argv[1:] if argv is None else argv)
    names = selected(a)
    version, revision = unity_version()
    unity = a.unity or Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")
    if not unity.is_file():
        print(f"NO UNITY EDITOR at {unity}: install Unity {version} ({revision}) through Unity Hub (docs/dev-setup.md)",
              file=sys.stderr)
        return 2
    editor_root = unity.parents[3]   # <version>/Unity.app/Contents/MacOS/Unity
    missing = missing_modules(names, editor_root)
    if missing:
        print(modules_message(missing, version), file=sys.stderr)
        return 2
    dirty = dirty_lines()
    if dirty:
        print("REFUSED: the working tree is not clean. A build record names a commit, so build only a committed tree.\n"
              + "\n".join("  " + l for l in dirty)
              + "\nAfter a Unity run, `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset` "
                "drops the settings churn (and the UNITY_MCP_* defines).", file=sys.stderr)
        return 1

    commit = git("rev-parse", "HEAD").stdout.strip()
    vers = versions()
    started = utc_now()
    out = (a.out or default_out(commit, vers["bundle_version"] or "0", started.strftime("%Y%m%dT%H%M%SZ"))).absolute()
    if inside_repo_and_tracked(out):
        print(f"REFUSED: {out} is inside the repository and not git-ignored; choose a folder outside it (the default "
              "is ~/statecraft-release/).", file=sys.stderr)
        return 1
    shareable = not a.dev
    tag = a.tag or f"v{vers['bundle_version']}"
    if not TAG.fullmatch(tag):
        print(f"REFUSED: {tag!r} is not a release tag (vX.Y.Z)", file=sys.stderr)
        return 1
    plan = [(PLATFORMS[n], out / "players" / n / PLATFORMS[n].output, out / "logs" / f"unity-build-{n}.log") for n in names]
    sim = [sys.executable, str(REPO / "tools/build_sim_for_unity.py")]
    if a.dry_run:
        print("DRY RUN: nothing is built or written.\n" + " ".join(sim))
        for p, target, log in plan:
            print(" ".join(f'"{c}"' if " " in c else c for c in build_command(unity, p, target, log, shareable)))
            print("git checkout -- unity/ProjectSettings unity/Assets/Settings")
            print(f"archive: {out / archive_name(tag, p.name)}")
            if p.runs_here:
                print(" ".join(golden_command(target / "Contents/MacOS/<CFBundleExecutable>", out / "logs/golden-mac.log")))
        print(f"record: {out / 'build-record.json'}")
        return 0

    (out / "logs").mkdir(parents=True, exist_ok=True)
    if run(sim, cwd=REPO).returncode != 0:
        print("FAILED: tools/build_sim_for_unity.py", file=sys.stderr)
        return 1
    expected = expected_goldens()
    artifacts, all_ok = [], True
    for p, target, log in plan:
        print(f"release_build: building {p.name} ({p.backend}) -> {target}", flush=True)
        t0 = time.monotonic()
        try:
            code = run(build_command(unity, p, target, log, shareable), cwd=REPO).returncode
        finally:
            run(["git", "checkout", "--", "unity/ProjectSettings", "unity/Assets/Settings"], cwd=REPO)
        wall = round(time.monotonic() - t0)
        log_text = log.read_text(errors="replace") if log.exists() else ""
        if code != 0 or not target.exists():
            print(f"FAILED: the {p.name} build (exit {code}); see {log}", file=sys.stderr)
            return 1
        folder = out / "players" / p.name
        text, files, size = tree_manifest(folder)
        listing = out / f"files-{p.name}.sha256"
        listing.write_text(text)
        if problem := player_problem(p, folder):
            print(f"FAILED: {problem}", file=sys.stderr)
            return 1
        archive = out / archive_name(tag, p.name)
        make_archive(p, folder, target, archive)
        arch = None
        if p.name == "mac":
            arch = run(["lipo", "-archs", str(mac_executable(target))], capture=True).stdout.strip() or None
        golden = {"status": "unchecked", "exit_code": None, "lines": [],
                  "note": "this player cannot run on the Mac; the CI player golden check runs it (spec 6.3)"}
        if p.runs_here:
            glog = out / "logs" / f"golden-{p.name}.log"
            gcode = run(golden_command(mac_executable(target), glog), cwd=REPO).returncode
            gtext = glog.read_text(errors="replace") if glog.exists() else ""
            golden = {"status": golden_status(gtext, gcode, expected), "exit_code": gcode, "lines": golden_lines(gtext),
                      "log": str(glog.relative_to(out))}
            all_ok &= golden["status"] == "pass"
        artifacts.append({"platform": p.name, "build_target": p.build_target, "backend": p.backend,
                          "architecture": arch, "build_method": METHOD + p.method,
                          "path": str(target.relative_to(out)), "files": files, "bytes": size,
                          "archive": {"name": archive.name, "bytes": archive.stat().st_size,
                                      "sha256": file_sha256(archive)},
                          "file_list_sha256": file_sha256(listing), "file_list": listing.name,
                          "unity_build_seconds": unity_seconds(log_text), "wall_seconds_with_lock_wait": wall,
                          "unity_log": str(log.relative_to(out)), "golden": golden})

    record = {"schema": 1, "tool": "tools/release_build.py", "commit": commit,
              "describe": git("describe", "--tags", "--always", "--dirty").stdout.strip(),
              "branch": git("rev-parse", "--abbrev-ref", "HEAD").stdout.strip(), "tree_clean": True,
              "date_utc": started.strftime("%Y-%m-%dT%H:%M:%SZ"), "unity": {"version": version, "revision": revision},
              "version": vers, "tag": tag, "shareable": shareable, "signing": "none", "golden_expected": expected,
              "artifacts": artifacts, "steam_build_id": None}
    (out / "build-record.json").write_text(json.dumps(record, indent=2) + "\n")
    (out / "build-record.md").write_text(markdown(record))
    print(f"RECORD {out / 'build-record.json'}\nRECORD {out / 'build-record.md'}")
    for a_ in artifacts:
        print(f"{a_['archive']['name']}: sha256 {a_['archive']['sha256']}, {a_['archive']['bytes']} bytes, "
              f"golden {a_['golden']['status']}")
    print("Nothing was pushed or uploaded. Copy the record files to the build records repository.")
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
