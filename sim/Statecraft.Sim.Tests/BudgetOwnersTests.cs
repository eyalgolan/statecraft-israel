using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Budget lines owned by an office and a date (#108): rules.yaml budget_owners, read through
/// Offices.BudgetOwner and Offices.BudgetLineOf, and the factor and competence the owner of the day gives.</summary>
public class BudgetOwnersTests
{
    // A supply ministry ends on 1 January 1950; its line then passes to the Prime Minister (pm_one, competence 8).
    // sup_one (competence 2) holds supply until then. def_two (competence 4) holds defense and finance.
    const string SupplyOffice = "  - id: supply_minister\n    title: Minister of Supply\n    until: 1950-01-01\n";
    const string SupplyPerson = @"
  - id: sup_one
    name: Sup One
    living: false
    competence: 2
    loyalty: 5
    offices:
      - office: supply_minister
        from: 1948-05-14
        to: 1950-01-01
";
    const string Shares = "budget_start: { finance_minister: 0.4, defense_minister: 0.4, supply: 0.2 }\n";
    const string Owners = "budget_owners:\n  supply:\n    - { from: 1948-05-15, office: supply_minister }\n    - { from: 1950-01-01, office: prime_minister }\n";

    static Dictionary<string, string> Files(string owners = Owners, string shares = Shares, string? offices = null) => new Dictionary<string, string>
    {
        ["offices.yaml"] = offices ?? Fixture.Files["offices.yaml"] + SupplyOffice,
        ["people.yaml"] = Fixture.Files["people.yaml"] + SupplyPerson,
        ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("budget_start: { finance_minister: 1.0 }\n", shares + owners),
    };

    static Content Load(string owners = Owners, string shares = Shares, string? offices = null) =>
        ContentLoader.Load(Fixture.Write(Files(owners, shares, offices)));

    static GameState At(Content c, DateTime date)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Date = date;
        return s;
    }

    static readonly DateTime Last = new DateTime(1949, 12, 31);
    static readonly DateTime Change = new DateTime(1950, 1, 1);

    [Fact]
    public void TheOwnerChangesOnItsDate()
    {
        var c = Load();
        Assert.Equal("supply_minister", Offices.BudgetOwner(c, "supply", new DateTime(1948, 5, 15)));
        Assert.Equal("supply_minister", Offices.BudgetOwner(c, "supply", Last));
        Assert.Equal("prime_minister", Offices.BudgetOwner(c, "supply", Change));
        Assert.Equal("prime_minister", Offices.BudgetOwner(c, "supply", new DateTime(1956, 12, 31)));
    }

    [Fact]
    public void ALineWithNoEntryBelongsToTheOfficeOfItsOwnIdAndAnUnknownLineToNobody()
    {
        var c = Load();
        Assert.Equal("finance_minister", Offices.BudgetOwner(c, "finance_minister", Last));
        Assert.Equal("finance_minister", Offices.BudgetOwner(c, "finance_minister", Change));
        Assert.Null(Offices.BudgetOwner(c, "nonsense", Last));
        Assert.Null(Offices.BudgetOwner(c, "prime_minister", Last));   // an office is not a line
    }

    [Fact]
    public void AnOfficeFindsTheLineItOwnsOnADate()
    {
        var c = Load();
        Assert.Equal("supply", Offices.BudgetLineOf(c, "supply_minister", Last));
        Assert.Null(Offices.BudgetLineOf(c, "prime_minister", Last));
        Assert.Null(Offices.BudgetLineOf(c, "supply_minister", Change));
        Assert.Equal("supply", Offices.BudgetLineOf(c, "prime_minister", Change));
        Assert.Equal("finance_minister", Offices.BudgetLineOf(c, "finance_minister", Change));
    }

    [Fact]
    public void TheFactorAndTheCompetenceAreTheOwnersOfTheDay()
    {
        var c = Load();
        var line = 0.5 + 0.2 * 3 / 2.0;                                   // 0.8: the supply line's factor
        var before = At(c, Last);
        Assert.Equal((0.5 + 2 / 20.0) * line, EffectsSystem.Multiplier(c, before, "supply_minister"), 9);   // sup_one
        Assert.Equal(line, EffectsSystem.BudgetFactor(c, before, "supply_minister"), 9);
        Assert.Equal(0.5 + 8 / 20.0, EffectsSystem.Multiplier(c, before, "prime_minister"), 9);              // no line yet: neutral
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(c, before, "prime_minister"), 9);
        var after = At(c, Change);
        Assert.Equal(0.5, EffectsSystem.Multiplier(c, after, "supply_minister"), 9);                         // vacant, no line
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(c, after, "supply_minister"), 9);
        Assert.Equal((0.5 + 8 / 20.0) * line, EffectsSystem.Multiplier(c, after, "prime_minister"), 9);       // pm_one now scales it
        Assert.Equal(line, EffectsSystem.BudgetFactor(c, after, "prime_minister"), 9);
    }

    [Fact]
    public void TheStateKeepsTheLineIdAcrossTheDate()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(0.2, s.Budget["supply"]);
        Assert.DoesNotContain("supply_minister", s.Budget.Keys);
        // The player sets the line by its own id on either side of the date.
        Assert.Null(EconomySystem.Allocate(c, s, new Dictionary<string, double> { ["finance_minister"] = 0.3, ["defense_minister"] = 0.3, ["supply"] = 0.4 }));
        s.Date = Change;
        Assert.Null(EconomySystem.Allocate(c, s, new Dictionary<string, double> { ["finance_minister"] = 0.2, ["defense_minister"] = 0.2, ["supply"] = 0.6 }));
        Assert.NotNull(EconomySystem.Allocate(c, s, new Dictionary<string, double> { ["finance_minister"] = 0.2, ["defense_minister"] = 0.2, ["supply_minister"] = 0.6 }));
    }

    [Fact]
    public void ALineNeverPointsAtAnInactiveOfficeInTheRealContent()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var era = c.Eras.First();
        int checkedLines = 0;
        foreach (var line in c.Rules.BudgetOwners.Keys)
            for (var d = era.From; d < era.To; d = d.AddDays(7))
            {
                var owner = Offices.BudgetOwner(c, line, d)!;
                Assert.True(c.Offices.Single(o => o.Id == owner).ExistsOn(d), $"{line} on {d:yyyy-MM-dd} points at inactive {owner}");
                checkedLines++;
            }
        Assert.True(checkedLines > 0);
        // Every line whose id is an office id is owned by that office, which is not abolished.
        foreach (var line in c.Rules.BudgetStart.Keys.Where(k => !c.Rules.BudgetOwners.ContainsKey(k)))
            Assert.Null(c.Offices.Single(o => o.Id == line).Until);
    }

    [Fact]
    public void TheRealImmigrationLineMovesToThePrimeMinisterOn8October1951()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal("immigration_minister", Offices.BudgetOwner(c, "immigration", c.Rules.Start));
        Assert.Equal("immigration_minister", Offices.BudgetOwner(c, "immigration", new DateTime(1951, 10, 7)));
        Assert.Equal("prime_minister", Offices.BudgetOwner(c, "immigration", new DateTime(1951, 10, 8)));
        Assert.Equal(c.Offices.Single(o => o.Id == "immigration_minister").Until, c.Rules.BudgetOwners["immigration"][1].From);   // the date cannot drift from offices.yaml
        var share = c.Rules.BudgetStart["immigration"];
        var factor = Math.Min(1.5, 0.5 + share * c.Rules.BudgetStart.Count / 2.0);

        var before = At(c, new DateTime(1951, 10, 7));
        var shapira = Cabinet.Holder(c, before, "immigration_minister")!;
        Assert.Equal("shapira", shapira.Id);
        Assert.Equal((0.5 + shapira.Competence / 20.0) * factor, EffectsSystem.Multiplier(c, before, "immigration_minister"), 9);
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(c, before, "prime_minister"), 9);

        var after = At(c, new DateTime(1951, 10, 8));
        var pm = Cabinet.Holder(c, after, "prime_minister")!;
        Assert.Equal((0.5 + pm.Competence / 20.0) * factor, EffectsSystem.Multiplier(c, after, "prime_minister"), 9);
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(c, after, "immigration_minister"), 9);
    }

    // ---- the loader ----

    [Theory]
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: supply_minister }\n    - { from: 1950-01-01, office: supply_minister }\n", "budget_owners.supply[1].office", "does not exist on 1950-01-01")]   // inactive on its dates
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: supply_minister }\n    - { from: 1949-06-01, office: prime_minister }\n    - { from: 1950-01-01, office: supply_minister }\n", "budget_owners.supply[2].office", "does not exist on 1950-01-01")]
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: supply_minister }\n", "budget_owners.supply[0].office", "is abolished on 1950-01-01")]   // the last owner must last to the end of the era
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: supply_ministre }\n", "budget_owners.supply[0].office", "unknown office")]   // a typo
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: prime_minister }\n    - { from: 1957-01-01, office: prime_minister }\n", "budget_owners.supply[1].from", "must be inside the era")]   // on the era's end
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: prime_minister }\n    - { from: 1960-01-01, office: prime_minister }\n", "budget_owners.supply[1].from", "must be inside the era")]
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-14, office: supply_minister }\n    - { from: 1950-01-01, office: prime_minister }\n", "budget_owners.supply[0].from", "must be inside the era")]   // before the era
    [InlineData("budget_owners:\n  supply:\n    - { from: 1949-01-01, office: supply_minister }\n    - { from: 1950-01-01, office: prime_minister }\n", "budget_owners.supply[0].from", "no gap")]
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: supply_minister }\n    - { from: 1950-01-01, office: prime_minister }\n    - { from: 1949-06-01, office: prime_minister }\n", "budget_owners.supply[2].from", "must be after the previous owner")]
    [InlineData("budget_owners:\n  supply: []\n", "budget_owners.supply", "needs at least one owner")]
    [InlineData("budget_owners:\n  suply:\n    - { from: 1948-05-15, office: prime_minister }\n", "budget_owners.suply", "unknown budget line")]
    [InlineData("budget_owners:\n  supply:\n    - { office: prime_minister }\n", "budget_owners.supply[0].from", "required")]
    [InlineData("budget_owners:\n  supply:\n    - { from: 1948-05-15, office: prime_minister }\n  finance_minister:\n    - { from: 1948-05-15, office: prime_minister }\n", "budget_start.supply", "would own both")]   // two lines, one office, one day
    public void TheLoaderRefusesABadOwner(string owners, string field, string message)
    {
        var e = Assert.Throws<ContentException>(() => Load(owners));
        Assert.Equal("rules.yaml", e.File);
        Assert.Equal(field, e.Field);
        Assert.Contains(message, e.Message);
    }

    [Fact]
    public void ATypoInAnOwnerKeyFailsAtTheYamlLevel()
    {
        Assert.Throws<ContentException>(() => Load("budget_owners:\n  supply:\n    - { from: 1948-05-15, ofice: prime_minister }\n"));
        Assert.Throws<ContentException>(() => Load("budget_owners:\n  supply: prime_minister\n"));   // a bare id is not a dated list
        Assert.Throws<ContentException>(() => Load("budget_owners:\n  supply:\n    - { from: not-a-date, office: prime_minister }\n"));
    }

    [Fact]
    public void ALineWithNoOwnerEntryNeedsAnOfficeOfItsOwnId()
    {
        var e = Assert.Throws<ContentException>(() => Load(owners: "", shares: Shares));
        Assert.Equal("budget_start.supply", e.Field);
        Assert.Contains("unknown office", e.Message);
    }

    [Fact]
    public void AnOfficeEndingBeforeTheEraEndStillNeedsADatedOwnerForItsOwnLine()
    {
        // The older rule still holds: a line keyed by an abolished office, with no dated owners, is refused.
        var e = Assert.Throws<ContentException>(() => Load(owners: "", shares: "budget_start: { finance_minister: 0.5, supply_minister: 0.5 }\n"));
        Assert.Equal("budget_start.supply_minister", e.Field);
        Assert.Contains("is abolished on 1950-01-01", e.Message);
        Assert.Contains("budget_owners", e.Message);
    }

    [Fact]
    public void AnEraOverrideCannotSetTheOwners()
    {
        var eras = "eras:\n  - { id: founding, name: The Founding, from: 1948-05-15, to: 1952-01-01 }\n  - { id: later, name: A Later Chapter, from: 1952-01-01, to: 1957-01-01 }\n";
        var files = Files();
        files["eras.yaml"] = eras;
        var dir = Fixture.Write(files);
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "rules"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "rules", "later.yaml"), "budget_owners:\n  supply:\n    - { from: 1952-01-01, office: prime_minister }\n");
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Equal("budget_owners", e.Field);
    }
}
