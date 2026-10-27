#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class CollectionOtherLeaveTests
{
    GameObject prefab = null!;
    int acquired, released;
    readonly List<Action<GameObject?>> pending = new List<Action<GameObject?>>();
    Action<string, CollectionDetail, Action<GameObject?>> sea = null!, building = null!;
    sealed class Lease : IDisposable
    {
        Action? done; public Lease(Action done) { this.done = done; }
        public void Dispose() { var call = done; done = null; call?.Invoke(); }
    }
    CollectionResidencyPolicy Policy => new CollectionResidencyPolicy(1000000, 1000001, 100000, 110000, 0, "TEST ONLY");
    IDisposable Acquire(string id, CollectionDetail detail, Action<GameObject?> answer)
    { acquired++; if (delayed) pending.Add(answer); else answer(prefab); return new Lease(() => released++); }
    bool delayed, ownedUnits;
    [SetUp] public void Setup()
    {
        acquired = released = 0; delayed = ownedUnits = false; pending.Clear();
        sea = SeaAirCollection.Request; building = SpecialBuildingModels.Request;
        UnitFigures.Source = (_, __, done) => done(prefab); UnitFigures.Forget();
        SeaAirCollection.Request = SpecialBuildingModels.Request = (_, __, done) => done(prefab);
    }
    [TearDown] public void Reset()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SeaAirCollection.Request = sea; SpecialBuildingModels.Request = building;
        UnitFigures.Source = null; UnitFigures.Forget();
        CollectionResidencyMetrics.Stop();
    }
    void Prefab()
    { prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); prefab.transform.position = Vector3.one * -100000; }
    Component MapLayer(bool special)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single); Prefab();
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        // Install a fixture-owned source BEFORE ShowAt constructs its first demand, as production does.
        var root = new GameObject(special ? SpecialBuildingModels.Name : SeaAirCollection.Name);
        root.transform.SetParent(map.Terrain.transform, false);
        Component component = special ? (Component)root.AddComponent<SpecialBuildingModels>() : root.AddComponent<SeaAirCollection>();
        // Private map reference is fixture construction only; policy uses the internal typed API.
        component.GetType().GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component, map);
        Configure(component);
        return special ? SpecialBuildingModels.ShowAt(map, new DateTime(1948, 5, 15))! :
            SeaAirCollection.ShowAt(map, new DateTime(1949, 8, 15), Array.Empty<Placement>(), ContentLoader.Load(Statecraft.Game.Paths.Content))!;
    }
    void Configure(Component layer)
    {
        var method = layer.GetType().GetMethod("ConfigureResidency", BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(layer, new object[] { Policy, new Func<string, CollectionDetail, Action<GameObject?>, IDisposable>(Acquire) });
    }
    static void View(Component layer, float height)
    {
        var go = new GameObject("TEST ONLY camera"); var camera = go.AddComponent<Camera>();
        go.transform.position = new Vector3(0, height, 0); go.transform.rotation = Quaternion.Euler(90, 0, 0);
        layer.GetType().GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(layer,
            new object[] { default(UnityEngine.Rendering.ScriptableRenderContext), camera });
        Object.DestroyImmediate(go);
    }
    [TestCase(false)] [TestCase(true)] public void CraftAndBuildingLeaveAndRevisit(bool special)
    {
        var layer = MapLayer(special); View(layer, 10000);
        Assert.Greater(layer.transform.childCount, 0); int before = acquired;
        View(layer, 250000); Assert.AreEqual(0, layer.transform.childCount, "forceRenderingOff is not release");
        Assert.AreEqual(acquired, released);
        if (!special) Assert.AreEqual(0, ((SeaAirCollection)layer).KeepClear.Count, "labels must forget retired hulls");
        View(layer, 10000); Assert.Greater(layer.transform.childCount, 0); Assert.Greater(acquired, before);
        Object.DestroyImmediate(layer.gameObject); Assert.AreEqual(acquired, released);
    }
    [TestCase(false)] [TestCase(true)] public void LateAnswerDoesNotRebuildDepartedCraftOrBuilding(bool special)
    {
        delayed = true; var layer = MapLayer(special); View(layer, 10000); View(layer, 250000);
        foreach (var done in pending.ToArray()) done(prefab);
        Assert.AreEqual(0, layer.transform.childCount); Assert.AreEqual(acquired, released);
    }
    GameObject Piece(string name)
    {
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cube); piece.name = name;
        new GameObject("PlateMustSurvive").transform.SetParent(piece.transform);
        if (ownedUnits) UnitFigures.Dress(piece.transform, UnitFigures.YoungRecruit, Policy, Acquire);
        else UnitFigures.Dress(piece.transform, UnitFigures.YoungRecruit); return piece;
    }
    void UnitConfig()
    {
        Prefab();
        ownedUnits = true;
    }
    [Test] public void UnitLeaveRestoresCodeAndPlateAndLastOwnerClearsSharedCache()
    {
        UnitConfig(); var a = Piece("layer A" ); var b = Piece("layer B");
        a.SetActive(false); // experimental holder disable retires its imported children only
        Assert.IsNull(a.transform.Find(UnitFigures.ModelName)); Assert.IsTrue(a.GetComponent<MeshRenderer>().enabled);
        Assert.IsNotNull(a.transform.Find("PlateMustSurvive"));
        Assert.IsTrue(UnitFigures.Arrived(UnitFigures.YoungRecruit), "other live layer still owns the prefab");
        Assert.IsTrue(UnitFigures.Dressed(b.transform));
        Object.DestroyImmediate(b); Assert.IsFalse(UnitFigures.Arrived(UnitFigures.YoungRecruit));
        a.SetActive(true); Assert.IsTrue(UnitFigures.Dressed(a.transform));
        Object.DestroyImmediate(a); Assert.AreEqual(acquired, released);
    }
    [Test] public void LateUnitAnswerCannotDressDisabledHolder()
    {
        UnitConfig(); delayed = true; var a = Piece("departed unit"); a.SetActive(false);
        foreach (var done in pending.ToArray()) done(prefab);
        Assert.IsNull(a.transform.Find(UnitFigures.ModelName)); Assert.IsFalse(UnitFigures.Arrived(UnitFigures.YoungRecruit));
        Assert.AreEqual(acquired, released);
    }
    [Test] public void NearCacheIsReleasedOnlyAfterTheLastUnitLayerLeaves()
    {
        UnitConfig(); var a = Piece("near A"); var b = Piece("near B");
        a.GetComponent<UnitFigureResidency>().RequestNear(); b.GetComponent<UnitFigureResidency>().RequestNear();
        Assert.IsNotNull(UnitFigures.Near(UnitFigures.YoungRecruit));
        a.SetActive(false); Assert.IsNotNull(UnitFigures.Near(UnitFigures.YoungRecruit));
        Assert.IsTrue(UnitFigures.Dressed(b.transform));
        b.SetActive(false); Assert.IsNull(UnitFigures.Near(UnitFigures.YoungRecruit));
        Assert.AreEqual(acquired, released);
    }
    [Test] public void UnitSpatialLeaveRetiresAndRevisitDressesTheSameHolder()
    {
        UnitConfig(); var a = Piece("spatial unit"); var owner = a.GetComponent<UnitFigureResidency>();
        View(owner, 250000); Assert.IsNull(a.transform.Find(UnitFigures.ModelName));
        Assert.IsTrue(a.GetComponent<MeshRenderer>().enabled); Assert.AreEqual(acquired, released);
        View(owner, 10000); Assert.IsTrue(UnitFigures.Dressed(a.transform));
        Object.DestroyImmediate(a); Assert.AreEqual(acquired, released);
    }
    [Test] public void ExistingPermanentUnitPinsStayProtectedWhenAnOwnedHolderLeaves()
    {
        Prefab(); var legacy = Piece("legacy unit");
        ownedUnits = true; var ownedPiece = Piece("owned unit");
        Object.DestroyImmediate(ownedPiece);
        Assert.IsTrue(UnitFigures.Dressed(legacy.transform));
    }

}
