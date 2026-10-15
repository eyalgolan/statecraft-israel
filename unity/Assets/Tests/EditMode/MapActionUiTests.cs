using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class MapActionUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly HexFile Hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
    static readonly System.Collections.Generic.List<Settlement> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void HexPickerAgreesWithTheToolsSettlementRule()
    {
        // tools/settlement_hexes.py moves a settlement off a water hex to its nearest land neighbour (Tiberias); every
        // other settlement must land on exactly the hex the C# port computes.
        foreach (var t in Towns)
        {
            var picked = HexPicker.FromLonLat(t.Lon, t.Lat, Hexes.bounds, Hexes.hex_size_km);
            var owner = C.SettlementHexes.Single(s => s.Id == t.Id);
            if (t.Id == "tiberias") { Assert.AreNotEqual((owner.Q, owner.R), picked); continue; }
            Assert.AreEqual((owner.Q, owner.R), picked, t.Id);
        }
    }

    [Test]
    public void GeoFrameRoundTripsLonLat()
    {
        var f = new GeoFrame(34.2, 35.9, 29.45, 33.35, 161530f, 431220f);
        var (lon, lat) = f.ToLonLat(f.ToWorld(34.78, 32.08, 0));
        Assert.AreEqual(34.78, lon, 1e-5); Assert.AreEqual(32.08, lat, 1e-5);
    }

    static SimBridge At(DateTime date)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < date) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g, Text);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge) =>
        new InfoPanelPresenter(ui.Root, bridge, Text, Towns.ToDictionary(t => t.Id, t => t.Name));

    [Test]
    public void BeforeTheMaabarotTheButtonsSayWhy()
    {
        using var ui = new UiTestKit();
        var info = Panel(ui, At(new DateTime(1949, 6, 1)));
        Assert.AreEqual("Click the map to inspect a hex", ui.Q<Label>("info-title").text);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text);
        StringAssert.StartsWith("Coastal plain · elevation ", ui.Q<Label>("info-subtitle").text);
        StringAssert.DoesNotContain("hex (", ui.Q<Label>("info-subtitle").text, "no debug coordinates in player text (playtest 2, P2-6)");
        Assert.IsFalse(ui.Q<Button>("build-camp").enabledSelf);
        Assert.AreEqual("Transit camps and housing projects can be placed from 1 January 1950", ui.Q<Label>("info-reason").text);
    }

    [Test]
    public void ATransitCampIsOrderedAndBuiltAtEndTurn()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));                        // after the ma'abarot decision (May 1950)
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        Assert.IsTrue(ui.Q<Button>("build-camp").enabledSelf, ui.Q<Label>("info-reason").text);
        Assert.AreEqual("Place a transit camp (IL 1.5m)", ui.Q<Button>("build-camp").text);
        ui.Press("build-camp");
        Assert.AreEqual("Ordered: A transit camp, at End Turn", ui.Q<Label>("info-placement").text);
        bridge.EndTurn();
        Assert.AreEqual(MapActions.TransitCamp, MapActions.At(bridge.State, tlv.Q, tlv.R)!.Action);
        Assert.AreEqual("A transit camp built here in August 1950", ui.Q<Label>("info-placement").text);
    }

    /// <summary>The minimap's painted relief (tools/minimap_bake.py) is north up over the hex grid's bounds: sea off Tel Aviv
    /// and in the Gulf of Aqaba, land in the hills and the Negev. Read from the PNG itself (the imported texture is not
    /// readable on the CPU).</summary>
    [Test]
    public void TheMinimapReliefHasLandAndSeaNorthUp()
    {
        var tex = new Texture2D(2, 2);
        try
        {
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "unity", MinimapPresenter.AssetPath))), MinimapPresenter.AssetPath);
            var b = Hexes.bounds;
            Color At(double lon, double lat) =>
                tex.GetPixel((int)((lon - b.lon0) / (b.lon1 - b.lon0) * tex.width), (int)((lat - b.lat0) / (b.lat1 - b.lat0) * tex.height));
            void Sea(double lon, double lat, string where) { var c = At(lon, lat); Assert.Greater(c.b, c.r + 0.1f, $"{where} is sea: {c}"); }
            void Land(double lon, double lat, string where) { var c = At(lon, lat); Assert.Greater(c.r, c.b, $"{where} is land: {c}"); }
            Sea(34.30, 32.60, "the Mediterranean off Tel Aviv");
            Sea(34.92, 29.47, "the Gulf of Aqaba (its middle: about 34.88-34.96 E at 29.47 N)");
            Land(35.21, 31.77, "Jerusalem");
            Land(34.90, 30.50, "the Negev");
            Land(35.10, 32.95, "the Galilee");
        }
        finally { UnityEngine.Object.DestroyImmediate(tex); }
    }

    [Test]
    public void PlacementsAreDrawnOnTheirHexes()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
            var map = new GameObject("MapScene").AddComponent<MapScene>();
            map.Configure(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSelection.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sea.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MapFrame.mat"));
            map.Build();
            var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
            var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
            layer.Render(new[] { new Placement(MapActions.TransitCamp, tlv.Q, tlv.R, new DateTime(1950, 8, 26)) }, C, map);
            layer.Render(new[] { new Placement(MapActions.Housing, tlv.Q, tlv.R, new DateTime(1950, 8, 26)) }, C, map);
            Assert.AreEqual(1, layer.transform.childCount);                 // Render replaces what it drew
            var box = layer.transform.GetChild(0);
            Assert.AreEqual($"housing_{tlv.Q}_{tlv.R}", box.name);
            var hex = C.HexAt(tlv.Q, tlv.R)!;
            var expected = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
            Assert.AreEqual(expected.x, box.position.x, 1f); Assert.AreEqual(expected.z, box.position.z, 1f);
            Assert.AreEqual((tlv.Q, tlv.R), MapPointer.Pick(map, box.position));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void TheInfoPanelShowsNoTileYields()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        var shown = string.Join(" | ", new[] { "info-title", "info-subtitle", "info-settlements", "info-placement", "info-reason" }.Select(n => ui.Q<Label>(n).text)).ToLowerInvariant();
        foreach (var word in new[] { "yield", "food", "production", "gold", "income" }) StringAssert.DoesNotContain(word, shown);
    }

    [Test]
    public void ThePerTurnCapAndRefusalsComeFromTheSim()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        new ToastPresenter(ui.Root, bridge, Text);
        var info = Panel(ui, bridge);
        var hexes = C.SettlementHexes.Select(h => (h.Q, h.R)).Distinct().Take(3).ToList();
        foreach (var (q, r) in hexes) { info.Show(q, r); ui.Press("build-housing"); }
        Assert.AreEqual(3, bridge.Queued.OfType<MapAction>().Count());      // the sim, not the UI, enforces the cap at End Turn
        bridge.EndTurn();
        Assert.AreEqual(1, bridge.Session.Last!.Rejected.Count);
        StringAssert.Contains($"No more than {C.Rules.MapActionsPerTurn} map actions in one turn", ui.Q<Label>("toast").text);
    }

    [Test]
    public void ABuiltHexCannotBeBuiltOnAgainAndSaysWhy()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        ui.Press("build-housing");
        bridge.EndTurn();
        Assert.IsFalse(ui.Q<Button>("build-housing").enabledSelf);
        Assert.AreEqual(MapActions.Check(C, bridge.State, MapActions.Housing, tlv.Q, tlv.R), ui.Q<Label>("info-reason").text);
    }

    [UnityTest]
    public System.Collections.IEnumerator AClickOnTheMinimapMapsToTheDrawnMapNotTheLetterbox()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes);
        var image = ui.Q<VisualElement>("minimap-image");
        for (int i = 0; i < 30 && !(image.layout.width > 0 && Mathf.Abs(image.layout.width / image.layout.height - mini.Aspect) < 0.01f); i++) yield return null;
        Assert.Greater(image.layout.width, 0f);
        // the element is exactly as big as the picture (aspect of the texture), so there is no unused strip inside it
        Assert.AreEqual(mini.Aspect, image.layout.width / image.layout.height, 0.01f);
        (double lon, double lat)? clicked = null;
        mini.Clicked += (lon, lat) => clicked = (lon, lat);
        var b = Hexes.bounds;
        // a click a quarter of the way across and a quarter of the way down the visible map
        var local = new Vector2(image.layout.width * 0.25f, image.layout.height * 0.25f);
        using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = image.LocalToWorld(local) }))
        { e.target = image; image.SendEvent(e); }
        Assert.IsNotNull(clicked);
        Assert.AreEqual(b.lon0 + 0.25 * (b.lon1 - b.lon0), clicked!.Value.lon, 1e-6);
        Assert.AreEqual(b.lat1 - 0.25 * (b.lat1 - b.lat0), clicked.Value.lat, 1e-6);
        clicked = null;
        using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = image.LocalToWorld(local) }))
        { e.target = image; image.SendEvent(e); }
        Assert.IsNull(clicked, "only the left button clicks");
    }

    [UnityTest]
    public System.Collections.IEnumerator TheViewFrameLiesInsideTheDrawnMap()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes);
        var image = ui.Q<VisualElement>("minimap-image");
        for (int i = 0; i < 30 && !(image.layout.width > 0); i++) yield return null;
        var b = Hexes.bounds;
        var f = new GeoFrame(b.lon0, b.lon1, b.lat0, b.lat1, 161530f, 431220f);
        mini.SetView(f, f.ToWorld(34.95, 31.95, 0), 20000f);
        yield return null;
        var view = ui.Q<VisualElement>("minimap-view");
        Assert.AreEqual(LengthUnit.Percent, view.style.left.value.unit);
        Assert.GreaterOrEqual(view.style.left.value.value, 0f);
        Assert.GreaterOrEqual(view.style.top.value.value, 0f);
        Assert.LessOrEqual(view.style.left.value.value + view.style.width.value.value, 100f);
        Assert.LessOrEqual(view.style.top.value.value + view.style.height.value.value, 100f);
        // a view at the very edge stays inside the picture
        foreach (var (lon, lat) in new[] { (b.lon0, b.lat0), (b.lon1, b.lat0), (b.lon0, b.lat1), (b.lon1, b.lat1) })
        {
            mini.SetView(f, f.ToWorld(lon, lat, 0), 20000f);
            yield return null;
            Assert.GreaterOrEqual(view.style.left.value.value, 0f);
            Assert.GreaterOrEqual(view.style.top.value.value, 0f);
            Assert.LessOrEqual(view.style.left.value.value + view.style.width.value.value, 100.001f, "right edge");
            Assert.LessOrEqual(view.style.top.value.value + view.style.height.value.value, 100.001f, "bottom edge");
            if (view.layout.width > 0)
            {
                Assert.LessOrEqual(view.layout.xMax, image.layout.width + 0.5f);
                Assert.LessOrEqual(view.layout.yMax, image.layout.height + 0.5f);
            }
        }
        Assert.AreSame(image, view.parent, "the frame is positioned in the picture's own coordinates");
    }

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSelection.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sea.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MapFrame.mat"));
        map.Build();
        Physics.SyncTransforms();
        return map;
    }

    static bool Down(MapScene map, double lon, double lat, out RaycastHit hit)
    {
        var from = map.Frame.ToWorld(lon, lat, 100_000f);
        return map.Terrain.GetComponent<TerrainCollider>().Raycast(new Ray(from, Vector3.down), out hit, 200_000f);
    }

    [Test]
    public void RaysDownOntoTheTerrainPickTheRightHexes()
    {
        var map = BuildMap();
        try
        {
            var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
            var a = C.HexAt(tlv.Q, tlv.R)!;
            Assert.IsTrue(Down(map, a.Lon, a.Lat, out var hit));
            Assert.AreEqual((tlv.Q, tlv.R), MapPointer.Pick(map, hit.point), "Tel Aviv's hex centre");

            // two points 200 m either side of the middle of the edge Tel Aviv's hex shares with its eastern neighbour
            var b = C.HexAt(tlv.Q + 1, tlv.R)!;
            var wa = map.Frame.ToWorld(a.Lon, a.Lat, 0); var wb = map.Frame.ToWorld(b.Lon, b.Lat, 0);
            var mid = (wa + wb) / 2; var dir = (wb - wa).normalized;
            foreach (var (side, expected) in new[] { (-1f, (tlv.Q, tlv.R)), (1f, (tlv.Q + 1, tlv.R)) })
            {
                var (lon, lat) = map.Frame.ToLonLat(mid + dir * 200f * side);
                Assert.IsTrue(Down(map, lon, lat, out hit));
                Assert.AreEqual(expected, MapPointer.Pick(map, hit.point), "side " + side);
            }

            Assert.IsTrue(Down(map, 34.30, 32.60, out hit));
            var seaHex = MapPointer.Pick(map, hit.point);
            Assert.IsFalse(C.HexAt(seaHex.q, seaHex.r)!.Land, "the Mediterranean");

            var f = map.Frame; const double inset = 0.005;
            foreach (var (lon, lat) in new[] { (f.Lon0 + inset, f.Lat0 + inset), (f.Lon1 - inset, f.Lat0 + inset), (f.Lon0 + inset, f.Lat1 - inset), (f.Lon1 - inset, f.Lat1 - inset) })
            {
                Assert.IsTrue(Down(map, lon, lat, out hit), $"a ray reaches the terrain at ({lon}, {lat})");
                var corner = MapPointer.Pick(map, hit.point);
                Assert.AreEqual(HexPicker.FromLonLat(lon, lat, map.Hexes.bounds, map.Hexes.hex_size_km), corner);
            }
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void AClickOverTheUiNeverReachesTheMapAndOtherwiseHitsTheHexUnderTheCamera()
    {
        var map = BuildMap();
        try
        {
            var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
            var hex = C.HexAt(tlv.Q, tlv.R)!;
            var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
            var cam = Camera.main!;
            cam.transform.position = new Vector3(ground.x, 50_000f, ground.z);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.farClipPlane = 200_000f;
            var pointer = new GameObject("Pointer").AddComponent<MapPointer>();
            pointer.Map = map;
            var picked = new System.Collections.Generic.List<(int, int)>();
            pointer.HexClicked += (q, r) => picked.Add((q, r));
            var centre = new Vector2(cam.pixelWidth / 2f, cam.pixelHeight / 2f);

            pointer.IsOverUi = _ => false;
            Assert.IsTrue(pointer.HandleClick(centre));
            Assert.AreEqual(new[] { (tlv.Q, tlv.R) }, picked);

            picked.Clear();
            pointer.IsOverUi = _ => true;
            Assert.IsFalse(pointer.HandleClick(centre));
            Assert.IsEmpty(picked);

            pointer.Map = null;                                          // the UI is asked first, even with no map
            bool asked = false; pointer.IsOverUi = _ => { asked = true; return true; };
            Assert.IsFalse(pointer.HandleClick(centre));
            Assert.IsTrue(asked);
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void AMissingRuleShowsNoFalseCost()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var rule = C.Rules.MapActions[MapActions.Housing];
        C.Rules.MapActions.Remove(MapActions.Housing);
        try
        {
            info.Show(tlv.Q, tlv.R);
            Assert.AreEqual("Build housing", ui.Q<Button>("build-housing").text);
            Assert.IsFalse(ui.Q<Button>("build-housing").enabledSelf);
        }
        finally { C.Rules.MapActions[MapActions.Housing] = rule; }
    }

    [Test]
    public void ADisposedMinimapLetsGoOfItsPictureAndADisposedPanelHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        info.Dispose();
        ui.Q<Label>("info-title").text = "untouched";
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("info-title").text);
        var mini = new MinimapPresenter(ui.Root, Hexes);
        var tex = mini.Texture;
        mini.Dispose();
        // The picture is the shared baked asset: the presenter lets go of it and does not destroy it.
        Assert.IsTrue(ui.Q<VisualElement>("minimap-image").style.backgroundImage.keyword == StyleKeyword.Null, "nothing draws the picture after Dispose");
        Assert.IsTrue(tex != null, "the baked picture is an asset and stays loaded");
    }

    [Test]
    public void AClickBeforeLayoutRaisesNothing()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes);
        var image = ui.Q<VisualElement>("minimap-image");
        Assert.IsFalse(image.layout.width > 0, "no layout has run yet");
        int clicks = 0;
        mini.Clicked += (lon, lat) => clicks++;
        using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(10, 10) }))
        { e.target = image; image.SendEvent(e); }
        Assert.AreEqual(0, clicks);
    }

    /// <summary>eras.yaml systems.map_actions (#58): an action the era does not allow has no button and no reason; with
    /// both allowed both show. Played on a copy of the content, switching the list between renders.</summary>
    [Test]
    public void OnlyTheErasMapActionsHaveButtons()
    {
        var c = ContentLoader.Load(Paths.Content);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 8, 1)) g.Step(HistoricalAI.Commands(c, g.State));
        using var ui = new UiTestKit();
        var info = new InfoPanelPresenter(ui.Root, new SimBridge(g, Text), Text, Towns.ToDictionary(t => t.Id, t => t.Name));
        var tlv = c.SettlementHexes.Single(s => s.Id == "tel_aviv");
        void ShowWith(params string[] actions) { c.Eras[0].Systems.MapActions = actions.ToList(); info.Show(tlv.Q, tlv.R); }
        DisplayStyle Display(string name) => ui.Q<Button>(name).style.display.value;

        ShowWith(MapActions.TransitCamp, MapActions.Housing);
        Assert.AreEqual(DisplayStyle.Flex, Display("build-camp")); Assert.AreEqual(DisplayStyle.Flex, Display("build-housing"));
        Assert.IsTrue(ui.Q<Button>("build-camp").enabledSelf, ui.Q<Label>("info-reason").text);

        ShowWith(MapActions.Housing);
        Assert.AreEqual(DisplayStyle.None, Display("build-camp")); Assert.AreEqual(DisplayStyle.Flex, Display("build-housing"));
        Assert.IsFalse(ui.Q<Button>("build-camp").enabledSelf);
        StringAssert.DoesNotContain("chapter", ui.Q<Label>("info-reason").text);

        ShowWith();
        Assert.AreEqual(DisplayStyle.None, Display("build-camp")); Assert.AreEqual(DisplayStyle.None, Display("build-housing"));
        Assert.AreEqual("", ui.Q<Label>("info-reason").text);
    }
}
