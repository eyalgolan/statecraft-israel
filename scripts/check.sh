#!/usr/bin/env bash
# One command for "does my change pass?": the same checks CI runs, in order, cheapest first. Needs dotnet and uv, not
# Unity (Unity edit-mode tests are not run by CI either; CONTRIBUTING.md, "What CI runs").
# Stops at the first failure, prints the last lines of that step's output and exits non-zero.
#
# Usage: scripts/check.sh [BASE_REF]      BASE_REF defaults to $CHECK_BASE, then origin/main.
# The changelog step compares BASE_REF...HEAD, so it sees committed changes only (as CI does). Set
# CHECK_NO_CHANGELOG=1 if the pull request carries the no-changelog label. CHECK_TAIL sets the lines shown (default 40).
set -uo pipefail
cd "$(dirname "$0")/.."
base="${1:-${CHECK_BASE:-origin/main}}"
tail_lines="${CHECK_TAIL:-40}"
export UV_PYTHON="${UV_PYTHON:-3.12}"   # the Python CI uses (UV_PYTHON in .github/workflows/ci.yml)
log="$(mktemp)"
trap 'rm -f "$log"' EXIT

for tool in dotnet uv; do
  command -v "$tool" >/dev/null || { echo "missing: $tool (see docs/dev-setup.md)"; exit 2; }
done
git rev-parse --verify --quiet "$base^{commit}" >/dev/null || { echo "base ref not found: $base (try: git fetch origin)"; exit 2; }

skip=false
[ "${CHECK_NO_CHANGELOG:-}" = 1 ] && skip=true

run() {
  local name="$1"; shift
  echo "== $name"
  if ! "$@" >"$log" 2>&1; then
    echo "FAILED: $name"
    echo "-- last $tail_lines lines of: $*"
    tail -n "$tail_lines" "$log"
    exit 1
  fi
  tail -n 3 "$log"
}

run "changelog (against $base)" uv run scripts/oss/changelog_check.py --base "$base" --skip-label "$skip"
run "reviewed_by guard (against $base)" uv run scripts/oss/reviewed_by_guard.py --base "$base"
run "gdd lint" uv run scripts/oss/gdd_lint.py
run "policy checker tests" uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q
run "repo policy" uv run scripts/oss/check_repo.py
run "rule ids" uv run scripts/oss/rule_ids.py
run "sim build, warnings as errors" dotnet build sim/Statecraft.sln -c Release --nologo -warnaserror
run "sim and content tests" dotnet test sim/Statecraft.sln -c Release --nologo
run "data tool tests" uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q

echo
echo "ALL CHECKS PASSED"
