#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Sea round 3 (#388): the dune coasts' wider beach, for the kits. The terrain draws it from coast.png, whose
    /// land side tools/terrain_bake.py bakes beach-scaled there (BEACH_EXTRA_M, BEACH_BANDS, beach_keep); KitGround does not
    /// read coast.png, so this is its C# twin: the bands are DuneCoasts.json, which the bake reads too (#690), and the
    /// constants are pinned to the bake by tools/tests/test_terrain_bake.py.</summary>
    public static class BeachBands
    {
        /// <summary>The extra beach width (m) inside a band: ScShore's 900 m beach becomes about 2.7 km.</summary>
        public const float ExtraM = 1800f;
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
            double w = DuneCoasts.Weight(lon, lat);   // the bands: DuneCoasts.json, the one definition (#690)
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
                foreach (var b in DuneCoasts.Bands)
                    if (lonE > b.lon0 && lonW < b.lon1 && latN > b.lat0 && latS < b.lat1) { list.Add((new Vector2(w.x, w.z), reach)); break; }
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
