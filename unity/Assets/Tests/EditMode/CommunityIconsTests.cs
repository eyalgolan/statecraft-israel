#nullable enable
using System.Collections;
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
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>#1236 (v0.1.4): Star, Report and Share as three round icons, one above the other, at the foot of the map
/// screen's left icon lane, under the side-menu discs. Each does what its Settings button does (ExternalLink to the
/// repository, the Report panel, the Share panel); each one's name is its text (drawn clear) and its tooltip, which HoverTip
/// shows beside it, in English and Hebrew. They stay at the bottom left in Hebrew, as the side menu does (#550).</summary>
public class CommunityIconsTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly HexFile Hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static string MapFile(string name) => File.ReadAllText(Path.Combine(Paths.Content, "map", name));
    static MapRegions Regions() => new MapRegions(Subdistricts1948.Parse(MapFile(Subdistricts1948.FileName)), Armistice1949.Parse(MapFile(Armistice1949.FileName)));
    static Dictionary<string, string> Names() => SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName)).ToDictionary(t => t.Id, t => t.Name);

    const string Column = "community-column";   // CommunityIconsPresenter.ColumnName

    /// <summary>Top to bottom: the button, the string its name and tooltip are (the Settings buttons' own), its glyph.</summary>
    static readonly (string name, string key, string glyph)[] Icons =
    {
        ("hud-star", "community.star", "star"), ("hud-report", "feedback.open", "speech"), ("hud-share", "share.open", "share"),
    };

    /// <summary>The layout suites' sizes (InfoPanelLayoutTests, LeftColumnTests: the smaller upright phones) and two phones
    /// held in landscape (TopBarTests' 844x390, and 667x375).</summary>
    static readonly (int w, int h)[] Landscape = { (844, 390), (667, 375) };
    static readonly (int w, int h)[] Windows = InfoPanelLayoutTests.Windows.Concat(new[] { (360, 800), (375, 667) }).Concat(Landscape).ToArray();

    static GameSession WithAnEventOpen()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; g.State.PendingEvents.Count == 0; t++)
        {
            if (t >= 50) Assert.Fail("no event within 50 turns");
            g.Step(new Command[0]);
        }
        return g;
    }

    /// <summary>The real Game.uxml on the real PanelSettings at w x h, with the top bar, the side menu, the decision card (open
    /// when the session has an event), the info panel on the hex given, the minimap and the border caption.</summary>
    static IEnumerator Rendered(int w, int h, bool hebrew, GameSession g, (int q, int r)? hex, System.Action<VisualElement> check, int uiScale = 100)
    {
        var text = hebrew ? He : En;
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        if (uiScale != 100) ps.referenceResolution = new Vector2Int(Mathf.RoundToInt(1920f * 100f / uiScale), Mathf.RoundToInt(1080f * 100f / uiScale));   // #40: the panel as the Settings scale choice sets it
        var go = new GameObject("CommunityIconsTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var bridge = new SimBridge(g, text);
        var hud = new HudPresenter(root, bridge, text);
        var nav = new ScreenNav(root);
        var card = new EventCardPresenter(root, bridge, text, null);
        var info = new InfoPanelPresenter(root, bridge, text, Names(), regions: Regions());
        var mini = new MinimapPresenter(root, Hexes, text);
        var layer = root.Q<Label>("map-layer");
        layer.text = "Mandate sub-districts, 1947";
        layer.style.display = DisplayStyle.Flex;
        try
        {
            if (hex is { } x) info.Show(x.q, x.r);
            for (int i = 0; i < 30 && !(root.Q("nav-cabinet").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            check(root);
        }
        finally
        {
            mini.Dispose(); info.Dispose(); card.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static Rect Hull(IEnumerable<VisualElement> parts) => parts.Select(b => b.worldBound)
        .Aggregate((a, b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax)));

    /// <summary>At every size, in English and Hebrew, with Lydda's hex and Tel Aviv's (the GDD city panel) chosen and a card
    /// open: the three icons lie in the window, at its bottom left, one above the other, at least 24 screen px each, and
    /// overlap neither the info panel, the side menu, the minimap, the border caption, the top bar nor the card. The info
    /// panel, moved right of the lane to make room, clears the side menu too.</summary>
    [UnityTest]
    public IEnumerator TheIconsFitEveryWindowAtTheBottomLeftClearOfThePanels()
    {
        var problems = new List<string>();
        var g = WithAnEventOpen();
        var lod = C.SettlementHexes.Single(s => s.Id == "lod"); var city = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                foreach (var (what, q, r) in new[] { ("Lydda", lod.Q, lod.R), ("Tel Aviv", city.Q, city.R) })
                    yield return Rendered(w, h, hebrew, g, (q, r), root =>
                    {
                        string at = $"{(hebrew ? "he" : "en")} {w}x{h} {what}";
                        var window = root.worldBound;
                        float scale = w / window.width;
                        var buttons = Icons.Select(i => root.Q<Button>(i.name)).ToList();
                        if (buttons.Any(b => b == null)) { problems.Add($"{at}: precondition: the three icons exist"); return; }
                        foreach (var shown in new[] { "event-card", "info-panel", "minimap", "map-layer" })
                            if (!(root.Q(shown).worldBound.width > 0)) problems.Add($"{at}: precondition: {shown} shows");
                        var hull = Hull(buttons);
                        TestContext.WriteLine($"{at}: icons {hull}, info {root.Q("info-panel").worldBound}, column {Hull(root.Q("left-column").Query<Button>().ToList())}");
                        if (!(hull.xMin >= window.xMin && hull.xMax <= window.xMax && hull.yMin >= window.yMin && hull.yMax <= window.yMax)) problems.Add($"{at}: the icons {hull} leave the window {window}");
                        if (hull.xMin - window.xMin > 0.03f * window.width) problems.Add($"{at}: the icons {hull} are not at the left edge");
                        if (window.yMax - hull.yMax > 0.03f * window.height) problems.Add($"{at}: the icons {hull} are not at the bottom");
                        for (int i = 1; i < buttons.Count; i++)
                            if (!(buttons[i].worldBound.yMin >= buttons[i - 1].worldBound.yMax) || Mathf.Abs(buttons[i].worldBound.center.x - buttons[0].worldBound.center.x) > 0.5f)
                                problems.Add($"{at}: {buttons[i].name} {buttons[i].worldBound} is not under {buttons[i - 1].name} {buttons[i - 1].worldBound}");
                        foreach (var b in buttons)
                        {
                            // The 24 screen px floor at the sizes the side menu keeps it (LeftColumnTests); on a phone held in landscape
                            // the one-row bar's 45 px discs come out smaller, the side menu's too: there the icons are its size.
                            if (Landscape.Contains((w, h))) { if (b.worldBound.width < root.Q<Button>("nav-cabinet").worldBound.width - 0.5f) problems.Add($"{at}: {b.name} is smaller than the side menu's discs"); }
                            else if (b.worldBound.width * scale < 24f || b.worldBound.height * scale < 24f) problems.Add($"{at}: {b.name} is {b.worldBound.width * scale:0} px, under 24");
                            if (b.resolvedStyle.color.a > 0.01f) problems.Add($"{at}: {b.name}'s caption shows");
                        }
                        var column = Hull(root.Q("left-column").Query<Button>().ToList());
                        foreach (var (name, other) in new[] { ("the info panel", root.Q("info-panel").worldBound), ("the side menu", column), ("the minimap", root.Q("minimap").worldBound),
                                                              ("the border caption", root.Q("map-layer").worldBound), ("the top bar", root.Q("top-bar").worldBound), ("the decision card", root.Q("event-card").worldBound) })
                            if (other.Overlaps(hull)) problems.Add($"{at}: the icons {hull} overlap {name} {other}");
                        if (root.Q("info-panel").worldBound.Overlaps(column)) problems.Add($"{at}: the info panel {root.Q("info-panel").worldBound} overlaps the side menu {column}");
                    });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>#40, owner decision of 22 Oct 2026 (a condition on #1135): at 150% on a 1280x720 or 1920x1080 window (the tight
    /// HUD) the three icons stay clear of the tightened side menu above them, with a gap, in English and Hebrew.</summary>
    [UnityTest]
    public IEnumerator AtOneHundredFiftyPercentTheIconsClearTheTightSideMenu()
    {
        var problems = new List<string>();
        var g = WithAnEventOpen();
        var lod = C.SettlementHexes.Single(s => s.Id == "lod");
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
                yield return Rendered(w, h, hebrew, g, (lod.Q, lod.R), root =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h} at 150%";
                    if (!root.Q("root").ClassListContains(HudPresenter.TightClass)) problems.Add($"{at}: precondition: the HUD is tight");
                    var icons = Hull(Icons.Select(i => root.Q<Button>(i.name)));
                    var column = Hull(root.Q("left-column").Query<Button>().ToList());
                    TestContext.WriteLine($"{at}: side menu {column}, icons {icons}, gap {icons.yMin - column.yMax:0.0} px");
                    if (column.Overlaps(icons) || icons.yMin < column.yMax) problems.Add($"{at}: the icons {icons} overlap the side menu {column}");
                }, 150);
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Each icon is a round disc with its own glyph, named in both languages by the Settings button's string (its
    /// text, drawn clear, and its tooltip); Star opens the repository (a click, Enter or Space), Report and Share open their
    /// panels, and Close gives the focus back to the icon. Disposed, the icons are unwired.</summary>
    [Test]
    public void EachIconIsNamedAndDoesWhatItsSettingsButtonDoes([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var opened = new List<string>();
        ExternalLink.Opener = opened.Add;
        ShareShot.Keep = (png, name) => { };
        ShareShot.CanShareFiles = () => true;
        try
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            using var feedback = new FeedbackPresenter(ui.Root, text, "v-test");
            using var share = new SharePresenter(ui.Root, bridge, text, done => done(new byte[] { 1 }));
            var icons = new CommunityIconsPresenter(ui.Root, text, feedback, share);
            CollectionAssert.AreEqual(Icons.Select(i => i.name).ToArray(), ui.Q<VisualElement>(Column).Query<Button>().ToList().Select(b => b.name).ToArray(), "the icons, top to bottom");
            foreach (var (name, key, glyph) in Icons)
            {
                var b = ui.Q<Button>(name);
                Assert.AreEqual(text[key], b.text, name + ": its name is the Settings button's");
                Assert.AreEqual(b.text, b.tooltip, name + ": the tooltip says the name");
                Assert.AreEqual(glyph, b.Q<UiIcon>()?.Glyph, name + ": its glyph");
                Assert.IsTrue(b.focusable, name + ": Tab reaches it");
            }
            CollectionAssert.Contains(UiIcon.Glyphs, "speech"); CollectionAssert.Contains(UiIcon.Glyphs, "share");

            var star = ui.Q<Button>("hud-star");
            SettingsScreenTests.PressWithPointer(star);
            UiTestKit.Press(star);
            CollectionAssert.AreEqual(new[] { ExternalLink.Repository, ExternalLink.Repository }, opened, "a click, then Enter or Space");

            var report = ui.Q<Button>("hud-report");
            UiTestKit.Press(report);
            Assert.IsTrue(feedback.IsOpen, "Report opens the report panel");
            feedback.Close();
            Assert.AreEqual(report, report.focusController.focusedElement, "Close gives the focus back to the icon");

            var shareIcon = ui.Q<Button>("hud-share");
            UiTestKit.Press(shareIcon);
            Assert.IsTrue(share.IsOpen, "Share opens the share panel");
            share.Close();
            Assert.AreEqual(shareIcon, shareIcon.focusController.focusedElement, "Close gives the focus back to the icon");

            icons.Dispose();
            UiTestKit.Press(star); UiTestKit.Press(report);
            Assert.AreEqual(2, opened.Count, "disposed, Star is unwired");
            Assert.IsFalse(feedback.IsOpen, "disposed, Report is unwired");
            UiTestKit.Press(shareIcon);
            Assert.IsFalse(share.IsOpen, "disposed, Share is unwired");
        }
        finally
        {
            ExternalLink.Opener = ExternalLink.Default;
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.CanShareFiles = ShareShot.DefaultCanShareFiles;
        }
    }

    /// <summary>The icons follow the side menu: usable under the decision card (they sit in its lane, which the card's
    /// backdrop leaves free), out of reach under the start screen, an outcome clip and the turn report, hidden under the
    /// start screen and while loading; Tab reaches them after the info panel's actions. Each shows its name on hover.</summary>
    [UnityTest]
    public IEnumerator TheIconsAreCoveredWhereTheSideMenuIsAndShowTheirNames()
    {
        var g = WithAnEventOpen();
        var lod = C.SettlementHexes.Single(s => s.Id == "lod");
        foreach (var hebrew in new[] { false, true })
            yield return Rendered(1920, 1080, hebrew, g, (lod.Q, lod.R), root =>
            {
                string at = hebrew ? "he" : "en";
                var text = hebrew ? He : En;
                using var feedback = new FeedbackPresenter(root, text, "v-test");
                using var share = new SharePresenter(root, new SimBridge(g, text), text, done => done(new byte[] { 1 }));
                using var icons = new CommunityIconsPresenter(root, text, feedback, share);
                var column = root.Q(Column);
                var r = root.Q("root");
                Assert.IsTrue(r.ClassListContains(EventCardPresenter.ModalClass), at + ": precondition: the card is open");
                ScreenNav.DisableCovered(root);
                Assert.IsTrue(column.enabledInHierarchy, at + ": usable under the card, as the side menu is");
                var star = root.Q<Button>("hud-star");
                var picked = root.panel.Pick(star.worldBound.center);
                Assert.IsTrue(picked == star || picked?.GetFirstAncestorOfType<Button>() == star, $"{at}: the card's backdrop does not take its press ({picked?.name})");
                var order = root.Query<VisualElement>().Where(e => e.focusable && e.canGrabFocus && e.tabIndex >= 0 && e is Button).ToList().Select(e => e.name).ToList();
                CollectionAssert.AreEqual(Icons.Select(i => i.name).ToArray(), order.Where(n => n.StartsWith("hud-")).ToArray(), at + ": Tab reaches them top to bottom");
                Assert.Less(order.IndexOf("end-turn"), order.IndexOf("hud-star"), at + ": after End Turn");
                foreach (var cover in new[] { StartScreenPresenter.OpenClass, OutcomePresenter.OpenClass, TurnReportPresenter.OpenClass })
                {
                    r.AddToClassList(cover); ScreenNav.DisableCovered(root);
                    Assert.IsFalse(column.enabledSelf, $"{at}: out of reach under {cover}");
                    r.RemoveFromClassList(cover); ScreenNav.DisableCovered(root);
                    Assert.IsTrue(column.enabledSelf, $"{at}: back after {cover}");
                }
                var tip = root.Q<Label>(HoverTip.TipName);
                foreach (var (name, _, _) in Icons)
                {
                    var b = root.Q<Button>(name);
                    using (var e = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = b.worldBound.center }))
                    { e.target = b; b.SendEvent(e); }
                    Assert.AreEqual(DisplayStyle.Flex, tip.style.display.value, $"{at}: hovering {name} shows its tip");
                    Assert.AreEqual(b.tooltip, tip.text, $"{at}: {name}'s tip says its name");
                    Assert.GreaterOrEqual(tip.style.left.value.value, b.worldBound.xMax - 0.5f, $"{at}: the tip of {name} is beside it, not over it");
                    using (var e = PointerLeaveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = Vector2.zero }))
                    { e.target = b; b.SendEvent(e); }
                }
            });
        // The stylesheet hides them with the side menu under the start screen and while loading.
        foreach (var hebrew in new[] { false, true })
            foreach (var cover in new[] { StartScreenPresenter.OpenClass, "loading" })
            {
                yield return RenderedWith(1920, 1080, hebrew, cover, root =>
                {
                    var col = root.Q(Column).resolvedStyle;
                    if (cover == "loading") Assert.AreEqual(Visibility.Hidden, col.visibility, $"{cover}: hidden while loading, as the side menu");
                    else Assert.AreEqual(DisplayStyle.None, col.display, $"{cover}: hidden under the start screen, as the side menu");
                });
            }
    }

    /// <summary>The bare Game.uxml at w x h with <paramref name="rootClass"/> on its root, after its layout.</summary>
    static IEnumerator RenderedWith(int w, int h, bool hebrew, string rootClass, System.Action<VisualElement> check)
    {
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("CommunityIconsCover");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        root.Q("root").AddToClassList(rootClass);
        try
        {
            for (int i = 0; i < 5; i++) yield return null;
            check(root);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    /// <summary>On a phone upright the hint chip (no hex chosen yet) sits over the icons, clear of them, in English and Hebrew.</summary>
    [UnityTest]
    public IEnumerator OnAPhoneTheEmptyHintClearsTheIcons()
    {
        var problems = new List<string>();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (390, 844), (360, 800), (375, 667) })
                yield return Rendered(w, h, hebrew, g, null, root =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var panel = root.Q("info-panel");
                    if (!panel.ClassListContains("info-empty") || !(panel.worldBound.width > 0)) { problems.Add($"{at}: precondition: the hint chip shows"); return; }
                    var hull = Hull(Icons.Select(i => root.Q<Button>(i.name)));
                    if (panel.worldBound.Overlaps(hull)) problems.Add($"{at}: the hint {panel.worldBound} overlaps the icons {hull}");
                    if (panel.worldBound.Overlaps(root.Q("minimap").worldBound)) problems.Add($"{at}: the hint {panel.worldBound} overlaps the minimap");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
