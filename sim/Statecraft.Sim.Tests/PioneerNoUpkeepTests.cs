using System.Linq;
using Statecraft.Sim;

/// <summary>#1713 (GDD 0.3 pioneer work units) "No per-hex upkeep for fields, roads or rails", pinned so a later change
/// cannot add one unseen. Not red-first: it holds on main today (#803).</summary>
public class PioneerNoUpkeepTests
{
    // R-COR-003 (Q-COR-7): "A built corridor needs no upkeep and yields nothing in the MVP"; R-FIELD-002: a plowed field
    // adds Food with no workers (Q-FIELD-2); docs/gdd/balance.md's road_segment, rail_segment, hula_drainage and
    // plough_field rows have upkeep 0. In the shipped content under gdd-0.1 and gdd-0.3, a game with every corridor segment
    // built and every field and swamp site done keeps the same Treasury, Debt and FX reserves, turn by turn, as the same
    // game without them, while every field yields. Imports are cut in both (the fx crisis, as FieldsTests does), so the
    // fields' Food cannot move Money through what an import costs: only an upkeep could.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuiltRoadsRailsFieldsAndADrainedSwampCostNoMoneyAfterwards(bool gdd03)
    {
        var c = HistoricalPopulationTests.Real(gdd03);
        Assert.Equal(gdd03, c.Ruleset == SaveGame.Gdd03Ruleset);
        var without = Simulation.NewGame(c, 1, Mode.Historical);
        without.ActiveSituations.Add(EconomySystem.FxCrisis);
        without.FxReserves = -1000;
        var with = without.Clone();
        var r = c.Rules.Mvp!;
        foreach (var d in c.Corridors)
        {
            var (_, turns) = Projects.Terms(r, d.Kind == Corridors.Rail ? Corridors.BuildRail : Corridors.BuildRoad);
            with.Corridors[d.Id] = new CorridorWork(d.Id, d.Segments.Select(_ => (SegmentWork?)new SegmentWork(null, turns, without.Date)).ToArray());
        }
        var done = c.Sites.Where(x => x.Kinds.Contains("field") || x.Kinds.Contains("swamp")).ToList();
        foreach (var site in done) with.Sites[site.Id] = SiteWork.Built(site.Id, without.Date);
        Assert.Contains(c.Corridors, d => d.Kind == Corridors.Road);
        Assert.Contains(c.Corridors, d => d.Kind == Corridors.Rail);
        Assert.Contains(done, x => x.Kinds.Contains("swamp"));
        Assert.True(c.Corridors.All(d => Corridors.IsComplete(c, with, d.Id)));

        for (int turn = 1; turn <= 3; turn++)
        {
            without = Simulation.Step(c, without, new Command[0]).State;
            var step = Simulation.Step(c, with, new Command[0]);
            with = step.State;
            Assert.Equal((without.Treasury, without.Debt, without.FxReserves), (with.Treasury, with.Debt, with.FxReserves));
            // R-FIELD-002: every done field yields this turn, so the fields are live in the comparison.
            Assert.Equal(done.Where(x => x.Kinds.Contains("field")).Select(x => x.Id).OrderBy(x => x, System.StringComparer.Ordinal),
                step.Why.Where(x => x.Key == CauseKeys.FoodHarvest).Select(x => x.Args[0]).OrderBy(x => x, System.StringComparer.Ordinal));
        }
        Assert.True(without.Treasury > 0 || without.Debt > 0, "the comparison is over a moving Treasury or Debt");
    }
}
