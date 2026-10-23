using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>#1399 (spec 2026-10-23 section 8.4): every Why line that names a unit carries its key, a snapshot of each unit
/// it names and its values, so the UI can name the unit in the player's language, even once the unit has left the state.</summary>
public class UnitLinesTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The real start, plus a raised recruit (an id past the starting forces, home Tel Aviv) on Golani's hex.</summary>
    static (GameState s, int raised) Start()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId;
        var golani = s.Units[0];
        s.Units.Add(id, new Unit(id, "recruit", golani.Q, golani.R, side: "israel", strength: 2, homeCity: "tel_aviv"));
        s.NextId = id + 1;
        return (s, id);
    }

    static void Names(Cause line, string key, GameState s, int id)
    {
        Assert.Equal(key, line.Key);
        var u = Assert.Single(line.Units);
        Assert.Equal((id, s.Units[id].Type, s.Units[id].HomeCity), (u.Id, u.Type, u.HomeCity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AMoveNamesItsUnit(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        var to = Movement.Reachable(C, s, id).Keys.First();
        var line = Simulation.Step(C, s, new Command[] { new Move(id, to.Q, to.R) }).Why.Single(w => w.Key == WhyKeys.UnitMoved && w.Units[0].Id == id);
        Names(line, WhyKeys.UnitMoved, s, id);
        Assert.Equal($"{to.Q},{to.R}", line.Args[0]);
    }

    // The enemy's units fortify and hold in the same turn, so each line is picked by our unit's id.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FortifyHoldAndFortifyLostNameTheirUnit(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        var hex = $"{s.Units[id].Q},{s.Units[id].R}";
        Names(Simulation.Step(C, s, new Command[] { new UnitOrder(id, Orders.Fortify) }).Why.Single(w => w.Key == WhyKeys.Fortifying && w.Units[0].Id == id), WhyKeys.Fortifying, s, id);
        Names(Simulation.Step(C, s, new Command[] { new UnitOrder(id, Orders.Hold) }).Why.Single(w => w.Key == WhyKeys.Holds && w.Units[0].Id == id), WhyKeys.Holds, s, id);
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Orders.Fortify, hex, u.HomeCity);
        var to = Movement.Reachable(C, s, id).Keys.First();
        var lost = Simulation.Step(C, s, new Command[] { new Move(id, to.Q, to.R) }).Why.Single(w => w.Key == WhyKeys.FortifyLost && w.Units[0].Id == id);
        Names(lost, WhyKeys.FortifyLost, s, id);
        Assert.Equal(hex, lost.Args[0]);
    }

    // The unit leaves the state when it goes home; its line keeps its identity.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SentHomeKeepsTheUnitsIdentity(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        foreach (var front in Prisoners.Fronts) s.ChosenOptions["armistice_" + front] = "sign";
        Assert.Null(Demobilization.Check(C, s, id));   // precondition: every armistice signed
        var ended = Simulation.Step(C, s, new Command[] { new UnitOrder(id, Demobilization.SendHome) });
        Assert.False(ended.State.Units.ContainsKey(id));
        Names(ended.Why.Single(w => w.Key == WhyKeys.SentHome), WhyKeys.SentHome, s, id);
    }

    // The player's standing attack ended by a truce (Combat.EndAttacksForTruce) names its unit and the hex.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APlayerAttackEndedByATruceNamesItsUnit(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Combat.Attack, "10,10", u.HomeCity);
        var why = new List<Cause>();
        Combat.EndAttacksForTruce(s, why);
        var line = why.Single(w => w.Key == WhyKeys.AttackEndedTruce);
        Names(line, WhyKeys.AttackEndedTruce, s, id);
        Assert.Equal("10,10", line.Args[0]);
    }

    [Fact]
    public void AnEnemyAttackEndedByATruceNamesTheEnemyUnit()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var (id, e) = s.Units.First(kv => kv.Value.Side != "" && kv.Value.Side != Movement.PlayerSide);
        s.Units[id] = new Unit(e.Id, e.Type, e.Q, e.R, e.Service, e.Side, e.Strength, Combat.Attack, $"{e.Q + 1},{e.R}", e.HomeCity);
        s.Date = new DateTime(1948, 6, 15);   // a truce turn (R-ENEMY-006)
        var why = new List<Cause>();
        EnemyAI.Order(C, s, why, new DateTime(1948, 7, 15));
        Assert.Equal(id, why.Single(w => w.Key == WhyKeys.AttackEndedTruce).Units[0].Id);
    }

    [Fact]
    public void EveryKeyIsListedOnce() => Assert.Equal(WhyKeys.All.Distinct().Count(), WhyKeys.All.Count);
}
