# Annex or withdraw after an occupation (#799). Plan

> **For the implementer:** the owner approved this plan and answered all four of its questions on 24 October 2026. Every
> task can start now. Task 5 (Deport of a fled town) is its own small PR after the Task 0 to 4 PR. Red-first tests. No
> puppet regime anywhere.

**Status (24 October 2026):** approved. Read against `origin/main` at `ef62e9f1c` (#1517, the Civ movement capture at the
click, merged 23 October 2026). No open or merged PR implements #799; #1324 (Occupy) says `EndOccupation` is not in it.

**Design record:** [docs/GDD.md](../../GDD.md) "After capturing an enemy city or village" (still says "puppet regime or
annex" and "X is unset"; Task 0 brings it up to date), [docs/gdd/settlement-control.md](../../gdd/settlement-control.md)
(the owner's rulings as rules, lines 15 and 19 to 22), the Phase 6 plan's PR 7
([2026-10-15-gdd-phase-6.md](2026-10-15-gdd-phase-6.md), "PR 7"), and the implemented rules R-CTRL-001 to R-CTRL-022 in
[docs/rules/mvp.md](../../rules/mvp.md).

## What the owner has already decided

| Ruling | Words | Where |
|---|---|---|
| Q-PUP-1 (c) | No puppet regime in the MVP: the end of an occupation offers annex or withdraw only | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249846499) |
| Q-ANNEX-1 | "arab residents in 1948-1949 flee when their settelment is captured, except for places where historically we know they didn't run. for those places - (a)": (a) is "the residents join the player's population, with their housing and jobs" | same comment |
| Q-ANNEX-1 follow-ups | "default is fled, unless you have super credible israeli sources that say otherwise"; a place emptied by flight: "Territory only, no gain" (no housing, jobs or score) | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249967916) |
| Q-ANNEX-2 (a) | After Deport, neither puppet nor annex is offered; Deport ends the occupation for good | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249846499) |
| Per place | Acre "Count as stayed"; Nazareth stayed; Lydda, Ramle and Beersheba "Default 'fled'; Morris isn't enough" | [#793](https://github.com/eyalgolan/statecraft-israel/issues/793#issuecomment-6252718065) |
| Jaffa | "Only in a what-if": never captured in the historical course | [#793](https://github.com/eyalgolan/statecraft-israel/issues/793#issuecomment-6254333177) |
| Q-OCC-3 | X = 6 monthly turns (R-CTRL-012) | [#1324](https://github.com/eyalgolan/statecraft-israel/pull/1324#issuecomment-6338594839) |
| Q-REF-1 (a) | The reference player and `HistoricalAI` always Raid, so they never reach the end of an occupation | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249822320) |
| This plan's OQ 1 to 3 | See "The owner's answers" below | [#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433) |
| This plan's OQ 4 | "Hide it, button explains (Recommended)": see OWNER QUESTION 4 below | [#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6375591758) |

The 23 October rulings on #1510 to #1513 (replacements, qualified civilians, terrain, ministers) say nothing about
occupation or annexation. #1511's "qualified civilians count as ordinary civilians" does not define annexed residents.

## What main has today (checked)

- `SettlementControl.AwaitingEnd = "awaiting_end"` and the six-turn timer (`SettlementControl.Tick`), which keeps charging
  `mvp.occupation_upkeep_per_turn` while a place waits for `EndOccupation`, and ends the occupation as a Raid when no
  Israeli military unit is left on the hex (R-CTRL-013).
- `ResolveCapture` and its touch-points: `Commands.cs` (class and `CommandText.Validate`), `AttentionSystem.cs` (both kind
  sets), `Simulation.Execute`, `SaveGame.cs` (`capture <settlement> <outcome>`), `Scenario.cs` (`Allowed`, `Build`,
  `Describe`), `HistoricalAI.cs`, `content/rules.yaml` (`attention_cost.resolve_capture: 0`), `Fixture.GddCosts`,
  `CommandGuardTests.Samples`, `TurnPreviewTests.WhichOrdersAreInstant`, and in Unity `SimBridge.Key`/`Validate` and
  `SimBridgeGuardTests`. `EndOccupation` needs the same set.
- After #1517: `CaptureHeld` runs after the route step and after each order, and `Stamp` rebuilds every new record with the
  full positional constructor. `Stamp` dates only records with `Since` unset whose owner is `israel` or that are
  depopulated (Deport's ownerless record). `CaptureHeld` skips any target that already has a record, so an annexed record
  can never be captured again by the player, and neither can any other record unless Task 2 says so.
- Every way a captured place returns to enemy control removes its record today: Raid (`Withdraw`, R-CTRL-010), the
  timer's raid when no unit is left (`Tick`, R-CTRL-013), the end-of-game raid (`EndPending`, R-CTRL-008), and enemy units
  retaking a capture still awaiting the choice (`Capture`, R-CTRL-004).
- The player-facing lines that say "Its residents are unchanged" are in `Withdraw` (two variants), `StartOccupation` and
  `Tick` (the timer's raid), all in `SettlementControl.cs`. No test asserts those words; `RaidTests` and `OccupyTests` say
  them only in comments.
- No `EndOccupation`, no `annexed` state, no fled or stayed field. The record's resident state is `Depopulated` alone
  (`GddState.cs`, R-CTRL-003). In Unity, `CivilianPeople.Inhabited` hides a town's villager figures when its record is
  depopulated; its comment already expects "a capture whose residents fled records them as gone".
- `content/settlement_targets.yaml` has six rows, all `status: sensitivity-reviewed` (#793); R-CTRL-001's
  "each `status: draft`" sentence is out of date and Task 6 fixes it.
- The only scenario golden with a `ctrl{}` section is `scenario-mvp-occupy-cost.txt`, which captures Nazareth (stayed).
  No golden captures Lydda, Ramle or Beersheba, so the fled field moves no existing golden.
- `cities.yaml` holds the twelve Jewish cities and towns. Several systems read `c.Cities` (content) and not `s.Cities`
  (state): `Training.cs:46` does `c.Cities.First(k => k.Id == city)`, which throws for a city that exists only in state;
  `Cities.Apportion` spreads arrivals over `c.Cities` only; `Projects.cs:268` reads a city's resources with
  `FirstOrDefault` (safe). A city created by annexation must not crash any of these (Task 4).

## The owner's answers (24 October 2026)

The owner, in the coordinator session, verbatim: "1520: do this: 1. When the player annexes a town whose residents stayed
(Acre, Nazareth), how many people, homes and jobs join the game? - a. The same rule as the 12 starting cities: a sourced
resident count you approve. (Recommended) 2.b 3.a"
([#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433)).

### OWNER QUESTION 1: When the player annexes a town whose residents stayed (Acre, Nazareth), how many people, homes and jobs join the game?

The options were (a) the starting-city rule (Recommended), (b) the owner sets each town's numbers, (c) build the rest and
leave annexing Acre and Nazareth refused until later.

**Then decided (owner, 24 Oct 2026, in the coordinator session, "a. The same rule as the 12 starting cities: a sourced
resident count you approve. (Recommended)";
[#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433)):** the town becomes one new
city. Its residents come from a sourced count that a researcher finds and the owner approves; homes equal the residents,
jobs equal the people in work at the national share already used for seven towns (43.9%), and housing starts at the usual
condition (Q-START-1's rule for the starting cities).

### OWNER QUESTION 2: When does the game treat a town's residents as having fled?

The options were (a) only when the player annexes (Recommended), (b) at capture, as the ruling's words say literally.

**Then decided (owner, 24 Oct 2026, in the coordinator session, "2.b";
[#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433)):** at capture. Capturing a
town whose target row says `residents: fled` records the flight at once. The "residents are unchanged" texts change for
those towns, the record of the flight stays after a Raid, and Deport of a town whose residents already fled needs a new
ruling (OWNER QUESTION 4). Towns whose residents stayed (Acre, Nazareth) behave at capture exactly as today.

### OWNER QUESTION 3: After annexation, does the town stay Israeli for good?

The options were (a) yes, for good (Recommended), (b) only while a unit stays, (c) for good, but the upkeep continues.

**Then decided (owner, 24 Oct 2026, in the coordinator session, "3.a";
[#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433)):** the occupation ends, its
upkeep stops and no unit has to stay. Enemy units treat the town like any Israeli town (today they cannot enter one;
enemy captures of Israeli towns are #1305's work).

## OWNER QUESTION 4: Deport of a town whose residents fled when it was captured

Under OWNER QUESTION 2 (b), the residents of Lydda, Ramle or Beersheba are recorded as fled the moment the player captures
the town. Deport (R-CTRL-018) displaces "every resident the state holds" and charges Social cohesion, International
Support, Money and a Support drag for the rest of the game. For a town the game already records as emptied, what does the
Deport button do?

- **(a) Deport is not offered for a town whose residents fled (Recommended).** The capture dialog still lists Raid,
  Occupy and Deport in the fixed order, but Deport is refused with a plain reason, for example "Its residents fled when it
  was captured, so there is no one to deport", and the button shows that reason. Raid and Occupy work as for any town.
- **(b) Deport is offered as for any town**, with its full costs and the Support drag, and the record becomes
  depopulated (the place is then closed and can never be captured again, R-CTRL-020).
- **(c) Deport is left out of the list** for such a town: the dialog shows only Raid and Occupy, with no explanation.

Recommendation: (a). It matches the record (no one is left to deport), keeps the dialog's fixed order and its "every option
says why it is refused" pattern (R-CTRL-008), and cannot make Deport cheaper or better than Raid, so `DeportNeverDominates`
(R-CTRL-022) is untouched. Deport is off in the real content (`mvp.deport_in_play`), and Phase 6 PR 10, which turns it
on, must not land before Task 5.

**Then decided (owner, 24 Oct 2026, in the coordinator session, "Hide it, button explains (Recommended)";
[#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6375591758)):** option (a). For a town whose residents fled at capture, Deport is
not available: the dialog keeps Raid, Occupy and Deport in the fixed order, and the Deport button is refused with its
plain reason. Raid and Occupy work as for any town. Task 5 is unblocked.

## Settled by this plan, not owner questions

These follow from a ruling or an existing precedent. The owner can overrule any of them in review.

- **Jaffa takes the default "fled"**, by the "default is fled" ruling. It is never captured in the historical course, so
  it can only reach annexation through a future what-if card.
- **`attention_cost.end_occupation: 0`**, a labelled design value, as `resolve_capture` (D-A6: unit orders are free;
  agents propose design values, #375).
- **The options are Annex, then Withdraw**, the order of the ruling's words ("annex or withdraw"), with no default or
  selected flag, as `CaptureOptions` (R-CTRL-008).
- **Withdraw is the Raid withdrawal** (R-CTRL-010), the same code path, as Q-OCC-2 (a) already makes early withdrawal.
- **Annex needs the garrison**: an Israeli military unit on the hex, as Occupy does (R-CTRL-011). Without one, step 10
  already ends the occupation as a Raid (R-CTRL-013).
- **The game's end changes nothing new.** A place still occupied or awaiting `EndOccupation` when the game ends stays as
  it is, as on main today: no automatic annexation, no automatic withdrawal, no score. (The "Counts as Raid" ruling covers
  only an unanswered capture, R-CTRL-008.)
- **What OWNER QUESTION 2 (b) implies** (the plan's analysis before the answer, made concrete):
  - The record gains one field, `Fled` (`fled=1` in the hash, written only when true, R-CTRL-002). It is set at capture
    from the target row and never cleared. Stayed towns never get it, so their behaviour and every existing golden are
    unchanged.
  - **Flight at capture is not Deport.** It adds nothing to `DisplacedByPlayer` (Q-DEP-2 (a) counts Deport only), costs
    no indicator, Money or score, and starts no Support drag. It changes no unit, city, resource or objective.
  - **The flight is remembered after the place returns to enemy control.** Raid, the timer's raid, the end-of-game raid
    and enemy units retaking an awaiting capture leave an ownerless record, `since=<capture date>,fled=1`, instead of
    removing it: no owner means enemy control, as Deport's ownerless record does.
  - **A fled town can be captured again.** `CaptureHeld` treats an ownerless `fled` record (not depopulated) as no
    record, and the new capture's record keeps `fled=1` and takes the new turn's date (`Stamp`). Movement already leaves
    the target open (`Movement.Closed` closes only depopulated targets).
  - **`Stamp` dates a fled record too**, so a capture answered with Raid in the same turn keeps its date, as Deport's
    record does (#1327).
  - The map shows no villager figures at a fled town once it is captured (`CivilianPeople.Inhabited` reads `Fled` as it
    reads `Depopulated`).
  - Annexing reads the record's `Fled`, not the content row, so the outcome is the one recorded at capture.

## Rule IDs (the Phase 6 plan's numbering)

- **R-CTRL-014:** `EndOccupation(settlement, outcome)` is one deferred command kind with one `attention_cost.end_occupation`.
  It is valid only for a place in `awaiting_end` with an Israeli military unit on its hex and an outcome of `annex` or
  `withdraw`; otherwise it is refused with a reason, the state unchanged and the Attention refunded.
- **R-CTRL-015:** No puppet regime (Q-PUP-1 (c)). `SettlementControl.EndOptions` offers Annex and Withdraw, in that order,
  with no default; Withdraw is R-CTRL-010's withdrawal.
- **R-CTRL-016:** Annexation and the residents (Q-ANNEX-1; OWNER QUESTIONS 1 and 3). The record becomes
  `owner=israel,state=annexed`, keeping its capture date and `fled`, with no timer. A fled place adds nothing else. A
  stayed place adds one city by the starting-city rule. Upkeep stops and no unit has to stay.
- **R-CTRL-017:** A depopulated place (Deport) is never offered or given `EndOccupation` (Q-ANNEX-2 (a)), so it is never
  annexed.
- **Existing rules amended by OWNER QUESTION 2 (b):** R-CTRL-003 (residents: inhabited, fled or depopulated), R-CTRL-004
  (a fled target's capture record, recapture, and the ownerless record when enemy units retake it), R-CTRL-005 (capture
  writes `fled` for a fled target), R-CTRL-008 (the end-of-game raid keeps a fled record), R-CTRL-010 and R-CTRL-013 (the
  record is kept as ownerless and fled instead of removed, and the line no longer says "unchanged" for a fled town),
  R-CTRL-011 (Occupy's line). No new ID.

## Global constraints

- No puppet constant, state, option, string or alias.
- A fled capture and a fled annexation add no city, people, housing, jobs, Food, Money, Arms, indicator change, score,
  objective progress or `DisplacedByPlayer`.
- Every number for a stayed annexation is sourced and owner-approved. Never infer a 1948 count from the 1945 figures in
  `content/map/settlement_population_1945.yaml`, and never put a placeholder number in content.
- The resident outcome is factual content: each changed row in `content/settlement_targets.yaml` goes back to
  `status: draft` until the owner's sensitivity review. An AI-assisted commit never sets `sensitivity-reviewed`.
  **This blocks shareable builds** (`ReleaseGate.SettlementTargetsUnchecked`) from the merge until the owner re-reviews the
  six rows, so the PR lands outside a release window.
- Every new player-facing line about residents (capture, Raid, Occupy, the timer's raid, annexation) is in the owner's
  sensitivity-review packet (Task 7) before the rows go back to `sensitivity-reviewed`.
- Deterministic: ordinal order, no RNG draw, invariant formatting. A new state field is written only when not default.
- `EndOccupation` is deferred (not in `Simulation.IsInstant`), as `ResolveCapture` is (Civ movement plan, spec 3.2).
- Every new R-CTRL rule has a citing test; `uv run scripts/oss/rule_ids.py` passes.
- `changelog.d/799.md`. No em dashes. They/them for people.
- Out of scope: the end-of-occupation dialog (#802), map markers (#803), Hebrew UI strings, follow-up cards, enemy
  captures of Israeli towns (#1305), diplomacy, puppet regimes.

## Overlap with work in flight

- **Civ movement (#1398):** S5c and S6 merged as #1517. `Stamp` rebuilds records with the positional constructor, so the
  new `Fled` field must be passed there and in every other rebuild (`StartOccupation`, `Tick`). The U1 to U5 Unity branch
  (`civ/u1-u5-movement-ui`, not yet a PR) rewrites `SimBridge.cs` (the queue becomes committed plus deferred orders) and
  U6 (Codex, the capture dialog after a capturing move) touches `CaptureDialogPresenter`. #799's Unity changes are two
  lines in `SimBridge.Key` and `SimBridge.Validate` and one condition in `CivilianPeople.Inhabited`; whichever lands
  second takes the other's file as base and re-adds its lines. `EndOccupation` stays deferred, so U1's committed list
  never holds it.
- **#1305 (enemy captures of Israeli towns, R-CTRL-006):** under OWNER QUESTION 3 (a) an annexed town is an Israeli town
  for the enemy, so #1305's rule covers it once built. Task 4 makes `Movement.NoEntry` treat an annexed target as Israeli
  (closed to the enemy, open to the player, not an "enemy-held locality" for retreat or withdrawal). #1305 touches the
  same helper; whichever lands second keeps both conditions.
- **Phase 6 PR 10 (Deport in play):** must not turn `mvp.deport_in_play` on before Task 5 lands.
- **Advisors S1 (`feat/advisors-s1`):** Unity only, no settlement code. No overlap.

## Tasks

| Task | What | Can start |
|---|---|---|
| 0 | Record the answers in the design docs | now |
| 1 | The resident outcome in each target row, and the research packet for Acre and Nazareth | now; the stayed rows' `people` wait for the owner's approval of the counts |
| 2 | Residents flee at capture (OWNER QUESTION 2 (b)) | now |
| 3 | `EndOccupation`, its options and every touch-point | now |
| 4 | Withdraw and annex | now; the stayed annexation's real numbers come from Task 1 |
| 5 | Deport of a fled town | now, as its own PR after the Task 0 to 4 PR (OWNER QUESTION 4 answered: (a)) |
| 6 | One real-content scenario, rules text, changelog | now |
| 7 | Review and gates | with the PR |

Tasks 0 to 4 and 6 to 7 are one PR. It can be written and reviewed now, and it merges once the owner has approved the
Acre and Nazareth counts (Task 1). Task 5 is its own small PR after that one.

### Task 0: Record the answers (docs only)

**Files:** `docs/GDD.md` (the "After X turns of occupation" paragraph and the Settlement control row),
`docs/gdd/settlement-control.md` (lines 12, 19 to 22 and the R-CTRL references), `docs/superpowers/plans/2026-10-15-gdd-phase-6.md`
(PR 7: drop `Puppet` and R-CTRL-015 "the puppet regime"), `docs/oss/DECISIONS.md`.

- [ ] Copy the four answers, with their #799 links, as "Then decided (owner, 24 Oct 2026, in the coordinator session, ...)"
  lines, as this plan does. The GDD says: no puppet regime in the MVP; X is six monthly turns; annex or withdraw; residents
  of a fled town are recorded as fled at capture; an emptied town adds land only; a stayed town adds one city by the
  starting-city rule; an annexed town stays Israeli for good with no upkeep. `settlement-control.md` line 12 ("only whether
  a place is inhabited") becomes inhabited, fled or depopulated. OWNER QUESTION 4's answer (a) is recorded too.
- [ ] `uv run scripts/oss/gdd_lint.py`, `uv run scripts/oss/rule_ids.py`, `uv run scripts/oss/check_repo.py`.

### Task 1: The resident outcome in each target row

**Files:** `sim/Statecraft.Sim/Content.cs` (`SettlementTarget`), `sim/Statecraft.Sim/ContentLoader.cs`
(`ValidateSettlementTargets`), `content/settlement_targets.yaml`, `sim/Statecraft.Sim.Tests/CaptureTests.cs` (the fixture
row helper `Target`), `ContentLoaderTests.cs`, `RealContentTests.cs`; a research note under `research_notes/GDD phase 6/`.

**Shape** (OWNER QUESTION 1 (a)):

```yaml
  - id: acre
    ...
    residents: stayed              # fled or stayed (Q-ANNEX-1); fled is the default the ruling sets
    residents_sources:             # at least one: the per-place sourcing the ruling asks for
      - { url: "https://...", checked: 2026-10-24 }
    annexation:                    # stayed only: what joins the player's population (OWNER QUESTION 1 (a))
      people: { value: <approved>, status: sourced, date: <day>, sources: [{ url: "https://...", checked: <day> }] }
      employed_share: { value: 0.439, status: design, sources: [...] }   # DESIGN VALUE: the national share (Q-START-1), as cities.yaml's seven towns
    status: draft
```

`SettlementTarget` gains `string Residents`, `List<Source> ResidentsSources` and `TargetAnnexation? Annexation` (with
`CityValue? People` and `CityValue? EmployedShare`). Rules, each a `ContentException` naming the file and field:
`residents` is `fled` or `stayed`; `residents_sources` has at least one https source with a `checked` date (the existing
source check); `annexation` is required for `stayed` and refused for `fled`; `people` is a whole number (`Whole`) with
`status: sourced`, and `employed_share` a share from 0 to 1, each with the same status and source rules as a `cities.yaml`
value. Housing, jobs and condition are not stored: they follow the starting-city rule (Task 4).

- [ ] **Research packet (can start at once):** for Acre and Nazareth, the resident count after their capture in July 1948
  from a source the starting cities' rule accepts (for example the November 1948 Registration of Population, where it
  lists the town), with the URL, the exact figure and its date, in a research note. The owner approves each count on #799
  before it enters content.
- [ ] **Red tests** in `ContentLoaderTests`: a valid stayed row loads with its people; a missing `residents`, an unknown
  value (`puppet`, `unknown`), no `residents_sources`, a fled row with an `annexation` block, a stayed row without one and
  a stayed row whose `people` is a design value are each refused with the field named.
- [ ] **Fixture:** `CaptureTests.Target` writes `residents: stayed`, one fixture source and an `annexation` block of
  fixture values (`people: 60` as `status: sourced`, `employed_share: 0.5` as `status: design`, fixture URLs only) on every row, so every existing Raid, Occupy and
  Deport test keeps its behaviour. Add `internal static Content LoadFled(string[] fled, params string[] targets)`, which
  writes the rows in `fled` as `residents: fled` with no `annexation` block, through the existing
  `Load(Action<Dictionary<string, string>>, ...)` overload.
- [ ] **Real rows:** acre and nazareth `stayed`; jaffa, lod, ramla and beersheba `fled`; sources from
  `research_notes/GDD phase 6/settlement-targets.md` and the rulings. Each row goes to `status: draft`. The two `people`
  values enter only after the owner approves them; until then the PR stays a draft.
- [ ] **Pin the map** in `RealContentTests`:

```csharp
// R-CTRL-016: the owner's per-place outcomes (#793): stayed only where the ruling says so.
[Fact]
public void SettlementTargetResidentOutcomesAreTheOwnersRulings()
{
    Assert.Equal(new[] { ("acre", "stayed"), ("beersheba", "fled"), ("jaffa", "fled"),
                         ("lod", "fled"), ("nazareth", "stayed"), ("ramla", "fled") },
        C.SettlementTargets.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x => (x.Id, x.Residents)).ToArray());
}
```

- [ ] The content hash changes (the save's content hash test proves it); no state golden should move from the schema.

### Task 2: Residents flee at capture (OWNER QUESTION 2 (b))

**Files:** `sim/Statecraft.Sim/GddState.cs` (`SettlementControl`: the `Fled` property, the constructor's last optional
`bool fled = false`, `Canonical`), `sim/Statecraft.Sim/SettlementControl.cs` (`CaptureHeld`, `Stamp`, `Capture`,
`Withdraw`, `StartOccupation`, `Tick`), new `sim/Statecraft.Sim.Tests/FledAtCaptureTests.cs`,
`unity/Assets/Scripts/Map/Collection/CivilianPeople.cs` (`Inhabited`), its EditMode test, `docs/rules/mvp.md`
(R-CTRL-003, -004, -005, -008, -010, -011, -013).

- `CaptureHeld`: a target with no record, or with an ownerless record that is `Fled` and not `Depopulated`, is captured.
  The new record is `new SettlementControl(Movement.PlayerSide, AwaitingChoice, fled: t.Residents == "fled")`. A fled
  capture's Why line adds one sober sentence, for example "{name} is captured: Israeli units hold it and no enemy unit is
  there. Its residents fled. It awaits a decision"; a stayed capture's line is unchanged. A recapture of a fled town adds
  no second sentence about the flight beyond the same line.
- `Stamp`: also dates a record that is `Fled` (`Owner == PlayerSide || Depopulated || Fled`), and passes `Fled` on.
- Every rebuild passes `Fled` on: `StartOccupation`, `Tick`'s countdown, and `Stamp`.
- A helper `Release(GameState s, string id)` replaces the four removals: a `Fled` record becomes
  `new SettlementControl(since: x.Since, fled: true)`; any other record is removed as today. `Withdraw`, `Tick`'s raid and
  `Capture`'s enemy return call it (`EndPending` goes through `Withdraw`).
- The lines: for a fled town, `Withdraw` says "{name}: Israeli units withdrew and it is under enemy control again. Its
  residents fled when it was captured" (and the no-unit variant), `StartOccupation` says "... an Israeli unit stays there.
  Its residents fled when it was captured", and `Tick`'s raid the same ending. Stayed towns keep "Its residents are
  unchanged". All wording goes into the owner's sensitivity packet (Task 7).
- `Depopulate` is not changed here (Task 5).
- Unity: `CivilianPeople.Inhabited` returns false for a `Fled` record as for a `Depopulated` one, so a fled town shows no
  villager figures once captured.

- [ ] **Red tests** in `FledAtCaptureTests` (fixture: `CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets)`;
  town_a on hex 1,0):
  - `AFledTownIsRecordedAsFledAtCapture`: a soldier's move ends on town_a; after `Step`, the record is
    `owner=israel,state=awaiting_choice,since=<turn end>,fled=1`, `DisplacedByPlayer`, indicators, Money, cities and score
    unchanged, `RngState` unchanged.
  - `AStayedTownIsCapturedAsToday`: the same move on a stayed row gives the record with no `fled` (pins the unchanged
    path).
  - `RaidOfAFledTownKeepsTheFlight`: capture and Raid in the same turn; the record is `since=<turn end>,fled=1` with no
    owner or state (proves `Stamp` dates it), the units left, and the Why line does not say "unchanged".
  - `TheFlightOutlastsEveryReturnToEnemyControl` (theory): the timer's raid with no unit left, the end-of-game raid, and an
    enemy unit alone on an awaiting capture each leave the ownerless fled record.
  - `AFledTownCanBeCapturedAgain`: after the Raid, a soldier's move ends on town_a again; the record is a new
    `awaiting_choice` capture with `fled=1` and the new turn's date.
  - `OccupyAndTheTimerCarryTheFlight`: Occupy, then six turns; every record on the way keeps `fled=1`.
  - Unity EditMode: `Inhabited` is false for `new SettlementControl(since: d, fled: true)` and for an owned fled record,
    true for a stayed capture.
- [ ] **Mutate and restore:** drop `Fled` from `Stamp`'s condition (the same-turn Raid test fails); drop it from the
  rebuild in `Tick` (the Occupy test fails); make `Release` remove every record (the raid tests fail).
- [ ] `RaidTests`, `OccupyTests`, `DeportTests`, `DeportNeverDominatesTests`, `CaptureTests`, `MovementTests` pass
  unchanged (the fixture's default rows are stayed). Every golden stays bit-for-bit.

### Task 3: `EndOccupation`, its options and every touch-point

**Files:** `Commands.cs`, `AttentionSystem.cs`, `Simulation.cs`, `SaveGame.cs`, `Scenario.cs`, `SettlementControl.cs`,
`content/rules.yaml`, `Fixture.cs` (`GddCosts`), new `EndOccupationTests.cs`, `CommandGuardTests.cs`, `SaveGameTests.cs`,
`TurnPreviewTests.cs`, `DesignValuesTests.cs`, `unity/Assets/Scripts/UI/SimBridge.cs`,
`unity/Assets/Tests/EditMode/SimBridgeGuardTests.cs`, `docs/rules/mvp.md` (R-CTRL-014, R-CTRL-015).

**Interfaces:**

```csharp
public sealed class EndOccupation : Command
{
    public override string Kind => SettlementControl.EndKind;
    public string Settlement { get; } public string Outcome { get; }
    public EndOccupation(string settlement, string outcome) { Settlement = settlement; Outcome = outcome; }
}
```

- `SettlementControl.EndKind = "end_occupation"`, `Annex = "annex"`, `Annexed = "annexed"` (the state), and Withdraw reuses
  the existing `Raid` withdrawal; the outcome string is `withdraw`.
- `public static IReadOnlyList<CaptureOption> EndOptions(Content c, GameState s, string id)`: clone, `TrySpend`, `End`, as
  `CaptureOptions` does with `Resolve`; empty when the place is not `awaiting_end`. `CaptureOption` and `CapturePreview`
  are reused as they are.
- `internal static string? End(Content c, GameState s, string id, string outcome, List<Cause> why)`.
- Save line `endocc <settlement> <outcome>`; scenario `{ kind: end_occupation, settlement, outcome }`; bridge key
  `"endocc:" + settlement`; `SimBridge.Validate` returns null for it, as for `ResolveCapture`.

- [ ] **Add the sample first:** `[typeof(EndOccupation)] = new EndOccupation("town_a", "annex")` in
  `CommandGuardTests.Samples` (refused: nothing awaits the end) and in `SimBridgeGuardTests`. Run `CommandGuardTests`; it
  names every missing touch-point.
- [ ] **Red tests** in `EndOccupationTests` (the fixture's town_a is on hex 1,0; `CaptureTests.Soldier(1, 1, 0)` holds it):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>R-CTRL-014 to R-CTRL-017 (#799): annex or withdraw when the occupation timer ends; no puppet regime.</summary>
public class EndOccupationTests
{
    internal static GameState AwaitingEnd(Content c, bool fled, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, s.Date, fled: fled);
        return s;
    }

    // R-CTRL-014: only a place whose timer has ended can be answered; a refusal changes nothing.
    [Theory]
    [InlineData(SettlementControl.AwaitingChoice, 0)]
    [InlineData(SettlementControl.Occupied, 3)]
    public void OnlyAnEndedOccupationCanBeAnswered(string state, int turns)
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, CaptureTests.Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, state, s.Date, turns: turns);
        var before = s.Describe();
        var why = new List<Cause>();
        Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, why));
        Assert.Empty(why);
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-015: Annex then Withdraw, nothing else, no puppet.
    [Fact]
    public void TheOptionsAreAnnexThenWithdraw()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, false, CaptureTests.Soldier(1, 1, 0));
        Assert.Equal(new[] { SettlementControl.Annex, "withdraw" },
            SettlementControl.EndOptions(c, s, "town_a").Select(x => x.Outcome).ToArray());
        Assert.NotNull(SettlementControl.End(c, s, "town_a", "puppet", new List<Cause>()));
    }

    // R-CTRL-014: annexing needs the unit that holds the place (R-CTRL-011's garrison).
    [Fact]
    public void AnnexNeedsAnIsraeliUnitOnThePlace()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, false, CaptureTests.Soldier(1, 2, 0));
        var before = s.Describe();
        Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-017: a depopulated place (no state) is never answered.
    [Fact]
    public void ADepopulatedPlaceIsNeverAnnexed()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, CaptureTests.Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(since: s.Date, depopulated: true);
        Assert.Empty(SettlementControl.EndOptions(c, s, "town_a"));
        Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
    }
}
```

- [ ] **Save and scenario** (in `SaveGameTests`; `SaveGame.Record` is internal and visible to the tests):

```csharp
[Fact]
public void EndOccupationRoundTripsThroughTheSaveAndTheScenario()
{
    var save = new SaveGame(new string('a', 64), 1, Mode.Historical, SaveGame.GddRuleset, new string('b', 64));
    save.Record(new Command[] { new EndOccupation("town a", "annex") }, new GameState());
    var text = save.Write();
    Assert.Contains("endocc town%20a annex", text);
    var back = Assert.IsType<EndOccupation>(SaveGame.Parse(text).Turns.Single().Single());
    Assert.Equal(("town a", "annex"), (back.Settlement, back.Outcome));
    Assert.IsType<EndOccupation>(Scenario.Build(new Dictionary<string, object>
        { ["kind"] = "end_occupation", ["settlement"] = "town_a", ["outcome"] = "annex" }));
}
```

- [ ] `TurnPreviewTests.WhichOrdersAreInstant`: add `Assert.False(Simulation.IsInstant(new EndOccupation("nazareth", SettlementControl.Annex)));`.
- [ ] Implement the touch-points; `content/rules.yaml` gets `end_occupation: 0` with its `# DESIGN VALUE` comment, the
  fixture's `GddCosts` the same, and `DesignValuesTests` lists it. No presenter or string.

### Task 4: Withdraw and annex

**Files:** `SettlementControl.cs`, `Movement.cs` (`NoEntry`), `Cities.cs` (new `Annex`), `Training.cs`, `Cities.cs`
(`Apportion`, unchanged behaviour), `EndOccupationTests.cs`, `MovementTests.cs`, `docs/rules/mvp.md` (R-CTRL-015 to
R-CTRL-017, and R-MOVE-005, R-ENEMY-001).

- `End(..., "withdraw")` calls the existing `Withdraw(c, s, id, why, false)`: same moves, same refusal, and for a fled town
  the ownerless fled record of Task 2.
- `End(..., Annex)`: refuse unless garrisoned; then, when the record is not `Fled`, `Cities.Annex` first (a refusal leaves
  the state as it was); then replace the record with `new SettlementControl(Movement.PlayerSide, Annexed, x.Since, fled: x.Fled)`.
  One sober Why line: fled, "{name} is annexed. Its residents fled when it was captured, so no people, homes or jobs are
  added"; stayed, "{name} is annexed. Its residents join the population". Both go into the owner's sensitivity packet.
- OWNER QUESTION 3 (a): `Tick` already ignores any state but `occupied` and `awaiting_end`, so upkeep and the garrison
  check stop with no change. `Movement.NoEntry` drops an annexed target from the enemy-held set (closed to the enemy, not
  an enemy-held locality for the player's withdrawal and retreat). `CaptureHeld` never takes an annexed record (it has an
  owner).
- `Cities.Annex(Content c, GameState s, SettlementTarget t, List<Cause> why) -> string?` (OWNER QUESTION 1 (a)):
  `people = Cities.StartUnits(t.Annexation.People!.Value, ppu)`, the employed converted as `cities.yaml` does (R-CITY-007,
  people times `employed_share` to the nearest unit), `housing = people`, `jobs = employed`, condition
  `mvp.housing_start_condition`, no refugees, recruits or craftsmen. Refused if `s.Cities` already has the id.
- **A city that exists only in state:** `Training.cs:46` becomes `c.Cities.FirstOrDefault(...)?.Facilities.Contains(f) == true`
  (an annexed city has no facility); `Cities.Apportion` keeps spreading arrivals over the starting cities only (existing
  rule, no change); check every other `c.Cities` reader listed above with a test that runs a turn with an annexed city.

- [ ] **Red tests:**

```csharp
// R-CTRL-015: withdrawing at the end is the Raid withdrawal: the units leave, the record goes.
[Fact]
public void WithdrawAtTheEndIsTheRaidWithdrawal()
{
    var c = CaptureTests.Load(CaptureTests.AllTargets);   // stayed rows
    var s = AwaitingEnd(c, false, CaptureTests.Soldier(2, 1, 0));
    var after = Simulation.Step(c, s, new Command[] { new EndOccupation("town_a", "withdraw") });
    Assert.Empty(after.Rejected);
    Assert.False(after.State.Settlements.ContainsKey("town_a"));
    Assert.NotEqual((1, 0), (after.State.Units[2].Q, after.State.Units[2].R));
}

// R-CTRL-016: a place emptied by flight is territory only, no gain ("Territory only, no gain", owner, 18 October 2026).
[Fact]
public void AnnexingAFledPlaceChangesOnlyItsRecord()
{
    var c = CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
    var s = AwaitingEnd(c, true, CaptureTests.Soldier(1, 1, 0));
    var before = s.Clone();
    Assert.Null(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
    Assert.Equal((Movement.PlayerSide, SettlementControl.Annexed, before.Settlements["town_a"].Since, 0, true),
        (s.Settlements["town_a"].Owner, s.Settlements["town_a"].State, s.Settlements["town_a"].Since, s.Settlements["town_a"].Turns, s.Settlements["town_a"].Fled));
    s.Settlements["town_a"] = before.Settlements["town_a"];
    Assert.Equal(before.Describe(), s.Describe());   // nothing else moved: resources, cities, units, score, objectives
    Assert.Equal(before.RngState, s.RngState);
}

// R-CTRL-016, OWNER QUESTION 3 (a): an annexed place pays no upkeep and needs no unit.
[Fact]
public void AnAnnexedPlaceCostsNothingAndStaysWithoutAUnit()
{
    var c = CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
    var s = AwaitingEnd(c, true, CaptureTests.Soldier(1, 1, 0));
    s = Simulation.Step(c, s, new Command[] { new EndOccupation("town_a", SettlementControl.Annex) }).State;
    s.Units.Remove(1);
    var next = Simulation.Step(c, s, Array.Empty<Command>());
    Assert.Equal(SettlementControl.Annexed, next.State.Settlements["town_a"].State);
    Assert.DoesNotContain(next.Why, x => x.Target == "town_a" || x.Message.Contains("occupation of", StringComparison.Ordinal));
}

// R-CTRL-016, OWNER QUESTION 1 (a): a stayed place becomes one city by the starting-city rule (people_per_unit 10).
[Fact]
public void AnnexingAStayedPlaceAddsExactlyOneCity()
{
    var c = CaptureTests.Load(CaptureTests.AllTargets);   // fixture rows are stayed, people 60
    var s = AwaitingEnd(c, false, CaptureTests.Soldier(1, 1, 0));
    Assert.Null(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
    var city = s.Cities["town_a"];
    Assert.Equal((6, 6), (city.Civilians, city.HousingCapacity));
    Assert.Equal(city.Employed, city.JobSlots);
    Assert.Equal(0, city.Refugees);
    Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));   // no second city
}
```

  `StepResult` has `State`, `Why` and `Rejected` (`Commands.cs`). Add one
  integration test that steps a turn with an annexed stayed city and a `Train` order there: refused with a reason, no
  exception. Add a `MovementTests` case: an enemy unit cannot end a move on an annexed target; a player's withdrawal may.
- [ ] **Mutate and restore:** give the fled annex one score point, one job, then IL 0.1m: each fails
  `AnnexingAFledPlaceChangesOnlyItsRecord`. Call `Cities.Annex` twice: the stayed test fails.
- [ ] Run `EndOccupationTests`, `FledAtCaptureTests`, `RaidTests`, `OccupyTests`, `DeportTests`,
  `DeportNeverDominatesTests`, `CaptureTests`, `MovementTests`, then the suite. `CalibrationTests` stays bit-for-bit (the
  reference player always Raids, and no golden captures a fled town).

### Task 5: Deport of a fled town (OWNER QUESTION 4 (a))

Its own PR, after the Task 0 to 4 PR. The owner chose (a) on 24 October 2026: "Hide it, button explains (Recommended)".

**Files:** `SettlementControl.cs` (`Resolve`'s Deport branch), `DeportTests.cs`,
`docs/rules/mvp.md` (R-CTRL-018, R-CTRL-020), `docs/gdd/settlement-control.md`, the capture dialog's refusal display only
if it does not already show the sim's refusal text.

- [ ] `Resolve(..., Deport)` on a `Fled` record is refused with "Its residents fled when it was captured, so
  there is no one to deport" (wording to the owner's sensitivity packet), the state unchanged, so `CaptureOptions` shows
  the reason on the Deport button. Red test in `DeportTests` with `LoadFled` and Deport in play: the option is listed third
  with that refusal, and resolving it changes nothing. `DeportNeverDominatesTests` stays on stayed rows.

### Task 6: One real-content scenario, rules text, changelog

- [ ] `sim/Statecraft.Sim.Tests/scenarios/mvp-annex-nazareth.yaml`: `mvp-occupy-cost`'s two steps, then
  `{ kind: end_occupation, settlement: nazareth, outcome: annex }` on 1948-12-15, `until: 1949-01-15`. Bless only the new
  golden with `STATECRAFT_UPDATE_GOLDEN=1`, inspect it, add its history row in `docs/determinism.md` and its SMOKE line in
  `RELEASING.md` if that file lists scenario pins. No other golden may move. (It needs Nazareth's approved count.)
- [ ] `docs/rules/mvp.md`: R-CTRL-014 to R-CTRL-017, the OWNER QUESTION 2 (b) amendments listed under "Rule IDs",
  R-MOVE-005 and R-ENEMY-001 (annexed targets), and R-CTRL-001's status sentence (the rows were sensitivity-reviewed on
  #793, and #799 returns the changed ones to draft).
- [ ] `changelog.d/799.md`, factual: residents of a fled town are recorded as fled when it is captured; when an
  occupation period ends the player can annex or withdraw; no puppet regime.

### Task 7: Review and gates

- [ ] Review packet for the owner: the six rows' outcomes and sources, the approved people counts and their sources, and
  every new line about residents (capture, Raid, Occupy, the timer's raid, the two annexation lines). The owner sets
  `sensitivity-reviewed` in their own commit after reading them.
- [ ] `dotnet test sim/Statecraft.sln` (0 warnings), the tools and policy suites (AGENT-RULES commands), `bash scripts/check.sh origin/main`
  ending "ALL CHECKS PASSED".
- [ ] Unity: `uv run tools/build_sim_for_unity.py`, then a full EditMode run under `tools/machine_lock.py` (the DLL,
  `SimBridge.cs` and `CivilianPeople.cs` change); XML total > 0, 0 NEW failures.
- [ ] Screenshots, English and Hebrew, same state, camera and zoom, on a `captures/pr-<n>` branch: Lydda's villager
  figures before and after its capture (the one visible change). Nothing else on screen changes until #802 and #803.

## Done when

- The owner's four answers are in `docs/GDD.md` and #799.
- Each target row has a sourced fled or stayed outcome, reviewed by the owner, and Acre and Nazareth have approved counts.
- A fled town is recorded as fled at capture, keeps the record after it returns to enemy control, and can be captured
  again; a stayed town is captured as today.
- `EndOccupation` has every touch-point; Withdraw is the Raid withdrawal; a fled annexation changes only the record; a
  stayed annexation adds exactly the approved city; a depopulated place is never annexed.
- R-CTRL-014 to R-CTRL-017 and the amended rules have citing tests; `scripts/check.sh` and the full EditMode run pass.
- Task 5 lands after the Task 0 to 4 PR, before Deport is turned on.
