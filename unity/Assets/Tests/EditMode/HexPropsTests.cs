#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.HexTiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The hex tiles' 20 Higgsfield props (Tal Heuberger, #1155) load under the owner's ruling of 23 October 2026,
/// "Own streamed bundle": outside every Resources folder (so the default first download does not carry them), outside
/// The 1948 Collection (which stays Tal's Tripo models), and in a bundle of their own that only a ?hex-tiles board asks for.</summary>
public class HexPropsTests
{
    const string Art = "Assets/Art/HexTiles/";

    /// <summary>The bundle's contents, pinned: one prefab per prop the tiles name, beside its model.</summary>
    static readonly string[] Prefabs =
    {
        "Assets/Art/HexTiles/arab_village/minaret.prefab", "Assets/Art/HexTiles/arab_village/prickly_pear.prefab",
        "Assets/Art/HexTiles/caesarea_ruins/arches.prefab", "Assets/Art/HexTiles/caesarea_ruins/boulders.prefab",
        "Assets/Art/HexTiles/caesarea_ruins/jetty.prefab",
        "Assets/Art/HexTiles/coastal_dunes/scrub.prefab",
        "Assets/Art/HexTiles/desert_edge/tent.prefab",
        "Assets/Art/HexTiles/jaffa_old_city/belfry.prefab", "Assets/Art/HexTiles/jaffa_old_city/clock_tower.prefab",
        "Assets/Art/HexTiles/jaffa_old_city/sea_minaret.prefab",
        "Assets/Art/HexTiles/judean_highlands/garrigue.prefab",
        "Assets/Art/HexTiles/railway_halt/crates.prefab", "Assets/Art/HexTiles/railway_halt/eucalyptus.prefab",
        "Assets/Art/HexTiles/railway_halt/packing_house.prefab", "Assets/Art/HexTiles/railway_halt/station.prefab",
        "Assets/Art/HexTiles/railway_halt/well.prefab",
        "Assets/Art/HexTiles/village_mill/mill.prefab", "Assets/Art/HexTiles/village_mill/sycamore_fig.prefab",
        "Assets/Art/HexTiles/village_mill/terrace_wall.prefab", "Assets/Art/HexTiles/village_mill/threshing_floor.prefab",
    };

    static bool UnderResources(string path) => path.Split('/').Any(s => s.Equals("Resources", StringComparison.OrdinalIgnoreCase));

