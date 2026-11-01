# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""The Steam hand-over: SteamPipe scripts and a never-published draft release for .github/workflows/steam-upload.yml
(plan docs/superpowers/plans/2026-11-01-steamworks-integration.md Task 8, #2155; spec
docs/superpowers/specs/2026-11-01-steamworks-integration-design.md; owner answers Q7 "Also nightlies", Q8 "GitHub
Actions").

Usage:
  uv run tools/steam_upload.py stage --out <release folder> --tag vX.Y.Z      (the owner's Mac, or the nightly)
  uv run tools/steam_upload.py render --root <content> --tag vX.Y.Z --commit <sha> --branch beta|nightly   (CI)
  uv run tools/steam_upload.py verify --dir <downloaded draft> --tag vX.Y.Z --branch beta|nightly           (CI)

- stage reads the folder tools/release_build.py --steam wrote (build-record.json and steam/<platform>/), refuses a
  record that is not a clean, non-dev Steam build of that tag for `beta` or `nightly`, and steam/steam.json while any
  ID is null. It zips each platform folder to steam-<tag>-<platform>.zip (keeping the executable bits and symlinks:
  steamcmd uploads from a Linux machine), writes steam-record.json (tag, commit, branch, each zip's SHA-256 and size),
  attaches them to a DRAFT release steam-<tag> (never published; an existing release of that name is replaced only if
  it is a draft) and runs `gh workflow run steam-upload.yml --ref main -f tag=<tag> -f branch=<branch>`.
- render writes app_build_<appid>.vdf and one depot_build_<depotid>.vdf per platform from steam/app_build.vdf.in and
  steam/depot_build.vdf.in (written for this project from the keys of Valve's SteamPipe documentation,
  https://partner.steamgames.com/doc/sdk/uploading, read 1 Nov 2026), into <root>/../steam-scripts. SetLive is the
  branch; `default` is never accepted (Valve: "the 'default' branch can not be set live automatically"; the owner sets
  it live in App Admin).
- verify is the upload job's check of the downloaded draft against steam-record.json; it prints the record's commit.

None of these takes, reads or prints a credential, and none runs steamcmd: only steam-upload.yml does, on GitHub's
machines, with the two secrets the owner created in the `steam` environment.
"""
import argparse
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from string import Template
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
CONFIG = REPO / "steam/steam.json"
PLATFORMS = ("windows", "mac", "linux")
TAG = re.compile(r"v[0-9]+\.[0-9]+\.[0-9]+", re.ASCII)   # the upload job's own input check
COMMIT = re.compile(r"[0-9a-f]{40}")
WORKFLOW = "steam-upload.yml"
NEVER = "default"   # the branch players get; only the owner sets it live, in App Admin


@dataclass
class Result:
    returncode: int
    stdout: str = ""
    stderr: str = ""


def gh_cli(args: list[str]) -> Result:
    """The one place a process starts: GitHub's own CLI, as the person running this (tests replace it)."""
    p = subprocess.run(["gh", *map(str, args)], cwd=REPO, text=True, capture_output=True)
    return Result(p.returncode, p.stdout, p.stderr)


def load_config(path: Path = CONFIG) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def refuse(config: dict, record: dict | None = None) -> str | None:
    """Why nothing may be uploaded, or None: every null ID by name; with a record, a build that is not a clean,
    non-dev Steam build for one of the configured branches."""
    problems = []
    depots = config.get("depots") or {}
    nulls = [n for n, v in [("app_id", config.get("app_id")), *((f"depots.{p}", depots.get(p)) for p in PLATFORMS)]
             if v is None]
    if nulls:
        problems.append("steam/steam.json has no " + ", ".join(nulls) + " yet (the owner's step, #2151)")
    if record is not None:
        if record.get("steam") is not True:
            problems.append("the record is not a Steam build (tools/release_build.py --steam)")
        if record.get("dev") is not False:
            problems.append("the record is a --dev build, which never goes to Steam")
        if not (record.get("tree_clean") is True and record.get("tree_clean_after_builds") is True):
            problems.append("the record was built from a dirty tree")
        channel = record.get("channel")
        if channel == NEVER or channel not in config.get("branches", []):
            problems.append(f"the record's channel {channel!r} is not one of {config.get('branches')}")
    return "; ".join(problems) or None


def _quoted(value: str) -> str:
    if '"' in value or "\\" in value or "\n" in value:
        raise ValueError(f"{value!r} cannot be written into a VDF string")
    return value


def render(config: dict, root: Path, tag: str, commit: str, branch: str) -> list[Path]:
    """The SteamPipe scripts for one upload; returns their paths (the app script first)."""
    if problem := refuse(config):
        raise ValueError(problem)
    if branch.lower() == NEVER or branch not in config["branches"]:
        raise ValueError(f"{branch!r} is not a branch this job may set live ({config['branches']}; never {NEVER!r})")
    if not TAG.fullmatch(tag) or not COMMIT.fullmatch(commit):
        raise ValueError(f"expected a vX.Y.Z tag and a full commit SHA, not {tag!r} and {commit!r}")
    root = root.resolve()
    scripts = root.parent / "steam-scripts"   # beside the content, never inside it
    scripts.mkdir(parents=True, exist_ok=True)
    app_id, depots = config["app_id"], config["depots"]
    depot_template = Template((REPO / "steam/depot_build.vdf.in").read_text(encoding="utf-8"))
    written = []
    for platform in PLATFORMS:
        path = scripts / f"depot_build_{depots[platform]}.vdf"
        path.write_text(depot_template.substitute(depot_id=int(depots[platform]), platform=platform), encoding="utf-8")
        written.append(path)
    lines = "\n".join(f'\t\t"{int(depots[p])}" "depot_build_{int(depots[p])}.vdf"' for p in PLATFORMS)
    app = scripts / f"app_build_{int(app_id)}.vdf"
    app.write_text(Template((REPO / "steam/app_build.vdf.in").read_text(encoding="utf-8")).substitute(
        app_id=int(app_id), desc=f"{tag} {commit[:8]}", content_root=_quoted(str(root)),
        build_output=_quoted(f"{root}/../steam-output"), branch=branch, depots=lines), encoding="utf-8")
    return [app, *written]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def zip_folder(folder: Path, dest: Path) -> None:
    """A zip of folder's contents with each entry's Unix mode (the executable bits) and symlinks kept as links, so
    `unzip` on the Linux upload machine restores the players as they were built."""
    with zipfile.ZipFile(dest, "w", zipfile.ZIP_DEFLATED) as z:
        for path in sorted(folder.rglob("*")):
            rel = path.relative_to(folder).as_posix()
            mode = os.lstat(path).st_mode
            if path.is_symlink():
                info = zipfile.ZipInfo(rel)
                info.create_system = 3
                info.external_attr = (stat.S_IFLNK | 0o777) << 16
                z.writestr(info, os.readlink(path))
            elif path.is_file():
                info = zipfile.ZipInfo.from_file(path, rel)
                info.create_system = 3
                info.external_attr = (stat.S_IFREG | stat.S_IMODE(mode)) << 16
                info.compress_type = zipfile.ZIP_DEFLATED
                with open(path, "rb") as f:
                    z.writestr(info, f.read())


def stage(out: Path, tag: str, gh: Callable[[list[str]], Result] = gh_cli, config_path: Path = CONFIG) -> None:
    """Zip the Steam folders, write steam-record.json, attach them to the draft steam-<tag> and start the upload."""
    record_path = out / "build-record.json"
    try:
        record = json.loads(record_path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise SystemExit(f"REFUSED: cannot read {record_path}: {e}")
    if problem := refuse(load_config(config_path), record):
        raise SystemExit(f"REFUSED: {problem}")
    if not TAG.fullmatch(tag) or record.get("tag") != tag:
        raise SystemExit(f"REFUSED: the upload takes a vX.Y.Z tag that is the record's ({record.get('tag')!r}), "
                         f"not {tag!r}")
    commit = record.get("commit", "")
    if not COMMIT.fullmatch(commit):
        raise SystemExit(f"REFUSED: the record's commit {commit!r} is not a full SHA")
    folders = {a.get("platform"): a.get("steam_folder") for a in record.get("artifacts", [])}
    if set(folders) != set(PLATFORMS) or any(folders[p] != f"steam/{p}" or not (out / folders[p]).is_dir()
                                             for p in PLATFORMS):
        raise SystemExit(f"REFUSED: a Steam upload needs all three platform folders (steam/{{{','.join(PLATFORMS)}}}); "
                         f"the record has {sorted(folders)}")
    branch = record["channel"]
    assets = {}
    for platform in PLATFORMS:
        dest = out / f"steam-{tag}-{platform}.zip"
        zip_folder(out / "steam" / platform, dest)
        assets[dest.name] = {"sha256": sha256(dest), "size": dest.stat().st_size}
    steam_record = out / "steam-record.json"
    steam_record.write_text(json.dumps({"tag": tag, "commit": commit, "branch": branch, "assets": assets}, indent=2)
                            + "\n", encoding="utf-8")
    name = f"steam-{tag}"
    seen = gh(["release", "view", name, "--json", "isDraft"])
    if seen.returncode == 0:
        if json.loads(seen.stdout).get("isDraft") is not True:
            raise SystemExit(f"REFUSED: {name} is a published release; it is never replaced (check by hand)")
        if (r := gh(["release", "delete", name, "--yes"])).returncode != 0:
            raise SystemExit(f"FAILED: could not delete the old draft {name}: {r.stderr.strip()}")
    elif "release not found" not in seen.stderr:
        raise SystemExit(f"FAILED: gh release view {name}: {seen.stderr.strip()}")
    files = [str(out / n) for n in assets] + [str(steam_record)]
    notes = (f"Steam content for {tag} ({commit[:8]}), branch {branch}. A draft that is never published: "
             "steam-upload.yml downloads it, uploads to Steam and deletes it.")
    if (r := gh(["release", "create", name, "--draft", "--target", commit, "--title", name, "--notes", notes,
                 *files])).returncode != 0:
        raise SystemExit(f"FAILED: gh release create {name}: {r.stderr.strip()}")
    if (r := gh(["workflow", "run", WORKFLOW, "--ref", "main", "-f", f"tag={tag}", "-f", f"branch={branch}"])
            ).returncode != 0:
        raise SystemExit(f"FAILED: gh workflow run {WORKFLOW}: {r.stderr.strip()} (the draft {name} is ready; "
                         "dispatch it again by hand)")
    print(f"STAGED {name} (draft) and started {WORKFLOW} for {tag} on {branch}")


def verify(folder: Path, tag: str, branch: str) -> str:
    """The upload job's check of the downloaded draft: the record names this tag and branch, a full commit, and every
    zip with its exact SHA-256 and size. Returns the commit."""
    try:
        rec = json.loads((folder / "steam-record.json").read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise SystemExit(f"REFUSED: no readable steam-record.json: {e}")
    if rec.get("tag") != tag:
        raise SystemExit(f"REFUSED: the record's tag {rec.get('tag')!r} is not {tag!r}")
    if rec.get("branch") != branch or branch == NEVER:
        raise SystemExit(f"REFUSED: the record's branch {rec.get('branch')!r} is not the input {branch!r}")
    if not COMMIT.fullmatch(str(rec.get("commit", ""))):
        raise SystemExit("REFUSED: the record's commit is not a full SHA")
    wanted = {f"steam-{tag}-{p}.zip" for p in PLATFORMS}
    if set(rec.get("assets", {})) != wanted:
        raise SystemExit(f"REFUSED: the record lists {sorted(rec.get('assets', {}))}, not {sorted(wanted)}")
    for name, meta in rec["assets"].items():
        path = folder / name
        if not path.is_file() or sha256(path) != meta.get("sha256") or path.stat().st_size != meta.get("size"):
            raise SystemExit(f"REFUSED: {name} does not match its sha256 and size in steam-record.json")
    return rec["commit"]


def parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser(description="Steam hand-over: never takes a credential, never runs steamcmd.")
    sub = ap.add_subparsers(dest="command", required=True)
    s = sub.add_parser("stage", help="draft release steam-<tag> and the upload dispatch (the owner's Mac)")
    s.add_argument("--out", type=Path, required=True, help="the folder tools/release_build.py --steam wrote")
    s.add_argument("--tag", required=True)
    r = sub.add_parser("render", help="the SteamPipe scripts (the upload job)")
    r.add_argument("--root", type=Path, required=True, help="the content root holding windows/, mac/ and linux/")
    r.add_argument("--tag", required=True)
    r.add_argument("--commit", required=True)
    r.add_argument("--branch", required=True, choices=["beta", "nightly"])
    v = sub.add_parser("verify", help="check a downloaded draft against steam-record.json (the upload job)")
    v.add_argument("--dir", type=Path, required=True)
    v.add_argument("--tag", required=True)
    v.add_argument("--branch", required=True, choices=["beta", "nightly"])
    return ap


def main(argv: list[str] | None = None) -> int:
    a = parser().parse_args(argv)
    if a.command == "stage":
        stage(a.out, a.tag)
    elif a.command == "render":
        try:
            paths = render(load_config(), a.root, a.tag, a.commit, a.branch)
        except ValueError as e:
            print(f"REFUSED: {e}", file=sys.stderr)
            return 1
        print(paths[0])
    else:
        print(verify(a.dir, a.tag, a.branch))
    return 0


if __name__ == "__main__":
    sys.exit(main())
