using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Combat in the GDD v0.1 loop (Phase 5 PR 8; docs/rules/mvp.md R-COMBAT-001 to R-COMBAT-006). A unit given
    /// UnitOrder(unit, attack, q, r) attacks the adjacent hex (q, r) in resolver step 2 of every turn until its hex holds no
    /// enemy unit next to it (spec 2026-10-12 section 2.3: a standing order). Units of one side attacking one hex are one
    /// engagement, their strengths summed (Q-COMBAT-2); it draws once from the RNG and reads rules.yaml mvp.combat_table
    /// (Q-COMBAT-1). Combat changes the units and the Arms only (R-COMBAT-005): no resident, city, locality or score is ever
    /// a target or an outcome (Q-CIV-2, impact plan section 4.3).</summary>
    public static class Combat
    {
        public const string Attack = "attack";

        /// <summary>RNG values each engagement draws, whatever its outcome (docs/determinism.md, "Combat draws").</summary>
        public const int DrawsPerEngagement = 1;

        /// <summary>The player's team: Israel's units and the halutzim (no side). Every other side is the enemy, and the Arab
        /// sides fight on one team (Movement's reading of "the enemy").</summary>
        static bool Player(Unit u) => u.Side == "" || u.Side == Movement.PlayerSide;

        /// <summary>Only military units fight (R-UNIT-001): a halutz is never a target or an attacker.</summary>
        static bool Military(Unit u) => u.Side != "";

        static string Hex(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);

        static bool Adjacent(Unit u, int q, int r) => Movement.Neighbours.Any(d => u.Q + d.Q == q && u.R + d.R == r);

        /// <summary>The enemy military units on (q, r), seen from <paramref name="attacker"/>'s team, in id order.</summary>
        static List<Unit> Defenders(GameState s, Unit attacker, int q, int r) =>
            s.Units.Values.Where(x => x.Q == q && x.R == r && Military(x) && Player(x) != Player(attacker)).ToList();

        /// <summary>R-COMBAT-001: gives the player's military unit a standing attack on the adjacent hex (q, r), replacing its
        /// order (a fortify is lost). Refused, state untouched, for an unknown unit, a halutz, an enemy unit, no hex, the attack
        /// it already has, a hex not next to it or with no enemy unit, or fewer Arms than one attack takes on top of the
        /// player's other attacks already ordered (Q-COMBAT-4). It costs no Attention (D-A6) and resolves in this turn's step 2.</summary>
        internal static string? Give(Content c, GameState s, int unitId, int? q, int? r, List<Cause> why)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (!Military(u)) return "Only a military unit takes that order";
            if (!Player(u)) return "That unit is not yours";
            if (q == null || r == null) return "An attack names a hex";
            var target = Hex(q.Value, r.Value);
            if (u.Order == Attack && u.OrderTarget == target) return "That unit already has that order";
            if (!Adjacent(u, q.Value, r.Value)) return "That hex is not next to the unit";
            if (Defenders(s, u, q.Value, r.Value).Count == 0) return "There is no enemy unit on that hex";
            var need = c.RulesAt(s.Date).Mvp!.ArmsPerAttack!.Value;
            // Arms are paid at resolution, so the stock must also cover the player's other standing attacks, this turn's
            // included; the unit's own attack, which this order replaces, is not counted.
            var ordered = s.Units.Values.Count(x => x.Id != unitId && x.Order == Attack && Military(x) && Player(x));
            if (s.Arms < need * (ordered + 1)) return Short(need, s.Arms, ordered * need);
            s.Units[unitId] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Attack, target);
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), 0, $"Unit {unitId} will attack hex {target}"));
            return null;
        }

        static string Short(int need, int have, int committed) => committed == 0
            ? FormattableString.Invariant($"An attack needs {need} Arms and the stock has {have}")
            : FormattableString.Invariant($"An attack needs {need} Arms and the stock has {have}, of which {committed} go to attacks already ordered");

        /// <summary>The attack preview (spec section 2.6; Q-UI-1): the ratio, in percent and rounded down, of unit
        /// <paramref name="unitId"/>'s strength plus that of the units of its team already ordered to attack (q, r) from next to
        /// it, against the enemy units there after fortify and terrain, and the combat_table row it falls in. Null when (q, r)
        /// is not next to the unit or holds no enemy unit, or the unit is unknown or no military unit.</summary>
        public static (int RatioPercent, int Row)? Preview(Content c, GameState s, int unitId, int q, int r)
        {
            if (!s.Units.TryGetValue(unitId, out var u) || !Military(u) || !Adjacent(u, q, r)) return null;
            var defenders = Defenders(s, u, q, r);
            if (defenders.Count == 0) return null;
            var target = Hex(q, r);
            var attackers = s.Units.Values.Where(x => x.Id == unitId
                || (x.Order == Attack && x.OrderTarget == target && Military(x) && Player(x) == Player(u) && Adjacent(x, q, r))).ToList();
            var mvp = c.RulesAt(s.Date).Mvp!;
            var ratio = Ratio(c, mvp, attackers, defenders, q, r);
            return (ratio, Row(mvp, ratio));
        }

        /// <summary>R-COMBAT-002: the attackers' summed strength over the defenders' summed strength, each defender's strength
        /// times its order's defence percent (Orders.DefencePercent) and its hex's biome percent, in whole percent rounded down.
        /// Integer arithmetic only, so it is the same on every runtime.</summary>
        static int Ratio(Content c, MvpRules mvp, List<Unit> attackers, List<Unit> defenders, int q, int r)
        {
            long attack = attackers.Sum(x => (long)x.Strength);
            var terrain = mvp.CombatTerrainPercent[c.HexAt(q, r)!.Biome];   // a unit stands on land, and the loader requires every land biome
            long defence = defenders.Sum(x => (long)x.Strength * Orders.DefencePercent(mvp, x) * terrain);   // in 1/10,000 of a strength unit
            return defence == 0 ? int.MaxValue : (int)Math.Min(int.MaxValue, attack * 1_000_000 / defence);   // 0: no real unit has no strength
        }

        /// <summary>The last combat_table row whose min_ratio_percent the ratio reaches (the first row starts at 0).</summary>
        static int Row(MvpRules mvp, int ratio)
        {
            var row = 0;
            for (int i = 1; i < mvp.CombatTable.Count; i++) if (ratio >= mvp.CombatTable[i].MinRatioPercent) row = i;
            return row;
        }

        /// <summary>Resolver step 2 (R-COMBAT-002 to R-COMBAT-006): in ascending attacker id, each unit with an attack order not
        /// yet in an engagement this turn leads one, with every unit of its team attacking the same hex. A unit no longer next
        /// to its hex, or whose hex holds no enemy unit, ends its attack; a player's unit pays mvp.arms_per_attack when it
        /// joins, and one the stock cannot pay for has its attack called off (Q-COMBAT-4). Enemy units pay nothing (Q-ENEMY-4:
        /// no supply). An engagement with an attacker left draws once. Afterwards every attack whose hex is now empty ends.</summary>
        public static void Resolve(Content c, GameState s, List<Cause> why)
        {
            var mvp = c.RulesAt(s.Date).Mvp!;
            var done = new HashSet<int>();
            foreach (var id in s.Units.Keys.ToList())   // Units is a SortedList: ascending id
            {
                if (done.Contains(id) || !s.Units.TryGetValue(id, out var lead) || lead.Order != Attack) continue;
                var (q, r) = Target(lead);
                var group = s.Units.Values.Where(x => !done.Contains(x.Id) && x.Order == Attack && x.OrderTarget == lead.OrderTarget && Player(x) == Player(lead)).ToList();
                var defenders = Defenders(s, lead, q, r);
                var attackers = new List<Unit>();
                foreach (var x in group)
                {
                    done.Add(x.Id);
                    if (defenders.Count == 0 || !Adjacent(x, q, r)) { End(s, x, $"Unit {x.Id}'s attack on hex {x.OrderTarget} ends: no enemy unit is there", why); continue; }
                    if (Player(x))
                    {
                        var need = mvp.ArmsPerAttack!.Value;
                        if (s.Arms < need)
                        {
                            End(s, x, $"Unit {x.Id}'s attack on hex {x.OrderTarget} is called off: it needs {need.ToString(CultureInfo.InvariantCulture)} Arms and the stock has {s.Arms.ToString(CultureInfo.InvariantCulture)}", why);
                            continue;
                        }
                        s.Arms -= need;
                    }
                    attackers.Add(x);
                }
                if (attackers.Count > 0) Engage(c, s, mvp, attackers, defenders, q, r, why);
            }
            foreach (var u in s.Units.Values.ToList())
                if (u.Order == Attack)
                {
                    var (q, r) = Target(u);
                    if (Defenders(s, u, q, r).Count == 0 || !Adjacent(u, q, r))
                        End(s, u, $"Unit {u.Id}'s attack on hex {u.OrderTarget} ends: no enemy unit is there", why);
                }
        }

        static (int Q, int R) Target(Unit u)
        {
            var parts = u.OrderTarget.Split(',');
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        static void End(GameState s, Unit u, string line, List<Cause> why)
        {
            s.Units[u.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, "", "");
            why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0, line));
        }

        /// <summary>R-COMBAT-002, R-COMBAT-003, R-COMBAT-006: one engagement. The face is the one draw; its row's losses fall on
        /// each side one population unit at a time on its strongest unit (a tie on the lower id), a unit at 0 leaves the state,
        /// and when the row says so each defender left retreats one hex, in id order, or surrenders when it has nowhere to go.
        /// The attackers stay where they are (no advance, Q-COMBAT-3).</summary>
        static void Engage(Content c, GameState s, MvpRules mvp, List<Unit> attackers, List<Unit> defenders, int q, int r, List<Cause> why)
        {
            var ratio = Ratio(c, mvp, attackers, defenders, q, r);
            var row = mvp.CombatTable[Row(mvp, ratio)];
            var rng = new Rng(s.RngState);
            var face = (int)(rng.NextDouble() * row.AttackerLoss.Count);   // DrawsPerEngagement
            s.RngState = rng.State;
            var target = Hex(q, r);
            var ppu = mvp.PeoplePerUnit;
            var removed = new List<int>();
            var attackersLost = Take(s, attackers.Select(x => x.Id).ToList(), row.AttackerLoss[face], removed);
            var defendersLost = Take(s, defenders.Select(x => x.Id).ToList(), row.DefenderLoss[face], removed);
            why.Add(new Cause("map", target, 0, FormattableString.Invariant(
                $"The attack on hex {target} ({Ids(attackers)} against {Ids(defenders)}, strength ratio {ratio}%): the attackers lost {attackersLost * ppu:N0} people and the defenders lost {defendersLost * ppu:N0} people")));
            foreach (var id in removed)
                why.Add(new Cause("map", id.ToString(CultureInfo.InvariantCulture), 0, $"Unit {id} was destroyed"));
            if (row.DefenderRetreats[face] != 1) return;
            foreach (var d in defenders)
            {
                if (!s.Units.TryGetValue(d.Id, out var u)) continue;
                if (Retreat(c, s, mvp, u) is (int, int) to)
                {
                    s.Units[u.Id] = new Unit(u.Id, u.Type, to.Item1, to.Item2, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget);
                    why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0, $"Unit {u.Id} retreated to hex {Hex(to.Item1, to.Item2)}"));
                }
                else
                {
                    // The only way a unit is captured (impact plan section 4.3 item 1); the prisoners themselves are Phase 5 PR 12.
                    s.Units.Remove(u.Id);
                    why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0,
                        FormattableString.Invariant($"Unit {u.Id} surrendered with {u.Strength * ppu:N0} people: it had no hex to retreat to")));
                }
            }
        }

        static string Ids(List<Unit> units) => (units.Count == 1 ? "unit " : "units ") + string.Join(", ", units.Select(x => x.Id.ToString(CultureInfo.InvariantCulture)));

        /// <summary>Takes up to <paramref name="loss"/> population units from <paramref name="ids"/>, one at a time from the
        /// strongest unit left (a tie on the lower id; the list is in id order), removing each that reaches 0 (its id is never
        /// reused: NextId is not touched) and adding it to <paramref name="removed"/>. Returns the units taken.</summary>
        static int Take(GameState s, List<int> ids, int loss, List<int> removed)
        {
            int taken = 0;
            for (; taken < loss; taken++)
            {
                Unit? top = null;
                foreach (var id in ids)
                    if (s.Units.TryGetValue(id, out var x) && (top == null || x.Strength > top.Strength)) top = x;
                if (top == null) break;
                if (top.Strength <= 1) { s.Units.Remove(top.Id); removed.Add(top.Id); }
                else s.Units[top.Id] = new Unit(top.Id, top.Type, top.Q, top.R, top.Service, top.Side, top.Strength - 1, top.Order, top.OrderTarget);
            }
            return taken;
        }

        /// <summary>R-COMBAT-003: the first neighbour of the unit's hex, in the fixed order (R-MOVE-002), that is land, holds no
        /// unit of the other team, has room under mvp.stack_limit for the unit's side (R-MOVE-003) and, for the player's unit,
        /// is not an enemy-held locality (R-MOVE-005); null when there is none.</summary>
        static (int, int)? Retreat(Content c, GameState s, MvpRules mvp, Unit u)
        {
            var noEntry = Player(u) ? Movement.NoEntry(c, s) : new HashSet<(int Q, int R)>();
            foreach (var (dq, dr) in Movement.Neighbours)
            {
                int q = u.Q + dq, r = u.R + dr;
                if (!(c.HexAt(q, r) is MapHex hex) || !hex.Land || noEntry.Contains((q, r))) continue;
                var here = s.Units.Values.Where(x => x.Q == q && x.R == r).ToList();
                if (here.Any(x => Player(x) != Player(u)) || here.Count(x => x.Side == u.Side) >= mvp.StackLimit!.Value) continue;
                return (q, r);
            }
            return null;
        }
    }
}
