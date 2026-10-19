# Hex tiles: Civ-style prototype hexes

Status: local prototype for review (Tal, lead designer). The default game is unchanged.

- **In the game, opt-in:** the board runs on the real map behind `?hex-tiles` (web) or `-hex-tiles` (desktop), wired in
  `GameRoot` next to `?map-look`.
- **Where the code lives:**
  - Runtime: `unity/Assets/Scripts/Map/HexTiles/`, namespace `Statecraft.HexTiles`, inside `Statecraft.Game`. That is
    `Core/` (the board placed by `HexRealMap`), `Prototypes/`, `Units/` and `HexTilesLook.cs` (the switch).
  - Board files: `unity/Assets/Resources/HexTiles/Boards/`.
  - Shipped material templates: `unity/Assets/Resources/HexTiles/Materials/`, made by the menu
    **Statecraft/Hex Tiles/Make Materials And Local Ground**.
  - Editor-only captures and tests: `unity/Assets/Editor/HexTiles/` (`Statecraft.HexTiles.Editor`).
- **It becomes the default when** the whole map is covered and its models are cleared for shipping. Then the switch
  goes.

## The idea

The map is drawn hex by hex, the way Civilization draws it:

- **The ground follows the hexes, not a real heightmap.** Each hex is flat, hills, mountain or water. The ground is
  shaped and painted from that, and blended softly across edges.
- **Each hex has one job.** It might be a city core, a forest, a farmed plain or an orange grove. One *prototype*
  fills it with a few large pieces. Units (soldiers, vehicles) are not tile jobs: they are a separate layer on top (see
  "Units are a layer, not a tile").
- **Prototypes are tried on a fixed board.** The first board is 37 hexes around Tel Aviv, in
  `unity/Assets/Resources/HexTiles/Boards/tel_aviv.json`. It uses the game's own `q`/`r` coordinates, so a board maps
  onto the real map later.

## Boards: generated, then hand-edited

`central_israel` (108 hexes, Hadera to Jerusalem, coast to the Judean hills) is what the game shows behind `?hex-tiles`.
Name another board to show it instead: `?hex-tiles=tel_aviv_crop` (desktop `-hex-tiles tel_aviv_crop`). `tel_aviv_crop` is
the Tel Aviv crop that sets the look: about 30 play hexes inside an environment ring. Ring hexes (`"env": true`) carry no
tiles. They are one continuous landscape of noise-placed woods and meandering ground (HexEnvironment), with no hex lines.
It is generated from the game's own hexes and 1948 settlements:

```bash
python3 tools/hex_tiles/make_board.py --id central_israel --lat 31.62 32.48 --lon 34.4 35.42
```

The generator does the following:

- **Terrain:** by elevation, and game-friendly as in Civ: flat below 120 m, hills to 450 m, smooth highland above. It
  draws no real cliffs or ravines, so units cross every land hex.
- **Ground:** by biome.
- **Settlements:** each gets its own tile:
  - Tel Aviv, Jaffa and Jerusalem by id;
  - the other towns as moshavot or Arab villages;
  - kibbutzim and moshavim as kibbutz tiles.
- **Every other hex:** a tile chosen by terrain, biome and coast.
- **Connections:** roads between the main towns along hex lines, and the Yarkon.

The output is a plain file, and re-running overwrites it. Hand edits that should survive a re-run go in the board's
overrides file (see "Painting hexes by hand"). `tel_aviv` stays the small test board.

## Laying out the whole map: region recipes

`tools/hex_tiles/regions.yaml` holds the recipes, as data. One command lays out every hex of the game map from it:

```bash
python3 tools/hex_tiles/make_board.py --regions tools/hex_tiles/regions.yaml --id israel --all --env-ring 1
```

That writes `Boards/israel.json`: 537 hexes in 18 regions, clipped to the play area (see below). Capture it with
`HexBoardCapture -board israel -boardOnly`, or open it in the Hex Painter. In the game it would be `?hex-tiles=israel`,
but that has not been tried yet: it builds about 500 tiles on the real map, so check the load time before relying on
it. `--regions` needs PyYAML. With `--regions`, the generator works like this:

