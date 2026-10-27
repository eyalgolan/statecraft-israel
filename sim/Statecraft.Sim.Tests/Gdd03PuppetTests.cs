using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

public class Gdd03PuppetTests
{
    static Content Load(string identity = SaveGame.Gdd03Ruleset) => CaptureTests.Load(files =>
    {
        files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + identity);
        files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
    }, CaptureTests.AllTargets);

    static GameState Ending(Content c, bool fled = false) => EndOccupationTests.AwaitingEnd(c, fled, CaptureTests.Soldier(1, 1, 0));

    // R-CTRL-024: expected red before implementation: only Annex and Withdraw are offered.
    [Fact]
    public void OptionsAreAnnexPuppetWithdrawAndPreviewIsPure()
    {
        var c = Load(); var s = Ending(c); var before = s.Describe();
        var options = SettlementControl.EndOptions(c, s, "town_a");
        Assert.Equal(new[] { "annex", "puppet", "withdraw" }, options.Select(x => x.Outcome));
        var puppet = options.Single(x => x.Outcome == "puppet");
        Assert.Null(puppet.Refused);
        Assert.Null(puppet.Preview!.Attention);
        Assert.Equal((0.0, 0.0, 0, 0, 0.0, 0.0, 0),
            (puppet.Preview.Money, puppet.Preview.Debt, puppet.Preview.Food, puppet.Preview.Arms,
             puppet.Preview.SocialCohesion, puppet.Preview.InternationalSupport, puppet.Preview.Score));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-024: expected red: puppet is rejected. Restore only the record to prove there are no side effects.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PuppetChangesOnlyControlAndPreservesResidents(bool fled)
    {
        var c = Load(); var s = Ending(c, fled); var before = s.Clone(); var why = new List<Cause>();
        Assert.Null(SettlementControl.End(c, s, "town_a", "puppet", why));
        var record = s.Settlements["town_a"];
        Assert.Equal(("israel", "puppet", before.Settlements["town_a"].Since, 0, fled),
            (record.Owner, record.State, record.Since, record.Turns, record.Fled));
        Assert.Equal(before.Settlements["town_a"].Combatants, record.Combatants);
        Assert.Equal(before.Settlements["town_a"].Buildings, record.Buildings);
        Assert.False(record.Depopulated);
        Assert.Equal("place_puppeted", Assert.Single(why).Key);
        Assert.Empty(why[0].Args);
        s.Settlements["town_a"] = before.Settlements["town_a"];
        Assert.Equal(before.Describe(), s.Describe());
        Assert.Equal(before.RngState, s.RngState);
    }

    // R-CTRL-024: expected red: installation fails. Tick must neither release nor charge a puppet without a garrison.
    [Fact]
    public void GarrisonCanLeaveWithoutLosingIndirectControlOrCreatingWorkforce()
    {
        var c = Load(); var s = Ending(c); var why = new List<Cause>();
        Assert.Null(SettlementControl.End(c, s, "town_a", "puppet", why));
        var moved = Simulation.Preview(c, s, new Command[] { new Move(1, 2, 0) });
        Assert.Empty(moved.Rejected);
        Assert.Equal((2, 0), (moved.State.Units[1].Q, moved.State.Units[1].R));
        Assert.Equal("puppet", moved.State.Settlements["town_a"].State);
        s.Units.Clear(); why.Clear(); var before = s.Describe();
        SettlementControl.Tick(c, s, c.RulesAt(s.Date).Mvp!, why);
        SettlementControl.Capture(c, s, s.Date, why);
        Assert.Equal(before, s.Describe()); Assert.Empty(why); Assert.Empty(s.Units);
        Assert.Empty(SettlementControl.EndOptions(c, s, "town_a"));
        Assert.NotNull(SettlementControl.End(c, s, "town_a", "puppet", why));
        Assert.Equal(before, s.Describe());
        // town_c has no fixture city: Puppet must not create one or make its residents assignable.
        s.Settlements["town_c"] = new SettlementControl("israel", SettlementControl.AwaitingEnd, s.Date);
        s.Units[1] = CaptureTests.Soldier(1, 4, 0);
        var cities = s.Cities.Count;
        Assert.Null(SettlementControl.End(c, s, "town_c", "puppet", why));
        Assert.False(s.Cities.ContainsKey("town_c")); Assert.Equal(cities, s.Cities.Count);
        Assert.Single(s.Units);
    }

    // R-CTRL-024: expected red before movement recognizes indirect control.
    [Fact]
    public void PuppetIsNoLongerAnEnemyHeldLocalityForWithdrawal()
    {
        var c = Load(); var s = Ending(c);
        Assert.Contains((1, 0), Movement.NoEntry(c, s));
        Assert.Null(SettlementControl.End(c, s, "town_a", "puppet", new List<Cause>()));
        Assert.DoesNotContain((1, 0), Movement.NoEntry(c, s));
    }

    // R-CTRL-024: Puppet, like annexation, stays closed to enemy recapture until enemy capture is implemented (#1305).
    [Fact]
    public void EnemyCannotEndAMoveOnAPuppetPlace()
    {
        var c = Load(); var s = Ending(c);
        Assert.Null(SettlementControl.End(c, s, "town_a", "puppet", new List<Cause>()));
        s.Units.Clear(); // No garrison or opposing unit can account for the refusal.
        s.Units[2] = CaptureTests.Enemy(2, 2, 0);
        var before = s.Describe(); var spent = new Dictionary<int, int>(); var why = new List<Cause>();
        Assert.Contains((1, 0), Movement.NoEntry(c, s, enemy: true));
        Assert.Equal("Units may not enter an enemy-held locality before its capture is possible",
            Movement.Move(c, s, 2, 1, 0, spent, why, enemy: true));
        Assert.Equal((2, 0), (s.Units[2].Q, s.Units[2].R));
        Assert.Equal(before, s.Describe()); Assert.Empty(spent); Assert.Empty(why);
    }

    // R-CTRL-024: negative controls; expected green before implementation.
    [Theory]
    [InlineData(SettlementControl.AwaitingChoice)]
    [InlineData(SettlementControl.Occupied)]
    [InlineData("")]
    public void PuppetNeedsAnEndedOccupation(string state)
    {
        var c = Load(); var s = Ending(c);
        s.Settlements["town_a"] = new SettlementControl("israel", state, s.Date, depopulated: state == "");
        var before = s.Describe(); var why = new List<Cause>();
        Assert.NotNull(SettlementControl.End(c, s, "town_a", "puppet", why));
        Assert.Empty(why); Assert.Equal(before, s.Describe());
    }

    // R-CTRL-024: expected red: the option is absent rather than disabled with NeedsUnit.
    [Fact]
    public void MissingGarrisonRefusesPuppetWithoutChangingState()
    {
        var c = Load(); var s = Ending(c); s.Units.Clear(); var before = s.Describe();
        var option = SettlementControl.EndOptions(c, s, "town_a").Single(x => x.Outcome == "puppet");
        Assert.Equal(CaptureRefusal.NeedsUnit, option.Refused!.Id);
        Assert.Equal(option.Refusal, SettlementControl.End(c, s, "town_a", "puppet", new List<Cause>()));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-015, R-CTRL-024: compatibility controls, expected green before implementation.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldIdentitiesStillOfferOnlyAnnexAndWithdraw(bool gdd)
    {
        var c = gdd ? Load(SaveGame.GddRuleset) : ContentLoader.Load(Fixture.Write());
        var s = gdd ? Ending(c) : Simulation.NewGame(c, 1, Mode.Historical);
        var before = s.Describe(); var why = new List<Cause>();
        Assert.Equal(gdd ? new[] { "annex", "withdraw" } : Array.Empty<string>(),
            SettlementControl.EndOptions(c, s, "town_a").Select(x => x.Outcome));
        Assert.NotNull(SettlementControl.End(c, s, "town_a", "puppet", why));
        Assert.Empty(why); Assert.Equal(before, s.Describe());
    }

    // R-CTRL-024, R-SAVE-003: expected red: endocc parses already, but replay rejects the new outcome.
    [Fact]
    public void CompletedPuppetOutcomeLoadsAndReplaysWithTheSameBytes()
    {
        var c = Load();
        c.StartingForces.Add(new StartingForce { Row = "fixture", Type = "soldier", Side = "israel", Strength = 2, Q = 1, R = 0 });
        var game = GameSession.New(c, 1, Mode.Historical);
        Assert.Empty(game.Step(new Command[] { new CaptureLocality("town_a"), new ResolveCapture("town_a", "occupy") }).Rejected);
        while (game.State.Settlements["town_a"].State == SettlementControl.Occupied)
            Assert.Empty(game.Step(Array.Empty<Command>()).Rejected);
        var command = Scenario.Build(new Dictionary<string, object>
            { ["kind"] = "end_occupation", ["settlement"] = "town_a", ["outcome"] = "puppet" });
        Assert.Empty(game.Step(new[] { command }).Rejected);
        Assert.Equal("puppet", game.State.Settlements["town_a"].State);
        var text = game.Save.Write(); Assert.Contains("endocc town_a puppet\n", text);
        var parsed = SaveGame.Parse(text, c.Ruleset);
        Assert.Equal("puppet", Assert.IsType<EndOccupation>(parsed.Turns.Last().Single()).Outcome);
        var loaded = GameSession.Load(c, text, out var warnings); Assert.Empty(warnings);
        Assert.Equal(game.State.Describe(), loaded.State.Describe()); Assert.Equal(text, loaded.Save.Write());
        var replay = GameSession.New(c, 1, Mode.Historical);
        foreach (var batch in parsed.Turns) Assert.Empty(replay.Step(batch).Rejected);
        Assert.Equal(game.State.Hash(), replay.State.Hash()); Assert.Equal(text, replay.Save.Write());
    }
}
