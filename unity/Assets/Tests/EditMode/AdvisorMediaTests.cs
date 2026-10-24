#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using Statecraft.Game;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Spec 12 and Review Focus 1 and 2: the picture is silent and the narration a separate file; they start in the
/// same tick only once both are ready; a drift past 0.1 s is corrected to the narration; live captions follow the
/// narration's position; one press or a covering layer stops both; the card's own loop is never stopped by the advisor.</summary>
public class AdvisorMediaTests
{
    sealed class FakePicture : AdvisorMedia.IPicture
    {
        public bool Ready { get; set; }
        public double Time { get; set; }
        public bool Started, Stopped; public int Loads; public readonly List<double> Seeks = new List<double>();
        public void Load(string url, VisualElement target, Action<string> failed) { Loads++; }
        public void Start() { Started = true; Stopped = false; }
        public void Stop() => Stopped = true;
        public void Seek(double t) { Seeks.Add(t); Time = t; }
    }

    sealed class FakeOutput : DeclarationRecording.IOutput
    {
        public bool Decoded = true; public bool IsPlaying { get; set; } public float Position { get; set; }
        public bool Played, Stopped;
        public bool? Ready(AudioClip clip) => Decoded;
        public void Play(AudioClip clip) { Played = true; Stopped = false; IsPlaying = true; }
        public void Stop() { Stopped = true; IsPlaying = false; }
        public void Dispose() { }
    }

    sealed class Done : DeclarationRecording.IRequest { public ulong Bytes => 0; public void Abort() { } }
    static DeclarationRecording.Fetch Immediate => (url, done) => { done(AudioClip.Create("n", 4410, 1, 44100, false), null); return new Done(); };

