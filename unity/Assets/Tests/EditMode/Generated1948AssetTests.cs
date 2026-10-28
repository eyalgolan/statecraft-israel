#nullable enable
using System;
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
    [Test] public void PlacedGeneratedPropsKeepTheirSourceMeshMaterialAndMap()
    {
        var props = CollectionCatalog.Ids.Where(CollectionCatalog.IsGeneratedProp).ToList();
        CollectionAssert.IsSubsetOf(new[] { CollectionCatalog.BedouinTent, CollectionCatalog.SyrianR35 }, props);
        foreach (string id in props)
        {
            string dir = $"{CollectionCatalog.Folder}/{id}", fbx = CollectionCatalog.SourceModelPath(id), map = $"{dir}/{id}_basecolor.jpg";
            var sourceMeshes = AssetDatabase.LoadAssetAtPath<GameObject>(fbx).GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.Greater(sourceMeshes.Length, 0, fbx);
            CollectionAssert.AreEquivalent(sourceMeshes, filters.Select(f => f.sharedMesh), id + " draws its FBX's own meshes, every one");
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{id}.mat");
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.AreEqual(filters.Length, renderers.Length, id + " one renderer per part");
            Assert.IsTrue(renderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m == material)), id + " its own material");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(map);
            Assert.AreEqual(texture, material.mainTexture, id + " its own colour map");
            var ti = (TextureImporter)AssetImporter.GetAtPath(map);
            Assert.IsTrue(ti.sRGBTexture, map); Assert.IsTrue(ti.mipmapEnabled, map); Assert.IsFalse(ti.isReadable, map);
            Assert.Greater(texture.mipmapCount, 1, map + " imported mipmaps");
        }
    }

    [Test] public void CampaignActivationWaitsForStreamingAndAddsNoBootAssetReference()
    {
        Assert.IsFalse(TownModels.Ids.Any(id => id.StartsWith("generated-1948-", StringComparison.Ordinal)));
        Assert.IsFalse(File.ReadAllText(Disk(TownModels.SetPath)).Contains("generated-1948-"));
        Assert.IsFalse(AssetDatabase.GetDependencies("Assets/Map.unity", true)
            .Any(p => p.StartsWith(Generated1948BuildingsPropsReview.AssetFolder, StringComparison.Ordinal)));
    }
}
