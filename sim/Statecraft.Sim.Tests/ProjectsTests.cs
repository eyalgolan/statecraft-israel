using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD slice's pioneer work (Phase 1 PR 5): PioneerWork(build_yishuv) (R-SITE-001) and StopWork (R-SITE-002) at a
/// designated site, and construction in TurnResolver (R-SITE-003, step 5 of spec 2026-10-12 section 2.3, D-A6). The answers to
/// #378's questions are the plan's recommendations, which the owner took on 14 October 2026.</summary>
public class ProjectsTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture: town_a at (1, 0) and site_a at (2, 0). A build takes 3 turns and costs IL 1.5m.</summary>
    static Content Town(Dictionary<string, string>? extra = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    /// <summary>A new game on <paramref name="c"/> with one idle halutz group at town_a's hex; its id is returned.</summary>
    static GameState WithHalutz(Content c, out int id, string type = "halutz")
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        id = s.NextId++;
        s.Units.Add(id, new Unit(id, type, 1, 0));
        return s;
    }

    static PioneerWork Build(int unit, string site = "site_a") => new PioneerWork(unit, Projects.BuildYishuv, site);

    // R-SITE-001
    [Fact]
    public void StartingTheBuildCommitsTheUnitAndPays()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        var r = Step(c, s, Build(id));
        Assert.Empty(r.Rejected);
        var work = r.State.Sites["site_a"];
        Assert.Equal(("site_a", (int?)id, 2, (DateTime?)null), (work.Site, work.Unit, work.TurnsLeft, work.BuiltOn));   // 3 turns, one of them this one
        Assert.True(Projects.IsBusy(r.State, id));
        Assert.Equal(Step(c, s).State.Treasury - 1.5, r.State.Treasury, 9);   // paid in full at the start (question 12)
        Assert.Equal((1, 0), (r.State.Units[id].Q, r.State.Units[id].R));   // the unit works from where it stands (question 9)
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Site A: a group of halutzim began building a yishuv, cost IL 1.5m");
    }

    public static IEnumerable<object[]> Refusals() => new List<object[]>
    {
        new object[] { "unknown unit", "There is no such unit" },
        new object[] { "not a halutz", "Only halutzim do pioneer work" },
        new object[] { "busy", "That group of halutzim is already building Site B" },
        new object[] { "unknown action", "There is no such pioneer work" },
        new object[] { "unknown site", "There is no such site" },
        new object[] { "not a yishuv site", "Site B is not a site for a new yishuv" },
        new object[] { "being built", "Site A is already being built" },
        new object[] { "built", "Site A is already built" },
    };

    /// <summary>Site B, on (3, 0), for the refusals that need a second site; <paramref name="kinds"/> its kinds.</summary>
    static string TwoSites(string kinds = "[yishuv]") => Fixture.GddSites + Fixture.GddSites.Replace("sites:\n", "")
        .Replace("site_a", "site_b").Replace("Site A", "Site B").Replace("q: 2", "q: 3").Replace("kinds: [yishuv]", "kinds: " + kinds)
        .Replace("    founded: 1949-06-12\n", kinds == "[yishuv]" ? "    founded: 1949-06-12\n" : "");   // only a yishuv has a founding date

    // R-SITE-001: each refusal leaves the state as a step with no order would, so Attention is refunded too. R-SITE-004:
    // "not a yishuv site" is build_yishuv at a field site.
    [Theory]
    [MemberData(nameof(Refusals))]
    public void TheLimitsAreRefusedAndChangeNothing(string limit, string reason)
    {
        var c = Town(new Dictionary<string, string> { ["sites.yaml"] = TwoSites(limit == "not a yishuv site" ? "[field]" : "[yishuv]") });
        var s = WithHalutz(c, out var id, limit == "not a halutz" ? "recruit" : "halutz");
        var other = s.NextId++;
        s.Units.Add(other, new Unit(other, "halutz", 1, 0));
        Command order = Build(id);
        switch (limit)
        {
            case "unknown unit": order = Build(999); break;
            case "busy": s.Sites["site_b"] = new SiteWork("site_b", id, 2, null); break;
            case "unknown action": order = new PioneerWork(id, "dig_well", "site_a"); break;   // build_road is a corridor action since #786
            case "unknown site": order = Build(id, "nowhere"); break;
            case "not a yishuv site": order = Build(id, "site_b"); break;
            case "being built": s.Sites["site_a"] = new SiteWork("site_a", other, 2, null); break;   // question 15: one unit per site
            case "built": s.Sites["site_a"] = SiteWork.Built("site_a", new DateTime(1948, 8, 15)); break;
        }
        var r = Step(c, s, order);
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-SITE-001: a short Treasury borrows the shortfall, as map actions do (question 13, owner decision "Allow borrowing",
    // 2026-10-15); a Treasury of exactly the cost borrows nothing.
    [Fact]
    public void AShortTreasuryBorrowsTheShortfall()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        s.Treasury = 1.4;
        s.Debt = 3;
        var why = new List<Cause>();
        Assert.Null(Projects.Start(c, s, id, Projects.BuildYishuv, "site_a", why));
        Assert.Equal(0, s.Treasury);
        Assert.Equal(3.1, s.Debt, 9);
        Assert.Single(s.Sites);
        Assert.Contains(why, x => x.System == "economy" && x.Target == "debt" && x.Message == "Borrowed IL 0.1m to build a yishuv");

        var exact = WithHalutz(c, out var other);
        exact.Treasury = 1.5;
        exact.Debt = 3;
        var none = new List<Cause>();
        Assert.Null(Projects.Start(c, exact, other, Projects.BuildYishuv, "site_a", none));
        Assert.Equal((0.0, 3.0), (exact.Treasury, exact.Debt));
        Assert.DoesNotContain(none, x => x.System == "economy");
    }

    // R-SITE-001, R-SITE-002
    [Fact]
    public void TheLegacyLoopRefusesBothOrdersAndRefundsAttention()
    {
        var c = ContentLoader.Load(Fixture.Write());   // the legacy loop (a fixture since the cutover)
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var r = Step(c, s, new PioneerWork(1, Projects.BuildYishuv, "mefalsim"), new StopWork(1));
        Assert.Equal(2, r.Rejected.Count);
        Assert.All(r.Rejected, x => Assert.Equal("That order is not available in this game", x.Reason));
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-SITE-002 (D-A3, question 14: the progress is lost and no Money is returned)
    [Fact]
    public void StoppingFreesTheUnitAndTheSiteAndReturnsNoMoney()
    {
        var c = Town();
        var started = Step(c, WithHalutz(c, out var id), Build(id)).State;
        var stopped = Step(c, started, new StopWork(id));
        Assert.Empty(stopped.Rejected);
        Assert.Empty(stopped.State.Sites);
        Assert.False(Projects.IsBusy(stopped.State, id));
        Assert.Equal(Step(c, started).State.Treasury, stopped.State.Treasury, 9);   // the IL 1.5m paid stays paid
        Assert.Contains(stopped.Why, x => x.System == "map" && x.Message == "Site A: the halutzim stopped building; the Money spent is not returned");
        var again = Step(c, stopped.State, Build(id));   // the site is available again, from the start
        Assert.Empty(again.Rejected);
        Assert.Equal(2, again.State.Sites["site_a"].TurnsLeft);
    }

    // R-SITE-002
    [Fact]
    public void StoppingAUnitWithNoWorkIsRefusedAndChangesNothing()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        foreach (var (unit, reason) in new[] { (999, "There is no such unit"), (id, "That group of halutzim is not building anything") })
        {
            var r = Step(c, s, new StopWork(unit));
            Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
            Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
        }
        var built = s.Clone();
        built.Sites["site_a"] = SiteWork.Built("site_a", new DateTime(1948, 8, 15));   // a finished build holds no unit
        Assert.Equal("That group of halutzim is not building anything", Assert.Single(Step(c, built, new StopWork(id)).Rejected).Reason);
    }

    // R-SITE-001, R-SITE-002
    [Fact]
    public void BothOrdersSurviveASave()
    {
        var c = Town();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new Train("town_a", "halutz", 1) });
        g.Step(new Command[0]);
        g.Step(new Command[0]);   // the fixture's halutz trains for 3 turns
        var id = Assert.Single(g.State.Units.Keys);
        g.Step(new Command[] { Build(id) });
        g.Step(new Command[] { new StopWork(id) });
        g.Step(new Command[] { Build(id) });
        var text = g.Save.Write();
        Assert.Contains($"\npioneer {id} build_yishuv site_a\n", text);
        Assert.Contains($"\nstop {id}\n", text);
        var loaded = GameSession.Load(c, text, out _).State;
        Assert.Equal(g.State.Hash(), loaded.Hash());
        Assert.Single(loaded.Sites);
    }

    // ---- construction in the resolver (R-SITE-003, step 5 of section 2.3) ----

    // R-SITE-003
    [Fact]
    public void TheYishuvIsBuiltOnTheExactTurnWithTheTurnsEndDate()
    {
        var c = Town();
        var s = Step(c, WithHalutz(c, out var id), Build(id)).State;   // the end of turn T
        s = Step(c, s).State;                                           // T+1
        Assert.Equal(1, s.Sites["site_a"].TurnsLeft);
        Assert.True(Projects.IsBusy(s, id));
        var r = Step(c, s);                                             // T+2: built
        var work = r.State.Sites["site_a"];
        Assert.Equal(r.State.Date, work.BuiltOn);   // the turn's end date
        Assert.False(Projects.IsBusy(r.State, id));   // the unit is idle again (question 10: it stays a halutz unit)
        Assert.Equal("halutz", r.State.Units[id].Type);
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Site A founded");
        Assert.Equal("Site A is already built", Assert.Single(Step(c, r.State, Build(id)).Rejected).Reason);
    }

    /// <summary>The timing rule, the same reading of D-A6 as training's (R-TRAIN-003): the turn of the order is the first build
    /// turn, so with build_yishuv_turns 6 an order on turn T is built at the end of turn T+5.</summary>
    // R-SITE-001, R-SITE-003
    [Fact]
    public void AnOrderOnTurnTWithSixTurnsIsBuiltAtTheEndOfTurnTPlusFive()
    {
        Assert.Equal(6, Mvp.RulesAt(Mvp.Eras[0].From).Mvp!.BuildYishuvTurns);
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", -3, 19));
        s = Step(Mvp, s, Build(id, "mefalsim")).State;   // the end of turn T
        for (int turn = 1; turn <= 4; turn++) s = Step(Mvp, s).State;   // T+1 to T+4
        Assert.Null(s.Sites["mefalsim"].BuiltOn);
        var built = Step(Mvp, s).State;                  // T+5
        Assert.Equal(built.Date, built.Sites["mefalsim"].BuiltOn);
    }

    /// <summary>Q-SITE-1 (#782): every MVP yishuv site builds as Mefalsim does, in build_yishuv_turns from the order.</summary>
    // R-SITE-001, R-SITE-003, R-SITE-004
    [Theory]
    [InlineData("beit_kama")]
    [InlineData("gilat")]
    [InlineData("mefalsim")]
    [InlineData("reim")]
    public void EveryYishuvSiteBuildsAsMefalsimDoes(string site)
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", -3, 19));
        var r = Step(Mvp, s, Build(id, site));
        Assert.Empty(r.Rejected);
        s = r.State;
        for (int turn = 1; turn <= 4; turn++) s = Step(Mvp, s).State;
        Assert.Null(s.Sites[site].BuiltOn);
        var built = Step(Mvp, s).State;
        Assert.Equal(built.Date, built.Sites[site].BuiltOn);
    }

    // R-SITE-003, R-TRAIN-003: training (step 4) runs in the resolver, after the orders, so a halutz group that finishes
    // training on turn T is a unit from the end of T and can be ordered on T+1, not on T.
    [Fact]
    public void AHalutzTrainedThisTurnCanBeOrderedNextTurn()
    {
        var c = Town();
        var s = Step(c, Simulation.NewGame(c, 1, Mode.Historical), new Train("town_a", "halutz", 1)).State;
        var id = s.NextId;   // the cohort took an id; the unit takes the next one when it finishes
        s = Step(c, s).State;
        Assert.Empty(s.Units);
        var last = Step(c, s, Build(id));   // the turn it finishes: there is no unit yet when the order is read
        Assert.Equal("There is no such unit", Assert.Single(last.Rejected).Reason);
        Assert.Equal(id, Assert.Single(last.State.Units.Keys));
        Assert.Empty(Step(c, last.State, Build(id)).Rejected);
    }

    // R-SITE-003: sites advance in ordinal id order; two that finish on the same turn are founded in that order.
    [Fact]
    public void TwoSitesAdvanceInIdOrder()
    {
        var c = Town(new Dictionary<string, string> { ["sites.yaml"] = TwoSites() });
        var s = WithHalutz(c, out var a);
        var b = s.NextId++;
        s.Units.Add(b, new Unit(b, "halutz", 1, 0));
        s = Step(c, s, Build(a, "site_b"), Build(b, "site_a")).State;
        s = Step(c, s).State;
        var why = Step(c, s).Why.Where(x => x.Message.EndsWith(" founded", StringComparison.Ordinal)).Select(x => x.Message);
        Assert.Equal(new[] { "Site A founded", "Site B founded" }, why);
    }

    // R-SITE-003 (D-A6): construction is step 5, after training (4) and before production (6).
    [Fact]
    public void ConstructionRunsAfterTrainingAndBeforeProduction()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        s = Step(c, s, Build(id), new Train("town_a", "halutz", 1)).State;   // both end on T+2
        var why = Step(c, Step(c, s).State).Why.Select(x => x.Message).ToList();
        var trained = why.FindIndex(m => m.Contains("finished pioneer training"));
        var founded = why.IndexOf("Site A founded");
        var paid = why.FindIndex(m => m.Contains("jobs paid"));
        Assert.True(trained >= 0 && founded > trained && paid > founded, string.Join(" | ", why));
    }

    // R-SITE-003 (question 11): in Phase 1 a built site is only marked built. No city, housing, jobs, people or Money change.
    [Fact]
    public void BuildingChangesNoCity()
    {
        var c = Town();
        var start = WithHalutz(c, out var id);
        var built = start; var idle = start;
        built = Step(c, built, Build(id)).State; idle = Step(c, idle).State;
        for (int i = 0; i < 2; i++) { built = Step(c, built).State; idle = Step(c, idle).State; }
        Assert.NotNull(built.Sites["site_a"].BuiltOn);
        Assert.Equal(idle.Cities.Values.Select(x => x.Canonical()), built.Cities.Values.Select(x => x.Canonical()));
        // The unit's service is the one change (R-VET-001, VeteranTests); its type and hex stay.
        Assert.Equal(idle.Units.Values.Select(x => (x.Id, x.Type, x.Q, x.R)), built.Units.Values.Select(x => (x.Id, x.Type, x.Q, x.R)));
        Assert.Equal(idle.Indicators["morale"], built.Indicators["morale"]);   // Social cohesion
        Assert.Equal(idle.Treasury - 1.5, built.Treasury, 9);   // only the cost paid at the start: founding adds no Money
        Assert.Equal(idle.Debt, built.Debt);
    }

    [Fact]
    public void TheResolverBuildsNothingInTheLegacyLoop()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Step(c, Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical)).State;
        Assert.Empty(s.Sites);
        Assert.DoesNotContain("site{", s.Describe());
    }
}
