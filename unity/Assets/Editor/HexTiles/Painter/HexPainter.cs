#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>The Hex Painter's write-back, apart from its window so tests can drive it: one edit sets a field of one
    /// hex on the board in memory and records it in the board's sibling &lt;id&gt;.overrides.json (HexBoardOverrides), which
    /// tools/hex_tiles/make_board.py applies last on every regeneration and HexBoard.Load applies at load.</summary>
    public static class HexPainter
    {
        public const string BoardsFolder = "Assets/Resources/HexTiles/Boards";

        public static string OverridesPath(string boardsDir, string id) => Path.Combine(boardsDir, id + HexBoardOverrides.Suffix + ".json");

        /// <summary>The board ids in a boards folder (its .json files that are not overrides).</summary>
        public static string[] BoardIds(string boardsDir) => Directory.GetFiles(boardsDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)).Where(n => !n.EndsWith(HexBoardOverrides.Suffix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();

        /// <summary>Checks a value before it reaches a board or an overrides file: a registered prototype or unit (or none),
        /// a HexGround layer, a finite facing. The painter edits hex-tile presentation only: terrain, control and
        /// locations are the map editor's (content/map/overrides.yaml, #1009) and are not fields here.</summary>
        public static void Validate(string field, string value)
        {
            bool ok = field switch
            {
                "prototype" => value == "" || HexPrototypes.All.ContainsKey(value),
                "unit" => value == "" || HexUnitTypes.All.ContainsKey(value),
                "ground" => HexGround.Ids.Contains(value),
                "facing" => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) && !float.IsNaN(f) && !float.IsInfinity(f),
                "region" => true,   // checked against the board's regions in Write
                _ => throw new ArgumentException($"override field '{field}' is not one of {string.Join(", ", HexBoardOverrides.Fields)}"),
            };
            if (!ok) throw new ArgumentException($"'{value}' is not a valid {field}");
        }

        /// <summary>Sets <paramref name="field"/> of hex (q, r) on <paramref name="board"/> and records the edit in the
        /// overrides file at <paramref name="overridesPath"/> (created if missing; an earlier edit of the same field is
        /// replaced). Returns the overrides as written.</summary>
        public static HexBoardOverrides Write(HexBoard board, string overridesPath, int q, int r, string field, string value)
        {
            Validate(field, value);
            if (!board.Hexes.TryGetValue((q, r), out var h)) throw new ArgumentException($"board {board.File.id} has no hex ({q},{r})");
            if (field == "region" && value != "" && !board.Regions.ContainsKey(value)) throw new ArgumentException($"board {board.File.id} has no region '{value}'");
            var overrides = File.Exists(overridesPath) ? HexBoardOverrides.FromJson(File.ReadAllText(overridesPath)) : new HexBoardOverrides();
            overrides.board = board.File.id;
            overrides.Record(q, r, field, value);
            HexBoardOverrides.Set(h, field, value);
            File.WriteAllText(overridesPath, overrides.ToJson());
            return overrides;
        }

        /// <summary>Writes one edit for the board under <paramref name="root"/> (built with Options.PerHex) and rebuilds
        /// just that hex. Returns the hex's new tile report (null when it has no tile).</summary>
        public static HexTileReport? Change(GameObject root, string overridesPath, int q, int r, string field, string value)
        {
            var state = root.GetComponent<HexBoardState>() ?? throw new InvalidOperationException("load the board with the Hex Painter first");
            Write(state.Board, overridesPath, q, r, field, value);
            return HexBoardBuilder.RebuildHex(root, q, r);
        }

        /// <summary>The board hex under a ray (a Scene-view click): where it meets the board's terrain, else sea level.
        /// Null off the board.</summary>
        public static (int q, int r)? Pick(GameObject root, Ray ray)
        {
            var state = root.GetComponent<HexBoardState>();
            if (state == null) return null;
            var board = root.transform;
            Vector3 point;
            var hit = Physics.RaycastAll(ray, 1e7f).Where(h => h.collider.transform.IsChildOf(board)).OrderBy(h => h.distance).ToList();
            if (hit.Count > 0) point = hit[0].point;
            else if (new Plane(Vector3.up, board.position).Raycast(ray, out var d)) point = ray.GetPoint(d);
            else return null;
            var k = HexGeometry.Axial(board.InverseTransformPoint(point), HexScale.HexM);
            return state.Board.Hexes.ContainsKey(k) ? k : null;
        }

        /// <summary>Builds board <paramref name="id"/> (with its overrides) in the open scene, one object per hex.</summary>
        public static GameObject Load(string id)
        {
            var root = HexBoardBuilder.Build(HexBoard.Load(id), Capture.HexBoardCapture.Ground, options: new HexBoardBuilder.Options { PerHex = true }).root;
            Physics.SyncTransforms();   // so Pick's rays meet the new terrain's collider
            return root;
        }

        /// <summary>Re-imports an overrides file written under Assets, so Resources (and HexBoard.Load) see it.</summary>
        public static void Import(string overridesPath)
        {
            var full = Path.GetFullPath(overridesPath).Replace('\\', '/');
            var assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (full.StartsWith(assets + "/", StringComparison.Ordinal)) AssetDatabase.ImportAsset("Assets" + full.Substring(assets.Length));
        }
    }
}
