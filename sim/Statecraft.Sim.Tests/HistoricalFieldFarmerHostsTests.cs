using Statecraft.Sim;

/// <summary>#1725 PR 4 (Refs #1689): each opening field's farmer, counted per host; HistoricalFieldFarmerRecordsTests
/// checks the records built from the count. Owner, 7 November 2026 (#1689 comment 6704389240): "On top of census (Recommended)"; (comment
/// 6704565066) "Yes to all three (Recommended)": every farmer staffed on their field from turn 1 and one added housing
/// place per farmer in their host; (comment 6704985642) "Per host, 1/18/1 (Recommended)". Owner, 3 November 2026 (#1694):
/// "One farmer per field (Recommended)". Hosts: regions.yaml field_hexes (#1717, #2924).</summary>
public class HistoricalFieldFarmerHostsTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    // The ruling as numbers: one farmer per opening field, counted in the field's host, split 1:2:1 per host by
    // Gdd03FarmerTierSplit. Tiberias hosts six fields (1 / 4 / 1, the 6-farmer ruling on #2465); every other host has
    // one or two, which the split makes all trained.
    [Fact]
    public void EachHostSplitsItsFarmersOneToTwoToOne()
    {
        var expected = new (string Host, int Novice, int Trained, int Veteran)[]
        {
            ("hadera", 0, 2, 0), ("haifa", 0, 2, 0), ("nir_am", 0, 1, 0), ("nirim", 0, 1, 0), ("petah_tikva", 0, 2, 0),
            ("rehovot", 0, 1, 0), ("rishon_lezion", 0, 1, 0), ("rosh_pinna", 0, 2, 0), ("tiberias", 1, 4, 1),
            ("zikhron_yaakov", 0, 2, 0),
        };
        var hosts = HistoricalPopulation.FieldFarmerHosts(Gdd03());
        Assert.Equal(expected, hosts.Select(h => (h.Host, h.Novice, h.Trained, h.Veteran)));
        foreach (var h in hosts)
        {
            Assert.Equal(Gdd03FarmerTierSplit.Split(h.Fields.Count), (h.Novice, h.Trained, h.Veteran));
            Assert.Equal(h.Fields.Count, h.Farmers);
        }
    }

    // "Per host, 1/18/1 (Recommended)": the per-host split gives 1 / 18 / 1 nationally, 20 farmers, the Agreed
    // start.prepared_fields, all on top of the census.
    [Fact]
    public void ThePerHostSplitGivesOneEighteenOneNationally()
    {
        var c = Gdd03();
        var hosts = HistoricalPopulation.FieldFarmerHosts(c);
        Assert.Equal((1, 18, 1), (hosts.Sum(h => h.Novice), hosts.Sum(h => h.Trained), hosts.Sum(h => h.Veteran)));
        Assert.Equal(20, hosts.Sum(h => h.Farmers));
        Assert.Equal(c.RequirePopulationInteger("start.prepared_fields"), hosts.Sum(h => h.Farmers));
    }

    // "One farmer per field": every opening field (Gdd03FieldFixture's ids) is counted once, in one host, and that host
    // is a member location of the field's own region (Q27: a farmer lives in the field's region).
    [Fact]
    public void EveryOpeningFieldHasOneFarmerInAHostOfItsRegion()
    {
        var c = Gdd03();
        var hosts = HistoricalPopulation.FieldFarmerHosts(c);
        var fields = Gdd03FieldFixture.Fields(c);
        var counted = hosts.SelectMany(h => h.Fields).ToList();
        Assert.Equal(fields.Select(f => f.Id).OrderBy(x => x, StringComparer.Ordinal), counted.OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(counted.Count, counted.Distinct(StringComparer.Ordinal).Count());
        foreach (var h in hosts)
        {
            Assert.Equal(h.Region, c.PlayerRegions.AtLocation(h.Host)!.Id);
            foreach (var id in h.Fields) Assert.Equal(h.Region, fields.Single(f => f.Id == id).Region);
        }
    }

    // "one added housing place per farmer in their host": the count each host gains. Nir Am and Nirim host one farmer each
    // and have no cities.yaml record, so no city housing exists there to add the place to (an open question, not built).
    [Fact]
    public void EachFarmerGivesOneHousingPlaceInTheirHost()
    {
        var c = Gdd03();
        var hosts = HistoricalPopulation.FieldFarmerHosts(c);
        foreach (var h in hosts) Assert.Equal(h.Farmers, h.HousingPlaces);
        Assert.Equal(20, hosts.Sum(h => h.HousingPlaces));
        foreach (var settlement in new[] { "nir_am", "nirim" })
        {
            Assert.Equal(1, hosts.Single(h => h.Host == settlement).HousingPlaces);
            Assert.DoesNotContain(c.Cities, x => x.Id == settlement);
        }
    }

    // A field total other than the Agreed start.prepared_fields (20) is refused: no farmer count is invented or dropped.
    [Fact]
    public void AFieldTotalOtherThanThePreparedFieldsIsRefused()
    {
        var root = RealContentTests.ContentRoot();
        var regions = File.ReadAllText(Path.Combine(root, "regions.yaml"));
        var f20 = regions.Split('\n').Single(l => l.Contains("field: F20,", StringComparison.Ordinal));
        var trimmed = regions.Replace("  field_hexes:\n" + f20 + "\n", "  field_hexes: []\n", StringComparison.Ordinal);
        Assert.NotEqual(regions, trimmed);
        var c = ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            if (rel == "regions.yaml") return trimmed;
            return rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
        Assert.Equal(19, Gdd03FieldFixture.Fields(c).Count);
        var e = Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.FieldFarmerHosts(c));
        Assert.Contains("start.prepared_fields", e.Message, StringComparison.Ordinal);
    }

    // R-POP-001: only exact gdd-0.3 has farmers on the ledger; gdd-0.1 content refuses.
    [Fact]
    public void Gdd01ContentRefuses()
        => Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.FieldFarmerHosts(HistoricalPopulationTests.Real(false)));
}
