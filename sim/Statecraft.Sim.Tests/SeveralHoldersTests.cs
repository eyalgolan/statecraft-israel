using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Offices that several people may hold at once (#277): the loader rule, the lookups, and the 1952-1956 terms.</summary>
public class SeveralHoldersTests
{
    // Third person on defense beside def_two, same dates.
    static readonly string People = Fixture.Files["people.yaml"] + @"  - id: fin_three
    name: Fin Three
    living: false
    competence: 5
    loyalty: 5
    offices:
      - office: defense_minister
        from: 1949-01-01
        to: 1949-06-01
";

    static Dictionary<string, string> Files(bool several) => new Dictionary<string, string>
    {
        ["people.yaml"] = People,
        ["offices.yaml"] = Fixture.Files["offices.yaml"].Replace("    title: Minister of Defense\n",
            "    title: Minister of Defense\n" + (several ? "    several_holders: true\n" : "")),
    };

    [Fact]
    public void TwoHoldersOfAnOrdinaryOfficeStillFailToLoad()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Files(false))));
        Assert.Equal("offices.defense_minister", ex.Field);
        Assert.Contains("overlapping dates", ex.Message);
    }

    [Fact]
    public void AnOfficeWithSeveralHoldersAcceptsOverlappingTerms()
    {
        var c = ContentLoader.Load(Fixture.Write(Files(true)));
        var day = new DateTime(1949, 3, 1);
        Assert.Equal(new[] { "def_two", "fin_three" }, Offices.Holders(c, "defense_minister", day).Select(p => p.Id).ToArray());
        Assert.Equal("def_two", Offices.Holder(c, "defense_minister", day)!.Id);
        Assert.Equal(new[] { "def_two" }, Offices.Holders(c, "defense_minister", new DateTime(1949, 7, 1)).Select(p => p.Id).ToArray());
        Assert.Empty(Offices.Holders(c, "prime_minister", new DateTime(1948, 1, 1)));
    }

    [Fact]
    public void AHolderWhoDiesLeavesTheListOfHolders()
    {
        var files = Files(true);
        // An open-ended term in the latest government may outlive a death (#167): the holder leaves on the day of death.
        files["people.yaml"] = files["people.yaml"].Replace("    name: Fin Three\n", "    name: Fin Three\n    died: 1949-04-01\n")
            .Replace("        from: 1949-01-01\n        to: 1949-06-01\n", "        from: 1949-03-10\n");
        var c = ContentLoader.Load(Fixture.Write(files));
        Assert.Equal(new[] { "def_two", "fin_three" }, Offices.Holders(c, "defense_minister", new DateTime(1949, 3, 31)).Select(p => p.Id).ToArray());
        Assert.Equal(new[] { "def_two" }, Offices.Holders(c, "defense_minister", new DateTime(1949, 4, 1)).Select(p => p.Id).ToArray());
    }

    [Fact]
    public void TheCabinetListsEveryHolderAndRefusesAppointments()
    {
        var c = ContentLoader.Load(Fixture.Write(Files(true)));
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1949, 3, 15);
        var seat = Cabinet.Lineup(c, s).Single(x => x.Office.Id == "defense_minister");
        Assert.Equal(2, seat.Holders.Count);
        Assert.Equal("def_two", seat.Holder!.Id);
        Assert.Single(Cabinet.Lineup(c, s).Single(x => x.Office.Id == "finance_minister").Holders);
        var reason = Cabinet.Check(c, s, "defense_minister", "pm_one");
        Assert.Contains("several holders", reason);
    }

    [Fact]
    public void AnAppointeeFromAnOlderSaveIsTheSeatsOnlyHolder()
    {
        var c = ContentLoader.Load(Fixture.Write(Files(true)));
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1949, 3, 15);
        s.Appointments["defense_minister"] = new Appointment("pm_one", new DateTime(1949, 3, 12));
        s.AppointedUnder = "gov2";
        var seat = Cabinet.Lineup(c, s).Single(x => x.Office.Id == "defense_minister");
        Assert.True(seat.Appointed);
        Assert.Equal("pm_one", seat.Holder!.Id);
        Assert.Equal(new[] { "pm_one" }, seat.Holders.Select(p => p.Id).ToArray());
    }

    [Fact]
    public void AnEventPlaceholderForSuchAnOfficeFailsToLoad()
    {
        var ev = "id: shortage\ntitle: Bread lines\ntext: '{defense_minister} reports shortages.'\nfrom: 1948-06-01\noptions:\n  - id: a\n    text: Ration food\n    effects:\n      - target: food_supply\n        delta: 0.2\n";
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Files(true), new Dictionary<string, string> { ["shortage.yaml"] = ev })));
        Assert.Contains("several holders", ex.Message);
    }

    /// <summary>The fixture with finance_minister marked several_holders; <paramref name="policiesOwned"/> false moves the
    /// policies to defense_minister (a policy must have an office), so only the effect still names it.</summary>
    static Dictionary<string, string> FinanceSeveral(bool policiesOwned) => new Dictionary<string, string>
    {
        ["offices.yaml"] = Fixture.Files["offices.yaml"].Replace("    title: Minister of Finance\n", "    title: Minister of Finance\n    several_holders: true\n"),
        ["model.yaml"] = policiesOwned ? Fixture.Files["model.yaml"]
            : Fixture.Files["model.yaml"].Replace("    office: finance_minister\n    name: Rationing\n", "    office: defense_minister\n    name: Rationing\n")
                .Replace("    office: finance_minister\n    name: Transit camps\n", "    office: defense_minister\n    name: Transit camps\n"),
    };

    [Fact]
    public void SuchAnOfficeCannotOwnAPolicy()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(FinanceSeveral(true))));
        Assert.Equal("policies[rationing].office", ex.Field);
        Assert.Contains("several holders", ex.Message);
    }

    [Fact]
    public void SuchAnOfficeCannotOwnAnEffect()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(FinanceSeveral(false))));
        Assert.Equal("effects[rationing_food].office", ex.Field);
        Assert.Contains("several holders", ex.Message);
    }

    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());
    static string[] Who(string office, int y, int m, int d) => Offices.Holders(Real, office, new DateTime(y, m, d)).Select(p => p.Id).OrderBy(x => x).ToArray();

    [Fact]
    public void YosefAndLavonAreBothWithoutPortfolioInTheFourthGovernment()
    {
        Assert.Equal(new[] { "dov_yosef", "lavon" }, Who("minister_without_portfolio", 1953, 3, 1));
        Assert.Equal(new[] { "lavon" }, Who("minister_without_portfolio", 1953, 6, 15)); // Yosef's term ends on 15 June 1953
        Assert.Equal(new[] { "lavon" }, Who("minister_without_portfolio", 1952, 12, 23));
    }

    [Fact]
    public void AssafIsASecondDeputyMinisterOfEducationFromJune1956()
    {
        Assert.Equal(new[] { "unna" }, Who("deputy_education_minister", 1956, 6, 10));
        Assert.Equal(new[] { "assaf", "unna" }, Who("deputy_education_minister", 1956, 6, 11));
    }
}
