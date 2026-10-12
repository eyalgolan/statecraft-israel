#!/usr/bin/env bash
# Owner art checkpoint (non-blocking): a WebGL build (skipped with --no-build when the task has just built), screenshots at
# the promo poses from the player itself (?shot: the web tier with its UI and labels), editor renders of both quality
# tiers, and side-by-side images against the promo with colour-blind simulations. Re-checks both golden hashes in the same
# player run and fails if a screenshot is missing or blank. The art log lives in the main checkout's captures/ so every
# worktree appends to one history. --units adds the unit art slice (docs/units.md) to both the WebGL shots (&units) and the
# editor renders (-units), and its art-log rows are named <checkpoint-name>-units, so they cannot be mistaken for plain
# checkpoints; without it the captures stay comparable across tracks. --borderproof does the same for the region borders'
# synthetic, labelled proof pattern (rows named <checkpoint-name>-borderproof; docs/visual-v1.md, "Region borders"): only
# the editor renders (-borderproof) show it, since a release WebGL build ignores &borderproof. Usage:
# tools/art_checkpoint.sh [--no-build] [--units] [--borderproof] <checkpoint-name>
set -euo pipefail
build=1; units_query=""; units_arg=(); proof_query=""; proof_arg=()
while [[ "${1:-}" == --* ]]; do
  case "$1" in
    --no-build) build=0 ;;
    --units) units_query="&units"; units_arg=(-units) ;;
    --borderproof) proof_query="&borderproof"; proof_arg=(-borderproof) ;;
    *) echo "art_checkpoint: unknown option $1" >&2; exit 2 ;;
  esac
  shift
done
name="${1:?usage: tools/art_checkpoint.sh [--no-build] [--units] [--borderproof] <checkpoint-name>}"
root="$(git rev-parse --show-toplevel)"
cd "$root"
main_root="$(cd "$(git rev-parse --path-format=absolute --git-common-dir)/.." && pwd)"
: "${UNITY:?set UNITY as in docs/dev-setup.md}"
shared() { uv run tools/machine_lock.py --shared -- "$@"; }
# The build rewrites URP and player settings, and the editor capture may write QualitySettings' m_CurrentQuality; this
# script restores them from the index, so it refuses to run while any of them has unstaged changes (review M4: never
# discard an edit silently) and restores them on every exit, a failed build included (review M3).
guarded=(unity/ProjectSettings/QualitySettings.asset)
[[ $build == 1 ]] && guarded+=(unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset)
if ! git diff --quiet -- "${guarded[@]}"; then
  echo "art_checkpoint: stage your changes to ${guarded[*]} first (this script restores them from the index)" >&2
  exit 2
fi
trap 'git checkout -- "${guarded[@]}"' EXIT
if [[ $build == 1 ]]; then
  shared "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$root/unity-build-web-art.log"
  git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
  if grep -n "Shader error" "$root/unity-build-web-art.log"; then echo "art_checkpoint: shader errors in the WebGL build" >&2; exit 1; fi
fi
[[ -f unity/Builds/Web-disabled/index.html ]] || { echo "art_checkpoint: no build at unity/Builds/Web-disabled (run without --no-build)" >&2; exit 2; }
out="captures/visual/$name"
rm -rf "$out"; mkdir -p "$out"
uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&shot${units_query}${proof_query}" --expect "GOLDEN OK 1950 4304d8ec14493b3963758957c2bb07fd9166e54e1b3ea36e5565280db5647681" --expect "GOLDEN OK 1952 082f9af406a6ffa445421617e327679ea094f4c1c056871dd18c54f58beef6e0" --expect "SHOT done" --shots "$out" --stall 180 \
  --fail 'GOLDEN FAIL|SMOKE FAIL|BOOT FAIL|Statecraft/.*(not supported|Unsupported)|(not supported|Unsupported).*Statecraft/'
# The poses come from PromoPose.All's source, so a new pose is checked without editing this script (review M3).
poses=$(grep -o 'new PromoPose("[A-Za-z0-9_]*"' unity/Assets/Scripts/Map/PromoPose.cs | cut -d'"' -f2)
[[ -n "$poses" ]] || { echo "art_checkpoint: no PromoPose found in PromoPose.cs" >&2; exit 1; }
for pose in $poses; do
  [[ -s "$out/$pose.png" ]] || { echo "art_checkpoint: $out/$pose.png is missing" >&2; exit 1; }
done
shared "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget OSXUniversal -executeMethod MapCapture.Promo -captureDir "$root/$out/editor" ${units_arg[@]+"${units_arg[@]}"} ${proof_arg[@]+"${proof_arg[@]}"} -logFile "$root/unity-capture-promo.log"
git checkout -- unity/ProjectSettings/QualitySettings.asset   # SetQualityLevel may have written m_CurrentQuality (no unstaged edits: checked above)
uv run tools/art_compare.py "$out" "$name${units_query:+-units}${proof_query:+-borderproof}" --log "$main_root/captures/visual/art-log.tsv"
echo "ART CHECKPOINT $name: $(ls "$out"/*-compare*.png "$out"/editor/*-compare*.png | tr '\n' ' ')"
