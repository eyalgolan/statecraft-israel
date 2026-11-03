#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Object = UnityEngine.Object;
using Is = UnityEngine.TestTools.Constraints.Is;

public sealed class CollectionResidencyViewAllocationTests
{
    [Test]
    public void RepeatedObservationAndPruningAllocateNoManagedMemory()
    {
        var cameraObject = new GameObject("collection residency allocation camera", typeof(Camera));
        try
        {
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            var view = new CollectionResidencyView(new CollectionResidencyPolicy(
                100f, 110f, 100f, 110f, 2, "TEST ONLY"));
            var anchors = new List<(string key, Vector3 at, float radius)>
            {
                ("fixed", Vector3.zero, 0f),
            };

            for (int frame = 0; frame < 5; frame++)
            {
                view.Observe(camera, anchors, frame);
                Assert.AreEqual(1, view.Keys.Count);
                Assert.IsFalse(view.Prune(frame));
            }

            // Prove the recorder catches the kind of managed allocation this regression guards against.
            Assert.That(() => { var allocated = new int[1]; GC.KeepAlive(allocated); }, new AllocatingGCMemoryConstraint());
            int nextFrame = 5; bool demandStayedFixed = true, nothingWasPruned = true;
            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var before = view.Keys;
                    view.Observe(camera, anchors, nextFrame++);
                    // Explicit membership, not SetEquals: Unity's Mono HashSet.SetEquals may box an enumerator, and this
                    // guards the view's own allocations, not the class library's.
                    demandStayedFixed &= before.Count == 1 && before.Contains("fixed")
                        && view.Keys.Count == 1 && view.Keys.Contains("fixed");
                    nothingWasPruned &= !view.Prune(nextFrame - 1);
                }
            }, Is.Not.AllocatingGCMemory());
            Assert.IsTrue(demandStayedFixed, "the fixed camera keeps the same resident key");
            Assert.IsTrue(nothingWasPruned, "the observed camera remains live");
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void TheFillDelegateRunsOnlyWhenTheCameraIsCloseEnoughToScanAndAllocatesNothing()
    {
        var cameraObject = new GameObject("collection residency fill camera", typeof(Camera));
        try
        {
            var camera = cameraObject.GetComponent<Camera>();
            var view = new CollectionResidencyView(new CollectionResidencyPolicy(
                100f, 110f, 100f, 110f, 2, "TEST ONLY"));
            int fills = 0;
            Action<List<(string key, Vector3 at, float radius)>> fill = anchors => { fills++; anchors.Add(("fixed", Vector3.zero, 0f)); };

            // Too high to scan (1 km over a 100 m view enter height): the anchors are never built, as with the old lazy sources.
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1000f, 0f), Quaternion.Euler(90f, 0f, 0f));
            for (int frame = 0; frame < 3; frame++) view.Observe(camera, fill, frame);
            Assert.AreEqual(0, fills, "a camera above the view height builds no anchors");
            Assert.AreEqual(0, view.Keys.Count);

            camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            for (int frame = 3; frame < 8; frame++) view.Observe(camera, fill, frame);
            Assert.AreEqual(5, fills, "a close camera builds the anchors once per observation");
            CollectionAssert.AreEquivalent(new[] { "fixed" }, view.Keys);

            int nextFrame = 8;
            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++) { view.Observe(camera, fill, nextFrame); view.Prune(nextFrame); nextFrame++; }
            }, Is.Not.AllocatingGCMemory());
            Assert.AreEqual(105, fills);
            Assert.IsTrue(view.Keys.Count == 1 && view.Keys.Contains("fixed"));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
