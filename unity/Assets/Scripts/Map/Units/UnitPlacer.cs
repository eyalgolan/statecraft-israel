#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Where an anchor's token stands: its hex, its centre on the ground plane (metres), whether it is drawn
    /// compact (a smaller ring and a tighter trio, to fit beside a town) and whether it stands in a town (the kits then
    /// keep a clearing for it).</summary>
    public readonly struct AnchorSpot : IEquatable<AnchorSpot>
    {
        public readonly int Q, R; public readonly double X, Z; public readonly bool Compact, InTown;
        public AnchorSpot(int q, int r, double x, double z, bool compact, bool inTown = false) { Q = q; R = r; X = x; Z = z; Compact = compact; InTown = inTown; }
        public bool Equals(AnchorSpot o) => Q == o.Q && R == o.R && X == o.X && Z == o.Z && Compact == o.Compact && InTown == o.InTown;
        public override bool Equals(object? o) => o is AnchorSpot s && Equals(s);
        public override int GetHashCode() => HashCode.Combine(Q, R, X, Z, Compact, InTown);
        public override string ToString() => $"({Q},{R}){(Compact ? " compact" : "")}{(InTown ? " in town" : "")}";
    }

    /// <summary>A token with its spot.</summary>
    public readonly struct PlacedToken : IEquatable<PlacedToken>
    {
        public readonly UnitToken Token; public readonly AnchorSpot Spot;
        public PlacedToken(UnitToken token, AnchorSpot spot) { Token = token; Spot = spot; }
        public int Q => Spot.Q;
        public int R => Spot.R;
        public bool Equals(PlacedToken o) => Token.Equals(o.Token) && Spot.Equals(o.Spot);
        public override bool Equals(object? o) => o is PlacedToken p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(Token, Spot);
        public override string ToString() => $"{Token} {Spot}";
    }

    /// <summary>Puts every anchor of the unit data on one spot, once, from the data alone (review M9: a token never moves
    /// because another unit appears). The spot lies within one hex step of the anchor point (review I2: about 8.7 km
    /// between hex centres on 5 km hexes) and is the nearest to it, or, when the anchor has a sourced `toward` point on
    /// the unit's own side (review MF1), the nearest to that, among the following; an anchor with a sourced `place_hex`
    /// considers only that hex:
    /// 1. the centres of the anchor's own hex and its six neighbours, drawn full size;
    /// 2. then, when none is clear, those centres and eighteen points inside each hex (six on each of
    ///    <see cref="InsideOffsets"/> of the hex circumradius), and rings round a `toward` point, drawn compact;
    /// 3. then, when the anchor's whole neighbourhood is town (Tel Aviv), the same compact points with the ring allowed
    ///    into the town: the one that reaches least deep into it (then the nearest; with a `toward` point, simply the nearest
    ///    to it) is taken, and the kits keep a clearing for it (MapScene), so the token never stands in a building.
    /// A spot must be land (sea for a ship), on a hex no other anchor with an overlapping window holds, and clear (the
    /// caller's test: the ring off the settlement footprints except in round 3, the figures on land and off the railways
    /// and roads). An anchor with no such spot is left out and reported. The map holds no 1948 front line yet: where the
    /// nearest spot would be the other side's ground, the anchor's sourced `toward` point keeps the token on its own side
    /// (docs/units.md). Anchors are placed in the data's order.</summary>
    public static class UnitPlacer
    {
        /// <summary>Hexes from the anchor's own hex (axial distance).</summary>
        public const int MaxShift = 1;
        /// <summary>The inside points' distances from a hex centre, as fractions of the circumradius (all inside the
        /// inradius, 0.87): six points on each ring.</summary>
        public static readonly double[] InsideOffsets = { 0.25, 0.45, 0.65 };

        /// <param name="toXZ">lon, lat to the map's ground plane (metres).</param>
        /// <param name="clear">(service, x, z, compact, inTown, anchor x, anchor z): true when a token of that service may
        /// stand there.</param>
        /// <param name="townOverlap">(x, z): how deep (m) a compact token there reaches into a town (round 3's ranking).</param>
        public static Dictionary<UnitAnchor, AnchorSpot> PlaceAll(IReadOnlyList<UnitEntry> units, IReadOnlyList<MapHex> hexes, double hexSizeM,
            Func<double, double, (double x, double z)> toXZ, Func<string, double, double, bool, bool, double, double, bool> clear,
            Func<double, double, double> townOverlap, Action<string>? skipped = null)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (hexes == null || hexes.Count == 0) throw new ArgumentException("no hexes", nameof(hexes));
            var centres = new (double x, double z)[hexes.Count];
            for (int i = 0; i < hexes.Count; i++) centres[i] = toXZ(hexes[i].Lon, hexes[i].Lat);
            double maxOffset = Math.Sqrt(3.0) * hexSizeM + 1.0;   // one hex step (plus a metre of rounding)
            var held = new List<(int q, int r, DateTime from, DateTime to)>();
            var result = new Dictionary<UnitAnchor, AnchorSpot>();
            foreach (var u in units)
            {
                if (!UnitRoster.OnMap(u)) continue;
                bool sea = u.Service == UnitData.Navy;
                foreach (var a in u.Anchors)
                {
                    var p = toXZ(a.Lon, a.Lat);
                    // A sourced point on the unit's own side, when the anchor has one: spots are ranked by distance to it.
                    var target = a.Toward != null ? toXZ(a.Toward.Lon, a.Toward.Lat) : p;
                    DateTime from = UnitData.Day(a.From), to = UnitData.Day(a.To);
                    int home = Nearest(centres, p);
                    var (hq, hr) = (hexes[home].Q, hexes[home].R);
                    // The hexes within the one-hex limit, in list order: the only ones whose points can pass the test below,
                    // so the others' candidate points are never made (issue #321: boot time; UnitPlacerSpeedTests).
                    var near = new List<int>();
                    for (int i = 0; i < hexes.Count; i++) if (MapActions.Distance(hexes[i].Q, hexes[i].R, hq, hr) <= MaxShift) near.Add(i);
                    AnchorSpot? best = null;
                    foreach (var round in new[] { 1, 2, 3 })
                    {
                        bool compact = round > 1, inTown = round == 3;
                        double bestD = double.MaxValue, bestDepth = double.MaxValue;
                        foreach (var (i, x, z) in Candidates(near, centres, hexSizeM, compact, a.Toward != null ? target : ((double, double)?)null))
                        {
                            var h = hexes[i];
                            if (h.Land == sea || MapActions.Distance(h.Q, h.R, hq, hr) > MaxShift) continue;
                            if (a.PlaceHex != null && (h.Q != a.PlaceHex[0] || h.R != a.PlaceHex[1])) continue;
                            if (held.Exists(c => c.q == h.Q && c.r == h.R && c.from < to && from < c.to)) continue;
                            if (Math.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) > maxOffset) continue;
                            double d = Math.Sqrt((x - target.x) * (x - target.x) + (z - target.z) * (z - target.z));
                            // Round 3 ranks by depth into the town first (rounded to a metre), then by distance; with a
                            // sourced own-side point it ranks by distance to that alone (the town edge may be the other side's).
                            double overlap = inTown ? Math.Max(0.0, townOverlap(x, z)) : 0.0;
                            double depth = a.Toward == null ? Math.Round(overlap) : 0.0;
                            if (depth > bestDepth || (depth == bestDepth && d > bestD)) continue;
                            // Ties go to the lower (q, r): the result never depends on the hex list's order.
                            if (depth == bestDepth && d == bestD && best != null && (h.Q, h.R).CompareTo((best.Value.Q, best.Value.R)) > 0) continue;
                            if (!clear(u.Service, x, z, compact, inTown, p.x, p.z)) continue;
                            best = new AnchorSpot(h.Q, h.R, x, z, compact, inTown && overlap > 0); bestD = d; bestDepth = depth;
                        }
                        if (best != null) break;
                        if (sea) break;   // a ship's sea hex is all its test; rounds 2 and 3 add nothing
                    }
                    if (best == null) { skipped?.Invoke(SkipMessage(u, a)); continue; }
                    held.Add((best.Value.Q, best.Value.R, from, to));
                    result[a] = best.Value;
                }
            }
            return result;
        }

        /// <summary>The message for an anchor PlaceAll found no clear spot for.</summary>
        public static string SkipMessage(UnitEntry u, UnitAnchor a) => $"UNITS no clear spot within one hex of {a.Place} for {u.Id}; not drawn";

        /// <summary>PlaceAll's skip messages for a placement it made of <paramref name="units"/>: one for every anchor of a
        /// drawn unit with no spot, in its order (issue #321: the units layer reports the map's skips when it takes its spots).</summary>
        public static List<string> SkipMessages(IReadOnlyList<UnitEntry> units, IReadOnlyDictionary<UnitAnchor, AnchorSpot> spots)
        {
            var list = new List<string>();
            foreach (var u in units)
            {
                if (!UnitRoster.OnMap(u)) continue;
                foreach (var a in u.Anchors) if (!spots.ContainsKey(a)) list.Add(SkipMessage(u, a));
            }
            return list;
        }

        /// <summary>Within the one-hex limit around a `toward` point (every round): rings every
        /// <see cref="TowardStepM"/> out to <see cref="TowardReachM"/>, twelve points each, so a spot can sit between a
        /// town's roads near the sourced place, not only on the hex grid's points.</summary>
        public const double TowardStepM = 500.0, TowardReachM = 2500.0;

        /// <summary>The round's candidate spots, each with the index of the hex it lies in: the centre of every hex in
        /// <paramref name="hexIndices"/> (in that order), and for a compact round the points inside each; and in every round
        /// the rings round the `toward` point (each with its nearest hex, whichever it is).</summary>
        static IEnumerable<(int i, double x, double z)> Candidates(IReadOnlyList<int> hexIndices, (double x, double z)[] centres, double hexSizeM,
            bool compact, (double x, double z)? toward)
        {
            foreach (var i in hexIndices)
            {
                yield return (i, centres[i].x, centres[i].z);
                if (!compact) continue;
                for (int k = 0; k < 6 * InsideOffsets.Length; k++) { var (x, z) = Inside(centres[i], k, hexSizeM); yield return (i, x, z); }
            }
            if (toward == null) yield break;   // the rings round a `toward` point are tried in every round
            var t = toward.Value;
            for (double rad = 0; rad <= TowardReachM; rad += TowardStepM)
                for (int k = 0; k < (rad == 0 ? 1 : 12); k++)
                {
                    double x = t.x + Math.Cos(Math.PI / 6.0 * k) * rad, z = t.z + Math.Sin(Math.PI / 6.0 * k) * rad;
                    yield return (Nearest(centres, (x, z)), x, z);
                }
        }

        /// <summary>The tokens on their anchors' spots; a token whose anchor has no spot is left out.</summary>
        public static List<PlacedToken> Place(IReadOnlyList<UnitToken> tokens, IReadOnlyDictionary<UnitAnchor, AnchorSpot> spots)
        {
            var list = new List<PlacedToken>(tokens.Count);
            foreach (var t in tokens) if (spots.TryGetValue(t.Anchor, out var s)) list.Add(new PlacedToken(t, s));
            return list;
        }

        static (double x, double z) Inside((double x, double z) c, int k, double hexSizeM)
        {
            double ang = Math.PI / 3.0 * (k % 6), off = InsideOffsets[k / 6] * hexSizeM;
            return (c.x + Math.Cos(ang) * off, c.z + Math.Sin(ang) * off);
        }

        static int Nearest((double x, double z)[] centres, (double x, double z) p)
        {
            int best = 0; double bestD = double.MaxValue;
            for (int i = 0; i < centres.Length; i++)
            {
                double d = (centres[i].x - p.x) * (centres[i].x - p.x) + (centres[i].z - p.z) * (centres[i].z - p.z);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
