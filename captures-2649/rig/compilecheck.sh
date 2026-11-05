#!/bin/zsh
# compilecheck.sh <src-unity-dir> <outdir> <rig files...>: csc compile of Statecraft.Game + Statecraft.Tests.EditMode (no Unity).
set -u
SRC=$1; OUT=$2; shift 2
LIB=$HOME/israel-game-wt/e21-cap/unity/Library
A=$LIB/Bee/artifacts/2000b0aE.dag
NP=${NP:-$HOME/israel-game-wt/e21-cap/unity/Assets/Plugins/Statecraft}
U=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/Resources/Scripting
echo "SRC=$SRC OUT=$OUT rigs=$*"
mkdir -p "$OUT"
fix() { sed -e "s#\"Library/#\"$LIB/#g" -e "s#\"Assets/Plugins/Statecraft/#\"$NP/#g" -e '/^"Assets/d' -e '/^-out:/d' -e '/^-refout:/d' -e '/^\/additionalfile/d' -e '/^-analyzer/d' "$1"; }
fix $A/Statecraft.Game.rsp > "$OUT/game.rsp"
echo "-out:\"$OUT/Statecraft.Game.dll\"" >> "$OUT/game.rsp"
find "$SRC/Assets/Scripts" -name '*.cs' | sort | sed 's/.*/"&"/' >> "$OUT/game.rsp"
"$U/NetCoreRuntime/dotnet" "$U/DotNetSdkRoslyn/csc.dll" @"$OUT/game.rsp" > "$OUT/game.out" 2>&1; echo "game exit $? errors=$(grep -c 'error CS' "$OUT/game.out")"
fix $A/Statecraft.Tests.EditMode.rsp | sed "s#$LIB/Bee/artifacts/2000b0aE.dag/Statecraft.Game.ref.dll#$OUT/Statecraft.Game.dll#" > "$OUT/tests.rsp"
echo "-out:\"$OUT/Statecraft.Tests.EditMode.dll\"" >> "$OUT/tests.rsp"
{ find "$SRC/Assets/Tests/EditMode" -name '*.cs' | sort; for r in "$@"; do echo "$r"; done; } | sed 's/.*/"&"/' >> "$OUT/tests.rsp"
"$U/NetCoreRuntime/dotnet" "$U/DotNetSdkRoslyn/csc.dll" @"$OUT/tests.rsp" > "$OUT/tests.out" 2>&1; echo "tests exit $? errors=$(grep -c 'error CS' "$OUT/tests.out")"
grep 'error CS' "$OUT/game.out" "$OUT/tests.out" | head -20
