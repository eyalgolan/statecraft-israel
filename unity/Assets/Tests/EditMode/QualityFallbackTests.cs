#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Issue #44: the web tier's runtime quality fallback. The controller is pure logic, so these feed it synthetic
/// frame times (no rendering); the last tests check where it is wired.</summary>
public class QualityFallbackTests
{
    const float Fine = 16f, Slow = 45f, Edge = 25f;

    /// <summary>Frames of <paramref name="ms"/> for <paramref name="seconds"/>; returns the levels it moved through.</summary>
    static int[] Run(QualityFallback q, float ms, float seconds)
    {
        var levels = new System.Collections.Generic.List<int>();
        for (float t = 0; t < seconds * 1000f; t += ms) if (q.Feed(ms)) levels.Add(q.Level);
        return levels.ToArray();
    }

    /// <summary>Frames of <paramref name="ms"/> until <paramref name="count"/> more windows are measured; returns the levels it
    /// moved through. <paramref name="before"/> frames open each window (hitches).</summary>
    static int[] Windows(QualityFallback q, float ms, int count, params float[] before)
    {
        var levels = new System.Collections.Generic.List<int>();
        for (int w = 0; w < count; w++)
        {
            int at = q.Windows;
            foreach (var b in before) if (q.Feed(b)) levels.Add(q.Level);
            while (q.Windows == at) if (q.Feed(ms)) levels.Add(q.Level);
        }
        return levels.ToArray();
    }

    /// <summary>A controller past its start-up settle (3 s of fine frames).</summary>
    static QualityFallback Started() { var q = new QualityFallback(); Run(q, Fine, QualityFallback.StartupSeconds); return q; }

    [Test]
    public void TheStepsLowerTheRenderScaleWithinTheResearchRangeThenTurnTheTiltShiftOff()
    {
        var s = QualityFallback.Steps;
        Assert.AreEqual(4, s.Count);
        Assert.AreEqual((1f, true), (s[0].RenderScale, s[0].TiltShift), "level 0 is the tier's own look");
        for (int i = 1; i < s.Count; i++)
        {
            Assert.That(s[i].RenderScale, Is.InRange(0.8f, 1f), "research notes 2.3: 0.8-1.0 dynamic");
            Assert.LessOrEqual(s[i].RenderScale, s[i - 1].RenderScale);
        }
        int firstTiltOff = s.ToList().FindIndex(x => !x.TiltShift);
        Assert.AreEqual(s.Count - 1, firstTiltOff, "the tilt-shift goes last");
        Assert.AreEqual(0.8f, s[firstTiltOff - 1].RenderScale, "only once the scale is at its floor");
    }

