#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>#1800: the base compiles these fixtures too; reflection is confined to the new, absent-on-base API.</summary>
public class CollectionLeaveTests
{
    Action<string, CollectionDetail, Action<GameObject?>> people = null!, rural = null!;
    readonly List<Action<GameObject?>> delayed = new List<Action<GameObject?>>();
    GameObject prefab = null!;
    int acquired, released;
    readonly Dictionary<CollectionDetail, int> acquiredBy = new Dictionary<CollectionDetail, int>(), releasedBy = new Dictionary<CollectionDetail, int>();
    int Acquired(CollectionDetail detail) => acquiredBy.TryGetValue(detail, out int n) ? n : 0;
    int Released(CollectionDetail detail) => releasedBy.TryGetValue(detail, out int n) ? n : 0;
    sealed class Lease : IDisposable
    {
        Action? release;
        public Lease(Action release) { this.release = release; }
        public void Dispose() { var call = release; release = null; call?.Invoke(); }
    }
    [SetUp] public void Setup()
    {
        acquired = released = 0; delayed.Clear(); acquiredBy.Clear(); releasedBy.Clear();
        people = CivilianPeople.Request; rural = RuralLife.Request;
        prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "TEST ONLY original"; prefab.transform.position = Vector3.one * -100000;
        CivilianPeople.Request = RuralLife.Request = (_, __, answer) => answer(prefab);
    }
    [TearDown] public void Reset()
    {
        CivilianPeople.Request = people; RuralLife.Request = rural;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CollectionResidencyMetrics.Stop();
    }
    MapScene Map()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); prefab.transform.position = Vector3.one * -100000;
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build(); return map;
    }
    Component Layer(bool isRural, MapScene map) => isRural ? RuralLife.Show(map)! :
        CivilianPeople.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), ContentLoader.Load(Statecraft.Game.Paths.Content))!;
    static int Count(Component layer) => ((System.Collections.ICollection)layer.GetType().GetField("Placed")!.GetValue(layer)!).Count;
    /// <summary>Placed Far originals: every civilian figure (each has its Far), or RuralLife's Far rows.</summary>
    static int FarCount(Component layer)
    {
        int n = 0;
        foreach (var placed in (System.Collections.IEnumerable)layer.GetType().GetField("Placed")!.GetValue(layer)!)
        {
            // RuralLife.Placed is a (Key, Model, Detail, ...) value tuple: its Detail is the runtime field Item3.
            var detail = placed.GetType().GetField("Item3");
            if (detail == null || detail.FieldType != typeof(CollectionDetail)
                || (CollectionDetail)detail.GetValue(placed)! == CollectionDetail.Far) n++;
        }
        return n;
    }
    /// <summary>Live Near original instances: a figure's "Near" child or a rural ":near" instance.</summary>
    static int NearInstances(Component layer) =>
        layer.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Near" || t.name.EndsWith(":near", StringComparison.Ordinal));
    void Configure(Component layer, bool delay)
    {
        // The baseline has neither API; still drive its existing visible/leave behaviour and observe the regression.
        var configure = layer.GetType().GetMethod("ConfigureResidency", BindingFlags.Instance | BindingFlags.NonPublic);
        if (configure == null) return;
        var policyType = configure.GetParameters()[0].ParameterType;
        var policy = Activator.CreateInstance(policyType, 1000000f, 1000001f, 100000f, 110000f, 0, "TEST ONLY");
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable> acquire = (_, __, answer) =>
        {
            acquired++; acquiredBy[__] = Acquired(__) + 1;
            if (delay) delayed.Add(answer); else answer(prefab);
            return new Lease(() => { released++; releasedBy[__] = Released(__) + 1; });
        };
        configure.Invoke(layer, new object[] { policy!, acquire });
    }
    static void Camera(Component layer, float height)
    {
        var go = new GameObject("TEST ONLY camera"); var camera = go.AddComponent<Camera>();
        go.transform.position = new Vector3(0, height, 0); go.transform.rotation = Quaternion.Euler(90, 0, 0);
        layer.GetType().GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer,
            new object[] { default(UnityEngine.Rendering.ScriptableRenderContext), camera });
        Object.DestroyImmediate(go);
    }
    [TestCase(false)] [TestCase(true)]
    public void LeavingDestroysOriginalsAndRevisitAcquiresAgain(bool isRural)
    {
        var layer = Layer(isRural, Map()); Configure(layer, false);
        Camera(layer, 10000); Assert.Greater(Count(layer), 0);
        var first = layer.transform.GetChild(0); int stableAcquires = acquired;
        Camera(layer, 10000);
        Assert.AreSame(first, layer.transform.GetChild(0), "an unchanged camera demand must not rebuild every frame");
        Assert.AreEqual(stableAcquires, acquired);
        // Owner ruling 29 (30 Oct 2026): "Far models map-wide (Recommended)": Far stays map-wide; only Near loads/unloads by camera.
        // Leaving keeps every Far original and its lease, and retires (destroys and releases) every Near one.
        int far = FarCount(layer), nearFirst = Acquired(CollectionDetail.Near);
        if (isRural) Assert.Greater(nearFirst, 0, "the rural scenes took Near leases at 10 km");
        Assert.AreEqual(nearFirst > 0, NearInstances(layer) > 0, "Near leases have Near instances");
        Camera(layer, 250000);
        Assert.AreEqual(far, FarCount(layer), "leaving keeps every map-wide Far original");
        Assert.IsTrue(first != null, "the Far instances are not rebuilt");
        Assert.AreEqual(0, NearInstances(layer), "hiding is not retirement");
        Assert.AreEqual(Acquired(CollectionDetail.Near), Released(CollectionDetail.Near), "every old Near lease retired");
        Assert.AreEqual(0, Released(CollectionDetail.Far), "no Far lease retires on leave");
        int farBefore = Acquired(CollectionDetail.Far), nearBefore = Acquired(CollectionDetail.Near);
        Camera(layer, 10000); Assert.AreEqual(far, FarCount(layer));
        Assert.AreEqual(farBefore, Acquired(CollectionDetail.Far), "revisit re-acquires no Far");
        Assert.AreEqual(nearFirst, Acquired(CollectionDetail.Near) - nearBefore, "revisit acquires the same Near set again");
        Assert.AreEqual(nearFirst > 0, NearInstances(layer) > 0);
        Object.DestroyImmediate(layer.gameObject); Assert.AreEqual(acquired, released);
    }
    [TestCase(false)] [TestCase(true)]
    public void ADelayedAnswerCannotResurrectALeftCategory(bool isRural)
    {
        var layer = Layer(isRural, Map()); Configure(layer, true);
        Camera(layer, 10000); Camera(layer, 250000);
        foreach (var answer in delayed.ToArray()) answer(prefab);
        // Owner ruling 29 (30 Oct 2026): "Far models map-wide (Recommended)": Far stays map-wide; only Near loads/unloads by camera.
        // A Far demand never left, so its late answer draws; a late Near answer for the departed view cannot.
        if (isRural) Assert.Greater(Acquired(CollectionDetail.Near), 0, "the rural scenes asked for Near at 10 km");
        Assert.AreEqual(0, NearInstances(layer), "no Near original is resurrected");
        Assert.AreEqual(Acquired(CollectionDetail.Near), Released(CollectionDetail.Near), "every departed Near lease retired");
        Assert.AreEqual(0, Released(CollectionDetail.Far), "the map-wide Far leases are kept");
        Assert.Greater(FarCount(layer), 0, "the Far answers draw map-wide");
    }
}
