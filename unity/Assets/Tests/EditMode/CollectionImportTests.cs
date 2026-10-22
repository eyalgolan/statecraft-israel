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

/// <summary>The 1948 Collection's 31 non-building models (issue #664; Tal Heuberger's Tripo models from #507, slimmed by
/// tools/slim_collection.py under the owner's ruling of 15 October 2026, "Plain git, slimmed first") are imported to load
/// light (the maintainer's gate: "we need to make sure this is built in a way that doesnt make the game extremely hard to
/// load"): nothing under a Resources folder; each model a far mesh of at most 1,500 triangles (4,500 for a vehicle, ship
/// or aircraft) with no rig or clip and a near mesh of at most 8,000, skinned with its one idle clip only on the 13 models that have one; compressed,
/// non-readable meshes; crunched 512 px colour maps; instanced materials; and each model's two prefabs in its category's
/// streamed bundle, with every dependency inside the collection's own folder (CollectionImport.Run sets all of this).</summary>
public class CollectionImportTests
{
    const string Folder = CollectionCatalog.Folder;
    const int MaxTextureSize = 512, FarTriangles = 1500, FarTrianglesVehicle = 4500, FarTrianglesTown = 4500, NearTriangles = 8000;

    static string Dir(string id) => $"{Folder}/{id}";
    static string Model(string id, string level) => $"{Dir(id)}/{id}-{level}.fbx";
    static string ColourMap(string id) => $"{Dir(id)}/{id}_basecolor.jpg";
    static IEnumerable<string> Ids => CollectionCatalog.Ids.Where(id => CollectionCatalog.BundleOf(id) != CollectionCatalog.Buildings);
    static bool Animated(string id) => CollectionCatalog.Animated.Contains(id);
    static readonly string[] GeneratedInfantry =
    {
        "figure-syrian-infantry-1948-v1", "figure-ala-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
    };

    static bool UnderResources(string path) => path.Split('/').Any(s => s.Equals("Resources", StringComparison.OrdinalIgnoreCase));

