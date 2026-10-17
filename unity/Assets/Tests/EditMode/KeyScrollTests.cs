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

/// <summary>Accessibility audit H3 (#41): PageUp, PageDown, Home and End scroll the open screen's list, or the open decision
/// card's text, from the keyboard (KeyScroll), since those panels hold labels that Tab never reaches and the arrows pan the
/// camera.</summary>
public class KeyScrollTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>A real panel in the playtest window (1568x752) with the game's UXML.</summary>
    sealed class Rig : IDisposable
    {
        public readonly VisualElement Root;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig()
        {
            rt = new RenderTexture(1568, 752, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("KeyScrollTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root")?.RemoveFromClassList("loading");
        }

        public T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>Holds a view to <paramref name="height"/> px, so its content overflows whatever the window.</summary>
    static void Shorten(ScrollView view, float height)
    {
        view.style.height = height; view.style.minHeight = height; view.style.maxHeight = height; view.style.flexGrow = 0;
    }

    /// <summary>PageDown moves one view less a line, End reaches the bottom, PageUp moves back and Home reaches the top; at
    /// the top PageUp does nothing. The focus never moves.</summary>
    static void PagesThrough(VisualElement root, ScrollView view, string? screen, bool cardOpen, string where)
    {
        var focused = root.panel.focusController.focusedElement;
        float high = view.verticalScroller.highValue, page = Mathf.Max(24f, view.contentViewport.layout.height - 24f);
        Assert.Greater(high, page, where + ": precondition, the view overflows by more than a page");
        Assert.AreEqual(0f, view.scrollOffset.y, 0.5f, where + ": precondition, at the top");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageDown), where + ": PageDown scrolls");
        Assert.AreEqual(Mathf.Min(page, high), view.scrollOffset.y, 0.5f, where + ": PageDown moves one view less a line");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.End), where + ": End scrolls");
        Assert.AreEqual(high, view.scrollOffset.y, 0.5f, where + ": End reaches the bottom");
        Assert.IsFalse(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageDown), where + ": PageDown at the bottom");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageUp), where + ": PageUp scrolls");
        Assert.AreEqual(high - page, view.scrollOffset.y, 0.5f, where + ": PageUp moves one view less a line");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.Home), where + ": Home scrolls");
        Assert.AreEqual(0f, view.scrollOffset.y, 0.5f, where + ": Home reaches the top");
        Assert.IsFalse(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageUp), where + ": PageUp at the top");
        Assert.AreSame(focused, root.panel.focusController.focusedElement, where + ": the keys move no focus");
    }

    [UnityTest]
    public IEnumerator ThePageKeysScrollTheWhyListAndTheDefenceBody()
    {
        using var r = new Rig();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        r.Own(new DefencePresenter(r.Root, bridge, Text));
        bridge.Queue(new SetMobilization(1.0));
        bridge.EndTurn();
        foreach (var (screen, view) in new[] { ("why", "why-rows"), ("defence", "defence-body") })
        {
            var v = r.Root.Q<ScrollView>(view);
            Shorten(v, 80f);
            nav.Open(screen);
            yield return Frames(4);
            PagesThrough(r.Root, v, screen, false, screen);
        }
    }

    [UnityTest]
    public IEnumerator FocusInTheCandidateListScrollsThatListNotTheCards()
    {
        using var r = new Rig();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        var nav = r.Own(new ScreenNav(r.Root));
        var cabinet = r.Own(new CabinetPresenter(r.Root, new SimBridge(g, Text), Text, null));
        nav.Open("cabinet");
        cabinet.Select("finance_minister");
        var candidates = r.Root.Q<ScrollView>("cabinet-candidates");
        var seats = r.Root.Q<ScrollView>("cabinet-seats");
        Shorten(candidates, 60f);
        yield return Frames(4);
        Assert.Greater(seats.verticalScroller.highValue, 0f, "precondition: the cards overflow too");
        var first = candidates.contentContainer.Query<Button>().ToList().First(b => b.canGrabFocus);
        first.Focus();
        yield return Frames(2);
        Assert.AreSame(first, r.Root.panel.focusController.focusedElement, "precondition: a candidate has the focus");
        PagesThrough(r.Root, candidates, "cabinet", false, "candidates");
        Assert.AreEqual(0f, seats.scrollOffset.y, 0.5f, "the cards did not move");

        first.Blur();
        yield return Frames(2);
        Assert.IsTrue(KeyScroll.Press(r.Root, "cabinet", false, KeyScroll.Key.PageDown), "no focus in a list: the cards scroll");
        Assert.Greater(seats.scrollOffset.y, 0f, "the cards moved");
    }

    [UnityTest]
    public IEnumerator TheCardsTextScrollsOnlyWhileTheCardIsOpenAndNoScreenCoversIt()
    {
        using var r = new Rig();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var bridge = new SimBridge(g, Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        var card = r.Own(new EventCardPresenter(r.Root, bridge, Text, null));
        var text = r.Root.Q<ScrollView>("event-scroll");
        Shorten(text, 16f);
        yield return Frames(4);
        Assert.IsNotNull(card.Shown, "precondition: the card is open");
        Assert.IsFalse(card.Collapsed, "precondition: the card is not hidden");
        Assert.Greater(text.worldBound.height, 0f, "precondition: the card's text is laid out");
        Assert.IsFalse(KeyScroll.Press(r.Root, null, false, KeyScroll.Key.PageDown), "a hidden card's text does not scroll");
        Assert.AreEqual(0f, text.scrollOffset.y, 0.5f, "hidden card");
        nav.Open("why");
        yield return Frames(2);
        KeyScroll.Press(r.Root, "why", true, KeyScroll.Key.PageDown);
        Assert.AreEqual(0f, text.scrollOffset.y, 0.5f, "an open screen takes the keys, not the card under it");
        nav.Open(null);
        yield return Frames(4);
        PagesThrough(r.Root, text, null, true, "event text");
    }

    [UnityTest]
    public IEnumerator NothingScrollsUnderTheStartScreen()
    {
        using var r = new Rig();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        bridge.EndTurn();
        var rows = r.Root.Q<ScrollView>("why-rows");
        Shorten(rows, 40f);
        nav.Open("why");
        r.Root.Q("root").AddToClassList(StartScreenPresenter.OpenClass);
        yield return Frames(4);
        Assert.Greater(rows.verticalScroller.highValue, 0f, "precondition: the list overflows");
        foreach (KeyScroll.Key key in Enum.GetValues(typeof(KeyScroll.Key)))
            Assert.IsFalse(KeyScroll.Press(r.Root, "why", false, key), key + " under the start screen");
        Assert.AreEqual(0f, rows.scrollOffset.y, 0.5f, "the list did not move");
    }
}
