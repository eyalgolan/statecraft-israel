#nullable enable
using System;
using System.IO;
using System.Linq;
using Statecraft.Game.Map.Collection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>The 1948 Collection's import (issue #664): sets the import settings of the 31 slimmed models in
    /// <see cref="CollectionCatalog.Folder"/> (written by tools/slim_collection.py from Tal's #507 files), makes each model's
    /// &lt;id&gt;-far and &lt;id&gt;-near prefabs (and the near idle controller where there is one), and builds the streamed
    /// AssetBundles for a player build. CollectionImportTests pins what Run leaves behind. Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.CollectionImport.Run</summary>
    public static class CollectionImport
    {
        public const int MaxTextureSize = 512;

        static string Dir(string id) => $"{CollectionCatalog.Folder}/{id}";
        public static string Model(string id, CollectionDetail d) => $"{Dir(id)}/{id}-{(d == CollectionDetail.Far ? "far" : "near")}.fbx";
        public static string Controller(string id) => $"{Dir(id)}/{id}-near.controller";

        public static void Run()
        {
            foreach (var id in CollectionCatalog.Ids)
            {
                ImportModel(Model(id, CollectionDetail.Far), animated: false);
                ImportModel(Model(id, CollectionDetail.Near), animated: CollectionCatalog.Animated.Contains(id));
                ImportColourMap($"{Dir(id)}/{id}_basecolor.jpg");
            }
            foreach (var id in CollectionCatalog.Ids)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Dir(id)}/{id}.mat")
                    ?? throw new InvalidOperationException($"{id}: no material");
                if (!mat.enableInstancing) { mat.enableInstancing = true; EditorUtility.SetDirty(mat); }
                MakePrefab(id, CollectionDetail.Far, mat, null);
                MakePrefab(id, CollectionDetail.Near, mat, CollectionCatalog.Animated.Contains(id) ? IdleController(id) : null);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"CollectionImport: {CollectionCatalog.Ids.Count()} models");
        }

        static void ImportModel(string path, bool animated)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter ?? throw new FileNotFoundException("no model", path);
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.meshCompression = ModelImporterMeshCompression.Medium;
            imp.isReadable = false;
            imp.optimizeMeshPolygons = true;
            imp.optimizeMeshVertices = true;
            imp.importBlendShapes = false;
            imp.importVisibility = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.addCollider = false;
            imp.generateSecondaryUV = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.None;   // no normal map
            imp.materialImportMode = ModelImporterMaterialImportMode.None;   // the prefab draws with the .mat beside it
            if (animated)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.optimizeGameObjects = true;
                imp.importAnimation = true;
                imp.resampleCurves = true;
                imp.animationCompression = ModelImporterAnimationCompression.Optimal;
                var takes = imp.defaultClipAnimations;
                if (takes.Length != 1) throw new InvalidOperationException($"{path}: {takes.Length} takes, expected the idle clip only");
                var clip = takes[0];
                clip.name = "idle";
                clip.loopTime = true;
                imp.clipAnimations = new[] { clip };
            }
            else
            {
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
                imp.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
            }
            imp.SaveAndReimport();
        }

        static void ImportColourMap(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new FileNotFoundException("no colour map", path);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.isReadable = false;
            imp.maxTextureSize = MaxTextureSize;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.crunchedCompression = true;
            imp.compressionQuality = 50;
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var s = imp.GetPlatformTextureSettings(target);
                s.overridden = true;
                s.maxTextureSize = MaxTextureSize;
                s.format = TextureImporterFormat.Automatic;
                s.textureCompression = TextureImporterCompression.Compressed;
                s.crunchedCompression = true;
                s.compressionQuality = 50;
                imp.SetPlatformTextureSettings(s);
            }
            imp.SaveAndReimport();
        }

        /// <summary>The near model's one-state controller playing its idle clip (made once, then kept, so its GUID holds).</summary>
        static AnimatorController IdleController(string id)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Model(id, CollectionDetail.Near)).OfType<AnimationClip>()
                .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var path = Controller(id);
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) return AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
            var state = ctrl.layers[0].stateMachine.defaultState;
            if (state.motion != clip) { state.motion = clip; EditorUtility.SetDirty(ctrl); }
            return ctrl;
        }

        static void MakePrefab(string id, CollectionDetail detail, Material mat, AnimatorController? idle)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model(id, detail));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterials = Enumerable.Repeat(mat, Math.Max(1, r.sharedMaterials.Length)).ToArray();
                    if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = false;
                }
                var animator = go.GetComponent<Animator>();
                if (idle != null)
                {
                    animator.runtimeAnimatorController = idle;
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;
                }
                else if (animator != null) UnityEngine.Object.DestroyImmediate(animator);
                PrefabUtility.SaveAsPrefabAsset(go, CollectionCatalog.PrefabPath(id, detail));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Where a target's bundles are built (incremental, git-ignored).</summary>
        public static string BundleCache(BuildTarget target) => Path.Combine("Library", "CollectionBundles", target.ToString());

        /// <summary>The bundles: each catalog bundle carries its models' far and near prefabs; their meshes, clips,
        /// controllers, materials and colour maps come in as dependencies.</summary>
        public static AssetBundleBuild[] Builds() => CollectionCatalog.Bundles.Select(b => new AssetBundleBuild
        {
            assetBundleName = b.Bundle,
            assetNames = b.Ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near) }).ToArray(),
        }).ToArray();

        /// <summary>Writes <see cref="CollectionCatalog.ChecksFile"/> into <paramref name="folder"/> for the built
        /// <paramref name="bundles"/> there: each one's Unity CRC, size and SHA-256. Throws when Unity has no CRC for one.</summary>
        public static void WriteChecks(string folder, string[] bundles)
        {
            var lines = bundles.OrderBy(b => b, StringComparer.Ordinal).Select(b =>
            {
                var path = Path.Combine(folder, b);
                if (!BuildPipeline.GetCRCForAssetBundle(path, out var crc))
                    throw new InvalidOperationException("CollectionImport: no CRC for the bundle " + path);
                var check = new CollectionCatalog.BundleCheck(crc, new FileInfo(path).Length, CollectionCatalog.Sha256(path));
                return $"{b} {check}";
            });
            File.WriteAllText(Path.Combine(folder, CollectionCatalog.ChecksFile), string.Join("\n", lines) + "\n");
        }

        /// <summary>Builds the bundles for <paramref name="target"/> and copies them into StreamingAssets/collection (git-ignored,
        /// like content/), so the player build carries them as plain files beside it. Returns the manifest path for
        /// BuildPlayerOptions.assetBundleManifestPath (which keeps the classes only the bundles use from being stripped).
        /// Throws when the build fails: a player build without its bundles would boot without the models.</summary>
        public static string BuildBundles(BuildTarget target)
        {
            var cache = BundleCache(target);
            Directory.CreateDirectory(cache);
            var manifest = BuildPipeline.BuildAssetBundles(cache, Builds(),
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
            if (manifest == null) throw new InvalidOperationException("CollectionImport: the bundle build failed");
            var dest = Path.Combine(Application.streamingAssetsPath, CollectionCatalog.StreamingFolder);
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.CreateDirectory(dest);
            long bytes = 0;
            foreach (var b in manifest.GetAllAssetBundles())
            {
                File.Copy(Path.Combine(cache, b), Path.Combine(dest, b));
                var size = new FileInfo(Path.Combine(dest, b)).Length;
                bytes += size;
                Debug.Log($"BUNDLE {b} {size} bytes");
            }
            // Unity reads a bundle's CRC from its .manifest, which stays in the cache (the player ships the bundles only), so
            // the checks are written there, over the same bytes, and copied beside the shipped bundles (found by #668: from
            // #680 every player build failed with "no CRC for the bundle").
            WriteChecks(cache, manifest.GetAllAssetBundles());
            File.Copy(Path.Combine(cache, CollectionCatalog.ChecksFile), Path.Combine(dest, CollectionCatalog.ChecksFile));
            Debug.Log($"BUNDLES {target}: {manifest.GetAllAssetBundles().Length} bundles, {bytes} bytes -> {dest}");
            return Path.Combine(cache, Path.GetFileName(cache) + ".manifest");
        }
    }
}
