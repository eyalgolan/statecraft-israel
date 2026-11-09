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
        internal static bool Signed(GameState s, string front) =>
            s.ChosenOptions.TryGetValue("armistice_" + front, out var o) && o == "sign";

        /// <summary>The front <paramref name="u"/> fights on (owner, 22 October 2026, #1309): of the enemy units it attacked
        /// this turn or borders (an adjacent hex), the ones whose side belongs to a front (R-POW-004's
        /// map; the ALA and the local forces belong to none, Q-OBJ-1, so they name no front - accepted per maintainer comment 6341877062 on #1309), the nearest
        /// one's, ties by the lowest unit id. Null when no front-bearing enemy unit is attacked or bordered: the ruling's
        /// "no enemy units in play" case, which any signed armistice covers (accepted per maintainer comment 6341877062
        /// on #1309: a frontless enemy counts as none in play).</summary>
        internal static string? Front(GameState s, Unit u, IReadOnlyDictionary<int, AttackRecord>? attacked = null)
        {
            var candidates = s.Units.Values
                .Where(e => e.Side != "" && e.Side != Movement.PlayerSide && Prisoners.FrontOf.ContainsKey(e.Side))
                .Where(e => MapActions.Distance(u.Q, u.R, e.Q, e.R) == 1)
                .Select(e => (Distance: 1, Unit: e.Id, Front: Prisoners.FrontOf[e.Side]))
                .ToList();
            if (attacked != null && attacked.TryGetValue(u.Id, out var target)
                && target.Unit is int targetUnit && target.Front is string targetFront)
                candidates.Add((MapActions.Distance(u.Q, u.R, target.Q, target.R), targetUnit, targetFront));
            if (candidates.Count == 0) return null;
            return candidates.OrderBy(e => e.Distance).ThenBy(e => e.Unit).First().Front;
        }

        /// <summary>Send home's refusal while no armistice is signed (#2707: a constant, for the UI's string).</summary>
        public const string RefusedNoArmistice = "No armistice is signed";

        /// <summary>R-UNIT-005's refusals: null when <see cref="Give"/> takes the order, else the reason shown to the player.
        /// Changes nothing: the unit card asks it before it offers Send home. Refused for an unknown unit, a unit with no
        /// side (a halutz) or an enemy unit, as the standing orders are; then while the armistice on the unit's front is
        /// unsigned, or - no enemy attacked or bordered - while no armistice is signed at all.</summary>
        public static string? Check(Content c, GameState s, int unitId,
                                    IReadOnlyDictionary<int, AttackRecord>? attacked = null)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (u.Side == "") return "Only a military unit takes that order";
            if (u.Side != Movement.PlayerSide) return "That unit is not yours";
            var front = Front(s, u, attacked);
            if (front != null)
                return Signed(s, front) ? null : $"The armistice with {PlayerText.Capitalised(front)} is not signed";
            return Prisoners.Fronts.Any(f => Signed(s, f)) ? null : RefusedNoArmistice;
        }

        /// <summary>R-UNIT-005: sends the unit home at once, like CancelTraining returns a cohort (R-TRAIN-002): the unit
        /// leaves the map and its strength joins the nearest city's civilians, employed count untouched, so the people count
        /// in the city's jobs and housing rules from the same turn. Refused, state untouched, with <see cref="Check"/>'s
        /// reason. On a Historical ledger game (#1725 PR 7) the formation's soldiers are records instead: see
        /// <see cref="LedgerSendHome"/>.</summary>
        internal static string? Give(Content c, GameState s, int unitId,
                                     IReadOnlyDictionary<int, AttackRecord>? attacked, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the soldiers sent home are records
            var refused = Check(c, s, unitId, attacked);
            if (refused != null) return refused;
            var u = s.Units[unitId];
            var city = Nearest(c, u.Q, u.R);
            var back = 0;
            if (s.PopulationLedger.IsHistorical)
            {
                refused = LedgerSendHome(c, s, u, city, out back);
                if (refused != null) return refused;
            }
            else
            {
                var x = s.Cities[city];
                s.Cities[city] = x.WithCounts(x.Civilians + u.Strength, x.Employed, x.Refugees);
            }
            s.Units.Remove(unitId);
            var ppu = c.RulesAt(s.Date).Mvp!.PeoplePerUnit;
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), u.Strength, FormattableString.Invariant(
                $"Unit {unitId} is sent home: its {u.Strength * ppu:N0} people are civilians in {PlayerText.Settlement(c, city)} now"),
                key: WhyKeys.SentHome, units: new[] { new UnitRef(u) },
                args: new[] { (u.Strength * ppu).ToString(CultureInfo.InvariantCulture), city }));
            if (back > 0)   // the ledger path only (owner, 8 November 2026: "Yes, one Why line (Recommended)"), as Training's Cancel writes it
                why.Add(new Cause("city", "arms", back, FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: unit {unitId} returned {back} Arms"), key: CauseKeys.SentHomeArmsReturned, args: new[] { city, CauseKeys.Number(unitId) }));
            return null;
        }

        /// <summary>R-UNIT-005 on a Historical ledger game (#1699, #1725 PR 7; owner, 8 November 2026, #1725
        /// issuecomment-6726421643 and issuecomment-6727007834). "Every soldier in it (Recommended)": every active-soldier record
        /// serving the formation's target (<see cref="Training.FormationTarget"/>) leaves service. "Refuse with a reason
        /// (Recommended)": when the formation's strength differs from those records, the order is refused with a plain reason
        /// and nothing changes. "Nearest city, unemployed (Recommended)": each record becomes a free civilian without an
        /// assignment located in <paramref name="city"/> (its region plus the city), so the city houses it from the same turn.
        /// "Settled in (Recommended)": its reception ticks are reception.turns. "Keep both (Recommended)": its military
        /// qualification and combat XP stay. "Back to Arms stock (Recommended)": its issued military item leaves the record and
        /// goes back to the stock once. "No, records only (Recommended)": no aggregate city field is written. The records are
        /// written in one validated commit; the caller removes the unit after it and, for "Yes, one Why line (Recommended)",
        /// writes one Why line naming the <paramref name="back"/> Arms returned. Null when done, else the reason.</summary>
        static string? LedgerSendHome(Content c, GameState s, Unit u, string city, out int back)
        {
            back = 0;
            var target = Training.FormationTarget(c, u.Id);
            var serving = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.ActiveSoldier
                && string.Equals(p.Assignment.Target, target, StringComparison.Ordinal)).ToList();
            if (serving.Count != u.Strength)
                return string.Format(CultureInfo.InvariantCulture, Training.LedgerRefusedStrength, u.Id, serving.Count, u.Strength);
            var region = c.PlayerRegions.AtLocation(city)?.Id
                ?? throw new InvalidOperationException("No region in regions.yaml for " + city + " (R-POP-004).");
            var location = new PopulationLocation(region, city: city);
            var received = c.RequirePopulationInteger("reception.turns");
            var items = 0;
            var civilians = serving.Select(p =>
            {
                var e = p.Equipment;
                if (e.MilitaryId != null) items++;
                return new PopulationUnit(p.Id, PopulationStatus.FreeCivilian, received, location, PopulationAssignment.Unassigned,
                    p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp, new PopulationEquipment(e.ToolsId, e.ToolsPaid),
                    p.Captor, p.Home);
            }).ToList();
            s.PopulationLedger.Commit(civilians, Array.Empty<PopulationTarget>());
            back = (int)Math.Min(items, (long)int.MaxValue - s.Arms);   // the stock stops at int.MaxValue, as Training's cancel leaves it
            s.Arms += back;
            return null;
        }

        /// <summary>The nearest of the cities by hex distance to its settlement hex; a tie takes the city's id in ordinal
        /// order (agent default - the ruling sets no tie break).</summary>
        internal static string Nearest(Content c, int q, int r) => c.Cities
            .OrderBy(x => { var h = c.SettlementHexes.First(h => h.Id == x.Id); return MapActions.Distance(q, r, h.Q, h.R); })
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .First().Id;
    }
}
