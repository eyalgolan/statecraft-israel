#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 8: the shader-drawn hex grid.</summary>
public class HexGridTests
{
    const float Size = 5000f;

    [TearDown]
    public void Reset() { RenderSample.TiltShift(true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    [Test]
    public void CentresAreTheInradiusFromEveryEdgeAndCornersAndEdgesAreZero()
    {
        var c = HexGrid.Centre(3, -2, Size);
        float inradius = Mathf.Sqrt(3f) / 2f * Size;
        Assert.AreEqual(inradius, HexGrid.EdgeDistance(c, Size), 1e-2f);
        Assert.AreEqual(0f, HexGrid.EdgeDistance(c + new Vector2(inradius, 0f), Size), 1e-2f, "middle of the east edge");
        Assert.AreEqual(0f, HexGrid.EdgeDistance(c + new Vector2(inradius, -Size / 2f), Size), 1e-2f, "a corner");
        Assert.AreEqual(inradius / 2f, HexGrid.EdgeDistance(c + new Vector2(inradius / 2f, 0f), Size), 1e-2f);
    }

    [Test]
    public void NearestCentreAndToWorldInvertTheGridMapping()
    {
        var c = HexGrid.Centre(-2, 5, Size);
        Assert.AreEqual(c, HexGrid.NearestCentre(c + new Vector2(1000f, -800f), Size), "a point inside a hex rounds to its centre");
        var origin = new Vector3(10f, 5f, 20f); const float length = 431000f;
        var world = HexGrid.ToWorld(c, origin, length);
        Assert.AreEqual(c.x, HexGrid.FromWorld(world, origin, length).x, 1e-2f);
        Assert.AreEqual(c.y, HexGrid.FromWorld(world, origin, length).y, 1e-2f);
        Assert.Less(HexGrid.ToWorld(new Vector2(-4330f, 100f), origin, length).x, origin.x, "west of the map stays west");
    }

    /// <summary>The shader's layout is tools/hexgrid.py's: every hex centre in hexes.json lies on a grid centre.</summary>
    [Test]
    public void ItAgreesWithEveryHexCentreInHexesJson()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        float size = map.Hexes.hex_size_km * 1000f, inradius = Mathf.Sqrt(3f) / 2f * size;
        foreach (var h in map.Hexes.hexes)
        {
            var p = HexGrid.FromWorld(map.Frame.ToWorld(h.lon, h.lat, 0), map.Terrain.transform.position, map.Frame.LengthM);
            Assert.AreEqual(0f, (p - HexGrid.Centre(h.q, h.r, size)).magnitude, 50f, $"hex ({h.q}, {h.r}) centre");
            Assert.AreEqual(inradius, HexGrid.EdgeDistance(p, size), 50f, $"hex ({h.q}, {h.r})");
        }
    }

    static (MapScene map, Camera cam, CameraRig rig) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var cam = Camera.main;
        var rig = cam.GetComponent<CameraRig>();
        RenderSample.TiltShift(false);
        return (map, cam, rig);
    }

