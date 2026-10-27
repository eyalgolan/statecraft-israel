#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using Statecraft.Game.Map.Collection;

/// <summary>Last-owner eviction and cancellation must preserve other users and never resurrect an abandoned request.</summary>
public class CollectionResidencyTests
{
    const string Sheep = "figure-sheep-idle-1948-v1";
    sealed class Source : CollectionStream.ISource
    {
        public readonly List<string> Opened = new List<string>(), Unloaded = new List<string>();
        public GameObject? Prefab;
        public int FailUnloads, UnloadAttempts;
        public Action? BeforeUnload;
        public IEnumerator Open(string bundle, Action<string?> done) { Opened.Add(bundle); yield return null; done(null); }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done) { yield return null; done(Prefab); }
        // Extra method also compiles against the old ISource; the deferred red revision needs no production changes.
        public void Unload(string bundle)
        {
            UnloadAttempts++; BeforeUnload?.Invoke();
            if (FailUnloads-- > 0) throw new InvalidOperationException("throw once");
            Unloaded.Add(bundle);
        }
    }
    static IDisposable Retain(CollectionStream stream, Action<GameObject?> done)
    {
        var method = typeof(CollectionStream).GetMethod("Retain", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(method, "owned streaming requests must release their original bundle");
        return (IDisposable)method!.Invoke(stream, new object[] { Sheep, CollectionDetail.Far, done })!;
    }
    sealed class Pump
    {
        readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        public Pump(IEnumerator run) { stack.Push(run); }
        public void Step(int frames)
        {
            int seen = 0;
            while (stack.Count != 0 && seen < frames)
            {
                var top = stack.Peek();
                if (!top.MoveNext()) stack.Pop();
                else if (top.Current is IEnumerator nested) stack.Push(nested);
                else seen++;
            }
        }
    }
    [Test]
    public void SharedOwnersKeepOneBundleUntilTheLastReleaseAndCanReopenIt()
    {
        var go = new GameObject("original");
        try
        {
            var source = new Source { Prefab = go }; var stream = new CollectionStream(source); int answers = 0;
            var a = Retain(stream, _ => answers++); var b = Retain(stream, _ => answers++); var run = new Pump(stream.Run()); run.Step(20);
            Assert.AreEqual(2, answers); Assert.AreEqual(1, source.Opened.Count);
            a.Dispose(); a.Dispose(); CollectionAssert.IsEmpty(source.Unloaded);
            b.Dispose(); Assert.AreEqual(1, source.Unloaded.Count);
            var c = Retain(stream, _ => answers++); run.Step(20);
            Assert.AreEqual(2, source.Opened.Count, "cache entries retired with the bundle"); Assert.AreEqual(3, answers);
            c.Dispose(); Assert.AreEqual(2, source.Unloaded.Count);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test]
    public void CancellingBeforeFetchDoesNotLoadOrAnswer()
    {
        var source = new Source(); var stream = new CollectionStream(source); int answers = 0;
        var lease = Retain(stream, _ => answers++); lease.Dispose(); new Pump(stream.Run()).Step(20);
        Assert.AreEqual(0, answers); CollectionAssert.IsEmpty(source.Opened); CollectionAssert.IsEmpty(source.Unloaded);
    }
    [Test]
    public void ReleasingAnInflightLoadWaitsForItThenUnloadsWithoutAnswering()
    {
        var go = new GameObject("original");
        try
        {
            var source = new Source { Prefab = go }; var stream = new CollectionStream(source); int answers = 0;
            var lease = Retain(stream, _ => answers++); var run = new Pump(stream.Run()); run.Step(2);
            Assert.AreEqual(1, source.Opened.Count); lease.Dispose(); CollectionAssert.IsEmpty(source.Unloaded);
            run.Step(20); Assert.AreEqual(0, answers); Assert.AreEqual(1, source.Unloaded.Count);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test]
    public void ExistingPinnedConsumerCannotBeUnloadedByATownOwner()
    {
        var go = new GameObject("original");
        try
        {
            var source = new Source { Prefab = go }; var stream = new CollectionStream(source);
            stream.Enqueue(Sheep, CollectionDetail.Far, _ => { });
            var lease = Retain(stream, _ => { }); new Pump(stream.Run()).Step(20); lease.Dispose();
            CollectionAssert.IsEmpty(source.Unloaded, "unchanged consumers remain valid");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void AThrowingUnloadRetainsCacheAndRetriesWithoutAnotherOwner()
    {
        var go = new GameObject("original");
        try
        {
            var source = new Source { Prefab = go, FailUnloads = 1 }; var stream = new CollectionStream(source);
            var lease = Retain(stream, _ => { }); var run = new Pump(stream.Run()); run.Step(20);
            LogAssert.Expect(LogType.Warning, new Regex("Collection: could not release .*throw once"));
            lease.Dispose();
            var field = typeof(CollectionStream).GetField("loaded", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var cache = (System.Collections.IDictionary)field.GetValue(stream)!;
            Assert.AreEqual(1, cache.Count, "failed unload must remain a retryable owned resource record");
            Assert.AreEqual(1, source.UnloadAttempts); CollectionAssert.IsEmpty(source.Unloaded);
            run.Step(1);
            Assert.AreEqual(2, source.UnloadAttempts); Assert.AreEqual(1, source.Unloaded.Count);
            Assert.AreEqual(0, cache.Count, "only success retires the stream cache");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test]
    public void OwnerClearsItsAssetReferencesBeforeTheSourceUnloads()
    {
        var go = new GameObject("original");
        try
        {
            GameObject? held = null;
            var source = new Source { Prefab = go, BeforeUnload = () => Assert.IsNull(held) };
            var stream = new CollectionStream(source); var lease = Retain(stream, p => held = p);
            new Pump(stream.Run()).Step(20); Assert.AreSame(go, held);
            held = null; lease.Dispose(); Assert.AreEqual(1, source.Unloaded.Count);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
