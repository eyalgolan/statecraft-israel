#nullable enable
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The player's graphics quality (#40, owner decision of 18 Oct 2026), the same three choices on the web and the
    /// desktop. Auto is today's behaviour: the tier the device picked (the per-platform default quality level) at its own
    /// render scale, with the WebGL fallback (QualityFallback) stepping down when frames are slow. High is the PC tier at
    /// render scale 1.0, Low the Mobile tier at 0.8 (the fallback's lowest scale), both fixed: the fallback holds while one is
    /// set (QualityFallbackDriver.Feed). Presentation only: never the sim or the save. The tilt-shift stays its own option.</summary>
    public static class GraphicsQuality
    {
        public enum Choice { Auto, High, Low }

        public const string HighTier = "PC", LowTier = "Mobile";
        public const float HighRenderScale = 1f, LowRenderScale = 0.8f;

        public static Choice Current { get; private set; } = Choice.Auto;

        /// <summary>After a tier switch (CameraRig refits the new tier's shadow distance, which ShadowFit keeps per asset).</summary>
        public static event Action? Changed;

        static int bootLevel = -1;   // the level the device picked, which Auto goes back to

#if UNITY_EDITOR
        // In the editor the URP assets are project files: leaving Play mode puts every tier's own scale and the editor's
        // level back, so a choice made while playing is never saved into Assets/Settings.
        [UnityEditor.InitializeOnLoadMethod]
        static void PutTheTiersBackOnExit() => UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode || Current == Choice.Auto) return;
            Apply(Choice.Auto);
            int low = Array.IndexOf(QualitySettings.names, LowTier);
            if (low >= 0 && QualitySettings.GetRenderPipelineAssetAt(low) is UniversalRenderPipelineAsset mobile)
                mobile.renderScale = QualityTierValues.WebRenderScale;
        };
#endif

        /// <summary>The choice a stored value names; anything else (no file, a value the game does not know) is Auto.</summary>
        public static Choice Parse(string? stored) => stored == "high" ? Choice.High : stored == "low" ? Choice.Low : Choice.Auto;

        public static string Format(Choice c) => c == Choice.High ? "high" : c == Choice.Low ? "low" : "auto";

        /// <summary>The choice to apply at start: the stored one, except under an instrument that must see the tier's own
        /// look, which wins over the file as it does over the fallback: on the web ?vprofile, ?profile, ?shot or
        /// ?nofallback; on the desktop -vprofile, -profile or -shot (FrameProfile, PixelErrorProfile, VisualShots).</summary>
        public static Choice AtStart(string? stored, string? url, string[] args, bool webgl) =>
            (webgl ? !QualityFallback.Wanted(url, webgl: true) : args.Any(a => a == "-vprofile" || a == "-profile" || a == "-shot"))
                ? Choice.Auto : Parse(stored);

        /// <summary>Switches to <paramref name="c"/> at once: the quality level (and with it the URP asset) and the active
        /// asset's render scale. A tier missing from this build keeps the current one (a warning, never a failed start).</summary>
        public static void Apply(Choice c)
        {
            if (bootLevel < 0) bootLevel = QualitySettings.GetQualityLevel();
            int level = c == Choice.Auto ? bootLevel : Array.IndexOf(QualitySettings.names, c == Choice.High ? HighTier : LowTier);
            if (level < 0) { Debug.LogWarning($"Settings: no {c} tier in this build; the quality stays as it is"); return; }
            Current = c;
            if (QualitySettings.GetQualityLevel() != level) QualitySettings.SetQualityLevel(level, true);
            var asset = UniversalRenderPipeline.asset;
            // Auto puts the tier's own scale back (QualityTiers.Apply: Mobile at WebRenderScale, PC at 1.0).
            if (asset != null)
                asset.renderScale = c == Choice.High ? HighRenderScale : c == Choice.Low ? LowRenderScale
                    : QualitySettings.names[level] == LowTier ? QualityTierValues.WebRenderScale : 1f;
            TiltShift.FallbackOff = false;   // a fixed choice ends the fallback's last step; Auto starts it again from the top
            Changed?.Invoke();
        }
    }
}
