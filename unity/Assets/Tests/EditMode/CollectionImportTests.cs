#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
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
    static string ColourMap(string id) => CollectionCatalog.ColourMapPath(id);
    static IEnumerable<string> Ids => CollectionCatalog.Ids.Where(id => !CollectionCatalog.IsBuilding(id));
    static bool Animated(string id) => CollectionCatalog.Animated.Contains(id);
    static readonly string[] GeneratedInfantry =
    {
        "figure-syrian-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
    };

    static bool UnderResources(string path) => path.Split('/').Any(s => s.Equals("Resources", StringComparison.OrdinalIgnoreCase));
    /// <summary>The figures that attack with a fire take baked beside their model (CollectionImport.BorrowedFireTakes, #2375).</summary>
    static IEnumerable<string> BorrowingFigures => ((IReadOnlyDictionary<string, string>)Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor")!
        .GetField("BorrowedFireTakes")!.GetValue(null)).Keys;

    /// <summary>Found by #668's WebGL build: every Build.* ran BuildBundles, which copied the bundles into
    /// StreamingAssets/collection without their .manifest files and then asked Unity for each one's CRC there, which Unity
    /// reads from the .manifest: "no CRC for the bundle", and no player build since #680. The real path, for the editor's
    /// build target: the shipped folder holds the bundles and a checks file whose every line matches its bundle.
    /// The timeout is 10 minutes, not Unity's default 3 (#759). Nearly all of the time is BuildAssetBundles: 0.1 s with
    /// the bundles already built, 5 s without them, 42 s in a fresh Library clone (its first build imports and compiles
    /// for the target), and 240-249 s in six fresh clones built at once at a load of about 20. 600 s is 2.4 times the
    /// slowest run seen. Unity checks the timeout when the test returns; it does not abort a running build.
    /// The build writes the shipped folder itself, so a folder that was not there before the test is deleted after it,
    /// with its .meta, and the asset database forgets them: the nightly's EditMode run found 126 files left in Assets (#2655).</summary>
    [Test, Timeout(600000)]
    public void BuildBundlesWritesTheChecksBesideTheShippedBundles()
    {
        var dest = System.IO.Path.Combine(Application.streamingAssetsPath, CollectionCatalog.StreamingFolder);
        var hadFolder = System.IO.Directory.Exists(dest);
        try { BuildAndCheckTheShippedBundles(dest); }
        finally
        {
            if (!hadFolder)
            {
                if (System.IO.Directory.Exists(dest)) System.IO.Directory.Delete(dest, true);
                System.IO.File.Delete(dest + ".meta");
                AssetDatabase.Refresh(); // the run's leftover check reads the asset database, which may have imported them
            }
        }
    }

    static void BuildAndCheckTheShippedBundles(string dest)
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "CollectionImport");
        try { t!.GetMethod("BuildBundles")!.Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget }); }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException != null) { throw e.InnerException; }
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
    public void ChecksUseUnsignedInvariantMachineFields()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.PositiveSign = "plus";
            culture.NumberFormat.NegativeSign = "minus";
            CultureInfo.CurrentCulture = culture;
            var hash = new string('a', 64);

            Assert.IsNull(CollectionCatalog.ParseChecks($"bundle plus1 2 {hash}\n"),
                "the checks format accepts digits only");
            Assert.AreEqual($"1 -2 {hash}",
                new CollectionCatalog.BundleCheck(1, -2, hash).ToString());
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Test]
    public void TheCatalogKeeps73IndependentOriginalsAndTheGeneratedInfantryBundle()
    {
        var ids = CollectionCatalog.Ids.ToList();
        Assert.AreEqual(103, ids.Count);
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), "an id in two bundles");
        Assert.AreEqual(125, CollectionCatalog.Bundles.Count,
            "73 independent originals, the 1948 soldier's rigged copy (#2384), #1763's shared generated bundle, one bundle for each later generated model (#2083's two weapons, #2084's pose source and #2274's Dakota, four props and local irregular among them), one for the Blender-built 101 Squadron Spitfire (#2734) and one for each of the 23 town models' far-off copies (#2378)");
        Assert.IsTrue(CollectionCatalog.Bundles.Where(b => b.Bundle != CollectionCatalog.ArabArmies)
            .All(b => b.Ids.Length == 1 && b.Bundle == "collection-" + b.Ids[0]));
        // #2378: each town model's far-off copy streams in a bundle of its own; the copies are not among the originals.
        var farCopies = Statecraft.Game.Map.Kits.TownModels.Ids.Select(CollectionCatalog.FarCopy).ToList();
        Assert.AreEqual(23, farCopies.Count);
        CollectionAssert.IsEmpty(farCopies.Intersect(ids).ToList(), "a far-off copy among the originals");
        foreach (var far in farCopies)
        {
            Assert.IsTrue(CollectionCatalog.IsFarCopy(far), far);
            Assert.AreEqual("collection-" + far, CollectionCatalog.BundleOf(far), far);
            Assert.IsFalse(CollectionCatalog.IsBuilding(far), far + " is no original building");
            StringAssert.EndsWith("/" + far + ".prefab", CollectionCatalog.PrefabPath(far, CollectionDetail.Far), far);
            Assert.AreEqual(CollectionCatalog.PrefabPath(far, CollectionDetail.Far), CollectionCatalog.PrefabPath(far, CollectionDetail.Near), far);
        }
        Assert.IsFalse(CollectionCatalog.IsFarCopy("b07-kibbutz-dining-hall-v2"));
        Assert.IsFalse(CollectionCatalog.IsFarCopy("b11-shikun-housing-v2-far"), "a special building has no far-off copy");
        CollectionAssert.AreEqual(new[] { 22, 13, 6, 10, 5, 2, 5, 9, 4, 2, 1, 1 }, CollectionCatalog.Groups.Select(g => g.Ids.Length));
        CollectionAssert.AreEqual(GeneratedInfantry, CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.ArabArmies).Ids);
        Assert.AreEqual(29, CollectionCatalog.Animated.Count);
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
    /// twelve; eight keep their idle clip, and the soldier, which #336 ships with no rig, is static at both zooms (his rigged
    /// copy, #2384, follows the nine as the group's last model). Placing them is #666, #665, #200 and #886's work, not this
    /// import's.</summary>
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
        CollectionAssert.AreEqual(nine, people.Skip(12).Take(9).ToArray());
        CollectionAssert.AreEqual(new[] { CollectionCatalog.Soldier1948Rigged }, people.Skip(21).ToArray(), "the soldier's rigged copy, after #336's nine");
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
            // A figure that borrows its fire take has it baked beside its model, in its own bundle (#2375).
            var fire = BorrowingFigures.Contains(id) ? new[] { $"{Dir(id)}/fire.anim" } : Array.Empty<string>();
            // #2084's pose source: Tal's export byte for byte and the prefab beside it; no map, material or record of its own.
            if (CollectionCatalog.IsFigurePose(id)) return new[] { CollectionCatalog.SourceModelPath(id), $"{Dir(id)}/{id}.prefab" };
            if (CollectionCatalog.IsGenerated(id))
                return new[] { CollectionCatalog.SourceModelPath(id), $"{Dir(id)}/{id}.mat", ColourMap(id), $"{Dir(id)}/{id}.prefab", $"{Dir(id)}/provenance.json" }
                    .Concat(CollectionCatalog.IsGeneratedProp(id) || CollectionCatalog.IsGeneratedTown(id) || CollectionCatalog.IsGeneratedWeapon(id) || CollectionCatalog.IsAuthoredUnit(id) ? Array.Empty<string>() : new[] { $"{Dir(id)}/{id}.controller" })
                    .ToArray();
            files.AddRange(fire);
            if (Animated(id) || OriginalModelPose.Required(id)) files.Add($"{Dir(id)}/{id}-near.controller");
            // The 1948 soldier's rigged copy (#2384) records its derivation beside it, as the generated models do.
            if (id == CollectionCatalog.Soldier1948Rigged) files.Add($"{Dir(id)}/provenance.json");
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
        foreach (var id in Ids.Where(id => !CollectionCatalog.IsGenerated(id)))
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
        foreach (var id in Ids.Where(id => !CollectionCatalog.IsGenerated(id)))
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
        foreach (var id in Ids.Where(id => !CollectionCatalog.IsGenerated(id)))
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
        foreach (var id in Ids.Where(id => !CollectionCatalog.IsGenerated(id)))
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
            // A town model's far-off copy (#2378) lives beside its original.
            var folders = ids.Select(id => CollectionCatalog.IsBuilding(id) || CollectionCatalog.IsFarCopy(id)
                ? $"Assets/Art/Settlements/Prototype/Models/{(CollectionCatalog.IsFarCopy(id) ? id.Substring(0, id.Length - CollectionCatalog.FarSuffix.Length) : id)}/" : Dir(id) + "/").ToArray();
            var deps = AssetDatabase.GetDependencies(entries, true);
            var outside = deps.Where(d => !folders.Any(f => d.StartsWith(f, StringComparison.Ordinal))
                && !d.StartsWith("Packages/com.unity.render-pipelines.", StringComparison.Ordinal)
                && d != "Assets/Scripts/Map/Collection/OriginalModelDistance.cs" && d != "Assets/Scripts/Map/Collection/OriginalModelPose.cs"
                && d != "Assets/Scripts/Map/Units/UnitFigurePoseSource.cs").ToArray();   // #2084's pose source prefab
            CollectionAssert.IsEmpty(outside, bundle + " pulls in files from outside its models' folders");
        }
        // No asset carries a bundle label: the bundles are built from the catalog alone (CollectionImport.Builds).
        CollectionAssert.IsEmpty(AssetDatabase.GetAllAssetBundleNames(), "asset bundle labels");
    }

    /// <summary>The paths of <paramref name="closure"/> that belong to the model <paramref name="id"/>: the files of its folder
    /// (the collection's, or the town buildings' for a house), but not those of its far-off copy (#2378: a bundle of its
    /// own, beside the original).</summary>
    static string[] FilesOf(string id, IEnumerable<string> closure)
    {
        string source = CollectionCatalog.SourceModelPath(id), folder = source.Substring(0, source.LastIndexOf('/') + 1);
        string farCopy = folder + CollectionCatalog.FarCopy(id) + ".";
        return closure.Where(p => p.StartsWith(folder, StringComparison.Ordinal) && !p.StartsWith(farCopy, StringComparison.Ordinal)).ToArray();
    }

    /// <summary>Every placed generated model (Tal Heuberger's figures, props and houses, #756) reaches the player only through its own
    /// streamed bundle: no enabled build scene and no asset under any Resources folder (nested ones too) depends on a file of
    /// its folder, its own bundle's build entry pulls in its model, and no other bundle pulls in any of its files.</summary>
    [Test]
    public void PlacedGeneratedModelsReachThePlayerOnlyThroughTheirOwnBundles()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        var builds = (AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!;
        var closures = builds.ToDictionary(b => b.assetBundleName, b => AssetDatabase.GetDependencies(b.assetNames, true));
        var scenes = AssetDatabase.GetDependencies(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), true);
        var resources = ResourcesClosure();
        var placed = CollectionCatalog.Ids.Where(id => CollectionCatalog.IsGenerated(id) || CollectionCatalog.IsGeneratedHouse(id)).ToList();
        CollectionAssert.IsSubsetOf(new[] { CollectionCatalog.BedouinCivilian, CollectionCatalog.BedouinTent, CollectionCatalog.SyrianR35, CollectionCatalog.KhakiTent,
            CollectionCatalog.EgyptianDakota, CollectionCatalog.StoneWell, CollectionCatalog.Sandbags, CollectionCatalog.CrateStack,
            CollectionCatalog.StoneWall, CollectionCatalog.LocalIrregular, CollectionCatalog.IsraeliSpitfire }, placed);
        Assert.AreEqual(5, placed.Count(CollectionCatalog.IsGeneratedHouse), "the five generated houses");
        foreach (var id in placed)
        {
            CollectionAssert.IsEmpty(FilesOf(id, scenes), id + " in a player scene's closure");
            CollectionAssert.IsEmpty(FilesOf(id, resources), id + " in a Resources asset's closure");
            string own = CollectionCatalog.BundleOf(id)!;
            string source = CollectionCatalog.SourceModelPath(id), folder = source.Substring(0, source.LastIndexOf('/') + 1);
            // Its model, streamed prefab, material and colour map are all in its own bundle's closure; #2084's pose source has
            // only its export (for the clip) and its prefab.
            foreach (var file in CollectionCatalog.IsFigurePose(id) ? new[] { source, CollectionCatalog.PrefabPath(id, CollectionDetail.Near) }
                : new[] { source, CollectionCatalog.PrefabPath(id, CollectionDetail.Near), folder + id + ".mat", CollectionCatalog.ColourMapPath(id) })
                CollectionAssert.Contains(closures[own], file, id + " in its own bundle");
            foreach (var other in closures.Where(c => c.Key != own))
                CollectionAssert.IsEmpty(FilesOf(id, other.Value), id + " pulled into " + other.Key);
        }
    }

    /// <summary>#2734 (coordinator's option 1): the Blender-built 101 Squadron Spitfire (tools/spitfire_101_model.py) streams in
    /// a bundle of its own after the first frame, not the Egyptian Spitfire's, from one prefab beside its model in the
    /// Collection's folder; nothing of it is under a Resources folder, so the WebGL player's first download does not carry it,
    /// and Resources.Load of the old path finds nothing. Its colour map is the tool's PNG (not a JPG). The importer leaves its
    /// committed import settings alone (it is no original: CollectionImport.Run imports only those), so the FBX still remaps
    /// its material to the .mat beside it and imports without compression, animation or file-scale changes.</summary>
    [Test]
    public void TheIsraeliSpitfireStreamsInABundleOfItsOwnAndNothingOfItIsUnderResources()
    {
        const string id = "spitfire-mk9e-101sqn-1948";
        Assert.AreEqual(id, CollectionCatalog.IsraeliSpitfire);
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitShowcase.SpitfireType, id, "the unit type is the catalog id");
        Assert.IsTrue(CollectionCatalog.Ids.Contains(id));
        Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id), "a bundle of its own");
        Assert.AreNotEqual(CollectionCatalog.BundleOf(CollectionCatalog.EgyptianSpitfire), CollectionCatalog.BundleOf(id), "not the Egyptian Spitfire's");
        Assert.IsTrue(CollectionCatalog.IsAuthoredUnit(id) && CollectionCatalog.IsGenerated(id), "imported by none of the originals' importers");
        CollectionAssert.DoesNotContain(CollectionCatalog.Animated, id, "a static model");
        var path = CollectionCatalog.PrefabPath(id, CollectionDetail.Far);
        Assert.AreEqual($"{Folder}/{id}/{id}.prefab", path);
        Assert.AreEqual(path, CollectionCatalog.PrefabPath(id, CollectionDetail.Near), "one prefab at both details");
        Assert.IsFalse(UnderResources(path), path);
        Assert.AreEqual($"{Folder}/{id}/{id}.fbx", CollectionCatalog.SourceModelPath(id));
        Assert.AreEqual($"{Folder}/{id}/{id}_basecolor.png", CollectionCatalog.ColourMapPath(id), "the tool's PNG");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, path + " (made by CollectionImport.MakeAuthoredUnitPrefabs)");
        Assert.IsNotNull(prefab!.transform.Find(Statecraft.Game.Map.Units.StreamedAircraft.PropellerName), "its propeller, a direct child, for the flight to turn");
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{id}/{id}.mat");
        Assert.IsNotNull(mat);
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        Assert.AreEqual(2, renderers.Length, "the body and the propeller");
        Assert.IsTrue(renderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m == mat)), "each part draws with the .mat beside it");
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(ColourMap(id)), mat!.GetTexture("_BaseMap"), "the PNG colour map");
        // The committed import settings (the .fbx.meta beside it), which CollectionImport does not touch.
        var imp = (ModelImporter)AssetImporter.GetAtPath(CollectionCatalog.SourceModelPath(id));
        Assert.AreEqual(ModelImporterMaterialImportMode.ImportViaMaterialDescription, imp.materialImportMode, "its material is remapped, not dropped");
        Assert.AreEqual(ModelImporterMeshCompression.Off, imp.meshCompression);
        Assert.AreEqual(ModelImporterAnimationCompression.Off, imp.animationCompression);
        Assert.IsFalse(imp.importAnimation, "no takes");
        Assert.IsFalse(imp.isReadable);
        Assert.AreEqual(1f, imp.globalScale); Assert.IsTrue(imp.useFileScale, "real metres, as the FBX has them");
        // Nothing of it under any Resources folder, and the old path finds nothing.
        Assert.IsNull(Resources.Load<GameObject>("Units/Authored/" + id), "no Resources copy");
        CollectionAssert.IsEmpty(AssetDatabase.FindAssets(id).Select(AssetDatabase.GUIDToAssetPath).Where(UnderResources), "no copy under any Resources folder");
        CollectionAssert.IsEmpty(FilesOf(id, ResourcesClosure()), "no Resources asset depends on it");
        // Its bundle's entries are its prefab and model; its material and colour map come in as dependencies.
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        var own = ((AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!).Single(b => b.assetBundleName == "collection-" + id);
        CollectionAssert.AreEquivalent(new[] { path, CollectionCatalog.SourceModelPath(id) }, own.assetNames);
        var closure = AssetDatabase.GetDependencies(own.assetNames, true);
        CollectionAssert.Contains(closure, $"{Folder}/{id}/{id}.mat");
        CollectionAssert.Contains(closure, ColourMap(id));
    }

    /// <summary>#2734 review: the real imported prefab is the aircraft's real size in metres, so a later import change (file
    /// scale, global scale, a unit mix-up) cannot ship a 100x or 0.01x Spitfire. The PR body ("Against the coordinator's
    /// requirements", 2. Size and frame) gives "real metres, span 10.16 m, length 9.47 m (spinner tip to rudder trailing
    /// edge)" and "Nose +Z, Y up, wings along X". Its Sources give the length from en.wikipedia "Supermarine Spitfire variants:
    /// specifications, performance and armament" (LF Mk IXe, 9.47 m) and the span from en.wikipedia "Supermarine Spitfire"
    /// (11.23 m less the 1.07 m clip). The span is an open owner question (10.16 m or the variants page's 9.9 m); plus or minus
    /// 5 percent of 10.16 m holds either. Both a lower and an upper bound, so a 100x or 0.01x import fails.</summary>
    [Test]
    public void TheIsraeliSpitfirePrefabIsItsRealSizeWingsAlongXNoseAlongZ()
    {
        const float span = 10.16f, length = 9.47f, tolerance = 0.05f;   // metres; plus or minus 5 percent
        var path = CollectionCatalog.PrefabPath(CollectionCatalog.IsraeliSpitfire, CollectionDetail.Far);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, path + " (made by CollectionImport.MakeAuthoredUnitPrefabs)");
        var go = UnityEngine.Object.Instantiate(prefab!, Vector3.zero, Quaternion.identity);
        try
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers, "the body and the propeller");
            var b = renderers.Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            Assert.GreaterOrEqual(b.size.x, span * (1f - tolerance), $"span along X {b.size.x} m");
            Assert.LessOrEqual(b.size.x, span * (1f + tolerance), $"span along X {b.size.x} m");
            Assert.GreaterOrEqual(b.size.z, length * (1f - tolerance), $"length along Z {b.size.z} m");
            Assert.LessOrEqual(b.size.z, length * (1f + tolerance), $"length along Z {b.size.z} m");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    /// <summary>What every asset under any Resources folder (nested ones too) depends on: all of it ships in every build.</summary>
    static string[] ResourcesClosure() => AssetDatabase.GetDependencies(AssetDatabase.GetAllAssetPaths()
        .Where(p => p.Contains("/Resources/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)).ToArray(), true);

    /// <summary>The isolation check above rejects a real reference: a prefab in a nested Resources folder that places the tent
    /// puts the tent's files in the Resources closure. The probe is deleted afterwards.</summary>
    [Test]
    public void TheResourcesCheckSeesANestedResourcesPrefabThatPlacesAGeneratedModel()
    {
        // The tent (a collection prop) and a house (a town building), each placed by its own probe.
        foreach (var id in new[] { CollectionCatalog.BedouinTent, "generated-1948-01-netanya-tiled-house-original" })
        {
            string root = "Assets/TalClosureProbe" + Guid.NewGuid().ToString("N");
            try
            {
                AssetDatabase.CreateFolder("Assets", root.Substring("Assets/".Length));
                AssetDatabase.CreateFolder(root, "Deep");
                AssetDatabase.CreateFolder(root + "/Deep", "Resources");
                CollectionAssert.IsEmpty(FilesOf(id, ResourcesClosure()), id + ": no Resources reference before the probe");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
                Assert.IsNotNull(model, id);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                try { PrefabUtility.SaveAsPrefabAsset(go, root + "/Deep/Resources/Probe.prefab"); }
                finally { UnityEngine.Object.DestroyImmediate(go); }
                CollectionAssert.IsNotEmpty(FilesOf(id, ResourcesClosure()), id + ": the probe's reference reaches its files");
            }
            finally { AssetDatabase.DeleteAsset(root); }
            CollectionAssert.IsEmpty(FilesOf(id, ResourcesClosure()), id + ": the probe is gone");
        }
    }

    [Test]
    public void TheBundleBuildMapIsTheCatalog()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "CollectionImport");
        var builds = (AssetBundleBuild[])t!.GetMethod("Builds")!.Invoke(null, null)!;
        CollectionAssert.AreEqual(CollectionCatalog.Bundles.Select(b => b.Bundle), builds.Select(b => b.assetBundleName));
        // Each model's source file, written out here rather than read from CollectionCatalog.SourceModelPath (which Builds
        // uses): a town building's <id>.fbx in the buildings' folder, the R35's and the Dakota's compatibility FBX, every other model's
        // <id>.fbx in its own Collection folder (never a -near.fbx or -far.fbx). #2084's pose source builds only its prefab
        // (the clip comes in as its dependency, never the export's mesh as an entry); so does a town model's far-off copy
        // (#2378: its derived mesh, baked map and material come in as dependencies, never the original's FBX).
        string[] Entries(string id) => CollectionCatalog.IsFigurePose(id) ? new[] { $"{Dir(id)}/{id}.prefab" }
            : CollectionCatalog.IsFarCopy(id) ? new[] { $"{CollectionCatalog.BuildingsFolder}/{id.Substring(0, id.Length - CollectionCatalog.FarSuffix.Length)}/{id}.prefab" }
            : new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near), Source(id) };
        string Source(string id) => CollectionCatalog.IsBuilding(id) ? $"{CollectionCatalog.BuildingsFolder}/{id}/{id}.fbx"
            : id == CollectionCatalog.SyrianR35 || id == CollectionCatalog.EgyptianDakota ? $"{Dir(id)}/{id}-compatibility.fbx" : Model(id);
        foreach (var (b, (bundle, ids)) in builds.Zip(CollectionCatalog.Bundles, (b, c) => (b, c)))
            CollectionAssert.AreEquivalent(ids.SelectMany(Entries).Distinct(),
                b.assetNames, bundle);
        // A strict bundle build fails on a name with no asset behind it, and the map above would not see it: every name is a file.
        var missing = builds.SelectMany(b => b.assetNames.Where(n => !System.IO.File.Exists(n) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(n))).Select(n => b.assetBundleName + ": " + n)).ToList();
        CollectionAssert.IsEmpty(missing, "bundle asset names with no asset");
        // The R35 is imported from the vehicle delivery's compatibility FBX, not from an <id>.fbx.
        var r35 = builds.Single(b => b.assetBundleName == CollectionCatalog.BundleOf(CollectionCatalog.SyrianR35));
        CollectionAssert.Contains(r35.assetNames, $"{CollectionCatalog.Folder}/{CollectionCatalog.SyrianR35}/{CollectionCatalog.SyrianR35}-compatibility.fbx");
    }
}
