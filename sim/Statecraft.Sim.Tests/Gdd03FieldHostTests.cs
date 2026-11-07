using Statecraft.Sim;

/// <summary>R-POP-004 (#1717, #1689; owner, 7 November 2026): the host of each opening field, data under exact gdd-0.3 only.</summary>
public class Gdd03FieldHostTests
{
    static string Real => File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml"));

    // Each row against research_notes/GDD phase 4/opening-field-farmer-cities-1717.md, whose F numbers follow the file's
    // order, and the owner's comments on #1689. Basis: the note's class (H sourced, A administrative, R forced, I inference),
    // the owner's "Haifa, Haifa, Rehovot (Recommended)" (design choices, not sourced), and Decision A for the two settlements.
    static readonly (string Field, int Q, int R, string Region, string Host, string Basis)[] Approved =
    {
        ("F01", 9, 5, "galilee", "rosh_pinna", "inference"),
        ("F02", 14, 7, "galilee", "rosh_pinna", "inference"),
        ("F03", 7, 11, "valleys", "tiberias", "forced"),
        ("F04", 7, 12, "valleys", "tiberias", "forced"),
        ("F05", 8, 13, "valleys", "tiberias", "forced"),
        ("F06", 9, 13, "valleys", "tiberias", "forced"),
        ("F07", 11, 12, "valleys", "tiberias", "administrative"),
        ("F08", 12, 11, "valleys", "tiberias", "administrative"),
        ("F09", 1, 15, "haifa_sharon", "hadera", "sourced"),
        ("F10", 2, 14, "haifa_sharon", "zikhron_yaakov", "sourced"),
        ("F11", 2, 15, "haifa_sharon", "hadera", "inference"),
        ("F12", 3, 13, "haifa_sharon", "zikhron_yaakov", "sourced"),
        ("F13", 6, 10, "haifa_sharon", "haifa", "design_choice"),
        ("F14", 6, 12, "haifa_sharon", "haifa", "design_choice"),
        ("F15", -2, 21, "tel_aviv_jaffa", "petah_tikva", "sourced"),
        ("F16", -1, 20, "tel_aviv_jaffa", "petah_tikva", "sourced"),
        ("F17", -5, 25, "central_plain", "rehovot", "design_choice"),
        ("F18", -4, 25, "central_plain", "rishon_lezion", "inference"),
        ("F19", -11, 31, "southern_coast", "nir_am", "decision_a"),
        ("F20", -10, 33, "negev", "nirim", "decision_a"),
    };

    [Fact]
    public void EveryOpeningFieldHasItsApprovedHostInItsOwnRegion()
    {
        var c = PlayerRegionsContentTests.Load(Real);
        var shipped = c.PlayerRegions.Regions.Values.SelectMany(r => r.FieldHosts.Select(h => (r.Id, h))).OrderBy(x => x.h.Field, StringComparer.Ordinal).ToArray();
        Assert.Equal(Approved, shipped.Select(x => (x.h.Field, x.h.Q, x.h.R, x.Id, x.h.Host, x.h.Basis)));
        Assert.All(shipped, x => Assert.False(string.IsNullOrWhiteSpace(x.h.Source)));
        Assert.All(shipped, x => Assert.Equal(x.Id, c.PlayerRegions.AtLocation(x.h.Host)!.Id));
        Assert.All(shipped, x => Assert.Contains((x.h.Q, x.h.R), c.PlayerRegions.Regions[x.Id].FieldHexes));
    }

