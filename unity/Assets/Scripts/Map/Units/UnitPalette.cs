#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The paint pots of the unit miniatures: one column per slot, one row per shade. Sources are in docs/units.md:
    /// 1948 IDF khaki drill and British 37-pattern webbing (photographs); the IAF roundel (a blue star on a white disc);
    /// 101 Squadron's red spinner and red-and-white rudder (from early October 1948: Efrati, Colors &amp; Markings of the Israeli
    /// Air Force, p. 12). The Spitfire's RAF day-fighter colours (Ocean Grey and Dark Green over Medium Sea Grey) are
    /// Efrati's p. 14. The S-199's khaki green is about RAL 6013 (Efrati pp. 8 and 11). The corvette's light grey follows its 1948 photograph. Colours are
    /// pushed a little warmer and lighter than the photographs so they read at map scale under the warm grade, as the
    /// promo's painted figures do. The figures' uniform slots (shirt, faded shirt, trousers, bush hat) are the exception:
    /// they are darker and browner than the photographs' khaki, so a figure stands out against the sunlit ground as promo
    /// 03's do (a readability choice at map scale, not a claim about the cloth: the 1948 photographs are black and white). The texture is made at runtime (no file ships), owned by the showcase.</summary>
    public static class UnitPalette
    {
        public const int Skin = 0, Shirt = 1, Trousers = 2, Webbing = 3, Helmet = 4, SlouchHat = 5, WoolCap = 6, Boots = 7,
            RifleWood = 8, RifleMetal = 9, Hair = 10, ShirtFaded = 11,
            CamoGrey = 12, CamoGreen = 13, Underside = 14, RoundelWhite = 15, RoundelBlue = 16, PropBlack = 17, Glass = 18,
            HullGrey = 19, Deck = 20, Waterline = 21, Funnel = 22, MarkingRed = 23, AviaGreen = 24, Earth = 25, Count = 26;

        /// <summary>Texels per slot along u: the sample at a slot's centre never reaches its neighbours (bilinear, no mips).</summary>
        public const int SlotWidth = 4, Rows = 32;

        static readonly Color[] Base =
        {
            new Color(0.80f, 0.60f, 0.46f),   // Skin: sun-tanned
            new Color(0.42f, 0.31f, 0.21f),   // Shirt: khaki drill, darker and warmer than the photographs' (see the class note)
            new Color(0.26f, 0.19f, 0.14f),   // Trousers: khaki drill, darker and dustier (the photographs' trousers read darker than the shirts)
            new Color(0.86f, 0.80f, 0.62f),   // Webbing: 37-pattern, pale
            new Color(0.30f, 0.31f, 0.22f),   // Helmet: Mk II steel helmet, dark olive
            new Color(0.38f, 0.28f, 0.19f),   // SlouchHat: khaki felt
            new Color(0.36f, 0.33f, 0.27f),   // WoolCap: knitted stocking cap, dark
            new Color(0.20f, 0.14f, 0.10f),   // Boots
            new Color(0.45f, 0.28f, 0.16f),   // RifleWood
            new Color(0.20f, 0.20f, 0.21f),   // RifleMetal
            new Color(0.17f, 0.13f, 0.10f),   // Hair
            new Color(0.50f, 0.38f, 0.26f),   // ShirtFaded: sun-bleached khaki
            new Color(0.47f, 0.50f, 0.50f),   // CamoGrey: ocean grey
            new Color(0.27f, 0.32f, 0.22f),   // CamoGreen: dark green
            new Color(0.68f, 0.71f, 0.72f),   // Underside: medium sea grey
            new Color(0.95f, 0.95f, 0.93f),   // RoundelWhite
            new Color(0.07f, 0.17f, 0.45f),   // RoundelBlue
            new Color(0.08f, 0.08f, 0.08f),   // PropBlack
            new Color(0.20f, 0.26f, 0.30f),   // Glass: canopy
            new Color(0.56f, 0.59f, 0.60f),   // HullGrey
            new Color(0.48f, 0.46f, 0.42f),   // Deck
            new Color(0.12f, 0.12f, 0.13f),   // Waterline
            new Color(0.30f, 0.31f, 0.32f),   // Funnel top
            new Color(0.72f, 0.12f, 0.10f),   // MarkingRed: 101 Squadron's spinner and rudder stripes
            new Color(0.47f, 0.45f, 0.32f),   // AviaGreen: the S-199's one khaki green on all surfaces, about RAL 6013 (Efrati pp. 8 and 11: confirmed from an Avia fuel cap; Eduard Info 11/2025 pp. 53-54)
            new Color(0.45f, 0.33f, 0.22f),   // Earth: a token's base, the dark warm earth of demo2 T29's bases (rendered about 0.24-0.30 luma there), mottled by its facets' shade rows
        };

        /// <summary>u of a slot's column centre.</summary>
        public static float U(int slot) => (slot * SlotWidth + SlotWidth * 0.5f) / (Count * SlotWidth);

        /// <summary>The paint at shade row v (0 at the model's lowest point, 1 at its highest): a dark wash near the ground
        /// (with a little dust in it), the base colour through the middle and a dry-brushed highlight on top.</summary>
        public static Color Shade(int slot, float v)
        {
            var c = Base[slot];
            float k = Mathf.Lerp(0.62f, 1.0f, Mathf.SmoothStep(0f, 0.45f, v)) + 0.08f * Mathf.SmoothStep(0.7f, 1f, v);
            var dust = new Color(0.58f, 0.50f, 0.38f);
            var shaded = c * k;
            return Color.Lerp(shaded, dust * 0.8f, 0.25f * (1f - Mathf.SmoothStep(0f, 0.12f, v)));
        }

        /// <summary>The palette texture. <paramref name="readable"/> false frees its CPU copy after the upload (the runtime
        /// path; tests read the pixels back).</summary>
        public static Texture2D MakeTexture(bool readable = true)
        {
            int w = Count * SlotWidth;
            var tex = new Texture2D(w, Rows, TextureFormat.RGBA32, false, false)
            { name = "UnitPalette", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[w * Rows];
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = Shade(x / SlotWidth, (y + 0.5f) / Rows); c.a = 1f;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px); tex.Apply(false, !readable);
            return tex;
        }
    }
}
