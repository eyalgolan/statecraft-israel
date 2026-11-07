using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>The enemy AI, resolver step 1 (GDD Phase 5 PR 10, part 1; #749). Q-ENEMY-1 to Q-ENEMY-4 are the plan's
/// recommendations, taken by the owner on 15 October 2026 ("Take all recommendations", #375): script the 15 May and 11 June
/// 1948 positions two sources agree on (arab-forces-1948.md section 10), then the simple goals (hold fortified, attack at a
/// design ratio), no RNG, no reinforcements, no enemy supply. The fixture's numbers are fixture values: MovementTests' 6 x 3
/// coastal plain, 10 people a population unit, two units of a side per hex and an attack ratio of 200.</summary>
public class EnemyAITests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";
    const string OperationRegions = "player_regions:\n"
        + "  - id: fixture_region\n    capital_location: town_a\n    member_hexes: [{ q: 0, r: 0 }, { q: 1, r: 0 }]\n"
        + "    member_locations: [town_a]\n    design_note: 'Operation requirement test region'\n"
        + "    sources: ['test fixture: no historical claim']\n    capital_approval: NEEDS-RESEARCH\n"
        + "    binding_status: NEEDS-RESEARCH\n    opening_control: { status: NEEDS-RESEARCH, value: null }\n";

    static Content Load() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
    })));

    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    static Unit Soldier(int id, int q, int r, int strength) => new Unit(id, "soldier", q, r, side: "israel", strength: strength);
    static Unit Enemy(int id, int q, int r, int strength) => new Unit(id, "enemy", q, r, side: "egypt", strength: strength);

    static bool IsEnemy(Unit u) => u.Side != "" && u.Side != Movement.PlayerSide;

    static int Distance(int q0, int r0, int q1, int r1)
    {
        int dq = q1 - q0, dr = r1 - r0;
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
    }

    static GameState Play(Content c, GameState s, int turns)
    {
        for (int i = 0; i < turns && s.GameOver == null; i++) s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        return s;
    }

    // R-ENEMY-001: the enemy's orders come from the state alone. Two states equal in every field give the same orders, and the
    // enemy unit acts (here it holds its hex, fortified, as its ratio is below the threshold: Q-ENEMY-2).
    [Fact]
    public void OrdersComeOnlyFromTheState()
    {
        var c = Load();
        GameState Make() => CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3));
        var a = Simulation.Step(c, Make(), Array.Empty<Command>()).State;
        var b = Simulation.Step(c, Make(), Array.Empty<Command>()).State;
        Assert.Equal(a.Hash(), b.Hash());
        Assert.Equal((Orders.Fortify, "2,0"), (a.Units[2].Order, a.Units[2].OrderTarget));
    }

    // R-ENEMY-001: an enemy order goes through the player's validation, with the sides swapped, and a refused one leaves the
    // state as it was. The locality rule is R-MOVE-005's mirror: no enemy unit enters a locality off enemy_held_localities.
    [Fact]
    public void AnInvalidEnemyOrderIsRefusedLikeThePlayers()
    {
        var c = Load();   // town_a, a player's locality here (enemy_held_localities is empty), is on (1, 0)
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3), Enemy(3, 2, 2, 1), Enemy(4, 2, 2, 1), Enemy(5, 5, 2, 1));
        var before = s.Hash();
        var spent = new Dictionary<int, int>();
        var why = new List<Cause>();
        Assert.Equal("That unit is not yours", Movement.Move(c, s, 1, 4, 0, spent, why, enemy: true));
        Assert.Equal("An enemy unit holds that hex", Movement.Move(c, s, 2, 3, 0, spent, why, enemy: true));
        Assert.Equal("Units may not enter an enemy-held locality before its capture is possible", Movement.Move(c, s, 2, 1, 0, spent, why, enemy: true));
        Assert.Equal("Units cannot enter water or leave the map", Movement.Move(c, s, 2, 2, -1, spent, why, enemy: true));
        Assert.Equal("That hex already holds 2 units of that side, the most one hex can hold", Movement.Move(c, s, 5, 2, 2, spent, why, enemy: true));
        Assert.Equal("That unit is not yours", Combat.Give(c, s, 1, 2, 0, why, enemy: true));
        Assert.Equal("That hex is not next to the unit", Combat.Give(c, s, 3, 3, 0, why, enemy: true));
        Assert.Equal("That unit is not yours", Orders.Give(c, s, 1, Orders.Fortify, why, enemy: true));
        Assert.Equal(before, s.Hash());
        Assert.Empty(why);
    }

    // R-ENEMY-001: an enemy attack pays no Arms (Q-ENEMY-4) and an enemy unit cannot be ordered by the player.
    [Fact]
    public void AnEnemyAttackPaysNoArms()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 3));
        s.Arms = 0;
        Assert.Null(Combat.Give(c, s, 2, 3, 0, new List<Cause>(), enemy: true));
        Assert.Equal((Combat.Attack, "3,0"), (s.Units[2].Order, s.Units[2].OrderTarget));
        var fought = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));   // the AI attacks at 2:1 in step 1
        var arms = fought.Arms;
        var next = Simulation.Step(c, fought, Array.Empty<Command>()).State;
        Assert.Equal(4, next.Units[1].Strength);   // the engagement happened
        Assert.Equal(arms, next.Arms);             // and the enemy paid nothing for it
        Assert.Equal("That unit is not yours", Assert.Single(Simulation.Step(c, s, new Command[] { new UnitOrder(2, Orders.Fortify) }).Rejected).Reason);
    }

    // R-ENEMY-003: with no script, the unit attacks the first adjacent hex with a player unit, in the fixed neighbour order,
    // where Combat.EnemyPreview's ratio reaches mvp.enemy_attack_ratio_percent; otherwise it fortifies its hex.
    [Theory]
    [InlineData(10, Combat.Attack, "3,0")]    // 10 against 5: 200%, the threshold
    [InlineData(9, Orders.Fortifying, "2,0")]   // 180%: below it
    public void TheGoalsAttackAtTheThresholdElseFortify(int strength, string order, string target)
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, strength));
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal((order, target), (s.Units[2].Order, s.Units[2].OrderTarget));
    }

    // R-ENEMY-003: each enemy is only 1:1 against the player unit, but an attack already standing on the target makes the
    // joining unit's preview 2:1. Step 2 will group those same-target attacks into one engagement.
    [Fact]
    public void AUnitJoinsAnAlreadyOrderedAttackWhenTogetherTheyReachTheThreshold()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 3, 1, 5), Enemy(3, 2, 0, 5));
        Assert.Null(Combat.Give(c, s, 3, 3, 0, new List<Cause>(), enemy: true));

        EnemyAI.Order(c, s, new List<Cause>());

        Assert.Equal((Combat.Attack, "3,0"), (s.Units[2].Order, s.Units[2].OrderTarget));
        Assert.Equal((Combat.Attack, "3,0"), (s.Units[3].Order, s.Units[3].OrderTarget));
    }

    // R-ENEMY-003: the same 1:1 unit has no qualifying attack without a same-target standing attacker, so it fortifies.
    [Fact]
    public void AUnitAloneBelowTheAttackThresholdFortifies()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 3, 1, 5));

        EnemyAI.Order(c, s, new List<Cause>());

        Assert.Equal((Orders.Fortifying, "3,1"), (s.Units[2].Order, s.Units[2].OrderTarget));
    }

    // R-ENEMY-003: the attack resolves in the same turn's step 2, and a unit already fortified keeps its order quietly.
    [Fact]
    public void TheEnemyAttackResolvesInStepTwo()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));
        var next = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Equal(4, next.Units[1].Strength);   // the fixture's 2:1 row: the defender loses 1 and retreats
        Assert.NotEqual((3, 0), (next.Units[1].Q, next.Units[1].R));
        var held = CombatTests.With(c, Enemy(2, 2, 0, 3));
        held = Simulation.Step(c, held, Array.Empty<Command>()).State;
        var why = new List<Cause>();
        EnemyAI.Order(c, held, why);
        Assert.Equal(Orders.Fortify, held.Units[2].Order);
        Assert.Empty(why);
    }

    // R-ENEMY-003: a standing attack is kept. Step 1 on the next turn finds the unit already attacking the same hex and leaves
    // the order as it is; giving it again would be refused, and the unit would fortify instead.
    [Fact]
    public void AStandingAttackIsKeptOnTheNextTurn()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));
        var why = new List<Cause>();
        EnemyAI.Order(c, s, why);
        Assert.Equal((Combat.Attack, "3,0"), (s.Units[2].Order, s.Units[2].OrderTarget));
        EnemyAI.Order(c, s, why);   // the next turn's step 1, the attack still standing
        Assert.Equal((Combat.Attack, "3,0"), (s.Units[2].Order, s.Units[2].OrderTarget));
    }

    // R-ENEMY-001: the enemy orders in ascending unit id. Two scripted units march on the same hex, which has one slot left
    // (the fixture's two units of a side a hex): unit 0 takes it, and unit 1's move is refused, so it fortifies where it is.
    [Fact]
    public void TheLowerIdTakesTheLastStackSlot()
    {
        string Row(string id) => "- id: " + id + "\n  service: army\n  side: egypt\n  status: draft\n  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Start, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1948-06-11' }\n"
            + "  - { place: June, lon: 34.29, lat: 33.35, from: '1948-06-11', to: '1948-07-08', sources: [{ url: 'https://example.org/a', checked: '2026-10-13' }, { url: 'https://example.org/b', checked: '2026-10-13' }] }\n";
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types,
            ["map/hexes.json"] = MovementTests.Board(),
            ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n" + Row("first") + Row("second"),
            ["map/unit_anchor_hexes.yaml"] = "anchors:\n  - { id: first, anchor: 0, q: 2, r: 2 }\n  - { id: first, anchor: 1, q: 3, r: 2 }\n"
                + "  - { id: second, anchor: 0, q: 4, r: 2 }\n  - { id: second, anchor: 1, q: 3, r: 2 }\n",
        })));
        var s = CombatTests.With(c, Enemy(2, 3, 2, 1));   // units 0 and 1 are the two rows; unit 2 already holds one slot
        Assert.Equal((2, 2), (s.Units[0].Q, s.Units[0].R));
        Assert.Equal((4, 2), (s.Units[1].Q, s.Units[1].R));
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal((3, 2), (s.Units[0].Q, s.Units[0].R));
        Assert.Equal((4, 2, Orders.Fortifying), (s.Units[1].Q, s.Units[1].R, s.Units[1].Order));
    }

    // Q-ENEMY-2: no RNG in the AI. Step 1 leaves RngState as it found it.
    [Fact]
    public void NoRngInTheAI()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10), Enemy(3, 4, 0, 3));
        var rng = s.RngState;
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal(rng, s.RngState);
        var real = Simulation.NewGame(Real, 1, Mode.Historical);
        rng = real.RngState;
        EnemyAI.Order(Real, real, new List<Cause>());
        Assert.Equal(rng, real.RngState);
    }

    // R-ENEMY-002: the script never invents a position. Each scripted target is an anchor of its row from 11 June 1948 that
    // cites at least two sources that are not Wikipedia or Wikidata, on that anchor's hex (unit_anchor_hexes.yaml), in the
    // file's order; the ALA has none. #1270's first slice: the Syrian bridgehead from 8 July 1948 to the Syrian armistice (S1).
    [Fact]
    public void TheScriptNeverInventsAPosition()
    {
        var scripted = Real.StartingForces.SelectMany(f => f.Script.Select(x => (f.Row, x.Q, x.R, x.Until)))
            .OrderBy(x => x.Row, StringComparer.Ordinal).ToList();
        var until = new DateTime(1948, 7, 8);   // the 11 June anchors' window end: the first truce's (units_1948.yaml)
        Assert.Equal(new[]
        {
            ("arab_legion", -3, 25, until),                     // Latrun
            ("egyptian_expeditionary_force", -8, 27, until),    // Isdud, the forward base (the row's first 11 June anchor)
            ("egyptian_expeditionary_force", -9, 28, until),    // Majdal, the rear base: listed second, so never the target
            ("iraqi_expeditionary_force", 6, 15, until),        // Jenin
            ("syrian_army", 15, 6, until),                      // the bridgehead at Mishmar HaYarden, next to Rosh Pinna's hex (owner, #1183)
            ("syrian_army", 15, 5, new DateTime(1949, 7, 20)),  // the bridgehead to the Syrian armistice (#1270, S1): the tool's free neighbour of Rosh Pinna's hex
        }, scripted);
        Assert.All(Real.StartingForces.Where(f => f.Side == Movement.PlayerSide), f => Assert.Empty(f.Script));
    }

    // R-ENEMY-002: an 11 June anchor on one non-Wikipedia source (with Wikipedia beside it) is not scripted; two are. One URL
    // listed twice is one source, and a Wikipedia host in capitals is still Wikipedia (#2804).
    [Theory]
    [InlineData("['https://example.org/a', 'https://en.wikipedia.org/wiki/Isdud', 'https://www.wikidata.org/wiki/Q1']", false)]
    [InlineData("['https://example.org/a', 'https://example.org/b']", true)]
    [InlineData("['https://example.org/a', 'https://example.org/a']", false)]
    [InlineData("['https://example.org/a', 'https://EN.WIKIPEDIA.ORG/wiki/Isdud']", false)]
    [InlineData("['https://example.org/a', 'https://www.WikiData.org/wiki/Q1']", false)]
    public void AScriptedAnchorNeedsTwoSources(string urls, bool scripted)
    {
        var sources = string.Join(", ", urls.Trim('[', ']').Split(", ").Select(u => "{ url: " + u + ", checked: '2026-10-13' }"));
        var row = "- id: force\n  service: army\n  side: egypt\n  status: draft\n  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Start, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1948-06-11' }\n"
            + "  - { place: June, lon: 34.29, lat: 33.35, from: '1948-06-11', to: '1948-07-08', sources: [" + sources + "] }\n";
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types,
            ["map/hexes.json"] = MovementTests.Board(),
            ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n" + row,
            ["map/unit_anchor_hexes.yaml"] = "anchors:\n  - { id: force, anchor: 0, q: 5, r: 0 }\n  - { id: force, anchor: 1, q: 3, r: 2 }\n",
        })));
        var f = Assert.Single(c.StartingForces);
        Assert.Equal(scripted ? new[] { (3, 2, new DateTime(1948, 7, 8)) } : Array.Empty<(int, int, DateTime)>(), f.Script);
    }

    // R-ENEMY-002, R-MOVE-001: under gdd-0.1 too, a scripted unit that has not moved this turn may enter one neighbouring
    // land hex whose cost exceeds its full allowance, spending all its points. Fixture: two points, adjacent desert costs 3.
    [Fact]
    public void AScriptedFirstStepMayCostMoreThanTheUnitsFullPoints()
    {
        const string SlowTypes = Fixture.GddUnitTypes
            + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
            + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 2 }\n";
        const string Two = "[{ url: 'https://example.org/a', checked: '2026-10-13' }, { url: 'https://example.org/b', checked: '2026-10-13' }]";
        var row = "- id: force\n  service: army\n  side: egypt\n  status: draft\n  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Start, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1948-06-11' }\n"
            + "  - { place: June, lon: 34.29, lat: 33.35, from: '1948-06-11', to: '1948-07-08', sources: " + Two + " }\n";
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = SlowTypes,
            ["map/hexes.json"] = MovementTests.Board((3, 1, "desert")),
            ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n" + row,
            ["map/unit_anchor_hexes.yaml"] = "anchors:\n  - { id: force, anchor: 0, q: 2, r: 1 }\n  - { id: force, anchor: 1, q: 3, r: 1 }\n",
        })));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var why = new List<Cause>();
        EnemyAI.Order(c, s, why);
        Assert.Equal((3, 1), (s.Units[0].Q, s.Units[0].R));
        Assert.Contains(why, x => x.Message == "Unit 0 moved to hex 3,1 for 2 movement points");
    }

    // R-ENEMY-002 past the first truce (#1270; owner, 31 October 2026, ruling a: "First listed (as today)"): the script is
    // every anchor from 11 June 1948 on two non-wiki sources, in the file's order, and on each turn the unit marches on the
    // first of them whose window has not ended. Fixture: a later anchor on one source is skipped; of two that overlap from
    // 8 July, the first listed is the target until it ends on 1 November, then the second until 24 February 1949; after
    // that the unit follows the goals (here it fortifies). Hexes and dates are fixture values (town_a, a player's
    // locality, is on 1,0).
    [Fact]
    public void TheScriptMarchesOnTheFirstListedAnchorWhoseWindowHasNotEnded()
    {
        const string Two = "[{ url: 'https://example.org/a', checked: '2026-10-13' }, { url: 'https://example.org/b', checked: '2026-10-13' }]";
        const string One = "[{ url: 'https://example.org/a', checked: '2026-10-13' }, { url: 'https://en.wikipedia.org/wiki/X', checked: '2026-10-13' }]";
        var row = "- id: force\n  service: army\n  side: egypt\n  status: draft\n  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Start, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1948-06-11' }\n"
            + "  - { place: June, lon: 34.29, lat: 33.35, from: '1948-06-11', to: '1948-07-08', sources: " + Two + " }\n"
            + "  - { place: OneSource, lon: 34.29, lat: 33.35, from: '1948-07-08', to: '1949-02-24', sources: " + One + " }\n"
            + "  - { place: Autumn, lon: 34.29, lat: 33.35, from: '1948-07-08', to: '1948-11-01', sources: " + Two + " }\n"
            + "  - { place: Armistice, lon: 34.29, lat: 33.35, from: '1948-07-08', to: '1949-02-24', sources: " + Two + " }\n";
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types,
            ["map/hexes.json"] = MovementTests.Board(),
            ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n" + row,
            ["map/unit_anchor_hexes.yaml"] = "anchors:\n  - { id: force, anchor: 0, q: 5, r: 0 }\n  - { id: force, anchor: 1, q: 3, r: 2 }\n"
                + "  - { id: force, anchor: 2, q: 5, r: 2 }\n  - { id: force, anchor: 3, q: 4, r: 1 }\n  - { id: force, anchor: 4, q: 2, r: 1 }\n",
        })));
        var f = Assert.Single(c.StartingForces);
        Assert.Equal(new[] { (3, 2, new DateTime(1948, 7, 8)), (4, 1, new DateTime(1948, 11, 1)), (2, 1, new DateTime(1949, 2, 24)) }, f.Script);

        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal((5, 0), (s.Units[0].Q, s.Units[0].R));
        s.Date = new DateTime(1948, 7, 15);
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal((4, 1), (s.Units[0].Q, s.Units[0].R));   // the first listed of the two, not the one-source anchor
        s.Date = new DateTime(1948, 11, 15);
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal((2, 1), (s.Units[0].Q, s.Units[0].R));   // the first has ended: the second
        s.Date = new DateTime(1949, 3, 15);
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal((2, 1, Orders.Fortifying), (s.Units[0].Q, s.Units[0].R, s.Units[0].Order));   // every window has ended
    }

    // R-ENEMY-002: in the real game each scripted unit marches on its 11 June hex along its cheapest path (round the player's
    // units and localities), as far as its movement points reach, on the turns that start before the anchor's window ends
    // (15 May and 15 June). With 3 points a turn each ends on its hex or next to it. On the turn of 15 July only the Syrian
    // unit has a script left (#1270, S1, to 20 July 1949): on the 4.4 km grid S1's point falls in Rosh Pinna's hex, a
    // player's locality, so the anchor tool put it on a free neighbour; the unit steps from its June hex onto that one,
    // and no other unit moves.
    [Fact]
    public void AScriptedUnitMarchesToItsJuneAnchorThenFollowsTheGoals()
    {
        var c = Real;
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var targets = c.StartingForces.Select((f, i) => (f, i)).Where(x => x.f.Script.Count > 0)
            .ToDictionary(x => x.i, x => (x.f.Script[0].Q, x.f.Script[0].R));
        Assert.Equal(4, targets.Count);
        var first = Simulation.Step(c, s, Array.Empty<Command>());
        foreach (var id in targets.Keys)
            Assert.Contains(first.Why, x => x.Message.StartsWith($"Unit {id} moved to hex", StringComparison.Ordinal));
        var june = Play(c, first.State, 1);
        foreach (var (id, hex) in targets.Select(t => (t.Key, t.Value)))
            Assert.True(Distance(june.Units[id].Q, june.Units[id].R, hex.Q, hex.R) <= 1, $"unit {id} is not at or next to its 11 June hex");
        Assert.Equal(new DateTime(1948, 7, 15), june.Date);
        var why = new List<Cause>();
        int syria = c.StartingForces.FindIndex(f => f.Row == "syrian_army");
        var positions = june.Units.Values.Where(u => u.Id != syria).Select(u => (u.Id, u.Q, u.R)).ToList();
        Assert.Equal(targets[syria], (june.Units[syria].Q, june.Units[syria].R));
        var s1 = (c.StartingForces[syria].Script[1].Q, c.StartingForces[syria].Script[1].R);
        Assert.Equal(1, Distance(targets[syria].Q, targets[syria].R, s1.Q, s1.R));
        EnemyAI.Order(c, june, why);
        Assert.Equal(positions, june.Units.Values.Where(u => u.Id != syria).Select(u => (u.Id, u.Q, u.R)).ToList());
        Assert.Equal(s1, (june.Units[syria].Q, june.Units[syria].R));
        Assert.Equal(new[] { $"Unit {syria} moved to hex" }, why.Where(x => x.Message.Contains("moved to hex", StringComparison.Ordinal))
            .Select(x => x.Message.Substring(0, x.Message.IndexOf("hex", StringComparison.Ordinal) + 3)));
    }

    // R-ENEMY-002: the script is Historical mode's only; another mode's units follow the goals from the first turn.
    [Fact]
    public void OnlyAHistoricalGameFollowsTheScript()
    {
        var s = Simulation.NewGame(Real, 1, Mode.Historical);
        s.Mode = Mode.Open;
        var positions = s.Units.Values.Select(u => (u.Id, u.Q, u.R)).ToList();
        EnemyAI.Order(Real, s, new List<Cause>());
        Assert.Equal(positions, s.Units.Values.Select(u => (u.Id, u.Q, u.R)).ToList());
    }

    // R-ENEMY-002: a scripted move that cannot be made (a player unit holds the 11 June hex) falls back to the goals.
    [Fact]
    public void ABlockedScriptFallsBackToTheGoals()
    {
        var c = Real;
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var legion = c.StartingForces.FindIndex(f => f.Row == "arab_legion");
        var latrun = (c.StartingForces[legion].Script[0].Q, c.StartingForces[legion].Script[0].R);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, StartingForces.IsraeliBrigadeType, latrun.Q, latrun.R, side: "israel", strength: 1));
        var before = (s.Units[legion].Q, s.Units[legion].R);
        var why = new List<Cause>();
        EnemyAI.Order(c, s, why);
        var u = s.Units[legion];
        Assert.Equal(before, (u.Q, u.R));
        Assert.Contains(u.Order, new[] { Orders.Fortifying, Combat.Attack });
    }

    // R-ENEMY-006: no enemy attack inside a truce window. In the reference run Harel (unit 5, by Sha'ar HaGai) stands next
    // to the Arab Legion at Latrun. The turn of 15 July 1948 runs to 15 August, its middle (30 July) inside the second truce,
    // so the Legion fortifies and Harel is still there on 15 August. Without the guard the Legion destroyed it at 4:1 on
    // that turn.
    [Fact]
    public void TheArabLegionDoesNotAttackHarelAtLatrunInTheSecondTruce()
    {
        var s = GoldenRun.Play(Real, new DateTime(1948, 7, 15));
        Assert.Equal(new DateTime(1948, 7, 15), s.Date);
        var legion = Real.StartingForces.FindIndex(f => f.Row == "arab_legion");
        var harel = Real.StartingForces.FindIndex(f => f.Row == "harel");
        Assert.Equal((-3, 25), (s.Units[legion].Q, s.Units[legion].R));   // the Legion on Latrun
        Assert.Equal(1, Distance(s.Units[legion].Q, s.Units[legion].R, s.Units[harel].Q, s.Units[harel].R));
        var strength = s.Units[harel].Strength;
        var step = Simulation.Step(Real, s, GoldenRun.Orders(Real, s));
        Assert.Equal(new DateTime(1948, 8, 15), step.State.Date);
        Assert.DoesNotContain(step.Why, x => x.Message.StartsWith($"Unit {legion} will attack", StringComparison.Ordinal));
        Assert.True(step.State.Units.ContainsKey(harel), "Harel was destroyed");
        Assert.Equal(strength, step.State.Units[harel].Strength);
        Assert.Equal(Orders.Fortify, step.State.Units[legion].Order);
    }

    // R-ENEMY-006: a standing attack ends on a truce turn even when the unit moves on its script (owner's repro, #1263). On
    // the 15 May 1948 turn Givati goes to -7,24 and the Negev to -10,27, and Egypt attacks the Negev (ids looked up by row,
    // #1241). On the 15 June turn Givati goes back to -5,23, Egypt marches on its script to -9,27, and without the guard its
    // standing attack destroyed the Negev at 900% inside the first truce. The golden seed: the Negev survives the first turn
    // at 2, its start before #1240 (Ben-Gurion's diary gives it 1, which the first turn's attack destroys), so it is set here.
    [Fact]
    public void AScriptedMoveDoesNotCarryAnAttackIntoTheFirstTruce()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(new DateTime(1948, 5, 15), s.Date);
        int givati = Real.StartingForces.FindIndex(f => f.Row == "givati");
        int negevId = Real.StartingForces.FindIndex(f => f.Row == "negev");
        int egypt = Real.StartingForces.FindIndex(f => f.Row == "egyptian_expeditionary_force");
        var negev = s.Units[negevId];
        Assert.Equal(1, negev.Strength);
        s.Units[negevId] = new Unit(negev.Id, negev.Type, negev.Q, negev.R, negev.Service, negev.Side, 2, negev.Order, negev.OrderTarget);
        var first = Simulation.Step(Real, s, new Command[] { new Move(givati, -8, 27), new Move(negevId, -11, 30) });
        Assert.Contains(first.Why, x => x.Message == $"Unit {egypt} will attack hex -11,30");
        s = first.State;
        Assert.Equal(new DateTime(1948, 6, 15), s.Date);
        Assert.True(s.Units.ContainsKey(negevId), "the Negev fell on the 15 May turn");
        Assert.Equal((Combat.Attack, "-11,30"), (s.Units[egypt].Order, s.Units[egypt].OrderTarget));   // the attack stands
        var strength = s.Units[negevId].Strength;
        var step = Simulation.Step(Real, s, new Command[] { new Move(givati, -6, 26) });
        Assert.Equal((-9, 29), (step.State.Units[egypt].Q, step.State.Units[egypt].R));   // Egypt moved on its script
        Assert.True(step.State.Units.ContainsKey(negevId), "the Negev was destroyed");
        Assert.Equal(strength, step.State.Units[negevId].Strength);
    }

    // R-ENEMY-006: the truce windows are the sourced ones, half-open: the first truce from 11 June to 8 July 1948 and the
    // second from 18 July to 15 October 1948. The Ten Days and Operation Yoav's first day are open to attacks. When `next`
    // is omitted the turn is read on its first day.
    [Theory]
    [InlineData(1948, 6, 10, false)]
    [InlineData(1948, 6, 11, true)]
    [InlineData(1948, 7, 7, true)]
    [InlineData(1948, 7, 8, false)]
    [InlineData(1948, 7, 17, false)]
    [InlineData(1948, 7, 18, true)]
    [InlineData(1948, 10, 14, true)]
    [InlineData(1948, 10, 15, false)]
    public void TheTruceWindowsAreTheSourcedOnes(int y, int m, int d, bool truce)
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));
        s.Date = new DateTime(y, m, d);
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal(truce ? (Orders.Fortifying, "2,0") : (Combat.Attack, "3,0"), (s.Units[2].Order, s.Units[2].OrderTarget));
    }

    // R-ENEMY-006: a turn is in a truce when its middle day is. The 15 May 1948 turn (middle 30 May) is war, though the first
    // truce starts before it ends; the 15 July turn (30 July) is the second truce, though the Ten Days are on when it starts;
    // the 15 October turn (30 October) is war again.
    [Theory]
    [InlineData(5, 6, false)]
    [InlineData(6, 7, true)]
    [InlineData(7, 8, true)]
    [InlineData(9, 10, true)]
    [InlineData(10, 11, false)]
    public void ATurnIsReadAtItsMiddle(int from, int to, bool truce)
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));
        s.Date = new DateTime(1948, from, 15);
        EnemyAI.Order(c, s, new List<Cause>(), new DateTime(1948, to, 15));
        Assert.Equal(truce ? Orders.Fortifying : Combat.Attack, s.Units[2].Order);
    }

    // R-ENEMY-006: "The guard remains date-driven in every mode" (#2806 item 11). The cases above all build a Historical game
    // (CombatTests.With); here the same game in Open mode, on both sides of the first truce's start. The day before it the
    // enemy still attacks, so Open mode alone does not stop the attack; on 11 June the truce does.
    [Theory]
    [InlineData(1948, 6, 10, false)]
    [InlineData(1948, 6, 11, true)]
    public void TheTruceHoldsInOpenModeToo(int y, int m, int d, bool truce)
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));
        s.Mode = Mode.Open;
        s.Date = new DateTime(y, m, d);
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal(truce ? (Orders.Fortifying, "2,0") : (Combat.Attack, "3,0"), (s.Units[2].Order, s.Units[2].OrderTarget));
    }

    // R-ENEMY-004: no reinforcements in Phase 5 (Q-ENEMY-3). No turn adds an enemy unit, and none grows: no enemy unit's
    // strength goes above its start.
    [Fact]
    public void NoReinforcements()
    {
        var s = Simulation.NewGame(Real, 1, Mode.Historical);
        var ids = s.Units.Values.Where(IsEnemy).Select(u => u.Id).ToHashSet();
        var start = s.Units.Values.Where(IsEnemy).ToDictionary(u => u.Id, u => u.Strength);
        for (int i = 0; i < 8 && s.GameOver == null; i++)
        {
            s = Simulation.Step(Real, s, Array.Empty<Command>()).State;
            Assert.Subset(ids, s.Units.Values.Where(IsEnemy).Select(u => u.Id).ToHashSet());   // xUnit: every id now is a starting one (B ⊆ A), so none arrived
            foreach (var u in s.Units.Values.Where(IsEnemy))
                Assert.True(u.Strength <= start[u.Id], $"enemy unit {u.Id} grew from {start[u.Id]} to {u.Strength}");
        }
    }

    // R-ENEMY-001, R-ENEMY-002: no enemy move ends on a locality off enemy_held_localities (the mirror of R-MOVE-005), so the
    // script creates no capture while enemy capture (Q-TGT-2 (c)) is unbuilt. A unit that never moved may still stand where it
    // started (Syria's start hex is Degania Alef's, R-CTRL-004).
    [Fact]
    public void NoEnemyMoveEndsOnAPlayersLocality()
    {
        var c = Real;
        var held = new HashSet<string>(c.Rules.Mvp!.EnemyHeldLocalities, StringComparer.Ordinal);
        var heldHexes = c.SettlementHexes.Where(h => held.Contains(h.Id)).Select(h => (h.Q, h.R)).ToHashSet();
        var players = c.SettlementHexes.Select(h => (h.Q, h.R)).Where(h => !heldHexes.Contains(h)).ToHashSet();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var start = s.Units.Values.ToDictionary(u => u.Id, u => (u.Q, u.R));
        for (int i = 0; i < 8 && s.GameOver == null; i++)
        {
            s = Simulation.Step(c, s, Array.Empty<Command>()).State;
            foreach (var u in s.Units.Values.Where(IsEnemy))
                if ((u.Q, u.R) != start[u.Id]) Assert.DoesNotContain((u.Q, u.R), players);
        }
        // R-ENEMY-007: with no player moves, the delegated operations postpone before their card orders can capture
        // Beersheba or Ramla. This test's enemy-movement assertion therefore leaves the player's settlement ledger empty.
        Assert.Empty(s.Settlements);
    }

    // R-ENEMY-001: what the enemy does is named on its cause (Cause.EnemyOrder), for the turn report (#1293): a scripted
    // move, an attack (on its engagement, #1297) and a fortify. The player's own orders, through the same checks, name none.
    [Fact]
    public void EachEnemyOrderIsNamedOnItsCause()
    {
        var c = Load();
        var fought = Simulation.Step(c, CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10)), Array.Empty<Command>());
        // #2534: the new order's own cause names it too, ahead of the engagement's.
        Assert.Equal(new[] { Combat.Attack, Combat.Attack }, fought.Why.Where(x => x.EnemyOrder != "").Select(x => x.EnemyOrder));
        var mine = Simulation.Step(c, CombatTests.With(c, Soldier(1, 3, 0, 10), Enemy(2, 2, 0, 1)), new Command[] { new UnitOrder(1, Combat.Attack, 2, 0) });
        Assert.Contains(mine.Why, x => x.Message.StartsWith("The attack on hex 2,0", StringComparison.Ordinal));
        Assert.DoesNotContain(mine.Why, x => x.EnemyOrder == Combat.Attack);
        var r = Simulation.Step(c, CombatTests.With(c, Soldier(1, 0, 0, 5), Enemy(2, 5, 2, 1)),
            new Command[] { new Move(1, 1, 0), new UnitOrder(1, Orders.Hold) });
        Assert.Equal(new[] { "", "", Orders.Fortify }, r.Why.Where(x => x.System == "map").Select(x => x.EnemyOrder).ToArray());
        var first = Simulation.Step(Real, Simulation.NewGame(Real, 1, Mode.Historical), Array.Empty<Command>());
        var scripted = Real.StartingForces.Select((f, i) => (f, i)).Where(x => x.f.Script.Count > 0).Select(x => x.i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(scripted, first.Why.Where(x => x.EnemyOrder == Movement.Kind).Select(x => x.Target));
    }

    // #1297: a standing attack the AI keeps (R-ENEMY-003) still engages every turn (R-COMBAT-002), so the turn report must
    // name it on each turn it engages, not only on the turn it is ordered. With no retreat at 2:1, unit 1 holds its hex
    // and unit 2's attack stands into the second turn.
    [Fact]
    public void AStandingEnemyAttackIsNamedOnEveryTurnItEngages()
    {
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
        });
        files["rules.yaml"] = files["rules.yaml"].Replace("defender_retreats: [1, 1]", "defender_retreats: [0, 0]");
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10));
        for (int turn = 0; turn < 2; turn++)
        {
            var r = Simulation.Step(c, s, Array.Empty<Command>());
            Assert.Equal((Combat.Attack, "3,0"), (r.State.Units[2].Order, r.State.Units[2].OrderTarget));
            Assert.Equal(4 - turn, r.State.Units[1].Strength);   // it engaged this turn
            var attack = Assert.Single(r.Why, x => x.EnemyOrder == Combat.Attack && x.Key == WhyKeys.Engagement);   // #2534: the order's own cause is marked too
            Assert.Equal("3,0", attack.Target);
            Assert.Equal(new[] { 2 }, attack.EnemyAttackers);
            s = r.State;
        }
    }

    // #2534: a newly issued enemy attack order names itself on its own cause, so the turn report's enemy-orders row can
    // show it (the engagement names its attackers only when the attack engages, #1297).
    [Fact]
    public void ANewEnemyAttackOrderNamesItselfForTheReport()
    {
        var c = Load();
        var r = Simulation.Step(c, CombatTests.With(c, Soldier(1, 3, 0, 5), Enemy(2, 2, 0, 10)), Array.Empty<Command>());
        var order = Assert.Single(r.Why, x => x.Key == WhyKeys.AttackOrdered && x.EnemyOrder == Combat.Attack);
        Assert.Equal(new[] { 2 }, order.EnemyAttackers);
        Assert.Equal("2", order.Target);
    }
    // R-ENEMY-005 (Q-ENEMY-5; owner, 31 October 2026, #749): an operation card's option gives a go-to order to a unit of a
    // units_1948.yaml row, toward one of that row's own anchor hexes, through the player's own go-to (Movement.GiveGoTo).
    // The loader refuses a row it does not know, and a hex that is not one of the row's anchors; a card's
    // order that breaks a movement rule is refused like the player's and changes nothing. Fixture values: one brigade row
    // starting on (0, 0), with a second anchor on (4, 0).
    [Fact]
    public void OperationCardOrdersAreValidated()
    {
        const string Rows = "units:\n- id: brig\n  service: army\n  side: israel\n  status: draft\n"
            + "  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Here, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n"
            + "  - { place: There, lon: 34.30, lat: 33.35, from: '1948-06-01', to: '1949-01-01' }\n";
        const string Hexes = "anchors:\n  - { id: brig, anchor: 0, q: 0, r: 0 }\n  - { id: brig, anchor: 1, q: 4, r: 0 }\n";
        static string Card(string order) => "id: probe\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n"
            + "  - id: ok\n    text: Accept\n    historical: true\n    requires_unit_regions: [fixture_region]\n    unit_orders: [" + order + "]\n"
            + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n";
        static Dictionary<string, string> Files() => Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = Rows, ["map/unit_anchor_hexes.yaml"] = Hexes,
            ["regions.yaml"] = OperationRegions, ["map/locality_hexes.yaml"] = "localities: []\n",
        });
        static ContentException Refused(Dictionary<string, string> files, string order) =>
            Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files, new() { ["probe.yaml"] = Card(order) })));

        var unknown = Refused(Files(), "{ unit: nobody, q: 4, r: 0 }");
        Assert.Equal(("events/probe.yaml", "events[probe].options[ok].unit_orders[0].unit"), (unknown.File, unknown.Field));
        // R-ENEMY-007 (#2621): an objective, exchange or Deport card skips the placement check when it is delegated, so it
        // cannot carry unit orders at all; the refusal names the option's unit_orders.
        foreach (var single in new[] { "prisoners: egypt", "objective: fixture_goal", "deported: fixture_place" })
        {
            var special = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Files(), new()
            {
                ["probe.yaml"] = Card("{ unit: brig, q: 4, r: 0 }").Replace("from: 1948-05-15\n", "from: 1948-05-15\n" + single + "\n")
            })));
            Assert.Equal(("events/probe.yaml", "events[probe].options[ok].unit_orders"), (special.File, special.Field));
            Assert.Contains("cannot give unit_orders", special.Message);
        }
        var invented = Refused(Files(), "{ unit: brig, q: 3, r: 0 }");
        Assert.Equal("events[probe].options[ok].unit_orders[0]", invented.Field);
        Assert.Contains("not an anchor", invented.Message);
        var missingQ = Refused(Files(), "{ unit: brig, r: 0 }");
        Assert.Equal("events[probe].options[ok].unit_orders[0].q", missingQ.Field);
        var missingR = Refused(Files(), "{ unit: brig, q: 0 }");
        Assert.Equal("events[probe].options[ok].unit_orders[0].r", missingR.Field);
        var legacyMissingQ = Assert.Throws<ContentException>(() => ContentLoader.Load(
            Fixture.Write(null, new() { ["probe.yaml"] = Card("{ unit: brig, r: 0 }") })));
        Assert.Equal("events[probe].options[ok].unit_orders[0].q", legacyMissingQ.Field);
        // R-ENEMY-005: every order names a unit row, in every ruleset (the schema requires unit, q and r).
        var missingUnit = Refused(Files(), "{ q: 4, r: 0 }");
        Assert.Equal("events[probe].options[ok].unit_orders[0].unit", missingUnit.Field);
        Assert.EndsWith(": required", missingUnit.Message);
        var legacyMissingUnit = Assert.Throws<ContentException>(() => ContentLoader.Load(
            Fixture.Write(null, new() { ["probe.yaml"] = Card("{ q: 4, r: 0 }") })));
        Assert.Equal("events[probe].options[ok].unit_orders[0].unit", legacyMissingUnit.Field);
        var missing = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Files(), new()
        {
            ["probe.yaml"] = Card("{ unit: brig, q: 4, r: 0 }").Replace("    requires_unit_regions: [fixture_region]\n", "")
        })));
        Assert.Equal(("events/probe.yaml", "events[probe].options[ok].requires_unit_regions"), (missing.File, missing.Field));
        var unknownRegion = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Files(), new()
        {
            ["probe.yaml"] = Card("{ unit: brig, q: 4, r: 0 }").Replace("fixture_region]", "missing_region]")
        })));
        Assert.Equal("events[probe].options[ok].requires_unit_regions[0]", unknownRegion.Field);
        // The legacy loop reads the shared cards too: the order does not stop it loading, and it has no units to order.
        Assert.Empty(ContentLoader.Load(Fixture.Write(null, new() { ["probe.yaml"] = Card("{ unit: brig, q: 4, r: 0 }") })).StartingForces);

        var c = ContentLoader.Load(Fixture.Write(Files(), new() { ["probe.yaml"] = Card("{ unit: brig, q: 4, r: 0 }") }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(("brig", (0, 0)), (c.StartingForces[0].Row, (s.Units[0].Q, s.Units[0].R)));
        Assert.Contains("probe", s.PendingEvents);
        // The card's order is the player's go-to, with the movement points the turn's orders have already spent.
        var card = Simulation.Preview(c, s, new Command[] { new Move(0, 1, 0), new ChooseEventOption("probe", "ok") });
        var mine = Simulation.Preview(c, s, new Command[] { new Move(0, 1, 0), new UnitOrder(0, Movement.GoTo, 4, 0) });
        Assert.Empty(card.Rejected);
        Assert.Equal((Movement.GoTo, "4,0", 3, 0), (card.State.Units[0].Order, card.State.Units[0].OrderTarget, card.State.Units[0].Q, card.State.Units[0].R));
        Assert.Equal((mine.State.Units[0].Q, mine.State.Units[0].R, mine.State.Units[0].Order), (card.State.Units[0].Q, card.State.Units[0].R, card.State.Units[0].Order));
        // Left to the cabinet at End turn, the card gives the same order.
        var left = Simulation.Step(c, s, Array.Empty<Command>()).State.Units[0];
        Assert.Equal((Movement.GoTo, "4,0"), (left.Order, left.OrderTarget));
        // An enemy unit holds the anchor: the player's go-to is refused, and the card's order changes nothing (and is reported,
        // ARefusedCardOrderIsReportedLikeThePlayersOwn).
        s.Units.Add(9, Enemy(9, 4, 0, 1));
        var held = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok") });
        Assert.Equal(Orders.RefusedCardOrder, Assert.Single(held.Rejected).Reason);
        Assert.Equal(("", 0, 0), (held.State.Units[0].Order, held.State.Units[0].Q, held.State.Units[0].R));
        Assert.Equal("An enemy unit holds that hex", Assert.Single(Simulation.Preview(c, s, new Command[] { new UnitOrder(0, Movement.GoTo, 4, 0) }).Rejected).Reason);
    }

    // R-ENEMY-005 (#2806 item 10): "the legacy loop has no units and ignores them". In the legacy loop, choosing the probe
    // card's option with its unit order gives the state the same option with no order gives: the same hash, no card order
    // and nothing refused. Fixture values.
    [Fact]
    public void TheLegacyLoopIgnoresACardsUnitOrders()
    {
        static Content Legacy(string orders) => ContentLoader.Load(Fixture.Write(null, new()
        {
            ["probe.yaml"] = "id: probe\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n"
                + "  - id: ok\n    text: Accept\n    historical: true\n" + orders
                + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n",
        }));
        var withOrders = Legacy("    unit_orders: [{ unit: brig, q: 4, r: 0 }]\n");
        var without = Legacy("");
        Assert.Equal(SaveGame.LegacyRuleset, withOrders.Ruleset);
        Assert.Single(withOrders.Events.Single(e => e.Id == "probe").Options.Single().UnitOrders);

        GameState Chosen(Content c, out IReadOnlyList<Rejection> rejected)
        {
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            Assert.Empty(s.Units);   // the legacy loop has no units
            Assert.Contains("probe", s.PendingEvents);
            var r = Simulation.Step(c, s, new Command[] { new ChooseEventOption("probe", "ok") });
            rejected = r.Rejected;
            return r.State;
        }
        var ordered = Chosen(withOrders, out var refused);
        var plain = Chosen(without, out _);
        Assert.Empty(refused);
        Assert.Empty(ordered.CardOrders);
        Assert.Equal(plain.Hash(), ordered.Hash());
    }

    // R-ENEMY-005 (owner, 31 October 2026, #749: "Sourced anchors only"; 1 November 2026, #2087: "military operations must
    // have unit orders", "Kiryati to Ramle", Harel toward Latrun; 2 November 2026, option 1: the new ones are order targets,
    // not anchors): only these sourced places receive orders. Every other alternative option gives none until sources are found.
    [Fact]
    public void OnlyTheSourcedAnchorsReceiveCardOrders()
    {
        var orders = Real.Events.SelectMany(e => e.Options.SelectMany(o => o.UnitOrders.Select(x => (e.Id, o.Id, o.Historical, x.Unit, x.Q, x.R))));
        Assert.Equal(new[]
        {
            ("latrun_proposal", "press_offensive", false, "harel", -3, 25),        // Latrun police fort, harel order_targets[0]
            ("operation_hiram", "stop_at_ceasefire", true, "carmeli", 15, 3),      // Manara, carmeli order_targets[0]
            ("operation_hiram", "stop_at_ceasefire", true, "brigade_7", 14, 4),    // al-Malikiyya, brigade_7 order_targets[0]
            ("operation_horev", "launch", true, "golani", -8, 28),                 // Julis camp, golani anchors[1]
            ("operation_yoav", "stop_at_ceasefire", true, "negev", -10, 35),       // Beersheba, negev order_targets[0]
            ("operation_yoav", "stop_at_ceasefire", true, "givati", -9, 29),       // Huleiqat, givati order_targets[0]
            ("ten_days", "central_front", true, "kiryati", -3, 24),                // Ramle, kiryati order_targets[0]
            ("ten_days", "central_front", true, "carmeli", 14, 6),                 // facing the Syrian bridgehead, carmeli anchors[1]
        }, orders.OrderBy(x => x.Item1, StringComparer.Ordinal));
    }

    // R-ENEMY-007: the automated players stage an operation's units as a player's ordinary go-to, onto a hex of an allowed
    // region, but never onto an enemy-held place (Movement.NoEntry): staging is not an attack, and Acre (8, 7), a Galilee
    // member place, must not be captured as a side effect of putting Carmeli in Galilee. Played over the historical run to
    // the end of 1948. Red without the filter in HistoricalAI.OperationMoves: a go-to targets an enemy-held place.
    [Fact]
    public void StagingNeverTargetsAnEnemyHeldPlace()
    {
        var c = Real;
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var staged = 0;
        while (s.Date < new DateTime(1949, 1, 1) && s.GameOver == null)
        {
            var closed = Movement.NoEntry(c, s);
            foreach (var o in HistoricalAI.OperationMoves(c, s, e => e.Options.FirstOrDefault(x => x.Historical) ?? e.Options[0]).OfType<UnitOrder>())
            {
                staged++;
                Assert.True(o.Q.HasValue && o.R.HasValue, "a staging order is a go-to to a hex");
                Assert.False(closed.Contains((o.Q!.Value, o.R!.Value)), FormattableString.Invariant($"{s.Date:yyyy-MM-dd}: unit {o.Unit} staged onto the enemy-held hex ({o.Q}, {o.R})"));
            }
            s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
        }
        Assert.True(staged > 0, "the run stages at least one unit");
    }

    // R-ENEMY-007: every region an operation names has an English name in the sim's cause text (EventSystem's
    // OperationRegionName), so a region added to an operation but not to the names shows as its raw id and fails here.
    // Unity's operation.region.* strings are checked in EventCardTests.
    [Fact]
    public void EveryRequiredOperationRegionHasAnEnglishName()
    {
        var ids = Real.Events.SelectMany(e => e.Options).SelectMany(o => o.RequiresUnitRegions).Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.NotEmpty(ids);
        Assert.All(ids, id => Assert.True(Real.PlayerRegions.Regions.ContainsKey(id), id + " is a regions.yaml region"));
        Assert.All(ids, id => Assert.NotEqual(id, EventSystem.OperationRegionName(id)));
    }

    [Fact]
    public void EveryMilitaryOperationOptionHasTheRecordedRegionRequirement()
    {
        var requirements = Real.Events.SelectMany(e => e.Options.Where(o => o.RequiresUnitRegions.Count != 0)
            .Select(o => (Event: e.Id, Option: o.Id, Units: string.Join(",", o.UnitOrders.Select(x => x.Unit)), Regions: string.Join(",", o.RequiresUnitRegions))));
        Assert.Equal(new[]
        {
            ("latrun_proposal", "press_offensive", "harel", "central_plain,jerusalem_hills"),
            ("operation_hiram", "stop_at_ceasefire", "carmeli,brigade_7", "galilee"),
            ("operation_horev", "launch", "golani", "southern_coast,negev"),
            ("operation_yoav", "stop_at_ceasefire", "negev,givati", "southern_coast,negev"),
            ("ten_days", "central_front", "kiryati,carmeli", "central_plain,jerusalem_hills,galilee"),
        }, requirements.OrderBy(x => x.Event, StringComparer.Ordinal));
    }

    static Unit At(Unit u, int q, int r) => new Unit(u.Id, u.Type, q, r, u.Service, u.Side, u.Strength,
        u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp);

    static void PutOperationInPlace(Content c, GameState s, EventOption option)
    {
        var at = c.PlayerRegions.Regions[option.RequiresUnitRegions[0]].MemberHexes[0];
        foreach (var row in option.UnitOrders.Select(x => x.Unit).Distinct(StringComparer.Ordinal))
        {
            int id = c.StartingForces.FindIndex(x => x.Row == row);
            var force = c.StartingForces[id];
            var unit = s.Units.TryGetValue(id, out var existing) ? existing
                : new Unit(id, force.Type, force.Q, force.R, side: force.Side, strength: force.Strength);
            s.Units[id] = At(unit, at.Q, at.R);
        }
    }

    // R-ENEMY-007: every one of the five operation options refuses before mutation if one ordered unit is outside every
    // allowed region, and resolves once all ordered units are in any allowed region.
    [Theory]
    [InlineData("ten_days", "central_front")]
    [InlineData("operation_yoav", "stop_at_ceasefire")]
    [InlineData("operation_hiram", "stop_at_ceasefire")]
    [InlineData("operation_horev", "launch")]
    [InlineData("latrun_proposal", "press_offensive")]
    public void AnOperationRefusesOutOfPlaceAndAcceptsAllInPlace(string card, string choice)
    {
        foreach (var c in new[] { Real, Gdd03EventRulesTests.Real(true) })
        {
            var e = c.Events.First(x => x.Id == card); var option = e.Options.First(x => x.Id == choice);
            var s = Simulation.NewGame(c, 17, Mode.Historical);
            s.PendingEvents.Clear(); s.PendingEvents.Add(card); PutOperationInPlace(c, s, option);
            var outside = c.PlayerRegions.Regions.Values.SelectMany(x => x.MemberHexes)
                .First(h => !option.RequiresUnitRegions.Contains(c.PlayerRegions.AtHex(h.Q, h.R)!.Id, StringComparer.Ordinal));
            int first = c.StartingForces.FindIndex(x => x.Row == option.UnitOrders[0].Unit);
            s.Units[first] = At(s.Units[first], outside.Q, outside.R);
            var before = s.Hash(); var why = new List<Cause>();
            Assert.Equal(EventSystem.RefusedOperationUnitsNotInPlace, EventSystem.Choose(c, s, card, choice, why));
            Assert.Equal(before, s.Hash()); Assert.Empty(why); Assert.Contains(card, s.PendingEvents); Assert.False(s.ChosenOptions.ContainsKey(card));

            PutOperationInPlace(c, s, option);
            Assert.Null(EventSystem.Choose(c, s, card, choice, why));
            Assert.Equal(choice, s.ChosenOptions[card]); Assert.DoesNotContain(card, s.PendingEvents);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADelegatedOperationWithNoPlayerMovesPostponesThenLapsesWithoutAChoice(bool modern)
    {
        var c = modern ? Gdd03EventRulesTests.Real(true) : Real;
        var e = c.Events.First(x => x.Id == "operation_horev");
        var s = Simulation.NewGame(c, 23, Mode.Historical);
        s.Date = e.From; s.PendingEvents.Clear(); s.PendingEvents.Add(e.Id);
        var before = s.Hash(); var rng = s.RngState; var why = new List<Cause>();
        EventSystem.Delegate(c, s, why);
        Assert.Contains(e.Id, s.PendingEvents); Assert.False(s.ChosenOptions.ContainsKey(e.Id)); Assert.Equal(rng, s.RngState);
        // R-ENEMY-007 (owner, 7 November 2026: "Keep the option (Recommended)"): the postponement records only the kept option
        // and the turn its wait counts from; nothing else in the state changes.
        var kept = Assert.Single(s.PostponedOperations);
        Assert.Equal((e.Id, "launch", s.Turn), (kept.Key, kept.Value.Option, kept.Value.Turn));
        var unrecorded = s.Clone(); unrecorded.PostponedOperations.Clear();
        Assert.Equal(before, unrecorded.Hash());
        var postponed = Assert.Single(why, x => x.Key == CauseKeys.OperationPostponed);
        Assert.Equal(new[] { "golani", "negev|southern_coast" }, postponed.Args.Skip(2));
        Assert.Contains("Golani Brigade", postponed.Message); Assert.Contains("Negev", postponed.Message);
        Assert.DoesNotContain("golani", postponed.Message); Assert.DoesNotContain("southern_coast", postponed.Message);

        // "Wait, with a limit (Recommended)", "6 turns (Recommended)": the window's end no longer ends a postponed card; it is
        // shown again on mvp.operation_wait_turns turns after the first postponement, and lapses at the end of the last.
        var wait = c.Rules.Mvp!.OperationWaitTurns!.Value;
        Assert.Equal(6, wait);
        s.Date = e.To!.Value; s.Turn = kept.Value.Turn + wait; why.Clear(); EventSystem.LapseExpiredOperations(c, s, why);
        Assert.Contains(e.Id, s.PendingEvents); Assert.Empty(why);
        s.Turn++; EventSystem.LapseExpiredOperations(c, s, why);
        Assert.DoesNotContain(e.Id, s.PendingEvents); Assert.False(s.ChosenOptions.ContainsKey(e.Id)); Assert.Empty(s.PostponedOperations);
        var lapsed = Assert.Single(why);
        Assert.Equal(CauseKeys.OperationLapsed, lapsed.Key);
        Assert.Contains("Golani Brigade", lapsed.Message); Assert.Contains("Southern Coast", lapsed.Message);
    }

    // R-ENEMY-007 (the owner's words as recorded on #2543, 4 November 2026: "Postpone until they are in the right area and
    // if the operation is still relevant"): an ordinary card whose window lies wholly inside the turn just played still
    // late-fires at the turn's end, but an operation card does not first appear once its window has closed. Fixture dates:
    // the turn runs 15 May to 15 June 1948, and both windows run from 20 May to 1 June. With the operation's window still
    // open at the turn's end (to 20 June), it comes as usual.
    [Theory]
    [InlineData("1948-06-01", false)]
    [InlineData("1948-06-20", true)]
    public void AnOperationWhoseWindowClosedInsideTheTurnDoesNotFirstAppear(string to, bool fires)
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        var probe = c.Events.Single(e => e.Id == "probe");
        probe.From = new DateTime(1948, 5, 20); probe.To = DateTime.ParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        c.Events.Add(new GameEvent { Id = "ordinary", Title = "Ordinary", Text = "Ordinary.", From = probe.From, To = new DateTime(1948, 6, 1),
            Options = { new EventOption { Id = "ok", Text = "Ok" } } });
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.PendingEvents.Clear(); s.FiredEvents.Clear();
        s.Date = new DateTime(1948, 6, 15);
        EventSystem.Trigger(c, s, new List<Cause>(), new DateTime(1948, 5, 15));
        Assert.Contains("ordinary", s.PendingEvents);
        Assert.Equal(fires, s.PendingEvents.Contains("probe"));
    }

    // R-ENEMY-007, the owner's rulings of 7 November 2026 ("Wait, with a limit (Recommended)", "6 turns (Recommended)",
    // "Keep the option (Recommended)", "Lapse at once (Recommended)", "Signing wins (Recommended)"). The probe card's window is
    // the game's first day only, so it closes inside the first turn, as a real operation's does, and the brigade starts out of
    // its region on (2, 0). Fixture values, not history.
    static (Content C, GameState S) Waiting(string cardLines = "")
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board(), cardLines: cardLines);
        var probe = c.Events.Single(x => x.Id == "probe");
        probe.From = c.Rules.Start; probe.To = c.Rules.Start.AddDays(1);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Contains("probe", s.PendingEvents);
        s.Units[0] = At(s.Units[0], 2, 0);   // outside fixture_region
        return (c, s);
    }

    // Postponed on its first turn, the card no longer lapses at its window's end: it waits, and launches on the turn its
    // brigade is back in the region. Red with the old lapse (at the window's end): the card is gone after the first turn.
    [Fact]
    public void APostponedOperationWaitsPastItsWindowAndLaunchesWhenItsUnitArrives()
    {
        var (c, s) = Waiting();
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Contains(r.Why, x => x.Key == CauseKeys.OperationPostponed);
        Assert.DoesNotContain(r.Why, x => x.Key == CauseKeys.OperationLapsed);
        Assert.Contains("probe", r.State.PendingEvents);
        Assert.True(r.State.Date >= c.Events.Single(x => x.Id == "probe").To, "the window has closed");
        var back = r.State.Clone();
        back.Units[0] = At(back.Units[0], 0, 0);   // in fixture_region, as a player's move would have put it
        var next = Simulation.Step(c, back, Array.Empty<Command>());
        Assert.Equal("ok", next.State.ChosenOptions["probe"]);
        Assert.Empty(next.State.PostponedOperations);
    }

    // The wait's limit: the card is shown again on mvp.operation_wait_turns (6) turns after its first postponement, and lapses
    // at the end of the last. Red with the old lapse: it is gone after the first turn.
    [Fact]
    public void APostponedOperationLapsesAfterItsWaitTurns()
    {
        var (c, s) = Waiting();
        var wait = c.Rules.Mvp!.OperationWaitTurns!.Value;
        for (int turn = 1; turn <= wait + 1; turn++)
        {
            var r = Simulation.Step(c, s, Array.Empty<Command>());
            s = r.State;
            if (turn <= wait) Assert.Contains("probe", s.PendingEvents);
            else Assert.Contains(r.Why, x => x.Key == CauseKeys.OperationLapsed && x.Target == "probe");
        }
        Assert.DoesNotContain("probe", s.PendingEvents);
        Assert.False(s.ChosenOptions.ContainsKey("probe"));
        Assert.Empty(s.PostponedOperations);
    }

    // "Lapse at once (Recommended)": a postponed card whose required unit is no longer on the board lapses on its next
    // consideration with its own reason, instead of waiting out its turns.
    [Fact]
    public void APostponedOperationLapsesAtOnceWhenItsUnitIsGone()
    {
        var (c, s) = Waiting();
        s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Contains("probe", s.PendingEvents);
        s.Units.Remove(0);   // the brigade is destroyed
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        var lapsed = Assert.Single(r.Why, x => x.Target == "probe" && x.Key.StartsWith("operation_lapsed", StringComparison.Ordinal));
        Assert.Equal(CauseKeys.OperationLapsedUnitLost, lapsed.Key);
        Assert.DoesNotContain("probe", r.State.PendingEvents);
    }

    // "Signing wins (Recommended)": a pending operation whose armistice_front has signed lapses with its own reason.
    [Fact]
    public void AnOperationLapsesOnceItsFrontsArmisticeIsSigned()
    {
        var (c, s) = Waiting("armistice_front: egypt\n");
        s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Contains("probe", s.PendingEvents);
        s.ChosenOptions["armistice_egypt"] = "sign";
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        var lapsed = Assert.Single(r.Why, x => x.Target == "probe" && x.Key.StartsWith("operation_lapsed", StringComparison.Ordinal));
        Assert.Equal(CauseKeys.OperationLapsedSigned, lapsed.Key);
        Assert.Contains("Egypt", lapsed.Message);
        Assert.DoesNotContain("probe", r.State.PendingEvents);
    }

    // "Keep the option (Recommended)": a postponed card keeps the option it was postponed with. Open mode, with a free option the
    // minister would usually draw (recommended): over many seeds the card stays postponed with "ok" and is never resolved
    // with "free". Red without the kept option (a fresh draw on each consideration): some seeds resolve it with "free".
    [Fact]
    public void APostponedOperationKeepsItsOptionInsteadOfDrawingAgain()
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        c.Events.Single(x => x.Id == "probe").Options.Add(new EventOption { Id = "free", Text = "Free", Recommended = true });
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var s = Simulation.NewGame(c, seed, Mode.Historical); s.Mode = Mode.Open;
            s.Units[0] = At(s.Units[0], 2, 0);
            s.PostponedOperations["probe"] = new PostponedOperation("ok", s.Turn);
            var why = new List<Cause>();
            EventSystem.Delegate(c, s, why);
            Assert.False(s.ChosenOptions.ContainsKey("probe"), $"seed {seed}: resolved with {(s.ChosenOptions.TryGetValue("probe", out var o) ? o : "")}");
            Assert.Equal("ok", s.PostponedOperations["probe"].Option);
        }
    }

    // Latrun's ruling ("Takes the vote, wait ends (Recommended)"): a postponed card whose historical option needs no units is
    // decided by its minister with that option. Red without that rule: the kept operation keeps it waiting.
    [Fact]
    public void APostponedCardWhoseHistoricalOptionNeedsNoUnitsTakesIt()
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        var probe = c.Events.Single(x => x.Id == "probe");
        probe.Options[0].Historical = false;
        probe.Options.Add(new EventOption { Id = "vote", Text = "Vote", Historical = true });
        var s = Simulation.NewGame(c, 1, Mode.Historical); s.Mode = Mode.Open;
        s.Units[0] = At(s.Units[0], 2, 0);
        s.PostponedOperations["probe"] = new PostponedOperation("ok", s.Turn);
        EventSystem.Delegate(c, s, new List<Cause>());
        Assert.Equal("vote", s.ChosenOptions["probe"]);
        Assert.Empty(s.PostponedOperations);
    }

    [Fact]
    public void APostponedDelegationRollsBackItsDrawBeforeLaterCards()
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        c.Events.Add(new GameEvent { Id = "later", Title = "Later", Text = "Later.", From = c.Rules.Start,
            Options = { new EventOption { Id = "a", Text = "A" }, new EventOption { Id = "b", Text = "B" } } });
        var postponed = Simulation.NewGame(c, 91, Mode.Historical); postponed.Mode = Mode.Open;
        postponed.Units[0] = At(postponed.Units[0], 2, 0);   // outside fixture_region
        postponed.PendingEvents.Clear(); postponed.PendingEvents.Add("probe"); postponed.PendingEvents.Add("later");
        var control = postponed.Clone(); control.PendingEvents.Remove("probe");
        EventSystem.Delegate(c, postponed, new List<Cause>()); EventSystem.Delegate(c, control, new List<Cause>());
        Assert.Contains("probe", postponed.PendingEvents);
        Assert.Equal(control.ChosenOptions["later"], postponed.ChosenOptions["later"]);
        Assert.Equal(control.RngState, postponed.RngState);
    }

    // R-ENEMY-005, the owner's rulings of 1 November 2026 on #2087: each operation card's option orders the named unit, on the
    // board, to the named hex, until its order target's `to`. The reference game (GoldenRun) is played to the card's turn; an enemy
    // unit standing on the hex is taken off first (the ALA on al-Malikiyya), as that refusal is pinned by
    // WarEvents1948Tests.NoTurnBeforeTheArmisticeRefusesAnOrder and ARefusedCardOrderIsReportedLikeThePlayersOwn.
    [Theory]
    [InlineData("ten_days", "central_front", "1948-07-15", "kiryati", -3, 24, "1948-08-16")]
    [InlineData("operation_yoav", "stop_at_ceasefire", "1948-10-15", "negev", -10, 35, "1949-02-24")]
    [InlineData("operation_yoav", "stop_at_ceasefire", "1948-10-15", "givati", -9, 29, "1948-11-01")]
    [InlineData("operation_hiram", "stop_at_ceasefire", "1948-11-15", "carmeli", 15, 3, "1949-03-23")]
    [InlineData("operation_hiram", "stop_at_ceasefire", "1948-11-15", "brigade_7", 14, 4, "1949-03-23")]
    [InlineData("latrun_proposal", "press_offensive", "1948-09-15", "harel", -3, 25, "1948-12-22")]
    public void AnOperationCardOrdersItsUnitToItsHex(string card, string option, string turn, string row, int q, int r, string until)
    {
        static DateTime Day(string d) => DateTime.Parse(d, System.Globalization.CultureInfo.InvariantCulture);
        var s = GoldenRun.Play(Real, Day(turn));
        Assert.Equal(Day(turn), s.Date);
        int id = Real.StartingForces.FindIndex(f => f.Row == row);
        Assert.True(s.Units.ContainsKey(id), row);
        PutOperationInPlace(Real, s, Real.Events.First(e => e.Id == card).Options.First(o => o.Id == option));
        foreach (var held in s.Units.Values.Where(u => u.Side != Movement.PlayerSide && (u.Q, u.R) == (q, r)).Select(u => u.Id).ToList())
            s.Units.Remove(held);
        if (!s.PendingEvents.Contains(card)) s.PendingEvents.Add(card);
        var rejected = new List<Rejection>();
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(Real, s, card, option, why, rejected: rejected));
        Assert.DoesNotContain(rejected, x => x.Command is UnitOrder o && o.Unit == id);   // Hiram's other order may be refused
        var hex = FormattableString.Invariant($"{q},{r}");
        Assert.Contains(why, x => x.Key == WhyKeys.OnItsWay && x.Args.Contains(hex));
        // The go-to moves the unit at once as far as its points reach (R-MOVE-008): either it is there, or the order stands.
        var u = s.Units[id];
        if ((u.Q, u.R) != (q, r))
        {
            Assert.Equal((Movement.GoTo, hex), (u.Order, u.OrderTarget));
            Assert.Equal(Day(until), s.CardOrders[id]);
        }
    }

    // R-ENEMY-005, the owner's answer 1 on #2087 ("Orders expire with the operation", 31 October 2026): a fixture game of one
    // operation card. One brigade row starting on (0, 0); the card orders it to its second anchor's hex (q, r), whose
    // window ends on `to`. With `held`, town_a (1, 0) is an enemy-held locality and an open target (R-MOVE-005, #1304).
    // Fixture values, not history.
    static Content CardGame(int q, int r, string to, string board, bool held = false, string cardLines = "")
    {
        var rows = "units:\n- id: brig\n  service: army\n  side: israel\n  status: draft\n"
            + "  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Here, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n"
            + "  - { place: There, lon: 34.30, lat: 33.35, from: '1948-05-15', to: '" + to + "' }\n";
        var hexes = FormattableString.Invariant($"anchors:\n  - {{ id: brig, anchor: 0, q: 0, r: 0 }}\n  - {{ id: brig, anchor: 1, q: {q}, r: {r} }}\n");
        var card = "id: probe\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\n" + cardLines + "options:\n"
            + FormattableString.Invariant($"  - id: ok\n    text: Accept\n    historical: true\n    requires_unit_regions: [fixture_region]\n    unit_orders: [{{ unit: brig, q: {q}, r: {r} }}]\n")
            + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n";
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = board, ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = rows, ["map/unit_anchor_hexes.yaml"] = hexes,
            ["regions.yaml"] = OperationRegions, ["map/locality_hexes.yaml"] = "localities: []\n",
        });
        if (held)
        {
            files["rules.yaml"] = files["rules.yaml"].Replace("enemy_held_localities: []", "enemy_held_localities: [town_a]");
            files["settlement_targets.yaml"] = "targets:\n" + CaptureTests.Target("town_a", 1, 0);
            // town_a is now an Arab town, so the fixture's city moves to town_h, a town on a far corner (R-CITY-006).
            files["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"].Replace("type: town", "type: arab_town")
                + "  - { id: town_h, name: Town H, lon: 34.79, lat: 33.15, type: town, founded: 1900, pre_modern: false }\n";
            files["map/settlement_hexes.yaml"] = Fixture.Files["map/settlement_hexes.yaml"] + "  - { id: town_h, q: 5, r: 2 }\n";
            files["cities.yaml"] = "cities:\n" + Fixture.City("town_h", 12, 8, 10, 6, "[military_camp, hakhshara]");
        }
        return ContentLoader.Load(Fixture.Write(files, new() { ["probe.yaml"] = card }));
    }

    // R-ENEMY-007: HistoricalAI previews the ordinary staging go-to before deciding whether to answer the open operation.
    // Red first: OperationStatus reads the turn-start hex (2,0), omits the choice, and end-turn delegation resolves it instead.
    [Fact]
    public void HistoricalAIAnswersAnOperationWhenThisTurnsStagingMovePlacesItsLastUnit()
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Units[0] = At(s.Units[0], 2, 0);   // outside fixture_region; this turn's staging reaches its (1,0) member

        var commands = HistoricalAI.Commands(c, s);

        Assert.Contains(commands, x => x is UnitOrder { Unit: 0, Order: Movement.GoTo });
        Assert.Contains(commands, x => x is ChooseEventOption { EventId: "probe", OptionId: "ok" });
        var r = Simulation.Step(c, s, commands);
        Assert.Empty(r.Rejected);
        Assert.Contains(r.Why, x => x.Key == CauseKeys.EventChosen && x.Target == "probe");
        Assert.DoesNotContain(r.Why, x => x.Key == CauseKeys.EventDelegated && x.Target == "probe");
    }

    // (a) The order ends when its anchor's window ends (the anchor's `to`, sourced in units_1948.yaml), so the unit stops
    // where it is: no late arrival. With a later end the same march arrives on the next turn.
    [Fact]
    public void ACardOrderEndsWhenItsAnchorsWindowEnds()
    {
        var c = CardGame(5, 0, "1948-05-20", MovementTests.Board());
        var first = Simulation.Step(c, Simulation.NewGame(c, 1, Mode.Historical), new Command[] { new ChooseEventOption("probe", "ok") });
        Assert.Equal((3, 0, Movement.GoTo), (first.State.Units[0].Q, first.State.Units[0].R, first.State.Units[0].Order));
        Assert.Equal(new DateTime(1948, 5, 22), first.State.Date);   // weekly turns: the window has ended
        var next = Simulation.Step(c, first.State, Array.Empty<Command>());
        Assert.Equal((3, 0, ""), (next.State.Units[0].Q, next.State.Units[0].R, next.State.Units[0].Order));
        Assert.Contains(next.Why, x => x.Key == WhyKeys.CardOrderEnded && x.Args.SequenceEqual(new[] { "5,0" }));

        var open = CardGame(5, 0, "1948-06-01", MovementTests.Board());
        var later = Simulation.Step(open, Simulation.NewGame(open, 1, Mode.Historical), new Command[] { new ChooseEventOption("probe", "ok") });
        later = Simulation.Step(open, later.State, Array.Empty<Command>());
        Assert.Equal((5, 0, ""), (later.State.Units[0].Q, later.State.Units[0].R, later.State.Units[0].Order));
        // An order to an anchor with no end could outlast its operation: the loader refuses it.
        var endless = Assert.Throws<ContentException>(() => CardGame(5, 0, "", MovementTests.Board()));
        Assert.Equal(("events/probe.yaml", "events[probe].options[ok].unit_orders[0]"), (endless.File, endless.Field));
        Assert.Contains("no 'to' date", endless.Message);
    }

    // R-ENEMY-005, the owner's option 1 on #2087 (2 November 2026): a card may order a row's unit to one of its `order_targets`,
    // sourced and dated as an anchor but never drawn and never a start; the order ends on the target's `to`. The loader
    // refuses a target with no sources or no from/to date. Fixture values: one brigade row on (0, 0), a target on (4, 0).
    static Content OrderTargetGame(string target)
    {
        var rows = "units:\n- id: brig\n  service: army\n  side: israel\n  status: draft\n"
            + "  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Here, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n"
            + "  order_targets:\n  - { place: There, lon: 34.30, lat: 33.35" + target + " }\n";
        const string Hexes = "anchors:\n  - { id: brig, anchor: 0, q: 0, r: 0 }\norder_targets:\n  - { id: brig, target: 0, q: 4, r: 0 }\n";
        const string Card = "id: probe\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n"
            + "  - id: ok\n    text: Accept\n    historical: true\n    requires_unit_regions: [fixture_region]\n    unit_orders: [{ unit: brig, q: 4, r: 0 }]\n"
            + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n";
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = rows, ["map/unit_anchor_hexes.yaml"] = Hexes,
            ["regions.yaml"] = OperationRegions, ["map/locality_hexes.yaml"] = "localities: []\n",
        });
        return ContentLoader.Load(Fixture.Write(files, new() { ["probe.yaml"] = Card }));
    }

    [Fact]
    public void ACardOrdersAUnitToASourcedDatedOrderTarget()
    {
        const string Sources = ", sources: [{ url: 'https://example.org/a', checked: '2026-11-02' }]";
        var c = OrderTargetGame(", from: '1948-05-15', to: '1948-05-20'" + Sources);
        var start = Assert.Single(c.StartingForces);
        Assert.Equal((0, 0), (start.Q, start.R));   // a target starts no unit
        var first = Simulation.Step(c, Simulation.NewGame(c, 1, Mode.Historical), new Command[] { new ChooseEventOption("probe", "ok") });
        Assert.Empty(first.Rejected);
        Assert.Equal((3, 0, Movement.GoTo), (first.State.Units[0].Q, first.State.Units[0].R, first.State.Units[0].Order));
        var next = Simulation.Step(c, first.State, Array.Empty<Command>());   // the target's `to` has passed: the order ends
        Assert.Contains(next.Why, x => x.Key == WhyKeys.CardOrderEnded && x.Args.SequenceEqual(new[] { "4,0" }));

        foreach (var (target, field) in new[]
        {
            (", from: '1948-05-15', to: '1948-05-20'", "units[brig].order_targets[0].sources"),
            (", from: '1948-05-15', to: '1948-05-20', sources: []", "units[brig].order_targets[0].sources"),
            (", to: '1948-05-20'" + Sources, "units[brig].order_targets[0].from"),
            (", from: '1948-05-15'" + Sources, "units[brig].order_targets[0].to"),
        })
        {
            var e = Assert.Throws<ContentException>(() => OrderTargetGame(target));
            Assert.Equal(("map/units_1948.yaml", field), (e.File, e.Field));
        }
    }

    // (b) The card's march never ends on an enemy-held place it was not sent to: it goes round town_a, where the player's
    // own go-to on the same route ends its first turn and captures it (unchanged). (2, 0) is desert, so the cheapest way
    // runs through town_a and the way round costs one more.
    [Fact]
    public void ACardOrderGoesRoundAnEnemyHeldPlaceItWasNotSentTo()
    {
        var c = CardGame(2, 0, "1949-01-01", MovementTests.Board((2, 0, "desert")), held: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var mine = Simulation.Preview(c, s, new Command[] { new UnitOrder(0, Movement.GoTo, 2, 0) });
        Assert.Equal((1, 0), (mine.State.Units[0].Q, mine.State.Units[0].R));
        Assert.Equal(new[] { "town_a" }, mine.State.Settlements.Keys);

        var card = Simulation.Step(c, s, new Command[] { new ChooseEventOption("probe", "ok") });
        Assert.NotEqual((1, 0), (card.State.Units[0].Q, card.State.Units[0].R));
        Assert.Empty(card.State.Settlements);
        var arrived = Simulation.Step(c, card.State, Array.Empty<Command>());
        Assert.Equal((2, 0, ""), (arrived.State.Units[0].Q, arrived.State.Units[0].R, arrived.State.Units[0].Order));
        Assert.Empty(arrived.State.Settlements);
    }

    // (c) A refused card order is told to the player the way a refused order of the player's is: a rejection with a reason
    // the UI has in English and Hebrew (SimBridge.ReasonKeys). Here an enemy unit holds the anchor, chosen and delegated.
    [Fact]
    public void ARefusedCardOrderIsReportedLikeThePlayersOwn()
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Units.Add(9, Enemy(9, 4, 0, 1));
        foreach (var commands in new[] { new Command[] { new ChooseEventOption("probe", "ok") }, Array.Empty<Command>() })
        {
            var r = Simulation.Step(c, s, commands);
            var refused = Assert.Single(r.Rejected);
            Assert.Equal(Orders.RefusedCardOrder, refused.Reason);
            var order = Assert.IsType<UnitOrder>(refused.Command);
            Assert.Equal((0, Movement.GoTo, (int?)4, (int?)0), (order.Unit, order.Order, order.Q, order.R));
            Assert.Equal((0, 0, ""), (r.State.Units[0].Q, r.State.Units[0].R, r.State.Units[0].Order));
        }
    }

    // Review of #2087, optional 1: a delegated card's order shares the movement points a Move spent in the same turn, as a
    // chosen card's does (OperationCardOrdersAreValidated). Unshared, the unit would arrive on (4, 0).
    [Fact]
    public void ADelegatedCardOrderSharesTheTurnsMovementPoints()
    {
        var c = CardGame(4, 0, "1949-01-01", MovementTests.Board());
        var r = Simulation.Step(c, Simulation.NewGame(c, 1, Mode.Historical), new Command[] { new Move(0, 1, 0) });
        Assert.Equal((3, 0, Movement.GoTo), (r.State.Units[0].Q, r.State.Units[0].R, r.State.Units[0].Order));
    }

    // R-ENEMY-005 (#2806 item 9): a card's orders are given in the option's list order. Two brigade rows, brig_a on (0, 0)
    // and brig_b on (0, 1), are both sent to (2, 0), which already holds one Israeli unit (the fixture's stack limit is 2 a
    // side). The order listed first arrives and fills the hex; the one listed second finds it full and stops where it
    // stands (Movement.Advance). Listed the other way round, the other unit arrives. Fixture values, not history.
    [Theory]
    [InlineData("brig_a", "brig_b")]
    [InlineData("brig_b", "brig_a")]
    public void ACardsOrdersAreGivenInListOrder(string first, string second)
    {
        static string Row(string id) => "- id: " + id + "\n  service: army\n  side: israel\n  status: draft\n"
            + "  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
            + "  - { place: Here, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n"
            + "  - { place: There, lon: 34.30, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n";
        const string Hexes = "anchors:\n  - { id: brig_a, anchor: 0, q: 0, r: 0 }\n  - { id: brig_a, anchor: 1, q: 2, r: 0 }\n"
            + "  - { id: brig_b, anchor: 0, q: 0, r: 1 }\n  - { id: brig_b, anchor: 1, q: 2, r: 0 }\n";
        var card = "id: probe\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n"
            + "  - id: ok\n    text: Accept\n    historical: true\n    requires_unit_regions: [fixture_region]\n"
            + "    unit_orders: [{ unit: " + first + ", q: 2, r: 0 }, { unit: " + second + ", q: 2, r: 0 }]\n"
            + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n";
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n" + Row("brig_a") + Row("brig_b"), ["map/unit_anchor_hexes.yaml"] = Hexes,
            // R-ENEMY-007 (#2543): both rows start in the card's region, so the operation is met at once.
            ["regions.yaml"] = OperationRegions.Replace("{ q: 0, r: 0 }, { q: 1, r: 0 }]", "{ q: 0, r: 0 }, { q: 0, r: 1 }, { q: 1, r: 0 }]"),
            ["map/locality_hexes.yaml"] = "localities: []\n",
        }), new() { ["probe.yaml"] = card }));
        int a = c.StartingForces.FindIndex(f => f.Row == first), b = c.StartingForces.FindIndex(f => f.Row == second);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var start = (s.Units[b].Q, s.Units[b].R);
        s.Units.Add(9, Soldier(9, 2, 0, 3));

        var r = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok") });
        Assert.Empty(r.Rejected);   // a full destination stops the march; it is no refusal
        Assert.Equal((2, 0, ""), (r.State.Units[a].Q, r.State.Units[a].R, r.State.Units[a].Order));
        Assert.Equal((start.Q, start.R, ""), (r.State.Units[b].Q, r.State.Units[b].R, r.State.Units[b].Order));
        Assert.Contains(r.Why, x => x.Key == WhyKeys.StoppedFull && x.Args.Contains("2,0"));
        Assert.Equal(new[] { a, 9 }, r.State.Units.Values.Where(u => (u.Q, u.R) == (2, 0)).Select(u => u.Id).OrderBy(x => x));
    }

    // R-ENEMY-007 changed this interaction: Golani at Nazareth is outside Horev's allowed southern regions, so delegation
    // postpones before R-ENEMY-005 can issue an order. The occupation and its last-garrison state remain untouched.
    [Fact]
    public void HorevPostponesWhileGolaniIsTheLastUnitAtNazareth()
    {
        var s = AloneInNazareth("1948-12-15");
        Assert.Contains("operation_horev", s.PendingEvents);
        var why = new List<Cause>(); var rejected = new List<Rejection>();
        EventSystem.Delegate(Real, s, why, rejected: rejected);
        Assert.Empty(rejected);
        Assert.Contains(why, x => x.Key == CauseKeys.OperationPostponed && x.Message.Contains("Golani Brigade"));
        Assert.Contains("operation_horev", s.PendingEvents);
        var golani = s.Units[0];
        Assert.Equal((Nazareth.Q, Nazareth.R), (golani.Q, golani.R));
        Assert.NotEqual(Movement.GoTo, golani.Order);
        Assert.False(s.CardOrders.ContainsKey(0));
        Assert.Equal(SettlementControl.AwaitingEnd, s.Settlements["nazareth"].State);
    }

    // A second garrison does not satisfy R-ENEMY-007 either: the required Golani row itself must be in an allowed region.
    [Fact]
    public void ASecondNazarethGarrisonDoesNotBypassHorevsPlacementRequirement()
    {
        var s = AloneInNazareth("1948-12-15");
        var g = s.Units[0];
        var second = s.NextId++;
        s.Units.Add(second, new Unit(second, g.Type, g.Q, g.R, g.Service, g.Side, g.Strength, "", "", g.HomeCity, g.Full));
        var r = Simulation.Step(Real, s, Array.Empty<Command>());
        Assert.Empty(r.Rejected);
        Assert.Contains(r.Why, x => x.Key == CauseKeys.OperationPostponed);
        Assert.Equal((Nazareth.Q, Nazareth.R, ""), (r.State.Units[0].Q, r.State.Units[0].R, r.State.Units[0].Order));
        Assert.Equal((Nazareth.Q, Nazareth.R), (r.State.Units[second].Q, r.State.Units[second].R));
        Assert.Equal(SettlementControl.AwaitingEnd, r.State.Settlements["nazareth"].State);
    }

    // "Never": the check holds on every turn of the march, not only when the card gives the order. A card's go-to still
    // standing for Golani alone on Nazareth (as when the march ends a turn on Nazareth and the unit that held it is then lost,
    // or a second unit there let the order go ahead but Golani could not move that turn and the other unit then left) is taken
    // away at the route step, listed with the same reason, and Golani stays. The order is cleared, not paused: nothing brings
    // it back. The 15 November 1948 turn is used, before Horev, so Horev's
    // own order is not in the turn; Hiram's orders name Carmeli and the 7th Brigade, not Golani.
    // Red without the rule: ContinueRoutes advances the card's go-to, Golani leaves Nazareth, no refusal names unit 0, and step
    // 10 ends the occupation. The player's own go-to (no CardOrders entry) is never held back: Golani leaves.
    [Fact]
    public void AStandingCardOrderStopsRatherThanEmptyNazareth()
    {
        var s = AloneInNazareth("1948-11-15");
        Assert.DoesNotContain("operation_horev", s.PendingEvents);
        var g = s.Units[0];
        s.Units[0] = new Unit(g.Id, g.Type, g.Q, g.R, g.Service, g.Side, g.Strength, Movement.GoTo, "-8,28", g.HomeCity, g.Full, g.CombatXp);
        var mine = Simulation.Step(Real, s, Array.Empty<Command>());   // the player's go-to, before the card's entry is added
        Assert.DoesNotContain(mine.Rejected, j => j.Command is UnitOrder { Unit: 0 });
        Assert.NotEqual((Nazareth.Q, Nazareth.R), (mine.State.Units[0].Q, mine.State.Units[0].R));

        s.CardOrders[0] = new DateTime(1949, 2, 24);   // Golani's Julis anchor's window (R-ENEMY-005)
        var r = Simulation.Step(Real, s, Array.Empty<Command>());
        Assert.Equal(Orders.RefusedLastGarrison, Assert.Single(r.Rejected, j => j.Command is UnitOrder { Unit: 0 }).Reason);
        Assert.Equal((Nazareth.Q, Nazareth.R, ""), (r.State.Units[0].Q, r.State.Units[0].R, r.State.Units[0].Order));
        Assert.False(r.State.CardOrders.ContainsKey(0));
        Assert.Equal(SettlementControl.AwaitingEnd, r.State.Settlements["nazareth"].State);
    }

    // R-ENEMY-005, R-CTRL-013, the tie-break (review of #2087, C1): two Israeli units on Nazareth, both on a card's march to
    // the Julis camp. The route step takes the units in ascending id and checks each as it comes (Movement.ContinueRoutes,
    // SettlementControl.LastGarrison): Golani (unit 0) is not the last, so it marches off; the second unit, a higher id, is
    // then the last one there, so its order is taken away and it stays. The 15 December 1948 state is the one
    // HorevsOrderGoesAheadWhenAnotherUnitHoldsNazareth uses, where a card's march takes Golani off Nazareth. Only the route
    // step runs, so Horev's own order is not in it.
    // Red without the rule: both march and Nazareth is left with no unit, and no refusal names the second unit. Red for a
    // check made once for the hex before either moved (neither is the last, so both march), or for one in descending id
    // (Golani would stay and the second unit would march).
    [Fact]
    public void OfTwoUnitsOnACardsMarchTheHigherIdStays()
    {
        var s = AloneInNazareth("1948-12-15");
        var g = s.Units[0];
        var second = s.NextId++;
        Assert.True(second > 0);
        foreach (var id in new[] { 0, second })
        {
            s.Units[id] = new Unit(id, g.Type, g.Q, g.R, g.Service, g.Side, g.Strength, Movement.GoTo, "-8,28", g.HomeCity, g.Full, id == 0 ? g.CombatXp : 0);
            s.CardOrders[id] = new DateTime(1949, 2, 24);   // Golani's Julis anchor's window (R-ENEMY-005)
        }
        var rejected = new List<Rejection>();
        Movement.ContinueRoutes(Real, s, new Dictionary<int, int>(), new List<Cause>(), rejected);
        Assert.DoesNotContain(rejected, j => j.Command is UnitOrder { Unit: 0 });
        Assert.NotEqual((Nazareth.Q, Nazareth.R), (s.Units[0].Q, s.Units[0].R));
        Assert.Equal(Orders.RefusedLastGarrison, Assert.Single(rejected, j => j.Command is UnitOrder o && o.Unit == second).Reason);
        Assert.Equal((Nazareth.Q, Nazareth.R, ""), (s.Units[second].Q, s.Units[second].R, s.Units[second].Order));
        Assert.False(s.CardOrders.ContainsKey(second));
    }

    static SettlementTarget Nazareth =>Real.SettlementTargets.Single(t => t.Id == "nazareth");

    /// <summary>mvp-occupy-cost played to <paramref name="until"/> (its own steps, the run's end replaced): Golani (unit 0,
    /// the first starting force) alone on Nazareth, which awaits the end of its occupation. The scenario uses the real
    /// gdd-0.1 content; operation placement and card-order handling are shared by both GDD ruleset identities.</summary>
    static GameState AloneInNazareth(string until)
    {
        Assert.NotEqual(SaveGame.Gdd03Ruleset, Real.Ruleset);
        Assert.Equal("golani", Real.StartingForces[0].Row);
        var text = System.IO.File.ReadAllText(System.IO.Path.Combine(ScenarioTests.Dir(), "mvp-occupy-cost.yaml"));
        var s = Scenario.Parse(System.Text.RegularExpressions.Regex.Replace(text, "(?m)^until: .*$", "until: " + until)).Play(Real);
        Assert.Equal(DateTime.Parse(until, System.Globalization.CultureInfo.InvariantCulture), s.Date);
        Assert.Equal(new[] { 0 }, s.Units.Values.Where(u => (u.Q, u.R) == (Nazareth.Q, Nazareth.R) && u.Side == Movement.PlayerSide).Select(u => u.Id));
        Assert.Equal(SettlementControl.AwaitingEnd, s.Settlements["nazareth"].State);
        return s;
    }

    // R-ENEMY-003, #2114 (O4, owner ruling 1 Nov 2026): under exact gdd-0.3 the threshold is the catalogue's
    // combat.enemy_attack_ratio, 1.5:1 (150%); gdd-0.1 keeps mvp.enemy_attack_ratio_percent (the fixture's 200).
    static Content Gdd03() => ContentLoader.Load(Fixture.Write(Gdd03Files()));

    static Dictionary<string, string> Gdd03Files()
    {
        var f = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
        });
        f["rules.yaml"] = f["rules.yaml"].Replace("move_cost: { coastal_plain: 1, hills: 2, desert: 3 }",
            "move_cost: { coastal_plain: 1, lowland: 1, valley: 1, hills: 2, mountains: 3, desert: 3 }")
            .Replace("combat_terrain_percent: { coastal_plain: 100, hills: 150, desert: 100 }",
            "combat_terrain_percent: { coastal_plain: 100, lowland: 100, valley: 100, hills: 150, mountains: 150, desert: 100 }");
        f["eras.yaml"] = f["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + SaveGame.Gdd03Ruleset);
        f[Gdd03BalanceCatalogue.FileName] = System.IO.File.ReadAllText(System.IO.Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        return f;
    }

    [Theory]
    [InlineData(true, 30, 150, Combat.Attack, "3,0")]       // gdd-0.3: exactly 1.5:1 attacks
    [InlineData(true, 29, 145, Orders.Fortifying, "2,0")]   // gdd-0.3: just under 1.5:1 holds
    [InlineData(false, 30, 150, Orders.Fortifying, "2,0")]  // gdd-0.1: 1.5:1 holds
    [InlineData(false, 40, 200, Combat.Attack, "3,0")]      // gdd-0.1: 2:1 attacks
    public void Gdd03AttacksAtOneAndAHalfAndGdd01KeepsTwo(bool gdd03, int strength, int ratio, string order, string target)
    {
        var c = gdd03 ? Gdd03() : Load();
        Assert.Equal(gdd03 ? SaveGame.Gdd03Ruleset : SaveGame.GddRuleset, c.Ruleset);
        var s = CombatTests.With(c, Soldier(1, 3, 0, 20), Enemy(2, 2, 0, strength));
        Assert.Equal(ratio, Combat.Preview(c, s, 2, 3, 0)!.Value.RatioPercent);
        EnemyAI.Order(c, s, new List<Cause>());
        Assert.Equal((order, target), (s.Units[2].Order, s.Units[2].OrderTarget));
    }
}
