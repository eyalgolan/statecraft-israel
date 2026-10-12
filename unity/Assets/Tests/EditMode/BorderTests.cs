#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>promo-borders: the region border renderer (MapBorders, StatecraftBorders.hlsl). The rendered tests draw the
/// synthetic BorderProof pattern; the real 1948 sub-districts, which play draws (GameRoot), are SubdistrictTests'.</summary>
public class BorderTests
{
    [TearDown]
    public void Reset()
    {
        MapShaderGlobals.PinWaterTime(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    internal static (MapScene map, Camera cam, CameraRig rig) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        MapShaderGlobals.PinWaterTime(100f);   // renders compared pixel for pixel: the sea must not move between them
        var cam = Camera.main;
        return (map, cam, cam.GetComponent<CameraRig>());
    }

    [Test]
    public void ARegionLayerRejectsBadIdsAndRegionsWithoutAStyle()
    {
        var hexes = new Dictionary<(int q, int r), int> { [(0, 0)] = 1 };
        Assert.Throws<System.ArgumentException>(() => new RegionLayer("t", hexes, new Dictionary<int, BorderStyle>()));
        Assert.Throws<System.ArgumentException>(() => new RegionLayer("t", hexes, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Primary, [0] = BorderStyle.Primary }));
        Assert.Throws<System.ArgumentException>(() => new RegionLayer("t", hexes, new Dictionary<int, BorderStyle> { [1] = (BorderStyle)7 }));
        Assert.Throws<System.ArgumentException>(() => new RegionLayer(" ", hexes, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Primary }));
        var ok = new RegionLayer("t", hexes, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Secondary });
        Assert.AreEqual(1, ok.RegionOf(0, 0)); Assert.AreEqual(0, ok.RegionOf(1, 0));
        Assert.AreEqual(BorderStyle.Secondary, ok.StyleOf(1)); Assert.AreEqual(BorderStyle.None, ok.StyleOf(2));
    }

    [Test]
    public void AHexTexelCarriesItsRegionInTwoBytesAndItsStyle()
    {
        var c = MapBorders.Encode(300, BorderStyle.Secondary);
        Assert.AreEqual((44, 1, 2, 255), ((int)c.r, (int)c.g, (int)c.b, (int)c.a));
        Assert.AreEqual(RegionLayer.MaxId, MapBorders.Encode(RegionLayer.MaxId, BorderStyle.None).r + 256 * MapBorders.Encode(RegionLayer.MaxId, BorderStyle.None).g);
    }

    [Test]
    public void TheProofIsACheckerOfHexBlocksOnLandOnlyAndLabelledSynthetic()
    {
        var (map, _, _) = Open();
        var layer = BorderProof.Build(map.Hexes);
        StringAssert.Contains("SYNTHETIC TEST PATTERN", layer.Banner);
        foreach (var h in map.Hexes.hexes)
        {
            int id = layer.RegionOf(h.q, h.r);
            if (!h.land) { Assert.AreEqual(0, id, $"water hex ({h.q}, {h.r}) is in no region"); continue; }
            var (bq, br) = BorderProof.Block(h.q, h.r);
            Assert.AreEqual(((bq + br) & 1) == 0 ? BorderStyle.Primary : BorderStyle.Secondary, layer.StyleOf(id));
            // Hexes share a region exactly when they share a block.
            var east = (h.q + 1, h.r);
            if (layer.RegionOf(east.Item1, east.Item2) != 0)
                Assert.AreEqual(BorderProof.Block(h.q, h.r) == BorderProof.Block(east.Item1, east.Item2), id == layer.RegionOf(east.Item1, east.Item2));
        }
        Assert.AreEqual((0, 0), BorderProof.Block(3, 3)); Assert.AreEqual((-1, 1), BorderProof.Block(-1, 4));
        Assert.AreEqual((-2, 0), BorderProof.Block(-5, 0));
    }

    /// <summary>Review I3: a release (shareable) build ignores the proof's flag; the editor and development builds honour it.</summary>
    [Test]
    public void AReleaseBuildIgnoresTheProofFlag()
    {
        const string url = "https://example.invalid/index.html?golden&borderproof";
        var args = new[] { "Statecraft", "-borderproof" };
        Assert.IsFalse(BorderProof.Requested(url, args, webgl: true, devBuild: false));
        Assert.IsFalse(BorderProof.Requested(url, args, webgl: false, devBuild: false));
        Assert.IsTrue(BorderProof.Requested(url, args, webgl: true, devBuild: true));
        Assert.IsTrue(BorderProof.Requested(url, args, webgl: false, devBuild: true));
        Assert.IsFalse(BorderProof.Requested("https://example.invalid/?borderproofx", new string[0], webgl: true, devBuild: true));
    }

    /// <summary>Re-review: the label really renders (a legacy TextMesh under URP), in the upper left, at 16:9 and at 4:3:
    /// the upper-left region changes between renders with the label shown and hidden, nothing else does, and its left end
    /// is on screen.</summary>
    [UnityTest]
    public IEnumerator TheProofLabelRendersAtTheTopLeft()
    {
        var (map, cam, rig) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        BorderProof.Show(map);
        var banner = Camera.main.transform.Find("BorderBanner").gameObject;
        foreach (var (w, h) in new[] { (1920, 1080), (1440, 1080) })
        {
            var on = RenderSample.Render(cam, w, h);
            banner.SetActive(false);
            var off = RenderSample.Render(cam, w, h);
            banner.SetActive(true);
            int topLeft = 0, elsewhere = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color a = on.At(x, y), b = off.At(x, y);
                    if (Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))) <= 0.1f) continue;
                    if (x < w * 0.75f && y > h * 0.5f) topLeft++; else elsewhere++;   // row 0 is the bottom
                }
            TestContext.WriteLine($"{w}x{h}: {topLeft} label pixels at the top left, {elsewhere} elsewhere");
            Assert.Greater(topLeft, w * h / 2000, $"{w}x{h}: the label draws at the top left");
            Assert.AreEqual(0, elsewhere, $"{w}x{h}: and nowhere else");
            // Its left end is on screen: some changed pixel within the first 8% of the width.
            bool leftEnd = false;
            for (int y = (int)(h * 0.5f); y < h && !leftEnd; y++)
                for (int x = 0; x < w * 0.08f && !leftEnd; x++)
                {
                    Color a = on.At(x, y), b = off.At(x, y);
                    leftEnd = Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))) > 0.1f;
                }
            Assert.IsTrue(leftEnd, $"{w}x{h}: the label starts at the left edge, not off screen");
        }
    }

    /// <summary>Review I3: whenever the proof is drawn its label is on screen, and it goes with the drawing.</summary>
    [Test]
    public void TheProofIsLabelledOnScreenWhileDrawn()
    {
        var (map, _, _) = Open();
        BorderProof.Show(map);
        var banner = Camera.main.transform.Find("BorderBanner");
        Assert.IsNotNull(banner, "the label exists");
        Assert.IsTrue(banner.gameObject.activeSelf);
        foreach (var tm in banner.GetComponentsInChildren<TextMesh>(true)) Assert.AreEqual(BorderProof.Label, tm.text);
        VisualToggles.Set(MapBorders.ToggleName, false);
        Assert.IsFalse(banner.gameObject.activeSelf, "hidden with the borders");
        VisualToggles.Set(MapBorders.ToggleName, true);
        Assert.IsTrue(banner.gameObject.activeSelf);
        MapBorders.Use(null);
        Assert.IsTrue(banner == null, "gone with the layer");
    }

    [Test]
    public void UseRejectsAHexOffTheMapAndWorksOnlyOnABuiltMap()
    {
        var (map, _, _) = Open();
        var bad = new RegionLayer("t", new Dictionary<(int q, int r), int> { [(500, 500)] = 1 }, new Dictionary<int, BorderStyle> { [1] = BorderStyle.Primary });
        Assert.Throws<System.ArgumentException>(() => MapBorders.Use(bad));
        Reset();
        Assert.Throws<System.InvalidOperationException>(() => MapBorders.Use(null));
    }

    /// <summary>Without data (MapScene.Build alone; play draws the 1948 sub-districts, SubdistrictTests) the "borders" toggle is a no-op: the frame is identical with it on and off (that both run the
    /// same shader is the point: the branch's frame against main's is checked by the art checkpoints, docs/visual-v1.md,
    /// "Region borders"). Once a layer is in use the toggle removes it (with its label), and an empty layer draws
    /// nothing.</summary>
    [UnityTest]
    public IEnumerator WithoutRegionDataTheToggleIsANoOp()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        CollectionAssert.Contains(VisualToggles.Names, MapBorders.ToggleName);
        Assert.IsNull(MapBorders.Current);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var on = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, 480, 270);
        Assert.AreEqual(0f, Changed(on, off, 0f), "no layer: the toggle changes nothing");
        VisualToggles.Set(MapBorders.ToggleName, true);
        BorderProof.Show(map);
        var proof = RenderSample.Render(cam, 480, 270);
        Assert.Greater(Changed(proof, off, 0.02f), 0.002f, "the proof draws");
        VisualToggles.Set(MapBorders.ToggleName, false);
        Assert.AreEqual(0f, Changed(RenderSample.Render(cam, 480, 270), off, 0f), "the toggle removes a layer in use");
        VisualToggles.Set(MapBorders.ToggleName, true);
        MapBorders.Use(new RegionLayer("empty", new Dictionary<(int q, int r), int>(), new Dictionary<int, BorderStyle>()));
        Assert.AreEqual(0f, Changed(RenderSample.Render(cam, 480, 270), off, 0f), "an empty layer draws nothing");
    }

    /// <summary>Share of pixels whose largest channel differs by more than <paramref name="by"/>.</summary>
    internal static float Changed(RenderSample a, RenderSample b, float by)
    {
        int n = 0;
        for (int i = 0; i < a.Pixels.Length; i++)
        {
            Color x = a.Pixels[i], y = b.Pixels[i];
            if (Mathf.Max(Mathf.Abs(x.r - y.r), Mathf.Max(Mathf.Abs(x.g - y.g), Mathf.Abs(x.b - y.b))) > by) n++;
        }
        return (float)n / a.Pixels.Length;
    }

    /// <summary>One walk across a hex edge at one point along it: the pixels from 1.5 km inside the inner hex to 1.5 km inside
    /// the outer one, with the borders on and off, and each pixel's signed distance (m; negative: the inner hex).</summary>
    internal sealed class Walk
    {
        public readonly List<Color> On = new List<Color>(), Off = new List<Color>();
        public readonly List<float> SignedM = new List<float>();
        public readonly List<Vector2> Px = new List<Vector2>();
    }

    const float WalkM = 1500f, StepM = 10f;

    /// <summary>Walks across the edge between axial hexes <paramref name="inner"/> and <paramref name="outer"/> (neighbours)
    /// at <paramref name="samples"/> points spread over the middle 60% of the edge, at the camera's current pose, rendering
    /// at width x height with the proof drawn (on) and the borders off. Distinct pixels only, in walk order.</summary>
    internal static List<Walk> WalkEdge(MapScene map, Camera cam, (int q, int r) inner, (int q, int r) outer, int samples, int width, int height)
    {
        float size = map.Hexes.hex_size_km * 1000f, inradius = Mathf.Sqrt(3f) / 2f * size;
        var origin = map.Terrain.transform.position;
        Vector2 ci = HexGrid.Centre(inner.q, inner.r, size), co = HexGrid.Centre(outer.q, outer.r, size);
        var n = (co - ci).normalized; var t = new Vector2(-n.y, n.x);
        int steps = Mathf.RoundToInt(2 * WalkM / StepM) + 1;
        var world = new Vector3[samples * steps]; var signed = new float[world.Length];
        for (int i = 0; i < samples; i++)
            for (int k = 0; k < steps; k++)
            {
                float u = samples == 1 ? 0f : -0.3f + 0.6f * i / (samples - 1), s = -WalkM + k * StepM;
                var p = ci + n * (inradius + s) + t * (u * size);
                var w = HexGrid.ToWorld(p, origin, map.Frame.LengthM);
                w.y = map.Terrain.SampleHeight(w) + origin.y;
                world[i * steps + k] = w; signed[i * steps + k] = s;
            }
        var px = new Vector2[world.Length];
        var on = RenderSample.Render(cam, width, height, world, px);
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, width, height, world, px);
        VisualToggles.Set(MapBorders.ToggleName, true);
        var walks = new List<Walk>();
        for (int i = 0; i < samples; i++)
        {
            var walk = new Walk(); var seen = new HashSet<(int, int)>();
            for (int k = 0; k < steps; k++)
            {
                var q = px[i * steps + k]; int x = Mathf.FloorToInt(q.x), y = Mathf.FloorToInt(q.y);
                if (!seen.Add((x, y))) continue;
                walk.On.Add(on.At(x, y)); walk.Off.Add(off.At(x, y)); walk.SignedM.Add(signed[i * steps + k]); walk.Px.Add(new Vector2(x, y));
            }
            walks.Add(walk);
        }
        return walks;
    }

    /// <summary>A primary-secondary edge of the proof: the land hex of <paramref name="biome"/> nearest (lon, lat) that is
    /// Primary with a Secondary east neighbour of the same biome (their shared edge runs north-south).</summary>
    internal static ((int q, int r) inner, (int q, int r) outer) ProofEdge(MapScene map, double lon, double lat, string biome)
    {
        var layer = MapBorders.Current ?? throw new System.InvalidOperationException("show the proof first");
        var byHex = map.Hexes.hexes.ToDictionary(h => (h.q, h.r));
        HexJson? best = null; double bestD = double.MaxValue;
        foreach (var h in map.Hexes.hexes)
        {
            if (!h.land || h.biome != biome || layer.StyleOf(layer.RegionOf(h.q, h.r)) != BorderStyle.Primary) continue;
            if (!byHex.TryGetValue((h.q + 1, h.r), out var e) || !e.land || e.biome != biome) continue;
            if (layer.StyleOf(layer.RegionOf(e.q, e.r)) != BorderStyle.Secondary) continue;
            double d = (h.lon - lon) * (h.lon - lon) + (h.lat - lat) * (h.lat - lat);
            if (d < bestD) { bestD = d; best = h; }
        }
        Assert.IsNotNull(best, $"a {biome} primary-secondary edge");
        return ((best!.q, best.r), (best.q + 1, best.r));
    }

    /// <summary>Looks at an edge from gameplay03's distance and yaw (the edge's midpoint in the middle of the frame).</summary>
    internal static void LookAtEdge(MapScene map, CameraRig rig, (int q, int r) inner)
    {
        var h = map.Hexes.hexes.First(x => x.q == inner.q && x.r == inner.r);
        rig.LookAt = map.Frame.ToWorld(h.lon + 0.04, h.lat, 0); rig.Distance = PromoPose.Gameplay03.Distance; rig.Yaw = PromoPose.Gameplay03.Yaw;
        rig.TopInset = 0; rig.Apply();
    }

    /// <summary>What one side of a walk shows: its most saturated border pixel (the coloured stripe), its lightest (the light
    /// stripe), the land under them (borders off), and how far from the edge (px along the walk) the stripes reach.</summary>
    internal readonly struct Side
    {
        public readonly Color Stripe, Light, Land; public readonly float WidthPx;
        public Side(Color stripe, Color light, Color land, float widthPx) { Stripe = stripe; Light = light; Land = land; WidthPx = widthPx; }
    }

    /// <summary>The side of the walk with sign <paramref name="sign"/> (-1 inner, +1 outer). The stripe is the pixel that
    /// gains most in <paramref name="hue"/> over the land, the light stripe the lightest pixel. The width counts the band's
    /// pixels: those that gained <paramref name="stripeGain"/> in hue (the coloured stripe) or are light stripe colour (all
    /// channels >= 0.75 and lighter than the land by deltaE76 >= 8: white or cream, which no wash over this land reaches).</summary>
    internal static Side Read(Walk w, int sign, System.Func<Color, float> hue, float stripeGain = 0.1f)
    {
        int stripe = -1, light = -1, band = 0; float bestHue = float.MinValue, bestLuma = float.MinValue;
        for (int k = 0; k < w.On.Count; k++)
        {
            if (Mathf.Sign(w.SignedM[k]) != sign || Mathf.Abs(w.SignedM[k]) > 900f) continue;
            float gain = hue(w.On[k]) - hue(w.Off[k]);
            if (gain > bestHue) { bestHue = gain; stripe = k; }
            float luma = RenderSample.Luma(w.On[k]);
            if (luma > bestLuma) { bestLuma = luma; light = k; }
            var c = w.On[k];
            bool lighter = Mathf.Min(c.r, Mathf.Min(c.g, c.b * 1.6f)) >= 0.75f && luma > RenderSample.Luma(w.Off[k]) && RenderSample.DeltaE(c, w.Off[k]) >= 8f;
            if (gain >= stripeGain || lighter) band++;
        }
        return new Side(stripe < 0 ? Color.clear : w.On[stripe], light < 0 ? Color.clear : w.On[light], stripe < 0 ? Color.clear : w.Off[stripe], band);
    }

    internal static float Blueness(Color c) => c.b - c.r;
    internal static float Redness(Color c) => c.r - c.g;

    /// <summary>The promo 03 look at gameplay03's zoom (1920x1080, tilt-shift off), on a coastal-plain edge (flat, as the
    /// promo's lines are measured): along the whole edge, the Primary side carries a blue stripe and a near-white one, the
    /// Secondary side a red-orange stripe and a cream one, each band about its promo width (promo 03 at 1920 px: blue +
    /// white about 8 px, red + cream about 5 px), the Primary band the wider; deep inside either hex nothing changes.</summary>
    [UnityTest]
    public IEnumerator BothStylesFollowTheEdgeAtAboutThePromoWidthAtGameplay03()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        var layer = BorderProof.Show(map);
        var (inner, outer) = ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        LookAtEdge(map, rig, inner);
        var walks = WalkEdge(map, cam, inner, outer, 9, 1920, 1080);
        var pw = new List<float>(); var sw = new List<float>();
        foreach (var w in walks)
        {
            var p = Read(w, -1, Blueness, 0.45f); var s = Read(w, +1, Redness);   // 0.45: the blue wash gains about 0.3
            TestContext.WriteLine($"primary stripe {p.Stripe} light {p.Light} land {p.Land} width {p.WidthPx:0.0} px | secondary stripe {s.Stripe} light {s.Light} land {s.Land} width {s.WidthPx:0.0} px");
            Assert.Greater(Blueness(p.Stripe) - Blueness(p.Land), 0.15f, "a blue stripe on the primary side");
            Assert.Greater(Mathf.Min(p.Light.r, Mathf.Min(p.Light.g, p.Light.b)), 0.80f, "a near-white stripe on the primary side");
            Assert.Greater(Redness(s.Stripe) - Redness(s.Land), 0.10f, "a red-orange stripe on the secondary side");
            Assert.Greater(RenderSample.Luma(s.Light), RenderSample.Luma(s.Land) + 0.03f, "a light cream stripe on the secondary side");
            Assert.Greater(s.Light.r, s.Light.b + 0.15f, "the secondary light stripe is cream, not white");
            pw.Add(p.WidthPx); sw.Add(s.WidthPx);
            // 1.2 km or more from the edge (past the widest wash at this zoom) the borders change nothing.
            for (int k = 0; k < w.On.Count; k++)
                if (Mathf.Abs(w.SignedM[k]) >= 1200f) Assert.Less(RenderSample.DeltaE(w.On[k], w.Off[k]), 2f, $"{w.SignedM[k]} m from the edge");
        }
        float pm = Median(pw), sm = Median(sw);
        TestContext.WriteLine($"edge {inner}-{outer}: band widths (median px): primary {pm:0.0}, secondary {sm:0.0}");
        Assert.That(pm, Is.InRange(6f, 11f), "primary band width (promo about 8 px)");
        Assert.That(sm, Is.InRange(3.5f, 8f), "secondary band width (promo about 5 px)");
        Assert.Greater(pm, sm, "the primary band is the heavier");
    }

    /// <summary>Review M6: the early-out (pixels beyond any band's reach fetch nothing) is exact: with it switched off
    /// (_ScBorders.z) the proof renders pixel for pixel the same, at both promo poses.</summary>
    [UnityTest]
    public IEnumerator TheEarlyOutIsExact()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        try
        {
            foreach (var pose in PromoPose.All)
            {
                pose.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
                var fast = RenderSample.Render(cam, 960, 540);
                var again = RenderSample.Render(cam, 960, 540);   // control: the same settings twice
                MapBorders.NoEarlyOut = true;
                var full = RenderSample.Render(cam, 960, 540);
                MapBorders.NoEarlyOut = false;
                float control = Changed(fast, again, 0f), exact = Changed(fast, full, 0f), maxDiff = 0f;
                for (int i = 0; i < fast.Pixels.Length; i++)
                {
                    Color a = fast.Pixels[i], b = full.Pixels[i];
                    maxDiff = Mathf.Max(maxDiff, Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))));
                }
                TestContext.WriteLine($"{pose.Name}: early-out vs none {exact:P4} of pixels (largest channel step {maxDiff * 255f:0.0}/255); repeat render {control:P4}");
                Assert.AreEqual(0f, exact, pose.Name);
            }
        }
        finally { MapBorders.NoEarlyOut = false; }
    }

    /// <summary>Review M7: bands stay on land. At gameplay03 (where the proof's checker boundaries reach the coast inside
    /// coastal land hexes) no pixel that is clearly water with the borders off changes with them on.</summary>
    [UnityTest]
    public IEnumerator BandsStayOffTheWater()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set(MapBorders.ToggleName, true);
        BorderProof.Show(map);
        Camera.main.transform.Find("BorderBanner").gameObject.SetActive(false);   // the label is not a band
        var on = RenderSample.Render(cam, 1920, 1080);
        int water = 0, moved = 0, changed = 0;
        for (int i = 0; i < on.Pixels.Length; i++)
        {
            Color a = off.Pixels[i], b = on.Pixels[i];
            bool diff = Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))) > 0.02f;
            if (diff) changed++;
            if (a.b - a.r > 0.15f && a.g > a.r) { water++; if (diff) moved++; }
        }
        TestContext.WriteLine($"gameplay03: {water} water pixels, {moved} moved; {changed} pixels changed in all");
        Assert.Greater(water, 100000, "the frame shows the sea");
        Assert.Greater(changed, 5000, "the proof draws");
        Assert.AreEqual(0, moved);
    }

    /// <summary>A hex in a region next to a hex in none (here the coast: the proof leaves the sea out) gets no band: the
    /// middle of such an edge is unchanged.</summary>
    [UnityTest]
    public IEnumerator NoBandRunsAgainstAHexInNoRegion()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var layer = BorderProof.Show(map);
        var centre = BorderProof.HexAt(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat);
        // The coastal land hex nearest the centre whose west neighbour is sea, and whose land neighbours share its region,
        // so no other border is near that edge.
        var byHex = map.Hexes.hexes.ToDictionary(h => (h.q, h.r));
        var steps = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
        (int q, int r)? land = null;
        foreach (var h in map.Hexes.hexes.OrderBy(h => BorderProof.Distance((h.q, h.r), centre)))
        {
            if (!h.land || !byHex.TryGetValue((h.q - 1, h.r), out var west) || west.land) continue;
            int id = layer.RegionOf(h.q, h.r);
            if (steps.Any(d => { int o = layer.RegionOf(h.q + d.Item1, h.r + d.Item2); return o != 0 && o != id; })) continue;
            land = (h.q, h.r); break;
        }
        Assert.IsNotNull(land);
        var walks = WalkEdge(map, cam, land!.Value, (land.Value.q - 1, land.Value.r), 3, 960, 540);
        int moved = 0, total = 0;
        foreach (var w in walks)
            for (int k = 0; k < w.On.Count; k++) { total++; if (RenderSample.DeltaE(w.On[k], w.Off[k]) >= 2f) moved++; }
        TestContext.WriteLine($"coast edge at ({land.Value.q}, {land.Value.r}) region {layer.RegionOf(land.Value.q, land.Value.r)}: {moved}/{total} pixels moved");
        Assert.AreEqual(0, moved);
    }

    /// <summary>At the farthest zoom (diorama02) the outlines stay visible but thin: they change some of the frame and never
    /// wash it (promo 03's minimap shows thin region outlines at the overview).</summary>
    [UnityTest]
    public IEnumerator AtTheOverviewTheBandsAreThin()
    {
        var (map, cam, rig) = Open();
        yield return null;
        RenderSample.TiltShift(false);
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set(MapBorders.ToggleName, true);
        BorderProof.Show(map);
        var on = RenderSample.Render(cam, 1920, 1080);
        float share = Changed(on, off, 0.06f);
        TestContext.WriteLine($"diorama02: {share:P2} of pixels changed by the proof's outlines");
        Assert.That(share, Is.InRange(0.01f, 0.15f));
    }

    /// <summary>The same layer and pose give the same pixels (no time, no randomness in the borders). The pattern is shown
    /// once and a warm-up frame is drawn first, with shaders compiled synchronously: showing it again re-created the
    /// banner's TextMesh, which can repack the dynamic font's atlas, and an editor shader variant could finish compiling
    /// between the two frames; either changed a pixel or two now and then, not the borders.</summary>
    [UnityTest]
    public IEnumerator BordersAreDeterministic()
    {
        bool async = UnityEditor.ShaderUtil.allowAsyncCompilation;
        UnityEditor.ShaderUtil.allowAsyncCompilation = false;
        try
        {
            var (map, cam, rig) = Open();
            yield return null;
            RenderSample.TiltShift(false);
            PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
            BorderProof.Show(map);
            RenderSample.Render(cam, 480, 270);   // warm-up: the banner's glyphs and every variant in place
            var a = RenderSample.Render(cam, 480, 270);
            var b = RenderSample.Render(cam, 480, 270);
            Assert.AreEqual(0f, Changed(a, b, 0f));
        }
        finally { UnityEditor.ShaderUtil.allowAsyncCompilation = async; }
    }

    static float Median(List<float> v) { var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
}
