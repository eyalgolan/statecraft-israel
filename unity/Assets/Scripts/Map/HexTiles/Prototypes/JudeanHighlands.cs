#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The Judean and Samarian highlands, 1948: a smooth highland dome (no cliffs, so units cross it easily) that
    /// reads as rugged mountain from far away. Three great pale limestone crags stand out at the hex corners, never at the
    /// centre. Rocky scree aprons are painted round their feet and along the rim. Sparse olives grow on painted terraces on
    /// the gentler (lowest) side. Garrigue scrub clumps, a few carobs and Aleppo pines are scattered between them. A goat
    /// track winds across from one pass between crags to the next, through the clear unit spot.
    ///
    /// Layout, by corner (bearings 0, 60 ... 300; corner g is the lowest one, the gentle side): olives at g, crags at g+1,
    /// g+3 and g+5 (sometimes g+5 is left out), and the track from corner g+2 to corner g+4. Crags sit with a flat face
    /// towards the centre (yaw = bearing + k x 90), so by arithmetic the footprint clears the unit spot (inner face at
    /// 0.31 S or more) and stays in the inset hexagon (0.866 d + 0.683 W at most 0.796 at a corner). That caps a crag at
    /// about 0.48 S wide; a refused crag is retried 10% smaller, twice.
    ///
    /// Triangle budget by arithmetic (caps below, with measured per-model counts): 3 crags + 4 boulders x ~700 = 4,900.
    /// 10 olives x ~1,600 = 16,000. 5 carobs x ~650 = 3,250. 3 pines x ~1,600 = 4,800. 24 garrigue stand-ins x under 100 =
    /// under 2,400. Total under 31,400 of 40,000 (still under 36,000 if the limestone variants run twice as heavy).
    ///
    /// The garrigue has no model (tiles:judean_highlands/garrigue), so it draws the Tree stand-in tinted dusty green.</summary>
    [HexPrototype("judean_highlands")]
    public sealed class JudeanHighlands : IHexPrototype
    {
        public string Title => "Judean highlands: limestone crags, terraced olives (1948)";
        public HexTerrain Terrain => HexTerrain.Mountain;
        public string Ground => "mountain";
        public int TriangleBudget => 40000;

        // Models: EZ-Tree trees and Poly Haven rocks are local-only for now; the stand-ins draw without them.
        const string Rock = "sandbox:limestone*", Olive = "sandbox:olive_lite", Carob = "sandbox:carob_lite",
            Pine = "sandbox:aleppo_pine_lite", Garrigue = "tiles:judean_highlands/garrigue";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxBoulders = 4, MaxOlives = 10, MaxCarobs = 5, MaxPines = 3, MaxGarrigue = 24;

        static readonly Color Limestone = new Color(0.88f, 0.85f, 0.77f), OliveGreen = new Color(0.50f, 0.55f, 0.38f),
            CarobGreen = new Color(0.27f, 0.39f, 0.20f), PineGreen = new Color(0.20f, 0.33f, 0.18f),
            DustyGreen = new Color(0.52f, 0.56f, 0.40f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float Corner(int k) => 60f * (((k % 6) + 6) % 6);

            // 1. The gentle side: the lowest corner at 0.7 S, scanned from a seeded start with a strict "<" so ties (a flat
            //    surface) resolve the same way every time.
            int start = ctx.Rng.Next(6), g = start;
            float lowest = float.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                int k = (start + i) % 6;
                float y = ctx.GroundY(ctx.At(Corner(k), 0.7f));
                if (y < lowest) { lowest = y; g = k; }
            }
            float gentle = Corner(g);

            // 2. The crags: at corners g+3 (opposite the gentle side), g+1 and, two times in three, g+5. Width W, centre at
            //    0.30 + W/2 + a little, a flat face to the centre. maxSlopeDeg 70 so the dome's flanks never refuse them.
            var crags = new List<(Vector3 at, float yaw, float w)>();
            var cragCorners = ctx.Rng.Next(3) == 0 ? new[] { g + 3, g + 1 } : new[] { g + 3, g + 1, g + 5 };
            foreach (int k in cragCorners)
            {
                float bearing = Corner(k), w = ctx.Range(0.38f, 0.46f) * s, yaw = bearing + 90f * ctx.Rng.Next(4);
                float slack = ctx.Range(0.01f, 0.03f) * s;
                for (int attempt = 0; attempt < 3; attempt++, w *= 0.9f)
                {
                    var at = ctx.At(bearing, (0.30f * s + 0.5f * w + slack) / s);
                    if (ctx.InUnitSpot(at, 0.5f * w)) continue;
                    if (ctx.PlaceByWidth(Rock, at, yaw, w, StandIn.Rock, Limestone, maxSlopeDeg: 70f)) { crags.Add((at, yaw, w)); break; }
                }
            }
            bool InCrag(Vector3 p, float marginM)
            {
                foreach (var c in crags)
                {
                    var local = Quaternion.Euler(0f, -c.yaw, 0f) * Flat(p - c.at);
                    float half = 0.5f * c.w + marginM;
                    if (Mathf.Abs(local.x) < half && Mathf.Abs(local.z) < half) return true;
                }
                return false;
            }

            // 3. The goat track: a curve from corner g+2 to corner g+4, bowed towards the gentle side so it crosses near the
            //    centre, with a small wiggle that dies out at both ends; painted as dirt strokes.
            Vector3 p0 = ctx.At(Corner(g + 2), 0.90f), p2 = ctx.At(Corner(g + 4), 0.90f), pc = ctx.At(gentle, ctx.Range(0.25f, 0.35f));
            float wiggle = ctx.Range(0.03f, 0.05f) * s, phase = ctx.Range(0f, 6.283f);
            var track = new List<Vector3>();
            for (int k = 0; k <= 16; k++)
            {
                float t = k / 16f, u = 1f - t;
                var at = u * u * p0 + 2f * u * t * pc + t * t * p2;
                var tangent = Flat(2f * u * (pc - p0) + 2f * t * (p2 - pc)).normalized;
                var side = new Vector3(tangent.z, 0f, -tangent.x);
                track.Add(at + side * (wiggle * Mathf.Sin(Mathf.PI * t) * Mathf.Sin(3f * Mathf.PI * t + phase)));
            }
            for (int k = 0; k < track.Count - 1; k++) ctx.PaintStroke("dirt", track[k], track[k + 1], 0.03f * s, 0.8f);
            bool NearTrack(Vector3 p, float within)
            {
                for (int k = 0; k < track.Count - 1; k++) if (DistToSegment(p, track[k], track[k + 1]) < within) return true;
                return false;
            }

            // 4. Scree: pale rock aprons round each crag's foot and fanning to the rim (the mountain layer, which holds the
            //    rocky read where neighbouring ground blends in), a rocky rim band away from the gentle side, and earthier
            //    patches (the hills layer) on the open slopes beside the crags.
            foreach (var c in crags)
            {
                float bearing = Bearing(ctx, c.at);
                ctx.PaintGround("mountain", c.at, c.yaw, 1.6f * c.w, 1.4f * c.w, 0.45f, 1f);
                ctx.PaintGround("mountain", ctx.At(bearing, 0.80f), bearing, 1.1f * c.w, 0.22f * s, 0.5f, 0.9f);
            }
            for (int k = 1; k <= 4; k++)   // the four edges not touching corner g
                ctx.PaintStroke("mountain", ctx.At(Corner(g + k), 0.86f), ctx.At(Corner(g + k + 1), 0.86f), 0.12f * s, 0.7f);
            for (int k = 1; k < 6; k += 2)   // edge midpoints g+1.5, g+3.5 and g+5.5, each beside a crag corner
            {
                float bearing = Corner(g + k) + 30f;
                ctx.PaintGround("hills", ctx.At(bearing + ctx.Range(-8f, 8f), ctx.Range(0.50f, 0.60f)), bearing + ctx.Range(-20f, 20f),
                    ctx.Range(0.18f, 0.24f) * s, ctx.Range(0.10f, 0.14f) * s, 0.5f, 0.6f);
            }

            // 5. Boulders at the crags' feet: beside each crag (in its frame, off a side face), never in the unit spot.
            int boulders = 0;
            foreach (var c in crags)
            {
                if (boulders >= MaxBoulders) break;
                float w = ctx.Range(0.07f, 0.10f) * s, sideSign = ctx.Rng.Next(2) == 0 ? -1f : 1f;
                var at = c.at + Quaternion.Euler(0f, c.yaw, 0f) * new Vector3(sideSign * (0.5f * c.w + 0.6f * w + 0.02f * s), 0f, ctx.Range(-0.2f, 0.2f) * c.w);
                float yaw = ctx.Range(0f, 360f);
                if (ctx.InUnitSpot(at, 0.75f * w) || NearTrack(at, 0.75f * w + 0.02f * s)) continue;
                if (ctx.PlaceByWidth(Rock, at, yaw, w, StandIn.Rock, Limestone, maxSlopeDeg: 70f)) boulders++;
            }

            // Trees already down, so later pieces keep off them.
            var trees = new List<(Vector3 at, float r)>();
            bool Clear(Vector3 at, float r)
            {
                foreach (var t in trees) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 6. Terraced olives on the gentle side: three contour arcs about corner g (contours on the dome are circles),
            //    each painted as an earthy terrace band, olives spaced along them.
            int olives = 0;
            for (int row = 0; row < 3; row++)
            {
                float r = 0.42f + 0.13f * row, half = 20f + 8f * row;
                var arc = new List<Vector3>();
                for (int k = 0; k <= 6; k++) arc.Add(ctx.At(gentle - half + 2f * half * k / 6f, r));
                for (int k = 0; k < arc.Count - 1; k++) ctx.PaintStroke("hills", arc[k], arc[k + 1], 0.055f * s, 0.85f);

                float stepDeg = Mathf.Rad2Deg * (0.13f / r), jitter = ctx.Range(-0.5f, 0.5f) * stepDeg;
                for (float b = gentle - half + 0.5f * stepDeg + jitter; b <= gentle + half; b += stepDeg)
                {
                    if (olives >= MaxOlives) break;
                    var at = ctx.At(b, r + ctx.Range(-0.012f, 0.012f));
                    float height = ctx.Range(0.12f, 0.14f) * s, yaw = ctx.Range(0f, 360f);
                    if (ctx.InUnitSpot(at, 0.04f * s) || InCrag(at, 0.04f * s) || NearTrack(at, 0.05f * s) || !Clear(at, 0.04f * s)) continue;
                    if (ctx.PlaceByHeight(Olive, at, yaw, height, StandIn.Tree, OliveGreen, footprintShare: 0.45f, maxSlopeDeg: 50f))
                    {
                        trees.Add((at, 0.04f * s)); olives++;
                    }
                }
            }
            bool InTerraces(Vector3 p)
            {
                float d = Flat(p - ctx.Centre).magnitude / s, off = Mathf.Abs(Mathf.DeltaAngle(Bearing(ctx, p), gentle));
                return d > 0.36f && off < 40f;
            }

            // 7. A few Aleppo pines on the rugged side: edge midpoints in a seeded order, off the crags, track and terraces.
            int pines = 0, firstEdge = ctx.Rng.Next(6);
            for (int i = 0; i < 12 && pines < MaxPines; i++)
            {
                float bearing = Corner(firstEdge + i) + 30f + ctx.Range(-6f, 6f), r = i < 6 ? 0.66f : 0.72f;
                var at = ctx.At(bearing, r);
                float height = ctx.Range(0.15f, 0.18f) * s, yaw = ctx.Range(0f, 360f);
                if (InTerraces(at) || InCrag(at, 0.04f * s) || NearTrack(at, 0.06f * s) || !Clear(at, 0.05f * s)) continue;
                if (ctx.PlaceByHeight(Pine, at, yaw, height, StandIn.Pine, PineGreen, footprintShare: 0.3f, maxSlopeDeg: 50f))
                {
                    trees.Add((at, 0.04f * s)); pines++;
                }
            }

            // 8. Carobs, scattered at the margins between the crags.
            int carobs = 0;
            for (int i = 0; i < 30 && carobs < MaxCarobs; i++)
            {
                var at = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.40f, 0.74f));
                float height = ctx.Range(0.12f, 0.14f) * s, yaw = ctx.Range(0f, 360f);
                if (ctx.InUnitSpot(at, 0.04f * s) || InTerraces(at) || InCrag(at, 0.04f * s) || NearTrack(at, 0.05f * s) || !Clear(at, 0.05f * s)) continue;
                if (ctx.PlaceByHeight(Carob, at, yaw, height, StandIn.Tree, CarobGreen, footprintShare: 0.45f, maxSlopeDeg: 50f))
                {
                    trees.Add((at, 0.04f * s)); carobs++;
                }
            }

            // 9. Garrigue: clumps of two to four small, dusty-green bushes on the open slopes, off the unit spot and track.
            int garrigue = 0;
            for (int clump = 0; clump < 14 && garrigue < MaxGarrigue; clump++)
            {
                var centre = ctx.RandomPoint(0.05f * s);
                int n = 2 + ctx.Rng.Next(3);
                for (int k = 0; k < n && garrigue < MaxGarrigue; k++)
                {
                    var at = centre + new Vector3(ctx.Range(-0.05f, 0.05f) * s, 0f, ctx.Range(-0.05f, 0.05f) * s);
                    float height = ctx.Range(0.04f, 0.06f) * s, yaw = ctx.Range(0f, 360f);
                    if (ctx.InUnitSpot(at, 0.02f * s) || InCrag(at, 0.01f * s) || NearTrack(at, 0.03f * s) || !Clear(at, 0.02f * s)) continue;
                    if (ctx.PlaceByHeight(Garrigue, at, yaw, height, StandIn.Tree, DustyGreen, footprintShare: 1.0f, maxSlopeDeg: 60f)) garrigue++;
                }
            }
        }

        static float Bearing(HexTileContext ctx, Vector3 p) => Mathf.Atan2(p.x - ctx.Centre.x, p.z - ctx.Centre.z) * Mathf.Rad2Deg;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = Flat(b - a), ap = Flat(p - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (ap - ab * t).magnitude;
        }
    }
}
