#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Capture
{
    /// <summary>One cleared source house on the existing tel_aviv board's moshava hex (-3,21).
    /// Generates only transient objects: no asset, prefab, board or loader is overwritten.</summary>
    public static class BuildingLodCapture
    {
        public const string Asset = "b05a-moshava-house-v2";
        public const string SourceFolder = "Assets/Art/Settlements/Prototype/Models/" + Asset + "/";
        const int Width = 1920, Height = 1080, BakeScale = 2;
        public const float GameplayDistance = 12500f, CloseDistance = 3000f;

        [MenuItem("Statecraft/Hex Tiles/Experiments/Preview One Building LOD")]
        public static void Preview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Create();
        }

        public static (GameObject root, BuildingLodProbe probe, Camera camera, Vector3 target) Create()
        {
            // A manual Camera.Render otherwise includes other loaded scenes' renderers/lights/volumes.
            if (Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length != 0
                || Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length != 0
                || Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Open an empty scene for the isolated one-building fixture.");
            var root = new GameObject("One building LOD — prototype only");
            var owner = root.AddComponent<HexBoardAssets>();
            owner.OnTeardown(MapLighting.Capture(null));
            try
            {
                var board = HexBoard.Load("tel_aviv");
                var hex = board.Hexes[(-3, 21)];
                if (hex.prototype != "moshava_village") throw new InvalidOperationException("The fixture hex changed.");
                var surface = new HexBoardSurface(board);
                var ctx = new HexTileContext(hex.q, hex.r, surface, 1948);
                HexPrototypes.Create(hex.prototype).Build(ctx);
                var placement = ctx.Placements.First(p => p.Model == "sandbox:kit_moshava");
                // Real board ground, one real prototype placement. Other models are deliberately absent.
                var (ground, _) = HexBoardBuilder.Build(board, null, only: "__ground_only__");
                ground.transform.SetParent(root.transform, false);
                var oldRoots = root.scene.GetRootGameObjects();
                var camera = HexBoardCapture.Stage();
                foreach (var go in root.scene.GetRootGameObjects().Except(oldRoots)) go.transform.SetParent(root.transform, true);
                var volume = root.GetComponentInChildren<Volume>();
                if (volume != null)
                {
                    owner.Own(volume.profile);
                    foreach (var component in volume.profile.components) owner.Own(component);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFolder + Asset + ".fbx")
                    ?? throw new FileNotFoundException(Asset);
                var original = Object.Instantiate(prefab, root.transform);
                var bounds = HexModels.Bounds(original);
                var normalise = Matrix4x4.Scale(HexModels.Normaliser(bounds.size, placement.Fit))
                    * Matrix4x4.Translate(new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z));
                var matrix = placement.Matrix * normalise;
                original.transform.SetPositionAndRotation(matrix.GetColumn(3), matrix.rotation);
                original.transform.localScale = matrix.lossyScale;
                var renderer = original.GetComponentInChildren<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceFolder + Asset + ".mat");
                renderer.shadowCastingMode = ShadowCastingMode.On;
                var target = renderer.bounds.center;
                SetCamera(camera, target, GameplayDistance);
                var card = Bake(camera, renderer, owner);
                card.transform.SetParent(root.transform, true);
                card.enabled = false;
                var probe = root.AddComponent<BuildingLodProbe>();
                probe.Configure(renderer, card, camera, 10000f, root.GetComponentInChildren<Light>());
                probe.ExperimentEnabled = true;
                probe.Refresh();
                return (root, probe, camera, target);
            }
            catch { Object.DestroyImmediate(root); throw; }
        }

        static void SetCamera(Camera camera, Vector3 target, float distance)
        {
            camera.aspect = (float)Width / Height;
            camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            camera.transform.position = target - camera.transform.forward * distance;
        }

        static MeshRenderer Bake(Camera camera, MeshRenderer original, HexBoardAssets owner)
        {
            const int layer = 30;
            var bounds = original.bounds;
            int w = Width * BakeScale, h = Height * BakeScale;
            var corners = new List<Vector3>();
            for (int i = 0; i < 8; i++) corners.Add(camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            int x = Mathf.FloorToInt(corners.Min(p => p.x) * w) - 4, y = Mathf.FloorToInt(corners.Min(p => p.y) * h) - 4;
            int cw = Mathf.CeilToInt(corners.Max(p => p.x) * w) + 4 - x;
            int ch = Mathf.CeilToInt(corners.Max(p => p.y) * h) + 4 - y;
            if (x < 0 || y < 0 || x + cw > w || y + ch > h) throw new InvalidOperationException("Building is outside bake camera.");
            var bake = new GameObject("Temporary building bake camera").AddComponent<Camera>();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = RenderTexture.active;
            int oldLayer = original.gameObject.layer;
            var pipeline = UniversalRenderPipeline.asset;
            float oldShadowDistance = pipeline != null ? pipeline.shadowDistance : 0f;
            Texture2D? image = null;
            try
            {
                bake.CopyFrom(camera);
                bake.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
                bake.cullingMask = 1 << layer;
                bake.clearFlags = CameraClearFlags.SolidColor;
                bake.backgroundColor = Color.clear;
                bake.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                original.gameObject.layer = layer;
                bake.targetTexture = rt;
                if (pipeline != null) pipeline.shadowDistance = ShadowFit.Distance(bake, pipeline.cascadeBorder);
                bake.Render();
                RenderTexture.active = rt;
                image = owner.Own(new Texture2D(cw, ch, TextureFormat.RGBA32, false) { name = "b05a colour impostor (local prototype)" });
                image.ReadPixels(new Rect(x, y, cw, ch), 0, 0);
                // Transparent MSAA resolves against black; keep straight-alpha RGB for the transparent card.
                var pixels = image.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i].a > 0f && pixels[i].a < 1f)
                        pixels[i] = new Color(pixels[i].r / pixels[i].a, pixels[i].g / pixels[i].a, pixels[i].b / pixels[i].a, pixels[i].a);
                image.SetPixels(pixels); image.Apply(); image.wrapMode = TextureWrapMode.Clamp;
            }
            finally
            {
                original.gameObject.layer = oldLayer;
                if (pipeline != null) pipeline.shadowDistance = oldShadowDistance;
                RenderTexture.active = previous;
                bake.targetTexture = null;
                rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(bake.gameObject);
            }
            var go = new GameObject("b05a colour card (original shadow retained)", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(owner.transform, true);
            float z = Vector3.Dot(bounds.center - camera.transform.position, camera.transform.forward);
            Vector3 At(float px, float py) => camera.ViewportToWorldPoint(new Vector3(px / w, py / h, z));
            var mesh = owner.Own(new Mesh { name = "b05a single-view impostor" });
            mesh.vertices = new[] { At(x, y), At(x, y + ch), At(x + cw, y + ch), At(x + cw, y) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var material = owner.Own(HexMaterials.New(HexMaterials.Edges));
            material.SetTexture("_BaseMap", image); material.SetColor("_BaseColor", Color.white);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return renderer;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the Preview menu in an interactive editor.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject? ownedRoot = null;
            try
            {
                var (root, probe, camera, target) = Create();
                ownedRoot = root;
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../captures/building-lod"));
                Directory.CreateDirectory(dir);
                var stats = new List<string> { "DRAFT one-building colour-pass impostor; board tel_aviv, hex (-3,21), seed 1948.",
                    "Camera: 1920x1080, pitch 50, yaw 0, FOV 35. Same transforms/light/ground in each pair.",
                    "Ground: real HexBoardSurface and HexBoardBuilder; missing local textures use shipped flat colours.",
                    "No actual frame-time or per-camera draw-call measurement: manual editor Camera.Render is not a player benchmark." };
                foreach (var shot in new[] { ("gameplay", GameplayDistance), ("close", CloseDistance) })
                {
                    SetCamera(camera, target, shot.Item2);
                    foreach (bool enabled in new[] { false, true })
                    {
                        probe.ExperimentEnabled = enabled; probe.Refresh();
                        string name = shot.Item1 + (enabled ? "-after" : "-before");
                        Shoot(camera, Path.Combine(dir, name + ".png"));
                        stats.Add($"{name}: impostor={probe.UsingImpostor}, distance={shot.Item2}, editor_drawCalls_counter={UnityStats.drawCalls} (whole scene; attribution unverified)");
                    }
                }
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null || !renderer.name.Contains("tripo") && !renderer.name.Contains("b05a")) continue;
                    stats.Add($"{renderer.name}: triangles={HexModels.IndexTriangles(mesh)}, vertices={mesh.vertexCount}, submeshes={mesh.subMeshCount}, materialSlots={renderer.sharedMaterials.Length}");
                    if (renderer.name.StartsWith("b05a colour card", StringComparison.Ordinal) && renderer.sharedMaterial.mainTexture != null)
                        stats.Add($"impostor texture: {renderer.sharedMaterial.mainTexture.width}x{renderer.sharedMaterial.mainTexture.height} RGBA32, no mipmaps; additional resident texture, not a source replacement");
                }
                stats.Add("Building submissions expected: original=1 colour + original shadow pass(es); impostor=1 transparent colour + same original shadow pass(es). Total draw-call benefit is unmeasured.");
                stats.Add("Colour triangles: 6000 -> 2. Original 6000-triangle shadow mesh remains; no whole-frame triangle or frame-time saving claimed.");
                File.WriteAllLines(Path.Combine(dir, "render-report.txt"), stats);
                Debug.Log("BUILDING LOD PROBE\n" + string.Join("\n", stats));
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
            finally
            {
                if (ownedRoot != null) Object.DestroyImmediate(ownedRoot);
            }
        }

        static void Shoot(Camera camera, string path)
        {
            var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
            var pipeline = UniversalRenderPipeline.asset;
            float shadowDistance = pipeline != null ? pipeline.shadowDistance : 0f;
            var rt = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                if (pipeline != null) pipeline.shadowDistance = ShadowFit.Distance(camera, pipeline.cascadeBorder);
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                if (pipeline != null) pipeline.shadowDistance = shadowDistance;
                camera.targetTexture = previousTarget; RenderTexture.active = previous;
                rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
        }
    }
}
