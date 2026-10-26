#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The Dead Sea's western shore, 1948: a flat strip of desert between the water and the cliffs. Along the water
    /// runs a white salt-crust band (the beach layer at full strength, found with ctx.IsWater, with small white salt-pan
    /// flats on it) and low white salt formations. On the land side, the far side from the water, a steep cliff wall rises
    /// as a row of large rock stand-ins over a rocky band with a scree apron. Between them, off to one side of the unit
    /// spot, a spring oasis: green ground, reeds and a few date palms, with a seep running down to the shore. No settlement;
    /// the unit spot stays open. No spring, oasis or place is named.
    ///
    /// The oasis follows the western-shore spring oases. Springs at the foot of the cliffs, with reeds and cane round a
    /// spring pool, are described at Ein Feshkha in 19th-century accounts:
    /// https://en.wikipedia.org/wiki/Ein_Feshkha (checked 2026-10-19). Its date palms are described today:
    /// https://jewishpress.com/sections/travel/the-einot-tzukim-nature-reserve-oasis-in-the-desert/2013/11/22 (checked
    /// 2026-10-19); the palms are kept to a few. The potash works (Kalia, Sodom) are not drawn here (see the lane notes).
    ///
    /// With no water in the hex (the test surface, or a board without the sea), the sea side defaults to east, bearing 90
    /// (the Dead Sea lies east of the western-shore cliffs), and the salt band follows a straight virtual shore at 0.78 S.
    ///
    /// Stand-ins: tiles:dead_sea_shore/salt (Rock, white), tiles:dead_sea_shore/cliff (Rock, tan and brown in turn) and
    /// tiles:dead_sea_shore/reeds (Pine, reed green). Cliff rocks are spaced a little closer than their scaled width
    /// (PieceScale 1.35), so the wall reads continuous; every other piece keeps a clearance of about 0.95 x its asked width.
    ///
    /// Worst-case triangles on a flat surface (caps below; the date palm counted from its OBJ file on 19 October 2026):
    /// 5 date palms x 284 = 1,420. 12 cliff + 8 salt Rock stand-ins x 20 = 400. 12 reed Pine stand-ins x 50 = 600. 5 salt-pan
    /// flats x 12 = 60. Total about 2,480; budget 5,000.</summary>
    [HexPrototype("dead_sea_shore", Kind = "nature", Region = "south")]
    public sealed class DeadSeaShore : IHexPrototype
    {
        public string Title => "Dead Sea shore: salt crust, cliffs, spring oasis (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "desert";
        public int TriangleBudget => 5000;

        const string Salt = "tiles:dead_sea_shore/salt", Cliff = "tiles:dead_sea_shore/cliff", Reeds = "tiles:dead_sea_shore/reeds",
            Palm = "sandbox:date_palm";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxCliffs = 12, MaxSalt = 8, MaxPans = 5, MaxPalms = 5, MaxReeds = 12;

        static readonly Color SaltWhite = new Color(0.95f, 0.94f, 0.90f), PanWhite = new Color(0.92f, 0.91f, 0.87f),
            CliffTan = new Color(0.70f, 0.58f, 0.44f), CliffBrown = new Color(0.58f, 0.46f, 0.35f),
            ReedGreen = new Color(0.50f, 0.58f, 0.30f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The sea side: the bearing on which water comes nearest the centre (as JaffaOldCity finds it); east if none.
            float seaB = 90f, shoreR = 1f;
            bool sea = false;
            for (int i = 0; i < 24; i++)
            {
                float b = 15f * i;
                for (float r = 0.20f; r < shoreR && r <= 0.95f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(b, r))) { seaB = b; shoreR = r; sea = true; break; }
            }
            float landB = seaB + 180f;
            var seaDir = Flat(ctx.At(seaB, 1f) - ctx.Centre).normalized;
            var across = new Vector3(seaDir.z, 0f, -seaDir.x);

            // 2. Ground: the sand base, then a broad paler strip on the sea side (shore flats).
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.8f * s, 2.0f * s, 0.2f, 0.85f);
            ctx.PaintGround("beach", ctx.At(seaB, 0.6f), seaB, 1.7f * s, 0.45f * s, 0.45f, 0.5f);

            // 3. The shore line: on bearings either side of the sea direction, step out until water is just ahead (as
            //    TelAvivCity's promenade does); with no water, the straight virtual shore at 0.78 S.
            var shore = new List<Vector3>();
            for (float d = -80f; d <= 80f; d += 8f)
            {
                float b = seaB + d;
                if (sea)
                {
                    for (float r = 0.25f; r <= 0.95f; r += 0.02f)
                        if (ctx.IsWater(ctx.At(b, r + 0.04f))) { shore.Add(ctx.At(b, r)); break; }
                }
                else
                {
                    float r = 0.78f / Mathf.Cos(Mathf.Deg2Rad * d);
                    if (r <= 0.92f) shore.Add(ctx.At(b, r - 0.03f));
                }
            }

            // 4. The salt crust: a white band along the shore, the beach layer at full strength.
            foreach (var p in shore) ctx.PaintGround("beach", p, seaB, 0.10f * s, 0.09f * s, 0.3f, 1f);
            for (int k = 0; k < shore.Count - 1; k++)
                if (Flat(shore[k + 1] - shore[k]).magnitude < 0.2f * s) ctx.PaintStroke("beach", shore[k], shore[k + 1], 0.09f * s, 1f);

            // Pieces already down, as circles (centre, radius in metres), so later pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 5. Salt pans (small white flats, never overlapping: every third shore point, about 0.33 S apart) and white salt
            //    formations between them, just inland of the water.
            int pans = 0, salt = 0;
            for (int k = 0; k < shore.Count; k++)
            {
                var inland = -seaDir;
                if (k % 3 == 0 && pans < MaxPans)
                {
                    var at = shore[k] + inland * (0.06f * s);
                    float w = ctx.Range(0.06f, 0.09f) * s, dpt = ctx.Range(0.04f, 0.06f) * s;
                    if (!ctx.InUnitSpot(at, 0.05f * s) && Free(at, 0.06f * s) && ctx.PlaceFlat(at, seaB + 90f + ctx.Range(-20f, 20f), w, dpt, PanWhite))
                    {
                        taken.Add((at, 0.06f * s)); pans++;
                    }
                }
                else if (salt < MaxSalt)
                {
                    var at = shore[k] + inland * (ctx.Range(0.04f, 0.12f) * s) + across * (ctx.Range(-0.03f, 0.03f) * s);
                    float w = ctx.Range(0.035f, 0.06f) * s, r = 0.95f * w;
                    if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                    if (ctx.PlaceByWidth(Salt, at, ctx.Range(0f, 360f), w, StandIn.Rock, SaltWhite)) { taken.Add((at, r)); salt++; }
                }
            }

            // 6. The cliffs on the land side: a rocky band and a scree apron, then a row of large rocks across the hex at
            //    0.64 S from the centre, tan and brown in turn, spaced so they touch and read as one wall.
            ctx.PaintGround("mountain", ctx.At(landB, 0.74f), landB, 1.8f * s, 0.32f * s, 0.4f, 0.95f);
            ctx.PaintGround("hills", ctx.At(landB, 0.54f), landB, 1.6f * s, 0.14f * s, 0.5f, 0.6f);
            int cliffs = 0;
            var landDir = -seaDir;
            for (float t = -0.90f; t <= 0.90f && cliffs < MaxCliffs; t += 0.16f)
            {
                float w = ctx.Range(0.14f, 0.17f) * s;
                var at = ctx.Centre + landDir * ((0.64f + ctx.Range(-0.01f, 0.03f)) * s) + across * (t * s);
                if (ctx.InUnitSpot(at, 0.95f * w)) continue;
                if (ctx.PlaceByWidth(Cliff, at, ctx.Range(0f, 360f), w, StandIn.Rock, cliffs % 2 == 0 ? CliffTan : CliffBrown, maxSlopeDeg: 90f))
                {
                    taken.Add((at, 0.6f * w)); cliffs++;
                }
            }

            // 7. The spring oasis, to one side of the unit spot between the cliff foot and the shore: green ground, a seep to
            //    the shore, a few date palms near its heart and a ring of reeds round them.
            float side = ctx.Rng.Next(2) == 0 ? -1f : 1f;
            var oasis = ctx.Centre + across * (side * 0.50f * s) + landDir * (0.18f * s);
            ctx.PaintGround("grass", oasis, landB, 0.32f * s, 0.24f * s, 0.5f, 0.9f);
            ctx.PaintStroke("grass", oasis, oasis + seaDir * (0.55f * s), 0.04f * s, 0.7f);
            int palms = 0;
            for (int i = 0; i < 20 && palms < MaxPalms; i++)
            {
                var at = oasis + new Vector3(ctx.Range(-0.09f, 0.09f) * s, 0f, ctx.Range(-0.09f, 0.09f) * s);
                float h = ctx.Range(0.13f, 0.16f) * s, r = 0.045f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Palm, at, ctx.Range(0f, 360f), h, StandIn.Palm, null, footprintShare: 0.3f)) { taken.Add((at, r)); palms++; }
            }
            int reeds = 0;
            float turn = ctx.Range(0f, 30f);
            for (int i = 0; i < 18 && reeds < MaxReeds; i++)
            {
                var at = oasis + Quaternion.Euler(0f, turn + 20f * i, 0f) * Vector3.forward * (ctx.Range(0.13f, 0.17f) * s);
                float h = ctx.Range(0.045f, 0.06f) * s, r = 0.025f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Reeds, at, ctx.Range(0f, 360f), h, StandIn.Pine, ReedGreen, footprintShare: 0.5f)) { taken.Add((at, r)); reeds++; }
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
