using System;
using System.Linq;
using Statecraft.Sim;

/// <summary>The Yishuv windows in the real content (#80): a fresh Founding start holds exactly the records it held before.</summary>
public class YishuvWindowsTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    [Fact]
    public void AFreshFoundingStartHoldsTheSameRecordsAsBeforeTheWindows()
    {
        var s = Simulation.NewGame(C, 1, Mode.Historical);
        Assert.Equal(new[] { "food_supply", "housing", "intl_standing", "morale", "security" }, s.Indicators.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Equal(new[] { "border_settlement", "housing_construction", "maabarot", "rationing" }, s.Policies.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Equal(new[] { "border_villages_secure", "building_houses", "food_lifts_cohesion", "homes_lift_morale", "rationing_feeds", "rationing_grates", "safety_lifts_morale", "work_lifts_cohesion" },
            s.EffectContributions.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void TheStateRecordsBeginOnTheFoundingDateAndTheUnOnItsOwn()
    {
        var founding = new DateTime(1948, 5, 15);
        foreach (var id in new[] { "ussr", "egypt", "jordan", "syria", "lebanon", "iraq", "arab_refugees", "arab_citizens", "west_germany" })
            Assert.Equal(founding, C.Actors.Single(a => a.Id == id).From);
        Assert.Equal(new DateTime(1945, 10, 24), C.Actors.Single(a => a.Id == "un").From);
        Assert.Null(C.Actors.Single(a => a.Id == "usa").From);
        foreach (var id in new[] { "housing_construction", "border_settlement", "maabarot" })
            Assert.Equal(founding, C.Policies.Single(p => p.Id == id).From);
    }

    [Fact]
    public void DorisTwoTermsSplitAtTheFoundingAndKeepTheirSources()
    {
        var dori = C.People.Single(p => p.Id == "dori");
        var haganah = dori.Offices.Single(t => t.Office == "haganah_chief_of_staff");
        var idf = dori.Offices.Single(t => t.Office == "chief_of_staff");
        Assert.Equal(new DateTime(1947, 6, 1), haganah.From); Assert.Equal(new DateTime(1948, 5, 14), haganah.To);
        Assert.Equal(new DateTime(1948, 5, 14), idf.From); Assert.Equal(new DateTime(1949, 11, 9), idf.To);
        Assert.NotEmpty(haganah.Sources); Assert.Equal(haganah.Sources[0].Url, idf.Sources[0].Url);
        Assert.Equal(new DateTime(1948, 5, 14), C.Offices.Single(o => o.Id == "haganah_chief_of_staff").Until);
        Assert.Equal("dori", Offices.Holder(C, "chief_of_staff", new DateTime(1948, 5, 15))!.Id);
    }
}
