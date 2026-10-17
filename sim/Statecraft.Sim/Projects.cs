using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 pioneer work (docs/GDD.md "Pioneer work"; Phase 1 PR 5): a halutz unit builds a yishuv at a
    /// designated site (sites.yaml) over several turns (D-A3), one project per unit (D-D6). Runs only when eras.yaml
    /// systems.loop is gdd. The answers to #378's open questions are the plan's recommendations, which the owner took on
    /// 14 October 2026: the unit works from where it stands (9), stays a halutz unit when the yishuv is founded (10), the
    /// founding changes no city (11), the cost is paid in full at the start (12), a short Treasury borrows (13, the owner's decision of 15 October 2026), stopping loses
    /// the progress (14), and one unit builds a site (15). Its causes go to the "map" group of the Why screen ("Building").</summary>
    public static class Projects
    {
        public const string PioneerKind = "pioneer_work";
        public const string StopKind = "stop_work";
        /// <summary>The one pioneer action Phase 1 has (GDD "Pioneer work": "Build yishuv").</summary>
        public const string BuildYishuv = "build_yishuv";
        /// <summary>The kind of site a yishuv may be built at (sites.yaml kinds).</summary>
        public const string YishuvKind = "yishuv";
        /// <summary>The unit type a halutz unit becomes after veteran_halutz_service turns of work (R-VET-001; impact plan §1.3's id, D-E3).</summary>
        public const string VeteranType = "veteran_halutz";

        /// <summary>Whether a unit of <paramref name="type"/> does pioneer work: a halutz, or a veteran one (R-SITE-001, R-VET-001).</summary>
        public static bool IsPioneer(string type) => type == "halutz" || type == VeteranType;

        /// <summary>Whether a site being built names <paramref name="unit"/>: the unit has a project (D-D6).</summary>
        public static bool IsBusy(GameState s, int unit) => s.Sites.Values.Any(w => w.BuiltOn == null && w.Unit == unit);

        /// <summary>R-SITE-001: an idle halutz unit starts <paramref name="action"/> (build_yishuv) at a yishuv site that has no
        /// record. The Treasury pays build_yishuv_cost now and borrows what it lacks; the site gets build_yishuv_turns to go; the unit
        /// does not move. Refused, state untouched, for an unknown unit, a unit that is not a halutz or is busy, an unknown
        /// action or site, a site that is not for a yishuv, or is being built or is built. Step
        /// has already refused it in the legacy loop.</summary>
        public static string? Start(Content c, GameState s, int unit, string action, string site, List<Cause> why)
        {
            if (!s.Units.TryGetValue(unit, out var u)) return "There is no such unit";
            if (!IsPioneer(u.Type)) return "Only halutzim do pioneer work";
            var busy = s.Sites.Values.FirstOrDefault(w => w.BuiltOn == null && w.Unit == unit);
            if (busy != null) return $"That group of halutzim is already building {PlayerText.Site(c, busy.Site)}";
            if (action != BuildYishuv) return "There is no such pioneer work";
            var place = c.Sites.FirstOrDefault(x => x.Id == site);
            if (place == null) return "There is no such site";
            if (!place.Kinds.Contains(YishuvKind)) return $"{place.Name} is not a site for a new yishuv";
            if (s.Sites.TryGetValue(site, out var work))
                return work.BuiltOn == null ? $"{place.Name} is already being built" : $"{place.Name} is already built";
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
            // Owner decision "Allow borrowing" (2026-10-15, #378 question 13): a short Treasury borrows, as map actions do.
            EconomySystem.PayOrBorrow(c, s, r.BuildYishuvCost, "to build a yishuv", why);
            // The same reading of D-A6 as training's: the turn of the order is the first build turn, so with
            // build_yishuv_turns 6 an order on turn T is built at the end of T+5
            // (ProjectsTests.AnOrderOnTurnTWithSixTurnsIsBuiltAtTheEndOfTurnTPlusFive).
            s.Sites[site] = new SiteWork(site, unit, r.BuildYishuvTurns, null);
            why.Add(new Cause("map", site, 1, $"{place.Name}: a group of halutzim began building a yishuv, cost {PlayerText.Money(c, "treasury", r.BuildYishuvCost)}"));
            return null;
        }

        /// <summary>R-SITE-002 (D-A3): the unit's site loses its record, so it is available again and the unit is idle. The
        /// progress and the Money spent are lost. Refused, state untouched, for an unknown unit or one with no work. Step has
        /// already refused it in the legacy loop.</summary>
        public static string? Stop(Content c, GameState s, int unit, List<Cause> why)
        {
            if (!s.Units.ContainsKey(unit)) return "There is no such unit";
            var work = s.Sites.Values.FirstOrDefault(w => w.BuiltOn == null && w.Unit == unit);
            if (work == null) return "That group of halutzim is not building anything";
            s.Sites.Remove(work.Site);
            why.Add(new Cause("map", work.Site, -1, $"{PlayerText.Site(c, work.Site)}: the halutzim stopped building; the Money spent is not returned"));
            return null;
        }

        /// <summary>R-SITE-003, step 5 of the turn (spec 2026-10-12 section 2.3, D-A6): in ordinal site id order, each site being
        /// built has one turn less to go, or veteran_progress less with a veteran (R-VET-002). One that reaches 0 is built on
        /// <paramref name="next"/>, the turn's end date, and its unit is idle again. No city is founded (Phase 1). Then the
        /// unit, a halutz, gains a turn of service, and at veteran_halutz_service becomes a veteran_halutz (R-VET-001).</summary>
        public static void Advance(Content c, GameState s, DateTime next, List<Cause> why)
        {
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop, the only loop TurnResolver runs this in
            foreach (var w in s.Sites.Values.Where(x => x.BuiltOn == null).ToList())
            {
                var u = s.Units[w.Unit!.Value];   // a site being built always names its unit (SiteWork)
                var progress = u.Type == VeteranType ? r.VeteranProgress!.Value : 1;
                if (w.TurnsLeft > progress) s.Sites[w.Site] = w.WithTurnsLeft(w.TurnsLeft - progress);
                else
                {
                    s.Sites[w.Site] = SiteWork.Built(w.Site, next);
                    why.Add(new Cause("map", w.Site, 1, $"{PlayerText.Site(c, w.Site)} founded"));
                }
                // R-VET-001: the project progressed this turn, so a halutz serves a turn; a veteran counts no further.
                if (u.Type != "halutz") continue;
                var service = u.Service + 1;
                var promoted = service >= r.VeteranHalutzService!.Value;
                s.Units[u.Id] = new Unit(u.Id, promoted ? VeteranType : u.Type, u.Q, u.R, service);
                if (promoted) why.Add(new Cause("map", w.Site, 1, "A group of halutzim became veteran pioneers"));
            }
        }
    }
}
