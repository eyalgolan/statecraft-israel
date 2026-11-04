#!/bin/zsh
# run.sh: #2375 C04 firing stills, before (b2269c3d7 + main's FireTakeRetargetShots) and after (main). en_US only:
# TokenCaptures draws no UI text, so one locale covers EN and HE. Under gate_slots.py only.
set -u
WT=$HOME/israel-game-wt/e21-c04
S=/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-c04
OUT=$S/logs; mkdir -p "$OUT"
BEFORE=b2269c3d746798f742e1d41ae7294d3e4849c7ab; AFTER=$1
FIX=unity/Assets/Tests/EditMode/FireTakeRetargetShots.cs
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
export UNITY_MCP_SERVER_PATH=/private/tmp/claude-501/-Users-eyalgolan/13dcc884-385c-4871-b80d-c77a529df6ce/scratchpad/gates/no-mcp-server
cd "$WT"
echo "start $(date '+%H:%M') HEAD=$(git rev-parse --short HEAD) dirty=$(git status --porcelain | wc -l | tr -d ' ')" > "$OUT/status"
L() { uv run tools/machine_lock.py --shared -- "$U" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath "$WT/unity" -buildTarget OSXUniversal "$@"; }
for side in before after; do
  if [ $side = before ]; then git checkout -q --detach "$BEFORE" 2>>"$OUT/status" || { echo "checkout $side failed" >> "$OUT/status"; exit 1; }; cp "$S/rig/FireTakeRetargetShots.cs" "$FIX"
  else git checkout -q --detach "$AFTER" 2>>"$OUT/status" || { echo "checkout $side failed" >> "$OUT/status"; exit 1; }; fi
  echo "$side HEAD=$(git rev-parse HEAD) $(date '+%H:%M') porcelain=$(git status --porcelain | tr '\n' ' ')" >> "$OUT/status"
  uv run tools/machine_lock.py --shared -- uv run tools/build_sim_for_unity.py > "$OUT/$side-build-sim.log" 2>&1; echo "$side build-sim exit $?" >> "$OUT/status"
  L -executeMethod Statecraft.Game.EditorTools.UnityMcpGuard.WarmUp -logFile "$OUT/$side-warmup.log" > "$OUT/$side-warmup.out" 2>&1; echo "$side warmup exit $? ok=$(grep -c 'UnityMcpGuard warm-up: gate defines in place' "$OUT/$side-warmup.log")" >> "$OUT/status"
  for loc in en_US; do
    rm -rf "$S/raw-$side-$loc"
    STATECRAFT_FIRE_STILLS="$S/raw-$side-$loc" LANG=$loc.UTF-8 LC_ALL=$loc.UTF-8 L -runTests -testPlatform EditMode -testFilter "FireTakeRetargetShots.TokenCaptures" -testResults "$OUT/$side-$loc.xml" -logFile "$OUT/$side-$loc.log" > "$OUT/$side-$loc.out" 2>&1
    echo "$side $loc exit $? $(grep -o -m1 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"[^>]*skipped="[0-9]*"' "$OUT/$side-$loc.xml" 2>/dev/null)" >> "$OUT/status"
    grep -a "token \|attack played\|right hand moved" "$S/raw-$side-$loc/tokens/README.txt" >> "$OUT/status" 2>/dev/null
  done
  [ $side = before ] && rm -f "$FIX" "$FIX.meta"
  git checkout -- unity/ProjectSettings unity/Assets/Settings "unity/Assets/Art/HexTiles/Props/arab_village_prickly_pear/prickly_pear-rock.mat" 2>>"$OUT/status"
done
echo "final HEAD=$(git rev-parse --short HEAD) porcelain=$(git status --porcelain | wc -l | tr -d ' ')" >> "$OUT/status"
echo DONE >> "$OUT/status"
