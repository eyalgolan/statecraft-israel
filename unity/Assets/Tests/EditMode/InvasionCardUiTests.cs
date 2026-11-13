#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The Arab armies' invasion card on New game (#3142): its strict reader, its review gate and the card itself. The sim test
/// InvasionCardTests holds each step's row to the forces the game starts with. Fixtures live in memory only: none is a human
/// review of the shipped file.</summary>
public class InvasionCardUiTests
{
    static string Shipped => AssetDatabase.LoadAssetAtPath<TextAsset>(InvasionCardContent.SourcePath).text;
    static UiText Text(string lang) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
        "Assets/UI/strings." + lang + ".yaml").text);

    const string Valid = @"status: draft
title: ""T""
title_he: ""ט""
he_review: pending
steps:
  - text: ""Intro""
    text_he: ""פתיחה""
  - focus: arab_legion
    side: jordan
    text: ""Front""
    text_he: ""חזית""
sources:
  - url: ""https://example.org/a""
    supports: ""what it supports""
";

    [Test]
    public void TheShippedCardIsALabelledDraftThatAPlayerBuildDoesNotShow()
    {
        var data = InvasionCardContent.Load();
        Assert.IsNotNull(data, "Resources/" + InvasionCardContent.ResourcePath + " loads");
        Assert.AreEqual(6, data!.Steps.Count, "an intro and the five forces on the map on 15 May");
        Assert.IsNull(data.Steps[0].Focus, "the intro moves no camera");
        Assert.IsTrue(data.Steps.Skip(1).All(s => !string.IsNullOrWhiteSpace(s.Focus)));
        Assert.IsTrue(data.HasHebrew, "every new line has its Hebrew");
        Assert.IsFalse(data.HebrewApproved, "agent-drafted Hebrew waits for a native read (#564)");
        Assert.AreEqual("draft", data.Status, "the owner has not reviewed it");
        Assert.IsTrue(data.Sources.All(s => !s.IsChecked), "no person has checked its sources: no date may claim it");
        Assert.IsFalse(data.Reviewed);
        Assert.IsFalse(InvasionCardContent.MayPresent(data, editorPreview: false), "a player build does not show the draft");
        Assert.IsTrue(InvasionCardContent.MayPresent(data, editorPreview: true), "the Editor previews it, labelled");
    }

    [Test]
    public void AReviewedCopyIsWhatAPlayerBuildWouldShow()
    {
        var data = InvasionCardContent.Parse(Shipped);
        data.Status = Statecraft.Sim.ReleaseGate.ReviewedStatus;
        Assert.IsFalse(data.Reviewed, "sources still unchecked");
        foreach (var source in data.Sources) source.Checked = "2026-11-11";
        Assert.IsFalse(data.Reviewed, "Hebrew still pending");
        data.HeReview = OpeningContent.HeApproved;
        Assert.IsTrue(data.Reviewed);   // memory only: never a human sign-off on the shipped file
        Assert.IsTrue(InvasionCardContent.MayPresent(data, editorPreview: false));
    }

    [Test]
    public void TheValidFixtureParses()
    {
        var data = InvasionCardContent.Parse(Valid);
        Assert.AreEqual(2, data.Steps.Count); Assert.IsTrue(data.HasHebrew);
        Assert.IsFalse(InvasionCardContent.Parse(Valid.Replace("title_he: \"ט\"\n", "").Replace("he_review: pending\n", "")
            .Replace("    text_he: \"פתיחה\"\n", "").Replace("    text_he: \"חזית\"\n", "")).HasHebrew, "English alone parses too");
    }

    [TestCase("title: \"T\"\n", "title: \"\"\n", TestName = "NoTitle")]
    [TestCase("  - focus: arab_legion\n    side: jordan\n    text: \"Front\"\n    text_he: \"חזית\"\n", "", TestName = "OneStepOnly")]
    [TestCase("  - text: \"Intro\"\n", "  - focus: egyptian_expeditionary_force\n    text: \"Intro\"\n", TestName = "TheIntroFocuses")]
    [TestCase("    side: jordan\n", "", TestName = "AFrontWithoutItsSide")]
    [TestCase("  - focus: arab_legion\n    side: jordan\n", "  - side: jordan\n", TestName = "AFrontWithoutItsRow")]
    [TestCase("    supports: \"what it supports\"\n", "", TestName = "ASourceWithoutItsClaim")]
    [TestCase("  - url: \"https://example.org/a\"\n", "  - url: \"\"\n", TestName = "ASourceWithoutItsUrl")]
    [TestCase("    text_he: \"חזית\"\n", "", TestName = "PartHebrew")]
    [TestCase("he_review: pending\n", "he_review: yes\n", TestName = "AnUnknownReviewState")]
    [TestCase("status: draft\n", "status: draft\nlayout: wide\n", TestName = "AnUnknownField")]
    [TestCase("status: draft\n", "status: draft\nstatus: draft\n", TestName = "ARepeatedField")]
    public void ItFailsClosed(string from, string to)
    {
        StringAssert.Contains(from, Valid, "precondition: the fixture has the line to change");
        var ex = Assert.Throws<InvalidOperationException>(() => InvasionCardContent.Parse(Valid.Replace(from, to)));
        StringAssert.StartsWith(InvasionCardContent.SourcePath, ex!.Message);
    }

    [Test]
    public void NextFocusesEachFrontInTurnAndContinueClosesTheCardOnce()
    {
        using var ui = new UiTestKit();
        var data = InvasionCardContent.Parse(Shipped); var en = Text("en");
        var focused = new System.Collections.Generic.List<string?>(); var shown = new System.Collections.Generic.List<bool>(); int finished = 0;
        using var card = new InvasionCardPresenter(ui.Root, data, en, shown.Add, focused.Add, () => finished++);
        card.Show(); card.Show();
        Assert.AreEqual(new[] { true }, shown.ToArray(), "a second Show while open does nothing (New game reports twice)");
        Assert.IsTrue(ui.Q<VisualElement>("root").ClassListContains(InvasionCardPresenter.OpenClass));
        Assert.IsFalse(ui.Q<VisualElement>("top-bar").enabledInHierarchy, "the HUD waits under the card");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("invasion-preview").style.display.value, "the Editor draft is labelled");
        Assert.AreEqual(en["opening.preview"], ui.Q<Label>("invasion-preview").text);
        Assert.AreEqual(data.Title, ui.Q<Label>("invasion-title").text);
        var next = ui.Q<Button>("invasion-next");
        for (int i = 0; i < data.Steps.Count; i++)
        {
            Assert.AreEqual(i, card.Index);
            Assert.AreEqual(data.Steps[i].Text, ui.Q<Label>("invasion-text").text, "step " + i);
            Assert.AreEqual(en.Format("opening.count", i + 1, data.Steps.Count), ui.Q<Label>("invasion-count").text);
            Assert.AreEqual(en[i == data.Steps.Count - 1 ? "invasion.continue" : "opening.next"], next.text);
            Assert.AreEqual(0, finished, "open until its last step's Continue");
            UiTestKit.Press(next);
        }
        CollectionAssert.AreEqual(data.Steps.Select(s => s.Focus), focused, "the intro moves no camera; each front hands over its row once");
        Assert.IsFalse(card.IsOpen); Assert.IsTrue(card.ClosedThisFrame, "Escape in the same frame goes no further");
        Assert.AreEqual(1, finished);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("invasion-layer").style.display.value);
        Assert.IsTrue(ui.Q<VisualElement>("top-bar").enabledInHierarchy, "the HUD is back");
        UiTestKit.Press(next);
        Assert.AreEqual(1, finished, "a closed card hands over no second time");
        Assert.AreEqual(new[] { true, false }, shown.ToArray());
        card.Show();
        Assert.AreEqual(0, card.Index, "a later New game opens on the intro");
    }

    [Test]
    public void EscapeClosesTheCardAndDisposeNeverHandsOver()
    {
        using var ui = new UiTestKit();
        int finished = 0;
        var card = new InvasionCardPresenter(ui.Root, InvasionCardContent.Parse(Shipped), Text("en"), _ => { }, _ => { }, () => finished++);
        Assert.IsFalse(card.Escape(), "closed: Escape is not the card's");
        card.Show(); UiTestKit.Press(ui.Q<Button>("invasion-next"));
        Assert.IsTrue(card.Escape());
        Assert.AreEqual(1, finished, "Escape on any step closes the card and hands over");
        card.Show();
        card.Dispose();
        Assert.AreEqual(1, finished, "disposing never starts the advisors");
        Assert.IsFalse(ui.Q<VisualElement>("root").ClassListContains(InvasionCardPresenter.OpenClass));
        UiTestKit.Press(ui.Q<Button>("invasion-next"));
        Assert.AreEqual(1, finished, "disposed callbacks do not fire");
        card.Show();
        Assert.IsFalse(card.IsOpen, "a disposed card never shows");
    }

    [Test]
    public void TheHebrewUiShowsTheHebrewRightToLeft()
    {
        using var ui = new UiTestKit();
        var data = InvasionCardContent.Parse(Shipped); var he = Text("he");
        using var card = new InvasionCardPresenter(ui.Root, data, he, _ => { }, _ => { }, () => { });
        card.Show();
        Assert.AreEqual(data.TitleHe, ui.Q<Label>("invasion-title").text);
        for (int i = 0; i < data.Steps.Count; i++)
        {
            var body = ui.Q<Label>("invasion-text");
            Assert.AreEqual(data.Steps[i].TextHe, body.text); Assert.AreEqual(LanguageDirection.RTL, body.languageDirection);
            Assert.AreEqual(he[i == data.Steps.Count - 1 ? "invasion.continue" : "opening.next"], ui.Q<Button>("invasion-next").text);
            UiTestKit.Press(ui.Q<Button>("invasion-next"));
        }
        Assert.AreNotEqual("invasion.continue", he["invasion.continue"], "its Hebrew is there");
    }
}
