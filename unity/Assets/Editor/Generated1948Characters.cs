using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Explicit seven-character import and asset-preview evidence. Walk is a review clip, not movement behaviour.</summary>
    public static class Generated1948Characters
    {
        public static readonly string[] Ids = {
            "figure-egyptian-infantry-1948-v1", "figure-syrian-infantry-1948-v1", "figure-ala-infantry-1948-v1",
            "figure-iraqi-infantry-1948-v1", "figure-lebanese-regular-1948-v1", "figure-bedouin-civilian-1948-v1",
            "figure-local-arab-irregular-1948-v1"
        };
        public static string Dir(string id)
        {
            int index = Array.IndexOf(Ids, id);
            if (index < 0) throw new ArgumentException("Character is outside the explicit import scope", nameof(id));
            return "Assets/Art/" + (index > 0 && index < 4 ? "Collection/" : "Generated1948Review/") + id;
        }
        static string Model(string id, string level) => $"{Dir(id)}/{id}-{level}.fbx";
        static AnimationClip Clip(string id, string level) => AssetDatabase.LoadAllAssetsAtPath(Model(id, level))
            .OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));

        [MenuItem("Statecraft/Generated 1948/Import seven characters")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            // Fail before creating prefabs if any delivery is incomplete.
            foreach (string id in Ids)
            {
                foreach (string level in new[] { "near", "far", "walk" })
                    if (!File.Exists(Model(id, level))) throw new FileNotFoundException("Incomplete character delivery", Model(id, level));
                if (!File.Exists($"{Dir(id)}/{id}_basecolor.jpg")) throw new FileNotFoundException("Missing character colour map", id);
            }
            foreach (string id in Ids)
            {
                ImportModel(Model(id, "near"), "idle");
                ImportModel(Model(id, "far"), null);
                ImportModel(Model(id, "walk"), "walk");
                ImportTexture($"{Dir(id)}/{id}_basecolor.jpg");
                var mat = Material(id);
                var controller = Controller(id);
                MakePrefab(id, "near", mat, controller);
                MakePrefab(id, "far", mat, null);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Generated1948Characters: seven explicit candidates imported. Walk remains a separate review clip; no movement behaviour added.");
        }

        static void ImportModel(string path, string clipName)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter ?? throw new FileNotFoundException("No model importer", path);
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
            imp.importTangents = ModelImporterTangents.None;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            if (clipName != null)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.optimizeGameObjects = true;
                imp.importAnimation = true;
                imp.resampleCurves = true;
                imp.animationCompression = ModelImporterAnimationCompression.Optimal;
                // Refresh takes after enabling animation if a prior review import disabled it.
                imp.SaveAndReimport();
                var takes = imp.defaultClipAnimations;
                if (takes.Length != 1) throw new InvalidOperationException(path + ": expected one " + clipName + " take, got " + takes.Length);
                var clip = takes[0];
                clip.name = clipName;
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

        static void ImportTexture(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new FileNotFoundException("No colour map importer", path);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.isReadable = false;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.crunchedCompression = true;
            imp.compressionQuality = 50;
            foreach (string target in new[] { "Standalone", "WebGL" })
            {
                var settings = imp.GetPlatformTextureSettings(target);
                settings.overridden = true;
                settings.maxTextureSize = 512;
                settings.format = TextureImporterFormat.Automatic;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.crunchedCompression = true;
                settings.compressionQuality = 50;
                imp.SetPlatformTextureSettings(settings);
            }
            imp.SaveAndReimport();
        }

        static Material Material(string id)
        {
            string path = $"{Dir(id)}/{id}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("URP Lit unavailable");
                mat = new Material(shader) { name = id };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir(id)}/{id}_basecolor.jpg"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Cull", 0f); // Keep thin cloth visible from both sides; skin/topology checks remain separate.
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController Controller(string id)
        {
            string path = $"{Dir(id)}/{id}-near.controller";
            var clip = Clip(id, "near");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) return AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
            if (controller.layers.Length != 1 || controller.layers[0].stateMachine.states.Length != 1)
                throw new InvalidOperationException(id + ": expected an owned one-state idle controller");
            controller.layers[0].stateMachine.defaultState.motion = clip;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void MakePrefab(string id, string level, Material mat, AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model(id, level)) ?? throw new InvalidOperationException("Model missing: " + id);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = Enumerable.Repeat(mat, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
                    if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = false;
                }
                var animator = go.GetComponentInChildren<Animator>(true);
                if (controller != null)
                {
                    if (animator == null || animator.avatar == null || !animator.avatar.isValid)
                        throw new InvalidOperationException(id + ": no valid Generic Avatar");
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;
                }
                else if (animator != null) Object.DestroyImmediate(animator);
                PrefabUtility.SaveAsPrefabAsset(go, $"{Dir(id)}/{id}-{level}.prefab");
            }
            finally { Object.DestroyImmediate(go); }
        }

        static PlayableGraph Pose(GameObject go, AnimationClip clip, float fraction)
        {
            var animator = go.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            var graph = PlayableGraph.Create("Generated1948 asset preview");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            var output = AnimationPlayableOutput.Create(graph, "actual Animator", animator);
            output.SetSourcePlayable(playable);
            graph.Play();
            playable.SetTime(clip.length * fraction);
            graph.Evaluate(0f);
            return graph;
        }

        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Preview has no renderers");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        [MenuItem("Statecraft/Generated 1948/Capture character asset previews")]
        public static void Capture()
        {
            string output = Path.GetFullPath("../captures/generated-1948-characters");
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var files = new List<string>();
            try
            {
                var camera = new GameObject("Character preview camera").AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.18f, .21f, .24f);
                camera.orthographic = true;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 100f;
                foreach (var angle in new[] { new Vector3(35, -30, 0), new Vector3(25, 160, 0) })
                {
                    var light = new GameObject("Character preview light").AddComponent<Light>();
                    SceneManager.MoveGameObjectToScene(light.gameObject, scene);
                    light.type = LightType.Directional;
                    light.intensity = .9f;
                    light.transform.rotation = Quaternion.Euler(angle);
                }
                foreach (string id in Ids)
                {
                    foreach (string level in new[] { "near", "far" })
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir(id)}/{id}-{level}.prefab")
                            ?? throw new InvalidOperationException("Import characters before capture: " + id);
                        var go = Object.Instantiate(prefab);
                        SceneManager.MoveGameObjectToScene(go, scene);
                        try
                        {
                            if (level == "near")
                            {
                                foreach (string motion in new[] { "idle", "walk" })
                                    foreach (float fraction in new[] { .1f, .6f })
                                    {
                                        var graph = Pose(go, Clip(id, motion == "idle" ? "near" : "walk"), fraction);
                                        try
                                        {
                                            foreach (bool rear in new[] { false, true })
                                            {
                                                string name = id + "-near-" + motion + "-" + (fraction < .5f ? "10" : "60") + "-" + (rear ? "rear" : "front") + "-ASSET-PREVIEW.png";
                                                Render(camera, go, rear, 512, 420, Path.Combine(output, name));
                                                files.Add(name);
                                            }
                                        }
                                        finally { graph.Destroy(); }
                                    }
                            }
                            else foreach (int pixels in new[] { 24, 48, 96 })
                                foreach (bool rear in new[] { false, true })
                                {
                                    string name = id + "-far-" + pixels + "px-" + (rear ? "rear" : "front") + "-ASSET-PREVIEW.png";
                                    Render(camera, go, rear, 256, pixels, Path.Combine(output, name));
                                    files.Add(name);
                                }
                        }
                        finally { Object.DestroyImmediate(go); }
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            File.WriteAllLines(Path.Combine(output, "ASSET-PREVIEWS.txt"), new[] {
                "ACTUAL UNITY ASSET PREVIEWS — NOT GAMEPLAY",
                "Near: actual Animator evaluation at idle/walk fractions 0.1 and 0.6; front and rear.",
                "Far: static derivative at approximate 24/48/96 pixel figure heights on a 256 pixel canvas.",
                "Headcloth, feet, silhouette and likeness still require visual review. Camera labels refer to generated asset axes.",
                "Lighting/camera are isolated preview settings, without gameplay post-processing."
            }.Concat(files));
            Debug.Log("Generated1948Characters: wrote " + files.Count + " Unity asset previews, not gameplay: " + output);
        }

        static void Render(Camera camera, GameObject go, bool rear, int canvasPixels, int heightPixels, string path)
        {
            // Use actual baked skin bounds for framing; imported rest-pose bounds can be larger than an evaluated pose.
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var bounds = BoundsOf(go);
            if (skin != null)
            {
                var baked = new Mesh();
                try
                {
                    skin.BakeMesh(baked);
                    var vertices = baked.vertices.Select(v => skin.transform.TransformPoint(v)).ToArray();
                    bounds = new Bounds(vertices[0], Vector3.zero);
                    foreach (var vertex in vertices) bounds.Encapsulate(vertex);
                }
                finally { Object.DestroyImmediate(baked); }
            }
            camera.orthographicSize = bounds.size.y * canvasPixels / (2f * heightPixels);
            float distance = Mathf.Max(4f, bounds.size.magnitude * 3f);
            camera.transform.position = bounds.center + new Vector3(rear ? -.08f : .08f, .04f, rear ? -1f : 1f).normalized * distance;
            camera.transform.LookAt(bounds.center);
            var rt = new RenderTexture(canvasPixels, canvasPixels, 24);
            var image = new Texture2D(canvasPixels, canvasPixels, TextureFormat.RGB24, false);
            var prior = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, canvasPixels, canvasPixels), 0, 0);
                // Explicit labels live in the image bytes, not just filenames or surrounding prose.
                Label(image, "ASSET PREVIEW", 6, 6);
                Label(image, "NOT GAMEPLAY", 6, 16);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = prior;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(image);
            }
        }

        static readonly Dictionary<char, string> Glyph = new Dictionary<char, string> {
            ['A']="01110/10001/10001/11111/10001/10001/10001", ['S']="01111/10000/10000/01110/00001/00001/11110",
            ['E']="11111/10000/10000/11110/10000/10000/11111", ['T']="11111/00100/00100/00100/00100/00100/00100",
            ['P']="11110/10001/10001/11110/10000/10000/10000", ['R']="11110/10001/10001/11110/10100/10010/10001",
            ['V']="10001/10001/10001/10001/10001/01010/00100", ['I']="11111/00100/00100/00100/00100/00100/11111",
            ['W']="10001/10001/10001/10101/10101/11011/10001", ['N']="10001/11001/11001/10101/10011/10011/10001",
            ['O']="01110/10001/10001/10001/10001/10001/01110", ['G']="01111/10000/10000/10111/10001/10001/01111",
            ['M']="10001/11011/10101/10101/10001/10001/10001", ['L']="10000/10000/10000/10000/10000/10000/11111",
            ['Y']="10001/10001/01010/00100/00100/00100/00100"
        };
        static void Label(Texture2D image, string text, int x, int y)
        {
            for (int j = -1; j < 8; j++) for (int i = -1; i < text.Length * 6; i++) image.SetPixel(x + i, y + j, Color.black);
            foreach (char ch in text)
            {
                if (Glyph.TryGetValue(ch, out string glyph))
                {
                    var rows = glyph.Split('/');
                    for (int row = 0; row < 7; row++) for (int col = 0; col < 5; col++)
                        if (rows[row][col] == '1') image.SetPixel(x + col, y + 6 - row, Color.white);
                }
                x += 6;
            }
        }
    }
}