    [Test]
    public void SustainedSlowFramesStepDownOneLevelPerWindowInOrder()
    {
        var q = Started();
        var levels = Run(q, Slow, 60f);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, levels, "one step per window, never past the last");
        Assert.AreEqual(3, q.Level); Assert.IsFalse(q.Current.TiltShift); Assert.AreEqual(0.8f, q.Current.RenderScale);
        Assert.AreEqual(Slow, q.LastP95);
    }

    /// <summary>Review M2: the slowest devices (1-3 fps) get the fallback too; a 3 s window of such frames holds only 3-6.</summary>
    [TestCase(500f)]    // 2 fps
    [TestCase(1000f)]   // 1 fps
    [TestCase(333f)]    // 3 fps
    public void VerySlowFramesStillStepDown(float ms)
    {
        var q = new QualityFallback();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Run(q, ms, 60f));
        Assert.AreEqual(ms, q.LastP95);
    }

    [Test]
    public void TheFirstStepComesAfterOneFullWindowNotBefore()
    {
        var q = Started();
        Assert.IsEmpty(Run(q, Slow, QualityFallback.WindowSeconds - 0.1f));
        Assert.AreEqual(0, q.Level);
        CollectionAssert.AreEqual(new[] { 1 }, Run(q, Slow, 0.2f));
    }

    [Test]
    public void NothingIsMeasuredDuringStartUp()
    {
        var q = new QualityFallback();
        Assert.IsEmpty(Run(q, Slow, QualityFallback.StartupSeconds), "scene load and shader compiles are not load");
        Assert.AreEqual(0, q.Level);
    }

    [Test]
    public void HitchesAPauseAndFramesJustAtTheBudgetDoNotTrigger()
    {
        var q = Started();
        // 3 hitches of 200 ms in every window of 60 fps frames: 3 of ~150 frames slow, under 5%.
        Assert.IsEmpty(Windows(q, Fine, 5, 200f, 200f, 200f), "one-off hitches");
        Assert.AreEqual(0, q.Level);
        q.Feed(30000f);   // a hidden tab coming back
        Run(q, Fine, 10f);
        Assert.AreEqual(0, q.Level, "a pause");
        Assert.IsEmpty(Run(q, QualityFallback.FloorMs, 30f), "33 ms frames meet the 30 fps floor");
        for (int i = 0; i < 2000; i++) q.Feed(i % 3 == 0 ? 33f : 34f);   // a 30 Hz cap as the 1 ms WebGL clock reads it
        Assert.AreEqual(0, q.Level, "33.3 ms frames read as 33 and 34 ms: at the floor, not over it");
        var steps = Run(q, QualityFallback.OverMs + 1f, 2 * QualityFallback.WindowSeconds);
        Assert.IsNotEmpty(steps, "35 ms is over"); Assert.AreEqual(1, steps[0]);
    }

    /// <summary>Issue #216 (integration B18): a Chromium window behind others (document.hidden) ran at about 5 fps (168-204 ms frames, each under the
    /// 1 s pause line) and stepped to level 3 within 90 s. Frame times alone cannot tell that from a slow device (the same
    /// frames with the page in front do step down), so the hidden page is not measured at all.</summary>
    [Test]
    public void AHiddenThrottledPageIsNotMeasured()
    {
        var shown = Started();
        Assert.IsNotEmpty(Run(shown, 200f, 90f), "control: 200 ms frames in front are load, and step down");

        var q = Started();
        int windows = q.Windows;
        q.SetBackground(true);
        Assert.IsTrue(q.Background);
        Assert.IsEmpty(Run(q, 200f, 90f), "B18: 90 s of a throttled background window");
        Assert.IsEmpty(Run(q, 170f, 90f));
        Assert.AreEqual(0, q.Level); Assert.AreEqual(windows, q.Windows, "no window was measured while hidden");
        q.SetBackground(false);
        Assert.IsEmpty(Run(q, Slow, QualityFallback.ResumeSettleSeconds - 0.1f), "the first frames back settle");
        Assert.AreEqual(windows, q.Windows);
        CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Slow, 1), "a real shortfall in front still steps down");
    }

    /// <summary>Issue #216: going hidden drops the partial window, so slow frames just before a tab switch and fine ones
    /// after it are never mixed into one p95; repeated calls with the same state change nothing.</summary>
    [Test]
    public void GoingHiddenDropsThePartialWindow()
    {
        var q = Started();
        Run(q, Slow, QualityFallback.WindowSeconds - 0.5f);   // most of a slow window
        q.SetBackground(true); q.SetBackground(true);
        q.SetBackground(false); q.SetBackground(false);
        Assert.IsEmpty(Windows(q, Fine, 3), "the slow frames before the switch were dropped");
        Assert.AreEqual(0, q.Level);
        Assert.Less(q.LastP95, QualityFallback.RecoverMs);
    }

    /// <summary>Issue #216: a level set by a real shortfall is kept while hidden and recovers in reverse once the page is
    /// back in front, after the usual RecoverWindows good windows (a hidden stretch counts neither for nor against).</summary>
    [Test]
    public void AFallenLevelRecoversOnceThePageIsBackInFront()
    {
        var q = Started();
        CollectionAssert.AreEqual(new[] { 1, 2 }, Windows(q, Slow, 2));
        q.SetBackground(true);
        Assert.IsEmpty(Run(q, 200f, 120f));
        Assert.AreEqual(2, q.Level, "kept while hidden: nothing is known about the device then");
        q.SetBackground(false);
        int n = QualityFallback.RecoverWindows;
        Assert.IsEmpty(Windows(q, Fine, n - 1));
        CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Fine, 1), "back in front, good windows step up as usual");
        CollectionAssert.AreEqual(new[] { 0 }, Windows(q, Fine, n));
    }

    [Test]
    public void ItRecoversInReverseOnlyAfterSustainedComfortableFrames()
    {
        var q = Started();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Windows(q, Slow, 3));
        int n = QualityFallback.RecoverWindows;
        Assert.IsEmpty(Windows(q, Fine, n - 1), "two good windows are not enough");
        CollectionAssert.AreEqual(new[] { 2 }, Windows(q, Fine, 1), "the third one steps up: the tilt-shift comes back first");
        Assert.IsTrue(q.Current.TiltShift); Assert.AreEqual(0.8f, q.Current.RenderScale);
        CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Fine, n), "then the render scale, in reverse");
        CollectionAssert.AreEqual(new[] { 0 }, Windows(q, Fine, n));
        Assert.AreEqual(1f, q.Current.RenderScale);
        Assert.IsEmpty(Windows(q, Fine, 10), "level 0 is the top");
    }

    [Test]
    public void FramesBetweenTheRecoveryLineAndTheBudgetHoldTheLevel()
    {
        var q = Started();
        CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Slow, 1));
        Assert.IsEmpty(Windows(q, Edge, 20), "the dead band between 20 and 34 ms: no step either way");
        int n = QualityFallback.RecoverWindows;
        Assert.IsEmpty(Windows(q, Fine, n - 1));
        Assert.IsEmpty(Windows(q, Edge, 1));
        Assert.IsEmpty(Windows(q, Fine, n - 1), "a dead-band window restarted the run of good windows");
        CollectionAssert.AreEqual(new[] { 0 }, Windows(q, Fine, 1));
    }

    [Test]
    public void AStepUpThatFailsDoublesTheWaitForTheNextOne()
    {
        var q = Started();
        CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Slow, 1));
        Assert.AreEqual(QualityFallback.RecoverWindows, q.RecoverNeeded);
        // A step up that holds for more than two windows is not a failed one.
        CollectionAssert.AreEqual(new[] { 0 }, Windows(q, Fine, q.RecoverNeeded));
        Assert.IsEmpty(Windows(q, Fine, QualityFallback.FailedRecoveryWindows));
        CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Slow, 1));
        Assert.AreEqual(QualityFallback.RecoverWindows, q.RecoverNeeded);
        // A device on the edge: fine at the lower step, slow again right after stepping up.
        for (int cycle = 1; cycle <= 6; cycle++)
        {
            CollectionAssert.AreEqual(new[] { 0 }, Windows(q, Fine, q.RecoverNeeded), $"cycle {cycle}: up after RecoverNeeded windows");
            CollectionAssert.AreEqual(new[] { 1 }, Windows(q, Slow, 1), $"cycle {cycle}: back down");
            Assert.AreEqual(System.Math.Min(QualityFallback.RecoverWindows << cycle, QualityFallback.MaxRecoverWindows), q.RecoverNeeded);
        }
        Assert.AreEqual(QualityFallback.MaxRecoverWindows, q.RecoverNeeded, "capped");
    }

    [Test]
    public void ItRunsOnTheWebTierOnlyAndNeverUnderTheInstruments()
    {
        Assert.IsTrue(QualityFallback.Wanted("http://localhost:8000/", webgl: true));
        Assert.IsTrue(QualityFallback.Wanted("http://localhost:8000/?golden&smoke=5", webgl: true), "a golden run renders; the sim is untouched");
        Assert.IsFalse(QualityFallback.Wanted("http://localhost:8000/", webgl: false), "desktop and the editor");
        foreach (var flag in new[] { "vprofile", "profile", "shot", "nofallback" })
            Assert.IsFalse(QualityFallback.Wanted("http://localhost:8000/?bootlog&" + flag, webgl: true), flag);
        Assert.IsTrue(QualityFallback.Wanted("http://localhost:8000/?profiles", webgl: true), "whole query parameters only");
    }

    [TearDown]
    public void Reset()
    {
        TiltShift.Reset();
        VisualToggles.Clear();
    }

    /// <summary>Integration: CameraRig.Init wires the fallback into the map's lifecycle through Attach and the map's
    /// teardown, like the visual toggles. In the editor (not the WebGL tier) a built map has no driver. With the web tier's
    /// answer forced (the FallbackWanted seam), the map's camera gets one, it applies its steps to the active URP asset and
    /// to TiltShift.FallbackOff (never the player's tilt-shift toggle), and tearing the map down removes it and puts the
    /// tier's render scale back.</summary>
    [Test]
    public void TheMapWiresItOnTheWebTierOnlyAndItsTeardownPutsTheTierBack()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var cam = Camera.main!.gameObject;
        Assert.IsNull(cam.GetComponent<QualityFallbackDriver>(), "the editor is not the web tier: no driver");

        var asset = UniversalRenderPipeline.asset;
        Assert.IsNotNull(asset);
        float tierScale = asset!.renderScale;
        var wanted = CameraRig.FallbackWanted;
        try
        {
            CameraRig.FallbackWanted = () => true;
            map.Build();
            var drivers = cam.GetComponents<QualityFallbackDriver>();
            Assert.AreEqual(1, drivers.Length, "the rebuild's teardown removed nothing it should keep, and added one driver");
            var driver = drivers[0];
            for (float t = 0; t < 60_000f; t += Slow) driver.Feed(Slow);
            Assert.AreEqual(3, driver.Controller.Level);
            Assert.AreEqual(Mathf.Min(tierScale, 0.8f), asset.renderScale, 1e-6f);
            Assert.IsTrue(TiltShift.FallbackOff);
            Assert.IsTrue(TiltShift.Enabled, "the tilt-shift toggle (the player's and the profile's) is left alone");

            map.Build();   // a rebuild tears the old map down first: one driver, back at level 0
            Assert.AreEqual(1, cam.GetComponents<QualityFallbackDriver>().Length);
            Assert.AreEqual(tierScale, asset.renderScale, "the old map's teardown put the tier's scale back");
            Assert.IsFalse(TiltShift.FallbackOff);

            map.Clear(go => Object.DestroyImmediate(go));
            Assert.IsNull(cam.GetComponent<QualityFallbackDriver>(), "the map's teardown removes the driver");
            Assert.AreEqual(tierScale, asset.renderScale);
        }
        finally
        {
            CameraRig.FallbackWanted = wanted;
            asset.renderScale = tierScale;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    [Test]
    public void TheAttachUndoIsSafeToRunTwiceAndANotWantedAttachAddsNothing()
    {
        var go = new GameObject("cam");
        try
        {
            QualityFallbackDriver.Attach(go, wanted: false)();
            Assert.IsNull(go.GetComponent<QualityFallbackDriver>());
            var asset = UniversalRenderPipeline.asset!;
            float tierScale = asset.renderScale;
            var undo = QualityFallbackDriver.Attach(go, wanted: true);
            Assert.IsNotNull(go.GetComponent<QualityFallbackDriver>());
            undo(); undo();
            Assert.IsNull(go.GetComponent<QualityFallbackDriver>());
            Assert.AreEqual(tierScale, asset.renderScale);
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>High or Low in Settings (#40) fixes the quality: the fallback holds (no step, the render scale and the
    /// tilt-shift are the choice's). Back on Automatic it starts again from the tier's own look, not from the level it had
    /// reached before.</summary>
    [Test]
    public void AFixedQualityChoiceHoldsTheFallbackAndAutomaticStartsItAgain()
    {
        var go = new GameObject("cam");
        int level = QualitySettings.GetQualityLevel();
        var asset = UniversalRenderPipeline.asset!;
        float tierScale = asset.renderScale;
        var mobile = (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(System.Array.IndexOf(QualitySettings.names, "Mobile"));
        float mobileScale = mobile.renderScale;
        try
        {
            var undo = QualityFallbackDriver.Attach(go, wanted: true);
            var driver = go.GetComponent<QualityFallbackDriver>();
            GraphicsQuality.Apply(GraphicsQuality.Choice.High);
            for (float t = 0; t < 60_000f; t += Slow) driver.Feed(Slow);
            Assert.AreEqual(0, driver.Controller.Level, "slow frames move nothing while High is set");
            Assert.AreEqual(1f, asset.renderScale, 1e-6f);
            Assert.IsFalse(TiltShift.FallbackOff);

            GraphicsQuality.Apply(GraphicsQuality.Choice.Auto);
            for (float t = 0; t < 60_000f; t += Slow) driver.Feed(Slow);
            Assert.AreEqual(3, driver.Controller.Level, "on Automatic the fallback steps down as before");
            Assert.IsTrue(TiltShift.FallbackOff);

            GraphicsQuality.Apply(GraphicsQuality.Choice.Low);
            Assert.IsFalse(TiltShift.FallbackOff, "a fixed choice ends the fallback's last step");
            GraphicsQuality.Apply(GraphicsQuality.Choice.Auto);
            Assert.AreEqual(tierScale, asset.renderScale, 1e-6f, "Automatic puts the tier's own scale back");
            driver.Feed(Fine);
            Assert.AreEqual(0, driver.Controller.Level, "and the fallback starts again from the top");
            undo();
        }
        finally
        {
            GraphicsQuality.Apply(GraphicsQuality.Choice.Auto);
            QualitySettings.SetQualityLevel(level, true);
            asset.renderScale = tierScale; mobile.renderScale = mobileScale;
            Object.DestroyImmediate(go);
        }
    }
}
