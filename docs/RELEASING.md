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
   `PACKAGE REFUSED …`. It then lists the zip, and prints the SHA-256 and `OK: notices inside …`. If it prints anything
   else, stop. This one zip is the file for GitHub Releases and for the M4 itch.io upload (`docs/m3-release.md`); never
   zip a build by hand.
6. **Tag and publish (owner):**

   ```bash
   git tag -a vX.Y.Z -m "Statecraft: Israel vX.Y.Z" && git push origin vX.Y.Z
   gh release create vX.Y.Z unity/Builds/statecraft-israel-vX.Y.Z-web.zip --title "vX.Y.Z" --notes-file <(awk '/^## \[X.Y.Z\]/{f=1;next} /^## \[/{f=0} f' CHANGELOG.md)
   ```

   Paste the SHA-256 from step 5 into the release notes.
7. **Check every published artefact:** download each uploaded zip (GitHub Releases, and itch.io once used) into an empty
   folder, then run `unzip -Z1 <zip> | grep -E '^(LICENSE|NOTICE|THIRD-PARTY-NOTICES.md|OFL.txt)$'`. It must print four
   lines for each zip.

## Refreshing the README screenshots

The README's screenshots are claims about the current build. Re-take them whenever the build looks different, and
**always in the pull request that merges a visual change** (visual-v1 included), never as a follow-up:

1. Build the web player (`docs/dev-setup.md`) and serve it. After visual-v1 has merged, capture with its
   `uv run tools/web_check.py <build> --shots …` under its machine lock, so the capture does not disturb profiling.
   Before that, capture by hand at 1640×920 in Chrome.
2. Replace `docs/images/build-m3-map.jpg`, `build-m3-cabinet.jpg` and `build-m3-why.jpg` (or add `build-<milestone>-*`
   and update the README to the new names). Each must show a non-sensitive moment: no unreviewed sensitive event card.
3. Update the visible caption "Real screenshots from build X.Y.Z (Mn), YYYY-MM-DD." in `README.md`, and the
   "Is this any good?" and "Roadmap" text the change made stale.
4. Rebuild `docs/images/social-preview.jpg` (1280×640) from the new map shot: `sips --resampleWidth 1280 <shot> --out /tmp/sp.jpg` then `sips --padToHeightWidth 640 1280 --padColor 0B1220 /tmp/sp.jpg --out docs/images/social-preview.jpg` (padded, not cropped, so no text is cut off). The owner re-uploads it under Settings →
   General → Social preview. Never use concept art for the social preview.

## Replacing the concept-art hero (planned)

The hero at the top of the README is AI concept art of the target look (owner ruling, 2026-10-11). When a build reaches
that look (the owner judges this at a visual-v1 or later art checkpoint, side by side with
`promo/final/03-gameplay-1936.png`), a real screenshot takes its place:

1. Capture the build at the promo pose, 2000 px wide, as `docs/images/hero-build.jpg`. Keep
   `docs/images/hero-concept-1936.jpg`.
2. In `README.md` and `README.he.md`, point the hero `<img>` at `hero-build.jpg` with alt text describing the real
   build, and move the concept art down to a "Where it's going" line under the screenshots, still captioned as concept
   art.
3. In `ASSETS.yaml`, add a `docs/images/hero-build.jpg` row (`licence: CC-BY-4.0`, `status: verified`, source: the build
   version and date).
4. In `scripts/oss/tests/test_readme.py`, change `HERO` to `docs/images/hero-build.jpg`, and keep the assertion that
   any concept-art image carries the concept-art caption.
5. Add a CHANGELOG line, and land it as a normal pull request.

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
  The README build shots (`docs/images/build-m3-*.jpg`, 1568 x 752 and not 16:9) do not meet this minimum. Show the
  actual UI when useful; do not pass off `promo/final/03-gameplay-1936.png` or the AI likenesses in promo/final/04 and
  08 as gameplay. Review each screenshot's age-suitability flag individually; never mark a sensitive scene suitable for
  all ages just to gain placement.
- [ ] Prepare a trailer from the real build, not the M2 recruiting flythrough or concept-art target. The deterministic
  1920 x 1080, 60 fps capture and draft cut are the separate trailer part of #45, still pending; this checklist does
  not provide a capture script or a finished trailer. Record the build/seed and capture settings with the cut.
  Steam requires a trailer and recommends that the first one be mostly gameplay; this project's rule (real build only)
  is stricter. Upload up to 1920 x 1080 at 30 or 60 fps, 5,000+ Kbps, preferably .mp4 with H.264/AAC. The game cannot
  be released while a trailer is still encoding.
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
