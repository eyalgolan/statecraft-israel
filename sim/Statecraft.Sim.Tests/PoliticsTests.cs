using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class PoliticsTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void CoalitionSeatsFollowGovernmentAndLegislatureOnDate()
    {
        Assert.Equal((3, 5), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));   // gov1 [alpha] in council
        Assert.Equal((4, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 3, 1)));  // gov1 [alpha] in house
        Assert.Equal((10, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 3, 10))); // gov2 [alpha,beta]
        Assert.False(PoliticsSystem.HasMajority(C, new DateTime(1949, 3, 1)));
        Assert.True(PoliticsSystem.HasMajority(C, new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void BudgetVoteRequirementIsPerLegislature()
    {
        Assert.False(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1948, 6, 1)));
        Assert.True(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1949, 6, 1)));
    }

    [Fact]
    public void VoteFailsWithoutMajorityAndPassesWithStableMajority()
    {
        var rng = new Rng(1);
        var s = new GameState { Date = new DateTime(1949, 3, 1), CoalitionStability = 1.0 };
        Assert.False(PoliticsSystem.Vote(C, s, ref rng));
        s.Date = new DateTime(1949, 6, 1);
        Assert.True(PoliticsSystem.Vote(C, s, ref rng));
    }

    [Fact]
    public void StabilityMovesTowardApprovalAndCollapseEndsGame()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1), CoalitionStability = 1.0 };
        EffectsSystem.Init(C, s); // approval 0.5
        var why = new List<Cause>();
        PoliticsSystem.UpdateStability(C, s, 0.25, why);
        Assert.Equal(0.75, s.CoalitionStability, 6);
        s.CoalitionStability = 0.1; s.Indicators["morale"] = 0.0;
        PoliticsSystem.UpdateStability(C, s, 0.25, why);
        Assert.Equal("coalition_collapse", s.GameOver);
    }

    [Fact]
    public void ReportsNewLegislatureAndGovernment()
    {
        var why = new List<Cause>();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 2, 1), new DateTime(1949, 5, 1), why);
        Assert.Contains(why, w => w.Target == "house");
        Assert.Contains(why, w => w.Target == "gov2");
    }

    [Fact]
    public void PolicyRejectedWithNoLegislatureSaysSo()
    {
        var c = ContentLoader.Load(Fixture.Write(new System.Collections.Generic.Dictionary<string, string>
        {
            ["legislatures.yaml"] = Fixture.Files["legislatures.yaml"].Replace("from: 1948-05-14", "from: 1948-06-01"),
        }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Null(Offices.Legislature(c, s.Date));
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("rationing", 1.0) });
        Assert.Equal("No legislature is sitting", Assert.Single(r.Rejected).Reason);
    }

    [Fact]
    public void GovernmentChangeNamesThePartiesByName()
    {
        var why = new System.Collections.Generic.List<Cause>();
        PoliticsSystem.ReportChanges(C, new System.DateTime(1949, 3, 1), new System.DateTime(1949, 3, 15), why);
        Assert.Contains(why, w => w.Message == "Government Two took office (Alpha, Beta)");
    }

    [Fact]
    public void PoliticalProducerArgsKeepSeatOrderAndPartyIds()
    {
        var legislature = new List<Cause>();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 2, 1), new DateTime(1949, 3, 1), legislature);
        var convened = Assert.Single(legislature, w => w.Target == "house");
        Assert.Equal(CauseKeys.LegislatureConvened, convened.Key);
        Assert.Equal(new[] { "4", "10" }, convened.Args); // unequal fixture counts discriminate a swap
        var government = new List<Cause>();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 3, 1), new DateTime(1949, 3, 15), government);
        var installed = Assert.Single(government, w => w.Target == "gov2");
        Assert.Equal(CauseKeys.GovernmentTookOffice, installed.Key);
        Assert.Equal(new[] { "alpha|beta" }, installed.Args);
        Assert.Equal("Government Two took office (Alpha, Beta)", installed.Message);
    }

}
