using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>GDD Phase 2 PR 2: Money is the Treasury (spec §1.2, "Treasury becomes Money"; owner, 15 October 2026,
/// Q-MONEY-4), and debt and foreign currency stay in the sim, internal under Money (D-C3, Q-MONEY-1).</summary>
public class MoneyTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    // R-MONEY-001: the player sees the Treasury as Money; the account keeps its id, and debt and foreign currency keep theirs.
    [Fact]
    public void TheTreasuryIsShownAsMoney()
    {
        Assert.Equal("Money", PlayerText.Target(C, "treasury"));
        Assert.Equal("IL 2.0m", PlayerText.Money(C, "treasury", 2));
        Assert.Equal(new[] { "treasury", "debt", "fx_reserves" }, C.Money.Select(m => m.Id));
        Assert.Equal("Debt", PlayerText.Target(C, "debt"));
        Assert.Equal("Foreign currency", PlayerText.Target(C, "fx_reserves"));
    }

    // R-MONEY-002: step 7 pays the annual upkeep of each completed civilian place for the turn's share of a year.
    // The fixture uses non-zero rates to exercise the rule; every real MVP rate is 0 (owner option 3, 18 October 2026).
    [Fact]
    public void UpkeepIsPaidInStepSeven()
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"]
            .Replace("city_upkeep_per_year: 0", "city_upkeep_per_year: 1")
            .Replace("housing_upkeep_per_year: 0", "housing_upkeep_per_year: 2")
            .Replace("industry_upkeep_per_year: 0", "industry_upkeep_per_year: 3")
            .Replace("transit_camp_upkeep_per_year: 0", "transit_camp_upkeep_per_year: 4")
            .Replace("military_camp_upkeep_per_year: 0", "military_camp_upkeep_per_year: 5")
            .Replace("hakhshara_upkeep_per_year: 0", "hakhshara_upkeep_per_year: 6");
        // town_a has a military camp and a Hakhshara; site_b adds a built camp, site_c a Hakhshara still being built.
        files["sites.yaml"] = Fixture.GddSites
            + Camp("site_b", "Site B", "military_camp", 3) + Camp("site_c", "Site C", "hakhshara", 4);
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.CityProjects.Add(1, new CityProject(1, "housing", "town_a", 1, 0, 0, s.Date));
        s.CityProjects.Add(2, new CityProject(2, "industry", "town_a", 2, 0, 0, s.Date));
        s.CityProjects.Add(3, new CityProject(3, "transit_camp", "town_a", 3, 0, 0, s.Date));
        s.CityProjects.Add(4, new CityProject(4, "housing", "town_a", 4, 0, 2));   // unfinished: no upkeep
        s.Sites["site_b"] = SiteWork.Built("site_b", s.Date);
        s.Units.Add(98, new Unit(98, "halutz", 4, 0));                             // a site being built names its unit
        s.Sites["site_c"] = new SiteWork("site_c", 98, 2, null);                  // unfinished: no upkeep
        var shortTreasury = s.Clone();
        s.Treasury = 100;
        const double yearFraction = 0.25;
        var jobIncome = 6 * 1.2 * yearFraction;
        var upkeep = (1 + 2 + 3 + 4 + 2 * 5 + 6) * yearFraction;   // one city, two military camps, one Hakhshara
        var why = new List<Cause>();

        TurnResolver.Resolve(c, s, s.Date.AddMonths(3), yearFraction, why);

        Assert.Equal(100 + jobIncome - upkeep, s.Treasury, 9);
        Assert.Equal(-upkeep, Assert.Single(why, x => x.Message.EndsWith("(civilian upkeep)")).Delta, 9);

        // R-MONEY-003: a Treasury that cannot pay borrows the shortfall and stops at 0.
        shortTreasury.Treasury = 0;
        var debt = shortTreasury.Debt;
        TurnResolver.Resolve(c, shortTreasury, shortTreasury.Date.AddMonths(3), yearFraction, new List<Cause>());
        Assert.Equal(0, shortTreasury.Treasury);
        Assert.Equal(debt + upkeep - jobIncome, shortTreasury.Debt, 9);
    }

    static string Camp(string id, string name, string kind, int q) => Fixture.GddSites.Replace("sites:\n", "")
        .Replace("site_a", id).Replace("Site A", name).Replace("q: 2", $"q: {q}").Replace("kinds: [yishuv]", $"kinds: [{kind}]")
        .Replace("    founded: 1949-06-12\n", "    city: town_a\n");

    // R-MONEY-002: every rate is required in the GDD loop, so an omitted kind never silently becomes free.
    [Theory]
    [InlineData("  hakhshara_upkeep_per_year: 0\n", "", "mvp.hakhshara_upkeep_per_year", "required")]
    [InlineData("  city_upkeep_per_year: 0\n", "  city_upkeep_per_year: -1\n", "mvp.city_upkeep_per_year", ">= 0")]
    [InlineData("  housing_upkeep_per_year: 0\n", "  housing_upkeep_per_year: .nan\n", "mvp.housing_upkeep_per_year", ">= 0")]
    public void TheUpkeepRatesAreRequiredAndInRange(string line, string replacement, string field, string reason)
    {
        var files = Fixture.Gdd();
        Assert.Contains(line, files["rules.yaml"]);
        files["rules.yaml"] = files["rules.yaml"].Replace(line, replacement);
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Contains(field, e.Message);
        Assert.Contains(reason, e.Message);
    }

    // R-MONEY-002, owner option 3 (18 October 2026, #884): in the real MVP no civilian place pays upkeep, so a turn with
    // every kind built charges nothing and names no upkeep cause.
    [Fact]
    public void TheMvpChargesNoCivilianUpkeep()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var city = C.Cities[0].Id;
        foreach (var (id, kind) in new[] { (1, "housing"), (2, "industry"), (3, "transit_camp") })
            s.CityProjects.Add(id, new CityProject(id, kind, city, id, 0, 0, s.Date));
        foreach (var site in C.Sites.Where(x => x.Kinds.Contains("military_camp") || x.Kinds.Contains("hakhshara")))
            s.Sites[site.Id] = SiteWork.Built(site.Id, s.Date);

        Assert.Equal(0, Cities.AnnualUpkeep(C, s, C.Rules.Mvp!));
        var why = new List<Cause>();
        TurnResolver.Resolve(C, s, s.Date.AddMonths(1), 1 / 12.0, why);
        Assert.DoesNotContain(why, x => x.Message.Contains("upkeep"));
    }

    // R-MONEY-001 (D-C3): a guard, not a change. The real game runs the GDD loop, its debt and foreign currency are in the
    // state hash, and the sourced 1951 crisis still happens: debt is taken on, and reserves run out after 19 October 1951
    // (FRUS 1951 v.V doc 513) and before the turn that covers the end of 1951, as EconomyCalibrationTests anchors.
    [Fact]
    public void DebtAndForeignCurrencyStayInTheSim()
    {
        Assert.True(C.IsGdd);
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var history = new List<GameState> { s };
        while (s.Date <= new DateTime(1952, 1, 1) && s.GameOver == null)
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            history.Add(s);
        }
        Assert.True(history.Last().Debt > 0, "the run never borrowed");
        var i = history.FindIndex(x => x.ActiveSituations.Contains(EconomySystem.FxCrisis));
        Assert.True(i > 0, "foreign currency never ran out");
        Assert.True(history[i].Date > new DateTime(1951, 10, 19) && history[i - 1].Date < new DateTime(1952, 1, 1),
            "reserves ran out in the turn ending " + PlayerText.Date(history[i].Date));

        var start = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var hash = start.Hash();
        var withDebt = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical); withDebt.Debt += 1;
        var withFx = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical); withFx.FxReserves += 1;
        Assert.NotEqual(hash, withDebt.Hash());
        Assert.NotEqual(hash, withFx.Hash());
    }
}
