#!/bin/zsh
# run.sh: #2490 owner captures. before = main 1957078d3 (labels), after = H6' tree 7294073ac (labels + shadow). Under gate_slots.py only.
set -u
WT=$HOME/israel-game-wt/e21-dakota
S=/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-2490
OUT=$S/logs; mkdir -p "$OUT"
BEFORE=1957078d3; AFTER=7294073acf880654392ea86875c1f902aa21fd9a
D=unity/Assets/Tests/EditMode
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
export UNITY_MCP_SERVER_PATH=/private/tmp/claude-501/-Users-eyalgolan/13dcc884-385c-4871-b80d-c77a529df6ce/scratchpad/gates/no-mcp-server
cd "$WT"
echo "start $(date '+%H:%M') HEAD=$(git rev-parse --short HEAD) dirty=$(git status --porcelain | wc -l | tr -d ' ')" > "$OUT/status"
L() { uv run tools/machine_lock.py --shared -- "$U" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath "$WT/unity" -buildTarget OSXUniversal "$@"; }
L -executeMethod Statecraft.Game.EditorTools.UnityMcpGuard.WarmUp -logFile "$OUT/warmup.log" > "$OUT/warmup.out" 2>&1; echo "warmup exit $? ok=$(grep -c 'UnityMcpGuard warm-up: gate defines in place' "$OUT/warmup.log")" >> "$OUT/status"
for side in before after; do
  if [ $side = before ]; then git checkout -q --detach "$BEFORE" 2>>"$OUT/status"; FILTER="E21Labels2490"; else git checkout -q --detach "$AFTER" 2>>"$OUT/status"; FILTER="E21Labels2490;E21Shadow2490"; fi
  echo "$side HEAD=$(git rev-parse HEAD) filter=$FILTER $(date '+%H:%M')" >> "$OUT/status"
  cp "$S/rig/E21Labels2490.cs" "$D/"
  [ $side = after ] && cp "$S/rig/E21Shadow2490.cs" "$D/"
  rm -rf .superpowers/e21-labels-2490
  LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 L -runTests -testPlatform EditMode -testFilter "$FILTER" -testResults "$OUT/$side.xml" -logFile "$OUT/$side.log" > "$OUT/$side.out" 2>&1
  echo "$side exit $? $(grep -o -m1 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"[^>]*skipped="[0-9]*"' "$OUT/$side.xml" 2>/dev/null)" >> "$OUT/status"
  grep -a -E "E21LABELS|E21SHADOW|error CS" "$OUT/$side.log" > "$OUT/$side-lines.txt"; wc -l < "$OUT/$side-lines.txt" | sed "s/^/$side lines /" >> "$OUT/status"
  rm -rf "$S/raw-$side"; mkdir -p "$S/raw-$side"
  [ -d .superpowers/e21-labels-2490 ] && mv .superpowers/e21-labels-2490/* "$S/raw-$side/"
  rm -rf .superpowers/e21-labels-2490
  rm -f "$D/E21Labels2490.cs" "$D/E21Labels2490.cs.meta" "$D/E21Shadow2490.cs" "$D/E21Shadow2490.cs.meta"
  git checkout -- unity/ProjectSettings unity/Assets/Settings "unity/Assets/Art/HexTiles/Props/arab_village_prickly_pear/prickly_pear-rock.mat" 2>>"$OUT/status"
done
git checkout -q --detach "$AFTER" 2>>"$OUT/status"
rmdir .superpowers/civ-1398-shots 2>/dev/null
echo "final HEAD=$(git rev-parse --short HEAD) porcelain=$(git status --porcelain | wc -l | tr -d ' ')" >> "$OUT/status"
echo DONE >> "$OUT/status"
