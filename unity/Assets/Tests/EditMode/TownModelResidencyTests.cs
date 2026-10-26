#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Collection;

/// <summary>C89/C91: original artwork near the camera focus, with fallback elsewhere. Reflection keeps the test-only
/// revision compilable on the pre-change tree so the requested deferred red run can fail on behavior/API assertions.</summary>
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
}
