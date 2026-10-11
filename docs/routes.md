# Railways and roads of 1948 (issue #35)

The map draws the railways and main roads of 1948, each from its sourced date, after the promo (`promo/final/03-gameplay-1936.png`,
`02-map-diorama.png`, and the 0:21 close-up of `promo/demo/out/demo-16x9.mp4`). Civilian boats are out of scope (owner).

## Files

| What | Where |
|---|---|
| Routes, sources, waypoints, status (hand-written) | `content/map/routes_1948.yaml` |
| Traced lines (generated) | `content/map/route_paths_1948.yaml` |
| Tracer | `tools/routes_1948.py` (tests: `tools/tests/test_routes_1948.py`) |
| Map layer | `unity/Assets/Scripts/Map/Routes/` (`RoutePaths`, `RouteLayer`, `RouteTrain`, `TrainModels`) |
| Shader and materials | `unity/Assets/Shaders/Route.shader`; `Resources/Routes/Rail.mat`, `Road.mat` (`RouteMaterialAssets.Create`) |
| Tests | `unity/Assets/Tests/EditMode/RouteTests.cs` |

Both route files are map-only. `ContentLoader` never reads them, so the content hash and the goldens do not move
(`RouteTests.TheRoutesDoNotMoveTheContentHash` loads the content with and without them). The game reads only the
traced file, so the WebGL manifest ships only that one: `ContentManifest.ToolInputs` leaves the hand-written
`routes_1948.yaml` out. If the traced file is missing or malformed (a stale manifest, an old content copy), the map
builds without routes and logs a warning (`RoutePaths.ReadOrNone`). A route with an impossible date (year 0000, a 13th
month) is skipped alone, with a warning.

## The data

**Source of every line:** the Survey of Palestine 1:250,000 map of 1946 (three sheets).
- It is public domain: PD-UKGov. It is a Mandate government survey published in 1946. Crown copyright on published
  works lasts 50 years from publication, and HMSO treats the expiry as worldwide. It is also PD-scan.
  - That is how Wikimedia Commons files these sheets, for example `File:Gaza_sh._04,_1946.jpg` (checked 2026-10-11).
- The scans and georeferencing are Palestine Open Maps' (credited in `NOTICE`).
  - Their About page calls the maps public domain under the UK Copyright, Designs and Patents Act. It publishes no terms
    of its own for the tiles or the georeferencing.
  - The traced lines are our own derivative and copy none of its layers.
  - Station coordinates fall on their map symbols to about 100 m.

**How a line is made.** `tools/routes_1948.py` traces each route from waypoint to waypoint.
- The waypoints are towns and stations that the cited sources name on the route. Their coordinates come from the
  Wikipedia API (`prop=coordinates`, as `settlements_1948.yaml`), or from Wikidata (CC0) where the article has none.
- Between two waypoints the line is the least-cost path over the scanned map. A pixel costs 1 on the route's own
  symbol: the red main road, or the black railway (thick, with white dashes). A thin dark line costs 4 for a railway
  (curves drawn thinner, as in the Sorek canyon). Any other pixel costs 40.
- The 1,050 mm narrow-gauge Jezreel Valley line is drawn thin, so it traces as a `track`.
- A waypoint first snaps to its symbol within 1.5 km. A waypoint off the line (a town circle, its name, a modern
  town's point such as Ashkelon's for al-Majdal) is reached by an out-and-back spur, which the tool cuts.
- The chain is smoothed over 15 pixels and simplified at 35 m. With 5 pixels, the close views showed a 100–200 m
  wobble: the trace hops between the two edges of a thick symbol.
- A hook at either end is cut. The line goes out to a waypoint just off the route's end and back; for example the Burma
  Road began "just east of Dayr Muhaysin".
- `--preview DIR` writes every traced segment over the map, which is how each was checked by eye.
- The tool records each trace's quality in the generated file, and `test_routes_1948.py` guards both values, so an
  invented chord cannot hide in a straight stretch:
  - `on_symbol`: the share of traced pixels on the route's own symbol, at least 0.85; today 0.87–1.00.
  - `max_gap_m`: the longest stretch of the kept line off that symbol, at most 800 m; today at most 632 m.
  - Those stretches are names printed over a road, and towns.
- Roads printed over the valleys' green tint read darker, about RGB (146, 64, 42) by the Sea of Galilee, so the road
  colour rule takes those too. The hill tint's orange stays out, because its green and blue differ by 60 or more.

