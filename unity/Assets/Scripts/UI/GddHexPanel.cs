#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>GDD Phase 1 PR 8 (#381): the info panel's part for the GDD loop (Content.IsGdd), built into the
    /// <see cref="HostName"/> element on every render.
    /// <para>On a city hex (State.Cities), its counts (housing, jobs, refugees waiting, recruits) and training cohorts with
    /// their turns left, the Assign stepper (R-CITY-001), Train for each facility the city has (R-TRAIN-001) and Cancel
    /// (R-TRAIN-002). On the designated site's hex (sites.yaml), its state, and Build with each idle halutz group, with the
    /// cost and turns from the rules (R-SITE-001), or Stop while it is built (R-SITE-002).</para>
    /// <para>Every number is the sim's, and every order goes through the bridge's queue: the sim rules on it when the turn
    /// runs. No rule lives here; a button is disabled only where the sim would refuse the order anyway.</para></summary>
    public static class GddHexPanel
    {
        public const string HostName = "info-gdd";
        const string LineClass = "info-gdd-line", RowClass = "info-gdd-row";

        public static void Hide(VisualElement host) { host.Clear(); host.style.display = DisplayStyle.None; }

        /// <summary>Builds the hex's GDD part into <paramref name="host"/> (hidden when there is none) and returns the
        /// designated site's name in the UI's language when the hex holds one, else null.</summary>
        public static string? Render(VisualElement host, SimBridge bridge, UiText text, int q, int r)
        {
            host.Clear();
            var c = bridge.Content; var s = bridge.State;
            string? title = null;
            if (c.IsGdd)
            {
                foreach (var at in c.SettlementHexes.Where(x => x.Q == q && x.R == r && s.Cities.ContainsKey(x.Id)))
                    City(host, bridge, text, at.Id);
                var site = c.Sites.FirstOrDefault(x => x.Q == q && x.R == r);
                if (site != null)
                {
                    title = text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrWhiteSpace(site.NameHe) ? site.NameHe : site.Name;
                    Site(host, bridge, text, site);
                }
            }
            host.style.display = host.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            return title;
        }

        static void City(VisualElement host, SimBridge bridge, UiText text, string id)
        {
            var c = bridge.Content; var s = bridge.State; var city = s.Cities[id];
            Line(host, "city-housing", text.Format("city.housing", city.Civilians, city.HousingCapacity));
            Line(host, "city-jobs", text.Format("city.jobs", city.Employed, city.JobSlots));
            Line(host, "city-refugees", text.Format("city.refugees", city.Refugees));
            Line(host, "city-recruits", text.Format("city.recruits", city.Recruits));

            // The Assign stepper: the ordered count (or the current one), one group a press, within R-CITY-001's limits;
            // stepping back to the current count withdraws the order.
            var queued = bridge.QueuedLike(new AssignWorkers(id, "job", 0)) as AssignWorkers;
            var target = queued?.Count ?? city.Employed;
            var max = Math.Min(city.JobSlots, city.Civilians);
            var workers = Row(host, "city-workers-row");
            Button(workers, "city-workers-less", "−", () => Workers(bridge, id, city.Employed, target - 1)).SetEnabled(target > 0);
            Line(workers, "city-workers", text.Format("city.workers", target));
            Button(workers, "city-workers-more", "+", () => Workers(bridge, id, city.Employed, target + 1)).SetEnabled(target < max);

            // Train, for each facility the city has (D-D5): each press orders one more group; R-TRAIN-001 takes only civilians
            // without work.
            var facilities = c.Cities.First(x => x.Id == id).Facilities;
            foreach (var type in c.UnitTypes.Where(t => t.Facility != "" && facilities.Contains(t.Facility)))
            {
                var ordered = (bridge.QueuedLike(new Train(id, type.Id, 1)) as Train)?.Count ?? 0;
                var row = Row(host, $"city-train-{type.Id}-row");
                Button(row, "city-train-" + type.Id, text.Format("city.train_" + type.Id, PlayerText.Money(c, "treasury", type.TrainingCost), type.TrainingTurns),
                    () => bridge.Queue(new Train(id, type.Id, ordered + 1))).SetEnabled(ordered + 1 <= city.Civilians - city.Employed);
                if (ordered > 0) Line(row, $"city-train-{type.Id}-ordered", text.Format("city.train_ordered", ordered));
            }

            foreach (var k in s.Cohorts.Values.Where(x => x.City == id))
            {
                var row = Row(host, $"city-cohort-{k.Id}-row");
                Line(row, $"city-cohort-{k.Id}", text.Format("city.cohort", text["city.track_" + k.Type], k.Count, k.TurnsLeft));
                var cancelled = bridge.QueuedLike(new CancelTraining(k.Id)) != null;
                Button(row, $"city-cohort-{k.Id}-cancel", text[cancelled ? "city.cancel_ordered" : "city.cancel"],
                    () => bridge.Queue(new CancelTraining(k.Id))).SetEnabled(!cancelled);
            }
        }

        static void Workers(SimBridge bridge, string city, int current, int count)
        {
            if (count != current) { bridge.Queue(new AssignWorkers(city, "job", count)); return; }
            if (bridge.QueuedLike(new AssignWorkers(city, "job", 0)) is Command order) bridge.Withdraw(order);
        }

        static void Site(VisualElement host, SimBridge bridge, UiText text, Statecraft.Sim.Site site)
        {
            var c = bridge.Content; var s = bridge.State;
            s.Sites.TryGetValue(site.Id, out var work);
            if (work == null)
            {
                Line(host, "site-state", text["site.available"]);
                var rules = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
                var idle = s.Units.Values.Where(u => u.Type == "halutz" && !Projects.IsBusy(s, u.Id)).ToList();
                if (idle.Count == 0) Line(host, "site-no-halutz", text["site.no_halutz"]);
                foreach (var u in idle)
                {
                    // PioneerWork and StopWork share the unit's key: an order for this unit at this site is this button's.
                    var ordered = bridge.QueuedLike(new PioneerWork(u.Id, Projects.BuildYishuv, site.Id)) is PioneerWork p && p.Site == site.Id;
                    Button(host, $"site-build-{u.Id}", ordered ? text["site.ordered"]
                            : text.Format("site.build", text.Format("site.group", u.Id), PlayerText.Money(c, "treasury", rules.BuildYishuvCost), rules.BuildYishuvTurns),
                        () => bridge.Queue(new PioneerWork(u.Id, Projects.BuildYishuv, site.Id))).SetEnabled(!ordered);
                }
            }
            else if (work.BuiltOn == null)
            {
                var unit = work.Unit!.Value;   // a site being built names its unit (SiteWork)
                Line(host, "site-state", text.Format("site.building", text.Format("site.group", unit), work.TurnsLeft));
                var stopping = bridge.QueuedLike(new StopWork(unit)) is StopWork;
                Button(host, "site-stop", text[stopping ? "site.ordered" : "site.stop"], () => bridge.Queue(new StopWork(unit))).SetEnabled(!stopping);
            }
            else Line(host, "site-state", text.Format("site.built", Date(text, work.BuiltOn.Value)));
        }

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
