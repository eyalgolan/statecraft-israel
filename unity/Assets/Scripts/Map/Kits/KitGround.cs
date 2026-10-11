#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>What the kits ask of the map while they are built: ground height (world y, exaggerated relief), real
    /// elevation, water, the biome of the hex under a point, and the footprints already claimed by settlements (so groves
    /// stay out of towns). Built once per map build.</summary>
    public sealed class KitGround
    {
        readonly Terrain terrain;
        readonly float terrainY, seaLevelY, exaggeration, elevMinM;
        readonly Dictionary<(int, int), string> biome = new Dictionary<(int, int), string>();
        readonly HexBounds bounds; readonly double hexSizeKm;
        public readonly GeoFrame Frame;
        public readonly float HexSizeM;
        /// <summary>Settlement footprints (centre x, z; radius), claimed in placement order.</summary>
        public readonly List<(Vector2 centre, float radius, string id)> Footprints = new List<(Vector2, float, string)>();

        public KitGround(Terrain terrain, GeoFrame frame, HexFile hexes, float elevMinM)
        {
            this.terrain = terrain; Frame = frame; this.elevMinM = elevMinM;
            terrainY = terrain.transform.position.y;
            exaggeration = MapBuilder.VerticalExaggeration;
            seaLevelY = terrainY + (0f - elevMinM) * exaggeration;
            bounds = hexes.bounds; hexSizeKm = hexes.hex_size_km; HexSizeM = hexes.hex_size_km * 1000f;
            foreach (var h in hexes.hexes) biome[(h.q, h.r)] = h.biome;
        }

        public float Width => Frame.WidthM;
        public float Length => Frame.LengthM;

        public bool Inside(float x, float z) => x > 50f && z > 50f && x < Frame.WidthM - 50f && z < Frame.LengthM - 50f;

        /// <summary>World y of the ground at (x, z).</summary>
        public float Y(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainY;

        /// <summary>Real metres above sea level at (x, z) (the heightmap's own elevation, before exaggeration).</summary>
        public float ElevM(float x, float z) => (Y(x, z) - seaLevelY) / exaggeration;

        public string Biome(float x, float z)
        {
            var (lon, lat) = Frame.ToLonLat(new Vector3(x, 0f, z));
            return biome.TryGetValue(HexPicker.FromLonLat(lon, lat, bounds, hexSizeKm), out var b) ? b : "sea";
        }

        /// <summary>Open sea (the heightmap writes it as exactly 0 m: MapBuilder.SeaToleranceM), a lake hex, or off the map.</summary>
        public bool Water(float x, float z)
        {
            if (!Inside(x, z)) return true;
            if (Mathf.Abs(ElevM(x, z)) <= MapBuilder.SeaToleranceM + 0.5f) return true;
            var b = Biome(x, z);
            return b == "lake" || (b == "sea" && ElevM(x, z) < 2f);
        }

        /// <summary>The steepest rise (real metres per real metre) between the four points of a w-wide square around (x, z).</summary>
        public float Slope(float x, float z, float w)
        {
            float h = w * 0.5f;
            float a = ElevM(x - h, z), b = ElevM(x + h, z), c = ElevM(x, z - h), d = ElevM(x, z + h);
            return Mathf.Max(Mathf.Abs(a - b), Mathf.Abs(c - d)) / w;
        }

        /// <summary>True when (x, z) lies within <paramref name="share"/> of any claimed footprint's radius.</summary>
        /// <summary>Every building's and landmark's lot (centre x, z; radius), filled by SettlementKits.Build before the trees
        /// are planted, so no tree grows through a roof (integration bug B9).</summary>
        public readonly List<(Vector2 centre, float radius)> Lots = new List<(Vector2, float)>();

        /// <summary>True when a crown of radius <paramref name="crown"/> at (x, z) would reach into any building's or landmark's
        /// lot. A linear scan: a few hundred lots and about ten thousand trees at load.</summary>
        public bool OnLot(float x, float z, float crown)
        {
            var p = new Vector2(x, z);
            foreach (var (c, r) in Lots) { float d = r + crown; if ((p - c).sqrMagnitude < d * d) return true; }
            return false;
        }

        // Route corridors (the railways' and roads' ribbons, RouteLayer.Corridors): segments with their clear half-width, in
        // a 2 km grid so a placement test scans only the segments near it.
        const float CorridorCellM = 2000f;
        readonly List<(Vector2 a, Vector2 b, float half)> corridorSegments = new List<(Vector2, Vector2, float)>();
        readonly Dictionary<(int, int), List<int>> corridorCells = new Dictionary<(int, int), List<int>>();
        float corridorMaxHalf;

        /// <summary>Route segments registered with <see cref="AddCorridor"/>.</summary>
        public int CorridorSegments => corridorSegments.Count;

        /// <summary>Keeps buildings and trees off a route: no lot or crown may come within <paramref name="halfWidth"/> of
        /// the polyline <paramref name="line"/> (world x, z). Call before SettlementKits.Build and Vegetation.Plant.</summary>
        public void AddCorridor(IReadOnlyList<Vector2> line, float halfWidth)
        {
            for (int i = 0; i + 1 < line.Count; i++)
            {
                var a = line[i]; var b = line[i + 1];
                int index = corridorSegments.Count;
                corridorSegments.Add((a, b, halfWidth));
                corridorMaxHalf = Mathf.Max(corridorMaxHalf, halfWidth);
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - halfWidth) / CorridorCellM), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + halfWidth) / CorridorCellM);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - halfWidth) / CorridorCellM), z1 = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + halfWidth) / CorridorCellM);
                for (int cz = z0; cz <= z1; cz++)
                    for (int cx = x0; cx <= x1; cx++)
                    {
                        if (!corridorCells.TryGetValue((cx, cz), out var list)) corridorCells[(cx, cz)] = list = new List<int>();
                        list.Add(index);
                    }
            }
        }

        /// <summary>True when a disc of radius <paramref name="r"/> at <paramref name="p"/> reaches into any route corridor.</summary>
        public bool OnRoute(Vector2 p, float r)
        {
            if (corridorSegments.Count == 0) return false;
            float reach = r + corridorMaxHalf;
            int x0 = Mathf.FloorToInt((p.x - reach) / CorridorCellM), x1 = Mathf.FloorToInt((p.x + reach) / CorridorCellM);
            int z0 = Mathf.FloorToInt((p.y - reach) / CorridorCellM), z1 = Mathf.FloorToInt((p.y + reach) / CorridorCellM);
            for (int cz = z0; cz <= z1; cz++)
                for (int cx = x0; cx <= x1; cx++)
                {
                    if (!corridorCells.TryGetValue((cx, cz), out var list)) continue;
                    foreach (var i in list)
                    {
                        var (a, b, half) = corridorSegments[i];
                        if (SegmentDistance(p, a, b) < half + r) return true;
                    }
                }
            return false;
        }

        /// <summary>Distance from <paramref name="p"/> to the segment a-b.</summary>
        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return (p - (a + ab * t)).magnitude;
        }

        public bool InTown(float x, float z, float share = 1f)
        {
            foreach (var f in Footprints)
            {
                float r = f.radius * share;
                if ((new Vector2(x, z) - f.centre).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        /// <summary>Distance (m) to the nearest open-sea sample on a ring of 12 directions at up to <paramref name="maxM"/>,
        /// or float.MaxValue: the coast test for palms.</summary>
        public float SeaDistance(float x, float z, float maxM)
        {
            float best = float.MaxValue;
            for (int step = 1; step <= 4; step++)
            {
                float r = maxM * step / 4f;
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6f;
                    float px = x + Mathf.Cos(a) * r, pz = z + Mathf.Sin(a) * r;
                    if (Inside(px, pz) && Mathf.Abs(ElevM(px, pz)) <= MapBuilder.SeaToleranceM + 0.5f) { best = Mathf.Min(best, r); }
                }
                if (best < float.MaxValue) return best;
            }
            return best;
        }
    }
}
