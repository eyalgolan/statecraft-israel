#nullable enable
using System;
using System.Collections.Generic;
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

/// <summary>#1122: city projects in the info panel (R-PROJ-001 to R-PROJ-003), on a real SimBridge with the real content. On a
/// hex a city can build on (Projects.CitiesFor), the city that builds (the player picks when several can) and Build for each
/// loaded kind, with the content's cost and turns; the order goes into the bridge's queue for that kind, city and hex; a
/// refused kind shows the sim's own reason (Projects.CheckCityReason) in English and Hebrew in place of its Build; a project under way shows its
/// turns left and Cancel, a finished one its day.</summary>
public class CityProjectPanelTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly Dictionary<string, string> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    // Haifa has hexes no other city reaches; Tel Aviv's own hex is also in Petah Tikva's reach.
    const string City = "haifa", TelAviv = "tel_aviv", Neighbour = "petah_tikva";

    static (int q, int r) TelAvivHex => C.SettlementHexes.Where(x => x.Id == TelAviv).Select(x => (x.Q, x.R)).Single();

    /// <summary>The first hex in (q, r) order whose land flag is <paramref name="land"/> and whose builders are exactly
    /// <paramref name="cities"/>, by the sim's own helper: the panel's preconditions, not a second range rule.</summary>
    static (int q, int r) HexWith(bool land, params string[] cities)
    {
        var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;
        var hex = C.Hexes.Where(h => h.Land == land && Projects.CitiesFor(C, s, h.Q, h.R).SequenceEqual(cities))
            .OrderBy(h => h.Q).ThenBy(h => h.R).First();
        return (hex.Q, hex.R);
    }

    static (int q, int r) Lone => HexWith(true, City);

    static SimBridge Bridge(UiText text, Action<GameState>? edit = null)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        Assert.IsTrue(C.IsGdd, "precondition: the real content runs the GDD loop");
        edit?.Invoke(g.State);
        return new SimBridge(g, text);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge, UiText text) => new InfoPanelPresenter(ui.Root, bridge, text, Towns);

    static string Name(UiText t, string kind) => ContentNames.Of(t, C.ProjectKinds.Single(k => k.Id == kind));

    static string Start(UiText t, ProjectKind k, GameState s) =>
        t.Format("city.project_start", ContentNames.Of(t, k), PlayerText.Money(C, "treasury", k.Cost), GddHexPanel.Turns(t, Projects.CityTurnsToBuild(C, s, k.Id, k.Turns, 0)));

    [Test]
    public void EveryLoadedKindIsOfferedWithItsContentCostAndTurns([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        var (q, r) = Lone;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s => s.Policies[MapActions.MaabarotPolicy] = 1);   // so the sim refuses no kind here
        using var panel = Panel(ui, bridge, t);
        panel.Show(q, r);
        Assert.AreEqual(t.Format("city.project_for", Towns[City]), ui.Q<Label>("city-project-for").text);
        Assert.IsNull(ui.Root.Q("city-project-cities"), "one city in reach: nothing to pick");
        Assert.GreaterOrEqual(C.ProjectKinds.Count, 3, "precondition: housing, the transit camp and industry");
        foreach (var k in C.ProjectKinds)
        {
            // A kind that needs a resource Haifa lacks (the winery's vineyards, R-PROJ-005) shows the sim's reason instead.
            if (Projects.CheckCityReason(C, bridge.State, k.Id, City, q, r)?.Kind == "no_resource") { Assert.IsNotNull(ui.Root.Q<Label>($"city-project-{k.Id}-reason"), k.Id); continue; }
            Assert.AreEqual(Start(t, k, bridge.State), ui.Q<Button>("city-project-" + k.Id).text, k.Id);
        }
        // Draft Hebrew names (owner ruling 3), the English name where a kind has none.
        Assert.AreEqual(hebrew ? "מיזם דיור" : "a housing project", Name(t, "housing"));
        Assert.AreEqual("a depot", ContentNames.Of(t, new ProjectKind { Id = "depot", Name = "a depot" }));
    }

    [Test]
    public void BuildQueuesTheKindForTheCityOnTheHexAndAnotherKindReplacesIt()
    {
        var (q, r) = Lone;
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(q, r);
        Assert.IsTrue(ui.Q<Button>("city-project-housing").enabledSelf, "precondition: housing may be started");
        ui.Press("city-project-housing");
        var order = (StartProject)bridge.Queued.Single();
        Assert.AreEqual(("housing", City, q, r), (order.ProjectKind, order.City, order.Q, order.R));
        var housing = ui.Q<Button>("city-project-housing");
        Assert.AreEqual(En.Format("city.project_ordered", Name(En, "housing")), housing.text);
        Assert.IsFalse(housing.enabledSelf, "ordered: pressing again would change nothing");

        ui.Press("city-project-industry");   // one order a hex (SimBridge's key): the other kind replaces it
        order = (StartProject)bridge.Queued.Single();
        Assert.AreEqual(("industry", City, q, r), (order.ProjectKind, order.City, order.Q, order.R));
        Assert.AreEqual(Start(En, C.ProjectKinds.Single(k => k.Id == "housing"), bridge.State), ui.Q<Button>("city-project-housing").text);
        Assert.IsTrue(ui.Q<Button>("city-project-housing").enabledSelf);
        Assert.AreEqual(1, bridge.EndTurn().State.CityProjects.Values.Count(p => p.Kind == "industry" && p.City == City && p.Q == q && p.R == r),
            "the sim accepts the order the panel queued");
    }

    [Test]
    public void WhenSeveralCitiesCanBuildThePlayerPicksAndTheFocusStaysInTheRow()
    {
        var (q, r) = TelAvivHex;
        Assert.AreEqual(new[] { TelAviv, Neighbour }, Projects.CitiesFor(C, Bridge(En).State, q, r), "precondition: Tel Aviv's own hex, and Petah Tikva next to it");
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(q, r);
        Assert.AreEqual(En.Format("city.project_for", Towns[TelAviv]), ui.Q<Label>("city-project-for").text, "the nearest first");
        Assert.IsFalse(ui.Q<Button>("city-project-city-" + TelAviv).enabledSelf);
        var other = ui.Q<Button>("city-project-city-" + Neighbour);
        Assert.AreEqual(Towns[Neighbour], other.text);
        Assert.IsTrue(other.canGrabFocus, "keyboard: the pick is a focusable button");
        other.Focus();
        UiTestKit.Press(other);
        Assert.AreEqual(En.Format("city.project_for", Towns[Neighbour]), ui.Q<Label>("city-project-for").text);
        Assert.AreEqual("city-project-city-" + TelAviv, (ui.Root.focusController.focusedElement as VisualElement)?.name, "the focus stays in the row");
        Assert.IsEmpty(bridge.Queued, "picking a city orders nothing");
        ui.Press("city-project-industry");
        var order = (StartProject)bridge.Queued.Single();
        Assert.AreEqual(("industry", Neighbour, q, r), (order.ProjectKind, order.City, order.Q, order.R));
        Assert.AreEqual(En.Format("city.project_for", Towns[Neighbour]), ui.Q<Label>("city-project-for").text, "the pick holds after the order");

        panel.Show(Lone.q, Lone.r);   // the pick is for its hex only
        Assert.AreEqual(En.Format("city.project_for", Towns[City]), ui.Q<Label>("city-project-for").text);
    }

    [Test]
    public void AProjectUnderWayShowsItsTurnsLeftAndCancelQueuesCancelProject([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        var (q, r) = Lone;
        int id = -1;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s => { id = s.NextId++; s.CityProjects.Add(id, new CityProject(id, "housing", City, q, r, 4)); });
        using var panel = Panel(ui, bridge, t);
        panel.Show(q, r);
        Assert.AreEqual(t.Format("city.project_building", PlayerText.Capitalised(Name(t, "housing")), Towns[City], GddHexPanel.Turns(t, Projects.CityTurnsToBuild(C, bridge.State, "housing", 4, 0))), ui.Q<Label>("city-project-state").text);
        Assert.IsNull(ui.Root.Q("city-project-housing"), "the hex is taken: no Build");
        ui.Press("city-project-cancel");
        Assert.AreEqual(id, ((CancelProject)bridge.Queued.Single()).Project);
        var cancel = ui.Q<Button>("city-project-cancel");
        Assert.AreEqual(t["city.cancel_ordered"], cancel.text);
        Assert.IsFalse(cancel.enabledSelf);
        Assert.IsFalse(bridge.EndTurn().State.CityProjects.ContainsKey(id), "the sim cancels it");
    }

    // #1150 (owner review): since #1147 the minister multiplier scales a city project's progress, so the turns on Build and
    // under way are the sim's forecast (Projects.CityTurnsToBuild), not the kind's base turns or the raw turns left.
    [Test]
    public void TheTurnsAreTheSimsForecastAtTheMinistersMultiplier([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        var (q, r) = Lone;
        var housing = C.ProjectKinds.Single(k => k.Id == "housing");
        // Labour's budget line unfunded: its factor at the 0.5 floor, so the multiplier is not 1.
        void Unfund(GameState s) => s.Budget[Offices.BudgetLineOf(C, "labour_minister", s.Date)!] = 0;
        using (var ui = new UiTestKit())
        {
            var bridge = Bridge(t, Unfund);
            var turns = Projects.CityTurnsToBuild(C, bridge.State, "housing", housing.Turns, 0);
            Assert.AreNotEqual(housing.Turns, turns, "precondition: the multiplier is not 1");
            using var panel = Panel(ui, bridge, t);
            panel.Show(q, r);
            Assert.AreEqual(t.Format("city.project_start", Name(t, "housing"), PlayerText.Money(C, "treasury", housing.Cost), GddHexPanel.Turns(t, turns)),
                ui.Q<Button>("city-project-housing").text);
        }
        using (var ui = new UiTestKit())
        {
            var bridge = Bridge(t, s => { Unfund(s); var id = s.NextId++; s.CityProjects.Add(id, new CityProject(id, "housing", City, q, r, 4).WithProgress(4, 0.5)); });
            var left = Projects.CityTurnsToBuild(C, bridge.State, "housing", 4, 0.5);
            Assert.AreNotEqual(4, left, "precondition: the multiplier is not 1");
            using var panel = Panel(ui, bridge, t);
            panel.Show(q, r);
            Assert.AreEqual(t.Format("city.project_building", PlayerText.Capitalised(Name(t, "housing")), Towns[City], GddHexPanel.Turns(t, left)),
                ui.Q<Label>("city-project-state").text);
        }
    }

    [Test]
    public void AFinishedProjectShowsItsDayAndNoCancel()
    {
        var (q, r) = Lone;
        var day = new DateTime(1948, 9, 30);
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { var id = s.NextId++; s.CityProjects.Add(id, new CityProject(id, "industry", City, q, r, 1).Built(day)); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(q, r);
        Assert.AreEqual(En.Format("city.project_built", "An industry project", Towns[City], En.Format("date.day_month_year", 30, En["date.month_9"], 1948)),
            ui.Q<Label>("city-project-state").text);
        Assert.IsNull(ui.Root.Q("city-project-cancel"));
    }

    /// <summary>Each refusal the panel can meet, from the sim's validator, worded in the UI's language: one kind refused (the
    /// policy, a resource) under its own button; every kind refused for one reason (not land, a placement, the turn's limit)
    /// said once with no buttons.</summary>
    [Test]
    public void TheSimsRefusalsShowInEnglishAndHebrew([Values] bool hebrew, [Values("policy", "resource", "sea", "placement", "turn_cap")] string why)
    {
        var t = hebrew ? He : En;
        var (q, r) = why == "sea" ? HexWith(false, City) : Lone;
        var winery = new ProjectKind { Id = "a_winery", Name = "a winery", Cost = 2, Turns = 2, JobSlots = 6, Requires = "vineyards" };
        if (why == "resource") C.ProjectKinds.Add(winery);
        try
        {
            using var ui = new UiTestKit();
            var bridge = Bridge(t, s =>
            {
                // With the policy the transit camp is refused for the shared reason too, not for the policy first.
                if (why != "policy" && why != "resource") s.Policies[MapActions.MaabarotPolicy] = 1;
                if (why == "placement") s.Placements.Add(new Placement(MapActions.Housing, q, r, s.Date));
                if (why == "turn_cap") { s.MapActionsTurnDate = s.Date; s.MapActionsThisTurn = C.RulesAt(s.Date).MapActionsPerTurn; }
            });
            using var panel = Panel(ui, bridge, t);
            panel.Show(q, r);
            var s0 = bridge.State;
            var expected = why switch
            {
                "policy" => t["info.map_action_reason.maabarot_policy"],
                "resource" => t.Format("city.project_reason.no_resource", Towns[City], t["city.resource.vineyards"], ContentNames.Of(t, winery)),
                "sea" => t["info.map_action_reason.not_land"],
                "placement" => t.Format("info.map_action_reason.occupied", t["info.map_action.housing"]),
                _ => t.Format("city.project_reason.turn_cap", C.RulesAt(s0.Date).MapActionsPerTurn),
            };
            var kind = why == "policy" ? MapActions.TransitCamp : why == "resource" ? winery.Id : "housing";
            Assert.IsNotNull(Projects.CheckCityReason(C, s0, kind, City, q, r), "precondition: the sim refuses it");
            if (why == "policy" || why == "resource")
            {
                Assert.AreEqual(expected, ui.Q<Label>($"city-project-{kind}-reason").text);
                Assert.IsNull(ui.Root.Q("city-project-" + kind), "the reason alone, which names the kind");
                Assert.IsTrue(ui.Q<Button>("city-project-industry").enabledSelf, "the other kinds stay open");
                Assert.IsNull(ui.Root.Q("city-project-industry-reason"));
            }
            else
            {
                Assert.AreEqual(expected, ui.Q<Label>("city-project-reason").text);
                Assert.IsNull(ui.Root.Q("city-project-housing"), "refused for all alike: no buttons");
            }
            if (!hebrew && why != "resource")   // the English is the sim's own sentence
                Assert.AreEqual(Projects.CheckCity(C, s0, kind, City, q, r), expected);
        }
        finally { C.ProjectKinds.Remove(winery); }
    }

    [Test]
    public void AnOrderTheAttentionCannotPayIsRefusedByTheBridge()
    {
        var (q, r) = Lone;
        var refusals = new List<string>();
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => s.Attention = 0);
        bridge.Refused += refusals.Add;
        using var panel = Panel(ui, bridge, En);
        panel.Show(q, r);
        var order = new StartProject("housing", City, q, r);
        Assert.IsFalse(bridge.CanPay(order), "precondition: no Attention left");
        ui.Press("city-project-housing");
        Assert.IsEmpty(bridge.Queued);
        Assert.AreEqual(new[] { En.Format("order.no_attention", bridge.Price(order)!.Value, 0.0) }, refusals);
    }

    [Test]
    public void AHexNoCityReachesShowsNoProjects()
    {
        var (q, r) = HexWith(true);
        using var ui = new UiTestKit();
        var bridge = Bridge(En);
        using var panel = Panel(ui, bridge, En);
        panel.Show(q, r);
        Assert.IsNull(ui.Root.Q("city-project-for"));
        Assert.IsNull(ui.Root.Q("city-project-housing"));
    }

    /// <summary>#1781: the Hebrew refusal names the resource and the kind in Hebrew, with no Latin letters left in the
    /// line. Before this, `winery` had no name_he and the resource had no name at all, so a Hebrew player read
    /// "ב־תל אביב אין vineyards עבור a winery". This uses the real content's kind rather than a fixture, because the
    /// point is the shipped wording.</summary>
    [Test]
    public void TheHebrewRefusalForTheRealWineryHasNoEnglishWords()
    {
        var kind = C.ProjectKinds.First(x => x.Id == "winery");
        Assert.AreEqual("vineyards", kind.Requires, "precondition: the winery is the kind that requires a resource");
        Assert.IsTrue(He.Has("city.resource." + kind.Requires), "the required resource is named in the Hebrew strings");
        // The town name comes from the settlement's own name_he and is not this issue's scope, so the line is built
        // with a Hebrew town to isolate the two words that were English: the resource and the kind.
        var line = He.Format("city.project_reason.no_resource", "\u05ea\u05dc \u05d0\u05d1\u05d9\u05d1",
            He["city.resource." + kind.Requires], ContentNames.Of(He, kind));
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(line, "[A-Za-z]"),
            "no Latin letters left in the Hebrew refusal: " + line);
        StringAssert.Contains("\u05db\u05e8\u05de\u05d9\u05dd", line);   // כרמים
        StringAssert.Contains("\u05d9\u05e7\u05d1", line);                 // יקב
    }
}

