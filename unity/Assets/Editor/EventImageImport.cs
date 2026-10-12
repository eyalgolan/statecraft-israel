using System.IO;
using System.Linq;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Rebuilds Assets/Resources/EventImageCatalog.asset from Assets/UI/EventImages/event-images.yaml (the
    /// Unity-side event -> photograph mapping, never game content) and sets web-friendly import settings on every
    /// picture. Every row is checked first (EventImageMapping.Problems, and the file must exist), so a bad mapping leaves
    /// the catalog as it was. The JPGs themselves are made by tools/event_images.py. Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.EventImageImport.Run</summary>
    public static class EventImageImport
    {
        public const string CatalogPath = "Assets/Resources/" + EventImageCatalog.ResourcePath + ".asset";
        public const int MaxEdge = 1024;

        public static void Run()
        {
            var rows = EventImageMapping.Parse(File.ReadAllText(EventImageMapping.Path));
            var problems = EventImageMapping.Problems(rows, ContentLoader.Load(Paths.Content));
            foreach (var r in rows)
                if (!File.Exists($"{EventImageMapping.Folder}/{r.File}")) problems.Add($"{r.Event}: {EventImageMapping.Folder}/{r.File} missing (run tools/event_images.py)");
            if (problems.Count > 0) throw new System.InvalidOperationException(EventImageMapping.Path + ":\n" + string.Join("\n", problems));

            var catalog = AssetDatabase.LoadAssetAtPath<EventImageCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
                catalog = ScriptableObject.CreateInstance<EventImageCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.entries.Clear();
            foreach (var r in rows.OrderBy(r => r.Event, System.StringComparer.Ordinal))
            {
                var path = $"{EventImageMapping.Folder}/{r.File}";
                AssetDatabase.ImportAsset(path);
                Configure(path);
                catalog.entries.Add(new EventImageCatalog.Entry
                {
                    eventId = r.Event, image = AssetDatabase.LoadAssetAtPath<Texture2D>(path), alt = r.Alt, credit = r.Credit,
                    sensitive = r.Sensitive, sensitivityReviewed = r.SensitivityReviewed,
                });
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The portraits' settings (PortraitImport.Configure) with a higher quality: no mipmaps, clamp, sRGB,
        /// crunched, the same cap on every platform block. Saves and reimports only when something changed.</summary>
        static void Configure(string path)
        {
            const int quality = 75;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool changed = importer.textureType != TextureImporterType.Default || importer.maxTextureSize != MaxEdge || importer.mipmapEnabled
                || !importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Clamp || !importer.crunchedCompression
                || importer.compressionQuality != quality || importer.npotScale != TextureImporterNPOTScale.None;
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = MaxEdge;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.crunchedCompression = true;
            importer.compressionQuality = quality;
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var s = importer.GetPlatformTextureSettings(target);
                if (s.overridden || s.maxTextureSize != MaxEdge || !s.crunchedCompression || s.compressionQuality != quality) changed = true;
                s.overridden = false;
                s.maxTextureSize = MaxEdge;
                s.crunchedCompression = true;
                s.compressionQuality = quality;
                importer.SetPlatformTextureSettings(s);
            }
            var so = new SerializedObject(importer);
            var legacy = so.FindProperty("m_MaxTextureSize");
            if (legacy != null && legacy.intValue != MaxEdge) { legacy.intValue = MaxEdge; so.ApplyModifiedPropertiesWithoutUndo(); changed = true; }
            if (changed) importer.SaveAndReimport();
        }
    }
}