    static readonly AdvisorCatalog.Media Row = new AdvisorCatalog.Media
    {
        Picture = "x.mp4", Narration = new Dictionary<string, string> { ["en"] = "x.en.wav" },
        Cues = new Dictionary<string, List<double>> { ["en"] = new List<double> { 0.0, 2.5 } }, Label = "lab",
    };
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text +
        "\nadvisor.t.teach.text: \"Teach.\"\nadvisor.t.warn.text: \"Warn.\"\nadvisor.t.crit.text: \"Critical.\"\nadvisor.x.cue.1: \"First.\"\nadvisor.x.cue.2: \"Second.\"\nlab: \"Label.\"\n" +
        "advisor.t.teach.cue.1: \"Teach one.\"\nadvisor.t.teach.cue.2: \"Teach two.\"\n");

    readonly List<AdvisorMedia> owned = new List<AdvisorMedia>();
    [TearDown] public void Clean() { foreach (var media in owned) media.Dispose(); owned.Clear(); }

    (AdvisorMedia media, FakePicture pic, FakeOutput output, Label caption) Make()
    {
        var pic = new FakePicture(); var output = new FakeOutput(); var caption = new Label();
        var media = new AdvisorMedia(Text, pic, null, Immediate, output);
        owned.Add(media);
        media.Play(Row, "advisor.x", false, new VisualElement(), caption, () => { }, _ => { });
        return (media, pic, output, caption);
    }

    [Test]
    public void PictureAndNarrationStartTogether()
    {
        var (media, pic, output, _) = Make();
        media.Tick();
        Assert.IsFalse(pic.Started || output.Played, "the picture is not ready: neither starts");
        pic.Ready = true;
        media.Tick();
        Assert.IsTrue(pic.Started && output.Played, "both in the same tick");
    }

    [Test]
    public void ADriftIsCorrectedToTheNarration()
    {
        var (media, pic, output, _) = Make();
        pic.Ready = true; media.Tick();
        output.Position = 3.0f; pic.Time = 2.8; media.Tick();
        CollectionAssert.AreEqual(new[] { 3.0 }, pic.Seeks);
        output.Position = 3.5f; pic.Time = 3.45; media.Tick();
        Assert.AreEqual(1, pic.Seeks.Count, "within 0.1 s: left alone");
    }

    [Test]
    public void CaptionsFollowTheNarrationsPosition()
    {
        var (media, pic, output, caption) = Make();
        pic.Ready = true; media.Tick();
        output.Position = 0.1f; media.Tick();
        Assert.AreEqual("First.", caption.text);
        output.Position = 2.6f; media.Tick();
        Assert.AreEqual("Second.", caption.text);
        Assert.AreEqual(-1, AdvisorCaptions.At(new[] { 0.5 }, 0.2));
    }

    [Test]
    public void OnePressStopsBoth()
    {
        var (media, pic, output, caption) = Make();
        pic.Ready = true; media.Tick(); output.Position = 0.1f; media.Tick();
        media.Stop();
        Assert.IsTrue(pic.Stopped && output.Stopped);
        Assert.AreEqual("", caption.text);
        Assert.IsFalse(media.Playing);
    }

    /// <summary>Review Focus 1: the advisor borrows the card's player only while no card is open; whatever stops the advisor's
    /// clip never stops the card's own illustration loop on the same player.</summary>
    [Test]
    public void ALayerOverAPlayingClipStopsBothAndLeavesTheCardsLoopAlone()
    {
        bool previous = LogAssert.ignoreFailingMessages;
        try
        {
            LogAssert.ignoreFailingMessages = true; // only the deliberately nonexistent URL ownership probe
            using var video = new CardVideo();
            var header = new VisualElement(); var clip = new ClipPicture(video);
            clip.Load("advisor-clip.mp4", header, _ => { });
            video.Play("card-loop.mp4", header, _ => { });
            clip.Stop();
            Assert.AreEqual("card-loop.mp4", video.Current, "the card's loop is not the advisor's to stop");
        }
        finally { LogAssert.ignoreFailingMessages = previous; }

        using var rig = new AdvisorPopupTests.Rig(text: Text);
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, pic, null, Immediate, output);
        rig.Advisor.NewGame();
        rig.Advisor.Watch(Row, "advisor.x");
        rig.Advisor.Media.Tick();
        Assert.IsTrue(rig.Advisor.Media.Playing, "precondition");
        rig.HudRoot.AddToClassList(EventCardPresenter.OpenClass); ScreenNav.DisableCovered(rig.Ui.Root);
        Assert.IsTrue(pic.Stopped && output.Stopped, "a layer over it stops picture and narration at once");
    }
    [TestCase(HudTier.OneRow, 506f, true)]     // 1920x1080
    [TestCase(HudTier.OneRow, 400f, false)]    // 844x390: a phone held sideways
    [TestCase(HudTier.TwoRow, 746f, true)]     // 960x1080
    [TestCase(HudTier.ThreeRow, 1164f, false)] // an upright phone: always the overlay
    public void InlineWhereTheSlotHasRoom(HudTier tier, float slot, bool inline) => Assert.AreEqual(inline, AdvisorMedia.Inline(tier, slot));

    [Test]
    public void WatchPlaysInThePopupWithCaptionsInTheTextsPlace()
    {
        using var rig = new AdvisorPopupTests.Rig(text: Text);
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, pic, null, Immediate, output);
        rig.Advisor.ForceInline = true;                                   // EditMode's panel has no real slot height
        rig.Advisor.NewGame();
        rig.Advisor.Watch(Row, "advisor.x");
        Assert.IsTrue(rig.Ui.Q<VisualElement>("advisor-popup").ClassListContains("advisor-watching"));
        rig.Ui.Press("advisor-got-it");
        Assert.IsTrue(output.Stopped, "any button stops it");
        Assert.IsFalse(rig.Ui.Q<VisualElement>("advisor-popup").ClassListContains("advisor-watching"));
    }

    [Test]
    public void TheOverlayClosesWithItsXOrEscapeAndStopsBoth()
    {
        using var rig = new AdvisorPopupTests.Rig(text: Text);
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, pic, null, Immediate, output);
        rig.Advisor.ForceInline = false;
        rig.Advisor.NewGame();
        rig.Advisor.Watch(Row, "advisor.x");
        Assert.IsTrue(rig.HudRoot.ClassListContains("advisor-overlay-open"));
        Assert.IsTrue(rig.Advisor.CloseOverlay());
        Assert.IsTrue(pic.Stopped && output.Stopped);
        Assert.IsFalse(rig.HudRoot.ClassListContains("advisor-overlay-open"));
        Assert.IsFalse(rig.Advisor.CloseOverlay(), "nothing to close");
    }
    [Test]
    public void ConstructionDoesNotFetchOrPlay()
    {
        int fetched = 0;
        var pic = new FakePicture(); var output = new FakeOutput();
        using var media = new AdvisorMedia(Text, pic, null, (url, done) => { fetched++; return new Done(); }, output);
        Assert.AreEqual(0, fetched); Assert.AreEqual(0, pic.Loads);
        Assert.IsFalse(output.Played || pic.Started || media.Playing);
    }

    [Test]
    public void AReadyPictureWaitsForTheAudioToDecode()
    {
        var (media, pic, output, _) = Make();
        pic.Ready = true; output.Decoded = false; media.Tick();
        Assert.IsFalse(pic.Started || output.Played);
        output.Decoded = true; media.Tick();
        Assert.IsTrue(pic.Started && output.Played);
    }

    [Test]
    public void AStoppedFetchCannotRestartPlayback()
    {
        Action<AudioClip?, string?>? done = null;
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput(); var caption = new Label();
        using var media = new AdvisorMedia(Text, pic, null, (url, callback) => { done = callback; return new Done(); }, output);
        media.Play(Row, "advisor.x", false, new VisualElement(), caption, () => Assert.Fail("stopped"), _ => Assert.Fail("stopped"));
        media.Stop();
        done!(AudioClip.Create("late", 4410, 1, 44100, false), null);
        media.Tick();
        Assert.IsFalse(media.Playing || output.Played || pic.Started); Assert.AreEqual("", caption.text);
    }

    [Test]
    public void AnUnmovingFetchFailsOnceWithinTheExistingStallBudget()
    {
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput(); int failed = 0;
        using var media = new AdvisorMedia(Text, pic, null, (url, done) => new Done(), output);
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ADVISOR MEDIA failed.*fetch.*"));
        media.Play(Row, "advisor.x", false, new VisualElement(), new Label(), () => Assert.Fail("not loaded"), _ => failed++);
        for (int i = 0; i <= DeclarationRecording.StallTimeoutMs / AdvisorMedia.TickMs; i++) media.Tick();
        Assert.AreEqual(1, failed); Assert.IsFalse(media.Playing); Assert.IsTrue(pic.Stopped && output.Stopped);
    }
    [Test]
    public void AButtonStopsMediaBeforeItOpensAnotherWindow()
    {
        using var rig = new AdvisorPopupTests.Rig(text: Text);
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, pic, null, Immediate, output);
        rig.Advisor.ForceInline = true; rig.Advisor.NewGame(); rig.Advisor.Watch(Row, "advisor.x"); rig.Advisor.Media.Tick();
        Assert.IsTrue(rig.Advisor.Media.Playing);
        rig.Ui.Press("nav-budget");
        Assert.IsTrue(pic.Stopped && output.Stopped); Assert.AreEqual("budget", rig.Nav.Current);
    }

    [Test]
    public void ASecondWatchPressStopsWithoutRestarting()
    {
        var catalog = AdvisorPopupTests.Fixture.Replace("level: new, actions: [{ open_screen: budget }]",
            "level: new, media: { picture: x.mp4, narration: { en: x.en.wav }, cues: { en: [0, 2.5] }, label: lab }");
        using var rig = new AdvisorPopupTests.Rig(catalog: catalog, text: Text);
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, pic, null, Immediate, output);
        rig.Advisor.ForceInline = true; rig.Advisor.NewGame();
        rig.Ui.Press("advisor-watch"); rig.Advisor.Media.Tick();
        Assert.IsTrue(rig.Advisor.Media.Playing);
        rig.Ui.Press("advisor-watch");
        Assert.IsFalse(rig.Advisor.Media.Playing); Assert.AreEqual(1, pic.Loads);
    }

    [UnityTest]
    public System.Collections.IEnumerator InlineKeepsItsXAndAPhoneStopsItBeforeUsingTheOverlay()
    {
        var wide = new RenderTexture(1280, 720, 0); var phone = new RenderTexture(390, 844, 0);
        var panel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); panel.targetTexture = wide;
        var go = new GameObject("AdvisorMediaGeometry"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = panel;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
        var root = doc.rootVisualElement; var hudRoot = root.Q("root"); hudRoot.RemoveFromClassList("loading");
        var bridge = new SimBridge(GameSession.New(ContentLoader.Load(Paths.Content), GoldenRun.Seed, Mode.Historical), Text);
        var hud = new HudPresenter(root, bridge, Text); var nav = new ScreenNav(root);
        var card = new EventCardPresenter(root, bridge, Text, null); card.Collapse(takeFocus: false);
        var session = new AdvisorSession(AdvisorCatalog.Parse(AdvisorPopupTests.Fixture), new AdvisorSettings(null), true);
        var advisor = new AdvisorPresenter(root, bridge, Text, session, hooks: new AdvisorHooks { Card = card });
        var pic = new FakePicture { Ready = true }; var output = new FakeOutput();
        advisor.Media = new AdvisorMedia(Text, pic, null, Immediate, output);
        try
        {
            advisor.NewGame(); for (int i = 0; i < 6; i++) yield return null;
            advisor.ForceInline = true; advisor.Watch(Row, "advisor.x"); advisor.Media.Tick(); output.Position = 0.1f;
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsTrue(AdvisorTargets.Shown(root.Q("advisor-close")), "inline keeps X usable");
            advisor.ForceInline = null; panel.targetTexture = phone;
            for (int i = 0; i < 6; i++) yield return null;
            Assert.IsTrue(hudRoot.ClassListContains(HudPresenter.ThreeRowClass));
            Assert.IsFalse(advisor.Media.Playing, "inline stops when the new tier has no room; no automatic replay");
            advisor.ForceInline = false; advisor.Watch(Row, "advisor.x"); advisor.Media.Tick(); output.Position = 0.1f;
            for (int i = 0; i < 6; i++) yield return null;
            var picture = root.Q("advisor-overlay-media").worldBound;
            Assert.Greater(picture.height, 0); Assert.AreEqual(EventCardPresenter.MediaAspect, picture.width / picture.height, 0.02f);
            Assert.GreaterOrEqual(root.Q("advisor-overlay-caption").resolvedStyle.fontSize * root.panel.scaledPixelsPerPoint, HudPresenter.TextFloorPx - 0.01f);
        }
        finally
        {
            advisor.Dispose(); card.Dispose(); nav.Dispose(); hud.Dispose();
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(panel);
            wide.Release(); phone.Release(); UnityEngine.Object.DestroyImmediate(wide); UnityEngine.Object.DestroyImmediate(phone);
        }
    }

    // The owner's ruling of 24 October 2026 (#1547; spec 12; plan Task 25), per line: play the clip, or a supplied still,
    // when present; otherwise keep the advisor's portrait and play the narration with live captions when the current
    // language has it; otherwise the portrait and the text, with no Watch. The disclosure (#1530, OQ-16) is one small line
    // under the live caption.

    const string Disclosure = "AI animation and voice. Fictional dialogue.";
    const string VoiceOnly = "{ narration: { en: t.en.wav }, cues: { en: [0, 2.5] }, label: advisor.media_label }";
    static string TeachWith(string media) =>
        AdvisorPopupTests.Fixture.Replace("level: new, actions: [{ open_screen: budget }]", "level: new, media: " + media);
    static AdvisorCatalog.Media Voice(string label = "lab") => new AdvisorCatalog.Media
    {
        Narration = new Dictionary<string, string> { ["en"] = "x.en.wav" },
        Cues = new Dictionary<string, List<double>> { ["en"] = new List<double> { 0.0, 2.5 } }, Label = label,
    };
    static AdvisorCatalog.Media Picture(string file, string label = "lab") => new AdvisorCatalog.Media
    {
        Picture = file, Narration = new Dictionary<string, string> { ["en"] = "x.en.wav" },
        Cues = new Dictionary<string, List<double>> { ["en"] = new List<double> { 0.0, 2.5 } }, Label = label,
    };

    [Test]
    public void ANarrationOnlyRowPlaysTheVoiceWithNoPicturePlayer()
    {
        var clip = new FakePicture(); var still = new FakePicture(); var output = new FakeOutput(); var caption = new Label();
        var media = new AdvisorMedia(Text, clip, still, Immediate, output); owned.Add(media);
        media.Play(Voice(), "advisor.x", false, new VisualElement(), caption, () => { }, why => Assert.Fail("a voice-only line plays: " + why));
        media.Tick();
        Assert.IsTrue(output.Played && media.Playing, "the voice starts with no picture to wait for");
        Assert.AreEqual(0, clip.Loads + still.Loads, "no picture player is used");
        output.Position = 0.1f; media.Tick();
        Assert.AreEqual("First.", caption.text);
        output.Position = 2.6f; media.Tick();
        Assert.AreEqual("Second.", caption.text);
        media.Stop();
        Assert.IsTrue(output.Stopped); Assert.AreEqual("", caption.text); Assert.IsFalse(media.Playing);
    }

    [Test]
    public void ASuppliedStillIsAPictureAndPlaysAsOne()
    {
        using var rig = new AdvisorPopupTests.Rig(text: Text);
        var clip = new FakePicture { Ready = true }; var still = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, clip, still, Immediate, output);
        rig.Advisor.ForceInline = true; rig.Advisor.NewGame();
        rig.Advisor.Watch(Picture("x.jpg"), "advisor.x"); rig.Advisor.Media.Tick();
        Assert.AreEqual(1, still.Loads); Assert.AreEqual(0, clip.Loads);
        Assert.IsTrue(still.Started && output.Played);
        Assert.IsTrue(rig.Ui.Q<VisualElement>("advisor-popup").ClassListContains("advisor-watching"), "the still takes the portrait row's place");
    }

    [Test]
    public void ANarrationOnlyLineKeepsThePortraitAndSpeaksWithLiveCaptions()
    {
        using var rig = new AdvisorPopupTests.Rig(catalog: TeachWith(VoiceOnly), text: Text);
        var clip = new FakePicture(); var still = new FakePicture(); var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, clip, still, Immediate, output);
        rig.Advisor.ForceInline = false;                                  // where a picture would take the overlay
        rig.Advisor.NewGame();
        Assert.AreNotEqual(DisplayStyle.None, rig.Ui.Q<Button>("advisor-watch").style.display.value, "a voice is playable: Watch shows");
        rig.Ui.Press("advisor-watch"); rig.Advisor.Media!.Tick();
        Assert.IsTrue(output.Played && rig.Advisor.Media.Playing);
        var popup = rig.Ui.Q<VisualElement>("advisor-popup");
        Assert.IsFalse(popup.ClassListContains("advisor-watching"), "the portrait row stays");
        Assert.IsFalse(rig.HudRoot.ClassListContains("advisor-overlay-open"), "no overlay without a picture");
        Assert.AreEqual(0, clip.Loads + still.Loads, "the card's player is not borrowed");
        output.Position = 0.1f; rig.Advisor.Media.Tick();
        Assert.AreEqual("Teach one.", rig.Ui.Q<Label>("advisor-text").text, "the live caption takes the text's place");
        Assert.AreEqual(Disclosure, rig.Ui.Q<Label>("advisor-media-label").text);
        rig.Ui.Press("advisor-got-it");
        Assert.IsTrue(output.Stopped, "any popup button stops the voice");
        Assert.IsFalse(rig.Advisor.Media.Playing);
    }

    [TestCase("")]                                                                              // no media row
    [TestCase("{ picture: x.mp4, narration: { he: x.he.wav }, cues: { he: [0] }, label: advisor.media_label }")] // nothing in English
    public void ALineWithNothingToPlayHasNoWatchAndKeepsThePortraitAndText(string media)
    {
        using var rig = new AdvisorPopupTests.Rig(catalog: media == "" ? AdvisorPopupTests.Fixture : TeachWith(media), text: Text);
        var clip = new FakePicture { Ready = true }; var still = new FakePicture { Ready = true }; var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, clip, still, Immediate, output);
        rig.Advisor.ForceInline = true; rig.Advisor.NewGame();
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Q<Button>("advisor-watch").style.display.value, "no Watch control");
        if (rig.Session.Catalog.Messages.First(m => m.Id == "t.teach").Media is AdvisorCatalog.Media row) rig.Advisor.Watch(row, "advisor.t.teach");
        Assert.IsFalse(rig.Advisor.Media!.Playing || output.Played); Assert.AreEqual(0, clip.Loads + still.Loads);
        Assert.AreEqual("Teach.", rig.Ui.Q<Label>("advisor-text").text);
        Assert.IsFalse(rig.Ui.Q<VisualElement>("advisor-popup").ClassListContains("advisor-watching"));
        Assert.IsFalse(rig.HudRoot.ClassListContains("advisor-overlay-open"));
    }

    [Test]
    public void AVoiceOnlyFailureHidesThatLinesWatchByItsOwnFile()
    {
        using var rig = new AdvisorPopupTests.Rig(catalog: TeachWith(VoiceOnly), text: Text);
        var output = new FakeOutput();
        rig.Advisor.Media = new AdvisorMedia(Text, new FakePicture(), new FakePicture(), (url, done) => { done(null, "404"); return new Done(); }, output);
        rig.Advisor.NewGame();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ADVISOR MEDIA failed t\\.en\\.wav: fetch"));
        rig.Ui.Press("advisor-watch");
        CollectionAssert.AreEquivalent(new[] { "t.en.wav" }, rig.Session.MediaFailed, "never an empty picture name that would hide every voice-only line");
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Q<Button>("advisor-watch").style.display.value);
    }

    /// <summary>OQ-16 (#1530): the disclosure is the exact text, one small muted line at the bar's small text size, under
    /// the live caption, never over the picture: under the voice's caption, the inline picture's and the overlay's.</summary>
    [UnityTest]
    public System.Collections.IEnumerator TheDisclosureIsSmallAndUnderTheCaptionWhereverItPlays()
    {
        Assert.AreEqual(Disclosure, Text["advisor.media_label"]);
        var wide = new RenderTexture(1280, 720, 0);
        var panel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); panel.targetTexture = wide;
        var go = new GameObject("AdvisorDisclosure"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = panel;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
        var root = doc.rootVisualElement; var hudRoot = root.Q("root"); hudRoot.RemoveFromClassList("loading");
        var bridge = new SimBridge(GameSession.New(ContentLoader.Load(Paths.Content), GoldenRun.Seed, Mode.Historical), Text);
        var hud = new HudPresenter(root, bridge, Text); var nav = new ScreenNav(root);
        var card = new EventCardPresenter(root, bridge, Text, null); card.Collapse(takeFocus: false);
        var session = new AdvisorSession(AdvisorCatalog.Parse(TeachWith(VoiceOnly)), new AdvisorSettings(null), true);
        var advisor = new AdvisorPresenter(root, bridge, Text, session, hooks: new AdvisorHooks { Card = card });
        var output = new FakeOutput();
        advisor.Media = new AdvisorMedia(Text, new FakePicture { Ready = true }, new FakePicture { Ready = true }, Immediate, output);
        void Small(string where, string label, string caption, string? picture)
        {
            var l = root.Q<Label>(label); var c = root.Q<Label>(caption);
            Assert.AreEqual(Disclosure, l.text, where);
            Assert.IsTrue(AdvisorTargets.Shown(l), where + ": the disclosure shows");
            Assert.GreaterOrEqual(l.worldBound.yMin, c.worldBound.yMax - 0.5f, where + ": under the live caption");
            if (picture != null) Assert.GreaterOrEqual(l.worldBound.yMin, root.Q(picture).worldBound.yMax - 0.5f, where + ": never over the picture");
            Assert.AreEqual(HudPresenter.SmallTextSize(HudPresenter.ScreenPxPerPanelPx(root.panel)), l.resolvedStyle.fontSize, 0.01f, where + ": the bar's small text size");
            Assert.Less(l.resolvedStyle.fontSize, c.resolvedStyle.fontSize, where + ": smaller than the caption");
        }
        try
        {
            advisor.NewGame(); for (int i = 0; i < 6; i++) yield return null;
            advisor.Watch(session.Catalog.Messages.First(m => m.Id == "t.teach").Media!, "advisor.t.teach"); advisor.Media.Tick(); output.Position = 0.1f; advisor.Media.Tick();
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsTrue(AdvisorTargets.Shown(root.Q("advisor-portrait")), "a voice-only line keeps the portrait");
            Small("voice only", "advisor-media-label", "advisor-text", null);

            advisor.ForceInline = true; advisor.Watch(Picture("x.mp4", "advisor.media_label"), "advisor.x"); advisor.Media.Tick(); output.Position = 0.1f; advisor.Media.Tick();
            for (int i = 0; i < 4; i++) yield return null;
            Small("inline picture", "advisor-media-label", "advisor-text", "advisor-media");

            advisor.ForceInline = false; advisor.Watch(Picture("x.mp4", "advisor.media_label"), "advisor.x"); advisor.Media.Tick(); output.Position = 0.1f; advisor.Media.Tick();
            for (int i = 0; i < 4; i++) yield return null;
            Small("overlay", "advisor-overlay-label", "advisor-overlay-caption", "advisor-overlay-media");
        }
        finally
        {
            advisor.Dispose(); card.Dispose(); nav.Dispose(); hud.Dispose();
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(panel);
            wide.Release(); UnityEngine.Object.DestroyImmediate(wide);
        }
    }
}
