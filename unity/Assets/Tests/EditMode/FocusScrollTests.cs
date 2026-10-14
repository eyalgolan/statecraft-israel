#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Accessibility audit H1 (#41): keyboard focus that lands on an element scrolled out of a ScrollView brings it into
/// view (FocusScroll); focus that follows a pointer press leaves the scroll position alone, so a click never jolts a list.</summary>
public class FocusScrollTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>A real panel of w x h pixels, with the game's UXML (or an empty tree) and a FocusScroll at its root.</summary>
    sealed class Rig : IDisposable
    {
        public readonly VisualElement Root;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig(int w, int h, bool game = true)
        {
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("FocusScrollTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            if (game) doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root")?.RemoveFromClassList("loading");
            Own(new FocusScroll(Root));
        }

        public T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        public Focusable? Focused => Root.panel.focusController.focusedElement;

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>The historical game on 26 February 1950 (as CabinetScreenTests): a full cabinet, more cards than one row.</summary>
    static SimBridge In1950()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g, Text);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static bool Inside(VisualElement e, ScrollView view)
    {
        Rect r = e.worldBound, v = view.contentViewport.worldBound;
        return r.yMin >= v.yMin - 0.5f && r.yMax <= v.yMax + 0.5f;
    }

    static void Tab(VisualElement from)
    {
        using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = from; from.SendEvent(e); }
    }

    /// <summary>The Cabinet open in the playtest window (1568x752), its cards laid out, and the first card below the view.</summary>
    static IEnumerator OpenCabinet(Rig r, List<Button> cards, Action<int> below)
    {
        var bridge = In1950();
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new CabinetPresenter(r.Root, bridge, Text, null));
        nav.Open("cabinet");
        yield return Frames(4);
        var seats = r.Root.Q<ScrollView>("cabinet-seats");
        cards.AddRange(seats.contentContainer.Query<Button>().Where(b => b.canGrabFocus).ToList());
        int k = cards.FindIndex(b => !Inside(b, seats));
        Assert.Greater(k, 0, "precondition: the first row shows and a later card is below the view");
        Assert.Less(k + 1, cards.Count, "precondition: a card after the first hidden one");
        Assert.AreEqual(0f, seats.scrollOffset.y, "precondition: the list starts at the top");
        below(k);
    }

    [UnityTest]
    public IEnumerator TabOntoACardBelowTheViewScrollsTheCardsToIt()
    {
        using var r = new Rig(1568, 752);
        var cards = new List<Button>(); int k = -1;
        yield return OpenCabinet(r, cards, i => k = i);
        var seats = r.Root.Q<ScrollView>("cabinet-seats");
        cards[k - 1].Focus();
        yield return Frames(2);
        Assert.AreSame(cards[k - 1], r.Focused, "precondition: the last card in view has the focus");
        Assert.AreEqual(0f, seats.scrollOffset.y, "focus on a card already in view does not scroll");

        Tab(cards[k - 1]);
        yield return Frames(3);
        Assert.AreSame(cards[k], r.Focused, "Tab moves on to the first card below the view");
        Assert.Greater(seats.scrollOffset.y, 0f, "the cards scrolled");
        Assert.IsTrue(Inside(cards[k], seats), $"the focused card {cards[k].worldBound} is inside the view {seats.contentViewport.worldBound}");
    }

    [UnityTest]
    public IEnumerator FocusAfterAPointerPressLeavesTheCardsWhereTheyAre()
    {
        using var r = new Rig(1568, 752);
        var cards = new List<Button>(); int k = -1;
        yield return OpenCabinet(r, cards, i => k = i);
        var seats = r.Root.Q<ScrollView>("cabinet-seats");
        var card = cards[k];
        using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = card.worldBound.center }))
        { down.target = card; card.SendEvent(down); }
        card.Focus();
        yield return Frames(3);
        Assert.AreSame(card, r.Focused, "precondition: the pressed card has the focus");
        Assert.AreEqual(0f, seats.scrollOffset.y, "a pointer press does not scroll the list");

        // The next key press is the keyboard again: Tab on to the next card scrolls to it.
        Tab(card);
        yield return Frames(3);
        Assert.AreSame(cards[k + 1], r.Focused, "precondition: Tab moved on");
        Assert.IsTrue(Inside(cards[k + 1], seats), "after a key, focus scrolls again");
    }

    [UnityTest]
    public IEnumerator FocusOnTheLastCandidateScrollsTheCandidateList()
    {
        using var r = new Rig(1920, 1080);
        var bridge = In1950();
        var nav = r.Own(new ScreenNav(r.Root));
        var cabinet = r.Own(new CabinetPresenter(r.Root, bridge, Text, null));
        nav.Open("cabinet");
        cabinet.Select("finance_minister");
        yield return Frames(4);
        var list = r.Root.Q<ScrollView>("cabinet-candidates");
        Assert.Greater(list.contentViewport.worldBound.height, 40f, "precondition: the candidate list has room to show a row");
        var last = list.contentContainer.Query<Button>().Where(b => b.canGrabFocus).Last();
        Assert.IsFalse(Inside(last, list), "precondition: the last candidate that can be appointed is below the view");
        last.Focus();
        yield return Frames(3);
        Assert.AreSame(last, r.Focused, "precondition: focused");
        Assert.IsTrue(Inside(last, list), $"the candidate {last.worldBound} is inside the view {list.contentViewport.worldBound}");
    }

    /// <summary>Focus given in the same frame the rows are made (before any layout) still scrolls, once they are laid out.</summary>
    [UnityTest]
    public IEnumerator FocusBeforeLayoutScrollsOnceLaidOut()
    {
        using var r = new Rig(800, 600, game: false);
        var view = new ScrollView { style = { height = 100, width = 200 } };
        r.Root.Add(view);
        yield return Frames(2);
        int focusIns = 0;
        r.Root.RegisterCallback<FocusInEvent>(_ => focusIns++);
        Button? last = null;
        for (int i = 0; i < 20; i++) view.Add(last = new Button { text = "row " + i, style = { height = 30 } });
        last!.Focus();
        for (int i = 0; i < 30 && !(r.Focused == last && Inside(last, view)); i++) yield return null;
        Assert.AreSame(last, r.Focused, "precondition: focused");
        Assert.IsTrue(Inside(last, view), $"the row {last.worldBound} is inside the view {view.contentViewport.worldBound} " +
            $"(offset {view.scrollOffset.y}, range {view.verticalScroller.lowValue} to {view.verticalScroller.highValue}, {focusIns} focus-in)");
    }
}
