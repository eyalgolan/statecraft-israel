using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Statecraft.Sim.Reference;

/// <summary>GDD Phase 4 PR 2 (#786): building corridors segment by segment (R-COR-001 to R-COR-003). The owner took the
/// plan's recommendations on 17 October 2026 (#782): any unbuilt segment in any order, one unit per segment (Q-COR-4), the
/// unit works from where it stands (Q-COR-5), the cost is paid in full at the start and borrowed if short, and stopping
/// loses the progress and returns no Money (Q-COR-6).</summary>
public class CorridorsTests
{
    /// <summary>The GDD fixture with road_a (two segments, 3 turns and IL 1.5m each) and rail_a (one segment, 2 turns and
    /// IL 3m).</summary>
    static Content Town() => ContentLoader.Load(Fixture.Write(Fixture.Gdd()));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static GameState WithHalutzim(Content c, int n, out int[] ids)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        ids = new int[n];
        for (int i = 0; i < n; i++) { ids[i] = s.NextId++; s.Units.Add(ids[i], new Unit(ids[i], "halutz", 1, 0)); }
        return s;
    }

    static PioneerWork Road(int unit, string target = "road_a:1") => new PioneerWork(unit, Corridors.BuildRoad, target);

    // R-COR-001
    [Fact]
    public void StartingASegmentCommitsTheUnitAndPaysTheKindsCost()
    {
        var c = Town();
        var s = WithHalutzim(c, 1, out var id);
        var r = Step(c, s, Road(id[0], "road_a:2"));
        Assert.Empty(r.Rejected);
        var seg = r.State.Corridors["road_a"].Segments[1]!;
        Assert.Equal(((int?)id[0], 1, (DateTime?)null), (seg.Unit, seg.Progress, seg.BuiltOn));   // this turn is the first build turn
        Assert.Null(r.State.Corridors["road_a"].Segments[0]);
        Assert.True(Projects.IsBusy(r.State, id[0]));
        Assert.Equal(Step(c, s).State.Treasury - 1.5, r.State.Treasury, 9);
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Road A, segment 2: a group of halutzim began building, cost IL 1.5m");
        var rail = Step(c, s, new PioneerWork(id[0], Corridors.BuildRail, "rail_a:1"));
        Assert.Empty(rail.Rejected);
        Assert.Equal(Step(c, s).State.Treasury - 3, rail.State.Treasury, 9);   // the rail kind's cost
    }

    public static IEnumerable<object[]> Refusals() => new List<object[]>
    {
        new object[] { "busy at a site", "That group of halutzim is already building Site A" },
        new object[] { "busy on a segment", "That group of halutzim is already building Rail A, segment 1" },
        new object[] { "unknown corridor", "There is no such corridor segment" },
        new object[] { "segment 0", "There is no such corridor segment" },
        new object[] { "segment 3", "There is no such corridor segment" },
        new object[] { "no segment", "There is no such corridor segment" },
        new object[] { "held", "Road A, segment 1 is already being built" },
        new object[] { "built", "Road A, segment 1 is already built" },
        new object[] { "rail on a road", "Road A is not a railway" },
        new object[] { "road on a rail", "Rail A is not a road" },
    };

    // R-COR-001: each refusal leaves the state as a step with no order would, so Attention is refunded too.
    [Theory]
    [MemberData(nameof(Refusals))]
    public void TheRefusalsChangeNothing(string limit, string reason)
    {
        var c = Town();
        var s = WithHalutzim(c, 2, out var id);
        Command order = Road(id[0]);
        switch (limit)
        {
            case "busy at a site": s.Sites["site_a"] = new SiteWork("site_a", id[0], 2, null); break;
            case "busy on a segment": s.Corridors["rail_a"] = new CorridorWork("rail_a", new SegmentWork?[] { new SegmentWork(id[0], 1, null) }); break;
            case "unknown corridor": order = Road(id[0], "nowhere:1"); break;
            case "segment 0": order = Road(id[0], "road_a:0"); break;
            case "segment 3": order = Road(id[0], "road_a:3"); break;
            case "no segment": order = Road(id[0], "road_a"); break;
            case "held": s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[] { new SegmentWork(id[1], 1, null), null }); break;
            case "built": s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[] { new SegmentWork(null, 3, new DateTime(1948, 8, 15)), null }); break;
            case "rail on a road": order = new PioneerWork(id[0], Corridors.BuildRail, "road_a:1"); break;
            case "road on a rail": order = Road(id[0], "rail_a:1"); break;
        }
        var r = Step(c, s, order);
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-COR-001: the legacy loop refuses the order before Corridors sees it.
    [Fact]
    public void TheLegacyLoopRefusesARoadOrder()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var r = Step(c, s, Road(1, "burma_road:1"));
        Assert.Equal("That order is not available in this game", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-COR-002 (Review focus 2: a segment's progress reaches its turns once)
    [Fact]
    public void ASegmentIsBuiltOnceAfterItsTurns()
    {
        var c = Town();
        var s = WithHalutzim(c, 1, out var id);
        var r = Step(c, s, Road(id[0]));   // turn T: progress 1 of 3
        r = Step(c, r.State);             // T+1: 2 of 3
        Assert.Equal(2, r.State.Corridors["road_a"].Segments[0]!.Progress);
        Assert.True(Projects.IsBusy(r.State, id[0]));
        r = Step(c, r.State);             // T+2: built, on the turn's end date
        var seg = r.State.Corridors["road_a"].Segments[0]!;
        Assert.Equal(((int?)null, 3, (DateTime?)r.State.Date), (seg.Unit, seg.Progress, seg.BuiltOn));
        Assert.False(Projects.IsBusy(r.State, id[0]));
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Road A, segment 1 built");
        var after = Step(c, r.State).State;   // nothing more happens to a built segment
        Assert.Equal(seg.Canonical(), after.Corridors["road_a"].Segments[0]!.Canonical());
        Assert.False(Corridors.IsComplete(c, after, "road_a"));
    }

    // R-VET-002 on a segment (#868's rule; the plan's PR 6 names Corridors.cs): a veteran's segment gains veteran_progress
    // turns of progress a turn, and one with that many or fewer left is built that turn. A veteran gains no more service.
    [Fact]
    public void AVeteranBuildsASegmentFaster()
    {
        var c = Town();   // road_a: 3 turns a segment; veteran_progress 2
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "veteran_halutz", 1, 0, 4));
        var r = Step(c, s, Road(id));   // turn T: 2 of 3
        Assert.Empty(r.Rejected);
        Assert.Equal(2, r.State.Corridors["road_a"].Segments[0]!.Progress);
        r = Step(c, r.State);           // T+1: built, at 3 of 3
        var seg = r.State.Corridors["road_a"].Segments[0]!;
        Assert.Equal(((int?)null, 3, (DateTime?)r.State.Date), (seg.Unit, seg.Progress, seg.BuiltOn));
        Assert.Equal(("veteran_halutz", 4), (r.State.Units[id].Type, r.State.Units[id].Service));
    }

    // R-VET-001 on a segment: a halutz whose segment progressed gains a turn of service, and at veteran_halutz_service it
    // becomes a veteran at the end of that turn, keeping its segment, which then gains veteran_progress a turn.
    [Fact]
    public void WorkOnASegmentCountsAsService()
    {
        var c = Town();   // veteran_halutz_service 4
        var s = WithHalutzim(c, 1, out var id);
        s.Units[id[0]] = new Unit(id[0], "halutz", 1, 0, 2);
        var r = Step(c, s, Road(id[0]));   // turn T: 1 of 3, service 3
        Assert.Equal(("halutz", 3), (r.State.Units[id[0]].Type, r.State.Units[id[0]].Service));
        r = Step(c, r.State);             // T+1: 2 of 3 (a halutz's progress), service 4: promoted
        Assert.Equal(("veteran_halutz", 4), (r.State.Units[id[0]].Type, r.State.Units[id[0]].Service));
        Assert.Equal(2, r.State.Corridors["road_a"].Segments[0]!.Progress);
        Assert.True(Projects.IsBusy(r.State, id[0]));
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "A group of halutzim became veteran pioneers");
        r = Step(c, r.State);             // T+2: built
        Assert.NotNull(r.State.Corridors["road_a"].Segments[0]!.BuiltOn);
        Assert.Equal(4, r.State.Units[id[0]].Service);
    }

    // R-UNIT-002's fields survive a segment turn and the promotion, as on a site (Projects.Advance).
    [Fact]
    public void ASegmentTurnKeepsTheUnitsSideStrengthAndOrder()
    {
        var c = Town();   // veteran_halutz_service 4
        var s = WithHalutzim(c, 1, out var id);
        s.Units[id[0]] = new Unit(id[0], "halutz", 1, 0, 3, "israel", 5, "hold", "x");
        var u = Step(c, s, Road(id[0])).State.Units[id[0]];   // service 4: promoted
        Assert.Equal(("veteran_halutz", "israel", 5, "hold", "x"), (u.Type, u.Side, u.Strength, u.Order, u.OrderTarget));
    }

    // R-COR-001, R-COR-003 (Q-COR-4: any order, one unit per segment)
    [Fact]
    public void TwoUnitsBuildOneCorridorInParallel()
    {
        var c = Town();
        var s = WithHalutzim(c, 2, out var id);
        var r = Step(c, s, Road(id[0], "road_a:2"), Road(id[1], "road_a:1"));
        Assert.Empty(r.Rejected);
        Assert.False(Corridors.IsComplete(c, r.State, "road_a"));
        r = Step(c, Step(c, r.State).State);
        Assert.True(Corridors.IsComplete(c, r.State, "road_a"));
        Assert.All(r.State.Corridors["road_a"].Segments, x => Assert.NotNull(x!.BuiltOn));
    }

    // R-COR-003
    [Fact]
    public void ACorridorWithAGapIsNotComplete()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.False(Corridors.IsComplete(c, s, "road_a"));   // nothing started
        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[] { null, new SegmentWork(null, 3, new DateTime(1948, 8, 15)) });
        Assert.False(Corridors.IsComplete(c, s, "road_a"));
        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[] { new SegmentWork(1, 2, null), new SegmentWork(null, 3, new DateTime(1948, 8, 15)) });
        Assert.False(Corridors.IsComplete(c, s, "road_a"));
        s.Corridors["rail_a"] = new CorridorWork("rail_a", new SegmentWork?[] { new SegmentWork(null, 3, new DateTime(1948, 8, 15)) });
        Assert.True(Corridors.IsComplete(c, s, "rail_a"));
    }

    // R-COR-003 with R-SITE-002's rule (D-A3): the progress is lost and the Money stays paid.
    [Fact]
    public void StoppingLosesTheSegmentsProgressAndReturnsNoMoney()
    {
        var c = Town();
        var started = Step(c, WithHalutzim(c, 1, out var id), Road(id[0])).State;
        var stopped = Step(c, started, new StopWork(id[0]));
        Assert.Empty(stopped.Rejected);
        Assert.Empty(stopped.State.Corridors);   // no segment of road_a is left with a record
        Assert.False(Projects.IsBusy(stopped.State, id[0]));
        Assert.Equal(Step(c, started).State.Treasury, stopped.State.Treasury, 9);
        Assert.Contains(stopped.Why, x => x.System == "map" && x.Message == "Road A, segment 1: the halutzim stopped building; the Money spent is not returned");
        var again = Step(c, stopped.State, Road(id[0]));
        Assert.Empty(again.Rejected);
        Assert.Equal(1, again.State.Corridors["road_a"].Segments[0]!.Progress);
    }

    // R-COR-001: a short Treasury borrows the shortfall (Q-MONEY-3, as for sites).
    [Fact]
    public void AShortTreasuryBorrows()
    {
        var c = Town();
        var s = WithHalutzim(c, 1, out var id);
        s.Treasury = 0.5;
        s.Debt = 3;
        var why = new List<Cause>();
        Assert.Null(Projects.Start(c, s, id[0], Corridors.BuildRoad, "road_a:1", why));
        Assert.Equal((0.0, 4.0), (s.Treasury, s.Debt));
        Assert.Contains(why, x => x.System == "economy" && x.Target == "debt" && x.Message == "Borrowed IL 1.0m to build a corridor segment");
    }

    // R-COR-002: corridors advance after sites, in ordinal corridor id, then segment index.
    [Fact]
    public void CorridorsAdvanceInIdThenSegmentOrderAfterSites()
    {
        var c = Town();
        var s = WithHalutzim(c, 3, out var id);
        s.Sites["site_a"] = new SiteWork("site_a", id[0], 1, null);
        s.Corridors["road_a"] = new CorridorWork("road_a", new SegmentWork?[] { new SegmentWork(id[1], 2, null), null });
        s.Corridors["rail_a"] = new CorridorWork("rail_a", new SegmentWork?[] { new SegmentWork(id[2], 1, null) });
        var built = Step(c, s).Why.Where(x => x.System == "map").Select(x => x.Message).ToList();
        Assert.Equal(new[] { "Site A founded", "Rail A, segment 1 built", "Road A, segment 1 built" }, built);
    }

    // R-COR-001
    [Fact]
    public void TheOrderSurvivesASave()
    {
        var c = Town();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new Train("town_a", "halutz", 1) });
        g.Step(new Command[0]);
        g.Step(new Command[0]);
        var id = Assert.Single(g.State.Units.Keys);
        g.Step(new Command[] { new PioneerWork(id, Corridors.BuildRail, "rail_a:1") });
        var text = g.Save.Write();
        Assert.Contains($"\npioneer {id} build_rail rail_a%3A1\n", text);   // the save escapes the colon
        var loaded = GameSession.Load(c, text, out _).State;
        Assert.Equal(g.State.Hash(), loaded.Hash());
        Assert.Single(loaded.Corridors);
    }

    // R-COR-001: the reference player's stated policy (D-A7): yishuv sites first (then ready fields and camp sites, none in
    // this fixture), then the lowest unbuilt, unclaimed segment of the lowest-id corridor, with the action its kind takes.
    [Fact]
    public void TheReferencePlayerTakesTheLowestFreeSegmentOnceTheSitesAreTaken()
    {
        var c = Town();
        var s = WithHalutzim(c, 3, out var id);
        s.Treasury = 10;
        var orders = ReferencePlayer.Commands(c, s).OfType<PioneerWork>().Select(p => (p.Unit, p.Action, p.Site)).ToList();
        Assert.Equal(new[] { (id[0], Projects.BuildYishuv, "site_a"), (id[1], Corridors.BuildRail, "rail_a:1"), (id[2], Corridors.BuildRoad, "road_a:1") }, orders);
        Assert.Empty(Step(c, s, ReferencePlayer.Commands(c, s).ToArray()).Rejected);
    }

    static Content TransportationTown()
    {
        var files = Fixture.Gdd();
        files["offices.yaml"] = Fixture.Files["offices.yaml"] + @"  - id: transportation_minister
    title: Minister of Transportation
";
        files["rules.yaml"] = files["rules.yaml"]
            .Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { transportation_minister: 1.0, finance_minister: 0.0 }")
            + @"  minister_systems:
    corridors: transportation_minister
";
        return ContentLoader.Load(Fixture.Write(files));
    }

    // R-MIN-002/R-COR-002: actual Transportation output is fractional work, not rounded away each turn.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TransportationScalesActualSegmentProgress(bool veteran)
    {
        var c = TransportationTown();
        var s = WithHalutzim(c, 1, out var ids);
        if (veteran) s.Units[ids[0]] = new Unit(ids[0], Projects.VeteranType, 1, 0, 4);
        Assert.Equal(0.75, EffectsSystem.GddSystemMultiplier(c, s, c.Rules.Mvp!, "corridors"), 9);
        Assert.Equal(3, c.Rules.Mvp!.RoadSegmentTurns);
        var first = Step(c, s, Road(ids[0]));
        Assert.Empty(first.Rejected);
        s = first.State;
        Assert.Equal(veteran ? 1 : 0, s.Corridors["road_a"].Segments[0]!.Progress);
        Assert.Null(s.Corridors["road_a"].Segments[0]!.BuiltOn);
        // Same integer progress is insufficient state: the actual fraction must reach the hash.
        Assert.NotEqual(new SegmentWork(ids[0], veteran ? 1 : 0, null).Canonical(),
            s.Corridors["road_a"].Segments[0]!.Canonical());
        Assert.Equal(s.Hash(), s.Clone().Hash());
        var startedHash = s.Hash();
        var second = Step(c, s);
        Assert.Equal(startedHash, s.Hash()); // the real step advances a clone, not its input
        s = second.State;
        if (!veteran)
        {
            Assert.Equal(1, s.Corridors["road_a"].Segments[0]!.Progress); // 0.75 + 0.75 carries through the whole turn
            Assert.Null(s.Corridors["road_a"].Segments[0]!.BuiltOn);
            s = Step(c, s).State;
            Assert.Equal(2, s.Corridors["road_a"].Segments[0]!.Progress);
            Assert.Null(s.Corridors["road_a"].Segments[0]!.BuiltOn);
            s = Step(c, s).State;
        }
        var built = s.Corridors["road_a"].Segments[0]!;
        Assert.Equal(3, built.Progress);
        Assert.Equal(s.Date, built.BuiltOn);
        Assert.False(Projects.IsBusy(s, ids[0]));
        Assert.Equal(new SegmentWork(null, 3, s.Date).Canonical(), built.Canonical());
    }

    [Fact]
    public void TransportationCarryReplaysAndStoppingDiscardsIt()
    {
        var c = TransportationTown();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new Train("town_a", "halutz", 1) });
        g.Step(Array.Empty<Command>()); g.Step(Array.Empty<Command>());
        var id = Assert.Single(g.State.Units.Keys);
        g.Step(new Command[] { Road(id) });
        Assert.Equal(0, g.State.Corridors["road_a"].Segments[0]!.Progress);
        var saved = g.Save.Write();
        var replay = GameSession.Load(c, saved, out _);
        Assert.Equal(g.State.Hash(), replay.State.Hash());
        g.Step(Array.Empty<Command>()); replay.Step(Array.Empty<Command>());
        Assert.Equal(1, g.State.Corridors["road_a"].Segments[0]!.Progress);
        Assert.Equal(g.State.Hash(), replay.State.Hash());
        g.Step(new Command[] { new StopWork(id) });
        Assert.Empty(g.State.Corridors);
        g.Step(new Command[] { Road(id) });
        Assert.Equal(0, g.State.Corridors["road_a"].Segments[0]!.Progress);
    }
}
