using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/projects.yaml (R-PROJ-001): the real kinds load with the owner's proposed design values (Q-PROJ-1,
/// Q-HOUSE-3), and the loader refuses a kind that could not be built, would cost less than nothing, or would move a condition
/// out of its 0 to 1 scale in one step.</summary>
public class ProjectsContentTests
{
    [Fact]
    public void TheRealKindsLoad()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(new[] { ("housing", 6.0, 6, 5, 0.03, 0), ("transit_camp", 1.5, 1, 5, -0.02, 0), ("industry", 2.0, 6, 0, 0.0, 5),
                             ("winery", 2.0, 6, 0, 0.0, 10) },
                     c.ProjectKinds.Select(k => (k.Id, k.Cost, k.Turns, k.Capacity, k.Condition, k.JobSlots)));
        Assert.Equal((1.0, 0.5), (c.Rules.AttentionCost["start_project"], c.Rules.AttentionCost["cancel_project"]));   // Q-PROJ-5
    }

    // R-PROJ-005: the winery needs vineyards, and Rishon LeZion is the one city that has them (owner, 20 October 2026, on
    // #737, replacing the 18 October default of Zikhron Ya'akov), with what its sources leave open.
    [Fact]
    public void TheWineryIsInRishonLeZion()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal("vineyards", c.ProjectKinds.Single(k => k.Id == "winery").Requires);
        Assert.Equal(new[] { "rishon_lezion" }, c.Cities.Where(x => x.Resources.Any(r => r.Id == "vineyards")).Select(x => x.Id));
        Assert.NotEmpty(c.Cities.Single(x => x.Id == "rishon_lezion").Resources.Single().ToVerify);
    }

    [Theory]
    [InlineData("id: housing, name: a housing project", "id: transit_camp, name: a housing project", "projects[transit_camp].id")]   // duplicate id
    [InlineData("name: a housing project", "name: ''", "projects[housing].name")]
    [InlineData("cost: 3, turns: 2", "cost: -1, turns: 2", "projects[housing].cost")]
    [InlineData("cost: 3, turns: 2", "cost: 3, turns: 0", "projects[housing].turns")]
    [InlineData("capacity: 4", "capacity: -1", "projects[housing].capacity")]
    [InlineData("condition: 0.1, job_slots: 0 }\n  - { id: transit_camp", "condition: 1.5, job_slots: 0 }\n  - { id: transit_camp", "projects[housing].condition")]
    [InlineData("job_slots: 3", "job_slots: -3", "projects[industry].job_slots")]
    public void ABadKindIsRefused(string from, string to, string where)
    {
        Assert.Contains(from, Fixture.GddProjects);
        var dir = Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["projects.yaml"] = Fixture.GddProjects.Replace(from, to) }));
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Contains(where, e.Message);
    }

    /// <summary>The fixture's cities.yaml with <paramref name="row"/> (a `resources:` key) added to town_a.</summary>
    static string WithVineyards(string row) => "\ncities:\n" + Fixture.City("town_a", 12, 8, 10, 6, "[military_camp, hakhshara]").Replace("hakhshara] }", "hakhshara], " + row + " }");

    // R-PROJ-005: a resource is a fact about the city, so it names its sources, each with a url and a checked date.
    [Theory]
    [InlineData("resources: [{ id: vineyards }]", "cities[town_a].resources[vineyards].sources")]
    [InlineData("resources: [{ id: vineyards, sources: [{ url: \"https://example.org/v\" }] }]", "cities[town_a].resources[vineyards].sources[0]")]
    [InlineData("resources: [{ id: '', sources: [{ url: \"https://example.org/v\", checked: 2026-10-18 }] }]", "cities[town_a].resources[].id")]
    public void AResourceNeedsASource(string row, string where)
    {
        var dir = Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["cities.yaml"] = WithVineyards(row) }));
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Contains(where, e.Message);
    }

    // R-PROJ-005: a kind that requires a resource no city has could never be built, so the loader refuses it; with the
    // resource on a city it loads.
    [Fact]
    public void AKindNeedsACityWithItsResource()
    {
        var projects = new Dictionary<string, string> { ["projects.yaml"] = Fixture.GddProjects + Fixture.Winery };
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(projects))));
        Assert.Contains("projects[winery].requires", e.Message);
        projects["cities.yaml"] = WithVineyards(Fixture.Vineyards);
        Assert.Equal("vineyards", ContentLoader.Load(Fixture.Write(Fixture.Gdd(projects))).ProjectKinds.Single(k => k.Id == "winery").Requires);
    }
}
