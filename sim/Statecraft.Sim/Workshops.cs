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

        /// <summary>R-ARMS-005: whether AssignWorkers(its city, "workshop", <paramref name="count"/>) staffs the built workshop
        /// <paramref name="project"/>, one of its city's first count built workshops in ascending id (#2711: the UI's word for a
        /// queued order).</summary>
        public static bool Staffs(Content c, GameState s, CityProject project, int count) =>
            BuiltIn(c, s, project.City).FindIndex(p => p.Id == project.Id) is int i && i >= 0 && i < count;

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
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the workers are records
            if (s.PopulationLedger.IsHistorical) return LedgerStaff(c, s, city, count, why);
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
            why.Add(StaffedLine(city, name, count - staffed, count));
            return null;
        }

        /// <summary>R-ARMS-005: a staffing order's Why line, the same for the aggregate cities and a Historical ledger game.</summary>
        static Cause StaffedLine(string city, string name, int delta, int count) =>
            new Cause("city", city, delta, FormattableString.Invariant($"{name}: arms workshops staffed: {count}"),
                key: CauseKeys.WorkshopWorkers, args: new[] { CauseKeys.Number(count) });

        // #1725 PR 7, producer 07: the refusal only a Historical ledger game gives. Internal, not a Refused* constant: no
        // player reaches it yet (the turn, TurnResolver.Resolve, still refuses a ledger game, R-POP-002), and the UI string follows when it does.
        /// <summary>Owner, 8 November 2026, Q7 "Unassigned, lowest id (Rec.)": more newly staffed workshops than the city's
        /// unassigned free-civilian records.</summary>
        internal const string LedgerRefusedFree = "{0} has {1} free civilians without an assignment";

        /// <summary>Owner, 8 November 2026, "Refuse with a reason (Recommended)": a staffing order in a city with builders
        /// (R-CITY-011), which are city counts and not records yet.</summary>
        internal const string LedgerRefusedBuilders = "{0} has builders on city projects, who are not on the population ledger yet";

        /// <summary>Owner, 8 November 2026, Q1 "Work assignment (Rec.)": the Work target a workshop's worker record holds.</summary>
        internal static string WorkerTarget(int project) => "workshop/" + project.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Owner, 8 November 2026, Q7 "Unassigned, lowest id (Rec.)": the city's free-civilian records with no
        /// assignment, located in the city, ascending id. A worker (Industry or a workshop), a farmer on a field, a teacher, a
        /// trainee and a refugee are not among them.</summary>
        static List<PopulationUnit> LedgerFree(GameState s, string city) => s.PopulationLedger.Units
            .Where(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned
                && string.Equals(p.Location?.City, city, StringComparison.Ordinal)).ToList();   // Units is ascending id

        /// <summary>The records on the workshop's Work target, ascending id: its worker, or none.</summary>
        static List<PopulationUnit> OnTarget(GameState s, int project)
        {
            var target = WorkerTarget(project);
            return s.PopulationLedger.Units.Where(p => p.Assignment.Kind == PopulationAssignmentKind.Work
                && string.Equals(p.Assignment.Target, target, StringComparison.Ordinal)).ToList();
        }

        /// <summary>Owner, 8 November 2026, Q6 "Waits, stays staffed (Rec.)": the workshop's bound worker when it is still a
        /// free civilian of the workshop's city on its target, else null (the worker is gone and the workshop waits).</summary>
        static PopulationUnit? LedgerWorker(GameState s, int project, string city)
        {
            var on = OnTarget(s, project);
            if (on.Count > 1) throw new InvalidOperationException("Arms workshop " + project.ToString(System.Globalization.CultureInfo.InvariantCulture) + " has more than one worker record (R-ARMS-004, worker_slots 1).");
            var p = on.FirstOrDefault();
            return p != null && p.Status == PopulationStatus.FreeCivilian && string.Equals(p.Location?.City, city, StringComparison.Ordinal) ? p : null;
        }

        /// <summary>Owner, 8 November 2026, Q4 "Only craftsmen discount (Rec.)": only a craftsman apprentice or expert lowers
        /// the price; every other record (profession None, a farmer or pioneer at any tier, a craftsman novice) is priced
        /// Ordinary, IL 5m.</summary>
        static SyntheticWorkshopTier TierOf(PopulationUnit p) => p.Profession != CivilianProfession.Craftsman ? AggregateTier
            : p.Tier == CivilianTier.Apprentice ? SyntheticWorkshopTier.Apprentice
            : p.Tier == CivilianTier.Expert ? SyntheticWorkshopTier.Expert : AggregateTier;

        /// <summary>R-ARMS-005 on a Historical ledger game (#1725 PR 7, producer 07). Owner, 8 November 2026: Q2 "Keep existing
        /// order (Rec.)", Q7 "Unassigned, lowest id (Rec.)", Q1 "Work assignment (Rec.)", Q8 "Keep tier field (Rec.)". The
        /// same standing order: the city's first <paramref name="count"/> built workshops, in ascending id, are staffed. A
        /// workshop no longer staffed releases its worker record (an unassigned free civilian again, in place); then each
        /// newly staffed workshop, in ascending project id, takes the lowest-id unassigned free civilian of the city, who
        /// holds a Work assignment on <see cref="WorkerTarget"/> (bound now if it is not yet) with no profession, tier or XP
        /// change (Q55), and the workshop is priced at that record's tier (<see cref="TierOf"/>), its cycle kept. Every check
        /// comes before the first write, and the records and targets are one validated commit. No city field is read or
        /// written: Q3 "Wait for Industry records (Rec.)", so no count of people without a job is made here.</summary>
        static string? LedgerStaff(Content c, GameState s, string city, int count, List<Cause> why)
        {
            if (!s.Cities.ContainsKey(city)) return Training.RefusedNoCity;
            if (c.Ruleset != SaveGame.Gdd03Ruleset) return Cities.RefusedNoRole;
            if (count < 0) return Cities.RefusedNegativeWorkers;
            var name = PlayerText.Settlement(c, city);
            var built = BuiltIn(c, s, city);
            if (count > built.Count) return built.Count == 1 ? Refused(RefusedBuiltOne, name) : Refused(RefusedBuilt, name, built.Count);
            // R-CITY-011's builders are city counts (CityProject.Builder), not records, so which records they are is not known.
            // Refused with a reason before any write rather than guessed (owner, 8 November 2026: "Refuse with a reason (Recommended)").
            if (Projects.Builders(s, city) > 0) return Refused(LedgerRefusedBuilders, name);
            var on = new List<int>(); var off = new List<int>();
            for (int i = 0; i < built.Count; i++)
            {
                s.Workshops.TryGetValue(built[i].Id, out var w);
                var staff = i < count;
                if (staff && w?.Staffed != true) on.Add(built[i].Id);
                else if (!staff && w?.Staffed == true) off.Add(built[i].Id);
            }
            var released = new SortedDictionary<int, PopulationUnit>();
            foreach (var project in off)
                foreach (var p in OnTarget(s, project))
                    released[p.Id] = p.WithPlacement(p.Status, p.Location, PopulationAssignment.Unassigned, p.Captor, p.Home);
            var free = LedgerFree(s, city).Concat(released.Values.Where(p => p.Status == PopulationStatus.FreeCivilian
                && string.Equals(p.Location?.City, city, StringComparison.Ordinal))).OrderBy(p => p.Id).ToList();
            if (on.Count > free.Count) return Refused(LedgerRefusedFree, name, free.Count);
            var region = c.PlayerRegions.AtLocation(city)?.Id
                ?? throw new InvalidOperationException("No region in regions.yaml for " + city + " (R-POP-004).");
            var changed = new SortedDictionary<int, PopulationUnit>(released);
            var bind = new List<PopulationTarget>();
            var tiers = new Dictionary<int, SyntheticWorkshopTier>();
            for (int i = 0; i < on.Count; i++)
            {
                var p = free[i]; var target = WorkerTarget(on[i]);
                if (s.PopulationLedger.Target(target) == null) bind.Add(new PopulationTarget(target, PopulationAssignmentKind.Work, region));
                changed[p.Id] = p.WithPlacement(p.Status, p.Location, new PopulationAssignment(PopulationAssignmentKind.Work, target), p.Captor, p.Home);
                tiers[on[i]] = TierOf(p);
            }
            var staffed = Staffed(s, city);
            s.PopulationLedger.Commit(changed.Values.ToList(), bind);
            foreach (var project in off) s.Workshops[project] = s.Workshops[project].WithStaffed(false);
            foreach (var project in on)
                s.Workshops[project] = s.Workshops.TryGetValue(project, out var w)
                    ? new Workshop(project, true, tiers[project], w.PaidCost, w.Progress)
                    : new Workshop(project, true, tiers[project]);
            why.Add(StaffedLine(city, name, count - staffed, count));
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
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger each workshop has its worker record
            var ledger = s.PopulationLedger.IsHistorical;
            var rules = Rules(c);
            var ceiling = c.RulesAt(s.Date).Mvp!.DebtCeiling!.Value;
            var held = Cities.Held(c, s);
            var working = new Dictionary<string, int>(StringComparer.Ordinal);
            // #1725 PR 7 review S2: on a Historical ledger every workshop the loop reaches has its worker records checked
            // first, in the loop's own order, so a corrupted ledger (more than one record on a target) throws before any write.
            if (ledger)
                foreach (var w in s.Workshops.Values)
                {
                    if (!w.Staffed) continue;
                    var city = s.CityProjects[w.Project].City;
                    if (!held.Contains(city)) LedgerWorker(s, w.Project, city);
                }
            foreach (var w in s.Workshops.Values.ToList())
            {
                if (!w.Staffed) continue;
                var city = s.CityProjects[w.Project].City;
                if (held.Contains(city)) continue;
                var name = PlayerText.Settlement(c, city);
                // #1725 PR 7, producer 07 (owner, 8 November 2026, Q6 "Waits, stays staffed (Rec.)"; Q3 "Wait for Industry
                // records (Rec.)"): on a Historical ledger the workshop works when its own bound worker is there, a named
                // record no builder can take (R-CITY-011), so no count of the city's people without a job is read.
                var worker = ledger ? LedgerWorker(s, w.Project, city) : null;
                var noWorker = ledger && worker == null;
                if (!ledger)
                {
                    var x = s.Cities[city];
                    // R-CITY-011: step 5's builders took their people first.
                    if (!working.TryGetValue(city, out var taken)) taken = Projects.Builders(s, city);
                    noWorker = taken >= x.Civilians + x.Craftsmen - x.Employed;
                    if (!noWorker) working[city] = taken + 1;
                }
                if (noWorker)
                {
                    why.Add(new Cause("city", city, 0, $"{name}: an arms workshop has no worker without a job this month", key: CauseKeys.WorkshopPaused, args: new[] { NoWorker, w.Project.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
                    continue;
                }
                var cycle = w;
                if (cycle.PaidCost == null)
                {
                    // Q8 "Keep tier field (Rec.)": a new cycle is priced at the worker record's tier, kept in WorkerTier.
                    if (worker != null) cycle = new Workshop(cycle.Project, cycle.Staffed, TierOf(worker));
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