- **Play area.** `defaults.clip` in the recipes, or `--clip`, bounds the board.
  - `mandate` keeps the land of Mandatory Palestine on 15 May 1948. That is every hex the map's own sub-district layer
    (`content/map/subdistrict_hexes_1948.yaml`) puts in a sub-district. One hex of the land outside stays as
    environment, so the edge reads as land going on. The sea stays within `--sea-ring` hexes of playing land, and the
    rest of the game map (south Lebanon, the Golan, Transjordan, Sinai) is dropped.
  - `none` keeps every hex of the game map (920).
  - Which line bounds the play area is a design call. The Mandate outline is the stand-in until Tal and the owner
    choose. The 1949 armistice lines are the other option, and they are not built.
- **Regions.** Eighteen landscape zones: the Hula, the Upper Galilee, the Dead Sea shore, the Jordan valley, the
  Carmel, the Jezreel valley, the Lower Galilee, the Sharon, the coastal plain, Jerusalem, the Judean desert, the
  Judean hills, the Shephelah, the northern Negev's loess, the Ramon highlands, the Arava, the central Negev and the
  central hills (Samaria). Each is a `polygon` of `[lon, lat]` points, or a `hexes` list of `q, r` pairs. A hex goes
  to:
  - the first region that lists it;
  - else the first region whose polygon holds its centre;
  - else `defaults.region`.

  The polygons are landscape zones drawn roughly by eye. They are not borders.
- **Ground.** The region's `ground` table maps each biome of `content/map/hexes.json` to a ground layer, over the
  defaults.
- **Settlements.** First the game's 25 settlements. Then the 1948 localities (`content/map/localities_1948.tsv`) of at
  least `min_population` people, largest first. Each settlement takes one hex, and a hex that is already taken keeps its
  first owner. The tile is chosen by the region's `settlements` rules, then the defaults'. A rule can match on:
  - `id`;
  - `type` (city, town, arab_town, village, kibbutz, moshav);
  - `tier` (from `tiers`, by the 1945 population);
  - `terrain`;
  - `biome`.
- **Other land.** The region's `nature` rules, then the defaults'. A rule can match on `terrain`, `biome` and `coastal`.
  It sets a `prototype`, or a weighted `pick` drawn by a stable per-hex value. It may also set a `ground`, and a
  `set_terrain` for a tile that wants a different presentation terrain. For example, the Hula swamp flattens its valley
  hexes, whose average height reads as hills.
- **Roads.** They follow the traced 1948 main roads (`content/map/route_paths_1948.tsv`), hex centre to hex centre.
- **Environment.** `--env-ring N` makes the land within N hexes of the board's edge environment: landscape with no tiles
  and no hex lines. Each region's `flora` sets the trees and rocks on its environment hexes (see below).
- **Settings.** Without `--regions` the generator keeps its built-in choices. `central_israel` and `tel_aviv_crop`
  regenerate exactly as before.

### Environment trees by region

The board file carries a `regions` list, and each hex names its `region`. `HexEnvironment` reads the region's
dress:

- `flat` and `hills` species, each with a `model`, a stand-in `shape` (tree, pine, palm, rock or box) and a `weight`.
  The species are picked by a noise field, so woods of one kind clump.
- `density`, a multiplier on the woods.
- a `rock` model, its `rockChance` (two tries a hex), and `rocksOnFlat`.

The recipes plant, by region:

| Region | Trees | Rocks |
|---|---|---|
| Hula | Papyrus (stand-in) and Tabor oak | |
| Upper and Lower Galilee | Tabor oak, Aleppo pine, olive and carob | |
| Carmel | Aleppo pine, oak and carob | |
| Jezreel valley | Cypress windbreaks and carob | |
| Sharon and the coastal plain | Citrus and date palm | |
| Jerusalem, the Judean hills and the Shephelah | Olive, carob, cypress and pine | |
| Judean desert, Dead Sea, Negev, Ramon and Arava | Acacia, and date palm at the Dead Sea and in the Arava | Desert rock |
| Jordan valley | Date palm and acacia | |

A hex with no region, as on every board made before regions, gets the built-in mix: olive and carob on the flat, oak
and pine on hills, limestone outcrops on hills.

### Regional variants of the farm and forest tiles

Each farm and forest type has two variants for each of the north, centre and south: 30 tiles, all `Kind` farm or
forest.

| Type | Ids | Base |
|---|---|---|
| Wheat | `wheat_fields_<region>_a` and `_b` | `WheatFields` |
| Citrus | `citrus_orchard_<region>_a` and `_b` | `CitrusOrchard` |
| Olive | `olive_terraces_<region>_a` and `_b` | `OliveTerraces` |
| Mixed farm | `mixed_farm_<region>_a` and `_b` | `MixedFarm` (`mixed_farm`) |
| Forest | `forest_<region>_a` and `_b` | `PineForest` |

