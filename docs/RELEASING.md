# Releasing

A release is a tagged commit on `main` with a CHANGELOG section and, from the first shared build on, a WebGL zip on the
GitHub Releases page (and, from M4, on itch.io). Steps 1–3 are a normal pull request. Steps 4–7 happen on the merged `main`, and steps 5–7 are owner steps.

1. **Choose the version** (Semantic Versioning, pre-1.0 while the game is pre-release).
2. **Bump every version site in one pull request:** `sim/Statecraft.Sim/SimInfo.cs` (`Version`) and
   `unity/ProjectSettings/ProjectSettings.asset` (`bundleVersion`). Then move the `## [Unreleased]` entries in
   `CHANGELOG.md` under `## [X.Y.Z] - YYYY-MM-DD` and leave an empty `## [Unreleased]` above it.
   `scripts/oss/tests/test_release.py::test_versions_agree` fails if the three disagree.
3. **Merge** the pull request (squash).
4. **Run the release gate** on the merged `main` (it refuses to run elsewhere): `bash scripts/oss/release_gate.sh`. It must print
   `RELEASE GATE OK`. It refuses while an owner decision is open (including D10, history acknowledgement, and D11, the
   sensitivity review), while an asset row is `owner-verify`, while the sim's sensitivity gate refuses `content/`, or
   while the history scan shows an open secret-shape row.
