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

/// <summary>HUD round 2, part 1 (#398): promo/final/03's minimap. The painted relief picture is a bake
/// (tools/minimap_bake.py; tools/tests/test_minimap_bake.py checks it is fresh), and beside it are the promo's three round
/// buttons: the whole map, the settlement names, the region borders. The picture stays mouse-only (accessibility audit
/// B1d); the buttons take Tab after End Turn.</summary>
public class MinimapReliefTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly HexFile Hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static string MapFile(string name) => File.ReadAllText(Path.Combine(Paths.Content, "map", name));
    static MapRegions Regions() => new MapRegions(Subdistricts1948.Parse(MapFile(Subdistricts1948.FileName)), Armistice1949.Parse(MapFile(Armistice1949.FileName)));
    static Dictionary<string, string> Names() => SettlementPlacer.Parse(MapFile(SettlementPlacer.DefaultFileName)).ToDictionary(t => t.Id, t => t.Name);

    static readonly string[] ButtonNames = { "minimap-overview", "minimap-labels", "minimap-borders" };

    /// <summary>InfoPanelLayoutTests' 19 window sizes.</summary>
    static readonly (int w, int h)[] Windows =
    {
        (1920, 1080), (1280, 720), (1366, 768), (1760, 1000), (1920, 1200), (1440, 900), (1680, 1050), (1024, 768), (1280, 1024),
        (1024, 1024), (960, 1080), (800, 1000), (1531, 784), (2560, 1080), (1920, 900), (1600, 757), (1366, 657), (960, 600), (390, 844),
    };

    /// <summary>The real Game.uxml on the real PanelSettings at w x h, out of its loading state, in English or Hebrew, with the
    /// top bar, the info panel on Lydda's hex (its longest region line), the border layer's caption and the minimap.</summary>
    static IEnumerator Rendered(int w, int h, bool hebrew, System.Action<VisualElement, MinimapPresenter> check)
    {
        var text = hebrew ? He : En;
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("MinimapTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        var hud = new HudPresenter(root, bridge, text);
        var info = new InfoPanelPresenter(root, bridge, text, Names(), regions: Regions());
        var mini = new MinimapPresenter(root, Hexes, text);
        var layer = root.Q<Label>("map-layer");
        layer.text = text["map.layer_front_no_data"].Replace("{0}", "Mandate sub-districts, 1947 · Egypt");
        layer.style.display = DisplayStyle.Flex;
        try
        {
            var lod = C.SettlementHexes.Single(s => s.Id == "lod");
            info.Show(lod.Q, lod.R);
            for (int i = 0; i < 30 && !(root.Q("minimap-image").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;   // the picture is fitted after the box's first layout
            check(root, mini);
        }
        finally
        {
            mini.Dispose(); info.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>The picture is the baked relief (Resources), drawn smooth, not the 128-pixel hex strip painted at boot.</summary>
    [Test]
    public void TheMinimapShowsTheBakedReliefPicture()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes);
        var baked = Resources.Load<Texture2D>(MinimapPresenter.Resource);
        Assert.IsNotNull(baked, $"Resources/{MinimapPresenter.Resource} (tools/minimap_bake.py)");
        Assert.AreSame(baked, mini.Texture, "the presenter shows the baked picture");
        Assert.AreSame(baked, ui.Q<VisualElement>("minimap-image").style.backgroundImage.value.texture);
        Assert.AreEqual((320, 856), (baked.width, baked.height), "the bake's size (tools/minimap_bake.py WIDTH, HEIGHT)");
        Assert.AreNotEqual(FilterMode.Point, baked.filterMode, "a painting, not pixels");
        var b = Hexes.bounds;
        double widthKm = (b.lon1 - b.lon0) * HexPicker.KmPerDegLon(b.lat0, b.lat1), lengthKm = (b.lat1 - b.lat0) * HexPicker.KmPerDegLat;
        Assert.AreEqual(widthKm / lengthKm, (double)baked.width / baked.height, 0.005, "the picture has the map's aspect");
    }

    /// <summary>At every window size, in English and in Hebrew: the minimap lies in the window at its bottom right (the
    /// Hebrew UI mirrors rows, not the screen's corners), under the bar; the picture and the three buttons lie inside its frame
    /// and do not overlap; each button is at least 24 px square (WCAG 2.5.8); and the minimap is clear of the info panel and
    /// the border layer's caption.</summary>
    [UnityTest]
    public IEnumerator TheMinimapAndItsButtonsFitEveryWindowInEnglishAndHebrew()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                yield return Rendered(w, h, hebrew, (root, mini) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var window = root.worldBound;
                    var box = root.Q("minimap").worldBound;
                    var picture = root.Q("minimap-image").worldBound;
                    if (!Inside(box, window)) problems.Add($"{at}: the minimap {box} leaves the window {window}");
                    if (window.xMax - box.xMax > 0.05f * window.width) problems.Add($"{at}: the minimap {box} is not at the right edge");
                    if (box.yMin < root.Q("top-bar").worldBound.yMax) problems.Add($"{at}: the minimap {box} rises into the top bar");
                    if (!Inside(picture, box)) problems.Add($"{at}: the picture {picture} leaves the minimap {box}");
                    if (!(picture.width > 0)) problems.Add($"{at}: the picture has no size");
                    var buttons = ButtonNames.Select(n => root.Q<Button>(n)).ToList();
                    foreach (var bt in buttons)
                    {
                        var r = bt.worldBound;
                        if (!Inside(r, box)) problems.Add($"{at}: {bt.name} {r} leaves the minimap {box}");
                        if (r.Overlaps(picture)) problems.Add($"{at}: {bt.name} {r} covers the picture {picture}");
                        float scale = rtScale(root, w);
                        if (r.width * scale < 24f || r.height * scale < 24f) problems.Add($"{at}: {bt.name} is {r.width * scale:0}x{r.height * scale:0} screen px, under 24");
                    }
                    for (int i = 0; i < buttons.Count; i++)
                        for (int j = i + 1; j < buttons.Count; j++)
                            if (buttons[i].worldBound.Overlaps(buttons[j].worldBound)) problems.Add($"{at}: {buttons[i].name} overlaps {buttons[j].name}");
                    var panel = root.Q("info-panel").worldBound;
                    if (panel.Overlaps(box)) problems.Add($"{at}: the info panel {panel} overlaps the minimap {box}");
                    var layer = root.Q("map-layer").worldBound;
                    if (layer.Overlaps(box)) problems.Add($"{at}: the border caption {layer} overlaps the minimap {box}");
                    if (!Inside(layer, window)) problems.Add($"{at}: the border caption {layer} leaves the window");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    // Panel units to screen pixels: the panel scales with the screen (PanelSettings: scale with screen size).
    static float rtScale(VisualElement root, int w) => w / root.worldBound.width;

    /// <summary>The audit's rules (docs/accessibility-audit-2026-10-14.md): the picture stays mouse-only (B1d: pan covers it)
    /// and takes no focus; the three buttons are buttons, so Tab reaches them, after End Turn and before anything else, and
    /// each has its name in its tooltip (for a later accessibility layer, as the meters' are).</summary>
    [UnityTest]
    public IEnumerator ThePictureTakesNoFocusAndTheButtonsComeAfterEndTurn()
    {
        foreach (var hebrew in new[] { false, true })
            yield return Rendered(1920, 1080, hebrew, (root, mini) =>
            {
                string at = hebrew ? "he" : "en";
                var text = hebrew ? He : En;
                Assert.IsFalse(root.Q("minimap-image").focusable, at + ": the picture takes no focus (B1d)");
                Assert.IsFalse(root.Q("minimap").focusable, at + ": the frame takes no focus");
                var keys = new[] { "minimap.overview", "minimap.labels", "minimap.borders" };
                for (int i = 0; i < ButtonNames.Length; i++)
                {
                    var bt = root.Q<Button>(ButtonNames[i]);
                    Assert.IsTrue(bt.focusable && bt.canGrabFocus, $"{at}: {bt.name} takes focus");
                    Assert.GreaterOrEqual(bt.tabIndex, 0, $"{at}: {bt.name} is in the Tab order");
                    Assert.AreEqual(text[keys[i]], bt.tooltip, $"{at}: {bt.name} has its name");
                }
                // Tree order is Tab order: End Turn, then the three buttons, then nothing else of the minimap.
                var order = root.Query<VisualElement>().Where(e => e.focusable && e.canGrabFocus && e.tabIndex >= 0).ToList().Select(e => e.name).ToList();
                int end = order.IndexOf("end-turn");
                Assert.GreaterOrEqual(end, 0, at + ": End Turn takes focus");
                CollectionAssert.AreEqual(ButtonNames, order.Skip(end + 1).Take(3).ToArray(), $"{at}: the Tab order after End Turn: {string.Join(", ", order)}");
            });
    }

    /// <summary>The buttons do only what the game already does: the first zooms all the way out (the camera's own limit),
    /// the second switches the settlement names, the third the region borders (the map's "labels" and "borders" switches).
    /// A switched-off layer dims its button, and pressing it again switches the layer back on.</summary>
    [Test]
    public void TheButtonsZoomOutAndSwitchTheNamesAndTheBorders()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes, En);
        var labels = new List<bool>(); var borders = new List<bool>();
        var removeLabels = VisualToggles.Register("labels", on => labels.Add(on));
        var removeBorders = VisualToggles.Register(MapBorders.ToggleName, on => borders.Add(on));
        try
        {
            int overviews = 0, bordersChanged = 0;
            mini.Overview += () => overviews++;
            mini.BordersChanged += () => bordersChanged++;
            ui.Press("minimap-overview");
            Assert.AreEqual(1, overviews, "the whole-map button asks for the overview");
            CollectionAssert.IsEmpty(labels); CollectionAssert.IsEmpty(borders);

            ui.Press("minimap-labels");
            CollectionAssert.AreEqual(new[] { false }, labels, "the names go off");
            Assert.IsFalse(mini.LabelsShown);
            Assert.IsTrue(ui.Q<Button>("minimap-labels").ClassListContains(MinimapPresenter.OffClass), "its button dims");
            ui.Press("minimap-labels");
            CollectionAssert.AreEqual(new[] { false, true }, labels, "and back on");
            Assert.IsFalse(ui.Q<Button>("minimap-labels").ClassListContains(MinimapPresenter.OffClass));

            ui.Press("minimap-borders");
            CollectionAssert.AreEqual(new[] { false }, borders, "the borders go off");
            Assert.IsFalse(mini.BordersShown);
            Assert.AreEqual(1, bordersChanged, "the border caption hears of it");
            Assert.IsTrue(ui.Q<Button>("minimap-borders").ClassListContains(MinimapPresenter.OffClass));
            ui.Press("minimap-borders");
            CollectionAssert.AreEqual(new[] { false, true }, borders);
            Assert.IsTrue(mini.BordersShown);
            Assert.AreEqual(1, overviews, "no other button asks for the overview");
        }
        finally { removeLabels(); removeBorders(); }
    }

    /// <summary>With no map built (no switch registered) the buttons still keep their own state and throw nothing.</summary>
    [Test]
    public void TheSwitchButtonsWorkWithNoMap()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes, En);
        Assert.IsFalse(VisualToggles.Names.Contains("labels") && VisualToggles.Names.Contains(MapBorders.ToggleName), "precondition: no map");
        Assert.DoesNotThrow(() => ui.Press("minimap-labels"));
        Assert.DoesNotThrow(() => ui.Press("minimap-borders"));
        Assert.IsFalse(mini.LabelsShown); Assert.IsFalse(mini.BordersShown);
    }

    /// <summary>A disposed presenter hears no button.</summary>
    [Test]
    public void ADisposedMinimapHearsNoButton()
    {
        using var ui = new UiTestKit();
        var mini = new MinimapPresenter(ui.Root, Hexes, En);
        int overviews = 0;
        mini.Overview += () => overviews++;
        mini.Dispose();
        ui.Press("minimap-overview");
        ui.Press("minimap-labels");
        Assert.AreEqual(0, overviews);
        Assert.IsTrue(mini.LabelsShown, "the names' state did not change");
    }
}
