#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class CollectionMetricsTests
{
    [SetUp] public void Setup() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); CollectionResidencyMetrics.Enable(); }
    [TearDown] public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); CollectionResidencyMetrics.Stop(); CollectionResidencyMetrics.ResetForTests(); }
    [Test] public void SnapshotHasAllCategoriesAndOnlyLogsChangedOwnershipOrAnExplicitPose()
    {
        var lines = new List<string>(); var snapshot = CollectionResidencyMetrics.Capture();
        Assert.AreEqual(12, snapshot.categories.Length);
        foreach (var key in new[] { "people", "rural", "craft", "special", "units", "town_buildings", "whole_towns", "hex_board" })
            Assert.IsTrue(snapshot.categories.Any(c => c.category == key));
        Assert.IsTrue(CollectionResidencyMetrics.EmitSnapshot(snapshot, "state", false, lines.Add));
        snapshot.unityAllocatedBytes++; snapshot.reason = "different frame";
        Assert.IsFalse(CollectionResidencyMetrics.EmitSnapshot(snapshot, "state", false, lines.Add), "memory/frame changes do not create per-frame logs");
        Assert.IsTrue(CollectionResidencyMetrics.EmitSnapshot(snapshot, "pose:jaffa12", true, lines.Add));
        Assert.AreEqual(24, lines.Count); StringAssert.StartsWith("RESIDENCY {", lines[12]);
        StringAssert.Contains("pose:jaffa12", lines[12]); StringAssert.Contains("unityReservedBytes", lines[12]);
        snapshot.categories[0].demandedAnchors++;
        Assert.IsTrue(CollectionResidencyMetrics.EmitSnapshot(snapshot, "state", false, lines.Add));
        var fresh = CollectionResidencyMetrics.Capture(); Assert.AreEqual(0, fresh.categories[0].demandedAnchors, "snapshots detach from owners");
    }
    [Test] public void EachCategoryHasOneBoundedCorrelatedLineEvenWithLongIds()
    {
        var snapshot = CollectionResidencyMetrics.Capture();
        snapshot.categories[0].originalIds = new[] { new string('x', 12000) };
        snapshot.categories[0].detailIds = new[] { new string('y', 12000) };
        snapshot.categories[0].uniqueIds = 1; snapshot.categories[0].uniqueDetails = 1;
        var lines = new List<string>();
        Assert.IsTrue(CollectionResidencyMetrics.EmitSnapshot(snapshot, "pose:long-id", true, lines.Add));
        Assert.AreEqual(snapshot.categories.Length, lines.Count, "one line per category");
        long sequence = -1;
        foreach (string line in lines)
        {
            Assert.Less(line.Length, 4000);
            var json = line.Substring("RESIDENCY ".Length);
            var row = JsonUtility.FromJson<CollectionResidencyMetrics.Snapshot>(json);
            Assert.AreEqual(1, row.categories.Length);
            Assert.AreEqual("pose:long-id", row.reason);
            long current = row.sequence;
            if (sequence < 0) sequence = current;
            Assert.AreEqual(sequence, current);
        }
        StringAssert.Contains("\"payloadOmitted\":true", lines[0]);
        StringAssert.Contains("\"uniqueIds\":1", lines[0]);
        Assert.AreEqual(12000, snapshot.categories[0].originalIds[0].Length, "logging must not mutate captured ownership");
    }
    sealed class Lease : IDisposable
    { readonly Action done; public Lease(Action done) { this.done = done; } public void Dispose() => done(); }
    [Test] public void RetirementReceiptSurvivesUntilDestroyAndLeaseCompletionAndThenClears()
    {
        var model = new GameObject("retired original"); Object.DestroyImmediate(model); bool completed = false;
        CollectionResidencyMetrics.AfterDestroyed("whole_towns", new UnityEngine.Object[] { model }, new[] { new Lease(() =>
        {
            var row = CollectionResidencyMetrics.Capture().categories.Single(c => c.category == "whole_towns");
            Assert.AreEqual(1, row.retiringLeases); Assert.AreEqual(0, row.pendingDestroy);
            CollectionAssert.Contains(row.originalIds, "tel-aviv-white-city-v1");
        }) }, () => completed = true, null, new[] { ("tel-aviv-white-city-v1", CollectionDetail.Far) });
        Assert.IsTrue(completed);
        var after = CollectionResidencyMetrics.Capture().categories.Single(c => c.category == "whole_towns");
        Assert.AreEqual(0, after.retiringLeases); Assert.AreEqual(0, after.originalIds.Length);
    }
    [Test] public void PartialTownNeverSuppressesFallbackAndExpiredCameraDropsEveryLease()
    {
        var old = TownModelLayer.ModelAcquire; var answers = new List<Action<GameObject?>>(); int released = 0;
        var root = new GameObject("town"); var layer = root.AddComponent<TownModelLayer>();
        var kit = new GameObject("a").AddComponent<MeshRenderer>(); kit.transform.SetParent(root.transform);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); var camera = new GameObject("camera").AddComponent<Camera>();
        try
        {
            TownModelLayer.ModelAcquire = (_, __, done) => { answers.Add(done); return new Lease(() => released++); };
            layer.ConfigureResidency(1000, 2000, 0, 0, "TEST ONLY");
            layer.InitStreaming(new[] {
                new SettlementKits.ModelInstance("a", TownModel.B01WhiteCity, Matrix4x4.identity, Vector2.zero, 0, 100, 100, 0, 100),
                new SettlementKits.ModelInstance("a", TownModel.B03aArches, Matrix4x4.identity, Vector2.one, 0, 100, 100, 0, 100) },
                new List<(Renderer, Renderer?)> { (kit, null) });
            camera.transform.position = new Vector3(0, 10000, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            layer.PrepareCamera(camera); Assert.AreEqual(2, answers.Count);
            answers[0](prefab); layer.PrepareCamera(camera); Assert.IsFalse(kit.forceRenderingOff); Assert.IsFalse(layer.ReadyForTown("a", camera));
            answers[1](prefab); layer.PrepareCamera(camera); Assert.IsTrue(layer.ReadyForTown("a", camera));
            layer.AdvanceCameraFrame(Time.frameCount + ProductionResidency.Approved().Towns.Grace + 1); layer.RetireReleasedModels();
            Assert.AreEqual(2, released); Assert.IsFalse(layer.ReadyForTown("a", camera)); Assert.IsFalse(kit.forceRenderingOff);
            Assert.AreEqual(0, layer.ResidencySnapshot().ownedLeases);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(prefab); Object.DestroyImmediate(camera.gameObject); TownModelLayer.ModelAcquire = old; }
    }
    sealed class Source : CollectionStream.ISource
    {
        public System.Collections.IEnumerator Open(string bundle, Action<string?> done) { done(null); yield break; }
        public System.Collections.IEnumerator Load(string bundle, string path, Action<GameObject?> done) { done(null); yield break; }
        public void Unload(string bundle) { }
    }
    [Test] public void ARemainingPinStaysAttributedAfterItsObservedTownLayerIsDestroyed()
    {
        // Reflection is only for the stream's private singleton, isolated/restored so this fixture cannot alter another stream.
        var field = typeof(CollectionStream).GetField("shared", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var previous = field.GetValue(null); var old = TownModelLayer.ModelAcquire;
        var stream = new CollectionStream(new Source()); var root = new GameObject("town"); var layer = root.AddComponent<TownModelLayer>();
        var kit = new GameObject("a").AddComponent<MeshRenderer>(); kit.transform.SetParent(root.transform);
        var camera = new GameObject("camera").AddComponent<Camera>();
        try
        {
            field.SetValue(null, stream); TownModelLayer.ModelAcquire = stream.Retain;
            layer.ConfigureResidency(1000, 2000, 0, 0, "TEST ONLY");
            layer.InitStreaming(new[] { new SettlementKits.ModelInstance("a", TownModel.B01WhiteCity, Matrix4x4.identity, Vector2.zero, 0, 100, 100, 0, 100) },
                new List<(Renderer, Renderer?)> { (kit, null) });
            camera.transform.position = new Vector3(0, 10000, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            layer.PrepareCamera(camera);
            var before = CollectionResidencyMetrics.Capture().categories.Single(c => c.category == "town_buildings");
            Assert.AreEqual(1, before.bundleOwners);
            stream.Enqueue("b01-white-city-v2", CollectionDetail.Near, _ => { }); // independent permanent legacy consumer
            Object.DestroyImmediate(root);
            var after = CollectionResidencyMetrics.Capture().categories.Single(c => c.category == "town_buildings");
            Assert.AreEqual(0, after.ownedLeases); Assert.AreEqual(0, after.strongCacheRefs); Assert.AreEqual(0, after.originalIds.Length);
            Assert.AreEqual(1, after.pins); CollectionAssert.Contains(after.bundleScopeIds, "b01-white-city-v2");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(camera.gameObject); TownModelLayer.ModelAcquire = old; field.SetValue(null, previous); }
    }

}
