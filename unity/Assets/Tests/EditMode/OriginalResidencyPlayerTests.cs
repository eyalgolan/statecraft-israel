#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.HexTiles;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Player-branch ownership/destruction tests, entered from the native EditMode suite. No eager editor set,
/// real artwork fetch or real bundle is needed: both representation paths use one actual stream and an observable source.</summary>
public sealed class OriginalResidencyPlayerTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    readonly Dictionary<FieldInfo, object?> originals = new Dictionary<FieldInfo, object?>();
    readonly List<Object> owned = new List<Object>();
    CollectionStreamHost? host;
    static object? Call(object? owner, Type type, string name, params object?[] args)
    {
        var method = type.GetMethod(name, Flags); Assert.IsNotNull(method, name);
        return method!.Invoke(owner, args);
    }
    void Hook(Type type, string name, object value)
    {
        var field = type.GetField(name, Flags); Assert.IsNotNull(field, name);
        if (!originals.ContainsKey(field!)) originals[field!] = field!.GetValue(null);
        field!.SetValue(null, value);
    }
    [UnitySetUp]
    public IEnumerator EnterPlayerBranch()
    {
        if (Application.isPlaying) yield break;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        // Prevent game startup, UI and unrelated model consumers from driving this focused test.
        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!(behaviour is MapScene)) behaviour.enabled = false;
        // Map.Build needs Camera.main: keep the tagged scene camera until the controlled fixture replaces it.
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { camera.enabled = camera.CompareTag("MainCamera"); camera.cullingMask = 0; }
        Assert.IsNotNull(Camera.main, "the player fixture must keep an enabled MainCamera for Map.Build");
        Object.FindFirstObjectByType<MapScene>().ConfigureTownModels(null!);
        yield return new EnterPlayMode();
    }
    [UnityTearDown]
    public IEnumerator LeavePlayerBranch()
    {
        foreach (var pair in originals) pair.Key.SetValue(null, pair.Value); originals.Clear();
        foreach (var item in owned) if (item != null) Object.Destroy(item); owned.Clear();
        // Let the destruction acknowledgement hosts finish before stopping the observed stream.
        for (int i = 0; i < 5; i++) yield return null;
        if (host != null) Object.Destroy(host.gameObject);
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    sealed class Source : CollectionStream.ISource
    {
        public GameObject Prefab = null!;
        public bool FailNear;
        public readonly HashSet<string> Opened = new HashSet<string>(), Unloaded = new HashSet<string>(), CompletedLoads = new HashSet<string>();
        public Action<string>? BeforeUnload;
        public IEnumerator Open(string bundle, Action<string?> done) { Opened.Add(bundle); yield return null; done(null); }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done)
        { yield return null; CompletedLoads.Add(bundle); done(FailNear && path.Contains("/Settlements/Prototype/", StringComparison.Ordinal) ? null : Prefab); }
        public void Unload(string bundle) { BeforeUnload?.Invoke(bundle); Unloaded.Add(bundle); }
    }
    (CollectionStream stream, Source source, List<(string id, CollectionDetail detail)> asks) Stream()
    {
        var prefab = new GameObject("test original"); owned.Add(prefab);
        var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } }; owned.Add(mesh);
        var material = new Material(Shader.Find("Hidden/InternalErrorShader")); owned.Add(material);
        prefab.AddComponent<MeshFilter>().sharedMesh = mesh; prefab.AddComponent<MeshRenderer>().sharedMaterial = material;
        var source = new Source { Prefab = prefab }; var stream = new CollectionStream(source);
        var asks = new List<(string, CollectionDetail)>();
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable> acquire = (id, detail, answer) =>
        {
            asks.Add((id, detail));
            return (IDisposable)Call(stream, typeof(CollectionStream), "Retain", id, detail, answer)!;
        };
        Hook(typeof(TownModelLayer), "ModelAcquire", acquire); Hook(typeof(TownLandmarks), "Acquire", acquire);
        var hex = typeof(HexStreamedPlacements).Assembly.GetType("Statecraft.HexTiles.HexCollectionFigures")!;
        Hook(hex, "Source", null!); Hook(hex, "Acquire", acquire);
        host = new GameObject("observable stream").AddComponent<CollectionStreamHost>();
        CollectionStream.GameReady(); host.StartCoroutine(stream.Run());
        return (stream, source, asks);
    }
    static IEnumerator Until(Func<bool> condition, string phase = "lifecycle")
    {
        for (int i = 0; i < 120 && !condition(); i++) yield return null;
        Assert.IsTrue(condition(), "bounded player lifecycle condition did not complete: " + phase);
    }
    (MapScene map, TownModelLayer layer, TownLandmarks towns, TownLandmarks.Want focus, Camera camera) Map()
    {
        Assert.IsTrue(Application.isPlaying, "must exercise the player streaming branch");
        var startupCamera = Camera.main; Assert.IsNotNull(startupCamera);
        var map = Object.FindFirstObjectByType<MapScene>(); map.ConfigureTownModels(null!); map.Build();
        startupCamera!.enabled = false; // Only the controlled camera may drive ownership after Build.
        var layer = map.GetComponentInChildren<TownModelLayer>(true);
        Assert.IsNull(typeof(TownModelLayer).GetField("set", Flags)!.GetValue(layer), "no eager editor model set");
        Assert.IsTrue((bool)typeof(TownModelLayer).GetField("streaming", Flags)!.GetValue(layer)!);
        Call(layer, typeof(TownModelLayer), "ConfigureResidency", 1000f, 2000f, 1000f, 0, "player test only; NOT diagnostic acceptance");
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var towns = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
        foreach (var behaviour in map.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(behaviour is TownModelLayer) && !(behaviour is TownLandmarks) && !(behaviour is MapScene)) behaviour.enabled = false;
        var places = (IReadOnlyList<(string id, Bounds bounds)>)typeof(TownModelLayer).GetField("places", Flags)!.GetValue(layer)!;
        var focus = towns.Wanted.Where(w => w.OnTown && layer.CountFor(w.Key) > 0).OrderBy(w => w.Key, StringComparer.Ordinal).First(w =>
        {
            var ids = (string[])Call(null, typeof(TownModelLayer), "FocusedTowns", places, new Vector3(w.At.x, 0, w.At.y),
                null, null, 1000f, 2000f, 1000f, 0)!;
            return ids.Length == 1 && ids[0] == w.Key;
        });
        var camera = new GameObject("controlled residency camera").AddComponent<Camera>(); owned.Add(camera.gameObject);
        camera.tag = "MainCamera";
        camera.cullingMask = 0; // begin/end lifecycle still runs; unrelated scene geometry never renders
        return (map, layer, towns, focus, camera);
    }
    static void Choose(TownLandmarks towns, Camera camera, Vector2 at, bool close)
    {
        float h = close ? 10000 : 40000; camera.transform.position = new Vector3(at.x, h, at.y - h);
        camera.transform.LookAt(new Vector3(at.x, 0, at.y)); towns.Choose(default, camera);
        Call(towns, typeof(TownLandmarks), "RequestWanted");
    }
    [UnityTest]
    public IEnumerator ProductionMapStreamsNearThenWholeAndWaitsForDetachedConsumersBeforeUnload()
    {
        var (_, source, asks) = Stream(); var (_, layer, towns, focus, camera) = Map();
        Choose(towns, camera, focus.At, true);
        yield return Until(() => asks.Any(a => a.detail == CollectionDetail.Near), "near requested");
        Assert.IsTrue(asks.Any(a => a.detail == CollectionDetail.Near), "Map.Build player path must acquire Near originals");
        Assert.IsFalse(asks.Any(a => a.id == focus.Model), "no duplicate whole-town acquisition while Near loads");
        Assert.IsFalse((bool)Call(layer, typeof(TownModelLayer), "ReadyForTown", focus.Key, camera)!);
        yield return Until(() => { towns.Choose(default, camera); return (bool)Call(layer, typeof(TownModelLayer), "ReadyForTown", focus.Key, camera)!; }, "near ready: " + focus.Key);
        var near = asks.Where(a => a.detail == CollectionDetail.Near).Select(a => CollectionCatalog.BundleOf(a.id)!).ToHashSet();
        Choose(towns, camera, focus.At, false);
        var activeNear = (System.Collections.IDictionary)typeof(TownModelLayer).GetField("models", Flags)!.GetValue(layer)!;
        yield return Until(() => activeNear.Count == 0, "near references retired");
        Assert.IsEmpty(activeNear, "Near consumer references are retired on overview handoff; leases await safe release");
        yield return Until(() => { towns.Choose(default, camera); return towns.Seats.ContainsKey(focus.Key) && near.All(source.Unloaded.Contains); }, "whole ready and near unloaded: " + focus.Key);
        Assert.IsTrue(asks.Any(a => a.id == focus.Model && a.detail == CollectionDetail.Far));
        var instance = towns.transform.Find(focus.Key).gameObject;
        var detached = instance.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
        var renderers = instance.GetComponentsInChildren<Renderer>(true); Assert.IsNotEmpty(renderers);
        string whole = CollectionCatalog.BundleOf(focus.Model)!;
        source.BeforeUnload = bundle =>
        {
            if (bundle != whole) return;
            foreach (var go in detached) Assert.IsTrue(go == null, "reparented but live generated consumer");
            foreach (var renderer in renderers) Assert.IsTrue(renderer == null, "live renderer retains original references");
        };
        towns.enabled = false;
        Assert.IsTrue(instance != null, "Destroy must be deferred in this player-branch test");
        Assert.IsNull(instance!.transform.parent, "the test retains detached instances, not a hierarchy lookup");
        Assert.IsFalse(source.Unloaded.Contains(whole), "lease must outlive scheduled destruction");
        yield return Until(() => source.Unloaded.Contains(whole));
        Assert.IsTrue(instance == null); Assert.IsTrue(detached.All(go => go == null));
    }
    [UnityTest]
    public IEnumerator NullStreamedNearKeepsFallbackAndDoesNotAcquireAWholeReplacement()
    {
        var (_, source, asks) = Stream(); source.FailNear = true;
        var (_, layer, towns, focus, camera) = Map(); Choose(towns, camera, focus.At, true);
        yield return Until(() => asks.Any(a => a.detail == CollectionDetail.Near), "near requested");
        var nearIds = asks.Where(a => a.detail == CollectionDetail.Near).Select(a => a.id).Distinct().ToArray();
        Assert.IsNotEmpty(nearIds, "failure proof requires actual Near requests");
        foreach (var _ in nearIds)
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Collection: bundle .* has no .*"));
        yield return Until(() => nearIds.All(id => source.CompletedLoads.Contains(CollectionCatalog.BundleOf(id)!)));
        Assert.IsFalse((bool)Call(layer, typeof(TownModelLayer), "ReadyForTown", focus.Key, camera)!);
        Assert.IsFalse((bool)Call(layer, typeof(TownModelLayer), "WantsWholeTown", focus.Key, camera)!);
        Assert.IsFalse(asks.Any(a => a.id == focus.Model));
        var ids = (IDictionary<Renderer, string>)typeof(TownModelLayer).GetField("swapIds", Flags)!.GetValue(layer)!;
        foreach (var pair in ids) if (pair.Value == focus.Key) Assert.IsFalse(pair.Key.forceRenderingOff);
    }
    [UnityTest]
    public IEnumerator HexPlayerTeardownWaitsForGeneratedObjectsAndTheirRendererReferences()
    {
        var (_, source, asks) = Stream();
        var root = new GameObject("owned hex board"); owned.Add(root); var assets = root.AddComponent<HexBoardAssets>();
        var placement = new HexPlacement("proto:figure-palmach-scout-c12f-1948-v2", StandIn.Figure, Fit.Height,
            Color.white, Matrix4x4.identity, 0, 0);
        HexModels.DrawCollectionFigures = true; HexModels.Clear();
        HexBoardRenderer.Emit(root.transform, new[] { placement }, assets);
        var refresh = root.GetComponentInChildren<HexStreamedPlacements>(); Assert.IsNotNull(refresh);
        yield return Until(() => asks.Count > 0 && asks.All(a => source.CompletedLoads.Contains(CollectionCatalog.BundleOf(a.id)!)));
        refresh.RefreshNow();
        var consumers = refresh.GetComponentsInChildren<Transform>(true).Where(t => t != refresh.transform).Select(t => t.gameObject).ToArray();
        var renderers = refresh.GetComponentsInChildren<Renderer>(true);
        Assert.IsNotEmpty(consumers); Assert.IsNotEmpty(renderers);
        source.BeforeUnload = _ =>
        {
            foreach (var go in consumers) Assert.IsTrue(go == null, "hex generated instance still alive");
            foreach (var renderer in renderers) Assert.IsTrue(renderer == null, "hex renderer still holds mesh/material");
        };
        assets.Teardown();
        Assert.IsTrue(consumers.Any(go => go != null), "must observe the deferred Destroy branch");
        Assert.IsEmpty(source.Unloaded);
        yield return Until(() => source.Unloaded.Count > 0);
        Assert.IsTrue(consumers.All(go => go == null));
    }
    [UnityTest]
    public IEnumerator RetainedTownViewCanAcquireANewFocusAboveTheOldStrategicCap()
    {
        var (_, source, asks) = Stream(); source.Prefab.AddComponent<OriginalModelDistance>();
        var (_, layer, towns, focus, camera) = Map();
        var target = new RenderTexture(320, 180, 24); owned.Add(target); camera.targetTexture = target;
        camera.farClipPlane = 1000000; camera.ResetProjectionMatrix(); camera.ResetWorldToCameraMatrix();
        layer.ConfigureProduction(ProductionResidency.Approved().Towns);
        var next = towns.Wanted.First(w => w.OnTown && w.Model != focus.Model);
        // Private selector inputs isolate this source-handoff test from the real overlapping kit polygons.
        var places = (List<(string id, Bounds bounds)>)typeof(TownModelLayer).GetField("places", Flags)!.GetValue(layer)!;
        places.Clear(); places.Add((focus.Key, new Bounds(new Vector3(focus.At.x, 0, focus.At.y), new Vector3(1000, 0, 1000))));
        places.Add((next.Key, new Bounds(new Vector3(next.At.x, 0, next.At.y), new Vector3(1000, 0, 1000))));
        void Drive()
        {
            towns.Choose(default, camera);
            // Drive the real private render callback; no screenshot/editor render is required by this lifecycle fixture.
            foreach (var request in towns.GetComponents<OriginalModelRequests>())
                Call(request, typeof(OriginalModelRequests), "Begin", default(UnityEngine.Rendering.ScriptableRenderContext), camera);
        }
        camera.transform.rotation = Quaternion.Euler(90, 0, 0);
        camera.transform.position = new Vector3(focus.At.x, 100000, focus.At.y);
        int ticks = 0;
        yield return Until(() =>
        {
            Drive();
            if (++ticks == 120)
            {
                var observedRay = camera.ViewportPointToRay(new Vector3(.5f, .5f));
                bool hit = new Plane(Vector3.up, Vector3.zero).Raycast(observedRay, out float distance);
                Debug.Log("RAY TRACE " + observedRay + " hit=" + hit + " at=" + observedRay.GetPoint(distance) + " pixels=" + camera.pixelWidth + "x" + camera.pixelHeight);
                Debug.Log("BAND TRACE focus=" + focus.Key + " wants=" + layer.WantsWholeTown(focus.Key, camera)
                + " asks=" + string.Join(",", asks.Select(a => a.id)) + " opened=" + string.Join(",", source.Opened)
                + " placed=" + string.Join(",", towns.Seats.Keys) + " active=" + towns.isActiveAndEnabled
                + " draw=" + layer.Draw + " view=" + TownModelLayer.ViewDistance(camera) + " camera=" + camera.transform.position
                + " configured=" + layer.SpatialPolicyConfigured + " bounds=" + string.Join(";", places.Select(p => p.id + ":" + p.bounds))
                + " keys=" + string.Join(",", ((System.Collections.IDictionary)typeof(TownModelLayer).GetField("choices", Flags)!.GetValue(layer)!).Values.Cast<object>().Select(c =>
                    c.GetType().GetField("primary", Flags)!.GetValue(c) + "/" + c.GetType().GetField("close", Flags)!.GetValue(c))));
            }
            return towns.Seats.ContainsKey(focus.Key);
        }, "first overview original");
        camera.transform.position = new Vector3(next.At.x, 150000, next.At.y);
        yield return Until(() => { Drive(); return towns.Seats.ContainsKey(next.Key); }, "new focus in retained 160km band");
        var cap = towns.transform.Find(next.Key).GetComponent<OriginalModelDistance>();
        Assert.IsFalse(cap.enabled, "owned town uses its approved per-camera policy, not a conflicting prefab cap");
        Assert.IsTrue(asks.Any(a => a.id == next.Model));
    }
}
