#nullable enable
using UnityEngine;

/// <summary>Edit-mode render sampling for the visual tests: renders a camera into a texture and reads the pixels back.
/// Row 0 is the bottom of the image (viewport y = 0). Luma is Rec. 709 over the stored (display-referred) values.</summary>
public sealed class RenderSample
{
    public readonly int Width, Height;
    public readonly Color[] Pixels;

    RenderSample(int width, int height, Color[] pixels) { Width = width; Height = height; Pixels = pixels; }

    public static RenderSample Render(Camera cam, int width, int height) => Render(cam, width, height, System.Array.Empty<Vector3>(), System.Array.Empty<Vector2>());

    /// <summary>Renders, and projects each world point to this image's pixels while the camera still targets it.</summary>
    public static RenderSample Render(Camera cam, int width, int height, Vector3[] world, Vector2[] screenOut)
    {
        var rt = new RenderTexture(width, height, 24);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        var oldTarget = cam.targetTexture;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            for (int i = 0; i < world.Length; i++) { var p = cam.WorldToScreenPoint(world[i]); screenOut[i] = new Vector2(p.x, p.y); }
            return new RenderSample(width, height, tex.GetPixels());
        }
        finally
        {
            RenderTexture.active = null; cam.targetTexture = oldTarget;
            rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }

    /// <summary>The per-pixel, per-channel median of <paramref name="renders"/> (odd) renders of the same frame, for tests that
    /// compare two frames pixel for pixel at a zero threshold. With every input pinned (the water's clock too), about 1 in
    /// 100 editor renders of the map still differs from the one before it by 1-11/255 on up to about 50 of 129,600 pixels,
    /// all in the terrain's pass, and the next render matches again (issue #440: no CPU-side state changes; URP's own
    /// Terrain/Lit material on the same terrain never does it). A transient pixel loses the vote, so the comparison can
    /// stay exact: a change that is really in the frame is in every render and still counts.</summary>
    public static RenderSample RenderSteady(Camera cam, int width, int height, int renders = 5)
    {
        var all = new RenderSample[renders];
        for (int k = 0; k < renders; k++) all[k] = Render(cam, width, height);
        var pixels = new Color[all[0].Pixels.Length];
        var v = new float[renders];
        float Median(int i, int ch) { for (int k = 0; k < renders; k++) v[k] = all[k].Pixels[i][ch]; System.Array.Sort(v); return v[renders / 2]; }
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(Median(i, 0), Median(i, 1), Median(i, 2), 1f);
        return new RenderSample(width, height, pixels);
    }

    public Color At(int x, int y) => Pixels[Mathf.Clamp(y, 0, Height - 1) * Width + Mathf.Clamp(x, 0, Width - 1)];

