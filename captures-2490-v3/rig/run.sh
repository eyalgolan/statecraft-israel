#!/bin/zsh
# run.sh <before-sha> <after-sha>: #2490 owner captures v3, IN PLAY (EnterPlayMode, -nostart, no -shot). Under gate_serial.py only.
set -u
BEFORE=$1; AFTER=$2
WT=$HOME/israel-game-wt/e21-dakota3
S=/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-2490v3
OUT=$S/logs; mkdir -p "$OUT"
D=unity/Assets/Tests/EditMode
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
export UNITY_MCP_SERVER_PATH=/private/tmp/claude-501/-Users-eyalgolan/13dcc884-385c-4871-b80d-c77a529df6ce/scratchpad/gates/no-mcp-server
echo "BEFORE=$BEFORE AFTER=$AFTER WT=$WT OUT=$OUT"
cd "$WT" || exit 1
echo "start $(date '+%H:%M:%S') HEAD=$(git rev-parse --short HEAD) dirty=$(git status --porcelain | wc -l | tr -d ' ')" > "$OUT/status"
L() { uv run tools/machine_lock.py --shared -- "$U" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath "$WT/unity" -buildTarget OSXUniversal "$@"; }
warmed=0
for side in before after; do
  if [ $side = before ]; then SHA=$BEFORE; FILTER='^E21Play2490\.English$;^E21Play2490\.Hebrew$'; else SHA=$AFTER; FILTER='^E21Play2490\.Hebrew$;^E21Play2490\.EnglishFull$'; fi
  git checkout -q --detach "$SHA" 2>>"$OUT/status"
  echo "$side HEAD=$(git rev-parse HEAD) filter=$FILTER $(date '+%H:%M:%S')" >> "$OUT/status"
  uv run tools/machine_lock.py --shared -- uv run tools/build_sim_for_unity.py > "$OUT/$side-build-sim.log" 2>&1; echo "$side build-sim exit $?" >> "$OUT/status"
  if [ $warmed = 0 ]; then
    L -executeMethod Statecraft.Game.EditorTools.UnityMcpGuard.WarmUp -logFile "$OUT/warmup.log" > "$OUT/warmup.out" 2>&1
    echo "warmup exit $? ok=$(grep -c 'UnityMcpGuard warm-up: gate defines in place' "$OUT/warmup.log")" >> "$OUT/status"; warmed=1
  fi
  cp "$S/rig/E21Play2490.cs" "$D/"
  rm -rf .superpowers/e21-play-2490
  LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 L -runTests -testPlatform EditMode -testFilter "$FILTER" -testResults "$OUT/$side.xml" -logFile "$OUT/$side.log" -nostart > "$OUT/$side.out" 2>&1
  echo "$side exit $? $(grep -o -m1 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"[^>]*skipped="[0-9]*"' "$OUT/$side.xml" 2>/dev/null) $(date '+%H:%M:%S')" >> "$OUT/status"
  grep -a -E "E21PLAY|error CS|Exception|AssertionException" "$OUT/$side.log" > "$OUT/$side-lines.txt"; wc -l < "$OUT/$side-lines.txt" | sed "s/^/$side lines /" >> "$OUT/status"
  rm -rf "$S/raw-$side"; mkdir -p "$S/raw-$side"
  [ -d .superpowers/e21-play-2490 ] && mv .superpowers/e21-play-2490/* "$S/raw-$side/"
  rm -rf .superpowers/e21-play-2490
  rm -f "$D/E21Play2490.cs" "$D/E21Play2490.cs.meta"
  git checkout -- unity/ProjectSettings unity/Assets/Settings unity/Assets/Map.unity "unity/Assets/Art/HexTiles/Props/arab_village_prickly_pear/prickly_pear-rock.mat" 2>>"$OUT/status"
  echo "$side porcelain=$(git status --porcelain | wc -l | tr -d ' ') $(git status --porcelain | head -5 | tr '\n' ' ')" >> "$OUT/status"
done
rmdir .superpowers/civ-1398-shots 2>/dev/null
echo "final HEAD=$(git rev-parse --short HEAD) porcelain=$(git status --porcelain | wc -l | tr -d ' ')" >> "$OUT/status"
echo DONE >> "$OUT/status"
