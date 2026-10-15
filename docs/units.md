# Unit miniatures: the art slice

The owner's target for units (2026-10-11) is the promo: `promo/final/03-gameplay-1936.png` and the demo trailer
(`promo/demo/out/demo-16x9.mp4`, 0:06–0:30). It shows Civ-style miniatures standing on hexes at symbolic scale: a trio of
era-correct riflemen with a selection ring, a mounted rider, and aircraft with Israeli roundels flying low with a shadow.
This page records the slice that matches it, the pipeline choice behind it, the scale, and the sources for every uniform,
aircraft and ship.

The slice is a **showcase only**. It reads nothing from the sim and changes no game state. Normal play draws the units
from the sim's state and sourced map-only data with the same models: see "Units in play" below. Where units move and
fight is war mode's job (#104, with the sim side in #21). The golden hashes do not move.

## How to see it

- **WebGL:** add `units` to the page URL (`index.html?units`). Normal play never shows it, and neither does a plain
  `?shot`: every track's art-checkpoint metrics stay comparable (controller ruling, 2026-10-11).
- **Art checkpoint:** `tools/art_checkpoint.sh --units <name>` adds `&units` to the WebGL screenshots and `-units` to the
  editor renders. With `shot` the aircraft holds a fixed point, so the screenshots repeat.
- **Desktop player:** run with `-units`.
- **Editor:** `MapCapture.Promo` renders it when given `-units`.
- **Aircraft close-ups:** `MapCapture.Aircraft` renders the S-199 and the Spitfire alone, from the top, the bottom, the left
  side and three quarters, into `-captureDir` (default `captures/visual/aircraft`), for checking the markings against the
  photographs. `MapCapture.Promo -units -unitsDate YYYY-MM-DD` draws the units in play as on that day.
- **Frame cost:** `?vprofile&units` prices it as the `unitshowcase` toggle (`median(all) - median(-unitshowcase)`). The
  `units` toggle is the units in play.

Code: `unity/Assets/Scripts/Map/Units/`. The materials are made by `UnitMaterialAssets.Create` in
`unity/Assets/Resources/Units/`. Tests: `UnitShowcaseTests`.

## Owner rulings (2026-10-11)

1. **Units are Civ-style tokens at symbolic scale** (see "Scale"). The 11 October ruling said about 2,000x; the owner's
   decision of 14 October supersedes it: figures at 1.8 times a block, as T29 (1,400x).
2. **Promo-quality close-zoom models come from a separate track:** a cloud-GPU AI-3D pipeline (TRELLIS, MIT). The
   code-built miniatures here stay as the far-zoom and fallback version.

**The art hook.** An authored model replaces the code-built one for its unit type when a prefab exists at
`unity/Assets/Resources/Units/Authored/<unit type>`. The unit types are the `*Type` constants in `UnitShowcase`, for
example `rifleman-1948-steel-helmet` and `spitfire-mk9e-101sqn-1948`.

An authored model follows the code-built convention:
- real metres, facing +Z;
- origin on the ground (an aircraft: its centre of gravity);
- its own materials;
- an authored aircraft brings its own propeller.

The showcase places and scales it as it does the code-built model. That is the whole hook: there is no registry beyond
it, and there are no level-of-detail switches yet.

## What is in the slice

| Piece | Model | Where (PromoPose.Gameplay03) |
|---|---|---|
| Rifle trio | three 1948 IDF riflemen: bush hat with rifle held ready, Mk II steel helmet with rifle grounded, Palmach stocking cap with rifle slung | on the centre of the hex nearest 34.88 E, 32.01 N (east of Tel Aviv, clear of the settlement labels), inside a white selection ring that fits within the hex |
| Aircraft | Supermarine Spitfire LF Mk IXe of 101 Squadron, from October 1948: clipped wing, red spinner and diagonal red-and-white rudder stripes, filled Star of David roundels with no outer ring (Efrati pp. 12 and 14) | a 16 km loop over Tel Aviv and the coast, 4.2 km above the highest ground under it, banked 28 degrees, its shadow cast on the ground by the sun |
| Navy | modified Flower-class corvette INS Haganah (K-20), 1948, 63.4 m | off Jaffa (34.63 E, 32.02 N) |

