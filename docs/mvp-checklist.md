# MVP master checklist

Current state on `main` at `767bfe93a`, 28 October 2026. Part of #374. A completed prerequisite does not complete its dependent task.

The design of record is [the GDD](GDD.md), especially "MVP scope 1948 to 1951". The implementation is split across the seven approved phase plans in `docs/superpowers/plans/`. **Done** means the requirement is on `main`; **open** cites its open issue, and an open pull request for it is named beside it; **blocked on owner decision** cites both the issue and the design question that prevents implementation.

## Owner gates (blocking the critical path)

- [x] **Done: `docs/rules/mvp.md`, R-ARMS-001 to R-ARMS-003.** The four owner-approved Arms values and the national pool are on `main` (#1047; fixes #781).
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-4.md`.** The Phase 4 rulings (#782, then Q-CAMP-2's camp sites, Q-SWAMP-3's Hula clearance and the Kiryat Shmona and Beit Shemesh sign-off) are recorded in the plan's status line and decision rows.
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-6.md`.** The Phase 6 rulings, including the target list and the sixteen final owner picks, are recorded (#1060, #1069). Every card still needs a person's source check and sensitivity review before it ships.
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-7.md`.** Q-DEF-4 and Q-OPEN-3 through Q-OPEN-5 are recorded with the other Phase 7 rulings (#1069).
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-5.md`.** Q-ART-2 and its selected artillery and armoured-car models are recorded on `main`, and so is Q-ART-3, the owner as the unit-art reviewer, with Tal Heuberger's models counted as reviewed (#997).
- [x] **Done: `ASSETS.yaml`.** The `first_new_yishuv` photograph's row is verified (CHANGELOG: "Its `ASSETS.yaml` row became `verified` in #913").
- [x] **Done: #382 ([owner sign-off, 17 Oct](https://github.com/eyalgolan/statecraft-israel/issues/382#issuecomment-6231566027)).** The cutover reviews (the re-timed event windows and the `DesignValuesTests` list) are signed off.
- [x] **Done: `docs/rules/mvp.md`, R-MONEY-002.** Civilian places have no upkeep in the MVP and the per-head tax remains (#1044; fixes #884).
- [x] **Done: the v0.1.0 and v0.1.1 GitHub pre-releases.** Both are published, web only (#969, #993); #879 is closed. The MVP remains WebGL only; desktop players and IL2CPP parity are after the MVP (#814, #905).
- [x] **Done: #791 (owner, 18 Oct 2026, Q-CARD-1; [#791](https://github.com/eyalgolan/statecraft-israel/issues/791#issuecomment-6252484778)).** The Burma Road card's photograph: the 7 December 1948 Road of Valour opening frame (Rothenberg 997008137268105171) or the June-July 1948 track frame (Rothenberg 997008136513105171), and whether the card is flagged sensitive ([#791](https://github.com/eyalgolan/statecraft-israel/issues/791#issuecomment-6226235251)). The Burma Road card uses the Road of Valour opening frame of 7 December 1948 (Benno Rothenberg, Meitar Collection, CC BY 4.0, 997008137268105171), and the card is marked `sensitive`, because the opening was also a memorial for the fallen. It went through the owner's sensitivity review like every sensitive card: the owner approved the card's text on 21 Oct 2026, and `content/events/burma_road_complete.yaml` is `sensitivity-reviewed` (#1284).
- [x] **Done: the CBS scan verification note.** Alexago checked the twelve cities' census figures against the scans (#1126; #1005).

## Phase 1: the vertical slice

- [x] **Done: `docs/rules/mvp.md`, R-TURN-001.** PR 1 monthly turns (#349).
- [x] **Done: `docs/rules/mvp.md`, R-LOOP and R-SAVE.** PR 2 loop switch and save schema 2 (#364).
- [x] **Done: `docs/rules/mvp.md`, R-CITY-001 to R-CITY-005.** PR 3 one city (#461).
- [x] **Done: `docs/rules/mvp.md`, R-TRAIN-001 to R-TRAIN-003.** PR 4 training (#468).
- [x] **Done: `docs/rules/mvp.md`, R-SITE-001 to R-SITE-003.** PR 5 one designated site and Build yishuv (#566).
- [x] **Done: `docs/rules/mvp.md`, R-OBJ-001 to R-OBJ-003.** PR 6 objective card and score (#653).
- [x] **Done: CHANGELOG entry "Social cohesion on the top bar".** PR 7 Unity core: Social cohesion meter, card line and photograph (#831).
- [x] **Done: CHANGELOG entry "The GDD loop's city and site panels".** PR 8 Unity city panel, site button and site layer (#869; #381 closed).

## Phase 2: cutover and the resource layer

- [x] **Done: `docs/rules/mvp.md`, R-LOOP-004.** PR 1 and 1a: 44 monthly turns ending 15 January 1952 (#600).
- [x] **Done: `docs/rules/mvp.md`, R-MONEY-001 to R-MONEY-003.** PR 2 Money as the Treasury (#688, #684, #727); civilian-place upkeep and the per-head tax followed in #1044.
- [x] **Done: `docs/rules/mvp.md`, R-FOOD-001 to R-FOOD-003.** PR 3 Food pool (#712).
- [x] **Done: `docs/rules/mvp.md`, R-ARMS-001 to R-ARMS-003.** PR 3b Arms pool (#1047; fixes #781).
- [x] **Done: CHANGELOG entry "Social cohesion is now the `morale` indicator".** PR 4 Social cohesion and International Support (#857).
- [x] **Done: `docs/rules/mvp.md`, R-EFX-001 and R-EFX-002.** PR 5 effects on all five resources; Arms joined the rule with the pool (#1047), and the authoring guidance followed in #1166.
- [x] **Done: `docs/rules/mvp.md`, R-GOV-001 and R-GOV-002.** PR 6 cabinet, coalition and Attention (#830).
- [ ] **Open: #731.** PR 7 HUD for the five resources; the Social cohesion meter is on `main` (CHANGELOG, "Social cohesion on the top bar"), and Food and Arms show in the turn report, Why and Defence, which queues Arms purchases (#1790; the owner kept them off the top bar). The Defence and Cabinet leftovers remain.

## Phase 3: cities and absorption

- [x] **Done: CHANGELOG entry "The GDD game has its twelve cities".** PR 1 twelve cities' start values (#821).
- [x] **Done: CHANGELOG entry "The GDD game's immigrants arrive in all twelve cities".** PR 2 arrivals over the cities (#847).
- [x] **Done: CHANGELOG entry "The GDD game's cities house every arriving immigrant".** PR 3 housing capacity, condition and overflow (#855; `docs/rules/mvp.md`, R-HOUSE-001 to R-HOUSE-004).
- [x] **Done: `docs/rules/mvp.md`, R-PROJ-001 to R-PROJ-004.** PR 4 city projects: housing types and industry (#982), and the legacy map actions refused in the GDD loop (#735).
- [x] **Done: `docs/rules/mvp.md`, R-CRAFT-001, R-CRAFT-002 and R-CITY-009.** PR 5 the craftsman (#1049; #736).
- [x] **Done: `docs/rules/mvp.md`, R-PROJ-005, and `content/projects.yaml`.** PR 6 the winery. The local-resource rule is on `main` (#1025). **Decided (owner, 20 Oct 2026; #737):** the winery is in Rishon LeZion. Its content rows are the `winery` kind and Rishon LeZion's vineyards, flagged `to_verify` (#1198; #737 closed); the vineyards' source still waits for a person's check.
- [x] **Done: `docs/rules/mvp.md`, R-OBJ-004, and `content/objectives.yaml`.** PR 7 objective 2, absorb immigrants: the condition is on `main` (#1019), followed by the sourced objective and card (#1055; #1039, #738).
- [x] **Done: CHANGELOG entry "City projects in the info panel".** PR 8 Unity city panel at twelve cities: overflow and craftsmen (#1132), city projects in the info panel (#1150) and on the map (#1146), and the objective 2 card's arrival photograph (#1268); #739 closed (#1294).
- [x] **Done: `docs/rules/mvp.md`, R-MONEY-002.** No upkeep for civilian places in the MVP, with the per-head tax kept (#1044; fixes #884).

## Phase 4: pioneers, sites and corridors

- [ ] **Open: #785.** PR 1 designated sites. The four yishuv sites and their fields are on `main` (CHANGELOG, "The MVP has four designated yishuv sites"), and the two camp sites are on `main` (CHANGELOG, "Pioneers can build a training camp"); Kiryat Shmona and Beit Shemesh (#944) and the Hula (#968) are on `main` too, so all 16 site rows are; a person's check of the 15 `draft` rows remains in #785.
- [x] **Done: `docs/rules/mvp.md`, R-COR-001 to R-COR-003.** PR 2 corridors: Burma Road, Haifa-Tel Aviv rail link and Tel Aviv-Netanya road, built segment by segment (#870; #786).
- [x] **Done: CHANGELOG entry "Pioneers can build a training camp".** PR 3 build a camp or Hakhshara (#927; `docs/rules/mvp.md`, R-CAMP-001 and R-CAMP-002).
- [x] **Done: CHANGELOG entry "every plowed field adds 1 unit of Food".** PR 4 plow fields (#871; `docs/rules/mvp.md`, R-FIELD-001 and R-FIELD-002).
- [x] **Done: CHANGELOG entry "Pioneers can drain the Hula swamp".** PR 5 dry swamp at the Hula (#968, fixes #789; `docs/rules/mvp.md`, R-SWAMP-001 and R-SWAMP-002).
- [x] **Done: CHANGELOG entry "A halutz group becomes a veteran pioneer".** PR 6 veteran pioneer (#868; `docs/rules/mvp.md`, R-VET-001 and R-VET-002).
- [x] **Done: `docs/rules/mvp.md`, R-OBJ-005 and R-OBJ-006, and `content/objectives.yaml`.** PR 7 objective 3, key infrastructure, in three parts with three cards (#1226; #791 closed); the Burma Road card's photograph and its sensitivity flag are **decided (owner, 18 Oct 2026, Q-CARD-1; [#791](https://github.com/eyalgolan/statecraft-israel/issues/791#issuecomment-6252484778)):** the Road of Valour opening frame of 7 December 1948, and `sensitive`; the card is `sensitivity-reviewed` (#1284).
- [ ] **Open: #792.** PR 8 Unity corridors, kits and pioneer orders. The camp, field and Hula orders from the map (#1133), built corridor segments (#1312), camp and rural kits (#1316) and corridor build orders (#1320) are on `main`; phase close and the Roadmap remain.

## Phase 5: military units, movement and combat

- [x] **Done: CHANGELOG entry "The Arab forces of 15 May and 11 June 1948".** PR 1 Arab-forces data rows (#860).
- [x] **Done: `docs/rules/mvp.md`, R-UNIT-001 and R-UNIT-002.** PR 2 military unit record and types (#1024; #741 closed).
- [x] **Done: `docs/rules/mvp.md`, R-UNIT-003.** PR 3 recruits become units (#1130 merged; #742 closed).
- [x] **Done: `docs/rules/mvp.md`, R-UNIT-006 and R-UNIT-007.** PR 4 sourced starting forces (#1186, on #1176's strengths; #743 closed). The seven IDF rows take Ben-Gurion's 15 May 1948 figures (#1276), and the owner person-checked all twelve rows (#1278; #1240 closed). Brigades that enter after 15 May arrive on their sourced date (#1272, R-UNIT-009); Etzioni, the 7th and Yiftach as units are #1241, and the local forces are #1243.
- [x] **Done: `docs/rules/mvp.md`, R-MOVE-001 to R-MOVE-006.** PR 5 movement (#1073; fixes #744).
- [x] **Done: `docs/rules/mvp.md`, R-MOVE-007.** PR 6 roads and rail in movement (#1193; #745 closed).
- [x] **Done: `docs/rules/mvp.md`, R-ORDER-001 to R-ORDER-003.** PR 7 fortify and hold (#1071; fixes #746).
- [x] **Done: `docs/rules/mvp.md`, R-COMBAT-001 to R-COMBAT-006.** PR 8 combat, with its Arms cost (#1194; #747 closed).
- [x] **Done: `docs/rules/mvp.md`, R-COMBAT-007, R-COMBAT-008 and R-UNIT-008.** PR 9 artillery and the armoured car: combat (#1245) and training at a military camp (#1346; #748 closed).
- [ ] **Open: #749.** PR 10 enemy AI. Part 1, the scripted positions and the truce guard, is on `main` (#1263; R-ENEMY-001 to R-ENEMY-004 and R-ENEMY-006); part 2, R-ENEMY-005, the operation cards' orders, remains.
- [x] **Done: `docs/rules/mvp.md`, R-WAR-001 to R-WAR-004.** PR 11 war state, call-up retirement and truces (#1148, #1310, #1339 merged; #750 closed).
- [ ] **Open: #751.** PR 12 prisoners. The mechanism is on `main` (#1264, #1271; R-POW-001 to R-POW-005); the four sourced exchange cards remain.
- [ ] **Open: #752.** PR 13 objective 1, the war and its goldens.
- [x] **Done: `UnitRoster.FromState`.** PR 14 Unity roster from the state, figures by Q-UI-3 (#1205; #753 closed).
- [x] **Done: the Unity unit card with orders and sim previews.** PR 15 Move (#1265), fortify and hold (#1269), the `smoke-orders` golden (#1289), attack and its odds (#1277), and keyboard orders (#1347); #754 closed.
- [x] **Done: the Unity Defence screen.** PR 16 war state, truce line and prisoners in the GDD loop (#1493; #755 closed).
- [ ] **Open: #756.** PR 17 art for the new units; Q-ART-2 and Q-ART-3 are recorded in `docs/superpowers/plans/2026-10-15-gdd-phase-5.md`.
- [ ] **Open: #757.** PR 18 close: WebGL frame budget and playtest.

## Phase 6: settlement control

PRs 1 to 12 have their owner answers recorded in the Phase 6 plan (#1060, #1069).

- [x] **Done: `docs/rules/mvp.md`, R-CTRL-001.** PR 1 target-list research (#977, #1046, #1299, #1333; #793 closed). The later residents fields returned the six target rows to `draft`; their source checks, sensitivity review and release gates remain with #801, #802 and #804.
- [x] **Done: `docs/rules/mvp.md`, R-EFX-007 and R-GATE-001.** PR 2 `displacement` tag and release gate (#1070; #794).
- [x] **Done: `docs/rules/mvp.md`, R-CTRL-001 to R-CTRL-003.** PR 3 target list and settlement state (#1077; #795).
- [x] **Done: `docs/rules/mvp.md`, R-CTRL-004 and R-CTRL-005.** PR 4 capture, resolver step 3 (#1239; #796 closed). Enemy captures of the player's localities, R-CTRL-006, remain #1305; player units entering enemy-held targets are done (#1314 merged; #1304 closed).
- [x] **Done: `docs/rules/mvp.md`, R-CTRL-007 to R-CTRL-010.** PR 5 `ResolveCapture`, the options helper, and Raid and withdraw (#1257; #797 closed).
- [x] **Done: `docs/rules/mvp.md`, R-CTRL-011 to R-CTRL-013.** PR 6 Occupy, the occupation timer and withdrawal (#1324 merged; #798 closed).
- [x] **Done: `docs/rules/mvp.md`, R-CTRL-014 to R-CTRL-017.** PR 7 annex or withdraw (Q-PUP-1: no puppet regime in the MVP; #1526 merged; #799 closed).
- [ ] **Open: #800.** PR 8 Deport, honest cost, no reward.
- [ ] **Open: #801.** PR 9 displacement cards.
- [ ] **Open: #802.** PR 10 Unity capture and end-of-occupation dialogs are on `main` (#1317 and #1572 merged); reviewed Deport activation remains.
- [ ] **Open: #803.** PR 11 Unity settlement states on the map.
- [ ] **Open: #804.** PR 12 close.
- [x] **Done: `docs/gdd/settlement-control.md`.** The final section records the owner's Phase 6 rulings (#1045; #532).

## Phase 7: events, ministers and the end

- [x] **Done: CHANGELOG entry "The GDD loop's event rules have IDs and tests".** PR 1 event rules in the GDD loop (#898; `docs/rules/mvp.md`, R-EVT-001 to R-EVT-005).
- [x] **Done: `docs/rules/mvp.md`, R-EFX-003 to R-EFX-006 and R-EFX-008.** PR 2 event effects on units, cities and projects; project unlock completed the row (#1072; #806).
- [ ] **Open: #807.** PR 3 the MVP event set.
- [x] **Done: `docs/rules/mvp.md`, R-MIN-001 to R-MIN-003.** PR 4 ministers on the GDD systems (#1147; #808).
- [x] **Done: `docs/rules/mvp.md`, R-END-001 to R-END-003.** PR 5 defeat. The economy half (#989) and the war loss, `defeat_war` (#1246), are on `main`; #8 is closed. Q-DEF-4 is decided and recorded (#1069); the Hebrew goes to native-speaker review, and the history note beside each cause goes with #812 (the war note's research is done: #1315 merged; #1306 closed).
- [ ] **Open: #809.** PR 6 every objective card. The objectives screen is in the side menu, with status and dates and no score (owner, 20 Oct 2026; #1202, #1189 closed).
- [x] **Done: `docs/rules/mvp.md`, R-END-005 and R-END-006.** PR 7 the simulation end report (#1149; #810).
- [ ] **Open: #811.** PR 8 Unity event-card effects.
- [ ] **Open: #812.** PR 9 Unity end screen. The end screen shows the end report (#1156); the war loss's line and the history notes remain. Q-END-3 and Q-DEF-4 are decided and recorded (#997, #1069); the Hebrew defeat text goes to native-speaker review.
- [ ] **Open: #530.** PR 10 opening cutscene. Q-OPEN-3 through Q-OPEN-5 are decided and recorded (#1069).
- [x] **Done: the first-turn guide.** PR 11 onboarding for the GDD loop: a five-step guide, skippable and re-openable (#1209; #42 closed), with follow-ups (#1225, #1234, #1286).
- [ ] **Open: #813.** PR 12 balance pass; the target is a 60 to 90 minute session (below), and it also decides the IL 250m debt ceiling and the loyalty floor of 5 (owner, 18 Oct 2026).
- [ ] **Open: #814.** PR 13 MVP release: `mvp-end`, `smoke-mvp`, WebGL and GitHub pre-release. The MVP ships WebGL only; desktop players and IL2CPP parity move to #905 after the MVP.
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-3.md`, `-phase-5.md` and `-phase-6.md`.** The plans contain the 17 October rulings (#918).

## Player feedback on v0.1.1 (#1038)

The first outside play-test (18 October). The plan of record for these is #1038.

- [x] **Done: the unaffordable-card message.** A decision card with no Attention left says that End turn hands it to the minister (#1053; fixes #1034).
- [x] **Done: the side-menu close control.** Every side-menu screen has an X button (#1027; #1035).
- [x] **Done: the Settings language choice.** English and Hebrew apply at once and persist (#1058; #1036).
- [x] **Done: the turn report and top-bar explanations.** They show changes and their main causes after End turn (#1074; #1037).
- [x] **Done: the short How to play screen.** It was the bridge until #42 landed (#1059; #1041).

## Critical path

1. **Open: #731.** The Arms pool, five-resource effects and the Food and Arms display are done (#1047, #1790); the Defence and Cabinet leftovers remain.
2. **Done: Phase 3.** City projects, the winery and the city panel are done (#1255, #1198, #1294).
3. **Open: #785.** Finish the site reviews, then #792; corridors and objective 3 are done (#870, #1226).
4. **Open: #749, #751, #752 and #756, then #757.** Recruits, artillery acquisition, war state, unit orders and the Defence screen are done (#1130, #1346, #1148, #1310, #1277, #1493).
5. **Open: #800, #801, #802 and #803, then #804.** Capture, Raid, Occupy and annex or withdraw are done (#1239, #1257, #1324, #1526); source checks, sensitivity review, Deport activation and phase close remain.
6. **Open: #807 and #809.** Defeat, the event effects, ministers and simulation end report are done (#1246, #1072, #1147, #1149); the implementation path ends at the WebGL-only release in #814.

## Release before the MVP: v0.1.0

- [x] **Done: v0.1.0 (#969) and v0.1.1 (#993).** `docs/RELEASING.md` was run for both web pre-releases. D-P and D-R are ticked in `docs/oss/DECISIONS.md`.

## Parallel track: "looks like demo2"

The art and map issues under #18 remain a parallel track, not a substitute for the phase work above.

- [ ] **Open: #1183, pull request #1796.** Denser hexes: Tel Aviv to Jerusalem in 7 to 8 hexes. The measured plan is on `main` (#1185); the 4.4 km bake is in the open pull request.
- [ ] **Open: #663, #707, #666 and #667.** Remaining model placement and art work. #659, #665, #708 and #886 are closed; the last Collection figures, including Tal Heuberger's Arab villager, are in play (#1321). #669, #670 and #725 are closed (#1086, #1022, #1028). The Burma Road vehicles' approximate end boundary (#1040) is implemented in [#1067](https://github.com/eyalgolan/statecraft-israel/pull/1067): the convoy truck and lorry from #1028 stop on 7 December 1948; both `burma_road` vehicle plans use that exclusive end date. `RoadVehiclesTests.BurmaRoadVehiclesStopOn7DecemberWhileOtherRoutesContinue` checks 6 and 7 December. The route itself has no new end date.
- [x] **Done: the CHANGELOG entry for the nine whole-town models (0.1.0).** The nine whole-town landmarks (#917). #885 is closed into #708.
- [x] **Done: #1004.** The building models stood far bigger than the train, and Jaffa's over Tel Aviv's (v0.1.1). #1030 puts every building model at 24x real (owner: "Merge, then fix landmarks + grids"); the kit landmarks and plot grids followed in #1048 (#1032), and townspeople and wildlife at the same scale in #1050 (#1033).
- [x] **Done: `NOTICE` and `ASSETS.yaml`.** The artist credit is present (`NOTICE:12`), and The 1948 Collection's `ASSETS.yaml` artist is Tal Heuberger (#895; the credit confirmed by the owner on 18 October in [#887](https://github.com/eyalgolan/statecraft-israel/issues/887#issuecomment-6249500690), the full name since #1078); #887 is closed.

## Follow-ups

- [x] **Done: CHANGELOG entry "the map's name pills show each row's `name_he`".** Map name pills use `name_he` (#897). The tier badge sits on the right in Hebrew (#940), with its test across the size matrix (#964); the WebGL `?lang=he` check passed and #880 is closed.
- [x] **Done: CHANGELOG entry "The info panel now translates every map-action refusal".** Hebrew map-action reasons (#924, fixes #881).
- [x] **Done: CHANGELOG entry "The 50 people names in Hebrew are flagged agent-checked".** People-row review flags (#907, fixes #882).
- [x] **Done: #388.** The owner accepted the current sea and coast look ("Accept the current look", 18 Oct 2026); #388 is closed.

## Open design calls outside the phase checklist

- [x] **Done: `docs/GDD.md`, "Terrain type".** The eight `hexes.json` biomes are the complete MVP terrain catalogue, with no terrain conversions; projects change project or site state. New types, overlays and conversions are deferred beyond the MVP.
- [x] **Done: `docs/GDD.md`, "MVP completion rules" (owner, 18 Oct 2026, "60-90 minutes"; [#813](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6249771314)).** The session's target play time, 60 to 90 minutes, is recorded in the GDD and the Phase 7 plan (#1254; #1242 closed); #813 tunes to it. **Then changed (owner, 30 Oct 2026, "120 (data wins)"; [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164)):** the target is 120 minutes, matching `gdd03-balance.yaml`.

## GDD v0.2 additions outside the phase plans

The owner approved these on 17 October on #856 (the location roster and the map editor: [B1-B7](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6230935027); Nir Am and Umm Rashrash: [the four further additions](https://github.com/eyalgolan/statecraft-israel/pull/856#issuecomment-6233524779)); no phase plan covers them yet.

- [ ] **Open: #1006.** Nir Am's infrastructure-protection objective is decided: keep the Negev water line in Israeli hands until 24 February 1949, with a sourced card and balance-pass points. The research draft is on `main` (#1161); implementation remains, blocked by enemy captures (#1305).
- [ ] **Open: #1007.** Umm Rashrash is decided as a bonus inside objective 1, with the Ink Flag card dated 10 March 1949 and balance-pass points. The research draft is on `main` (#1159); the map record is claimed, and the bonus waits on #752.
- [ ] **Open: #1008, pull requests #1924 and #1926.** The GDD v0.2 must-have location rows and English and Hebrew labels are on `main` (#1057); the remaining source checks and a person's factual review remain.
- [ ] **Open: #1009.** The Unity editor-window design is decided. The override schema, loader and application order are on `main` as step 1 (#1099), and the editor window as step 2 (#1266); a person's check in a headed editor remains.

## After the MVP

The eras before 1948 and after 1951, M4 1952–1956 work, diplomacy, desktop players and IL2CPP parity (#905), Steam/store work, historian review, Hebrew content text, cut states for existing routes and 1952–1956 settlements remain outside the MVP scope set by `docs/GDD.md`. **Then decided (owner, 18 Oct 2026, "Everything now"; [#55](https://github.com/eyalgolan/statecraft-israel/issues/55#issuecomment-6254311071)):** Hebrew versions of all content text come now, with native-speaker review before they count as reviewed and sources retained for sourced names.
