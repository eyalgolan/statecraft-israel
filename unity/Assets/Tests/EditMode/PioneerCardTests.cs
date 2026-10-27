#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// R-QUAL-003, R-TURN-004: presentation asks the sim and uses its existing immediate unit-order path.
public class PioneerCardTests
{
    public static readonly Content C = ContentLoader.Load(Paths.Content);
    public static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    public static int Add(GameState s, int q, int r, string type = "halutz")
    {
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, type, q, r));
        return id;
    }
    public static InfoPanelPresenter Panel(UiTestKit ui, SimBridge b, UiText t) =>
        new InfoPanelPresenter(ui.Root, b, t, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id));
    public static Button Link(VisualElement root, int id) => root.Q<Button>("pioneer-select-" + id.ToString(CultureInfo.InvariantCulture));
    static void Open(UiTestKit ui, InfoPanelPresenter p, int id, int q, int r)
    {
        p.Show(q, r);
        var link = Link(ui.Root, id);
        Assert.IsNotNull(link, "RED on base: a pioneer on ordinary land has no selection control");
        UiTestKit.Press(link);
    }

    [TestCase(false, "halutz")] [TestCase(true, "halutz")]
    [TestCase(false, "veteran_halutz")] [TestCase(true, "veteran_halutz")]
    public void ATokenFreeListOpensALiveCardAndReturnsExactlyOnce(bool he, string type)
    {
        using var ui = new UiTestKit(); var t = Text(he);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var hex = C.Hexes.First(h => h.Land && !C.SettlementHexes.Any(x => x.Q == h.Q && x.R == h.R) && !C.Sites.Any(x => x.Q == h.Q && x.R == h.R));
        var id = Add(g.State, hex.Q, hex.R, type); var other = Add(g.State, hex.Q, hex.R);
        var b = new SimBridge(g, t); using var p = Panel(ui, b, t);
        Open(ui, p, id, hex.Q, hex.R);
        Assert.IsTrue(p.ShowsUnit);
        Assert.AreEqual(GddHexPanel.Group(t, b.State, id), ui.Q<Label>("info-title").text);
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain(t["pioneer.card.idle"]));
        Assert.That(ui.Q<Label>("info-subtitle").text, Does.Contain(t[type == Projects.VeteranType ? "unit.name.veteran_halutz" : "city.track_halutz"]));
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain(t.Format("info.hex", t["info.biome." + hex.Biome], hex.ElevM)));
        foreach (var name in new[] { "info-defence", "info-move", "info-fortify", "info-hold", "info-attack", "info-send-home", "info-reinforce", "info-skip" })
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>(name).style.display.value, name);
        var back = ui.Q<Button>("info-pioneer-return"); Assert.IsTrue(back.enabledSelf);
        UiTestKit.Press(back); UiTestKit.Press(back);
        var order = b.Session.Committed.OfType<UnitOrder>().Single();
        Assert.AreEqual((id, Qualification.ReturnOrder), (order.Unit, order.Order));
        Assert.IsNull(order.Q); Assert.IsNull(order.R); Assert.IsEmpty(b.Queued);
        Assert.IsFalse(b.State.Units.ContainsKey(id)); Assert.IsTrue(b.State.Units.ContainsKey(other));
        Assert.IsFalse(p.ShowsUnit); Assert.AreEqual((hex.Q, hex.R), p.Selected);
        Assert.IsNotNull(Link(ui.Root, other)); Assert.IsNull(Link(ui.Root, id));
    }

    [TestCase(false, false, "halutz")] [TestCase(false, true, "halutz")]
    [TestCase(true, false, "halutz")] [TestCase(true, true, "halutz")]
    [TestCase(false, false, "veteran_halutz")] [TestCase(false, true, "veteran_halutz")]
    [TestCase(true, false, "veteran_halutz")] [TestCase(true, true, "veteran_halutz")]
    public void BusySiteAndCorridorLinksOpenTheWorkersCard(bool corridor, bool he, string type)
    {
        using var ui = new UiTestKit(); var t = Text(he);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var home = C.SettlementHexes.First(x => x.Id == "tel_aviv");
        var id = Add(g.State, home.Q, home.R, type);
        int q, r; string target;
        if (corridor)
        {
            var d = C.Corridors.First(x => x.Segments[0].Hexes.Any(h => !C.SettlementHexes.Any(city => city.Q == h[0] && city.R == h[1]))); var h = d.Segments[0].Hexes.First(h => !C.SettlementHexes.Any(city => city.Q == h[0] && city.R == h[1])); q = h[0]; r = h[1];
            target = t.Format("corridor.segment", he && !string.IsNullOrWhiteSpace(d.NameHe) ? d.NameHe : d.Name, 1);
            g.State.Corridors[d.Id] = new CorridorWork(d.Id, d.Segments.Select((_, i) => i == 0 ? new SegmentWork(id, 0, null) : null).ToArray());
        }
        else
        {
            var site = C.Sites.First(x => Projects.IsYishuvSite(x)); q = site.Q; r = site.R;
            target = he && !string.IsNullOrWhiteSpace(site.NameHe) ? site.NameHe : site.Name;
            g.State.Sites[site.Id] = new SiteWork(site.Id, id, 3, null);
        }
        var b = new SimBridge(g, t); using var p = Panel(ui, b, t);
        p.Show(q, r);
        var link = ui.Root.Q<Button>("pioneer-project-" + id.ToString(CultureInfo.InvariantCulture));
        Assert.IsNotNull(link, "RED on base: project worker has no card link"); UiTestKit.Press(link);
        Assert.IsFalse(ui.Q<Button>("info-pioneer-return").enabledSelf);
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain(t["pioneer.card.stop_first"]));
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain(t.Format("pioneer.card.working", target)));
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain("tel_aviv"), "test town-name resolver identifies the current hex separately from the work target");
        Assert.AreEqual(0, b.Session.Committed.Count);
    }

    [TestCase(false)] [TestCase(true)]
    public void CardRefusesQueuedBuildAndStopWithoutWithdrawingEither(bool stop)
    {
        using var ui = new UiTestKit(); var t = Text(false);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var home = C.SettlementHexes.First(x => x.Id == "tel_aviv"); var id = Add(g.State, home.Q, home.R);
        var b = new SimBridge(g, t); using var p = Panel(ui, b, t);
        Open(ui, p, id, home.Q, home.R);
        Command work = stop ? (Command)new StopWork(id) : new PioneerWork(id, Projects.BuildYishuv, C.Sites.First(x => Projects.IsYishuvSite(x)).Id);
        Assert.IsNull(b.Queue(work)); Assert.IsFalse(ui.Q<Button>("info-pioneer-return").enabledSelf);
        // Force a stale enabled control to exercise the dispatch guard as well as disabled presentation.
        ui.Q<Button>("info-pioneer-return").SetEnabled(true);
        UiTestKit.Press(ui.Q<Button>("info-pioneer-return")); Assert.IsEmpty(b.Session.Committed);
        Assert.AreSame(work, b.Queued.Single()); Assert.IsTrue(b.State.Units.ContainsKey(id));
        b.Withdraw(work); b.State.GameOver = GameOverCodes.MvpEnded; p.Render();
        Assert.IsFalse(ui.Q<Button>("info-pioneer-return").enabledSelf);
    }

    [Test]
    public void ReplacingTheSessionCannotReturnAReusedId()
    {
        using var ui = new UiTestKit(); var t = Text(false);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical); var h = C.SettlementHexes.First();
        var id = Add(g.State, h.Q, h.R); var b = new SimBridge(g, t); using var p = Panel(ui, b, t);
        Open(ui, p, id, h.Q, h.R);
        var next = GameSession.New(C, GoldenRun.Seed, Mode.Historical); Add(next.State, h.Q, h.R);
        b.Replace(next); Assert.IsFalse(p.ShowsUnit);
        UiTestKit.Press(ui.Q<Button>("info-pioneer-return")); Assert.IsEmpty(next.Committed);
    }

    [TestCase("en-US")] [TestCase("he-IL")]
    public void HebrewElevationAndGroupNumbersKeepTheirDirection(string culture)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var t = Text(true); var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var h = C.Hexes.First(x => x.Land && x.ElevM < 0); var id = Add(g.State, h.Q, h.R);
            using var ui = new UiTestKit(); using var p = Panel(ui, new SimBridge(g, t), t);
            Open(ui, p, id, h.Q, h.R);
            var value = h.ElevM.ToString("N0", CultureInfo.InvariantCulture);
            Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain("\u200e" + value + "\u200e"));
            Assert.That(ui.Q<Label>("info-title").text, Does.Contain("\u200e"));
        }
        finally { CultureInfo.CurrentCulture = before; }
    }
    [TestCase(false)] [TestCase(true)]
    public void CompletedWorkReadsIdleAndPromotionRefreshesTheTrack(bool corridor)
    {
        using var ui = new UiTestKit(); var t = Text(false);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical); var h = C.SettlementHexes.First();
        var id = Add(g.State, h.Q, h.R);
        if (corridor)
        {
            var d = C.Corridors.First();
            g.State.Corridors[d.Id] = new CorridorWork(d.Id, d.Segments.Select((_, i) => i == 0 ? new SegmentWork(null, Projects.Terms(C.RulesAt(g.State.Date).Mvp!, d.Kind == Corridors.Rail ? Corridors.BuildRail : Corridors.BuildRoad).Turns, g.State.Date) : null).ToArray());
        }
        else
        {
            var site = C.Sites.First(); g.State.Sites[site.Id] = SiteWork.Built(site.Id, g.State.Date);
        }
        // Both completed records have no worker: arrangement can now reach the card assertions.
        Assert.IsFalse(Projects.IsBusy(g.State, id));
        var b = new SimBridge(g, t); using var p = Panel(ui, b, t); Open(ui, p, id, h.Q, h.R);
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain(t["pioneer.card.idle"]));
        Assert.IsTrue(ui.Q<Button>("info-pioneer-return").enabledSelf);
        g.State.Units[id] = new Unit(id, Projects.VeteranType, h.Q, h.R); p.Render();
        Assert.That(ui.Q<Label>("info-subtitle").text, Does.Contain(t["unit.name.veteran_halutz"]));
        g.State.Units.Remove(id); p.Render(); Assert.IsFalse(p.ShowsUnit);
        UiTestKit.Press(ui.Q<Button>("info-pioneer-return")); Assert.IsEmpty(b.Session.Committed);
    }

    [Test]
    public void HiddenDisabledAndCoveredCardsCannotDispatchAndDisposeUnhooksReturn()
    {
        using var ui = new UiTestKit(); var t = Text(false);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical); var h = C.SettlementHexes.First();
        var id = Add(g.State, h.Q, h.R); var b = new SimBridge(g, t); var p = Panel(ui, b, t);
        Open(ui, p, id, h.Q, h.R); var back = ui.Q<Button>("info-pioneer-return");
        ui.Q<VisualElement>("info-panel").SetEnabled(false); UiTestKit.Press(back); Assert.IsEmpty(b.Session.Committed);
        ui.Q<VisualElement>("info-panel").SetEnabled(true);
        var hud = ui.Q<VisualElement>("info-panel").parent;
        hud.AddToClassList(ScreenNav.WideOpenClass); UiTestKit.Press(back); Assert.IsEmpty(b.Session.Committed);
        hud.RemoveFromClassList(ScreenNav.WideOpenClass);
        p.Show(h.Q, h.R); UiTestKit.Press(back); Assert.IsEmpty(b.Session.Committed);
        Open(ui, p, id, h.Q, h.R); p.Dispose(); UiTestKit.Press(back); Assert.IsEmpty(b.Session.Committed);
        using var again = Panel(ui, b, t); Open(ui, again, id, h.Q, h.R); UiTestKit.Press(back);
        Assert.AreEqual(1, b.Session.Committed.Count);
    }

    [Test]
    public void OptionalProjectCallbackLeavesExistingRenderCallersUnchanged()
    {
        var t = Text(false); var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var site = C.Sites.First(x => Projects.IsYishuvSite(x)); var id = Add(g.State, site.Q, site.R);
        g.State.Sites[site.Id] = new SiteWork(site.Id, id, 3, null);   // the worker's row is the one that links to its card
        using var ui = new UiTestKit();
        var b = new SimBridge(g, t);
        var host = ui.Q<VisualElement>(GddHexPanel.HostName);
        var projects = ui.Q<VisualElement>(GddHexPanel.ProjectsHostName);
        GddHexPanel.Render(host, projects, b, t, site.Q, site.R, x => x);
        var actions = host.Query<Button>().ToList().Select(x => (x.name, x.text, x.enabledSelf)).ToList();
        Assert.IsNull(host.Q<Button>("pioneer-project-" + id.ToString(CultureInfo.InvariantCulture)));
        int? picked = null; int calls = 0;
        var render = typeof(GddHexPanel).GetMethod("Render");
        Assert.AreEqual(8, render!.GetParameters().Length, "optional navigation callback is absent on base");
        render.Invoke(null, new object[] { host, projects, b, t, site.Q, site.R,
            (System.Func<string, string>)(x => x), (System.Action<int>)(x => { picked = x; calls++; }) });
        CollectionAssert.AreEqual(actions, host.Query<Button>().ToList().Where(x => !x.name.StartsWith("pioneer-project-", System.StringComparison.Ordinal))
            .Select(x => (x.name, x.text, x.enabledSelf)).ToList());
        var link = host.Q<Button>("pioneer-project-" + id.ToString(CultureInfo.InvariantCulture));
        Assert.IsNotNull(link.panel, "UiTestKit hosts attach generated links to the runtime panel");
        UiTestKit.Press(link); Assert.AreEqual(id, picked); Assert.AreEqual(1, calls);
    }

    [TestCase(false)] [TestCase(true)]
    public void CardRefusesWhenCheckReturnFailsBeforeTheNextRender(bool corridor)
    {
        using var ui = new UiTestKit(); var t = Text(false);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical); var h = C.SettlementHexes.First();
        var id = Add(g.State, h.Q, h.R); var b = new SimBridge(g, t); using var p = Panel(ui, b, t);
        Open(ui, p, id, h.Q, h.R); var back = ui.Q<Button>("info-pioneer-return");
        Assert.IsTrue(back.enabledSelf);
        var site = C.Sites.First(x => Projects.IsYishuvSite(x));
        if (corridor)
        {
            var d = C.Corridors.First();
            b.State.Corridors[d.Id] = new CorridorWork(d.Id, d.Segments.Select((_, i) => i == 0 ? new SegmentWork(id, 0, null) : null).ToArray());
        }
        else b.State.Sites[site.Id] = new SiteWork(site.Id, id, 3, null);
        Assert.IsNotNull(Qualification.CheckReturn(b.State, id), "the sim, not the card, refuses committed work");
        var refused = 0; b.Refused += _ => refused++;
        UiTestKit.Press(back); Assert.IsEmpty(b.Session.Committed); Assert.IsTrue(b.State.Units.ContainsKey(id));
        Assert.AreEqual(0, refused, "the shared guard asks CheckReturn first: the order never reaches the sim to be refused");
    }

    [TestCase(false)] [TestCase(true)]
    public void BusyToCompletedWorkRefreshesTheSameCard(bool corridor)
    {
        using var ui = new UiTestKit(); var t = Text(false);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical); var home = C.SettlementHexes.First();
        var id = Add(g.State, home.Q, home.R);
        var site = C.Sites.First(x => Projects.IsYishuvSite(x)); var d = C.Corridors.First();
        var turns = Projects.Terms(C.RulesAt(g.State.Date).Mvp!, d.Kind == Corridors.Rail ? Corridors.BuildRail : Corridors.BuildRoad).Turns;
        if (corridor) g.State.Corridors[d.Id] = new CorridorWork(d.Id, d.Segments.Select((_, i) => i == 0 ? new SegmentWork(id, 0, null) : null).ToArray());
        else g.State.Sites[site.Id] = new SiteWork(site.Id, id, 1, null);
        var b = new SimBridge(g, t); using var p = Panel(ui, b, t); Open(ui, p, id, home.Q, home.R);
        Assert.IsFalse(ui.Q<Button>("info-pioneer-return").enabledSelf);
        if (corridor) g.State.Corridors[d.Id] = new CorridorWork(d.Id, d.Segments.Select((_, i) => i == 0 ? new SegmentWork(null, turns, g.State.Date) : null).ToArray());
        else g.State.Sites[site.Id] = SiteWork.Built(site.Id, g.State.Date);
        p.Render();
        Assert.IsTrue(p.ShowsUnit);
        Assert.That(ui.Q<Label>("info-settlements").text, Does.Contain(t["pioneer.card.idle"]));
        Assert.IsTrue(ui.Q<Button>("info-pioneer-return").enabledSelf);
    }

}
