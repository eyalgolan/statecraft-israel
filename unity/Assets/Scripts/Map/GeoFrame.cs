using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Equirectangular lon/lat -> world metres. x = east, z = north, origin = south-west corner. The biome colours
    /// are baked from ToWorld's hex centres (BiomeColourFile): a change to it needs a re-bake and a bump of
    /// BiomeColourFile.Version (tools/tests/test_biome_colour_bake.py fails until then).</summary>
    public readonly struct GeoFrame
    {
        public readonly double Lon0, Lon1, Lat0, Lat1; public readonly float WidthM, LengthM;
        public GeoFrame(double lon0, double lon1, double lat0, double lat1, float widthM, float lengthM)
        { Lon0 = lon0; Lon1 = lon1; Lat0 = lat0; Lat1 = lat1; WidthM = widthM; LengthM = lengthM; }
        public Vector3 ToWorld(double lon, double lat, float y) =>
            new Vector3((float)((lon - Lon0) / (Lon1 - Lon0) * WidthM), y, (float)((lat - Lat0) / (Lat1 - Lat0) * LengthM));

        /// <summary>The inverse of ToWorld on the ground plane (y is ignored).</summary>
        public (double lon, double lat) ToLonLat(Vector3 world) =>
            (Lon0 + world.x / WidthM * (Lon1 - Lon0), Lat0 + world.z / LengthM * (Lat1 - Lat0));
    }
}
