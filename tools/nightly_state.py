# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""State, configuration, naming and the nightly record for the nightly release job (tools/nightly*.py).

Everything the other nightly modules share lives here: the ways a night ends early (Stop and its subclasses), the
command runner they are given, the job's config and state files, how a night is named and dated, and the one-line
record of each night. Stdlib only. A bad state or config file refuses the night instead of falling back to defaults,
so a typo can never publish on a guess."""
from __future__ import annotations

import contextlib
import json
import math
import os
import re
import subprocess
from dataclasses import asdict, dataclass, field, fields
from datetime import date, datetime, time, timedelta
from pathlib import Path
from typing import ContextManager, Iterator, Protocol
from zoneinfo import ZoneInfo

TZ = ZoneInfo("Asia/Jerusalem")
HOME = Path.home()
JOB = HOME / "israel-game/.superpowers/nightly"
OUTCOMES = ("PUBLISHED", "SKIPPED", "REFUSED", "MISSED")
SHA_SHOWN = 12


class Stop(Exception):
    """A night that ends without publishing. `outcome` is REFUSED, SKIPPED or MISSED."""
    outcome = "REFUSED"

    def __init__(self, gate: str, detail: str):
        super().__init__(f"{gate}: {detail}")
        self.gate = gate
        self.detail = detail


class Refused(Stop):
    outcome = "REFUSED"

    def __init__(self, gate: str, detail: str):
        super().__init__(gate, detail)


class Skipped(Stop):
    outcome = "SKIPPED"

    def __init__(self, detail: str):
        super().__init__("-", detail)


class Missed(Stop):
    outcome = "MISSED"

    def __init__(self, detail: str):
        super().__init__("-", detail)


class Runner(Protocol):
    """Runs commands for the job. `deadline` is a time.monotonic() value; `run` returns 124 when it stopped the command
    at the deadline. `capture` raises Refused("command", ...) on a non-zero exit."""

    def run(self, cmd: list[str], *, cwd: Path, log: Path, deadline: float,
            env: dict[str, str] | None = None, holder: Path | None = None) -> int: ...

    def capture(self, cmd: list[str], *, cwd: Path, timeout: float = 300) -> str: ...


# --- config -----------------------------------------------------------------------------------------------------

@dataclass(frozen=True)
class Config:
    main_repo: Path = HOME / "israel-game"
    clone: Path = HOME / ".cache/statecraft/nightly/repo"
    unity_slots: Path = HOME / "israel-game/.superpowers/coordinator-tools/unity_slots.py"
    exclusive_marker: Path = HOME / "israel-game/.superpowers/coordination/EXCLUSIVE-WINDOW"
    handoff: Path = HOME / "israel-game/.superpowers/HANDOFF.md"
    licence: Path = HOME / "Library/Unity/licenses/UnityEntitlementLicense.xml"
    repo_slug: str = "eyalgolan/statecraft-israel"
    site_demo_url: str = "https://eyalgolan.github.io/statecraft-israel/demo/"
    itch_target: str = "eyalgolan/statecraft-israel:html5"
    itch_page_url: str | None = None
    savetest_itch_target: str | None = None   # a separate test page: the save test never pushes to the public one
    savetest_itch_page_url: str | None = None
    itch_enabled: bool = False
    itch_evidence: str | None = None
    butler: Path = HOME / ".local/bin/butler"
    min_free_gb: int = 40
    window_end: str = "03:30"
    deadline_hours: float = 6.0
    unity_slots_count: int = 3


def _atomic_write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(f"{path.name}.tmp{os.getpid()}")
    tmp.write_text(text, encoding="utf-8")
    os.replace(tmp, path)


def _read_json_object(path: Path, gate: str) -> dict:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise Refused(gate, f"{path} is unreadable: {e}") from e
    if not isinstance(data, dict):
        raise Refused(gate, f"{path} must hold a JSON object")
    return data


def load_config(path: Path) -> Config:
    if not path.exists():
        return Config()
    raw = _read_json_object(path, "config")
    known = {f.name: f for f in fields(Config)}
    values: dict[str, object] = {}
    for key, value in raw.items():
        if key not in known:
            raise Refused("config", f"unknown key {key!r} in {path}")
        default = known[key].default
        if isinstance(default, Path):
            ok = isinstance(value, str) and bool(value)
            values[key] = Path(value).expanduser() if ok else value
        elif default is None:   # an optional string
            ok = value is None or isinstance(value, str)
            values[key] = value
        elif isinstance(default, bool):
            ok = isinstance(value, bool)
            values[key] = value
        elif isinstance(default, float):
            ok = isinstance(value, (int, float)) and not isinstance(value, bool)
            values[key] = value
        elif isinstance(default, int):
            ok = isinstance(value, int) and not isinstance(value, bool)
            values[key] = value
        else:
            ok = isinstance(value, str)
            values[key] = value
        if not ok:
            raise Refused("config", f"{key!r} in {path} has the wrong type: {value!r}")
    return Config(**values)


def enable_itch(path: Path, evidence: Path) -> None:
    """Turn the itch push on and record the evidence file for it, keeping every other key."""
    load_config(path)   # refuses on a broken file instead of overwriting it
    raw = _read_json_object(path, "config") if path.exists() else {}
    raw["itch_enabled"] = True
    raw["itch_evidence"] = str(evidence)
    _atomic_write(path, json.dumps(raw, indent=2, sort_keys=True) + "\n")


# --- state ------------------------------------------------------------------------------------------------------

@dataclass
class Published:
    tag: str
    sha: str
    version: str
    sizes: dict[str, int]
    boot_ms: float | None


@dataclass
class State:
    last_published: Published | None = None
    boot_baseline: list[float] = field(default_factory=list)
    last_run_day: str | None = None
    open_draft: str | None = None
    site_pending: str | None = None


def _number(value: object) -> bool:
    if not isinstance(value, (int, float)) or isinstance(value, bool):
        return False
    try:
        return math.isfinite(value)
    except OverflowError:   # an integer too large for a float
        return False


def _opt_str(value: object, name: str) -> str | None:
    if value is not None and not isinstance(value, str):
        raise TypeError(f"{name} must be a string or null")
    return value


def _day(value: object) -> str | None:
    if value is not None:
        if not isinstance(value, str):
            raise TypeError("last_run_day must be a string or null")
        date.fromisoformat(value)   # ValueError (a bad date) is caught by load_state
    return value


def _state_from(raw: dict) -> State:
    extra = set(raw) - {f.name for f in fields(State)}
    if extra:
        raise TypeError(f"unknown keys {sorted(extra)}")
    published = None
    lp = raw.get("last_published")
    if lp is not None:
        if not isinstance(lp, dict) or set(lp) != {f.name for f in fields(Published)}:
            raise TypeError("last_published has the wrong shape")
        sizes = lp["sizes"]
        boot = lp["boot_ms"]
        if (not all(isinstance(lp[k], str) for k in ("tag", "sha", "version")) or not isinstance(sizes, dict)
                or not all(isinstance(k, str) and type(v) is int for k, v in sizes.items())
                or not (boot is None or _number(boot))):
            raise TypeError("last_published has the wrong types")
        published = Published(lp["tag"], lp["sha"], lp["version"], dict(sizes), boot)
    baseline = raw.get("boot_baseline", [])
    if not isinstance(baseline, list) or not all(_number(x) for x in baseline):
        raise TypeError("boot_baseline must be a list of numbers")
    return State(published, list(baseline), _day(raw.get("last_run_day")),
                 _opt_str(raw.get("open_draft"), "open_draft"), _opt_str(raw.get("site_pending"), "site_pending"))


def load_state(path: Path) -> State:
    if not path.exists():
        return State()
    raw = _read_json_object(path, "state")
    try:
        return _state_from(raw)
    except (TypeError, KeyError, ValueError) as e:
        raise Refused("state", f"{path} has a bad shape: {e}") from e


def save_state(path: Path, state: State) -> None:
    _atomic_write(path, json.dumps(asdict(state), indent=2, sort_keys=True) + "\n")


# --- naming and dates -------------------------------------------------------------------------------------------

def next_version(releases: list[dict]) -> str:
    """One patch above the numerically highest published exact vX.Y.Z tag, regardless of release type."""
    versions = []
    for row in releases:
        if (not isinstance(row, dict) or not isinstance(row.get("tag_name"), str)
                or not isinstance(row.get("draft"), bool)):
            raise Refused("version", "gh returned a release without a tag or draft flag")
        match = re.fullmatch(r"v([0-9]+)\.([0-9]+)\.([0-9]+)", row["tag_name"])
        if match and not row["draft"]:
            versions.append(tuple(int(part) for part in match.groups()))
    if not versions:
        raise Refused("version", "no published vX.Y.Z release to increment")
    major, minor, patch = max(versions)
    return f"{major}.{minor}.{patch + 1}"


def tag_for(version: str) -> str:
    return f"v{version}"


def night_of(now: datetime) -> date:
    """The night is the Jerusalem calendar date of the start."""
    return now.astimezone(TZ).date()


def late_start(now: datetime, window_end: str) -> bool:
    local = now.astimezone(TZ)
    return local.time().replace(tzinfo=None) > time.fromisoformat(window_end)


def missed_days(last_run_day: str | None, today: date) -> list[date]:
    """The dates strictly between the last run and today."""
    if last_run_day is None:
        return []
    last = date.fromisoformat(last_run_day)
    return [last + timedelta(days=i) for i in range(1, (today - last).days)]


# --- the night's record -----------------------------------------------------------------------------------------

def log_line(now: datetime, outcome: str, *, tag: str | None, sha: str | None, gate: str, log: Path | None) -> str:
    if outcome not in OUTCOMES:
        raise ValueError(f"outcome must be one of {OUTCOMES}, not {outcome!r}")
    stamp = now.astimezone(TZ).strftime("%Y-%m-%d %H:%M")
    short = sha[:SHA_SHOWN] if sha else "-"
    return f"{stamp} {outcome} tag={tag or '-'} sha={short} gate={gate} log={log if log else '-'}"


def _append_line(path: Path, line: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    lead = ""
    if path.exists() and path.stat().st_size:
        with path.open("rb") as f:
            f.seek(-1, os.SEEK_END)
            lead = "" if f.read(1) == b"\n" else "\n"
    with path.open("a", encoding="utf-8") as f:
        f.write(f"{lead}{line}\n")


def record(log_md: Path, handoff: Path, line: str) -> None:
    _append_line(log_md, f"- {line}")
    _append_line(handoff, f"- NIGHTLY: {line}")


def notify(title: str, message: str, run=None) -> None:
    run = run or subprocess.run   # looked up now, so a test that patches subprocess.run also covers the default
    def esc(s: str) -> str:
        return s.replace("\\", "\\\\").replace('"', '\\"')
    script = f'display notification "{esc(message)}" with title "{esc(title)}"'
    try:
        run(["osascript", "-e", script], check=False, capture_output=True, timeout=30)
    except (OSError, subprocess.SubprocessError):
        pass   # a missing notification never changes the night's outcome


@contextlib.contextmanager
def _run_lock(job: Path) -> Iterator[bool]:
    import fcntl   # POSIX only; imported here so the shared types load anywhere
    job.mkdir(parents=True, exist_ok=True)
    with (job / "run.lock").open("a") as fh:
        try:
            fcntl.flock(fh, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError:
            yield False
            return
        try:
            yield True
        finally:
            fcntl.flock(fh, fcntl.LOCK_UN)


def run_lock(job: Path) -> ContextManager[bool]:
    """Yields True when this process holds <job>/run.lock, False when another run already does."""
    return _run_lock(job)
