# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""GitHub and the site for the nightly release job: the draft-first release, the site deploy and the /demo/ check.

Every call goes through the job's Runner, so tests script gh's JSON and no test touches the network. Anything gh says
that this module cannot parse or does not expect is a Refused, never a pass. Stdlib only."""
from __future__ import annotations

import hashlib
import html
import http.client
import json
import re
import time
import urllib.error
import urllib.request
import zipfile
from datetime import date, timedelta
from pathlib import Path
from typing import Callable

import nightly_checks
from nightly_state import Refused, Runner, next_version

SHA_SHOWN = 12
PR_LIMIT = 500   # `gh pr list` returns at most this many rows; a full page may be a truncated one
RELEASE_LIST_LIMIT = 30
UPLOAD_TIMEOUT = 1800
ITCH_POLL_SECONDS = 30
LIVE_MARK = "\u2713"   # butler prints this in the BUILD cell only once the build is live


def sha256_of(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def sha256_lines(files: list[Path]) -> list[str]:
    """`shasum -a 256` lines (hash, two spaces, file name), as RELEASING step 6 pastes them."""
    return [f"{sha256_of(f)}  {Path(f).name}" for f in files]


def release_notes(*, version: str, sha: str, since: str, subjects: list[str], sha256_lines: list[str]) -> str:
    shown = since[:SHA_SHOWN] if len(since) == 40 and all(c in "0123456789abcdef" for c in since) else since
    lines = [f"Automated nightly build of `main` at {sha[:SHA_SHOWN]}.", "",
             f"Version {version}.", "", f"Changes since {shown}:", ""]
    lines += [f"- {s}" for s in subjects] or ["- (no commits)"]
    lines += ["", "SHA-256:", "", "```", *sha256_lines, "```", ""]
    return "\n".join(lines)


FRAME_URL = re.compile(r"https://html-classic\.itch\.zone/html/[0-9-]+/[^\"'&<>\s]*index\.html")


def itch_frame_url(page_html: str) -> str | None:
    """The game-frame URL the public itch page embeds (None when it embeds none), without its ?v= cache query. The
    page holds it HTML-escaped in a data attribute; its path segment is the upload and build ids joined by "-"."""
    m = FRAME_URL.search(html.unescape(page_html))
    return m.group(0) if m else None


def unzip_to(zip_path: Path, dest: Path) -> Path:
    """Extract a zip into `dest` (created), refusing any entry that would land outside it."""
    dest = Path(dest)
    dest.mkdir(parents=True, exist_ok=True)
    root = dest.resolve()
    try:
        with zipfile.ZipFile(zip_path) as z:
            for info in z.infolist():
                target = (root / info.filename).resolve()
                if target != root and root not in target.parents:
                    raise Refused("unzip", f"{info.filename!r} in {Path(zip_path).name} leaves the target folder")
            z.extractall(root)
    except (zipfile.BadZipFile, OSError) as e:
        raise Refused("unzip", f"cannot extract {Path(zip_path).name}: {e}") from e
    return dest


class Itch:
    """The itch.io channel of the nightly, through butler (which finds its saved login by itself). `target` is
    `user/project:channel`. Every butler failure is a Refused("itch"); nothing here reads or sets a credential."""

    def __init__(self, runner: Runner, butler: Path, target: str):
        project, sep, channel = target.rpartition(":")
        user, slash, name = project.partition("/")
        if not (sep and slash and user and name and channel and "/" not in name and ":" not in project):
            raise Refused("itch", f"the itch target {target!r} is not user/project:channel")
        self.runner, self.butler, self.target = runner, Path(butler), target
        self.project, self.channel = project, channel
        self._floor: int | None = None   # the highest live build id seen; a push is live only with a newer one

    @staticmethod
    def _row(status_text: str, channel: str) -> list[str] | None:
        """The cells of the one table row for `channel`; None when it is missing or appears twice."""
        rows = []
        for line in status_text.splitlines():
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if line.strip().startswith("|") and len(cells) >= 4 and cells[0] == channel:
                rows.append(cells)
        return rows[0] if len(rows) == 1 else None

    @staticmethod
    def live_version(status_text: str, channel: str = "html5") -> str | None:
        """The version of the channel's live build. Anything but a build cell that starts with the live mark (a
        build still processing, an unknown text, a missing row) is None, so it can never count as live."""
        row = Itch._row(status_text, channel)
        if row is None or not row[2].startswith(LIVE_MARK) or not row[3]:
            return None
        return row[3]

    @staticmethod
    def build_id(status_text: str, channel: str = "html5") -> str | None:
        """The id of the channel's live build, under the same rule as live_version."""
        row = Itch._row(status_text, channel)
        if row is None or not row[2].startswith(LIVE_MARK):
            return None
        m = re.search(r"#(\d+)", row[2])
        return m.group(1) if m else None

    def live_build_id(self) -> int | None:
        """The id of the channel's live build now (None when there is none), from `butler status`."""
        return self._live_id()

    def _run(self, args: list[str], cwd: Path, timeout: float = 300) -> str:
        try:
            return self.runner.capture([str(self.butler), *args], cwd=cwd, timeout=timeout)
        except Refused as e:
            raise Refused("itch", e.detail) from e

    def _live_id(self) -> int | None:
        text = self._run(["status", self.project], Path.cwd())
        found = self.build_id(text, self.channel)
        return int(found) if found else None

    def push(self, folder: Path, version: str) -> None:
        """Push `folder`. The live build id is read first, so wait_live can tell the new build from the old row.
        With no readable id nothing is pushed (fail closed); a rollback after a stalled push keeps the earlier floor."""
        current = self._live_id()
        known = [i for i in (current, self._floor) if i is not None]
        if not known:
            raise Refused("itch", f"{self.target} shows no live build id before the push, so the push cannot be confirmed")
        self._floor = max(known)
        self._run(["push", str(folder), self.target, "--userversion", version], Path(folder), UPLOAD_TIMEOUT)

    def wait_live(self, version: str, deadline: float, sleep: Callable[[float], None] = time.sleep,
                  now: Callable[[], float] = time.monotonic) -> None:
        """Poll `butler status` until `version` is live under a build id newer than the one before the push;
        Refused("itch") at the deadline."""
        if self._floor is None:
            raise Refused("itch", "wait_live needs a push first: no build id was read before it")
        seen = "nothing"
        while True:
            try:
                text = self._run(["status", self.project], Path.cwd())
                live = self.live_version(text, self.channel)
                found = self.build_id(text, self.channel)
                if live is not None and found is not None:
                    new_id = int(found)
                    if live == version and new_id > self._floor:
                        self._floor = new_id
                        return
                    seen = f"{live!r} on build {new_id}"
                else:
                    seen = "no live build"
            except Refused as e:   # a failed status call is "not live yet", the deadline decides
                seen = f"a status error ({e.detail})"
            if now() >= deadline:
                raise Refused("itch", f"{self.target} still shows {seen}, not {version} on a build newer than "
                                      f"{self._floor}")
            sleep(ITCH_POLL_SECONDS)

    @staticmethod
    def _hashes(root: Path) -> dict[str, str]:
        return {p.relative_to(root).as_posix(): sha256_of(p) for p in sorted(Path(root).rglob("*")) if p.is_file()}

    def matches(self, folder: Path, dest: Path) -> None:
        """Fetch the live build into `dest` and refuse unless it holds exactly the files that were pushed."""
        dest = Path(dest)
        dest.mkdir(parents=True, exist_ok=True)
        self._run(["fetch", self.target, str(dest)], dest.parent, UPLOAD_TIMEOUT)
        want, got = self._hashes(Path(folder)), self._hashes(dest)
        if want != got:
            diff = sorted(k for k in want.keys() | got.keys() if want.get(k) != got.get(k))
            raise Refused("itch", f"the fetched build differs from the pushed folder in {len(diff)} files, "
                                  f"first: {diff[0]}")


