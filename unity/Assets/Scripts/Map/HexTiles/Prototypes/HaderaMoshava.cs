#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Hadera in 1948: the moshava founded on 24 January 1891 (https://en.wikipedia.org/wiki/Hadera, checked
    /// 2026-10-19; some sources give 1890) on the coastal plain, in its eucalyptus groves.
    /// - The sea: the tile looks for the shore with ctx.IsWater on the northern and western bearings and lays everything
    ///   out from the nearest water. With no water on the hex (the flat test surface) it assumes the sea to the
    ///   north-west. The coast is painted sand with dune patches.
    /// - The main street runs inland from the dunes through a small plaza at the hex centre (kept open for a unit), lined
    ///   by moshava houses (kit_moshava farmhouses and kit_moshava_2 two-storey houses), with a barn (kit_moshava_3) behind the Khan's side.
    /// - The Khan, low-key, beside the plaza: the mid-1800s sandstone farmstead in a U round a courtyard, bought in 1891,
    ///   where the first settlers lived (https://shimur.org/sites/khan-museum-hadera/?lang=en, checked 2026-10-19). Three
    ///   stone stand-in blocks round a painted courtyard that opens to the street.
    /// - Dense eucalyptus groves on both flanks: Rothschild's agency planted eucalyptus here from 1895 to 1896 to drain the
    ///   swamps (https://kkl-jnf.org/hiking_and_walking_tracks/hadera-forest-hiking/ and
    ///   https://www.encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/haderah, checked 2026-10-19).
    ///   There is no eucalyptus model, so each is a tall, grey-green Tree stand-in (tiles:hadera_moshava/eucalyptus).
    /// - Citrus groves in rows on tilled soil inland, across the street from the Khan (generic; not sourced for Hadera as a fact).
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 10 houses and 1 barn (kit_moshava*, up to 1,400 each) = 15,400.
    /// - At most 9 citrus (citrus_lite, 1,602 each) = 14,418.
    /// - At most 52 eucalyptus stand-ins (about 56 each) = 2,912. If a real model is dropped in under
    ///   Resources/HexTiles/hadera_moshava/eucalyptus, it must stay under about 150 triangles, or lower MaxEucalyptus.
    /// - The Khan: 3 Box stand-ins, about 36. One donkey = 2,500; one townsperson (up to 4,000).
    /// - Total about 39,300 of 50,000.</summary>
    [HexPrototype("hadera_moshava", Kind = "town", Region = "centre")]
    public sealed class HaderaMoshava : IHexPrototype
    {
        public string Title => "Hadera, moshava in its eucalyptus groves (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 50000;

        // Explicit ids: the wildcard kit_moshava* would also pick the barn.
        const string Farmhouse = "sandbox:kit_moshava", TwoStorey = "sandbox:kit_moshava_2", Barn = "sandbox:kit_moshava_3",
            Citrus = "sandbox:citrus_lite", Eucalyptus = "tiles:hadera_moshava/eucalyptus", Khan = "tiles:hadera_moshava/khan",
            Donkey = "proto:figure-donkey-lite-1948", Settler = "proto:figure-sabra-pioneer-c04-1948-v2";

        static readonly Color RedRoof = new Color(0.72f, 0.36f, 0.26f), Timber = new Color(0.52f, 0.38f, 0.26f),
            Sandstone = new Color(0.82f, 0.72f, 0.56f), CitrusLeaf = new Color(0.16f, 0.34f, 0.14f), GumLeaf = new Color(0.47f, 0.56f, 0.47f);

        const int MaxHouses = 10, MaxCitrus = 9, MaxEucalyptus = 52;

        static float Edge(float bearing) => (0.8660254f - HexScale.InsetShare) / Mathf.Cos(Mathf.Deg2Rad * (Mathf.Repeat(bearing, 60f) - 30f));

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 0. Find the sea from 225 to 405 degrees (south-west round to north-east). Bearings that tie with the nearest
            //    are averaged, so a straight coast gives its true normal. Default: north-west, sand from 0.62 S.
            float seaB = 315f, waterR = 9f;
            var hit = new float[19];
            for (int i = 0; i < hit.Length; i++)
            {
                hit[i] = 9f;
                for (float r = 0.3f; r <= 0.95f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(225f + 10f * i, r))) { hit[i] = r; break; }
                waterR = Mathf.Min(waterR, hit[i]);
            }
            if (waterR < 9f)
            {
                float sum = 0f; int n = 0;
                for (int i = 0; i < hit.Length; i++) if (hit[i] <= waterR + 0.015f) { sum += 225f + 10f * i; n++; }
                seaB = sum / n;
            }
            float sand = waterR < 9f ? Mathf.Min(waterR - 0.16f, Edge(seaB) - 0.12f) : 0.62f;
            var toSea = Quaternion.Euler(0f, seaB, 0f) * Vector3.forward;
            var along = Quaternion.Euler(0f, seaB + 90f, 0f) * Vector3.forward;
            // A point in the coast's frame, shares of S: u toward the sea (along the main street), v across it.
            Vector3 L(float u, float v) => ctx.Centre + (toSea * u + along * v) * s;
            float yaw = seaB;

            // 1. The coast: sand from the dune line to the sea, with dune patches along it.
            ctx.PaintGround("beach", L(sand + 0.25f, 0f), yaw, 0.50f * s, 2.0f * s, 0.25f, 0.95f);
            foreach (float v in new[] { -0.55f, -0.15f, 0.30f, 0.65f })
                ctx.PaintGround("beach", L(sand - 0.04f, v), yaw + ctx.Range(-20f, 20f), 0.14f * s, 0.24f * s, 0.5f, 0.75f);

            // 2. The main street, from the dunes inland through the plaza at the centre.
            ctx.PaintStroke("dirt", L(sand, 0f), L(-0.95f, 0f), 0.055f * s, 0.95f);
            ctx.PaintGround("dirt", ctx.Centre, yaw + 45f, 0.22f * s, 0.22f * s, 0.3f, 0.8f);

            // 3. The houses, facing the street from both sides, five slots a side, a garden behind each; the barn on its
            //    yard inland of the Khan.
            int houses = 0;
            foreach (float side in new[] { -1f, 1f })
                foreach (float u in new[] { -0.56f, -0.36f, -0.18f, 0.18f, 0.36f })
                {
                    float w = ctx.Range(0.12f, 0.135f);
                    if (u > sand - 0.2f) continue;
                    var at = L(u, side * 0.15f);
                    if (houses >= MaxHouses || !ctx.PlaceByWidth(houses % 2 == 0 ? Farmhouse : TwoStorey, at, yaw + 90f * side, w * s, StandIn.Box, RedRoof)) continue;
                    houses++;
                    ctx.PaintGround("grass", L(u, side * 0.27f), yaw, 0.15f * s, 0.10f * s, 0.3f, 0.6f);
                }
            ctx.PaintGround("dirt", L(-0.42f, 0.42f), yaw, 0.20f * s, 0.20f * s, 0.3f, 0.75f);
            ctx.PlaceByWidth(Barn, L(-0.42f, 0.42f), yaw - 90f, 0.14f * s, StandIn.Box, Timber);

            // 4. The Khan beside the plaza: a back range and two wings round a courtyard opening to the street.
            ctx.PaintGround("dirt", L(0f, 0.43f), yaw, 0.16f * s, 0.20f * s, 0.25f, 0.85f);
            ctx.PlaceByWidth(Khan, L(0f, 0.54f), yaw, 0.10f * s, StandIn.Box, Sandstone);
            foreach (float u in new[] { -0.135f, 0.135f })
                ctx.PlaceByWidth(Khan, L(u, 0.405f), yaw, 0.10f * s, StandIn.Box, Sandstone);

            // 5. Citrus inland across the street from the Khan, in rows on tilled soil.
            ctx.PaintGround("ploughed", L(-0.42f, -0.48f), yaw, 0.34f * s, 0.34f * s, 0.25f, 0.7f);
            int citrus = 0;
            foreach (float u in new[] { -0.30f, -0.42f, -0.54f })
                foreach (float v in new[] { -0.36f, -0.48f, -0.60f })
                    if (citrus < MaxCitrus && ctx.PlaceByHeight(Citrus, L(u, v), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, CitrusLeaf, 0.5f))
                        citrus++;

            // 6. Eucalyptus groves: a jittered grid 0.11 S apart over everything that is not street, houses, Khan, citrus
            //    or sand, and outside the unit spot.
            int gums = 0;
            for (float u = -0.88f; u <= 0.88f; u += 0.11f)
                for (float v = -0.88f; v <= 0.88f; v += 0.11f)
                {
                    float ju = ctx.Range(-0.02f, 0.02f), jv = ctx.Range(-0.02f, 0.02f), h = ctx.Range(0.15f, 0.18f);
                    float uu = u + ju, vv = v + jv;
                    if (uu > sand - 0.08f) continue;                                   // the dunes and the beach
                    if (Mathf.Abs(vv) < 0.31f && uu > -0.72f) continue;                // the street and its houses
                    if (vv < -0.29f && uu < -0.24f) continue;                          // the citrus
                    if (vv > 0.29f && Mathf.Abs(uu) < 0.27f && vv < 0.68f) continue;   // the Khan
                    if (vv > 0.29f && vv < 0.56f && Mathf.Abs(uu + 0.42f) < 0.16f) continue;   // the barn
                    var at = L(uu, vv);
                    if (ctx.InUnitSpot(at) || gums >= MaxEucalyptus) continue;
                    if (ctx.PlaceByHeight(Eucalyptus, at, ctx.Range(0f, 360f), h * s, StandIn.Tree, GumLeaf, 0.35f)) gums++;
                }

            // 7. Life: a donkey and a settler on the street, off the plaza.
            ctx.PlaceByHeight(Donkey, L(-0.28f, 0.02f), yaw + 180f, 0.06f * s, StandIn.Figure, null, 0.5f);
            ctx.PlaceByHeight(Settler, L(-0.28f, -0.05f), yaw + 160f, 0.075f * s, StandIn.Figure, null, 0.4f);
        }
    }
}
