#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 4: the grade, the tonemapper, the vignette, and what they do to readability.</summary>
public class GradeTests
{
    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static (MapScene map, CameraRig rig, VolumeProfile profile) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var rig = Camera.main.GetComponent<CameraRig>();
        return (map, rig, rig.Volume.profile);
    }

    [Test]
    public void TheGradeIsWarmLandTealShadowsAndAGentleVignette()
    {
        var (_, _, profile) = Open();
        Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var ca));
        Assert.AreEqual(MapGrade.Saturation, ca.saturation.value); Assert.Greater(ca.contrast.value, 0f);
        Assert.That(MapGrade.Saturation, Is.InRange(-10f, 4f), "the grade does not lift saturation (merge point)");
        Assert.IsTrue(profile.TryGet<WhiteBalance>(out var wb)); Assert.Greater(wb.temperature.value, 0f, "warm");
        Assert.IsTrue(profile.TryGet<SplitToning>(out var st));
        Assert.Greater(st.shadows.value.b, st.shadows.value.r, "teal shadows"); Assert.Greater(st.highlights.value.r, st.highlights.value.b, "amber highlights");
        Assert.IsTrue(profile.TryGet<Vignette>(out var v)); Assert.That(v.intensity.value, Is.InRange(0.2f, 0.35f));
        Assert.IsTrue(profile.TryGet<Tonemapping>(out var tm)); Assert.AreEqual(TonemappingMode.Neutral, tm.mode.value);
        CollectionAssert.Contains(VisualToggles.Names, "grade");
        // The runtime profile is invisible to URP's build-time post-processing stripping, which keeps the tonemapper's
        // uber-shader variants only when some VolumeProfile asset under Assets has a Tonemapping component (URP 17.3
        // ShaderBuildPreprocessor.GetSupportedFeaturesFromVolumes; strip-unused-post-processing is on). The pipeline assets'
        // own profile (Settings/SampleSceneProfile) and the global settings' Settings/DefaultVolumeProfile both carry one today,
        // so this fails only if every such asset loses it, which would make the WebGL player silently drop Neutral.
        bool kept = false;
        foreach (var guid in AssetDatabase.FindAssets("t:VolumeProfile", new[] { "Assets" }))
            kept |= AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(guid))?.Has<Tonemapping>() == true;
        Assert.IsTrue(kept, "a VolumeProfile asset with Tonemapping keeps _TONEMAP_NEUTRAL in builds");
    }

    static HexJson NearestLandHex(MapScene map, double lon, double lat)
    {
        HexJson? best = null;
        foreach (var h in map.Hexes.hexes)
            if (h.land && h.biome != "lake" && (best == null || (h.lon - lon) * (h.lon - lon) + (h.lat - lat) * (h.lat - lat) < (best.lon - lon) * (best.lon - lon) + (best.lat - lat) * (best.lat - lat))) best = h;
        return best!;
    }

    /// <summary>Selects the land hex nearest (lon, lat) and returns world points on the band (the middle of an edge, read
    /// from the band's own mesh, so it holds whatever height the band rides at) and on the land 1 km inside it. The edge is
    /// the second, or the next whose land point lies outside every settlement's and 1948 locality's kit ground (its
    /// footprint and tint, plus 500 m): the band is measured against the land, not against a village's houses.</summary>
    static (Vector3 band, Vector3 land) SelectNear(MapScene map, double lon, double lat)
    {
        var hex = NearestLandHex(map, lon, lat);
        map.Select(hex.q, hex.r);
        var sel = map.Terrain.transform.Find(MapScene.SelectionName);
        var v = sel.GetComponent<MeshFilter>().sharedMesh.vertices;
        int n = MapBuilder.RingVertices;
        var centre = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        var places = map.Settlements.Concat(map.Localities.Select(p => p.Settlement)).ToList();
        float hexM = map.Hexes.hex_size_km * 1000f;
        bool OnKitGround(Vector3 p) => places.Any(s =>
        {
            var c = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            return new Vector2(p.x - c.x, p.z - c.z).magnitude < Statecraft.Game.Map.Kits.SettlementKits.FootprintRadiusM(s, hexM) * 1.1f + 500f;
        });
        (Vector3, Vector3) At(int edge)
        {
            int j = edge * MapBuilder.OverlaySegmentsPerEdge + MapBuilder.OverlaySegmentsPerEdge / 2;
            var band = sel.TransformPoint((v[j] + v[n + j]) / 2f);
            var inward = new Vector3(centre.x - band.x, 0, centre.z - band.z).normalized;
            var land = new Vector3(band.x, 0, band.z) + inward * 1000f; land.y = map.Terrain.SampleHeight(land);
            return (band, land);
        }
        for (int k = 0; k < 6; k++) { var (b, l) = At((1 + k) % 6); if (!OnKitGround(l)) return (b, l); }
        return At(1);
    }

    /// <summary>Review I9: the selected hex still reads on graded land, at both promo poses, graded and ungraded.</summary>
    [UnityTest]
    public IEnumerator TheSelectedHexStillStandsOutOnGradedLand()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        VisualToggles.Set("tiltshift", false);   // the band is a thin line: measured sharp, as LightingTests measures detail
        // The ring against the land's graded colour, not against a grove: a row of crowns (100x trees, far taller than the
        // band's 30 m clearance) can stand on the sampled edge, and where a row ends moves whenever a town's lots change
        // which trees stand (towns round 2).
        VisualToggles.Set("trees", false);
        foreach (var pose in PromoPose.All)
        {
            pose.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
            var (band, land) = SelectNear(map, pose.Lon, pose.Lat);
            foreach (var graded in new[] { true, false })
            {
                VisualToggles.Set("grade", graded);
                var px = new Vector2[2];
                var s = RenderSample.Render(cam, 960, 540, new[] { band, land }, px);
                Color b = s.At((int)px[0].x, (int)px[0].y), l = s.Mean((int)px[1].x, (int)px[1].y, 1);
                float de = RenderSample.DeltaE(b, l);
                TestContext.WriteLine($"{pose.Name} graded={graded}: band {b}, land {l}, deltaE {de:0.0}");
                Assert.GreaterOrEqual(de, 20f, $"{pose.Name} graded={graded}");
            }
            VisualToggles.Set("grade", true);
        }
        map.Select(-9999, -9999);
    }

    /// <summary>Review I9: colour-blind players. Under simulated deuteranopia and protanopia (Machado 2009, severity 1), at
    /// the gameplay pose: sea against land, the selection against land and the selection against sea stay at deltaE76 >= 15.
    /// (The region borders' colours: ColourBlindPlayersTellTheBorderStylesApart.)</summary>
    [UnityTest]
    public IEnumerator ColourBlindPlayersStillTellCoastAndSelectionApart()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        VisualToggles.Set("tiltshift", false);   // the band is a thin line: measured sharp
        VisualToggles.Set("trees", false);       // the ring against the land, not a grove (TheSelectedHexStillStandsOutOnGradedLand)
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var (band, land) = SelectNear(map, 34.86, 32.00);
        var sea = map.Frame.ToWorld(34.66, 32.00, 0); sea.y = map.Terrain.SampleHeight(sea);
        var px = new Vector2[3];
        var s = RenderSample.Render(cam, 960, 540, new[] { band, land, sea }, px);
        Color b = s.At((int)px[0].x, (int)px[0].y), l = s.Mean((int)px[1].x, (int)px[1].y, 1), w = s.Mean((int)px[2].x, (int)px[2].y, 2);
        foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
        {
            Color B = RenderSample.SimulateCvd(b, kind), L = RenderSample.SimulateCvd(l, kind), W = RenderSample.SimulateCvd(w, kind);
            float seaLand = RenderSample.DeltaE(W, L), selLand = RenderSample.DeltaE(B, L), selSea = RenderSample.DeltaE(B, W);
            TestContext.WriteLine($"{kind}: sea-land {seaLand:0.0}, selection-land {selLand:0.0}, selection-sea {selSea:0.0}");
            Assert.GreaterOrEqual(seaLand, 15f, $"{kind}: the coast");
            Assert.GreaterOrEqual(selLand, 15f, $"{kind}: the selection on land");
            Assert.GreaterOrEqual(selSea, 15f, $"{kind}: the selection against the sea");
        }
        map.Select(-9999, -9999);
    }

    /// <summary>promo-borders: colour-blind players tell the two border styles apart and see each against the land, under
    /// simulated deuteranopia and protanopia (Machado 2009, severity 1), on two grounds at gameplay03's zoom: a proof edge on
    /// the olive coastal plain and one on the Negev's pale sand, five points along each. Asserted per point: the blue and
    /// red-orange stripes >= 15 apart; each band's stronger stripe >= 15 from the land (the bound the coast and the
    /// selection meet above); its two stripes, dark against light, >= 20 apart, so it reads as a line on any ground; and its
    /// weaker stripe >= 5 (review M5: a band must not quietly read as one stripe), unless the stronger one stands >= 25 from
    /// the land. Why not >= 15 for both stripes: a light stripe is meant to be light, and on the Negev's pale sand the
    /// promo's cream stripe melts into the ground for every player (normal vision logged alongside); the band then reads by
    /// its red-orange stripe, which the 25 bound holds well clear.</summary>
    [UnityTest]
    public IEnumerator ColourBlindPlayersTellTheBorderStylesApart()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        try
        {
            VisualToggles.Set("tiltshift", false);
            BorderProof.Show(map);
            foreach (var (biome, lon, lat) in new[] { ("coastal_plain", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat), ("desert", 34.80, 30.90) })
            {
                var (inner, outer) = BorderTests.ProofEdge(map, lon, lat, biome);
                BorderTests.LookAtEdge(map, rig, inner);
                foreach (var w in BorderTests.WalkEdge(map, cam, inner, outer, 5, 1920, 1080))
                {
                    var p = BorderTests.Read(w, -1, BorderTests.Blueness, 0.45f); var s = BorderTests.Read(w, +1, BorderTests.Redness);
                    TestContext.WriteLine($"{biome} normal vision: secondary colour-land {RenderSample.DeltaE(s.Stripe, s.Land):0.0}, light-land {RenderSample.DeltaE(s.Light, s.Land):0.0}; primary colour-land {RenderSample.DeltaE(p.Stripe, p.Land):0.0}, light-land {RenderSample.DeltaE(p.Light, p.Land):0.0}");
                    foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
                    {
                        Color Sim(Color c) => RenderSample.SimulateCvd(c, kind);
                        float styles = RenderSample.DeltaE(Sim(p.Stripe), Sim(s.Stripe));
                        float pColour = RenderSample.DeltaE(Sim(p.Stripe), Sim(p.Land)), pLight = RenderSample.DeltaE(Sim(p.Light), Sim(p.Land));
                        float sColour = RenderSample.DeltaE(Sim(s.Stripe), Sim(s.Land)), sLight = RenderSample.DeltaE(Sim(s.Light), Sim(s.Land));
                        float pPair = RenderSample.DeltaE(Sim(p.Stripe), Sim(p.Light)), sPair = RenderSample.DeltaE(Sim(s.Stripe), Sim(s.Light));
                        TestContext.WriteLine($"{biome} {kind}: blue-orange {styles:0.0}; primary colour-land {pColour:0.0}, light-land {pLight:0.0}, colour-light {pPair:0.0}; secondary colour-land {sColour:0.0}, light-land {sLight:0.0}, colour-light {sPair:0.0}");
                        string at = $"{biome} {kind}";
                        Assert.GreaterOrEqual(styles, 15f, $"{at}: the two styles");
                        Assert.GreaterOrEqual(Mathf.Max(pColour, pLight), 15f, $"{at}: the primary band on land");
                        Assert.GreaterOrEqual(Mathf.Max(sColour, sLight), 15f, $"{at}: the secondary band on land");
                        if (Mathf.Max(pColour, pLight) < 25f) Assert.GreaterOrEqual(Mathf.Min(pColour, pLight), 5f, $"{at}: the primary band's weaker stripe");
                        if (Mathf.Max(sColour, sLight) < 25f) Assert.GreaterOrEqual(Mathf.Min(sColour, sLight), 5f, $"{at}: the secondary band's weaker stripe");
                        Assert.GreaterOrEqual(pPair, 20f, $"{at}: the primary band's two stripes");
                        Assert.GreaterOrEqual(sPair, 20f, $"{at}: the secondary band's two stripes");
                    }
                }
            }
        }
        finally { MapShaderGlobals.PinWaterTime(null); }
    }

    /// <summary>Two-colour borders (owner ruling 2026-10-14): the grey Neutral band (contested, no-man's land, a demilitarized
    /// zone) reads apart from both sides' bands and against the land for colour-blind players, on the same two grounds and
    /// with the same bounds as ColourBlindPlayersTellTheBorderStylesApart: neutral against blue and against red-orange >= 15
    /// apart, the neutral band's stronger stripe >= 15 from the land, its two stripes >= 20 apart; and it is dashed, a cue
    /// that is not colour (review S9 of #555): some walks fall in a gap.</summary>
    [UnityTest]
    public IEnumerator ColourBlindPlayersTellTheNeutralBandFromBothSides()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        try
        {
            VisualToggles.Set("tiltshift", false);
            BorderProof.Show(map);
            // the grey stripe is the band's darkest pixel (its pale stripe the lightest, which Read takes separately)
            float Greyness(Color c) => -RenderSample.Luma(c);
            // the edges are found on the proof's checker before its styles are swapped below
            var edges = new[] { ("coastal_plain", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat), ("desert", 34.80, 30.90) }
                .Select(e => (e.Item1, BorderTests.ProofEdge(map, e.Item2, e.Item3, e.Item1))).ToList();
            int gaps = 0, drawn = 0;
            foreach (var (biome, (inner, outer)) in edges)
            {
                BorderTests.LookAtEdge(map, rig, inner);
                // the proof's primary blocks (inner side) against neutral blocks, then neutral against secondary
                MapBorders.Use(BorderTests.Blocks(map, primary => primary ? BorderStyle.Primary : BorderStyle.Neutral));
                var a = BorderTests.WalkEdge(map, cam, inner, outer, 5, 1920, 1080);
                MapBorders.Use(BorderTests.Blocks(map, primary => primary ? BorderStyle.Neutral : BorderStyle.Secondary));
                var b = BorderTests.WalkEdge(map, cam, inner, outer, 5, 1920, 1080);
                for (int i = 0; i < a.Count; i++)
                {
                    var p = BorderTests.Read(a[i], -1, BorderTests.Blueness, 0.45f); var n = BorderTests.Read(a[i], +1, Greyness, 0.02f);
                    var s = BorderTests.Read(b[i], +1, BorderTests.Redness);
                    if (n.WidthPx < 1f) { gaps++; continue; }   // a walk through a gap between the neutral band's dashes
                    drawn++;
                    TestContext.WriteLine($"{biome}: neutral stripe {n.Stripe} light {n.Light} land {n.Land}");
                    foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
                    {
                        Color Sim(Color c) => RenderSample.SimulateCvd(c, kind);
                        float np = RenderSample.DeltaE(Sim(n.Stripe), Sim(p.Stripe)), ns = RenderSample.DeltaE(Sim(n.Stripe), Sim(s.Stripe));
                        float nColour = RenderSample.DeltaE(Sim(n.Stripe), Sim(n.Land)), nLight = RenderSample.DeltaE(Sim(n.Light), Sim(n.Land));
                        float nPair = RenderSample.DeltaE(Sim(n.Stripe), Sim(n.Light));
                        TestContext.WriteLine($"{biome} {kind}: neutral-blue {np:0.0}, neutral-orange {ns:0.0}; neutral colour-land {nColour:0.0}, light-land {nLight:0.0}, colour-light {nPair:0.0}");
                        string at = $"{biome} {kind}";
                        Assert.GreaterOrEqual(np, 15f, $"{at}: neutral against the Israeli side's blue");
                        Assert.GreaterOrEqual(ns, 15f, $"{at}: neutral against the other side's red-orange");
                        Assert.GreaterOrEqual(Mathf.Max(nColour, nLight), 15f, $"{at}: the neutral band on land");
                        Assert.GreaterOrEqual(nPair, 20f, $"{at}: the neutral band's two stripes");
                    }
                }
            }
            TestContext.WriteLine($"neutral band: {drawn} walks on a dash, {gaps} in a gap");
            Assert.Greater(gaps, 0, "the neutral band is dashed (a cue that is not colour)");
            Assert.GreaterOrEqual(drawn, 4, "enough walks on the dashes to measure them");
        }
        finally { MapShaderGlobals.PinWaterTime(null); }
    }

    /// <summary>Review I9: with the Neutral tonemapper the sunlit desert does not clip: in the Negev (34.9 E, 30.8 N, 60 km)
    /// and at the gameplay pose, under 1% of pixels have any channel at the top 8-bit step. The control: with the grade's
    /// tonemapper set to None (which also overrides the pipeline profile's Neutral) the graded Negev does clip.</summary>
    [UnityTest]
    public IEnumerator TheSunlitDesertDoesNotClip()
    {
        var (map, rig, profile) = Open();
        yield return null;
        var cam = Camera.main;
        foreach (var (name, lon, lat, distance) in new[] { ("Negev", 34.9, 30.8, 60000f), ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance) })
        {
            rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var s = RenderSample.Render(cam, 480, 270);
            float clipped = s.FractionClipped(0.995f);
            TestContext.WriteLine($"{name}: clipped {clipped:P2}");
            Assert.Less(clipped, 0.01f, name);
        }
        Assert.IsTrue(profile.TryGet<Tonemapping>(out var tm));
        rig.LookAt = map.Frame.ToWorld(34.9, 30.8, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        tm.mode.Override(TonemappingMode.None);
        float control = RenderSample.Render(cam, 480, 270).FractionClipped(0.995f);
        tm.mode.Override(MapGrade.Tonemapper);
        TestContext.WriteLine($"Negev without a tonemapper (control): clipped {control:P2}");
        Assert.Greater(control, 0.01f, "without the tonemapper the graded desert clips");
    }

    /// <summary>Rendered at the gameplay pose: the grade neither washes the colour out nor pumps it. Since the merge point
    /// MapGrade.Saturation has stayed near 0 (merged diorama02 was over the promo's saturation at 16; issue #38 set -7 to
    /// take back most of what Contrast 18 adds), so the grade's effect on saturation is mostly the side effect of contrast,
    /// white balance and split toning: measured 0.568 -> 0.598 (x1.053) at the merge point, 0.399 -> 0.399 (x1.000) since #38.
    /// The ceiling catches a return of the saturation lever (about +0.7% of frame saturation per unit here, so x1.12 is
    /// about Saturation 10); the floor catches a milky grade.</summary>
    [UnityTest]
    public IEnumerator TheGradeKeepsSaturationNearTheUngradedFrame()
    {
        var (map, rig, _) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var cam = Camera.main;
        VisualToggles.Set("grade", false); var flat = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("grade", true); var graded = RenderSample.Render(cam, 480, 270);
        float ratio = graded.MeanSaturation() / flat.MeanSaturation();
        TestContext.WriteLine($"saturation {flat.MeanSaturation():0.000} -> {graded.MeanSaturation():0.000} (x{ratio:0.000})");
        Assert.That(ratio, Is.InRange(0.98f, 1.12f));
    }

    /// <summary>Rendered: MapGrade's vignette darkens the corners relative to the centre more than the vignette it replaces
    /// (the pipeline's template profile, Settings/SampleSceneProfile, applies a 0.2 black one, which "plain" still shows).</summary>
    [UnityTest]
    public IEnumerator TheVignetteDarkensTheCorners()
    {
        var (map, rig, profile) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var cam = Camera.main;
        profile.TryGet<Vignette>(out var v);
        v.active = false; var plain = RenderSample.Render(cam, 480, 270);
        v.active = true; var vignetted = RenderSample.Render(cam, 480, 270);
        float Ratio(RenderSample s) => s.MeanLuma(0f, 0f, 0.1f, 0.1f) / s.MeanLuma(0.45f, 0.45f, 0.55f, 0.55f);
        TestContext.WriteLine($"corner/centre {Ratio(plain):0.000} -> {Ratio(vignetted):0.000}");
        Assert.Less(Ratio(vignetted), Ratio(plain) * 0.9f);
    }
}
