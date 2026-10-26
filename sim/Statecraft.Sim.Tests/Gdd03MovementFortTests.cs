using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1708 bounded slice. Synthetic units/forts are fixture data, not a production roster.</summary>
public class Gdd03MovementFortTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: car, kind: unit, side: israel, class: armour, movement_points: 5 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy_car, kind: unit, side: egypt, class: armour, movement_points: 5 }\n"
        + "  - { id: gun, kind: unit, side: israel, class: artillery, movement_points: 2, range: 2, arms_per_fire: 2 }\n";

    static Dictionary<string, string> Files(string identity = SaveGame.Gdd03Ruleset, string biome = "coastal_plain")
    {
        var f = Fixture.Gdd(new Dictionary<string, string> {
            ["unit_types.yaml"] = Types,
            ["map/hexes.json"] = MovementTests.Board((3, 1, biome)),
            ["corridors.yaml"] = "corridors: []\n"
        });
        f["rules.yaml"] = f["rules.yaml"].Replace("move_cost: { coastal_plain: 1, hills: 2, desert: 3 }",
            "move_cost: { coastal_plain: 1, lowland: 1, valley: 1, hills: 2, mountains: 3, desert: 3 }")
            .Replace("combat_terrain_percent: { coastal_plain: 100, hills: 150, desert: 100 }",
            "combat_terrain_percent: { coastal_plain: 100, lowland: 100, valley: 100, hills: 150, mountains: 150, desert: 100 }");
        f["eras.yaml"] = f["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + identity);
        f[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        return f;
    }

    static Content Load(string identity = SaveGame.Gdd03Ruleset, string biome = "coastal_plain") =>
        ContentLoader.Load(Fixture.Write(Files(identity, biome)));

    static Unit Of(string type, int id = 1, int q = 2, int r = 1, string side = "israel", string order = "", string target = "") =>
        new Unit(id, type, q, r, side: side, strength: 6, order: order, orderTarget: target);

    static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);

    // R-MOVE-001, R-SAVE-001: exact identity gate, even with a catalogue present.
    [Fact]
    public void LegacyStillReadsItsAuthoredAllowanceAndHasNoFortBonus()
    {
        var c = ContentLoader.Load(Fixture.Write(new() { [Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(),Gdd03BalanceCatalogue.FileName)) }));
        c.UnitTypes.Add(new UnitType { Id = "car", Side = "israel", Class = "armour", MovementPoints = 5 });
        c.MapLocations.Add(new MapLocation { Id = "fixture", Kind = "fort", Q = 3, R = 1 });
        Assert.Equal(SaveGame.LegacyRuleset, c.Ruleset);
        Assert.Equal(5, Movement.Allowance(c, Of("car")));
        Assert.False(c.HasPermanentFort(3, 1));
    }

    // R-MOVE-001: class, not id or army, chooses the allowance; older identities keep their authored values.
    [Theory]
    [InlineData("recruit", "israel", 2)]
    [InlineData("enemy", "egypt", 2)]
    [InlineData("car", "israel", 3)]
    [InlineData("enemy_car", "egypt", 3)]
    public void BothArmiesUseCatalogueAllowances(string type, string side, int expected)
    {
        var c = Load();
        Assert.Equal(expected, Movement.Allowance(c, Of(type, side: side)));
        var old = Load(SaveGame.GddRuleset);
        Assert.Equal(type.Contains("car") ? 5 : 3, Movement.Allowance(old, Of(type, side: side)));
    }

    // R-MOVE-001, R-MOVE-004: every land column, with water still impassable.
    [Theory]
    [InlineData("coastal_plain", 1, 1)]
    [InlineData("lowland", 1, 1)]
    [InlineData("valley", 1, 1)]
    [InlineData("hills", 2, 2)]
    [InlineData("mountains", 2, 3)]
    [InlineData("desert", 2, 2)]
    public void RoleTerrainCosts(string biome, int foot, int car)
    {
        var c = Load(biome: biome);
        foreach (var pair in new[] { ("recruit", foot), ("car", car) })
        {
            var s = With(c, Of(pair.Item1));
            Assert.Equal(pair.Item2, Movement.Reachable(c, s, 1)[(3, 1)]);
        }
    }

    [Theory]
    [InlineData("sea")]
    [InlineData("lake")]
    public void WaterIsNeverReachable(string biome)
    {
        var c = Load(biome: biome); var s = With(c, Of("car"));
        Assert.DoesNotContain((3, 1), Movement.Reachable(c, s, 1).Keys); // R-MOVE-004
    }

    // R-MOVE-001, R-MOVE-006: accumulate immediately, reset only on the next turn.
    [Fact]
    public void ImmediateMovesSpendTheNewAllowance()
    {
        var c = Load(); var s = With(c, Of("recruit"));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 3, 1), new Move(1, 4, 1), new Move(1, 5, 1) });
        Assert.Equal((4, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Single(now.Rejected);
        Assert.Equal(2, now.Spent[1]);
        Assert.Equal(0, Movement.PointsLeft(c, now.State, 1, now.Spent));
        var next = Simulation.Step(c, s, new Command[] { new Move(1, 3, 1), new Move(1, 4, 1) });
        Assert.Equal((4, 1), (next.State.Units[1].Q, next.State.Units[1].R));
        Assert.Equal(2, Movement.PointsLeft(c, next.State, 1, new Dictionary<int, int>()));
    }

    // R-MOVE-001, R-MOVE-008, R-MOVE-009: synthetic expensive terrain tests the full-points exception.
    [Fact]
    public void ExpensiveFirstStepIsOnlyAllowedAtFullPointsAndEtaAgrees()
    {
        var f = Files();
        f["map/hexes.json"] = MovementTests.Board((3, 1, "mountains"), (4, 1, "sea"), (4, 0, "sea"),
            (3, 0, "sea"), (2, 2, "sea"), (3, 2, "sea"));
        var c = ContentLoader.Load(Fixture.Write(f));
        c.Gdd03Balance!.Values["mountains"].Parts["movement"].Values[0] = 9;
        var s = With(c, Of("recruit"));
        Assert.Equal(2, Movement.Reachable(c, s, 1)[(3, 1)]);
        Assert.DoesNotContain((3, 1), Movement.Reachable(c, s, 1, new Dictionary<int, int> { [1] = 1 }).Keys);
        Assert.Equal(1, Movement.Route(c, s, 1, 3, 1, new Dictionary<int, int>()).Last().Turn);
        Assert.Equal(2, Movement.Route(c, s, 1, 3, 1, new Dictionary<int, int> { [1] = 1 }).Last().Turn);
    }

    // R-MOVE-002, R-MOVE-008: deterministic route and initial advancement share the allowance.
    [Fact]
    public void GoToAdvancesAtOnceAndOnlyTheOutstandingRouteContinues()
    {
        var c = Load(); var s = With(c, Of("recruit", q: 0)); var spent = new Dictionary<int, int>(); var why = new List<Cause>();
        var route = Movement.Route(c, s, 1, 5, 1, spent);
        Assert.Equal(new[] { 1, 1, 2, 2, 3 }, route.Select(h => h.Turn));
        Assert.Equal(route, Movement.Route(c, s, 1, 5, 1, spent));
        Assert.Null(Movement.GiveGoTo(c, s, 1, 5, 1, spent, why));
        Assert.Equal(2, s.Units[1].Q);
        Movement.ContinueRoutes(c, s, spent, why); // no second allowance in the same turn
        Assert.Equal(2, s.Units[1].Q);
        spent.Clear(); Movement.ContinueRoutes(c, s, spent, why);
        Assert.Equal(4, s.Units[1].Q);
        spent.Clear(); Movement.ContinueRoutes(c, s, spent, why);
        Assert.Equal(5, s.Units[1].Q); Assert.Equal("", s.Units[1].Order);
    }

    // R-MOVE-003: catalogue selection, not accidental equality with the legacy stack default.
    [Fact]
    public void StackLimitComesFromCatalogue()
    {
        var c = Load(); c.Gdd03Balance!.Values["military.stack_limit"].Values[0] = 1;
        var s = With(c, Of("recruit"), Of("car", 2, 3));
        Assert.NotNull(Movement.Full(c, s, s.Units[1], 3, 1));
    }

    // R-MOVE-001: no artillery movement number or infantry-column fallback is introduced.
    [Fact]
    public void DeferredArtilleryCannotMoveButItsExistingFireContractRemains()
    {
        var c = Load(); var s = With(c, Of("gun"), Of("enemy", 2, 4, side: "egypt"));
        Assert.Contains(1, Movement.NeedsOrders(c,s,new Dictionary<int,int>()));
        Assert.Empty(Movement.Reachable(c, s, 1));
        Assert.Equal(Movement.MissingArtilleryMovement, Movement.GiveGoTo(c, s, 1, 3, 1, new Dictionary<int, int>(), new List<Cause>()));
        s.Arms = 0;
        Assert.Null(Combat.Check(c, s, 1, 4, 1, spent: new Dictionary<int, int>())); // R-COMBAT-007
        var spent = new Dictionary<int,int>(); var why = new List<Cause>();
        var before = s.Hash();
        Assert.Equal(Movement.MissingArtilleryMovement, Movement.Move(c,s,1,3,1,spent,why));
        Assert.Equal(before,s.Hash()); Assert.Empty(why); Assert.Empty(spent);
        Assert.True(Movement.ActionsLeft(c,s,1,spent) > 0);
        Assert.Null(Combat.Strike(c,s,1,4,1,spent,new Dictionary<int,(int Q,int R)>(),why));
        Assert.Equal(0,s.Arms); Assert.DoesNotContain(1,Movement.NeedsOrders(c,s,spent));
        Assert.Equal(0,Movement.ActionsLeft(c,s,1,spent));
        Assert.NotNull(Combat.Check(c,s,1,4,1,spent:spent));

    }

    // R-COMBAT-010, R-COMBAT-002, R-ORDER-002, R-ORDER-003: additive, either army, no Hold or unsettled bonus.
    [Theory]
    [InlineData("israel", "", "", 50)]
    [InlineData("egypt", "", "", 50)]
    [InlineData("egypt", "hold", "", 50)]
    [InlineData("egypt", "fortifying", "3,1", 50)]
    [InlineData("egypt", "fortify", "3,1", 44)]
    [InlineData("egypt", "fortify", "2,1", 50)]
    public void FortDefenceIsAdditiveAndIndependentOfOccupant(string defenderSide, string order, string target, int expected)
    {
        var c = Load(biome: "mountains");
        c.MapLocations.Add(new MapLocation { Id = "fixture_fort", Kind = "fort", Q = 3, R = 1 });
        c.MapLocations.Add(new MapLocation { Id = "duplicate_marker", Kind = "fort", Q = 3, R = 1 });
        string attackSide = defenderSide == "israel" ? "egypt" : "israel";
        var s = With(c, Of("recruit", side: attackSide), Of("enemy", 2, 3, side: defenderSide, order: order, target: target));
        Assert.Equal(expected, Combat.Preview(c, s, 1, 3, 1)!.Value.RatioPercent);
        s.Units.Remove(2); // destruction/vacancy cannot remove a content property
        Assert.True(c.HasPermanentFort(3, 1));
        s.Units.Add(2, Of("enemy", 2, 3, side: defenderSide));
        Assert.Equal(50, Combat.Preview(c, s, 1, 3, 1)!.Value.RatioPercent);
        c.MapLocations.Clear();
        Assert.Equal(66, Combat.Preview(c, s, 1, 3, 1)!.Value.RatioPercent);
    }

    // R-COMBAT-010: the actual existing fort roster, no name/coordinate guessing.
    [Fact]
    public void ExistingFortRecordsSupplyBothHexesOnlyUnderGdd03()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var forts = c.MapLocations.Where(l => l.Kind == "fort").ToArray();
        Assert.Equal(new[] { "iraq_suwaydan_fort", "latrun_fort" }, forts.Select(l => l.Id).OrderBy(x => x));
        Assert.All(forts, l => Assert.False(c.HasPermanentFort(l.Q, l.R)));
        c.Eras[0].Systems.Ruleset = SaveGame.Gdd03Ruleset;
        Assert.All(forts, l => Assert.True(c.HasPermanentFort(l.Q, l.R)));
        foreach (var fort in forts)
        foreach (var defenderSide in new[] { "israel", "egypt" })
        {
            var neighbour = Movement.Neighbours.Select(d => c.HexAt(fort.Q + d.Q, fort.R + d.R)).First(h => h != null && h.Land)!;
            var s = Simulation.NewGame(c, 1, Mode.Historical); s.Units.Clear();
            var attackerSide = defenderSide == "israel" ? "egypt" : "israel";
            s.Units.Add(1, Of("recruit", q: neighbour.Q, r: neighbour.R, side: attackerSide));
            s.Units.Add(2, Of("recruit", 2, fort.Q, fort.R, side: defenderSide));
            var biome = c.HexAt(fort.Q, fort.R)!.Biome;
            var terrain = biome == "mountains" ? 50 : biome == "hills" ? 25 : 0;
            Assert.Equal(10_000 / (150 + terrain), Combat.Preview(c, s, 1, fort.Q, fort.R)!.Value.RatioPercent);
        }
    }

    // R-MOVE-001, R-COMBAT-010: malformed consumed values fail at load, never use old numbers.
    [Theory]
    [InlineData("military.move_infantry", "basis: Deferred, expression: pending, note: fixture")]
    [InlineData("military.move_armoured", "basis: Default, values: [1.5], note: fixture")]
    [InlineData("military.move_road", "basis: Default, values: [0], note: fixture")]
    [InlineData("military.move_rail", "basis: Default, values: [1, 2], note: fixture")]
    [InlineData("military.fort_bonus", "basis: Default, values: [-1], note: fixture")]
    public void LoaderRejectsInvalidConsumedScalar(string key, string row)
    {
        var f = Files();
        f[Gdd03BalanceCatalogue.FileName] = System.Text.RegularExpressions.Regex.Replace(f[Gdd03BalanceCatalogue.FileName],
            "(?m)^  \"" + System.Text.RegularExpressions.Regex.Escape(key) + "\":\\n(?:    .*\\n)*", "  \"" + key + "\": { " + row + " }\n");
        var error = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f)));
        Assert.Contains(key, error.Message);
        f["rules.yaml"] = f["rules.yaml"].Replace("move_cost: { coastal_plain: 1, hills: 2, desert: 3 }",
            "move_cost: { coastal_plain: 1, lowland: 1, valley: 1, hills: 2, mountains: 3, desert: 3 }")
            .Replace("combat_terrain_percent: { coastal_plain: 100, hills: 150, desert: 100 }",
            "combat_terrain_percent: { coastal_plain: 100, lowland: 100, valley: 100, hills: 150, mountains: 150, desert: 100 }");
        f["eras.yaml"] = f["eras.yaml"].Replace("gdd-0.3", "gdd-0.1");
        // Catalogue structural validation still applies, but none of these valid catalogue rows is a consumed old-rule requirement.
        Assert.NotNull(ContentLoader.Load(Fixture.Write(f)));
    }
    // R-MOVE-007: both endpoints must belong to the same built segment for either corridor kind.
    [Theory]
    [InlineData("road")]
    [InlineData("rail")]
    public void CorridorCostsUseCatalogueAndRequireBuiltSharedSegment(string kind)
    {
        var f = Files();
        f["corridors.yaml"] = Fixture.GddCorridors;
        f["map/hexes.json"] = MovementTests.Board((3, 0, "mountains"), (3, 1, "mountains"));
        var c = ContentLoader.Load(Fixture.Write(f));
        var r = kind == "road" ? 0 : 1;
        var s = With(c, Of("car", r: r));
        Assert.Equal(3, Movement.Reachable(c, s, 1)[(3, r)]);
        var id = kind == "road" ? "road_a" : "rail_a";
        var built = new DateTime(1948, 5, 15);
        s.Corridors[id] = new CorridorWork(id, kind == "road"
            ? new SegmentWork?[] { new SegmentWork(null, 6, built), null }
            : new SegmentWork?[] { new SegmentWork(null, 6, built) });
        Assert.Equal(1, Movement.Reachable(c, s, 1)[(3, r)]);
        c.Gdd03Balance!.Values["military.move_" + kind].Values[0] = 2;
        Assert.Equal(2, Movement.Reachable(c, s, 1)[(3, r)]);
        // Crossing into the segment from outside it gets no discount.
        s.Units[1] = Of("car", q: 3, r: r + 1);
        Assert.Equal(3, Movement.Reachable(c, s, 1)[(3, r)]);
        s.Corridors[id] = new CorridorWork(id, kind == "road"
            ? new SegmentWork?[] { new SegmentWork(9, 5, null), null }
            : new SegmentWork?[] { new SegmentWork(9, 5, null) });
        s.Units[1] = Of("car", r: r);
        Assert.Equal(3, Movement.Reachable(c, s, 1)[(3, r)]);
    }

    // R-COMBAT-010, R-MOVE-006: moving clears temporary fortify, never the permanent property.
    [Fact]
    public void LeavingAndReturningKeepsOnlyThePermanentFort()
    {
        var c = Load(); c.MapLocations.Add(new MapLocation { Id = "fixture", Kind = "fort", Q = 2, R = 1 });
        var s = With(c, Of("recruit", order: Orders.Fortify, target: "2,1"));
        var why = new List<Cause>(); var spent = new Dictionary<int, int>();
        Assert.Null(Movement.Move(c, s, 1, 3, 1, spent, why));
        Assert.True(c.HasPermanentFort(2, 1));
        Assert.Null(Movement.Move(c, s, 1, 2, 1, spent, why));
        Assert.Equal("", s.Units[1].Order);
        Assert.True(c.HasPermanentFort(2, 1));
    }

    // R-COMBAT-002, R-COMBAT-010: preview and resolution use exactly the same ratio; one engagement draws once.
    [Fact]
    public void FortPreviewMatchesImmediateEngagementWithMultipleDefenders()
    {
        var c = Load(biome: "mountains");
        c.MapLocations.Add(new MapLocation { Id = "fixture", Kind = "fort", Q = 3, R = 1 });
        var s = With(c, Of("recruit"), Of("enemy", 2, 3, side: "egypt"), Of("enemy", 3, 3, side: "egypt"));
        var before = s.RngState; var rng = new Rng(before); rng.NextDouble();
        Assert.Equal(25, Combat.Preview(c, s, 1, 3, 1)!.Value.RatioPercent);
        var why = new List<Cause>();
        Assert.Null(Combat.Strike(c, s, 1, 3, 1, new Dictionary<int, int>(), new Dictionary<int, (int Q, int R)>(), why));
        Assert.Equal(rng.State, s.RngState);
        Assert.Contains(why, line => line.Key == WhyKeys.Engagement && line.Args[1] == "25");
        Assert.True(c.HasPermanentFort(3, 1));
    }

    // R-MOVE-001: every missing consumed scalar must be named, with no old-rule fallback.
    [Theory]
    [InlineData("military.move_infantry")]
    [InlineData("military.move_armoured")]
    [InlineData("military.move_road")]
    [InlineData("military.move_rail")]
    [InlineData("military.stack_limit")]
    [InlineData("military.fort_bonus")]
    [InlineData("military.fortify_bonus")]
    [InlineData("military.fortify_turns")]
    public void LoaderRejectsMissingConsumedKey(string key)
    {
        var f = Files();
        f[Gdd03BalanceCatalogue.FileName] = f[Gdd03BalanceCatalogue.FileName].Replace("\"" + key + "\":", "\"unused.fixture\":");
        Assert.Contains(key, Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f))).Message);
    }

    // R-MOVE-001: the two role columns have fixed shape, and every biome part is required.
    [Theory]
    [InlineData("movement", "basis: Default, values: [1], note: fixture")]
    [InlineData("movement", "basis: Default, values: [1, 1.5], note: fixture")]
    [InlineData("movement", "basis: Deferred, expression: pending, note: fixture")]
    [InlineData("defence_percent", "basis: Default, values: [-1], note: fixture")]
    public void LoaderRejectsMalformedBiomePart(string part, string row)
    {
        var f = Files();
        var yaml = f[Gdd03BalanceCatalogue.FileName];
        var start = yaml.IndexOf("  \"mountains\":", StringComparison.Ordinal);
        var end = yaml.IndexOf("  \"desert\":", start, StringComparison.Ordinal);
        var section = yaml.Substring(start, end - start);
        section = System.Text.RegularExpressions.Regex.Replace(section,
            "(?m)^      \"" + part + "\":\\n(?:        .*\\n)*", "      \"" + part + "\": { " + row + " }\n");
        f[Gdd03BalanceCatalogue.FileName] = yaml.Substring(0, start) + section + yaml.Substring(end);
        Assert.Contains(part, Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f))).Message);
    }

    // R-MOVE-001: absent biome rows/parts never fall through to the old move_cost table.
    [Theory]
    [InlineData("mountains", "missing_mountains")]
    [InlineData("movement", "missing_movement")]
    [InlineData("defence_percent", "missing_defence")]
    public void LoaderRejectsMissingBiomeData(string key, string replacement)
    {
        var f = Files();
        f[Gdd03BalanceCatalogue.FileName] = f[Gdd03BalanceCatalogue.FileName].Replace("\"" + key + "\":", "\"" + replacement + "\":");
        Assert.Contains(key, Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f))).Message);
    }

    // R-COMBAT-010: fort identity follows the effective authoring placement rather than original coordinates.
    [Fact]
    public void FortQueryUsesMapOverridesAndRejectsWaterPlacement()
    {
        var f = Files();
        var fort = Fixture.Files["map/locations_1948.yaml"].Replace("locations:", "")
            .Replace("town_a","fixture_fort").Replace("kind: city","kind: fort").Replace("q: 1","q: 3").Replace("r: 0","r: 1").Replace("managed_city: true","managed_city: false");
        f["map/locations_1948.yaml"] = Fixture.Files["map/locations_1948.yaml"] + fort;
        f[MapOverrides.FileName] = "terrain: []\ncontrol: []\nlocations: [{ id: fixture_fort, hex: { q: 4, r: 1 } }]\n";
        var c = ContentLoader.Load(Fixture.Write(f));
        Assert.False(c.HasPermanentFort(3,1)); Assert.True(c.HasPermanentFort(4,1));
        var state = With(c,Of("recruit",q:3),Of("enemy",2,4,side:"egypt"));
        Assert.Equal(66,Combat.Preview(c,state,1,4,1)!.Value.RatioPercent);
        state.Units[1]=Of("recruit",q:2); state.Units[2]=Of("enemy",2,3,side:"egypt");
        Assert.Equal(100,Combat.Preview(c,state,1,3,1)!.Value.RatioPercent);
        f["map/hexes.json"] = MovementTests.Board((4,1,"sea"));
        var ex = Assert.Throws<ContentException>(()=>ContentLoader.Load(Fixture.Write(f)));
        Assert.Equal(MapOverrides.FileName,ex.File); Assert.Equal("locations[0].hex",ex.Field);
        Assert.Contains("not a land hex",ex.Message); // Existing generic override rejection, not claimed as new fort-specific red.

    }

    // R-ENEMY-002, R-MOVE-001: enemy path progress uses its class allowance and the same execution cost.
    [Fact]
    public void EnemyProgressUsesTheTwoPointInfantryAllowance()
    {
        var c = Load(); var u = Of("enemy", side: "egypt"); var s = With(c, u);
        var target = Movement.Toward(c, s, u, 5, 1);
        Assert.NotNull(target); Assert.Equal((4, 1), target!.Value);
        var spent = new Dictionary<int, int>();
        Assert.Null(Movement.Move(c, s, 1, target.Value.Q, target.Value.R, spent, new List<Cause>(), enemy: true));
        Assert.Equal(2, spent[1]);
    }

    // R-COMBAT-010, R-COMBAT-005: a fort helps defenders only; invalid combat changes no state.
    [Fact]
    public void AttackingFromAFortAddsNothingAndRefusalMutatesNothing()
    {
        var c = Load(); c.MapLocations.Add(new MapLocation { Id = "fixture", Kind = "fort", Q = 2, R = 1 });
        var state = With(c, Of("recruit"), Of("enemy", 2, 3, side: "egypt"));
        Assert.Equal(100, Combat.Preview(c, state, 1, 3, 1)!.Value.RatioPercent);
        var before = state.Hash();
        Assert.NotNull(Combat.Strike(c, state, 1, 5, 1, new Dictionary<int,int>(),new Dictionary<int,(int Q,int R)>(),new List<Cause>()));
        Assert.Equal(before,state.Hash()); Assert.True(c.HasPermanentFort(2,1));
    }

    // R-SAVE-001/002, R-MOVE-001, R-COMBAT-010: authored synthetic content precedes NewGame, actual committed commands replay.
    [Fact]
    public void Gdd03MoveAndFortCombatReplayWithoutAnUnsupportedGoldenPolicy()
    {
        var c=Load(); c.StartingForces.Clear();
        c.StartingForces.Add(new StartingForce { Row="fixture_own",Type="recruit",Side="israel",Strength=6,Q=1,R=1 });
        c.StartingForces.Add(new StartingForce { Row="fixture_enemy",Type="enemy",Side="egypt",Strength=6,Q=3,R=1 });
        c.MapLocations.Add(new MapLocation { Id="fixture_fort",Kind="fort",Q=3,R=1 });
        var g=GameSession.New(c,1,Mode.Historical);
        Assert.Null(g.Commit(new Move(0,2,1))); Assert.Equal(1,Movement.PointsLeft(c,g.Now.State,0,g.Now.Spent));
        Assert.Equal(66,Combat.Preview(c,g.Now.State,0,3,1)!.Value.RatioPercent);
        Assert.Null(g.Commit(new UnitOrder(0,Combat.Attack,3,1)));
        var save=g.Save.Write(); var loaded=GameSession.Load(c,save,out var warnings);
        Assert.Empty(warnings); Assert.Equal(save,loaded.Save.Write()); Assert.Equal(g.Now.State.Hash(),loaded.Now.State.Hash());
        Assert.Equal(g.Now.Why.Select(x=>x.Message),loaded.Now.Why.Select(x=>x.Message));
        Assert.True(c.HasPermanentFort(3,1));
    }

}
