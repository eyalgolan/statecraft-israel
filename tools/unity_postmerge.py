# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Post-merge Unity EditMode watcher for the owner's Mac (T72; owner, 16 October 2026: "Approve, failures to issue").

CI cannot run Unity (CONTRIBUTING.md, "What CI runs"), so two PRs can each be green and still break Unity together on
`main`. A launchd job (tools/unity_postmerge.plist.in) runs this script every 15 minutes. Each run:

1. fetches origin/main in the main checkout and reads its tip. If the tip is the commit this job last tested, it stops.
   Only the tip is tested: when several commits landed since the last run, the issue lists them all (`prev..tip`).
2. checks the tip out in its own worktree (~/.cache/statecraft/unity-postmerge/worktree, forced, `git clean -ffdx`
   except unity/Library and the Unity-MCP NuGet download). The first run APFS-clones the main checkout's Library.
   launchd starts tools/unity_postmerge_launch.py, which moves this worktree to origin/main and runs this script from
   it, so a merged change to the script takes effect on the next run.
3. builds the sim DLLs (tools/build_sim_for_unity.py), then runs the full EditMode suite through
   `machine_lock.py --shared --wait-unity --timeout 14400`, never with -nographics, and afterwards reverts
   unity/ProjectSettings and unity/Assets/Settings (also on an exception). A Unity still running after
   --unity-timeout-min (90) minutes, counted from its start, is stopped by the PID machine_lock recorded for it
   (SIGTERM, then SIGKILL after 60 s), never by name, and the run is reported as timed out.
4. passes only when Unity exits 0 and its result file names at least one test and no failure. A pass appends one line
   to results.log and nothing else. Anything else (failing tests, 0 tests, no result file, a compile error, a sim build
   failure, a lock or Unity timeout) is reported once for that commit, with the SHA, the range since the last tested
   commit, the failing test names and the log tail. When an issue this job opened is still open (main is still
   broken), the report is a comment on it; otherwise it is a new issue, labelled `area: unity` when the label exists.
   An open issue that already names the commit, or the job's own record of every commit it reported, means no report.
   The log tail, compile errors, test messages, broken-run reason and commit list are redacted first (redact():
   email addresses, the home folder, the user name); if redacting raises, the report says "Log withheld" and
   carries none of them.
5. deletes unity/Builds, Temp and Logs in the worktree, keeps the last KEEP_RUNS run folders, and deletes the
   worktree's Library when it grows past --cap-gb (the next run clones it again).

If main's tip has no PASS or FAIL line in results.log STALE_AFTER (6 h) after it was committed, each run until then
appends one STALE line for it to results.log (and launchd.log), so a job that is stuck or cannot report is visible.

If opening the issue fails, the report is kept in the state file and retried on the next run without running Unity
again. The state is advanced only after a pass is logged or the issue exists. It uses the existing `gh` login and
never reads, prints or stores a token.

