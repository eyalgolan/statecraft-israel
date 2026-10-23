#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: the Hula valley's swamp and lake margin as it stood in 1948, before the drainage. Wet green
    /// ground, large stands of papyrus, reed beds round a few open pools of dark blue-green water, a small herd of water
    /// buffalo, and a raised dirt causeway track across. No settlement. A tile is static for the whole game span
    /// (15 May 1948 to 15 January 1952), so the swamp is drawn undrained throughout.
    ///
    /// Sources (checked 2026-10-19):
    /// - The Jewish National Fund drained the lake and swamp from 1951; the work was completed by 1958. The marsh was
    ///   thickly overgrown with papyrus, which residents harvested for matting; water buffalo had been kept there since
    ///   the eighth century, for milk and draught: https://en.wikipedia.org/wiki/Lake_Hula and
    ///   https://en.wikipedia.org/wiki/Hula_Valley
    /// - The reed beds and the causeway are a generic look for a marsh crossing, not a named place.
    ///
    /// Layout: the causeway runs between the fixed edge midpoints at bearings 270 and 90 (so like neighbours join), with a
    /// seeded bow; it is a wide band of dry plains ground with a dirt track on it. Pools are flats (the only flats; they
    /// sit low on flat ground, maxRiseM 120), at most four, of different sizes, checked pairwise against each other's
    /// half-diagonals so they never overlap, and kept out of the unit spot and off the causeway. Papyrus grows in tight
    /// clumps, reeds ring the pools. Tall pieces keep out of ctx.InUnitSpot. Every prop is a deliberate stand-in.
    ///
    /// Triangle budget by arithmetic (hard caps below; FlatSurface refuses nothing): 4 pools x 12 = 48; 72 papyrus +
    /// 44 reeds + 4 buffalo = 120 stand-ins x under 100 = under 12,000. Worst case under 12,100. The budget is 30,000, so a
    /// light papyrus model can replace the stand-in later.</summary>
    [HexPrototype("hula_swamp", Kind = "nature", Region = "north")]
    public sealed class HulaSwamp : IHexPrototype
    {
        public string Title => "Hula swamp and lake margin, before the drainage (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "grass";
        public int TriangleBudget => 30000;

        const string Papyrus = "tiles:hula_swamp/papyrus", Reed = "tiles:hula_swamp/reed", Buffalo = "tiles:hula_swamp/buffalo";
        const int MaxPools = 4, MaxPapyrus = 72, MaxReeds = 44, Herd = 4;
        const float Scale = HexScale.PieceScale;

        static readonly Color PoolWater = new Color(0.10f, 0.24f, 0.24f), PapyrusGreen = new Color(0.42f, 0.66f, 0.20f),
            ReedTan = new Color(0.58f, 0.60f, 0.34f), BuffaloGrey = new Color(0.17f, 0.16f, 0.15f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. Wet ground: lush green patches over the grass, thickest towards the pools (painted later round each pool).
            for (int k = 0; k < 7; k++)
                ctx.PaintGround("green_crop", ctx.RandomPoint(0.05f * s), ctx.Range(0f, 180f), ctx.Range(0.22f, 0.36f) * s,
                    ctx.Range(0.16f, 0.26f) * s, 0.5f, ctx.Range(0.45f, 0.7f));

            // 2. The causeway: fixed edge midpoints, a seeded bow; a raised band of drier ground carrying a dirt track.
            //    Its points are fixed here; its paint goes last (step 7), over the wet margins.
            Vector3 p0 = ctx.At(270f, 0.97f), p2 = ctx.At(90f, 0.97f);
            Vector3 north = (ctx.At(0f, 1f) - ctx.Centre) / s;
            Vector3 pc = ctx.Centre + north * (ctx.Range(-0.20f, 0.20f) * s);
            var road = new List<Vector3>();
            for (int k = 0; k <= 12; k++)
            {
                float t = k / 12f, u = 1f - t;
                road.Add(u * u * p0 + 2f * u * t * pc + t * t * p2);
            }
            bool NearRoad(Vector3 p, float within)
            {
                for (int k = 0; k < road.Count - 1; k++) if (DistToSegment(p, road[k], road[k + 1]) < within) return true;
                return false;
            }

            // 3. Open pools: flats of different sizes, each refused unless it clears the others, the unit spot and the road.
            var pools = new List<(Vector3 at, float yaw, float w, float d)>();
            float[] sizes = { 0.30f, 0.22f, 0.16f, 0.12f };
            for (int i = 0; i < sizes.Length && pools.Count < MaxPools; i++)
            {
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    float w = sizes[i] * s * ctx.Range(0.9f, 1.1f), d = w * ctx.Range(0.55f, 0.75f), yaw = ctx.Range(0f, 180f);
                    float half = 0.5f * Mathf.Sqrt(w * w + d * d);
                    var at = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.30f, 0.78f));
                    if (!ctx.Inside(at, half) || ctx.InUnitSpot(at, half + 0.03f * s) || NearRoad(at, half + 0.06f * s)) continue;
                    bool clear = true;
                    foreach (var q in pools)
                        if (Flat(at - q.at).magnitude < half + 0.5f * Mathf.Sqrt(q.w * q.w + q.d * q.d) + 0.08f * s) { clear = false; break; }
                    if (!clear) continue;
                    if (!ctx.PlaceFlat(at, yaw, w, d, PoolWater, 120f)) continue;
                    pools.Add((at, yaw, w, d));
                    ctx.PaintGround("green_crop", at, yaw, w + 0.12f * s, d + 0.12f * s, 0.4f, 0.85f);   // the wet margin
                    break;
                }
            }
            bool InPool(Vector3 p, float marginM)
            {
                foreach (var q in pools)
                {
                    var local = Quaternion.Euler(0f, -q.yaw, 0f) * Flat(p - q.at);
                    if (Mathf.Abs(local.x) < 0.5f * q.w + marginM && Mathf.Abs(local.z) < 0.5f * q.d + marginM) return true;
                }
                return false;
            }

            // 4. Reed beds: low tan-green tufts set just outside each pool's rim, along its long sides.
            int reeds = 0;
            foreach (var q in pools)
            {
                var rot = Quaternion.Euler(0f, q.yaw, 0f);
                for (int k = 0; k < 12 && reeds < MaxReeds; k++)
                {
                    float x = ctx.Range(-0.5f, 0.5f) * q.w, side = (k & 1) == 0 ? 1f : -1f;
                    var at = q.at + rot * new Vector3(x, 0f, side * (0.5f * q.d + 0.035f * s));
                    if (ctx.InUnitSpot(at, 0.03f * s) || NearRoad(at, 0.05f * s)) continue;
                    if (ctx.PlaceByHeight(Reed, at, ctx.Range(0f, 360f), ctx.Range(0.045f, 0.065f) * s, StandIn.Tree, ReedTan, 0.6f)) reeds++;
                }
            }

            // The buffalo wallow, chosen before the papyrus so the clumps keep off it.
            Vector3? wallow = null;
            if (pools.Count > 0)
                wallow = pools[0].at + Quaternion.Euler(0f, pools[0].yaw, 0f) * new Vector3(0.5f * pools[0].w + 0.12f * s, 0f, 0f);
            bool NearWallow(Vector3 p) => wallow is Vector3 wl && Flat(p - wl).magnitude < 0.20f * s;

            // 5. Papyrus: large, tight clumps of tall thin bright-green stems in the wet ground between the pools.
            int papyrus = 0;
            for (int clump = 0; clump < 30 && papyrus < MaxPapyrus; clump++)
            {
                var centre = ctx.RandomPoint(0.06f * s);
                if (ctx.InUnitSpot(centre, 0.10f * s) || NearRoad(centre, 0.12f * s) || InPool(centre, 0.10f * s) || NearWallow(centre)) continue;
                int n = 8 + ctx.Rng.Next(5);
                for (int k = 0; k < n && papyrus < MaxPapyrus; k++)
                {
                    var at = centre + new Vector3(ctx.Range(-0.08f, 0.08f) * s, 0f, ctx.Range(-0.08f, 0.08f) * s);
                    float h = ctx.Range(0.10f, 0.14f) * s;
                    if (ctx.InUnitSpot(at, 0.03f * s) || NearRoad(at, 0.05f * s) || InPool(at, 0.03f * s)) continue;
                    if (ctx.PlaceByHeight(Papyrus, at, ctx.Range(0f, 360f), h, StandIn.Pine, PapyrusGreen, 0.22f)) papyrus++;
                }
            }

            // 6. Life: a few water buffalo wallowing at the largest pool's edge (the first placed), one heading.
            if (wallow is Vector3 edge)
            {
                float heading = ctx.Range(0f, 360f);
                for (int b = 0; b < Herd; b++)
                {
                    var at = edge + new Vector3(ctx.Range(-0.06f, 0.06f) * s, 0f, ctx.Range(-0.06f, 0.06f) * s);
                    if (ctx.InUnitSpot(at, 0.06f * s) || InPool(at, 0.06f * s)) continue;
                    ctx.PlaceByHeight(Buffalo, at, heading + ctx.Range(-35f, 35f), 0.06f * s, StandIn.Figure, BuffaloGrey, 1.4f);
                }
            }

            // 7. The causeway's paint, last (paints apply in order).
            for (int k = 0; k < road.Count - 1; k++)
            {
                ctx.PaintStroke("plains", road[k], road[k + 1], 0.075f * s, 0.8f);
                ctx.PaintStroke("dirt", road[k], road[k + 1], 0.03f * s, 0.9f);
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
