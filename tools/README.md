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
- `hexgrid.py` writes `content/map/hexes.json` from the heightmap and those committed precipitation inputs (no network).
- `settlement_hexes.py` writes `content/map/settlement_hexes.yaml` (committed): the hex that owns each settlement, by the
  `hexgrid.settlement_hex` rule. The sim reads it, so it needs no projection maths.
- `routes_1948.py [--fetch] [--only ID ...] [--preview DIR]` traces the railways and main roads of May 1948 (issue #35)
  on the Survey of Palestine 1:250,000 map of 1946 (public domain; Palestine Open Maps' tiles, downloaded with `--fetch`
  into `tools/.cache/pal250k-1946/`, not committed): it reads the hand-written routes, sources and waypoints in
  `content/map/routes_1948.yaml` and writes the traced lines to `content/map/route_paths_1948.yaml` (committed; the map
  draws it, the sim never reads it). `--preview` writes each traced segment over the map for checking by eye.
- `localities_bake.py [--check]` bakes `content/map/localities_1948.yaml` (the sourced 1948 localities, edited by hand)
  into `content/map/localities_1948.tsv` (committed), the compact runtime form the map reads at boot. Rerun it after
  editing the YAML: `test_localities.py` fails a stale bake, and `--check` does the same.
- `routes_accuracy.py [--fetch]` measures how far those traced lines lie from OpenHistoricalMap (CC0) and OpenStreetMap
  (ODbL) lines, downloaded with `--fetch` into `tools/.cache/routes-accuracy/` (never committed or copied into content),
  and prints the accuracy table of `docs/routes.md`.
- `web_check.py` serves a WebGL build on http://localhost and opens it in headless Chrome for Testing (Playwright's
  cache; macOS only, arm64 or x64); it passes when the console shows the expected lines (`GOLDEN OK ...`, `SMOKE OK ...`).
- `package_release.py` zips a Brotli WebGL build for sharing (`unity/Builds/statecraft-m3-web.zip`) and refuses
  (exit 1) until every sensitive event is `sensitivity-reviewed` (docs/m3-release.md). The files under `Build/` are
  named by their content hash (issue #437); it reads their names from the build's `index.html`, zips only those and
  refuses a build that is missing one.
- `release_build.py [--platform mac|linux|windows|all] [--out DIR] [--dev] [--dry-run]` builds the desktop players
  under the machine lock, runs the macOS player's golden check and writes a build record outside the repository
  (`docs/RELEASING.md`, "Desktop builds"). It never commits, pushes or uploads.
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
- `terrain_bake.py` writes `unity/Assets/Textures/Map/terrain_bake.png` (AO, curvature, sky-view), `ao.png` (the AO
  alone, imported uncompressed) and `coast.png`
  (exact signed Euclidean distance to the shoreline; lakes under 1 km2 count as land) from the committed heightmap (no
  network). Re-run it whenever the heightmap changes; its PNG bytes are reproducible only on the same toolchain.
- `land_bake.py` writes `unity/Assets/Textures/Map/landuse.png` (the land-use colour and weight: field parcels with
  farm tracks, garrigue, terraces and scrub, placed by slope, rainfall and towns) from the heightmap, the CHELSA rainfall,
  the hex biomes and the 1948 settlements (no network); `StatecraftLand.hlsl` lays it over the biome ground. Re-run it
  whenever one of those inputs changes.
- `terrain_graph.py` writes `unity/Assets/Shaders/StatecraftTerrain.shadergraph` from Unity's "Terrain Simple" template
  with one Custom Function node (`StatecraftTerrain.hlsl`); re-run it only when that function's inputs or outputs change.
- `art_compare.py` puts screenshots next to the promo image of their pose, writes deuteranopia and protanopia copies and
  logs rough colour numbers; `art_checkpoint.sh [--no-build] <name>` builds WebGL, takes the screenshots
  (`web_check.py --shots`) and runs it (docs/visual-v1.md).
- `machine_lock.py` serialises frame-time measurements against every other Unity job on the machine (all worktrees,
  batch runs and open editors).
- `headed.py` drives a WebGL build in a headed Chrome for Testing over CDP (real GPU, real mouse events, a temporary
  profile deleted on exit): `boot` (?bootlog phase timings, optional CPU throttling), `navpairs` (same-frame click pairs
  under ?navlog), `loadshot` (screenshots during the load) and `profile` (visual v1's `?vprofile` in a headed window at a
  chosen DPR); `boot --runs N --baseline MS` checks the boot budget. Its browser test runs only with `pytest --headed`.

## Running the tests

Run from the repo root; this installs every pinned dependency (rasterio included) and runs all tools tests:

```
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q
```

Expected: `722 passed, 4 skipped` (one waits for `content/sites.yaml`, two need skimage and scipy, and one is the headed browser check; add `--headed` and `--with websockets==15.0.1` to run it). Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

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
