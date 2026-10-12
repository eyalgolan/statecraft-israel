using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>eras.yaml `systems` (#58): the legislature and map_actions switches, each tested both ways.</summary>
public class EraSystemsTests
{
    static string Eras(string systems) => $@"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1957-01-01
{systems}";

    static Content Load(string systems, bool noMajority = false)
    {
        var o = new Dictionary<string, string> { ["eras.yaml"] = Eras(systems) };
        // Government One has no majority in the council (beta holds 2 of 5), so every vote fails.
        if (noMajority) o["governments.yaml"] = Fixture.Files["governments.yaml"].Replace("coalition: [alpha]", "coalition: [beta]");
        return ContentLoader.Load(Fixture.Write(o));
    }

    [Fact]
    public void RealContentUsesTheDefaultSwitches()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal("state", c.SystemsAt(c.Rules.Start).Legislature);
        Assert.Equal(new[] { "transit_camp", "housing" }, c.SystemsAt(c.Rules.Start).MapActions);
        Assert.Equal("state", c.SystemsAt(new DateTime(1960, 1, 1)).Legislature);   // outside every era: defaults
    }

    [Theory]
    [InlineData("    systems: { legislature: parliament }", "eras[founding].systems.legislature", "must be state or assembly")]
    [InlineData("    systems: { map_actions: [bulldozer] }", "eras[founding].systems.map_actions", "unknown map action 'bulldozer'")]
    [InlineData("    systems: { map_actions: [housing, housing] }", "eras[founding].systems.map_actions", "lists an action twice")]
    public void BadSwitchesFailNamingTheField(string systems, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(systems));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void AnUnknownSwitchKeyFailsNamingTheFileAndKey()
    {
        var ex = Assert.Throws<ContentException>(() => Load("    systems: { war_mode: true }"));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Contains("war_mode", ex.Message);
    }

    [Fact]
    public void AnEmptySystemsKeyFailsNamingTheField()
    {
        var ex = Assert.Throws<ContentException>(() => Load("    systems:"));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal("eras[founding].systems", ex.Field);
    }

    /// <summary>The budget vote (the fixture's House approves budgets from 1949-02-14; Government One holds 4 of 10, so the
    /// vote fails): a state legislature puts the old budget back, an assembly's vote is reported and the budget stands.</summary>
    [Theory]
    [InlineData("state", 0.5, "House voted the budget down")]
    [InlineData("assembly", 1.0, "House voted against the budget, but its vote does not bind")]
    public void TheBudgetVoteBindsOnlyInAState(string legislature, double finance, string cause)
    {
        var c = Load($"    systems: {{ legislature: {legislature} }}");
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1949, 3, 1);
        s.Budget["finance_minister"] = 0.5;   // an older budget to fall back to
        var r = Simulation.Step(c, s, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 1.0 }) });
        Assert.Empty(r.Rejected);
        Assert.Equal(finance, r.State.Budget["finance_minister"]);
        Assert.Contains(r.Why, w => w.System == "politics" && w.Target == "budget" && w.Message == cause);
    }

    /// <summary>A map action switched off is refused through Step with the chapter reason and builds nothing; the same order in
    /// an era that allows it builds.</summary>
    [Theory]
    [InlineData("    systems: { map_actions: [transit_camp] }", false)]
    [InlineData("    systems: { map_actions: [] }", false)]
    [InlineData("    systems: { map_actions: [transit_camp, housing] }", true)]
    [InlineData("", true)]
    public void ASwitchedOffMapActionIsRefusedThroughStep(string systems, bool builds)
    {
        var c = Load(systems);
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);   // map actions open in the fixture
        var r = Simulation.Step(c, s, new Command[] { new MapAction(MapActions.Housing, 2, 0) });
        if (builds)
        {
            Assert.Empty(r.Rejected);
            Assert.Equal(MapActions.Housing, MapActions.At(r.State, 2, 0)!.Action);
        }
        else
        {
            Assert.Equal("That map action is not available in this chapter", r.Rejected.Single().Reason);
            Assert.Empty(r.State.Placements);
        }
    }

    /// <summary>The switches follow the turn's starting date, like RulesAt: across a boundary inside a turn (1948-06-01; the
    /// fixture's weekly war turns run 05-29 to 06-05) the turn that starts in the assembly era is not bound by the vote, and
    /// the first turn that starts in the state era is.</summary>
    [Fact]
    public void TheSwitchesFollowTheTurnsStartingDate()
    {
        var o = new Dictionary<string, string>
        {
            ["eras.yaml"] = @"
eras:
  - id: yishuv
    name: An Assembly Chapter
    from: 1948-05-15
    to: 1948-06-05
    systems: { legislature: assembly, map_actions: [] }
  - id: founding
    name: The Founding
    from: 1948-06-05
    to: 1957-01-01
",
            ["governments.yaml"] = Fixture.Files["governments.yaml"].Replace("coalition: [alpha]", "coalition: [beta]"),
        };
        var c = ContentLoader.Load(Fixture.Write(o));
        var boundary = new DateTime(1948, 6, 5);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        while ((boundary - s.Date).Days > 14) s = Simulation.Step(c, s, new Command[0]).State;
        Assert.True(s.Date < boundary);
        var crossing = Simulation.Step(c, s, new Command[] { new SetPolicy("rationing", 1.0) });
        Assert.True(crossing.State.Date == boundary);   // the turn ends on the boundary, in the state era
        Assert.Equal(1.0, crossing.State.Policies["rationing"]);
        Assert.Equal("assembly", c.SystemsAt(s.Date).Legislature);
        var next = Simulation.Step(c, crossing.State, new Command[] { new SetPolicy("rationing", 0.0) });
        Assert.Equal(1.0, next.State.Policies["rationing"]);   // voted down: the state era's vote binds
        Assert.Contains(next.Why, w => w.Message.Contains("voted down"));
    }

    [Fact]
    public void AStateLegislatureVotesDownAPolicy()
    {
        var c = Load("    systems: { legislature: state }", noMajority: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("rationing", 1.0) });
        Assert.Equal(0.0, r.State.Policies["rationing"]);
        Assert.Contains(r.Why, w => w.Message.Contains("voted down"));
    }

    [Fact]
    public void AnAssemblyVotesAgainstAPolicyButItStillTakesEffect()
    {
        var c = Load("    systems: { legislature: assembly }", noMajority: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("rationing", 1.0) });
        Assert.Equal(1.0, r.State.Policies["rationing"]);
        Assert.Contains(r.Why, w => w.Message.Contains("its vote does not bind"));
        Assert.DoesNotContain(r.Why, w => w.Message.Contains("voted down"));
    }

    [Fact]
    public void AnEmptyMapActionsListAllowsNone()
    {
        var c = Load("    systems: { map_actions: [] }");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal("That map action is not available in this chapter", MapActions.Check(c, s, MapActions.Housing, 0, 0));
    }

    [Fact]
    public void MapActionsAreLimitedToTheErasList()
    {
        var c = Load("    systems: { map_actions: [housing] }");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal("That map action is not available in this chapter", MapActions.Check(c, s, MapActions.TransitCamp, 0, 0));
        Assert.NotEqual("That map action is not available in this chapter", MapActions.Check(c, s, MapActions.Housing, 0, 0));
        var all = Load("    systems: { map_actions: [transit_camp, housing] }");
        var s2 = Simulation.NewGame(all, 1, Mode.Historical);
        Assert.NotEqual("That map action is not available in this chapter", MapActions.Check(all, s2, MapActions.TransitCamp, 0, 0));
    }
}
