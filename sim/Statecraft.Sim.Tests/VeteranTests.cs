using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The veteran pioneer (GDD "Experienced pioneer unit"; D-D9; Phase 4 PR 6). A halutz group that has worked
/// veteran_halutz_service turns becomes a veteran_halutz (R-VET-001), whose project gains veteran_progress turns of
/// progress a turn (R-VET-002). Q-VET-1 and Q-VET-2 are the plan's recommendations, which the owner took on 17 October
/// 2026 (#782).</summary>
public class VeteranTests
{
    /// <summary>The GDD fixture (town_a, site_a) with each (from, to) of <paramref name="edits"/> replaced in rules.yaml.
    /// The fixture's build takes 3 turns; a halutz becomes a veteran after 4 turns of work, one more than a build, and a veteran makes 2 turns of
    /// progress a turn.</summary>
    static Content Town(params (string From, string To)[] edits)
    {
        var files = Fixture.Gdd();
        foreach (var (from, to) in edits) files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        return ContentLoader.Load(Fixture.Write(files));
    }

    static GameState WithUnit(Content c, string type, out int id)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        id = s.NextId++;
        s.Units.Add(id, new Unit(id, type, 1, 0));
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static PioneerWork Build(int unit) => new PioneerWork(unit, Projects.BuildYishuv, "site_a");

    // R-VET-001: only a turn whose project progressed counts, and the promotion keeps the unit's id, hex and project.
    [Fact]
    public void AHalutzBecomesAVeteranAfterNTurnsOfWork()
    {
        var c = Town(("veteran_halutz_service: 4", "veteran_halutz_service: 2"));
        var s = WithUnit(c, "halutz", out var id);
        s = Step(c, s).State;                                    // an idle turn
        Assert.Equal(0, s.Units[id].Service);
        s = Step(c, s, Build(id)).State;                         // the first turn of work: the order's turn counts (D-A6)
        Assert.Equal(("halutz", 1), (s.Units[id].Type, s.Units[id].Service));
        var r = Step(c, s);                                      // the second: promoted at the end of the turn
        var u = r.State.Units[id];
        Assert.Equal(("veteran_halutz", 2, 1, 0), (u.Type, u.Service, u.Q, u.R));
        Assert.Equal(1, r.State.Sites["site_a"].TurnsLeft);      // promoted after this turn's progress, which was a halutz's
        Assert.True(Projects.IsBusy(r.State, id));               // the same project
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "A group of halutzim became veteran pioneers");
        var built = Step(c, r.State).State;                      // a veteran gains no more service
        Assert.Equal(2, built.Units[id].Service);
        Assert.NotNull(built.Sites["site_a"].BuiltOn);
    }

    // R-VET-001: one completed yishuv makes a veteran pioneer in the real content (veteran_halutz_service is
    // build_yishuv_turns), and the turn the build finishes counts.
    [Fact]
    public void OneYishuvMakesAVeteranInTheRealContent()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var r = c.Rules.Mvp!;
        Assert.Equal(r.BuildYishuvTurns, r.VeteranHalutzService);
        var s = WithUnit(c, "halutz", out var id);
        var site = c.Sites.First(x => x.Kinds.Contains(Projects.YishuvKind)).Id;
        s = Step(c, s, new PioneerWork(id, Projects.BuildYishuv, site)).State;
        for (int i = 1; i < r.BuildYishuvTurns; i++) s = Step(c, s).State;
        Assert.NotNull(s.Sites[site].BuiltOn);
        Assert.Equal("veteran_halutz", s.Units[id].Type);
    }

    // R-VET-002: a veteran may start pioneer work, and the same build finishes sooner, by veteran_progress turns a turn.
    [Fact]
    public void AVeteranBuildsFaster()
    {
        // A halutz that is not promoted during the build: service above the build's 6 turns.
        var c = Town(("build_yishuv_turns: 3", "build_yishuv_turns: 6"), ("veteran_halutz_service: 4", "veteran_halutz_service: 7"));
        int TurnsToBuild(string type)
        {
            // "With the same cost": both types pay build_yishuv_cost and the site starts with build_yishuv_turns, so only
            // the progress a turn differs (review of #868).
            var m = c.Rules.Mvp!;
            var paid = WithUnit(c, type, out var payer);
            double before = paid.Treasury;
            Assert.Null(Projects.Start(c, paid, payer, Projects.BuildYishuv, "site_a", new List<Cause>()));
            Assert.Equal((before - m.BuildYishuvCost, m.BuildYishuvTurns), (paid.Treasury, paid.Sites["site_a"].TurnsLeft));
            var s = WithUnit(c, type, out var id);
            var r = Step(c, s, Build(id));
            Assert.Empty(r.Rejected);
            int turns = 1;
            for (s = r.State; s.Sites["site_a"].BuiltOn == null; turns++) s = Step(c, s).State;
            return turns;
        }
        Assert.Equal(6, TurnsToBuild("halutz"));
        Assert.Equal(3, TurnsToBuild("veteran_halutz"));   // 6 turns of progress at 2 a turn
    }

    // R-SITE-003, R-VET-001, R-VET-002: TurnsToBuild is Advance's count, for a new build and for one under way, so the panel
    // can show it (#869: it showed build_yishuv_turns for every group). A halutz promoted during the build speeds up.
    [Theory]
    [InlineData("halutz", 0, "veteran_halutz_service: 4", 3)]
    [InlineData("veteran_halutz", 0, "veteran_halutz_service: 4", 2)]   // 3 turns of progress at 2 a turn
    [InlineData("halutz", 0, "veteran_halutz_service: 1", 2)]           // promoted after its first turn: 1, then 2
    [InlineData("halutz", 1, "veteran_halutz_service: 2", 2)]           // service carried from an earlier build
    public void TurnsToBuildIsTheTurnsAdvanceTakes(string type, int service, string serviceRule, int expected)
    {
        var c = Town(("veteran_halutz_service: 4", serviceRule));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, type, 1, 0, service));
        Assert.Equal(expected, Projects.TurnsToBuild(c, s, id, c.Rules.Mvp!.BuildYishuvTurns));
        s = Step(c, s, Build(id)).State;
        for (int done = 1; ; done++)
        {
            if (s.Sites["site_a"].BuiltOn != null) { Assert.Equal(expected, done); break; }
            Assert.Equal(expected - done, Projects.TurnsToBuild(c, s, id, s.Sites["site_a"].TurnsLeft));   // the turns left under way
            s = Step(c, s).State;
        }
    }

    // R-VET-001, R-VET-002: both values are required in the GDD loop and at least 1, so a missing key is never read as 0.
    [Theory]
    [InlineData("  veteran_halutz_service: 4\n", "", "mvp.veteran_halutz_service", "required when eras.yaml systems.loop is gdd")]
    [InlineData("veteran_halutz_service: 4", "veteran_halutz_service: 0", "mvp.veteran_halutz_service", "must be >= 1")]
    [InlineData("  veteran_progress: 2\n", "", "mvp.veteran_progress", "required when eras.yaml systems.loop is gdd")]
    [InlineData("veteran_progress: 2", "veteran_progress: 0", "mvp.veteran_progress", "must be >= 1")]
    public void BadVeteranValuesFailLoad(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Town((from, to)));
        Assert.Equal(("rules.yaml", field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }
}