- **One variant, one file.** Each variant is a short file in `Prototypes/` named by its region (`wheat_fields_north_a`
  is `WheatFieldsNorthA.cs`). It subclasses its base and overrides only the base's `protected virtual` members: grid
  turn, parcel size, crops, species, caps, yard, and `Dress`.
- **The regions differ:**
  - north is greener, with oak and pine hedgerows;
  - centre has coastal-plain citrus;
  - south is drier and sparser, with date palms and acacia. Its forest is an acacia or date-palm grove, not a pine
    wood.
- **a and b differ** in layout, parcel size, crop mix and species, so they read apart at board distance.
- **Choosing a variant.** The region recipes in `regions.yaml` pick between the variants of a hex's region with the
  stable per-hex `pick`.
- **`HexVariantTests` checks them:**
  - every variant is registered with its kind and region;
  - each variant builds differently from its pair and from its base;
  - the base tiles still build exactly as before. Their placements and paints are pinned by hash, so a base edit that
    changes what `wheat_fields` and the others build fails the test.

## Painting hexes by hand: overrides and the Hex Painter

**Overrides.** Hand edits that must survive regeneration go in `Boards/<id>.overrides.json`, beside the board. The file
has the same shape as the map editor's `content/map/overrides.yaml` (#1099): one list per field of `{hex: {q, r}, value}`,
and no hex appears twice in a list.

```json
{ "board": "israel", "prototype": [ { "hex": { "q": -1, "r": 18 }, "value": "kibbutz" } ], "region": [] }
```

- **The fields are hex-tile presentation only:**
  - `prototype`, the tile;
  - `ground`, its ground layer;
  - `unit` and `facing` (degrees, as a string), the unit standing on it;
  - `region`, whose trees dress an environment hex.
