#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.UI;
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
        "\nadvisor.t.teach.text: \"Teach.\"\nadvisor.t.warn.text: \"Warn.\"\nadvisor.t.crit.text: \"Critical.\"\nadvisor.x.cue.1: \"First.\"\nadvisor.x.cue.2: \"Second.\"\nlab: \"Label.\"\n");

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

}
