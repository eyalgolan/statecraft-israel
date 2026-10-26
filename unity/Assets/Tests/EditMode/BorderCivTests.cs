#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The Civ-like border prototype (#196, owner 2026-10-26): the rules of MapBorders.BandWidths, TintAlpha and the
/// neutral dashes, which StatecraftBorders.hlsl mirrors (BorderTests renders them), and the variants' switch.</summary>
public class BorderCivTests
{
    const float Inradius = 4330.127f;   // 5 km hexes
    static readonly float[] MetresPerPx = { 2f, 12f, 35f, 87f, 146f, 300f, 600f };
    static MapBorders.Style[] Styles => new[] { MapBorders.Primary, MapBorders.Secondary, MapBorders.Neutral, MapBorders.PrimaryStrongBlue };

    [TearDown]
    public void Reset()
    {
        MapBorders.Variant = BorderVariant.A;
        MapShaderGlobals.PinWaterTime(null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }

    /// <summary>Minimum widths: colour stripe >= 3 px, light >= 1 px, wash >= 12 px at every zoom, unless band plus wash
    /// would pass half the inradius: then the wash gives way first, and only then the stripes. Up close the world widths
    /// rule (the band grows towards the camera).</summary>
    [Test]
    public void BandWidthsKeepTheirPixelFloorsUnderTheCap()
    {
        foreach (var s in Styles)
            foreach (var mpp in MetresPerPx)
            {
                var (rim, colour, light, wash) = MapBorders.BandWidths(s, mpp, Inradius, rim: false);
                float widest = MapBorders.MaxShareOfInradius * Inradius / mpp;
                string at = $"{mpp} m/px, band {s.BandM} m";
                Assert.AreEqual(0f, rim, at);
                Assert.LessOrEqual(colour + light + wash, widest + 1e-3f, $"{at}: never past the cap");
                if (MapBorders.MinColourPx + MapBorders.MinLightPx + MapBorders.MinWashPx <= widest)
                {
                    Assert.GreaterOrEqual(colour, MapBorders.MinColourPx - 1e-4f, at);
                    Assert.GreaterOrEqual(light, MapBorders.MinLightPx - 1e-4f, at);
                    Assert.GreaterOrEqual(wash, MapBorders.MinWashPx - 1e-4f, at);
                }
                else if (MapBorders.MinColourPx + MapBorders.MinLightPx <= widest)
                {
                    Assert.GreaterOrEqual(colour, MapBorders.MinColourPx - 1e-4f, $"{at}: the stripes keep their floors");
                    Assert.AreEqual(widest - colour - light, wash, 1e-3f, $"{at}: the wash gives way first");
                }
                else Assert.AreEqual(0f, wash, $"{at}: the wash goes before the stripes shrink");
            }
        // The stronger tier's colour floor (A2, B2, C2): 6 px wherever the cap leaves room.
        foreach (var mpp in MetresPerPx)
        {
            var (_, colour, _, _) = MapBorders.BandWidths(MapBorders.Primary, mpp, Inradius, rim: false, MapBorders.StrongMinColourPx);
            if (MapBorders.StrongMinColourPx + MapBorders.MinLightPx <= MapBorders.MaxShareOfInradius * Inradius / mpp)
                Assert.GreaterOrEqual(colour, MapBorders.StrongMinColourPx - 1e-4f, $"A2 at {mpp} m/px");
        }
        var close = MapBorders.BandWidths(MapBorders.Primary, 2f, Inradius, rim: false);
        Assert.AreEqual(230f * 0.5f / 2f, close.colour, 1e-3f, "up close the world width rules");
        Assert.AreEqual(MapBorders.WashReachShare * Inradius / 2f, close.colour + close.light + close.wash, 1e-2f, "the wash reaches a quarter of the inradius");
    }

    /// <summary>Variant B's rim is at least 1 px, and 50 m up close.</summary>
    [Test]
    public void TheRimKeepsItsPixelFloor()
    {
        Assert.AreEqual(MapBorders.RimMinPx, MapBorders.BandWidths(MapBorders.Primary, 300f, Inradius, rim: true).rim);
        Assert.AreEqual(MapBorders.RimM / 5f, MapBorders.BandWidths(MapBorders.Primary, 5f, Inradius, rim: true).rim, 1e-4f);
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

    /// <summary>The territory tint: 0.08 while a hex is large on screen, rising to 0.25 at the overview.</summary>
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

    /// <summary>The variants switch by a URL parameter or a command-line flag (A by default), and the switch reaches the shader:
    /// B turns the rim on, C swaps the Primary colour; A and C draw no rim.</summary>
    [Test]
    public void TheVariantsSwitchWithoutCodeCopies()
    {
        Assert.AreEqual(BorderVariant.A, MapBorders.VariantRequested("https://x.invalid/?golden", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.B, MapBorders.VariantRequested("https://x.invalid/?golden&bordervariant=b", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.C, MapBorders.VariantRequested("https://x.invalid/?bordervariant=C#x", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.A, MapBorders.VariantRequested("https://x.invalid/?bordervariant=bx", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.B, MapBorders.VariantRequested(null, new[] { "Statecraft", "-bordervariant", "b" }, webgl: false));
        Assert.AreEqual(BorderVariant.A2, MapBorders.VariantRequested("https://x.invalid/?bordervariant=a2", new string[0], webgl: true));
        Assert.AreEqual(BorderVariant.C2, MapBorders.VariantRequested(null, new[] { "Statecraft", "-bordervariant", "C2" }, webgl: false));
        BorderTests.Open();
        Vector4 Rim() => Shader.GetGlobalVector("_ScBorderRim");
        Vector4 Blue() => Shader.GetGlobalVector("_ScBorderColour1");
        MapBorders.Variant = BorderVariant.A;
        Assert.AreEqual(0f, Rim().z);
        var a = Blue();
        MapBorders.Variant = BorderVariant.B;
        Assert.AreEqual(MapBorders.RimOpacity, Rim().z);
        Assert.AreEqual(a, Blue(), "B keeps A's blue");
        MapBorders.Variant = BorderVariant.C;
        Assert.AreEqual(0f, Rim().z);
        var c = MapBorders.PrimaryStrongBlue.Colour.linear;
        Assert.AreEqual(new Vector4(c.r, c.g, c.b, 1f), Blue());
        Assert.AreEqual(MapBorders.MinColourPx, Shader.GetGlobalVector("_ScBorders").x);
        // The stronger tier: its floor, wash, hold and tint; B2 its rim, C2 its blue.
        MapBorders.Variant = BorderVariant.A2;
        Assert.AreEqual(MapBorders.StrongMinColourPx, Shader.GetGlobalVector("_ScBorders").x);
        Assert.AreEqual(MapBorders.StrongWashStrength, Shader.GetGlobalVector("_ScBorderShape1").w);
        Assert.AreEqual(MapBorders.StrongWashStrength, Shader.GetGlobalVector("_ScBorderShape2").w);
        Assert.AreEqual(1f, Shader.GetGlobalVector("_ScBorderMin").z);
        Assert.AreEqual(MapBorders.StrongTintNear, Shader.GetGlobalVector("_ScBorderTint").x);
        Assert.AreEqual(a, Blue()); Assert.AreEqual(0f, Rim().z);
        MapBorders.Variant = BorderVariant.B2;
        Assert.AreEqual(MapBorders.RimOpacity, Rim().z);
        MapBorders.Variant = BorderVariant.C2;
        Assert.AreEqual(new Vector4(c.r, c.g, c.b, 1f), Blue());
        MapBorders.Variant = BorderVariant.A;
        Assert.AreEqual(0f, Shader.GetGlobalVector("_ScBorderMin").z);
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
        (float band, float wash) Measure()
        {
            var walks = BorderTests.WalkEdge(map, cam, inner, outer, 9, 1920, 1080);
            var band = walks.Select(w => BorderTests.Read(w, -1, BorderTests.Blueness, 0.45f).WidthPx).OrderBy(x => x).ElementAt(4);
            var wash = walks.SelectMany(w => Enumerable.Range(0, w.On.Count).Where(k => w.SignedM[k] <= -700f && w.SignedM[k] >= -1000f)
                .Select(k => RenderSample.DeltaE(w.On[k], w.Off[k]))).OrderBy(x => x).ToList();
            return (band, wash[wash.Count / 2]);
        }
        var a = Measure();
        MapBorders.Variant = BorderVariant.A2;
        var a2 = Measure();
        TestContext.WriteLine($"gameplay03: primary band A {a.band:0.0} px, A2 {a2.band:0.0} px; wash deltaE 700-1000 m in: A {a.wash:0.0}, A2 {a2.wash:0.0}");
        Assert.GreaterOrEqual(a2.band, a.band + 1f, "A2's band is wider");
        Assert.Greater(a2.wash, a.wash * 1.3f, "A2's wash reads farther in");
    }

    /// <summary>Variant B: a dark rim on the edge. Close up (20 km), the darkest Primary pixel within 60 m of a primary-secondary
    /// proof edge is clearly darker with B than with A.</summary>
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
        float Darkest() => BorderTests.WalkEdge(map, cam, inner, outer, 5, 1920, 1080)
            .Select(w => Enumerable.Range(0, w.On.Count).Where(k => w.SignedM[k] < 0f && w.SignedM[k] > -60f).Select(k => RenderSample.Luma(w.On[k])).DefaultIfEmpty(1f).Min())
            .OrderBy(x => x).ElementAt(2);
        float a = Darkest();
        MapBorders.Variant = BorderVariant.B;
        float b = Darkest();
        TestContext.WriteLine($"20 km: darkest primary pixel within 60 m of the edge: A {a:0.000}, B {b:0.000}");
        Assert.Less(b, a - 0.05f, "B's rim darkens the edge");
    }
}