Usage: uv run tools/unity_postmerge.py [--repo PATH] [--home DIR] [--cap-gb N] [--unity-timeout-min N] [--dry-run]
--dry-run runs everything but prints the issue instead of opening it and leaves the state file alone.
Exit 0 when nothing was new, the commit passed or its issue exists; 1 when fetching or reporting failed."""
from __future__ import annotations

import argparse
import fcntl
import getpass
import json
import os
import re
import shutil
import subprocess
import signal
import sys
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path

import machine_lock   # tools/: the holder record that names the Unity process to stop on a timeout

TOOLS = Path(__file__).resolve().parent
REPO_SLUG = "eyalgolan/statecraft-israel"
LABEL = "area: unity"
LABEL_ID = "com.statecraft.unity-postmerge"
LOCK_TIMEOUT = 14400
UNITY_TIMEOUT_MIN = 90     # from Unity's start, not counting the lock wait
UNITY_GRACE = 60           # seconds between SIGTERM and SIGKILL
LOCKED_ENV = "UNITY_POSTMERGE_LOCKED"   # set by unity_postmerge_launch.py, which already holds run.lock
KEEP_RUNS = 10
LOG_TAIL = 80
MAX_FAILURES = 50
MAX_MESSAGE = 500
# Kept between runs: Unity's import cache, and the Unity-MCP plugin's NuGet download (it fetches it on first open).
KEEP = ("/unity/Library/", "/unity/Assets/Plugins/NuGet/", "/unity/Assets/Plugins/NuGet.meta")
SHA_RE = re.compile(r"^[0-9a-f]{40}$")
# No result (PASS or FAIL) for main's tip this long after the commit means the job is stuck or failing quietly: the
# longest legitimate run is the lock wait (LOCK_TIMEOUT), Unity (UNITY_TIMEOUT_MIN) and 30 min for the poll and build.
STALE_AFTER = LOCK_TIMEOUT + UNITY_TIMEOUT_MIN * 60 + 30 * 60     # 6 h
EMAIL_RE = re.compile(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+")
HOME_RE = re.compile(r"(/Users/|/home/)[^/\s'\"]+")
PIPE_RE = re.compile(r"(LicenseClient-)[^\s/'\"]+?(?=-\d|[\s/'\"]|$)")


# --- change detection -------------------------------------------------------------------------------------------

def load_state(path: Path) -> dict:
    try:
        state = json.loads(path.read_text(encoding="utf-8"))
        return state if isinstance(state, dict) else {}
    except (OSError, ValueError):
        return {}


def save_state(path: Path, state: dict) -> None:
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(json.dumps(state, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def fetch_tip(git) -> str:
    git("fetch", "--quiet", "origin", "+refs/heads/main:refs/remotes/origin/main")
    tip = git("rev-parse", "--verify", "refs/remotes/origin/main^{commit}").strip()
    if not SHA_RE.match(tip):
        raise RuntimeError(f"origin/main did not resolve to a commit: {tip!r}")
    return tip


def commit_to_test(tip: str, state: dict) -> str | None:
    return None if state.get("last_tested") == tip else tip


def commit_time(git, sha: str) -> int:
    return int(git("show", "-s", "--format=%ct", sha).strip())


def stale_note(log_text: str, tip: str, tip_epoch: float, now: float, state: dict) -> str | None:
    """A results.log line when main's tip has no PASS or FAIL line STALE_AFTER after it was committed (and none was
    logged for it yet), so a job that is stuck or failing quietly shows in results.log, not only in launchd.log."""
    if state.get("last_tested") == tip or re.search(rf" {tip} (PASS|FAIL|STALE) ", log_text + " "):
        return None
    age = now - tip_epoch
    if age <= STALE_AFTER:
        return None
    return (f"{tip} STALE no PASS or FAIL logged {age / 3600:.1f} h after the commit (bound {STALE_AFTER // 3600} h); "
            "see launchd.log")


# --- results ----------------------------------------------------------------------------------------------------

@dataclass
class Results:
    total: int = 0
    passed: int = 0
    failed: int = 0
    failures: list[tuple[str, str]] = field(default_factory=list)
    broken: str | None = None


def parse_results(path: Path) -> Results:
    """Unity's NUnit 3 result file. A missing or unreadable file, or one with 0 tests, is a broken run (never a pass)."""
    if not path.is_file():
        return Results(broken=f"no result file was written ({path.name}): Unity did not run the tests")
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as e:
        return Results(broken=f"the result file could not be read: {e}")
    r = Results(total=int(root.get("total", 0)), passed=int(root.get("passed", 0)), failed=int(root.get("failed", 0)))
    for case in root.iter("test-case"):
        if case.get("result") == "Failed":
            message = (case.findtext("failure/message") or "").strip()
            r.failures.append((case.get("fullname") or case.get("name") or "?", message))
    if r.total == 0:
        r.broken = "0 tests ran: a broken run (the suite did not load, or the filter matched nothing)"
    return r


def timed_out(pid: int, minutes: float) -> Results:
    return Results(broken=f"timed out: Unity (PID {pid}) ran longer than {minutes:g} min and was stopped")


def passed(r: Results, unity_exit: int) -> bool:
    return unity_exit == 0 and r.broken is None and r.total > 0 and r.failed == 0 and not r.failures


# --- issues -----------------------------------------------------------------------------------------------------

MARKER_PREFIX = "<!-- unity-postmerge sha="


def marker(sha: str) -> str:
    return f"{MARKER_PREFIX}{sha} -->"


def issue_title(sha: str) -> str:
    return f"Unity EditMode broken on main at {sha[:12]} ({sha})"


