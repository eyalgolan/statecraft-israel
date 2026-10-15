#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The map's colour grade (research notes §1, §2.2 "Colour grading"): a Neutral tonemapper owned by the map (the
    /// pipeline assets' template profile already applied one; without any, the graded Negev clips about 2.5% of its
    /// pixels: GradeTests.TheSunlitDesertDoesNotClip), brighter, more contrast with darker shadows, slightly less saturated,
    /// a warm white balance with a touch of green tint, near-neutral shadows and highlights, and a dark-navy vignette. LDR
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
        /// <summary>Issue #38: Contrast 18 (was 14) and PostExposure 0.45 (was 0.15): crisper, with the low end pulled down by
        /// the balance below and the mid-tones lifted back (owner's decision on #519, 2026-10-15, "Exposure 0.45, sea slightly
        /// darker"). Tint -2 (was 2): a touch of green instead of magenta, part of the owner's "Less pink" (2026-10-15), so the
        /// land does not read rose.</summary>
        public const float PostExposure = 0.45f, Contrast = 18f, Tint = -2f;
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
