#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEngine;

public class CollectionResidencyDiagnosticsTests
{
    sealed class Source : CollectionStream.ISource
    {
        public IEnumerator Open(string bundle, Action<string?> done) { yield return null; done(null); }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done) { yield return null; done(null); }
        public void Unload(string bundle) { }
    }
    [Test]
    public void DiagnosticsDistinguishPinsOwnersAndQueuedWorkWithoutMutatingTheStream()
    {
        const string id = "figure-sheep-idle-1948-v1";
        var stream = new CollectionStream(new Source());
        using (var owner = stream.Retain(id, CollectionDetail.Far, _ => { }))
        {
            var before = stream.Diagnostics();
            Assert.AreEqual(1, before.Count); Assert.AreEqual("collection-" + id, before[0].Bundle);
            Assert.AreEqual(1, before[0].Owners); Assert.AreEqual(1, before[0].PendingRequests);
            Assert.IsFalse(before[0].Pinned); Assert.IsFalse(before[0].InFlight);
            stream.Enqueue(id, CollectionDetail.Near, _ => { });
            var after = stream.Diagnostics(); Assert.IsTrue(after[0].Pinned);
            Assert.AreEqual(2, after[0].PendingRequests); Assert.AreEqual(1, before[0].PendingRequests, "detached snapshot");
        }
        Assert.AreEqual(0, stream.Diagnostics()[0].Owners);
        Assert.IsTrue(stream.Diagnostics()[0].Pinned, "diagnostics never remove a legacy pin");
    }
}