def existing_issue(issues: list[dict], sha: str) -> str | None:
    for i in issues:
        if marker(sha) in (i.get("body") or "") or sha in (i.get("title") or ""):
            return i.get("url") or f"#{i.get('number')}"
    return None


def redact(text: str) -> str:
    """Strip what identifies the Mac's owner before text goes into a public issue: every email address, the home
    folder (to ~), any /Users/<name> or /home/<name>, Unity's LicenseClient-<name> pipe, and the user name itself."""
    text = EMAIL_RE.sub("<email>", text)
    for home in {os.environ.get("HOME", ""), str(Path.home())}:
        if len(home.rstrip("/")) > 1:
            text = text.replace(home.rstrip("/"), "~")
    text = HOME_RE.sub(r"\1<user>", text)
    text = PIPE_RE.sub(r"\1<user>", text)
    for user in {getpass.getuser(), os.environ.get("USER", "")}:
        if user:
            text = re.sub(re.escape(user), "<user>", text, flags=re.IGNORECASE)
    return text


def _tail(text: str, n: int) -> str:
    return "\n".join(text.splitlines()[-n:])


def _fence(text: str) -> str:
    return "```\n" + text.replace("```", "'''") + "\n```"


def issue_body(sha: str, prev: str | None, r: Results, unity_exit: int, log_text: str, commits: str) -> str:
    rng = f"{prev[:12]}..{sha[:12]}" if prev else sha[:12]
    try:                     # fail closed: if redacting fails, no log text and no test message leaves the Mac
        log_text, commits = redact(log_text), redact(commits)
        broken = redact(r.broken) if r.broken else None      # e.g. git's stderr when the worktree can't be prepared
        failures, withheld = [(redact(n), redact(m)) for n, m in r.failures], None
    except Exception as e:   # noqa: BLE001 (any error withholds; only its type is shown, never its text)
        log_text, failures = "", [(n, "") for n, _ in r.failures]
        broken = "details withheld (see below)" if r.broken else None
        withheld = (f"Log withheld: redacting it failed ({type(e).__name__}), so the log tail, compile errors, "
                    "test messages and the broken-run reason stay in the run folder on the Mac.")
    out = [f"The post-merge Unity EditMode run (tools/unity_postmerge.py, on the owner's Mac) did not pass on `main` "
           f"at {sha}.", "", f"- Commit: https://github.com/{REPO_SLUG}/commit/{sha}"]
    if prev:
        out.append(f"- Untested since the last run: {rng} (https://github.com/{REPO_SLUG}/compare/{prev}...{sha})")
    out.append(f"- Unity exit code: {unity_exit}")
    if broken:
        out.append(f"- **Broken run:** {broken}")
    else:
        out.append(f"- Result: {r.failed} failed of {r.total} ({r.passed} passed)")
    if commits.strip():
        out += ["", f"Commits in {rng}:", _fence(_tail(commits.strip(), 30))]
    if failures:
        out += ["", "Failing tests:"]
        for name, message in failures[:MAX_FAILURES]:
            msg = " ".join(message.split())
            msg = msg[:MAX_MESSAGE] + ("..." if len(msg) > MAX_MESSAGE else "")
            out.append(f"- `{name}`" + (f": {msg}" if msg else ""))
        if len(r.failures) > MAX_FAILURES:
            out.append(f"- ... and {len(r.failures) - MAX_FAILURES} more (see the result file on the Mac)")
    errors = [line for line in log_text.splitlines() if "error CS" in line]
    if errors:
        out += ["", "Compile errors:", _fence("\n".join(dict.fromkeys(errors))[:8000])]
    if withheld:
        out += ["", withheld]
    else:
        out += ["", f"Last {LOG_TAIL} lines of the Unity log (home folder, user name and email addresses redacted):",
                _fence(_tail(log_text, LOG_TAIL)[-20000:])]
    out += ["", "Run on the commit itself, in a clean worktree. While `main` stays broken, each newly failing commit "
            "is a comment on this issue, not a new issue. Close this when `main` is green again.", marker(sha)]
    return "\n".join(out)


def open_postmerge_issue(issues: list[dict]) -> dict | None:
    """The oldest open issue this job opened (its body carries a marker), the one a still-broken main is reported on."""
    ours = [i for i in issues if MARKER_PREFIX in (i.get("body") or "")]
    return min(ours, key=lambda i: int(i["number"])) if ours else None


