using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Routes;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #725: MATKOT's four road vehicles on their 1948 roads, streamed, close zoom only, on ground Israel held,
/// and sized against the steam locomotive (the owner on v0.1.1, 18 October 2026: MATKOT's models were "way bigger then the
/// train").</summary>
public class RoadVehiclesTests
{
    static readonly DateTime Start = new DateTime(1948, 5, 15), BurmaOpen = new DateTime(1948, 6, 14),
        BurmaConvoysEnd = new DateTime(1948, 12, 7), TrainDate = new DateTime(1950, 3, 2);

    Action<string, CollectionDetail, Action<GameObject>> saved, savedTrain;
    readonly List<(string id, CollectionDetail detail, Action<GameObject> done)> asked =
        new List<(string, CollectionDetail, Action<GameObject>)>();
    readonly List<Action<GameObject>> trainAsked = new List<Action<GameObject>>();

    [SetUp]
    public void Seam()
    {
        saved = RoadVehicles.Request; savedTrain = ServiceTrain.Request;
        asked.Clear(); trainAsked.Clear();
        RoadVehicles.Request = (id, detail, done) => asked.Add((id, detail, done));
        ServiceTrain.Request = (id, detail, done) => trainAsked.Add(done);
    }

    [TearDown]
    public void Reset()
    {
        RoadVehicles.Request = saved; ServiceTrain.Request = savedTrain;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
    }

