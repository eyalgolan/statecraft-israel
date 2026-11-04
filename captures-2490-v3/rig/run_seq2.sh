#!/bin/zsh
# run_seq2.sh <after-sha>: #2490 bomb sequence v2 on the after tree only, IN PLAY. Under gate_serial.py only.
set -u
AFTER=$1
WT=$HOME/israel-game-wt/e21-dakota3
S=/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-2490v3
OUT=$S/logs-seq2; mkdir -p "$OUT"
D=unity/Assets/Tests/EditMode
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
export UNITY_MCP_SERVER_PATH=/private/tmp/claude-501/-Users-eyalgolan/13dcc884-385c-4871-b80d-c77a529df6ce/scratchpad/gates/no-mcp-server
echo "AFTER=$AFTER WT=$WT OUT=$OUT"
cd "$WT" || exit 1
git checkout -q --detach "$AFTER"
echo "start $(date '+%H:%M:%S') HEAD=$(git rev-parse HEAD) dirty=$(git status --porcelain | wc -l | tr -d ' ')" > "$OUT/status"
L() { uv run tools/machine_lock.py --shared -- "$U" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath "$WT/unity" -buildTarget OSXUniversal "$@"; }
cp "$S/rig/E21Play2490.cs" "$D/"
rm -rf .superpowers/e21-play-2490
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 L -runTests -testPlatform EditMode -testFilter '^E21Play2490\.EnglishSeq$' -testResults "$OUT/seq2.xml" -logFile "$OUT/seq2.log" -nostart > "$OUT/seq2.out" 2>&1
echo "seq2 exit $? $(grep -o -m1 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"[^>]*skipped="[0-9]*"' "$OUT/seq2.xml" 2>/dev/null) $(date '+%H:%M:%S')" >> "$OUT/status"
grep -a -E "E21PLAY|error CS|Exception" "$OUT/seq2.log" > "$OUT/seq2-lines.txt"
rm -rf "$S/raw-seq2"; mkdir -p "$S/raw-seq2"
[ -d .superpowers/e21-play-2490 ] && mv .superpowers/e21-play-2490/* "$S/raw-seq2/"
rm -rf .superpowers/e21-play-2490
rm -f "$D/E21Play2490.cs" "$D/E21Play2490.cs.meta"
git checkout -- unity/ProjectSettings unity/Assets/Settings unity/Assets/Map.unity unity/Assets/Art 2>>"$OUT/status"
rmdir .superpowers/civ-1398-shots 2>/dev/null
echo "final HEAD=$(git rev-parse --short HEAD) porcelain=$(git status --porcelain | wc -l | tr -d ' ')" >> "$OUT/status"
echo DONE >> "$OUT/status"
