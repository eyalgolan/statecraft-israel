# MVP master checklist

Current state on `main` at `55780c2a`, 18 October 2026. Part of #374.

The design of record is [the GDD](GDD.md), especially "MVP scope 1948 to 1951". The implementation is split across the seven approved phase plans in `docs/superpowers/plans/`. **Done** means the requirement is on `main`; **open** cites its open issue, and an open pull request for it is named beside it; **blocked on owner decision** cites both the issue and the design question that prevents implementation.

## Owner gates (blocking the critical path)

- [ ] **Blocked on owner decision: #781.** Arms start stock and unit, price, Arms per recruit and Attention cost are the owner questions on #781. [The GDD](GDD.md#resources-and-national-parameters) decides only where Arms come from (D-G6).
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-4.md`.** The Phase 4 rulings (#782, then Q-CAMP-2's camp sites, Q-SWAMP-3's Hula clearance and the Kiryat Shmona and Beit Shemesh sign-off) are recorded in the plan's status line and decision rows.
- [ ] **Blocked on owner decision: #783.** The 37 questions marked `OWNER PICK` in `docs/superpowers/plans/2026-10-15-gdd-phase-6.md` (Q-TGT-1 to Q-TGT-3, Q-DEP-1 to Q-DEP-7 and the rest) are unanswered; agents draft, the owner edits.
- [ ] **Blocked on owner decision: #784.** Q-DEF-2, Q-DEF-4, Q-MIN-2, Q-NEW-3, Q-END-3 and Q-OPEN-3 through Q-OPEN-5 remain owner picks in `docs/superpowers/plans/2026-10-15-gdd-phase-7.md`.
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-5.md`.** Q-ART-2 and its selected artillery and armoured-car models are recorded on `main`; Q-ART-3 remains part of open implementation issue #756.
- [x] **Done: `ASSETS.yaml`.** The `first_new_yishuv` photograph's row is verified (CHANGELOG: "Its `ASSETS.yaml` row became `verified` in #913").
- [x] **Done: #382 ([owner sign-off, 17 Oct](https://github.com/eyalgolan/statecraft-israel/issues/382#issuecomment-6231566027)).** The cutover reviews (the re-timed event windows and the `DesignValuesTests` list) are signed off.
- [ ] **Blocked on owner decision: #884.** Money upkeep and the per-head tax await the Q-MONEY-2 choices stated in #884.
- [ ] **Blocked on owner decision: #879.** Publishing v0.1.0 awaits the owner's release call (version PR #969); the release procedure is `docs/RELEASING.md`.
- [ ] **Open, no issue.** The human check of the twelve cities' census figures against the CBS scans. The owner ruled "merge now and check later" ([#821](https://github.com/eyalgolan/statecraft-israel/pull/821#issuecomment-6228845606)); the ruling left it on #569, which is a merged pull request, so nothing open tracks it now.

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
- [x] **Done: `docs/rules/mvp.md`, R-MONEY-001 and R-MONEY-003.** PR 2 Money as the Treasury (#688, #684, #727). R-MONEY-002 is separately blocked on #884.
- [x] **Done: `docs/rules/mvp.md`, R-FOOD-001 to R-FOOD-003.** PR 3 Food pool (#712).
- [ ] **Blocked on owner decision: #781.** PR 3b Arms pool, R-ARMS-001 to R-ARMS-003.
- [x] **Done: CHANGELOG entry "Social cohesion is now the `morale` indicator".** PR 4 Social cohesion and International Support (#857).
- [ ] **Open: #729.** PR 5 effects on all five resources. Food and the no-gain rule are on `main` (`docs/rules/mvp.md`, R-EFX-001 and R-EFX-002); Arms waits on #781.
- [x] **Done: `docs/rules/mvp.md`, R-GOV-001 and R-GOV-002.** PR 6 cabinet, coalition and Attention (#830).
- [ ] **Open: #731.** PR 7 HUD for the five resources; the Social cohesion meter is on `main` (CHANGELOG, "Social cohesion on the top bar"), while Food and Arms remain.

## Phase 3: cities and absorption

- [x] **Done: CHANGELOG entry "The GDD game has its twelve cities".** PR 1 twelve cities' start values (#821).
- [x] **Done: CHANGELOG entry "The GDD game's immigrants arrive in all twelve cities".** PR 2 arrivals over the cities (#847).
- [x] **Done: CHANGELOG entry "The GDD game's cities house every arriving immigrant".** PR 3 housing capacity, condition and overflow (#855; `docs/rules/mvp.md`, R-HOUSE-001 to R-HOUSE-004).
- [ ] **Open: #735.** PR 4 city projects.
- [ ] **Open: #736.** PR 5 the craftsman.
- [ ] **Open: #737.** PR 6 the winery; its dated 1948–1951 vineyard source and city remain part of that issue.
- [ ] **Open: #738.** PR 7 objective 2, absorb immigrants.
- [ ] **Open: #739.** PR 8 Unity city panel at twelve cities.
- [ ] **Blocked on owner decision: #884.** Money upkeep and the per-head tax after the twelve cities.

## Phase 4: pioneers, sites and corridors

- [ ] **Open: #785.** PR 1 designated sites. The four yishuv sites and their fields are on `main` (CHANGELOG, "The MVP has four designated yishuv sites"), and the two camp sites are on `main` (CHANGELOG, "Pioneers can build a training camp"); the remaining Hula, Kiryat Shmona and Beit Shemesh site work stays in #785 (Kiryat Shmona and Beit Shemesh: PR #944).
- [ ] **Open: #786.** PR 2 corridors: Burma Road, Haifa–Tel Aviv rail link and Tel Aviv–Netanya road (PR #870).
- [x] **Done: CHANGELOG entry "Pioneers can build a training camp".** PR 3 build a camp or Hakhshara (#927; `docs/rules/mvp.md`, R-CAMP-001 and R-CAMP-002).
- [x] **Done: CHANGELOG entry "every plowed field adds 1 unit of Food".** PR 4 plow fields (#871; `docs/rules/mvp.md`, R-FIELD-001 and R-FIELD-002).
- [ ] **Open: #789.** PR 5 dry swamp at the Hula (PR #968); Q-SWAMP-3 is decided, so it is not owner-gated.
- [x] **Done: CHANGELOG entry "A halutz group becomes a veteran pioneer".** PR 6 veteran pioneer (#868; `docs/rules/mvp.md`, R-VET-001 and R-VET-002).
- [ ] **Open: #791.** PR 7 objective 3, key infrastructure, three cards.
- [ ] **Open: #792.** PR 8 Unity corridors, kits and pioneer orders.

## Phase 5: military units, movement and combat

- [x] **Done: CHANGELOG entry "The Arab forces of 15 May and 11 June 1948".** PR 1 Arab-forces data rows (#860).
- [ ] **Open: #741.** PR 2 military unit record and types.
- [ ] **Open: #742.** PR 3 recruits become units; the Arms cost is blocked on #781.
- [ ] **Open: #743.** PR 4 sourced starting forces (research: PR #906; the force count is still open).
- [ ] **Open: #744.** PR 5 movement.
- [ ] **Open: #745.** PR 6 roads and rail in movement; depends on #786.
- [ ] **Open: #746.** PR 7 fortify and hold.
- [ ] **Open: #747.** PR 8 combat; Arms per attack is blocked on #781.
- [ ] **Open: #748.** PR 9 artillery and the armoured car.
- [ ] **Open: #749.** PR 10 enemy AI.
- [ ] **Open: #750.** PR 11 war state, call-up retirement and truces.
- [ ] **Open: #751.** PR 12 prisoners.
- [ ] **Open: #752.** PR 13 objective 1, the war and its goldens.
- [ ] **Open: #753.** PR 14 Unity roster from the state.
- [ ] **Open: #754.** PR 15 Unity unit card with orders.
- [ ] **Open: #755.** PR 16 Unity Defence screen.
- [ ] **Open: #756.** PR 17 art for the new units; Q-ART-2 is recorded in `docs/superpowers/plans/2026-10-15-gdd-phase-5.md`.
- [ ] **Open: #757.** PR 18 close: WebGL frame budget and playtest.

## Phase 6: settlement control

The phase is **blocked on owner decision: #783**. The unanswered questions are marked `OWNER PICK` in `docs/superpowers/plans/2026-10-15-gdd-phase-6.md`.

- [ ] **Open: #793.** PR 1 target-list research (PR #977).
- [ ] **Open: #794.** PR 2 `displacement` tag and release gate.
- [ ] **Open: #795.** PR 3 target list and settlement state.
- [ ] **Open: #796.** PR 4 capture.
- [ ] **Open: #797.** PR 5 Raid and withdraw.
- [ ] **Open: #798.** PR 6 Occupy.
- [ ] **Open: #799.** PR 7 puppet or annex.
- [ ] **Open: #800.** PR 8 Deport, honest cost, no reward.
- [ ] **Open: #801.** PR 9 displacement cards.
- [ ] **Open: #802.** PR 10 Unity capture dialogs.
- [ ] **Open: #803.** PR 11 Unity settlement states on the map.
- [ ] **Open: #804.** PR 12 close.
- [ ] **Open: #532.** `docs/gdd/settlement-control.md`; its final section waits on #783.

## Phase 7: events, ministers and the end

- [x] **Done: CHANGELOG entry "The GDD loop's event rules have IDs and tests".** PR 1 event rules in the GDD loop (#898; `docs/rules/mvp.md`, R-EVT-001 to R-EVT-005).
- [ ] **Open: #806.** PR 2 event effects on units, cities and projects. Unit grants plus housing-capacity and job-slot effects are on `main` (CHANGELOG, "Event options in the GDD loop can grant units"; `docs/rules/mvp.md`, R-EFX-003 to R-EFX-006); condition and project unlock remain.
- [ ] **Open: #807.** PR 3 the MVP event set.
- [ ] **Open: #808.** PR 4 ministers on the GDD systems; Q-MIN-2 is blocked on owner decision #784.
- [ ] **Open: #8.** PR 5 defeat; Q-DEF-2 and Q-DEF-4 are blocked on owner decision #784.
- [ ] **Open: #809.** PR 6 every objective card and score parts.
- [ ] **Open: #810.** PR 7 end report.
- [ ] **Open: #811.** PR 8 Unity event-card effects.
- [ ] **Open: #812.** PR 9 Unity end screen; Q-END-3 is blocked on owner decision #784.
- [ ] **Open: #530.** PR 10 opening cutscene; Q-OPEN-3 through Q-OPEN-5 are blocked on owner decision #784.
- [ ] **Open: #42.** PR 11 onboarding for the GDD loop.
- [ ] **Open: #813.** PR 12 balance pass.
- [ ] **Open: #814.** PR 13 MVP release: `mvp-end`, `smoke-mvp`, WebGL and macOS IL2CPP parity and GitHub pre-release.
- [x] **Done: `docs/superpowers/plans/2026-10-15-gdd-phase-3.md`, `-phase-5.md` and `-phase-6.md`.** The plans contain the 17 October rulings (#918).

## Critical path

1. **Blocked on owner decision: #781.** Decide Arms, then finish open Phase 2 issues #729 and #731.
2. **Open: #735.** Phase 3 PR 3 is done; city projects are next, followed by #737 and #739, with #736 and #738 alongside.
3. **Open: #785 and #786.** Finish Phase 4 sites and corridors, then #791 and #792; #789 remains alongside.
4. **Open: #741.** Build the Phase 5 sequence through #757; #742 and #747 also depend on owner decision #781.
5. **Blocked on owner decision: #783.** Phase 6 implementation starts after the owner-pick rows and Phase 5.
6. **Blocked on owner decision: #784.** Phase 7's remaining owner picks gate #808, #8, #812 and #530; the implementation path ends at #814.

## Release before the MVP: v0.1.0

- [ ] **Blocked on owner decision: #879.** Run `docs/RELEASING.md` steps 1–7 and publish after the owner's release call. D-P and D-R are already ticked in `docs/oss/DECISIONS.md`.

## Parallel track: "looks like demo2"

The art and map issues under #18 remain a parallel track, not a substitute for the phase work above.

- [ ] **Open: #659, #707, #665, #666, #667, #725, #669, #670, #708, #886.** Remaining model placement and art work (#659: draft PR #700).
- [x] **Done: CHANGELOG entry "MATKOT's nine whole-town models".** The nine whole-town landmarks (#917). #885 stays open.
- [x] **Done: `NOTICE`.** The MATKOT credit is present (`NOTICE:12`). #887 stays open for the owner's follow-up: whether the `ASSETS.yaml` artist field also names MATKOT.

## Follow-ups

- [x] **Done: CHANGELOG entry "the map's name pills show each row's `name_he`".** Map name pills use `name_he` (#897). #880 stays open for the tier badge's place in the Hebrew pill.
- [x] **Done: CHANGELOG entry "The info panel now translates every map-action refusal".** Hebrew map-action reasons (#924, fixes #881).
- [x] **Done: CHANGELOG entry "The 50 people names in Hebrew are flagged agent-checked".** People-row review flags (#907, fixes #882).
- [ ] **Blocked on owner decision: #388.** The owner may accept or re-bar the palette distance (diorama02 reads 0.57 against the issue's 0.56 bar).

## Open design calls outside the phase checklist

- [ ] **Blocked on owner decision: `docs/GDD.md`, "Terrain type" and "Map editor tool".** The GDD v0.2 additions leave new terrain types, the editor host, authoring format and editable-property list TBD. These do not block the current MVP movement rules.
- [ ] **Blocked on owner decision: `docs/GDD.md`, "MVP completion rules".** The session's target play time is open, asked of Amir on #856.

## After the MVP

The eras before 1948 and after 1951, M4 1952–1956 work, diplomacy, Steam/store work, historian review, Hebrew content text, cut states for existing routes and 1952–1956 settlements remain outside the MVP scope set by `docs/GDD.md`.
