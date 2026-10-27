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

/// <summary>#1273: all 73 full originals stream independently after the first frame, without generated LODs,
/// mesh/animation compression or texture resizing. Import tests preserve source geometry/clips and isolate dependencies.</summary>
public class CollectionImportTests
{
    const string Folder = CollectionCatalog.Folder;

    static string Dir(string id) => $"{Folder}/{id}";
    static string Model(string id) => $"{Dir(id)}/{id}.fbx";
    static string ColourMap(string id) => $"{Dir(id)}/{id}_basecolor.jpg";
    static IEnumerable<string> Ids => CollectionCatalog.Ids.Where(id => !CollectionCatalog.IsBuilding(id));
    static bool Animated(string id) => CollectionCatalog.Animated.Contains(id);
    static readonly string[] GeneratedInfantry =
    {
        "figure-syrian-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
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
    public void TheCatalogKeeps73IndependentOriginalsAndTheGeneratedInfantryBundle()
    {
        var ids = CollectionCatalog.Ids.ToList();
        Assert.AreEqual(75, ids.Count);
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), "an id in two bundles");
        Assert.AreEqual(74, CollectionCatalog.Bundles.Count, "73 independent originals plus #1763's shared generated bundle");
        Assert.IsTrue(CollectionCatalog.Bundles.Where(b => b.Bundle != CollectionCatalog.ArabArmies)
            .All(b => b.Ids.Length == 1 && b.Bundle == "collection-" + b.Ids[0]));
        CollectionAssert.AreEqual(new[] { 21, 13, 6, 10, 5, 2 }, CollectionCatalog.Groups.Select(g => g.Ids.Length));
        CollectionAssert.AreEqual(GeneratedInfantry, CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.ArabArmies).Ids);
        Assert.AreEqual(23, CollectionCatalog.Animated.Count);
        CollectionAssert.IsSubsetOf(CollectionCatalog.Animated, ids);
        CollectionAssert.DoesNotContain(CollectionCatalog.Animated, "figure-c05-pioneer-veteran-v2", "C05's only idle is a walk");
        Assert.AreEqual("collection-vehicle-altalena-lst-1948-v2wl", CollectionCatalog.BundleOf("vehicle-altalena-lst-1948-v2wl"));
        Assert.AreEqual("collection-b07-kibbutz-dining-hall-v2", CollectionCatalog.BundleOf("b07-kibbutz-dining-hall-v2"), "the 18 kit originals now stream too");
    }

    [Test]
    public void TheFiveSpecialBuildingsUsePrototypeAssetsInTheirOwnStreamedBundle()
    {
        var ids = new[] { "b11-shikun-housing-v2", "b12-tegart-fort-v2", "b18-military-barracks-v2", "b21-artisan-workshop-v2", "b22-industrial-factory-v2" };
        CollectionAssert.AreEqual(ids, CollectionCatalog.Groups.Single(b => b.Bundle == CollectionCatalog.Buildings).Ids);
        foreach (var id in ids)
        {
            Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id));
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
            Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id), id);
            CollectionAssert.DoesNotContain(CollectionCatalog.Animated, id, id + " is one static mesh");
            foreach (var d in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var path = CollectionCatalog.PrefabPath(id, d);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                Assert.IsFalse(UnderResources(path), path);
            }
        }
        CollectionAssert.AreEqual(towns, CollectionCatalog.Groups.Single(b => b.Bundle == CollectionCatalog.Towns).Ids);
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        var builds = (AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!;
        Assert.IsTrue(towns.All(id => builds.Any(b => b.assetBundleName == "collection-" + id)), "each town has its own bundle");
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
        var people = CollectionCatalog.Groups.Single(b => b.Bundle == CollectionCatalog.People).Ids;
        CollectionAssert.AreEqual(nine, people.Skip(12).ToArray());
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
        var expected = Ids.SelectMany<string, string>(id =>
        {
            var files = new List<string>
            {
                Model(id), $"{Dir(id)}/{id}.mat", ColourMap(id),
                CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near),
            };
            if (GeneratedInfantry.Contains(id))
                return new[] { $"{Dir(id)}/{id}.fbx", $"{Dir(id)}/{id}.mat", ColourMap(id),
                    $"{Dir(id)}/{id}.prefab", $"{Dir(id)}/{id}.controller", $"{Dir(id)}/provenance.json" };
            if (Animated(id) || OriginalModelPose.Required(id)) files.Add($"{Dir(id)}/{id}-near.controller");
            foreach (var suffix in new[] { "-far.fbx", "-near.fbx", "-far.prefab", "-near.prefab" })
            {
                var retained = $"{Dir(id)}/{id}{suffix}";
                if (System.IO.File.Exists(retained)) files.Add(retained);
            }
            return files;
        }).Distinct().OrderBy(p => p, StringComparer.Ordinal);
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
        Assert.AreEqual(ModelImporterMeshCompression.Off, imp!.meshCompression, path + " mesh compression");
        Assert.IsFalse(imp.isReadable, path + " Read/Write");
        Assert.IsTrue(imp.importBlendShapes, path + " blend shapes");
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

    [Test]
    public void BothPlacementDetailsUseOnlyTheOriginalGeometry()
    {
        foreach (var id in Ids.Where(id => !GeneratedInfantry.Contains(id)))
        {
            var path = Model(id); var imp = Importer(path);
            var meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
            Assert.IsNotEmpty(meshes, path + " original geometry");
            Assert.IsTrue(meshes.All(m => Triangles(m) > 0 && !m.isReadable), path + " geometry stays GPU-only");
            Assert.AreEqual(CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near), id + " one original at both placement details");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));
            foreach (var mesh in prefab.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
                .Concat(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(f => f.sharedMesh)))
                Assert.AreEqual(path, AssetDatabase.GetAssetPath(mesh), id + " no baked/decimated geometry");
            var serialized = new SerializedObject(imp);
            var lod = serialized.FindProperty("m_GenerateMeshLods") ?? serialized.FindProperty("generateMeshLods");
            Assert.IsNotNull(lod, path + " inspectable LOD setting"); Assert.IsFalse(lod.boolValue, path);
        }
    }

    [Test]
    public void OriginalModelsRetainAllSourceTakesAndBonesWithoutReduction()
    {
        foreach (var id in Ids.Where(id => !GeneratedInfantry.Contains(id)))
        {
            var path = Model(id); var imp = Importer(path);
            Assert.AreEqual(ModelImporterAnimationType.Generic, imp.animationType, path + " source rig");
            Assert.IsTrue(imp.importAnimation, path + " source takes");
            Assert.IsFalse(imp.optimizeGameObjects, path + " bones retained");
            Assert.IsFalse(imp.resampleCurves, path + " original curves");
            Assert.AreEqual(ModelImporterAnimationCompression.Off, imp.animationCompression, path);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            Assert.AreEqual(imp.defaultClipAnimations.Length, clips.Length, path + " every original take");
            if (Animated(id))
            {
                Assert.IsNotEmpty(clips, path + " animated original has a take");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<SkinnedMeshRenderer>(true), path + " skin retained");
            }
        }
    }

    [Test]
    public void EveryColourMapKeepsItsSourceSizeAndMipmapsWithoutCrunch()
    {
        foreach (var id in Ids.Where(id => !GeneratedInfantry.Contains(id)))
        {
            var path = ColourMap(id); var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp, path); imp!.GetSourceTextureWidthAndHeight(out int width, out int height);
            Assert.GreaterOrEqual(imp.maxTextureSize, Math.Max(width, height), path + " no downscaling");
            Assert.IsFalse(imp.crunchedCompression, path + " no Crunch");
            Assert.IsTrue(imp.mipmapEnabled, path + " mipmaps"); Assert.IsTrue(imp.sRGBTexture, path + " sRGB");
            Assert.IsFalse(imp.isReadable, path + " GPU-only");
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var setting = imp.GetPlatformTextureSettings(target);
                Assert.GreaterOrEqual(setting.maxTextureSize, Math.Max(width, height), path + " " + target + " keeps source size");
                Assert.IsFalse(setting.crunchedCompression, path + " " + target);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.AreEqual(width, tex.width, path + " source width"); Assert.AreEqual(height, tex.height, path + " source height");
            Assert.Greater(tex.mipmapCount, 1, path + " imported mipmaps");
        }
    }

    [Test]
    public void BothPlacementAliasesKeepTheOriginalMaterialRigAndController()
    {
        foreach (var id in Ids.Where(id => !GeneratedInfantry.Contains(id)))
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
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(Model(id));
                Assert.AreEqual(original.GetComponentsInChildren<Renderer>(true).Length, renderers.Length, path + " every original part retained");
                Assert.Greater(renderers.Length, 0, path + " geometry present");
                foreach (var renderer in renderers)
                    Assert.IsTrue(renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(m => m == mat), path + " each part uses its own material");
                var animator = prefab.GetComponentInChildren<Animator>(true);
                if (Animated(id) || OriginalModelPose.Required(id))
                {
                    Assert.IsNotNull(prefab.GetComponentInChildren<SkinnedMeshRenderer>(true), path + " original skin");
                    Assert.IsNotNull(animator, path + " animator");
                    var ctrl = animator!.runtimeAnimatorController as AnimatorController;
                    Assert.IsNotNull(ctrl, path + " controller");
                    Assert.AreEqual(AssetDatabase.GetAssetPath(ctrl), $"{Dir(id)}/{id}-near.controller", path);
                    var clip = ctrl!.layers[0].stateMachine.defaultState.motion as AnimationClip;
                    Assert.IsNotNull(clip, path + " original take selected");
                    Assert.Greater(clip!.length, 0f, path + " nonempty take");
                    Assert.AreEqual(Model(id), AssetDatabase.GetAssetPath(clip), path + " clip source");
                    Assert.AreEqual(AnimatorCullingMode.CullCompletely, animator.cullingMode, path + " culling");
                }
                else
                {
                    Assert.IsTrue(renderers.All(r => r is MeshRenderer || r is SkinnedMeshRenderer), path + " original renderer types");
                    Assert.IsNull(animator, path + " no animator");
                }
            }
        }
    }

    [Test]
    public void EachOriginalBundleKeepsItsPrefabAndOnlyItsOwnAssetsAndPlacementScripts()
    {
        foreach (var (bundle, ids) in CollectionCatalog.Bundles)
        {
            var entries = ids.SelectMany(id => new[] { CollectionDetail.Far, CollectionDetail.Near }.Select(d => CollectionCatalog.PrefabPath(id, d))).ToArray();
            foreach (var e in entries) Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(e), e);
            var folders = ids.Select(id => CollectionCatalog.IsBuilding(id)
                ? $"Assets/Art/Settlements/Prototype/Models/{id}/" : Dir(id) + "/").ToArray();
            var deps = AssetDatabase.GetDependencies(entries, true);
            var outside = deps.Where(d => !folders.Any(f => d.StartsWith(f, StringComparison.Ordinal))
                && !d.StartsWith("Packages/com.unity.render-pipelines.", StringComparison.Ordinal)
                && d != "Assets/Scripts/Map/Collection/OriginalModelDistance.cs" && d != "Assets/Scripts/Map/Collection/OriginalModelPose.cs").ToArray();
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
            CollectionAssert.AreEquivalent(ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near), CollectionCatalog.IsBuilding(id) ? $"{CollectionCatalog.BuildingsFolder}/{id}/{id}.fbx" : Model(id) }).Distinct(),
                b.assetNames, bundle);
    }
}
