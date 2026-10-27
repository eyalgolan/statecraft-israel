using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The enemy AI, resolver step 1 (GDD Phase 5 PR 10, part 1; docs/rules/mvp.md R-ENEMY-001 to R-ENEMY-004). Every
    /// non-player military unit gets its orders from the state alone, through the player's own validation (Movement.Move,
    /// Combat.Give, Orders.Give with the sides swapped). Q-ENEMY-1: a starting unit marches on its row's sourced 11 June 1948
    /// position; after that, or when the move cannot be made, it follows the simple goals of Q-ENEMY-2: attack an adjacent player
    /// unit at mvp.enemy_attack_ratio_percent or better, else hold its hex fortified. No RNG (Q-ENEMY-2), no reinforcements
    /// (Q-ENEMY-3), no supply (Q-ENEMY-4).</summary>
    public static class EnemyAI
    {
        /// <summary>The script's dated position (Q-ENEMY-1; research_notes/GDD phase 5/arab-forces-1948.md section 10): an
        /// anchor of map/units_1948.yaml from this day. The other scripted date, 15 May 1948, is the start (R-UNIT-006).</summary>
        public const string ScriptDate = "1948-06-11";

        /// <summary>R-ENEMY-006: the truce windows, half-open [from, until), in which no enemy unit attacks (owner, 21 October
        /// 2026, #749). The first truce from 11 June 1948 (content/events/first_truce.yaml: S/834, four weeks from 06:00 GMT on
        /// 11 June) to 8 July, when Egypt renewed the fighting (first_truce.yaml, ten_days.yaml; the 11 June anchors' `to` in
        /// map/units_1948.yaml). The second from 18 July 1948 (second_truce.yaml: 19:00 on 18 July) to 15 October, Operation
        /// Yoav (second_truce.yaml; operation_yoav.yaml `from`). The same windows as the map's display-only truce periods
        /// (docs/rules/mvp.md R-ENEMY-006). A stopgap by date: #750 replaces it with the full war state (a refused truce, the
        /// security rung), so a refused truce card does not lift it yet.</summary>
        static readonly (DateTime From, DateTime Until)[] Truces =
        {
            (new DateTime(1948, 6, 11), new DateTime(1948, 7, 8)),
            (new DateTime(1948, 7, 18), new DateTime(1948, 10, 15)),
        };

        /// <summary>R-ENEMY-006: whether the turn from <paramref name="start"/> to <paramref name="next"/> is in a truce: its
        /// middle is inside a window. A monthly turn spans a truce's edge (15 July to 15 August 1948 holds three days of the Ten
        /// Days and 28 of the second truce), and its middle puts it on the side that holds most of it. Agent default (#1263).</summary>
        static bool InTruce(DateTime start, DateTime next)
        {
            var mid = start.AddTicks((next - start).Ticks / 2);
            return Truces.Any(t => mid >= t.From && mid < t.Until);
        }

        static bool IsEnemy(Unit u) => u.Side != "" && u.Side != Movement.PlayerSide;

        /// <summary>Resolver step 1: in ascending unit id, each enemy unit makes its scripted move (R-ENEMY-002) or, failing
        /// that, takes the goals' order (R-ENEMY-003). A refused order changes nothing, as the player's does.
        /// <paramref name="next"/> is the turn's end (R-ENEMY-006); without it the turn is read on its first day.</summary>
        public static void Order(Content c, GameState s, List<Cause> why, DateTime? next = null)
        {
            var truce = InTruce(s.Date, next ?? s.Date);
            var mvp = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
            var spent = new Dictionary<int, int>();
            foreach (var id in s.Units.Keys.ToList())   // Units is a SortedList: ascending id
            {
                if (!s.Units.TryGetValue(id, out var u) || !IsEnemy(u)) continue;
                // R-ENEMY-006: a truce ends a standing attack whatever the unit does: a scripted move keeps every order but
                // fortify (Movement.Move), and step 2 would resolve it inside the truce (owner, #1263).
                if (truce && u.Order == Combat.Attack)
                {
                    s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, "", "", u.HomeCity, u.Full);
                    why.Add(UnitLines.Line(WhyKeys.AttackEndedTruce,
                        $"Unit {id}'s attack on hex {u.OrderTarget} ends: a truce is on", new[] { u }, u.OrderTarget));
                }
                if (Script(c, s, u) is (int q, int r) && Movement.Toward(c, s, u, q, r) is (int tq, int tr)
                    && Movement.Move(c, s, id, tq, tr, spent, why, enemy: true) == null) continue;
                Goals(c, s, mvp, s.Units[id], truce, why);
            }
        }

        /// <summary>R-ENEMY-002: the hex unit <paramref name="u"/> marches on this turn, or null. Only a starting force has a
        /// script (the unit with id i of a new game is c.StartingForces[i], R-UNIT-006), and only in Historical mode, on a turn
        /// that starts before its anchor's window ends.</summary>
        static (int Q, int R)? Script(Content c, GameState s, Unit u)
        {
            if (s.Mode != Mode.Historical || u.Id >= c.StartingForces.Count) return null;
            var f = c.StartingForces[u.Id];
            if (f.Side != u.Side || f.Type != u.Type || f.Script == null || s.Date >= f.ScriptUntil) return null;
            return f.Script;
        }

        /// <summary>R-ENEMY-003: attack the first neighbour, in the fixed order (R-MOVE-002), with a player military unit whose
        /// attack preview reaches the threshold (a standing attack on it is kept); else fortify the unit's hex, which keeps a
        /// fortify it already has. Inside a truce window (R-ENEMY-006) it only fortifies, which also replaces a standing
        /// attack.</summary>
        static void Goals(Content c, GameState s, MvpRules mvp, Unit u, bool truce, List<Cause> why)
        {
            foreach (var (dq, dr) in truce ? Array.Empty<(int Q, int R)>() : Movement.Neighbours)   // a truce: straight to fortify
            {
                int q = u.Q + dq, r = u.R + dr;
                if (!(Combat.Preview(c, s, u.Id, q, r) is (int ratio, _)) || ratio < mvp.EnemyAttackRatioPercent!.Value) continue;
                var target = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", q, r);
                if (u.Order == Combat.Attack && u.OrderTarget == target) return;
                if (Combat.Give(c, s, u.Id, q, r, why, enemy: true) == null) return;
            }
            Orders.Give(c, s, u.Id, Orders.Fortify, why, enemy: true);
        }
    }
}
