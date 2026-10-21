using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, a two-band tilt-shift (TiltShiftFeature) driven from Apply.
    /// Yaw turns the view around the look-at point (0 = looking north); CameraController drives LookAt, Distance and Yaw.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float Pitch = 50f, Fov = 35f;
        // The opening zoom: under the ~106 km where a 16:9 view grows wider than the map and CameraController centres it
        // (issue #1062: at 120 km the map could not be panned left or right).
        public Vector3 LookAt; public float Distance = 90000f; public float Yaw;
        /// <summary>Share of the screen height (0..1) the UI covers at the top. The view is shifted so LookAt sits in the
        /// middle of the map area below it (an off-centre frustum: picking rays and WorldToScreenPoint follow it).</summary>
        public float TopInset;
        /// <summary>The largest inset the projection applies (TopInset is clamped to it); at this pitch and FOV the top of
        /// the view still looks down, so the sky never shows (MapSceneTests.TheCameraClearsToTheSeaColour).</summary>
        public const float MaxTopInset = 0.5f;

        /// <summary>Undo actions for state Init changes outside the rig (the URP asset's shadow distance...). MapScene.Build hands
        /// them to the map's MapRuntimeAssets right after Init, so destroying the map undoes them.</summary>
        public readonly List<Action> TeardownActions = new List<Action>();
        bool initialised;   // only a rig the map set up fits the shadow distance (bare rigs in tests must not touch the URP asset)

        /// <summary>The global post-processing volume Init creates. Its runtime VolumeProfile must be owned by the caller
        /// (MapScene registers it with MapRuntimeAssets).</summary>
        public Volume Volume { get; private set; }

        /// <summary>Whether Init attaches the runtime quality fallback (QualityFallback.Wanted: the WebGL tier, no instrument
        /// running). A seam: the integration test forces the web tier's answer in the editor.</summary>
        internal static Func<bool> FallbackWanted = QualityFallback.Wanted;

        public void Init()
        {
            // Undo actions an earlier Init left behind (Init called outside MapScene.Build) run now instead of piling up.
            for (int i = TeardownActions.Count - 1; i >= 0; i--) TeardownActions[i]();
            TeardownActions.Clear();
            var cam = GetComponent<Camera>(); cam.fieldOfView = Fov; cam.farClipPlane = 1_000_000f; cam.nearClipPlane = 50f;
            // Beyond the terrain the view shows open sea, not a grey void (playtest 1, item 9). At this pitch the top of the
            // view still looks down, so the sky never shows and a solid clear in the sea biome colour is the whole fix.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MapBuilder.BiomeColours["sea"];
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var vol = new GameObject("PostFX").AddComponent<Volume>(); vol.isGlobal = true;
            vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Volume = vol;
            MapGrade.Apply(vol.profile);
            var sun = MapLighting.FindSun();
            TeardownActions.Add(MapLighting.Capture(sun));   // first, so it runs last (review M1)
            MapLighting.Apply(sun);
            TeardownActions.Add(VisualToggles.Register("shadows", on => { var s = MapLighting.FindSun(); if (s != null) s.shadows = on ? LightShadows.Soft : LightShadows.None; }));
            TeardownActions.Add(ShadowFit.Restore);
            TeardownActions.Add(TiltShift.Reset);
            TeardownActions.Add(VisualToggles.Register("tiltshift", on => TiltShift.Enabled = on));
            TeardownActions.Add(VisualToggles.Register("grade", on => MapGrade.SetActive(vol.profile, on)));
            if (TiltShift.OffRequested()) TiltShift.Enabled = false;
            // The runtime quality fallback (WebGL only); its undo removes the driver and restores the tier's render scale.
            TeardownActions.Add(QualityFallbackDriver.Attach(gameObject, FallbackWanted()));
            // A quality switch in Settings (#40) changes the URP asset: refit its shadow distance now, not at the next move.
            GraphicsQuality.Changed += Apply;
            TeardownActions.Add(() => GraphicsQuality.Changed -= Apply);
            initialised = true;
            Apply();
        }

        public void Apply()
        {
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.position = LookAt - transform.forward * Distance;
            TiltShift.Apply(TopInset, Distance);
            ApplyProjection();
            if (initialised) ShadowFit.Apply(GetComponent<Camera>());   // after the projection: the fit reads the corner rays
        }

        /// <summary>Sets the camera's projection for <see cref="TopInset"/> only (the transform is left alone). Apply calls it
        /// every frame, so a new window aspect is picked up too.</summary>
        public void ApplyProjection()
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;
            cam.ResetProjectionMatrix();   // follows the aspect and FOV again
            if (TopInset <= 0f) return;
            // NDC y = (m11 y + m12 z) / -z, so m12 = s moves every point down by s: the centre of the area below the bar
            // (NDC y from -1 to 1 - 2 TopInset) is at -TopInset.
            var p = cam.projectionMatrix; p[1, 2] = Mathf.Clamp(TopInset, 0f, MaxTopInset); cam.projectionMatrix = p;
        }
    }
}
