# Developer setup (verified 2026-10-09 on macOS 27.0.1, arm64)

- .NET SDK 10.0.401: `brew install dotnet`
- Unity Hub 3.22.2: `brew install --cask unity-hub`. Sign in and activate a Personal license.
- Unity Editor 6000.3.25f1 (6.3 LTS), changeset e1dba0a9aba4, installed through the Hub CLI:
  `"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install -v 6000.3.25f1 -c e1dba0a9aba4 -a arm64 -m webgl -m mac-il2cpp`
  Modules: Web Build Support, Mac Build Support (IL2CPP).
- `export UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"`
- Python tools: `uv` (0.12.5 verified). Dependencies are declared inline in each tools/*.py script.
- ffmpeg (Homebrew) is used for flythrough videos.

## After cloning
- Run `tools/build_sim_for_unity.sh` before opening `unity/`. The sim DLLs are build output and are git-ignored.

## Builds and browser checks (M3)
- Web build: `"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"`
  (`-compression` is `disabled`, `gzip` or `brotli`; output in `unity/Builds/Web-<compression>/`, about 2 minutes).
- macOS IL2CPP build: `"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile "$PWD/unity-build-mac.log"`
  (output `unity/Builds/Mac/Statecraft.app`).
- Browser check: `uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&smoke=5"` with the three full `--expect`
  values pinned in `docs/determinism.md` (both goldens and the smoke line).
  It uses Chrome for Testing from the Playwright cache (`~/Library/Caches/ms-playwright/chromium-1228` on this Mac;
  elsewhere `npx playwright install chromium`).
- Page flags: `?golden` (golden check), `?smoke=N` (play N turns through the UI; bare `?smoke` = 5, `?smoke=0` = off),
  `?profile` (terrain profile), `?camlog` (log the camera distance), `?bootlog` (each boot phase's timing) and `?navlog`
  (every UI pointer and submit event, screen toggle and map click, with its frame), `?vprofile` (the visual frame-time
  profile per feature), `?shot` (screenshots at the promo poses as `SHOT` console lines) and `?kitscale=N`, `?housescale=N`, `?treescale=N` (the settlement kits' symbolic scale for city blocks, houses and trees, clamped to 5–200; defaults 60, 100, 100). In the editor, `MapCapture.Kits` takes `-kitScale`, `-houseScale` and `-treeScale`. Desktop players take `-golden`,
  `-smoke N`, `-profile`, `-camlog`, `-bootlog`, `-navlog`, `-vprofile` and `-shot`. The console also shows `GAME OK <date> turn <n> hash <h>` at start and `TURN <n> <date> hash <h>` per turn.
- Visual checks (docs/visual-v1.md): measure only under the machine lock, which also waits for any open Unity editor:
  `uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py <build> --query vprofile --expect "PROFILE visual done" --timeout 1800 --stall 120`
  (headless), `... -- uv run tools/headed.py profile --build <build> --dpr 1` (headed; `--dpr 2` is the
  fill-rate stress proxy) and `... -- uv run tools/headed.py boot --build <build> --runs 3 --baseline <ms>
  --baseline-first-frame <ms>` (the boot budget: two spans, default allowance max(300 ms, 50%)); all of them fix the
  viewport to 1920x1080 over CDP and fail on another `res=`; wrap Unity
  batch commands in `uv run tools/machine_lock.py --shared -- ...`; `tools/art_checkpoint.sh [--no-build] <name>` for the
  owner's side-by-side images.
- Quality tiers: `uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.QualityTiers.Apply`
  writes the decisions in `QualityTierValues` to the URP assets (docs/visual-v1.md "Tier decisions"). The built macOS
  player (`unity/Builds/Mac/Statecraft.app`) does not run in the background (`runInBackground` is off): its `-vprofile`
  waits at `GAME OK` until that player's window is in front (`open -a unity/Builds/Mac/Statecraft.app` brings it
  forward). Never script, `open -a` or AppleScript the Unity editor itself.
- A player build rewrites some URP asset and player-setting fields as build-time caches; drop them afterwards with
  `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`.
- Play a build locally: `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.
  A Brotli build needs `Content-Encoding: br`; serve it with web_check's handler (command in `docs/m3-release.md`).
