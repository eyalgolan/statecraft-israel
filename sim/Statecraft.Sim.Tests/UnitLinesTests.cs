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

    // Task 4b: the combat lines, on CombatTests' fixture (below 2:1 both sides lose 1; from 2:1 the defenders lose 1 and
    // retreat, whatever the draw).
    static Content Fixture6x3() => TurnPreviewTests.Plain();
    static Unit Ours(int id, int q, int r, int strength) => new Unit(id, "soldier", q, r, side: "israel", strength: strength, homeCity: "town_a");
    static Unit Theirs(int id, int q, int r, int strength, int tq, int tr) =>
        new Unit(id, "enemy", q, r, side: "egypt", strength: strength, order: Combat.Attack, orderTarget: $"{tq},{tr}");

    // The engagement names the attackers, then the defenders, and the count of attackers; a destroyed unit keeps its identity.
    [Fact]
    public void TheEngagementAndADestroyedUnitNameTheirUnits()
    {
        var c = Fixture6x3();
        var s = CombatTests.With(c, Ours(1, 2, 1, 1), Theirs(2, 3, 1, 9, 2, 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        var fight = why.Single(w => w.Key == WhyKeys.Engagement);
        Assert.Equal(new[] { 2, 1 }, fight.Units.Select(u => u.Id));
        Assert.Equal(("2,1", "1"), (fight.Args[0], fight.Args[4]));
        Assert.False(s.Units.ContainsKey(1));
        var gone = why.Single(w => w.Key == WhyKeys.Destroyed).Units.Single();
        Assert.Equal((1, "soldier", "town_a"), (gone.Id, gone.Type, gone.HomeCity));
    }

    [Fact]
    public void ARetreatNamesTheUnit()
    {
        var c = Fixture6x3();
        var s = CombatTests.With(c, Ours(1, 2, 1, 3), Theirs(2, 3, 1, 9, 2, 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        Assert.Equal(1, why.Single(w => w.Key == WhyKeys.Retreated).Units.Single().Id);
    }

    // 0,0's land neighbours on the 6 x 3 board are 1,0 and 0,1: with enemy units on both, the defender has nowhere to go.
    [Fact]
    public void ASurrenderNamesTheUnit()
    {
        var c = Fixture6x3();
        var s = CombatTests.With(c, Ours(1, 0, 0, 3), Theirs(2, 1, 0, 9, 0, 0), new Unit(3, "enemy", 0, 1, side: "egypt", strength: 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        var line = why.Single(w => w.Key == WhyKeys.SurrenderedHeld);   // the player's soldiers, held by the enemy (Prisoners.cs:40)
        Assert.Equal((1, "town_a"), (line.Units.Single().Id, line.Units.Single().HomeCity));
    }

    // An enemy unit that surrenders to the player's attack becomes prisoners: the other key.
    [Fact]
    public void AnEnemySurrenderIsPrisoners()
    {
        var c = Fixture6x3();
        var mine = new Unit(2, "soldier", 1, 0, side: "israel", strength: 9, order: Combat.Attack, orderTarget: "0,0");
        var s = CombatTests.With(c, new Unit(1, "enemy", 0, 0, side: "egypt", strength: 3), mine, Ours(3, 0, 1, 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        Assert.Equal(1, why.Single(w => w.Key == WhyKeys.SurrenderedPrisoners).Units.Single().Id);
    }

    // The enemy's standing attacks (R-ENEMY-001; the player's are fought at once): the attack order, an attack that ends with
    // no unit there and guns holding fire name their unit and the hex.
    [Fact]
    public void TheEnemysAttackOrderLinesNameTheirUnit()
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Fixture.GddUnitTypes
                + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
                + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n"
                + "  - { id: egun, kind: unit, side: egypt, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3 }\n",
            ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
        })));
        var s = CombatTests.With(c, new Unit(9, "enemy", 3, 0, side: "egypt", strength: 5, homeCity: "gaza"), Ours(1, 2, 0, 3));
        var given = new List<Cause>();
        Assert.Null(Combat.Give(c, s, 9, 2, 0, given, enemy: true));
        var ordered = given.Single(w => w.Key == WhyKeys.AttackOrdered);
        Assert.Equal((9, "gaza", "2,0"), (ordered.Units.Single().Id, ordered.Units.Single().HomeCity, ordered.Args[0]));
        s.Units.Remove(1);
        var ended = new List<Cause>();
        Combat.Resolve(c, s, ended);
        var end = ended.Single(w => w.Key == WhyKeys.AttackEnded);
        Assert.Equal((9, "2,0"), (end.Units.Single().Id, end.Args[0]));

        var gun = new Unit(5, "egun", 4, 0, side: "egypt", strength: 2, order: Combat.Attack, orderTarget: "2,0");
        var quiet = CombatTests.With(c, gun, Ours(1, 2, 0, 3));
        var held = new List<Cause>();
        Combat.Resolve(c, quiet, held);
        var fire = held.Single(w => w.Key == WhyKeys.HoldsFire);
        Assert.Equal((5, "2,0"), (fire.Units.Single().Id, fire.Args[0]));
    }

    [Fact]
    public void AWithdrawalNamesTheUnit()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, CaptureTests.Soldier(1, 1, 0));
        s = Simulation.Step(c, s, new Command[0]).State;   // captured at the turn's end
        var raid = Simulation.Step(c, s, new Command[] { new ResolveCapture("town_a", SettlementControl.Raid) });
        var line = raid.Why.Single(w => w.Key == WhyKeys.Withdrew);
        Assert.Equal((1, "town_a"), (line.Units.Single().Id, line.Args[0]));
    }
}
