#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using UnityEngine;
using Object = UnityEngine.Object;

public class CollectionTownLifetimeTests
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    sealed class Lease : IDisposable
    { Action? call; public Lease(Action call) { this.call = call; } public void Dispose() { var done = call; call = null; done?.Invoke(); } }
    [Test] public void SynchronousDisableCannotLeakTheReturnedBuildingLease()
    {
        var old = TownModelLayer.ModelAcquire;
        var root = new GameObject("building owner"); var layer = root.AddComponent<TownModelLayer>();
        var kit = new GameObject("a").AddComponent<MeshRenderer>(); kit.transform.SetParent(root.transform);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); var camera = new GameObject("camera").AddComponent<Camera>();
        int released = 0;
        try
        {
            TownModelLayer.ModelAcquire = (_, __, done) => { done(prefab); layer.enabled = false; return new Lease(() => released++); };
            layer.ConfigureResidency(1000, 2000, 0, 0, "TEST ONLY");
            layer.InitStreaming(new[] { new SettlementKits.ModelInstance("a", TownModel.B01WhiteCity, Matrix4x4.identity, Vector2.zero, 0, 100, 100, 0, 100) },
                new List<(Renderer, Renderer?)> { (kit, null) });
            camera.transform.position = new Vector3(0, 10000, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            layer.PrepareCamera(camera);
            Assert.AreEqual(1, released, "Acquire returned after disable: retire the orphan return too");
            Assert.IsFalse(kit.forceRenderingOff);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(prefab); Object.DestroyImmediate(camera.gameObject); TownModelLayer.ModelAcquire = old; }
    }
    [Test] public void SynchronousDisableCannotLeakTheReturnedWholeTownLease()
    {
        var old = TownLandmarks.Acquire; var root = new GameObject("whole-town owner"); var layer = root.AddComponent<TownLandmarks>();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); int released = 0;
        try
        {
            TownLandmarks.Acquire = (_, __, done) => { done(prefab); layer.enabled = false; return new Lease(() => released++); };
            typeof(TownLandmarks).GetField("wants", F)!.SetValue(layer, new List<TownLandmarks.Want> {
                new TownLandmarks.Want("test", "tel-aviv-white-city-v1", Vector2.zero, 1, false) });
            typeof(TownLandmarks).GetMethod("RequestWanted", F)!.Invoke(layer, Array.Empty<object>());
            Assert.AreEqual(1, released, "old request generation may not receive ownership after disable");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(prefab); TownLandmarks.Acquire = old; }
    }
    [Test] public void MetricsHaveAnExplicitSnapshotAndPoseMarkerEntryPoint()
    {
        var type = typeof(CollectionStream).Assembly.GetType("Statecraft.Game.Map.Collection.CollectionResidencyMetrics");
        Assert.IsNotNull(type, "per-category ownership and destroy receipts must be observable, not inferred from placement logs");
        Assert.IsNotNull(type!.GetMethod("Capture", F)); Assert.IsNotNull(type.GetMethod("PoseSettled", F));
    }
    [Test] public void TownAdmissionMustBeExplicitInProductionInput()
    {
        var policy = ProductionResidency.Approved();
        Assert.Throws<ArgumentNullException>(() => new ProductionResidency(policy.People, policy.Rural,
            policy.Special, policy.Units, policy.SharedRoad, null!));
    }
}
