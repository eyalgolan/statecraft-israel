#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>The hex-tile map's time of day. Day is today's warm look (HexLook); dusk and night are presets laid over it.</summary>
    public enum HexTime { Day, Dusk, Night }

    /// <summary>Time-of-day presets for the opt-in hex-tile map, next to HexLook: presentation only, no simulation state is
    /// read or written. Web ?hex-time=dusk|night (a top-level query parameter, with ?hex-tiles), desktop -hex-time dusk|night;
    /// the Hex Painter has a switch. Dusk is a low, warm-orange sun with long shadows under a cooler sky; night is a dim,
    /// blue-white moon, a dark-blue sky and a lower exposure, still bright enough to read the tiles and units, with lit
    /// windows in city and town tiles. Apply returns the undo, which restores everything it changed.</summary>
    public static class HexTimeOfDay
    {
        public static readonly string[] Ids = { "day", "dusk", "night" };
        /// <summary>The child of the board root that holds the night's lit windows (one mesh, one unlit material).</summary>
        public const string WindowsName = "HexNightWindows";
        static readonly Regex Flag = new Regex(@"(?:^|&)hex-time=(?<time>[a-z]+)(?=&|$)", RegexOptions.CultureInvariant);

        public static string Id(HexTime time) => Ids[(int)time];

        /// <summary>A preset by its id (day, dusk, night).</summary>
        public static bool TryParse(string? id, out HexTime time)
        {
            int i = Array.IndexOf(Ids, id ?? "");
            time = i < 0 ? HexTime.Day : (HexTime)i;
            return i >= 0;
        }

        public static HexTime Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>The preset asked for, day when the flag is absent or names no preset. Web: only a top-level query
        /// parameter counts (HexTilesLook's rule: a value after '#' does not). Desktop: the argument after -hex-time.</summary>
        public static HexTime Requested(string? url, string[] args, bool webgl)
        {
            if (!webgl)
            {
                int i = Array.IndexOf(args, "-hex-time");
                return i >= 0 && i + 1 < args.Length && TryParse(args[i + 1], out var t) ? t : HexTime.Day;
            }
            var address = (url ?? "").Split('#')[0];
            var start = address.IndexOf('?');
            if (start < 0) return HexTime.Day;
            var m = Flag.Match(address.Substring(start + 1));
            return m.Success && TryParse(m.Groups["time"].Value, out var time) ? time : HexTime.Day;
        }

        /// <summary>One preset's light and grade. The sun is set outright (MapLighting's convention: elevation above the
        /// horizon, azimuth clockwise from north); the grade replaces HexLook's exposure, saturation and white balance.</summary>
        public sealed class Preset
        {
            public float SunElevation, SunAzimuth, SunIntensity, ShadowStrength;
            public Color SunColour, AmbientSky, AmbientEquator, AmbientGround;
            public float PostExposure, Saturation, Temperature, Tint;
            /// <summary>Lit windows in city and town tiles.</summary>
            public bool Windows;
        }

        /// <summary>Dusk: the sun low in the west-north-west (where the map's sun already stands, so the shadows only
        /// lengthen), deep orange, under a cooler, dimmer sky. The grade stays warm but a little darker.</summary>
        public static readonly Preset Dusk = new Preset
        {
            SunElevation = 14f, SunAzimuth = 290f, SunIntensity = 2.0f, ShadowStrength = 0.75f,
            SunColour = new Color(1.00f, 0.70f, 0.47f),
            AmbientSky = new Color(0.48f, 0.52f, 0.72f), AmbientEquator = new Color(0.52f, 0.47f, 0.50f), AmbientGround = new Color(0.24f, 0.21f, 0.20f),
            PostExposure = 0.30f, Saturation = 22f, Temperature = 6f, Tint = 3f,
        };

        /// <summary>Night: a blue-white moon high in the south-west, dim, with soft shadows; a dark-blue sky fill strong
        /// enough that the shadow side still reads; a lower exposure and a cool white balance. Not a horror game: the
        /// board is a strategy board at night, so the tiles keep their colours, only darker and bluer.</summary>
        public static readonly Preset Night = new Preset
        {
            SunElevation = 48f, SunAzimuth = 215f, SunIntensity = 0.85f, ShadowStrength = 0.55f,
            SunColour = new Color(0.70f, 0.80f, 1.00f),
            AmbientSky = new Color(0.22f, 0.27f, 0.42f), AmbientEquator = new Color(0.17f, 0.20f, 0.28f), AmbientGround = new Color(0.08f, 0.09f, 0.12f),
            PostExposure = 0.10f, Saturation = -4f, Temperature = -10f, Tint = 0f,
            Windows = true,
        };

        /// <summary>The preset for <paramref name="time"/>; null for day, which is HexLook as it is.</summary>
        public static Preset? PresetOf(HexTime time) => time switch { HexTime.Dusk => Dusk, HexTime.Night => Night, _ => null };

        /// <summary>Lays <paramref name="time"/> over the day look already applied (HexLook.Apply): the grade in
        /// <paramref name="profile"/>, the <paramref name="sun"/>, the ambient light and, for night, the board under
        /// <paramref name="boardRoot"/> (null: none). Day changes nothing. Returns the undo. The lit windows need the board:
        /// a board built for the Hex Painter carries it (HexBoardState); otherwise pass it to the overload.</summary>
        public static Action Apply(HexTime time, VolumeProfile? profile, Light? sun, GameObject? boardRoot)
        {
            // Not "?.": in the editor a missing component is a fake null that "?." does not see.
            var state = boardRoot != null ? boardRoot.GetComponent<HexBoardState>() : null;
            return Apply(time, profile, sun, boardRoot, state != null ? state.Board : null);
        }

        /// <summary>Apply, with the <paramref name="board"/> built under <paramref name="boardRoot"/> (board space is the
        /// root's local space), for the night's lit windows; <paramref name="only"/> lights just that prototype's tiles
        /// (a capture that built one tile). The undo restores each value and override state it changed, removes the
        /// volume components it added (never HexLook's) and destroys the windows, their mesh and their material.</summary>
        public static Action Apply(HexTime time, VolumeProfile? profile, Light? sun, GameObject? boardRoot, HexBoard? board, string? only = null)
        {
            var preset = PresetOf(time);
            if (preset == null) return () => { };
            var undo = new List<Action>();
            if (profile != null)
            {
                bool addedCa = !profile.TryGet(out ColorAdjustments ca);
                if (addedCa) ca = profile.Add<ColorAdjustments>();
                var c0 = (ca.postExposure.value, ca.postExposure.overrideState, ca.saturation.value, ca.saturation.overrideState);
                ca.postExposure.Override(preset.PostExposure); ca.saturation.Override(preset.Saturation);
                undo.Add(() =>
                {
                    if (addedCa) { profile.Remove<ColorAdjustments>(); return; }
                    ca.postExposure.value = c0.Item1; ca.postExposure.overrideState = c0.Item2; ca.saturation.value = c0.Item3; ca.saturation.overrideState = c0.Item4;
                });
                bool addedWb = !profile.TryGet(out WhiteBalance wb);
                if (addedWb) wb = profile.Add<WhiteBalance>();
                var w0 = (wb.temperature.value, wb.temperature.overrideState, wb.tint.value, wb.tint.overrideState);
                wb.temperature.Override(preset.Temperature); wb.tint.Override(preset.Tint);
                undo.Add(() =>
                {
                    if (addedWb) { profile.Remove<WhiteBalance>(); return; }
                    wb.temperature.value = w0.Item1; wb.temperature.overrideState = w0.Item2; wb.tint.value = w0.Item3; wb.tint.overrideState = w0.Item4;
                });
            }
            if (sun != null)
            {
                var s0 = (sun.transform.localRotation, sun.intensity, sun.color, sun.shadows, sun.shadowStrength);
                // A light shines along its forward axis: towards the azimuth opposite the sun, tilted down by the elevation.
                sun.transform.rotation = Quaternion.Euler(preset.SunElevation, preset.SunAzimuth - 180f, 0f);
                sun.intensity = preset.SunIntensity; sun.color = preset.SunColour;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = preset.ShadowStrength;
                undo.Add(() =>
                {
                    if (sun == null) return;
                    sun.transform.localRotation = s0.Item1; sun.intensity = s0.Item2; sun.color = s0.Item3; sun.shadows = s0.Item4; sun.shadowStrength = s0.Item5;
                });
            }
            var a0 = (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor, RenderSettings.ambientProbe);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = preset.AmbientSky; RenderSettings.ambientEquatorColor = preset.AmbientEquator; RenderSettings.ambientGroundColor = preset.AmbientGround;
            undo.Add(() =>
            {
                RenderSettings.ambientMode = a0.Item1; RenderSettings.ambientSkyColor = a0.Item2;
                RenderSettings.ambientEquatorColor = a0.Item3; RenderSettings.ambientGroundColor = a0.Item4;
                // Last: a Trilight scene regenerates its probe from the colours anyway; any other mode gets its own back.
                RenderSettings.ambientProbe = a0.Item5;
            });
            if (preset.Windows && boardRoot != null && board != null)
            {
                var windows = LitWindows(board, only);
                if (windows.Count > 0) undo.Add(Emit(boardRoot.transform, windows));
            }
            bool done = false;
            return () => { if (done) return; done = true; for (int i = undo.Count - 1; i >= 0; i--) undo[i](); };
        }

        // --- the night's lit windows ---

        /// <summary>The prototype kinds whose buildings get lit windows at night.</summary>
        public static readonly string[] LitKinds = { "city", "town" };
        public static readonly Color WindowColour = new Color(1.00f, 0.80f, 0.46f);
        /// <summary>A window's width and height and the gap off the wall, as shares of the building's footprint side; the
        /// share of windows lit.</summary>
        const float WindowW = 0.13f, WindowH = 0.085f, OffWall = 0.012f, LitShare = 0.55f;

        /// <summary>One window: its centre, its outward normal and its in-wall right axis, in board space, and its size (m).</summary>
        public readonly struct Window
        {
            public readonly Vector3 Centre, Normal, Right; public readonly float Width, Height;
            public Window(Vector3 centre, Vector3 normal, Vector3 right, float width, float height) { Centre = centre; Normal = normal; Right = right; Width = width; Height = height; }
        }

        /// <summary>The lit windows of the board's city and town tiles (deterministic: the tiles' own placements at the
        /// build's seed, and a hash of each window's place for which are lit). A building is a piece placed by its width
        /// as a box (its model's footprint and height, or the box stand-in's); its four walls get a grid of windows, the
        /// rows by its height.</summary>
        public static List<Window> LitWindows(HexBoard board, string? only = null, int seed = 1948)
        {
            var list = new List<Window>();
            var surface = new HexBoardSurface(board);
            var shapes = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            foreach (var h in board.Hexes.Values.OrderBy(h => h.q).ThenBy(h => h.r))
            {
                if (h.prototype == "" || (only != null && h.prototype != only) || !HexPrototypes.All.ContainsKey(h.prototype)) continue;
                if (Array.IndexOf(LitKinds, HexPrototypes.Info(h.prototype).Kind) < 0) continue;
                var ctx = new HexTileContext(h.q, h.r, surface, seed);
                HexPrototypes.Create(h.prototype).Build(ctx);
                foreach (var p in ctx.Placements)
                {
                    if (p.StandIn != StandIn.Box || p.Fit != Fit.Width) continue;
                    var id = HexModels.Resolve(p.Model, p.Position);
                    if (!shapes.TryGetValue(id, out var shape)) shapes[id] = shape = Shape(id);
                    AddWalls(list, p.Matrix, shape);
                }
            }
            return list;
        }

        /// <summary>A building's size in its placement's unit frame (the larger footprint side is 1).</summary>
        static Vector3 Shape(string id)
        {
            var prefab = HexModels.Prefab(id);
            if (prefab == null || HexModels.Skinned(prefab)) return new Vector3(1f, 0.65f, 1f);   // the box stand-in
            var go = Object.Instantiate(prefab);
            try
            {
                var b = HexModels.Bounds(go).size;
                float side = Mathf.Max(b.x, b.z, 1e-4f);
                return new Vector3(b.x / side, b.y / side, b.z / side);
            }
            finally { Object.DestroyImmediate(go); }
        }

        static void AddWalls(List<Window> list, Matrix4x4 m, Vector3 shape)
        {
            float side = m.lossyScale.x;
            int rows = Mathf.Clamp(Mathf.RoundToInt(shape.y / 0.2f), 1, 8);
            for (int wall = 0; wall < 4; wall++)
            {
                bool alongX = wall % 2 == 0;   // walls 0 and 2 face +z and -z and run along x
                float sign = wall < 2 ? 1f : -1f;
                var normal = alongX ? new Vector3(0f, 0f, sign) : new Vector3(sign, 0f, 0f);
                var right = alongX ? Vector3.right : Vector3.forward;
                float half = (alongX ? shape.z : shape.x) * 0.5f, length = alongX ? shape.x : shape.z;
                int cols = Mathf.Clamp(Mathf.RoundToInt(length / 0.28f), 1, 4);
                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < cols; col++)
                    {
                        var local = normal * (half + OffWall) + right * ((col + 0.5f) / cols - 0.5f) * length * 0.8f
                            + Vector3.up * shape.y * (0.18f + 0.62f * (row + 0.5f) / rows);
                        var centre = m.MultiplyPoint3x4(local);
                        uint hash = unchecked((uint)(Mathf.FloorToInt(centre.x) * 73856093 ^ Mathf.FloorToInt(centre.y) * 83492791 ^ Mathf.FloorToInt(centre.z) * 19349663));
                        if ((hash % 1000u) / 1000f >= LitShare) continue;
                        list.Add(new Window(centre, m.MultiplyVector(normal).normalized, m.MultiplyVector(right).normalized,
                            WindowW * side, WindowH * side * Mathf.Min(1f, shape.y * 2f)));
                    }
            }
        }

        /// <summary>The windows as one mesh of quads with one unlit, warm material under <paramref name="root"/>; returns
        /// the action that destroys all three.</summary>
        static Action Emit(Transform root, List<Window> windows)
        {
            var vertices = new List<Vector3>(windows.Count * 4); var triangles = new List<int>(windows.Count * 6);
            foreach (var w in windows)
            {
                int i = vertices.Count;
                var r = w.Right * (w.Width * 0.5f); var u = Vector3.up * (w.Height * 0.5f);
                vertices.Add(w.Centre - r - u); vertices.Add(w.Centre - r + u); vertices.Add(w.Centre + r + u); vertices.Add(w.Centre + r - u);
                // Wound to face along the window's normal.
                bool front = Vector3.Dot(Vector3.Cross(2f * u, 2f * r), w.Normal) > 0f;
                if (front) triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                else triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            var mesh = new Mesh { name = WindowsName, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            // The shipped unlit template (HexMaterials: Shader.Find returns null in players), opaque warm light.
            var material = HexMaterials.New(HexMaterials.Edges);
            material.name = WindowsName;
            material.SetColor("_BaseColor", WindowColour);
            var go = new GameObject(WindowsName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return () => { Destroy(go); Destroy(mesh); Destroy(material); };
        }

        static void Destroy(Object? o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }
    }
}
