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
        /// <summary>The command kinds of the city projects (R-PROJ-001, R-PROJ-002).</summary>
        public const string StartProjectKind = "start_project";
        public const string CancelProjectKind = "cancel_project";
        /// <summary>The one pioneer action Phase 1 has (GDD "Pioneer work": "Build yishuv").</summary>
        public const string BuildYishuv = "build_yishuv";
        /// <summary>The pioneer action that builds a military camp or a Hakhshara, as the site's kind says (GDD "Pioneer work":
        /// "Build training camp"; Q-CAMP-3; Phase 4 PR 3).</summary>
        public const string BuildCamp = "build_camp";
        /// <summary>The kind of site a yishuv may be built at (sites.yaml kinds).</summary>
        public const string YishuvKind = "yishuv";
        /// <summary>The unit type a halutz unit becomes after veteran_halutz_service turns of work (R-VET-001; impact plan §1.3's id, D-E3).</summary>
        public const string VeteranType = "veteran_halutz";

        /// <summary>Whether a unit of <paramref name="type"/> does pioneer work: a halutz, or a veteran one (R-SITE-001, R-VET-001).</summary>
        public static bool IsPioneer(string type) => type == "halutz" || type == VeteranType;
        /// <summary>The pioneer action that plows a field (GDD "Pioneer work": "Prepare agricultural land"; Phase 4 PR 4).</summary>
        public const string PlowFields = "plow_fields";
        /// <summary>The kind of site a field may be plowed at (sites.yaml kinds). Built, a field site is plowed (R-FIELD-001).</summary>
        public const string FieldKind = "field";
        /// <summary>The pioneer action that drains a swamp (GDD "Pioneer work": "Dry swamp"; Phase 4 PR 5).</summary>
        public const string DrySwamp = "dry_swamp";
        /// <summary>The kind of site a swamp may be drained at (sites.yaml kinds). Drained, its hex's field site is ready (R-SWAMP-001).</summary>
        public const string SwampKind = "swamp";

        /// <summary>Whether a yishuv may be built at <paramref name="site"/>: its kinds name one (R-SITE-001, R-SITE-004). A hex may
        /// hold more than one site, as a yishuv hex holds a field one; the panel finds the yishuv by this, not by file order.</summary>
        public static bool IsYishuvSite(Site site) => site.Kinds.Contains(YishuvKind);

        /// <summary>The turns of progress <paramref name="unit"/>'s project gains in one turn: veteran_progress for a veteran,
        /// else 1 (R-SITE-003, R-VET-002).</summary>
        public static int Progress(MvpRules r, Unit unit) => unit.Type == VeteranType ? r.VeteranProgress!.Value : 1;

        /// <summary>The turns, this one included, in which <paramref name="unit"/> builds a site with <paramref name="left"/>
        /// turns of progress to go: Advance's count, a halutz promoted on the way (R-VET-001) building faster from the next turn
        /// (R-SITE-003, R-VET-002). With build_yishuv_turns it is how long a Build ordered now takes. Read with the rules of
        /// the current date, as Advance reads them; it changes no state.</summary>
        public static int TurnsToBuild(Content c, GameState s, int unit, int left)
        {
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
            var u = s.Units[unit];
            for (int turns = 1; ; turns++)
            {
                var progress = Progress(r, u);
                if (left <= progress) return turns;
                left -= progress;
                if (u.Type == "halutz")
                {
                    var service = u.Service + 1;
                    u = new Unit(u.Id, service >= r.VeteranHalutzService!.Value ? VeteranType : u.Type, u.Q, u.R, service, u.Side, u.Strength, u.Order, u.OrderTarget);
                }
            }
        }

        /// <summary>Whether a site being built names <paramref name="unit"/>: the unit has a project (D-D6).</summary>
        public static bool IsBusy(GameState s, int unit) => s.Sites.Values.Any(w => w.BuiltOn == null && w.Unit == unit);

        /// <summary>R-SITE-001: an idle halutz unit starts <paramref name="action"/> (build_yishuv) at a yishuv site that has no
        /// record. The Treasury pays build_yishuv_cost now and borrows what it lacks; the site gets build_yishuv_turns to go; the unit
        /// does not move. Refused, state untouched, for an unknown unit, a unit that is not a halutz or is busy, an unknown
        /// action or site, a site that is not for a yishuv, or is being built or is built. Step
        /// has already refused it in the legacy loop. R-CAMP-001: build_camp at a military_camp or hakhshara site follows the same
        /// rules, with build_camp_turns and build_camp_cost. R-FIELD-001: plow_fields at a field site follows the same rules, with
        /// plow_fields_turns and plow_fields_cost, and is refused while a yishuv site in the field's hex is not built. R-SWAMP-001:
        /// dry_swamp at a swamp site follows the same rules, with dry_swamp_turns and dry_swamp_cost; a field site in the swamp's
        /// hex is refused until the swamp is drained.</summary>
        public static string? Start(Content c, GameState s, int unit, string action, string site, List<Cause> why)
        {
            if (!s.Units.TryGetValue(unit, out var u)) return "There is no such unit";
            if (!IsPioneer(u.Type)) return "Only halutzim do pioneer work";
            var busy = s.Sites.Values.FirstOrDefault(w => w.BuiltOn == null && w.Unit == unit);
            if (busy != null) return $"That group of halutzim is already building {PlayerText.Site(c, busy.Site)}";
            if (action != BuildYishuv && action != BuildCamp && action != PlowFields && action != DrySwamp) return "There is no such pioneer work";
            var place = c.Sites.FirstOrDefault(x => x.Id == site);
            if (place == null) return "There is no such site";
            var camp = action == BuildCamp;
            var plow = action == PlowFields;
            var drain = action == DrySwamp;
            if (camp ? !IsCamp(place) : plow ? !place.Kinds.Contains(FieldKind) : drain ? !place.Kinds.Contains(SwampKind) : !IsYishuvSite(place))
                return camp ? $"{place.Name} is not a site for a training camp"
                    : plow ? $"{place.Name} is not a site for fields" : drain ? $"{place.Name} is not a swamp" : $"{place.Name} is not a site for a new yishuv";
            if (s.Sites.TryGetValue(site, out var work))
                return work.BuiltOn == null ? $"{place.Name} is already being built" : $"{place.Name} is already built";
            if (plow && YishuvToBuildFirst(c, s, place) is Site first)
                return $"{place.Name} can be plowed once {first.Name} is {(first.Kinds.Contains(SwampKind) ? "drained" : "built")}";
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
            // Owner decision "Allow borrowing" (2026-10-15, #378 question 13): a short Treasury borrows, as map actions do.
            var cost = camp ? r.BuildCampCost!.Value : plow ? r.PlowFieldsCost!.Value : drain ? r.DrySwampCost!.Value : r.BuildYishuvCost;   // the loader requires them in the GDD loop
            EconomySystem.PayOrBorrow(c, s, cost, camp ? "to build a training camp" : plow ? "to plow fields" : drain ? "to drain a swamp" : "to build a yishuv", why);
            // The same reading of D-A6 as training's: the turn of the order is the first build turn, so with
            // build_yishuv_turns 6 an order on turn T is built at the end of T+5
            // (ProjectsTests.AnOrderOnTurnTWithSixTurnsIsBuiltAtTheEndOfTurnTPlusFive).
            s.Sites[site] = new SiteWork(site, unit, camp ? r.BuildCampTurns!.Value : plow ? r.PlowFieldsTurns : drain ? r.DrySwampTurns!.Value : r.BuildYishuvTurns, null);
            why.Add(plow || drain
                ? new Cause("map", site, 1, $"{place.Name}: a group of halutzim began {(plow ? "plowing" : "draining the swamp")}, cost {PlayerText.Money(c, "treasury", cost)}")
                : new Cause("map", site, 1, $"{place.Name}: a group of halutzim began building {(camp ? "a training camp" : "a yishuv")}, cost {PlayerText.Money(c, "treasury", cost)}"));
            return null;
        }

        /// <summary>Whether a training camp may be built at <paramref name="site"/>: its kinds include military_camp or hakhshara
        /// (R-CAMP-001). Built, its city gains those facilities (Training.HasFacility).</summary>
        public static bool IsCamp(Site site) => site.Kinds.Contains("military_camp") || site.Kinds.Contains("hakhshara");

        /// <summary>R-FIELD-001 (Q-FIELD-1): the yishuv site in <paramref name="field"/>'s hex while it is not built yet, so the
        /// field cannot be plowed; null when the field is ready (its yishuv is built, or it has none in its hex). R-SWAMP-001
        /// (Q-SWAMP-1): a swamp site in the hex counts the same way, so its field is ready only once it is drained.</summary>
        public static Site? YishuvToBuildFirst(Content c, GameState s, Site field) =>
            c.Sites.FirstOrDefault(y => (y.Kinds.Contains(YishuvKind) || y.Kinds.Contains(SwampKind)) && y.Q == field.Q && y.R == field.R
                && !(s.Sites.TryGetValue(y.Id, out var w) && w.BuiltOn != null));

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
        /// unit, a halutz, gains a turn of service, and at veteran_halutz_service becomes a veteran_halutz (R-VET-001). A built
        /// camp site gives its city the facility from the next turn (R-CAMP-001, Training.HasFacility). A built field site is
        /// plowed (R-FIELD-001), and yields from this turn's step 6 on (R-FIELD-002). A built swamp site is drained, and its
        /// hex's field site is ready (R-SWAMP-001).</summary>
        public static void Advance(Content c, GameState s, DateTime next, List<Cause> why)
        {
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop, the only loop TurnResolver runs this in
            foreach (var w in s.Sites.Values.Where(x => x.BuiltOn == null).ToList())
            {
                var u = s.Units[w.Unit!.Value];   // a site being built always names its unit (SiteWork)
                var progress = Progress(r, u);
                if (w.TurnsLeft > progress) s.Sites[w.Site] = w.WithTurnsLeft(w.TurnsLeft - progress);
                else
                {
                    s.Sites[w.Site] = SiteWork.Built(w.Site, next);
                    var place = c.Sites.First(x => x.Id == w.Site);   // a site being built is always in sites.yaml (Start)
                    why.Add(new Cause("map", w.Site, 1, $"{place.Name} {(IsCamp(place) ? "built" : place.Kinds.Contains(FieldKind) ? "plowed" : place.Kinds.Contains(SwampKind) ? "drained" : "founded")}"));
                }
                // R-VET-001: the project progressed this turn, so a halutz serves a turn; a veteran counts no further.
                if (u.Type != "halutz") continue;
                var service = u.Service + 1;
                var promoted = service >= r.VeteranHalutzService!.Value;
                s.Units[u.Id] = new Unit(u.Id, promoted ? VeteranType : u.Type, u.Q, u.R, service, u.Side, u.Strength, u.Order, u.OrderTarget);
                if (promoted) why.Add(new Cause("map", w.Site, 1, "A group of halutzim became veteran pioneers"));
            }
        }

        /// <summary>The city project at hex (q, r), being built or built, or null (Q-PROJ-2: one project or placement a hex).</summary>
        public static CityProject? CityProjectAt(GameState s, int q, int r) => s.CityProjects.Values.FirstOrDefault(p => p.Q == q && p.R == r);

        /// <summary>R-PROJ-001: null when city project <paramref name="kind"/> may be started for <paramref name="city"/> on hex
        /// (q, r) now, else the reason. Refused for an unknown kind or city; a kind whose resource the city lacks (R-PROJ-005); a
        /// transit camp without the ma'abarot policy (Q-HOUSE-3, D-D3: the policy carries its sourced date); a hex that is not
        /// land, is further than map_action_max_distance from the city's settlement hex, or holds a placement or another city
        /// project (Q-PROJ-2); and once map_actions_per_turn map actions and city projects together were started this turn
        /// (Q-PROJ-4). A short Treasury is not a reason: it borrows (Q-MONEY-3). The reference player asks this before it orders.</summary>
        public static string? CheckCity(Content c, GameState s, string kind, string city, int q, int r)
        {
            var k = c.ProjectKinds.FirstOrDefault(x => x.Id == kind);
            if (k == null) return "There is no such city project";
            if (!s.Cities.ContainsKey(city)) return "There is no such city";
            if (kind == MapActions.TransitCamp && !(s.Policies.TryGetValue(MapActions.MaabarotPolicy, out var m) && m >= 0.5))
                return "Transit camps need the ma'abarot policy";
            // R-PROJ-005. A city founded in play is not in cities.yaml, so it has no resources.
            if (!string.IsNullOrEmpty(k.Requires) && c.Cities.FirstOrDefault(x => x.Id == city)?.Resources.Any(x => x.Id == k.Requires) != true)
                return $"{PlayerText.Settlement(c, city)} has no {k.Requires} for {k.Name}";
            var hex = c.HexAt(q, r);
            if (hex == null || !hex.Land) return "This hex is not land";
            var rules = c.RulesAt(s.Date);   // the era the turn starts in
            var home = c.SettlementHexes.First(h => h.Id == city);   // every city has one (ValidateGdd)
            var max = rules.MapActionMaxDistance;
            var name = PlayerText.Settlement(c, city);
            if (MapActions.Distance(q, r, home.Q, home.R) > max)
                return FormattableString.Invariant($"This hex is more than {max} {(max == 1 ? "hex" : "hexes")} from {name}");
            if (MapActions.At(s, q, r) is Placement placed) return $"This hex already has {PlayerText.MapAction(c, placed.Action)}";
            if (CityProjectAt(s, q, r) is CityProject other) return $"This hex already has {c.ProjectKinds.First(x => x.Id == other.Kind).Name}";
            // Last, as for map actions, so a player over the cap still sees the specific reason an order is illegal. The count
            // is the map actions' own (one national limit, Q-PROJ-4).
            if ((s.MapActionsTurnDate == s.Date ? s.MapActionsThisTurn : 0) >= rules.MapActionsPerTurn)
                return FormattableString.Invariant($"No more than {rules.MapActionsPerTurn} map actions and city projects in one turn");
            return null;
        }

        /// <summary>R-PROJ-001: starts city project <paramref name="kind"/> for <paramref name="city"/> on hex (q, r), or returns
        /// the reason (<see cref="CheckCity"/>; state untouched). The Treasury pays the kind's cost now and borrows what it lacks
        /// (Q-PROJ-1, Q-MONEY-3); the project takes the next id and the kind's turns, the turn of the order the first (D-A3). It
        /// needs no workers (Q-PROJ-3). Step has already refused it in the legacy loop.</summary>
        public static string? StartCity(Content c, GameState s, string kind, string city, int q, int r, List<Cause> why)
        {
            var reason = CheckCity(c, s, kind, city, q, r);
            if (reason != null) return reason;
            var k = c.ProjectKinds.First(x => x.Id == kind);
            if (s.MapActionsTurnDate != s.Date) { s.MapActionsTurnDate = s.Date; s.MapActionsThisTurn = 0; }
            s.MapActionsThisTurn++;
            var name = PlayerText.Settlement(c, city);
            EconomySystem.PayOrBorrow(c, s, k.Cost, "to build " + k.Name, why);
            var id = s.NextId++;
            s.CityProjects.Add(id, new CityProject(id, kind, city, q, r, k.Turns));
            why.Add(new Cause("city", city, 1, $"{name}: {k.Name} began, cost {PlayerText.Money(c, "treasury", k.Cost)}"));
            return null;
        }

        /// <summary>R-PROJ-002 (D-A3): city project <paramref name="project"/> leaves the state; the Money spent is not returned,
        /// and with no workers (Q-PROJ-3) there are no people to return. Refused, state untouched, for a project that does not
        /// exist or is built. Step has already refused it in the legacy loop.</summary>
        public static string? CancelCity(Content c, GameState s, int project, List<Cause> why)
        {
            if (!s.CityProjects.TryGetValue(project, out var p)) return "There is no such city project";
            var k = c.ProjectKinds.First(x => x.Id == p.Kind);
            if (p.BuiltOn != null) return $"{PlayerText.Capitalised(k.Name)} is already built";
            s.CityProjects.Remove(project);
            why.Add(new Cause("city", p.City, -1, $"{PlayerText.Settlement(c, p.City)}: {k.Name} was cancelled; the Money spent is not returned"));
            return null;
        }

        /// <summary>R-PROJ-003, step 5 of the turn (D-A6), after the pioneer work: in ascending id, each city project being built
        /// has one turn less to go. One that reaches 0 is built on <paramref name="next"/>, the turn's end date, and adds its
        /// kind's housing capacity, condition (kept from 0 to 1) and job slots to its city, once: a built project is never
        /// advanced again.</summary>
        public static void AdvanceCity(Content c, GameState s, DateTime next, List<Cause> why)
        {
            foreach (var p in s.CityProjects.Values.Where(x => x.BuiltOn == null).ToList())
            {
                if (p.TurnsLeft > 1) { s.CityProjects[p.Id] = p.WithTurnsLeft(p.TurnsLeft - 1); continue; }
                s.CityProjects[p.Id] = p.Built(next);
                var k = c.ProjectKinds.First(x => x.Id == p.Kind);
                var x = s.Cities[p.City];
                var condition = x.Condition is double now ? Math.Min(1.0, Math.Max(0.0, now + k.Condition)) : (double?)null;
                s.Cities[p.City] = new City(x.Id, x.HousingCapacity + k.Capacity, x.JobSlots + k.JobSlots, x.Civilians, x.Employed, x.Refugees, x.Recruits, condition, x.Craftsmen, x.JobsDefault);
                why.Add(new Cause("city", p.City, 1, $"{PlayerText.Settlement(c, p.City)}: {k.Name} was finished"));
            }
        }
    }
}
