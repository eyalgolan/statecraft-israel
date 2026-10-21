using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Artillery's combat role and the armoured car (GDD Phase 5 PR 9, part 1; #748). Q-ARTY-1 and Q-ARTY-2 are the
/// plan's recommendations, taken by the owner on 15 October 2026 ("Take all recommendations", #375): artillery adds its
/// strength to an attack on a hex within its range, never attacks alone or holds ground, and spends Arms when it fires; the
/// armoured car has its own combat factor and no other special rule. The fixture's numbers are fixture values: a gun with
/// range 2, 3 Arms a firing and a factor of 200, a car with a factor of 150, and CombatTests' table (below 2:1 both sides
/// lose 1, from 2:1 the defenders lose 1 and retreat), so a test does not depend on the draw.</summary>
public class ArtilleryTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: gun, kind: unit, side: israel, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3, combat_percent: 200 }\n"
        + "  - { id: car, kind: unit, side: israel, class: armour, movement_points: 5, combat_percent: 150 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    /// <summary>MovementTests' 6 x 3 coastal plain; town_a is on (1, 0).</summary>
    static Content Load(string types = Types) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
    })));

    static Unit Of(string type, int id, int q, int r, int strength, string side = "israel") => new Unit(id, type, q, r, side: side, strength: strength);
    static Unit Enemy(int id, int q, int r, int strength) => Of("enemy", id, q, r, strength, "egypt");
    static Unit Attacking(Unit u, int q, int r) =>
        new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Combat.Attack, FormattableString.Invariant($"{q},{r}"));

    static StepResult Step(Content c, GameState s, params Command[] cmds) => Simulation.Step(c, s, cmds);

    // R-COMBAT-007: the guns take an attack on an enemy hex within their range, two hexes here; beyond it the order is
    // refused, the state untouched. An infantry unit still needs the hex next to it.
    [Fact]
    public void ArtilleryTakesAnAttackOnAHexWithinItsRange()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Of("soldier", 3, 4, 1, 2), Enemy(2, 2, 0, 3), Enemy(4, 1, 0, 3));
        Assert.Empty(Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected);
        var refused = Step(c, s, new UnitOrder(1, Combat.Attack, 1, 0));   // three hexes away
        Assert.Equal("That hex is beyond the unit's range", Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), refused.State.Hash());
        Assert.Equal("That hex is not next to the unit", Assert.Single(Step(c, s, new UnitOrder(3, Combat.Attack, 2, 0)).Rejected).Reason);
    }

    // R-COMBAT-007: artillery never attacks alone. With no other unit of its side attacking the hex, it holds its fire: no
    // draw, no Arms, no loss, a line, and the order stands for a later turn.
    [Fact]
    public void ArtilleryAloneHoldsItsFire()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Enemy(2, 2, 0, 3));
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0));
        Assert.Empty(r.Rejected);
        Assert.Equal(3, r.State.Units[2].Strength);
        Assert.Equal(s.Arms, r.State.Arms);
        Assert.Equal(Step(c, s).State.RngState, r.State.RngState);
        Assert.Equal((Combat.Attack, "2,0"), (r.State.Units[1].Order, r.State.Units[1].OrderTarget));
        Assert.Contains(r.Why, w => w.Message == "Unit 1 holds its fire: no attack on hex 2,0 to support");
    }

    // R-COMBAT-007: in support, the guns add their strength times their factor and pay their own Arms. 1 + 2 x 200% = 5
    // against 2 is 250 percent, the 2:1 row; the soldier alone would be at 50.
    [Fact]
    public void ArtilleryAddsItsStrengthTimesItsFactorToAnAttack()
    {
        var c = Load();
        var s = CombatTests.With(c, Attacking(Of("gun", 1, 4, 0, 2), 2, 0), Of("soldier", 3, 3, 0, 1), Enemy(2, 2, 0, 2));
        Assert.Equal(250, Combat.Preview(c, s, 3, 2, 0)!.Value.RatioPercent);
        var r = Step(c, s, new UnitOrder(3, Combat.Attack, 2, 0));
        Assert.Empty(r.Rejected);
        Assert.Equal(1, r.State.Units[2].Strength);   // the 2:1 row: the defenders lose 1 and retreat, the attackers nothing
        Assert.NotEqual((2, 0), (r.State.Units[2].Q, r.State.Units[2].R));
        Assert.Equal(s.Arms - 2 - 3, r.State.Arms);   // the soldier's attack and the guns' firing
    }

    // R-COMBAT-007: the guns fire from behind the line, so the attackers' losses fall on the other attackers only. 1 + 2 x
    // 200% = 5 against 3 is 166 percent: the attackers lose 1. The gun is the stronger attacker, so only this rule keeps it
    // whole and puts the loss on the soldier.
    [Fact]
    public void TheAttackersLossesSpareTheGuns()
    {
        var c = Load();
        var s = CombatTests.With(c, Attacking(Of("gun", 1, 4, 0, 2), 2, 0), Attacking(Of("soldier", 3, 3, 0, 1), 2, 0), Enemy(2, 2, 0, 3));
        var r = Step(c, s);
        Assert.Equal(2, r.State.Units[1].Strength);
        Assert.False(r.State.Units.ContainsKey(3));   // the soldier took the loss and was destroyed
        Assert.Equal(2, r.State.Units[2].Strength);
        Assert.Contains(r.Why, w => w.Message.StartsWith("The attack on hex 2,0 (units 1, 3 against unit 2,", StringComparison.Ordinal));   // id order, though the guns join last
    }

    // R-COMBAT-007: the guns pay after the other attackers; a stock that cannot pay for the firing calls it off, and the
    // attack goes ahead without them. At the order, the firing counts the attacks already ordered.
    [Fact]
    public void ArtilleryTheStockCannotPayForIsCalledOff()
    {
        var c = Load();
        var s = CombatTests.With(c, Attacking(Of("gun", 1, 4, 0, 2), 2, 0), Attacking(Of("soldier", 3, 3, 0, 1), 2, 0), Enemy(2, 2, 0, 2));
        s.Arms = 4;   // the soldier's 2, then 2 left for the guns' 3
        var r = Step(c, s);
        Assert.Contains(r.Why, w => w.Message == "Unit 1's attack on hex 2,0 is called off: it needs 3 Arms and the stock has 2");
        Assert.Equal("", r.State.Units[1].Order);
        Assert.Equal(2, r.State.Arms);
        Assert.False(r.State.Units.ContainsKey(3));   // 1 against 2, the first row: both sides lose 1
        Assert.Equal(1, r.State.Units[2].Strength);

        var t = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Attacking(Of("soldier", 3, 3, 0, 1), 2, 0), Enemy(2, 2, 0, 2));
        t.Arms = 4;
        Assert.Equal("An attack needs 3 Arms and the stock has 4, of which 2 go to attacks already ordered",
            Assert.Single(Step(c, t, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected).Reason);

        // The other way round: a gun already ordered counts at its own 3 Arms, not at one attack's 2.
        var g = CombatTests.With(c, Attacking(Of("gun", 1, 4, 0, 2), 2, 0), Of("soldier", 3, 3, 0, 1), Enemy(2, 2, 0, 2));
        g.Arms = 4;
        Assert.Equal("An attack needs 2 Arms and the stock has 4, of which 3 go to attacks already ordered",
            Assert.Single(Step(c, g, new UnitOrder(3, Combat.Attack, 2, 0)).Rejected).Reason);
    }

    // R-COMBAT-007: artillery never holds ground. The hold order is refused with a reason; fortify is not.
    [Fact]
    public void ArtilleryDoesNotHoldGround()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2));
        var refused = Step(c, s, new UnitOrder(1, Orders.Hold));
        Assert.Equal("Artillery does not hold ground", Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), refused.State.Hash());
        Assert.Empty(Step(c, s, new UnitOrder(1, Orders.Fortify)).Rejected);
    }

    // R-COMBAT-008: the armoured car's factor counts attacking and defending. 2 x 150% = 3 against 3 is 100 percent; 3
    // against the car's 3 is 100 too. Without the factor they would be 66 and 150.
    [Fact]
    public void TheArmouredCarFightsAtItsFactor()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("car", 1, 3, 0, 2), Enemy(2, 2, 0, 3));
        Assert.Equal(100, Combat.Preview(c, s, 1, 2, 0)!.Value.RatioPercent);
        Assert.Equal(100, Combat.Preview(c, s, 2, 3, 0)!.Value.RatioPercent);
    }

    // R-COMBAT-007, R-COMBAT-008: range and Arms per firing belong to an artillery type, which needs both; a combat factor
    // belongs to a military type and is at least 1.
    [Theory]
    [InlineData("  - { id: g, kind: unit, side: israel, class: artillery, movement_points: 2, arms_per_fire: 1 }\n", "types[g].range", "required for an artillery type, at least 1")]
    [InlineData("  - { id: g, kind: unit, side: israel, class: artillery, movement_points: 2, range: 2 }\n", "types[g].arms_per_fire", "required for an artillery type, at least 1")]
    [InlineData("  - { id: g, kind: unit, side: israel, class: infantry, movement_points: 2, range: 2 }\n", "types[g].range", "only an artillery type has a range")]
    [InlineData("  - { id: g, kind: unit, side: israel, class: armour, movement_points: 2, arms_per_fire: 1 }\n", "types[g].arms_per_fire", "only an artillery type fires")]
    [InlineData("  - { id: g, kind: unit, combat_percent: 150 }\n", "types[g].combat_percent", "only a military type has a combat factor")]
    [InlineData("  - { id: g, kind: unit, side: israel, class: armour, movement_points: 2, combat_percent: 0 }\n", "types[g].combat_percent", "must be at least 1")]
    public void TheCombatValuesAreValidated(string row, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Fixture.GddUnitTypes + row));
        Assert.Equal("unit_types.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-COMBAT-007, R-COMBAT-008: the real types carry the values the owner accepted on #748 (21 October 2026).
    [Fact]
    public void TheRealTypesCarryTheirCombatValues()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var gun = c.UnitTypes.Single(t => t.Id == "artillery");
        Assert.Equal((2, 2, 200), (gun.Range, gun.ArmsPerFire, gun.CombatPercent));
        Assert.Equal(150, c.UnitTypes.Single(t => t.Id == "armoured_car").CombatPercent);
        Assert.Null(c.UnitTypes.Single(t => t.Id == "veteran_soldier").CombatPercent);
    }
}
