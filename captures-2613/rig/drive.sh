#!/bin/zsh
# drive.sh <tag>: pre-checks (window, <=2 other batch Unity, console unlocked, clean tree, compile check), then run.sh through the slot gate.
set -u
TAG=${1:?tag}
S=/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-cap-2613
WT=$HOME/israel-game-wt/e21-cap
H=$(date +%H%M); if [ "$H" -lt 0435 ] && [ "$H" -ge 0225 ]; then echo "REFUSED window $H"; exit 9; fi
n=0
while :; do
  c=$(pgrep -f "^/Applications/[U]nity/Hub/Editor/[^ ]*/Unity.app/Contents/MacOS/Unity -batchmode" | wc -l | tr -d ' ')
  [ "$c" -le 2 ] && break
  n=$((n+1)); [ $n -gt 40 ] && { echo "TIMEOUT waiting: $c Unity -batchmode"; exit 4; }
  echo "waiting: $c Unity -batchmode $(date +%H:%M:%S)"; sleep 60
done
echo "unity count ok ($c) $(date +%H:%M:%S)"
ioreg -n Root -d1 -a | grep -A1 IOConsoleLocked | grep -q '<true/>' && { echo "CONSOLE LOCKED"; exit 5; }
echo "console unlocked"
[ -z "$(git -C $WT status --porcelain)" ] || { echo "DIRTY"; git -C $WT status --porcelain | head; exit 2; }
echo "HEAD=$(git -C $WT rev-parse HEAD)"
zsh $S/rig/compilecheck.sh $WT/unity $S/cc-$TAG $S/E21Play2613.cs | tail -3
e=$(cat $S/cc-$TAG/game.out $S/cc-$TAG/tests.out | grep -c 'error CS'); [ "$e" = 0 ] && [ -s $S/cc-$TAG/Statecraft.Tests.EditMode.dll ] || { echo "COMPILE FAIL ($e errors)"; exit 6; }
cd ~/israel-game || exit 1
uv run ~/israel-game/.superpowers/coordinator-tools/gate_slots.py -- zsh $S/rig/run.sh $TAG
echo "gate exit $? $(date +%H:%M:%S)"
cat $S/logs-$TAG/status
