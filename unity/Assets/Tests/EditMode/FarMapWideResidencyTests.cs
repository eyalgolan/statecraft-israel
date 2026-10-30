#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Routes;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Owner decision, 30 October 2026: far originals are consumer-lifetime demand; only Near is spatial.</summary>
public sealed class FarMapWideResidencyTests
{
    sealed class Lease : IDisposable
    {
        readonly Action release;
        bool disposed;
        public Lease(Action release) { this.release = release; }
        public void Dispose() { if (disposed) return; disposed = true; release(); }
    }

    [TearDown]
    public void TearDown()
    {
        RoadVehicles.Request = null;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void RemovingOneKeyRetiresOnlyItsInstancesAndKeepsEverySurvivorAlive()
    {
        int acquired = 0, released = 0;
        var a = new GameObject("a instance");
        var b = new GameObject("b instance");
        var instances = new Dictionary<(string id, CollectionDetail detail), GameObject>
        {
            [("a", CollectionDetail.Near)] = a,
            [("b", CollectionDetail.Far)] = b,
        };
        var residency = new CollectionResidency((_, __, answer) =>
            { acquired++; return new Lease(() => released++); }, keys =>
            {
                var removed = new List<Object>();
                foreach (var key in keys)
                    if (instances.Remove(key, out var instance)) { removed.Add(instance); Object.DestroyImmediate(instance); }
                return removed;
            }, (_, __, ___) => { }, (_, owners, __, done) =>
            { foreach (var owner in owners) owner.Dispose(); done(); }, true);

        residency.Replace(instances.Keys);
        residency.Replace(new[] { ("b", CollectionDetail.Far) });

        Assert.AreSame(b, instances[("b", CollectionDetail.Far)], "a pan cannot replace or blink a still-demanded instance");
        Assert.AreEqual(2, acquired);
        Assert.AreEqual(1, released, "only the departed Near key retires");
        Assert.AreEqual(1, residency.OwnerCount);
        residency.Dispose();
        Assert.AreEqual(acquired, released, "all refcounts return to zero on teardown");
        Assert.IsTrue(b == null, "the final bundle lease is released only after its instance is destroyed");
    }

    [Test]
    public void PeopleFarDemandSurvivesA90KmCameraOutsideTheNearDisc() =>
        AssertFarDemand<CivilianPeople>(new List<CivilianPeople.Want>
        { new CivilianPeople.Want("far/0", CivilianPeople.Townsman, new Vector3(200000, 0, 0), 0) }, "wanted");

    [Test]
    public void RuralFarDemandSurvivesA90KmCameraOutsideTheNearDisc() =>
        AssertFarDemand<RuralLife>(new List<RuralLife.Want>
        { new RuralLife.Want("far", RuralLife.Tractor, new Vector2(200000, 0), 0) }, "wants");

    void AssertFarDemand<T>(object wants, string field) where T : MonoBehaviour
    {
        int acquired = 0, released = 0;
        var go = new GameObject(typeof(T).Name); var layer = go.AddComponent<T>();
        try
        {
            typeof(T).GetMethod("ConfigureResidency", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer,
                new object[] { ProductionResidency.Approved().People,
                    (Func<string, CollectionDetail, Action<GameObject?>, IDisposable>)((_, detail, __) =>
                    { Assert.AreEqual(CollectionDetail.Far, detail); acquired++; return new Lease(() => released++); }) });
            typeof(T).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(layer, wants);
            typeof(T).GetMethod("RefreshResidency", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer, null);
            var camera = new GameObject("90 km camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 90000, 0), Quaternion.Euler(90, 0, 0));
            typeof(T).GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer,
                new object[] { default(UnityEngine.Rendering.ScriptableRenderContext), camera });
            Object.DestroyImmediate(camera.gameObject);
            Assert.AreEqual(1, acquired, "the Far model is consumer demand, not camera-focus demand");
        }
        finally { Object.DestroyImmediate(go); }
        Assert.AreEqual(acquired, released);
    }

    [Test]
    public void SpecialFarRepresentationIsDemandedWithoutCameraAdmission()
    {
        AssertUnfocusedDemand<SpecialBuildingModels, SpecialBuildingModels.Want>(
            new SpecialBuildingModels.Want("fort", SpecialBuildingModels.Tegart, DateTime.MinValue, 0, 0, "TEST"),
            "wants", CollectionDetail.Near);
    }

    [Test]
    public void CraftFarRepresentationIsDemandedWithoutCameraAdmission()
    {
        AssertUnfocusedDemand<SeaAirCollection, SeaAirCollection.Want>(
            new SeaAirCollection.Want("boat", SeaAirCollection.FishingBoat, "jaffa"),
            "wants", CollectionDetail.Far);
    }

    void AssertUnfocusedDemand<TLayer, TWant>(TWant want, string field, CollectionDetail expected) where TLayer : MonoBehaviour
    {
        int acquired = 0, released = 0;
        var go = new GameObject(typeof(TLayer).Name); var layer = go.AddComponent<TLayer>();
        try
        {
            typeof(TLayer).GetMethod("ConfigureResidency", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer,
                new object[] { ProductionResidency.Approved().Special,
                    (Func<string, CollectionDetail, Action<GameObject?>, IDisposable>)((_, detail, __) =>
                    { Assert.AreEqual(expected, detail); acquired++; return new Lease(() => released++); }) });
            typeof(TLayer).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(layer, new List<TWant> { want });
            typeof(TLayer).GetMethod("RefreshResidency", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(layer, null);
            Assert.AreEqual(1, acquired);
        }
        finally { Object.DestroyImmediate(go); }
        Assert.AreEqual(acquired, released);
    }

    [Test]
    public void SharedRoadVehiclesOwnFarLeasesUntilLayerTeardown()
    {
        int acquired = 0;
        RoadVehicles.Request = (_, detail, __) => { Assert.AreEqual(CollectionDetail.Far, detail); acquired++; };
        var go = new GameObject("roads"); var roads = go.AddComponent<RoadVehicles>();
        roads.ShowAt(new DateTime(1948, 5, 15));
        Assert.AreEqual(2, acquired, "the two start-date road models are retained before any camera chooses Near visibility");
        // The fake request seam has no asset ownership; the coordinator still drops every owned production lease on destroy.
        Object.DestroyImmediate(go);
    }

    [Test]
    public void WholeTownAndPlacementLandmarksAreAllowedWithoutAFocusTown()
    {
        var go = new GameObject("towns"); var layer = go.AddComponent<TownLandmarks>();
        try
        {
            var allowed = typeof(TownLandmarks).GetMethod("Allowed", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.IsTrue((bool)allowed.Invoke(layer, new object[]
            { new TownLandmarks.Want("town", TownLandmarks.Moshava, Vector2.zero, 1, true) })!);
            Assert.IsTrue((bool)allowed.Invoke(layer, new object[]
            { new TownLandmarks.Want("camp", TownLandmarks.Maabara, Vector2.zero, 1, false) })!);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
