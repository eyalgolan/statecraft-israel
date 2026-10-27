"""Parsers for the summaries the gates print (stdlib only). Each verdict is None for a pass, otherwise the reason.
Anything missing or unrecognised counts as a failure, never a pass."""
from __future__ import annotations

import re
import zipfile
from fractions import Fraction
from pathlib import Path

import package_release

DOTNET_RE = re.compile(r"(?:Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+\d+,\s+Skipped:\s+\d+,\s+Total:\s+(\d+)")
COMPILE_RAN = "== unity compile (dotnet, outside Unity)"


def dotnet_totals(text: str) -> tuple[int, int]:
    """(failed, total) summed over every test project's summary line."""
    failed = total = 0
    for m in DOTNET_RE.finditer(text):
        failed += int(m.group(1))
        total += int(m.group(2))
    return failed, total


def check_sh_verdict(text: str, code: int) -> str | None:
    if code != 0:
        return f"check.sh exited {code}"
    if "unity compile: SKIPPED" in text:
        return "check.sh SKIPPED the Unity compile, so the gate did not run"
    if COMPILE_RAN not in text:
        return "check.sh never ran the Unity compile"
    if "ALL CHECKS PASSED" not in text:
        return "check.sh did not print ALL CHECKS PASSED"
    return None


def release_gate_verdict(text: str, code: int) -> str | None:
    if code != 0:
        return f"release_gate.sh exited {code}"
    if "RELEASE GATE OK" not in text:
        return "release_gate.sh did not print RELEASE GATE OK"
    return None


PRODUCT_VERSION_RE = re.compile(r'productVersion:\s*"([^"]*)"')


def product_version(index_html: str) -> str | None:
    """The version the WebGL template printed into a built index.html, or None when the page has none."""
    m = PRODUCT_VERSION_RE.search(index_html)
    return m.group(1) if m else None


# --- the web checks of RELEASING step 5 ---------------------------------------------------------------------------

def _manifest_goldens(manifest: str) -> list[tuple[str, str]]:
    """(name, hash) of every non-comment manifest line, as the RELEASING step 5 loop reads them."""
    rows = []
    for line in manifest.splitlines():
        parts = line.split()
        if parts and not parts[0].startswith("#"):
            rows.append((parts[0], parts[1] if len(parts) > 1 else ""))
    return rows


def _golden_hash(manifest: str, name: str) -> str:
    for n, h in _manifest_goldens(manifest):
        if n == name and h:
            return h
    raise ValueError(f"the golden manifest has no {name} line")


def golden_expects(manifest: str) -> list[str]:
    return [f"GOLDEN OK {n} {h}" for n, h in _manifest_goldens(manifest)]


def smoke_expect(manifest: str) -> str:
    return f"SMOKE OK 5 turns 1948-10-15 {_golden_hash(manifest, 'smoke5')}"


def smoke_orders_expect(manifest: str) -> str:
    return f"SMOKE ORDERS OK 1948-06-15 {_golden_hash(manifest, 'smoke-orders')}"


def web_check_passes(build: Path, manifest: str) -> list[list[str]]:
    """The arguments after web_check.py for every pass of RELEASING step 5, plus the smoke-orders golden."""
    b = str(build)
    steady = ["--timeout", "300", "--stall", "90"]
    golden = [b, "--query", "golden&smoke=5"]
    for g in golden_expects(manifest):
        golden += ["--expect", g]
    golden += ["--expect", smoke_expect(manifest)]
    return [golden,
            [b, "--query", "smoke-orders", "--expect", smoke_orders_expect(manifest)],
            [b, "--startscreen", "new", *steady],
            [b, "--startscreen", "recording", *steady],
            [b, "--startscreen", "new", "--query", "lang=he", *steady],
            [b, "--startscreen", "recording", "--query", "lang=he", *steady],
            [b, "--persistence", *steady]]


# --- download size ---------------------------------------------------------------------------------------------------

SIZE_KINDS = {"data": ".data.gz", "wasm": ".wasm.gz", "framework": ".framework.js.gz"}
BUDGET_RE = re.compile(r"^Nightly size budget: \+(\d+(?:\.\d+)?) MB\s*$", re.M)


