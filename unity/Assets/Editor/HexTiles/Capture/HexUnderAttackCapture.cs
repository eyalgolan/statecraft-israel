#nullable enable
using System;
using System.Collections.Generic;
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
    /// <summary>Before and after shots of the "under attack" look (HexUnderAttack) on a board, in a new, unsaved scene
    /// staged as HexBoardCapture stages it: a city hex and a unit hex, each at mid and close distance, without the look
    /// and with it at two pulse phases (the border low and high, the smoke a half-period apart). Writes
    /// &lt;target&gt;-before-{mid,close}.png, &lt;target&gt;-after-{low,high}-{mid,close}.png and stats.txt (the look's
    /// triangles, vertices and draws).
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity
    ///   -executeMethod Statecraft.HexTiles.Capture.HexUnderAttackCapture.Run [-board israel] [-city tel_aviv_city]
    ///   [-unit palmach_squad] [-captureDir asset-staging/ui-local/hex-tiles/round2/attack] [-captureSize 3840x2160]</summary>
    public static class HexUnderAttackCapture
    {
        /// <summary>HexBoardCapture's tile shot distances: about three hexes, and one.</summary>
        const float MidDistanceM = 26000f, CloseDistanceM = 12500f;

        [MenuItem("Statecraft/Hex Tiles/Capture Under Attack")]
        public static void Run()
        {
            GameObject? root = null;
            try
            {
                var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var dir = Path.GetFullPath(Path.Combine(repo, HexBoardCapture.Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles/round2/attack"));
                var size = (HexBoardCapture.Arg("-captureSize") ?? "3840x2160").Split('x');
                int width = int.Parse(size[0]), height = int.Parse(size[1]);
                var boardId = HexBoardCapture.Arg("-board") ?? "israel";
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var board = HexBoardCapture.LoadBoard(boardId);
                var cam = HexBoardCapture.Stage();
                (root, _) = HexBoardBuilder.Build(board, HexBoardCapture.Ground);
                var surface = new HexBoardSurface(board);
                Directory.CreateDirectory(dir);
                var city = HexBoardCapture.Arg("-city") ?? "tel_aviv_city";
                var unit = HexBoardCapture.Arg("-unit") ?? "palmach_squad";
                // The first hex (by q, r) with that tile or unit: on israel, Tel Aviv (-3,19) and the Palmach squad's hex.
                var hexes = board.Hexes.Values.OrderBy(h => h.q).ThenBy(h => h.r).ToList();
                var targets = new[]
                {
                    ("city", hexes.FirstOrDefault(h => h.prototype == city) ?? throw new ArgumentException($"board {boardId} has no tile '{city}'")),
                    ("unit", hexes.FirstOrDefault(h => h.unit == unit) ?? throw new ArgumentException($"board {boardId} has no unit '{unit}'")),
                };
                var lines = new List<string> { $"board {boardId}, {width}x{height}" };
                foreach (var (name, h) in targets)
                {
                    var c = HexGeometry.Centre(h.q, h.r, HexScale.HexM);
                    c.y = Mathf.Max(surface.Height(c), 0f);
                    void Shoot(string shot)
                    {
                        HexBoardCapture.Shoot(cam, c, MidDistanceM, Path.Combine(dir, $"{name}-{shot}-mid.png"), width, height);
                        HexBoardCapture.Shoot(cam, c, CloseDistanceM, Path.Combine(dir, $"{name}-{shot}-close.png"), width, height);
                    }
                    Shoot("before");
                    var go = HexUnderAttack.Show(root.transform, surface, h.q, h.r);
                    var look = go.GetComponent<HexUnderAttackLook>(); look.Hold = true;
                    // Low and high border, half a pulse apart (the smoke has risen a little between them).
                    const float t0 = 2f * HexUnderAttack.PulsePeriodS;   // Pulse(t0) = 0
                    look.Time(t0); Shoot("after-low");
                    look.Time(t0 + HexUnderAttack.PulsePeriodS * 0.5f); Shoot("after-high");
                    var meshes = go.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToList();
                    lines.Add($"{name}: {h.prototype} {(h.unit == "" ? "" : "+ " + h.unit + " ")}({h.q},{h.r}) | look: "
                        + $"{meshes.Sum(m => m.triangles.Length / 3)} tris, {meshes.Sum(m => m.vertexCount)} verts, "
                        + $"{go.GetComponentsInChildren<Renderer>().Length} draws ("
                        + string.Join(", ", meshes.Select(m => $"{m.name} {m.triangles.Length / 3}")) + ")");
                    HexUnderAttack.Hide(root.transform, h.q, h.r);
                }
                File.WriteAllLines(Path.Combine(dir, "stats.txt"), lines);
                Debug.Log("HEX UNDER ATTACK " + string.Join(" | ", lines) + " -> " + dir);
            }
            catch (Exception e)
            {
                Debug.LogError("HEX UNDER ATTACK CAPTURE FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
            finally
            {
                TiltShift.Reset();
                if (root != null) Object.DestroyImmediate(root);
            }
        }
    }
}
