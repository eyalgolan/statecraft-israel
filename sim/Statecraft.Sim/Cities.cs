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

        /// <summary>R-CITY-001: the most of the city's people AssignWorkers accepts in jobs: its job slots, and no more than its
        /// civilians and craftsmen (a craftsman fills an ordinary job slot, R-CRAFT-002). R-CITY-009's default fills to it.</summary>
        public static int MaxWorkers(City city) => Math.Min(city.JobSlots, city.Civilians + city.Craftsmen);

        /// <summary>R-CITY-001 with R-TRAIN-001: <see cref="MaxWorkers(City)"/> once the city's Train orders in
        /// <paramref name="pending"/> (this turn's queue) have taken their civilians, so a count up to it keeps every one of
        /// them accepted, whatever order they run in. Other orders do not count.</summary>
        public static int MaxWorkers(GameState s, string city, IEnumerable<Command> pending)
        {
            var x = s.Cities[city];
            return Math.Max(0, Math.Min(x.JobSlots, x.Civilians + x.Craftsmen - Training.Ordered(city, pending)));
        }

        /// <summary>R-CITY-007: a city's start people in units: the nearest unit (a half rounds up), and at least 1 for a city
        /// with people (Q-START-2, owner, 15 October 2026), so a small town is not left empty.</summary>
        public static int StartUnits(decimal people, int peoplePerUnit) => people > 0 ? Math.Max(1, NearestUnit(people, peoplePerUnit)) : 0;

        /// <summary>R-CITY-007: people in units to the nearest unit, a half rounding up. Decimal, so it is exact on every runtime.</summary>
        public static int NearestUnit(decimal people, int peoplePerUnit) =>
            (int)Math.Round(people / peoplePerUnit, MidpointRounding.AwayFromZero);

        /// <summary>Fills the state's cities from cities.yaml, with no refugees yet and the start housing condition (R-HOUSE-001). Does nothing in the legacy loop.</summary>
        public static void Init(Content c, GameState s)
        {
            if (!c.IsGdd) return;
            var r = c.RulesAt(s.Date).Mvp!;
            foreach (var x in c.Cities) s.Cities[x.Id] = new City(x.Id, x.HousingCapacity, x.JobSlots, x.Civilians, x.Employed, 0, 0, r.HousingStartCondition!.Value);
            var housing = c.At(s.Date).Indicators.FirstOrDefault(i => i.Id == HousingIndicator);
            if (housing != null) s.Indicators[housing.Id] = EffectsSystem.Level(c, s, housing);   // R-HOUSE-004 from the first day
        }

        /// <summary>R-CITY-001: sets the number of the city's people in jobs (a standing order), and turns that city's job
        /// default off (R-CITY-009). Refused, state untouched, when the city or role is unknown or the count is negative, above
        /// the job slots or above the civilians and craftsmen. Step has already refused it in the legacy loop.</summary>
        public static string? Assign(Content c, GameState s, string city, string role, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            // Craftsmen fill ordinary job slots (Q-CRAFT-2, R-CRAFT-002), so "job" stays the only role.
            if (role != "job") return "There is no such role";
            if (count < 0) return "The number of workers cannot be negative";
            if (count > MaxWorkers(x))
                return count > x.JobSlots
                    ? FormattableString.Invariant($"{PlayerText.Settlement(c, city)} has {x.JobSlots} jobs")
                    : FormattableString.Invariant($"{PlayerText.Settlement(c, city)} has {x.Civilians + x.Craftsmen} civilians and craftsmen");
            s.Cities[city] = x.WithCounts(x.Civilians, count, x.Refugees).WithJobsDefault(false);
            why.Add(new Cause("city", city, count - x.Employed, FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: {count} groups in work")));
            return null;
        }

        /// <summary>R-CITY-002, R-CRAFT-002: each city's employed units earn income_per_job_per_year for the turn's share of a
        /// year. Craftsmen take the jobs first, and each employed craftsman earns craftsman_income_multiple times that.</summary>
        public static void Produce(Content c, GameState s, MvpRules r, double yearFraction, List<Cause> why)
        {
            foreach (var x in s.Cities.Values)
            {
                if (x.Employed == 0) continue;
                var craftsmen = Math.Min(x.Craftsmen, x.Employed);
                var paid = ((x.Employed - craftsmen) + craftsmen * r.CraftsmanIncomeMultiple) * r.IncomePerJobPerYear * yearFraction;
                s.Treasury += paid;
                why.Add(new Cause("city", x.Id, paid, $"{PlayerText.Settlement(c, x.Id)}: jobs paid {PlayerText.MoneyChange(c, "treasury", paid)}"));
            }
        }

        /// <summary>R-MONEY-002: annual Money upkeep for the civilian places that exist at the start of step 7. Only completed
        /// projects and camp sites count. The six rates are explicit content design values; all are 0 in the MVP.</summary>
        public static double AnnualUpkeep(Content c, GameState s, MvpRules r)
        {
            var total = s.Cities.Count * r.CityUpkeepPerYear!.Value;
            foreach (var p in s.CityProjects.Values)
            {
                if (p.BuiltOn == null) continue;
                if (p.Kind == "housing") total += r.HousingUpkeepPerYear!.Value;
                else if (p.Kind == "industry") total += r.IndustryUpkeepPerYear!.Value;
                else if (p.Kind == "transit_camp") total += r.TransitCampUpkeepPerYear!.Value;
            }
            var military = c.Cities.Sum(x => x.Facilities.Count(f => f == "military_camp"));
            var hakhsharot = c.Cities.Sum(x => x.Facilities.Count(f => f == "hakhshara"));
            foreach (var w in s.Sites.Values)
            {
                if (w.BuiltOn == null) continue;
                var site = c.Sites.First(x => x.Id == w.Site);
                if (site.Kinds.Contains("military_camp")) military++;
                if (site.Kinds.Contains("hakhshara")) hakhsharot++;
            }
            return total + military * r.MilitaryCampUpkeepPerYear!.Value + hakhsharot * r.HakhsharaUpkeepPerYear!.Value;
        }

        /// <summary>R-CITY-004 (D-A5; D-D1, Q-HOUSE-1): no hard cap. Every refugee becomes a civilian, without a job, on the
        /// housing step; the people beyond the city's housing are its overflow (<see cref="Overflow"/>), still counted in it.
        /// Residents are housed automatically (Q-HOUSE-4). Then R-CITY-009 (Q-JOBS-1): in ascending city id, a city whose job
        /// default is on fills its free job slots from its people without a job, at no Attention cost.</summary>
        public static void House(Content c, GameState s, List<Cause> why)
        {
            foreach (var x in s.Cities.Values.ToList())
            {
                var housed = x.Refugees == 0 ? x : x.WithCounts(x.Civilians + x.Refugees, x.Employed, 0);
                if (x.Refugees != 0)
                    why.Add(new Cause("city", x.Id, x.Refugees, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {x.Refugees} groups of immigrants housed")));
                var work = MaxWorkers(housed);
                if (housed.JobsDefault && work != housed.Employed)
                {
                    why.Add(new Cause("city", x.Id, work - housed.Employed, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {work} groups in work")));
                    housed = housed.WithCounts(housed.Civilians, work, housed.Refugees);
                }
                s.Cities[x.Id] = housed;
            }
        }

        /// <summary>R-HOUSE-003 (Q-HOUSE-5): the people who take the city's housing: its civilians, craftsmen, recruits and groups
        /// in training (a halutz group too, until it becomes a movable unit and leaves). Refugees wait for housing and are not
        /// in it. Summed as integers, so the order of the training groups cannot change it.</summary>
        public static int Housed(GameState s, City x)
        {
            var n = x.Civilians + x.Craftsmen + x.Recruits;
            foreach (var k in s.Cohorts.Values) if (k.City == x.Id) n += k.Count;
            return n;
        }

        /// <summary>R-HOUSE-002 (D-D1): the people beyond the city's housing, one capacity point a unit (Q-HOUSE-1); 0 when
        /// they fit.</summary>
        public static int Overflow(GameState s, City x) => Math.Max(0, Housed(s, x) - x.HousingCapacity);

        /// <summary>The national indicator whose base the cities set (R-HOUSE-004).</summary>
        public const string HousingIndicator = "housing";

        /// <summary>R-HOUSE-004 (owner, 17 October 2026, Q-PR3-A option (a)): the people-weighted mean of the cities' housing
        /// condition, each city weighed by its <see cref="Housed"/> people. It replaces the base of the national `housing`
        /// indicator in the GDD loop; policy, map actions and events still act on top (EffectsSystem.Inner). Null when no city
        /// with a condition houses anybody, and the indicator then reads model.yaml's base. The sum runs in ordinal city order
        /// with integer weights, then one division.</summary>
        public static double? HousingLevel(GameState s)
        {
            long people = 0;
            var sum = 0.0;
            foreach (var x in s.Cities.Values)   // SortedDictionary, ordinal
            {
                if (x.Condition is not double condition) continue;
                var n = Housed(s, x);
                people += n;
                sum += condition * n;
            }
            return people == 0 ? (double?)null : sum / people;
        }

        /// <summary>R-HOUSE-002 (D-D1, Q-HOUSE-1): after the housing step, each city with overflow loses
        /// <c>overflow_condition_loss * overflow / housed</c> of its housing condition, never below 0. A city with none keeps
        /// its condition; nothing in this rule raises it (housing projects do, Phase 3 PR 4). Each city reads only its own
        /// counts (R-HOUSE-001: the condition is per city).</summary>
        public static void UpdateCondition(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            foreach (var x in s.Cities.Values.ToList())
            {
                var over = Overflow(s, x);
                if (over == 0) continue;
                if (x.Condition is double now)
                    s.Cities[x.Id] = x.WithCondition(Math.Max(0.0, now - r.OverflowConditionLoss!.Value * over / Housed(s, x)));
                why.Add(new Cause("city", x.Id, over, FormattableString.Invariant($"{PlayerText.Settlement(c, x.Id)}: {over} groups more than its homes hold")));
            }
        }

        /// <summary>The effect source id of the cities' employed share (R-CITY-005): a GDD-only source of `morale`, Social
        /// cohesion, read by the effects web after the turn's housing (D-A6). Phase 1's separate cohesion value is folded into it.</summary>
        public const string EmployedShareSource = "employed_share";

        /// <summary>R-CITY-005: employed over civilians and craftsmen, summed over all cities as integers (so city order cannot
        /// change it); 1 with none, as nobody is out of work.</summary>
        public static double EmployedShare(GameState s)
        {
            long civilians = 0, employed = 0;
            foreach (var x in s.Cities.Values) { civilians += x.Civilians + x.Craftsmen; employed += x.Employed; }
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