- **The split with the map editor (#1009).** Terrain, control and settlement locations belong to the map editor, in
  `content/map/overrides.yaml`. That file is in the content hash and the sim reads it. The hex overrides stay out of
  `content/`, so painting a hex never moves the content hash.
- **The generator reads both files:**
  - It applies the map editor's terrain (a hex's biome) and location moves (a settlement's hex) first, so boards follow
    the game map.
  - It applies the hex edits last, on every run, and reports any edit whose hex is not on the board.
- **`HexBoard.Load` applies the hex edits again at load,** so an edit shows in the game before the board is
  regenerated.

**The Hex Painter** (**Statecraft/Hex Tiles/Hex Painter**, editor only):

1. Pick a board and press **Load into scene**. It builds the board in the open scene, one object per hex. **Add the
   captures' light and grade** lights an empty scene.
2. Click a hex in the Scene view. It is outlined, and the window shows its terrain, region and note.
3. Change its prototype, ground, unit, facing or region. The painter rebuilds only that hex (its pieces, its trees, its
   unit, and the ground round it) and writes the edit to the board's overrides file. It does not edit terrain, control or
   locations: those are the map editor's (#1009).

The board in the scene is never saved: load it again after a script reload.

**The palette.** Below the selected hex, the painter lists every registered tile.

- **How it is grouped:**
  - Tiles are grouped by kind (city, town, village, farm, forest, nature, other), then by region (north, centre, south,
    any). The groups come from `[HexPrototype(Kind, Region)]`, so a new tile appears by itself.
  - Units come after the tiles, as characters (a squad of 8) and vehicles (one each).
- **Thumbnails.** Each entry shows its thumbnail, or its id where none has been made yet. **Make thumbnails** (or
  `HexPaletteThumbs.Run` in batch) renders them into `asset-staging/ui-local/hex-tiles/thumbs/`. It replaces the open
  scene after asking to save, then reopens the scene and the board.
- **Paint tiles.** Pick a tile, which switches the mode to *Paint tile*, then click or drag over hexes in the Scene
  view. Each hex is rebuilt and written to the board's overrides. The "no tile" brush clears a hex's prototype.
- **Stand units.** Pick a unit and a facing (the six neighbour directions, or any angle), then click a hex, or use
  *Stand the unit on the selected hex*. This writes `unit` and `facing`. *Clear unit* removes it, and the removal is
  recorded, so the hex stays empty after regenerating.
- **Previews.** *Day*, *Dusk*, *Night* (see "Time of day") and *Under attack* (see "Under attack") are previews only.
  - They are never written.
  - They are undone on Unload, on loading another board, on closing the window and on a script reload.
  - After a painted hex is rebuilt, its under-attack look is shown again and the time preset is applied again.

To check the painter's path from the command line, use `HexPainterCapture`. It loads the board, shoots the hex, makes
one edit, and shoots it again. The edit goes to a scratch overrides file in the capture folder unless `-overrides` names
another:

```bash
Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.HexTiles.Painter.HexPainterCapture.Run -board israel -hex -3,19 -thumbs -ops "prototype=kibbutz;unit=palmach_squad@120;time=dusk;time=night;attack"
```

Each operation writes `<n>-<op>/before.png` and `after.png`. The capture also writes `painter.txt` and, with
`-thumbs`, the palette's contact sheet: `palette.png` and `palette.txt`. The older `-field <f> -value <v>
[-facing <deg>]` form still works.

## Adding a prototype hex (the contributor workflow)

**One prototype is one new file** (or one unit is one new file under `Units/`). **You never edit anyone else's file.**

1. **Copy the reference** `unity/Assets/Scripts/Map/HexTiles/Prototypes/TelAvivCity.cs` to
   `Prototypes/<YourName>.cs`. Then:
   - Give it a new `[HexPrototype("your_id", Kind = "...")]`, in lower_snake_case and unique.
     - `Kind` is its group in the painter's palette, one of `HexPrototypes.Kinds`: city, town, village, farm, forest,
       nature or other.
     - Add `Region = "north"`, `"centre"` or `"south"` for a regional variant.
   - Use the namespace `Statecraft.HexTiles.Prototypes`.
   - Write it in C# 9: no `record` types.
   - Keep one prototype class per file. Do not copy `WheatFields.cs`: it holds two (`wheat_fields` and `grazing_land`).
     `MountainPeaks.cs` is a clean single-class example of a light context tile.
2. **Implement `IHexPrototype`:**
   - `Title`, the `Terrain` you want under it, the `Ground` layer, and a `TriangleBudget` (see `HexScale.Budget*`).
   - `Build(HexTileContext ctx)`. Place everything through `ctx`.

   `Terrain` shapes the ground (`HexBoardSurface`, heights in metres above sea level):

   | Terrain | What it looks like |
   |---|---|
   | `Flat` | About 80 m, with roughly ±50 m of gentle noise |
   | `Hills` | A dome rising from 80 m to up to about 700 m above it at the centre |
   | `Mountain` | A raised base (80 + 500 m) and a dome (+600 m). A mountain hex with no prototype gets `mountain_peaks` rocks |
   | `Water` | Below sea level (about -520 m). Anything under 4 m reads as water, and `Place*` refuses it |

   `Ground` must be one of the `HexGround.Layers` ids:

   | Ground | Look |
   |---|---|
   | `grass` | Green grass with rock |
   | `plains` | Drier yellow-green grassland (the city and fields use it) |
   | `hills` | Earth and rock |
   | `mountain` | Rocky terrain |
   | `desert` | Sand |
   | `beach` | Shore sand (the board also paints it on every shoreline) |
   | `seabed` | Under water (painted automatically below the sea) |

3. **Use only the context API** (`Core/HexTileContext.cs`):
   - `ctx.S` is the hex size. Size everything as a share of it.
   - `ctx.Centre` is the hex centre. `ctx.At(bearingDeg, share)` gives a point at a bearing and distance from it.
     `ctx.RandomPoint(margin)` gives a random point in the hex. `ctx.Inside(p, margin)` tests whether a point is in.
   - `ctx.Rng` and `ctx.Range(a, b)` are the *only* randomness allowed. They are seeded by the hex, so the same hex always
     builds the same way.
   - `ctx.GroundY(p)`, `ctx.IsWater(p)` and `ctx.SlopeDeg(p)` read the ground.
   - `ctx.PlaceByWidth(model, at, yaw, widthM, standIn, tint)` places buildings and rocks, sized by footprint.
   - `ctx.PlaceByHeight(model, at, yaw, heightM, standIn, tint, footprintShare)` places trees, figures and animals,
     sized by height.
   - `ctx.PlaceFlat(at, yaw, width, depth, colour)` places fields, roads and plazas.
   - Every `Place*` refuses, and returns false, when the footprint leaves the inset hexagon or lands on water or steep
     ground. A refused placement is normal, so just carry on.
   - `PlaceFlat` also refuses when the ground under it rises more than `maxRiseM` (default 60 m). Large flats can fail
     even on flat hexes because of the noise, and always on hills. Pass a larger value (`WheatFields` passes 120) or use
     many small flats.
   - On mountains the slope limit (`maxSlopeDeg`, 30 or 35 by default) refuses most pieces. `MountainPeaks` passes 90.
   - **There is no collision or reservation.** `Place*` never checks earlier pieces. Plan positions so pieces do not
     overlap: lots, rings, rows (the reference uses rings and avenues). Do not stack flats on the same spot, because
     co-planar flats flicker. Vary their size or offset them instead.
   - `tint` colours stand-ins and flats only. A real model keeps its own material.
   - **Facing:** at yaw 0 a model's front faces north (+Z), if it was exported the usual way. Several Tripo vehicles face
     +X instead. Check the close capture, and add ±90 to the yaw if a vehicle drives sideways. The camera looks north from
     the south, so the reference turns its landmark 180° (`GridYaw + 180f`) to show the main façade.
4. **Keep to the scale** in `Core/HexTileContract.cs` (`HexScale`). The goal is few, large pieces, as in Civ VI:

   | Piece | Size (share of S) | Per hex |
   |---|---|---|
   | Building footprint | 0.12 to 0.20 | 12 to 25 in a city hex |
   | Landmark | up to 0.40 | |
   | Tree height | 0.12 to 0.20 | 25 to 60 in a forest, 15 to 40 in an orchard |
   | People and animals as a tile's life | small (the reference uses about 0.075 for townspeople, grazing about 0.06 for sheep) | a few |

   Soldiers and vehicles are units, not tile pieces. See "Units are a layer, not a tile".
5. **Models** are ids (see `Core/HexModels.cs`):
   - `tiles:<your_id>/<name>` loads your own model from `Assets/Resources/HexTiles/<your_id>/<name>.prefab`.
   - `sandbox:<name>` is the local EZ-Tree trees, Poly Haven rocks and building kit.
   - `proto:<name>` is the local Tripo models (characters, animals, vehicles, landmarks).
   - A trailing `*` picks among the variants `name`, `name_2`, `name_3` and so on.
   - **A missing model draws its stand-in** (`StandIn.Box/Tree/Pine/Palm/Rock/Figure`). Your tile must read well
     with stand-ins alone, because most machines will not have the local models (see "What is local-only").
   - To place a deliberate stand-in-only piece, use a model id that does not exist (for example `tiles:<your_id>/tent`).
     It shows as `~` in the stats line.
6. **Triangle costs.** A flat piece costs 12 triangles. A stand-in costs tens. A local model costs what its mesh has,
   and the stats line reports the tile total. The common local models, approximately:

   | Model | Triangles |
   |---|---|
   | `sandbox:aleppo_pine_lite` | ~1,600 |
   | `sandbox:tabor_oak_lite`, `sandbox:carob_lite` | ~650 |
   | `sandbox:olive_lite`, `sandbox:citrus_lite` | ~1,600 |
   | `sandbox:cypress_lite` | ~1,600 |
   | `sandbox:date_palm` | ~280 |
   | `sandbox:limestone*`, `sandbox:desert_rock*` | ~700 |
   | `sandbox:kit_<style>*` | ~700 to 1,400 |
   | `sandbox:hq_white_city*` | ~3,500 (the hero ~9,000) |
   | `proto:` v2 vehicles | 18,000 to 31,000 |
   | `proto:` v2 characters (`figure-*-c##-1948-v2`) | ~3,000 to 4,000 |
   | `proto:figure-sheep-lite-1948`, `proto:figure-donkey-lite-1948` | ~2,500 |

## Seeing it, testing it

Render a single prototype on the board with its ground and neighbours. The output is the mid (about 3 hexes) and close
(1 hex) 4K shots, plus a stats line in `asset-staging/ui-local/hex-tiles/`:

```bash
Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.HexTiles.Capture.HexBoardCapture.Run -prototype your_id
```

Leave out `-prototype` to render the whole board, which adds `board.png`. Each prototype and unit is shot once, at its
last hex in the board file. `-boardOnly` skips those shots. A board too large for the standard shot (the whole country)
is framed whole. Add `-on <tile>` to try your prototype on
another tile's hex without editing the board file, for example `-prototype tel_aviv_city_mixed -on tel_aviv_city`
(an A/B variant on the city's hex). The stats line lists:

- the pieces, and how many fell back to stand-ins (a missing model is marked `~`);
- triangles against the tile's budget.

Run the tests. Every registered prototype is checked automatically, with no edit to the tests:

```bash
Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath unity -runTests -testPlatform EditMode -testFilter HexPrototypeTests
```

The tests require that every prototype:

- has a lower_snake_case id;
- names a palette kind, and a known region if any;
- builds something;
- is deterministic;
- keeps every piece inside its inset hexagon;
- stays within its triangle budget.

They also check that every board names only known prototypes, terrains and ground layers.

After any Unity run, revert the settings it rewrites before committing:

```bash
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset unity/ProjectSettings/QualitySettings.asset
```

## Units are a layer, not a tile

Lead designer's rules (Tal, 18 October):

- **Units (characters, vehicles) are never part of a tile prototype.** A board hex has a tile `prototype` (the land) and
  optionally a `unit` (who stands on it), plus `facing` (degrees from north).
- **A character unit is exactly 8 characters, and only the characters.** No tents, tracks, paving or other ground
  change: they walk on the land and move between hexes.
- **A vehicle unit is exactly one vehicle, and no road.**
- **Units are drawn larger than buildings**, as Civ draws them (`HexUnitScale`):

  | Measure | Share of S |
  |---|---|
  | Figure height (`HexUnitScale.FigureHeight`) | 0.26 |
  | Spacing between figures | 0.15 |
  | Vehicle length | 0.48 |

**Defining a unit.** One file per unit under `unity/Assets/Scripts/Map/HexTiles/Units/`, marked `[HexUnitType("your_id")]`
and implementing:

```csharp
public interface IHexUnitType { string Title { get; } HexUnit Create(); }
```

`HexUnit` (`Core/HexUnits.cs`) has `Members`, each a `HexUnitMember`:

| Field | Meaning |
|---|---|
| `Model` | Model id, as for tiles |
| `StandIn` | Drawn when the model is missing (`Figure` by default, `Box` for vehicles) |
| `Offset` | Place in the formation: x to the right, z forward, as shares of S |
| `HeightShare` | Height as a share of S (or the length, with `ByWidth`) |
| `CycleShare` | Distance per walk or wheel cycle, as a share of S |
| `ByWidth` | Size by footprint instead of height (vehicles) |
| `YawOffset` | Degrees to turn the model so its front faces the way it travels (many Tripo vehicles need -90 or +90) |

**Movement.** The simulation decides the move from hex A to hex B. The view (`HexUnitMotion`) then:

- glides the unit along the hex centres and turns smoothly at each centre;
- plays the walk clip locked to distance travelled, so feet do not slide (stride is about 0.86 × figure height, so
  set `CycleShare` to about 0.86 × `HeightShare`), and the idle clip when it stands;
- turns vehicle wheels by distance.

Tripo clips are exported in place (no root motion), so units land exactly on hex centres.

**Character clips.** A character needs a `walk` take and an `idle` take. Clips are matched by name, so the names must
contain "walk" and "idle". The converter `asset-staging/tools/kit/characters_to_fbx.py` keeps them when run with
`CHAR_KEEP=walk`. This tool is local.

**Seeing a move.** Render the move video:

```bash
Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.HexTiles.Capture.HexUnitMoveCapture.Run
```

The output is `asset-staging/ui-local/hex-tiles/unit-move/unit-move.mp4`, plus its frames. The MP4 is written only
when ffmpeg is on PATH. `-captureDir` and `-captureSize 1920x1080` are optional.

## Buildings: the shipped b*-v2 models

The building ids the tiles use (`sandbox:hq_white_city*`, `sandbox:hq_white_city_hero` and every `sandbox:kit_*`) draw
the shipped b*-v2 town models at full detail.

- **Where they come from:** `TownModelSet`, `Assets/Art/Settlements/TownModels.asset`, the set the map scene already
  loads.
- **How the ids map:** `HexModels.TownAliases` holds the mapping. No prototype file names the models.
- **Why:** our own decimated kit had lost faces, and its materials cull back faces, so the ground showed through walls
  and windows (Tal, 19 October 2026).
- **Fallbacks:** without the set, an id draws its local sandbox prefab, then its stand-in.
- **Naming a model directly:** use `town:<folder>`, for example `town:b04a-haifa-stone-balconies-v2`. The folders are
  listed in `TownModels.Ids`. These models are cleared on main, so they draw on every machine.
  - `town:<folder>@mid` draws the set's baked mid level. Most mid levels see through, so don't use it.
- **Remaps.** b11 shikun and b21 workshop are not in the set and are not pulled into a build. `kit_white_city_4` draws
  b13a; `kit_jaffa_2` and `kit_negev_3` draw b03a. `kit_haifa` draws b17, because b04a's full mesh has open wall
  triangles.
- **Cost:** a full mesh is about 6,000 triangles, so the city, village and farm budgets were raised to match. A lighter
  level is a follow-up: the LOD generator's in-between levels are whole for most models, but they smear their
  textures.
- **Checking a new building mesh:**
  - `HexModelMeshTests` counts view rays that meet a model but none of its front faces: the ground shows there.
    b02a and b13a are allowed their thin balcony slivers. The report goes to
    `asset-staging/ui-local/hex-tiles/round2/buildings/mesh-report.txt`.
  - `HexModelCapture.Run` shoots every building alias from four sides.
  - On Tripo meshes, open-edge counts are noise. Only see-through rays find holes.

## Figures: the Collection's whole characters

- **One character per unit.** A character unit is one model × 8 (`HexUnitTypes.Squad(title, model)`), never a mix.
  `palmach_squad` is 8 Palmach scouts (c12f).
- **Where the figures come from.** In the editor (captures, painter, tests), the `proto:figure-*` ids draw the cleared
  Collection figures (`HexModels.FigureAliases`, `Assets/Art/Collection/<figure>/<figure>-near|far.prefab`).
  - Our own decimated copies (`characters_to_fbx.py`) had lost faces, 2.7 to 6.5% see-through.
  - Soldiers take `near`. Townspeople take `far`, except c01, whose far level is holed.
  - The grazing flock's sheep and donkey take `far`; their decimated copies were 6 to 8% see-through.
- **In a player** the Collection streams from bundles, so the hex tiles still draw the local proto figures, then
  stand-ins. Wiring the bundles in is a follow-up.
- **No walk cycle.** The Collection's near figures have an idle clip only, so a moving squad glides.
- **Checking a figure:** `HexUnitTests` checks one model per unit, whole figures, facing, height and the fallback.

## Time of day

- **Turning it on:** add `&hex-time=dusk` or `&hex-time=night` to a `?hex-tiles` URL (desktop: `-hex-time dusk|night`).
- **What counts:**
  - Only a top-level query parameter counts.
  - A value after `#`, a bad value, or the flag alone gives day.
  - `?hex-time` without `?hex-tiles` does nothing.
- **The presets** (`Core/HexTimeOfDay.cs`, presentation only):
  - **Day** is the warm `HexLook`, unchanged.
  - **Dusk** is a low (14°) west-north-west sun: deep warm light and long shadows under a cooler sky.
  - **Night** is a dim blue-white moon, a dark-blue ambient and a lower exposure. It keeps about 55–60% of day's
    brightness, so tiles and units stay readable. City and town tiles get warm lit windows: one mesh, under the board
    root as `HexNightWindows`.
- **Teardown:** the preset's undo runs before `HexLook`'s. It puts back:
  - the grade values and override states it changed;
  - the sun's rotation, intensity, colour and shadows;
  - the ambient mode, colours and probe.
  It removes only the volume components it added, and destroys the window mesh and material.
- **Captures** take `-time day|dusk|night`. Night also darkens the background.
- **In the Hex Painter:** the window has the same switch.

## Under attack

`HexUnderAttack.Show(boardRoot, surface, q, r)` marks a hex as under attack: three grey smoke columns, a low warm glow
where each rises, and a muted red border that pulses slowly. The columns rise in the hex's north half, so they stand
behind a unit or the city's centre as the board camera sees it.

- **Presentation only:** nothing reads or writes the simulation, and it is never written to a board or its overrides.
  It is the visual state the sim can drive later; movement and combat rules are the owner's
  (`docs/gdd/military-combat.md`).
- **Removing it:** `Hide(boardRoot, q, r)` removes it and frees the 3 meshes, the 3 materials and the texture it made.
  `IsShown` tells whether a hex has it.
- **Animation:** it animates in edit mode and in play (`HexUnderAttackLook`). For a deterministic frame, in captures and
  tests, set `look.Hold = true` and call `look.Time(t)`. Smoke placement is seeded by (q, r).
- **Materials:** they ship as templates in `Resources/HexTiles/Materials` (`HexAttackSmoke`, `HexAttackGlow`,
  `HexAttackBorder`), made by **Statecraft/Hex Tiles/Make Materials**. After running that menu, revert the five older
  templates it rewrites.
- **Cost:** about 723 triangles and 3 draws.
- **Captures:**

```bash
Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.HexTiles.Capture.HexUnderAttackCapture.Run -board israel
```

## Review and hand-in

- **Branch:** work on `hex/<your_id>`. The change is your one prototype file (or one `Units/<id>.cs`), plus your models
  under `Resources/HexTiles/<your_id>/` if they are licensed for the repository (see below).
- **Pull request:** include your mid and close captures, and your stats line.
- **Who decides:** Tal decides which prototypes go on a board, and edits the board files.
- **Do not edit:** `Core/`, `Capture/`, `Tests/` or another contributor's prototype. Propose a contract change in an
  issue instead.

## What is local-only (read before relying on a model)

- **Tripo models are local-only while their rights are pending:**
  - figures, animals, vehicles and landmarks (`proto:`);
  - the decimated city kit (`sandbox:kit_*`). The tiles no longer draw it while the shipped town models are found (see
  "Buildings").
  They are never committed, shared or built into a shareable build. On other machines they draw as stand-ins.
- **Not yet in the repository:**
  - the EZ-Tree trees (MIT), Poly Haven rocks and ground textures (CC0) under `sandbox:`;
  - the ground folder.
  Adding them is an asset batch that needs Tal's approval and `ASSETS.yaml` rows. Until then the board paints flat
  colours and those models draw as stand-ins.
- **Licences for your own models:** they need a licence that allows the repository, and an `ASSETS.yaml` row
  (CONTRIBUTING.md item 7). Material from the Unity Asset Store is never accepted.

## Files

| Path | What it is |
|---|---|
| `Core/HexTileContract.cs` | `HexTerrain`, `[HexPrototype]`, `IHexPrototype`, `HexScale`, the registry, hex geometry |
| `Core/HexTileContext.cs` | The API a prototype uses, and the placement record |
| `Core/HexModels.cs` | Model ids, variants, stand-ins, triangle counts, the shipped town models (`TownAliases`) |
| `Core/HexTimeOfDay.cs` | Day, dusk and night presets (`?hex-time`) |
| `Core/HexUnderAttack.cs` | The under-attack look for a hex |
| `Core/HexBoard.cs` | Board files, the Civ-style ground (`HexBoardSurface`), ground layers |
| `Core/HexBoardBuilder.cs` | Terrain, sea, rendering, tile edges, per-tile reports |
| `Core/HexUnits.cs` | `HexUnit`, `HexUnitMember`, and `HexUnitMotion` (moving a unit along hex centres) |
| `Core/HexBoardOverrides.cs` | A board's hand edits (`<id>.overrides.json`) |
| `Capture/HexBoardCapture.cs` | The capture command (`-prototype <id>`, plus `-on <tile>` to try it on another tile's hex; `-time`) |
| `Capture/HexModelCapture.cs` | Four-side shots of each building model, and the see-through check (`HexMeshCheck`) |
| `Capture/HexUnderAttackCapture.cs` | Before and after shots of the under-attack look |
| `Painter/HexPainterWindow.cs` | The Hex Painter window |
| `Painter/HexPainter.cs` | The painter's write-back and one-hex rebuild |
| `Painter/HexPainterCapture.cs` | The painter's path from the command line (`-ops`, `-thumbs`) |
| `Painter/HexPalette.cs` | The palette's groups, facings and brushes |
| `Painter/HexPaletteThumbs.cs` | Palette thumbnails and the contact sheet |
| `Painter/HexPainterSwitches.cs`, `HexPainterStage.cs` | The painter's time-of-day and under-attack previews, and its light and grade |
| `tools/hex_tiles/regions.yaml` | The region recipes |
| `Capture/HexUnitMoveCapture.cs` | The unit-move video |
| `Tests/HexPrototypeTests.cs` | The contract tests |
| `Tests/HexVariantTests.cs`, `HexModelMeshTests.cs`, `HexTimeOfDayTests.cs`, `HexUnderAttackTests.cs`, `HexBoardCaptureTests.cs` | Variants, whole buildings, time of day, under attack, whole-board framing |
| `Prototypes/` | One file per prototype hex (`TelAvivCity.cs` is the reference) |
| `Prototypes/MountainPeaks.cs` | `mountain_peaks`, the rocks on a mountain hex |
| `Prototypes/WheatFields.cs` | `wheat_fields` and `grazing_land` (two classes in one file: do not copy it) |
| `Prototypes/TelAvivCityMixed.cs` | `tel_aviv_city_mixed`, the A/B variant of the reference |
| `Units/` | One file per unit type (`[HexUnitType]`) |
| `Boards/` | Board layouts (Tal's) |
