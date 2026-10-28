#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Review;
using UnityEditor;
using UnityEngine;

public class Generated1948AssetTests
{
    [Serializable] sealed class Receipt
    {
        public string id = "", source_glb_sha256 = "", source_archive_path = "", embedded_image_sha256 = "";
        public int number = 0, source_triangles = 0;
        public int[] embedded_dimensions = Array.Empty<int>();
        public string[] source_clips = Array.Empty<string>();
    }
    static string Hash(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static string Disk(string path) => Path.Combine(Paths.RepoRoot, "unity", path);

    [Test] public void OriginalDeliveriesAndExactMapsMatchReceiptsWithLosslessImport()
    {
        var files = AssetDatabase.FindAssets("t:Model", new[] { Generated1948BuildingsPropsReview.AssetFolder, Statecraft.Game.Map.Collection.CollectionCatalog.Folder })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".fbx", StringComparison.Ordinal))
            // The folder also holds #1377's review-only characters (figure-*); the buildings and props are generated-1948-*.
            .Where(p => Path.GetFileName(p).StartsWith("generated-1948-", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(11, files.Length);
        foreach (string fbx in files)
        {
            string dir = Path.GetDirectoryName(fbx)!;
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(Disk(dir + "/provenance.json")));
            Assert.AreEqual(receipt.source_glb_sha256, Hash(Path.Combine(Paths.RepoRoot, receipt.source_archive_path)), receipt.id);
            Assert.IsFalse(fbx.Contains("-near") || fbx.Contains("-far"), fbx);
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            Assert.AreEqual(ModelImporterMeshCompression.Off, importer.meshCompression, fbx);
            Assert.AreEqual(ModelImporterAnimationCompression.Off, importer.animationCompression, fbx);
            Assert.IsTrue(importer.importAnimation, "all source clips remain enabled");
            Assert.AreEqual(receipt.source_clips.Length, importer.defaultClipAnimations.Length, fbx);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var meshes = source.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
            Assert.AreEqual(receipt.source_triangles, meshes.Sum(m => (long)m.GetIndexCount(0) / 3), fbx);
            Assert.IsTrue(meshes.All(m => m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)));
            Assert.IsEmpty(source.GetComponentsInChildren<LODGroup>(true));
            var material = AssetDatabase.LoadAssetAtPath<Material>(dir + "/" + receipt.id + ".mat");
            Assert.AreEqual(0f, material.GetFloat("_Metallic"), 1e-6f, receipt.id);
            Assert.AreEqual(.1f, material.GetFloat("_Smoothness"), 1e-6f, receipt.id);
            var texture = material.mainTexture; Assert.IsNotNull(texture, receipt.id);
            string image = AssetDatabase.GetAssetPath(texture);
            Assert.AreEqual(receipt.embedded_image_sha256, Hash(Disk(image)), receipt.id);
            Assert.AreEqual(receipt.embedded_dimensions[0], texture.width, receipt.id);
            Assert.AreEqual(receipt.embedded_dimensions[1], texture.height, receipt.id);
            var ti = (TextureImporter)AssetImporter.GetAtPath(image);
            Assert.IsFalse(ti.crunchedCompression, image);
            Assert.AreEqual(TextureImporterCompression.Compressed, ti.textureCompression, image);
            Assert.GreaterOrEqual(ti.maxTextureSize, Math.Max(receipt.embedded_dimensions[0], receipt.embedded_dimensions[1]));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + receipt.id + ".prefab"));
        }
    }

    /// <summary>A placed generated prop (#756) keeps its source in its streamed prefab: every part of the prop's own FBX, each
    /// drawn with the prop's own material, whose map is the prop's own colour map, imported sRGB with mipmaps and not readable
    /// on the CPU.</summary>
    /// <summary>What is wrong with a placed prop's prefab: it must draw every one of <paramref name="sourceMeshes"/>, one
    /// renderer per part, each with <paramref name="material"/>, whose map is <paramref name="map"/>.</summary>
    static List<string> PrefabProblems(GameObject prefab, Mesh[] sourceMeshes, Material material, Texture map)
    {
        var problems = new List<string>();
        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        if (sourceMeshes.Length == 0) problems.Add("no source mesh");
        var drawn = filters.Select(f => f.sharedMesh).ToList();
        if (sourceMeshes.Length != drawn.Count || sourceMeshes.Except(drawn).Any() || drawn.Except(sourceMeshes).Any()) problems.Add("not its FBX's own meshes, every one");
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length != filters.Length) problems.Add("not one renderer per part");
        if (!renderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m == material))) problems.Add("not its own material");
        if (material.mainTexture != map) problems.Add("not its own colour map");
        return problems;
    }

    /// <summary>What is wrong with a placed prop's colour map import: sRGB, mipmaps imported, not readable on the CPU.</summary>
    static List<string> MapProblems(bool sRGB, bool mipmaps, bool readable, int mipmapCount)
    {
        var problems = new List<string>();
        if (!sRGB) problems.Add("not sRGB");
        if (!mipmaps || mipmapCount <= 1) problems.Add("no mipmaps");
        if (readable) problems.Add("readable on the CPU");
        return problems;
    }

    /// <summary>A placed generated prop (#756) keeps its source in its streamed prefab: every part of the prop's own FBX, each
    /// drawn with the prop's own material, whose map is the prop's own colour map, imported sRGB with mipmaps and not readable
    /// on the CPU. The checks reject a wrong material and a map without mipmaps.</summary>
    [Test] public void PlacedGeneratedPropsKeepTheirSourceMeshMaterialAndMap()
    {
        var props = CollectionCatalog.Ids.Where(CollectionCatalog.IsGeneratedProp).ToList();
        CollectionAssert.Contains(props, CollectionCatalog.BedouinTent);
        foreach (string id in props)
        {
            string dir = $"{CollectionCatalog.Folder}/{id}", fbx = $"{dir}/{id}.fbx", map = $"{dir}/{id}_basecolor.jpg";
            var sourceMeshes = AssetDatabase.LoadAssetAtPath<GameObject>(fbx).GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{id}.mat");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(map);
            CollectionAssert.IsEmpty(PrefabProblems(prefab, sourceMeshes, material, texture), id);
            var ti = (TextureImporter)AssetImporter.GetAtPath(map);
            CollectionAssert.IsEmpty(MapProblems(ti.sRGBTexture, ti.mipmapEnabled, ti.isReadable, texture.mipmapCount), map);
        }
        // The checks see a wrong material on a copy of the tent and a colour map imported without mipmaps.
        string tent = CollectionCatalog.BedouinTent, tentDir = $"{CollectionCatalog.Folder}/{tent}";
        var tentMeshes = AssetDatabase.LoadAssetAtPath<GameObject>($"{tentDir}/{tent}.fbx").GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
        var tentMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{tentDir}/{tent}.mat");
        var copy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(tent, CollectionDetail.Near)));
        var other = new Material(tentMaterial);
        try
        {
            foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = other;
            CollectionAssert.Contains(PrefabProblems(copy, tentMeshes, tentMaterial, tentMaterial.mainTexture), "not its own material");
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); UnityEngine.Object.DestroyImmediate(other); }
        CollectionAssert.Contains(MapProblems(true, false, false, 1), "no mipmaps");
    }

    [Test] public void CampaignActivationWaitsForStreamingAndAddsNoBootAssetReference()
    {
        Assert.IsFalse(TownModels.Ids.Any(id => id.StartsWith("generated-1948-", StringComparison.Ordinal)));
        Assert.IsFalse(File.ReadAllText(Disk(TownModels.SetPath)).Contains("generated-1948-"));
        Assert.IsFalse(AssetDatabase.GetDependencies("Assets/Map.unity", true)
            .Any(p => p.StartsWith(Generated1948BuildingsPropsReview.AssetFolder, StringComparison.Ordinal)));
    }
}
