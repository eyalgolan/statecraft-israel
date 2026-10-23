#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>An Arava spring oasis, 1948: on the flat sand and gravel of the valley, off to one side of the hex (0.42 S
    /// from the centre, so the unit spot stays open and dry), a small spring pool (one low flat, tinted water) with a stone
    /// well beside it. Round it, a cluster of date palms in two loose rings, reeds at the pool's edge and between the palm
    /// rings, and an outer ring of tamarisks, all on green ground with a damp seep running away from the centre. Acacias
    /// stand further out in the open desert. No settlement, and no spring or place is named.
    ///
    /// Source that palm oases grew round springs in the Arava: KKL-JNF, "Saving the Wild Date Palms of the Arava" (palm
    /// trees "flourished around springs in the central Arava for thousands of years", and a date oasis at one spring is
    /// recorded in 1838): https://kkl-jnf.org/about-kkl-jnf/green-israel-news/press-releases-2021/date-palms-in-the-arava.aspx
    /// (checked 2026-10-19). The reed and tamarisk ring and the well are generic.
    ///
    /// Spacing (PieceScale 1.35): pool half-size 0.06 x 0.045 S (a flat is not scaled); pool-edge reeds at 0.085 S from the
    /// pool's centre; inner palms at 0.12 S and outer palms at 0.20 S (each palm keeps 0.045 S clearance), the second reed
    /// ring at 0.16 S between them, tamarisks at 0.28 S (0.065 S clearance), acacias at least 0.40 S from the pool (0.08 S
    /// clearance). Every tall piece is tested with InUnitSpot, so the rings are partial on the centre side.
    ///
    /// Stand-ins: tiles:arava_oasis/reeds (Pine, reed green), tiles:arava_oasis/tamarisk (Tree, grey-green) and
    /// tiles:arava_oasis/well (Box, stone).
    ///
    /// Worst-case triangles on a flat surface (caps below; local meshes counted from their OBJ files on 19 October 2026):
    /// 12 date palms x 284 = 3,408. 8 acacias x 1,164 = 9,312. 3 desert rocks x 700 = 2,100. 14 reed Pine stand-ins x 50 =
    /// 700. 5 tamarisk Tree stand-ins x 56 = 280. The well box 16 and the pool flat 12. Total about 15,830; budget 18,000.</summary>
    [HexPrototype("arava_oasis", Kind = "nature", Region = "south")]
    public sealed class AravaOasis : IHexPrototype
    {
        public string Title => "Arava oasis: spring pool, date palms, tamarisks (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "desert";
        public int TriangleBudget => 18000;

        const string Palm = "sandbox:date_palm", Acacia = "sandbox:acacia_lite", Rock = "sandbox:desert_rock*",
            Reeds = "tiles:arava_oasis/reeds", Tamarisk = "tiles:arava_oasis/tamarisk", Well = "tiles:arava_oasis/well";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxPalms = 12, MaxAcacias = 8, MaxRocks = 3, MaxReeds = 14, MaxTamarisks = 5;

        static readonly Color PoolWater = new Color(0.24f, 0.42f, 0.46f), WellStone = new Color(0.70f, 0.64f, 0.55f),
            ReedGreen = new Color(0.47f, 0.58f, 0.28f), TamariskGrey = new Color(0.55f, 0.60f, 0.50f),
            AcaciaGreen = new Color(0.46f, 0.50f, 0.30f), RockRed = new Color(0.66f, 0.50f, 0.40f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float oasisB = ctx.Range(0f, 360f);
            var oasis = ctx.At(oasisB, 0.42f);
            var outward = Flat(oasis - ctx.Centre).normalized;
            var across = new Vector3(outward.z, 0f, -outward.x);
            Vector3 Around(float deg, float share) => oasis + Quaternion.Euler(0f, deg, 0f) * Vector3.forward * (share * s);

            // 1. Ground: sand to the rim, gravel sheets and sand streaks away from the oasis; then the oasis's green ground,
            //    damp earth under the pool, and a green seep running outward, away from the centre.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.8f * s, 2.0f * s, 0.2f, 0.8f);
            for (int k = 0; k < 3; k++)
                ctx.PaintGround("hills", ctx.At(oasisB + 180f + ctx.Range(-70f, 70f), ctx.Range(0.30f, 0.60f)), ctx.Range(0f, 180f),
                    ctx.Range(0.35f, 0.50f) * s, ctx.Range(0.25f, 0.40f) * s, 0.5f, 0.45f);
            for (int k = 0; k < 3; k++)
                ctx.PaintGround("beach", ctx.RandomPoint(0.08f * s), ctx.Range(-15f, 15f), ctx.Range(0.05f, 0.08f) * s, ctx.Range(0.30f, 0.45f) * s, 0.5f, 0.4f);
            ctx.PaintGround("grass", oasis, oasisB, 0.62f * s, 0.56f * s, 0.45f, 0.9f);
            ctx.PaintGround("dirt", oasis, oasisB, 0.18f * s, 0.15f * s, 0.4f, 0.6f);
            ctx.PaintStroke("grass", oasis, ctx.At(oasisB + ctx.Range(-25f, 25f), 0.95f), 0.06f * s, 0.65f);

            // Pieces already down, as circles (centre, radius in metres), so later pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 2. The spring pool, one low flat, and the well beside it, to one side (not towards the unit spot).
            float poolYaw = oasisB + ctx.Range(-20f, 20f);
            if (ctx.PlaceFlat(oasis, poolYaw, 0.12f * s, 0.09f * s, PoolWater)) taken.Add((oasis, 0.055f * s));
            var well = oasis + across * ((ctx.Rng.Next(2) == 0 ? -1f : 1f) * 0.12f * s);
            if (ctx.PlaceByWidth(Well, well, poolYaw, 0.035f * s, StandIn.Box, WellStone)) taken.Add((well, 0.035f * s));

            // 3. Reeds at the pool's edge.
            int reeds = 0;
            float turn = ctx.Range(0f, 45f);
            for (int i = 0; i < 8 && reeds < MaxReeds; i++)
            {
                var at = Around(turn + 45f * i, ctx.Range(0.08f, 0.09f));
                float h = ctx.Range(0.045f, 0.06f) * s, r = 0.02f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Reeds, at, ctx.Range(0f, 360f), h, StandIn.Pine, ReedGreen, footprintShare: 0.5f)) { taken.Add((at, r)); reeds++; }
            }

            // 4. Date palms in two loose rings round the pool: seven inner, five outer, offset from one another.
            int palms = 0;
            for (int ring = 0; ring < 2; ring++)
            {
                int n = ring == 0 ? 7 : 5;
                float share = ring == 0 ? 0.12f : 0.20f, start = ctx.Range(0f, 360f);
                for (int i = 0; i < n && palms < MaxPalms; i++)
                {
                    var at = Around(start + 360f * i / n + ctx.Range(-10f, 10f), share + ctx.Range(-0.01f, 0.01f));
                    float h = ctx.Range(0.13f, 0.18f) * s, r = 0.045f * s;
                    if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                    if (ctx.PlaceByHeight(Palm, at, ctx.Range(0f, 360f), h, StandIn.Palm, null, footprintShare: 0.3f)) { taken.Add((at, r)); palms++; }
                }
            }

            // 5. A second reed ring between the palm rings.
            for (int i = 0; i < 12 && reeds < MaxReeds; i++)
            {
                var at = Around(turn + 30f * i + 15f, ctx.Range(0.155f, 0.165f));
                float h = ctx.Range(0.045f, 0.06f) * s, r = 0.02f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Reeds, at, ctx.Range(0f, 360f), h, StandIn.Pine, ReedGreen, footprintShare: 0.5f)) { taken.Add((at, r)); reeds++; }
            }

            // 6. Tamarisks in an outer ring.
            int tamarisks = 0;
            float tStart = ctx.Range(0f, 360f);
            for (int i = 0; i < 8 && tamarisks < MaxTamarisks; i++)
            {
                var at = Around(tStart + 45f * i, ctx.Range(0.27f, 0.30f));
                float h = ctx.Range(0.09f, 0.11f) * s, r = 0.065f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Tamarisk, at, ctx.Range(0f, 360f), h, StandIn.Tree, TamariskGrey, footprintShare: 0.6f)) { taken.Add((at, r)); tamarisks++; }
            }

            // 7. Acacias out in the open desert, at least 0.40 S from the pool, widely spaced.
            int acacias = 0;
            for (int i = 0; i < 60 && acacias < MaxAcacias; i++)
            {
                var at = ctx.RandomPoint(0.06f * s);
                float h = ctx.Range(0.12f, 0.15f) * s, r = 0.08f * s;
                if (Flat(at - oasis).magnitude < 0.40f * s || ctx.InUnitSpot(at, r) || !Free(at, r + 0.06f * s)) continue;
                if (ctx.PlaceByHeight(Acacia, at, ctx.Range(0f, 360f), h, StandIn.Tree, AcaciaGreen, footprintShare: 0.6f)) { taken.Add((at, r)); acacias++; }
            }

            // 8. A few small reddish rocks on the gravel.
            int rocks = 0;
            for (int i = 0; i < 30 && rocks < MaxRocks; i++)
            {
                float w = ctx.Range(0.03f, 0.05f) * s, rr = 0.95f * w;
                var at = ctx.RandomPoint(0.05f * s);
                if (Flat(at - oasis).magnitude < 0.36f * s || ctx.InUnitSpot(at, rr) || !Free(at, rr + 0.02f * s)) continue;
                if (ctx.PlaceByWidth(Rock, at, ctx.Range(0f, 360f), w, StandIn.Rock, RockRed)) { taken.Add((at, rr)); rocks++; }
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
