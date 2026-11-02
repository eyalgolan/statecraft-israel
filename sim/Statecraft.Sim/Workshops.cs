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

        /// <summary>R-ARMS-005: the city's built workshops (a project whose kind has a worker slot, R-ARMS-004), in ascending id.</summary>
        static List<CityProject> BuiltIn(Content c, GameState s, string city) =>
            s.CityProjects.Values.Where(p => p.City == city && p.BuiltOn != null && c.ProjectKinds.First(k => k.Id == p.Kind).WorkerSlots > 0).ToList();

        /// <summary>R-ARMS-005: the city's staffed workshops, each worked by one of its people who holds no job. 0 outside gdd-0.3,
        /// which has no workshop record.</summary>
        public static int Staffed(GameState s, string city) =>
            s.Workshops.Values.Count(w => w.Staffed && s.CityProjects[w.Project].City == city);

        /// <summary>R-ARMS-005 (27M-13: "Any free civilian. The cost tier follows the worker"): AssignWorkers(city, "workshop",
        /// count), a standing order: the city's first <paramref name="count"/> built workshops, in ascending id, are staffed and
        /// the rest are not. A newly staffed workshop takes one of the city's civilians and craftsmen who hold no job (it stays
        /// one of them, with no job, profession or qualification change: Q55), priced Ordinary on the aggregate cities (#1701
        /// OQ6). An unstaffed workshop keeps its record, paid cost and progress (a pause, R-ARMS-007). Refused, state untouched,
        /// for a negative count, more than the city's built workshops, or more newly staffed than its people without a job.</summary>
        internal static string? Staff(Content c, GameState s, string city, int count, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            if (count < 0) return "The number of workers cannot be negative";
            var name = PlayerText.Settlement(c, city);
            var built = BuiltIn(c, s, city);
            if (count > built.Count)
                return FormattableString.Invariant($"{name} has {built.Count} {(built.Count == 1 ? "arms workshop" : "arms workshops")}");
            var x = s.Cities[city];
            var staffed = Staffed(s, city);
            // R-CITY-009: while the city's job default is on, a newly staffed workshop may take its worker from the default's
            // jobs (the default then fills one job fewer, R-CITY-001); a number the player set is not lowered.
            var free = x.Civilians + x.Craftsmen - (x.JobsDefault ? 0 : x.Employed) - staffed;
            if (count - staffed > free)
                return FormattableString.Invariant($"{name} has {Math.Max(0, free)} civilians and craftsmen{(x.JobsDefault ? "" : " without a job")}");
            var employed = Math.Min(x.Employed, x.Civilians + x.Craftsmen - count);
            if (employed != x.Employed)
            {
                s.Cities[city] = x.WithCounts(x.Civilians, employed, x.Refugees);
                why.Add(new Cause("city", city, employed - x.Employed, FormattableString.Invariant($"{name}: {employed} groups in work"), key: CauseKeys.WorkersAssigned, args: new[] { CauseKeys.Number(employed) }));
            }
            for (int i = 0; i < built.Count; i++)
            {
                var on = i < count;
                if (s.Workshops.TryGetValue(built[i].Id, out var w)) { if (w.Staffed != on) s.Workshops[w.Project] = w.WithStaffed(on); }
                else if (on) s.Workshops.Add(built[i].Id, new Workshop(built[i].Id, true, SyntheticWorkshopTier.Ordinary));
            }
            why.Add(new Cause("city", city, count - staffed, FormattableString.Invariant($"{name}: arms workshops staffed: {count}"),
                key: CauseKeys.WorkshopWorkers, args: new[] { CauseKeys.Number(count) }));
            return null;
        }

        /// <summary>R-ARMS-006, step 6 of the turn after the cities' wages, gdd-0.3 only. In ascending project id, a staffed
        /// workshop whose city the enemy does not hold (R-CITY-010) works this month when one of its city's people without a
        /// job is left for it (each workshop of the city takes one). With no cycle under way it first pays for one at its
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
            var rules = SyntheticWorkshopRules.FromCatalogue(c.Gdd03Balance!);   // the loader requires it under gdd-0.3
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
                working.TryGetValue(city, out var taken);
                if (taken >= x.Civilians + x.Craftsmen - x.Employed)
                {
                    why.Add(new Cause("city", city, 0, $"{name}: an arms workshop has no worker without a job this month", key: CauseKeys.WorkshopPaused, args: new[] { NoWorker }));
                    continue;
                }
                working[city] = taken + 1;
                var cycle = w;
                if (cycle.PaidCost == null)
                {
                    var price = rules.Price(cycle.WorkerTier);
                    var left = s.Treasury - (double)price;   // R-MONEY-003's debt after: the debt plus what the Treasury lacks
                    if (left < 0 && s.Debt - left > ceiling)
                    {
                        why.Add(new Cause("city", city, 0, $"{name}: an arms workshop's next cycle waits: paying for it would take the debt past its ceiling", key: CauseKeys.WorkshopPaused, args: new[] { DebtCeiling }));
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
