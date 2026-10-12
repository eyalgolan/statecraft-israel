#!/usr/bin/env bash
# The release gate: run on main before every release tag (docs/RELEASING.md). Read-only: it changes nothing locally or
# on GitHub. Exit 0 means the tip of main may be released; anything else names what is open.
set -uo pipefail
cd "$(dirname "$0")/../.."
fail=0
scan_out="$(mktemp)"
trap 'rm -f "$scan_out"' EXIT
step() { echo; echo "== $1"; }

step "on main, clean tree"
[ "$(git rev-parse --abbrev-ref HEAD)" = "main" ] || { echo "not on main"; fail=1; }
[ -z "$(git status --porcelain --untracked-files=no)" ] || { echo "tracked changes present"; fail=1; }

step "tip policy, release mode (open decisions D1-D12, owner-verify and unknown-licence rows refuse)"
uv run scripts/oss/check_repo.py --release || fail=1

step "policy tests"
uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q || fail=1

step "simulation and content tests"
dotnet test sim/Statecraft.sln -c Release --nologo -v q || fail=1

step "sensitivity gate on content/ (decision D11; Statecraft.Cli --release-gate)"
dotnet run --project sim/Statecraft.Cli -v q -- --release-gate --content "$PWD/content" || fail=1

step "tools tests"
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q || fail=1

step "history scan of main (information; rows in docs/oss/history-accepted.tsv are accepted)"
uv run scripts/oss/scan_history.py --ref main | tee "$scan_out"
if ! grep -q ' 0 open secret-shape findings' "$scan_out"; then
  echo "OPEN SECRET-SHAPE FINDING: rotate it at its provider (owner), then add the row to docs/oss/history-accepted.tsv"
  fail=1
fi

echo
if [ "$fail" -eq 0 ]; then echo "RELEASE GATE OK"; else echo "RELEASE GATE FAILED"; fi
exit "$fail"
