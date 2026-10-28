# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""The nightly release job: a bounded command runner, the preflight and the run skeleton (stdlib only).

The job works in its own clone of the repository, because release_gate.sh refuses unless `main` is checked out and
the owner's checkout already holds it. Every way a night can end early is a Stop from nightly_state; run_night turns
each one, and any unexpected failure, into one line of record, so no night leaves without a trace."""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import statistics
import signal
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from dataclasses import dataclass, field
from datetime import date, datetime
from pathlib import Path
from typing import Callable

import nightly_checks
import unity_postmerge
from nightly_publish import (GitHub, Itch, demo_serves, itch_frame_url, release_notes, sha256_lines, sha256_of,
                             unzip_to)
from nightly_state import (JOB, TZ, Config, Missed, Published, Refused, Runner, Skipped, State, Stop,
                           enable_itch, late_start, load_config, load_state, log_line, missed_days, night_of,
                           notify, record, run_lock, save_state, tag_for)
from web_check import find_chrome

HOLDER_ENV = unity_postmerge.machine_lock.HOLDER_ENV
STAGE_ERRORS = (FileNotFoundError, OSError, subprocess.SubprocessError)


# --- the runner -------------------------------------------------------------------------------------------------

class ProcRunner(Runner):
    """Runs the job's real commands. `grace` is the pause between SIGTERM and SIGKILL at the deadline."""

    def __init__(self, grace: float = 60):
        self.grace = grace

    def run(self, cmd: list[str], *, cwd: Path, log: Path, deadline: float,
            env: dict[str, str] | None = None, holder: Path | None = None) -> int:
        full = dict(os.environ if env is None else env)
        if holder is not None:
            holder.unlink(missing_ok=True)
            full[HOLDER_ENV] = str(holder)
        Path(log).parent.mkdir(parents=True, exist_ok=True)
        with open(log, "ab") as out:
            proc = subprocess.Popen(cmd, cwd=cwd, stdout=out, stderr=subprocess.STDOUT, env=full,
                                    start_new_session=True)
            try:
                return proc.wait(timeout=max(0.0, deadline - time.monotonic()))
            except subprocess.TimeoutExpired:
                pass
            if holder is not None:
                self._stop_holder(holder)
            self._stop_group(proc)
        return 124

    def _stop_holder(self, record_file: Path) -> None:
        """Stop the command machine_lock recorded (the Unity process), while it is still machine_lock's child."""
        held = unity_postmerge._holder(record_file)
        if held is None:
            return
        lock_pid, child = held
        for sig in (signal.SIGTERM, signal.SIGKILL):
            if unity_postmerge._parent(child) != lock_pid:
                return
            try:
                os.kill(child, sig)
            except ProcessLookupError:
                return
            end = time.monotonic() + self.grace
            while unity_postmerge._parent(child) == lock_pid and time.monotonic() < end:
                time.sleep(min(0.5, self.grace))

    def _stop_group(self, proc: subprocess.Popen) -> None:
        try:
            os.killpg(proc.pid, signal.SIGTERM)
            proc.wait(timeout=self.grace)
        except (ProcessLookupError, subprocess.TimeoutExpired):
            pass
        try:   # always: a member that ignores SIGTERM (a build's grandchild) must not outlive the deadline
            os.killpg(proc.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        try:
            proc.wait(timeout=self.grace + 5)
        except subprocess.TimeoutExpired:
            pass

    def capture(self, cmd: list[str], *, cwd: Path, timeout: float = 300) -> str:
        try:
            p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, timeout=timeout)
        except subprocess.TimeoutExpired as e:
            raise Refused("command", f"{cmd[0]} timed out after {timeout:g}s") from e
        if p.returncode:
            raise Refused("command", f"{' '.join(cmd[:3])} exited {p.returncode}: {p.stderr.strip()[-300:]}")
        return p.stdout


# --- preflight --------------------------------------------------------------------------------------------------

@dataclass
class Night:
    now: datetime
    day: date
    sha: str
    version: str
    tag: str
    run_dir: Path
    deadline: float
    job: Path
    cfg: Config
    manifest: str
    notes: list[str] = field(default_factory=list)   # lines for the night's LOG.md entry


def _git(runner: Runner, clone: Path, *args: str) -> str:
    return runner.capture(["git", "-C", str(clone), *args], cwd=clone).strip()


def _check_clone(runner: Runner, clone: Path) -> str:
    branch = _git(runner, clone, "rev-parse", "--abbrev-ref", "HEAD")
    sha = _git(runner, clone, "rev-parse", "HEAD")
    origin = _git(runner, clone, "rev-parse", "refs/remotes/origin/main")
    dirty = _git(runner, clone, "status", "--porcelain", "--untracked-files=no")
    if branch != "main":
        raise Refused("clone", f"the clone is on {branch!r}, not main")
    if sha != origin:
        raise Refused("clone", f"the clone is at {sha[:12]}, not origin/main {origin[:12]}")
    if dirty:
        raise Refused("clone", "the clone has uncommitted changes to tracked files")
    return sha


