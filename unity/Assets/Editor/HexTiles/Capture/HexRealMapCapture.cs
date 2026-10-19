#nullable enable
using System;
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
    /// <summary>The hex board on the game's real map (Map.unity, built as the game builds it, never saved): the board's
    /// hexes are the game's own q/r, so its root sits at (0, sea level, map length) with no turn or scale. Under the
    /// board the real ground is eased down to the board's own hex-shaped ground (blending out over about one hex), the
    /// board's terrain is cut away everywhere but its land hexes so the real coast, sea and country show around it, and
    /// the real settlement markers inside the board are hidden (the tiles replace them). Editor proof of the in-game
    /// layer: shots country.png (the board inside Israel), area.png, and per tile &lt;id&gt;-real.png.
    /// Unity -batchmode ... -executeMethod Statecraft.HexTiles.Capture.HexRealMapCapture.Run [-board tel_aviv]</summary>
    public static class HexRealMapCapture
    {
        const string ScenePath = "Assets/Map.unity";
        
        [MenuItem("Statecraft/Hex Tiles/Capture Board On Real Map")]
        public static void Run()
        {
            try
            {
                var repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                var dir = Path.GetFullPath(Path.Combine(repo, HexBoardCapture.Arg("-captureDir") ?? "asset-staging/ui-local/hex-tiles/real-map"));
                var size = (HexBoardCapture.Arg("-captureSize") ?? "3840x2160").Split('x');
                Capture(HexBoardCapture.Arg("-board") ?? HexTilesLook.Board, dir, int.Parse(size[0]), int.Parse(size[1]));
            }
            catch (Exception e)
            {
                Debug.LogError("HEX REAL MAP FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void Capture(string boardId, string dir, int width, int height)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<MapScene>() ?? throw new InvalidOperationException("Map.unity has no MapScene");
            map.Build();
            var board = HexBoardCapture.LoadBoard(boardId);
            // The same placement the game uses behind ?hex-tiles (HexRealMap), here with the editor's ground textures.
            var (root, reports) = HexRealMap.Place(map, board, HexBoardCapture.Ground);
            var surface = new HexBoardSurface(board);
            var origin = root.transform.position;
            var centres = board.Hexes.Values.Select(h => HexGeometry.Centre(h.q, h.r, HexScale.HexM)).ToList();
            Directory.CreateDirectory(dir);
            var cam = Camera.main ?? throw new InvalidOperationException("no main camera");
            try
            {
                Vector3 World(Vector3 b) => origin + new Vector3(b.x, Mathf.Max(surface.Height(b), 0f), b.z);
                var middle = new Vector3(centres.Average(c => c.x), 0f, centres.Average(c => c.z));
                HexBoardCapture.Shoot(cam, World(middle), 230000f, Path.Combine(dir, "country.png"), width, height);
                HexBoardCapture.Shoot(cam, World(middle), 125000f, Path.Combine(dir, "area.png"), width, height);
                foreach (var h in board.Hexes.Values.Where(h => h.prototype is "tel_aviv_city" or "jerusalem_old_city" or "jaffa_old_city" or "kibbutz" || h.unit != "")
                    .GroupBy(h => h.unit != "" ? "unit-" + h.unit : h.prototype).Select(g => g.First()))
                    HexBoardCapture.Shoot(cam, World(HexGeometry.Centre(h.q, h.r, HexScale.HexM)), 20000f, Path.Combine(dir, $"{(h.unit != "" ? "unit-" + h.unit : h.prototype)}-real.png"), width, height);
                Debug.Log($"HEX REAL MAP board {boardId} at {origin}, {reports.Count} tiles and units -> {dir}");
            }
            finally { TiltShift.Reset(); Object.DestroyImmediate(root); }
        }
    }
}
