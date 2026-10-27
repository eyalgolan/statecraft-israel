#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Lydda (Lod) in 1948: a compact old stone town, the Mandate's main railway junction on its west side, and
    /// olive groves round the rest. Drawn as architecture and landscape only: no ruins, damage or abandonment cues. It
    /// differs from ramla_town on purpose: the rail yard, a round old core instead of a street grid, and olives instead of
    /// citrus and cactus.
    /// - The old core: stone houses (kit_jerusalem, kit_jerusalem_2, kit_stone_village_2, kit_jaffa_2) in two rings round
    ///   a small square at the centre, kept open for a unit.
    /// - North of the square, low-key, the Church of St George with the adjoining mosque. The present Greek Orthodox
    ///   church dates from 1870 to 1872 (completed by 1893 in some accounts), with its bell tower on the west side; the
    ///   mosque beside it has a minaret noted by 1495 (https://en.wikipedia.org/wiki/Church_of_Saint_George,_Lod and
    ///   https://biblewalks.com/stgeorgelod, both checked 2026-10-19). Drawn as a modest stone church block with a
    ///   bell-tower spire at its west end, and a smaller block with a minaret spire at its east end.
    /// - The railway, west of the town: Lod station opened on 24 May 1891 on the Jaffa to Jerusalem line, was rebuilt in
    ///   1917 to 1920 and was the main railway hub of Mandatory Palestine (https://en.wikipedia.org/wiki/Lod_railway_station,
    ///   checked 2026-10-19). Three tracks run north to south as painted strokes on a ballast bed, a fourth branches off
    ///   to the south-east, the station building stands on the town side, two goods sheds on the far side, and a water
    ///   tank by the line. The yard's details are generic, not drawn from a plan.
    /// - Olive groves: two staggered rings of olive trees on tilled soil, east of the line.
    /// - No figures: Lydda changed hands during 1948, and a static tile cannot say who lives there across 1948 to 1952.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 15 houses (kit_*, up to 1,400 each) = 21,000.
    /// - At most 22 olives (olive_lite, 1,602 each) = 35,244.
    /// - Church, mosque, bell tower, minaret, station, two sheds and the water tank: 8 stand-ins, about 400.
    /// - Total about 56,600 of 72,000.
    ///
    /// The church, mosque, towers, station, sheds and tank have no models (tiles:lydda_town/...), so they draw stand-ins.
    /// The bell tower and minaret are Pine spires (the minaret slimmer and paler) because a Box stand-in cannot be taller
    /// than it is wide; the water tank is a Palm (a shaft under a wide top).</summary>
    [HexPrototype("lydda_town", Kind = "town", Region = "centre")]
    public sealed class LyddaTown : IHexPrototype
    {
        public string Title => "Lydda, old town and railway junction (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        // Measured 99,871 triangles worst case with the buildings drawn as the shipped b*-v2 town models at full
        // level (about 6,000 each, HexModels.TownAliases); the budget is that plus 10%. The doc comment's estimate predates them.
        public int TriangleBudget => 110000;

        // Explicit ids: kit_jaffa, kit_jerusalem_4 and kit_stone_village are the mosque-like b17 mesh.
        static readonly string[] Houses = { "sandbox:kit_jerusalem", "sandbox:kit_stone_village_2", "sandbox:kit_jerusalem_2", "sandbox:kit_jaffa_2" };
        const string Olive = "sandbox:olive_lite", Church = "tiles:lydda_town/st_george_church", BellTower = "tiles:lydda_town/bell_tower",
            Mosque = "tiles:lydda_town/mosque", Minaret = "tiles:lydda_town/minaret", Station = "tiles:lydda_town/station",
            Shed = "tiles:lydda_town/goods_shed", Tank = "tiles:lydda_town/water_tank";

        static readonly Color Stone = new Color(0.82f, 0.76f, 0.64f), ChurchStone = new Color(0.78f, 0.72f, 0.60f),
            PaleStone = new Color(0.88f, 0.84f, 0.74f), StationStone = new Color(0.80f, 0.70f, 0.56f),
            ShedRoof = new Color(0.55f, 0.50f, 0.45f), Iron = new Color(0.35f, 0.36f, 0.36f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

        const int MaxHouses = 15, MaxOlives = 22;
        /// <summary>The track lines (x, shares of S, in the tile frame); the yard lies west of the first.</summary>
        static readonly float[] Tracks = { -0.50f, -0.56f, -0.62f };

        static float Edge(float bearing) => (0.8660254f - HexScale.InsetShare) / Mathf.Cos(Mathf.Deg2Rad * (Mathf.Repeat(bearing, 60f) - 30f));

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(-8f, 8f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 P(float x, float z) => ctx.Centre + rot * new Vector3(x * s, 0f, z * s);
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 p, float r) { foreach (var t in taken) if ((t.at - p).magnitude < t.r + r) return false; return true; }
            void Take(Vector3 p, float widthShare) => taken.Add((p, 0.5f * widthShare * HexScale.PieceScale * s));

            // 1. Ground: beaten earth over the core, the square at the centre.
            ctx.PaintGround("dirt", P(0.05f, 0f), yaw, 1.0f * s, 1.0f * s, 0.45f, 0.35f);
            ctx.PaintGround("dirt", ctx.Centre, yaw + 45f, 0.24f * s, 0.24f * s, 0.3f, 0.9f);
            ctx.PaintStroke("dirt", ctx.Centre, P(-0.40f, 0f), 0.05f * s, 0.85f);      // the lane to the station

            // 2. The railway: a ballast bed, three tracks north to south, a branch to the south-east, the station on the town
            //    side, two goods sheds beyond the tracks, a water tank by the line.
            ctx.PaintGround("dirt", P(-0.56f, 0f), yaw, 0.20f * s, 1.9f * s, 0.25f, 0.6f);
            foreach (float x in Tracks) ctx.PaintStroke("dirt", P(x, -0.95f), P(x, 0.95f), 0.022f * s, 1f);
            ctx.PaintStroke("dirt", P(-0.50f, -0.30f), P(-0.26f, -0.92f), 0.022f * s, 1f);
            var station = P(-0.40f, 0f);
            ctx.PlaceByWidth(Station, station, yaw + 90f, 0.13f * s, StandIn.Box, StationStone);
            Take(station, 0.13f);
            foreach (float z in new[] { -0.20f, 0.20f })
            {
                var shed = P(-0.715f, z);
                ctx.PlaceByWidth(Shed, shed, yaw, 0.095f * s, StandIn.Box, ShedRoof);
                Take(shed, 0.095f);
            }
            var tank = P(-0.40f, 0.30f);
            ctx.PlaceByHeight(Tank, tank, yaw, 0.15f * s, StandIn.Palm, Iron, 0.35f);
            taken.Add((tank, 0.05f * s));

            // 3. The church and the mosque side by side north of the square: the church block with its bell tower at the
            //    west end, the smaller mosque block east of it with its minaret.
            var church = P(-0.04f, 0.34f);
            var mosque = P(0.13f, 0.34f);
            ctx.PlaceByWidth(Church, church, yaw + 180f, 0.15f * s, StandIn.Box, ChurchStone);
            ctx.PlaceByHeight(BellTower, P(-0.12f, 0.34f), yaw, 0.19f * s, StandIn.Pine, ChurchStone, 0.15f);
            ctx.PlaceByWidth(Mosque, mosque, yaw + 180f, 0.10f * s, StandIn.Box, Stone);
            ctx.PlaceByHeight(Minaret, P(0.19f, 0.37f), yaw, 0.17f * s, StandIn.Pine, PaleStone, 0.12f);
            Take(church, 0.15f);
            Take(mosque, 0.10f);
            taken.Add((P(0.19f, 0.37f), 0.035f * s));   // the minaret, at the mosque's east end

            // 4. The old core: an inner ring of 6 and an outer ring of 12 lots round the square, each house turned to face it;
            //    lots that would hit the church, the mosque or the station are skipped.
            int houses = 0;
            foreach (var (ring, count) in new[] { (0.29f, 6), (0.51f, 12) })
                for (int k = 0; k < count; k++)
                {
                    float b = yaw + 30f + 360f / count * k + ctx.Range(-5f, 5f), w = ctx.Range(0.12f, 0.13f);
                    var at = ctx.At(b, ring + ctx.Range(-0.01f, 0.01f));
                    float r = 0.5f * w * HexScale.PieceScale * s;
                    if (houses >= MaxHouses || !Free(at, r)) continue;
                    if (!ctx.PlaceByWidth(Houses[houses % Houses.Length], at, b + 180f + ctx.Range(-10f, 10f), w * s, StandIn.Box, Stone)) continue;
                    houses++;
                    taken.Add((at, r));
                }

            // 5. Olive groves east of the line: two staggered rings on tilled soil, kept off the houses and the yard.
            int olives = 0;
            foreach (var (ring, count, offset) in new[] { (0.68f, 16, 0f), (0.84f, 16, 11.25f) })
                for (int k = 0; k < count; k++)
                {
                    float b = yaw + offset + 360f / count * k;
                    float r = Mathf.Min(ring, Edge(b) - 0.09f) + ctx.Range(-0.01f, 0.01f);
                    var at = ctx.At(b, r);
                    var local = Quaternion.Euler(0f, -yaw, 0f) * (at - ctx.Centre) / s;
                    if (local.x < -0.38f || olives >= MaxOlives || !Free(at, 0.06f * s)) continue;
                    ctx.PaintGround("ploughed", at, b, 0.15f * s, 0.13f * s, 0.3f, 0.75f);
                    if (ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, OliveLeaf, 0.5f)) olives++;
                }
        }
    }
}
