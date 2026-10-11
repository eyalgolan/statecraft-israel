#nullable enable
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Import settings of the baked map textures: linear data (not sRGB), clamped, mipmapped, trilinear, 2048. The
    /// shading bake (curvature, sky-view) is block-compressed; the coast distance and the AO are each one uncompressed 8-bit
    /// channel (R8): compression blocks would show in the 350 m foam band, and BC1 stepped the AO on broad slopes (Task 6
    /// fix round 1). The land (2048 x 4096, sRGB colour + weight, tools/land_bake.py) is block-compressed too. Idempotent; MapMaterials.Create calls it.</summary>
    public static class MapTextures
    {
        public static void Configure()
        {
            Set(MapTexturePaths.Bake, singleChannel: false);
            Set(MapTexturePaths.Coast, singleChannel: true);
            Set(MapTexturePaths.Ao, singleChannel: true);
            Set(MapTexturePaths.Land, singleChannel: false);   // the land's colour and weight, block-compressed
            // The land: sRGB colour (the sampler decodes it; alpha, the weight, stays linear), 4096 rows on every platform
            // (the platforms' own 2048 caps would halve it: hedges and rims blur, -21% local detail at gameplay03), and an
            // automatic compressed format, so the WebGL build's texture subtarget picks it: DXT5 for the desktop build,
            // ASTC 6x6 or ETC2 for a mobile one (Build.Web -textureSubtarget; docs/visual-v1.md, "Promo track: land use").
            var land = (TextureImporter)AssetImporter.GetAtPath(MapTexturePaths.Land);
            land.sRGBTexture = true;
            foreach (var platform in new[] { "Standalone", "WebGL" })
                land.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    { name = platform, overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.Automatic,
                      textureCompression = TextureImporterCompression.Compressed });
            land.SaveAndReimport();
        }

        static void Set(string path, bool singleChannel)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new System.IO.FileNotFoundException("run tools/terrain_bake.py", path);
            imp.textureType = singleChannel ? TextureImporterType.SingleChannel : TextureImporterType.Default;
            if (singleChannel)
            {
                var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
                s.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                imp.SetTextureSettings(s);
            }
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.isReadable = false;
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Trilinear;   // no mip seams across the pitched view
            imp.anisoLevel = path == MapTexturePaths.Bake || path == MapTexturePaths.Land ? 2 : 1;   // seen at a grazing angle
            imp.maxTextureSize = path == MapTexturePaths.Land ? 4096 : 2048;
            imp.textureCompression = singleChannel ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
    }
}
