#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>RenderSample.RenderSteady's median (issue #440): the fast vote returns, bit for bit, what sorting every channel of
/// every pixel returned (the reference below is the code it replaced), on real frames and on hard synthetic values.</summary>
public class RenderSteadyTests
{
    [TearDown]
    public void Reset()
    {
        MapShaderGlobals.PinWaterTime(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    /// <summary>RenderSteady's median before the fast path, verbatim: Array.Sort over each channel of each pixel.</summary>
    static Color[] ReferenceMedian(Color[][] frames)
    {
        int renders = frames.Length;
        var pixels = new Color[frames[0].Length];
        var v = new float[renders];
        float Median(int i, int ch) { for (int k = 0; k < renders; k++) v[k] = frames[k][i][ch]; System.Array.Sort(v); return v[renders / 2]; }
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(Median(i, 0), Median(i, 1), Median(i, 2), 1f);
        return pixels;
    }

    static readonly int[] Pow5 = { 1, 5, 25, 125, 625 };

    static Color[][] First(Color[][] frames, int n) => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(frames, n));

    static void AssertBitEqual(Color[] expected, Color[] actual, string what)
    {
        Assert.AreEqual(expected.Length, actual.Length, what);
        for (int i = 0; i < expected.Length; i++)
            for (int ch = 0; ch < 4; ch++)
                if (System.BitConverter.SingleToInt32Bits(expected[i][ch]) != System.BitConverter.SingleToInt32Bits(actual[i][ch]))
                    Assert.Fail($"{what}: pixel {i} channel {ch}: {expected[i][ch]:R} expected, {actual[i][ch]:R} returned");
    }

    /// <summary>Pixels where the frames do not all agree on every channel: what makes the comparison more than a copy.</summary>
    static int Disagreeing(Color[][] frames)
    {
        int n = 0;
        for (int i = 0; i < frames[0].Length; i++)
            for (int k = 1; k < frames.Length; k++)
                if (frames[k][i] != frames[0][i]) { n++; break; }
        return n;
    }

    /// <summary>The gameplay03 frame (as BorderTests draws it) at 480x270, five times as RenderSteady takes it and, so the vote
    /// has real disagreement to settle, five times with the camera turned 0.4 degrees further each time. Both medians agree on
    /// every channel of every pixel, for 5 frames (the fast path) and for 3 and 7 (the sort).</summary>
    [UnityTest]
    public IEnumerator OnRealFramesTheFastMedianIsBitEqualToTheSort()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var steady = new Color[5][];
        for (int k = 0; k < 5; k++) steady[k] = RenderSample.Render(cam, 480, 270).Pixels;
        var turned = new Color[7][];
        for (int k = 0; k < 7; k++) { rig.Yaw = PromoPose.Gameplay03.Yaw + 0.4f * k; rig.Apply(); turned[k] = RenderSample.Render(cam, 480, 270).Pixels; }
        int disagree = Disagreeing(First(turned, 5));
        TestContext.WriteLine($"steady frames disagree on {Disagreeing(steady)} px, turned frames on {disagree} of {480 * 270}");
        Assert.Greater(disagree, 480 * 270 / 4, "the turned frames give the vote real work");

        AssertBitEqual(ReferenceMedian(steady), RenderSample.Median(steady), "5 steady frames");
        AssertBitEqual(ReferenceMedian(First(turned, 5)), RenderSample.Median(First(turned, 5)), "5 turned frames");
        AssertBitEqual(ReferenceMedian(First(turned, 3)), RenderSample.Median(First(turned, 3)), "3 turned frames");
        AssertBitEqual(ReferenceMedian(turned), RenderSample.Median(turned), "7 turned frames");
        // The same five frames shuffled: the network is not tied to which render came first.
        AssertBitEqual(ReferenceMedian(First(turned, 5)), RenderSample.Median(new[] { turned[4], turned[2], turned[0], turned[3], turned[1] }), "5 turned frames, shuffled");
    }

