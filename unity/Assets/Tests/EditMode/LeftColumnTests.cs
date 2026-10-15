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

/// <summary>HUD round 2, part 2 (#398): promo/final/03's left icon column. It holds the four screens' buttons, moved from
/// the top bar (no new action): Cabinet, Budget, Defence, Why?. They are round icons with no caption; each one's name is its
/// text (clear) and its tooltip, which HoverTip shows beside it. The Hebrew UI mirrors rows, not the screen's corners (#550),
/// so the column stays at the left. Tab still runs Cabinet, Budget, Defence, Why?, New Game, End Turn.</summary>
public class LeftColumnTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly HexFile Hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static string MapFile(string name) => File.ReadAllText(Path.Combine(Paths.Content, "map", name));
    static MapRegions Regions() => new MapRegions(Subdistricts1948.Parse(MapFile(Subdistricts1948.FileName)), Armistice1949.Parse(MapFile(Armistice1949.FileName)));
    static Dictionary<string, string> Names() => SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName)).ToDictionary(t => t.Id, t => t.Name);

    const string Column = "left-column";   // HudPresenter.RailName

    static readonly (string name, string key, string glyph)[] Rail =
    {
        ("nav-cabinet", "hud.nav.cabinet", "columns"), ("nav-budget", "hud.nav.budget", "ledger"),
        ("nav-defence", "hud.nav.defence", "helmet"), ("nav-why", "hud.nav.why", "question"),
    };

    /// <summary>InfoPanelLayoutTests' 19 window sizes.</summary>
    static readonly (int w, int h)[] Windows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1680, 1050), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (1531, 784), (2560, 1080), (1920, 900), (1600, 757), (1366, 657), (960, 600), (390, 844),
    };

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

    /// <summary>The real Game.uxml on the real PanelSettings at w x h, in English or Hebrew, with the top bar, the screens'
    /// navigation, the decision card (open when the session has an event), the info panel on Lydda's hex, the minimap and the
    /// border caption.</summary>
    static IEnumerator Rendered(int w, int h, bool hebrew, GameSession g, System.Action<VisualElement, ScreenNav> check, System.Action<VisualElement, ScreenNav>? act = null)
    {
        var text = hebrew ? He : En;
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("LeftColumnTest");
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
            var lod = C.SettlementHexes.Single(s => s.Id == "lod");
            info.Show(lod.Q, lod.R);
            act?.Invoke(root, nav);
            for (int i = 0; i < 30 && !(root.Q("nav-cabinet").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            check(root, nav);
        }
        finally
        {
            mini.Dispose(); info.Dispose(); card.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>The column holds the four screens, in the top bar's old order, each a round icon whose name is its text and
    /// its tooltip in both languages; the top bar keeps New Game and End Turn only. No new button: the screens are ScreenNav's.</summary>
    [Test]
    public void TheColumnHoldsTheFourScreensAsRoundIconsNamedInBothLanguages()
    {
        foreach (var hebrew in new[] { false, true })
        {
            var text = hebrew ? He : En;
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            using var hud = new HudPresenter(ui.Root, bridge, text);
            var column = ui.Q<VisualElement>(Column);
            CollectionAssert.AreEqual(Rail.Select(r => r.name).ToArray(), column.Query<Button>().ToList().Select(b => b.name).ToArray(), "the column's buttons, in order");
            CollectionAssert.AreEquivalent(ScreenNav.Screens.Select(s => "nav-" + s).ToArray(), Rail.Select(r => r.name).ToArray(), "exactly ScreenNav's screens");
            foreach (var (name, key, glyph) in Rail)
            {
                var b = ui.Q<Button>(name);
                Assert.AreEqual(text[key], b.text, $"{name}: its name is still its text (the event log records it)");
                Assert.IsFalse(string.IsNullOrWhiteSpace(b.tooltip), $"{name}: a tooltip");
                Assert.AreEqual(text[key], b.tooltip, $"{name}: the tooltip is its name");
                Assert.AreEqual(glyph, b.Q<UiIcon>(className: "nav-disc").Glyph, name);
            }
            CollectionAssert.AreEqual(new[] { "new-game" }, ui.Q<VisualElement>("nav").Query<Button>().ToList().Select(b => b.name).ToArray(), "the top bar keeps New Game (and End Turn)");
        }
    }

    /// <summary>At every window size, in English and Hebrew, with an event card open and Lydda's hex chosen: the column lies in
    /// the window at its left edge (Hebrew too: #550 mirrors rows, not corners), under the bar, each disc at least 24 screen
    /// px with no caption showing, clear of the info panel, the decision card and its backdrop, the minimap column and the
    /// border caption, and the toast.</summary>
    [UnityTest]
    public IEnumerator TheColumnFitsEveryWindowInEnglishAndHebrew()
    {
        var problems = new List<string>();
        var g = WithAnEventOpen();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                yield return Rendered(w, h, hebrew, g, (root, nav) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var window = root.worldBound;
                    float scale = w / window.width;
                    // Preconditions, so no overlap check below is vacuous (a hidden element has no size).
                    foreach (var shown in new[] { "event-card", "event-scrim", "event-gutter", "info-panel", "minimap", "map-layer" })
                        if (!(root.Q(shown).worldBound.width > 0)) problems.Add($"{at}: precondition: {shown} shows");
                    var col = root.Q(Column).worldBound;
                    var buttons = root.Q(Column).Query<Button>().ToList();
                    var hull = buttons.Select(b => b.worldBound).Aggregate((a, b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax)));
                    if (!(hull.xMin >= window.xMin && hull.xMax <= window.xMax && hull.yMax <= window.yMax)) problems.Add($"{at}: the column {hull} leaves the window {window}");
                    if (hull.xMin - window.xMin > 0.03f * window.width) problems.Add($"{at}: the column {hull} is not at the left edge");
                    if (hull.yMin < root.Q("top-bar").worldBound.yMax) problems.Add($"{at}: the column {hull} rises into the top bar");
                    foreach (var b in buttons)
                    {
                        if (b.worldBound.width * scale < 24f || b.worldBound.height * scale < 24f) problems.Add($"{at}: {b.name} is {b.worldBound.width * scale:0} px, under 24");
                        if (b.resolvedStyle.color.a > 0.01f) problems.Add($"{at}: {b.name}'s caption shows");
                    }
                    foreach (var (name, r) in new[] { ("the info panel", root.Q("info-panel").worldBound), ("the decision card", root.Q("event-card").worldBound),
                                                      ("the card's backdrop", root.Q("event-scrim").worldBound), ("the backdrop's strip, over the column", ColumnOverGutter(root)), ("the minimap", root.Q("minimap").worldBound),
                                                      ("the border caption", root.Q("map-layer").worldBound) })
                        if (r.Overlaps(hull)) problems.Add($"{at}: the column {hull} overlaps {name} {r}");
                    if (!root.Q(Column).enabledInHierarchy) problems.Add($"{at}: the column is disabled under the open card (the top bar is not)");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Each screen opens clear of the column at every window size (a screen under the column would hide the button
    /// that closes it), and the toast too, in English and Hebrew.</summary>
    [UnityTest]
    public IEnumerator EveryScreenAndTheToastOpenClearOfTheColumn()
    {
        var problems = new List<string>();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
            {
                string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                yield return Rendered(w, h, hebrew, g, (root, nav) =>
                {
                    var toast = root.Q("toast").worldBound;
                    if (!(toast.width > 0)) problems.Add($"{at}: precondition: the toast shows");
                    else if (toast.Overlaps(Hull(root))) problems.Add($"{at}: the toast {toast} covers the column {Hull(root)}");
                }, (root, nav) => { var t = root.Q<Label>("toast"); t.text = "A note"; t.style.display = DisplayStyle.Flex; });
                foreach (var screen in ScreenNav.Screens)
                    yield return Rendered(w, h, hebrew, g, (root, nav) =>
                    {
                        var r = root.Q("screen-" + screen).worldBound;
                        if (!(r.width > 0)) problems.Add($"{at}: precondition: the {screen} screen shows");
                        else if (r.Overlaps(Hull(root))) problems.Add($"{at}: the {screen} screen {r} covers the column {Hull(root)}");
                    }, (root, nav) => nav.Toggle(screen));
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Empty unless the strip draws over the column: the strip comes before the column in the tree, so the column's
    /// buttons are above it (checked by picking a disc's centre).</summary>
    static Rect ColumnOverGutter(VisualElement root)
    {
        var b = root.Q<Button>("nav-cabinet");
        var picked = root.panel.Pick(b.worldBound.center);
        return picked == b || picked?.GetFirstAncestorOfType<Button>() == b ? Rect.zero : b.worldBound;
    }

    static Rect Hull(VisualElement root) => root.Q(Column).Query<Button>().ToList().Select(b => b.worldBound)
        .Aggregate((a, b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax)));

    /// <summary>The audit's focus order is kept: Cabinet, Budget, Defence, Why?, New Game, End Turn (then the minimap's
    /// buttons, out of reach under the open card). The column is out of Tab's reach under the start screen and an outcome clip, as the top bar is, and usable
    /// under the decision card, as the top bar is.</summary>
    [UnityTest]
    public IEnumerator TheTabOrderIsKeptAndTheColumnIsCoveredWhereTheTopBarIs()
    {
        var g = WithAnEventOpen();
        foreach (var hebrew in new[] { false, true })
            yield return Rendered(1920, 1080, hebrew, g, (root, nav) =>
            {
                string at = hebrew ? "he" : "en";
                var order = root.Query<VisualElement>().Where(e => e.focusable && e.canGrabFocus && e.tabIndex >= 0 && e is Button).ToList()
                    .Select(e => e.name).Where(n => n.StartsWith("nav-") || n == "new-game" || n == "end-turn" || n.StartsWith("minimap-")).ToList();
                // Under the open card the minimap column is out of reach (the backdrop covers it); the column and the bar are not.
                CollectionAssert.AreEqual(new[] { "nav-cabinet", "nav-budget", "nav-defence", "nav-why", "new-game", "end-turn" },
                    order, $"{at}: the Tab order under the open card");
                var r = root.Q("root");
                Assert.IsTrue(r.ClassListContains(EventCardPresenter.ModalClass), at + ": precondition: the card is open");
                Assert.IsTrue(root.Q(Column).enabledSelf, at + ": usable under the card, as the top bar is");
                r.AddToClassList(StartScreenPresenter.OpenClass); ScreenNav.DisableCovered(root);
                Assert.IsFalse(root.Q(Column).enabledSelf, at + ": out of reach under the start screen");
                r.RemoveFromClassList(StartScreenPresenter.OpenClass); r.AddToClassList(OutcomePresenter.OpenClass); ScreenNav.DisableCovered(root);
                Assert.IsFalse(root.Q(Column).enabledSelf, at + ": out of reach under an outcome clip");
                r.RemoveFromClassList(OutcomePresenter.OpenClass); ScreenNav.DisableCovered(root);
                Assert.IsTrue(root.Q(Column).enabledSelf, at + ": back");
            });
    }

    /// <summary>Each button's name shows beside it, to its right, on hover and on focus, inside the window, and goes on leave
    /// and blur; pressing it still opens its screen.</summary>
    [UnityTest]
    public IEnumerator EachButtonShowsItsNameOnHoverAndFocusAndOpensItsScreen()
    {
        var problems = new List<string>();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (960, 600), (390, 844) })
                yield return Rendered(w, h, hebrew, g, (root, nav) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var tip = root.Q<Label>(HoverTip.TipName);
                    foreach (var (name, key, _) in Rail)
                    {
                        var b = root.Q<Button>(name);
                        using (var e = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = b.worldBound.center }))
                        { e.target = b; b.SendEvent(e); }
                        if (tip.style.display.value != DisplayStyle.Flex || tip.text != b.tooltip) problems.Add($"{at}: hovering {name} shows no tip");
                        float left = tip.style.left.value.value;
                        float width = tip.MeasureTextSize(tip.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x + 22;
                        if (left < b.worldBound.xMax - 0.5f) problems.Add($"{at}: the tip of {name} covers its button");
                        if (left + width > root.worldBound.xMax) problems.Add($"{at}: the tip of {name} leaves the window");
                        using (var e = PointerLeaveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = Vector2.zero }))
                        { e.target = b; b.SendEvent(e); }
                        if (tip.style.display.value != DisplayStyle.None) problems.Add($"{at}: the tip of {name} stays after leave");
                        b.Focus();
                        if (tip.style.display.value != DisplayStyle.Flex || tip.text != b.tooltip) problems.Add($"{at}: focusing {name} shows no tip");
                        b.Blur();
                        if (tip.style.display.value != DisplayStyle.None) problems.Add($"{at}: the tip of {name} stays after blur");
                    }
                    var budget = root.Q<Button>("nav-budget");
                    budget.Focus();
                    UiTestKit.Press(budget);
                    if (tip.style.display.value != DisplayStyle.None) problems.Add($"{at}: the tip stays over the screen Budget opened (review of #602)");
                    if (nav.Current != "budget") problems.Add($"{at}: Budget in the column opens the Budget screen (was {nav.Current ?? "none"})");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
