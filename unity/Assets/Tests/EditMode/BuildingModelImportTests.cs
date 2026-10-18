#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>MATKOT's 23 Tripo building models (issue #659, from #507) are imported to load light, the maintainer's gate of
/// 15 October 2026: "we need to make sure this is built in a way that doesnt make the game extremely hard to load". No
/// model sits under a Resources folder (Unity ships every file there whether used or not); each mesh is a static,
/// compressed, non-readable mesh with no animation, rig, blend shapes, cameras, lights, colliders or tangents; each
/// colour map is crunch-compressed, mipmapped and at most 512 px on every target; each material draws its own colour
/// map with GPU instancing on. The reasons for each value are in the PR that added the models.</summary>
public class BuildingModelImportTests
{
    const string Folder = "Assets/Art/Settlements/Prototype/Models";
    const int MaxTextureSize = 512, MaxTriangles = 8000;

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
        var streamed = Statecraft.Game.Map.Collection.CollectionCatalog.Bundles
            .Single(b => b.Bundle == Statecraft.Game.Map.Collection.CollectionCatalog.Buildings).Ids;
        Assert.AreEqual(5, streamed.Length);
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
    public void EveryModelImportsAsAStaticCompressedMeshAndNothingElse()
    {
        foreach (var id in Ids)
        {
            var path = Model(id);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            Assert.IsNotNull(imp, path);
            Assert.AreEqual(ModelImporterMeshCompression.Medium, imp!.meshCompression, path + " mesh compression");
            Assert.IsFalse(imp.isReadable, path + " Read/Write");
            Assert.IsFalse(imp.importAnimation, path + " animation");
            Assert.AreEqual(ModelImporterAnimationType.None, imp.animationType, path + " rig");
            Assert.IsFalse(imp.importBlendShapes, path + " blend shapes");
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
            Assert.LessOrEqual(triangles, MaxTriangles, path + " triangles");
            Assert.IsFalse(mesh.isReadable, path + " mesh readable");
            Assert.IsFalse(mesh.HasVertexAttribute(VertexAttribute.Tangent), path + " tangent stream");
            CollectionAssert.IsEmpty(assets.OfType<AnimationClip>(), path + " clips");
            CollectionAssert.IsEmpty(assets.OfType<Avatar>(), path + " avatar");
            CollectionAssert.IsEmpty(assets.OfType<Material>(), path + " embedded materials");
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            CollectionAssert.IsEmpty(root.GetComponentsInChildren<Component>(true)
                .Where(c => !(c is Transform || c is MeshFilter || c is MeshRenderer)), path + " other components");
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
            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            Assert.LessOrEqual(Mathf.Max(w, h), MaxTextureSize, path + " committed file size (no masters in the repository)");
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
