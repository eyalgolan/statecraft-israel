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
                "resource" => t.Format("city.project_reason.no_resource", Towns[City], "vineyards", ContentNames.Of(t, winery)),
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
        Assert.AreEqual(new[] { En.Format("order.no_attention", bridge.Price(order), 0.0) }, refusals);
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
}
