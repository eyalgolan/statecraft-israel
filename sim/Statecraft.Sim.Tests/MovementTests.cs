using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;
using Xunit.Abstractions;

/// <summary>Movement (GDD Phase 5 PR 5, #744): Move(unit, q, r) and the Reachable preview. Q-MOVE-1, Q-MOVE-3, Q-MOVE-4 and
/// Q-MOVE-5 are the plan's recommendations, taken by the owner on 15 October 2026 ("Take all recommendations", #375); Q-CIV-1
/// is the owner's "No entry until Phase 6". The fixture's numbers are fixture values: coastal plain 1, hills 2, desert 3,
/// two units of a side per hex, and a soldier with 3 movement points.</summary>
public class MovementTests
{
    readonly ITestOutputHelper _out;
    public MovementTests(ITestOutputHelper output) { _out = output; }

    /// <summary>A soldier, an Egyptian unit (an enemy side) and the fixture's halutz types.</summary>
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    /// <summary>A 6 x 3 board of coastal plain, q 0 to 5 and r 0 to 2, with <paramref name="biomes"/> replacing hexes (a
    /// "sea" or "lake" one is water). town_a stays on (1, 0) and site_a on (2, 0), as the fixture's other files need.</summary>
    internal static string Board(params (int Q, int R, string Biome)[] biomes)
    {
        var rows = new List<string>();
        for (int r = 0; r < 3; r++)
            for (int q = 0; q < 6; q++)
            {
                var b = biomes.Where(x => x.Q == q && x.R == r).Select(x => x.Biome).DefaultIfEmpty("coastal_plain").First();
                var land = b != "sea" && b != "lake";
                rows.Add(FormattableString.Invariant($"{{\"q\":{q},\"r\":{r},\"lon\":{34.3 + 0.1 * q},\"lat\":{33.3 - 0.1 * r},\"elev_m\":0.0,\"land\":{(land ? "true" : "false")},\"biome\":\"{b}\"}}"));
            }
        return "{\"attribution\":[\"test\"],\"bounds\":{\"lon0\":34.2,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35},\"hex_size_km\":5.0,\"layout\":\"pointy\",\"hexes\":[\n"
            + string.Join(",\n", rows) + "]}\n";
    }

    static Content Load(string board, params (string From, string To)[] edits) => Load(board, "corridors: []\n", edits);

