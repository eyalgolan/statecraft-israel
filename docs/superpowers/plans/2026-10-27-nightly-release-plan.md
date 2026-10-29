# Nightly Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every night at 03:00 Asia/Jerusalem, build the newest `origin/main` on the owner's Mac and, only if every gate passes, publish it as a GitHub pre-release that the website's `/demo/` serves (and, once the save test passes, on itch.io), refusing anything that could be worse than what is live.

**Architecture:** A launchd agent starts `tools/nightly_launch.py` (a copy in the job folder), which takes the run lock, moves a dedicated clone's own `main` to `origin/main` and runs `tools/nightly.py run` from that clone. `nightly.py` is a straight pipeline (preflight, gates, Unity EditMode, two stamped WebGL builds, no-regression checks, draft-first publish, record), where every stage raises `Refused`/`Skipped`/`Missed` and one `finally` writes the night's line. Pure decisions live in `nightly_checks.py`, store side effects (gh, butler, Pages) in `nightly_publish.py`, and state/config/log in `nightly_state.py`. Every process goes through one injectable `Runner`, so the tests run the whole pipeline on fakes.

**Tech Stack:** Python 3.12 stdlib (the tools' `uv run` scripts), pytest 9.1.1, `gh` 2.97 and `butler` 15.32 through their stored logins, Unity 6000.3.25f1 batch mode, launchd.

**Spec:** `docs/superpowers/specs/2026-10-27-nightly-release-design.md` (approved by the owner on 27 October 2026, "Approve as written"; committed in Task 9). Read it with this plan.

## Global Constraints

- Runs on the owner's Mac under launchd at 03:00 Asia/Jerusalem. The Mac stays awake (owner: "Mac stays awake"), so there is no wake schedule.
- Never spend money. This means no paid service, no purchase and no top-up. GitHub Actions on this public repository and itch.io uploads are free.
- Never print or store a token or key. `gh` and `butler` use their existing stored credentials (`butler` reads `~/Library/Application Support/itch/butler_creds` by itself). No code reads that file, sets `GH_TOKEN` or runs `gh auth token`.
- No force-push, no history rewrite, no `git push` at all, no merge, and no deletion of a published release or tag. The only deletion is `gh release delete <tag> --yes` of a release that `gh release view` shows `isDraft: true` at that moment, never with `--cleanup-tag`.
- Unity runs only through `UNITY_SLOTS=3 python3 -I ~/israel-game/.superpowers/coordinator-tools/unity_slots.py [--webgl] <clone>/unity -- uv run <clone>/tools/machine_lock.py --shared -- <Unity ...>`. Only the two `Build.Web` runs pass `--webgl`, and there is no `--wait-unity`. The headless boot measurement alone takes `machine_lock.py --exclusive --wait-unity`.
- Every Unity batch job runs with `UNITY_MCP_SERVER_PATH` set to an existing empty file (`<job>/no-mcp-server`). `-quit` goes on `-executeMethod` jobs (builds) only, never with `-runTests`. The warm-up has no `-quit`, because it exits by itself.
- Disk: at least 40 GB free (`shutil.disk_usage`) at preflight and again before each WebGL build, otherwise REFUSED. It never builds anyway.
- Unknown counts as fail: a missing file, an unparsable output, a timeout, a missing baseline or a `gh`/`butler` error is REFUSED, never a pass.
- Tests are pytest with fakes for git, gh, butler, Unity and HTTP. No test starts `git`, `gh`, `butler`, Unity or Chrome, or opens a network connection. The only real child process allowed in tests is `sys.executable` (the deadline test).
- Tools tests run in CI on ubuntu-24.04 with `uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q`. New modules use the stdlib only. Test modules that import a module using `fcntl` start with `pytest.importorskip("fcntl")`, as `test_unity_postmerge.py` does.
- Repo rules apply:
  - The PR adds `changelog.d/nightly-release.md`.
  - No em dashes anywhere (code, docs, commit messages).
  - No internal task ids or session names in committed text.
  - Agents never tick the PR template's licence attestation box; the owner does.
- Docs in the same PR:
  - `docs/RELEASING.md` gets a "Nightly builds" section.
  - `docs/m3-release.md:330-332` says the itch upload is automated for nightlies (the owner's 27 Oct choice).
  - The spec is committed as `docs/superpowers/specs/2026-10-27-nightly-release-design.md`.
  - This plan is committed as `docs/superpowers/plans/2026-10-27-nightly-release-plan.md`.
- itch pushes stay disabled (`itch_enabled: false` in the job's `config.json`) until `nightly.py savetest` passes. A pass flips that flag through `nightly_state.enable_itch`, which changes config and no code.
- Paths:
  - Job folder: `~/israel-game/.superpowers/nightly/` (ignored by git), holding `config.json`, `state.json`, `LOG.md`, `runs/`, `run.lock`, `launchd.log`, `nightly_launch.py` and `no-mcp-server`.
  - Clone: `~/.cache/statecraft/nightly/repo`.
- Version: `X.Y.Z-nightly.YYYYMMDD`, with X.Y.Z from `sim/Statecraft.Sim/SimInfo.cs` and the date the Asia/Jerusalem date at the start of the run. The tag is `v` + version. The zips are `statecraft-israel-v<version>-web.zip` and `statecraft-israel-v<version>-web-pages.zip`.

## Review Focus

- **launchd's environment:** PATH may lack a tool, or gh's keychain login may be unavailable. The expectation is a REFUSED line in LOG.md naming the step, never a crash with no record. Test `test_a_missing_tool_is_refused_and_recorded` (Task 2).
- **A second run on the same date:** this is a manual rerun after a REFUSED night, or after a PUBLISHED one with a newer `main`. The expectation:
  - A draft left by this job is deleted first.
  - An unknown draft or a published release with tonight's tag is REFUSED.
  - Nothing is ever overwritten.
  
  Test `test_same_day_rerun_cleans_its_draft_and_refuses_a_published_tag` (Task 2).
- **`main` moving during the run:** a merge at 04:00 must not change what ships. The tag targets the preflight commit, and the notes stop there. Test `test_release_targets_the_preflight_commit` (Task 6).
- **The deadline or a Unity timeout hitting mid-build:** the expectation is REFUSED, settings restored, no stamp left, and nothing published. Test `test_a_build_timeout_restores_settings_and_refuses` (Task 4).
- **A corrupt `state.json` or `config.json`:** this must fail closed (REFUSED), never act as a first night with no baseline and no last published commit. Test `test_a_corrupt_state_or_config_refuses` (Task 1).

---

## File Structure

| File | Responsibility |
|---|---|
| `tools/nightly_state.py` (create) | Config, state, version and tag naming, night/late-start/missed-day logic, LOG.md and HANDOFF lines, desktop notification, run lock, the `Refused`/`Skipped`/`Missed` outcomes |
| `tools/nightly_checks.py` (create) | Pure checks: check.sh, release gate and dotnet summaries; golden and smoke expects; web_check pass list; sizes and the PR size budget; boot parse and bound; zip notices; `productVersion` |
| `tools/nightly_publish.py` (create) | `GitHub` (draft, verify, publish, delete draft, previous build, merged PR bodies, site dispatch), `demo_serves`, `Itch` (push, status, fetch-compare), `release_notes`, `unzip_to` |
| `tools/nightly.py` (create) | `ProcRunner`, preflight, gates, EditMode, stamp + builds + packaging, checks wiring, publish order with rollback, `run` and `savetest` subcommands |
| `tools/nightly_launch.py` (create) | launchd entry point: run lock, clone fetch/reset/clean, first Library clone, then runs the clone's `tools/nightly.py run` |
| `tools/nightly.plist.in` (create) | launchd job, `StartCalendarInterval` 03:00 |
| `tools/web_check.py` (modify) | `--url`, `--profile`, `--first-save` for the itch save test |
| `tools/tests/conftest.py` (modify) | `FakeRunner` and the `no_processes` fixture |
| `tools/tests/test_nightly_state.py`, `test_nightly_checks.py`, `test_nightly_publish.py`, `test_nightly.py`, `test_nightly_launch.py` (create); `test_web_check.py` (modify) | Tests |
| `docs/RELEASING.md`, `docs/m3-release.md`, `tools/README.md`, `changelog.d/nightly-release.md`, `docs/superpowers/specs/…`, `docs/superpowers/plans/…` | Docs |

Shared types (defined in Task 1, used everywhere):

```python
class Stop(Exception):                 # outcome in {"REFUSED", "SKIPPED", "MISSED"}
    outcome: str; gate: str; detail: str
class Refused(Stop): ...               # Refused(gate: str, detail: str)
class Skipped(Stop): ...               # Skipped(detail: str), gate "-"
class Missed(Stop): ...                # Missed(detail: str), gate "-"

class Runner(Protocol):
    def run(self, cmd: list[str], *, cwd: Path, log: Path, deadline: float,
            env: dict[str, str] | None = None, holder: Path | None = None) -> int: ...
    def capture(self, cmd: list[str], *, cwd: Path, timeout: float = 300) -> str: ...   # raises Refused("command", ...) on non-zero
```

`deadline` is a `time.monotonic()` value. `run` returns 124 when it stopped the command at the deadline.

---

### Task 1: State, config, naming and the night's record

**Files:**
- Create: `tools/nightly_state.py`
- Modify: `tools/tests/conftest.py` (append `FakeRunner`, `no_processes`)
- Test: `tools/tests/test_nightly_state.py`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `Stop`, `Refused`, `Skipped`, `Missed`, `Runner` (above).
  - `TZ = ZoneInfo("Asia/Jerusalem")`; `JOB = Path.home() / "israel-game/.superpowers/nightly"`.
  - `@dataclass(frozen=True) class Config`. The fields and defaults are:
    - `main_repo=HOME/israel-game`
    - `clone=HOME/.cache/statecraft/nightly/repo`
    - `unity_slots=HOME/israel-game/.superpowers/coordinator-tools/unity_slots.py`
    - `exclusive_marker=HOME/israel-game/.superpowers/coordination/EXCLUSIVE-WINDOW`
    - `handoff=HOME/israel-game/.superpowers/HANDOFF.md`
    - `licence=HOME/Library/Unity/licenses/UnityEntitlementLicense.xml`
    - `repo_slug="eyalgolan/statecraft-israel"`
    - `site_demo_url="https://eyalgolan.github.io/statecraft-israel/demo/"`
    - `itch_target="eyalgolan/statecraft-israel:html5"`
    - `itch_page_url: str | None = None`
    - `itch_enabled=False`
    - `itch_evidence: str | None = None`
    - `butler=HOME/.local/bin/butler`
    - `min_free_gb=40`
    - `window_end="03:30"`
    - `deadline_hours=6.0`
    - `unity_slots_count=3`
  - `load_config(path: Path) -> Config`.
  - `enable_itch(path: Path, evidence: Path) -> None`.
  - `@dataclass class Published: tag: str; sha: str; version: str; sizes: dict[str, int]; boot_ms: float | None`.
  - `@dataclass class State: last_published: Published | None = None; boot_baseline: list[float] = []; last_run_day: str | None = None; open_draft: str | None = None; site_pending: str | None = None`.
  - `load_state(path: Path) -> State`; `save_state(path: Path, state: State) -> None`.
  - `base_version(siminfo_text: str) -> str`; `nightly_version(base: str, day: date) -> str`; `tag_for(version: str) -> str`.
  - `night_of(now: datetime) -> date`; `late_start(now: datetime, window_end: str) -> bool`; `missed_days(last_run_day: str | None, today: date) -> list[date]`.
  - `log_line(now: datetime, outcome: str, *, tag: str | None, sha: str | None, gate: str, log: Path | None) -> str`.
  - `record(log_md: Path, handoff: Path, line: str) -> None`.
  - `notify(title: str, message: str, run=subprocess.run) -> None`.
  - `run_lock(job: Path) -> ContextManager[bool]`.

- [ ] **Step 1: Write the failing tests**

```python
def test_version_and_tag_follow_the_spec():
    assert ns.base_version('public const string Version = "0.3.1";') == "0.3.1"
    assert ns.nightly_version("0.3.1", date(2026, 10, 28)) == "0.3.1-nightly.20261028"
    assert ns.tag_for("0.3.1-nightly.20261028") == "v0.3.1-nightly.20261028"
    with pytest.raises(ValueError): ns.base_version('Version = "0.3"')

def test_the_night_is_the_jerusalem_date():
    assert ns.night_of(datetime(2026, 10, 28, 0, 30, tzinfo=timezone.utc)) == date(2026, 10, 28)   # 02:30 IST
    assert ns.night_of(datetime(2026, 10, 27, 21, 59, tzinfo=timezone.utc)) == date(2026, 10, 27)

def test_a_start_after_0330_is_late():
    il = ns.TZ
    assert not ns.late_start(datetime(2026, 10, 28, 3, 0, tzinfo=il), "03:30")
    assert ns.late_start(datetime(2026, 10, 28, 3, 31, tzinfo=il), "03:30")

def test_missed_days_are_the_dates_between_runs():
    assert ns.missed_days("2026-10-25", date(2026, 10, 28)) == [date(2026, 10, 26), date(2026, 10, 27)]
    assert ns.missed_days(None, date(2026, 10, 28)) == []

def test_log_line_names_outcome_tag_sha_gate_and_log(tmp_path):
    line = ns.log_line(datetime(2026, 10, 28, 3, 0, tzinfo=ns.TZ), "REFUSED", tag="v0.3.1-nightly.20261028",
                       sha="0123456789abcdef0123", gate="editmode", log=tmp_path / "runs/x")
    assert line == f"2026-10-28 03:00 REFUSED tag=v0.3.1-nightly.20261028 sha=0123456789ab gate=editmode log={tmp_path}/runs/x"
    with pytest.raises(ValueError): ns.log_line(datetime.now(ns.TZ), "OK", tag=None, sha=None, gate="-", log=None)

def test_record_appends_to_log_and_handoff(tmp_path):
    ns.record(tmp_path / "LOG.md", tmp_path / "HANDOFF.md", "L1"); ns.record(tmp_path / "LOG.md", tmp_path / "HANDOFF.md", "L2")
    assert (tmp_path / "LOG.md").read_text().splitlines()[-2:] == ["- L1", "- L2"]
    assert (tmp_path / "HANDOFF.md").read_text().splitlines()[-1] == "- NIGHTLY: L2"

def test_state_round_trips_and_a_missing_file_is_empty(tmp_path): ...   # Published survives save/load; missing -> State()

def test_a_corrupt_state_or_config_refuses(tmp_path):                      # Review Focus
    (tmp_path / "state.json").write_text("{bad"); (tmp_path / "config.json").write_text('{"itch_enabled": "yes"}')
    with pytest.raises(ns.Refused) as e: ns.load_state(tmp_path / "state.json")
    assert e.value.gate == "state"
    with pytest.raises(ns.Refused): ns.load_config(tmp_path / "config.json")
    (tmp_path / "config.json").write_text('{"surprise": 1}')
    with pytest.raises(ns.Refused): ns.load_config(tmp_path / "config.json")

def test_only_enable_itch_flips_the_flag_and_keeps_other_keys(tmp_path):
    p = tmp_path / "config.json"; p.write_text('{"itch_page_url": "https://x.itch.io/y"}')
    assert ns.load_config(p).itch_enabled is False
    ns.enable_itch(p, tmp_path / "evidence.json")
    c = ns.load_config(p); assert c.itch_enabled and c.itch_page_url == "https://x.itch.io/y" and c.itch_evidence.endswith("evidence.json")

def test_notify_quotes_the_message_for_applescript():
    calls = []; ns.notify("Statecraft nightly", 'gate "x" \\ y', run=lambda a, **k: calls.append(a))
    assert calls[0][:2] == ["osascript", "-e"] and '\\"x\\"' in calls[0][2] and "\\\\ y" in calls[0][2]

def test_the_run_lock_is_exclusive(tmp_path):
    with ns.run_lock(tmp_path) as first:
        with ns.run_lock(tmp_path) as second:
            assert first is True and second is False
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_state.py -q`
Expected: FAIL, `ModuleNotFoundError: No module named 'nightly_state'`

- [ ] **Step 3: Implement `tools/nightly_state.py`, then add `FakeRunner` and `no_processes` to `conftest.py`**

`load_config` behaviour:
- A missing file gives the defaults.
- Unreadable JSON, an unknown key or a value whose type differs from the default's raises `Refused("config", ...)`.
- `Path` fields are read from strings with `~` expanded.

`load_state` behaviour:
- A missing file gives `State()`.
- Any parse or shape error raises `Refused("state", ...)`.
- `save_state` and `enable_itch` write atomically (a temp file, then `os.replace`, as `unity_postmerge.save_state` does).

`late_start` compares the local wall time with `window_end` on the same date. `record` opens each file in append mode and writes one line per call.

`notify` runs `osascript -e 'display notification "<msg>" with title "<title>"'`, with backslashes and double quotes escaped.

`run_lock` uses `fcntl.flock(LOCK_EX | LOCK_NB)` on `<job>/run.lock`.

Add to conftest:
- `FakeRunner(Runner)` keeps `rules: list[(prefix tuple, code, out, effect)]` added by `add(prefix, code=0, out="", effect=None)`. It records `calls`. `run` writes `out` to `log` and calls `effect(cmd)`. A call no rule matches raises `AssertionError(f"unexpected command {cmd}")`.
- `no_processes` (fixture) monkeypatches `subprocess.Popen`, `subprocess.run` and `urllib.request.urlopen` to raise.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_state.py -q`
Expected: `11 passed`

- [ ] **Step 5: Commit**

```bash
git add tools/nightly_state.py tools/tests/test_nightly_state.py tools/tests/conftest.py
git commit -m "nightly: state, config, naming and the nightly record"
```

---

### Task 2: Runner, preflight and the dedicated clone

`scripts/oss/release_gate.sh` refuses unless `git rev-parse --abbrev-ref HEAD` is `main`. `~/israel-game` itself has `main` checked out, so a worktree cannot hold `main`. The nightly therefore works in its own clone, whose `main` it alone moves.

**Files:**
- Create: `tools/nightly.py` (runner, preflight, `main` skeleton)
- Test: `tools/tests/test_nightly.py`

**Interfaces:**
- Consumes:
  - Task 1 (all).
  - `GitHub.release_state`, `GitHub.delete_draft`, `GitHub.dispatch_site` and `demo_serves` from Task 6. They are faked here, so this task does not wait for Task 6.
- Produces:
  - `class ProcRunner(Runner)`, constructed as `ProcRunner(grace: float = 60)`: `run` starts the command in its own process group with `env | {"STATECRAFT_LOCK_HOLDER": holder}` when `holder` is given. At the deadline it does the following:
    1. Signals the PID in the holder record (`unity_postmerge._holder`, `_parent` check) with SIGTERM, then SIGKILL after 60 s.
    2. Sends the top process SIGTERM, then SIGKILL after 60 s.
    3. Returns 124.
  - `@dataclass class Night: now: datetime; day: date; sha: str; version: str; tag: str; run_dir: Path; deadline: float; job: Path; cfg: Config; manifest: str`.
  - `preflight(cfg: Config, state: State, runner: Runner, gh: "GitHub", *, job: Path, now: datetime, manual: bool, free_gb: Callable[[Path], float]) -> Night`.
  - `main(argv: list[str] | None = None) -> int` with subcommands:
    - `run [--manual] [--no-publish]`
    - `savetest --tag TAG` (Task 8)
    
    Exit codes: 0 PUBLISHED/SKIPPED/MISSED/no-publish pass, 1 REFUSED, 2 usage.
    - `run_night(cfg, state_path: Path, job: Path, runner: Runner, gh, itch, *, now, manual, no_publish) -> str` (the outcome). In a `finally` it saves state and writes `record(...)` for every outcome except `--no-publish`. It calls `notify` on REFUSED. `KeyboardInterrupt` propagates after recording.

- [ ] **Step 1: Write the failing tests** (all with `no_processes`, `FakeRunner`, a fake `gh`)

```python
def test_an_unchanged_main_is_skipped(tmp_path): ...
    # state.last_published.sha == clone HEAD -> outcome "SKIPPED", LOG line "SKIPPED", no Unity/gh write call in runner.calls
def test_a_late_start_is_missed_and_earlier_gaps_are_reported(tmp_path): ...
    # now 09:10 IST, not manual -> "MISSED"; last_run_day 2026-10-25, today 10-28 -> LOG has MISSED lines for 10-26, 10-27, 10-28
def test_a_manual_run_ignores_the_window(tmp_path): ...
def test_low_disk_refuses_before_anything_runs(tmp_path):      # free_gb=39.9 -> Refused gate "disk"; runner.calls == clone checks only
def test_the_exclusive_window_marker_refuses(tmp_path): ...     # gate "exclusive-window"
def test_a_missing_licence_unity_editor_or_unity_slots_refuses(tmp_path): ...   # gates "unity-licence", "unity-editor", "unity-slots"
def test_the_clone_must_be_main_at_origin_main_and_clean(tmp_path): ...         # branch != main / HEAD != origin/main / dirty -> gate "clone"
def test_same_day_rerun_cleans_its_draft_and_refuses_a_published_tag(tmp_path): # Review Focus
    # state.open_draft == tonight's tag and gh says isDraft -> gh.delete_draft(tag) called, run continues
    # gh says the tag is a published release -> Refused gate "tag-exists"; an unknown draft with the tag -> Refused gate "tag-exists"
def test_a_missing_tool_is_refused_and_recorded(tmp_path):    # Review Focus
    # runner.capture raises FileNotFoundError("gh") -> outcome REFUSED, LOG line has gate=preflight, notify called once
def test_run_bounded_terminates_at_the_deadline(tmp_path):     # the one real child: sys.executable -c "time.sleep(60)"
    t = time.monotonic(); code = nightly.ProcRunner(grace=1).run([sys.executable, "-c", "import time; time.sleep(60)"],
        cwd=tmp_path, log=tmp_path / "l", deadline=time.monotonic() + 1)
    assert code == 124 and time.monotonic() - t < 10
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly.py -q`
Expected: FAIL, `ModuleNotFoundError: No module named 'nightly'`

- [ ] **Step 3: Implement `ProcRunner`, `preflight`, `run_night` (stages after preflight stubbed to `raise NotImplementedError`) and `main`**

`preflight` runs these checks in this order, each failure raising the named gate:
1. Late start: `late_start` and not `manual` raises `Missed`. Before any outcome is written, `missed_days` lines are recorded with outcome MISSED.
2. The clone: `git -C <clone> rev-parse --abbrev-ref HEAD` == `main`, `rev-parse HEAD` == `rev-parse refs/remotes/origin/main`, and `status --porcelain --untracked-files=no` is empty (gate `clone`).
3. Unchanged: `sha == state.last_published.sha` raises `Skipped("main unchanged since <tag>")`. Before skipping, retry the site when `state.site_pending` is set (Task 6 `GitHub.dispatch_site` + `demo_serves`).
4. `free_gb(clone) >= cfg.min_free_gb` (gate `disk`).
5. `cfg.exclusive_marker` is absent (gate `exclusive-window`).
6. `cfg.licence.is_file()`, checked with stat only and never read (gate `unity-licence`).
7. `unity_postmerge.unity_path(<m_EditorVersion>)` exists (gate `unity-editor`).
8. `cfg.unity_slots.is_file()` (gate `unity-slots`).
9. The tag check from the Review Focus test (gate `tag-exists`).

Then it builds `run_dir = job/"runs"/f"{now:%Y%m%d-%H%M%S}"`, sets `deadline = monotonic() + cfg.deadline_hours*3600`, and reads `manifest` from `sim/Statecraft.Sim.Tests/golden/manifest.txt` in the clone.

`run_night` converts `FileNotFoundError`, `OSError` and `subprocess.SubprocessError` raised in a stage into `Refused(<stage name>, str(e))`.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly.py -q`
Expected: `10 passed`

- [ ] **Step 5: Commit**

```bash
git add tools/nightly.py tools/tests/test_nightly.py
git commit -m "nightly: runner with a deadline, preflight in a dedicated clone"
```

---

### Task 3: Gates before building, and the full EditMode run

**Files:**
- Create: `tools/nightly_checks.py` (summary parsers)
- Modify: `tools/nightly.py` (`run_gates`, `unity_argv`, `unity_env`, `run_editmode`)
- Test: `tools/tests/test_nightly_checks.py`, `tools/tests/test_nightly.py`

**Interfaces:**
- Consumes: `Night`, `Runner`, `Refused`; from `unity_postmerge`: `parse_results(path) -> Results`, `passed(r, exit) -> bool`, `unity_path(version) -> str`, `WARM_UP`.
- Produces:
  - `check_sh_verdict(text: str, code: int) -> str | None`; `release_gate_verdict(text: str, code: int) -> str | None`; `dotnet_totals(text: str) -> tuple[int, int]` (failed, total) (in `nightly_checks.py`).
  - `unity_argv(night: Night, unity_args: list[str], *, webgl: bool) -> list[str]`; `unity_env(night: Night) -> dict[str, str]`.
  - `run_gates(night: Night, runner: Runner) -> None`; `run_editmode(night: Night, runner: Runner) -> None`; `restore_settings(night: Night, runner: Runner) -> None`.

- [ ] **Step 1: Write the failing tests**

```python
PASSED = "Passed!  - Failed:     0, Passed:  3216, Skipped:     0, Total:  3216, Duration: 1 m"   # format seen in real logs
def test_dotnet_totals_sum_every_project():
    assert nc.dotnet_totals(PASSED + "\n" + PASSED.replace("3216", "12")) == (0, 3228)
    assert nc.dotnet_totals("Failed!  - Failed:     5, Passed:  3211, Skipped:     0, Total:  3216") == (5, 3216)
    assert nc.dotnet_totals("Build succeeded.") == (0, 0)

def test_check_sh_must_pass_and_must_have_compiled_unity():
    ok = "== unity compile (dotnet, outside Unity)\n...\nALL CHECKS PASSED\n"
    assert nc.check_sh_verdict(ok, 0) is None
    assert "SKIPPED" in nc.check_sh_verdict("== unity compile: SKIPPED, no unity/Library ...\nALL CHECKS PASSED", 0)
    assert nc.check_sh_verdict(ok, 1) is not None

def test_release_gate_needs_its_ok_line_and_exit_0():
    assert nc.release_gate_verdict("...\n\nRELEASE GATE OK\n", 0) is None
    assert nc.release_gate_verdict("...\nRELEASE GATE FAILED\n", 1) is not None
    assert nc.release_gate_verdict("RELEASE GATE OK\n", 1) is not None

# tools/tests/test_nightly.py
def test_unity_goes_through_slots_and_the_shared_lock(night):
    argv = nightly.unity_argv(night, ["-runTests"], webgl=False)
    assert argv[:3] == ["python3", "-I", str(night.cfg.unity_slots)] and "--webgl" not in argv
    i = argv.index("--"); assert argv[i + 1:i + 6] == ["uv", "run", str(night.cfg.clone / "tools/machine_lock.py"), "--shared", "--"]
    assert "--wait-unity" not in argv and "-nographics" not in argv
    assert nightly.unity_argv(night, ["-quit"], webgl=True)[3] == "--webgl"
    env = nightly.unity_env(night); assert env["UNITY_SLOTS"] == "3" and Path(env["UNITY_MCP_SERVER_PATH"]).is_file()

def test_gates_run_in_order_with_both_locales(night, fake_runner): ...
    # order: build_sim_for_unity.py, bash scripts/check.sh origin/main, bash scripts/oss/release_gate.sh,
    # dotnet test sim/Statecraft.sln -c Release --nologo with LANG=LC_ALL=he_IL.UTF-8, then en_US.UTF-8
def test_a_locale_run_with_zero_tests_or_a_failure_refuses(night, fake_runner): ...   # gate "sim-he_IL" / "sim-en_US"
def test_editmode_needs_tests_and_no_failures_and_never_quits(night, fake_runner, tmp_path):
    # warm-up argv has "-executeMethod", WARM_UP and no "-quit"; test argv has "-runTests" and no "-quit";
    # results xml with total 0 -> Refused gate "editmode"; restore_settings called even when the runner raises
def test_the_exclusive_marker_appearing_before_a_unity_step_refuses(night, fake_runner): ...
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_checks.py tools/tests/test_nightly.py -q`
Expected: FAIL, `ModuleNotFoundError: No module named 'nightly_checks'`

- [ ] **Step 3: Implement**

`dotnet_totals` matches `(Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+\d+,\s+Skipped:\s+\d+,\s+Total:\s+(\d+)`.

`run_gates` runs each step with `runner.run(..., cwd=clone, log=run_dir/"<step>.log", deadline=min(night.deadline, now+limit))`, using these step limits in minutes:

| Step | Limit (min) |
|---|---|
| build_sim | 15 |
| check | 45 |
| release_gate | 60 |
| each locale | 30 |

It then reads the log and applies the matching verdict. Gate names are `build-sim`, `check`, `release-gate`, `sim-he_IL` and `sim-en_US`.

`unity_argv` is `python3 -I <unity_slots> [--webgl] <clone>/unity -- uv run <clone>/tools/machine_lock.py --shared -- <unity_path> -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath <clone>/unity <unity_args...>`.

`unity_env` sets `UNITY_SLOTS=str(cfg.unity_slots_count)` and touches `<job>/no-mcp-server`.

`run_editmode` does the following:
1. Re-checks the marker.
2. Runs the warm-up: `-buildTarget OSXUniversal -executeMethod <WARM_UP> -logFile <run_dir>/warmup.log`, limit 60 min.
3. Runs the tests: `-buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults <run_dir>/editmode.xml -logFile <run_dir>/editmode.log`, limit 150 min, with `holder=run_dir/"lock-holder"`.
4. Calls `restore_settings` (`git checkout -- unity/ProjectSettings unity/Assets/Settings`) in a `finally`.
5. Raises `Refused("editmode", ...)` unless `up.passed(up.parse_results(xml), code)`.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_checks.py tools/tests/test_nightly.py -q`
Expected: all pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add tools/nightly_checks.py tools/nightly.py tools/tests/test_nightly_checks.py tools/tests/test_nightly.py
git commit -m "nightly: check.sh, release gate, both locales and the full EditMode run"
```

---

### Task 4: Version stamp, the two WebGL builds and packaging

RELEASING.md step 5 runs `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset` after each build, which also removes the stamp. So the stamp is applied before each build and removed after it. The built `index.html` carries `productVersion: "<bundleVersion>"` (template `unity/Assets/WebGLTemplates/Statecraft/index.html:43`), which proves the stamp took effect. In-game, `Application.version` feeds `ReportLinks.Version` (the feedback presenter).

**Files:**
- Modify: `tools/nightly.py` (`stamp_bundle_version`, `build_web`, `package_zip`), `tools/nightly_checks.py` (`product_version`)
- Test: `tools/tests/test_nightly.py`, `tools/tests/test_nightly_checks.py`

**Interfaces:**
- Consumes: `unity_argv`, `unity_env`, `restore_settings` (Task 3).
- Produces:
  - `stamp_bundle_version(settings: Path, version: str) -> None`.
  - `product_version(index_html: str) -> str | None`.
  - `build_web(night: Night, runner: Runner, *, pages: bool, free_gb) -> Path`, which returns `<clone>/unity/Builds/Web-gzip` or `Web-pages`.
  - `package_zip(night: Night, runner: Runner, build: Path, pages: bool) -> Path`, which returns `<run_dir>/statecraft-israel-v<version>-web[-pages].zip`.
  - `assert_clean_tree(night: Night, runner: Runner) -> None`.

- [ ] **Step 1: Write the failing tests**

```python
def test_the_stamp_replaces_exactly_the_one_bundle_version(tmp_path):
    s = tmp_path / "ProjectSettings.asset"; s.write_text("  productName: X\n  bundleVersion: 0.3.1\n  other: 1\n")
    nightly.stamp_bundle_version(s, "0.3.1-nightly.20261028")
    assert s.read_text() == "  productName: X\n  bundleVersion: 0.3.1-nightly.20261028\n  other: 1\n"
    s.write_text("no version here\n")
    with pytest.raises(ValueError): nightly.stamp_bundle_version(s, "0.3.1-nightly.20261028")

def test_product_version_reads_the_built_page():
    assert nc.product_version('productName: "S",\n        productVersion: "0.3.1-nightly.20261028",') == "0.3.1-nightly.20261028"
    assert nc.product_version("<html></html>") is None

def test_builds_follow_releasing_step_5(night, fake_runner):
    # gzip argv tail: -quit -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression gzip -shareable -logFile <abs>
    # pages adds: -decompressionFallback -out Builds/Web-pages ; both have "--webgl" in the slots part
def test_the_stamp_is_applied_before_and_removed_after_each_build(night, fake_runner): ...
    # effect on the build call asserts ProjectSettings.asset holds the stamp; afterwards restore_settings ran
def test_a_build_page_without_tonights_version_refuses(night, fake_runner): ...   # gate "version-stamp"
def test_low_disk_before_the_second_build_refuses(night, fake_runner): ...         # free_gb 39 on the 2nd call -> gate "disk"
def test_a_build_timeout_restores_settings_and_refuses(night, fake_runner):        # Review Focus
    # runner returns 124 for the Build.Web call -> Refused gate "build-web"; restore_settings called; settings file unstamped
def test_packaging_needs_the_notices_line(night, fake_runner): ...
    # package_web.sh exit 0 without "OK: notices inside" -> Refused gate "package"
def test_the_tree_must_be_clean_after_the_builds(night, fake_runner): ...          # status --porcelain non-empty -> gate "dirty-tree"
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly.py tools/tests/test_nightly_checks.py -q`
Expected: FAIL on the new tests (`AttributeError: module 'nightly' has no attribute 'stamp_bundle_version'`).

- [ ] **Step 3: Implement**

`stamp_bundle_version` uses `re.subn(r"^(  bundleVersion: )\S+$", ..., flags=re.M)` and requires exactly one replacement.

`build_web` does the following:
1. Re-checks disk and the marker.
2. Stamps the version.
3. Runs Unity with `webgl=True`, limit 120 min, log `<run_dir>/unity-build-web-<gz|pages>.log` (absolute).
4. Calls `restore_settings` in a `finally`.
5. Requires exit 0 and `product_version(index.html) == night.version`.

`package_zip` runs `bash scripts/oss/package_web.sh <build> <zip>` in the clone (limit 20 min). It requires exit 0, and the log must contain `PACKAGED ` and `OK: notices inside`.

`assert_clean_tree` runs `git status --porcelain --untracked-files=no` and requires empty output.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly.py tools/tests/test_nightly_checks.py -q`
Expected: all pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add tools/nightly.py tools/nightly_checks.py tools/tests/test_nightly.py tools/tests/test_nightly_checks.py
git commit -m "nightly: stamped gzip and Pages WebGL builds, packaged as in RELEASING step 5"
```

---

### Task 5: No-regression checks (web_check passes, sizes with the PR budget, headless boot)

**Files:**
- Modify: `tools/nightly_checks.py`, `tools/nightly.py` (`run_web_checks`, `check_sizes`, `measure_boot`)
- Test: `tools/tests/test_nightly_checks.py`, `tools/tests/test_nightly.py`

**Interfaces:**
- Consumes: `package_release.build_files(build) -> list[str]`, `package_release.BUILD_FILE`; `GitHub.previous_build_tag`, `GitHub.download`, `GitHub.merged_pr_bodies` (Task 6, faked here).
- Produces (in `nightly_checks.py`):
  - `golden_expects(manifest: str) -> list[str]`; `smoke_expect(manifest: str) -> str`; `smoke_orders_expect(manifest: str) -> str`.
  - `web_check_passes(build: Path, manifest: str) -> list[list[str]]`.
  - `web_sizes(build: Path) -> dict[str, int]`; `web_sizes_from_zip(zip_path: Path) -> dict[str, int]`, both with keys `data`, `wasm`, `framework`.
  - `size_budget_bytes(pr_bodies: list[str]) -> int`; `size_verdict(old: dict[str, int], new: dict[str, int], budget: int, tolerance: float = 0.02) -> str | None`.
  - `boot_ms(web_check_output: str) -> int`; `boot_verdict(median_ms: float, baseline: list[float], nights: int = 5) -> tuple[str, str | None]`.
  - `zip_missing_notices(zip_path: Path) -> list[str]`.
- Produces (in `nightly.py`):
  - `run_web_checks(night: Night, runner: Runner, build: Path) -> None`.
  - `check_sizes(night: Night, gh, build: Path, previous_zip: Path, prev_sha: str | None) -> dict[str, int]`.
  - `measure_boot(night: Night, runner: Runner, build: Path) -> float`.

- [ ] **Step 1: Write the failing tests**

```python
MANIFEST = "# c\nhash1950 aa\nhash1952 bb\nsmoke5 cc\nsmoke-orders dd\n"
def test_expects_come_from_the_manifest():
    assert nc.golden_expects(MANIFEST) == ["GOLDEN OK hash1950 aa", "GOLDEN OK hash1952 bb", "GOLDEN OK smoke5 cc", "GOLDEN OK smoke-orders dd"]
    assert nc.smoke_expect(MANIFEST) == "SMOKE OK 5 turns 1948-10-15 cc"
    assert nc.smoke_orders_expect(MANIFEST) == "SMOKE ORDERS OK 1948-06-15 dd"
    with pytest.raises(ValueError): nc.smoke_expect("hash1950 aa\n")

def test_the_passes_are_releasing_step_5_plus_smoke_orders(tmp_path):
    p = nc.web_check_passes(tmp_path, MANIFEST)
    assert len(p) == 7 and p[0][1:3] == ["--query", "golden&smoke=5"] and p[1][1:3] == ["--query", "smoke-orders"]
    assert [x[x.index("--startscreen") + 1] for x in p if "--startscreen" in x] == ["new", "recording", "new", "recording"]
    assert sum("lang=he" in x for x in p) == 2 and "--persistence" in p[6]
    assert all(x[x.index("--stall") + 1] == "90" for x in p[2:])

def test_sizes_sum_data_wasm_framework_named_by_index_html(tmp_path): ...   # a stale Build/ file not in index.html is ignored
def test_zip_and_folder_sizes_agree(tmp_path): ...
def test_size_growth_within_2_percent_passes_and_beyond_refuses():
    old = {"data": 900, "wasm": 90, "framework": 10}
    assert nc.size_verdict(old, {"data": 920, "wasm": 90, "framework": 10}, 0) is None          # +20 = 2.0%
    assert "1021" in nc.size_verdict(old, {"data": 921, "wasm": 90, "framework": 10}, 0)        # +21 > 2%
    assert nc.size_verdict(old, {"data": 921, "wasm": 90, "framework": 10}, 1) is None          # budgeted

def test_the_budget_line_is_exact_and_summed():
    bodies = ["x\nNightly size budget: +1.5 MB\n", "Nightly size budget: +2 MB", "nightly size budget +9 MB", "Nightly size budget: -3 MB"]
    assert nc.size_budget_bytes(bodies) == 3_500_000

def test_boot_reads_screens_ready_and_needs_a_pass():
    out = "console: BOOT phase=screens ready at=8123 ms dt=40 ms\nconsole: GAME OK 1948-05-15 turn 0 hash ab\nPASS: saw GAME OK\n"
    assert nc.boot_ms(out) == 8123
    with pytest.raises(ValueError): nc.boot_ms(out.replace("PASS: saw GAME OK", "TIMEOUT: never saw GAME OK"))

def test_the_boot_bound_is_the_baseline_spread():
    assert nc.boot_verdict(9000, [8000, 8100]) == ("collecting", None)
    base = [8000, 8100, 8050, 8200, 8150]                     # max 8200, spread 200 -> limit 8400
    assert nc.boot_verdict(8400, base) == ("checked", None)
    assert "8401" in nc.boot_verdict(8401, base)[1]

def test_web_checks_run_under_the_shared_lock_and_all_must_pass(night, fake_runner): ...   # any exit != 0 or no "PASS:" -> gate "web-check"
def test_boot_takes_the_exclusive_lock_from_the_main_checkout_three_times(night, fake_runner):
    # each argv: uv run <clone>/tools/machine_lock.py --exclusive --wait-unity --timeout 2700 -- uv run <clone>/tools/web_check.py
    #   <abs build> --query bootlog --expect "GAME OK" --timeout 300 --stall 90 ; cwd == cfg.main_repo ; median of 3 returned
def test_sizes_refuse_without_a_baseline_zip(night, fake_gh): ...                 # gate "sizes"
def test_a_budget_is_looked_up_only_when_growth_exceeds_2_percent(night, fake_gh): ...
def test_a_truncated_pr_list_refuses(night, fake_gh): ...                         # merged_pr_bodies raises Refused("sizes", ...)
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_checks.py tools/tests/test_nightly.py -q`
Expected: FAIL, `AttributeError: module 'nightly_checks' has no attribute 'golden_expects'`

- [ ] **Step 3: Implement**

`golden_expects` mirrors the RELEASING step 5 loop: every non-comment manifest line gives `GOLDEN OK <name> <hash>`.

`web_check_passes` returns the argument lists after `web_check.py`:
1. `[build, --query, golden&smoke=5, *(--expect g for g in golden_expects), --expect smoke_expect]`
2. `[build, --query, smoke-orders, --expect smoke_orders_expect]`
3. The four `--startscreen` passes and `--persistence`, each with `--timeout 300 --stall 90`, exactly as in RELEASING step 5.

`run_web_checks` runs each pass for both builds as `uv run <clone>/tools/machine_lock.py --shared -- uv run <clone>/tools/web_check.py <pass>` (limit 10 min each), cwd = clone.

`web_sizes` picks the single `index.html`-named file ending `.data.gz`, `.wasm.gz` and `.framework.js.gz` (ValueError on zero or several of one kind). The `_from_zip` form reads the zip's `index.html` entry and its member sizes.

`size_budget_bytes` matches `^Nightly size budget: \+(\d+(?:\.\d+)?) MB\s*$` with `re.M`. MB is 1,000,000 bytes.

`size_verdict` refuses when `sum(new) - sum(old) > tolerance * sum(old) + budget`. The reason names the old and new totals and the budget.

`check_sizes` behaves as follows:
- The baseline is `web_sizes_from_zip(previous_zip)`, the `-web.zip` of the previous published build (Task 6's `previous_build_tag`). A missing zip raises `Refused("sizes", "no baseline")`.
- PR bodies are fetched only when growth exceeds 2%. The shas in `git rev-list <prev_sha>..<night.sha>` are passed to `gh.merged_pr_bodies`, and `prev_sha` comes from `state.last_published.sha` or `git rev-list -n 1 <previous tag>`.

`measure_boot` runs the exclusive argv from the test 3 times. `--timeout 2700` makes machine_lock give up after 45 minutes waiting for Unity, which is then REFUSED with gate `boot`. The step limit is 60 min, the cwd is the main checkout (so `machine_lock.worktrees()` sees every lane's editor), and the result is the median of `boot_ms`.

`boot_verdict` uses the spread from the spec: limit = max(baseline) + (max - min). It is "collecting" until 5 published nights have recorded a median.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_checks.py tools/tests/test_nightly.py -q`
Expected: all pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add tools/nightly_checks.py tools/nightly.py tools/tests/test_nightly_checks.py tools/tests/test_nightly.py
git commit -m "nightly: web checks on both builds, size budget against the last published build, headless boot"
```

---

### Task 6: GitHub draft-first publish, the site deploy and the /demo/ check

**Files:**
- Create: `tools/nightly_publish.py` (`GitHub`, `demo_serves`, `release_notes`, `unzip_to`)
- Modify: `tools/nightly.py` (`publish_night` without itch; wire stages into `run_night`)
- Test: `tools/tests/test_nightly_publish.py`, `tools/tests/test_nightly.py`

**Interfaces:**
- Consumes: `Runner`, `Refused`, `State`, `Published`, `zip_missing_notices`.
- Produces:
  - `release_notes(*, version: str, sha: str, since: str, subjects: list[str], sha256_lines: list[str]) -> str`.
  - `class GitHub(runner: Runner, slug: str, cwd: Path)`:
    - `release_state(tag) -> str | None`, returning `"draft"`, `"published"` or `None`
    - `previous_build_tag(state_tag: str | None) -> str`
    - `download(tag, pattern, dest: Path) -> Path`
    - `create_draft(tag, sha, notes: Path, assets: list[Path]) -> None`
    - `verify_draft(tag, local: list[Path], dest: Path) -> None`
    - `publish(tag, sha) -> None`
    - `delete_draft(tag) -> None`
    - `merged_pr_bodies(shas: set[str]) -> list[str]`
    - `dispatch_site(deadline: float) -> None`
  - `demo_serves(url: str, version: str, *, deadline: float, fetch=urllib.request.urlopen, sleep=time.sleep, now=time.monotonic) -> None`.
  - `unzip_to(zip_path: Path, dest: Path) -> Path`.
  - `publish_night(night, state, gh, itch, zips: tuple[Path, Path], sizes, boot) -> None` (in `nightly.py`; Task 7 adds the itch branch).

- [ ] **Step 1: Write the failing tests** (`FakeRunner` scripts gh's JSON)

```python
def test_the_draft_is_a_prerelease_never_latest_targeting_the_commit(fake_runner, tmp_path):
    gh.create_draft("v0.3.1-nightly.20261028", SHA, tmp_path / "notes.md", [WEB, PAGES])
    assert fake_runner.calls[-1] == ["gh", "release", "create", "v0.3.1-nightly.20261028", "--repo", SLUG, "--draft",
        "--prerelease", "--latest=false", "--target", SHA, "--title", "v0.3.1-nightly.20261028 (nightly)",
        "--notes-file", str(tmp_path / "notes.md"), str(WEB), str(PAGES)]

def test_verify_draft_compares_hashes_and_notices(fake_runner, tmp_path): ...
    # downloaded bytes differ -> Refused gate "github-draft"; a zip without LICENSE -> Refused; isDraft false -> Refused
def test_publish_checks_draft_off_prerelease_on_latest_unchanged_and_tag_at_sha(fake_runner): ...
    # gh release edit <tag> --draft=false ; view json; gh api repos/<slug>/releases/latest --jq .tag_name != tag;
    # git ls-remote origin refs/tags/<tag> == SHA ; any mismatch -> Refused gate "github-publish"
def test_delete_draft_refuses_a_published_release_and_never_cleans_tags(fake_runner):
    # view says isDraft false -> raises, and no "gh release delete" call is made; argv never contains "--cleanup-tag"
def test_previous_build_is_state_tag_else_newest_release_with_a_web_zip(fake_runner): ...
def test_merged_pr_bodies_filter_by_merge_commit_and_refuse_truncation(fake_runner): ...   # 500 rows -> Refused "sizes"
def test_site_dispatch_finds_the_new_run_and_watches_it(fake_runner): ...
    # gh workflow run site.yml --repo SLUG --ref main ; gh run list --repo SLUG --workflow site.yml --event workflow_dispatch
    #   --limit 5 --json databaseId,createdAt ; gh run watch <id> --repo SLUG --exit-status ; non-zero -> Refused gate "site"
def test_demo_serves_polls_until_the_version_appears():
    pages = iter([b'productVersion: "0.3.1"', b'productVersion: "0.3.1-nightly.20261028"'])
    gp.demo_serves(DEMO, "0.3.1-nightly.20261028", deadline=100, fetch=lambda u, timeout: io.BytesIO(next(pages)),
                   sleep=lambda s: None, now=iter(range(0, 1000, 30)).__next__)
def test_release_notes_list_commits_and_hashes_and_say_nightly(): ...
def test_release_targets_the_preflight_commit(night, fake_gh):                   # Review Focus
    # origin/main moves after preflight (fake ls-remote of main returns another sha): create_draft gets night.sha,
    # notes come from git log <prev>..<night.sha>
def test_a_failed_draft_check_deletes_the_draft_and_refuses(night, fake_gh): ...
def test_a_site_failure_after_publish_retries_once_then_refuses_and_marks_site_pending(night, fake_gh):
    # dispatch_site fails twice -> outcome REFUSED gate "site"; release stays published (no delete_draft call);
    # state.last_published is tonight (the release is public); state.site_pending == tonight's version
def test_a_published_night_updates_state_and_boot_baseline(night, fake_gh): ...
    # last_published = Published(tag, sha, version, sizes, boot_ms); boot_baseline appended while len < 5
def test_no_publish_stops_before_any_store_call(night, fake_gh, capsys): ...
    # no gh release/workflow or butler call; stdout has "NO-PUBLISH: all gates passed for v0.3.1-nightly.20261028";
    # LOG.md and HANDOFF untouched; state unchanged
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_publish.py tools/tests/test_nightly.py -q`
Expected: FAIL, `ModuleNotFoundError: No module named 'nightly_publish'`

- [ ] **Step 3: Implement `nightly_publish.py` and `publish_night`**

`release_notes` contains:
1. The line "Automated nightly build of `main` at <sha12>. A pre-release, not a versioned release."
2. `- <subject>` lines from `git log --format=%s <since>..<sha>`.
3. The `shasum -a 256` lines, as in RELEASING step 6.

`since` is the previous nightly's sha, or on the first night the previous build tag.

`verify_draft` works as follows:
1. `gh release view <tag> --repo <slug> --json isDraft,isPrerelease,assets` must show `isDraft` true, `isPrerelease` true, and asset names equal to the local names.
2. `gh release download <tag> --repo <slug> --dir <dest> --pattern "*.zip"` is run.
3. The SHA-256 of each downloaded file must equal its local file, and `zip_missing_notices` must be empty (RELEASING step 7's five notices).

`publish_night` order (itch added in Task 7):
1. Write the notes.
2. `create_draft`.
3. `verify_draft`. On failure: `delete_draft`, then REFUSED `github-draft`.
4. `publish`. On failure: `delete_draft` if `release_state` is still `"draft"`, then REFUSED `github-publish`.
5. Set `state.last_published`, and append to `boot_baseline` while it is shorter than 5.
6. `dispatch_site`, then `demo_serves(cfg.site_demo_url, version, deadline=now+15 min)`. On failure, run them once more; a second failure sets `state.site_pending` and is REFUSED `site`, with the notification saying the release is public and the demo is unchanged.

The outcome is PUBLISHED.

Wire the stages in `run_night`:

preflight → gates → editmode → build gz → build pages → assert_clean_tree → package both → web checks → sizes → boot → (no-publish? stop) → publish.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_publish.py tools/tests/test_nightly.py -q`
Expected: all pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add tools/nightly_publish.py tools/nightly.py tools/tests/test_nightly_publish.py tools/tests/test_nightly.py
git commit -m "nightly: draft-first GitHub pre-release, site deploy and /demo/ version check"
```

---

### Task 7: itch.io push with rollback (behind `itch_enabled`)

**Files:**
- Modify: `tools/nightly_publish.py` (`Itch`), `tools/nightly.py` (`publish_night` itch branch)
- Create: `tools/tests/data/butler_status_live.txt`, `tools/tests/data/butler_status_pending.txt`
- Test: `tools/tests/test_nightly_publish.py`, `tools/tests/test_nightly.py`

**Interfaces:**
- Consumes: `GitHub.download`, `unzip_to`, `Config.itch_*`, `Config.butler`.
- Produces:
  - `class Itch(runner: Runner, butler: Path, target: str)`, where target is `user/project:channel`:
    - `live_version(status_text: str) -> str | None` (static, parses this target's channel row)
    - `push(folder: Path, version: str) -> None`
    - `wait_live(version: str, deadline: float, sleep=time.sleep) -> None`
    - `matches(folder: Path, dest: Path) -> None`
    - `build_id(status_text: str) -> str | None`

- [ ] **Step 1: Capture the real `butler status` output as fixtures (read-only, done by hand once)**

Run: `~/.local/bin/butler status eyalgolan/statecraft-israel > tools/tests/data/butler_status_live.txt`
Expected: a table naming channel `html5` with its upload, build and version columns, and no token in the text (check with `grep -ciE 'key|token' tools/tests/data/butler_status_live.txt` printing `0`). If the project or channel does not exist yet, stop here. Itch stays disabled, and the owner creates the project (docs/m3-release.md steps 1-4) first. For the pending fixture, copy the live one and edit the build cell to the state butler prints while a build processes, as seen at the first real push. Until then, mark that test `xfail(strict=True, reason="pending fixture not captured")`.

- [ ] **Step 2: Write the failing tests**

```python
def test_live_version_reads_the_channel_row():
    text = (DATA / "butler_status_live.txt").read_text()
    assert gp.Itch.live_version(text) == "<the version in the captured row>"
def test_a_processing_build_is_not_live(): ...                       # pending fixture -> None
def test_push_uses_the_folder_target_and_userversion(fake_runner, tmp_path):
    gp.Itch(fake_runner, BUTLER, "eyalgolan/statecraft-israel:html5").push(tmp_path, "0.3.1-nightly.20261028")
    assert fake_runner.calls[-1] == [str(BUTLER), "push", str(tmp_path), "eyalgolan/statecraft-israel:html5",
                                     "--userversion", "0.3.1-nightly.20261028"]
def test_fetch_compare_refuses_any_difference(fake_runner, tmp_path): ...   # butler fetch <target> <dest>; path->sha256 maps differ -> Refused "itch"
def test_itch_disabled_is_logged_and_never_calls_butler(night, fake_gh): ...  # run log says "itch: skipped (save test not passed)"
def test_itch_rollback_target_is_ready_before_the_push(night, fake_gh):
    # previous -web.zip cannot be downloaded/unzipped -> Refused gate "itch-rollback" and butler push never called
def test_an_itch_failure_rolls_back_and_deletes_the_draft(night, fake_gh):
    # wait_live times out -> push(previous folder, previous version); delete_draft(tag); REFUSED gate "itch"
def test_a_github_publish_failure_after_itch_rolls_itch_back(night, fake_gh): ...
def test_no_butler_command_names_the_credentials_file():
    src = (TOOLS / "nightly_publish.py").read_text() + (TOOLS / "nightly.py").read_text()
    assert "butler_creds" not in src and "--identity" not in src and "auth token" not in src and "GH_TOKEN" not in src
```

- [ ] **Step 3: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_publish.py tools/tests/test_nightly.py -q`
Expected: FAIL, `AttributeError: module 'nightly_publish' has no attribute 'Itch'`

- [ ] **Step 4: Implement**

`live_version` splits the status table rows and finds the row whose channel cell is the target's channel. It returns its version cell, or `None` when that row's build cell shows the processing state or the row is missing. The parsing is written against the captured fixture, not guessed.

`wait_live` polls `butler status <user/project>` every 30 s until `live_version == version` or the deadline (15 min).

`matches` runs `butler fetch <target> <dest>` and compares the relative-path to SHA-256 maps of `dest` and the pushed folder.

The itch branch in `publish_night` sits between `verify_draft` and `publish`. When `cfg.itch_enabled` is true:
1. Download and unzip the previous build's `-web.zip`, with version `previous_tag[1:]`, as the rollback folder. On failure: REFUSED `itch-rollback`, nothing pushed.
2. Unzip tonight's `-web.zip`.
3. `push`, `wait_live`, `matches`. On failure: push the rollback folder, run its `wait_live`, `delete_draft`, then REFUSED `itch`.
4. A later failure in `publish` also pushes the rollback folder back.

After a successful `publish`, a site failure does not roll itch back. The build passed every gate, and only the Pages deploy failed.

- [ ] **Step 5: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_publish.py tools/tests/test_nightly.py -q`
Expected: all pass, 0 failed (one `xfail` until the pending fixture exists).

- [ ] **Step 6: Commit**

```bash
git add tools/nightly_publish.py tools/nightly.py tools/tests/test_nightly_publish.py tools/tests/test_nightly.py tools/tests/data/butler_status_live.txt tools/tests/data/butler_status_pending.txt
git commit -m "nightly: itch.io push behind a config flag, verified by status and fetch, with rollback"
```

---

### Task 8: The two-upload itch save test, and docs/m3-release.md

The test follows docs/m3-release.md:354-388 (unchanged content, same game-frame origin, different upload URL, full `date, turn, hash` tuple, two loads of B). IndexedDB is per origin, so it opens the itch game-frame URL directly in one kept Chrome profile.

**Files:**
- Modify: `tools/web_check.py` (`--url`, `--profile`, `--first-save`; `PersistenceCheck(first_save_only=...)`), `tools/nightly.py` (`savetest`), `tools/nightly_publish.py` (`itch_frame_url`), `docs/m3-release.md:330-332`, `docs/m3-release.md:354` (heading), `docs/m3-release.md:418`
- Create: `tools/tests/data/itch_page.html` (captured once, read-only)
- Test: `tools/tests/test_web_check.py`, `tools/tests/test_nightly.py`, `tools/tests/test_nightly_publish.py`

**Interfaces:**
- Consumes: `Itch.push`, `Itch.wait_live`, `Itch.build_id`, `GitHub.download`, `unzip_to`, `enable_itch`.
- Produces:
  - `web_check` CLI: `web_check.py [build] [--url URL] [--profile DIR] [--first-save] ...`. Exactly one of `build` and `--url` is required. With `--first-save`, it prints `PERSISTENCE saved <date> turn <n> hash <h>` and passes after the first durable commit.
  - `itch_frame_url(page_html: str) -> str | None`.
  - `savetest(cfg: Config, config_path: Path, job: Path, runner: Runner, gh: GitHub, itch: Itch, *, tag: str, fetch=urllib.request.urlopen) -> str`, returning `"PASS"`, `"FAIL"` or `"INCONCLUSIVE"`.

- [ ] **Step 1: Capture the itch page once (read-only)**

Run: `curl -s "<itch_page_url>" > tools/tests/data/itch_page.html && grep -c "html-classic.itch.zone" tools/tests/data/itch_page.html`
Expected: a count of 1 or more. A 0 (a draft or password page) means the page cannot be tested yet, so the save test stays INCONCLUSIVE and itch stays disabled. Report that to the owner and continue with the other steps using a hand-made fixture holding one escaped `data-iframe` placeholder.

- [ ] **Step 2: Write the failing tests**

```python
# test_web_check.py
def test_first_save_stops_after_the_first_durable_commit(capsys):
    # drive PersistenceCheck(first_save_only=True) with: GAME OK ... turn 0, TURN 1 1948-05-16 hash ab,
    # AUTOSAVE PENDING 1 1948-05-16 turn 1 hash ab, AUTOSAVE OK 1 1948-05-16 turn 1 hash ab
    # -> done True, no "reload" action sent, stdout has "PERSISTENCE saved 1948-05-16 turn 1 hash ab"
def test_url_and_build_are_exclusive_and_one_is_required(): ...   # parser errors on both / neither
def test_a_given_profile_is_kept(tmp_path): ...                   # the profile dir chosen by main() is --profile and is not removed (unit-level helper)

# test_nightly_publish.py
def test_the_frame_url_is_read_from_the_page():
    assert gp.itch_frame_url((DATA / "itch_page.html").read_text()).startswith("https://html-classic.itch.zone/html/")
    assert gp.itch_frame_url("<html>no game</html>") is None

# test_nightly.py
def test_savetest_passes_only_with_matching_tuple_new_url_same_origin(fake_runner, fake_gh, fake_itch, tmp_path):
    # push A -> frame A; web_check --url A --profile P --first-save -> tuple; push B (same folder) -> new build id, frame B
    # frame B != frame A and same scheme+host; web_check --url B --profile P --expect "GAME OK <d> turn <n> hash <h>" twice
    # -> "PASS", enable_itch called with the evidence file (both frame URLs, build ids, zip sha256, tuple, Chrome path)
def test_savetest_same_frame_url_is_inconclusive_and_keeps_itch_off(...): ...
def test_savetest_mismatch_fails_and_keeps_itch_off(...): ...
def test_savetest_needs_a_published_nightly_and_a_page_url(...): ...   # draft tag / no itch_page_url -> INCONCLUSIVE, no push
```

- [ ] **Step 3: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 --with websockets==15.0.1 pytest tools/tests/test_web_check.py tools/tests/test_nightly.py tools/tests/test_nightly_publish.py -q`
Expected: FAIL, `TypeError: PersistenceCheck.__init__() got an unexpected keyword argument 'first_save_only'`

- [ ] **Step 4: Implement**

`web_check.py`:
- `build` becomes `nargs="?"`.
- With `--url`, no server starts and the page opens that URL, with `page_query` appended using `&` when it already has a query.
- With `--profile`, that folder is used and not deleted in `finally`.
- `--first-save` implies the persistence driver with `first_save_only=True`. In `_reload_when_durable`, when that is set, it prints the saved line and sets `_done` instead of reloading.
- `res_error` and the viewport stay as they are.

`itch_frame_url` HTML-unescapes the page and returns the first `https://html-classic\.itch\.zone/html/\d+/[^"'&<>\s]*index\.html`.

`savetest` does the following:
1. Requires `cfg.itch_page_url` and `gh.release_state(tag) == "published"`, else INCONCLUSIVE.
2. Downloads and unzips the tag's `-web.zip`.
3. Pushes it as upload A and runs `wait_live`. It reads `build_id` and the frame URL, then runs the A first-save in a new profile under `<job>/savetest/<stamp>/profile`.
4. Pushes the same folder again as upload B and runs `wait_live`. It requires a new `build_id`, a different frame URL and the same origin, else INCONCLUSIVE.
5. Loads B twice expecting the `GAME OK` tuple, else FAIL.
6. Writes `evidence.json` and calls `enable_itch` on PASS.
7. Records `SAVETEST <result>` in LOG.md.

`docs/m3-release.md` edits:
- 330-332: the heading becomes "Upload to itch.io". The paragraph keeps steps 1-6 for the project's first setup (owner) and says nightly builds are now pushed with `butler` by `tools/nightly.py`, enabled only after `nightly.py savetest` passes (owner, 27 October 2026, "Public: website + itch"). It keeps "the build must pass the sensitivity gate above (`-shareable`)".
- 354: the heading drops "(the test is deferred to M4)". A line under the test plan says the automated test is `uv run tools/nightly.py savetest --tag <published nightly>` and that its result is recorded in the job's evidence file.
- Keep every phrase `tools/tests/test_cross_upload_save_plan.py` asserts, including "Result: not run on itch.io", until a real run's result is recorded in a later PR.
- 418: "Not uploaded to itch.io" says nightlies upload once the save test passes.

- [ ] **Step 5: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 --with websockets==15.0.1 pytest tools/tests/test_web_check.py tools/tests/test_nightly.py tools/tests/test_nightly_publish.py tools/tests/test_cross_upload_save_plan.py -q`
Expected: all pass, 0 failed.

- [ ] **Step 6: Commit**

```bash
git add tools/web_check.py tools/nightly.py tools/nightly_publish.py tools/tests/test_web_check.py tools/tests/test_nightly.py tools/tests/test_nightly_publish.py tools/tests/data/itch_page.html docs/m3-release.md
git commit -m "nightly: automated two-upload itch save test that enables itch pushes on a pass"
```

---

### Task 9: launchd job, launcher, install docs, RELEASING section, spec and changelog

**Files:**
- Create: `tools/nightly_launch.py`, `tools/nightly.plist.in`, `changelog.d/nightly-release.md`, `docs/superpowers/specs/2026-10-27-nightly-release-design.md`, `docs/superpowers/plans/2026-10-27-nightly-release-plan.md`
- Modify: `tools/README.md` (new "Nightly release" section after "Post-merge Unity run"; the expected test count line at :117), `docs/RELEASING.md` (new "## Nightly builds" section before "## Refreshing the README screenshots", :174)
- Test: `tools/tests/test_nightly_launch.py`

**Interfaces:**
- Consumes: `unity_postmerge.fill_plist(text, *, uv, repo, home, path) -> str`, `unity_postmerge.KEEP`; `nightly_state.run_lock`, `load_config`.
- Produces:
  - `nightly_launch.main(argv) -> int` with `--uv PATH --home JOB`.
  - `prepare_clone(cfg: Config, git=...) -> None`.
  - The `com.statecraft.nightly` launchd label.

- [ ] **Step 1: Write the failing tests**

```python
def test_the_plist_runs_at_0300_and_never_on_an_interval():
    filled = up.fill_plist((TOOLS / "nightly.plist.in").read_text(), uv="/opt/uv", repo="/r", home="/h", path="/usr/bin:/bin")
    p = plistlib.loads(filled.encode())
    assert p["Label"] == "com.statecraft.nightly" and p["StartCalendarInterval"] == {"Hour": 3, "Minute": 0}
    assert "StartInterval" not in p and not p.get("RunAtLoad") and "@" not in filled
    assert p["ProgramArguments"][:3] == ["/opt/uv", "run", "/h/nightly_launch.py"]

def test_prepare_clone_resets_only_the_clones_own_main_and_keeps_the_library(tmp_path):
    # fake git records: fetch --quiet --tags origin +refs/heads/main:refs/remotes/origin/main ; checkout --quiet main ;
    # reset --hard refs/remotes/origin/main ; clean -ffdx -e /unity/Library/ ... ; every call has cwd == cfg.clone,
    # none has cwd == cfg.main_repo except the one-time "cp -c -R <main_repo>/unity/Library <clone>/unity/Library"
def test_a_missing_clone_is_refused_and_recorded_not_cloned(tmp_path): ...  # LOG line "REFUSED ... gate=clone"
def test_a_run_in_progress_exits_0_and_touches_nothing(tmp_path): ...
def test_no_nightly_module_pushes_merges_or_deletes_tags():
    src = "".join((TOOLS / n).read_text() for n in ("nightly.py", "nightly_publish.py", "nightly_state.py", "nightly_checks.py", "nightly_launch.py"))
    for banned in ("git push", "\"push\", \"--force\"", "--cleanup-tag", "\"tag\", \"-d\"", "pr merge", "push --force"):
        assert banned not in src
```

- [ ] **Step 2: Run them to verify they fail**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_launch.py -q`
Expected: FAIL, `ModuleNotFoundError: No module named 'nightly_launch'`

- [ ] **Step 3: Implement the launcher and the plist**

The launcher does the following:
1. Holds `run_lock(home)`. When the lock is held, it prints "a run is in progress" and returns 0.
2. Runs `prepare_clone`:
   - If the clone is missing, it records a REFUSED line with gate `clone` (the install section creates the clone; the launcher never clones).
   - The first time, it APFS-clones the main checkout's Library.
3. Returns `subprocess.call([uv, "run", str(clone / "tools/nightly.py"), "run"])`. The clone's own script therefore runs, so a merged change takes effect on the next night.

The plist mirrors `tools/unity_postmerge.plist.in`, with `StartCalendarInterval` `{Hour 3, Minute 0}` in place of `StartInterval`, no `RunAtLoad`, and stdout/stderr to `@HOME@/launchd.log`.

- [ ] **Step 4: Run them to verify they pass**

Run: `uv run --with pytest==9.1.1 pytest tools/tests/test_nightly_launch.py -q`
Expected: `5 passed`

- [ ] **Step 5: Write the docs**

`tools/README.md` "Nightly release" section covers the following (each fact once, in the voice of the "Post-merge Unity run" section):
- What runs and when.
- The job folder and the clone.
- The install commands: `git clone https://github.com/eyalgolan/statecraft-israel.git ~/.cache/statecraft/nightly/repo`, `mkdir -p` the job folder, `cp tools/nightly_launch.py`, the `sed` fill with `@PATH@` including the folders of `uv`, `gh`, `dotnet`, `git` and `~/.local/bin`, then `plutil -lint` and `launchctl bootstrap gui/$(id -u) ...`.
- A first by-hand `uv run tools/nightly.py run --manual --no-publish` from the clone, which must print `NO-PUBLISH: all gates passed for v<version>`.
- Uninstall (`launchctl bootout gui/$(id -u)/com.statecraft.nightly`).
- How to reset the boot baseline (delete `boot_baseline` from `state.json`; the next 5 published nights re-measure it).
- The savetest command.

`docs/RELEASING.md` "## Nightly builds" covers the following:
- The schedule and what is public.
- The gates, by name and in order.
- The PR body line `Nightly size budget: +N MB` (MB = 1,000,000 bytes) that a PR adding download size must carry, or the next nightly is refused.
- The outcomes in `LOG.md`.
- That versioned releases (steps 1-7) are unchanged.
- That nightlies are never marked Latest and never deleted.

Update the `tools/README.md` expected test count to the number the full run in Step 6 prints.

Copy the spec to `docs/superpowers/specs/2026-10-27-nightly-release-design.md` with only these edits:
- The status line becomes "Approved by the owner on 27 October 2026 ("Approve as written"). Built from docs/superpowers/plans/2026-10-27-nightly-release-plan.md."
- The parenthetical after "need >= 40 GB free at launch", which names an internal file, becomes "(the coordinator's disk rule)".
- The session name after each "builder's call," is removed, leaving "(builder's call)".

Copy this plan to `docs/superpowers/plans/2026-10-27-nightly-release-plan.md`.

`changelog.d/nightly-release.md`:

```markdown
- Nightly builds: every night at 03:00 (Israel time) the newest `main` is built, checked against the build that is live
  and, only if every check passes, published as a GitHub pre-release that the website's demo serves (`docs/RELEASING.md`,
  "Nightly builds"). itch.io uploads follow once the automated save test passes.
```

Run: `grep -rnP "\x{2014}" tools/nightly*.py tools/nightly.plist.in docs/RELEASING.md docs/m3-release.md tools/README.md changelog.d/nightly-release.md docs/superpowers/specs/2026-10-27-nightly-release-design.md docs/superpowers/plans/2026-10-27-nightly-release-plan.md; grep -c "builder's call," docs/superpowers/specs/2026-10-27-nightly-release-design.md; grep -c "the coordinator's disk rule" docs/superpowers/specs/2026-10-27-nightly-release-design.md; diff "$APPROVED_SPEC" docs/superpowers/specs/2026-10-27-nightly-release-design.md`
Expected:
- The em dash grep prints nothing.
- Then `0`, then `1`.
- `diff` (with `APPROVED_SPEC` set to the approved copy of the spec, and echoed first) shows only the status line, the disk-rule line and the "builder's call" lines.

- [ ] **Step 6: Run the full checks on the branch and on its base, and compare**

Run (the branch, then `origin/main` in a scratch worktree): `bash scripts/check.sh origin/main`
Expected: `ALL CHECKS PASSED` on the branch. The tools tests line has the base's passed count plus the new tests, with the same skipped count. A failure that also appears on the base is PRE-EXISTING and is reported. A NEW failure blocks the PR.

- [ ] **Step 7: Commit**

```bash
git add tools/nightly_launch.py tools/nightly.plist.in tools/tests/test_nightly_launch.py tools/README.md docs/RELEASING.md changelog.d/nightly-release.md docs/superpowers/specs/2026-10-27-nightly-release-design.md docs/superpowers/plans/2026-10-27-nightly-release-plan.md
git commit -m "nightly: launchd job at 03:00, install and release docs, spec and changelog"
```

---

## After the PR merges (owner, not part of the PR)

1. Install with the `tools/README.md` steps, then run the `--manual --no-publish` trial in the clone. Each REFUSED gate it prints is fixed before bootstrapping launchd.
2. After the first PUBLISHED nightly, with `itch_page_url` set in `config.json`, run `uv run tools/nightly.py savetest --tag <that tag>` once.