def report(gh, state: dict, sha: str, title: str, body: str) -> str:
    """Report a commit once: nothing when this job already reported it or an open issue names it; a comment on the open
    post-merge issue when there is one (main is still broken); otherwise a new issue."""
    issued = state.setdefault("issued", {})
    if sha in issued:
        return issued[sha]
    issues = json.loads(gh(["issue", "list", "--repo", REPO_SLUG, "--state", "open", "--limit", "1000",
                            "--json", "number,title,body,url"]))
    found = existing_issue(issues, sha)
    if found:
        issued[sha] = found
        return found
    if still_open := open_postmerge_issue(issues):
        url = gh(["issue", "comment", str(still_open["number"]), "--repo", REPO_SLUG, "--body-file", "-"],
                 stdin=f"Still broken at {sha}.\n\n{body}").strip().splitlines()[-1]
        issued[sha] = url
        return url
    labels = {lab["name"] for lab in json.loads(gh(["label", "list", "--repo", REPO_SLUG, "--limit", "500",
                                                     "--json", "name"]))}
    args = ["issue", "create", "--repo", REPO_SLUG, "--title", title, "--body-file", "-"]
    if LABEL in labels:
        args += ["--label", LABEL]
    url = gh(args, stdin=body).strip().splitlines()[-1]
    issued[sha] = url
    return url


# --- running ----------------------------------------------------------------------------------------------------

def unity_path(version: str) -> str:
    return f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity"


def unity_command(wt: Path, version: str, xml: Path, log: Path) -> list[str]:
    return ["uv", "run", str(TOOLS / "machine_lock.py"), "--shared", "--wait-unity", "--timeout", str(LOCK_TIMEOUT),
            "--", unity_path(version), "-batchmode", "-CI", "false", "-GITHUB_ACTIONS", "false", "-TF_BUILD", "false",
            "-projectPath", str(wt / "unity"), "-buildTarget", "OSXUniversal", "-runTests", "-testPlatform", "EditMode",
            "-testResults", str(xml), "-logFile", str(log)]


def fill_plist(text: str, *, uv: str, repo: str, home: str, path: str) -> str:
    for key, value in (("UV", uv), ("REPO", repo), ("HOME", home), ("PATH", path)):
        text = text.replace(f"@{key}@", value)
    return text


def _parent(pid: int) -> int | None:
    out = subprocess.run(["ps", "-o", "ppid=", "-p", str(pid)], capture_output=True, text=True).stdout.strip()
    return int(out) if out.isdigit() else None


def _holder(record: Path) -> tuple[int, int] | None:
    try:
        lock_pid, child = (int(x) for x in record.read_text().split())
        return lock_pid, child
    except (OSError, ValueError):
        return None


def run_with_timeout(cmd: list[str], record: Path, *, timeout: float, grace: float, poll: float = 5.0, cwd: Path,
                     stdout) -> tuple[int, int | None]:
    """Run `cmd` (machine_lock.py wrapping Unity). The clock starts when machine_lock records the command it started
    (machine_lock.HOLDER_ENV), so waiting for the lock never counts. Past `timeout` seconds, that recorded PID gets
    SIGTERM, then SIGKILL after `grace` seconds, each only while its parent is still the recorded machine_lock (so a
    reused PID is never signalled). machine_lock then returns and its lock is released when it exits. Returns the
    exit code and the PID that was stopped, or None when the command finished in time."""
    record.unlink(missing_ok=True)
    proc = subprocess.Popen(cmd, cwd=cwd, stdout=stdout, stderr=subprocess.STDOUT,
                            env={**os.environ, machine_lock.HOLDER_ENV: str(record)})
    started = holder = None
    while (code := proc.poll()) is None:
        if started is None and (holder := _holder(record)):
            started = time.monotonic()
        if started is not None and time.monotonic() - started > timeout:
            break
        time.sleep(poll)
    else:
        return code, None
    lock_pid, child = holder
    for sig, wait in ((signal.SIGTERM, grace), (signal.SIGKILL, grace)):
        if _parent(child) != lock_pid:
            break
        os.kill(child, sig)
        deadline = time.monotonic() + wait
        while _parent(child) == lock_pid and time.monotonic() < deadline:
            time.sleep(min(poll, 0.5))
    try:
        proc.wait(grace + 60)
    except subprocess.TimeoutExpired:      # machine_lock did not return: stop it (and so its lock) by its own PID
        proc.kill()
        proc.wait()
    return proc.returncode, child


