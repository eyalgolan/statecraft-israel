#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The map's colour grade (research notes §1, §2.2 "Colour grading"): a Neutral tonemapper owned by the map (the
    /// pipeline assets' template profile already applied one; without any, the graded Negev clips about 2.5% of its
    /// pixels: GradeTests.TheSunlitDesertDoesNotClip), brighter, more contrast with darker shadows, slightly less saturated,
    /// a warm white balance with a touch of green tint, near-neutral shadows and highlights, hue curves that turn the land to
    /// gold (issue #2427), and a dark-navy vignette. LDR
    /// grading on both tiers: the tonemapper and one LUT in the uber pass.
    ///
    /// Issue #38 retune (docs/visual-v1.md, "Issue #38: grade retune"), fitted on WebGL frames against promo 03's land and
    /// re-tuned under the owner's four decisions on #519. Against main at the gameplay pose: the land's contrast comes to the
    /// promo's (0.185 against 0.187; main 0.167), its warmth and saturation stay at main's (0.193 / 0.353; main 0.191 /
    /// 0.351, promo 0.181 / 0.363), its hue stays tan rather than rose, and it is brighter (luma 0.505; main 0.485, promo
    /// 0.425); the sea stays blue, a little darker (0.335; main 0.340, promo 0.351). One global grade cannot match the land
    /// fully and keep the sea.</summary>
    public static class MapGrade
    {
        /// <summary>Neutral, not ACES: it only rolls off highlights, so the sea's blue and the amber land keep their hue (plan Task 4).</summary>
        public const TonemappingMode Tonemapper = TonemappingMode.Neutral;
        /// <summary>Temperature 3 since issue #38 (was 5). With the near-neutral highlights below and Saturation -7 it keeps
        /// diorama02's whole-frame warmth at 0.142, under #38's 0.145 (docs/visual-v1.md, "Issue #38: grade retune"). History:
        /// Temperature 5, not the plan's 9 (Task 4 merge arithmetic, docs/visual-v1.md "Task 4: grade"): at 9 the t04 diorama
        /// warmth was 0.118, and Track B's land (+0.021 before the grade, about +0.027 after it) would put the merged diorama at
        /// the promo's 0.145 with nothing left for Track B's later haze and palette work.</summary>
        public const float Temperature = 3f;
        /// <summary>Saturation -7 since issue #38 (was 0). Contrast 18 raises every colour's saturation; -7 takes most of it
        /// back, which keeps the land's warmth at main's and the Israeli border stripe near the owner's ruled blue
        /// (ControlTests' ThePrimaryStripeIsTheDeeperBlueOfTheRuling: 2.1 deltaE76, 0.6 on main). The frame's saturation
        /// is x1.000 of the ungraded frame (GradeTests' floor x0.98; a -8 candidate measured x0.980 and failed it). History: 0, not Task 4's
        /// 16 (merge-point carry, review I1): with Track B's palette the merged WebGL diorama02 measured 0.53 against the
        /// promo's 0.43 at 16; the bar is promo + 0.05 (docs/visual-v1.md "Merge point").</summary>
        public const float Saturation = -7f;
        /// <summary>Issue #2427: Contrast 20 (#38's 18), for the land's depth; the sea sits below the contrast's pivot, and the
        /// stronger sun (MapLighting.SunIntensity) gives back what it takes. PostExposure stays the owner's 0.45.
        /// Issue #38: Contrast 18 (was 14) and PostExposure 0.45 (was 0.15): crisper, with the low end pulled down by
        /// the balance below and the mid-tones lifted back (owner's decision on #519, 2026-10-15, "Exposure 0.45, sea slightly
        /// darker"). Tint -2 (was 2): a touch of green instead of magenta, part of the owner's "Less pink" (2026-10-15), so the
        /// land does not read rose.</summary>
        public const float PostExposure = 0.45f, Contrast = 20f, Tint = -2f;
        /// <summary>Issue #38: balance -31 (was -15). The vignette stays at 0.26 and 0.45: the retune's stronger, softer 0.34 and
        /// 0.84 darkened gameplay03's sea, which sits on the frame's left edge, well below promo 03's (owner's rule on #519:
        /// "if the sea is still dark, use main's vignette"; docs/visual-v1.md, "Issue #38: grade retune").</summary>
        public const float SplitBalance = -31f, VignetteIntensity = 0.26f, VignetteSmoothness = 0.45f;
        /// <summary>Issue #38: near-neutral shadows (0.43, 0.46, 0.49), were teal (0.36, 0.52, 0.60). Under neutral in every
        /// channel, so they darken the low end. Their blue only a little over red: bluer shadows (the retune's slate 0.45,
        /// 0.45, 0.53) pushed the border stripe 8 deltaE76 from the owner's blue, and a red-heavy tone turned the diorama's
        /// table maroon. Warm shadows would match the land better (#391) but turn the sea grey.</summary>
        public static readonly Color SplitShadows = new Color(0.43f, 0.46f, 0.49f);
        /// <summary>Issue #38: near-neutral highlights (0.84, 0.87, 0.835), were amber (0.96, 0.78, 0.55). Green highest so
        /// the land reads tan, not rose (the retune's pale rose 0.89, 0.80, 0.88 turned the hills and the Negev pink; owner's
        /// "Less pink", 2026-10-15); red still just over blue (by 0.005), as GradeTests' warm-highlights check asks.</summary>
        public static readonly Color SplitHighlights = new Color(0.84f, 0.87f, 0.835f);
        public static readonly Color VignetteColour = new Color(0.05f, 0.06f, 0.10f);

        /// <summary>Issue #2427: saturation by linear luminance (0 = no change): dark tones lose 14%, which keeps the hill olives
        /// grey (KitTests' olive crowns: 0.331 on main, promo 0.274-0.370) under the warmer light and comes towards the promo's
        /// shade (saturation about 0.3 against our 0.46). It ends at 0.09, about the sea's mean; the sea's mean colour moves
        /// by under 1/255 (GradeTests' TheLandReadsGoldenAndTheSeaHolds).</summary>
        public static readonly Keyframe[] LumVsSat = { Key(0f, -0.07f), Key(0.075f, -0.07f), Key(0.09f, 0f), Key(1f, 0f) };

        static Keyframe Key(float hue, float value) => new Keyframe(hue, 0.5f + value, 0f, 0f);

        /// <summary>Issue #2427: the land's golden turn, as an offset to the hue of each colour (hue 0-1 in linear HSV, 0 = no
        /// change; docs/visual-v1.md, "Issue #2427"). The land's terracotta (hue about 0.06) moves up and the green fields (0.2)
        /// move down, so they meet at gold. Flat at the reds (roofs, the other side's stripe, up to 0.035), at the sand (0.09-0.13:
        /// shifting it took the dune coast under SeaTests' luma floor) and from 0.38 up (the surf, the sea, the Israeli stripe),
        /// which is what one global grade could not do (#38: warm grades turned the sea grey). Flat keys, so no curve overshoots
        /// between them: smoothed tangents leaked a shift into the sea's hue, 0.014 of luma. LUT inputs: no pass, no variant.</summary>
        public static readonly Keyframe[] HueVsHue =
        {
            Key(0f, 0f), Key(0.035f, 0f), Key(0.06f, 0.013f), Key(0.08f, 0.002f), Key(0.09f, 0f), Key(0.13f, 0f), Key(0.15f, -0.004f),
            Key(0.2f, -0.03f), Key(0.3f, -0.02f), Key(0.38f, 0f), Key(1f, 0f),
        };

        /// <summary>Issue #2427: saturation by hue (0 = no change, 0.5 would double it): richer land, the sand unchanged, so the
        /// Negev stays clear of mustard. Global Saturation stays -7: raising it moves the sea and the stripe (a +9 trial: sea
        /// warmth -0.216 -> -0.238).</summary>
        public static readonly Keyframe[] HueVsSat =
        {
            Key(0f, 0f), Key(0.025f, 0f), Key(0.055f, 0.02f), Key(0.075f, 0.015f), Key(0.09f, 0f), Key(1f, 0f),
        };

        public static void Apply(VolumeProfile profile)
        {
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(Tonemapper);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(PostExposure); ca.contrast.Override(Contrast); ca.saturation.Override(Saturation);
            ca.hueShift.Override(0f); ca.colorFilter.Override(Color.white);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(Temperature); wb.tint.Override(Tint);
            var st = profile.Add<SplitToning>(true);
            st.shadows.Override(SplitShadows); st.highlights.Override(SplitHighlights); st.balance.Override(SplitBalance);
            var curves = profile.Add<ColorCurves>(true);
            curves.hueVsHue.Override(new TextureCurve(HueVsHue, 0.5f, true, new Vector2(0f, 1f)));
            curves.hueVsSat.Override(new TextureCurve(HueVsSat, 0.5f, true, new Vector2(0f, 1f)));
            curves.lumVsSat.Override(new TextureCurve(LumVsSat, 0.5f, false, new Vector2(0f, 1f)));
            var v = profile.Add<Vignette>(true);
            v.intensity.Override(VignetteIntensity); v.smoothness.Override(VignetteSmoothness); v.color.Override(VignetteColour);
            v.center.Override(new Vector2(0.5f, 0.5f)); v.rounded.Override(false);
        }

        /// <summary>The "grade" toggle. Off falls back to the pipeline assets' own profile (Settings/SampleSceneProfile: Neutral
        /// tonemapping and a 0.2 black vignette; its bloom is off on both tiers since Task 10), which every frame had before Task 4, not to a raw image.</summary>
        public static void SetActive(VolumeProfile profile, bool on)
        {
            foreach (var c in profile.components)
                if (c is Tonemapping || c is ColorAdjustments || c is WhiteBalance || c is SplitToning || c is ColorCurves || c is Vignette) c.active = on;
        }
    }
}
