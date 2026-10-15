#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Sea round 3 (#388): the dune coasts' wider beach, for the kits. The terrain draws it from coast.png, whose
    /// land side tools/terrain_bake.py bakes beach-scaled there (BEACH_EXTRA_M, BEACH_BANDS); KitGround does not read
    /// coast.png, so this is its C# twin, pinned to the bake by tools/tests/test_terrain_bake.py.</summary>
    public static class BeachBands
    {
        /// <summary>The extra beach width (m) inside a band: ScShore's 900 m beach becomes about 2.7 km.</summary>
        public const float ExtraM = 1800f;
        /// <summary>(lon west, lon east, lat south, lat north, ramp in degrees): Jaffa south to the map's southern coast, and
        /// Haifa Bay. The extra width falls to 0 over the ramp at each edge.</summary>
        static readonly (double lon0, double lon1, double lat0, double lat1, double ramp)[] Bands =
        {
            (34.0, 34.86, 31.0, 32.07, 0.04),
            (35.02, 35.2, 32.77, 32.94, 0.02),
        };
        /// <summary>How far (m) from the sea ScShore (StatecraftMap.hlsl) paints full sand at its widest: 0.55 of a width
        /// that reaches 1.4 x ScBeachM (900 m).</summary>
        public const float FullSandM = 693f;
        /// <summary>The farthest any full sand reaches from the sea (m).</summary>
        public const float MaxFullSandM = FullSandM + ExtraM;

        static double Smooth(double t) { t = t < 0 ? 0 : t > 1 ? 1 : t; return t * t * (3 - 2 * t); }

        /// <summary>The extra beach width (m) at (lon, lat): ExtraM inside a band, ramped to 0 at its edges.</summary>
        public static float ExtraAt(double lon, double lat)
        {
            double w = 0;
            foreach (var (lon0, lon1, lat0, lat1, ramp) in Bands)
                w = System.Math.Max(w, Smooth((lon - lon0) / ramp) * Smooth((lon1 - lon) / ramp) * Smooth((lat - lat0) / ramp) * Smooth((lat1 - lat) / ramp));
            return (float)(w * ExtraM);
        }

        /// <summary>How far (m) from the sea the full sand reaches at world (x, z).</summary>
        public static float FullSandAt(GeoFrame frame, float x, float z)
        {
            var (lon, lat) = frame.ToLonLat(new Vector3(x, 0f, z));
            return FullSandM + ExtraAt(lon, lat);
        }
    }
}
