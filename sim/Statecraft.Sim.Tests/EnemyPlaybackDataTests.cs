using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>#2202 phase A (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 1): what the map needs to play
/// the enemy's turn from the sim's own Why lines. A unit line names the hex the unit stood on when the line was written
/// (UnitRef.Q, UnitRef.R), so a move gives where it came from as well as where it went; and the order of the enemy's lines is
/// pinned (R-ENEMY-001, R-COMBAT-002). The states are the real 1948 start (GoldenRun.Seed), with an Israeli unit placed by
/// hand where a case needs one, as UnitLinesTests does.</summary>
public class EnemyPlaybackDataTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    static GameState Start() => Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);

    static bool IsEnemy(Unit u) => u.Side != "" && u.Side != Movement.PlayerSide;

    static string Hex(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);

    /// <summary>A copy of <paramref name="s"/> with a new Israeli unit of Golani's type on (q, r); returns its id.</summary>
    static (GameState s, int id) WithIsraeli(GameState s, int q, int r, int strength)
    {
        var t = s.Clone();
        var id = t.NextId;
        t.Units.Add(id, new Unit(id, s.Units[0].Type, q, r, side: Movement.PlayerSide, strength: strength));
        t.NextId = id + 1;
        return (t, id);
    }

    /// <summary>R-COMBAT-001, R-COMBAT-003: a first-turn state in which an Israeli unit's instant attack makes an enemy unit
    /// that moves this turn retreat, and the enemy AI then moves it. Found by running the sim: the first enemy mover, its
    /// neighbours in the fixed order (R-MOVE-002), and the RNG state, the one draw (R-COMBAT-002), from 1 up.</summary>
    static (GameState s, Command[] strike, int enemy, (int Q, int R) from, (int Q, int R) retreatedTo) StrikeThatRetreats()
    {
        var s = Start();
        var movers = Simulation.Step(C, s, Array.Empty<Command>()).Why
            .Where(w => w.EnemyOrder == Movement.Kind).Select(w => w.Units[0].Id).ToList();
        foreach (var m in movers)
            foreach (var (dq, dr) in Movement.Neighbours)
                for (ulong rng = 1; rng <= 64; rng++)
                {
                    var e = s.Units[m];
                    var (t, id) = WithIsraeli(s, e.Q + dq, e.R + dr, strength: 20);
                    t.RngState = rng;
                    if (Combat.Check(C, t, id, e.Q, e.R) != null) break;
                    var strike = new Command[] { new UnitOrder(id, Combat.Attack, e.Q, e.R) };
                    var orders = Simulation.Preview(C, t, strike);
                    if (!orders.State.Units.TryGetValue(m, out var after) || (after.Q, after.R) == (e.Q, e.R)) continue;
                    if (!Simulation.Step(C, t, strike).Why.Any(w => w.EnemyOrder == Movement.Kind && w.Units[0].Id == m)) continue;
                    return (t, strike, m, (e.Q, e.R), (after.Q, after.R));
                }
        throw new Xunit.Sdk.XunitException("no first-turn strike makes an enemy mover retreat (fixture precondition)");
    }

    // R-ENEMY-001: with no orders, the enemy's step-1 moves start from the turn's state.
    [Fact]
    public void AnEnemyMoveLineNamesTheHexItLeftAndTheHexItReached()
    {
        var s = Start();
        var r = Simulation.Step(C, s, Array.Empty<Command>());
        var moves = r.Why.Select((w, i) => (w, i)).Where(x => x.w.EnemyOrder == Movement.Kind).ToList();
        Assert.NotEmpty(moves);   // precondition: the 1948 start's first turn has enemy moves
        foreach (var (w, i) in moves)
        {
            var named = w.Units[0];
            var before = s.Units[named.Id];
            Assert.Equal((before.Q, before.R), (named.Q, named.R));
            var later = r.Why.Skip(i + 1).Any(x => (x.Key == WhyKeys.Retreated || x.Key == WhyKeys.Destroyed) && x.Units[0].Id == named.Id);
            if (!later) Assert.Equal(Hex(r.State.Units[named.Id].Q, r.State.Units[named.Id].R), w.Args[0]);
        }
    }

    // R-COMBAT-001, R-COMBAT-003, R-ENEMY-001: the player's instant attack pushes an enemy unit back before step 1, so the
    // turn-start state (GameSession.Previous) has the wrong "from" hex for its move. The line has the right one.
    [Fact]
    public void AnEnemyMoveAfterTheirUnitRetreatedFromOurStrikeStartsWhereItRetreatedTo()
    {
        var (s, strike, enemy, from, retreatedTo) = StrikeThatRetreats();
        Assert.NotEqual(from, retreatedTo);   // precondition: the strike really moved the unit
        Assert.Equal(from, (s.Units[enemy].Q, s.Units[enemy].R));
        var move = Simulation.Step(C, s, strike).Why.Single(w => w.EnemyOrder == Movement.Kind && w.Units[0].Id == enemy);
        Assert.Equal(retreatedTo, (move.Units[0].Q, move.Units[0].R));
    }

    // R-COMBAT-003: a retreat line names the hex the unit left (the engagement's) and, in Args[0], where it went.
    [Fact]
    public void ARetreatLineNamesTheHexTheUnitLeft()
    {
        var (s, strike, enemy, from, retreatedTo) = StrikeThatRetreats();
        var why = Simulation.Preview(C, s, strike).Why;
        var engagement = why.Single(w => w.Key == WhyKeys.Engagement);
        var retreat = why.Single(w => w.Key == WhyKeys.Retreated && w.Units[0].Id == enemy);
        Assert.Equal(Hex(from.Q, from.R), engagement.Args[0]);
        Assert.Equal(from, (retreat.Units[0].Q, retreat.Units[0].R));
        Assert.Equal(Hex(retreatedTo.Q, retreatedTo.R), retreat.Args[0]);
    }

    // R-ENEMY-001: only the resolver's steps write enemy causes. The player's orders (a move and an attack) write none, and
    // End turn's enemy lines all come after the orders' lines.
    [Fact]
    public void NoPlayerOrderWritesAnEnemyCause()
    {
        var (s, strike, _, _, _) = StrikeThatRetreats();
        var to = Movement.Reachable(C, s, 0).Keys.First();
        var orders = strike.Prepend(new Move(0, to.Q, to.R)).ToArray();
        var preview = Simulation.Preview(C, s, orders);
        Assert.Empty(preview.Rejected);   // precondition: both orders were carried out
        Assert.Contains(preview.Why, w => w.Key == WhyKeys.UnitMoved);
        Assert.Contains(preview.Why, w => w.Key == WhyKeys.Engagement);
        Assert.All(preview.Why, w => Assert.Equal("", w.EnemyOrder));
        var step = Simulation.Step(C, s, orders);
        var enemyLines = step.Why.Select((w, i) => (w, i)).Where(x => x.w.EnemyOrder != "").ToList();
        Assert.NotEmpty(enemyLines);
        Assert.All(enemyLines, x => Assert.True(x.i >= preview.Why.Count, $"enemy line {x.i} ({x.w}) is among the orders' {preview.Why.Count} lines"));
    }

    // R-ENEMY-001, R-COMBAT-002: step 1 moves the enemy's units in ascending id, then step 2 fights, so every enemy
    // engagement comes after the last enemy move. A weak Israeli unit beside an enemy unit that does not move draws its attack.
    [Fact]
    public void EnemyMovesComeInAscendingUnitIdBeforeTheEngagements()
    {
        var s = Start();
        var movers = Simulation.Step(C, s, Array.Empty<Command>()).Why
            .Where(w => w.EnemyOrder == Movement.Kind).Select(w => w.Units[0].Id).ToHashSet();
        StepResult? r = null;
        foreach (var e in s.Units.Values.Where(u => IsEnemy(u) && !movers.Contains(u.Id)))
        {
            foreach (var (dq, dr) in Movement.Neighbours)
            {
                var (t, _) = WithIsraeli(s, e.Q + dq, e.R + dr, strength: 1);
                var step = Simulation.Step(C, t, Array.Empty<Command>());
                if (step.Why.Any(w => w.EnemyOrder == Movement.Kind) && step.Why.Any(w => w.EnemyOrder == Combat.Attack)) { r = step; break; }
            }
            if (r != null) break;
        }
        Assert.NotNull(r);   // precondition: a first turn with both enemy moves and an enemy attack
        var lines = r!.Why.Select((w, i) => (w, i)).ToList();
        var moves = lines.Where(x => x.w.EnemyOrder == Movement.Kind).ToList();
        var ids = moves.Select(x => x.w.Units[0].Id).ToList();
        Assert.Equal(ids.OrderBy(x => x), ids);
        var lastMove = moves.Max(x => x.i);
        Assert.All(lines.Where(x => x.w.EnemyOrder == Combat.Attack), x => Assert.True(x.i > lastMove));
    }

    // R-MOVE-006: the player's own move writes its line from the unit as it stood before the move (Movement.Move, the same
    // path as the enemy's), so the "from" hex is the old one and Args[0] the new one.
    [Fact]
    public void APlayerMoveLineNamesTheHexItLeft()
    {
        var s = Start();
        var u = s.Units[0];
        var to = Movement.Reachable(C, s, 0).Keys.First(h => h != (u.Q, u.R));
        var why = Simulation.Preview(C, s, new Command[] { new Move(0, to.Q, to.R) }).Why;
        var line = why.Single(w => w.Key == WhyKeys.UnitMoved && w.Units[0].Id == 0);
        Assert.Equal("", line.EnemyOrder);
        Assert.Equal((u.Q, u.R), (line.Units[0].Q, line.Units[0].R));
        Assert.Equal(Hex(to.Q, to.R), line.Args[0]);
    }

    /// <summary>R-COMBAT-002, R-COMBAT-003: a first turn in which an enemy unit's step-2 attack on an Israeli unit placed beside
    /// it ends with that unit's line of <paramref name="key"/> (retreated or destroyed). Found by running the sim over the enemy
    /// units, their neighbours (R-MOVE-002), a few strengths and the RNG state (the AI's choice to attack draws nothing,
    /// Q-ENEMY-2, so a turn with no enemy attack ends that strength's search). A unit on its march (R-ENEMY-002) attacks only
    /// when the march is blocked, so a second Israeli unit holds its 11 June hex: on the 4.4 km grid (#1183) the one enemy
    /// unit without a script reaches 2:1 (enemy_attack_ratio_percent) only on a unit of strength 1, which cannot retreat.</summary>
    static (StepResult r, int id, (int Q, int R) placed) EnemyAttackThatEndsIn(string key)
    {
        var s = Start();
        foreach (var e in s.Units.Values.Where(IsEnemy).ToList())
        {
            var held = HoldingItsMarch(s, e);
            foreach (var (dq, dr) in Movement.Neighbours)
                foreach (var strength in new[] { 1, 2, 3, 4, 6, 8 })
                    for (ulong rng = 1; rng <= 64; rng++)
                    {
                        var (t, id) = WithIsraeli(held, e.Q + dq, e.R + dr, strength);
                        t.RngState = rng;
                        var r = Simulation.Step(C, t, Array.Empty<Command>());
                        if (!r.Why.Any(w => w.EnemyOrder == Combat.Attack)) break;
                        if (r.Why.Any(w => w.Key == key && w.Units[0].Id == id)) return (r, id, (e.Q + dq, e.R + dr));
                    }
        }
        throw new Xunit.Sdk.XunitException($"no first-turn enemy attack ends in {key} for an Israeli unit (fixture precondition)");
    }

    /// <summary>A copy of <paramref name="s"/> with an Israeli unit of strength 1 on enemy unit <paramref name="e"/>'s script
    /// hex when it has one and the hex is empty: no path enters it, so Movement.Toward finds none and the unit takes the
    /// goals (R-ENEMY-002, R-ENEMY-003).</summary>
    static GameState HoldingItsMarch(GameState s, Unit e) =>
        e.Id < C.StartingForces.Count && C.StartingForces[e.Id].Script is (int q, int r) && !s.Units.Values.Any(u => (u.Q, u.R) == (q, r))
            ? WithIsraeli(s, q, r, strength: 1).s
            : s;

    // R-COMBAT-003: our unit pushed back by the enemy's step-2 attack is named on the hex it was attacked on (the
    // engagement's), and Args[0] is where it went.
    [Fact]
    public void OurRetreatFromAnEnemyAttackNamesTheHexWeLeft()
    {
        var (r, id, placed) = EnemyAttackThatEndsIn(WhyKeys.Retreated);
        var retreat = r.Why.Single(w => w.Key == WhyKeys.Retreated && w.Units[0].Id == id);
        Assert.Equal(placed, (retreat.Units[0].Q, retreat.Units[0].R));
        Assert.NotEqual(placed, (r.State.Units[id].Q, r.State.Units[id].R));   // precondition: it really moved
        Assert.Equal(Hex(r.State.Units[id].Q, r.State.Units[id].R), retreat.Args[0]);
        Assert.Contains(r.Why, w => w.Key == WhyKeys.Engagement && w.EnemyOrder == Combat.Attack && w.Args[0] == Hex(placed.Q, placed.R));
    }

    // R-COMBAT-003: a destroyed unit is named on the hex it was destroyed on, though it has left the state.
    [Fact]
    public void ADestroyedLineNamesTheHexTheUnitDiedOn()
    {
        var (r, id, placed) = EnemyAttackThatEndsIn(WhyKeys.Destroyed);
        var line = r.Why.Single(w => w.Key == WhyKeys.Destroyed && w.Units[0].Id == id);
        Assert.False(r.State.Units.ContainsKey(id));
        Assert.Equal(placed, (line.Units[0].Q, line.Units[0].R));
    }
}
