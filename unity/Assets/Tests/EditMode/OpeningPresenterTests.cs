#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#530: fixtures never confer a human review on the shipped opening data.</summary>
public class OpeningPresenterTests
{
    static OpeningContent Asset => AssetDatabase.LoadAssetAtPath<OpeningContent>("Assets/Resources/OpeningContent.asset");
    static UiText Text(string lang) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
        "Assets/UI/strings." + lang + ".yaml").text);

    static OpeningContent.Document ReviewedFixture()
    {
        var data = Asset.Read();
        data.Status = "sensitivity-reviewed";
        foreach (var source in data.Panels.SelectMany(p => p.Sources).Concat(data.Closing.Sources)) source.Checked = "2026-10-22";
        foreach (var panel in data.Panels) { panel.Image.Sensitive = true; panel.Image.SensitivityReviewed = true; }
        return data;   // memory only: a fixture is never a human sign-off on the shipped file
    }

    [Test]
    public void AReviewedInMemoryFixtureMayBePresentedToPlayers()
    {
        var data = ReviewedFixture();
        Assert.IsTrue(data.Reviewed);
        Assert.IsTrue(OpeningContent.MayPresent(data, editorPreview: false));
    }

    [Test]
    public void ApprovedPhotosDoNotMakeTheDraftTextPlayerReady()
    {
        var data = ReviewedFixture();
        data.Status = "draft";
        foreach (var source in data.Panels.SelectMany(p => p.Sources).Concat(data.Closing.Sources)) source.Checked = "";
        Assert.IsFalse(data.Reviewed, "approved-photo fixture does not approve draft text or its unchecked sources");
        Assert.IsFalse(OpeningContent.MayPresent(data, editorPreview: false), "ordinary players must not see an unchecked draft");
        Assert.IsTrue(OpeningContent.MayPresent(data, editorPreview: true), "only the labelled Editor preview is available");
    }

    [Test]
    public void EditorPreviewNeverBypassesPhotographReview()
    {
        var data = Asset.Read();
        data.Panels[0].Image.SensitivityReviewed = false;
        Assert.IsFalse(OpeningContent.MayPresent(data, editorPreview: true), "Editor preview only relaxes text review, never photo review");
        Assert.IsFalse(OpeningContent.MayPresent(data, editorPreview: false));
        using var ui = new UiTestKit();
        using var opening = new OpeningPresenter(ui.Root, Asset, data, Text("en"), _ => { }, _ => { });
        opening.Show(replay: false);
        Assert.IsFalse(opening.IsOpen, "the actual presenter also refuses an unreviewed photograph");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("opening-layer").style.display.value);
    }

    [Test]
    public void EveryParagraphAndClosingSourceMustHaveACheckedDate()
    {
        var data = ReviewedFixture();
        Assert.IsTrue(OpeningContent.MayPresent(data, editorPreview: false), "an in-memory gate fixture, not a real review");
        data.Closing.Sources[0].Checked = "";
        Assert.IsFalse(data.Reviewed, "closing needs its own source check too");
        data.Closing.Sources[0].Checked = "2026-02-30";
        Assert.IsFalse(data.Reviewed, "an impossible calendar day is not a checked date");
        data.Closing.Sources[0].Checked = "2026-10-22";
        data.Panels[1].Image.SensitivityReviewed = false;
        Assert.IsFalse(data.Reviewed, "text checks do not approve a photo");
    }

    [TestCase("en")]
    [TestCase("he")]
    public void ThreePanelsAndClosingKeepTheirExactTextAndCredits(string lang)
    {
        using var ui = new UiTestKit();
        var data = Asset.Read();
        using var opening = new OpeningPresenter(ui.Root, Asset, data, Text(lang), _ => { }, _ => { });
        opening.Show(replay: true);
        Assert.IsTrue(opening.IsOpen);
        Assert.IsTrue(ui.Q<VisualElement>("root").ClassListContains(OpeningPresenter.OpenClass));
        Assert.IsFalse(ui.Q<VisualElement>("top-bar").enabledInHierarchy, "HUD keys cannot reach covered controls");
        Assert.IsFalse(ui.Q<VisualElement>("start-screen").enabledInHierarchy, "the replay's start screen cannot receive input underneath");
        Assert.IsFalse(ui.Q<Button>("opening-back").enabledSelf);
        Assert.AreEqual(Text(lang)["opening.skip"], ui.Q<Button>("opening-skip").text);
        Assert.AreEqual(Text(lang)["opening.preview"], ui.Q<Label>("opening-preview").text);
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(i, opening.Index);
            Assert.AreEqual(data.Panels[i].Text, ui.Q<Label>("opening-text").text);
            Assert.AreEqual(data.Panels[i].Image.Credit, ui.Q<Label>("opening-credit").text);
            Assert.AreEqual(LanguageDirection.LTR, ui.Q<Label>("opening-text").languageDirection,
                "the approved-source English fallback is not labelled Hebrew");
            Assert.AreEqual(Text(lang).Format("opening.count", i + 1, 3), ui.Q<Label>("opening-count").text,
                "the approved opening has exactly three panels, not a fourth closing screen");
            Assert.AreEqual(i == 2 ? data.Closing.Text : "", ui.Q<Label>("opening-closing").text);
            if (i < 2) UiTestKit.Press(ui.Q<Button>("opening-next"));
        }
        Assert.IsTrue(opening.IsOpen, "the third panel retains its image, arrivals and closing line together");
        Assert.AreEqual(2, opening.Index);
        Assert.AreEqual(data.Closing.Text, ui.Q<Label>("opening-closing").text);
        UiTestKit.Press(ui.Q<Button>("opening-back"));
        Assert.AreEqual(1, opening.Index);
        Assert.AreEqual(data.Panels[1].Image.Credit, ui.Q<Label>("opening-credit").text);
        UiTestKit.Press(ui.Q<Button>("opening-next"));
        UiTestKit.Press(ui.Q<Button>("opening-next"));
        Assert.IsFalse(opening.IsOpen, "the third panel's final action closes the opening");
    }

    [Test]
    public void EscapeAndDisposeRemoveHandlersAndDoNotFinishTwice()
    {
        using var ui = new UiTestKit();
        var data = Asset.Read();
        int finished = 0;
        var opening = new OpeningPresenter(ui.Root, Asset, data, Text("en"), _ => { }, _ => finished++);
        opening.Show(replay: false);
        Assert.IsTrue(opening.Escape());
        Assert.IsFalse(opening.Escape());
        Assert.AreEqual(1, finished);
        opening.Show(replay: true);
        opening.Dispose();
        Assert.AreEqual(1, finished, "disposing must not start the advisors or finish a new game");
        Assert.IsFalse(ui.Q<VisualElement>("root").ClassListContains(OpeningPresenter.OpenClass));
        UiTestKit.Press(ui.Q<Button>("opening-next"));
        UiTestKit.Press(ui.Q<Button>("opening-skip"));
        Assert.AreEqual(1, finished, "disposed callbacks do not fire");
    }

    [Test]
    public void TheCatalogReferencesTheExistingSourceAndPhotographs()
    {
        Assert.IsNotNull(Asset);
        Assert.AreEqual("Assets/UI/Opening/opening.yaml", AssetDatabase.GetAssetPath(Asset.source));
        var data = Asset.Read();
        for (int i = 0; i < 3; i++) Assert.AreEqual("Assets/UI/Opening/" + data.Panels[i].Image.File,
            AssetDatabase.GetAssetPath(Asset.photos[i]));
        Assert.Throws<InvalidOperationException>(() => OpeningContent.Parse(Asset.source.text.Replace("id: yemen", "id: europe")));
    }
}
