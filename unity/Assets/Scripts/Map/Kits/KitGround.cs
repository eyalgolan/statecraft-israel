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
        /// <summary>Settlement footprints (centre x, z; radius), claimed in placement order. Indexed for questions up to 1.7
        /// radii from a centre (SettlementKits' cells reach 1.6, InTown's shares 1.2).</summary>
        public readonly DiscIndex Footprints = new DiscIndex(FootprintReach, 0f);
        public const float FootprintReach = 1.7f;

        public KitGround(Terrain terrain, GeoFrame frame, HexFile hexes, float elevMinM)
        {
            this.terrain = terrain; Frame = frame; this.elevMinM = elevMinM;
            terrainY = terrain.transform.position.y;
            exaggeration = MapBuilder.VerticalExaggeration;
            seaLevelY = terrainY + (0f - elevMinM) * exaggeration;
            bounds = hexes.bounds; hexSizeKm = hexes.hex_size_km; HexSizeM = hexes.hex_size_km * 1000f;
            foreach (var h in hexes.hexes) biome[(h.q, h.r)] = h.biome;
            // The lakes' hexes and their neighbours: where a lake's water can lie (a lake hex is at least 40% water, so the
            // lake's edge stays within its hexes' neighbours; KitTests.NothingStandsInALakesDrawnWater checks the whole rift).
            foreach (var h in hexes.hexes)
                if (h.biome == "lake")
                    foreach (var (dq, dr) in new[] { (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, -1), (-1, 1) })
                        lakeZone.Add((h.q + dq, h.r + dr));
            foreach (var h in hexes.hexes)
            {
                bool wetMountain = false;
                if (h.biome == "mountains")
                    foreach (var (dq, dr) in Neighbours)
                        wetMountain |= biome.TryGetValue((h.q + dq, h.r + dr), out var b) && b == "hills";
                if (h.biome != "hills" && !wetMountain) continue;
                hillCountry.Add((h.q, h.r));
                var c = frame.ToWorld(h.lon, h.lat, 0f);
                hillMin = Vector2.Min(hillMin, new Vector2(c.x, c.z) - Vector2.one * HexSizeM);
                hillMax = Vector2.Max(hillMax, new Vector2(c.x, c.z) + Vector2.one * HexSizeM);
            }
            int res = Mathf.Max(2, terrain.terrainData.heightmapResolution);
            sampleX = frame.WidthM / (res - 1); sampleZ = frame.LengthM / (res - 1);
        }

        readonly float sampleX, sampleZ;   // the heightmap's sample spacing (m): the lake test's 3 x 3 window

        /// <summary>The terrain's world position (the hex grid's origin: HexGrid.ToWorld).</summary>
        public Vector3 TerrainOrigin => terrain.transform.position;
        public float Width => Frame.WidthM;
        public float Length => Frame.LengthM;

        public bool Inside(float x, float z) => x > 50f && z > 50f && x < Frame.WidthM - 50f && z < Frame.LengthM - 50f;

        /// <summary>World y of the ground at (x, z).</summary>
        public float Y(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainY;

        /// <summary>Real metres above sea level at (x, z) (the heightmap's own elevation, before exaggeration).</summary>
        public float ElevM(float x, float z) => (Y(x, z) - seaLevelY) / exaggeration;

        public string Biome(float x, float z) => biome.TryGetValue(Hex(x, z), out var b) ? b : "sea";

        internal (int, int) Hex(float x, float z)
        {
            var (lon, lat) = Frame.ToLonLat(new Vector3(x, 0f, z));
            return HexPicker.FromLonLat(lon, lat, bounds, hexSizeKm);
        }

        readonly HashSet<(int, int)> lakeZone = new HashSet<(int, int)>();
        readonly HashSet<(int, int)> hillCountry = new HashSet<(int, int)>();
        static readonly (int, int)[] Neighbours = { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };   // axial (tools/hexgrid.py)

        /// <summary>Whether (x, z) lies in the wet hill country, where the land-use bake paints garrigue, terraces and scrub
        /// (tools/land_bake.py: "wet", 250-600 mm of rain): a "hills" hex (at least 200 mm, below 700 m: tools/hexgrid.py) or
        /// a "mountains" hex beside one (the Judean and Galilee heights). The mountains of the Negev, Sinai and Edom border
        /// only desert and stay bare. Hexes carry no rainfall, so this is the neighbour rule; against the CHELSA rain of the
        /// 130 mountain hexes up to 950 m it takes 36 of the 40 with 250 mm or more and 5 of the 90 drier ones.</summary>
        public bool HillCountry(float x, float z) => hillCountry.Contains(Hex(x, z));
        /// <summary>A world rectangle (x, z) that holds all of the hill country.</summary>
        public Rect HillCountryBounds => hillMax.x < hillMin.x ? Rect.zero : Rect.MinMaxRect(hillMin.x, hillMin.y, hillMax.x, hillMax.y);
        Vector2 hillMin = Vector2.positiveInfinity, hillMax = Vector2.negativeInfinity;

        /// <summary>Open sea (the heightmap writes it as exactly 0 m: MapBuilder.SeaToleranceM), a lake's water, or off the
        /// map. A lake is its water surface, not its hexes (#317): a lake hex also holds its shore (Tiberias stands in one,
        /// 60-160 m above the Sea of Galilee), and counting all of it as water left Tiberias no ground to build on, while a
        /// shore hex of another biome can hold some of the lake. The surface is told as the drawn water tells it
        /// (MapBuilder.WaterMeshGrid, tools/terrain_bake.py): below <see cref="MapBuilder.LakeBelowM"/> and flat within
        /// <see cref="MapBuilder.LakeFlatM"/> over the heightmap's 3 x 3 window, at the point or one sample from it.</summary>
        public bool Water(float x, float z)
        {
            if (!Inside(x, z)) return true;
            float e = ElevM(x, z);
            if (Mathf.Abs(e) <= MapBuilder.SeaToleranceM + 0.5f) return true;
            // Only the rift lies this low (no sea hex does), and only a lake's hex or its neighbour holds lake water.
            if (e < MapBuilder.LakeBelowM) return lakeZone.Contains(Hex(x, z)) && NearLakeSurface(x, z);
            return e < 2f && Biome(x, z) == "sea";
        }

        /// <summary>The lake's flat surface lies at (x, z) or within one heightmap sample of it. The drawn shore (coast.png's
        /// distance, interpolated between texel centres) runs up to about a sample past the last flat sample, so a tree or
        /// house one sample short of the flat water would still stand in the drawn lake (a shore palm by Ghuweir Abu Shusha
        /// did).</summary>
        bool NearLakeSurface(float x, float z)
        {
            int gx = Mathf.RoundToInt(x / sampleX), gz = Mathf.RoundToInt(z / sampleZ);
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                    if (LakeFlat(gx + i, gz + j)) return true;
            return false;
        }

        /// <summary>Whether heightmap sample (gx, gz) is a lake's flat surface: below LakeBelowM, its 3 x 3 window within
        /// LakeFlatM. Cached per sample (the rift's floor is asked about thousands of times while the kits are built).</summary>
        bool LakeFlat(int gx, int gz)
        {
            long key = ((long)gx << 32) | (uint)gz;
            if (lakeFlat.TryGetValue(key, out var flat)) return flat;
            float lo = float.MaxValue, hi = float.MinValue;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    float e = ElevM((gx + i) * sampleX, (gz + j) * sampleZ);
                    lo = Mathf.Min(lo, e); hi = Mathf.Max(hi, e);
                }
            return lakeFlat[key] = hi < MapBuilder.LakeBelowM && hi - lo <= MapBuilder.LakeFlatM;
        }

        readonly Dictionary<long, bool> lakeFlat = new Dictionary<long, bool>();

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
        public readonly DiscIndex Lots = new DiscIndex(1f, LotQueryM);
        /// <summary>The largest crown or lot radius a lot query is indexed for (a bigger one scans every lot).</summary>
        public const float LotQueryM = 1500f;

        /// <summary>True when a crown of radius <paramref name="crown"/> at (x, z) would reach into any building's or landmark's
        /// lot (through the grid index: some 2,000 lots and 10,000 trees at load).</summary>
        public bool OnLot(float x, float z, float crown)
        {
            var p = new Vector2(x, z);
            if (crown > LotQueryM) { foreach (var (c, r, _) in Lots) { float d = r + crown; if ((p - c).sqrMagnitude < d * d) return true; } return false; }
            var near = Lots.Near(p);
            for (int k = 0; k < near.Count; k++) { var (c, r, _) = Lots[near[k]]; float d = r + crown; if ((p - c).sqrMagnitude < d * d) return true; }
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

        /// <summary>True when a disc of radius <paramref name="r"/> at <paramref name="p"/> (world x, z) covers the midpoint of
        /// an edge of the hex it stands in. The 1948 villages' houses and the camps' tents keep off these, so the hex grid
        /// still reads through them where it is easiest to see (review M10, KitTests.TheGridStaysReadableThroughTownsAndGroves).</summary>
        public bool OnGridMark(Vector2 p, float r)
        {
            var g = HexGrid.FromWorld(new Vector3(p.x, 0f, p.y), TerrainOrigin, Length);
            var c = HexGrid.NearestCentre(g, HexSizeM);
            float inradius = 0.8660254f * HexSizeM;
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f;
                if ((g - (c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inradius)).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        /// <summary>The unit tokens that stand outside every footprint (centre x, z; ring radius m; MapScene). The Bedouin camps,
        /// the one kit placed where no footprint is, keep off them (units-in-play merge, review I5).</summary>
        public readonly List<(Vector2 centre, float radius)> Tokens = new List<(Vector2, float)>();

        /// <summary>True when a disc of radius <paramref name="r"/> at <paramref name="p"/> reaches a unit token's ring.</summary>
        public bool OnToken(Vector2 p, float r)
        {
            foreach (var (c, tr) in Tokens) if ((p - c).sqrMagnitude < (tr + r) * (tr + r)) return true;
            return false;
        }

        /// <summary>The unit tokens outside the towns with the day each one's window ends (exclusive; MapScene.UnitTokens), for
        /// the transit camps built later (PlacementLayer.CampGround): a camp keeps off only those still present on or after
        /// its own date (T67, owner 15 October 2026: "Yes, date-aware").</summary>
        public readonly List<(Vector2 centre, float radius, System.DateTime until)> DatedTokens = new List<(Vector2, float, System.DateTime)>();

        /// <summary>As <see cref="OnToken(Vector2, float)"/>, also reaching a dated token whose window ends after
        /// <paramref name="from"/>: one present on that day or later.</summary>
        public bool OnToken(Vector2 p, float r, System.DateTime from)
        {
            if (OnToken(p, r)) return true;
            foreach (var (c, tr, until) in DatedTokens)
                if (until > from && (p - c).sqrMagnitude < (tr + r) * (tr + r)) return true;
            return false;
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
            var p = new Vector2(x, z);
            if (share > FootprintReach)
            {
                foreach (var f in Footprints) { float r = f.radius * share; if ((p - f.centre).sqrMagnitude < r * r) return true; }
                return false;
            }
            var near = Footprints.Near(p);
            for (int k = 0; k < near.Count; k++)
            {
                var f = Footprints[near[k]]; float r = f.radius * share;
                if ((p - f.centre).sqrMagnitude < r * r) return true;
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
