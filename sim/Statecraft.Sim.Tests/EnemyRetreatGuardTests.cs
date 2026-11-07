using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>#2932 (owner, 7 November 2026: "Fix now (Recommended)"): a combat retreat is held to the same locality guard as a
/// move, on both sides. Before it, an enemy retreat could end on an empty Israeli city, which ended a gdd-0.1 game at once
/// (R-END-003) and held the city under gdd-0.3 (R-CITY-010). The fixture tests use Gdd03MovementFortTests' board, with town_a
/// a city on (1, 0), and a table whose faces agree: from 2:1 the defenders lose 1 and retreat. Each runs under gdd-0.1 and
/// gdd-0.3. The fixture's places are fixture values, not history.</summary>
public class EnemyRetreatGuardTests
{
    static Content Load(bool gdd03) => Gdd03MovementFortTests.Load(gdd03 ? SaveGame.Gdd03Ruleset : SaveGame.GddRuleset);

    static Unit Ours(int id, int q, int r, int strength) => new Unit(id, "recruit", q, r, side: Movement.PlayerSide, strength: strength);
    static Unit Theirs(int id, int q, int r, int strength) => new Unit(id, "enemy", q, r, side: "egypt", strength: strength);

    static StepResult Attack(Content c, GameState s, int unit, int q, int r)
    {
        var result = Simulation.Step(c, s, new Command[] { new UnitOrder(unit, Combat.Attack, q, r) });
        Assert.Empty(result.Rejected);
        return result;
    }

    // R-COMBAT-003, R-ENEMY-001, R-END-003, R-CITY-010: (1, 1) holds the attacker and (1, 0), the next neighbour in the fixed
    // order, is a city, so the defender goes to the one after it, (0, 0). No defeat under gdd-0.1, no held city under gdd-0.3.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnEnemyRetreatPassesOverACityToTheNextFreeHex(bool gdd03)
    {
        var c = Load(gdd03);
        var after = Attack(c, CombatTests.With(c, Ours(1, 1, 1, 6), Theirs(2, 0, 1, 2)), 1, 0, 1);
        Assert.Contains(after.Why, w => w.Key == WhyKeys.Retreated && w.Args[0] == "0,0");
        Assert.Equal((0, 0), (after.State.Units[2].Q, after.State.Units[2].R));
        Assert.Null(after.State.GameOver);
        Assert.DoesNotContain(after.Why, w => w.Message.Contains("held by the enemy"));
    }

    // R-COMBAT-003, R-POW-001: (0, 0) has two land neighbours, the city (1, 0) and the attacker's (0, 1), so a defender sent
    // back has no legal hex and takes the existing surrender path: it leaves the state, its people are prisoners, no id is used.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnEnemyWhoseOnlyFreeHexIsACitySurrenders(bool gdd03)
    {
        var c = Load(gdd03);
        var s = CombatTests.With(c, Ours(1, 0, 1, 6), Theirs(2, 0, 0, 2));
        var after = Attack(c, s, 1, 0, 0);
        Assert.False(after.State.Units.ContainsKey(2));
        Assert.Contains(after.Why, w => w.Key == WhyKeys.SurrenderedPrisoners);
        Assert.Equal(1, after.State.PrisonersHeld["egypt"]);
        Assert.Equal(s.NextId, after.State.NextId);
        Assert.Null(after.State.GameOver);
    }

    // R-COMBAT-003: the guard is the enemy's: a player's unit may still fall back onto its own city, as before #2932.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APlayersRetreatStillEndsOnItsOwnCity(bool gdd03)
    {
        var c = Load(gdd03);
        var attacker = new Unit(2, "enemy", 1, 1, side: "egypt", strength: 6, order: Combat.Attack, orderTarget: "0,1");
        var s = CombatTests.With(c, Ours(1, 0, 1, 2), attacker);
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        Assert.Contains(why, w => w.Key == WhyKeys.Retreated && w.Args[0] == "1,0");
        Assert.Equal((1, 0), (s.Units[1].Q, s.Units[1].R));
    }

