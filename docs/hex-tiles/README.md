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

The output is a plain file: edit it by hand afterwards, and re-running overwrites it. `tel_aviv` stays the small test
board.

## Adding a prototype hex (the contributor workflow)

**One prototype is one new file** (or one unit is one new file under `Units/`). **You never edit anyone else's file.**

1. **Copy the reference** `unity/Assets/Scripts/Map/HexTiles/Prototypes/TelAvivCity.cs` to
   `Prototypes/<YourName>.cs`. Then:
   - Give it a new `[HexPrototype("your_id")]`, in lower_snake_case and unique.
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

Leave out `-prototype` to render the whole board, which adds `board.png`. Add `-on <tile>` to try your prototype on
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
  - the decimated city kit (`sandbox:kit_*`).
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
| `Core/HexModels.cs` | Model ids, variants, stand-ins, triangle counts |
| `Core/HexBoard.cs` | Board files, the Civ-style ground (`HexBoardSurface`), ground layers |
| `Core/HexBoardBuilder.cs` | Terrain, sea, rendering, tile edges, per-tile reports |
| `Core/HexUnits.cs` | `HexUnit`, `HexUnitMember`, and `HexUnitMotion` (moving a unit along hex centres) |
| `Capture/HexBoardCapture.cs` | The capture command (`-prototype <id>`, plus `-on <tile>` to try it on another tile's hex) |
| `Capture/HexUnitMoveCapture.cs` | The unit-move video |
| `Tests/HexPrototypeTests.cs` | The contract tests |
| `Prototypes/` | One file per prototype hex (`TelAvivCity.cs` is the reference) |
| `Prototypes/MountainPeaks.cs` | `mountain_peaks`, the rocks on a mountain hex |
| `Prototypes/WheatFields.cs` | `wheat_fields` and `grazing_land` (two classes in one file: do not copy it) |
| `Prototypes/TelAvivCityMixed.cs` | `tel_aviv_city_mixed`, the A/B variant of the reference |
| `Units/` | One file per unit type (`[HexUnitType]`) |
| `Boards/` | Board layouts (Tal's) |
