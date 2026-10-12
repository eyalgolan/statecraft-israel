using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Parties joining or leaving a coalition inside a government's term (#278): the loader checks, and every
/// reader of the coalition (seats, majority, the cabinet's party rule, the causes) uses the coalition of the day.</summary>
public class CoalitionChangesTests
{
    const string Src = "sources: [{ url: \"https://example.org/x\", checked: 2026-10-12 }]";
    const string Open = "    coalition: [alpha, beta]\n";

    static string Changes(params string[] items) => Open + "    coalition_changes:\n" + string.Concat(items.Select(i => "      - " + i + "\n"));
    static string Leave(string date, string party = "beta") => $"{{ date: {date}, leaves: [{party}], {Src} }}";
    static string Join(string date, string party = "beta") => $"{{ date: {date}, joins: [{party}], {Src} }}";

    static Content Load(params string[] items) => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
    {
        ["governments.yaml"] = Fixture.Files["governments.yaml"].Replace(Open, Changes(items)),
        ["people.yaml"] = Fixture.Files["people.yaml"] + Outsider,
    }));

    const string Outsider = @"
  - id: outsider
    name: Out Sider
    living: false
    party: beta
    competence: 9
    loyalty: 3
    offices: []
";

    // Government Two: alpha and beta from 10 March 1949, in a House of 10 where alpha holds 4 and beta 6.
    static readonly Content C = Load(Leave("1949-06-01"), Join("1949-09-01"));

    [Fact]
    public void TheCoalitionOfTheDayFollowsTheChanges()
    {
        var gov = Offices.Government(C, new DateTime(1949, 4, 1))!;
        Assert.Equal(new[] { "alpha", "beta" }, gov.CoalitionOn(new DateTime(1949, 3, 10)));
        Assert.Equal(new[] { "alpha", "beta" }, gov.CoalitionOn(new DateTime(1949, 5, 31)));
        Assert.Equal(new[] { "alpha" }, gov.CoalitionOn(new DateTime(1949, 6, 1)));          // a change applies from its own date
        Assert.Equal(new[] { "alpha" }, gov.CoalitionOn(new DateTime(1949, 8, 31)));
        Assert.Equal(new[] { "alpha", "beta" }, gov.CoalitionOn(new DateTime(1949, 9, 1)));
        Assert.Equal(new[] { "alpha", "beta" }, gov.Coalition);                              // the opening list is untouched
    }

    [Fact]
    public void SeatsAndMajorityUseTheCoalitionOfTheDay()
    {
        Assert.Equal((10, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 5, 31)));
        Assert.True(PoliticsSystem.HasMajority(C, new DateTime(1949, 5, 31)));
        Assert.Equal((4, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 6, 1)));
        Assert.False(PoliticsSystem.HasMajority(C, new DateTime(1949, 6, 1)));
        Assert.Equal((10, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 9, 1)));
        Assert.True(PoliticsSystem.HasMajority(C, new DateTime(1949, 9, 1)));
    }

    [Fact]
    public void AVoteFailsWhileTheCoalitionHasNoMajority()
    {
        var rng = new Rng(1);
        var s = new GameState { Date = new DateTime(1949, 7, 1), CoalitionStability = 1.0 };
        Assert.False(PoliticsSystem.Vote(C, s, ref rng));
        s.Date = new DateTime(1949, 9, 1);
        Assert.True(PoliticsSystem.Vote(C, s, ref rng));
    }

    [Fact]
    public void ExactlyHalfOfTheSeatsIsNotAMajority()
    {
        // The Third Government's 60 of 120 seats (September to December 1952) rest on this rule.
        var half = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["legislatures.yaml"] = Fixture.Files["legislatures.yaml"].Replace("seats: { alpha: 4, beta: 6 }", "seats: { alpha: 5, beta: 5 }"),
            ["governments.yaml"] = Fixture.Files["governments.yaml"].Replace("    coalition: [alpha, beta]\n", "    coalition: [alpha]\n"),
        }));
        Assert.Equal((5, 10), PoliticsSystem.CoalitionSeats(half, new DateTime(1949, 6, 1)));
        Assert.False(PoliticsSystem.HasMajority(half, new DateTime(1949, 6, 1)));
    }

    [Fact]
    public void TheCabinetsPartyRuleUsesTheCoalitionOfTheDay()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1949, 5, 31);
        Assert.Null(Cabinet.Check(C, s, "finance_minister", "outsider"));                    // Beta is in
        s.Date = new DateTime(1949, 6, 1);
        Assert.Equal("Out Sider's party (Beta) is not in the coalition", Cabinet.Check(C, s, "finance_minister", "outsider"));
        s.Date = new DateTime(1949, 9, 1);
        Assert.Null(Cabinet.Check(C, s, "finance_minister", "outsider"));                    // and back in
    }

    [Fact]
    public void TheCausesReportEachChangeAndTheOpeningCoalition()
    {
        var why = new List<Cause>();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 3, 1), new DateTime(1949, 3, 15), why);
        Assert.Equal("Government Two took office (Alpha, Beta)", why.Single(w => w.Target == "gov2").Message);

        why.Clear();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 5, 28), new DateTime(1949, 6, 4), why);
        Assert.Equal("Beta left the coalition (coalition holds 4/10)", why.Single().Message);

        why.Clear();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 8, 28), new DateTime(1949, 9, 4), why);
        Assert.Equal("Beta joined the coalition (coalition holds 10/10)", why.Single().Message);

        why.Clear();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 6, 4), new DateTime(1949, 6, 11), why);
        Assert.Empty(why);
    }

    [Fact]
    public void ATookOfficeCauseNamesTheCoalitionOnTheStartDate()
    {
        var changed = Load(Leave("1949-03-12"));                                             // two days after Government Two starts
        var why = new List<Cause>();
        PoliticsSystem.ReportChanges(changed, new DateTime(1949, 3, 1), new DateTime(1949, 3, 15), why);
        Assert.Equal(new[] { "Government Two took office (Alpha, Beta)", "Beta left the coalition (coalition holds 4/10)" }, why.Select(w => w.Message).ToArray());
    }

    [Fact]
    public void AGovernmentWithoutChangesIsAsBefore()
    {
        var plain = ContentLoader.Load(Fixture.Write());
        var gov = Offices.Government(plain, new DateTime(1949, 4, 1))!;
        Assert.Empty(gov.CoalitionChanges);
        Assert.Equal(gov.Coalition, gov.CoalitionOn(new DateTime(1970, 1, 1)));
    }

    // Loader checks.

    [Fact]
    public void AChangeOutsideTheTermIsALoadError()
    {
        foreach (var date in new[] { "1949-03-10", "1949-03-09" })                           // the start day itself, and before it
        {
            var ex = ValidationTests.Bad("governments.yaml", Open, Changes(Leave(date)));
            Assert.Equal("governments.yaml", ex.File);
            Assert.Equal("governments[gov2].coalition_changes[0].date", ex.Field);
            Assert.Contains("must be inside the term, after 1949-03-10", ex.Message);
        }
        var late = ValidationTests.Bad("governments.yaml", "    to: 1949-03-10\n    coalition: [alpha]\n",
            "    to: 1949-03-10\n    coalition: [alpha]\n    coalition_changes:\n      - " + Join("1949-03-10") + "\n");   // the end day is Government Two's
        Assert.Equal("governments[gov1].coalition_changes[0].date", late.Field);
        Assert.Contains("and before 1949-03-10", late.Message);
    }

    [Fact]
    public void AChangeNeedsADateASourceAndAPartyList()
    {
        var noDate = ValidationTests.Bad("governments.yaml", Open, Changes($"{{ leaves: [beta], {Src} }}"));
        Assert.Equal("governments[gov2].coalition_changes[0].date", noDate.Field);
        var noSource = ValidationTests.Bad("governments.yaml", Open, Changes("{ date: 1949-06-01, leaves: [beta] }"));
        Assert.Equal("governments[gov2].coalition_changes[0].sources", noSource.Field);
        var empty = ValidationTests.Bad("governments.yaml", Open, Changes($"{{ date: 1949-06-01, {Src} }}"));
        Assert.Equal("governments[gov2].coalition_changes[0]", empty.Field);
    }

    [Fact]
    public void AChangeNamesKnownPartiesThatAreInOrOutOfTheCoalition()
    {
        Assert.Equal("governments[gov2].coalition_changes[0].leaves.nobody", ValidationTests.Bad("governments.yaml", Open, Changes(Leave("1949-06-01", "nobody"))).Field);
        Assert.Equal("governments[gov2].coalition_changes[0].joins.nobody", ValidationTests.Bad("governments.yaml", Open, Changes(Join("1949-06-01", "nobody"))).Field);
        Assert.Equal("governments[gov2].coalition_changes[0].joins.beta", ValidationTests.Bad("governments.yaml", Open, Changes(Join("1949-06-01"))).Field);   // already in
        Assert.Equal("governments[gov2].coalition_changes[1].leaves.beta", ValidationTests.Bad("governments.yaml", Open, Changes(Leave("1949-06-01"), Leave("1949-07-01"))).Field);   // already out
        var both = ValidationTests.Bad("governments.yaml", Open, Changes($"{{ date: 1949-06-01, leaves: [beta], joins: [beta], {Src} }}"));
        Assert.Equal("governments[gov2].coalition_changes[0].joins.beta", both.Field);
    }

    [Fact]
    public void ChangesMustComeInDateOrder()
    {
        var ex = ValidationTests.Bad("governments.yaml", Open, Changes(Leave("1949-09-01"), Join("1949-06-01")));
        Assert.Equal("governments[gov2].coalition_changes[1].date", ex.Field);
        Assert.Contains("must be after the previous change (1949-09-01)", ex.Message);
        var same = ValidationTests.Bad("governments.yaml", Open, Changes(Leave("1949-06-01"), Join("1949-06-01")));
        Assert.Equal("governments[gov2].coalition_changes[1].date", same.Field);
    }

    // The real content.

    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    [Fact]
    public void TheThirdGovernmentLosesTheAgudotIn1952AndHasNoMajorityUntilTheFourth()
    {
        Assert.Equal((65, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1952, 9, 17)));
        Assert.Equal((60, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1952, 9, 18)));   // Levin's Welfare term ends that day
        Assert.Equal((60, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1952, 12, 23)));
        Assert.True(PoliticsSystem.HasMajority(Real, new DateTime(1952, 9, 17)));
        Assert.False(PoliticsSystem.HasMajority(Real, new DateTime(1952, 9, 18)));
        Assert.False(PoliticsSystem.HasMajority(Real, new DateTime(1952, 12, 23)));
        Assert.True(PoliticsSystem.HasMajority(Real, new DateTime(1952, 12, 24)));                  // the Fourth Government
        Assert.Equal(new DateTime(1952, 9, 18), Real.Governments.Single(g => g.Id == "third_government").CoalitionChanges.Single().Date);
        Assert.NotNull(Offices.Holder(Real, "welfare_minister", new DateTime(1952, 9, 17)));
        Assert.Null(Offices.Holder(Real, "welfare_minister", new DateTime(1952, 9, 18)));           // no Agudat Yisrael minister left
    }

    [Fact]
    public void TheFourthGovernmentLosesTheGeneralZionistsForAWeekIn1953()
    {
        Assert.Equal((87, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1953, 5, 25)));
        Assert.Equal((64, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1953, 5, 26)));   // he.wiki: 64 without the General Zionist faction
        Assert.Equal((64, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1953, 6, 1)));
        Assert.Equal((87, 120), PoliticsSystem.CoalitionSeats(Real, new DateTime(1953, 6, 2)));
        Assert.True(PoliticsSystem.HasMajority(Real, new DateTime(1953, 5, 26)));                   // 64 of 120 is still a majority
    }

    [Fact]
    public void TheFifthGovernmentGainsTheProgressivesOnTheDayRosenBecomesMinisterOfJustice()
    {
        var before = PoliticsSystem.CoalitionSeats(Real, new DateTime(1954, 1, 31)).seats;
        var after = PoliticsSystem.CoalitionSeats(Real, new DateTime(1954, 2, 1)).seats;
        Assert.Equal(PoliticsSystem.CoalitionSeats(Real, new DateTime(1954, 1, 26)).seats, before);
        Assert.Equal(87, after);                                                                    // the Fourth Government's 87 with the Progressives
        Assert.Equal(Real.Legislatures.Single(l => l.Id == "second_knesset").Seats["progressive"], after - before);
        Assert.Equal("rosen", Offices.Holder(Real, "justice_minister", new DateTime(1954, 2, 1))?.Id);
        Assert.NotEqual("rosen", Offices.Holder(Real, "justice_minister", new DateTime(1954, 1, 31))?.Id);
        var fifth = Real.Governments.Single(g => g.Id == "fifth_government");
        Assert.DoesNotContain("progressive", fifth.CoalitionOn(new DateTime(1954, 1, 31)));
        Assert.Contains("progressive", fifth.CoalitionOn(new DateTime(1954, 2, 1)));
        Assert.Equal(new DateTime(1954, 2, 1), fifth.CoalitionChanges.Single().Date);
    }

    [Fact]
    public void EveryRealChangeHasASourceAndTheRealCoalitionsEndAsTheyBegan()
    {
        foreach (var g in Real.Governments)
            foreach (var ch in g.CoalitionChanges)
            {
                Assert.NotEmpty(ch.Sources);
                Assert.All(ch.Sources, s => Assert.StartsWith("https://", s.Url));
            }
        // The Fourth Government's coalition is whole again when it ends, the Third Government's is not.
        var fourth = Real.Governments.Single(g => g.Id == "fourth_government");
        Assert.Equal(fourth.Coalition.OrderBy(p => p), fourth.CoalitionOn(fourth.To!.Value.AddDays(-1)).OrderBy(p => p));
    }

    [Fact]
    public void ARealRunReportsTheThreeChangesOnTheirDays()
    {
        foreach (var (from, to, text) in new[]
        {
            (new DateTime(1952, 9, 15), new DateTime(1952, 9, 22), "Agudat Yisrael, Poalei Agudat Yisrael left the coalition (coalition holds 60/120)"),
            (new DateTime(1953, 5, 24), new DateTime(1953, 5, 28), "General Zionists, Sephardim and Oriental Communities, Yemenite Association left the coalition (coalition holds 64/120)"),
            (new DateTime(1954, 1, 30), new DateTime(1954, 2, 3), "Progressive Party joined the coalition (coalition holds 87/120)"),
        })
        {
            var why = new List<Cause>();
            PoliticsSystem.ReportChanges(Real, from, to, why);
            Assert.Equal(text, why.Single(w => w.Message.Contains("coalition holds")).Message);
        }
    }
}
