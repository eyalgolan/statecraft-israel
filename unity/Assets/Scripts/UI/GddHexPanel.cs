#nullable enable
using System;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>GDD Phase 1 PR 8 (#381): the info panel's part for the GDD loop (Content.IsGdd), built into the
    /// <see cref="HostName"/> element on every render.
    /// <para>On a city hex (State.Cities), its counts (housing, overflow, jobs, craftsmen, refugees waiting, recruits) and training cohorts with
    /// their turns left, the Assign stepper (R-CITY-001), Train for each facility the city has (R-TRAIN-001, R-CAMP-001) and Cancel
    /// (R-TRAIN-002). On a site's hex (sites.yaml: a yishuv, camp, field or swamp site, #1124), its state, and Build with each idle pioneer
    /// group (Projects.IsPioneer: a halutz or a veteran one), with the rules' cost and the unit's own turns (Projects.TurnsToBuild;
    /// R-SITE-001, R-VET-002), or Stop while it is built (R-SITE-002). The same for each corridor segment through the hex: Build
    /// road or Build railway (R-COR-001 to R-COR-003, #792).</para>
    /// <para>On any hex a city can build on (Projects.CitiesFor, #1122): the city that builds (the player picks when several
    /// can), and Build for each content project kind with its cost and turns (Projects.CityTurnsToBuild), disabled with the sim's own refusal
    /// (Projects.CheckCityReason) worded in the UI's language; or the project there, with its turns left and Cancel, or the
    /// day it was finished (R-PROJ-001 to R-PROJ-003).</para>
    /// <para>Every number is the sim's, and every order goes through the bridge's queue: the sim rules on it when the turn
    /// runs. No rule lives here: the limits are the sim's public helpers (Cities.MaxWorkers, Training.Available), asked with
    /// the queue, and a button is disabled only where the sim would refuse that order, or another one in the queue, anyway.</para></summary>
    public static class GddHexPanel
    {
        public const string HostName = "info-gdd";
        /// <summary>The city-project part's host (#1122), in the hex's facts column: the GDD column beside it is already full on a city's
        /// own hex, and the facts column has room (InfoPanelLayoutTests).</summary>
        public const string ProjectsHostName = "info-projects";
        /// <summary>On the city's own hex, on a stacked bar or a short window, the project part folds behind this toggle
        /// ("City projects (N)") and the list it opens (owner ruling on #1150, 21 Oct 2026; Statecraft.uss .city-projects-list).</summary>
        public const string ToggleName = "city-projects-toggle", ListName = "city-projects-list", OpenClass = "info-projects-open";
        /// <summary>The name over each city's block on a hex that holds two (Statecraft.uss).</summary>
        public const string CityNameClass = "city-part-name";
        const string LineClass = "info-gdd-line", RowClass = "info-gdd-row", PairsClass = "info-gdd-pairs";

        public static void Hide(VisualElement host) { host.Clear(); host.style.display = DisplayStyle.None; }

        /// <summary>Builds the hex's GDD part into <paramref name="host"/> and its city projects into <paramref name="projects"/>
        /// (each hidden when there is none) and returns the designated site's name in the UI's language when the hex holds one,
        /// else null. <paramref name="townName"/> names a city in the UI's language (InfoPanelPresenter's town names).</summary>
        public static string? Render(VisualElement host, VisualElement projects, SimBridge bridge, UiText text, int q, int r, Func<string, string> townName)
        {
            host.Clear(); projects.Clear();
            var c = bridge.Content; var s = bridge.State;
            string? title = null;
            if (c.IsGdd)
            {
                // A hex with two cities (Rishon LeZion and Rehovot) shows two stat blocks: each gets its own part, named, so the
                // player can tell whose jobs stepper is whose (playtest B6). One city's block needs no name: the title has it.
                var cities = c.SettlementHexes.Where(x => x.Q == q && x.R == r && s.Cities.ContainsKey(x.Id)).ToList();
                foreach (var at in cities)
                {
                    var part = new VisualElement { name = "city-part-" + at.Id };
                    host.Add(part);
                    if (cities.Count > 1) Line(part, "city-name", townName(at.Id)).AddToClassList(CityNameClass);
                    City(part, bridge, text, at.Id);
                }
                var cityHere = host.childCount > 0;
                // A hex may hold two sites, a yishuv or the Hula swamp and the field it readies (R-FIELD-001, R-SWAMP-001): each
                // gets its own part, the one to finish first on top, and its name is the title (#1124).
                foreach (var site in c.Sites.Where(x => x.Q == q && x.R == r && Work(x) != null).OrderBy(x => x.Kinds.Contains(Projects.FieldKind)))
                {
                    title ??= Name(text, site);
                    var part = new VisualElement { name = "site-part-" + site.Id };
                    host.Add(part);
                    Site(part, bridge, text, site, Work(site)!.Value);
                }
                // #792: each corridor segment through the hex (corridors.yaml; a hex where two segments meet is in both). Not on a
                // city's own hex, whose column is full (InfoPanelLayoutTests): every segment has another hex.
                if (!cityHere)
                    foreach (var d in c.Corridors)
                        for (int i = 0; i < d.Segments.Count; i++)
                            if (d.Segments[i].Hexes.Any(h => h[0] == q && h[1] == r))
                            {
                                var part = new VisualElement { name = $"corridor-part-{d.Id}-{i + 1}" };
                                host.Add(part);
                                Segment(part, bridge, text, d, i);
                            }
                CityProjects(projects, bridge, text, q, r, townName, cityHere);
            }
            host.style.display = host.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            projects.style.display = projects.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            return title;
        }

        static void City(VisualElement host, SimBridge bridge, UiText text, string id)
        {
            var c = bridge.Content; var s = bridge.State; var city = s.Cities[id];
            // R-HOUSE-003: the housed are the civilians, the recruits and the groups in training (Cities.Housed), against the
            // capacity; R-HOUSE-001: the city's housing condition, 0 to 1, through the one percent formatter, on the same line
            // so the panel is no taller (the GDD loop always has a condition; the plain form is for a city without one).
            var housed = Cities.Housed(s, city); var capacity = Groups(text, city.HousingCapacity);
            Line(host, "city-housing", city.Condition is double condition
                ? text.Format("city.housing_condition", housed, capacity, PlayerText.Percent(condition))
                : text.Format("city.housing", housed, capacity));
            // #1121: the craftsmen (R-CRAFT-001) beside Jobs, as they fill ordinary jobs (R-CRAFT-002), and the overflow
            // (R-HOUSE-002, Cities.Overflow) beside the refugees waiting, the other people without housing. Each pair shares a
            // row (the labels keep their width and the row wraps only when they do not fit, Statecraft.uss), so on a short
            // window the panel stays clear of the left column (InfoPanelLayoutTests). Bare counts, like the Jobs line, to fit.
            var jobs = Row(host, "city-jobs-row"); jobs.AddToClassList(PairsClass);
            Line(jobs, "city-jobs", text.Format("city.jobs", city.Employed, city.JobSlots));
            Line(jobs, "city-craftsmen", text.Format("city.craftsmen", city.Craftsmen));
            var waiting = Row(host, "city-waiting-row"); waiting.AddToClassList(PairsClass);
            Line(waiting, "city-refugees", text.Format("city.refugees", Groups(text, city.Refugees)));
            Line(waiting, "city-overflow", text.Format("city.overflow", Cities.Overflow(s, city)));
            // #1345: trained recruits are formations housed in their home city since #1130, so the line counts every Israeli
            // formation the city houses (Cities.Soldiers, part of Cities.Housed), not City.Recruits, which nothing writes now.
            Line(host, "city-soldiers", text.Format("city.soldiers", Groups(text, Cities.Soldiers(s, city))));

            // The Assign stepper: the ordered count (or the current one), one group a press, within R-CITY-001's limits as
            // the sim gives them with this turn's Train orders; stepping back to the current count withdraws the order.
            var queued = bridge.QueuedLike(new AssignWorkers(id, "job", 0)) as AssignWorkers;
            var target = queued?.Count ?? city.Employed;
            var max = Cities.MaxWorkers(s, id, bridge.Queued);
            var workers = Row(host, "city-workers-row");
            Button(workers, "city-workers-less", "−", () => Workers(bridge, id, city.Employed, target - 1)).SetEnabled(target > 0);
            Line(workers, "city-workers", text.Format(queued == null ? "city.workers" : "city.workers_ordered", Groups(text, target)));
            Button(workers, "city-workers-more", "+", () => Workers(bridge, id, city.Employed, target + 1)).SetEnabled(target < max);
            // The Assign order's Attention price (the queue's, at this turn's start) and when it applies (playtest B7): the
            // Jobs line stays the sim's actual count until End turn. Changing a queued target does not buy another order.
            Line(host, "city-workers-cost", text.Format("city.workers_cost", bridge.Price(new AssignWorkers(id, "job", target))));

            // Train, for each facility the city has (Training.HasFacility: its own, D-D5, or a built camp site's, R-CAMP-001):
            // each press orders one more group; R-TRAIN-001 takes only civilians without work, and the sim counts every track's
            // order and the Assign order in the queue against them.
            var free = Training.Available(s, id, bridge.Queued);
            foreach (var type in c.UnitTypes.Where(t => t.Facility != "" && Training.HasFacility(c, s, id, t.Facility)))
            {
                var ordered = (bridge.QueuedLike(new Train(id, type.Id, 1)) as Train)?.Count ?? 0;
                var row = Row(host, $"city-train-{type.Id}-row");
                Button(row, "city-train-" + type.Id, text.Format("city.train_" + type.Id, PlayerText.Money(c, "treasury", type.TrainingCost), Turns(text, type.TrainingTurns)),
                    () => bridge.Queue(new Train(id, type.Id, ordered + 1))).SetEnabled(free >= 1);
                if (ordered > 0) Line(row, $"city-train-{type.Id}-ordered", text.Format("city.train_ordered", ordered));
            }

            foreach (var k in s.Cohorts.Values.Where(x => x.City == id))
            {
                var row = Row(host, $"city-cohort-{k.Id}-row");
                Line(row, $"city-cohort-{k.Id}", text.Format("city.cohort", text["city.track_" + k.Type], Groups(text, k.Count), Turns(text, k.TurnsLeft)));
                var cancelled = bridge.QueuedLike(new CancelTraining(k.Id)) != null;
                Button(row, $"city-cohort-{k.Id}-cancel", text[cancelled ? "city.cancel_ordered" : "city.cancel"],
                    () => bridge.Queue(new CancelTraining(k.Id))).SetEnabled(!cancelled);
            }

            // Return and Reassign (#1511, R-QUAL-002 to R-QUAL-005): one group a press, like Train, shown only where the city has
            // the people. The sim keeps the qualification and rules on each order; a button is off only where it would refuse it
            // (more than the craftsmen or qualified groups, or than the civilians without work, Training.Idle, with this
            // turn's orders counted by Training.Available).
            if (city.Craftsmen > 0)
            {
                var back = (bridge.QueuedLike(new ReturnCraftsmen(id, 1)) as ReturnCraftsmen)?.Count ?? 0;
                var row = Row(host, "city-return-craftsmen-row");
                Button(row, "city-return-craftsmen", text["city.return_craftsmen"], () => bridge.Queue(new ReturnCraftsmen(id, back + 1))).SetEnabled(back < city.Craftsmen);
                if (back > 0) Line(row, "city-return-craftsmen-ordered", text.Format("city.train_ordered", back));
            }
            // Training.Available already takes this turn's Train, Reinforce, Assign and Reassign orders off the idle civilians.
            var idleLeft = Training.Available(s, id, bridge.Queued);
            var q = city.Qualified;
            // A queued Train or Reinforce that leaves fewer civilians than the city has qualified costs it qualifications first
            // (R-QUAL-006, craftsmen first) and runs before the reassignment, so only what survives it can be reassigned.
            // Training.Ordered now includes the reassignments themselves, which leave the civilians where they are, so only Train and Reinforce count here.
            var trained = bridge.Queued.OfType<Train>().Where(x => x.City == id).Sum(x => x.Count) + bridge.Queued.OfType<Reinforce>().Where(x => x.City == id).Sum(x => x.Count);
            var kept = q.Lose(Math.Max(0, q.Total - (city.Civilians - trained)));
            foreach (var (role, have, left) in new[] { ("craftsmen", q.Craftsmen, kept.Craftsmen), ("halutz", q.Halutzim.Count, kept.Halutzim.Count), (Projects.VeteranType, q.Veterans, kept.Veterans) })
            {
                if (have == 0) continue;
                Command Order(int n) => role == "craftsmen" ? new ReassignCraftsmen(id, n) : (Command)new ReassignPioneers(id, role, n);
                var again = bridge.QueuedLike(Order(1));
                var ordered = again is ReassignCraftsmen rc ? rc.Count : again is ReassignPioneers rp ? rp.Count : 0;
                var row = Row(host, $"city-reassign-{role}-row");
                Button(row, "city-reassign-" + role, text.Format("city.reassign_" + role, have), () => bridge.Queue(Order(ordered + 1)))
                    .SetEnabled(ordered < left && idleLeft >= 1);
                if (ordered > 0) Line(row, $"city-reassign-{role}-ordered", text.Format("city.train_ordered", ordered));
            }
            var hex = c.SettlementHexes.First(h => h.Id == id);   // every settlement has one
            foreach (var u in s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && u.Q == hex.Q && u.R == hex.R))
                PioneerReturn(host, bridge, text, u.Id);
        }

        /// <summary>A pioneer group's Return (R-QUAL-003; #1511): the sim's unit order return_civilian, offered where
        /// Qualification.CheckReturn takes the group (not one at work). Pioneer groups have no map token and so no unit card:
        /// this sits beside the places that list them. A unit order, it takes effect at once (R-TURN-004): the group leaves
        /// the lists, and its Build buttons with it.</summary>
        static void PioneerReturn(VisualElement host, SimBridge bridge, UiText text, int unit)
        {
            if (Qualification.CheckReturn(bridge.State, unit) != null) return;
            // A Build or Stop queued for the group waits for End turn, which the group would no longer be there for.
            var working = bridge.Queued.Any(x => (x is PioneerWork w && w.Unit == unit) || (x is StopWork sw && sw.Unit == unit));
            Button(host, $"pioneer-return-{unit}", text.Format("pioneer.return", Group(text, bridge.State, unit)),
                () => bridge.Queue(new UnitOrder(unit, Qualification.ReturnOrder))).SetEnabled(!working);
        }

        static void Workers(SimBridge bridge, string city, int current, int count)
        {
            if (count != current) { bridge.Queue(new AssignWorkers(city, "job", count)); return; }
            if (bridge.QueuedLike(new AssignWorkers(city, "job", 0)) is Command order) bridge.Withdraw(order);
        }

        /// <summary>The pioneer action a site is for, as Projects.Start matches them (R-SITE-001, R-CAMP-001, R-FIELD-001,
        /// R-SWAMP-001), and the suffix of its strings; null for a site no pioneer work is for. A site of two kinds would get
        /// the first here; sites.yaml has none.</summary>
        static (string Action, string Key)? Work(Statecraft.Sim.Site site) =>
            Projects.IsYishuvSite(site) ? (Projects.BuildYishuv, "")
            : Projects.IsCamp(site) ? (Projects.BuildCamp, "_camp")
            : site.Kinds.Contains(Projects.FieldKind) ? (Projects.PlowFields, "_field")
            : site.Kinds.Contains(Projects.SwampKind) ? (Projects.DrySwamp, "_swamp")
            : ((string, string)?)null;

        static string Name(UiText text, Statecraft.Sim.Site site) =>
            text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrWhiteSpace(site.NameHe) ? site.NameHe : site.Name;

        static void Site(VisualElement host, SimBridge bridge, UiText text, Statecraft.Sim.Site site, (string Action, string Key) kind)
        {
            var c = bridge.Content; var s = bridge.State;
            var (action, key) = kind;
            s.Sites.TryGetValue(site.Id, out var work);
            if (work == null)
            {
                // The sim refuses to plow while the hex's yishuv or swamp is unfinished: the same helper Projects.Start asks.
                if (action == Projects.PlowFields && Projects.YishuvToBuildFirst(c, s, site) is Statecraft.Sim.Site first)
                {
                    Line(host, "site-state", text.Format(first.Kinds.Contains(Projects.SwampKind) ? "site.waits_swamp" : "site.waits_yishuv", Name(text, first)));
                    return;
                }
                Line(host, "site-state", text["site.available" + key]);
                var terms = Projects.Terms(c.RulesAt(s.Date).Mvp!, action);   // the loader requires the rules in the GDD loop
                var idle = s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && !Projects.IsBusy(s, u.Id)).ToList();
                if (idle.Count == 0) Line(host, "site-no-halutz", text["site.no_halutz"]);
                // One unit builds a site (question 15): once a group is ordered here, the others' Build is off, as the sim
                // would refuse the second order ("already being built").
                var taken = bridge.Queued.OfType<PioneerWork>().Any(w => w.Site == site.Id);
                foreach (var u in idle)
                {
                    // PioneerWork and StopWork share the unit's key: an order for this unit at this site is this button's.
                    var ordered = bridge.QueuedLike(new PioneerWork(u.Id, action, site.Id)) is PioneerWork p && p.Site == site.Id;
                    Button(host, $"site-build-{u.Id}", ordered ? text["site.ordered"]
                            : text.Format("site.build" + key, Group(text, s, u.Id), PlayerText.Money(c, "treasury", terms.Cost),
                                Turns(text, Projects.TurnsToBuild(c, s, u.Id, site, terms.Turns, 0))),
                        () => bridge.Queue(new PioneerWork(u.Id, action, site.Id))).SetEnabled(!ordered && !taken);
                    PioneerReturn(host, bridge, text, u.Id);
                }
            }
            else if (work.BuiltOn == null)
            {
                var unit = work.Unit!.Value;   // a site being built names its unit (SiteWork)
                Line(host, "site-state", text.Format("site.building" + key, Group(text, s, unit), Turns(text, Projects.TurnsToBuild(c, s, unit, site, work.TurnsLeft, work.Progress))));
                var stopping = bridge.QueuedLike(new StopWork(unit)) is StopWork;
                Button(host, "site-stop", text[stopping ? "site.ordered" : "site.stop" + key], () => bridge.Queue(new StopWork(unit))).SetEnabled(!stopping);
            }
            else Line(host, "site-state", text.Format("site.built" + key, Date(text, work.BuiltOn.Value)));
        }

        /// <summary>A corridor segment (#792, R-COR-001 to R-COR-003): its name, and Build road or Build railway (its corridor's
        /// kind) with each idle pioneer group, with the kind's cost and the unit's own turns (Projects.Terms,
        /// Corridors.TurnsToBuild); or its turns left and Stop while it is built; or the day it was built. The order is
        /// PioneerWork(unit, action, "corridor:N"), SimBridge's corridor key.</summary>
        static void Segment(VisualElement host, SimBridge bridge, UiText text, CorridorDef d, int index)
        {
            var c = bridge.Content; var s = bridge.State;
            var action = d.Kind == Corridors.Rail ? Corridors.BuildRail : Corridors.BuildRoad;
            var target = d.Id + ":" + (index + 1).ToString(CultureInfo.InvariantCulture);
            var terms = Projects.Terms(c.RulesAt(s.Date).Mvp!, action);   // the loader requires the rules in the GDD loop
            var name = text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrWhiteSpace(d.NameHe) ? d.NameHe : d.Name;
            Line(host, "corridor-name", text.Format("corridor.segment", name, index + 1));
            s.Corridors.TryGetValue(d.Id, out var corridor);
            var work = corridor?.Segments[index];
            if (work == null)
            {
                var idle = s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && !Projects.IsBusy(s, u.Id)).ToList();
                if (idle.Count == 0) Line(host, "corridor-no-halutz", text["site.no_halutz"]);
                // One unit a segment (Q-COR-4): once a group is ordered here, the others' Build is off, as the sim would refuse it.
                var taken = bridge.Queued.OfType<PioneerWork>().Any(w => w.Site == target);
                foreach (var u in idle)
                {
                    var ordered = bridge.QueuedLike(new PioneerWork(u.Id, action, target)) is PioneerWork p && p.Site == target;
                    Button(host, $"corridor-build-{u.Id}", ordered ? text["site.ordered"]
                            : text.Format("corridor.build_" + d.Kind, Group(text, s, u.Id), PlayerText.Money(c, "treasury", terms.Cost),
                                Turns(text, Corridors.TurnsToBuild(c, s, u.Id, terms.Turns, 0))),
                        () => bridge.Queue(new PioneerWork(u.Id, action, target))).SetEnabled(!ordered && !taken);
                    PioneerReturn(host, bridge, text, u.Id);
                }
            }
            else if (work.BuiltOn == null)
            {
                var unit = work.Unit!.Value;   // a segment being built names its unit (SegmentWork)
                Line(host, "corridor-state", text.Format("site.building", Group(text, s, unit), Turns(text, Corridors.TurnsToBuild(c, s, unit, terms.Turns - work.Progress, work.Carry))));
                var stopping = bridge.QueuedLike(new StopWork(unit)) is StopWork;
                Button(host, "corridor-stop", text[stopping ? "site.ordered" : "site.stop"], () => bridge.Queue(new StopWork(unit))).SetEnabled(!stopping);
            }
            else Line(host, "corridor-state", text.Format("corridor.built", Date(text, work.BuiltOn.Value)));
        }

        /// <summary>The city projects of hex (q, r) (#1122). A project there shows its state and, while it is built, Cancel
        /// (R-PROJ-002). Else, for the cities in reach (Projects.CitiesFor; none: nothing shows), the city that builds: the one
        /// the player picked on this hex, else the queued order's, else the nearest. Build for each kind queues StartProject for
        /// that city and hex, one order a hex (SimBridge's key, so another kind or city replaces it); the Attention is the
        /// bridge's, which refuses an order it cannot pay for as for every order. On a city's own hex (<paramref name="cityHere"/>)
        /// the cities and the kinds go into the ListName list under the ToggleName toggle, which the stylesheet shows only on a
        /// stacked bar or a short window (InfoPanelPresenter.ShortClass) and which opens and closes the list there.</summary>
        static void CityProjects(VisualElement host, SimBridge bridge, UiText text, int q, int r, Func<string, string> townName, bool cityHere)
        {
            var c = bridge.Content; var s = bridge.State;
            string Kind(string id) => PlayerText.Capitalised(ContentNames.Of(text, c.ProjectKinds.First(x => x.Id == id)));
            if (Projects.CityProjectAt(s, q, r) is CityProject project)
            {
                if (project.BuiltOn is DateTime built)
                {
                    Line(host, "city-project-state", text.Format("city.project_built", Kind(project.Kind), townName(project.City), Date(text, built)));
                    return;
                }
                Line(host, "city-project-state", text.Format("city.project_building", Kind(project.Kind), townName(project.City), Turns(text, Projects.CityTurnsToBuild(c, s, project.Kind, project.TurnsLeft, project.Progress))));
                var cancelled = bridge.QueuedLike(new CancelProject(project.Id)) != null;
                Button(host, "city-project-cancel", text[cancelled ? "city.cancel_ordered" : "city.cancel"],
                    () => bridge.Queue(new CancelProject(project.Id))).SetEnabled(!cancelled);
                return;
            }
            var cities = Projects.CitiesFor(c, s, q, r);
            if (cities.Count == 0 || c.ProjectKinds.Count == 0) return;
            var queued = bridge.QueuedLike(new StartProject("", "", q, r)) as StartProject;
            // Owner ruling on #1150 (21 Oct 2026): under the city's own part the full list ran into the left column on a stacked
            // bar or a short window, so there it folds behind one toggle; a refusal is then one tap away. The open state is the
            // host's class, kept across renders (InfoPanelPresenter.Show closes it for a newly chosen hex).
            var list = host;
            if (cityHere)
            {
                Button(host, ToggleName, text.Format("city.projects_toggle", c.ProjectKinds.Count), () => host.ToggleInClassList(OpenClass));
                list = new VisualElement { name = ListName };
                list.AddToClassList(ListName);
                host.Add(list);
            }
            // The pick lives on the host, which the panel keeps across renders; it is for this hex only.
            var city = host.userData is ValueTuple<int, int, string> pick && pick.Item1 == q && pick.Item2 == r && cities.Contains(pick.Item3) ? pick.Item3
                : queued != null && cities.Contains(queued.City) ? queued.City : cities[0];
            // The city's line and, when several can build, the pick on one row, so the part stays short (InfoPanelLayoutTests).
            if (cities.Count == 1) Line(list, "city-project-for", text.Format("city.project_for", townName(city)));
            else
            {
                var row = Row(list, "city-project-cities");
                Line(row, "city-project-for", text.Format("city.project_for", townName(city)));
                foreach (var id in cities)
                    Button(row, "city-project-city-" + id, townName(id), () =>
                    {
                        host.userData = (q, r, id);
                        host.Clear();
                        CityProjects(host, bridge, text, q, r, townName, cityHere);
                        // The pressed button is rebuilt (and now disabled): the focus stays in the row, for the keyboard.
                        host.Q("city-project-cities")?.Query<Button>().Where(b => b.enabledSelf).First()?.Focus();
                    }).SetEnabled(id != city);
            }
            var refusals = c.ProjectKinds.Select(k => Projects.CheckCityReason(c, s, k.Id, city, q, r)).ToList();
            var words = refusals.Select(x => x == null ? null : ReasonText(x, text, c, townName)).ToList();
            // Every kind refused for one reason (not land, a placement there, the turn's limit): said once, with no buttons,
            // as the map actions do (#398). A kind this city lacks the resource for (R-PROJ-005: the winery outside Rishon
            // LeZion) does not split that line into one per kind.
            var shared = words.Where((x, i) => refusals[i]?.Kind != "no_resource").ToList();
            if (words.All(x => x != null) && shared.Count > 0 && shared.Distinct().Count() == 1) { Line(list, "city-project-reason", shared[0]!); return; }
            // A kind the sim refuses shows its reason alone, which names the kind (no disabled Build above it: one line, not two).
            for (int i = 0; i < c.ProjectKinds.Count; i++)
            {
                var k = c.ProjectKinds[i];
                if (words[i] != null) { Line(list, $"city-project-{k.Id}-reason", words[i]!); continue; }
                var ordered = queued != null && queued.ProjectKind == k.Id && queued.City == city;
                Button(list, "city-project-" + k.Id, ordered ? text.Format("city.project_ordered", ContentNames.Of(text, k))
                        : text.Format("city.project_start", ContentNames.Of(text, k), PlayerText.Money(c, "treasury", k.Cost), Turns(text, Projects.CityTurnsToBuild(c, s, k.Id, k.Turns, 0))),
                    () => bridge.Queue(new StartProject(k.Id, city, q, r))).SetEnabled(!ordered);
            }
        }

        /// <summary>A city-project refusal (Projects.CheckCityReason) in the UI's language: the map actions' strings where the
        /// sim's English is theirs, else city.project_reason.*. The sim's English for the kinds this panel cannot meet: an
        /// unknown kind or city, a distance (CitiesFor lists only the cities in reach) or another project on the hex (its
        /// state shows instead).</summary>
        static string ReasonText(MapActionReason reason, UiText text, Content c, Func<string, string> townName)
        {
            switch (reason.Kind)
            {
                case "maabarot_policy": case "not_land": return text["info.map_action_reason." + reason.Kind];
                case "occupied": return text.Format("info.map_action_reason.occupied", text["info.map_action." + reason.Values[0]]);
                case "no_resource": return text.Format("city.project_reason.no_resource", townName(reason.Values[0]), reason.Values[1],
                    ContentNames.Of(text, c.ProjectKinds.First(x => x.Id == reason.Values[2])));
                case "turn_cap": return text.Format("city.project_reason.turn_cap", int.Parse(reason.Values[0], CultureInfo.InvariantCulture));
                default: return reason.Text;
            }
        }

        /// <summary>"1 group" or "n groups" (population groups, D-A1), in the UI's language.</summary>
        public static string Groups(UiText t, int n) => n == 1 ? t["city.groups_one"] : t.Format("city.groups", n);
        /// <summary>"1 turn" or "n turns", in the UI's language.</summary>
        public static string Turns(UiText t, int n) => n == 1 ? t["city.turns_one"] : t.Format("city.turns", n);
        /// <summary>A pioneer group's name for the player: "halutz group n", numbered from 1 among the pioneer units (halutz and
        /// veteran, Projects.IsPioneer) in id order, so a group keeps its number when it becomes a veteran (unit ids are shared
        /// with cohorts and start at 0, so they are no names).</summary>
        public static string Group(UiText t, GameState s, int unit) =>
            t.Format("site.group", 1 + s.Units.Values.Where(u => Projects.IsPioneer(u.Type)).Select(u => u.Id).TakeWhile(id => id != unit).Count());

        /// <summary>A date in the UI's language, as the event card writes it (EventCardPresenter.ObjectiveLine).</summary>
        static string Date(UiText t, DateTime d) => t.Format("date.day_month_year", d.Day, t["date.month_" + d.Month], d.Year);

        static Label Line(VisualElement parent, string name, string value)
        {
            var l = new Label(value) { name = name };
            l.AddToClassList("wrap"); l.AddToClassList(LineClass);
            parent.Add(l);
            return l;
        }

        static VisualElement Row(VisualElement parent, string name)
        {
            var row = new VisualElement { name = name };
            row.AddToClassList("row"); row.AddToClassList(RowClass);
            parent.Add(row);
            return row;
        }

        static Button Button(VisualElement parent, string name, string label, Action onClick)
        {
            var b = new Button(onClick) { name = name, text = label };
            parent.Add(b);
            return b;
        }
    }
}
