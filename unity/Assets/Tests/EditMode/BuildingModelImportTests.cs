#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>#1273: the 23 full original buildings stream outside Resources, retain original geometry/takes and
/// texture dimensions, and use their own instanced materials. Scene placement and frame costs have separate gates.</summary>
public class BuildingModelImportTests
{
    const string Folder = "Assets/Art/Settlements/Prototype/Models";


    static readonly string[] Ids =
    {
        "b01-white-city-v2", "b02a-tel-aviv-corner-block-v2", "b02b-tel-aviv-narrow-block-v2", "b03a-jerusalem-arches-v2",
        "b03b-jerusalem-external-stair-v2", "b03c-jerusalem-plain-v2", "b04a-haifa-stone-balconies-v2",
        "b04b-haifa-loading-doors-v2", "b05a-moshava-house-v2", "b07-kibbutz-dining-hall-v2",
        "b09a-kibbutz-communal-building-v2", "b11-shikun-housing-v2", "b12-tegart-fort-v2", "b13a-netanya-two-storey-v2",
        "b16-farmhouse-v2", "b17-stone-courtyard-house-v2", "b18-military-barracks-v2", "b21-artisan-workshop-v2",
        "b22-industrial-factory-v2", "b23-winery-v2", "b24-agricultural-barn-v2", "b28-water-tower-v2", "b32-port-cargo-shed-v2",
    };

    static string Model(string id) => $"{Folder}/{id}/{id}.fbx";
    static string ColourMap(string id) => $"{Folder}/{id}/{id}_basecolor.jpg";

    static bool UnderResources(string path) => path.Split('/').Any(s => s.Equals("Resources", StringComparison.OrdinalIgnoreCase));

    /// <summary>The 23 buildings (model, material, colour map), and beside each of #707's five the prefab its streamed
    /// bundle carries (CollectionImport.MakeBuildingPrefabs).</summary>
    [Test]
    public void TheFolderHoldsTheTwentyThreeBuildingsAndNothingElse()
    {
        Assert.AreEqual(23, Ids.Length);
        var streamed = Statecraft.Game.Map.Collection.CollectionCatalog.Ids
            .Where(Statecraft.Game.Map.Collection.CollectionCatalog.IsBuilding).ToArray();
        Assert.AreEqual(23, streamed.Length);
        CollectionAssert.IsSubsetOf(streamed, Ids);
        var expected = Ids.SelectMany(id => new[] { Model(id), $"{Folder}/{id}/{id}.mat", ColourMap(id) })
            .Concat(streamed.Select(id => Statecraft.Game.Map.Collection.CollectionCatalog.PrefabPath(id, Statecraft.Game.Map.Collection.CollectionDetail.Near)))
            .OrderBy(p => p, StringComparer.Ordinal);
        var found = AssetDatabase.FindAssets("", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p)).Distinct().OrderBy(p => p, StringComparer.Ordinal);
        CollectionAssert.AreEqual(expected, found);
    }

    [Test]
    public void NoModelAndNoBuildingFileIsUnderAResourcesFolder()
    {
        var models = AssetDatabase.FindAssets("t:Model", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Assert.GreaterOrEqual(models.Length, Ids.Length, "the search sees the models");
        CollectionAssert.IsEmpty(models.Where(UnderResources), "models under a Resources folder ship in every build");
        Assert.IsFalse(UnderResources(Folder), Folder);
    }

    /// <summary>A Resources folder ships every asset in it and everything those assets depend on, so a prefab or
    /// material there that points at a building would put the building in every build.</summary>
    [Test]
    public void NothingUnderAResourcesFolderDependsOnTheBuildings()
    {
        var inResources = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && UnderResources(p) && !AssetDatabase.IsValidFolder(p))
            .ToArray();
        Assert.IsNotEmpty(inResources, "the search sees the Resources folders");
        var leaks = AssetDatabase.GetDependencies(inResources, true)
            .Where(d => d.StartsWith(Folder + "/", StringComparison.Ordinal)).OrderBy(d => d, StringComparer.Ordinal);
        CollectionAssert.IsEmpty(leaks, "building files reached from a Resources folder ship in every build");
    }

    [Test]
    public void EveryOriginalBuildingImportsWithoutCompressionAndKeepsItsSourceTakes()
    {
        foreach (var id in Ids)
        {
            var path = Model(id);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            Assert.IsNotNull(imp, path);
            Assert.AreEqual(ModelImporterMeshCompression.Off, imp!.meshCompression, path + " mesh compression");
            Assert.IsFalse(imp.isReadable, path + " Read/Write");
            Assert.IsTrue(imp.importAnimation, path + " all original takes retained");
            Assert.AreEqual(ModelImporterAnimationType.Generic, imp.animationType, path + " rig");
            Assert.IsTrue(imp.importBlendShapes, path + " blend shapes");
            Assert.IsFalse(imp.importVisibility, path + " visibility");
            Assert.IsFalse(imp.importCameras, path + " cameras");
            Assert.IsFalse(imp.importLights, path + " lights");
            Assert.IsFalse(imp.addCollider, path + " colliders");
            Assert.IsFalse(imp.generateSecondaryUV, path + " lightmap UVs");
            Assert.AreEqual(ModelImporterTangents.None, imp.importTangents, path + " tangents (no normal map)");
            Assert.AreEqual(ModelImporterMaterialImportMode.None, imp.materialImportMode, path + " materials (the .mat beside it)");

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var meshes = assets.OfType<Mesh>().ToArray();
            Assert.AreEqual(1, meshes.Length, path + " meshes");
            var mesh = meshes[0];
            long triangles = Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i)) / 3;
            Assert.Greater(triangles, 0, path);
            Assert.AreEqual(Model(id), AssetDatabase.GetAssetPath(mesh), path + " exact original geometry");
            Assert.IsFalse(mesh.isReadable, path + " mesh readable");
            Assert.IsFalse(mesh.HasVertexAttribute(VertexAttribute.Tangent), path + " tangent stream");
            Assert.AreEqual(imp.defaultClipAnimations.Length, assets.OfType<AnimationClip>().Count(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)), path + " every original take");
            CollectionAssert.IsEmpty(assets.OfType<Material>(), path + " embedded materials");
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            CollectionAssert.IsEmpty(root.GetComponentsInChildren<Component>(true)
                .Where(c => !(c is Transform || c is MeshFilter || c is MeshRenderer || c is Animator)), path + " other components");
        }
    }

    [Test]
    public void EveryColourMapKeepsItsSourceSizeAndMipmapsWithoutCrunch()
    {
        foreach (var id in Ids)
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
    public void EveryMaterialDrawsItsOwnColourMapWithGpuInstancing()
    {
        foreach (var id in Ids)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{id}/{id}.mat");
            Assert.IsNotNull(mat, id);
            Assert.AreEqual("Universal Render Pipeline/Lit", mat.shader.name, id);
            Assert.IsTrue(mat.enableInstancing, id + " GPU instancing");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(ColourMap(id)), mat.GetTexture("_BaseMap"), id + " colour map");
        }
    }
}
