#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The 1948 Collection's loader (issue #664): a request is served after the frame that made it, never at once; a
/// missing or broken bundle fails open (one warning per bundle, null for every request, the map runs on); an unknown id
/// gets null and one warning.</summary>
public class CollectionStreamTests
{
    const string Sheep = "figure-sheep-idle-1948-v1", Donkey = "figure-donkey-1948-v1", Jeep = "figure-vehicle-willys-jeep-1948-v2";

    /// <summary>Steps a coroutine (nested enumerators included) by hand; returns the number of frames (yield null) it took
    /// to reach a state where it only waits, capped at <paramref name="frames"/>.</summary>
    static void Step(IEnumerator run, int frames)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(run);
        int seen = 0;
        while (stack.Count > 0 && seen < frames)
        {
            var top = stack.Peek();
            if (!top.MoveNext()) { stack.Pop(); continue; }
            if (top.Current is IEnumerator nested) stack.Push(nested);
            else seen++;
        }
    }

    /// <summary>CollectionImport.WriteChecks (editor code, reached by reflection as CollectionImportTests does).</summary>
    static void WriteChecks(string folder, string[] bundles)
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "CollectionImport");
        try { t!.GetMethod("WriteChecks")!.Invoke(null, new object[] { folder, bundles }); }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException != null) { throw e.InnerException; }
    }

    sealed class FakeSource : CollectionStream.ISource
    {
        public readonly List<string> Opened = new List<string>();
        public string? Error;
        public GameObject? Prefab;

        public IEnumerator Open(string bundle, Action<string?> done) { Opened.Add(bundle); yield return null; done(Error); }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done) { yield return null; done(Prefab); }
    }

    [Test]
    public void ARequestIsServedAfterTheFrameThatMadeIt()
    {
        var go = new GameObject("prefab");
        try
        {
            var src = new FakeSource { Prefab = go };
            var stream = new CollectionStream(src);
            var got = new List<GameObject?>();
            stream.Enqueue(Sheep, CollectionDetail.Far, got.Add);
            var run = stream.Run();
            Assert.IsTrue(run.MoveNext(), "the first step waits a frame");
            CollectionAssert.IsEmpty(got, "nothing comes back in the frame of the request");
            CollectionAssert.IsEmpty(src.Opened, "nothing is fetched in the frame of the request");
            Step(run, 10);
            CollectionAssert.AreEqual(new[] { go }, got);
            CollectionAssert.AreEqual(new[] { CollectionCatalog.Animals }, src.Opened);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void AMissingBundleWarnsOnceAndEveryRequestForItGetsNull()
    {
        var src = new FakeSource { Error = "no file" };
        var stream = new CollectionStream(src);
        var got = new List<GameObject?>();
        var calls = 0;
        foreach (var (id, d) in new[] { (Sheep, CollectionDetail.Far), (Donkey, CollectionDetail.Near), (Sheep, CollectionDetail.Near) })
            stream.Enqueue(id, d, g => { calls++; got.Add(g); });
        LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-animals did not load \\(no file\\)"));
        var run = stream.Run();
        Step(run, 20);
        Assert.AreEqual(3, calls);
        CollectionAssert.AreEqual(new GameObject?[] { null, null, null }, got);
        CollectionAssert.AreEqual(new[] { CollectionCatalog.Animals }, src.Opened, "a failed bundle is tried once");
        stream.Enqueue(Donkey, CollectionDetail.Far, g => { calls++; got.Add(g); });
        Step(run, 5);
        Assert.AreEqual(4, calls, "a later request is still answered");
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void AThrowingCallbackWarnsOnceForItsIdAndLaterRequestsAreStillServed()
    {
        var go = new GameObject("prefab");
        try
        {
            var stream = new CollectionStream(new FakeSource { Prefab = go });
            var got = new List<string>();
            stream.Enqueue(Sheep, CollectionDetail.Far, _ => throw new InvalidOperationException("parent destroyed"));
            stream.Enqueue(Donkey, CollectionDetail.Far, g => got.Add(Donkey + (g == go ? "" : " null")));
            LogAssert.Expect(LogType.Warning, new Regex($"^Collection: the callback for '{Sheep}' threw \\(InvalidOperationException: parent destroyed\\)"));
            var run = stream.Run();
            Step(run, 20);
            CollectionAssert.AreEqual(new[] { Donkey }, got, "the request after the throwing callback is answered");
            stream.Enqueue(Sheep, CollectionDetail.Near, _ => throw new InvalidOperationException("again"));
            stream.Enqueue(Jeep, CollectionDetail.Far, g => got.Add(Jeep + (g == go ? "" : " null")));
            Step(run, 20);
            CollectionAssert.AreEqual(new[] { Donkey, Jeep }, got, "and so is every later one");
            LogAssert.NoUnexpectedReceived();   // the second throw for the same id adds no warning
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    sealed class ThrowingSource : CollectionStream.ISource
    {
        public GameObject? Prefab;
        public IEnumerator Open(string bundle, Action<string?> done)
        {
            yield return null;
            if (bundle == CollectionCatalog.Animals) throw new IOException("disk gone");
            done(null);
        }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done)
        {
            if (path.EndsWith("-near.prefab", StringComparison.Ordinal)) throw new InvalidOperationException("bad asset");
            yield return null;
            done(Prefab);
        }
    }

    [Test]
    public void ASourceThatThrowsFailsOnlyThatBundleOrPrefabAndTheStreamGoesOn()
    {
        var go = new GameObject("prefab");
        try
        {
            var stream = new CollectionStream(new ThrowingSource { Prefab = go });
            var got = new List<GameObject?>();
            foreach (var (id, d) in new[] { (Sheep, CollectionDetail.Far), (Jeep, CollectionDetail.Near), (Jeep, CollectionDetail.Far),
                         (Donkey, CollectionDetail.Far), (Jeep, CollectionDetail.Near) })
                stream.Enqueue(id, d, got.Add);
            LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-animals did not load \\(IOException: disk gone\\)"));
            LogAssert.Expect(LogType.Warning, new Regex($"^Collection: bundle collection-vehicles did not load .*{Jeep}-near.prefab \\(InvalidOperationException: bad asset\\)"));
            Step(stream.Run(), 40);
            CollectionAssert.AreEqual(new GameObject?[] { null, null, go, null, null }, got, "every request answered; only the jeep's far prefab loads");
            LogAssert.NoUnexpectedReceived();
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void ADamagedBundleFileWarnsOnceWithoutUnitysOwnError()
    {
        var folder = Path.Combine(Path.GetTempPath(), "statecraft-bad-collection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            // 10 bytes of garbage, and a bundle header whose declared size is longer than the file (a cut-short copy)
            File.WriteAllBytes(Path.Combine(folder, CollectionCatalog.Animals), System.Text.Encoding.ASCII.GetBytes("not-bundle"));
            var cut = new List<byte>(System.Text.Encoding.ASCII.GetBytes("UnityFS\0\0\0\0\x08" + "5.x.x\0" + "6000.3.25f1\0"));
            cut.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0x10, 0 });   // 4096 bytes declared
            cut.AddRange(new byte[64]);
            File.WriteAllBytes(Path.Combine(folder, CollectionCatalog.Vehicles), cut.ToArray());
            var stream = new CollectionStream(new CollectionStream.BundleSource(folder));
            var got = new List<GameObject?>();
            foreach (var (id, d) in new[] { (Sheep, CollectionDetail.Far), (Donkey, CollectionDetail.Near), (Jeep, CollectionDetail.Far) })
                stream.Enqueue(id, d, got.Add);
            LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-animals did not load \\(not an AssetBundle "));
            LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-vehicles did not load \\(a damaged AssetBundle \\(" + cut.Count + " bytes, its header says 4096\\)"));
            Step(stream.Run(), 20);
            CollectionAssert.AreEqual(new GameObject?[] { null, null, null }, got);
            LogAssert.NoUnexpectedReceived();
        }
        finally { Directory.Delete(folder, true); }
    }

    /// <summary>A bundle whose header is whole is still refused, before Unity opens it, when the checks file is missing,
    /// malformed or has no line for it: nothing is ever loaded unchecked.</summary>
    [Test]
    public void ABundleIsNeverOpenedWithoutItsLineInTheChecksFile()
    {
        var head = new List<byte>(System.Text.Encoding.ASCII.GetBytes("UnityFS\0\0\0\0\x08" + "5.x.x\0" + "6000.3.25f1\0"));
        const int size = 256;
        head.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 1, 0 });   // 256 bytes declared, and written
        head.AddRange(new byte[size - head.Count]);
        foreach (var (checks, why) in new[]
                 {
                     ((string?)null, "no file .*" + CollectionCatalog.ChecksFile),
                     ("collection-animals 12 x " + new string('0', 64) + "\n", "a damaged " + CollectionCatalog.ChecksFile),
                     ("collection-people 12 256 " + new string('0', 64) + "\n", "not in " + CollectionCatalog.ChecksFile),
                 })
        {
            var folder = Path.Combine(Path.GetTempPath(), "statecraft-unchecked-collection-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllBytes(Path.Combine(folder, CollectionCatalog.Animals), head.ToArray());
                Assert.IsNull(CollectionStream.BundleSource.CheckHeader(Path.Combine(folder, CollectionCatalog.Animals)));
                if (checks != null) File.WriteAllText(Path.Combine(folder, CollectionCatalog.ChecksFile), checks);
                var stream = new CollectionStream(new CollectionStream.BundleSource(folder));
                var got = new List<GameObject?>();
                stream.Enqueue(Sheep, CollectionDetail.Far, got.Add);
                stream.Enqueue(Donkey, CollectionDetail.Near, got.Add);
                LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-animals did not load \\(" + why));
                Step(stream.Run(), 20);
                CollectionAssert.AreEqual(new GameObject?[] { null, null }, got, why);
                LogAssert.NoUnexpectedReceived();
            }
            finally { Directory.Delete(folder, true); }
        }
    }

    [Test]
    public void AnUnknownIdGetsNullAndOneWarning()
    {
        var stream = new CollectionStream(new FakeSource());
        var calls = new List<GameObject?>();
        LogAssert.Expect(LogType.Warning, new Regex("^Collection: no model 'b07-kibbutz-dining-hall-v2'"));
        Assert.IsFalse(stream.Enqueue("b07-kibbutz-dining-hall-v2", CollectionDetail.Far, calls.Add));
        Assert.IsFalse(stream.Enqueue("b07-kibbutz-dining-hall-v2", CollectionDetail.Near, calls.Add));
        CollectionAssert.AreEqual(new GameObject?[] { null, null }, calls);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void TheBuildsBundleSourceFailsOpenWhenTheBundleFolderIsMissing()
    {
        var folder = Path.Combine(Path.GetTempPath(), "statecraft-no-collection-" + Guid.NewGuid().ToString("N"));
        Assert.IsFalse(Directory.Exists(folder));
        var stream = new CollectionStream(new CollectionStream.BundleSource(folder));
        var got = new List<GameObject?>();
        stream.Enqueue(Jeep, CollectionDetail.Far, got.Add);
        stream.Enqueue(Jeep, CollectionDetail.Near, got.Add);
        LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-vehicles did not load \\(no file .*collection-vehicles\\)"));
        Step(stream.Run(), 10);
        CollectionAssert.AreEqual(new GameObject?[] { null, null }, got);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void TheEditorSourceServesTheProjectsPrefabs()
    {
        var stream = new CollectionStream(new CollectionStream.EditorSource());
        var got = new List<GameObject?>();
        stream.Enqueue(Sheep, CollectionDetail.Near, got.Add);
        stream.Enqueue(Jeep, CollectionDetail.Far, got.Add);
        Step(stream.Run(), 10);
        Assert.AreEqual(2, got.Count);
        Assert.AreEqual(Sheep + "-near", got[0]?.name);
        Assert.AreEqual(Jeep + "-far", got[1]?.name);
    }

    /// <summary>A built bundle with one byte flipped in the middle of its data (its header and size intact) is refused
    /// before Unity reads it: one warning, every request for it gets null, and nothing from Unity's reader.</summary>
    [UnityTest]
    public IEnumerator ABundleDamagedInsideItsDataWarnsOnceAndIsNeverRead()
    {
        var folder = Path.Combine(Path.GetTempPath(), "statecraft-flipped-collection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var animals = CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.Animals);
        var build = new AssetBundleBuild
        {
            assetBundleName = animals.Bundle,
            assetNames = animals.Ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near) }).ToArray(),
        };
        var manifest = BuildPipeline.BuildAssetBundles(folder, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            EditorUserBuildSettings.activeBuildTarget);
        Assert.IsNotNull(manifest, "bundle build");
        WriteChecks(folder, manifest.GetAllAssetBundles());
        var file = Path.Combine(folder, animals.Bundle);
        var bytes = File.ReadAllBytes(file);
        bytes[bytes.Length / 2] ^= 0xFF;
        File.WriteAllBytes(file, bytes);
        Assert.IsNull(CollectionStream.BundleSource.CheckHeader(file), "the header and size are intact");
        var source = new CollectionStream.BundleSource(folder);
        try
        {
            var stream = new CollectionStream(source);
            var got = new List<GameObject?>();
            foreach (var id in animals.Ids)
                foreach (var d in new[] { CollectionDetail.Far, CollectionDetail.Near })
                    stream.Enqueue(id, d, got.Add);
            LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-animals did not load \\(it does not match the build that made it"));
            var stack = new Stack<IEnumerator>();
            stack.Push(stream.Run());
            for (int frame = 0; frame < 2000 && got.Count < animals.Ids.Length * 2; frame++)
            {
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    if (!top.MoveNext()) { stack.Pop(); continue; }
                    if (top.Current is IEnumerator nested) { stack.Push(nested); continue; }
                    if (top.Current is AsyncOperation op) while (!op.isDone) yield return null;
                    break;
                }
                yield return null;
            }
            Assert.AreEqual(animals.Ids.Length * 2, got.Count, "every request answered");
            Assert.IsTrue(got.All(g => g == null), "a damaged bundle serves nothing");
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            source.UnloadAll();
            Directory.Delete(folder, true);
        }
    }

    /// <summary>End to end: the animals bundle, built from the catalog for this editor's target, opens from disk through
    /// the build's own source and hands back the real prefabs, the sheep's near one still skinned and animated.</summary>
    [UnityTest]
    public IEnumerator ABuiltBundleServesItsPrefabsThroughTheBuildsSource()
    {
        var folder = Path.Combine(Path.GetTempPath(), "statecraft-collection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var animals = CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.Animals);
        var build = new AssetBundleBuild
        {
            assetBundleName = animals.Bundle,
            assetNames = animals.Ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near) }).ToArray(),
        };
        var manifest = BuildPipeline.BuildAssetBundles(folder, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            EditorUserBuildSettings.activeBuildTarget);
        Assert.IsNotNull(manifest, "bundle build");
        WriteChecks(folder, manifest.GetAllAssetBundles());
        var source = new CollectionStream.BundleSource(folder);
        try
        {
            var stream = new CollectionStream(source);
            var got = new List<GameObject?>();
            stream.Enqueue(Sheep, CollectionDetail.Near, got.Add);
            stream.Enqueue(Donkey, CollectionDetail.Far, got.Add);
            var run = stream.Run();
            var stack = new Stack<IEnumerator>();
            stack.Push(run);
            for (int frame = 0; frame < 2000 && got.Count < 2; frame++)
            {
                // step until the coroutine waits on a frame or an operation, then let the editor run a frame
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    if (!top.MoveNext()) { stack.Pop(); continue; }
                    if (top.Current is IEnumerator nested) { stack.Push(nested); continue; }
                    if (top.Current is AsyncOperation op) while (!op.isDone) yield return null;
                    break;
                }
                yield return null;
            }
            Assert.AreEqual(2, got.Count, "both requests answered");
            Assert.AreEqual(Sheep + "-near", got[0]?.name);
            Assert.IsNotNull(got[0]!.GetComponentInChildren<SkinnedMeshRenderer>(true), "skinned");
            Assert.IsNotNull(got[0]!.GetComponentInChildren<Animator>(true)?.runtimeAnimatorController, "idle controller");
            Assert.AreEqual(Donkey + "-far", got[1]?.name);
            Assert.IsNotNull(got[1]!.GetComponentInChildren<MeshRenderer>(true)?.sharedMaterial, "material");
            var built = Path.Combine(folder, animals.Bundle);
            Assert.IsNull(CollectionStream.BundleSource.CheckHeader(built), "a whole built bundle passes the header check");
            var cut = built + "-cut";
            var bytes = File.ReadAllBytes(built);
            File.WriteAllBytes(cut, bytes.Take(bytes.Length / 2).ToArray());
            StringAssert.StartsWith("a damaged AssetBundle", CollectionStream.BundleSource.CheckHeader(cut), "a cut-short copy does not");
        }
        finally
        {
            source.UnloadAll();
            Directory.Delete(folder, true);
        }
    }
}
