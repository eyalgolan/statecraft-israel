#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Map
{
    /// <summary>The map's light (research notes §1 "Lighting", promo 03): a low, warm late-afternoon sun from the
    /// west-north-west, whose long shadows fall to the right of the default view, and a cool sky fill that keeps the shadow
    /// side readable. The sun is fixed in the world, so orbiting the camera moves the light across the screen.
    ///
    /// Issue #2427 (docs/visual-v1.md, "Issue #2427"): more of the land's light comes from the sun (1.8, was 1.6) and a tenth
    /// less from the fill (was 0.52, 0.60, 0.72 / 0.48, 0.46, 0.42 / 0.26, 0.22, 0.17), a little warmer in the sun (was 1.00,
    /// 0.84, 0.64). The relief reads deeper, and the sea, which reads the sun but not the ambient (ScWaterShade), holds its luma
    /// at the owner's exposure. A deeper cut of the fill (a quarter) took TerrainShaderTests' relief ratio from 1.11 to 1.06,
    /// under its 1.08. The elevation stays 30: a 27-degree trial changed the picture little.</summary>
    public static class MapLighting
    {
        public const float SunElevation = 30f;   // degrees above the horizon
        public const float SunAzimuth = 285f;    // degrees clockwise from north: west-north-west
        public const float SunIntensity = 1.8f;
        public static readonly Color SunColour = new Color(1.00f, 0.83f, 0.61f);
        public static readonly Color AmbientSky = new Color(0.47f, 0.54f, 0.65f);
        public static readonly Color AmbientEquator = new Color(0.43f, 0.415f, 0.38f);
        public static readonly Color AmbientGround = new Color(0.235f, 0.2f, 0.155f);

        /// <summary>Records the sun's transform and light settings and the active scene's ambient, and returns the action that
        /// puts them back (review M1: CameraRig.Init registers it as its first teardown action, so destroying the map leaves
        /// the scene's lighting as it was and saving an open Map.unity does not bake the runtime values). The ambient is put
        /// back only while the same scene is still active, so a map destroyed by loading another scene never writes into it.</summary>
        public static Action Capture(Light? sun)
        {
            var scene = SceneManager.GetActiveScene();
            var mode = RenderSettings.ambientMode; var sky = RenderSettings.ambientSkyColor;
            var equator = RenderSettings.ambientEquatorColor; var ground = RenderSettings.ambientGroundColor;
            var probe = RenderSettings.ambientProbe; var renderSun = RenderSettings.sun;
            bool hadSun = sun != null;
            Quaternion rot = default; Color colour = default; float intensity = 0f, strength = 0f; LightShadows shadows = default;
            if (sun != null) { rot = sun.transform.rotation; colour = sun.color; intensity = sun.intensity; strength = sun.shadowStrength; shadows = sun.shadows; }
            return () =>
            {
                if (hadSun && sun != null)
                {
                    sun.transform.rotation = rot; sun.color = colour; sun.intensity = intensity;
                    sun.shadowStrength = strength; sun.shadows = shadows;
                }
                if (SceneManager.GetActiveScene() != scene) return;
                RenderSettings.ambientMode = mode; RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
                RenderSettings.ambientProbe = probe; RenderSettings.sun = renderSun;
            };
        }

        /// <summary>The scene's first directional light, or null.</summary>
        public static Light? FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
                if (l.type == LightType.Directional) return l;
            return null;
        }

        /// <summary>Points, colours and enables shadows on <paramref name="sun"/> (when there is one) and sets the trilight
        /// ambient. Unity converts the three (sRGB) colours to the linear SH probe the shaders read, at once, in the editor
        /// and in the WebGL player; a probe assigned in Trilight mode is replaced by that conversion (Task 4, review M4), so
        /// the colours are the only input.</summary>
        public static void Apply(Light? sun)
        {
            if (sun != null)
            {
                // A light shines along its forward axis: towards the azimuth opposite the sun, tilted down by the elevation.
                sun.transform.rotation = Quaternion.Euler(SunElevation, SunAzimuth - 180f, 0f);
                sun.color = SunColour; sun.intensity = SunIntensity;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky; RenderSettings.ambientEquatorColor = AmbientEquator; RenderSettings.ambientGroundColor = AmbientGround;
        }
    }
}
