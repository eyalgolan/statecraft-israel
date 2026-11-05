#!/bin/zsh
# run.sh <tag>: #2649 Dakotas on 15 July (S-199s, flak, kill) captures IN PLAY on the e21-cap worktree HEAD (copy of e21-cap-2691 rig).
# Copy of the #2594 baseline rig (e21-2594/rig/run.sh) with paths moved; outputs logs-<tag>/ and raw-<tag>/.
set -u
TAG=${1:?tag}
WT=$HOME/israel-game-wt/e21-cap
S=/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-cap-2649
OUT=$S/logs-$TAG; rm -rf "$OUT"; mkdir -p "$OUT"
D=unity/Assets/Tests/EditMode
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
export UNITY_MCP_SERVER_PATH=/private/tmp/claude-501/-Users-eyalgolan/13dcc884-385c-4871-b80d-c77a529df6ce/scratchpad/gates/no-mcp-server
cd "$WT" || exit 1
H=$(date +%H%M); if [ "$H" -lt 0435 ] && [ "$H" -ge 0225 ]; then echo "REFUSED window $H" > "$OUT/status"; exit 9; fi
echo "start $(date '+%H:%M:%S') HEAD=$(git rev-parse HEAD) dirty=$(git status --porcelain | wc -l | tr -d ' ')" > "$OUT/status"
L() { uv run tools/machine_lock.py --shared -- "$U" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath "$WT/unity" -buildTarget OSXUniversal "$@"; }
uv run tools/machine_lock.py --shared -- uv run tools/build_sim_for_unity.py > "$OUT/build-sim.log" 2>&1; echo "build-sim exit $?" >> "$OUT/status"
L -executeMethod Statecraft.Game.EditorTools.UnityMcpGuard.WarmUp -logFile "$OUT/warmup.log" > "$OUT/warmup.out" 2>&1
echo "warmup exit $? ok=$(grep -c 'UnityMcpGuard warm-up: gate defines in place' "$OUT/warmup.log") $(date '+%H:%M:%S')" >> "$OUT/status"
cp "$S/E21Play2649.cs" "$D/"; FILTER="^E21Play2649\.English$"; [ "$TAG" = head ] && FILTER="$FILTER;^E21Play2649\.Hebrew$"
rm -rf .superpowers/e21-play-2649
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 L -runTests -testPlatform EditMode -testFilter "$FILTER" -testResults "$OUT/run.xml" -logFile "$OUT/run.log" -nostart > "$OUT/run.out" 2>&1
echo "run exit $? $(grep -o -m1 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"[^>]*skipped="[0-9]*"' "$OUT/run.xml" 2>/dev/null) $(date '+%H:%M:%S')" >> "$OUT/status"
grep -a -E "E21PLAY|error CS|Exception|AssertionException" "$OUT/run.log" > "$OUT/lines.txt"; wc -l < "$OUT/lines.txt" | sed "s/^/lines /" >> "$OUT/status"
rm -rf "$S/raw-$TAG"; mkdir -p "$S/raw-$TAG"
[ -d .superpowers/e21-play-2649 ] && mv .superpowers/e21-play-2649/* "$S/raw-$TAG/"
rm -rf .superpowers/e21-play-2649
rm -f "$D/E21Play2649.cs" "$D/E21Play2649.cs.meta"
git checkout -- unity/ProjectSettings unity/Assets/Settings unity/Assets/Map.unity "unity/Assets/Art/HexTiles/Props/arab_village_prickly_pear/prickly_pear-rock.mat" "unity/Assets/Art/HexTiles/Props/desert_edge_tent/tent-rock.mat" 2>>"$OUT/status"
echo "porcelain=$(git status --porcelain | wc -l | tr -d ' ') $(git status --porcelain | head -8 | tr '\n' ' ')" >> "$OUT/status"
echo DONE >> "$OUT/status"
