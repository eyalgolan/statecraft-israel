#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The Arava's wide gravel plain, 1948: flat sand (desert) streaked with paler wind-blown sand (beach) and
    /// broad gravel sheets (hills, with mountain at low strength; there is no gravel layer). A braided wadi bed runs roughly
    /// north to south down one side of the hex, several thin dirt channels with a pale bed, and two shorter wadis cross the
    /// plain to join it. Acacia savanna: acacias in loose groups of one to three along the wadis, the groups widely spaced.
    /// A few small reddish rocks lie on the gravel. The mountains on either side of the valley are off the hex and are not
    /// drawn. No settlement; the unit spot stays open.
    ///
    /// Spacing (PieceScale 1.35): an acacia asked 0.12 to 0.15 S tall has a footprint of about 0.11 S, so trees keep
    /// 0.08 S of clearance each (0.16 S apart at least), groups keep 0.28 S apart, and every piece is tested with InUnitSpot.
    ///
    /// Worst-case triangles on a flat surface (caps below; local meshes counted from their OBJ files on 19 October 2026):
    /// 12 acacias x 1,164 = 13,968. 4 desert rocks x 700 = 2,800. Total about 16,770; budget 19,000.</summary>
    [HexPrototype("arava_acacia", Kind = "nature", Region = "south")]
    public sealed class AravaAcacia : IHexPrototype
    {
        public string Title => "Arava plain: acacia savanna and braided wadis (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "desert";
        public int TriangleBudget => 19000;

        const string Acacia = "sandbox:acacia_lite", Rock = "sandbox:desert_rock*";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxAcacias = 12, MaxGroups = 6, MaxRocks = 4;

        static readonly Color AcaciaGreen = new Color(0.46f, 0.50f, 0.30f), RockRed = new Color(0.66f, 0.50f, 0.40f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. Ground: sand to the rim; broad gravel sheets; a few small darker gravel patches; pale sand streaks along
            //    one (north-south) wind.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.8f * s, 2.0f * s, 0.2f, 0.8f);
            for (int k = 0; k < 4; k++)
                ctx.PaintGround("hills", ctx.RandomPoint(0.15f * s), ctx.Range(0f, 180f), ctx.Range(0.40f, 0.60f) * s, ctx.Range(0.30f, 0.45f) * s, 0.5f, ctx.Range(0.4f, 0.55f));
            for (int k = 0; k < 3; k++)
                ctx.PaintGround("mountain", ctx.RandomPoint(0.1f * s), ctx.Range(0f, 180f), ctx.Range(0.15f, 0.25f) * s, ctx.Range(0.10f, 0.18f) * s, 0.5f, 0.3f);
            float wind = ctx.Range(-15f, 15f);
            for (int k = 0; k < 4; k++)
                ctx.PaintGround("beach", ctx.RandomPoint(0.08f * s), wind + ctx.Range(-8f, 8f), ctx.Range(0.05f, 0.08f) * s, ctx.Range(0.30f, 0.50f) * s, 0.5f, 0.45f);

            // 2. The braided wadi: a main bed roughly north-south, 0.30 to 0.38 S east or west of the centre, with three
            //    braids that part from it and rejoin; then two shorter wadis from the far side that join it.
            float axis = ctx.Range(-20f, 20f), side = ctx.Rng.Next(2) == 0 ? -1f : 1f;
            var along = Quaternion.Euler(0f, axis, 0f) * Vector3.forward;
            var lateral = new Vector3(along.z, 0f, -along.x);
            float offset = side * ctx.Range(0.30f, 0.38f) * s, amp = ctx.Range(0.03f, 0.05f) * s, phase = ctx.Range(0f, 6.283f);
            Vector3 Main(float t) => ctx.Centre + lateral * (offset + amp * Mathf.Sin(3.2f * t + phase)) + along * (t * s);
            var wadis = new List<List<Vector3>>();
            var main = new List<Vector3>();
            for (int k = 0; k <= 20; k++) main.Add(Main(-1f + 0.1f * k));
            wadis.Add(main);
            for (int j = 0; j < 3; j++)
            {
                float t0 = -0.9f + 0.6f * j + ctx.Range(-0.1f, 0.1f), t1 = t0 + ctx.Range(0.35f, 0.5f);
                float lens = (j % 2 == 0 ? 1f : -1f) * ctx.Range(0.05f, 0.08f) * s;
                var braid = new List<Vector3>();
                for (int k = 0; k <= 8; k++)
                {
                    float u = k / 8f;
                    braid.Add(Main(Mathf.Lerp(t0, t1, u)) + lateral * (lens * Mathf.Sin(Mathf.PI * u)));
                }
                wadis.Add(braid);
            }
            var tributaries = new List<List<Vector3>>();
            for (int j = 0; j < 2; j++)
            {
                float tj = (j == 0 ? -0.45f : 0.4f) + ctx.Range(-0.1f, 0.1f);
                var head = ctx.Centre - lateral * (side * 0.9f * s) + along * ((tj + ctx.Range(-0.25f, 0.25f)) * s);
                var mouth = Main(tj);
                var bow = Vector3.Lerp(head, mouth, 0.5f) + along * (ctx.Range(-0.12f, 0.12f) * s);
                var trib = new List<Vector3>();
                for (int k = 0; k <= 10; k++) trib.Add(Bezier(head, bow, mouth, k / 10f));
                wadis.Add(trib);
                tributaries.Add(trib);
            }
            for (int w = 0; w < wadis.Count; w++)
            {
                var line = wadis[w];
                float width = w == 0 ? 0.035f : (tributaries.Contains(line) ? 0.02f : 0.024f);
                for (int k = 0; k < line.Count - 1; k++) ctx.PaintStroke("dirt", line[k], line[k + 1], width * s, w == 0 ? 0.7f : 0.55f);
            }
            for (int k = 0; k < main.Count - 1; k++) ctx.PaintStroke("beach", main[k], main[k + 1], 0.015f * s, 0.6f);
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

            // 3. Acacia groups along the wadis: candidate spots every few points along the main bed and the tributaries,
            //    taken from a seeded start, each on a bank and at least 0.28 S from the last groups; one to three trees in
            //    a loose cluster about it.
            var spots = new List<Vector3>();
            for (int k = 1; k < main.Count - 1; k += 3)
            {
                var dirK = Flat(main[k + 1] - main[k]).normalized;
                spots.Add(main[k] + new Vector3(dirK.z, 0f, -dirK.x) * (((k / 3) % 2 == 0 ? 1f : -1f) * 0.07f * s));
            }
            foreach (var trib in tributaries)
                for (int k = 2; k < trib.Count - 1; k += 4)
                {
                    var dirK = Flat(trib[k + 1] - trib[k]).normalized;
                    spots.Add(trib[k] + new Vector3(dirK.z, 0f, -dirK.x) * (0.06f * s));
                }
            var groups = new List<Vector3>();
            int acacias = 0, first = ctx.Rng.Next(spots.Count);
            for (int i = 0; i < spots.Count && groups.Count < MaxGroups && acacias < MaxAcacias; i++)
            {
                var spot = spots[(first + i) % spots.Count];
                bool apart = true;
                foreach (var g in groups) if (Flat(spot - g).magnitude < 0.28f * s) { apart = false; break; }
                if (!apart || ctx.InUnitSpot(spot, 0.08f * s)) continue;
                int want = 1 + ctx.Rng.Next(3), placed = 0;
                for (int k = 0; k < 6 && placed < want && acacias < MaxAcacias; k++)
                {
                    var at = k == 0 ? spot : spot + Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f) * Vector3.forward * (ctx.Range(0.16f, 0.20f) * s);
                    float h = ctx.Range(0.12f, 0.15f) * s, r = 0.08f * s;
                    if (ctx.InUnitSpot(at, r) || !Free(at, r)) continue;
                    if (ctx.PlaceByHeight(Acacia, at, ctx.Range(0f, 360f), h, StandIn.Tree, AcaciaGreen, footprintShare: 0.6f))
                    {
                        taken.Add((at, r)); placed++; acacias++;
                    }
                }
                if (placed > 0) groups.Add(spot);
            }

            // 4. A few small reddish rocks on the gravel, off the wadis, trees and unit spot.
            int rocks = 0;
            for (int i = 0; i < 30 && rocks < MaxRocks; i++)
            {
                float w = ctx.Range(0.03f, 0.05f) * s, r = 0.95f * w;
                var at = ctx.RandomPoint(0.05f * s);
                if (ctx.InUnitSpot(at, r) || NearWadi(at, r + 0.03f * s) || !Free(at, r + 0.02f * s)) continue;
                if (ctx.PlaceByWidth(Rock, at, ctx.Range(0f, 360f), w, StandIn.Rock, RockRed)) { taken.Add((at, r)); rocks++; }
            }
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
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
