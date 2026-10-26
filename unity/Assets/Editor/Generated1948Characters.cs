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
            return (index == 1 || index == 3 ? "Assets/Art/Collection/" : "Assets/Editor/Generated1948Review/") + id;
        }
        static string Model(string id, string level) => $"{Dir(id)}/{id}.fbx";
        static AnimationClip Clip(string id, string level) => AssetDatabase.LoadAllAssetsAtPath(Model(id, level))
            .OfType<AnimationClip>().Single(c => c.name == (level == "walk" ? "walk" : "idle"));

        [MenuItem("Statecraft/Generated 1948/Import seven characters")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            foreach (string id in Ids) Import(id);
            RebindReview();
            AssetDatabase.SaveAssets();
        }
        public static void ImportRuntime()
        {
            foreach (string id in Ids.Where(Statecraft.Game.Map.Collection.CollectionCatalog.IsFullQualityGeneratedInfantry)) Import(id);
        }
        static void Import(string id)
        {
            string path = Model(id, "original");
            if (!File.Exists(path)) throw new FileNotFoundException("Missing full-quality compatibility export", path);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter ?? throw new FileNotFoundException("No model importer", path);
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
            imp.importTangents = ModelImporterTangents.Import;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.optimizeGameObjects = false;
            imp.importAnimation = true;
            imp.resampleCurves = false;
            imp.animationCompression = ModelImporterAnimationCompression.Off;
            imp.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
            imp.SaveAndReimport();
            var takes = imp.defaultClipAnimations;
            foreach (var clip in takes)
            {
                if (clip.name.EndsWith("|idle", StringComparison.Ordinal)) clip.name = "idle";
                if (clip.name.EndsWith("|walk", StringComparison.Ordinal)) clip.name = "walk";
                clip.loopTime = clip.name == "idle" || clip.name == "walk";
            }
            if (!takes.Any(c => c.name == "idle") || !takes.Any(c => c.name == "walk"))
                throw new InvalidOperationException(path + ": local idle/walk takes missing; no source clips may be discarded");
            imp.clipAnimations = takes;
            imp.SaveAndReimport();
            ImportTexture($"{Dir(id)}/{id}_basecolor.jpg");
            MakePrefab(id, "original", Material(id), Controller(id));
        }
        static void ImportTexture(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new FileNotFoundException("Missing original map", path);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.isReadable = false;
            imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.crunchedCompression = false;
            foreach (string target in new[] { "Standalone", "WebGL" }) imp.ClearPlatformTextureSettings(target);
            imp.SaveAndReimport();
        }
        static void RebindReview()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(Generated1948CharacterReviewMenu.ScenePath);
                var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Statecraft.Game.Review.Generated1948CharacterReview>()).Single();
                foreach (var entry in review.Characters)
                {
                    entry.Model = AssetDatabase.LoadAssetAtPath<GameObject>(Model(entry.Id, "original"));
                    entry.Material = Material(entry.Id);
                    entry.Idle = Clip(entry.Id, "near");
                    entry.Walk = Clip(entry.Id, "walk");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (previous.Any(s => s.isLoaded) && previous.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous); }
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
            string path = $"{Dir(id)}/{id}.controller";
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
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind();
                    animator.Update(0f);
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;
                }
                else if (animator != null) Object.DestroyImmediate(animator);
                PrefabUtility.SaveAsPrefabAsset(go, $"{Dir(id)}/{id}.prefab");
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

        /// <summary>Matched editor renders of the real legacy roster/map using the game's camera. No simulation changes.</summary>
        public static void CaptureCampaign()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            string output = Path.GetFullPath("../captures/generated-1948-characters/matched-campaign");
            Directory.CreateDirectory(output);
            var pending = new List<(string id, Action<GameObject> done)>();
            Statecraft.Game.Map.Units.UnitFigures.Forget();
            Statecraft.Game.Map.Units.UnitFigures.Source = (id, detail, done) => pending.Add((id, done));
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var map = new GameObject("MapScene").AddComponent<Statecraft.Game.Map.MapScene>();
                UnityEngine.Material Load(string name) => AssetDatabase.LoadAssetAtPath<UnityEngine.Material>($"Assets/Materials/{name}.mat");
                map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
                Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
                    File.ReadAllText(Path.Combine(Statecraft.Game.Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
                map.Build();
                var events = Directory.GetFiles(Path.Combine(Statecraft.Game.Paths.Content, "events"), "*.yaml")
                    .Select(f => "events/" + Path.GetFileName(f)).ToList();
                var content = Statecraft.Sim.ContentLoader.Load(rel => {
                    string path = Path.Combine(Statecraft.Game.Paths.Content, rel);
                    if (!File.Exists(path)) return null;
                    string text = File.ReadAllText(path);
                    return rel == "eras.yaml" ? text.Replace("    systems: { loop: gdd }\n", "") : text;
                }, events);
                var state = Statecraft.Sim.Simulation.NewGame(content, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical).Clone();
                state.Date = new DateTime(1948, 6, 20); state.Rung = 5; state.Mobilization = 0.8;
                var layer = Statecraft.Game.Map.Units.UnitLayer.Create(map, map.UnitEntries, content.Hexes, true);
                layer.Show(content, state);
                foreach (var request in pending.ToArray().Where(r => !Statecraft.Game.Map.Collection.CollectionCatalog.IsFullQualityGeneratedInfantry(r.id)))
                    request.done(AssetDatabase.LoadAssetAtPath<GameObject>(Statecraft.Game.Map.Collection.CollectionCatalog.PrefabPath(request.id,
                        Statecraft.Game.Map.Collection.CollectionDetail.Far)));
                var camera = Object.FindFirstObjectByType<Statecraft.Game.Map.CameraRig>().GetComponent<Camera>();
                var rig = camera.GetComponent<Statecraft.Game.Map.CameraRig>();
                camera.aspect = 16f / 9f;
                var records = new List<string>();
                foreach (string side in new[] { "syria", "iraq" })
                {
                    var row = map.UnitEntries.First(u => u.Side == side && u.Anchors.Count > 0);
                    var spot = layer.Spots.First(p => row.Anchors.Contains(p.Key) && state.Date >= Statecraft.Game.Map.Units.UnitData.Day(p.Key.From) && state.Date < Statecraft.Game.Map.Units.UnitData.Day(p.Key.To)).Value;
                    rig.LookAt = Statecraft.Game.Map.Units.UnitShowcase.Ground(map, new Vector3((float)spot.X, 0, (float)spot.Z));
                    rig.Distance = 24000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply(); layer.FaceCamera(camera);
                    string before = Path.Combine(output, side + "-before.jpg");
                    RenderMap(camera, before);
                    string id = Statecraft.Game.Map.Units.UnitFigures.SideFigure(side);
                    foreach (var request in pending.ToArray().Where(r => r.id == id))
                        request.done(AssetDatabase.LoadAssetAtPath<GameObject>(Statecraft.Game.Map.Collection.CollectionCatalog.PrefabPath(id,
                            Statecraft.Game.Map.Collection.CollectionDetail.Far)));
                    foreach (var animator in layer.GetComponentsInChildren<Animator>(true)) { animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Update(0f); }
                    layer.FaceCamera(camera);
                    RenderMap(camera, Path.Combine(output, side + "-after.jpg"));
                    records.Add($"{side}: row={row.Id}; lookAt={rig.LookAt}; distance={rig.Distance}; pitch={Statecraft.Game.Map.CameraRig.Pitch}; fov={camera.fieldOfView}");
                }
                File.WriteAllText(Path.Combine(output, "README.txt"), "Unity editor renders, real map and dated legacy roster fixture (20 June 1948, rung 5, mobilization 0.8).\n" +
                    "Before delays only that new side's model arrival: main draws no token until its own model exists. Same camera/state/lighting for each pair.\n" +
                    "No UI or location text in captures. Not a WebGL/player or GDD campaign run; no simulated state or placements modified.\n" + string.Join("\n", records));
            }
            finally
            {
                Statecraft.Game.Map.Units.UnitFigures.Source = null;
                Statecraft.Game.Map.Units.UnitFigures.Forget();
                if (previous.Any(s => s.isLoaded) && previous.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
        static void RenderMap(Camera camera, string path)
        {
            var old = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24); target.Create();
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToJPG(92)); }
            finally { camera.targetTexture = old; RenderTexture.active = active; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
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
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir(id)}/{id}.prefab")
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