def _editor_version(clone: Path) -> str:
    text = (clone / "unity/ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
    m = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", text, re.M)
    if not m:
        raise Refused("unity-editor", "no m_EditorVersion in ProjectVersion.txt")
    return m.group(1)


SITE_WAIT_SECONDS = 15 * 60
BASELINE_NIGHTS = 5


def preflight(cfg: Config, state: State, runner: Runner, gh: GitHub, *, job: Path, now: datetime, manual: bool,
              free_gb: Callable[[Path], float], no_publish: bool = False) -> Night:
    day = night_of(now)
    if late_start(now, cfg.window_end) and not manual:
        raise Missed(f"started after {cfg.window_end}")
    clone = cfg.clone
    sha = _check_clone(runner, clone)
    last = state.last_published
    if last is not None and sha == last.sha:
        detail = f"main unchanged since {last.tag}"
        if state.site_pending and not no_publish:   # a deploy is a public change, so --no-publish never starts one
            try:
                gh.dispatch_site(time.monotonic() + SITE_WAIT_SECONDS)
                demo_serves(cfg.site_demo_url, state.site_pending,
                            deadline=time.monotonic() + SITE_WAIT_SECONDS)
                state.site_pending = None
                detail += "; the pending site deploy now serves"
            except Exception as e:   # any error leaves the deploy pending for the next night, and the night skipped
                detail += (f"; the pending site deploy still failed: "
                           f"{e.detail if isinstance(e, Stop) else f'{type(e).__name__}: {e}'}")
        raise Skipped(detail)
    free = free_gb(clone)
    if free < cfg.min_free_gb:
        raise Refused("disk", f"{free:.1f} GB free, {cfg.min_free_gb} GB needed")
    if cfg.exclusive_marker.exists():
        raise Refused("exclusive-window", f"{cfg.exclusive_marker} exists")
    if not cfg.licence.is_file():   # stat only, never read
        raise Refused("unity-licence", f"{cfg.licence} is missing")
    editor = Path(unity_postmerge.unity_path(_editor_version(clone)))
    if not editor.exists():
        raise Refused("unity-editor", f"{editor} is missing")
    if not cfg.unity_slots.is_file():
        raise Refused("unity-slots", f"{cfg.unity_slots} is missing")
    version = gh.next_version()
    tag = tag_for(version)
    if gh.release_state(tag) == "draft" and state.open_draft == tag:
        if not no_publish:   # --no-publish makes no store write, not even this cleanup
            gh.delete_draft(tag)   # this job's own draft from an earlier run
            state.open_draft = None
    gh.ensure_tag_available(tag)
    run_dir = job / "runs" / f"{now.astimezone(TZ):%Y%m%d-%H%M%S}"
    manifest = (clone / "sim/Statecraft.Sim.Tests/golden/manifest.txt").read_text(encoding="utf-8")
    return Night(now, day, sha, version, tag, run_dir, time.monotonic() + cfg.deadline_hours * 3600, job, cfg,
                 manifest)


# --- gates, Unity and the full EditMode run ---------------------------------------------------------------------

WARM_UP_OK = "UnityMcpGuard warm-up: gate defines in place"


def _limit(night: Night, minutes: float) -> float:
    return min(night.deadline, time.monotonic() + minutes * 60)


def _step(night: Night, runner: Runner, name: str, cmd: list[str], minutes: float,
          env: dict[str, str] | None = None) -> tuple[int, str]:
    log = night.run_dir / f"{name}.log"
    code = runner.run(cmd, cwd=night.cfg.clone, log=log, deadline=_limit(night, minutes), env=env)
    return code, log.read_text(encoding="utf-8", errors="replace") if log.is_file() else ""


def run_gates(night: Night, runner: Runner) -> None:
    base_env = {**os.environ, "MSBUILDDISABLENODEREUSE": "1", "DOTNET_CLI_USE_MSBUILD_SERVER": "0",
                "UseSharedCompilation": "false"}

    def admitted():
        check_window(night)
        if time.monotonic() >= night.deadline:
            raise Refused("managed-deadline", "work deadline expired before a managed command could start")

    def step(name, cmd, minutes, env=None):
        admitted()
        return _step(night, runner, name, cmd, minutes, base_env if env is None else env)

    check_window(night)
    with unity_postmerge.machine_lock.hold():
        # A long acquisition must drain normally; expired work starts no child after it gets the lock.
        admitted()
        try:
            clone = night.cfg.clone
            code, _ = step("build-sim", ["uv", "run", str(clone / "tools/build_sim_for_unity.py")], 15)
            if code != 0:
                raise Refused("build-sim", "timed out" if code == 124 else f"build_sim_for_unity exited {code}")
            code, text = step("check", ["bash", "scripts/check.sh", "origin/main"], 45)
            why = nightly_checks.check_sh_verdict(text, code)
            if why:
                raise Refused("check", "timed out" if code == 124 else why)
            code, text = step("release-gate", ["bash", "scripts/oss/release_gate.sh"], 60)
            why = nightly_checks.release_gate_verdict(text, code)
            if why:
                raise Refused("release-gate", "timed out" if code == 124 else why)
            for locale in ("he_IL", "en_US"):
                lang = f"{locale}.UTF-8"
                env = {**base_env, "LANG": lang, "LC_ALL": lang}
                code, text = step(f"sim-{locale}",
                                   ["dotnet", "test", "sim/Statecraft.sln", "-c", "Release", "--nologo",
                                    "-nodeReuse:false", "-p:UseSharedCompilation=false"], 30, env)
                failed, total = nightly_checks.dotnet_totals(text)
                if code == 124:
                    raise Refused(f"sim-{locale}", "timed out")
                if code != 0 or failed or total == 0:
                    raise Refused(f"sim-{locale}", f"dotnet test exited {code}: {failed} failed of {total} tests")
        finally:
            # Preserve the primary refusal/interrupt; secondary cleanup failure must still be visible.
            pending = sys.exc_info()[1]
            shutdown_failure = None
            try:
                code = runner.run(["dotnet", "build-server", "shutdown"], cwd=night.cfg.clone,
                                  log=night.run_dir / "build-server-shutdown.log",
                                  deadline=time.monotonic() + 120, env=base_env)
                if code != 0:
                    shutdown_failure = f"compiler server shutdown exited {code}"
            except BaseException as error:
                if pending is None and not isinstance(error, (OSError, subprocess.SubprocessError)):
                    raise
                shutdown_failure = f"compiler server shutdown raised {type(error).__name__}: {error}"
            if shutdown_failure is not None:
                if pending is None:
                    raise Refused("build-server-shutdown", shutdown_failure)
                night.notes.append(shutdown_failure)
                try:
                    (night.run_dir / "build-server-shutdown.note").write_text(shutdown_failure + "\n", encoding="utf-8")
                except OSError:
                    print(shutdown_failure, file=sys.stderr)


def unity_argv(night: Night, unity_args: list[str], *, webgl: bool) -> list[str]:
    clone = night.cfg.clone
    editor = unity_postmerge.unity_path(_editor_version(clone))
    return ["python3", "-I", str(night.cfg.unity_slots), *(["--webgl"] if webgl else []), str(clone / "unity"), "--",
            "uv", "run", str(clone / "tools/machine_lock.py"), "--shared", "--",
            editor, "-batchmode", "-CI", "false", "-GITHUB_ACTIONS", "false", "-TF_BUILD", "false",
            "-projectPath", str(clone / "unity"), *unity_args]


def unity_env(night: Night) -> dict[str, str]:
    empty = night.job / "no-mcp-server"
    empty.parent.mkdir(parents=True, exist_ok=True)
    empty.touch()
    return {**os.environ, "UNITY_SLOTS": str(night.cfg.unity_slots_count), "UNITY_MCP_SERVER_PATH": str(empty)}


def check_window(night: Night) -> None:
    if night.cfg.exclusive_marker.exists():
        raise Refused("exclusive-window", f"{night.cfg.exclusive_marker} appeared before a Unity step")


# Unity 6000.3.25f1 flips m_DoubleSidedGI in this material during EditMode; left alone, assert_clean_tree refuses every night.
CHURNED_MATERIAL = "unity/Assets/Art/HexTiles/Props/arab_village_prickly_pear/prickly_pear-rock.mat"
RESTORE_ARGV = ["git", "checkout", "--", "unity/ProjectSettings", "unity/Assets/Settings", CHURNED_MATERIAL]


def restore_settings(night: Night, runner: Runner) -> None:
    """Unity rewrites project settings while it runs; put them back so the tree stays clean."""
    code = runner.run(list(RESTORE_ARGV),
                      cwd=night.cfg.clone, log=night.run_dir / "restore-settings.log",
                      deadline=time.monotonic() + 120)
    if code != 0:
        raise Refused("restore-settings", f"git checkout of the Unity settings exited {code}")


def _note_restore(night: Night, err: Refused) -> None:
    note = f"restore-settings failed while another refusal was pending: {err.detail}"
    print(note, file=sys.stderr)
    (night.run_dir / "restore-settings.note").write_text(note + "\n", encoding="utf-8")


def run_editmode(night: Night, runner: Runner) -> None:
    env = unity_env(night)
    xml = night.run_dir / "editmode.xml"
    restore_error: Refused | None = None
    pending = False
    check_window(night)   # before Unity has touched anything, so there is nothing to restore
    try:
        warm = unity_argv(night, ["-buildTarget", "OSXUniversal", "-executeMethod", unity_postmerge.WARM_UP,
                                  "-logFile", str(night.run_dir / "warmup.log")], webgl=False)
        warm_code = runner.run(warm, cwd=night.cfg.clone, log=night.run_dir / "warmup.out",
                               deadline=_limit(night, 60), env=env, holder=night.run_dir / "lock-holder")
        if warm_code == 124:
            raise Refused("editmode", "the Unity warm-up timed out")
        warm_log = night.run_dir / "warmup.log"
        warm_text = warm_log.read_text(encoding="utf-8", errors="replace") if warm_log.is_file() else ""
        if warm_code != 0 or WARM_UP_OK not in warm_text:
            raise Refused("editmode", f"the Unity warm-up failed or did not confirm (exit {warm_code})")
        check_window(night)
        argv = unity_argv(night, ["-buildTarget", "OSXUniversal", "-runTests", "-testPlatform", "EditMode",
                                  "-testResults", str(xml), "-logFile", str(night.run_dir / "editmode.log")],
                          webgl=False)
        code = runner.run(argv, cwd=night.cfg.clone, log=night.run_dir / "editmode.out",
                          deadline=_limit(night, 150), env=env, holder=night.run_dir / "lock-holder")
    except BaseException:
        pending = True
        raise
    finally:
        try:
            restore_settings(night, runner)
        except Refused as e:
            restore_error = e
        if restore_error is not None and pending:
            _note_restore(night, restore_error)   # never replace the error already on its way
            restore_error = None
    results = unity_postmerge.parse_results(xml)
    if not unity_postmerge.passed(results, code):
        why = ("timed out" if code == 124 else results.broken
               or f"exit {code}, {results.failed} failed of {results.total} tests")
        if restore_error is not None:
            _note_restore(night, restore_error)
        raise Refused("editmode", why)
    if restore_error is not None:
        raise restore_error


# --- the version stamp, the two web builds and packaging ---------------------------------------------------------

SETTINGS_ASSET = "unity/ProjectSettings/ProjectSettings.asset"
BUNDLE_VERSION_RE = re.compile(r"^(  bundleVersion: )\S+$", re.M)
BUILD_WEB = ["-quit", "-buildTarget", "WebGL", "-executeMethod", "Statecraft.Game.EditorTools.Build.Web"]


def stamp_bundle_version(settings: Path, version: str) -> None:
    """Put tonight's version in the settings file; the stamp is never committed, restore_settings removes it."""
    text = settings.read_text(encoding="utf-8")
    new, n = BUNDLE_VERSION_RE.subn(lambda m: m.group(1) + version, text)
    if n != 1:
        raise ValueError(f"expected exactly one bundleVersion line in {settings}, found {n}")
    settings.write_text(new, encoding="utf-8")


BUILD_FAILURE_RE = re.compile(r"error CS\d+|Build completed with a result of 'Failed'|BuildFailedException|"
                              r"\bException\b|Fatal Error|Aborting batchmode", re.I)


def _build_failure_detail(log: Path) -> str:
    """The last few error lines of Unity's own log, so the record says why a build failed."""
    try:
        text = log.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return " (no Unity log was written)"
    lines = [ln.strip()[:160] for ln in text.splitlines() if BUILD_FAILURE_RE.search(ln)]
    return f": {' | '.join(lines[-3:])}" if lines else " (no error line in the Unity log)"


def build_web(night: Night, runner: Runner, *, pages: bool, free_gb: Callable[[Path], float]) -> Path:
    """One WebGL build as in RELEASING step 5: gzip for GitHub Releases and itch, or the Pages variant."""
    clone = night.cfg.clone
    free = free_gb(clone)
    if free < night.cfg.min_free_gb:
        raise Refused("disk", f"{free:.1f} GB free, {night.cfg.min_free_gb} GB needed before a WebGL build")
    check_window(night)
    name = "Web-pages" if pages else "Web-gzip"
    build = clone / "unity/Builds" / name
    shutil.rmtree(build, ignore_errors=True)   # a page left by an earlier run must not pass the stamp check
    log = night.run_dir / f"unity-build-web-{'pages' if pages else 'gz'}.log"
    args = [*BUILD_WEB, "-compression", "gzip"]
    if pages:
        args += ["-decompressionFallback", "-shareable", "-out", f"Builds/{name}"]
    else:
        args += ["-shareable"]
    args += ["-logFile", str(log.resolve())]
    env = unity_env(night)
    restore_error: Refused | None = None
    pending = False
    code = 0
    try:
        try:
            stamp_bundle_version(clone / SETTINGS_ASSET, night.version)
        except ValueError as e:
            raise Refused("version-stamp", str(e)) from e
        code = runner.run(unity_argv(night, args, webgl=True), cwd=clone,
                          log=night.run_dir / f"unity-build-web-{'pages' if pages else 'gz'}.out",
                          deadline=_limit(night, 120), env=env, holder=night.run_dir / "lock-holder")
    except BaseException:
        pending = True
        raise
    finally:
        try:
            restore_settings(night, runner)
        except Refused as e:
            restore_error = e
        if restore_error is not None and pending:
            _note_restore(night, restore_error)   # never replace the error already on its way
            restore_error = None
    if code != 0:
        if restore_error is not None:
            _note_restore(night, restore_error)
        if code == 124:
            raise Refused("build-web", "the Unity build timed out")
        raise Refused("build-web", f"the Unity build exited {code}{_build_failure_detail(log)}")
    if restore_error is not None:
        raise restore_error
    index = build / "index.html"
    page = index.read_text(encoding="utf-8", errors="replace") if index.is_file() else ""
    built = nightly_checks.product_version(page)
    if built != night.version:
        raise Refused("version-stamp", f"{name}/index.html says {built!r}, tonight's version is {night.version!r}")
    return build


def package_zip(night: Night, runner: Runner, build: Path, pages: bool) -> Path:
    zip_path = night.run_dir / f"statecraft-israel-v{night.version}-web{'-pages' if pages else ''}.zip"
    code, text = _step(night, runner, f"package-{'pages' if pages else 'gz'}",
                       ["bash", "scripts/oss/package_web.sh", str(build), str(zip_path)], 20)
    if code != 0:
        raise Refused("package", "timed out" if code == 124 else f"package_web.sh exited {code}")
    if "PACKAGED " not in text or "OK: notices inside" not in text:
        raise Refused("package", "package_web.sh did not confirm the zip and its notices")
    return zip_path


def assert_clean_tree(night: Night, runner: Runner) -> None:
    dirty = _git(runner, night.cfg.clone, "status", "--porcelain", "--untracked-files=no")
    if dirty:
        raise Refused("dirty-tree", f"the clone has tracked changes after the builds: {dirty[:200]}")


# --- the no-regression checks: web_check passes, download size, headless boot -------------------------------------

def run_web_checks(night: Night, runner: Runner, build: Path) -> None:
    """Every RELEASING step 5 pass on one build, under the shared machine lock; each must exit 0 and print PASS:."""
    clone = night.cfg.clone
    try:
        passes = nightly_checks.web_check_passes(build, night.manifest)
    except ValueError as e:
        raise Refused("web-check", str(e)) from e
    for i, args in enumerate(passes, 1):
        argv = ["uv", "run", str(clone / "tools/machine_lock.py"), "--shared", "--",
                "uv", "run", str(clone / "tools/web_check.py"), *args]
        code, text = _step(night, runner, f"web-check-{build.name}-{i}", argv, 10)
        if code == 124:
            raise Refused("web-check", f"{build.name} pass {i} timed out")
        if code != 0 or not re.search(r"^PASS:", text, re.M):
            raise Refused("web-check", f"{build.name} pass {i} exited {code} without a PASS line")


def check_sizes(night: Night, gh: GitHub, build: Path, previous_zip: Path, prev_sha: str | None, *,
                runner: Runner) -> dict[str, int]:
    """Refuse when the download grew more than 2% over the previous published build, unless the PRs merged since
    then record a size budget. Returns the new sizes, which become the next night's baseline."""
    try:
        if not Path(previous_zip).is_file():
            raise Refused("sizes", "no baseline: the previous published web zip is missing")
        old = nightly_checks.web_sizes_from_zip(previous_zip)
        new = nightly_checks.web_sizes(build)
    except (ValueError, OSError, zipfile.BadZipFile) as e:
        raise Refused("sizes", f"cannot measure the download size: {e}") from e
    if nightly_checks.size_verdict(old, new, 0) is None:
        return new
    if prev_sha is None:
        raise Refused("sizes", "the download grew and the previous published commit is unknown, so no budget can be read")
    shas = set(_git(runner, night.cfg.clone, "rev-list", f"{prev_sha}..{night.sha}").split())
    budget = nightly_checks.size_budget_bytes(gh.merged_pr_bodies(shas))
    why = nightly_checks.size_verdict(old, new, budget)
    if why:
        raise Refused("sizes", why)
    return new


def measure_boot(night: Night, runner: Runner, build: Path) -> float:
    """The median of three headless boots, each alone on the machine (no other Unity, no other check)."""
    clone = night.cfg.clone
    runs: list[int] = []
    for i in (1, 2, 3):
        argv = ["uv", "run", str(clone / "tools/machine_lock.py"), "--exclusive", "--wait-unity", "--timeout", "2700",
                "--", "uv", "run", str(clone / "tools/web_check.py"), str(build), "--query", "bootlog",
                "--expect", "GAME OK", "--timeout", "300", "--stall", "90"]
        log = night.run_dir / f"boot-{i}.log"
        code = runner.run(argv, cwd=night.cfg.main_repo, log=log, deadline=_limit(night, 60))
        text = log.read_text(encoding="utf-8", errors="replace") if log.is_file() else ""
        if code == 124:
            raise Refused("boot", f"boot run {i} timed out")
        if code == 3:
            raise Refused("boot", f"boot run {i}: another Unity was still running after 45 minutes")
        if code != 0:
            raise Refused("boot", f"boot run {i} exited {code}")
        try:
            runs.append(nightly_checks.boot_ms(text))
        except ValueError as e:
            raise Refused("boot", f"boot run {i}: {e}") from e
    return float(statistics.median(runs))


# --- publishing -------------------------------------------------------------------------------------------------

def previous_build(night: Night, state: State, gh: GitHub, runner: Runner) -> tuple[str, str | None]:
    """The tag of the build tonight is compared with, and its commit when known (None blocks a size budget lookup)."""
    last = state.last_published
    tag = gh.previous_build_tag(last.tag if last else None)
    if last is not None:
        return tag, last.sha
    # the first night: the release's commit, so the clone needs its tag; a failed fetch refuses, it is not guessed
    _git(runner, night.cfg.clone, "fetch", "--no-tags", "origin", f"refs/tags/{tag}:refs/tags/{tag}")
    return tag, _git(runner, night.cfg.clone, "rev-list", "-n", "1", f"refs/tags/{tag}") or None


def _drop_draft(night: Night, state: State, gh: GitHub) -> None:
    """Delete tonight's draft if it is still one. A failure here never replaces the refusal that caused it."""
    if state.open_draft != night.tag:
        return
    try:
        present = gh.release_state(night.tag)
        if present == "draft":
            gh.delete_draft(night.tag)
        if present != "published":
            state.open_draft = None
    except Exception as e:
        print(f"could not delete the draft {night.tag}: {e}", file=sys.stderr)   # open_draft stays, so a rerun cleans it


ITCH_WAIT_SECONDS = 15 * 60


def _detail(e: BaseException) -> str:
    return e.detail if isinstance(e, Stop) else f"{type(e).__name__}: {e}"


def _itch_rollback(night: Night, itch, folder: Path, version: str) -> str:
    """Put the previous build back on itch and wait until it is live. Returns "" on success, otherwise a sentence
    for the refusal detail: a failed rollback leaves the public itch page possibly broken, so it is also notified."""
    try:
        itch.push(folder, version)
        itch.wait_live(version, time.monotonic() + ITCH_WAIT_SECONDS)
        return ""
    except Exception as e:
        note = (f"; the itch rollback to {version} failed ({_detail(e)}), so the public itch page may be broken: "
                "check it by hand")
        print(f"itch rollback failed: {_detail(e)}", file=sys.stderr)
        notify("Nightly itch rollback failed", f"{night.tag}: the itch rollback failed, the public itch page may be broken"[:200])
        return note


def _restore_latest(night: Night, gh: GitHub, previous: str | None) -> str:
    target = f"previous Latest {previous}" if previous is not None else "no previous Latest"
    try:
        acted, current = gh.restore_latest(previous, night.tag)
        if acted:
            note = (f"restored {target}" if previous is not None else
                    "no previous Latest; removed the failed release's Latest mark")
        elif current is None:
            note = f"there is no Latest now, so {night.tag} is not Latest: left alone"
        elif current == previous:
            note = f"{target} still stands; nothing to restore"
        else:
            note = f"Latest is now {current}, not {night.tag}: left alone"
    except Exception as e:
        note = f"Latest recovery failed ({target}): {_detail(e)}; check by hand"
    print(note, file=sys.stderr)
    try:
        (night.run_dir / "latest-rollback.log").write_text(note + "\n", encoding="utf-8")
    except OSError as e:
        print(f"could not write Latest recovery log: {e}", file=sys.stderr)
    return "; " + note


def publish_night(night: Night, state: State, gh: GitHub, itch, zips: tuple[Path, Path], sizes: dict[str, int],
                  boot: float, *, previous: tuple[str, str | None], save: Callable[[], None],
                  runner: Runner) -> None:
    """Draft first, so nothing public changes until the draft's bytes match: draft, verify, publish, then the site.
    `save` persists the state; it runs right after the draft exists and right after the release is public, so a
    kill leaves a record of both."""
    prev_tag, prev_sha = previous
    use_itch = night.cfg.itch_enabled
    if use_itch and itch is None:   # before any store change: enabled means the push is part of this night
        raise Refused("itch", "itch is enabled but there is no itch client")
    since = prev_sha or prev_tag
    subjects = [ln for ln in _git(runner, night.cfg.clone, "log", "--format=%s", f"{since}..{night.sha}").splitlines()
                if ln]
    notes = night.run_dir / "release-notes.md"
    notes.write_text(release_notes(version=night.version, sha=night.sha, since=since, subjects=subjects,
                                   sha256_lines=sha256_lines(list(zips))), encoding="utf-8")
    gh.ensure_tag_available(night.tag)   # again just before create: a release made since preflight is never ours
    state.open_draft = night.tag   # set before create: a draft a failed upload leaves behind is then cleaned
    save()
    try:
        gh.create_draft(night.tag, night.sha, notes, list(zips))
        gh.verify_draft(night.tag, list(zips), night.run_dir / "draft-download")
    except Exception as e:
        _drop_draft(night, state, gh)
        raise Refused("github-draft", e.detail if isinstance(e, Stop) else f"{type(e).__name__}: {e}") from e
    except BaseException:
        _drop_draft(night, state, gh)
        raise
    rollback_folder: Path | None = None
    rollback_version = prev_tag[1:] if prev_tag.startswith("v") else prev_tag
    if use_itch:
        try:   # the way back is ready before anything is pushed
            previous_zip = gh.download(prev_tag, "*-web.zip", night.run_dir / "itch-rollback-zip")
            rollback_folder = unzip_to(previous_zip, night.run_dir / "itch-rollback")
        except Exception as e:
            _drop_draft(night, state, gh)
            raise Refused("itch-rollback", f"the previous build {prev_tag} cannot be prepared: {_detail(e)}") from e
        except BaseException:
            _drop_draft(night, state, gh)
            raise
        pushed = False
        try:
            folder = unzip_to(zips[0], night.run_dir / "itch-push")
            pushed = True   # from here a push may have reached itch, even if it then raises
            itch.push(folder, night.version)
            itch.wait_live(night.version, time.monotonic() + ITCH_WAIT_SECONDS)
            itch.matches(folder, night.run_dir / "itch-fetch")
        except Exception as e:
            detail = _detail(e)
            if pushed:
                detail += _itch_rollback(night, itch, rollback_folder, rollback_version)
            _drop_draft(night, state, gh)
            raise Refused("itch", detail) from e
        except BaseException:
            if pushed:
                _itch_rollback(night, itch, rollback_folder, rollback_version)
            _drop_draft(night, state, gh)
            raise
    previous_latest = None
    publish_started = verified = False
    try:
        previous_latest = gh.latest_tag()
        (night.run_dir / "latest-before-publish.txt").write_text(
            previous_latest + "\n" if previous_latest is not None else "no previous Latest\n", encoding="utf-8")
        publish_started = True   # an edit can reach GitHub even if the command then fails
        gh.publish(night.tag, night.sha)
        verified = True
        state.open_draft = None
        state.last_published = Published(night.tag, night.sha, night.version, sizes, boot)
        if len(state.boot_baseline) < BASELINE_NIGHTS:
            state.boot_baseline.append(boot)
        state.site_pending = night.version   # until the demo serves this version, a later night retries the deploy
        save()
        if not use_itch:
            skip = "itch: skipped (save test not passed)"
            print(skip)
            night.notes.append(skip)
        failure = "no attempt"
        for _ in range(2):   # the release is public already, so retry the site once before restoring Latest
            try:
                gh.dispatch_site(time.monotonic() + SITE_WAIT_SECONDS)
                demo_serves(night.cfg.site_demo_url, night.version, deadline=time.monotonic() + SITE_WAIT_SECONDS)
                state.site_pending = None
                return
            except Exception as e:
                failure = _detail(e)
        raise Refused("site", f"{night.tag} is public and the demo is unchanged: {failure}")
    except BaseException as e:
        detail = _detail(e)
        if publish_started:
            detail += _restore_latest(night, gh, previous_latest)
        if not verified:
            if rollback_folder is not None:   # itch must not keep a build the release does not have
                detail += _itch_rollback(night, itch, rollback_folder, rollback_version)
            try:
                now_state = gh.release_state(night.tag)
            except Exception:
                now_state = "unknown"
            if now_state == "draft":
                _drop_draft(night, state, gh)
            elif now_state in ("published", "unknown"):
                detail += f" (the release may be public: check {night.tag} by hand)"
        if isinstance(e, Exception):
            raise Refused(e.gate if isinstance(e, Stop) else "github-publish", detail) from e
        night.notes.append(detail)   # run_night records recovery even when it re-raises an interrupt
        raise


# --- the night --------------------------------------------------------------------------------------------------

def _build_both(night: Night, runner: Runner, free_gb: Callable[[Path], float]) -> tuple[Path, Path]:
    """The gzip and Pages builds. The tree is checked after them, and also after a failed one, so the record says
    whether the settings came back."""
    try:
        gz = build_web(night, runner, pages=False, free_gb=free_gb)
        pages = build_web(night, runner, pages=True, free_gb=free_gb)
    except BaseException:
        try:
            assert_clean_tree(night, runner)
        except Exception as e:   # never replace the build failure on its way
            note = f"the tree was not clean after a failed build: {e}"
            print(note, file=sys.stderr)
            (night.run_dir / "dirty-tree.note").write_text(note + "\n", encoding="utf-8")
        raise
    assert_clean_tree(night, runner)
    return gz, pages


def run_stages(night: Night, state: State, runner: Runner, gh: GitHub, itch, *, no_publish: bool,
               free_gb: Callable[[Path], float] = lambda p: shutil.disk_usage(p).free / 1e9,
               save: Callable[[], None] = lambda: None) -> bool:
    """Every gate, then (unless no_publish) the publish. True when published, False when stopped before any store."""
    run_gates(night, runner)
    run_editmode(night, runner)
    gz, pages = _build_both(night, runner, free_gb)
    zips = (package_zip(night, runner, gz, False), package_zip(night, runner, pages, True))
    run_web_checks(night, runner, gz)
    run_web_checks(night, runner, pages)
    previous = previous_build(night, state, gh, runner)   # once a night, shared with the notes
    prev_tag, prev_sha = previous
    previous_zip = gh.download(prev_tag, "*-web.zip", night.run_dir / "previous")
    sizes = check_sizes(night, gh, gz, previous_zip, prev_sha, runner=runner)
    boot = measure_boot(night, runner, gz)
    phase, why = nightly_checks.boot_verdict(boot, state.boot_baseline, BASELINE_NIGHTS)
    if why:
        raise Refused("boot", why)
    print(f"boot: {boot:.0f} ms ({phase}, {len(state.boot_baseline)} baseline nights)")
    if no_publish:
        print(f"NO-PUBLISH: all gates passed for {night.tag}")
        return False
    publish_night(night, state, gh, itch, zips, sizes, boot, previous=previous, save=save, runner=runner)
    return True


def _try_save(state_path: Path, state: State) -> None:
    try:
        save_state(state_path, state)
    except OSError as e:   # never costs the night its record, and the final save tries again
        print(f"could not save state: {e}", file=sys.stderr)
        notify("Nightly state not saved", f"{e}"[:200])   # a published release with unsaved state must not be silent


def run_night(cfg: Config, state_path: Path, job: Path, runner: Runner, gh: GitHub | None, itch, *, now: datetime,
              manual: bool, no_publish: bool, client_error: Refused | None = None,
              free_gb: Callable[[Path], float] = lambda p: shutil.disk_usage(p).free / 1e9) -> str:
    outcome, gate, detail = "REFUSED", "internal", ""
    state: State | None = None
    night: Night | None = None
    day = night_of(now)
    try:
        try:
            state = load_state(state_path)
            if not no_publish:
                for gap in missed_days(state.last_run_day, day):
                    stamp = datetime(gap.year, gap.month, gap.day, tzinfo=TZ)
                    record(job / "LOG.md", cfg.handoff,
                           log_line(stamp, "MISSED", tag=None, sha=None, gate="-", log=None))
            if client_error is not None or gh is None:   # the record names the step, not "internal"
                raise client_error or Refused("github-client", "no GitHub client")
            night = preflight(cfg, state, runner, gh, job=job, now=now, manual=manual, free_gb=free_gb,
                              no_publish=no_publish)
            published = run_stages(night, state, runner, gh, itch, no_publish=no_publish, free_gb=free_gb,
                                   save=lambda: _try_save(state_path, state))
            outcome, gate = ("PUBLISHED" if published else "NO-PUBLISH"), "-"
        except Stop as e:
            outcome, gate, detail = e.outcome, e.gate, e.detail
        except STAGE_ERRORS as e:
            stage = "preflight" if night is None else "run"
            outcome, gate, detail = "REFUSED", stage, str(e)
        except KeyboardInterrupt:
            outcome, gate, detail = "REFUSED", "interrupted", "interrupted"
            raise
        except Exception as e:   # fail closed with a record, never a silent crash
            outcome, gate, detail = "REFUSED", "internal", f"{type(e).__name__}: {e}"
    finally:
        if not no_publish:
            if night is not None and night.notes:
                detail = " ".join(filter(None, [detail, *night.notes]))
            line = log_line(now, outcome, tag=night.tag if night else None, sha=night.sha if night else None,
                            gate=gate, log=night.run_dir if night else None)
            record(job / "LOG.md", cfg.handoff, f"{line} {detail}".rstrip() if detail else line)
            if state is not None:   # after the record, so a failed save never costs the night its line
                state.last_run_day = day.isoformat()
                _try_save(state_path, state)
            if outcome == "REFUSED":
                notify("Nightly refused", f"{gate}: {detail}"[:200])
    return outcome


# --- the itch.io save test --------------------------------------------------------------------------------------------

PERSIST_SAVED = re.compile(r"^PERSISTENCE saved (\d{4}-\d{2}-\d{2}) turn (\d+) hash ([0-9a-f]+)$", re.M)
# The page names a new upload's frame only after itch has unpacked it; until then it carries just a JSON load_url
# ({"status":"extracting"}). Unpacking the web build took about 17 minutes on 28 October 2026, longer than the old
# 10-minute wait, so the save test now waits up to 40 minutes.
# While it waited for the second upload that day, itch answered 429 Too Many Requests; a 429 now slows the polling
# down (Retry-After, bounded) instead of ending the test, within the same 40 minutes.
SAVETEST_FRAME_TRIES = 80
SAVETEST_FRAME_SLEEP = 30
SAVETEST_RATE_LIMIT_SLEEP = 120   # after an HTTP 429 without a usable Retry-After
SAVETEST_RATE_LIMIT_MAX = 300   # a longer Retry-After is capped; the total wait stays within the frame budget
SAVETEST_RUN_MINUTES = 10
TOOLS = Path(__file__).resolve().parent


def _page_text(fetch, url: str) -> str:
    try:
        with fetch(url, timeout=30) as response:
            return response.read().decode("utf-8", "replace")
    except (OSError, ValueError) as e:   # URLError and HTTPException subclass OSError or ValueError
        raise Refused("savetest", f"cannot read {url}: {type(e).__name__}: {e}") from e


def _rate_limit_pause(e: Refused) -> int | None:
    """Seconds to wait after itch answered HTTP 429 (its Retry-After, kept within bounds), or None for any other error."""
    cause = e.__cause__
    if not isinstance(cause, urllib.error.HTTPError) or cause.code != 429:
        return None
    try:
        asked = int((cause.headers or {}).get("Retry-After", ""))
    except ValueError:
        asked = SAVETEST_RATE_LIMIT_SLEEP
    return min(max(asked, SAVETEST_FRAME_SLEEP), SAVETEST_RATE_LIMIT_MAX)


def _frame_url(fetch, sleep, page_url: str, other: str | None = None) -> str | None:
    """The game-frame URL the itch page embeds. With `other` it waits for a frame that is not that one; the last
    frame seen (or None) comes back when the wait budget runs out. A 429 from itch only slows the polling down; if
    itch still refuses when the budget runs out, that refusal is raised."""
    found, limited = None, None
    waited, budget = 0, SAVETEST_FRAME_TRIES * SAVETEST_FRAME_SLEEP
    while True:
        try:
            found, limited = itch_frame_url(_page_text(fetch, page_url)), None
            pause = SAVETEST_FRAME_SLEEP
        except Refused as e:
            pause = _rate_limit_pause(e)
            if pause is None:
                raise
            limited = e
            print(f"savetest: itch answered 429, waiting {pause} s", file=sys.stderr)
        else:
            if found is not None and found != other:
                return found
        if waited + pause > budget:
            if limited is not None:
                raise limited
            return found
        sleep(pause)
        waited += pause


def _frame_parts(url: str) -> tuple[str, str, str]:
    """(scheme, host, upload segment): the segment of /html/<upload>-<build>/index.html names the upload."""
    parts = urllib.parse.urlsplit(url)
    segments = parts.path.split("/")
    return parts.scheme, parts.netloc, segments[2] if len(segments) > 2 else ""


def _frame_check(runner: Runner, job_dir: Path, name: str, url: str, profile: Path, *extra: str) -> tuple[int, str]:
    """web_check on one itch game-frame URL, in the savetest's one kept Chrome profile, under the shared machine lock."""
    argv = ["uv", "run", str(TOOLS / "machine_lock.py"), "--shared", "--", "uv", "run", str(TOOLS / "web_check.py"),
            "--url", url, "--profile", str(profile), "--timeout", "420", *extra]
    log = job_dir / f"{name}.log"
    code = runner.run(argv, cwd=TOOLS, log=log, deadline=time.monotonic() + SAVETEST_RUN_MINUTES * 60)
    return code, log.read_text(encoding="utf-8", errors="replace") if log.is_file() else ""


def savetest(cfg: Config, config_path: Path, job: Path, runner: Runner, gh: GitHub, itch: Itch, *, tag: str,
             fetch=urllib.request.urlopen, sleep: Callable[[float], None] = time.sleep) -> str:
    """The two-upload save test of docs/m3-release.md: upload A, play a turn and let the autosave commit; upload B
    (the same content, a new upload URL on the same frame origin); B must load that date, turn and state hash twice.
    "PASS" turns the itch push on (enable_itch); "INCONCLUSIVE" (the test could not be run or proves nothing) and
    "FAIL" leave it off. It pushes twice to the separate test page in savetest_itch_target, never to the public page:
    each upload hides the game while itch unpacks it (up to an hour on 28 October 2026). Only the owner starts it."""
    stamp = datetime.now(TZ).strftime("%Y%m%d-%H%M%S")
    work = Path(job) / "savetest" / stamp
    evidence: dict = {"tag": tag, "page_url": cfg.savetest_itch_page_url, "target": cfg.savetest_itch_target}
    result, detail = "INCONCLUSIVE", ""
    try:
        result, detail = _savetest(cfg, work, runner, gh, itch, tag, evidence, fetch, sleep)
    except Stop as e:
        detail = f"{e.gate}: {e.detail}"
    except (OSError, SystemExit) as e:   # find_chrome exits when there is no Chrome
        detail = f"{type(e).__name__}: {e}"
    evidence.update(result=result, detail=detail)
    if work.is_dir():
        (work / "evidence.json").write_text(json.dumps(evidence, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if result == "PASS":
        enable_itch(config_path, work / "evidence.json")
    record(Path(job) / "LOG.md", cfg.handoff, f"SAVETEST {result} tag={tag} {detail}".rstrip())
    return result


def _itch_project(target: str) -> str:
    """`user/project` of a butler target, without its channel: every channel of a project uploads to its one page."""
    return target.partition(":")[0].lower()


def _page_key(url: str) -> tuple[str, str]:
    parts = urllib.parse.urlsplit(url.strip())
    return parts.netloc.lower(), parts.path.rstrip("/").lower()


def _test_page_problem(cfg: Config) -> str | None:
    """Why the save test may not push, or None: it needs its own test project, distinct from the public one."""
    if not cfg.savetest_itch_target or not cfg.savetest_itch_page_url:
        return "savetest_itch_target and savetest_itch_page_url must name a separate test page in the job's config"
    if _itch_project(cfg.savetest_itch_target) == _itch_project(cfg.itch_target):
        return "the save test's test page must not be the public itch project (any channel of it uploads to its page)"
    try:
        test_page = _page_key(cfg.savetest_itch_page_url)
        public_page = _page_key(cfg.itch_page_url) if cfg.itch_page_url else None
    except ValueError as e:
        return f"the save test's test page URL or the public page URL is not usable: {e}"
    if test_page == public_page:
        return "the save test's test page must not be the public itch page"
    return None


def _savetest(cfg: Config, work: Path, runner: Runner, gh: GitHub, itch: Itch, tag: str, evidence: dict, fetch,
              sleep) -> tuple[str, str]:
    """(result, detail) of the test; a Refused or OSError out of it is the caller's INCONCLUSIVE."""
    reason = _test_page_problem(cfg)
    if reason:
        return "INCONCLUSIVE", reason
    state = gh.release_state(tag)
    if state != "published":
        return "INCONCLUSIVE", f"{tag} is {state or 'not a release'}, the save test needs a published nightly"
    evidence["chrome"] = str(find_chrome())
    work.mkdir(parents=True, exist_ok=True)
    version = tag[1:] if tag.startswith("v") else tag
    zip_path = gh.download(tag, "*-web.zip", work / "zip")
    evidence.update(zip=zip_path.name, zip_sha256=sha256_of(zip_path))
    folder = unzip_to(zip_path, work / "folder")
    profile = work / "profile"

    itch.push(folder, f"{version}-savetest-a")
    itch.wait_live(f"{version}-savetest-a", time.monotonic() + ITCH_WAIT_SECONDS)
    frame_a = _frame_url(fetch, sleep, cfg.savetest_itch_page_url)
    evidence.update(upload_a={"build_id": itch.live_build_id(), "frame_url": frame_a})
    if frame_a is None:
        return "INCONCLUSIVE", "the itch page names no game frame after upload A"
    code, text = _frame_check(runner, work, "a-first-save", frame_a, profile, "--first-save")
    saved = PERSIST_SAVED.search(text)
    if code != 0 or not re.search(r"^PASS:", text, re.M) or saved is None:
        return "INCONCLUSIVE", f"upload A did not reach a durable first save (web_check exited {code})"
    tuple_a = {"date": saved.group(1), "turn": int(saved.group(2)), "hash": saved.group(3)}
    evidence["tuple"] = tuple_a

    itch.push(folder, f"{version}-savetest-b")
    itch.wait_live(f"{version}-savetest-b", time.monotonic() + ITCH_WAIT_SECONDS)
    frame_b = _frame_url(fetch, sleep, cfg.savetest_itch_page_url, other=frame_a)
    evidence.update(upload_b={"build_id": itch.live_build_id(), "frame_url": frame_b})
    if frame_b is None:
        return "INCONCLUSIVE", "the itch page names no game frame after upload B"
    (scheme_a, host_a, seg_a), (scheme_b, host_b, seg_b) = _frame_parts(frame_a), _frame_parts(frame_b)
    if (scheme_a, host_a) != (scheme_b, host_b):
        return "INCONCLUSIVE", f"the frame origin changed ({scheme_a}://{host_a} to {scheme_b}://{host_b}): IndexedDB is per origin"
    if seg_a == seg_b:
        return "INCONCLUSIVE", f"upload B has the same frame URL as upload A ({seg_a}), so the test proves nothing"

    expect = f"GAME OK {tuple_a['date']} turn {tuple_a['turn']} hash {tuple_a['hash']}"
    for n in (1, 2):
        code, text = _frame_check(runner, work, f"b-load-{n}", frame_b, profile, "--expect", expect)
        if code == 124:
            return "INCONCLUSIVE", f"load {n} of upload B timed out"
        if code != 0 or not re.search(r"^PASS:", text, re.M):
            return "FAIL", f"load {n} of upload B did not show '{expect}' (web_check exited {code})"
    return "PASS", f"upload B shows '{expect}' on two loads"


# --- command line -----------------------------------------------------------------------------------------------

def _clients(cfg: Config):
    """(GitHub, itch). itch is an Itch client only when the config enables it, else None and the night skips itch."""
    try:
        if shutil.which("gh") is None:
            raise Refused("github-client", "gh is not on PATH")
        runner = ProcRunner()
        itch = None
        if cfg.itch_enabled:
            if not Path(cfg.butler).is_file():
                raise Refused("itch-client", f"itch is enabled but {cfg.butler} is missing")
            itch = Itch(runner, cfg.butler, cfg.itch_target)
        return GitHub(runner, cfg.repo_slug, cfg.clone), itch
    except Refused:
        raise
    except Exception as e:
        raise Refused("github-client", f"{type(e).__name__}: {e}") from e


def run_savetest(cfg: Config, job: Path, tag: str) -> int:
    """The savetest command: real clients (butler is needed whether or not itch is enabled yet); 0 only on PASS."""
    try:
        if shutil.which("gh") is None:
            raise Refused("github-client", "gh is not on PATH")
        if not Path(cfg.butler).is_file():
            raise Refused("itch-client", f"{cfg.butler} is missing")
        problem = _test_page_problem(cfg)
        if problem:
            raise Refused("savetest", problem)
        runner = ProcRunner()
        gh, itch = GitHub(runner, cfg.repo_slug, cfg.clone), Itch(runner, cfg.butler, cfg.savetest_itch_target)
    except Refused as e:
        print(e, file=sys.stderr)
        return 1
    result = savetest(cfg, job / "config.json", job, runner, gh, itch, tag=tag)
    print(result)
    return 0 if result == "PASS" else 1


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="nightly.py")
    sub = parser.add_subparsers(dest="command", required=True)
    run = sub.add_parser("run")
    run.add_argument("--manual", action="store_true")
    run.add_argument("--no-publish", action="store_true")
    test = sub.add_parser("savetest", help="the two-upload itch.io save test: pushes twice to the separate test page, never the public one")
    test.add_argument("--tag", required=True, help="a published nightly release tag")
    args = parser.parse_args(argv)
    job = JOB
    now = datetime.now(TZ)
    try:
        cfg = load_config(job / "config.json")
    except Refused as e:   # a corrupt config is a recorded REFUSED night, not a first night on defaults
        if not getattr(args, "no_publish", False):   # the savetest command has no such flag
            line = log_line(now, "REFUSED", tag=None, sha=None, gate=e.gate, log=None)
            record(job / "LOG.md", Config().handoff, f"{line} {e.detail}")
            notify("Nightly refused", str(e)[:200])
        print(e, file=sys.stderr)
        return 1
    with run_lock(job) as held:
        if not held:
            print("another nightly run holds run.lock", file=sys.stderr)
            return 1
        if args.command == "savetest":
            return run_savetest(cfg, job, args.tag)
        client_error = None
        try:
            gh, itch = _clients(cfg)
        except Refused as e:   # run_night records the unusable client as a REFUSED night
            gh = itch = None
            client_error = e
        outcome = run_night(cfg, job / "state.json", job, ProcRunner(), gh, itch, now=now, manual=args.manual,
                            no_publish=args.no_publish, client_error=client_error)
    print(outcome)
    return 1 if outcome == "REFUSED" else 0


if __name__ == "__main__":
    sys.exit(main())
