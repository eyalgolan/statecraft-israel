#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>One hex of a board file. q and r are the game's own axial coordinates (content/map/hexes.json), so a board
    /// maps one to one onto the map later. terrain: flat, hills, mountain or water. ground: a HexGround layer id. prototype:
    /// a HexPrototype id, or empty for a context hex (ground only; a mountain context hex gets "mountain_peaks"). unit: an
/// optional HexUnitType standing on the hex (units are a layer, never part of the tile), facing in degrees from north.</summary>
    [Serializable] public sealed class BoardHex
    {
        public int q, r; public string terrain = "flat"; public string ground = "grass"; public string prototype = ""; public string note = "";
        /// <summary>A unit standing on this hex (a HexUnitType id), drawn on its own layer above the tile; its heading.</summary>
        public string unit = ""; public float facing;
        /// <summary>Environment, not play: dressed as continuous landscape (no tile, no hex lines) round the playable hexes.</summary>
        public bool env;
        /// <summary>The landscape region the generator put the hex in (a BoardRegion id; tools/hex_tiles/regions.yaml).
        /// Its environment hexes take that region's trees and rocks; empty takes the built-in mix.</summary>
        public string region = "";
    }

    /// <summary>One kind of tree (or rock) a region plants on its environment hexes: a model id, the stand-in shape drawn
    /// while the model is missing (tree, pine, palm, rock, box), and its share of the mix.</summary>
    [Serializable] public sealed class BoardSpecies
    {
        public string model = ""; public string shape = "tree"; public float weight = 1f;
        public StandIn StandIn => shape switch { "pine" => StandIn.Pine, "palm" => StandIn.Palm, "rock" => StandIn.Rock, "box" => StandIn.Box, _ => StandIn.Tree };
    }

    /// <summary>A landscape region's environment dress (written by tools/hex_tiles/make_board.py from regions.yaml): the
    /// species on flat and on hills ground, a density multiplier, and the rock outcrop model, how often it shows (two
    /// tries a hex) and whether it also shows on the flat.</summary>
    [Serializable] public sealed class BoardRegion
    {
        public string id = ""; public string title = ""; public float density = 1f;
        public BoardSpecies[] flat = Array.Empty<BoardSpecies>(); public BoardSpecies[] hills = Array.Empty<BoardSpecies>();
        public string rock = "sandbox:limestone*"; public float rockChance = 0.6f; public bool rocksOnFlat;

        /// <summary>The mix every hex without a region gets: olive and carob on the flat, Tabor oak and Aleppo pine on
        /// hills, limestone outcrops on hills.</summary>
        public static readonly BoardRegion BuiltIn = new BoardRegion
        {
            id = "", title = "built-in",
            flat = new[] { new BoardSpecies { model = "sandbox:olive_lite", weight = 0.6f }, new BoardSpecies { model = "sandbox:carob_lite", weight = 0.4f } },
            hills = new[] { new BoardSpecies { model = "sandbox:tabor_oak_lite", weight = 0.55f }, new BoardSpecies { model = "sandbox:aleppo_pine_lite", shape = "pine", weight = 0.45f } },
        };
    }

    /// <summary>A board file (Boards/&lt;id&gt;.json): a fixed, hand-edited layout, so captures compare like for like.</summary>
    /// <summary>A road through hex centres, Civ-style: q0, r0, q1, r1, ... (painted as a dirt road, centre to centre).</summary>
    [Serializable] public sealed class BoardRoad { public string note = ""; public int[] hexes = Array.Empty<int>(); }

    /// <summary>A river along hex edges, Civ-style: q, r, corner triples (corner 0 = north, clockwise), joined in order;
    /// the ground is carved into a channel below sea level along it, so it fills with water.</summary>
    [Serializable] public sealed class BoardRiver { public string note = ""; public int[] corners = Array.Empty<int>(); }

    [Serializable] public sealed class HexBoardFile
    {
        public string id = ""; public string title = ""; public BoardHex[] hexes = Array.Empty<BoardHex>();
        public BoardRoad[] roads = Array.Empty<BoardRoad>(); public BoardRiver[] rivers = Array.Empty<BoardRiver>();
        /// <summary>The landscape regions the hexes name (environment dress); empty on boards made without regions.</summary>
        public BoardRegion[] regions = Array.Empty<BoardRegion>();
    }

    public sealed class HexBoard
    {
        public readonly HexBoardFile File;
        public readonly Dictionary<(int q, int r), BoardHex> Hexes;
        public readonly Dictionary<string, BoardRegion> Regions;
        public HexBoard(HexBoardFile file)
        {
            File = file; Hexes = file.hexes.ToDictionary(h => (h.q, h.r));
            Regions = (file.regions ?? Array.Empty<BoardRegion>()).ToDictionary(g => g.id, StringComparer.Ordinal);
        }

        /// <summary>The region dressing hex <paramref name="h"/>'s environment: its own, else the built-in mix.</summary>
        public BoardRegion RegionOf(BoardHex h) => h.region != null && Regions.TryGetValue(h.region, out var g) ? g : BoardRegion.BuiltIn;

        /// <summary>Each river as world points along hex corners.</summary>
        public List<Vector3[]> RiverLines(float sizeM = HexScale.HexM) => (File.rivers ?? Array.Empty<BoardRiver>()).Select(rv =>
            Enumerable.Range(0, rv.corners.Length / 3).Select(i => HexGeometry.Corners(HexGeometry.Centre(rv.corners[3 * i], rv.corners[3 * i + 1], sizeM), sizeM)[rv.corners[3 * i + 2]]).ToArray()).ToList();

        /// <summary>Each road as world points through hex centres.</summary>
        public List<Vector3[]> RoadLines(float sizeM = HexScale.HexM) => (File.roads ?? Array.Empty<BoardRoad>()).Select(rd =>
            Enumerable.Range(0, rd.hexes.Length / 2).Select(i => HexGeometry.Centre(rd.hexes[2 * i], rd.hexes[2 * i + 1], sizeM)).ToArray()).ToList();
        public static HexBoard FromJson(string json) => new HexBoard(JsonUtility.FromJson<HexBoardFile>(json));

        public const string BoardsFolder = "HexTiles/Boards";
        /// <summary>A board shipped in Resources/HexTiles/Boards/&lt;id&gt;.json (runtime and editor alike), with the hand edits
        /// of its sibling &lt;id&gt;.overrides.json applied last (HexBoardOverrides), so the Hex Painter's edits show before
        /// the board is regenerated.</summary>
        public static HexBoard Load(string id)
        {
            var board = FromJson((Resources.Load<TextAsset>(BoardsFolder + "/" + id) ?? throw new System.IO.FileNotFoundException($"no board Resources/{BoardsFolder}/{id}.json")).text);
            var overrides = Resources.Load<TextAsset>(BoardsFolder + "/" + id + HexBoardOverrides.Suffix);
            if (overrides != null) HexBoardOverrides.FromJson(overrides.text).Apply(board);
            return board;
        }

        /// <summary>Ground textures from Resources/HexTiles/Ground/&lt;texture&gt;_diff_2k and _nor_gl_2k (local while the
        /// textures await their asset batch); missing ones paint the layer's flat colour.</summary>
        public static (Texture2D?, Texture2D?) GroundFromResources(string texture) =>
            (Resources.Load<Texture2D>($"HexTiles/Ground/{texture}_diff_2k"), Resources.Load<Texture2D>($"HexTiles/Ground/{texture}_nor_gl_2k"));

        public static HexTerrain TerrainOf(BoardHex h) => h.terrain switch
        {
            "hills" => HexTerrain.Hills, "mountain" => HexTerrain.Mountain, "water" => HexTerrain.Water, _ => HexTerrain.Flat,
        };
    }

    /// <summary>Heights are game-friendly, as Civ's: hills a broad dome up to ~0.5 km, highland "mountain" a raised plateau
    /// with a smooth ~0.6 km dome (no cliffs or ravines), so units and vehicles cross every land hex.
    /// The board's ground (Civ-style): every hex has a height profile by its terrain (water below sea level,
    /// flat a little above, hills a broad dome, mountains a raised base for the peak props), blended smoothly across edges
    /// so coasts and hill feet round off; outside the board is sea. Water is height below 0. Pure, so tiles, tests and the
    /// Unity terrain all read the same numbers.</summary>
    public sealed class HexBoardSurface : IHexSurface
    {
        public const float SeaY = 0f, WaterY = -520f, FlatY = 80f, HillsY = 450f, MountainBaseY = 420f, MountainDomeY = 560f;
        const float BlendFrom = 0.5f, BlendTo = 1.12f, NoiseM = 9000f, SlopeStepM = 60f;

        readonly HexBoard board;
        readonly float size;
        readonly List<Vector3[]> rivers;
        /// <summary>River channel half-width and bank width (metres): water within the channel, banks easing down to it.</summary>
        public const float RiverChannelM = 110f, RiverBankM = 420f, RiverBedY = -14f;

        public HexBoardSurface(HexBoard board, float sizeM = HexScale.HexM) { this.board = board; size = sizeM; rivers = board.RiverLines(sizeM); }

        public float Height(Vector3 p)
        {
            float h = Terrain(p);
            foreach (var line in rivers)
            {
                float d = DistanceToLine(p, line);
                if (d < RiverBankM) h = Mathf.Lerp(Mathf.Min(h, RiverBedY), h, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RiverChannelM, RiverBankM, d)));
            }
            return h;
        }

        public static float DistanceToLine(Vector3 p, IReadOnlyList<Vector3> line)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = new Vector2(line[i].x, line[i].z), b = new Vector2(line[i + 1].x, line[i + 1].z), q = new Vector2(p.x, p.z);
                var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                best = Mathf.Min(best, (q - (a + ab * t)).magnitude);
            }
            return best;
        }

        float Terrain(Vector3 p)
        {
            var (q0, r0) = HexGeometry.Axial(p, size);
            float sum = 0f, wsum = 0f;
            for (int i = -1; i < 6; i++)
            {
                var (dq, dr) = i < 0 ? (0, 0) : i < 6 ? HexGeometry.Neighbours[i] : Ring2[i - 6];
                int q = q0 + dq, r = r0 + dr;
                var c = HexGeometry.Centre(q, r, size);
                float d = new Vector2(p.x - c.x, p.z - c.z).magnitude / size;
                float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BlendTo, BlendFrom, d));
                if (w <= 0f) continue;
                sum += w * Profile(q, r, d, p); wsum += w;
            }
            return wsum > 0f ? sum / wsum : WaterY;
        }

        float Profile(int q, int r, float d, Vector3 p)
        {
            var terrain = board.Hexes.TryGetValue((q, r), out var h) ? HexBoard.TerrainOf(h) : HexTerrain.Water;
            float n = Mathf.PerlinNoise(p.x / NoiseM + 31.7f, p.z / NoiseM + 11.3f) - 0.5f;
            float dome = Mathf.Clamp01(1f - d * d);
            return terrain switch
            {
                HexTerrain.Water => WaterY + 40f * n,
                HexTerrain.Hills => FlatY + HillsY * dome * (0.8f + 0.5f * n) + 140f * Mathf.PerlinNoise(p.x / 2600f + 5.1f, p.z / 2600f + 9.7f) * dome,
                HexTerrain.Mountain => FlatY + MountainBaseY + MountainDomeY * dome,
                _ => FlatY + 50f * n,
            };
        }

        public bool IsWater(Vector3 p) => Height(p) < SeaY + 4f;

        public float SlopeDeg(Vector3 p)
        {
            float dx = Height(p + new Vector3(SlopeStepM, 0f, 0f)) - Height(p - new Vector3(SlopeStepM, 0f, 0f));
            float dz = Height(p + new Vector3(0f, 0f, SlopeStepM)) - Height(p - new Vector3(0f, 0f, SlopeStepM));
            return Mathf.Atan(new Vector2(dx, dz).magnitude / (2f * SlopeStepM)) * Mathf.Rad2Deg;
        }

        /// <summary>Per-layer ground weights at p: each hex's own layer blended like the heights; sand on the shore, seabed
        /// under water.</summary>
        public void GroundWeights(Vector3 p, IReadOnlyList<string> layers, float[] w)
        {
            Array.Clear(w, 0, w.Length);
            float y = Height(p);
            int beach = IndexOf(layers, "beach"), seabed = IndexOf(layers, "seabed"), green = IndexOf(layers, "grass");
            if (y < SeaY - 20f) { w[seabed] = 1f; return; }
            if (y < SeaY + 30f) { w[beach] = 1f; return; }
            var (q0, r0) = HexGeometry.Axial(p, size);
            float wsum = 0f;
            float warpX = (Mathf.PerlinNoise(p.x / 6000f + 2.7f, p.z / 6000f + 8.1f) - 0.5f) * 0.9f * size;
            float warpZ = (Mathf.PerlinNoise(p.x / 6000f + 6.3f, p.z / 6000f + 1.9f) - 0.5f) * 0.9f * size;
            bool inPlay = board.Hexes.TryGetValue((q0, r0), out var own) && !own.env;
            for (int i = -1, n = inPlay ? 6 : 18; i < n; i++)   // ring 2 can only reach an environment pixel
            {
                var (dq, dr) = i < 0 ? (0, 0) : i < 6 ? HexGeometry.Neighbours[i] : Ring2[i - 6];
                int q = q0 + dq, r = r0 + dr;
                if (!board.Hexes.TryGetValue((q, r), out var h) || HexBoard.TerrainOf(h) == HexTerrain.Water) continue;
                if (h.env && inPlay) continue;   // the landscape never bleeds into a play hex
                var c = HexGeometry.Centre(q, r, size);
                // Play hexes keep a crisp edge (it reaches past the corners, so three hexes always share a corner). The
                // environment ring blends wide over a noise-warped distance, so its grounds meander like a landscape.
                float k;
                if (h.env) k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.45f, 0.35f, new Vector2(p.x + warpX - c.x, p.z + warpZ - c.z).magnitude / size)) * 0.6f;
                else k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.04f, 0.78f, new Vector2(p.x - c.x, p.z - c.z).magnitude / size));
                int l = IndexOf(layers, h.ground); if (l < 0 || k <= 0f) continue;
                if (h.env && green >= 0 && h.ground != "grass" && h.ground != "desert")
                {
                    // Landscape: meandering green meadows through the dry ground, which also breaks up texture tiling.
                    float f = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.62f, Mathf.PerlinNoise(p.x / 4200f + 4.4f, p.z / 4200f + 0.6f)));
                    w[green] += k * f; w[l] += k * (1f - f); wsum += k;
                    continue;
                }
                w[l] += k; wsum += k;
            }
            if (wsum <= 0f) { w[beach] = 1f; return; }
            for (int l = 0; l < w.Length; l++) w[l] /= wsum;
        }

        static readonly (int q, int r)[] Ring2 = { (2, 0), (2, -1), (2, -2), (1, -2), (0, -2), (-1, -1), (-2, 0), (-2, 1), (-2, 2), (-1, 2), (0, 2), (1, 1) };

        static int IndexOf(IReadOnlyList<string> list, string id) { for (int i = 0; i < list.Count; i++) if (list[i] == id) return i; return -1; }
    }

    /// <summary>Ground texture layers: an id, the Poly Haven texture (local, git-ignored; HexGroundTextures supplies it in the
    /// editor), tile size, tint, and a flat colour used when the texture is not on this machine.</summary>
    public static class HexGround
    {
        public static readonly (string id, string texture, float tileM, Color tint, Color fallback)[] Layers =
        {
            ("grass", "aerial_grass_rock", 3400f, new Color(1.00f, 1.42f, 0.62f), new Color(0.47f, 0.60f, 0.27f)),
            ("plains", "grass_ground", 4600f, new Color(1.28f, 1.30f, 0.66f), new Color(0.62f, 0.62f, 0.33f)),
            ("hills", "aerial_ground_rock", 4000f, new Color(1.00f, 1.36f, 0.62f), new Color(0.55f, 0.56f, 0.36f)),
            ("mountain", "rocky_terrain_02", 6000f, new Color(1.45f, 1.38f, 1.22f), new Color(0.52f, 0.49f, 0.44f)),
            ("desert", "aerial_sand", 4500f, new Color(1.38f, 1.20f, 0.88f), new Color(0.82f, 0.70f, 0.50f)),
            ("beach", "aerial_beach_01", 2200f, new Color(1.30f, 1.24f, 1.05f), new Color(0.86f, 0.80f, 0.64f)),
            ("seabed", "aerial_beach_01", 3000f, new Color(0.55f, 0.95f, 0.95f), new Color(0.35f, 0.45f, 0.45f)),
            // Painted by tiles and roads (ctx.PaintGround / PaintStroke), generated here (asset-staging/tools/kit/make_crop_textures.py).
            ("wheat", "hex_crop_wheat", 1700f, Color.white, new Color(0.84f, 0.70f, 0.38f)),
            ("green_crop", "hex_crop_green", 1700f, Color.white, new Color(0.41f, 0.55f, 0.23f)),
            ("ploughed", "hex_crop_ploughed", 1500f, Color.white, new Color(0.50f, 0.38f, 0.27f)),
            ("dirt", "hex_dirt", 700f, Color.white, new Color(0.59f, 0.50f, 0.38f)),
        };

        public static IReadOnlyList<string> Ids => Layers.Select(l => l.id).ToArray();
    }
}
