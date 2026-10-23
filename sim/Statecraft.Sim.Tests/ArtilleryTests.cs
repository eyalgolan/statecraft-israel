using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Artillery's combat role and the armoured car (GDD Phase 5 PR 9, part 1; #748). Q-ARTY-1 and Q-ARTY-2 are the
/// plan's recommendations, updated by OQ-1 on 23 October 2026: player artillery attacks alone at once from range, never
/// holds ground, and spends Arms when it fires; enemy artillery still supports another attacker in resolver step 2. The
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

    // R-COMBAT-007 (owner, 23 October 2026, OQ-1: "One unit per attack, Civ"): artillery attacks alone from range, at once,
    // pays its arms_per_fire and loses nothing.
    [Fact]
    public void ArtilleryAloneFiresAtOnceAndLosesNothing()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Enemy(2, 2, 0, 3));
        var arms = s.Arms;
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 2, 0) });
        Assert.Empty(now.Rejected);
        Assert.Contains(now.Why, w => w.Key == WhyKeys.Engagement);
        Assert.Equal(arms - 3, now.State.Arms);
        Assert.Equal(2, now.State.Units[1].Strength);
        Assert.Equal((2, 0), now.Attacked[1]);
    }

    // R-UNIT-005 (spec 4.6): a gun's front is that of the enemy it attacked this turn, two hexes away, though it borders none.
    [Fact]
    public void AGunsFrontIsTheEnemyItAttackedThisTurn()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Enemy(2, 2, 0, 9));
        s.ChosenOptions["armistice_syria"] = "sign";
        Assert.Null(Demobilization.Check(c, s, 1));
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 2, 0) });
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, now.State, 1, now.Attacked));
        Assert.Equal("The armistice with Egypt is not signed",
            Simulation.Step(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 2, 0), new UnitOrder(1, Demobilization.SendHome) }).Rejected.Single().Reason);
    }

    // R-COMBAT-007, OQ-1: the preview and engagement count the selected player unit alone, not another standing attacker.
    [Fact]
    public void APlayersAttackDoesNotAddAStandingGun()
    {
        var c = Load();
        var s = CombatTests.With(c, Attacking(Of("gun", 1, 4, 0, 2), 2, 0), Of("soldier", 3, 3, 0, 1), Enemy(2, 2, 0, 2));
        Assert.Equal(50, Combat.Preview(c, s, 3, 2, 0)!.Value.RatioPercent);
        var r = Step(c, s, new UnitOrder(3, Combat.Attack, 2, 0));
        Assert.Empty(r.Rejected);
        Assert.Equal(s.Arms - 2, r.State.Arms);
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

    // R-COMBAT-007: artillery never holds ground. The hold order is refused with a reason; fortify is not.
    [Fact]
    public void ArtilleryDoesNotHoldGround()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2));
        var refused = Step(c, s, new UnitOrder(1, Orders.Hold));
        Assert.Equal("Artillery does not hold ground", Assert.Single(refused.Rejected).Reason);
        Assert.Equal("Artillery does not hold ground", Orders.Check(c, s, 1, Orders.Hold));   // the unit card shows it (#754)
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

    // ---- R-UNIT-008 (part 2; Q-ARTY-3, owner, 15 October 2026: "Take all recommendations"): the guns and the armoured cars
    // are acquired by training at a military camp, a crew of civilians plus an Arms and Money cost. Fixture values: the gun
    // trains 2 turns for IL 1.5m and 3 Arms a group, the car 2 turns for IL 1.0m and 2 Arms a group; the fixture's stock is 20.
    const string Trained = Fixture.GddUnitTypes
        + "  - { id: gun, kind: unit, side: israel, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3, facility: military_camp, training_turns: 2, training_cost: 1.5, training_arms: 3 }\n"
        + "  - { id: car, kind: unit, side: israel, class: armour, movement_points: 5, facility: military_camp, training_turns: 2, training_cost: 1.0, training_arms: 2 }\n";

    // R-UNIT-008, R-TRAIN-001, R-ARMS-003: Train(city, gun, n) takes n civilians without work as the crew, pays n times the
    // type's Money and takes n times its training_arms when it starts; the cohort keeps the Arms it took.
    [Theory]
    [InlineData("gun", 3, 1.5, "artillery training")]
    [InlineData("car", 2, 1.0, "armoured car training")]
    public void AMilitaryCampTrainsACrewForArmsAndMoney(string track, int arms, double cost, string course)
    {
        var c = Load(Trained);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new Train("town_a", track, 2));
        Assert.Empty(r.Rejected);
        var k = Assert.Single(r.State.Cohorts.Values);
        Assert.Equal((track, "town_a", 2, 2, 2 * arms), (k.Type, k.City, k.Count, k.TurnsLeft + 1, k.Arms));
        Assert.Equal(s.Arms - 2 * arms, r.State.Arms);
        Assert.Equal(s.Cities["town_a"].Civilians - 2, r.State.Cities["town_a"].Civilians);
        Assert.Contains(r.Why, w => w.Target == "town_a" && w.Message.StartsWith("Town A: 2 groups began " + course + ", cost ", StringComparison.Ordinal));
        Assert.Contains(r.Why, w => w.Target == "arms" && w.Delta == -2 * arms && w.Message == $"Town A: {course} took {2 * arms} Arms");
        var paid = Assert.Single(r.Why, w => w.Message.StartsWith("Town A: 2 groups began", StringComparison.Ordinal));
        Assert.Contains(PlayerText.Money(c, "treasury", 2 * cost), paid.Message);
    }

    // R-UNIT-008, R-UNIT-003, R-HOUSE-003 (Q-HOUSE-5): a finished crew is one formation of its own type at its city's
    // settlement hex, strength = its groups, housed in that city. It is not a recruit, so service never promotes it (R-UNIT-004).
    [Theory]
    [InlineData("gun", "artillery")]
    [InlineData("car", "armoured cars")]
    public void AFinishedCrewIsOneFormationOfItsType(string track, string noun)
    {
        var c = Load(Trained);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.Cohorts.Add(id, new Cohort(id, track, "town_a", 2, 1, 4));
        var next = s.NextId;
        var arms = s.Arms;
        var housed = Cities.Housed(s, s.Cities["town_a"]);
        var why = new List<Cause>();
        Training.Progress(c, s, why);
        Assert.Empty(s.Cohorts);
        var u = s.Units[next];
        var hex = c.SettlementHexes.Single(h => h.Id == "town_a");
        Assert.Equal((track, "israel", 2, hex.Q, hex.R, "town_a"), (u.Type, u.Side, u.Strength, u.Q, u.R, u.HomeCity));
        Assert.Equal(next + 1, s.NextId);
        Assert.Equal(arms, s.Arms);
        Assert.Equal(housed, Cities.Housed(s, s.Cities["town_a"]));
        Assert.Contains(why, w => w.Message.EndsWith("training and are " + noun, StringComparison.Ordinal));
        for (int i = 0; i < 10; i++) Military.Serve(s, c.RulesAt(s.Date).Mvp!, why);
        Assert.Equal(track, s.Units[next].Type);
    }

    // R-UNIT-008, R-ARMS-003: a stock short of the crew's Arms refuses the order, state and Attention as if never given;
    // cancelling the cohort (R-TRAIN-002) returns the Arms it took.
    [Fact]
    public void TooFewArmsRefusesACrewAndCancellingReturnsThem()
    {
        var c = Load(Trained);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Arms = 5;
        var r = Step(c, s, new Train("town_a", "gun", 2));
        Assert.Equal("Town A: not enough Arms for artillery training (need 6, have 5)", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
        s.Arms = 6;
        var trained = Step(c, s, new Train("town_a", "gun", 2)).State;
        Assert.Equal(0, trained.Arms);
        var back = Step(c, trained, new CancelTraining(trained.Cohorts.Keys.Single()));
        Assert.Empty(back.Rejected);
        Assert.Equal(6, back.State.Arms);
        Assert.Contains(back.Why, w => w.Target == "arms" && w.Delta == 6 && w.Message == "Town A: artillery training returned 6 Arms");
    }

    // R-UNIT-008: a trained military type other than the recruit carries its own Arms a group, at least 1; the recruit's is
    // rules.yaml mvp.arms_per_recruit_group (R-ARMS-003), and a type nobody trains has none.
    [Theory]
    [InlineData("  - { id: g, kind: unit, side: israel, class: armour, movement_points: 2, facility: military_camp, training_turns: 2, training_cost: 1.0 }\n", "types[g].training_arms", "required for a trained military type, at least 1")]
    [InlineData("  - { id: g, kind: unit, side: israel, class: armour, movement_points: 2, facility: military_camp, training_turns: 2, training_cost: 1.0, training_arms: 0 }\n", "types[g].training_arms", "required for a trained military type, at least 1")]
    [InlineData("  - { id: g, kind: unit, side: israel, class: armour, movement_points: 2, training_arms: 2 }\n", "types[g].training_arms", "only a type with a facility trains")]
    [InlineData("  - { id: g, kind: unit, facility: hakhshara, training_turns: 2, training_cost: 1.0, training_arms: 2 }\n", "types[g].training_arms", "only a trained military type other than the recruit")]
    public void TheTrainingArmsAreValidated(string row, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Fixture.GddUnitTypes + row));
        Assert.Equal("unit_types.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-UNIT-008: the recruit's Arms stay in rules.yaml, so a training_arms on it is refused rather than read by nothing.
    [Fact]
    public void ARecruitTakesNoTrainingArms()
    {
        var types = Fixture.GddUnitTypes.Replace("training_cost: 0.5, movement_points: 3 }", "training_cost: 0.5, movement_points: 3, training_arms: 1 }");
        Assert.NotEqual(Fixture.GddUnitTypes, types);
        var ex = Assert.Throws<ContentException>(() => Load(types));
        Assert.Equal("types[recruit].training_arms", ex.Field);
        Assert.Contains("only a trained military type other than the recruit", ex.Message);
    }

    // R-UNIT-008: the real types train at a military camp with the values proposed on #748 (22 October 2026, for the owner
    // to set or accept): 3 turns and IL 1.0m a group for both, 3 Arms a group for the guns and 2 for the armoured car.
    [Fact]
    public void TheRealTypesTrainAtAMilitaryCamp()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var gun = c.UnitTypes.Single(t => t.Id == "artillery");
        var car = c.UnitTypes.Single(t => t.Id == "armoured_car");
        Assert.Equal(("military_camp", 3, 1.0, (int?)3), (gun.Facility, gun.TrainingTurns, gun.TrainingCost, gun.TrainingArms));
        Assert.Equal(("military_camp", 3, 1.0, (int?)2), (car.Facility, car.TrainingTurns, car.TrainingCost, car.TrainingArms));
    }
}