**The routes.** Every route has `status: draft`: traced and checked by eye against the 1946 map, and reviewed once by a
staff and historian review (2026-10-11), whose fixes are in. `to_verify` lines say what the sources leave open. Every
note states only what its cited source says, and the source is named in the note.

**Dates.** Each route has a display window, `from` and optionally `to`. The map shows a route only while the game date
is inside it (review C1):
- `RouteLayer` builds one ribbon group per window.
- `RouteLayer.ShowAt` switches the groups. The game calls it with its date on every state change (`GameRoot`).
- `ShowAt` also remembers the date on the map (`MapScene.RouteDate`), so a map rebuilt mid-game draws its routes as at
  the game's date at once.
- It is presentation only: the sim does not read the routes, and no golden moves.
- `from` is the earliest date the sources support for the whole line as drawn. Where no source gives an earlier one, it
  is 1946, the map's date.

| Kind | Route | from | Notes |
|---|---|---|---|
| rail | Jaffa–Jerusalem railway | 1920-10-05 | Opened 1892. The Jaffa–Lydda track was removed in 1915, laid at 600 mm in 1918, and rebuilt to standard gauge (inaugurated 5 October 1920). Service stopped in the 1948 war; Ramla–Jerusalem trains resumed in March 1950. |
| rail | Main line, Lydda to Rafah | 1946 | Opened 1 November 1920 by Gaza and Majdal to Lydda. The direct Majdal–Isdud–Yibna–Lydda section drawn here came later, with no year in the source (to-verify). |
| rail | Eastern Railway, Lydda to Hadera | 1915 | The Tulkarm bypass is not drawn. Its date is to-verify: August 1948 (Palestine Railways) or 1949 (Eastern Railway). |
| rail | Main line, Hadera to Haifa | 1918 | "Reaching Haifa by the end of 1918" (Palestine Railways). In 1948 Israel's trains ran on it from Haifa to Hadera. |
| rail | Haifa–Acre and the Haifa–Beirut–Tripoli line | 1942 | Haifa–Acre opened 1913; the line north to Ras an-Naqura opened 1942. The Rosh HaNikra railway bridge was destroyed in late February 1948. |
| rail | Jezreel Valley railway, Haifa to al-Hamma | 1905-10-15 | Cut off from the Hejaz line since the June 1946 Night of the Bridges, then cut at Geva (2 March 1948) and Gesher (14 May 1948). The track stays drawn. |
| road | Jaffa–Jerusalem by Latrun and Bab al-Wad, the coastal road south to Rafah, Jerusalem–Hebron–Beersheba, Jerusalem–Nablus–Jenin–Nazareth, Tel Aviv–Tulkarm–Nablus, Haifa–Nazareth–Tiberias, Jerusalem–Jericho, Tiberias–Metula, Gaza–Beersheba, Beersheba–Auja al-Hafir | 1946 | Red main roads on the 1946 map. |
| road | Tel Aviv–Haifa | 1937 | The Hadera–Haifa section's date (OpenHistoricalMap), to-verify. |
| road | Haifa–Acre–Ras an-Naqura | 1933 | OpenHistoricalMap way dates. |
| track | Burma Road | 1948-06-14 | Completed on 14 June 1948. The first jeep attempt failed on the night of 30–31 May; a second got through on 31 May – 1 June. Drawn along the 1946 tracks (to-verify). It is hidden at the 15 May start. |

**Left out, on purpose.**
- The Afula–Nablus–Tulkarm branch: its service ended in 1932 and 1938 (Palestine Railways article). Freight on the
  Tulkarm–Nablus part ended in July 1946 (Jezreel Valley railway).
- The Ottoman Beersheba line: dismantled after the First World War.
- Branches with no 1948 source here: the Petah Tikva branch, and the port and quarry sidings.

**Accuracy, measured but nothing copied.** `uv run tools/routes_accuracy.py --fetch` reproduces this table. It
downloads the references into the git-ignored `tools/.cache/routes-accuracy/`.
- OpenHistoricalMap (CC0) has the Jaffa–Jerusalem railway with its dates.
- OpenStreetMap has today's lines and pieces of the abandoned ones. It is ODbL, which this repository's licence policy
  does not allow, so its geometry is used only to measure ours.

