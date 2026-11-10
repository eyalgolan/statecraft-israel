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
from nightly_publish import (GitHub, Itch, demo_serves, itch_build_frame_url, release_notes, sha256_lines, sha256_of,
                             unzip_to)
from nightly_state import (JOB, TZ, Config, LatestRecovery, Missed, Published, Refused, Runner, Skipped, State, Stop,
                           check_version_form, enable_itch, late_start, load_config, load_state, log_line,
                           missed_days, night_of, notify, record, run_lock, save_state, savetest_lock, tag_for,
                           write_refusal_log)
from web_check import find_chrome

HOLDER_ENV = unity_postmerge.machine_lock.HOLDER_ENV
STAGE_ERRORS = (FileNotFoundError, OSError, subprocess.SubprocessError)


# --- the runner -------------------------------------------------------------------------------------------------

class ProcRunner(Runner):
    """Runs the job's real commands. `grace` is the pause between SIGTERM and SIGKILL at the deadline."""

    def __init__(self, grace: float = 60):
        self.grace = grace

    def run(self, cmd: list[str], *, cwd: Path, log: Path, deadline: float,
            env: dict[str, str] | None = None, holder: Path | None = None,
            stop_on_interrupt: bool = False) -> int:
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
            except BaseException as primary:
                # Managed phases already hold the lock. Default callers may still be waiting for one.
                if stop_on_interrupt:
                    try:
                        self._stop_group(proc)
                    except BaseException as cleanup:
                        primary.add_note(f"managed child cleanup raised {type(cleanup).__name__}: {cleanup}")
                raise
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
            try:
                os.killpg(proc.pid, signal.SIGTERM)
                proc.wait(timeout=self.grace)
            except (ProcessLookupError, subprocess.TimeoutExpired):
                pass
        finally:   # a second interrupt during the grace period must not skip the final kill and reap
            try:
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
              free_gb: Callable[[Path], float], no_publish: bool = False, version: str | None = None) -> Night:
    if version is not None and not manual:   # main refuses this too; the 03:00 job never passes a version
        raise Refused("version", "a version given by hand needs a --manual run")
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
                latest = gh.latest_tag()
                if latest is None:
                    raise Refused("site", "no Latest target; pending demo verification remains open")
                # The workflow follows Latest, including after recovery; never wait for a rolled-back version.
                state.site_pending = latest[1:] if latest.startswith("v") else latest
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
    if version is None:
        version = gh.next_version()
    else:   # checked against the same release list: above every published vX.Y.Z
        version = gh.next_version(version)
        if state.open_draft is not None and state.open_draft != tag_for(version):
            raise Refused("version", f"the job's own draft {state.open_draft} from an earlier night is still open; "
                                     f"remove it first, before choosing {version} (tools/README.md, "
                                     f"\"Nightly release\")")
    tag = tag_for(version)
    if gh.release_state(tag) == "draft" and state.open_draft == tag:
        if not no_publish:   # --no-publish makes no store write, not even this cleanup
            gh.delete_draft(tag, state.open_draft_id)   # refuses old state with no ID, or a replacement draft
            state.open_draft = None
            state.open_draft_id = None
    gh.ensure_tag_available(tag)
    run_dir = job / "runs" / f"{now.astimezone(TZ):%Y%m%d-%H%M%S}"
    manifest = (clone / "sim/Statecraft.Sim.Tests/golden/manifest.txt").read_text(encoding="utf-8")
    return Night(now, day, sha, version, tag, run_dir, time.monotonic() + cfg.deadline_hours * 3600, job, cfg,
                 manifest)


# --- gates, Unity and the full EditMode run ---------------------------------------------------------------------

WARM_UP_OK = "UnityMcpGuard warm-up: gate defines in place"
# The EditMode tests that compare against gitignored local art (Category in HexModelMeshTests/HexUnitTests): a clean
# clone always ignores them, so the nightly leaves them out and its skip count is only the [Explicit] tests (#2655).
LOCAL_ONLY = "LocalOnly"


def _limit(night: Night, minutes: float) -> float:
    return min(night.deadline, time.monotonic() + minutes * 60)


def _step(night: Night, runner: Runner, name: str, cmd: list[str], minutes: float,
          env: dict[str, str] | None = None, *, stop_on_interrupt: bool = False) -> tuple[int, str, Path]:
    log = night.run_dir / f"{name}.log"
    kwargs = {"stop_on_interrupt": True} if stop_on_interrupt else {}
    try:
        code = runner.run(cmd, cwd=night.cfg.clone, log=log, deadline=_limit(night, minutes), env=env, **kwargs)
    except (OSError, subprocess.SubprocessError) as error:
        raise Refused(name, f"{type(error).__name__}: {error}", log=log) from error
    try:
        text = log.read_text(encoding="utf-8", errors="replace") if log.is_file() else ""
    except OSError as error:
        raise Refused(name, f"could not read command log: {error}", log=log) from error
    return code, text, log


