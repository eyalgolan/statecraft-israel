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
        public string id = "", source_glb_sha256 = "", embedded_image_sha256 = "";
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
        var files = AssetDatabase.FindAssets("t:Model", new[] { Generated1948BuildingsPropsReview.AssetFolder })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".fbx", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(11, files.Length);
        foreach (string fbx in files)
        {
            string dir = Path.GetDirectoryName(fbx)!;
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(Disk(dir + "/provenance.json")));
            Assert.AreEqual(receipt.source_glb_sha256, Hash(Disk(dir + "/" + receipt.id + ".glb")), receipt.id);
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
            Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression, image);
            Assert.GreaterOrEqual(ti.maxTextureSize, Math.Max(receipt.embedded_dimensions[0], receipt.embedded_dimensions[1]));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/" + receipt.id + ".prefab"));
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
