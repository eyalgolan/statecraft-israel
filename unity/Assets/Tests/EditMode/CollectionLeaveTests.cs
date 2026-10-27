#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>C217a: the base compiles these fixtures too; reflection is confined to the new, absent-on-base API.</summary>
public class CollectionLeaveTests
{
    Action<string, CollectionDetail, Action<GameObject?>> people = null!, rural = null!;
    readonly List<Action<GameObject?>> delayed = new List<Action<GameObject?>>();
    GameObject prefab = null!;
    int acquired, released;
    sealed class Lease : IDisposable
    {
        Action? release;
        public Lease(Action release) { this.release = release; }
        public void Dispose() { var call = release; release = null; call?.Invoke(); }
    }
    [SetUp] public void Setup()
    {
        acquired = released = 0; delayed.Clear();
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
    void Configure(Component layer, bool delay)
    {
        // The baseline has neither API; still drive its existing visible/leave behaviour and observe the regression.
        var configure = layer.GetType().GetMethod("ConfigureResidency", BindingFlags.Instance | BindingFlags.NonPublic);
        if (configure == null) return;
        var policyType = configure.GetParameters()[0].ParameterType;
        var policy = Activator.CreateInstance(policyType, 1000000f, 1000001f, 100000f, 110000f, 0, "TEST ONLY");
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable> acquire = (_, __, answer) =>
        {
            acquired++;
            if (delay) delayed.Add(answer); else answer(prefab);
            return new Lease(() => released++);
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
        Camera(layer, 250000); Assert.AreEqual(0, Count(layer), "hiding is not retirement");
        Assert.AreEqual(acquired, released, "every old lease retired");
        int before = acquired;
        Camera(layer, 10000); Assert.Greater(Count(layer), 0); Assert.Greater(acquired, before);
        Object.DestroyImmediate(layer.gameObject); Assert.AreEqual(acquired, released);
    }
    [TestCase(false)] [TestCase(true)]
    public void ADelayedAnswerCannotResurrectALeftCategory(bool isRural)
    {
        var layer = Layer(isRural, Map()); Configure(layer, true);
        Camera(layer, 10000); Camera(layer, 250000);
        foreach (var answer in delayed) answer(prefab);
        Assert.AreEqual(0, Count(layer));
        Assert.AreEqual(acquired, released);
    }
}
