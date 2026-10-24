using System;
using System.Collections.Generic;
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

        /// <summary>The facility's name in text the player reads.</summary>
        static string Place(string facility) => facility == "military_camp" ? "military camp" : facility == Industry ? "completed industry project" : "Hakhshara";

        /// <summary>The course a type trains in, in text the player reads: the facility's, or the guns' or the armoured cars' own
        /// at a military camp (R-UNIT-008).</summary>
        static string Course(UnitType t) =>
            t.Class == Combat.ArtilleryClass ? "artillery training" : t.Class == Armour ? "armoured car training"
            : t.Facility == "military_camp" ? "basic training" : t.Facility == Industry ? "craft training" : "pioneer training";

        /// <summary>The armoured car's class (R-UNIT-001).</summary>
        const string Armour = "armour";

        /// <summary>What a finished military cohort is, in text the player reads (R-UNIT-003, R-UNIT-008).</summary>
        static string Trained(UnitType t) => t.Class == Combat.ArtilleryClass ? "artillery" : t.Class == Armour ? "armoured cars" : "recruits";

        static string Groups(int n) => n == 1 ? "1 group" : FormattableString.Invariant($"{n} groups");

        /// <summary>R-TRAIN-001, R-CAMP-001, R-CRAFT-001: whether <paramref name="city"/> has <paramref name="facility"/>: one it
        /// starts with (cities.yaml, D-D5), a camp site of that kind built for it (sites.yaml city), or, for industry, a
        /// completed industry project in it (R-PROJ-003). A camp or a project being built does not count.</summary>
        public static bool HasFacility(Content c, GameState s, string city, string facility) =>
            c.Cities.First(k => k.Id == city).Facilities.Contains(facility)
            || (facility == Industry && s.CityProjects.Values.Any(p => p.City == city && p.Kind == Industry && p.BuiltOn != null))
            || c.Sites.Any(x => x.City == city && x.Kinds.Contains(facility) && s.Sites.TryGetValue(x.Id, out var w) && w.BuiltOn != null);

        /// <summary>R-TRAIN-001: the city's civilians without work, the most a Train order there accepts. Craftsmen take the
        /// city's jobs first (R-CRAFT-002), so only the employed beyond its craftsmen are civilians in work.</summary>
        public static int Idle(City city) => city.Civilians - Math.Max(0, city.Employed - city.Craftsmen);

        /// <summary>The groups the Train and Reinforce orders in <paramref name="pending"/> (this turn's queue) take from
        /// <paramref name="city"/>, every track together.</summary>
        public static int Ordered(string city, IEnumerable<Command> pending) =>
            pending.OfType<Train>().Where(t => t.City == city).Sum(t => t.Count) + pending.OfType<Reinforce>().Where(t => t.City == city).Sum(t => t.Count);

        /// <summary>R-TRAIN-001 with R-CITY-001: the groups still free to train in <paramref name="city"/> once the orders in
        /// <paramref name="pending"/> (this turn's queue) have run: <see cref="Idle"/>, less every track's Train order there,
        /// and less a pending AssignWorkers' rise in jobs. A count up to it keeps every one of those orders accepted, whatever
        /// order they run in. A pending CancelTraining is not counted, since it may run after the Train.</summary>
        public static int Available(GameState s, string city, IEnumerable<Command> pending)
        {
            var x = s.Cities[city];
            var list = pending as IReadOnlyCollection<Command> ?? pending.ToList();
            var employed = Math.Max(x.Employed, list.OfType<AssignWorkers>().Where(w => w.City == city).Select(w => w.Count).DefaultIfEmpty(0).Max());
            var ordinaryEmployed = Math.Max(0, employed - x.Craftsmen);
            return Math.Max(0, x.Civilians - ordinaryEmployed - Ordered(city, list));
        }

        /// <summary>R-TRAIN-001: <paramref name="count"/> of the city's civilians without work start training as
        /// <paramref name="track"/>, a unit type with a facility. The Treasury pays count * training_cost now and borrows what it
        /// lacks. The trainees leave the civilians at once, and the cohort takes the next id. Refused, state untouched, when the
        /// city or track is unknown, the count is below 1, the city has no such facility (<see cref="HasFacility"/>), or it has fewer civilians without
        /// work. Step has already refused it in the legacy loop.</summary>
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
            if (!s.Units.TryGetValue(unit, out var u)) return "There is no such unit";
            if (u.Side == "") return "Only a military unit takes that order";
            if (u.Side != Movement.PlayerSide) return "That unit is not yours";
            if (!s.Cities.ContainsKey(city)) return "There is no such city";
            var hex = c.SettlementHexes.First(h => h.Id == city);
            if ((u.Q, u.R) != (hex.Q, hex.R)) return FormattableString.Invariant($"Unit {unit} is not in {PlayerText.Settlement(c, city)}");
            var type = c.UnitTypes.First(t => t.Id == u.Type);
            var course = c.UnitTypes.FirstOrDefault(t => t.Side == type.Side && t.Class == type.Class && t.Facility != "");
            if (course == null) return FormattableString.Invariant($"Unit {unit} cannot take replacements");
            var room = Room(s, u);
            if (room <= 0) return FormattableString.Invariant($"Unit {unit} has no losses to replace");
            if (count > room) return FormattableString.Invariant($"Unit {unit} can take {room} more {(room == 1 ? "group" : "groups")}");
            return Start(c, s, city, course.Id, count, why, unit);
        }

        /// <summary>R-TRAIN-004: the groups <paramref name="u"/> can still take: the strength it had before its first loss, less
        /// its strength and the replacements already training for it (0 for a unit that has lost nobody). The unit card reads
        /// it for the Reinforce button's count.</summary>
        public static int Room(GameState s, Unit u) =>
            (u.Full == 0 ? u.Strength : u.Full) - u.Strength - s.Cohorts.Values.Where(k => k.Unit == u.Id).Sum(k => k.Count);

        static string? Start(Content c, GameState s, string city, string track, int count, List<Cause> why, int? unit)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            var type = c.UnitTypes.FirstOrDefault(t => t.Id == track && t.Facility != "");
            if (type == null) return "There is no such training";
            if (count < 1) return "Train at least one group";
            var name = PlayerText.Settlement(c, city);
            if (!HasFacility(c, s, city, type.Facility)) return $"{name} has no {Place(type.Facility)}";
            var idle = Idle(x);
            if (count > idle) return FormattableString.Invariant($"{name} has {idle} civilians without work");
            // R-ARMS-003 (D-G6, Q-UNIT-3): a recruit group takes arms_per_recruit_group Arms when its training starts, a gun or
            // armoured car crew its type's training_arms (R-UNIT-008, Q-ARTY-3); a short stock refuses the order. Pioneer
            // training takes none. The cohort keeps the count, and cancelling (R-TRAIN-002) returns it.
            var arms = type.Id == "recruit" ? (long)count * c.RulesAt(s.Date).Mvp!.ArmsPerRecruitGroup!.Value : (long)count * (type.TrainingArms ?? 0);
            if (arms > s.Arms) return FormattableString.Invariant($"{name}: not enough Arms for {Course(type)} (need {arms}, have {s.Arms})");
            var cost = count * type.TrainingCost;
            // Owner decision "Allow borrowing" (2026-10-15, #468 question 1): a short Treasury borrows, as map actions do.
            EconomySystem.PayOrBorrow(c, s, cost, "for training", why);
            // R-HOUSE-003: trainees remain housed as a cohort, and recruits keep their home city's housing as formations.
            // A pioneer unit leaves city housing; a craftsman remains. The employed share also reads the new counts.
            // R-QUAL-006: the trainees are taken from the unqualified civilians first; a qualified one taken loses its
            // qualification, which is part of the civilians that remain.
            var lost = Math.Max(0, x.Qualified.Total - (x.Civilians - count));
            s.Cities[city] = x.WithCounts(x.Civilians - count, x.Employed, x.Refugees).WithQualified(lost == 0 ? x.Qualified : x.Qualified.Lose(lost));
            var id = s.NextId++;
            // The plan's reading of D-A6, not part of the owner's ruling: the turn of the order is the first training turn, so
            // with training_turns 3 an order on turn T is done at the end of T+2 (TrainingTests.ACohortOrderedOnTurnTWithThreeTurnsFinishesAtTheEndOfTurnTPlusTwo). For "done at the end of
            // T+3", open the cohort with type.TrainingTurns + 1 here; nothing else changes.
            s.Cohorts.Add(id, new Cohort(id, type.Id, city, count, type.TrainingTurns, (int)arms, unit));
            var aim = unit is int target ? FormattableString.Invariant($" as replacements for unit {target}") : "";
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} began {Course(type)}{aim}, cost {PlayerText.Money(c, "treasury", cost)}"));
            if (arms > 0)
            {
                s.Arms -= (int)arms;
                why.Add(new Cause("city", "arms", -arms, FormattableString.Invariant($"{name}: {Course(type)} took {arms} Arms")));
            }
            return null;
        }

        /// <summary>R-TRAIN-002 (D-A3): the cohort's people return to its city as civilians without work, and the Money spent is
        /// not returned. The Arms its training took go back to the pool (R-ARMS-003; owner, #1047: "Refund on cancel"). Refused,
        /// state untouched, for a cohort that does not exist, including one that has finished. Step has already refused it in
        /// the legacy loop.</summary>
        public static string? Cancel(Content c, GameState s, int cohort, List<Cause> why)
        {
            if (!s.Cohorts.TryGetValue(cohort, out var k)) return "There is no such training group";
            var x = s.Cities[k.City];
            s.Cities[k.City] = x.WithCounts(x.Civilians + k.Count, x.Employed, x.Refugees);
            s.Cohorts.Remove(cohort);
            var type = c.UnitTypes.First(t => t.Id == k.Type);
            why.Add(new Cause("city", k.City, k.Count,
                $"{PlayerText.Settlement(c, k.City)}: {Groups(k.Count)} left {Course(type)}; the Money spent is not returned"));
            if (k.Arms > 0)
            {
                var back = (int)Math.Min(k.Arms, (long)int.MaxValue - s.Arms);   // the stock stops at int.MaxValue, as an event or a purchase leaves it
                s.Arms += back;
                why.Add(new Cause("city", "arms", back, FormattableString.Invariant($"{PlayerText.Settlement(c, k.City)}: {Course(type)} returned {back} Arms")));
            }
            return null;
        }

        /// <summary>R-TRAIN-003, step 4 of the turn (spec 2026-10-12 section 2.3, D-A6): in ascending id, each cohort has one turn
        /// less to go. One that reaches 0 leaves the state: a recruit, gun or armoured car cohort becomes one military formation
        /// of its type with its count as strength (R-UNIT-003, R-UNIT-008), craftsmen join the city's count, and halutzim become one unit per group at its hex.</summary>
        public static void Progress(Content c, GameState s, List<Cause> why)
        {
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
                        why.Add(new Cause("city", k.City, 0, FormattableString.Invariant($"{PlayerText.Settlement(c, k.City)}: unit {aim} is no longer in {PlayerText.Settlement(c, k.City)}, so its replacements stop training")));
                        Cancel(c, s, k.Id, why);
                        continue;
                    }
                }
                if (k.TurnsLeft > 1) { s.Cohorts[k.Id] = k.WithTurnsLeft(k.TurnsLeft - 1); continue; }
                s.Cohorts.Remove(k.Id);
                var type = c.UnitTypes.First(t => t.Id == k.Type);
                var name = PlayerText.Settlement(c, k.City);
                if (target != null)
                {
                    // The unit keeps its type, service and order (a veteran stays one); full again, it forgets its Full.
                    var strength = target.Strength + k.Count;
                    s.Units[target.Id] = new Unit(target.Id, target.Type, target.Q, target.R, target.Service, target.Side, strength,
                        target.Order, target.OrderTarget, target.HomeCity, strength >= target.Full ? 0 : target.Full);
                    why.Add(new Cause("city", k.City, k.Count, FormattableString.Invariant($"{name}: {Groups(k.Count)} finished {Course(type)} and joined unit {target.Id}")));
                }
                else if (type.Side != "")
                {
                    var hex = c.SettlementHexes.First(h => h.Id == k.City);
                    var id = s.NextId++;
                    s.Units.Add(id, new Unit(id, type.Id, hex.Q, hex.R, side: type.Side, strength: k.Count, homeCity: k.City));
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type)} and are {Trained(type)}"));
                }
                else if (type.Kind == "unit")
                {
                    var hex = c.SettlementHexes.First(h => h.Id == k.City);   // every settlement has one (ValidateMap)
                    for (int i = 0; i < k.Count; i++) { var id = s.NextId++; s.Units.Add(id, new Unit(id, type.Id, hex.Q, hex.R)); }
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type)} and are halutzim"));
                }
                else if (type.Id == Craftsman)
                {
                    var x = s.Cities[k.City];
                    s.Cities[k.City] = x.WithCraftsmen(x.Craftsmen + k.Count);
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type)} and are craftsmen"));
                }
            }
        }
    }
}
