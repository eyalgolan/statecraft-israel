#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>The 1948 Collection's 31 non-building models (issue #664; Tal's Tripo models from #507, slimmed by
/// tools/slim_collection.py under the owner's ruling of 15 October 2026, "Plain git, slimmed first") are imported to load
/// light (the maintainer's gate: "we need to make sure this is built in a way that doesnt make the game extremely hard to
/// load"): nothing under a Resources folder; each model a far mesh of at most 1,500 triangles (4,500 for a vehicle, ship
/// or aircraft) with no rig or clip and a near mesh of at most 8,000, skinned with its one idle clip only on the 13 models that have one; compressed,
/// non-readable meshes; crunched 512 px colour maps; instanced materials; and each model's two prefabs in its category's
/// streamed bundle, with every dependency inside the collection's own folder (CollectionImport.Run sets all of this).</summary>
public class CollectionImportTests
{
    const string Folder = CollectionCatalog.Folder;
    const int MaxTextureSize = 512, FarTriangles = 1500, FarTrianglesVehicle = 4500, NearTriangles = 8000;

    static string Dir(string id) => $"{Folder}/{id}";
    static string Model(string id, string level) => $"{Dir(id)}/{id}-{level}.fbx";
    static string ColourMap(string id) => $"{Dir(id)}/{id}_basecolor.jpg";
    static IEnumerable<string> Ids => CollectionCatalog.Ids;
    static bool Animated(string id) => CollectionCatalog.Animated.Contains(id);

    static bool UnderResources(string path) => path.Split('/').Any(s => s.Equals("Resources", StringComparison.OrdinalIgnoreCase));