def run_gates(night: Night, runner: Runner) -> None:
    base_env = {**os.environ, "MSBUILDDISABLENODEREUSE": "1", "DOTNET_CLI_USE_MSBUILD_SERVER": "0",
                "UseSharedCompilation": "false"}

    def admitted():
        check_window(night)
        if time.monotonic() >= night.deadline:
            raise Refused("managed-deadline", "work deadline expired before a managed command could start")

    def step(name, cmd, minutes, env=None):
        admitted()
        return _step(night, runner, name, cmd, minutes, base_env if env is None else env, stop_on_interrupt=True)

    check_window(night)
    with unity_postmerge.machine_lock.hold():
        # A long acquisition must drain normally; expired work starts no child after it gets the lock.
        admitted()
        pending = None
        try:
            clone = night.cfg.clone
            code, _, log = step("build-sim", ["uv", "run", str(clone / "tools/build_sim_for_unity.py")], 15)
            if code != 0:
                raise Refused("build-sim", "timed out" if code == 124 else f"build_sim_for_unity exited {code}", log=log)
            code, text, log = step("check", ["bash", "scripts/check.sh", "origin/main"], 45)
            why = nightly_checks.check_sh_verdict(text, code)
            if why:
                raise Refused("check", "timed out" if code == 124 else why, log=log)
            code, text, log = step("release-gate", ["bash", "scripts/oss/release_gate.sh"], 60)
            why = nightly_checks.release_gate_verdict(text, code)
            if why:
                raise Refused("release-gate", "timed out" if code == 124 else why, log=log)
            for locale in ("he_IL", "en_US"):
                lang = f"{locale}.UTF-8"
                env = {**base_env, "LANG": lang, "LC_ALL": lang}
                code, text, log = step(f"sim-{locale}",
                                   ["dotnet", "test", "sim/Statecraft.sln", "-c", "Release", "--nologo",
                                    "-nodeReuse:false", "-p:UseSharedCompilation=false"], 30, env)
                failed, total = nightly_checks.dotnet_totals(text)
                if code == 124:
                    raise Refused(f"sim-{locale}", "timed out", log=log)
                if code != 0 or failed or total == 0:
                    raise Refused(f"sim-{locale}", f"dotnet test exited {code}: {failed} failed of {total} tests", log=log)
        except BaseException as primary:
            pending = primary
            raise
        finally:
            # Preserve only this invocation's primary failure, never an exception handled by its caller.
            shutdown_failure = None
            try:
                code = runner.run(["dotnet", "build-server", "shutdown"], cwd=night.cfg.clone,
                                  log=night.run_dir / "build-server-shutdown.log",
                                  deadline=time.monotonic() + 120, env=base_env, stop_on_interrupt=True)
                if code != 0:
                    shutdown_failure = f"compiler server shutdown exited {code}"
            except BaseException as error:
                if pending is None and not isinstance(error, (OSError, subprocess.SubprocessError)):
                    raise
                shutdown_failure = f"compiler server shutdown raised {type(error).__name__}: {error}"
            if shutdown_failure is not None:
                if pending is None:
                    raise Refused("build-server-shutdown", shutdown_failure,
                                  log=night.run_dir / "build-server-shutdown.log")
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
    log = night.run_dir / "restore-settings.log"
    try:
        code = runner.run(list(RESTORE_ARGV), cwd=night.cfg.clone, log=log,
                          deadline=time.monotonic() + 120)
    except (OSError, subprocess.SubprocessError) as error:
        raise Refused("restore-settings", f"{type(error).__name__}: {error}", log=log) from error
    if code != 0:
        raise Refused("restore-settings", f"git checkout of the Unity settings exited {code}", log=log)


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
        warm_out = night.run_dir / "warmup.out"
        try:
            warm_code = runner.run(warm, cwd=night.cfg.clone, log=warm_out,
                                   deadline=_limit(night, 60), env=env, holder=night.run_dir / "lock-holder")
        except (OSError, subprocess.SubprocessError) as error:
            raise Refused("editmode", f"{type(error).__name__}: {error}", log=warm_out) from error
        if warm_code == 124:
            raise Refused("editmode", "the Unity warm-up timed out", log=warm_out)
        warm_log = night.run_dir / "warmup.log"
        warm_text = warm_log.read_text(encoding="utf-8", errors="replace") if warm_log.is_file() else ""
        if warm_code != 0 or WARM_UP_OK not in warm_text:
            raise Refused("editmode", f"the Unity warm-up failed or did not confirm (exit {warm_code})", log=warm_out)
        check_window(night)
        argv = unity_argv(night, ["-buildTarget", "OSXUniversal", "-runTests", "-testPlatform", "EditMode",
                                  "-testCategory", "!" + LOCAL_ONLY, "-testResults", str(xml),
                                  "-logFile", str(night.run_dir / "editmode.log")],
                          webgl=False)
        edit_out = night.run_dir / "editmode.out"
        try:
            code = runner.run(argv, cwd=night.cfg.clone, log=edit_out,
                              deadline=_limit(night, 150), env=env, holder=night.run_dir / "lock-holder")
        except (OSError, subprocess.SubprocessError) as error:
            raise Refused("editmode", f"{type(error).__name__}: {error}", log=edit_out) from error
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
        raise Refused("editmode", why, log=night.run_dir / "editmode.out")
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
    variant = "pages" if pages else "gz"
    gate = f"build-web-{variant}"
    build = clone / "unity/Builds" / name
    shutil.rmtree(build, ignore_errors=True)   # a page left by an earlier run must not pass the stamp check
    log = night.run_dir / f"unity-build-web-{variant}.log"
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
        out_log = night.run_dir / f"unity-build-web-{variant}.out"
        try:
            code = runner.run(unity_argv(night, args, webgl=True), cwd=clone, log=out_log,
                              deadline=_limit(night, 120), env=env, holder=night.run_dir / "lock-holder")
        except (OSError, subprocess.SubprocessError) as error:
            raise Refused(gate, f"{type(error).__name__}: {error}", log=out_log) from error
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
            raise Refused(gate, "the Unity build timed out", log=log)
        raise Refused(gate, f"the Unity build exited {code}{_build_failure_detail(log)}", log=log)
    if restore_error is not None:
        raise restore_error
    index = build / "index.html"
    page = index.read_text(encoding="utf-8", errors="replace") if index.is_file() else ""
    built = nightly_checks.product_version(page)
    if built != night.version:
        raise Refused("version-stamp", f"{name}/index.html says {built!r}, tonight's version is {night.version!r}")
    return build


