#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

/// <summary>Issue #321: the biome ground colours (MapBuilder.BiomePixels, about a third of the "biome texture baked" boot
/// phase's work) are baked into BiomeColours.bytes by Editor BiomeColourBake, so a player reads them instead of blending
/// a million texels at boot. The bake must be what the map would blend now, texel for texel, so the map looks the same;
/// a bake made from other inputs is refused and the map blends the colours itself, so it is never wrong, only slower.</summary>
public class BiomeColourBakeTests
{
    static (HeightmapMeta meta, float[,] heights, HexFile hexes, GeoFrame frame) Inputs()
    {
        var (meta, heights) = HeightmapLoader.Load(Paths.StreamingMap);
        var hexes = MapBuilder.LoadHexes(System.IO.Path.Combine(Paths.Content, "map", "hexes.json"));
        var size = MapBuilder.TerrainSize(meta);
        return (meta, heights, hexes, new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z));
    }

    [Test]
    public void TheBakedBiomeColoursAreTheOnesTheMapWouldBlend()
    {
        var (_, _, hexes, frame) = Inputs();
        var baked = AssetDatabase.LoadAssetAtPath<TextAsset>(BiomeColourFile.AssetPath);
        Assert.IsNotNull(baked, BiomeColourFile.AssetPath);
        var blended = MapBuilder.BiomePixels(frame, hexes);
        CollectionAssert.AreEqual(BiomeColourFile.Bytes(frame, hexes, blended), baked.bytes,
            "stale: re-run Statecraft.Game.EditorTools.BiomeColourBake.Bake");
        var read = new Color32[blended.Length];
        Assert.IsTrue(BiomeColourFile.TryRead(baked.bytes, frame, hexes, read, out var why), why);
        for (int i = 0; i < blended.Length; i++)
            if (!blended[i].Equals(read[i])) Assert.Fail($"texel {i}: baked {read[i]}, blended {blended[i]}");
    }

    [Test]
    public void ABakeFromOtherInputsIsRefused()
    {
        var (meta, _, hexes, frame) = Inputs();
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BiomeColourFile.AssetPath);
        Assert.IsNotNull(asset, BiomeColourFile.AssetPath);
        var bytes = asset.bytes;
        var px = new Color32[MapBuilder.BiomeTextureResolution * MapBuilder.BiomeTextureResolution];
        Assert.IsTrue(BiomeColourFile.TryRead(bytes, frame, hexes, px, out var why), why);

        var h = hexes.hexes[400]; var was = h.biome;
        h.biome = was == "desert" ? "hills" : "desert";
        Assert.IsFalse(BiomeColourFile.TryRead(bytes, frame, hexes, px, out why), "another biome");
        h.biome = was;
        var wider = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, frame.WidthM + 1f, frame.LengthM);
        Assert.IsFalse(BiomeColourFile.TryRead(bytes, wider, hexes, px, out why), "another frame");
        Assert.IsFalse(BiomeColourFile.TryRead(new byte[12], frame, hexes, px, out why), "short");
        var cut = new byte[bytes.Length - 3]; System.Array.Copy(bytes, cut, cut.Length);   // a whole header, a texel short
        Assert.IsFalse(BiomeColourFile.TryRead(cut, frame, hexes, px, out why), "truncated");
        StringAssert.Contains("truncated", why);
        var flipped = (byte[])bytes.Clone(); flipped[0] ^= 1;
        Assert.IsFalse(BiomeColourFile.TryRead(flipped, frame, hexes, px, out why), "not a bake");
    }

    /// <summary>The boot path: MapBuilder.Build with the bake hands the ground tint (and so the texture) exactly the texels
    /// it hands it without, and logs no fallback.</summary>
    [Test]
    public void TheMapGetsTheSameGroundFromTheBake()
    {
        var (meta, heights, hexes, _) = Inputs();
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat");
        Assert.IsNotNull(material);
        Color32[] Ground(bool baked)
        {
            Color32[]? seen = null;
            var go = MapBuilder.Build(meta, heights, hexes, material, (px, res, f) => seen = (Color32[])px.Clone(), baked);
            try { Assert.IsNotNull(seen); return seen!; }
            finally
            {
                go.GetComponent<MapRuntimeAssets>().RunTeardown();
                Object.DestroyImmediate(go);
            }
        }
        var derived = new List<string>();
        void Watch(string message, string stack, LogType type) { if (message.Contains("blending the biome colours")) derived.Add(message); }
        Application.logMessageReceived += Watch;
        Color32[] fromBake;
        try { fromBake = Ground(true); }
        finally { Application.logMessageReceived -= Watch; }
        CollectionAssert.IsEmpty(derived, "the map uses the bake");
        var blended = Ground(false);
        Assert.AreEqual(blended.Length, fromBake.Length);
        for (int i = 0; i < blended.Length; i++)
            if (!blended[i].Equals(fromBake[i])) Assert.Fail($"texel {i}: baked {fromBake[i]}, blended {blended[i]}");
    }
}
