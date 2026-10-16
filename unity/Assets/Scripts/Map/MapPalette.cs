#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The ground colours the terrain shader paints per biome (sRGB; MapBuilder bakes them, blended between hexes,
    /// into the biome texture). Warmer and more saturated than MapBuilder.BiomeColours, which the minimap keeps. Land round 2
    /// (#392): promo 03's warm dry ground on the coastal plain, the valleys and the lowland, at about the promo ground's hue
    /// (28.6 degrees); an orange brown on the hills and mountains (22 degrees, saturation about 0.3; review of #392: a
    /// red hue at low saturation read rose); and
    /// sand-amber desert (research notes §1); issue #709 (owner, 16 October 2026): a warmer sand, less blue (was 0.84,
    /// 0.70, 0.48), toward promo 02's, held to what keeps the red-orange border stripe readable on it for colour-blind
    /// players (GradeTests.ColourBlindPlayersTellTheBorderStylesApart). Merge fix round 1's plain brightness (both promos' plain V about 0.54) is kept.
    /// "sea" and "lake" are only seen where a lake or sea hex's blend reaches the shore; the water itself is shaded from the
    /// coast texture.</summary>
    public static class MapPalette
    {
        /// <summary>Every ground colour is brightened by this factor in sRGB (about 1.25 in linear light) and clamped to
        /// MaxChannel: the land keeps its luma once the haze no longer lifts it, warm instead of milky (Task 6 review I1).</summary>
        public const float Brightness = 1.107f, MaxChannel = 0.95f;

        public static IReadOnlyDictionary<string, Color> Ground { get; } = new Dictionary<string, Color>
        {
            ["sea"] = Lit(0.62f, 0.60f, 0.42f), ["lake"] = Lit(0.60f, 0.58f, 0.42f),
            ["coastal_plain"] = Lit(0.603f, 0.508f, 0.43f), ["hills"] = Lit(0.66f, 0.526f, 0.449f),
            ["mountains"] = Lit(0.639f, 0.518f, 0.447f), ["valley"] = Lit(0.58f, 0.505f, 0.425f),
            ["lowland"] = Lit(0.595f, 0.51f, 0.43f), ["desert"] = Lit(0.86f, 0.70f, 0.44f),
        };

        static Color Lit(float r, float g, float b) =>
            new Color(Mathf.Min(r * Brightness, MaxChannel), Mathf.Min(g * Brightness, MaxChannel), Mathf.Min(b * Brightness, MaxChannel));
    }
}
