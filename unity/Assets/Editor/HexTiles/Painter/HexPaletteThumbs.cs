#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>The palette's thumbnails: each prototype on a one-hex board with a ring of plain ground round it, each
    /// unit on plain grass, shot with the captures' camera, light and grade (HexBoardCapture) at about 256 px into the
    /// git-ignored asset-staging/ui-local/hex-tiles/thumbs (&lt;id&gt;.png, unit-&lt;id&gt;.png); plus a contact sheet of them in
    /// palette order with a text listing beside it.
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity
    ///   -executeMethod Statecraft.HexTiles.Painter.HexPaletteThumbs.Run [-thumbSize 256] [-only &lt;id,...&gt;]
    ///   [-sheetDir asset-staging/ui-local/hex-tiles/round2/painter]</summary>
    public static class HexPaletteThumbs
    {
        /// <summary>Shot distances: a tile's hex fills the square; a unit's figures fill it.</summary>
        const float TileDistanceM = 15000f, UnitDistanceM = 8500f;
        /// <summary>A unit faces south-east in its thumbnail: three-quarters towards the camera, which looks north.</summary>
        const float UnitFacing = 150f;
        public const int Size = 256, SheetColumns = 8;

        public static void Run()
        {
            try
            {
                int size = int.Parse(HexBoardCapture.Arg("-thumbSize") ?? Size.ToString());
                var only = HexBoardCapture.Arg("-only")?.Split(',');
                var sheetDir = Path.Combine(HexPalette.RepoRoot, HexBoardCapture.Arg("-sheetDir") ?? "asset-staging/ui-local/hex-tiles/round2/painter");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var written = MakeAll(HexPalette.ThumbsDir, size, only);
                Sheet(HexPalette.ThumbsDir, Path.Combine(sheetDir, "palette.png"), Path.Combine(sheetDir, "palette.txt"), size);
                Debug.Log($"HEX PALETTE THUMBS {written.Count} -> {HexPalette.ThumbsDir}; sheet -> {sheetDir}");
            }
            catch (Exception e)
            {
                Debug.LogError("HEX PALETTE THUMBS FAILED " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>Every palette entry, tiles then units, in palette order.</summary>
        public static List<(HexPaletteGroup group, HexPaletteEntry entry)> Ordered() =>
            HexPalette.Tiles().Concat(HexPalette.Units()).SelectMany(g => g.Entries.Select(e => (g, e))).ToList();

        /// <summary>Makes the thumbnails of every entry (or of the ids in <paramref name="only"/>) into
        /// <paramref name="dir"/>, in the open scene, which should be empty: it adds the captures' light and grade.</summary>
        public static List<string> MakeAll(string dir, int size = Size, string[]? only = null)
        {
            Directory.CreateDirectory(dir);
            var cam = HexBoardCapture.Stage();
            var written = new List<string>();
            try
            {
                foreach (var (_, e) in Ordered().Where(p => only == null || only.Contains(p.entry.Id)))
                {
                    var path = HexPalette.ThumbPath(dir, e);
                    Make(cam, e, path, size);
                    written.Add(path);
                }
            }
            finally { TiltShift.Reset(); }
            return written;
        }

        /// <summary>One thumbnail: builds the entry's little board, shoots it, and destroys it.</summary>
        public static void Make(Camera cam, HexPaletteEntry e, string path, int size)
        {
            var board = ThumbBoard(e);
            var root = HexBoardBuilder.Build(board, HexBoardCapture.Ground).root;
            try
            {
                var surface = new HexBoardSurface(board);
                var c = HexGeometry.Centre(0, 0, HexScale.HexM);
                c.y = Mathf.Max(surface.Height(c), 0f);
                HexBoardCapture.Shoot(cam, c, e.IsUnit ? UnitDistanceM : TileDistanceM, path, size, size);
            }
            finally
            {
                root.GetComponent<HexBoardAssets>()?.Teardown();
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Hex (0, 0) with the entry (a prototype on its own terrain and ground; a unit on flat grass) and a ring of
        /// six plain hexes of the same ground round it.</summary>
        public static HexBoard ThumbBoard(HexPaletteEntry e)
        {
            string terrain = "flat", ground = "grass";
            if (!e.IsUnit)
            {
                var proto = HexPrototypes.Create(e.Id);
                terrain = proto.Terrain.ToString().ToLowerInvariant(); ground = proto.Ground;
            }
            string ring = terrain == "mountain" ? "hills" : terrain == "water" ? "flat" : terrain;
            var hexes = new List<BoardHex>
            {
                new BoardHex { q = 0, r = 0, terrain = terrain, ground = ground, prototype = e.IsUnit ? "" : e.Id, unit = e.IsUnit ? e.Id : "", facing = UnitFacing },
            };
            hexes.AddRange(HexGeometry.Neighbours.Select(n => new BoardHex { q = n.q, r = n.r, terrain = ring, ground = ground }));
            return new HexBoard(new HexBoardFile { id = "thumb-" + e.Id, hexes = hexes.ToArray() });
        }

        /// <summary>The contact sheet: the thumbnails in palette order, SheetColumns to a row, each group starting a new
        /// row (a missing thumbnail is a grey cell), and a text listing of each cell's row, column, group, id and title.</summary>
        public static void Sheet(string thumbsDir, string pngPath, string txtPath, int cell = Size)
        {
            var cells = new List<(int row, int col, HexPaletteGroup group, HexPaletteEntry entry)>();
            int row = 0;
            foreach (var g in HexPalette.Tiles().Concat(HexPalette.Units()))
            {
                for (int i = 0; i < g.Entries.Count; i++) cells.Add((row + i / SheetColumns, i % SheetColumns, g, g.Entries[i]));
                row += (g.Entries.Count + SheetColumns - 1) / SheetColumns;
            }
            var sheet = new Texture2D(SheetColumns * cell, Math.Max(row, 1) * cell, TextureFormat.RGB24, false);
            var fill = Enumerable.Repeat(new Color32(40, 40, 40, 255), sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(fill);
            var lines = new List<string> { $"Hex Painter palette: {cells.Count} entries, {SheetColumns} to a row, each group on new rows; cells {cell} px; row 1 is the top." };
            string lastGroup = "";
            foreach (var (r, c, g, e) in cells)
            {
                if (g.Name != lastGroup) { lines.Add(""); lines.Add($"[{g.Name}]"); lastGroup = g.Name; }
                var thumb = HexPalette.LoadThumb(thumbsDir, e);
                if (thumb != null)
                {
                    var px = Scaled(thumb, cell);
                    sheet.SetPixels(c * cell, sheet.height - (r + 1) * cell, cell, cell, px);
                    Object.DestroyImmediate(thumb);
                }
                lines.Add($"  row {r + 1}, col {c + 1}: {e.Id}  {e.Title}{(thumb == null ? "  (no thumbnail)" : "")}");
            }
            sheet.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(pngPath)!);
            File.WriteAllBytes(pngPath, sheet.EncodeToPNG());
            File.WriteAllLines(txtPath, lines);
            Object.DestroyImmediate(sheet);
        }

        /// <summary>A thumbnail's pixels at <paramref name="cell"/> square (nearest sample; thumbnails are made at that size).</summary>
        static Color[] Scaled(Texture2D t, int cell)
        {
            if (t.width == cell && t.height == cell) return t.GetPixels();
            var src = t.GetPixels(); var dst = new Color[cell * cell];
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                    dst[y * cell + x] = src[(y * t.height / cell) * t.width + x * t.width / cell];
            return dst;
        }
    }
}
