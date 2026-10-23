# Annex or withdraw after an occupation (#799). Plan

> **For the implementer:** plan only. The owner approves this plan and answers the three OWNER QUESTIONS before any code.
> Red-first tests, one focused PR (or two if the owner picks OWNER QUESTION 1 (c)). No puppet regime anywhere.

**Status (24 October 2026):** proposed. Read against `origin/main` at `ef62e9f1c` (#1517, the Civ movement capture at the
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
  full positional constructor. `CaptureHeld` skips any target that already has a record, so an annexed record can never be
  captured again by the player.
- No `EndOccupation`, no `annexed` state, no fled or stayed field. `content/settlement_targets.yaml` has six rows, all
  `status: sensitivity-reviewed` (#793); R-CTRL-001's "each `status: draft`" sentence is out of date and Task 4 fixes it.
- `cities.yaml` holds the twelve Jewish cities and towns. Several systems read `c.Cities` (content) and not `s.Cities`
  (state): `Training.cs:46` does `c.Cities.First(k => k.Id == city)`, which throws for a city that exists only in state;
  `Cities.Apportion` spreads arrivals over `c.Cities` only; `Projects.cs:268` reads a city's resources with
  `FirstOrDefault` (safe). A city created by annexation must not crash any of these (Task 3).

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
- **No new resident field in the state** if the owner picks OWNER QUESTION 2 (a). The fled or stayed outcome is content,
  read when the player annexes.

## OWNER QUESTIONS

Code does not start until the owner answers all three. Record the answers on #799 and in `docs/GDD.md` (Task 0).

### OWNER QUESTION 1: When the player annexes a town whose residents stayed (Acre, Nazareth), how many people, homes and jobs join the game?

Your ruling says the residents join the player's population with their housing and jobs. The game needs a number for each,
and no number has been approved yet.

- **(a) The same rule as the twelve starting cities (Recommended).** The town becomes a new city. Its residents come from a
  sourced count (for example the November 1948 census, where it lists the town), which a researcher finds and you approve.
  Homes equal the residents, jobs equal the people in work at the national share already used for seven towns (43.9%), and
  housing starts at the usual condition. This is the rule you approved for the starting cities (Q-START-1).
- **(b) You set each town's numbers yourself**, as labelled design values, with no research step.
- **(c) Build the rest now and leave this for later.** Withdraw and annexing an emptied town work now; annexing Acre or
  Nazareth is refused ("not built yet") until the numbers are approved in a follow-up.

Recommendation: (a). It reuses a rule you already approved and keeps every number sourced.

### OWNER QUESTION 2: When does the game treat a town's residents as having fled?

Your ruling says residents flee "when their settlement is captured". The rules already built say capture, Raid and Occupy
leave residents unchanged, and the player is told "Its residents are unchanged" after a Raid or Occupy.

- **(a) Only when the player annexes (Recommended).** Capture, Raid and Occupy stay as they are. The fled or stayed outcome
  matters only at annexation: an emptied town adds land only, a town whose residents stayed adds its people.
- **(b) At capture, as your words say literally.** Capturing Lydda, Ramle or Beersheba records the residents as fled at
  once. The "residents are unchanged" texts change for those towns, the record stays after a Raid, and Deport of a town
  whose residents already fled needs a new ruling from you.

Recommendation: (a). It delivers the ruling's effect (no gain from an emptied town) without rewriting rules and texts you
have already reviewed, or reopening Deport.

### OWNER QUESTION 3: After annexation, does the town stay Israeli for good?

The GDD leaves defence and upkeep after annexation open.

- **(a) Yes, for good (Recommended).** The occupation ends: its upkeep stops and no unit has to stay. Enemy units treat it
  like any Israeli town (today they cannot enter one; enemy captures of Israeli towns are #1305's work).
- **(b) Only while a unit stays.** If the last Israeli unit leaves, the town returns to enemy control, as an occupation
  does today.
- **(c) For good, but the upkeep continues** every turn, as during the occupation.

Recommendation: (a). It makes annexation a finished state without inventing a new defence system or a hidden cost.

## Rule IDs (the Phase 6 plan's numbering)

- **R-CTRL-014:** `EndOccupation(settlement, outcome)` is one deferred command kind with one `attention_cost.end_occupation`.
  It is valid only for a place in `awaiting_end` with an Israeli military unit on its hex and an outcome of `annex` or
  `withdraw`; otherwise it is refused with a reason, the state unchanged and the Attention refunded.
- **R-CTRL-015:** No puppet regime (Q-PUP-1 (c)). `SettlementControl.EndOptions` offers Annex and Withdraw, in that order,
  with no default; Withdraw is R-CTRL-010's withdrawal.
- **R-CTRL-016:** Annexation and the residents (Q-ANNEX-1). The record becomes `owner=israel,state=annexed`, keeping its
  capture date, with no timer. A fled place adds nothing else. A stayed place adds one city as OWNER QUESTION 1 sets.
  Upkeep and defence follow OWNER QUESTION 3.
- **R-CTRL-017:** A depopulated place (Deport) is never offered or given `EndOccupation` (Q-ANNEX-2 (a)), so it is never
  annexed.

## Global constraints

- No puppet constant, state, option, string or alias.
- A fled annexation adds no city, people, housing, jobs, Food, Money, Arms, indicator change, score or objective progress.
- Every number for a stayed annexation is sourced or an owner-approved labelled design value. Never infer a 1948 count
  from the 1945 figures in `content/map/settlement_population_1945.yaml`.
- The resident outcome is factual content: each changed row in `content/settlement_targets.yaml` goes back to
  `status: draft` until the owner's sensitivity review. An AI-assisted commit never sets `sensitivity-reviewed`.
  **This blocks shareable builds** (`ReleaseGate.SettlementTargetsUnchecked`) from the merge until the owner re-reviews the
  six rows, so the PR lands outside a release window.
- Deterministic: ordinal order, no RNG draw, invariant formatting. A new state field is written only when not default.
- `EndOccupation` is deferred (not in `Simulation.IsInstant`), as `ResolveCapture` is (Civ movement plan, spec 3.2).
- Every new R-CTRL rule has a citing test; `uv run scripts/oss/rule_ids.py` passes.
- `changelog.d/799.md`. No em dashes. They/them for people.
- Out of scope: the end-of-occupation dialog (#802), map markers (#803), Hebrew UI strings, follow-up cards, enemy
  captures of Israeli towns (#1305), diplomacy, puppet regimes.

## Overlap with work in flight

- **Civ movement (#1398):** S5c and S6 merged as #1517. `Stamp` rebuilds records with the positional constructor; any new
  `SettlementControl` field must be passed there too (only under OWNER QUESTION 2 (b)). The U1 to U5 Unity branch
  (`civ/u1-u5-movement-ui`, not yet a PR) rewrites `SimBridge.cs` (the queue becomes committed plus deferred orders) and
  U6 (Codex, the capture dialog after a capturing move) touches `CaptureDialogPresenter`. #799's only Unity change is two
  lines in `SimBridge.Key` and `SimBridge.Validate`; whichever lands second takes the other's `SimBridge.cs` as base and
  re-adds its lines. `EndOccupation` stays deferred, so U1's committed list never holds it.
- **#1305 (enemy captures of Israeli towns, R-CTRL-006):** under OWNER QUESTION 3 (a) an annexed town is an Israeli town
  for the enemy, so #1305's rule covers it once built. Task 3 makes `Movement.NoEntry` treat an annexed target as Israeli
  (closed to the enemy, open to the player, not an "enemy-held locality" for retreat or withdrawal). #1305 touches the
  same helper; whichever lands second keeps both conditions.
- **Advisors S1 (`feat/advisors-s1`):** Unity only, no settlement code. No overlap.

## Tasks

### Task 0: Record the answers (docs only)

**Files:** `docs/GDD.md` (the "After X turns of occupation" paragraph and the Settlement control row), 
`docs/gdd/settlement-control.md` (lines 19 to 22 and the R-CTRL references), `docs/superpowers/plans/2026-10-15-gdd-phase-6.md`
(PR 7: drop `Puppet` and R-CTRL-015 "the puppet regime"), `docs/oss/DECISIONS.md`, and this plan.

- [ ] Copy the three answers, with links, as "Then decided (owner, <date>, "<words>"; <link>)" lines, the repository's
  pattern. The GDD says: no puppet regime in the MVP; X is six monthly turns; annex or withdraw; what annexation adds per
  OWNER QUESTIONS 1 and 2; defence and upkeep per OWNER QUESTION 3.
- [ ] `uv run scripts/oss/gdd_lint.py`, `uv run scripts/oss/rule_ids.py`, `uv run scripts/oss/check_repo.py`.

### Task 1: The resident outcome in each target row

**Files:** `sim/Statecraft.Sim/Content.cs` (`SettlementTarget`), `sim/Statecraft.Sim/ContentLoader.cs`
(`ValidateSettlementTargets`), `content/settlement_targets.yaml`, `sim/Statecraft.Sim.Tests/CaptureTests.cs` (the fixture
row helper `Target`), `ContentLoaderTests.cs`, `RealContentTests.cs`.

**Shape** (under OWNER QUESTION 1 (a); (b) uses the same block with `status: design` values):

```yaml
  - id: acre
    ...
    residents: stayed              # fled or stayed (Q-ANNEX-1); fled is the default the ruling sets
    residents_sources:             # at least one: the per-place sourcing the ruling asks for
      - { url: "https://...", checked: 2026-10-24 }
    annexation:                    # stayed only: what joins the player's population (OWNER QUESTION 1)
      people: { value: <approved>, status: sourced, date: <day>, sources: [{ url: "https://...", checked: <day> }] }
      employed_share: { value: 0.439, status: design, sources: [...] }   # DESIGN VALUE: the national share (Q-START-1), as cities.yaml's seven towns
    status: draft
```

`SettlementTarget` gains `string Residents`, `List<Source> ResidentsSources` and `TargetAnnexation? Annexation` (with
`CityValue? People` and `CityValue? EmployedShare`). Rules, each a `ContentException` naming the file and field:
`residents` is `fled` or `stayed`; `residents_sources` has at least one https source with a `checked` date (the existing
source check); `annexation` is required for `stayed` and refused for `fled`; `people` is a whole number (`Whole`) and
`employed_share` a share from 0 to 1, each with the same status and source rules as a `cities.yaml` value. Housing, jobs and condition are not stored: under (a) they follow
the starting-city rule (Task 3).

- [ ] **Red tests** in `ContentLoaderTests`: a valid stayed row loads with its people; a missing `residents`, an unknown
  value (`puppet`, `unknown`), no `residents_sources`, a fled row with an `annexation` block and a stayed row without one
  are each refused with the field named.
- [ ] **Fixture:** `CaptureTests.Target` writes `residents: fled` and one fixture source on every row (fixture URLs only);
  add `internal static Content LoadStayed(string id, int people)` that rewrites that one row as `stayed` with an
  `annexation` block of fixture design values (`people`, and `employed_share: 0.5`), through the existing `Load(Action<Dictionary<string, string>>, ...)` overload.
- [ ] **Real rows:** acre and nazareth `stayed`; jaffa, lod, ramla and beersheba `fled`; sources from
  `research_notes/GDD phase 6/settlement-targets.md` and the rulings. Each row goes to `status: draft`. Leave `people`
  empty and the PR red until OWNER QUESTION 1's numbers are approved; never put a placeholder number in content.
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

### Task 2: `EndOccupation`, its options and every touch-point

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
    internal static GameState AwaitingEnd(Content c, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, s.Date);
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
        var s = AwaitingEnd(c, CaptureTests.Soldier(1, 1, 0));
        Assert.Equal(new[] { SettlementControl.Annex, "withdraw" },
            SettlementControl.EndOptions(c, s, "town_a").Select(x => x.Outcome).ToArray());
        Assert.NotNull(SettlementControl.End(c, s, "town_a", "puppet", new List<Cause>()));
    }

    // R-CTRL-014: annexing needs the unit that holds the place (R-CTRL-011's garrison).
    [Fact]
    public void AnnexNeedsAnIsraeliUnitOnThePlace()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, CaptureTests.Soldier(1, 2, 0));
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

### Task 3: Withdraw and annex

**Files:** `SettlementControl.cs`, `Movement.cs` (`NoEntry`), `Cities.cs` (new `Annex`), `Training.cs`, `Cities.cs`
(`Apportion`, unchanged behaviour), `EndOccupationTests.cs`, `MovementTests.cs`, `docs/rules/mvp.md` (R-CTRL-015 to
R-CTRL-017, and R-MOVE-005, R-ENEMY-001, R-CTRL-001's out-of-date sentence).

- `End(..., "withdraw")` calls the existing `Withdraw(c, s, id, why, false)`: same moves, same refusal.
- `End(..., Annex)`: refuse unless garrisoned; then, for `stayed`, `Cities.Annex` first (a refusal leaves the state as it
  was); then replace the record with `new SettlementControl(Movement.PlayerSide, Annexed, x.Since)`. One sober Why line:
  fled, "{name} is annexed. Its residents had fled, so no people, homes or jobs are added"; stayed, "{name} is annexed.
  Its residents join the population". Both wait for the owner's sensitivity review with the rows.
- Under OWNER QUESTION 3 (a): `Tick` already ignores any state but `occupied` and `awaiting_end`, so upkeep and the
  garrison check stop with no change. `Movement.NoEntry` drops an annexed target from the enemy-held set (closed to the
  enemy, not an enemy-held locality for the player's withdrawal and retreat).
- `Cities.Annex(Content c, GameState s, SettlementTarget t, List<Cause> why) -> string?` under OWNER QUESTION 1 (a):
  `people = Cities.StartUnits(t.Annexation.People!.Value, ppu)`, the employed converted as `cities.yaml` does (R-CITY-007,
  people times `employed_share` to the nearest unit), `housing = people`, `jobs = employed`, condition `mvp.housing_start_condition`,
  no refugees, recruits or craftsmen. Refused if `s.Cities` already has the id.
- **A city that exists only in state:** `Training.cs:46` becomes `c.Cities.FirstOrDefault(...)?.Facilities.Contains(f) == true`
  (an annexed city has no facility); `Cities.Apportion` keeps spreading arrivals over the starting cities only (existing
  rule, no change); check every other `c.Cities` reader listed above with a test that runs a turn with an annexed city.

- [ ] **Red tests:**

```csharp
// R-CTRL-015: withdrawing at the end is the Raid withdrawal: the units leave, the record goes.
[Fact]
public void WithdrawAtTheEndIsTheRaidWithdrawal()
{
    var c = CaptureTests.Load(CaptureTests.AllTargets);
    var s = AwaitingEnd(c, CaptureTests.Soldier(2, 1, 0));
    var after = Simulation.Step(c, s, new Command[] { new EndOccupation("town_a", "withdraw") });
    Assert.Empty(after.Rejected);
    Assert.False(after.State.Settlements.ContainsKey("town_a"));
    Assert.NotEqual((1, 0), (after.State.Units[2].Q, after.State.Units[2].R));
}

// R-CTRL-016: a place emptied by flight is territory only, no gain ("Territory only, no gain", owner, 18 October 2026).
[Fact]
public void AnnexingAFledPlaceChangesOnlyItsRecord()
{
    var c = CaptureTests.Load(CaptureTests.AllTargets);   // every fixture row is fled
    var s = AwaitingEnd(c, CaptureTests.Soldier(1, 1, 0));
    var before = s.Clone();
    Assert.Null(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
    Assert.Equal((Movement.PlayerSide, SettlementControl.Annexed, before.Settlements["town_a"].Since, 0),
        (s.Settlements["town_a"].Owner, s.Settlements["town_a"].State, s.Settlements["town_a"].Since, s.Settlements["town_a"].Turns));
    s.Settlements["town_a"] = before.Settlements["town_a"];
    Assert.Equal(before.Describe(), s.Describe());   // nothing else moved: resources, cities, units, score, objectives
    Assert.Equal(before.RngState, s.RngState);
}

// R-CTRL-016, OWNER QUESTION 3 (a): an annexed place pays no upkeep and needs no unit.
[Fact]
public void AnAnnexedPlaceCostsNothingAndStaysWithoutAUnit()
{
    var c = CaptureTests.Load(CaptureTests.AllTargets);
    var s = AwaitingEnd(c, CaptureTests.Soldier(1, 1, 0));
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
    var c = CaptureTests.LoadStayed("town_a", people: 60);
    var s = AwaitingEnd(c, CaptureTests.Soldier(1, 1, 0));
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
- [ ] Run `EndOccupationTests`, `RaidTests`, `OccupyTests`, `DeportTests`, `DeportNeverDominatesTests`, `CaptureTests`,
  `MovementTests`, then the suite. `CalibrationTests` stays bit-for-bit (the reference player always Raids).

### Task 4: One real-content scenario, rules text, changelog

- [ ] `sim/Statecraft.Sim.Tests/scenarios/mvp-annex-nazareth.yaml`: `mvp-occupy-cost`'s two steps, then
  `{ kind: end_occupation, settlement: nazareth, outcome: annex }` on 1948-12-15, `until: 1949-01-15`. Bless only the new
  golden with `STATECRAFT_UPDATE_GOLDEN=1`, inspect it, add its history row in `docs/determinism.md` and its SMOKE line in
  `RELEASING.md` if that file lists scenario pins. No other golden may move.
- [ ] `docs/rules/mvp.md`: R-CTRL-014 to R-CTRL-017, R-MOVE-005 and R-ENEMY-001 (annexed targets), and R-CTRL-001's
  status sentence (the rows were sensitivity-reviewed on #793, and #799 returns the changed ones to draft).
- [ ] `changelog.d/799.md`, factual: when an occupation period ends the player can annex or withdraw; no puppet regime.

### Task 5: Review and gates

- [ ] Review packet for the owner: the six rows' outcomes and sources, the approved people counts and their sources, and
  the two Why lines. The owner sets `sensitivity-reviewed` in their own commit after reading them.
- [ ] `dotnet test sim/Statecraft.sln` (0 warnings), the tools and policy suites (AGENT-RULES commands), `bash scripts/check.sh origin/main`
  ending "ALL CHECKS PASSED".
- [ ] Unity: `uv run tools/build_sim_for_unity.py`, then a full EditMode run under `tools/machine_lock.py` (the DLL and
  `SimBridge.cs` change); XML total > 0, 0 NEW failures. No screenshots: nothing on screen changes until #802 and #803.

## Done when

- The owner's three answers are in `docs/GDD.md` and #799.
- Each target row has a sourced fled or stayed outcome, reviewed by the owner.
- `EndOccupation` has every touch-point; Withdraw is the Raid withdrawal; a fled annexation changes only the record; a
  stayed annexation adds exactly the approved city; a depopulated place is never annexed.
- R-CTRL-014 to R-CTRL-017 have citing tests; `scripts/check.sh` and the full EditMode run pass.
