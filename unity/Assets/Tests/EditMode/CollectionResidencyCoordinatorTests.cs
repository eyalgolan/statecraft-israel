#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using Object = UnityEngine.Object;

public class CollectionResidencyCoordinatorTests
{
    sealed class Lease : IDisposable
    {
        public int Releases; public void Dispose() { Releases++; Assert.AreEqual(1, Releases); }
    }
    [Test]
    public void ReentryWaitsForRetirementAndOldCallbacksCannotAnswerNewDemand()
    {
        var loaded = new List<string>(); var leases = new List<Lease>(); var answers = new List<Action<GameObject?>>();
        Action? acknowledge = null; int callbacks = 0, clears = 0;
        var residency = new CollectionResidency((id, detail, answer) =>
        { loaded.Add(id); answers.Add(answer); var lease = new Lease(); leases.Add(lease); return lease; },
        () => { clears++; return Array.Empty<Object>(); }, (_, __, ___) => callbacks++,
        (instances, owners, keys, complete) =>
        {
            if (owners.Count == 0) { complete(); return; }
            acknowledge = () => { foreach (var owner in owners) owner.Dispose(); complete(); };
        });
        residency.Replace(new[] { ("a", CollectionDetail.Far) });
        Assert.AreEqual(1, residency.OwnerCount);
        residency.Replace(new[] { ("b", CollectionDetail.Far) });
        Assert.IsTrue(residency.IsRetiring); CollectionAssert.AreEqual(new[] { "a" }, loaded);
        answers[0](null); Assert.AreEqual(0, callbacks);
        residency.Replace(new[] { ("c", CollectionDetail.Near) }); // latest focus replaces a pending one
        acknowledge!();
        CollectionAssert.AreEqual(new[] { "a", "c" }, loaded);
        Assert.AreEqual(1, leases[0].Releases);
        answers[0](null); answers[1](null); Assert.AreEqual(1, callbacks);
        residency.Dispose(); acknowledge!(); Assert.AreEqual(1, leases[1].Releases);
        Assert.GreaterOrEqual(clears, 2);
    }
    [Test]
    public void DisposeDuringRetirementNeverAdmitsPendingDemand()
    {
        int acquired = 0; Action? retire = null;
        var r = new CollectionResidency((_, __, ___) => { acquired++; return new Lease(); },
            () => Array.Empty<Object>(), (_, __, ___) => { },
            (_, owners, __, complete) =>
            {
                if (owners.Count == 0) complete();
                else retire = () => { foreach (var lease in owners) lease.Dispose(); complete(); };
            });
        r.Replace(new[] { ("a", CollectionDetail.Far) }); r.Replace(new[] { ("b", CollectionDetail.Far) });
        r.Dispose(); r.Dispose(); retire!(); Assert.AreEqual(1, acquired);
    }
    [Test]
    public void TwoCamerasKeepAUnionUntilTheirOwnDemandExpires()
    {
        var policy = new CollectionResidencyPolicy(1, 2, 100, 110, 1, "TEST ONLY");
        var view = new CollectionResidencyView(policy);
        var a = new GameObject("a").AddComponent<Camera>(); var b = new GameObject("b").AddComponent<Camera>();
        try
        {
            a.transform.position = new Vector3(0, 10, 0); b.transform.position = new Vector3(1000, 10, 0);
            a.transform.rotation = b.transform.rotation = Quaternion.Euler(90, 0, 0);
            var anchors = new[] { ("a", Vector3.zero, 0f), ("b", new Vector3(1000, 0, 0), 0f) };
            view.Observe(a, anchors, 0); view.Observe(b, anchors, 0);
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, view.Keys);
            a.enabled = false; Assert.IsTrue(view.Prune(0)); CollectionAssert.AreEquivalent(new[] { "b" }, view.Keys);
            Assert.IsTrue(view.Prune(2)); Assert.IsEmpty(view.Keys);
        }
        finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
    }
    [Test]
    public void NoPolicyNumberHasAnImplicitDefault()
    {
        Assert.Throws<ArgumentException>(() => new CollectionResidencyPolicy(float.NaN, 2, 3, 4, 0, "TEST ONLY"));
        Assert.Throws<ArgumentException>(() => new CollectionResidencyPolicy(2, 1, 3, 4, 0, "TEST ONLY"));
        Assert.Throws<ArgumentException>(() => new CollectionResidencyPolicy(1, 2, 3, 4, -1, "TEST ONLY"));
        Assert.Throws<ArgumentException>(() => new CollectionResidencyPolicy(1, 2, 3, 4, 0, ""));
    }

    [Test]
    public void AddingNearKeepsTheStillNeededFarLeaseAndCallback()
    {
        var keys = new List<CollectionDetail>(); int clears = 0;
        var r = new CollectionResidency((_, detail, __) => { keys.Add(detail); return new Lease(); },
            () => { clears++; return Array.Empty<Object>(); }, (_, __, ___) => { },
            (_, owners, __, done) => { foreach (var owner in owners) owner.Dispose(); done(); });
        r.Replace(new[] { ("a", CollectionDetail.Far) });
        r.Replace(new[] { ("a", CollectionDetail.Far), ("a", CollectionDetail.Near) });
        CollectionAssert.AreEqual(new[] { CollectionDetail.Far, CollectionDetail.Near }, keys);
        Assert.AreEqual(0, clears); Assert.AreEqual(2, r.OwnerCount); r.Dispose();
    }

    [Test]
    public void LegacyEmptyOwnershipKeepsItsOriginalRetirementContract()
    {
        var live = new GameObject("TEST ONLY unowned consumer");
        try
        {
            Assert.DoesNotThrow(() => CollectionRetirement.AfterDestroyed(new Object[] { live }, Array.Empty<IDisposable>()));
            Assert.Throws<InvalidOperationException>(() => CollectionRetirement.AfterDestroyed(new Object[] { live },
                Array.Empty<IDisposable>(), () => Assert.Fail("not destroyed"), null));
        }
        finally { Object.DestroyImmediate(live); }
    }
}
