#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>JNF pine forest on the first foothills east of Tel Aviv, 1948 (the Ben Shemen plantings): Aleppo pines in
    /// planted, slightly irregular contour rows over most of the hex, closed into one canopy with native fill; two grassy
    /// clearings with limestone outcrops; native Tabor oaks and carobs at the margins; a dirt forest track winding west to
    /// east (Tel Aviv side to the hills). The clearings and the track are painted into the ground (no flats), and tall
    /// pieces keep out of the unit spot (ctx.InUnitSpot).
    /// Triangle budget by arithmetic (caps below): 27 pines x ~1600 + 9 broadleaf x ~650 + 3 rocks x ~700 = 51,150, and
    /// no flats; about 51k of 60k.</summary>
    [HexPrototype("pine_forest")]
    public sealed class PineForest : IHexPrototype
    {
        public string Title => "JNF pine forest, foothills east of Tel Aviv (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => HexScale.BudgetForest;

        // Models: EZ-Tree trees and Poly Haven rocks are local-only for now; the stand-ins draw without them.
        const string Pine = "sandbox:aleppo_pine_lite", Oak = "sandbox:tabor_oak_lite", Carob = "sandbox:carob_lite", Rock = "sandbox:limestone*";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxPines = 22, MaxBroadleaf = 9, MaxFill = 26;

        static readonly Color PineGreen = new Color(0.20f, 0.33f, 0.18f), OakGreen = new Color(0.36f, 0.44f, 0.23f),
            CarobGreen = new Color(0.27f, 0.39f, 0.20f), Limestone = new Color(0.86f, 0.83f, 0.74f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The track: a sine-wiggled line across the hex, roughly west (Tel Aviv) to east, painted as dirt strokes.
            float trackBearing = 80f + ctx.Range(-15f, 15f), amp = ctx.Range(0.10f, 0.15f) * s, phase = ctx.Range(0f, 6.283f);
            var along = new Vector3(Mathf.Sin(trackBearing * Mathf.Deg2Rad), 0f, Mathf.Cos(trackBearing * Mathf.Deg2Rad));
            var side = new Vector3(along.z, 0f, -along.x);
            Vector3 TrackAt(float t) => ctx.Centre + along * (t * s) + side * (amp * Mathf.Sin(2.6f * t + phase));
            var track = new List<Vector3>();
            for (int k = 0; k <= 16; k++) track.Add(TrackAt(-0.95f + k * (1.9f / 16f)));
            for (int k = 0; k < track.Count - 1; k++) ctx.PaintStroke("dirt", track[k], track[k + 1], 0.035f * s, 0.85f);

            // 2. Two clearings, one each side of the track, pulled inwards until they sit in the hex, painted as dry grass.
            var clearings = new[]
            {
                (centre: TrackAt(ctx.Range(-0.40f, -0.15f)) + side * (0.22f * s), radius: 0.16f * s),
                (centre: TrackAt(ctx.Range(0.20f, 0.42f)) - side * (0.20f * s), radius: 0.12f * s),
            };
            for (int c = 0; c < clearings.Length; c++)
            {
                var at = clearings[c].centre;
                for (int i = 0; i < 6 && !ctx.Inside(at, clearings[c].radius * 0.6f); i++) at = Vector3.Lerp(at, ctx.Centre, 0.3f);
                clearings[c].centre = at;
                ctx.PaintGround("plains", at, ctx.Range(0f, 90f), 1.3f * clearings[c].radius, 1.3f * clearings[c].radius, 0.4f, 0.8f);
            }

            // 3. Limestone outcrops in the clearings: two in the larger, one in the smaller (none in the unit spot).
            for (int c = 0; c < clearings.Length; c++)
                for (int k = 0; k < 2 - c; k++)
                {
                    var at = clearings[c].centre + Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f) * new Vector3(0f, 0f, ctx.Range(0.2f, 0.5f) * clearings[c].radius);
                    float yaw = ctx.Range(0f, 360f), width = ctx.Range(0.06f, 0.09f) * s;   // drawn even when skipped, so the sequence holds
                    if (ctx.InUnitSpot(at, 0.04f * s)) continue;
                    ctx.PlaceByWidth(Rock, at, yaw, width, StandIn.Rock, Limestone, 40f);
                }

            bool InClearing(Vector3 p, float extra)
            {
                foreach (var c in clearings) if (Flat(p - c.centre).magnitude < c.radius + extra) return true;
                return false;
            }
            bool NearTrack(Vector3 p, float within)
            {
                for (int k = 0; k < track.Count - 1; k++) if (DistToSegment(p, track[k], track[k + 1]) < within) return true;
                return false;
            }

            // 4a. Spots for the native broadleaf at the margins: around the clearings' rims and the forest edge. Chosen
            //     before the rows, so no pine is planted on them.
            var margins = new List<Vector3>();
            foreach (var c in clearings)
            {
                float start = ctx.Range(0f, 360f);
                for (int k = 0; k < 3; k++)
                    margins.Add(c.centre + Quaternion.Euler(0f, start + 120f * k + ctx.Range(-25f, 25f), 0f) * new Vector3(0f, 0f, c.radius * 0.95f));
            }
            for (int k = 0; k < 6; k++) margins.Add(ctx.At(60f * k + ctx.Range(10f, 50f), ctx.Range(0.62f, 0.72f)));
            bool NearMargin(Vector3 p)
            {
                foreach (var m in margins) if (Flat(p - m).magnitude < 0.08f * s) return true;
                return false;
            }

            // 4b. The planted stands: staggered contour rows, each cell jittered; cells in the unit spot stay empty. Pines
            //    go in a seeded order until the cap.
            float rowYaw = ctx.Range(15f, 40f), rowGap = 0.17f * s, treeGap = 0.16f * s;
            var turn = Quaternion.Euler(0f, rowYaw, 0f);
            var cells = new List<(Vector3 at, double key)>();
            for (int j = -5; j <= 5; j++)
                for (int i = -5; i <= 5; i++)
                {
                    float x = (i + (j & 1) * 0.5f) * treeGap, z = j * rowGap;
                    var at = ctx.Centre + turn * new Vector3(x, 0f, z)
                        + new Vector3(ctx.Range(-0.03f, 0.03f) * s, 0f, ctx.Range(-0.03f, 0.03f) * s);
                    double key = ctx.Rng.NextDouble();
                    if (!ctx.Inside(at, 0.04f * s) || ctx.InUnitSpot(at, 0.04f * s) || InClearing(at, 0.06f * s) || NearTrack(at, 0.075f * s)) continue;
                    bool shaded = !NearMargin(at);   // a broadleaf spot keeps its floor but takes no pine
                    cells.Add((at, shaded ? key : 2.0));
                }

            var order = new int[cells.Count];
            for (int k = 0; k < order.Length; k++) order[k] = k;
            Array.Sort(order, (a, b) => cells[a].key != cells[b].key ? cells[a].key.CompareTo(cells[b].key) : a.CompareTo(b));
            int pines = 0, fill = 0;
            foreach (int k in order)
            {
                if (cells[k].key > 1.0) break;
                if (pines < MaxPines)
                {
                    if (ctx.PlaceByHeight(Pine, cells[k].at, ctx.Range(0f, 360f), ctx.Range(0.16f, 0.20f) * s, StandIn.Pine, PineGreen, 0.3f)) pines++;
                }
                // The rest of the stand closes into canopy with round-crowned native trees (light: ~650 triangles each).
                else if (fill < MaxFill && ctx.PlaceByHeight(ctx.Rng.Next(3) == 0 ? Carob : Oak, cells[k].at, ctx.Range(0f, 360f), ctx.Range(0.13f, 0.16f) * s, StandIn.Tree, OakGreen, 0.45f))
                    fill++;
            }

            // 5. Native broadleaf at the margins (spots chosen in step 4a): oak or carob.
            int broadleaf = 0;
            foreach (var at in margins)
            {
                if (broadleaf >= MaxBroadleaf) break;
                if (NearTrack(at, 0.05f * s) || ctx.InUnitSpot(at, 0.04f * s)) continue;
                bool oak = ctx.Rng.Next(3) != 0;   // Tabor oak about two in three, carob the rest
                if (ctx.PlaceByHeight(oak ? Oak : Carob, at, ctx.Range(0f, 360f), ctx.Range(0.14f, 0.17f) * s, StandIn.Tree, oak ? OakGreen : CarobGreen, 0.45f))
                    broadleaf++;
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
