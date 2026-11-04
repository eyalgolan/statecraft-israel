using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class Gdd03PromotionGateTests
{
    /// <summary>The real content with the shipped gdd loop forced to the gdd-0.3 ruleset (Gdd03MoneyLedgerTests' pattern).</summary>
    static Content Load()
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(root, rel);
            var text = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
            return rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, System.IO.Directory.GetFiles(System.IO.Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + System.IO.Path.GetFileName(p)));
    }

    // #2528: R-VET-001/002 apply "Under legacy/gdd-0.1" (mvp.md:207-209); under gdd-0.3 the profession tiers take over
    // (#1689). A halutz building the first yishuv site in a real gdd-0.3 game gains no service and is not promoted.
    [Fact]
    public void Gdd03HalutzBuildingIsNotPromoted()
    {
        var c = Load();
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var halutz = s.Units.Values.First(u => u.Type == "halutz");
        var site = c.Sites.First(x => x.Kinds.Contains(Projects.YishuvKind)).Id;
        s.Sites[site] = new SiteWork(site, halutz.Id, 5, null);

        Projects.Advance(c, s, s.Date.AddDays(1), new List<Cause>());

        Assert.Equal("halutz", s.Units[halutz.Id].Type);
        Assert.Equal(halutz.Service, s.Units[halutz.Id].Service);
    }
}
