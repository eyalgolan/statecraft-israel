# Denser hexes for the MVP (#1183): 5.0, 4.4 and 4.0 km, measured. Plan

> **For agentic workers:** this is step 1 of #1183, a costed plan with no code and no bake. **The owner picks the size from
> the table below.** Nothing here is built until the owner approves the plan and the size (#1183 step 2); then step 3 is one
> bake PR, plus one rules PR if a design value is re-tuned ("The PRs, in order" below).

**Status (20 October 2026):** proposed. The owner's go-ahead (#1183, 20 October 2026): "Go ahead with step 1: a docs-only
plan PR on `docs/denser-hexes-plan-1183`, with no code and no bake. 4.0 and 4.4 km are the right pair ... Please measure
Tel Aviv to Jerusalem along the actual hex path, not the straight line, and put the WebGL load time next to the current
gate. The owner picks the size from your table."

**Spec:** issue #1183. Rules: [docs/rules/mvp.md](../../rules/mvp.md). Determinism and goldens: [docs/determinism.md](../../determinism.md). Boot budget: [docs/visual-v1.md](../../visual-v1.md). Design of record: [docs/GDD.md](../../GDD.md).

**Goal (#1183, owner decision of 20 October 2026):** a denser MVP map, Tel Aviv to Jerusalem in 7 to 8 hexes. The map's
extent does not change, only the hex size. The platform stays web only for the MVP, so the WebGL load-time gate stays the
budget.

**How this was measured.** Every number below comes from trial bakes on `main` at `74c39d58`, made in scratch copies
(`git archive` into `/tmp`) and in a throwaway detached worktree. None of it is committed; this PR adds only this file. Each
copy changed only `HEX_SIZE_KM` (`tools/hexgrid.py:56`) and then re-ran the repo's own tools in order: `hexgrid.py`,
`settlement_hexes.py`, `corridor_hexes.py`, `borders_bake.py`, `armistice_bake.py`, `control_bake.py`, `regions_bake.py`,
`minimap_bake.py`, `land_bake.py` and `terrain_bake.py`, then `BiomeColourBake.Bake` in Unity, then `Build.Web` (WebGL,
compression disabled). The 5.0 copy reproduces every committed `content/` file byte for byte (`cmp`), so the pipeline is
the one `main` was baked with. Two steps needed trial-only stand-ins: `borders_bake.py` was let past one conflict (item 1 under "What the trial found"),
and the hand-placed sites and locations were re-placed by a script (item 2). The
machine is the contributors' Linux build box (8 vCPU, Chrome for Testing 155 with SwiftShader, no GPU), not the development
Mac, so its boot times are compared with each other and against the gate's rule, not against that Mac's milliseconds.

## The table

"Measured" means a command was run on the trial bake. "Derived" means plain arithmetic on measured or committed numbers.

| | 5.0 km (today) | 4.4 km | 4.0 km | how |
|---|---|---|---|---|
| Hexes (land) | 1,102 (870) | 1,419 (1,118) | 1,692 (1,329) | measured: `hexgrid.py` output |
| Hexes vs today | 1.00x | 1.29x | 1.54x | derived |
| Centre to centre | 8.66 km | 7.62 km | 6.93 km | derived: size x sqrt(3) |
| Tel Aviv to Jerusalem, straight line / spacing | 6.2 | 7.1 | 7.8 | derived: 53.9 km between the two `settlements_1948.yaml` points, `hexgrid.lonlat_to_km` |
| Tel Aviv hex, Jerusalem hex | (-3, 19), (0, 23) | (-3, 21), (0, 26) | (-4, 24), (0, 29) | measured: `settlement_hexes.py` |
| Hex distance (fewest steps, any terrain) | 7 | 8 | 9 | measured: axial distance of the two hexes |
| **Hexes entered on the game's path** | **7** | **9** | **9** | measured: `Movement.Path` (below) |
| Movement points on that path | 12 | 15 | 15 | measured: `Movement.Reachable` cost |
| Path with no enemy-held localities | 7 hexes, 12 points | 8 hexes, 14 points | 9 hexes, 15 points | measured: same, `mvp.enemy_held_localities: []` |
| Turns for a 3-point `veteran_soldier` | 5 | 6 | 6 | measured: turn-by-turn search with `Movement.Reachable` |
| Turns if it had 4 points | 4 | 5 | 5 | measured: same, `movement_points: 4` |
| `hexes.json` | 99,961 B | 128,568 B | 153,315 B | measured: `ls -l` |
| `subdistrict_hexes_1948.yaml` / `.tsv` | 17,903 / 10,560 B | 22,203 / 12,879 B | 25,859 / 14,845 B | measured |
| `control_hexes_1948_49.yaml` / `.tsv` | 17,017 / 12,643 B | 20,793 / 15,290 B | 24,568 / 17,933 B | measured |
| `armistice_hexes_1949.yaml` / `.tsv` | 10,502 / 9,000 B | 10,781 / 9,143 B | 10,964 / 9,242 B | measured |
| `settlement_hexes.yaml`, `corridors.yaml` | 1,045, 3,964 B | 1,049, 3,944 B | 1,050, 3,954 B | measured |
| `BiomeColours.bytes`, `RouteRibbons.bytes` | 3,145,744, 2,855,330 B | same sizes (fixed-size texel grid; ribbons do not read hexes) | same sizes | measured (biome); ribbons: `RouteRibbonBake.cs:8-10` inputs |
| PNG bakes `landuse`, `MinimapRelief`, `coast` (re-baked on this box) | 5,826,948, 324,131, 102,114 B | 5,822,952, 329,772, 102,103 B | 5,826,759, 332,235, 102,046 B | measured; the committed files differ from this box's 5.0 bake (PNG bytes depend on the toolchain, `tools/README.md`), so the columns compare like with like |
| WebGL build, disabled compression | 130,276,160 B (a rebuild for the boot runs: 130,276,166 B) | 130,365,367 B (+89 KB) | 130,440,178 B (+164 KB) | measured: `du -sb unity/Builds/Web-disabled` |
| of which `.data`, `StreamingAssets` | 37,183,113 B, 34,620,218 B | 37,182,532 B, 34,710,006 B | 37,178,245 B, 34,789,104 B | measured; `.wasm` (57,918,793 B) is identical in all three |
| **Boot, `map_build_ms`, median of 9 runs on this box** | 1,885 ms | 2,160 ms (+15%) | 2,286 ms (+21%) | measured: `tools/headed.py boot`, see "Load time" |
| **Boot, `first_frame_ms`, median of 9 runs on this box** | 19,967 ms | 20,766 ms (+4%) | 21,616 ms (+8%) | measured: same |
| **The gate on this box** (today + max(300 ms, 50%)) | map 2,828 ms, first frame 29,951 ms | pooled medians within: +275 of +943 ms allowed, +799 of +9,984 ms | pooled medians within: +401 of +943 ms, +1,649 of +9,984 ms; round 2 alone over (map +985 ms) | derived from the 5.0 row, rule at `docs/visual-v1.md:69-70` |
| The gate as pinned on the development Mac (`--cpu 1`) | map 743 ms, first frame 5,321 ms (baseline 443 / 3,547 ms) | | | `docs/visual-v1.md:178-182` |
| Mac `--cpu 1`, **estimated** | | map 508 ms, first frame 3,689 ms | map 537 ms, first frame 3,840 ms | estimated: the Mac baseline times this box's measured growth; if map build grew in step with the hex count instead, 570 / 680 ms. Not measured on a Mac |

**Which option gives "7 to 8 hexes" on the measured path.** On the game's own path today's 5 km grid is already 7 hexes
(12 points), because the path cannot run along the straight line: the straight line is 6.2 hex spacings, the hex distance
is 7. At 4.4 km the path is 9 hexes (8 if Lod and Ramla were not closed to the player by R-MOVE-005), at 4.0 km 9 hexes.
Counted as the issue counts (straight line over the centre spacing), 4.4 gives 7.1 and 4.0 gives 7.8. Which count the "7 to
8" means is the owner's call; the table shows both.

### The path measurement

The path is the sim's own: `Movement.Path` (`sim/Statecraft.Sim/Movement.cs:42`), the cheapest land path of
`Movement.Search` (`Movement.cs:106`, R-MOVE-002), at the `content/rules.yaml:142-148` biome costs, never through an
enemy-held locality (`rules.yaml:153`, R-MOVE-005: Lod and Ramla lie between the two cities; the file marks that list a draft, unchecked, with research #1096 to check it). A scratch console program
loaded each trial `content/` with `ContentLoader.Load`, started `Simulation.NewGame(c, 1, Mode.Historical)`, removed every
unit, put one `veteran_soldier` on Tel Aviv's hex with its `movement_points` raised to 999 (so one turn reaches Jerusalem)
and asked `Movement.Path` for Jerusalem's hex. The turn counts use the real 3 points: a breadth-first search over turns, each
turn adding every hex `Movement.Reachable` (`Movement.cs:30`) gives from a hex reached the turn before. The paths:

- 5.0: (-3,20) (-2,20) (-2,21) (-2,22) (-2,23) (-1,23) (0,23): coastal plain 2, lowland, hills 2, mountains, hills.
- 4.4: (-3,22) (-2,22) (-2,23) (-3,24) (-3,25) (-3,26) (-2,26) (-1,26) (0,26): coastal plain 4, lowland, hills 2,
  mountains 2.
- 4.0: (-4,25) (-3,25) (-3,26) (-3,27) (-3,28) (-3,29) (-2,29) (-1,29) (0,29): coastal plain 3, lowland, hills 3,
  mountains, hills.

### Load time

**The gate.** The repo has no fixed load-time number. The boot budget is relative: "each span (`map_build_ms` and
`first_frame_ms`, median of 3, at `--cpu 1` and at `--cpu 4`) grows by at most max(300 ms, 50% of its Task 1 baseline)"
(`docs/visual-v1.md:69-70`), pinned on the development Mac as 743 ms map build and 5,321 ms first frame at `--cpu 1`
(`docs/visual-v1.md:181-182`, restated in `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md:89`).
`tools/headed.py boot --baseline MS --baseline-first-frame MS` checks it. `map_build_ms` runs from the decoded heightmap to
the built map, `first_frame_ms` from navigation start to the second frame after `GAME OK` (`docs/visual-v1.md:42`).

**This box.** `uv run tools/headed.py boot --build <build> --runs 3` under `xvfb-run`, Chrome for Testing 155 linux64 with
`--no-sandbox --use-angle=swiftshader` (without SwiftShader the page reports "Your browser does not support WebGL"). The
renderer is software, so every span is slower than on the Mac and the absolute numbers are not the Mac's. The three
builds were measured in alternation: three rounds of 5.0, 4.4, 4.0, each `--runs 3`, so 9 cold loads per size. Two earlier single sets (5.0 at 1,387 / 14,792 ms, then 4.4 alone at load average ~16 at
2,113 / 21,282 ms) were not interleaved and are left out.
Two other Unity jobs were running at the time (load average 6 to 19 on 8 vCPUs), and the noise is larger
than the effect: single loads ranged 1,377-3,314 ms (map build) and 18,466-28,062 ms (first frame) at 5.0 alone, and
round 3 put 4.4 and 4.0 below 5.0. The per-round medians (map build / first frame, ms):

| round | 5.0 | 4.4 | 4.0 |
|---|---|---|---|
| 1 | 1,654 / 18,702 | 1,943 / 20,766 | 2,286 / 21,616 |
| 2 | 1,885 / 23,322 | 2,347 / 24,800 | 2,870 / 27,933 |
| 3 | 2,088 / 20,895 | 1,364 / 16,328 | 1,593 / 16,615 |

**Reading.** The pooled medians of both sizes are inside the gate's rule on this box: the median map build grows 15%
(4.4) and 21% (4.0) against an allowance of 50%, and the median first frame 4% and 8%. One paired round is not: in round 2
the 4.0 map build is 2,870 ms against 5.0's 1,885 ms, +985 ms over an allowance of +943 ms. The noise here is larger than
the effect, so these runs bound the growth roughly but do not decide the gate. The build barely grows (+89 KB and +164
KB, almost all of it `hexes.json` and the region files in `StreamingAssets`), so download time does not move. A Mac run
of `uv run tools/headed.py boot --runs 3 --baseline 443 --baseline-first-frame 3547` on the bake PR is the real check.

## What the trial found

These are things a smaller hex breaks that a plain re-bake does not fix. Each needs a decision in step 3.

1. **`borders_bake.py` fails at both sizes.** Rule 5 (`tools/borders_bake.py:256-260`) gives a Gazette-listed place's hex
   that place's sub-district, and fails when two listed places of different sub-districts share a hex. Today they never do.
   At 4.4 km (hex (-1, 20)) and at 4.0 km (hex (-1, 22)) Kefar Sava (Jaffa sub-district) shares a hex with Kafr Saba and
   Jaljuliya (Tulkarm): `ValueError: hex (-1, 22): listed places of different sub-districts {'jaffa': ['Kefar Sava'],
   'tulkarm': ['Kafr Saba', 'Jaljuliya']}`. The trial kept the region of rules 2 to 4 for that hex to go on; the real
   bake needs a rule for it (the module docstring, `borders_bake.py:23-26`, describes keeping the rule 2-4 region "if it is
   one of theirs", which the code at :258-260 does not do).
2. **Hand-placed hexes must be placed again.** `content/sites.yaml` (16 `q`/`r` fields; each site's sourced point, which the trial re-placed
   from, is in a comment) and `content/map/locations_1948.yaml` (10, from their `lon`/`lat` by `hexgrid.settlement_hex`, :1-2) hold 5 km
   coordinates; with the new `hexes.json` the loader refuses them (at 4.4 its message names Negba's old hex, (-7, 25), as not a land hex).
   The trial re-placed them by script: a site's commented point, a `*_field` on its yishuv's hex (Q-FIELD-1), Hula from its
   old hex centre. One collision is real: **Tzrifin's own point lands on a settlement's hex** (Rishon LeZion's at 4.4, Lod
   and Ramla's at 4.0), which the loader refuses (`sites[tzrifin]: ... holds the 1948 settlement`). The trial moved it to the
   nearest free land neighbour; the bake PR needs an owner or source decision. R-CAMP-002 (`docs/rules/mvp.md:135`, the
   camp's city is the nearest by hex distance) then has to be re-checked for every camp.
3. **Which settlements share a hex changes.** Today Tel Aviv and Jaffa, Lod and Ramla, and Rishon LeZion and Rehovot share
   hexes (`settlement_hexes.yaml`). At 4.4 km only Ramla and Rehovot share one; at 4.0 km Tel Aviv and Jaffa, and Lod and
   Ramla. `MapActionTests.JaffaAndTelAvivShareAHexAndBuildingThereWorks` fails at both sizes.
4. **The goldens move only by coordinates.** `STATECRAFT_UPDATE_GOLDEN=1 dotnet test` on each trial moves `hash1950`,
   `hash1952` and `mvp-end`; `smoke5` and the four scenario goldens hold. The diff is 5 files and 20 lines: 5 hash lines
   (`manifest.txt`, `GoldenRun.cs`) and 15 snapshot lines, all hex coordinates of the scripted run's halutz and Tel Aviv housing (`veteran_halutz@-3,19` becomes `-3,21` at 4.4 and
   `-4,24` at 4.0); no number in the run changes.
5. **Tests that fail on the trial bake.** `dotnet test sim/Statecraft.sln -c Release` (before the trial re-pin): today 2,083 pass; at 4.4 and at
   4.0 the same 15 fail: the 5 golden tests above (`DeterminismTests` x3, `EraGoldenTests` x2), `PreviewTests` no-op golden,
   `SaveGameTests` 1952 round trip, `SliceSmokeTests`, `MapContentTests.TheRealMapHas1102HexesAndEvery1948Settlement`
   (`MapContentTests.cs:158`), `MapActionTests` (item 3), `SitesContentTests` x3 (Mefalsim, the sourced list, the camp's
   nearest city), `SwampTests.TheRealSwampIsThePlannedOne` and `TrainingTests` (a cohort at its city's hex). `tools/tests`
   (pytest): today 1,441 pass; at 4.4, 15 fail (not run at 4.0): `test_hexgrid.py` x2 (Mitzpe Ramon's arid hex, Haifa's coastal hex),
   `test_settlement_hexes.py` (two pinned settlements), `test_borders.py` x4 (two follow from the trial's stand-in for item
   1), `test_control.py` x3 (Huleiqat, the Ramle and Acre cuts, grey in the opening period), `test_biome_colour_bake.py` x2,
   `test_minimap_bake.py`, `test_regions_bake.py` (one scalar case) and `test_terrain_bake.py::test_the_plot_reaches_are_the_kits_footprints`.
   Unity EditMode was not run on the trial; 15 EditMode files read `hex_size_km` (listed under "Unity" below).

## Everything that changes

### Bakes and data files (one bake PR)

| File | Made by | Note |
|---|---|---|
| `tools/hexgrid.py:56` `HEX_SIZE_KM` | hand | the one source of the size; `:31` docstring pins today's counts |
| `content/map/hexes.json` | `tools/hexgrid.py` | |
| `content/map/settlement_hexes.yaml` | `tools/settlement_hexes.py` | |
| `content/corridors.yaml` (`hexes:` lists) | `tools/corridor_hexes.py` | `ContentLoader` requires consecutive hexes to be adjacent |
| `content/map/subdistrict_hexes_1948.yaml` + `.tsv` | `tools/borders_bake.py`, `tools/regions_bake.py` | fails today, item 1 |
| `content/map/armistice_hexes_1949.yaml` + `.tsv` | `tools/armistice_bake.py`, `regions_bake.py` | the `no_hex` list (zones narrower than a hex) changes: today `syria_dmz_north` and `_south`; 4.4 `_north` and `_central`; 4.0 all three |
| `content/map/control_hexes_1948_49.yaml` + `.tsv` | `tools/control_bake.py`, `regions_bake.py` | 410 hexes today, 523 at 4.4, 636 at 4.0 |
| `content/sites.yaml`, `content/map/locations_1948.yaml` | hand | item 2; the prose in `sites.yaml` that counts hexes and km (e.g. ":261 one hex and about 6.7 km") too |
| `unity/Assets/Resources/Map/MinimapRelief.png` | `tools/minimap_bake.py` | reads `hex_size_km` (`minimap_bake.py:120-124`) |
| `unity/Assets/Textures/Map/landuse.png` | `tools/land_bake.py` | reads the hex biomes |
| `unity/Assets/Textures/Map/coast.png` | `tools/terrain_bake.py` | settlement ground from hex area (`terrain_bake.py:72`, `:293-298`); `terrain_bake.png` and `ao.png` do not change (trial bytes identical at all sizes) |
| `unity/Assets/Resources/Map/BiomeColours.bytes` | `BiomeColourBake.Bake` (Unity) | its header stores `hex_size_km` (`BiomeColourFile.cs:76`); a stale file falls back to blending at boot |
| `unity/Assets/Resources/HexTiles/Boards/*.json` (3 boards on `main`) | `tools/hex_tiles/make_board.py` | opt-in hex-tile map, q/r on the 5 km grid; the open stack adds a fourth board, see "Order against the open hex-tile stack" below |
| `content/map/overrides.yaml` (#1099) | hand, or the map editor's save (step 2 of #1009) | keyed by hex q/r (`content/schema/map-overrides.schema.json:12-13`; `MapOverrides.cs:132-133`, `:148`), **empty today** (`overrides.yaml:2-4`: `terrain: []`, `control: []`, `locations: []`). No bake tool reads or rewrites it (`grep -l overrides.yaml tools/*.py` finds none), so the bake does **not** re-place rows. Plan: **no rows go in before the bake**: the loader refuses a row whose hex is not on the grid (`unknown hex`, `not a land hex`), and a row that still parses would name a different place. Rows made after the bake use the new grid. If the owner wants rows before the bake, the bake PR must re-key them by hand |
| `docs/GDD.md:203`, `:242` | hand | both say "the existing grid"; `:242` records the owner's #856 approval of it. **Not a plain edit**: the bake PR adds a "Then decided" note (owner, date, #1183) after each, the form the file already uses at `:244`, and leaves the approval text standing |

Not changed: `unity/Assets/Resources/Routes/RouteRibbons.bytes` (reads the heightmap and `route_paths_1948.yaml` only, `RouteRibbonBake.cs:8-10`),
`terrain_water.bytes` (heightmap only, `TerrainWaterBake.cs:7-9`), `route_paths_1948.tsv`, `localities_1948.tsv`.

### Rules and design values (which may need re-tuning)

The sim reads no kilometre value: `HexSizeKm` is declared (`ContentLoader.cs:43`) and never read. Every rule counts hexes,
so a smaller hex makes each one shorter on the ground.

| Value | Where | Today | Keeping today's ground distance would need |
|---|---|---|---|
| `movement_points`: veteran_soldier 3, artillery 2, armoured_car 5 | `content/unit_types.yaml:34`, `:39`, `:44` (the `:28` comment: "about 8.7 km between hex centres, 3 plain hexes is about 26 km a monthly turn") | 26.0 km a turn for 3 points | 3.41 points at 4.4, 3.75 at 4.0 (integers: 3 is 22.9 / 20.8 km, 4 is 30.5 / 27.7 km). Measured: Tel Aviv to Jerusalem takes 5 turns today, 6 at either size with 3 points, 5 with 4 |
| `move_cost` per biome, `stack_limit: 3` | `content/rules.yaml:142-149` | per hex | cost per hex is unchanged; a stack is 3 units on a smaller area |
| Project and map-action radius `map_action_max_distance: 1` | `content/rules.yaml:162`; `Projects.cs:225-227`, `MapActions.cs:81-83`; R-PROJ-001 (`docs/rules/mvp.md:79`), `docs/gdd/city-projects.md:13` | 1 hex, 8.7 km | 1 hex is 7.6 / 6.9 km; the 7-hex ring around a city covers 0.77x / 0.64x today's area |
| Camp's city by hex distance | R-CAMP-002, `docs/rules/mvp.md:135` | | re-check per camp (item 2) |
| Combat reach | `docs/gdd/military-combat.md:15` (adjacent hexes) | 1 hex | adjacency only; nothing to re-tune |
| Absorption reach | | | no hex-distance absorption rule was found in `content/`, `sim/`, `docs/rules/mvp.md` or `docs/gdd/` (the absorption objective, R-OBJ-004, counts people, not hexes) |

### Goldens and the content hash (one re-pin, alone in its merge slot)

`hexes.json`, `settlement_hexes.yaml`, `sites.yaml`, `corridors.yaml` and `locations_1948.yaml` are content files, so the
content hash moves (`ContentLoader.cs:381`, `:400-409`). The re-pin follows `docs/determinism.md:60-70`:
`STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln` rewrites `sim/Statecraft.Sim.Tests/golden/manifest.txt`, the
snapshots and the `sim/Statecraft.Sim/GoldenRun.cs:19` and `:25` constants; then the hash goes by hand into `docs/determinism.md:35`,
`docs/RELEASING.md:22` and `tools/web_check.py:11` (`GoldenManifestTests` names any place left), plus a row in the
rebaseline history. The trial says three goldens move (item 4). Plan: the bake PR is the only golden mover in its merge
slot, and a rules PR that re-tunes a design value either lands in the same PR or after it, re-pinning only if its own
change moves a golden.

### Unity: scales that are set against 5 km hexes

Shares of a hex follow the size by themselves; absolute metres do not.

| Item | Where | Today | With a smaller hex |
|---|---|---|---|
| Town footprints | `SettlementKits.cs:21` `PeoplePerHex = 60000`, `:23` `MinHexes = 0.08`, `:45` `FootprintRadiusM`; pinned in `tools/terrain_bake.py:72` | a town of N people covers N / 60,000 hexes | towns shrink by the area ratio (0.77x / 0.64x) unless re-tuned: 46,464 and 0.103 at 4.4, 38,400 and 0.125 at 4.0 keep today's metres |
| Kit scales | `SettlementKits.cs` `KitScale` (CityBlocks 60, Houses 100, Trees 100) | absolute | unchanged, so a town's buildings fill more of its hex |
| Biome blend | `BiomeBlender.cs:17` `BlendRadiusHexes = 1.72` | 1.72 hexes | blends 12% / 20% narrower on the ground; 1.95 / 2.15 keeps today's width |
| Unit tokens | `UnitShowcase.cs:37` `TrioSpread 0.40`, `RingRadius 0.66` (shares of the hex); `UnitShowcase.FigureScale` (absolute) | | the ring and spread shrink with the hex, the figures do not: check for overlap in a stack of 3 |
| Unit placement offset | `UnitPlacer.cs:70` | one hex step | follows the size |
| Camera | `CameraController.cs:34-36` `MinDistance = 6000` (the "hero zoom", owner ruling of 11 October 2026) | 6 km | absolute; check that a hex still reads at the closest zoom |
| Labels | `MapLabelsPresenter.cs:73` `DeclutterDistanceM = 150000`, `:76` `LocalityDistanceM = 40000` | absolute | unchanged; more towns per screen at a given zoom, check the declutter |
| Sea and air pieces | `SeaAirCollection.cs:255` `hexHalf = 500f * hex_size_km` | | follows the size |
| Hex-tile map (opt-in `?hex-tiles`) | `HexTileContract.cs:40` `HexScale.HexM = 5000f` (hard-coded), the three boards above; used by `HexEnvironment.cs:19-31` and five other `HexTiles/Core` files and two Editor capture tools (`HexBoardCapture.cs`, `HexRealMapCapture.cs`); switched on in `GameRoot.cs:205-208` | 5 km | must read `hex_size_km` or change with it, and the boards be re-made |
| Comments that name 5 km | `SettlementKits.Maabara.cs:91` ("4.33 km inradius"), `MapScene.cs:205` ("1,102 small objects") | | text only |
| EditMode tests that read `hex_size_km` | BorderTests, GradeTests, HexGridTests, KitTests, LocalityNameTests, MapActionUiTests, MapTests, SettlementTests, SeaAirCollectionTests, SeaTests, TerrainShaderTests, UnitPlacerSpeedTests, UnitPlinthTests, UnitShowcaseTests, UnitsInPlayTests | | to be run on the bake PR; not run on the trial |

The sim test fixtures (`Fixture.cs:175`, `MovementTests.cs:35`) are self-contained 5 km boards and need not change.

### Docs that name the size

`docs/units.md:257` and `docs/hex-tiles/CIV-LAYERS-AND-TEAM-PLAN.md:8-9` ("`HexScale.HexM` = 5,000 m", "about 8.7 km"), `content/unit_types.yaml:28`,
`docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md:120` ("1,102 hexes, 870 land") and `tools/hexgrid.py:31`.
`docs/GDD.md` names no hex size, but it does name the grid: `:203` ("Use the existing grid in Map units") and `:242` (the owner's #856 approval of "the existing grid"). Both are in the bakes table above and get a "Then decided" note in the bake PR, not a plain edit; #1183's "done when" asks that the GDD name the new size, and that note is where it goes.

## Order against the open hex-tile stack

Open PRs add more 5 km-grid content that the bake would have to move (read from their heads on 20 October 2026; none is on
`main`). #1131 (`2153830`) adds `Boards/israel.json` (it is also in #1153 and #1154, and inherited by #1155), `HexBoardOverrides.cs` and the
README section "Painting hexes by hand" for per-board `Boards/<id>.overrides.json` files keyed by q/r like #1099's. No
`*.overrides.json` is committed in #1131, #1153, #1154 or #1155, so today only the format exists. `HexScale.HexM = 5000f` is at
`HexTileContract.cs:40` in #1131, and the text "`HexScale.HexM` = 5,000 m" is already on `main` at
`docs/hex-tiles/CIV-LAYERS-AND-TEAM-PLAN.md:8`. #1162 (`f2fc344`) adds `HexBoardPickingTests` with 5 km coordinates:
`TestCase(1, 13)`, `(4, 13)`, `(3, 17)` (`:57-59`), `(0, 13)` and `(-8, 32)` (`:108-109`), and
`HexGeometry.Centre(1, 13, HexScale.HexM)` at `:127` and `:152`. #1160 (canopy containment, `HexModels.cs`) touches no 5 km coordinates, so order does not matter for it. Two orders are possible; the owner picks:

- **Bake after the stack lands.** The bake PR moves one set of boards, README text and picking coordinates, and re-makes
  any `*.overrides.json` that exist by then. Cost: the bake waits on five open PRs of the stack, four of them drafts (#1153-#1155, #1162).
- **Bake before the stack.** The stack's PRs then have to be rebased onto the new grid, with `israel.json` and the picking
  coordinates re-made in each. Cost: that rework falls on PRs the bake does not own.

Recommendation: after the stack lands. Every file above keys on the grid being baked, the stack is already open, and the
bake is meant to be the only golden mover in its merge slot, so one pass over the finished set is smaller than a pass over
the bake plus a rework of each stacked PR. The owner may weigh the wait differently.

## The PRs, in order (from #1183)

1. **This plan** (docs only; #1183 step 1, and step 2 is the owner's approval). The owner picks 4.4 or 4.0 km (or keeps 5.0) from the table.
2. **The bake PR** (#1183 step 3; alone in its merge slot, the only golden mover): `HEX_SIZE_KM`, every file in "Bakes and data files" in
   the order above, the rule for item 1, the re-placed sites and locations (with the Tzrifin decision), the tests that pin
   counts or coordinates, the three goldens and the content hash re-pinned once, `BiomeColours.bytes`, the hex-tile boards
   and `HexScale.HexM` (order: see above), `content/map/overrides.yaml` (empty, left empty), the docs that name the size
   and the two GDD lines with their "Then decided" notes. Evidence: Tel Aviv to Jerusalem on the game's path,
   the full sim, tools and EditMode suites, and `tools/headed.py boot` against the gate.
3. **A rules PR** (#1183 step 3), **only if the owner re-tunes a design value** (movement points, the map-action radius, `PeoplePerHex` and
   `MinHexes`, `BlendRadiusHexes`): each is a `DESIGN VALUE` the owner sets. Merged with or right after the bake PR, so the
   goldens move once.

## Not measured

- `--cpu 4` boot and the frame-time budget (`docs/visual-v1.md:62-68`, p95 at most 16.7 ms), and any of it on the development Mac.
- Unity EditMode on the trial bake, and how the map looks (no screenshots were taken).
- A gzip or Brotli build: only the disabled-compression build was made.
