#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>The fallback shape drawn when a model is not on this machine (Tripo models are local-only; a collaborator
    /// sees stand-ins), and the shape of flat pieces.</summary>
    public enum StandIn { Box, Tree, Pine, Palm, Rock, Figure, Flat }

    /// <summary>How a model's size is read: by its largest footprint side (buildings, rocks), by its height (figures, trees),
    /// or stretched to width x height (flat pieces and stand-ins only).</summary>
    public enum Fit { Width, Height, Stretch }

    /// <summary>One placed piece: a model id (HexModels), the stand-in if it is missing, a tint for stand-ins and flats,
    /// and the world matrix of its unit frame (footprint centred on the origin, base at y = 0).</summary>
    public readonly struct HexPlacement
    {
        public readonly string Model; public readonly StandIn StandIn; public readonly Fit Fit; public readonly Color Tint;
        public readonly Matrix4x4 Matrix; public readonly int Q, R;
        public HexPlacement(string model, StandIn standIn, Fit fit, Color tint, Matrix4x4 matrix, int q, int r)
        { Model = model; StandIn = standIn; Fit = fit; Tint = tint; Matrix = matrix; Q = q; R = r; }
        public Vector3 Position => Matrix.GetColumn(3);
    }

    /// <summary>A stroke of ground texture (HexGround layer) painted into the terrain instead of placing a piece: a soft
    /// rectangle (a field, a dune, a yard) or a soft line (a track). Blends into neighbouring ground, as Civ paints fields.</summary>
    public readonly struct HexPaint
    {
        public readonly string Layer; public readonly Vector3 A, B; public readonly float Width, Depth, Yaw, Soft, Strength; public readonly bool Line;
        public HexPaint(string layer, Vector3 a, Vector3 b, float width, float depth, float yaw, float soft, float strength, bool line)
        { Layer = layer; A = a; B = b; Width = width; Depth = depth; Yaw = yaw; Soft = soft; Strength = strength; Line = line; }
    }

    /// <summary>The ground a tile is built on: height, slope and water at a world point. The board's own (HexBoardSurface)
    /// in captures; a flat one in tests.</summary>
    public interface IHexSurface
    {
        float Height(Vector3 p);
        float SlopeDeg(Vector3 p);
        bool IsWater(Vector3 p);
    }

    public sealed class FlatSurface : IHexSurface
    {
        public float Height(Vector3 p) => 0f;
        public float SlopeDeg(Vector3 p) => 0f;
        public bool IsWater(Vector3 p) => false;
    }

    /// <summary>Everything a prototype may use: where its hex is, a generator seeded by (q, r), the ground, and Place.
    /// Every Place checks that the whole footprint stays inside the inset hexagon and on dry, not too steep ground, seats
    /// the piece at the lowest corner, and records it; it returns false (and places nothing) otherwise.</summary>
    public sealed class HexTileContext
    {
        public readonly int Q, R;
        public readonly Vector3 Centre;
        public readonly float SizeM, InsetM;
        public readonly System.Random Rng;
        public readonly IHexSurface Surface;
        readonly List<HexPlacement> placements = new List<HexPlacement>();
        readonly List<HexPaint> paints = new List<HexPaint>();
        public IReadOnlyList<HexPlacement> Placements => placements;
        public IReadOnlyList<HexPaint> Paints => paints;

        /// <summary>Paints a soft rectangle of a ground layer (HexGround.Layers id: wheat, green_crop, ploughed, dirt,
        /// desert, beach, grass ...) centred at <paramref name="at"/>, clipped to this hex. Costs no triangles; use it for
        /// fields, dunes, yards and anything that is ground rather than an object. <paramref name="soft"/> is the edge blend
        /// as a share of the smaller side.</summary>
        public void PaintGround(string layer, Vector3 at, float yawDeg, float widthM, float depthM, float soft = 0.12f, float strength = 1f) =>
            paints.Add(new HexPaint(layer, at, at, widthM, depthM, yawDeg, soft, strength, false));

        /// <summary>Paints a soft line of a ground layer from a to b (a track, a path), clipped to this hex.</summary>
        public void PaintStroke(string layer, Vector3 a, Vector3 b, float widthM, float strength = 1f) =>
            paints.Add(new HexPaint(layer, a, b, widthM, 0f, 0f, 0.45f, strength, true));

        public HexTileContext(int q, int r, IHexSurface surface, int seed = 1948, float sizeM = HexScale.HexM, float insetShare = HexScale.InsetShare)
        {
            Q = q; R = r; SizeM = sizeM; InsetM = sizeM * insetShare; Surface = surface;
            Centre = HexGeometry.Centre(q, r, sizeM);
            Rng = new System.Random(seed ^ (q * 73856093) ^ (r * 19349663));
        }

        /// <summary>S, the hex circumradius in metres; scale pieces as shares of it (HexScale).</summary>
        public float S => SizeM;

        public float GroundY(Vector3 p) => Surface.Height(p);
        public bool IsWater(Vector3 p) => Surface.IsWater(p);
        public float SlopeDeg(Vector3 p) => Surface.SlopeDeg(p);

        /// <summary>Inside the inset hexagon, less <paramref name="marginM"/> more.</summary>
        public bool Inside(Vector3 p, float marginM = 0f) => HexGeometry.InHex(p, Centre, SizeM, InsetM + marginM);

        /// <summary>Inside the unit spot (HexScale.UnitSpotShare of S about the centre), widened by <paramref name="marginM"/>;
        /// context tiles keep tall pieces out of it, so any unit can stand on them.</summary>
        public bool InUnitSpot(Vector3 p, float marginM = 0f) => new Vector2(p.x - Centre.x, p.z - Centre.z).magnitude < HexScale.UnitSpotShare * SizeM + marginM;

        /// <summary>A point from the centre: <paramref name="bearingDeg"/> clockwise from north, <paramref name="share"/> of S.</summary>
        public Vector3 At(float bearingDeg, float share)
        {
            float a = Mathf.Deg2Rad * (90f - bearingDeg);
            return Centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (share * SizeM);
        }

        /// <summary>A uniform random point inside the inset hexagon less <paramref name="marginM"/>.</summary>
        public Vector3 RandomPoint(float marginM = 0f)
        {
            for (int i = 0; i < 64; i++)
            {
                var p = Centre + new Vector3((float)(Rng.NextDouble() * 2 - 1) * SizeM, 0f, (float)(Rng.NextDouble() * 2 - 1) * SizeM);
                if (Inside(p, marginM)) return p;
            }
            return Centre;
        }

        public float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

        /// <summary>A model sized by its largest footprint side, <paramref name="widthM"/> (buildings, rocks).</summary>
        public bool PlaceByWidth(string model, Vector3 at, float yawDeg, float widthM, StandIn standIn, Color? tint = null, float maxSlopeDeg = 30f) =>
            PlaceByWidthExact(model, at, yawDeg, widthM * HexScale.PieceScale, standIn, tint, maxSlopeDeg);

        bool PlaceByWidthExact(string model, Vector3 at, float yawDeg, float widthM, StandIn standIn, Color? tint, float maxSlopeDeg) =>
            Place(model, at, yawDeg, new Vector3(widthM, widthM, widthM), Fit.Width, standIn, tint, widthM, maxSlopeDeg);

        /// <summary>A model sized by its height, <paramref name="heightM"/> (figures, trees); its checked footprint is
        /// <paramref name="footprintShare"/> of the height, enlarged to contain the stand-in when the model is missing.</summary>
        public bool PlaceByHeight(string model, Vector3 at, float yawDeg, float heightM, StandIn standIn, Color? tint = null, float footprintShare = 0.5f, float maxSlopeDeg = 35f)
        {
            heightM *= HexScale.PieceScale;
            float footprintM = heightM * footprintShare;
            if (HexModels.Prefab(HexModels.Resolve(model, at)) == null)
                footprintM = Mathf.Max(footprintM, heightM * HexModels.StandInFootprint(standIn));
            return Place(model, at, yawDeg, new Vector3(heightM, heightM, heightM), Fit.Height, standIn, tint, footprintM, maxSlopeDeg);
        }

        /// <summary>A flat piece (a field, a road, a plaza) of width x depth, its top a few metres above the highest ground.</summary>
        public bool PlaceFlat(Vector3 at, float yawDeg, float widthM, float depthM, Color tint, float maxRiseM = 60f)
        {
            var rot = Quaternion.Euler(0f, yawDeg, 0f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var c in Footprint(widthM, depthM))
            {
                var q = at + rot * c;
                if (!Inside(q) || IsWater(q)) return false;
                float y = GroundY(q); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            if (hi - lo > maxRiseM) return false;
            const float depth = 12f;
            placements.Add(new HexPlacement("", StandIn.Flat, Fit.Stretch, tint,
                Matrix4x4.TRS(new Vector3(at.x, lo - depth, at.z), rot, new Vector3(widthM, hi - lo + depth + 5f, depthM)), Q, R));
            return true;
        }

        bool Place(string model, Vector3 at, float yawDeg, Vector3 scale, Fit fit, StandIn standIn, Color? tint, float footprintM, float maxSlopeDeg)
        {
            var rot = Quaternion.Euler(0f, yawDeg, 0f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var c in Footprint(footprintM, footprintM))
            {
                var q = at + rot * c;
                if (!Inside(q) || IsWater(q) || SlopeDeg(q) > maxSlopeDeg) return false;
                float y = GroundY(q); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            float sink = 0.02f * footprintM + (hi - lo) * 0.5f;
            placements.Add(new HexPlacement(model, standIn, fit, tint ?? Color.white,
                Matrix4x4.TRS(new Vector3(at.x, (lo + hi) * 0.5f - sink, at.z), rot, scale), Q, R));
            return true;
        }

        static IEnumerable<Vector3> Footprint(float w, float d)
        {
            yield return Vector3.zero;
            yield return new Vector3(w, 0f, d) * 0.5f; yield return new Vector3(-w, 0f, d) * 0.5f;
            yield return new Vector3(w, 0f, -d) * 0.5f; yield return new Vector3(-w, 0f, -d) * 0.5f;
        }
    }
}
