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

        readonly IReadOnlyList<(Vector2 centre, float radius)> footprints;
        readonly (List<Vector2> line, float half, Rect bounds)[] corridors;
        readonly IReadOnlyList<MapHex> hexes;
        readonly Vector2[] hexXZ;
        public readonly float HexSizeM;

        public UnitGround(IReadOnlyList<(Vector2 centre, float radius)> footprints, IReadOnlyList<(List<Vector2> line, float halfWidth)> routeCorridors,
            IReadOnlyList<MapHex> hexes, Func<double, double, Vector2> toXZ, float hexSizeM)
        {
            this.footprints = footprints; this.hexes = hexes; HexSizeM = hexSizeM;
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

        /// <summary>The radius (m) the kits keep clear round a token that stands in a town: its compact ring.</summary>
        public float ClearingRadius => RingRadius(compact: true);

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

        /// <summary>True when a token of <paramref name="service"/> may stand at <paramref name="centre"/>: its ring off every
        /// town, except, when <paramref name="inTown"/>, the town its anchor <paramref name="anchor"/> stands in (the kits then
        /// keep a clearing for it there, never in a neighbouring town), and its figures (an army token's trio, a parked
        /// aircraft's centre) on land and off the railways and roads. A ship needs only the sea hex the placer already
        /// checked.</summary>
        public bool Clear(string service, Vector2 centre, bool compact, bool inTown, Vector2 anchor)
        {
            if (service == UnitData.Navy) return true;
            float disc = RingRadius(compact);
            foreach (var (fc, fr) in footprints)
            {
                bool own = inTown && (fc - anchor).magnitude < fr * FootprintMargin;
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

        /// <summary>Every anchor's spot (UnitPlacer.PlaceAll) on this ground.</summary>
        public Dictionary<UnitAnchor, AnchorSpot> Place(IReadOnlyList<UnitEntry> units, Func<double, double, Vector2> toXZ, Action<string>? skipped) =>
            UnitPlacer.PlaceAll(units, hexes, HexSizeM, (lon, lat) => { var v = toXZ(lon, lat); return (v.x, v.y); },
                (service, x, z, compact, inTown, ax, az) => Clear(service, new Vector2((float)x, (float)z), compact, inTown, new Vector2((float)ax, (float)az)),
                (x, z) => TownOverlap(new Vector2((float)x, (float)z), compact: true), skipped);
    }
}