def package_zip(night: Night, runner: Runner, build: Path, pages: bool) -> Path:
    variant = "pages" if pages else "gz"
    gate = f"package-{variant}"
    zip_path = night.run_dir / f"statecraft-israel-v{night.version}-web{'-pages' if pages else ''}.zip"
    code, text, log = _step(night, runner, gate,
                            ["bash", "scripts/oss/package_web.sh", str(build), str(zip_path)], 20)
    if code != 0:
        raise Refused(gate, "timed out" if code == 124 else f"package_web.sh exited {code}", log=log)
    if "PACKAGED " not in text or "OK: notices inside" not in text:
        raise Refused(gate, "package_web.sh did not confirm the zip and its notices", log=log)
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
        name = f"web-check-{build.name}-{i}"
        code, text, log = _step(night, runner, name, argv, 10)
        if code == 124:
            raise Refused("web-check", f"{build.name} pass {i} timed out", log=log)
        if code != 0 or not re.search(r"^PASS:", text, re.M):
            raise Refused("web-check", f"{build.name} pass {i} exited {code} without a PASS line", log=log)


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


# The boots follow two WebGL builds, 14 browser runs and a ~466 MB download. On 7 November 2026 they ran 2 s slower
# than an A/B run of the same build seven minutes later (#2914), so the first boot waits for the machine to settle.
# The bound is untouched: an unsettled machine is logged and measured anyway.
SETTLE_LOAD = 2.0     # the 1-minute load average boot 1 waits to see under; tune it from the logged state lines
SETTLE_MAX_S = 300    # the longest the wait may take
SETTLE_POLL_S = 15


def _read_state(runner: Runner, cmd: list[str], cwd: Path) -> str:
    """One read-only macOS state command's output on one line. A failed read is written down, never a refusal."""
    try:
        text = runner.capture(cmd, cwd=cwd, timeout=30)
    except (Refused, *STAGE_ERRORS) as error:
        text = f"unavailable ({getattr(error, 'detail', error)})"
    return "; ".join(line.strip() for line in text.splitlines() if line.strip())


def _load_1min(loadavg: str) -> float | None:
    """`sysctl -n vm.loadavg` prints `{ 1.52 1.40 1.31 }`; None when the read failed."""
    m = re.match(r"\{\s*([\d.]+)", loadavg)
    return float(m.group(1)) if m else None


def machine_state(runner: Runner, cwd: Path) -> list[str]:
    """The lines at the top of each boot-N.log, so a slow boot can be explained from its own log (#2914)."""
    load = _read_state(runner, ["sysctl", "-n", "vm.loadavg"], cwd)
    memory = _read_state(runner, ["memory_pressure"], cwd)
    free = re.search(r"System-wide memory free percentage: \S+", memory)   # the rest is page counts
    return [f"state: load averages {load.strip('{} ')}",
            f"state: vm.swapusage {_read_state(runner, ['sysctl', '-n', 'vm.swapusage'], cwd)}",
            f"state: memory_pressure {free.group(0) if free else memory}",
            f"state: pmset -g therm {_read_state(runner, ['pmset', '-g', 'therm'], cwd)}"]


def settle(runner: Runner, cwd: Path, sleep: Callable[[float], None]) -> str:
    """Wait until the 1-minute load average is under SETTLE_LOAD, at most SETTLE_MAX_S; the line saying how it went."""
    waited = 0
    while True:
        load = _load_1min(_read_state(runner, ["sysctl", "-n", "vm.loadavg"], cwd))
        if load is None:   # nothing to wait on, and an unreadable load must not cost every night the full wait
            return "settle: 1-minute load unreadable, not waiting"
        if load < SETTLE_LOAD:
            return f"settle: 1-minute load {load:.2f} under {SETTLE_LOAD} after {waited} s"
        if waited >= SETTLE_MAX_S:
            return (f"settle: not settled after {waited} s (1-minute load {load:.2f}, limit {SETTLE_LOAD}); "
                    "measuring anyway")
        sleep(SETTLE_POLL_S)
        waited += SETTLE_POLL_S


def measure_boot(night: Night, runner: Runner, build: Path, *, sleep: Callable[[float], None] = time.sleep,
                 boots: tuple[int, int, int] = (1, 2, 3)) -> float:
    """The median of three headless boots, each alone on the machine (no other Unity, no other check), the first
    after the machine settles. Each boot-N.log starts with the machine's state, read just before that boot; `boots`
    numbers the logs, so a re-measure (#2921) keeps the first three."""
    clone = night.cfg.clone
    runs: list[int] = []
    settled = settle(runner, night.cfg.main_repo, sleep)
    for i in boots:
        argv = ["uv", "run", str(clone / "tools/machine_lock.py"), "--exclusive", "--wait-unity", "--timeout", "2700",
                "--", "uv", "run", str(clone / "tools/web_check.py"), str(build), "--query", "bootlog",
                "--expect", "GAME OK", "--timeout", "300", "--stall", "90"]
        log = night.run_dir / f"boot-{i}.log"
        try:
            header = machine_state(runner, night.cfg.main_repo) + ([settled] if i == boots[0] else [])
            log.write_text("\n".join(header) + "\n", encoding="utf-8")   # the runner appends the boot after it
            code = runner.run(argv, cwd=night.cfg.main_repo, log=log, deadline=_limit(night, 60))
        except (OSError, subprocess.SubprocessError) as error:
            raise Refused("boot", f"{type(error).__name__}: {error}", log=log) from error
        text = log.read_text(encoding="utf-8", errors="replace") if log.is_file() else ""
        if code == 124:
            raise Refused("boot", f"boot run {i} timed out", log=log)
        if code == 3:
            raise Refused("boot", f"boot run {i}: another Unity was still running after 45 minutes", log=log)
        if code != 0:
            raise Refused("boot", f"boot run {i} exited {code}", log=log)
        try:
            runs.append(nightly_checks.boot_ms(text))
        except ValueError as e:
            raise Refused("boot", f"boot run {i}: {e}", log=log) from e
    return float(statistics.median(runs))