    static Content Load(string board, string corridors, params (string From, string To)[] edits)
    {
        // The shared fixture's corridors belong to its own map, so this custom board has only the corridors given.
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = board, ["corridors.yaml"] = corridors
        });
        foreach (var (from, to) in edits) files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        return ContentLoader.Load(Fixture.Write(files));
    }

    static Content Plain(params (string From, string To)[] edits) => Load(Board(), edits);

    /// <summary>A new game with each of <paramref name="units"/> added in the order given.</summary>
    static GameState With(Content c, params Unit[] units)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        foreach (var u in units) s.Units.Add(u.Id, u);
        s.NextId = units.Length == 0 ? s.NextId : Math.Max(s.NextId, units.Max(u => u.Id) + 1);
        return s;
    }

    static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel");
    static Unit Enemy(int id, int q, int r) => new Unit(id, "enemy", q, r, side: "egypt");

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    // R-MOVE-001: the cost of entering one hex is its biome's.
    [Theory]
    [InlineData("coastal_plain", 1)]
    [InlineData("hills", 2)]
    [InlineData("desert", 3)]
    public void TerrainCosts(string biome, int cost)
    {
        var c = Load(Board((3, 1, biome)));
        var s = With(c, Soldier(1, 2, 1));
        Assert.Equal(cost, Movement.Reachable(c, s, 1)[(3, 1)]);
    }

    // R-MOVE-001: the real game's numbers, design values this PR proposes (Q-MOVE-1): one cost per land biome of hexes.json,
    // none for lake and sea, and the movement points of the three military types.
    [Fact]
    public void TheRealGameHasACostForEveryLandBiomeAndPointsForEveryMilitaryType()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var costs = c.Rules.Mvp!.MoveCost;
        Assert.Equal(new SortedDictionary<string, int> { ["coastal_plain"] = 1, ["desert"] = 2, ["hills"] = 2, ["lowland"] = 1, ["mountains"] = 3, ["valley"] = 1 },
                     new SortedDictionary<string, int>(costs));
        Assert.Equal(new SortedSet<string>(costs.Keys), new SortedSet<string>(c.Hexes.Where(h => h.Land).Select(h => h.Biome)));
        Assert.DoesNotContain(c.Hexes, h => h.Land && (h.Biome == "sea" || h.Biome == "lake"));
        Assert.Equal(new[] { ("veteran_soldier", 3), ("artillery", 2), ("armoured_car", 5), ("egyptian_army", 3), ("arab_legion", 3),
                             ("syrian_army", 3), ("iraqi_army", 3), ("arab_liberation_army", 3) },   // the Arab types: #743, owner, 20 October 2026
                     c.UnitTypes.Where(t => t.Side != "").Select(t => (t.Id, t.MovementPoints)));
    }

    // R-MOVE-001: a path's cost is the sum of the hexes it enters, not of the one it leaves.
    [Fact]
    public void APathCostsTheSumOfTheHexesItEnters()
    {
        var c = Load(Board((0, 1, "desert"), (1, 1, "hills")));
        var s = With(c, Soldier(1, 0, 1));
        var reach = Movement.Reachable(c, s, 1);
        Assert.Equal(2, reach[(1, 1)]);      // hills; leaving the desert costs nothing
        Assert.Equal(3, reach[(2, 1)]);      // hills then coastal plain, or three plain hexes: both 3
        Assert.Equal(new[] { (1, 1), (2, 1) }, Movement.Path(c, s, 1, 2, 1));   // (1, 1) comes first in (cost, q, r) order
        Assert.False(reach.ContainsKey((0, 1)));   // the unit's own hex is not a move
    }

    // R-MOVE-002: the same state, built twice and with its units added in another order, gives the same path and preview.
    [Fact]
    public void ThePathIsTheSameEveryRun()
    {
        var c = Load(Board((2, 1, "hills"), (3, 0, "desert")));
        var a = With(c, Soldier(1, 0, 1), Soldier(2, 1, 2), Enemy(3, 2, 2));
        var b = With(c, Enemy(3, 2, 2), Soldier(2, 1, 2), Soldier(1, 0, 1));
        Assert.Equal(a.Hash(), b.Hash());
        var path = Movement.Path(c, a, 1, 2, 1);
        Assert.NotEmpty(path);
        Assert.Equal(path, Movement.Path(c, b, 1, 2, 1));
        Assert.Equal(path, Movement.Path(c, a, 1, 2, 1));
        Assert.Equal(Movement.Reachable(c, a, 1).ToList(), Movement.Reachable(c, b, 1).ToList());
        Assert.Equal(Step(c, a, new Move(1, 2, 1)).State.Hash(), Step(c, b, new Move(1, 2, 1)).State.Hash());
    }

    // R-MOVE-002: of two paths of equal cost, a hex is reached from the neighbour that comes first in (cost, q, r) order:
    // (0, 1) to (2, 0) goes by (1, 0), not by (1, 1) (r decides); (0, 1) to (1, 2) goes by (0, 2), not by (1, 1) (q decides);
    // and (2, 1) to (0, 2) goes by (1, 1), not by (1, 2).
    [Fact]
    public void EqualCostPathsBreakTiesByTheStatedOrder()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1), Soldier(2, 2, 1));
        Assert.Equal(new[] { (1, 0), (2, 0) }, Movement.Path(c, s, 1, 2, 0));
        Assert.Equal(new[] { (0, 2), (1, 2) }, Movement.Path(c, s, 1, 1, 2));
        Assert.Equal(new[] { (1, 1), (0, 2) }, Movement.Path(c, s, 2, 0, 2));
    }

    // R-MOVE-003: a hex holds at most stack_limit units of one side; a full hex is refused as a target and not previewed,
    // and a path may still pass through it.
    [Fact]
    public void StackingLimit()
    {
        var c = Load(Board((0, 1, "sea"), (1, 1, "sea"), (2, 1, "sea")));
        // With row 1 at sea, (0, 2) - (1, 2) - (2, 2) is a corridor; two soldiers hold (1, 2), the limit.
        var s = With(c, Soldier(1, 0, 2), Soldier(2, 1, 2), Soldier(3, 1, 2));
        Assert.False(Movement.Reachable(c, s, 1).ContainsKey((1, 2)));
        var r = Step(c, s, new Move(1, 1, 2));
        Assert.Equal("That hex already holds 2 units of that side, the most one hex can hold", Assert.Single(r.Rejected).Reason);
        Assert.Equal((0, 2), (r.State.Units[1].Q, r.State.Units[1].R));
        Assert.Equal(new[] { (1, 2), (2, 2) }, Movement.Path(c, s, 1, 2, 2));   // passing through is not stacking
        Assert.Empty(Step(c, s, new Move(1, 2, 2)).Rejected);
        var one = With(c, Soldier(1, 0, 2), Soldier(2, 1, 2));
        var moved = Step(c, one, new Move(1, 1, 2));
        Assert.Empty(moved.Rejected);
        Assert.Equal((1, 2), (moved.State.Units[1].Q, moved.State.Units[1].R));
        // Only the unit's own side counts: two halutzim (no side) on the hex leave room for soldiers.
        Assert.True(Movement.Reachable(c, With(c, Soldier(1, 0, 2), new Unit(2, "halutz", 1, 2), new Unit(3, "halutz", 1, 2)), 1).ContainsKey((1, 2)));
    }

    // R-MOVE-004: no zone of control, so a unit passes next to an enemy at the terrain's cost; it never enters or passes
    // through an enemy unit's hex (entering one is an attack, Q-MOVE-3).
    [Fact]
    public void EnemyGround()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1), Enemy(2, 2, 1));
        var reach = Movement.Reachable(c, s, 1);
        Assert.False(reach.ContainsKey((2, 1)));
        Assert.Equal(1, reach[(1, 1)]);          // next to the enemy, at the plain's cost
        Assert.Equal(new[] { (1, 0), (2, 0), (3, 0) }, Movement.Path(c, s, 1, 3, 0));   // past it, not through it
        Assert.False(reach.ContainsKey((3, 1)));   // the straight line runs through it, and the way round costs 4
        var r = Step(c, s, new Move(1, 2, 1));
        Assert.Equal("An enemy unit holds that hex", Assert.Single(r.Rejected).Reason);
        Assert.Equal((0, 1), (r.State.Units[1].Q, r.State.Units[1].R));
    }

    // R-MOVE-005 (Q-CIV-1, "No entry until Phase 6"): a player unit cannot enter the hex of a locality listed in
    // mvp.enemy_held_localities, and no path passes through one. The real list is the owner-accepted DRAFT (19 October 2026,
    // #1073): unchecked until research task #1096 checks each place's 15 May 1948 control.
    [Fact]
    public void NoEntryToEnemyHeldLocalities()
    {
        var c = Plain(("enemy_held_localities: []", "enemy_held_localities: [town_a]"));
        var s = With(c, Soldier(1, 0, 0));
        var reach = Movement.Reachable(c, s, 1);
        Assert.False(reach.ContainsKey((1, 0)));
        Assert.Equal(3, reach[(2, 0)]);          // around town_a's hex, by (0, 1) and (1, 1), not through it for 2
        Assert.DoesNotContain((1, 0), Movement.Path(c, s, 1, 2, 0));
        var r = Step(c, s, new Move(1, 1, 0));
        Assert.Equal("Units may not enter an enemy-held locality before its capture is possible", Assert.Single(r.Rejected).Reason);
        Assert.Equal((0, 0), (r.State.Units[1].Q, r.State.Units[1].R));
        var real = ContentLoader.Load(RealContentTests.ContentRoot());
        var draft = real.Rules.Mvp!.EnemyHeldLocalities;
        Assert.Equal(new[] { "acre", "nazareth", "lod", "ramla", "gaza", "beersheba" }, draft);
        Assert.All(draft, id => Assert.Contains(real.SettlementHexes, h => h.Id == id));
    }

    // R-MOVE-006 and the preview: every hex Reachable lists can be reached with Move, at the listed cost, and no other hex
    // can, on a board with water, terrain, an enemy, a full hex and an enemy-held locality.
    [Fact]
    public void ReachableMatchesMove()
    {
        var c = Load(Board((2, 1, "hills"), (3, 1, "desert"), (4, 0, "sea"), (0, 2, "lake"), (3, 2, "hills")),
                     ("enemy_held_localities: []", "enemy_held_localities: [town_a]"));
        // Strengths so the enemy's AI holds instead of attacking the soldier when it ends next to it (R-ENEMY-003).
        var s = With(c, new Unit(1, "soldier", 1, 1, side: "israel", strength: 5), new Unit(2, "enemy", 3, 0, side: "egypt", strength: 1), Soldier(3, 2, 2), Soldier(4, 2, 2));
        var reach = Movement.Reachable(c, s, 1);
        Assert.NotEmpty(reach);
        foreach (var h in c.Hexes)
        {
            var r = Step(c, s, new Move(1, h.Q, h.R));
            if (reach.ContainsKey((h.Q, h.R)))
            {
                Assert.Empty(r.Rejected);
                Assert.Equal((h.Q, h.R), (r.State.Units[1].Q, r.State.Units[1].R));
            }
            else
            {
                Assert.Single(r.Rejected);
                Assert.Equal((1, 1), (r.State.Units[1].Q, r.State.Units[1].R));
            }
        }
    }

    // R-MOVE-006: refused with the state unchanged, with the reason, for each case.
    [Theory]
    [InlineData(9, 2, 1, "There is no such unit")]
    [InlineData(2, 3, 1, "That unit is not yours")]
    [InlineData(3, 1, 1, "That unit cannot move")]
    [InlineData(1, 0, 1, "That unit is already there")]
    [InlineData(1, 0, 0, "Units cannot enter water or leave the map")]
    [InlineData(1, 9, 9, "Units cannot enter water or leave the map")]
    [InlineData(1, 5, 1, "That hex is out of reach this turn")]
    public void AMoveThatBreaksARuleIsRefusedWithTheStateUnchanged(int unit, int q, int r, string reason)
    {
        var c = Load(Board((0, 0, "sea")));
        var s = With(c, Soldier(1, 0, 1), Enemy(2, 4, 2), new Unit(3, "halutz", 1, 2));
        var before = Step(c, s).State.Hash();
        var result = Step(c, s, new Move(unit, q, r));
        Assert.Equal(reason, Assert.Single(result.Rejected).Reason);
        Assert.Equal(before, result.State.Hash());
    }

    // R-MOVE-001: the allowance is per turn. A second Move of the same unit in one turn has only what the first left, and
    // the next turn has the full allowance again.
    [Fact]
    public void MovementPointsArePerTurn()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var r = Step(c, s, new Move(1, 2, 1), new Move(1, 3, 1), new Move(1, 5, 1));
        Assert.Equal("That hex is out of reach this turn", Assert.Single(r.Rejected).Reason);
        Assert.Equal((3, 1), (r.State.Units[1].Q, r.State.Units[1].R));
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Unit 1 moved to hex 2,1 for 2 movement points");
        Assert.Empty(Step(c, r.State, new Move(1, 5, 1)).Rejected);
    }

    // R-MOVE-006, Q-HOLD-1 ("lost on move"): a move that goes through clears fortify, set or given this turn, and the hex it
    // names, even when the unit comes back to that hex in the same turn; hold moves with the unit; a refused move changes
    // nothing. The order values are the standing orders' (Phase 5 PR 7, #746), written here as records.
    [Theory]
    [InlineData("fortify", "0,1", "", "")]
    [InlineData("fortifying", "0,1", "", "")]
    [InlineData("hold", "", "hold", "")]
    public void AMoveOutAndBackClearsFortifyAndKeepsHold(string order, string target, string afterOrder, string afterTarget)
    {
        var c = Plain();
        var s = With(c, new Unit(1, "soldier", 0, 1, side: "israel", order: order, orderTarget: target));
        var r = Step(c, s, new Move(1, 1, 1), new Move(1, 0, 1));
        Assert.Empty(r.Rejected);
        Assert.Equal((0, 1, afterOrder, afterTarget), (r.State.Units[1].Q, r.State.Units[1].R, r.State.Units[1].Order, r.State.Units[1].OrderTarget));
        Assert.Equal(order == "fortify" ? 1 : 0, r.Why.Count(x => x.System == "map" && x.Message == "Unit 1 left hex 0,1 and is no longer fortified"));

        var refused = Step(c, s, new Move(1, 5, 1));   // refused by the last check before the move
        Assert.Equal("That hex is out of reach this turn", Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), refused.State.Hash());   // as a turn with no Move: the order is the turn's own
    }

    /// <summary>One corridor of <paramref name="kind"/> on the desert row r = 1: segment 1 over (1, 1) and (2, 1), segment 2
    /// over (2, 1) and (3, 1). Fixture values, not history.</summary>
    static string DesertCorridor(string kind) => "corridors:\n  - id: cor_a\n    kind: " + kind + "\n    name: Cor A\n    name_he: מסלול א\n"
        + "    route: cor_a\n    segments:\n      - { to: X, hexes: [[1, 1], [2, 1]] }\n      - { to: Y, hexes: [[2, 1], [3, 1]] }\n"
        + "    sources:\n      - { url: \"https://example.org/cor_a\", checked: 2026-10-15 }\n    status: draft\n";

    static Content DesertRow(string kind) => Load(Board((1, 1, "desert"), (2, 1, "desert"), (3, 1, "desert"), (4, 1, "desert")), DesertCorridor(kind));

    static readonly DateTime Built = new DateTime(1948, 6, 15);

    // R-MOVE-007: a step from one hex of a built segment to another hex of the same segment costs the kind's cost (the
    // fixture's road 1, rail 2) instead of the desert's 3, and Move charges it; the step onto the unbuilt segment 2 costs
    // the terrain again.
    [Theory]
    [InlineData("road", 1)]
    [InlineData("rail", 2)]
    public void RoadsAreCheaper(string kind, int cost)
    {
        var c = DesertRow(kind);
        var s = With(c, Soldier(1, 1, 1));
        s.Corridors["cor_a"] = new CorridorWork("cor_a", new SegmentWork?[] { new SegmentWork(null, 6, Built), null });
        var reach = Movement.Reachable(c, s, 1);
        Assert.Equal(cost, reach[(2, 1)]);
        Assert.False(reach.ContainsKey((3, 1)));   // cost + the desert's 3 is more than 3 points
        var r = Step(c, s, new Move(1, 2, 1));
        Assert.Empty(r.Rejected);
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == $"Unit 1 moved to hex 2,1 for {cost} movement points");
    }

    // R-MOVE-007: a kind's cost applies only when it is lower than the terrain's, so the fixture's rail (2) on coastal plain
    // (1) costs the plain's 1.
    [Fact]
    public void ACorridorNeverCostsMoreThanTheTerrain()
    {
        var c = Load(Board(), DesertCorridor("rail"));
        var s = With(c, Soldier(1, 1, 1));
        s.Corridors["cor_a"] = new CorridorWork("cor_a", new SegmentWork?[] { new SegmentWork(null, 6, Built), null });
        Assert.Equal(1, Movement.Reachable(c, s, 1)[(2, 1)]);
    }

    // R-MOVE-007: a segment not started, or being built, is no corridor yet: the step costs the terrain.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnUnbuiltSegmentCostsTerrain(bool started)
    {
        var c = DesertRow("road");
        var s = With(c, Soldier(1, 1, 1));
        if (started) s.Corridors["cor_a"] = new CorridorWork("cor_a", new SegmentWork?[] { new SegmentWork(9, 5, null), null });
        Assert.Equal(3, Movement.Reachable(c, s, 1)[(2, 1)]);
    }

    // R-MOVE-007: a kind's cost needs BOTH ends on the segment: a step from a hex off the built segment onto one of its hexes
    // costs the terrain's 3, not the road's 1.
    [Fact]
    public void AStepOntoABuiltSegmentFromOutsideItCostsTerrain()
    {
        var c = DesertRow("road");
        var s = With(c, Soldier(1, 1, 2));
        s.Corridors["cor_a"] = new CorridorWork("cor_a", new SegmentWork?[] { new SegmentWork(null, 6, Built), null });
        Assert.Equal(3, Movement.Reachable(c, s, 1)[(1, 1)]);
    }

    // R-MOVE-001: the loader refuses a military type with no movement points, a population type with some, a land biome
    // with no cost, a cost for water, a stack limit below 1, a missing or zero corridor cost and an unknown locality.
    [Theory]
    [InlineData("unit_types.yaml", "types[soldier].movement_points", "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }", "  - { id: soldier, kind: unit, side: israel, class: infantry }", "required for a military type, at least 1")]
    [InlineData("unit_types.yaml", "types[civilian].movement_points", "  - { id: civilian, kind: population }", "  - { id: civilian, kind: population, movement_points: 2 }", "only a unit moves")]
    [InlineData("rules.yaml", "mvp.move_cost", "move_cost: { coastal_plain: 1, hills: 2, desert: 3 }", "move_cost: { coastal_plain: 1, desert: 3 }", "no cost for land biome 'hills'")]
    [InlineData("rules.yaml", "mvp.move_cost.sea", "move_cost: { coastal_plain: 1, hills: 2, desert: 3 }", "move_cost: { coastal_plain: 1, hills: 2, desert: 3, sea: 1 }", "is water in map/hexes.json: lake and sea are impassable")]
    [InlineData("rules.yaml", "mvp.move_cost.hills", "move_cost: { coastal_plain: 1, hills: 2, desert: 3 }", "move_cost: { coastal_plain: 1, hills: 0, desert: 3 }", "must be >= 1")]
    [InlineData("rules.yaml", "mvp.stack_limit", "stack_limit: 2", "stack_limit: 0", "must be >= 1")]
    [InlineData("rules.yaml", "mvp.road_move_cost", "  road_move_cost: 1\n", "", "required when eras.yaml systems.loop is gdd")]
    [InlineData("rules.yaml", "mvp.rail_move_cost", "rail_move_cost: 2", "rail_move_cost: 0", "must be >= 1")]
    [InlineData("rules.yaml", "mvp.enemy_held_localities", "enemy_held_localities: []", "enemy_held_localities: [atlantis]", "unknown settlement 'atlantis'")]
    public void TheLoaderRefusesABadMovementValue(string file, string field, string from, string to, string message)
    {
        var files = Fixture.Gdd(new Dictionary<string, string> { ["unit_types.yaml"] = Types });
        Assert.Contains(from, files[file]);
        files[file] = files[file].Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal((file, field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }

    // Performance (spec §2.5): a Reachable for a fast unit on each of the real game's 25 settlement hexes, on the 870-hex
    // board, stays under one Step's 16 ms WebGL budget. Measured on .NET, best of five runs; the margin is printed.
    [Fact]
    public void AFullTurnOfMovesIsFast()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        foreach (var h in c.SettlementHexes) { var id = s.NextId++; s.Units.Add(id, new Unit(id, "armoured_car", h.Q, h.R, side: "israel")); }
        Assert.Equal(25 + c.StartingForces.Count(f => f.Arrives == null), s.Units.Count);   // with the starting forces (R-UNIT-006): the enemy's hexes block
        var best = double.MaxValue;
        var listed = 0;
        for (int run = 0; run < 5; run++)
        {
            var w = Stopwatch.StartNew();
            listed = s.Units.Keys.Sum(id => Movement.Reachable(c, s, id).Count);
            best = Math.Min(best, w.Elapsed.TotalMilliseconds);
        }
        _out.WriteLine(string.Format(CultureInfo.InvariantCulture, "{3} Reachable calls, {0} hexes listed: {1:F2} ms (budget 16 ms, {2:F1}x margin)", listed, best, 16 / best, s.Units.Count));
        Assert.True(listed > 25);
        Assert.True(best < 16, string.Format(CultureInfo.InvariantCulture, "{0:F2} ms", best));
    }
}
