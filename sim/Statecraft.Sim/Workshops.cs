using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>#1701 (GDD 0.3 arms workshops) on the aggregate cities, under gdd-0.3 only: the arms_workshop project kind
    /// (R-ARMS-004), its worker (R-ARMS-005) and its monthly cycle (R-ARMS-006, R-ARMS-007).</summary>
    public static class Workshops
    {
        /// <summary>The projects.yaml id of the arms workshop (R-ARMS-004; gdd03-balance.yaml `arms_workshop`).</summary>
        public const string ArmsWorkshop = "arms_workshop";

        /// <summary>R-ARMS-005: the AssignWorkers role that staffs a city's arms workshops (gdd-0.3 only).</summary>
        public const string Role = "workshop";

        /// <summary>R-ARMS-006: why a staffed workshop did not work this month (CauseKeys.WorkshopPaused's argument): no person
        /// of its city without a job was left for it, or paying for its next cycle would take the debt past mvp.debt_ceiling.</summary>
        public const string NoWorker = "no_worker", DebtCeiling = "debt_ceiling";

        /// <summary>R-ARMS-005: a staffing order's refusals (Check), formats with the settlement's name ({0}) and a count ({1},
        /// invariant). The English is what a turn's Rejected list and a save carry; the UI words it in its language
        /// (SimBridge.ReasonText, #1722). <see cref="RefusedPeople"/> is also the jobs order's refusal past the city's
        /// civilians and craftsmen less its staffed workshops (Cities.Assign, R-CITY-001).</summary>
        public const string RefusedBuiltOne = "{0} has 1 arms workshop", RefusedBuilt = "{0} has {1} arms workshops",
            RefusedPeople = "{0} has {1} civilians and craftsmen", RefusedPeopleNoJob = "{0} has {1} civilians and craftsmen without a job";

        /// <summary>R-ARMS-005 (#1701 OQ6): the tier a newly staffed workshop's worker is priced at on the aggregate cities,
        /// until the population ledger's tiers are live (R-POP-004). Staff gives it, and NextCyclePrice prices a workshop
        /// never staffed at it.</summary>
        internal const SyntheticWorkshopTier AggregateTier = SyntheticWorkshopTier.Ordinary;

        static string Refused(string format, params object[] values) => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, values);

        /// <summary>R-ARMS-005: the number of the city's built arms workshops, the most AssignWorkers(city, "workshop", n) staffs.</summary>
        public static int Built(Content c, GameState s, string city) => BuiltIn(c, s, city).Count;

        /// <summary>R-ARMS-006 (#1722, the hex panel): what the production step would charge for the built workshop
        /// <paramref name="project"/>'s next cycle, at its worker's tier, or at <see cref="AggregateTier"/> for a workshop
        /// never staffed (the tier Staff would give it). Null while a cycle is under way (its price is the Workshop record's
        /// PaidCost), for an unknown id, a project still being built or with no worker slot, and outside gdd-0.3. Nothing
        /// changes.</summary>
        public static decimal? NextCyclePrice(Content c, GameState s, int project)
        {
            if (c.Ruleset != SaveGame.Gdd03Ruleset) return null;
            if (!s.CityProjects.TryGetValue(project, out var p) || !IsBuilt(c, p)) return null;
            s.Workshops.TryGetValue(project, out var w);
            return w?.PaidCost != null ? null : Price(Rules(c), w);
        }

        /// <summary>R-ARMS-006: a cycle's staffed months (gdd03-balance workshop.staffed_turns) under gdd-0.3, else null.</summary>
        public static int? CycleMonths(Content c) => c.Ruleset == SaveGame.Gdd03Ruleset ? Rules(c).StaffedTurns : (int?)null;

        static SyntheticWorkshopRules Rules(Content c) => SyntheticWorkshopRules.FromCatalogue(c.Gdd03Balance!);   // the loader requires it under gdd-0.3

        /// <summary>R-ARMS-006 (27M-13): a cycle's price at the worker's tier; <see cref="AggregateTier"/> with no record.</summary>
        static decimal Price(SyntheticWorkshopRules rules, Workshop? w) => rules.Price(w?.WorkerTier ?? AggregateTier);

        /// <summary>R-ARMS-005: why AssignWorkers(<paramref name="city"/>, "workshop", <paramref name="count"/>) would be
        /// refused, or null; nothing changes. With an empty <paramref name="pending"/> it is the order's own check. With this
        /// turn's queue (#1722 slice E, the hex panel) it also counts the city's pending "job" order (whose jobs then hold
        /// no workshop worker, and which turns R-CITY-009's default off) and its Train and reassignment orders
        /// (Training.Ordered) and the builder each queued gdd-0.3 StartProject there may take (R-CITY-011), so a count it
        /// accepts is still accepted whichever of them runs first.</summary>
        public static string? Check(Content c, GameState s, string city, int count, IEnumerable<Command> pending)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return Training.RefusedNoCity;
            if (c.Ruleset != SaveGame.Gdd03Ruleset) return Cities.RefusedNoRole;
            if (count < 0) return Cities.RefusedNegativeWorkers;
            var name = PlayerText.Settlement(c, city);
            var built = Built(c, s, city);
            if (count > built) return built == 1 ? Refused(RefusedBuiltOne, name) : Refused(RefusedBuilt, name, built);
            var list = pending as IReadOnlyCollection<Command> ?? pending.ToList();
            var job = list.OfType<AssignWorkers>().Where(w => w.City == city && w.Role == "job").Select(w => (int?)w.Count).Max();
            var byDefault = x.JobsDefault && job == null;
            var staffed = Staffed(s, city);
            // R-CITY-009: while the city's job default is on, a newly staffed workshop may take its worker from the default's
            // jobs (the default then fills one job fewer, R-CITY-001); a number the player set is not lowered. R-CITY-011: a
            // builder is not free, nor the one a queued start there may take.
            var free = x.Civilians + x.Craftsmen - Math.Max(x.JobsDefault ? 0 : x.Employed, job ?? 0) - staffed - Projects.Builders(s, city) - Projects.Starting(s, city, list) - Training.Ordered(city, list);
            if (count - staffed > free) return Refused(byDefault ? RefusedPeople : RefusedPeopleNoJob, name, Math.Max(0, free));
            return null;
        }

        /// <summary>R-ARMS-005: the city's built workshops (a project whose kind has a worker slot, R-ARMS-004), in ascending id.</summary>
        static List<CityProject> BuiltIn(Content c, GameState s, string city) =>
            s.CityProjects.Values.Where(p => p.City == city && IsBuilt(c, p)).ToList();

        /// <summary>R-ARMS-004: a built project whose kind has a worker slot.</summary>
        static bool IsBuilt(Content c, CityProject p) => p.BuiltOn != null && c.ProjectKinds.First(k => k.Id == p.Kind).WorkerSlots > 0;

        /// <summary>R-ARMS-005: the city's staffed workshops, each worked by one of its people who holds no job. 0 outside gdd-0.3,
        /// which has no workshop record.</summary>
        public static int Staffed(GameState s, string city) =>
            s.Workshops.Values.Count(w => w.Staffed && s.CityProjects[w.Project].City == city);

        // #2530: a count of 1 names the singular, as the UI's helper does.
        static string Groups(int n) => n == 1 ? "1 group" : FormattableString.Invariant($"{n} groups");

        /// <summary>R-ARMS-005 (27M-13: "Any free civilian. The cost tier follows the worker"): AssignWorkers(city, "workshop",
        /// count), a standing order: the city's first <paramref name="count"/> built workshops, in ascending id, are staffed and
        /// the rest are not. A newly staffed workshop takes one of the city's civilians and craftsmen who hold no job (it stays
        /// one of them, with no job, profession or qualification change: Q55), priced Ordinary on the aggregate cities (#1701
        /// OQ6). An unstaffed workshop keeps its record, paid cost and progress (a pause, R-ARMS-007). Refused, state untouched,
        /// for a negative count, more than the city's built workshops, or more newly staffed than its people without a job.</summary>
        internal static string? Staff(Content c, GameState s, string city, int count, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            if (Check(c, s, city, count, Array.Empty<Command>()) is string refused) return refused;
            var name = PlayerText.Settlement(c, city);
            var built = BuiltIn(c, s, city);
            var x = s.Cities[city];
            var staffed = Staffed(s, city);
            var employed = Math.Min(x.Employed, x.Civilians + x.Craftsmen - count - Projects.Builders(s, city));   // R-CITY-011: nor a builder's
            if (employed != x.Employed)
            {
                s.Cities[city] = x.WithCounts(x.Civilians, employed, x.Refugees);
                why.Add(new Cause("city", city, employed - x.Employed, FormattableString.Invariant($"{name}: {Groups(employed)} in work"), key: CauseKeys.WorkersAssigned, args: new[] { CauseKeys.Number(employed) }));
            }
            for (int i = 0; i < built.Count; i++)
            {
                var on = i < count;
                if (s.Workshops.TryGetValue(built[i].Id, out var w)) { if (w.Staffed != on) s.Workshops[w.Project] = w.WithStaffed(on); }
                else if (on) s.Workshops.Add(built[i].Id, new Workshop(built[i].Id, true, AggregateTier));
            }
            why.Add(new Cause("city", city, count - staffed, FormattableString.Invariant($"{name}: arms workshops staffed: {count}"),
                key: CauseKeys.WorkshopWorkers, args: new[] { CauseKeys.Number(count) }));
            return null;
        }

        /// <summary>R-ARMS-006, step 6 of the turn after the cities' wages, gdd-0.3 only. In ascending project id, a staffed
        /// workshop whose city the enemy does not hold (R-CITY-010) works this month when one of its city's people without a
        /// job is left for it (each workshop of the city takes one, after the city's builders, R-CITY-011). With no cycle under way it first pays for one at its
        /// worker's tier (gdd03-balance.yaml workshop.cost: 27M-11, "At the monthly production step."), borrowing a short
        /// Treasury's rest (R-MONEY-003; 27M-12, "paid like any GDD cost"), unless that would take the debt past
        /// mvp.debt_ceiling (#1701 OQ4, owner 2 November 2026: "Refuse past the ceiling (Recommended)"): then the workshop
        /// waits this month and the turn report says why. The month it is paid counts (#1701 OQ3); at workshop.staffed_turns
        /// months the cycle adds 1 Arms (R-ARMS-001) and ends, and the next month starts the next by itself (27M-12,
        /// "Automatic each month while the workshop is staffed"). An unstaffed workshop or one in a held city keeps its cycle
        /// as it is (a pause, R-ARMS-007). Nobody's profession or qualification changes (Q55).</summary>
        public static void Produce(Content c, GameState s, List<Cause> why)
        {
            if (c.Ruleset != SaveGame.Gdd03Ruleset || s.Workshops.Count == 0) return;
            PopulationLedger.RefuseAggregate(s);
            var rules = Rules(c);
            var ceiling = c.RulesAt(s.Date).Mvp!.DebtCeiling!.Value;
            var held = Cities.Held(c, s);
            var working = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var w in s.Workshops.Values.ToList())
            {
                if (!w.Staffed) continue;
                var city = s.CityProjects[w.Project].City;
                if (held.Contains(city)) continue;
                var name = PlayerText.Settlement(c, city);
                var x = s.Cities[city];
                // R-CITY-011: step 5's builders took their people first.
                if (!working.TryGetValue(city, out var taken)) taken = Projects.Builders(s, city);
                if (taken >= x.Civilians + x.Craftsmen - x.Employed)
                {
                    why.Add(new Cause("city", city, 0, $"{name}: an arms workshop has no worker without a job this month", key: CauseKeys.WorkshopPaused, args: new[] { NoWorker, w.Project.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
                    continue;
                }
                working[city] = taken + 1;
                var cycle = w;
                if (cycle.PaidCost == null)
                {
                    var price = Price(rules, cycle);
                    var left = s.Treasury - (double)price;   // R-MONEY-003's debt after: the debt plus what the Treasury lacks
                    if (left < 0 && s.Debt - left > ceiling)
                    {
                        why.Add(new Cause("city", city, 0, $"{name}: an arms workshop's next cycle waits: paying for it would take the debt past its ceiling", key: CauseKeys.WorkshopPaused, args: new[] { DebtCeiling, w.Project.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
                        continue;
                    }
                    EconomySystem.PayOrBorrow(c, s, (double)price, "for an arms workshop cycle", why, CauseKeys.Purpose.Workshop);
                    cycle = cycle.WithCycle(price, 0);
                }
                var progress = cycle.Progress + 1;
                // A full stock (int.MaxValue) cannot take the Arms: the cycle stays complete and waits, still paid.
                if (progress < rules.StaffedTurns || s.Arms == int.MaxValue) { s.Workshops[w.Project] = cycle.WithCycle(cycle.PaidCost, Math.Min(progress, rules.StaffedTurns)); continue; }
                s.Arms += 1;
                s.Workshops[w.Project] = cycle.WithCycle(null, 0);
                why.Add(new Cause("city", city, 1, $"{name}: an arms workshop made 1 Arms", key: CauseKeys.WorkshopArms));
            }
        }
    }
}
