using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Sending a unit home after the armistices (GDD Phase 7, #1309; docs/rules/mvp.md R-UNIT-005): the player's
    /// order "Send home" (owner, 22 October 2026, #1309), allowed only after the armistice on the unit's front; the unit
    /// leaves the map and its strength, in population units, joins the nearest of the twelve cities as civilians without a
    /// job (owner: "nearest ... as unemployed civilians"). No automatic demobilisation: the order is the only way home.</summary>
    public static class Demobilization
    {
        public const string SendHome = "send_home";

        /// <summary>The armistice event that signs each front (content/events/armistice_<front>.yaml); the sign option is
        /// `sign` in all four.</summary>
        static bool Signed(GameState s, string front) =>
            s.ChosenOptions.TryGetValue("armistice_" + front, out var o) && o == "sign";

        /// <summary>The front <paramref name="u"/> fights on (owner, 22 October 2026, #1309): of the enemy units it attacks
        /// (the hex its attack order names) or borders (an adjacent hex), the ones whose side belongs to a front (R-POW-004's
        /// map; the ALA and the local forces belong to none, Q-OBJ-1, so they name no front - accepted per maintainer comment 6341877062 on #1309), the nearest
        /// one's, ties by the lowest unit id. Null when no front-bearing enemy unit is attacked or bordered: the ruling's
        /// "no enemy units in play" case, which any signed armistice covers (accepted per maintainer comment 6341877062
        /// on #1309: a frontless enemy counts as none in play).</summary>
        internal static string? Front(GameState s, Unit u)
        {
            (int Q, int R)? attacks = null;
            if (u.Order == Combat.Attack)
            {
                var parts = u.OrderTarget.Split(',');
                attacks = (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
            }
            var candidates = s.Units.Values
                .Where(e => e.Side != "" && e.Side != Movement.PlayerSide && Prisoners.FrontOf.ContainsKey(e.Side))
                .Where(e => MapActions.Distance(u.Q, u.R, e.Q, e.R) == 1 || (attacks != null && attacks.Value == (e.Q, e.R)))
                .ToList();
            if (candidates.Count == 0) return null;
            var pick = candidates.OrderBy(e => MapActions.Distance(u.Q, u.R, e.Q, e.R)).ThenBy(e => e.Id).First();
            return Prisoners.FrontOf[pick.Side];
        }

        /// <summary>R-UNIT-005's refusals: null when <see cref="Give"/> takes the order, else the reason shown to the player.
        /// Changes nothing: the unit card asks it before it offers Send home. Refused for an unknown unit, a unit with no
        /// side (a halutz) or an enemy unit, as the standing orders are; then while the armistice on the unit's front is
        /// unsigned, or - no enemy attacked or bordered - while no armistice is signed at all.</summary>
        public static string? Check(Content c, GameState s, int unitId)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (u.Side == "") return "Only a military unit takes that order";
            if (u.Side != Movement.PlayerSide) return "That unit is not yours";
            var front = Front(s, u);
            if (front != null)
                return Signed(s, front) ? null : $"The armistice with {PlayerText.Capitalised(front)} is not signed";
            return Prisoners.Fronts.Any(f => Signed(s, f)) ? null : "No armistice is signed";
        }

        /// <summary>R-UNIT-005: sends the unit home at once, like CancelTraining returns a cohort (R-TRAIN-002): the unit
        /// leaves the map and its strength joins the nearest city's civilians, employed count untouched, so the people count
        /// in the city's jobs and housing rules from the same turn. Refused, state untouched, with <see cref="Check"/>'s
        /// reason.</summary>
        internal static string? Give(Content c, GameState s, int unitId, List<Cause> why)
        {
            var refused = Check(c, s, unitId);
            if (refused != null) return refused;
            var u = s.Units[unitId];
            var city = Nearest(c, u.Q, u.R);
            s.Units.Remove(unitId);
            var x = s.Cities[city];
            s.Cities[city] = x.WithCounts(x.Civilians + u.Strength, x.Employed, x.Refugees);
            var ppu = c.RulesAt(s.Date).Mvp!.PeoplePerUnit;
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), u.Strength, FormattableString.Invariant(
                $"Unit {unitId} is sent home: its {u.Strength * ppu:N0} people are civilians in {PlayerText.Settlement(c, city)} now")));
            return null;
        }

        /// <summary>The nearest of the cities by hex distance to its settlement hex; a tie takes the city's id in ordinal
        /// order (agent default - the ruling sets no tie break).</summary>
        static string Nearest(Content c, int q, int r) => c.Cities
            .OrderBy(x => { var h = c.SettlementHexes.First(h => h.Id == x.Id); return MapActions.Distance(q, r, h.Q, h.R); })
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .First().Id;
    }
}