/// <summary>#1722 slice E (#1701; R-ARMS-005 to R-ARMS-007): a built arms workshop's staffing control in the info panel,
/// gdd-0.3 only, on a real SimBridge with the real content run under gdd-0.3 (its projects.yaml has the arms_workshop row).
/// The stepper is the city's AssignWorkers(city, "workshop", n); every number, limit and refusal is the sim's
/// (Workshops.Built, Workshops.Staffed, Workshops.Check, the Workshop record, Cities.Held, the last turn's causes).</summary>
public class WorkshopPanelTests
{
    static readonly Content C01 = ContentLoader.Load(Paths.Content);
    static readonly Content C03 = Load03();
    static readonly Dictionary<string, string> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
    static readonly Dictionary<string, string> TownsHe =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.NameHe);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    const string City = "haifa";

    /// <summary>The real content under gdd-0.3, as NoAttentionPresentationTests loads it.</summary>
    static Content Load03()
    {
        var dir = Paths.Content;
        return ContentLoader.Load(rel =>
        {
            var path = Path.Combine(dir, rel);
            if (!File.Exists(path)) return null;
            var value = File.ReadAllText(path);
            return rel == "eras.yaml" ? value.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : value;
        }, Directory.GetFiles(Path.Combine(dir, "events"), "*.yaml").Select(f => "events/" + Path.GetFileName(f)));
    }

    /// <summary>A hex only Haifa reaches (CityProjectPanelTests' Lone), by the sim's own helper.</summary>
    static (int q, int r) Hex(Content c)
    {
        var s = GameSession.New(c, GoldenRun.Seed, Mode.Historical).State;
        var hex = c.Hexes.Where(h => h.Land && Projects.CitiesFor(c, s, h.Q, h.R).SequenceEqual(new[] { City })).OrderBy(h => h.Q).ThenBy(h => h.R).First();
        return (hex.Q, hex.R);
    }

    /// <summary>A game of <paramref name="c"/> with a project of <paramref name="kind"/> for Haifa on its hex (built unless
    /// <paramref name="turnsLeft"/> is above 0), and Haifa with 10 civilians, <paramref name="employed"/> of them in the jobs
    /// the player set (the job default off). The project's id is the second value.</summary>
    static (SimBridge Bridge, int Id) Game(Content c, UiText text, string kind = Workshops.ArmsWorkshop, int employed = 6, int turnsLeft = 0,
        Action<GameState, int>? edit = null, Func<GameSession, StepResult?>? last = null)
    {
        var (q, r) = Hex(c);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var s = g.State;
        var x = s.Cities[City];
        s.Cities[City] = new City(x.Id, x.HousingCapacity, 10, 10, employed, 0, 0, x.Condition, jobsDefault: false);
        var id = s.NextId++;
        var p = new CityProject(id, kind, City, q, r, turnsLeft);
        s.CityProjects.Add(id, turnsLeft == 0 ? p.Built(s.Date) : p);
        edit?.Invoke(s, id);
        if (last?.Invoke(g) is StepResult result) typeof(GameSession).GetProperty(nameof(GameSession.Last))!.SetValue(g, result);
        return (new SimBridge(g, text) { TownNamesHe = TownsHe }, id);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge, UiText text) => new InfoPanelPresenter(ui.Root, bridge, text, Towns, townNamesHe: TownsHe);

    static AssignWorkers? Queued(SimBridge b) => b.QueuedLike(new AssignWorkers(City, Workshops.Role, 0)) as AssignWorkers;

    // R-ARMS-005: the control shows on a built arms workshop under gdd-0.3, and nowhere else: not on a workshop being
    // built, not on another built kind, and never under gdd-0.1 (whose content has no workshop kind).
    [Test]
    public void TheControlShowsOnABuiltArmsWorkshopUnderGdd03Only([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        Assert.AreEqual(SaveGame.Gdd03Ruleset, C03.Ruleset, "precondition: the content runs under gdd-0.3");
        Assert.IsTrue(C03.ProjectKinds.Any(k => k.Id == Workshops.ArmsWorkshop), "precondition: gdd-0.3 has the workshop kind");
        Assert.IsFalse(C01.ProjectKinds.Any(k => k.Id == Workshops.ArmsWorkshop), "precondition: gdd-0.1 has none");
        foreach (var (c, kind, turns, shown) in new[]
        {
            (C03, Workshops.ArmsWorkshop, 0, true),
            (C03, Workshops.ArmsWorkshop, 2, false),
            (C03, "industry", 0, false),
            (C01, "industry", 0, false),
        })
        {
            using var ui = new UiTestKit();
            var (bridge, _) = Game(c, t, kind, turnsLeft: turns);
            using var panel = Panel(ui, bridge, t);
            var (q, r) = Hex(c);
            panel.Show(q, r);
            Assert.IsNotNull(ui.Root.Q("city-project-state"), $"{c.Ruleset} {kind} {turns}: precondition, the project's part shows");
            Assert.AreEqual(shown, ui.Root.Q("city-workshops-row") != null, $"{c.Ruleset} {kind} {turns}");
            Assert.AreEqual(shown, ui.Root.Q("city-workshop-state") != null, $"{c.Ruleset} {kind} {turns}");
            if (!shown) continue;
            Assert.AreEqual(t.Format("city.workshops_staffed", 0, 1), ui.Q<Label>("city-workshops").text, "the sim's counts: none staffed of 1 built");
            Assert.AreEqual(t["city.workshop_unstaffed"], ui.Q<Label>("city-workshop-state").text);
            Assert.IsFalse(ui.Q<Button>("city-workshops-less").enabledSelf, "none staffed: nothing to unstaff");
            Assert.IsTrue(ui.Q<Button>("city-workshops-more").enabledSelf, "4 people without a job: one may staff it");
            Assert.IsNull(ui.Root.Q("city-workshops-reason"));
        }
    }

    // R-ARMS-005: + queues AssignWorkers(city, "workshop", n + 1) for the workshop's city, the line reads the ordered count,
    // and - back to the current count withdraws the order (as the jobs stepper does). The sim accepts the queued order.
    [Test]
    public void TheStepperQueuesTheWorkshopRoleAndStepsBackToNoOrder()
    {
        using var ui = new UiTestKit();
        var (bridge, id) = Game(C03, En);
        using var panel = Panel(ui, bridge, En);
        var (q, r) = Hex(C03);
        panel.Show(q, r);
        ui.Press("city-workshops-more");
        var order = Queued(bridge);
        Assert.IsNotNull(order, "the stepper queues an AssignWorkers order");
        Assert.AreEqual((City, Workshops.Role, 1), (order!.City, order.Role, order.Count));
        Assert.AreEqual(En.Format("city.workshops_ordered", 1, 1), ui.Q<Label>("city-workshops").text);
        Assert.IsFalse(ui.Q<Button>("city-workshops-more").enabledSelf, "its one workshop is ordered staffed");
        Assert.IsNull(ui.Root.Q("city-workshops-reason"), "every workshop is staffed: no refusal to explain");
        ui.Press("city-workshops-less");
        Assert.IsNull(Queued(bridge), "back at the current count: no order is left queued");
        Assert.AreEqual(En.Format("city.workshops_staffed", 0, 1), ui.Q<Label>("city-workshops").text);
        ui.Press("city-workshops-more");
        Assert.IsTrue(bridge.EndTurn().State.Workshops[id].Staffed, "the sim accepts the order the panel queued");
    }

    // #1722 (2 Nov note, 1): a jobs order and a workshop order for the same city keep their own slots in the queue, so
    // neither replaces the other; and (2) Training.Available, the Train buttons' limit, counts the pending staffing.
    [Test]
    public void AJobsOrderAndAStaffingOrderForOneCityAreBothQueuedAndTrainingCountsIt()
    {
        using var ui = new UiTestKit();
        var (bridge, _) = Game(C03, En);
        using var panel = Panel(ui, bridge, En);
        var (q, r) = Hex(C03);
        Assert.IsNull(bridge.Queue(new AssignWorkers(City, "job", 7)));
        var before = Training.Available(bridge.State, City, bridge.Queued);
        panel.Show(q, r);
        ui.Press("city-workshops-more");
        Assert.AreEqual(2, bridge.Queued.OfType<AssignWorkers>().Count(w => w.City == City), "the staffing order did not replace the jobs order");
        Assert.AreEqual(7, ((AssignWorkers)bridge.QueuedLike(new AssignWorkers(City, "job", 0))!).Count);
        Assert.AreEqual(1, Queued(bridge)!.Count);
        Assert.AreEqual(before - 1, Training.Available(bridge.State, City, bridge.Queued), "the pending workshop worker is not offered for training");
    }

    // R-ARMS-005: with nobody left for it, + is off and the panel shows the sim's own refusal (Workshops.Check with this
    // turn's queue), in the UI's language: SimBridge.ReasonText, as the end-of-turn toast words it.
    [Test]
    public void WithNobodyFreeTheSimsRefusalShowsInBothLanguages([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        using var ui = new UiTestKit();
        var (bridge, _) = Game(C03, t, employed: 10);
        using var panel = Panel(ui, bridge, t);
        var (q, r) = Hex(C03);
        panel.Show(q, r);
        var reason = Workshops.Check(C03, bridge.State, City, 1, bridge.Queued);
        Assert.AreEqual(string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedPeopleNoJob, PlayerText.Settlement(C03, City), 0), reason,
            "precondition: the sim refuses one more");
        Assert.IsFalse(ui.Q<Button>("city-workshops-more").enabledSelf);
        var line = ui.Q<Label>("city-workshops-reason").text;
        Assert.AreEqual(SimBridge.ReasonText(t, reason!, C03, TownsHe), line);
        if (hebrew)
        {
            Assert.AreNotEqual(reason, line, "the Hebrew string, not the sim's English");
            StringAssert.Contains(TownsHe[City], line);
        }
        else Assert.AreEqual("Haifa has 0 civilians and craftsmen without a job", line);
    }

    // R-ARMS-005: each staffing refusal has a string in both languages, and the English is the sim's own words.
    [Test]
    public void EveryStaffingRefusalHasAStringInBothLanguages()
    {
        foreach (var reason in new[]
        {
            string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedBuiltOne, "Haifa"),
            string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedBuilt, "Haifa", 2),
            string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedPeople, "Haifa", 0),
            string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedPeopleNoJob, "Haifa", 0),
        })
        {
            Assert.AreEqual(reason, SimBridge.ReasonText(En, reason, C03, TownsHe), "the English string is the sim's own");
            var he = SimBridge.ReasonText(He, reason, C03, TownsHe);
            Assert.AreNotEqual(reason, he, "no Hebrew string for: " + reason);
            StringAssert.IsMatch("[א-ת]", he, reason);
            StringAssert.Contains(TownsHe[City], he, "the town in Hebrew");
        }
    }

    // R-ARMS-006, R-ARMS-007, R-CITY-010: a staffed workshop's cycle under way (staffed months of workshop.staffed_turns and
    // the Money paid), the pause while the enemy holds its city, and the last turn's pauses for its city as the Why screen
    // words them; with no cycle, the next cycle's price at its worker's tier.
    [Test]
    public void AStaffedWorkshopShowsItsCycleAndWhyItWaits([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        var rules = SyntheticWorkshopRules.FromCatalogue(C03.Gdd03Balance!);
        var paused = new Cause("city", City, 0, "Haifa: an arms workshop has no worker without a job this month", key: CauseKeys.WorkshopPaused, args: new[] { Workshops.NoWorker });
        using (var ui = new UiTestKit())
        {
            var (bridge, _) = Game(C03, t, edit: (s, id) =>
            {
                s.Workshops.Add(id, new Workshop(id, true, SyntheticWorkshopTier.Ordinary, rules.Price(SyntheticWorkshopTier.Ordinary), 1));
                var enemy = s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide);
                var home = C03.SettlementHexes.First(h => h.Id == City);
                var uid = s.NextId++;
                s.Units.Add(uid, new Unit(uid, enemy.Type, home.Q, home.R, side: enemy.Side, strength: enemy.Strength));
            }, last: g => new StepResult(g.State, new[] { paused }, Array.Empty<Rejection>()));
            using var panel = Panel(ui, bridge, t);
            var (q, r) = Hex(C03);
            panel.Show(q, r);
            Assert.AreEqual(t.Format("city.workshops_staffed", 1, 1), ui.Q<Label>("city-workshops").text);
            Assert.AreEqual(t["city.workshop_staffed"], ui.Q<Label>("city-workshop-state").text);
            Assert.AreEqual(t.Format("city.workshop_cycle", 1, rules.StaffedTurns, PlayerText.Money(C03, "treasury", (double)rules.Price(SyntheticWorkshopTier.Ordinary))),
                ui.Q<Label>("city-workshop-cycle").text);
            Assert.IsTrue(Cities.Held(C03, bridge.State).Contains(City), "precondition: the enemy holds Haifa");
            Assert.AreEqual(t.Format("city.workshop_held", hebrew ? TownsHe[City] : Towns[City]), ui.Q<Label>("city-workshop-held").text);
            Assert.AreEqual(CauseText.Line(paused, C03, Array.Empty<Statecraft.Game.Map.Units.UnitEntry>(), t, TownsHe), ui.Q<Label>("city-workshop-paused-0").text);
        }
        using (var ui = new UiTestKit())
        {
            var (bridge, _) = Game(C03, t, edit: (s, id) => s.Workshops.Add(id, new Workshop(id, false, SyntheticWorkshopTier.Ordinary)));
            using var panel = Panel(ui, bridge, t);
            var (q, r) = Hex(C03);
            panel.Show(q, r);
            Assert.AreEqual(t["city.workshop_unstaffed"], ui.Q<Label>("city-workshop-state").text);
            Assert.AreEqual(t.Format("city.workshop_next", PlayerText.Money(C03, "treasury", (double)rules.Price(SyntheticWorkshopTier.Ordinary))),
                ui.Q<Label>("city-workshop-cycle").text);
            Assert.IsNull(ui.Root.Q("city-workshop-held"), "Haifa is not held");
            Assert.IsNull(ui.Root.Q("city-workshop-paused-0"), "no turn has run: no pause to report");
        }
    }
}
