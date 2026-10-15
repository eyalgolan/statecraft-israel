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

    // ---- the real content ------------------------------------------------------------------------------------------

    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Every person in the Knesset's government position records for governments 0-7 was checked against
    /// KNS_Person.GenderDesc (2026-10-15): Golda Meir is the only woman, and the only women's titles are hers.</summary>
    [Fact]
    public void GoldaMeirIsTheOnlyFeminineMarkedPersonAndHoldsEveryFeminineTitle()
    {
        var women = C.People.Where(p => p.TitleHeForm == TitleHeForms.Feminine).ToList();
        Assert.Equal(new[] { "golda_meir" }, women.Select(p => p.Id));
        // A title_he_f no marked person holds would be shown to no one: the two lists stay equal.
        Assert.Equal(C.Offices.Where(o => o.TitleHeF != "").Select(o => o.Id).OrderBy(x => x, StringComparer.Ordinal),
            women.SelectMany(p => p.Offices.Select(t => t.Office)).Distinct().OrderBy(x => x, StringComparer.Ordinal));
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
