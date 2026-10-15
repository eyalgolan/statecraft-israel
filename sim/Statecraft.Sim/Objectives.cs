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

        /// <summary>Step 11 of the GDD turn (spec 2026-10-12 §2.3, D-A6): records, in id order, every objective whose condition
        /// holds, completed on <paramref name="next"/>, the turn's end date (R-OBJ-001). <paramref name="r"/> is the rules of
        /// the date the turn starts from (docs/determinism.md, "Rules per era").</summary>
        public static void Complete(Content c, GameState s, MvpRules r, DateTime next, List<Cause> why)
        {
            foreach (var o in c.Objectives)   // ordered by id (the loader)
                if (Holds(s, o)) Record(c, s, r, o.Id, next, why);
        }

        /// <summary>Whether the objective's condition holds: its site is built (R-SITE-003).</summary>
        static bool Holds(GameState s, Objective o) =>
            s.Sites.TryGetValue(o.CompletesWhen!.SiteBuilt, out var w) && w.BuiltOn != null;

        /// <summary>Records objective <paramref name="id"/> as completed on <paramref name="date"/> and adds its points to the
        /// score (R-OBJ-001, R-OBJ-002). Returns false, and changes nothing, when it is already recorded or the loop is not
        /// gdd. <paramref name="r"/> is the rules of the date the turn starts from, not of <paramref name="date"/>, so a turn
        /// that crosses an era boundary scores with the era it started in.</summary>
        public static bool Record(Content c, GameState s, MvpRules r, string id, DateTime date, List<Cause> why)
        {
            if (!c.IsGdd || s.Objectives.ContainsKey(id)) return false;
            var o = c.Objectives.FirstOrDefault(x => x.Id == id) ?? throw new ArgumentException($"unknown objective '{id}'", nameof(id));
            var points = Points(r, date, o.ReferenceDate);
            s.Objectives[id] = date;
            s.Score += points;
            // In the Why list's "events" group: the card that follows is an event (Unity's WhyPresenter groups by system).
            why.Add(new Cause("events", id, points, string.Format(CultureInfo.InvariantCulture, "{0}: completed, {1} points", o.Name, points)));
            return true;
        }
    }
}
