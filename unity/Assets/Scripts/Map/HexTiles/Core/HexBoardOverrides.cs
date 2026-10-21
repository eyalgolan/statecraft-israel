#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Statecraft.HexTiles
{
    [Serializable] public sealed class BoardHexRef { public int q, r; }

    /// <summary>One hand edit: set one field of one hex to <see cref="value"/> (facing as invariant-culture degrees).</summary>
    [Serializable] public sealed class BoardHexValue { public BoardHexRef hex = new BoardHexRef(); public string value = ""; }

    /// <summary>A board's hand edits, kept beside it as &lt;id&gt;.overrides.json so they survive regeneration:
    /// tools/hex_tiles/make_board.py applies them last when it writes the board, and HexBoard.Load applies them again at
    /// load (the same edits twice give the same board). The Hex Painter writes them.
    /// The shape follows the map editor's content/map/overrides.yaml (#1099): one list per field of {hex: {q, r}, value},
    /// no hex twice in a list, so an edit that sets a unit to "" or a facing to 0 is still an edit and an absent field is
    /// never touched. The fields are hex-tile presentation only (prototype, ground, unit, facing, region: the tile, its
    /// ground layer, the unit standing on it, and the region whose trees dress it); terrain, control and locations are
    /// the map editor's, in content/map/overrides.yaml. This file stays out of content/, so the sim never reads it and
    /// painting a hex never moves the content hash.</summary>
    [Serializable] public sealed class HexBoardOverrides
    {
        public const string Suffix = ".overrides";
        public static readonly string[] Fields = { "prototype", "ground", "unit", "facing", "region" };

        public string board = "";
        public BoardHexValue[] prototype = Array.Empty<BoardHexValue>();
        public BoardHexValue[] ground = Array.Empty<BoardHexValue>();
        public BoardHexValue[] unit = Array.Empty<BoardHexValue>();
        public BoardHexValue[] facing = Array.Empty<BoardHexValue>();
        public BoardHexValue[] region = Array.Empty<BoardHexValue>();

        public static HexBoardOverrides FromJson(string json)
        {
            var o = JsonUtility.FromJson<HexBoardOverrides>(json) ?? new HexBoardOverrides();
            foreach (var field in Fields)
            {
                var list = o.List(field) ?? Array.Empty<BoardHexValue>();
                o.SetList(field, list);
                var seen = new HashSet<(int, int)>();
                foreach (var e in list)
                    if (!seen.Add((e.hex.q, e.hex.r))) throw new ArgumentException($"a second {field} override of hex ({e.hex.q},{e.hex.r})");
            }
            return o;
        }

        public string ToJson() => JsonUtility.ToJson(this, true) + "\n";

        /// <summary>Applies every edit. Returns the edits whose hex the board does not have (not applied), with their field.</summary>
        public List<(string field, BoardHexValue edit)> Apply(HexBoard target)
        {
            var missing = new List<(string, BoardHexValue)>();
            foreach (var field in Fields)
                foreach (var e in List(field))
                {
                    if (target.Hexes.TryGetValue((e.hex.q, e.hex.r), out var h)) Set(h, field, e.value);
                    else missing.Add((field, e));
                }
            return missing;
        }

        /// <summary>Records an edit: replaces this hex's earlier edit of the same field, else adds one at the end.</summary>
        public void Record(int q, int r, string field, string value)
        {
            var list = List(field).ToList();
            int i = list.FindIndex(e => e.hex.q == q && e.hex.r == r);
            if (i >= 0) list[i].value = value; else list.Add(new BoardHexValue { hex = new BoardHexRef { q = q, r = r }, value = value });
            SetList(field, list.ToArray());
        }

        /// <summary>The edits of one field.</summary>
        public BoardHexValue[] List(string field) => field switch
        {
            "prototype" => prototype, "ground" => ground, "unit" => unit, "facing" => facing, "region" => region,
            _ => throw new ArgumentException(Unknown(field)),
        };

        void SetList(string field, BoardHexValue[] list)
        {
            switch (field)
            {
                case "prototype": prototype = list; break;
                case "ground": ground = list; break;
                case "unit": unit = list; break;
                case "facing": facing = list; break;
                case "region": region = list; break;
                default: throw new ArgumentException(Unknown(field));
            }
        }

        /// <summary>The value of <paramref name="field"/> on a hex, as an edit stores it.</summary>
        public static string Get(BoardHex h, string field) => field switch
        {
            "prototype" => h.prototype, "ground" => h.ground, "unit" => h.unit, "region" => h.region,
            "facing" => h.facing.ToString("R", CultureInfo.InvariantCulture),
            _ => throw new ArgumentException(Unknown(field)),
        };

        public static void Set(BoardHex h, string field, string value)
        {
            switch (field)
            {
                case "prototype": h.prototype = value; break;
                case "ground": h.ground = value; break;
                case "unit": h.unit = value; break;
                case "region": h.region = value; break;
                case "facing": h.facing = float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture); break;
                default: throw new ArgumentException(Unknown(field));
            }
        }

        static string Unknown(string field) => $"override field '{field}' is not one of {string.Join(", ", Fields)}";
    }
}
