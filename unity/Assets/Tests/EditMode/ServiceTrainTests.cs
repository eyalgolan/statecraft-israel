using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Routes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #668 (maintainer decision on #35, 16 October 2026: "Yes, normal play", on the Jaffa–Jerusalem line, only
/// from the sourced date regular service resumed): the steam train in normal play. Its date and stretch come from the
/// content (routes_1948.yaml, through the runtime file), its model is Tal's H09, streamed after the first frame through
/// CollectionStream, and it fails open.</summary>
public class ServiceTrainTests
{
    static readonly DateTime ServiceDate = new DateTime(1950, 3, 2);
    static readonly (double lon, double lat) TelAvivSouth = (34.777, 32.0636), Jerusalem = (35.22471, 31.76685);
    static readonly (double lon, double lat) Lod = RouteLayer.TrainTurnAt;

    Action<string, CollectionDetail, Action<GameObject>> saved;
    readonly List<(string id, CollectionDetail detail, Action<GameObject> done)> asked = new List<(string, CollectionDetail, Action<GameObject>)>();

    [SetUp]
    public void Seam()
    {
        saved = ServiceTrain.Request;
        asked.Clear();
        ServiceTrain.Request = (id, detail, done) => asked.Add((id, detail, done));
    }

    [TearDown]
    public void Reset()
    {
        ServiceTrain.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
    }

