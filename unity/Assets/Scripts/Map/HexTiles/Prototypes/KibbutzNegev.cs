#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A Negev outpost kibbutz of 1946 to 1948, one of the "11 points" set up on the night of 5 to 6 October
    /// 1946. Sources: https://en.wikipedia.org/wiki/11_points_in_the_Negev and
    /// https://he.wikipedia.org/wiki/11_%D7%94%D7%A0%D7%A7%D7%95%D7%93%D7%95%D7%AA (both checked 2026-10-19).
    /// The Hebrew article describes each point as a few huts brought in by truck and tents, a perimeter that was only a
    /// barbed-wire fence, and a wooden tower carrying a water barrel. The tile follows that, not a later concrete tower.
    /// - A small fenced compound stands in the north half of the hex, toward a flat side (bearing 30 or 330), 0.33 S out
    ///   at its nearest, so the unit spot stays open. Its perimeter is a ring of thin stand-in posts
    ///   (tiles:kibbutz_negev/fence_post). The gate faces the centre, and a dirt road runs from it through the centre.
    /// - Inside the fence: a dining hut (sandbox:kit_kibbutz_2) facing the gate, three prefab huts (sandbox:kit_negev_2 and _3, 0.12 S),
    ///   two small canvas tents (tiles:kibbutz_negev/tent, 0.055 S, as DesertEdge keeps its tents under the building size), and the wooden water tower as the signature
    ///   (tiles:kibbutz_negev/water_tower: a Palm stand-in reads as a frame with the barrel on top). No kibbutz is named.
    /// - The water line: work began in January 1947 on a line from the Nir Am area, and by October 1947 water reached the
    ///   southernmost points (the Hebrew article, checked 2026-10-19). It is a narrow painted dirt stroke from the back of
    ///   the compound out of the hex.
    /// - Small irrigated plots (green_crop paint) beside the compound and past the unit spot, against loess desert. Young
    ///   trees stand by the plots and the road: tabor oaks for eucalyptus, and tamarisk stand-ins. One kibbutz woman walks
    ///   by the plots. A few desert rocks lie out in the corners.
    /// - No trenches and no military pieces.
    ///
    /// Triangle estimate at the maximum counts (FlatSurface refuses nothing):
    /// - Dining hut and 3 huts x 1,400 = 5,600.
    /// - 4 young eucalyptus (sandbox:tabor_oak_lite) x 650 = 2,600.
    /// - 3 desert rocks x 700 = 2,100.
    /// - 1 figure (proto:figure-kibbutz-woman-v1, assumed 4,000) = 4,000.
    /// - Stand-ins: 30 fence posts, 2 tents, the tower and 4 tamarisks, about 37 x 60 = 2,220.
    /// - Total about 16,500 of 20,000.</summary>
    [HexPrototype("kibbutz_negev", Kind = "village", Region = "south")]
    public sealed class KibbutzNegev : IHexPrototype
    {
        public string Title => "Negev outpost kibbutz (1946 to 1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "desert";
        public int TriangleBudget => 20000;

        // Explicit ids: kit_kibbutz* would mix in the concrete water tower (kit_kibbutz_4), which this tile does not use.
        const string DiningHut = "sandbox:kit_kibbutz_2", Hut = "sandbox:kit_negev_2", Hut2 = "sandbox:kit_negev_3", Tent = "tiles:kibbutz_negev/tent",
            Tower = "tiles:kibbutz_negev/water_tower", Post = "tiles:kibbutz_negev/fence_post",
            Tamarisk = "tiles:kibbutz_negev/tamarisk", Eucalyptus = "sandbox:tabor_oak_lite", Rock = "sandbox:desert_rock*",
            Woman = "proto:figure-kibbutz-woman-v1";
        const int MaxPosts = 30, MaxEucalyptus = 4, MaxTamarisks = 4, MaxRocks = 3;

        static readonly Color HutTimber = new Color(0.62f, 0.52f, 0.40f), Canvas = new Color(0.80f, 0.75f, 0.62f),
            TowerTimber = new Color(0.45f, 0.34f, 0.24f), PostGrey = new Color(0.40f, 0.36f, 0.32f),
            Leaf = new Color(0.30f, 0.42f, 0.24f), TamariskGreen = new Color(0.50f, 0.56f, 0.42f),
            RockTan = new Color(0.70f, 0.58f, 0.44f);

        // The compound's half-extents across (U) and outward (V), and its centre's distance from the hex centre, shares of S.
        const float U = 0.34f, V = 0.19f, Out = 0.52f;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float bearing = (ctx.Rng.Next(2) == 0 ? -30f : 30f) + ctx.Range(-3f, 3f);
            var turn = Quaternion.Euler(0f, bearing, 0f);
            // The field frame (x across, z toward the compound, shares of S, about the hex centre) and the compound frame.
            Vector3 F(float x, float z) => ctx.Centre + turn * new Vector3(x * s, 0f, z * s);
            Vector3 L(float u, float v) => F(u, Out + v);

            // 1. Ground: loess desert to the rim, a few paler wind streaks, the compound yard trodden to dirt.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.75f * s, 2.0f * s, 0.2f, 0.75f);
            float wind = ctx.Range(0f, 180f);
            for (int k = 0; k < 4; k++)
            {
                var at = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.45f, 0.75f));
                ctx.PaintGround("beach", at, wind + ctx.Range(-10f, 10f), ctx.Range(0.25f, 0.40f) * s, ctx.Range(0.04f, 0.07f) * s, 0.5f, 0.55f);
            }
            ctx.PaintGround("dirt", L(0f, 0f), bearing, 2f * U * s, 2f * V * s, 0.2f, 0.7f);

            // 2. Irrigated plots: green against the desert, two flanking the compound and three past the unit spot.
            (float x, float z, float w, float d)[] plots =
            {
                (-0.56f, 0.30f, 0.16f, 0.22f), (0.56f, 0.30f, 0.16f, 0.22f),
                (-0.38f, -0.40f, 0.20f, 0.16f), (0f, -0.52f, 0.18f, 0.14f), (0.38f, -0.40f, 0.20f, 0.16f),
            };
            foreach (var p in plots) ctx.PaintGround("green_crop", F(p.x, p.z), bearing, p.w * s, p.d * s, 0.12f, 0.9f);

            // 3. The road from the gate through the hex centre to the far edge; the water line from the back of the compound
            //    out of the hex, a narrow straight stroke.
            ctx.PaintStroke("dirt", L(0f, -V), ctx.At(bearing + 180f, 0.95f), 0.035f * s, 0.9f);
            ctx.PaintStroke("dirt", L(0.26f, V), ctx.At(bearing + 22f, 1.0f), 0.016f * s, 0.95f);

            // 4. The fence: posts round a chamfered rectangle, every ~0.07 S, a gap at the gate.
            Vector2[] ring = { new Vector2(-U, V - 0.05f), new Vector2(-U + 0.05f, V), new Vector2(U - 0.05f, V), new Vector2(U, V - 0.05f),
                new Vector2(U, -V), new Vector2(-U, -V) };
            int posts = 0;
            for (int k = 0; k < ring.Length && posts < MaxPosts; k++)
            {
                Vector2 a = ring[k], b = ring[(k + 1) % ring.Length];
                int n = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / 0.07f));
                for (int i = 0; i < n && posts < MaxPosts; i++)
                {
                    var q = Vector2.Lerp(a, b, i / (float)n);
                    if (Mathf.Abs(q.y + V) < 0.01f && Mathf.Abs(q.x) < 0.05f) continue;   // the gate
                    if (ctx.PlaceByHeight(Post, L(q.x, q.y), bearing, 0.035f * s, StandIn.Pine, PostGrey, 0.3f)) posts++;
                }
            }

            // 5. Inside: the dining hut facing the gate, three huts, two tents, the wooden water tower in the back corner.
            ctx.PlaceByWidth(DiningHut, L(0f, -0.07f), bearing + 180f, 0.13f * s, StandIn.Box, HutTimber);
            // Explicit hut ids: kit_negev (the first variant) is the mosque-like b17 mesh, so kit_negev* could draw it.
            int hut = 0;
            foreach (var (u, v) in new[] { (-0.19f, -0.09f), (-0.19f, 0.09f), (0.20f, -0.09f) })
                ctx.PlaceByWidth(hut++ % 2 == 0 ? Hut : Hut2, L(u, v), bearing + 90f, 0.12f * s, StandIn.Box, HutTimber);
            foreach (float u in new[] { -0.05f, 0.07f })
                ctx.PlaceByWidth(Tent, L(u, 0.12f), bearing, 0.055f * s, StandIn.Box, Canvas);
            ctx.PlaceByHeight(Tower, L(0.20f, 0.09f), bearing, 0.20f * s, StandIn.Palm, TowerTimber, 0.3f);

            // 6. Young trees: eucalyptus along the road past the unit spot, tamarisks at the plots' outer corners.
            int euc = 0, tam = 0;
            foreach (var (x, z) in new[] { (0.06f, -0.36f), (-0.06f, -0.50f), (0.06f, -0.62f), (-0.06f, -0.72f) })
            {
                var at = F(x, z);
                if (euc < MaxEucalyptus && !ctx.InUnitSpot(at, 0.05f * s) &&
                    ctx.PlaceByHeight(Eucalyptus, at, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, Leaf, 0.5f)) euc++;
            }
            foreach (var (x, z) in new[] { (-0.56f, 0.46f), (0.56f, 0.46f), (-0.52f, -0.30f), (0.52f, -0.30f) })
            {
                var at = F(x, z);
                if (tam < MaxTamarisks && !ctx.InUnitSpot(at, 0.05f * s) &&
                    ctx.PlaceByHeight(Tamarisk, at, ctx.Range(0f, 360f), 0.12f * s, StandIn.Tree, TamariskGreen, 0.6f)) tam++;
            }

            // 7. Life: one kibbutz woman by the near plot, beside the road.
            ctx.PlaceByHeight(Woman, F(0.14f, -0.44f), bearing + ctx.Range(150f, 210f), 0.07f * s, StandIn.Figure, null, 0.4f);

            // 8. Desert rocks out on the flanks, clear of the plots, compound and unit spot.
            int rocks = 0;
            foreach (var (x, z) in new[] { (-0.64f, -0.10f), (0.64f, -0.10f), (-0.22f, -0.70f) })
            {
                var at = F(x, z);
                if (rocks < MaxRocks && ctx.PlaceByWidth(Rock, at, ctx.Range(0f, 360f), ctx.Range(0.05f, 0.07f) * s, StandIn.Rock, RockTan, 40f)) rocks++;
            }
        }
    }
}
