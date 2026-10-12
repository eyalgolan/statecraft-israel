using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class PoliticsSystem
    {
        public const double CollapseBelow = 0.2;

        public static (int seats, int size) CoalitionSeats(Content c, DateTime date)
        {
            var leg = Offices.Legislature(c, date);
            var gov = Offices.Government(c, date);
            if (leg == null || gov == null) return (0, 0);
            return (gov.CoalitionOn(date).Sum(p => leg.Seats.TryGetValue(p, out var n) ? n : 0), leg.Size);
        }

        public static bool HasMajority(Content c, DateTime date)
        {
            var (seats, size) = CoalitionSeats(c, date);
            return size > 0 && seats * 2 > size;
        }

        public static bool BudgetNeedsVote(Content c, DateTime date) => Offices.Legislature(c, date)?.BudgetApproval ?? false;

        /// <summary>No majority: fails. Majority with stability >= 0.5: passes. Otherwise passes with probability 2 * stability.</summary>
        public static bool Vote(Content c, GameState s, ref Rng rng)
        {
            if (!HasMajority(c, s.Date)) return false;
            return s.CoalitionStability >= 0.5 || rng.NextDouble() < 2 * s.CoalitionStability;
        }

        public static void UpdateStability(Content c, GameState s, double yearFraction, List<Cause> why)
        {
            var old = s.CoalitionStability;
            s.CoalitionStability = old + (EffectsSystem.Approval(c, s) - old) * Math.Min(1.0, yearFraction * 2);
            if (Math.Abs(s.CoalitionStability - old) > 1e-9)
                why.Add(new Cause("politics", "coalition_stability", s.CoalitionStability - old, $"coalition stability {PlayerText.PercentChange(s.CoalitionStability - old)} (public approval)"));
            if (s.CoalitionStability < CollapseBelow && s.GameOver == null)
            {
                s.GameOver = GameOverCodes.CoalitionCollapse;
                why.Add(new Cause("politics", "game_over", 0, "The coalition collapsed. Your premiership is over."));
            }
        }

        public static void ReportChanges(Content c, DateTime from, DateTime to, List<Cause> why)
        {
            var l0 = Offices.Legislature(c, from); var l1 = Offices.Legislature(c, to);
            if (l1 != null && l1 != l0)
            {
                var (seats, size) = CoalitionSeats(c, to);
                var elected = l1.Elected.HasValue ? $", elected {PlayerText.Date(l1.Elected.Value)}" : "";
                why.Add(new Cause("politics", l1.Id, 0, FormattableString.Invariant($"{l1.Body} convened ({l1.Size} seats{elected}); coalition holds {seats}/{size}")));
            }
            var g0 = Offices.Government(c, from); var g1 = Offices.Government(c, to);
            var tookOffice = g1 != null && g1 != g0;
            var reported = false;
            void TookOffice() { reported = true; why.Add(new Cause("politics", g1!.Id, 0, $"{g1.Name} took office ({string.Join(", ", PartyNames(c, g1.CoalitionOn(g1.From)))})")); }
            // Changes inside a term, in date order; a government taking office comes before the changes dated on or after its start.
            var changes = c.Governments.SelectMany(g => g.CoalitionChanges.Select(ch => (g, ch)))
                .Where(x => x.ch.Date > from && x.ch.Date <= to).OrderBy(x => x.ch.Date);
            foreach (var (g, ch) in changes)
            {
                if (tookOffice && !reported && ch.Date >= g1!.From) TookOffice();
                var (seats, size) = CoalitionSeats(c, ch.Date);
                var what = new List<string>();
                if (ch.Leaves.Count > 0) what.Add($"{string.Join(", ", PartyNames(c, ch.Leaves))} left the coalition");
                if (ch.Joins.Count > 0) what.Add($"{string.Join(", ", PartyNames(c, ch.Joins))} joined the coalition");
                why.Add(new Cause("politics", g.Id, 0, FormattableString.Invariant($"{string.Join("; ", what)} (coalition holds {seats}/{size})")));
            }
            if (tookOffice && !reported) TookOffice();
        }

        static IEnumerable<string> PartyNames(Content c, IEnumerable<string> ids) => ids.Select(id => c.Parties.First(p => p.Id == id).Name);
    }
}
