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
   "$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression brotli -shareable -logFile "$PWD/unity-build-web-br.log"
   git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
   uv run tools/web_check.py unity/Builds/Web-brotli --query "golden&smoke=5" \
     --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" \
     --expect "GOLDEN OK 1952 62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8" \
     --expect "SMOKE OK 5 turns 1948-06-19 d3fdd459ddb876bd8d775c20edd1202cf56ca7134a8ad06f873d4d92034aa5b6"
   bash scripts/oss/package_web.sh unity/Builds/Web-brotli unity/Builds/statecraft-israel-vX.Y.Z-web.zip
   ```

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
6. **Tag and publish (owner):**

   ```bash
   git tag -a vX.Y.Z -m "Statecraft: Israel vX.Y.Z" && git push origin vX.Y.Z
   gh release create vX.Y.Z unity/Builds/statecraft-israel-vX.Y.Z-web.zip --title "vX.Y.Z" --notes-file <(awk '/^## \[X.Y.Z\]/{f=1;next} /^## \[/{f=0} f' CHANGELOG.md)
   ```

   Paste the SHA-256 from step 5 into the release notes.
7. **Check every published artefact:** download each uploaded zip (GitHub Releases, and itch.io once used) into an empty
   folder, then run `unzip -Z1 <zip> | grep -E '^(LICENSE|NOTICE|THIRD-PARTY-NOTICES.md|OFL.txt)$'`. It must print four
   lines for each zip.

## Desktop builds (macOS, Linux, Windows)

Desktop players are built on the owner's Mac (decisions O7 and O8 in `docs/oss/DECISIONS.md`; plan in
`docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md`). On the merged `main`, after the tag
(step 6's `git tag -a vX.Y.Z`, not yet pushed if you like) is on `HEAD`:

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
made with `ditto`; `-linux-x64.tar.gz`; `-windows-x64.zip`) and writes `build-record.json`, `build-record.md`, a
`files-<platform>.sha256` list of each archive's files and the build's `event-photos-<platform>.json` to
`~/statecraft-release/v<version>-<commit>-<UTC time>/` (or `--out`), and prints the path. It never commits, pushes or
uploads: copy the record files to the build records repository by hand. Adding the notices to the archives, signing and
Steam upload are later steps, not yet scripted.

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
- [ ] Prepare a trailer from the real build, not the M2 recruiting flythrough or concept-art target. The deterministic
  1920 x 1080, 60 fps capture and draft cut are the separate trailer part of #45, still pending; this checklist does
  not provide a capture script or a finished trailer. Record the build/seed and capture settings with the cut.
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
