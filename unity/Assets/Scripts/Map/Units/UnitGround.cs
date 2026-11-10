#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>What a unit token may stand on, from the built map's presentation data only: the settlement footprints
    /// (the kits' discs), the railways' and roads' corridors, and the hexes' land. The map (MapScene, to keep the kits'
    /// buildings off a token that stands in a town) and the layer (UnitLayer) both place the units through it, so they
    /// agree.</summary>
    public sealed class UnitGround
    {
        /// <summary>A footprint's buildings and tinted ground reach 1.1 of its radius (SettlementKits.PaintGround).</summary>
        public const float FootprintMargin = 1.1f;
        /// <summary>Clearance (m) between a figure's centre and a railway's or road's corridor: about half a figure's width
        /// at the first round's 2000x (0.6 m x 2000 = 1.2 km across) plus a margin. Kept when the figures went to 1400x
        /// (refs #197), so no token's spot moved.</summary>
        public const float FigureClearanceM = 700f;
        /// <summary>A compact token (beside or in a town) draws its ring and trio at this share of the full size.</summary>
        public const float CompactScale = 0.5f;
        /// <summary>The radius a parked 101 Squadron fighter keeps clear at the approved aircraft scale: the Spitfire's
        /// full-wing 11.23 m span at 420x, a conservative bound above the clipped wing of the LF Mk IXe the token shows
        /// (about 10.2 m: every Spitfire flown to Israel in the war had it, Efrati p. 14; docs/units.md). Placement must not
        /// depend on which authored mesh has arrived.</summary>
        public const float ParkedAircraftClearanceM = 11.23f * UnitShowcase.AircraftScale * 0.5f;

        readonly IReadOnlyList<(Vector2 centre, float radius)> footprints;
        readonly (List<Vector2> line, float half, Rect bounds)[] corridors;
        readonly IReadOnlyList<MapHex> hexes;
        readonly Vector2[] hexXZ;
        readonly Func<float, float, bool>? water;
        public readonly float HexSizeM;

        /// <param name="water">(x, z): true where the drawn map is water (KitGround.Water over the map's terrain). With it, a
        /// ship's whole drawn hull must lie in water (<see cref="HullInWater"/>); null: a ship needs only its sea hex.</param>
        public UnitGround(IReadOnlyList<(Vector2 centre, float radius)> footprints, IReadOnlyList<(List<Vector2> line, float halfWidth)> routeCorridors,
            IReadOnlyList<MapHex> hexes, Func<double, double, Vector2> toXZ, float hexSizeM, Func<float, float, bool>? water = null)
        {
            this.footprints = footprints; this.hexes = hexes; HexSizeM = hexSizeM; this.water = water;
            hexXZ = hexes.Select(h => toXZ(h.Lon, h.Lat)).ToArray();
            corridors = routeCorridors.Where(c => c.line.Count > 0).Select(c =>
            {
                float minX = c.line.Min(p => p.x), maxX = c.line.Max(p => p.x), minY = c.line.Min(p => p.y), maxY = c.line.Max(p => p.y);
                float grow = c.halfWidth + FigureClearanceM;
                return (c.line, c.halfWidth, Rect.MinMaxRect(minX - grow, minY - grow, maxX + grow, maxY + grow));
            }).ToArray();
        }

        /// <summary>The ground ring's radius of a token (m).</summary>
        public float RingRadius(bool compact) => UnitShowcase.RingRadius * HexSizeM * (compact ? CompactScale : 1f);

        /// <summary>The radius (m) the kits keep clear round a token that stands in a town: the parked fighter's span for an
        /// air anchor, and the existing compact ring for every other service.</summary>
        public float ClearingRadius(string service) => service == UnitData.Air ? ParkedAircraftClearanceM : RingRadius(compact: true);

        /// <summary>The footprint placement keeps clear of towns: an air token keeps clear by the larger of its ring and the
        /// parked fighter's span (the span does not compact; the full ring stays where it is wider), every other service by
        /// its ring.</summary>
        public float ClearanceRadius(string service, bool compact) =>
            service == UnitData.Air ? Math.Max(ParkedAircraftClearanceM, RingRadius(compact)) : RingRadius(compact);

        /// <summary>The figures' ground points of an army token standing at <paramref name="centre"/>: always the full trio,
        /// so a spot does not change with the call-up.</summary>
        public Vector2[] FigurePoints(Vector2 centre, bool compact)
        {
            float k = UnitShowcase.TrioSpread * HexSizeM * (compact ? CompactScale : 1f);
            return UnitShowcase.FigureOffsets(3).Select(o => centre + o * k).ToArray();
        }

        /// <summary>How far (m) the token's ring reaches into the deepest settlement footprint (with its margin); 0 or less
        /// when it is clear of every town.</summary>
        public float TownOverlap(Vector2 centre, bool compact)
        {
            float worst = float.MinValue, disc = RingRadius(compact);
            foreach (var (fc, fr) in footprints) worst = Mathf.Max(worst, fr * FootprintMargin + disc - (fc - centre).magnitude);
            return worst;
        }

        /// <summary>True when a token of <paramref name="service"/> may stand at <paramref name="centre"/>: its footprint off every
        /// town (the full parked-aircraft span for air, the ring for other services), except an army token in
        /// <paramref name="inTown"/> may enter the town its <paramref name="anchor"/> stands in (the kits then keep a clearing
        /// for it there, never in a neighbouring town), and its figures (an army token's trio, a parked
        /// aircraft's centre) on land and off the railways and roads. A ship needs its sea hex (the placer checks it) and,
        /// when this ground knows the water, its whole drawn hull in the water (issue #3038: INS Haganah stood on Mount
        /// Carmel's headland at the centre of a hex that is more than half sea).</summary>
        public bool Clear(string service, Vector2 centre, bool compact, bool inTown, Vector2 anchor)
        {
            if (service == UnitData.Navy) return water == null || HullInWater(centre, water);
            float disc = ClearanceRadius(service, compact);
            foreach (var (fc, fr) in footprints)
            {
                // A town may make a kit clearing for an army token. An aircraft's whole parked footprint instead has to
                // clear every town; it must not win round 3 merely because its compact token ring fits at the anchor.
                bool own = service != UnitData.Air && inTown && (fc - anchor).magnitude < fr * FootprintMargin;
                if (!own && fr * FootprintMargin + disc - (fc - centre).magnitude > 0f) return false;
            }
            var points = service == UnitData.Army ? FigurePoints(centre, compact) : new[] { centre };
            foreach (var p in points)
            {
                if (!LandAt(p)) return false;
                foreach (var (line, half, bounds) in corridors)
                    if (bounds.Contains(p))
                        for (int i = 0; i + 1 < line.Count; i++)
                            if (DistanceToSegment(p, line[i], line[i + 1]) < half + FigureClearanceM) return false;
            }
            return true;
        }

        /// <summary>The drawn ship's length and beam on the map (m): UnitModels.Corvette's 63.4 m hull, 10.1 m across at its
        /// waterline band (radius 5.0 m, widened 1.01), at <see cref="UnitShowcase.ShipScale"/>. Every navy token draws that
        /// mesh (UnitLayer).</summary>
        public const float ShipLengthM = 63.4f * UnitShowcase.ShipScale, ShipBeamM = 10.1f * UnitShowcase.ShipScale;

        /// <summary>The spacing (m) of the hull's samples: under the heightmap's finer sample spacing (about 79 m east-west,
        /// 210 m north-south), so no strip of land as wide as a sample slips between two of them.</summary>
        public const float HullSampleM = 75f;

        /// <summary>The points of the drawn hull's rectangle (<see cref="ShipLengthM"/> by <see cref="ShipBeamM"/>, its bow
        /// turned <see cref="UnitShowcase.ShipHeadingDegrees"/> about the vertical as UnitLayer draws it, from +z towards +x),
        /// at most <see cref="HullSampleM"/> apart along and across it, its edges and corners included.</summary>
        public static IEnumerable<Vector2> HullPoints(Vector2 centre)
        {
            float a = UnitShowcase.ShipHeadingDegrees * Mathf.Deg2Rad;
            var bow = new Vector2(Mathf.Sin(a), Mathf.Cos(a)); var side = new Vector2(bow.y, -bow.x);
            int n = Mathf.CeilToInt(ShipLengthM / HullSampleM), m = Mathf.CeilToInt(ShipBeamM / HullSampleM);
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= m; j++)
                    yield return centre + bow * ShipLengthM * (i / (float)n - 0.5f) + side * ShipBeamM * (j / (float)m - 0.5f);
        }

        /// <summary>True when every point of the drawn hull (<see cref="HullPoints"/>) is water.</summary>
        public static bool HullInWater(Vector2 centre, Func<float, float, bool> water)
        {
            foreach (var p in HullPoints(centre)) if (!water(p.x, p.y)) return false;
            return true;
        }

        bool LandAt(Vector2 p)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < hexXZ.Length; i++)
            {
                float d = (hexXZ[i] - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best >= 0 && hexes[best].Land;
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return (p - (a + t * ab)).magnitude;
        }

        /// <summary>Every anchor's spot (UnitPlacer.PlaceAll) on this ground. With <paramref name="control"/>, an Arab force's
        /// anchor keeps off the hexes Israel held on its first day (#226, ruling 9).</summary>
        public Dictionary<UnitAnchor, AnchorSpot> Place(IReadOnlyList<UnitEntry> units, Func<double, double, Vector2> toXZ, Action<string>? skipped,
            IEnumerable<(int q, int r)>? taken = null, Control1948? control = null) =>
            UnitPlacer.PlaceAll(units, hexes, HexSizeM, (lon, lat) => { var v = toXZ(lon, lat); return (v.x, v.y); },
                (service, x, z, compact, inTown, ax, az) => Clear(service, new Vector2((float)x, (float)z), compact, inTown, new Vector2((float)ax, (float)az)),
                (x, z) => TownOverlap(new Vector2((float)x, (float)z), compact: true), skipped, taken,
                control == null ? null : (u, a, h) => u.Side != UnitData.Israel && control.At(h.Q, h.R, UnitData.Day(a.From)).side == Control1948.Side.Israel);
    }
}
