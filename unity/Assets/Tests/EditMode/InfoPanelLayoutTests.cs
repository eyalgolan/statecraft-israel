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

/// <summary>HUD round 1 (#398): the info panel in the promo's shape (promo/final/03): a compact hint before a hex is chosen,
/// the hex's facts as icon rows (only what the game models: no yields, no land-use name), its text inside the panel at
/// every window size (#397), and the panels' filigree trim, which takes no clicks.</summary>
public class InfoPanelLayoutTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static string MapFile(string name) => File.ReadAllText(Path.Combine(Paths.Content, "map", name));
    static MapRegions Regions() => new MapRegions(Subdistricts1948.Parse(MapFile(Subdistricts1948.FileName)), Armistice1949.Parse(MapFile(Armistice1949.FileName)));
    static Dictionary<string, string> Names() => SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName)).ToDictionary(t => t.Id, t => t.Name);

    /// <summary>Every window size the HUD's tests use (UiLayoutTests, PromoHudTests, StartScreenTests) and a 16:10 laptop.</summary>
    internal static readonly (int w, int h)[] Windows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1680, 1050), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (1531, 784), (2560, 1080), (1920, 900), (1600, 757), (1366, 657), (960, 600), (390, 844),
    };

    /// <summary>Windows where the city's own hex must show its projects behind the toggle (owner ruling on #1150, 21 Oct 2026):
    /// the stacked bars and the short window whose full list ran into the left column. 1920x1080 keeps the full list.</summary>
    static readonly (int w, int h)[] CollapsedWindows = { (1024, 1024), (960, 1080), (390, 844), (1366, 657) };

    /// <summary>The real Game.uxml on the real PanelSettings at w x h, out of its loading state, with the top bar, an open event
    /// card and the info panel (with the region lines) on the session given.</summary>
    static IEnumerator Rendered(int w, int h, GameSession g, System.Action<VisualElement, InfoPanelPresenter> act, System.Action<VisualElement> check,
                                UiText? text = null, bool hebrew = false, System.Action<VisualElement>? settled = null,
                                System.Func<Statecraft.Game.Map.Units.PlacedToken?>? unit = null, System.Func<int, string?>? unitName = null)
    {
        var t = text ?? Text;
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("InfoPanelTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var bridge = new SimBridge(g, t);
        var hud = new HudPresenter(root, bridge, t);
        var card = new EventCardPresenter(root, bridge, t, null);
        var info = new InfoPanelPresenter(root, bridge, t, Names(), regions: Regions(), selectedUnit: unit, unitName: unitName);
        try
        {
            act(root, info);
            for (int i = 0; i < 20 && float.IsNaN(root.Q("info-panel").worldBound.width); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;   // text measured after the first layout settles
            // A press on a button the stylesheet shows only once the HUD has its tier (the bar's classes come with the layout).
            if (settled != null) { settled(root); for (int i = 0; i < 3; i++) yield return null; }
            check(root);
        }
        finally
        {
            info.Dispose(); card.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>A label's text fits its box: on one line, or wrapped within its height.</summary>
    static bool TextFits(TextElement l)
    {
        var box = l.contentRect;
        var natural = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        if (natural.x <= box.width + 1f) return true;
        if (l.resolvedStyle.whiteSpace != WhiteSpace.Normal) return false;
        var wrapped = l.MeasureTextSize(l.text, box.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
        // Text and layout round to raster pixels: at fractional panel scale one pixel can exceed one logical px.
        var pixel = l.panel == null ? 1f : 1f / l.panel.scaledPixelsPerPoint;
        return wrapped.y <= box.height + pixel + 0.01f;
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    static bool Shown(VisualElement e)
    {
        for (var x = e; x != null; x = x.parent) if (x.resolvedStyle.display == DisplayStyle.None) return false;
        return true;
    }

    /// <summary>The session one turn in, with an event open (the card shows), as a player sees it after the first End Turn.</summary>
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

    /// <summary>#397: with Lydda's hex chosen (the longest region line: "Ramle Sub-District, Lydda District (Mandate
    /// sub-districts, 1947)") and an event open, every shown label of the info panel lies inside the panel and its text fits
    /// its box, the panel lies in the window, and it overlaps neither the minimap nor the event card (unless the card's backdrop
    /// covers it, the panel disabled under it), at every window size.</summary>
    [UnityTest]
    public IEnumerator InfoPanelTextStaysInsideThePanel()
    {
        var g = WithAnEventOpen();
        var lod = C.SettlementHexes.Single(s => s.Id == "lod");
        var problems = new List<string>();
        foreach (var (w, h) in Windows)
            yield return Rendered(w, h, g, (root, info) => info.Show(lod.Q, lod.R), root =>
            {
                var panel = root.Q("info-panel");
                var window = root.worldBound;
                Assert.AreEqual(DisplayStyle.Flex, root.Q("event-card").resolvedStyle.display, $"{w}x{h}: precondition: an event is open");
                Assert.IsNotEmpty(root.Q<Label>("info-region").text, $"{w}x{h}: precondition: the hex names its region");
                if (!Inside(panel.worldBound, window)) problems.Add($"{w}x{h}: the panel {panel.worldBound} leaves the window {window}");
                if (panel.worldBound.Overlaps(root.Q("minimap").worldBound)) problems.Add($"{w}x{h}: the panel {panel.worldBound} overlaps the minimap {root.Q("minimap").worldBound}");
                // The open decision card sits in the middle over a backdrop (decision cards, 2026-10-13): the panel may lie
                // under the card, and then it waits out of reach until the card is hidden or answered.
                if (panel.worldBound.Overlaps(root.Q("event-card").worldBound)
                    && !(root.Q("root").ClassListContains(EventCardPresenter.ModalClass) && !panel.enabledInHierarchy))
                    problems.Add($"{w}x{h}: the panel {panel.worldBound} overlaps the event card {root.Q("event-card").worldBound} with no backdrop over it");
                foreach (var l in panel.Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && Shown(l)))
                {
                    if (!Inside(l.worldBound, panel.worldBound)) problems.Add($"{w}x{h}: '{l.text}' {l.worldBound} leaves the panel {panel.worldBound}");
                    if (!TextFits(l)) problems.Add($"{w}x{h}: '{l.text}' does not fit its box {l.contentRect} ({l.name})");
                }
            });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>GDD Phase 1 PR 8 (#381): the city panel with two training cohorts (a Cancel each) and the
    /// designated site's panel (two idle halutz groups, so two Build buttons), and a hex one city can build a city project on
    /// (#1122; the city also with the ma'abarot policy, so every project kind is a button), at every window size in English and Hebrew:
    /// the panel lies in the window, clear of the minimap, the left column and the top bar, and every shown label and
    /// button lies inside the panel with its text fitting its box.</summary>
    [UnityTest]
    public IEnumerator TheGddCityAndSitePanelsFitEveryWindow()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var city = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var site = C.Sites.Single(s => s.Id == "mefalsim");
        var start = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;
        var lone = C.Hexes.Where(x => x.Land && Projects.CitiesFor(C, start, x.Q, x.R).Count == 1).OrderBy(x => x.Q).ThenBy(x => x.R).First();
        var hula = C.Sites.Single(s => s.Id == "hula");   // #1124: the swamp and its waiting field, the tallest site hex
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                foreach (var (what, q, r) in new[] { ("city", city.Q, city.R), ("site", site.Q, site.R), ("projects", lone.Q, lone.R), ("city with the policy", city.Q, city.R),
                    ("field", site.Q, site.R), ("hula", hula.Q, hula.R) })
                {
                    var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
                    var s = g.State;
                    for (int k = 0; k < 2; k++) { var id = s.NextId++; s.Cohorts.Add(id, new Cohort(id, k == 0 ? "recruit" : "halutz", "tel_aviv", 12, 4)); }
                    for (int k = 0; k < 2; k++) { var id = s.NextId++; s.Units.Add(id, new Unit(id, "halutz", city.Q, city.R)); }
                    // With the ma'abarot policy every project kind is a Build button: the city's tallest project part (#1122).
                    // #1511: the city's own hex also lays out Return craftsmen, Reassign craftsmen, halutz and veteran, and a Return per pioneer group.
                    if (what.StartsWith("city")) { var x = s.Cities["tel_aviv"]; s.Cities["tel_aviv"] = x.WithCraftsmen(2).WithQualified(new Qualified(1, 1, new[] { 3, 5 })).WithCounts(x.Civilians, x.Employed, x.Refugees); }
                    if (what == "city with the policy") s.Policies[MapActions.MaabarotPolicy] = 1;
                    if (what == "field") s.Sites[site.Id] = SiteWork.Built(site.Id, s.Date);   // #1124: the yishuv built, its field's Plow
                    var at = $"{(hebrew ? "he" : "en")} {w}x{h} {what}";
                    yield return Rendered(w, h, g, (root, info) => info.Show(q, r), root =>
                    {
                        var panel = root.Q("info-panel"); var window = root.worldBound;
                        var host = what == "projects" ? GddHexPanel.ProjectsHostName : GddHexPanel.HostName;
                        if (root.Q(host).resolvedStyle.display != DisplayStyle.Flex) { problems.Add($"{at}: precondition: {host} shows"); return; }
                        // Owner ruling on #1150 (21 Oct 2026): on a stacked bar or a short window the city's own hex shows its
                        // projects collapsed behind one toggle, so the city cases there are the collapsed state; elsewhere the full list.
                        // The opened list may run past the panel there: the ruling accepts it (TheCityProjectsToggleOpensAndCloses...).
                        if (what.StartsWith("city"))
                        {
                            var collapsed = root.Q("root").ClassListContains(HudPresenter.TwoRowClass) || panel.ClassListContains(InfoPanelPresenter.ShortClass);
                            if (collapsed ? (w, h) == (1920, 1080) : CollapsedWindows.Contains((w, h))) problems.Add($"{at}: the city projects collapse: {collapsed}, expected {!collapsed}");
                            var toggle = root.Q(GddHexPanel.ToggleName); var list = root.Q(GddHexPanel.ListName);
                            if (toggle == null || list == null) problems.Add($"{at}: precondition: the city's projects have a toggle and a list");
                            else if (Shown(toggle) != collapsed || Shown(list) == collapsed) problems.Add($"{at}: toggle shown {Shown(toggle)}, list shown {Shown(list)}, collapsed {collapsed}");
                        }
                        if (!Inside(panel.worldBound, window)) problems.Add($"{at}: the panel {panel.worldBound} leaves the window {window}");
                        foreach (var other in new[] { "minimap", "left-column", "top-bar" })
                            if (panel.worldBound.Overlaps(root.Q(other).worldBound)) problems.Add($"{at}: the panel {panel.worldBound} overlaps the {other} {root.Q(other).worldBound}");
                        foreach (var l in panel.Query<TextElement>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && Shown(l)))
                        {
                            if (!Inside(l.worldBound, panel.worldBound)) problems.Add($"{at}: '{l.text}' {l.worldBound} leaves the panel {panel.worldBound}");
                            if (!TextFits(l)) problems.Add($"{at}: '{l.text}' does not fit its box {l.contentRect} ({l.name})");
                        }
                    }, hebrew ? he : Text, hebrew);
                }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>GDD Phase 5 PR 15, slices 1 to 3 (#754): Golani's card at the start with its Move, Fortify and Hold buttons,
    /// as it is, picking a Move's hex (the pick line under the card's lines), with Fortify queued (its line), after the
    /// armistices with Send home offered (#1309: its button) and queued (its line, and no other order: #1527's follow-up), on the turn
    /// after a Hold (Hold refused, with the sim's reason), and picking an Attack's hex (an enemy put in its reach if none is) (the
    /// pick line, the enemy's name, strength ratio and outcome bands), at every window size in English and Hebrew: the panel lies in the
    /// window, and every shown label and button lies inside the panel with its text fitting its box.</summary>
    [UnityTest]
    public IEnumerator TheUnitCardWithItsOrdersFitsEveryWindow()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var units = Statecraft.Game.Map.Units.UnitData.Parse(MapFile(Statecraft.Game.Map.Units.UnitData.DefaultFileName));
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                foreach (var state in new[] { "", "info-move", "info-fortify", "armistice", "info-send-home", "holding", "info-attack" })
                {
                    var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
                    var token = Statecraft.Game.Map.Units.UnitRoster.FromState(units, C, g.State, null, (row, a, q, r) => a).Single(x => x.Unit.Id == "golani");
                    if (state == "holding") g.Step(new Command[] { new UnitOrder(UnitCard.SimUnit(C, g.State, token.Unit)!.Value, Orders.Hold) });
                    if (state == "info-attack") UnitOrdersTests.EnemyInGolanisReach(g.State);
                    // "armistice": Send home is offered beside the other orders; "info-send-home": pressed, the unit takes no other order.
                    if (state == "armistice" || state == "info-send-home") foreach (var front in Prisoners.Fronts) g.State.ChosenOptions["armistice_" + front] = "sign";
                    var goingHome = state == "info-send-home";
                    var t = hebrew ? he : Text;
                    var names = Statecraft.Game.Map.Units.UnitRoster.FromState(units, C, g.State, null, (row, a, q, r) => a)
                        .Where(x => UnitCard.SimUnit(C, g.State, x.Unit) != null)
                        .ToDictionary(x => UnitCard.SimUnit(C, g.State, x.Unit)!.Value, x => hebrew && x.Unit.NameHe != "" ? x.Unit.NameHe : x.Unit.Name);
                    Statecraft.Game.Map.Units.PlacedToken? golani = new Statecraft.Game.Map.Units.PlacedToken(token, new Statecraft.Game.Map.Units.AnchorSpot(0, 0, 0, 0, false));
                    var pressed = state.StartsWith("info-");
                    var at = $"{(hebrew ? "he" : "en")} {w}x{h} {state}";
                    yield return Rendered(w, h, g, (root, info) => info.ShowUnit(), root =>
                    {
                        var panel = root.Q("info-panel"); var window = root.worldBound;
                        var move = root.Q<Button>("info-move");
                        if (Shown(move) == goingHome) { problems.Add($"{at}: precondition: the Move button shows unless the unit is going home"); return; }
                        if (Shown(root.Q<Button>("info-send-home")) != (state == "armistice")) problems.Add($"{at}: Send home visibility");
                        var lines = root.Q<Label>("info-settlements").text;
                        foreach (var (key, when) in new[] { ("unit.move_pick", "info-move"), ("unit.fortify_queued", "info-fortify"), ("unit.send_home_queued", "info-send-home"), ("unit.attack_pick", "info-attack") })
                            if (lines.Contains((hebrew ? he : Text)[key]) != (state == when)) problems.Add($"{at}: {key} shows: {state != when}");
                        if (Shown(root.Q<Button>("info-hold")) == (state == "holding" || goingHome)) problems.Add($"{at}: precondition: Hold shows only while it is not held and the unit is not going home");
                        if (!Inside(panel.worldBound, window)) problems.Add($"{at}: the panel {panel.worldBound} leaves the window {window}");
                        foreach (var l in panel.Query<TextElement>().ToList().Where(l => !string.IsNullOrEmpty(l.text) && Shown(l)))
                        {
                            if (!Inside(l.worldBound, panel.worldBound)) problems.Add($"{at}: '{l.text}' {l.worldBound} leaves the panel {panel.worldBound}");
                            if (!TextFits(l)) problems.Add($"{at}: '{l.text}' does not fit its box {l.contentRect} ({l.name})");
                        }
                    }, t, hebrew, root =>
                    {
                        if (!pressed) return;
                        var hide = root.Q<Button>("event-hide");
                        if (hide != null && Shown(hide)) UiTestKit.Press(hide);   // the turn's decision card disables the panel under it
                        UiTestKit.Press(root.Q<Button>(state));
                    }, () => golani, id => names.TryGetValue(id, out var n) ? n : null);
                }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Owner ruling on #1150 (21 Oct 2026): on a stacked bar (1024x1024) the city's own hex shows "City projects (N)", N
    /// the kinds listed, and the list behind it; a tap opens it and another closes it. A hex only one city reaches keeps the full
    /// list there, and the city's own hex keeps it on a tall one-row window (1920x1080). English and Hebrew.</summary>
    [UnityTest]
    public IEnumerator TheCityProjectsToggleOpensAndClosesTheListOnAStackedBar()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var city = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var lone = C.Hexes.Where(x => x.Land && Projects.CitiesFor(C, g.State, x.Q, x.R).Count == 1).OrderBy(x => x.Q).ThenBy(x => x.R).First();
        foreach (var hebrew in new[] { false, true })
        {
            var t = hebrew ? he : Text;
            for (int presses = 0; presses < 3; presses++)
            {
                var n = presses;
                yield return Rendered(1024, 1024, g, (root, info) => info.Show(city.Q, city.R), root =>
                {
                    var toggle = root.Q<Button>(GddHexPanel.ToggleName);
                    Assert.IsTrue(Shown(toggle), $"{hebrew} {n}: the toggle shows");
                    Assert.AreEqual(t.Format("city.projects_toggle", C.ProjectKinds.Count), toggle.text);
                    Assert.AreEqual(n == 1, Shown(root.Q(GddHexPanel.ListName)), $"{hebrew}: the list after {n} presses");
                }, t, hebrew, root =>
                {
                    // The turn's decision card opens over the map and disables the panel under it: hidden first, as a player would.
                    UiTestKit.Press(root.Q<Button>("event-hide"));
                    Assert.IsTrue(root.Q("info-panel").enabledInHierarchy, "precondition: the panel takes the press");
                    for (int i = 0; i < n; i++) UiTestKit.Press(root.Q<Button>(GddHexPanel.ToggleName));
                });
            }
            foreach (var (w, h, q, r) in new[] { (1024, 1024, lone.Q, lone.R), (1920, 1080, city.Q, city.R) })
                yield return Rendered(w, h, g, (root, info) => info.Show(q, r), root =>
                {
                    var toggle = root.Q(GddHexPanel.ToggleName);
                    Assert.IsTrue(toggle == null || !Shown(toggle), $"{hebrew} {w}x{h} ({q}, {r}): no toggle");
                    Assert.IsTrue(root.Q(GddHexPanel.ProjectsHostName).Query<Button>().ToList().Any(b => b.name != GddHexPanel.ToggleName && Shown(b)), $"{hebrew} {w}x{h} ({q}, {r}): the Build buttons show");
                }, t, hebrew);
        }
    }

    /// <summary>The empty hint stays inside the window and clear of the minimap in both languages: one line on desktop,
    /// at most two on the two-row and phone tiers, with no terrain sign, fact rows or spare panel height.</summary>
    [UnityTest]
    public IEnumerator TheEmptyInfoPanelHintFitsBothLanguages()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (960, 1080), (390, 844) })
                yield return Rendered(w, h, g, (root, info) => { }, root =>
                {
                    var at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var panel = root.Q("info-panel");
                    var hint = root.Q<Label>("info-title");
                    Assert.AreEqual((hebrew ? he : Text)["info.hint"], hint.text, at);
                    Assert.IsTrue(TextFits(hint), $"{at}: the hint fits");
                    Assert.IsTrue(Inside(hint.worldBound, panel.worldBound), $"{at}: the hint fits the panel");
                    Assert.IsTrue(Inside(panel.worldBound, root.worldBound), $"{at}: the hint panel fits the window");
                    Assert.IsFalse(panel.worldBound.Overlaps(root.Q("minimap").worldBound), $"{at}: the hint stays clear of the minimap");
                    var lines = root.Q("root").ClassListContains("hud-two-row") ? 2 : 1;
                    var natural = hint.MeasureTextSize(hint.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(hint.contentRect.height, natural.y * lines + 1f, $"{at}: the hint uses at most {lines} lines");
                    Assert.AreEqual(DisplayStyle.None, root.Q("info-thumb").resolvedStyle.display, $"{at}: no terrain sign");
                    foreach (var row in panel.Query(className: InfoPanelPresenter.RowClass).ToList())
                        Assert.IsFalse(Shown(row), $"{at}: no fact row before a hex is chosen");
                    var s = panel.resolvedStyle;
                    var tightHeight = hint.worldBound.height + hint.resolvedStyle.marginTop + hint.resolvedStyle.marginBottom
                        + s.paddingTop + s.paddingBottom + s.borderTopWidth + s.borderBottomWidth;
                    Assert.LessOrEqual(panel.worldBound.height, tightHeight + 4f, $"{at}: the empty panel {panel.worldBound} hugs its hint ({hint.worldBound})");
                }, hebrew ? he : Text, hebrew);
    }

    /// <summary>The hex's facts in the promo's rhythm, as icon rows: the region and the settlements each sit in a row whose
    /// first child is a drawable sign; rows with nothing to say are hidden; and no tile yield appears (owner ruling).</summary>
    [UnityTest]
    public IEnumerator TheFactsAreIconRowsAndThereAreNoYields()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        yield return Rendered(1920, 1080, g, (root, info) => info.Show(tlv.Q, tlv.R), root =>
        {
            foreach (var name in new[] { "info-region", "info-settlements" })
            {
                var label = root.Q<Label>(name);
                Assert.IsNotEmpty(label.text, name);
                var row = label.parent;
                Assert.IsTrue(row.ClassListContains(InfoPanelPresenter.RowClass), $"{name} sits in a fact row");
                Assert.IsTrue(Shown(row), $"{name}: its row shows");
                var sign = row.Children().First() as UiIcon;
                Assert.IsNotNull(sign, $"{name}: the row starts with its sign");
                CollectionAssert.Contains(UiIcon.Glyphs, sign!.Glyph, name);
                Assert.Less(sign.worldBound.xMax, label.worldBound.xMin + 0.5f, $"{name}: the sign is before the text");
            }
            foreach (var row in root.Q("info-panel").Query(className: InfoPanelPresenter.RowClass).ToList())
            {
                var text = row.Q<Label>()?.text ?? "";
                Assert.AreEqual(text.Length > 0, Shown(row), $"a row shows exactly when it has text ('{text}')");
            }
            var words = root.Q("info-panel").Query<Label>().ToList().Select(l => l.text).ToList();
            foreach (var yield in new[] { "Food", "Production", "Culture", "grove", "Grove" })
                Assert.IsFalse(words.Any(w => w.Contains(yield)), $"no '{yield}' in the panel (owner ruling: no yields; land use is modelled, not sourced)");
        });
    }

    /// <summary>When both map actions are refused for the same reason (in 1948: "...can be placed from 1 January 1950"), the
    /// panel says it once and shows no buttons; when they are refused for different reasons, or allowed, the buttons show.</summary>
    [Test]
    public void OneSharedRefusalShowsOnceWithoutButtons()
    {
        using var ui = new UiTestKit();
        var legacy = SimBridgeGddTests.Legacy;   // the GDD loop shows no map-action button or reason (R-PROJ-004)
        var bridge = new SimBridge(GameSession.New(legacy, GoldenRun.Seed, Mode.Historical), Text);
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, Names());
        var tlv = legacy.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        var why = MapActions.Check(legacy, bridge.State, MapActions.TransitCamp, tlv.Q, tlv.R);
        Assert.AreEqual(why, MapActions.Check(legacy, bridge.State, MapActions.Housing, tlv.Q, tlv.R), "precondition: one shared reason in 1948");
        Assert.AreEqual(why, ui.Q<Label>("info-reason").text, "the reason, once");
        foreach (var b in new[] { "build-camp", "build-housing" })
        {
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>(b).style.display.value, $"{b}: no button for a shared refusal");
            Assert.IsFalse(ui.Q<Button>(b).enabledSelf, $"{b} stays disabled");
        }
        info.Dispose();
    }

    /// <summary>#204: Tiberias lies in the lake hex and is drawn on the land hex next to it (hexgrid.settlement_hex), so that
    /// hex's 70 m is not the town's: its panel says so in one line, in English and in Hebrew. An ordinary hex (Lydda's,
    /// whose towns lie in it) has no such line. The Hebrew line names the town by its name_he (טבריה).</summary>
    [Test]
    public void ATownDrawnFromTheWaterHexNextDoorSaysSo()
    {
        var settlements = SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName));
        var townHex = InfoPanelPresenter.TownHex(settlements, JsonUtility.FromJson<HexFile>(MapFile("hexes.json")));
        var namesHe = settlements.ToDictionary(t => t.Id, t => t.NameHe);
        var tiberias = C.SettlementHexes.Single(s => s.Id == "tiberias");
        var lod = C.SettlementHexes.Single(s => s.Id == "lod");
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        foreach (var (text, line, words) in new[]
        {
            (Text, "Tiberias is shown here because its own hex is water.", "its own hex is water"),
            (he, "טבריה מוצגת כאן, כי המשושה שלה מכוסה במים.", "מכוסה במים"),
        })
        {
            using var ui = new UiTestKit();
            var info = new InfoPanelPresenter(ui.Root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text), text, Names(),
                townHex: townHex, townNamesHe: namesHe);
            info.Show(tiberias.Q, tiberias.R);
            var lines = ui.Q<Label>("info-settlements").text.Split('\n');
            Assert.AreEqual(line, lines.Last(), $"{text["ui.lang"]}: Tiberias' hex names the water hex");
            info.Show(lod.Q, lod.R);
            StringAssert.DoesNotContain(words, ui.Q<Label>("info-settlements").text, $"{text["ui.lang"]}: Lydda's hex has no note");
            info.Dispose();
        }
    }

    /// <summary>#899: under Hebrew the panel's title and its "Settlements" line name the hex's towns by their name_he (all 25
    /// settlement rows have one); under English both stay the English names.</summary>
    [Test]
    public void InHebrewTheTitleAndTheSettlementsLineAreTheTownsHebrewNames()
    {
        var namesHe = SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName)).ToDictionary(t => t.Id, t => t.NameHe);
        var names = Names();
        var lod = C.SettlementHexes.Single(s => s.Id == "lod");
        var owned = C.SettlementHexes.Where(s => s.Q == lod.Q && s.R == lod.R).Select(s => s.Id).ToList();
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        Assert.AreEqual("לוד", namesHe["lod"]);
        foreach (var (text, name) in new (UiText, System.Func<string, string>)[] { (Text, id => names[id]), (he, id => namesHe[id]!) })
        {
            using var ui = new UiTestKit();
            var info = new InfoPanelPresenter(ui.Root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text), text, names,
                townNamesHe: namesHe);
            info.Show(lod.Q, lod.R);
            Assert.AreEqual(name(owned[0]), ui.Q<Label>("info-title").text, $"{text["ui.lang"]}: the title");
            Assert.AreEqual(text.Format("info.owned", string.Join(", ", owned.Select(name))), ui.Q<Label>("info-settlements").text,
                $"{text["ui.lang"]}: the Settlements line");
            info.Dispose();
        }
    }

    /// <summary>#204 drift guard: the note says "water hex" because a town's own hex (TownHex) differs from its drawn hex
    /// only where hexgrid.settlement_hex moved it off a water hex. Every such town's own hex is on the map and not land, and
    /// there is at least one (Tiberias), so the guard cannot pass on an empty list.</summary>
    [Test]
    public void EveryTownDrawnOffItsOwnHexLiesInWater()
    {
        var townHex = InfoPanelPresenter.TownHex(SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName)),
            JsonUtility.FromJson<HexFile>(MapFile("hexes.json")));
        var moved = C.SettlementHexes.Where(s => townHex(s.Id) != (s.Q, s.R)).ToList();
        CollectionAssert.Contains(moved.Select(s => s.Id).ToList(), "tiberias", "Tiberias is drawn off its own (lake) hex");
        foreach (var s in moved)
        {
            var (q, r) = townHex(s.Id);
            var own = C.HexAt(q, r);
            Assert.IsNotNull(own, $"{s.Id}: its own hex ({q}, {r}) is on the map");
            Assert.IsFalse(own!.Land, $"{s.Id}: drawn on ({s.Q}, {s.R}) but its own hex ({q}, {r}) is land, so the water-hex note would be wrong");
        }
    }

    /// <summary>#401's DPR-2 p95 rise was the filigree drawn as Painter2D curves. It is now one small code-made texture per
    /// variant, made once and shared by every frame and every corner: the same instance on every call, with the scroll
    /// and leaf in it (some pixels covered, most clear), and the side scroll only in the tall variant.</summary>
    [Test]
    public void TheFiligreeIsOneSharedTexturePerVariant()
    {
        var tall = UiFrame.Ornament.Get(true);
        Assert.AreSame(tall, UiFrame.Ornament.Get(true), "made once");
        var low = UiFrame.Ornament.Get(false);
        Assert.AreNotSame(tall, low);
        float Covered(Texture2D t) => t.GetPixels32().Count(p => p.a > 127) / (float)(t.width * t.height);
        Assert.Greater(Covered(low), 0.01f, "the scroll and the leaf are drawn");
        Assert.Less(Covered(tall), 0.2f, "an ornament, not a block");
        Assert.Greater(Covered(tall), Covered(low), "the tall variant adds the scroll down the side");
    }

    /// <summary>The filigree trim (promo/final/03's frames): the date plate, the event card, the info panel and the minimap carry
    /// it; it takes no clicks, so a press on a panel's corner reaches the panel, never the trim.</summary>
    [UnityTest]
    public IEnumerator FiligreeFramesTakeNoClicks()
    {
        var g = WithAnEventOpen();
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        yield return Rendered(1920, 1080, g, (root, info) => info.Show(tlv.Q, tlv.R), root =>
        {
            foreach (var panel in new[] { root.Q(className: "date-box"), root.Q("event-card"), root.Q("info-panel"), root.Q("minimap") })
            {
                var frame = panel.Children().OfType<UiFrame>().Single();
                Assert.IsTrue(frame.Filigree, $"{panel.name}{panel.GetClasses().FirstOrDefault()}: filigree");
                Assert.AreEqual(PickingMode.Ignore, frame.pickingMode);
                var b = panel.worldBound;
                foreach (var corner in new[] { new Vector2(b.xMin + 6, b.yMin + 6), new Vector2(b.xMax - 6, b.yMax - 6) })
                {
                    var hit = root.panel.Pick(corner);
                    Assert.IsFalse(hit is UiFrame, $"{panel.name}: a press at {corner} hits the trim");
                }
            }
        });
    }
    [UnityTest]
    public IEnumerator LaptopPrimaryUnitOrdersStayTogether([Values(false, true)] bool hebrew)
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var data = Statecraft.Game.Map.Units.UnitData.Parse(MapFile(Statecraft.Game.Map.Units.UnitData.DefaultFileName));
        var token = Statecraft.Game.Map.Units.UnitRoster.FromState(data, C, g.State, _ => true, (row, a, q, r) => a).Single(t => t.Unit.Id == "golani");
        Statecraft.Game.Map.Units.PlacedToken? selected = new Statecraft.Game.Map.Units.PlacedToken(token, new Statecraft.Game.Map.Units.AnchorSpot(0, 0, 0, 0, false));
        yield return Rendered(1512, 982, g, (root, info) => info.ShowUnit(), root =>
        {
            var move = root.Q("info-move"); var fortify = root.Q("info-fortify"); var hold = root.Q("info-hold");
            foreach (var b in new[] { move, fortify, hold })
            {
                Assert.IsTrue(Shown(b), b.name + " precondition");
                Assert.Greater(b.worldBound.width, 0f, b.name + " is laid out");
                Assert.IsTrue(Inside(b.worldBound, root.Q("info-panel").worldBound), b.name + " stays inside the panel");
            }
            Assert.That(fortify.worldBound.yMin, Is.EqualTo(move.worldBound.yMin).Within(1f));
            Assert.That(hold.worldBound.yMin, Is.EqualTo(move.worldBound.yMin).Within(1f), "Hold does not get stranded on a line of its own");
        }, text: text, hebrew: hebrew, unit: () => selected);
    }

}
