using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>The import tool lives in the editor assembly, which tests cannot reference, so it is called by reflection.</summary>
public class PortraitImportTests
{
    const string Folder = "Assets/UI/Portraits";

    static int ExpectedCount() => ContentLoader.Load(Paths.Content).People.Count(p => p.Portrait != null && p.Portrait.File != ""
        && (p.Portrait.License == "Public domain" || p.Portrait.License == "CC0" || p.Portrait.License == "CC BY 4.0"
            || p.Portrait.License == "CC BY-SA 4.0"));

    static void RunImport()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.PortraitImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "PortraitImport not found");
        t!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
    }

    static string Snapshot()
    {
        using var sha = SHA256.Create();
        var sb = new System.Text.StringBuilder();
        foreach (var f in Directory.GetFiles(Folder).OrderBy(x => x, StringComparer.Ordinal))
            sb.Append(Path.GetFileName(f)).Append(' ').Append(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f)))).Append('\n');
        return sb.ToString();
    }

    [Test]
    public void RunningTheImportAgainChangesNothing()
    {
        var before = Snapshot();
        RunImport();
        Assert.AreEqual(before, Snapshot());
    }

    [Test]
    public void NoPortraitIsLargerThan512PixelsOnAnyTarget()
    {
        var paths = Directory.GetFiles(Folder, "*.jpg");
        Assert.AreEqual(ExpectedCount(), paths.Length);
        Assert.Greater(paths.Length, 0);
        foreach (var path in paths.Select(p => p.Replace('\\', '/')))
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.LessOrEqual(imp.maxTextureSize, 512, path);
            foreach (var target in new[] { "DefaultTexturePlatform", "Standalone", "WebGL" })
                Assert.LessOrEqual(imp.GetPlatformTextureSettings(target).maxTextureSize, 512, path + " " + target);
            StringAssert.DoesNotContain("  maxTextureSize: 2048", File.ReadAllText(path + ".meta"), path + " legacy field");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.LessOrEqual(Mathf.Max(tex.width, tex.height), 512, path + " source size");
        }
    }

    [Test]
    public void AnUnreadableSourceLeavesTheFolderAndCatalogUntouched()
    {
        var stale = Folder + "/zz_stale.jpg";
        File.WriteAllBytes(stale, new byte[] { 1, 2, 3 });      // would be deleted by a half-run import
        try
        {
            var before = Snapshot();
            var t = Type.GetType("Statecraft.Game.EditorTools.PortraitImport, Assembly-CSharp-Editor")!;
            int calls = 0;
            Func<string, byte[]> corruptSecond = f => ++calls == 2 ? new byte[] { 0, 1, 2, 3, 4 } : File.ReadAllBytes(f);
            var ex = Assert.Throws<TargetInvocationException>(() => t.GetMethod("Import", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { corruptSecond }));
            StringAssert.Contains("cannot be read", ex!.InnerException!.Message);
            Assert.AreEqual(before, Snapshot());
            Assert.IsTrue(File.Exists(stale), "nothing was deleted");
        }
        finally { File.Delete(stale); File.Delete(stale + ".meta"); }
    }
}