def boot_gate(night: Night, state: State, runner: Runner, build: Path) -> tuple[float, str]:
    """measure_boot's median against the bound. A median over it gets one re-measure after the machine settles again
    (the owner's ruling on #2921: v0.3.8 was refused at 6313 ms and then booted in about 4450 ms), and only a second
    median over the bound refuses. Returns the median the night passed with and the verdict's phase."""
    boot = measure_boot(night, runner, build)
    phase, why = nightly_checks.boot_verdict(boot, state.boot_baseline, BASELINE_NIGHTS)
    if why is None:
        return boot, phase
    print(f"boot: {why}; settling and measuring once more (boot-4..6.log)")
    again = measure_boot(night, runner, build, boots=(4, 5, 6))
    phase, why_again = nightly_checks.boot_verdict(again, state.boot_baseline, BASELINE_NIGHTS)
    if why_again:
        raise Refused("boot", f"{why}; measured again after settling: {why_again}")
    print(f"boot: first median {boot:.0f} ms was over the bound; re-measure after settling: {again:.0f} ms, under it")
    return again, phase


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
            gh.delete_draft(night.tag, state.open_draft_id)
        if present != "published":
            state.open_draft = None
            state.open_draft_id = None
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


def _recover_latest(state: State, gh: GitHub, failed: str, previous: str | None,
                    save: Callable[[], bool | None]) -> tuple[bool, str]:
    """Checkpoint recovery before trying it. Only a verified result clears it; every outcome is reported."""
    target = f"previous Latest {previous}" if previous is not None else "no previous Latest"
    state.latest_recovery_pending = LatestRecovery(failed, previous)
    try:
        checkpoint_saved = save() is not False
    except Exception as e:
        checkpoint_saved = False
        print(f"could not checkpoint Latest recovery: {_detail(e)}", file=sys.stderr)
    recovered = False
    try:
        acted, current = gh.restore_latest(previous, failed)
        if acted:
            note = (f"restored {target}" if previous is not None else
                    "no previous Latest; removed the failed release's Latest mark")
        elif current is None:
            note = f"there is no Latest now, so {failed} is not Latest: left alone"
        elif current == previous:
            note = f"{target} still stands; left alone"
        else:
            note = f"Latest is now {current}, not {failed}: left alone"
        state.latest_recovery_pending = None
        recovered = True
    except Exception as e:
        note = f"Latest recovery failed ({target}): {_detail(e)}; check by hand"
    try:
        result_saved = save() is not False
    except Exception as e:
        result_saved = False
        print(f"could not save Latest recovery result: {_detail(e)}", file=sys.stderr)
    if not checkpoint_saved or not result_saved:
        note += "; recovery state could not be saved; check by hand"
        recovered = False
    note = f"{failed} ({target}): {note}"
    print(note, file=sys.stderr)
    notify("Nightly Latest recovery", note[:200])
    return recovered, note


def _restore_latest(night: Night, state: State, gh: GitHub, previous: str | None,
                    save: Callable[[], bool | None]) -> str:
    _, note = _recover_latest(state, gh, night.tag, previous, save)
    try:
        (night.run_dir / "latest-rollback.log").write_text(note + "\n", encoding="utf-8")
    except OSError as e:
        print(f"could not write Latest recovery log: {e}", file=sys.stderr)
    return "; " + note


def publish_night(night: Night, state: State, gh: GitHub, itch, zips: tuple[Path, Path], sizes: dict[str, int],
                  boot: float, *, previous: tuple[str, str | None], save: Callable[[], bool | None],
                  runner: Runner) -> None:
    """Draft first, so nothing public changes until the draft's bytes match: draft, verify, publish, then the site.
    `save` persists the state; it runs right after the draft exists and right after the release is public, so a
    kill leaves a record of both."""
    prev_tag, prev_sha = previous
    itch_skip = itch_skip_reason(night.cfg)
    use_itch = itch_skip is None
    if use_itch and itch is None:   # before any store change: enabled means the push is part of this night
        raise Refused("itch", "itch is enabled but there is no itch client")
    since = prev_sha or prev_tag
    # %aN is the author name after .mailmap, so a mailmap entry gives a contributor their credited name (#2654)
    log = _git(runner, night.cfg.clone, "log", "--format=%s%x1f%aN", f"{since}..{night.sha}")
    changes = [(s, a) for s, _, a in (ln.partition("\x1f") for ln in log.split("\n") if ln)]
    notes = night.run_dir / "release-notes.md"
    notes.write_text(release_notes(version=night.version, sha=night.sha, since=since, changes=changes,
                                   sha256_lines=sha256_lines(list(zips))), encoding="utf-8")
    gh.ensure_tag_available(night.tag)   # again just before create: a release made since preflight is never ours
    def created(release_id: int) -> None:
        state.open_draft = night.tag
        state.open_draft_id = release_id
        if save() is False:
            raise Refused("github-draft", f"could not persist ownership of {night.tag} (release ID {release_id}) before upload; check by hand")

    try:
        gh.create_draft(night.tag, night.sha, notes, list(zips), created=created)
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
        state.open_draft_id = None
        state.last_published = Published(night.tag, night.sha, night.version, sizes, boot)
        state.site_pending = night.version   # until the demo serves this version, a later night retries the deploy
        save()
        if not use_itch:
            skip = f"itch: skipped ({itch_skip})"
            print(skip)
            night.notes.append(skip)
        failure = "no attempt"
        for _ in range(2):   # the release is public already, so retry the site once before restoring Latest
            try:
                gh.dispatch_site(time.monotonic() + SITE_WAIT_SECONDS)
                demo_serves(night.cfg.site_demo_url, night.version, deadline=time.monotonic() + SITE_WAIT_SECONDS)
                if len(state.boot_baseline) < BASELINE_NIGHTS:
                    state.boot_baseline.append(boot)
                state.site_pending = None
                return
            except Exception as e:
                failure = _detail(e)
        raise Refused("site", f"{night.tag} is public; the demo was not verified to serve {night.version} "
                              f"and may have changed (unverified): {failure}")
    except BaseException as e:
        detail = _detail(e)
        if publish_started:
            detail += _restore_latest(night, state, gh, previous_latest, save)
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


