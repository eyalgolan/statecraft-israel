#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The map's colour grade (research notes §1, §2.2 "Colour grading"): a Neutral tonemapper owned by the map (the
    /// pipeline assets' template profile already applied one; without any, the graded Negev clips about 2.5% of its
    /// pixels: GradeTests.TheSunlitDesertDoesNotClip), brighter, more contrast with darker shadows, saturation about
    /// neutral, a warm white balance, cool slate shadows against amber highlights (the promo's complementary sea and land),
    /// and a dark-navy vignette. LDR grading on both tiers: the tonemapper and one LUT in the uber pass.
    ///
    /// Issue #38 retune (docs/visual-v1.md, "Issue #38: grade retune"): fitted on WebGL frames to bring the land towards
    /// promo 03's land (p5 luma, contrast, saturation, warmth, luma) while the sea stays as blue as before. One global grade
    /// cannot do both fully; this is the closest land match that keeps the sea tests (SeaTests, BorderTests) passing.</summary>
    public static class MapGrade
    {
        /// <summary>Neutral, not ACES: it only rolls off highlights, so the sea's blue and the amber land keep their hue (plan Task 4).</summary>
        public const TonemappingMode Tonemapper = TonemappingMode.Neutral;
        /// <summary>Temperature 7 since issue #38 (was 5): the cooler highlights and slate shadows take some frame warmth,
        /// and this gives it back (gameplay03 frame warmth 0.05-0.07). History: Temperature 5, not the plan's 9 (Task 4 merge arithmetic, docs/visual-v1.md "Task 4: grade"): at 9 the t04
        /// diorama warmth was 0.118, and Track B's land (+0.021 before the grade, about +0.027 after it) would put the merged
        /// diorama at the promo's 0.145 with nothing left for Track B's later haze and palette work. Each unit is about 0.003
        /// of frame warmth, so 5 aims the merge at about 0.133.</summary>
        public const float Temperature = 7f;
        /// <summary>Saturation -4 since issue #38 (offsets part of what Contrast 18 adds). Before: 0, not Task 4's 16 (merge-point carry, review I1): with Track B's palette the merged WebGL
        /// diorama02 measured 0.53 against the promo's 0.43 at 16; the bar is promo + 0.05 (docs/visual-v1.md "Merge point").</summary>
        public const float Saturation = -4f;
        /// <summary>Issue #38: Contrast 18 (was 14) and PostExposure 0.55 (was 0.15). The darker shadow tone and the stronger
        /// split balance below pull the low end down; the exposure keeps the land's mean luma close to where it was.</summary>
        public const float PostExposure = 0.55f, Contrast = 18f, Tint = 2f;
        /// <summary>Issue #38: balance -31 (was -15). The vignette stays at 0.26 and 0.45: the retune's stronger, softer 0.34 and
        /// 0.84 darkened gameplay03's sea, which sits on the frame's left edge, well below promo 03's (owner's rule on #519:
        /// "if the sea is still dark, use main's vignette"; docs/visual-v1.md, "Issue #38: grade retune").</summary>
        public const float SplitBalance = -31f, VignetteIntensity = 0.26f, VignetteSmoothness = 0.45f;
        /// <summary>Issue #38: cool slate shadows (0.45, 0.45, 0.53), under neutral in red and green so they darken the low
        /// end. They were teal (0.36, 0.52, 0.60); warm shadows would match the land better (#391) but turn the sea grey.</summary>
        public static readonly Color SplitShadows = new Color(0.45f, 0.45f, 0.53f);
        /// <summary>Issue #38: blue 0.62 (was 0.55), a little less amber, so the land's warmth stays near the promo's.</summary>
        public static readonly Color SplitHighlights = new Color(0.96f, 0.78f, 0.62f);
        public static readonly Color VignetteColour = new Color(0.05f, 0.06f, 0.10f);

        public static void Apply(VolumeProfile profile)
        {
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(Tonemapper);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(PostExposure); ca.contrast.Override(Contrast); ca.saturation.Override(Saturation);
            ca.hueShift.Override(0f); ca.colorFilter.Override(Color.white);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(Temperature); wb.tint.Override(Tint);
            var st = profile.Add<SplitToning>(true);
            st.shadows.Override(SplitShadows); st.highlights.Override(SplitHighlights); st.balance.Override(SplitBalance);
            var v = profile.Add<Vignette>(true);
            v.intensity.Override(VignetteIntensity); v.smoothness.Override(VignetteSmoothness); v.color.Override(VignetteColour);
            v.center.Override(new Vector2(0.5f, 0.5f)); v.rounded.Override(false);
        }

        /// <summary>The "grade" toggle. Off falls back to the pipeline assets' own profile (Settings/SampleSceneProfile: Neutral
        /// tonemapping and a 0.2 black vignette; its bloom is off on both tiers since Task 10), which every frame had before Task 4, not to a raw image.</summary>
        public static void SetActive(VolumeProfile profile, bool on)
        {
            foreach (var c in profile.components)
                if (c is Tonemapping || c is ColorAdjustments || c is WhiteBalance || c is SplitToning || c is Vignette) c.active = on;
        }
    }
}
