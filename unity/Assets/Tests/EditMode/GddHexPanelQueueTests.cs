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

/// <summary>#2518 and #2743 item 2: the GDD hex panel's order buttons follow what End turn would do with this turn's queue
/// (Simulation.Preview over the committed orders and the queue). A button is off, with the sim's reason, where End turn would
/// refuse the press; it is on where a queued order makes the press valid (a Cancel or a Stop that frees the place, a Return
/// or a Reassign that makes the inverse order possible); and a queued order End turn refuses anyway holds nothing back.
/// Real content and a real SimBridge, as CityPanelTests and CityProjectPanelTests use them.</summary>
public class GddHexPanelQueueTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly Dictionary<string, string> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    const string City = "tel_aviv", Site = "mefalsim", Corridor = "burma_road", Lone = "haifa";

    static (int q, int r) CityHex => C.SettlementHexes.Where(x => x.Id == City).Select(x => (x.Q, x.R)).Single();
    static Statecraft.Sim.Site SiteDef => C.Sites.Single(x => x.Id == Site);

    static SimBridge Bridge(UiText text, Action<GameState>? edit = null)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        Assert.IsTrue(C.IsGdd, "precondition: the real content runs the GDD loop");
        edit?.Invoke(g.State);
        return new SimBridge(g, text);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge, UiText text) => new InfoPanelPresenter(ui.Root, bridge, text, Towns);

    static int AddHalutz(GameState s)
    {
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", CityHex.q, CityHex.r));
        return id;
    }

    static Action<GameState> Qualify(int craftsmen) =>
        s => s.Cities[City] = s.Cities[City].WithQualified(new Qualified(craftsmen, 0, Array.Empty<int>()));

    /// <summary>What End turn's own order runner refuses for <paramref name="queue"/> after the bridge's committed orders.</summary>
    static IReadOnlyList<Rejection> Refused(SimBridge bridge, params Command[] queue) =>
        Simulation.Preview(C, bridge.TurnStart, bridge.Session.Committed.Concat(queue).ToList()).Rejected;

    /// <summary>A hex only <see cref="Lone"/> reaches (CityProjectPanelTests' Lone), by the sim's own helper.</summary>
    static (int q, int r) LoneHex(Content c)
    {
        var s = GameSession.New(c, GoldenRun.Seed, Mode.Historical).State;
        var hex = c.Hexes.Where(h => h.Land && Projects.CitiesFor(c, s, h.Q, h.R).SequenceEqual(new[] { Lone })).OrderBy(h => h.Q).ThenBy(h => h.R).First();
        return (hex.Q, hex.R);
    }

    // #2518, done-when 1. Red on main e29447748: GddHexPanel.cs:176-178 turns Train off when the preview refuses it, but
    // adds no line, so city-train-recruit-reason is null.
    [Test]
    public void TrainIsOffWithTheSimsReasonWhenArmsAreShort([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s => s.Arms = 0);
        using var panel = Panel(ui, bridge, t);
        panel.Show(CityHex.q, CityHex.r);
        var press = new Train(City, "recruit", 1);
        var refused = Refused(bridge, press).Single(x => ReferenceEquals(x.Command, press)).Reason;
        StringAssert.Contains("not enough Arms", refused, "precondition: the sim refuses basic training for its Arms");
        Assert.IsFalse(ui.Q<Button>("city-train-recruit").enabledSelf);
        var line = ui.Q<Label>("city-train-recruit-reason");
        Assert.IsNotNull(line, "the disabled Train says why");
        Assert.AreEqual(SimBridge.ReasonText(t, refused, C, bridge.TownNamesHe), line.text);
        if (hebrew) Assert.AreNotEqual(refused, line.text, "the Hebrew string, not the sim's English");
        Assert.IsEmpty(Refused(bridge, new Train(City, "halutz", 1)), "precondition: the halutz course needs no Arms");
        Assert.IsTrue(ui.Q<Button>("city-train-halutz").enabledSelf);
        Assert.IsNull(ui.Root.Q("city-train-halutz-reason"), "an accepted Train has no reason line");
    }

    // #2518 residual (1). Red on main e29447748: GddHexPanel.cs:386-387 returns after a project's Cancel whenever the hex is
    // not a city's own, queued Cancel or not, so city-project-housing is null; the sim takes Cancel then Start in one turn.
    [Test]
    public void AQueuedCancelOffersBuildOnTheProjectsHex([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        var (q, r) = LoneHex(C);
        int id = -1;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s => { id = s.NextId++; s.CityProjects.Add(id, new CityProject(id, "housing", Lone, q, r, 4)); });
        using var panel = Panel(ui, bridge, t);
        panel.Show(q, r);
        Assert.IsNull(ui.Root.Q("city-project-housing"), "precondition: the hex is taken");
        ui.Press("city-project-cancel");
        var build = ui.Q<Button>("city-project-housing");
        Assert.IsNotNull(build, "the cancelled project frees its hex this turn");
        Assert.IsTrue(build.enabledSelf);
        UiTestKit.Press(build);
        Assert.IsNotNull(bridge.Queued.OfType<StartProject>().SingleOrDefault(x => x.ProjectKind == "housing" && x.Q == q && x.R == r));
        var turn = bridge.EndTurn();
        Assert.IsEmpty(turn.Rejected, "the sim takes the Cancel and then the Start");
        Assert.IsFalse(turn.State.CityProjects.ContainsKey(id));
        Assert.AreEqual(1, turn.State.CityProjects.Values.Count(x => x.Q == q && x.R == r), "the new project is on the hex");
    }

    // #2518 residual (2). Red on main e29447748: GddHexPanel.cs:301-309 shows only Stop on a site being built, so
    // site-build-{b} is null; the sim takes Stop then another group's Build in one turn.
    [Test]
    public void AQueuedStopOffersTheSiteToAnotherIdleGroup()
    {
        using var ui = new UiTestKit();
        int a = -1, b = -1;
        var bridge = Bridge(En, s => { a = AddHalutz(s); b = AddHalutz(s); s.Sites[Site] = new SiteWork(Site, a, 3, null); });
        using var panel = Panel(ui, bridge, En);
        panel.Show(SiteDef.Q, SiteDef.R);
        Assert.IsNull(ui.Root.Q($"site-build-{b}"), "precondition: the site is being built");
        ui.Press("site-stop");
        var build = ui.Q<Button>($"site-build-{b}");
        Assert.IsNotNull(build, "the stopped site is free for another group this turn");
        Assert.IsTrue(build.enabledSelf);
        Assert.IsNull(ui.Root.Q($"site-build-{a}"), "the group that stops is still at work when the panel renders");
        UiTestKit.Press(build);
        Assert.IsFalse(ui.Q<Button>($"site-build-{b}").enabledSelf, "ordered");
        var turn = bridge.EndTurn();
        Assert.IsEmpty(turn.Rejected, "the sim takes the Stop and then the Build");
        Assert.AreEqual(b, turn.State.Sites[Site].Unit);
    }

    // #2518 residual (2), on a corridor segment. Red on main e29447748: GddHexPanel.cs:344-352 shows only Stop on a segment
    // being built, so corridor-build-{b} is null.
    [Test]
    public void AQueuedStopOffersTheSegmentToAnotherIdleGroup()
    {
        using var ui = new UiTestKit();
        int a = -1, b = -1;
        var d = C.Corridors.Single(x => x.Id == Corridor);
        var bridge = Bridge(En, s =>
        {
            a = AddHalutz(s); b = AddHalutz(s);
            s.Corridors[Corridor] = new CorridorWork(Corridor, d.Segments.Select((_, i) => i == 0 ? new SegmentWork(a, 0, null) : null).ToArray());
        });
        using var panel = Panel(ui, bridge, En);
        var h = d.Segments[0].Hexes[0];
        panel.Show(h[0], h[1]);
        VisualElement Box() => ui.Root.Q<VisualElement>($"corridor-part-{Corridor}-1");
        Assert.IsNull(Box().Q($"corridor-build-{b}"), "precondition: the segment is being built");
        UiTestKit.Press(Box().Q<Button>("corridor-stop"));
        var build = Box().Q<Button>($"corridor-build-{b}");
        Assert.IsNotNull(build, "the stopped segment is free for another group this turn");
        Assert.IsTrue(build.enabledSelf);
        UiTestKit.Press(build);
        var turn = bridge.EndTurn();
        Assert.IsEmpty(turn.Rejected, "the sim takes the Stop and then the Build");
        Assert.AreEqual(b, turn.State.Corridors[Corridor].Segments[0]!.Unit);
    }

    // #2518 residual (4). Red on main e29447748: GddHexPanel.cs:211-212 skips a role the city has no qualified group of now,
    // so after a queued Return city-reassign-craftsmen is null; the sim takes Return then Reassign in one turn.
    [Test]
    public void AQueuedReturnOffersTheReassignOfThoseCraftsmen([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s => { Qualify(0)(s); s.Cities[City] = s.Cities[City].WithCraftsmen(1); });
        using var panel = Panel(ui, bridge, t);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsNull(ui.Root.Q("city-reassign-craftsmen"), "precondition: no qualified craftsman yet");
        ui.Press("city-return-craftsmen");
        var reassign = ui.Q<Button>("city-reassign-craftsmen");
        Assert.IsNotNull(reassign, "the returned group can be reassigned this turn");
        Assert.AreEqual(t.Format("city.reassign_craftsmen", 1), reassign.text, "the one group the Return qualifies");
        Assert.IsTrue(reassign.enabledSelf);
        UiTestKit.Press(reassign);
        Assert.IsFalse(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "the one qualified group is ordered");
        Assert.IsEmpty(bridge.EndTurn().Rejected, "the sim takes the Return and then the Reassign");
    }

    // #2518 residual (4), the other way. Red on main e29447748: GddHexPanel.cs:195 shows Return only for the city's
    // craftsmen now, so after a queued Reassign city-return-craftsmen is null.
    [Test]
    public void AQueuedReassignOffersTheReturnOfThoseCraftsmen([Values] bool hebrew)
    {
        var t = hebrew ? He : En;
        using var ui = new UiTestKit();
        var bridge = Bridge(t, s => { Qualify(1)(s); s.Cities[City] = s.Cities[City].WithCraftsmen(0); });
        using var panel = Panel(ui, bridge, t);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsNull(ui.Root.Q("city-return-craftsmen"), "precondition: no craftsmen yet");
        ui.Press("city-reassign-craftsmen");
        var back = ui.Q<Button>("city-return-craftsmen");
        Assert.IsNotNull(back, "the reassigned group can return this turn");
        Assert.IsTrue(back.enabledSelf);
        UiTestKit.Press(back);
        Assert.IsFalse(ui.Q<Button>("city-return-craftsmen").enabledSelf, "the one craftsman group is ordered");
        Assert.IsEmpty(bridge.EndTurn().Rejected, "the sim takes the Reassign and then the Return");
    }

    // #2743 item 2, the issue's example: 1 civilian, none employed, no Arms, Train(recruit, 1) queued. Red on main e29447748:
    // GddHexPanel.cs:125 asks Cities.MaxWorkers with the whole queue, which takes the refused trainee off (Cities.cs:29-35),
    // so the maximum is 0 and + is off, though End turn would refuse the Train and accept the Assign.
    [Test]
    public void ARefusedQueuedTrainDoesNotHoldBackTheWorkersStepper()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { s.Cities[City] = s.Cities[City].WithCounts(1, 0, s.Cities[City].Refugees).WithCraftsmen(0); s.Arms = 0; });
        Assert.IsNull(bridge.Queue(new Train(City, "recruit", 1)), "precondition: the bridge queues the Train");
        var train = bridge.Queued.OfType<Train>().Single();
        Assert.AreEqual(train, Refused(bridge, bridge.Queued.ToArray()).Single().Command, "precondition: End turn refuses the Train");
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(ui.Q<Button>("city-workers-more").enabledSelf, "the refused Train takes nobody");
        ui.Press("city-workers-more");
        Assert.AreEqual(1, ((AssignWorkers)bridge.QueuedLike(new AssignWorkers(City, "job", 0))!).Count);
        Assert.AreEqual(train, Refused(bridge, bridge.Queued.ToArray()).Single().Command, "End turn takes the Assign; only the Train is refused");
    }

    // #2743 item 2, Reassign. Red on main e29447748: GddHexPanel.cs:204 asks Training.Available with the whole queue, so the
    // refused trainee takes the only civilian without work (idleLeft 0), and :209-210 count it against the qualifications.
    [Test]
    public void ARefusedQueuedTrainDoesNotHoldBackReassign()
    {
        using var ui = new UiTestKit();
        var bridge = Bridge(En, s => { s.Cities[City] = s.Cities[City].WithCounts(1, 0, s.Cities[City].Refugees).WithCraftsmen(0); Qualify(1)(s); s.Arms = 0; });
        Assert.IsNull(bridge.Queue(new Train(City, "recruit", 1)), "precondition: the bridge queues the Train");
        var train = bridge.Queued.OfType<Train>().Single();
        Assert.AreEqual(train, Refused(bridge, bridge.Queued.ToArray()).Single().Command, "precondition: End turn refuses the Train");
        using var panel = Panel(ui, bridge, En);
        panel.Show(CityHex.q, CityHex.r);
        Assert.IsTrue(ui.Q<Button>("city-reassign-craftsmen").enabledSelf, "the refused Train takes nobody");
        ui.Press("city-reassign-craftsmen");
        Assert.AreEqual(train, Refused(bridge, bridge.Queued.ToArray()).Single().Command, "End turn takes the Reassign; only the Train is refused");
    }

    // #2743 item 2, Workshop +, gdd-0.3. A queued Reassign of a craftsman Haifa has not got is refused by End turn. Red on main
    // e29447748: GddHexPanel.cs:498 asks Workshops.Check with the whole queue, which takes its civilian off (Training.Ordered,
    // Workshops.cs:79), leaving nobody for the workshop, so + is off.
    [Test]
    public void ARefusedQueuedOrderDoesNotHoldBackTheWorkshopStepper()
    {
        var c03 = WorkshopPanelTests.Load03();
        var g = GameSession.New(c03, GoldenRun.Seed, Mode.Historical);
        var s = g.State;
        var x = s.Cities[Lone];
        // 10 civilians, 9 in the jobs the player set (WorkshopPanelTests.Game): one free for the workshop.
        s.Cities[Lone] = new City(x.Id, x.HousingCapacity, 10, 10, 9, 0, 0, x.Condition, jobsDefault: false);
        var (q, r) = LoneHex(c03);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, Workshops.ArmsWorkshop, Lone, q, r, 0).Built(s.Date));
        var bridge = new SimBridge(g, En);
        Assert.IsNull(Workshops.Check(c03, bridge.State, Lone, 1, Array.Empty<Command>()), "precondition: one may staff it");
        Assert.IsNull(bridge.Queue(new ReassignCraftsmen(Lone, 1)), "precondition: the bridge queues the Reassign");
        var reassign = bridge.Queued.Single();
        var rejected = Simulation.Preview(c03, bridge.TurnStart, bridge.Session.Committed.Concat(bridge.Queued).ToList()).Rejected;
        Assert.AreEqual(reassign, rejected.Single().Command, "precondition: End turn refuses it (no qualified craftsman)");
        using var ui = new UiTestKit();
        using var panel = Panel(ui, bridge, En);
        panel.Show(q, r);
        Assert.IsTrue(ui.Q<Button>("city-workshops-more").enabledSelf, "the refused Reassign takes nobody");
        Assert.IsNull(ui.Root.Q("city-workshops-reason"));
    }
}
