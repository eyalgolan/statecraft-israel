# Civ layers and the team plan for hex tiles

Status: a design note for Tal (lead designer). It decides nothing. Everything marked **Proposal** needs Tal's yes. The
hex tiles are presentation only: an Editor-only assembly (`unity/Assets/Editor/HexTiles`) that draws what the
deterministic simulation (`sim/`, data in `content/`) decides. Nothing here changes the shipped game, its look or the
simulation goldens.

Read with `docs/hex-tiles/README.md` (the contributor contract). Scale reminder: `HexScale.HexM` = 5,000 m is the hex
circumradius (`content/map/hexes.json` `hex_size_km: 5.0`), so a hex is about 8.7 km flat to flat. One hex is a whole
landscape: the Bauhaus "White City" is one hex, and the Tel Aviv to Jerusalem road, from (-3,19) to (0,23), is 7 hexes.

State of the working tree on 18 October, evening: `Core/` is being extended now, uncommitted. It has gained
`ctx.PaintGround` and `ctx.PaintStroke` (ground painted into the splat), the crop layers `wheat`, `green_crop`,
`ploughed` and `dirt`, and board-level `roads` (hex centre to hex centre, painted dirt) and `rivers` (along hex
corners, carved as a channel). The tables below count these as "in progress".

## 1. Civ's layer model, mapped to ours

Civ stacks a hex in layers. Civ VI has terrain, then a hills or mountain modifier, a feature, a resource, an
improvement or district, routes, rivers on edges, borders and units. Civ VII renames base terrain as *biome*
(grassland, plains, tropical, desert, tundra, marine) and keeps the terrain types (flat, rough, mountainous, water). It
also moves rivers into tiles: minor rivers, plus navigable rivers that fill whole tiles. Settlements grow onto rural
tiles (improvements) and urban districts, and an urban district with two buildings becomes a *quarter*.

| Civ layer | Civ examples | Ours today | Status | 1948 examples |
|---|---|---|---|---|
| Base terrain / biome | grassland, plains, desert, coast | `BoardHex.ground` (grass, plains, hills, mountain, desert, beach, seabed); `hexes.json` `biome` (7 kinds) | Have (needs a biome mapping, see WP9) | Coastal plain = `plains`; Negev = `desert` |
| Terrain modifier | flat, hills, mountain | `BoardHex.terrain` = `HexTerrain` Flat/Hills/Mountain/Water | Have | Judean hills at Jerusalem (0,23) = hills, 689 m |
| Water | coast, ocean, lake | `terrain: water`, `seabed`, shore beach painted automatically | Have | Mediterranean; Sea of Galilee and the Dead Sea (`lake`, 16 hexes) |
| Feature | woods, rainforest, marsh, floodplain, oasis | Drawn as a whole-hex `prototype` (`pine_forest`, `coastal_dunes`) | Partly: no separate field, so a hex can't be both "forest" and "village" | JNF pine forests; Hula swamp (marsh); Jordan floodplain; Ein Gedi (oasis) |
| Resource | wheat, fish, iron, citrus | Nothing | Missing | Jaffa oranges; Dead Sea potash; fish at Jaffa and Acre; Negev sheep |
| Improvement / rural tile | farm, mine, pasture, plantation, camp, fishing boats | `prototype` art (`wheat_fields`, `grazing_land`, `citrus_orchard`) plus `PaintGround` crop layers | Have (as art) | Kibbutz fields; citrus plantations at Petah Tikva; Dead Sea Works as a mine |
| District / urban quarter | campus, harbour; VII quarters | `prototype` (`tel_aviv_city`, `moshava_village`, `arab_village`) | Have (one per hex) | Bauhaus White City; Jaffa port; Jerusalem Old City |
| Settlement | city centre | `content/map/settlements_1948.yaml` and `settlement_hexes.yaml` (owned by the sim) | Have, in the sim | Tel Aviv, Haifa, Jerusalem |
| River | VI: on edges; VII: minor in tile, navigable fills the tile | `BoardRiver`, along hex corners, carved channel | In progress (Civ VI style) | Yarkon (north edge of Tel Aviv), Kishon (Haifa bay), Jordan |
| Road / railroad | road, railroad | `BoardRoad`, centre to centre, painted dirt | In progress; no railway | Tel Aviv to Jerusalem road via Bab al-Wad; Jaffa to Jerusalem railway; coastal road; the Burma Road (June 1948) |
| Borders / territory | coloured border on owner edges | Nothing (`owner` missing) | Missing | Yishuv, Arab, Transjordan and Egypt control; British Mandate until 15 May |
| Units | VI: one military plus one civilian; VII: commanders pack armies | `BoardHex.unit` plus `facing`, one per hex, own layer (`HexUnitMotion`) | Have (one slot) | `palmach_squad`, `sandwich_truck`, `willys_jeep` |
| Yields | food, production, gold icons | Nothing on the board | Missing, and should stay in the sim | Citrus exports, housing, morale (`MapActions`) |

