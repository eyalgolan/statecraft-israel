using Statecraft.Sim;

/// <summary>#1693 pioneer slice: the fixture-only turn-end producer that turns pioneer project advancement into work XP on
/// the population ledger (R-VET-001, R-VET-002). Synthetic fixtures plus the Historical fixture of #2323; no game-loop
/// caller, so Simulation.NewGame, Projects.cs and Corridors.cs are untouched. Owner answers of 3 November 2026 on #1693:
/// no tools charge in the MVP (1), course-trained pioneers only (2), tier and work XP kept dormant off the job (4), and
/// a corridor segment counts as a pioneer project (5).</summary>
public class Gdd03PioneerWorkFixtureTests
{
    static readonly PopulationLocation Here = new PopulationLocation("fixture");

    static Content Catalogue()
    {
        var c = PopulationLedgerTests.Content();
        c.Gdd03Balance = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance;
        return c;
    }

    // A course-trained pioneer (answer 2) at work on a target, with reception done and an independent military history.
    static PopulationUnit Pioneer(string target, int xp = 0, CivilianTier tier = CivilianTier.Trained, int combatXp = 0)
        => new PopulationUnit(0, PopulationStatus.FreeCivilian, 6, Here, new PopulationAssignment(PopulationAssignmentKind.Work, target),
            CivilianProfession.Pioneer, tier, xp, "recruit", combatXp, PopulationEquipment.None);

