#nullable enable
using System;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The tilt-shift band (plan Task 3; promo 03 blurs about the bottom 15% and top 20%: full blur starts
    /// HalfWidth + Feather = 0.30 from the focus, review I2). CameraRig.Apply calls
    /// Apply every frame with the top bar's inset and the rig distance; TiltShiftFeature draws the blur while Enabled. The
    /// radius is a share of the picture's height, so no pixel size is needed. Weight mirrors TiltWeight in TiltShift.shader.</summary>
    public static class TiltShift
    {
        public static readonly int ParamsId = Shader.PropertyToID("_ScTiltShift");
        public static readonly int FarId = Shader.PropertyToID("_ScTiltShiftFar");
        public static readonly int NearId = Shader.PropertyToID("_ScTiltShiftNear");
        public const float HalfWidth = 0.13f, Feather = 0.17f;
        /// <summary>The largest blur radius as a share of the picture's height: 14 px of a 1080-line picture (tap spacing
        /// 1.75 texels of the half-resolution target, still adequately sampled by the 9 taps).</summary>
        public const float MaxRadiusUv = 14f / 1080f;
        public const float FullStrengthDistanceM = 60000f, MinStrength = 0.35f;
        public static bool Enabled = true;
        /// <summary>Set by the web tier's runtime quality fallback (QualityFallbackDriver) at its last step. Kept apart from
        /// Enabled (the "tiltshift" toggle, ?notiltshift), so stepping back up never turns on a tilt-shift the player or a
        /// profile turned off. The blur draws only when Enabled and not FallbackOff.</summary>
        public static bool FallbackOff;
        public static bool Draws => Enabled && !FallbackOff;

        /// <summary>HLSL smoothstep(p.y, p.y + p.z, |y - p.x|); a zero feather is a step at the half-width.</summary>
        public static float Weight(float y, Vector4 p)
        {
            float d = Mathf.Abs(y - p.x);
            if (p.z <= 0f) return d >= p.y ? 1f : 0f;
            float t = Mathf.Clamp01((d - p.y) / p.z);
            return t * t * (3f - 2f * t);
        }

        /// <summary>The middle of the map area below a top bar covering <paramref name="topInset"/> of the screen.</summary>
        public static float FocusCentre(float topInset) => (1f - Mathf.Clamp(topInset, 0f, CameraRig.MaxTopInset)) * 0.5f;

        public static float Strength(float distance) =>
            Mathf.Lerp(1f, MinStrength, Mathf.InverseLerp(FullStrengthDistanceM, CameraController.MaxDistance, distance));

        /// <summary>The far (top) band's strength, scaling its radius and its blend (issue #390, the owner's ruling of
        /// 2026-10-13: a lighter tilt-shift at the gameplay zoom). FarGameplayStrength up to FarLightBelowM, the far edge only
        /// gently soft as on promo 03, so the far towns and tokens stay readable; rising to 1 at the overview
        /// (CameraController.MaxDistance, diorama02), which keeps the far band in full.</summary>
        public const float FarGameplayStrength = 0.35f, FarLightBelowM = 150000f;

        public static float FarStrength(float distance) =>
            Mathf.Lerp(FarGameplayStrength, 1f, Mathf.InverseLerp(FarLightBelowM, CameraController.MaxDistance, distance));

        /// <summary>The near (bottom) band's strength, scaling its radius and its blend (the owner's ruling of 2026-10-14: the
        /// near band as on promo 03). NearGameplayStrength up to NearLightBelowM, falling to 0 at the overview
        /// (CameraController.MaxDistance, diorama02), where promo 02's foreground is sharp: the opposite of FarStrength,
        /// which rises there. 0.75 is set by measure, not by eye: the gameplay03 shot's near band (rows 80-95%, columns
        /// 30-75%) then has promo 03's mean |Laplacian| (0.021; 1 gives 0.0056, 0.4 about 0.046). The half-resolution
        /// copy alone is softer than the frame, so the blend, not the radius, sets most of the sharpness.</summary>
        public const float NearGameplayStrength = 0.75f, NearLightBelowM = FarLightBelowM;

        public static float NearStrength(float distance) =>
            Mathf.Lerp(NearGameplayStrength, 0f, Mathf.InverseLerp(NearLightBelowM, CameraController.MaxDistance, distance));

        /// <summary>A band's strength at viewport y: the far band's above the focus, the near band's below (mirrors
        /// BandStrength in TiltShift.shader).</summary>
        public static float BandStrength(float y, Vector4 p, float far, float near) => y > p.x ? far : near;

        public static Vector4 Params(float topInset, float distance) =>
            new Vector4(FocusCentre(topInset), HalfWidth, Feather, MaxRadiusUv * Strength(distance));

        public static void Apply(float topInset, float distance)
        {
            Shader.SetGlobalVector(ParamsId, Params(topInset, distance));
            Shader.SetGlobalFloat(FarId, FarStrength(distance));
            Shader.SetGlobalFloat(NearId, NearStrength(distance));
        }

        /// <summary>No band set: TiltShiftFeature draws nothing until the next Apply (a camera no rig drives, or the map torn
        /// down), and the toggle is back on. CameraRig.Init hands it to the map's teardown.</summary>
        public static void Reset()
        {
            Enabled = true;
            FallbackOff = false;
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
        }

        /// <summary>"notiltshift" in the page URL (-notiltshift on desktop): boot with the tilt-shift off, so a WebGL shot
        /// with the real top bar can be compared with one that has it (review m3). CameraRig.Init reads it.</summary>
        public static bool OffRequested() =>
            OffRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool OffRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "notiltshift") : args.Contains("-notiltshift");

        /// <summary>True when a rig has set a band with a radius. With the zero global (before the map's first Apply, or after
        /// its teardown) smoothstep(0, 0, |y|) is about 1 everywhere, so the composite would replace the whole frame with its
        /// half-resolution copy at full cost.</summary>
        public static bool HasBand => Shader.GetGlobalVector(ParamsId).w > 0f;
    }

    /// <summary>Asset paths the tests share with the editor scripts (tests cannot reference Assets/Editor).</summary>
    public static class PostFxPaths
    {
        public const string TiltShiftMaterial = "Assets/Materials/TiltShift.mat";
    }
}
