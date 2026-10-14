using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 cities (docs/GDD.md "Cities and construction"; Phase 1 PR 3): civilians and refugees are counts
    /// per city (D-A2), in population units (D-A1). Runs only when eras.yaml systems.loop is gdd.</summary>
    public static class Cities
    {
        public const string Kind = "assign_workers";

        /// <summary>Fills the state's cities from cities.yaml, with no refugees yet. Does nothing in the legacy loop.</summary>
        public static void Init(Content c, GameState s)
        {
            if (!c.IsGdd) return;
            foreach (var x in c.Cities) s.Cities[x.Id] = new City(x.Id, x.HousingCapacity, x.JobSlots, x.Civilians, x.Employed, 0);
            UpdateCohesion(s, c.RulesAt(s.Date).Mvp!);
        }

        /// <summary>R-CITY-001: sets the number of the city's civilians in jobs (a standing order). Refused, state untouched,
        /// when the city or role is unknown or the count is negative, above the job slots or above the civilians. Step has
        /// already refused it in the legacy loop.</summary>
        public static string? Assign(Content c, GameState s, string city, string role, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            if (role != "job") return "There is no such role";   // craftsman: Phase 3 (D-D4)
            if (count < 0) return "The number of workers cannot be negative";
            if (count > x.JobSlots) return FormattableString.Invariant($"{PlayerText.Settlement(c, city)} has {x.JobSlots} jobs");
            if (count > x.Civilians) return FormattableString.Invariant($"{PlayerText.Settlement(c, city)} has {x.Civilians} civilians");
            s.Cities[city] = x.WithCounts(x.Civilians, count, x.Refugees);
            why.Add(new Cause("city", city, count - x.Employed, FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: {count} groups in work")));
            return null;
        }

        /// <summary>R-CITY-002: each city's employed units earn income_per_job_per_year for the turn's share of a year.</summary>
        public static void Produce(Content c, GameState s, MvpRules r, double yearFraction, List<Cause> why)
        {
            foreach (var x in s.Cities.Values)
            {
                if (x.Employed == 0) continue;
                var paid = x.Employed * r.IncomePerJobPerYear * yearFraction;
                s.Treasury += paid;
                why.Add(new Cause("city", x.Id, paid, $"{PlayerText.Settlement(c, x.Id)}: jobs paid {PlayerText.MoneyChange(c, "treasury", paid)}"));
            }
        }

        /// <summary>R-CITY-004 (D-A5): refugees become civilians, without a job, as far as the city's free housing goes. The cap is
        /// a placeholder: what happens to people beyond it is D-D1, still open.</summary>
        public static void House(Content c, GameState s, List<Cause> why)
        {
            foreach (var x in s.Cities.Values.ToList())
            {
                var moved = Math.Min(x.Refugees, Math.Max(0, x.HousingCapacity - x.Civilians));
                if (moved == 0) continue;
                s.Cities[x.Id] = x.WithCounts(x.Civilians + moved, x.Employed, x.Refugees - moved);
                why.Add(new Cause("city", x.Id, moved, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {moved} groups of immigrants housed")));
            }
        }

        /// <summary>R-CITY-005, a placeholder while D-C1 is open: over all cities, the housed share (civilians of civilians and
        /// refugees; 1 with nobody) and the employed share (employed of civilians; 1 with no civilians), weighted by the mvp
        /// cohesion weights, clamped to 0 to 1. Counts are summed as integers, so city order cannot change the result.</summary>
        public static void UpdateCohesion(GameState s, MvpRules r)
        {
            long civilians = 0, employed = 0, refugees = 0;
            foreach (var x in s.Cities.Values) { civilians += x.Civilians; employed += x.Employed; refugees += x.Refugees; }
            var housed = civilians + refugees == 0 ? 1.0 : (double)civilians / (civilians + refugees);
            var working = civilians == 0 ? 1.0 : (double)employed / civilians;
            s.Cohesion = Math.Max(0, Math.Min(1, r.CohesionHousingWeight * housed + r.CohesionJobsWeight * working));
        }

        /// <summary>R-CITY-003 (D-A5): the turn's immigrants (the CBS series, aliyah.yaml) arrive as refugees in the mvp
        /// arrivals_city, in whole units. Before cutover PopulationSystem still adds the same people to GameState.Population,
        /// the head count the legacy economy reads: two views of one group until Phase 3 makes arrivals counts (spec section 6).</summary>
        public static void Arrive(Content c, GameState s, MvpRules r, DateTime next, List<Cause> why)
        {
            var units = ArrivedUnits(c, s.Date, next, r.PeoplePerUnit);
            if (units == 0) return;
            var x = s.Cities[r.ArrivalsCity];
            s.Cities[x.Id] = x.WithCounts(x.Civilians, x.Employed, x.Refugees + units);
            why.Add(new Cause("city", x.Id, units, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {units} groups of immigrants arrived and wait for housing")));
        }

        /// <summary>The whole units of <paramref name="peoplePerUnit"/> people that arrive in [from, to). Counted from the game's
        /// start, floor(people by to) minus floor(people by from), so over any run of turns no unit is lost or counted twice.</summary>
        public static int ArrivedUnits(Content c, DateTime from, DateTime to, int peoplePerUnit)
        {
            int UnitsBy(DateTime t)
            {
                var people = 0.0;
                foreach (var kv in PopulationSystem.Arrivals(c, c.Eras[0].From, t).OrderBy(k => k.Key, StringComparer.Ordinal)) people += kv.Value;
                return (int)Math.Floor(people / peoplePerUnit);
            }
            return UnitsBy(to) - UnitsBy(from);
        }
    }
}
