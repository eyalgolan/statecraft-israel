#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The Civ-like border prototype (#196, owner 2026-10-26): its variants' rules (MapBorders.BandWidths, TintAlpha,
/// the neutral dashes), which StatecraftBorders.hlsl mirrors, rendered checks of each variant, and the switch. Legacy (main's
/// look) stays the default, so BorderTests, GradeTests and SubdistrictTests run unchanged on it (review C320).</summary>
public class BorderCivTests
{
    const float Inradius = 4330.127f;   // 5 km hexes
    static readonly float[] MetresPerPx = { 2f, 12f, 35f, 87f, 146f, 300f, 600f, 2000f };
    static readonly BorderVariant[] Prototypes = { BorderVariant.A, BorderVariant.B, BorderVariant.C, BorderVariant.A2, BorderVariant.B2, BorderVariant.C2 };
    static MapBorders.Style[] Styles => new[] { MapBorders.Primary, MapBorders.Secondary, MapBorders.Neutral, MapBorders.PrimaryStrongBlue };

    [TearDown]
    public void Reset()
    {
        MapBorders.Variant = BorderVariant.Legacy;
        MapBorders.NoEarlyOut = false;
        MapShaderGlobals.PinWaterTime(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    static float Median(IEnumerable<float> v) { var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }

    /// <summary>The variant's Primary style and colour floor, as SetStyles installs them.</summary>
    static (MapBorders.Style style, float minColour) PrimaryOf(BorderVariant v) =>
        (MapBorders.StrongBlue(v) ? MapBorders.PrimaryStrongBlue : MapBorders.Primary, MapBorders.Strong(v) ? MapBorders.StrongMinColourPx : MapBorders.MinColourPx);

    // ---- The rules (MapBorders' mirror of the shader) ----

    /// <summary>Minimum widths: colour stripe >= 3 px (6 on the stronger tier), light >= 1 px, wash >= 12 px at every zoom,
    /// unless rim, band and wash would pass half the inradius: then the wash gives way first, then the stripes, the rim
    /// last. Up close the world widths rule (the band grows towards the camera).</summary>
    [Test]
    public void BandWidthsKeepTheirPixelFloorsUnderTheCap()
    {
        foreach (var minColour in new[] { MapBorders.MinColourPx, MapBorders.StrongMinColourPx })
            foreach (var rim in new[] { false, true })
                foreach (var s in Styles)
                    foreach (var mpp in MetresPerPx)
                    {
                        var w = MapBorders.BandWidths(s, mpp, Inradius, rim, minColour);
                        float widest = MapBorders.MaxShareOfInradius * Inradius / mpp, room = widest - w.rim;
                        string at = $"{mpp} m/px, band {s.BandM} m, rim {rim}, floor {minColour}";
                        Assert.LessOrEqual(w.rim + w.colour + w.light + w.wash, widest + 1e-3f, $"{at}: never past the cap, rim included");
                        if (rim) Assert.AreEqual(Mathf.Min(Mathf.Max(MapBorders.RimM / mpp, MapBorders.RimMinPx), widest), w.rim, 1e-4f, at);
                        else Assert.AreEqual(0f, w.rim, at);
                        if (minColour + MapBorders.MinLightPx + MapBorders.MinWashPx <= room)
                        {
                            Assert.GreaterOrEqual(w.colour, minColour - 1e-4f, at);
                            Assert.GreaterOrEqual(w.light, MapBorders.MinLightPx - 1e-4f, at);
                            Assert.GreaterOrEqual(w.wash, MapBorders.MinWashPx - 1e-4f, at);
                        }
                        else if (minColour + MapBorders.MinLightPx <= room)
                        {
                            Assert.GreaterOrEqual(w.colour, minColour - 1e-4f, $"{at}: the stripes keep their floors");
                            Assert.AreEqual(room - w.colour - w.light, w.wash, 1e-3f, $"{at}: the wash gives way first");
                        }
                        else Assert.AreEqual(0f, w.wash, $"{at}: the wash goes before the stripes shrink");
                    }
        var close = MapBorders.BandWidths(MapBorders.Primary, 2f, Inradius, rim: false);
        Assert.AreEqual(230f * 0.5f / 2f, close.colour, 1e-3f, "up close the world width rules");
        Assert.AreEqual(MapBorders.WashReachShare * Inradius / 2f, close.colour + close.light + close.wash, 1e-2f, "the wash reaches a quarter of the inradius");
        Assert.AreEqual(MapBorders.RimMinPx, MapBorders.BandWidths(MapBorders.Primary, 300f, Inradius, rim: true).rim, "the rim keeps 1 px");
        Assert.AreEqual(MapBorders.RimM / 5f, MapBorders.BandWidths(MapBorders.Primary, 5f, Inradius, rim: true).rim, 1e-4f, "and 50 m up close");
    }

    /// <summary>Equal wash: both sides (and variant C's blue) wash alike, every style reaches the same quarter of the inradius.</summary>
    [Test]
    public void BothSidesWashAlike()
    {
        Assert.AreEqual(MapBorders.Primary.WashStrength, MapBorders.Secondary.WashStrength);
        Assert.AreEqual(MapBorders.Primary.WashStrength, MapBorders.PrimaryStrongBlue.WashStrength);
        Assert.That(MapBorders.Primary.WashStrength, Is.InRange(0.35f, 0.40f));
        foreach (var s in Styles) Assert.AreEqual(MapBorders.WashReachShare, s.WashReach);
        Assert.AreEqual(0.25f, MapBorders.WashReachShare);
    }

    /// <summary>The territory tint: 0.08 (0.12 on the stronger tier) while a hex is large on screen, rising to 0.25 at the
    /// overview.</summary>
    [Test]
    public void TheTintRisesAsTheHexShrinks()
    {
        Assert.AreEqual(0.08f, MapBorders.TintAlpha(400f), 1e-6f);
        Assert.AreEqual(0.08f, MapBorders.TintAlpha(120f), 1e-6f);
        Assert.AreEqual(0.165f, MapBorders.TintAlpha(80f), 1e-6f);
        Assert.AreEqual(0.25f, MapBorders.TintAlpha(40f), 1e-6f);
        Assert.AreEqual(0.25f, MapBorders.TintAlpha(8f), 1e-6f);
        for (float px = 8f; px < 400f; px += 1f) Assert.LessOrEqual(MapBorders.TintAlpha(px + 1f), MapBorders.TintAlpha(px));
        Assert.AreEqual(0.12f, MapBorders.TintAlpha(400f, MapBorders.StrongTintNear), 1e-6f, "the stronger tier up close");
        Assert.AreEqual(0.25f, MapBorders.TintAlpha(40f, MapBorders.StrongTintNear), 1e-6f);
    }

    /// <summary>The neutral dashes stay at least 8 px on screen: their period doubles from 2 km as the camera pulls out
    /// (doubling keeps every other dash in place).</summary>
    [Test]
    public void NeutralDashesStayAtLeastEightPixels()
    {
        Assert.AreEqual(MapBorders.NeutralDashM, MapBorders.NeutralDashPeriodM(10f), "up close: the 2 km period");
        for (float mpp = 1f; mpp < 2000f; mpp *= 1.13f)
        {
            float period = MapBorders.NeutralDashPeriodM(mpp);
            Assert.GreaterOrEqual(period * MapBorders.NeutralDashOn / mpp, MapBorders.NeutralDashMinPx - 1e-3f, $"{mpp} m/px");
            float doublings = Mathf.Log(period / MapBorders.NeutralDashM, 2f);
            Assert.AreEqual(Mathf.Round(doublings), doublings, 1e-4f, $"{mpp} m/px: a power-of-two multiple of 2 km");
        }
    }

    /// <summary>Review C320: with no selection (or a bad one) the map draws Legacy, main's look; a URL parameter or a
    /// command-line flag picks a variant, and the switch reaches the shader. Every transition installs its whole set of
    /// globals (B2 to C2 drops the rim, C2 to A drops the blue, back to Legacy restores main's).</summary>
    [Test]
    public void TheVariantsSwitchAndLegacyIsTheDefault()
    {
        Assert.AreEqual(BorderVariant.Legacy, MapBorders.VariantRequested("https://x.invalid/?golden", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.Legacy, MapBorders.VariantRequested("https://x.invalid/?bordervariant=bx", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.Legacy, MapBorders.VariantRequested(null, new[] { "Statecraft", "-bordervariant", "zz" }, webgl: false));
        Assert.AreEqual(BorderVariant.Legacy, MapBorders.VariantRequested(null, new[] { "Statecraft" }, webgl: false));
        Assert.AreEqual(BorderVariant.A, MapBorders.VariantRequested("https://x.invalid/?bordervariant=a", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.B, MapBorders.VariantRequested("https://x.invalid/?golden&bordervariant=b", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.C, MapBorders.VariantRequested("https://x.invalid/?bordervariant=C#x", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.A2, MapBorders.VariantRequested("https://x.invalid/?bordervariant=a2", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.B, MapBorders.VariantRequested(null, new[] { "Statecraft", "-bordervariant", "b" }, webgl: false));
        Assert.AreEqual(BorderVariant.C2, MapBorders.VariantRequested(null, new[] { "Statecraft", "-bordervariant", "C2" }, webgl: false));

        BorderTests.Open();
        Vector4 G(string name) => Shader.GetGlobalVector(name);
        var blue = MapBorders.Primary.Colour.linear; var strong = MapBorders.PrimaryStrongBlue.Colour.linear;
        void Expect(BorderVariant v)
        {
            MapBorders.Variant = v;
            string at = v.ToString();
            if (v == BorderVariant.Legacy)
            {
                Assert.AreEqual(1f, G("_ScBorderMin").w, at);
                Assert.AreEqual(MapBorders.LegacyMinBandPx, G("_ScBorders").x, at);
                Assert.AreEqual(new Vector4(230f, 0.5f, 0.9f, 0.40f), G("_ScBorderShape1"), at);
                Assert.AreEqual(new Vector4(200f, 0.6f, 1.0f, 0.18f), G("_ScBorderShape2"), at);
                Assert.AreEqual(new Vector4(200f, 0.6f, 0.8f, 0.14f), G("_ScBorderShape3"), at);
                Assert.AreEqual(0f, G("_ScBorderRim").z, at);
                Assert.AreEqual(new Vector4(blue.r, blue.g, blue.b, 1f), G("_ScBorderColour1"), at);
                return;
            }
            Assert.AreEqual(0f, G("_ScBorderMin").w, at);
            Assert.AreEqual(MapBorders.Strong(v) ? MapBorders.StrongMinColourPx : MapBorders.MinColourPx, G("_ScBorders").x, at);
            float wash = MapBorders.Strong(v) ? MapBorders.StrongWashStrength : MapBorders.SideWashStrength;
            Assert.AreEqual(wash, G("_ScBorderShape1").w, at); Assert.AreEqual(wash, G("_ScBorderShape2").w, at);
            Assert.AreEqual(MapBorders.Strong(v) ? 1f : 0f, G("_ScBorderMin").z, at);
            Assert.AreEqual(MapBorders.Strong(v) ? MapBorders.StrongTintNear : MapBorders.TintNear, G("_ScBorderTint").x, at);
            Assert.AreEqual(MapBorders.HasRim(v) ? MapBorders.RimOpacity : 0f, G("_ScBorderRim").z, at);
            var c = MapBorders.StrongBlue(v) ? strong : blue;
            Assert.AreEqual(new Vector4(c.r, c.g, c.b, 1f), G("_ScBorderColour1"), at);
        }
        foreach (var v in new[] { BorderVariant.Legacy, BorderVariant.A, BorderVariant.B, BorderVariant.C, BorderVariant.A2, BorderVariant.B2,
                     BorderVariant.C2, BorderVariant.B2, BorderVariant.A, BorderVariant.C, BorderVariant.Legacy })
            Expect(v);
    }

    // ---- Rendered: the shader against its mirror, for every variant ----

    /// <summary>Review C320: each prototype variant's rendered Primary band (colour and light stripes) is the width
    /// MapBorders.BandWidths predicts for the metres per pixel measured along the walk, at gameplay03 and at 150 km (where
    /// the pixel floors bind): no narrower than 0.5 px under it, and no more than 2 px over it (the band's two box-filtered
    /// end pixels count whole when partly covered; measured +1.3 to +1.7 px). A shader that drifted from its C# mirror
    /// fails here.</summary>
    [UnityTest]
    public IEnumerator EveryVariantDrawsTheWidthsItsMirrorPredicts()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        foreach (var distance in new[] { PromoPose.Gameplay03.Distance, 150000f })
        {
            BorderTests.LookAtEdge(map, rig, inner);
            rig.Distance = distance; rig.Apply();
            foreach (var v in Prototypes)
            {
                MapBorders.Variant = v;
                var (style, minColour) = PrimaryOf(v);
                var walks = BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080);
                // metres per pixel along each walk, from its first and last distinct pixel
                float mpp = Median(walks.Select(w => (w.SignedM[w.SignedM.Count - 1] - w.SignedM[0]) / Vector2.Distance(w.Px[w.Px.Count - 1], w.Px[0])));
                var want = MapBorders.BandWidths(style, mpp, Inradius, MapBorders.HasRim(v), minColour);
                float gain = MapBorders.StrongBlue(v) ? 0.6f : 0.45f;   // above each blue's wash
                float got = Median(walks.Select(w => BorderTests.Read(w, -1, BorderTests.Blueness, gain).WidthPx));
                TestContext.WriteLine($"{v} at {distance / 1000f:0} km ({mpp:0.0} m/px): band {got:0.0} px, mirror {want.colour + want.light:0.0} px (colour {want.colour:0.0}, light {want.light:0.0})");
                Assert.That(got - (want.colour + want.light), Is.InRange(-0.5f, 2f), $"{v} at {distance / 1000f:0} km");
            }
        }
    }

    /// <summary>Review M6 for every prototype variant: the early-out is exact (BorderTests.TheEarlyOutIsExact, run with
    /// each variant drawn; that test itself runs on Legacy).</summary>
    [UnityTest]
    public IEnumerator TheEarlyOutIsExactInEveryVariant()
    {
        foreach (var v in Prototypes)
        {
            TestContext.WriteLine($"variant {v}");
            MapBorders.Variant = v;
            var run = new BorderTests().TheEarlyOutIsExact();   // stepped here: its frames are this test's
            while (run.MoveNext()) yield return run.Current;
        }
    }

    /// <summary>The colour-blind checks (GradeTests, unchanged bounds) for every prototype variant; the neutral stripe is
    /// read above the variants' grey wash and tint (0.10, Legacy's 0.02). Each variant asserts its own dash gaps and walks.</summary>
    [UnityTest]
    public IEnumerator ColourBlindPlayersTellTheStylesApartInEveryVariant()
    {
        foreach (var v in Prototypes)
        {
            TestContext.WriteLine($"variant {v}");
            MapBorders.Variant = v;
            var styles = new GradeTests().ColourBlindPlayersTellTheBorderStylesApart();   // stepped here: its frames are this test's
            while (styles.MoveNext()) yield return styles.Current;
            MapBorders.Variant = v;
            var neutral = GradeTests.NeutralBandApart(0.10f);
            while (neutral.MoveNext()) yield return neutral.Current;
        }
    }

    // ---- Rendered: the new rules (A; red on main where noted, measured on c7851de3 before Legacy kept main's look) ----

    /// <summary>Far off a band holds its weight instead of fading to a hairline. At the overview's distance on the proof's
    /// coastal-plain edge by gameplay03, the Primary side still shows its light stripe and its band, wash and tint change
    /// several pixels within 1.5 km of the edge (main: 3 px changed, light lift 0.000).</summary>
    [UnityTest]
    public IEnumerator AtTheOverviewABandKeepsItsLightStripeAndWash()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        MapBorders.Variant = BorderVariant.A;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        BorderTests.LookAtEdge(map, rig, inner);
        rig.Distance = PromoPose.Diorama02.Distance; rig.Apply();
        var changed = new List<float>(); var lift = new List<float>();
        foreach (var w in BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080))
        {
            int n = 0; float l = float.MinValue;
            for (int k = 0; k < w.On.Count; k++)
            {
                if (w.SignedM[k] >= 0f) continue;
                if (RenderSample.DeltaE(w.On[k], w.Off[k]) >= 3f) n++;
                l = Mathf.Max(l, RenderSample.Luma(w.On[k]) - RenderSample.Luma(w.Off[k]));
            }
            changed.Add(n); lift.Add(l);
        }
        float c = Median(changed), li = Median(lift);
        TestContext.WriteLine($"overview distance, edge {inner}-{outer}: median {c:0} primary-side pixels changed within 1.5 km, light stripe lift {li:0.000}");
        Assert.GreaterOrEqual(c, 7f, "far off, band, wash and tint keep their weight");
        Assert.GreaterOrEqual(li, 0.05f, "far off, the light stripe still shows");
    }

    /// <summary>Both sides' territory is washed alike: at gameplay03, 500-900 m inside each side of a primary-secondary
    /// edge, the wash's opacity (each pixel's step from the land towards its side's stripe colour) is within 25% on the two
    /// sides (main: nothing that far in; the Secondary's wash was 0.18 against the Primary's 0.40).</summary>
    [UnityTest]
    public IEnumerator BothSidesWashTheirTerritoryAlike()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        MapBorders.Variant = BorderVariant.A;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        BorderTests.LookAtEdge(map, rig, inner);
        var p = new List<float>(); var s = new List<float>();
        foreach (var w in BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080))
        {
            var pc = BorderTests.Read(w, -1, BorderTests.Blueness, 0.45f).Stripe; var sc = BorderTests.Read(w, +1, BorderTests.Redness, 0.2f).Stripe;
            for (int k = 0; k < w.On.Count; k++)
            {
                float m = Mathf.Abs(w.SignedM[k]);
                if (m < 500f || m > 900f) continue;
                // opacity: the projection of on - off onto stripe - off
                var colour = w.SignedM[k] < 0f ? pc : sc;
                Vector3 d = new Vector3(w.On[k].r - w.Off[k].r, w.On[k].g - w.Off[k].g, w.On[k].b - w.Off[k].b);
                Vector3 to = new Vector3(colour.r - w.Off[k].r, colour.g - w.Off[k].g, colour.b - w.Off[k].b);
                if (to.sqrMagnitude < 1e-3f) continue;
                (w.SignedM[k] < 0f ? p : s).Add(Vector3.Dot(d, to) / to.sqrMagnitude);
            }
        }
        float pm = Median(p), sm = Median(s);
        TestContext.WriteLine($"gameplay03 500-900 m in: wash opacity primary {pm:0.000} ({p.Count} px), secondary {sm:0.000} ({s.Count} px)");
        Assert.GreaterOrEqual(Mathf.Min(pm, sm), 0.05f, "both sides washed 500-900 m in");
        Assert.GreaterOrEqual(Mathf.Min(pm, sm) / Mathf.Max(pm, sm), 0.75f, "the two sides' wash within 25%");
    }

    /// <summary>The pixel floors: at 150 km, where the Primary band's 230 m is under 3 px, its colour and light stripes
    /// together still measure at least 3.5 px (main: 3.0 px, one 2 px floor for the whole band).</summary>
    [UnityTest]
    public IEnumerator AtMidZoomTheBandKeepsItsPixelFloors()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        MapBorders.Variant = BorderVariant.A;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        BorderTests.LookAtEdge(map, rig, inner);
        rig.Distance = 150000f; rig.Apply();
        float pm = Median(BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080).Select(w => BorderTests.Read(w, -1, BorderTests.Blueness, 0.45f).WidthPx));
        TestContext.WriteLine($"150 km, edge {inner}-{outer}: primary band {pm:0.0} px (median)");
        Assert.GreaterOrEqual(pm, 3.5f, "colour stripe >= 3 px plus light stripe >= 1 px");
    }

