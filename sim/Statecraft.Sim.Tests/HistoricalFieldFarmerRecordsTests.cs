using System.Globalization;
using Statecraft.Sim;

/// <summary>#1725 PR 4 (Refs #1689): the 20 opening field farmers as Historical ledger records. Owner, 7 November 2026
/// (#1689 comment 6704389240): "On top of census (Recommended)"; (comment 6704565066) "Yes to all three (Recommended)":
/// every farmer staffed on their field from turn 1, one added housing place per farmer in their host; (comment 6704985642)
/// "Per host, 1/18/1 (Recommended)". Owner, 7 November 2026, relayed by the coordinator: Tiberias "F08 veteran, F03 novice
/// (Recommended)"; work XP "0 for all (Recommended)"; housing at Nir Am and Nirim "None for those two for now
/// (Recommended)". Maintainer calls (the coordinator, 7 November 2026): source key field/&lt;label&gt;, ordinal 0; reception
/// done (reception.turns), as for the census.</summary>
public class HistoricalFieldFarmerRecordsTests
{
    const string Prefix = "field/F";

    static Content Gdd03() => HistoricalPopulationTests.Real(true);
    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static (string Label, string Host, string Region, string FieldId)[] Fields(Content c) => c.PlayerRegions.Regions.Values
        .SelectMany(r => r.FieldHosts.Select(h => (h.Field, h.Host, r.Id,
            "field/" + h.Q.ToString(CultureInfo.InvariantCulture) + "," + h.R.ToString(CultureInfo.InvariantCulture))))
        .OrderBy(x => x.Field, StringComparer.Ordinal).ToArray();

    static List<(HistoricalPopulationRow Row, PopulationUnit Unit)> Farmers(Content c)
    {
        var rows = HistoricalPopulation.Inventory(c);
        var units = HistoricalPopulation.LedgerFixture(c).PopulationLedger.Units;
        return rows.Select((r, i) => (r, units[i])).Where(x => x.r.Source.StartsWith(Prefix, StringComparison.Ordinal)).ToList();
    }

