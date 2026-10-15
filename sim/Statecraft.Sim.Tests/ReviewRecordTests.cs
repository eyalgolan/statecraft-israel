using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;

/// <summary>A person's sign-off beside a status (owner, 15 October 2026, #375: "Yes, humans only"): `reviewed_by` (a GitHub
/// handle) and `reviewed_on` (the day), both or neither. Only a person writes them; scripts/oss/reviewed_by_guard.py
/// refuses an AI-co-authored commit that touches them.</summary>
public class ReviewRecordTests
{
    const string Signed = "reviewed_by: eyalgolan\nreviewed_on: 2026-10-15\n";

    static string Indent(string lines, int n) => string.Concat(lines.Split('\n').Where(l => l != "").Select(l => new string(' ', n) + l + "\n"));

    static Dictionary<string, string> With(string file, string after, string lines) =>
        new Dictionary<string, string> { [file] = Fixture.Files[file].Replace(after, after + lines) };

    static Content Load(Dictionary<string, string> files, Dictionary<string, string>? events = null) =>
        ContentLoader.Load(Fixture.Write(files, events));

    [Fact]
    public void ASignOffLoadsOnEveryKindOfStatusRow()
    {
        var files = With("people.yaml", "    name: Pm One\n", Indent(Signed, 4));
        files["legislatures.yaml"] = Fixture.Files["legislatures.yaml"].Replace("    body: Council\n", "    body: Council\n" + Indent(Signed, 4));
        files["governments.yaml"] = Fixture.Files["governments.yaml"].Replace("    name: Government One\n", "    name: Government One\n" + Indent(Signed, 4));
        files["aliyah.yaml"] = Fixture.Files["aliyah.yaml"].Replace("    to: 1948-12-31\n", "    to: 1948-12-31\n" + Indent(Signed, 4));
        var c = Load(files, new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event + Signed });
        var day = new DateTime(2026, 10, 15);
        Assert.Equal(("eyalgolan", day), (c.People.Single(p => p.Id == "pm_one").ReviewedBy, c.People.Single(p => p.Id == "pm_one").ReviewedOn));
        Assert.Equal("eyalgolan", c.Legislatures.Single(l => l.Id == "council").ReviewedBy);
        Assert.Equal(day, c.Governments.Single(g => g.Id == "gov1").ReviewedOn);
        Assert.Equal("eyalgolan", c.Aliyah[0].ReviewedBy);
        Assert.Equal(day, c.Events.Single().ReviewedOn);
        Assert.Equal(("", (DateTime?)null), (c.People.Single(p => p.Id != "pm_one").ReviewedBy, c.People.Single(p => p.Id != "pm_one").ReviewedOn));
    }

    [Theory]
    [InlineData("    reviewed_by: eyalgolan\n", "people[pm_one].reviewed_on")]                                  // both or neither
    [InlineData("    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: ''\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]          // blank is not a person
    [InlineData("    reviewed_by: ~\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: eyal@example.org\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]   // never an email
    [InlineData("    reviewed_by: '@eyalgolan'\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: eyal.golan\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: -eyal\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: eyal--golan\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: Eyal Golan\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: dependabot[bot]\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]
    [InlineData("    reviewed_by: a123456789012345678901234567890123456789\n    reviewed_on: 2026-10-15\n", "people[pm_one].reviewed_by")]   // 40 > 39
    [InlineData("    reviewed_by: eyalgolan\n    reviewed_on: ~\n", "people[pm_one].reviewed_on")]
    [InlineData("    reviewed_on: ~\n", "people[pm_one].reviewed_by")]                                            // a blank key is still a key
    public void ABadSignOffNamesTheFileAndField(string lines, string field)
    {
        var ex = Assert.Throws<ContentException>(() => Load(With("people.yaml", "    name: Pm One\n", lines)));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Theory]
    [InlineData("    reviewed_by: eyalgolan\n    reviewed_on: yesterday\n")]
    [InlineData("    reviewed_by: eyalgolan\n    reviewed_on: 2026-13-01\n")]
    [InlineData("    reviewed_by: [eyalgolan]\n    reviewed_on: 2026-10-15\n")]
    public void ASignOffThatIsNotADateOrAHandleDoesNotLoad(string lines)
    {
        var ex = Assert.Throws<ContentException>(() => Load(With("people.yaml", "    name: Pm One\n", lines)));
        Assert.Equal("people.yaml", ex.File);
    }

    [Fact]
    public void ABadSignOffOnAnEventOrAGovernmentNamesItsField()
    {
        var ev = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string>(), new Dictionary<string, string>
            { ["drought.yaml"] = ValidationTests.Event + "reviewed_by: someone@example.org\nreviewed_on: 2026-10-15\n" }));
        Assert.Equal("events/drought.yaml", ev.File);
        Assert.EndsWith("reviewed_by", ev.Field);
        var gov = Assert.Throws<ContentException>(() => Load(With("governments.yaml", "    name: Government One\n", "    reviewed_on: 2026-10-15\n")));
        Assert.Equal(("governments.yaml", "governments[gov1].reviewed_by"), (gov.File, gov.Field));
    }

    [Theory]
    [InlineData("eyalgolan", true)]
    [InlineData("a", true)]
    [InlineData("guy-shoham", true)]
    [InlineData("agamrafaeli-instinct", true)]
    [InlineData("Jeremy-Hoover1", true)]
    [InlineData("", false)]
    [InlineData("eyal@example.org", false)]
    [InlineData("eyal-", false)]
    [InlineData("eyal_golan", false)]
    [InlineData("dependabot[bot]", false)]
    public void TheHandleShapeIsGitHubs(string handle, bool ok) => Assert.Equal(ok, ReviewRecord.IsHandle(handle));

    /// <summary>The real content: every sign-off is a handle and a day that has already come (the loader keeps no clock; the
    /// test does).</summary>
    [Fact]
    public void EveryRealSignOffIsAHandleAndAPastDay()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var signed = c.People.Select(p => (p.ReviewedBy, p.ReviewedOn)).Concat(c.Legislatures.Select(l => (l.ReviewedBy, l.ReviewedOn)))
            .Concat(c.Governments.Select(g => (g.ReviewedBy, g.ReviewedOn))).Concat(c.Aliyah.Select(a => (a.ReviewedBy, a.ReviewedOn)))
            .Concat(c.PopulationAnchors.Select(a => (a.ReviewedBy, a.ReviewedOn))).Concat(c.Events.Select(e => (e.ReviewedBy, e.ReviewedOn)))
            .Concat(c.Sites.Select(s => (s.ReviewedBy, s.ReviewedOn))).Where(x => x.ReviewedBy != "").ToList();
        Assert.All(signed, x => { Assert.True(ReviewRecord.IsHandle(x.ReviewedBy)); Assert.True(x.ReviewedOn <= DateTime.UtcNow.Date); });
    }
}