    static List<RoutePath> Routes()
    {
        var inputs = GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        var b = HeightmapLoader.Load(Paths.StreamingMap).meta;
        var warnings = new List<string>();
        var routes = RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, (b.lon0, b.lon1, b.lat0, b.lat1), warnings.Add);
        CollectionAssert.IsEmpty(warnings);
        return routes;
    }

    static Control1948 Control() => Control1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Control1948.FileName)));

    static GameObject Prefab(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));

    /// <summary>A built map with the control layer (as GameRoot gives it), its routes rebuilt at <paramref name="date"/>.</summary>
    (MapScene map, RoadVehicles layer) Build(DateTime date, RouteLayer.TrainMode train = RouteLayer.TrainMode.None, bool control = true)
    {
        var map = RouteTests.BuildMap();
        if (control) map.Control = Control();
        Object.DestroyImmediate(RouteTests.RoutesRoot(map).gameObject);
        asked.Clear(); trainAsked.Clear();   // the map's own build asked too; that layer is gone
        RouteLayer.Build(map.Terrain, map.Frame, Routes(), train, date, map: map);
        return (map, RouteTests.RoutesRoot(map).Find(RoadVehicles.Name).GetComponent<RoadVehicles>());
    }

    void AnswerAll()
    {
        foreach (var q in asked.ToArray()) q.done(Prefab(q.id));
    }

    /// <summary>A shape's extent along <paramref name="axis"/> (world x-z) and its height: the corners of each mesh's own
    /// bounds through its transform.</summary>
    static (float along, float across, float height) Extent(Transform t, Vector2 axis)
    {
        axis = axis.normalized; var right = new Vector2(axis.y, -axis.x);
        float a0 = float.MaxValue, a1 = float.MinValue, c0 = float.MaxValue, c1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
        foreach (var f in t.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null) continue;
            var b = f.sharedMesh.bounds;
            for (int c = 0; c < 8; c++)
            {
                var p = f.transform.TransformPoint(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z));
                var xz = new Vector2(p.x, p.z);
                float a = Vector2.Dot(xz, axis), r = Vector2.Dot(xz, right);
                a0 = Mathf.Min(a0, a); a1 = Mathf.Max(a1, a); c0 = Mathf.Min(c0, r); c1 = Mathf.Max(c1, r); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
            }
        }
        return (a1 - a0, c1 - c0, y1 - y0);
    }

    [Test]
    public void EveryModelHasItsRouteAndDate()
    {
        CollectionAssert.AreEquivalent(new[] { RoadVehicles.IntercityBus, RoadVehicles.CitrusTruck, RoadVehicles.CanvasLorry, RoadVehicles.SandwichConvoy },
            RoadVehicles.Plans.Select(p => p.Model).Distinct(), "all four of MATKOT's road vehicles are placed");
        CollectionAssert.AreEqual(new[]
        {
            (RoadVehicles.SandwichConvoy, "burma_road", "1948-06-14", "1948-12-07", 1),
            (RoadVehicles.CanvasLorry, "burma_road", "1948-06-14", "1948-12-07", 1),
            (RoadVehicles.IntercityBus, "tel_aviv_haifa_road", "1948-05-15", (string)null, 2),
            (RoadVehicles.CitrusTruck, "tel_aviv_haifa_road", "1948-05-15", (string)null, 2),
        }, RoadVehicles.Plans.Select(p => (p.Model, p.Route, p.From.ToString("yyyy-MM-dd"), p.To?.ToString("yyyy-MM-dd"), p.Count)));
        var routes = Routes().ToDictionary(r => r.Id);
        // The Burma Road's date is its route record's (content, sourced): the convoy runs only while the road is drawn.
        Assert.AreEqual(RoutePaths.Bound(routes["burma_road"].From), BurmaOpen);
        Assert.IsFalse(RoadVehicles.Plans.Any(p => p.Route == "jaffa_jerusalem_road"), "no convoy date on the Latrun road is sourced in the game window");
    }

    [Test]
    public void BurmaRoadVehiclesStopOn7DecemberWhileOtherRoutesContinue()
    {
        var on6December = RoadVehicles.Plans.Where(p => p.Covers(BurmaConvoysEnd.AddDays(-1))).ToArray();
        var on7December = RoadVehicles.Plans.Where(p => p.Covers(BurmaConvoysEnd)).ToArray();

        CollectionAssert.AreEquivalent(new[] { RoadVehicles.SandwichConvoy, RoadVehicles.CanvasLorry },
            on6December.Where(p => p.Route == "burma_road").Select(p => p.Model), "6 December: both Burma Road vehicles");
        CollectionAssert.IsEmpty(on7December.Where(p => p.Route == "burma_road"), "7 December: neither Burma Road vehicle");
        CollectionAssert.AreEquivalent(new[] { RoadVehicles.IntercityBus, RoadVehicles.CitrusTruck },
            on6December.Where(p => p.Route == "tel_aviv_haifa_road").Select(p => p.Model), "6 December: other routes unchanged");
        CollectionAssert.AreEquivalent(new[] { RoadVehicles.IntercityBus, RoadVehicles.CitrusTruck },
            on7December.Where(p => p.Route == "tel_aviv_haifa_road").Select(p => p.Model), "7 December: other routes unchanged");
    }

    [Test]
    public void ModelsStreamFromTheirDateAndOneThatFailsDropsOnlyItsOwn()
    {
        var (map, layer) = Build(Start);
        try
        {
            CollectionAssert.AreEquivalent(new[] { RoadVehicles.IntercityBus, RoadVehicles.CitrusTruck }, asked.Select(q => q.id),
                "15 May 1948: only the Tel Aviv–Haifa road's models are asked for");
            Assert.IsTrue(asked.All(q => q.detail == CollectionDetail.Near), "close zoom: the near models");
            Assert.IsNull(saved, "the default: the game's stream in Play mode (CollectionStream.Request), none in edit mode");
            CollectionAssert.IsEmpty(layer.Placed, "nothing drawn until the stream answers (a later frame)");
            AnswerAll();
            Assert.AreEqual(4, layer.Placed.Count, "two buses and two citrus trucks");

            RouteLayer.ShowAt(map, BurmaOpen.AddDays(-1));
            Assert.AreEqual(2, asked.Count, "13 June: the convoy models are not asked for yet");
            RouteLayer.ShowAt(map, BurmaOpen);
            Assert.AreEqual(4, asked.Count, "14 June: the Burma Road's two models");
            var failed = asked.Single(q => q.id == RoadVehicles.SandwichConvoy);
            LogAssert.Expect(LogType.Warning, $"ROAD VEHICLES: {RoadVehicles.SandwichConvoy} did not stream; its vehicles are not drawn, and the map plays on without them.");
            failed.done(null);
            asked.Single(q => q.id == RoadVehicles.CanvasLorry).done(Prefab(RoadVehicles.CanvasLorry));
            Assert.AreEqual(5, layer.Placed.Count, "the failed convoy truck removes only itself");
            Assert.IsTrue(layer.Placed.All(p => p.Model != RoadVehicles.SandwichConvoy));

            int builds = layer.Builds;
            RouteLayer.ShowAt(map, BurmaOpen.AddDays(1));
            RouteLayer.ShowAt(map, BurmaOpen.AddDays(2));
            Assert.AreEqual(builds, layer.Builds, "a state change with nothing new lays nothing out again");
            RouteLayer.ShowAt(map, Start);
            Assert.AreEqual(4, layer.Placed.Count, "an earlier date (a loaded save): no convoy");
            Assert.AreEqual(4, asked.Count, "date changes never ask again");
            LogAssert.NoUnexpectedReceived();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The issue's history rule: no vehicle on a road the other side held at that date. Every vehicle stands on a
    /// hex the control layer gives to Israel on the date; with no control layer, none.</summary>
    [Test]
    public void EveryVehicleStandsWhereIsraelHeldTheGround()
    {
        var control = Control();
        var (map, layer) = Build(BurmaOpen);
        try
        {
            AnswerAll();
            foreach (var date in new[] { Start, BurmaOpen, new DateTime(1948, 7, 20), new DateTime(1948, 11, 1), TrainDate })
            {
                RouteLayer.ShowAt(map, date); AnswerAll();
                Assert.Greater(layer.Placed.Count, 0, $"{date:yyyy-MM-dd}: some vehicles");
                foreach (var p in layer.Placed)
                {
                    var (q, r) = MapPointer.Pick(map, new Vector3(p.At.x, 0f, p.At.y));
                    Assert.AreEqual(Control1948.Side.Israel, control.At(q, r, date).side, $"{date:yyyy-MM-dd} {p.Model} on {p.Route} at hex {q},{r}");
                }
            }
            // A control layer that gives Israel no hex: no vehicle anywhere.
            map.Control = Control1948.Parse("layer: x\nshown_from: 1948-05-15\nperiods:\n  - { id: none_held, from: 1948-05-15, until: null, legend: held, line_date: '1948-07-09', fronts: [] }\nhexes: []\n");
            RouteLayer.ShowAt(map, TrainDate);
            CollectionAssert.IsEmpty(layer.Placed, "no hex held: no vehicle");
        }
        finally { Object.DestroyImmediate(map.gameObject); }

        asked.Clear();
        (map, layer) = Build(BurmaOpen, control: false);
        try
        {
            AnswerAll();
            CollectionAssert.IsEmpty(layer.Placed, "no control layer: no claim about who held a road, so no vehicle");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void SeededSpotsRepeatFaceTheRoadStandUprightAndKeepClear()
    {
        List<RoadVehicles.PlacedVehicle> first;
        var (map, layer) = Build(BurmaOpen);
        try
        {
            AnswerAll();
            first = layer.Placed.ToList();
            Assert.AreEqual(6, first.Count, "two of each on the Tel Aviv–Haifa road, one of each on the Burma Road");
            foreach (var p in first)
            {
                var (along, across, _) = Extent(p.Transform, p.Along);
                Assert.Greater(along, 1.5f * across, $"{p.Model}: its long side runs along the road");
                // Upright: the prefab's own root rotation (the FBX's axis turn) is kept under the yaw, so its up stays up.
                var own = Prefab(p.Model).transform.localRotation;
                Assert.Less(Vector3.Angle(p.Transform.rotation * Quaternion.Inverse(own) * Vector3.up, Vector3.up), 0.01f, $"{p.Model} is upright");
                var bounds = p.Transform.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                float ground = RouteLayer.Ground(map.Terrain.terrainData, map.Terrain.transform.position, p.At) + RoadVehicles.RideM;
                Assert.AreEqual(ground, bounds.min.y, 1f, $"{p.Model} stands on the road");
                foreach (var f in map.KitFootprints)
                    Assert.GreaterOrEqual((p.At - f.centre).magnitude, f.radius + RoadVehicles.AnchorClearanceM, $"{p.Model} clear of {f.id} (its town and label)");
                foreach (var t in map.UnitTokens)
                    Assert.GreaterOrEqual((p.At - t.centre).magnitude, t.radius + RoadVehicles.AnchorClearanceM, $"{p.Model} clear of a unit token");
                foreach (var c in map.UnitClearings)
                    Assert.GreaterOrEqual((p.At - c.centre).magnitude, c.radius + RoadVehicles.AnchorClearanceM, $"{p.Model} clear of a unit's clearing");
            }
            for (int i = 0; i < first.Count; i++)
                for (int j = i + 1; j < first.Count; j++)
                    Assert.GreaterOrEqual((first[i].At - first[j].At).magnitude, RoadVehicles.PairSpacingM, "vehicles apart");
        }
        finally { Object.DestroyImmediate(map.gameObject); }

        asked.Clear();
        (map, layer) = Build(BurmaOpen);
        try
        {
            // The other order: a model streaming in late never moves another's vehicles.
            foreach (var q in asked.ToArray().Reverse()) q.done(Prefab(q.id));
            CollectionAssert.AreEqual(first.Select(p => (p.Model, p.Route, p.At)).ToList(),
                layer.Placed.Select(p => (p.Model, p.Route, p.At)).ToList(), "same seed, same spots, bit for bit");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The owner on v0.1.1 (18 October 2026): MATKOT's models were "way bigger then the train". A bus or a lorry
    /// reads smaller than the steam locomotive the map runs: each vehicle's length is 0.3 to 0.8 of the locomotive's car as
    /// drawn (ServiceTrain, measured on the map, not from the constant), and it stands lower than the locomotive. The unit
    /// figures (#992) are Civ-sized pieces far larger still; the vehicles are scenery and stay under a figure's height.</summary>
    [Test]
    public void EveryVehicleReadsSmallerThanTheLocomotive()
    {
        float locoLong, locoHigh;
        var (map, layer) = Build(TrainDate, RouteLayer.TrainMode.Service);
        try
        {
            Assert.AreEqual(1, trainAsked.Count, "the service train asks for its locomotive on its date");
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("^TRAIN: the steam train runs"));
            trainAsked[0](AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(ServiceTrain.ModelId, CollectionDetail.Far)));
            var service = RouteTests.RoutesRoot(map).Find(ServiceTrain.Name).GetComponent<ServiceTrain>();
            var loco = service.Train.Cars[0];
            Assert.AreEqual("Locomotive", loco.name);
            (locoLong, _, locoHigh) = Extent(loco, new Vector2(loco.forward.x, loco.forward.z));
            Assert.AreEqual(RoadVehicles.LocomotiveCarM, locoLong, 0.05f * locoLong, "the locomotive's car as drawn");

            AnswerAll();
            Assert.AreEqual(4, layer.Placed.Count, "the Burma Road convoy truck and lorry have stopped by 1950 (BurmaConvoysEnd)");
            AssertBelowTheLocomotive(layer, locoLong, locoHigh);
        }
        finally { Object.DestroyImmediate(map.gameObject); }

        // The two Burma Road models run only until 7 December 1948, before the service train: measure them on 6 December
        // against the locomotive car as drawn above.
        var (map48, layer48) = Build(BurmaConvoysEnd.AddDays(-1));
        try
        {
            AnswerAll();
            Assert.That(layer48.Placed.Count(p => p.Model == RoadVehicles.SandwichConvoy || p.Model == RoadVehicles.CanvasLorry), Is.EqualTo(2));
            AssertBelowTheLocomotive(layer48, locoLong, locoHigh);
        }
        finally { Object.DestroyImmediate(map48.gameObject); }
    }

    static void AssertBelowTheLocomotive(RoadVehicles layer, float locoLong, float locoHigh)
    {
        float figure = UnitBadge.FigureHeightM * UnitShowcase.FigureScale;
        foreach (var p in layer.Placed)
        {
            var (len, _, high) = Extent(p.Transform, p.Along);
            float share = len / locoLong;
            TestContext.WriteLine($"{p.Model}: {len:0} m long, {high:0} m high; locomotive {locoLong:0} m long, {locoHigh:0} m high; share {share:0.00}");
            Assert.That(share, Is.InRange(0.3f, 0.8f), $"{p.Model}: {len:0} m against the locomotive's {locoLong:0} m");
            Assert.Less(high, locoHigh, $"{p.Model} stands lower than the locomotive");
            Assert.Less(high, figure, $"{p.Model} stands lower than a unit figure");
        }
    }

    [Test]
    public void VehiclesDrawOnlyAtCloseZoomAndAssetsStayOutOfResources()
    {
        Assert.IsTrue(RoadVehicles.CloseAt(RoadVehicles.CloseEnterM, false));
        Assert.IsTrue(RoadVehicles.CloseAt(RoadVehicles.CloseExitM, true));
        Assert.IsFalse(RoadVehicles.CloseAt(RoadVehicles.CloseEnterM + 1f, false));
        Assert.IsFalse(RoadVehicles.CloseAt(RoadVehicles.CloseExitM + 1f, true));
        Assert.Greater(RoadVehicles.CloseExitM / RoadVehicles.CloseEnterM, 1f / (1f - CameraController.ZoomPerStep), "the band is wider than one zoom step");

        foreach (var id in RoadVehicles.Plans.Select(p => p.Model).Distinct())
        {
            Assert.AreEqual(CollectionCatalog.Vehicles, CollectionCatalog.BundleOf(id));
            var path = CollectionCatalog.PrefabPath(id, CollectionDetail.Near);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
            StringAssert.DoesNotContain("/Resources/", path);
            foreach (var dep in AssetDatabase.GetDependencies(path, true)) StringAssert.DoesNotContain("/Resources/", dep);
            Assert.IsNull(Resources.Load<GameObject>(id));
        }
    }
}
