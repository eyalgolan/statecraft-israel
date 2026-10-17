# Railways and roads of 1948 (issue #35)

The map draws the railways and main roads of 1948, each from its sourced date, after the promo (`promo/final/03-gameplay-1936.png`,
`02-map-diorama.png`, and the 0:21 close-up of `promo/demo/out/demo-16x9.mp4`). Civilian boats are out of scope (owner).
On 2026-10-14 the owner reversed that for civilian boats ("Keep civilian boats"); they are map art in #536, not routes.

## Relationship to location authoring

The [Locations specification](locations.md) defines map-production priorities; [Corridors](gdd/corridors.md) keeps
the required construction routes and their reference-date decisions. Optional additional work on a town or immigrant
camp does not make a required station or route waypoint optional. The MVP railway is the 1949 link via Rosh HaAyin, not
the later coastal line (Q-RAIL-1, decided by the owner on 17 October 2026, [#782](https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128)).

The [Map editor](gdd/map-editor.md) must report dependencies when a location or corridor endpoint moves. Authored
gameplay placement is separate from sourced waypoint coordinates and traced geometry. A marker move must not silently
retrace a route, replace historical source evidence or break a sim corridor's hex bindings. Resolve the dependency in
the appropriate authoring inputs, then regenerate and validate affected traces, segment bindings and baked outputs.

## Files

| What | Where |
|---|---|
| Routes, sources, waypoints, status (hand-written) | `content/map/routes_1948.yaml` |
| Traced lines (generated) | `content/map/route_paths_1948.yaml` |
| Tracer | `tools/routes_1948.py` (tests: `tools/tests/test_routes_1948.py`) |
| Map layer | `unity/Assets/Scripts/Map/Routes/` (`RoutePaths`, `RouteLayer`, `RouteTrain`, `TrainModels`) |
| Shader and materials | `unity/Assets/Shaders/Route.shader`; `Resources/Routes/Rail.mat`, `Road.mat` (`RouteMaterialAssets.Create`) |
| Runtime form (generated) | `content/map/route_paths_1948.tsv` (`tools/route_paths_bake.py`; tests: `tools/tests/test_route_paths_bake.py`) |
| Baked ribbons (generated) | `unity/Assets/Resources/Routes/RouteRibbons.bytes` (`RouteRibbonBake.Bake`; tests: `tools/tests/test_route_ribbon_bake.py`) |
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
- The chain is smoothed over 15 pixels, resampled every 40 m, smoothed again and simplified at 35 m.
- The second smoothing is a curvature-adaptive Gaussian (`gaussian_smooth`, review B14 / #212).
  - The trace hops between the two edges of a thick symbol, which drew kinks every few hundred metres. Hadera–Haifa had
    20 zigzags in 92 points; the main line south had 54 in 185.
  - A wobble's turns cancel out over ±320 m; a real bend's add up. So a straight stretch takes sigma 250 m, and a bend
    of 30–90° net turn eases to 60 m.
  - That removes the wobble. The Sorek meanders keep their shape: the Jaffa–Jerusalem line traces 79.4 km against
    80.7 km before.
  - The accuracy table is unchanged within a few metres (Jaffa–Jerusalem median 64 m, 96% within 300 m).
  - `test_no_sawtooth_on_straight_lines` bounds the zigzags: at most 3 on the plain railways, and at most 15% of points
    anywhere. The hill roads and the Jericho descent have real hairpins.
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
| Jaffa–Jerusalem railway | OHM, 1890 ways | 64 m | 96% |
| Eastern Railway | OSM rail, current and abandoned | 108 m | 87% |
| Hadera–Haifa railway | OSM | 217 m | 88% |
| Jezreel Valley railway | OSM | 153 m | 83% |
| Haifa–Beirut railway | OSM | 241 m | 75% |
| Main line south | OSM | 145 m | 58% |
| Beersheba–Auja road | OHM roads valid in 1948 | 39 m | 100% |
| Gaza–Beersheba road | OHM | 187 m | 95% |

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
| Dirt road | about 8 px | 420 m (its pale core, inside the soft verges, about 280 m) |
| Burma Road | (not in the promo) | 200 m |
| Train (six cars) | about 250 px, about 8.8 km | 8.8 km: locomotive, tender and four coaches at 80x real length and 1.5x stockier across (at a uniform 65x the first checkpoint drew a thin dark line) |

**Jerusalem's Old City** (#319). The kits draw the Old City as a walled square 4 km across
(`SettlementKits.OldCityWalls`), about 4 times the real one. The four roads that start there in the traced data
(Jaffa–Jerusalem, Hebron, Nablus and Jericho) meet at a point about 60 m west of New Gate, outside the real walls, and
none crosses the real walled city, but near the city they fall inside the drawn square. The data keeps the real lines; the map cuts them instead (`RouteLayer.Drawn`):
- each road's run inside the square is cut off at the wall line, where the walls leave a gate for it;
- only the ribbons are cut; the corridors the kits and the units keep clear of stay on the traced lines, so no building
  or unit moves, and inside the walls the roads' clear ways read as the streets from the gates;
- the Jaffa–Jerusalem railway is cut the same way (owner, 16 October 2026: "Cut it at the walls"). Its station was outside
  the real walls, south-west of Jaffa Gate, but falls inside the drawn square, so the railway ends at the south wall;
- only a line's ends are cut, so a road or railway traced through the square would fail
  `RouteTests.TheRoadsAndTheRailwayStopAtTheOldCityWalls` and `test_no_road_or_railway_runs_inside_the_old_city_walls`.

**The mesh** (`RouteLayer.Ribbon`).
- A cross-section every 120 m, square to the line's direction averaged over two steps each way.
- Five vertices across each section.
- **Roads drape as on main.** Each vertex sits 20 m above the highest ground at its own point and half a step or half
  a spacing around it, and takes the terrain's normal there (`DrapeHeight`).
  - Their pale band has no rails or sleepers to warp.
  - Narrowing them made every hill road about 40% thinner and beaded its width (review A-I1).
- **Railways keep their own grade** (review B15 / #213). Each section is flat across at `GradeHeight`: 20 m above the
  highest ground at the line and 30 m along it either way, with nothing sampled across.
  - **The old rule.** Each vertex took the highest ground around itself, so in the Sorek gorge the 680 m band rode up
    both walls and its rails and sleepers warped.
  - **Width.** It narrows to the gorge floor and ramps: `RailHalfWidths`.
    - Per section, `FlatHalfWidth` looks for ground more than 40 m above the band or more than 60 m below it, out to
      the band's edge on each side.
      - Where it finds such ground on both sides (a gorge floor, a gorge side, a ridge), it cuts the band back
        symmetrically to the last sample before the first one, down to 25% of the width.
      - Where only one side has it (one hill or one drop beside the line), the band keeps its full width. That edge reads
        as a cutting or an embankment (#252).
        - Cutting both sides for it had narrowed the foothill lines: on main (f974c9f), 85% of the narrowed
          Jaffa–Jerusalem sections and 93% of the Jezreel Valley ones failed on one side only. That covers the Latrun
          foothills, the Carmel foot by Haifa and the Jordan terraces.
        - The smoothing below cost only 5–7 points.
      - An edge under a little higher ground reads as a cutting.
      - A stricter rule (any rise over the clearance) also thinned the band on the rolling Shephelah foothills.
    - Then it takes the minimum over two sections each way.
    - Then it limits the change to 5% of the width per 120 m step, in forward and backward passes that only narrow.
    - So the width never jumps by more than a tenth between sections (review A-I1).
  - **Lift.** A section is raised where needed, so the ground never rises under its rails (±30% of the half-width)
    and the rails clear it by 3 m (review A-I2). The narrowed gorge band had buried the rails by up to 32 m.
    - Clearing the whole sleeper span instead lifted the band a median 39 m off the gorge floor, so the sleepers' outer
      ends may meet a wall, as in a cutting.
  - **Shading.** Every vertex takes the terrain's normal at the line.
  - **The train** rides `GradeHeight`.
  - **Tests.**
    - `TheRailwayKeepsItsGradeInTheSorekGorge`: every section is flat. Main's rule warped the band by a median 315 m
      across. The band now sits a median 10 m over the ground at the line (p90 25 m, max 40 m; bounds 12 m and 35 m).
    - `TheRailClearsTheGroundUnderItsRailsAndRampsItsWidth`: on every railway, no ground over the rails, no width
      jump over 10%, and the edges' float bounded (p99 85 m, bound 100 m). It fails on 005e60e: 67 sections with ground
      over the rails, 819 width jumps and an edge float p99 of 107 m.
    - `EveryRailwayKeepsItsWidthOutsideTheGorges` (#252): every line keeps at least 90% of its width, on average, outside
      the gorges (sections where the ground leaves the tolerance on both sides), and its mean width stays at its pinned
      floor. The edge float p99 stays 86 m; the Sorek numbers do not move.

      | Line | Mean width, main (f974c9f) | Now | Outside the gorges, main | Now | Sections in gorges |
      |---|---|---|---|---|---|
      | Jaffa–Jerusalem | 70.7% | 76.2% | 87.4% | 94.9% | 178 of 665 (the Sorek) |
      | Main line south | 98.9% | 100.0% | 98.9% | 100.0% | 0 of 856 |
      | Eastern Railway | 99.3% | 100.0% | 99.3% | 100.0% | 0 of 525 |
      | Hadera–Haifa | 92.0% | 97.4% | 93.5% | 99.0% | 9 of 419 |
      | Haifa–Beirut | 95.1% | 95.5% | 97.0% | 97.4% | 9 of 326 |
      | Jezreel Valley | 74.5% | 82.5% | 82.8% | 92.1% | 113 of 775 (the Beisan–Jordan descent, the Yarmuk, the Carmel foot) |
- u runs across the ribbon and v along it, in metres.

**The shader** (`Statecraft/Route`). It is transparent and has no new keywords; the shadow keywords are the terrain's.
- The road is a wide, pale dirt track (refs #35, `roads-dirt`): cream dirt, varied with a warmer dust in patches
  hundreds of metres long (two octaves of map-position noise, `RoadPatchM` 900 m, a few pixels at gameplay03, so it does
  not shimmer), faint wheel ruts, and narrow soft verges (0.33 of the width either side of the line, strength 0.6)
  where the soft edge begins. At the country-wide pose (a pixel over 60–150 m), it darkens to a grey-brown line, so it
  reads as the promo diorama's roads do instead of vanishing on pale ground.
  - **Why.** At 300 m with dark verges (#404), the pale core was about 4 px at gameplay03, a thin line between two dark
    ones that almost vanished; promo 03's tracks are about 8 px of pale dirt with soft edges.
  - **Measured** (`RoadsReadAsWidePaleTracks`): across every road in the frame's middle (6 points each, tilt-shift off),
    the run of pixels from the line out that the routes make paler than the ground by 0.03 luma, measured on screen.
    Median at gameplay03: 4 px on main, 9 px here (bound 6); at close30 (30 km): 7 px on main, 19 px here (bound 12).
    The core stands a median 0.19 luma over the ground (main 0.15).
  - It stays under the railway (680 m, dark ballast and bright rails) and the region borders (saturated blue).
  - The kits' corridor along a road stays 250 m either side of the line, as on main: half the 420 m width plus
    `RoadCorridorMarginM` (40 m, other routes keep 100 m). The road's outer 22% is its soft edge, so lots and crowns
    still keep about 130 m from the visible dirt. With 100 m (a 310 m corridor) the kits lost Jerusalem's Haram
    al-Sharif landmarks and Beersheba's houses (`TownCensusTests`).
- The railway is a cool grey stone ballast bed with darker shoulders, dark sleepers every 100 m and two bright steel
  rails, after promo 03 and the video's 0:21 close-up (refs #35, `.superpowers/borders-routes-r1/PLAN.md` R1 and R2).
  - A rail is 0.036 of the 680 m bed, about 0.7 px at gameplay03, so its box filter left it faint: averaged along the
    line (`TheRailsReadAsTwoBrightLinesAtGameplay03`, two railways, 30 points each), a rail stood 0.10–0.11 luma above
    the bed beside it. It is now drawn at least 0.8 px wide (`RailMinPx`) and brighter, and stands 0.18–0.21 above it
    (bound 0.15). The two fade out where they would merge, under 3 px apart (the overview).
  - Roads: on the eight palest-ground road crossings in the gameplay03 frame (`RoadsReadOnPaleGroundByTheirVerges`),
    the verges are darker than the core by at least 0.04, and either darker than the ground beside them (#404's dark
    verges) or the core is paler than the ground under it by 0.08 (the wide pale track): 7 of 8 (bound 5). The verge
    windows scale with the road's width (0.2–0.43 of it, 60–130 m at 300 m). With the soft verges alone and #404's
    rule, 1 of 8 read; the pale core is what carries them now.
  - The sleepers were every 150 m at a duty of 0.4: a dark bar of about 1.7 px every 4.3 px, a coarse ladder. At 100 m
    and 0.35 they are a fine texture, as the promo's.
  - The ballast was a grey-brown (0.50, 0.46, 0.41) close to the land's ochre; it is now a stone grey (0.56, 0.54, 0.50),
    with shoulders (0.44, 0.42, 0.39).
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

**Runtime form** (issue #321). Boot reads the traced lines from `content/map/route_paths_1948.tsv` instead of parsing
the YAML (about 125 ms at `--cpu 4`), as the localities do since #291.
- `tools/route_paths_bake.py` writes it: one row per route, its points in one field, the layout's version, the YAML's
  FNV-1a check (the same check the ribbon bake's header holds), the counts of routes and points, and a last
  `body_check` row (FNV-1a over the route rows).
- The YAML still ships. When the runtime file is missing, of another version, made from another YAML, cut short (even
  inside a number) or edited by hand, the map reads the YAML with a warning (`...; reading the YAML`): the same routes,
  only slower.
- Rerun `uv run tools/route_paths_bake.py` after `tools/routes_1948.py` rewrites the YAML. `test_route_paths_bake.py`
  fails a stale file and pins a fingerprint of the C# readers; Unity's `RoutePathsRuntimeTests` compares every route
  and point with the YAML's.

**Baked ribbons** (issue #321). Boot places the ribbon meshes from a bake instead of deriving them, as the terrain water
does (#320, docs/visual-v1.md).
- `RouteRibbonFile` (format) and the editor's `RouteRibbonBake` write every mesh `RouteLayer.Build` draws, in its order
  (`RouteLayer.Plan`): vertices, normals, uvs and indices, to `Resources/Routes/RouteRibbons.bytes`.
- The header holds the bake's version, the heightmap's resolution and `HeightsCheck`, FNV-1a checks of `heightmap.json`
  and `route_paths_1948.yaml`, the terrain's size and origin, and the geometry constants (widths, `StepM`, `ClearanceM`,
  the tolerances, the sample counts, `VerticalExaggeration`).
- When any of them differs from what the map loaded, the bake is refused with a warning (`ROUTES ...: deriving the route
  ribbons at load`) and the ribbons are derived as before: the same meshes, only slower.
- **Route index** (for the GDD Phase 4 corridors, #384). Each mesh lists its routes, each with its first vertex, vertex
  count, first index and index count (`RouteSpan`), so a later change can show one route by its construction state.
  Nothing reads it yet; the vertex layout is unchanged.
- **Re-bake** whenever the heightmap, `heightmap.json`, the route file, a constant in the header or the ribbon rule
  (`RouteLayer.Geometry`; bump `RouteRibbonFile.Version` with it) changes:
  `Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod
  Statecraft.Game.EditorTools.RouteRibbonBake.Bake`, under `tools/machine_lock.py` as any Unity run.
- **Checks.** `tools/tests/test_route_ribbon_bake.py` (CI, no Unity) fails on a stale header, reading the constants from
  the C# source, and checks the route index. `RouteTests.TheBakedRibbonsMatchTheRoutes` compares the bake with a fresh
  derivation byte for byte, and with the map's own terrain vertex for vertex; `AStaleRibbonBakeIsDerivedAtLoad` checks
  the fallback. A change to `RouteLayer.Geometry` without a version bump is caught only by the Unity test.
- **Boot** (`tools/headed.py boot --runs 3 --cpu 4`, phase `routes built`, three rounds interleaved with main `bc67247`
  under the exclusive lock, 9 cold loads each, medians, 2026-10-13): 162 ms on main, 51 ms with the bake. The whole map
  build is 3384 ms on main and 3414 ms with the bake, inside the runs' spread (3158-4240 ms); its other phases move by the
  same few tens of milliseconds both ways from run to run.

**Kits keep off the routes** (settlement kits and trees, #179).
- `MapScene` reads the routes before building the kits, and hands `KitLayer.Build` every route's corridor
  (`RouteLayer.Corridors`): half the drawn width plus `CorridorMarginM` (100 m; 40 m for roads, `RoadCorridorMarginM`).
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
- the road renders (0.81, 0.71, 0.50) over ground (0.60, 0.57, 0.32); with the wide pale track (`roads-dirt`, a later
  point of the frame) (0.86, 0.75, 0.55) over (0.50, 0.51, 0.29), main (0.77, 0.67, 0.47) at the same point.

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
- rail 17,830 vertices and roads 37,005 (after #212 and #213);
- 87,584 triangles in all, in 10 draw calls (one per display window and look);
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