    // "On top of census": 20 records, one per field, source field/<label> with ordinal 0 (maintainer call), added to the
    // census's R-CITY-007 units and the soldiers, not taken from them.
    [Fact]
    public void TwentyFarmerRecordsAreAddedOnTopOfTheCensus()
    {
        var c = Gdd03();
        var rows = HistoricalPopulation.Inventory(c);
        var farmers = rows.Where(r => r.Source.StartsWith(Prefix, StringComparison.Ordinal)).ToList();
        Assert.Equal(Fields(c).Select(f => "field/" + f.Label), farmers.Select(r => r.Source));
        Assert.All(farmers, r => { Assert.Equal(0, r.Ordinal); Assert.Equal(PopulationStatus.FreeCivilian, r.Status); });
        Assert.Equal(20, farmers.Count);
        Assert.Equal(c.Cities.Sum(x => x.Civilians), rows.Count(r => r.Source.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal)));
        Assert.Equal(c.Cities.Sum(x => x.Civilians) + HistoricalPopulation.OpeningFormations(c).Sum(f => f.Strength) + 20, rows.Count);
    }

    // "every farmer is staffed on their field from turn 1": a free civilian farmer with the field's Work assignment, living
    // in the field's host (its region, city = host), reception done (maintainer call), work XP 0 (owner, relayed: "0 for
    // all (Recommended)"), no military fields, no tools (#1693: "Tools in the MVP: no.").
    [Fact]
    public void EachFarmerIsStaffedOnItsFieldFromTurnOne()
    {
        var c = Gdd03();
        var byLabel = Fields(c).ToDictionary(f => "field/" + f.Label, StringComparer.Ordinal);
        var farmers = Farmers(c);
        Assert.Equal(20, farmers.Count);
        foreach (var (row, u) in farmers)
        {
            var f = byLabel[row.Source];
            Assert.Equal(PopulationStatus.FreeCivilian, u.Status);
            Assert.Equal(PopulationAssignmentKind.Work, u.Assignment.Kind);
            Assert.Equal(f.FieldId, u.Assignment.Target);
            Assert.Equal(f.Region, u.Location!.Region);
            Assert.Equal(f.Host, u.Location.City);
            Assert.Equal(CivilianProfession.Farmer, u.Profession);
            Assert.Equal(0, u.WorkXp);
            Assert.Equal(c.RequirePopulationInteger("reception.turns"), u.ReceptionTicks);
            Assert.Equal("", u.Qualification);
            Assert.Equal(0, u.CombatXp);
            Assert.Null(u.Equipment.ToolsId);
            Assert.False(u.Equipment.ToolsPaid);
            Assert.Null(u.Equipment.MilitaryId);
        }
    }

    // Tiers: 1:2:1 per host (Gdd03FarmerTierSplit), in field-label order within a host. Tiberias (F03 to F08) is the only
    // host with more than one tier: owner, relayed, "F08 veteran, F03 novice (Recommended)". 1 / 18 / 1 nationally.
    [Fact]
    public void TiersFollowThePerHostSplitWithF03NoviceAndF08Veteran()
    {
        var c = Gdd03();
        var tiers = Farmers(c).ToDictionary(x => x.Row.Source, x => x.Unit.Tier, StringComparer.Ordinal);
        Assert.Equal(CivilianTier.Novice, tiers["field/F03"]);
        foreach (var l in new[] { "F04", "F05", "F06", "F07" }) Assert.Equal(CivilianTier.Trained, tiers["field/" + l]);
        Assert.Equal(CivilianTier.Veteran, tiers["field/F08"]);
        Assert.Equal(18, tiers.Values.Count(t => t == CivilianTier.Trained));
        Assert.Equal(1, tiers.Values.Count(t => t == CivilianTier.Novice));
        Assert.Equal(1, tiers.Values.Count(t => t == CivilianTier.Veteran));
        var hostOf = Fields(c).ToDictionary(f => "field/" + f.Label, f => f.Host, StringComparer.Ordinal);
        foreach (var h in HistoricalPopulation.FieldFarmerHosts(c))
        {
            var mine = tiers.Where(t => hostOf[t.Key] == h.Host).Select(t => t.Value).ToList();
            Assert.Equal((h.Novice, h.Trained, h.Veteran),
                (mine.Count(t => t == CivilianTier.Novice), mine.Count(t => t == CivilianTier.Trained), mine.Count(t => t == CivilianTier.Veteran)));
        }
    }

    // "one added housing place per farmer in their host": the dark start adds each census host's farmers to its city's
    // housing capacity. Nir Am and Nirim have no city record: "None for those two for now (Recommended)", so 18 of the 20
    // places exist until the housing producer (#1725 PR 7). The public NewGame is unchanged.
    [Fact]
    public void TheDarkStartAddsOneHousingPlacePerFarmerInCensusHostsOnly()
    {
        var c = Gdd03();
        var dark = Dark(c);
        var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var hosted = Fields(c).GroupBy(f => f.Host).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        foreach (var x in pub.Cities.Values)
            Assert.Equal(x.HousingCapacity + (hosted.TryGetValue(x.Id, out var n) ? n : 0), dark.Cities[x.Id].HousingCapacity);
        Assert.Equal(18, dark.Cities.Values.Sum(x => x.HousingCapacity) - pub.Cities.Values.Sum(x => x.HousingCapacity));
        Assert.Equal(pub.Cities.Keys.OrderBy(k => k, StringComparer.Ordinal), dark.Cities.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.DoesNotContain("nir_am", dark.Cities.Keys);
        Assert.DoesNotContain("nirim", dark.Cities.Keys);
        Assert.Equal(c.Cities.Single(x => x.Id == "haifa").HousingCapacity, pub.Cities["haifa"].HousingCapacity);
    }

    // Farmers stay out of the employed (R-CITY-007) and so out of R-OBJ-004's employed and civilians: the dark start's
    // cities keep the census's civilians and employed, and every farmer holds a field, not an Industry or city job.
    [Fact]
    public void FarmersStayOutOfTheEmployedAndRObj004()
    {
        var c = Gdd03();
        var dark = Dark(c);
        var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var farmers = dark.PopulationLedger.Units.Where(u => u.Profession == CivilianProfession.Farmer).ToList();
        Assert.Equal(20, farmers.Count);
        var fields = new HashSet<string>(Gdd03FieldFixture.Fields(c).Select(f => f.Id), StringComparer.Ordinal);
        Assert.All(farmers, u => Assert.Contains(u.Assignment.Target!, fields));
        foreach (var x in pub.Cities.Values)
        {
            Assert.Equal(x.Employed, dark.Cities[x.Id].Employed);
            Assert.Equal(x.Civilians, dark.Cities[x.Id].Civilians);
            Assert.Equal(x.Craftsmen, dark.Cities[x.Id].Craftsmen);
        }
    }

    // R-TRAIN-001: Train takes "civilians without a job". A farmer holds its field's Work assignment, so no farmer is an
    // unassigned free civilian, and the aggregate Train pool (Training.Available) is the public start's in every city.
    [Fact]
    public void FarmersAreNotInTheTrainPool()
    {
        var c = Gdd03();
        var dark = Dark(c);
        var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var farmers = dark.PopulationLedger.Units.Where(u => u.Profession == CivilianProfession.Farmer).ToList();
        Assert.Equal(20, farmers.Count);
        Assert.DoesNotContain(farmers, u => u.Assignment.Kind == PopulationAssignmentKind.Unassigned);
        foreach (var id in pub.Cities.Keys)
            Assert.Equal(Training.Available(pub, id, Array.Empty<Command>()), Training.Available(dark, id, Array.Empty<Command>()));
    }

    // R-POP-001: field/ rows are the Historical start's, like census/ and units_1948/, so another fixture mode refuses them.
    [Fact]
    public void FarmerRowsNeedAHistoricalLedger()
    {
        var c = Gdd03();
        var region = c.PlayerRegions.Regions.Keys.First();
        var bindings = new PopulationBindings(new[] { new PopulationLocation(region) }, Array.Empty<PopulationTarget>(),
            Array.Empty<string>(), Array.Empty<string>());
        var row = new PopulationInventoryRow("field/F01", 0, new PopulationUnit(0, PopulationStatus.FreeCivilian, 0,
            new PopulationLocation(region), PopulationAssignment.Unassigned, CivilianProfession.None, CivilianTier.None, 0, "", 0,
            PopulationEquipment.None));
        var open = new GameState(c);
        Assert.Throws<ArgumentException>(() => open.PopulationLedger.Initialize(c, bindings, new[] { row }, 0, Mode.Open));
    }
}
