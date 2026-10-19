using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class UiFoundationTests
{
    [Test]
    public void PanelSettingsScaleWithA1080pReference()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
        Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ps.scaleMode);
        Assert.AreEqual(new Vector2Int(1920, 1080), ps.referenceResolution);
        Assert.AreEqual(PanelScreenMatchMode.MatchWidthOrHeight, ps.screenMatchMode);
        Assert.AreEqual(0.5f, ps.match);
        Assert.IsNotNull(ps.themeStyleSheet, "theme");
    }

    /// <summary>Playtest v0.1.1: every screen has an X in its header that closes it - a phone has no Escape, and
    /// re-tapping the lit rail button is not discoverable.</summary>
    [Test]
    public void EveryScreenHasACloseButtonThatClosesIt()
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        foreach (var s in ScreenNav.Screens)
        {
            nav.Toggle(s);
            Assert.AreEqual(s, nav.Current, s + " open");
            var x = ui.Q<Button>("screen-close-" + s);
            x.Focus();
            UiTestKit.Press(x);   // Press(Button) is static; only Press(string) is an instance method
            Assert.IsNull(nav.Current, s + ": the X closes the screen, like Escape");
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-" + s).style.display.value, s + " hidden");
            Assert.AreSame(ui.Q<Button>("nav-" + s), ui.Root.panel.focusController.focusedElement,
                s + ": the focus goes back to the screen's rail button, as after Escape (#1035)");
        }
    }

    /// <summary>#1035's ruling: the X's hit area is 44 screen px at every window size, so in panel units it grows as
    /// the panel shrinks. On the web a screen pixel is a CSS pixel, so the device-pixel ratio multiplies the target
    /// (44 device px was about 15 CSS px on a DPR-3 phone; the ruling's own phone figure is in CSS px).</summary>
    [Test]
    public void TheCloseButtonsHitAreaIs44ScreenPixelsAtEveryScale()
    {
        Assert.AreEqual(44f, ScreenNav.CloseTargetPanelPx(1f), 1e-3f);
        Assert.AreEqual(66f, ScreenNav.CloseTargetPanelPx(2f / 3f), 1e-2f);      // 1280x720's scale
        Assert.AreEqual(88f, ScreenNav.CloseTargetPanelPx(0.5f), 1e-3f);         // a small phone window's scale
        Assert.AreEqual(89.4f, ScreenNav.CloseTargetPanelPx(1.477f, 3f), 1e-1f); // a 390x844 phone at DPR 3: 44 CSS px
        Assert.AreEqual(132f, ScreenNav.CloseTargetPanelPx(2f / 3f, 2f), 1e-2f); // a DPR-2 laptop at 1280x720
        Assert.AreEqual(44f, ScreenNav.CloseTargetPanelPx(0f), "no scale yet: the USS size stands");
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        var scale = ui.Root.panel.scaledPixelsPerPoint;
        foreach (var s in ScreenNav.Screens)
        {
            var w = ui.Q<Button>("screen-close-" + s).style.width.value.value;   // StyleLength -> Length -> float, panel px
            Assert.AreEqual(44f, w * scale, 1e-2f, s + ": panel px x the panel's scale = 44 screen px");
        }
    }

    /// <summary>#1035: the X keeps clear of the screen's title in both languages - in Hebrew it sits on the left, and the
    /// header's padding keeps the title away from it. (The Hebrew overlap this guards was measured on all five screens.)</summary>
    [UnityTest]
    public IEnumerator TheCloseButtonKeepsClearOfTheTitleInBothLanguages()
    {
        foreach (var he in new[] { false, true })
        {
            using var ui = new UiTestKit();
            using var nav = new ScreenNav(ui.Root);
            if (he) ui.Q<VisualElement>("root").AddToClassList(UiLanguage.HebrewClass);
            var lang = he ? "HE" : "EN";
            foreach (var s in ScreenNav.Screens)
            {
                nav.Open(s);
                yield return null;   // as in the window-size test: a reopened screen lays out on the next frame
                var x = ui.Q<Button>("screen-close-" + s);
                var title = ui.Q<VisualElement>("screen-" + s).Q(className: "title");
                Assert.IsNotNull(title, s + " has a title");
                for (int i = 0; i < 10 && (float.IsNaN(x.worldBound.width) || float.IsNaN(title.worldBound.width)); i++) yield return null;
                Assert.IsFalse(x.worldBound.Overlaps(title.worldBound), $"{lang} {s}: the X is clear of the title");
            }
        }
    }

    /// <summary>#1035, at the ruling's own window sizes (1280x720 and a 390x844 phone), in EN and HE: the X renders
    /// at least 44 screen px, keeps clear of the title (the header's padding grows with it), and fits inside its
    /// header. The rig drives real window sizes: a copy of the PanelSettings with a RenderTexture target, as
    /// DecisionCardTests.Rig does. To see it red, remove the header padding/min-height lines in FitCloseButtons:
    /// at 1280x720 the 66 px X overlaps every title ("EN cabinet: the X is clear of the title").</summary>
    [UnityTest]
    public IEnumerator TheCloseButtonFitsItsHeaderAndMissesTheTitleAtTheRulingsWindowSizes()
    {
        foreach (var (w, h) in new[] { (1280, 720), (390, 844) })
        foreach (var he in new[] { false, true })
        {
            using var rig = new WindowRig(w, h, he);
            var lang = he ? "HE" : "EN";
            var scale = rig.Root.panel.scaledPixelsPerPoint;
            foreach (var s in ScreenNav.Screens)
            {
                rig.Nav.Open(s);
                yield return null;   // a screen that was display:none lays out on the next frame; its X's bounds are 0, not NaN, until then
                var x = rig.Root.Q<Button>("screen-close-" + s);
                var header = x.parent;
                var title = rig.Root.Q<VisualElement>("screen-" + s).Q(className: "title");
                for (int i = 0; i < 10 && (float.IsNaN(x.worldBound.width) || float.IsNaN(title.worldBound.width)); i++) yield return null;
                Assert.GreaterOrEqual(x.worldBound.width * scale, 44f - 1e-1f, $"{w}x{h} {lang} {s}: the X renders at least 44 screen px");
                Assert.IsFalse(x.worldBound.Overlaps(title.worldBound), $"{w}x{h} {lang} {s}: the X is clear of the title");
                Assert.LessOrEqual(x.worldBound.yMax, header.worldBound.yMax + 1e-1f, $"{w}x{h} {lang} {s}: the X fits inside its header");
                Assert.AreEqual(he, x.worldBound.center.x < header.worldBound.center.x, $"{w}x{h} {lang} {s}: the X sits on the left in Hebrew, the right in English");
            }
        }
    }

    /// <summary>The game's UI on a panel whose PanelSettings copy targets a RenderTexture of a real window size (the
    /// DecisionCardTests.Rig pattern), so scaledPixelsPerPoint is the window's true scale.</summary>
    sealed class WindowRig : System.IDisposable
    {
        public readonly VisualElement Root; public readonly ScreenNav Nav;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt; readonly HudPresenter hud;

        public WindowRig(int w, int h, bool hebrew)
        {
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("UiFoundationWindowTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root").RemoveFromClassList("loading");
            if (hebrew) Root.Q("root").AddToClassList(UiLanguage.HebrewClass);
            // The narrow-window classes (hud-narrow/-two-row/-three-row) only come from a live HudPresenter; without
            // one a small window keeps the desktop .screen insets and the screens' widths go negative.
            var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text);
            hud = new HudPresenter(Root, new SimBridge(Statecraft.Sim.GameSession.New(Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content),
                Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical), text), text);
            Nav = new ScreenNav(Root);
        }

        public void Dispose()
        {
            Nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    [Test]
    public void ASubmittedButtonFiresClickedSynchronously()
    {
        using var ui = new UiTestKit();
        int clicks = 0;
        ui.Q<Button>("end-turn").clicked += () => clicks++;
        ui.Press("end-turn");
        Assert.AreEqual(1, clicks);
    }

    [Test]
    public void TheLayoutHasEveryRegionOfTheTargetScreen()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "top-bar", "event-card", "info-panel", "minimap", "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name);
        foreach (var name in new[] { "treasury", "immigration", "security", "support", "cohesion", "standing", "fx" })
        {
            ui.Q<Label>(name + "-value");
            ui.Q<Label>(name + "-delta");
        }
        Assert.AreEqual(PickingMode.Ignore, ui.Q<VisualElement>("root").pickingMode, "empty screen areas must let clicks reach the map");
    }

    [UnityTest]
    public IEnumerator TextUsesTheSerifFaces()
    {
        using var ui = new UiTestKit();
        for (int i = 0; i < 10 && ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font == null; i++) yield return null;
        var regular = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontRegular);
        var semibold = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontSemiBold);
        Assert.AreSame(semibold, ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font);
        Assert.AreSame(regular, ui.Q<Label>("turn").resolvedStyle.unityFontDefinition.font);
    }

    [Test]
    public void StringsLoadFromTheYamlFileAndFormatInvariantly()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        Assert.AreEqual("Turn 3", text.Format("hud.turn", 3));
        Assert.AreEqual("1,234,567", text.Format("hud.people", 1234567.0));
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { var _ = text["no.such.key"]; });
    }

    [UnityTest]
    public IEnumerator PickingFindsButtonsButNotTheTransparentRoot()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name).style.display = DisplayStyle.None;   // closed, as at game start
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");        // ready, as GameRoot leaves it
        var button = ui.Q<Button>("end-turn");
        for (int i = 0; i < 10 && float.IsNaN(button.worldBound.width); i++) yield return null;
        var panel = ui.Root.panel;
        Assert.AreSame(button, panel.Pick(button.worldBound.center));
        Assert.IsNull(panel.Pick(ui.Root.worldBound.center), "the empty middle of the screen must reach the map");
        // Input System screen positions are y-up; ToPanel flips y before RuntimePanelUtils.ScreenToPanel (which does not).
        Assert.AreEqual(RuntimePanelUtils.ScreenToPanel(panel, new Vector2(10, 20)), UiPointer.ToPanel(panel, new Vector2(10, 1060), 1080f));
    }

    [UnityTest]
    public IEnumerator ButtonsUseTheStatecraftTheme()
    {
        using var ui = new UiTestKit();
        var navy = new Color(9 / 255f, 14 / 255f, 26 / 255f, 1f);
        var goldDim = new Color(196 / 255f, 160 / 255f, 92 / 255f, 0.45f);
        var gold = new Color(196 / 255f, 160 / 255f, 92 / 255f, 1f);
        // A screen button is a round, gold-rimmed navy disc with the screen's name under it (promo HUD pass): the disc
        // carries the theme's navy and dim gold.
        var nav = ui.Q<Button>("nav-cabinet").Q<UiIcon>(className: "nav-disc");
        Assert.IsNotNull(nav, "the screen button has its disc");
        for (int i = 0; i < 10 && nav.resolvedStyle.backgroundColor.a < 0.5f; i++) yield return null;
        void Near(Color e, Color a, string what) => Assert.That(Mathf.Abs(e.r - a.r) + Mathf.Abs(e.g - a.g) + Mathf.Abs(e.b - a.b) + Mathf.Abs(e.a - a.a), Is.LessThan(0.02f), $"{what}: expected {e} got {a}");
        Near(navy, nav.resolvedStyle.backgroundColor, "nav background");
        Near(goldDim, nav.resolvedStyle.borderTopColor, "nav border");
        var end = ui.Q<Button>("end-turn");
        Near(navy, end.resolvedStyle.backgroundColor, "end-turn background");
        Near(gold, end.resolvedStyle.borderTopColor, "end-turn border");
    }

    [Test]
    public void AMissingKeyNamesTheFileGivenToParse()
    {
        var text = UiText.Parse("a: b", "other.yaml");
        var e = Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { var _ = text["x"]; });
        StringAssert.Contains("other.yaml", e.Message);
    }

    /// <summary>Task 24 review M7: world-space settlement labels must not show through the top bar.</summary>
    [UnityTest]
    public IEnumerator TheTopBarIsOpaque()
    {
        using var ui = new UiTestKit();
        var bar = ui.Q<VisualElement>("top-bar");
        for (int i = 0; i < 10 && bar.resolvedStyle.backgroundColor.a < 0.5f; i++) yield return null;
        Assert.AreEqual(1f, bar.resolvedStyle.backgroundColor.a, 1e-3f);
    }

    /// <summary>Playtest 2, P2-1: until GameRoot has started the game, the tree shows only a styled loading line: no screen,
    /// no event card (and never its DRAFT placeholder), no empty top-bar values, info panel or minimap.</summary>
    [UnityTest]
    public IEnumerator BeforeTheGameIsReadyOnlyALoadingLineShows()
    {
        using var ui = new UiTestKit();
        var loading = ui.Q<Label>("loading");
        for (int i = 0; i < 10 && float.IsNaN(loading.worldBound.width); i++) yield return null;
        Assert.IsTrue(ui.Q<VisualElement>("root").ClassListContains("loading"), "the tree starts in the loading state");
        Assert.AreEqual(DisplayStyle.Flex, loading.resolvedStyle.display);
        Assert.AreEqual(1f, ui.Q<VisualElement>("root").resolvedStyle.backgroundColor.a, 1e-3f, "a navy page while loading, not the grey sky (final review M1)");
        Assert.IsNotEmpty(loading.text);
        foreach (var s in ScreenNav.Screens)
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-" + s).resolvedStyle.display, s);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-card").resolvedStyle.display, "event card");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-draft").resolvedStyle.display, "DRAFT placeholder");
        foreach (var cls in new[] { "date-box", "meters", "nav", "turn-box" })
            Assert.AreEqual(Visibility.Hidden, ui.Root.Q(className: cls).resolvedStyle.visibility, cls);
        foreach (var name in new[] { "info-panel", "right-column" })
            Assert.AreEqual(Visibility.Hidden, ui.Q<VisualElement>(name).resolvedStyle.visibility, name);
    }
}
