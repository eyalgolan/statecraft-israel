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
/// the clip in the same frame, labelled "AI-generated illustration" from the cards' own string; every shipped panel has one
/// (Yemen's is the new shot, O3), and a panel without one shows only its photograph; a clip that fails puts the
/// photograph back; changing or closing the panel stops the clip; with video off (CardVideo.ForcedOff: GameRoot passes no
/// player) nothing plays. The clip rows' provenance (the file, its ASSETS.yaml row and the card row it reuses) is checked
/// by tools/tests/test_opening_sources.py, which CI runs.</summary>
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
        public Label Caption => Ui.Q<Label>("opening-caption");
        public bool CaptionShown => Caption.style.display.value == DisplayStyle.Flex;
        public VisualElement Photo => Ui.Q<VisualElement>("opening-photo");
        public Texture2D? Background => Photo.style.backgroundImage.value.texture;
        public void Dispose() { Opening.Dispose(); Video?.Dispose(); Ui.Dispose(); }
    }

    static void Fail(CardVideo video) =>
        typeof(CardVideo).GetMethod("Fail", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(video, new object[] { "simulated error" });

    [SetUp]
    public void QuietVideoPlayer() => LogAssert.ignoreFailingMessages = true;   // the edit-mode VideoPlayer may log while it prepares

    [Test]
    public void TheShippedPanelsCarryTheRuledClips()
    {
        var panels = Asset.Read().Panels;
        Assert.AreEqual("law_of_return.mp4", panels[0].Clip?.File, "Europe reuses the law_of_return card's loop (K04a)");
        Assert.AreEqual("K04a", panels[0].Clip?.Shot);
        Assert.AreEqual("iraqi_exodus.mp4", panels[1].Clip?.File, "the Arab countries reuse the iraqi_exodus card's loop (A07)");
        Assert.AreEqual("A07", panels[1].Clip?.Shot);
        Assert.AreEqual("Operation Ezra and Nehemiah brought some 120,000 Iraqi Jews to Israel.", panels[1].Caption?.Text,
            "owner, 2 Nov 2026: \"Change to 'brought' (Recommended)\"");
        Assert.AreEqual("opening_yemen.mp4", panels[2].Clip?.File, "Yemen has the ruling's one new shot, made for the opening");
        Assert.AreEqual("O3", panels[2].Clip?.Shot);
        var data = Asset.Read();
        Assert.IsTrue(data.ClipsReviewed, "owner, 2 Nov 2026: \"Approve both\" (the card clips) and \"Approve it\" (Yemen)");
        Assert.IsTrue(panels.All(p => p.Clip!.Sensitive && p.Clip.SensitivityReviewed), "all three stay sensitive: true, now reviewed");
        Assert.IsTrue(data.CaptionsReviewed, "owner, 2 Nov 2026: \"Version 1, fixed (Recommended)\"");
        Assert.IsTrue(panels.All(p => p.Caption != null && p.Caption.HebrewApproved), "a caption for each panel");
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
        Assert.IsFalse(r.CaptionShown, "no caption with the photograph");
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
        Assert.IsTrue(r.CaptionShown, "the panel's caption shows with the clip");
        Assert.AreEqual("About two-thirds of Europe's displaced persons came to Israel.", r.Caption.text);
        Assert.AreEqual(LanguageDirection.LTR, r.Caption.languageDirection);
    }

    [Test]
    public void APanelWithoutAClipShowsOnlyThePhotograph()
    {
        using var r = new Rig();
        r.Data.Panels[2].Clip = null;   // every shipped panel has a clip, so drop Yemen's in memory (read at Show)
        r.Opening.Show(replay: false);
        r.Ui.Press("opening-next"); r.Ui.Press("opening-next");
        Assert.AreEqual(2, r.Opening.Index, "Yemen");
        Assert.IsFalse(r.Opening.ClipWaiting, "no clip, no hold");
        r.Opening.HoldElapsed();
        Assert.IsNull(r.Video!.Current, "nothing plays");
        Assert.AreEqual(r.Data.Panels[2].Image.Credit, r.Credit.text);
        Assert.AreSame(Asset.photos[2], r.Background);
        Assert.IsNotNull(r.Data.Panels[2].Caption, "the panel still has its caption");
        Assert.IsFalse(r.CaptionShown, "and it is not shown without a clip");
    }

    [Test]
    public void YemenPlaysItsOwnShotWithItsCaption()
    {
        using var r = new Rig();
        r.Opening.Show(replay: false);
        r.Ui.Press("opening-next"); r.Ui.Press("opening-next");
        Assert.AreEqual(2, r.Opening.Index, "Yemen");
        Assert.AreSame(Asset.photos[2], r.Background, "the panel opens on its archival photograph");
        Assert.AreEqual(r.Data.Panels[2].Image.Credit, r.Credit.text);
        Assert.IsFalse(r.CaptionShown, "no caption with the photograph");
        Assert.IsTrue(r.Opening.ClipWaiting, "the clip waits for the hold");
        r.Opening.HoldElapsed();
        Assert.AreEqual(CardVideo.Url("opening_yemen.mp4"), r.Video!.Current, "the new shot plays in the same frame");
        Assert.AreEqual("AI-generated illustration", r.Credit.text);
        Assert.AreEqual(r.Data.Panels[2].Clip!.Alt, r.Photo.tooltip, "the clip's alt text");
        Assert.IsTrue(r.CaptionShown, "Yemen's caption shows with its clip");
        Assert.AreEqual("Operation On Wings of Eagles flew about 49,000 Yemenite Jews.", r.Caption.text);
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
        Assert.IsFalse(r.CaptionShown, "the caption goes with the clip");
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
        Assert.IsFalse(r.CaptionShown, "and hides its caption");
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
        Assert.AreEqual(r.Data.Panels[0].Image.AltHe, r.Photo.tooltip);
        Assert.AreEqual(LanguageDirection.LTR, r.Credit.languageDirection, "the photograph's English credit");
        r.Opening.HoldElapsed();
        Assert.AreEqual(r.Data.Panels[0].Clip!.AltHe, r.Photo.tooltip);
        Assert.AreEqual(Text("he")["event.ai_label"], r.Credit.text);
        Assert.AreEqual("איור שנוצר בבינה מלאכותית", r.Credit.text);
        Assert.AreEqual(LanguageDirection.RTL, r.Credit.languageDirection, "the Hebrew label reads right to left");
        Assert.IsFalse(r.Credit.ClassListContains("opening-source-en"), "and is not set as English");
        Assert.IsTrue(r.CaptionShown);
        Assert.AreEqual("כשני שלישים מעקורי אירופה עלו לישראל לאחר קום המדינה.", r.Caption.text, "the owner-read Hebrew caption");
        Assert.AreEqual(LanguageDirection.RTL, r.Caption.languageDirection);
        r.Ui.Press("opening-next");
        Assert.IsFalse(r.CaptionShown);
        r.Opening.HoldElapsed();
        Assert.AreEqual("מבצע עזרא ונחמיה העלה כ־120,000 מיהודי עיראק.", r.Caption.text);
        Assert.AreEqual(LanguageDirection.RTL, r.Caption.languageDirection);
        r.Ui.Press("opening-next");
        Assert.IsFalse(r.CaptionShown, "Yemen holds its photograph first, with no caption");
        Assert.AreEqual(r.Data.Panels[2].Image.Credit, r.Credit.text);
        Assert.AreEqual(LanguageDirection.LTR, r.Credit.languageDirection, "the next credit is English again");
        Assert.IsTrue(r.Credit.ClassListContains("opening-source-en"));
        r.Opening.HoldElapsed();
        Assert.AreEqual("מבצע על כנפי נשרים הטיס מעדן כ־49,000 מיהודי תימן.", r.Caption.text, "Yemen's owner-read Hebrew caption");
        Assert.AreEqual(LanguageDirection.RTL, r.Caption.languageDirection);
    }

    [Test]
    public void HebrewMediaAltFallsBackToEnglishWhenMissing()
    {
        using var r = new Rig("he");
        r.Data.Panels[0].Image.AltHe = "";
        r.Data.Panels[0].Clip!.AltHe = "";
        r.Opening.Show(replay: false);
        Assert.AreEqual(r.Data.Panels[0].Image.Alt, r.Photo.tooltip);
        r.Opening.HoldElapsed();
        Assert.AreEqual(r.Data.Panels[0].Clip.Alt, r.Photo.tooltip);
    }

    [TestCase("file: law_of_return.mp4", "file: ../law_of_return.mp4", TestName = "ClipFileMustBeABareMp4Name")]
    [TestCase("file: law_of_return.mp4", "file: law_of_return.webm", TestName = "ClipFileMustBeAnMp4")]
    [TestCase("shot: K04a", "shot: \"\"", TestName = "ClipNeedsItsShot")]
    [TestCase("      ai_generated: true\n      sensitive: true\n", "      ai_generated: false\n      sensitive: true\n",
        TestName = "ClipMustBeMarkedAiGenerated")]
    [TestCase("      sensitive: true\n      # The owner's sensitivity review of this clip in the opening: owner, 2 Nov 2026: \"Approve both\".\n      sensitivity_reviewed: true",
        "      sensitive: false\n      sensitivity_reviewed: false", TestName = "AClipOnASensitivePanelMustBeSensitive")]
    [TestCase("    caption:\n      text: \"About two-thirds of Europe's displaced persons came to Israel.\"\n      text_he: \"כשני שלישים מעקורי אירופה עלו לישראל לאחר קום המדינה.\"\n      he_review: approved\n",
        "", TestName = "AClipNeedsItsCaption")]
    [TestCase("      text_he: \"כשני שלישים מעקורי אירופה עלו לישראל לאחר קום המדינה.\"", "      text_he: \"\"", TestName = "ACaptionNeedsItsHebrew")]
    [TestCase("      he_review: approved\n    image:\n      file: opening_europe.jpg", "      he_review: yes\n    image:\n      file: opening_europe.jpg",
        TestName = "ACaptionHeReviewIsPendingOrApproved")]
    [TestCase("alt: \"Illustration: an immigrant ship", "alt: \"An immigrant ship", TestName = "ClipAltMustStartIllustration")]
    [TestCase("      alt_he_review: agent-checked\n      ai_generated: true", "      alt_he_review: pending\n      ai_generated: true", TestName = "ClipAltHeReviewMustBeAgentChecked")]
    [TestCase("      alt_he_review: agent-checked\n      crop:", "      alt_he_review: \"\"\n      crop:", TestName = "PhotoAltHeNeedsItsReview")]
    [TestCase("      shot: K04a\n", "      shot: K04a\n      colour: sepia\n", TestName = "ClipUnknownFieldFailsClosed")]
    public void AMalformedClipRowFailsTheLoad(string from, string to)
    {
        var yaml = Asset.source.text;
        StringAssert.Contains(from, yaml, "precondition: the shipped row");
        Assert.DoesNotThrow(() => OpeningContent.Parse(yaml));
        Assert.Throws<InvalidOperationException>(() => OpeningContent.Parse(yaml.Replace(from, to)));
    }
}
