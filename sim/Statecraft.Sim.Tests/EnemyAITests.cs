using System;
using System.Collections.Generic;
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
    // where Combat.Preview's ratio reaches mvp.enemy_attack_ratio_percent; otherwise it fortifies its hex.
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

    // R-ENEMY-002: the script never invents a position. Each scripted target is its row's 11 June 1948 anchor that cites at
    // least two sources that are not Wikipedia or Wikidata, on that anchor's hex (unit_anchor_hexes.yaml); the ALA has none.
    [Fact]
    public void TheScriptNeverInventsAPosition()
    {
        var scripted = Real.StartingForces.Where(f => f.Script != null)
            .Select(f => (f.Row, f.Script!.Value.Q, f.Script!.Value.R, f.ScriptUntil)).OrderBy(x => x.Row, StringComparer.Ordinal).ToList();
        var until = new DateTime(1948, 7, 8);   // the four anchors' window end: the first truce's (units_1948.yaml)
        Assert.Equal(new[]
        {
            ("arab_legion", -2, 22, until),                     // Latrun
            ("egyptian_expeditionary_force", -7, 24, until),    // Isdud, the forward base (the row's first 11 June anchor)
            ("iraqi_expeditionary_force", 6, 13, until),        // Jenin
            ("syrian_army", 13, 5, until),                      // the bridgehead at Mishmar HaYarden
        }, scripted);
        Assert.All(Real.StartingForces.Where(f => f.Side == Movement.PlayerSide), f => Assert.Null(f.Script));
    }

    // R-ENEMY-002: an 11 June anchor on one non-Wikipedia source (with Wikipedia beside it) is not scripted; two are.
    [Theory]
    [InlineData("['https://example.org/a', 'https://en.wikipedia.org/wiki/Isdud', 'https://www.wikidata.org/wiki/Q1']", false)]
    [InlineData("['https://example.org/a', 'https://example.org/b']", true)]
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
        Assert.Equal(scripted ? (3, 2) : ((int, int)?)null, f.Script);
    }

    // R-ENEMY-002: in the real game each scripted unit marches on its 11 June hex along its cheapest path (round the player's
    // units and localities), as far as its movement points reach, on the turns that start before the anchor's window ends
    // (15 May and 15 June). With 3 points a turn each ends on its hex or next to it. From the turn of 15 July the AI moves
    // no unit.
    [Fact]
    public void AScriptedUnitMarchesToItsJuneAnchorThenFollowsTheGoals()
    {
        var c = Real;
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var targets = c.StartingForces.Select((f, i) => (f, i)).Where(x => x.f.Script != null)
            .ToDictionary(x => x.i, x => x.f.Script!.Value);
        Assert.Equal(4, targets.Count);
        var first = Simulation.Step(c, s, Array.Empty<Command>());
        foreach (var id in targets.Keys)
            Assert.Contains(first.Why, x => x.Message.StartsWith($"Unit {id} moved to hex", StringComparison.Ordinal));
        var june = Play(c, first.State, 1);
        foreach (var (id, hex) in targets.Select(t => (t.Key, t.Value)))
            Assert.True(Distance(june.Units[id].Q, june.Units[id].R, hex.Q, hex.R) <= 1, $"unit {id} is not at or next to its 11 June hex");
        Assert.Equal(new DateTime(1948, 7, 15), june.Date);
        var why = new List<Cause>();
        var positions = june.Units.Values.Select(u => (u.Id, u.Q, u.R)).ToList();
        EnemyAI.Order(c, june, why);
        Assert.Equal(positions, june.Units.Values.Select(u => (u.Id, u.Q, u.R)).ToList());
        Assert.DoesNotContain(why, x => x.Message.Contains("moved to hex", StringComparison.Ordinal));
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
        var latrun = c.StartingForces[legion].Script!.Value;
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
        Assert.Equal((-2, 22), (s.Units[legion].Q, s.Units[legion].R));   // the Legion on Latrun
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
        var first = Simulation.Step(Real, s, new Command[] { new Move(givati, -7, 24), new Move(negevId, -10, 27) });
        Assert.Contains(first.Why, x => x.Message == $"Unit {egypt} will attack hex -10,27");
        s = first.State;
        Assert.Equal(new DateTime(1948, 6, 15), s.Date);
        Assert.True(s.Units.ContainsKey(negevId), "the Negev fell on the 15 May turn");
        Assert.Equal((Combat.Attack, "-10,27"), (s.Units[egypt].Order, s.Units[egypt].OrderTarget));   // the attack stands
        var strength = s.Units[negevId].Strength;
        var step = Simulation.Step(Real, s, new Command[] { new Move(givati, -5, 23) });
        Assert.Equal((-9, 27), (step.State.Units[egypt].Q, step.State.Units[egypt].R));   // Egypt moved on its script
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

    // R-ENEMY-004: no reinforcements in Phase 5 (Q-ENEMY-3). No turn adds an enemy unit.
    [Fact]
    public void NoReinforcements()
    {
        var s = Simulation.NewGame(Real, 1, Mode.Historical);
        var ids = s.Units.Values.Where(IsEnemy).Select(u => u.Id).ToHashSet();
        for (int i = 0; i < 8 && s.GameOver == null; i++)
        {
            s = Simulation.Step(Real, s, Array.Empty<Command>()).State;
            Assert.Subset(ids, s.Units.Values.Where(IsEnemy).Select(u => u.Id).ToHashSet());
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
        Assert.Empty(s.Settlements);
    }
}