    // R-COMBAT-003, R-MOVE-005: a player's retreat still skips an enemy-held place, and the guard is NoEntry's, not "any
    // locality": an enemy's retreat may end on one its own side holds.
    [Fact]
    public void EachSideFallsBackOnlyOntoAPlaceItMayEnter()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);   // town_a, on (1, 0), is enemy-held
        var ours = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: Movement.PlayerSide, strength: 2),
            new Unit(2, "enemy", 1, 1, side: "egypt", strength: 6, order: Combat.Attack, orderTarget: "0,1"));
        Combat.Resolve(c, ours, new List<Cause>());
        Assert.Equal((0, 0), (ours.Units[1].Q, ours.Units[1].R));

        var theirs = CombatTests.With(c, new Unit(1, "soldier", 1, 1, side: Movement.PlayerSide, strength: 6), Theirs(2, 0, 1, 2));
        var after = Attack(c, theirs, 1, 0, 1);
        Assert.Equal((1, 0), (after.State.Units[2].Q, after.State.Units[2].R));
    }

    // R-COMBAT-003, R-CTRL-016: an annexed target is Israeli for good, so it closes to an enemy retreat as to an enemy move.
    [Fact]
    public void AnEnemyRetreatPassesOverAnAnnexedTarget()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, new Unit(1, "soldier", 1, 1, side: Movement.PlayerSide, strength: 6), Theirs(2, 0, 1, 2));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.Annexed, s.Date);
        var after = Attack(c, s, 1, 0, 1);
        Assert.Equal((0, 0), (after.State.Units[2].Q, after.State.Units[2].R));
    }

    // R-COMBAT-003, R-END-003, R-CITY-010: the issue's case on the shipped content, a real NewGame and a real Step: an Egyptian
    // unit on (10, 10) beside empty Tiberias, its first neighbour, and an Israeli attacker on (11, 9). RngState 23 draws a
    // retreat face. Before #2932 the unit retreated onto Tiberias and gdd-0.1 ended in defeat_war. The placement is by hand.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnEnemyRetreatNeverEndsOnEmptyTiberias(bool gdd03)
    {
        var c = Gdd03HousingConditionTests.Load(gdd03);
        Assert.Equal(gdd03 ? SaveGame.Gdd03Ruleset : SaveGame.GddRuleset, c.Ruleset);
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var tiberias = c.SettlementHexes.Single(h => h.Id == "tiberias");
        Assert.Equal((11, 10), (tiberias.Q, tiberias.R));
        Assert.DoesNotContain(s.Units.Values, u => (u.Q, u.R) == (11, 10) || (u.Q, u.R) == (10, 10) || (u.Q, u.R) == (11, 9));
        var egypt = s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide);
        var brigade = s.Units.Values.First(u => u.Side == Movement.PlayerSide && c.UnitTypes.First(t => t.Id == u.Type).Class != "artillery");
        int enemy = s.NextId, mine = s.NextId + 1;
        s.Units.Add(enemy, new Unit(enemy, egypt.Type, 10, 10, side: egypt.Side, strength: 10));
        s.Units.Add(mine, new Unit(mine, brigade.Type, 11, 9, side: Movement.PlayerSide, strength: 20));
        s.NextId = mine + 1;
        s.RngState = 23;

        var after = Attack(c, s, mine, 10, 10);

        var retreat = after.Why.Single(w => w.Key == WhyKeys.Retreated && w.Units[0].Id == enemy);   // the face did send it back
        Assert.NotEqual("11,10", retreat.Args[0]);
        Assert.NotEqual((11, 10), (after.State.Units[enemy].Q, after.State.Units[enemy].R));
        Assert.Null(after.State.GameOver);
    }

    // R-COMBAT-003, R-END-003: on the shipped content the guard closes every GDD city's hex, so no enemy retreat can trigger
    // the war loss.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryCityHexIsClosedToAnEnemyRetreat(bool gdd03)
    {
        var c = Gdd03HousingConditionTests.Load(gdd03);
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var closed = Movement.NoEntry(c, s, enemy: true);
        Assert.NotEmpty(c.Cities);
        Assert.All(c.Cities, city =>
        {
            var hex = c.SettlementHexes.Single(h => h.Id == city.Id);
            Assert.Contains((hex.Q, hex.R), closed);
        });
    }
}