# --- the Steam stage (plan 2026-11-01-steamworks-integration Task 7, #2155; owner answer Q7 "Also nightlies") ------

STEAM_BUILD_MINUTES = 240   # three Unity players and the macOS golden check
STEAM_STAGE_MINUTES = 60    # zips, the draft release upload and the workflow dispatch


def steam_ids_missing(clone: Path) -> str | None:
    """Why the Steam stage cannot run yet, or None: steam/steam.json must name the app and every depot."""
    path = clone / "steam/steam.json"
    try:
        config = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        return f"cannot read {path}: {type(e).__name__}"
    if not isinstance(config, dict) or not isinstance(config.get("depots"), dict):
        raise ValueError(f"{path} is not an object with a depots object")
    depots = config["depots"]
    missing = [name for name, value in [("app_id", config.get("app_id")),
                                        *((f"depots.{p}", depots.get(p)) for p in ("windows", "mac", "linux"))]
               if value is None]
    return f"steam/steam.json has no {', '.join(missing)} yet" if missing else None


def run_steam(night: Night, runner: Runner) -> None:
    """After the web nightly is published: build the Steam players, stage them in a draft release and dispatch the
    upload to the `nightly` branch. It never refuses, unpublishes or rolls back the web nightly: any failure only adds
    steam=failed and the exact evidence log to the night's line, and notifies."""
    out = night.run_dir / "steam-release"
    try:
        if (why := steam_ids_missing(night.cfg.clone)) is not None:
            print(f"steam: skipped ({why})")
            return
        check_window(night)
        # tonight's tag was made on GitHub by the publish; release_build.py checks it points at HEAD and never fetches
        _git(runner, night.cfg.clone, "fetch", "--no-tags", "origin", f"refs/tags/{night.tag}:refs/tags/{night.tag}")
        code, _, log = _step(night, runner, "steam-build",
                             ["uv", "run", "tools/release_build.py", "--platform", "all", "--steam", "--tag", night.tag,
                              "--nightly-version", night.version, "--out", str(out)], STEAM_BUILD_MINUTES,
                             unity_env(night), stop_on_interrupt=True)
        if code != 0:
            raise Refused("steam-build", "release_build.py timed out" if code == 124 else f"release_build.py exited {code}",
                          log=log)
        code, _, log = _step(night, runner, "steam-stage",
                             ["uv", "run", "tools/steam_upload.py", "stage", "--out", str(out), "--tag", night.tag],
                             STEAM_STAGE_MINUTES, stop_on_interrupt=True)
        if code != 0:
            raise Refused("steam-stage", "steam_upload.py timed out" if code == 124 else f"steam_upload.py exited {code}",
                          log=log)
    except Exception as e:
        detail = _detail(e)
        print(f"steam: failed: {detail}", file=sys.stderr)
        source = e.log if isinstance(e, Stop) else None
        try:
            failure_log = write_refusal_log(night.run_dir, e.gate if isinstance(e, Stop) else "steam", detail, source)
            night.notes.append(f"steam=failed steam_log={failure_log}")
            where = str(failure_log)
        except OSError as error:
            night.notes.append(f"steam=failed detail={detail}; could not write steam evidence: {error}")
            where = f"evidence unavailable ({error})"
        notify("Nightly Steam stage failed", f"{night.tag} is published; Steam: {detail}; log: {where}"[:200])
    finally:
        # Nothing prunes the run folders, and three players and their zips a night would eat the free space the next
        # web nightly's disk gate needs (review of #2155). The draft holds the zips; the record and the logs stay.
        shutil.rmtree(out / "steam", ignore_errors=True)
        for z in out.glob("steam-*.zip"):
            z.unlink(missing_ok=True)


# --- the night --------------------------------------------------------------------------------------------------

def _build_both(night: Night, runner: Runner, free_gb: Callable[[Path], float]) -> tuple[Path, Path]:
    """The gzip and Pages builds. The tree is checked after them, and also after a failed one, so the record says
    whether the settings came back."""
    try:
        gz = build_web(night, runner, pages=False, free_gb=free_gb)
        # Bee may retire the previous player's files while building Pages. Keep the exact first player outside its outputs.
        saved_gz = night.run_dir / "Web-gzip"
        shutil.copytree(gz, saved_gz)   # refuse a collision or incomplete copy; never reuse older run evidence
        gz = saved_gz
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
               save: Callable[[], bool | None] = lambda: None, no_steam: bool = False) -> bool:
    """Every gate, then (unless no_publish) the publish, then (unless no_steam) the Steam stage. True when published,
    False when stopped before any store."""
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
    boot, phase = boot_gate(night, state, runner, gz)
    print(f"boot: {boot:.0f} ms ({phase}, {len(state.boot_baseline)} baseline nights)")
    if no_publish:
        print(f"NO-PUBLISH: all gates passed for {night.tag}")
        return False
    publish_night(night, state, gh, itch, zips, sizes, boot, previous=previous, save=save, runner=runner)
    if no_steam:
        print("steam: skipped (--no-steam)")
    else:
        run_steam(night, runner)
    return True


def _try_save(state_path: Path, state: State) -> bool:
    try:
        save_state(state_path, state)
        return True
    except OSError as e:   # never costs the night its record, and the final save tries again
        print(f"could not save state: {e}", file=sys.stderr)
        notify("Nightly state not saved", f"{e}"[:200])   # a published release with unsaved state must not be silent
        return False