    /// <summary>Synthetic channels: every one of the 5^5 sequences over five levels (ties of every shape, all equal, all
    /// different, in every order), then random draws from a pool of extremes (0, 1, the 8-bit steps, the smallest and largest
    /// finite floats of both signs, both infinities; no NaN and no -0, which the 8-bit reads never give). Bit-equal to the
    /// sort for 5 frames and, on the same data, for 1, 3, 4 and 7.</summary>
    [Test]
    public void OnSyntheticValuesTheFastMedianIsBitEqualToTheSort()
    {
        float[] levels = { 0f, 1f / 255f, 0.5f, 254f / 255f, 1f };
        float[] extremes =
        {
            0f, 1f, 1f / 255f, 128f / 255f, 254f / 255f, float.Epsilon, -float.Epsilon, float.MaxValue, -float.MaxValue,
            float.MinValue, float.PositiveInfinity, float.NegativeInfinity, 1e-30f, -1e-30f, 0.1f, -0.1f, 1.0000001f, 0.99999994f,
        };
        const int Exhaustive = 5 * 5 * 5 * 5 * 5, Random = 20000;
        var frames = new Color[7][];
        for (int k = 0; k < frames.Length; k++) frames[k] = new Color[Exhaustive + Random];
        for (int i = 0; i < Exhaustive; i++)
            for (int k = 0; k < 5; k++)
            {
                int digit = i / Pow5[k] % 5;
                // r walks the sequences in order, g reversed, b with the levels relabelled, so each channel sees them all.
                frames[k][i] = new Color(levels[digit], levels[4 - digit], levels[(digit * 2 + 1) % 5], 1f);
            }
        var rng = new System.Random(440);
        for (int i = Exhaustive; i < Exhaustive + Random; i++)
            for (int k = 0; k < 5; k++)
                frames[k][i] = new Color(extremes[rng.Next(extremes.Length)], extremes[rng.Next(extremes.Length)], extremes[rng.Next(extremes.Length)], 1f);
        for (int i = 0; i < Exhaustive + Random; i++)
            for (int k = 5; k < 7; k++)
                frames[k][i] = new Color(extremes[rng.Next(extremes.Length)], levels[rng.Next(levels.Length)], extremes[rng.Next(extremes.Length)], 1f);

        AssertBitEqual(ReferenceMedian(First(frames, 5)), RenderSample.Median(First(frames, 5)), "5 frames");
        foreach (var n in new[] { 1, 3, 4, 7 }) AssertBitEqual(ReferenceMedian(First(frames, n)), RenderSample.Median(First(frames, n)), $"{n} frames");
        // Spot checks against hand values: ties, all equal, all different.
        var tie = new[] { new[] { Color.white }, new[] { Color.black }, new[] { Color.white }, new[] { Color.black }, new[] { Color.white } };
        Assert.AreEqual(Color.white, RenderSample.Median(tie)[0]);
        var same = new Color[5][]; for (int k = 0; k < 5; k++) same[k] = new[] { new Color(0.2f, 0.4f, 0.6f, 1f) };
        Assert.AreEqual(new Color(0.2f, 0.4f, 0.6f, 1f), RenderSample.Median(same)[0]);
        var spread = new Color[5][]; for (int k = 0; k < 5; k++) spread[k] = new[] { new Color((4 - k) / 4f, k / 4f, (k * 3 % 5) / 4f, 0.3f) };
        Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f, 1f), RenderSample.Median(spread)[0]);
    }

    /// <summary>Timing only (run by name): RenderSteady at 480x270, 1600x900 and 1920x1080, split into the five renders, the
    /// old sorting median and the fast one, the best of three each.</summary>
    [UnityTest, Explicit("timing, run by name")]
    public IEnumerator RenderSteadyTiming()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        foreach (var (w, h) in new[] { (480, 270), (1600, 900), (1920, 1080) })
        {
            double renders = double.MaxValue, sorted = double.MaxValue, fast = double.MaxValue, steady = double.MaxValue;
            for (int run = 0; run < 3; run++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var frames = new Color[5][];
                for (int k = 0; k < 5; k++) frames[k] = RenderSample.Render(cam, w, h).Pixels;
                renders = System.Math.Min(renders, sw.Elapsed.TotalMilliseconds);
                sw.Restart(); var a = ReferenceMedian(frames); sorted = System.Math.Min(sorted, sw.Elapsed.TotalMilliseconds);
                sw.Restart(); var b = RenderSample.Median(frames); fast = System.Math.Min(fast, sw.Elapsed.TotalMilliseconds);
                AssertBitEqual(a, b, $"{w}x{h}");
                sw.Restart(); RenderSample.RenderSteady(cam, w, h); steady = System.Math.Min(steady, sw.Elapsed.TotalMilliseconds);
            }
            Debug.Log($"RENDERSTEADY {w}x{h}: 5 renders {renders:F1} ms, sorting median {sorted:F1} ms, fast median {fast:F1} ms; " +
                      $"before {renders + sorted:F1} ms, RenderSteady now {steady:F1} ms");
        }
    }
}
