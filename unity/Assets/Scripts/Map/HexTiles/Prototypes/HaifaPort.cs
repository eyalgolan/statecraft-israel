#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Haifa's lower town and port (1948), drawn Civ-style as a city-centre hex. Designed for board hex 5,8: flat,
    /// with the sea to the north and west and the Carmel hills hex to the south.
    /// - A small paved square at the centre, ringed by four palms and kept open for a unit.
    /// - The port along the north and west shore. Each bearing finds its shore with ctx.IsWater (with no water, the hex
    ///   edge stands in for it). Along it run a line of breakwater blocks on the last dry ground, a painted quay apron, two
    ///   cargo sheds (kit_port) on the west quays and the Haifa port cluster (proto:haifa-port-city-v1) on the north quay.
    ///   The deep-water harbour was opened on 31 October 1933 (https://en.wikipedia.org/wiki/Haifa_Port, checked 2026-10-19).
    /// - A railway strip runs east from the north quay past a generic station block toward the bay. Haifa East station opened
    ///   in 1904 next to the port (https://en.wikipedia.org/wiki/Haifa_East_railway_station, checked 2026-10-19).
    /// - The stone-balcony downtown: a ring of kit_haifa / kit_haifa_2 buildings round the square, on the port side.
    /// - Hadar HaCarmel on the slope to the south-east: two terraced rows of buildings with a painted street along each,
    ///   rising toward the south edge, with Carmel pines above them
    ///   (https://en.wikipedia.org/wiki/Hadar_HaCarmel, checked 2026-10-19).
    /// - The German Colony: one avenue of Templer houses (kit_moshava, kit_moshava_2) lined with shade trees, running from
    ///   the west shore up toward the Carmel at the south-west. The Templers founded the colony in 1868 as a garden city on
    ///   a main street that ran down to the harbour (https://en.wikipedia.org/wiki/German_Colony,_Haifa, checked 2026-10-19).
    /// - At the avenue's upper end, low-key: the Shrine of the Báb as it stood in 1948, a small stone mausoleum (1909, rooms
    ///   added 1929) in a garden with two cypresses. No arcade, no dome and no terraces: the superstructure was begun in
    ///   1949 and finished in 1953, and the monumental terraces came decades later
    ///   (https://en.wikipedia.org/wiki/Shrine_of_the_B%C3%A1b and
    ///   https://en.wikipedia.org/wiki/Bah%C3%A1%CA%BC%C3%AD_World_Centre_buildings, checked 2026-10-19; the garden round the
    ///   original shrine: https://bahai-library.com/giachery_shoghi_effendi_recollections&amp;chapter=8, checked 2026-10-19).
    ///
    /// Triangle estimate, at the maximum attempted counts (an upper bound; FlatSurface refuses only what leaves the hex):
    /// - Downtown 5 + Hadar 9 + Templer houses 6 = 20 kit buildings x 1,400 = 28,000.
    /// - Two cargo sheds (kit_port) x 1,400 = 2,800. The port cluster: 10,000.
    /// - 4 palms x 280 = 1,120. 4 avenue carobs x 650 = 2,600. 2 shrine cypresses and 3 Carmel pines x 1,600 = 8,000.
    /// - 2 townspeople x 4,000 = 8,000.
    /// - At most 48 breakwater blocks, the station and the shrine as stand-ins, about 650. 2 rail flats: 24.
    /// - Total about 61,200 of 70,000.
    ///
    /// The breakwater, station and shrine have no models (tiles:haifa_port/...), so they always draw stand-ins.</summary>
    [HexPrototype("haifa_port", Kind = "city", Region = "north")]
    public sealed class HaifaPort : IHexPrototype
    {
        public string Title => "Haifa, the port and lower town (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 70000;

        // Explicit kit ids: kit_haifa_3 is the moshava farmhouse mesh and kit_moshava_3 is a barn.
        static readonly string[] Downtown = { "sandbox:kit_haifa", "sandbox:kit_haifa_2" };
        static readonly string[] Templer = { "sandbox:kit_moshava", "sandbox:kit_moshava_2" };
        static readonly string[] People = { "proto:figure-townsman-c02-1948-v2", "proto:figure-woman-blue-dress-c10-1948-v2" };
        const string Shed = "sandbox:kit_port", PortCluster = "proto:haifa-port-city-v1", Palm = "sandbox:date_palm",
            ShadeTree = "sandbox:carob_lite", Cypress = "sandbox:cypress_lite", Pine = "sandbox:aleppo_pine_lite",
            Breakwater = "tiles:haifa_port/breakwater", Station = "tiles:haifa_port/station", Shrine = "tiles:haifa_port/shrine_of_the_bab";

        static readonly Color Stone = new Color(0.86f, 0.81f, 0.70f), TemplerStone = new Color(0.84f, 0.78f, 0.66f),
            PortGrey = new Color(0.66f, 0.64f, 0.60f), Shrine1948 = new Color(0.88f, 0.84f, 0.74f), Rail = new Color(0.30f, 0.27f, 0.24f),
            Leaf = new Color(0.22f, 0.36f, 0.18f), CypressLeaf = new Color(0.14f, 0.27f, 0.14f), PineLeaf = new Color(0.20f, 0.33f, 0.18f);

        const int MaxBlocks = 48, MaxHadar = 9;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                return true;
            }
            // A building sized by width (metres), kept clear of every piece already placed. The clearance counts PieceScale.
            bool Building(string model, Vector3 at, float yaw, float w, StandIn standIn, Color tint)
            {
                float r = 0.6f * w * HexScale.PieceScale;
                if (!Free(at, r) || !ctx.PlaceByWidth(model, at, yaw, w, standIn, tint)) return false;
                taken.Add((at, r));
                return true;
            }
            bool Tree(string model, Vector3 at, float h, StandIn standIn, Color tint, float footprint)
            {
                float r = 0.3f * h * HexScale.PieceScale;
                if (!Free(at, r) || !ctx.PlaceByHeight(model, at, ctx.Range(0f, 360f), h, standIn, tint, footprint)) return false;
                taken.Add((at, r));
                return true;
            }
            // The shore on a bearing, as a share of S: the first wet point, or just past the hex edge when there is none.
            float Shore(float b)
            {
                float edge = Edge(b);
                for (float r = 0.3f; r <= edge; r += 0.02f) if (ctx.IsWater(ctx.At(b, r))) return r;
                return edge + 0.05f;
            }

            // 1. Ground: the town's beaten earth at low strength, the Carmel's hill ground along the south edge, and the
            //    paved square at the centre, ringed by four palms and kept open for a unit.
            ctx.PaintGround("dirt", ctx.Centre, 0f, 1.0f * s, 1.0f * s, 0.45f, 0.35f);
            ctx.PaintGround("hills", ctx.At(180f, 0.74f), 0f, 0.95f * s, 0.30f * s, 0.4f, 0.6f);
            ctx.PaintGround("beach", ctx.Centre, 0f, 0.34f * s, 0.34f * s, 0.25f);
            taken.Add((ctx.Centre, 0.17f * s));
            for (int k = 0; k < 4; k++)
                ctx.PlaceByHeight(Palm, ctx.At(45f + 90f * k, 0.165f), ctx.Range(0f, 360f), 0.12f * s, StandIn.Palm, null, 0.3f);

            // 2. The breakwater: blocks 0.05 S apart on the last dry ground of the north and west shore, alternately 0.04 and
            //    0.032 S so neighbours never share a face plane. With no water they trace the hex edge.
            int blocks = 0;
            for (float b = 250f; b <= 410f && blocks < MaxBlocks;)
            {
                float rs = Shore(b), r = rs - 0.075f;
                ctx.PlaceByWidth(Breakwater, ctx.At(b, r), b, (blocks % 2 == 0 ? 0.04f : 0.032f) * s, StandIn.Box, PortGrey);
                blocks++;
                b += Mathf.Rad2Deg * 0.05f / Mathf.Max(r, 0.3f);
            }

            // 3. The quays: a painted apron behind the breakwater, two cargo sheds on the west quays, the port cluster on the
            //    north quay (it tries a few spots and takes the first that is clear and dry).
            for (float b = 260f; b <= 400f; b += 20f)
                ctx.PaintGround("dirt", ctx.At(b, Shore(b) - 0.15f), b, 0.22f * s, 0.12f * s, 0.3f, 0.9f);
            foreach (float b in new[] { 296f, 324f })
                Building(Shed, ctx.At(b, Shore(b) - 0.19f), b, 0.12f * s, StandIn.Box, PortGrey);
            bool cluster = false;
            foreach (float b in new[] { 354f, 4f, 14f })
            {
                foreach (float back in new[] { 0.27f, 0.23f, 0.31f })
                    if (Building(PortCluster, ctx.At(b, Shore(b) - back), b + 180f, 0.20f * s, StandIn.Box, Stone)) { cluster = true; break; }
                if (cluster) break;
            }

            // 4. The railway east from the north quay toward the bay: a ballast stroke, two thin rail flats along it, and a
            //    generic station block beside it.
            Vector3 railA = ctx.At(20f, Shore(20f) - 0.30f), railE = ctx.At(88f, 0.95f);
            ctx.PaintStroke("dirt", railA, railE, 0.035f * s, 0.9f);
            Vector3 railEnd = ctx.At(85f, 0.76f);
            float railYaw = Bearing(railEnd - railA);
            foreach (var (t0, t1) in new[] { (0.02f, 0.46f), (0.52f, 0.96f) })
            {
                Vector3 a = Vector3.Lerp(railA, railEnd, t0), e = Vector3.Lerp(railA, railEnd, t1);
                ctx.PlaceFlat((a + e) * 0.5f, railYaw, 0.014f * s, Planar(e - a), Rail, 150f);
            }
            Building(Station, ctx.At(55f, Shore(55f) - 0.26f), railYaw, 0.11f * s, StandIn.Box, Stone);

            // 5. The downtown: five stone-balcony buildings round the square on the port side, each facing it.
            int k5 = 0;
            foreach (float b in new[] { 288f, 324f, 0f, 36f, 72f })
                Building(Downtown[k5++ % 2], ctx.At(b, 0.36f), b + 180f + ctx.Range(-6f, 6f), ctx.Range(0.125f, 0.135f) * s, StandIn.Box, Stone);

            // 6. The German Colony avenue, from the west shore up toward the Carmel at the south-west. Templer houses stand
            //    on both sides, turned square to the avenue, with shade trees in the gaps along its edges. The Shrine stands
            //    at the upper end in its garden.
            Vector3 avA = ctx.At(265f, 0.58f), avB = ctx.At(212f, 0.56f);
            Vector3 along = (avB - avA).normalized, across = new Vector3(along.z, 0f, -along.x);
            float avYaw = Bearing(along);
            ctx.PaintStroke("dirt", ctx.At(266f, Mathf.Min(Shore(266f), Edge(266f)) - 0.02f), avA, 0.04f * s, 0.9f);
            ctx.PaintStroke("dirt", avA, avB, 0.04f * s, 0.9f);
            var shrine = avB + along * (0.10f * s);
            ctx.PaintGround("grass", shrine + along * (0.02f * s), avYaw, 0.18f * s, 0.18f * s, 0.3f, 0.85f);
            if (Building(Shrine, shrine, avYaw + 180f, 0.08f * s, StandIn.Box, Shrine1948))
                foreach (float side in new[] { -1f, 1f })
                    ctx.PlaceByHeight(Cypress, shrine + along * (0.08f * s) + across * (side * 0.06f * s), ctx.Range(0f, 360f), 0.12f * s, StandIn.Pine, CypressLeaf, 0.3f);
            int k6 = 0;
            foreach (float t in new[] { 0.06f, 0.28f, 0.50f })
                foreach (float side in new[] { -1f, 1f })
                    Building(Templer[k6++ % 2], avA + along * (t * s) + across * (side * 0.14f * s), avYaw + (side > 0 ? 90f : -90f), 0.12f * s, StandIn.Box, TemplerStone);
            foreach (float t in new[] { 0.17f, 0.39f })
                foreach (float side in new[] { -1f, 1f })
                    Tree(ShadeTree, avA + along * (t * s) + across * (side * 0.04f * s), 0.12f * s, StandIn.Tree, Leaf, 0.5f);

            // 7. Hadar HaCarmel: two terraced rows on the slope to the south-east, each with its street painted along it,
            //    buildings narrower in the upper row, all facing down toward the port. Carmel pines above them.
            Arc(ctx, 0.43f, 104f, 200f);
            Arc(ctx, 0.57f, 110f, 204f);
            int hadar = 0;
            foreach (var (r, bearings, w) in new[] { (0.50f, new[] { 112f, 136f, 160f, 184f }, 0.125f), (0.64f, new[] { 118f, 137f, 156f, 175f, 194f }, 0.115f) })
                foreach (float b in bearings)
                {
                    if (hadar >= MaxHadar) break;
                    if (Building(Downtown[hadar % 2], ctx.At(b, r), b + 180f + ctx.Range(-4f, 4f), w * s, StandIn.Box, Stone)) hadar++;
                }
            foreach (var (b, r) in new[] { (165f, 0.78f), (180f, 0.82f), (195f, 0.79f) })
                Tree(Pine, ctx.At(b, r), ctx.Range(0.13f, 0.15f) * s, StandIn.Pine, PineLeaf, 0.4f);

            // 8. Life: two townspeople on the square, on the side toward the camera.
            for (int k = 0; k < People.Length; k++)
            {
                float b = 160f + 45f * k;
                ctx.PlaceByHeight(People[k], ctx.At(b, 0.10f), b + 180f + ctx.Range(-30f, 30f), 0.07f * s, StandIn.Figure, null, 0.4f);
            }
        }

        /// <summary>Paints a terrace street as chords along an arc about the centre, between two bearings.</summary>
        static void Arc(HexTileContext ctx, float r, float b0, float b1)
        {
            for (float b = b0; b < b1; b += 12f)
                ctx.PaintStroke("dirt", ctx.At(b, r), ctx.At(Mathf.Min(b + 12f, b1), r), 0.03f * ctx.S, 0.8f);
        }

        /// <summary>The inset hexagon's edge on a bearing, as a share of S (the flat sides face 30, 90, 150 ... degrees).</summary>
        static float Edge(float b)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(b, 30f + 60f * Mathf.Round((b - 30f) / 60f)));
            return (0.8660254f - HexScale.InsetShare) / Mathf.Cos(d * Mathf.Deg2Rad);
        }

        static float Bearing(Vector3 v) => Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;

        static float Planar(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
