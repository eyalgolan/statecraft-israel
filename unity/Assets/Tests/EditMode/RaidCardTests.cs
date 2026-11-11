#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The Tel Aviv raid card on New game (owner, 8 Nov 2026): its strict reader, its review gate and the card itself.
/// Fixtures live in memory only: none is a human review of the shipped file.</summary>
public class RaidCardTests
{
    static string Shipped => AssetDatabase.LoadAssetAtPath<TextAsset>(RaidCardContent.SourcePath).text;
    static UiText Text(string lang) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
        "Assets/UI/strings." + lang + ".yaml").text);

    static RaidCardContent.Document ReviewedFixture()
    {
        var data = RaidCardContent.Parse(Shipped);
        data.Status = Statecraft.Sim.ReleaseGate.ReviewedStatus;
        foreach (var source in data.Sources) source.Checked = "2026-11-08";
        data.HeReview = OpeningContent.HeApproved;
        data.Clip.SensitivityReviewed = true;
        return data;   // memory only: a fixture is never a human sign-off on the shipped file
    }

    [Test]
    public void TheShippedCardIsReviewedSoAPlayerBuildShowsIt()
    {
        var data = RaidCardContent.Load();
        Assert.IsNotNull(data, "Resources/" + RaidCardContent.ResourcePath + " loads");
        // Owner, 10 November 2026: "Approve text, HE, clip", and "Approve as is" for the Hebrew (native read, #564).
        Assert.AreEqual(Statecraft.Sim.ReleaseGate.ReviewedStatus, data!.Status, "the owner reviewed the card");
        Assert.AreEqual(OpeningContent.HeApproved, data.HeReview, "the owner's native read of the Hebrew");
        Assert.IsTrue(data.Clip.SensitivityReviewed, "the owner's sensitivity review of the clip");
        Assert.IsTrue(data.Reviewed);
        Assert.IsTrue(RaidCardContent.MayPresent(data, editorPreview: false), "a player build shows the reviewed card");
        Assert.IsTrue(RaidCardContent.MayPresent(data, editorPreview: true));
        Assert.IsTrue(data.Sources.Count >= 2 && data.Sources.All(s => s.IsChecked), "every source carries its checked date");
        Assert.IsTrue(data.Clip.AiGenerated);
        Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, "video", data.Clip.File)),
            "the clip ships in StreamingAssets/video");
    }

    [Test]
    public void AReviewedInMemoryFixtureMayBePresentedAndEachMissingReviewBlocksIt()
    {
        Assert.IsTrue(RaidCardContent.MayPresent(ReviewedFixture(), editorPreview: false), "an in-memory gate fixture, not a review");
        var draft = ReviewedFixture(); draft.Status = "draft";
        Assert.IsFalse(draft.Reviewed, "draft text");
        var unchecked_ = ReviewedFixture(); unchecked_.Sources[0].Checked = "";
        Assert.IsFalse(unchecked_.Reviewed, "an unchecked source");
        var impossible = ReviewedFixture(); impossible.Sources[0].Checked = "2026-02-30";
        Assert.IsFalse(impossible.Reviewed, "an impossible day is not a checked date");
        var hebrew = ReviewedFixture(); hebrew.HeReview = OpeningContent.HePending;
        Assert.IsFalse(hebrew.Reviewed, "the Hebrew needs its native read");
        var clip = ReviewedFixture(); clip.Clip.SensitivityReviewed = false;
        Assert.IsFalse(clip.Reviewed, "the clip needs the owner's sensitivity review");
        Assert.IsTrue(RaidCardContent.MayPresent(clip, editorPreview: true), "the Editor preview relaxes only the reviews");
    }

    [TestCase("status: sensitivity-reviewed", "status: sensitivity-reviewed\nextra: 1", TestName = "an unknown key")]
    [TestCase("status: sensitivity-reviewed", "status: sensitivity-reviewed\nstatus: draft", TestName = "a repeated key")]
    [TestCase("he_review: approved", "he_review: maybe", TestName = "a he_review that is neither pending nor approved")]
    [TestCase("title_he: ", "# title_he: ", TestName = "Hebrew that is not complete")]
    [TestCase("  ai_generated: true", "  ai_generated: false", TestName = "an AI clip not marked as one")]
    [TestCase("  sensitive: true", "  sensitive: false", TestName = "a clip of the raid's aftermath not marked sensitive")]
    [TestCase("  alt: \"Illustration: ", "  alt: \"", TestName = "a clip alt that does not say it is an illustration")]
    [TestCase("  file: tlv_raid_intro.mp4", "  file: ../tlv_raid_intro.mp4", TestName = "a clip that is not a bare mp4 name")]
    [TestCase("  ai_generated: true", "  credit: \"x\"\n  ai_generated: true", TestName = "a credit line on the AI-only clip")]
    [TestCase("  alt_he_review: agent-checked", "  alt_he_review: approved", TestName = "an alt_he_review other than agent-checked")]
    public void TheReaderFailsClosed(string from, string to)
    {
        StringAssert.Contains(from, Shipped, "precondition: the shipped file has the line the case changes");
        var broken = ReplaceFirst(Shipped, from, to);
        Assert.Throws<InvalidOperationException>(() => RaidCardContent.Parse(broken));
    }

    [Test]
    public void TheReaderNeedsASourceWithAUrlAndWhatItSupports()
    {
        var head = Shipped.Substring(0, Shipped.IndexOf("sources:", StringComparison.Ordinal));
        var clip = Shipped.Substring(Shipped.IndexOf("\nclip:", StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() => RaidCardContent.Parse(head + "sources: []" + clip), "no source");
        Assert.Throws<InvalidOperationException>(() => RaidCardContent.Parse(head
            + "sources:\n  - url: \"https://example.org\"\n    checked: 2026-11-08" + clip), "a source that supports nothing");
        Assert.DoesNotThrow(() => RaidCardContent.Parse(head
            + "sources:\n  - url: \"https://example.org\"\n    supports: \"x\"\n    checked: 2026-11-08" + clip), "positive control");
    }

    static string ReplaceFirst(string text, string from, string to)
    {
        int at = text.IndexOf(from, StringComparison.Ordinal);
        return text.Substring(0, at) + to + text.Substring(at + from.Length);
    }

    [TestCase("en")]
    [TestCase("he")]
    public void TheCardShowsTheTextTheClipsLabelsAndOnlyAnOkButton(string lang)
    {
        using var ui = new UiTestKit();
        var data = RaidCardContent.Parse(Shipped);
        using var card = new RaidCardPresenter(ui.Root, data, Text(lang), _ => { }, () => { });
        card.Show();
        Assert.IsTrue(card.IsOpen);
        var hud = ui.Q<VisualElement>("root");
        Assert.IsTrue(hud.ClassListContains(RaidCardPresenter.OpenClass));
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("raid-layer").style.display.value);
        Assert.IsFalse(ui.Q<VisualElement>("top-bar").enabledInHierarchy, "the HUD under the card takes no input");
        Assert.IsFalse(ui.Q<VisualElement>("event-layer").enabledInHierarchy, "nor does the turn 1 decision card");
        var buttons = ui.Q<VisualElement>("raid-card").Query<Button>().ToList();
        Assert.AreEqual(1, buttons.Count, "the player only has OK (owner, 8 Nov 2026)");
        Assert.AreEqual("raid-ok", buttons[0].name);
        Assert.AreEqual(Text(lang)["raid.ok"], buttons[0].text);
        Assert.AreEqual(Text(lang)["opening.preview"], ui.Q<Label>("raid-preview").text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("raid-preview").style.display.value, "the reviewed card has no draft label");
        bool he = lang == "he";
        Assert.AreEqual(he ? data.TitleHe : data.Title, ui.Q<Label>("raid-title").text);
        Assert.AreEqual(he ? data.TextHe : data.Text, ui.Q<Label>("raid-text").text);
        Assert.AreEqual(he ? LanguageDirection.RTL : LanguageDirection.LTR, ui.Q<Label>("raid-text").languageDirection);
        Assert.IsNull(ui.Root.Q<Label>("raid-credit"), "the clip is AI-only (CC0, ASSETS.yaml): no credit line");
        Assert.AreEqual(Text(lang)["event.ai_label"], ui.Q<Label>("raid-ai").text, "labelled as the cards label AI media");
        Assert.AreEqual(he ? data.Clip.AltHe : data.Clip.Alt, ui.Q<VisualElement>("raid-media").tooltip);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("raid-media").style.display.value,
            "with video off (CardVideo.ForcedOff) the card shows no empty frame");
    }

    [Test]
    public void AReviewedCardHidesThePreviewLabelAndADraftShowsIt()
    {
        using (var ui = new UiTestKit())
        using (var card = new RaidCardPresenter(ui.Root, ReviewedFixture(), Text("en"), _ => { }, () => { }))
        {
            card.Show();
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("raid-preview").style.display.value);
        }
        var draft = ReviewedFixture(); draft.Status = "draft";
        using (var ui = new UiTestKit())
        using (var card = new RaidCardPresenter(ui.Root, draft, Text("en"), _ => { }, () => { }))
        {
            card.Show();
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("raid-preview").style.display.value, "the Editor draft is labelled");
        }
    }

    [Test]
    public void TheClipPlaysInTheCardsFrameAndAFailureHidesTheFrame()
    {
        using var ui = new UiTestKit();
        var data = RaidCardContent.Parse(Shipped);
        using var video = new CardVideo();
        using var card = new RaidCardPresenter(ui.Root, data, Text("en"), _ => { }, () => { }, video);
        card.Show();
        var media = ui.Q<VisualElement>("raid-media");
        Assert.AreEqual(DisplayStyle.Flex, media.style.display.value);
        Assert.AreEqual(CardVideo.Url(data.Clip.File), video.Current, "the card's clip loops in its frame");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("raid-ai").style.display.value, "labelled while it plays");
        UiTestKit.Press(ui.Q<Button>("raid-ok"));
        Assert.IsNull(video.Current, "closing stops the clip");
        card.Show();
        Assert.AreEqual(CardVideo.Url(data.Clip.File), video.Current, "a later New game plays it again");
        card.ClipFailed();
        Assert.IsNull(video.Current);
        Assert.AreEqual(DisplayStyle.None, media.style.display.value, "a clip that fails leaves no black frame");
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("raid-ai").style.display.value, "nor its label");
        Assert.IsTrue(card.IsOpen, "the card keeps its text and OK");
    }

    [Test]
    public void OkClosesTheCardAndHandsOverOnce()
    {
        using var ui = new UiTestKit();
        int finished = 0; var shown = new System.Collections.Generic.List<bool>();
        using var card = new RaidCardPresenter(ui.Root, RaidCardContent.Parse(Shipped), Text("en"), shown.Add, () => finished++);
        card.Show();
        card.Show();
        Assert.AreEqual(new[] { true }, shown.ToArray(), "a second Show while open does nothing (New game reports twice)");
        UiTestKit.Press(ui.Q<Button>("raid-ok"));
        Assert.IsFalse(card.IsOpen);
        Assert.IsTrue(card.ClosedThisFrame, "Escape in the same frame goes no further");
        Assert.IsFalse(ui.Q<VisualElement>("root").ClassListContains(RaidCardPresenter.OpenClass));
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("raid-layer").style.display.value);
        Assert.IsTrue(ui.Q<VisualElement>("top-bar").enabledInHierarchy, "the HUD is back");
        Assert.AreEqual(1, finished);
        UiTestKit.Press(ui.Q<Button>("raid-ok"));
        Assert.AreEqual(1, finished, "a closed card hands over no second time");
        Assert.AreEqual(new[] { true, false }, shown.ToArray());
    }

    [Test]
    public void EscapeActsAsOkAndDisposeNeverHandsOver()
    {
        using var ui = new UiTestKit();
        int finished = 0;
        var card = new RaidCardPresenter(ui.Root, RaidCardContent.Parse(Shipped), Text("en"), _ => { }, () => finished++);
        Assert.IsFalse(card.Escape(), "closed: Escape is not the card's");
        card.Show();
        Assert.IsTrue(card.Escape());
        Assert.AreEqual(1, finished, "Escape is the same OK");
        card.Show();
        Assert.IsTrue(card.IsOpen, "a later New game shows it again");
        card.Dispose();
        Assert.AreEqual(1, finished, "disposing never starts the advisors");
        Assert.IsFalse(ui.Q<VisualElement>("root").ClassListContains(RaidCardPresenter.OpenClass));
        UiTestKit.Press(ui.Q<Button>("raid-ok"));
        Assert.AreEqual(1, finished, "disposed callbacks do not fire");
        card.Show();
        Assert.IsFalse(card.IsOpen, "a disposed card never shows");
    }
}