class GitHub:
    """The release and site calls of the nightly. `cwd` is the job's clone (used for `git ls-remote` and `git log`)."""

    def __init__(self, runner: Runner, slug: str, cwd: Path, *, sleep: Callable[[float], None] = time.sleep,
                 now: Callable[[], float] = time.monotonic):
        self.runner, self.slug, self.cwd = runner, slug, Path(cwd)
        self.sleep, self.now = sleep, now

    # --- plumbing --------------------------------------------------------------------------------------------------

    def _run(self, gate: str, cmd: list[str], timeout: float = 300) -> str:
        try:
            return self.runner.capture(cmd, cwd=self.cwd, timeout=timeout)
        except Refused as e:
            raise Refused(gate, e.detail) from e

    def _json(self, gate: str, cmd: list[str], kind: type, timeout: float = 300):
        text = self._run(gate, cmd, timeout)
        try:
            data = json.loads(text)
        except ValueError as e:
            raise Refused(gate, f"{' '.join(cmd[:3])} printed something that is not JSON") from e
        if not isinstance(data, kind):
            raise Refused(gate, f"{' '.join(cmd[:3])} printed JSON of the wrong shape")
        return data

    def _view(self, gate: str, tag: str, fields: str) -> dict:
        return self._json(gate, ["gh", "release", "view", tag, "--repo", self.slug, "--json", fields], dict)

    # --- release state ---------------------------------------------------------------------------------------------

    def next_version(self) -> str:
        """Read every release page through gh; drafts and non-version tags cannot choose the version."""
        pages = self._json("version", ["gh", "api", f"repos/{self.slug}/releases?per_page=100",
                                       "--paginate", "--slurp"], list)
        if not all(isinstance(page, list) for page in pages):
            raise Refused("version", "gh returned release pages of the wrong shape")
        return next_version([row for page in pages for row in page])

    def ensure_tag_available(self, tag: str) -> None:
        """Refuse every remaining release or local/remote tag after owned-draft cleanup."""
        release = self.release_state(tag)
        if release is not None:
            raise Refused("tag-exists", f"a {release} release already has the tag {tag}")
        local = self._run("tag-exists", ["git", "-C", str(self.cwd), "tag", "--list", tag]).strip()
        remote = self._run("tag-exists", ["git", "-C", str(self.cwd), "ls-remote", "origin",
                                          f"refs/tags/{tag}", f"refs/tags/{tag}^{{}}"]).strip()
        if local or remote:
            raise Refused("tag-exists", f"the git tag {tag} already exists; never reused or overwritten")

    def release_state(self, tag: str) -> str | None:
        """'draft', 'published', or None when no release has this tag."""
        try:
            data = self._view("github-state", tag, "isDraft")
        except Refused as e:
            if "release not found" in e.detail:
                return None
            raise
        flag = data.get("isDraft")
        if not isinstance(flag, bool):
            raise Refused("github-state", f"gh did not say whether {tag} is a draft")
        return "draft" if flag else "published"

    def latest_tag(self) -> str | None:
        """The current public Latest, independently of the previous nightly asset baseline."""
        try:
            tag = self._run("github-latest", ["gh", "api", f"repos/{self.slug}/releases/latest", "--jq",
                                              ".tag_name"]).strip()
        except Refused as e:
            if "HTTP 404" in e.detail:
                return None
            raise
        if not tag or tag == "null":
            raise Refused("github-latest", "gh returned no usable Latest tag")
        return tag

    def restore_latest(self, previous: str | None, failed: str) -> tuple[bool, str | None]:
        """Only while the failed release is still Latest: restore and verify the saved Latest (without one, remove the
        failed release's explicit Latest mark). Returns (acted, the Latest now); a Latest that is not the failed
        release, or no Latest at all, is left untouched."""
        current = self.latest_tag()
        if current != failed:
            return False, current
        self._run("github-latest", ["gh", "release", "edit", previous if previous is not None else failed,
                                    "--repo", self.slug, "--latest" if previous is not None else "--latest=false"])
        latest = self.latest_tag()
        if (previous is not None and latest != previous) or (previous is None and latest == failed):
            raise Refused("github-latest", f"Latest recovery did not take effect (Latest is {latest})")
        return True, latest

    def previous_build_tag(self, state_tag: str | None) -> str:
        """The last published nightly, or on the first night the newest release that carries a web zip."""
        if state_tag:
            return state_tag
        rows = self._json("sizes", ["gh", "release", "list", "--repo", self.slug, "--exclude-drafts", "--limit",
                                    str(RELEASE_LIST_LIMIT), "--json", "tagName"], list)
        for row in rows:
            tag = row.get("tagName") if isinstance(row, dict) else None
            if not isinstance(tag, str):
                raise Refused("sizes", "gh release list returned a row without a tag")
            names = self._asset_names("sizes", self._view("sizes", tag, "assets"))
            if any(n.endswith("-web.zip") for n in names):
                return tag
        raise Refused("sizes", f"none of the newest {RELEASE_LIST_LIMIT} releases has a web zip to compare with")

    @staticmethod
    def _asset_names(gate: str, data: dict) -> list[str]:
        assets = data.get("assets")
        if not isinstance(assets, list) or not all(isinstance(a, dict) and isinstance(a.get("name"), str)
                                                   for a in assets):
            raise Refused(gate, "gh returned no usable asset list")
        return [a["name"] for a in assets]

    def download(self, tag: str, pattern: str, dest: Path) -> Path:
        """Download the assets of a release matching `pattern`; exactly one file must arrive."""
        dest = Path(dest)
        dest.mkdir(parents=True, exist_ok=True)
        self._run("github-download", ["gh", "release", "download", tag, "--repo", self.slug, "--dir", str(dest),
                                      "--pattern", pattern], UPLOAD_TIMEOUT)
        found = sorted(dest.glob(pattern))
        if len(found) != 1:
            raise Refused("github-download", f"{len(found)} files matching {pattern} came from {tag}, expected one")
        return found[0]

    # --- the draft, its check, publishing --------------------------------------------------------------------------

    def create_draft(self, tag: str, sha: str, notes: Path, assets: list[Path]) -> None:
        """A normal draft marked Latest, whose tag will be created at exactly `sha` when it is published."""
        self._run("github-draft", ["gh", "release", "create", tag, "--repo", self.slug, "--draft",
                                   "--latest", "--target", sha, "--title", f"{tag} (nightly)",
                                   "--notes-file", str(notes), *[str(a) for a in assets]], UPLOAD_TIMEOUT)

    def verify_draft(self, tag: str, local: list[Path], dest: Path) -> None:
        data = self._view("github-draft", tag, "isDraft,isPrerelease,assets")
        if data.get("isDraft") is not True or data.get("isPrerelease") is not False:
            raise Refused("github-draft", f"{tag} is not a normal draft release")
        names = sorted(self._asset_names("github-draft", data))
        want = sorted(Path(p).name for p in local)
        if names != want:
            raise Refused("github-draft", f"the draft's assets are {names}, expected {want}")
        dest = Path(dest)
        self._run("github-draft", ["gh", "release", "download", tag, "--repo", self.slug, "--dir", str(dest),
                                   "--pattern", "*.zip"], UPLOAD_TIMEOUT)
        for path in local:
            got = dest / Path(path).name
            if not got.is_file() or sha256_of(got) != sha256_of(Path(path)):
                raise Refused("github-draft", f"{Path(path).name} on the draft differs from the local file")
            missing = nightly_checks.zip_missing_notices(got)
            if missing:
                raise Refused("github-draft", f"{got.name} on the draft lacks {', '.join(missing)}")

    def publish(self, tag: str, sha: str) -> None:
        """Make the draft public, then check it is normal, Latest, and its tag points at `sha`."""
        try:
            self._publish(tag, sha)
        except Refused as e:
            raise Refused("github-publish", e.detail) from e

    def _publish(self, tag: str, sha: str) -> None:
        self._run("github-publish", ["gh", "release", "edit", tag, "--repo", self.slug, "--draft=false",
                                     "--latest"])
        data = self._view("github-publish", tag, "isDraft,isPrerelease")
        if data.get("isDraft") is not False or data.get("isPrerelease") is not False:
            raise Refused("github-publish", f"{tag} is not a public normal release after publishing")
        latest = self._run("github-publish", ["gh", "api", f"repos/{self.slug}/releases/latest", "--jq",
                                              ".tag_name"]).strip()
        if latest != tag:
            raise Refused("github-publish", f"{tag} is not the Latest release after publishing (Latest is {latest})")
        refs: dict[str, str] = {}
        for line in self._run("github-publish", ["git", "-C", str(self.cwd), "ls-remote", "origin",
                                                 f"refs/tags/{tag}"]).splitlines():
            parts = line.split()
            if len(parts) == 2:
                refs[parts[1]] = parts[0]
        at = refs.get(f"refs/tags/{tag}^{{}}") or refs.get(f"refs/tags/{tag}")
        if at != sha:
            raise Refused("github-publish", f"the tag {tag} points at {at}, not at {sha}")

    def delete_draft(self, tag: str) -> None:
        """Delete a release only while gh shows it as a draft; never touches a tag."""
        data = self._view("github-delete", tag, "isDraft")
        if data.get("isDraft") is not True:
            raise Refused("github-delete", f"{tag} is not a draft, so it is not deleted")
        self._run("github-delete", ["gh", "release", "delete", tag, "--repo", self.slug, "--yes"])

    # --- the size budget -------------------------------------------------------------------------------------------

    def merged_pr_bodies(self, shas: set[str]) -> list[str]:
        """The bodies of the PRs whose merge commit is one of `shas` (squash and merge commits alike)."""
        if not shas:
            return []
        dates = self._run("sizes", ["git", "-C", str(self.cwd), "log", "--no-walk", "--format=%cs", *sorted(shas)])
        days = [d for d in dates.split() if len(d) == 10]
        if not days:
            raise Refused("sizes", "cannot date the commits since the previous build")
        try:
            since = (date.fromisoformat(min(days)) - timedelta(days=1)).isoformat()   # a day of slack
        except ValueError as e:
            raise Refused("sizes", f"git printed an unreadable commit date: {e}") from e
        rows = self._json("sizes", ["gh", "pr", "list", "--repo", self.slug, "--state", "merged", "--limit",
                                    str(PR_LIMIT), "--search", f"merged:>={since}", "--json", "body,mergeCommit"],
                          list)
        if len(rows) >= PR_LIMIT:
            raise Refused("sizes", f"{len(rows)} merged PRs came back, so the list may be truncated and no size "
                                   "budget can be trusted")
        bodies = []
        for row in rows:
            commit = row.get("mergeCommit") if isinstance(row, dict) else None
            if isinstance(commit, dict) and commit.get("oid") in shas:
                bodies.append(row.get("body") or "")
        return bodies

    # --- the site --------------------------------------------------------------------------------------------------

    def dispatch_site(self, deadline: float) -> None:
        """Run site.yml on main and wait for that run (not an older one) to succeed."""
        listing = ["gh", "run", "list", "--repo", self.slug, "--workflow", "site.yml", "--event",
                   "workflow_dispatch", "--limit", "5", "--json", "databaseId,createdAt"]
        before = {r.get("databaseId") for r in self._json("site", listing, list) if isinstance(r, dict)}
        self._run("site", ["gh", "workflow", "run", "site.yml", "--repo", self.slug, "--ref", "main"])
        run_id = None
        while run_id is None:
            fresh = [r for r in self._json("site", listing, list)
                     if isinstance(r, dict) and r.get("databaseId") not in before]
            if fresh:
                run_id = max(fresh, key=lambda r: str(r.get("createdAt")))["databaseId"]
            elif self.now() >= deadline:
                raise Refused("site", "the dispatched site run did not appear before the deadline")
            else:
                self.sleep(5)
        self._run("site", ["gh", "run", "watch", str(run_id), "--repo", self.slug, "--exit-status"],
                  max(1.0, deadline - self.now()))


def demo_serves(url: str, version: str, *, deadline: float, fetch=urllib.request.urlopen, sleep=time.sleep,
                now=time.monotonic) -> None:
    """Poll the demo page until it prints `version`; Refused("site") at the deadline."""
    sep = "&" if "?" in url else "?"
    probe = f"{url}{sep}nightly={version}"   # a fresh URL, so a cache cannot keep serving the old page
    seen = "nothing"
    while True:
        try:
            with fetch(probe, timeout=30) as resp:
                page = resp.read().decode("utf-8", errors="replace")
            seen = repr(nightly_checks.product_version(page))
            if nightly_checks.product_version(page) == version:
                return
        except (OSError, urllib.error.URLError, http.client.HTTPException, ValueError) as e:
            seen = f"an error ({e})"
        if now() >= deadline:
            raise Refused("site", f"{url} still serves {seen}, not {version}")
        sleep(30)
