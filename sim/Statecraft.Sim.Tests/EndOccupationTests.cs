using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>R-CTRL-014 to R-CTRL-017 (#799): annex or withdraw when the occupation timer ends; no puppet regime (Q-PUP-1
/// (c)). The owner's answers of 24 October 2026 on #799: 1 (a) a stayed place becomes one city by the starting-city rule,
/// 3 (a) an annexed place stays Israeli for good with no upkeep. The places are CaptureTests' fixture values, not history:
/// town_a on hex 1,0, stayed rows with people 60 and employed_share 0.5, people_per_unit 10.</summary>
public class EndOccupationTests
{
    internal static GameState AwaitingEnd(Content c, bool fled, params Unit[] units) => AwaitingEnd(c, "town_a", fled, units);

    /// <summary>A new game with <paramref name="units"/> and <paramref name="id"/> awaiting the end of its occupation. The
    /// fixture's cities.yaml already has a town_a city, so the stayed annexation tests use town_c (hex 4,0).</summary>
    static GameState AwaitingEnd(Content c, string id, bool fled, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements[id] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, s.Date, fled: fled);
        return s;
    }

    static Unit Soldier(int id, int q, int r) => CaptureTests.Soldier(id, q, r);

    // R-CTRL-014: only a place whose timer has ended can be answered; a refusal changes nothing.
    [Theory]
    [InlineData(SettlementControl.AwaitingChoice, 0)]
    [InlineData(SettlementControl.Occupied, 3)]
    public void OnlyAnEndedOccupationCanBeAnswered(string state, int turns)
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, state, s.Date, turns: turns);
        var before = s.Describe();
        var why = new List<Cause>();
        Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, why));
        Assert.Empty(why);
        Assert.Equal(before, s.Describe());
        Assert.Empty(SettlementControl.EndOptions(c, s, "town_a"));
    }

    // R-CTRL-014: a refused EndOccupation is rejected with its reason and its Attention refunded.
    [Fact]
    public void ARefusedEndIsRejectedWithItsReason()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        var result = Simulation.Step(c, s, new Command[] { new EndOccupation("town_a", SettlementControl.Annex) });
        Assert.Contains("not awaiting the end of its occupation", Assert.Single(result.Rejected).Reason);
    }

    // R-CTRL-015: Annex then Withdraw, nothing else, no puppet.
    [Fact]
    public void TheOptionsAreAnnexThenWithdraw()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, "town_c", false, Soldier(1, 4, 0));   // town_a is a fixture city
        var before = s.Describe();
        var options = SettlementControl.EndOptions(c, s, "town_c");
        Assert.Equal(new[] { SettlementControl.Annex, SettlementControl.WithdrawOutcome }, options.Select(x => x.Outcome).ToArray());
        Assert.All(options, x => Assert.Null(x.Refusal));
        Assert.Equal(before, s.Describe());   // the options leave the state as they found it
        Assert.NotNull(SettlementControl.End(c, s, "town_c", "puppet", new List<Cause>()));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-014: annexing needs the unit that holds the place (R-CTRL-011's garrison).
    [Fact]
    public void AnnexNeedsAnIsraeliUnitOnThePlace()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, false, Soldier(1, 2, 0));
        var before = s.Describe();
        Assert.Contains("needs an Israeli military unit", SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-014: withdrawing needs the unit that holds the place too; with none there it is refused, the state unchanged.
    [Fact]
    public void WithdrawNeedsAnIsraeliUnitOnThePlace()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, false, Soldier(1, 2, 0));
        var before = s.Describe();
        var why = new List<Cause>();
        Assert.Equal("Withdraw needs an Israeli military unit on TOWN A", SettlementControl.End(c, s, "town_a", SettlementControl.WithdrawOutcome, why));
        Assert.Empty(why);
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-017 (Q-ANNEX-2 (a)): a depopulated place (no state) is never answered, under gdd-0.1 and gdd-0.3 alike.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADepopulatedPlaceIsNeverAnnexed(bool gdd03)
    {
        var c = OccupyTests.Load(gdd03);
        Assert.Equal(gdd03 ? SaveGame.Gdd03Ruleset : SaveGame.GddRuleset, c.Ruleset);
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(since: s.Date, depopulated: true);
        Assert.Empty(SettlementControl.EndOptions(c, s, "town_a"));
        Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
        Assert.True(s.Settlements["town_a"].Depopulated);
    }

    // R-CTRL-015: withdrawing at the end is the Raid withdrawal: the units leave, the record goes.
    [Fact]
    public void WithdrawAtTheEndIsTheRaidWithdrawal()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);   // stayed rows
        var s = AwaitingEnd(c, false, Soldier(2, 1, 0));
        var after = Simulation.Step(c, s, new Command[] { new EndOccupation("town_a", SettlementControl.WithdrawOutcome) });
        Assert.Empty(after.Rejected);
        Assert.False(after.State.Settlements.ContainsKey("town_a"));
        Assert.NotEqual((1, 0), (after.State.Units[2].Q, after.State.Units[2].R));
        Assert.Contains(after.Why, x => x.Target == "town_a" && x.Message.Contains("Its residents are unchanged"));
    }

    // R-CTRL-015 with OWNER QUESTION 2 (b): withdrawing from a fled place keeps the ownerless fled record.
    [Fact]
    public void WithdrawFromAFledPlaceKeepsTheFlight()
    {
        var c = CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
        var s = AwaitingEnd(c, true, Soldier(2, 1, 0));
        var since = s.Date;
        Assert.Null(SettlementControl.End(c, s, "town_a", SettlementControl.WithdrawOutcome, new List<Cause>()));
        var x = s.Settlements["town_a"];
        Assert.Equal(("", "", since, true), (x.Owner, x.State, x.Since, x.Fled));
    }

    // R-CTRL-016: a place emptied by flight is territory only, no gain ("Territory only, no gain", owner, 18 October 2026).
    [Fact]
    public void AnnexingAFledPlaceChangesOnlyItsRecord()
    {
        var c = CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
        var s = AwaitingEnd(c, true, Soldier(1, 1, 0));
        var before = s.Clone();
        var why = new List<Cause>();
        Assert.Null(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, why));
        var x = s.Settlements["town_a"];
        Assert.Equal((Movement.PlayerSide, SettlementControl.Annexed, before.Settlements["town_a"].Since, 0, true),
            (x.Owner, x.State, x.Since, x.Turns, x.Fled));
        Assert.Contains(why, w => w.Target == "town_a" && w.Message.Contains("no people, homes or jobs are added"));
        s.Settlements["town_a"] = before.Settlements["town_a"];
        Assert.Equal(before.Describe(), s.Describe());   // nothing else moved: resources, cities, units, score, objectives
        Assert.Equal(before.RngState, s.RngState);
    }

    // R-CTRL-016, OWNER QUESTION 3 (a): an annexed place pays no upkeep and needs no unit.
    [Fact]
    public void AnAnnexedPlaceCostsNothingAndStaysWithoutAUnit()
    {
        var c = CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
        var s = AwaitingEnd(c, true, Soldier(1, 1, 0));
        var first = Simulation.Step(c, s, new Command[] { new EndOccupation("town_a", SettlementControl.Annex) });
        Assert.Empty(first.Rejected);
        Assert.DoesNotContain(first.Why, x => x.Message.Contains("for the occupation of", StringComparison.Ordinal));
        s = first.State;
        s.Units.Remove(1);
        var next = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Equal((Movement.PlayerSide, SettlementControl.Annexed), (next.State.Settlements["town_a"].Owner, next.State.Settlements["town_a"].State));
        Assert.DoesNotContain(next.Why, x => (x.System == "map" && x.Target == "town_a") || x.Message.Contains("occupation of", StringComparison.Ordinal));
        Assert.Empty(SettlementControl.EndOptions(c, next.State, "town_a"));   // answered for good
    }

    // R-CTRL-016, OWNER QUESTION 1 (a): a stayed place becomes one city by the starting-city rule (people_per_unit 10):
    // 60 people are 6 units, homes equal the residents, jobs the employed (60 x 0.5 = 3 units), the usual condition.
    [Fact]
    public void AnnexingAStayedPlaceAddsExactlyOneCity()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);   // fixture rows are stayed, people 60
        var s = AwaitingEnd(c, "town_c", false, Soldier(1, 4, 0));
        var cities = s.Cities.Count;
        var why = new List<Cause>();
        Assert.Null(SettlementControl.End(c, s, "town_c", SettlementControl.Annex, why));
        var city = s.Cities["town_c"];
        Assert.Equal((6, 6, 3, 3, 0), (city.Civilians, city.HousingCapacity, city.Employed, city.JobSlots, city.Refugees));
        Assert.Equal(c.RulesAt(s.Date).Mvp!.HousingStartCondition, city.Condition);
        Assert.Equal(cities + 1, s.Cities.Count);
        Assert.Contains(why, w => w.Target == "town_c" && w.Message.Contains("Its residents join the population"));
        Assert.NotNull(SettlementControl.End(c, s, "town_c", SettlementControl.Annex, new List<Cause>()));   // no second city
        Assert.Equal(cities + 1, s.Cities.Count);
    }

    // R-CTRL-016: an id that is already a city is refused, the state unchanged (Cities.Annex adds one city, never two).
    [Fact]
    public void AnnexingOntoAnExistingCityIsRefused()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, false, Soldier(1, 1, 0));   // the fixture already has a town_a city
        var before = s.Describe();
        Assert.NotNull(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-016: a city that exists only in state runs a turn, and a Train order there is refused with a reason (it has
    // no training facility), with no exception.
    [Fact]
    public void AnAnnexedCityRunsATurnAndRefusesTraining()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = AwaitingEnd(c, "town_c", false, Soldier(1, 4, 0));
        s = Simulation.Step(c, s, new Command[] { new EndOccupation("town_c", SettlementControl.Annex) }).State;
        Assert.True(s.Cities.ContainsKey("town_c"));
        var next = Simulation.Step(c, s, new Command[] { new Train("town_c", "halutz", 1) });
        Assert.NotEmpty(Assert.Single(next.Rejected).Reason);
        Assert.True(next.State.Cities.ContainsKey("town_c"));
    }

    // R-CTRL-016, R-MOVE-005, R-ENEMY-001 (OWNER QUESTION 3 (a) on #799): an annexed target is an Israeli town for movement:
    // no longer an enemy-held locality the player's withdrawal must avoid, and closed to enemy units.
    [Fact]
    public void AnAnnexedTargetIsAnIsraeliTownForMovement()
    {
        var c = CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
        var s = AwaitingEnd(c, true, Soldier(1, 1, 0));
        Assert.Contains((1, 0), Movement.NoEntry(c, s));
        Assert.DoesNotContain((1, 0), Movement.NoEntry(c, s, true));
        Assert.Null(SettlementControl.End(c, s, "town_a", SettlementControl.Annex, new List<Cause>()));
        Assert.DoesNotContain((1, 0), Movement.NoEntry(c, s));
        Assert.Contains((1, 0), Movement.NoEntry(c, s, true));
    }

    // R-CTRL-014, R-CTRL-015 (R-CTRL-008's contract; owner, 27 October 2026, #1712: "Existing Raid/Occupy", with the preview
    // equal to the result): each end option's preview is the change resolving it makes, checked on a clone the test resolves
    // itself. end_occupation is given a cost here, so the Attention field is checked at a value other than 0. EndOptions
    // leaves the state as it found it.
    [Fact]
    public void TheEndPreviewIsTheOutcome()
    {
        var c = CaptureTests.Load(f => f["rules.yaml"] = f["rules.yaml"].Replace("end_occupation: 0", "end_occupation: 1"), CaptureTests.AllTargets);
        var s = AwaitingEnd(c, "town_c", false, Soldier(1, 4, 0));   // town_a is a fixture city
        s.Attention = 5;
        var cost = AttentionSystem.Cost(c, s, SettlementControl.EndKind);
        Assert.True(cost > 0);
        var before = s.Describe();
        var options = SettlementControl.EndOptions(c, s, "town_c");
        Assert.Equal(new[] { SettlementControl.Annex, SettlementControl.WithdrawOutcome }, options.Where(o => o.Preview != null).Select(o => o.Outcome).ToArray());
        foreach (var o in options)
        {
            var after = s.Clone();
            Assert.Null(AttentionSystem.TrySpend(c, after, SettlementControl.EndKind));
            Assert.Null(SettlementControl.End(c, after, "town_c", o.Outcome, new List<Cause>()));
            var p = o.Preview!;
            Assert.Equal(after.Attention - s.Attention, p.Attention);
            Assert.Equal(after.Treasury - s.Treasury, p.Money);
            Assert.Equal(after.Debt - s.Debt, p.Debt);
            Assert.Equal(after.Food - s.Food, p.Food);
            Assert.Equal(after.Arms - s.Arms, p.Arms);
            Assert.Equal(after.Indicators.GetValueOrDefault("morale") - s.Indicators.GetValueOrDefault("morale"), p.SocialCohesion);
            Assert.Equal(after.Indicators.GetValueOrDefault("intl_standing") - s.Indicators.GetValueOrDefault("intl_standing"), p.InternationalSupport);
            Assert.Equal(after.Score - s.Score, p.Score);
            Assert.Equal(-cost, p.Attention);
        }
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-014, R-CTRL-016 under gdd-0.3 (owner, 27 October 2026, #1712: "3 turns, keep upkeep", "Keep joined city"; owner,
    // 1 November 2026: "30 Oct wins for the MVP (Recommended)", so the annexation is for good): Occupy, three turns to the
    // end of the timer, Annex of the stayed town_c, then the soldier is removed and the place stays annexed and a city.
    // Every turn ends the same under gdd-0.1 and gdd-0.3, the Attention pool aside.
    [Fact]
    public void OccupyThenAnnexIsTheSameUnderGdd03()
    {
        var turns = new[]
        {
            new Command[] { new ResolveCapture("town_c", SettlementControl.Occupy) }, Array.Empty<Command>(), Array.Empty<Command>(),
            new Command[] { new EndOccupation("town_c", SettlementControl.Annex) }, Array.Empty<Command>(),
        };
        var expected = new[] { "occupied/2@4,0", "occupied/1@4,0", "awaiting_end/0@4,0", "annexed/0@4,0", "annexed/0@gone" };
        var runs = new[] { false, true }.Select(gdd03 =>
        {
            var c = OccupyTests.Load(gdd03);
            var s = CombatTests.With(c, Soldier(1, 4, 0));
            s.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
            var run = OccupyTests.Play(c, s, "town_c", (i, x) => { if (i == 4) x.Units.Remove(1); }, turns);
            return run;
        }).ToList();
        Assert.Equal(expected, runs[0].Select(x => x.Trace));
        Assert.Equal(expected, runs[1].Select(x => x.Trace));
        Assert.Equal(runs[0].Select(x => x.State), runs[1].Select(x => x.State));
        Assert.All(runs[1].Skip(3), x => Assert.Contains("town_c=6,3,6,3,0", x.State));   // the joined city: 60 people, 6 units, 3 employed
    }

    // R-CTRL-016, R-CTRL-014 (OWNER QUESTION 1 (a) on #799; owner, 27 October 2026, #1712: "Keep joined city"): annexing a
    // stayed town of the real content adds exactly one city, with its id, and no unit: the unit set and the next unit id
    // are what they were (no free units, no local military). SettlementControl.End is called directly, as a Step would
    // also bring in the turn's dated arrivals.
    [Theory]
    [InlineData("acre")]
    [InlineData("nazareth")]
    public void AnnexingAStayedRealTownAddsOneCityAndNoUnit(string id)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var t = c.SettlementTargets.Single(x => x.Id == id);
        Assert.Equal(ContentLoader.Stayed, t.Residents);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var u = s.Units.Values.First(x => x.Side == Movement.PlayerSide);
        s.Units[u.Id] = new Unit(u.Id, u.Type, t.Q, t.R, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full);
        s.Settlements[id] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingEnd, s.Date);
        static string Units(GameState x) => string.Join(";", x.Units.Values.Select(v => v.Canonical()));
        var (units, nextId, cities) = (Units(s), s.NextId, s.Cities.Keys.ToList());
        Assert.DoesNotContain(id, cities);
        Assert.Null(SettlementControl.End(c, s, id, SettlementControl.Annex, new List<Cause>()));
        Assert.Equal(units, Units(s));
        Assert.Equal(nextId, s.NextId);
        Assert.Equal(cities.Append(id).OrderBy(x => x, StringComparer.Ordinal), s.Cities.Keys.OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(SettlementControl.Annexed, s.Settlements[id].State);
    }
}
