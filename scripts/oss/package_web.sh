#!/usr/bin/env bash
# Put the licence and attribution files INSIDE a WebGL build, have M3's sensitivity gate zip it
# (tools/package_release.py: it refuses until every sensitive event is sensitivity-reviewed), then prove the notices
# are in the zip by listing it. no_human shipped a bundle whose notices file pointed at a script that never copied
# them; this script checks the artefact itself.
# Usage: bash scripts/oss/package_web.sh unity/Builds/Web-gzip unity/Builds/statecraft-israel-vX.Y.Z-web.zip
# PACKAGE_RELEASE overrides the zipping script (tests only).
set -euo pipefail
build="${1:?usage: package_web.sh BUILD_DIR ZIP_PATH}"
zip_path="${2:?usage: package_web.sh BUILD_DIR ZIP_PATH}"
root="$(cd "$(dirname "$0")/../.." && pwd)"
packager="${PACKAGE_RELEASE:-$root/tools/package_release.py}"
[ -f "$build/index.html" ] || { echo "not a WebGL build (no index.html): $build"; exit 1; }
case "$zip_path" in /*) ;; *) zip_path="$PWD/$zip_path" ;; esac

mkdir -p "$build/LICENSES"
cp "$root/LICENSE" "$root/NOTICE" "$root/THIRD-PARTY-NOTICES.md" "$build/"
cp "$root/LICENSES/"*.txt "$build/LICENSES/"
# Each font's own OFL text travels with it (its copyright line differs), under the names site/public/fonts uses.
cp "$root/unity/Assets/UI/Fonts/OFL.txt" "$build/OFL-EBGaramond.txt"
cp "$root/unity/Assets/UI/Fonts/FrankRuhlLibre/OFL.txt" "$build/OFL-FrankRuhlLibre.txt"

rm -f "$zip_path"   # a refusal must not leave an earlier zip behind
python3 "$packager" --build "$build" --zip "$zip_path"

listing="$(unzip -Z1 "$zip_path")"
for f in LICENSE NOTICE THIRD-PARTY-NOTICES.md OFL-EBGaramond.txt OFL-FrankRuhlLibre.txt LICENSES/CC-BY-4.0.txt LICENSES/CC-BY-SA-4.0.txt \
         LICENSES/CC0-1.0.txt LICENSES/YamlDotNet-MIT.txt index.html; do
  printf '%s\n' "$listing" | grep -qxF "$f" || { echo "MISSING in zip: $f"; rm -f "$zip_path"; exit 1; }
done
shasum -a 256 "$zip_path"
echo "OK: notices inside $zip_path"