def run_night(cfg: Config, state_path: Path, job: Path, runner: Runner, gh: GitHub | None, itch, *, now: datetime,
              manual: bool, no_publish: bool, client_error: Refused | None = None,
              free_gb: Callable[[Path], float] = lambda p: shutil.disk_usage(p).free / 1e9,
              no_steam: bool = False, version: str | None = None) -> str:
    outcome, gate, detail = "REFUSED", "internal", ""
    state: State | None = None
    night: Night | None = None
    evidence: Path | None = None
    run_dir = job / "runs" / f"{now.astimezone(TZ):%Y%m%d-%H%M%S}"
    recovery_note = ""
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
            pending = state.latest_recovery_pending
            if pending is not None:
                if no_publish:
                    raise Refused("github-latest", "Latest recovery is pending; --no-publish leaves it alone")
                recovered, recovery_note = _recover_latest(state, gh, pending.failed, pending.previous,
                                                            lambda: _try_save(state_path, state))
                if not recovered:
                    raise Refused("github-latest", recovery_note)
            night = preflight(cfg, state, runner, gh, job=job, now=now, manual=manual, free_gb=free_gb,
                              no_publish=no_publish, version=version)
            published = run_stages(night, state, runner, gh, itch, no_publish=no_publish, free_gb=free_gb,
                                   save=lambda: _try_save(state_path, state), no_steam=no_steam)
            outcome, gate = ("PUBLISHED" if published else "NO-PUBLISH"), "-"
        except Stop as e:
            outcome, gate, detail = e.outcome, e.gate, e.detail
            evidence = e.log
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
            if recovery_note and recovery_note not in detail:
                detail = " ".join(filter(None, [detail, recovery_note]))
            if night is not None and night.notes:
                detail = " ".join(filter(None, [detail, *night.notes]))
            if outcome == "REFUSED":
                try:
                    evidence = write_refusal_log(run_dir, gate, detail, evidence)
                except OSError as error:
                    evidence = None
                    detail = f"{detail}; could not write refusal log: {error}"
            elif night is not None:
                evidence = night.run_dir
            line = log_line(now, outcome, tag=night.tag if night else None, sha=night.sha if night else None,
                            gate=gate, log=evidence)
            record(job / "LOG.md", cfg.handoff, f"{line} {detail}".rstrip() if detail else line)
            if state is not None:   # after the record, so a failed save never costs the night its line
                state.last_run_day = day.isoformat()
                _try_save(state_path, state)
            if outcome == "REFUSED":
                notify("Nightly refused", f"{gate}: {detail}"[:200])
    return outcome


# --- the itch.io save test --------------------------------------------------------------------------------------------

PERSIST_SAVED = re.compile(r"^PERSISTENCE saved (\d{4}-\d{2}-\d{2}) turn (\d+) hash ([0-9a-f]+)$", re.M)
# The frame URL comes from `butler status` (the upload and build ids), so the test page is never read and can stay
# Restricted (#2056, owner 2 Nov 2026: "Code change, page stays Restricted"). itch serves a new upload's frame only
# after it has unpacked it: unpacking the web build took about 17 minutes on 28 October 2026, so the save test polls
# the frame for up to 40 minutes. While it waited for the second upload that day, itch answered 429 Too Many
# Requests; a 429 slows the polling down (Retry-After, bounded) instead of ending the test, within the same 40 minutes.
SAVETEST_FRAME_TRIES = 80
SAVETEST_FRAME_SLEEP = 30
SAVETEST_RATE_LIMIT_SLEEP = 120   # after an HTTP 429 without a usable Retry-After
SAVETEST_RATE_LIMIT_MAX = 300   # a longer Retry-After is capped; the total wait stays within the frame budget
SAVETEST_FRAME_WAITING = (403, 404)   # itch answers these until the frame is served; anything else ends the wait
SAVETEST_GAME_MARKER = b"createUnityInstance("   # in the index.html of unity/Assets/WebGLTemplates/Statecraft
SAVETEST_RUN_MINUTES = 10
TOOLS = Path(__file__).resolve().parent


def _rate_limit_pause(e: urllib.error.HTTPError) -> int:
    """Seconds to wait after itch answered HTTP 429: its Retry-After, kept within bounds."""
    try:
        asked = int((e.headers or {}).get("Retry-After", ""))
    except ValueError:
        asked = SAVETEST_RATE_LIMIT_SLEEP
    return min(max(asked, SAVETEST_FRAME_SLEEP), SAVETEST_RATE_LIMIT_MAX)


def _wait_for_frame(fetch, sleep, url: str) -> str | None:
    """None once the game frame at `url` is served: HTTP 200 from that very URL (no redirect) with the game's loader
    in it. Otherwise, when the wait budget runs out, what itch last answered ("HTTP 404", or a page that is not the
    game). A 403, a 404 or another page is "not served yet", a 429 only slows the polling down; any other failure is
    a Refused("savetest")."""
    waited, budget = 0, SAVETEST_FRAME_TRIES * SAVETEST_FRAME_SLEEP
    while True:
        try:
            with fetch(url, timeout=30) as response:
                body = response.read()
                status, final = getattr(response, "status", None), response.geturl()
            if status == 200 and final == url and SAVETEST_GAME_MARKER in body:
                return None
            seen = f"HTTP {status} from {final}, not the game page"
            pause = SAVETEST_FRAME_SLEEP
        except urllib.error.HTTPError as e:
            if e.code == 429:
                pause = _rate_limit_pause(e)
                print(f"savetest: itch answered 429, waiting {pause} s", file=sys.stderr)
            elif e.code in SAVETEST_FRAME_WAITING:
                pause = SAVETEST_FRAME_SLEEP
            else:
                raise Refused("savetest", f"cannot read {url}: HTTP {e.code}") from e
            seen = f"HTTP {e.code}"
        except (OSError, ValueError) as e:   # URLError and HTTPException subclass OSError or ValueError
            raise Refused("savetest", f"cannot read {url}: {type(e).__name__}: {e}") from e
        if waited + pause > budget:
            return seen
        sleep(pause)
        waited += pause


