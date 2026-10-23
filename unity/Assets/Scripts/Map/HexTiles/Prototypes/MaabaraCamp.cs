#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A ma'abara (immigrant transit camp) of 1950 to 1951 on flat plains. The camps date from 1950: the first
    /// one "became functional in May 1950". Source: https://en.wikipedia.org/wiki/Ma%27abarot (checked 2026-10-19).
    /// This tile is not placed on the 1948 board by any recipe. It is a painter tile for 1950 on.
    ///
    /// The same article says the early camps had a tent for each family, then hut-shaped canvas tents, then tin or wooden
    /// shacks, and that water came from a few shared taps. The tile draws that calmly, with dignity and no caricature:
    /// - Rows of family dwellings on a dirt-lane grid, turned a few degrees, in a diamond of at most 22. Tin huts
    ///   (tiles:maabara_camp/tin_hut, a grey Box) and canvas tents (tiles:maabara_camp/tent, a canvas Rock stand-in,
    ///   low and sloped) alternate by row. Each is 0.12 S, the smallest building size.
    /// - The proto:maabara-camp-v1 cluster takes two dwelling slots in one quadrant.
    /// - Two communal sheds (tiles:maabara_camp/shed) take two slots in the opposite quadrant. That makes at most 25
    ///   buildings.
    /// - The centre is an open square where the cross lane meets the rows, kept clear for a unit. North of it, on the
    ///   cross lane, stands the water point: a standpipe (tiles:maabara_camp/water_tap) and a small tank
    ///   (tiles:maabara_camp/water_tank) on wet ground.
    /// - Three laundry lines hang along the lanes (tiles:maabara_camp/laundry_post with tiles:maabara_camp/laundry stand-ins).
    /// - Four people as life, two at the water point and two at the square's edge, outside the unit spot: an elder, a woman,
    ///   a child and a Yemenite immigrant.
    ///
    /// Triangle estimate at the maximum counts (FlatSurface refuses nothing):
    /// - The maabara-camp-v1 cluster, assumed 10,000.
    /// - 4 figures (proto:figure-elder-immigrant-v1, figure-immigrant-woman-v1, figure-immigrant-child-v1,
    ///   figure-yemenite-immigrant-v1), assumed 4,000 each = 16,000.
    /// - Stand-ins: at most 22 dwellings, 2 sheds, the tap, the tank, 6 laundry posts and 9 laundry pieces, so 41 x 60 =
    ///   2,460.
    /// - Total about 28,500 of 32,000.</summary>
    [HexPrototype("maabara_camp", Kind = "village")]
    public sealed class MaabaraCamp : IHexPrototype
    {
        public string Title => "Ma'abara transit camp (1950 to 1951)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 32000;

        const string Cluster = "proto:maabara-camp-v1", TinHut = "tiles:maabara_camp/tin_hut", Tent = "tiles:maabara_camp/tent",
            Shed = "tiles:maabara_camp/shed", Tap = "tiles:maabara_camp/water_tap", Tank = "tiles:maabara_camp/water_tank",
            LaundryPost = "tiles:maabara_camp/laundry_post", Laundry = "tiles:maabara_camp/laundry";
        static readonly string[] People =
        {
            "proto:figure-elder-immigrant-v1", "proto:figure-immigrant-woman-v1", "proto:figure-immigrant-child-v1",
            "proto:figure-yemenite-immigrant-v1",
        };
        const int MaxDwellings = 22;

        static readonly Color Tin = new Color(0.62f, 0.64f, 0.65f), TinDark = new Color(0.52f, 0.54f, 0.55f),
            Canvas = new Color(0.80f, 0.76f, 0.64f), ShedWood = new Color(0.55f, 0.47f, 0.38f),
            TapGrey = new Color(0.45f, 0.47f, 0.48f), PostWood = new Color(0.45f, 0.36f, 0.27f);
        static readonly Color[] Cloth = { new Color(0.92f, 0.91f, 0.88f), new Color(0.62f, 0.70f, 0.80f), new Color(0.82f, 0.72f, 0.60f) };

        // Dwellings stand in columns every Col (x) and rows every Row (z), shares of S. There is no column 0: the cross lane
        // runs there.
        const float Col = 0.23f, Row = 0.25f, DwellingW = 0.12f;

        /// <summary>The diamond of dwelling slots: columns +-1 take rows -3..3, columns +-2 rows -2..2, columns +-3 rows -1..1.</summary>
        static bool InCamp(int j, int k) => j != 0 && Mathf.Abs(j) <= 3 && Mathf.Abs(k) <= 4 - Mathf.Abs(j);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float yaw = ctx.Range(-12f, 12f);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Vector3 G(float x, float z) => ctx.Centre + turn * new Vector3(x * s, 0f, z * s);
            // The quadrant the cluster stands in; the sheds take the opposite one.
            int sx = ctx.Rng.Next(2) == 0 ? -1 : 1, sz = ctx.Rng.Next(2) == 0 ? -1 : 1;

            // 1. Ground: the camp trodden to dirt at half strength, then the lanes between the rows and the cross lane.
            ctx.PaintGround("dirt", ctx.Centre, yaw, 1.4f * s, 1.4f * s, 0.35f, 0.45f);
            for (int k = -3; k <= 2; k++)
                ctx.PaintStroke("dirt", G(-0.80f, (k + 0.5f) * Row), G(0.80f, (k + 0.5f) * Row), 0.04f * s, 0.9f);
            ctx.PaintStroke("dirt", G(0f, -0.85f), G(0f, 0.85f), 0.05f * s, 0.9f);

            // 2. The dwellings, row by row. The cluster's slots (column 2, rows 1 and 2, in its quadrant), the sheds' slots
            //    (column -2, rows -1 and -2) and the open square about the centre are left out.
            int dwellings = 0;
            float half = 0.5f * DwellingW * HexScale.PieceScale * s;
            for (int k = 3; k >= -3; k--)
                for (int j = -3; j <= 3; j++)
                {
                    if (!InCamp(j, k)) continue;
                    if (j * sx == 2 && (k * sz == 1 || k * sz == 2)) continue;     // the cluster
                    if (j * sx == -2 && (k * sz == -1 || k * sz == -2)) continue;  // the sheds
                    var at = G(j * Col, k * Row);
                    if (ctx.InUnitSpot(at, half) || dwellings >= MaxDwellings) continue;
                    bool tinRow = (k & 1) == 0;
                    bool placed = tinRow
                        ? ctx.PlaceByWidth(TinHut, at, yaw + 90f * ctx.Rng.Next(2), DwellingW * s, StandIn.Box, ctx.Rng.Next(3) == 0 ? TinDark : Tin)
                        : ctx.PlaceByWidth(Tent, at, yaw, DwellingW * s, StandIn.Rock, Canvas);
                    if (placed) dwellings++;
                }

            // 3. The cluster across its two slots, and the two communal sheds.
            ctx.PlaceByWidth(Cluster, G(sx * 2f * Col, sz * 1.5f * Row), yaw + 180f, 0.19f * s, StandIn.Box, Tin);
            foreach (int k in new[] { 1, 2 })
                ctx.PlaceByWidth(Shed, G(-sx * 2f * Col, -sz * k * Row), yaw, 0.14f * s, StandIn.Box, ShedWood);

            // 4. The water point on the cross lane north of the square: wet ground, a standpipe, a small tank beside it.
            float wz = 1.5f * Row;
            ctx.PaintGround("dirt", G(0f, wz), yaw, 0.12f * s, 0.07f * s, 0.4f, 1f);
            ctx.PlaceByHeight(Tap, G(-0.04f, wz), yaw, 0.05f * s, StandIn.Pine, TapGrey, 0.3f);
            ctx.PlaceByWidth(Tank, G(0.05f, wz), yaw, 0.05f * s, StandIn.Box, TapGrey);

            // 5. Laundry lines along three lanes, each two posts with three hanging pieces between, a little off the lane's
            //    middle and short of the huts on either side. They take lanes beside dwellings, not the cluster or sheds.
            (float x, float k)[] lines = { (-sx * 2f * Col, sz * 1.5f), (sx * 2f * Col, -sz * 1.5f), (-sx * 1f * Col, sz * 2.5f) };
            foreach (var line in lines)
            {
                float z = line.k * Row + 0.02f;
                foreach (float dx in new[] { -0.06f, 0.06f })
                    ctx.PlaceByHeight(LaundryPost, G(line.x + dx, z), yaw, 0.04f * s, StandIn.Pine, PostWood, 0.3f);
                for (int c = 0; c < 3; c++)
                    ctx.PlaceByWidth(Laundry, G(line.x + (c - 1) * 0.035f, z), yaw, 0.018f * s, StandIn.Box, Cloth[c]);
            }

            // 6. Life: two people at the water point, two at the square's edge just outside the unit spot; four at most.
            Vector3[] spots = { G(-0.10f, wz - 0.04f), G(0.12f, wz - 0.03f), G(-0.31f, -0.06f), G(0.28f, -0.15f) };
            for (int k = 0; k < People.Length; k++)
            {
                float h = k == 2 ? 0.05f : 0.07f;
                ctx.PlaceByHeight(People[k], spots[k], yaw + ctx.Range(0f, 360f), h * s, StandIn.Figure, null, 0.4f);
            }
        }
    }
}
