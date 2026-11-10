using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 training (docs/GDD.md "Progression"; Phase 1 PR 4): a city's civilians train in a military camp to
    /// become recruits, in a Hakhshara to become halutzim, or in a city with a completed industry project to become craftsmen
    /// (D-D4, Q-CRAFT-1), over several turns (D-A3). The facilities are read from
    /// cities.yaml: one of each exists at the start (D-D5, a design assumption), and pioneers build more at camp sites
    /// (R-CAMP-001). Runs only
    /// when eras.yaml systems.loop is gdd. Its causes go to the "city" group of the Why screen: they move a city's people and
    /// pay for it, like the other city rules.</summary>
    public static class Training
    {
        public const string TrainKind = "train";
        public const string CancelKind = "cancel_training";
        public const string ReinforceKind = "reinforce";

        /// <summary>The craftsman's unit type id (D-D4; R-CRAFT-001).</summary>
        public const string Craftsman = "craftsman";

        /// <summary>The facility a craftsman trains at: a completed city project of this kind (projects.yaml; Q-CRAFT-1).</summary>
        public const string Industry = "industry";

        /// <summary><see cref="Reinforce"/>'s and <see cref="Start"/>'s refusals (#2072); the unit and side ones are Orders.Refused*.
        /// The UI maps each to its own string (SimBridge.ReasonText). The English is the reason a turn's Rejected list and a save
        /// carry, so it does not change; one with {0} is a format, its values (a unit id, a settlement's or course's name, a
        /// number) filled in invariantly. A missing facility has one refusal per facility.</summary>
        public const string RefusedNoCohort = "There is no such training group";   // #2707: Cancel's
        public const string RefusedNoCity = "There is no such city";
        public const string RefusedNotInCity = "Unit {0} is not in {1}";
        public const string RefusedNoCourse = "Unit {0} cannot take replacements";
        public const string RefusedNoLosses = "Unit {0} has no losses to replace";
        public const string RefusedRoomOne = "Unit {0} can take {1} more group";
        public const string RefusedRoomMany = "Unit {0} can take {1} more groups";
        public const string RefusedNoTraining = "There is no such training";
        public const string RefusedCount = "Train at least one group";
        public const string RefusedNoCamp = "{0} has no military camp";
        public const string RefusedNoIndustry = "{0} has no completed industry project";
        public const string RefusedNoHakhshara = "{0} has no Hakhshara";
        public const string RefusedIdle = "{0} has {1} civilians without work";
        public const string RefusedArms = "{0}: not enough Arms for {1} (need {2}, have {3})";

        // #1725 PR 7, producer 12: the refusals only a Historical ledger game gives. Public for the UI's strings (#3020:
        // SimBridge.ReasonText words each in the UI's language), and not named Refused*, as only a ledger game gives them.
        /// <summary>Owner, 8 November 2026, "Recruit+crews+halutz (Rec.)" (#1725 issuecomment-6724941867): the craftsman track is refused with a plain reason until #2476.</summary>
        public const string LedgerRefusedCraftsman = "Craft training does not run on the population ledger yet (#2476)";
        /// <summary>Owner, 8 November 2026, "Enforce it here (Rec.)" (#1725 issuecomment-6724941867): the camp capacity, 50 records per camp city, over all military tracks.</summary>
        public const string LedgerRefusedCampFull = "{0}'s military camp has room for {1} more trainees (capacity {2})";
        /// <summary>Owner, 8 November 2026, "Full minus strength (Rec.)" (#1725 issuecomment-6724941867): a formation whose strength differs from its serving records is refused.</summary>
        public const string LedgerRefusedStrength = "Unit {0} has {1} soldiers on record but strength {2}";
        /// <summary>Owner, 8 November 2026, Q3, "Refuse, with reason (Recommended)": a military trainee who already carries a military item.</summary>
        public const string LedgerRefusedCarried = "A civilian without work in {0} already carries a military item";
        /// <summary>Owner, 8 November 2026, Q3, "Refuse, with reason (Recommended)": a farmer or craftsman on the halutz track.</summary>
        public const string LedgerRefusedHalutzTrade = "A farmer or craftsman in {0} cannot train as a halutz";

        static string Refused(string format, params object[] values) => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, values);

        /// <summary>The course a type trains in, in text the player reads: the facility's, or the guns' or the armoured cars' own
        /// at a military camp (R-UNIT-008).</summary>
        public static string Course(UnitType t) =>
            t.Class == Combat.ArtilleryClass ? "artillery training" : t.Class == Armour ? "armoured car training"
            : t.Facility == "military_camp" ? "basic training" : t.Facility == Industry ? "craft training" : "pioneer training";

        /// <summary>The course of <see cref="Course"/> as a report Arg (#1682): the UI words it from why.cause.course.&lt;id&gt;.</summary>
        public static string CourseId(UnitType t) =>
            t.Class == Combat.ArtilleryClass ? "artillery" : t.Class == Armour ? "armour"
            : t.Facility == "military_camp" ? "basic" : t.Facility == Industry ? "craft" : "pioneer";

        /// <summary>The armoured car's class (R-UNIT-001).</summary>
        const string Armour = "armour";

        /// <summary>What a finished military cohort is, in text the player reads (R-UNIT-003, R-UNIT-008).</summary>
        static string Trained(UnitType t) => t.Class == Combat.ArtilleryClass ? "artillery" : t.Class == Armour ? "armoured cars" : "recruits";

        static string Groups(int n) => n == 1 ? "1 group" : FormattableString.Invariant($"{n} groups");

        /// <summary>R-TRAIN-001, R-CAMP-001, R-CRAFT-001: whether <paramref name="city"/> has <paramref name="facility"/>: one it
        /// starts with (cities.yaml, D-D5), a camp site of that kind built for it (sites.yaml city), or, for industry, a
        /// completed industry project in it (R-PROJ-003). A camp or a project being built does not count.</summary>
        public static bool HasFacility(Content c, GameState s, string city, string facility) =>
            c.Cities.FirstOrDefault(k => k.Id == city)?.Facilities.Contains(facility) == true   // an annexed city (R-CTRL-016) has none
            || (facility == Industry && s.CityProjects.Values.Any(p => p.City == city && p.Kind == Industry && p.BuiltOn != null))
            || c.Sites.Any(x => x.City == city && x.Kinds.Contains(facility) && s.Sites.TryGetValue(x.Id, out var w) && w.BuiltOn != null);

        /// <summary>R-TRAIN-001: the city's civilians without work, the most a Train order there accepts. Craftsmen take the
        /// city's jobs first (R-CRAFT-002), so only the employed beyond its craftsmen are civilians in work.</summary>
        public static int Idle(City city) => city.Civilians - Math.Max(0, city.Employed - city.Craftsmen);

        /// <summary>R-TRAIN-001 with R-ARMS-005 and R-CITY-011: <see cref="Idle(City)"/>, less the city's staffed arms
        /// workshops' workers and its builders, who hold no job but are not idle (each one is a craftsman without a job first,
        /// then a civilian), as <see cref="Cities.MaxWorkers(GameState, City)"/> takes them off the jobs. The same number where
        /// no workshop is staffed and no project has a builder (always, outside gdd-0.3).</summary>
        public static int Idle(GameState s, City city) => Idle(city, city.Employed, Workshops.Staffed(s, city.Id) + Projects.Builders(s, city.Id));

        static int Idle(City city, int employed, int staffed) =>
            Math.Max(0, Math.Min(city.Civilians - Math.Max(0, employed - city.Craftsmen), city.Civilians + city.Craftsmen - employed - staffed));

        /// <summary>The groups the Train, Reinforce, ReassignCraftsmen and ReassignPioneers orders in <paramref name="pending"/>
        /// (this turn's queue) take from <paramref name="city"/>'s idle civilians, every track together (#1548; owner, 24 October
        /// 2026: "Yes, count them").</summary>
        public static int Ordered(string city, IEnumerable<Command> pending) =>
            pending.OfType<Train>().Where(t => t.City == city).Sum(t => t.Count) + pending.OfType<Reinforce>().Where(t => t.City == city).Sum(t => t.Count)
            + pending.OfType<ReassignCraftsmen>().Where(t => t.City == city).Sum(t => t.Count) + pending.OfType<ReassignPioneers>().Where(t => t.City == city).Sum(t => t.Count);

        /// <summary>R-TRAIN-001 with R-CITY-001: the groups still free to train in <paramref name="city"/> once the orders in
        /// <paramref name="pending"/> (this turn's queue) have run: <see cref="Idle"/>, less every track's Train order there
        /// and every queued reassignment (R-QUAL-004, R-QUAL-005), less a pending "job" AssignWorkers' rise in jobs, and less
        /// a pending "workshop" AssignWorkers' rise in staffed arms workshops (R-ARMS-005), and less the city's builders and
        /// the builder each queued gdd-0.3 StartProject there may take (R-CITY-011). A count up to
        /// it keeps every one of those orders accepted, whatever order they run in. A pending CancelTraining is not counted, since it may run after the Train.</summary>
        public static int Available(GameState s, string city, IEnumerable<Command> pending)
        {
            var x = s.Cities[city];
            var list = pending as IReadOnlyCollection<Command> ?? pending.ToList();
            int Pending(string role, int now) =>
                Math.Max(now, list.OfType<AssignWorkers>().Where(w => w.City == city && w.Role == role).Select(w => w.Count).DefaultIfEmpty(0).Max());
            return Math.Max(0, Idle(x, Pending("job", x.Employed), Pending(Workshops.Role, Workshops.Staffed(s, city)) + Projects.Builders(s, city) + Projects.Starting(s, city, list)) - Ordered(city, list));
        }

        /// <summary>R-TRAIN-001: <paramref name="count"/> of the city's civilians without work start training as
        /// <paramref name="track"/>, a unit type with a facility. The Treasury pays count * training_cost now and borrows what it
        /// lacks. The trainees leave the civilians at once, and the cohort takes the next id. Refused, state untouched, when the
        /// city or track is unknown, the count is below 1, the city has no such facility (<see cref="HasFacility"/>), or it has fewer civilians without
        /// work. Step has already refused it in the legacy loop. On a Historical ledger game the trainees are records
        /// (<see cref="LedgerStart"/>, #1725 PR 7).</summary>
        public static string? Start(Content c, GameState s, string city, string track, int count, List<Cause> why) => Start(c, s, city, track, count, why, null);

        /// <summary>R-TRAIN-004 (#1510; owner, 23 October 2026: "Replacements cost Arms" and "Default (Recommended)"):
        /// <paramref name="count"/> groups of <paramref name="city"/>'s civilians without work train as replacements for the
        /// player's formation <paramref name="unit"/>, which stands on the city's settlement hex. The course is the trained type
        /// of the unit's side and class (a veteran soldier's is the recruit's basic training), and <see cref="Start"/>'s checks,
        /// Money and Arms apply. Refused, state untouched, for a unit that is unknown, not military or not the player's, an
        /// unknown city, a unit off the city's hex, a type no course trains, or a count past its <see cref="Unit.Full"/> less
        /// its strength and the replacements already training for it.</summary>
        public static string? Reinforce(Content c, GameState s, int unit, string city, int count, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the replacements are records
            if (!s.Units.TryGetValue(unit, out var u)) return Orders.RefusedNoUnit;
            if (u.Side == "") return Orders.RefusedNotMilitary;
            if (u.Side != Movement.PlayerSide) return Orders.RefusedNotYours;
            if (!s.Cities.ContainsKey(city)) return RefusedNoCity;
            var hex = c.SettlementHexes.First(h => h.Id == city);
            if ((u.Q, u.R) != (hex.Q, hex.R)) return Refused(RefusedNotInCity, unit, PlayerText.Settlement(c, city));
            var type = c.UnitTypes.First(t => t.Id == u.Type);
            var course = c.UnitTypes.FirstOrDefault(t => t.Side == type.Side && t.Class == type.Class && t.Facility != "");
            if (course == null) return Refused(RefusedNoCourse, unit);
            if (s.PopulationLedger.IsHistorical)
            {
                // Owner, 8 November 2026, "Full minus strength (Rec.)" (#1725 issuecomment-6724941867): the Reinforce room is Unit.Full minus
                // Strength minus training, and a formation whose strength differs from its serving records is refused.
                var serving = Serving(s, FormationTarget(c, u.Id));
                if (serving != u.Strength) return Refused(LedgerRefusedStrength, unit, serving, u.Strength);
            }
            var room = Room(s, u);
            if (room <= 0) return Refused(RefusedNoLosses, unit);
            if (count > room) return Refused(room == 1 ? RefusedRoomOne : RefusedRoomMany, unit, room);
            return Start(c, s, city, course.Id, count, why, unit);
        }

        /// <summary>R-TRAIN-004: the groups <paramref name="u"/> can still take: the strength it had before its first loss, less
        /// its strength and the replacements already training for it (0 for a unit that has lost nobody). The unit card reads
        /// it for the Reinforce button's count.</summary>
        public static int Room(GameState s, Unit u) =>
            (u.Full == 0 ? u.Strength : u.Full) - u.Strength - s.Cohorts.Values.Where(k => k.Unit == u.Id).Sum(k => k.Count);

        static string? Start(Content c, GameState s, string city, string track, int count, List<Cause> why, int? unit)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the trainees are records
            if (!s.Cities.TryGetValue(city, out var x)) return RefusedNoCity;
            var type = c.UnitTypes.FirstOrDefault(t => t.Id == track && t.Facility != "");
            if (type == null) return RefusedNoTraining;
            if (count < 1) return RefusedCount;
            if (s.PopulationLedger.IsHistorical && type.Id == Craftsman) return LedgerRefusedCraftsman;
            var name = PlayerText.Settlement(c, city);
            if (!HasFacility(c, s, city, type.Facility)) return Refused(type.Facility == "military_camp" ? RefusedNoCamp : type.Facility == Industry ? RefusedNoIndustry : RefusedNoHakhshara, name);
            if (s.PopulationLedger.IsHistorical) return LedgerStart(c, s, city, type, count, why, unit, name);
            var idle = Idle(s, x);
            if (count > idle) return Refused(RefusedIdle, name, idle);
            // R-ARMS-003 (D-G6, Q-UNIT-3): a recruit group takes arms_per_recruit_group Arms when its training starts, a gun or
            // armoured car crew its type's training_arms (R-UNIT-008, Q-ARTY-3); a short stock refuses the order. Pioneer
            // training takes none. The cohort keeps the count, and cancelling (R-TRAIN-002) returns it.
            var arms = type.Id == "recruit" ? (long)count * c.RulesAt(s.Date).Mvp!.ArmsPerRecruitGroup!.Value : (long)count * (type.TrainingArms ?? 0);
            if (arms > s.Arms) return Refused(RefusedArms, name, Course(type), arms, s.Arms);
            // R-HOUSE-003: trainees remain housed as a cohort, and recruits keep their home city's housing as formations.
            // A pioneer unit leaves city housing; a craftsman remains. The employed share also reads the new counts.
            // R-QUAL-006: the trainees are taken from the unqualified civilians first; a qualified one taken loses its
            // qualification, which is part of the civilians that remain.
            var lost = Math.Max(0, x.Qualified.Total - (x.Civilians - count));
            s.Cities[city] = x.WithCounts(x.Civilians - count, x.Employed, x.Refugees).WithQualified(lost == 0 ? x.Qualified : x.Qualified.Lose(lost));
            Open(c, s, city, name, type, count, arms, (int)arms, unit, null, why);
            return null;
        }

        /// <summary><see cref="Start"/>'s last part, every check passed, on either path: the Treasury pays count *
        /// training_cost, the cohort opens with the next id, holding <paramref name="pooled"/> of the <paramref name="arms"/>
        /// its training takes from the stock, and the Why lines are written.</summary>
        static void Open(Content c, GameState s, string city, string name, UnitType type, int count, long arms, int pooled, int? unit, IEnumerable<int>? members, List<Cause> why)
        {
            var cost = count * type.TrainingCost;
            // Owner decision "Allow borrowing" (2026-10-15, #468 question 1): a short Treasury borrows, as map actions do.
            EconomySystem.PayOrBorrow(c, s, cost, "for training", why, CauseKeys.Purpose.Training);
            var id = s.NextId++;
            // The plan's reading of D-A6, not part of the owner's ruling: the turn of the order is the first training turn, so
            // with training_turns 3 an order on turn T is done at the end of T+2 (TrainingTests.ACohortOrderedOnTurnTWithThreeTurnsFinishesAtTheEndOfTurnTPlusTwo). For "done at the end of
            // T+3", open the cohort with type.TrainingTurns + 1 here; nothing else changes.
            s.Cohorts.Add(id, new Cohort(id, type.Id, city, count, type.TrainingTurns, pooled, unit, members));
            var aim = unit is int target ? FormattableString.Invariant($" as replacements for unit {target}") : "";
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} began {Course(type)}{aim}, cost {PlayerText.Money(c, "treasury", cost)}", key: CauseKeys.TrainingStarted, args: new[] { CauseKeys.Number(count), CourseId(type), unit.HasValue ? CauseKeys.Number(unit.Value) : "", CauseKeys.Number(cost) }));
            if (arms > 0)
            {
                s.Arms -= (int)arms;
                why.Add(new Cause("city", "arms", -arms, FormattableString.Invariant($"{name}: {Course(type)} took {arms} Arms"), key: CauseKeys.TrainingArms, args: new[] { city, CourseId(type) }));
            }
        }

        /// <summary>R-TRAIN-002 (D-A3): the cohort's people return to its city as civilians without work, and the Money spent is
        /// not returned. The Arms its training took go back to the pool (R-ARMS-003; owner, #1047: "Refund on cancel"). Refused,
        /// state untouched, for a cohort that does not exist, including one that has finished. Step has already refused it in
        /// the legacy loop. On a Historical ledger game the cohort's records are released (<see cref="Release"/>, #1725 PR 7).</summary>
        public static string? Cancel(Content c, GameState s, int cohort, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the cohort's records go back
            if (!s.Cohorts.TryGetValue(cohort, out var k)) return RefusedNoCohort;
            if (s.PopulationLedger.IsHistorical)
            {
                var (released, issued) = Release(s, k);
                s.PopulationLedger.Commit(released, Array.Empty<PopulationTarget>());
                Cancelled(c, s, k, issued, why);
                return null;
            }
            var x = s.Cities[k.City];
            s.Cities[k.City] = x.WithCounts(x.Civilians + k.Count, x.Employed, x.Refugees);
            Cancelled(c, s, k, 0, why);
            return null;
        }

        /// <summary><see cref="Cancel"/>'s state part on either path: the cohort leaves, with its Why lines, and its Arms (the
        /// <paramref name="issued"/> items a ledger course issued plus the cohort's own Arms) go back to the stock. The Money
        /// is not refunded.</summary>
        static void Cancelled(Content c, GameState s, Cohort k, int issued, List<Cause> why)
        {
            s.Cohorts.Remove(k.Id);
            var type = c.UnitTypes.First(t => t.Id == k.Type);
            why.Add(new Cause("city", k.City, k.Count,
                $"{PlayerText.Settlement(c, k.City)}: {Groups(k.Count)} left {Course(type)}; the Money spent is not returned", key: CauseKeys.TrainingCancelled, args: new[] { CauseKeys.Number(k.Count), CourseId(type) }));
            var arms = (long)issued + k.Arms;
            if (arms > 0)
            {
                var back = (int)Math.Min(arms, (long)int.MaxValue - s.Arms);   // the stock stops at int.MaxValue, as an event or a purchase leaves it
                s.Arms += back;
                why.Add(new Cause("city", "arms", back, FormattableString.Invariant($"{PlayerText.Settlement(c, k.City)}: {Course(type)} returned {back} Arms"), key: CauseKeys.TrainingArmsReturned, args: new[] { k.City, CourseId(type) }));
            }
        }

        /// <summary>R-TRAIN-003, step 4 of the turn (spec 2026-10-12 section 2.3, D-A6): in ascending id, each cohort has one turn
        /// less to go. One that reaches 0 leaves the state: a recruit, gun or armoured car cohort becomes one military formation
        /// of its type with its count as strength (R-UNIT-003, R-UNIT-008), craftsmen join the city's count, and halutzim become one unit per group at its hex. On a Historical ledger game the
        /// records change too (<see cref="LedgerProgress"/>, #1725 PR 7).</summary>
        public static void Progress(Content c, GameState s, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the cohorts' records progress
            if (s.PopulationLedger.IsHistorical) { LedgerProgress(c, s, why); return; }
            foreach (var k in s.Cohorts.Values.ToList())
            {
                // R-TRAIN-004 (owner, 23 October 2026, "Default"): replacements whose unit is gone (destroyed, captured, sent
                // home) or off the city's hex are cancelled as R-TRAIN-002 cancels a cohort, before they lose a turn.
                Unit? target = null;
                if (k.Unit is int aim)
                {
                    var at = c.SettlementHexes.First(h => h.Id == k.City);
                    if (!s.Units.TryGetValue(aim, out target) || (target.Q, target.R) != (at.Q, at.R))
                    {
                        why.Add(Stopped(c, k, aim));
                        Cancel(c, s, k.Id, why);
                        continue;
                    }
                }
                if (k.TurnsLeft > 1) { s.Cohorts[k.Id] = k.WithTurnsLeft(k.TurnsLeft - 1); continue; }
                s.Cohorts.Remove(k.Id);
                var type = c.UnitTypes.First(t => t.Id == k.Type);
                var name = PlayerText.Settlement(c, k.City);
                if (target != null) Joined(c, s, k, type, name, target.Id, why);
                else if (type.Side != "") Formed(c, s, k, type, name, s.NextId++, why);
                else if (type.Kind == "unit")
                {
                    var first = s.NextId;
                    s.NextId += k.Count;
                    Halutzim(c, s, k, type, name, first, null, why);
                }
                else if (type.Id == Craftsman)
                {
                    var x = s.Cities[k.City];
                    s.Cities[k.City] = x.WithCraftsmen(x.Craftsmen + k.Count);
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type)} and are craftsmen", key: CauseKeys.CraftsmenTrained, args: new[] { CauseKeys.Number(k.Count), CourseId(type) }));
                }
            }
        }

        /// <summary>R-TRAIN-004: the line for replacements whose unit <paramref name="aim"/> is gone or off the city's hex.</summary>
        static Cause Stopped(Content c, Cohort k, int aim) => new Cause("city", k.City, 0, FormattableString.Invariant($"{PlayerText.Settlement(c, k.City)}: unit {aim} is no longer in {PlayerText.Settlement(c, k.City)}, so its replacements stop training"), key: CauseKeys.ReplacementsStopped, args: new[] { CauseKeys.Number(aim) });

        /// <summary>R-TRAIN-004: finished replacements join map unit <paramref name="unit"/>.</summary>
        static void Joined(Content c, GameState s, Cohort k, UnitType type, string name, int unit, List<Cause> why)
        {
            // The unit keeps its type, service and order (a veteran stays one); full again, it forgets its Full.
            var target = s.Units[unit];
            var strength = target.Strength + k.Count;
            s.Units[target.Id] = new Unit(target.Id, target.Type, target.Q, target.R, target.Service, target.Side, strength,
                target.Order, target.OrderTarget, target.HomeCity, strength >= target.Full ? 0 : target.Full, target.CombatXp, target.Records);
            why.Add(new Cause("city", k.City, k.Count, FormattableString.Invariant($"{name}: {Groups(k.Count)} finished {Course(type)} and joined unit {target.Id}"), key: CauseKeys.ReplacementsJoined, args: new[] { CauseKeys.Number(k.Count), CourseId(type), CauseKeys.Number(target.Id) }));
        }

        /// <summary>R-UNIT-003, R-UNIT-008: a finished recruit, gun or armoured car cohort is map unit <paramref name="id"/>.</summary>
        static void Formed(Content c, GameState s, Cohort k, UnitType type, string name, int id, List<Cause> why)
        {
            var hex = c.SettlementHexes.First(h => h.Id == k.City);
            s.Units.Add(id, new Unit(id, type.Id, hex.Q, hex.R, side: type.Side, strength: k.Count, homeCity: k.City));
            why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type)} and are {Trained(type)}", key: CauseKeys.MilitaryTrained, args: new[] { CauseKeys.Number(k.Count), CourseId(type), type.Class == Combat.ArtilleryClass ? "artillery" : type.Class == Armour ? "armour" : "recruit" }));
        }

        /// <summary>R-TRAIN-003: finished halutzim are one map unit per group, ids <paramref name="first"/> on. On a Historical
        /// ledger game <paramref name="records"/> are the cohort's records, ascending, one per group, and unit
        /// <c>first + i</c> is linked to record i (owner, 9 November 2026, "Add the link first (Recommended)" and "List on
        /// the unit (Recommended)"); null on any other game.</summary>
        static void Halutzim(Content c, GameState s, Cohort k, UnitType type, string name, int first, IReadOnlyList<int>? records, List<Cause> why)
        {
            var hex = c.SettlementHexes.First(h => h.Id == k.City);   // every settlement has one (ValidateMap)
            for (int i = 0; i < k.Count; i++)
                s.Units.Add(first + i, new Unit(first + i, type.Id, hex.Q, hex.R, records: records == null ? null : new[] { records[i] }));
            why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type)} and are halutzim", key: CauseKeys.PioneersTrained, args: new[] { CauseKeys.Number(k.Count), CourseId(type) }));
        }

        // ---- #1725 PR 7, producer 12: training on a Historical ledger game (the dark start, R-POP-002) ----

        /// <summary>The source of a ledger trainee's issued military item: this prefix, the cohort id and the population id
        /// (<c>training/&lt;cohort&gt;/&lt;id&gt;</c>). It is disjoint from the census, units_1948, field and basic_training
        /// prefixes, and names the course that issued it, so a cancel returns only that course's items (R-ARMS-003).</summary>
        internal const string IssuedPrefix = "training/";

        /// <summary>The ActiveService target of a formation a cohort becomes at graduation: this prefix and the map unit id
        /// (owner, 8 November 2026, "New unit at graduation (Rec.)" (#1725 issuecomment-6724941867): a new formation target is bound at
        /// graduation, one new formation per cohort, R-UNIT-003).</summary>
        internal const string FormationPrefix = "formation/";

        static string IssuedId(int cohort, int id) => IssuedPrefix + cohort.ToString(CultureInfo.InvariantCulture) + "/" + id.ToString(CultureInfo.InvariantCulture);

        /// <summary>A track that runs on the ledger: every trained type but the craftsman (owner, 8 November 2026,
        /// "Recruit+crews+halutz (Rec.)" (#1725 issuecomment-6724941867): the recruit, crew and halutz tracks run, and the craftsman track is
        /// refused with a plain reason until #2476).</summary>
        static bool RunsOnLedger(UnitType t) => t.Facility != "" && t.Id != Craftsman;

        /// <summary>The facilities of <paramref name="facilities"/> some ledger track trains at, in ordinal order, each once.</summary>
        internal static IEnumerable<string> LedgerFacilities(Content c, IEnumerable<string> facilities) => facilities
            .Where(f => c.UnitTypes.Any(t => RunsOnLedger(t) && t.Facility == f)).Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal);

        /// <summary>Owner, 8 November 2026, "Cohorts stay main (Rec.)" (#1725 issuecomment-6724941867): each trainee's Course target is per
        /// facility (military_camp/&lt;city&gt;, hakhshara/&lt;city&gt;).</summary>
        internal static string CourseTarget(string facility, string city) => facility + "/" + city;

        /// <summary><see cref="CourseTarget"/> as a ledger binding in <paramref name="region"/>, the city's. Its progress limit
        /// is the longest training_turns of the ledger tracks at that facility, so no record's course ticks can pass it.</summary>
        internal static PopulationTarget CourseTargetOf(Content c, string facility, string city, string region) =>
            new PopulationTarget(CourseTarget(facility, city), PopulationAssignmentKind.Course, region,
                c.UnitTypes.Where(t => RunsOnLedger(t) && t.Facility == facility).Max(t => t.TrainingTurns));

        /// <summary>The unit_types.yaml classes of the military tracks (recruit, the gun and armoured car crews): a graduate's
        /// military qualification, as the opening soldiers' is their formation type's class (#1689), so the ledger binds them
        /// at Initialize (owner, 8 November 2026, "Start + runtime bind (Rec.)").</summary>
        internal static IEnumerable<string> TrainedClasses(Content c) => c.UnitTypes
            .Where(t => RunsOnLedger(t) && t.Side != "" && !string.IsNullOrWhiteSpace(t.Class)).Select(t => t.Class).Distinct(StringComparer.Ordinal);

        /// <summary>The ActiveService target of map unit <paramref name="unit"/>: a starting force's own (unit id i is
        /// <c>c.StartingForces[i]</c>, StartingForces.Init; R-POP-004), else the one bound at its graduation.</summary>
        internal static string FormationTarget(Content c, int unit) => unit >= 0 && unit < c.StartingForces.Count
            ? HistoricalPopulation.FormationPrefix + c.StartingForces[unit].Row
            : FormationPrefix + unit.ToString(CultureInfo.InvariantCulture);

        static int Serving(GameState s, string target) => s.PopulationLedger.Units.Count(p =>
            p.Status == PopulationStatus.ActiveSoldier && string.Equals(p.Assignment.Target, target, StringComparison.Ordinal));

        static string CityRegion(Content c, string city) => c.PlayerRegions.AtLocation(city)?.Id
            ?? throw new InvalidOperationException("No region in regions.yaml for " + city + " (R-POP-004).");

        /// <summary>R-TRAIN-001 and R-QUAL-006 on the ledger: the city's free-civilian records without an assignment, located in
        /// the city. Owner, 8 November 2026, "Unskilled first, keep (Rec.)" (#1725 issuecomment-6724941867): civilians with no profession first, then
        /// the qualified, each in ascending id. A refugee, a worker, a farmer on a field and a teaching teacher are not idle.</summary>
        static List<PopulationUnit> LedgerIdle(GameState s, string city) => s.PopulationLedger.Units
            .Where(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned
                && string.Equals(p.Location?.City, city, StringComparison.Ordinal))
            .OrderBy(p => p.Profession == CivilianProfession.None ? 0 : 1).ThenBy(p => p.Id).ToList();

        /// <summary>R-TRAIN-001, R-TRAIN-004, R-ARMS-003 and R-QUAL-006 on the ledger, after <see cref="Start"/>'s city, track,
        /// count and facility checks. Then the idle records (<see cref="LedgerIdle"/>), the camp's capacity and the Arms are
        /// checked, all before the first write. The first <paramref name="count"/> idle records become trainees on the
        /// facility's course in the same place (27M-7), keeping their profession dormant (owner, 8 November 2026, "Unskilled
        /// first, keep (Rec.)"), and are validated as one candidate with any Course target a built camp still needs
        /// (owner, "Start + runtime bind (Rec.)"). The Money, the cohort's id and the Why lines are the aggregate game's; no
        /// city field is written, but for City.Qualified when the order takes a returned pioneer (R-QUAL-006; owner, 9 November
        /// 2026, "Yes, lower it (Recommended)").</summary>
        static string? LedgerStart(Content c, GameState s, string city, UnitType type, int count, List<Cause> why, int? unit, string name)
        {
            var ledger = s.PopulationLedger;
            var idle = LedgerIdle(s, city);
            if (count > idle.Count) return Refused(RefusedIdle, name, idle.Count);
            var target = CourseTarget(type.Facility, city);
            if (type.Facility == "military_camp")
            {
                // Owner, 8 November 2026, "Enforce it here (Rec.)" (#1725 issuecomment-6724941867): the camp capacity, 50 records per camp city,
                // over all military tracks, is enforced here. military.camp_capacity counts population units at D2's 1,000 people (#2739, option 1), which is a
                // Historical ledger's own scale, so the capacity is in records.
                var capacity = Gdd03MilitaryCatalogue.ReadCampCapacity(c.Gdd03Balance
                    ?? throw new InvalidOperationException("The camp capacity needs gdd03-balance.yaml (R-POP-002)."));
                var room = capacity - ledger.Units.Count(p => p.Status == PopulationStatus.Trainee
                    && string.Equals(p.Assignment.Target, target, StringComparison.Ordinal));
                if (count > room) return Refused(LedgerRefusedCampFull, name, Math.Max(0, room), capacity);
            }
            // R-ARMS-003 as in Start. Owner, 8 November 2026, "Keep costs, pool rest (Rec.)" (#1725 issuecomment-6724941867): one item is issued
            // per record, and the rest of training_arms is taken from the stock and kept in Cohort.Arms, so a cancel returns it.
            var per = type.Id == "recruit" ? c.RulesAt(s.Date).Mvp!.ArmsPerRecruitGroup!.Value : type.TrainingArms ?? 0;
            var arms = (long)count * per;
            if (arms > s.Arms) return Refused(RefusedArms, name, Course(type), arms, s.Arms);
            var taken = idle.Take(count).ToList();
            // Owner, 8 November 2026, Q3, "Refuse, with reason (Recommended)": a military trainee who already carries a
            // military item, and a farmer or craftsman on the halutz track, are refused with a plain reason before any write.
            if (per > 0 && taken.Any(p => p.Equipment.MilitaryId != null)) return Refused(LedgerRefusedCarried, name);
            if (type.Side == "" && taken.Any(p => p.Profession == CivilianProfession.Farmer || p.Profession == CivilianProfession.Craftsman))
                return Refused(LedgerRefusedHalutzTrade, name);
            // R-QUAL-006 on the ledger (owner, 9 November 2026, "Yes, lower it (Recommended)"): a returned pioneer record the
            // order takes loses its qualification, so the city's qualified pioneers (City.Qualified) never outnumber its
            // free pioneer records. As the aggregate path counts it, only what the remaining records cannot hold is lost.
            // The training centre's census teacher (#1689), while untouched, is not a returned pioneer, so it holds no
            // qualified group; once linked to a unit it is a pioneer like any other (HistoricalPopulation.UntouchedTeacherId).
            var x = s.Cities[city];
            var teacher = HistoricalPopulation.UntouchedTeacherId(ledger);
            bool CityPioneer(PopulationUnit p) => p.Status == PopulationStatus.FreeCivilian && p.Profession == CivilianProfession.Pioneer
                && string.Equals(p.Location?.City, city, StringComparison.Ordinal) && p.Id != teacher;
            var pioneers = ledger.Units.Count(CityPioneer) - taken.Count(CityPioneer);
            var lost = Math.Max(0, x.Qualified.Halutzim.Count + x.Qualified.Veterans - pioneers);
            var id = s.NextId;
            var course = new PopulationAssignment(PopulationAssignmentKind.Course, target);
            var bind = ledger.Target(target) == null
                ? new[] { CourseTargetOf(c, type.Facility, city, CityRegion(c, city)) } : Array.Empty<PopulationTarget>();
            ledger.Commit(taken.Select(p =>
            {
                var e = p.Equipment;
                return new PopulationUnit(p.Id, PopulationStatus.Trainee, p.ReceptionTicks, p.Location, course, p.Profession, p.Tier,
                    p.WorkXp, p.Qualification, p.CombatXp, per > 0 ? new PopulationEquipment(e.ToolsId, e.ToolsPaid, IssuedId(id, p.Id), true) : e,
                    p.Captor, p.Home);
            }).ToList(), bind);
            if (lost > 0) s.Cities[city] = x.WithQualified(x.Qualified.LosePioneers(lost));
            Open(c, s, city, name, type, count, arms, (int)(arms - (per > 0 ? count : 0)), unit, taken.Select(p => p.Id), why);
            return null;
        }

        /// <summary>The cohort's trainee records, ascending id (owner, 8 November 2026, "Cohorts stay main (Rec.)").</summary>
        static List<PopulationUnit> Members(GameState s, Cohort k)
        {
            var members = k.Members ?? throw new InvalidOperationException("Cohort " + k.Id.ToString(CultureInfo.InvariantCulture) + " has no member list on a ledger game.");
            var list = members.Select(s.PopulationLedger.Get).ToList();
            if (list.Any(p => p.Status != PopulationStatus.Trainee || p.Assignment.Kind != PopulationAssignmentKind.Course))
                throw new InvalidOperationException("Cohort " + k.Id.ToString(CultureInfo.InvariantCulture) + " has a member who is not its trainee.");
            return list;
        }

        /// <summary>R-TRAIN-002 on the ledger, the records' part: each member is a free civilian without a job again, in place,
        /// its reception kept (docs/GDD.md:374, 27M-9). Only the item this course issued leaves the record, to go back to the
        /// stock (R-ARMS-003, "the Arms it took"); a carried item stays. Returns the records and the items returned.</summary>
        static (List<PopulationUnit> Records, int Issued) Release(GameState s, Cohort k)
        {
            var issued = 0;
            var records = Members(s, k).Select(p =>
            {
                var e = p.Equipment;
                var mine = e.MilitaryId == IssuedId(k.Id, p.Id);
                if (mine) issued++;
                return new PopulationUnit(p.Id, PopulationStatus.FreeCivilian, p.ReceptionTicks, p.Location, PopulationAssignment.Unassigned,
                    p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp, mine ? new PopulationEquipment(e.ToolsId, e.ToolsPaid) : e, p.Captor, p.Home);
            }).ToList();
            return (records, issued);
        }

        /// <summary>A graduate soldier of formation <paramref name="target"/>: an active soldier serving it, with the course's
        /// class as its military qualification and its item kept, its profession dormant (owner, 8 November 2026, "Unskilled
        /// first, keep (Rec.)"). Like an opening soldier (R-POP-004) it has the region-only location of its formation's region
        /// (owner, 8 November 2026, Q2: "Formation region (Recommended)").</summary>
        static PopulationUnit Soldier(PopulationUnit p, string target, string region, string qualification) =>
            new PopulationUnit(p.Id, PopulationStatus.ActiveSoldier, p.ReceptionTicks, new PopulationLocation(region),
                new PopulationAssignment(PopulationAssignmentKind.ActiveService, target), p.Profession, p.Tier, p.WorkXp, qualification,
                p.CombatXp, p.Equipment, p.Captor, p.Home);

        /// <summary>Owner, 8 November 2026, "Free pioneer, not housed (Rec.)" (#1725 issuecomment-6724941867): a halutz graduate is a free
        /// civilian, Pioneer, Novice and unassigned, in the city's region with no city, so not housed. A record that is a pioneer already keeps its tier and work XP
        /// (its profession kept, "Unskilled first, keep (Rec.)"; owner, 8 November 2026, Q1: "Keeps tier and XP (Recommended)").</summary>
        static PopulationUnit Pioneer(PopulationUnit p, string region) =>
            new PopulationUnit(p.Id, PopulationStatus.FreeCivilian, p.ReceptionTicks, new PopulationLocation(region), PopulationAssignment.Unassigned,
                CivilianProfession.Pioneer, p.Profession == CivilianProfession.Pioneer ? p.Tier : CivilianTier.Novice,
                p.Profession == CivilianProfession.Pioneer ? p.WorkXp : 0, p.Qualification, p.CombatXp, p.Equipment, p.Captor, p.Home);

        /// <summary>R-TRAIN-003 and R-TRAIN-004 on the ledger: <see cref="Progress"/>'s order and Why lines, with each cohort's
        /// records changed as its outcome says. A cohort with turns to go counts one course tick on each record; replacements
        /// whose unit is gone are released as <see cref="Cancel"/> releases them; replacements that finish serve their
        /// formation; a recruit or crew cohort that finishes serves a new formation target bound for its new map unit (owner,
        /// 8 November 2026, "New unit at graduation (Rec.)"); halutzim become free pioneers (<see cref="Pioneer"/>), each map unit linked to its record (<see cref="Unit.Records"/>). Every
        /// record change and new target of the turn is validated as one candidate before any write; the state's writes then
        /// follow in cohort order, taking the same ids as the aggregate step.</summary>
        static void LedgerProgress(Content c, GameState s, List<Cause> why)
        {
            var ledger = s.PopulationLedger;
            var records = new List<PopulationUnit>();
            var bind = new List<PopulationTarget>();
            var apply = new List<Action>();
            var next = s.NextId;
            foreach (var k in s.Cohorts.Values.ToList())
            {
                var members = Members(s, k);
                Unit? target = null;
                if (k.Unit is int aim)
                {
                    var at = c.SettlementHexes.First(h => h.Id == k.City);
                    if (!s.Units.TryGetValue(aim, out target) || (target.Q, target.R) != (at.Q, at.R))
                    {
                        var (released, issued) = Release(s, k);
                        records.AddRange(released);
                        apply.Add(() => { why.Add(Stopped(c, k, aim)); Cancelled(c, s, k, issued, why); });
                        continue;
                    }
                }
                if (k.TurnsLeft > 1)
                {
                    records.AddRange(members.Select(p => p.WithPlacement(p.Status, p.Location,
                        new PopulationAssignment(PopulationAssignmentKind.Course, p.Assignment.Target, checked(p.Assignment.ProgressTicks + 1)), p.Captor, p.Home)));
                    apply.Add(() => s.Cohorts[k.Id] = k.WithTurnsLeft(k.TurnsLeft - 1));
                    continue;
                }
                var type = c.UnitTypes.First(t => t.Id == k.Type);
                var name = PlayerText.Settlement(c, k.City);
                if (target != null)
                {
                    var formation = FormationTarget(c, target.Id);
                    var region = ledger.Target(formation)?.Region
                        ?? throw new InvalidOperationException("Unit " + target.Id.ToString(CultureInfo.InvariantCulture) + " has no formation target on the ledger.");
                    records.AddRange(members.Select(p => Soldier(p, formation, region, type.Class)));
                    var joined = target.Id;
                    apply.Add(() => { s.Cohorts.Remove(k.Id); Joined(c, s, k, type, name, joined, why); });
                }
                else if (type.Side != "")
                {
                    var id = next++;
                    var formation = FormationPrefix + id.ToString(CultureInfo.InvariantCulture);
                    var region = CityRegion(c, k.City);
                    bind.Add(new PopulationTarget(formation, PopulationAssignmentKind.ActiveService, region));
                    records.AddRange(members.Select(p => Soldier(p, formation, region, type.Class)));
                    apply.Add(() => { s.Cohorts.Remove(k.Id); Formed(c, s, k, type, name, id, why); });
                }
                else if (type.Kind == "unit")
                {
                    var region = CityRegion(c, k.City);
                    records.AddRange(members.Select(p => Pioneer(p, region)));
                    var first = next;
                    next = checked(next + k.Count);
                    var linked = members.Select(p => p.Id).ToList();   // ascending id, one per group (Members, Cohort)
                    apply.Add(() => { s.Cohorts.Remove(k.Id); Halutzim(c, s, k, type, name, first, linked, why); HistoricalPopulation.NoteLinked(ledger, linked); });
                }
                else throw new InvalidOperationException("Cohort " + k.Id.ToString(CultureInfo.InvariantCulture) + " of type " + k.Type + " does not run on the population ledger.");
            }
            ledger.Commit(records, bind);
            foreach (var a in apply) a();
            s.NextId = next;
        }
    }
}