5. **Build and package** on the owner's machine, from the merged `main`:

   ```bash
   "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression gzip -shareable -logFile "$PWD/unity-build-web-gz.log"
   git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
   golden_expect=(); while read -r name hash; do [[ -z "$name" || "$name" == \#* ]] || golden_expect+=(--expect "GOLDEN OK $name $hash"); done < sim/Statecraft.Sim.Tests/golden/manifest.txt
   # one GOLDEN OK line per manifest golden (#394), e.g. "GOLDEN OK hash1950 9922495a92307f87d7b5338cd8501fb60ad0424ea6b19ef2d1ec55c44a8bdfb8"
   # and "GOLDEN OK hash1952 548a6acce93c677f566e7d6febdeb50ed6aeba0e4f61a884708ef0fc38774d87"
   uv run tools/web_check.py unity/Builds/Web-gzip --query "golden&smoke=5" "${golden_expect[@]}" \
     --expect "SMOKE OK 5 turns 1948-10-15 3005405d64abc6e7418fa1f0d35c36ce5f64ed3af7c5a4c24a73d5dfdb16121f"
   uv run tools/web_check.py unity/Builds/Web-gzip --startscreen new --timeout 300 --stall 90
   uv run tools/web_check.py unity/Builds/Web-gzip --startscreen recording --timeout 300 --stall 90
   uv run tools/web_check.py unity/Builds/Web-gzip --startscreen new --query lang=he --timeout 300 --stall 90
   uv run tools/web_check.py unity/Builds/Web-gzip --startscreen recording --query lang=he --timeout 300 --stall 90
   bash scripts/oss/package_web.sh unity/Builds/Web-gzip unity/Builds/statecraft-israel-vX.Y.Z-web.zip
   ```

   The release build is gzip, not Brotli (owner decision on #879, 17 October 2026: "gzip for both", one zip for GitHub
   Releases and itch.io). itch.io's CDN decodes a `.br` file and re-encodes it by content type, and it sends Unity's
   `.data` (`application/octet-stream`) to the player uncompressed; a gzip `.data` stays gzip. Estimated from the 15 October
   build's files, that is about 34 MB to download per player instead of about 46 MB. Both load in a browser with Decompression
   Fallback off, as `Build.Web` sets it, and `web_check.py` serves `*.gz` with `Content-Encoding: gzip`.

   The four `--startscreen` passes (owner decision on #967, 17 October 2026) open the start screen every player sees
   first, which the golden and smoke run skips: New game, and Play the recording then New game, in English and in
   Hebrew. Each must print `PASS`. A console exception, `RECORDING failed` or `BOOT FAIL` fails it (#908).

   For the website's playable demo, build a second web variant with Unity's Decompression Fallback - GitHub Pages
   cannot set response headers, so the loader must decompress the files itself (the site's Play page explains) -
   check it and package it the same way:

   ```bash
   "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression gzip -decompressionFallback -shareable -out Builds/Web-pages -logFile "$PWD/unity-build-web-pages.log"
   git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
   uv run tools/web_check.py unity/Builds/Web-pages --query "golden&smoke=5" "${golden_expect[@]}" \
     --expect "SMOKE OK 5 turns 1948-10-15 3005405d64abc6e7418fa1f0d35c36ce5f64ed3af7c5a4c24a73d5dfdb16121f"
   uv run tools/web_check.py unity/Builds/Web-pages --startscreen new --timeout 300 --stall 90
   uv run tools/web_check.py unity/Builds/Web-pages --startscreen recording --timeout 300 --stall 90
   uv run tools/web_check.py unity/Builds/Web-pages --startscreen new --query lang=he --timeout 300 --stall 90
   uv run tools/web_check.py unity/Builds/Web-pages --startscreen recording --query lang=he --timeout 300 --stall 90
   bash scripts/oss/package_web.sh unity/Builds/Web-pages unity/Builds/statecraft-israel-vX.Y.Z-web-pages.zip
   ```

   When the latest release carries this `statecraft-israel-vX.Y.Z-web-pages.zip`, the site's deploy
   (`.github/workflows/site.yml`) unpacks it at `/demo/`, so the demo is live at the site's `/demo/` after the next
   deploy (`gh workflow run site.yml` starts one on demand). Until then the site deploys without it, and the Play
   page keeps saying there is no hosted build. Updating the Play page (and the README's Play section) to link to
   `/demo/` is a follow-up step when the first such release ships, not part of this release flow.

   `package_web.sh` copies the licence and attribution files into the build folder, then runs
   `tools/package_release.py`, M3's sensitivity gate, which zips the folder and prints `PACKAGED …` or
   `PACKAGE REFUSED …`. It refuses a build that was not made with `-shareable` (the build's `event-photos.json` says
   so) or that carries an unreviewed sensitive event photograph. It then lists the zip, and prints the SHA-256 and `OK: notices inside …`. If it prints anything
   else, stop. This one zip is the file for GitHub Releases and for the M4 itch.io upload (`docs/m3-release.md`); never
   zip a build by hand.

   The files under `Build/` are named by their content hash (`webGLNameFilesAsHashes: 1`, set by `ProjectSetup.Apply`;
   issue #437), so their names change with every build that changes them. Only the build's `index.html` says which ones
   belong to it: never refer to them by a fixed name. Unity empties `Build/` on each build; `package_release.py` zips
   only the `Build/` files `index.html` names (it prints `LEFT OUT Build/<name>` for any other) and refuses the folder
   when one of them is missing.

   With `webGLDataCaching: 1`, Unity's loader also keeps each build's `.data` in the browser's IndexedDB (UnityCache),
   keyed by its URL. Its clean-up drops entries made under another `productVersion` (`bundleVersion`), and otherwise
   trims the least recently used above 1 GiB (`MaximumCacheSize=1073741824` in the 6000.3 loader). With hashed names,
   every build with the same `bundleVersion` adds one more `.data` there until that cap; the version bump in step 2
   clears them at each release.

   **Desktop builds (macOS, Linux, Windows).** Desktop players are built on the owner's Mac (decisions O7 and O8 in
   `docs/oss/DECISIONS.md`; plan in `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md`). On the merged `main`, tag `HEAD` first
   (`git tag -a vX.Y.Z -m "Statecraft: Israel vX.Y.Z"`; step 6 pushes it), then:

   ```bash
   uv run tools/release_build.py --platform all --tag vX.Y.Z   # or mac, linux, windows; --dry-run prints the commands
   ```

   Before building anything it refuses: a dirty tree; a branch other than `main`, or a `HEAD` that your last
   `git fetch origin` did not show on `origin/main` (the script never fetches); a tag that is not `v` plus `bundleVersion`
   (a `-suffix` is allowed; it is the default) or does not point at `HEAD`; an editor whose version is not the one in `ProjectVersion.txt`; an
   `--out` inside any checkout or worktree of the repository (ignored folders too: a build empties
   `StreamingAssets/content`), or one that is not empty. It also stops if Unity Hub lacks a module the platforms need: Mac
   Build Support (IL2CPP), Linux Build Support (Mono) or Windows Build Support (Mono); the message names them and gives the
   Hub command. `--dev` makes a test build from any branch: not `-shareable`, no `--tag`, and archives named
   `vX.Y.Z-dev.<commit>` so they cannot pass for a release.

   Each build runs under `tools/machine_lock.py --shared --wait-unity`. The script runs the `-golden` check on the macOS
   player, the only one the Mac can run; the Linux and Windows players are recorded as unchecked until the CI player check
   runs them. It archives each player under the names that check expects (`statecraft-israel-vX.Y.Z-macos-universal.zip`,
   made with `ditto`; `-linux-x64.tar.gz`; `-windows-x64.zip`), each with `LICENSE`, `NOTICE`, `THIRD-PARTY-NOTICES.md`,
   the two font licences (`OFL-EBGaramond.txt`, `OFL-FrankRuhlLibre.txt`) and `LICENSES/` beside the player (it lists the
   archive to check), and writes `build-record.json`, `build-record.md`, a `files-<platform>.sha256` list of each
   archive's files and the build's `event-photos-<platform>.json` to `~/statecraft-release/v<version>-<commit>-<UTC time>/`
   (or `--out`), and prints the path. It never commits, pushes or uploads: copy the record files to the build records
   repository by hand. Signing and Steam upload are later steps, not yet scripted.
6. **Tag and publish (owner), in one `gh release create`.** The release carries the web zip and, once desktop players
   ship (decisions O1, O7 and O9 in `docs/oss/DECISIONS.md`), all three desktop archives from "Desktop builds" above, as
   a pre-release with "unchecked" in its notes. Never publish first and upload the players afterwards:

   ```bash
   git tag -a vX.Y.Z -m "Statecraft: Israel vX.Y.Z"   # skip if "Desktop builds" already made it
   git push origin vX.Y.Z
   gh release create vX.Y.Z --prerelease --title "vX.Y.Z" --notes-file <notes from CHANGELOG.md, saying "unchecked"> \
     unity/Builds/statecraft-israel-vX.Y.Z-web.zip unity/Builds/statecraft-israel-vX.Y.Z-web-pages.zip \
     <out>/statecraft-israel-vX.Y.Z-windows-x64.zip \
     <out>/statecraft-israel-vX.Y.Z-linux-x64.tar.gz <out>/statecraft-israel-vX.Y.Z-macos-universal.zip
   ```

   `<out>` is the folder `tools/release_build.py` printed. The notes are the version's CHANGELOG section
   (`awk '/^## \[X.Y.Z\]/{f=1;next} /^## \[/{f=0} f' CHANGELOG.md`), the SHA-256 from step 5, and "unchecked".

   Publishing starts the `Player golden` workflow (`.github/workflows/player-golden.yml`). It downloads each player,
   checks it against the SHA-256 GitHub records for the asset, and runs `-batchmode -nographics -golden` on GitHub's
   Windows and Linux machines (the Linux player under `xvfb-run -a`: with no display at all it crashes, issue #533), and the Mac app twice on Apple silicon (`macos-15`):
   its arm64 slice, and its x86_64 slice under Rosetta (`arch -x86_64`; GitHub retires its last Intel image,
   `macos-15-intel`, in August 2027, issue #548). It fails when only one or two of the
   three archives are attached. A release with none (a web-only release, before desktop players ship) passes, but its
   run summary says UNCHECKED.
   - **The release stays a pre-release marked "unchecked" until that run is green.** Testers can download a pre-release
     as soon as it is published, so do not announce it before then. Then remove "unchecked" from the notes.
   - **Uploading or replacing an archive later** (`gh release upload`, with or without `--clobber`) starts no run. After
     any such upload, run `gh workflow run player-golden.yml -f tag=vX.Y.Z` and wait for it to pass
     (`gh run watch`). A run that started before the replacement fails on the digest; run it again.
7. **Check every published artefact:** download each uploaded archive (GitHub Releases, and itch.io once used) into an
   empty folder, then list it and look for the notices: for a `.zip` (the web zip, the macOS and Windows players),
   `unzip -Z1 <zip> | grep -E '^(LICENSE|NOTICE|THIRD-PARTY-NOTICES.md|OFL-EBGaramond.txt|OFL-FrankRuhlLibre.txt)$'`;
   for the Linux `.tar.gz`,
   `tar -tzf <archive> | grep -E '^(\./)?(LICENSE|NOTICE|THIRD-PARTY-NOTICES.md|OFL-EBGaramond.txt|OFL-FrankRuhlLibre.txt)$'`.
   It must print five lines for each archive.

## Refreshing the README screenshots

The README's screenshots are claims about the current build. Re-take them whenever the build looks different, and
**always in the pull request that merges a visual change** (visual-v1 included), never as a follow-up:

1. Build the web player (`docs/dev-setup.md`) and serve it. After visual-v1 has merged, capture with its
   `uv run tools/web_check.py <build> --shots …` under its machine lock, so the capture does not disturb profiling.
   Before that, capture by hand at 1640×920 in Chrome.
2. Replace `docs/images/build-m3-map.jpg`, `build-m3-start.jpg`, `build-m3-city.jpg`, `build-m3-cabinet.jpg` and `build-m3-why.jpg` (or add `build-<milestone>-*`
   and update the README to the new names). Each must show a non-sensitive moment: no unreviewed sensitive event card.
3. Update the visible caption "Real screenshots from build X.Y.Z (Mn), YYYY-MM-DD." in `README.md`, and the
   "Is this any good?" and "Roadmap" text the change made stale.
4. Rebuild `docs/images/social-preview.jpg` (1280×640) from the new map shot: `sips --resampleWidth 1280 <shot> --out /tmp/sp.jpg` then `sips --padToHeightWidth 640 1280 --padColor 0B1220 /tmp/sp.jpg --out docs/images/social-preview.jpg` (padded, not cropped, so no text is cut off). The owner re-uploads it under Settings →
   General → Social preview. Never use concept art for the social preview.

## The README's hero, concept art and screenshots

What `README.md` shows at the top, and what `scripts/oss/tests/test_readme.py` checks:

1. **The hero is the cinematic trailer.** It is a `https://github.com/user-attachments/assets/<id>` URL on its own line
   above the `# Statecraft: Israel` heading, which GitHub plays in its video player, with a caption saying it was made
   with AI video tools and is not in-game footage (`test_hero_is_the_trailer_in_githubs_player_disclosed_as_ai`).
2. **The concept art opens "What is this?"** (`CONCEPT`, `docs/images/hero-concept-1936.jpg`). Its alt text says it is
   AI-generated concept art and not the current build, and the caption under it says "Concept art (AI-generated)",
   "set in 1936" and "not the current build" (`test_concept_art_opens_what_is_this_captioned_as_concept_art`).
3. **Real gameplay comes next:** the clip (`CLIP`) and the five screenshots (`GRID`, one wide over two pairs), with the
   captions "Real gameplay, build X.Y.Z, YYYY-MM-DD, commit abc1234." and "Real screenshots from build X.Y.Z (M3),
   YYYY-MM-DD." (`test_real_dated_screenshots_in_a_one_plus_two_plus_two_grid`; refreshing them is the section above).

To change any of these, edit `README.md` and `README.he.md` together (the Hebrew README's tests keep the same facts),
give a new image an `ASSETS.yaml` row, change `CONCEPT`, `CLIP` or `GRID` in `test_readme.py` if a file name changes,
and land it as a normal pull request. Whether a real build screenshot later replaces the concept art in "What is
this?" is not decided.

## Capturing trailer footage (issue #45)

The trailer's map shots come from the real build: the editor renders the map frame by frame, so a capture can be run
again and gives the same camera, state and water on every frame. The pixels match too, except the unit badges' glyphs
at the whole-country zoom: two runs on the build server differed only there (1,288 of 6.2 million bytes per frame;
the 60 km frames were byte-identical), where a glyph and its plate draw at almost the same depth. Build the sim DLLs
first (`python3 tools/build_sim_for_unity.py`), run Unity with graphics (not `-nographics`; on Linux under
`xvfb-run -a`), then encode the frames with ffmpeg:

```sh
"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity \
  -executeMethod MapCapture.Trailer -captureDir "$PWD/captures/<name>" [-seed 1948] -logFile "$PWD/unity-trailer.log"
ffmpeg -framerate 60 -i captures/<name>/frame-%05d.png -vf scale=out_color_matrix=bt709 \
  -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -colorspace bt709 -color_primaries bt709 -color_trc bt709 \
  -movflags +faststart captures/<name>/trailer-map.mp4
```

Give `-captureDir` an absolute path (a relative one resolves against Unity's working directory) under the ignored
`captures/` or outside the repo: frames anywhere else in the tree would mark the recorded commit `-dirty`. The capture
deletes any `frame-*.png` already in it first, so an older, longer run cannot leak into the encode.

- **What it renders:** the camera moves in `TrailerShot.Default` (`unity/Assets/Scripts/Map/TrailerShot.cs`), built only
  from the promo poses the art checkpoint already compares (`PromoPose`): the whole country held for 2 s, down to the
  Jaffa-Tel Aviv-Lydda ground of promo 03 (6 s), and back up to the whole country (6 s). 840 frames, 14 s, each move
  eased in and out. That order is a placeholder; the shots are the owner's call. Change the list to change it.
- **Settings:** 1920 x 1080 on the desktop (PC) tier, 60 fps, the water's clock pinned to each frame's time, the 1948
  sub-districts and the units of turn 1 of a new historical game started with `-seed` (default the golden seed, 1948;
  turn 1's units do not depend on the seed today, it is recorded so a later moment can be repeated). The frames are
  `frame-00000.png` onwards, one sequence across the shots. `trailer.txt` beside them records the commit, Unity
  version, seed, size, fps, tier and each shot's frame range.
- **Not in it:** the UI (editor renders draw no UI Toolkit, so UI shots still come from the player), sound, and the cut.
  The .mp4 has no audio track; Steam prefers AAC audio, which the cut adds.
- **Why PNG frames:** a frame is written only when it is fully rendered, so a slow machine gives the same footage as a
  fast one, and the Linux editor needs no video codec. On the software-rendered Linux build server (llvmpipe) the
  default 840 frames took 15 to 25 minutes (907 s alone, 1,459 s beside another Unity run) and 2.3 GB of PNG; the
  encode took under a minute. Delete the frames after encoding.
- **Check the encode** before it goes anywhere:
  `ffprobe -v error -show_entries stream=codec_name,width,height,r_frame_rate,bit_rate captures/<name>/trailer-map.mp4`
  must say h264, 1920 x 1080, 60/1, and a bit rate above Steam's 5,000 Kbps. If it is under (long still holds compress
  well), encode again with `-b:v 8M -maxrate 12M -bufsize 16M` in place of `-crf 16`.

## Store-page checklist (issue #45; OWNER STEP)

This is preparation, not a published store page or an answered content survey. The checklist does not clear the name,
assets or content for commercial use. Finish the release gates above first; the owner reviews and submits the page.

- [ ] Resolve D8 in `docs/oss/DECISIONS.md` before using "Statecraft: Israel" as the commercial name. Do not treat the
  repository name as trademark clearance. Review the description against the actual release build: distinguish
  features that are playable now from roadmap plans, and do not promise a platform or language not verified in it.
- [ ] Prepare the Steam store capsules at the current upload sizes below, with a readable game logo and artwork.
  Capsule text is the game title only, not quotes, awards or marketing copy. Check the small capsule at its generated
  120 x 45 and 184 x 69 display sizes before approval. Use the current official templates, not older dimensions.

  | Store asset | Upload pixels |
  | --- | --- |
  | Header capsule | 920 x 430 |
  | Small capsule | 462 x 174 |
  | Main capsule | 1232 x 706 |
  | Vertical capsule | 748 x 896 |

- [ ] Capture at least five gameplay screenshots, minimum 1920 x 1080, 16:9, from the real release build, not concept art,
  pre-rendered cinematic stills or marketing images. Record the build commit and zip SHA-256 with the capture date.
  The README build shots (`docs/images/build-m3-*.jpg`, 1568 x 882) are smaller than this minimum. Show the
  actual UI when useful; do not pass off `promo/final/03-gameplay-1936.png` or the AI likenesses in promo/final/04 and
  08 as gameplay. Review each screenshot's age-suitability flag individually; never mark a sensitive scene suitable for
  all ages just to gain placement. If at least four genuinely qualify, mark them suitable for all ages; with fewer
  flagged, the game may not appear in some placements.
- [ ] Prepare a trailer from the real build, not the M2 recruiting flythrough or concept-art target. Capture the map
  footage with "Capturing trailer footage" above (1920 x 1080, 60 fps, deterministic); the draft cut is still pending
  (#45), so this checklist does not provide a finished trailer. Keep the capture's `trailer.txt` (build commit, seed and
  capture settings) with the cut.
  Steam requires a trailer and recommends that the first one be mostly gameplay; this project's rule (real build only)
  is stricter. Upload the highest resolution available, up to 1920 x 1080, at 30/29.97 or 60/59.94 fps and
  5,000+ Kbps, in a .mov, .wmv or .mp4 container; H.264 video with AAC audio is preferred
  ([Steamworks: Trailers](https://partner.steamgames.com/doc/store/trailer)). The game cannot be released while a
  trailer is still encoding.
- [ ] Complete all Steam Content Survey sections against the build and store page (OWNER STEP): general content,
  mature content and generative AI. Review war, terror and the Holocaust in both playable and bundled content;
  disclose all adult content uploaded in the build, even if it cannot be reached during play. Prepare honest answers
  and access instructions from what actually ships, not ratings guessed from the project's subject.
- [ ] Regenerate `docs/ai-disclosure.md` with
  `uv run scripts/oss/ai_disclosure.py --output docs/ai-disclosure.md` and review its diff against both manifests.
  It lists explicit `ai_generated: true` asset rows only and does not answer the survey. Steam's current AI section
  concerns player-consumed content such as artwork, sound, narrative and localization, not efficiency tools alone.
  Check shipped content outside the manifest's media rows too; do not infer a complete survey from an empty media
  group. Keep the no-live-generation statement only while it remains true. An `owner-verify` asset is not cleared
  by appearing in generated disclosure text; do not change flags merely to make an answer convenient.
- [ ] Have the owner inspect the final previews, descriptions, media and survey answers together before submission.
  For itch.io, use `docs/m3-release.md`'s separate upload and save test; Steam image sizes are not itch.io requirements.

Official Steam sources checked 11 October 2026; re-check them before submission:
- [Graphical assets overview](https://partner.steamgames.com/doc/store/assets).
- [Store assets and screenshots](https://partner.steamgames.com/doc/store/assets/standard).
- [Content Survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey).
- [Trailers](https://partner.steamgames.com/doc/store/trailer).
