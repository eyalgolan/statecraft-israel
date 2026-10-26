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

/// <summary>HUD round 2, part 2 (#398): promo/final/03's left icon column. It holds the screens' buttons, moved from
/// the top bar. They are round icons with no caption; each one's name is its
/// text (clear) and its tooltip, which HoverTip shows beside it. The Hebrew UI mirrors rows, not the screen's corners (#550),
/// so the column stays at the left. Tab runs the side-menu screens, New Game, then End Turn.</summary>
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
        ("nav-timeline", "nav.timeline", "hourglass"),
        ("nav-how-to-play", "hud.nav.how_to_play", "book"), ("nav-objectives", "hud.nav.objectives", "flag"),
        ("nav-settings", "hud.nav.settings", "gear"),
    };

    /// <summary>InfoPanelLayoutTests' 19 window sizes, and the smaller phones held upright the owner's floor reaches (#642,
    /// "down to 375x667"): 360x800 and 375x667.</summary>
    static readonly (int w, int h)[] Windows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1680, 1050), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (1531, 784), (2560, 1080), (1920, 900), (1600, 757), (1366, 657), (960, 600), (390, 844),
        (360, 800), (375, 667),
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
    /// border caption. <paramref name="photos"/>: the card shows its event's photograph (the taller card).</summary>
    static IEnumerator Rendered(int w, int h, bool hebrew, GameSession g, System.Action<VisualElement, ScreenNav> check, System.Action<VisualElement, ScreenNav>? act = null,
                                EventImageCatalog? photos = null)
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
        var card = new EventCardPresenter(root, bridge, text, null, photos);
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

    /// <summary>The column holds every ScreenNav screen in order, each a round icon whose name is its text and
    /// its tooltip in both languages; the top bar keeps New Game and End Turn only. No new button: the screens are ScreenNav's.</summary>
    [Test]
    public void TheColumnHoldsTheScreensAsRoundIconsNamedInBothLanguages()
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
            // The discs have no caption, so each glyph alone tells them apart (How to play is not a second Why? mark, #1041).
            CollectionAssert.AllItemsAreUnique(column.Query<UiIcon>(className: "nav-disc").ToList().Select(i => i.Glyph).ToArray(), "each disc its own glyph");
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

    /// <summary>Review of #859 at fb2a8dc (WebGL) and the owner's ruling there (17 October 2026): on a phone held upright,
    /// 390x844, 360x800 and 375x667, with a photo card open (turn 0's One Army and turn 1's The First Truce, taller than
    /// WithAnEventOpen's card, which the test above uses), no rail button sits under the card, Why? and Settings included
    /// (they did on main), in English and Hebrew.</summary>
    [UnityTest]
    public IEnumerator OnAPhoneThePhotoCardLeavesTheRailClear()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        Assert.IsNotNull(catalog, "precondition: the photo catalog");
        var problems = new List<string>();
        var turn0 = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        CollectionAssert.AreEqual(new[] { "idf_founding" }, turn0.State.PendingEvents, "precondition: One Army at turn 0");
        var turn1 = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        turn1.Step(new Command[0]);
        Assert.IsNotEmpty(turn1.State.PendingEvents, "precondition: an event at turn 1");
        foreach (var (turn, g) in new[] { (0, turn0), (1, turn1) })
            foreach (var hebrew in new[] { false, true })
                foreach (var (w, h) in new[] { (390, 844), (360, 800), (375, 667) })
                    yield return Rendered(w, h, hebrew, g, (root, nav) =>
                    {
                        string at = $"{(hebrew ? "he" : "en")} {w}x{h} turn {turn} {g.State.PendingEvents[0]}";
                        if (!(root.Q("event-photo").worldBound.height > 0)) { problems.Add($"{at}: precondition: the card shows its photograph"); return; }
                        var card = root.Q("event-card").worldBound;
                        foreach (var (name, _, _) in Rail)
                        {
                            var b = root.Q(name).worldBound;
                            TestContext.WriteLine($"{at}: {name} {b}, the card {card}");
                            if (b.Overlaps(card)) problems.Add($"{at}: the card {card} covers {name} {b}");
                        }
                    }, photos: catalog);
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

    /// <summary>The audit's focus order is kept: the side-menu screens, New Game, End Turn (then the minimap's
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
                CollectionAssert.AreEqual(new[] { "nav-cabinet", "nav-budget", "nav-defence", "nav-why", "nav-timeline", "nav-how-to-play", "nav-objectives", "nav-settings", "new-game", "end-turn" },
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

    /// <summary>#1323 (owner, v0.2.0): a screen closed with its X by the mouse hands no focus on (PointerBlur's rule), so its
    /// button in the column shows neither its tip nor the focus fill. Closed from the keyboard (Escape, even after a mouse
    /// press on the X; Enter on the X) the focus goes back to that button and its tip shows, and Tab moves the tip on.</summary>
    [UnityTest]
    public IEnumerator ClosingAScreenWithTheMouseShowsNoTipAndTheKeyboardKeepsIt()
    {
        var problems = new List<string>();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        yield return Rendered(1280, 720, false, g, (root, nav) =>
        {
            var tip = root.Q<Label>(HoverTip.TipName);
            var focus = root.panel.focusController;
            // HoverTip shows the tip inline (display: flex) and hides it inline; with no inline value the stylesheet hides it
            bool Shown() => tip.style.display.keyword == StyleKeyword.Undefined && tip.style.display.value == DisplayStyle.Flex;
            bool TipFor(VisualElement b) => Shown() && tip.text == b.tooltip;
            Assert.IsFalse(root.Q("root").ClassListContains(EventCardPresenter.ModalClass), "precondition: the decision card is hidden (open, it takes the focus when a screen closes)");
            foreach (var s in ScreenNav.Screens)
            {
                var button = root.Q<Button>("nav-" + s); var x = root.Q<Button>("screen-close-" + s);
                nav.Open(s);
                PressWithPointer(x);
                if (nav.Current != null) problems.Add($"{s}: the X pressed with the mouse closes the screen");
                if (focus.focusedElement == button) problems.Add($"{s}: the X pressed with the mouse puts the focus on {button.name}");
                if (Shown()) problems.Add($"{s}: the X pressed with the mouse shows the tip '{tip.text}'");

                nav.Open(s); x.Focus();
                using (var e = NavigationCancelEvent.GetPooled()) { e.target = x; x.SendEvent(e); }
                if (nav.Current != null || focus.focusedElement != button || !TipFor(button)) problems.Add($"{s}: Escape after a mouse press gives {button.name} the focus and its tip");

                nav.Open(s); x.Focus(); UiTestKit.Press(x);
                if (nav.Current != null || focus.focusedElement != button || !TipFor(button)) problems.Add($"{s}: Enter on the X gives {button.name} the focus and its tip");

                using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = button; button.SendEvent(e); }
                var next = focus.focusedElement as VisualElement;
                if (next == null || next == button || !TipFor(next)) problems.Add($"{s}: Tab from {button.name} moves the tip to the next button (focus on {next?.name ?? "nothing"}, tip '{tip.text}')");
            }
        }, act: (root, nav) => UiTestKit.Press(root.Q<Button>("event-hide")));   // turn 0's card (One Army) put behind "Decision waiting", as the owner had it
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>A mouse press as the player makes it (StartScreenTests' helper): pointer down, which also gives the button the
    /// focus, then the button's Clickable fires `clicked` on pointer up. UiTestKit.Press is the keyboard's Submit instead.</summary>
    static void PressWithPointer(Button b)
    {
        b.Focus();
        using (var down = PointerDownEvent.GetPooled()) { down.target = b; b.SendEvent(down); }
        using var click = ClickEvent.GetPooled();
        click.target = b;
        typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(EventBase) }, null)!.Invoke(b.clickable, new object[] { click });
    }

    /// <summary>#1109: the Cabinet disc wears a small gold badge, and its tip says why, while the sim has a vacant seat the
    /// player can fill this turn (SimBridge.Vacancies, Cabinet.Vacancies); queuing an appointment to every such seat takes
    /// it off. No other disc has a badge. Education is vacant from 19 May 1951 (as Minority Affairs, which has no holder
    /// after 10 March 1949).</summary>
    [UnityTest]
    public IEnumerator TheCabinetDiscShowsABadgeWhileASeatIsVacant()
    {
        foreach (var hebrew in new[] { false, true })
        {
            var text = hebrew ? He : En;
            using var ui = new UiTestKit();
            ui.Root.Q("root").RemoveFromClassList("loading");
            var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var bridge = new SimBridge(g, text);
            using var hud = new HudPresenter(ui.Root, bridge, text);
            var cabinet = ui.Q<Button>("nav-cabinet");
            var badge = cabinet.Q(className: HudPresenter.BadgeClass);
            Assert.IsNotNull(badge, "the Cabinet disc carries a badge element");
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(cabinet.ClassListContains(HudPresenter.ActionClass), "1948: no seat is vacant");
            Assert.AreEqual(DisplayStyle.None, badge.resolvedStyle.display, "1948: the badge is hidden");
            Assert.AreEqual(text["hud.nav.cabinet"], cabinet.tooltip, "1948: the tip is the disc's name");

            g.State.Date = new System.DateTime(1951, 5, 19);
            g.State.Attention = 10;
            hud.Render();   // a turn would (bridge.Changed)
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(cabinet.ClassListContains(HudPresenter.ActionClass), "May 1951: a seat to fill");
            Assert.AreEqual(DisplayStyle.Flex, badge.resolvedStyle.display, "May 1951: the badge shows");
            Assert.AreEqual(text["hud.nav.cabinet_vacant"], cabinet.tooltip, "May 1951: the tip says why");
            Assert.AreEqual(text["hud.nav.cabinet"], cabinet.text, "the name stays the text (the event log records it)");
            var disc = cabinet.Q(className: "nav-disc").worldBound; var dot = badge.worldBound;
            Assert.IsTrue(dot.width > 0 && dot.center.x > disc.center.x && dot.center.y < disc.center.y && disc.Overlaps(dot),
                $"the badge sits on the disc's upper right ({dot} on {disc})");
            foreach (var other in ScreenNav.Screens.Where(x => x != "cabinet" && x != "budget"))   // Budget's: the next test
                Assert.IsNull(ui.Q<Button>("nav-" + other).Q(className: HudPresenter.BadgeClass), other + ": no badge");

            foreach (var office in Cabinet.Vacancies(C, g.State, new Command[0]))
                Assert.IsNull(bridge.Queue(new Appoint(office.Id, Cabinet.Candidates(C, g.State, office.Id).First(x => x.reason == null).person.Id)), office.Id);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(cabinet.ClassListContains(HudPresenter.ActionClass), "every vacant seat has a queued appointment");
            Assert.AreEqual(DisplayStyle.None, badge.resolvedStyle.display, "the badge goes");
            Assert.AreEqual(text["hud.nav.cabinet"], cabinet.tooltip, "the tip is the name again");
        }
    }

    /// <summary>#1109: the Budget disc wears the same badge, and its tip says why, while the Budget screen's draft differs from
    /// what End turn will apply and Submit can apply it (EconomySystem.Unsubmitted through SimBridge.BudgetUnsubmitted): an
    /// edit lights it, Submit takes it off, an edit after Submit lights it again, and with no Attention left for an
    /// allocation it goes off, since Submit would be refused.</summary>
    [UnityTest]
    public IEnumerator TheBudgetDiscShowsABadgeWhileAnEditedBudgetIsNotSubmitted()
    {
        foreach (var hebrew in new[] { false, true })
        {
            var text = hebrew ? He : En;
            using var ui = new UiTestKit();
            ui.Root.Q("root").RemoveFromClassList("loading");
            var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            g.State.Attention = 4;   // the opening rung 5 prices an allocation at 2 x (1 + 4 x 0.25) = 4: just affordable
            var bridge = new SimBridge(g, text);
            using var hud = new HudPresenter(ui.Root, bridge, text);
            using var screen = new BudgetPresenter(ui.Root, bridge, text);
            var nav = ui.Q<Button>("nav-budget");
            var badge = nav.Q(className: HudPresenter.BadgeClass);
            Assert.IsNotNull(badge, "the Budget disc carries a badge element");
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(nav.ClassListContains(HudPresenter.ActionClass), "untouched: nothing to submit");
            Assert.AreEqual(DisplayStyle.None, badge.resolvedStyle.display, "untouched: the badge is hidden");
            Assert.AreEqual(text["hud.nav.budget"], nav.tooltip, "untouched: the tip is the disc's name");

            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(nav.ClassListContains(HudPresenter.ActionClass), "edited: not submitted");
            Assert.AreEqual(DisplayStyle.Flex, badge.resolvedStyle.display, "edited: the badge shows");
            Assert.AreEqual(text["hud.nav.budget_unsubmitted"], nav.tooltip, "edited: the tip says why");
            Assert.AreEqual(text["hud.nav.budget"], nav.text, "the name stays the text (the event log records it)");
            var disc = nav.Q(className: "nav-disc").worldBound; var dot = badge.worldBound;
            Assert.IsTrue(dot.width > 0 && dot.center.x > disc.center.x && dot.center.y < disc.center.y && disc.Overlaps(dot),
                $"the badge sits on the disc's upper right ({dot} on {disc})");

            ui.Press("budget-submit");
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(nav.ClassListContains(HudPresenter.ActionClass), "submitted: the badge goes");
            Assert.AreEqual(text["hud.nav.budget"], nav.tooltip, "submitted: the tip is the name again");

            UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
            Assert.IsTrue(nav.ClassListContains(HudPresenter.ActionClass), "edited after Submit: not submitted again (the queued budget's 4 is refunded first)");
            g.State.Attention = 0;
            screen.Render();
            Assert.IsFalse(nav.ClassListContains(HudPresenter.ActionClass), "no Attention left: Submit could not apply it");
        }
    }
}
