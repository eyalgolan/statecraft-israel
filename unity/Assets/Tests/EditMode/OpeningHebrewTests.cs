#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The opening in Hebrew (#530; owner, 1 Nov 2026, "Translate to Hebrew (Recommended)"): the Hebrew UI shows each
/// panel's title, text and arrivals and the closing line in Hebrew, right to left and set from the right like the rest of
/// the Hebrew UI; the photo credit stays the English credit line, left to right. opening.yaml's Hebrew comes complete
/// per panel or not at all, and a player sees it only after a native read (he_review: approved). Fixtures change the YAML
/// in memory only, never the review on disk.</summary>
public class OpeningHebrewTests
{
    const string SourceEnglish = "opening-source-en";
    static readonly Regex Latin = new Regex("[A-Za-z]", RegexOptions.CultureInvariant);
    static readonly Regex HebrewLetter = new Regex("[א-ת]", RegexOptions.CultureInvariant);
    static OpeningContent Asset => AssetDatabase.LoadAssetAtPath<OpeningContent>("Assets/Resources/OpeningContent.asset");
    static string Yaml => Asset.source.text;
    static UiText Text(string lang) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/UI/strings." + lang + ".yaml").text);

    /// <summary>The shipped YAML with every Hebrew field taken out: the file as it was before the Hebrew.</summary>
    static string WithoutHebrew(string yaml) =>
        Regex.Replace(Regex.Replace(yaml, @"(?m)^ *(title_he|text_he|he_review): .*\r?\n", ""), @", country_he: ""[^""]*""", "");

    static OpeningContent.Document Approved(string yaml)
    {
        var data = OpeningContent.Parse(yaml);
        data.Status = Statecraft.Sim.ReleaseGate.ReviewedStatus;
        foreach (var source in data.Panels.SelectMany(p => p.Sources).Concat(data.Closing.Sources)) source.Checked = "2026-10-22";
        foreach (var panel in data.Panels) { panel.Image.Sensitive = true; panel.Image.SensitivityReviewed = true; }
        foreach (var paragraph in data.Panels.Cast<OpeningContent.Paragraph>().Append(data.Closing)) paragraph.HeReview = OpeningContent.HeApproved;
        return data;   // memory only: a fixture is never a native read of the shipped file
    }

