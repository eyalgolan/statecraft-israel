# Enemy move playback after End turn (#2202) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After End turn, the map plays the enemy's moves and attacks one beat at a time, in the sim's order. The camera glides to each beat and back to the player's view. A Skip button, Esc and a Settings switch control it, and it never holds up input.

**Architecture:** The sim already names every enemy order and attack on its causes (`Cause.EnemyOrder`, R-ENEMY-001, #1293, #1297). This plan adds one read-only field pair, the hex a named unit stood on (`UnitRef.Q`, `UnitRef.R`). Then Unity needs no state from before the step. A pure planner turns the turn's enemy causes into beats. A driver, ticked by `GameRoot.Update`, plays the beats through #2176's token motion and #2177's attack hook. Two small additions to #2176's `UnitLayer` let it hold a token at its old place and release it. Presentation only. No rule moves into Unity, and nothing is saved.

**Tech Stack:** C# netstandard2.1 sim with xunit (`dotnet test sim/Statecraft.sln`), and Unity 6000.3.25f1 with UI Toolkit and NUnit EditMode (`scripts/unity_compile/compile.sh`, full EditMode in en_US and he_IL).

**Spec:** issue [#2202](https://github.com/eyalgolan/statecraft-israel/issues/2202), a player report the owner relayed on 1 November 2026. It has no comments. Built on PR #2176 (tokens slide, figures walk; refs #1866) and PR #2177 (figures fire; refs #1867), both open. #2177 is stacked on #2176. Read against `origin/main` at `a304ac65a`, #2176 at `871586d23` and #2177 at `de90957db`.

**Status (2 November 2026):** this is a plan only. The owner has not answered the questions below. Phase 1 (the sim) can start now. Phases 2 and 3 start after #2176 and #2177 merge.

## Global Constraints

- "Game rules live in the sim, not in Unity. Unity draws, reads input and calls sim helpers. A rule written in a presenter is a bug." (`CONTRIBUTING.md`, rule 1.) The planner only orders and groups the sim's own causes. It never decides what the enemy did.
- Determinism (`docs/determinism.md`): the sim change adds no RNG, no state and no hashed field. `GoldenRun.Hash1950`, `Hash1952` and the browser smoke value must not move. Every task's checks include `dotnet test sim/Statecraft.sln` with 0 warnings.
- From #2070: "the game never waits on an animation to accept input". The playback is never modal. Clicks, orders, screens and End turn all work while it plays.
- GDD [Movement preview ruling](../../GDD.md): "Enemy units get no arrow (#2134)". The playback draws no move arrow and no path for an enemy unit.
- GDD map: "The camera has a fixed viewing direction and pitch, supports pan and dolly in/out, and does not orbit." The glide changes only `CameraRig.LookAt`. It never changes `Yaw` or `Distance`.
- New strings follow [docs/WRITING_UI.md](../../WRITING_UI.md), [docs/WRITING_UI_HE.md](../../WRITING_UI_HE.md) and [docs/UX_GLOSSARY.md](../../UX_GLOSSARY.md) (binding since 22 October 2026, #622). Each new key goes into `strings.en.yaml` and `strings.he.yaml` in the same commit. The Hebrew is marked `native-review-pending (#564)` in a YAML comment.
- Unity runs only through `uv run tools/machine_lock.py --shared --wait-unity -- ...` with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (`CONTRIBUTING.md`, rule 3). EditMode is a local gate: paste its totals in each Unity PR.
- Any change under `unity/` or `sim/` gets a changelog fragment, `changelog.d/2202-enemy-move-playback.md`. A visible change needs matched before/after captures, and a short clip because this is motion (`CONTRIBUTING.md`, "Pull requests").
- Write each test first and see it fail (`AGENTS.md`). The EditMode red run goes in the PR body.

## Review Focus

1. **A turn whose enemy unit moves and is then destroyed in the same turn.** After the rebuild it has no token. A player expects the playback to go on, not to stall or throw. The move beat is dropped, and its engagement beat still plays. Pinned by Task 5's `ADestroyedMoversBeatIsSkippedAndThePlaybackGoesOn`.
2. **The player pans, zooms or uses an advisor's "show me" during the playback.** The player expects the camera to stay where they put it. From then on the playback stops steering the camera and does not "return". Pinned by Task 4's `APlayerMoveHandsTheCameraBack` and Task 5's `APlayerPanMidPlaybackIsNotUndone`.
3. **End turn pressed again, New Game, or a language switch while a playback runs.** The player expects the old playback to end at once (tokens at their places, camera back), and only the new turn's moves to play. Nothing may be doubled or left held. Pinned by Task 5's `EndTurnMidPlaybackFinishesTheOldOneFirst`, `NewGameMidPlaybackDropsIt` and `DisposingMidPlaybackReleasesEveryTokenAndReturnsTheCamera`.
4. **An enemy attack at End turn while the playback is on.** The player expects the fire take to play once, on its own beat. It must not also play at once from #2177's `CombatResolved` handler. With the playback off, #2177's behaviour must stay exactly as it is. Pinned by Task 5's `AClaimedEngagementPlaysOnItsBeatNotAtOnce` and `WithThePlaybackOffAttacksPlayAsBefore`.
5. **Hebrew (right to left) and phone width.** The Skip bar must mirror, stay inside a 360 px wide screen and never hide End turn. Pinned by Task 6's `TheSkipBarFitsAndMirrorsInHebrew` and by the he_IL EditMode run.

---

## What the sim exposes today (verified at `a304ac65a`)

| What | Where | Fields |
|---|---|---|
| Enemy orders, resolver step 1, in ascending unit id | `EnemyAI.Order`, called by `TurnResolver.Resolve` (`sim/Statecraft.Sim/TurnResolver.cs:19`) | |
| An enemy move | `Movement.Move(..., enemy: true)` (`Movement.cs:216-240`) | `EnemyOrder == Movement.Kind` ("move"), `Key == WhyKeys.UnitMoved`, `Target` = unit id, `Units[0]` = the unit *before* the move (`new UnitRef(u)`), `Args[0]` = destination `"q,r"`, `Args[1]` = cost |
| An enemy fortify | `Orders.Give(..., enemy: true)` (`Orders.cs:58-66`) | `EnemyOrder == Orders.Fortify`. Nothing moves. |
| An enemy standing attack order | `Combat.Give` (`Combat.cs:127-135`) | `Key == WhyKeys.AttackOrdered` only. It is fought in step 2. |
| Engagements, resolver step 2, in ascending attacker id | `Combat.Resolve` and then `Engage` (`Combat.cs:231`, `:290-333`) | `EnemyOrder == Combat.Attack` only when the attackers are the enemy's. `Key == WhyKeys.Engagement`, `Target` and `Args[0]` = the hex, `Units` = attackers then defenders, `Args[4]` = attacker count, `EnemyAttackers` = ids. It is followed by `Destroyed`, `Retreated` (`Units[0]` = the unit before it retreats, `Args[0]` = destination) and surrender lines. |
| The player's instant attack | `Combat.Strike`, which runs inside `RunOrders` (`Combat.cs:140-156`) | Its engagement has `EnemyOrder == ""` (`Player(attackers[0])`). A player strike that makes an enemy unit retreat moves that unit before step 1. |
| The step's result | `StepResult { State, Why, Rejected }` (`Commands.cs:216`) and `GameSession.Last`, `Previous` (the turn-start state, before the open turn's orders: `GameSession.cs:14, 63`) | |
| Bridge order at End turn (with #2177) | `SimBridge.EndTurn`: `Notify(Changed)` (map rebuilt; #2176 slides every moved token at once), then `CombatResolved(r.Why.Skip(Session.Now.Why.Count) engagements)`, then `TurnEnded` (outcome clips, report, autosave) | |

**The gap:** `UnitRef` has `Id`, `Type`, `Side` and `HomeCity`, but no hex (`sim/Statecraft.Sim/UnitLines.cs:10-21`). So a cause gives where a unit went, not where it came from. `GameSession.Previous` is wrong for an enemy unit that the player's instant strike pushed back earlier in the turn. Unity's pre-step `Session.Now` would be right, but only by inference. That is the class of coupling #2177's review flagged ("The replay skip depends on the sim's internal order", review comment 6562482511, item 1). So the sim records the hex itself (Task 1).

**What the playback does not need:** #2177's replay prefix (`Session.Now.Why.Count`). The beats come from causes labelled `EnemyOrder != ""`. Only resolver steps 1 and 2 write those, and the player's orders in `RunOrders` never do. Task 1 pins this with a sim test. That makes the playback immune to #2177 finding 1.

## Owner questions (Phase 0: answers needed before Phase 3)

Each question has a recommended default. The plan names where the default lands, so an answer changes one constant or one branch. AGENTS.md says to "leave to the maintainer ... anything that needs a design call". None of these is decided here.

1. **Q1, order against the turn report and the event card.** (a) The playback plays first. The turn report and any event card that opened at turn start wait until it ends or is skipped. Outcome clips still play before everything (Recommended). (b) The report opens at once and the playback runs beside it, above the report. (c) The report opens at once and the playback starts when it is closed. Default (a) lands in Task 6 (`TurnReportPresenter.Held`, `EventCardPresenter.HeldForPlayback`).
2. **Q2, which moves play.** (a) Every enemy move and attack, with the camera going to each one off screen (Recommended: the report says a move off screen is never seen). (b) Only moves in or near the current view. Others are only counted in the caption. (c) Only moves within N hexes of an Israeli unit or city. Default (a) lands in Task 2 (`EnemyTurnBeats.From`, no view filter).
3. **Q3, pacing.** Proposed: pan 0.3-0.8 s by distance, the 0.65 s slide from #2176 unchanged, 0.35 s dwell after a move, an attack held for its fire take (at most 1.5 s, at least 0.8 s), and a 12 s cap on the whole playback. A full first turn has 7 enemy units (`content/map/units_1948.yaml`: one each for ala, egypt, iraq, jordan, lebanon, syria), which is about 6-9 s. The session target is about 2 minutes a turn (#813). Accept these values, or give others. They land as constants in Task 2 (`EnemyTurnBeats.Timing`).
4. **Q4, grouping.** Proposed: consecutive moves whose destinations are within 3 hexes of the beat's first one share a beat, up to 4 units, and the camera goes to their centre. Beats past the 8th are played together in one last beat, with no camera move. Accept, or say "one unit a beat always". This lands in Task 2.
5. **Q5, zoom.** (a) Keep the player's zoom (Recommended, and the GDD camera never orbits). (b) Dolly in to a set distance for each beat and back out at the end.
6. **Q6, the switch.** (a) A Settings row, "Show the Arab forces' moves on the map", On or Off, default On (Recommended: #2202 asks for "a setting to turn it off"). (b) Also add the general reduced-motion switch that #2070 and #40 mention now. With it on, there would be no slides or glides: the camera would cut to each beat, and the caption would carry the move. The default is (a) only. The general switch stays with #2070's motion spec.
7. **Q7, the sim change.** (a) Add `UnitRef.Q` and `UnitRef.R`, the hex each named unit stood on (Task 1; Recommended. The issue says "Nothing new in `sim/`", but this adds no rule and no state). (b) No sim change: Unity takes the from-hex from `Session.Now.State` captured before the step. That is an inference like the one #2177's review flagged.
8. **Q8, the encyclopedia.** Add one sentence to `advisor.page.enemy_phase.text` (EN and HE) saying the map plays the moves and how to skip or turn them off? Proposed text is in "Strings". Yes or no.

---

## Data flow (with the Q1 to Q7 defaults)

```
End turn
 └ SimBridge.EndTurn
    ├ step → Session.Last = r
    ├ Stepped(r)                         NEW (Task 5), before Changed; never on a load, New Game or refused turn
    │   └ EnemyTurnPlayback.OnStepped: Finish() any old playback; beats = EnemyTurnBeats.From(r.Why, hexOf);
    │      UnitLayer.Hold(beats' unit ids); save the camera pose; card.HeldForPlayback = report.Held = true
    ├ Changed                            UnitLayer.Show → Rebuild: held ids stand at their old place (Task 3), others slide (#2176)
    ├ CombatResolved(engagements)        UnitAttackPresenter skips the ones playback.Claims (Task 5), plays the rest (#2177)
    └ TurnEnded                          OutcomePresenter clips first; then playback.Start() (now, or on the clips' close)
GameRoot.Update → playback.Tick(dt) → for each beat: CameraGlide to the focus (unless the player took the camera),
   UnitLayer.Release(ids) (slide 0.65 s, #2176) or UnitLayer.PlayAttack(id) (#2177) and release the retreaters, dwell
end / Skip / Esc → UnitLayer.ReleaseAll(snap: skip), glide (or cut, on skip) back to the saved view, Held = false
```

## File structure

| Path | Responsibility | Task |
|---|---|---|
| `sim/Statecraft.Sim/UnitLines.cs` | `UnitRef.Q`, `UnitRef.R` | 1 |
| `sim/Statecraft.Sim.Tests/EnemyPlaybackDataTests.cs` (new) | the sim facts the playback relies on | 1 |
| `unity/Assets/Scripts/UI/EnemyTurnBeats.cs` (new) | pure planner: causes to beats, grouping, timing, cap | 2 |
| `unity/Assets/Scripts/Map/Units/UnitTokenMotion.cs` (#2176) | `Hold(Vector3 offset)`, `Held`, `HeldOffset` | 3 |
| `unity/Assets/Scripts/Map/Units/UnitLayer.cs` (#2176) | `Hold`, `Release`, `ReleaseAll`, `Draws`, held set kept across rebuilds | 3 |
| `unity/Assets/Scripts/Map/CameraGlide.cs` (new) | pure: eased glide, in-view test, player-took-over test | 4 |
| `unity/Assets/Scripts/UI/SimBridge.cs` | `Stepped` event | 5 |
| `unity/Assets/Scripts/UI/EnemyTurnPlayback.cs` (new) | the driver: state machine, Tick, Skip, Finish, Dispose | 5 |
| `unity/Assets/Scripts/UI/UnitAttackPresenter.cs` (#2177) | optional `claimed` filter | 5 |
| `unity/Assets/Scripts/UI/GameRoot.cs` | own and wire the playback; Update tick; Escape chain | 5, 6 |
| `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/Statecraft.uss` | the Skip bar `enemy-playback` | 6 |
| `unity/Assets/Scripts/UI/TurnReportPresenter.cs`, `EventCardPresenter.cs` | `Held` / `HeldForPlayback` | 6 |
| `unity/Assets/Scripts/UI/SettingsPresenter.cs` | `EnemyMovesKey = "enemymoves"` row | 7 |
| `unity/Assets/UI/strings.en.yaml`, `strings.he.yaml` | the new keys | 6, 7 |
| `unity/Assets/Tests/EditMode/EnemyTurnBeatsTests.cs`, `UnitLayerHoldTests.cs`, `CameraGlideTests.cs`, `EnemyTurnPlaybackTests.cs`, `EnemyPlaybackUiTests.cs` (new); `SettingsScreenTests.cs`, `TurnReportTests.cs` | tests | 2-7 |

## Phases and PRs

| PR | Tasks | Needs Unity | Waits for |
|---|---|---|---|
| A: `sim: name the hex a unit stood on in its Why line (refs #2202)` | 1 | no (CI) | Q7 (a) |
| B: `Map: hold and release a token, camera glide, enemy beats (refs #2202)`. Nothing visible changes yet. | 2, 3, 4 | yes | #2176 and #2177 merged |
| C: `Map: play the enemy's moves after End turn (Fixes #2202)` | 5, 6, 7, 8 | yes | B; Q1-Q6, Q8 |

---

### Task 1: The hex a named unit stood on (sim; Q7 (a))

**Files:**
- Modify: `sim/Statecraft.Sim/UnitLines.cs:10-21`
- Test: `sim/Statecraft.Sim.Tests/EnemyPlaybackDataTests.cs` (new). It builds states the way `EnemyAITests.cs` and `UnitLinesTests.cs` do.

**Interfaces:**
- Produces: `UnitRef.Q`, `UnitRef.R` (`int`, set from `Unit.Q`, `Unit.R` in the existing constructor), documented as "the hex the unit stood on when the sim wrote the line: before the move for `unit_moved`, and before the retreat for `retreated`". No other file changes. The constructor signature stays the same, and `UnitRef` is neither hashed nor saved (checked: it is used only in `Cause.Units`).

- [ ] **Step 1: Write the failing tests**
  - `AnEnemyMoveLineNamesTheHexItLeftAndTheHexItReached`: on the Historical 1948 new game (`GoldenRun.Seed`), `Simulation.Step` with no commands. For every `w` with `w.EnemyOrder == Movement.Kind`, take `before = state.Units[w.Units[0].Id]`. Assert `(w.Units[0].Q, w.Units[0].R) == (before.Q, before.R)`, and that `w.Args[0]` equals the unit's hex in `r.State` unless a later `Retreated` or `Destroyed` line names it. Assert that at least one such line exists (a precondition, so the test cannot pass empty).
  - `AnEnemyMoveAfterTheirUnitRetreatedFromOurStrikeStartsWhereItRetreatedTo`: find, by running the sim, a first-turn `Attack` that makes an enemy unit retreat. Assert that the found fixture really retreats the unit. Then assert that the unit's move line has `Units[0]` at the retreat hex, not at its `Previous` hex. This is the case that `Previous` gets wrong. If no such fixture exists in the 1948 content, the test builds the state by hand, as `EnemyAITests` does.
  - `ARetreatLineNamesTheHexTheUnitLeft`: `Units[0]` is at the engagement hex, and `Args[0]` is where it went.
  - `NoPlayerOrderWritesAnEnemyCause`: commit a move and an attack (`Simulation.Preview`). Assert `preview.Why.All(w => w.EnemyOrder == "")`, and that the step's causes with `EnemyOrder != ""` all come after `preview.Why.Count`.
  - `EnemyMovesComeInAscendingUnitIdBeforeTheEngagements`: in `r.Why`, the move lines' unit ids are ascending, and every `EnemyOrder == Combat.Attack` line comes after the last move line.
- [ ] **Step 2: Run them to see them fail.** Run `dotnet test sim/Statecraft.sln --filter EnemyPlaybackDataTests`. Expected: a compile error, because `UnitRef` has no `Q`. The ordering tests pass once it builds. They pin today's behaviour.
- [ ] **Step 3: Add `Q` and `R` to `UnitRef`.**
- [ ] **Step 4: Run the full sim suite.** Run `dotnet test sim/Statecraft.sln`. Expected: all pass, 0 warnings, and the golden tests unchanged (the Why list is not in `GameState.Hash`).
- [ ] **Step 5: Commit** `sim: name the hex a unit stood on in its Why line (refs #2202)`, with the changelog fragment and the PR body's "implements" line citing R-ENEMY-001.

### Task 2: The beats (pure planner)

**Files:**
- Create: `unity/Assets/Scripts/UI/EnemyTurnBeats.cs`
- Test: `unity/Assets/Tests/EditMode/EnemyTurnBeatsTests.cs`

**Interfaces:**
- Consumes: `Cause`, `UnitRef.Q/R` (Task 1), `WhyKeys`, `Movement.Kind`, `Combat.Attack`, and `UnitAttackPresenter.Attackers(Cause)` (#2177, `internal static`). That parser stays the single reader of the engagement layout.
- Produces:
  - `public enum BeatKind { Move, Attack }`
  - `public sealed class Beat { public BeatKind Kind; public IReadOnlyList<int> Units; /* move: movers; attack: attackers */ public IReadOnlyList<int> Retreaters; public (int Q, int R) Focus; public IReadOnlyList<Cause> Causes; public bool Folded; public float Pan, Dwell; }`
  - `public static class EnemyTurnBeats { public static IReadOnlyList<Beat> From(IReadOnlyList<Cause> why); public static float Total(IReadOnlyList<Beat> beats); }`
  - `public static class Timing { PanMin = 0.3f, PanMax = 0.8f, PanPerHex = 0.05f, MoveDwell = 0.35f, AttackMin = 0.8f, AttackMax = 1.5f, Cap = 12f, MinPan = 0.25f, MinDwell = 0.1f; BatchRadiusHexes = 3, MaxUnitsPerBeat = 4, MaxBeats = 8 }` (Q3, Q4). The 0.65 s slide is `UnitTokenMotion.Duration`, read, not copied.
  - Rules: walk `why` in order. A `unit_moved` line with `EnemyOrder == Movement.Kind` is a move. It joins the previous beat when that beat is a move, has fewer than `MaxUnitsPerBeat` units, and the hex distance (`MapActions.Distance(q1, r1, q2, r2)`, public in the sim, `sim/Statecraft.Sim/MapActions.cs:42`) from the beat's first destination is at most `BatchRadiusHexes`. Otherwise it opens a beat. An `EnemyOrder == Combat.Attack` engagement opens an attack beat with its attackers. Following `Retreated` lines, up to the next engagement, add their unit to `Retreaters`. Fortify and attack-order lines make no beat (nothing moves on the map). A move's `Focus` is the mean of its destinations, rounded to a hex. An attack's is the engagement hex. Then apply the cap: if `Total > Cap`, scale every `Pan` and `Dwell` down to the minimums. Beats past `MaxBeats` become one `Folded` beat with no camera move.

- [ ] **Step 1: Write the failing tests**, on hand-built cause lists, plus one real turn:
  - `NoEnemyCausesMakeNoBeats` (a player-only list makes an empty list).
  - `MovesKeepTheSimsOrder` (ids 3, 1, 2 in the list come out as 3, 1, 2. The planner never sorts.)
  - `NearbyMovesShareABeatUpToFour` (five moves within 3 hexes make beats of 4 and 1. A sixth, 10 hexes away, opens its own.)
  - `AnEngagementIsItsOwnBeatAndTakesItsRetreaters`
  - `FortifyAndAttackOrdersMakeNoBeat`
  - `APlayerEngagementIsNotABeat` (`EnemyOrder == ""`)
  - `ALongTurnIsCappedAndFolded` (20 far-apart moves: `Total <= Cap`, exactly `MaxBeats` beats, and the last is `Folded` with every remaining unit)
  - `TheFirst1948TurnPlaysEveryEnemyMove`: on the real session, every `EnemyOrder == "move"` unit is in some beat exactly once.
- [ ] **Step 2: Run them to see them fail.** Run `-testFilter EnemyTurnBeatsTests`. Expected: the type is not found.
- [ ] **Step 3: Implement `EnemyTurnBeats.From`.**
- [ ] **Step 4: Run them to see them pass.**
- [ ] **Step 5: Commit** `Map: plan the enemy's turn as beats (refs #2202)`.

### Task 3: Hold and release a token (extends #2176)

**Files:**
- Modify: `unity/Assets/Scripts/Map/Units/UnitTokenMotion.cs` and `unity/Assets/Scripts/Map/Units/UnitLayer.cs` (the `Rebuild(bool slide)` and `BuildTokens(drawnAt, firing)` that #2176 adds)
- Test: `unity/Assets/Tests/EditMode/UnitLayerHoldTests.cs`. It builds its layer as #2176's `UnitTokenMotionTests` does, with `AnimateMoves = true`.

**Interfaces:**
- Produces:
  - `UnitTokenMotion.Hold(Vector3 offset)`: the token stands at `offset` (its old place), not `Moving`, with the component disabled and `Changed` raised so that the hit boxes and badge stand there (#2176's `FaceCamera` reads `b.parent.position`). `bool Held`, `Vector3 HeldOffset`. `Begin(HeldOffset)` releases it. A hidden token (`!activeInHierarchy`) is never held.
  - `UnitLayer.Hold(IEnumerable<int> simIds)`: adds to a held set. In `BuildTokens`, a held id with a `drawnAt` entry calls `Hold(was - centre)` instead of `Begin`. The set survives rebuilds: a rebuild from another order mid-playback re-holds from where the token is drawn. It is cleared by `Show(..., slide: false)` (New Game).
  - `bool UnitLayer.Release(int simId)`: removes the id and starts its slide. False when no token draws it.
  - `void UnitLayer.ReleaseAll(bool snap)`: releases every held id. With `snap`, tokens go straight to their places (skip).
  - `bool UnitLayer.Draws(int simId)`.

- [ ] **Step 1: Write the failing tests:**
  - `AHeldTokenStandsAtItsOldPlaceThroughARebuild`: show, move unit X in the state, `Hold([X])`, show. The token is at its old place. Show again for another unit's order: it is still there.
  - `ReleaseSlidesItFromThereLikeAnyMove`: after `Release(X)` and `Tick(Duration)`, it is at its place and `Moving` is false.
  - `ReleaseAllWithSnapPutsEveryHeldTokenAtItsPlaceAtOnce`
  - `AnUnheldMovedTokenStillSlidesAtOnce` (#2176's behaviour is unchanged for the Israeli units)
  - `MidHoldTheHitBoxIsAtTheOldPlace` (a click finds the token where it is drawn, as #2176's `MidSlideTheHitBoxesAreWhereTheTokenIsDrawn` does)
  - `ANewGameClearsTheHeldSet` (`Show(slide: false)`)
  - `AHiddenLayerHoldsNothing`
  - `ReleasingAnUndrawnUnitIsFalse`
- [ ] **Step 2: Run them to see them fail.** Expected: `Hold` is not found.
- [ ] **Step 3: Implement it.** Hold is a slide that has not started. Reuse `Begin` and `Snap`, with no second offset path.
- [ ] **Step 4: Run them, and #2176's `UnitTokenMotionTests` and `UnitsFromStateTests`, to see them pass.**
- [ ] **Step 5: Commit** `Map: a token can wait at its old place for its turn to move (refs #2202)`.

### Task 4: Camera glide (pure)

**Files:**
- Create: `unity/Assets/Scripts/Map/CameraGlide.cs`
- Test: `unity/Assets/Tests/EditMode/CameraGlideTests.cs`. It builds its rig as `FixedNorthCameraTests.cs` does.

**Interfaces:**
- Consumes: `CameraController.ViewFootprint(CameraRig, float groundTop)` and `CameraController.Step` (`unity/Assets/Scripts/Map/CameraController.cs:432`, `:503`), and `CameraController.Settle()` (`:331`). Settle holds the whole view on the map, which gives the pan limits for free.
- Produces:
  - `public static Vector3 CameraGlide.At(Vector3 from, Vector3 to, float t)`: smoothstep on `t` clamped to 0..1, in x and z. `y` comes from `from` (Step re-lifts it).
  - `public static float CameraGlide.Seconds(float metres, float hexMetres)`: `Mathf.Clamp(PanPerHex * metres / hexMetres, PanMin, PanMax)`.
  - `public static bool CameraGlide.InView(CameraRig rig, Vector3 point, float margin = 0.15f, float groundTop = ShadowFit.GroundY)`: inside the footprint, shrunk by `margin` of its size on each side.
  - `public static bool CameraGlide.TookOver(CameraRig rig, Vector3 lastSetLookAt, float lastSetDistance)`: the LookAt moved more than 1 m, or the Distance changed.

- [ ] **Step 1: Write the failing tests:**
  - `TheGlideStartsAndEndsWhereAsked`
  - `ThePointMidwayIsHalfway`
  - `APointInTheMiddleOfTheViewNeedsNoPan`
  - `APointOffScreenNeedsOne`
  - `APlayerMoveHandsTheCameraBack` (`TookOver` is true after a `CameraController.Step` with pan input, and false after a zero-input `Step`)
  - `AGlideTargetPastTheMapEdgeStopsWhereSettleHoldsIt`
  - `TheGlideNeverTurnsOrZooms` (`Yaw` and `Distance` are unchanged)
- [ ] **Step 2: Run them to see them fail.**
- [ ] **Step 3: Implement them.**
- [ ] **Step 4: Run them to see them pass**, then `compile.sh` and the full EditMode suite for PR B. Paste the en_US and he_IL totals against the nightly baseline.
- [ ] **Step 5: Commit** `Map: an eased camera glide that yields to the player (refs #2202)`, and open PR B.

### Task 5: The driver and the bridge event

**Files:**
- Modify: `unity/Assets/Scripts/UI/SimBridge.cs` (`EndTurn`, beside #2177's `replayed` line), `unity/Assets/Scripts/UI/UnitAttackPresenter.cs` and `unity/Assets/Scripts/UI/GameRoot.cs` (beside #2177's `new UnitAttackPresenter(...)` after `showUnits()`, and in `Update`)
- Create: `unity/Assets/Scripts/UI/EnemyTurnPlayback.cs`
- Test: `unity/Assets/Tests/EditMode/EnemyTurnPlaybackTests.cs`. It builds its `SimBridge` over a real session as #2177's `UnitAttackPresenterTests` does, with fakes for the layer and the camera.

**Interfaces:**
- Consumes: Tasks 2 to 4, `SimBridge.CombatResolved` (#2177), `OutcomePresenter.Showing` and its `closed` callback (`OutcomePresenter.cs:100-104`), `UnitLayer.PlayAttack(int)` (#2176).
- Produces:
  - `public event Action<StepResult>? SimBridge.Stepped`: raised in `EndTurn` after the step and before `Notify(Changed)`. Never on the failure path, `Replace` (New Game) or `Commit`. It is guarded like `Notify`.
  - `UnitAttackPresenter(SimBridge bridge, Func<int, bool> play, Func<Cause, bool>? claimed = null)`: an engagement for which `claimed` returns true is skipped. With null, nothing changes.
  - `public interface IPlaybackMap { void Hold(IEnumerable<int> ids); bool Release(int id); void ReleaseAll(bool snap); bool PlayAttack(int id); bool Draws(int id); bool UnitsShown { get; } Vector3? World(int q, int r); }`. GameRoot adapts `UnitLayer`, `VisualToggles` and `Content.HexAt` with `map.Frame.ToWorld`.
  - `public interface IPlaybackCamera { Vector3 LookAt { get; set; } float Distance { get; } bool InView(Vector3 p); void Settle(); }`. GameRoot adapts `CameraRig` with `CameraController.Settle`.
  - `public sealed class EnemyTurnPlayback : IDisposable`:
    - `EnemyTurnPlayback(SimBridge bridge, IPlaybackMap map, IPlaybackCamera camera, Func<bool> enabled, Func<bool> clipShowing)`
    - `bool Active`, `Beat? Current`, `int Index`, `int Count`
    - `event Action? Started, BeatChanged, Finished`
    - `bool Claims(Cause c)`: true for a cause in a beat that has not played yet.
    - `void Start()`: GameRoot calls it from `TurnEnded` and from the outcome clips' `closed`. It begins only when it has beats and no clip shows.
    - `void Tick(float dt)`
    - `bool Skip()`: true when one was playing. It snaps every held token, puts the camera back at once unless the player took over, and raises `Finished`.
    - `void Finish()`: the same, without the return glide.
    - `Dispose()` = `Finish()` and unsubscribe.
    - It is a plain class ticked by `GameRoot.Update`. It does not run as a coroutine, so EditMode tests drive it with `Tick`.
  - `enabled` in GameRoot is `Application.isPlaying && !smokeRun && !UnitLayer.HiddenInCaptures(...) && setting == "on"` (Task 7). It is never on in a smoke run, a capture or EditMode unless a test passes `() => true`. That keeps `GameRoot.Smoke`, the first-card check and promo captures unchanged.
  - Per beat: a pan (skipped if `InView`, and skipped for good once `TookOver`), then `Release` of each mover, or `PlayAttack` of each attacker and then `Release` of the retreaters, then the dwell. A beat whose units all fail `Draws` is skipped (a unit destroyed after it moved). At the end, glide back to the saved `LookAt` over `CameraGlide.Seconds`, unless the player took over. With `UnitsShown == false` at `Stepped`, there is no playback (nothing to see).

- [ ] **Step 1: Write the failing tests** (fakes record calls in order):
  - `TheBridgeRaisesSteppedThenChangedThenCombatResolvedThenTurnEnded`
  - `SteppedIsNotRaisedOnARefusedTurnOrANewGame`
  - `ThePlaybackVisitsEveryEnemyMoveInTheSimsOrder`: on the real first turn, the `Release` ids equal the `EnemyOrder == "move"` units of `bridge.Session.Last.Why`, in order.
  - `HeldTokensAreHeldBeforeTheMapRebuilds` (`Hold` is called before the fake's `Changed`-time `Show`)
  - `SkipReleasesEverythingAtOnceAndReturnsTheCamera` (`ReleaseAll(snap: true)`, and `LookAt` equals the saved pose in the same call)
  - `TheCameraReturnsToThePlayersViewAfterTheLastBeat` (tick to the end: `LookAt` equals the saved pose within 1 m)
  - `APlayerPanMidPlaybackIsNotUndone` (move the fake camera between ticks: no further `LookAt` writes and no return glide)
  - `ThePlaybackChangesNoState`: `bridge.State.Hash()`, `Session.Save` and `Session.Last` are the same before, during and after the playback, and after Skip.
  - `ADestroyedMoversBeatIsSkippedAndThePlaybackGoesOn`
  - `EndTurnMidPlaybackFinishesTheOldOneFirst` (`ReleaseAll` before the new `Hold`. Only the new turn's ids are held.)
  - `NewGameMidPlaybackDropsIt` (`bridge.Replace` leads to `Finish`, and no hold is left)
  - `DisposingMidPlaybackReleasesEveryTokenAndReturnsTheCamera` (a language switch runs `GameRoot.Begin`, which disposes it)
  - `ItWaitsForTheOutcomeClips` (with `clipShowing` true, `Start` does nothing. After `closed`, it begins.)
  - `AClaimedEngagementPlaysOnItsBeatNotAtOnce` (no `PlayAttack` at `CombatResolved`. One `PlayAttack` per attacker on its beat.)
  - `WithThePlaybackOffAttacksPlayAsBefore` (`enabled` false: #2177's `AFreshEndTurnEngagementPlaysBesideAReplayedOne` expectations hold)
  - `AnInstantAttackOrderIsNeverClaimed` (the `Committed` path is unchanged)
  - `NoPlaybackWhileTheUnitsAreHidden`
- [ ] **Step 2: Run them to see them fail.** Expected: the types and the `Stepped` event are not found. Record the red run for the PR.
- [ ] **Step 3: Implement `Stepped`, the `claimed` filter and `EnemyTurnPlayback`; wire them in `GameRoot`.** Construct the playback after the units layer and the `OutcomePresenter`. Pass `playback.Claims` to `UnitAttackPresenter`. Call `playback?.Tick(Time.deltaTime)` in `Update`. In the outcome clips' `closed`, call both `card?.Render()` and `playback?.Start()`.
- [ ] **Step 4: Run them, and #2177's `UnitAttackPresenterTests`, to see them pass.**
- [ ] **Step 5: Commit** `Map: play the enemy's turn beat by beat after End turn (refs #2202)`.

### Task 6: The Skip bar, Esc, and the report and card waiting (Q1 (a))

**Files:**
- Modify: `unity/Assets/UI/Game.uxml` (a new `enemy-playback` element after `turn-report` and before `outcome-layer`, so a clip covers it), `unity/Assets/UI/Statecraft.uss`, `unity/Assets/Scripts/UI/TurnReportPresenter.cs`, `unity/Assets/Scripts/UI/EventCardPresenter.cs` (`RenderCard`'s `shown` and `reopen` lines) and `unity/Assets/Scripts/UI/GameRoot.cs` (the Escape chain in `Update`), plus the strings files
- Create: `unity/Assets/Scripts/UI/EnemyPlaybackBar.cs`
- Test: `unity/Assets/Tests/EditMode/EnemyPlaybackUiTests.cs`, and additions to `TurnReportTests.cs`

**Interfaces:**
- Produces:
  - `EnemyPlaybackBar(VisualElement root, UiText text, EnemyTurnPlayback playback, SimBridge bridge, IReadOnlyList<UnitEntry>? units)`. It shows on `Started` and hides on `Finished`, with no focus taken (non-modal). It has a `Label` `enemy-playback-title` (`playback.title`), a `Label` `enemy-playback-step` (`playback.step` with the beat's index and count, then the beat's unit line in words, reusing `report.enemy_move` and `report.enemy_attack` through `UnitNames.Of`, as `TurnReportPresenter.EnemyOrders` names them), and a `Button` `enemy-playback-skip` (`playback.skip`, `focusable = true`, in the tab order, with `tooltip = playback.skip_tip`).
  - The caption gives each beat in words, so the motion is not the only signal (WRITING_UI UI-18). The tooltip holds the spoken text for the later accessibility layer, as `HudModel.Spoken` does. No runtime tooltip is read by a screen reader yet (`HudModel.cs:34-35`), and the PR must say so, not claim screen-reader support.
  - `TurnReportPresenter.Held` (`bool`): while true, `onTurn` remembers the turn and does not `Show`. Setting it false shows the remembered turn.
  - `EventCardPresenter.HeldForPlayback` (`bool`): treated like `hiddenInCapture` in `RenderCard`, and `Render()` runs on change. `Opened` (the card's sound cue) fires when the card comes into view on release, once.
  - Escape: in `GameRoot.Update`, after the `outcomes` line, `if (!done) done = playback?.Skip() == true;`.

- [ ] **Step 1: Write the failing tests:**
  - `TheReportWaitsForThePlaybackAndOpensWhenItEnds`
  - `SkipOpensTheReportAtOnce`
  - `ACardThatOpenedThisTurnWaitsThenOpensWithItsCue`
  - `TheSkipButtonIsFocusableAndSkips` (`Focus()`, then a `NavigationSubmitEvent`, leaves `Active` false)
  - `EscapeSkipsThePlaybackAndNothingElse` (no screen closes, and the selection is kept)
  - `TheCaptionNamesTheBeatsUnit` (en and he)
  - `TheSkipBarFitsAndMirrorsInHebrew` (at 360 px it is inside the root and does not overlap `end-turn`. In he it is right to left, by `UiDirection`.)
  - `EveryPlaybackKeyIsInBothLanguages` (each `playback.*` and `settings.enemymoves` key is in both files with the same placeholders, UI-15)
- [ ] **Step 2: Run them to see them fail.**
- [ ] **Step 3: Implement the bar, the holds and the Escape line; add the strings** (see "Strings").
- [ ] **Step 4: Run them to see them pass**, then the full `TurnReportTests` and `EventCardTests` fixtures.
- [ ] **Step 5: Commit** `UI: a Skip bar for the enemy's turn; the report and card wait for it (refs #2202)`.

### Task 7: The Settings switch (Q6 (a))

**Files:**
- Modify: `unity/Assets/Scripts/UI/SettingsPresenter.cs` (beside `TiltShiftKey`, `:21`, and its toggle at `:151-153`), `unity/Assets/UI/Game.uxml` (a row after `settings-tiltshift`, `:439-440`) and the strings files
- Test: `unity/Assets/Tests/EditMode/SettingsScreenTests.cs`

**Interfaces:**
- Produces: `SettingsPresenter.EnemyMovesKey = "enemymoves"`, with stored values `"on"` and `"off"` and the default on. `public static bool SettingsPresenter.EnemyMovesOn(SettingsStore? store)`. The button reads `settings.on` or `settings.off`, like the tilt-shift button. GameRoot's `enabled` delegate reads it at each `Stepped`, so a change applies from the next End turn. Turning it off mid-playback calls `playback.Skip()`.

- [ ] **Step 1: Write the failing tests:**
  - `TheEnemyMovesSwitchDefaultsOnAndIsRemembered`
  - `TurningItOffMidPlaybackSkips`
  - `WithItOffEndTurnPlaysNothing`
- [ ] **Step 2: Run them to see them fail.**
- [ ] **Step 3: Implement it.**
- [ ] **Step 4: Run them to see them pass.**
- [ ] **Step 5: Commit** `Settings: show the Arab forces' moves on the map, on or off (refs #2202)`.

### Task 8: Evidence, performance and the changelog (PR C)

- [ ] **Step 1: Run `scripts/unity_compile/compile.sh`.** Expected: 0 errors and 0 warnings. Then run the full EditMode suite in en_US and he_IL. Expected: `failed="0"`, and the total grows by the new tests against the nightly baseline. Paste the totals in the PR.
- [ ] **Step 2: Red-first evidence:** the Task 5 tests run with `EnemyTurnPlayback.Tick` stubbed out. Expected: the ordering, return and caption tests fail. Paste the red counts.
- [ ] **Step 3: WebGL performance.** On the nightly gzip build and the PR build of the same base, alternate base, PR, base, PR, and run `tools/headed.py boot` three times each. Expected: `first_frame_ms` and `map_build_ms` medians within the noise #2176 recorded. Then capture frame times (`FrameStats.Summary`) over one enemy turn's playback. Expected: p95 no worse than a keyboard pan of the same length. The driver allocates nothing per frame: beats are built once at `Stepped`, and at most `MaxUnitsPerBeat` `UnitTokenMotion` components run at a time (#2176 runs them only while moving).
- [ ] **Step 4: Captures.** Record a clip of one first-1948 enemy turn on main (tokens jump, no camera) and on the branch (beats, glide, return), with the same state, camera and zoom, in English and in Hebrew. Add a still of the Skip bar at 360 px width.
- [ ] **Step 5: Changelog.** Write `changelog.d/2202-enemy-move-playback.md`, player-readable: "After End turn, the map plays the Arab forces' moves one at a time and the camera follows them, then returns to your view. Skip or Esc ends it; Settings can turn it off."
- [ ] **Step 6: Commit and open PR C**, with `Fixes #2202`, the owner's Q answers quoted, the AI-assistance section, and the licence box left for the owner.

---

## Strings

Every Hebrew line below is agent-drafted: **native-review-pending (#564)**. Each gets a YAML comment saying so in `strings.he.yaml`. None of them puts a verb or pronoun that must agree with a unit-name placeholder (WRITING_UI_HE HE-15). The existing `report.enemy_move` ("{0} עבר לעמדה חדשה") does, and the bar reuses it. That issue predates this plan, and it is flagged for the #564 reviewer, not changed here (one string per PR, `AGENTS.md`). The side is named "Arab forces" / "הכוחות הערביים", as in `report.enemy` (UX_GLOSSARY front-line-and-control). No new side label is added.

| Key | EN | HE | Rule |
|---|---|---|---|
| `playback.title` | The Arab forces' turn | התור של הכוחות הערביים | UI-02 sentence case |
| `playback.step` | Move {0} of {1} | מהלך {0} מתוך {1} | UI-04 counts. No plural form changes with the number (HE-04). |
| `playback.skip` | Skip the Arab forces' moves | דילוג על המהלכים של הכוחות הערביים | UI-09 verb and destination, as in `opening.skip`. HE-03 action noun, as in `opening.skip` ("דילוג על הפתיח"). |
| `playback.skip_tip` | Skip the Arab forces' moves and return the map to your view (Esc) | דילוג על המהלכים של הכוחות הערביים וחזרה לתצוגה שלך במפה (Esc) | UI-11 second person. HE-03 "שלך". HE-09 the Latin "Esc" in parentheses. |
| `playback.folded` | The other moves at once | שאר המהלכים בבת אחת | caption of a folded beat |
| `settings.enemymoves` | Show the Arab forces' moves on the map | הצגת המהלכים של הכוחות הערביים במפה | Settings row, as in `settings.tiltshift`. On and Off reuse `settings.on` and `settings.off`. |
| `advisor.page.enemy_phase.text` (Q8, appended) | … After you end the turn, the map plays their moves one at a time; Esc skips them, and Settings can turn them off. | … אחרי סיום התור המפה מציגה את המהלכים שלהם אחד אחרי השני; Esc מדלג עליהם, ואפשר לכבות אותם בהגדרות. | changes an existing string: EN and HE together, sent to #564 |

## Out of scope (say so in the PR)

- No arrow or path for enemy units (GDD, #2134).
- No ghost for a unit destroyed in the turn. Its token is gone after the rebuild, and its move beat is skipped. A ghost would need a new token kind: ask the owner if it is wanted.
- The Israeli units' own End turn steps, the go-to routes of R-MOVE-009, still slide at once (#2176).
- The general reduced-motion switch (#2070, #40), unless the owner picks Q6 (b).
- No new fire takes for the generated Syrian, Iraqi, Egyptian and Lebanese infantry. Their attack beats show the camera and the caption, without a take (#2177 finding 4).

## Issue map

#2202 (this). #2176 / #1866 (slide: hold and release added on top). #2177 / #1867 (attack hook and `CombatResolved`: the `claimed` filter added; the playback avoids #2177's replay-prefix finding by reading `EnemyOrder`). #2134 / #2184 (no enemy arrow). #2070 and #40 (reduced motion, Settings). #813 (turn time). #564 (Hebrew review). #398 (no screen reader layer yet).