    /// <summary>Found by #668's WebGL build: every Build.* ran BuildBundles, which copied the bundles into
    /// StreamingAssets/collection without their .manifest files and then asked Unity for each one's CRC there, which Unity
    /// reads from the .manifest: "no CRC for the bundle", and no player build since #680. The real path, for the editor's
    /// build target: the shipped folder holds the bundles and a checks file whose every line matches its bundle.</summary>
    [Test]
    public void BuildBundlesWritesTheChecksBesideTheShippedBundles()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "CollectionImport");
        try { t!.GetMethod("BuildBundles")!.Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget }); }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException != null) { throw e.InnerException; }
        var dest = System.IO.Path.Combine(Application.streamingAssetsPath, CollectionCatalog.StreamingFolder);
        var checks = CollectionCatalog.ParseChecks(System.IO.File.ReadAllText(System.IO.Path.Combine(dest, CollectionCatalog.ChecksFile)));
        Assert.IsNotNull(checks, "a well-formed checks file");
        CollectionAssert.AreEquivalent(CollectionCatalog.Bundles.Select(b => b.Bundle), checks!.Keys);
        var cache = (string)t.GetMethod("BundleCache")!.Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget })!;
        foreach (var (bundle, check) in checks)
        {
            var file = System.IO.Path.Combine(dest, bundle);
            Assert.IsTrue(BuildPipeline.GetCRCForAssetBundle(System.IO.Path.Combine(cache, bundle), out var crc), bundle);
            Assert.AreEqual(crc, check.Crc, $"{bundle}: Unity's CRC, from the bundle build's own manifest");
            Assert.AreEqual(new System.IO.FileInfo(file).Length, check.Bytes, bundle);
            Assert.AreEqual(CollectionCatalog.Sha256(file), check.Sha256, bundle);
            Assert.IsFalse(System.IO.File.Exists(file + ".manifest"), "the build ships no .manifest files");
        }
    }

    [Test]
    public void TheCatalogHasThe31ModelsOfIssue664InThreeBundles()
    {
        var ids = Ids.ToList();
        Assert.AreEqual(31, ids.Count);
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), "an id in two bundles");
        CollectionAssert.AreEqual(new[] { 12, 13, 6 }, CollectionCatalog.Bundles.Select(b => b.Ids.Length), "people, vehicles, animals");
        Assert.AreEqual(13, CollectionCatalog.Animated.Count);
        CollectionAssert.IsSubsetOf(CollectionCatalog.Animated, ids);
        CollectionAssert.DoesNotContain(CollectionCatalog.Animated, "figure-c05-pioneer-veteran-v2", "C05's only idle is a walk");
        Assert.AreEqual(CollectionCatalog.Vehicles, CollectionCatalog.BundleOf("vehicle-altalena-lst-1948-v2wl"));
        Assert.IsNull(CollectionCatalog.BundleOf("b07-kibbutz-dining-hall-v2"), "the buildings are #659's");
    }

    [Test]
    public void TheFolderHoldsExactlyTheShippedFilesOfThe31Models()
    {
        var expected = Ids.SelectMany(id =>
        {
            var files = new List<string>
            {
                Model(id, "far"), Model(id, "near"), $"{Dir(id)}/{id}.mat", ColourMap(id),
                CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near),
            };
            if (Animated(id)) files.Add($"{Dir(id)}/{id}-near.controller");
            return files;
        }).OrderBy(p => p, StringComparer.Ordinal);
        var found = AssetDatabase.FindAssets("", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p)).Distinct().OrderBy(p => p, StringComparer.Ordinal);
        CollectionAssert.AreEqual(expected, found);
    }

    [Test]
    public void NothingOfTheCollectionIsUnderAResourcesFolder()
    {
        Assert.IsFalse(UnderResources(Folder), Folder);
        var all = AssetDatabase.FindAssets("", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Assert.Greater(all.Length, 0);
        CollectionAssert.IsEmpty(all.Where(UnderResources));
    }

    static ModelImporter Importer(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        Assert.IsNotNull(imp, path);
        Assert.AreEqual(ModelImporterMeshCompression.Medium, imp!.meshCompression, path + " mesh compression");
        Assert.IsFalse(imp.isReadable, path + " Read/Write");
        Assert.IsFalse(imp.importBlendShapes, path + " blend shapes");
        Assert.IsFalse(imp.importVisibility, path + " visibility");
        Assert.IsFalse(imp.importCameras, path + " cameras");
        Assert.IsFalse(imp.importLights, path + " lights");
        Assert.IsFalse(imp.addCollider, path + " colliders");
        Assert.IsFalse(imp.generateSecondaryUV, path + " lightmap UVs");
        Assert.AreEqual(ModelImporterTangents.None, imp.importTangents, path + " tangents (no normal map)");
        Assert.AreEqual(ModelImporterMaterialImportMode.None, imp.materialImportMode, path + " materials (the .mat beside it)");
        return imp;
    }

    static long Triangles(Mesh mesh) => Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i)) / 3;

    static void AssertStatic(string path, int maxTriangles)
    {
        var imp = Importer(path);
        Assert.IsFalse(imp.importAnimation, path + " animation");
        Assert.AreEqual(ModelImporterAnimationType.None, imp.animationType, path + " rig");
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var meshes = assets.OfType<Mesh>().ToArray();
        Assert.AreEqual(1, meshes.Length, path + " one mesh (parts joined)");
        Assert.That(Triangles(meshes[0]), Is.InRange(1, maxTriangles), path + " triangles");
        Assert.IsFalse(meshes[0].isReadable, path + " mesh readable");
        Assert.IsFalse(meshes[0].HasVertexAttribute(VertexAttribute.Tangent), path + " tangent stream");
        CollectionAssert.IsEmpty(assets.OfType<AnimationClip>(), path + " clips");
        CollectionAssert.IsEmpty(assets.OfType<Avatar>(), path + " avatar");
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        CollectionAssert.IsEmpty(root.GetComponentsInChildren<Component>(true)
            .Where(c => !(c is Transform || c is MeshFilter || c is MeshRenderer)), path + " other components");
    }

    [Test]
    public void EveryFarModelIsOneStaticMeshOfAtMost1500TrianglesOr4500ForAVehicle()
    {
        foreach (var id in Ids)
            AssertStatic(Model(id, "far"), CollectionCatalog.BundleOf(id) == CollectionCatalog.Vehicles ? FarTrianglesVehicle : FarTriangles);
    }

    [Test]
    public void EveryNearModelIsAtMost8000TrianglesAndOnlyTheAnimatedOnesKeepARigAndTheirIdleClip()
    {
        foreach (var id in Ids)
        {
            var path = Model(id, "near");
            if (!Animated(id)) { AssertStatic(path, NearTriangles); continue; }
            var imp = Importer(path);
            Assert.AreEqual(ModelImporterAnimationType.Generic, imp.animationType, path + " rig");
            Assert.IsTrue(imp.importAnimation, path + " animation");
            Assert.IsTrue(imp.optimizeGameObjects, path + " optimize game objects");
            Assert.AreEqual(ModelImporterAnimationCompression.Optimal, imp.animationCompression, path + " clip compression");
            Assert.AreEqual(1, imp.clipAnimations.Length, path + " clips");
            Assert.AreEqual("idle", imp.clipAnimations[0].name, path);
            Assert.IsTrue(imp.clipAnimations[0].loopTime, path + " loops");

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            Assert.AreEqual(1, clips.Length, path + " one clip");
            Assert.AreEqual("idle", clips[0].name, path);
            Assert.Greater(clips[0].length, 1f, path + " idle length");
            var meshes = assets.OfType<Mesh>().ToArray();
            Assert.AreEqual(1, meshes.Length, path + " meshes");
            Assert.That(Triangles(meshes[0]), Is.InRange(1, NearTriangles), path + " triangles");
            Assert.IsFalse(meshes[0].isReadable, path + " mesh readable");
            Assert.Greater(meshes[0].bindposeCount, 10, path + " the cut mesh is still skinned");
            Assert.IsTrue(meshes[0].HasVertexAttribute(VertexAttribute.BlendWeight), path + " vertex weights");
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.IsNotNull(skin, path + " skinned renderer");
        }
    }

    [Test]
    public void EveryColourMapIsCrunchedMipmappedAndAtMost512OnEveryTarget()
    {
        foreach (var id in Ids)
        {
            var path = ColourMap(id);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp, path);
            Assert.AreEqual(MaxTextureSize, imp!.maxTextureSize, path + " max size");
            Assert.IsTrue(imp.crunchedCompression, path + " crunch");
            Assert.AreNotEqual(TextureImporterCompression.Uncompressed, imp.textureCompression, path + " compression");
            Assert.IsTrue(imp.mipmapEnabled, path + " mipmaps");
            Assert.IsTrue(imp.sRGBTexture, path + " sRGB");
            Assert.IsFalse(imp.isReadable, path + " Read/Write");
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var s = imp.GetPlatformTextureSettings(target);
                Assert.LessOrEqual(s.maxTextureSize, MaxTextureSize, path + " " + target);
                Assert.IsTrue(s.crunchedCompression, path + " " + target + " crunch");
                Assert.AreNotEqual(TextureImporterCompression.Uncompressed, s.textureCompression, path + " " + target);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.LessOrEqual(Mathf.Max(tex.width, tex.height), MaxTextureSize, path + " imported size");
            Assert.Greater(tex.mipmapCount, 1, path + " imported mipmaps");
            StringAssert.Contains("Crunched", tex.format.ToString(), path + " imported format");
        }
    }

    [Test]
    public void EveryPrefabDrawsItsOwnInstancedMaterialAndOnlyTheAnimatedNearOnesPlayTheIdleClip()
    {
        foreach (var id in Ids)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Dir(id)}/{id}.mat");
            Assert.IsNotNull(mat, id);
            Assert.AreEqual("Universal Render Pipeline/Lit", mat.shader.name, id);
            Assert.IsTrue(mat.enableInstancing, id + " GPU instancing");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(ColourMap(id)), mat.GetTexture("_BaseMap"), id + " colour map");
            foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var path = CollectionCatalog.PrefabPath(id, detail);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.AreEqual(1, renderers.Length, path + " renderers");
                CollectionAssert.AreEqual(new[] { mat }, renderers[0].sharedMaterials, path + " material");
                var animator = prefab.GetComponentInChildren<Animator>(true);
                if (detail == CollectionDetail.Near && Animated(id))
                {
                    Assert.IsInstanceOf<SkinnedMeshRenderer>(renderers[0], path);
                    Assert.IsNotNull(animator, path + " animator");
                    var ctrl = animator!.runtimeAnimatorController as AnimatorController;
                    Assert.IsNotNull(ctrl, path + " controller");
                    Assert.AreEqual(AssetDatabase.GetAssetPath(ctrl), $"{Dir(id)}/{id}-near.controller", path);
                    var clip = ctrl!.layers[0].stateMachine.defaultState.motion as AnimationClip;
                    Assert.AreEqual("idle", clip?.name, path + " default state");
                    Assert.AreEqual(Model(id, "near"), AssetDatabase.GetAssetPath(clip), path + " clip source");
                    Assert.AreEqual(AnimatorCullingMode.CullCompletely, animator.cullingMode, path + " culling");
                }
                else
                {
                    Assert.IsInstanceOf<MeshRenderer>(renderers[0], path);
                    Assert.IsNull(animator, path + " no animator");
                }
            }
        }
    }

    [Test]
    public void EachBundleCarriesItsModelsTwoPrefabsAndNothingFromOutsideTheCollection()
    {
        foreach (var (bundle, ids) in CollectionCatalog.Bundles)
        {
            var entries = ids.SelectMany(id => new[] { CollectionDetail.Far, CollectionDetail.Near }.Select(d => CollectionCatalog.PrefabPath(id, d))).ToArray();
            foreach (var e in entries) Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(e), e);
            var folders = ids.Select(id => Dir(id) + "/").ToArray();
            var deps = AssetDatabase.GetDependencies(entries, true);
            var outside = deps.Where(d => !folders.Any(f => d.StartsWith(f, StringComparison.Ordinal))
                && !d.StartsWith("Packages/com.unity.render-pipelines.", StringComparison.Ordinal)).ToArray();
            CollectionAssert.IsEmpty(outside, bundle + " pulls in files from outside its models' folders");
        }
        // No asset carries a bundle label: the bundles are built from the catalog alone (CollectionImport.Builds).
        CollectionAssert.IsEmpty(AssetDatabase.GetAllAssetBundleNames(), "asset bundle labels");
    }

    [Test]
    public void TheBundleBuildMapIsTheCatalog()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "CollectionImport");
        var builds = (AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!;
        CollectionAssert.AreEqual(CollectionCatalog.Bundles.Select(b => b.Bundle), builds.Select(b => b.assetBundleName));
        foreach (var (b, (bundle, ids)) in builds.Zip(CollectionCatalog.Bundles, (b, c) => (b, c)))
            CollectionAssert.AreEquivalent(ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near) }),
                b.assetNames, bundle);
    }
}
