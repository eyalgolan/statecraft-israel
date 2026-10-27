#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Old Jaffa, the hilltop port town (1948), drawn Civ-style as a city-centre hex.
    /// - The tile first finds the sea side: the bearing on which water comes nearest. With no water it falls back to west.
    /// - Ochre and limestone houses cluster tightly in three rings round a summit between the centre and the sea. St
    ///   Peter's church and its belfry crown the summit.
    /// - The terrain is Flat, so the mound is drawn by size. Houses are widest, and so tallest, in the inner ring and
    ///   smaller in each ring below, so the town steps up toward the church.
    /// - The first-batch stone-village cluster stands once on one flank of the hill.
    /// - At the centre is the market (the clock-tower square). It is painted dirt and kept open for a unit, with the
    ///   Ottoman clock tower at its northern edge.
    /// - Dirt lanes run up the hill, down to the harbour, inland and up the coast road, with date palms along the inland
    ///   road and painted gardens beyond.
    /// - Where the sea is near, the small harbour has up to two fishing boats on the shore, a stone quay and the Sea
    ///   Mosque's minaret.
    ///
    /// Triangle estimate, at the maximum counts:
    /// - At most 20 houses (sandbox:kit_jaffa*, up to 1,400 each; kit_jaffa shares kit_jerusalem_4's mesh) = 28,000.
    /// - stone-village-v1 once = 8,642.
    /// - At most 2 fishing boats x 13,437 = 26,874. They appear only on a hex that touches water, so the FlatSurface test
    ///   never places them.
    /// - At most 8 date palms x 284 = 2,272.
    /// - Church Box, and belfry, clock tower and minaret Pine stand-ins: about 160. Quay flat: 12.
    /// - Total about 66,000 of 70,000 with the harbour, about 39,100 without it.
    ///
    /// The clock tower, church, belfry and minaret have no models (tiles:jaffa_old_city/...), so they always draw stand-ins.</summary>
    [HexPrototype("jaffa_old_city", Kind = "city")]
    public sealed class JaffaOldCity : IHexPrototype
    {
        public string Title => "Jaffa, the old port town (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 125000;   // the shipped b*-v2 buildings at full detail (Tal, 19 Oct 2026): measured 113k on the israel board and the test hex

        const string House = "sandbox:kit_jaffa*", Cluster = "proto:stone-village-v1", Boat = "proto:vehicle-fishing-boat-1948-v2",
            Palm = "sandbox:date_palm", ClockTower = "tiles:jaffa_old_city/clock_tower", Church = "tiles:jaffa_old_city/st_peter",
            Belfry = "tiles:jaffa_old_city/belfry", Minaret = "tiles:jaffa_old_city/sea_minaret";

        static readonly Color Ochre = new Color(0.84f, 0.68f, 0.46f), Stone = new Color(0.86f, 0.80f, 0.68f),
            TowerStone = new Color(0.80f, 0.73f, 0.58f), Quay = new Color(0.70f, 0.65f, 0.55f), Hull = new Color(0.45f, 0.33f, 0.22f);

        const int MaxHouses = 20, MaxBoats = 2, MaxPalms = 8;
        const float LaneHalf = 0.025f;
        /// <summary>The coast road leaves the summit this many degrees off the bearing to the sea.</summary>
        const float CoastTurnDeg = 100;

        /// <summary>The house rings about the summit: radius, slots, and footprint width range (shares of S). Each ring is
        /// smaller than the one inside it, so the town reads as a mound.</summary>
        static readonly (float r, int n, float wMin, float wMax)[] Rings =
        {
            (0.15f, 5, 0.13f, 0.14f), (0.28f, 10, 0.12f, 0.13f), (0.40f, 14, 0.105f, 0.115f),
        };

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            var taken = new List<(Vector3 at, float r)>();
            var lanes = new List<(Vector3 a, Vector3 b)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                return true;
            }
            bool OffLanes(Vector3 at, float r)
            {
                foreach (var l in lanes) if (SegmentDistance(at, l.a, l.b) < r + LaneHalf * s) return false;
                return true;
            }
            void Lane(Vector3 a, Vector3 b) { lanes.Add((a, b)); ctx.PaintStroke("dirt", a, b, 2f * LaneHalf * s, 0.9f); }
            bool NearerNorth(float a, float b) => Mathf.Abs(Mathf.DeltaAngle(a, 0f)) <= Mathf.Abs(Mathf.DeltaAngle(b, 0f));

            // 1. The sea side: the bearing on which water comes nearest the centre. With no water, west.
            float seaB = 270f, shoreR = 1f;
            bool sea = false;
            for (int i = 0; i < 24; i++)
            {
                float b = 15f * i;
                for (float r = 0.30f; r < shoreR && r <= 0.90f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(b, r))) { seaB = b; shoreR = r; sea = true; break; }
            }

            // 2. The market at the centre: the clock-tower square, painted dirt and kept open for a unit. The clock tower
            //    stands at the square's inland corner nearer north, so it does not hide a unit from the camera.
            taken.Add((ctx.Centre, 0.15f * s));
            ctx.PaintGround("dirt", ctx.Centre, seaB, 0.30f * s, 0.30f * s, 0.2f, 0.95f);
            float clockB = NearerNorth(seaB + 130f, seaB - 130f) ? seaB + 130f : seaB - 130f;
            var clock = ctx.At(clockB, 0.19f);
            ctx.PlaceByHeight(ClockTower, clock, clockB + 180f, 0.20f * s, StandIn.Pine, TowerStone, footprintShare: 0.2f);
            taken.Add((clock, 0.05f * s));

            // 3. The harbour. On a few bearings either side of the sea direction, step out until water shows just ahead,
            //    then try that spot and up to two steps back for a fishing boat on the shore. Boats are kept apart. Behind
            //    the first boat go a stone quay, the lane down from the market, and the Sea Mosque's minaret.
            int boats = 0;
            if (sea)
                foreach (float d in new[] { 30f, -30f, 50f, -50f, 15f, -15f, 0f })
                {
                    if (boats >= MaxBoats) break;
                    float b = seaB + d;
                    for (float r = 0.30f; r <= 0.85f; r += 0.03f)
                    {
                        if (!ctx.IsWater(ctx.At(b, r + 0.09f))) continue;
                        for (int back = 0; back < 3; back++)
                        {
                            float rr = r - 0.03f * back;
                            var at = ctx.At(b, rr);
                            if (!Free(at, 0.07f * s)) break;
                            if (!ctx.PlaceByWidth(Boat, at, b + ctx.Range(-15f, 15f), 0.13f * s, StandIn.Box, Hull)) continue;
                            if (boats == 0)
                            {
                                var quay = ctx.At(b, rr - 0.10f);
                                if (ctx.PlaceFlat(quay, b, 0.20f * s, 0.05f * s, Quay)) taken.Add((quay, 0.10f * s));
                                Lane(ctx.Centre, quay);
                                var minaret = ctx.At(b - 25f, rr - 0.13f);
                                if (Free(minaret, 0.03f * s) && ctx.PlaceByHeight(Minaret, minaret, 0f, 0.13f * s, StandIn.Pine, TowerStone, footprintShare: 0.2f))
                                    taken.Add((minaret, 0.04f * s));
                            }
                            taken.Add((at, 0.08f * s));
                            boats++;
                            break;
                        }
                        break;
                    }
                }

            // 4. The summit, between the market and the sea, carrying St Peter's church and its belfry. Lanes run up to it,
            //    inland, and along the coast road (on the side nearer north).
            float topR = sea ? Mathf.Clamp(shoreR - 0.26f, 0.30f, 0.40f) : 0.36f;
            var top = ctx.At(seaB, topR);
            ctx.PlaceByWidth(Church, top, seaB + 180f, 0.15f * s, StandIn.Box, Stone);
            ctx.PlaceByHeight(Belfry, top + Dir(seaB + 90f) * (0.06f * s), 0f, 0.17f * s, StandIn.Pine, TowerStone, footprintShare: 0.2f);
            taken.Add((top, 0.065f * s));
            float coastB = NearerNorth(seaB + CoastTurnDeg, seaB - CoastTurnDeg) ? seaB + CoastTurnDeg : seaB - CoastTurnDeg;
            Lane(ctx.Centre, top);
            Lane(ctx.Centre, ctx.At(seaB + 180f, 0.90f));
            Lane(ctx.Centre, ctx.At(coastB, 0.90f));

            // 5. The stone-village cluster, once, on the flank of the hill away from the coast road.
            float clusterB = coastB == seaB + CoastTurnDeg ? seaB - 90f : seaB + 90f;
            var clusterAt = top + Dir(clusterB) * (0.30f * s);
            if (OffLanes(clusterAt, 0.08f * s) && ctx.PlaceByWidth(Cluster, clusterAt, clusterB + 180f, 0.16f * s, StandIn.Box, Stone))
                taken.Add((clusterAt, 0.08f * s));

            // 6. Houses in three rings about the summit, each turned to face it. Each ring is filled from the inland side
            //    outward in both directions (slot order 0, -1, 1, -2, 2 ...), so the cap trims the seaward end evenly.
            //    Slots on a lane, in the market or on any piece already placed are skipped.
            int houses = 0;
            foreach (var ring in Rings)
                for (int k = 0; k < ring.n; k++)
                {
                    int m = (k + 1) / 2 * (k % 2 == 0 ? 1 : -1);
                    float b = seaB + 180f + m * (360f / ring.n) + ctx.Range(-5f, 5f);
                    var at = top + Dir(b) * ((ring.r + ctx.Range(-0.01f, 0.01f)) * s);
                    float w = ctx.Range(ring.wMin, ring.wMax) * s, yaw = b + 180f + ctx.Range(-10f, 10f);
                    var tint = ctx.Rng.Next(3) == 0 ? Stone : Ochre;
                    if (houses >= MaxHouses || !Free(at, 0.5f * w) || !OffLanes(at, 0.5f * w)) continue;
                    if (ctx.PlaceByWidth(House, at, yaw, w, StandIn.Box, tint)) houses++;
                }

            // 7. Date palms: two at the market's inland corners, then pairs along the inland road.
            int palms = 0;
            var palmSpots = new List<Vector3> { ctx.At(seaB + 155f, 0.22f), ctx.At(seaB + 205f, 0.22f) };
            foreach (float r in new[] { 0.42f, 0.58f, 0.74f })
                foreach (float side in new[] { -90f, 90f })
                    palmSpots.Add(ctx.At(seaB + 180f, r) + Dir(seaB + 180f + side) * (0.055f * s));
            foreach (var at in palmSpots)
            {
                if (palms >= MaxPalms || !Free(at, 0.02f * s)) continue;
                if (ctx.PlaceByHeight(Palm, at, ctx.Range(0f, 360f), ctx.Range(0.11f, 0.13f) * s, StandIn.Palm, null, 0.3f)) palms++;
            }

            // 8. The gardens and orchards inland of the town, painted into the plains on either side of the inland road.
            foreach (float side in new[] { -55f, 55f })
                ctx.PaintGround("green_crop", ctx.At(seaB + 180f + side, 0.60f), seaB + 180f + side, 0.22f * s, 0.15f * s, 0.3f, 0.6f);
        }

        static Vector3 Dir(float bearingDeg)
        {
            float a = bearingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        static float Planar(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>Distance from p to the segment a-b, in the ground plane.</summary>
        static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 q = new Vector2(p.x, p.z), u = new Vector2(a.x, a.z), ab = new Vector2(b.x - a.x, b.z - a.z);
            float t = Mathf.Clamp01(Vector2.Dot(q - u, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (q - (u + ab * t)).magnitude;
        }
    }
}