**Open contract issues to raise as issues (not to fix in a package):**
- **Unit sizes disagree.** README says 0.26 / 0.15 / 0.48 S. Code (`HexUnitScale`) says 0.30 / 0.22 / 0.50 S.
- **Squad size disagrees.** The `HexScale.FigureMin` comment says "3 to 7 to a unit", but `HexUnitScale.Characters` = 8.
- **Settlements share hexes.** `settlement_hexes.yaml` puts Tel Aviv and Jaffa in one hex (-3,19), and also Lod with
  Ramla and Rishon LeZion with Rehovot. On the real map, a Jaffa prototype can't sit beside the Tel Aviv one. Tal's
  options: a mixed prototype, a board-only exception, or sub-hex quarters.
- **Features have no field.** There is no separate feature or resource field. Today the prototype is the feature.

## 2. Visual rules that make the map feel connected

Civ hexes read as one thing each and still form one landscape. The rules for our renderer, with what exists:

| Rule | How, in our renderer | Status |
|---|---|---|
| **Ground is painted, not placed** | Fields, sand, yards and tracks go into the terrain splat (`ctx.PaintGround/PaintStroke`), not `PlaceFlat` slabs. Slabs flicker when co-planar and fail on slopes. Ground layers blend across edges (weights 0.75 to 0.95 of S) | Have; move remaining fields to paint |
| **Shape follows the hex** | `HexBoardSurface` per-hex profiles blended over 0.5 to 1.12 S: hills round off, coasts soften | Have |
| **Features thin out toward the edge** | Dense in the middle, sparse near the inset (0.07 S), so two forest hexes meet as one forest. A later contract option: a read-only `ctx.Neighbour(dir)` so a tile can spill toward a like neighbour | Prototype habit now; neighbour query = contract change |
| **Roads and rivers are board layers** | Drawn once by the builder as continuous lines (roads through centres, rivers on corners), never by a prototype. Prototypes leave a corridor so a road isn't hidden | In progress |
| **Units are a separate layer** | Never in a tile. `HexScale.UnitSpotShare` 0.3 S keeps tall pieces off the centre. Units are larger than buildings | Have |
| **Faint hex lines** | `HexBoardEdges`: a pale ribbon draped on land edges. Keep it faint; fade it out at close zoom | Have |
| **Territory borders** | A coloured ribbon inset on the owner's side, only where two owners meet. A softer wash fills inside. Drawn like the edges | Missing (needs `owner`) |
| **Resources read as a small icon plus one piece** | A 2D pin above the hex plus one modest model (orange crates, a fishing boat). The icon carries the meaning at map zoom | Missing |
| **One scale, one sun** | `HexScale` shares for every piece. One lighting rig in the captures. No tile brings its own light | Have |
| **Stand-ins must read** | Every tile and unit must read in stand-ins alone. Most machines lack the pending-rights models | Rule |

On rivers, the current code follows Civ VI (on edges). Civ VII's in-tile rivers would fit "one hex, one job" better: a
river hex is its own feature. Edge rivers fit "crossing a river costs movement" better. Tal should pick one. The data
format is the only part that is costly to change later.

## 3. Gameplay mechanics: the owner's rulings, and what the hex tiles must show