    [Test]
    public void EveryPanelAndTheClosingHaveTheirHebrewWithAReviewFlag()
    {
        var data = Asset.Read();
        foreach (var p in data.Panels)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(p.TitleHe), p.Id + " title_he");
            Assert.IsFalse(string.IsNullOrWhiteSpace(p.TextHe), p.Id + " text_he");
            Assert.That(p.HeReview, Is.EqualTo(OpeningContent.HePending).Or.EqualTo(OpeningContent.HeApproved), p.Id);
            foreach (var a in p.Arrivals) Assert.IsFalse(string.IsNullOrWhiteSpace(a.CountryHe), p.Id + " " + a.Country);
            foreach (var words in new[] { p.TitleHe!, p.TextHe! }.Concat(p.Arrivals.Select(a => a.CountryHe!)))
            {
                Assert.IsFalse(Latin.IsMatch(words), p.Id + ": static Hebrew has no Latin letters (HE-09): " + words);
                Assert.IsTrue(HebrewLetter.IsMatch(words), p.Id + ": " + words);
            }
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(data.Closing.TextHe), "closing text_he");
        Assert.That(data.Closing.HeReview, Is.EqualTo(OpeningContent.HePending).Or.EqualTo(OpeningContent.HeApproved));
        Assert.IsFalse(Latin.IsMatch(data.Closing.TextHe!), data.Closing.TextHe);
    }

    [Test]
    public void AFileWithoutHebrewStillReadsAndShowsEnglish()
    {
        var data = OpeningContent.Parse(WithoutHebrew(Yaml));
        Assert.IsTrue(data.Panels.All(p => p.TitleHe == null && p.TextHe == null && p.HeReview == null && p.Arrivals.All(a => a.CountryHe == null)));
        Assert.IsNull(data.Closing.TextHe);
        Assert.IsFalse(data.HebrewReviewed, "no Hebrew is not reviewed Hebrew");
    }

    [TestCase("yemen")]
    [TestCase("arab_countries")]
    [TestCase("europe")]
    [TestCase("closing")]
    public void PartialHebrewIsRefused(string where)
    {
        var yaml = Yaml.Replace("\r\n", "\n");
        var broken = where switch
        {
            "yemen" => yaml.Replace("    title_he: \"תימן\"\n", ""),                                          // no title
            "arab_countries" => yaml.Replace("country_he: \"מרוקו\", ", ""),                                   // one arrival short
            "europe" => Regex.Replace(yaml, "(?m)^    text_he: \"אחרי.*$", "    text_he: \" \""),            // a blank text
            _ => yaml.Replace("\n  he_review: pending\n", "\n"),                                             // no review flag
        };
        Assert.AreNotEqual(yaml, broken, "precondition: the fixture finds its line");
        var e = Assert.Throws<InvalidOperationException>(() => OpeningContent.Parse(broken));
        StringAssert.Contains(where, e.Message);
        StringAssert.Contains("Hebrew", e.Message);
    }

    [Test]
    public void ACountryNameInHebrewNeedsItsPanelsHebrew()
    {
        var yaml = Yaml.Replace("\r\n", "\n");
        int at = yaml.IndexOf("  - id: yemen", StringComparison.Ordinal), end = yaml.IndexOf("\nclosing:", StringComparison.Ordinal);
        Assert.That(at > 0 && end > at, "precondition: the Yemen panel");
        var yemen = Regex.Replace(yaml.Substring(at, end - at), @"(?m)^    (title_he|text_he|he_review): .*\n", "");
        StringAssert.Contains("country_he: \"תימן, כולל עדן לשעבר\"", yemen, "precondition: only the country name is left");
        var e = Assert.Throws<InvalidOperationException>(() => OpeningContent.Parse(yaml.Substring(0, at) + yemen + yaml.Substring(end)));
        StringAssert.Contains("yemen", e.Message);
    }

    [TestCase("reviewed")]
    [TestCase("agent-checked")]
    [TestCase("Approved")]
    [TestCase("''")]
    public void OnlyPendingOrApprovedIsAReviewFlag(string value)
    {
        var yaml = Yaml.Replace("\r\n", "\n").Replace("\n  he_review: pending\n", "\n  he_review: " + value + "\n");
        Assert.Throws<InvalidOperationException>(() => OpeningContent.Parse(yaml));
    }

    [Test]
    public void PendingHebrewKeepsTheOpeningFromPlayers()
    {
        var data = Approved(Yaml);
        Assert.IsTrue(data.HebrewReviewed && data.Reviewed, "precondition: the in-memory fixture is fully approved");
        Assert.IsTrue(OpeningContent.MayPresent(data, editorPreview: false));
        data.Panels[1].HeReview = OpeningContent.HePending;
        Assert.IsTrue(data.TextReviewed, "the English rule alone still passes");
        Assert.IsFalse(data.HebrewReviewed);
        Assert.IsFalse(data.Reviewed);
        Assert.IsFalse(OpeningContent.MayPresent(data, editorPreview: false), "a player never sees Hebrew no native speaker has read");
        Assert.IsTrue(OpeningContent.MayPresent(data, editorPreview: true), "the labelled Editor preview shows the draft");
        data = Approved(Yaml);
        data.Closing.HeReview = OpeningContent.HePending;
        Assert.IsFalse(data.HebrewReviewed, "the closing line is read too");
        data = Approved(WithoutHebrew(Yaml));
        Assert.IsFalse(data.HebrewReviewed, "a file with no Hebrew is not player-ready either");
    }

    static Label[] Prose(UiTestKit ui) => new[] { "opening-title", "opening-text", "opening-arrivals", "opening-closing" }
        .Select(n => ui.Q<Label>(n)).ToArray();

    [Test]
    public void TheHebrewUiShowsEachPanelAndTheClosingInHebrewRightToLeft()
    {
        using var ui = new UiTestKit();
        ui.Q<VisualElement>("root").AddToClassList(UiLanguage.HebrewClass);
        var data = Asset.Read();
        using var opening = new OpeningPresenter(ui.Root, Asset, data, Text("he"), _ => { }, _ => { });
        opening.Show(replay: false);
        for (int i = 0; i < 3; i++)
        {
            var p = data.Panels[i];
            Assert.AreEqual(p.TitleHe, ui.Q<Label>("opening-title").text);
            Assert.AreEqual(p.TextHe, ui.Q<Label>("opening-text").text);
            Assert.AreEqual(string.Join(" · ", p.Arrivals.Select(a => a.CountryHe + ": " + a.Count.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))),
                ui.Q<Label>("opening-arrivals").text);
            Assert.AreEqual(i == 2 ? data.Closing.TextHe : "", ui.Q<Label>("opening-closing").text);
            foreach (var label in Prose(ui).Where(l => l.text.Length > 0))
            {
                Assert.IsFalse(Latin.IsMatch(label.text), label.name + " has no Latin letters: " + label.text);
                Assert.IsTrue(HebrewLetter.IsMatch(label.text), label.name);
                Assert.AreEqual(LanguageDirection.RTL, label.languageDirection, label.name + " is right to left");
                Assert.IsFalse(label.ClassListContains(SourceEnglish), label.name + " is not styled as the English source");
            }
            var credit = ui.Q<Label>("opening-credit");
            Assert.AreEqual(p.Image.Credit, credit.text, "the credit line stays English");
            Assert.AreEqual(LanguageDirection.LTR, credit.languageDirection);
            Assert.IsTrue(credit.ClassListContains(SourceEnglish));
            if (i < 2) UiTestKit.Press(ui.Q<Button>("opening-next"));
        }
        UiTestKit.Press(ui.Q<Button>("opening-back"));
        Assert.AreEqual(data.Panels[1].TextHe, ui.Q<Label>("opening-text").text, "Back shows the Hebrew again");
        Assert.AreEqual("", ui.Q<Label>("opening-closing").text);
    }

    [Test]
    public void TheEnglishUiIsUnchanged()
    {
        using var ui = new UiTestKit();
        var data = Asset.Read();
        using var opening = new OpeningPresenter(ui.Root, Asset, data, Text("en"), _ => { }, _ => { });
        opening.Show(replay: false);
        for (int i = 0; i < 3; i++)
        {
            var p = data.Panels[i];
            Assert.AreEqual(p.Title, ui.Q<Label>("opening-title").text);
            Assert.AreEqual(p.Text, ui.Q<Label>("opening-text").text);
            Assert.AreEqual(string.Join(" · ", p.Arrivals.Select(a => a.Country + ": " + a.Count.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))),
                ui.Q<Label>("opening-arrivals").text);
            Assert.AreEqual(i == 2 ? data.Closing.Text : "", ui.Q<Label>("opening-closing").text);
            foreach (var label in Prose(ui).Append(ui.Q<Label>("opening-credit")))
            {
                Assert.IsFalse(HebrewLetter.IsMatch(label.text), label.name + ": " + label.text);
                Assert.AreEqual(LanguageDirection.LTR, label.languageDirection, label.name);
                Assert.IsTrue(label.ClassListContains(SourceEnglish), label.name);
            }
            if (i < 2) UiTestKit.Press(ui.Q<Button>("opening-next"));
        }
    }

    [Test]
    public void APanelWithoutHebrewStaysEnglishInTheHebrewUi()
    {
        var source = Object.Instantiate(Asset);
        var text = new TextAsset(WithoutHebrew(Yaml));
        try
        {
            source.source = text; source.photos = Asset.photos.ToArray();
            var data = source.Read();
            using var ui = new UiTestKit();
            ui.Q<VisualElement>("root").AddToClassList(UiLanguage.HebrewClass);
            using var opening = new OpeningPresenter(ui.Root, source, data, Text("he"), _ => { }, _ => { });
            opening.Show(replay: false);
            Assert.AreEqual(data.Panels[0].Text, ui.Q<Label>("opening-text").text);
            Assert.AreEqual(LanguageDirection.LTR, ui.Q<Label>("opening-text").languageDirection);
            Assert.IsTrue(ui.Q<Label>("opening-text").ClassListContains(SourceEnglish), "English prose keeps its left-aligned style");
        }
        finally { Object.DestroyImmediate(source); Object.DestroyImmediate(text); }
    }

    /// <summary>The styles as drawn: the Hebrew prose is set from the right (.lang-he), the English credit from the left
    /// (.opening-source-en); in the English UI all of it from the left, as before.</summary>
    [UnityTest]
    public IEnumerator TheHebrewProseIsSetFromTheRightAndTheCreditFromTheLeft()
    {
        foreach (var lang in new[] { "he", "en" })
        {
            using var ui = new UiTestKit();
            if (lang == "he") ui.Q<VisualElement>("root").AddToClassList(UiLanguage.HebrewClass);
            using var opening = new OpeningPresenter(ui.Root, Asset, Asset.Read(), Text(lang), _ => { }, _ => { });
            opening.Show(replay: false);
            UiTestKit.Press(ui.Q<Button>("opening-next")); UiTestKit.Press(ui.Q<Button>("opening-next"));   // the closing line too
            var body = ui.Q<Label>("opening-text");
            var expected = lang == "he" ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            for (int i = 0; i < 10 && body.resolvedStyle.unityTextAlign != expected; i++) yield return null;
            foreach (var label in Prose(ui))
                Assert.AreEqual(expected, label.resolvedStyle.unityTextAlign, lang + " " + label.name);
            Assert.AreEqual(TextAnchor.UpperLeft, ui.Q<Label>("opening-credit").resolvedStyle.unityTextAlign, lang + " credit");
        }
    }
}
