#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Capture
{
    /// <summary>Renders a hex board in a new, unsaved scene: no game, no UI, no real map (the ground is the board's own).
    /// Shots at -captureSize (default 3840x2160) with the game camera's pitch and FOV, the map's light and grade:
    /// &lt;dir&gt;/board.png (the whole board) and, per prototype tile, &lt;id&gt;-mid.png (about 3 hexes) and &lt;id&gt;-close.png
    /// (one hex); plus stats.txt with each tile's pieces, stand-ins and triangles against its budget.
    /// -prototype &lt;id&gt; builds and shoots only that tile (with the board's ground and mountains around it).
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity
    ///   -executeMethod Statecraft.HexTiles.Capture.HexBoardCapture.Run [-board tel_aviv] [-prototype &lt;id&gt; [-on &lt;tile&gt;]]
    ///   [-captureDir asset-staging/ui-local/hex-tiles] [-captureSize 3840x2160] [-boardOnly]
    /// A board too large for the standard board shot (the whole country) is framed whole instead. Each prototype and unit
    /// is shot once (its last hex in the board file, the one whose shots used to be kept); -boardOnly skips those shots.</summary>
    public static class HexBoardCapture
    {
        public const string BoardsFolder = "Assets/Resources/HexTiles/Boards";
        /// <summary>The ground textures, local and git-ignored (Poly Haven CC0, asset-staging/kits/PROVENANCE.md). Without
        /// them the board paints flat colours.</summary>
        const string GroundFolder = "Assets/Art/Settlements/Prototype/HexSandboxGround";
        /// <summary>A livelier grade than the shipped map's, for the hex-tile captures (the opt-in look's start values).</summary>
        const float Pitch = 50f, Fov = 35f, BoardDistanceM = 80000f, MidDistanceM = 26000f, CloseDistanceM = 12500f;

        [MenuItem("Statecraft/Hex Tiles/Capture Tel Aviv Board")]
        public static void Run()
        {
            try
            {
                var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var dir = Path.GetFullPath(Path.Combine(repo, Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles"));
                var size = (Arg("-captureSize") ?? "3840x2160").Split('x');
                Capture(Arg("-board") ?? "tel_aviv", Arg("-prototype"), dir, int.Parse(size[0]), int.Parse(size[1]));
            }
            catch (Exception e)
            {
                Debug.LogError("HEX TILES CAPTURE FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static HexBoard LoadBoard(string id) => HexBoard.Load(id);

        static void Capture(string boardId, string? only, string dir, int width, int height)
        {
            var clock = Stopwatch.StartNew();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var board = LoadBoard(boardId);
            // -on <tile>: try a prototype on another tile's hex (A/B variants) without editing the board file.
            var on = Arg("-on");
            if (only != null && on != null) foreach (var h in board.Hexes.Values) if (h.prototype == on) h.prototype = only;
            if (only != null && !board.Hexes.Values.Any(h => h.prototype == only)) throw new ArgumentException($"board {boardId} has no tile '{only}' (try -on <tile>)");
            var cam = Stage();
            var (root, reports) = HexBoardBuilder.Build(board, Ground, only: only);
            long buildMs = clock.ElapsedMilliseconds;
            Directory.CreateDirectory(dir);
            try
            {
                var surface = new HexBoardSurface(board);
                Vector3 OnGround(Vector3 p) => new Vector3(p.x, Mathf.Max(surface.Height(p), 0f), p.z);
                var centres = board.Hexes.Values.Select(h => HexGeometry.Centre(h.q, h.r, HexScale.HexM)).ToList();
                var middle = new Vector3(centres.Average(c => c.x), 0f, centres.Average(c => c.z));
                float boardDistance = BoardDistance(board, cam, width / (float)height);
                if (only == null) Shoot(cam, OnGround(middle), boardDistance, Path.Combine(dir, "board.png"), width, height);
                // A board with an environment ring: one wider shot that takes in the landscape round the play hexes.
                if (only == null && board.Hexes.Values.Any(h => h.env)) Shoot(cam, OnGround(middle), boardDistance * 1.6f, Path.Combine(dir, "board-wide.png"), width, height);
                bool tiles = !Environment.GetCommandLineArgs().Contains("-boardOnly");
                // One mid and one close shot per prototype, of its last hex (the shots are named by prototype).
                foreach (var t in board.Hexes.Values.Where(h => tiles && h.prototype != "" && (only == null || h.prototype == only)).GroupBy(h => h.prototype).Select(g => g.Last()))
                {
                    var c = OnGround(HexGeometry.Centre(t.q, t.r, HexScale.HexM));
                    Shoot(cam, c, MidDistanceM, Path.Combine(dir, $"{t.prototype}-mid.png"), width, height);
                    Shoot(cam, c, CloseDistanceM, Path.Combine(dir, $"{t.prototype}-close.png"), width, height);
                }
                // Units: one close shot each, at the unit's own zoom.
                foreach (var u in board.Hexes.Values.Where(h => tiles && h.unit != "" && (only == null || h.prototype == only)).GroupBy(h => h.unit).Select(g => g.Last()))
                    Shoot(cam, OnGround(HexGeometry.Centre(u.q, u.r, HexScale.HexM)), CloseDistanceM, Path.Combine(dir, $"unit-{u.unit}-close.png"), width, height);
                var lines = new List<string> { $"board {boardId}: {board.Hexes.Count} hexes, built in {buildMs} ms, {width}x{height}" };
                lines.AddRange(reports.Select(r => "  " + r));
                File.WriteAllLines(Path.Combine(dir, only == null ? "stats.txt" : $"{only}-stats.txt"), lines);
                Debug.Log("HEX TILES " + string.Join(" | ", lines) + " -> " + dir);
            }
            finally
            {
                TiltShift.Reset();
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>The map's light, a camera with the game rig's FOV and post-processing, and the map-look grade, in the
        /// open scene. Shared by every hex-tile capture.</summary>
        public static Camera Stage()
        {
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional;
            MapLighting.Apply(sun);
            var cam = new GameObject("Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.fieldOfView = Fov; cam.nearClipPlane = 50f; cam.farClipPlane = 1_000_000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.07f, 0.36f, 0.48f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = new GameObject("PostFX").AddComponent<Volume>(); volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            MapGrade.Apply(volume.profile);
            HexLook.Apply(volume.profile, sun);   // the hex-tile look's warm, saturated light, as in the game under ?hex-tiles
            return cam;
        }

        /// <summary>The board shot's distance: the standard one, unless the board would not fit at twice it (the whole
        /// country), when it is framed whole: its width across the view, its depth foreshortened by the pitch.</summary>
        static float BoardDistance(HexBoard board, Camera cam, float aspect)
        {
            var b = HexBoardBuilder.Bounds(board);
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), tanH = tanV * aspect;
            float fit = 1.05f * Mathf.Max(b.width * 0.5f / tanH, b.height * 0.5f * Mathf.Sin(Pitch * Mathf.Deg2Rad) / tanV);
            return fit > 2f * BoardDistanceM ? fit : BoardDistanceM;
        }

        public static void Shoot(Camera cam, Vector3 lookAt, float distance, string path, int width, int height)
        {
            cam.transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);
            cam.transform.position = lookAt - cam.transform.forward * distance;
            TiltShift.Apply(0f, distance);
            ShadowFit.Apply(cam);
            var rt = new RenderTexture(width, height, 24) { antiAliasing = 4 }; cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }

        /// <summary>A layer's Poly Haven albedo and normal from the local ground folder, or (null, null).</summary>
        public static (Texture2D?, Texture2D?) Ground(string texture)
        {
            Texture2D? Load(string file, bool normal)
            {
                var path = $"{GroundFolder}/{file}";
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return null;
                var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (ti.textureType != type) { ti.textureType = type; ti.SaveAndReimport(); }
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return (Load($"{texture}_diff_2k.jpg", false), Load($"{texture}_nor_gl_2k.jpg", true));
        }

        public static string? Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