    static MapInputs Inputs() => GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);

    static (double, double, double, double) Bounds()
    {
        var b = HeightmapLoader.Load(Paths.StreamingMap).meta;
        return (b.lon0, b.lon1, b.lat0, b.lat1);
    }

    /// <summary>The routes as the player reads them at boot (the runtime file).</summary>
    static List<RoutePath> BootRoutes()
    {
        var inputs = Inputs();
        var warnings = new List<string>();
        var routes = RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, Bounds(), warnings.Add);
        CollectionAssert.IsEmpty(warnings);
        return routes;
    }

    static GameObject FarPrefab() =>
        AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(ServiceTrain.ModelId, CollectionDetail.Far));

    /// <summary>A map whose routes are built for normal play (the service train) at <paramref name="date"/>; the editor's
    /// own build (the showcase's still train) is replaced.</summary>
    static (MapScene map, ServiceTrain service) NormalPlayMap(DateTime date)
    {
        var map = RouteTests.BuildMap();
        Object.DestroyImmediate(RouteTests.RoutesRoot(map).gameObject);
        RouteLayer.Build(map.Terrain, map.Frame, BootRoutes(), RouteLayer.TrainMode.Service, date);
        var service = RouteTests.RoutesRoot(map).Find(ServiceTrain.Name).GetComponent<ServiceTrain>();
        return (map, service);
    }

    [Test]
    public void TheRuntimeFileCarriesTheSourcedService()
    {
        var jjr = BootRoutes().Single(r => r.Id == RouteLayer.TrainRoute);
        Assert.IsNotNull(jjr.Service, "content/map/routes_1948.yaml's service reaches the map through the runtime file");
        Assert.AreEqual("1950-03-02", jjr.Service.From);
        Assert.AreEqual(Lod, (jjr.Service.StartLon, jjr.Service.StartLat), "Tel Aviv South to Lod is gated and not signed off");
        Assert.AreEqual(Jerusalem, (jjr.Service.EndLon, jjr.Service.EndLat));
        Assert.AreEqual(1, BootRoutes().Count(r => r.Service != null), "one train (#668)");
        Assert.IsNull(RoutePaths.Parse(Inputs().RoutesYaml, Bounds()).Single(r => r.Id == RouteLayer.TrainRoute).Service,
            "the YAML fallback carries no service: then no train runs");
    }

    [Test]
    public void ABadServiceDropsOnlyTheTrain()
    {
        var inputs = Inputs();
        int count = BootRoutes().Count;
        foreach (var (bad, why) in new[]
        {
            ("1950-3-2", "not a date"), ("1950", "a year, not a day"), ("1900-01-01", "before the route's window"),
        })
        {
            var warnings = new List<string>();
            var tsv = Rebody(inputs.RoutesTsv.Replace("\t1950-03-02\t", $"\t{bad}\t"));
            var routes = RoutePaths.ReadRuntimeOrYaml(tsv, inputs.RoutesYaml, Bounds(), warnings.Add);
            Assert.AreEqual(count, routes.Count, why);
            Assert.IsNull(routes.Single(r => r.Id == RouteLayer.TrainRoute).Service, why);
            Assert.AreEqual(1, warnings.Count, why);
            StringAssert.Contains("no train runs on it", warnings[0], why);
        }
        var orphan = Rebody(inputs.RoutesTsv.Replace("service\tjaffa_jerusalem_railway\t", "service\tno_such_route\t"));
        var e = Assert.Throws<InvalidDataException>(() => RoutePaths.ParseRuntime(orphan, inputs.RoutesYaml, Bounds()));
        StringAssert.Contains("no_such_route", e.Message);
    }

    /// <summary>The runtime file with its body_check recomputed, as the bake writes it after an edit.</summary>
    static string Rebody(string tsv)
    {
        var lines = tsv.TrimEnd('\n').Split('\n').ToList();
        var rows = lines.Where(l => l.StartsWith("route\t") || l.StartsWith("service\t")).ToList();
        lines[lines.Count - 1] = $"meta\tbody_check\t{RoutePaths.BodyCheck(rows)}";
        return string.Join("\n", lines) + "\n";
    }

    [Test]
    public void NormalPlayAsksForTheTrainOnlyFromTheServiceDate()
    {
        Assert.AreEqual(RouteLayer.TrainMode.Service, RouteLayer.ModeFor(playing: true, shot: false, requested: false), "normal play");
        var (map, service) = NormalPlayMap(RouteLayer.DefaultDate);
        try
        {
            Assert.AreEqual(ServiceDate, service.From);
            CollectionAssert.IsEmpty(asked, "the boot (15 May 1948) asks for nothing: no download for the first frame");
            Assert.IsNull(service.Train);
            Assert.IsNull(RouteTests.RoutesRoot(map).GetComponentInChildren<RouteTrain>(true), "no train before the service date");

            RouteLayer.ShowAt(map, ServiceDate.AddDays(-1));
            CollectionAssert.IsEmpty(asked, "1 March 1950: still none");
            RouteLayer.ShowAt(map, ServiceDate);
            Assert.AreEqual(1, asked.Count, "2 March 1950: the model is asked for");
            Assert.AreEqual((ServiceTrain.ModelId, CollectionDetail.Far), (asked[0].id, asked[0].detail));
            Assert.IsNull(service.Train, "nothing drawn until the stream answers (a later frame)");

            LogAssert.Expect(LogType.Log, new Regex("^TRAIN: the steam train runs on jaffa_jerusalem_railway from 1950-03-02"));
            asked[0].done(FarPrefab());
            Assert.IsNotNull(service.Train);
            Assert.IsTrue(service.Train.gameObject.activeInHierarchy);
            Assert.AreEqual(1, service.Train.Cars.Count, "one car: Tal's locomotive and tender");
            Assert.IsNotNull(service.Train.Cars[0].GetComponentInChildren<Renderer>(), "the streamed model is drawn");

            RouteLayer.ShowAt(map, new DateTime(1951, 1, 1));
            Assert.AreEqual(1, asked.Count, "asked for once");
            RouteLayer.ShowAt(map, new DateTime(1949, 1, 1));
            Assert.IsFalse(service.Train.gameObject.activeSelf, "an earlier date (a loaded save) hides it");
            RouteLayer.ShowAt(map, ServiceDate);
            Assert.IsTrue(service.Train.gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void AGameAlreadyPastTheDateAsksAtOnceAndTheStreamDefersIt()
    {
        var (map, service) = NormalPlayMap(new DateTime(1951, 6, 1));
        try
        {
            Assert.AreEqual(1, asked.Count, "a rebuilt map (or a later save) asks when it is built");
            Assert.IsNull(service.Train);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The load gate (owner, 15 October 2026): the model is streamed, never in Resources, and the game asks the
    /// stream for it (which serves nothing before the frame after a request: CollectionStreamTests).</summary>
    [Test]
    public void TheModelIsStreamedNeverInResources()
    {
        Assert.AreEqual(typeof(CollectionStream), saved.Method.DeclaringType, "the game's seam is CollectionStream.Request");
        Assert.AreEqual(nameof(CollectionStream.Request), saved.Method.Name);
        Assert.AreEqual(CollectionCatalog.Vehicles, CollectionCatalog.BundleOf(ServiceTrain.ModelId));
        foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
        {
            var path = CollectionCatalog.PrefabPath(ServiceTrain.ModelId, detail);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
            StringAssert.DoesNotContain("/Resources/", path);
            foreach (var dep in AssetDatabase.GetDependencies(path, true))
                StringAssert.DoesNotContain("/Resources/", dep, $"{path} pulls {dep} into the first download");
        }
        Assert.IsNull(Resources.Load<GameObject>(ServiceTrain.ModelId));
    }

    [Test]
    public void AModelThatDoesNotStreamLeavesNoTrainAndOneWarning()
    {
        var (map, service) = NormalPlayMap(ServiceDate);
        try
        {
            LogAssert.Expect(LogType.Warning, new Regex("^TRAIN: the locomotive \\(vehicle-steam-locomotive-1948-v2\\) did not stream"));
            asked[0].done(null);
            Assert.IsTrue(service.Failed);
            Assert.IsNull(service.Train);
            Assert.IsNull(RouteTests.RoutesRoot(map).GetComponentInChildren<RouteTrain>(true));
            RouteLayer.ShowAt(map, ServiceDate.AddMonths(1));
            Assert.AreEqual(1, asked.Count, "no retry on every turn");
            Assert.IsTrue(RouteTests.RoutesRoot(map).Find(RouteLayer.RibbonsName).GetComponentsInChildren<MeshRenderer>().Length > 0,
                "the railways and roads are still drawn");
            LogAssert.NoUnexpectedReceived();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void AModelArrivingAfterTheMapWasRebuiltIsDropped()
    {
        var (map, service) = NormalPlayMap(ServiceDate);
        try
        {
            Object.DestroyImmediate(RouteTests.RoutesRoot(map).gameObject);
            Assert.DoesNotThrow(() => asked[0].done(FarPrefab()));
            Assert.IsNull(Object.FindFirstObjectByType<RouteTrain>());
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A normal-play map at the service date with the model streamed in (the far prefab).</summary>
    (MapScene map, RouteTrain train) RunningTrain(List<RoutePath> routes = null)
    {
        var map = RouteTests.BuildMap();
        Object.DestroyImmediate(RouteTests.RoutesRoot(map).gameObject);
        RouteLayer.Build(map.Terrain, map.Frame, routes ?? BootRoutes(), RouteLayer.TrainMode.Service, ServiceDate);
        Assert.AreEqual(1, asked.Count);
        asked[0].done(FarPrefab());
        var service = RouteTests.RoutesRoot(map).Find(ServiceTrain.Name).GetComponent<ServiceTrain>();
        Assert.IsNotNull(service.Train);
        return (map, service.Train);
    }

    static Vector2 W(MapScene map, (double lon, double lat) p) { var v = map.Frame.ToWorld(p.lon, p.lat, 0); return new Vector2(v.x, v.z); }

    static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);

    /// <summary>The coordinator's gate for #668: Tel Aviv South to Lod rests on Wikipedia alone, so until a person signs
    /// it off the train runs from Lod and never on that stretch...</summary>
    [Test]
    public void TheGatedStretchDrawsNoTrainUntilSignedOff()
    {
        var (map, train) = RunningTrain();
        try
        {
            var (start, end) = train.Stretch;
            Assert.Less((Flat(train.At(start)) - W(map, Lod)).magnitude, 1000f, "the train starts at Lod");
            float lodToTelAvivSouth = (W(map, Lod) - W(map, TelAvivSouth)).magnitude;
            float leg = (end - start - train.Length) / RouteTrain.SpeedMps + RouteTrain.DwellSeconds;
            for (float t = 0f; t < 2f * leg; t += 2f)
            {
                var (h, d) = train.HeadAt(t); train.Place(h, d);
                var c = train.Cars[0];
                foreach (var tip in new[] { c.position + c.forward * train.Length / 2f, c.position - c.forward * train.Length / 2f })
                    Assert.Greater((Flat(tip) - W(map, TelAvivSouth)).magnitude, lodToTelAvivSouth - 1500f,
                        $"at {t:0} s the train reaches onto Tel Aviv South to Lod");
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>... and once the content is signed off (the bake then writes the span from Tel Aviv South:
    /// tools/tests/test_route_paths_bake.py), the same map code runs it from there.</summary>
    [Test]
    public void OnceSignedOffTheGatedStretchDraws()
    {
        var inputs = Inputs();
        var signed = Rebody(inputs.RoutesTsv.Replace("\t1950-03-02\t34.87556 31.94583\t", "\t1950-03-02\t34.777 32.0636\t"));
        Assert.AreNotEqual(inputs.RoutesTsv, signed);
        var warnings = new List<string>();
        var routes = RoutePaths.ReadRuntimeOrYaml(signed, inputs.RoutesYaml, Bounds(), warnings.Add);
        CollectionAssert.IsEmpty(warnings);
        var (map, train) = RunningTrain(routes);
        try
        {
            var (start, end) = train.Stretch;
            Assert.Less((Flat(train.At(start)) - W(map, TelAvivSouth)).magnitude, 1000f, "from Tel Aviv South");
            Assert.Less((Flat(train.At(end)) - W(map, Jerusalem)).magnitude, 1000f, "to Jerusalem");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    static float DistanceToLine(Vector2 p, List<Vector2> line)
    {
        float best = float.MaxValue;
        for (int i = 1; i < line.Count; i++)
        {
            var a = line[i - 1]; var ab = line[i] - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            best = Mathf.Min(best, (a + ab * t - p).magnitude);
        }
        return best;
    }

    [Test]
    public void TheTrainRunsOnTheRailwayFromLodToJerusalem()
    {
        var (map, train) = RunningTrain();
        try
        {
            var line = RouteLayer.World(map.Frame, BootRoutes().Single(r => r.Id == RouteLayer.TrainRoute));
            var (start, end) = train.Stretch;
            Assert.Less((Flat(train.At(start)) - W(map, Lod)).magnitude, 1000f, "it starts at Lod");
            Assert.Less((Flat(train.At(end)) - W(map, Jerusalem)).magnitude, 1000f, "and ends at Jerusalem");
            Assert.That(end - start, Is.InRange(50000f, 80000f), "Lod to Jerusalem by the Sorek valley, about 65 km of the line");
            Assert.AreEqual(RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize, train.Length, 1f, "the showcase's locomotive and tender, at ModelSize");

            // Over a whole run there and back, the car's middle is always on the line, and it passes Lod.
            float leg = (end - start - train.Length) / RouteTrain.SpeedMps + RouteTrain.DwellSeconds;
            int samples = 0; float nearLod = float.MaxValue, worst = 0f;
            for (float t = 0f; t < 2f * leg; t += 2f)
            {
                var (h, d) = train.HeadAt(t); train.Place(h, d);
                var c = train.Cars[0].position;
                worst = Mathf.Max(worst, DistanceToLine(new Vector2(c.x, c.z), line));
                nearLod = Mathf.Min(nearLod, (Flat(c) - W(map, Lod)).magnitude);
                samples++;
            }
            TestContext.WriteLine($"{samples} samples over {2f * leg:0} s; the car's middle at most {worst:0} m off the line; nearest Lod {nearLod:0} m");
            Assert.Greater(samples, 200);
            Assert.Less(worst, 60f, "on the railway (to the 50 m table)");
            Assert.Less(nearLod, train.Length, "it reaches Lod");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The coordinator's check for #668: on the Sorek valley's climb to Jerusalem the train's rigid body, about
    /// 2 km on the map, never dips into the ground, along its length or at its sides. Sampled finer than the lift reads
    /// it (RouteTrain.BodyLift), at every 25 m of the head from Lod to Jerusalem, both ways; the tolerance is
    /// <see cref="BaseToleranceM"/>.</summary>
    [Test]
    public void TheTrainsBaseStaysAboveTheGroundOnTheSorekGrade()
    {
        var (map, train) = RunningTrain();
        try
        {
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            float lodS = train.Nearest(new Vector2(map.Frame.ToWorld(Lod.lon, Lod.lat, 0).x, map.Frame.ToWorld(Lod.lon, Lod.lat, 0).z));
            var (start, end) = train.Stretch;
            float west = map.Frame.ToWorld(35.00, 31.75, 0).x, east = map.Frame.ToWorld(35.13, 31.75, 0).x;   // Artuf to Battir
            var car = train.Cars[0];
            float half = train.Length / 2f, halfWidth = train.CarHalfWidth;
            int placements = 0, onGrade = 0; float worst = float.MaxValue, maxLift = 0f;
            foreach (int dir in new[] { 1, -1 })
                for (float c = Mathf.Max(lodS, start + half); c <= end - half; c += 25f)   // the car's middle, Lod to Jerusalem
                {
                    train.Place(c + dir * half, dir);
                    var mid = car.position;
                    placements++;
                    if (mid.x >= west && mid.x <= east) onGrade++;
                    maxLift = Mathf.Max(maxLift, mid.y - train.At(c).y);
                    for (int i = -40; i <= 40; i++)
                        for (int j = -4; j <= 4; j++)
                        {
                            var p = mid + car.forward * (half * i / 40f) + car.right * (halfWidth * j / 4f);
                            worst = Mathf.Min(worst, p.y - RouteLayer.Ground(data, origin, new Vector2(p.x, p.z)));
                        }
                }
            TestContext.WriteLine($"{placements} placements from Lod to Jerusalem, {onGrade} on the Artuf-Battir grade; " +
                $"the base's least height over the ground {worst:0.0} m; the most the body was lifted off the line {maxLift:0} m");
            Assert.Greater(onGrade, 200, "non-vacuous: hundreds of placements on the grade itself");
            Assert.GreaterOrEqual(worst, -BaseToleranceM, "the base never sinks into the ground");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
    /// <summary>How far (m) the base may dip under the ground between the lift's own samples (RouteTrain.LiftSamplesAlong and
    /// Across: 41 along by 5 across, about 20 m by 75 m on the 816 m by 298 m body, against this test's 81 by 9): 5 m, under
    /// the 20 m the rail ribbon itself stands over the ground (RouteLayer.ClearanceM).</summary>
    const float BaseToleranceM = 5f;

    [Test]
    public void TheEditorsShowcaseIsUnchanged()
    {
        var map = RouteTests.BuildMap();
        try
        {
            CollectionAssert.IsEmpty(asked, "the editor build and ?shot show the promo's train and stream nothing");
            Assert.IsNull(RouteTests.RoutesRoot(map).Find(ServiceTrain.Name));
            Assert.AreEqual(2 + TrainModels.Coaches, RouteTests.RoutesRoot(map).GetComponentInChildren<RouteTrain>().Cars.Count);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
}
