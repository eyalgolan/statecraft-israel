#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The Judean Desert's east slope, 1948: bare tan and chalk hills falling east towards the Dead Sea. Rounded
    /// badland ridges read as long bands of ground running roughly north to south: rock (mountain), chalk (beach, the
    /// palest layer), earth (hills) and sand (desert), over a sand base. Two or three deep wadis cut straight across the
    /// bands, west to east (downslope), each a dark dirt bed in a rocky bank that narrows towards its western head. Desert
    /// rocks are scattered on the ridges, a few of them large. There are almost no trees: one or two acacias in a wadi bed.
    /// One Bedouin goat-hair tent and a few goats sit at the rim. No settlement; the unit spot stays open.
    ///
    /// How it differs from desert_edge: banded chalk ridges instead of an even slope, several parallel eastward wadis
    /// instead of one meander, one tent instead of a camp, and at most two trees.
    ///
    /// Stand-ins: the tent (tiles:judean_desert/tent) is two dark Box stand-ins in a row, so it reads long and low; the goats
    /// (tiles:judean_desert/goat) are small dark Figure stand-ins, as WheatFields draws its flock.
    ///
    /// Spacing (PieceScale 1.35 applies to every piece): a piece's clearance radius is about 0.95 x its asked width (half
    /// the diagonal of its scaled square footprint), and every piece checks InUnitSpot with that radius, so nothing tall
    /// stands in the unit spot.
    ///
    /// Worst-case triangles on a flat surface (caps below; local meshes counted from their OBJ files on 19 October 2026):
    /// 10 desert rocks x 700 = 7,000. 2 acacias x 1,164 = 2,328. 3 goat Figure stand-ins (a capsule) x ~830 = 2,490. 2 tent
    /// boxes x 16 = 32. Total about 11,850; budget 14,000.</summary>
    [HexPrototype("judean_desert", Kind = "nature", Region = "south")]
    public sealed class JudeanDesert : IHexPrototype
    {
        public string Title => "Judean Desert: chalk badlands and eastward wadis (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "desert";
        public int TriangleBudget => 14000;

        const string Rock = "sandbox:desert_rock*", Acacia = "sandbox:acacia_lite", Tent = "tiles:judean_desert/tent",
            Goat = "tiles:judean_desert/goat";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxRocks = 10, MaxBigRocks = 3, MaxAcacias = 2, MaxGoats = 3;

        static readonly Color Chalk = new Color(0.86f, 0.81f, 0.70f), RockTan = new Color(0.72f, 0.60f, 0.45f),
            AcaciaGreen = new Color(0.48f, 0.50f, 0.30f), TentBlack = new Color(0.17f, 0.14f, 0.12f),
            GoatBlack = new Color(0.13f, 0.11f, 0.10f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The sand base, to the rim.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.8f * s, 2.0f * s, 0.2f, 0.85f);

            // 2. Badland ridges: eleven long bands, roughly north to south (tilted a little), across the hex from west to
            //    east, cycling rock, chalk, earth, chalk, rock, sand from a seeded start.
            string[] bands = { "mountain", "beach", "hills", "beach", "mountain", "desert" };
            int first = ctx.Rng.Next(bands.Length);
            float tilt = ctx.Range(-12f, 12f);
            var east = Quaternion.Euler(0f, tilt, 0f) * Vector3.right;
            for (int i = 0; i < 11; i++)
            {
                var at = ctx.Centre + east * ((-0.85f + 0.17f * i) * s);
                ctx.PaintGround(bands[(first + i) % bands.Length], at, tilt + ctx.Range(-4f, 4f),
                    ctx.Range(0.08f, 0.13f) * s, 1.9f * s, 0.45f, ctx.Range(0.55f, 0.8f));
            }

            // 3. The wadis: two or three, west to east across the bands, meandering a little, offset north and south of the
            //    centre. Each is a rocky bank (mountain) under a dark dirt bed, both narrowing towards the western head.
            var wadis = new List<List<Vector3>>();
            float[] lanes = ctx.Rng.Next(3) == 0 ? new[] { -0.45f, 0.45f } : new[] { -0.55f, -0.05f, 0.50f };
            foreach (float lane in lanes)
            {
                float z0 = (lane + ctx.Range(-0.05f, 0.05f)) * s, amp = ctx.Range(0.03f, 0.05f) * s;
                float freq = ctx.Range(2.5f, 4f), phase = ctx.Range(0f, 6.283f), drift = ctx.Range(-0.08f, 0.08f) * s;
                var line = new List<Vector3>();
                for (int k = 0; k <= 20; k++)
                {
                    float x = -1f + 0.1f * k;
                    line.Add(ctx.Centre + new Vector3(x * s, 0f, z0 + amp * Mathf.Sin(freq * x + phase) + drift * x));
                }
                for (int k = 0; k < line.Count - 1; k++)
                {
                    float wide = Mathf.Lerp(0.6f, 1f, k / 19f);
                    ctx.PaintStroke("mountain", line[k], line[k + 1], 0.10f * wide * s, 0.5f);
                    ctx.PaintStroke("dirt", line[k], line[k + 1], 0.05f * wide * s, 0.95f);
                }
                wadis.Add(line);
            }
            bool NearWadi(Vector3 p, float within)
            {
                foreach (var line in wadis)
                    for (int k = 0; k < line.Count - 1; k++) if (DistToSegment(p, line[k], line[k + 1]) < within) return true;
                return false;
            }

            // Pieces already down, as circles (centre, radius in metres), so later pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 4. One goat-hair tent at the rim: the first edge midpoint (from a seeded start) clear of the wadis, two dark
            //    boxes along the edge; up to three goats grazing just inside it.
            int startEdge = ctx.Rng.Next(6);
            for (int e = 0; e < 6; e++)
            {
                float bearing = 30f + 60f * ((startEdge + e) % 6);
                var camp = ctx.At(bearing, 0.66f);
                if (NearWadi(camp, 0.14f * s)) continue;
                var radial = Flat(camp - ctx.Centre).normalized;
                var along = new Vector3(radial.z, 0f, -radial.x);
                float w = ctx.Range(0.045f, 0.055f) * s, yaw = bearing + 90f + ctx.Range(-8f, 8f);
                for (int b = 0; b < 2; b++)
                {
                    var at = camp + along * ((b - 0.5f) * 1.04f * 1.35f * w);
                    if (ctx.PlaceByWidth(Tent, at, yaw, w, StandIn.Box, TentBlack, maxSlopeDeg: 40f)) taken.Add((at, 0.95f * w));
                }
                int goats = 0;
                for (int g = 0; g < 8 && goats < MaxGoats; g++)
                {
                    var at = camp - radial * (ctx.Range(0.10f, 0.16f) * s) + along * (ctx.Range(-0.12f, 0.12f) * s);
                    float h = ctx.Range(0.04f, 0.05f) * s, r = 0.05f * s;
                    if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                    if (ctx.PlaceByHeight(Goat, at, ctx.Range(0f, 360f), h, StandIn.Figure, GoatBlack, footprintShare: 1.0f, maxSlopeDeg: 45f))
                    {
                        taken.Add((at, r)); goats++;
                    }
                }
                break;
            }

            // 5. One or two acacias in the eastern (lower) half of a wadi bed.
            int acacias = 0;
            foreach (var line in wadis)
            {
                if (acacias >= MaxAcacias) break;
                int k = 12 + ctx.Rng.Next(6);
                var at = line[k] + new Vector3(0f, 0f, ctx.Range(-0.03f, 0.03f) * s);
                float h = ctx.Range(0.11f, 0.13f) * s, r = 0.075f * s;
                if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                if (ctx.PlaceByHeight(Acacia, at, ctx.Range(0f, 360f), h, StandIn.Tree, AcaciaGreen, footprintShare: 0.6f, maxSlopeDeg: 45f))
                {
                    taken.Add((at, r)); acacias++;
                }
            }

            // 6. Desert rocks: up to three large ones out on the ridges, then small ones, off the wadis and unit spot. Chalk
            //    and tan tints alternate, as the bands do.
            int rocks = 0, big = 0;
            for (int i = 0; i < 60 && rocks < MaxRocks; i++)
            {
                bool large = big < MaxBigRocks && i < 30;
                float w = (large ? ctx.Range(0.09f, 0.12f) : ctx.Range(0.035f, 0.06f)) * s, r = 0.95f * w;
                var at = large ? ctx.At(ctx.Range(0f, 360f), ctx.Range(0.45f, 0.68f)) : ctx.RandomPoint(0.04f * s);
                if (ctx.InUnitSpot(at, r) || NearWadi(at, r + 0.04f * s) || !Free(at, r + 0.02f * s)) continue;
                if (ctx.PlaceByWidth(Rock, at, ctx.Range(0f, 360f), w, StandIn.Rock, rocks % 2 == 0 ? Chalk : RockTan, maxSlopeDeg: 50f))
                {
                    taken.Add((at, r)); rocks++;
                    if (large) big++;
                }
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = Flat(b - a), ap = Flat(p - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (ap - ab * t).magnitude;
        }
    }
}