    [Fact]
    public void OnlyF19AndF20AreHostedOutsideTheCensusCities()
    {
        var c = PlayerRegionsContentTests.Load(Real);
        var census = c.Cities.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var outside = c.PlayerRegions.Regions.Values.SelectMany(r => r.FieldHosts).Where(h => !census.Contains(h.Host)).Select(h => h.Field);
        Assert.Equal(new[] { "F19", "F20" }, outside.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void TheThreeDesignChoicesSayTheyAreNotSourced()
    {
        var c = PlayerRegionsContentTests.Load(Real);
        var design = c.PlayerRegions.Regions.Values.SelectMany(r => r.FieldHosts).Where(h => h.Basis == "design_choice").OrderBy(h => h.Field, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "F13", "F14", "F17" }, design.Select(h => h.Field));
        Assert.All(design, h => Assert.Contains("not sourced", h.Source));
    }

    // Owner, 7 November 2026: "Add Nirim to Negev (Recommended)". Its record is map/locations_1948.yaml's (#2113), on (-16,36).
    [Fact]
    public void NirimIsAMemberOfTheNegevAndTheLabelStays()
    {
        var c = PlayerRegionsContentTests.Load(Real);
        var negev = c.PlayerRegions.Regions["negev"];
        Assert.Same(negev, c.PlayerRegions.AtLocation("nirim"));
        Assert.Same(negev, c.PlayerRegions.AtHex(-16, 36));
        Assert.Equal("beersheba", negev.CapitalLocation);
        Assert.Equal(10, negev.MemberLocations.Count);
    }

    // Data only, under exact gdd-0.3 only: a new game initializes nothing, and the old identities never read the file.
    [Fact]
    public void HostsAreDataOnlyAndOldIdentitiesNeverReadThem()
    {
        var c = PlayerRegionsContentTests.Load(Real);
        var state = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(state.PlayerRegions);
        Assert.False(state.PopulationLedger.IsInitialized);
        var reads = new List<string>();
        var old = PlayerRegionsContentTests.Load(Real, modern: false, onRead: reads.Add);
        Assert.Equal(SaveGame.GddRuleset, old.Ruleset);
        Assert.DoesNotContain(PlayerRegionCatalogue.FileName, reads);
        Assert.Empty(old.PlayerRegions.Regions);
    }

    [Theory]
    // A settlement hosts only for F19 and F20 ("These two only (Recommended)"), even a member of the field's own region.
    [InlineData("field: F01, host: rosh_pinna", "field: F01, host: hanita", "player_regions[0].field_hexes[0].host", "F01: a settlement may host only for F19 and F20, not hanita")]
    [InlineData("field: F18, host: rishon_lezion", "field: F18, host: hulda", "player_regions[4].field_hexes[1].host", "F18: a settlement may host only for F19 and F20, not hulda")]
    // A census city hosts only in its field's own region.
    [InlineData("field: F03, host: tiberias", "field: F03, host: safed", "player_regions[1].field_hexes[0].host", "F03: census city safed is outside this field's region valleys")]
    [InlineData("field: F15, host: petah_tikva", "field: F15, host: rehovot", "player_regions[3].field_hexes[0].host", "F15: census city rehovot is outside this field's region tel_aviv_jaffa")]
    [InlineData("field: F19, host: nir_am", "field: F19, host: haifa", "player_regions[7].field_hexes[0].host", "F19: census city haifa is outside this field's region southern_coast")]
    // F19 and F20 keep the same-region rule (Decision A, option 1).
    [InlineData("field: F19, host: nir_am", "field: F19, host: nirim", "player_regions[7].field_hexes[0].host", "F19: nirim is not a member location of southern_coast")]
    [InlineData("field: F20, host: nirim", "field: F20, host: nir_am", "player_regions[8].field_hexes[0].host", "F20: nir_am is not a member location of negev")]
    // The label is the field's place in the file: the permission above must not move with a typed label.
    [InlineData("field: F05, host: tiberias", "field: F19, host: tiberias", "player_regions[1].field_hexes[2].field", "must be F05, this field's place in the file's order")]
    [InlineData("host_basis: design_choice, host_source: 'owner 7 Nov 2026 on #1689: \"Haifa, Haifa, Rehovot (Recommended)\", F13", "host_basis: guess, host_source: 'owner 7 Nov 2026 on #1689: \"Haifa, Haifa, Rehovot (Recommended)\", F13", "player_regions[2].field_hexes[4].host_basis", "F13: unknown basis guess")]
    public void TheLoaderRefusesABadHostNamingTheFileAndField(string from, string to, string field, string reason)
    {
        Assert.Contains(from, Real);
        var e = Assert.Throws<ContentException>(() => PlayerRegionsContentTests.Load(Real.Replace(from, to)));
        Assert.Equal("regions.yaml", e.File);
        Assert.Equal(field, e.Field);
        Assert.Contains(reason, e.Message);
    }

    [Fact]
    public void AHostNeedsItsProvenance()
    {
        const string From = "field: F02, host: rosh_pinna, host_basis: inference, host_source: '";
        var row = Real.Split('\n').Single(l => l.Contains(From));
        var bare = row.Substring(0, row.IndexOf(", host_source: '", StringComparison.Ordinal)) + " }";
        var e = Assert.Throws<ContentException>(() => PlayerRegionsContentTests.Load(Real.Replace(row, bare)));
        Assert.Equal("regions.yaml", e.File);
        Assert.Equal("player_regions[0].field_hexes[1].host_source", e.Field);
        Assert.Contains("F02: explicit provenance required", e.Message);
    }

    [Fact]
    public void AFieldMayHaveNoHostInASyntheticRegion()
    {
        var c = PlayerRegionsContentTests.Load("player_regions:\n" + PlayerRegionsContentTests.RamlaRehovot
            .Replace("    member_locations:", "    field_hexes: [{ q: -4, r: 24 }]\n    member_locations:"));
        Assert.Empty(Assert.Single(c.PlayerRegions.Regions).Value.FieldHosts);
    }
}
