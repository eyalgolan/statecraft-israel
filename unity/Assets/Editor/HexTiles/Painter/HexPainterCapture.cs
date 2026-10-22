#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Statecraft.Game.Map;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>The Hex Painter's path without its window, for checking it from the command line: loads a board as the
    /// painter does, shoots one hex, makes one edit through HexPainter.Change (written to -overrides, by default a file
    /// in the capture folder, so the board's own overrides are untouched), and shoots the hex again.
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity
    ///   -executeMethod Statecraft.HexTiles.Painter.HexPainterCapture.Run -board israel -hex -3,19 -field prototype
    ///   -value kibbutz [-overrides &lt;path&gt;] [-captureDir asset-staging/ui-local/hex-tiles/painter] [-captureSize 3840x2160]
    /// Writes &lt;dir&gt;/before.png, after.png and painter.txt (the overrides written, the rebuild time, the tile report).</summary>
    public static class HexPainterCapture
    {
        public static void Run()
        {
            try
            {
                var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var dir = Path.GetFullPath(Path.Combine(repo, HexBoardCapture.Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles/painter"));
                var size = (HexBoardCapture.Arg("-captureSize") ?? "3840x2160").Split('x');
                var hex = (HexBoardCapture.Arg("-hex") ?? throw new ArgumentException("-hex q,r")).Split(',');
                Directory.CreateDirectory(dir);
                var overrides = HexBoardCapture.Arg("-overrides") ?? Path.Combine(dir, "painter.overrides.json");
                if (HexBoardCapture.Arg("-overrides") == null && File.Exists(overrides)) File.Delete(overrides);
                Shoot(HexBoardCapture.Arg("-board") ?? "israel", int.Parse(hex[0]), int.Parse(hex[1]), HexBoardCapture.Arg("-field") ?? "prototype",
                    HexBoardCapture.Arg("-value") ?? "", overrides, dir, int.Parse(size[0]), int.Parse(size[1]));
            }
            catch (Exception e)
            {
                Debug.LogError("HEX PAINTER CAPTURE FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void Shoot(string boardId, int q, int r, string field, string value, string overrides, string dir, int width, int height)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = HexBoardCapture.Stage();
            var clock = Stopwatch.StartNew();
            var root = HexPainter.Load(boardId);
            long loadMs = clock.ElapsedMilliseconds;
            try
            {
                var state = root.GetComponent<HexBoardState>();
                var c = HexGeometry.Centre(q, r, HexScale.HexM);
                c.y = Mathf.Max(state.Surface.Height(c), 0f);
                var h = state.Board.Hexes[(q, r)];
                string before = HexBoardOverrides.Get(h, field);
                HexBoardCapture.Shoot(cam, c, 26000f, Path.Combine(dir, "before.png"), width, height);
                clock.Restart();
                var report = HexPainter.Change(root, overrides, q, r, field, value);
                long rebuildMs = clock.ElapsedMilliseconds;
                HexBoardCapture.Shoot(cam, c, 26000f, Path.Combine(dir, "after.png"), width, height);
                var lines = new[]
                {
                    $"board {boardId}: loaded in {loadMs} ms; ({q},{r}) {field} '{before}' -> '{value}', rebuilt in {rebuildMs} ms",
                    "tile: " + (report?.ToString() ?? "none"),
                    "overrides written to " + overrides + ":", File.ReadAllText(overrides),
                };
                File.WriteAllLines(Path.Combine(dir, "painter.txt"), lines);
                Debug.Log("HEX PAINTER " + string.Join(" | ", lines) + " -> " + dir);
            }
            finally
            {
                TiltShift.Reset();
                Object.DestroyImmediate(root);
            }
        }
    }
}
