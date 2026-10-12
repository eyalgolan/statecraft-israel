#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The ground colours the terrain shader paints per biome (sRGB; MapBuilder bakes them, blended between hexes,
    /// into the biome texture). Warmer and more saturated than MapBuilder.BiomeColours, which the minimap keeps: olive on
    /// the coastal plain and the valleys (merge fix round 1: both promos' plain measures V about 0.54, R-B about 0.22, where
    /// the earlier dry-grass gold rendered 0.73 and 0.37), olive-ochre hills, sand-amber desert (research notes §1, promo 03). "sea" and "lake" are
    /// only seen where a lake or sea hex's blend reaches the shore; the water itself is shaded from the coast texture.</summary>
    public static class MapPalette
    {
        /// <summary>Every ground colour is brightened by this factor in sRGB (about 1.25 in linear light) and clamped to
        /// MaxChannel: the land keeps its luma once the haze no longer lifts it, warm instead of milky (Task 6 review I1).</summary>
        public const float Brightness = 1.107f, MaxChannel = 0.95f;

        public static IReadOnlyDictionary<string, Color> Ground { get; } = new Dictionary<string, Color>
        {
            ["sea"] = Lit(0.62f, 0.60f, 0.42f), ["lake"] = Lit(0.60f, 0.58f, 0.42f),
            ["coastal_plain"] = Lit(0.52f, 0.54f, 0.33f), ["hills"] = Lit(0.66f, 0.58f, 0.38f),
            ["mountains"] = Lit(0.62f, 0.53f, 0.40f), ["valley"] = Lit(0.50f, 0.53f, 0.33f),
            ["lowland"] = Lit(0.51f, 0.535f, 0.33f), ["desert"] = Lit(0.84f, 0.70f, 0.48f),
        };

        static Color Lit(float r, float g, float b) =>
            new Color(Mathf.Min(r * Brightness, MaxChannel), Mathf.Min(g * Brightness, MaxChannel), Mathf.Min(b * Brightness, MaxChannel));
    }
}
