#nullable enable
using System.IO;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The promo HUD pass (promo/final/03): the top bar's indicator icons, the round screen buttons and their lit
/// state, the event card's option signs, the info panel's terrain sign (real hex facts, no tile yields) and the panel
/// trim. Behaviour is unchanged; these check the new pieces are wired to the real elements and data.</summary>
public class PromoHudTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void EveryIndicatorHasItsIcon()
    {
        using var ui = new UiTestKit();
        foreach (var (meter, glyph) in new[] { ("meter-treasury", "coin"), ("meter-immigration", "people"), ("meter-security", "shield"),
                                               ("meter-support", "crowd"), ("meter-standing", "globe"), ("meter-fx", "banknote") })
        {
            var icon = ui.Q<VisualElement>(meter).Q<UiIcon>();
            Assert.IsNotNull(icon, meter);
            Assert.AreEqual(glyph, icon.Glyph, meter);
            CollectionAssert.Contains(UiIcon.Glyphs, glyph, meter + ": a glyph the icon can draw");
        }
    }

    [Test]
    public void EveryScreenButtonIsARoundIconWithItsName()
    {
        using var ui = new UiTestKit();
        foreach (var (button, name, glyph) in new[] { ("nav-cabinet", "Cabinet", "columns"), ("nav-budget", "Budget", "ledger"),
                                                      ("nav-defence", "Defence", "helmet"), ("nav-why", "Why?", "question"), ("new-game", "New Game", "restart") })
        {
            var b = ui.Q<Button>(button);
            Assert.AreEqual(name, b.text, button + ": the name stays the button's text (legible, and what the event log records)");
            var disc = b.Q<UiIcon>(className: "nav-disc");
            Assert.IsNotNull(disc, button);
            Assert.AreEqual(glyph, disc.Glyph, button);
            Assert.AreEqual(PickingMode.Ignore, disc.pickingMode, button + ": a click on the disc reaches the button");
        }
    }

    [Test]
    public void TheOpenScreensButtonIsLit()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        try
        {
            Assert.IsFalse(ScreenNav.Screens.Any(s => ui.Q<Button>("nav-" + s).ClassListContains(ScreenNav.OpenClass)), "none lit at start");
            nav.Toggle("budget");
            foreach (var s in ScreenNav.Screens) Assert.AreEqual(s == "budget", ui.Q<Button>("nav-" + s).ClassListContains(ScreenNav.OpenClass), s);
            nav.Toggle("budget");
            Assert.IsFalse(ui.Q<Button>("nav-budget").ClassListContains(ScreenNav.OpenClass), "closing unlights it");
        }
        finally { nav.Dispose(); }
    }

    [Test]
    public void EachEventOptionCarriesItsSignAndStaysPressable()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var bridge = new SimBridge(g, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        var options = ui.Q<VisualElement>("event-options").Query<Button>().ToList();
        Assert.Greater(options.Count, 1);
        foreach (var b in options)
        {
            Assert.IsTrue(b.ClassListContains(EventCardPresenter.OptionClass), b.name);
            Assert.AreEqual(0, b.childCount, b.name + ": the sign is painted, so the button stays a leaf whose wrapped text is measured");
        }
        UiTestKit.Press(ui.Q<Button>("option-sign"));
        Assert.AreNotEqual("armistice_egypt", card.Shown?.Id, "the option still answers the event");
        card.Dispose();
    }

    [Test]
    public void TheInfoPanelShowsTheHexsTerrainSignAndNoYields()
    {
        using var ui = new UiTestKit();
        var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, towns.ToDictionary(t => t.Id, t => t.Name));
        var thumb = ui.Q<UiIcon>("info-thumb");
        Assert.AreEqual(DisplayStyle.None, thumb.style.display.value, "no sign before a hex is chosen");
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        var biome = C.HexAt(tlv.Q, tlv.R)!.Biome;
        Assert.AreEqual(DisplayStyle.Flex, thumb.style.display.value);
        Assert.AreEqual("biome-" + biome, thumb.Glyph);
        CollectionAssert.Contains(UiIcon.Glyphs, thumb.Glyph);
        Assert.AreEqual(MapBuilder.BiomeColours[biome], thumb.style.backgroundColor.value, "the biome's own map colour");
        var words = ui.Q<VisualElement>("info-panel").Query<Label>().ToList().Select(l => l.text).ToList();
        foreach (var yield in new[] { "Food", "Production", "Culture" })
            Assert.IsFalse(words.Any(w => w.Contains(yield)), $"no {yield} yield (owner ruling)");
        info.Dispose();
    }

    [Test]
    public void EveryBiomeHasATerrainSign()
    {
        foreach (var biome in MapBuilder.BiomeColours.Keys)
            CollectionAssert.Contains(UiIcon.Glyphs, "biome-" + biome, biome);
    }

    [Test]
    public void ThePanelsCarryTheTrimAndItTakesNoClicks()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "event-card", "minimap", "info-panel", "screen-cabinet", "screen-budget", "screen-defence", "screen-why" })
        {
            var frame = ui.Q<VisualElement>(name).Children().OfType<UiFrame>().SingleOrDefault();
            Assert.IsNotNull(frame, name);
            Assert.AreEqual(PickingMode.Ignore, frame!.pickingMode, name);
        }
    }

    /// <summary>The bar at every window shape, after a turn has played and with the widest late-game values, changes and
    /// date (the tiers: HudPresenter). 16:9 and a 16:10 MacBook browser keep the icons on the one-row bar; 16:10 fullscreen
    /// keeps them on the stacked bar; 4:3 and 5:4 drop them; square and narrower windows wrap into two rows. Everywhere:
    /// every meter label stays inside its meter and the bar, no two meters' labels overlap, the meters keep 5% spare room
    /// and stay 12 px clear of the screen buttons, End Turn and the badge, the badge holds its widest line inside its trim at
    /// a fixed width, and the panels start below the bar (reviews N2, N3, N5, N7).</summary>
    [UnityTest]
    public IEnumerator EveryWindowShapeFitsTheBarAfterATurn()
    {
        // (window, icons, stacked changes, two rows, three rows): 16:9 fullscreen; a 16:9 window; a 16:10 MacBook browser
        // (~1911 logical); 21:9 ultrawide (~2217); 16:10 fullscreen (~1821) keeps the icons on the stacked bar; 4:3 and 5:4
        // drop them; square and narrower wrap; a phone upright (390x844, ~790) takes three rows (#398).
        foreach (var (w, h, icons, narrow, twoRow, threeRow) in new[] { (1920, 1080, true, false, false, false), (1280, 720, true, false, false, false),
                                                              (1366, 768, true, false, false, false), (1760, 1000, true, false, false, false),
                                                              (2560, 1080, true, false, false, false),
                                                              (1920, 1200, true, true, false, false), (1440, 900, true, true, false, false),
                                                              (1024, 768, false, true, false, false), (1280, 1024, false, true, false, false),
                                                              (1024, 1024, false, true, true, false), (960, 1080, false, true, true, false),
                                                              (800, 1000, false, true, true, false), (390, 844, false, true, true, true) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("CompactTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            HudPresenter? hud = null;
            try
            {
                // As in the game: the bar is laid out (the loading screen) before the HUD is made, and the window never resizes.
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
                hud = new HudPresenter(root, bridge, Text);
                bridge.EndTurn();                                       // real changes on every meter that moved
                for (int i = 0; i < 6; i++) yield return null;
                if ((w, h) == (1920, 1080) || (w, h) == (2560, 1080))
                {
                    // Review P1: with typical (turn-1) values at 16:9, and at 21:9 (#398), the bar has room for the promo's sizes.
                    var at = $"{w}x{h}";
                    Assert.IsTrue(root.Q("top-bar").ClassListContains(HudPresenter.RoomyClass), at + ": roomy with typical values");
                    foreach (var icon in root.Q("meters").Query<UiIcon>().ToList())
                        Assert.GreaterOrEqual(icon.worldBound.width, 36f, $"{at}: the {icon.Glyph} icon is promo-sized");
                    foreach (var v in root.Q("meters").Query<Label>(className: "meter-value").ToList())
                        Assert.GreaterOrEqual(v.resolvedStyle.fontSize, 24f, $"{at}: '{v.text}' is promo-sized");
                    // Integration bug B8: on the roomy bar every caption keeps one line, as in the promo.
                    foreach (var c in root.Q("meters").Query<Label>(className: "meter-name").ToList().Where(c => c.name != "security-level"))
                        Assert.Less(c.worldBound.height, c.resolvedStyle.fontSize * 2f, $"{at}: '{c.text}' wraps");
                }
                // The widest realistic changes, formatted as HudModel formats them, and the widest date line.
                foreach (var (name, delta) in new[] { ("treasury", PlayerText.MoneyChange(C, "treasury", -12.4)), ("immigration", Text.Format("hud.delta_people", 12345.0)),
                                                      ("security", "-10%"), ("support", "-10%"), ("standing", "+10%"), ("fx", PlayerText.MoneyChange(C, "fx_reserves", 12.5)) })
                    root.Q<Label>(name + "-delta").text = delta;
                // ... and the widest late-game values (review N7).
                foreach (var (name, value) in new[] { ("treasury", PlayerText.Money(C, "treasury", -123.4)), ("immigration", Text.Format("hud.people", 123456.0)),
                                                      ("fx", PlayerText.Money(C, "fx_reserves", 123.4)), ("security", "100%"), ("support", "100%"), ("standing", "100%") })
                    root.Q<Label>(name + "-value").text = value;
                root.Q<Label>("security-level").text = Text.Format("hud.level", 5);
                root.Q<Label>("turn").text = Text.Format("hud.turn", 120);
                root.Q<Label>("date").text = Text.Format("hud.date", PlayerText.Date(new System.DateTime(1956, 9, 30)));
                for (int i = 0; i < 6; i++) yield return null;          // the roomy bar steps back down if it must
                var bar = root.Q("top-bar"); var barRect = bar.worldBound;
                var where = $"{w}x{h} (bar {barRect.width:0} px)";
                Assert.AreEqual(!icons, bar.ClassListContains(HudPresenter.CompactClass), where + ": compact (no icons)");
                Assert.AreEqual(narrow, root.Q("root").ClassListContains(HudPresenter.NarrowClass), where + ": narrow");
                Assert.AreEqual(twoRow, root.Q("root").ClassListContains(HudPresenter.TwoRowClass), where + ": two rows");
                Assert.AreEqual(threeRow, root.Q("root").ClassListContains(HudPresenter.ThreeRowClass), where + ": three rows");
                foreach (var icon in root.Q("meters").Query<UiIcon>().ToList())
                    Assert.AreEqual(icons ? DisplayStyle.Flex : DisplayStyle.None, icon.resolvedStyle.display, $"{where}: {icon.Glyph}");
                var badge = root.Q(className: "date-box");
                foreach (var l in badge.Query<Label>().ToList())
                    Assert.LessOrEqual(l.worldBound.xMax, badge.worldBound.xMax - 4f, $"{where}: '{l.text}' {l.worldBound} runs under the badge's trim {badge.worldBound}");
                var labels = new System.Collections.Generic.List<(string meter, Label label)>();
                foreach (var m in root.Q("meters").Children())
                    foreach (var l in m.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text)))
                    {
                        labels.Add((m.name, l));
                        var r = l.worldBound;
                        Assert.LessOrEqual(r.xMax, m.worldBound.xMax + 0.5f, $"{where}: '{l.text}' {r} leaves {m.name} {m.worldBound} on the right");
                        Assert.GreaterOrEqual(r.xMin + 0.5f, m.worldBound.xMin, $"{where}: '{l.text}' leaves {m.name} on the left");
                        Assert.GreaterOrEqual(r.yMin, barRect.yMin + 1f, $"{where}: '{l.text}' leaves the bar at the top");
                        Assert.LessOrEqual(r.yMax, barRect.yMax - 1f, $"{where}: '{l.text}' leaves the bar at the bottom");
                    }
                foreach (var a in labels)
                    foreach (var b in labels)
                        if (a.meter != b.meter && a.label.worldBound.Overlaps(b.label.worldBound))
                            Assert.Fail($"{where}: '{a.label.text}' ({a.meter}) overlaps '{b.label.text}' ({b.meter})");
                // The meters never reach the screen buttons or End Turn (beside them on one row, above them on two), with a
                // margin for the browser's text measuring a little wider than the editor's.
                foreach (var other in new[] { root.Q("nav").worldBound, root.Q(className: "turn-box").worldBound, badge.worldBound })
                    foreach (var (meter, l) in labels)
                    {
                        var r = l.worldBound; r.xMax += 12f;
                        Assert.IsFalse(r.Overlaps(other), $"{where}: '{l.text}' ({meter}) {r} is within 12 px of {other}");
                    }
                // The meters are not squeezed: they take their natural widths with at least 5% to spare before the screen
                // buttons (or the bar's end, on two rows), room for the browser's text measuring a little wider than the editor's.
                var meters = root.Q("meters").Children().ToList();
                var used = meters[meters.Count - 1].worldBound.xMax - meters[0].worldBound.xMin;
                var limit = twoRow ? barRect.xMax - 14f : root.Q("nav").worldBound.xMin;
                Assert.GreaterOrEqual(limit - meters[meters.Count - 1].worldBound.xMax, 0.05f * used, $"{where}: the meters use {used:0} of {limit - meters[0].worldBound.xMin:0} px");
                // Review P2: every screen button's caption fits its button.
                foreach (var nb in root.Q("nav").Query<Button>().ToList())
                {
                    var natural = nb.MeasureTextSize(nb.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(natural.x, nb.contentRect.width + 0.5f, $"{where}: the caption '{nb.text}' ({natural.x:0} px) overflows its button ({nb.contentRect.width:0} px)");
                }
                foreach (var e in new[] { root.Q("nav"), root.Q(className: "turn-box"), badge })
                    Assert.LessOrEqual(e.worldBound.yMax, barRect.yMax + 0.5f, $"{where}: {e.name}{string.Join(".", e.GetClasses())} leaves the bar");
                Assert.GreaterOrEqual(root.Q("right-column").worldBound.yMin, barRect.yMax, where + ": the minimap column starts under the bar");
                var screen = root.Q("screen-why");
                screen.style.display = DisplayStyle.Flex;                // opened, as ScreenNav does
                for (int i = 0; i < 2; i++) yield return null;
                Assert.GreaterOrEqual(screen.worldBound.yMin, barRect.yMax, where + ": the screens start under the bar");
                screen.style.display = DisplayStyle.None;
                // The badge keeps its width for a short date line, so nothing shifts when the month changes.
                var wide = badge.worldBound.width;
                root.Q<Label>("turn").text = Text.Format("hud.turn", 1);
                root.Q<Label>("date").text = Text.Format("hud.date", PlayerText.Date(new System.DateTime(1948, 5, 1)));
                for (int i = 0; i < 2; i++) yield return null;
                Assert.AreEqual(wide, badge.worldBound.width, 0.5f, where + ": the badge's width follows its text");
            }
            finally { hud?.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    static void Near(Color e, Color a, string what) =>
        Assert.That(Mathf.Abs(e.r - a.r) + Mathf.Abs(e.g - a.g) + Mathf.Abs(e.b - a.b), Is.LessThan(0.02f), $"{what}: expected {e} got {a}");

    /// <summary>WCAG relative luminance of a colour composited over the navy behind the HUD.</summary>
    static float Luminance(Color c)
    {
        var navy = new Color(9 / 255f, 14 / 255f, 26 / 255f);
        float Ch(float v, float under) { var x = v * c.a + under * (1 - c.a); return x <= 0.04045f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f); }
        return 0.2126f * Ch(c.r, navy.r) + 0.7152f * Ch(c.g, navy.g) + 0.0722f * Ch(c.b, navy.b);
    }

    static float Contrast(Color a, Color b) { float x = Luminance(a), y = Luminance(b); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }

    /// <summary>Review I1: "open" and keyboard focus are separate signals, and a pointer click does not leave a screen
    /// button focused, so a screen just closed with the mouse never looks open.</summary>
    [UnityTest]
    public IEnumerator FocusAndOpenAreDistinctAndAPointerClickClearsFocus()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        var blur = new PointerBlur(ui.Root);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var card = new EventCardPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");   // ready: a hidden (loading) button takes no focus
        try
        {
            VisualElement Disc(string s) => ui.Q<Button>("nav-" + s).Q(className: "nav-disc");
            nav.Toggle("budget");
            var cabinet = ui.Q<Button>("nav-cabinet");
            cabinet.Focus();
            for (int i = 0; i < 10 && cabinet.focusController.focusedElement != cabinet; i++) yield return null;
            yield return null; yield return null;
            Assert.AreSame(cabinet, cabinet.focusController.focusedElement, "precondition: the keyboard focus is on Cabinet");
            var open = Disc("budget").resolvedStyle; var focused = Disc("cabinet").resolvedStyle;
            // Open: a gold-light rim on the raised navy disc. Focus: an ink rim on the warm focus fill, at least 2.5:1 apart.
            Assert.GreaterOrEqual(Contrast(open.backgroundColor, focused.backgroundColor), 2.5f, "focus and open differ in fill");
            Assert.Greater(Contrast(open.borderTopColor, focused.borderTopColor), 1.1f, "focus and open differ in rim colour");

            // Close Budget with the mouse: the click blurs it, and its disc returns to the unlit rim.
            var budget = ui.Q<Button>("nav-budget");
            budget.Focus();
            nav.Toggle("budget");
            using (var e = ClickEvent.GetPooled()) { e.target = budget; budget.SendEvent(e); }   // bubbles to PointerBlur
            yield return null; yield return null;
            Assert.AreNotSame(budget, budget.focusController.focusedElement, "a pointer click leaves no focus behind");
            Assert.IsFalse(budget.ClassListContains(ScreenNav.OpenClass));
            Near(new Color(196 / 255f, 160 / 255f, 92 / 255f), Disc("budget").resolvedStyle.borderTopColor, "the closed button's rim is unlit");
            Assert.AreEqual(Disc("defence").resolvedStyle.borderTopWidth, Disc("budget").resolvedStyle.borderTopWidth, "the closed button looks like the others");

            // Every other Button too (review N1), the runtime-made event option included: a pointer click leaves no focus
            // fill. The click lands on the button's own text or on a child (the disc), and bubbles to the root's handler.
            var navy = new Color(9 / 255f, 14 / 255f, 26 / 255f);
            foreach (var (name, part) in new[] { ("end-turn", ""), ("new-game", "nav-disc"), ("option-sign", "") })
            {
                var b = ui.Q<Button>(name);
                VisualElement look = part == "" ? b : b.Q(className: part);
                b.Focus();
                for (int i = 0; i < 10 && b.focusController.focusedElement != b; i++) yield return null;
                Assert.AreSame(b, b.focusController.focusedElement, name + ": precondition, focused");
                VisualElement target = part == "" ? b : b.Q(className: part);
                using (var e = ClickEvent.GetPooled()) { e.target = target; target.SendEvent(e); }
                yield return null; yield return null;
                Assert.AreNotSame(b, b.focusController.focusedElement, name + ": a pointer click leaves no focus behind");
                Near(navy, look.resolvedStyle.backgroundColor, name + ": no focus fill after the click");
            }

            // Keyboard activation (Enter, Space: NavigationSubmitEvent) runs the button and keeps its focus (review N8): this pins
            // PointerBlur's assumption that only a pointer click raises ClickEvent.
            var endTurn = ui.Q<Button>("end-turn");
            int pressed = 0; System.Action count = () => pressed++;
            endTurn.clicked += count;
            endTurn.Focus();
            for (int i = 0; i < 10 && endTurn.focusController.focusedElement != endTurn; i++) yield return null;
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = endTurn; endTurn.SendEvent(e); }
            yield return null;
            endTurn.clicked -= count;
            Assert.AreEqual(1, pressed, "Enter/Space pressed End Turn");
            Assert.AreSame(endTurn, endTurn.focusController.focusedElement, "keyboard activation keeps the focus");
        }
        finally { nav.Dispose(); blur.Dispose(); card.Dispose(); }
    }

    /// <summary>Review I3: keyboard focus changes End Turn, an event option and a screen button by at least 3:1 (WCAG 2.4.13).</summary>
    [UnityTest]
    public IEnumerator FocusChangesEachControlByAtLeastThreeToOne()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var card = new EventCardPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        try
        {
            foreach (var (name, part) in new[] { ("end-turn", ""), ("option-sign", ""), ("nav-why", "nav-disc") })
            {
                var b = ui.Q<Button>(name);
                VisualElement target = part == "" ? b : b.Q(className: part);
                for (int i = 0; i < 5; i++) yield return null;
                var before = target.resolvedStyle;
                Color bgBefore = before.backgroundColor, rimBefore = before.borderTopColor;
                b.Focus();
                for (int i = 0; i < 10 && b.focusController.focusedElement != b; i++) yield return null;
                yield return null; yield return null;
                Assert.AreSame(b, b.focusController.focusedElement, name + ": focused");
                var after = target.resolvedStyle;
                var change = Mathf.Max(Contrast(bgBefore, after.backgroundColor), Contrast(rimBefore, after.borderTopColor));
                Assert.GreaterOrEqual(change, 3f, $"{name}: focus changes it by {change:0.00}:1");
                // The gold glyph on it (End Turn's hourglass, the disc's icon) turns gold-light: >= 3:1 on the focus fill (review N4).
                if (name != "option-sign")
                {
                    var glyphOn = part == "" ? (VisualElement)b : target;
                    Assert.IsTrue(UiIcon.IsFocused(glyphOn), name + ": its glyph is drawn lit");
                    var glyph = Contrast(UiIcon.LineColour(true), after.backgroundColor);
                    Assert.GreaterOrEqual(glyph, 3f, $"{name}: the lit glyph on the focus fill is {glyph:0.00}:1");
                }
                b.Blur();
            }
        }
        finally { card.Dispose(); }
    }

    /// <summary>Review M1: every glyph paints without an error (each is drawn in a laid-out panel, and a second callback on
    /// the same element proves the repaint ran).</summary>
    [UnityTest]
    public IEnumerator EveryGlyphDraws()
    {
        var rt = new RenderTexture(640, 360, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("GlyphTest");
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        try
        {
            yield return null;
            var drawn = new System.Collections.Generic.HashSet<string>();
            foreach (var glyph in UiIcon.Glyphs)
            {
                var icon = new UiIcon { Glyph = glyph };
                icon.style.width = 32; icon.style.height = 32;
                var name = glyph;
                icon.generateVisualContent += _ => drawn.Add(name);
                doc.rootVisualElement.Add(icon);
            }
            doc.rootVisualElement.style.flexDirection = FlexDirection.Row; doc.rootVisualElement.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < 30 && drawn.Count < UiIcon.Glyphs.Count; i++) { doc.rootVisualElement.MarkDirtyRepaint(); yield return null; }
            CollectionAssert.AreEquivalent(UiIcon.Glyphs, drawn, "every glyph was painted");
            LogAssert.NoUnexpectedReceived();
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    /// <summary>Integration bug B6: the camera keys never move UI focus or press a button. The UI action map binds no
    /// keyboard keys to Navigate, and NavigationGuard drops every directional move at the root, so with focus on a HUD button
    /// the WASD/arrow moves (and the W/A/S/D key presses) leave focus where it is, press nothing and never arm New Game.
    /// Tab (Next) still moves focus, so the HUD stays keyboard-reachable.</summary>
    [UnityTest]
    public IEnumerator CameraKeysNeverMoveFocusOrPressAButton()
    {
        // The input asset: no keyboard binding on the UI map's Navigate action.
        var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        Assert.IsNotNull(actions);
        foreach (var b in actions.FindActionMap("UI", true).FindAction("Navigate", true).bindings)
            StringAssert.DoesNotStartWith("<Keyboard>", b.path, "Navigate must not read a camera key");

        using var ui = new UiTestKit();
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statecraft-b6-" + System.Guid.NewGuid().ToString("N"));
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        bridge.Replace(g);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, new SaveStore(dir), toast, Text, () => 4UL);
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        var guard = new NavigationGuard(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        var presses = 0;
        var buttons = ui.Root.Query<Button>().ToList();
        System.Action count = () => presses++;
        foreach (var b in buttons) b.clicked += count;
        try
        {
            foreach (var name in new[] { "nav-cabinet", "new-game", "end-turn", "option-sign" })
            {
                var start = ui.Q<Button>(name);
                start.Focus();
                for (int i = 0; i < 10 && start.focusController.focusedElement != start; i++) yield return null;
                Assert.AreSame(start, start.focusController.focusedElement, name + ": precondition, focused");
                foreach (var d in new[] { NavigationMoveEvent.Direction.Right, NavigationMoveEvent.Direction.Left,
                                          NavigationMoveEvent.Direction.Up, NavigationMoveEvent.Direction.Down })
                {
                    using (var e = NavigationMoveEvent.GetPooled(d)) { e.target = start; start.SendEvent(e); }
                    yield return null;
                    Assert.AreSame(start, start.focusController.focusedElement, $"{name}: a {d} move left focus where it was");
                }
                foreach (var key in new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Q, KeyCode.E })
                {
                    using (var e = KeyDownEvent.GetPooled((char)0, key, EventModifiers.None)) { e.target = start; start.SendEvent(e); }
                    yield return null;
                    Assert.AreSame(start, start.focusController.focusedElement, $"{name}: {key} left focus where it was");
                }
                Assert.AreEqual(0, presses, name + ": the camera keys pressed no button");
                Assert.IsFalse(newGame.Armed, name + ": New Game is not armed");
            }
            // Tab still moves focus: the HUD stays reachable from the keyboard.
            var from = ui.Q<Button>("nav-cabinet");
            from.Focus();
            for (int i = 0; i < 10 && from.focusController.focusedElement != from; i++) yield return null;
            using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = from; from.SendEvent(e); }
            yield return null;
            Assert.AreNotSame(from, from.focusController.focusedElement, "Tab moves focus on");
            Assert.AreEqual(0, presses, "Tab pressed no button");
        }
        finally
        {
            foreach (var b in buttons) b.clicked -= count;
            guard.Dispose(); card.Dispose(); newGame.Dispose(); toast.Dispose();
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }

    /// <summary>Integration bug B7: the toast is opaque and never covers an open screen or the minimap column.</summary>
    [UnityTest]
    public IEnumerator TheToastIsOpaqueAndClearOfTheScreens()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        var toast = ui.Q<Label>("toast");
        toast.text = "Not done: Not enough attention (need 4.0, have 2.0)";
        toast.style.display = DisplayStyle.Flex;
        try
        {
            foreach (var s in ScreenNav.Screens)
            {
                nav.Open(s);
                for (int i = 0; i < 3; i++) yield return null;
                Assert.AreEqual(1f, toast.resolvedStyle.backgroundColor.a, 1e-3f, "opaque");
                Assert.IsFalse(toast.worldBound.Overlaps(ui.Q<VisualElement>("screen-" + s).worldBound), $"the toast {toast.worldBound} covers {s}");
                Assert.IsFalse(toast.worldBound.Overlaps(ui.Q<VisualElement>("right-column").worldBound), "the toast covers the minimap column");
                Assert.GreaterOrEqual(toast.worldBound.yMin, ui.Q<VisualElement>("top-bar").worldBound.yMax, "the toast starts under the bar");
            }
        }
        finally { nav.Dispose(); }
    }

    /// <summary>Integration bug B5, for the promo Cabinet (promo-screens): the "Appoint to ..." heading sits in the side
    /// column under the Prime Minister's card, clear of the portrait grid and its scroll bar and of the candidate list's
    /// scroll bar, inside the column; with the longest office title it breaks only between words (no word is wider than
    /// the column) and takes at most two lines.</summary>
    [UnityTest]
    public IEnumerator TheAppointHeadingHasRoom()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var nav = new ScreenNav(ui.Root);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        try
        {
            nav.Open("cabinet");
            var longest = Cabinet.Lineup(C, bridge.State).Select(x => x.Office).OrderByDescending(o => o.Title.Length).First();
            foreach (var office in new[] { "health_minister", longest.Id })
            {
                cabinet.Select(office);
                for (int i = 0; i < 5; i++) yield return null;
                var title = ui.Q<Label>("cabinet-pick-title");
                StringAssert.StartsWith("Appoint to ", title.text);
                var r = title.worldBound;
                var side = ui.Q<VisualElement>("cabinet-side").worldBound;
                var grid = ui.Q<ScrollView>("cabinet-seats");
                Assert.IsFalse(r.Overlaps(grid.worldBound), $"{office}: the heading {r} overlaps the portrait grid {grid.worldBound}");
                Assert.IsFalse(r.Overlaps(grid.verticalScroller.worldBound), $"{office}: the heading overlaps the grid's scroll bar");
                var list = ui.Q<ScrollView>("cabinet-candidates");
                if (list.verticalScroller.resolvedStyle.display == DisplayStyle.Flex)
                    Assert.IsFalse(r.Overlaps(list.verticalScroller.worldBound), $"{office}: the heading overlaps the candidates' scroll bar");
                Assert.GreaterOrEqual(r.xMin, side.xMin - 0.5f, $"{office}: the heading leaves the side column");
                Assert.LessOrEqual(r.xMax, side.xMax + 0.5f, $"{office}: the heading leaves the side column");
                foreach (var word in title.text.Split(' '))
                {
                    var w = title.MeasureTextSize(word, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                    Assert.LessOrEqual(w, title.contentRect.width + 0.5f, $"{office}: '{word}' ({w:0} px) is wider than the heading ({title.contentRect.width:0} px) and breaks");
                }
                var line = title.MeasureTextSize("A", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
                Assert.LessOrEqual(title.contentRect.height, line * 2f + 1f, $"{office}: '{title.text}' takes more than two lines ({title.contentRect.height:0} px, line {line:0} px)");
            }
        }
        finally { cabinet.Dispose(); nav.Dispose(); }
    }
}