def _git(cwd: Path):
    def run(*args: str) -> str:
        return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, check=True).stdout
    return run


def _gh(args: list[str], stdin: str | None = None) -> str:
    p = subprocess.run(["gh", *args], input=stdin, capture_output=True, text=True)
    if p.returncode:
        raise RuntimeError(f"gh {args[0]} {args[1]} failed (exit {p.returncode}): {p.stderr.strip()[-500:]}")
    return p.stdout


def prepare_worktree(repo: Path, wt: Path, sha: str) -> None:
    git = _git(repo)
    if not (wt / ".git").exists():
        git("worktree", "prune")
        wt.parent.mkdir(parents=True, exist_ok=True)
        git("worktree", "add", "--detach", "--force", str(wt), sha)
    w = _git(wt)
    w("checkout", "--force", "--detach", sha)
    w("clean", "-ffdx", *[x for k in KEEP for x in ("-e", k)])
    if w("status", "--porcelain").strip():
        raise RuntimeError("the worktree is not clean after checkout and clean")
    lib, src = wt / "unity" / "Library", repo / "unity" / "Library"
    if not lib.exists() and src.is_dir():
        subprocess.run(["cp", "-c", "-R", str(src), str(lib)], check=True)


def du_bytes(path: Path) -> int:
    if not path.exists():
        return 0
    out = subprocess.run(["du", "-sk", str(path)], capture_output=True, text=True).stdout.split()
    return int(out[0]) * 1024 if out and out[0].isdigit() else 0


def tidy(wt: Path, runs: Path, cap_bytes: int) -> str:
    for d in ("Builds", "Temp", "Logs"):
        shutil.rmtree(wt / "unity" / d, ignore_errors=True)
    old = sorted((p for p in runs.iterdir() if p.is_dir()), key=lambda p: p.stat().st_mtime)[:-KEEP_RUNS]
    for p in old:
        shutil.rmtree(p, ignore_errors=True)
    lib = wt / "unity" / "Library"
    size = du_bytes(lib)
    if size > cap_bytes:
        shutil.rmtree(lib, ignore_errors=True)
        return f"library {size >> 20} MiB > cap, deleted"
    return f"library {size >> 20} MiB"