    /// <summary>The mean colour of the (2 radius + 1)² patch around (cx, cy).</summary>
    public Color Mean(int cx, int cy, int radius)
    {
        Color sum = Color.clear; int n = 0;
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++) { sum += At(x, y); n++; }
        return sum / n;
    }

    /// <summary>Track A's tilt-shift blurs the top and bottom bands of every render; width and colour renders must not see
    /// it (task-3 carry, merge note m6). A no-op while the switch does not exist.</summary>
    public static void TiltShift(bool on)
    {
        if (System.Linq.Enumerable.Contains(Statecraft.Game.Map.VisualToggles.Names, "tiltshift")) Statecraft.Game.Map.VisualToggles.Set("tiltshift", on);
    }

    public static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    /// <summary>Mean |dL/dx| + |dL/dy| over the rows from y0 to y1 (fractions of the height, 0 = bottom): local contrast,
    /// which a blur lowers.</summary>
    public float GradientEnergy(float y0, float y1)
    {
        int r0 = Mathf.Clamp(Mathf.FloorToInt(y0 * Height), 0, Height - 2), r1 = Mathf.Clamp(Mathf.CeilToInt(y1 * Height), r0 + 1, Height - 1);
        double sum = 0; int n = 0;
        for (int y = r0; y < r1; y++)
            for (int x = 0; x < Width - 1; x++)
            {
                float l = Luma(At(x, y));
                sum += Mathf.Abs(Luma(At(x + 1, y)) - l) + Mathf.Abs(Luma(At(x, y + 1)) - l); n++;
            }
        return (float)(sum / n);
    }

    /// <summary>Mean luma over a rectangle given as fractions of the image (0 = left / bottom).</summary>
    public float MeanLuma(float x0, float y0, float x1, float y1)
    {
        double sum = 0; int n = 0;
        for (int y = Mathf.FloorToInt(y0 * Height); y < Mathf.CeilToInt(y1 * Height); y++)
            for (int x = Mathf.FloorToInt(x0 * Width); x < Mathf.CeilToInt(x1 * Width); x++) { sum += Luma(At(x, y)); n++; }
        return (float)(sum / n);
    }

    public float MeanSaturation()
    {
        double sum = 0;
        foreach (var c in Pixels) { Color.RGBToHSV(c, out _, out var s, out _); sum += s; }
        return (float)(sum / Pixels.Length);
    }

    public float LumaStd()
    {
        double sum = 0, sq = 0;
        foreach (var c in Pixels) { double l = Luma(c); sum += l; sq += l * l; }
        double mean = sum / Pixels.Length;
        return (float)System.Math.Sqrt(System.Math.Max(0, sq / Pixels.Length - mean * mean));
    }

    /// <summary>Share of pixels whose luma in <paramref name="a"/> is lower than in <paramref name="b"/> by more than <paramref name="by"/>.</summary>
    public static float FractionDarker(RenderSample a, RenderSample b, float by)
    {
        int n = 0;
        for (int i = 0; i < a.Pixels.Length; i++) if (Luma(a.Pixels[i]) < Luma(b.Pixels[i]) - by) n++;
        return (float)n / a.Pixels.Length;
    }

    /// <summary>Share of pixels with any channel at or above <paramref name="level"/> (clipping: 0.995 is the top 8-bit step).</summary>
    public float FractionClipped(float level)
    {
        int n = 0;
        foreach (var c in Pixels) if (c.r >= level || c.g >= level || c.b >= level) n++;
        return (float)n / Pixels.Length;
    }

    static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
    static float ToSrgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;

    /// <summary>CIELAB (D65) of a display (sRGB) colour.</summary>
    public static Vector3 Lab(Color srgb)
    {
        float r = ToLinear(srgb.r), g = ToLinear(srgb.g), b = ToLinear(srgb.b);
        float x = (0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f;
        float y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
        float z = (0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f;
        float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
        float fx = F(x), fy = F(y), fz = F(z);
        return new Vector3(116f * fy - 16f, 500f * (fx - fy), 200f * (fy - fz));
    }

    /// <summary>CIE76 colour difference: about 2.3 is just noticeable; 20 or more reads as a different colour at a glance.</summary>
    public static float DeltaE(Color a, Color b) => Vector3.Distance(Lab(a), Lab(b));

    public enum Cvd { Deuteranopia, Protanopia }

    // Machado, Oliveira and Fernandes (2009), severity 1.0, applied to linear RGB.
    static readonly float[,] Deutan = { { 0.367322f, 0.860646f, -0.227968f }, { 0.280085f, 0.672501f, 0.047413f }, { -0.011820f, 0.042940f, 0.968881f } };
    static readonly float[,] Protan = { { 0.152286f, 1.052583f, -0.204868f }, { 0.114503f, 0.786281f, 0.099216f }, { -0.003882f, -0.048116f, 1.051998f } };

    /// <summary>How a display colour looks to a player with full deuteranopia or protanopia.</summary>
    public static Color SimulateCvd(Color srgb, Cvd kind)
    {
        var m = kind == Cvd.Deuteranopia ? Deutan : Protan;
        float r = ToLinear(srgb.r), g = ToLinear(srgb.g), b = ToLinear(srgb.b);
        float Row(int i) => Mathf.Clamp01(m[i, 0] * r + m[i, 1] * g + m[i, 2] * b);
        return new Color(ToSrgb(Row(0)), ToSrgb(Row(1)), ToSrgb(Row(2)), srgb.a);
    }
}
