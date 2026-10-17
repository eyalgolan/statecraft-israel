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

        /// <summary>R-CITY-007: a city's start people in units: the nearest unit (a half rounds up), and at least 1 for a city
        /// with people (Q-START-2, owner, 15 October 2026), so a small town is not left empty.</summary>
        public static int StartUnits(decimal people, int peoplePerUnit) => people > 0 ? Math.Max(1, NearestUnit(people, peoplePerUnit)) : 0;

        /// <summary>R-CITY-007: people in units to the nearest unit, a half rounding up. Decimal, so it is exact on every runtime.</summary>
        public static int NearestUnit(decimal people, int peoplePerUnit) =>
            (int)Math.Round(people / peoplePerUnit, MidpointRounding.AwayFromZero);

        /// <summary>Fills the state's cities from cities.yaml, with no refugees yet. Does nothing in the legacy loop.</summary>
        public static void Init(Content c, GameState s)
        {
            if (!c.IsGdd) return;
            foreach (var x in c.Cities) s.Cities[x.Id] = new City(x.Id, x.HousingCapacity, x.JobSlots, x.Civilians, x.Employed, 0);
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
            // PLACEHOLDER (owner question, #377): only civilians take housing; people in training and recruits do not.
            foreach (var x in s.Cities.Values.ToList())
            {
                var moved = Math.Min(x.Refugees, Math.Max(0, x.HousingCapacity - x.Civilians));
                if (moved == 0) continue;
                s.Cities[x.Id] = x.WithCounts(x.Civilians + moved, x.Employed, x.Refugees - moved);
                why.Add(new Cause("city", x.Id, moved, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {moved} groups of immigrants housed")));
            }
        }

        /// <summary>The effect source id of the cities' employed share (R-CITY-005): a GDD-only source of `morale`, Social
        /// cohesion, read by the effects web after the turn's housing (D-A6). Phase 1's separate cohesion value is folded into it.</summary>
        public const string EmployedShareSource = "employed_share";

        /// <summary>R-CITY-005: employed over civilians, summed over all cities as integers (so city order cannot change it);
        /// 1 with no civilians, as nobody is out of work.</summary>
        public static double EmployedShare(GameState s)
        {
            long civilians = 0, employed = 0;
            foreach (var x in s.Cities.Values) { civilians += x.Civilians; employed += x.Employed; }
            return civilians == 0 ? 1.0 : (double)employed / civilians;
        }

        /// <summary>R-CITY-003 (D-A5, Q-ARRV-1): the turn's immigrants (the CBS series, aliyah.yaml) arrive as refugees, in whole
        /// units, spread over the cities by <see cref="Apportion"/>. The cities count them from mvp.city_arrivals_from, the
        /// census day their start values already include (Q-ARRV-3); PopulationSystem adds every arrival to the national head
        /// count from the game's start, and the cities are the part of it placed in towns (R-CITY-008, Q-ARRV-2).</summary>
        public static void Arrive(Content c, GameState s, MvpRules r, DateTime next, List<Cause> why)
        {
            var units = ArrivedUnits(c, s.Date, next, r.PeoplePerUnit);
            if (units == 0) return;
            var before = UnitsBy(c, s.Date, r.PeoplePerUnit);
            var had = Apportion(c, before);
            foreach (var kv in Apportion(c, before + units))
            {
                var added = kv.Value - had[kv.Key];
                if (added == 0) continue;
                var x = s.Cities[kv.Key];
                s.Cities[x.Id] = x.WithCounts(x.Civilians, x.Employed, x.Refugees + added);
                why.Add(new Cause("city", x.Id, added, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {added} groups of immigrants arrived and wait for homes")));
            }
        }

        /// <summary>R-CITY-003 (Q-ARRV-1): the cities' shares of their first <paramref name="units"/> arrived units, by id. The
        /// weights are the cities' census people. Units are handed out one at a time, each to the city whose share of the
        /// units so far most exceeds what it holds (the largest remainder), a tie to the lower id (ordinal). A fresh
        /// largest-remainder split of each total could take a unit back from a city when the total grows (the Alabama paradox;
        /// on the real cities at 7 units), so the split is built up from the first unit. Decimal, so exact on every runtime.</summary>
        public static SortedDictionary<string, int> Apportion(Content c, int units)
        {
            var held = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var x in c.Cities) held[x.Id] = 0;
            var weight = c.Cities.ToDictionary(x => x.Id, x => x.People!.Value);
            var total = weight.Values.Sum();
            for (int n = 1; n <= units; n++)
            {
                string? best = null;
                var most = 0m;
                foreach (var kv in held)   // ordinal order, and only a larger remainder replaces: a tie stays with the lower id
                {
                    var remainder = n * weight[kv.Key] - kv.Value * total;   // (the share minus what it holds) times the total
                    if (best == null || remainder > most) { best = kv.Key; most = remainder; }
                }
                held[best!]++;
            }
            return held;
        }

        /// <summary>The whole units of <paramref name="peoplePerUnit"/> people that arrive in the cities in [from, to). Counted
        /// from mvp.city_arrivals_from, floor(people by to) minus floor(people by from), so over any run of turns no unit is lost
        /// or counted twice.</summary>
        public static int ArrivedUnits(Content c, DateTime from, DateTime to, int peoplePerUnit) =>
            UnitsBy(c, to, peoplePerUnit) - UnitsBy(c, from, peoplePerUnit);

        /// <summary>The cities' whole units of arrivals from mvp.city_arrivals_from to <paramref name="t"/> (0 before it).</summary>
        static int UnitsBy(Content c, DateTime t, int peoplePerUnit)
        {
            var origin = c.RulesAt(c.Eras[0].From).Mvp!.CityArrivalsFrom;   // the same in every era (the loader checks it)
            if (t <= origin) return 0;
            var people = 0.0;
            foreach (var kv in PopulationSystem.Arrivals(c, origin, t).OrderBy(k => k.Key, StringComparer.Ordinal)) people += kv.Value;
            return (int)Math.Floor(people / peoplePerUnit);
        }
    }
}