def run_suite(repo: Path, wt: Path, sha: str, run_dir: Path, timeout_min: float) -> tuple[Results, int, str]:
    xml, log, build_log = run_dir / "editmode.xml", run_dir / "unity.log", run_dir / "build-sim.log"
    try:
        prepare_worktree(repo, wt, sha)
    except (subprocess.CalledProcessError, RuntimeError) as e:
        detail = getattr(e, "stderr", "") or str(e)
        return Results(broken=f"the worktree could not be prepared: {detail.strip()[-500:]}"), -1, ""
    with open(build_log, "w") as f:
        b = subprocess.run(["uv", "run", "tools/build_sim_for_unity.py"], cwd=wt, stdout=f, stderr=subprocess.STDOUT)
    if b.returncode:
        text = build_log.read_text(errors="replace")
        return Results(broken=f"building the sim DLLs failed (exit {b.returncode})"), b.returncode, text
    m = re.search(r"m_EditorVersion: (\S+)", (wt / "unity/ProjectSettings/ProjectVersion.txt").read_text())
    version = m.group(1) if m else "?"
    if not Path(unity_path(version)).is_file():
        return Results(broken=f"Unity {version} (unity/ProjectSettings/ProjectVersion.txt) is not installed"), -1, ""
    try:
        with open(run_dir / "machine-lock.log", "w") as f:
            code, stopped = run_with_timeout(unity_command(wt, version, xml, log), run_dir / "lock-holder", cwd=wt,
                                             stdout=f, timeout=timeout_min * 60, grace=UNITY_GRACE)
    finally:
        subprocess.run(["git", "checkout", "--", "unity/ProjectSettings", "unity/Assets/Settings"], cwd=wt,
                       capture_output=True)
    text = log.read_text(errors="replace") if log.is_file() else ""
    if stopped is not None:
        return timed_out(stopped, timeout_min), code, text
    if code == 3 and not log.is_file():
        lock_text = (run_dir / "machine-lock.log").read_text(errors="replace")
        return Results(broken=f"machine_lock timed out after {LOCK_TIMEOUT} s waiting for Unity"), code, lock_text
    return parse_results(xml), code, text


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--repo", type=Path, default=TOOLS.parent, help="the main checkout (default: this script's)")
    ap.add_argument("--home", type=Path, default=Path.home() / ".cache" / "statecraft" / "unity-postmerge")
    ap.add_argument("--cap-gb", type=float, default=6.0, help="delete the worktree's Library above this size")
    ap.add_argument("--unity-timeout-min", type=float, default=UNITY_TIMEOUT_MIN,
                    help="stop Unity (by its recorded PID) after this many minutes of running")
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args(argv)
    repo, home = a.repo.resolve(), a.home
    runs = home / "runs"
    runs.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    with open(home / "run.lock", "a+") as lock:
        if os.environ.get(LOCKED_ENV) != "1":       # the launcher holds run.lock for this run
            try:
                fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
            except BlockingIOError:
                print("unity_postmerge: another run is in progress")
                return 0
        state_path = home / "state.json"
        state = load_state(state_path)
        try:
            tip = fetch_tip(_git(repo))
        except (subprocess.CalledProcessError, RuntimeError) as e:
            print(f"unity_postmerge: fetching origin/main failed: {getattr(e, 'stderr', '') or e}", file=sys.stderr)
            return 1
        results_log = home / "results.log"
        logged = results_log.read_text() if results_log.is_file() else ""
        try:
            note = stale_note(logged, tip, commit_time(_git(repo), tip), time.time(), state)
        except (subprocess.CalledProcessError, ValueError) as e:      # fail loud: a check that cannot run says so
            note = None if f" {tip} STALE " in logged else f"{tip} STALE check failed: {e}"
        if note:
            with open(results_log, "a") as f:
                f.write(f"{stamp} {note}\n")
            print(f"unity_postmerge: {note}", file=sys.stderr)
        if (pending := state.get("pending")) and not a.dry_run:     # a report that could not be opened last time
            try:
                url = report(_gh, state, pending["sha"], pending["title"], pending["body"])
            except (RuntimeError, ValueError, OSError) as e:
                print(f"unity_postmerge: reporting {pending['sha'][:12]} failed again: {e}", file=sys.stderr)
                return 1
            state["last_tested"] = state.pop("pending")["sha"]
            save_state(state_path, state)
            print(f"unity_postmerge: reported {state['last_tested'][:12]}: {url}")
        sha = commit_to_test(tip, state)
        if sha is None:
            return 0
        prev = state.get("last_tested")
        run_dir = runs / f"{stamp.replace(':', '')}-{sha[:12]}"
        run_dir.mkdir()
        r, code, log_text = run_suite(repo, home / "worktree", sha, run_dir, a.unity_timeout_min)
        disk = tidy(home / "worktree", runs, int(a.cap_gb * 2**30))
        ok = passed(r, code)
        line = (f"{stamp} {sha} {'PASS' if ok else 'FAIL'} exit={code} total={r.total} passed={r.passed} "
                f"failed={r.failed}{' broken' if r.broken else ''} {disk} run={run_dir.name}")
        with open(results_log, "a") as f:
            f.write(line + "\n")
        print(line)
        if a.dry_run:
            if not ok:
                print(issue_title(sha) + "\n\n" + issue_body(sha, prev, r, code, log_text, ""))
            return 0
        if ok:
            state["last_tested"] = sha
            save_state(state_path, state)
            return 0
        commits = ""
        if prev:
            try:
                commits = _git(repo)("log", "--oneline", "--no-decorate", f"{prev}..{sha}")
            except subprocess.CalledProcessError:
                commits = ""
        title, body = issue_title(sha), issue_body(sha, prev, r, code, log_text, commits)
        try:
            url = report(_gh, state, sha, title, body)
        except (RuntimeError, ValueError, OSError) as e:
            state["pending"] = {"sha": sha, "title": title, "body": body}
            save_state(state_path, state)
            print(f"unity_postmerge: opening the issue failed, kept for the next run: {e}", file=sys.stderr)
            return 1
        state["last_tested"] = sha
        save_state(state_path, state)
        print(f"unity_postmerge: {url}")
        return 0


if __name__ == "__main__":
    sys.exit(main())
