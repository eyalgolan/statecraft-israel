#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Capture
{
    /// <summary>Renders a unit marching across hexes on a board, as a frame sequence and an MP4 (ffmpeg, when on PATH):
    /// the Palmach squad (eight 13 October Tripo soldiers with walk and idle clips) marches east into the next hex, turns
    /// at its centre and goes on south-east, while the sandwich armoured truck drives north up the plain, wheels turning.
    /// Units are a layer above the tiles, so the land under them never changes. The camera frames both.
    /// Unity -batchmode ... -executeMethod Statecraft.HexTiles.Capture.HexUnitMoveCapture.Run [-captureDir ...] [-captureSize 1920x1080]</summary>
    public static class HexUnitMoveCapture
    {
        const int Fps = 24, HoldFrames = 14, MoveFrames = 132;
        const float FollowDistanceM = 24000f;
        /// <summary>The squad marches east then south-east; the armoured truck drives north up the coast at the same time.</summary>
        static readonly (string unit, (int q, int r)[] route)[] Moves =
        {
            ("palmach_squad", new[] { (-2, 18), (-1, 18), (-1, 19) }),
            ("sandwich_truck", new[] { (0, 19), (0, 18), (0, 17) }),
        };

        [MenuItem("Statecraft/Hex Tiles/Capture Unit Move")]
        public static void Run()
        {
            try
            {
                var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var dir = Path.GetFullPath(Path.Combine(repo, HexBoardCapture.Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles/unit-move"));
                var size = (HexBoardCapture.Arg("-captureSize") ?? "1920x1080").Split('x');
                Capture(dir, int.Parse(size[0]), int.Parse(size[1]));
            }
            catch (Exception e)
            {
                Debug.LogError("HEX UNIT MOVE FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void Capture(string dir, int width, int height)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var board = HexBoardCapture.LoadBoard("tel_aviv");
            foreach (var h in board.Hexes.Values) h.unit = "";   // the moving units replace the board's standing ones
            var cam = HexBoardCapture.Stage();
            var (root, _) = HexBoardBuilder.Build(board, HexBoardCapture.Ground);
            var units = new GameObject("Units").transform;
            var surface = new HexBoardSurface(board);
            var motions = Moves.Select(m => new HexUnitMotion(HexUnitTypes.Create(m.unit).Create(), m.route, surface, units)).ToList();
            if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir, "frame-*.png")) File.Delete(f);
            Directory.CreateDirectory(dir);
            var clock = Stopwatch.StartNew();
            int frame = 0;
            try
            {
                // Every unit covers its own path over the same frames; the camera frames them all.
                void Shot(float progress, bool moving)
                {
                    var at = Vector3.zero;
                    foreach (var m in motions) at += m.Pose(progress * m.Length, moving);
                    HexBoardCapture.Shoot(cam, at / motions.Count, FollowDistanceM, Path.Combine(dir, $"frame-{frame++:0000}.png"), width, height);
                }
                for (int i = 0; i < HoldFrames; i++) Shot(0f, false);
                for (int i = 0; i <= MoveFrames; i++)
                {
                    float u = i / (float)MoveFrames;
                    Shot(u * u * (3f - 2f * u), i > 0 && i < MoveFrames);   // start and stop gently
                }
                for (int i = 0; i < HoldFrames; i++) Shot(1f, false);
            }
            finally { Object.DestroyImmediate(units.gameObject); Object.DestroyImmediate(root); TiltShift.Reset(); }
            var mp4 = Path.Combine(dir, "unit-move.mp4");
            var ok = Encode(dir, mp4);
            Debug.Log($"HEX UNIT MOVE {frame} frames in {clock.ElapsedMilliseconds} ms, paths {string.Join(", ", motions.Select(m => m.Length.ToString("0")))} m -> {(ok ? mp4 : dir)}");
        }

        static bool Encode(string dir, string mp4)
        {
            foreach (var ffmpeg in new[] { "ffmpeg", "/opt/homebrew/bin/ffmpeg", "/usr/local/bin/ffmpeg", Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", "miniconda3/bin/ffmpeg") })
            {
                try
                {
                    var p = Process.Start(new ProcessStartInfo(ffmpeg, $"-y -framerate {Fps} -i \"{dir}/frame-%04d.png\" -c:v libx264 -pix_fmt yuv420p -crf 18 \"{mp4}\"")
                    { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true });
                    if (p == null) continue;
                    p.StandardError.ReadToEnd(); p.WaitForExit();
                    if (p.ExitCode == 0) return true;
                }
                catch (System.ComponentModel.Win32Exception) { }
            }
            return false;
        }
    }
}
