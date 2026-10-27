using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The military unit record and the military unit types (GDD Phase 5 PR 2, #741). No behaviour: the state can hold
/// a unit of any side, and the content can describe the Israeli military types. Movement points, strength and upkeep per
/// type, and the enemy types, come in later PRs (owner, 18 October 2026, #741: "all defaults").</summary>
public class MilitaryUnitTests
{
    static Content Load(string types) =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["unit_types.yaml"] = types })));

    // R-UNIT-002: a halutz unit's hash text is the one main wrote before the military fields existed.
    [Fact]
    public void AHalutzHashesAsBefore()
    {
        Assert.Equal("1:halutz@0,0", new Unit(1, "halutz", 0, 0).Canonical());
        Assert.Equal("1:veteran_halutz@0,0/6", new Unit(1, "veteran_halutz", 0, 0, 6).Canonical());
        var s = new GameState();
        s.Units.Add(1, new Unit(1, "halutz", -3, 19));
        Assert.EndsWith("\nunits[\n  1:halutz@-3,19", s.Describe());
    }

    // R-UNIT-001, R-UNIT-002: a unit with a side, a strength and a standing order writes each of them.
    [Fact]
    public void AMilitaryUnitWritesItsFields()
    {
        Assert.Equal("2:veteran_soldier@3,4|side=israel|strength=5|order=hold|target=7",
                     new Unit(2, "veteran_soldier", 3, 4, side: "israel", strength: 5, order: "hold", orderTarget: "7").Canonical());
        Assert.Equal("2:artillery@3,4/1|side=israel", new Unit(2, "artillery", 3, 4, 1, side: "israel").Canonical());
    }

    // R-HOUSE-003, R-UNIT-002: home affects housing, so it is state and survives the immutable clone.
    [Fact]
    public void AFormationsHomeCityChangesItsHashAndSurvivesAClone()
    {
        var s = new GameState();
        s.Units.Add(1, new Unit(1, "recruit", 0, 0, side: "israel", strength: 3, homeCity: "tel_aviv"));
        var copy = s.Clone();
        Assert.Equal("tel_aviv", copy.Units[1].HomeCity);
        Assert.Equal(s.Hash(), copy.Hash());
        copy.Units[1] = new Unit(1, "recruit", 0, 0, side: "israel", strength: 3, homeCity: "jerusalem");
        Assert.NotEqual(s.Hash(), copy.Hash());
        Assert.Equal("tel_aviv", s.Units[1].HomeCity);
    }

    // R-UNIT-001: the recruit now joins the three military types introduced by #741, with its side and class.
    [Fact]
    public void TheRealGameHasTheFourIsraeliMilitaryTypes()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(new[] { ("recruit", "unit", "israel", "infantry"), ("veteran_soldier", "unit", "israel", "infantry"), ("artillery", "unit", "israel", "artillery"), ("armoured_car", "unit", "israel", "armour") },
                     c.UnitTypes.Where(t => t.Side == "israel").Select(t => (t.Id, t.Kind, t.Side, t.Class)));
        Assert.Equal("israel", c.UnitTypes.Single(t => t.Id == "recruit").Side); // R-TRAIN-003: now a formation.
    }

    // R-UNIT-001: a military type needs both a side and a class; a type with neither still loads.
    [Theory]
    [InlineData("  - { id: soldier, kind: unit, side: israel }\n", "types[soldier].class", "required for a military type")]
    [InlineData("  - { id: soldier, kind: unit, class: infantry }\n", "types[soldier].side", "required for a military type")]
    [InlineData("  - { id: soldier, kind: unit, side: atlantis, class: infantry }\n", "types[soldier].side", "unknown side 'atlantis'")]
    [InlineData("  - { id: soldier, kind: unit, side: israel, class: cavalry }\n", "types[soldier].class", "unknown class 'cavalry' (infantry, artillery or armour)")]
    [InlineData("  - { id: soldier, kind: population, side: israel, class: infantry }\n", "types[soldier].kind", "a military type is a unit")]
    public void AMilitaryTypeNeedsASideAndAClass(string row, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Fixture.GddUnitTypes + row));
        Assert.Equal("unit_types.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-UNIT-001
    [Fact]
    public void AMilitaryTypeLoadsAndAPopulationTypeNeedsNeither()
    {
        var c = Load(Fixture.GddUnitTypes + "  - { id: soldier, kind: unit, side: egypt, class: armour, movement_points: 4 }\n");
        var t = c.UnitTypes.Single(x => x.Id == "soldier");
        Assert.Equal(("egypt", "armour"), (t.Side, t.Class));
        Assert.Equal(("", ""), (c.UnitTypes.Single(x => x.Id == "civilian").Side, c.UnitTypes.Single(x => x.Id == "civilian").Class));
    }
}
