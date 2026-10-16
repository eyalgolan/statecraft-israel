#nullable enable
using System;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The dune coasts (#388, #690): the sand coasts, Jaffa south and Haifa Bay, where the beach and the surf are
    /// wide; under the kurkar north of Tel Aviv both stay narrow. One definition, Resources/Map/DuneCoasts.json, read here
    /// (Resources.Load, so the web build reads it at once) and by the tools; MapShaderGlobals hands it to the water pass as
    /// map-metre boxes. Each band is a lon/lat box whose weight ramps from 0 to 1 over its ramp (degrees) inside each edge,
    /// with a smoothstep on each axis; the weight at a point is the largest band's.</summary>
    public static class DuneCoasts
    {
        public const string Resource = "Map/DuneCoasts";
        public const string AssetPath = "Assets/Resources/Map/DuneCoasts.json";
        /// <summary>The water pass's band count (StatecraftMap.hlsl's _ScDuneRect / _ScDuneRamp).</summary>
        public const int MaxBands = 4;

        [Serializable] public sealed class Band { public string name = ""; public double lon0, lon1, lat0, lat1, ramp; }
        [Serializable] sealed class FileJson { public Band[] bands = Array.Empty<Band>(); }

        static Band[]? bands;

        /// <summary>The bands as the file gives them; none (and an error logged) when the file is missing or unreadable.</summary>
        public static Band[] Bands => bands ??= Parse(Resources.Load<TextAsset>(Resource)?.text);

        public static Band[] Parse(string? json)
        {
            if (string.IsNullOrEmpty(json)) { Debug.LogError($"DuneCoasts: {AssetPath} is missing"); return Array.Empty<Band>(); }
            try { return JsonUtility.FromJson<FileJson>(json)?.bands ?? Array.Empty<Band>(); }
            catch (ArgumentException e) { Debug.LogError($"DuneCoasts: {AssetPath} unreadable: {e.Message}"); return Array.Empty<Band>(); }
        }

        static double Smooth(double t) { t = t < 0 ? 0 : t > 1 ? 1 : t; return t * t * (3 - 2 * t); }

        /// <summary>0 off the dune coasts, 1 inside a band, ramped at its edges.</summary>
        public static float WeightAt(double lon, double lat) => (float)Weight(lon, lat);

        /// <summary><see cref="WeightAt"/> in double precision, as tools/terrain_bake.py computes it (BeachBands.ExtraAt).</summary>
        public static double Weight(double lon, double lat)
        {
            double w = 0;
            foreach (var b in Bands)
                w = Math.Max(w, Smooth((lon - b.lon0) / b.ramp) * Smooth((b.lon1 - lon) / b.ramp) * Smooth((lat - b.lat0) / b.ramp) * Smooth((b.lat1 - lat) / b.ramp));
            return w;
        }

        /// <summary>The bands in map metres from the south-west corner, as the water pass reads them: per band (x0, x1, z0, z1)
        /// and its ramp (east-west, north-south metres), and the count. A band past <see cref="MaxBands"/> is dropped with an
        /// error, not silently.</summary>
        public static int ToMapMetres(GeoFrame frame, Vector4[] rects, Vector4[] ramps)
        {
            var all = Bands;
            if (all.Length > MaxBands) Debug.LogError($"DuneCoasts: {all.Length} bands, the water pass reads {MaxBands}");
            int n = Math.Min(all.Length, MaxBands);
            double mx = frame.WidthM / (frame.Lon1 - frame.Lon0), mz = frame.LengthM / (frame.Lat1 - frame.Lat0);
            for (int i = 0; i < MaxBands; i++)
            {
                if (i >= n) { rects[i] = Vector4.zero; ramps[i] = Vector4.zero; continue; }
                var b = all[i];
                Vector3 a = frame.ToWorld(b.lon0, b.lat0, 0f), c = frame.ToWorld(b.lon1, b.lat1, 0f);
                rects[i] = new Vector4(a.x, c.x, a.z, c.z);
                ramps[i] = new Vector4((float)(b.ramp * mx), (float)(b.ramp * mz), 0f, 0f);
            }
            return n;
        }
    }
}
