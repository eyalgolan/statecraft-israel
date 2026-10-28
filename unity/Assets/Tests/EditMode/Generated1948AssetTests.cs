#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Statecraft.Game;
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
        var files = AssetDatabase.FindAssets("t:Model", new[] { Generated1948BuildingsPropsReview.AssetFolder, Statecraft.Game.Map.Collection.CollectionCatalog.Folder, TownModels.Folder })
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
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(Generated1948BuildingsPropsReview.PrefabOf(receipt.id)), receipt.id);
        }
    }

    /// <summary>The five houses placed in the town kits (owner, 27 October 2026, #756) stream after the first frame like the other
    /// town buildings: the map scene reaches none of the generated models, in review or placed.</summary>
    [Test] public void PlacedHousesStreamAndAddNoBootAssetReference()
    {
        CollectionAssert.AreEqual(new[] { "generated-1948-01-netanya-tiled-house-original", "generated-1948-02-netanya-two-storey-balcony-house",
            "generated-1948-03-netanya-flat-roof-porch-house", "generated-1948-04-dark-basalt-house", "generated-1948-05-plastered-courtyard-house-original" },
            TownModels.Ids.Where(id => id.StartsWith("generated-1948-", StringComparison.Ordinal)));
        Assert.IsFalse(AssetDatabase.GetDependencies("Assets/Map.unity", true)
            .Any(p => p.StartsWith(Generated1948BuildingsPropsReview.AssetFolder, StringComparison.Ordinal) || p.Contains("/generated-1948-")));
    }
}