    /// <summary>Found by #668's WebGL build: every Build.* ran BuildBundles, which copied the bundles into
    /// StreamingAssets/collection without their .manifest files and then asked Unity for each one's CRC there, which Unity
    /// reads from the .manifest: "no CRC for the bundle", and no player build since #680. The real path, for the editor's
    /// build target: the shipped folder holds the bundles and a checks file whose every line matches its bundle.
    /// The timeout is 10 minutes, not Unity's default 3 (#759). Nearly all of the time is BuildAssetBundles: 0.1 s with
    /// the bundles already built, 5 s without them, 42 s in a fresh Library clone (its first build imports and compiles
    /// for the target), and 240-249 s in six fresh clones built at once at a load of about 20. 600 s is 2.4 times the
    /// slowest run seen. Unity checks the timeout when the test returns; it does not abort a running build.</summary>
    [Test, Timeout(600000)]
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
    public void TheCatalogKeepsTheOriginalModelsAndAddsThreeInfantryInFiveBundles()
    {
        var ids = CollectionCatalog.Ids.ToList();
        Assert.AreEqual(58, ids.Count);
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), "an id in two bundles");
        CollectionAssert.AreEqual(new[] { 24, 13, 6, 10, 5 }, CollectionCatalog.Bundles.Select(b => b.Ids.Length), "people (#507's 12, #336's 9 and three generated infantry), vehicles, animals, towns, special buildings");
        Assert.AreEqual(24, CollectionCatalog.Animated.Count);
        CollectionAssert.AreEqual(GeneratedInfantry, CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.People).Ids.Skip(21));
        CollectionAssert.IsSubsetOf(CollectionCatalog.Animated, ids);
        CollectionAssert.DoesNotContain(CollectionCatalog.Animated, "figure-c05-pioneer-veteran-v2", "C05's only idle is a walk");
        Assert.AreEqual(CollectionCatalog.Vehicles, CollectionCatalog.BundleOf("vehicle-altalena-lst-1948-v2wl"));
        Assert.IsNull(CollectionCatalog.BundleOf("b07-kibbutz-dining-hall-v2"), "the buildings are #659's");
    }

    [Test]
    public void TheFiveSpecialBuildingsUsePrototypeAssetsInTheirOwnStreamedBundle()
    {
        var ids = new[] { "b11-shikun-housing-v2", "b12-tegart-fort-v2", "b18-military-barracks-v2", "b21-artisan-workshop-v2", "b22-industrial-factory-v2" };
        CollectionAssert.AreEqual(ids, CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.Buildings).Ids);
        foreach (var id in ids)
        {
            Assert.AreEqual(CollectionCatalog.Buildings, CollectionCatalog.BundleOf(id));
            var path = CollectionCatalog.PrefabPath(id, CollectionDetail.Near);
            StringAssert.StartsWith("Assets/Art/Settlements/Prototype/Models/", path);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
            Assert.IsFalse(UnderResources(path));
        }
    }

    /// <summary>Issue #663, owner ruling of 17 October 2026: "The nine whole-town models ship ... slimmed, streamed after the
    /// first frame, and pass the load gate". Tal Heuberger's nine from #507 are the towns bundle, streamed like
    /// the rest, with #336's Jerusalem 3denhance variant after them, an extra beside #507's Jerusalem (#886; owner, 19 October
    /// 2026: "Yes, both").</summary>
    [Test]
    public void TheNineWholeTownModelsOf507AreTheStreamedTownsBundleAndNoneIsUnderResources()
    {
        var towns = new[]
        {
            "tel-aviv-white-city-v1", "jerusalem-stone-city-v1", "haifa-port-city-v1", "moshava-town-v1", "nahalal-moshav-v1",
            "kibbutz-compound-v2", "stone-village-v1", "maabara-camp-v1", "shikun-housing-v1",
            "jerusalem-stone-city-v1-3denhance",
        };
        foreach (var id in towns)
        {
            Assert.AreEqual(CollectionCatalog.Towns, CollectionCatalog.BundleOf(id), id);
            CollectionAssert.DoesNotContain(CollectionCatalog.Animated, id, id + " is one static mesh");
            foreach (var d in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var path = CollectionCatalog.PrefabPath(id, d);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                Assert.IsFalse(UnderResources(path), path);
            }
        }
        CollectionAssert.AreEqual(towns, CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.Towns).Ids);
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        var builds = (AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!;
        Assert.IsTrue(builds.Any(b => b.assetBundleName == CollectionCatalog.Towns), "the towns are built as their own bundle");
        // No copy of a town model anywhere under a Resources folder (#507 shipped them all in one).
        CollectionAssert.IsEmpty(towns.SelectMany(id => AssetDatabase.FindAssets(id)).Select(AssetDatabase.GUIDToAssetPath).Where(UnderResources));
    }

    /// <summary>Issue #708, owner ruling of 18 October 2026: "the 8 figures in #708 can be merged without his license line";
    /// and #886, owner ruling of 19 October 2026: "Yes, both" (the pioneer farmer and the Jerusalem variant, on the same
    /// terms). The nine figures offered only in #336 are slimmed like #507's people and stream in the people bundle after its
    /// twelve; eight keep their idle clip, and the soldier, which #336 ships with no rig, is static at both zooms. Placing
    /// them is #666, #665, #200 and #886's work, not this import's.</summary>
    [Test]
    public void TheNineFiguresOf336StreamInThePeopleBundleAfterThe12Of507()
    {
        var nine = new[]
        {
            "figure-arab-villager-v1", "figure-builder-1950-v1", "figure-elder-immigrant-v1", "figure-immigrant-child-v1",
            "figure-immigrant-woman-v1", "figure-kibbutz-woman-v1", "figure-soldier-1948-v1", "figure-yemenite-immigrant-v1",
            "figure-pioneer-farmer-v1-tpose",
        };
        var people = CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.People).Ids;
        CollectionAssert.AreEqual(nine, people.Skip(12).Take(9).ToArray(), "the original people retain their order before the new infantry");
        CollectionAssert.AreEqual(nine.Where(id => id != "figure-soldier-1948-v1"), nine.Where(Animated));
        foreach (var id in nine)
            foreach (var d in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var path = CollectionCatalog.PrefabPath(id, d);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                Assert.IsFalse(UnderResources(path), path);
            }
        CollectionAssert.IsEmpty(nine.SelectMany(id => AssetDatabase.FindAssets(id)).Select(AssetDatabase.GUIDToAssetPath).Where(UnderResources));
    }

    [Test]
    public void TheFolderHoldsExactlyTheRuntimeModelFilesAndDeclaredGeneratedReviewFiles()
    {
        var expected = Ids.SelectMany(id =>
        {
            var files = new List<string>
            {
                Model(id, "far"), Model(id, "near"), $"{Dir(id)}/{id}.mat", ColourMap(id),
                CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near),
            };
            if (Animated(id)) files.Add($"{Dir(id)}/{id}-near.controller");
            if (GeneratedInfantry.Contains(id))
            {
                var reviewFiles = new[] { Model(id, "walk"), $"{Dir(id)}/provenance.json" };
                files.AddRange(reviewFiles);
                var dependencies = AssetDatabase.GetDependencies(new[]
                {
                    CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near),
                }, true);
                CollectionAssert.IsEmpty(reviewFiles.Intersect(dependencies), id + ": review files are not runtime dependencies");
            }
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
    public void EveryFarModelIsOneStaticMeshOfAtMost1500TrianglesOr4500ForAVehicleOrATown()
    {
        foreach (var id in Ids)
            AssertStatic(Model(id, "far"), CollectionCatalog.BundleOf(id) == CollectionCatalog.Towns ? FarTrianglesTown
                : CollectionCatalog.BundleOf(id) == CollectionCatalog.Vehicles ? FarTrianglesVehicle : FarTriangles);
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
            var folders = ids.Select(id => CollectionCatalog.BundleOf(id) == CollectionCatalog.Buildings
                ? $"Assets/Art/Settlements/Prototype/Models/{id}/" : Dir(id) + "/").ToArray();
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
            CollectionAssert.AreEquivalent(ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near) }).Distinct(),
                b.assetNames, bundle);
    }
}
