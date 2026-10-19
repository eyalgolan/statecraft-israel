#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Netanya in 1948: a young, small town on the sea cliff of the Sharon coast, founded on 18 February 1929 by
    /// members of the Bnei Binyamin association (https://en.wikipedia.org/wiki/Netanya, checked 2026-10-19). Small and
    /// open: a town, not a city.
    /// - The sea: the tile looks for the shore with ctx.IsWater on the western bearings and lays everything out facing
    ///   the nearest water. With no water on the hex (the flat test surface) it assumes the sea to the west.
    /// - Along the cliff top runs a promenade, a painted walk with date palms, and in its middle a small paved square
    ///   facing the sea. Below the cliff the beach is painted sand.
    /// - A main street runs inland from the cliff square to a small plaza at the hex centre, kept open for a unit.
    /// - Two-storey houses (kit_white_city 1 to 4, kit_moshava_2) stand on an open grid either side of it.
    /// - Citrus groves in rows on tilled soil on the inland side, and sand dunes painted at the north and south edges.
    /// - The kurkar (sandstone) cliff and the early groves are general landscape here; neither is sourced as a fact.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 16 houses (kit_*, up to 1,400 each) = 22,400.
    /// - At most 14 citrus (citrus_lite, 1,602 each) = 22,428.
    /// - At most 10 date palms (284 each) = 2,840.
    /// - Two townspeople (up to 4,000 each) = 8,000.
    /// - Total about 55,700 of 62,000.</summary>
    [HexPrototype("netanya_town", Kind = "town", Region = "centre")]
    public sealed class NetanyaTown : IHexPrototype
    {
        public string Title => "Netanya, town on the sea cliff (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 62000;

        static readonly string[] Houses = { "sandbox:kit_white_city", "sandbox:kit_moshava_2", "sandbox:kit_white_city_2", "sandbox:kit_white_city_3", "sandbox:kit_white_city_4" };
        static readonly string[] People = { "proto:figure-sabra-pioneer-c04-1948-v2", "proto:figure-woman-blue-dress-c10-1948-v2" };
        const string Citrus = "sandbox:citrus_lite", Palm = "sandbox:date_palm";

        static readonly Color Plaster = new Color(0.94f, 0.92f, 0.86f), CitrusLeaf = new Color(0.16f, 0.34f, 0.14f);

        const int MaxHouses = 16, MaxCitrus = 14, MaxPalms = 10;

        static float Edge(float bearing) => (0.8660254f - HexScale.InsetShare) / Mathf.Cos(Mathf.Deg2Rad * (Mathf.Repeat(bearing, 60f) - 30f));

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 0. Find the sea: on the western bearings, the nearest water along a ray. Default: west, cliff at 0.62 S.
            //    Bearings that tie with the nearest are averaged, so a straight coast gives its true normal.
            float seaB = 270f, waterR = 9f;
            var hit = new float[19];
            for (int i = 0; i < hit.Length; i++)
            {
                hit[i] = 9f;
                for (float r = 0.3f; r <= 0.95f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(180f + 10f * i, r))) { hit[i] = r; break; }
                waterR = Mathf.Min(waterR, hit[i]);
            }
            if (waterR < 9f)
            {
                float sum = 0f; int n = 0;
                for (int i = 0; i < hit.Length; i++) if (hit[i] <= waterR + 0.015f) { sum += 180f + 10f * i; n++; }
                seaB = sum / n;
            }
            float cliff = waterR < 9f ? Mathf.Min(waterR - 0.08f, Edge(seaB) - 0.06f) : 0.62f;
            var toSea = Quaternion.Euler(0f, seaB, 0f) * Vector3.forward;
            var along = Quaternion.Euler(0f, seaB + 90f, 0f) * Vector3.forward;
            // A point in the coast's frame, shares of S: u toward the sea, v along the coast.
            Vector3 L(float u, float v) => ctx.Centre + (toSea * u + along * v) * s;
            float gridYaw = seaB;
            int palms = 0;
            void PalmAt(float u, float v)
            {
                if (palms < MaxPalms && ctx.PlaceByHeight(Palm, L(u, v), ctx.Range(0f, 360f), ctx.Range(0.11f, 0.12f) * s, StandIn.Palm, null, 0.3f)) palms++;
            }

            // 1. The shore: sand from the cliff foot to the sea, dunes at the north and south ends.
            ctx.PaintGround("beach", L(cliff + 0.22f, 0f), gridYaw, 0.40f * s, 2.0f * s, 0.25f, 0.9f);
            foreach (float v in new[] { -0.80f, 0.80f })
                ctx.PaintGround("beach", L(cliff - 0.22f, v), gridYaw + 20f * Mathf.Sign(v), 0.34f * s, 0.22f * s, 0.5f, 0.7f);

            // 2. The cliff-top promenade with palms, and the square facing the sea in its middle.
            ctx.PaintStroke("dirt", L(cliff - 0.02f, -0.85f), L(cliff - 0.02f, 0.85f), 0.04f * s, 0.85f);
            var square = L(cliff - 0.12f, 0f);
            ctx.PaintGround("beach", square, gridYaw, 0.18f * s, 0.20f * s, 0.25f, 0.9f);
            foreach (float v in new[] { -0.55f, -0.35f, 0.35f, 0.55f }) PalmAt(cliff - 0.07f, v);
            foreach (float v in new[] { -0.12f, 0.12f }) PalmAt(cliff - 0.06f, v);

            // 3. The main street inland from the square to the plaza at the centre, and the plaza.
            ctx.PaintStroke("dirt", L(cliff - 0.21f, 0f), L(-0.95f, 0f), 0.05f * s, 0.9f);
            ctx.PaintGround("beach", ctx.Centre, gridYaw + 45f, 0.22f * s, 0.22f * s, 0.3f, 0.8f);

            // 4. Houses on an open grid, 0.22 S apart, off the main street, the plaza and the cliff top; a yard under each.
            int houses = 0;
            foreach (float u in new[] { -0.44f, -0.22f, 0f, 0.22f, 0.44f })
                foreach (float v in new[] { -0.46f, -0.23f, 0.23f, 0.46f })
                {
                    float w = ctx.Range(0.12f, 0.13f);
                    int turn = ctx.Rng.Next(4);
                    if (u > cliff - 0.25f || (u == 0f && Mathf.Abs(v) < 0.3f)) continue;
                    var at = L(u, v);
                    if (houses >= MaxHouses || !ctx.PlaceByWidth(Houses[houses % Houses.Length], at, gridYaw + 90f * turn, w * s, StandIn.Box, Plaster)) continue;
                    houses++;
                    ctx.PaintGround("beach", at, gridYaw, 0.20f * s, 0.20f * s, 0.35f, 0.45f);
                }

            // 5. Citrus groves inland, in rows on tilled soil.
            ctx.PaintGround("ploughed", L(-0.66f, 0f), gridYaw, 0.24f * s, 1.3f * s, 0.25f, 0.7f);
            int citrus = 0;
            foreach (float u in new[] { -0.60f, -0.71f })
                for (float v = -0.52f; v <= 0.53f; v += 0.13f)
                {
                    if (Mathf.Abs(v) < 0.05f) continue;   // the main street
                    if (citrus < MaxCitrus && ctx.PlaceByHeight(Citrus, L(u, v + (u < -0.65f ? 0.065f : 0f)), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, CitrusLeaf, 0.5f))
                        citrus++;
                }

            // 6. Life: two townspeople on the cliff square.
            for (int k = 0; k < People.Length; k++)
                ctx.PlaceByHeight(People[k], L(cliff - 0.12f, -0.04f + 0.08f * k), seaB + ctx.Range(-30f, 30f), 0.075f * s, StandIn.Figure, null, 0.4f);
        }
    }
}
