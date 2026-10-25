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

    static int AddHalutz(GameState s, string type = "halutz")
    {
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, type, CityHex.q, CityHex.r));
        return id;
    }

    /// <summary>Tel Aviv with <paramref name="civilians"/> civilians, <paramref name="employed"/> of them in jobs.</summary>
    static Action<GameState> Counts(int civilians, int employed) =>
        s => s.Cities[City] = s.Cities[City].WithCounts(civilians, employed, s.Cities[City].Refugees);

    static VisualElement Host(UiTestKit ui) => ui.Q<VisualElement>(GddHexPanel.HostName);

    [Test]
    public void TheCityHexShowsItsCountsFromTheSim()
    {
        // #1121: 4 civilians over the housing and 3 craftsmen trained (housed too, R-HOUSE-003) are 7 groups of overflow
        // (R-HOUSE-002), so both new lines read nonzero sim values.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { var x = s.Cities[City]; s.Cities[City] = x.WithCounts(x.HousingCapacity + 4, x.Employed, x.Refugees).WithCraftsmen(3); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities[City];
        Assert.AreEqual((0, 0), (city.Refugees, city.Recruits), "precondition: no refugees or recruits at the start (the plural forms below)");
        Assert.AreEqual((7, 3), (Cities.Overflow(bridge.State, city), city.Craftsmen), "precondition: the sim's overflow and craftsmen");
        Assert.AreEqual(DisplayStyle.Flex, Host(ui).style.display.value, "the GDD part of the panel shows");
        Assert.AreEqual(En.Format("city.housing_condition", Cities.Housed(bridge.State, city), En.Format("city.groups", city.HousingCapacity), PlayerText.Percent(city.Condition!.Value)), ui.Q<Label>("city-housing").text);
        Assert.AreEqual(En.Format("city.overflow", 7), ui.Q<Label>("city-overflow").text);
        Assert.AreEqual(En.Format("city.jobs", city.Employed, city.JobSlots), ui.Q<Label>("city-jobs").text);
        Assert.AreEqual(En.Format("city.craftsmen", 3), ui.Q<Label>("city-craftsmen").text);
        Assert.AreEqual(En.Format("city.refugees", En.Format("city.groups", city.Refugees)), ui.Q<Label>("city-refugees").text);
        Assert.AreEqual(En.Format("city.soldiers", En.Format("city.groups", 0)), ui.Q<Label>("city-soldiers").text);
        Assert.AreEqual(En.Format("city.workers", En.Format("city.groups", city.Employed)), ui.Q<Label>("city-workers").text);
    }

    [Test]
    public void TheHousingLineCountsRecruitsAndGroupsInTraining()
    {
        // R-HOUSE-003: the people who take a city's housing are its civilians, its recruits and its groups in training
        // (Cities.Housed), not its civilians alone. 10 civilians, 3 recruits and a cohort of 2 are 15 housed.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s =>
        {
            s.Cities[City] = s.Cities[City].WithCounts(10, 0, 0).WithRecruits(3);
            var id = s.NextId++;
            s.Cohorts.Add(id, new Cohort(id, "halutz", City, 2, 3));
        });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities[City];
        Assert.AreEqual(15, Cities.Housed(bridge.State, city), "precondition: the sim houses the recruits and the trainees too");
        Assert.AreEqual(En.Format("city.housing_condition", 15, En.Format("city.groups", city.HousingCapacity), PlayerText.Percent(city.Condition!.Value)), ui.Q<Label>("city-housing").text);
    }

    [Test]
    public void TheSoldiersLineCountsEveryIsraeliFormationHousedInTheCity([Values] bool hebrew)
    {
        // #1345 (owner ruling, 23 Oct 2026, option b): since #1130 trained recruits are formations housed in their home city
        // (R-HOUSE-003, R-UNIT-004), so the line counts every Israeli formation whose HomeCity is the city, the same sum
        // Cities.Housed takes: a recruit group, a promoted veteran, guns and armoured cars, wherever they stand. A formation
        // homed elsewhere and an Arab unit are not the city's.
        var t = hebrew ? He : En;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s =>
        {
            void Add(string type, string side, int strength, string home)
            {
                var id = s.NextId++;
                s.Units.Add(id, new Unit(id, type, CityHex.q, CityHex.r, 0, side, strength, "", "", home));
            }
            Add("recruit", "israel", 2, City);
            Add("veteran_soldier", "israel", 3, City);
            Add("artillery", "israel", 1, City);
            Add("armoured_car", "israel", 1, City);
            Add("recruit", "israel", 4, "jerusalem");
            Add("egyptian_army", "egypt", 5, City);
        });
        using var panel = Panel(ui, bridge, t);
        panel.Show(CityHex.q, CityHex.r);
        Assert.AreEqual(0, bridge.State.Cities[City].Recruits, "precondition: nothing writes City.Recruits since #1130");
        Assert.AreEqual(t.Format("city.soldiers", t.Format("city.groups", 7)), ui.Q<Label>("city-soldiers").text);
    }

    [Test]
    public void TheCityHexShowsItsHousingConditionFromTheSim()
    {
        // R-HOUSE-001: each city's housing condition, 0 to 1, on the Housing line (so the panel is no taller), shown with the
        // one percent formatter.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => s.Cities[City] = s.Cities[City].WithCondition(0.42));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities[City];
        Assert.AreEqual(En.Format("city.housing_condition", Cities.Housed(bridge.State, city), En.Format("city.groups", city.HousingCapacity), "42%"), ui.Q<Label>("city-housing").text);
        Assert.AreEqual("42%", PlayerText.Percent(0.42), "precondition: the formatter's reading");
    }

    [Test]
    public void ACityWithoutAConditionShowsThePlainHousingLine()
    {
        // City.Condition is null only on a hand-built record; the line then shows the count without a made-up condition.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { var x = s.Cities[City]; s.Cities[City] = new Statecraft.Sim.City(x.Id, x.HousingCapacity, x.JobSlots, 10, 0, 0, 3); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities[City];
        Assert.IsNull(city.Condition, "precondition: no condition");
        Assert.AreEqual(En.Format("city.housing", 13, En.Format("city.groups", city.HousingCapacity)), ui.Q<Label>("city-housing").text);
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
    public void TheStepperStopsAtTheCiviliansWhenThereAreMoreJobs()
    {
        // R-CITY-001's other limit: Tel Aviv's 248 civilians are above its 106 jobs at the start, so give it fewer.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, Counts(50, 50));
        Assert.Greater(bridge.State.Cities[City].JobSlots, 50, "precondition: jobs are not the limit");
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsFalse(ui.Q<Button>("city-workers-more").enabledSelf, "no more workers than civilians");
    }

    [Test]
    public void AQueuedTrainOrderTakesItsCiviliansFromTheStepper()
    {
        // 50 civilians, 49 in jobs: one more worker, or one trainee, but not both (Cities.MaxWorkers with the queue).
        using var ui = new UiTestKit();
        var bridge = Bridge(En, Counts(50, 49));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(ui.Q<Button>("city-workers-more").enabledSelf, "precondition: one more worker fits");
        UiTestKit.Press(ui.Q<Button>("city-train-recruit"));
        Assert.IsNotNull(bridge.QueuedLike(new Train(City, "recruit", 1)));
        Assert.IsFalse(ui.Q<Button>("city-workers-more").enabledSelf, "the trainee takes the last civilian without work");
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
    public void TrainIsDisabledWithNoCivilianWithoutWork()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En, Counts(50, 50));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsFalse(ui.Q<Button>("city-train-recruit").enabledSelf, "R-TRAIN-001: every civilian is in work");
        Assert.IsFalse(ui.Q<Button>("city-train-halutz").enabledSelf);
    }

    [Test]
    public void TrainingAcrossBothTracksStopsAtTheIdleCivilianLimit()
    {
        // One civilian without work: once recruits take it, halutzim cannot (Training.Available counts every track).
        using var ui = new UiTestKit();
        var bridge = Bridge(En, Counts(50, 49));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(ui.Q<Button>("city-train-halutz").enabledSelf, "precondition: one group can train");
        UiTestKit.Press(ui.Q<Button>("city-train-recruit"));
        Assert.IsFalse(ui.Q<Button>("city-train-recruit").enabledSelf, "the recruit order took the last civilian without work");
        Assert.IsFalse(ui.Q<Button>("city-train-halutz").enabledSelf, "the other track counts that recruit order too");
    }

    [Test]
    public void AQueuedAssignOrderTakesItsCiviliansFromTraining()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En, Counts(50, 49));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        UiTestKit.Press(ui.Q<Button>("city-workers-more"));
        Assert.AreEqual(50, (bridge.QueuedLike(new AssignWorkers(City, "job", 0)) as AssignWorkers)?.Count);
        Assert.IsFalse(ui.Q<Button>("city-train-recruit").enabledSelf, "the worker order took the last civilian without work");
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
    public void ABuiltCampGivesItsCityTheTrainButton()
    {
        // R-CAMP-001 (Training.HasFacility): a city gains a facility from a built camp site, not only from cities.yaml.
        var camp = C.Sites.First(x => Projects.IsCamp(x) && x.City != "" && x.Kinds.Contains("military_camp"));
        Assert.IsFalse(C.Cities.Single(x => x.Id == camp.City).Facilities.Contains("military_camp"), "precondition: the city starts without a camp");
        var hex = C.SettlementHexes.First(x => x.Id == camp.City);
        foreach (var built in new[] { false, true })
        {
            using var ui = new UiTestKit();
            var bridge = Bridge(En, s => s.Sites[camp.Id] = built ? SiteWork.Built(camp.Id, s.Date) : new SiteWork(camp.Id, AddHalutz(s), 2, null));
            using var panel = Panel(ui, bridge, En);
            panel.Show(hex.Q, hex.R);
            Assert.AreEqual(built, ui.Root.Q<Button>("city-train-recruit") != null, built ? "a built camp trains recruits" : "a camp being built does not count yet");
        }
    }

    [Test]
    public void EveryTrainableTypeHasThePanelsStringsInBothLanguages()
    {
        // #1357: the panel words a Train button as city.train_<type> and a cohort as city.track_<type>, for every type with a
        // facility; a missing key throws (UiText) and the whole city panel fails to open. A new trainable type needs both.
        var trainable = C.UnitTypes.Where(t => t.Facility != "").Select(t => t.Id).ToList();
        CollectionAssert.Contains(trainable, Training.Craftsman, "precondition: the craftsman trains at a facility (R-CRAFT-001)");
        var missing = (from strings in new[] { ("strings.en.yaml", En), ("strings.he.yaml", He) }
                       from id in trainable
                       from key in new[] { "city.train_" + id, "city.track_" + id }
                       where !strings.Item2.Has(key)
                       select $"{strings.Item1}: {key}").ToList();
        Assert.IsEmpty(missing, string.Join(", ", missing));
    }

    [Test]
    public void ACityWithAFinishedIndustryProjectOffersCraftTraining([Values] bool hebrew)
    {
        // #1357, R-CRAFT-001: a completed industry project gives its city the craftsman's Train button and its cohorts' rows.
        var t = hebrew ? He : En;
        var type = C.UnitTypes.Single(x => x.Id == Training.Craftsman);
        Assert.IsFalse(C.Cities.Single(x => x.Id == City).Facilities.Contains(Training.Industry), "precondition: Tel Aviv's industry comes from the project");
        using var ui = new UiTestKit();
        int cohort = -1;
        var bridge = Bridge(t, s =>
        {
            var id = s.NextId++;
            s.CityProjects.Add(id, new CityProject(id, Training.Industry, City, CityHex.q, CityHex.r, 1).Built(s.Date));
            cohort = s.NextId++;
            s.Cohorts.Add(cohort, new Cohort(cohort, Training.Craftsman, City, 2, 3));
        });
        using var panel = Panel(ui, bridge, t);
        panel.Show(CityHex.q, CityHex.r);
        Assert.AreEqual(t.Format("city.train_craftsman", PlayerText.Money(C, "treasury", type.TrainingCost), GddHexPanel.Turns(t, type.TrainingTurns)),
            ui.Q<Button>("city-train-craftsman").text);
        Assert.AreEqual(t.Format("city.cohort", t["city.track_craftsman"], GddHexPanel.Groups(t, 2), GddHexPanel.Turns(t, 3)), ui.Q<Label>($"city-cohort-{cohort}").text);
        UiTestKit.Press(ui.Q<Button>("city-train-craftsman"));
        Assert.IsNotNull(bridge.QueuedLike(new Train(City, Training.Craftsman, 1)), "the press queues craft training");
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
        Assert.IsFalse(ui.Q<Button>($"city-cohort-{id}-cancel").enabledSelf, "Cancel is disabled once ordered");
        Assert.AreEqual(En["city.cancel_ordered"], ui.Q<Button>($"city-cohort-{id}-cancel").text);
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
        Assert.AreEqual(0.624, EffectsSystem.GddSystemMultiplier(C, bridge.State, r, EffectsSystem.YishuvWorkSystem), 1e-9, "precondition: real Agriculture multiplier");
        Assert.AreEqual(6, r.BuildYishuvTurns, "the owner's base work is unchanged");
        Assert.AreEqual(0, bridge.State.Units[a].Service, "the idle group is not already partway to veteran");
        var played = bridge.State.Clone();
        int elapsed = 0;
        do
        {
            var result = Simulation.Step(C, played, elapsed == 0 ? new Command[] { new PioneerWork(a, Projects.BuildYishuv, Site) } : new Command[0]);
            Assert.IsEmpty(result.Rejected);
            played = result.State; elapsed++;
        } while (played.Sites[Site].BuiltOn == null && elapsed < 12);
        Assert.IsNotNull(played.Sites[Site].BuiltOn, "real resolver finishes the fixture");
        Assert.AreEqual(8, elapsed, "six work units at the real multiplier, including veteran promotion");
        var first = ui.Q<Button>($"site-build-{a}");
        Assert.AreEqual(En.Format("site.build", En.Format("site.group", 1), PlayerText.Money(C, "treasury", r.BuildYishuvCost), En.Format("city.turns", 8)), first.text);
        StringAssert.Contains(En.Format("site.group", 2), ui.Q<Button>($"site-build-{b}").text, "every idle group has its button, numbered from 1");
        UiTestKit.Press(first);
        var order = bridge.QueuedLike(new PioneerWork(a, Projects.BuildYishuv, Site)) as PioneerWork;
        Assert.IsNotNull(order); Assert.AreEqual(Site, order!.Site);
    }

    [Test]
    public void EachBuildButtonOrdersItsOwnGroupAndOneGroupASite()
    {
        using var ui = new UiTestKit();
        int a = -1, b = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); b = AddHalutz(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        UiTestKit.Press(ui.Q<Button>($"site-build-{b}"));
        var orders = bridge.Queued.OfType<PioneerWork>().ToList();
        Assert.AreEqual(1, orders.Count, "one order");
        Assert.AreEqual((b, Site), (orders[0].Unit, orders[0].Site), "the second group's button orders the second group");
        Assert.IsFalse(ui.Q<Button>($"site-build-{b}").enabledSelf, "Build is disabled once ordered");
        Assert.AreEqual(En["site.ordered"], ui.Q<Button>($"site-build-{b}").text);
        Assert.IsFalse(ui.Q<Button>($"site-build-{a}").enabledSelf, "one group builds a site: the sim would refuse a second order");
    }

    [Test]
    public void AGroupBuildingElsewhereIsNotOffered()
    {
        using var ui = new UiTestKit();
        int busy = -1, idle = -1;
        var other = C.Sites.First(x => Projects.IsYishuvSite(x) && x.Id != Site).Id;
        var bridge = Bridge(En, s => { busy = AddHalutz(s); idle = AddHalutz(s); s.Sites[other] = new SiteWork(other, busy, 3, null); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.IsNull(ui.Root.Q<Button>($"site-build-{busy}"), "a group with a project is not idle (D-D6)");
        Assert.IsNotNull(ui.Root.Q<Button>($"site-build-{idle}"));
    }

    [Test]
    public void AVeteranGroupIsOfferedBuildWithItsOwnTurns()
    {
        // R-VET-002: a veteran builds at veteran_progress a turn, so the label's turns are the sim's for that unit.
        using var ui = new UiTestKit();
        int vet = -1;
        var bridge = Bridge(En, s => vet = AddHalutz(s, Projects.VeteranType));
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        var r = C.RulesAt(bridge.State.Date).Mvp!;
        var turns = Projects.TurnsToBuild(C, bridge.State, vet, SiteDef, r.BuildYishuvTurns, 0);
        Assert.Less(turns, r.BuildYishuvTurns, "precondition: a veteran builds faster in the real content");
        var button = ui.Q<Button>($"site-build-{vet}");
        Assert.IsNotNull(button, "a veteran group has a Build button");
        Assert.AreEqual(En.Format("site.build", En.Format("site.group", 1), PlayerText.Money(C, "treasury", r.BuildYishuvCost), En.Format("city.turns", turns)), button.text);
        UiTestKit.Press(button);
        Assert.IsNotNull(bridge.QueuedLike(new PioneerWork(vet, Projects.BuildYishuv, Site)));
    }

    [TestCase(0d, 4)]
    [TestCase(0.75d, 3)]
    public void AVeteransSiteShowsTheTurnsItReallyHasLeft(double carry, int expected)
    {
        using var ui = new UiTestKit();
        int vet = -1;
        var bridge = Bridge(En, s => { vet = AddHalutz(s, Projects.VeteranType); s.Sites[Site] = new SiteWork(Site, vet, 4, null, carry); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        var turns = Projects.TurnsToBuild(C, bridge.State, vet, SiteDef, 4, carry);
        Assert.AreEqual(0.624, EffectsSystem.GddSystemMultiplier(C, bridge.State, C.RulesAt(bridge.State.Date).Mvp!, EffectsSystem.YishuvWorkSystem), 1e-9, "precondition: current real Agriculture rate");
        Assert.AreEqual(expected, turns, "four work units at 2 x 0.624 per turn, including saved carry");
        Assert.AreEqual(En.Format("site.building", En.Format("site.group", 1), En.Format("city.turns", turns)), ui.Q<Label>("site-state").text);
    }

    [Test]
    public void TheYishuvSiteIsFoundByItsKindNotItsPlaceInTheFile()
    {
        // Since #866 a yishuv hex also holds a field site; list one first and the panel still shows the yishuv.
        var field = new Statecraft.Sim.Site { Id = "a_field_listed_first", Name = "A field", Q = SiteDef.Q, R = SiteDef.R, Kinds = new System.Collections.Generic.List<string> { "field" } };
        C.Sites.Insert(0, field);
        try
        {
            using var ui = new UiTestKit();
            int a = -1;
            var bridge = Bridge(En, s => a = AddHalutz(s));
            using var panel = Panel(ui, bridge, En);
            panel.Show(SiteDef.Q, SiteDef.R);
            Assert.AreEqual(SiteDef.Name, ui.Q<Label>("info-title").text);
            Assert.AreEqual(En["site.available"], ui.Q<Label>("site-state").text);
            UiTestKit.Press(ui.Q<Button>($"site-build-{a}"));
            Assert.AreEqual(Site, (bridge.QueuedLike(new PioneerWork(a, Projects.BuildYishuv, Site)) as PioneerWork)?.Site);
        }
        finally { C.Sites.Remove(field); }
    }

    [Test]
    public void AHexWithOnlyAFieldSiteOffersPlow()
    {
        // #1124: a field with no yishuv or swamp in its hex is ready (Projects.YishuvToBuildFirst), so it offers Plow.
        var field = new Statecraft.Sim.Site { Id = "a_lone_field", Name = "A lone field", Q = CityHex.q, R = CityHex.r, Kinds = new System.Collections.Generic.List<string> { "field" } };
        C.Sites.Add(field);
        try
        {
            using var ui = new UiTestKit();
            int a = -1;
            var bridge = Bridge(En, s => a = AddHalutz(s));
            using var panel = Panel(ui, bridge, En);
            panel.Show(CityHex.q, CityHex.r);
            Assert.AreEqual(En["site.available_field"], SiteBox(ui, field.Id).Q<Label>("site-state").text);
            UiTestKit.Press(SiteBox(ui, field.Id).Q<Button>($"site-build-{a}"));
            Assert.AreEqual(Projects.PlowFields, (bridge.QueuedLike(new StopWork(a)) as PioneerWork)?.Action);
        }
        finally { C.Sites.Remove(field); }
    }

    /// <summary>The part of the panel for one site: a hex may hold two (a yishuv or swamp and its field).</summary>
    static VisualElement SiteBox(UiTestKit ui, string site) => ui.Q<VisualElement>("site-part-" + site);
    static Statecraft.Sim.Site Def(string site) => C.Sites.Single(x => x.Id == site);

    /// <summary>#1124: a camp site offers Build a training camp to each idle group, with build_camp_cost and the group's own
    /// turns (Projects.Terms, Projects.TurnsToBuild), and the button, a focusable one for the keyboard, queues build_camp there.</summary>
    [Test]
    public void ACampSiteOffersBuildCampWithTheRulesCostAndTurns()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(En, s => a = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        var camp = Def("camp_80");
        panel.Show(camp.Q, camp.R);
        Assert.AreEqual(camp.Name, ui.Q<Label>("info-title").text);
        Assert.AreEqual(En["site.available_camp"], SiteBox(ui, camp.Id).Q<Label>("site-state").text);
        var r = C.RulesAt(bridge.State.Date).Mvp!;
        var button = SiteBox(ui, camp.Id).Q<Button>($"site-build-{a}");
        Assert.AreEqual(En.Format("site.build_camp", En.Format("site.group", 1), PlayerText.Money(C, "treasury", r.BuildCampCost!.Value),
            GddHexPanel.Turns(En, Projects.TurnsToBuild(C, bridge.State, a, camp, r.BuildCampTurns!.Value, 0))), button.text);
        Assert.IsTrue(button.focusable, "the keyboard reaches it");
        UiTestKit.Press(button);
        var order = bridge.QueuedLike(new StopWork(a)) as PioneerWork;
        Assert.AreEqual((Projects.BuildCamp, camp.Id), (order?.Action, order?.Site));
        Assert.AreEqual(En["site.ordered"], SiteBox(ui, camp.Id).Q<Button>($"site-build-{a}").text);
    }

    /// <summary>#1124 (R-SWAMP-001): the Hula hex offers Drain the swamp, and its field says it waits for the drainage, as
    /// Projects.YishuvToBuildFirst has it, with no Plow button.</summary>
    [Test]
    public void TheHulaHexDrainsTheSwampAndItsFieldWaits()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(En, s => a = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        var swamp = Def("hula");
        panel.Show(swamp.Q, swamp.R);
        Assert.AreEqual(swamp.Name, ui.Q<Label>("info-title").text, "the swamp, not its field, is the title");
        Assert.AreEqual(En["site.available_swamp"], SiteBox(ui, "hula").Q<Label>("site-state").text);
        var r = C.RulesAt(bridge.State.Date).Mvp!;
        var drain = SiteBox(ui, "hula").Q<Button>($"site-build-{a}");
        Assert.AreEqual(En.Format("site.build_swamp", En.Format("site.group", 1), PlayerText.Money(C, "treasury", r.DrySwampCost!.Value),
            GddHexPanel.Turns(En, Projects.TurnsToBuild(C, bridge.State, a, swamp, r.DrySwampTurns!.Value, 0))), drain.text);
        Assert.AreEqual(En.Format("site.waits_swamp", swamp.Name), SiteBox(ui, "hula_field").Q<Label>("site-state").text);
        Assert.IsNull(SiteBox(ui, "hula_field").Q<Button>($"site-build-{a}"), "the sim refuses to plow it before the drainage");
        UiTestKit.Press(drain);
        var order = bridge.QueuedLike(new StopWork(a)) as PioneerWork;
        Assert.AreEqual((Projects.DrySwamp, "hula"), (order?.Action, order?.Site));
    }

    /// <summary>#41 (review of #1330): on a hex with two sites both parts hold a site-build-&lt;unit&gt; button; when the panel
    /// is rebuilt (any bridge change), the focus goes back to the button in the part that had it, not the first part's.</summary>
    [Test]
    public void ARebuiltSiteHexKeepsTheFocusInThePartThatHadIt()
    {
        var camp = Def("camp_80");
        var field = new Statecraft.Sim.Site { Id = "a_camp_field", Name = "A camp field", Q = camp.Q, R = camp.R, Kinds = new System.Collections.Generic.List<string> { "field" } };
        C.Sites.Add(field);
        try
        {
            using var ui = new UiTestKit();
            int a = -1;
            var bridge = Bridge(En, s => a = AddHalutz(s));
            using var panel = Panel(ui, bridge, En);
            panel.Show(camp.Q, camp.R);
            Assert.IsNotNull(SiteBox(ui, camp.Id).Q<Button>($"site-build-{a}"), "precondition: the camp's part, first, has the same button");
            SiteBox(ui, field.Id).Q<Button>($"site-build-{a}").Focus();
            panel.Render();
            var focused = ui.Root.focusController.focusedElement as VisualElement;
            Assert.AreEqual($"site-build-{a}", focused?.name);
            Assert.IsTrue(SiteBox(ui, field.Id).Contains(focused), "the field's rebuilt Build, not the camp's");
        }
        finally { C.Sites.Remove(field); }
    }

    /// <summary>#1124 (R-FIELD-001): a yishuv's field waits while the yishuv is not built, and offers Plow once it is.</summary>
    [Test]
    public void AYishuvsFieldIsPlowedOnceTheYishuvIsBuilt()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(En, s => a = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En.Format("site.waits_yishuv", SiteDef.Name), SiteBox(ui, "mefalsim_field").Q<Label>("site-state").text);
        Assert.IsNull(SiteBox(ui, "mefalsim_field").Q<Button>($"site-build-{a}"));

        bridge = Bridge(En, s => { a = AddHalutz(s); s.Sites[Site] = SiteWork.Built(Site, s.Date); });
        using var ui2 = new UiTestKit();
        using var panel2 = Panel(ui2, bridge, En);
        panel2.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En["site.available_field"], SiteBox(ui2, "mefalsim_field").Q<Label>("site-state").text);
        var r = C.RulesAt(bridge.State.Date).Mvp!;
        var plow = SiteBox(ui2, "mefalsim_field").Q<Button>($"site-build-{a}");
        var turns = Projects.TurnsToBuild(C, bridge.State, a, Def("mefalsim_field"), r.PlowFieldsTurns, 0);
        Assert.AreEqual(5, turns, "field work runs at Agriculture's rate (0.624), so a fresh plow takes 5 turns, not 3 (#1147)");
        Assert.AreEqual(En.Format("site.build_field", En.Format("site.group", 1), PlayerText.Money(C, "treasury", r.PlowFieldsCost!.Value),
            GddHexPanel.Turns(En, turns)), plow.text);
        UiTestKit.Press(plow);
        var order = bridge.QueuedLike(new StopWork(a)) as PioneerWork;
        Assert.AreEqual((Projects.PlowFields, "mefalsim_field"), (order?.Action, order?.Site));
    }

    /// <summary>#1124: a field being plowed and a plowed one say so in their own words, from the sim's state.</summary>
    [Test]
    public void AFieldBeingPlowedAndAPlowedFieldSaySo()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var on = new DateTime(1950, 3, 31);
        var bridge = Bridge(En, s => { a = AddHalutz(s); s.Sites[Site] = SiteWork.Built(Site, s.Date); s.Sites["mefalsim_field"] = new SiteWork("mefalsim_field", a, 2, null); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En.Format("site.building_field", En.Format("site.group", 1), GddHexPanel.Turns(En, Projects.TurnsToBuild(C, bridge.State, a, Def("mefalsim_field"), 2, bridge.State.Sites["mefalsim_field"].Progress))),
            SiteBox(ui, "mefalsim_field").Q<Label>("site-state").text);
        Assert.AreEqual(En["site.stop_field"], SiteBox(ui, "mefalsim_field").Q<Button>("site-stop").text);
        bridge.State.Sites["mefalsim_field"] = SiteWork.Built("mefalsim_field", on);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.AreEqual(En.Format("site.built_field", PlayerText.Date(on)), SiteBox(ui, "mefalsim_field").Q<Label>("site-state").text);
    }

    /// <summary>#1124: a group busy at one site is not offered at another (D-D6), and a group whose order is queued for one
    /// site is offered at another, where its button moves that order (one order a unit, SimBridge.Key).</summary>
    [Test]
    public void ABusyGroupIsNotOfferedAndAQueuedGroupsOrderMoves()
    {
        using var ui = new UiTestKit();
        int busy = -1, a = -1;
        var bridge = Bridge(En, s => { busy = AddHalutz(s); a = AddHalutz(s); s.Sites["hula"] = new SiteWork("hula", busy, 5, null); });
        Assert.IsNull(bridge.Queue(new PioneerWork(a, Projects.BuildYishuv, Site)));
        using var panel = Panel(ui, bridge, En);
        var camp = Def("camp_80");
        panel.Show(camp.Q, camp.R);
        Assert.IsNull(SiteBox(ui, camp.Id).Q<Button>($"site-build-{busy}"), "a group with a project is not idle");
        var button = SiteBox(ui, camp.Id).Q<Button>($"site-build-{a}");
        Assert.IsTrue(button.enabledSelf, "its order is for another site");
        UiTestKit.Press(button);
        var orders = bridge.Queued.OfType<PioneerWork>().ToList();
        Assert.AreEqual(1, orders.Count);
        Assert.AreEqual((a, Projects.BuildCamp, camp.Id), (orders[0].Unit, orders[0].Action, orders[0].Site));
    }

    /// <summary>#1124 (R-SITE-002): Stop on a field being plowed queues StopWork; the turn withdraws the group and cancels the
    /// work, so the field offers Plow to it again.</summary>
    [Test]
    public void StopOnAFieldWithdrawsTheGroupAndCancelsTheWork()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); s.Sites[Site] = SiteWork.Built(Site, s.Date); s.Sites["mefalsim_field"] = new SiteWork("mefalsim_field", a, 2, null); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        UiTestKit.Press(SiteBox(ui, "mefalsim_field").Q<Button>("site-stop"));
        Assert.IsInstanceOf<StopWork>(bridge.QueuedLike(new StopWork(a)));
        Assert.IsFalse(SiteBox(ui, "mefalsim_field").Q<Button>("site-stop").enabledSelf);
        var result = bridge.EndTurn();
        Assert.IsEmpty(result.Rejected.Where(x => x.Command is StopWork).ToList());
        Assert.IsFalse(bridge.State.Sites.ContainsKey("mefalsim_field"), "the work is cancelled");
        Assert.IsFalse(Projects.IsBusy(bridge.State, a), "the group is idle");
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.IsNotNull(SiteBox(ui, "mefalsim_field").Q<Button>($"site-build-{a}"), "the field offers Plow again");
    }

    [Test]
    public void TheHebrewCampSiteSpeaksHebrew()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(He, s => a = AddHalutz(s));
        using var panel = Panel(ui, bridge, He);
        var camp = Def("camp_80");
        panel.Show(camp.Q, camp.R);
        Assert.AreEqual(camp.NameHe, ui.Q<Label>("info-title").text);
        Assert.AreEqual(He["site.available_camp"], SiteBox(ui, camp.Id).Q<Label>("site-state").text);
        var r = C.RulesAt(bridge.State.Date).Mvp!;
        Assert.AreEqual(He.Format("site.build_camp", He.Format("site.group", 1), PlayerText.Money(C, "treasury", r.BuildCampCost!.Value),
            GddHexPanel.Turns(He, Projects.TurnsToBuild(C, bridge.State, a, camp, r.BuildCampTurns!.Value, 0))), SiteBox(ui, camp.Id).Q<Button>($"site-build-{a}").text);
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
        Assert.AreEqual(0.624, EffectsSystem.GddSystemMultiplier(C, bridge.State, C.RulesAt(bridge.State.Date).Mvp!, EffectsSystem.YishuvWorkSystem), 1e-9, "precondition: real Agriculture multiplier");
        Assert.AreEqual((0, 4, 0d), (bridge.State.Units[a].Service, bridge.State.Sites[Site].TurnsLeft, bridge.State.Sites[Site].Progress), "precondition: a fresh halutz with four whole work units and no carry");
        var played = bridge.State.Clone();
        int elapsed = 0;
        do
        {
            var result = Simulation.Step(C, played, new Command[0]);
            Assert.IsEmpty(result.Rejected);
            played = result.State; elapsed++;
        } while (played.Sites[Site].BuiltOn == null && elapsed < 12);
        Assert.IsNotNull(played.Sites[Site].BuiltOn, "real resolver finishes the fixture");
        Assert.AreEqual(7, elapsed, "remaining work at the real multiplier, including veteran promotion");
        Assert.AreEqual(En.Format("site.building", En.Format("site.group", 1), En.Format("city.turns", 7)), ui.Q<Label>("site-state").text);
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
        Assert.AreEqual(He.Format("city.housing_condition", Cities.Housed(bridge.State, city), He.Format("city.groups", city.HousingCapacity), PlayerText.Percent(city.Condition!.Value)), ui.Q<Label>("city-housing").text);
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
    /// trained, on the site's hex, Build with it; build every designated yishuv through the panel and End Turn until their objective card is pending, then answer
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
            else if (buildOn != null && bridge.State.Sites.TryGetValue(Site, out var firstWork) && firstWork.BuiltOn != null)
            {
                var remaining = C.Objectives.Single(o => o.Id == "yishuvim").CompletesWhen!.SitesBuilt
                    .Select(id => C.Sites.Single(site => site.Id == id))
                    .FirstOrDefault(site => !bridge.State.Sites.TryGetValue(site.Id, out var work) || work.BuiltOn == null);
                if (remaining != null)
                {
                    panel.Show(remaining.Q, remaining.R);
                    var build = ui.Root.Query<Button>().ToList().FirstOrDefault(b => b.name.StartsWith("site-build-"));
                    if (build != null) UiTestKit.Press(build);
                }
            }
            var r = bridge.EndTurn();
            Assert.IsEmpty(r.Rejected.Where(x => x.Command is Train || x.Command is PioneerWork).Select(x => x.Reason).ToList(), $"{bridge.State.Date:yyyy-MM-dd}: the panel's orders are taken");
        }
        Assert.IsNotNull(trainedOn, "Train halutzim was taken");
        Assert.IsNotNull(buildOn, "Build was taken");
        Assert.IsTrue(bridge.State.Sites.TryGetValue(Site, out var work) && work.BuiltOn != null, $"the site is built (by {bridge.State.Date:yyyy-MM-dd})");
        Assert.IsTrue(C.Objectives.Single(o => o.Id == "yishuvim").CompletesWhen!.SitesBuilt
            .All(id => bridge.State.Sites.TryGetValue(id, out var completed) && completed.BuiltOn != null), "all designated sites are built");
        Assert.IsTrue(bridge.State.Objectives.ContainsKey("yishuvim"), "the objective is recorded under its new id");
        Assert.IsTrue(bridge.State.PendingEvents.Contains("first_new_yishuv"), "the unchanged event-card id is pending");
        for (int guard = 0; card.Shown != null && card.Shown.Id != "first_new_yishuv" && guard < 20; guard++)
            UiTestKit.Press(ui.Root.Q<VisualElement>("event-options").Q<Button>());
        Assert.AreEqual("first_new_yishuv", card.Shown?.Id, "the event card shows the objective card");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("event-objective").style.display.value, "with its objective line");
        Assert.IsNotEmpty(ui.Q<Label>("event-objective").text);
    }

    // #792 (GDD Phase 4 PR 8, item 2): Build road and Build railway on a corridor segment's hex, as the sim's
    // PioneerWork(unit, build_road | build_rail, "corridor:N"), with the cost and turns from the sim (Projects.Terms,
    // Corridors.TurnsToBuild). Each segment through the hex has its own part.

    static (int q, int r) SegmentHex(string corridor, int n, int i = 0)
    {
        var h = C.Corridors.Single(x => x.Id == corridor).Segments[n - 1].Hexes[i];
        return (h[0], h[1]);
    }

    static VisualElement SegmentBox(UiTestKit ui, string corridor, int n) => ui.Root.Q<VisualElement>($"corridor-part-{corridor}-{n}");

    [Test]
    public void ASegmentHexOffersBuildRoadToEachIdleGroupWithTheSimsNumbers()
    {
        using var ui = new UiTestKit();
        int a = -1, b = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); b = AddHalutz(s); });
        using var panel = Panel(ui, bridge, En);
        var (q, r) = SegmentHex("burma_road", 1);
        panel.Show(q, r);
        var box = SegmentBox(ui, "burma_road", 1);
        Assert.IsNotNull(box, "the segment through the hex has its part");
        Assert.AreEqual(En.Format("corridor.segment", "Burma Road", 1), box.Q<Label>("corridor-name").text);
        var terms = Projects.Terms(C.RulesAt(bridge.State.Date).Mvp!, Corridors.BuildRoad);
        var turns = Corridors.TurnsToBuild(C, bridge.State, a, terms.Turns, 0);
        var button = box.Q<Button>($"corridor-build-{a}");
        Assert.AreEqual(En.Format("corridor.build_road", En.Format("site.group", 1), PlayerText.Money(C, "treasury", terms.Cost), GddHexPanel.Turns(En, turns)), button.text);
        StringAssert.Contains(En.Format("site.group", 2), box.Q<Button>($"corridor-build-{b}").text, "every idle group has its button");
        UiTestKit.Press(button);
        var order = bridge.QueuedLike(new StopWork(a)) as PioneerWork;
        Assert.AreEqual((Corridors.BuildRoad, "burma_road:1"), (order?.Action, order?.Site), "the order goes through the bridge's corridor:N key");
        box = SegmentBox(ui, "burma_road", 1);
        Assert.AreEqual(En["site.ordered"], box.Q<Button>($"corridor-build-{a}").text);
        Assert.IsFalse(box.Q<Button>($"corridor-build-{b}").enabledSelf, "one group builds a segment: the sim would refuse a second order");
        var result = bridge.EndTurn();
        Assert.IsEmpty(result.Rejected, "the sim takes the order");
        Assert.AreEqual(a, result.State.Corridors["burma_road"].Segments[0]?.Unit);
    }

    [Test]
    public void AHexOnARailwayOffersBuildRailwayAndShowsEverySegmentThroughIt()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var bridge = Bridge(En, s => a = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        var (q, r) = SegmentHex("tlv_haifa_rail", 1, 1);
        Assert.AreEqual((q, r), SegmentHex("tlv_netanya_road", 2), "precondition: the rail link and the Netanya road share the hex");
        panel.Show(q, r);
        Assert.IsNotNull(SegmentBox(ui, "tlv_netanya_road", 1));
        Assert.IsNotNull(SegmentBox(ui, "tlv_netanya_road", 2));
        var rail = SegmentBox(ui, "tlv_haifa_rail", 1);
        var terms = Projects.Terms(C.RulesAt(bridge.State.Date).Mvp!, Corridors.BuildRail);
        Assert.AreEqual(En.Format("corridor.build_rail", En.Format("site.group", 1), PlayerText.Money(C, "treasury", terms.Cost),
            GddHexPanel.Turns(En, Corridors.TurnsToBuild(C, bridge.State, a, terms.Turns, 0))), rail.Q<Button>($"corridor-build-{a}").text);
        UiTestKit.Press(rail.Q<Button>($"corridor-build-{a}"));
        var order = bridge.QueuedLike(new StopWork(a)) as PioneerWork;
        Assert.AreEqual((Corridors.BuildRail, "tlv_haifa_rail:1"), (order?.Action, order?.Site));
        Assert.IsEmpty(bridge.EndTurn().Rejected, "the sim takes the railway order");
    }

    [TestCase(0d)]
    [TestCase(0.75d)]
    public void ASegmentBeingBuiltShowsTheSimsTurnsLeftAndStop(double carry)
    {
        using var ui = new UiTestKit();
        int vet = -1;
        var bridge = Bridge(En, s => { vet = AddHalutz(s, Projects.VeteranType); s.Corridors["burma_road"] = new CorridorWork("burma_road", new SegmentWork?[] { new SegmentWork(vet, 2, null, carry), null, null, null }); });
        using var panel = Panel(ui, bridge, En);
        var (q, r) = SegmentHex("burma_road", 1);
        panel.Show(q, r);
        var box = SegmentBox(ui, "burma_road", 1);
        var left = Projects.Terms(C.RulesAt(bridge.State.Date).Mvp!, Corridors.BuildRoad).Turns - 2;
        Assert.AreEqual(En.Format("site.building", En.Format("site.group", 1), GddHexPanel.Turns(En, Corridors.TurnsToBuild(C, bridge.State, vet, left, carry))), box.Q<Label>("corridor-state").text);
        Assert.IsNull(box.Q<Button>($"corridor-build-{vet}"), "a segment being built takes no Build order (the sim refuses it)");
        UiTestKit.Press(box.Q<Button>("corridor-stop"));
        Assert.IsNotNull(bridge.QueuedLike(new StopWork(vet)) as StopWork, "Stop queues StopWork");
    }

    [Test]
    public void ABuiltSegmentShowsItsDateAndTakesNoOrder()
    {
        using var ui = new UiTestKit();
        int a = -1;
        var on = new DateTime(1948, 9, 1);
        var bridge = Bridge(He, s => { a = AddHalutz(s); s.Corridors["burma_road"] = new CorridorWork("burma_road", new SegmentWork?[] { new SegmentWork(null, 6, on), null, null, null }); });
        using var panel = Panel(ui, bridge, He);
        var (q, r) = SegmentHex("burma_road", 1);
        panel.Show(q, r);
        var box = SegmentBox(ui, "burma_road", 1);
        Assert.AreEqual(He.Format("corridor.segment", C.Corridors.Single(x => x.Id == "burma_road").NameHe, 1), box.Q<Label>("corridor-name").text, "the Hebrew name");
        Assert.AreEqual(He.Format("corridor.built", He.Format("date.day_month_year", 1, He["date.month_9"], 1948)), box.Q<Label>("corridor-state").text);
        Assert.IsNull(box.Q<Button>($"corridor-build-{a}"), "a built segment takes no Build order (the sim refuses it)");
    }

    [Test]
    public void AGroupBuildingASegmentIsNotOfferedAnother()
    {
        using var ui = new UiTestKit();
        int busy = -1, idle = -1;
        var bridge = Bridge(En, s => { busy = AddHalutz(s); idle = AddHalutz(s); s.Corridors["burma_road"] = new CorridorWork("burma_road", new SegmentWork?[] { null, null, null, new SegmentWork(busy, 1, null) }); });
        using var panel = Panel(ui, bridge, En);
        var (q, r) = SegmentHex("burma_road", 1);
        panel.Show(q, r);
        var box = SegmentBox(ui, "burma_road", 1);
        Assert.IsNull(box.Q<Button>($"corridor-build-{busy}"), "a group with a project is not idle (D-D6)");
        Assert.IsNotNull(box.Q<Button>($"corridor-build-{idle}"));
    }

    [Test]
    public void EverySegmentIsOfferedOnAHexWithoutACity()
    {
        // A city's own hex already fills the panel's GDD column (InfoPanelLayoutTests), so a segment shows on its other hexes.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(C.Corridors.Any(d => d.Segments.Any(x => x.Hexes.Any(h => (h[0], h[1]) == CityHex))), "precondition: a corridor starts on Tel Aviv's hex");
        Assert.IsNull(ui.Root.Query<VisualElement>().Where(x => x.name != null && x.name.StartsWith("corridor-part-")).First(), "no corridor part on a city's hex");
        foreach (var d in C.Corridors)
            for (int n = 1; n <= d.Segments.Count; n++)
            {
                var found = false;
                foreach (var h in d.Segments[n - 1].Hexes)
                {
                    panel.Show(h[0], h[1]);
                    if (SegmentBox(ui, d.Id, n)?.Q<Button>() != null) { found = true; break; }
                }
                Assert.IsTrue(found, $"{d.Id}:{n} has a hex that offers Build");
            }
    }

    // ---- #1511: Return and Reassign (the sim's orders from #1523; the buttons only queue them) ----

    static Action<GameState> Qualify(int craftsmen, int veterans, params int[] halutzim) =>
        s => { var x = s.Cities[City]; s.Cities[City] = x.WithQualified(new Qualified(craftsmen, veterans, halutzim)).WithCounts(x.Civilians, x.Employed, x.Refugees); };

    [Test]
    public void ReturnCraftsmenQueuesOneMoreGroupAPressUpToTheCitysCraftsmen()
    {
        foreach (var t in new[] { En, He })
        {
            using var ui = new UiTestKit();
            var bridge = Bridge(t, s => s.Cities[City] = s.Cities[City].WithCraftsmen(2));
            using var panel = Panel(ui, bridge, t);
            panel.Show(CityHex.q, CityHex.r);
            var button = ui.Q<Button>("city-return-craftsmen");
            Assert.AreEqual(t["city.return_craftsmen"], button.text);
            UiTestKit.Press(button); UiTestKit.Press(ui.Q<Button>("city-return-craftsmen"));
            Assert.AreEqual(2, (bridge.QueuedLike(new ReturnCraftsmen(City, 1)) as ReturnCraftsmen)?.Count, "two presses, two groups, one order");
            Assert.AreEqual(t.Format("city.train_ordered", 2), ui.Q<Label>("city-return-craftsmen-ordered").text);
            Assert.IsFalse(ui.Q<Button>("city-return-craftsmen").enabledSelf, "no more than the city's two craftsmen");
        }
        using var none = new UiTestKit();
        using var bare = Panel(none, Bridge(En), En);
        bare.Show(CityHex.q, CityHex.r);
        Assert.IsNull(none.Root.Q<Button>("city-return-craftsmen"), "no craftsmen, no Return");
    }

    [Test]
    public void ReassignOffersEachQualifiedRoleWithItsCountAndQueuesTheSimsOrder()
    {
        foreach (var t in new[] { En, He })
        {
            using var ui = new UiTestKit();
            var bridge = Bridge(t, Qualify(2, 1, 3, 5));
            using var panel = Panel(ui, bridge, t);
            panel.Show(CityHex.q, CityHex.r);
            Assert.AreEqual(t.Format("city.reassign_craftsmen", 2), ui.Q<Button>("city-reassign-craftsmen").text);
            Assert.AreEqual(t.Format("city.reassign_halutz", 2), ui.Q<Button>("city-reassign-halutz").text);
            Assert.AreEqual(t.Format("city.reassign_veteran_halutz", 1), ui.Q<Button>("city-reassign-veteran_halutz").text);
            UiTestKit.Press(ui.Q<Button>("city-reassign-craftsmen"));
            UiTestKit.Press(ui.Q<Button>("city-reassign-halutz"));
            UiTestKit.Press(ui.Q<Button>("city-reassign-veteran_halutz"));
            Assert.AreEqual(1, (bridge.QueuedLike(new ReassignCraftsmen(City, 1)) as ReassignCraftsmen)?.Count);
            Assert.AreEqual(1, (bridge.QueuedLike(new ReassignPioneers(City, "halutz", 1)) as ReassignPioneers)?.Count);
            Assert.AreEqual(1, (bridge.QueuedLike(new ReassignPioneers(City, "veteran_halutz", 1)) as ReassignPioneers)?.Count);
            Assert.IsFalse(ui.Q<Button>("city-reassign-veteran_halutz").enabledSelf, "the only qualified veteran is ordered");
            Assert.IsTrue(ui.Q<Button>("city-reassign-halutz").enabledSelf, "a second qualified halutz group is left");
        }
    }

    [Test]
    public void ReassignIsDisabledWhenNoCivilianIsWithoutWork()
    {
        // The sim refuses a reassignment beyond the civilians without work (R-QUAL-004), so the button is off, and a queued
        // reassignment takes one of them for the next press.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { Counts(50, 50)(s); Qualify(2, 0)(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsFalse(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "every civilian is in work");

        using var one = new UiTestKit();
        var b2 = Bridge(En, s => { Counts(50, 49)(s); Qualify(2, 0)(s); });
        using var p2 = Panel(one, b2, En);
        p2.Show(CityHex.q, CityHex.r);
        UiTestKit.Press(one.Q<Button>("city-reassign-craftsmen"));
        Assert.IsFalse(one.Q<Button>("city-reassign-craftsmen").enabledSelf, "one civilian without work, one group ordered");
    }

    [Test]
    public void ReassignCountsThisTurnsTrainOrdersAgainstTheCiviliansWithoutWork()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { Counts(50, 49)(s); Qualify(2, 0)(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "precondition: one civilian without work");
        UiTestKit.Press(ui.Q<Button>("city-train-recruit"));
        Assert.IsFalse(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "the trainee took it; the sim would refuse the reassignment");
    }

    [Test]
    public void AQueuedReassignmentIsCountedOnceAgainstTheCiviliansWithoutWork()
    {
        // #1552: Training.Available already takes a queued reassignment off the idle civilians, so the panel must not take it
        // off again. Two civilians without work: one craftsman group ordered leaves one for the halutz group and for a Train.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { Counts(50, 48)(s); Qualify(2, 0, 3)(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        UiTestKit.Press(ui.Q<Button>("city-reassign-craftsmen"));
        Assert.IsTrue(ui.Q<Button>("city-reassign-halutz").enabledSelf, "one civilian without work is left for the halutz group");
        Assert.IsTrue(ui.Q<Button>("city-train-recruit").enabledSelf, "and for a Train");
        UiTestKit.Press(ui.Q<Button>("city-reassign-halutz"));
        Assert.IsFalse(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "both civilians are ordered");
        Assert.IsFalse(ui.Q<Button>("city-train-recruit").enabledSelf, "none is left to train");
    }

    [Test]
    public void AQueuedReassignmentDoesNotCostTheCityItsOwnQualifications()
    {
        // Training.Ordered counts the reassignments too, but a reassignment leaves the civilians where they are, so only a
        // Train or Reinforce can cost qualifications (R-QUAL-006). 3 civilians, all qualified: ordering the halutz group
        // must leave the craftsman reassignable.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { Counts(3, 0)(s); Qualify(1, 0, 3, 5)(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        UiTestKit.Press(ui.Q<Button>("city-reassign-halutz"));
        Assert.IsTrue(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "the craftsman is still qualified");
    }

    [Test]
    public void AGroupWithABuildOrderedCannotBeReturnedToo()
    {
        using var ui = new UiTestKit();
        int idle = 0;
        var bridge = Bridge(En, s => idle = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        UiTestKit.Press(ui.Q<Button>($"site-build-{idle}"));
        Assert.IsFalse(ui.Q<Button>($"pioneer-return-{idle}").enabledSelf);
    }

    [Test]
    public void AReturnedGroupLeavesTheSiteListAtOnce()
    {
        // R-TURN-004: Return is a unit order, so it takes effect at once; the group is a civilian again and offers no Build.
        using var ui = new UiTestKit();
        int idle = 0;
        var bridge = Bridge(En, s => idle = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.IsTrue(ui.Q<Button>($"site-build-{idle}").enabledSelf, "precondition: an idle group can build");
        UiTestKit.Press(ui.Q<Button>($"pioneer-return-{idle}"));
        Assert.IsFalse(bridge.State.Units.ContainsKey(idle));
        Assert.IsNull(ui.Root.Q<Button>($"site-build-{idle}"));
        Assert.IsNull(ui.Root.Q<Button>($"pioneer-return-{idle}"));
    }

    [Test]
    public void AReturnedCorridorGroupLeavesTheSegmentAtOnce()
    {
        using var ui = new UiTestKit();
        int idle = 0;
        var bridge = Bridge(En, s => idle = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        var (q, r) = SegmentHex("burma_road", 1);
        panel.Show(q, r);
        var box = SegmentBox(ui, "burma_road", 1);
        Assert.IsTrue(box.Q<Button>($"corridor-build-{idle}").enabledSelf, "precondition: an idle group can build");
        UiTestKit.Press(box.Q<Button>($"pioneer-return-{idle}"));
        Assert.IsFalse(bridge.State.Units.ContainsKey(idle));
        Assert.IsNull(SegmentBox(ui, "burma_road", 1).Q<Button>($"corridor-build-{idle}"));
    }

    [Test]
    public void ReassignCountsTheQualificationAQueuedTrainTakesFirst()
    {
        // R-QUAL-006: a Train that leaves fewer civilians than the city has qualified costs craftsmen first. 3 civilians, all
        // qualified (1 craftsman, 2 halutz groups): after Train recruits the craftsman is gone when Reassign would run.
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { Counts(3, 0)(s); Qualify(1, 0, 3, 5)(s); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "precondition: a craftsman can be reassigned");
        UiTestKit.Press(ui.Q<Button>("city-train-recruit"));
        Assert.IsFalse(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "the sim would refuse it: no qualified craftsman is left");
        Assert.IsTrue(ui.Q<Button>("city-reassign-halutz").enabledSelf, "the halutz groups are still qualified");
    }

    [Test]
    public void AnIdlePioneerGroupOnTheCityHexOffersReturnAndTheOrderIsTheSims()
    {
        foreach (var t in new[] { En, He })
        {
            using var ui = new UiTestKit();
            int idle = 0, busy = 0;
            var other = C.Sites.First(x => Projects.IsYishuvSite(x) && x.Id != Site).Id;
            var bridge = Bridge(t, s => { busy = AddHalutz(s); idle = AddHalutz(s); s.Sites[other] = new SiteWork(other, busy, 3, null); });
            using var panel = Panel(ui, bridge, t);
            panel.Show(CityHex.q, CityHex.r);
            Assert.IsNull(ui.Root.Q<Button>($"pioneer-return-{busy}"), "a group at work must stop first (Qualification.CheckReturn)");
            var button = ui.Q<Button>($"pioneer-return-{idle}");
            Assert.AreEqual(t.Format("pioneer.return", GddHexPanel.Group(t, bridge.State, idle)), button.text);
            UiTestKit.Press(button);
            // R-TURN-004: a unit order is committed at once, not queued.
            var order = bridge.Session.Committed.OfType<UnitOrder>().Single();
            Assert.AreEqual((idle, Qualification.ReturnOrder), (order.Unit, order.Order));
            Assert.IsEmpty(bridge.Queued);
            Assert.IsNull(ui.Root.Q<Button>($"pioneer-return-{idle}"), "the group has returned");
        }
    }

    [Test]
    public void TheSiteListOffersReturnBesideBuildForAnIdleGroup()
    {
        using var ui = new UiTestKit();
        int idle = 0;
        var bridge = Bridge(En, s => idle = AddHalutz(s));
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.IsNotNull(ui.Root.Q<Button>($"site-build-{idle}"), "precondition: the group is offered Build");
        Assert.IsNotNull(ui.Root.Q<Button>($"pioneer-return-{idle}"));
    }
    [TestCase(false)] [TestCase(true)]
    public void EverySettlementBlockNamesItsOwnCity(bool hebrew)
    {
        var text = hebrew ? He : En; var bridge = Bridge(text);
        var pair = C.SettlementHexes.Where(h => bridge.State.Cities.ContainsKey(h.Id)).GroupBy(h => (h.Q, h.R)).First(g => g.Count() > 1);
        var host = new VisualElement(); var projects = new VisualElement();
        GddHexPanel.Render(host, projects, bridge, text, pair.Key.Q, pair.Key.R, id => "name:" + id);
        foreach (var h in pair)
        {
            var part = host.Q("city-part-" + h.Id);
            Assert.IsNotNull(part, "each settlement owns a distinct block: " + h.Id);
            Assert.AreEqual("name:" + h.Id, part!.Q<Label>("city-name").text);
            Assert.IsNotNull(part!.Q<Button>("city-workers-less"), "the name belongs to its own jobs stepper");
        }
        var single = C.SettlementHexes.Where(h => bridge.State.Cities.ContainsKey(h.Id)).GroupBy(h => (h.Q, h.R)).First(g => g.Count() == 1);
        GddHexPanel.Render(host, projects, bridge, text, single.Key.Q, single.Key.R, id => "name:" + id);
        Assert.IsNull(host.Q<Label>("city-name"), "one city's block is not named again under the panel's title");
    }

    [TestCase(false)] [TestCase(true)]
    public void WorkerChangesNameTheirTimingAndActualAttentionPrice(bool hebrew)
    {
        var text = hebrew ? He : En; var bridge = Bridge(text, s => s.Rung = 2);   // a fractional price (0.625)
        using var ui = new UiTestKit(); using var panel = Panel(ui, bridge, text);
        panel.Show(CityHex.q, CityHex.r);
        var city = bridge.State.Cities["tel_aviv"]; int before = city.Employed;
        Assert.Greater(before, 0, "precondition: there is a worker to remove");
        Assert.IsNotNull(ui.Q<Label>("city-workers-cost"), "the order price and timing are visible, not only in a tooltip");
        Assert.AreEqual(text.Format("city.workers_cost", bridge.Price(new AssignWorkers("tel_aviv", "job", before))), ui.Q<Label>("city-workers-cost").text);
        UiTestKit.Press(ui.Q<Button>("city-workers-less"));
        Assert.AreEqual(before, city.Employed, "queued work does not pretend the simulation already ran");
        Assert.AreEqual(text.Format("city.workers_ordered", GddHexPanel.Groups(text, before - 1)), ui.Q<Label>("city-workers").text);
        Assert.AreEqual(text.Format("city.jobs", before, city.JobSlots), ui.Q<Label>("city-jobs").text);
        StringAssert.Contains("0.6", ui.Q<Label>("city-workers-cost").text, "the price as the HUD and the cards show Attention (one decimal)");
        double left = bridge.AttentionLeft;
        if (before > 1)
        {
            UiTestKit.Press(ui.Q<Button>("city-workers-less"));
            Assert.AreEqual(left, bridge.AttentionLeft, 1e-9, "adjusting one queued assignment does not buy another order");
            UiTestKit.Press(ui.Q<Button>("city-workers-more"));
        }
        UiTestKit.Press(ui.Q<Button>("city-workers-more"));
        Assert.IsNull(bridge.QueuedLike(new AssignWorkers("tel_aviv", "job", 0)), "return to current count cancels the queued order");
        Assert.AreEqual(text.Format("city.workers", GddHexPanel.Groups(text, before)), ui.Q<Label>("city-workers").text);
    }

}
