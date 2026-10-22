using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 objectives (docs/GDD.md "Objectives" and "Historical events on objective completion"): at the end of
    /// each GDD turn an objective whose condition holds is recorded once, with the turn's end date, and scores once (R-OBJ-001,
    /// R-OBJ-002); its card fires once it is recorded (R-OBJ-003, in EventSystem). The point values are the owner's (D-B6 and
    /// D-B2, #456) and are read from the rules' mvp: block. Integers only, so the score is the same on every runtime.</summary>
    public static class Objectives
    {
        /// <summary>Calendar months from the month of <paramref name="done"/> to the month of <paramref name="reference"/>:
        /// (y2 − y1) × 12 + (m2 − m1). Positive when the objective was completed early, 0 in the same month ("on time, as in
        /// history"), negative when late (owner's card wording, #379, 14 October 2026). Public so the card can show it (spec
        /// 2026-10-12 §2.6).</summary>
        public static int MonthsFromHistory(DateTime done, DateTime reference) =>
            (reference.Year - done.Year) * 12 + (reference.Month - done.Month);

        /// <summary>The months early the bonus counts: <see cref="MonthsFromHistory"/>, or 0 when on time or late.</summary>
        public static int MonthsEarly(DateTime done, DateTime reference) => Math.Max(0, MonthsFromHistory(done, reference));

        /// <summary>The points an objective completed on <paramref name="done"/> earns (R-OBJ-002): objective_points plus
        /// objective_bonus_per_month for each month early, the bonus capped at objective_bonus_cap. Late costs nothing.</summary>
        public static int Points(MvpRules r, DateTime done, DateTime reference)
        {
            // The loader requires the three keys in the GDD loop. In long, so a large gap cannot overflow before the cap.
            var bonus = Math.Min((long)r.ObjectiveBonusCap!.Value, (long)r.ObjectiveBonusPerMonth!.Value * MonthsEarly(done, reference));
            return r.ObjectivePoints!.Value + (int)bonus;
        }

        /// <summary>The Objectives screen's reading (#1189): one row per objective, in objectives.yaml's order (the loader),
        /// done or not, with the player's completion date when it is done and the historical reference date. No score: the
        /// owner's ruling for the screen (20 October 2026). A pure reading: it writes nothing, so the hash stays put.</summary>
        public static IReadOnlyList<ObjectiveStatus> Status(Content c, GameState s) =>
            c.Objectives.Select(o => new ObjectiveStatus(o.Id, o.Name, s.Objectives.TryGetValue(o.Id, out var done) ? done : (DateTime?)null,
                o.ReferenceDate)).ToList();

        /// <summary>Step 11 of the GDD turn (spec 2026-10-12 §2.3, D-A6): records, in objectives.yaml's order, every objective whose condition
        /// holds, completed on <paramref name="next"/>, the turn's end date (R-OBJ-001). <paramref name="r"/> is the rules of
        /// the date the turn starts from (docs/determinism.md, "Rules per era").</summary>
        public static void Complete(Content c, GameState s, MvpRules r, DateTime next, List<Cause> why)
        {
            foreach (var o in c.Objectives)   // in objectives.yaml's order (the loader; R-OBJ-007)
                if (Holds(c, s, o)) Record(c, s, r, o.Id, next, why);
        }

        /// <summary>Whether the objective's condition holds: the immigrants are absorbed (R-OBJ-004), its site is built
        /// (R-SITE-003), or every segment/site named by an infrastructure part is built (R-OBJ-005).</summary>
        static bool Holds(Content c, GameState s, Objective o) =>
            o.CompletesWhen!.Absorbed is AbsorbCondition a ? Absorbed(s, a)
            : o.CompletesWhen.CorridorComplete != "" ? Corridors.IsComplete(c, s, o.CompletesWhen.CorridorComplete)
            : o.CompletesWhen.SitesBuilt.Count > 0 ? o.CompletesWhen.SitesBuilt.All(id => s.Sites.TryGetValue(id, out var site) && site.BuiltOn != null)
            : s.Sites.TryGetValue(o.CompletesWhen.SiteBuilt, out var w) && w.BuiltOn != null;

        /// <summary>R-OBJ-004 (D-B4; owner, 18 October 2026, #738, option (a)): once the Jewish population (the national count,
        /// as it stands when step 11 runs, before the turn's population step) has reached the gate, the cities' overflow is at
        /// most its share of the people they house and their employed at least its share of their civilians. Refugees waiting
        /// do not count: the turn's arrivals (step 9) always wait at step 11, after housing (step 8). The city counts are
        /// integer sums times a decimal share, so city order and the runtime cannot change them.</summary>
        static bool Absorbed(GameState s, AbsorbCondition a)
        {
            if (s.TotalPopulation < a.PopulationAtLeast) return false;
            long housed = 0, over = 0, civilians = 0, employed = 0;
            foreach (var x in s.Cities.Values)
            {
                housed += Cities.Housed(s, x);
                over += Cities.Overflow(s, x);
                civilians += x.Civilians;
                employed += x.Employed;
            }
            return over <= a.OverflowAtMost!.Value * housed && employed >= a.EmployedAtLeast * civilians;
        }

        /// <summary>Records objective <paramref name="id"/> as completed on <paramref name="date"/> and adds its points to the
        /// score (R-OBJ-001, R-OBJ-002). Returns false, and changes nothing, when it is already recorded or the loop is not
        /// gdd. <paramref name="r"/> is the rules of the date the turn starts from, not of <paramref name="date"/>, so a turn
        /// that crosses an era boundary scores with the era it started in.</summary>
        public static bool Record(Content c, GameState s, MvpRules r, string id, DateTime date, List<Cause> why)
        {
            if (!c.IsGdd || s.Objectives.ContainsKey(id)) return false;
            var o = c.Objectives.FirstOrDefault(x => x.Id == id) ?? throw new ArgumentException($"unknown objective '{id}'", nameof(id));
            var bonus = (int)Math.Min((long)r.ObjectiveBonusCap!.Value,
                (long)r.ObjectiveBonusPerMonth!.Value * MonthsEarly(date, o.ReferenceDate));
            // R-OBJ-006: standalone objectives receive their base as before. A parent objective's base is paid with the
            // last part, after every other part in the same group has already been recorded. The current part is included
            // explicitly because it is written below.
            var basePoints = (o.PartOf == "" || c.Objectives.Where(x => x.PartOf == o.PartOf)
                .All(x => x.Id == o.Id || s.Objectives.ContainsKey(x.Id))) ? r.ObjectivePoints!.Value : 0;
            var points = basePoints + bonus;
            s.Objectives[id] = date;
            s.Score += points;
            s.ScoreObjectivePoints += basePoints;   // R-SCORE-001: the parts as scored, not rebuilt later
            s.ScoreTimingBonuses += bonus;
            // In the Why list's "events" group: the card that follows is an event (Unity's WhyPresenter groups by system).
            why.Add(new Cause("events", id, points, string.Format(CultureInfo.InvariantCulture, "{0}: completed, {1} points", o.Name, points)));
            return true;
        }
    }

    /// <summary>One row of <see cref="Objectives.Status"/>: the objective's id and name, the date the player completed it (null
    /// until then) and the date of the real event.</summary>
    public sealed class ObjectiveStatus
    {
        public string Id { get; }
        public string Name { get; }
        public DateTime? Completed { get; }
        public DateTime Historical { get; }
        public bool Done => Completed != null;

        public ObjectiveStatus(string id, string name, DateTime? completed, DateTime historical)
        {
            Id = id; Name = name; Completed = completed; Historical = historical;
        }
    }
}
