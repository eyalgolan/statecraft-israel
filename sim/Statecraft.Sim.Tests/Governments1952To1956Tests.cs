using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The Fourth to Seventh Governments (24 December 1952 on, #26) in the real content/.</summary>
public class Governments1952To1956Tests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly DateTime Fourth = new DateTime(1952, 12, 24);
    /// <summary>Offices that are not cabinet posts: their holders do not change with the government.</summary>
    static readonly HashSet<string> NotMinisters = new HashSet<string> { "president", "chief_of_staff" };

    static string Who(string office, int y, int m, int d) => Offices.Holder(C, office, new DateTime(y, m, d))?.Id ?? "(vacant)";

    [Fact]
    public void TheGovernmentsFollowEachOther()
    {
        var ids = new[] { "third_government", "fourth_government", "fifth_government", "sixth_government", "seventh_government" };
        var govs = ids.Select(id => C.Governments.Single(g => g.Id == id)).ToList();
        for (int i = 0; i + 1 < govs.Count; i++) Assert.Equal(govs[i + 1].From, govs[i].To);
        Assert.Equal(new[] { new DateTime(1951, 10, 8), Fourth, new DateTime(1954, 1, 26), new DateTime(1955, 6, 29), new DateTime(1955, 11, 3) },
            govs.Select(g => g.From).ToArray());
        Assert.Null(govs[^1].To);   // to 7 January 1958, after the game ends
        Assert.Equal("seventh_government", Offices.Government(C, new DateTime(1956, 12, 31))!.Id);
    }

    [Fact]
    public void EveryMinistersTermFromTheFourthGovernmentOnLiesInsideOneGovernment()
    {
        foreach (var p in C.People)
            foreach (var t in p.Offices.Where(t => !NotMinisters.Contains(t.Office) && (t.To ?? DateTime.MaxValue) > Fourth))
            {
                var g = Offices.Government(C, t.From);
                Assert.True(g != null, $"{p.Id} {t.Office} from {t.From:yyyy-MM-dd}: no government");
                Assert.True(g!.To == null || (t.To.HasValue && t.To.Value <= g.To.Value),
                    $"{p.Id} {t.Office} {t.From:yyyy-MM-dd} to {t.To:yyyy-MM-dd} runs past the end of {g.Id} ({g.To:yyyy-MM-dd})");
            }
    }

    [Fact]
    public void NoOfficeHasTwoHoldersAtOnce()
    {
        foreach (var office in C.Offices.Where(o => !o.SeveralHolders))
        {
            var terms = C.People.SelectMany(p => p.Offices.Where(t => t.Office == office.Id).Select(t => (p.Id, t.From, To: t.To ?? DateTime.MaxValue)))
                .OrderBy(x => x.From).ToList();
            for (int i = 0; i + 1 < terms.Count; i++)
                Assert.True(terms[i].To <= terms[i + 1].From || terms[i].Id == terms[i + 1].Id,
                    $"{office.Id}: {terms[i].Id} and {terms[i + 1].Id} overlap from {terms[i + 1].From:yyyy-MM-dd}");
        }
    }

    /// <summary>Each government from the Fourth on has a majority on the day it takes office and on the day each new
    /// Knesset convenes during its term. Works with the Second Knesset running on, and with the Third Knesset (#221).</summary>
    [Fact]
    public void EachGovernmentHasAMajorityInEveryKnessetItFaces()
    {
        foreach (var g in C.Governments.Where(g => g.From >= Fourth))
        {
            var dates = new[] { g.From }.Concat(C.Legislatures.Where(l => l.From > g.From && (g.To == null || l.From < g.To.Value)).Select(l => l.From));
            foreach (var d in dates)
                Assert.True(PoliticsSystem.HasMajority(C, d), FormattableString.Invariant(
                    $"{g.Id} on {d:yyyy-MM-dd} ({Offices.Legislature(C, d)?.Id}): {PoliticsSystem.CoalitionSeats(C, d).seats} of {PoliticsSystem.CoalitionSeats(C, d).size}"));
        }
    }

    [Fact]
    public void CoalitionSeatsMatchTheSources()
    {
        Assert.Equal((87, 120), PoliticsSystem.CoalitionSeats(C, Fourth));                     // he.wiki: 87
        Assert.Equal((87, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1954, 1, 26)));
        Assert.Equal((64, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1955, 6, 29))); // election lists; he.wiki's 66 counts later factions
        // The Third Knesset convenes on 15 August 1955 once #221's results are in content/; until then the Second runs on.
        var third = C.Legislatures.FirstOrDefault(l => l.Id == "third_knesset");
        if (third == null) return;
        Assert.Equal((61, 120), PoliticsSystem.CoalitionSeats(C, third.From));
        Assert.Equal((80, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1955, 11, 3)));   // he.wiki and IDI: 80
    }

    [Fact]
    public void CabinetChangesOf1952To1956()
    {
        // Third Government, 1952
        Assert.Equal("haim_cohn", Who("justice_minister", 1952, 6, 25));
        Assert.Equal("dov_yosef", Who("justice_minister", 1952, 6, 24));
        Assert.Equal("levin", Who("welfare_minister", 1952, 9, 17));
        Assert.Equal("(vacant)", Who("welfare_minister", 1952, 9, 18));
        Assert.Equal("lavon", Who("minister_without_portfolio", 1952, 8, 18));
        Assert.Equal("nurock", Who("posts_minister", 1952, 11, 3));
        // Fourth
        Assert.Equal("ben_gurion", Who("prime_minister", 1953, 1, 1));
        Assert.Equal("yosef_sapir", Who("health_minister", 1952, 12, 24));
        Assert.Equal("serlin", Who("health_minister", 1952, 12, 29));
        Assert.Equal("shapira", Who("welfare_minister", 1952, 12, 24));
        Assert.Equal("burg", Who("posts_minister", 1952, 12, 24));
        Assert.Equal("rosen", Who("justice_minister", 1952, 12, 24));
        Assert.Equal("rokach", Who("interior_minister", 1952, 12, 24));
        Assert.Equal("bernstein", Who("trade_minister", 1952, 12, 24));
        Assert.Equal("(vacant)", Who("development_minister", 1953, 6, 14));
        Assert.Equal("dov_yosef", Who("development_minister", 1953, 6, 15));
        // Fifth
        Assert.Equal("sharett", Who("prime_minister", 1954, 1, 26));
        Assert.Equal("lavon", Who("defense_minister", 1954, 1, 26));
        Assert.Equal("ben_gurion", Who("defense_minister", 1955, 2, 21));
        Assert.Equal("(vacant)", Who("justice_minister", 1954, 1, 31));   // the Prime Minister held it; not modelled
        Assert.Equal("rosen", Who("justice_minister", 1954, 2, 1));
        Assert.Equal("aran", Who("minister_without_portfolio", 1954, 1, 26));
        // Sixth
        Assert.Equal("dov_yosef", Who("health_minister", 1955, 6, 29));
        Assert.Equal("naftali", Who("trade_minister", 1955, 6, 29));
        Assert.Equal("shapira", Who("interior_minister", 1955, 6, 29));
        Assert.Equal("aran", Who("transportation_minister", 1955, 6, 29));
        // Seventh
        Assert.Equal("ben_gurion", Who("prime_minister", 1955, 11, 3));
        Assert.Equal("bar_yehuda", Who("interior_minister", 1955, 11, 3));
        Assert.Equal("carmel", Who("transportation_minister", 1955, 11, 3));
        Assert.Equal("bentov", Who("development_minister", 1955, 11, 3));
        Assert.Equal("ben_gurion", Who("justice_minister", 1956, 2, 13));
        Assert.Equal("rosen", Who("justice_minister", 1956, 2, 28));
        Assert.Equal("sharett", Who("foreign_minister", 1956, 6, 18));
        Assert.Equal("golda_meir", Who("foreign_minister", 1956, 6, 19));
        Assert.Equal("namir", Who("labour_minister", 1956, 6, 19));
        // Chiefs of Staff
        Assert.Equal("yadin", Who("chief_of_staff", 1952, 12, 6));
        Assert.Equal("maklef", Who("chief_of_staff", 1952, 12, 7));
        Assert.Equal("dayan", Who("chief_of_staff", 1953, 12, 6));
        Assert.Equal("dayan", Who("chief_of_staff", 1956, 12, 31));
    }

    [Fact]
    public void ReligiousMinistersSitForTheNationalReligiousFrontInTheThirdKnesset()
    {
        foreach (var id in new[] { "shapira", "burg" })
        {
            var p = C.People.Single(x => x.Id == id);
            Assert.Equal("hapoel_hamizrachi", Offices.PartyOf(C, p, new DateTime(1955, 8, 14)));
            Assert.Equal("national_religious_front", Offices.PartyOf(C, p, new DateTime(1955, 8, 15)));
            Assert.Equal("national_religious_front", Offices.PartyOf(C, p, new DateTime(1956, 1, 1)));
        }
        Assert.Contains("national_religious_front", Offices.Government(C, new DateTime(1956, 1, 1))!.Coalition);
    }

    /// <summary>The Ministry of Rationing and Supply is not in the Fourth Government's records. Rationing stays with the
    /// Minister of Trade and Industry, Peretz Bernstein, who said in the Knesset on 4 May 1953 that his ministry holds the
    /// supply tasks and, to some extent, rationing (#283). Yosef, now without portfolio, no longer answers for it.</summary>
    [Fact]
    public void RationingIsTheTradeMinistersAfterTheFourthGovernmentForms()
    {
        Assert.Equal("dov_yosef", Who("rationing_minister", 1952, 12, 23));
        foreach (var (y, m, d) in new[] { (1952, 12, 24), (1953, 1, 1), (1953, 5, 4), (1953, 7, 1), (1953, 12, 31), (1954, 1, 25) })
        {
            Assert.Equal("bernstein", Who("rationing_minister", y, m, d));
            Assert.Equal(Who("trade_minister", y, m, d), Who("rationing_minister", y, m, d));
        }
        Assert.Equal("bernstein", Who("rationing_minister", 1954, 3, 15));
    }

    /// <summary>Postal Services (3 November 1952) and Development (15 June 1953) are not seats before they exist, cannot
    /// be filled then, and an appointment to them works once they do (#224).</summary>
    [Fact]
    public void TheNewOfficesExistOnlyFromTheirStartDates()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1952, 11, 2);
        Assert.DoesNotContain(Cabinet.Lineup(C, s), seat => seat.Office.Id == "posts_minister" || seat.Office.Id == "development_minister");
        Assert.Equal("The office of Minister of Postal Services is not established until " + PlayerText.Date(new DateTime(1952, 11, 3)),
            Cabinet.Check(C, s, "posts_minister", "golda_meir"));
        Assert.Null(Cabinet.Holder(C, s, "posts_minister"));
        s.Date = new DateTime(1953, 6, 14);
        Assert.Contains(Cabinet.Lineup(C, s), seat => seat.Office.Id == "posts_minister" && seat.Holder?.Id == "burg");
        Assert.DoesNotContain(Cabinet.Lineup(C, s), seat => seat.Office.Id == "development_minister");
        Assert.Equal("The office of Minister of Development is not established until " + PlayerText.Date(new DateTime(1953, 6, 15)),
            Cabinet.Check(C, s, "development_minister", "golda_meir"));
        s.Date = new DateTime(1953, 7, 1);
        Assert.Contains(Cabinet.Lineup(C, s), seat => seat.Office.Id == "development_minister" && seat.Holder?.Id == "dov_yosef");
        Assert.Null(Cabinet.Appoint(C, s, "development_minister", "golda_meir", new List<Cause>()));
        Assert.Equal("golda_meir", Cabinet.Holder(C, s, "development_minister")!.Id);
        Assert.Contains(Cabinet.Lineup(C, s), seat => seat.Office.Id == "development_minister" && seat.Appointed);
    }
}
