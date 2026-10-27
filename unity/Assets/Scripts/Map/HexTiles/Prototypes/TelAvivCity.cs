#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>REFERENCE PROTOTYPE: copy this file to start a new hex (docs/hex-tiles/README.md).
    /// Tel Aviv 1948, the White City core, built Civ-style around one square: the White City landmark on a round plaza
    /// ringed by palms (Dizengoff Square), four palm-lined avenues out to the edge, an inner ring of Bauhaus buildings
    /// facing the square and an outer ring on the city's grid (-20 degrees, turned to the coast), garden trees between,
    /// townspeople on the square, a horse wagon on an avenue, and a palm promenade along the shore. Models are the
    /// 13 October Tripo set (local, pending rights); every piece has a stand-in.
    /// Budget: this showcase tile is drawn at full quality (about 150k triangles with the shipped b*-v2 buildings at full
    /// detail); a lighter level that keeps its UV seams comes before any map rollout.</summary>
    [HexPrototype("tel_aviv_city", Kind = "city")]
    public class TelAvivCity : IHexPrototype
    {
        public virtual string Title => "Tel Aviv, White City (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 165000;   // the shipped b*-v2 buildings at full detail (Tal, 19 Oct 2026): measured 149k on the israel board and the test hex

        const string Hero = "sandbox:hq_white_city_hero", Building = "sandbox:hq_white_city*", Palm = "sandbox:date_palm",
            GardenTree = "sandbox:carob_lite", Wagon = "proto:figure-vehicle-horse-wagon-1948-v2";
        static readonly string[] People =
        {
            "proto:figure-townsman-c02-1948-v2", "proto:figure-woman-blue-dress-c10-1948-v2", "proto:figure-nurse-c13-1948-v2",
            "proto:figure-arriving-man-c01-1948-v2", "proto:figure-sabra-pioneer-c04-1948-v2",
        };
        static readonly Color Plaster = new Color(0.95f, 0.93f, 0.88f);

        const float GridYaw = -20f;

        /// <summary>The outer ring's k-th building and its footprint width; a variant may swap in other models here.</summary>
        protected virtual (string model, float width) OuterBuilding(int k, HexTileContext ctx) => (Building, ctx.Range(0.15f, 0.17f) * ctx.S);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            // Bearings are clockwise from north; the avenues follow the city grid.
            float[] avenues = { GridYaw, GridYaw + 90f, GridYaw + 180f, GridYaw + 270f };
            bool OnAvenue(float bearing, float clearDeg) { foreach (var a in avenues) if (Mathf.Abs(Mathf.DeltaAngle(bearing, a)) < clearDeg) return true; return false; }

            // 1. The square: an octagonal plaza (two crossed squares), the landmark on it, a ring of palms round its edge.
            float plaza = 0.42f * s;
            // Painted ground, not flat boxes (Tal, 19 Oct): pale sand paving for the square, the yards and the promenade.
            ctx.PaintGround("beach", ctx.Centre, GridYaw, 0.95f * s, 0.95f * s, 0.45f, 0.4f);   // the town's lighter ground
            ctx.PaintGround("beach", ctx.Centre, GridYaw, plaza, plaza, 0.25f);
            ctx.PaintGround("beach", ctx.Centre, GridYaw + 45f, plaza, plaza, 0.25f);
            ctx.PlaceByWidth(Hero, ctx.Centre, GridYaw + 180f, 0.24f * s, StandIn.Box, Plaster);
            for (int k = 0; k < 10; k++)
            {
                float b = GridYaw + 18f + 36f * k;
                if (!OnAvenue(b, 8f)) ctx.PlaceByHeight(Palm, ctx.At(b, 0.19f), b, 0.12f * s, StandIn.Palm, null, 0.3f);
            }

            // 2. Avenues out to the edge, palms on both sides.
            foreach (var a in avenues) ctx.PaintStroke("dirt", ctx.At(a, 0.23f), ctx.At(a, 0.88f), 0.05f * s);
            foreach (var a in avenues)
                for (float r = 0.26f; r <= 0.86f; r += 0.06f)
                {
                    if (r > 0.3f && Mathf.Repeat(r, 0.12f) < 0.06f)
                        foreach (float side in new[] { -90f, 90f })
                            ctx.PlaceByHeight(Palm, ctx.At(a, r) + Quaternion.Euler(0f, a + side, 0f) * Vector3.forward * (0.045f * s), 0f, 0.11f * s, StandIn.Palm, null, 0.3f);
                }

            // 3. Inner ring: eight buildings facing the square, between the avenues. Outer ring: twelve on the grid.
            for (int k = 0; k < 8; k++)
            {
                float b = GridYaw + 22.5f + 45f * k;
                var at = ctx.At(b, 0.41f);
                ctx.PaintGround("beach", at, b, 0.2f * s, 0.2f * s, 0.3f, 0.8f);
                ctx.PlaceByWidth(Building, at, b + 180f, ctx.Range(0.16f, 0.18f) * s, StandIn.Box, Plaster);
            }
            for (int k = 0; k < 12; k++)
            {
                float b = GridYaw + 15f + 30f * k;
                if (OnAvenue(b, 10f)) b += 10f;
                var at = ctx.At(b, 0.67f + ctx.Range(-0.03f, 0.03f));
                var (model, width) = OuterBuilding(k, ctx);
                if (!ctx.PlaceByWidth(model, at, GridYaw + 90f * ctx.Rng.Next(4), width, StandIn.Box, Plaster)) continue;
                ctx.PaintGround("beach", at, GridYaw, width * 1.15f, width * 1.15f, 0.3f, 0.8f);
                // A garden tree in the gap after each outer building.
                ctx.PlaceByHeight(GardenTree, ctx.At(b + 15f, 0.67f), ctx.Range(0f, 360f), 0.13f * s, StandIn.Tree, null, 0.5f);
            }

            // 4. Life: townspeople on the square in two small groups, a horse wagon coming in along the north avenue.
            for (int k = 0; k < People.Length; k++)
            {
                float b = GridYaw + 200f + 22f * k + (k > 2 ? 60f : 0f);
                ctx.PlaceByHeight(People[k], ctx.At(b, 0.15f), b + 160f + ctx.Range(-30f, 30f), 0.075f * s, StandIn.Figure, null, 0.4f);
            }
            ctx.PlaceByWidth(Wagon, ctx.At(avenues[0], 0.5f), avenues[0] + 180f, 0.1f * s, StandIn.Box, new Color(0.45f, 0.33f, 0.22f));

            // 5. The shore: where a bearing reaches the sea, a promenade strip and a palm just short of the water.
            for (float b = 180f; b <= 360f; b += 14f)
                for (float r = 0.3f; r <= 0.9f; r += 0.04f)
                {
                    if (!ctx.IsWater(ctx.At(b, r + 0.06f))) continue;
                    ctx.PaintGround("beach", ctx.At(b, r), b, 0.07f * s, 0.06f * s, 0.3f);
                    ctx.PlaceByHeight(Palm, ctx.At(b, r - 0.04f), 0f, 0.11f * s, StandIn.Palm, null, 0.3f);
                    break;
                }
        }
    }
}
