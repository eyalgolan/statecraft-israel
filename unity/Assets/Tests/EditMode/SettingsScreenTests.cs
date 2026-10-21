#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The Settings screen (#40): opened from the left column's gear like the other screens, its tilt-shift option
/// applied at once and kept in the SettingsStore across a reload, and laid out at 1280x720 in both languages. A reload
/// here is what a page reload leaves: the statics start over (TiltShift.Enabled back on) and only the file remains.</summary>
public class SettingsScreenTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    string dir = "";
    int level;
    float[] scales = new float[0];
    Vector2Int reference;
    Dictionary<int, (float, float)> verified = new Dictionary<int, (float, float)>();

    static UniversalRenderPipelineAsset Tier(int i) => (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(i);

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "sc-settings-" + System.Guid.NewGuid().ToString("N"));
        TiltShift.Enabled = true;
        level = QualitySettings.GetQualityLevel();
        scales = new float[QualitySettings.names.Length];
        for (int i = 0; i < scales.Length; i++) scales[i] = Tier(i).renderScale;
        reference = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel).referenceResolution;
        verified = new Dictionary<int, (float, float)>(SettingsPresenter.VerifiedShapes);
    }

    [TearDown]
    public void TearDown()
    {
        TiltShift.Enabled = true;
        // The editor's quality level and the URP assets are the project's: put back what a quality test switched.
        GraphicsQuality.Apply(GraphicsQuality.Choice.Auto);
        QualitySettings.SetQualityLevel(level, true);
        for (int i = 0; i < scales.Length; i++) Tier(i).renderScale = scales[i];
        AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel).referenceResolution = reference;   // the project's panel
        SettingsPresenter.VerifiedShapes.Clear();                                                       // and the game's table
        foreach (var kv in verified) SettingsPresenter.VerifiedShapes.Add(kv.Key, kv.Value);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Test]
    public void TheGearInTheLeftColumnOpensAndClosesTheSettingsScreen()
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var settings = new SettingsPresenter(ui.Root, En, new SettingsStore(dir));
        var screen = ui.Q<VisualElement>("screen-settings");
        Assert.IsFalse(ScreenNav.IsOpen(screen), "hidden at start");
        ui.Press("nav-settings");
        Assert.IsTrue(ScreenNav.IsOpen(screen), "the gear opens it");
        Assert.AreEqual("settings", nav.Current);
        ui.Press("nav-settings");
        Assert.IsFalse(ScreenNav.IsOpen(screen), "pressed again, it closes");
    }

    [Test]
    public void TheTiltShiftOptionTakesEffectAtOnceAndSurvivesAReload([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using (var ui = new UiTestKit())
        using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir)))
        {
            var button = ui.Q<Button>("settings-tiltshift");
            Assert.IsTrue(TiltShift.Enabled, "on by default, with no file");
            Assert.AreEqual(text["settings.on"], button.text);
            UiTestKit.Press(button);
            Assert.IsFalse(TiltShift.Enabled, "off at once, with no apply step");
            Assert.AreEqual(text["settings.off"], button.text, "the button says what is set");
        }
        TiltShift.Enabled = true;   // the reload
        using (var ui = new UiTestKit())
        using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir)))
        {
            Assert.IsFalse(TiltShift.Enabled, "still off after the reload");
            var button = ui.Q<Button>("settings-tiltshift");
            Assert.AreEqual(text["settings.off"], button.text);
            UiTestKit.Press(button);
            Assert.IsTrue(TiltShift.Enabled, "and on again at once");
            Assert.AreEqual(text["settings.on"], button.text);
        }
        TiltShift.Enabled = true;   // the reload
        using (var ui = new UiTestKit())
        using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir)))
            Assert.IsTrue(TiltShift.Enabled, "on survives a reload too");
    }

    /// <summary>?notiltshift (TiltShift.OffRequested) boots with the blur off; a stored "on" does not turn it back on.</summary>
    [Test]
    public void AStoredOnDoesNotOverrideNoTiltShift()
    {
        new SettingsStore(dir).Write(SettingsPresenter.TiltShiftKey, "on");
        TiltShift.Enabled = false;   // what CameraRig.Init does for ?notiltshift
        using var ui = new UiTestKit();
        using var settings = new SettingsPresenter(ui.Root, En, new SettingsStore(dir));
        Assert.IsFalse(TiltShift.Enabled);
        Assert.AreEqual(En["settings.off"], ui.Q<Button>("settings-tiltshift").text);
    }

    static readonly (string button, GraphicsQuality.Choice choice)[] QualityButtons =
    {
        ("settings-quality-auto", GraphicsQuality.Choice.Auto), ("settings-quality-high", GraphicsQuality.Choice.High),
        ("settings-quality-low", GraphicsQuality.Choice.Low),
    };

    /// <summary>The owner's three choices (#40, 18 Oct 2026): High is the PC tier at render scale 1.0, Low the Mobile tier
    /// at 0.8, Automatic the tier the device picked at its own scale. Each takes effect at once, the button of the choice
    /// that is set carries the selected mark, and the choice is kept in the SettingsStore.</summary>
    [Test]
    public void EachQualityChoiceSetsItsTierAndRenderScaleAtOnce([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        int device = QualitySettings.GetQualityLevel();
        using var ui = new UiTestKit();
        using var settings = new SettingsPresenter(ui.Root, text, new SettingsStore(dir));
        Assert.AreEqual(GraphicsQuality.Choice.Auto, GraphicsQuality.Current, "Automatic by default, with no file");
        Assert.AreEqual(device, QualitySettings.GetQualityLevel(), "and the device's tier is left alone");
        AssertSelected(ui, GraphicsQuality.Choice.Auto);

        UiTestKit.Press(ui.Q<Button>("settings-quality-low"));
        Assert.AreEqual("Mobile", QualitySettings.names[QualitySettings.GetQualityLevel()]);
        Assert.AreEqual(0.8f, UniversalRenderPipeline.asset!.renderScale, 1e-6f);
        AssertSelected(ui, GraphicsQuality.Choice.Low);
        Assert.AreEqual("low", new SettingsStore(dir).Read(SettingsPresenter.QualityKey));

        UiTestKit.Press(ui.Q<Button>("settings-quality-high"));
        Assert.AreEqual("PC", QualitySettings.names[QualitySettings.GetQualityLevel()]);
        Assert.AreEqual(1f, UniversalRenderPipeline.asset!.renderScale, 1e-6f);
        AssertSelected(ui, GraphicsQuality.Choice.High);
        Assert.AreEqual("high", new SettingsStore(dir).Read(SettingsPresenter.QualityKey));

        UiTestKit.Press(ui.Q<Button>("settings-quality-auto"));
        Assert.AreEqual(device, QualitySettings.GetQualityLevel(), "Automatic goes back to the device's tier");
        Assert.AreEqual(QualitySettings.names[device] == "Mobile" ? QualityTierValues.WebRenderScale : 1f, UniversalRenderPipeline.asset!.renderScale, 1e-6f, "at its own scale");
        AssertSelected(ui, GraphicsQuality.Choice.Auto);
        Assert.AreEqual("auto", new SettingsStore(dir).Read(SettingsPresenter.QualityKey));
    }

    [Test]
    public void TheQualityChoiceSurvivesAReload()
    {
        using (var ui = new UiTestKit())
        using (new SettingsPresenter(ui.Root, En, new SettingsStore(dir)))
            UiTestKit.Press(ui.Q<Button>("settings-quality-low"));
        GraphicsQuality.Apply(GraphicsQuality.Choice.Auto);   // the reload: the device's tier again, only the file remains
        using (var ui = new UiTestKit())
        using (new SettingsPresenter(ui.Root, En, new SettingsStore(dir)))
        {
            Assert.AreEqual(GraphicsQuality.Choice.Low, GraphicsQuality.Current);
            Assert.AreEqual("Mobile", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            Assert.AreEqual(0.8f, UniversalRenderPipeline.asset!.renderScale, 1e-6f);
            AssertSelected(ui, GraphicsQuality.Choice.Low);
        }
    }

    /// <summary>Pressing Automatic while it is set changes nothing: a step the fallback took for a slow device stays.</summary>
    [Test]
    public void PressingAutomaticAgainKeepsTheFallbacksStep()
    {
        var cam = new GameObject("cam");
        var undo = QualityFallbackDriver.Attach(cam, wanted: true);
        try
        {
            var driver = cam.GetComponent<QualityFallbackDriver>();
            using var ui = new UiTestKit();
            using var settings = new SettingsPresenter(ui.Root, En, new SettingsStore(dir));
            for (float t = 0; t < 60_000f; t += 45f) driver.Feed(45f);
            int level = driver.Controller.Level;
            Assert.Greater(level, 0, "slow frames stepped down");
            ui.Press("settings-quality-auto");
            Assert.AreEqual(level, driver.Controller.Level);
            Assert.AreEqual(QualityFallback.Steps[level].TiltShift, !TiltShift.FallbackOff);
        }
        finally { undo(); Object.DestroyImmediate(cam); }
    }

    /// <summary>The instruments that must see the tier's own look win over a stored choice, as they win over the fallback:
    /// ?vprofile, ?profile, ?shot and ?nofallback on the web, -vprofile, -profile and -shot on the desktop. A value the game
    /// does not know is Automatic.</summary>
    [Test]
    public void TheInstrumentFlagsWinOverAStoredQualityChoice()
    {
        const string page = "http://localhost:8000/";
        var none = new string[0];
        Assert.AreEqual(GraphicsQuality.Choice.Low, GraphicsQuality.AtStart("low", page, none, webgl: true));
        Assert.AreEqual(GraphicsQuality.Choice.High, GraphicsQuality.AtStart("high", page + "?golden", none, webgl: true));
        Assert.AreEqual(GraphicsQuality.Choice.High, GraphicsQuality.AtStart("high", "", new[] { "Statecraft", "-seed", "7" }, webgl: false));
        foreach (var flag in new[] { "vprofile", "profile", "shot", "nofallback" })
            Assert.AreEqual(GraphicsQuality.Choice.Auto, GraphicsQuality.AtStart("low", page + "?bootlog&" + flag, none, webgl: true), flag);
        foreach (var arg in new[] { "-vprofile", "-profile", "-shot" })
            Assert.AreEqual(GraphicsQuality.Choice.Auto, GraphicsQuality.AtStart("low", "", new[] { "Statecraft", arg }, webgl: false), arg);
        Assert.AreEqual(GraphicsQuality.Choice.Auto, GraphicsQuality.AtStart(null, page, none, webgl: true));
        Assert.AreEqual(GraphicsQuality.Choice.Auto, GraphicsQuality.AtStart("ultra", page, none, webgl: true));
    }

    /// <summary>"The same choices apply on web and desktop" (#40): both tiers must be in the WebGL and the desktop builds,
    /// or Low would have no Mobile tier to switch to in a desktop player.</summary>
    [Test]
    public void BothTiersShipOnTheWebAndTheDesktop()
    {
        foreach (var tier in new[] { GraphicsQuality.HighTier, GraphicsQuality.LowTier })
        {
            int i = System.Array.IndexOf(QualitySettings.names, tier);
            Assert.GreaterOrEqual(i, 0, tier);
            foreach (var platform in new[] { "WebGL", "Standalone" })
                Assert.IsTrue(QualitySettings.IsPlatformIncluded(platform, i), $"{tier} on {platform}");
        }
    }

    /// <summary>The labels Beri (a native speaker) approved on #40 (issuecomment-6241626427).</summary>
    [Test]
    public void TheQualityLabelsAreTheApprovedOnes()
    {
        Assert.AreEqual("Quality", En["settings.quality"]);
        Assert.AreEqual(("Automatic", "High", "Low"), (En["settings.quality.auto"], En["settings.quality.high"], En["settings.quality.low"]));
        Assert.AreEqual("איכות גרפית", He["settings.quality"]);
        Assert.AreEqual(("אוטומטית", "גבוהה", "נמוכה"), (He["settings.quality.auto"], He["settings.quality.high"], He["settings.quality.low"]));
    }

    static void AssertSelected(UiTestKit ui, GraphicsQuality.Choice set)
    {
        foreach (var (name, choice) in QualityButtons)
            Assert.AreEqual(choice == set, ui.Q<Button>(name).ClassListContains(SettingsPresenter.SelectedClass), $"{name} marked while {set} is set");
    }

    /// <summary>A tier switch refits the new tier's shadow distance at once (ShadowFit keeps one per URP asset): the map
    /// must not wait for the camera to move to draw its shadows right.</summary>
    [Test]
    public void ATierSwitchRefitsTheShadowsAtOnce()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        try
        {
            map.Build();
            var cam = Camera.main!;
            foreach (var choice in new[] { GraphicsQuality.Choice.Low, GraphicsQuality.Choice.High })
            {
                GraphicsQuality.Apply(choice);
                var asset = UniversalRenderPipeline.asset!;
                Assert.AreEqual(ShadowFit.Distance(cam, asset.cascadeBorder), asset.shadowDistance, 1e-3f, choice.ToString());
            }
        }
        finally
        {
            map.Clear(go => Object.DestroyImmediate(go));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    static readonly (string button, int percent)[] UiScaleButtons =
        { ("settings-uiscale-100", 100), ("settings-uiscale-125", 125), ("settings-uiscale-150", 150) };

    /// <summary>The owner's UI scale (#40, 18 Oct 2026): 100%, 125% and 150%, default 100%. A choice scales the whole panel
    /// at once (its reference resolution divided by the scale, so the panel's own ScaleWithScreenSize scale is multiplied by
    /// it), the set button is marked, the choice is kept in the SettingsStore and comes back after a reload. Closing the
    /// presenter puts the project's panel back, so a choice made in the editor never stays in the asset.</summary>
    [Test]
    public void EachUiScaleTakesEffectAtOnceAndSurvivesAReload([Values(false, true)] bool hebrew)
    {
        VerifyOnEveryShape(125, 150);   // the mechanism a scale gets once it is verified; the game verifies none yet
        var text = hebrew ? He : En;
        using (var ui = new UiTestKit())
        {
            var ps = ui.Doc.panelSettings;
            using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir), panel: ps))
            {
                Assert.AreEqual(new Vector2Int(1920, 1080), ps.referenceResolution, "100% by default, with no file");
                AssertUiScale(ui, 100);
                foreach (var (name, percent) in UiScaleButtons)
                    Assert.AreEqual(percent + "%", ui.Q<Button>(name).text, "the button says its scale");
                ui.Press("settings-uiscale-150");
                Assert.AreEqual(new Vector2Int(1280, 720), ps.referenceResolution, "150% at once");
                AssertUiScale(ui, 150);
                Assert.AreEqual("150", new SettingsStore(dir).Read(SettingsPresenter.UiScaleKey));
                ui.Press("settings-uiscale-125");
                Assert.AreEqual(new Vector2Int(1536, 864), ps.referenceResolution, "125% at once");
                AssertUiScale(ui, 125);
                Assert.AreEqual("125", new SettingsStore(dir).Read(SettingsPresenter.UiScaleKey));
            }
            Assert.AreEqual(new Vector2Int(1920, 1080), ps.referenceResolution, "the project's panel is put back");
        }
        using (var ui = new UiTestKit())   // the reload: only the file remains
        using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir), panel: ui.Doc.panelSettings))
        {
            Assert.AreEqual(new Vector2Int(1536, 864), ui.Doc.panelSettings.referenceResolution, "125% after the reload");
            AssertUiScale(ui, 125);
            ui.Press("settings-uiscale-100");
            Assert.AreEqual(new Vector2Int(1920, 1080), ui.Doc.panelSettings.referenceResolution, "and back to 100%");
            Assert.AreEqual("100", new SettingsStore(dir).Read(SettingsPresenter.UiScaleKey));
        }
    }

    /// <summary>A stored value the game does not offer is 100%.</summary>
    [Test]
    public void AnUnknownUiScaleIs100()
    {
        new SettingsStore(dir).Write(SettingsPresenter.UiScaleKey, "300");
        using var ui = new UiTestKit();
        using var settings = new SettingsPresenter(ui.Root, En, new SettingsStore(dir), panel: ui.Doc.panelSettings);
        Assert.AreEqual(new Vector2Int(1920, 1080), ui.Doc.panelSettings.referenceResolution);
        AssertUiScale(ui, 100);
    }

    /// <summary>As if the layout suites passed at these scales on every window shape: what a test of the scale's mechanism
    /// needs now that the game offers only verified scales (SettingsPresenter.VerifiedShapes, empty for now). TearDown puts
    /// the table back.</summary>
    public static void VerifyOnEveryShape(params int[] percents)
    {
        foreach (var p in percents) SettingsPresenter.VerifiedShapes[p] = (0f, float.PositiveInfinity);
    }

    /// <summary>The owner's ruling on #1011 (19 Oct 2026), case 1: only the scales verified to fit are offered, 100% always.
    /// No scale above 100% is verified yet (at 125% and 150% the layout suites fail on every window shape), so on each
    /// shape the suites cover Settings offers 100% alone, marked. When a scale is verified, this test changes with it.</summary>
    [UnityTest]
    public IEnumerator OnlyTheVerifiedUiScalesAreOffered()
    {
        Assert.IsEmpty(SettingsPresenter.VerifiedShapes, "no scale above 100% is verified yet");
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1024, 768), (960, 1080), (360, 800), (2560, 1080) })
        {
            var window = new Window(w, h, dir);
            try
            {
                yield return window.Settle();
                foreach (var (name, percent) in UiScaleButtons)
                    Assert.AreEqual(percent == 100, window.Root.Q<Button>(name).resolvedStyle.display == DisplayStyle.Flex, $"{w}x{h}: {percent}% offered");
                Assert.IsTrue(window.Root.Q<Button>("settings-uiscale-100").ClassListContains(SettingsPresenter.SelectedClass), $"{w}x{h}: 100% marked");
            }
            finally { window.Dispose(); }
        }
    }

    /// <summary>Case 2: a saved 125% or 150% that is not verified to fit comes up as 100% at the next start, marked. The
    /// file keeps the choice, so it comes back once that scale is verified for the window.</summary>
    [Test]
    public void ASavedScaleThatIsNotVerifiedFallsBackTo100OnLoad([Values(125, 150)] int saved)
    {
        new SettingsStore(dir).Write(SettingsPresenter.UiScaleKey, saved.ToString());
        using var ui = new UiTestKit();
        using var settings = new SettingsPresenter(ui.Root, En, new SettingsStore(dir), panel: ui.Doc.panelSettings);
        Assert.AreEqual(new Vector2Int(1920, 1080), ui.Doc.panelSettings.referenceResolution, $"a saved {saved}% starts at 100%");
        AssertUiScale(ui, 100);
        Assert.AreEqual(saved.ToString(), new SettingsStore(dir).Read(SettingsPresenter.UiScaleKey), "the choice is kept");
    }

    /// <summary>Case 3: the fit follows the window. With 150% verified only on shapes from 1:2 to 1:1 (a stand-in: the
    /// game verifies none yet), a saved 150% holds on 960x1080; a resize to 16:9 drops the panel to 100% and stops
    /// offering 150%; back on 960x1080 the kept choice returns.</summary>
    [UnityTest]
    public IEnumerator ResizingOutOfAVerifiedShapeFallsBackTo100()
    {
        SettingsPresenter.VerifiedShapes[150] = (0.5f, 1f);
        new SettingsStore(dir).Write(SettingsPresenter.UiScaleKey, "150");
        var window = new Window(960, 1080, dir);
        try
        {
            yield return window.Settle();
            Assert.AreEqual(new Vector2Int(1280, 720), window.Panel.referenceResolution, "150% where it is verified");
            Assert.IsTrue(window.Root.Q<Button>("settings-uiscale-150").ClassListContains(SettingsPresenter.SelectedClass), "150% is marked");
            Assert.AreEqual(DisplayStyle.None, window.Root.Q<Button>("settings-uiscale-125").resolvedStyle.display, "125% is not verified");
            yield return window.Resize(1920, 1080);
            Assert.AreEqual(new Vector2Int(1920, 1080), window.Panel.referenceResolution, "16:9 is outside 150%'s shapes: 100%");
            Assert.IsTrue(window.Root.Q<Button>("settings-uiscale-100").ClassListContains(SettingsPresenter.SelectedClass), "100% is marked");
            Assert.AreEqual(DisplayStyle.None, window.Root.Q<Button>("settings-uiscale-150").resolvedStyle.display, "150% is not offered on 16:9");
            Assert.AreEqual("150", new SettingsStore(dir).Read(SettingsPresenter.UiScaleKey), "the choice is kept");
            yield return window.Resize(960, 1080);
            Assert.AreEqual(new Vector2Int(1280, 720), window.Panel.referenceResolution, "150% again where it is verified");
        }
        finally { window.Dispose(); }
    }

    /// <summary>The real Game.uxml on its own copy of the project's panel at w x h, with the Settings screen's presenter on
    /// a SettingsStore in dir.</summary>
    sealed class Window : System.IDisposable
    {
        public readonly PanelSettings Panel; readonly GameObject go; readonly SettingsPresenter settings; RenderTexture rt;
        public VisualElement Root => go.GetComponent<UIDocument>().rootVisualElement;

        public Window(int w, int h, string dir)
        {
            rt = new RenderTexture(w, h, 0);
            Panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            Panel.targetTexture = rt;
            go = new GameObject("SettingsScaleTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = Panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root.Q("root").RemoveFromClassList("loading");
            settings = new SettingsPresenter(Root, En, new SettingsStore(dir), panel: Panel);
        }

        public IEnumerator Settle()
        {
            for (int i = 0; i < 30 && !(Root.layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
        }

        /// <summary>A new target texture of w x h, the way a window resize reaches the panel.</summary>
        public IEnumerator Resize(int w, int h)
        {
            var old = rt;
            rt = new RenderTexture(w, h, 0);
            Panel.targetTexture = rt;
            old.Release(); Object.DestroyImmediate(old);
            for (int i = 0; i < 5; i++) yield return null;
        }

        public void Dispose()
        {
            settings.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(Panel); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>The new label; its Hebrew waits for the native reviewer (#564).</summary>
    [Test]
    public void TheUiScaleLabelIsInBothLanguages()
    {
        Assert.AreEqual("UI scale", En["settings.uiscale"]);
        Assert.AreEqual("גודל הממשק", He["settings.uiscale"]);
    }

    /// <summary>#1168: the Community row's "Star on GitHub" asks to open exactly the repository, once per press, from the
    /// mouse (on pointer down, where the web build arms its listener: ExternalLink) and from the keyboard. Through the
    /// injectable opener, so no browser opens.</summary>
    [Test]
    public void TheStarButtonOpensTheRepositoryOncePerPress([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var opened = new List<string>();
        ExternalLink.Opener = opened.Add;
        try
        {
            using var ui = new UiTestKit();
            using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir)))
            {
                var star = ui.Q<Button>("settings-star");
                Assert.AreEqual(text["community.star"], star.text);
                Assert.AreEqual(text["settings.community"], ui.Q<Label>("settings-community-label").text);
                PressWithPointer(star);
                CollectionAssert.AreEqual(new[] { "https://github.com/eyalgolan/statecraft-israel" }, opened, "a click");
                UiTestKit.Press(star);
                Assert.AreEqual(2, opened.Count, "Enter or Space");
                using (var right = PointerDownEvent.GetPooled()) { right.target = star; SetButton(right, 1); star.SendEvent(right); }
                Assert.AreEqual(2, opened.Count, "a right click opens nothing");
            }
            UiTestKit.Press(ui.Q<Button>("settings-star"));
            Assert.AreEqual(2, opened.Count, "disposed, the button is unwired");
        }
        finally { ExternalLink.Opener = ExternalLink.Default; }
    }

    /// <summary>The two labels; Beri approved their Hebrew as a native speaker on #1178.</summary>
    [Test]
    public void TheCommunityLabelsAreInBothLanguages()
    {
        Assert.AreEqual("Community", En["settings.community"]);
        Assert.AreEqual("Star on GitHub", En["community.star"]);
        Assert.AreEqual("קהילה", He["settings.community"]);
        Assert.AreEqual("סימון בכוכב בגיטהאב", He["community.star"]);
    }

    /// <summary>A mouse press as the player makes it (StartScreenTests.PressWithPointer): a primary-button pointer down
    /// (GetPooled leaves the button at -1), then the button's Clickable fires on the release.</summary>
    internal static void PressWithPointer(Button b)
    {
        using (var down = PointerDownEvent.GetPooled()) { down.target = b; SetButton(down, 0); b.SendEvent(down); }
        using var click = ClickEvent.GetPooled();
        click.target = b;
        typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(EventBase) }, null)!.Invoke(b.clickable, new object[] { click });
    }

    /// <summary>PointerDownEvent.button has no public setter.</summary>
    static void SetButton(PointerDownEvent e, int button) =>
        typeof(PointerDownEvent).GetProperty("button")!.SetValue(e, button);

    static void AssertUiScale(UiTestKit ui, int set)
    {
        foreach (var (name, percent) in UiScaleButtons)
            Assert.AreEqual(percent == set, ui.Q<Button>(name).ClassListContains(SettingsPresenter.SelectedClass), $"{name} marked while {set}% is set");
    }

    /// <summary>A file the game cannot use (another key, a value it does not know, a line without "=") is no reason to fail
    /// a start: the option keeps its default.</summary>
    [Test]
    public void AnUnusableFileLeavesTheDefault()
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(new SettingsStore(dir).FilePath, "junk\nscale=2\ntiltshift=maybe\n");
        using var ui = new UiTestKit();
        using var settings = new SettingsPresenter(ui.Root, En, new SettingsStore(dir));
        Assert.IsTrue(TiltShift.Enabled);
        UiTestKit.Press(ui.Q<Button>("settings-tiltshift"));
        Assert.AreEqual("off", new SettingsStore(dir).Read(SettingsPresenter.TiltShiftKey));
        Assert.AreEqual("2", new SettingsStore(dir).Read("scale"), "another key's line is kept");
    }

    /// <summary>On the web the file sits in a fixed /idbfs folder, as the autosave does (SaveStore), not under
    /// persistentDataPath, which hashes the page's URL: itch.io serves every upload from a new URL. It is not the save's
    /// folder: the settings never touch the save.</summary>
    [Test]
    public void OnTheWebTheFolderIsFixedAndIsNotTheSaves()
    {
        Assert.AreEqual("/idbfs/statecraft-settings", SettingsStore.DirectoryFor(RuntimePlatform.WebGLPlayer, "/idbfs/0123456789abcdef"));
        Assert.AreNotEqual(SaveStore.WebDirectory, SettingsStore.WebDirectory);
        Assert.AreEqual(Path.Combine("/home/p", "settings"), SettingsStore.DirectoryFor(RuntimePlatform.LinuxPlayer, "/home/p"));
    }

    /// <summary>The open screen at 1280x720 (the issue's size), in English and Hebrew, at each UI scale: the title, the option's name and its
    /// button inside the screen, below the top bar and inside the window, each text whole (on one line or wrapped). 125% and
    /// 150% run as if verified (VerifyOnEveryShape): the screen itself must hold them once the rest of the UI does.</summary>
    [UnityTest]
    public IEnumerator TheOpenScreenFitsAt1280x720InBothLanguages([Values(100, 125, 150)] int uiScale)
    {
        var c = ContentLoader.Load(Paths.Content);
        VerifyOnEveryShape(125, 150);
        if (uiScale != 100) new SettingsStore(dir).Write(SettingsPresenter.UiScaleKey, uiScale.ToString());
        foreach (var hebrew in new[] { false, true })
        {
            var text = hebrew ? He : En;
            string at = (hebrew ? "he" : "en") + " at " + uiScale + "%";
            var rt = new RenderTexture(1280, 720, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("SettingsLayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            root.Q("root").RemoveFromClassList("loading");
            if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
            var hud = new HudPresenter(root, new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), text), text);
            var nav = new ScreenNav(root);
            var settings = new SettingsPresenter(root, text, new SettingsStore(dir), panel: ps);
            try
            {
                nav.Open("settings");
                for (int i = 0; i < 30 && !(root.Q("screen-settings").layout.width > 0); i++) yield return null;
                for (int i = 0; i < 3; i++) yield return null;
                // 1280x720 is 2/3 of the 1920x1080 reference: the panel's scale is 2/3 times the UI scale.
                Assert.AreEqual(1920f * 100f / uiScale, root.worldBound.width, 1f, at + ": the panel's logical width");
                var screen = root.Q("screen-settings").worldBound;
                Assert.IsTrue(Inside(screen, root.worldBound), $"{at}: the screen {screen} leaves the window {root.worldBound}");
                Assert.GreaterOrEqual(screen.yMin, root.Q("top-bar").worldBound.yMax - 0.5f, at + ": the screen starts below the top bar");
                foreach (var name in new[] { "settings-screen-title", "settings-tiltshift-label", "settings-tiltshift",
                    "settings-quality-label", "settings-quality-auto", "settings-quality-high", "settings-quality-low",
                    "settings-language-label", "settings-language-en", "settings-language-he", "settings-language-note",
                    "settings-uiscale-label", "settings-uiscale-100", "settings-uiscale-125", "settings-uiscale-150",
                    "settings-community-label", "settings-star" })
                {
                    var e = root.Q<TextElement>(name);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(e.text), $"{at}: {name} has its text");
                    Assert.IsTrue(Inside(e.worldBound, screen), $"{at}: {name} {e.worldBound} leaves the screen {screen}");
                    Assert.IsTrue(UiLayoutTests.TextFits(e), $"{at}: {name} '{e.text}' is cut ({e.contentRect})");
                }
                // The Community row (#1168) runs right to left in Hebrew, like the language row: the button left of its name.
                var starAt = root.Q("settings-star").worldBound; var communityAt = root.Q("settings-community-label").worldBound;
                Assert.AreEqual(hebrew, starAt.xMax <= communityAt.xMin + 0.5f, $"{at}: the Star button {starAt} beside its name {communityAt}");
                // The set choice reads as set while it has the focus a press leaves on it: dark text on the gold fill.
                var low = root.Q<Button>("settings-quality-low");
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = low; low.SendEvent(e); }
                low.Focus();
                for (int i = 0; i < 3; i++) yield return null;
                var ink = low.resolvedStyle.color; var fill = low.resolvedStyle.backgroundColor;
                Assert.Less(Luminance(ink), 0.1f, $"{at}: the selected button's text {ink} is dark");
                Assert.Greater(Luminance(fill), 0.3f, $"{at}: on the gold fill {fill}");
            }
            finally
            {
                settings.Dispose(); nav.Dispose(); hud.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }

    static float Luminance(Color c) => 0.2126f * c.linear.r + 0.7152f * c.linear.g + 0.0722f * c.linear.b;

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;
}
