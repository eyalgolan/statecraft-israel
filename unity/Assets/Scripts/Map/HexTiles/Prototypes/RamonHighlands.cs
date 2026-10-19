#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The Negev highlands around Makhtesh Ramon, 1948: the rim of an erosion crater. A curved cliff wall sweeps
    /// round one side of the hex (an arc of about 150 degrees), its cap a row of large rocks (desert rocks and stand-ins in
    /// turn) and its foot a lower row of red and ochre talus rocks, so the wall reads in layers. Beyond the wall the plateau
    /// is painted in concentric bands of rock, ochre sand and red-brown earth. Inside the arc lies the dark crater floor
    /// (dirt and mountain), with a dry sandy wadi across it and a few low dark knolls. One or two ibex stand on the rim where the wall fades out.
    /// No settlement; the unit spot, on the floor, stays clear.
    ///
    /// How it differs from mountain_peaks: one curved wall on one side and a dark open floor, instead of two to four rock
    /// masses about the centre.
    ///
    /// Geometry: the arc is a circle of radius 0.55 S about a point 0.05 S from the centre (away from the wall side), from
    /// -75 to +75 degrees about the wall's bearing. Cap rocks (asked 0.13 to 0.16 S, so 0.18 to 0.22 S drawn) sit 0.61 S
    /// out on it, 15 degrees (0.16 S) apart, so they touch and read as one wall; each is turned square to the arc, so its
    /// outer face (at most about 0.60 + 0.11 = 0.71 S from the centre) stays inside the inset hexagon (0.80 S at an edge). Talus
    /// rocks (0.08 to 0.10 S asked) sit at 0.47 S, offset half a step, at least 0.42 S from the centre, which clears the unit
    /// spot (0.30 S) by their 0.095 S clearance; every piece is still tested with InUnitSpot. maxSlopeDeg 90 as
    /// MountainPeaks uses.
    ///
    /// Ibex: the local model proto:figure-nubian-ibex-1948-v1 measures 29,345 triangles (counted from its FBX), so one
    /// alone would fill the 30,000 cap. They are stand-ins, tiles:ramon_highlands/ibex (Figure, sandy brown), ready for a
    /// light model.
    ///
    /// Stand-ins: tiles:ramon_highlands/cliff (Rock, grey and ochre), tiles:ramon_highlands/talus (Rock, red and ochre),
    /// tiles:ramon_highlands/knoll (Rock, dark) and tiles:ramon_highlands/ibex.
    ///
    /// Worst-case triangles on a flat surface (caps below; the rocks counted from their OBJ files on 19 October 2026, every
    /// desert_rock variant 700): 6 desert rocks x 700 = 4,200. 6 cliff + 10 talus + 3 knoll Rock
    /// stand-ins x 20 = 380. 2 ibex Figure stand-ins (a capsule) x ~830 = 1,660. Total about 6,240; budget 8,000.</summary>
    [HexPrototype("ramon_highlands", Kind = "nature", Region = "south")]
    public sealed class RamonHighlands : IHexPrototype
    {
        public string Title => "Ramon highlands: crater rim, cliff wall, dark floor (1948)";
        public HexTerrain Terrain => HexTerrain.Mountain;
        public string Ground => "mountain";
        public int TriangleBudget => 8000;

        const string Rock = "sandbox:desert_rock*", Cliff = "tiles:ramon_highlands/cliff", Talus = "tiles:ramon_highlands/talus",
            Knoll = "tiles:ramon_highlands/knoll", Ibex = "tiles:ramon_highlands/ibex";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxModelRocks = 6, MaxCliffStandIns = 6, MaxTalus = 10, MaxKnolls = 3, MaxIbex = 2;
        const float ArcR = 0.55f, ArcShift = 0.05f, CapOut = 0.06f, TalusIn = 0.08f;

        static readonly Color Grey = new Color(0.56f, 0.55f, 0.52f), Ochre = new Color(0.76f, 0.58f, 0.32f),
            Red = new Color(0.62f, 0.36f, 0.26f), Dark = new Color(0.30f, 0.27f, 0.26f), IbexBrown = new Color(0.62f, 0.52f, 0.40f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float wallB = ctx.Range(0f, 360f);
            var c0 = ctx.At(wallB + 180f, ArcShift);
            Vector3 OnArc(float deg, float r) => c0 + Quaternion.Euler(0f, wallB + deg, 0f) * Vector3.forward * (r * s);

            // 1. The crater floor: dark dirt over the hex's inner part, mountain patches on it, and a dry sandy wadi across
            //    it from one open side to the other.
            ctx.PaintGround("dirt", c0, wallB, 1.1f * s, 1.1f * s, 0.5f, 0.6f);
            for (int k = 0; k < 3; k++)
            {
                var at = OnArc(ctx.Range(-150f, 150f) + 180f, ctx.Range(0.10f, 0.35f));
                ctx.PaintGround("mountain", at, ctx.Range(0f, 180f), ctx.Range(0.20f, 0.30f) * s, ctx.Range(0.15f, 0.22f) * s, 0.5f, 0.55f);
            }
            var w0 = ctx.At(wallB + 110f, 0.85f);
            var w1 = ctx.At(wallB + 180f + ctx.Range(-15f, 15f), 0.30f);
            var w2 = ctx.At(wallB + 250f, 0.85f);
            for (int k = 0; k < 12; k++)
            {
                float t0 = k / 12f, t1 = (k + 1) / 12f;
                ctx.PaintStroke("desert", Bezier(w0, w1, w2, t0), Bezier(w0, w1, w2, t1), 0.04f * s, 0.7f);
            }

            // 2. The wall and the plateau: rock at the wall's foot, then concentric bands beyond it (rock, ochre sand,
            //    red-brown earth), each a chain of strokes along the arc.
            string[] bands = { "mountain", "desert", "hills", "mountain" };
            float[] radii = { ArcR, ArcR + 0.10f, ArcR + 0.17f, ArcR + 0.24f };
            float[] widths = { 0.12f, 0.07f, 0.07f, 0.08f };
            for (int b = 0; b < bands.Length; b++)
                for (float d = -85f; d < 85f; d += 10f)
                    ctx.PaintStroke(bands[b], OnArc(d, radii[b]), OnArc(d + 10f, radii[b]), widths[b] * s, b == 0 ? 1f : 0.8f);

            // Pieces already down, as circles (centre, radius in metres), so later pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 3. The cap rocks along the arc: desert rocks and grey or ochre stand-ins in turn. They touch on purpose, so
            //    they are recorded with a smaller radius than their clearance.
            int models = 0, cliffs = 0, index = 0;
            for (float d = -75f; d <= 75.1f; d += 15f, index++)
            {
                float w = ctx.Range(0.13f, 0.16f) * s;
                var at = OnArc(d, ArcR + CapOut + ctx.Range(-0.01f, 0.01f));
                if (ctx.InUnitSpot(at, 0.95f * w)) continue;
                bool model = index % 2 == 0 && models < MaxModelRocks;
                if (!model && cliffs >= MaxCliffStandIns) continue;
                var tint = index % 4 < 2 ? Grey : Ochre;
                if (ctx.PlaceByWidth(model ? Rock : Cliff, at, wallB + d + ctx.Range(-5f, 5f), w, StandIn.Rock, tint, maxSlopeDeg: 90f))
                {
                    taken.Add((at, 0.5f * w));
                    if (model) models++; else cliffs++;
                }
            }

            // 4. The talus at the wall's foot, half a step offset: red and ochre in turn, the wall's lower layers.
            int talus = 0;
            index = 0;
            for (float d = -67.5f; d <= 67.6f && talus < MaxTalus; d += 15f, index++)
            {
                float w = ctx.Range(0.08f, 0.10f) * s;
                var at = OnArc(d, ArcR - TalusIn + ctx.Range(-0.01f, 0.01f));
                if (ctx.InUnitSpot(at, 0.95f * w)) continue;
                if (ctx.PlaceByWidth(Talus, at, ctx.Range(0f, 360f), w, StandIn.Rock, index % 2 == 0 ? Red : Ochre, maxSlopeDeg: 90f))
                {
                    taken.Add((at, 0.5f * w)); talus++;
                }
            }

            // 5. Low dark knolls out on the floor, between the unit spot and the open side.
            int knolls = 0;
            for (int i = 0; i < 20 && knolls < MaxKnolls; i++)
            {
                float w = ctx.Range(0.06f, 0.08f) * s, r = 0.95f * w;
                var at = ctx.At(wallB + 180f + ctx.Range(-80f, 80f), ctx.Range(0.40f, 0.62f));
                if (ctx.InUnitSpot(at, r) || !Free(at, r + 0.02f * s)) continue;
                if (ctx.PlaceByWidth(Knoll, at, ctx.Range(0f, 360f), w, StandIn.Rock, Dark, maxSlopeDeg: 90f)) { taken.Add((at, r)); knolls++; }
            }

            // 6. The ibex: on the rim where the wall fades out, just past either end of the arc (one each side first).
            int ibex = 0;
            for (int i = 0; i < 16 && ibex < MaxIbex; i++)
            {
                float d = (i % 2 == 0 ? 1f : -1f) * ctx.Range(84f, 100f), h = ctx.Range(0.05f, 0.06f) * s, r = 0.06f * s;
                var at = OnArc(d, ArcR + ctx.Range(-0.02f, 0.06f));
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Ibex, at, ctx.Range(0f, 360f), h, StandIn.Figure, IbexBrown, footprintShare: 0.8f, maxSlopeDeg: 90f))
                {
                    taken.Add((at, r)); ibex++;
                }
            }
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
