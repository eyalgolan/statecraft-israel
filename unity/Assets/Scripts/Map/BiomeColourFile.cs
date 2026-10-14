#nullable enable
using System;
using System.Buffers.Binary;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>BiomeColours.bytes (issue #321): MapBuilder.BiomePixels baked by Editor BiomeColourBake, so a player reads the
    /// biome colours instead of blending them at boot. "SCB1", the version, the resolution and a check of the inputs (the
    /// frame, the hexes, the blend radius and the palette), all little-endian int32, then each texel's R, G and B, row 0 =
    /// south (alpha is always 255). A bake for other inputs is refused, and the map blends the colours itself.</summary>
    public static class BiomeColourFile
    {
        public const string Resource = "Map/BiomeColours";
        public const string AssetPath = "Assets/Resources/Map/BiomeColours.bytes";
        /// <summary>Bump it when the code that makes the texels changes: MapBuilder.BiomePixels and BiomeLayerColours,
        /// BiomeBlender, GeoFrame.ToWorld or MapPalette.Lit (their data is checked on its own). The pytest
        /// tools/tests/test_biome_colour_bake.py pins a fingerprint of that code.</summary>
        public const int Version = 1;
        const int Magic = 0x31424353;   // "SCB1"
        const int HeaderBytes = 16;

        public static byte[] Bytes(GeoFrame frame, HexFile hexes, Color32[] px)
        {
            int res = MapBuilder.BiomeTextureResolution;
            if (px.Length != res * res) throw new ArgumentException($"{px.Length} texels, not {res} x {res}", nameof(px));
            var bytes = new byte[HeaderBytes + 3 * px.Length];
            var head = new[] { Magic, Version, res, Check(frame, hexes) };
            for (int k = 0; k < head.Length; k++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4 * k), head[k]);
            for (int i = 0, o = HeaderBytes; i < px.Length; i++, o += 3) { bytes[o] = px[i].r; bytes[o + 1] = px[i].g; bytes[o + 2] = px[i].b; }
            return bytes;
        }

        /// <summary>Fills <paramref name="px"/> from a bake; false (and the reason) when it is not for these inputs.</summary>
        public static bool TryRead(byte[]? bytes, GeoFrame frame, HexFile hexes, Color32[] px, out string reason)
        {
            int res = MapBuilder.BiomeTextureResolution;
            if (bytes == null || bytes.Length < HeaderBytes) { reason = "missing or short"; return false; }
            var head = new int[4];
            for (int k = 0; k < head.Length; k++) head[k] = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4 * k));
            if (head[0] != Magic) { reason = "not a biome colour bake"; return false; }
            if (head[1] != Version) { reason = $"version {head[1]}, this build reads {Version} (re-bake: BiomeColourBake.Bake)"; return false; }
            if (head[2] != res || px.Length != res * res) { reason = $"made at {head[2]} texels a side, the map takes {res}"; return false; }
            if (head[3] != Check(frame, hexes)) { reason = "made from other hexes, frame, hex size, blend radius or palette (re-bake: BiomeColourBake.Bake)"; return false; }
            if (bytes.Length != HeaderBytes + 3L * px.Length) { reason = "truncated"; return false; }
            for (int i = 0, o = HeaderBytes; i < px.Length; i++, o += 3) px[i] = new Color32(bytes[o], bytes[o + 1], bytes[o + 2], 255);
            reason = "";
            return true;
        }

        /// <summary>The baked texels for these inputs, or null (with a warning) when the bake is missing or for others.</summary>
        public static Color32[]? Load(GeoFrame frame, HexFile hexes)
        {
            var asset = Resources.Load<TextAsset>(Resource);
            string why = "missing";
            if (asset != null)
            {
                var px = new Color32[MapBuilder.BiomeTextureResolution * MapBuilder.BiomeTextureResolution];
                bool ok = TryRead(asset.bytes, frame, hexes, px, out why);
                if (!Application.isEditor) Resources.UnloadAsset(asset);   // its bytes were copied; drop the native copy
                if (ok) return px;
            }
            Debug.LogWarning($"MAP {AssetPath}: {why}; blending the biome colours at load");
            return null;
        }

        /// <summary>FNV-1a over what the texels depend on: the resolution, the frame's bounds and size, the hex size and blend radius,
        /// each layer's colour, and every hex's centre and biome.</summary>
        public static int Check(GeoFrame frame, HexFile hexes)
        {
            uint h = 2166136261;
            void Bits(long v) { for (int k = 0; k < 8; k++) { h ^= (uint)(v >> (8 * k)) & 0xFF; h *= 16777619; } }
            void Single(float f) => Bits(BitConverter.SingleToInt32Bits(f));
            Bits(MapBuilder.BiomeTextureResolution);
            foreach (var d in new[] { frame.Lon0, frame.Lon1, frame.Lat0, frame.Lat1 }) Bits(BitConverter.DoubleToInt64Bits(d));
            Single(frame.WidthM); Single(frame.LengthM); Single(hexes.hex_size_km); Single(BiomeBlender.BlendRadiusHexes);
            foreach (var c in MapBuilder.BiomeLayerColours()) { Single(c.r); Single(c.g); Single(c.b); Single(c.a); }
            foreach (var hex in hexes.hexes)
            {
                Bits(BitConverter.DoubleToInt64Bits(hex.lon)); Bits(BitConverter.DoubleToInt64Bits(hex.lat));
                foreach (var ch in hex.biome) Bits(ch);
                Bits(-1);
            }
            return (int)h;
        }
    }
}
