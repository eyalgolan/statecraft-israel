using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Checks the real content/ folder at the repository root.</summary>
public class RealContentTests
{
    public static string ContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "content", "people.yaml"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("content/people.yaml not found above test dir"), "content");
    }

    static readonly Content C = ContentLoader.Load(ContentRoot());
    static readonly DateTime Checked = new DateTime(2026, 10, 9);

    [Fact]
    public void MaimonBirthUsesHebrewBiographyDateAndSource()
    {
        var p = C.People.Single(p => p.Id == "maimon");
        Assert.Equal(new DateTime(1875, 12, 11), p.Born);
        Assert.Contains(p.BiographySources["born"], source => source.Url == "https://he.wikipedia.org/wiki/יהודה_לייב_פישמן_מימון" && source.Checked == new DateTime(2026, 10, 11));
    }

    [Fact]
    public void CurrentPeopleHaveSourcedBiographyValues()
    {
        foreach (var p in C.People)
        {
            // living: false means the death date was confirmed, so it must be recorded; a living person has none.
            Assert.Equal(p.Living == false, p.Died.HasValue);
            if (p.Living == true) continue;   // living people are gated separately (#62); this pass covers the dead
            Assert.False(string.IsNullOrWhiteSpace(p.NameHe));
            Assert.NotNull(p.Born);
            foreach (var key in new[] { "name_he", "born", "died" })
            {
                Assert.True(p.BiographySources.TryGetValue(key, out var sources), $"{p.Id}: missing biography_sources.{key}");
                Assert.NotEmpty(sources);
                Assert.All(sources, source => Assert.True(Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http")));
            }
        }
    }

    [Fact]
    public void BenGurionBiographyValuesAreSourced()
    {
        var p = C.People.Single(p => p.Id == "ben_gurion");
        Assert.Equal("דוד בן-גוריון", p.NameHe);
        Assert.Equal(new DateTime(1886, 10, 16), p.Born);
        Assert.Equal(new DateTime(1973, 12, 1), p.Died);
        foreach (var key in new[] { "name_he", "born", "died" })
        {
            Assert.NotEmpty(p.BiographySources[key]);
            Assert.All(p.BiographySources[key], source =>
            {
                Assert.True(Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"));
                Assert.True(source.Checked >= new DateTime(2026, 10, 11), $"{key}: {source.Url} checked before the 11 October review");
            });
        }
    }

    [Fact]
    public void EveryHistoricalRecordIsSourcedAndFactChecked()
    {
        foreach (var p in C.People)
        {
            Assert.True(p.Status == "fact-checked", $"people[{p.Id}] status {p.Status}");
            foreach (var t in p.Offices) Assert.True(t.Sources.Count > 0, $"people[{p.Id}].offices.{t.Office} has no source");
        }
        foreach (var l in C.Legislatures) { Assert.Equal("fact-checked", l.Status); Assert.NotEmpty(l.Sources); }
        foreach (var g in C.Governments) { Assert.Equal("fact-checked", g.Status); Assert.NotEmpty(g.Sources); }
        foreach (var a in C.Aliyah) { Assert.Equal("fact-checked", a.Status); Assert.NotEmpty(a.Sources); }
        foreach (var a in C.PopulationAnchors) { Assert.Equal("fact-checked", a.Status); Assert.NotEmpty(a.Sources); }
        foreach (var e in C.Events)   // a sensitive event passes the owner's sensitivity review on top of fact-checking (D11)
        {
            Assert.Equal(ReleaseGate.SensitiveEvents(C).Contains(e.Id) ? ReleaseGate.ReviewedStatus : "fact-checked", e.Status);
            Assert.NotEmpty(e.Sources);
        }
        var all = C.People.SelectMany(p => p.Offices.SelectMany(t => t.Sources))
            .Concat(C.Legislatures.SelectMany(l => l.Sources)).Concat(C.Governments.SelectMany(g => g.Sources))
            .Concat(C.Aliyah.SelectMany(a => a.Sources)).Concat(C.PopulationAnchors.SelectMany(a => a.Sources))
            .Concat(C.Events.SelectMany(e => e.Sources));
        Assert.All(all, s => { Assert.StartsWith("https://", s.Url); Assert.True(s.Checked >= Checked, $"{s.Url} checked {s.Checked:yyyy-MM-dd}, before {Checked:yyyy-MM-dd}"); });   // later re-checks are allowed
    }

    [Theory]
    [InlineData("deputy_agriculture_minister", "efrati", 1951, 2, 26, 1951, 10, 8)]
    [InlineData("deputy_agriculture_minister", "efrati", 1951, 11, 4, 1952, 6, 25)]
    [InlineData("deputy_transportation_minister", "shari", 1951, 4, 2, 1951, 10, 8)]
    [InlineData("deputy_education_minister", "kahana", 1951, 11, 4, 1952, 12, 24)]
    [InlineData("deputy_religions_minister", "warhaftig", 1954, 6, 15, 1955, 6, 29)]
    [InlineData("deputy_welfare_minister", "ben_meir", 1953, 1, 5, 1954, 1, 26)]
    [InlineData("deputy_trade_minister", "susayeff", 1953, 6, 15, 1954, 1, 26)]
    public void DeputyTitlesHaveDatedTermsAndNoCabinetPowers(string office, string person,
        int y1, int m1, int d1, int y2, int m2, int d2)
    {
        var from = new DateTime(y1, m1, d1);
        var to = new DateTime(y2, m2, d2);
        Assert.True(C.Offices.Single(o => o.Id == office).HistoricalOnly);
        Assert.Null(Offices.Holder(C, office, from.AddDays(-1)));
        Assert.Equal(person, Offices.Holder(C, office, from)?.Id);
        Assert.Equal(person, Offices.Holder(C, office, to.AddDays(-1))?.Id);
        Assert.Null(Offices.Holder(C, office, to));
        var state = Simulation.NewGame(C, 42, Mode.Historical);
        state.Date = from;
        Assert.DoesNotContain(Cabinet.Lineup(C, state), seat => seat.Office.Id == office);
        Assert.NotNull(Cabinet.Check(C, state, office, "ben_gurion"));
    }

    [Fact]
    public void KaplanDeputyPrimeMinisterTitleFollowsHisDepartureFromFinance()
    {
        var change = new DateTime(1952, 6, 25);
        var end = new DateTime(1952, 7, 13);
        Assert.True(C.Offices.Single(o => o.Id == "deputy_prime_minister").HistoricalOnly);
        Assert.Null(Offices.Holder(C, "deputy_prime_minister", change.AddDays(-1)));
        Assert.Equal("kaplan", Offices.Holder(C, "finance_minister", change.AddDays(-1))?.Id);
        Assert.Equal("eshkol", Offices.Holder(C, "agriculture_minister", change.AddDays(-1))?.Id);
        Assert.Equal("naftali", Offices.Holder(C, "minister_without_portfolio", change.AddDays(-1))?.Id);
        Assert.Equal("kaplan", Offices.Holder(C, "deputy_prime_minister", change)?.Id);
        Assert.Equal("kaplan", Offices.Holder(C, "minister_without_portfolio", change)?.Id);
        Assert.Equal("eshkol", Offices.Holder(C, "finance_minister", change)?.Id);
        Assert.Equal("naftali", Offices.Holder(C, "agriculture_minister", change)?.Id);
        Assert.Equal("kaplan", Offices.Holder(C, "deputy_prime_minister", end.AddDays(-1))?.Id);
        Assert.Null(Offices.Holder(C, "deputy_prime_minister", end));
        Assert.Null(Offices.Holder(C, "minister_without_portfolio", end));
        // The same holders continue into the Fourth Government (24 December 1952).
        Assert.Equal("eshkol", Offices.Holder(C, "finance_minister", new DateTime(1952, 12, 24))?.Id);
        Assert.Equal("naftali", Offices.Holder(C, "agriculture_minister", new DateTime(1952, 12, 24))?.Id);
        var state = Simulation.NewGame(C, 42, Mode.Historical);
        state.Date = change.AddDays(-1);
        Assert.Equal(0.9, EffectsSystem.Multiplier(C, state, "agriculture_minister") /
            EffectsSystem.BudgetFactor(C, state, "agriculture_minister"), 10);
        state.Date = change;
        Assert.Equal(0.85, EffectsSystem.Multiplier(C, state, "agriculture_minister") /
            EffectsSystem.BudgetFactor(C, state, "agriculture_minister"), 10);
        Assert.Equal("kaplan", Cabinet.Holder(C, state, "minister_without_portfolio")?.Id);
        Assert.DoesNotContain(Cabinet.Lineup(C, state), seat => seat.Office.Id == "deputy_prime_minister");
        state.Date = end;
        Assert.Equal("Eliezer Kaplan died on " + PlayerText.Date(end), Cabinet.Check(C, state, "finance_minister", "kaplan"));
    }

    [Fact]
    public void OpenQuestionsAreExactlyTheKnownOnes()
    {
        // #3: research_notes/budget-approval-1948-56.md answers budget_approval for the council and the First and Second
        // Knessets. The Third Knesset's stays open: its 1956/57 law is read, its 1957-59 laws are not.
        var open = C.Legislatures.SelectMany(l => l.OpenQuestions.Select(q => $"{l.Id}.{q}")).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "third_knesset.budget_approval" }, open);
    }

    [Fact]
    public void TheProvisionalStateCouncilApprovedTheBudget()
    {
        // #3: the council adopted the January 1949 Budget Ordinance unanimously (sitting 33, 30 December 1948) and the
        // January-March 1949 Budget Ordinance 21-1 (sitting 38, 27 January 1949).
        var council = C.Legislatures.Single(l => l.Id == "provisional_state_council");
        Assert.True(council.BudgetApproval);
        foreach (var url in new[] { "https://fs.knesset.gov.il/0/law/0_lsr_312670.pdf", "https://fs.knesset.gov.il/0/Plenum/0_ptm_250001.pdf", "https://fs.knesset.gov.il/0/Plenum/0_ptm_250006.pdf" })
            Assert.Contains(council.Sources, s => s.Url == url);
        Assert.True(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void EveryEventTextResolvesOnItsStartDate()
    {
        foreach (var e in C.Events)
        {
            Offices.Fill(C, e.Title, e.From);
            Offices.Fill(C, e.Text, e.From);
            foreach (var o in e.Options) Offices.Fill(C, o.Text, e.From);
        }
    }

    [Fact]
    public void PartyFollowsTheTermHeldOnTheDate()
    {
        var rosen = C.People.Single(p => p.Id == "rosen");
        Assert.Equal("new_aliyah", Offices.PartyOf(C, rosen, new DateTime(1948, 6, 1)));
        Assert.Equal("progressive", Offices.PartyOf(C, rosen, new DateTime(1949, 6, 1)));
        foreach (var id in new[] { "shapira", "maimon", "levin" })
        {
            var p = C.People.Single(x => x.Id == id);
            Assert.Equal(p.Party, Offices.PartyOf(C, p, new DateTime(1948, 6, 1)));
            Assert.Equal("united_religious_front", Offices.PartyOf(C, p, new DateTime(1949, 6, 1)));
        }
    }

    [Fact]
    public void SpotChecksAgainstVerifiedFacts()
    {
        Assert.Equal("ben_gurion", Offices.Holder(C, "defense_minister", new DateTime(1948, 6, 1))!.Id);
        Assert.Equal("dori", Offices.Holder(C, "chief_of_staff", new DateTime(1949, 11, 8))!.Id);
        Assert.Equal("yadin", Offices.Holder(C, "chief_of_staff", new DateTime(1949, 11, 9))!.Id);
        Assert.Equal("dov_yosef", Offices.Holder(C, "rationing_minister", new DateTime(1949, 6, 1))!.Id);
        Assert.Null(Offices.Holder(C, "rationing_minister", new DateTime(1949, 3, 9)));    // First Government took office 10 March 1949
        Assert.Equal("dov_yosef", Offices.Holder(C, "rationing_minister", new DateTime(1949, 3, 10))!.Id);
        // #225: Yosef held Rationing from 10 March 1949 (Knesset OData, gov1.pdf). Agriculture was unassigned until 22 March 1949.
        Assert.Null(Offices.Holder(C, "agriculture_minister", new DateTime(1949, 3, 21)));
        Assert.Equal("dov_yosef", Offices.Holder(C, "agriculture_minister", new DateTime(1949, 3, 22))!.Id);
        // #310: Trade and Industry and Health were assigned on 22 March 1949 too (plenum 1_ptm_250087, Knesset OData).
        foreach (var (office, holder) in new[] { ("trade_minister", "kaplan"), ("health_minister", "shapira") })
        {
            Assert.Null(Offices.Holder(C, office, new DateTime(1949, 3, 10)));
            Assert.Null(Offices.Holder(C, office, new DateTime(1949, 3, 21)));
            Assert.Equal(holder, Offices.Holder(C, office, new DateTime(1949, 3, 22))!.Id);
        }
        Assert.Equal("bernstein", Offices.Holder(C, "trade_minister", new DateTime(1949, 3, 9))!.Id);
        Assert.Equal("shapira", Offices.Holder(C, "health_minister", new DateTime(1949, 3, 9))!.Id);
        Assert.Equal((30, 37), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));
        Assert.Equal((73, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 6, 1)));
        Assert.Equal(101828, C.Aliyah[0].Origins.Values.Sum());
        Assert.Equal(239954, C.Aliyah[1].Origins.Values.Sum());
        Assert.Equal(13, C.Actors.Count);
    }

    [Fact]
    public void ImposingAusterityTurnsRationingOn()
    {
        var e = C.Events.Single(x => x.Id == "austerity_1949");
        Assert.Equal(1.0, e.Options.Single(o => o.Id == "impose").SetPolicy["rationing"]);
        Assert.Empty(e.Options.Single(o => o.Id == "decline").SetPolicy);
    }

    [Fact]
    public void OfficeWithoutABudgetLineHasANeutralBudgetFactor()
    {
        Assert.DoesNotContain("rationing_minister", C.Rules.BudgetStart.Keys);
        var s = Simulation.NewGame(C, 1, Mode.Historical);
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(C, s, "rationing_minister"));
    }

    [Fact]
    public void GovernmentsAndKnessetsOf1950And1951()
    {
        Assert.Equal("first_government", Offices.Government(C, new DateTime(1950, 10, 31))!.Id);
        Assert.Equal("second_government", Offices.Government(C, new DateTime(1950, 11, 1))!.Id);
        Assert.Equal("third_government", Offices.Government(C, new DateTime(1951, 10, 8))!.Id);
        Assert.Equal("first_knesset", Offices.Legislature(C, new DateTime(1951, 8, 19))!.Id);
        Assert.Equal("second_knesset", Offices.Legislature(C, new DateTime(1951, 8, 20))!.Id);
        Assert.Equal((73, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1951, 1, 1)));
        Assert.Equal((51, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1951, 9, 1)));   // transitional government, new Knesset
        Assert.Equal((65, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1951, 10, 8)));
    }

    [Fact]
    public void BenGurionTransportationTermEndsAtFourthGovernmentHandover()
    {
        Assert.Equal("ben_gurion", Offices.Holder(C, "transportation_minister", new DateTime(1952, 12, 23))!.Id);
        Assert.Equal("serlin", Offices.Holder(C, "transportation_minister", new DateTime(1952, 12, 24))!.Id);
        Assert.Equal("yosef_sapir", Offices.Holder(C, "transportation_minister", new DateTime(1952, 12, 29))!.Id);
        var term = C.People.Single(p => p.Id == "ben_gurion").Offices.Single(t => t.Office == "transportation_minister" && t.From < new DateTime(1953, 1, 1));   // the 1953 one is #314's gap
        Assert.Equal(new DateTime(1952, 12, 24), term.To);
        Assert.NotEmpty(term.Sources);
    }

    [Fact]
    public void OfficesLeftByDeathsIn1952HaveTheirSuccessors()
    {
        string Who(string office, int y, int m, int d) => Offices.Holder(C, office, new DateTime(y, m, d))?.Id ?? "(vacant)";
        Assert.Equal("pinkas", Who("transportation_minister", 1952, 8, 13));
        Assert.Equal("ben_gurion", Who("transportation_minister", 1952, 8, 14));
        Assert.Equal("weizmann", Who("president", 1952, 11, 8));
        Assert.Equal("(vacant)", Who("president", 1952, 11, 9));   // Sprinzak acting; acting holders are not modelled
        Assert.Equal("(vacant)", Who("president", 1952, 12, 15));
        Assert.Equal("ben_zvi", Who("president", 1952, 12, 16));
    }

    [Fact]
    public void TheArmisticeEventsHaveAFiniteWindowEndingWith1949()
    {
        foreach (var id in new[] { "armistice_egypt", "armistice_jordan", "armistice_lebanon", "armistice_syria" })
            Assert.Equal(new DateTime(1950, 1, 1), C.Events.Single(e => e.Id == id).To);
    }

    [Fact]
    public void CabinetChangesOf1950And1951()
    {
        string Who(string office, int y, int m, int d) => Offices.Holder(C, office, new DateTime(y, m, d))?.Id ?? "(vacant)";
        Assert.Equal("shazar", Who("education_minister", 1950, 10, 31));
        Assert.Equal("remez", Who("education_minister", 1950, 11, 1));
        Assert.Equal("ben_gurion", Who("education_minister", 1951, 5, 19));
        Assert.Equal("dinur", Who("education_minister", 1951, 10, 8));
        Assert.Equal("lavon", Who("rationing_minister", 1950, 11, 1));
        Assert.Equal("lavon", Who("rationing_minister", 1951, 10, 7));
        Assert.Equal("dov_yosef", Who("rationing_minister", 1951, 10, 8));
        Assert.Equal("dov_yosef", Who("rationing_minister", 1951, 12, 31));
        Assert.Equal("dov_yosef", Who("rationing_minister", 1952, 12, 23));
        Assert.Equal("bernstein", Who("rationing_minister", 1952, 12, 24));   // Fourth Government, Trade and Industry (#283)
        Assert.Equal("dov_yosef", Who("transportation_minister", 1951, 1, 1));
        Assert.Equal("pinkas", Who("transportation_minister", 1951, 10, 8));
        Assert.Equal("lavon", Who("agriculture_minister", 1951, 1, 1));
        Assert.Equal("eshkol", Who("agriculture_minister", 1951, 10, 8));
        Assert.Equal("geri", Who("trade_minister", 1951, 1, 1));
        Assert.Equal("dov_yosef", Who("trade_minister", 1951, 10, 8));
        Assert.Equal("dov_yosef", Who("justice_minister", 1951, 10, 8));
        Assert.Equal("rosen", Who("justice_minister", 1951, 10, 7));
        Assert.Equal("burg", Who("health_minister", 1951, 10, 8));
        Assert.Equal("weizmann", Who("president", 1951, 11, 19));
        Assert.Equal("yadin", Who("chief_of_staff", 1951, 12, 31));
        Assert.Null(C.Offices.Single(o => o.Id == "rationing_minister").Until);   // the responsibility outlived the ministry (Addendum 2)
        Assert.Equal(new DateTime(1951, 10, 8), C.Offices.Single(o => o.Id == "immigration_minister").Until);
        Assert.Equal(new DateTime(1951, 10, 8), C.Offices.Single(o => o.Id == "war_victims_minister").Until);
        Assert.Equal("shapira", Who("immigration_minister", 1951, 10, 7));
        Assert.Equal(0.15, C.Rules.BudgetStart["prime_minister"]);   // the Immigration line keeps its id (#108)
        var sheetrit = C.People.Single(p => p.Id == "sheetrit");
        Assert.Equal("sephardim", Offices.PartyOf(C, sheetrit, new DateTime(1951, 1, 1)));
        Assert.Equal("mapai", Offices.PartyOf(C, sheetrit, new DateTime(1951, 10, 8)));
        // Second Knesset factions from 20 August 1951 (Knesset faction records, #457).
        Assert.Equal("mapai", Offices.PartyOf(C, sheetrit, new DateTime(1951, 8, 20)));
        Assert.Equal("sephardim", Offices.PartyOf(C, sheetrit, new DateTime(1951, 8, 19)));
        var shapira = C.People.Single(p => p.Id == "shapira");
        Assert.Equal("united_religious_front", Offices.PartyOf(C, shapira, new DateTime(1951, 8, 19)));
        Assert.Equal("hapoel_hamizrachi", Offices.PartyOf(C, shapira, new DateTime(1951, 8, 20)));
        var levin = C.People.Single(p => p.Id == "levin");
        Assert.Equal("united_religious_front", Offices.PartyOf(C, levin, new DateTime(1951, 8, 19)));
        Assert.Equal("agudat_yisrael", Offices.PartyOf(C, levin, new DateTime(1951, 8, 20)));
        Assert.Equal("", Offices.PartyOf(C, C.People.Single(p => p.Id == "geri"), new DateTime(1951, 1, 1)));
    }

    [Fact]
    public void TheTransitCampCardOpensInTheMonthEshkolProposedTheCamps()
    {
        // Kachansky, "The Ma'abarot" (Yad Ben-Zvi, 1987): Eshkol raised the camps' crisis at the Jewish Agency executive on
        // 27 March 1950 and proposed transit housing two weeks later, so in April; the card opens at the start of that
        // month (#442). Wikipedia's "March 1950" is recorded in the event's comment.
        var e = C.Events.Single(x => x.Id == "maabarot_policy");
        Assert.Equal(new DateTime(1950, 4, 1), e.From);
    }

    [Fact]
    public void PresidencyAndMaimonsTermsEnd()
    {
        Assert.Equal("weizmann", Offices.Holder(C, "president", new DateTime(1950, 1, 1))!.Id);
        Assert.Null(Offices.Holder(C, "president", new DateTime(1949, 2, 16)));
        var maimon = C.People.Single(p => p.Id == "maimon");
        // Knesset position records run both Second Government terms to 8 October 1951 (#457).
        foreach (var office in new[] { "religions_minister", "war_victims_minister" })
        {
            Assert.Equal(new DateTime(1951, 10, 8), maimon.Offices.Single(t => t.Office == office && t.From == new DateTime(1950, 11, 1)).To);
            Assert.Equal("maimon", Offices.Holder(C, office, new DateTime(1951, 3, 1))!.Id);
            Assert.Equal("maimon", Offices.Holder(C, office, new DateTime(1951, 10, 7))!.Id);
        }
    }

    [Fact]
    public void EveryEventOf1950And1951FiresInTheHistoricalPlaythrough()
    {
        var s = GoldenRun.Play(C, new DateTime(1952, 1, 1));
        Assert.Null(s.GameOver);
        var expected = new[] { "clothing_rationing_1950", "education_registration_1951", "forex_crisis_1951", "frumkin_commission",
            "hula_drainage", "iraqi_exodus", "israel_bonds", "law_of_return", "maabarot_policy", "ministry_supply_crisis", "reparations_claim" };
        var missing = expected.Where(id => !s.FiredEvents.Contains(id)).ToList();
        Assert.True(missing.Count == 0, "Never fired: " + string.Join(", ", missing));
        Assert.Equal(1.0, s.Policies["maabarot"]);   // the historical choice opens transit camps
    }
}
