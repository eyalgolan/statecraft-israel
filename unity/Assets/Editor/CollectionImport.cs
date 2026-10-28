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
    /// <summary>Imports Tal Heuberger's original models, with only the approved same-length author-path blanking (#1273).
    /// docs/original-models.json pins all 73. Uses no mesh or animation compression, keyframe reduction, Crunch, texture
    /// downscaling or generated LODs, keeping every source take. Makes one prefab per model and one integrity-checked
    /// streaming bundle per model. OriginalModelsStreamingTests
    /// pins the import settings. Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.CollectionImport.Run</summary>
    public static class CollectionImport
    {
        public const int MaxTextureSize = 8192;

        static string Dir(string id) => $"{CollectionCatalog.Folder}/{id}";
        public static string Model(string id, CollectionDetail d) => $"{Dir(id)}/{id}.fbx";
        public static string Controller(string id) => $"{Dir(id)}/{id}-near.controller";

        public static void Run()
        {
            foreach (var id in CollectionCatalog.Ids.Where(id => !CollectionCatalog.IsBuilding(id) && !CollectionCatalog.IsFullQualityGeneratedInfantry(id)))
            {
                ImportModel(Model(id, CollectionDetail.Far), animated: false);
                ImportModel(Model(id, CollectionDetail.Near), animated: CollectionCatalog.Animated.Contains(id));
                ImportColourMap($"{Dir(id)}/{id}_basecolor.jpg");
            }
            foreach (var id in CollectionCatalog.Ids.Where(id => !CollectionCatalog.IsBuilding(id) && !CollectionCatalog.IsFullQualityGeneratedInfantry(id)))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Dir(id)}/{id}.mat")
                    ?? throw new InvalidOperationException($"{id}: no material");
                if (!mat.enableInstancing) { mat.enableInstancing = true; EditorUtility.SetDirty(mat); }
                MakePrefab(id, CollectionDetail.Near, mat, (CollectionCatalog.Animated.Contains(id) || OriginalModelPose.Required(id)) ? IdleController(id) : null);
            }
            AssetDatabase.SaveAssets();
            Generated1948Characters.ImportRuntime();
            MakeBuildingPrefabs();
            Debug.Log(FormattableString.Invariant($"CollectionImport: {CollectionCatalog.Ids.Count(id => !CollectionCatalog.IsBuilding(id) && !CollectionCatalog.IsFullQualityGeneratedInfantry(id))} collection models"));
        }

        /// <summary>Issue #707: the special-buildings bundle's prefabs, one per model, beside #675's imported model in its own
        /// folder (CollectionCatalog.PrefabPath): the model with its own material on every renderer. The models' import
        /// settings are #675's and are left as they are.</summary>
        public static void MakeBuildingPrefabs()
        {
            foreach (var id in CollectionCatalog.Ids.Where(CollectionCatalog.IsBuilding))
            {
                string dir = $"{CollectionCatalog.BuildingsFolder}/{id}";
                ImportModel($"{dir}/{id}.fbx", animated: false);
                ImportColourMap($"{dir}/{id}_basecolor.jpg");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/{id}.fbx") ?? throw new InvalidOperationException($"{id}: no model");
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{id}.mat") ?? throw new InvalidOperationException($"{id}: no material");
                mat.enableInstancing = true; EditorUtility.SetDirty(mat);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                try
                {
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterials = Enumerable.Repeat(mat, Math.Max(1, r.sharedMaterials.Length)).ToArray();
                    go.AddComponent<OriginalModelDistance>();
                    PrefabUtility.SaveAsPrefabAsset(go, CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CollectionImport: 23 original building prefabs");
        }

        static void ImportModel(string path, bool animated)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter ?? throw new FileNotFoundException("no model", path);
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.meshCompression = ModelImporterMeshCompression.Off;
            imp.isReadable = false;
            imp.optimizeMeshPolygons = false;
            imp.optimizeMeshVertices = false;
            imp.importBlendShapes = true;
            imp.importVisibility = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.addCollider = false;
            imp.generateSecondaryUV = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.None;   // no normal map
            imp.materialImportMode = ModelImporterMaterialImportMode.None;   // the prefab draws with the .mat beside it
            // Lossless (#1273): every source take, no resampling, no keyframe reduction, no generated LODs.
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.optimizeGameObjects = false;
            imp.importAnimation = true;
            imp.resampleCurves = false;
            imp.animationCompression = ModelImporterAnimationCompression.Off;
            imp.clipAnimations = imp.defaultClipAnimations;
            var serialized = new SerializedObject(imp);
            var lods = serialized.FindProperty("m_GenerateMeshLods") ?? serialized.FindProperty("generateMeshLods");
            if (lods != null) { lods.boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo(); }
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
            imp.crunchedCompression = false;
            imp.compressionQuality = 50;
            foreach (var target in new[] { "Standalone", "WebGL" })
            {
                var s = imp.GetPlatformTextureSettings(target);
                s.overridden = true;
                s.maxTextureSize = MaxTextureSize;
                s.format = TextureImporterFormat.Automatic;
                s.textureCompression = TextureImporterCompression.Compressed;
                s.crunchedCompression = false;
                s.compressionQuality = 50;
                imp.SetPlatformTextureSettings(s);
            }
            imp.SaveAndReimport();
        }

        /// <summary>The original model's one-state controller using its idle clip (C05 uses his walk) (made once, then kept, so its GUID holds).</summary>
        static AnimatorController IdleController(string id)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Model(id, CollectionDetail.Near)).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).OrderBy(c => c.name.IndexOf(id == "figure-c05-pioneer-veteran-v2" ? "walk" : "idle", StringComparison.OrdinalIgnoreCase) < 0 ? 1 : 0).ThenBy(c => c.name, StringComparer.Ordinal).First();
            var path = Controller(id);
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
            var state = ctrl.layers[0].stateMachine.defaultState;
            if (state.motion != clip) { state.motion = clip; EditorUtility.SetDirty(ctrl); }
            AddWalkState(ctrl, Model(id, CollectionDetail.Near));
            return ctrl;
        }

        /// <summary>Manual preparation for the original army figures and generated figures, including civilians.
        /// Run from the menu or with -executeMethod Statecraft.Game.EditorTools.CollectionImport.PrepareFigureControllers.
        /// Adds only existing source states; does not reimport models, textures or prefabs.</summary>
        [MenuItem("Statecraft/Collection/Prepare Figure Controllers")]
        public static void PrepareFigureControllers()
        {
            var originals = new[] { "figure-c04-sabra-pioneer-v3", "figure-c05-pioneer-veteran-v2",
                "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2", "figure-c08-arab-legion-v2",
                "figure-c09-irregular-v2", "figure-c12f-palmach-scout-v2" };
            foreach (var id in originals)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller(id));
                if (controller == null) throw new InvalidOperationException(id + ": missing existing controller");
                AddWalkState(controller, Model(id, CollectionDetail.Near));
            }
            foreach (var id in Generated1948Characters.Ids)
            {
                var dir = Generated1948Characters.Dir(id);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{dir}/{id}.controller");
                if (controller == null) throw new InvalidOperationException(id + ": missing existing controller");
                AddWalkState(controller, $"{dir}/{id}.fbx");
            }
            AssetDatabase.SaveAssets();
        }

        internal static void AddWalkState(AnimatorController controller, string model)
        {
            var walk = AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                    (c.name.EndsWith("|walk", StringComparison.OrdinalIgnoreCase) || c.name == "walk" || c.name.EndsWith("|idle_walk", StringComparison.OrdinalIgnoreCase)));
            var fire = AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == "fire" || c.name.EndsWith("|fire", StringComparison.OrdinalIgnoreCase));
            if (fire != null)
            {
                var sm = controller.layers[0].stateMachine;
                var attack = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "attack") ?? sm.AddState("attack");
                attack.motion = fire;
                EditorUtility.SetDirty(controller);
            }
            if (walk == null) return;
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "walk") ?? machine.AddState("walk");
            state.motion = walk;
            EditorUtility.SetDirty(controller);
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
                go.AddComponent<OriginalModelDistance>();
                if (OriginalModelPose.Required(id)) go.AddComponent<OriginalModelPose>();
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
            assetNames = b.Ids.SelectMany(id => new[] { CollectionCatalog.PrefabPath(id, CollectionDetail.Far), CollectionCatalog.PrefabPath(id, CollectionDetail.Near), CollectionCatalog.IsBuilding(id) ? $"{CollectionCatalog.BuildingsFolder}/{id}/{id}.fbx" : Model(id, CollectionDetail.Near) }).Distinct().ToArray(),
        }).ToArray();

        /// <summary>Writes <see cref="CollectionCatalog.ChecksFile"/> into <paramref name="folder"/> for the built
        /// <paramref name="bundles"/> there: each one's Unity CRC, size and SHA-256. Throws when Unity has no CRC for one.</summary>
        public static void WriteChecks(string folder, string[] bundles) => WriteChecksFile(folder, bundles, CollectionCatalog.ChecksFile);

        /// <summary><see cref="WriteChecks(string, string[])"/> into the checks file named <paramref name="checksFile"/>.</summary>
        static void WriteChecksFile(string folder, string[] bundles, string checksFile)
        {
            var lines = bundles.OrderBy(b => b, StringComparer.Ordinal).Select(b =>
            {
                var path = Path.Combine(folder, b);
                if (!BuildPipeline.GetCRCForAssetBundle(path, out var crc))
                    throw new InvalidOperationException("CollectionImport: no CRC for the bundle " + path);
                var check = new CollectionCatalog.BundleCheck(crc, new FileInfo(path).Length, CollectionCatalog.Sha256(path));
                return $"{b} {check}";
            });
            File.WriteAllText(Path.Combine(folder, checksFile), string.Join("\n", lines) + "\n");
        }

        /// <summary>Builds the bundles for <paramref name="target"/> and copies them into StreamingAssets/collection (git-ignored,
        /// like content/), so the player build carries them as plain files beside it. Returns the manifest path for
        /// BuildPlayerOptions.assetBundleManifestPath (which keeps the classes only the bundles use from being stripped).
        /// Throws when the build fails: a player build without its bundles would boot without the models.</summary>
        public static string BuildBundles(BuildTarget target) =>
            BuildStreamed(target, BundleCache(target), Builds(), CollectionCatalog.StreamingFolder, CollectionCatalog.ChecksFile);

        /// <summary><see cref="BuildBundles"/> for any set of streamed bundles (CollectionStream.Catalog): builds
        /// <paramref name="builds"/> into <paramref name="cache"/> and copies them, with their checks file, into
        /// StreamingAssets/<paramref name="streamingFolder"/>. Returns the manifest path.</summary>
        public static string BuildStreamed(BuildTarget target, string cache, AssetBundleBuild[] builds, string streamingFolder, string checksFile)
        {
            Directory.CreateDirectory(cache);
            var manifest = BuildPipeline.BuildAssetBundles(cache, builds,
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
            if (manifest == null) throw new InvalidOperationException("CollectionImport: the bundle build failed for " + streamingFolder);
            var dest = Path.Combine(Application.streamingAssetsPath, streamingFolder);
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
            WriteChecksFile(cache, manifest.GetAllAssetBundles(), checksFile);
            File.Copy(Path.Combine(cache, checksFile), Path.Combine(dest, checksFile));
            Debug.Log($"BUNDLES {target}: {manifest.GetAllAssetBundles().Length} bundles, {bytes} bytes -> {dest}");
            return Path.Combine(cache, Path.GetFileName(cache) + ".manifest");
        }
    }
}
