#nullable enable
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

/// <summary>The screen layout at 1920x1080 and 1280x720 (the panel scales with the screen against a 1080p reference,
/// so both resolve to the same logical layout): no fixed region overlaps another, no centre screen covers a fixed region,
/// and the six top-bar meters fit before the screen buttons at their minimum width. The decision card is not a fixed
/// region: it opens in the middle over a backdrop that covers the others (DecisionCardTests).</summary>
public class UiLayoutTests
{
    static readonly string[] Fixed = { "top-bar", "info-panel", "minimap" };
    static readonly string[] Screens = { "screen-cabinet", "screen-budget", "screen-defence", "screen-why" };

    [UnityTest]
    public IEnumerator NothingOverlapsAt1080pAnd720p()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1280, 1024), (1531, 784), (2560, 1080), (1920, 900) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
            try
            {
                Rect R(string name) => root.Q(name).worldBound;
                foreach (var a in Fixed)
                    foreach (var b in Fixed)
                        if (string.CompareOrdinal(a, b) < 0) Assert.IsFalse(R(a).Overlaps(R(b)), $"{w}x{h}: {a} {R(a)} overlaps {b} {R(b)}");
                // A wide screen (Cabinet, Defence: promo 04 and 08) opens over the info panel, drawn above it; no screen
                // covers the top bar or the minimap.
                foreach (var s in Screens)
                    foreach (var f in Fixed)
                    {
                        if (f == "info-panel" && root.Q(s).ClassListContains("screen-wide"))
                            continue;   // checked below: hidden while the wide screen is open
                        Assert.IsFalse(R(s).Overlaps(R(f)), $"{w}x{h}: {s} {R(s)} overlaps {f} {R(f)}");
                    }
                Assert.LessOrEqual(R("meters").xMax, R("nav").xMin + 0.5f, $"{w}x{h}: the meters run into the screen buttons");
                Assert.LessOrEqual(R("top-bar").xMin, root.Q(className: "date-box").worldBound.xMin, $"{w}x{h}: date box starts outside the top bar");
                Assert.LessOrEqual(root.Q(className: "date-box").worldBound.xMax, R("meters").xMin + 0.5f, $"{w}x{h}: date box runs into the meters");
                Assert.LessOrEqual(R("nav").xMax, root.Q(className: "turn-box").worldBound.xMin + 0.5f, $"{w}x{h}: the screen buttons run into the End Turn box");
                Assert.LessOrEqual(root.Q(className: "turn-box").worldBound.xMax, R("top-bar").xMax + 0.5f, $"{w}x{h}: End Turn box overflows the top bar");
                foreach (var m in root.Q("meters").Children())   // text may wrap in a narrow meter but must stay inside it and inside the bar
                {
                    Assert.LessOrEqual(m.worldBound.yMax, R("top-bar").yMax + 0.5f, $"{w}x{h}: {m.name} overflows the top bar vertically");
                    foreach (var l in m.Query<Label>().ToList())
                    {
                        Assert.GreaterOrEqual(l.worldBound.xMin + 0.5f, m.worldBound.xMin, $"{w}x{h}: '{l.text}' leaves {m.name} on the left");
                        Assert.LessOrEqual(l.worldBound.xMax, m.worldBound.xMax + 0.5f, $"{w}x{h}: '{l.text}' leaves {m.name} on the right");
                        if (l.ClassListContains("meter-name") && !string.IsNullOrEmpty(l.text))
                            Assert.GreaterOrEqual(l.worldBound.height + 0.5f, l.resolvedStyle.fontSize, $"{w}x{h}: '{l.text}' collapsed");
                    }
                }
                foreach (var m in root.Q("meters").Children())
                    Assert.GreaterOrEqual(m.worldBound.width, 128.5f, $"{w}x{h}: {m.name} is squeezed below its minimum width");   // 130 less pixel rounding at 2/3 scale
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    static readonly Content C = ContentLoader.Load(Paths.Content);

    /// <summary>Review I3: a wide screen (Cabinet, Defence) covers the info panel, so while it is open the panel is hidden:
    /// it is drawn under the screen, takes no keyboard focus (its build buttons cannot be reached with Tab), and a map
    /// click does not change it. A narrow screen (Budget) leaves it as it was.</summary>
    [UnityTest]
    public IEnumerator AWideScreenHidesTheInfoPanelItCovers()
    {
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("WideTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        // August 1950, when the map actions are open (in 1948 one shared refusal hides both build buttons, #398), so the
        // build button below is really on the panel before a screen covers it.
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new System.DateTime(1950, 8, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        var bridge = new SimBridge(g, Text);
        var info = new InfoPanelPresenter(root, bridge, Text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id));
        var nav = new ScreenNav(root);
        try
        {
            root.Q("root").RemoveFromClassList("loading");
            var tlv = C.SettlementHexes.Single(x => x.Id == "tel_aviv");
            info.Show(tlv.Q, tlv.R);
            for (int i = 0; i < 4; i++) yield return null;
            var panel = root.Q("info-panel");
            var build = root.Q<Button>("build-camp");
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "precondition: the panel shows");
            Assert.AreEqual(DisplayStyle.Flex, build.resolvedStyle.display, "precondition: the build button shows");
            Assert.IsTrue(build.canGrabFocus, "precondition: the build button can take the focus before a screen covers it");
            foreach (var s in new[] { "cabinet", "defence" })
            {
                nav.Open(s);
                for (int i = 0; i < 3; i++) yield return null;
                Assert.IsTrue(root.Q("screen-" + s).ClassListContains(ScreenNav.WideClass), s);
                Assert.Greater(root.Q("root").IndexOf(root.Q("screen-" + s)), root.Q("root").IndexOf(panel), $"{s} is drawn above the info panel");
                Assert.IsTrue(root.Q("screen-" + s).worldBound.Overlaps(panel.worldBound) || panel.resolvedStyle.display == DisplayStyle.None, s);
                Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, $"{s}: the covered info panel is hidden");
                Assert.IsFalse(build.canGrabFocus, $"{s}: its build button cannot take the keyboard focus");
                build.Focus();
                Assert.AreNotSame(build, build.focusController?.focusedElement, $"{s}: focus does not reach it");
                info.Show(tlv.Q + 3, tlv.R);
                Assert.AreEqual((tlv.Q, tlv.R), info.Selected, $"{s}: a map click does not change the hidden panel");
            }
            nav.Open("budget");
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "a narrow screen leaves the panel");
            nav.Open(null);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "closing brings it back");
            info.Show(tlv.Q + 3, tlv.R);
            Assert.AreEqual((tlv.Q + 3, tlv.R), info.Selected, "and map clicks reach it again");
        }
        finally { nav.Dispose(); info.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    /// <summary>#400: in the two-row HUD every open screen takes the full width, and the stylesheet hides the minimap column
    /// and the info panel under it (.hud-two-row.screen-open). In August 1950, with a hex selected and an event waiting (its
    /// card collapsed, so its backdrop covers nothing), their buttons are live, and none may take the focus while hidden:
    /// when Budget opens in two rows, and when a window already showing Budget beside the panels narrows into two rows (the
    /// tier changes under the open screen). Widening again, or closing the screen, brings them back. Any open screen covers
    /// the decision layer: its "Decision waiting" button is out of reach until the screen closes.</summary>
    [UnityTest]
    public IEnumerator TheTwoRowHudKeepsWhatAScreenCoversOutOfReach()
    {
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("TwoRowTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new System.DateTime(1950, 8, 1) || g.State.PendingEvents.Count == 0) g.Step(HistoricalAI.Commands(C, g.State));
        var bridge = new SimBridge(g, Text);
        var owned = new List<System.IDisposable>();
        void Resize(int w, int h) { var old = rt; rt = new RenderTexture(w, h, 0); ps.targetTexture = rt; old.Release(); Object.DestroyImmediate(old); }
        try
        {
            root.Q("root").RemoveFromClassList("loading");
            owned.Add(new HudPresenter(root, bridge, Text));
            var nav = new ScreenNav(root); owned.Add(nav);
            var info = new InfoPanelPresenter(root, bridge, Text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)); owned.Add(info);
            var card = new EventCardPresenter(root, bridge, Text, null); owned.Add(card);
            var tlv = C.SettlementHexes.Single(x => x.Id == "tel_aviv");
            info.Show(tlv.Q, tlv.R);
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsNotNull(root.Q("event-options").Q<Button>(), "precondition: an event's options are shown");
            card.Collapse();
            for (int i = 0; i < 2; i++) yield return null;
            var build = root.Q<Button>("build-camp");
            var waiting = root.Q<Button>("event-reopen");
            Assert.IsTrue(build.enabledSelf && build.style.display.value == DisplayStyle.Flex, "precondition: build-camp is live: " + root.Q<Label>("info-reason").text);
            var covered = new[] { "right-column", "info-panel" };
            List<Button> Buttons() => covered.SelectMany(r => root.Q(r).Query<Button>().ToList()).ToList();
            void Out(Button b, string where)
            {
                Assert.IsFalse(b.canGrabFocus, $"{where}: {b.name} cannot take the focus");
                b.Focus();
                Assert.AreNotSame(b, b.focusController?.focusedElement, $"{where}: {b.name} is not focused");
            }
            void Reach(bool expected, string where)
            {
                // In reach: the button known to be shown. Out of reach: every button in the covered panels.
                if (expected) Assert.IsTrue(build.canGrabFocus, $"{where}: {build.name} can take the focus");
                else foreach (var b in Buttons()) Out(b, where);
                // Under any open screen the decision layer is hidden and out of reach; with none, "Decision waiting" is live.
                if (nav.Current != null) Out(waiting, where);
                else Assert.IsTrue(waiting.canGrabFocus, $"{where}: Decision waiting can take the focus");
            }

            // Budget, a narrow screen, at 1080p: the panels stay beside it, in reach.
            nav.Open("budget");
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(root.Q("root").ClassListContains(HudPresenter.TwoRowClass), "precondition: one row at 1080p");
            Reach(true, "1080p, Budget open");

            // The window narrows under the open screen: two rows, the screen full width, the panels hidden and out of reach.
            Resize(960, 1080);
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsTrue(root.Q("root").ClassListContains(HudPresenter.TwoRowClass), "precondition: two rows at 960x1080");
            foreach (var r in covered) Assert.AreEqual(DisplayStyle.None, root.Q(r).resolvedStyle.display, r + " is hidden");
            Reach(false, "narrowed with Budget open");

            // Closed, then Budget opened again in two rows.
            nav.Open(null);
            for (int i = 0; i < 3; i++) yield return null;
            Reach(true, "two rows, no screen");
            nav.Open("budget");
            for (int i = 0; i < 3; i++) yield return null;
            Reach(false, "two rows, Budget opened");

            // Widened again with Budget open: one row, the panels back.
            Resize(1920, 1080);
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsFalse(root.Q("root").ClassListContains(HudPresenter.TwoRowClass), "precondition: one row again");
            Reach(true, "widened with Budget open");
        }
        finally
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The window sizes a player is likely to have: 16:9, 5:4, the playtest's wide-and-short browser window
    /// (1531x784, about 1.95) and 21:9.</summary>
    static readonly (int w, int h)[] Windows = { (1531, 784), (2560, 1080), (1920, 1080), (1280, 1024), (1920, 900) };

    /// <summary>The real Game.uxml on the real PanelSettings (and so the real theme), rendered at <paramref name="w"/> x
    /// <paramref name="h"/>, with the HUD and the Cabinet screen filled from the opening of the historical game.</summary>
    static IEnumerator Filled(int w, int h, string? screen, System.Action<VisualElement> check)
    {
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("LayoutTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var hud = new HudPresenter(root, bridge, Text);
        var nav = new ScreenNav(root);
        var cabinet = new CabinetPresenter(root, bridge, Text, null);
        nav.Open(screen);
        try
        {
            for (int i = 0; i < 20 && (float.IsNaN(root.Q("minimap").worldBound.width) || root.Q("year").worldBound.height <= 0); i++) yield return null;
            yield return null;   // one more pass, so text measured after the first layout settles
            check(root);
        }
        finally
        {
            cabinet.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>Playtest 1, item 1: every top-bar label (the year, the date, the security level) lies inside the bar,
    /// clear of the window's top edge, and nothing else starts above the bar's bottom edge.</summary>
    [UnityTest]
    public IEnumerator TheTopBarHoldsItsContentInWideAndShortWindows()
    {
        foreach (var (w, h) in Windows)
            yield return Filled(w, h, null, root =>
            {
                var bar = root.Q("top-bar").worldBound;
                Assert.GreaterOrEqual(bar.yMin, -0.5f, $"{w}x{h}: the top bar starts above the window");
                var labels = root.Q("top-bar").Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text)).ToList();
                Assert.Greater(labels.Count, 10, "the HUD filled the bar");
                // The date plate hangs below the one-row bar, as promo 03's does (HUD round 2, part 3): its lines stay in the plate.
                var plate = root.Q(className: "date-box").worldBound;
                Assert.IsTrue(Inside(plate, root.worldBound), $"{w}x{h}: the plate {plate} leaves the window");
                foreach (var l in labels)
                {
                    bool inPlate = l.GetFirstAncestorOfType<VisualElement>() != null && root.Q(className: "date-box").Contains(l);
                    Assert.IsTrue(Inside(l.worldBound, inPlate ? plate : bar), $"{w}x{h}: '{l.text}' {l.worldBound} is outside the {(inPlate ? "plate " + plate : "top bar " + bar)}");
                    Assert.GreaterOrEqual(l.worldBound.yMin, bar.yMin + 2f, $"{w}x{h}: '{l.text}' sits flush against the top edge");
                }
                foreach (var name in new[] { "event-card", "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
                    if (root.Q(name).resolvedStyle.display != DisplayStyle.None)
                        Assert.GreaterOrEqual(root.Q(name).worldBound.yMin, bar.yMax - 0.5f, $"{w}x{h}: {name} starts under the top bar");
            });
    }

    /// <summary>Playtest 1, item 2: the Cabinet lineup is a list inside the screen panel, below its header, filling the
    /// panel; rows that do not fit scroll instead of spilling over the top bar.</summary>
    [UnityTest]
    public IEnumerator TheCabinetListStaysInsideItsPanel()
    {
        foreach (var (w, h) in Windows)
            yield return Filled(w, h, "cabinet", root =>
            {
                var screen = root.Q("screen-cabinet").worldBound;
                var header = root.Q("screen-cabinet").Q(className: "screen-header").worldBound;
                var list = root.Q<ScrollView>("cabinet-seats");
                var view = list.contentViewport.worldBound;
                Assert.IsTrue(Inside(header, screen), $"{w}x{h}: the header {header} is outside the screen {screen}");
                Assert.IsTrue(Inside(view, screen), $"{w}x{h}: the list {view} is outside the screen {screen}");
                Assert.GreaterOrEqual(view.yMin, header.yMax - 0.5f, $"{w}x{h}: the list starts above the screen header");
                Assert.GreaterOrEqual(view.yMax, root.Q("cabinet-status").worldBound.yMin - 16f, $"{w}x{h}: the list leaves the panel empty below it");
                var rows = list.Query<Button>(className: "seat").ToList();
                Assert.Greater(rows.Count, 5, "the lineup was rendered");
                foreach (var r in rows)
                {
                    Assert.GreaterOrEqual(r.worldBound.yMin, view.yMin - 0.5f, $"{w}x{h}: {r.name} sits above the list");
                    Assert.GreaterOrEqual(r.worldBound.xMin, screen.xMin - 0.5f, $"{w}x{h}: {r.name} leaves the panel on the left");
                    Assert.LessOrEqual(r.worldBound.xMax, screen.xMax + 0.5f, $"{w}x{h}: {r.name} leaves the panel on the right");
                    if (r.worldBound.yMax > view.yMax + 0.5f)
                        Assert.Greater(list.verticalScroller.highValue, 0f, $"{w}x{h}: {r.name} runs past the panel and the list does not scroll");
                }
            });
    }

    /// <summary>Playtest 1, item 3: the camera learns how much of the window the top bar covers, at every window shape,
    /// so the map is framed below it; disposing gives the whole screen back.</summary>
    [UnityTest]
    public IEnumerator TheCameraIsToldWhatTheTopBarCovers()
    {
        var camGo = new GameObject("InsetCamera");
        camGo.AddComponent<Camera>();
        var rig = camGo.AddComponent<Statecraft.Game.Map.CameraRig>();
        try
        {
            foreach (var (w, h) in Windows)
            {
                yield return Filled(w, h, null, root =>
                {
                    var inset = new TopBarInset(root, rig);
                    var bar = root.Q("top-bar").worldBound.height;
                    var screen = root.panel.visualTree.worldBound.height;
                    Assert.Greater(bar, 60f, $"{w}x{h}: the bar is laid out (70 px one-row since HUD round 2, part 3)");
                    Assert.AreEqual(bar / screen, rig.TopInset, 1e-3f, $"{w}x{h}: the inset is the bar's share of the window");
                    inset.Dispose();
                    Assert.AreEqual(0f, rig.TopInset, $"{w}x{h}: disposing gives the screen back");
                });
            }
        }
        finally { Object.DestroyImmediate(camGo); }
    }

    // Task 24, seen in headed Chrome: the top bar's labels were taller than the bar and, centred, ran off the top of the
    // screen at every size; the Cabinet list grew out of its screen over the top bar. Checked with every screen filled by
    // its presenter (an event open on the card), at 16:9 and at the wider viewports a browser window leaves.
    [UnityTest]
    public IEnumerator FilledScreensStayInsideTheirPanelsAndTheTopBarFits()
    {
        var c = ContentLoader.Load(Paths.Content);
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        // Bounded (Task 24 review M5): content that no longer raises these events fails the test instead of hanging the run.
        for (int t = 0; !g.State.PendingEvents.Contains("armistice_egypt"); t++)
        {
            if (t >= 500) Assert.Fail("armistice_egypt was not raised within 500 turns");
            g.Step(new Command[0]);
        }
        g.Step(new Command[0]);                                         // a Why report to show
        for (int t = 0; g.State.PendingEvents.Count == 0; t++)
        {
            if (t >= 500) Assert.Fail("no event was raised within 500 turns of the armistice");
            g.Step(new Command[0]);
        }
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1920, 960), (1600, 757), (1366, 657), (390, 844), (960, 600) })   // #210: and narrow windows
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var bridge = new SimBridge(g, text);
            var disposables = new List<System.IDisposable>
            {
                new HudPresenter(root, bridge, text), new EventCardPresenter(root, bridge, text, null),
                new CabinetPresenter(root, bridge, text, null), new BudgetPresenter(root, bridge, text),
                new DefencePresenter(root, bridge, text), new WhyPresenter(root, bridge, text),
            };
            var nav = new ScreenNav(root);
            try
            {
                for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
                yield return null;
                Rect R(VisualElement e) => e.worldBound;
                bool Inside(Rect inner, Rect outer) => inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;
                var bar = root.Q("top-bar");
                var plate = root.Q(className: "date-box");   // hangs below the one-row bar, as promo 03's (HUD round 2, part 3)
                if (!Inside(R(plate), R(root))) problems.Add($"{w}x{h}: the plate {R(plate)} leaves the window");
                foreach (var l in bar.Query<VisualElement>().ToList().Where(e => e != bar && e.resolvedStyle.display == DisplayStyle.Flex))
                {
                    var holder = l == plate || plate.Contains(l) ? (l == plate ? bar : plate) : bar;
                    if (l == plate) continue;
                    if (!Inside(R(l), R(holder))) problems.Add($"{w}x{h}: top bar child {l.name}/{(l as Label)?.text ?? l.GetClasses().FirstOrDefault()} {R(l)} leaves the {(holder == plate ? "plate" : "bar")} {R(holder)}");
                }
                var card = root.Q("event-card");
                if (!Inside(R(card), R(root.Q("event-layer")))) problems.Add($"{w}x{h}: event card {R(card)} leaves the decision layer {R(root.Q("event-layer"))}");
                if (R(card).yMin < R(bar).yMax - 0.5f) problems.Add($"{w}x{h}: event card {R(card)} starts over the top bar {R(bar)}");
                foreach (var s in ScreenNav.Screens)
                {
                    nav.Open(s);
                    for (int i = 0; i < 3; i++) yield return null;
                    var screen = root.Q("screen-" + s);
                    foreach (var e in screen.Children().Concat(screen.Query<ScrollView>().ToList()).Where(e => e.resolvedStyle.display == DisplayStyle.Flex))
                        if (!Inside(R(e), R(screen))) problems.Add($"{w}x{h}: {s} child {e.name}/{e.GetClasses().FirstOrDefault()} {R(e)} leaves the screen {R(screen)}");
                }
                nav.Open(null);
            }
            finally
            {
                nav.Dispose(); foreach (var d in disposables) d.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>A button's text fits its content box: on one line, or wrapped (white-space normal) within its height.</summary>
    static bool TextFits(TextElement b)
    {
        var box = b.contentRect;
        var natural = b.MeasureTextSize(b.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        if (natural.x <= box.width + 1f) return true;
        if (b.resolvedStyle.whiteSpace != WhiteSpace.Normal) return false;
        var wrapped = b.MeasureTextSize(b.text, box.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
        return wrapped.y <= box.height + 1f;
    }

    /// <summary>The window shapes the HUD supports (PromoHudTests.EveryWindowShapeFitsTheBarAfterATurn: one row, the
    /// stacked bar, two rows), a phone upright (three rows), a small laptop, the playtest's wide-and-short browser windows
    /// and 21:9.</summary>
    static readonly (int w, int h)[] CardWindows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (390, 844), (960, 600), (1531, 784), (1568, 752), (2560, 1080), (1920, 900), (1366, 657),
    };

    /// <summary>Decision cards (2026-10-13), after Task 24 review I2: every real event, the tallest included, fits the
    /// decision layer at every window shape, the narrow two- and three-row HUDs included: the card is centred, at most
    /// 1040 px wide, below the top bar and inside the layer; its option buttons stay inside it with their text; and its
    /// media header is 16:9 whenever the card has the height for it (on a full card it gives way first, as before).</summary>
    [UnityTest]
    public IEnumerator EveryEventCardFitsTheDecisionLayer()
    {
        var problems = new List<string>();
        foreach (var (w, h) in CardWindows)
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            // The real portraits: a minister's portrait adds the 200 px image row, so the tallest card is a portrait card.
            var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset");
            Assert.IsNotNull(portraits);
            // And the real event photographs: a photograph card adds the 200 px header band and its credit line.
            var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
            Assert.IsNotNull(photos);
            var hud = new HudPresenter(root, bridge, Text);   // the bar's tier (one, two or three rows) sets the layer's top
            var card = new EventCardPresenter(root, bridge, Text, portraits, photos);
            float tallest = 0; string tallestId = "";
            try
            {
                for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
                foreach (var e in C.Events)
                {
                    var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
                    g.State.Date = e.From;                       // the text names the office holders of the event's own time
                    g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
                    bridge.Replace(g);
                    for (int i = 0; i < 4; i++) yield return null;   // the fourth: a photograph dropped for height settles
                    CheckPhoto($"{w}x{h}: {e.Id}", root, card, problems);
                    CheckCard($"{w}x{h}: {e.Id}", root, problems);
                    var c = root.Q("event-card").worldBound;
                    if (c.height > tallest) { tallest = c.height; tallestId = e.Id; }
                    foreach (var b in root.Q("event-options").Query<Button>().ToList())
                    {
                        if (b.worldBound.yMax > c.yMax + 0.5f || b.worldBound.yMin < c.yMin - 0.5f || b.worldBound.xMax > c.xMax + 0.5f || b.worldBound.xMin < c.xMin - 0.5f)
                            problems.Add($"{w}x{h}: {e.Id}: option '{b.text}' {b.worldBound} is cut off by the card {c}");
                        // Playtest 2, P2-2: the option's text lies inside its button (it wraps; it is never cut at the edge).
                        if (!TextFits(b))
                            problems.Add($"{w}x{h}: {e.Id}: option text '{b.text}' does not fit its button {b.contentRect}");
                    }
                    // Playtest 2, P2-3: in a window at least as tall (logically) as 1366x657, the body and the options fit
                    // without scrolling; shorter windows may scroll the body as the fallback.
                    var scroll = root.Q<ScrollView>("event-scroll");
                    var logicalHeight = root.panel.visualTree.layout.height;
                    if (logicalHeight >= EventCardPresenter.ScrollFallbackHeight && scroll.contentContainer.layout.height > scroll.contentViewport.layout.height + 1f)
                        problems.Add($"{w}x{h} (logical height {logicalHeight:0}): {e.Id}: the body scrolls ({scroll.contentContainer.layout.height:0} px of text in {scroll.contentViewport.layout.height:0} px)");
                }
                Debug.Log($"I2 {w}x{h}: tallest card {tallestId} {tallest:0} px");
                Assert.Greater(tallest, 300f, $"{w}x{h}: the cards were laid out (a short window shrinks the tallest to fit)");
            }
            finally
            {
                card.Dispose(); hud.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The decision card's place: in the decision layer, centred across it, at most CardMaxWidth logical px wide and
    /// below the top bar, Hide inside it; a shown media header is 16:9 unless the card is as tall as the layer (then it gave
    /// way first), and while it shows the body does not scroll.</summary>
    // Tolerances: whole pixels at the panel's scale (2/3 at 1280x720; about 2 logical px per pixel at 390x844).
    static void CheckCard(string where, VisualElement root, List<string> problems)
    {
        var card = root.Q("event-card"); var layer = root.Q("event-layer"); var bar = root.Q("top-bar");
        var c = card.worldBound; var l = layer.worldBound;
        if (!Inside(c, l)) problems.Add($"{where}: the card {c} leaves the decision layer {l}");
        if (Mathf.Abs((c.xMin - l.xMin) - (l.xMax - c.xMax)) > 1f) problems.Add($"{where}: the card {c} is not centred in the layer {l}");
        if (c.width > CardMaxWidth + 1.5f) problems.Add($"{where}: the card is {c.width:0} px wide, over {CardMaxWidth}");
        if (c.yMin < bar.worldBound.yMax - 0.5f) problems.Add($"{where}: the card {c} starts over the top bar {bar.worldBound}");
        var hide = root.Q("event-hide").worldBound;
        if (!Inside(hide, c)) problems.Add($"{where}: Hide {hide} is cut off by the card {c}");
        // The header gives way before the text on every window: while a photograph shows, the body never scrolls.
        var scroll = root.Q<ScrollView>("event-scroll");
        if (root.Q("event-photo").resolvedStyle.display == DisplayStyle.Flex && scroll.contentContainer.layout.height > scroll.contentViewport.layout.height + 1f)
            problems.Add($"{where}: the body scrolls ({scroll.contentContainer.layout.height:0} px of text in {scroll.contentViewport.layout.height:0} px) while the photograph shows");
        var band = root.Q("event-photo");
        if (band.resolvedStyle.display == DisplayStyle.Flex && c.height < l.height - 1f)
        {
            var want = band.layout.width / EventCardPresenter.MediaAspect;
            if (Mathf.Abs(band.layout.height - want) > 2.5f) problems.Add($"{where}: the media header is {band.layout.width:0}x{band.layout.height:0}, not 16:9 ({want:0} px tall), with room to spare");
        }
    }

    /// <summary>The card's largest width (Statecraft.uss .event-card), logical px.</summary>
    const float CardMaxWidth = 1040f;

    /// <summary>Review I2 and I6: a shown photograph keeps at least MinPhotoHeight of picture above its whole credit line
    /// (CC BY 4.0 needs the credit), the credit's text fits the strip, and PhotoAlt says whether it is shown.</summary>
    static void CheckPhoto(string where, VisualElement root, EventCardPresenter card, List<string> problems)
    {
        var band = root.Q("event-photo");
        var credit = root.Q<Label>("event-photo-credit");
        if (band.resolvedStyle.display == DisplayStyle.None)
        {
            if (card.PhotoAlt != null) problems.Add($"{where}: PhotoAlt is set but the photograph is hidden");
            return;
        }
        if (card.PhotoAlt == null) problems.Add($"{where}: the photograph band shows with no PhotoAlt");
        if (band.layout.height + 0.5f < credit.layout.height + EventCardPresenter.MinPhotoHeight)
            problems.Add($"{where}: the photograph band is {band.layout.height:0} px, under its credit ({credit.layout.height:0} px) + {EventCardPresenter.MinPhotoHeight} px");
        if (!Inside(credit.worldBound, band.worldBound)) problems.Add($"{where}: the credit {credit.worldBound} is cut by the band {band.worldBound}");
        if (!TextFits(credit)) problems.Add($"{where}: the credit '{credit.text}' does not fit its strip {credit.contentRect}");
    }

    /// <summary>Review I6 and the re-review's ultrawide finding, for the 16:9 header: on a short window (logically under
    /// EventCardPresenter.ScrollFallbackHeight) a photograph is either shown with its whole credit (the header shorter,
    /// cropped, the body not scrolling) or dropped rather than squeezed (PhotoAlt is null); the credit is never cut and the
    /// card stays in the decision layer with its options and Hide inside it. An ultrawide 2560x1080 window shows
    /// photographs.</summary>
    [UnityTest]
    public IEnumerator AShortWindowShowsThePhotographWholeOrDropsIt()
    {
        var problems = new List<string>();
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        Assert.IsNotNull(photos);
        foreach (var (w, h) in new[] { (1280, 600), (2560, 1080) })   // logically about 985 and 935 px tall
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var card = new EventCardPresenter(root, bridge, Text, null, photos);
            int dropped = 0, shown = 0;
            try
            {
                for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
                foreach (var e in C.Events.Where(e => photos.Find(e.Id) != null))
                {
                    var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
                    g.State.Date = e.From;
                    g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
                    bridge.Replace(g);
                    for (int i = 0; i < 4; i++) yield return null;
                    CheckPhoto($"{w}x{h}: {e.Id}", root, card, problems);
                    if (card.PhotoAlt == null) dropped++; else shown++;
                    CheckCard($"{w}x{h}: {e.Id}", root, problems);
                    var c = root.Q("event-card").worldBound;
                    foreach (var b in root.Q("event-options").Query<Button>().ToList())
                        if (!Inside(b.worldBound, c)) problems.Add($"{w}x{h}: {e.Id}: option '{b.text}' {b.worldBound} is cut off by the card {c}");
                }
                Debug.Log($"I6 {w}x{h}: {shown} photographs shown, {dropped} dropped");
                Assert.Greater(shown, 0, $"{w}x{h}: photographs show on this window");
                Assert.Less(root.panel.visualTree.layout.height, EventCardPresenter.ScrollFallbackHeight, $"{w}x{h} is a short window");
            }
            finally
            {
                card.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Review I2: a photograph dropped because the window got shorter comes back when it grows again, without a
    /// new event (the card listens to the decision layer's size, which only the window changes).</summary>
    [UnityTest]
    public IEnumerator ADroppedPhotographReturnsWhenTheWindowGrows()
    {
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("LayoutTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == "armistice_egypt").From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add("armistice_egypt");
        var card = new EventCardPresenter(root, new SimBridge(g, Text), Text, null, photos);
        var column = root.Q("event-layer");
        try
        {
            for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsNotNull(card.PhotoAlt, "1080p has room for the photograph");
            column.style.bottom = 700;                    // as a much shorter window would leave the layer (268 px)
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsNull(card.PhotoAlt, "a short column drops the photograph");
            Assert.AreEqual(DisplayStyle.None, root.Q("event-photo").resolvedStyle.display);
            Assert.AreEqual("", root.Q("event-photo").tooltip, "the alt text goes with it");
            column.style.bottom = StyleKeyword.Null;      // the window grows back
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsNotNull(card.PhotoAlt, "the photograph is back without a new event");
            Assert.AreEqual(DisplayStyle.Flex, root.Q("event-photo").resolvedStyle.display);
            var problems = new List<string>();
            CheckPhoto("1920x1080 regrown", root, card, problems);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
        finally
        {
            card.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
