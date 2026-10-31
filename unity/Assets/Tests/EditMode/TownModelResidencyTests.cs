#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.UI;

/// <summary>C89/C91: original artwork near the camera focus, with fallback elsewhere. Reflection keeps the test-only
/// revision compilable on the pre-change tree so the requested deferred red run can fail on behavior/API assertions.</summary>
/// <remarks>That compatibility claim covers the original reflection-based residency tests only. The later footprint
/// admission tests intentionally call their new internal production APIs directly and are compile-time reds pre-change.</remarks>
public class TownModelResidencyTests
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static object? Call(object? target, string name, params object?[] args)
    {
        var method = typeof(TownModelLayer).GetMethod(name, Flags);
        Assert.IsNotNull(method, "option A must implement " + name);
        return method!.Invoke(target, args);
    }
    static (string id, Bounds bounds) Place(string id, float x, float z = 0) =>
        (id, new Bounds(new Vector3(x, 0, z), new Vector3(100, 100, 100)));
    static string[] Focus(IReadOnlyList<(string id, Bounds bounds)> places, Vector3 point, string? previous = null, Func<IReadOnlyList<string>, string, bool>? allowance = null) =>
        (string[])Call(null, "FocusedTowns", places, point, previous, allowance, 1000f, 2000f, 1000f, 2)!;
    static Dictionary<string, (Vector2 centre, float radius, string id)> FootprintLookup(
        IEnumerable<(Vector2 centre, float radius, string id)> footprints) =>
        footprints.ToDictionary(f => f.id, f => f, StringComparer.Ordinal);

    static Bounds InstanceBounds(TownModelLayer layer, string id)
    {
        var corners = new List<Vector3>();
        foreach (var instance in layer.InstancesOf(id)) instance.Corners(corners);
        Assert.IsNotEmpty(corners, id + ": detailed-model instances");
        var bounds = new Bounds(corners[0], Vector3.zero);
        foreach (var corner in corners) bounds.Encapsulate(corner);
        return bounds;
    }

    static Bounds PlaceBounds(TownModelLayer layer, string id)
    {
        var places = (IReadOnlyList<(string id, Bounds bounds)>)typeof(TownModelLayer).GetField("places", Flags)!.GetValue(layer)!;
        return places.Single(place => place.id == id).bounds;
    }

    static Camera CopyCamera(Camera source, string name)
    {
        var camera = new GameObject(name).AddComponent<Camera>();
        camera.CopyFrom(source);
        camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        return camera;
    }

    static Camera GroundCamera(Vector2 focus, float distanceM, string name)
    {
        var camera = new GameObject(name).AddComponent<Camera>();
        camera.transform.position = new Vector3(focus.x, distanceM, focus.y);
        camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        return camera;
    }

    /// <summary>R2: the production camera centres a town on its sourced label point, also the kit-footprint centre. Some
    /// detailed instances do not cover that point; admission must therefore use the authoritative footprint union rather
    /// than treating the detailed-instance rectangle as the town itself.</summary>
    [Test]
    public void EveryDetailedTownIsAdmittedAtItsLabelAndKitFootprintCentre()
    {
        const float ReportedCameraDistanceM = 11_800f;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        try
        {
            var map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
            var layer = map.GetComponentInChildren<TownModelLayer>(true);
            layer.EdgeAllowance = null;
            var rig = UnityEngine.Object.FindFirstObjectByType<CameraRig>();
            var rigCamera = rig.GetComponent<Camera>();
            var detailed = map.Settlements.Where(s => layer.CountFor(s.Id) > 0).OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
            CollectionAssert.IsSubsetOf(new[] { "jaffa", "jerusalem", "nazareth" }, detailed.Select(s => s.Id).ToArray(),
                "the three reported towns must remain in the mapped detailed-model census");

            var outsideInstances = new List<string>();
            var notAdmitted = new List<string>();
            var notReady = new List<string>();
            foreach (var town in detailed)
            {
                var footprint = map.KitFootprints.Single(f => f.id == town.Id);
                var label = MapLabelsPresenter.MarkerTop(map, town);
                Assert.AreEqual(footprint.centre.x, label.x, 0.01f, town.Id + ": label and kit centre x");
                Assert.AreEqual(footprint.centre.y, label.z, 0.01f, town.Id + ": label and kit centre z");

                rig.LookAt = new Vector3(label.x, 0f, label.z); rig.Distance = ReportedCameraDistanceM;
                rig.Yaw = 0f; rig.TopInset = 0f; rig.Apply();
                var camera = CopyCamera(rigCamera, town.Id + " fresh residency camera");
                try
                {
                    Assert.IsTrue(CollectionResidencyView.TryGroundFocus(camera, out var focus), town.Id + ": production ground focus");
                    var instances = InstanceBounds(layer, town.Id);
                    var instancePoint = focus; instancePoint.y = instances.center.y;
                    float instanceDistance = Mathf.Sqrt(instances.SqrDistance(instancePoint));
                    if (instanceDistance > 0f) outsideInstances.Add(town.Id);
                    float kitDistance = Mathf.Max(0f, Vector2.Distance(new Vector2(focus.x, focus.z), footprint.centre) - footprint.radius);
                    TestContext.WriteLine($"R2 {town.Id}: focus={focus}; instance_bounds={instances}; " +
                        $"instance_distance={instanceDistance:0.###}m; " +
                        $"kit_centre={footprint.centre}; kit_radius={footprint.radius:0.###}m; kit_distance={kitDistance:0.###}m");

                    layer.PrepareCamera(camera);
                    if (!layer.WantsTown(town.Id, camera)) notAdmitted.Add(town.Id);
                    if (!layer.ReadyForTown(town.Id, camera)) notReady.Add(town.Id);
                }
                finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); }
            }
            // QA reported Jaffa, Jerusalem and Nazareth; the measured label foci outside their instance rectangles are Acre,
            // Jaffa and Tel Aviv (Jerusalem and Nazareth lie inside theirs, instance_distance=0). The three reported towns are
            // still checked below with every other detailed town.
            CollectionAssert.IsSubsetOf(new[] { "acre", "jaffa", "tel_aviv" }, outsideInstances,
                "the regression evidence must keep covering the measured focus points outside their instance rectangles");
            CollectionAssert.IsEmpty(notAdmitted,
                "a camera focused on each authoritative town footprint must admit that town's detailed models");
            CollectionAssert.IsEmpty(notReady,
                "every admitted production town must have every required detailed-model kind");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VisualToggles.Clear();
        }
    }

    [Test]
    public void PanningFromEveryNestedDetailedTownToItsNeighboursLabelAdmitsTheNeighbour()
    {
        const float ReportedCameraDistanceM = 11_800f;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        try
        {
            var map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
            var layer = map.GetComponentInChildren<TownModelLayer>(true);
            layer.EdgeAllowance = null;
            // Pan targets are the game's towns (map.Settlements, from content): the focus partition is theirs. 1948 localities
            // also have model instances (CountFor > 0) but stand in merged locality tiles, which load with every place in view
            // and swap as one tile (owner ruling, 31 Oct 2026, "Load all in view"; see
            // TownLandmarksTests.ProductionKitWiringNeverTreatsAMixedRendererAsOneNamedTown), so they are not pan targets.
            var gameTowns = map.Settlements.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
            var detailed = map.KitFootprints.Where(f => layer.CountFor(f.id) > 0 && gameTowns.Contains(f.id))
                .OrderBy(f => f.id, StringComparer.Ordinal).ToList();
            var nestedPairs = new List<((Vector2 centre, float radius, string id) from,
                (Vector2 centre, float radius, string id) to)>();
            for (int i = 0; i < detailed.Count; i++) for (int j = i + 1; j < detailed.Count; j++)
            {
                var first = detailed[i]; var second = detailed[j];
                if (Vector2.Distance(first.centre, second.centre) >= Mathf.Max(first.radius, second.radius)) continue;
                if (second.radius > first.radius) (first, second) = (second, first);
                nestedPairs.Add((first, second));
            }
            Assert.IsNotEmpty(nestedPairs, "the real-map census must retain a nested detailed-town pair for this regression");

            foreach (var pair in nestedPairs)
            {
                var camera = GroundCamera(pair.from.centre, ReportedCameraDistanceM,
                    pair.from.id + " to " + pair.to.id + " sequential pan camera");
                try
                {
                    layer.PrepareCamera(camera);
                    Assert.IsTrue(layer.WantsTown(pair.from.id, camera), pair.from.id + ": admitted at its own label before the pan");
                    camera.transform.position = new Vector3(pair.to.centre.x, ReportedCameraDistanceM, pair.to.centre.y);
                    layer.PrepareCamera(camera);
                    Assert.IsTrue(layer.WantsTown(pair.to.id, camera),
                        pair.from.id + " -> " + pair.to.id + ": the normalized-distance owner is admitted at its label");
                    Assert.IsFalse(layer.WantsTown(pair.from.id, camera),
                        pair.from.id + " -> " + pair.to.id + ": the previous town cannot retain on its whole disc");
                }
                finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); }
            }
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VisualToggles.Clear();
        }
    }

    [Test]
    public void FocusOutsideEveryFootprintAndInstanceBoundsAdmitsNothing()
    {
        const float FootprintRadiusM = 500f;
        const float TownSeparationM = 4000f;
        const float CircleAabbCornerShare = 0.8f;
        const float OutsideFocusXM = FootprintRadiusM * CircleAabbCornerShare;
        const float OutsideFocusZM = FootprintRadiusM * CircleAabbCornerShare;
        var places = new[] { Place("jaffa", 0f), Place("tel_aviv", TownSeparationM) };
        var footprints = new[] {
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "jaffa"),
            (centre: new Vector2(TownSeparationM, 0f), radius: FootprintRadiusM, id: "tel_aviv") };
        var selected = TownModelLayer.FocusedTownsWithFootprints(places, FootprintLookup(footprints), footprints,
            new Vector3(OutsideFocusXM, 0f, OutsideFocusZM), null, null, 0f, 0f, 0f, 0);
        CollectionAssert.IsEmpty(selected);
    }

    [Test]
    public void InstanceBoundsStillAdmitOutsideTheKitFootprint()
    {
        const float InstanceCentreXM = 2000f;
        const float FootprintRadiusM = 500f;
        var places = new[] { Place("town", InstanceCentreXM) };
        var footprints = new[] { (centre: Vector2.zero, radius: FootprintRadiusM, id: "town") };
        var selected = TownModelLayer.FocusedTownsWithFootprints(places, FootprintLookup(footprints), footprints,
            new Vector3(InstanceCentreXM, 0f, 0f), null, null, 0f, 0f, 0f, 0);
        CollectionAssert.AreEqual(new[] { "town" }, selected);
    }

    [Test]
    public void NonDetailedFootprintOwnerExcludesADetailedNeighbour()
    {
        const float FootprintRadiusM = 1000f;
        const float DetailedCentreXM = 0f;
        const float NonDetailedCentreXM = 500f;
        const float FocusXM = 400f;
        var places = new[] { Place("detailed", DetailedCentreXM) };
        var footprints = new[] {
            (centre: new Vector2(DetailedCentreXM, 0f), radius: FootprintRadiusM, id: "detailed"),
            (centre: new Vector2(NonDetailedCentreXM, 0f), radius: FootprintRadiusM, id: "non_detailed") };
        var selected = TownModelLayer.FocusedTownsWithFootprints(places,
            new Dictionary<string, (Vector2, float, string)> { ["detailed"] = footprints[0] }, footprints,
            new Vector3(FocusXM, 0f, 0f), null, null, 0f, 0f, 0f, 0);
        CollectionAssert.IsEmpty(selected,
            "the detailed town cannot claim a point which the full kit partition assigns to a non-detailed settlement");
    }

    [Test]
    public void A1948LocalityDiscDoesNotTakeGroundFromAGameSettlement()
    {
        const float FootprintRadiusM = 1000f;
        const float LocalityCentreXM = 500f;
        const float FocusXM = 400f;
        using var fixture = new Fixture();
        var footprints = new[] {
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "a"),
            (centre: new Vector2(LocalityCentreXM, 0f), radius: FootprintRadiusM, id: "1948_locality") };
        fixture.layer.ConfigureTownFootprints(footprints, new[] { "1948_locality" });
        var camera = fixture.Camera(FocusXM); fixture.Prepare(camera);
        Assert.IsTrue(fixture.layer.WantsTown("a", camera),
            "camera ownership must ignore the same locality disc that game-town generation ignores");
    }

    [Test]
    public void AnIgnored1948LocalityDiscDoesNotSuppressInstanceBoundsFallback()
    {
        const float FootprintRadiusM = 500f;
        using var fixture = new Fixture();
        var footprints = new[] {
            (centre: new Vector2(-2000f, 0f), radius: FootprintRadiusM, id: "a"),
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "1948_locality") };
        fixture.layer.ConfigureTownFootprints(footprints, new[] { "1948_locality" });
        var camera = fixture.Camera(); fixture.Prepare(camera);
        Assert.IsTrue(fixture.layer.WantsTown("a", camera),
            "an ignored locality is not a game-settlement partition disc, so instance bounds remain the fallback");
    }

    [Test]
    public void PreviousTownInsideItsInstanceBoundsBeatsAnotherTownsFootprint()
    {
        const float FootprintRadiusM = 500f;
        const float PreviousFootprintXM = -1000f;
        var places = new[] { Place("p", 0f), Place("q", 1000f) };
        var footprints = new[] {
            (centre: new Vector2(PreviousFootprintXM, 0f), radius: FootprintRadiusM, id: "p"),
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "q") };
        var selected = TownModelLayer.FocusedTownsWithFootprints(places, FootprintLookup(footprints), footprints,
            Vector3.zero, "p", null, 0f, 0f, 0f, 0);
        CollectionAssert.AreEqual(new[] { "p" }, selected);
    }

    [Test]
    public void OneCameraTraversesTheJaffaTelAvivSeamInBothDirectionsDeterministically()
    {
        const float ReportedCameraDistanceM = 11_800f;
        const float SeamSampleShareOfCentreDistance = 0.02f;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        try
        {
            var map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
            var layer = map.GetComponentInChildren<TownModelLayer>(true);
            layer.EdgeAllowance = null;
            Assert.Greater(layer.CountFor("jaffa"), 0, "Jaffa remains a detailed town");
            Assert.Greater(layer.CountFor("tel_aviv"), 0, "Tel Aviv remains a detailed town");
            var footprints = map.KitFootprints;
            var ignoredLocalities = new HashSet<string>(map.Localities.Select(place => place.Settlement.Id), StringComparer.Ordinal);
            float focusExitM = ProductionResidency.Approved().Towns.Exit;
            var jaffa = footprints.Single(f => f.id == "jaffa");
            var telAviv = footprints.Single(f => f.id == "tel_aviv");
            var between = telAviv.centre - jaffa.centre;
            var seam = jaffa.centre + between * (jaffa.radius / (jaffa.radius + telAviv.radius));
            var offset = between * SeamSampleShareOfCentreDistance;
            var jaffaSide = seam - offset;
            var telAvivSide = seam + offset;
            float BoundsDistance(Bounds bounds, Vector2 point)
            {
                var point3 = new Vector3(point.x, bounds.center.y, point.y);
                return Mathf.Sqrt(bounds.SqrDistance(point3));
            }
            bool Owns((Vector2 centre, float radius, string id) footprint, Vector2 point) =>
                SettlementKits.NormalizedFootprintDistance(point, footprint.centre, footprint.radius)
                    <= SettlementKits.FootprintBoundary
                && SettlementKits.OwnsFootprint(point, footprint.id, footprint.centre, footprint.radius, footprints,
                    ignored: ignoredLocalities);
            void AssertOwnershipMechanism((Vector2 centre, float radius, string id) owner,
                (Vector2 centre, float radius, string id) previous, Bounds previousBounds, Vector2 point)
            {
                Assert.IsTrue(Owns(owner, point), owner.id + ": normalized-distance ownership admits this seam sample");
                Assert.IsFalse(Owns(previous, point), previous.id + ": normalized-distance ownership does not retain this sample");
                Assert.LessOrEqual(SettlementKits.NormalizedFootprintDistance(point, previous.centre, previous.radius),
                    SettlementKits.FootprintBoundary, previous.id + ": sample remains inside the previous whole disc");
                Assert.Greater(BoundsDistance(previousBounds, point), focusExitM,
                    previous.id + ": previous instance-bounds exit band does not retain this sample");
            }
            var jaffaBounds = PlaceBounds(layer, "jaffa");
            var telAvivBounds = PlaceBounds(layer, "tel_aviv");
            AssertOwnershipMechanism(jaffa, telAviv, telAvivBounds, jaffaSide);
            AssertOwnershipMechanism(telAviv, jaffa, jaffaBounds, telAvivSide);

            var camera = GroundCamera(jaffaSide, ReportedCameraDistanceM, "Jaffa Tel Aviv seam camera");
            void Move(Vector2 point)
            {
                camera.transform.position = new Vector3(point.x, ReportedCameraDistanceM, point.y);
                layer.PrepareCamera(camera);
            }
            try
            {
                Move(jaffaSide);
                Assert.IsTrue(layer.WantsTown("jaffa", camera)); Assert.IsFalse(layer.WantsTown("tel_aviv", camera));
                Move(telAvivSide);
                Assert.IsFalse(layer.WantsTown("jaffa", camera)); Assert.IsTrue(layer.WantsTown("tel_aviv", camera));
                Move(jaffaSide);
                Assert.IsFalse(layer.WantsTown("tel_aviv", camera)); Assert.IsTrue(layer.WantsTown("jaffa", camera));
            }
            finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); }
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VisualToggles.Clear();
        }
    }

    [TestCase("FocusEnterM", 0f)]
    [TestCase("FocusExitM", 0f)]
    [TestCase("EdgeM", 0f)]
    [TestCase("MaxEdgeTowns", 0f)]
    [TestCase("CameraGraceFrames", 1f)]
    public void SpatialNumbersAreNamedAndTunable(string name, float expected)
    {
        Assert.IsNull(typeof(TownModelLayer).GetField(name, Flags), name + ": no provisional alternative to the approved policy");
        var policy = ProductionResidency.Approved().Towns;
        float actual = name switch {
            "FocusEnterM" => policy.Enter, "FocusExitM" => policy.Exit, "EdgeM" => policy.Edge,
            "MaxEdgeTowns" => policy.MaxEdges, "CameraGraceFrames" => policy.Grace,
            _ => throw new ArgumentException(name)
        };
        Assert.AreEqual(expected, actual);
        Assert.AreEqual(120000f, policy.ViewEnter);
        Assert.AreEqual(160000f, policy.ViewExit);
    }

    [Test]
    public void FocusKeepsOnlyThePrimaryAndTwoNearestEdgeTowns()
    {
        var places = new[] { Place("far", 6000), Place("edge-c", 800), Place("focus", 0), Place("edge-b", 500), Place("edge-a", 300) };
        CollectionAssert.AreEqual(new[] { "focus", "edge-a", "edge-b" }, Focus(places, Vector3.zero, allowance: (_, __) => true));
    }
    /// <summary>Regression guard: a malformed negative edge allowance is clamped to the primary town only.</summary>
    [Test]
    public void NegativeMaximumEdgesSafelyKeepsOnlyThePrimary()
    {
        var places = new[] { Place("focus", 0), Place("edge", 300) };
        CollectionAssert.AreEqual(new[] { "focus" }, TownModelLayer.FocusedTowns(places, Vector3.zero, null,
            (_, __) => true, 1000f, 2000f, 1000f, -1));
    }
    [Test]
    public void EqualDistancesUseOrdinalTownIdRegardlessOfInputOrder()
    {
        var places = new[] { Place("z", 200), Place("a", -200) };
        Assert.AreEqual("a", Focus(places, Vector3.zero)[0]);
        Array.Reverse(places);
        Assert.AreEqual("a", Focus(places, Vector3.zero)[0]);
    }
    [Test]
    public void WaterOrAnEmptyMapRequestsNoNearTown()
    {
        CollectionAssert.IsEmpty(Focus(new[] { Place("town", 0) }, new Vector3(5000, 0, 0)));
        CollectionAssert.IsEmpty(Focus(Array.Empty<(string, Bounds)>(), Vector3.zero));
    }
    [Test]
    public void FocusRetentionHasASeparateExitBand()
    {
        var places = new[] { Place("old", 0), Place("new", 1800) };
        Assert.AreEqual("old", Focus(places, new Vector3(1500, 0, 0), "old")[0]);
        Assert.AreEqual("new", Focus(places, new Vector3(2200, 0, 0), "old")[0]);
    }
    [Test]
    public void AltitudeDoesNotChangeGroundMembership()
    {
        var places = new[] { Place("focus", 0), Place("edge", 500) };
        CollectionAssert.AreEqual(Focus(places, Vector3.zero), Focus(places, new Vector3(0, 100000, 0)));
    }

    sealed class Release : IDisposable
    {
        Action? action;
        public Release(Action action) { this.action = action; }
        public void Dispose() { var a = action; action = null; a?.Invoke(); }
    }

    [Test]
    public void LoadingKeepsFallbackAndReadinessSwapsOnlyTheFocusedTown()
    {
        var field = typeof(TownModelLayer).GetField("ModelAcquire", Flags);
        Assert.IsNotNull(field, "stream injection for delayed arrival");
        var before = field!.GetValue(null);
        var root = new GameObject("option A test"); var cameraObject = new GameObject("camera");
        var prefab = new GameObject("original"); var mesh = new Mesh();
        var material = new Material(Shader.Find("Hidden/InternalErrorShader"));
        Action<GameObject?>? answer = null; int released = 0;
        var requested = new List<string>();
        try
        {
            field.SetValue(null, new Func<string, Statecraft.Game.Map.Collection.CollectionDetail, Action<GameObject?>, IDisposable>((id, _, done) =>
            { requested.Add(id); answer = done; return new Release(() => released++); }));
            var layer = root.AddComponent<TownModelLayer>();
            Call(layer, "ConfigureResidency", 1000f, 2000f, 1000f, 2, "test fixture only; not measured");
            var kit = new GameObject("focus").AddComponent<MeshRenderer>(); kit.transform.SetParent(root.transform);
            var other = new GameObject("other").AddComponent<MeshRenderer>(); other.transform.SetParent(root.transform);
            var items = new[] {
                new SettlementKits.ModelInstance("focus", TownModel.B01WhiteCity, Matrix4x4.identity, Vector2.zero, 0, 100, 100, 0, 100),
                new SettlementKits.ModelInstance("other", TownModel.B03aArches, Matrix4x4.identity, new Vector2(6000, 0), 0, 100, 100, 0, 100) };
            Call(layer, "InitStreaming", items, new List<(Renderer kit, Renderer? close)> { (kit, null), (other, null) });
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 10000, -10000); camera.transform.LookAt(Vector3.zero);
            Call(layer, "PrepareCamera", camera);
            CollectionAssert.AreEqual(new[] { "b01-white-city-v2" }, requested);
            Assert.IsFalse(kit.forceRenderingOff, "fallback while loading");
            Assert.IsFalse(other.forceRenderingOff);
            Assert.IsFalse((bool)Call(layer, "ReadyForTown", "focus", camera)!);
            prefab.AddComponent<MeshFilter>().sharedMesh = mesh; prefab.AddComponent<MeshRenderer>().sharedMaterial = material;
            answer!(prefab);
            Call(layer, "RestoreRenderers"); Call(layer, "PrepareCamera", camera);
            Assert.IsTrue((bool)Call(layer, "ReadyForTown", "focus", camera)!);
            Assert.IsTrue(kit.forceRenderingOff);
            Assert.IsFalse(other.forceRenderingOff, "not all towns hide behind global HasModels");
            Call(layer, "RestoreRenderers");
            camera.transform.position = new Vector3(0, 100000, -100000); camera.transform.LookAt(Vector3.zero);
            Call(layer, "PrepareCamera", camera);
            Assert.IsFalse(kit.forceRenderingOff, "fallback before original release");
            Call(layer, "RetireReleasedModels");
            Assert.AreEqual(1, released);
        }
        finally
        {
            field.SetValue(null, before);
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material);
        }
    }
    sealed class Fixture : IDisposable
    {
        readonly FieldInfo field;
        readonly object? previous;
        readonly GameObject root;
        readonly GameObject prefab;
        readonly Mesh mesh;
        readonly Material material;
        readonly List<GameObject> cameras = new List<GameObject>();
        public readonly TownModelLayer layer;
        public readonly MeshRenderer a, b;
        public readonly List<string> RequestOrder = new List<string>();
        public readonly Dictionary<string, Action<GameObject?>> Answers = new Dictionary<string, Action<GameObject?>>();
        public readonly Dictionary<string, int> Requests = new Dictionary<string, int>(), Releases = new Dictionary<string, int>();
        public Fixture(bool mixedTile = false, float otherX = 8000, bool configurePolicy = true)
        {
            field = typeof(TownModelLayer).GetField("ModelAcquire", Flags)!;
            Assert.IsNotNull(field); previous = field.GetValue(null);
            root = new GameObject("town residency fixture"); prefab = new GameObject("original"); mesh = new Mesh();
            material = new Material(Shader.Find("Hidden/InternalErrorShader"));
            field.SetValue(null, new Func<string, Statecraft.Game.Map.Collection.CollectionDetail, Action<GameObject?>, IDisposable>((id, _, done) =>
            {
                Requests.TryGetValue(id, out int n); Requests[id] = n + 1; Answers[id] = done; RequestOrder.Add(id);
                return new Release(() => { Releases.TryGetValue(id, out int count); Releases[id] = count + 1; });
            }));
            prefab.AddComponent<MeshFilter>().sharedMesh = mesh; prefab.AddComponent<MeshRenderer>().sharedMaterial = material;
            layer = root.AddComponent<TownModelLayer>();
            a = new GameObject(mixedTile ? "Localities 0,0" : "a").AddComponent<MeshRenderer>(); a.transform.SetParent(root.transform);
            b = new GameObject("b").AddComponent<MeshRenderer>(); b.transform.SetParent(root.transform);
            var items = new[] {
                new SettlementKits.ModelInstance("a", TownModel.B01WhiteCity, Matrix4x4.identity, Vector2.zero, 0, 100, 100, 0, 100),
                new SettlementKits.ModelInstance("b", TownModel.B03aArches, Matrix4x4.identity, new Vector2(otherX, 0), 0, 100, 100, 0, 100) };
            if (configurePolicy)
            {
                Call(layer, "ConfigureResidency", 1000f, 2000f, 1000f, 2, "test fixture only; not measured");
                // Private-field reflection makes ONLY this fixture's historical grace2 explicit; leave its view path unchanged.
                typeof(TownModelLayer).GetField("cameraGraceFrames", Flags)!.SetValue(layer, 2);
            }
            Call(layer, "InitStreaming", items, new List<(Renderer kit, Renderer? close)> { (a, null), (b, null) });
            layer.EdgeAllowance = null;
        }
        public GameObject Original => prefab;
        public Camera Camera(float x = 0, bool far = false)
        {
            var go = new GameObject("camera"); cameras.Add(go); var c = go.AddComponent<Camera>();
            c.transform.position = new Vector3(x, far ? 100000 : 10000, far ? -100000 : -10000); c.transform.LookAt(new Vector3(x, 0, 0)); return c;
        }
        public void Prepare(Camera camera) => Call(layer, "PrepareCamera", camera);
        public bool Ready(string id, Camera camera) => (bool)Call(layer, "ReadyForTown", id, camera)!;
        public void Arrive(string model) => Answers[model](prefab);
        public void Retire() => Call(layer, "RetireReleasedModels");
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var c in cameras) UnityEngine.Object.DestroyImmediate(c);
            field.SetValue(null, previous);
            UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void ConfigureWholeTownsBeforeOrAfterFootprintsProducesTheSameAdmissionOutcomes()
    {
        const float FootprintRadiusM = 500f;
        const float WholeTownXM = 12_000f;
        const float FocusOffsetM = 200f;
        const float FocusXM = WholeTownXM + FocusOffsetM;
        var footprints = new[] {
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "a"),
            (centre: new Vector2(WholeTownXM, 0f), radius: FootprintRadiusM, id: "whole") };
        var whole = new[] { (id: "whole", bounds: new Bounds(new Vector3(WholeTownXM, 0f, 0f), Vector3.one)) };
        using var footprintsFirst = new Fixture();
        using var wholeTownsFirst = new Fixture();

        footprintsFirst.layer.ConfigureTownFootprints(footprints);
        footprintsFirst.layer.ConfigureWholeTowns(whole);

        wholeTownsFirst.layer.ConfigureWholeTowns(whole);
        wholeTownsFirst.layer.ConfigureTownFootprints(footprints);

        var firstCamera = footprintsFirst.Camera(FocusXM);
        var secondCamera = wholeTownsFirst.Camera(FocusXM);
        footprintsFirst.Prepare(firstCamera); wholeTownsFirst.Prepare(secondCamera);
        Assert.IsTrue(footprintsFirst.layer.WantsTown("whole", firstCamera));
        Assert.IsTrue(wholeTownsFirst.layer.WantsTown("whole", secondCamera));
        Assert.IsFalse(footprintsFirst.layer.WantsTown("a", firstCamera));
        Assert.IsFalse(wholeTownsFirst.layer.WantsTown("a", secondCamera));
    }

    [Test]
    public void ConfigureTownFootprintsLogsAndDropsDuplicateDetailedTownIds()
    {
        const float FootprintRadiusM = 500f;
        using var fixture = new Fixture();
        var duplicate = new[] {
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "a"),
            (centre: Vector2.right, radius: FootprintRadiusM, id: "a") };
        LogAssert.Expect(LogType.Error,
            "Detailed town footprint id 'a' occurs more than once; dropping its footprint and using instance bounds.");
        Assert.DoesNotThrow(() => fixture.layer.ConfigureTownFootprints(duplicate));
        Assert.DoesNotThrow(() => fixture.layer.ConfigureWholeTowns(Array.Empty<(string id, Bounds bounds)>()));
        Assert.DoesNotThrow(() => fixture.layer.ConfigureWholeTowns(Array.Empty<(string id, Bounds bounds)>()));
        LogAssert.NoUnexpectedReceived();
        var camera = fixture.Camera(); fixture.Prepare(camera);
        Assert.IsTrue(fixture.layer.WantsTown("a", camera), "the dropped footprint falls back to the detailed instance bounds");
    }

    [Test]
    public void ConfigureWholeTownsPreservesAPrimaryThatRemainsConfigured()
    {
        const float FootprintRadiusM = 1000f;
        const float OtherTownXM = 800f;
        const float OtherOwnedFocusXM = 600f;
        using var fixture = new Fixture(otherX: OtherTownXM);
        fixture.layer.ConfigureTownFootprints(new[] {
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "a"),
            (centre: new Vector2(OtherTownXM, 0f), radius: FootprintRadiusM, id: "b") });
        var camera = fixture.Camera(); fixture.Prepare(camera);
        Assert.IsTrue(fixture.layer.WantsTown("a", camera));

        camera.transform.position = new Vector3(OtherOwnedFocusXM, camera.transform.position.y, camera.transform.position.z);
        camera.transform.LookAt(new Vector3(OtherOwnedFocusXM, 0f, 0f));
        fixture.layer.ConfigureWholeTowns(Array.Empty<(string id, Bounds bounds)>());
        fixture.Prepare(camera);
        Assert.IsTrue(fixture.layer.WantsTown("a", camera),
            "whole-town streaming must preserve valid instance-bounds hysteresis");
        Assert.IsFalse(fixture.layer.WantsTown("b", camera));
    }

    [Test]
    public void ConfigureTownFootprintsKeepsEveryDiscForARepeatedNonDetailedCampId()
    {
        const float FootprintRadiusM = 1000f;
        const float FirstCampXM = 5000f;
        const float SecondCampXM = 200f;
        using var fixture = new Fixture();
        var footprints = new[] {
            (centre: Vector2.zero, radius: FootprintRadiusM, id: "a"),
            (centre: new Vector2(FirstCampXM, 0f), radius: FootprintRadiusM, id: "camp:subdistrict"),
            (centre: new Vector2(SecondCampXM, 0f), radius: FootprintRadiusM, id: "camp:subdistrict") };
        Assert.DoesNotThrow(() => fixture.layer.ConfigureTownFootprints(footprints));
        var camera = fixture.Camera(SecondCampXM); fixture.Prepare(camera);
        Assert.IsFalse(fixture.layer.WantsTown("a", camera)); Assert.IsFalse(fixture.layer.WantsTown("b", camera));
    }

    [Test]
    public void ReconfiguringFootprintsClearsThePreviousPrimarySelection()
    {
        const float FootprintRadiusM = 500f;
        const float FocusXM = 1000f;
        using var fixture = new Fixture();
        var first = new[] {
            (centre: new Vector2(FocusXM, 0f), radius: FootprintRadiusM, id: "a"),
            (centre: new Vector2(8000f, 0f), radius: FootprintRadiusM, id: "b") };
        fixture.layer.ConfigureTownFootprints(first);
        var camera = fixture.Camera(FocusXM); fixture.Prepare(camera);
        Assert.IsTrue(fixture.layer.WantsTown("a", camera));

        var second = new[] {
            (centre: new Vector2(-8000f, 0f), radius: FootprintRadiusM, id: "a"),
            (centre: new Vector2(FocusXM, 0f), radius: FootprintRadiusM, id: "b") };
        fixture.layer.ConfigureTownFootprints(second);
        fixture.Prepare(camera);
        Assert.IsFalse(fixture.layer.WantsTown("a", camera));
        Assert.IsTrue(fixture.layer.WantsTown("b", camera));
    }

    [Test]
    public void AnotherCameraKeepsItsTownWhenTheFirstCameraDisappears()
    {
        using var f = new Fixture(); var a = f.Camera(); var b = f.Camera(8000);
        f.Prepare(a); f.Arrive("b01-white-city-v2"); f.Prepare(a);
        f.Prepare(b); f.Arrive("b03a-jerusalem-arches-v2"); f.Prepare(b);
        Assert.IsTrue(f.Ready("a", a)); Assert.IsTrue(f.Ready("b", b));
        a.enabled = false; f.Prepare(b); f.Retire();
        Assert.AreEqual(1, f.Releases["b01-white-city-v2"]);
        Assert.IsFalse(f.Releases.ContainsKey("b03a-jerusalem-arches-v2")); Assert.IsTrue(f.Ready("b", b));
        Assert.IsFalse(f.a.forceRenderingOff, "inactive camera cannot leave its fallback hidden");
    }
    [Test]
    public void AnEvictedGenerationCannotAnswerANewRequestForTheSameModel()
    {
        using var f = new Fixture(); var near = f.Camera(); f.Prepare(near);
        var stale = f.Answers["b01-white-city-v2"];
        near.transform.position = new Vector3(0, 100000, -100000); near.transform.LookAt(Vector3.zero);
        f.Prepare(near); f.Retire();
        near.transform.position = new Vector3(0, 10000, -10000); near.transform.LookAt(Vector3.zero); f.Prepare(near);
        Assert.AreEqual(2, f.Requests["b01-white-city-v2"]);
        stale(f.Original); Assert.IsFalse(f.Ready("a", near));
        f.Arrive("b01-white-city-v2"); f.Prepare(near); Assert.IsTrue(f.Ready("a", near));
    }
    [Test]
    public void AMixedLocalityTileStaysWholeAsFallback()
    {
        using var f = new Fixture(mixedTile: true); var camera = f.Camera(); f.Prepare(camera);
        Assert.IsFalse(f.Requests.ContainsKey("b01-white-city-v2"));
        Assert.IsFalse(f.a.forceRenderingOff); Assert.IsFalse(f.Ready("a", camera));
    }
    [Test]
    public void AFailedOriginalKeepsFallbackWithoutRepeatedRequestsEveryCameraFrame()
    {
        using var f = new Fixture(); var camera = f.Camera(); f.Prepare(camera);
        f.Answers["b01-white-city-v2"](null); f.Prepare(camera); f.Prepare(camera);
        Assert.IsFalse(f.Ready("a", camera)); Assert.IsFalse(f.a.forceRenderingOff);
        Assert.AreEqual(1, f.Requests["b01-white-city-v2"]);
    }
    [Test]
    public void DisablingTheLayerReleasesItsOwnershipAndRestoresFallback()
    {
        using var f = new Fixture(); var camera = f.Camera(); f.Prepare(camera); f.Arrive("b01-white-city-v2"); f.Prepare(camera);
        Assert.IsTrue(f.a.forceRenderingOff);
        f.layer.enabled = false;
        Assert.IsFalse(f.a.forceRenderingOff); Assert.AreEqual(1, f.Releases["b01-white-city-v2"]);
    }

    [Test]
    public void TheFocusLoadsBeforeEdgeModelsRatherThanEnumOrder()
    {
        using var f = new Fixture(otherX: 600);
        typeof(TownModelLayer).GetField("EdgeAllowance", Flags)!.SetValue(f.layer,
            new Func<IReadOnlyList<string>, string, bool>((_, __) => true));
        var camera = f.Camera(600); f.Prepare(camera);
        CollectionAssert.AreEqual(new[] { "b03a-jerusalem-arches-v2", "b01-white-city-v2" }, f.RequestOrder);
    }


    [Test]
    public void EdgesWaitForADiagnosticAllowanceAndRejectUnaffordableNeighbours()
    {
        var towns = new[] { Place("focus", 0), Place("costly", 200), Place("cheap", 300) };
        CollectionAssert.AreEqual(new[] { "focus" }, Focus(towns, Vector3.zero));
        CollectionAssert.AreEqual(new[] { "focus", "cheap" }, Focus(towns, Vector3.zero,
            allowance: (resident, candidate) => resident.Count == 1 && candidate == "cheap"));
    }
    [Test]
    public void OneReadyTownDoesNotClaimTheEntireOriginalCatalogIsComplete()
    {
        using (var f = new Fixture())
        {
            var camera = f.Camera(); f.Prepare(camera); f.Arrive("b01-white-city-v2");
            Assert.IsTrue(f.Ready("a", camera)); Assert.IsFalse(f.layer.HasModels);
        }
    }
    [Test]
    public void ACameraThatStopsRenderingExpiresAfterTheNamedFrameGrace()
    {
        using (var f = new Fixture())
        {
            var camera = f.Camera(); f.Prepare(camera); f.Arrive("b01-white-city-v2");
            int start = Time.frameCount;
            Call(f.layer, "AdvanceCameraFrame", start + 2); f.Retire();
            Assert.IsFalse(f.Releases.ContainsKey("b01-white-city-v2"));
            Call(f.layer, "AdvanceCameraFrame", start + 3); f.Retire();
            Assert.AreEqual(1, f.Releases["b01-white-city-v2"]);
            Assert.IsFalse(f.Ready("a", camera));
        }
    }

    [Test]
    public void ProductionInstalledTownReleasesAtStartPlusTwo()
    {
        using (var f = new Fixture(configurePolicy: false))
        {
            Call(f.layer, "ConfigureProduction", ProductionResidency.Approved().Towns);
            var camera = f.Camera(); f.Prepare(camera); f.Arrive("b01-white-city-v2");
            Assert.IsTrue(f.Ready("a", camera));
            int start = Time.frameCount;
            Call(f.layer, "AdvanceCameraFrame", start + 1); f.Retire();
            Assert.IsFalse(f.Releases.ContainsKey("b01-white-city-v2"));
            Call(f.layer, "AdvanceCameraFrame", start + 2); f.Retire();
            Assert.AreEqual(1, f.Releases["b01-white-city-v2"]);
            Assert.IsFalse(f.Ready("a", camera));
        }
    }

    [Test]
    public void CameraPriorityUsesSortedOwnerIdsRatherThanInsertionOrder()
    {
        using (var f = new Fixture())
        {
            var a = f.Camera(); var b = f.Camera(8000);
            f.Prepare(b); f.Prepare(a);
            var ordered = (System.Collections.IEnumerable)Call(f.layer, "OrderedChoices")!;
            var ids = new List<int>();
            foreach (object choice in ordered)
                ids.Add(((Camera)choice.GetType().GetField("camera")!.GetValue(choice)!).GetInstanceID());
            var expected = new[] { a.GetInstanceID(), b.GetInstanceID() }; Array.Sort(expected);
            CollectionAssert.AreEqual(expected, ids);
        }
    }

    [Test]
    public void OverlappingFootprintsChooseTheNearestCentreBeforeTheOrdinalTieBreak()
    {
        var wide = new Bounds(new Vector3(500, 0, 0), new Vector3(2000, 100, 2000));
        var own = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
        Assert.AreEqual("z-own", Focus(new[] { ("a-neighbour", wide), ("z-own", own) }, Vector3.zero)[0]);
    }

    [Test]
    public void ProvisionalConstantsDoNotAdmitAPrimaryBeforeExplicitConfiguration()
    {
        using var f = new Fixture(configurePolicy: false); var camera = f.Camera(); f.Prepare(camera);
        CollectionAssert.IsEmpty(f.Requests); Assert.IsFalse(f.a.forceRenderingOff);
        Assert.IsFalse((bool)typeof(TownModelLayer).GetProperty("SpatialPolicyConfigured", Flags)!.GetValue(f.layer)!);
        Call(f.layer, "ConfigureResidency", 10f, 20f, 0f, 0, "test only; no shipping acceptance");
        f.Prepare(camera); Assert.IsTrue(f.Requests.ContainsKey("b01-white-city-v2"));
    }
    [Test]
    public void SelectionUsesExplicitValuesRatherThanTheProvisionalEntryDistance()
    {
        var towns = new[] { Place("town", 1000) };
        var small = (string[])Call(null, "FocusedTowns", towns, Vector3.zero, null, null, 10f, 20f, 0f, 0)!;
        var large = (string[])Call(null, "FocusedTowns", towns, Vector3.zero, null, null, 1000f, 2000f, 0f, 0)!;
        CollectionAssert.IsEmpty(small); CollectionAssert.AreEqual(new[] { "town" }, large);
    }
    [Test]
    public void FailedNearOriginalKeepsTheKitWithoutAcquiringAWholeTownReplacement()
    {
        using var f = new Fixture(); var camera = f.Camera(); f.Prepare(camera);
        f.Answers["b01-white-city-v2"](null); f.Prepare(camera);
        Assert.IsFalse(f.a.forceRenderingOff); Assert.IsFalse(f.Ready("a", camera));
        Assert.IsFalse((bool)Call(f.layer, "WantsWholeTown", "a", camera)!,
            "null Near means code-built fallback, not a second costly representation");
    }

    [Test]
    public void ConfigurationWithoutDiagnosticEvidenceCannotEnableAdmission()
    {
        using var f = new Fixture(configurePolicy: false);
        var error = Assert.Throws<TargetInvocationException>(() =>
            Call(f.layer, "ConfigureResidency", 1000f, 2000f, 1000f, 0, ""));
        Assert.IsInstanceOf<ArgumentException>(error!.InnerException);
        Assert.IsFalse((bool)typeof(TownModelLayer).GetProperty("SpatialPolicyConfigured", Flags)!.GetValue(f.layer)!);
    }

    /// <summary>Owner ruling, 31 Oct 2026, "Load all in view (Recommended)": below 40 km the building models of every town
    /// AND village in the camera's view (+3 km) load, nearest first, and unload on zoom-out. The real map in the player's
    /// streaming path: no editor model list, every original kind acquired through the ModelAcquire seam.</summary>
    sealed class StreamedMap : IDisposable
    {
        readonly FieldInfo acquire;
        readonly object? previousAcquire;
        readonly GameObject prefab;
        readonly Material material;
        readonly List<Camera> cameras = new List<Camera>();
        public readonly MapScene map;
        public readonly TownModelLayer layer;
        public readonly Transform kits;
        readonly CameraRig rig;
        public readonly List<string> RequestOrder = new List<string>();
        readonly Dictionary<string, Action<GameObject?>> answers = new Dictionary<string, Action<GameObject?>>();
        public int Released;
        public StreamedMap()
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            map = UnityEngine.Object.FindFirstObjectByType<MapScene>(); map.Build();
            layer = map.GetComponentInChildren<TownModelLayer>(true);
            layer.EdgeAllowance = null;
            kits = map.transform.Find(KitLayer.RootName);
            rig = UnityEngine.Object.FindFirstObjectByType<CameraRig>();
            prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); prefab.name = "stub original";
            // Submit draws with Graphics.RenderMeshInstanced, which needs an instancing material (as the originals' are).
            var renderer = prefab.GetComponent<MeshRenderer>();
            material = new Material(renderer.sharedMaterial) { enableInstancing = true }; renderer.sharedMaterial = material;
            acquire = typeof(TownModelLayer).GetField("ModelAcquire", Flags)!;
            previousAcquire = acquire.GetValue(null);
            acquire.SetValue(null, new Func<string, CollectionDetail, Action<GameObject?>, IDisposable>((id, _, done) =>
            {
                RequestOrder.Add(id); answers[id] = done;
                return new Release(() => Released++);
            }));
            // The player's path: no editor model list; each original kind streams on demand.
            typeof(TownModelLayer).GetField("set", Flags)!.SetValue(layer, null);
            typeof(TownModelLayer).GetField("streaming", Flags)!.SetValue(layer, true);
        }
        public (Vector2 centre, float radius, string id) Footprint(string id) => map.KitFootprints.Single(f => f.id == id);
        /// <summary>The production rig's pose (50 degree pitch, 35 degree FOV, 16:9) looking at <paramref name="at"/>.</summary>
        public Camera Camera(Vector2 at, float distanceM)
        {
            var camera = new GameObject("in-view residency camera").AddComponent<Camera>(); cameras.Add(camera);
            Pose(camera, at, distanceM);
            return camera;
        }
        public void Pose(Camera camera, Vector2 at, float distanceM)
        {
            rig.LookAt = new Vector3(at.x, 0f, at.y); rig.Distance = distanceM; rig.Yaw = 0f; rig.TopInset = 0f; rig.Apply();
            var source = rig.GetComponent<Camera>();
            camera.CopyFrom(source); camera.ResetProjectionMatrix();
            camera.fieldOfView = CameraRig.Fov; camera.aspect = 16f / 9f;
            camera.nearClipPlane = 50f; camera.farClipPlane = 1_000_000f;   // CameraRig.Init's planes
            camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        }
        /// <summary>Prepares the camera, answers every request with a stub original, and prepares it again.</summary>
        public void Settle(Camera camera)
        {
            layer.PrepareCamera(camera);
            foreach (var answer in answers.Values.ToList()) answer(prefab);
            layer.PrepareCamera(camera);
        }
        public void Submit(Camera camera) =>
            typeof(TownModelLayer).GetMethod("Submit", Flags)!.Invoke(layer, new object[] { default(UnityEngine.Rendering.ScriptableRenderContext), camera });
        /// <summary>The merged locality tile renderer (and its close mesh) that holds a place's ground point.</summary>
        public (MeshRenderer kit, MeshRenderer close) Tile(Vector2 at)
        {
            string name = $"Localities {Mathf.FloorToInt(at.x / SettlementKits.TileM)},{Mathf.FloorToInt(at.y / SettlementKits.TileM)}";
            var kit = kits.Find(name)?.GetComponent<MeshRenderer>();
            var close = kits.Find(name + KitLayer.CloseSuffix)?.GetComponent<MeshRenderer>();
            Assert.IsNotNull(kit, name + ": the merged locality tile"); Assert.IsNotNull(close, name + ": its close mesh");
            return (kit!, close!);
        }
        public void Dispose()
        {
            acquire.SetValue(null, previousAcquire);
            foreach (var camera in cameras) if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(material);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VisualToggles.Clear();
        }
    }

    const float TwelveKmM = 11_775f, ThirtyKmM = 30_394f, GameplayM = 60_000f;

    /// <summary>The v0.3.2 blocker (white 100x kit houses where v0.3.1 drew Tal Heuberger's building models): Imwas's merged
    /// locality tile swaps to its building models at 12 km and at 30 km, and the models draw.</summary>
    [Test]
    public void ImwasVillageTileSwapsToBuildingModelsAtTwelveAndThirtyKm()
    {
        using var s = new StreamedMap();
        var imwas = s.Footprint("imwas");
        Assert.Greater(s.layer.CountFor("imwas"), 0, "Imwas has building models");
        var (kit, close) = s.Tile(imwas.centre);
        foreach (float d in new[] { TwelveKmM, ThirtyKmM })
        {
            var camera = s.Camera(imwas.centre, d);
            string at = FormattableString.Invariant($"Imwas at {d / 1000f} km");
            s.Settle(camera);
            Assert.IsTrue(s.layer.ReadyForTown("imwas", camera), at + ": building models admitted and ready");
            Assert.IsTrue(kit.forceRenderingOff, at + ": the 100x kit tile gives way");
            Assert.IsFalse(close.forceRenderingOff, at + ": the tile's close mesh (ground, plinths) draws");
            s.Submit(camera);
            Assert.Greater(s.layer.LastDrawn, 0, at + ": the building models draw");
        }
    }

    /// <summary>The Latrun front, where no game town is under the screen centre: every village there (Imwas, Yalu, Beit Nuba)
    /// is admitted at 12 km and at 30 km.</summary>
    [Test]
    public void EveryLatrunVillageInViewIsAdmitted()
    {
        using var s = new StreamedMap();
        var imwas = s.Footprint("imwas");
        foreach (float d in new[] { TwelveKmM, ThirtyKmM })
        {
            var camera = s.Camera(imwas.centre, d);
            s.Settle(camera);
            foreach (var id in new[] { "imwas", "yalo", "bayt_nuba" })
            {
                Assert.Greater(s.layer.CountFor(id), 0, id + " has building models");
                Assert.IsTrue(s.layer.ReadyForTown(id, camera), FormattableString.Invariant($"{id} at {d / 1000f} km: admitted and ready"));
            }
        }
    }

    /// <summary>A town's neighbours in view load too, not only the town under the screen centre; the focused town's kinds
    /// are asked first (nearest first).</summary>
    [Test]
    public void NeighbouringTownsOfTheFocusLoadToo()
    {
        using var s = new StreamedMap();
        var telAviv = s.Footprint("tel_aviv");
        var camera = s.Camera(telAviv.centre, ThirtyKmM);
        s.Settle(camera);
        foreach (var id in new[] { "tel_aviv", "jaffa", "petah_tikva" })
        {
            Assert.Greater(s.layer.CountFor(id), 0, id + " has building models");
            Assert.IsTrue(s.layer.ReadyForTown(id, camera), id + " at 30 km over Tel Aviv: admitted and ready");
        }
        var telAvivKinds = s.layer.InstancesOf("tel_aviv").Select(m => TownModels.Ids[(int)m.Model]).ToHashSet(StringComparer.Ordinal);
        CollectionAssert.Contains(telAvivKinds, s.RequestOrder[0], "the focused town's kinds are asked first");
        var jaffa = s.Footprint("jaffa");
        s.Pose(camera, jaffa.centre, TwelveKmM); s.Settle(camera);
        Assert.IsTrue(s.layer.ReadyForTown("jaffa", camera), "Jaffa at 12 km");
        Assert.IsTrue(s.layer.ReadyForTown("tel_aviv", camera), "Tel Aviv beside Jaffa at 12 km");
    }

    /// <summary>The 1.5 GB guard: zooming out to the 60 km gameplay view releases every building-model lease and the kits
    /// draw again.</summary>
    [Test]
    public void BuildingModelsUnloadAtSixtyKm()
    {
        using var s = new StreamedMap();
        var imwas = s.Footprint("imwas");
        var (kit, _) = s.Tile(imwas.centre);
        var camera = s.Camera(imwas.centre, ThirtyKmM);
        s.Settle(camera);
        Assert.IsTrue(s.layer.ReadyForTown("imwas", camera), "precondition: Imwas ready at 30 km");
        Assert.IsNotEmpty(s.RequestOrder);
        s.Pose(camera, imwas.centre, GameplayM);
        s.layer.PrepareCamera(camera); s.layer.RetireReleasedModels();
        Assert.AreEqual(s.RequestOrder.Count, s.Released, "every building-model lease is released at 60 km");
        Assert.IsFalse(s.layer.ReadyForTown("imwas", camera), "no building models at 60 km");
        Assert.IsFalse(kit.forceRenderingOff, "the kit tile draws again at 60 km");
    }
}
