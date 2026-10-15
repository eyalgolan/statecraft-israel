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

/// <summary>HUD round 2, part 3 (#398): the top bar at about 6.5% of the frame's height, as promo/final/03 and demo2 T29 draw
/// it: the date plate hangs below the bar at its start, the meters take one line of caption and one of value and change, New
/// Game is a round icon named by its tip, and End Turn with the attention line fits the bar. Checked at InfoPanelLayoutTests'
/// 19 window sizes in English and Hebrew (the Hebrew UI mirrors the bar's row, #550), after a turn, with the widest
/// late-game values and date, and with a score line in the plate (#598 adds "Score {n}" there; its room is kept).</summary>
public class TopBarTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    static readonly (int w, int h)[] Windows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1680, 1050), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (1531, 784), (2560, 1080), (1920, 900), (1600, 757), (1366, 657), (960, 600), (390, 844),
    };

    /// <summary>Windows at the one-row tier's edge (review of #624), by their logical width. The panel scales by the plain mean of
    /// the two ratios (match 0.5: scale = (w/1920 + h/1080) / 2, logged by TheOneRowBarReachesFiveByFour), so a fullscreen
    /// 5:4 is 1585, 4:3 is 1646 and a square 1382, not the geometric mean's 1610, 1662 and 1440. Widths 1610, 1612 and 1662,
    /// fullscreen 5:4 and 4:3, either side of NarrowBelowPx (1642 one row, 1635 stacked), and a square window. Below about
    /// 1640 the one-row bar wraps "Public Support" and "International Standing" with the widest values (this round's first
    /// run, at 1563-1612), so 5:4 takes the stacked bar with its icons.</summary>
    static readonly (int w, int h, bool oneRow)[] Boundary =
        { (1315, 1024, false), (1317, 1024, false), (1389, 1024, true), (1280, 1024, false), (1024, 768, true),
          (1360, 1024, true), (1350, 1024, false), (1024, 1024, false) };

    /// <summary>The 16:9 windows, promo 03's frame shape. (21:9 is drawn larger: the panel scales by the geometric mean of the
    /// two ratios, so a 2560x1080 window has 926 panel rows; its share is logged, not pinned.)</summary>
    static readonly (int w, int h)[] PromoShapes = { (1920, 1080), (1280, 720), (1366, 768) };

    /// <summary>The real Game.uxml on the real PanelSettings at w x h, in English or Hebrew, as the game builds it: the bar laid
    /// out (the loading screen) before the HUD is made, a turn played, then the widest values, changes and date, and a
    /// score in the plate. <paramref name="widest"/> false keeps the turn-1 values.</summary>
    static IEnumerator Rendered(int w, int h, bool hebrew, System.Action<VisualElement, UiText> check, bool widest = true, bool withEvent = false)
    {
        var text = hebrew ? He : En;
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("TopBarTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        HudPresenter? hud = null; UiDirection? dir = null; EventCardPresenter? card = null; ScreenNav? nav = null;
        try
        {
            for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
            root.Q("root").RemoveFromClassList("loading");
            var session = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var bridge = new SimBridge(session, text);
            hud = new HudPresenter(root, bridge, text);
            if (withEvent) { nav = new ScreenNav(root); card = new EventCardPresenter(root, bridge, text, null); }
            if (hebrew) dir = new UiDirection(root, bridge);
            bridge.EndTurn();
            if (withEvent)
                for (int t = 0; bridge.State.PendingEvents.Count == 0; t++) { if (t >= 50) Assert.Fail("no event within 50 turns"); bridge.EndTurn(); }
            for (int i = 0; i < 6; i++) yield return null;
            var toast = root.Q<Label>("toast");
            toast.text = text["hud.new_game"]; toast.style.display = DisplayStyle.Flex;   // shown, so its place is checked
            // The plate's score line (#598): the real label when #598 has landed, a stand-in in its place until then.
            var score = root.Q<Label>("score");
            if (score == null)
            {
                score = new Label { name = "score" };
                score.AddToClassList("muted");
                root.Q(className: "date-row").Add(score);
            }
            score.text = hebrew ? "ניקוד 9,999" : "Score 9,999";
            score.style.display = DisplayStyle.Flex;
            if (widest)
            {
                foreach (var (name, delta) in new[] { ("treasury", PlayerText.MoneyChange(C, "treasury", -12.4)), ("immigration", text.Format("hud.delta_people", 12345.0)),
                                                      ("security", "-10%"), ("support", "-10%"), ("standing", "+10%"), ("fx", PlayerText.MoneyChange(C, "fx_reserves", 12.5)) })
                    root.Q<Label>(name + "-delta").text = delta;
                foreach (var (name, value) in new[] { ("treasury", PlayerText.Money(C, "treasury", -123.4)), ("immigration", text.Format("hud.people", 123456.0)),
                                                      ("fx", PlayerText.Money(C, "fx_reserves", 123.4)), ("security", "100%"), ("support", "100%"), ("standing", "100%") })
                    root.Q<Label>(name + "-value").text = value;
                root.Q<Label>("security-level").text = text.Format("hud.level", 5);
                root.Q<Label>("turn").text = text.Format("hud.turn", 120);
                root.Q<Label>("date").text = text.Format("hud.date", PlayerText.Date(new System.DateTime(1956, 9, 30)));
                root.Q<Label>("attention").text = text.Format("hud.attention", 4.0, 4.0);
            }
            dir?.Apply();
            for (int i = 0; i < 8; i++) yield return null;
            check(root, text);
        }
        finally
        {
            card?.Dispose(); dir?.Dispose(); hud?.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>A label's text fits its box: on one line, or wrapped within its height (InfoPanelLayoutTests).</summary>
    static bool TextFits(TextElement l)
    {
        var box = l.contentRect;
        var natural = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        if (natural.x <= box.width + 1f && natural.y <= box.height + 1f) return true;
        if (l.resolvedStyle.whiteSpace != WhiteSpace.Normal) return false;
        var wrapped = l.MeasureTextSize(l.text, box.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
        return wrapped.y <= box.height + 1f;
    }

    static bool Inside(Rect inner, Rect outer, float slack = 0.5f) =>
        inner.xMin >= outer.xMin - slack && inner.xMax <= outer.xMax + slack && inner.yMin >= outer.yMin - slack && inner.yMax <= outer.yMax + slack;

    static bool Shown(VisualElement e)
    {
        for (var x = e; x != null; x = x.parent) if (x.resolvedStyle.display == DisplayStyle.None) return false;
        return true;
    }

    /// <summary>Promo 03 and T29: the one-row bar is about 6.5% of the frame's height (T54: main's was 8.8%), and the date
    /// plate hangs below it, as promo 03's "1936 / Turn 1 / January 1936" plate does (about 8.6% of the height).</summary>
    [UnityTest]
    public IEnumerator TheOneRowBarIsAboutSixAndAHalfPercentOfTheFrame()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in PromoShapes)
                yield return Rendered(w, h, hebrew, (root, text) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var frame = root.worldBound;
                    var bar = root.Q("top-bar").worldBound;
                    float share = bar.height / frame.height;
                    TestContext.WriteLine($"{at}: bar {bar.height:0} of {frame.height:0} = {share:P1}; plate {root.Q(className: "date-box").worldBound}");
                    if (root.Q("root").ClassListContains(HudPresenter.NarrowClass) || root.Q("root").ClassListContains(HudPresenter.TwoRowClass))
                        problems.Add($"{at}: not the one-row bar");
                    if (share < 0.060f || share > 0.070f) problems.Add($"{at}: the bar is {share:P1} of the frame (promo 03 and T29: 6.5%)");
                    var plate = root.Q(className: "date-box").worldBound;
                    if (!(plate.yMax > bar.yMax + 4f)) problems.Add($"{at}: the date plate {plate} does not hang below the bar {bar}");
                }, widest: false);
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>At every window size, in English and Hebrew, with the widest values: no text in the bar or the plate is
    /// clipped (meters, date, score, Attention, End Turn, New Game's tip text aside); the meters, the plate, New Game and the
    /// turn box do not overlap and stay in the window; the meters' labels stay in the bar; the row mirrors in Hebrew (the plate
    /// at the bar's right end, End Turn at its left); and every panel under the bar, and under the hanging plate, starts below
    /// them.</summary>
    [UnityTest]
    public IEnumerator TheBarFitsEveryWindowInEnglishAndHebrew()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows.Concat(Boundary.Select(b => (b.w, b.h))).Distinct())
                yield return Rendered(w, h, hebrew, (root, text) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var frame = root.worldBound;
                    var barEl = root.Q("top-bar");
                    var bar = barEl.worldBound;
                    var plateEl = root.Q(className: "date-box");
                    var plate = plateEl.worldBound;
                    var turnBox = root.Q(className: "turn-box").worldBound;
                    var newGame = root.Q("new-game").worldBound;
                    var meters = root.Q("meters").Children().ToList();
                    // Preconditions: nothing below is checked against an empty rect.
                    foreach (var (name, r) in new[] { ("bar", bar), ("plate", plate), ("turn box", turnBox), ("New Game", newGame) })
                        if (!(r.width > 0 && r.height > 0)) problems.Add($"{at}: precondition: the {name} is laid out");
                    foreach (var l in barEl.Query<Label>().ToList().Concat<TextElement>(barEl.Query<Button>().ToList().Where(b => b.name == "end-turn"))
                                 .Where(l => !string.IsNullOrEmpty(l.text) && Shown(l)))
                    {
                        if (!TextFits(l)) problems.Add($"{at}: '{l.text}' ({l.name}) is clipped in {l.contentRect}");
                        if (!Inside(l.worldBound, frame)) problems.Add($"{at}: '{l.text}' {l.worldBound} leaves the window");
                    }
                    foreach (var l in plateEl.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && Shown(l)))
                        if (!Inside(l.worldBound, plate)) problems.Add($"{at}: '{l.text}' {l.worldBound} leaves the plate {plate}");
                    foreach (var m in meters)
                        foreach (var l in m.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && Shown(l)))
                        {
                            if (!Inside(l.worldBound, bar, 1f)) problems.Add($"{at}: '{l.text}' {l.worldBound} leaves the bar {bar}");
                            if (!Inside(l.worldBound, m.worldBound, 1f)) problems.Add($"{at}: '{l.text}' leaves {m.name} {m.worldBound}");
                        }
                    var blocks = new List<(string name, Rect r)> { ("the plate", plate), ("New Game", newGame), ("the turn box", turnBox) };
                    blocks.AddRange(meters.Select(m => (m.name, m.worldBound)));
                    // Neighbours in a row touch; a half-pixel inset keeps float rounding at a shared edge from counting as overlap.
                    static Rect In(Rect r) => new Rect(r.x + 0.5f, r.y + 0.5f, r.width - 1f, r.height - 1f);
                    for (int i = 0; i < blocks.Count; i++)
                    {
                        if (!Inside(blocks[i].r, frame)) problems.Add($"{at}: {blocks[i].name} {blocks[i].r} leaves the window");
                        for (int j = i + 1; j < blocks.Count; j++)
                            if (In(blocks[i].r).Overlaps(In(blocks[j].r))) problems.Add($"{at}: {blocks[i].name} {blocks[i].r} overlaps {blocks[j].name} {blocks[j].r}");
                    }
                    if (!Inside(newGame, bar) || !Inside(turnBox, bar)) problems.Add($"{at}: New Game {newGame} or the turn box {turnBox} leaves the bar {bar}");
                    // The row mirrors in Hebrew (#550): the plate at the reading start, End Turn at the end.
                    bool plateFirst = hebrew ? plate.center.x > turnBox.center.x : plate.center.x < turnBox.center.x;
                    if (!plateFirst) problems.Add($"{at}: the plate {plate} is not at the {(hebrew ? "right" : "left")} of End Turn {turnBox}");
                    // Everything under the bar starts below it, and below the plate where it lies under the plate.
                    var screen = root.Q("screen-why");
                    foreach (var name in new[] { "right-column", "left-column", "event-layer", "toast" })
                    {
                        var e = root.Q(name);
                        var r = e.worldBound;
                        if (!(r.height > 0)) problems.Add($"{at}: precondition: {name} is laid out");
                        if (r.yMin < bar.yMax - 0.5f) problems.Add($"{at}: {name} {r} starts in the bar {bar}");
                        if (r.xMin < plate.xMax && r.xMax > plate.xMin && r.yMin < plate.yMax - 0.5f && name != "event-layer" && name != "right-column")
                            problems.Add($"{at}: {name} {r} starts under the hanging plate {plate}");
                    }
                    // The decision layer's "Decision waiting" sits at its top right: clear of the plate in either language.
                    var layer = root.Q("event-layer").worldBound;
                    var waiting = new Rect(layer.xMax - 260f, layer.yMin, 260f, 40f);
                    if (waiting.Overlaps(plate)) problems.Add($"{at}: Decision waiting's corner {waiting} lies under the plate {plate}");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Review of #624: the one-row bar's edge. 4:3 (1646 logical px) and 1662 keep the one-row bar with its icons; a
    /// fullscreen 5:4 (1585) and 1610 or 1612 take the stacked bar with its icons (the one-row bar does not hold the widest
    /// values there); just above NarrowBelowPx (1642) one row, just below (1635) stacked; a square window (1382) the stacked
    /// bar without icons. The logical widths are logged, the numbers HudPresenter's notes quote.</summary>
    [UnityTest]
    public IEnumerator TheOneRowBarReachesFiveByFour()
    {
        var problems = new List<string>();
        foreach (var (w, h, oneRow) in Boundary)
            yield return Rendered(w, h, false, (root, text) =>
            {
                var r = root.Q("root");
                bool narrow = r.ClassListContains(HudPresenter.NarrowClass);
                TestContext.WriteLine($"{w}x{h}: panel {root.worldBound.width:0.0} x {root.worldBound.height:0.0}, bar {root.Q("top-bar").worldBound.width:0.0}; narrow {narrow}, compact {root.Q("top-bar").ClassListContains(HudPresenter.CompactClass)}, two rows {r.ClassListContains(HudPresenter.TwoRowClass)}");
                if (narrow == oneRow) problems.Add($"{w}x{h} ({root.Q("top-bar").worldBound.width:0} px): {(oneRow ? "not the one-row bar" : "the one-row bar")}");
                if (oneRow && root.Q("top-bar").ClassListContains(HudPresenter.CompactClass)) problems.Add($"{w}x{h}: the one-row bar dropped its icons");
            });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>A smaller bar must not open a gap in the decision card's backdrop (the fail-open of #602's review): with a
    /// card open, at every window size and in both languages, the backdrop's full-width band starts at the bar's bottom and
    /// reaches the column's top, where the column's strip takes over; points just under the bar are the band's; and the
    /// hanging date plate is dimmed evenly, by the band alone, across its width (review of #624: it was dimmed from x = 72
    /// with a hard edge).</summary>
    [UnityTest]
    public IEnumerator WithACardOpenTheBackdropMeetsTheBar()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                yield return Rendered(w, h, hebrew, (root, text) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    if (!root.Q("root").ClassListContains(EventCardPresenter.ModalClass)) { problems.Add($"{at}: precondition: the card is open"); return; }
                    var bar = root.Q("top-bar").worldBound;
                    var band = root.Q("event-scrim-top").worldBound;
                    var scrim = root.Q("event-scrim").worldBound;
                    var gutter = root.Q("event-gutter").worldBound;
                    var column = root.Q("left-column").worldBound;
                    if (!(band.height > 0) || !(scrim.height > 0) || !(gutter.height > 0)) { problems.Add($"{at}: precondition: the backdrop shows"); return; }
                    if (Mathf.Abs(band.yMin - bar.yMax) > 0.5f) problems.Add($"{at}: the band starts at {band.yMin:0}, not at the bar's bottom {bar.yMax:0}");
                    if (band.xMin > root.worldBound.xMin + 0.5f || band.xMax < root.worldBound.xMax - 0.5f) problems.Add($"{at}: the band {band} is not the window's width");
                    if (band.yMax < scrim.yMin - 0.5f || band.yMax < gutter.yMin - 0.5f) problems.Add($"{at}: a gap between the band {band} and the backdrop {scrim} or the strip {gutter}");
                    if (band.yMax > column.yMin + 0.5f) problems.Add($"{at}: the band {band} reaches over the column's discs {column}");
                    foreach (var x in new[] { 0.1f, 0.5f, 0.9f })
                    {
                        var probe = new Vector2(root.worldBound.width * x, bar.yMax + 2f);
                        var picked = root.panel.Pick(probe);
                        if (picked != root.Q("event-scrim-top")) problems.Add($"{at}: {probe}, just under the bar, is {(picked == null ? "the map" : picked.name)}, not the band");
                    }
                    var plate = root.Q("date-plate").worldBound;
                    if (scrim.yMin < plate.yMax - 0.5f && scrim.xMin < plate.xMax && scrim.xMax > plate.xMin) problems.Add($"{at}: the backdrop {scrim} also dims the plate {plate}: two shades");
                    if (gutter.yMin < plate.yMax - 0.5f && gutter.xMax > plate.xMin) problems.Add($"{at}: the column's strip {gutter} lies under the plate {plate}");
                    if (plate.yMax > bar.yMax + 4f)
                        foreach (var x in new[] { plate.xMin + 6f, plate.center.x, plate.xMax - 6f })
                        {
                            var p = new Vector2(x, (bar.yMax + plate.yMax) / 2);
                            var picked = root.panel.Pick(p);
                            if (picked != root.Q("event-scrim-top")) problems.Add($"{at}: the plate's hanging band at {p} is under {picked?.name ?? "nothing"}, not the band alone");
                        }
                }, widest: false, withEvent: true);
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>New Game is a round icon, as promo 03's bar buttons are: its name stays its text (the event log reads it) and
    /// is its tooltip in both languages, shown beside it on hover and on focus; no caption shows.</summary>
    [UnityTest]
    public IEnumerator NewGameIsARoundIconNamedInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            yield return Rendered(1920, 1080, hebrew, (root, text) =>
            {
                string at = hebrew ? "he" : "en";
                var b = root.Q<Button>("new-game");
                if (b.text != text["hud.new_game"]) problems.Add($"{at}: its text is '{b.text}'");
                if (b.tooltip != text["hud.new_game"] || string.IsNullOrWhiteSpace(b.tooltip)) problems.Add($"{at}: its tooltip is '{b.tooltip}'");
                if (b.resolvedStyle.color.a > 0.01f) problems.Add($"{at}: its caption shows");
                if (b.Q<UiIcon>(className: "nav-disc")?.Glyph != "restart") problems.Add($"{at}: no restart disc");
                var tip = root.Q<Label>(HoverTip.TipName);
                using (var e = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = b.worldBound.center })) { e.target = b; b.SendEvent(e); }
                if (tip.style.display.value != DisplayStyle.Flex || tip.text != b.tooltip) problems.Add($"{at}: hovering shows no tip");
                using (var e = PointerLeaveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = Vector2.zero })) { e.target = b; b.SendEvent(e); }
                if (tip.style.display.value != DisplayStyle.None) problems.Add($"{at}: the tip stays after leave");
            }, widest: false);
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>tools/headed.py clicks End Turn at a fixed point of its 1568x752 playtest window, and uses another bar point
    /// as "a panel" for the wheel checks: both must still land on what they name now that the bar is 70 px (part 3).</summary>
    [UnityTest]
    public IEnumerator HeadedPyClicksLandOnTheBar()
    {
        var src = System.IO.File.ReadAllText(System.IO.Path.Combine(Paths.RepoRoot, "tools", "headed.py"));
        Vector2 Point(string pattern)
        {
            var m = System.Text.RegularExpressions.Regex.Match(src, pattern);
            Assert.IsTrue(m.Success, pattern);
            return new Vector2(float.Parse(m.Groups[1].Value), float.Parse(m.Groups[2].Value));
        }
        var endTurn = Point(@"END_TURN, OPTION, MAP_UNDER_WHY = \((\d+), (\d+)\)");
        var panel = Point(@"WHEEL_VIEWPORT, START_NEW, MAP, PANEL = \(1568, 752\), \(\d+, \d+\), \(\d+, \d+\), \((\d+), (\d+)\)");
        yield return Rendered(1568, 752, false, (root, text) =>
        {
            float scale = 1568f / root.worldBound.width;
            Rect Css(VisualElement e) { var r = e.worldBound; return new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale); }
            var button = Css(root.Q("end-turn"));
            TestContext.WriteLine($"1568x752: End Turn at {button} CSS px, centre {button.center}; the bar {Css(root.Q("top-bar"))}");
            Assert.IsTrue(button.Contains(endTurn), $"headed.py's END_TURN {endTurn} is not on End Turn {button} (centre {button.center})");
            Assert.IsTrue(Css(root.Q("top-bar")).Contains(panel), $"headed.py's PANEL {panel} is not on the bar {Css(root.Q("top-bar"))}");
        }, widest: false);
    }

    /// <summary>The audit's Tab order is kept: Cabinet, Budget, Defence, Why? (the left column), New Game, End Turn, then the
    /// minimap's buttons.</summary>
    [UnityTest]
    public IEnumerator TheTabOrderIsKept()
    {
        foreach (var hebrew in new[] { false, true })
            yield return Rendered(1920, 1080, hebrew, (root, text) =>
            {
                var order = root.Query<Button>().ToList().Where(e => e.focusable && e.canGrabFocus && e.tabIndex >= 0)
                    .Select(e => e.name).Where(n => n.StartsWith("nav-") || n == "new-game" || n == "end-turn" || n.StartsWith("minimap-")).ToList();
                CollectionAssert.AreEqual(new[] { "nav-cabinet", "nav-budget", "nav-defence", "nav-why", "new-game", "end-turn", "minimap-overview", "minimap-labels", "minimap-borders" },
                    order, (hebrew ? "he" : "en") + ": the Tab order");
            }, widest: false);
    }
}