| Route | Reference | Median distance | Within 300 m |
|---|---|---|---|
| Jaffa–Jerusalem railway | OHM, 1890 ways | 68 m | 96% |
| Eastern Railway | OSM rail, current and abandoned | 112 m | 87% |
| Hadera–Haifa railway | OSM | 214 m | 84% |
| Jezreel Valley railway | OSM | 152 m | 83% |
| Haifa–Beirut railway | OSM | 242 m | 73% |
| Main line south | OSM | 139 m | 58% |
| Beersheba–Auja road | OHM roads valid in 1948 | 30 m | 100% |
| Gaza–Beersheba road | OHM | 185 m | 93% |

- The misses on the railways are the reference's, not the trace's. Today's lines were rebuilt off the old alignment:
  near Rosh HaAyin the 1946 line runs past Rantiya and Wilhelma, and south of Ashkelon it differs too. OSM has almost
  nothing in the Gaza Strip.
- The rest is the 1:250,000 sheet's generalisation of tight curves.
- OHM has 1948 roads only in patches, so most roads have no reference.

## How it is drawn

**Ribbons, not a texture.**
- The roads and railways are meshes draped on the terrain: one mesh per look and display window.
- The other way was to bake them into a texture the terrain shader samples, as promo-land does for its tracks
  (`tracks.png`). It was not taken, for three reasons:
  - A railway needs a coordinate along its line for the sleepers. A distance-field texture has only the distance across.
  - At promo-land's 316 x 421 m texel, a 680 m wide railway is about two texels.
  - The terrain shader and its bakes are being changed by promo-land and promo-sea at the same time, so a texture
    would mean shared-shader edits on three branches. The ribbons need no terrain change.
- The train needs the polyline anyway.

**Symbolic widths**, measured on promo 03 at gameplay03 (about 35 m a pixel at 1920 px), after the units' scale
measurement in `docs/units.md`:

| Feature | Promo | Here |
|---|---|---|
| Railway bed | about 16 px | 680 m (its soft edges take about a sixth) |
| Sleeper spacing | about 4 px | 150 m |
| Dirt road | about 8 px | 300 m |
| Burma Road | (not in the promo) | 200 m |
| Train (six cars) | about 250 px, about 8.8 km | 8.8 km: locomotive, tender and four coaches at 80x real length and 1.5x stockier across (at a uniform 65x the first checkpoint drew a thin dark line) |

**The mesh** (`RouteLayer.Ribbon`).
- A cross-section every 120 m, square to the line's direction averaged over two steps each way.
- Five vertices across each section. Each sits 20 m above the highest ground at its own point and half a step or half a
  spacing around it, and takes the terrain's normal.
- u runs across the ribbon and v along it, in metres.

**The shader** (`Statecraft/Route`). It is transparent and has no new keywords; the shadow keywords are the terrain's.
- The road is pale dirt with faint wheel ruts. At the country-wide pose (a pixel over 60–150 m), it darkens to a
  grey-brown line, so it reads as the promo diorama's roads do instead of vanishing on pale ground.
- The railway is a grey-brown ballast bed with darker shoulders, dark sleepers and two light steel rails, after the
  promo video's 0:21 close-up.
- Every pattern is box-filtered to the pixel, so it fades to its mean at the overview instead of shimmering.
- It is lit like the ground: the sun with its shadows and the ambient probe. Then come the hex grid and the land haze
  from `StatecraftMap.hlsl`.
- Edges are soft. The soft edge is at least a pixel wide, and a ribbon narrower than a pixel fades by its coverage, so
  far roads thin out instead of crawling (review M5).
- Two passes split each ribbon at half coverage (review M3):
  - The core (UniversalForward) writes a stencil bit. No later ribbon draws over it, so joins never double-blend.
  - The soft rim (SRPDefaultUnlit) only tests the bit. A faint edge therefore cannot notch the next ribbon's core.
  - The rail material draws first, so a railway stays on top of a road it crosses.

**The train** (`RouteTrain`, `TrainModels`) is a showcase, not normal play.
- **Controller ruling** (review I1, 2026-10-11): accuracy wins for facts. In normal play, no moving train runs where no
  service is sourced for that date.
- **Where the train appears:**
  - the promo and art-checkpoint screenshots (`?shot` / `-shot`) and editor renders, where it stands still;
  - `?train` or `?units` (desktop `-train` / `-units`), where it runs.
- **Normal play has none** (`RouteLayer.ModeFor`), for two reasons:
  - By the British withdrawal in May 1948, railway operations had effectively ceased. For the rest of 1948, Israel's
    services ran only around Haifa: south to Hadera and north to Kiryat Motzkin and later Nahariya (Palestine Railways).
    No specific 1948 date is sourced for that Haifa-area service.
  - Jaffa to Lydda crossed the front until Lydda was taken in July 1948. Ramla–Jerusalem trains resumed in March 1950.
