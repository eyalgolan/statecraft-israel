# Changelog

All notable changes to Statecraft: Israel. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Every pull request that changes the game (`sim/`, `content/`, `unity/`)
adds a line under **Unreleased**; CI's `Changelog` job checks this (see `CONTRIBUTING.md`).

## [Unreleased]

### Fixed
- Player builds work again (refs #680). Since #680 every `Build.*` failed with "no CRC for the bundle", because Unity
  reads a bundle's CRC from its `.manifest` and the collection's bundles were copied into StreamingAssets without them.
  The checks file is now written in the bundle cache and copied beside the shipped bundles.

### Changed
- Sourcing audit of the MVP-window office rows (#457): the First Government row no longer cites Wikipedia (the
  Knesset's position records, the 14 May 1948 minutes, Ben-Gurion's diary page, the plenum records and the
  Knesset's history pages cover every value); the Provisional Government keeps its two named Wikipedia lines for
  the coalition, the State Council keeps one named Wikipedia line for the seat distribution, and the Third
  Government keeps its named he.wiki lines. No values changed.
- The Treasury is now shown as Money, כסף in Hebrew, on the top bar, the Budget screen and in the causes (refs #382,
  R-MONEY-001; spec §1.2, owner, 15 October 2026, Q-MONEY-4 and Q-HUD-3). Debt and foreign currency stay in the sim
  (D-C3). No upkeep yet: per Q-MONEY-2 it waits for Phase 3. No golden moves; the content hash does (the name is in
  `content/model.yaml`), so an older save loads with the "different game content" warning.
- An event option's Treasury cost the Treasury cannot cover is now borrowed at once, like every other payment, with a cause
  naming the event (refs #681, R-MONEY-003). Before, the Treasury went below 0 until the deficit step, which reported the
  shortfall as "to cover the deficit". End-of-turn totals are unchanged and no golden moves. The top bar's Treasury is
  still cash minus debt, so it reads negative once anything is borrowed.
- The unit figures' shirts are darker (0.9 of their paint), so the figures read darker than their earth base as demo2
  T29's do, 0.5-0.9 of its luma at the Harel and plain 30 km looks and at gameplay03 (refs #645, #197), pinned by
  `TheFiguresReadDarkerThanTheirBaseAsT29`. #559's shirt sample landed on the chest brace; the new test reads the shirt.
- `bernadotte_assassination` says "about 250" Lehi members were arrested, not "about 200" (refs #516; owner, 15 October
  2026): Ben-Gurion told the Provisional State Council 246 (184 + 62), and S/1315 gives 266 by 23 September (184 + 82).
  Card text only: no golden moves, the content hash does, so an older save loads with the "different game content"
  warning. A sensitive event: the change returns to the owner for sensitivity review.
- A turn that could not run says what to do next (refs #622; maintainer, 15 October 2026): "Not done: The turn could
  not run. Your orders were cleared. Give them again.", in English and Hebrew. End turn already clears the orders when
  the turn fails. The Hebrew waits for the native reviewer (#564).
- The International standing meter is named "International support" (refs #622; maintainer, 15 October 2026), on the
  top bar, its tooltip and the "Why?" causes; in Hebrew תמיכה בינלאומית, the owner's word (Q-HUD-3, #375). The ids
  (`intl_standing`, `hud.caption.standing`) stay. No golden moves; the content hash does (the name is in
  `content/model.yaml`), so an older save loads with the "different game content" warning.
- Event timing for monthly turns (refs #382; lands with the GDD cutover, Phase 2 PR 1; owner decisions of 15 October 2026
  on #567): six war-period windows open on the monthly turn date before their decision (`idf_founding`,
  `jerusalem_military_government`, `latrun_proposal`, `operation_horev`, `elections_1949`; `operation_horev` also lets
  `sinai_withdrawal` and `rhodes_talks` fire again). `ten_days`, `bernadotte_assassination`, `operation_hiram`,
  `jerusalem_ceasefire` and `sinai_withdrawal` are rewritten as decisions during or after the fact; Hiram loses its
  "protest to the UN" option. `altalena` opens on 15 June 1948 without the Kfar Vitkin clause (Q-CUT-7). The owner
  re-reviewed the four changed sensitive events on 15 October 2026 ("Approve all 4", #600).
  Sensitive events may no longer raise food, morale or international standing either (Q-EFX-2): the gains in `altalena`,
  `bernadotte_assassination` and `reparations_claim` are removed, and each of those options carries a small cost in
  another resource instead (Q-EFX-3, design values; the reparations claim's is Money, #600). The event-gap ruling is restated in months: 6, and 2 in wartime
  (Q-CUT-8). Every golden moves; the cutover re-pins them once.

### Added
- Tal's (@pixelrenderai-hash) 23 single-building Tripo models from #507, imported for the strategic map (refs #659, step
  1): `unity/Assets/Art/Settlements/Prototype/Models/b*`, outside every `Resources/` folder, meshes compressed with no
  animation, rig, tangents or Read/Write, colour maps committed and crunched at 512 px; `BuildingModelImportTests` pins
  these settings. The texture paths from Tal's machine inside each FBX are blanked. Nothing references the models yet,
  so the game does not change and the WebGL build ships the same assets (its `.data` file differs by 56 bytes).
- The 1948 Collection's 31 non-building models by Tal (@pixelrenderai-hash, from #507), streamed after the first frame
  (refs #664, #663): 12 people, 13 vehicles, ships and aircraft and 6 animals in `unity/Assets/Art/Collection/`, slimmed
  by `tools/slim_collection.py` (owner, 15 October 2026: "Plain git, slimmed first"): a far mesh of at most 1,500
  triangles (4,500 for vehicles) and a near mesh of at most 8,000 per model, the idle clip kept on the 13 models that have one, colour maps
  crunched at 512 px. Every `Build.*` player build makes three AssetBundles into `StreamingAssets/collection/`, and
  `CollectionStream.Request(id, detail, callback)` loads a model after the frame that asks for it. The bundle build
  writes each bundle's Unity CRC, size and SHA-256 to `collection-checks.txt`; the loader passes the CRC to Unity and,
  on disk, opens only a file that matches, so a missing or damaged bundle (even one byte inside its data) logs one
  warning and the map runs on without the models, and an exception from the source or a caller's
  callback is caught for that request alone (one warning) while the stream keeps serving. The FBX files carry no path
  from the authoring machine: the tool exports them without material or texture references, overwrites the export path
  and refuses any file with a home, temp or session path left. Nothing places them yet (#665-#670), so the first
  download does not grow. `CollectionImportTests` and `CollectionStreamTests` pin the import settings, the bundles and
  the fail-open paths.
- `title_he_form: feminine` in `people.yaml` (refs #558): a person so marked takes the offices' feminine Hebrew title
  (`title_he_f`, שרת העבודה) through `Office.TitleHeFor`, sourced in `biography_sources.title_he_form`. The loader
  refuses any other value, a blank, a missing source, and a marked person holding an office without `title_he_f`.
  Golda Meir is marked; she is the only woman in the Knesset's government position records for 1948-1956 (KNS_Person,
  checked 15 October 2026). Display only: no golden moves.
- Portraits for eight 1948-1951 office holders who had none (refs #6; owner, 15 October 2026: "Yes, source more"):
  Lavon, Eshkol, Dinur, Burg, Naftali and the deputy ministers Efrati, Shari and Kahana, from Knesset Archives member
  portraits under CC BY-SA 4.0, each cropped and downscaled to 512 px tall with no upscaling and credited in `NOTICE`.
  Geri and Pinkas still have none: no single-subject photograph of them is under an accepted licence. Yadin keeps his
  lettered plate (owner, 15 October 2026: "Keep the plate"): photographs of him under accepted licences exist, but
  they are 1977-1981 Knesset portraits or 1950 Meitar Collection group shots, held back for era and composition.
- A person's sign-off beside a status (refs #375, #564, #611; owner, 15 October 2026: "Yes, humans only"): every status
  row takes `reviewed_by` (the reviewer's GitHub handle, never an email) and `reviewed_on` (the day), both or neither.
  The sim's loader, the map's readers (localities, the 1945 populations, units) and `tools/routes_1948.py` refuse a
  half, blank or malformed sign-off. `scripts/oss/reviewed_by_guard.py`, in CI's Changelog job, fails a pull request in
  which a commit with an AI co-author trailer adds, changes or removes either line, or a person's `reviewed_on` is
  after their commit; it fails closed. It also fails a pull request in which such a commit edits any field of a signed
  row, the id included, until a person signs the row again or removes its sign-off (owner: "Block it in CI"). The review
  queue leaves signed-off rows out and counts them.
  `docs/HISTORIAN_REVIEW.md` records the reversal of its old "no reviewed_by keys" rule. No row is signed off yet.
- Hebrew names wait for a person (refs #582, #564; owner, 15 October 2026): every Hebrew name #582 added, which only an
  AI agent checked against its sources, now carries `*_review: agent-checked` (`title_he_review`, `title_he_f_review`,
  `name_he_review`, `body_he_review`; 100 flags: offices, parties, governments, legislatures, the game settlements and
  five 1948 localities). A native speaker's review in #564 removes the flag. The loader and the map's readers accept
  only `agent-checked`, and only beside a name. CONTENT_POLICY section 2, rule 6, says so. The content hash moves; no
  golden does.
- GDD v0.1 Phase 1, the Unity queue takes the GDD orders (refs #380): `AssignWorkers`, `Train` and `CancelTraining`
  now queue in the slice, one per city, per city and track, and per cohort; an unknown city, training track or cohort is
  refused before End Turn. A reflection test fails for any command type that reaches the queue's default branch.
- GDD v0.1 Phase 1, a dev flag and the score (refs #380): `?mvp=1` in the WebGL page's URL loads the content through
  `MvpSlice.Overlay` (the GDD slice) for playtests. The golden check and the smoke run ignore it, and a slice game never
  reads, sets aside or writes the player's autosave (and has no New Game). In the GDD loop the top bar shows the score.
- The top bar at promo 03's height (HUD round 2, part 3, refs #398): 70 px, 6.5% of a 16:9 frame (it was 96, 8.8%), with
  the date plate hanging below it as promo 03's does, two-line meters (Security's level beside its caption), New Game a
  round icon named in a tip, and a smaller End Turn. With the screen buttons in the left column (#602) the bar needs about
  248 px less, so 16:10 and 4:3 windows keep the one-row bar, and 5:4 the stacked bar with its icons. The decision card's
  backdrop reaches up to the bar at every tier, so no strip of map stays live under a card. The bar's small text (captions,
  Security's level, the attention line, the plate's turn and date) stays at least 12 px on screen at small windows, the owner's floor: it is set
  larger as the panel scales down, and the bar takes a taller tier when it needs the room. Presentation only: no golden
  moves.
- The left icon column (HUD round 2, part 2, refs #398): the four screens' buttons (Cabinet, Budget, Defence, Why?) move
  from the top bar to promo 03's column of round icons at the left edge, icon-only, each named in a tip beside it on hover
  and on keyboard focus. No new action. Tab still runs Cabinet, Budget, Defence, Why?, New Game, End Turn; the column stays
  usable under the decision card, as the top bar does, and the card's backdrop, the screens and the toast start right of it.
  Budget's button draws promo 03's ledger (owner, 2026-10-15), a new `UiIcon` glyph drawn in code.
- The minimap as promo 03's (HUD round 2, part 1, refs #398): a painted relief of the country, baked once from the map's
  own heightmap, ground colours, land use and coast (`tools/minimap_bake.py`, `Resources/Map/MinimapRelief.png`, checked
  fresh in CI) in place of the 128-pixel hex strip painted at boot, and beside it promo 03's three round buttons: zoom all
  the way out, show or hide the settlement names, show or hide the region borders (and their caption), icon-only, each named in a tip beside it on hover and on keyboard
  focus (`HoverTip`; UI Toolkit draws `tooltip` only in Editor UI). The picture stays
  mouse-only (accessibility audit B1d); the buttons take Tab after End Turn. Presentation only: no golden moves.
- Sourced Hebrew names in content (refs #558, #193), data only (no screen shows them yet): `title_he` for 30 of 31
  offices, plus `title_he_f` where a woman held the office (Labour, Foreign Affairs), `name_he` for 25 of 26 parties,
  the 8 governments and the 25 game settlements, `body_he` for the 4 legislatures, each with its own `*_he_sources`
  (Knesset records or Hebrew Wikipedia, checked 14 October 2026); the loader and `SettlementPlacer` refuse a Hebrew name
  without a source. Places take their 1948 form (Tel Aviv, Beisan, Kfar Ata, Pardes Hanna, Tarshiha), and the localities'
  runtime file carries `name_he`. The content hash moves; the goldens do not.
- Two more brigades on the map (refs #385, #197): Carmeli facing the Syrian bridgehead at Mishmar HaYarden, from the
  Rosh Pinna side, during Operation Brosh (9 to 18 July 1948), and Alexandroni at its Netanya headquarters (Camp Dora;
  15 May to November 1948), each sourced in `content/map/units_1948.yaml`. Givati's window at Gedera now ends with its
  sourced move to the centre in November 1948 (it ran to the Egyptian armistice). Carmeli is
  not drawn at Haifa, its sector's place: the map has no clear spot outside the town (#588). The 8th Brigade's position
  (Tel Litvinsky, 24 May to 9 July 1948) is recorded, but it stays hidden: the map has no clear spot for it on its own
  side (#588). Oded stays hidden:
  its sources conflict and name no place. Map-only data: the sim goldens do not move; the units roster pins in
  `UnitsInPlayTests` are re-pinned.
- Two-colour borders (refs #196, owner rulings 2026-10-14): from 11 June 1948 the map colours areas by side, as promo 03 does:
  the Israeli side in a deeper blue, the other side orange-red, and a dashed grey for contested ground (only for the days an
  operation ran there), no-man's land and the demilitarized zones. The sides come from the front lines of Israel State
  Archives map 6158/1 (9 July, 18 July and end of October 1948), traced from the best public copy (about +-1-2 km), and from
  each 1949 agreement's date from the sides of its armistice line. Before 11 June 1948 the 1947 sub-districts are drawn in
  grey (no sourced line). The label names each side before its colour, the source line's date and, for Jordan, low accuracy;
  the info panel says who held the hex. The colours claim military control on a date, nothing else; the 1948 localities are
  unchanged. New data `content/map/control_1948_49.geojson` and `control_hexes_1948_49.yaml` (draft, owner-verify; licence
  basis decision D-C), tools `control_digitise.py` and `control_bake.py`, a third (neutral, dashed) border style in the
  shader. Presentation only: no golden moves.
- Portraits for Maimon, Bentov and Shapira (refs #30, D-P): their Government Press Office photographs, which Commons
  marks PD-Israel only, are replaced by Knesset Archives portraits under CC BY-SA 4.0 (Maimon, Bentov) and a Meitar
  Collection photograph by Benno Rothenberg, 23 October 1952, under CC BY 4.0 (Shapira, cropped from a group shot), each
  cropped and downscaled to 512 px tall with no upscaling, credited in `NOTICE`. `scripts/oss/commons_licences.py` now
  records an explicit Cc-by-sa-4.0 file as `CC-BY-SA-4.0`, verified only with a named artist and a `NOTICE` entry, with a
  share-alike note; `PortraitImport` accepts CC BY-SA 4.0. Kaplan, Gruenbaum, Bernstein, Zisling, Rosen and Sheetrit stay
  on GPO photographs for now.
- GDD v0.1 Phase 1, build yishuv at one designated site (refs #378), behind the `systems.loop` switch: `content/sites.yaml`
  holds Mefalsim (founded 12 June 1949; Gvati's settlement list and Yad Ben-Zvi), read only by the GDD loop.
  `PioneerWork(build_yishuv)` sends an idle halutz unit to build there, paid at once; `StopWork` frees the
  unit and the site and keeps the Money spent (D-A3). Construction is step 5 of the GDD turn (D-A6) (R-SITE-001 to
  R-SITE-003). The build turns and cost are placeholders in `MvpSlice.Overlay`, never under `content/`; both orders cost 0
  Attention (D-A6: unit orders free). By the owner's decision "Allow borrowing" (15 October 2026), `PioneerWork` and
  `Train` borrow what the Treasury lacks, as map actions do. `mvp-slice` moves (the reference player builds Mefalsim by
  15 March 1949, and the golden now runs to that date); the legacy goldens and `scenario-mvp-absorb-first` do not.
  The Unity order queue (`SimBridge`) takes both orders, one per unit.
- Map: every army token stands as in demo2 T29: the figures shoulder to shoulder on a round, dark, mottled earth base
  built on the terrain's own triangles (it follows the ground on any hill, no mesa, no gap downhill), a bright white
  ring on the ground just outside it, and its side's flag, drawn unlit, on a pole at the base's back edge, turning about
  the pole to face the camera (owner, 15 October); the figures at 1400x, 1.8 times a block (owner, 14 October; refs
  #197). `units_1948.yaml` gives each unit a `side`; only Israel's flag is drawn, from the 1948 proclamation, and other
  sides fly none until a sourced flag exists. The showcase trio stands off the routes in a kit clearing. Aircraft and
  ships are unchanged.
- Desktop release builds from the Mac (refs #54; Phases 0-1 of the plan in PR #482): `Build.LinuxMono` and
  `Build.WindowsMono` build the Linux and Windows x64 players on Mono (decisions O7, O8) and put the committed IL2CPP
  setting back afterwards, and
  `tools/release_build.py` builds the macOS, Linux and Windows players under the machine lock, runs the player golden
  check on the macOS player and writes a build record (commit, Unity version, UTC date, SHA-256 and size per player,
  golden results) outside the repository. It never commits, pushes or uploads.
- The map's frame (refs #36, promo 02): at the overview the board now ends in a walnut frame on a dark wooden table,
  with a bevel of cut earth where the relief stands above the frame and the frame's lip where the sea and low coast meet
  it. It replaces the border skirt's flat tan slab and the sea past the map's edge. At 16:9 only the overview shows it (the
  camera fit of #174 keeps it off screen wherever the view is no wider than the map: below about 106 km at 16:9, 53 km
  at 32:9; a portrait window never shows it); one draw call, no shadow pass, no golden moves.
- Map: a transit camp the player builds (from 1950) is drawn as a ma'abara instead of a marker box: rows of white and
  khaki canvas tents with a few tin or canvas huts, a long communal hut and, in some camps, a water tower, on the
  nearest free ground in its own hex (drawn as its box when the hex has none). Built when the camp is placed, not at map
  build; the tent/hut share is a parameter for a staged look later. The camp looks worn: tents sag between their poles,
  crease and lean, with dusty skirts, bleached roofs, patches and open door flaps; the tin huts are loose, rusting sheets,
  some lifted or missing; washing lines and bundles stand between the tents. Presentation only (refs #32).
- GDD v0.1 Phase 1, the first new yishuv objective and the score (refs #379): `content/objectives.yaml` holds `first_new_yishuv`, completed when Mefalsim is built, with Mefalsim's founding date
  (12 June 1949) as its reference date, and `content/events/first_new_yishuv.yaml` is its card. At the end of the turn the
  site is built, the objective is recorded once and scores 100 points plus 5 a calendar month before history, capped at 60
  (the owner's D-B6 and D-B2), and the card fires once; it never fires in the legacy loop (R-OBJ-001 to R-OBJ-003). An
  event's new `objective` key names its objective; an unanswered card resolves without an RNG draw. The point values are
  `rules.yaml` design values (`mvp.objective_points`, `objective_bonus_per_month`, `objective_bonus_cap`), read at the
  turn's start date. `hash1950` (`ad806776`), `hash1952` and `mvp-end` (`0f54dc51`) move: the reference player builds
  Mefalsim on 15 June 1949, so the objective completes on time for 100 points and its card fires and is answered;
  `smoke5` and the three scenarios do not move.
- GDD v0.1 Phase 1, training (refs #377), behind the `systems.loop` switch: `Train` sends a city's civilians without work
  to basic training in a military camp (recruits, a count) or pioneer training in a Hakhshara (halutzim, movable units at
  the city's hex), paid from the Treasury, with any shortfall borrowed as debt (owner decision, 15 October 2026);
  `CancelTraining` returns the people but not the Money (D-A3).
  Cohorts progress as step 4 of the GDD turn (D-A6) (R-TRAIN-001 to R-TRAIN-003). One camp and one Hakhshara at the start
  is a labelled design assumption (D-D5); training turns, costs and Attention costs are placeholders in `MvpSlice.Overlay`,
  never under `content/`. `mvp-slice` moves (the reference player trains one halutz group); the legacy goldens and
  `scenario-mvp-absorb-first` do not.
- The records that only make sense in a state now carry dates (refs #80): three policies, three effects and nine actors from
  15 May 1948, the United Nations from 24 October 1945, the state offices from 14 May 1948. Yaakov Dori's chief-of-staff
  term is split at 14 May 1948 into a new `haganah_chief_of_staff` office and `chief_of_staff`. A Founding start holds the
  same records and the goldens are unchanged. `rationing` and `rationing_feeds` stay windowless (their office begins in 1949).
- GDD v0.1 Phase 1, one city (refs #376), behind the `systems.loop` switch: Tel Aviv with housing, jobs, civilians and
  refugees as counts; `AssignWorkers` (Attention, refused in the legacy loop); and a `TurnResolver` that runs, in the
  approved order (D-A6), Money from jobs, housing, Social cohesion and arrivals from the CBS series as refugees, housed on
  the next turn (R-CITY-001 to R-CITY-005); `people_per_unit` must be the same in every era. Every balance value is a placeholder in `MvpSlice.Overlay`, never under
  `content/`; Social cohesion feeds nothing else and is not shown while D-C1 is open. The reference player for the GDD
  goldens (`Statecraft.Sim.Reference`, D-A7) is test policy that no game mode or UI reaches. New scenario golden
  `scenario-mvp-absorb-first`. The real game is unchanged: `hash1950`, `hash1952`, `smoke5` and the two 1948 scenarios do
  not move.
- The start screen can play David Ben-Gurion's reading of the sentence that proclaims the state, from the recording
  of the ceremony of 14 May 1948 (Israel State Archives). It plays only from a "Play the recording" button, over the
  still painting. The declaration's own Hebrew and English words show as captions once the audio is heard, and the
  credit "Recording: Ralph Hellinger, Israel State Archives" is on screen. It never plays on its own, never on Continue
  or New Game, and not in automated runs. A slow or failed load says so. The file is a cut of the archive's own upload,
  decoded to WAV with no other processing (1.8 MB), and fetched only when asked for.
- Decision videos, phase 3: outcome clips at End Turn. When the player has ordered the historical option of one of three
  events (the transit camps, the Hula works, Operation Horev) and the turn applies it, a 3.5 s silent AI-generated clip
  plays once, captioned "Your order: ...". It is drawn over any open screen, which closes, and holds the keyboard focus:
  a click, Enter or Space skips it, and Esc skips them all. A counterfactual option, a delegated answer, a refused order
  and Deport get none, and there is none in automated runs. The Horev clip waits for the owner's sensitivity review
  before a shareable build. Presentation only: no golden moves.
- Top bar (refs #398): each meter's tooltip holds its value and the change since last turn in words, for example
  "Treasury: IL 18.6 million, down IL 1.4 million since last turn", with captions from the strings (English and Hebrew).
  It is kept for a later accessibility layer: no screen reader reads a runtime tooltip yet. A resumed game now shows
  the same changes as before the reload on all six meters: `GameSession` keeps the step before the last
  (`BeforeLast`), so Immigration's change survives a load too. Not part of the state or the save: no golden moves.
- A Hebrew UI preview, asked for by hand: "?lang=he" in the page URL or "-lang he" on the desktop command line (#193).
  The UI loads `strings.he.yaml`, sets every text in Frank Ruhl Libre through the advanced text generator, gives each
  text the direction of its own words (dates, money and names are still English, #55), and mirrors the layout: rows,
  bars, the hemicycle, the option signs and End Turn's hourglass. English stays the default and has no visible switch.
  A long Budget office name now wraps in its column, so no row is wider than the screen.
- Map: boats and a calmer open sea (refs #388). Promo 03's white-sailed boats: 10 sailing boats off Jaffa, Tel Aviv, Bat Yam, Haifa, Acre and Gaza, and 7 craft moored in or off the harbours of Jaffa, Tel Aviv and Haifa (a small coastal steamer at anchor off Haifa), kept off the breakwaters and moles. Jaffa's 5 stub-sail hulls are gone and its houses are unchanged. At the gameplay zoom the open sea calms far from the shore (edge density 0.048 to 0.020; promo 0.012) and keeps broad light and dark swells, and #402's surf, shallows and near-coast chop are unchanged. At the overview a lighter teal shelf runs along the coast and tints the shallows' outer edge a little. The effects follow the camera's distance, not the pixel size, under a new `seacalm` toggle. Presentation only: no golden moves.
- The Hebrew UI preview translates the texts `Game.uxml` wrote in English (refs #193): the top bar's captions (the same
  words as their tooltips), screen buttons, New Game and End Turn; the screen titles; the Budget and Defence buttons; the
  draft mark; the loading line; and the start screen's source line, recording credit and buttons. The game's name and the
  Declaration's official English stay as they are. A seat count says "חברים" for the appointed Provisional State Council
  and "מנדטים" for the Knesset. "כישורים" for competence and "המצב הביטחוני" for the ladder. English is unchanged.
- Decision videos, phase 2 (#434 follow-up). On 11 decision cards an Illustration switch over the photograph plays a
  short, silent AI-generated loop. It is labelled "AI-generated illustration" in place of the credit and carries a small
  burned-in bilingual label. The photograph always shows first; nothing plays until the player asks, and the choice holds
  for the session. Clips stream from StreamingAssets only when asked for, so boot is unchanged, and video is off in every
  automated run. A clip that fails puts the photograph back. Deport never gets any video. The two clips on war-casualty
  events wait for the owner's sensitivity review before a shareable build. Presentation only: no golden moves.
- Portrait for Dov Yosef (refs #6): a Meitar Collection photograph (Benno Rothenberg, National Library of Israel, 1949) under CC BY 4.0, cropped, with its credit in `NOTICE`, on the extended `commons_licences.py` path from #30. Likeness check and the NLI confirmation before a paid release stay with the maintainer. Unity not run.
- Portraits for Remez and Levin (refs #30): Government Press Office photographs, which Commons marks PD-Israel only, are replaced by Meitar Collection photographs (National Library of Israel, 1950 and 1949) under CC BY 4.0, cropped, with per-photo credits in `NOTICE`. `scripts/oss/commons_licences.py` now records an explicit Cc-by-4.0 file as `CC-BY-4.0` and marks it verified only with a named artist and a `NOTICE` entry; public domain, CC0 and the PD-Israel-only refusal are unchanged. Likeness check and the NLI confirmation before a paid release stay with the maintainer. Unity not run.
- The website (`site/`, refs #393): a static Astro site in English (`/en/`) and Hebrew (`/he/`, right to left) with Home, Play, History, Sources, Media, Devlog, Contributors, How we make it, Press, Contribute and Privacy. Every image and video carries a visible label (in-engine capture with build and date, AI-generated concept art, or AI cinematic promo, not gameplay); the History and Sources pages show the events and sources from `content/` as they are. The in-engine images are the README's screenshots, read from `docs/images/build-*.jpg` and described in `docs/images/captures.yaml`, so retaking them updates the site. The cinematic trailer is hosted as one 720p H.264 encode, shown behind a click with captions in its own labelled section after the gameplay; a real gameplay clip listed in `captures.yaml` becomes the home page's muted hero loop, with a pause button. Self-hosted fonts and images, no analytics, no cookies and nothing loaded from another site. A `Site` workflow builds and checks it on pull requests; deploying to GitHub Pages waits for the owner.
- The GDD v0.1 loop switch (`systems.loop` in eras.yaml, default `legacy`), its state (cities, units, objectives and score,
  hashed only when set) and save schema 2, which names its ruleset: a game refuses a save made under another ruleset with a
  message to start a new game. The real game is unchanged and the goldens do not move; a new `mvp-slice` golden pins the
  switch on with monthly turns (GDD Phase 1, refs #357).
- The loader refuses an event whose window runs past the end of its era (chapters spec 9: an event may not span a boundary;
  split it). A `to` on the boundary is fine (it is exclusive); an open window (no `to`) is allowed only in the last era.
  No current event is affected (refs #65).
- The loader refuses a `from`/`to` window on the indicators the code reads by name (`morale`, `food_supply`, `housing`,
  `security`, `intl_standing`), a follow-up from the review of #313 (refs #57).
- Turn lengths per security rung are data: `turn_lengths` in `rules.yaml` or an era override, read in the era the turn
  starts in (R-TURN-001). The default is today's table, so the goldens do not move. GDD v0.1 Phase 1 uses it for monthly
  turns (refs #81).
- Indicators, policies, effects, situations, voter groups and actors can carry `from`/`to` dates (chapters spec 3.2, refs
  #57). The sim reads the records active on the game date; the state gains a record at its start and drops it at its end,
  and a `set_policy` outside the window is refused. The loader refuses a reference to a record that is not active for the
  whole window of the record using it; an event that fires after its window leaves out records that have ended. With no
  dates nothing changes (goldens unchanged). Offices and regions come later.
- Offices accept `successor`, the office that takes over when this one is abolished (chapters spec 3.2, refs #57). The
  loader refuses an unknown or self successor, one without `until`, one not in office at the handover, and one that
  could not own the budget lines and policies it takes over (several holders, or a historical title). Nothing reads it
  yet. Goldens unchanged.
- Aliyah regions accept `from`/`to` (chapters spec 3.2, refs #57). The loader refuses a window that runs backwards or is
  empty, and an aliyah period that counts a region outside its window. With no dates nothing changes (goldens unchanged).
- A `map_actions` entry in `rules.yaml` accepts `from`/`to` (chapters spec 3.2, refs #57). Outside the window the action
  is refused with the day it opens or the last day it was open ("A housing project can be placed from 1 January 1950").
  The loader refuses a window that runs backwards or is empty.
  With no dates nothing changes (goldens unchanged). This finishes the model-record windows of #57.
- Map: today's name of a 1948 locality by tap and in the info panel. A tap on a locality pill toggles its tooltip, a tap
  elsewhere hides it, and a mouse still hovers as before. The info panel lists the selected hex's 1948 localities with
  today's name where a living place carries on, by the same rule as the tooltip. Keyboard players reach the panel list
  once a hex can be selected from the keyboard (#41) (refs #245).
- Budget lines owned by office and date (`budget_owners` in `rules.yaml`, issue #108): the Immigration line (its id stays
  `prime_minister`) belongs to the Minister of Immigration until 7 October 1951 and to the Prime Minister from 8 October
  1951. A line scales effects through the owner of the day, and the Budget screen names that office. An office with
  several holders cannot own a line. The goldens do not move.
- Coalition changes inside a government's term (`coalition_changes` in governments.yaml, each with a date and sources).
  Seats, majority, the Cabinet's party rule and the causes use the coalition of the day. Adds Agudat Yisrael and Poalei
  Agudat Yisrael leaving the Third Government (18 September 1952, so 60 of 120 seats and no majority until 24 December),
  the General Zionists leaving the Fourth (25 May 1953) and returning (3 June 1953), and the Progressive Party joining
  the Fifth (1 February 1954). The goldens do not move (fixes #278).
- A turn now ends on the next era's start date, not only on the last era's end (chapters spec 4.1). A turn that would
  leave under 14 days before the boundary is lengthened to it. Real days scale the turn's rates. With one era, or no
  boundary in a run, nothing changes; goldens unchanged (refs #59).
- Writer preview: `dotnet run --project sim/Statecraft.Cli -- --preview --set key=value` plays the historical run with a
  rules.yaml change applied in memory and prints the changed state lines and a table of chosen values, without Unity and
  without touching `content/` (fixes #295).
- One golden manifest (`sim/Statecraft.Sim.Tests/golden/manifest.txt`) pins the golden hashes; `STATECRAFT_UPDATE_GOLDEN=1`
  now rewrites it and the `GoldenRun.cs` constants, `tools/art_checkpoint.sh` reads it, and a test names any doc that
  shows an old hash (fixes #293).
- Era system switches in eras.yaml (`systems`): `legislature: state | assembly` (an assembly's vote against a policy or
  the budget is held and reported, but does not bind) and `map_actions` (the actions the era allows). The Cabinet and
  Budget screens say when the body's vote does not bind, and the hex panel shows only the era's map actions. With no
  `systems` key an era behaves as before; goldens unchanged (fixes #58).
- An office can now have several holders at once (`several_holders: true` in offices.yaml): Minister without Portfolio
  and the deputy ministers of Education. Adds Dov Yosef's term without portfolio (24 December 1952 to 15 June 1953)
  and Ami Assaf as a second Deputy Minister of Education (from 11 June 1956). Event text, policies and effects cannot
  name such an office, and the Cabinet refuses to appoint to it. The Cabinet screen shows a card for each holder. No
  golden moves (#277).
- Rules per era (#58, second piece): the attention, economy, population, security, map-action and cabinet systems read
  `RulesAt(date)` of the turn's starting date instead of `rules.yaml` directly, and the Defence and Info screens and the
  HUD follow. With no `content/rules/<era>.yaml` files yet, nothing changes: the goldens do not move. A test fails if a
  system reads the base rules directly for anything but a start value.
- JSON Schemas for `people.yaml`, `offices.yaml`, `parties.yaml` and the event files (#265), in `content/schema/`. Each file
  names its schema on the first line, so an editor with a YAML language server flags a wrong field name or a missing
  `sources:` as you type. `SchemaTests` checks all real content against them, keeps their field names equal to the loader
  types and checks that they reject the usual mistakes. The comment changes the content hash, so an old save shows the
  content-changed warning once. The golden hashes do not move.
- The Fourth to Seventh Governments (24 December 1952 to 1956) with their coalitions and the ministers' dated terms,
  the 1953-1956 deputy ministers, the Ministries of Postal Services (from 3 November 1952) and Development (from
  15 June 1953), and the Chiefs of Staff Maklef and Dayan. Dates follow the Knesset's own position records and plenum
  minutes, checked against both Wikipedias. The Third Government now ends on 24 December 1952 and gains its 1952 changes:
  Haim Cohn at Justice, Lavon without portfolio, Nurock at Postal Services, and Welfare vacant after Levin left on
  18 September (refs #26).
- A golden-hash failure now shows what changed (#256): `GameState.Describe()` writes the state `Hash()` hashes as readable
  lines, each golden run's final state is committed under `sim/Statecraft.Sim.Tests/golden/`, and a failing golden test
  prints the changed lines by section. `STATECRAFT_UPDATE_GOLDEN=1 dotnet test` rewrites the snapshots. `Hash()` is
  unchanged, so the goldens do not move.
- A start screen (#247): a painting of the 14 May 1948 ceremony in the Tel Aviv Museum hall (AI-generated concept art
  with no identifiable person), two lines of the Declaration of Independence in the official Hebrew with the official
  English under each, and Continue (only with a save) and New Game (with the top bar's two-press confirmation). Automated
  runs skip it (golden, smoke, shot, the profiles, units, borderproof), as does `?nostart`. The Hebrew is set in Frank Ruhl
  Libre (OFL) and laid out right to left by UI Toolkit's advanced text generator (#193).
- Rules per era (#58, first piece): an era can override `rules.yaml` keys in `content/rules/<era>.yaml` (only the keys it
  changes; maps merge key by key, a list or a value replaces). The merged rules are read with the strict `Rules` type, so a
  misspelt key or wrong type fails naming the era file and line, start values (`treasury_start`, `start_rung` and so on) are
  refused, a file that names no era is refused, and `rules/` is in the player manifest. They are checked like `rules.yaml`. `Content.RulesAt(date)` returns the rules of
  the era a date is in (built once at load); the founding era has no file, so its rules are `rules.yaml`. Nothing reads
  `RulesAt` yet, so the goldens do not move.
- Units in play: the map draws the 1948 brigades, 101 Squadron and the first navy ships as Civ-style tokens at their
  dated, sourced positions (`content/map/units_1948.yaml`, map-only, outside the content hash). The figures follow the
  reserve call-up, the squadron flies at security level 4 or 5 (an Avia S-199, then the Spitfire from its first sourced
  sortie, 15 October 1948), a badge marks each token, and selecting one shows the promo's ring and a read-only card with
  a link to the Defence screen. Every position ends on a sourced date or on its front's armistice. Screenshot runs show
  units only with `&units`. The goldens do not move.
- `content/eras.yaml` with the `founding` era (15 May 1948 to 1 January 1957), a `Content.EraAt(date)` lookup, and the end
  of the last era: the turn that reaches it ends on it (a turn that would leave under 14 days is lengthened to it), the
  game ends with the stable code `era_ended` and the text "The chapter has ended", and later orders are refused. The
  rejection text for a finished game now shows the reason, not its code. The Why panel shows the end under a new
  "Chapter" group. The golden hashes do not change (#56).
- Region borders in the promo look (promo-borders): two-tone outlines along hex edges (a blue band with a white inner
  stripe, a red-orange band with a cream one), drawn inside each region by the terrain shader from one texel per hex, with
  a "borders" visual toggle. Visual only. No invented boundaries: the first real data is the 1948 sub-districts (below);
  the 1949 armistice lines and fronts are research items.
- A portrait of Yitzhak Ben-Zvi: a crop of Harry Pot's 1958 Anefo photo (CC0; later than the Founding slice; #6).
- A portrait of Chaim Weizmann: a crop of a Bain News Service photo, ca. 1915-1920, from the Library of Congress
  (US public domain, PD-Bain; an earlier likeness than his presidency; #6).
- A portrait of Yaakov Dori: a crop of Willem van de Poll's 1949 Nationaal Archief photo (CC0; #6).
- The Third Knesset and the 26 July 1955 election results (seats by list), with the National Religious Front and the
  Religious Torah Front as parties; the Second Knesset now ends on 15 August 1955 (refs #26).
- Hebrew UI translation data with key and format-token checks; not yet enabled in the game (#12).
- A local multi-size favicon for the WebGL page.
- A portrait of Zalman Shazar: a crop of Yoichi Okamoto's 1966 White House photo (US public domain, PD-USGov; #6).
- Hebrew names and sourced birth and death dates for all 29 people (issue #1).
- Successors for offices left by deaths in 1952: Ben-Gurion takes Transportation after Pinkas (14 August 1952) and
  Yitzhak Ben-Zvi becomes President on 16 December 1952 after Weizmann; the presidency is vacant in between, while
  the Speaker was acting President (issue #1).
- A portrait of Golda Meir: a 1964 Nationaal Archief photo by Willem van de Poll (CC0; #6).
- Historical-only deputy titles and sourced 1951 terms; Kaplan's June 1952 Deputy Prime Minister title and the linked Finance/Agriculture succession (issue #4).
- People can carry a birth date and a source for each biographical value (Hebrew name, birth and death dates) in
  `people.yaml`; the loader rejects unknown source keys, sources without a `url` or `checked` date, and impossible birth
  dates.
- Browser-playable 1948–1951 (milestone M3): HUD, Cabinet with appointments, Budget, Events, Defence and "Why did this
  happen?" screens over the 3D hex map; sourced content to 31 December 1951; saves; a calibrated economy; the golden
  hash checked in WebGL and macOS players.
- The map (milestone M2): terrain from Copernicus elevation data, biomes from rainfall, 1948 settlements on hexes.
- The 1948 sub-districts on the map (promo-borderdata): the sixteen Mandate sub-districts in force on 15 May 1948
  (Palestine Gazette No. 1552, 1947), digitised from the Survey of Palestine's "Administrative Divisions" map and stored
  as true lines (`content/map/subdistricts_1948.geojson`, sensitivity-reviewed); the map shows them snapped to hex edges in the
  blue border style, labelled "Mandate sub-districts, 1947" from the 1948 start until the 1949 armistice lines replace
  them; the info panel names a hex's sub-district and district. Display only: the sim does not read them.
- The 1949 armistice lines on the map (armistice-lines, #196): from each agreement's date (the Mandate sub-district
  layer's per-front switch) that front's armistice zones are drawn over the sub-districts, and the label and info panel
  name its lines; the sub-district bands stay as the base. Egypt (24 Feb 1949): the El Auja Demilitarized Zone and the Gaza-Rafah line from the
  Wadi Hasi to Gaza, and from 22 Feb 1950 the line on the signed modus vivendi map; Lebanon (23 Mar 1949): the
  international boundary; Jordan (3 Apr 1949, line A in the Iraqi sector by its May-July 1949 stages): low accuracy,
  traced from a low-resolution copy of the signed map and labelled so, Jerusalem left out; Syria (20 Jul 1949): the line
  through Annex I's points and the three-part Demilitarized Zone, from the annexed 1:50,000 map. Stored as true lines
  (`content/map/armistice_lines_1949.geojson`, status draft), drawn snapped to hex edges in the red-orange style; the
  info panel names the line or zone and its agreement. Display only: the sim does not read them.
- Open-source release (public since 2026-10-10): licences (MIT code, CC BY-SA 4.0 content, CC BY 4.0 original docs
  and art, CC0 for AI-only images), asset provenance (`ASSETS.yaml`), contributor
  and content policies, CI for the sim, tools and repository policy.
- Unity-MCP 0.93.3 (`com.ivanmurzak.unity.mcp`, from OpenUPM) as the standard AI-agent bridge in the editor, local
  Custom mode with token auth only (held by `UnityMcpGuard`). It stays offline in batch mode, opens no window there, and is
  left out of player builds.
- A runtime quality fallback for slow machines in the browser: when frames stay slower than 30 fps, the game lowers its
  render scale (to 0.9, then 0.8) and then turns the tilt-shift off, and steps back up when frames are comfortably fast
  again. Presentation only; `?nofallback` turns it off (#44).
- Map: procedural settlement kits and instanced trees (placeholder art) replace the marker boxes: a white Tel Aviv,
  stone Jaffa with its harbour, Arab towns, moshavot and kibbutzim sized by their 1945 population
  (`content/map/settlement_population_1945.yaml`), with citrus groves, olives, palms and cypresses, at the owner's
  symbolic scale (about 60x real size for city blocks, 100x for houses and trees). Presentation only (#31, #32, #34).
- Map: the 271 other places of 1,000 or more people on 15 May 1948 (Village Statistics, 1945) as Arab village and Jewish
  settlement kits beside the 25 game settlements (Nesher as a mixed locality), named by their 1948 names at close zoom,
  with today's name in a tooltip only where a living place carries on, and a light scatter of Bedouin camps sized by
  each sub-district's listed tribal total (`content/map/localities_1948.yaml`, draft; the player reads its baked
  compact form, `localities_1948.tsv`, from `tools/localities_bake.py`). The unit tokens keep off the
  villages and camps as they do off the towns. Presentation only: the sim never reads the file.
- The sea and coast in the promo look: turquoise shallows that deepen to a muted blue offshore, rolling surf filtered
  so it does not shimmer, a fine chop, and a sand strip with wet sand along every sea-level coast (salt flats at the
  Dead Sea). Presentation only.
- Event cards have a promo-style header photograph: real archival photographs for 24 of the 31 events (Meitar
  Collection / National Library of Israel under CC BY 4.0, Nationaal Archief under CC0, and the US National Archives),
  shown in black and white with a subtle sepia/navy grade and a gold frame, with a credit line and alt text. The seven
  events without a free photograph keep the minister's portrait. No card shows the outcome of the choice it asks
  for. On a short window the body scrolls to keep the photograph whole, which also shows photographs at 21:9; if the
  options would be cut, the photograph is dropped whole rather than cut through its credit, and it returns when the
  window grows. Sensitive pictures need the owner's review
  before a `-shareable` build, and `tools/package_release.py` now refuses a build folder that was not built
  `-shareable` or carries an unreviewed sensitive picture (docs/event-images.md). Presentation only: the content hash
  and saves are unchanged.
- The unit art slice (a showcase for the art track, not gameplay): a 1948 IDF rifle trio with a selection ring, an IAF
  Spitfire flying a loop with its shadow, and the corvette INS Haganah, built from code as painted miniatures at the
  promo's symbolic scale. Shown only with `?units` (desktop `-units`) and in `tools/art_checkpoint.sh --units`; an
  authored model can replace a code-built one per unit type (docs/units.md).
- The railways and main roads of 1948 on the map: the Jaffa–Jerusalem line, the main line from Lydda to Rafah, the
  Eastern Railway, Hadera–Haifa, Haifa–Acre–Ras an-Naqura and the Jezreel Valley line, with 13 main roads, traced on
  the public-domain 1946 Survey of Palestine map (`content/map/routes_1948.yaml`, sources per route), drawn as pale dirt
  roads and ballasted track with sleepers at the promo's scale. Each route shows only from its sourced date: the Burma
  Road appears on 14 June 1948, when it was completed. Settlement buildings and trees keep clear of every route. A steam train is a showcase only (`?train`, `?units` and the
  promo screenshots), not normal play. Map-only: the sim does not read them (#35).
- Land use on the map (#33): a patchwork of strip fields (green, ochre stubble, ploughed, fallow, groves in the citrus
  belt) with hedges and farm tracks, and garrigue, terraces and scrub on the wet hills, placed by real slope, rainfall and
  towns and baked by `tools/land_bake.py`. Presentation only. Frame time: measured only on a busy machine so far (about
  +0.9 ms at the gameplay zoom), not yet on an idle one.

### Changed
- **The GDD cutover (GDD Phase 2 PR 1, refs #382).** The real game now plays the GDD v0.1 loop on monthly turns, from
  15 May 1948 to the game's end on 15 January 1952, 44 turns (owner decisions D-B1, D-E1, D-E5):
  - `content/eras.yaml` sets `systems.loop: gdd` and `content/rules.yaml` sets `turn_lengths` all month (R-LOOP-001,
    R-TURN-001) and `mvp.end: 1952-01-15` (R-LOOP-004; the game is then over, `mvp_ended`).
  - The Phase 1 balance numbers move into `content/` unchanged as labelled design values (owner, Q-CUT-1): new
    `content/cities.yaml` and `content/unit_types.yaml`, the rules' `mvp:` block and the GDD orders' Attention costs, each
    tagged `# DESIGN VALUE` and listed by `DesignValuesTests` (R-LOOP-005). `attention_per_turn` stays 4 (Q-CUT-5); inertia
    and the soft-event cap stay per turn (Q-CUT-6). The overlay `MvpSlice`, its `?mvp=1` dev flag (#598) and the `mvp-slice` golden are retired: the real game is the GDD game, and a game started under it autosaves and has New Game again.
  - Event windows re-timed for monthly turns (#567): `idf_founding`, `altalena` (15 June 1948, sourced; Q-CUT-7),
    `jerusalem_military_government`, `latrun_proposal`, `operation_horev` and `elections_1949` open on the 15th on or
    before their decision (R-TURN-002), so `sinai_withdrawal` and `rhodes_talks` fire again. The gap rule is restated in
    months: 6, and 2 at full war (Q-CUT-8, R-TURN-003).
  - Old saves are refused, said once in English or Hebrew: a save from before the cutover as made under earlier rules
    (R-SAVE-001), and one from an unknown ruleset as made by a newer version (R-SAVE-002).
  - The golden runs play the reference player, and every golden moves once: `hash1950` `f97e9f81`, `hash1952` and the new
    `mvp-end` `75d38299`, `smoke5` `85191d48` (five turns end 15 October 1948), and the three scenarios
    (docs/determinism.md). The browser smoke answers cards while the turn's Attention lasts. The Defence screen shows
    monthly turns on every level. The arrivals line reads "wait for homes".
  - Every new game now opens on a card (One Army, 15 May 1948), so #620's wait under the start screen covers a new game
    too, and `GameRoot.Shutdown` disposes the card first, so New Game does not load the event photo catalog for the
    game it closes (refs #592, #321). Without a start screen (the automated runs) the visible card loads it at once.
  - A refused autosave whose code has no string falls back to the short localised reason, so the start never fails on
    it; `mvp.end` must fall before the last era's end, so the game always ends as `mvp_ended`.
- The 1948 Arab villages and towns on the map are dense packs of small stone houses with lanes between them (visual
  gap S1, refs #32): each Arab locality is laid out on the village plot lattice, as Lod and Ramla are, with smaller
  one-room houses (3.8-5.4 m, still at 100x), more two-storey houses in the core, roof rooms, and terraces under the
  hill houses that stand on a slope. A village keeps its mosque, now a one-storey hall with a short square minaret; the
  domed mosque with its tall minaret stays with the Arab towns. The median Arab locality draws 11 houses (main 5), in
  the same merged tile meshes (64 draws). Presentation only (`docs/visual-v1.md`, "Arab villages: dense, terraced cores").
- Map: the ma'abara kit (Beri's, #557; refs #32) stands at a town's edge, keeps clear only of the unit tokens present in
  its time, and is never smaller than a camp. A camp's centre is now looked for on rings 450 m apart, not 1.8 km, so
  Degania Alef's camp finds the free ground at the town's edge and is a kit, not a box. A camp keeps off only the unit
  tokens whose window runs to its own date or later (owner, 15 October 2026: "Yes, date-aware"). Camps are built from
  1950, so Rosh Pinna's camp is 15 tents and 3 huts again, not the box the 1948 Operation Brosh token left it. A camp
  has at least 6 plots, one of them a hut, or its search goes on further out in its hex, and it is its box only if
  nowhere in the hex has room (owner, 15 October 2026: "Yes, min 6 plots + a hut"). Gaza's edge has room for 4 tents
  only, so it stays a box. Built on 26 August 1950: 17 camps of 25 are kits (was 15), and Ein Harod's is smaller (6
  tents and 2 huts, was 10 and 2). Every structure still stands wholly inside its hex. No golden moves.
- Tel Aviv's White City (visual gap S2, refs #31; owner rulings on #652, 15 October 2026). Its blocks are fewer and
  larger: 27, at 1.67 times main's width (main drew 55). They stand four to eight storeys, in tiers of tall and low blocks,
  with a height spread of 0.31. Each has ribbon windows along both long sides, a balcony band on every upper storey and a
  flat roof terrace behind a parapet. A quarter stand on pilotis. 26 palms grow in the streets and in the open plots
  between the blocks. Tel Aviv's census floors fall to 24 buildings and 24.5% / 21% cover, per the ruling "Lower floor to
  ~30, wider blocks"; every other town's floor stays. All of it is in Tel Aviv's own kit mesh (no new draw), so a model that
  replaces the town (#507) hides it whole. Haifa's Hadar keeps its blocks, and `Kit.shader` is not changed. Presentation
  only.
- Sea round 3, the surf towards lace (refs #388): beyond the swash line, at least one broken white breaker now stands
  clear in the surf at the gameplay zoom, where there was one swash line and a smooth band (promo 03 shows 2-4 broken
  lines; that gap is read again after #519's grade). The surf zone widens from 900 m to 1.8 km, the breakers stand 450 m
  apart (300 m before) and the foam is whiter; a 200 m lace net, behind a new `foam` toggle, breaks each crest on its own
  and fades to the old mean at the overview. All of it is in the water's own pass; the terrain function is unchanged.
- The hill olives are larger and fewer (visual gap L1, refs #34; owner, 15 October 2026: "~1.7x bigger, fewer
  trees"): the olive clumps' trees on the hills and mountains and the hill scatter's single olives are 1.7 times the
  size and 0.6 of them are kept, so their crowns match promo 03's hand-marked crown size at gameplay03 and cover about as
  much of the hills, with fewer instances. The olive clumps now keep off the hex-edge midpoints as the scatter does
  (owner: "Yes, add the guard now"). The scrub, the plain's and the valleys' olives, the mesh, the colours and the
  cypress rows are unchanged.
- The map's region files (the 1948 sub-districts, the 1949 armistice lines, the 1948-49 control areas) are read
  from baked runtime forms at boot instead of being parsed as YAML (`tools/regions_bake.py`, refs #321), as the
  localities and the route paths are. The sub-districts are read once, for the camps and the region layers. A stale,
  cut or missing runtime file is refused with one warning and the YAML is read as before. The borders, the layer
  labels and the info panel are unchanged. Presentation only.
- The settlements file is parsed once at boot instead of twice: the info panel's settlement names come from the map's
  own parse (refs #321). The names are unchanged. Presentation only.
- The units layer takes the unit spots the map placed instead of placing every unit again (refs #321), when it draws
  the same units on the same hexes; otherwise it places them as before. Every token stands where it stood, and the
  anchors with no clear spot are reported as before. Presentation only.
- Decision videos (refs #434): the clips no longer carry the burned-in "AI-generated · נוצר בבינה מלאכותית" caption.
  Owner ruling of 14 October 2026, relayed by the coordinator: "the 3 ilustration clips are great but no need to say
  there were created by ai". All 14 clips were re-encoded from their promo shots (each checked by SHA-256) with the same
  encode otherwise; the card still says "AI-generated illustration" while a clip plays, every row keeps
  `ai_generated: true`, and the four Dreamina shots keep their own "AI" badge (owner: "Keep the badge"; DECISIONS G4, G5). The owner also reviewed the three sensitive
  clips (operation_yoav, ten_days, the operation_horev outcome), so a shareable build may carry them. No golden moves.
- For a new game, the event photo catalog (`EventImageCatalog`, with its 24 crunched photographs) loads in the background
  a second after the game starts, while the start screen is up, instead of in `GameRoot.Begin` (refs #321). The "screens
  wired" step is about 80 ms shorter at `--cpu 4`. The first event card shows its photograph as before, in a frame no
  longer than with the old load; a card that comes before the load ends waits for it. `?bootlog` logs when the card
  first gets the catalog. Presentation only.
- A Continue whose turn has a decision waiting no longer loads the event photo catalog in `GameRoot.Begin` either
  (refs #592, #321): the card is built under the start screen and asks for its photograph only when the screen closes,
  by when the background load has usually finished. "Screens wired" is about 90 ms shorter on such a Continue at
  `--cpu 4`. The card still opens with its photograph, never the minister's portrait first. Presentation only.
- The units file (`units_1948.yaml`) is parsed once at boot instead of twice: the map keeps its parse and the units
  layer draws from the same list (refs #321). The units and where they stand are unchanged. The second parse cost about
  25 ms at `--cpu 4` between the map and the start screen; the map build itself is unchanged. `?bootlog` has three more
  phases round the units layer. Presentation only.
- Tel Aviv's 1948 localities (Sheikh Muwannis, Jammasin, Salama, Ramat Gan, Givatayim, Bnei Brak and the rest inside its
  footprint) keep their mosques and water towers, at a quarter and three tenths of their size elsewhere, at the street
  crossing nearest their hearts (refs #318, owner ruling 2026-10-12). The city's blocks keep their plots round them: core
  cover 18.5% to 25.6%, the whole footprint 17.4% to 25.2%, 43 to 55 blocks.
- The map reads its railways' and roads' traced lines from a baked runtime form at boot (`route_paths_1948.tsv`,
  `tools/route_paths_bake.py`) instead of parsing the YAML, as the localities do (refs #321). A missing or stale file is
  refused with a warning and the YAML is read as before. The unit placer no longer makes candidate spots for hexes its
  one-hex limit rules out; every token stands where it stood. At `--cpu 4` the units' step takes about 90 ms instead of
  300, the map build at least 165 ms less (2379-2507 to 2196-2214 ms) and the start screen about 120 ms less (the
  placer runs again there). `?bootlog` has four
  more phases inside the units' step. Presentation only.
- CI checks the water and route-ribbon bakes against the code that makes them (#535), as it does the biome colours'
  (#529): a pinned fingerprint of `MapBuilder.WaterMeshGrid` and of `RouteLayer.Plan` and `Derive`, and a numpy port of
  the water mesh. Doc comments in `MapBuilder`, `RouteLayer` and `RouteRibbonFile` point to the checks. No change in the
  game.
- The map's biome colours are read from a bake at boot instead of being blended (refs #321), as the routes' ribbons
  and the water's mesh are. The map looks the same: the bake holds the same texels, and a stale or missing bake is
  refused with a warning and the colours are blended at load as before. The map build takes about 310 ms less at
  `--cpu 4` (3050 to 2745 ms under load). `?bootlog` has three more phases: `terrain object created`, `units placed`
  and `kit buildings built`. Presentation only.
- The map's near (bottom) tilt-shift band is lighter, as on promo 03: gently soft at the play zoom and almost none at the
  wide overview, where the foreground is now sharp as on promo 02. On the owner's ruling of 2026-10-14; the far band is
  unchanged (#390). Presentation only (#458).
- The `mvp-slice` golden is re-pinned (`0451920a` to `f6931e24`): the slice now has its city and the GDD turn, and the
  reference player fills the jobs (GDD Phase 1 PR 3, refs #376). The legacy goldens do not move.
- The hills carry a loose, uneven scatter of single olives and low scrub between the olive clumps, like promo 03's hills
  (refs #34): over the wet hill country where the land use is garrigue and terraces, outside the groves, the towns'
  footprints and the hex-edge midpoints. The olives already planted stay where they are; the map has about 20,000 trees
  instead of 8,400.
- Map roads read as wide, pale dirt tracks at the gameplay zoom, as in promo 03 (refs #35): 420 m wide (was 300 m), cream
  dirt varied in patches, softer verges. The pale band is a median 9 px at gameplay03 (was 4) and 19 px at 30 km (was
  7). The route ribbons are re-baked; no route was added.
- The map's colour grade is retuned towards promo 03 (refs #38, #391): crisper, with more contrast and darker shadows,
  near-neutral split toning and slightly less saturation, so the land keeps main's warmth and reads tan rather than rose;
  the vignette stays as it was. At the gameplay pose the land's contrast comes to the promo's; the sea stays blue, a little
  darker than promo 03's (the owner's decision), and the border stripe stays near the owner's blue. Presentation only.
- The trees are warm olive-brown instead of bright green, like the groves in promo 03 (refs #34): citrus groves, olive
  crowns, palms and cypresses. Only the leaf colours change; the fruit, meshes and frame cost are the same.
- Map: settlement quick wins (refs #31). The White City's blocks are lime white, with a solid-parapet balcony on each
  upper storey, deep windows and more roof rooms. The old towns' and villages' houses step back (the top storey a smaller
  block on the lower roof), have roof rooms and grey-buff limestone walls under lime roofs. Domes stay on hill houses only,
  fewer (old towns 22% to 8%, hill villages 7%; plain villages none, as their mud-brick houses had flat roofs) and low.
  Every house stands where it did: same lots, trees and building count (docs/visual-v1.md, "Settlement quick wins").
- HUD round 1, the info panel and the trim (promo look, refs #398). Before a hex is chosen, the info panel is a one-line
  hint, not an empty block. A hex's facts (region, settlements, 1948 localities, placements) are rows with a gold sign,
  with no yields and no land-use name. When both map actions are refused for the same reason, the reason shows once,
  without buttons. The date plate, the event card, the info panel and the minimap carry the promo's filigree trim, drawn
  in code, which takes no clicks.
- Map: the unit figures' uniforms are darker and warmer, so a trio stands out against the sunlit ground as promo 03's
  does instead of khaki on khaki; a figure's slope base is painted trodden earth instead of a dark grey disc. A
  readability choice at map scale; the sourced kit is unchanged (refs #197).
- The decision card opens in the middle of the screen (decision cards, 2026-10-13): up to 1040 px wide, with a 16:9 media
  header that shows the event's archival photograph at up to 960x540 (the 24 photographs re-cropped from the same originals
  to 16:9). While it is open the map is blocked: a dim backdrop takes map clicks, and the info panel and the minimap are
  out of reach, while the top bar stays usable. Hide (or Escape) collapses it to a "Decision waiting" button that brings
  it back, and a new event opens it again with the keyboard focus on the card (Tab reaches the options; none is lit before
  the player picks). Any open screen hides it until it closes. Presentation only: no golden moves.
- The scenario runner (`Scenario`) and the `mvp-slice` golden's player (`MvpGolden`) move from the tests into `Statecraft.Sim`,
  next to `GoldenRun`, so Unity can play those goldens too (step 1 of #394). Same code, same hashes; no golden moves.
- The map's railways and roads are placed from a bake at boot instead of being built (refs #321), as the water's
  mesh is since #195. The routes look the same: the bake holds the same meshes, and a stale or missing bake is refused
  with a warning and the routes are built at load as before. `routes built` takes 51 ms at `--cpu 4`, from
  162 ms. The bake also lists where each route lies in its mesh, for the corridors of GDD Phase 4 (#384).
  Presentation only.
- Sea round 2 (refs #388). The sea has texture at the gameplay and overview zooms, where it used to read as a flat
  surface: slow wind streaks, ripples from the waves' slope towards the camera (faded out as a pixel grows towards a
  wave's size), and sparse whitecaps on the streaks' bright side, close up only. All three sit behind a new `seatexture`
  visual toggle. The glint is stronger close up, the white surf line is at least 1.5 pixels wide at every zoom, the beach
  is wider (900 m) and paler, and the water's haze starts at 120 km, so the overview's sea pales towards the horizon. The
  water shader is the only shader changed. A slow pan and zoom at the overview showed no crawl.
- Map: a boundary between two regions of the same border style (two Mandate sub-districts) draws one band of the promo's
  width, centred on the edge, instead of two full bands side by side; the Mandate frontier and a primary-secondary edge
  keep the full band. Far off (a band under about 4 px, the overview) a band is a thin line of its colour, not a white
  one. Visual only (refs #196).
- Map: the railway and road look after promo 03 (`Route.shader` only): the two rails are drawn at least 0.8 px wide and
  brighter, so they stand out at the gameplay zoom; the sleepers run every 100 m, a fine texture instead of a coarse ladder; the
  ballast is a cooler stone grey; roads have darker worn verges, so they read on pale ground. No mesh, data or boot change
  (refs #35).
- The Unity sim build helper is now `uv run tools/build_sim_for_unity.py`, usable on Windows, macOS and Linux without
  a shell. It preserves the Release publish and required DLL checks. `tools/build_sim_for_unity.sh` stays as a thin
  wrapper that runs the Python script.
- Land round 2 (refs #33): the ground's colours move from yellow-olive and gold to promo 03's warm dry ground, in the
  biome palette and the land-use bake. Hedges, tree lines and farm tracks are drawn about 150 m wide, so the fields read
  at the gameplay zoom. The hills get round bushes with a soft shadow, a stronger garrigue and clearer terraces. It is a
  re-bake of `landuse.png` (same size and format) and a palette change, so the frame time and boot are unchanged.
- Inland lowlands are no longer labelled "Coastal plain" (refs #204). `tools/hexgrid.py` now measures the distance from
  each hex centre to the shore and keeps `coastal_plain` for 0 to 200 m land within 25 km of it. Further inland the new
  biome `lowland` ("Inland lowland") applies: 30 of the 109 coastal plain hexes change, among them the hex that holds
  Tiberias, Afula and the Hula valley margin. The map colours, the info panel text (English and Hebrew) and the land
  tools know the new name. The change moves the content hash, so an old save shows the content-changed warning once. The
  golden hashes do not move, because the sim does not read `biome`.
- Rationing no longer has a vacant minister from 24 December 1952. The Knesset records show no Ministry of Rationing and
  Supply in the Fourth Government, and on 4 May 1953 the Minister of Trade and Industry, Peretz Bernstein, told the
  Knesset that his ministry held the supply tasks and, to some extent, rationing. He answers for rationing until
  29 June 1955. Dov Yosef's Agriculture term now starts on 22 March 1949, as in the Knesset position record, not 10 March
  (issues #225 and #283). The golden hashes do not move.
- The game settlements Lod and Ramla show their 1948 names, Lydda and Ramle, as Village Statistics 1945 lists them
  (owner ruling). Content only: the goldens and the smoke run are unchanged; saves made before load with the
  "different game content" warning.
- Map, towns round 2 (#31): the towns are laid out on rows of plots, dense in the core and thinning to the edge, instead
  of random spots: the 25 towns draw 397 buildings where main drew 216, and no town draws fewer than on main (fixes #317: a small town
  whose lattice places fewer than 6 houses is filled up with round 1's full-size houses on ground no other place uses;
  the kits' water test counts a lake hex as water only at the lake's surface, so Tiberias builds on its shore again) (Jaffa 15 to 33, Lod and Ramla 5 to 11, Jerusalem
  70 to 110), with 55 landmarks instead of 27. Old Jaffa is packed small stone houses; Tel Aviv's white blocks are
  taller in the core (promo ruling, at most eight storeys); Haifa has its lower town by the port and Hadar on the slope; Jerusalem has its stone
  new city, its Arab quarters and a walled Old City; the large moshavot have a plastered centre. Sourced 1948 landmarks: Jerusalem's walls and gates, the
  citadel, the Haram with the Dome of the Rock (lead dome, as before 1959) and Al-Aqsa; Haifa's port and German Colony;
  Tel Aviv's port; Ramla's White Tower; Jaffa's lighthouse moved to the hill above the port. Map only: the goldens do not
  move.
- Sharett's in-game portrait uses a CC0 Van de Poll photograph in place of the US-unverified GPO source (refs #30).
- Hebrew UI wording, still not enabled: a count-neutral pending-decisions line, "מנדטים" for Knesset seats, and
  reworded attention and full-war labels (#193).
- Ben-Gurion's Transportation term ends on 24 December 1952, when the Fourth Government took office; the seat
  shows as vacant from then until Serlin and Sapir are authored (#26).
- The four armistice events end with 1949 (`to: 1950-01-01`). They still fire on their 1949 dates; a finite window
  means their Prime Minister placeholder doesn't need a holder after his death in 1973.
- Ben-Gurion's in-game portrait is now a crop of Abbie Rowe's 8 May 1951 White House photo (NARA 200318,
  PD-USGov) instead of a Government Press Office photo with no clear US status (#30, decision D-P).
- Later death dates can be recorded for people with open-ended terms in the latest authored government; explicit term ends still cannot pass the holder's death.
- Source Dov Yosef's food-supply and rationing responsibility in the Third Government from 8 October 1951,
  replacing the unsupported vacancy (#2).
- The interface is restyled to the promo art (`promo/final/03`, `04`, `05`, `08`):
  - navy panels with gold trim, corner brackets and a soft shadow;
  - an icon for each indicator in the top bar, with its value and its change since the last turn;
  - a framed date badge;
  - round icon buttons for the screens, with their names under them;
  - signs on the event options;
  - a terrain sign in the hex panel (still no tile yields);
  - the Cabinet as a grid of portrait cards.

  Narrower windows get a compact top bar, and windows narrower than about square get a taller one that puts each change
  under its value. Keyboard focus has a fill at least 3:1 against the unfocused button, and a mouse click leaves no
  focus behind. Presentation only.
- The Cabinet and Defence screens are laid out as the promo (`promo/final/04`, `08`), wider, over the hex panel (which
  is hidden while they are open, so its buttons take no focus):
  - Cabinet: the Prime Minister's card at the side (with every office the PM holds), the legislature as a half circle of
    seats, coalition and opposition, with the majority line and each party's seats, and a card per office with portrait
    (or initials), party, and competence and loyalty as bars;
  - Defence: the Prime Minister, Minister of Defense and Chief of Staff as cards (one per person), the escalation
    ladder with the ordered level marked, the reserve call-up as a bar with its minimum, and the defence share of the
    budget.

  Every figure is the sim's; the promo's forces, intelligence and procurement panels are left out because the sim does
  not model them yet. A hidden screen does no work when the game changes, and an open one updates in place, keeping
  keyboard focus. Presentation only.
- The screens fit narrow windows (#210, #211): in the two-row HUD (narrower than about square, such as a phone's 390 px)
  every screen takes the full width, with the event column and the hex panel hidden under it, and a screen's header
  wraps. Escape closes the open screen. The Cabinet says a refusal that applies to every candidate (the Prime Minister's
  or the President's office) once, not under each name (#215).
- The historical AI no longer sends an order the turn's attention cannot cover (the rule the UI's order queue applies),
  so the sim never has to refuse one; the golden playthroughs are unchanged.
- The browser build downloads a smaller heightmap: `heightmap.r16z` (4.1 MB, packed losslessly: row deltas in byte
  planes, one zlib stream) replaces the raw 8.4 MB `heightmap.r16`, which was served outside Unity's build compression.
  It decodes to the same bytes, so the terrain is unchanged (#44).

### Fixed
- Map labels (refs #599): close over Jaffa's harbour the "Tel Aviv" and "Jaffa" pills no longer read swapped. Every pill
  is now nearer its own settlement's marker than any other visible marker (by more than 4 px), in every placing pass,
  since two sprawling kits' screen rectangles overlap and a spot against one's edge could sit by the other town. A city
  keeps its name: when every spot round its sprawl fails it may hang just under its marker point, its last pass ignores
  the 1948 localities' markers (Silwan is 1.5 km from Jerusalem), and a kit reaching behind the camera at the closest
  zoom places its pill round the marker box instead of hiding it.
- Screenshot runs (`?shot`, `-shot`; `tools/art_checkpoint.sh`) show the map again: since the GDD cutover (#600) the
  opening card, One Army, covered it in every capture. The card, its scrim and "Decision waiting" now stay hidden in a
  screenshot run, as the units do (`EventCardPresenter.HiddenInCaptures`); normal play and the smoke run still show it.
- In the Hebrew UI a refused order names its kind in Hebrew (refs #595; owner, 15 October 2026: "Yes, translate them"),
  for example "לא ניתן לזהות מדיניות", not "policy לא מוכר". Each kind is a string, `order.kind.<kind>`, in both
  strings files; English reads as before ("Unknown policy"). The Hebrew words wait for the native reviewer (#564).
- An autosave that cannot be continued (refs #631, #622) no longer shows the sim's English reason or a replay's exception
  message to the player: the note always gives the short reason "the file could not be read or replayed", in English and
  Hebrew, and the full exception goes to the log. A replay that throws keeps that exception, with its stack, as the
  SaveException's inner exception. No save format change.
- Kit faces in shadow keep their own colour (refs #32): `Kit.shader`'s ambient takes the sun's warmth where the sun does
  not reach (a bounce term, 0.6 of it in full shade, at the trilight's own brightness), so a shaded wall, roof flank or
  tree crown reads as a darker version of itself and not blue-grey under the grade's teal shadows (Safed at 6 km, shaded
  wall hue 100 to 44 degrees, saturation 0.09 to 0.27, same value). Terrain, water and every other shader are unchanged;
  sunlit kit faces get a little warmer. `KitTests.AKitFaceInShadowKeepsItsOwnHue` pins it. No golden moves.
- UI text from Jeremy Hoover's review (refs #622): a refused order no longer shows a content id or an exception's
  message after "Not done:" ("Unknown policy", "The turn could not run", in English and Hebrew); both stay in the log.
  The six Title Case labels are in sentence case ("Public support", "International standing", "Foreign currency", "End
  turn", "New game"), and the lines that name the two buttons follow them. Hebrew acronyms in content use the gershayim
  character the strings use (״, U+05F4: מפא״י, רמטכ״ל, שז״ר). The strings.en.yaml header now says which strings format a
  date. The content hash moves; no golden does.
- Content: 101 Squadron's aircraft notes in `content/map/units_1948.yaml` (refs #607). The Avia S-199 note now gives the
  sourced arrivals (one on 20 May 1948, one each on 21 and 22 May, the fourth lost in a C-46 crash dated 23 or 23-24 May,
  sources disagree) instead of "the first four arrived on 20 May"; the Spitfire note names the two sources for the
  27 September 1948 landing of three aircraft at Ramat David. Sources added, checked 15 October 2026. Notes only: no
  date changes, map-only file outside the content hash, no golden moves.
- Hebrew UI preview (refs #596, #585): the map layer label (`MapRegions.Label`) formats through `UiText.Format`, so the
  values its Hebrew patterns insert (a front's English name, the "1950 modus vivendi" note) get the same left-to-right
  marks as the rest of the Hebrew UI and keep their own order. English is unchanged.
- Hebrew UI preview (refs #543, #193): a value inserted into a Hebrew line keeps its own order. `UiText.Format` puts a
  left-to-right mark (U+200E) on each side of each value with no Hebrew in it that it inserts into a Hebrew template, so
  negative foreign currency reads "-$4.2m" (not "$4.2m-") and "15 May 1948" in the Why screen's turn line stays one
  run. English templates, and templates with no letter of their own, format as before.
- Keyboard play, small fixes from the accessibility audit (refs #41): a Budget + or - keeps the focus on its button, as
  the rows are now updated in place (H2). Closing the start screen, answering the last open event and Escape from a
  screen no longer leave nothing focused: the focus goes to End Turn, End Turn, and the screen's top-bar button (M1).
  Text on the keyboard focus fill is a warm near-white, 5.7:1 where the gold was 3.7:1 (M3). The lit escalation rung's
  turn line is ink, 6.4:1 where it was 3.3:1, and the plain rungs' a lighter muted, 6.3:1 where it was 5.0:1 (M4). The
  Cabinet's "Ordered:" line wraps, and on short windows the Prime Minister's portrait gives way, framed again for its new
  height, so the candidate list keeps two rows; at 32:9 the list's floor follows the room left (M7). Presentation only:
  no golden moves.
- Keyboard focus that lands on something scrolled out of view, such as a Cabinet card below the first row or a
  candidate far down the list, now scrolls its list to it (accessibility audit H1, refs #41). Focus that follows a mouse
  press leaves the list where it is. Presentation only: no golden moves.
- Wheel zoom in the web build: one small wheel delta, such as a touchpad's half pixel, made every later frame zoom 27%,
  since Unity's player takes the smallest delta it has seen as one notch. The camera now counts the browser's wheel
  events itself (`WheelInput.jslib`): a touchpad's small pixel deltas zoom by how far the fingers move (100 px to a
  notch), and a mouse notch zooms 10% whatever its size: any pixel event of 50 px or more, a macOS mouse tick, three
  lines or one page is one notch. The listener asks for the mode first so that Firefox keeps a mouse notch in lines
  (refs #43).
- "The Mediator Assassinated" cites the Official Gazette text of the Prevention of Terrorism Ordinance for its number,
  No. 33 of 1948, which no source on the card gave. Comment and source row only (refs #442).
- Five recorded contradictions settled from primary sources. "The Second Truce": the Acting Mediator's report of
  18 October 1948 (S/1042) puts the Negev fighting on 15 October, against S/1357's 14 October. "Talks at Rhodes":
  Israel's reply to the British protest (JTA, 19 January 1949) agrees with the British statement that the RAF planes
  were shot down on 7 January. "Jerusalem": the Mediator's report of 12 July (S/888) has the demilitarisation proposal
  made in early July, and Shertok read Israel's rejection to the Provisional State Council on 29 July, before the
  proclamation of 2 August. "Electing the Constituent Assembly": the minutes' Hebrew date confirms the 25th sitting on
  4 November 1948 (the Knesset's database has a Saturday, 6 November), and CBS's figures give turnout 86.88%, so 86.9%,
  not 87.0%. "The Mediator Assassinated": the Official Gazette dates the ordinance 23 September and prints it on
  29 September. Comments and source rows only; text, dates, options and effects are unchanged (refs #442).
- Four 1948 localities corrected against the Village Statistics 1945 scan (#200): Ijzim is 2,970, not 4,280 (the
  transcription's "Ijzim (Nazareth subdistrict)" is 'Ilut, a different village); Ilar's Wikidata item is the place's
  main item, "Illar, Tulkarm"; Beit Nuba's row reads 'Ajanjul; Zakariya's article is now Zekharia. The rows stay
  `owner_verify`. The map's localities are outside the content hash, so no golden moves.
- "The Altalena" also cites the Provisional State Council's sixth sitting, 24 June 1948: the council approved the
  government's action against the Irgun's arms ship 24 to 4, with 5 abstaining, and a committee to secure a single army.
  Source rows and comments only (refs #442).
- "Transit Camps" opens on 1 April 1950, not 1 March: Eshkol raised the camps' crisis at the Jewish Agency executive
  on 27 March 1950 and proposed transit housing two weeks later (Kachansky, Yad Ben-Zvi 1987). Wikipedia's March date
  is kept in the event's comment. In the golden run the card first shows on 27 May 1950 with either date, so no golden
  moves (refs #442).
- A player's budget before the First Knesset now goes to the Provisional State Council's vote (`budget_approval: true`,
  refs #3), the same mechanic every Knesset already uses. The council fixed the budget by ordinance under the Law and
  Administration Ordinance, and adopted the January 1949 Budget Ordinance unanimously and the January-March 1949
  ordinance 21-1, from the protocols in `research_notes/budget-approval-1948-56.md`. The `budget_approval` open questions
  on the council and the First and Second Knessets are closed, each with its protocol sources. The Third Knesset's stays
  open: its 1956/57 law is read, its 1957-59 laws are not. No golden moves: the historical runs and the scenarios submit
  no budget.
- The Provisional State Council's window opens on 15 May 1948, the end of the Mandate, as the Declaration of
  Independence (clause 12) and the Knesset's dates table give it, not 14 May (refs #457). The game starts on 15 May, so
  nothing in play changes and no golden moves.
- The Rhodes talks card no longer says Israel accepted the cease-fire on 7 January: by 6 January both Egypt and Israel
  had notified the Acting Mediator of their acceptance (S/1187, FRUS 1949 vol. VI doc. 362), and the cease-fire took
  effect at 1200 GMT on 7 January (refs #457). Text only; no golden moves.
- Yehuda Leib Maimon holds Religions and War Victims to 8 October 1951, not 14 February 1951: the Knesset position
  records run both Second Government terms to the Third Government, and the plenum of 17 July 1951 has him answering as
  Minister of Religions (refs #457). The two offices are no longer vacant from February 1951. No golden moves.
- Six 1948-1950 cards cite more primary sources in place of Wikipedia. "The Second Truce" drops Wikipedia: the UN's
  published text of resolution 54 gives the 7-1-3 vote, and the IDF accounts and the Knesset's War of Independence
  summary date the truce from 19:00 on 18 July to Operation Yoav on 15 October. "Operation Yoav" drops the "1948
  Arab-Israeli War" article, as the Knesset summary dates Beersheba to 21 October. "The Ten Days" adds the IDF account
  of Operation Danny, the Knesset summary and the Palmach archive's pages for Allon and Rabin. "Rationing Clothes and
  Shoes" cites the JTA's reports of the merchants' strike in place of the "1950 in Israel" article. "Talks at Rhodes"
  adds Hansard for the RAF clash of 7 January 1949, and "Education in the Immigrant Camps" adds the JTA's report of
  the commission's findings, completed on 9 May 1950. Wikipedia stays only for lines these sources do not cover. Text,
  dates and effects are unchanged (refs #442).
- Second Government ministers take their Second Knesset factions from 20 August 1951, per the Knesset faction records:
  Haim-Moshe Shapira Hapoel HaMizrachi and Yitzhak-Meir Levin Agudat Yisrael (both United Religious Front before), and
  Bechor-Shalom Sheetrit Mapai (Sephardim before). Each term is split on that date (refs #457). No golden moves.
- The transitional Second Government holds 69 of 120 seats in the Second Knesset, not 51 (#315). From 20 August 1951
  its coalition gains the lists its parties' MKs sat for in the new Knesset, per the Knesset faction records: Hapoel
  HaMizrachi, Mizrachi, Agudat Yisrael and Poalei Agudat Yisrael (the United Religious Front) and the Democratic List
  for Israeli Arabs (the Democratic List of Nazareth). It has a majority again, and the player can re-appoint Shapira
  and Levin, whose parties follow those factions. No golden moves: the historical run holds no vote and makes no
  appointment, so the coalition is not read into its state.
- Contradictions flagged in #457 and #442 that cannot move a golden. YAML comments: the Provisional State Council last
  met on 10 February 1949, not 3 February; the 120-seat ordinance passed on 18 November 1948 and the 25 January vote
  was fixed on 25 November 1948 (Harpaz, Kesher 61); the 57-36 vote of 8 August 1950 was for the coalition's motion,
  not against a no-confidence motion. The Frumkin inquiry option now moves camp education from the Culture Department
  to the Education Department, both already in the Education Ministry. Moshe Sharett's birth date is 16 October 1894,
  from the Knesset member pages (Wikipedia gave 15 October); the sim reads `born` only to validate content. The items
  that would move a golden wait for the GDD rebaseline (#315).
- The browser build names the files under `Build/` by their content hash (Unity's Name Files As Hashes), so the names
  change with every build and a browser that cached an earlier build can no longer mix its loader, code or data into a
  newer one (the startup failure "InternalError: too much recursion" without a hard refresh). Found by @Chaiky3 (#437,
  reported in #43). `tools/package_release.py` now zips exactly the `Build/` files the build's `index.html` names, and
  refuses a folder that is missing one.
- The browser page itself can still come from the browser's cache after an update (a server that sends no
  Cache-Control, such as `python3 -m http.server`, allows it), and it then named the previous build's files, which are
  gone: the start failed with "Unknown data format". The page now asks the server for its current copy and loads the
  files that copy names, keeping its own names only when it cannot (offline, `file://`). Refs #437.
- The files the browser build fetches from StreamingAssets (the content, the heightmap, the event clips and the start
  screen's recording) keep fixed
  names, so a returning browser could pair new code with the previous build's cached content. Every such URL now
  carries the build's id (`?b=<Application.buildGUID>`), so each build fetches its own copies; desktop file paths are
  unchanged. Refs #437.
- The Provisional Government, the first three governments, the Provisional State Council and the First and Second
  Knessets now cite the Knesset's records: its position, faction and session data, the plenum minutes of each opening
  sitting and confidence vote, the 1951 transition and election-day laws, its make-up pages for the First and Second
  Knessets, and the Official Gazette notice of the 1949 results. Wikipedia stays only for the council's party split, the
  Provisional Government's smaller coalition groups and lines the YAML comments cite it for. Values are unchanged
  (refs #457).
- The Hebrew names and birth and death dates of the people in office from 1948 to 1952 now cite the Knesset's person
  records and member pages (Internet Archive copies) and, for Weizmann and Shazar, the President's Office pages, in
  place of Wikipedia. Wikipedia stays only where those records give no value or a different one. No value changed
  (refs #457).
- The art checkpoint's close checks (`hero06judea`, `hero06negev`) are shot with the settlement trees off, as the haze
  already was: one tree's crown had filled `hero06judea`, so it no longer showed the ground it is there to check
  (audit 2026-10-13). The game itself is unchanged.
- Settlement names keep clear of the unit tokens in normal play (refs #197). The Tel Aviv name no longer sits on the
  Kiryati Brigade's badge and figures at the gameplay zoom: a token standing in a town counts as part of it, so the
  name hangs just above the brigade, and a name keeps clear of any other token as it does of a panel. Presentation
  only: no golden moves.
- The Unity build has no compiler warnings again. The panel trim's corner ornament no longer uses
  `MeshWriteData.uvRegion`, which Unity 6000.3 marks obsolete (CS0618); the renderer now maps texture coordinates itself.
  The pixels are unchanged.
- "Operation Yoav" now dates the attack on the supply convoy to 15 October 1948, not 14 October, as Israel's own reply in
  the Acting Mediator's report S/1042 and the IDF account of the operation both give it. The rest of the card is
  unchanged (refs #442).
- The office terms of the people in office from 1948 to early 1952 (the Provisional Government, the first three
  governments, President Weizmann and the deputy ministers) now cite the Knesset's position records and, for the
  presidency, the Knesset sittings that elected and mourned Weizmann, in place of Wikipedia. Wikipedia stays for
  Ben-Gurion's 1951 Education term, Maimon's Second Government terms, Shapira's and Levin's United Religious Front
  party in the Second Government, Lavon's rationing term and the Chiefs of Staff. No value changed (refs #457).
- Six society events (austerity, clothing rationing, the transit-camp schools, the Law of Return, the transit camps and
  closing the Ministry of Rationing) now cite the Knesset records: plenum minutes for 1949-1951, the Law of Return as
  published in Sefer HaChukim, and the Knesset position data. Wikipedia stays only for lines no Knesset record covers.
  Text, dates and effects are unchanged (refs #442).
- Four more cards cite primary and scholarly sources in place of Wikipedia. "Operation Hiram" adds the Acting
  Mediator's report S/1071 (the Arab Liberation Army's positions near Manara from the night of 21-22 October; its
  refusal to withdraw on 28 October), Ben-Gurion's diary for 22-31 October 1948, the Ministry of Defense's Izkor page on
  Sheikh Abd and the Knesset's War of Independence summary. "Operation Yoav" adds Ben-Gurion's diary for the cabinet
  vote of 6 October 1948. "Transit Camps" adds the Central Zionist Archives and Miriam Kachansky's Yad Ben-Zvi study
  (Eshkol's proposal; Kesalon in May 1950). "Closing the Ministry of Rationing" drops Wikipedia: the Knesset's review of
  the First Knesset records the crisis in Mapai. The comment on "Austerity" now says the rationing plan was presented to
  the Knesset on 26 April 1949, already decided by the cabinet, citing the plenum and the JTA. Wikipedia stays only for
  lines these sources do not cover. Text, dates and effects are unchanged (refs #442).
- "The Altalena" cites the Provisional State Council's minutes of 23 June 1948 (Ben-Gurion's statement and reply, also
  on Project Ben-Yehuda), the Ministry of Defense's Izkor page on the Altalena monument and the Ministry of Education's
  page on Rabin in place of the "1948 in Israel" article. The source comments record that Ben-Gurion told the council
  there was no agreement to give the Irgun part of the arms for Jerusalem, against the Irgun's account that he agreed.
  "Altalena Affair" stays for the dispersal of the Irgun units in the IDF. Text, dates, options and effects are
  unchanged (refs #442).
- Seven more 1948-1950 cards cite primary sources. "Electing the Constituent Assembly" drops both Wikipedia articles:
  the Provisional State Council's minutes of its 25th sitting (the 16 to 14 vote for 120 seats) and of 18 November 1948
  (the ordinance adopted: proportional, one district), Harpaz for the curfew, and the CBS table from the Central Elections
  Committee for the 86.9% turnout. "The First Truce" cites Resolution 50 and S/834 in place of the Resolution 50 article;
  "One Army" cites the Knesset's law record and the council's minutes of 23 June 1948 in place of "1948 in Israel";
  "Jerusalem" cites the Mediator's progress report and the JTA in place of "Dov Yosef", and its comment records that the
  report dates the demilitarisation proposal to 3 July against 26 July in the JVL. "The Ten Days", "The Mediator
  Assassinated" (the council's minutes of 23 September 1948, the JTA, the Knesset's law record) and "Education in the
  Immigrant Camps" (the Knesset of 17 January 1950, the JTA) add sources and keep Wikipedia for the lines they do not
  cover; the commission's first session on 25 January still rests on Hebrew Wikipedia, against the JTA's hearings from
  2 February. Text, dates, options and effects are unchanged (refs #442).
- Four cards from 1948 and 1951 cite primary sources. "Operation Horev" drops its Wikipedia article for the IDF
  Archives' page on the operation (22 December to 7 January, Allon, five brigades), Security Council resolution 66 and
  the Egyptian-Israeli cease-fire agreement (S/1225). "A Claim on Germany" drops its Wikipedia article for Sharett's
  statement to the Knesset of 13 March 1951, which reads the note into the record, FRUS 1951 and the JTA; FRUS and the
  Knesset date the note to 12 March. "Israel Bonds" adds the Knesset's vote of 27 February 1951, the law record, the JTA
  and Israel Bonds' own history, and keeps Wikipedia for the 25 million dollar goal; its comment records that the JTA
  has the sale starting across the United States before the 10 May rally. "Foreign Currency Runs Out" adds FRUS and the
  JTA and keeps Wikipedia for the banks' and oil companies' refusal of credit. Text, dates, options and effects are
  unchanged (refs #442).
- "The Mediator Assassinated" drops two Wikipedia articles ("Folke Bernadotte" and "Prevention of Terror Ordinance")
  for Bunche's cable of 17 September 1948 (S/1004, the murder in Katamon) and Israel's report to the Security Council
  (S/1315: the regulations of 20 September, the ordinance of 23 September, the leaders' arrest on 29 September); its
  comment records that S/1315 has 82 arrests outside Jerusalem against Ben-Gurion's 62 in the Tel Aviv district.
  "Talks at Rhodes" adds the Acting Mediator's report of 6 January 1949 (S/1187), "Operation Yoav" the IDF History
  Department's study of the Hebron hills, "Education in the Immigrant Camps" Levitan's study of the Frumkin
  commission, "Foreign Currency Runs Out" FRUS 1951, "The Jews of Iraq" the JTA on the freeze of March 1951, and
  "Austerity" a comment on the Knesset sitting of 26 April 1949; each keeps Wikipedia for the lines these do not cover.
  Text, dates, options and effects are unchanged (refs #442).
- The four armistice cards cite the signed texts (Avalon Project) and the Acting Mediator's cables to the Security
  Council (S/1264, S/1296, S/1302, S/1353) in place of Wikipedia. "Electing the Constituent Assembly" adds Harpaz's
  study in Kesher 61, the CBS registration date, the IDI results page and the Official Gazette's notice of the results.
  "Education in the Immigrant Camps" cites the Knesset debates of 31 January and 28 February 1950 and keeps only the
  Hebrew Wikipedia article, for the commission's first session and report dates. Text, dates and effects are unchanged
  (refs #442).
- Six war cards ("The Altalena", "The Ten Days", "The Second Truce", "Operation Yoav", "Operation Hiram", "Talks at
  Rhodes") now cite primary sources beside Wikipedia: Security Council resolution 54, the Mediator's reports (A/648,
  S/1042, S/1055, S/1357), FRUS 1948 and 1949, UN press releases, the JTA archive and the Irgun's own history of the
  Altalena. "Talks at Rhodes" drops its Hebrew Wikipedia source, which FRUS now covers. Text, dates and effects are
  unchanged (refs #442).
- The info panel's region line ran out of the panel and over the map; it now wraps inside the panel at every window size
  (fixes #397).
- Map: kit orchard trees no longer grow through the unit figures' boots and bases. The trees under each figure drawn now
  are left out at draw time and come back when the figure goes (a later date, or the "units" toggle off, so plain
  screenshot checkpoints keep every tree); the kits' layout does not change (fixes #249).
- "The Jews of Iraq" cites two primary sources for its asset freeze and its count of those waiting to fly: the Israeli
  letter to the Palestine Conciliation Commission of 19 March 1951 and FRUS 1951, vol. V, doc. 340. Its English Wikipedia
  source puts the first flights a year late (#135) but stays, because it alone covers the transit camps' crowding. Text,
  dates and effects are unchanged (refs #135).
- In the two-row HUD (a window narrower than about square), a button in the event column or the info panel hidden under an
  open screen could still take the keyboard focus once the 1950 build buttons were live. The covered panels are now
  disabled as well as hidden, also when the window narrows under an open screen, and a panel hidden by the start screen
  or a wide screen is not re-enabled by a tier change or a screen closing under it (#400).
- The map's far tilt-shift band no longer blurs the far towns into a smear at the play zoom (#390). Since the map drew
  faster (#320), the quality fallback no longer turns the tilt-shift off, and the far band showed in full. On the owner's
  ruling it is now gently soft at the gameplay zoom, as on promo 03, and in full only at the overview. Presentation only.
- The map draws about four times faster at the gameplay zoom (#195). The water's shading moved out of the terrain's shader
  into its own pass over the water and the shore, and the region borders read one texel a pixel instead of up to seven.
  Since the promo sea (#138) and the borders (#206) the terrain's shader had grown large enough to slow every terrain pixel
  in the browser, even with the water and the borders toggled off. The look is unchanged. Presentation only.
- The four General Zionist ministers (Rokach, Bernstein, Sapir, Serlin) leave office on 25 May 1953 and return on 3
  June 1953, and the Prime Minister holds their ministries in between, as he told the Knesset on 25 May. The
  Knesset's position records show no break; the plenum record of the resignation and of the new declarations of
  allegiance is used. The goldens do not move (fixes #314).
- Eliezer Kaplan's Trade and Industry term and Haim-Moshe Shapira's Health term in the First Government start on 22
  March 1949, not 10 March: on 22 March the Prime Minister told the Knesset the government had assigned them, and the
  Knesset's position records give the same date. The goldens do not move (fixes #310).
- The start screen's painting no longer shows sunbeams: on 14 May 1948 the hall's windows were darkened for fear of an
  air raid, so the new painting shows them blacked out and the hall lamplit. It is a native 2048x1152 render, no longer
  a 1536x864 upscale, and a window narrower than 16:9 now crops both sides of it alike (fixes #268).
- The Unity idle test `PromoScreensTests.AnOpenScreenIsStillWhenIdle` no longer fails now and then (#242). It was not a
  font atlas and not a recurring layout. A batch-mode editor runs few frames, and now and then it tells
  `UIDocument` late that the test's GameObject exists. `UIDocument` then re-adds its root and the whole screen lays out
  once more. The test now queues a player-loop update on every step, waits for that editor notification plus 5 frames,
  and then allows no relayout or repaint in the next 60 frames (before: at most one, in the first 60 steps).
- The Ministry of Rationing and Supply, and Dov Yosef as its minister, now start on 10 March 1949, when the First
  Government took office, not on 26 April 1949 (the day rationing was decided, which the austerity event keeps). The
  Knesset's 8 March 1949 First Government document names him. Agriculture is unchanged. The goldens do not move; the
  content hash does, so an older save loads with the "different game content" warning (#225).
- Railways keep their width outside the gorges: a rail band narrows only where the ground leaves its tolerance on both
  sides (a gorge floor, a gorge side, a ridge), not for one hill or one drop beside the line. The Jaffa–Jerusalem line
  keeps 94.9% of its width outside the Sorek (87.4% before) and the Jezreel Valley line 92.1% outside its gorges (82.8%).
  Map only; the goldens do not move (#252).
- Railways and roads on the map no longer zigzag on straight stretches (the traced lines are smoothed, keeping real
  bends), and the Jaffa–Jerusalem railway no longer climbs the Sorek gorge's walls: the track keeps its own grade,
  flat across, narrows smoothly to the gorge floor and stays clear of the ground under its rails (#212, #213).
- Place names no longer cover the buildings of small settlements such as Degania Alef and Zikhron Ya'akov (#214). A name
  now sits above its town's buildings, or beside or below them when that space is taken, and covers at most a tenth of
  any town. With the "kits" visual toggle off, names keep clear of the marker boxes instead. Presentation only.
- The web build's quality fallback no longer lowers the quality while the page is hidden (#216). A background tab or a
  minimised window (and in Chromium a window covered by others) runs at a few frames a second, which had counted as
  load. Presentation only.
- The border determinism test no longer fails now and then on a pixel or two: it shows the proof pattern once, renders a
  warm-up frame, and compiles shaders synchronously, so both compared frames draw the same font atlas and shaders.
- The 1948 Cabinet no longer shows four vacant seats for offices that did not exist yet. Offices can now carry a `from`
  date, and before it the office is not a Cabinet seat and cannot be filled. The dates are Education and Culture 10 March
  1949, rationing 26 April 1949, President of Israel 16 February 1949 and Minister without Portfolio 8 October 1951. The
  Budget row for Education reads "no ministry until 10 March 1949" (#217).
- The Defence screen's UN ceasefire clock now reads "If full war goes on, the UN imposes a ceasefire in N weeks". The
  old "A UN ceasefire will be imposed in N weeks" contradicted the First Truce, which keeps the game at full war (#218).
- The camera keys (WASD and the arrows) no longer move keyboard focus through the HUD. Before, panning could walk focus
  onto New Game or End Turn, so a stray Enter or Space could press it. Tab still moves focus.
- The Cabinet's "Appoint to …" heading has its own column again and stays clear of the portrait grid's scroll bar.
- The "Not done" toast is opaque and sits clear of the open screen.
- "International Standing" keeps to one line on a full-width bar.
- The open sea no longer shows a blocky checker up close (about 6-20 km): its waves now come from smooth gradient noise
  on rotated lattices instead of value noise differenced along the map's axes. Presentation only.
- Panning past the map's edge no longer fills the view with flat land and a hard seam: the camera stops where the view
  meets the edge of the map, and at close zoom over high ground (the Golan, the Galilee hills) it no longer sinks into
  the hills. At the overview, where the view is wider than the map, it is centred east to west (#174).
