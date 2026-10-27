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
    /// no flats; about 51k of 60k.
    ///
    /// The regional forest variants (Forest&lt;Region&gt;&lt;A|B&gt;) subclass it through the protected virtual members: the
    /// stand's tree, the fill and margin trees, the caps, the row grid, the track and the clearings' ground. Their defaults
    /// are this tile's own, so pine_forest itself builds exactly as before (HexVariantTests).</summary>
    [HexPrototype("pine_forest", Kind = "forest")]
    public class PineForest : IHexPrototype
    {
        public virtual string Title => "JNF pine forest, foothills east of Tel Aviv (1948)";
        public virtual HexTerrain Terrain => HexTerrain.Hills;
        public virtual string Ground => "hills";
        public virtual int TriangleBudget => HexScale.BudgetForest;

        // Models: EZ-Tree trees and Poly Haven rocks are local-only for now; the stand-ins draw without them.
        protected const string Pine = "sandbox:aleppo_pine_lite", Oak = "sandbox:tabor_oak_lite", Carob = "sandbox:carob_lite", Rock = "sandbox:limestone*";

        protected static readonly Color PineGreen = new Color(0.20f, 0.33f, 0.18f), OakGreen = new Color(0.36f, 0.44f, 0.23f),
            CarobGreen = new Color(0.27f, 0.39f, 0.20f), Limestone = new Color(0.86f, 0.83f, 0.74f);

        /// <summary>One tree to place: model, stand-in, tint, height range (shares of S) and footprint share.</summary>
        protected readonly struct Tree
        {
            public readonly string Model; public readonly StandIn StandIn; public readonly Color Tint; public readonly float Min, Max, Footprint;
            public Tree(string model, StandIn standIn, Color tint, float min, float max, float footprint)
            { Model = model; StandIn = standIn; Tint = tint; Min = min; Max = max; Footprint = footprint; }
        }

        // Caps that keep the tile inside its budget whatever the models' exact counts.
        protected virtual int MaxPines => 22;
        protected virtual int MaxBroadleaf => 9;
        protected virtual int MaxFill => 26;
        /// <summary>The planted stand's tree (the first MaxPines cells, in a seeded order).</summary>
        protected virtual Tree StandTree(HexTileContext ctx) => new Tree(Pine, StandIn.Pine, PineGreen, 0.16f, 0.20f, 0.3f);
        /// <summary>The rest of the stand closes into canopy with round-crowned native trees (light: ~650 triangles each).</summary>
        protected virtual Tree FillTree(HexTileContext ctx) => new Tree(ctx.Rng.Next(3) == 0 ? Carob : Oak, StandIn.Tree, OakGreen, 0.13f, 0.16f, 0.45f);
        /// <summary>A margin tree: Tabor oak about two in three, carob the rest.</summary>
        protected virtual Tree MarginTree(HexTileContext ctx)
        {
            bool oak = ctx.Rng.Next(3) != 0;
            return new Tree(oak ? Oak : Carob, StandIn.Tree, oak ? OakGreen : CarobGreen, 0.14f, 0.17f, 0.45f);
        }
        /// <summary>The track's bearing, drawn in TrackBearing ± 15 degrees.</summary>
        protected virtual float TrackBearing => 80f;
        protected virtual string TrackLayer => "dirt";
        /// <summary>The clearings' painted ground, and their radii as shares of S.</summary>
        protected virtual string ClearingLayer => "plains";
        protected virtual (float big, float small) ClearingRadii => (0.16f, 0.12f);
        /// <summary>The rows' turn, drawn in this range, and the gaps between rows and between trees, as shares of S.</summary>
        protected virtual (float min, float max) RowYaw => (15f, 40f);
        protected virtual float RowGap => 0.17f;
        protected virtual float TreeGap => 0.16f;
        /// <summary>Position jitter of a stand cell, as a share of S.</summary>
        protected virtual float Jitter => 0.03f;
        /// <summary>Whether the clearings carry limestone outcrops.</summary>
        protected virtual string? OutcropModel => Rock;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The track: a sine-wiggled line across the hex, roughly west (Tel Aviv) to east, painted as dirt strokes.
            float trackBearing = TrackBearing + ctx.Range(-15f, 15f), amp = ctx.Range(0.10f, 0.15f) * s, phase = ctx.Range(0f, 6.283f);
            var along = new Vector3(Mathf.Sin(trackBearing * Mathf.Deg2Rad), 0f, Mathf.Cos(trackBearing * Mathf.Deg2Rad));
            var side = new Vector3(along.z, 0f, -along.x);
            Vector3 TrackAt(float t) => ctx.Centre + along * (t * s) + side * (amp * Mathf.Sin(2.6f * t + phase));
            var track = new List<Vector3>();
            for (int k = 0; k <= 16; k++) track.Add(TrackAt(-0.95f + k * (1.9f / 16f)));
            for (int k = 0; k < track.Count - 1; k++) ctx.PaintStroke(TrackLayer, track[k], track[k + 1], 0.035f * s, 0.85f);

            // 2. Two clearings, one each side of the track, pulled inwards until they sit in the hex, painted as dry grass.
            var (bigR, smallR) = ClearingRadii;
            var clearings = new[]
            {
                (centre: TrackAt(ctx.Range(-0.40f, -0.15f)) + side * (0.22f * s), radius: bigR * s),
                (centre: TrackAt(ctx.Range(0.20f, 0.42f)) - side * (0.20f * s), radius: smallR * s),
            };
            for (int c = 0; c < clearings.Length; c++)
            {
                var at = clearings[c].centre;
                for (int i = 0; i < 6 && !ctx.Inside(at, clearings[c].radius * 0.6f); i++) at = Vector3.Lerp(at, ctx.Centre, 0.3f);
                clearings[c].centre = at;
                ctx.PaintGround(ClearingLayer, at, ctx.Range(0f, 90f), 1.3f * clearings[c].radius, 1.3f * clearings[c].radius, 0.4f, 0.8f);
            }

            // 3. Outcrops in the clearings: two in the larger, one in the smaller (none in the unit spot).
            string? outcrop = OutcropModel;
            for (int c = 0; c < clearings.Length; c++)
                for (int k = 0; k < 2 - c; k++)
                {
                    var at = clearings[c].centre + Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f) * new Vector3(0f, 0f, ctx.Range(0.2f, 0.5f) * clearings[c].radius);
                    float yaw = ctx.Range(0f, 360f), width = ctx.Range(0.06f, 0.09f) * s;   // drawn even when skipped, so the sequence holds
                    if (outcrop == null || ctx.InUnitSpot(at, 0.04f * s)) continue;
                    ctx.PlaceByWidth(outcrop, at, yaw, width, StandIn.Rock, Limestone, 40f);
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
            //     before the rows, so no stand tree is planted on them.
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

            // 4b. The planted stands: staggered contour rows, each cell jittered; cells in the unit spot stay empty. Stand
            //    trees go in a seeded order until the cap.
            var (yawMin, yawMax) = RowYaw;
            float rowYaw = ctx.Range(yawMin, yawMax), rowGap = RowGap * s, treeGap = TreeGap * s, jitter = Jitter;
            var turn = Quaternion.Euler(0f, rowYaw, 0f);
            var cells = new List<(Vector3 at, double key)>();
            for (int j = -5; j <= 5; j++)
                for (int i = -5; i <= 5; i++)
                {
                    float x = (i + (j & 1) * 0.5f) * treeGap, z = j * rowGap;
                    var at = ctx.Centre + turn * new Vector3(x, 0f, z)
                        + new Vector3(ctx.Range(-jitter, jitter) * s, 0f, ctx.Range(-jitter, jitter) * s);
                    double key = ctx.Rng.NextDouble();
                    if (!ctx.Inside(at, 0.04f * s) || ctx.InUnitSpot(at, 0.04f * s) || InClearing(at, 0.06f * s) || NearTrack(at, 0.075f * s)) continue;
                    bool shaded = !NearMargin(at);   // a broadleaf spot keeps its floor but takes no stand tree
                    cells.Add((at, shaded ? key : 2.0));
                }

            var order = new int[cells.Count];
            for (int k = 0; k < order.Length; k++) order[k] = k;
            Array.Sort(order, (a, b) => cells[a].key != cells[b].key ? cells[a].key.CompareTo(cells[b].key) : a.CompareTo(b));
            int pines = 0, fill = 0, maxPines = MaxPines, maxFill = MaxFill;
            foreach (int k in order)
            {
                if (cells[k].key > 1.0) break;
                if (pines < maxPines)
                {
                    var t = StandTree(ctx);
                    if (ctx.PlaceByHeight(t.Model, cells[k].at, ctx.Range(0f, 360f), ctx.Range(t.Min, t.Max) * s, t.StandIn, t.Tint, t.Footprint)) pines++;
                }
                else if (fill < maxFill)
                {
                    var t = FillTree(ctx);
                    if (ctx.PlaceByHeight(t.Model, cells[k].at, ctx.Range(0f, 360f), ctx.Range(t.Min, t.Max) * s, t.StandIn, t.Tint, t.Footprint)) fill++;
                }
            }

            // 5. Native trees at the margins (spots chosen in step 4a).
            int broadleaf = 0, maxBroadleaf = MaxBroadleaf;
            foreach (var at in margins)
            {
                if (broadleaf >= maxBroadleaf) break;
                if (NearTrack(at, 0.05f * s) || ctx.InUnitSpot(at, 0.04f * s)) continue;
                var t = MarginTree(ctx);
                if (ctx.PlaceByHeight(t.Model, at, ctx.Range(0f, 360f), ctx.Range(t.Min, t.Max) * s, t.StandIn, t.Tint, t.Footprint))
                    broadleaf++;
            }
            Dress(ctx);
        }

        /// <summary>Anything a variant adds last: nothing here.</summary>
        protected virtual void Dress(HexTileContext ctx) { }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = Flat(b - a), ap = Flat(p - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (ap - ab * t).magnitude;
        }
    }
}
