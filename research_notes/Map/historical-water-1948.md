# Historical water on the 1948 map (research note, 2026-10-14)

Status: research and a design note for issue #16 (historical water bodies), with the link to the GDD's dry-swamp
pioneer action (#384) and the `hula_drainage` event. Nothing here is in `content/`, `tools/` or `unity/`. No data was
regenerated: every hex figure below comes from read-only scripts run in memory on the committed heightmap and
`content/map/hexes.json`. Which option to build, and the water dates where sources disagree, are the owner's calls.

## Conventions

- **[V]** read on 2026-10-14 in the cited source: a page, a PDF, a map sheet or a data API.
- **[S]** one weak source, or a search-result summary whose page could not be opened.
- **[C]** sources conflict. Every version is given.
- **[GAP]** searched and not found.
- **Computed** marks a number this note derived from the repo's own data (the Copernicus heightmap, `hexes.json`).
  It is a property of the game's inputs, not a historical fact.
- Hexes are axial `(q, r)` as in `hexes.json`; "lon/lat" is the hex centre as `hexes.json` gives it. A hex is about
  65 km² (5 km circumradius, pointy top).
- Every request was made with the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
  Sources are listed at the end with the check date.

## Summary

1. **Dead Sea level.** The Survey of Palestine 1:250,000 map of 1946 prints the Dead Sea at **−392 m (mean water level
   1938)** [V, map]. A published hydrograph of the level since 1929 puts 1948 at about **−392.5 to −393 m** [V, read off a
   plotted figure, about ±0.5 m]. The Water Authority's official monthly series starts on 1 October 1976 at −398.95 m [V].
   The heightmap's Dead Sea surface, −427.0 m, matches the official level of 2013-2014 (−426.7 to −428.0 m) [V; computed].
   So the map's Dead Sea is about 35 m too low for 1948, and its shore is drawn on land that was then under water.
2. **Southern basin.** In 1946 the shallow southern basin was open water south to about 31.06° N, joined to the northern
   basin by a wide strait west of the Lisan [V, map]. It was cut off when the level fell below the Lynch Strait: 1979 per
   the Geological Survey of Israel, 1976 per Hebrew Wikipedia, early 1977 per a 1999 paper seen only as a snippet [C].
   The heightmap shows today's evaporation ponds there (about 20 flat pools of 1-45 km² at −388 to −397 m) [computed],
   which the potash works began building in the late 1960s (Israel) and late 1970s (Jordan) [V, GSI]. In 1946 there were
   already Palestine Potash Company works and "Salt Pans" south of the lake's southern shore, at Sodom, and the company's
   northern works at Kallia [V, map]; they are small at the map's scale.
3. **Lake Hula.** In 1946 the map prints Lake Hula at **70 m** above sea level, with marsh north of it and the "Hula
   Concession Area" [V, map]. The lake was 5.3 km long and 4.4 km wide, 12-14 km², about 1.5 m deep in summer and 3 m in
   winter; lake and swamps covered up to 60 km² [V, Jewish Virtual Library reprint of an Israel Foreign Ministry
   article]. Other figures: 13 km² of lake and 47 km² of seasonal swamps [V, Bar-Ilan]; lake and marshes about 40,000
   dunams, and another 18,000 dunams flooded in winter [V, KKL-JNF]. Drainage: begun 1951 by the JNF; completed 1958 [V,
   JVL], or 30 October 1957 [V, Bar-Ilan] [C on the end date].
4. **Water that is modern.** The current water rules cannot make anything above −150 m a lake, so modern reservoirs,
   fishponds, Lake Agmon (1994, about 1 km², at the Hula valley floor) and the National Water Carrier's reservoirs are
   not water on today's map [computed from the code]. The one modern reservoir below −150 m, Karameh, is already dropped.
   The only modern water the map shows that 1948 lacked is the Dead Sea evaporation ponds (point 2). The water 1948 had
   that the map lacks is the higher Dead Sea, its open southern basin, and Lake Hula with its swamps.
5. **Sea of Galilee.** −212 m (mean water level 1937) on the 1946 map [V] against −214.5 m in the heightmap
   [computed]: no hex changes.
6. **Design.** Water is decided twice from the same heightmap rules: per hex in `tools/hexgrid.py` (`classify`,
   `water_share`, `is_sea`), and per pixel in `tools/terrain_bake.py` (`water_mask`, `lake_mask`) for `coast.png`. Unity
   repeats the pixel rule for the water mesh (`MapBuilder.WaterMeshGrid`, baked to `terrain_water.bytes`). There is no
   water data file, no hex override list and no Dead Sea level constant today. The Hula would make **one hex**,
   (14, 3), mostly wet (swamp); the lake itself is only about a fifth of a hex and makes no hex "lake" under the current
   0.4 share rule. The options and their costs are in [Design note](#part-2-design-note).
7. **Dry swamp.** If the Hula swamp becomes a mapped wetland at (14, 3), the GDD's dry-swamp pioneer action has a real
   target on the map, and `hula_drainage` (window 7 March to 18 May 1951) is its historical counterpart. They must not
   contradict each other (impact plan §4.4, D-B4; #384's done-when).

## Part 1: Research

### 1.1 The Dead Sea level in 1948

| Fact | Value | Tag and source |
|---|---|---|
| Mean water level, 1938, printed on the 1946 map | −392 m ("−392 (M.W.L. 1938)") | [V] Survey of Palestine 1:250,000, 1946, label in the northern basin west of the Lisan (Palestine Open Maps tiles, read as an image) |
| Level 1929-1993, annual points | about −390 m in 1929-30; −392 to −393 m from about 1934 to 1957, with highs near −391.5 m in 1945-46; about −395 m in 1960; about −399 m in 1976 | [V, read off the plot] Gat, "The Dead Sea: a model of a desiccating terminal salt lake", Fig. 2 "Hydrograph of the Dead Sea level since 1929" (IAEA/ETDEWEB). Values are read from the plotted figure, not a table |
| Level in 1948 | about −392.5 to −393 m (the 1948 point lies just below the 1947 and 1949 points, about −392.2 and −391.9 m) | [V, read off the plot] as above, about ±0.5 m. No table giving the 1948 value was reached [GAP] |
| Decline 1930-1973 | 17 cm a year | [V] USGS EROS, "Rates of decline" |
| The record since 1800 | Klein (1961), *On the fluctuations of the level of the Dead Sea since the beginning of the 19th century*, Israel Hydrological Service, Hydrological Paper 7, is the standard reconstruction; 1930/31-1962/63 is the "undisturbed" period before diversions | [V] abstract of Klein and Flohn (1987), Springer. Klein (1961) itself was not reached [GAP] |
| Official monthly series | starts 1 October 1976 at −398.95 m; −399.05 m on 1 January 1977; −426.7 to −428.0 m through 2013-2014; −441.50 m on 4 October 2026 | [V] Israel Water Authority, data.gov.il dataset "מפלס ים המלח" (read through the CKAN datastore API) |
| The Hydrological Service measures the level monthly since 1976 | | [V] GSI report GSI/16/2015 (Lensky and Dente), "data and methods" |
| The heightmap's Dead Sea surface | −427.0 m (`elev_min_m` −427.04) | Computed (`heightmap.json`). This is the level of about 2013-2014 in the official series, consistent with the Copernicus DEM's acquisition years |

Secondary leads not used as sources: Hebrew Wikipedia ("ירידת מפלס ים המלח") says the level was stable around −390 m in
the first half of the 20th century and that measurements began in 1930 [S]; Britannica (blocked, 403) is quoted in search
results as "some 400 metres below sea level" for the mid-century [S]; a commercial site gives about −393 m for the 1950s
[S, not used].

**Value to use (for the owner):** −392 m is the one printed, official-survey figure (1938); the 1948 value from the
hydrograph is about −392.5 m. The difference is under a metre, far below what the 5 km hexes or the 79 x 211 m heightmap
samples can show on the steep shores, and is visible only on the flat ground of the Jordan delta and the southern
basin.

### 1.2 The southern basin and the evaporation ponds

| Fact | Tag and source |
|---|---|
| In 1946 the southern basin is open water. The shore runs south to about 31.06° N (the Sabkha, "Soft Mud Impassable"); the strait between the Lisan and the western shore is wide open water | [V] Survey of Palestine 1:250,000, 1946 (read as an image; the latitudes are this note's reading of the tiles' web-mercator position, about ±0.5 km) |
| "Palestine Potash Co. (South)", a "Potash Refinery" and two areas marked "Salt Pans", south of the southern shore at Qa' es Safi and Sodom; "Palestine Potash Co. (North)" at Kallia at the north end | [V] the same sheet |
| The basins were connected by a strait in the Landsat image of 1973; the strait is now dry; in the southern part the salt and potash industries use the sea's water in evaporation ponds | [V] USGS EROS Earthshots, "Dead Sea, Israel, Jordan, West Bank" |
| In 1979 the level fell below the Lynch Strait; since then the northern basin is a terminal lake and the southern basin is kept as evaporation ponds by the Israeli and Jordanian potash works. Building the ponds began in the late 1960s (Israeli side) and the late 1970s (Jordanian side) | [V] GSI/16/2015, introduction (Hebrew) |
| The two basins were disconnected in 1976; the southern basin is flat at about −402 m | [S] Hebrew Wikipedia, "ירידת מפלס ים המלח" (lead only) |
| At the start of 1977 the level fell below −402 m, the sill of the southern basin, which became a pond held by a dike and filled by pumping (citing Steinhorn 1981) | [S] search snippet of "Thermodynamic constraints on Dead Sea evaporation: can the Dead Sea dry up?", Chemical Geology (ScienceDirect returned 403) |
| **Date the southern basin was cut off** | [C] 1976 (he.wiki), early 1977 (Chem. Geol. snippet), 1979 (GSI). The official level was −398.95 m in October 1976 and −399.05 m in January 1977, above the −402 m sill the snippet names, so the "below −402 m in early 1977" reading is doubtful. All three dates are after the game's 1948-1956 window, so for the founding era the basin is water either way |

What the heightmap shows there today (computed, `terrain_bake.lake_mask` run in memory): the northern basin, 589 km² at
−427.0 m, and 20 separate flat components south of 31.30° N of 1.05 to 45.3 km² at −388.0 to −397.0 m, which are the
ponds (the largest, 45.3 km² centred at 35.410° E, 31.178° N, is the one `tools/tests/test_terrain_bake.py` pins as "the
largest evaporation pan"). Another 22 flat pools of 0.2-0.9 km² in the same area are dropped by the 1 km² floor. Four of
the kept ponds, centred 31.02-31.06° N, lie south of the 1946 shoreline, on ground that was then sabkha or the
Palestine Potash pans.

### 1.3 Lake Hula and the swamps in 1948

| Fact | Tag and source |
|---|---|
| Lake Hula printed with level "70"; marsh symbols fill the valley north of the lake to about 33.15° N; "Hula Conces. Area" printed in the marsh; Jordan branches enter the marsh from the north | [V] Survey of Palestine 1:250,000, 1946 |
| The valley floor is about 70 m above sea level; the valley is 177 km² (25 km by 6-8 km) | [V] JVL, "Geography of Israel: Hula Valley" (the page's source line: Israeli Foreign Ministry) |
| The lake: 5.3 km long, 4.4 km wide, 12-14 km², pear-shaped, about 1.5 m deep in summer and 3 m in winter | [V] JVL, same page |
| Peat swamps with papyrus north of the lake; further north swampy meadows wet in winter and dry in summer; lake and swamps "up to 60 square kilometres", varying by season and year | [V] JVL, same page |
| At the start of the 20th century: 13 km² of lake and 47 km² of seasonal swamps | [V] Bar-Ilan University, "Draining Israel's Hula Valley" (Dotan Rotem) |
| The lake and its marshes covered about 40,000 dunams (40 km²); the drainage also affected another 18,000 dunams flooded in winter | [V] KKL-JNF, "Rehabilitation of the Hula Valley" |
| Search results also give 15 km² for the lake and 45-75 km² for lake and swamps (Hambright and Zohary) | [S] the paper's PDF timed out and the publisher pages returned 403 |
| **Extent of lake plus swamp** | [C] up to 60 km² (JVL), 13 + 47 = 60 km² (Bar-Ilan), 40 km² plus 18 km² flooded in winter (KKL-JNF), 45-75 km² ([S] Hambright and Zohary) |

**The drainage.**

| Fact | Tag and source |
|---|---|
| Carried out by the JNF; began in 1951, completed by 1958 | [V] JVL |
| In 1951 KKL-JNF began draining the lake and marshes; method: deepening the Jordan's bed south of the valley and digging two canals | [V] KKL-JNF |
| The Jordan downstream of the lake deepened and widened (the basalt plug); north of the lake the Jordan diverted into new East and West canals | [V] JVL |
| Draining completed on 30 October 1957; elsewhere on the same page "the years 1951-1959" | [V] Bar-Ilan |
| **End date** | [C] 1958 (JVL; issue #16's "1951-1958"), 30 October 1957 (Bar-Ilan, completion of the drainage itself); Bar-Ilan's "1951-1959" is the wider project |
| Works began about 14 February 1951; Syria complained on 21 February; the armistice chairman asked on 7 March that work stop | `content/events/hula_drainage.yaml` (comments and sources: FRUS 1951 v. V doc. 288; JTA). Not re-read for this note |
| A papyrus swamp of 350 ha (KKL-JNF: about 3,000 dunams) in the south-west of the valley was kept, and became Israel's first nature reserve in 1963 | [V] JVL; [V] KKL-JNF for the 3,000 dunams |
| After drainage the peat subsided by up to 3 m (JVL), more than 3 m by the early 1990s (KKL-JNF) | [V] both |

**Lake Agmon (modern).** Dug in the former transition between lake and swamps and flooded in April-May 1994; irregular,
about 1 km², mostly under 1 m deep [V, JVL]; KKL-JNF says 1,000 dunams were reflooded from 1994 [V]. It lies at the
valley floor, far above −150 m, so the current rules never draw it (section 2.1); nothing needs removing.

**Geometry for a 1948 polygon.** The 1946 sheet is the best reachable geometry. Points read from its tiles (web
mercator, about ±0.5 km; the hex is by `hexgrid.point_to_hex`): lake centre 35.616° E, 33.069° N, hex (14, 4); north
shore 33.091° N, hex (14, 4); south tip 35.628° E, 33.043° N, hex (13, 5); west shore 35.593° E, hex (13, 4); east
shore 35.640° E, hex (14, 4); marsh 35.578-35.642° E, 33.116-33.149° N, all in hex (14, 3). A traced polygon would need
a check against the Survey of Palestine 1:20,000 or 1:100,000 sheets, which were not read for this note [GAP].

### 1.4 Other water: the Sea of Galilee and modern reservoirs

| Fact | Tag and source |
|---|---|
| Lake Tiberias printed at −212 m (mean water level 1937) | [V] Survey of Palestine 1:250,000, 1946 |
| The heightmap's Sea of Galilee: 151.7 km² of flat water at −214.5 m; its three lake hexes (12, 7), (11, 8), (11, 9) | Computed. The 2.5 m difference moves the shore by tens to a few hundred metres; no hex changes |
| The Karameh reservoir (0.30 km², −312 m, 35.558° E, 32.019° N) is already dropped by the 1 km² floor | `tools/terrain_bake.py` docstring; [computed] the pool is found and dropped |
| Modern reservoirs on the coastal plain, the National Water Carrier's reservoirs, fishponds, Lake Agmon | Not water on the map: every one lies above −150 m, and only flat water below −150 m can be a lake (section 2.1). [Computed] no other flat pool of 1 km² or more below −150 m exists outside the two rift lakes and the Dead Sea ponds |
| Coastline changes (ports, reclamation) | Out of scope: the chapters spec §14 defers them |

Fishpond, reservoir and National Water Carrier dates were therefore not researched: they cannot reach the map under the
current rules.

## Part 2: Design note

### 2.1 How water is decided today

**Per hex (`tools/hexgrid.py`, writes `content/map/hexes.json`, which the sim reads and hashes).** `build()` groups
every heightmap pixel into its hex (`point_to_hex`) and computes, per hex, `sea_share` (share of pixels with
|elevation| < `SEA_EPS_M` = 0.5 m, `is_sea`), `water_share` (share of pixels below −150 m and within 1.5 m of the hex's
most common sub −150 m level, `water_share`), the median elevation, rainfall and `coast_km`. `classify()` then takes the
first match: `sea` if `sea_share >= 0.5`; `lake` if `water_share >= 0.4`; then the land biomes. `land` is
`biome not in ("sea", "lake")`. There is no input but the heightmap and the rainfall files: no water mask, no lake
polygon, no hex override list, and no level constant except the −150 m threshold inside `water_share`. Today: 216 sea
and 16 lake hexes (3 Sea of Galilee, 10 Dead Sea northern basin, 3 southern-basin ponds). [Computed] an in-memory
`hexgrid.build()` equals the committed `hexes.json` exactly.

**Per pixel (`tools/terrain_bake.py`, writes `unity/Assets/Textures/Map/coast.png`).** `water_mask(elev, dx, dy)` is
the sea (`|elev| < SEA_EPS_M` connected to the map border, `border_connected`) or `lake_mask`: pixels below
`LAKE_BELOW_M` (−150 m) whose 3 x 3 height range is at most `LAKE_FLAT_M` (0.25 m), opened twice, kept per component of
at least `MIN_LAKE_AREA_KM2` (1 km²). `bake()` turns the mask into a signed distance to the shore and `encode_coast`
writes it as 8 bits (0.5 = shoreline, range 12 km).

**In Unity.** The water surface is a mesh, not the coast texture alone. `MapBuilder.WaterMeshGrid` marks a sample wet
with the same two rules (`SeaToleranceM`, `LakeBelowM` = −150, `LakeFlatM` = 0.25) and builds the mesh only within
`WaterMeshReach` (2 samples) of a wet sample; the Editor's `TerrainWaterBake` writes it to
`Assets/Textures/Map/terrain_water.bytes`. `Sea.shader` draws water on that mesh with coverage `ScWater(coast)`. So
water added only to `coast.png` would have no mesh under it where the heightmap rule finds no wet sample (all of the
Hula, and the 1948 Dead Sea margin above today's shore). The shader also colours water by elevation (`ScGalileeSrgb`
for −300 to −100 m) and lays a sand beach along every "sea-level coast (above −100 m)" (`ScShore`); a lake at +70 m
would get the Mediterranean's colours and a beach. The minimap (`MinimapPresenter`) and the ground colour
(`BakeBiomeTexture`) are built at runtime from `hexes.json` biomes; they change only if `hexes.json` changes.
`tools/land_bake.py` reads `hexes.json` biomes and paints fields up to the waterline.

**The sim.** `MapActions.Check` refuses a hex whose `Land` flag is false (`sim/Statecraft.Sim/MapActions.cs:60`).
The chapters spec §10 rules water visual-only: the sim keeps the static `Land` flag.

### 2.2 Which hexes would change

All computed in memory (scratch scripts `hexes_now.py`, `water_fill.py`, `lakes.py`, `px2ll.py`; nothing written to
the repo). "Fill" is the share of a hex's pixels below the level and 4-connected to today's northern basin.

**Dead Sea at a 1948 level.**

| Hex (q, r) | lon, lat | Today | Fill −392 m | Today's water share | 1946 map | Change for 1948 |
|---|---|---|---|---|---|---|
| (1, 25) | 35.43044, 31.65424 | desert | 0.50 | 0.34 | western shore | borderline; becomes lake only at a 0.5 rule's edge |
| (−1, 30) | 35.47602, 31.31509 | desert | 0.69 | 0.23 | open water north of the Lisan | **lake** |
| (−2, 31) | 35.43044, 31.24726 | desert | 0.56 | 0.11 | strait and Lisan tip, open water | **lake** |
| (−2, 30) | 35.38487, 31.31509 | desert | 0.43 | 0.04 | western shore | stays land at 0.5; lake at a 0.4 rule |
| (−2, 33) | 35.52159, 31.11160 | desert | 0.37 | 0.10 | open water (eastern southern basin) | **lake** by the 1946 map; the DEM fill underestimates it |
| (−3, 32), (−2, 32), (−3, 33) | 35.38487, 31.17943; 35.47602, 31.17943; 35.43044, 31.11160 | lake (ponds) | 0.02, 0.82, 0.28 | 0.43, 0.31, 0.39 | open water | stay lake, now as the open basin, not ponds |
| (−3, 34), (−4, 34) | 35.47602, 31.04377; 35.38487, 31.04377 | desert | 0.22, 0.02 | 0.00, 0.14 | shore at about 31.06° N, south of it sabkha and salt pans | stay land; their pond water should go |
| the 10 northern-basin lake hexes | | lake | 0.66-1.00 | | water | no change |

A level fill works for the northern basin. It fails in the southern basin: the pond surfaces and dikes stand at −388 to
−397 m, so −392 m floods some ponds and not others (hex (−3, 32) drops to 0.02). The southern basin needs a polygon from
the 1946 sheet, not a level. [Computed] Fill areas: 850 km² at −395 m, 918 km² at −392 m (107 km² of it south of
31.2° N), 1,050 km² at −390 m, against 696 km² of water today in the same box. These are the DEM's areas, not sourced
lake areas; no 1948 area figure was reached [GAP] (a 1966 figure of about 940 km² was seen only as a search summary [S]).

**Lake Hula and its swamps** (the valley floor in the heightmap: 1st percentile 62.0 m, 10th 66.3 m; the subsided peat
lies below the old 70 m lake level).

| Hex (q, r) | lon, lat | Today | Below 70 m | Below 72 m | 1946 map |
|---|---|---|---|---|---|
| (14, 3) | 35.61273, 33.14651 | lowland | 0.45 | 0.60 | the marsh; the lake's north end at its southern edge |
| (14, 4) | 35.65830, 33.07868 | hills (median 322 m: it takes in the slopes east of the valley) | 0.27 | 0.28 | most of the lake |
| (13, 4) | 35.56716, 33.07868 | lowland | 0.19 | 0.21 | the lake's west shore |
| (13, 5) | 35.61273, 33.01085 | lowland | 0.05 | 0.07 | the lake's south tip |

[Computed] The valley floor below 70 m is 62.4 km², close to the sources' "up to 60 km²" of lake and swamp; that is a
coincidence of the subsided peat, not a reconstruction. The lake (12-14 km²) is about a fifth of one hex, so no hex
becomes "lake" under the 0.4 rule; with a swamp polygon, (14, 3) is the one hex that is mostly wetland.

**Sea of Galilee:** none. **Modern reservoirs:** none.

### 2.3 What follows any water change

| Changes | Follows |
|---|---|
| `coast.png` only (pixel water) | Re-run `tools/terrain_bake.py` (about 10 s; PNG bytes reproducible only on the same toolchain). Update `tools/tests/test_terrain_bake.py`, which pins the Dead Sea, the 45 km² pond as water and Karameh as land. Unity: the water mesh must cover the new water: change `MapBuilder.WaterMeshGrid`'s wet rule (or feed it the mask) and re-bake `terrain_water.bytes` in the Editor (`TerrainWaterBake`; `tools/tests/test_terrain_water_bake.py` checks it against the heightmap). Shader: a colour and no beach for fresh water above −100 m (the Hula). Unity EditMode tests and a WebGL check need Unity and are not in CI |
| `hexes.json` (hex biome or `land`) | Content hash changes (the sim reads it; old saves warn). Goldens move only if a golden run's map action touches a changed hex: unknown until `dotnet test` runs. Regenerate and re-check everything built from it: `settlement_hexes.yaml` (no game settlement sits in an affected hex today), `subdistrict_hexes_1948.yaml` and the armistice hexes (lake hexes are in no region), `landuse.png` (`land_bake.py`), the hexgrid docstring counts and `test_hexgrid.py`'s stale check, `test_localities.py`. Unity: the minimap and the biome ground change at runtime with no bake |
| A dated water file | If it lives under `content/`, the sim does not read or hash it unless `ContentLoader` is changed to (it reads named map files only); the content tests would need a schema or a tools test for its `sources:` |

### 2.4 Options

**A. Dated water file and one coast mask per water epoch (the chapters spec §10 recommendation and the owner's comment
on #16).** Data: `content/map/water.yaml` with each body, `from`/`to`, a level or a polygon, and sources: the Dead Sea
at about −392 m (northern basin by level fill), the southern basin as a polygon from the 1946 sheet (to 1976/1977/1979
[C]), the ponds (from the late 1960s), Lake Hula and the Hula swamps as polygons (to 1951-1958 [C on the end]).
Tool: `terrain_bake.water_mask` takes an epoch, removes the pond components inside the basin polygon, adds the level
fill and the polygons, and writes `coast_<epoch>.png` (about 86 KB each). Hexes: unchanged (visual only).
Cost: the bake and its tests; a Unity change to the water mesh (the union of every epoch's water, re-baked) and the
shader's colour rule; the epoch swap is a follow-up. Against: the sim never sees the Hula, so dry swamp has no map
target unless the sites file names one (option D).

**B. Change the hex grid.** Add the water bodies to `hexgrid.build()` (an override list or the same polygons) and a
wetland biome. (−1, 30), (−2, 31) and (−2, 33) become lake; (14, 3) becomes swamp (or lake). Cost: everything in the
`hexes.json` row above, and a sim decision: a swamp hex that is not `land` blocks map actions there, and a dated `land`
flag is the hashed change the spec postpones "until a rule needs them". Dry swamp is such a rule.

**C. One static 1948 mask.** Option A with a single epoch. The GDD's MVP is 1948-1951, and in that window the southern
basin is water throughout and the Hula is undrained until works start in 1951 (completion 1957/1958). Cheapest; wrong
after the window, which the MVP does not reach.

**D. Combine C or A with a site.** Keep `hexes.json` static; put the Hula swamp in the Phase 4 `sites.yaml` at hex
(14, 3) (or a polygon) as the dry-swamp target; on completion the map switches to the drained mask. This gives the
action its target without dated land flags.

### 2.5 The link to `hula_drainage` and the dry-swamp action (#384)

- `content/events/hula_drainage.yaml` (window 7 March to 18 May 1951) is about the demilitarised-zone dispute during
  works begun about 14 February 1951; it has no map effect today.
- The GDD's "Dry swamp: improve a wetland area" (Pioneer work table) has no target on the map today: there is no
  wetland hex or polygon, and the Hula floor is "lowland" (14, 3) and "lowland"/"hills" around the lake.
- If the Hula lake and swamps become map water or swamp (any option above), the drainage project and the dry-swamp
  action have a real map target: (14, 3) for the swamps, (14, 4)/(13, 4)/(13, 5) for the lake. The owner then has to
  rule (D-B4) how they relate: whether the event is the historical path of the same project, whether the player can
  drain it earlier or not at all, and what the map shows if the player halts the works in the event. The sourced
  facts to keep fixed are the start (1951) and the end (30 October 1957 or 1958 [C]), the 350 ha (about 3,000 dunams)
  reserve left wet, and Lake Agmon's reflooding in 1994 (outside the game's eras).

## Sources not reached

- gov.il, "Lake Hula, Lake Agmon" (https://www.gov.il/en/pages/lake-hula-lake-agmon): 403.
- Hambright and Zohary, PDF on davehambright.oucreate.com: connection timed out.
- Taylor and Francis, Hydrological Sciences Journal 2011 (doi 10.1080/02626667.2011.631013): 403.
- Lensky et al. 2005, Water Resources Research (Wiley PDF): 403.
- ScienceDirect, Chemical Geology, "Thermodynamic constraints on Dead Sea evaporation": 403 (snippet only).
- Britannica, "Dead Sea": 403.
- MDPI Water 15(5) 837, Kinneret levels: 403.
- data.gov.il direct CSV download: 403 (the same data was read through the CKAN API).
- KKL-JNF, "Sixth Decade: 1951-1960": 404.
- Klein (1961), Hydrological Paper 7, and any Central Bureau of Statistics table of Dead Sea levels before 1976: not
  found online [GAP].
- Survey of Palestine 1:100,000 and 1:20,000 sheets: not tried; only the 1:250,000 of 1946 was read.

## Sources (all checked 2026-10-14)

- Survey of Palestine, Palestine 1:250,000, 1946, via Palestine Open Maps: https://palopenmaps.org/en/maps?basemap=pal250k1946
  (tiles https://palopenmaps.org/tiles/pal250k-1946/{z}/{x}/{y}.jpg, zoom 12, read as images: the Dead Sea, Lake Hula
  and Lake Tiberias). The same sheets `content/map/routes_1948.yaml` cites.
- J.R. Gat, "The Dead Sea: a model of a desiccating terminal salt lake", IAEA/ETDEWEB: https://www.osti.gov/etdeweb/servlets/purl/20154842
- Israel Water Authority, "מפלס ים המלח" (Dead Sea level since 1976), data.gov.il: https://data.gov.il/dataset/ebf0005b-f5fe-4c6c-ab6d-28778167d025
  (API: https://data.gov.il/api/3/action/datastore_search?resource_id=823479b4-4771-43d8-9189-6a2a1dcaaf10)
- N. Lensky and E. Dente, "הגורמים לירידה המואצת של מפלס ים המלח בעשרות השנים האחרונות", Geological Survey of Israel
  report GSI/16/2015 (copy hosted by ICL): https://www.icl-group.com/wp-content/uploads/2024/05/Dead-Sea-Geological-Survey-of-Israel-3.pdf
- USGS EROS, "Dead Sea, Israel, Jordan, West Bank": https://eros.usgs.gov/earthshots/dead-sea-israel-jordan-west-bank
- USGS EROS, "Rates of decline": https://eros.usgs.gov/earthshots/rates-of-decline
- C. Klein and H. Flohn (1987), "Contributions to the knowledge of the fluctuations of the Dead Sea level", Theoretical
  and Applied Climatology (abstract): https://link.springer.com/article/10.1007/BF00868099
- Hebrew Wikipedia, "ירידת מפלס ים המלח" (lead only): https://he.wikipedia.org/wiki/%D7%99%D7%A8%D7%99%D7%93%D7%AA_%D7%9E%D7%A4%D7%9C%D7%A1_%D7%99%D7%9D_%D7%94%D7%9E%D7%9C%D7%97
- Jewish Virtual Library, "Geography of Israel: Hula Valley" (source line: Israeli Foreign Ministry): https://jewishvirtuallibrary.org/hula-valley
- KKL-JNF, "Rehabilitation of the Hula Valley": https://www.kkl-jnf.org/organization-chief-scientist/water-for-israel/water_activity_hula_valley_rehabilitation/
- Bar-Ilan University, "Draining Israel's Hula Valley" (Dotan Rotem): https://www.biu.ac.il/en/article/9627
- In the repo: [`hula_drainage.yaml`](../../content/events/hula_drainage.yaml),
  [`hexgrid.py`](../../tools/hexgrid.py), [`terrain_bake.py`](../../tools/terrain_bake.py),
  [`MapBuilder.cs`](../../unity/Assets/Scripts/Map/MapBuilder.cs), [`GDD.md`](../../docs/GDD.md),
  [chapters spec §10](../../docs/superpowers/specs/2026-10-10-chapters-architecture-design.md),
  [GDD impact plan](../../docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md).