def _live_frame(itch: Itch, fetch, sleep, name: str, version: str, evidence: dict) -> tuple[str | None, str]:
    """(frame URL, "") of this run's upload `version` once itch serves its frame, or (None, why not). The URL is built
    from the upload and build ids `butler status` shows for the test channel while `version` is live there; the ids
    go into the evidence."""
    ids = itch.live_frame_ids(version)
    if ids is None:
        return None, f"butler status shows no upload and build ids of {version} live on the test channel after {name}"
    frame = itch_build_frame_url(*ids)
    evidence[name.replace(" ", "_")] = {"upload_id": ids[0], "build_id": ids[1], "frame_url": frame}
    seen = _wait_for_frame(fetch, sleep, frame)
    if seen is not None:
        minutes = SAVETEST_FRAME_TRIES * SAVETEST_FRAME_SLEEP // 60
        return None, f"the game frame of {name} ({frame}) still answered {seen} after {minutes} minutes"
    return frame, ""


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
    each upload hides the game while itch unpacks it (up to an hour on 28 October 2026). The owner or an
    agent may start it (owner, 2 Nov 2026, "2 (a)"): it pushes only to the Restricted test page."""
    stamp = datetime.now(TZ).strftime("%Y%m%d-%H%M%S")
    work = Path(job) / "savetest" / stamp
    evidence: dict = {"tag": tag, "target": cfg.savetest_itch_target}
    result, detail = "INCONCLUSIVE", ""
    try:
        with savetest_lock(Path(job)) as held:
            if not held:
                detail = "another save test is running on this job (savetest.lock); nothing was pushed"
            else:
                result, detail = _savetest(cfg, work, runner, gh, itch, tag, evidence, fetch, sleep)
    except Stop as e:
        result = "INCONCLUSIVE"
        detail = f"{e.gate}: {e.detail}"
    except (OSError, SystemExit) as e:   # find_chrome exits when there is no Chrome
        result = "INCONCLUSIVE"
        detail = f"{type(e).__name__}: {e}"
    try:
        evidence.update(result=result, detail=detail)
        if work.is_dir():
            (work / "evidence.json").write_text(json.dumps(evidence, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        if result == "PASS":
            enable_itch(config_path, work / "evidence.json")
    except (Stop, OSError) as e:   # #2684 review: a failure after the test still gets its one SAVETEST record
        result = "INCONCLUSIVE"
        detail = f"{e.gate}: {e.detail}" if isinstance(e, Stop) else f"{type(e).__name__}: {e}"
        evidence.update(result=result, detail=detail)
        if work.is_dir():
            try:
                (work / "evidence.json").write_text(json.dumps(evidence, indent=2, sort_keys=True) + "\n",
                                                     encoding="utf-8")
            except OSError:
                pass   # LOG.md stays the record when the evidence file itself cannot be written
    record(Path(job) / "LOG.md", cfg.handoff, f"SAVETEST {result} tag={tag} {detail}".rstrip())
    return result


def _itch_project(target: str) -> str:
    """`user/project` of a butler target, without its channel: every channel of a project uploads to its one page."""
    return target.partition(":")[0].lower()


SAVETEST_TARGET = re.compile(r"[a-z0-9_-]+/[a-z0-9_-]+:[a-z0-9._-]+", re.I)   # user/project:channel, nothing else


def _page_project(url: str | None) -> str | None:
    """`user/project` of an itch page URL https://<user>.itch.io/<project>, or None for anything else."""
    parts = urllib.parse.urlsplit((url or "").strip())
    user, dot, host = parts.netloc.lower().partition(".")
    path = parts.path.strip("/").lower()
    return f"{user}/{path}" if dot and host == "itch.io" and path and "/" not in path else None


def _test_page_problem(cfg: Config) -> str | None:
    """Why the save test may not push, or None: it needs its own test project, distinct from the public one (the
    project of itch_target, on any channel, and the project of the public itch_page_url)."""
    target = cfg.savetest_itch_target
    if not target:
        return "savetest_itch_target must name the separate test page's project (user/project:channel) in the job's config"
    if not SAVETEST_TARGET.fullmatch(target):
        return f"the save test's test page target {target!r} is not user/project:channel (letters, digits, - and _)"
    public = {_itch_project(cfg.itch_target), _page_project(cfg.itch_page_url)}
    if _itch_project(target) in public:
        return "the save test's test page must not be the public itch project (any channel of it uploads to its page)"
    return None


SAVETEST_KEYS = ("savetest_itch_target",)