    /// <summary>Rendered: the middle of a land hex's east edge changes with the grid on (the promo's land lines are light).</summary>
    [UnityTest]
    public IEnumerator GridLinesShowOnHexEdges()
    {
        var (map, cam, rig) = Open();
        yield return null;
        var hex = System.Array.Find(map.Hexes.hexes, h => h.land && h.biome == "hills");
        var centre = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        var edge = centre + new Vector3(Mathf.Sqrt(3f) / 2f * map.Hexes.hex_size_km * 1000f, 0f, 0f);
        edge.y = map.Terrain.SampleHeight(edge);
        rig.LookAt = centre; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[1];
        var on = RenderSample.Render(cam, 960, 540, new[] { edge }, px);
        VisualToggles.Set("grid", false);
        var off = RenderSample.Render(cam, 960, 540, new[] { edge }, px);
        VisualToggles.Set("grid", true);
        float change = 0f;
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At((int)px[0].x + dx, (int)px[0].y + dy)) - RenderSample.Luma(off.At((int)px[0].x + dx, (int)px[0].y + dy))));
        TestContext.WriteLine($"edge luma change {change:0.000}");
        Assert.Greater(change, 0.03f);
    }

    /// <summary>Rendered at the closest zoom and the overview (250 km): lines are visible but cover little of the view
    /// (no moiré wash at the overview, still drawn up close).</summary>
    [UnityTest]
    public IEnumerator GridLinesStayThinAtBothZoomExtremes()
    {
        var (map, cam, rig) = Open();
        yield return null;
        // Look at the middle of a hex edge (review minor 2: a close view spans less than a few hexes, so an arbitrary point
        // might show no line at all).
        float size = map.Hexes.hex_size_km * 1000f;
        var origin = map.Terrain.transform.position;
        var centre = HexGrid.NearestCentre(HexGrid.FromWorld(map.Frame.ToWorld(35.0, 31.6, 0), origin, map.Frame.LengthM), size);
        var edgeMid = HexGrid.ToWorld(centre + new Vector2(Mathf.Sqrt(3f) / 2f * size, 0f), origin, map.Frame.LengthM);
        edgeMid.y = map.Terrain.SampleHeight(edgeMid);
        foreach (var distance in new[] { CameraController.MinDistance, CameraController.MaxDistance })
        {
            rig.LookAt = edgeMid; rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            // At the game's 1920x1080: lines fade out by their hexes' size in pixels (Task 8 fix round 1, I2).
            var on = RenderSample.Render(cam, 1920, 1080);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 1920, 1080);
            VisualToggles.Set("grid", true);
            float covered = RenderSample.FractionDarker(on, off, 0.02f) + RenderSample.FractionDarker(off, on, 0.02f);
            TestContext.WriteLine($"{distance} m: grid covers {covered:P2}");
            Assert.That(covered, Is.InRange(0.0005f, 0.25f), $"{distance} m");
        }
    }

    /// <summary>Task 8 review I1, rendered at the gameplay03 pose (1920x1080): along one straight hex edge on the sea and one
    /// on land, the line is about GridWidthPx wide (median full width at half maximum, across the line, 1 to 2.5 px; no
    /// sample under 1 px) and, on the sea, never breaks (each sample's peak at least 0.7 of the edge's strongest). Both
    /// opacities are set to 1 for the measurement, so the line's profile is measured, not its colour; differences are in
    /// linear luma. The break check is on the sea only: its background is even, while a land line's peak also follows
    /// the lit ground under it (0.54 along a land edge whose widths all lie in 1.4-2.0 px). Land and sea share ScGridLine.</summary>
    [UnityTest]
    public IEnumerator GridLinesAreUnbrokenAndAboutTheirWidthAtGameplay03()
    {
        var (map, cam, rig) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame);
        rig.TopInset = 0; rig.Apply();
        // The line's profile on the ground itself: the settlement kits and trees (art spike) stand on some edges.
        foreach (var n in new[] { "kits", "trees" }) if (VisualToggles.Names.Contains(n)) VisualToggles.Set(n, false);
        float size = map.Hexes.hex_size_km * 1000f, inradius = Mathf.Sqrt(3f) / 2f * size;
        var origin = map.Terrain.transform.position;
        HexJson? Find(double lon, double lat, System.Func<HexJson, bool> ok)
        {
            HexJson? best = null; double bestD = double.MaxValue;
            foreach (var h in map.Hexes.hexes)
            {
                var east = System.Array.Find(map.Hexes.hexes, e => e.q == h.q + 1 && e.r == h.r);
                if (east == null || !ok(h) || !ok(east)) continue;
                double dd = (h.lon - lon) * (h.lon - lon) + (h.lat - lat) * (h.lat - lat);
                if (dd < bestD) { bestD = dd; best = h; }
            }
            return best;
        }
        var edges = new[] { ("sea", Find(34.72, 32.02, h => h.biome == "sea")), ("land", Find(34.95, 32.0, h => h.land && h.biome != "lake")) };
        const int W = 1920, H = 1080, N = 25;
        try
        {
            foreach (var (what, hex) in edges)
            {
                Assert.IsNotNull(hex, what);
                var c = HexGrid.Centre(hex!.q, hex.r, size);
                var world = new Vector3[N + 2]; var px = new Vector2[N + 2];
                Vector3 At(float t)
                {
                    var w = HexGrid.ToWorld(new Vector2(c.x + inradius, c.y - size / 2f + t * size), origin, map.Frame.LengthM);
                    w.y = map.Terrain.SampleHeight(w) + origin.y;
                    return w;
                }
                for (int i = 0; i < N; i++) world[i] = At(0.1f + 0.8f * i / (N - 1));
                world[N] = At(0f); world[N + 1] = At(1f);
                Shader.SetGlobalVector(MapShaderGlobals.Grid, new Vector4(1f, 1f, MapShaderGlobals.GridWidthPx, 1f));
                var on = RenderSample.Render(cam, W, H, world, px);
                VisualToggles.Set("grid", false);
                var off = RenderSample.Render(cam, W, H, world, px);
                VisualToggles.Set("grid", true);
                // Across the line: a horizontal profile is 1 / |n.x| times as wide as the line, n the line's screen normal.
                var dir = (px[N + 1] - px[N]).normalized; float nx = Mathf.Abs(dir.y);
                float Lin(Color s) => RenderSample.Luma(new Color(Mathf.GammaToLinearSpace(s.r), Mathf.GammaToLinearSpace(s.g), Mathf.GammaToLinearSpace(s.b)));
                var peaks = new float[N]; var widths = new float[N];
                for (int i = 0; i < N; i++)
                {
                    int x0 = Mathf.RoundToInt(px[i].x), y0 = Mathf.RoundToInt(px[i].y);
                    var prof = new float[17];
                    for (int k = 0; k < 17; k++) prof[k] = Mathf.Max(0f, Lin(on.At(x0 + k - 8, y0)) - Lin(off.At(x0 + k - 8, y0)));
                    int m = 0; for (int k = 1; k < 17; k++) if (prof[k] > prof[m]) m = k;
                    float peak = prof[m], half = peak / 2f;
                    int l = m; while (l > 0 && prof[l - 1] >= half) l--;
                    int r = m; while (r < 16 && prof[r + 1] >= half) r++;
                    // Linear interpolation of the two half-maximum crossings.
                    float left = l > 0 ? l - (prof[l] - half) / Mathf.Max(prof[l] - prof[l - 1], 1e-6f) : l - 0.5f;
                    float right = r < 16 ? r + (prof[r] - half) / Mathf.Max(prof[r] - prof[r + 1], 1e-6f) : r + 0.5f;
                    peaks[i] = peak; widths[i] = (right - left) * nx;
                }
                float max = Mathf.Max(peaks), min = Mathf.Min(peaks);
                var sorted = (float[])widths.Clone(); System.Array.Sort(sorted);
                float median = sorted[N / 2];
                TestContext.WriteLine($"{what} edge of ({hex.q}, {hex.r}): peak min {min:0.000} max {max:0.000} ratio {min / max:0.00}; FWHM median {median:0.00} px, range {sorted[0]:0.00}-{sorted[N - 1]:0.00} (|n.x| {nx:0.00})");
                Assert.Greater(max, 0.02f, $"{what}: a line is drawn");
                if (what == "sea") Assert.GreaterOrEqual(min / max, 0.7f, $"{what}: the line never breaks along the edge");
                Assert.GreaterOrEqual(sorted[0], 1f, $"{what}: no sample along the edge is under a pixel wide");
                Assert.That(median, Is.InRange(1f, 2.5f), $"{what}: the line is about {MapShaderGlobals.GridWidthPx} px wide");
            }
        }
        finally { VisualToggles.Set("grid", true); }
    }

    [Test]
    public void TheLineMeshAndItsMaterialAreGone()
    {
        Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexOverlay.mat"));
        var map = new GameObject("m").AddComponent<MapScene>();
        try { Assert.IsNull(new SerializedObject(map).FindProperty("overlayMaterial")); }
        finally { Object.DestroyImmediate(map.gameObject); }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var built = Object.FindFirstObjectByType<MapScene>(); built.Build();
        Assert.IsNull(built.Terrain.transform.Find("HexOverlay"));
        CollectionAssert.Contains(VisualToggles.Names, "grid");
    }
}
