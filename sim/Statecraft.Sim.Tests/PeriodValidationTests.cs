using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Load-time checks that keep "who governed / which body sat / who held office on this date" unambiguous:
/// no overlapping legislatures or governments, no term in an abolished office, no term after its holder's death.</summary>
public class PeriodValidationTests
{
    [Fact]
    public void OverlappingLegislaturesAreALoadError()
    {
        var ex = ValidationTests.Bad("legislatures.yaml", "to: 1949-02-14", "to: 1949-03-01");
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.Equal("legislatures[house].from", ex.Field);
        Assert.Contains("overlaps council (1948-05-14 to 1949-03-01)", ex.Message);
    }

    [Fact]
    public void AnOpenEndedLegislatureFollowedByAnotherIsAnOverlap()
    {
        var ex = ValidationTests.Bad("legislatures.yaml", "to: 1949-02-14", "");
        Assert.Equal("legislatures[house].from", ex.Field);
        Assert.Contains("overlaps council (1948-05-14 to open)", ex.Message);
    }

    [Fact]
    public void OverlappingGovernmentsAreALoadError()
    {
        var ex = ValidationTests.Bad("governments.yaml", "to: 1949-03-10", "to: 1949-04-01");
        Assert.Equal("governments.yaml", ex.File);
        Assert.Equal("governments[gov2].from", ex.Field);
        Assert.Contains("overlaps gov1", ex.Message);
    }

    [Fact]
    public void AGapBetweenGovernmentsIsAllowed()
    {
        var gov = Fixture.Files["governments.yaml"].Replace("to: 1949-03-10", "to: 1949-03-01");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["governments.yaml"] = gov }));
        Assert.Null(Offices.Government(c, new DateTime(1949, 3, 5)));
    }

    [Fact]
    public void ExactlyTouchingPeriodsAreNotAnOverlap()
    {
        // The fixture's legislatures meet at 1949-02-14 and its governments at 1949-03-10: prev.to == next.from.
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>()));
        Assert.Equal(new DateTime(1949, 2, 14), c.Legislatures.Find(l => l.Id == "council")!.To);
        Assert.Equal(new DateTime(1949, 2, 14), c.Legislatures.Find(l => l.Id == "house")!.From);
        Assert.Equal(new DateTime(1949, 3, 10), c.Governments.Find(g => g.Id == "gov1")!.To);
        Assert.Equal(new DateTime(1949, 3, 10), c.Governments.Find(g => g.Id == "gov2")!.From);
    }

    [Fact]
    public void ATermRunningPastTheOfficesAbolitionIsALoadError()
    {
        var ex = ValidationTests.Bad("offices.yaml", "title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.finance_minister.to", ex.Field);
        Assert.Contains("must be on or before 1949-01-01, when the office was abolished", ex.Message);
    }

    [Fact]
    public void ATermStartingBeforeTheOfficeIsEstablishedIsALoadError()
    {
        var ex = ValidationTests.Bad("offices.yaml", "title: Minister of Finance", "title: Minister of Finance\n    from: 1949-01-01");
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.finance_minister.from", ex.Field);
        Assert.Contains("must be on or after 1949-01-01, when the office was established", ex.Message);
    }

    [Fact]
    public void AnOfficeAbolishedBeforeItIsEstablishedIsALoadError()
    {
        var ex = ValidationTests.Bad("offices.yaml", "title: Minister of Finance", "title: Minister of Finance\n    from: 1949-01-01\n    until: 1949-01-01");
        Assert.Equal("offices.yaml", ex.File);
        Assert.Equal("offices[finance_minister].until", ex.Field);
    }

    [Fact]
    public void AnAbolishedOfficeWhoseTermsEndInTimeLoads()
    {
        var offices = Fixture.Files["offices.yaml"].Replace("title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        var people = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1948-05-14\n        to: 1949-01-01");
        var rules = Fixture.Files["rules.yaml"].Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { prime_minister: 1.0 }");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices, ["people.yaml"] = people, ["rules.yaml"] = rules }));
        Assert.Equal(new DateTime(1949, 1, 1), c.Offices.Find(o => o.Id == "finance_minister")!.Until);
        Assert.Null(Offices.Holder(c, "finance_minister", new DateTime(1949, 1, 1)));
    }

    [Fact]
    public void ABudgetLineForAnAbolishedOfficeIsALoadError()
    {
        var offices = Fixture.Files["offices.yaml"].Replace("title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        var people = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1948-05-14\n        to: 1949-01-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices, ["people.yaml"] = people })));
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal("budget_start.finance_minister", ex.Field);
        Assert.Contains("is abolished on 1949-01-01: give its budget line to the office that took over", ex.Message);
    }

    [Theory]
    [InlineData("1949-03-09", false)]
    [InlineData("1949-03-10", true)]
    [InlineData("1973-12-01", true)]
    public void OpenTermDeathIsCheckedAgainstLatestGovernmentStart(string death, bool loads)
    {
        var people = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", "    name: Pm One\n    died: " + death + "\n");
        var dir = Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = people });
        if (loads) Assert.Equal(DateTime.Parse(death), ContentLoader.Load(dir).People.Find(p => p.Id == "pm_one")!.Died);
        else
        {
            var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
            Assert.Equal("people.yaml", ex.File);
            Assert.Equal("people[pm_one].offices.prime_minister.to", ex.Field);
        }
    }

    [Fact]
    public void AHolderWhoDiesInTheLatestGovernmentLeavesAnOpenTermOnTheDateOfDeath()
    {
        // gov2 (the latest) starts 1949-03-10; Pm One's prime_minister term is open-ended.
        var people = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", "    name: Pm One\n    died: 1949-06-01\n");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = people }));
        Assert.Equal("pm_one", Offices.Holder(c, "prime_minister", new DateTime(1949, 5, 31))!.Id);
        Assert.Null(Offices.Holder(c, "prime_minister", new DateTime(1949, 6, 1)));
        Assert.Null(Offices.Holder(c, "prime_minister", new DateTime(1950, 1, 1)));
    }

    [Fact]
    public void AnOpenTermStartingOnTheHoldersDeathIsALoadError()
    {
        // The later-death rule must not let an open term begin after its holder died.
        var people = Fixture.Files["people.yaml"].Replace("    name: Def Two\n", "    name: Def Two\n    died: 1949-06-01\n")
            .Replace("        from: 1949-01-01\n", "        from: 1949-06-01\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = people })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.defense_minister.from", ex.Field);
        Assert.Contains("must be before 1949-06-01, the date of death", ex.Message);
    }

    [Fact]
    public void WithNoGovernmentsAnOpenTermAfterDeathIsStillALoadError()
    {
        var people = Fixture.Files["people.yaml"].Replace("    name: Pm One\n", "    name: Pm One\n    died: 1973-12-01\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = people, ["governments.yaml"] = "governments: []\n" })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].offices.prime_minister.to", ex.Field);
    }

    [Fact]
    public void ATermRunningPastTheHoldersDeathIsALoadError()
    {
        var people = Fixture.Files["people.yaml"].Replace("    name: Def Two\n", "    name: Def Two\n    died: 1949-06-01\n")
            .Replace("        from: 1949-01-01\n", "        from: 1949-01-01\n        to: 1949-06-02\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = people })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.defense_minister.to", ex.Field);
        Assert.Contains("must be on or before 1949-06-01, the date of death", ex.Message);
    }
}
