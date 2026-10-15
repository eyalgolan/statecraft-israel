#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

/// <summary>Plan Task 9: settlement labels as UI Toolkit pills in EB Garamond, decluttered and on their markers.</summary>
public class MapLabelTests
{
    MapScene map = null!;
    Camera cam = null!;
    CameraRig rig = null!;

    [SetUp]
    public void Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        cam = Camera.main; rig = cam.GetComponent<CameraRig>();
        Pose(34.85, 32.0, 90000f);
    }

    [TearDown]
    public void Close() { cam.targetTexture = null; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    void Pose(double lon, double lat, float distance)
    {
        rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
    }

    static bool Shown(VisualElement slot) => slot.style.visibility.value == Visibility.Visible;

    /// <summary>A UIDocument with Game.uxml on a panel of its own size (scale-with-screen-size against 1920x1080, so a
    /// 2880x1620 target is a scale of 1.5), the camera drawing to a texture of the same size, "loading" removed.</summary>
    sealed class HdPanel : System.IDisposable
    {
        public readonly int Width, Height; public readonly VisualElement Root;
        readonly RenderTexture rt, camRt; readonly PanelSettings ps; readonly GameObject go; readonly Camera cam;

        public HdPanel(Camera cam, int width = 1920, int height = 1080, bool loading = false)
        {
            this.cam = cam; Width = width; Height = height;
            rt = new RenderTexture(width, height, 0); camRt = new RenderTexture(width, height, 24);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
            go = new GameObject("LabelPanel"); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            cam.targetTexture = camRt;
            Root = doc.rootVisualElement;
            if (!loading) Root.Q("root")?.RemoveFromClassList("loading");
        }

        /// <summary>A world point in panel coordinates, independently of the presenter: the camera's pixels, y flipped, times
        /// the panel's scale.</summary>
        public Vector2 ToPanel(Vector3 world, out bool inFront)
        {
            var p = cam.WorldToScreenPoint(world); inFront = p.z > 0;
            float scale = Root.worldBound.width / Width;
            return new Vector2(p.x * scale, (Height - p.y) * scale);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            cam.targetTexture = null; camRt.Release(); Object.DestroyImmediate(camRt);
        }
    }

    static Vector3 MarkerTopOf(string id)
    {
        var box = GameObject.Find("Settlements").transform.Find(id);
        return box.position + Vector3.up * box.lossyScale.y / 2;
    }

    /// <summary>A settlement's kit (its drawn buildings) as a panel rectangle, independently of the presenter: the hull of
    /// every vertex of its mesh through the camera's pixels.</summary>
    static Rect KitRectOf(HdPanel hd, string id)
    {
        var kit = GameObject.Find(Statecraft.Game.Map.Kits.KitLayer.RootName).transform.Find(id);
        Assert.IsNotNull(kit, id + " has a kit");
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (var v in kit.GetComponent<MeshFilter>().sharedMesh.vertices)
        {
            var p = hd.ToPanel(kit.localToWorldMatrix.MultiplyPoint3x4(v), out _);
            x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    static float Cover(Rect pill, Rect kit)
    {
        float w = Mathf.Min(pill.xMax, kit.xMax) - Mathf.Max(pill.xMin, kit.xMin), h = Mathf.Min(pill.yMax, kit.yMax) - Mathf.Max(pill.yMin, kit.yMin);
        return w <= 0 || h <= 0 ? 0f : w * h / (kit.width * kit.height);
    }

    /// <summary>Issue #214: a kit's columns hold every vertex, stand on the kit's lowest point, and keep a tall landmark's
    /// height to its own cell (a low house beside a tall tower gets a low column).</summary>
    [Test]
    public void KitColumnsHoldTheKitAndKeepATowerToItsCell()
    {
        var pts = new[] { new Vector3(0, 0, 0), new Vector3(10, 8, 10), new Vector3(90, 0, 90), new Vector3(100, 60, 100) };   // a house, a tower
        var cols = Statecraft.Game.Map.Kits.KitMesh.Columns(pts);
        Assert.AreEqual(2, cols.Length, "two occupied cells");
        foreach (var p in pts) Assert.IsTrue(cols.Any(c => c.min.x <= p.x && p.x <= c.max.x && c.min.y <= p.y && p.y <= c.max.y && c.min.z <= p.z && p.z <= c.max.z), p.ToString());
        foreach (var c in cols) Assert.AreEqual(0f, c.min.y, "from the ground");
        Assert.AreEqual(8f, cols.Min(c => c.max.y), "the house's column is as tall as the house");
        // The built map's columns come from the kit build, not from the uploaded mesh: the player has no CPU copy to read
        // (a WebGL boot read mesh.vertices and failed).
        var tlv = GameObject.Find(Statecraft.Game.Map.Kits.KitLayer.RootName).transform.Find("tel_aviv").GetComponent<MeshFilter>().sharedMesh;
        Assert.IsFalse(tlv.isReadable, "the kit meshes keep no CPU copy");
        foreach (var s in map.Settlements) Assert.IsTrue(map.TryKitColumns(s.Id, out var c) && c.Length > 0, s.Id + " has kit columns");
    }

    /// <summary>Review C-m3: with the "kits" toggle off the marker boxes are drawn, and a pill sits just above its box, not
    /// above the hidden kit; turning the kits back on moves it back above the kit.</summary>
    [UnityTest]
    public IEnumerator WithTheKitsOffAPillSitsAboveItsMarker()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            var deg = map.Settlements.Single(s => s.Id == "degania_alef");
            Pose(deg.Lon, deg.Lat, 10000f);
            var pill = labels.Labels.Single(l => l.name == "label-degania_alef");
            Assert.IsTrue(map.KitsShown);
            yield return Settle(labels);
            var withKit = pill.worldBound;
            VisualToggles.Set("kits", false);
            Assert.IsFalse(map.KitsShown);
            labels.Refresh(); yield return null;
            var box = GameObject.Find("Settlements").transform.Find("degania_alef");
            float top = float.MaxValue;
            for (int c = 0; c < 8; c++)
            {
                var corner = box.position + Vector3.Scale(box.lossyScale, new Vector3((c & 1) - 0.5f, ((c >> 1) & 1) - 0.5f, ((c >> 2) & 1) - 0.5f));
                top = Mathf.Min(top, hd.ToPanel(corner, out _).y);
            }
            var withMarker = pill.worldBound;
            TestContext.WriteLine($"Degania pill with the kit {withKit}, with the marker {withMarker}, marker top {top:F0}");
            Assert.AreEqual(top - MapLabelsPresenter.LiftPx, withMarker.yMax, 2f, "LiftPx above the marker box");
            Assert.Greater(withMarker.yMax, withKit.yMax + 2f, "lower than above the (hidden) kit");
            VisualToggles.Set("kits", true);
            labels.Refresh(); yield return null;
            Assert.AreEqual(withKit.yMax, pill.worldBound.yMax, 0.5f, "back above the kit");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Issue #214: the spots a pill may take against its kit's screen rectangle, LiftPx clear of it.</summary>
    [Test]
    public void SpotsSitClearOfTheKit()
    {
        var kit = new Rect(100, 200, 60, 40); var size = new Vector2(30, 10); var point = new Vector2(130, 215);
        float lift = MapLabelsPresenter.LiftPx;
        Assert.AreEqual(new Rect(115, 200 - lift - 10, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.AboveCentre, kit, point, size));
        Assert.AreEqual(new Rect(130, 200 - lift - 10, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.AboveLeft, kit, point, size));
        Assert.AreEqual(new Rect(100, 200 - lift - 10, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.AboveRight, kit, point, size));
        Assert.AreEqual(new Rect(160 + lift, 215, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.BesideRight, kit, point, size));
        Assert.AreEqual(new Rect(100 - lift - 30, 215, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.BesideLeft, kit, point, size));
        Assert.AreEqual(new Rect(115, 240 + lift, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.Below, kit, point, size));
        foreach (var spot in new[] { MapLabelsPresenter.Spot.AboveCentre, MapLabelsPresenter.Spot.AboveLeft, MapLabelsPresenter.Spot.AboveRight,
                     MapLabelsPresenter.Spot.BesideRight, MapLabelsPresenter.Spot.BesideLeft, MapLabelsPresenter.Spot.Below })
            Assert.AreEqual(0f, MapLabelsPresenter.Cover(MapLabelsPresenter.SpotRect(spot, kit, point, size), kit), spot.ToString());
        var over = MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.Over, kit, point, size);
        Assert.AreEqual(new Rect(115, 215 - lift - 10, 30, 10), over);
        Assert.AreEqual(30f * 9f / (60f * 40f), MapLabelsPresenter.Cover(over, kit), 1e-4f, "over the kit: its bottom 9 rows, 270 of its 2400 px");
        Assert.AreEqual(0f, MapLabelsPresenter.Cover(over, new Rect(0, 0, 0, 0)), "an empty kit is never covered");
        Assert.AreEqual(new Rect(115, 215 + lift, 30, 10), MapLabelsPresenter.SpotRect(MapLabelsPresenter.Spot.Under, kit, point, size), "under the point (#599)");
    }
    [Test]
    public void EverySettlementHasOneGaramondPillThatIgnoresThePointer()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var names = map.Settlements.Select(s => s.Name).ToArray();
        CollectionAssert.AreEqual(names, labels.Labels.Select(l => l.text).ToArray());
        var container = ui.Q<VisualElement>(MapLabelsPresenter.ContainerName);
        // The game settlements' pills, the 1948 localities' pills and the one tooltip.
        Assert.AreEqual(names.Length + map.Localities.Count + 1, container.childCount);
        CollectionAssert.AreEqual(map.Localities.Select(p => p.Settlement.Name).ToArray(), labels.LocalityLabels.Select(l => l.text).ToArray());
        foreach (var l in labels.LocalityLabels) { Assert.IsTrue(l.ClassListContains(MapLabelsPresenter.LocalityClass), l.text); Assert.AreEqual(PickingMode.Ignore, l.pickingMode, l.text); }
        Assert.AreEqual(PickingMode.Ignore, labels.Tooltip.pickingMode);
        foreach (var slot in labels.Slots) Assert.AreEqual(PickingMode.Ignore, slot.pickingMode);
        foreach (var l in labels.Labels)
        {
            Assert.IsTrue(l.ClassListContains(MapLabelsPresenter.LabelClass), l.text);
            Assert.AreEqual(PickingMode.Ignore, l.pickingMode, l.text + " must not catch map clicks");
        }
        Assert.AreEqual(PickingMode.Ignore, container.pickingMode);
        VisualToggles.Set("labels", false);
        Assert.AreEqual(DisplayStyle.None, container.style.display.value, "the labels toggle hides the layer");
        VisualToggles.Set("labels", true);
        Assert.AreEqual(StyleKeyword.Null, container.style.display.keyword, "on clears the inline display, so the loading rule still wins (review M1)");
    }

    [Test]
    public void LabelsOffScreenAreHiddenAndComeBack()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
        Pose(35.5, 33.2, CameraController.MinDistance);   // the far north, close up
        labels.Refresh(force: true);
        Assert.IsFalse(Shown(tlv), "Tel Aviv is off screen");
        Pose(34.85, 32.0, 90000f);
        labels.Refresh(force: true);
        Assert.IsTrue(Shown(tlv));
    }

    [Test]
    public void TheDenseClusterKeepsItsAnchorsAndMinorPlacesGiveWayAtTheOverview()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        string AnchorOf(string id) => labels.Labels.Single(l => l.name == "label-" + id).GetClasses().Single(c => c.StartsWith("anchor-"));
        Assert.AreEqual("anchor-lower-right", AnchorOf("jaffa")); Assert.AreEqual("anchor-lower-right", AnchorOf("rishon_lezion"));
        Assert.AreEqual("anchor-lower-left", AnchorOf("lod")); Assert.AreEqual("anchor-lower-left", AnchorOf("petah_tikva"));
        Assert.AreEqual("anchor-lower-centre", AnchorOf("tel_aviv"));
        Assert.Less(MapLabelsPresenter.Priority("city"), MapLabelsPresenter.Priority("town"));
        Assert.AreEqual(MapLabelsPresenter.Priority("town"), MapLabelsPresenter.Priority("arab_town"));
        Assert.Less(MapLabelsPresenter.Priority("arab_town"), MapLabelsPresenter.Priority("kibbutz"));
        foreach (var minor in new[] { "village", "kibbutz", "moshav" })
        {
            Assert.IsFalse(MapLabelsPresenter.HiddenByZoom(minor, 90000f), minor + " at 90 km");
            Assert.IsTrue(MapLabelsPresenter.HiddenByZoom(minor, CameraController.MaxDistance), minor + " at the overview");
        }
        Assert.IsFalse(MapLabelsPresenter.HiddenByZoom("town", CameraController.MaxDistance));
        Assert.AreEqual(new Rect(90, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-centre"));
        Assert.AreEqual(new Rect(100, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-left"));
        Assert.AreEqual(new Rect(80, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-right"));
    }

    [Test]
    public void RebuildingKeepsOneLabelPerSettlement()
    {
        using var ui = new UiTestKit();
        var first = new MapLabelsPresenter(ui.Root, map, cam);
        first.Dispose();
        Assert.AreEqual(0, ui.Q<VisualElement>(MapLabelsPresenter.ContainerName).childCount, "dispose clears");
        map.Build();
        using var second = new MapLabelsPresenter(ui.Root, map, cam);
        using var third = new MapLabelsPresenter(ui.Root, map, cam);
        Assert.AreEqual(map.Settlements.Count + map.Localities.Count + 1, ui.Q<VisualElement>(MapLabelsPresenter.ContainerName).childCount);
    }

    /// <summary>Review M2: an older presenter disposed after a newer one was made removes only its own pills.</summary>
    [Test]
    public void DisposingAnOlderPresenterLeavesTheNewerOnesPills()
    {
        using var ui = new UiTestKit();
        var older = new MapLabelsPresenter(ui.Root, map, cam);
        using var newer = new MapLabelsPresenter(ui.Root, map, cam);
        older.Dispose();
        var container = ui.Q<VisualElement>(MapLabelsPresenter.ContainerName);
        Assert.AreEqual(map.Settlements.Count + map.Localities.Count + 1, container.childCount);
        foreach (var slot in newer.Slots.Concat(newer.LocalitySlots)) Assert.AreSame(container, slot.parent, slot.name);
    }

    /// <summary>Behavioural, not text (review N4/N6): with the real theme, a laid-out pill resolves to EB Garamond SemiBold on
    /// the navy fill with the gold edge, and the whole layer is display:none while the root carries "loading" (neither the
    /// slots' inline visibility nor the labels switch turned on can override that, review M1), and back once it is
    /// removed. A 1920x1080 panel (scale 1), as in the game: UiTestKit's panel follows the batch-mode screen (about 0.39
    /// points per pixel there), and UI Toolkit rounds a 1 px border up to one physical pixel, 2.57 points.</summary>
    [UnityTest]
    public IEnumerator ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading()
    {
        var hd = new HdPanel(cam, loading: true);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            var root = hd.Root.Q<VisualElement>("root");
            var container = hd.Root.Q<VisualElement>(MapLabelsPresenter.ContainerName);
            Assert.IsTrue(root.ClassListContains("loading"), "Game.uxml starts loading");
            VisualToggles.Set("labels", true);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.None, container.resolvedStyle.display, "hidden while the game loads, the switch on or not");
            root.RemoveFromClassList("loading");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, container.resolvedStyle.display, "shown once ready");
            var pill = labels.Labels.Single(l => l.name == "label-tel_aviv");
            var font = pill.resolvedStyle.unityFontDefinition;
            var fontName = font.font != null ? font.font.name : font.fontAsset != null ? font.fontAsset.name : "";
            StringAssert.Contains("EBGaramond-SemiBold", fontName);
            var bg = pill.resolvedStyle.backgroundColor;
            Assert.AreEqual(14f / 255f, bg.r, 0.01f); Assert.AreEqual(22f / 255f, bg.g, 0.01f); Assert.AreEqual(38f / 255f, bg.b, 0.01f);
            Assert.AreEqual(0.82f, bg.a, 0.01f);
            var gold = pill.resolvedStyle.borderTopColor;
            Assert.AreEqual(196f / 255f, gold.r, 0.01f); Assert.AreEqual(160f / 255f, gold.g, 0.01f); Assert.AreEqual(92f / 255f, gold.b, 0.01f);
            Assert.AreEqual(1f, pill.resolvedStyle.borderTopWidth, 0.01f);
            Assert.GreaterOrEqual(pill.resolvedStyle.fontSize, labels.Labels.Single(l => l.name == "label-petah_tikva").resolvedStyle.fontSize + 2f,
                "a city's name is visibly larger than a town's (review M5)");
            Assert.IsNull(GameObject.Find("tel_aviv_label"), "no world-space TextMesh labels remain");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    static IEnumerator Settle(MapLabelsPresenter labels)
    {
        for (int i = 0; i < 20 && labels.Labels.Concat(labels.LocalityLabels).Any(l => float.IsNaN(l.resolvedStyle.width) || l.resolvedStyle.width <= 0); i++) yield return null;
        labels.Refresh(force: true); yield return null;   // the first refresh with sizes declutters
        labels.Refresh(force: true); yield return null;
    }

    /// <summary>Independent of the presenter's own projection call: the Tel Aviv pill sits centred over its kit and just
    /// above it (issue #214): its bottom at least LiftPx and at most 30 px above the top of the kit's drawn buildings (every
    /// vertex projected with Camera.WorldToScreenPoint and the panel's scale; the presenter's columns are a little looser). Also on a 2880x1620 target (panel scale 1.5, Retina-like) with the game's top-bar
    /// inset (review I5).</summary>
    [UnityTest]
    public IEnumerator APillSitsJustAboveItsKit() => PillOnMarker(1920, 1080, 0f);

    [UnityTest]
    public IEnumerator APillSitsJustAboveItsKitAtScaleOneAndAHalfWithTheTopInset() => PillOnMarker(2880, 1620, 0.088f);

    IEnumerator PillOnMarker(int width, int height, float topInset)
    {
        var hd = new HdPanel(cam, width, height);
        rig.TopInset = topInset; rig.ApplyProjection();
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            yield return Settle(labels);
            var kit = KitRectOf(hd, "tel_aviv");
            var wb = labels.Labels.Single(l => l.name == "label-tel_aviv").worldBound;
            TestContext.WriteLine($"{width}x{height} inset {topInset}: Tel Aviv pill {wb}, kit {kit}, panel {hd.Root.worldBound.size}");
            Assert.AreEqual(1920f, hd.Root.worldBound.width, 1f, $"the panel is 1920 points wide: a scale of {width / 1920f}");
            Assert.AreEqual(kit.center.x, wb.center.x, 0.05f * kit.width + 2f, "centred on the kit");
            Assert.LessOrEqual(wb.yMax, kit.yMin - MapLabelsPresenter.LiftPx + 1f, "clear of the kit, LiftPx above it");
            Assert.GreaterOrEqual(wb.yMax, kit.yMin - 30f, "just above the kit, not floating");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Owner rulings 2026-10-11 and 2026-10-12: the 1948 localities are named by their 1948 names only at close zoom,
    /// with today's name in a tooltip only where a living place carries on. At 30 km over Beisan its pill reads "Beisan"
    /// and, with the pointer over it (the hit test Hover runs, in the layer's coordinates), the tooltip "Today: Beit She'an"
    /// sits under it; off every pill it hides; no two shown pills overlap; at the gameplay zoom (60 km) no locality pill is
    /// shown. A place that did not carry on (Isdud), or whose name is today's (Nablus), has no tooltip; Nesher's says it was
    /// a mixed locality.</summary>
    [UnityTest]
    public IEnumerator LocalitiesAreNamedOnlyCloseUpWithTodaysNameOnHover()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            Assert.Greater(map.Localities.Count, 250);
            Pose(35.50, 32.49, 30000f);
            yield return Settle(labels);
            var beisan = labels.LocalitySlots.Single(x => x.name == "slot-beit_shean");
            Assert.AreEqual("Beisan", beisan.Q<Label>().text);
            Assert.IsTrue(Shown(beisan), "Beisan is named at 30 km");
            var shown = labels.Slots.Concat(labels.LocalitySlots).Where(Shown).Select(x => (id: x.name, rect: x.Q<Label>().worldBound)).ToList();
            TestContext.WriteLine($"30 km over Beisan: {shown.Count} pills shown: {string.Join(" ", shown.Select(x => x.id))}");
            for (int i = 0; i < shown.Count; i++)
                for (int j = i + 1; j < shown.Count; j++)
                    Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{shown[i].id} overlaps {shown[j].id}");
            var container = hd.Root.Q<VisualElement>(MapLabelsPresenter.ContainerName).worldBound.position;
            labels.HoverAt(beisan.Q<Label>().worldBound.center - container); yield return null;
            Assert.AreEqual(Visibility.Visible, labels.Tooltip.style.visibility.value, "the pointer over Beisan's pill shows its tooltip");
            Assert.AreEqual("Today: Beit She'an", labels.Tooltip.text);
            var pill = beisan.Q<Label>().worldBound; var tip = labels.Tooltip.worldBound;
            Assert.AreEqual(pill.center.x, tip.center.x, 2f, "the tooltip is centred under its pill");
            Assert.Greater(tip.yMin, pill.yMax - 1f, "and below it");
            labels.HoverAt(new Vector2(-50f, -50f));
            Assert.AreEqual(Visibility.Hidden, labels.Tooltip.style.visibility.value, "off every pill, no tooltip");
            var by = map.Localities.ToDictionary(p => p.Settlement.Id);
            Assert.IsNull(MapLabelsPresenter.DefaultTip(by["isdud"]), "no today name for a place that did not carry on");
            Assert.IsNull(MapLabelsPresenter.DefaultTip(by["nablus"]), "Nablus is Nablus: nothing to say");
            Assert.AreEqual("Mixed locality", MapLabelsPresenter.DefaultTip(by["nesher"]));
            labels.HoverLocality("isdud");
            Assert.AreEqual(Visibility.Hidden, labels.Tooltip.style.visibility.value, "Isdud's pill never shows a tooltip");

            Pose(PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance);
            yield return Settle(labels);
            Assert.IsFalse(labels.LocalitySlots.Any(Shown), "no locality is named at the gameplay zoom");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Issue #245: on a touchscreen a tap on a locality pill toggles its tooltip (a second tap hides it), and a tap
    /// elsewhere, or on a pill with nothing to say, hides it. The touch's own Hover(null) each frame leaves a tapped
    /// tooltip up. A mouse hover still shows and hides it as before, and a mouse in use takes a tapped tooltip away.</summary>
    [UnityTest]
    public IEnumerator ATapTogglesTheTooltipAndATapElsewhereHidesIt()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            Pose(35.50, 32.49, 30000f);
            yield return Settle(labels);
            var container = hd.Root.Q<VisualElement>(MapLabelsPresenter.ContainerName).worldBound.position;
            Vector2 Centre(string id) => labels.LocalitySlots.Single(x => x.name == "slot-" + id).Q<Label>().worldBound.center - container;
            bool Up() => labels.Tooltip.style.visibility.value == Visibility.Visible;
            Assert.IsTrue(Shown(labels.LocalitySlots.Single(x => x.name == "slot-beit_shean")));

            labels.TapAt(Centre("beit_shean"));
            Assert.IsTrue(Up(), "a tap on Beisan's pill shows its tooltip");
            Assert.AreEqual("Today: Beit She'an", labels.Tooltip.text);
            labels.Hover(null);   // the frames after the tap: a touchscreen has no hover position
            Assert.IsTrue(Up(), "and it stays up across frames");
            labels.TapAt(Centre("beit_shean"));
            Assert.IsFalse(Up(), "a second tap on the pill hides it");
            labels.TapAt(Centre("beit_shean"));
            labels.TapAt(new Vector2(-50f, -50f));
            Assert.IsFalse(Up(), "a tap elsewhere hides it");

            labels.HoverAt(Centre("beit_shean"));
            Assert.IsTrue(Up(), "a mouse hover still shows it");
            Assert.AreEqual("Today: Beit She'an", labels.Tooltip.text);
            labels.HoverAt(new Vector2(-50f, -50f));
            Assert.IsFalse(Up(), "and off the pill hides it");

            labels.TapAt(Centre("beit_shean"));
            Assert.IsTrue(Up());
            labels.Hover(new Vector2(-500f, -500f));   // a mouse position well off every pill
            Assert.IsFalse(Up(), "a mouse in use takes a tapped tooltip away");

            labels.TapAt(Centre("beit_shean"));
            Pose(PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance);
            yield return Settle(labels);
            labels.Hover(null);
            Assert.IsFalse(Up(), "zooming out hides the pill and the tooltip with it");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Issue #245: the device type decides. A touchscreen has no hover position (the Input System keeps its last
    /// touch after the finger lifts); a mouse and a pen do.</summary>
    [Test]
    public void OnlyANonTouchPointerHovers()
    {
        Assert.IsNull(MapLabelsPresenter.HoverPosition(null));
        var touch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
        var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
        var pen = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Pen>();
        try
        {
            Assert.IsNull(MapLabelsPresenter.HoverPosition(touch), "a touchscreen does not hover");
            Assert.IsNotNull(MapLabelsPresenter.HoverPosition(mouse), "a mouse hovers");
            Assert.IsNotNull(MapLabelsPresenter.HoverPosition(pen), "a pen hovers");
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(touch); UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); UnityEngine.InputSystem.InputSystem.RemoveDevice(pen);
        }
    }

    static float Gap(Rect r, Vector2 p) => new Vector2(Mathf.Max(0, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;
    static float RectGap(Rect a, Rect b) => new Vector2(Mathf.Max(0, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax)), Mathf.Max(0, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax))).magnitude;

    /// <summary>Issue #214 (integration B16): at the gameplay03 promo pose (with the game's top-bar inset), and close over
    /// Degania Alef (10 km) and Zikhron Ya'akov (25 km), no shown pill covers more than MaxKitCover of its own kit's screen
    /// rectangle or of any other visible kit's, and no two shown pills overlap; both small places are labelled close up.</summary>
    [UnityTest]
    public IEnumerator NoPillHidesAKit()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        var inset = new TopBarInset(hd.Root, rig);
        try
        {
            var degania = map.Settlements.Single(s => s.Id == "degania_alef");
            var zikhron = map.Settlements.Single(s => s.Id == "zikhron_yaakov");
            foreach (var (name, lon, lat, distance, yaw, must) in new[]
            {
                ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance, PromoPose.Gameplay03.Yaw, ""),
                ("degania 10 km", degania.Lon, degania.Lat, 10000f, 0f, "degania_alef"),
                ("zikhron 25 km", zikhron.Lon, zikhron.Lat, 25000f, 0f, "zikhron_yaakov"),
            })
            {
                rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = yaw; rig.Apply();
                yield return null;
                inset.Update();
                yield return Settle(labels);
                var tops = VisibleMarkerTops(hd, map);
                var shown = labels.Slots.Where(Shown).Select(s => (id: s.name.Substring("slot-".Length), rect: s.Q<Label>().worldBound)).ToList();
                TestContext.WriteLine($"{name}: {shown.Count} shown: {string.Join(" ", shown.Select(x => $"{x.id}{x.rect} kit {KitRectOf(hd, x.id)}"))}");
                Assert.Greater(shown.Count, 0, $"{name}: pills are shown");
                if (must != "") Assert.IsTrue(shown.Any(x => x.id == must), $"{name}: {must} is labelled");
                foreach (var (id, rect) in shown)
                {
                    float own = Cover(rect, KitRectOf(hd, id));
                    Assert.LessOrEqual(own, MapLabelsPresenter.MaxKitCover + 0.01f, $"{name}: {id}'s pill {rect} covers {own:P0} of its kit {KitRectOf(hd, id)}");
                    foreach (var other in tops.Keys.Where(o => o != id))
                    {
                        float c = Cover(rect, KitRectOf(hd, other));
                        Assert.LessOrEqual(c, MapLabelsPresenter.MaxKitCover + 0.01f, $"{name}: {id}'s pill {rect} covers {c:P0} of {other}'s kit");
                    }
                }
                for (int i = 0; i < shown.Count; i++)
                    for (int j = i + 1; j < shown.Count; j++)
                        Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{name}: {shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
            }
        }
        finally { inset.Dispose(); labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Issue #197 (audit 2026-10-13, webunits-gameplay03): in normal play at the gameplay03 pose (its yaw, with
    /// the game's top-bar inset) the Tel Aviv pill keeps clear of the Kiryati Brigade standing in Tel Aviv: it overlaps
    /// neither the brigade's badge nor its figures, and it still hangs above the town (over its kit's width, not beside
    /// it); no shown pill overlaps any token's badge. The rectangles are measured
    /// independently of the presenter: the badge plate's corners as it faces the camera, the figures' renderer bounds,
    /// both through the camera's pixels.</summary>
    [UnityTest]
    public IEnumerator PillsKeepClearOfTheUnitTokens()
    {
        var layer = StartUnits();
        HdPanel hd = new HdPanel(cam);   // typed, not var: the local functions below capture it
        var labels = new MapLabelsPresenter(hd.Root, map, cam, keepClear: () => layer.KeepClear);
        var inset = new TopBarInset(hd.Root, rig);
        try
        {
            var pose = PromoPose.Gameplay03;
            rig.LookAt = map.Frame.ToWorld(pose.Lon, pose.Lat, 0); rig.Distance = pose.Distance; rig.Yaw = pose.Yaw; rig.Apply();
            yield return null;
            inset.Update();
            yield return Settle(labels);
            var tokens = GameObject.Find(Statecraft.Game.Map.Units.UnitLayer.RootName).transform.Find("Tokens");
            Rect Hull(System.Collections.Generic.IEnumerable<Vector3> points)
            {
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var w in points)
                {
                    var p = hd.ToPanel(w, out bool inFront); Assert.IsTrue(inFront);
                    x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
                }
                return Rect.MinMaxRect(x0, y0, x1, y1);
            }
            Rect BadgeRect(Transform token)
            {
                var badge = token.Find("Badge"); var turn = cam.transform.rotation;   // the plate faces the camera as it renders
                float half = Statecraft.Game.Map.Units.UnitBadge.SizeM / 2f;
                return Hull(new[] { new Vector3(-half, -half, 0), new Vector3(half, -half, 0), new Vector3(-half, half, 0), new Vector3(half, half, 0) }
                    .Select(c => badge.position + turn * c));
            }
            Rect FiguresRect(Transform token) => Hull(token.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Rifleman"))
                .SelectMany(r => { var b = r.bounds; return Enumerable.Range(0, 8).Select(c => new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z)); }));

            var kiryati = layer.Tokens.Single(t => t.Token.Unit.Id == "kiryati");
            Assert.AreEqual("Tel Aviv", kiryati.Token.Anchor.Place, "the brigade stands in Tel Aviv on the first turn");
            var token = tokens.Find("kiryati");
            Rect badgeRect = BadgeRect(token), figuresRect = FiguresRect(token);
            var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
            Assert.IsTrue(Shown(tlv), "Tel Aviv is named at gameplay03");
            var pill = tlv.Q<Label>().worldBound;
            TestContext.WriteLine($"gameplay03: Tel Aviv pill {pill}, Kiryati badge {badgeRect}, figures {figuresRect}");
            Assert.IsFalse(pill.Overlaps(badgeRect), $"the Tel Aviv pill {pill} overlaps the Kiryati badge {badgeRect}");
            Assert.IsFalse(pill.Overlaps(figuresRect), $"the Tel Aviv pill {pill} overlaps the Kiryati figures {figuresRect}");
            var kit = KitRectOf(hd, "tel_aviv");
            Assert.LessOrEqual(pill.yMax, kit.yMin + 1f, $"the pill {pill} still hangs above Tel Aviv's kit {kit}");
            Assert.IsTrue(pill.center.x >= kit.xMin && pill.center.x <= kit.xMax, $"over the town, not beside it: pill {pill}, kit {kit}");
            var badges = layer.Tokens.Select(t => (id: t.Token.Unit.Id, rect: BadgeRect(tokens.Find(t.Token.Unit.Id)))).ToList();
            foreach (var slot in labels.Slots.Concat(labels.LocalitySlots).Where(Shown))
                foreach (var (id, rect) in badges)
                    Assert.IsFalse(slot.Q<Label>().worldBound.Overlaps(rect), $"{slot.name} {slot.Q<Label>().worldBound} overlaps {id}'s badge {rect}");

            // The units off: with no camera move, the plain refresh re-places Tel Aviv's pill at its home above the kit.
            VisualToggles.Set(Statecraft.Game.Map.Units.UnitLayer.ToggleName, false);
            Assert.AreEqual(0, layer.KeepClear.Count, "a hidden layer asks no pill to keep clear");
            labels.Refresh(); yield return null;
            var home = tlv.Q<Label>().worldBound;
            TestContext.WriteLine($"units off: Tel Aviv pill {home}, kit {kit}");
            Assert.Greater(home.yMax, pill.yMax + 20f, $"the pill {home} came back down from {pill}");
            Assert.That(kit.yMin - home.yMax, Is.InRange(MapLabelsPresenter.LiftPx - 1f, 30f), $"back just above its kit: pill {home}, kit {kit}");
            // And on again: it goes back above the brigade.
            VisualToggles.Set(Statecraft.Game.Map.Units.UnitLayer.ToggleName, true);
            labels.Refresh(); yield return null;
            Assert.AreEqual(pill.yMax, tlv.Q<Label>().worldBound.yMax, 0.5f, "the units on again: above the brigade again");
        }
        finally { inset.Dispose(); labels.Dispose(); hd.Dispose(); }
    }

    static readonly string[] Occluders = { "top-bar", "info-panel", "minimap" };

    /// <summary>The units in play as a new game starts (golden seed, historical mode), as the game draws them.</summary>
    Statecraft.Game.Map.Units.UnitLayer StartUnits()
    {
        var content = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var units = Statecraft.Game.Map.Units.UnitData.Parse(File.ReadAllText(Path.Combine(Statecraft.Game.Paths.Content, "map", Statecraft.Game.Map.Units.UnitData.DefaultFileName)));
        var layer = Statecraft.Game.Map.Units.UnitLayer.Create(map, units, content.Hexes, still: true);
        layer.Show(content, Statecraft.Sim.Simulation.NewGame(content, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical));
        return layer;
    }

    /// <summary>The presenter, keeping clear of the units in play as the game does (GameRoot), or without them.</summary>
    MapLabelsPresenter Labels(HdPanel hd, bool units)
    {
        if (!units) return new MapLabelsPresenter(hd.Root, map, cam);
        var layer = StartUnits();
        return new MapLabelsPresenter(hd.Root, map, cam, keepClear: () => layer.KeepClear);
    }

    /// <summary>Marker tops on screen and not under a panel, in panel coordinates (independent of the presenter).</summary>
    static System.Collections.Generic.Dictionary<string, Vector2> VisibleMarkerTops(HdPanel hd, MapScene map)
    {
        var panels = Occluders.Select(n => hd.Root.Q<VisualElement>(n).worldBound).ToList();
        var size = hd.Root.worldBound.size;
        var tops = new System.Collections.Generic.Dictionary<string, Vector2>();
        foreach (var s in map.Settlements)
        {
            var p = hd.ToPanel(MarkerTopOf(s.Id), out bool inFront);
            if (inFront && p.x >= 0 && p.x <= size.x && p.y >= 0 && p.y <= size.y && !panels.Any(r => r.Contains(p))) tops[s.Id] = p;
        }
        return tops;
    }

    /// <summary>M2's cluster test, on layout bounds (review I5), widened by reviews I2/I4 and made robust to terrain changes
    /// (re-review, task 3). At 90 km (the flythrough's frame 120) and at the overview: no two shown pills overlap; every
    /// shown pill's marker is visible and the pill is more than twice as far (plus 4 px) from every other visible marker's
    /// top as from its own, so none reads as a neighbour's; completeness: every visible marker of a place not decluttered
    /// by zoom with no other visible marker within 2 pill heights + MarginPx is shown; only unambiguous names are pinned
    /// (well apart from any other marker), with a lower bound on the count; and at the overview a coastal town between
    /// Haifa and Tel Aviv is labelled. The full shown set is logged, for a new baseline.</summary>
    [UnityTest]
    public IEnumerator ShownPillsNeverOverlapAndEachReadsAsItsOwn() => NeverOverlapAndOwn(false);

    /// <summary>The same with the units in play on the map (review of #518): the tokens move pills, and a city with tokens
    /// on it (Tel Aviv, Jerusalem) keeps its name; each pill still reads as its own town's, measured from the kit.</summary>
    [UnityTest]
    public IEnumerator ShownPillsNeverOverlapAndEachReadsAsItsOwnWithTheUnits() => NeverOverlapAndOwn(true);

    IEnumerator NeverOverlapAndOwn(bool units)
    {
        var hd = new HdPanel(cam);
        var labels = Labels(hd, units);
        try
        {
            foreach (var (name, lon, lat, distance, pinned, atLeast) in new[]
            {
                ("90 km", 34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 90000f, new[] { "tel_aviv", "petah_tikva" }, 8),
                ("overview", PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, CameraController.MaxDistance, new[] { "tel_aviv", "jerusalem", "gaza", "beersheba" }, 7),
            })
            {
                Pose(lon, lat, distance);
                yield return Settle(labels);
                var tops = VisibleMarkerTops(hd, map);
                var shown = labels.Slots.Where(Shown).Select(s => (id: s.name.Substring("slot-".Length), rect: s.Q<Label>().worldBound)).ToList();
                TestContext.WriteLine($"{name}: {shown.Count} of {labels.Slots.Count} pills shown: {string.Join(" ", shown.Select(s => s.id))}");
                TestContext.WriteLine($"{name}: visible markers {string.Join(" ", tops.Select(t => $"{t.Key}@{t.Value.x:F0},{t.Value.y:F0}"))}");
                foreach (var id in pinned) Assert.IsTrue(shown.Any(s => s.id == id), $"{name}: {id} is shown");
                Assert.GreaterOrEqual(shown.Count, atLeast, $"{name}: shown pills");
                if (name == "overview") Assert.IsTrue(shown.Any(s => s.id == "hadera" || s.id == "netanya"), "overview: a coastal town between Haifa and Tel Aviv is labelled");
                for (int i = 0; i < shown.Count; i++)
                    for (int j = i + 1; j < shown.Count; j++)
                        Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{name}: {shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
                foreach (var (id, rect) in shown)
                {
                    Assert.IsTrue(tops.ContainsKey(id), $"{name}: {id} is shown but its marker is not visible");
                    // The pill's gap to its own kit (issue #214), as the presenter measures it: at most LiftPx, since its
                    // kit columns may reach a few px past the drawn vertices this test projects.
                    float own = Mathf.Min(RectGap(rect, KitRectOf(hd, id)), MapLabelsPresenter.LiftPx);
                    foreach (var other in tops.Where(o => o.Key != id))
                        Assert.Greater(Gap(rect, other.Value), 2f * own + 4f, $"{name}: {id} pill is {own:F0} px from its kit but closer to {other.Key}'s marker");
                }
                foreach (var s in map.Settlements.Where(s => tops.ContainsKey(s.Id) && !MapLabelsPresenter.HiddenByZoom(s.Type, distance)))
                {
                    float clear = 2f * labels.Labels.Single(l => l.name == "label-" + s.Id).worldBound.height + MapLabelsPresenter.MarginPx;
                    if (tops.Any(o => o.Key != s.Id && Vector2.Distance(o.Value, tops[s.Id]) <= clear)) continue;
                    Assert.IsTrue(shown.Any(x => x.id == s.Id), $"{name}: {s.Id} has no visible marker within {clear:F0} px, so it must be labelled");
                }
                if (distance > MapLabelsPresenter.DeclutterDistanceM)
                    foreach (var s in map.Settlements.Where(s => MapLabelsPresenter.HiddenByZoom(s.Type, distance)))
                        Assert.IsFalse(Shown(labels.Slots.Single(x => x.name == "slot-" + s.Id)), $"{name}: {s.Id} ({s.Type}) is decluttered");
            }
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Issue #599: close over Jaffa's harbour (gameplay03's yaw at 30, 40 and 50% of its distance, with the game's
    /// top-bar inset), Tel Aviv's and Jaffa's kit rectangles are hundreds of pixels across and overlap, and each holds or
    /// touches the other's marker. Every shown pill must still be nearer its own settlement's marker than any other
    /// visible marker, so "Tel Aviv" never hangs by Jaffa's harbour and "Jaffa" never sits on the pier by Tel Aviv. The
    /// markers are measured independently of the presenter. Jaffa is named at every pose, and Tel Aviv wherever its
    /// marker is visible (a city keeps its name). gameplay03 itself is checked the same way.</summary>
    [UnityTest]
    public IEnumerator EachPillIsNearerItsOwnMarkerByJaffasHarbour() => NearerOwnMarker(false);

    /// <summary>The same with the units in play (#518's keep-clear moves the spots round the Kiryati Brigade).</summary>
    [UnityTest]
    public IEnumerator EachPillIsNearerItsOwnMarkerByJaffasHarbourWithTheUnits() => NearerOwnMarker(true);

    IEnumerator NearerOwnMarker(bool units)
    {
        var hd = new HdPanel(cam);
        var labels = Labels(hd, units);
        var inset = new TopBarInset(hd.Root, rig);
        try
        {
            var g03 = PromoPose.Gameplay03;
            foreach (var pose in new[] { g03, new PromoPose("jaffa30", 34.76, 32.05, 0.3f * g03.Distance, g03.Yaw),
                         new PromoPose("jaffa40", 34.76, 32.05, 0.4f * g03.Distance, g03.Yaw), new PromoPose("jaffa50", 34.76, 32.05, 0.5f * g03.Distance, g03.Yaw) })
            {
                pose.Apply(rig, map.Frame);
                yield return null;
                inset.Update();
                yield return Settle(labels);
                var tops = VisibleMarkerTops(hd, map);
                var shown = labels.Slots.Where(Shown).Select(s => (id: s.name.Substring("slot-".Length), rect: s.Q<Label>().worldBound)).ToList();
                foreach (var id in new[] { "tel_aviv", "jaffa" })
                {
                    var p = shown.FirstOrDefault(x => x.id == id);
                    string Gaps() => string.Join(" ", new[] { "tel_aviv", "jaffa" }.Where(tops.ContainsKey).Select(o => $"{o} {Gap(p.rect, tops[o]):F0}"));
                    TestContext.WriteLine($"{pose.Name} units={units}: {id} marker {(tops.TryGetValue(id, out var t) ? t.ToString() : "not visible")} " +
                        (p.id == null ? "hidden" : $"pill {p.rect}, gap to {Gaps()}"));
                }
                Assert.IsTrue(shown.Any(x => x.id == "jaffa"), $"{pose.Name}: Jaffa is named");
                if (tops.ContainsKey("tel_aviv")) Assert.IsTrue(shown.Any(x => x.id == "tel_aviv"), $"{pose.Name}: Tel Aviv's marker is visible, so it is named");
                foreach (var (id, rect) in shown)
                {
                    Assert.IsTrue(tops.ContainsKey(id), $"{pose.Name}: {id} is shown but its marker is not visible");
                    float own = Gap(rect, tops[id]);
                    foreach (var other in tops.Where(o => o.Key != id))
                        Assert.Less(own, Gap(rect, other.Value), $"{pose.Name}: the {id} pill {rect} is {own:F0} px from its own marker but {Gap(rect, other.Value):F0} px from {other.Key}'s");
                }
            }
        }
        finally { inset.Dispose(); labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Re-review N1: a panel that appears (the event card) hides the pill it covers when its layout lands, from the
    /// geometry event itself, with no other refresh (in the game that is before the frame is drawn, not one frame later).</summary>
    [UnityTest]
    public IEnumerator TheEventCardAppearingHidesAnOverlappedPillAtOnce()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            Pose(34.78, 32.08, 90000f);   // Tel Aviv in the middle, where the decision card opens
            yield return Settle(labels);
            var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
            var before = tlv.Q<Label>().worldBound;
            Assert.IsTrue(Shown(tlv), "Tel Aviv is shown before the card opens");
            var card = hd.Root.Q<VisualElement>("event-card");
            card.style.minHeight = 500; card.style.display = DisplayStyle.Flex;
            for (int i = 0; i < 10 && (float.IsNaN(card.worldBound.width) || card.worldBound.width <= 0); i++) yield return null;
            TestContext.WriteLine($"Tel Aviv {before}, the card {card.worldBound}");
            Assert.IsTrue(card.worldBound.Overlaps(before), "precondition: the card covers Tel Aviv's pill");
            var after = tlv.Q<Label>().worldBound;
            Assert.IsTrue(!Shown(tlv) || !after.Overlaps(card.worldBound), $"Tel Aviv's pill {after} is still under the card {card.worldBound}");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Re-review N2: a pill that changes size with the camera still (a late font, a new text) is re-placed by the
    /// next plain refresh: label sizes are part of what Refresh checks before returning early.</summary>
    [UnityTest]
    public IEnumerator APillThatGrowsIsRePlacedWithoutACameraChange()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            Pose(34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 90000f);
            yield return Settle(labels);
            var tlv = labels.Labels.Single(l => l.name == "label-tel_aviv");
            float width = tlv.resolvedStyle.width;
            tlv.text = "Tel Aviv, the first Hebrew city, with Jaffa to the south";
            for (int i = 0; i < 10 && tlv.resolvedStyle.width <= width + 1f; i++) yield return null;
            Assert.Greater(tlv.resolvedStyle.width, width + 200f, "the pill grew");
            labels.Refresh();
            var shown = labels.Slots.Where(Shown).Select(s => (id: s.name, rect: s.Q<Label>().worldBound)).ToList();
            for (int i = 0; i < shown.Count; i++)
                for (int j = i + 1; j < shown.Count; j++)
                    Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Review I1: with the game's top-bar inset, no shown pill reaches under the top bar, the info panel or the
    /// minimap, at both promo poses.</summary>
    [UnityTest]
    public IEnumerator NoPillSitsUnderAPanel()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        var inset = new TopBarInset(hd.Root, rig);
        try
        {
            foreach (var pose in new[] { PromoPose.Diorama02, PromoPose.Gameplay03 })
            {
                pose.Apply(rig, map.Frame);
                yield return null;
                inset.Update();
                Assert.Greater(rig.TopInset, 0.05f, "the top bar's inset applies");
                yield return Settle(labels);
                var bar = hd.Root.Q<VisualElement>("top-bar").worldBound;
                var info = hd.Root.Q<VisualElement>("info-panel").worldBound;
                var mini = hd.Root.Q<VisualElement>("minimap").worldBound;
                var layer = hd.Root.Q<VisualElement>("map-layer").worldBound;   // the minimap's caption (#234)
                int count = 0;
                foreach (var slot in labels.Slots.Where(Shown))
                {
                    var r = slot.Q<Label>().worldBound; count++;
                    Assert.GreaterOrEqual(r.yMin, bar.yMax - 0.5f, $"{pose.Name}: {slot.name} {r} reaches under the top bar {bar}");
                    Assert.IsFalse(r.Overlaps(info), $"{pose.Name}: {slot.name} {r} under the info panel {info}");
                    Assert.IsFalse(r.Overlaps(mini), $"{pose.Name}: {slot.name} {r} under the minimap {mini}");
                    Assert.IsFalse(layer.width > 0 && r.Overlaps(layer), $"{pose.Name}: {slot.name} {r} under the map-layer caption {layer}");
                }
                var zikhron = labels.Slots.Single(s => s.name == "slot-zikhron_yaakov");
                TestContext.WriteLine($"{pose.Name}: {count} shown; Zikhron Ya'akov shown={Shown(zikhron)} {zikhron.Q<Label>().worldBound}; bar {bar}");
                Assert.Greater(count, 4, $"{pose.Name}: pills are still shown");
            }
        }
        finally { inset.Dispose(); labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>One step of the camera sweep the refresh tests share: a turn of 1.5 degrees and a zoom from 60 km out to
    /// the farthest, over 240 steps, so that every refresh sees a new view.</summary>
    void SweepStep(int i)
    {
        rig.Yaw += 1.5f; rig.Distance = Mathf.Lerp(60000f, CameraController.MaxDistance, i / 239f); rig.Apply();
    }

    /// <summary>Review I3: a refresh allocates nothing, forced or while the camera turns and zooms (240 refreshes, each with
    /// a new view, after one sweep that lets the presenter's lists reach their size). Counted by the profiler, so it does
    /// not depend on the machine's load (#322).</summary>
    [UnityTest]
    public IEnumerator RefreshWhileTheCameraMovesAllocatesNothing() => AllocatesNothing(false);

    /// <summary>The same with the unit tokens to keep clear of (the game's path).</summary>
    [UnityTest]
    public IEnumerator RefreshWhileTheCameraMovesAllocatesNothingWithTheUnits() => AllocatesNothing(true);

    IEnumerator AllocatesNothing(bool units)
    {
        var hd = new HdPanel(cam);
        var labels = Labels(hd, units);
        try
        {
            yield return Settle(labels);
            Assert.That(() => labels.Refresh(force: true), Is.Not.AllocatingGCMemory());
            for (int i = 0; i < 240; i++) { SweepStep(i); labels.Refresh(); }   // warm-up: the lists grow to their size
            for (int i = 0; i < 240; i++)
            {
                SweepStep(i);
                Assert.That(() => labels.Refresh(), Is.Not.AllocatingGCMemory(), $"refresh {i} of the sweep");
            }
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Review I3: placing every pill while the camera turns and zooms is cheap. The figure is the median refresh of
    /// the quickest of 5 sweeps of 240 refreshes (each with a new view): the median ignores the refreshes the scheduler
    /// interrupts, and the quickest sweep the stretches when another job holds the cores. The limit is set from measurements
    /// (#322): quiet runs give about 115-130 us, the worst loaded full runs averaged 193-216 us, and a refresh made 3x
    /// slower gives about 350 us or more.</summary>
    [UnityTest]
    public IEnumerator RefreshWhileTheCameraMovesIsCheap()
    {
        const int Sweeps = 5, Steps = 240;
        const double LimitUs = 300.0;   // editor Mono
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            yield return Settle(labels);
            var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
            var times = new double[Steps];
            var medians = new double[Sweeps];
            var watch = new System.Diagnostics.Stopwatch();
            int moved = 0; var last = tlv.style.translate.value;
            for (int s = 0; s < Sweeps; s++)
            {
                for (int i = 0; i < Steps; i++)
                {
                    SweepStep(i);
                    watch.Restart(); labels.Refresh(); watch.Stop();
                    times[i] = watch.Elapsed.TotalMilliseconds * 1000.0;
                    var now = tlv.style.translate.value;
                    if (now.x != last.x || now.y != last.y) moved++;
                    last = now;
                }
                System.Array.Sort(times);
                medians[s] = (times[Steps / 2 - 1] + times[Steps / 2]) / 2.0;
            }
            double best = medians.Min();
            TestContext.WriteLine(System.FormattableString.Invariant(
                $"LABELS refresh moving median_us={best:0} sweeps_us={string.Join(",", medians.Select(m => m.ToString("0", System.Globalization.CultureInfo.InvariantCulture)))} moved={moved}"));
            Assert.Greater(moved, Sweeps * Steps / 2, "the refreshes did work: Tel Aviv's pill moved");
            Assert.Less(best, LimitUs, $"median refresh of the quickest sweep under {LimitUs} us (editor Mono)");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Keeping clear of the unit tokens (#197) costs little next to placing the pills: one presenter, whose
    /// keep-clear list is switched between the units in play and none, runs interleaved sweeps (off, on, off, on, ...) of
    /// 240 refreshes along the same camera path; the median refresh with the units is at most 1.25 times the median without,
    /// both taken from the same run, so the machine's load cancels out (the absolute bound stays on the test above).</summary>
    [UnityTest]
    public IEnumerator KeepingClearOfTheUnitsIsCheap()
    {
        const int Pairs = 5, Steps = 240;
        const double MaxRatio = 1.25;
        var layer = StartUnits();
        bool on = false;
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam, keepClear: () => on ? layer.KeepClear : System.Array.Empty<Bounds[]>());
        try
        {
            yield return Settle(labels);
            var times = new double[Steps];
            var off = new System.Collections.Generic.List<double>(); var with = new System.Collections.Generic.List<double>();
            var watch = new System.Diagnostics.Stopwatch();
            for (int s = 0; s < 2 * Pairs; s++)
            {
                on = s % 2 == 1;
                for (int i = 0; i < Steps; i++)
                {
                    SweepStep(i);
                    watch.Restart(); labels.Refresh(); watch.Stop();
                    times[i] = watch.Elapsed.TotalMilliseconds * 1000.0;
                }
                System.Array.Sort(times);
                (on ? with : off).Add((times[Steps / 2 - 1] + times[Steps / 2]) / 2.0);
            }
            Assert.Greater(layer.KeepClear.Count, 0, "the units were on the map");
            double Median(System.Collections.Generic.List<double> xs) { xs.Sort(); return xs.Count % 2 == 1 ? xs[xs.Count / 2] : (xs[xs.Count / 2 - 1] + xs[xs.Count / 2]) / 2.0; }
            double mOff = Median(off), mOn = Median(with);
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            TestContext.WriteLine(System.FormattableString.Invariant(
                $"LABELS keep-clear cost off_us={mOff:0} on_us={mOn:0} ratio={mOn / mOff:0.00} off={string.Join(",", off.Select(m => m.ToString("0", inv)))} on={string.Join(",", with.Select(m => m.ToString("0", inv)))}"));
            Assert.LessOrEqual(mOn, MaxRatio * mOff, $"keeping clear of the units: {mOn:0} us against {mOff:0} us without them");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }
}
