#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine;
using static Statecraft.Game.Map.Units.UnitPlacer;

/// <summary>Issue #321: UnitPlacer.PlaceAll was about 185 ms of the map build at cpu 4, most of it the candidate spots of
/// hexes the one-hex limit throws away. Its result must stay exactly the original algorithm's (kept here verbatim as the
/// reference), for any ground: the spots decide where every unit token stands.</summary>
public class UnitPlacerSpeedTests
{
    /// <summary>UnitPlacer.PlaceAll, Candidates, Inside and Nearest as they were before issue #321's change, verbatim.</summary>
    static class Reference
    {
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
                if (u.Hidden != null) continue;
                bool sea = u.Service == UnitData.Navy;
                foreach (var a in u.Anchors)
                {
                    var p = toXZ(a.Lon, a.Lat);
                    // A sourced point on the unit's own side, when the anchor has one: spots are ranked by distance to it.
                    var target = a.Toward != null ? toXZ(a.Toward.Lon, a.Toward.Lat) : p;
                    DateTime from = UnitData.Day(a.From), to = UnitData.Day(a.To);
                    int home = Nearest(centres, p);
                    var (hq, hr) = (hexes[home].Q, hexes[home].R);
                    AnchorSpot? best = null;
                    foreach (var round in new[] { 1, 2, 3 })
                    {
                        bool compact = round > 1, inTown = round == 3;
                        double bestD = double.MaxValue, bestDepth = double.MaxValue;
                        foreach (var (i, x, z) in Candidates(hexes, centres, hexSizeM, compact, a.Toward != null ? target : ((double, double)?)null))
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
                    if (best == null) { skipped?.Invoke($"UNITS no clear spot within one hex of {a.Place} for {u.Id}; not drawn"); continue; }
                    held.Add((best.Value.Q, best.Value.R, from, to));
                    result[a] = best.Value;
                }
            }
            return result;
        }

        /// <summary>Within the one-hex limit around a `toward` point (every round): rings every
        /// <see cref="TowardStepM"/> out to <see cref="TowardReachM"/>, twelve points each, so a spot can sit between a
        /// town's roads near the sourced place, not only on the hex grid's points.</summary>
        public const double TowardStepM = 500.0, TowardReachM = 2500.0;

