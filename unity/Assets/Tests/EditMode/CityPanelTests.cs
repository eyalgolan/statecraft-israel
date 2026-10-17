#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>GDD Phase 1 PR 8 (#381): the info panel on a city hex (housing, jobs, refugees, recruits, training cohorts; the
/// Assign stepper, Train recruit, Train halutz and Cancel) and on the designated site's hex (its state; Build with each idle
/// halutz group, with the cost and turns from the rules; Stop while it is built). Every number is the sim's; every order goes
/// through the bridge's queue, which the sim then rules on. The legacy loop shows none of it.</summary>
public class CityPanelTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly System.Collections.Generic.List<Settlement> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    const string City = "tel_aviv", Site = "mefalsim";

    static (int q, int r) CityHex => C.SettlementHexes.Where(x => x.Id == City).Select(x => (x.Q, x.R)).Single();
    static Statecraft.Sim.Site SiteDef => C.Sites.Single(x => x.Id == Site);

    /// <summary>A GDD game at the start, changed by <paramref name="edit"/> before the bridge sees it.</summary>
    static SimBridge Bridge(UiText text, Action<GameState>? edit = null)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        Assert.IsTrue(C.IsGdd, "precondition: the real content runs the GDD loop");
        edit?.Invoke(g.State);
        return new SimBridge(g, text);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge, UiText text) =>
        new InfoPanelPresenter(ui.Root, bridge, text, Towns.ToDictionary(t => t.Id, t => t.Name));

    static int AddHalutz(GameState s)
    {
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", CityHex.q, CityHex.r));
        return id;
    }

    static VisualElement Host(UiTestKit ui) => ui.Q<VisualElement>(GddHexPanel.HostName);

    [Test]
    public void TheCityHexShowsItsCountsFromTheSim()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities[City];
        Assert.AreEqual((0, 0), (city.Refugees, city.Recruits), "precondition: no refugees or recruits at the start (the plural forms below)");
        Assert.AreEqual(DisplayStyle.Flex, Host(ui).style.display.value, "the GDD part of the panel shows");
        Assert.AreEqual(En.Format("city.housing", city.Civilians, En.Format("city.groups", city.HousingCapacity)), ui.Q<Label>("city-housing").text);
        Assert.AreEqual(En.Format("city.jobs", city.Employed, city.JobSlots), ui.Q<Label>("city-jobs").text);
        Assert.AreEqual(En.Format("city.refugees", En.Format("city.groups", city.Refugees)), ui.Q<Label>("city-refugees").text);
        Assert.AreEqual(En.Format("city.recruits", En.Format("city.groups", city.Recruits)), ui.Q<Label>("city-recruits").text);
        Assert.AreEqual(En.Format("city.workers", En.Format("city.groups", city.Employed)), ui.Q<Label>("city-workers").text);
    }

    [Test]
    public void TheWorkersStepperQueuesAssignWorkersAndStepsBackToNoOrder()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var employed = bridge.State.Cities[City].Employed;
        UiTestKit.Press(ui.Q<Button>("city-workers-less"));
        var order = bridge.QueuedLike(new AssignWorkers(City, "job", 0)) as AssignWorkers;
        Assert.IsNotNull(order, "the stepper queues an AssignWorkers order");
        Assert.AreEqual(employed - 1, order!.Count);
        Assert.AreEqual(En.Format("city.workers", En.Format("city.groups", employed - 1)), ui.Q<Label>("city-workers").text, "the stepper shows the ordered count");
        UiTestKit.Press(ui.Q<Button>("city-workers-more"));
        Assert.IsNull(bridge.QueuedLike(new AssignWorkers(City, "job", 0)), "back at the current count: no order is left queued");
    }

    [Test]
    public void TheStepperStopsAtTheSimsLimits()
    {
        using var ui = new UiTestKit();
        // R-CITY-001: at most the job slots and the civilians; never below 0.
        var bridge = Bridge(En, s => s.Cities[City] = s.Cities[City].WithCounts(s.Cities[City].Civilians, 0, s.Cities[City].Refugees));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsFalse(ui.Q<Button>("city-workers-less").enabledSelf, "no fewer than 0 workers");
        var full = Math.Min(bridge.State.Cities[City].JobSlots, bridge.State.Cities[City].Civilians);
        var bridge2 = Bridge(En, s => s.Cities[City] = s.Cities[City].WithCounts(s.Cities[City].Civilians, full, s.Cities[City].Refugees));
        using var ui2 = new UiTestKit();
        using var panel2 = Panel(ui2, bridge2, En);
        panel2.Show(CityHex.q, CityHex.r);
        Assert.IsFalse(ui2.Q<Button>("city-workers-more").enabledSelf, "no more than the job slots and the civilians");
    }

    [Test]
    public void EachTrainPressQueuesOneMoreGroupAtTheRulesCost()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        foreach (var track in new[] { "recruit", "halutz" })
        {
            var type = C.UnitTypes.Single(t => t.Id == track);
            var button = ui.Q<Button>("city-train-" + track);
            Assert.AreEqual(En.Format("city.train_" + track, PlayerText.Money(C, "treasury", type.TrainingCost), En.Format("city.turns", type.TrainingTurns)), button.text);
            UiTestKit.Press(button); UiTestKit.Press(ui.Q<Button>("city-train-" + track));
            var order = bridge.QueuedLike(new Train(City, track, 1)) as Train;
            Assert.IsNotNull(order, track);
            Assert.AreEqual(2, order!.Count, $"{track}: two presses, two groups");
            Assert.AreEqual(En.Format("city.train_ordered", 2), ui.Q<Label>("city-train-" + track + "-ordered").text, track);
        }
    }

    [Test]
    public void ACityWithoutTheFacilityOffersNoSuchTraining()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        var haifa = C.SettlementHexes.Single(x => x.Id == "haifa");
        Assert.IsFalse(C.Cities.Single(x => x.Id == "haifa").Facilities.Any(), "precondition: Haifa has no training facility");
        panel.Show(haifa.Q, haifa.R);
        Assert.IsNull(ui.Root.Q<Button>("city-train-recruit"));
        Assert.IsNull(ui.Root.Q<Button>("city-train-halutz"));
        Assert.IsNotNull(ui.Root.Q<Label>("city-housing"), "the counts still show");
    }

    [Test]
    public void ACohortShowsItsTurnsLeftAndCancelQueuesCancelTraining()
    {
        using var ui = new UiTestKit();
        int id = -1;
        var bridge = Bridge(En, s => { id = s.NextId++; s.Cohorts.Add(id, new Cohort(id, "halutz", City, 2, 3)); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var row = ui.Q<Label>($"city-cohort-{id}");
        Assert.AreEqual(En.Format("city.cohort", En["city.track_halutz"], En.Format("city.groups", 2), En.Format("city.turns", 3)), row.text);
        UiTestKit.Press(ui.Q<Button>($"city-cohort-{id}-cancel"));
        Assert.IsNotNull(bridge.QueuedLike(new CancelTraining(id)), "Cancel queues CancelTraining");
    }

    [Test]
    public void OneGroupAndOneTurnAreSingular()
    {
        using var ui = new UiTestKit();
        int id = -1;
        var bridge = Bridge(En, s => { id = s.NextId++; s.Cohorts.Add(id, new Cohort(id, "halutz", City, 1, 1)); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.AreEqual("Halutzim: 1 group in training, 1 turn left", ui.Q<Label>($"city-cohort-{id}").text);
    }

    [Test]
    public void TheSiteHexOffersBuildToEachIdleHalutzGroup()
    {
        using var ui = new UiTestKit();
        int a = -1, b = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); b = AddHalutz(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(SiteDef.Name, ui.Q<Label>("info-title").text, "the site's name is the title");
        Assert.AreEqual(En["site.available"], ui.Q<Label>("site-state").text);
        var r = C.RulesAt(bridge.State.Date).Mvp!;
        var first = ui.Q<Button>($"site-build-{a}");
        Assert.AreEqual(En.Format("site.build", En.Format("site.group", 1), PlayerText.Money(C, "treasury", r.BuildYishuvCost), En.Format("city.turns", r.BuildYishuvTurns)), first.text);
        StringAssert.Contains(En.Format("site.group", 2), ui.Q<Button>($"site-build-{b}").text, "every idle group has its button, numbered from 1");
        UiTestKit.Press(first);
        var order = bridge.QueuedLike(new PioneerWork(a, Projects.BuildYishuv, Site)) as PioneerWork;
        Assert.IsNotNull(order); Assert.AreEqual(Site, order!.Site);
    }

    [Test]
    public void WithNoIdleHalutzTheSiteSaysHowToGetOne()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En["site.no_halutz"], ui.Q<Label>("site-no-halutz").text);
        Assert.IsFalse(ui.Root.Query<Button>().ToList().Any(x => x.name.StartsWith("site-build-")), "no Build button without a group");
    }

    [Test]
    public void ASiteBeingBuiltShowsItsTurnsAndStopQueuesStopWork()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); s.Sites[Site] = new SiteWork(Site, a, 4, null); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En.Format("site.building", En.Format("site.group", 1), En.Format("city.turns", 4)), ui.Q<Label>("site-state").text);
        UiTestKit.Press(ui.Q<Button>("site-stop"));
        Assert.IsNotNull(bridge.QueuedLike(new StopWork(a)), "Stop queues StopWork");
    }

    [Test]
    public void ABuiltSiteSaysWhenItWasFounded()
    {
        using var ui = new UiTestKit();
        var on = new DateTime(1949, 6, 30);
        var bridge = Bridge(En, s => s.Sites[Site] = SiteWork.Built(Site, on));
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En.Format("site.built", PlayerText.Date(on)), ui.Q<Label>("site-state").text);
        Assert.IsNull(ui.Root.Q<Button>("site-stop"));
    }

    [Test]
    public void TheHebrewPanelNamesTheSiteInHebrew()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(He);
        using var panel = Panel(ui, bridge, He);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(SiteDef.NameHe, ui.Q<Label>("info-title").text);
        Assert.AreEqual(He["site.available"], ui.Q<Label>("site-state").text);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities[City];
        Assert.AreEqual(He.Format("city.housing", city.Civilians, He.Format("city.groups", city.HousingCapacity)), ui.Q<Label>("city-housing").text);
    }

    [Test]
    public void TheLegacyLoopShowsNoCityPanel()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(SimBridgeGddTests.Legacy, GoldenRun.Seed, Mode.Historical), En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.AreEqual(DisplayStyle.None, Host(ui).style.display.value);
        Assert.IsNull(ui.Root.Q<Label>("city-housing"));
    }

    /// <summary>The Done-when playthrough (#381) through the panel's own buttons, on the real content from a new game: on
    /// Tel Aviv's hex, Train halutzim (pressed again on a later turn when the Attention left refuses it); when the group has
    /// trained, on the site's hex, Build with it; End Turn until the first new yishuv's objective card is pending, then answer
    /// the cards before it with their first option until the event card shows it, with its objective line. Cards are left to
    /// their ministers otherwise, as the smoke run does with the cards the Attention cannot pay for.</summary>
    [Test]
    public void APlaythroughThroughThePanelReachesTheObjectiveCard()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        using var card = new EventCardPresenter(ui.Root, bridge, En, null);
        DateTime? trainedOn = null, buildOn = null;
        for (int turn = 0; turn < 60 && bridge.State.GameOver == null && !bridge.State.PendingEvents.Contains("first_new_yishuv"); turn++)
        {
            if (trainedOn == null)
            {
                panel.Show(CityHex.q, CityHex.r);
                UiTestKit.Press(ui.Q<Button>("city-train-halutz"));
                if (bridge.QueuedLike(new Train(City, "halutz", 1)) != null) trainedOn = bridge.State.Date;
            }
            else if (buildOn == null && bridge.State.Units.Values.Any(u => u.Type == "halutz"))
            {
                panel.Show(SiteDef.Q, SiteDef.R);
                var build = ui.Root.Query<Button>().ToList().FirstOrDefault(b => b.name.StartsWith("site-build-"));
                Assert.IsNotNull(build, $"{bridge.State.Date:yyyy-MM-dd}: the trained group has its Build button");
                UiTestKit.Press(build!);
                if (bridge.Queued.Any(x => x is PioneerWork)) buildOn = bridge.State.Date;
            }
            var r = bridge.EndTurn();
            Assert.IsEmpty(r.Rejected.Where(x => x.Command is Train || x.Command is PioneerWork).Select(x => x.Reason).ToList(), $"{bridge.State.Date:yyyy-MM-dd}: the panel's orders are taken");
        }
        Assert.IsNotNull(trainedOn, "Train halutzim was taken");
        Assert.IsNotNull(buildOn, "Build was taken");
        Assert.IsTrue(bridge.State.Sites.TryGetValue(Site, out var work) && work.BuiltOn != null, $"the site is built (by {bridge.State.Date:yyyy-MM-dd})");
        Assert.IsTrue(bridge.State.PendingEvents.Contains("first_new_yishuv"), "the objective card is pending");
        for (int guard = 0; card.Shown != null && card.Shown.Id != "first_new_yishuv" && guard < 20; guard++)
            UiTestKit.Press(ui.Root.Q<VisualElement>("event-options").Q<Button>());
        Assert.AreEqual("first_new_yishuv", card.Shown?.Id, "the event card shows the objective card");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("event-objective").style.display.value, "with its objective line");
        Assert.IsNotEmpty(ui.Q<Label>("event-objective").text);
    }
}
