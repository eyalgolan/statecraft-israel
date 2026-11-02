#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The opening's illustration clips (owner ruling of 2 November 2026, "B: A + one new Yemen shot (~2,424)";
/// docs/event-videos.md): a panel with a clip opens on its archival photograph, and only after the photograph's hold plays
/// the clip in the same frame, labelled "AI-generated illustration" from the cards' own string; a panel without one shows
/// only its photograph; a clip that fails puts the photograph back; changing or closing the panel stops the clip; with
/// video off (CardVideo.ForcedOff: GameRoot passes no player) nothing plays. The clip rows' provenance (the file, its
/// ASSETS.yaml row and the card row it reuses) is checked by tools/tests/test_opening_sources.py, which CI runs.</summary>
public class OpeningClipTests
{
    static OpeningContent Asset => AssetDatabase.LoadAssetAtPath<OpeningContent>("Assets/Resources/OpeningContent.asset");
    static UiText Text(string lang) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
        "Assets/UI/strings." + lang + ".yaml").text);

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly CardVideo? Video;
        public readonly OpeningContent.Document Data;
        public readonly OpeningPresenter Opening;
        public Rig(string lang = "en", bool video = true)
        {
            Data = Asset.Read();
            Video = video ? new CardVideo() : null;
            Opening = new OpeningPresenter(Ui.Root, Asset, Data, Text(lang), _ => { }, _ => { }, Video);
        }
        public Label Credit => Ui.Q<Label>("opening-credit");
        public VisualElement Photo => Ui.Q<VisualElement>("opening-photo");
        public Texture2D? Background => Photo.style.backgroundImage.value.texture;
        public void Dispose() { Opening.Dispose(); Video?.Dispose(); Ui.Dispose(); }
    }

    static void Fail(CardVideo video) =>
        typeof(CardVideo).GetMethod("Fail", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(video, new object[] { "simulated error" });

    [SetUp]
    public void QuietVideoPlayer() => LogAssert.ignoreFailingMessages = true;   // the edit-mode VideoPlayer may log while it prepares

    [Test]
    public void TheShippedPanelsCarryTheRuledClipsAndYemenWaitsForItsOwn()
    {
        var panels = Asset.Read().Panels;
        Assert.AreEqual("law_of_return.mp4", panels[0].Clip?.File, "Europe reuses the law_of_return card's loop (K04a)");
        Assert.AreEqual("K04a", panels[0].Clip?.Shot);
        Assert.AreEqual("iraqi_exodus.mp4", panels[1].Clip?.File, "the Arab countries reuse the iraqi_exodus card's loop (A07)");
        Assert.AreEqual("A07", panels[1].Clip?.Shot);
        Assert.IsNull(panels[2].Clip, "Yemen's clip is not generated yet: photograph only");
    }

    [Test]
    public void APanelWithAClipShowsThePhotographFirstThenTheLabelAndTheClip()
    {
        using var r = new Rig();
        r.Opening.Show(replay: false);
        var panel = r.Data.Panels[0];
        Assert.AreSame(Asset.photos[0], r.Background, "the panel opens on its archival photograph");
        Assert.AreEqual(panel.Image.Credit, r.Credit.text, "with its credit line");
        Assert.AreEqual(panel.Image.Alt, r.Photo.tooltip);
        Assert.IsNull(r.Video!.Current, "no clip during the photograph's hold");
        Assert.IsTrue(r.Opening.ClipWaiting, "the clip waits for the hold");
        Assert.Greater(OpeningPresenter.PhotoHoldMs, 0, "the photograph is held before the clip");

        r.Opening.HoldElapsed();   // what the hold's scheduled item calls
        Assert.AreEqual(CardVideo.Url(panel.Clip!.File), r.Video.Current, "the clip plays in the same frame");
        Assert.AreEqual(Text("en")["event.ai_label"], r.Credit.text, "labelled with the cards' own string");
        Assert.AreEqual("AI-generated illustration", r.Credit.text);
        Assert.AreEqual(panel.Clip.Alt, r.Photo.tooltip, "the clip's alt text");
        Assert.AreSame(Asset.photos[0], r.Background, "the photograph stays until the clip's first frame (CardVideo)");
        Assert.IsFalse(r.Opening.ClipWaiting);
    }

    [Test]
    public void APanelWithoutAClipShowsOnlyThePhotograph()
    {
        using var r = new Rig();
        r.Opening.Show(replay: false);
        r.Ui.Press("opening-next"); r.Ui.Press("opening-next");
        Assert.AreEqual(2, r.Opening.Index, "Yemen");
        Assert.IsFalse(r.Opening.ClipWaiting, "no clip, no hold");
        r.Opening.HoldElapsed();
        Assert.IsNull(r.Video!.Current, "nothing plays");
        Assert.AreEqual(r.Data.Panels[2].Image.Credit, r.Credit.text);
        Assert.AreSame(Asset.photos[2], r.Background);
    }

    [Test]
    public void WithVideoOffAPanelWithAClipShowsOnlyThePhotograph()
    {
        using var r = new Rig(video: false);   // CardVideo.ForcedOff: GameRoot passes no player
        r.Opening.Show(replay: false);
        Assert.IsFalse(r.Opening.ClipWaiting);
        r.Opening.HoldElapsed();
        Assert.AreEqual(r.Data.Panels[0].Image.Credit, r.Credit.text);
        Assert.AreSame(Asset.photos[0], r.Background);
    }

    [Test]
    public void AClipThatFailsFallsBackToThePhotograph()
    {
        using var r = new Rig();
        r.Opening.Show(replay: false);
        r.Opening.HoldElapsed();
        Assert.IsNotNull(r.Video!.Current, "precondition: the clip was asked for");
        // CardVideo's own failure path: what errorReceived (a missing or broken file) and the 8 s prepare timeout call. The
        // edit-mode VideoPlayer's own timing is not under test, as in EventVideoTests.
        Fail(r.Video);
        Assert.IsNull(r.Video.Current, "the failed clip is stopped");
        Assert.AreEqual(r.Data.Panels[0].Image.Credit, r.Credit.text, "the photograph's credit is back");
        Assert.AreEqual(r.Data.Panels[0].Image.Alt, r.Photo.tooltip);
        Assert.AreSame(Asset.photos[0], r.Background, "and the photograph");
        r.Ui.Press("opening-next"); r.Ui.Press("opening-back");
        Assert.IsFalse(r.Opening.ClipWaiting, "a failed clip is not tried again this session");
    }

    [Test]
    public void ChangingOrClosingThePanelStopsTheClip()
    {
        using var r = new Rig();
        r.Opening.Show(replay: false);
        r.Opening.HoldElapsed();
        Assert.IsNotNull(r.Video!.Current, "precondition: Europe's clip plays");
        r.Ui.Press("opening-next");
        Assert.IsNull(r.Video.Current, "Next stops it");
        Assert.AreEqual(r.Data.Panels[1].Image.Credit, r.Credit.text, "the next panel opens on its photograph and credit");
        Assert.AreSame(Asset.photos[1], r.Background);
        Assert.IsTrue(r.Opening.ClipWaiting, "and holds it before its own clip");
        r.Opening.HoldElapsed();
        Assert.AreEqual(CardVideo.Url("iraqi_exodus.mp4"), r.Video.Current);
        r.Ui.Press("opening-back");
        Assert.IsNull(r.Video.Current, "Back stops it");
        Assert.AreSame(Asset.photos[0], r.Background);
        r.Opening.HoldElapsed();
        Assert.IsNotNull(r.Video.Current);
        Assert.IsTrue(r.Opening.Escape());
        Assert.IsNull(r.Video.Current, "Esc stops it");
        Assert.IsFalse(r.Opening.ClipWaiting, "and no clip starts after the opening closed");
        r.Opening.Show(replay: true);
        r.Opening.HoldElapsed();
        Assert.IsNotNull(r.Video.Current);
        r.Ui.Press("opening-skip");
        Assert.IsNull(r.Video.Current, "Skip stops it");
        Assert.IsFalse(r.Opening.ClipWaiting);
    }

    [Test]
    public void InHebrewTheLabelIsTheHebrewString()
    {
        using var r = new Rig("he");
        r.Opening.Show(replay: false);
        Assert.AreEqual(LanguageDirection.LTR, r.Credit.languageDirection, "the photograph's English credit");
        r.Opening.HoldElapsed();
        Assert.AreEqual(Text("he")["event.ai_label"], r.Credit.text);
        Assert.AreEqual("איור שנוצר בבינה מלאכותית", r.Credit.text);
        Assert.AreEqual(LanguageDirection.RTL, r.Credit.languageDirection, "the Hebrew label reads right to left");
        Assert.IsFalse(r.Credit.ClassListContains("opening-source-en"), "and is not set as English");
        r.Ui.Press("opening-next");
        Assert.AreEqual(r.Data.Panels[1].Image.Credit, r.Credit.text);
        Assert.AreEqual(LanguageDirection.LTR, r.Credit.languageDirection, "the next credit is English again");
        Assert.IsTrue(r.Credit.ClassListContains("opening-source-en"));
    }

    [TestCase("file: law_of_return.mp4", "file: ../law_of_return.mp4", TestName = "ClipFileMustBeABareMp4Name")]
    [TestCase("file: law_of_return.mp4", "file: law_of_return.webm", TestName = "ClipFileMustBeAnMp4")]
    [TestCase("shot: K04a", "shot: \"\"", TestName = "ClipNeedsItsShot")]
    [TestCase("      ai_generated: true\n      sensitive: true\n      sensitivity_reviewed: false\n    image:\n      file: opening_europe.jpg",
        "      ai_generated: false\n      sensitive: true\n      sensitivity_reviewed: false\n    image:\n      file: opening_europe.jpg",
        TestName = "ClipMustBeMarkedAiGenerated")]
    [TestCase("      ai_generated: true\n      sensitive: true\n      sensitivity_reviewed: false\n    image:\n      file: opening_europe.jpg",
        "      ai_generated: true\n      sensitive: false\n      sensitivity_reviewed: false\n    image:\n      file: opening_europe.jpg",
        TestName = "AClipOnASensitivePanelMustBeSensitive")]
    [TestCase("alt: \"Illustration: an immigrant ship", "alt: \"An immigrant ship", TestName = "ClipAltMustStartIllustration")]
    [TestCase("      shot: K04a\n", "      shot: K04a\n      colour: sepia\n", TestName = "ClipUnknownFieldFailsClosed")]
    public void AMalformedClipRowFailsTheLoad(string from, string to)
    {
        var yaml = Asset.source.text;
        StringAssert.Contains(from, yaml, "precondition: the shipped row");
        Assert.DoesNotThrow(() => OpeningContent.Parse(yaml));
        Assert.Throws<InvalidOperationException>(() => OpeningContent.Parse(yaml.Replace(from, to)));
    }
}
