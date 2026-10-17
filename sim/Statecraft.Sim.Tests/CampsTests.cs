using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Build a training camp (GDD "Pioneer work": "Build training camp"; D-D5's "More are built by pioneers"; Phase 4
/// PR 3). Q-CAMP-1 and Q-CAMP-3 are the plan's recommendations, which the owner took on 17 October 2026 (#782): a camp site
/// belongs to one city, building it adds the site's facility to that city, and one action builds a military camp or a
/// Hakhshara, as the site's kind says, with no capacity limit.</summary>
public class CampsTests
{
    /// <summary>The GDD fixture with town_a holding no facility, its yishuv site site_a on (2, 0), and camp_a, a site of
    /// <paramref name="kind"/> on (3, 0) that belongs to town_a. The fixture's camp takes 2 turns and IL 1m.</summary>
    static Content Town(string kind = "military_camp") => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["cities.yaml"] = "\ncities:\n" + Fixture.City("town_a", 12, 8, 10, 6),
        ["sites.yaml"] = Fixture.GddSites + Fixture.GddSites.Replace("sites:\n", "").Replace("site_a", "camp_a").Replace("Site A", "Camp A")
            .Replace("q: 2", "q: 3").Replace("kinds: [yishuv]", $"kinds: [{kind}]").Replace("    founded: 1949-06-12\n", "    city: town_a\n"),
    })));

    static GameState WithHalutz(Content c, out int id)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static readonly Train Recruits = new Train("town_a", "recruit", 1);

    // R-CAMP-001: the order pays build_camp_cost and starts build_camp_turns; Train is refused until the camp is built, and
    // accepted from the turn after the one it is built on (an order on turn T with 2 turns is built at the end of T+1).
    [Fact]
    public void ABuiltCampLetsItsCityTrain()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        var paid = s.Clone();
        double before = paid.Treasury;
        Assert.Null(Projects.Start(c, paid, id, Projects.BuildCamp, "camp_a", new List<Cause>()));
        Assert.Equal((before - 1.0, 2), (paid.Treasury, paid.Sites["camp_a"].TurnsLeft));

        var first = Step(c, s, new PioneerWork(id, Projects.BuildCamp, "camp_a"), Recruits);
        Assert.Equal("Town A has no military camp", Assert.Single(first.Rejected).Reason);
        var built = Step(c, first.State, Recruits);
        Assert.Equal("Town A has no military camp", Assert.Single(built.Rejected).Reason);   // the order came before the build
        Assert.Equal(built.State.Date, built.State.Sites["camp_a"].BuiltOn);
        Assert.Contains(built.Why, x => x.System == "map" && x.Message == "Camp A built");
        Assert.False(Projects.IsBusy(built.State, id));
        var next = Step(c, built.State, Recruits);
        Assert.Empty(next.Rejected);
        Assert.Single(next.State.Cohorts);
    }

    // R-CAMP-001, Q-CAMP-3: the camp gives its city the site's facility and no other; a Hakhshara trains halutzim.
    [Fact]
    public void ACampGivesItsCityOnlyTheSitesFacility()
    {
        var c = Town("hakhshara");
        var s = WithHalutz(c, out var id);
        s.Sites["camp_a"] = SiteWork.Built("camp_a", s.Date);
        Assert.True(Training.HasFacility(c, s, "town_a", "hakhshara"));
        Assert.False(Training.HasFacility(c, s, "town_a", "military_camp"));
        Assert.Equal("Town A has no military camp", Training.Start(c, s.Clone(), "town_a", "recruit", 1, new List<Cause>()));
        Assert.Empty(Step(c, s, new Train("town_a", "halutz", 1)).Rejected);
    }

    // R-SITE-004: build_camp only at a camp site, and build_yishuv not at one; refused with the state untouched.
    [Fact]
    public void ACampSiteOfTheOtherKindIsRefused()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        var before = s.Clone();
        Assert.Equal("Site A is not a site for a training camp", Projects.Start(c, s, id, Projects.BuildCamp, "site_a", new List<Cause>()));
        Assert.Equal("Camp A is not a site for a new yishuv", Projects.Start(c, s, id, Projects.BuildYishuv, "camp_a", new List<Cause>()));
        Assert.Equal(before.Treasury, s.Treasury);
        Assert.Empty(s.Sites);
    }

    // R-CAMP-001: both values are required in the GDD loop, so a missing key is never read as 0 (a free camp, or one built at once).
    [Theory]
    [InlineData("  build_camp_turns: 2\n", "", "mvp.build_camp_turns", "required when eras.yaml systems.loop is gdd")]
    [InlineData("build_camp_turns: 2", "build_camp_turns: 0", "mvp.build_camp_turns", "must be >= 1")]
    [InlineData("  build_camp_cost: 1\n", "", "mvp.build_camp_cost", "required when eras.yaml systems.loop is gdd")]
    [InlineData("build_camp_cost: 1", "build_camp_cost: -1", "mvp.build_camp_cost", "must be a finite number >= 0")]
    [InlineData("build_camp_cost: 1", "build_camp_cost: .nan", "mvp.build_camp_cost", "must be a finite number >= 0")]
    public void BadCampValuesFailLoad(string from, string to, string field, string message)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(("rules.yaml", field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }
}
