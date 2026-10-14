using System.IO;
using Statecraft.Game.Map;
using UnityEditor;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Bakes the biome colours (MapBuilder.BiomePixels) from the shipped hexes and heightmap frame into
    /// BiomeColourFile.AssetPath, so a player reads them instead of blending them at boot (issue #321). Re-run it whenever
    /// hexes.json, the heightmap's bounds, MapPalette.Ground or the blend change (BiomeColourBakeTests fails until then;
    /// the map also falls back to blending them). Run headless:
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod
    /// Statecraft.Game.EditorTools.BiomeColourBake.Bake</summary>
    public static class BiomeColourBake
    {
        public static byte[] Bytes()
        {
            var (meta, _) = HeightmapLoader.Load(Paths.StreamingMap);
            var hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
            var size = MapBuilder.TerrainSize(meta);
            var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
            return BiomeColourFile.Bytes(frame, hexes, MapBuilder.BiomePixels(frame, hexes));
        }

        public static void Bake()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BiomeColourFile.AssetPath));
            File.WriteAllBytes(BiomeColourFile.AssetPath, Bytes());
            AssetDatabase.ImportAsset(BiomeColourFile.AssetPath);
        }
    }
}
