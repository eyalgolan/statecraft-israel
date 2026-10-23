using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>R-COMBAT-002 (docs/determinism.md, "Combat draws"): each engagement draws exactly Combat.DrawsPerEngagement
/// values from the one RNG, whatever its outcome, and a step 2 with no engagement draws none. Joint attackers on one hex are
/// one engagement.</summary>
public class CombatDrawCountTests
{
    static Content Load() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Fixture.GddUnitTypes
            + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
            + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n",
        ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
    })));

    static Unit Soldier(int id, int q, int r, int strength, string target) =>
        new Unit(id, "soldier", q, r, side: "israel", strength: strength, order: Combat.Attack, orderTarget: target);
    static Unit Enemy(int id, int q, int r, int strength) => new Unit(id, "enemy", q, r, side: "egypt", strength: strength);

    static ulong After(ulong state, int draws)
    {
        var rng = new Rng(state);
        for (int i = 0; i < draws; i++) rng.NextULong();
        return rng.State;
    }

    [Fact]
    public void EachEngagementDrawsExactlyN()
    {
        var c = Load();
        // Three engagements: a joint attack of units 1 and 3 on (2, 0); unit 5 on (4, 0), which destroys both sides; unit 7
        // on (5, 1), which sends its defender back. Every outcome draws the same.
        var s = CombatTests.With(c, Soldier(1, 3, 0, 4, "2,0"), Enemy(2, 2, 0, 3), Soldier(3, 2, 1, 1, "2,0"),
            Soldier(5, 4, 1, 1, "4,0"), Enemy(6, 4, 0, 1), Soldier(7, 5, 2, 9, "5,1"), Enemy(8, 5, 1, 2));
        var before = s.RngState;
        Combat.Resolve(c, s, new List<Cause>());
        Assert.Equal(1, Combat.DrawsPerEngagement);
        Assert.Equal(After(before, 3 * Combat.DrawsPerEngagement), s.RngState);
    }

    [Fact]
    public void AStepTwoWithNoEngagementDrawsNothing()
    {
        var c = Load();
        // Standing attacks whose hexes are empty are not engagements.
        var s = CombatTests.With(c, Soldier(1, 3, 0, 4, "2,0"), Soldier(3, 0, 2, 4, "1,2"));
        var before = s.RngState;
        Combat.Resolve(c, s, new List<Cause>());
        Assert.Equal(before, s.RngState);
        Assert.Equal(("", ""), (s.Units[1].Order, s.Units[3].Order));
    }
}
