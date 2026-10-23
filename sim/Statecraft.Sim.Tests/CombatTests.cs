using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Combat (GDD Phase 5 PR 8, #747): UnitOrder(unit, attack, q, r) and resolver step 2. Q-COMBAT-1 to Q-COMBAT-4 are
/// the plan's recommendations, taken by the owner on 15 October 2026 ("Take all recommendations", #375); Q-CIV-2 is his
/// "Enemy units only, no effect" and Q-CIV-4 his "2+3". The numbers were accepted by the owner on #747 (20 October 2026). The fixture's numbers are
/// fixture values: 10 people a population unit, 2 Arms an attack, hills 150, fortify 150, and a two-face table whose faces
/// agree, so a test does not depend on the draw: below 2:1 both sides lose 1, from 2:1 the defenders lose 1 and retreat.</summary>
public class CombatTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n"
        + "  - { id: ally, kind: unit, side: jordan, class: infantry, movement_points: 3 }\n";

    /// <summary>MovementTests' 6 x 3 coastal plain, with <paramref name="biomes"/> replacing hexes; town_a is on (1, 0).</summary>
    static Content Load(params (int Q, int R, string Biome)[] biomes) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(biomes), ["corridors.yaml"] = "corridors: []\n"
    })));

    internal static GameState With(Content c, params Unit[] units)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        foreach (var u in units) s.Units.Add(u.Id, u);
        s.NextId = units.Length == 0 ? s.NextId : Math.Max(s.NextId, units.Max(u => u.Id) + 1);
        return s;
    }

    /// <summary>The real content, loaded once for the tests that play it.</summary>
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    static Unit Soldier(int id, int q, int r, int strength) => new Unit(id, "soldier", q, r, side: "israel", strength: strength);
    static Unit Enemy(int id, int q, int r, int strength) => new Unit(id, "enemy", q, r, side: "egypt", strength: strength);
    static Unit Attacking(Unit u, int q, int r) =>
        new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Combat.Attack, FormattableString.Invariant($"{q},{r}"), u.HomeCity);

    static StepResult Step(Content c, GameState s, params Command[] cmds) => Simulation.Step(c, s, cmds);

    static List<Cause> Resolve(Content c, GameState s)
    {
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        return why;
    }

    // R-HOUSE-003, R-COMBAT-001: issuing an attack preserves the existing formation's housing home.
    [Fact]
    public void GivingAnAttackRetainsTheFormationsHomeAndHousing()
    {
        var c = Load();
        var s = With(c, new Unit(1, "soldier", 3, 0, side: "israel", strength: 5, homeCity: "town_a"), Enemy(2, 2, 0, 5));
        Assert.Equal("town_a", s.Units[1].HomeCity);
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));

        Assert.Null(Combat.Give(c, s, 1, 2, 0, new List<Cause>()));

        var u = s.Units[1];
        Assert.Equal((1, "israel", 5, "town_a", Combat.Attack, "2,0"), (u.Id, u.Side, u.Strength, u.HomeCity, u.Order, u.OrderTarget));
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));
    }

    // R-HOUSE-003, R-COMBAT-001: ending an attack does not release a surviving formation's housing.
    [Fact]
    public void EndingAnAttackRetainsTheFormationsHomeAndHousing()
    {
        var c = Load();
        var s = With(c, Attacking(new Unit(1, "soldier", 3, 0, side: "israel", strength: 5, homeCity: "town_a"), 2, 0));
        Assert.Equal("town_a", s.Units[1].HomeCity); // The fixture must not lose the home before the Act step.
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));

        Resolve(c, s); // There is no defender, so the standing attack ends without combat.

        var u = s.Units[1];
        Assert.Equal((1, "israel", 5, "town_a", "", ""), (u.Id, u.Side, u.Strength, u.HomeCity, u.Order, u.OrderTarget));
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));
    }

    // R-HOUSE-003, R-COMBAT-003: isolate the retreat copy from the separate surviving-loss copy.
    [Fact]
    public void RetreatingRetainsTheFormationsHomeAndHousing()
    {
        var c = Load();
        c.Rules.Mvp!.CombatTable[1].DefenderLoss = new List<int> { 0, 0 }; // Fixture: retreat without losses.
        var s = With(c, Attacking(Enemy(1, 3, 0, 10), 2, 0), new Unit(2, "soldier", 2, 0, side: "israel", strength: 5, homeCity: "town_a"));
        Assert.Equal("town_a", s.Units[2].HomeCity);
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));

        Resolve(c, s);

        var u = s.Units[2];
        Assert.Equal((2, "israel", 5, "town_a", 1, 0), (u.Id, u.Side, u.Strength, u.HomeCity, u.Q, u.R));
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));
    }

    // R-HOUSE-003, R-COMBAT-003: casualties release only their strength, not the survivor's entire home allocation.
    [Fact]
    public void SurvivingLossReleasesOnlyLostStrengthFromHomeHousing()
    {
        var c = Load();
        var s = With(c, Attacking(Enemy(1, 3, 0, 5), 2, 0), new Unit(2, "soldier", 2, 0, side: "israel", strength: 5, homeCity: "town_a"));
        Assert.Equal("town_a", s.Units[2].HomeCity);
        Assert.Equal(15, Cities.Housed(s, s.Cities["town_a"]));

        Resolve(c, s); // The fixture's lower row loses one per side and does not retreat.

        var u = s.Units[2];
        Assert.Equal((2, "israel", 4, "town_a", 2, 0), (u.Id, u.Side, u.Strength, u.HomeCity, u.Q, u.R));
        Assert.Equal(14, Cities.Housed(s, s.Cities["town_a"]));
    }

    // R-HOUSE-003: a trained formation's home survives a real attack command and command-log replay.
    [Fact]
    public void AHomeFormationRetainsHousingThroughAttackCommandReplay()
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types,
            ["map/hexes.json"] = MovementTests.Board(),
            ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n  - id: opponent\n    service: army\n    side: egypt\n    status: draft\n    strength: { value: 50, as_of: '1948-05-15' }\n    anchors:\n      - { place: Fixture, from: '1948-05-15', to: '1949-01-01' }\n",
            ["map/unit_anchor_hexes.yaml"] = "anchors:\n  - { id: opponent, anchor: 0, q: 2, r: 0 }\n",
        })));
        var g = GameSession.New(c, 1, Mode.Historical);
        Assert.Empty(g.Step(new Command[] { new Train("town_a", "recruit", 2) }).Rejected);
        g.Step(new Command[0]);
        var recruit = Assert.Single(g.State.Units.Values, u => u.Type == "recruit");
        Assert.Equal(("town_a", 2), (recruit.HomeCity, recruit.Strength));
        Assert.Empty(g.State.Cohorts);
        var city = g.State.Cities["town_a"];
        Assert.Equal(city.Civilians + city.Craftsmen + city.Recruits + 2, Cities.Housed(g.State, city));

        var result = g.Step(new Command[] { new UnitOrder(recruit.Id, Combat.Attack, 2, 0) });

        Assert.Empty(result.Rejected);
        var survivor = g.State.Units[recruit.Id];
        Assert.Equal((recruit.Id, "israel", 1, "town_a"), (survivor.Id, survivor.Side, survivor.Strength, survivor.HomeCity));
        var loaded = GameSession.Load(c, g.Save.Write(), out var warning);
        Assert.Empty(warning);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        Assert.Equal(g.State.Describe(), loaded.State.Describe());
        Assert.Equal(survivor.Canonical(), loaded.State.Units[recruit.Id].Canonical());
        city = loaded.State.Cities["town_a"];
        Assert.Equal(city.Civilians + city.Craftsmen + city.Recruits + 1, Cities.Housed(loaded.State, city));
    }

    // R-COMBAT-001: an attack names an adjacent hex that holds an enemy unit, and the player's unit must have the Arms for
    // it. Every refusal leaves the state as it was.
    [Theory]
    [InlineData(9, 2, 0, "There is no such unit")]
    [InlineData(2, 2, 0, "That unit is not yours")]
    [InlineData(3, 2, 0, "Only a military unit takes that order")]
    [InlineData(1, 5, 0, "That hex is not next to the unit")]
    [InlineData(1, 3, 1, "There is no enemy unit on that hex")]   // the halutz is no enemy
    public void AnAttackIsRefusedWithAReason(int unit, int q, int r, string reason)
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3), new Unit(3, "halutz", 3, 1));
        var before = s.Hash();
        Assert.Equal(reason, Combat.Check(c, s, unit, q, r));   // the unit card asks before it offers the target (#754)
        Assert.Equal(before, s.Hash());
        var refused = Step(c, s, new UnitOrder(unit, Combat.Attack, q, r));
        Assert.Equal(reason, Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), refused.State.Hash());
    }

    [Fact]
    public void AnAttackNeedsAHexAndTheArms()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3));
        Assert.Equal("An attack names a hex", Assert.Single(Step(c, s, new UnitOrder(1, Combat.Attack)).Rejected).Reason);
        Assert.Equal("An attack names a hex", Combat.Check(c, s, 1, null, null));
        Assert.Equal("That order names no hex", Assert.Single(Step(c, s, new UnitOrder(1, Orders.Hold, 2, 0)).Rejected).Reason);
        s.Arms = 1;   // the fixture's attack takes 2
        Assert.Equal("An attack needs 2 Arms and the stock has 1", Assert.Single(Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected).Reason);
        Assert.Equal("An attack needs 2 Arms and the stock has 1", Combat.Check(c, s, 1, 2, 0));
        s.Arms = 2;
        var given = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0)).State;   // resolved the same turn
        Assert.Equal(0, given.Arms);
        Assert.Equal("That unit already has that order", Assert.Single(Step(c, given, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected).Reason);
        Assert.Equal("That unit already has that order", Combat.Check(c, given, 1, 2, 0));
    }

    // R-COMBAT-001: Check is null exactly where Give takes the attack (the unit card offers the target, #754), changes
    // nothing, and counts the attacks already standing as Give does (Q-COMBAT-4).
    [Fact]
    public void CheckIsNullForAnAttackGiveTakes()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 5), Soldier(3, 1, 1, 5), Enemy(2, 2, 0, 3));
        s.Arms = 2;   // the fixture's attack takes 2
        var before = s.Hash();
        Assert.Null(Combat.Check(c, s, 1, 2, 0));
        Assert.Null(Combat.Check(c, s, 3, 2, 0));
        Assert.Equal(before, s.Hash());
        Assert.Empty(Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected);
        s.Units[1] = Attacking(s.Units[1], 2, 0);
        Assert.Equal("An attack needs 2 Arms and the stock has 2, of which 2 go to attacks already ordered", Combat.Check(c, s, 3, 2, 0));
    }

    // R-ENEMY-001: with enemy, Check runs the same checks with the sides swapped and asks for no Arms (Q-ENEMY-4), as Give
    // does for the enemy AI.
    [Fact]
    public void CheckForTheEnemySwapsTheSidesAndAsksNoArms()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3));
        s.Arms = 0;
        Assert.Null(Combat.Check(c, s, 2, 3, 0, enemy: true));
        Assert.Equal("That unit is not yours", Combat.Check(c, s, 1, 2, 0, enemy: true));
        Assert.Equal("An attack needs 2 Arms and the stock has 0", Combat.Check(c, s, 1, 2, 0));
    }

    // Q-UI-1: the preview's outcome bands are its combat_table row's faces, the same outcomes together, in people. The
    // fixture's two faces agree: below 2:1 both sides lose 1 unit (10 people); from 2:1 the defenders lose 1 and retreat.
    [Fact]
    public void TheOutcomesAreTheRowsFacesInPeople()
    {
        var c = Load();
        var s = With(c);
        Assert.Equal(new[] { (2, 2, 10, 10, false) }, Combat.Outcomes(c, s, 0));
        Assert.Equal(new[] { (2, 2, 0, 10, true) }, Combat.Outcomes(c, s, 1));
    }

    // Q-UI-1: on the real table every row's bands cover its faces exactly once, in the order each outcome first appears, and
    // no two bands are the same outcome.
    [Fact]
    public void TheRealTablesOutcomesCoverEachRowsFaces()
    {
        var s = Simulation.NewGame(Real, 1, Mode.Historical);
        var mvp = Real.RulesAt(s.Date).Mvp!;
        for (int row = 0; row < mvp.CombatTable.Count; row++)
        {
            var t = mvp.CombatTable[row];
            var faces = Enumerable.Range(0, t.AttackerLoss.Count)
                .Select(i => (t.AttackerLoss[i] * mvp.PeoplePerUnit, t.DefenderLoss[i] * mvp.PeoplePerUnit, t.DefenderRetreats[i] == 1)).ToList();
            var bands = Combat.Outcomes(Real, s, row);
            Assert.Equal(faces.Distinct(), bands.Select(b => (b.AttackersLose, b.DefendersLose, b.Retreat)));
            Assert.All(bands, b => Assert.Equal((faces.Count(f => f == (b.AttackersLose, b.DefendersLose, b.Retreat)), faces.Count), (b.Faces, b.Of)));
        }
    }

    // Q-COMBAT-4: the Arms are paid at resolution, so a new attack counts the attacks already ordered. With the stock for
    // one attack, two new orders in one turn: the first is taken, the second refused (#1194 review).
    [Fact]
    public void ASecondAttackTheStockCannotPayForIsRefused()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 5), Soldier(3, 1, 1, 5), Enemy(2, 2, 0, 3));
        s.Arms = 2;   // the fixture's attack takes 2
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0), new UnitOrder(3, Combat.Attack, 2, 0));
        var refused = Assert.Single(r.Rejected);
        Assert.Equal("An attack needs 2 Arms and the stock has 2, of which 2 go to attacks already ordered", refused.Reason);
        Assert.Equal("", r.State.Units[3].Order);
        Assert.Equal(0, r.State.Arms);
    }

    // A unit that replaces its own attack is not counted twice: the stock for one attack still lets it switch hex.
    [Fact]
    public void ReplacingAnAttackDoesNotCountItTwice()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 3, 0, 5), 2, 0), Enemy(2, 2, 0, 3), Enemy(4, 4, 0, 3));
        s.Arms = 2;
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 4, 0));
        Assert.Empty(r.Rejected);
        Assert.Equal(0, r.State.Arms);
        Assert.Equal(2, r.State.Units[4].Strength);   // the new hex was attacked
    }

    // R-COMBAT-002, R-COMBAT-004: the attack resolves in the turn it is given, in step 2; the player's unit pays the Arms then.
    // Below 2:1 the fixture's row has both sides lose 1.
    [Fact]
    public void AnAttackResolvesInStepTwoAndSpendsArms()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3));
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0));
        Assert.Empty(r.Rejected);
        Assert.Equal((4, 2), (r.State.Units[1].Strength, r.State.Units[2].Strength));
        Assert.Equal(s.Arms - 2, r.State.Arms);
        Assert.Equal((Combat.Attack, "2,0"), (r.State.Units[1].Order, r.State.Units[1].OrderTarget));
    }

    // R-COMBAT-002: several units attack one hex together, their strengths summed, as one engagement: 2 + 2 against 2 is 2:1.
    [Fact]
    public void JointAttacksAddTheirStrengths()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 2), Soldier(3, 1, 1, 2), Enemy(2, 2, 0, 2));
        Assert.Equal(100, Combat.Preview(c, s, 1, 2, 0)!.Value.RatioPercent);
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0), new UnitOrder(3, Combat.Attack, 2, 0));
        Assert.Empty(r.Rejected);
        var e = r.State.Units[2];
        Assert.Equal((1, 2, 2), (e.Strength, r.State.Units[1].Strength, r.State.Units[3].Strength));   // 4 against 2, the 2:1 row: the defenders lose 1, the attackers nothing
        Assert.NotEqual((2, 0), (e.Q, e.R));   // and retreat
        Assert.Equal(s.Arms - 4, r.State.Arms);
    }

    // R-COMBAT-002: the defender's biome and fortify multiply its strength. 9 against 2 fortified (150) on hills (150) is
    // 9 / 4.5, 200 percent; on the plain, unfortified, 450.
    [Fact]
    public void TerrainAndFortifyModifyTheRatio()
    {
        var c = Load((2, 0, "hills"));
        var s = With(c, Soldier(1, 3, 0, 9), Enemy(2, 2, 0, 2));
        Assert.Equal(300, Combat.Preview(c, s, 1, 2, 0)!.Value.RatioPercent);
        s.Units[2] = new Unit(2, "enemy", 2, 0, side: "egypt", strength: 2, order: Orders.Fortify, orderTarget: "2,0");
        Assert.Equal(200, Combat.Preview(c, s, 1, 2, 0)!.Value.RatioPercent);
        Assert.Equal(1, Combat.Preview(c, s, 1, 2, 0)!.Value.Row);
        s.Units[1] = new Unit(1, "soldier", 3, 0, side: "israel", strength: 8);
        Assert.Equal(0, Combat.Preview(c, s, 1, 2, 0)!.Value.Row);   // 177 percent
        var plain = Load();
        Assert.Equal(450, Combat.Preview(plain, With(plain, Soldier(1, 3, 0, 9), Enemy(2, 2, 0, 2)), 1, 2, 0)!.Value.RatioPercent);
        Assert.Null(Combat.Preview(plain, With(plain, Soldier(1, 3, 0, 9)), 1, 2, 0));   // nothing to attack
    }

    // R-COMBAT-002: engagements resolve in ascending attacker id, so the order the units were added in changes nothing, and
    // when the Arms last for one attack only, the lower id attacks and the other is called off.
    [Fact]
    public void AttackersResolveInAscendingId()
    {
        var c = Load();
        var units = new[] { Attacking(Soldier(1, 3, 0, 5), 2, 0), Enemy(2, 2, 0, 3), Attacking(Soldier(3, 4, 1, 5), 4, 0), Enemy(4, 4, 0, 3) };
        var a = With(c, units);
        var b = With(c, units.Reverse().ToArray());
        a.Arms = b.Arms = 2;
        var why = Resolve(c, a);
        Resolve(c, b);
        Assert.Equal(a.Hash(), b.Hash());
        Assert.Equal((4, 2, 5, 3), (a.Units[1].Strength, a.Units[2].Strength, a.Units[3].Strength, a.Units[4].Strength));
        Assert.Equal("", a.Units[3].Order);
        Assert.Contains(why, x => x.Message == "Unit 3's attack on hex 4,0 is called off: it needs 2 Arms and the stock has 0");
    }

    // R-COMBAT-003: a defender the row sends back retreats to the first free neighbour in the fixed order: (3, 0) holds the
    // attacker, so (1, 0)... is town_a's hex, free to an enemy unit (R-MOVE-005 binds the player's units only).
    [Fact]
    public void ALosingDefenderRetreatsOneHex()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 3, 0, 6), 2, 0), Enemy(2, 2, 0, 3));
        var why = Resolve(c, s);
        var e = s.Units[2];
        Assert.Equal((2, 1, 0), (e.Strength, e.Q, e.R));
        Assert.Contains(why, x => x.Message == "Unit 2 retreated to hex 1,0");
        Assert.Equal((3, 0), (s.Units[1].Q, s.Units[1].R));   // the attacker does not advance
        // The attack ends: no enemy unit is on the hex now.
        Assert.Equal(("", ""), (s.Units[1].Order, s.Units[1].OrderTarget));
    }

    // R-COMBAT-003: a defender that must retreat and has no free hex surrenders and leaves the state. (0, 0) is a corner:
    // its two land neighbours hold the attackers.
    [Fact]
    public void ADefenderWithNoHexToRetreatToSurrenders()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 1, 0, 4), 0, 0), Attacking(Soldier(3, 0, 1, 4), 0, 0), Enemy(2, 0, 0, 3));
        var next = s.NextId;
        var why = Resolve(c, s);
        Assert.False(s.Units.ContainsKey(2));
        Assert.Contains(why, x => x.Message == "Unit 2 surrendered with 20 people: it had no hex to retreat to. They are held as prisoners");
        Assert.Equal(next, s.NextId);
    }

    // Review focus 4: a unit at 0 is removed, and its id is never reused.
    [Fact]
    public void ADestroyedUnitLeavesTheState()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 3, 0, 1), 2, 0), Enemy(2, 2, 0, 1));
        var next = s.NextId;
        var why = Resolve(c, s);
        Assert.False(s.Units.ContainsKey(1));
        Assert.False(s.Units.ContainsKey(2));
        Assert.Contains(why, x => x.Message == "Unit 1 was destroyed");
        Assert.Contains(why, x => x.Message == "Unit 2 was destroyed");
        Assert.Equal(next, s.NextId);
    }

    // R-COMBAT-003: losses fall one population unit at a time on the strongest unit of the side, a tie on the lower id.
    [Fact]
    public void LossesFallOnTheStrongestUnit()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 3, 0, 2), 2, 0), Attacking(Soldier(3, 1, 1, 2), 2, 0), Enemy(2, 2, 0, 5), Enemy(4, 2, 0, 5));
        Resolve(c, s);   // 4 against 10: both sides lose 1
        Assert.Equal((1, 2, 4, 5), (s.Units[1].Strength, s.Units[3].Strength, s.Units[2].Strength, s.Units[4].Strength));
    }

    // R-COMBAT-006 (Q-CIV-4, "2+3"): the engagement's Why line gives the people each side lost, in the same words for both.
    [Fact]
    public void TheWhyLineGivesPeopleLostOnBothSides()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 3, 0, 5), 2, 0), Enemy(2, 2, 0, 3));
        var line = Assert.Single(Resolve(c, s), x => x.Message.StartsWith("The attack on hex", StringComparison.Ordinal));
        Assert.Equal("The attack on hex 2,0 (unit 1 against unit 2, strength ratio 166%): the attackers lost 10 people and the defenders lost 10 people", line.Message);
    }

    // R-COMBAT-002: enemy units attack under the same rule (their orders come from PR 10's AI), and pay no Arms (Q-ENEMY-4:
    // no supply). Units of two Arab sides fight together as one side.
    [Fact]
    public void EnemyAttacksResolveTooAndPayNoArms()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 0, 3), Attacking(Enemy(2, 3, 0, 5), 2, 0), Attacking(new Unit(4, "ally", 2, 1, side: "jordan", strength: 1), 2, 0));
        var arms = s.Arms;
        Resolve(c, s);
        Assert.Equal((2, 5, 1), (s.Units[1].Strength, s.Units[2].Strength, s.Units[4].Strength));   // 6 against 3, 2:1: the defender loses 1
        Assert.Equal(arms, s.Arms);
    }

    // R-COMBAT-001: attack is a standing order: it repeats each turn while its hex holds an enemy unit next to the attacker,
    // and ends, with a line, once it does not.
    [Fact]
    public void AnAttackStandsUntilItsHexIsEmpty()
    {
        var c = Load();
        var s = With(c, Soldier(1, 3, 0, 9), Enemy(2, 2, 0, 5));
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0));
        Assert.Equal((8, 4), (r.State.Units[1].Strength, r.State.Units[2].Strength));   // 9 against 5, below 2:1
        Assert.Equal(Combat.Attack, r.State.Units[1].Order);
        r = Step(c, r.State);   // no order: the attack stands, against a unit its AI fortified on turn 1 (R-ENEMY-003)
        Assert.Equal((7, 3, 2, 0), (r.State.Units[1].Strength, r.State.Units[2].Strength, r.State.Units[2].Q, r.State.Units[2].R));   // 8 against 4 x 150%, below 2:1
        Assert.Equal(Combat.Attack, r.State.Units[1].Order);
        r = Step(c, Step(c, r.State).State);   // 7 against 3 x 150%, below 2:1; then 6 against 2 x 150%, 2:1
        var e = r.State.Units[2];
        Assert.Equal((1, 1, 0), (e.Strength, e.Q, e.R));   // lost 1 and retreated
        Assert.Equal(s.Arms - 8, r.State.Arms);
        Assert.Equal("", r.State.Units[1].Order);
        Assert.Contains(r.Why, x => x.Message == "Unit 1's attack on hex 2,0 ends: no enemy unit is there");
    }

    // R-COMBAT-005, R-COMBAT-001 (Q-CIV-2, "Enemy units only, no effect"): enemy units on town_a's hex may be attacked; the
    // town, its people and every other part of the state but the units and the Arms are unchanged.
    [Fact]
    public void ALocalityHexIsAttackedOnlyThroughItsUnits()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 2, 0, 9), 1, 0), Enemy(2, 1, 0, 2));
        var before = s.Clone();
        var why = Resolve(c, s);
        Assert.Contains(why, x => x.Message.StartsWith("The attack on hex 1,0", StringComparison.Ordinal));
        Assert.Equal(before.Cities["town_a"].Canonical(), s.Cities["town_a"].Canonical());
        AssertOnlyCombatantsChanged(before, s);
    }

    /// <summary>R-COMBAT-005: <paramref name="after"/> differs from <paramref name="before"/> only in its units, its Arms, the
    /// RNG and, from a surrender, the counts of prisoners and of the player's soldiers held (R-POW-001, R-POW-005): with those
    /// copied back, the hashes match.</summary>
    internal static void AssertOnlyCombatantsChanged(GameState before, GameState after)
    {
        var copy = before.Clone();
        copy.Units.Clear();
        foreach (var u in after.Units.Values) copy.Units.Add(u.Id, u);
        copy.PrisonersHeld = new SortedDictionary<string, int>(after.PrisonersHeld, StringComparer.Ordinal);
        copy.SoldiersHeld = new SortedDictionary<string, int>(after.SoldiersHeld, StringComparer.Ordinal);
        copy.Arms = after.Arms;
        copy.RngState = after.RngState;
        Assert.Equal(after.Describe(), copy.Describe());
    }

    // R-COMBAT-005 (§4.3 item 2): no combat outcome changes the score: a destroyed and a surrendered enemy unit score nothing.
    [Fact]
    public void KillsNeverScore()
    {
        var c = Load();
        var s = With(c, Attacking(Soldier(1, 1, 0, 9), 0, 0), Attacking(Soldier(3, 0, 1, 9), 0, 0), Enemy(2, 0, 0, 2));
        s.Score = 7;
        Resolve(c, s);
        Assert.False(s.Units.ContainsKey(2));
        Assert.Equal(7, s.Score);
    }

    // R-COMBAT-005, R-ENEMY-001: over the reference war run (the goldens' run to the game's end), steps 1 and 2 run on each
    // turn's end state, with its standing attacks and the enemy's orders, change only the units, the Arms and the RNG. The run
    // has engagements, so the test is not empty: since #1240 the real start has none (Golani's 4 against the Syrian 3 is under
    // the policy's 3:2), so unit 0 starts at its old 5 (ReferencePlayerTests.AtFive).
    [Fact]
    public void CombatTouchesOnlyCombatants()
    {
        var c = Real;
        var s = ReferencePlayerTests.AtFive(Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical));
        int engagements = 0;
        for (int turn = 0; turn < 1000 && s.Date < c.End && s.GameOver == null; turn++)
        {
            s = Simulation.Step(c, s, GoldenRun.Orders(c, s)).State;
            var after = s.Clone();
            EnemyAI.Order(c, after, new List<Cause>());
            engagements += Resolve(c, after).Count(x => x.Message.StartsWith("The attack on hex", StringComparison.Ordinal));
            AssertOnlyCombatantsChanged(s, after);
        }
        Assert.True(engagements > 0, "the reference run has no engagement");
    }

    // R-COMBAT-002, R-COMBAT-003: every cell of the real table, on the real map: an attacker and a defender on two
    // neighbouring hexes whose biome counts 100, strengths that land in the row, and the RNG seeded to the face.
    public static IEnumerable<object[]> Cells()
    {
        var table = Real.Rules.Mvp!.CombatTable;
        for (int row = 0; row < table.Count; row++)
            for (int face = 0; face < table[row].AttackerLoss.Count; face++)
                yield return new object[] { row, face };
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void TheTableIsApplied(int row, int face)
    {
        var c = Real;
        var mvp = c.Rules.Mvp!;
        var cell = mvp.CombatTable[row];
        var faces = cell.AttackerLoss.Count;
        var plain = new HashSet<(int, int)>(c.Hexes.Where(h => h.Land && mvp.CombatTerrainPercent[h.Biome] == 100).Select(h => (h.Q, h.R)));
        var (q, r) = plain.OrderBy(h => h).First(h => plain.Contains((h.Item1 + 1, h.Item2)) && c.SettlementHexes.All(x => (x.Q, x.R) != h));
        // Strengths of 5 or more, above any loss in the table, so no unit is emptied: the ratio is the row's own min, or a
        // third for the first row.
        var (att, def) = cell.MinRatioPercent == 0 ? (10, 30) : (cell.MinRatioPercent / 10, 10);
        var s = With(c);
        s.Units.Clear();
        s.Units.Add(1, Attacking(new Unit(1, "veteran_soldier", q + 1, r, side: "israel", strength: att), q, r));
        s.Units.Add(2, new Unit(2, "egyptian_army", q, r, side: "egypt", strength: def));
        Assert.Equal(row, Combat.Preview(c, s, 1, q, r)!.Value.Row);
        ulong seed = 0;
        while ((int)(new Rng(seed).NextDouble() * faces) != face) seed++;
        s.RngState = seed;
        Resolve(c, s);
        Assert.Equal(att - cell.AttackerLoss[face], s.Units[1].Strength);
        var e = s.Units[2];
        Assert.Equal(def - cell.DefenderLoss[face], e.Strength);
        Assert.Equal(cell.DefenderRetreats[face] == 1, (e.Q, e.R) != (q, r));
    }

    // R-COMBAT-002, R-COMBAT-003: the loader refuses a combat value that is missing or out of range.
    [Theory]
    [InlineData("mvp.arms_per_attack", "arms_per_attack: 2", "arms_per_attack: -1", "must be >= 0")]
    [InlineData("mvp.combat_terrain_percent", "{ coastal_plain: 100, hills: 150, desert: 100 }", "{ coastal_plain: 100, desert: 100 }", "no percent for land biome 'hills'")]
    [InlineData("mvp.combat_terrain_percent.hills", "{ coastal_plain: 100, hills: 150, desert: 100 }", "{ coastal_plain: 100, hills: 0, desert: 100 }", "must be >= 1")]
    [InlineData("mvp.combat_table[0].min_ratio_percent", "- { min_ratio_percent: 0,", "- { min_ratio_percent: 10,", "the first row must start at 0")]
    [InlineData("mvp.combat_table[1].min_ratio_percent", "- { min_ratio_percent: 200,", "- { min_ratio_percent: 0,", "must be above the row before")]
    [InlineData("mvp.combat_table[1]", "attacker_loss: [0, 0]", "attacker_loss: [0]", "one entry per face")]
    [InlineData("mvp.combat_table[1]", "defender_loss: [1, 1], defender_retreats: [1, 1]", "defender_loss: [-1, 1], defender_retreats: [1, 1]", "a loss must be >= 0")]
    [InlineData("mvp.combat_table[1].defender_retreats", "defender_retreats: [1, 1]", "defender_retreats: [2, 1]", "must be 0 or 1")]
    public void TheLoaderRefusesABadCombatValue(string field, string from, string to, string message)
    {
        var files = Fixture.Gdd(new Dictionary<string, string> { ["unit_types.yaml"] = Types });
        Assert.Contains(from, files["rules.yaml"]);
        files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(("rules.yaml", field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }

    // A save and a scenario carry the attack's hex; fortify and hold carry none.
    [Fact]
    public void AnAttackOrderRoundTripsThroughASave()
    {
        var c = Load();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new UnitOrder(1, Combat.Attack, 2, 0) });   // refused (unit 1 does not exist), and recorded
        g.Step(new Command[] { new UnitOrder(1, Orders.Hold) });
        var text = g.Save.Write();
        Assert.Contains("order 1 attack 2 0\n", text);
        Assert.Contains("order 1 hold\n", text);
        var back = GameSession.Load(c, text, out _).Save.Turns;
        var attack = (UnitOrder)back[0][0];
        Assert.Equal((1, Combat.Attack, (int?)2, (int?)0), (attack.Unit, attack.Order, attack.Q, attack.R));
        var hold = (UnitOrder)back[1][0];
        Assert.Equal(((int?)null, (int?)null), (hold.Q, hold.R));
    }
}
