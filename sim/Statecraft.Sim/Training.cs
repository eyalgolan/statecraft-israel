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

        /// <summary>The craftsman's unit type id (D-D4; R-CRAFT-001).</summary>
        public const string Craftsman = "craftsman";

        /// <summary>The facility a craftsman trains at: a completed city project of this kind (projects.yaml; Q-CRAFT-1).</summary>
        public const string Industry = "industry";

        /// <summary>The facility's name in text the player reads.</summary>
        static string Place(string facility) => facility == "military_camp" ? "military camp" : facility == Industry ? "completed industry project" : "Hakhshara";

        /// <summary>The course the facility gives, in text the player reads.</summary>
        static string Course(string facility) => facility == "military_camp" ? "basic training" : facility == Industry ? "craft training" : "pioneer training";

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

        /// <summary>The groups the Train orders in <paramref name="pending"/> (this turn's queue) take from
        /// <paramref name="city"/>, every track together.</summary>
        public static int Ordered(string city, IEnumerable<Command> pending) => pending.OfType<Train>().Where(t => t.City == city).Sum(t => t.Count);

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
        public static string? Start(Content c, GameState s, string city, string track, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            var type = c.UnitTypes.FirstOrDefault(t => t.Id == track && t.Facility != "");
            if (type == null) return "There is no such training";
            if (count < 1) return "Train at least one group";
            var name = PlayerText.Settlement(c, city);
            if (!HasFacility(c, s, city, type.Facility)) return $"{name} has no {Place(type.Facility)}";
            var idle = Idle(x);
            if (count > idle) return FormattableString.Invariant($"{name} has {idle} civilians without work");
            var cost = count * type.TrainingCost;
            // Owner decision "Allow borrowing" (2026-10-15, #468 question 1): a short Treasury borrows, as map actions do.
            EconomySystem.PayOrBorrow(c, s, cost, "for training", why);
            // PLACEHOLDER (owner question, #377): trainees leave the civilians, and housing (R-CITY-004) counts civilians only,
            // so their places are free for waiting refugees while they train. Social cohesion (R-CITY-005) also moves: its
            // employed share is employed over civilians, so it rises when civilians without work leave for training.
            s.Cities[city] = x.WithCounts(x.Civilians - count, x.Employed, x.Refugees);
            var id = s.NextId++;
            // The plan's reading of D-A6, not part of the owner's ruling: the turn of the order is the first training turn, so
            // with training_turns 3 an order on turn T is done at the end of T+2 (TrainingTests.ACohortOrderedOnTurnTWithThreeTurnsFinishesAtTheEndOfTurnTPlusTwo). For "done at the end of
            // T+3", open the cohort with type.TrainingTurns + 1 here; nothing else changes.
            s.Cohorts.Add(id, new Cohort(id, type.Id, city, count, type.TrainingTurns));
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} began {Course(type.Facility)}, cost {PlayerText.Money(c, "treasury", cost)}"));
            return null;
        }

        /// <summary>R-TRAIN-002 (D-A3): the cohort's people return to its city as civilians without work, and the Money spent is
        /// not returned. Refused, state untouched, for a cohort that does not exist, including one that has finished. Step has
        /// already refused it in the legacy loop.</summary>
        public static string? Cancel(Content c, GameState s, int cohort, List<Cause> why)
        {
            if (!s.Cohorts.TryGetValue(cohort, out var k)) return "There is no such training group";
            var x = s.Cities[k.City];
            s.Cities[k.City] = x.WithCounts(x.Civilians + k.Count, x.Employed, x.Refugees);
            s.Cohorts.Remove(cohort);
            var type = c.UnitTypes.First(t => t.Id == k.Type);
            why.Add(new Cause("city", k.City, k.Count,
                $"{PlayerText.Settlement(c, k.City)}: {Groups(k.Count)} left {Course(type.Facility)}; the Money spent is not returned"));
            return null;
        }

        /// <summary>R-TRAIN-003, step 4 of the turn (spec 2026-10-12 section 2.3, D-A6): in ascending id, each cohort has one turn
        /// less to go. One that reaches 0 leaves the state: recruits and craftsmen join the city's counts (R-CRAFT-001), and
        /// halutzim become one movable unit per group at the city's settlement hex, taking the next ids in turn.</summary>
        public static void Progress(Content c, GameState s, List<Cause> why)
        {
            foreach (var k in s.Cohorts.Values.ToList())
            {
                if (k.TurnsLeft > 1) { s.Cohorts[k.Id] = k.WithTurnsLeft(k.TurnsLeft - 1); continue; }
                s.Cohorts.Remove(k.Id);
                var type = c.UnitTypes.First(t => t.Id == k.Type);
                var name = PlayerText.Settlement(c, k.City);
                if (type.Kind == "unit")
                {
                    var hex = c.SettlementHexes.First(h => h.Id == k.City);   // every settlement has one (ValidateMap)
                    for (int i = 0; i < k.Count; i++) { var id = s.NextId++; s.Units.Add(id, new Unit(id, type.Id, hex.Q, hex.R)); }
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type.Facility)} and are halutzim"));
                }
                else if (type.Id == Craftsman)
                {
                    var x = s.Cities[k.City];
                    s.Cities[k.City] = x.WithCraftsmen(x.Craftsmen + k.Count);
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type.Facility)} and are craftsmen"));
                }
                else
                {
                    var x = s.Cities[k.City];
                    s.Cities[k.City] = x.WithRecruits(x.Recruits + k.Count);
                    why.Add(new Cause("city", k.City, k.Count, $"{name}: {Groups(k.Count)} finished {Course(type.Facility)} and are recruits"));
                }
            }
        }
    }
}
