# tools/

Offline data tools (uv inline-script Python). Run from the repo root.

- `dem_to_heightmap.py` downloads Copernicus GLO-30 tiles into `tools/.cache/dem/` (not committed) and writes
  `unity/Assets/StreamingAssets/map/heightmap.r16z` + `heightmap.json`. The `.r16z` is the 16-bit grid packed losslessly
  by `heightmap_codec.py` (row deltas in byte planes, one zlib stream: 4.1 MB instead of 8.4 MB, issue #44); every tool
  reads it through `heightmap_codec.read`, and the game decodes the same layout (`HeightmapCodec.cs`).
- `precip_clip.py` reads the map window of CHELSA V2.1 bio12 (mean annual precipitation 1981-2010) in place over HTTP and
  writes `content/map/precip_bio12.u16` + `precip_bio12.json` (committed).
- `ims_normals.py` downloads Israel Meteorological Service rain-gauge records from data.gov.il into `tools/.cache/ims/`
  (not committed) and writes the 1981-2010 station normals `content/map/precip_stations.json` (committed).
- `slim_collection.py --src DIR --out unity/Assets/Art/Collection` (issue #664) slims the 31 non-building models of The
  1948 Collection and its nine whole-town models (MATKOT's Tripo models from PR #507; issue #663; owner ruling of 15 October 2026: "Plain git, slimmed first") into the
  files the game ships: per model a far mesh of at most 1,500 triangles (4,500 for vehicles), a near mesh of at most 8,000 (skinned, with only
  its idle clip, on the 13 models that have one) and a 512 px colour map. It runs Blender as the `bpy` module
  (`uv run --no-project --python 3.13 --with bpy==5.2.2 --with pillow==12.3.0 python -I tools/slim_collection.py ...`);
  the source is #507's `Models/` folder at `3ef0a7a5` (the command is in the script). Then run
  `CollectionImport.Run` in Unity for the import settings and prefabs.
- `hexgrid.py` writes `content/map/hexes.json` from the heightmap and those committed precipitation inputs (no network).
- `settlement_hexes.py` writes `content/map/settlement_hexes.yaml` (committed): the hex that owns each settlement, by the
  `hexgrid.settlement_hex` rule. The sim reads it, so it needs no projection maths.
- `routes_1948.py [--fetch] [--only ID ...] [--preview DIR]` traces the railways and main roads of May 1948 (issue #35)
  on the Survey of Palestine 1:250,000 map of 1946 (public domain; Palestine Open Maps' tiles, downloaded with `--fetch`
  into `tools/.cache/pal250k-1946/`, not committed): it reads the hand-written routes, sources and waypoints in
  `content/map/routes_1948.yaml` and writes the traced lines to `content/map/route_paths_1948.yaml` (committed; the map
  draws it, the sim never reads it). `--preview` writes each traced segment over the map for checking by eye.
- `regions_bake.py [--check]` bakes the region files `content/map/subdistrict_hexes_1948.yaml`,
  `armistice_hexes_1949.yaml` and `control_hexes_1948_49.yaml` into `.tsv` runtime forms beside them (committed), which
  the map reads at boot (issue #321). Rerun it after `borders_bake.py`, `armistice_bake.py` or `control_bake.py`
  rewrites one: `test_regions_bake.py` fails a stale file, naming it, and `--check` does the same.
- `route_paths_bake.py [--check]` bakes `content/map/route_paths_1948.yaml` into `content/map/route_paths_1948.tsv`
  (committed), the compact runtime form the map reads at boot (issue #321). Rerun it after `routes_1948.py`:
  `test_route_paths_bake.py` fails a stale bake, and `--check` does the same.
- `localities_bake.py [--check]` bakes `content/map/localities_1948.yaml` (the sourced 1948 localities, edited by hand)
  into `content/map/localities_1948.tsv` (committed), the compact runtime form the map reads at boot. Rerun it after
  editing the YAML: `test_localities.py` fails a stale bake, and `--check` does the same.
- `routes_accuracy.py [--fetch]` measures how far those traced lines lie from OpenHistoricalMap (CC0) and OpenStreetMap
  (ODbL) lines, downloaded with `--fetch` into `tools/.cache/routes-accuracy/` (never committed or copied into content),
  and prints the accuracy table of `docs/routes.md`.
- `web_check.py` serves a WebGL build on http://localhost and opens it in headless Chrome for Testing (Playwright's
  cache; macOS only, arm64 or x64); it passes when the console shows the expected lines (`GOLDEN OK ...`, `SMOKE OK ...`).
- `player_golden_check.py` is the logic of `.github/workflows/player-golden.yml`: `plan` lists the desktop players a
  GitHub Release carries (their asset names are the contract with the release script) and refuses a partial set, and
  `run` checks one against GitHub's SHA-256 digest for the asset, unpacks it, checks the notices sit beside the player, runs it
  with `-batchmode -nographics -golden` and passes only on exit 0 with every `GOLDEN OK` line from the golden manifest,
  printing the player log otherwise. Stdlib only; no Unity and no licence needed.
- `package_release.py` zips a WebGL release build (gzip, `unity/Builds/Web-gzip`; #879) for sharing (`unity/Builds/statecraft-m3-web.zip`) and refuses
  (exit 1) until every sensitive event is `sensitivity-reviewed` (docs/m3-release.md). The files under `Build/` are
  named by their content hash (issue #437); it reads their names from the build's `index.html`, zips only those and
  refuses a build that is missing one.
- `release_build.py [--platform mac|linux|windows|all] [--out DIR] [--tag vX.Y.Z] [--dev] [--dry-run] [--unity PATH]`
  builds the desktop players under the machine lock, runs the macOS player's golden check and writes a build record
  outside every checkout (`docs/RELEASING.md`, step 5, "Desktop builds"). A release must be `main` at its tag; `--dev` builds
  anywhere under a `-dev.<commit>` name. It never commits, pushes or uploads.
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
- `terrain_bake.py` writes `unity/Assets/Textures/Map/terrain_bake.png` (AO, curvature, sky-view), `ao.png` (the AO
  alone, imported uncompressed) and `coast.png`
  (exact signed Euclidean distance to the shoreline; lakes under 1 km2 count as land; on the dune coasts its land side is
  beach-scaled for the wider beach, #388, except over each settlement's own ground) from the committed heightmap and
  the settlement, locality and population files in `content/map/` (no network). Re-run it whenever any of them changes
  (`test_the_committed_coast_is_the_bake_of_todays_settlements` fails on a stale `coast.png`); its PNG bytes are
  reproducible only on the same toolchain.
- `land_bake.py` writes `unity/Assets/Textures/Map/landuse.png` (the land-use colour and weight: field parcels with
  farm tracks, garrigue, terraces and scrub, placed by slope, rainfall and towns) from the heightmap, the CHELSA rainfall,
  the hex biomes and the 1948 settlements (no network); `StatecraftLand.hlsl` lays it over the biome ground. Re-run it
  whenever one of those inputs changes.
- `terrain_graph.py` writes `unity/Assets/Shaders/StatecraftTerrain.shadergraph` from Unity's "Terrain Simple" template
  with one Custom Function node (`StatecraftTerrain.hlsl`); re-run it only when that function's inputs or outputs change.
- `art_compare.py` puts screenshots next to the promo image of their pose, writes deuteranopia and protanopia copies and
  logs rough colour numbers, leaving out the HUD (`HUD_MASK`) of both images (rows named `<name>-hudmask`), and
  prints diorama02's warmth per material (sand, sea, wood) against promo 02's (#695);
  `art_checkpoint.sh [--no-build] <name>` builds WebGL, takes the screenshots (`web_check.py --shots`) and runs it
  (docs/visual-v1.md).
- `coast_measure.py <shots...> --window south|north [--promo] [--check]` measures the beach width, foam band, lace
  lines, striped rows and sand lightness across the coast of a gameplay03 screenshot (or promo 03's same coast), the
  median over several shots, against bands set from promo 03 (#388).
- `machine_lock.py` serialises frame-time measurements against every other Unity job on the machine (all worktrees,
  batch runs and open editors).
- `unity_postmerge.py` runs the full Unity EditMode suite on each new `main` commit, on the owner's Mac, and reports a
  failure as one issue while `main` stays broken, with a comment for each newly failing commit (T72; details in
  "Post-merge Unity run" below).
- `headed.py` drives a WebGL build in a headed Chrome for Testing over CDP (real GPU, real mouse events, a temporary
  profile deleted on exit): `boot` (?bootlog phase timings, optional CPU throttling), `navpairs` (same-frame click pairs
  under ?navlog), `loadshot` (screenshots during the load) and `profile` (visual v1's `?vprofile` in a headed window at a
  chosen DPR); `boot --runs N --baseline MS` checks the boot budget. Its browser test runs only with `pytest --headed`.
- `yaml12.py` is how the tools, scripts and their tests read YAML. `yaml12.load` resolves plain scalars by the YAML 1.2
  core schema and refuses duplicate keys, where `yaml.safe_load` follows YAML 1.1 (`yes` becomes True, `1:30` becomes
  90, `2026-10-15` becomes a date). That matches YamlDotNet's reads into string fields, not its bool and int fields,
  which also take `yes`, `y`, `off`, `017`, `1_000` and `1:30`. `yaml12.ambiguous` lists those scalars, and
  `test_yaml12_data.py` keeps them out of the game's YAML. `load_raw` keeps every scalar as its text, and
  `uv run tools/yaml12.py FILE...` lists both kinds of hit. `test_yaml12_guard.py` fails on a new PyYAML load in
  `tools/`, `scripts/` or `.github/` outside its allowlist.

## Running the tests

Run from the repo root; this installs every pinned dependency (rasterio included) and runs all tools tests:

```
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q
```

Expected: `917 passed, 4 skipped` (two need skimage and scipy, and two are the headed browser checks; add `--headed` and `--with websockets==15.0.1` to run them). Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

## Required attribution (Copernicus DEM licence, modified data)

produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

This notice must also appear in the game credits and the Steam page legal notes.

## Required attribution (precipitation inputs)

Precipitation: CHELSA V2.1 bio12 (1981-2010), CC0 1.0. Karger, D.N., Conrad, O., Böhner, J., Kawohl, T., Kreft, H.,
Soria-Auza, R.W., Zimmermann, N.E., Linder, H.P. & Kessler, M. (2021) Climatologies at high resolution for the earth's land
surface areas. EnviDat. https://doi.org/10.16904/envidat.228.v2.1 ; Karger et al. (2017) Scientific Data 4, 170122.

Rain-gauge data: Israel Meteorological Service (השירות המטאורולוגי), meteorological data archive, data.gov.il dataset 481
(https://data.gov.il/dataset/481), licence "Other (Open)" as stated on the dataset.

CHELSA is CC0 (no attribution required); both are credited here and should be credited in the game credits alongside the
Copernicus notice.

## Manifest AI-disclosure review text (#45)

`uv run scripts/oss/ai_disclosure.py --output docs/ai-disclosure.md` regenerates the checked-in review text from
`ASSETS.yaml` and `ASSETS.portraits.yaml`. It groups only explicit `ai_generated: true` rows by media kind,
keeps source/status and manifest names, and includes the README's no-live-generation statement. It does not
change flags, clear licences, expand asset patterns into filenames, or answer the store content survey.

## Steam screenshot dimensions (#45)

Before the owner reviews a proposed set of store screenshots, run:

```bash
uv run tools/store_screenshots.py path/to/shot1.png path/to/shot2.png path/to/shot3.png path/to/shot4.png path/to/shot5.png
```

This refuses unreadable images, fewer than five distinct paths, dimensions below 1920 x 1080, or a non-16:9 ratio.
Repeated paths do not satisfy the count. It does not compare image contents: copies at different paths can still pass.
`DIMENSIONS OK` is not proof of real gameplay, rights, age suitability or store acceptance. The owner still reviews
what each screenshot shows and records the build commit/zip SHA-256 and capture date. This does not capture anything,
change a release gate or submit a store page. Steam's [official screenshot requirements](https://partner.steamgames.com/doc/store/assets/standard)
were checked 11 October 2026; re-check before submission. These are not itch.io requirements.

## Post-merge Unity run (T72)

CI cannot run Unity (CONTRIBUTING.md, "What CI runs"), so two pull requests can each pass and still break Unity once
both are on `main`. `tools/unity_postmerge.py` closes that gap after the merge, on the owner's Mac, from a launchd job
(`tools/unity_postmerge.plist.in`, every 15 minutes).
- launchd starts `unity_postmerge_launch.py` (a copy in the job folder, `~/.cache/statecraft/unity-postmerge`). It
  fetches `origin/main`, moves the job's own worktree (`~/.cache/statecraft/unity-postmerge/worktree`) to the tip and
  runs `tools/unity_postmerge.py` from there. So a merged change to the script takes effect on the next run, and no
  checkout's working tree is used or changed.
- When the tip is not the commit it last tested, the script cleans the worktree (keeping the Library, capped at 6 GB),
  runs the whole EditMode suite under `machine_lock.py --shared --wait-unity` and reverts the settings churn. A Unity
  still running 90 minutes after it started (`--unity-timeout-min`) is stopped by the PID `machine_lock.py` recorded
  for it (SIGTERM, then SIGKILL), and the run counts as timed out.
- A pass adds one line to `~/.cache/statecraft/unity-postmerge/results.log`. Anything else, 0 tests included, is
  reported once per commit with the failing tests and the log tail: as a new issue (label `area: unity`), or, while an
  issue it opened is still open, as a comment on that issue. It tests the tip only; the report lists the commits since
  the last tested one.
- Before the log tail, compile errors, test messages, the broken-run reason (git's stderr, for one) and the commit
  list go into the public issue, email addresses, the home folder (to `~`), any `/Users/<name>`, Unity's
  `LicenseClient-<name>` and the user name are redacted. If redacting fails, the report says "Log withheld" and
  carries none of them.
- Fetch and `gh` failures, and a launcher that cannot start the script, are written to `launchd.log` in the job folder.
  When `main`'s tip still has no PASS or FAIL line in `results.log` 6 hours after it was committed (the 4 h lock wait,
  90 min of Unity and 30 min to spare), the job appends one `STALE` line for that commit to `results.log`, so a
  stuck job or an expired `gh` login shows there. A job that does not run at all (launchd, `uv` or the fetch broken)
  writes nothing: a `results.log` whose last line is older than `main`'s tip by more than 6 hours means that.

Install (owner's Mac only, from a checkout of `main` that has these files):

```bash
job=~/.cache/statecraft/unity-postmerge
mkdir -p "$job" && cp tools/unity_postmerge_launch.py "$job/"
sed -e "s#@UV@#$(command -v uv)#g" -e "s#@REPO@#$HOME/israel-game#g" -e "s#@HOME@#$job#g" \
    -e "s#@PATH@#$(dirname "$(command -v uv)"):$(dirname "$(command -v gh)"):$(dirname "$(command -v dotnet)"):/usr/bin:/bin:/usr/sbin:/sbin#g" \
    tools/unity_postmerge.plist.in > ~/Library/LaunchAgents/com.statecraft.unity-postmerge.plist
plutil -lint ~/Library/LaunchAgents/com.statecraft.unity-postmerge.plist
launchctl bootstrap gui/$(id -u) ~/Library/LaunchAgents/com.statecraft.unity-postmerge.plist
```

`@REPO@` is the main checkout: the launcher only fetches there and clones its Library once. A change to the launcher
itself needs the `cp` again; a change to `unity_postmerge.py` does not.

Uninstall: `launchctl bootout gui/$(id -u)/com.statecraft.unity-postmerge`, then delete the plist. To remove the
worktree too: `git worktree remove --force ~/.cache/statecraft/unity-postmerge/worktree`. A trial without opening an
issue or moving the state: `uv run tools/unity_postmerge.py --dry-run --home <scratch folder>`.