def itch_skip_reason(cfg: Config) -> str | None:
    """Why tonight's build must not be pushed to the public itch page, or None when it may (#2056). A push needs a
    separate test page in the job's config and a recorded save test PASS on it: itch_enabled alone is not enough,
    so a hand-set flag or a test page that is the public one never leads to a push. Every reason is logged."""
    missing = [key for key in SAVETEST_KEYS if not getattr(cfg, key)]
    if missing:
        return f"the job config has no {' or '.join(missing)}, so no save test can run on a separate test page"
    problem = _test_page_problem(cfg)
    if problem:
        return problem
    if not cfg.itch_enabled:
        return "save test not passed"
    if not cfg.itch_evidence:
        return "itch_enabled is set but no save test evidence is recorded (itch_evidence)"
    try:
        evidence = json.loads(Path(cfg.itch_evidence).read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        return f"the save test evidence {cfg.itch_evidence} is unreadable ({type(e).__name__})"
    if not isinstance(evidence, dict) or evidence.get("result") != "PASS":
        return f"the save test evidence {cfg.itch_evidence} does not record a save test PASS"
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
    if not (folder / "index.html").is_file():
        return "INCONCLUSIVE", f"{zip_path.name} has no index.html at its root, so the build's frame URL would not load"
    profile = work / "profile"

    itch.push(folder, f"{version}-savetest-a")
    itch.wait_live(f"{version}-savetest-a", time.monotonic() + ITCH_WAIT_SECONDS)
    frame_a, why = _live_frame(itch, fetch, sleep, "upload a", f"{version}-savetest-a", evidence)
    if frame_a is None:
        return "INCONCLUSIVE", why
    code, text = _frame_check(runner, work, "a-first-save", frame_a, profile, "--first-save")
    saved = PERSIST_SAVED.search(text)
    if code != 0 or not re.search(r"^PASS:", text, re.M) or saved is None:
        return "INCONCLUSIVE", f"upload A did not reach a durable first save (web_check exited {code})"
    tuple_a = {"date": saved.group(1), "turn": int(saved.group(2)), "hash": saved.group(3)}
    evidence["tuple"] = tuple_a

    itch.push(folder, f"{version}-savetest-b")
    itch.wait_live(f"{version}-savetest-b", time.monotonic() + ITCH_WAIT_SECONDS)
    frame_b, why = _live_frame(itch, fetch, sleep, "upload b", f"{version}-savetest-b", evidence)
    if frame_b is None:
        return "INCONCLUSIVE", why
    if frame_b == frame_a:   # both frames are on itch's one frame host, so IndexedDB is shared between them
        return "INCONCLUSIVE", f"upload B has the same frame URL as upload A ({frame_a}), so the test proves nothing"

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
        record(Path(job) / "LOG.md", cfg.handoff,
               f"SAVETEST INCONCLUSIVE tag={tag} {e.gate}: {e.detail}")
        print(e, file=sys.stderr)
        return 1
    result = savetest(cfg, job / "config.json", job, runner, gh, itch, tag=tag)
    print(result)
    return 0 if result == "PASS" else 1


def _mark_night(job: Path, now: datetime) -> None:
    """Sets state.json's last_run_day to tonight after main() records a refusal before run_night starts, as run_night
    does for every night it records, so the next night does not also record it as MISSED (#3009; the launcher's
    _mark_night, #2694). Written only under run.lock: on contention the run holding it writes the day itself. A
    state file load_state refuses is left alone (the next night records that refusal as gate=state)."""
    try:
        with run_lock(job) as held:
            if not held:
                return
            path = job / "state.json"
            state = load_state(path)
            state.last_run_day = night_of(now).isoformat()
            save_state(path, state)
    except (Refused, OSError) as error:
        print(f"could not mark {night_of(now)} as run in {job / 'state.json'}: {error}", file=sys.stderr)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="nightly.py")
    sub = parser.add_subparsers(dest="command", required=True)
    run = sub.add_parser("run")
    run.add_argument("--manual", action="store_true")
    run.add_argument("--no-publish", action="store_true")
    run.add_argument("--no-steam", action="store_true", help="skip the Steam stage after the web publish")
    run.add_argument("--version", dest="release_version", metavar="X.Y.Z",
                     help="with --manual only: tonight's version instead of the next patch; it must be above "
                          "every published vX.Y.Z")
    test = sub.add_parser("savetest", help="the two-upload itch.io save test: pushes twice to the separate test page, never the public one")
    test.add_argument("--tag", required=True, help="a published nightly release tag")
    args = parser.parse_args(argv)
    if getattr(args, "release_version", None) is not None:   # refused before any work, lock or record
        if not args.manual:
            parser.error("--version needs --manual (the 03:00 job always takes the next patch)")
        try:
            check_version_form(args.release_version)
        except Refused as e:
            parser.error(f"--version: {e.detail}")
    job = JOB
    now = datetime.now(TZ)
    try:
        cfg = load_config(job / "config.json")
    except Refused as e:   # a corrupt config is a recorded REFUSED night, not a first night on defaults
        if not getattr(args, "no_publish", False):   # the savetest command has no such flag
            detail = e.detail
            try:
                evidence = write_refusal_log(job / "runs" / f"{now.astimezone(TZ):%Y%m%d-%H%M%S}",
                                             e.gate, detail, e.log)
            except OSError as error:
                evidence = None
                detail = f"{detail}; could not write refusal log: {error}"
            line = log_line(now, "REFUSED", tag=None, sha=None, gate=e.gate, log=evidence)
            record(job / "LOG.md", Config().handoff, f"{line} {detail}")
            if args.command == "run":   # after the record, as in run_night; the save test is not the night's run
                _mark_night(job, now)
            notify("Nightly refused", str(e)[:200])
        print(e, file=sys.stderr)
        return 1
    # #2644: run_lock raises Refused on a lock-system failure (not contention). Before the operation starts that is
    # a recorded REFUSED night; after it records its own result, a teardown failure is only an stderr note.
    operation_finished = False
    savetest_code: int | None = None
    outcome: str | None = None
    try:
        with run_lock(job) as held:
            if not held:
                print("another nightly run holds run.lock", file=sys.stderr)
                return 1
            if args.command == "savetest":
                savetest_code = run_savetest(cfg, job, args.tag)
            else:
                client_error = None
                try:
                    gh, itch = _clients(cfg)
                except Refused as e:   # run_night records the unusable client as a REFUSED night
                    gh = itch = None
                    client_error = e
                outcome = run_night(cfg, job / "state.json", job, ProcRunner(), gh, itch, now=now,
                                    manual=args.manual, no_publish=args.no_publish, client_error=client_error,
                                    no_steam=args.no_steam, version=args.release_version)
            operation_finished = True
    except Refused as e:
        if operation_finished:
            print(f"{e}; the completed operation's existing record is unchanged", file=sys.stderr)
            if savetest_code is not None:
                return savetest_code
            assert outcome is not None
            print(outcome)
            return 1 if outcome == "REFUSED" else 0
        if not getattr(args, "no_publish", False):   # a by-hand --no-publish run writes no record (tools/README.md)
            line = log_line(now, "REFUSED", tag=None, sha=None, gate=e.gate, log=None)
            record(job / "LOG.md", cfg.handoff, f"{line} {e.detail}")
            if args.command == "run":   # the lock is retaken; a lock still failing is an stderr note
                _mark_night(job, now)
            notify("Nightly refused", str(e)[:200])
        print(e, file=sys.stderr)
        return 1
    if savetest_code is not None:
        return savetest_code
    assert outcome is not None
    print(outcome)
    return 1 if outcome == "REFUSED" else 0


if __name__ == "__main__":
    sys.exit(main())