    static AssetBundleBuild[] Builds()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.HexPropsBundle, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "HexPropsBundle");
        return (AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!;
    }

    [Test]
    public void ThePropsAreNotInResourcesAndNotInTheCollection()
    {
        Assert.AreEqual(20, HexProps.Ids.Count);
        foreach (var id in HexProps.Ids)
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(id)), id);
            Assert.IsFalse(UnderResources(HexProps.PrefabPath(id)), id);
            Assert.IsNull(Resources.Load<GameObject>(HexModels.PathOf(id)), id + " loads from Resources");
            Assert.IsNull(CollectionCatalog.BundleOf(id), id + " is in The 1948 Collection");
        }
        // Nothing a Resources folder ships (the first download) pulls in a prop, its model, material or colour map.
        var resources = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && UnderResources(p)).ToArray();
        Assert.That(resources, Is.Not.Empty);
        CollectionAssert.IsEmpty(AssetDatabase.GetDependencies(resources, true).Where(d => d.StartsWith(Art, StringComparison.Ordinal)),
            "Resources assets that depend on the hex props");
        // The Collection's bundles carry none of them, and the two streams share no folder or checks file.
        var collection = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(collection, "CollectionImport");
        var collectionBuilds = (AssetBundleBuild[])collection!.GetMethod("Builds")!.Invoke(null, null)!;
        CollectionAssert.IsEmpty(AssetDatabase.GetDependencies(collectionBuilds.SelectMany(b => b.assetNames).ToArray(), true)
            .Where(d => d.StartsWith(Art, StringComparison.Ordinal)), "Collection bundles that carry a hex prop");
        CollectionAssert.DoesNotContain(CollectionCatalog.Bundles.Select(b => b.Bundle), HexProps.Bundle);
        Assert.AreNotEqual(CollectionCatalog.StreamingFolder, HexProps.StreamingFolder);
        Assert.AreNotEqual(CollectionCatalog.ChecksFile, HexProps.ChecksFile);
    }

    [Test]
    public void TheHexPropsBundleCarriesTheTwentyPropsAndNothingElse()
    {
        var builds = Builds();
        Assert.AreEqual(1, builds.Length);
        Assert.AreEqual("hex-props", builds[0].assetBundleName);
        CollectionAssert.AreEqual(Prefabs, builds[0].assetNames);
        CollectionAssert.AreEqual(Prefabs, HexProps.Ids.Select(HexProps.PrefabPath));
        // Every prefab under the props' folder is in the bundle (none is left behind), and every dependency stays in it.
        var found = AssetDatabase.FindAssets("t:Prefab", new[] { Art.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath);
        CollectionAssert.AreEquivalent(Prefabs, found);
        var outside = AssetDatabase.GetDependencies(Prefabs, true).Where(d => !d.StartsWith(Art, StringComparison.Ordinal)
            && !d.StartsWith("Packages/com.unity.render-pipelines.", StringComparison.Ordinal)).ToArray();
        CollectionAssert.IsEmpty(outside, "the hex-props bundle pulls in files from outside Assets/Art/HexTiles");
        // Each prop is named by a tile: the ids are the "tiles:" ids the prototypes place.
        foreach (var id in HexProps.Ids) StringAssert.StartsWith("tiles:", id);
    }

    [Test]
    public void TheCollectionsLoaderServesThePropsFromTheirOwnCatalog()
    {
        var stream = new CollectionStream(new CollectionStream.EditorSource(), HexProps.Catalog);
        var got = new List<GameObject?>();
        foreach (var id in HexProps.Ids) stream.Enqueue(id, CollectionDetail.Far, got.Add);
        LogAssert.Expect(LogType.Warning, new Regex("^Collection: no model 'figure-sheep-idle-1948-v1' in the hex-tile props"));
        Assert.IsFalse(stream.Enqueue("figure-sheep-idle-1948-v1", CollectionDetail.Far, got.Add), "a Collection model is not a prop");
        Step(stream.Run(), 100);
        Assert.AreEqual(21, got.Count);
        Assert.IsNull(got[0], "the Collection id, answered at once");
        CollectionAssert.AreEqual(Prefabs.Select(p => System.IO.Path.GetFileNameWithoutExtension(p)), got.Skip(1).Select(g => g?.name));
        LogAssert.NoUnexpectedReceived();
    }

    /// <summary>On a board that streams, a prop draws its stand-in (no Resources prefab) until its bundle answers, asked for
    /// once by its own id through the props' stream (never the Collection's), and then the streamed prefab.</summary>
    [Test]
    public void ABoardAsksThePropsStreamAndDrawsTheStandInUntilItAnswers()
    {
        var props = new List<(string id, Action<GameObject?> done)>();
        var figures = new List<string>();
        HexCollectionFigures.PropSource = (id, _, done) => props.Add((id, done));
        HexCollectionFigures.Source = (id, _, __) => figures.Add(id);
        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            const string id = "tiles:railway_halt/station";
            Assert.IsTrue(HexCollectionFigures.Streams(id));
            int changes = 0;
            using var models = new HexCollectionFigures(() => changes++);
            Assert.IsNull(models.Prefab(id), "the stand-in while pending");
            Assert.IsNull(models.Prefab(id));
            Assert.AreEqual(1, props.Count); Assert.AreEqual(id, props[0].id);
            CollectionAssert.IsEmpty(figures, "a prop is never asked of The 1948 Collection");
            props[0].done(model);
            Assert.AreSame(model, models.Prefab(id));
            Assert.AreEqual(1, changes);
        }
        finally
        {
            HexCollectionFigures.PropSource = null; HexCollectionFigures.Source = null;
            UnityEngine.Object.DestroyImmediate(model);
        }
        // Without a stream (the editor, the painter) the prop draws from the AssetDatabase.
        Assert.IsFalse(HexCollectionFigures.Streams("tiles:railway_halt/station"));
        HexModels.Clear();
        Assert.AreEqual("station", HexModels.Prefab("tiles:railway_halt/station")?.name);
    }

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
}
