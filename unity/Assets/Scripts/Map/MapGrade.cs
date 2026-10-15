#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The map's colour grade (research notes §1, §2.2 "Colour grading"): a Neutral tonemapper owned by the map (the
    /// pipeline assets' template profile already applied one; without any, the graded Negev clips about 2.5% of its
    /// pixels: GradeTests.TheSunlitDesertDoesNotClip), brighter, more contrast with darker shadows, saturation about
    /// neutral, a warm white balance, cool slate shadows and pale highlights,
    /// and a dark-navy vignette. LDR grading on both tiers: the tonemapper and one LUT in the uber pass.
    ///
    /// Issue #38 retune (docs/visual-v1.md, "Issue #38: grade retune"), fitted on WebGL frames against promo 03's land and
    /// re-tuned under the owner's three decisions on #519: the land's contrast comes to the promo's (0.184 against 0.187;
    /// main 0.167) and the sea stays blue, while the land stays brighter (luma 0.501 against 0.425) and slightly warmer
    /// than main's (0.196 against 0.191; promo 0.181). One global grade cannot match the land fully and keep the sea.</summary>
    public static class MapGrade
    {
        /// <summary>Neutral, not ACES: it only rolls off highlights, so the sea's blue and the amber land keep their hue (plan Task 4).</summary>
        public const TonemappingMode Tonemapper = TonemappingMode.Neutral;
        /// <summary>Temperature 3 since issue #38 (was 5). With the paler highlights below it takes diorama02's whole-frame
        /// warmth to 0.144, under #38's 0.145. At post exposure 0.55 a white balance this cool put the Israeli border stripe
        /// further from the owner's ruled blue than main's lighter blue (ControlTests' ThePrimaryStripeIsTheDeeperBlueOfTheRuling);
        /// at 0.45 the stripe is 8.1 deltaE76 from the ruled blue (0.6 on main), nearer than main's blue (9.2), so the test
        /// passes (owner's decision on #519, 2026-10-15; docs/visual-v1.md, "Issue #38: grade retune"). History: Temperature 5, not the plan's 9 (Task 4
        /// merge arithmetic, docs/visual-v1.md "Task 4: grade"): at 9 the t04 diorama warmth was 0.118, and Track B's land
        /// (+0.021 before the grade, about +0.027 after it) would put the merged diorama at the promo's 0.145 with nothing left
        /// for Track B's later haze and palette work.</summary>
        public const float Temperature = 3f;
        /// <summary>Saturation -2 since issue #38 (was 0). It offsets part of what Contrast 18 adds and moves the border stripe
        /// less than 0 did; -4 with the paler highlights fell under GradeTests' 0.98 floor for the frame (x0.967), and with
        /// this grade -2 measures x0.998. History: 0, not Task 4's 16 (merge-point carry, review I1): with Track B's palette the merged WebGL
        /// diorama02 measured 0.53 against the promo's 0.43 at 16; the bar is promo + 0.05 (docs/visual-v1.md "Merge point").</summary>
        public const float Saturation = -2f;
        /// <summary>Issue #38: Contrast 18 (was 14) and PostExposure 0.45 (was 0.15). The darker shadow tone and the stronger
        /// split balance below pull the low end down; the exposure lifts it back. 0.45, not the first fit's 0.55: the owner's
        /// decision on #519 (2026-10-15, "Exposure 0.45, sea slightly darker"). Against 0.55 it darkens the land (luma 0.518
        /// -> 0.501) and the border stripe, so the cooler white balance above passes the stripe test, and the open sea by
        /// 0.015 (luma 0.348 -> 0.333; promo 03 0.351, main 0.340).</summary>
        public const float PostExposure = 0.45f, Contrast = 18f, Tint = 2f;
        /// <summary>Issue #38: balance -31 (was -15). The vignette stays at 0.26 and 0.45: the retune's stronger, softer 0.34 and
        /// 0.84 darkened gameplay03's sea, which sits on the frame's left edge, well below promo 03's (owner's rule on #519:
        /// "if the sea is still dark, use main's vignette"; docs/visual-v1.md, "Issue #38: grade retune").</summary>
        public const float SplitBalance = -31f, VignetteIntensity = 0.26f, VignetteSmoothness = 0.45f;
        /// <summary>Issue #38: cool slate shadows (0.45, 0.45, 0.53), under neutral in red and green so they darken the low
        /// end. They were teal (0.36, 0.52, 0.60); warm shadows would match the land better (#391) but turn the sea grey.</summary>
        public static readonly Color SplitShadows = new Color(0.45f, 0.45f, 0.53f);
        /// <summary>Issue #38: pale highlights (0.89, 0.80, 0.88), were amber (0.96, 0.78, 0.55). Less red and more blue than
        /// the first fit's (0.96, 0.78, 0.62) take the land's warmth from 0.229 to 0.196 with the cooler white balance (main
        /// 0.191, promo 03 0.181). Red stays over blue only by 0.01 and green is lowest, so the hue is a pale rose rather than
        /// amber, and the land reads rosier than main's. Hue only: the land's and the sea's luma do not move with it.</summary>
        public static readonly Color SplitHighlights = new Color(0.89f, 0.80f, 0.88f);
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
