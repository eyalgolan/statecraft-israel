#!/usr/bin/env bash
# Compile unity/'s two asmdefs with dotnet, outside Unity (T72): catches compile breaks such as a PR calling code another
# PR deleted, in about 2 seconds and with no Unity licence. scripts/check.sh runs it only where the project's editor
# and a unity/Library exist. It does not run any test: the EditMode suite still needs Unity.
#
# Usage: scripts/unity_compile/compile.sh <path to Unity.app/Contents>
# Writes nothing into the checkout: the project files, the published sim DLLs and the output go to a temp folder.
set -euo pipefail
cd "$(dirname "$0")/../.."
contents="${1:?usage: compile.sh <Unity.app/Contents>}"
here=scripts/unity_compile
version="$(sed -n 's/^m_EditorVersion: //p' unity/ProjectSettings/ProjectVersion.txt)"
pinned="$(sed -n 's:.*<UnityVersion>\(.*\)</UnityVersion>.*:\1:p' "$here/Common.props")"
if [ "$version" != "$pinned" ]; then
  echo "unity/ is on Unity $version, but $here/Common.props has the define list of $pinned."
  echo "Copy DefineConstants from Unity's generated unity/Statecraft.Game.csproj into UnityDefines and set UnityVersion."
  exit 1
fi
# A Library without these (never opened in Unity, or an import that failed) is an error here, not a skip.
lib=unity/Library
for f in "$contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.CoreModule.dll" \
         "$contents/Resources/Scripting/NetStandard/ref/2.1.0/netstandard.dll" \
         "$lib"/ScriptAssemblies/{Unity.RenderPipelines.Core.Runtime,Unity.RenderPipelines.Universal.Runtime}.dll \
         "$lib"/ScriptAssemblies/{Unity.InputSystem,UnityEngine.TestRunner,UnityEditor.TestRunner}.dll; do
  [ -f "$f" ] || { echo "missing: $f (open the project in Unity once so it builds its Library)"; exit 1; }
done
nunit=("$lib"/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll)
[ -f "${nunit[0]}" ] || { echo "missing: Unity's nunit.framework.dll under $lib/PackageCache"; exit 1; }
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
cp "$here"/Common.props "$here"/Game.csproj "$here"/Tests.csproj "$tmp/"
dotnet publish sim/Statecraft.Sim -c Release -o "$tmp/sim" --nologo -v q
if ! dotnet build "$tmp/Tests.csproj" --nologo -v q -p:Editor="$contents" -p:Lib="$PWD/unity/Library" \
    -p:Src="$PWD/unity" -p:Sim="$tmp/sim" -p:OutputPath="$tmp/bin/"; then
  echo "unity/ does not compile. If the error names a package type (URP, Input System, the test framework),"
  echo "unity/Library may be older than unity/Packages: open the project in Unity once and run this again."
  exit 1
fi
echo "unity compile: Statecraft.Game and Statecraft.Tests.EditMode build with 0 errors and 0 warnings"