        /// <summary>The round's candidate spots, each with the index of the hex it lies in: every hex centre, and for a
        /// compact round the points inside each hex; and in every round the rings round the `toward` point.</summary>
        static IEnumerable<(int i, double x, double z)> Candidates(IReadOnlyList<MapHex> hexes, (double x, double z)[] centres, double hexSizeM,
            bool compact, (double x, double z)? toward)
        {
            for (int i = 0; i < hexes.Count; i++)
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

    static (List<UnitEntry> units, List<MapHex> hexes, Func<double, double, (double, double)> toXZ, double hexSizeM) Inputs()
    {
        var inputs = GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        var hexFile = MapBuilder.LoadHexes(inputs.HexesJson, "hexes.json");
        var meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
        var size = MapBuilder.TerrainSize(meta);
        var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
        var hexes = hexFile.hexes.Select(h => new MapHex { Q = h.q, R = h.r, Lon = h.lon, Lat = h.lat, ElevM = h.elev_m, Land = h.land, Biome = h.biome }).ToList();
        var units = UnitData.Parse(inputs.UnitsYaml);
        return (units, hexes, (lon, lat) => { var w = frame.ToWorld(lon, lat, 0); return (w.x, w.z); }, hexFile.hex_size_km * 1000.0);
    }

    static ulong Mix(double a, double b, ulong salt)
    {
        ulong h = 1469598103934665603UL ^ salt;
        foreach (var v in new[] { BitConverter.DoubleToInt64Bits(a), BitConverter.DoubleToInt64Bits(b) }) { h ^= (ulong)v; h *= 1099511628211UL; h ^= h >> 29; }
        return h;
    }

    [Test]
    public void TheFastPlacerGivesExactlyTheOriginalSpots()
    {
        var (units, hexes, toXZ, hexSizeM) = Inputs();
        Assert.Greater(units.Sum(u => u.Anchors.Count), 10);
        // Grounds from open to harsh: everything clear; a seeded share of spots clear; nothing clear in the first round (so
        // rounds 2 and 3 and the town ranking run); and nothing clear at all (every anchor skipped).
        var grounds = new (string name, Func<string, double, double, bool, bool, double, double, bool> clear, Func<double, double, double> overlap)[]
        {
            ("open", (s, x, z, c, t, ax, az) => true, (x, z) => 0.0),
            ("seeded", (s, x, z, c, t, ax, az) => Mix(x, z, c ? 3UL : 1UL) % 3 != 0, (x, z) => (Mix(x, z, 7) % 900) - 300.0),
            ("compact only", (s, x, z, c, t, ax, az) => c && Mix(x, z, t ? 5UL : 9UL) % 4 == 0, (x, z) => (Mix(x, z, 11) % 2000) / 3.0),
            ("closed", (s, x, z, c, t, ax, az) => false, (x, z) => 0.0),
        };
        // Which neighbours of the anchors' home hexes the reference picks (review of #547, N2): the offsets (dq, dr) the
        // test checks the fast placer against, by ground.
        var centres = hexes.Select(h => toXZ(h.Lon, h.Lat)).ToArray();
        (int, int) Home(UnitAnchor a)
        {
            var (px, pz) = toXZ(a.Lon, a.Lat);
            int best = 0; double bestD = double.MaxValue;
            for (int i = 0; i < centres.Length; i++) { double d = (centres[i].Item1 - px) * (centres[i].Item1 - px) + (centres[i].Item2 - pz) * (centres[i].Item2 - pz); if (d < bestD) { bestD = d; best = i; } }
            return (hexes[best].Q, hexes[best].R);
        }
        var offsets = new SortedSet<(int, int)>();
        foreach (var (name, clear, overlap) in grounds)
        {
            var skippedA = new List<string>(); var skippedB = new List<string>();
            var fast = UnitPlacer.PlaceAll(units, hexes, hexSizeM, toXZ, clear, overlap, skippedA.Add);
            // The reference places what the map draws (UnitRoster.OnMap): the fast placer skips the other rows (#740).
            var reference = Reference.PlaceAll(units.Where(UnitRoster.OnMap).ToList(), hexes, hexSizeM, toXZ, clear, overlap, skippedB.Add);
            CollectionAssert.AreEqual(skippedB, skippedA, name);
            Assert.AreEqual(reference.Count, fast.Count, name);
            var here = new SortedSet<(int, int)>();
            foreach (var kv in reference) { var (hq, hr) = Home(kv.Key); here.Add((kv.Value.Q - hq, kv.Value.R - hr)); }
            TestContext.WriteLine($"{name}: {reference.Count} spots, offsets from the home hex {string.Join(" ", here)}");
            offsets.UnionWith(here);
            foreach (var kv in reference)
            {
                Assert.IsTrue(fast.TryGetValue(kv.Key, out var spot), $"{name}: {kv.Key.Place}");
                Assert.AreEqual(kv.Value, spot, $"{name}: {kv.Key.Place}");
            }
        }
        // Measured on the units of 2026-10-14: the home hex and five of its six neighbours, (-1, 0) (0, -1) (0, 1) (1, -1)
        // (1, 0); no reference spot lies at (-1, 1). A change to the units may move the set; it must keep at least four.
        TestContext.WriteLine($"all grounds: offsets {string.Join(" ", offsets)}");
        CollectionAssert.Contains(offsets, (0, 0));
        Assert.GreaterOrEqual(offsets.Count(o => o != (0, 0)), 4, $"too few neighbour offsets exercised: {string.Join(" ", offsets)}");
    }
}