    /// <summary>At the overview (diorama02) the territory reads as tinted with a firm edge: the proof changes much of the
    /// land, but under a light tint, and its primary pixels read blue with few near-white (the light stripe is 1 px).</summary>
    [UnityTest]
    public IEnumerator AtTheOverviewTerritoryReadsTintedWithAFirmEdge()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        MapBorders.Variant = BorderVariant.A;
        RenderSample.TiltShift(false);
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set(MapBorders.ToggleName, false);
        var off = RenderSample.Render(cam, 1920, 1080);
        VisualToggles.Set(MapBorders.ToggleName, true);
        BorderProof.Show(map);
        var on = RenderSample.Render(cam, 1920, 1080);
        float share = BorderTests.Changed(on, off, 0.06f), strong = BorderTests.Changed(on, off, 0.25f);
        int changed = 0, white = 0; float gain = 0f;
        for (int i = 0; i < on.Pixels.Length; i++)
        {
            Color a = on.Pixels[i], b = off.Pixels[i];
            if (Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))) <= 0.06f || BorderTests.Blueness(a) <= BorderTests.Blueness(b)) continue;
            changed++; gain += BorderTests.Blueness(a) - BorderTests.Blueness(b);
            if (Mathf.Min(a.r, Mathf.Min(a.g, a.b)) > 0.80f) white++;
        }
        float whiteShare = white / (float)Mathf.Max(changed, 1), meanGain = gain / Mathf.Max(changed, 1);
        TestContext.WriteLine($"diorama02: {share:P2} changed (> 0.06), {strong:P2} strongly (> 0.25); {whiteShare:P1} of bluer pixels near-white, mean blueness gain {meanGain:0.000}");
        Assert.That(share, Is.InRange(0.15f, 0.60f), "the territory reads tinted");
        Assert.Less(strong, 0.10f, "a light tint, not an opaque fill");
        Assert.Less(whiteShare, 0.08f, "far bands read as the region's colour");
        Assert.Greater(meanGain, 0.10f, "far primary territory reads blue");
    }

    /// <summary>No band along a hex in no region (the coast): with the tint, the land within 300 m of the coast edge changes
    /// no more than the same hex 1-1.5 km in (+2 ΔE), and no sea pixel changes.</summary>
    [UnityTest]
    public IEnumerator NoBandRunsAlongTheCoastOnlyTheTint()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        MapBorders.Variant = BorderVariant.A;
        RenderSample.TiltShift(false);
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var layer = BorderProof.Show(map);
        var centre = BorderProof.HexAt(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat);
        var byHex = map.Hexes.hexes.ToDictionary(h => (h.q, h.r));
        (int q, int r)? land = null;
        foreach (var h in map.Hexes.hexes.OrderBy(h => BorderProof.Distance((h.q, h.r), centre)))
        {
            if (!h.land || !byHex.TryGetValue((h.q - 1, h.r), out var west) || west.land) continue;
            int id = layer.RegionOf(h.q, h.r);
            if (MapBorders.NeighbourSteps.Any(d => { int o = layer.RegionOf(h.q + d.dq, h.r + d.dr); return o != 0 && o != id; })) continue;
            land = (h.q, h.r); break;
        }
        Assert.IsNotNull(land);
        int moved = 0, sea = 0; var near = new List<float>(); var deep = new List<float>();
        foreach (var w in BorderTests.WalkEdge(map, cam, land!.Value, (land.Value.q - 1, land.Value.r), 3, 960, 540))
            for (int k = 0; k < w.On.Count; k++)
            {
                float d = RenderSample.DeltaE(w.On[k], w.Off[k]), m = w.SignedM[k];
                if (m > 0f) { sea++; if (d >= 2f) moved++; }
                else if (m > -300f) near.Add(d);
                else if (m < -1000f) deep.Add(d);
            }
        float nm = Median(near), dm = Median(deep);
        TestContext.WriteLine($"coast edge at {land.Value}: {moved}/{sea} sea pixels moved; land ΔE near the edge {nm:0.0}, deep in {dm:0.0}");
        Assert.AreEqual(0, moved, "the sea is untouched");
        Assert.LessOrEqual(nm, dm + 2f, "no band along the coast: only the tint the whole hex has");
    }

    /// <summary>Review C320: a shared neutral edge's dashes line up on both sides (one dash phase per edge, not per hex).
    /// Across the proof's blocks drawn all Neutral (every block boundary a shared neutral edge), at 11 points along an edge
    /// the two sides within 80 m of it are both on a dash or both in a gap at least 9 times.</summary>
    [UnityTest]
    public IEnumerator SharedNeutralDashesLineUpAcrossTheEdge()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        MapBorders.Variant = BorderVariant.A;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        MapBorders.Use(BorderTests.Blocks(map, _ => BorderStyle.Neutral));
        BorderTests.LookAtEdge(map, rig, inner);
        rig.Distance = 20000f; rig.Apply();
        int agree = 0, total = 0, onDash = 0;
        foreach (var w in BorderTests.WalkEdge(map, cam, inner, outer, 11, 1920, 1080))
        {
            bool Drawn(int sign) => Enumerable.Range(0, w.On.Count).Any(k => Mathf.Sign(w.SignedM[k]) == sign && Mathf.Abs(w.SignedM[k]) <= 80f
                && RenderSample.DeltaE(w.On[k], w.Off[k]) >= 10f);   // the grey stripe, well above the neutral wash and tint
            bool a = Drawn(-1), b = Drawn(+1);
            total++; if (a == b) agree++; if (a) onDash++;
        }
        TestContext.WriteLine($"shared neutral edge {inner}-{outer}: {agree}/{total} walks agree across the edge, {onDash} on a dash");
        Assert.GreaterOrEqual(agree, 9, "the two halves' dashes line up");
        Assert.That(onDash, Is.InRange(1, total - 1), "the walks see both dashes and gaps");
    }

    /// <summary>The stronger tier at gameplay03 (the view players live in): A2's Primary band is wider than A's (the 6 px
    /// colour floor) and its wash reads farther in (700-1000 m: the held wash and the 0.12 tint).</summary>
    [UnityTest]
    public IEnumerator TheStrongerTierReadsHeavierAtGameplay03()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        BorderTests.LookAtEdge(map, rig, inner);
        (float band, float wash) Measure(BorderVariant v)
        {
            MapBorders.Variant = v;
            var walks = BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080);
            float band = Median(walks.Select(w => BorderTests.Read(w, -1, BorderTests.Blueness, 0.45f).WidthPx));
            float wash = Median(walks.SelectMany(w => Enumerable.Range(0, w.On.Count).Where(k => w.SignedM[k] <= -700f && w.SignedM[k] >= -1000f)
                .Select(k => RenderSample.DeltaE(w.On[k], w.Off[k]))));
            return (band, wash);
        }
        var a = Measure(BorderVariant.A); var a2 = Measure(BorderVariant.A2);
        TestContext.WriteLine($"gameplay03: primary band A {a.band:0.0} px, A2 {a2.band:0.0} px; wash ΔE 700-1000 m in: A {a.wash:0.0}, A2 {a2.wash:0.0}");
        Assert.GreaterOrEqual(a2.band, a.band + 1f, "A2's band is wider");
        Assert.Greater(a2.wash, a.wash * 1.3f, "A2's wash reads farther in");
    }

    /// <summary>Variant B: a dark rim on the edge. Close up (20 km), the darkest Primary pixel within 60 m of a
    /// primary-secondary proof edge is clearly darker with B than with A (and with B2 than with A2).</summary>
    [UnityTest]
    public IEnumerator VariantBDrawsADarkRimOnTheEdge()
    {
        var (map, cam, rig) = BorderTests.Open();
        yield return null;
        RenderSample.TiltShift(false);
        BorderProof.Show(map);
        var (inner, outer) = BorderTests.ProofEdge(map, PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, "coastal_plain");
        BorderTests.LookAtEdge(map, rig, inner);
        rig.Distance = 20000f; rig.Apply();
        float Darkest(BorderVariant v)
        {
            MapBorders.Variant = v;
            return BorderTests.WalkEdge(map, cam, inner, outer, 5, 1920, 1080)
                .Select(w => Enumerable.Range(0, w.On.Count).Where(k => w.SignedM[k] < 0f && w.SignedM[k] > -60f).Select(k => RenderSample.Luma(w.On[k])).DefaultIfEmpty(1f).Min())
                .OrderBy(x => x).ElementAt(2);
        }
        float a = Darkest(BorderVariant.A), b = Darkest(BorderVariant.B), a2 = Darkest(BorderVariant.A2), b2 = Darkest(BorderVariant.B2);
        TestContext.WriteLine($"20 km: darkest primary pixel within 60 m of the edge: A {a:0.000}, B {b:0.000}; A2 {a2:0.000}, B2 {b2:0.000}");
        Assert.Less(b, a - 0.05f, "B's rim darkens the edge");
        Assert.Less(b2, a2 - 0.05f, "B2's rim darkens the edge");
    }
}
