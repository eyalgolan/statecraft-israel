using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1700 with #1703 (GDD 0.3 housing repairs), under gdd-0.3 only: R-HOUSE-006. A repair is a city project costing
/// 0.5 Money and 1 work that raises its city's housing condition by 10 percentage points, never above the maximum, and uses
/// the turn's city start (owner, 1 November 2026: "Fixed price 0.5 Money + 1 work"; docs/GDD.md "Housing repair price"). It
/// is done on the city itself, with no hex of its own (owner, 3 November 2026: "No hex needed (Recommended)"), so it leaves
/// no record once done. The fixture's town_a is on (1, 0); its land in reach is (1, 0), (2, 0), (1, 1) and (0, 1).</summary>
public class Gdd03HousingRepairTests
{
    const string Repair = "housing_repair";
    /// <summary>The real projects.yaml row, so every test runs the shipped values.</summary>
    static string Row => File.ReadAllLines(Path.Combine(RealContentTests.ContentRoot(), "projects.yaml")).Single(l => l.Contains("id: housing_repair")) + "\n";

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static Content Load(bool gdd03, string? projects = null) => ArmsWorkshopTests.Load(gdd03, projects ?? Fixture.GddProjects + Row);

    /// <summary>A gdd-0.3 game whose town_a houses its people with room to spare (no overflow, so R-HOUSE-005 moves no
    /// condition) at <paramref name="condition"/>, with <paramref name="civilians"/> civilians, <paramref name="employed"/> of
    /// them in jobs.</summary>
    static (Content C, GameState S) Game(double condition = 0.35, int civilians = 10, int employed = 8)
    {
        var c = Load(true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = new City(x.Id, 100, employed, civilians, employed, 0, 0, condition, jobsDefault: false);
        return (c, s);
    }

    static StartProject Start(int q = 1, int r = 0) => new StartProject(Repair, "town_a", q, r);

    static double Condition(GameState s) => s.Cities["town_a"].Condition!.Value;

    // R-HOUSE-006: the row is read under gdd-0.3 only; gdd-0.1 keeps exactly its kinds.
    [Fact]
    public void TheRepairIsAKindUnderGdd03Only()
    {
        Assert.Equal(new[] { "housing", "transit_camp", "industry" }, Load(false).ProjectKinds.Select(k => k.Id));
        var k = Load(true).ProjectKinds.Single(x => x.Id == Repair);
        Assert.Equal((0.5, 1, 0, 0.1, 0, 0), (k.Cost, k.Turns, k.Capacity, k.Condition, k.JobSlots, k.WorkerSlots));
        Assert.Equal(new[] { SaveGame.Gdd03Ruleset }, k.Rulesets);
    }

    // R-HOUSE-006: the real row is the ruled repair (content/gdd03-balance.yaml housing_repair: cost 0.5, work 1,
    // condition_pp 10), and the loader refuses a row that does not match that catalogue under gdd-0.3.
    [Fact]
    public void TheRealRepairRowMatchesTheCatalogue()
    {
        var k = Load(true).ProjectKinds.Single(x => x.Id == Repair);
        // The owner approved its Hebrew name on 3 November 2026 (#564), so the row has no name_he_review line.
        Assert.Equal(("שיפוץ דיור", "", false), (k.NameHe, k.NameHeReview, k.NameHeReviewGiven));
        // The real game (gdd-0.1) has no repair.
        Assert.DoesNotContain(ContentLoader.Load(RealContentTests.ContentRoot()).ProjectKinds, x => x.Id == Repair);
        foreach (var (from, to, field, says) in new[]
        {
            ("cost: 0.5", "cost: 0.6", "cost", "balance catalogue's housing_repair cost"),
            ("turns: 1", "turns: 2", "turns", "balance catalogue's housing_repair work"),
            ("condition: 0.1", "condition: 0.2", "condition", "balance catalogue's housing_repair condition_pp"),
            ("capacity: 0", "capacity: 1", "capacity", "adds no capacity"),
            ("job_slots: 0", "job_slots: 1", "job_slots", "adds no job slots"),
            ("rulesets: [gdd-0.3]", "rulesets: [gdd-0.1, gdd-0.3]", "rulesets", "under gdd-0.3 only"),
        })
        {
            Assert.Contains(from, Row);
            var e = Assert.Throws<ContentException>(() => Load(true, Fixture.GddProjects + Row.Replace(from, to)));
            Assert.Contains($"projects[housing_repair].{field}", e.Message);
            Assert.Contains(says, e.Message);
        }
        // A row with no rulesets would be a gdd-0.1 kind: refused there too.
        Assert.Contains("under gdd-0.3 only", Assert.Throws<ContentException>(() => Load(false, Fixture.GddProjects + Row.Replace(", rulesets: [gdd-0.3]", ""))).Message);
    }

    // R-HOUSE-006 with R-PROJ-001 and R-MONEY-003: the repair is paid in full at the start, borrowing the shortfall.
    [Fact]
    public void ARepairCostsHalfAMoneyAndBorrowsTheRest()
    {
        var (c, s) = Game();
        s.Treasury = 0.2; s.Debt = 0;
        var r = Step(c, s, Start());
        Assert.Empty(r.Rejected);
        Assert.Equal(-0.5, r.Why.Single(w => w.Key == CauseKeys.Paid && w.Args[0] == CauseKeys.Purpose.Project).Delta, 9);
        Assert.Equal(0.3, r.Why.Single(w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.Project).Delta, 9);
    }

    // R-HOUSE-006 with R-CITY-011 and R-PROJ-003: with a builder, the repair is done in its one turn of work and adds 10 pp
    // of condition; its builder is free again.
    [Fact]
    public void ARepairWithABuilderRaisesConditionTenPoints()
    {
        var (c, s) = Game(0.35);
        Assert.Equal(0.35, Condition(Step(c, s).State), 9);   // precondition: no overflow, so the turn alone moves nothing
        var r = Step(c, s, Start());
        Assert.Empty(r.Rejected);
        Assert.Equal(0.45, Condition(r.State), 9);
        Assert.Equal(0, Projects.Builders(r.State, "town_a"));
        Assert.Contains(r.Why, w => w.Key == CauseKeys.ProjectBuilder && w.Args.Single() == Repair);
        Assert.Contains(r.Why, w => w.Key == CauseKeys.ProjectCompleted && w.Args.Single() == Repair);
        Assert.Contains(r.Why, w => w.Key == CauseKeys.ProjectBuilderFreed && w.Args.Single() == Repair);
    }

    // R-HOUSE-006: never above the maximum.
    [Fact]
    public void ARepairNeverTakesConditionAboveOne()
    {
        var (c, s) = Game(0.95);
        Assert.Equal(1.0, Condition(Step(c, s, Start()).State), 9);
    }

    // R-HOUSE-006 with R-CITY-011 and R-PROJ-003: with nobody free at the start, the repair waits with no builder and
    // changes nothing; once the player puts a builder on it, it is done the next turn.
    [Fact]
    public void ARepairWithNoBuilderWaits()
    {
        var (c, s) = Game(0.35, civilians: 10, employed: 10);
        var state = Step(c, s, Start()).State;
        var id = state.CityProjects.Keys.Single();
        for (int i = 0; i < 3; i++)
        {
            Assert.False(state.CityProjects[id].Builder);
            Assert.Equal((1, (DateTime?)null), (state.CityProjects[id].TurnsLeft, state.CityProjects[id].BuiltOn));
            Assert.Equal(0.35, Condition(state), 9);
            state = Step(c, state).State;
        }
        var x = state.Cities["town_a"];
        state.Cities["town_a"] = new City(x.Id, x.HousingCapacity, x.JobSlots, x.Civilians + 1, x.Employed, 0, 0, x.Condition, jobsDefault: false);
        var built = Step(c, state, new AssignWorkers("town_a", Projects.BuildRole, 1));
        Assert.Empty(built.Rejected);
        Assert.Equal(0.45, Condition(built.State), 9);
        Assert.Empty(built.State.CityProjects);
    }

    // R-HOUSE-006 with R-PROJ-001 (owner, 3 November 2026: "One per city, plus 2 nationally"): a repair uses the city's one
    // start of the turn, and counts toward the national map_actions_per_turn.
    [Fact]
    public void ARepairUsesTheCitysStart()
    {
        var (c, s) = Game();
        var both = Step(c, s, new StartProject("housing", "town_a", 1, 1), Start());
        var refused = Assert.Single(both.Rejected);
        Assert.Equal(Repair, ((StartProject)refused.Command).ProjectKind);
        Assert.Equal("Town A has started a city project this turn", refused.Reason);
        Assert.Single(both.Why, w => w.Key == CauseKeys.Paid && w.Args[0] == CauseKeys.Purpose.Project);
        // The refused repair changed nothing: the state is as after the first start alone (as CityStartCapTests does for R-PROJ-001).
        Assert.Equal(Step(c, s, new StartProject("housing", "town_a", 1, 1)).State.Hash(), both.State.Hash());

        var full = s.Clone();
        full.MapActionsTurnDate = full.Date; full.MapActionsThisTurn = c.RulesAt(full.Date).MapActionsPerTurn;
        Assert.Equal("turn_cap", Projects.CheckCityReason(c, full, Repair, "town_a", 1, 0)?.Kind);
    }

    // R-HOUSE-006 (owner, 3 November 2026: "No hex needed (Recommended)", departing from Q-PROJ-2 for repairs only): with
    // every hex in reach built on, the city itself included, a repair still starts; once done it leaves no record, so the
    // hex still shows its own project, and the next turn another repair starts.
    [Fact]
    public void ARepairNeedsNoFreeHexAndLeavesNone()
    {
        var (c, s) = Game(0.35);
        foreach (var (q, r) in new[] { (1, 0), (2, 0), (1, 1), (0, 1) })
        {
            var built = s.NextId++;
            s.CityProjects.Add(built, new CityProject(built, "housing", "town_a", q, r, 0, s.Date));
        }
        Assert.Equal("occupied_project", Projects.CheckCityReason(c, s, "housing", "town_a", 1, 0)?.Kind);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = new City(x.Id, 100, x.JobSlots, x.Civilians, x.Employed, 0, 0, x.Condition, jobsDefault: false);
        var one = Step(c, s, Start());
        Assert.Empty(one.Rejected);
        Assert.Equal(0.45, Condition(one.State), 9);
        Assert.Equal(4, one.State.CityProjects.Count);
        Assert.DoesNotContain(one.State.CityProjects.Values, p => p.Kind == Repair);
        Assert.Equal("housing", Projects.CityProjectAt(one.State, 1, 0)?.Kind);
        var two = Step(c, one.State, Start());
        Assert.Empty(two.Rejected);
        Assert.Equal(0.55, Condition(two.State), 9);
    }

    // R-HOUSE-006: a repair is done on the city itself, so a neighbouring hex is refused, with the state unchanged.
    [Fact]
    public void ARepairIsOnlyOnTheCitysOwnHex()
    {
        var (c, s) = Game();
        Assert.Equal("repair_city_hex", Projects.CheckCityReason(c, s, Repair, "town_a", 1, 1)?.Kind);
        var r = Step(c, s, Start(1, 1));
        Assert.Equal("A housing repair is done on Town A's own hex", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-HOUSE-006 with R-PROJ-002: a repair being built can be cancelled; the Money is not returned and its builder is free
    // again at once.
    [Fact]
    public void CancellingARepairKeepsTheMoneySpent()
    {
        var (c, s) = Game(0.35);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, Repair, "town_a", 1, 0, 1, builder: true));
        var cancel = Step(c, s, new CancelProject(id));
        Assert.Empty(cancel.Rejected);
        Assert.Empty(cancel.State.CityProjects);
        Assert.Equal(0, Projects.Builders(cancel.State, "town_a"));
        Assert.Contains(cancel.Why, w => w.Key == CauseKeys.ProjectBuilderFreed && w.Args.Single() == Repair);
        Assert.Equal(0.35, Condition(cancel.State), 9);
        var none = s.Clone(); none.CityProjects.Clear();
        Assert.Equal(Step(c, none).State.Treasury, cancel.State.Treasury, 9);
    }

    // R-HOUSE-006 with R-CITY-010: in a city the enemy holds, a repair gains no progress: its record and the condition stay.
    [Fact]
    public void ARepairInAHeldCityWaits()
    {
        var (c, s) = Game(0.35);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, Repair, "town_a", 1, 0, 1, builder: true));
        s.Units.Add(s.NextId, new Unit(s.NextId++, "enemy", 1, 0, side: "egypt", strength: 3));
        var held = Step(c, s).State;
        Assert.Equal((1, (DateTime?)null, true), (held.CityProjects[id].TurnsLeft, held.CityProjects[id].BuiltOn, held.CityProjects[id].Builder));
        Assert.Equal(0.35, Condition(held), 9);
    }

    // R-HOUSE-006: the real game (gdd-0.1) has no repair, with the row in projects.yaml: StartProject on a city's own hex
    // refuses it as unknown and nothing changes.
    [Fact]
    public void Gdd01HasNoRepair()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(SaveGame.GddRuleset, c.Ruleset);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var home = c.SettlementHexes.First(h => s.Cities.ContainsKey(h.Id));
        var r = Step(c, s, new StartProject(Repair, home.Id, home.Q, home.R));
        Assert.Equal("There is no such city project", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }
}