def _pick_sizes(names: list[str], size_of) -> dict[str, int]:
    out = {}
    for kind, suffix in SIZE_KINDS.items():
        found = [n for n in names if n.endswith(suffix)]
        if len(found) != 1:
            raise ValueError(f"index.html names {len(found)} {suffix} files, expected exactly one")
        out[kind] = size_of(found[0])
    return out


def web_sizes(build: Path) -> dict[str, int]:
    """Bytes of the data, wasm and framework files the build's index.html names (a stale Build/ file is ignored)."""
    def size_of(name: str) -> int:
        f = build / "Build" / name
        if not f.is_file():
            raise ValueError(f"index.html names Build/{name}, which is missing")
        return f.stat().st_size
    return _pick_sizes(package_release.build_files(build), size_of)


def web_sizes_from_zip(zip_path: Path) -> dict[str, int]:
    with zipfile.ZipFile(zip_path) as z:
        try:
            page = z.read("index.html").decode("utf-8", errors="replace")
        except KeyError as e:
            raise ValueError(f"{zip_path} has no index.html") from e
        names = sorted(set(package_release.BUILD_FILE.findall(page)))

        def size_of(name: str) -> int:
            try:
                return z.getinfo(f"Build/{name}").file_size
            except KeyError as e:
                raise ValueError(f"index.html names Build/{name}, which {zip_path} lacks") from e
        return _pick_sizes(names, size_of)


def size_budget_bytes(pr_bodies: list[str]) -> int:
    """The sum of every `Nightly size budget: +N MB` line (MB = 1,000,000 bytes), in integers."""
    total = Fraction(0)
    for body in pr_bodies:
        for m in BUDGET_RE.finditer(body):
            total += Fraction(m.group(1)) * 1_000_000
    return int(total)


def size_verdict(old: dict[str, int], new: dict[str, int], budget: int, tolerance: float = 0.02) -> str | None:
    """None when the growth is within tolerance plus the budget. Integer and Fraction math only: byte totals are
    never converted to float, and the tolerance is read from its decimal text, so +2.0% is exact."""
    old_total, new_total = sum(old.values()), sum(new.values())
    allowed = Fraction(str(tolerance)) * old_total + budget
    if new_total - old_total > allowed:
        return (f"download size grew from {old_total} to {new_total} bytes, more than {tolerance:.0%} "
                f"plus the {budget} byte budget the merged PRs record")
    return None


# --- headless boot ---------------------------------------------------------------------------------------------------

BOOT_RE = re.compile(r"^(?:console: )?BOOT phase=screens ready at=(\d+) ms\b", re.M)


def boot_ms(web_check_output: str) -> int:
    """The `screens ready` boot phase (DiagLog.BootPhase, via ?bootlog), which web_check.py echoes as `console: ...`.
    A run without web_check's PASS line, or with a TIMEOUT or FAIL line, is not a measurement."""
    if not re.search(r"^PASS:", web_check_output, re.M) or re.search(r"^(TIMEOUT|FAIL|STALLED|BROWSER EXITED)",
                                                                   web_check_output, re.M):
        raise ValueError("web_check did not pass, so the boot time is not a measurement")
    m = BOOT_RE.search(web_check_output)
    if not m:
        raise ValueError("web_check printed no BOOT phase=screens ready line")
    return int(m.group(1))


def boot_verdict(median_ms: float, baseline: list[float], nights: int = 5) -> tuple[str, str | None]:
    """("collecting", None) until `nights` baseline medians exist; then the bound is max + (max - min) of them."""
    if len(baseline) < nights:
        return "collecting", None
    recent = baseline[-nights:]
    top, spread = max(recent), max(recent) - min(recent)
    limit = top + spread
    if median_ms > limit:
        return "checked", f"headless boot took {median_ms:.0f} ms, over the {limit:.0f} ms bound (max {top:.0f} + spread {spread:.0f})"
    return "checked", None


# --- the published zip -----------------------------------------------------------------------------------------------

NOTICES = ("LICENSE", "NOTICE", "THIRD-PARTY-NOTICES.md", "OFL-EBGaramond.txt", "OFL-FrankRuhlLibre.txt")


def zip_missing_notices(zip_path: Path) -> list[str]:
    """The notices of RELEASING step 7 that a downloaded zip lacks (all five when it cannot be read)."""
    try:
        with zipfile.ZipFile(zip_path) as z:
            names = set(z.namelist())
    except (OSError, zipfile.BadZipFile):
        return list(NOTICES)
    return [n for n in NOTICES if n not in names]
