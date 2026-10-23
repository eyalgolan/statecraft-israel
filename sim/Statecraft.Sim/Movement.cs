using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Unit movement in the GDD v0.1 loop (Phase 5 PRs 5 and 6; docs/rules/mvp.md R-MOVE-001 to R-MOVE-007). A Move
    /// goes at command time along the cheapest path within the unit's movement points (spec 2026-10-12 section 2.3); Reachable
    /// and Path are the preview the UI reads (spec section 2.6), from the same search.</summary>
    public static class Movement
    {
        public const string Kind = "move";

        /// <summary>The player's side (units_1948.yaml's id). A unit of any other side is the enemy's; a halutz has no side and
        /// is the player's.</summary>
        public const string PlayerSide = "israel";

        static readonly IReadOnlyDictionary<int, int> NothingSpent = new Dictionary<int, int>();

        /// <summary>The six axial directions in tools/hexgrid.py's NEIGHBOURS order, the fixed order of hexes.json's layout
        /// (R-MOVE-002).</summary>
        internal static readonly (int Q, int R)[] Neighbours = { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };

        static bool IsEnemy(Unit u) => u.Side != "" && u.Side != PlayerSide;

        internal static int Points(Content c, Unit u) => c.UnitTypes.FirstOrDefault(t => t.Id == u.Type)?.MovementPoints ?? 0;

        /// <summary>The hexes unit <paramref name="unitId"/> can move to this turn, each with its path's cost, in (q, r) order:
        /// what Move accepts on a turn the unit has not moved yet (R-MOVE-001 to R-MOVE-005). Empty for an unknown unit, an
        /// enemy unit or one that cannot move.</summary>
        public static IReadOnlyDictionary<(int Q, int R), int> Reachable(Content c, GameState s, int unitId) =>
            Reachable(c, s, unitId, NothingSpent);

        /// <summary>The hexes unit <paramref name="unitId"/> can move to with the points left after <paramref name="spent"/>,
        /// each with its path's cost, in (q, r) order. Empty for an unknown unit, an enemy unit or one that cannot move.</summary>
        public static IReadOnlyDictionary<(int Q, int R), int> Reachable(Content c, GameState s, int unitId, IReadOnlyDictionary<int, int> spent)
        {
            var result = new SortedDictionary<(int Q, int R), int>();
            if (!s.Units.TryGetValue(unitId, out var u) || IsEnemy(u)) return result;
            var points = Points(c, u);
            if (points == 0) return result;
            spent.TryGetValue(unitId, out var used);
            var (dist, _) = Search(c, s, u, points - used, fresh: used == 0);
            foreach (var kv in dist)
                if (kv.Key != (u.Q, u.R) && Full(c, s, u, kv.Key.Q, kv.Key.R) == null) result[kv.Key] = kv.Value;
            return result;
        }

        /// <summary>The hexes the unit's cheapest path to (<paramref name="q"/>, <paramref name="r"/>) enters, in order, within
        /// this turn's movement points (R-MOVE-002); empty when the hex is out of reach.</summary>
        public static IReadOnlyList<(int Q, int R)> Path(Content c, GameState s, int unitId, int q, int r) =>
            Path(c, s, unitId, q, r, NothingSpent);

        /// <summary>The hexes the unit's cheapest path to (<paramref name="q"/>, <paramref name="r"/>) enters, in order, within
        /// the points left after <paramref name="spent"/>; empty when the hex is out of reach.</summary>
        public static IReadOnlyList<(int Q, int R)> Path(Content c, GameState s, int unitId, int q, int r, IReadOnlyDictionary<int, int> spent)
        {
            if (!Reachable(c, s, unitId, spent).ContainsKey((q, r))) return Array.Empty<(int, int)>();
            var u = s.Units[unitId];
            spent.TryGetValue(unitId, out var used);
            var (_, prev) = Search(c, s, u, Points(c, u) - used, fresh: used == 0);
            var path = new List<(int Q, int R)>();
            for (var h = (q, r); h != (u.Q, u.R); h = prev[h]) path.Add(h);
            path.Reverse();
            return path;
        }

        /// <summary>R-MOVE-001: the movement points unit <paramref name="unitId"/> has left this turn; 0 for an unknown unit.</summary>
        public static int PointsLeft(Content c, GameState s, int unitId, IReadOnlyDictionary<int, int> spent)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return 0;
            spent.TryGetValue(unitId, out var used);
            return Math.Max(0, Points(c, u) - used);
        }

        /// <summary>R-MOVE-010: the player's military units, in ascending id, that have movement points, have neither moved nor
        /// attacked this turn (nothing in <paramref name="spent"/>) and have no order (fortify, fortifying, hold and go-to are
        /// orders). End turn waits for them (spec 2026-10-23 section 7); Skip is the UI's and not in the sim.</summary>
        public static IReadOnlyList<int> NeedsOrders(Content c, GameState s, IReadOnlyDictionary<int, int> spent) =>
            s.Units.Values.Where(u => u.Side == PlayerSide && Points(c, u) > 0 && u.Order == ""
                    && !(spent.TryGetValue(u.Id, out var used) && used > 0))
                .Select(u => u.Id).ToList();

        /// <summary>R-MOVE-006: moves the unit, spending the path's cost from what <paramref name="spent"/> says it has used this
        /// turn (R-MOVE-001). Refused, state untouched, for an unknown or enemy unit, one that cannot move or is already there,
        /// water or a hex off the map, an enemy unit's hex (entering it is an attack, Q-MOVE-3), an enemy-held locality that
        /// is not a target (R-MOVE-005), a hex full of the unit's side (R-MOVE-003), or a hex out of reach this turn. With <paramref name="enemy"/>
        /// the enemy AI orders its own unit through the same checks, the sides swapped (R-ENEMY-001).</summary>
        internal static string? Move(Content c, GameState s, int unitId, int q, int r, Dictionary<int, int> spent, List<Cause> why, bool enemy = false)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (IsEnemy(u) != enemy) return "That unit is not yours";
            var points = Points(c, u);
            if (points == 0) return "That unit cannot move";
            if (u.Q == q && u.R == r) return "That unit is already there";
            if (!(c.HexAt(q, r) is MapHex hex) || !hex.Land) return "Units cannot enter water or leave the map";
            if (s.Units.Values.Any(x => x.Q == q && x.R == r && IsEnemy(x) != enemy)) return "An enemy unit holds that hex";
            if (Closed(c, s, enemy).Contains((q, r))) return "Units may not enter an enemy-held locality before its capture is possible";
            if (Full(c, s, u, q, r) is string full) return full;
            spent.TryGetValue(unitId, out var used);
            var (dist, _) = Search(c, s, u, points - used, fresh: used == 0);
            if (!dist.TryGetValue((q, r), out var cost)) return "That hex is out of reach this turn";
            // Moving clears fortify at once (Q-HOLD-1, "lost on move"; the standing orders' "fortify", or "fortifying" the turn it
            // is given, with the hex it names), so a unit that goes out and comes back the same turn is not fortified; hold moves with
            // the unit. The order step at the end of the turn only sees the final hex.
            var fortified = u.Order == "fortify" || u.Order == "fortifying";
            s.Units[unitId] = new Unit(u.Id, u.Type, q, r, u.Service, u.Side, u.Strength, fortified ? "" : u.Order, fortified ? "" : u.OrderTarget, u.HomeCity);
            spent[unitId] = used + cost;
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), cost,
                FormattableString.Invariant($"Unit {unitId} moved to hex {q},{r} for {cost} movement points"), enemy ? Kind : "",
                key: WhyKeys.UnitMoved, units: new[] { new UnitRef(u) },
                args: new[] { FormattableString.Invariant($"{q},{r}"), cost.ToString(CultureInfo.InvariantCulture) }));
            if (u.Order == "fortify")
                why.Add(UnitLines.Line(WhyKeys.FortifyLost,
                    $"Unit {unitId} left hex {u.OrderTarget} and is no longer fortified", new[] { u }, u.OrderTarget));
            return null;
        }

        /// <summary>R-MOVE-003: the refusal when (q, r) already holds stack_limit units of <paramref name="u"/>'s side, else null.</summary>
        internal static string? Full(Content c, GameState s, Unit u, int q, int r)
        {
            var limit = c.RulesAt(s.Date).Mvp!.StackLimit!.Value;   // the loader requires it in the GDD loop
            var held = s.Units.Values.Count(x => x.Id != u.Id && x.Side == u.Side && x.Q == q && x.R == r);
            return held >= limit ? FormattableString.Invariant($"That hex already holds {held} units of that side, the most one hex can hold") : null;
        }

        /// <summary>R-MOVE-005: the hexes of mvp.enemy_held_localities. For the enemy (R-ENEMY-001, the rule's mirror) every
        /// other locality's hex, so no enemy move ends on a player's locality while enemy capture (Q-TGT-2 (c)) is unbuilt.</summary>
        internal static HashSet<(int Q, int R)> NoEntry(Content c, GameState s, bool enemy = false)
        {
            var ids = c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities;
            var held = new HashSet<(int Q, int R)>(c.SettlementHexes.Where(h => ids.Contains(h.Id)).Select(h => (h.Q, h.R)));
            return enemy ? new HashSet<(int Q, int R)>(c.SettlementHexes.Select(h => (h.Q, h.R)).Where(h => !held.Contains(h))) : held;
        }

        /// <summary>R-MOVE-005 in Phase 6 (#1304): the hexes a move and its path may not enter. For the player, those of
        /// mvp.enemy_held_localities that are not curated targets (settlement_targets.yaml), so a target can be captured in play
        /// (the owner's #793 ruling) while the rest, Gaza today, stay closed; for the enemy, <see cref="NoEntry"/>'s mirror
        /// unchanged. Raid's withdrawal and combat retreat still read <see cref="NoEntry"/>, so neither ends on an enemy-held
        /// place.</summary>
        static HashSet<(int Q, int R)> Closed(Content c, GameState s, bool enemy)
        {
            if (enemy) return NoEntry(c, s, true);
            var ids = c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities.Except(c.SettlementTargets.Select(t => t.Id));
            return new HashSet<(int Q, int R)>(c.SettlementHexes.Where(h => ids.Contains(h.Id)).Select(h => (h.Q, h.R)));
        }

        /// <summary>R-ENEMY-002: the furthest hex the unit can reach this turn on its cheapest path to (<paramref name="q"/>,
        /// <paramref name="r"/>), by the same search as Move, or null when no path leads there or its first step is out of reach.</summary>
        internal static (int Q, int R)? Toward(Content c, GameState s, Unit u, int q, int r)
        {
            var (dist, prev) = Search(c, s, u, int.MaxValue);
            if (!dist.ContainsKey((q, r)) || (u.Q, u.R) == (q, r)) return null;
            var points = Points(c, u);
            var h = (q, r);
            while (dist[h] > points) h = prev[h];
            return h == (u.Q, u.R) ? ((int, int)?)null : h;
        }

        /// <summary>R-MOVE-007: the hexes of each built corridor segment (a segment not started or being built is no road yet),
        /// with its kind's mvp.road_move_cost or mvp.rail_move_cost.</summary>
        static List<(HashSet<(int Q, int R)> Hexes, int Cost)> BuiltSegments(Content c, GameState s)
        {
            var m = c.RulesAt(s.Date).Mvp!;   // the loader requires both costs in the GDD loop
            var built = new List<(HashSet<(int Q, int R)>, int)>();
            foreach (var d in c.Corridors)
                if (s.Corridors.TryGetValue(d.Id, out var w))
                    for (int i = 0; i < d.Segments.Count; i++)
                        if (w.Segments[i]?.BuiltOn != null)
                            built.Add((new HashSet<(int Q, int R)>(d.Segments[i].Hexes.Select(h => (h[0], h[1]))),
                                d.Kind == Corridors.Rail ? m.RailMoveCost!.Value : m.RoadMoveCost!.Value));
            return built;
        }

        /// <summary>R-MOVE-001, R-MOVE-002, R-MOVE-004, R-MOVE-005, R-MOVE-007: the cheapest cost of every hex within
        /// <paramref name="budget"/> points from the unit's hex, and the hex each is entered from. A path enters only land hexes,
        /// at their biome's cost or, from a hex of the same built corridor segment, at the kind's cost when that is lower, and
        /// never an enemy unit's hex or a closed enemy-held locality's (<see cref="Closed"/>); passing next to either costs nothing extra (no zone of
        /// control, Q-MOVE-4). Hexes leave the frontier in (cost, q, r) order and neighbours are tried in the fixed
        /// <see cref="Neighbours"/> order, so a hex reached at the same cost twice keeps the first, and the path is the same
        /// every run. With <paramref name="fresh"/>, one neighbouring hex whose cost exceeds the budget costs the whole budget
        /// (R-MOVE-001, OQ-3). Stacking limits only where a move ends, so it is not checked here.</summary>
        static (Dictionary<(int Q, int R), int> Dist, Dictionary<(int Q, int R), (int Q, int R)> Prev) Search(
            Content c, GameState s, Unit u, int budget, bool fresh = false)
        {
            var costs = c.RulesAt(s.Date).Mvp!.MoveCost;   // the loader requires a cost for every land biome in the GDD loop
            var blocked = Closed(c, s, IsEnemy(u));
            var built = BuiltSegments(c, s);
            foreach (var x in s.Units.Values) if (IsEnemy(x) != IsEnemy(u)) blocked.Add((x.Q, x.R));
            var dist = new Dictionary<(int Q, int R), int> { [(u.Q, u.R)] = 0 };
            var prev = new Dictionary<(int Q, int R), (int Q, int R)>();
            var frontier = new SortedSet<(int Cost, int Q, int R)> { (0, u.Q, u.R) };
            while (frontier.Count > 0)
            {
                var (cost, q, r) = frontier.Min;
                frontier.Remove(frontier.Min);
                foreach (var (dq, dr) in Neighbours)
                {
                    var next = (q + dq, r + dr);
                    if (!(c.HexAt(next.Item1, next.Item2) is MapHex hex) || !hex.Land || blocked.Contains(next)) continue;
                    var step = costs[hex.Biome];
                    foreach (var (hexes, kindCost) in built)
                        if (kindCost < step && hexes.Contains((q, r)) && hexes.Contains(next)) step = kindCost;
                    var total = cost + step;
                    if (fresh && cost == 0 && total > budget) total = budget;   // R-MOVE-001: with full points a unit can always move one hex (OQ-3)
                    if (total > budget || (dist.TryGetValue(next, out var known) && known <= total)) continue;
                    if (dist.ContainsKey(next)) frontier.Remove((known, next.Item1, next.Item2));
                    dist[next] = total;
                    prev[next] = (q, r);
                    frontier.Add((total, next.Item1, next.Item2));
                }
            }
            return (dist, prev);
        }
    }
}