Not in the slice:
- **The mounted rider.** No 1948 IDF mounted unit was sourced for this slice. The promo's rider belongs to 1936, so it
  waits for the Yishuv era (#49) and a source.
- **Civilian boats.** The owner ruled them out of scope; navy ships are units.
  - 2026-10-14: the owner reversed this for civilian boats ("Keep civilian boats"); they are drawn as map art in #536.
    Navy ships are still units.

## Units in play

Normal play draws one token per unit from two inputs: the sim's state and `content/map/units_1948.yaml`.
- **The state** gives the date, the security level and the reserve call-up (`GameState.Date`, `Rung`, `Mobilization`).
  Nothing else in the sim describes the military: it has no formations and no positions (design:
  `docs/superpowers/specs/2026-10-11-units-in-play-design.md`).
- **The data file** is map-only. `ContentLoader` never reads it, so the content hash and the goldens do not move
  (`UnitsInPlayTests.TheUnitFileDoesNotMoveTheContentHash`). The WebGL manifest ships it.

Code:
- `UnitData`: the file.
- `UnitRoster`: which units show now.
- `UnitPlacer` and `UnitGround`: each anchor's spot.
- `UnitLayer`: the tokens and the ring.
- `UnitBadge`: the badge.
- `UnitCard` and `InfoPanelPresenter`: the card.

Tests: `UnitsInPlayTests` (EditMode) and `tools/tests/test_units_1948.py` (the data rules, in CI's Python job).

### Rulings (2026-10-11)

The owner ruled:
1. One token per brigade.
2. The number of figures follows the call-up: 3 at or above the security-level-4 floor (`rung_min_mobilization`, 0.4),
   2 at or above the level-3 floor (0.15), else 1.
3. 101 Squadron shows an Avia S-199 until the Spitfire's sourced date, then the Spitfire.
4. The four brigades with no sourced position (Carmeli, Alexandroni, the 8th, Oded) are listed in the file and hidden.
   - 2026-10-15 (refs #385): Carmeli (from 9 July 1948; coordinator's option (d)) and Alexandroni now have sourced
     positions (below) and are shown. The 8th
     is sourced at Tel Litvinsky (24 May to 9 July 1948; owner, 2026-10-15: "Accept, verify later") but stays hidden:
     the map has no clear spot for it on its own side within one hex of the camp. Oded stays hidden (its sources
     conflict and name no place).

The controller adopted the design's defaults:
- tokens stand only at dated, sourced positions and do not move with operation events;
- after the armistices, only what a source says continued is shown;
- there are no orders: the card links to the Defence screen.

After the staff review the controller ruled:
- **No invented end dates (C1).** A window ends at the earliest of its sourced end, the unit's sourced disbandment, and
  the armistice of the front it stood on. Each anchor says which: `end_basis: source` with an `end_note`, or
  `end_basis: front_armistice` with its `front`. The armistices were signed with Egypt on 24 February 1949, Lebanon on
  23 March, Jordan on 3 April and Syria on 20 July (1949 Armistice Agreements). No source yet gives a position after
  the armistices, so no unit shows after them.
- **"Since" only from a sourced start (I1).** A window that opens before the game shows from 15 May 1948. The card says
  "since February 1948" from the anchor's sourced `since`, and gives no start where no source dates the arrival
  (Givati at Gedera, the ships).
- **Within one hex (I2).** See "Placement".
- **The squadron's bases where two sources agree (I3), and the Spitfire from its first sourced service (I4).** See the
  table.
- **Plain checkpoints stay comparable (I5).** A screenshot run (`?shot`, `-shot`) switches the units off unless it also
  asks for them (`&units`, `-units`). The editor's promo renders draw them only with `-units`.
- **The promo's badge, a wider trio, sourced headgear; the ring stays selection-only (I6).** See "Look".

### What the player sees

| Unit | Positions (dated, from the sources) | Ends |
|---|---|---|
| Golani (1st) | Yavne'el (since February 1948); the Julis camp from December 1948 | December 1948 (source); 24 Feb 1949 (Egypt) |
| Carmeli (2nd) | Facing the Syrian bridgehead at Mishmar HaYarden, on the Rosh Pinna side, 9-18 July 1948 (Operation Brosh). Not drawn at Haifa, its sector's seat: no clear spot outside the town (#588) | 19 Jul 1948, the second truce (source) |
| Alexandroni (3rd) | Netanya, its headquarters at Camp Dora (since March 1948) | November 1948, the move to the Faluja pocket (source) |
| Kiryati (4th) | Tel Aviv (since February 1948) | 3 Apr 1949 (Jordan) |
| Givati (5th) | Gedera, one of the three places its sector names (no start given) | November 1948, the move to the centre (source) |
| Etzioni (6th) | Jerusalem (since late 1947) | 3 Apr 1949 (Jordan) |
| 7th Brigade | Hulda, its headquarters for the Latrun attacks, 24–31 May 1948; Ein Shemer, June 1948 | sources |
| Harel (10th) | Sha'ar HaGai, one place picked from the Jerusalem corridor (since the first half of May 1948) | 22 Dec 1948, Operation Horev (source) |
| Yiftach (11th) | Lydda, 11–19 July 1948 (Operation Dani; the IDF entered Lydda on 11 July) | 20 Jul 1948 (source); disbanded May 1949 |
| Negev (12th) | the Mekorot camp near Nir Am (since March 1948) | 24 Feb 1949 (Egypt) |
| 101 Squadron | Ekron, 20–29 May 1948; the Herzliya strip from mid-June; Hatzor from 21 November 1948 | June 1949, the move to Ramat David (source) |
| INS Haganah, INS Wedgwood | Haifa harbour (commissioned 1948; no start given) | 24 Feb 1949 (Egypt: their sourced actions were against the Egyptian forces) |

**101 Squadron's bases.** The map follows the dates two sources agree on:
- en.wiki and he.wiki agree it was at Ekron from 20 to 30 May, and at Herzliya from mid-June.
- Between 30 May and mid-June they disagree. en.wiki says Herzliya after the strafing of 30 May; he.wiki says Ekron,
  with Modi Alon taking off from there on 3 June. So the map shows no base in that gap.
- Hatzor from 21 November 1948: Eduard p. 54 and he.wiki ("November"). en.wiki and Eduard p. 53's profile caption say
  October. he.wiki has Alon killed landing at Herzliya on 16 October, which fits November.

**The aircraft.** The S-199 until 15 October 1948, then the Spitfire.
- The first Spitfires flew in, unarmed, on 27 September 1948 (Operation Velvetta, he.wiki).
- The first sourced Spitfire sortie is 15 October: three Spitfires escorting Beaufighters to El Arish (he.wiki
  Supermarine Spitfire). The squadron flew both types (he.wiki 101 Squadron), and the S-199 flew on sporadically until
  mid-December (Avia S-199; Eduard p. 54). The ruling allows showing the S-199 until the switch and the Spitfire after.
- en.wiki's Spitfire operational history calls a 101 Squadron claim of 21 October the IAF's "first ever aerial
  victory". That is wrong: the first victories were on 3 June 1948, by Modi Alon in an S-199. The file records this.

**Placement** (`UnitPlacer.PlaceAll`). Every anchor is placed once, from the data alone, so a token never moves because
another unit appears. Its spot is at most one hex step from the sourced place (about 8.7 km between hex centres on 5 km
hexes). It is the nearest such spot to the place, or to the anchor's sourced `toward` point when it has one (see below).
The candidates are tried in three rounds:
1. The centres of the anchor's own hex and its six neighbours, with the token at full size.
2. If none is clear, those centres plus six points inside each hex, with the token **compact**: half the ring and half
   the trio's spread, to fit beside a town. Yiftach at Lydda and the squadron at Herzliya stand compact.
3. If the whole neighbourhood is town, the compact point that reaches least deep into it (then the nearest; with a
   `toward` point, the nearest to that). The map then keeps a clearing of the compact ring's radius free of the kits'
   buildings and trees there (`MapScene.UnitClearings`, computed before the kits from the same data), so the token never
   stands in a building. The clearing may cut only into the anchor's own town, never a neighbour's. Kiryati (Tel Aviv)
   and Etzioni (West Jerusalem) stand this way.

A spot is clear when:
- the ring's disc stays off every settlement footprint (the kits' discs, times 1.1);
- an army token's three figure spots (always three, so the spot does not change with the call-up), or a parked
  aircraft's centre, are on land and at least 700 m beyond every railway and road corridor;
- a ship has a sea hex;
- no other anchor whose window overlaps holds the hex.

**Own side.** The map holds no 1948 front line yet, and the one-hex limit alone does not keep a token on its own side:
the nearest clear spot can be the other side's ground. So an anchor where that happens has a sourced `toward`, a place on its own side, and where needed a `place_hex`, the hex
next to the anchor on that side. The token stands at the clear spot nearest that place, in that hex when one is given:
- Etzioni stands in West Jerusalem, about 2 km west of the place the West Jerusalem article gives, not on the Hizma and
  Anata side, which Jordan held. It is in town, with a clearing. In every round the placer also tries rings round a
  `toward` point, every 500 m out to 2.5 km, so a spot can sit between a town's roads near the sourced place.
- The 7th Brigade is anchored at Hulda, its headquarters for the Latrun attacks (Battles of Latrun), not at Latrun.
  The Arab Legion held Latrun and the Yalu–Bayt Nuba ridge. The ground between was contested until the Bayt Jiz and
  Bayt Susin area was cleared on 28 May, and Sar'a was taken only on 13–14 July.
- Harel stands by Saris on the corridor road, hex (-2,23), taken on 16–17 April 1948. It no longer stands by Sar'a and
  Artuf.

The test pins Etzioni's distance from the West Jerusalem point, Harel's hex, and the 7th within 2.6 km of Hulda; both
the 7th and Harel stay more than 3 km from Sar'a. Once dated front data lands, the placer should also reject ground the other side held. `EveryAnchorStandsWithinOneHexClearOfTheTownsAndTheRoutes`
checks every anchor and logs each spot and its distance.

**Look.**
- **Models:** the slice's models and the authored-prefab hook.
- **Army:** a token is 1 to 3 riflemen, spread across the ring as in promo 03, about 2 km apart, so each figure reads on
  its own.
- **Headgear:** only the Negev Brigade's is tied to it by a source: knitted stocking caps and Mk II steel helmets, from
  its two 1948 photographs. Every other brigade has the uniform default (bush hat, steel helmet, bush hat), from the 1948
  photographs listed under "Sources".
- **Air:** the squadron flies the slice's loop over its base at security level 4 or 5 and stands parked below that.
- **Navy:** a ship is the K-20 corvette model; Wedgwood was the same modified Flower class.
- **Badge:** each token has the promo's badge (`UnitBadge`): a dark rounded plate with an ivory glyph (two figures, an
  aircraft or a ship). It is turned to face each camera as it renders.
- **Ring:** the white ring appears only on selection.
- **Base and flag (demo2 T29, refs #197).** An army token's figures stand shoulder to shoulder on one round earth base
  ringed by a bright white rim (`UnitPlinth`), and the side's flag flies on a pole at the base's back edge (`UnitFlags`).
  See "The base and the flag".
- **Slopes (owner question, 2026-10-11; round 3 of #559).** The base is draped on the ground: its top is a polar grid
  of 32 rings and 96 segments whose every vertex stands the lip (`UnitPlinth.LipShare`, a twentieth of its radius, about
  100 m) over the ground at its point, and an edge lip of that height closes it to the ground. So on a hill it neither
  stands up as a mesa nor leaves a gap downhill. A figure stands on the base's top over the highest ground under its boots
  (`UnitPlinth.FigureY`).
  - **Tolerance:** between its vertices the top strays from the lip by at most half the lip (about 47 m): it never dips into
    the ground and never floats more than one and a half lips over it. Measured on every hill token: Harel 45.5 m, the
    7th Brigade 41.1 m, the rest under 14 m. The level discs before stood up to 1,081 m (round 1, on the highest ground)
    or 707 m (round 2, on the median) over Harel's valley side.
  - **Tests:** `ABaseIsDrapedOnASteepCurvedSlope`, `TheBaseFollowsTheGroundOnEveryHillToken` and
    `NoRiflemanSinksIntoTheHillsOfTheMap`.
- **Trees (#249).** The kit trees whose crowns reach a drawn token's base are left out at draw time (`VegetationLayer.Hide`), so no orchard grows through a figure. Only the figures drawn
  that day clear trees, and they come back when the figure goes. With the `units` toggle off, as in a plain `?shot` run,
  every tree is drawn. The planting, and so the kits' layout, never changes. Tests:
  `NoKitTreeGrowsThroughAShownFiguresFeet` and `OnlyTheShownFiguresTreesGoAndTheToggleBringsThemBack`.

**Selection and the card.**
- Clicking a token's hex selects it. The ring goes round it and the hex outline is cleared, so it has one highlight.
- A second click shows the hex and its map actions. A hidden layer (the `units` toggle off) selects nothing.
- The card gives the name, the number and the service, where the unit stands (with "since" only from a sourced start),
  its formation and first commander where sourced, and the sector in the source's words.
- It also gives the national values the token reflects ("Reserve call-up 80% · security level 5 of 5") and the source
  hosts, with a draft note that says "Wikipedia only" when the position's own sources are.
- It has no orders; a button opens the Defence screen.

**The Avia S-199** (`UnitModels.AviaS199`; type `avia-s199-101sqn-1948`):
- The Bf 109G airframe, 8.94 m long with a 9.92 m span, with the Jumo 211F's wide three-blade paddle propeller and the
  MG 151 pods under the wings (Avia S-199 article).
- One khaki green on all surfaces, about RAL 6013: Efrati pp. 8 and 11, confirmed from an Avia fuel cap. Eduard Info
  11/2025, pp. 53–54, also has one green.
- The identification band round the rear fuselage is white, blue, white (Efrati pp. 8 and 11).
- Filled Star of David roundels sit on both fuselage sides behind the cockpit and under the wings (Efrati pp. 6, 8 and 11,
  and the 1948 photographs). None sit on top of the wings: over-wing roundels were "standard" on no IAF aircraft from May
  1948 to January 1949 (p. 6) and were adopted on the Avias only after the Velvetta Spitfires arrived (pp. 11 and 14).
- The red spinner, the rudder stripes and the upper-wing roundels of Operation Ten Plagues (from 15 October 1948, Eduard
  p. 53) are not drawn, because the map shows the Spitfire from that day.
- The squadron's skull badge on the nose is too small to read at map scale and is left out.

**Cost.** The budget is 2.5 ms or less for the `units` toggle at gameplay03 (docs/visual-v1.md). The measurement was
headless `?vprofile` on the WebGL build of 943184c, 1920x1080 at DPR 1, 2026-10-11. It ran under the main checkout's
`machine_lock.py --exclusive --wait-unity`, so no Unity job ran beside it.

| Pose | all | -units | units cost | none |
|---|---|---|---|---|
| gameplay03 | 14.74 ms (spread 0.48, p95 17) | 14.14 ms (spread 0.71, p95 15) | 0.60 ms | 11.09 ms |
| diorama02 | 17.66 ms (spread 0.53, p95 20) | 17.39 ms (spread 0.17, p95 20) | 0.27 ms | 13.18 ms |

- The units cost well under the 2.5 ms budget at both poses. The new-game state draws 6 brigades (up to 18 riflemen,
  with their badges) and 2 ships.
- The whole scene's p95 is over 16.7 ms at gameplay03 (17 ms), and at diorama02 even without units (20 ms). The base
  frame here (`none` 11.09 ms) is slower than in earlier runs. That is a scene-wide matter for the perf track, not
  this layer's.
- The layer rebuilds only when the roster changes (a new turn, a call-up or security order). Between those, its only
  per-frame work is a flying aircraft's transform and turning about ten badges to the camera.

**Left open.**
- The rows are drafts from Wikipedia (and Eduard and Commons) until a historian checks them against Lorch or the
  archives.
- No dated front line: tokens keep within one hex of their sourced place, and three anchors carry a sourced `toward`
  point to stay on their own side (review MF1).
- **Kit baseline break:** from 943184c the kits keep the unit clearings in Tel Aviv and West Jerusalem, in plain
  checkpoints too. So kit metrics before and after that commit are not comparable; this is marked in the art log.
- Merges still to come (review I7). With promo-borderdata, the info panel's region label must be cleared when a unit
  card shows. With promo-screens, the wide-screen click guard must cover `ShowUnit` and `UnitLayer.Select` as well as
  `Show`, and the info panel must keep its `info-defence` button. Both branches add optional `InfoPanelPresenter`
  parameters, so the call in `GameRoot` should name its arguments.
- Wedgwood's Israeli name and pennant number conflict between its two articles. The card shows "INS Wedgwood" and no
  number.
- No source yet places any unit after the armistices.
- The Spitfire's red spinner and diagonal rudder stripes date from early October 1948 on the Spitfires themselves
  (Efrati p. 12); IsraDecal attests them for January 1949.
- In the historical playthrough the call-up stays at or above 0.4 until the armistices, so the tokens keep three
  figures.

## Pipeline choice: code-built miniatures

Three pipelines were tried on a development Mac (Apple M5 Pro, 48 GB) on 2026-10-11. Each was placed on the map at the
same scale and rendered at the gameplay zoom and close up, next to the promo's trio; the table summarises what each
produced. The renders are not in the repository.

| Pipeline | Result | Licence |
|---|---|---|
| (a) Image-to-3D, run locally: TripoSR (MIT code and weights), CPU, about 3.5 s per mesh | Inputs were a promo crop and a 1948 photograph. Out came a blurry blob of about 45,700 triangles with baked vertex colours: the head and rifle were lost to the background cut-out, the side view was a flat card for the rider, and there was no rig. It would need clean, sharp turnaround references, which we cannot make locally, plus manual retopology. | MIT; fine. |
| (a) Hunyuan3D-2, TRELLIS | Not run locally. Hunyuan3D-2's community licence excludes the EU, the UK and South Korea, which is not compatible with an open project. TRELLIS needs an NVIDIA GPU with CUDA; the owner chose it for the cloud-GPU close-zoom track (rulings above). | Hunyuan3D-2: not compatible. TRELLIS: MIT. |
| (b) CC0 base meshes: Quaternius Universal Base Characters (free "Standard" kit, CC0) | A clean, rigged body, but a stylised "superhero" figure in underwear, in a T-pose. Every uniform, hat, belt and rifle would have to be modelled by hand in Blender. Kenney's characters are blocky, low-poly figures, which the spec rules out (§6, "no low-poly packs mixed in"). | CC0; fine. |
| **(c) Code-built from primitives (chosen)** | Lathed limbs and torsos, ellipsoids, boxes and plates, painted from one palette texture with a dark wash at the feet and a dry-brushed highlight on top. One URP Lit material, made at runtime, with no texture file. About 3,600 triangles per figure. Real silhouettes for each headgear and rifle pose. The figures cast and receive shadows. | Our own code, MIT. |

**Why (c):** at the gameplay zoom a figure is about 90 px tall. What reads at that size is the silhouette (hat brim,
rifle, stance), the colour blocks (khaki shirt, dark trousers, pale webbing) and the shadow. In the gameplay03
checkpoint the code-built trio matches the promo's trio in size (87 px) and reads with all three. They are exact to the
sources: each part is a sourced item, and a change is a reviewed line of code. Era variants (1956 helmets, 1967 Uzis, 1973 flak vests) are parameters. Builds are deterministic, there is no
licence question, and the build-size cost is code only.

**Their limit:** close up (the 6 km hero zoom) they look like wooden mannequins, with faceted limbs and no faces or
folds. The promo's close shots (0:21) show sculpted miniatures. The way past that limit is the owner's close-zoom track,
which plugs authored models in through the art hook above.

## Scale

Measured on the promo scaled to 1920 px wide: a rifleman is about 87 px tall, inside a ring about 190 px across. Our
gameplay03 pose frames the same hex count as the promo (about 9 across: see `PromoPose`). The first checkpoint, at 2600x,
drew our riflemen 113 px tall, so the first round used 2000x. The ring is at 0.66 of a hex's circumradius. The aircraft
(x420, a 4.7 km span) and the ship (x95, 6.0 km) are sized against the figures, as Civ sizes them, not against each other.

**Owner ruling (2026-10-11), superseded on 14 October (below): units stay Civ-style tokens at about 2,000x.** This is far above the 10–25x ruling for
buildings and trees. At 10–25x a man is 18–45 m tall, which is under one pixel at 60 km. The promo, which wins over strict
scale by the owner's ruling, draws units as tokens. The scale is one constant.

**Owner decision (2026-10-14): figures at 1.8 times a block, as T29; it supersedes the 11 October ~2,000x ruling. 1,400x (refs #197).** T29's figures stand about 1.8 times a house. At 2000x ours
stood about 2.5 times the White City blocks beside the showcase trio at close30 (the T54 audit's measure). The block is the
kit's seven-storey heart block, 7 x 3.3 m at the city blocks' 60x, about 1.4 km; on the close30 render it measures about
1.3 km against the figure. So `UnitShowcase.FigureScale = 1400`: a man about 2.5 km tall, 1.8 times that block
(`UnitShowcase.HouseRatio`, pinned to 1.6–2.0 by `TheFiguresStandAbout1Point8TimesAHouse`). A one- or two-storey village
house (100x) is 0.33–0.66 km, so against those the figures still stand about 4–7 times as tall. The token's ring and its
spot are unchanged, so no unit moved; the figures are drawn closer together (below), inside the ground the placer checked.

## The base and the flag

Demo2 T29 (owner's reference) stands every unit on a round earth base ringed in bright white and flies the side's flag on
a pole. Measured on 4x crops of three of its tokens (the 1:12 frame): the base (the ring's outer edge) is 1.41, 1.62 and
1.71 times the trio's width; the trio is about 1.15 figure heights wide (shoulder to shoulder); the ring is about 8% of the
base's radius wide; the flag's cloth is 0.47–0.60 of a figure's height tall and 0.65–0.70 wide, on a pole about 1.5
figures tall standing at the base's back edge. `UnitPlinth` and `UnitFlags` draw the same from code:
- **Figures:** drawn at `UnitShowcase.DrawSpread` (0.19 of a hex's circumradius between neighbours), shoulder to
  shoulder, the trio about 1.15 figure heights wide. The placer still checks a spot at `TrioSpread`, so no spot moved.
  The showcase trio stands the same way.
- **Base:** one draped earth disc per army token (see "Slopes"), its ring 1.45 times the full trio's width across
  (`UnitPlinth.BaseToTrio`; 1.42–1.44 measured on the built tokens, pinned to 1.3–1.7 by `TheBaseHugsTheTrioAsInT29`).
  The earth is the palette's `Earth`, dark and warm as T29's, mottled by smooth value noise over the world in its shade
  rows (no texture).
- **Ring:** a band on the ground just outside the base, from 1.02 to 1.10 of its radius, each vertex over the highest ground
  across the band (the selection ring's rule), drawn unlit in white with the selection ring's shader. The selection ring
  is unchanged and still appears only on selection.
- **On renders** (`TheBaseRingAndFlagReadAsT29OnARender`: a 30 km look at Harel's hill token and at the showcase trio,
  through the game's camera, measured as T29 was): see the numbers in PR #559. The bands are T29's own measures, set
  before measuring ours.
- **Flag:** by the unit's `side` in `units_1948.yaml` (`israel` on every row today; the Arab states' sides are known to the
  loader for #226). The cloth is 0.52 of a figure's height tall and 0.72 wide, on a pole 1.5 tall at 0.9 of the base's
  radius, behind the trio and east of the badge (`TheFlagIsAboutHalfAFigureTallOnTheBasesEdge`). The cloth is drawn
  unlit (white, and the proclamation's dark sky-blue), so it reads white under the map's light and grade as T29's. Only a side whose 1948
  flag is sourced here gets one, so no flag is invented:
  - Israel: the Provisional Council of State's "Proclamation on the Flag of the State of Israel" of 25 Tishrei 5709
    (28 October 1948), Iton Rishmi No. 32, 10 Heshvan 5709 (12 November 1948), p. 62
    (https://fs.knesset.gov.il/0/law/0_lsr_340940.pdf, read 2026-10-14): 220 cm by 160 cm; white, with two dark
    sky-blue stripes 25 cm broad along its length, 15 cm from the top and the bottom edge; in the middle, equally far from
    both stripes, a Star of David of six sky-blue stripes 5.5 cm broad forming two equilateral triangles whose bases are
    parallel to the stripes. The Flag and Emblem Law, 5709-1949 (Sefer HaChukim No. 8, 24 May 1949, p. 37, s. 1(a);
    https://fs.knesset.gov.il/1/law/1_lsr_208225.pdf, read 2026-10-14) makes that proclaimed flag the State flag "in
    any size". Neither text sets the star's size: it is drawn 69 cm across, as the construction drawing at
    https://en.wikipedia.org/wiki/Flag_of_Israel (checked 2026-10-14), which fits the gazette's own drawing. The same
    design hung at the declaration on 14 May 1948 (https://en.wikipedia.org/wiki/Israeli_Declaration_of_Independence,
    checked 2026-10-14), so it flies from the game's first day. Pinned by `TheIsraeliFlagFollowsTheSpecification`.
  - Egypt, Jordan, Syria, Iraq, Lebanon: no sourced flag yet, so a token of theirs flies none
    (`TheFlagIsTheSidesAndOnlyASourcedFlagIsDrawn`).
- **The showcase trio** (`?units`, `-units`) stands at the land hex nearest its old spot whose base keeps off the
  railways and roads, and the kits keep a clearing there in such a run, as they do for a unit in a town
  (`MapScene.ShowcaseClearing`, `UnitShowcase.Clearing`), so no kit building or route crosses its base
  (`NoKitBuildingOrRailCrossesTheShowcasePlinth`). A plain screenshot keeps every kit.
- **Aircraft and ships** have no base and no flag: they fly or float, and carry their own sourced markings.
- **Cost:** per army token, a draped top (6,048 triangles), its lip (192) and its ring (384), built once per roster change;
  one shared flag mesh (about 250). The base, lip and pole in the units' palette material; the ring and the cloth in three
  unlit colours. No texture, no new shader, no per-pixel cost (docs/visual-v1.md, #391), and no per-frame code.

## Colour at map scale

The uniform's colours (shirt, faded shirt, trousers, bush hat in `UnitPalette`) are darker and browner than the khaki
drill of the photographs. This is a readability choice at map scale, not a new historical claim: the 1948 photographs
are black and white, and the kit itself (khaki drill, 37-pattern webbing, the three headgear) is unchanged and sourced
below. Measured on editor renders of both tiers (gameplay03, the showcase trio at 20 km, the 7th Brigade at 15 km,
Golani at 12 km, Harel at 15 km in the Judean hills and Etzioni at 30 km), a figure was 0.68–0.82 of the surrounding
ground's luminance with the old, lighter khaki, and blended into it; it is now 0.50–0.57, and warm (red above green).
Promo 03's figures are about 0.45 (figures round 1, refs #197). The webbing stays pale, as the promo's straps do. The
token's base is painted earth (`Earth`), lighter than the uniforms (it replaced round 1's per-figure slope base). The aircraft and ship colours are untouched.

## Sources (checked 2026-10-11)

Uniform and equipment, 1948:
- Palmach (Negev Brigade) soldiers briefed before an operation, 1948: khaki shirts and trousers, web belts with pouches,
  knitted stocking caps, rifles. Public domain.
  https://commons.wikimedia.org/wiki/File:Palmach-negev-instructed2operation.jpg
- Negev Brigade soldiers, 20 October 1948: Mk II steel helmets, khaki drill, field gun. Public domain.
  https://commons.wikimedia.org/wiki/File:Negev_Brigade_soldiers_1948.jpg
- Beersheba, 21 October 1948: brimmed bush hats, khaki shirts and shorts, belts. CC BY 4.0 (National Library of Israel).
  https://commons.wikimedia.org/wiki/File:1948_Arab-Israeli_War_997008137085905171.jpg
- A woman fighter in Jerusalem, about March 1948 (photograph by Yehuda Aizenshtark): khaki shirt and trousers, web
  belt, a grenade, and a Lee-Enfield No. 4 rifle (per the Commons category). CC BY 2.5 (PikiWiki).
  https://commons.wikimedia.org/wiki/File:PikiWiki_Israel_38816_Israel_Defense_Forces.jpg

Aircraft:
- Israel bought 59 Spitfire Mk IXs from Czechoslovakia in 1948–49; IAF Spitfires fought in the 1948 war (the last air
  combat was on 7 January 1949). https://en.wikipedia.org/wiki/Supermarine_Spitfire_operational_history
- Ferry flights of the Czech Spitfires to Israel (Operation Velvetta, 1948). https://en.wikipedia.org/wiki/Operation_Velvetta
- The Avia S-199 flew first, on 29 May 1948 (Operation Pleshet). https://en.wikipedia.org/wiki/Avia_S-199
  The Spitfire was chosen over it because the promo shows a Spitfire (promo beats era, and both are 1948-correct).
- Spitfire Mk IX shape (a four-blade propeller): the IAF Museum airframe at Hatzerim. CC BY 2.5.
  https://commons.wikimedia.org/wiki/File:Spitfire-MkIX-hatzerim-1-1.jpg
  **Clipped wing (corrected 2026-10-12):** "All Spitfires ferry flown to Israel during the 1948 war were clipped wing
  Mk.IXe versions" (Efrati p. 14). The clipped tips cut the span by 3 ft 6 in
  (https://en.wikipedia.org/wiki/Supermarine_Spitfire). The model now has the clipped wing, not the promo's full
  elliptical one.
- Variant **LF.IXe**, and 101 Squadron's **red spinner** and **red-and-white striped rudder**. On the Spitfires these are
  attested for **January 1949** (IsraDecal), and from early October 1948 by Efrati (below).
  - IsraDecal Studio, "Spitfire Mk.IX, Israeli Air Force" (IAF-123, 1:24) marking instructions, May 2023. It lists
    "Spitfire LF.IXe 2011/26, 101 Squadron, January 1949", painted in RAF colours, with a red spinner and the rudder
    painted "in White. Paint Red stripes".
    https://www.isradecal.com/_files/ugd/ddb4b4_42f20e2bc83f4ba895c816e97ad03151.pdf
  - Eduard Info 11/2025: "In preparation for Operation Ten Plagues (October 15-22, 1948), later renamed Operation Yoav,
    101 Tayeset fighters obtained high visibility markings consisting of red spinners and red and white stripes on the
    rudders". https://info.eduard.com/en/11-2025-1/strana-53
    The page shows the squadron's Avia S-199s ("Messers"), not its Spitfires. So it dates the squadron's markings to
    October 1948. On the Spitfires, Efrati p. 12 confirms that date (below).
  - A Fundekals decal review also says 101 Squadron "had the white and red stripes on the rudder".
    https://mail.modelingmadness.com/scott/decals/fundekals/fd48idfspit.htm
  - **Diagonal (corrected 2026-10-12):** from early October 1948 the squadron painted "their spinners red and rudders
    with diagonal red and white stripes" (Efrati p. 12, with the photographs of Spitfires 14 and 17). This also
    dates the Spitfires' markings to October 1948.
- Roundel of 1948–49: a **filled** blue Star of David on a white disc, the star clear of the disc's edge, **with no outer
  blue ring**.
  - **Filled, not an outline** (checked 2026-10-12, after the owner asked).
    - Efrati, p. 6, on the Bonanzas after 15 May 1948: "Ground visibility of the open star was poor so he [Boris Senior]
      suggested that the Star of David be filled resulting in the IAF insignia as applied on IAF aircraft ever since."
    - Every 1948–49 photograph checked shows the star filled: the S-199s D-120 and D-123 and the Spitfires 14 and 17
      (Efrati pp. 11–12), and the public-domain S-199 photographs on Commons (below).
    - An open, outline star was used only briefly before that change. The model draws the filled star, and
      `UnitsInPlayTests.TheRoundelIsAFilledStarOfDavidOnAWhiteDiscWithNoRing` pins it.
  - Where the roundels sit:
    - Spitfire: both fuselage sides and both wing surfaces. The Velvetta Spitfires arrived with over-wing roundels
      painted over their Czechoslovak ones (Efrati p. 14).
    - S-199: the fuselage sides and under the wings (see "The Avia S-199" above).
    - On the fuselage a point is straight up, as in the photographs.
  - Flags of the World, citing a 2002 survey of *Born in Battle*: "almost all the roundels show the star within, but not
    reaching the edges of, the white disk ... This includes early planes from the 1948/49 war of independence (like
    Spitfire ...)". It adds: "the only aircraft showing a roundel with a thin blue ring around are Mosquitos and Meteors
    from the 1950ies". https://crwflags.com/fotw/flags/il%5Eairfo.html
  - The Commons "early variant" SVG with the ring is drawn from a Mosquito photograph, a 1950s type, so it is not used.
- The camouflage, RAF Ocean Grey and Dark Green over Medium Sea Grey, is now **confirmed** for "all Spitfires ferry flown
  to Israel during the 1948 war" (Efrati p. 14; it was a single hobby source before). Alex Yofe's *Spitfire Mk. IX in
  Israeli Air Force Service 1948–1956* has still not been checked.
- Yoav Efrati, *Colors & Markings of the Israeli Air Force* (IAF, 2005), http://files.iaflibrary.org.il/DigitalLibrary/English/ColorsMarkings.pdf: the IAF's own library publishes this reference, which these marking corrections rest on (pages are the
  book's own numbers).

Navy:
- INS Haganah (K-20), ex-HMCS Norsyd, a modified Flower-class corvette, 208 ft (63.4 m) overall, with an upright
  funnel. https://en.wikipedia.org/wiki/HMCS_Norsyd
  - In 1946 it was sold to the Mossad for Aliyah Bet as the *Balboa*, renamed *Haganah*, and carried Holocaust survivors
    towards Mandate Palestine.
  - The Israeli Navy took it over and rearmed it in 1948.
- Photograph of K-20 in Haifa harbour, 1948: light grey, mast at the bridge, funnel aft of it. Public domain.
  https://commons.wikimedia.org/wiki/File:Hagana_K-20_ship_Israel.jpg
- Photograph of INS Haganah, about 1949. Public domain. https://commons.wikimedia.org/wiki/File:INSHaganah.jpg

Avia S-199 (units in play):
- Type, dimensions, the Jumo 211F with its paddle-bladed propeller, the underwing MG 151 kit; the first four arrived on
  20 May 1948 and flew first on 29 May; mostly withheld by the end of October 1948.
  https://en.wikipedia.org/wiki/Avia_S-199
- Eduard Info 11/2025, p. 53: 101 Squadron at Herzliya (July–August 1948) and Hatzor (October 1948); D-123 in "the same
  shade of green"; for Operation Ten Plagues (15–22 October 1948) red spinners, red and white rudder stripes, and Star
  of David roundels "applied atop the wings for the first time". https://info.eduard.com/en/11-2025-1/strana-53
- Eduard Info 11/2025, p. 54: Avia-built aircraft "bore the standard MNO Smalt Avion 2036.02 paint scheme on all
  surfaces"; 101 Squadron moved to Qastina (Hatzor) on 21 November 1948. https://info.eduard.com/en/11-2025-1/strana-54
- Photographs of 101 Squadron S-199s in 1948 (public domain): the white rear-fuselage bands, the roundels on the lower
  wing surfaces, the paddle propeller.
  https://commons.wikimedia.org/wiki/File:Avia-S-199-IAF-101Sqn-Tel-Nof-Israel-1948-01.jpg and
  https://commons.wikimedia.org/wiki/File:Avia_S-199_in_June_1948_(Israeli_Air_Force).png
- The Spitfire from 15 October 1948, the first sourced sortie (three Spitfires escorting Beaufighters to El Arish).
  https://he.wikipedia.org/wiki/%D7%A1%D7%95%D7%A4%D7%A8%D7%9E%D7%A8%D7%99%D7%9F_%D7%A1%D7%A4%D7%99%D7%98%D7%A4%D7%99%D7%99%D7%A8
  The first Spitfires flew in on 27 September 1948 (Operation Velvetta).
  https://he.wikipedia.org/wiki/%D7%9E%D7%91%D7%A6%D7%A2_%D7%95%D7%9C%D7%95%D7%95%D7%98%D7%94
  en.wiki's operational history has IAF Spitfires in combat on 21 October, but wrongly calls a claim that day the
  IAF's first victory: the first were on 3 June 1948.
  https://en.wikipedia.org/wiki/Supermarine_Spitfire_operational_history

Brigades, the squadron's bases and the ships: every position, date and coordinate has its own sources and check date
in `content/map/units_1948.yaml`.

The photographs are references only: no image file is copied into the repository.
