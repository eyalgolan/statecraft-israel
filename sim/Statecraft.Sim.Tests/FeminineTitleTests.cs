using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>people.yaml's title_he_form (#558): a person marked feminine is shown with the offices' feminine Hebrew title
/// (title_he_f, שרת העבודה), everyone else with title_he.</summary>
public class FeminineTitleTests
{
    const string Src = "[{ url: 'https://example.org/he', checked: 2026-10-15 }]";

    /// <summary>The fixture with Hebrew titles on the Prime Minister (both forms) and Defense (masculine only), and the
    /// given lines after Pm One's name.</summary>
    static Dictionary<string, string> Files(string pmOneLines, bool pmFeminine = true) => new Dictionary<string, string>
    {
        ["offices.yaml"] = Fixture.Files["offices.yaml"]
            .Replace("    title: Prime Minister\n", "    title: Prime Minister\n    title_he: ראש הממשלה\n    title_he_sources: " + Src + "\n"
                + (pmFeminine ? "    title_he_f: ראשת הממשלה\n    title_he_f_sources: " + Src + "\n" : ""))
            .Replace("    title: Minister of Defense\n", "    title: Minister of Defense\n    title_he: שר הביטחון\n    title_he_sources: " + Src + "\n    title_he_f: שרת הביטחון\n    title_he_f_sources: " + Src + "\n"),
        ["people.yaml"] = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", "    name: Pm One\n" + pmOneLines),
    };

    const string Feminine = "    title_he_form: feminine\n    biography_sources:\n      title_he_form: " + Src + "\n";

    static Content Load(Dictionary<string, string> files) => ContentLoader.Load(Fixture.Write(files));

    [Fact]
    public void AFeminineMarkedPersonGetsTheFeminineTitleAndOthersTheMasculine()
    {
        var c = Load(Files(Feminine));
        var pm = c.Offices.Single(o => o.Id == "prime_minister");
        var defense = c.Offices.Single(o => o.Id == "defense_minister");
        var finance = c.Offices.Single(o => o.Id == "finance_minister");
        var (pmOne, defTwo) = (c.People.Single(p => p.Id == "pm_one"), c.People.Single(p => p.Id == "def_two"));
        Assert.Equal(TitleHeForms.Feminine, pmOne.TitleHeForm);
        Assert.Equal("", defTwo.TitleHeForm);
        Assert.Equal("ראשת הממשלה", pm.TitleHeFor(pmOne));
        Assert.Equal("שרת הביטחון", defense.TitleHeFor(pmOne));
        Assert.Equal("שר הביטחון", defense.TitleHeFor(defTwo));
        Assert.Equal("ראש הממשלה", pm.TitleHeFor(null));   // a vacant seat
        Assert.Equal("", finance.TitleHeFor(defTwo));       // no Hebrew title: the UI keeps the English
    }

    [Theory]
    // Not an allowed value, or blank: an error, never a silent masculine.
    [InlineData("    title_he_form: female\n    biography_sources:\n      title_he_form: " + Src + "\n", true, "people.yaml", "people[pm_one].title_he_form")]
    [InlineData("    title_he_form: masculine\n    biography_sources:\n      title_he_form: " + Src + "\n", true, "people.yaml", "people[pm_one].title_he_form")]
    [InlineData("    title_he_form:\n    biography_sources:\n      title_he_form: " + Src + "\n", true, "people.yaml", "people[pm_one].title_he_form")]
    [InlineData("    title_he_form: ''\n    biography_sources:\n      title_he_form: " + Src + "\n", true, "people.yaml", "people[pm_one].title_he_form")]
    // Sourced like every biography value, and a source needs the value.
    [InlineData("    title_he_form: feminine\n", true, "people.yaml", "people[pm_one].biography_sources.title_he_form")]
    [InlineData("    biography_sources:\n      title_he_form: " + Src + "\n", true, "people.yaml", "people[pm_one].title_he_form")]
    [InlineData("    title_he_form: feminine\n    biography_sources:\n      title_he_form: [{ url: 'https://example.org/he' }]\n", true, "people.yaml", "people[pm_one].biography_sources.title_he_form[0]")]
    // She holds the Prime Minister's office, which has no feminine title.
    [InlineData(Feminine, false, "offices.yaml", "offices[prime_minister].title_he_f")]
    public void ABadTitleHeFormNamesFileAndField(string lines, bool pmFeminine, string file, string field)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Files(lines, pmFeminine)));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    /// <summary>Every office with a title_he has its title_he_f, whoever holds it: the player can order a woman into any
    /// office (Content.cs, Office.TitleHeF), so a missing row is a load error, not a silent masculine title.</summary>
    [Fact]
    public void AHebrewTitledOfficeWithNoFeminineTitleIsALoadError()
    {
        var ex = Assert.Throws<ContentException>(() => Load(Files("", pmFeminine: false)));
        Assert.Equal("offices.yaml", ex.File);
        Assert.Equal("offices[prime_minister].title_he_f", ex.Field);
        Assert.Contains("prime_minister", ex.Message);
    }

    // ---- the real content ------------------------------------------------------------------------------------------

    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Every person in the Knesset's government position records for governments 0-7 was checked against
    /// KNS_Person.GenderDesc (2026-10-15): Golda Meir is the only woman, and the only women's titles are hers.</summary>
    [Fact]
    public void GoldaMeirIsTheOnlyFeminineMarkedPerson()
    {
        Assert.Equal(new[] { "golda_meir" }, C.People.Where(p => p.TitleHeForm == TitleHeForms.Feminine).Select(p => p.Id));
    }

    /// <summary>The player can order a woman into any office, so every office with a Hebrew title has its feminine form
    /// in the content, sourced and flagged for the native reviewer, and the UI derives none (owner on #861, 17 October
    /// 2026: "Move to content"). The forms Beri chose as a native speaker the same day are pinned here.</summary>
    [Fact]
    public void EveryHebrewTitleHasItsFeminineFormInTheContent()
    {
        // Labour's and Foreign Affairs' were read by a native speaker in #930; every other one still waits for #564.
        var read = new[] { "foreign_minister", "labour_minister" };
        Assert.All(C.Offices.Where(o => o.TitleHe != ""), o =>
        {
            Assert.NotEqual("", o.TitleHeF);
            Assert.Equal(read.Contains(o.Id) ? "" : "agent-checked", o.TitleHeFReview);
        });
        string F(string id) => C.Offices.Single(o => o.Id == id).TitleHeF;
        Assert.Equal("ראש הממשלה", F("prime_minister"));
        Assert.Equal("שרת האוצר", F("finance_minister"));
        Assert.Equal("השרה לנפגעי מלחמה", F("war_victims_minister"));
        Assert.Equal("שרה בלי תיק", F("minister_without_portfolio"));
    }

    [Fact]
    public void GoldaMeirIsShownAsShrat()
    {
        var golda = C.People.Single(p => p.Id == "golda_meir");
        Assert.Equal("שרת העבודה", C.Offices.Single(o => o.Id == "labour_minister").TitleHeFor(golda));
        Assert.Equal("שרת החוץ", C.Offices.Single(o => o.Id == "foreign_minister").TitleHeFor(golda));
        Assert.Equal("שר החוץ", C.Offices.Single(o => o.Id == "foreign_minister").TitleHeFor(C.People.Single(p => p.Id == "sharett")));
        Assert.Equal("שר העבודה", C.Offices.Single(o => o.Id == "labour_minister").TitleHeFor(C.People.Single(p => p.Id == "namir")));
    }
}