Movement and stacking are decided on `main`, in `docs/gdd/military-combat.md` (`Q-MOVE-1` to `Q-MOVE-5`, 15 October
2026). The values are labelled design values in `content/rules.yaml`, set in the implementing PRs (#1071, #1073). The
hex tiles only show the sim's result and never decide a rule. Movement costs are keyed on the content `biome`, never
on the presentation `HexTerrain`.

What the tiles must show:
- **Costs (`Q-MOVE-1`):** integer movement points per unit type against an integer cost per biome. Lakes and the sea
  are impassable. A cost overlay, if wanted, reads the sim's values.
- **Roads (`Q-MOVE-2`):** only a *built corridor road* lowers its hex's cost. Board roads are drawn for every period,
  so the look must tell a built corridor road from an ordinary track.
- **Stacking (`Q-MOVE-3`):** a stacking limit per side per hex; #1073 proposes 3. The units layer must therefore draw
  **up to three units in one hex**. Today one 8-figure wedge fills it, so three smaller formations or offset spots are
  a contract change (`units[]` on the board hex). Entering a hex an enemy unit holds is an attack.
- **No zone of control (`Q-MOVE-4`) and no transport or embarkation (`Q-MOVE-5`).** Nothing in the look should suggest
  either.
- **Territory:** control already comes per period from `content/map/control_hexes_1948_49.yaml`, and the map editor
  (#1009) edits it. Borders on the tiles read that data.

Still open, to bring to the owner as proposals with options and costs (AGENTS.md): river edges (no rule yet), and new
tile or hex types beyond the eight biomes, which the GDD marks TBD.

## 4. Team work split

How packages work: each package is one branch `hex/<id>`, done with the collaborator's own Claude. Each owns its listed
files only.

Everyone's definition of done:
- mid and close captures with local models, **and** a stand-in-only capture;
- `HexPrototypeTests` green;
- the stats line within `TriangleBudget`;
- settings rewrites reverted (`git checkout -- unity/Assets/Settings unity/ProjectSettings/...`).

Asset rules for everyone:
- New Tripo generation is Tal's (credits, rights). Pending-rights `proto:` models stay local and are never committed.
- Any committed model or texture needs a repository-compatible licence and an `ASSETS.yaml` row. Unity Asset Store
  material is never accepted.

**Independent** packages need only new files. **Blocked** packages need WP0 first.

**WP0. Contract v2.** Gating; proposed by Tal in an issue and approved by the owner (eyalgolan, the sole code owner). Size L.
- Goal: one reviewed change to `Core/` and the board format for what the other packages need: per-hex `feature`,
  `resource` and `owner`; a second unit slot; an overlay registry (`[HexOverlay]`, `Overlays/`, one file per overlay,
  like prototypes); a read-only neighbour query.
- Also: settle the rivers choice (edge or in-tile) and the three open issues in section 1.
- Done: tests extended, the Tel Aviv board captures unchanged.

**WP1. Galilee kibbutz.** Independent. Size M.
- Goal: `kibbutz_galilee` with dining hall, water tower, cowshed, fishponds and fields painted with `PaintGround`.
  Optionally a stockade-and-watchtower variant.
- Owns: `Prototypes/KibbutzGalilee.cs`.
- Inputs: 1930s and 1940s kibbutz photos (Hanita, Degania); building kit stand-ins.
- Done: universal list above, on `-on moshava_village`.

**WP2. Jerusalem quarters.** Independent. Size M.
- Goal: `jerusalem_old_city`, the walled Old City as one landmark plus domes; then `jerusalem_stone_quarter`, Rehavia
  stone houses on a hills hex.
- Owns: one file each in `Prototypes/`.
- Inputs: Old City plans, Rehavia photos; stone colours.
- Done: reads on `terrain: hills` (slope limits); landmark ≤ 0.40 S.

**WP3. Coastal cities.** Independent. Size M. Can be two people.
- Goal: `haifa_port` (bay, breakwater, refinery tanks, Carmel slope behind) and `jaffa_old_city` (hilltop old town,
  clock tower, harbour, fishing boats).
- Owns: `Prototypes/HaifaPort.cs` and `Prototypes/JaffaOldCity.cs`.
- Inputs: 1940s aerial photos.
- Done: shore pieces respect `IsWater`; Jaffa tried with `-on tel_aviv_city`; the shared-hex question is left to Tal.

**WP4. Negev desert and Bedouin.** Independent. Size S.
- Goal: `negev_desert` (wadis painted `dirt`, rocks, acacias) and `bedouin_camp` (black tents as stand-ins, camels and
  goats as a tile's life, kept small).
- Owns: one file each.
- Inputs: Beersheba-area photos; `sandbox:desert_rock*`.
- Done: on `ground: desert`, at `BudgetFields` or below.

**WP5. Infantry units.** Independent. Size S each.
- Goal: `haganah_field_corps`, `arab_legion_squad`, `arab_irregulars` (ALA or local fighters), each exactly 8
  characters via `HexUnitTypes.Squad`.
- Owns: one `Units/<Id>.cs` each.
- Inputs: uniform references (Tal's character roster docs); stand-in figures until Tal generates models.
- Done: the unit-move video renders; walk and idle clips named "walk" and "idle"; `CycleShare` ≈ 0.86 × `HeightShare`.

**WP6. Vehicles and guns.** Partly blocked. Size M.
- Goal: the sim's unit types (`content/unit_types.yaml`: `veteran_soldier`, `artillery`, `armoured_car`). `armoured_car`
  (one vehicle) is independent.
- Needs a look decision (Tal proposes, owner approves): `artillery`, a gun plus crew that fits neither "8 characters"
  nor "one vehicle". No convoy unit: the sim has no transport (`Q-MOVE-5`).
- Owns: `Units/<Id>.cs` per unit.
- Done: `YawOffset` checked in the close capture; ≤ `BudgetUnits`.

**WP7. Ground layers.** Blocked on `HexGround.Layers` in Core. Size M.
- Goal: textures for citrus rows, vineyard rows, sand ripples, wadi gravel, riverbed and mud banks, swamp. Made by
  extending `asset-staging/tools/kit/make_crop_textures.py` (local) or from CC0 sources.
- Owns: textures in git-ignored staging, plus an issue listing the layer rows.
- Done: a swatch sheet and a before/after board capture; `ASSETS.yaml` rows ready for when Tal approves the batch.

**WP8. Rivers and roads look.** Blocked on WP0 for railways and borders. Size L.
- Goal: finish the in-progress roads and rivers as overlays: road grades (track, road, the paved main road), railway
  (Jaffa to Jerusalem), bridges, river banks and reeds, then territory borders.
- Owns: `Overlays/<Id>.cs` after WP0.
- Inputs: 1940s Survey of Palestine maps (check licence); the route list from Tal.
- Done: continuous across hexes in mid and board captures; road triangles near zero (painted).

**WP9. Real-map boards.** Independent tooling. Size M.
- Goal: a generator `tools/hex_board_from_map.py` that cuts a region of `content/map/hexes.json` into a board JSON.
  Biome to `terrain` and `ground`: coastal_plain and valley → flat/plains or grass; hills → hills; mountains → hills,
  or mountain above a height Tal sets; desert → flat/desert; sea and lake → water/seabed. It also marks settlement
  hexes from `settlement_hexes.yaml`.
- Owns: the tool, its pytest, and proposed boards in `asset-staging/ui-local/hex-tiles/boards/`.
- Done: pytest green; whole-board captures of the Jerusalem corridor and Haifa bay. Tal copies the chosen ones into
  `Boards/`.

**WP10. LOD and impostors.** Blocked on Core (`HexModels`, builder). Size L.
- Goal: keep a whole-region board interactive: two decimation levels per local model, octahedral impostors for trees at
  board zoom, a per-tile triangle report at three zooms.
- Owns: offline tooling and a measured report, then a WP0-style issue.
- Done: board frame time and triangle totals before and after on Tal's machine.

**WP11. Unit banners and icons.** Art independent; integration blocked. Size S/M.
- Goal: Civ-style unit flags: a faction colour plate, a type icon (infantry, armour, artillery), a strength
  pip; plus resource pins.
- Owns: SVG and PNG icons in `asset-staging/ui-local/hex-tiles/icons/`, and an HTML mockup (not a Unity render).
- Done: legible at board zoom in mockups; original art only; the flag drawn as a billboard over the unit spot (after
  WP0).

**Stays with Tal:**
- board layouts (`Boards/*.json`), which prototypes go on a board, and the routes and rivers lists;
- approval of before/after captures before any wider rollout;
- Tripo generation and credits, rights decisions, `ASSETS.yaml` approval of asset batches;
- every gameplay proposal in section 3;
- push, PR and deployment.

**Must not be touched by a package:**
- `Core/`, `Capture/` and `Tests/` without an approved issue (WP0);
- another contributor's prototype or unit file;
- the simulation (`sim/`) and `content/` rules or data, except through Tal's own sim PR;
- the shipped game, scenes and default look; tracked Unity folders for pending-rights models;
- any committed, shared or uploaded build while pending-rights models exist.

## Sources

- Civ VII Dev Diary #5, Combat (commanders, packing units, ZOC): https://civilization.2k.com/civ-vii/archive/dev-diary/combat/
- Civ VII Dev Diary #3, Managing Your Empire (rural and urban districts, growth): https://civilization.2k.com/civ-vii/game-guide/dev-diary/managing-your-empire/
- Civ VII glossary (biomes, navigable rivers, zone of control): https://civilization.2k.com/civ-vii/glossary/
- Civ VII TSL Earth map (rivers and resources placed to match the real Earth): https://civilization.2k.com/civ-vii/game-guide/gameplay/earth-map/
- Quarter (Civ7), Civilization Wiki: https://civilization.fandom.com/wiki/Quarter_(Civ7)
- Civilization VI: Terrain, CivFanatics (hills and woods cost 2; pre-release river note): https://civfanatics.com/?p=2010
- Firaxis interview, PC Games Hardware (procedural placement for organic cities and roads): https://www.pcgameshardware.de/Civilization-6-Spiel-56986/Specials/Interview-technical-details-1199995/
- From general knowledge, to verify before relying on it: Civ VI's one military plus one civilian per tile rule, Civ VI
  road costs by era, and the exact Civ VI river-crossing cost.