- **How it is built:**
  - code-built painted miniatures, made with the units' `MiniatureMesh` and palette;
  - generic period shapes, with authored close-zoom models left to the AI-3D track;
  - the wheels ride the ribbon's own centre height (the same max-of-samples rule, review M4).
- **How it moves** in the showcase:
  - it shuttles between Jaffa and Lydda junction, the stretch the promo draws, at 300 m/s of map;
  - it stops for 6 s at each end, and the locomotive leads each way.
- **Still position:** its middle by Mikve Israel, heading inland, as in the promo.
- **A station correction:** the line's Tel Aviv station of 1920–1970 was Beit Hadar, "the original Tel Aviv South
  station" (Jaffa–Jerusalem railway). The later Tel Aviv South opened when Beit Hadar closed.

**Toggles.** `routes` (the ribbons) and `train` (when there is one) are visual toggles, so `?vprofile` prices each one.

**Kits keep off the routes** (settlement kits and trees, #179).
- `MapScene` reads the routes before building the kits, and hands `KitLayer.Build` every route's corridor
  (`RouteLayer.Corridors`): half the drawn width plus `CorridorMarginM` (100 m).
- `KitGround.OnRoute` (a 2 km segment grid) keeps building lots, landmarks and tree crowns out of these corridors.
  B9 still keeps trees out of building lots.
- Every route gets its corridor, whatever its date: the kits are built once, not per date, so the Burma Road's corridor
  stays clear from the start.
- `RouteTests.NoKitBuildingOrTreeStandsOnARoute` checks every lot and tree against every segment by brute force.

## Measurements

All of these are on `promo-roads`.

**Checkpoints.** `tools/art_checkpoint.sh promo-roads-r0`, `-r1` and `-r2`. Images are in
`captures/visual/promo-roads-r*/`, git-ignored. Close views come from `RouteCapture.Close`.

| | promo-sea r1 (main's last recorded checkpoint, docs/visual-v1.md) | r2 (this branch) | promo |
|---|---|---|---|
| gameplay03 saturation, contrast, warmth | 0.48 / 0.174 / 0.102 | 0.48 / 0.174 / 0.100 | 0.38 / 0.203 / 0.064 |
| gameplay03 palette distance | 0.60 | 0.58 | - |
| diorama02 saturation, contrast, warmth | 0.42 / 0.187 / 0.117 | 0.42 / 0.187 / 0.117 | 0.43 / 0.200 / 0.145 |

**Look test** (`RouteTests.AtTheGameplayPoseTheRailwayIsDarkAndTheRoadPale`, gameplay03, editor, review round):
- the railway renders RGB (0.46, 0.40, 0.31) over ground (0.62, 0.59, 0.33);
- the road renders (0.81, 0.71, 0.50) over ground (0.60, 0.57, 0.32).

**Frame time.** Headless `?vprofile`, 1920x1080 at DPR 1, `machine_lock.py --exclusive`.
- These numbers come from the first round, with the train shown in the profile.
- The machine was loaded by other sessions (load average 4.3).

| Pose | all | -routes | -train | none | routes cost | train cost |
|---|---|---|---|---|---|---|
| gameplay03 | 8.58 ms (p95 10) | 8.50 | 8.63 | 7.22 | 0.08 ms | below the noise (spread 0.18) |
| diorama02 | 14.58 ms (p95 17, spread 1.44) | 14.38 | 14.50 | 10.77 | 0.20 ms | 0.08 ms |

- **Read honestly** (review M6): the routes' and the train's deltas, 0.08–0.20 ms, are **below this run's noise**
  (spreads 0.09–1.44 ms). They show the layer is cheap: 89k triangles, about a dozen draw calls, no CPU copy, and six
  transforms a frame for the showcase train. They do not measure its cost to 0.1 ms.
- diorama02's p95 is 17 ms, over the 16.7 ms budget. It is also 17 ms with `-routes`, so it is consistent with the base
  scene on a loaded machine, but at 1 ms p95 granularity that is not proved.
- The idle-machine re-measure is centralised (controller ruling) and is not repeated here.

**Size.** The meshes, after the review round:
- rail 18,100 vertices and roads 37,515;
- 88,832 triangles in all, in 10 draw calls (one per display window and look);
- uploaded without a CPU copy.

The train is about 1,100 triangles; that figure is counted from the builders, not measured, and `TrainModelsAreRepeatable` bounds it under 5,000. The data file is 55 KB.

**Tests.** `RouteTests` covers:
- the data parses, and bad files name the file and the problem (dates included);
- the content hash is unchanged, and the hand-written file is left out of the WebGL manifest;
- resampling;
- the ground clearance and bounds of the ribbons, and a pinned SHA-256 of the rail mesh. A fresh editor process must
  build the same geometry, bit for bit.
- the toggles, and the teardown freeing the meshes, the train's material and its palette texture;
- the Burma Road, hidden at 15 May 1948 and shown from 14 June 1948 (and hidden again for an earlier date);
- the window bounds;
- a missing or malformed route file, which gives no routes, a warning and a map that still builds;
- the train modes: still for `?shot` and editor renders, moving for `?train` and `?units`, none in normal play;
- the still train's place and heading, on the ribbon's own height;
- the shuttle's motion;
- the train models;
- GLES3 for the shader's two passes, and the materials' order;
- the look at gameplay03.

`tools/tests/test_routes_1948.py` checks the data against its sources, the windows (the Burma Road from 1948-06-14), the trace
quality guards (`on_symbol`, `max_gap_m`), the validator (dates, a boolean `opened`), the tracer's pure parts, and a
synthetic trace (where scikit-image is installed).

## Integration with promo-land

promo-land (`tools/land_bake.py`, `tracks.png`, `StatecraftLand.hlsl`) draws 110 m dust lines between towns. Its
`track_links` joins every settlement to its three nearest within 40 km, and `track_points` bends each link by two
seeded sine meanders. Those lines are invented routes. Where they do follow a real road, they would draw a second,
thinner road beside this branch's ribbon. The proposal, for promo-land's next round:

1. **Drop the town-to-town tracks from `tracks.png`** (`track_links`, `track_points`, `track_offsets`, the B channel
   and `ScTrackHalfM`'s line in `ScLandUse`).
   - The real 1948 roads are this branch's ribbons.
   - Keep promo-land's field-scale "block-edge tracks", painted into `landuse.png`. They are farm texture, not routes,
     and sit under the ribbons without competing.
2. **If a finer network is wanted later**, trace it rather than invent it. The 1946 sheet draws secondary roads and
   cart tracks as double dashed lines.
   - `tools/routes_1948.py` already traces those (`kind: track`, as the Burma Road).
   - `land_bake.py` could then read `content/map/route_paths_1948.yaml` (`kind: track` only) instead of
     `track_links`, keeping its texture and shader exactly as they are.
3. **Nothing else overlaps.**
   - The ribbons are a transparent pass over whatever the terrain draws, so no shader or texture file is shared.
   - Merging in either order needs no conflict resolution beyond `CHANGELOG.md`.

## What still differs from the promo

- **The towns.** The promo's line runs out of a dense Tel Aviv and Jaffa past orange groves; here it leaves
  placeholder boxes over bare plain. That is the settlement and land-use tracks' job.
- **Close zoom.** The promo video's 0:21 rail bed is raised and stony and its sleepers are timber blocks. Here they are
  flat and painted, with lighting from the terrain's normals only. Shadows are received, but the bed casts no edge
  shadow. A low embankment mesh, or a normal tilt at the shoulders, is the next step if the close zoom matters.
- **The train.** It is generic code-built miniatures, as the units are: no smoke, no wheels, no Palestine Railways
  livery source. Authored close-zoom models belong to the AI-3D track; `TrainModels` is the far-zoom fallback.
- **Roads in the hills.** At gameplay03 they read clearly, but in the Judean hills the hill shading partly hides them.
  The promo has no 1948 hill roads to compare against.
- **The country-wide view.** The far grey-brown roads fit promo 02's road lines, but promo 02 draws modern highways;
  1948 had no such network.
- **The Burma Road** is drawn along the 1946 tracks (to-verify), from 14 June 1948.
- **Not drawn:** Israel's Tulkarm bypass (its date is to-verify), and lines and roads with no 1948 source here.
- **No train in normal play.** The promo's moving train appears only as a showcase (`?train`, `?units`, the
  screenshots). A sourced, dated 1948 Haifa-area service could add one in normal play later.
- **Broken bridges.** The 1948 cuts (Rosh HaNikra in late February, Geva on 2 March, Gesher on 14 May) are in the
  notes, not drawn. The track still lay on the ground.