    // Record ids follow the row order: 0, 1, 2, ...
    static GameState State(Content c, params PopulationUnit[] people) => PopulationLedger.CreateFixture(c,
        new PopulationBindings(new[] { Here },
            new[] {
                new PopulationTarget("site-a", PopulationAssignmentKind.Work, "fixture"),
                new PopulationTarget("segment-b", PopulationAssignmentKind.Work, "fixture"),
                new PopulationTarget("road-t", PopulationAssignmentKind.Travel, "fixture"),
                new PopulationTarget("course-c", PopulationAssignmentKind.Course, "fixture", 3) },
            Array.Empty<string>(), new[] { "recruit" }),
        people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)));

    static PioneerWorkTurn Resolve(Content c, GameState s, params string[] advancing)
        => Gdd03PioneerWorkFixture.ResolveTurnEnd(c, s, advancing);
    static GameState Turns(Content c, GameState s, int n, params string[] advancing)
    {
        for (int i = 0; i < n; i++) s = Resolve(c, s, advancing).State;
        return s;
    }

    // R-VET-001, answer 5: a site and a corridor segment that advance give one tick a turn; idle, travel, a farmer and a
    // pioneer still in reception give none and add no work, and a turn whose project does not advance gives none. One turn-start snapshot resolved twice still gives one tick.
    [Fact]
    public void Gdd03PioneerGainsWorkXpOnlyOnTurnsItsProjectAdvances()
    {
        var c = Catalogue();
        var idle = Pioneer("site-a").WithPlacement(PopulationStatus.FreeCivilian, Here, PopulationAssignment.Unassigned, null, null);
        var travel = Pioneer("site-a").WithPlacement(PopulationStatus.CivilianInTransit, Here,
            new PopulationAssignment(PopulationAssignmentKind.Travel, "road-t"), null, null);
        var farmer = new PopulationUnit(0, PopulationStatus.FreeCivilian, 6, Here,
            new PopulationAssignment(PopulationAssignmentKind.Work, "site-a"), CivilianProfession.Farmer, CivilianTier.Trained, 0,
            "", 0, PopulationEquipment.None);
        var start = State(c, Pioneer("site-a"), Pioneer("segment-b"), idle, travel, farmer, Pioneer("site-a").WithReception(0));
        var before = start.Describe();

        var first = Resolve(c, start, "site-a", "segment-b");
        Assert.Equal(new[] { ("segment-b", 1m), ("site-a", 1m) }, first.WorkByTarget.Select(x => (x.Key, x.Value)));
        var once = first.State;
        var twice = Resolve(c, start, "site-a", "segment-b").State;
        Assert.Equal(before, start.Describe());
        Assert.Equal(once.Describe(), twice.Describe());
        Assert.Equal(1, once.PopulationLedger.Get(0).WorkXp);

        var s = Turns(c, start, 3, "site-a", "segment-b");
        Assert.Equal(new[] { 3, 3, 0, 0, 0, 0 }, s.PopulationLedger.Units.Select(p => p.WorkXp));
        s = Turns(c, s, 1);   // no project advances this turn
        Assert.Equal(new[] { 3, 3, 0, 0, 0, 0 }, s.PopulationLedger.Units.Select(p => p.WorkXp));
        s = Turns(c, s, 1, "segment-b");
        Assert.Equal(new[] { 3, 4, 0, 0, 0, 0 }, s.PopulationLedger.Units.Select(p => p.WorkXp));
    }

    // R-VET-001: veteran at profession.work_to_veteran (9) working turns, not the six service turns of gdd-0.1. The ninth
    // turn's work is quoted at the turn-start tier; the record is veteran from the next turn, same id and project.
    [Fact]
    public void Gdd03PioneerBecomesVeteranAfterNineWorkingTurnsNotSix()
    {
        var c = Catalogue();
        var start = State(c, Pioneer("site-a"));
        var p = start.PopulationLedger.Get(0);
        var s = Turns(c, start, 6, "site-a");
        Assert.Equal(6, s.PopulationLedger.Get(0).WorkXp);
        Assert.Equal(CivilianTier.Trained, s.PopulationLedger.Get(0).Tier);
        s = Turns(c, s, 2, "site-a");
        var ninth = Resolve(c, s, "site-a");
        Assert.Equal(1m, ninth.WorkByTarget["site-a"]);
        var vet = ninth.State.PopulationLedger.Get(0);
        Assert.Equal(9, vet.WorkXp); Assert.Equal(CivilianTier.Veteran, vet.Tier);
        Assert.Equal(p.Id, vet.Id); Assert.Equal(p.Assignment.Canonical(), vet.Assignment.Canonical());
        Assert.Equal(1.5m, Resolve(c, ninth.State, "site-a").WorkByTarget["site-a"]);
    }

    // R-VET-002: the work quote reads pioneer.work_rate at the turn-start tier (novice 0.75, trained 1, veteran 1.5), not
    // veteran_progress 2. Only advancing targets are quoted.
    [Fact]
    public void Gdd03VeteranPioneerWorksAtCatalogueRateNotDoubleProgress()
    {
        var c = Catalogue();
        var s = State(c, Pioneer("site-a"), Pioneer("site-a", 9, CivilianTier.Veteran), Pioneer("segment-b", 9, CivilianTier.Veteran));
        var turn = Resolve(c, s, "site-a");
        Assert.Equal(2.5m, turn.WorkByTarget["site-a"]);
        Assert.Equal(0.75m, Resolve(c, State(c, Pioneer("site-a", 0, CivilianTier.Novice)), "site-a").WorkByTarget["site-a"]);
        Assert.False(turn.WorkByTarget.ContainsKey("segment-b"));
        Assert.Equal(10, turn.State.PopulationLedger.Get(1).WorkXp);
        Assert.Equal(CivilianTier.Veteran, turn.State.PopulationLedger.Get(1).Tier);
    }

    // R-QUAL-003, R-QUAL-005: a pioneer taken off the job and put back keeps its id and work XP, and earns none while idle.
    [Fact]
    public void Gdd03ReturnAndReassignKeepPioneerIdentityAndXp()
    {
        var c = Catalogue();
        var s = Turns(c, State(c, Pioneer("site-a")), 4, "site-a");
        s.PopulationLedger.ChangeAssignment(0, PopulationAssignment.Unassigned);
        s = Turns(c, s, 1, "site-a");
        Assert.Equal(4, s.PopulationLedger.Get(0).WorkXp);
        s.PopulationLedger.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "segment-b"));
        Assert.Equal(4, s.PopulationLedger.Get(0).WorkXp); Assert.Equal(0, s.PopulationLedger.Get(0).Id);
        s = Turns(c, s, 1, "segment-b");
        Assert.Equal(5, s.PopulationLedger.Get(0).WorkXp);
    }

    // R-QUAL-006, answer 4: a pioneer a Train order takes into a course keeps its tier and work XP dormant and earns no work
    // XP on course turns; back at work it continues from the same count.
    [Fact]
    public void Gdd03TrainOrderKeepsPioneerTierAndWorkXpDormant()
    {
        var c = Catalogue();
        var s = Turns(c, State(c, Pioneer("site-a")), 4, "site-a");
        s.PopulationLedger.Transition(0, PopulationStatus.Trainee, Here, new PopulationAssignment(PopulationAssignmentKind.Course, "course-c"));
        Assert.Empty(Resolve(c, s, "site-a", "course-c").WorkByTarget);
        s = Turns(c, s, 2, "site-a", "course-c");
        Assert.Equal(4, s.PopulationLedger.Get(0).WorkXp);
        Assert.Equal(CivilianTier.Trained, s.PopulationLedger.Get(0).Tier);
        Assert.Equal(CivilianProfession.Pioneer, s.PopulationLedger.Get(0).Profession);
        s.PopulationLedger.Transition(0, PopulationStatus.FreeCivilian, Here, new PopulationAssignment(PopulationAssignmentKind.Work, "site-a"));
        s = Turns(c, s, 1, "site-a");
        Assert.Equal(5, s.PopulationLedger.Get(0).WorkXp);
    }

    // R-VET-001, R-UNIT-011: work promotion leaves combat XP and qualification as they were, and combat XP changes
    // neither work XP, tier nor the work quote.
    [Fact]
    public void WorkXpAndCombatXpAreIndependent()
    {
        var c = Catalogue();
        var s = Resolve(c, State(c, Pioneer("site-a", 8, combatXp: 7)), "site-a").State;
        var vet = s.PopulationLedger.Get(0);
        Assert.Equal(CivilianTier.Veteran, vet.Tier); Assert.Equal(7, vet.CombatXp); Assert.Equal("recruit", vet.Qualification);

        var a = State(c, Pioneer("site-a", 3, combatXp: 0));
        var b = State(c, Pioneer("site-a", 3, combatXp: 12));
        var ta = Resolve(c, a, "site-a"); var tb = Resolve(c, b, "site-a");
        Assert.Equal(ta.WorkByTarget["site-a"], tb.WorkByTarget["site-a"]);
        Assert.Equal(4, tb.State.PopulationLedger.Get(0).WorkXp); Assert.Equal(12, tb.State.PopulationLedger.Get(0).CombatXp);
        Assert.Equal(CivilianTier.Trained, tb.State.PopulationLedger.Get(0).Tier);
    }

    // R-POP-004 (#2323): the Historical ledger fixture has no pioneer at work, so the producer passes it through unchanged.
    // A game without an initialized ledger (gdd-0.1) is refused, so the aggregate halutz path is never touched.
    [Fact]
    public void HistoricalFixturePassesThroughAndGdd01IsRefused()
    {
        var c = HistoricalPopulationTests.Real(true);
        var s = HistoricalPopulation.LedgerFixture(c);
        var turn = Gdd03PioneerWorkFixture.ResolveTurnEnd(c, s, c.PlayerRegions.Regions.Keys);
        Assert.Equal(s.Describe(), turn.State.Describe());
        Assert.Empty(turn.WorkByTarget);

        var old = HistoricalPopulationTests.Real(false);
        Assert.Throws<InvalidOperationException>(() =>
            Gdd03PioneerWorkFixture.ResolveTurnEnd(old, Simulation.NewGame(old, 1, Mode.Historical), new[] { "site-a" }));
    }
}
