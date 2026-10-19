#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A Galilee kibbutz of the 1940s, grown round its tower-and-stockade core, as a Civ-style settlement tile.
    /// Flat terrain and green grass ground: the northern valleys, greener than the generic kibbutz tile.
    /// - The core stands in the north-east of the hex (bearing about 40, 0.52 S out): a square palisade of timber posts with
    ///   a gate on the side toward the hex centre, a wooden watchtower in the middle and two sheds. The tower-and-stockade
    ///   method built a gravel-filled timber wall round a yard of about one dunam, with a prefabricated wooden tower and
    ///   sheds inside, at 57 settlements from late 1936 to October 1939, Galilee ones among them (Hanita, Ein Gev, Ginosar)
    ///   (https://en.wikipedia.org/wiki/Tower_and_stockade, checked 2026-10-19).
    /// - Round it, on a lawn, the grown farm: the dining hall (proto:kibbutz-dining-hall-v1) facing the hex centre, five low
    ///   quarters (kit_kibbutz), the water tower (kit_kibbutz_4) and a cowshed (kit_kibbutz_3).
    /// - Fishponds to the south-west: a 2 x 3 grid of dark-water flats on painted dykes. Carp ponds were dug at Nir David
    ///   (Tel Amal) in the Beit She'an valley and at other kibbutzim from 1939 on
    ///   (https://www.algemeiner.com/?p=68807 and https://en.wikipedia.org/wiki/Nir_David, checked 2026-10-19).
    /// - An orchard to the south-east (citrus on painted green crop), fields painted round the rest, a straight cypress
    ///   windbreak along the west edge, and the farm road from the gate through the hex centre (where a unit stands) to the
    ///   south edge. A pioneer by the dining hall is the tile's life.
    /// - It differs from the generic kibbutz tile (Kibbutz.cs) in its stockade core, grass ground, pond grid and orchard.
    /// - No tall piece enters the unit spot.
    ///
    /// Triangle estimate, at the maximum attempted counts, with the per-model counts measured for Kibbutz.cs:
    /// - Dining hall cluster 10,000. Five quarters kit_kibbutz x 700 = 3,500. Cowshed kit_kibbutz_3 1,400. Water tower
    ///   kit_kibbutz_4 1,400.
    /// - 8 cypresses and 6 citrus x 1,600 = 22,400. The pioneer figure (a v2 character) about 4,000.
    /// - About 36 palisade posts, the watchtower and two sheds as stand-ins, about 500. 6 pond flats x 12 = 72.
    /// - Total about 43,300 of 50,000.
    ///
    /// The palisade, watchtower and sheds have no models (tiles:kibbutz_galilee/...), so they always draw stand-ins: Box for
    /// a post or a shed, Palm (a tall trunk with a crown) for the watchtower.</summary>
    [HexPrototype("kibbutz_galilee", Kind = "village", Region = "north")]
    public sealed class KibbutzGalilee : IHexPrototype
    {
        public string Title => "Galilee kibbutz, stockade and fishponds (1940s)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "grass";
        // Measured 50,821 triangles worst case with the buildings drawn as the shipped b*-v2 town models at full
        // level (about 6,000 each, HexModels.TownAliases); the budget is that plus 10%. The doc comment's estimate predates them.
        public int TriangleBudget => 60000;

        // Explicit kit ids, not "kit_kibbutz*": the wildcard would mix the dining hall, barn and water tower into the rows.
        const string DiningHall = "proto:kibbutz-dining-hall-v1", Quarters = "sandbox:kit_kibbutz", Cowshed = "sandbox:kit_kibbutz_3",
            WaterTower = "sandbox:kit_kibbutz_4", Cypress = "sandbox:cypress_lite", Citrus = "sandbox:citrus_lite",
            Pioneer = "proto:figure-sabra-pioneer-c04-1948-v2", Post = "tiles:kibbutz_galilee/palisade_post",
            Watchtower = "tiles:kibbutz_galilee/watchtower", Shed = "tiles:kibbutz_galilee/stockade_shed";

        static readonly Color Timber = new Color(0.50f, 0.37f, 0.25f), DarkTimber = new Color(0.40f, 0.29f, 0.20f),
            RedRoof = new Color(0.72f, 0.36f, 0.26f), Plaster = new Color(0.86f, 0.82f, 0.72f), Concrete = new Color(0.82f, 0.80f, 0.74f),
            PondWater = new Color(0.16f, 0.30f, 0.36f), CypressLeaf = new Color(0.12f, 0.26f, 0.13f), CitrusLeaf = new Color(0.18f, 0.38f, 0.16f);

        /// <summary>The stockade's half-side and its centre's distance from the hex centre, as shares of S.</summary>
        const float Half = 0.12f, Out = 0.52f;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float b0 = 40f + ctx.Range(-4f, 4f);
            Vector3 outward = Dir(b0), across = Dir(b0 + 90f);
            // The compound frame: u across, v outward, shares of S, about the stockade's centre.
            Vector3 L(float u, float v) => ctx.Centre + outward * ((Out + v) * s) + across * (u * s);
            // A frame from the hex centre toward a bearing: x across, z out.
            Vector3 F(float bearing, float x, float z) => ctx.Centre + Dir(bearing) * (z * s) + Dir(bearing + 90f) * (x * s);

            var taken = new List<(Vector3 at, float r)>();
            bool Put(Vector3 at, float r, System.Func<bool> place)
            {
                if (ctx.InUnitSpot(at, r)) return false;
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                if (!place()) return false;
                taken.Add((at, r));
                return true;
            }
            float Clear(float w) => 0.6f * w * HexScale.PieceScale;

            // 1. Fields: large painted parcels round the hex, clear of the compound, the ponds and the orchard.
            string[] crops = { "wheat", "green_crop", "ploughed", "green_crop" };
            foreach (var (b, r, turn) in new[] { (275f, 0.42f, 0f), (282f, 0.66f, 90f), (320f, 0.60f, 0f), (348f, 0.40f, 90f), (178f, 0.62f, 0f), (95f, 0.45f, 90f) })
                ctx.PaintGround(crops[ctx.Rng.Next(crops.Length)], ctx.At(b, r), b + turn, 0.24f * s, 0.22f * s, 0.1f, 0.9f);

            // 2. The compound ground: a lawn round the farm, beaten earth inside the stockade, and the farm road from the gate
            //    through the hex centre to the south edge.
            ctx.PaintGround("grass", L(0f, 0f), b0, 0.95f * s, 0.55f * s, 0.2f, 0.9f);
            ctx.PaintGround("dirt", L(0f, 0f), b0, 2f * Half * s, 2f * Half * s, 0.1f, 0.9f);
            ctx.PaintStroke("dirt", L(0f, -Half), ctx.Centre, 0.035f * s, 0.95f);
            ctx.PaintStroke("dirt", ctx.Centre, ctx.At(195f, 0.95f), 0.035f * s, 0.95f);

            // 3. The stockade: timber posts every 0.03 S round the square, alternately 0.026 and 0.02 S wide so neighbours never
            //    share a face plane, a gate gap on the side toward the hex centre, the watchtower in the middle and two sheds.
            taken.Add((L(0f, 0f), (Half * 1.42f) * s));
            int i = 0;
            for (float t = -Half; t < Half - 0.001f; t += 0.03f, i++)
            {
                float w = (i % 2 == 0 ? 0.026f : 0.02f) * s;
                ctx.PlaceByWidth(Post, L(t, Half), b0, w, StandIn.Box, DarkTimber);
                ctx.PlaceByWidth(Post, L(Half, -t), b0, w, StandIn.Box, DarkTimber);
                if (Mathf.Abs(t + 0.015f) > 0.03f) ctx.PlaceByWidth(Post, L(-t, -Half), b0, w, StandIn.Box, DarkTimber);
                ctx.PlaceByWidth(Post, L(-Half, t), b0, w, StandIn.Box, DarkTimber);
            }
            ctx.PlaceByHeight(Watchtower, L(0f, 0.03f), b0, 0.20f * s, StandIn.Palm, Timber, 0.2f);
            ctx.PlaceByWidth(Shed, L(-0.065f, -0.05f), b0, 0.045f * s, StandIn.Box, Timber);
            ctx.PlaceByWidth(Shed, L(0.065f, -0.05f), b0, 0.045f * s, StandIn.Box, Timber);

            // 4. The farm round the stockade: dining hall, water tower, cowshed and five quarters, each kept clear of the others.
            var hall = L(-0.38f, 0f);
            Put(hall, Clear(0.16f * s), () => ctx.PlaceByWidth(DiningHall, hall, b0 + 180f, 0.16f * s, StandIn.Box, Plaster));
            var tower = L(-0.22f, 0.14f);
            Put(tower, 0.065f * s, () => ctx.PlaceByHeight(WaterTower, tower, b0 + 180f, 0.17f * s, StandIn.Palm, Concrete, 0.55f));
            var cowshed = L(0.28f, 0.17f);
            Put(cowshed, Clear(0.13f * s), () => ctx.PlaceByWidth(Cowshed, cowshed, b0 + 90f, 0.13f * s, StandIn.Box, Timber));
            foreach (var (u, v) in new[] { (0.30f, -0.04f), (0.24f, -0.20f), (-0.24f, -0.20f), (0.46f, -0.16f), (-0.50f, -0.18f) })
            {
                var at = L(u, v);
                Put(at, Clear(0.10f * s), () => ctx.PlaceByWidth(Quarters, at, b0 + 90f, 0.10f * s, StandIn.Box, RedRoof));
            }

            // 5. The fishponds to the south-west: dykes painted as dirt, six dark-water flats in a 2 x 3 grid with gaps
            //    between them (no two flats overlap).
            float pb = b0 + 180f;
            ctx.PaintGround("dirt", F(pb, 0f, 0.53f), pb, 0.40f * s, 0.44f * s, 0.15f, 0.9f);
            foreach (float z in new[] { 0.40f, 0.53f, 0.66f })
                foreach (float x in new[] { -0.085f, 0.085f })
                    ctx.PlaceFlat(F(pb, x, z), pb, 0.15f * s, 0.11f * s, PondWater, 80f);

            // 6. The orchard to the south-east: citrus in two rows of three on painted green crop.
            float ob = b0 + 100f;
            ctx.PaintGround("green_crop", F(ob, 0f, 0.56f), ob, 0.30f * s, 0.42f * s, 0.15f, 0.9f);
            foreach (float z in new[] { 0.42f, 0.56f, 0.70f })
                foreach (float x in new[] { -0.08f, 0.08f })
                {
                    var at = F(ob, x + ctx.Range(-0.008f, 0.008f), z);
                    float h = ctx.Range(0.12f, 0.13f) * s;
                    Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(Citrus, at, ctx.Range(0f, 360f), h, StandIn.Tree, CitrusLeaf, 0.5f));
                }

            // 7. The windbreak: a straight row of eight cypresses along the west edge.
            for (int k = 0; k < 8; k++)
            {
                var at = ctx.Centre + new Vector3(-0.70f * s, 0f, (-0.30f + 0.095f * k) * s);
                float h = ctx.Range(0.15f, 0.17f) * s;
                Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(Cypress, at, ctx.Range(0f, 360f), h, StandIn.Pine, CypressLeaf, 0.3f));
            }

            // 8. Life: a pioneer on the lawn by the dining hall.
            var woman = L(-0.22f, -0.06f);
            Put(woman, 0.02f * s, () => ctx.PlaceByHeight(Pioneer, woman, b0 + 180f + ctx.Range(-30f, 30f), 0.07f * s, StandIn.Figure, null, 0.4f));
        }

        static Vector3 Dir(float bearingDeg)
        {
            float a = bearingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        static float Planar(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
