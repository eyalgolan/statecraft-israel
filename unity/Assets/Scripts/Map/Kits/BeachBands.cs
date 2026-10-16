#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Sea round 3 (#388): the dune coasts' wider beach, for the kits. The terrain draws it from coast.png, whose
    /// land side tools/terrain_bake.py bakes beach-scaled there (BEACH_EXTRA_M, BEACH_BANDS, beach_keep); KitGround does not
    /// read coast.png, so this is its C# twin, pinned to the bake by tools/tests/test_terrain_bake.py.</summary>
    public static class BeachBands
    {
        /// <summary>The extra beach width (m) inside a band: ScShore's 900 m beach becomes about 2.7 km.</summary>
        public const float ExtraM = 1800f;
        /// <summary>(lon west, lon east, lat south, lat north, ramp in degrees): Jaffa south to the map's southern coast, and
        /// Haifa Bay from the Kishon's mouth to the Na'aman's, south of Acre (owner, 16 October 2026: Acre was not dune
        /// country). The extra width falls to 0 over the ramp at each edge.</summary>
        static readonly (double lon0, double lon1, double lat0, double lat1, double ramp)[] Bands =
        {
            (34.0, 34.86, 31.0, 32.07, 0.04),
            (35.02, 35.2, 32.77, 32.91, 0.02),
        };
        /// <summary>Option (c) (owner, 16 October 2026: each settlement's own lots and fields stay off the sand): the extra
        /// width is 0 inside a settlement's plot reach (SettlementKits.FootprintRadiusM x PlotReach) and comes back over this
        /// many metres beyond it.</summary>
        public const float YieldRampM = 1000f;
        /// <summary>How far (m) from the sea ScShore (StatecraftMap.hlsl) paints full sand at its widest: 0.55 of a width
        /// that reaches 1.4 x ScBeachM (900 m).</summary>
        public const float FullSandM = 693f;
        /// <summary>The farthest any full sand reaches from the sea (m).</summary>
        public const float MaxFullSandM = FullSandM + ExtraM;

        static double Smooth(double t) { t = t < 0 ? 0 : t > 1 ? 1 : t; return t * t * (3 - 2 * t); }

        /// <summary>The band's extra beach width (m) at (lon, lat): ExtraM inside a band, ramped to 0 at its edges, before
        /// the settlements' yield.</summary>
        public static float ExtraAt(double lon, double lat)
        {
            double w = 0;
            foreach (var (lon0, lon1, lat0, lat1, ramp) in Bands)
                w = System.Math.Max(w, Smooth((lon - lon0) / ramp) * Smooth((lon1 - lon) / ramp) * Smooth((lat - lat0) / ramp) * Smooth((lat1 - lat) / ramp));
            return (float)(w * ExtraM);
        }

        /// <summary>The plot reaches (world centre, m) of <paramref name="places"/> that the extra width yields to, as the
        /// bake's plot_reaches: only those whose yield (reach plus YieldRampM) can reach a band.</summary>
        public static List<(Vector2 centre, float reach)> Reaches(IReadOnlyList<Settlement> places, KitGround ground)
        {
            var list = new List<(Vector2, float)>();
            foreach (var s in places)
            {
                var w = ground.Frame.ToWorld(s.Lon, s.Lat, 0);
                float reach = SettlementKits.FootprintRadiusM(s, ground.HexSizeM) * SettlementKits.PlotReach, rim = reach + YieldRampM;
                var (lonW, latS) = ground.Frame.ToLonLat(new Vector3(w.x - rim, 0f, w.z - rim));
                var (lonE, latN) = ground.Frame.ToLonLat(new Vector3(w.x + rim, 0f, w.z + rim));
                foreach (var (lon0, lon1, lat0, lat1, _) in Bands)
                    if (lonE > lon0 && lonW < lon1 && latN > lat0 && latS < lat1) { list.Add((new Vector2(w.x, w.z), reach)); break; }
            }
            return list;
        }

        /// <summary>How far (m) from the sea the full sand reaches at world (x, z): the extra width yields to
        /// <paramref name="reaches"/> (<see cref="Reaches"/>).</summary>
        public static float FullSandAt(GeoFrame frame, float x, float z, List<(Vector2 centre, float reach)> reaches)
        {
            var (lon, lat) = frame.ToLonLat(new Vector3(x, 0f, z));
            float extra = ExtraAt(lon, lat);
            if (extra <= 0f) return FullSandM;
            double keep = 1; var p = new Vector2(x, z);
            foreach (var (c, reach) in reaches)
            {
                keep = System.Math.Min(keep, Smooth(((p - c).magnitude - reach) / YieldRampM));
                if (keep <= 0) break;
            }
            return FullSandM + (float)(extra * keep);
        }
    }
}
