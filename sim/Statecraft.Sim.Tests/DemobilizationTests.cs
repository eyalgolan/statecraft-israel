using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>R-UNIT-005 (GDD Phase 7, #1309): the player's "Send home" order, allowed after the armistice on the unit's
/// front (owner, 22 October 2026, #1309); the unit leaves the map and its strength joins the nearest city's civilians
/// without a job, counting in its jobs and housing. The front is the front of the enemy side the unit attacks or borders
/// (ties: the nearest enemy unit, then the lowest unit id); a side that belongs to no front names none (Q-OBJ-1), and a
/// unit that attacks and borders no front-bearing enemy may go home once any armistice is signed.</summary>
public class DemobilizationTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n"
        + "  - { id: syrian, kind: unit, side: syria, class: infantry, movement_points: 3 }\n"
        + "  - { id: arab, kind: unit, side: ala, class: infantry, movement_points: 3 }\n"
        + "  - { id: gun, kind: unit, side: israel, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3, combat_percent: 200 }\n";

    static Content Load(Dictionary<string, string>? extra = null)
    {
        var files = new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
        };
        if (extra != null) foreach (var kv in extra) files[kv.Key] = kv.Value;
        return ContentLoader.Load(Fixture.Write(Fixture.Gdd(files)));
    }

    /// <summary>Two cities (ArrivalsTests' pattern): town_a on (1, 0), town_b on (2, 1), one civilian each.</summary>
    static readonly Dictionary<string, string> TwoCities = new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
        ["map/locations_1948.yaml"] = Fixture.Files["map/locations_1948.yaml"]
            + "  - id: town_b\n    name: Town B\n    name_he: עיר ב\n    name_he_sources: [{ url: 'https://example.org/town-b-he', checked: 2026-10-18 }]\n    kind: city\n    lon: 34.42\n    lat: 33.28\n    q: 2\n    r: 1\n    priority: must_have\n    marker: true\n    managed_city: true\n    designated_site: false\n    settlement_control_target: false\n    sources: [{ url: 'https://example.org/town-b', checked: 2026-10-18 }]\n    status: draft\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 40, 1, 1, 0) + Fixture.City("town_b", 40, 1, 1, 0),
    };

    static GameState State(Content c, params Unit[] units)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        foreach (var u in units) s.Units.Add(u.Id, u);
        s.NextId = units.Length == 0 ? s.NextId : Math.Max(s.NextId, units.Max(u => u.Id) + 1);
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] cmds) => Simulation.Step(c, s, cmds);

    static Unit Soldier(int id, int q, int r, string order = "", string target = "", string homeCity = "") =>
        new Unit(id, "soldier", q, r, side: "israel", strength: 5, order: order, orderTarget: target, homeCity: homeCity);

    // R-UNIT-005: before the armistice on the unit's front the order is refused, the state as a turn without the order
    // leaves it; another front's signature does not open it.
    [Fact]
    public void TheFrontsArmisticeMustBeSigned()
    {
        var c = Load();
        var s = State(c, Soldier(1, 3, 0), new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3));
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, s, 1));
        var refused = Step(c, s, new UnitOrder(1, Demobilization.SendHome));
        Assert.Equal("The armistice with Egypt is not signed", Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), refused.State.Hash());
        refused.State.ChosenOptions["armistice_syria"] = "sign";   // another front's signature does not open it
        var again = Step(c, refused.State, new UnitOrder(1, Demobilization.SendHome));
        Assert.Equal("The armistice with Egypt is not signed", Assert.Single(again.Rejected).Reason);
    }

    // R-UNIT-005: the order is the only way home (owner, 22 October 2026, #1309). After the front's armistice is
    // signed, a turn with no Send home order sends nothing: the unit and the city counts (civilians, employed,
    // refugees and housing) are exactly those of the same turn before the signature - no automatic demobilisation.
    [Fact]
    public void NothingGoesHomeWithoutTheOrder()
    {
        var c = Load(TwoCities);
        GameState Fresh() => State(c, Soldier(1, 3, 0, homeCity: "town_a"), new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3));
        var unsigned = Step(c, Fresh());
        var signed = Fresh();
        signed.ChosenOptions["armistice_egypt"] = "sign";
        var after = Step(c, signed);
        Assert.DoesNotContain(after.Why, w => w.Message.Contains("sent home"));
        Assert.Equal(unsigned.State.Units.Count, after.State.Units.Count);
        Assert.Equal((unsigned.State.Units[1].Strength, unsigned.State.Units[1].Q, unsigned.State.Units[1].R),
            (after.State.Units[1].Strength, after.State.Units[1].Q, after.State.Units[1].R));
        foreach (var id in after.State.Cities.Keys)
        {
            Assert.Equal(unsigned.State.Cities[id].Civilians, after.State.Cities[id].Civilians);
            Assert.Equal(unsigned.State.Cities[id].Employed, after.State.Cities[id].Employed);
            Assert.Equal(unsigned.State.Cities[id].Refugees, after.State.Cities[id].Refugees);
            // housing too (#1309's "done when"): the unit's home town_a keeps its share while no order runs
            Assert.Equal(Cities.Housed(unsigned.State, unsigned.State.Cities[id]), Cities.Housed(after.State, after.State.Cities[id]));
        }
    }

    // R-UNIT-005: the standing refusals are the orders' own (R-ORDER-001's wording).
    [Theory]
    [InlineData(9, "There is no such unit")]
    [InlineData(2, "That unit is not yours")]
    [InlineData(3, "Only a military unit takes that order")]
    public void TheRefusals(int unit, string reason)
    {
        var c = Load();
        var s = State(c, Soldier(1, 3, 0), new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3), new Unit(3, "halutz", 3, 1));
        Assert.Equal(reason, Demobilization.Check(c, s, unit));
    }

    // R-UNIT-005: like fortify and hold, the order names no hex.
    [Fact]
    public void SendHomeNamesNoHex()
    {
        var c = Load();
        var s = State(c, Soldier(1, 3, 0));
        s.ChosenOptions["armistice_egypt"] = "sign";
        Assert.Equal("That order names no hex", Assert.Single(Step(c, s, new UnitOrder(1, Demobilization.SendHome, 2, 0)).Rejected).Reason);
    }

    // R-UNIT-005: after the front's armistice the unit leaves the map and its strength joins the nearest city's
    // civilians, counting in its jobs (R-CITY-009 fills town_b's one slot the same turn) and its housing: the unit's
    // home is town_a, so its people move out of town_a's housing (R-HOUSE-003) and into town_b's, never counted twice
    // or lost. The move is deterministic.
    [Fact]
    public void SentHomeAfterTheArmistice()
    {
        var c = Load(TwoCities);
        GameState Fresh()
        {
            var s = State(c, Soldier(1, 3, 0, homeCity: "town_a"), new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3));
            s.ChosenOptions["armistice_egypt"] = "sign";
            return s;
        }
        var before = Fresh();
        Assert.Equal(6, Cities.Housed(before, before.Cities["town_a"]));   // the fixture's 1 civilian plus the unit's 5
        Assert.Equal(1, Cities.Housed(before, before.Cities["town_b"]));
        var done = Step(c, Fresh(), new UnitOrder(1, Demobilization.SendHome));
        Assert.Empty(done.Rejected);
        Assert.DoesNotContain(done.State.Units.Keys, id => id == 1);
        var b = done.State.Cities["town_b"];   // (2, 1) is 1 hex from (3, 0); town_a on (1, 0) is 2
        Assert.Equal(6, b.Civilians);          // the fixture's 1 plus the unit's 5
        Assert.Equal(1, b.Employed);           // the one job slot fills in the same turn
        Assert.Equal(1, done.State.Cities["town_a"].Civilians);
        Assert.Equal(1, Cities.Housed(done.State, done.State.Cities["town_a"]));   // out of the home city's housing
        Assert.Equal(6, Cities.Housed(done.State, done.State.Cities["town_b"]));   // into the nearest city's
        Assert.Contains(done.Why, w => w.Message == "Unit 1 is sent home: its 50 people are civilians in Town B now");
        Assert.Equal(done.State.Hash(), Step(c, Fresh(), new UnitOrder(1, Demobilization.SendHome)).State.Hash());
    }

    // R-UNIT-005: a unit that attacks and borders no front-bearing enemy waits for any armistice (the ruling's "no
    // enemy units in play"); an ALA unit, belonging to no front (Q-OBJ-1), names none.
    [Fact]
    public void NoFrontBearingEnemyWaitsForAnyArmistice()
    {
        var c = Load();
        var alone = State(c, Soldier(1, 3, 0));
        Assert.Equal("No armistice is signed", Demobilization.Check(c, alone, 1));
        alone.ChosenOptions["armistice_lebanon"] = "sign";
        Assert.Null(Demobilization.Check(c, alone, 1));
        Assert.Empty(Step(c, alone, new UnitOrder(1, Demobilization.SendHome)).State.Units);

        var bordered = State(c, Soldier(1, 3, 0), new Unit(4, "arab", 2, 0, side: "ala", strength: 3));
        Assert.Equal("No armistice is signed", Demobilization.Check(c, bordered, 1));
        bordered.ChosenOptions["armistice_jordan"] = "sign";
        Assert.Null(Demobilization.Check(c, bordered, 1));
    }

    // R-UNIT-005: the attack-target clause of Front() matters on its own. A gun attacks beyond the next hex
    // (Combat.InReach), so an enemy two hexes away that nothing borders still names the front: drop
    // `|| (attacks != null && ...)` and this becomes the any-armistice case and goes green after the Jordan signature.
    [Fact]
    public void AnAttackTargetTwoHexesAwayNamesItsFront()
    {
        var c = Load();
        var s = State(c, new Unit(1, "gun", 3, 0, side: "israel", strength: 2, order: Combat.Attack, orderTarget: "1,0"),
            new Unit(2, "enemy", 1, 0, side: "egypt", strength: 3));
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, s, 1));
        s.ChosenOptions["armistice_jordan"] = "sign";   // would open the order if the target did not count
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, s, 1));
    }

    // R-UNIT-005: the front is the nearest enemy unit's, ties by the lowest unit id; an attack order's target counts.
    [Fact]
    public void TheFrontIsTheNearestEnemysThenTheLowestIds()
    {
        var c = Load();
        var s = State(c, Soldier(1, 3, 0),
            new Unit(9, "syrian", 4, 0, side: "syria", strength: 3), new Unit(7, "enemy", 2, 0, side: "egypt", strength: 3));
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, s, 1));   // both border: unit 7 wins
        s.Units.Remove(7);
        Assert.Equal("The armistice with Syria is not signed", Demobilization.Check(c, s, 1));
        s.ChosenOptions["armistice_syria"] = "sign";
        Assert.Null(Demobilization.Check(c, s, 1));

        var attacking = State(c, Soldier(1, 3, 0, Combat.Attack, "2,0"), new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3));
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, attacking, 1));
    }
}
