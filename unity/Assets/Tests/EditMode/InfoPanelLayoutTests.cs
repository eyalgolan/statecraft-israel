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

/// <summary>HUD round 1 (#398): the info panel in the promo's shape (promo/final/03): a one-line hint before a hex is chosen,
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

    /// <summary>The real Game.uxml on the real PanelSettings at w x h, out of its loading state, with the top bar, an open event
    /// card and the info panel (with the region lines) on the session given.</summary>
    static IEnumerator Rendered(int w, int h, GameSession g, System.Action<VisualElement, InfoPanelPresenter> act, System.Action<VisualElement> check)
    {
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
        var bridge = new SimBridge(g, Text);
        var hud = new HudPresenter(root, bridge, Text);
        var card = new EventCardPresenter(root, bridge, Text, null);
        var info = new InfoPanelPresenter(root, bridge, Text, Names(), regions: Regions());
        try
        {
            act(root, info);
            for (int i = 0; i < 20 && float.IsNaN(root.Q("info-panel").worldBound.width); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;   // text measured after the first layout settles
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
        return wrapped.y <= box.height + 1f;
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

    /// <summary>Before a hex is chosen the panel is a one-line hint (the promo shows no panel until a tile is chosen): no
    /// terrain sign, no rows, and no taller than the hint's line and the panel's padding.</summary>
    [UnityTest]
    public IEnumerator TheEmptyInfoPanelIsAOneLineHint()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (390, 844) })
            yield return Rendered(w, h, g, (root, info) => { }, root =>
            {
                var panel = root.Q("info-panel");
                var hint = root.Q<Label>("info-title");
                Assert.AreEqual(Text["info.hint"], hint.text);
                Assert.IsTrue(TextFits(hint), $"{w}x{h}: the hint fits");
                Assert.AreEqual(DisplayStyle.None, root.Q("info-thumb").resolvedStyle.display, $"{w}x{h}: no terrain sign");
                foreach (var row in panel.Query(className: InfoPanelPresenter.RowClass).ToList())
                    Assert.IsFalse(Shown(row), $"{w}x{h}: no fact row before a hex is chosen");
                var s = panel.resolvedStyle;
                var oneLine = hint.worldBound.height + hint.resolvedStyle.marginTop + hint.resolvedStyle.marginBottom
                    + s.paddingTop + s.paddingBottom + s.borderTopWidth + s.borderBottomWidth;
                Assert.LessOrEqual(panel.worldBound.height, oneLine + 4f, $"{w}x{h}: the empty panel {panel.worldBound} is one line ({hint.worldBound})");
            });
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
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, Names());
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        var why = MapActions.Check(C, bridge.State, MapActions.TransitCamp, tlv.Q, tlv.R);
        Assert.AreEqual(why, MapActions.Check(C, bridge.State, MapActions.Housing, tlv.Q, tlv.R), "precondition: one shared reason in 1948");
        Assert.AreEqual(why, ui.Q<Label>("info-reason").text, "the reason, once");
        foreach (var b in new[] { "build-camp", "build-housing" })
        {
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>(b).style.display.value, $"{b}: no button for a shared refusal");
            Assert.IsFalse(ui.Q<Button>(b).enabledSelf, $"{b} stays disabled");
        }
        info.Dispose();
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
}
