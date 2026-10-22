using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 corridors (docs/GDD.md "Pioneer work": build road, build railroad; docs/gdd/corridors.md; Phase 4
    /// PR 2, #786): a halutz unit builds one segment of a corridor (corridors.yaml) over several turns, one project per unit
    /// (D-D6). The owner took the plan's recommendations on 17 October 2026 (#782): any unbuilt segment in any order, one unit
    /// per segment (Q-COR-4); the unit works from where it stands (Q-COR-5); the cost is paid in full at the start and
    /// borrowed if short, and stopping loses the progress and returns no Money (Q-COR-6); a built corridor needs no upkeep and
    /// yields nothing (Q-COR-7). A segment is named on screen by its corridor's name and its number (Q-SENS-1). Runs only when
    /// eras.yaml systems.loop is gdd; Projects dispatches to it.</summary>
    public static class Corridors
    {
        public const string BuildRoad = "build_road";
        public const string BuildRail = "build_rail";
        /// <summary>The corridor kinds (corridors.yaml kind).</summary>
        public const string Road = "road";
        public const string Rail = "rail";

        /// <summary>Whether <paramref name="action"/> is a corridor action.</summary>
        public static bool IsAction(string action) => action == BuildRoad || action == BuildRail;

        /// <summary>A segment's on-screen name: the corridor's name and the segment's number, from 1 ("Burma Road, segment 2").</summary>
        public static string SegmentName(CorridorDef d, int index) => d.Name + ", segment " + (index + 1).ToString(CultureInfo.InvariantCulture);

        /// <summary>The segment <paramref name="unit"/> is building, as (corridor, 0-based index), or null (D-D6).</summary>
        public static (CorridorWork Work, int Index)? WorkOf(GameState s, int unit)
        {
            foreach (var w in s.Corridors.Values)
                for (int i = 0; i < w.Segments.Count; i++)
                    if (w.Segments[i] is SegmentWork x && x.BuiltOn == null && x.Unit == unit) return (w, i);
            return null;
        }

        /// <summary>R-COR-001: an idle halutz unit (Projects.Start has checked the unit) starts an unbuilt, unclaimed segment
        /// <paramref name="target"/> ("corridor:N", N from 1) of a corridor whose kind <paramref name="action"/> builds. The
        /// Treasury pays the kind's cost now and borrows what it lacks. Refused, state untouched, for an unknown corridor or
        /// segment, a kind the action does not build, or a segment being built or built.</summary>
        internal static string? Start(Content c, GameState s, int unit, string action, string target, List<Cause> why)
        {
            var colon = target.LastIndexOf(':');
            var def = colon < 0 ? null : c.Corridors.FirstOrDefault(x => x.Id == target.Substring(0, colon));
            if (def == null || !int.TryParse(target.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
                || n < 1 || n > def.Segments.Count)
                return "There is no such corridor segment";
            var wantRail = action == BuildRail;
            if ((def.Kind == Rail) != wantRail) return wantRail ? $"{def.Name} is not a railway" : $"{def.Name} is not a road";
            var index = n - 1;
            var name = SegmentName(def, index);
            s.Corridors.TryGetValue(def.Id, out var work);
            if (work?.Segments[index] is SegmentWork seg)
                return seg.BuiltOn == null ? $"{name} is already being built" : $"{name} is already built";
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
            var (cost, _) = Projects.Terms(r, action);
            EconomySystem.PayOrBorrow(c, s, cost, "to build a corridor segment", why);
            // As for sites (R-SITE-001): the turn of the order is the first build turn. The segment starts at 0, step 5 of this
            // turn's resolve gives it its first turn of progress, and with 6 turns an order on turn T is built at the end of T+5.
            var started = new SegmentWork(unit, 0, null);
            s.Corridors[def.Id] = work == null
                ? new CorridorWork(def.Id, Enumerable.Range(0, def.Segments.Count).Select(i => i == index ? started : null).ToArray())
                : work.With(index, started)!;
            why.Add(new Cause("map", def.Id, 1, $"{name}: a group of halutzim began building, cost {PlayerText.Money(c, "treasury", cost)}"));
            return null;
        }

        /// <summary>R-COR-003 with R-SITE-002's rule (D-A3): the unit's segment loses its record, so it is unbuilt and free
        /// again, and the unit is idle. The progress and the Money spent are lost. Returns false when the unit builds no
        /// segment.</summary>
        internal static bool Stop(Content c, GameState s, int unit, List<Cause> why)
        {
            if (!(WorkOf(s, unit) is var (work, index))) return false;
            var rest = work.With(index, null);
            if (rest == null) s.Corridors.Remove(work.Id); else s.Corridors[work.Id] = rest;
            var def = c.Corridors.First(x => x.Id == work.Id);
            why.Add(new Cause("map", work.Id, -1, $"{SegmentName(def, index)}: the halutzim stopped building; the Money spent is not returned"));
            return true;
        }

        /// <summary>R-COR-002, step 5 of the turn after the sites: in ordinal corridor id order, then segment index, each segment
        /// being built gains one turn of progress, or veteran_progress with a veteran (R-VET-002), scaled by Transportation
        /// (R-MIN-002), with the fraction carried as on a site. One whose progress reaches its
        /// kind's turns is built on <paramref name="next"/>, the turn's end date, and its unit is idle again. Then the unit, a
        /// halutz, gains a turn of service, and at veteran_halutz_service becomes a veteran_halutz (R-VET-001), as on a site.</summary>
        public static void Advance(Content c, GameState s, DateTime next, List<Cause> why)
        {
            var r = c.RulesAt(s.Date).Mvp!;
            foreach (var w in s.Corridors.Values.ToList())
            {
                var def = c.Corridors.First(x => x.Id == w.Id);
                var (_, turns) = Projects.Terms(r, def.Kind == Rail ? BuildRail : BuildRoad);
                var cur = w;
                for (int i = 0; i < cur.Segments.Count; i++)
                {
                    if (!(cur.Segments[i] is SegmentWork x) || x.BuiltOn != null) continue;
                    var u = s.Units[x.Unit!.Value];   // a segment being built always names its unit (SegmentWork)
                    // Capped at the kind's turns, so a built segment's progress reads the same whoever built it.
                    var acc = x.Carry + (u.Type == Projects.VeteranType ? r.VeteranProgress!.Value : 1)
                        * EffectsSystem.GddSystemMultiplier(c, s, r, "corridors");
                    var whole = (int)Math.Floor(acc);
                    acc -= whole;
                    var progress = Math.Min(turns, x.Progress + whole);
                    if (progress < turns) cur = cur.With(i, new SegmentWork(x.Unit, progress, null, acc))!;
                    else
                    {
                        cur = cur.With(i, new SegmentWork(null, progress, next))!;
                        why.Add(new Cause("map", w.Id, 1, $"{SegmentName(def, i)} built"));
                    }
                    // R-VET-001: the segment progressed this turn, so a halutz serves a turn; a veteran counts no further.
                    if (u.Type != "halutz") continue;
                    var service = u.Service + 1;
                    var promoted = service >= r.VeteranHalutzService!.Value;
                    s.Units[u.Id] = new Unit(u.Id, promoted ? Projects.VeteranType : u.Type, u.Q, u.R, service, u.Side, u.Strength, u.Order, u.OrderTarget);
                    if (promoted) why.Add(new Cause("map", w.Id, 1, "A group of halutzim became veteran pioneers"));
                }
                s.Corridors[w.Id] = cur;
            }
        }

        /// <summary>The turns, this one included, in which <paramref name="unit"/> completes a segment's <paramref name="left"/>
        /// whole turns and <paramref name="carry"/> as <see cref="Advance"/> counts them (Projects.TurnsToBuild at the corridors
        /// multiplier). Changes no state; a new order passes the kind's turns (Projects.Terms) and zero carry (#792).</summary>
        public static int TurnsToBuild(Content c, GameState s, int unit, int left, double carry) =>
            Projects.TurnsToBuild(c, s, unit, "corridors", left, carry);

        /// <summary>R-COR-003: a corridor is complete when every one of its segments is built, in any order.</summary>
        public static bool IsComplete(Content c, GameState s, string corridor)
        {
            var def = c.Corridors.First(x => x.Id == corridor);
            return s.Corridors.TryGetValue(corridor, out var w) && w.Segments.Count == def.Segments.Count
                && w.Segments.All(x => x?.BuiltOn != null);
        }
    }
}
