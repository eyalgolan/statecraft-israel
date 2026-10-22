using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Standing orders in the GDD v0.1 loop (Phase 5 PR 7; docs/rules/mvp.md R-ORDER-001 to R-ORDER-003). A unit's
    /// order is fortify or hold. Fortify takes a turn to set, is free and is lost when the unit leaves its hex (Q-HOLD-1); hold
    /// acts on the unit's own hex only and intercepts nothing (Q-HOLD-2). The order is free (D-A6).</summary>
    public static class Orders
    {
        public const string Kind = "unit_order";
        public const string Fortify = "fortify";
        public const string Hold = "hold";

        /// <summary>Fortify given this turn: the unit's Order until the turn's Settle step sets it (R-ORDER-002). Combat
        /// (resolver step 2) runs before that step, so it never sees a fortify given the same turn.</summary>
        public const string Fortifying = "fortifying";

        /// <summary>The player's side (units_1948.yaml's id). Only the player's military units take orders.</summary>
        const string PlayerSide = "israel";

        /// <summary>The hex a fortify order names, as Unit.OrderTarget holds it: "q,r".</summary>
        static string Hex(Unit u) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", u.Q, u.R);

        static Unit With(Unit u, string order, string target) =>
            new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, order, target);

        /// <summary>R-ORDER-001's refusals: null when <see cref="Give"/> takes the order, else the reason shown to the player.
        /// Changes nothing: the unit card asks it before it offers fortify or hold (#754). Refused for an unknown order or
        /// unit, a unit with no side (a halutz), an enemy unit, hold for artillery (R-COMBAT-007: it never holds ground,
        /// Q-ARTY-1), or the order it already has. With <paramref name="enemy"/> the enemy AI orders its own unit through the
        /// same checks, the sides swapped (R-ENEMY-001). The legacy loop's refusal is Simulation.Step's, before Give, so Check
        /// does not give it.</summary>
        public static string? Check(Content c, GameState s, int unitId, string order, bool enemy = false)
        {
            if (order != Fortify && order != Hold) return "That order is not known";
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (u.Side == "") return "Only a military unit takes that order";
            if ((u.Side != PlayerSide) != enemy) return "That unit is not yours";
            if (order == Hold && c.UnitTypes.FirstOrDefault(t => t.Id == u.Type)?.Class == Combat.ArtilleryClass) return "Artillery does not hold ground";
            var already = order == Hold ? u.Order == Hold : (u.Order == Fortify || u.Order == Fortifying) && u.OrderTarget == Hex(u);
            return already ? "That unit already has that order" : null;
        }

        /// <summary>R-ORDER-001: gives the unit its standing order, replacing any other. Fortify names the unit's hex and starts
        /// as Fortifying; hold names nothing, as it acts where the unit is. Refused, state untouched, with <see cref="Check"/>'s
        /// reason.</summary>
        internal static string? Give(Content c, GameState s, int unitId, string order, List<Cause> why, bool enemy = false)
        {
            var refused = Check(c, s, unitId, order, enemy);
            if (refused != null) return refused;
            var u = s.Units[unitId]; var here = Hex(u);
            s.Units[unitId] = order == Fortify ? With(u, Fortifying, here) : With(u, Hold, "");
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), 0, order == Fortify
                ? $"Unit {unitId} is fortifying hex {here}; it is fortified from the end of the turn"
                : $"Unit {unitId} holds hex {here}", enemy ? order : ""));
            return null;
        }

        /// <summary>R-ORDER-001, R-ORDER-002, in the resolver after combat: in id order, a unit no longer on the hex its fortify
        /// names loses the order (moving clears fortify), and a fortify given this turn on the unit's hex is set.</summary>
        public static void Settle(GameState s, List<Cause> why)
        {
            foreach (var u in s.Units.Values.ToList())   // Units is a SortedList: ascending id
            {
                if (u.Order != Fortify && u.Order != Fortifying) continue;
                if (u.OrderTarget != Hex(u))
                {
                    s.Units[u.Id] = With(u, "", "");
                    if (u.Order == Fortify)
                        why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0,
                            $"Unit {u.Id} left hex {u.OrderTarget} and is no longer fortified"));
                }
                else if (u.Order == Fortifying) s.Units[u.Id] = With(u, Fortify, u.OrderTarget);
            }
        }

        /// <summary>R-ORDER-002, R-ORDER-003: the unit's defence in percent of its strength, for combat (Phase 5 PR 8): the
        /// fortify percent while it is fortified on the hex its order names, the hold percent while it holds, else 100.</summary>
        public static int DefencePercent(MvpRules r, Unit u)
        {
            if (u.Order == Fortify && u.OrderTarget == Hex(u)) return r.FortifyDefencePercent!.Value;
            if (u.Order == Hold) return r.HoldDefencePercent!.Value;
            return 100;
        }
    }
}
