using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>Spec 5's one sim helper for the advisors: the cities the enemy will likely attack or enter this turn, from
/// resolver step 1 run on a copy (R-ENEMY-001: the orders come from the state alone). Read-only.</summary>
public class ThreatsTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    static Content Load() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
    })));

    static Unit Soldier(int id, int q, int r, int strength) => new Unit(id, "soldier", q, r, side: "israel", strength: strength);
    static Unit Enemy(int id, int q, int r, int strength) => new Unit(id, "enemy", q, r, side: "egypt", strength: strength);

    // R-ENEMY-001, R-ENEMY-003: the helper names town_a (the fixture's city, on 1,0) exactly when the turn's own step 1
    // orders an attack on its hex, and it changes nothing.
    [Theory]
    [InlineData(20, 1, true)]
    [InlineData(1, 20, false)]
    public void NamesTheCityTheEnemyAttacksAndChangesNothing(int enemy, int defender, bool atRisk)
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 1, 0, defender), Enemy(2, 2, 0, enemy));
        var hash = s.Hash();
        var risk = Threats.CitiesAtRisk(c, s);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(atRisk ? new[] { "town_a" } : Array.Empty<string>(), risk);
        var turn = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Equal(atRisk, turn.Why.Any(w => w.Key == WhyKeys.AttackOrdered && w.Message.EndsWith("hex 1,0", StringComparison.Ordinal)));
    }

    [Fact]
    public void NothingOnceTheGameIsOver()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 1, 0, 1), Enemy(2, 2, 0, 20));
        s.GameOver = GameOverCodes.DefeatWar;
        Assert.Empty(Threats.CitiesAtRisk(c, s));
    }

    // The turn's end is Step's own: a turn run after TurnEnd is called ends on that date.
    [Fact]
    public void TurnEndIsTheDateTheTurnEndsOn()
    {
        var c = Load();
        var s = CombatTests.With(c);
        var end = Simulation.TurnEnd(c, s);
        Assert.Equal(end, Simulation.Step(c, s, Array.Empty<Command>()).State.Date);
    }
}
