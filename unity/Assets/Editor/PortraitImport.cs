using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Copies every portrait people.yaml names (public-domain files under promo/ref/people) into
    /// Assets/UI/Portraits/&lt;person id&gt;.jpg, downscaled so the longest edge is at most <see cref="MaxEdge"/> pixels (the
    /// originals stay in promo/ref/people), sets web-friendly import settings on every platform block, writes PORTRAITS.txt
    /// (id, licence, source) and rebuilds PortraitCatalog.asset. Every licence and file is checked before anything is
    /// written, and portraits of people who no longer have one are removed. Idempotent: a second run changes nothing.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.PortraitImport.Run</summary>
    public static class PortraitImport
    {
        public const string Folder = "Assets/UI/Portraits";
        public const string CatalogPath = Folder + "/PortraitCatalog.asset";
        public const int MaxEdge = 512;

        public static void Run() => Import(f => File.ReadAllBytes(f));

        /// <summary>Run with a different way of reading source files (tests inject a corrupt one).</summary>
        public static void Import(System.Func<string, byte[]> readSource)
        {
            var content = ContentLoader.Load(Paths.Content);
            var people = content.People.Where(p => p.Portrait != null && p.Portrait.File != "").ToList();

            // Validate everything first, so a bad record leaves nothing half-imported.
            foreach (var p in people)
            {
                if (p.Portrait!.License != "Public domain" && p.Portrait.License != "CC0" && p.Portrait.License != "CC BY 4.0"
                    && p.Portrait.License != "CC BY-SA 4.0")
                    throw new System.InvalidOperationException($"people.yaml: {p.Id} portrait licence '{p.Portrait.License}' is not public domain, CC0, CC BY 4.0 or CC BY-SA 4.0");
                var source = Path.Combine(Paths.RepoRoot, p.Portrait.File);
                if (!File.Exists(source)) throw new FileNotFoundException($"people.yaml: {p.Id} portrait file missing", source);
            }

            // Decode and downscale everything in memory before touching the folder or the catalog: an unreadable image
            // must leave both exactly as they were.
            var images = new Dictionary<string, byte[]>();
            foreach (var p in people)
            {
                try { images[p.Id] = Downscale(readSource(Path.Combine(Paths.RepoRoot, p.Portrait!.File))); }
                catch (System.Exception e) when (!(e is System.InvalidOperationException && e.Message.StartsWith("people.yaml")))
                { throw new System.InvalidOperationException($"people.yaml: {p.Id} portrait '{p.Portrait!.File}' cannot be read: {e.Message}", e); }
            }

            Directory.CreateDirectory(Folder);
            var wanted = new HashSet<string>(people.Select(p => $"{Folder}/{p.Id}.jpg"));
            foreach (var stale in Directory.GetFiles(Folder, "*.jpg").Select(f => f.Replace('\\', '/')).Where(f => !wanted.Contains(f)).ToList())
                AssetDatabase.DeleteAsset(stale);

            var credits = new StringBuilder("# Portraits shipped in the build (copied from people.yaml portrait records; public domain, CC0, CC BY 4.0 and CC BY-SA 4.0 (attribution in NOTICE)).\n");
            var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PortraitCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.entries.Clear();
            foreach (var p in people)
            {
                var target = $"{Folder}/{p.Id}.jpg";
                var bytes = images[p.Id];
                if (!File.Exists(target) || !File.ReadAllBytes(target).SequenceEqual(bytes))
                {
                    File.WriteAllBytes(target, bytes);
                    AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
                }
                else AssetDatabase.ImportAsset(target);
                Configure(target);
                catalog.entries.Add(new PortraitCatalog.Entry { personId = p.Id, image = AssetDatabase.LoadAssetAtPath<Texture2D>(target), focusY = (float)p.Portrait!.FocusY!.Value });
                credits.Append(p.Id).Append(" | ").Append(p.Portrait.License).Append(" | ").Append(p.Portrait.Source).Append('\n');
            }
            var creditsPath = Folder + "/PORTRAITS.txt";
            if (!File.Exists(creditsPath) || File.ReadAllText(creditsPath) != credits.ToString())
            {
                File.WriteAllText(creditsPath, credits.ToString());
                AssetDatabase.ImportAsset(creditsPath);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Sets the same limits on the default block and on every platform block, so no stale per-platform value
        /// (an old 2048) can bypass the cap. Saves and reimports only when something changed.</summary>
        static void Configure(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool changed = importer.textureType != TextureImporterType.Default || importer.maxTextureSize != MaxEdge || importer.mipmapEnabled
                || !importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Clamp || !importer.crunchedCompression || importer.compressionQuality != 50;
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = MaxEdge;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.crunchedCompression = true;
            importer.compressionQuality = 50;
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var s = importer.GetPlatformTextureSettings(target);
                if (s.overridden || s.maxTextureSize != MaxEdge || !s.crunchedCompression || s.compressionQuality != 50) changed = true;
                s.overridden = false;
                s.maxTextureSize = MaxEdge;
                s.crunchedCompression = true;
                s.compressionQuality = 50;
                importer.SetPlatformTextureSettings(s);
            }
            // The .meta file's top-level (legacy) maxTextureSize, which Unity writes as 2048 for a new texture, is not
            // exposed by the importer API; set it through the serialized property.
            var so = new SerializedObject(importer);
            var legacy = so.FindProperty("m_MaxTextureSize");
            if (legacy != null && legacy.intValue != MaxEdge) { legacy.intValue = MaxEdge; so.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        /// <summary>Returns the image as a JPEG whose longest edge is at most <see cref="MaxEdge"/> (area-averaged), or the
        /// original bytes when it already fits. Deterministic, so the output is byte-identical on every run.</summary>
        static byte[] Downscale(byte[] jpg)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(src, jpg)) throw new System.InvalidOperationException("portrait is not a readable image");
                int w = src.width, h = src.height;
                if (System.Math.Max(w, h) <= MaxEdge) return jpg;
                float scale = (float)MaxEdge / System.Math.Max(w, h);
                int nw = System.Math.Max(1, Mathf.RoundToInt(w * scale)), nh = System.Math.Max(1, Mathf.RoundToInt(h * scale));
                var px = src.GetPixels32();
                var outPx = new Color32[nw * nh];
                for (int y = 0; y < nh; y++)
                {
                    int y0 = y * h / nh, y1 = System.Math.Max(y0 + 1, (y + 1) * h / nh);
                    for (int x = 0; x < nw; x++)
                    {
                        int x0 = x * w / nw, x1 = System.Math.Max(x0 + 1, (x + 1) * w / nw);
                        long r = 0, g = 0, b = 0, n = 0;
                        for (int sy = y0; sy < y1; sy++)
                            for (int sx = x0; sx < x1; sx++) { var c = px[sy * w + sx]; r += c.r; g += c.g; b += c.b; n++; }
                        outPx[y * nw + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                    }
                }
                var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
                try { dst.SetPixels32(outPx); return ImageConversion.EncodeToJPG(dst, 90); }
                finally { Object.DestroyImmediate(dst); }
            }
            finally { Object.DestroyImmediate(src); }
        }
    }
}
