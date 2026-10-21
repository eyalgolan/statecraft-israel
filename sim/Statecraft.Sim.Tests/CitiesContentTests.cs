using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;

/// <summary>The GDD slice's city data (Phase 1 PR 3, Task 3.1): cities.yaml, unit_types.yaml and the mvp: rules block. The
/// loader reads and requires them only when systems.loop is gdd. The real values are labelled design values in content/
/// (Q-CUT-1, R-LOOP-005, DesignValuesTests); the fixtures' are test values.</summary>
public class CitiesContentTests
{
    static Content Load(Dictionary<string, string>? extra = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));

    [Fact]
    public void TheGddFixtureLoadsItsCityTypesAndRules()
    {
        var c = Load();
        var city = Assert.Single(c.Cities);
        Assert.Equal(("town_a", 12, 8, 10, 6), (city.Id, city.HousingCapacity, city.JobSlots, city.Civilians, city.Employed));
        Assert.Equal(new[] { "civilian", "refugee", "recruit", "halutz", "craftsman" }, c.UnitTypes.Select(t => t.Id));
        var halutz = c.UnitTypes.Single(t => t.Id == "halutz");
        Assert.Equal(("unit", "hakhshara", 3, 0.25), (halutz.Kind, halutz.Facility, halutz.TrainingTurns, halutz.TrainingCost));
        Assert.Equal("", c.UnitTypes.Single(t => t.Id == "civilian").Facility);
        var mvp = c.RulesAt(c.Eras[0].From).Mvp!;
        Assert.Equal((10, 1.2, new DateTime(1948, 5, 15)), (mvp.PeoplePerUnit, mvp.IncomePerJobPerYear, mvp.CityArrivalsFrom));
        Assert.Equal("Town A", c.SettlementNames["town_a"]);
    }

    [Fact]
    public void TheLegacyLoopReadsNoCityData()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>   // a legacy fixture with city files next to it
            { ["cities.yaml"] = Fixture.GddCities, ["unit_types.yaml"] = Fixture.GddUnitTypes }));
        Assert.Empty(c.Cities);
        Assert.Empty(c.UnitTypes);
        Assert.Null(c.RulesAt(c.Eras[0].From).Mvp);
        Assert.Equal("Town A", c.SettlementNames["town_a"]);   // names are read in both loops: refusals and causes name places
    }

    [Fact]
    public void TheRealGameStartsTheTrainingInTelAviv()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(new DateTime(1948, 11, 8), c.RulesAt(c.Eras[0].From).Mvp!.CityArrivalsFrom);   // the census day (Q-ARRV-3)
        Assert.True(c.Rules.AttentionCost.ContainsKey("assign_workers"));
        Assert.Equal(new[] { "tel_aviv" }, c.Cities.Where(x => x.Facilities.Count > 0).Select(x => x.Id));
        Assert.Equal(new[] { "military_camp", "hakhshara" }, c.Cities.Single(x => x.Id == "tel_aviv").Facilities);   // D-D5: one of each at the start
        Assert.Equal(new[] { ("recruit", "military_camp"), ("halutz", "hakhshara"), ("craftsman", "industry") },
                     c.UnitTypes.Where(t => t.Facility != "").Select(t => (t.Id, t.Facility)));
        Assert.True(c.Rules.AttentionCost.ContainsKey("train") && c.Rules.AttentionCost.ContainsKey("cancel_training"));
    }

    /// <summary>The MVP's cities are the Jewish `city` and `town` rows of the 1948 settlements (the Phase 3 plan, PR 1; the six
    /// kibbutzim and moshavim are not cities, Q-START-4, owner, 15 October 2026).</summary>
    // R-CITY-006
    [Fact]
    public void TheTwelveCitiesAreTheCityAndTownRows()
    {
        var root = RealContentTests.ContentRoot();
        var yaml = new YamlStream();
        using (var r = new StringReader(File.ReadAllText(Path.Combine(root, "map", "settlements_1948.yaml")))) yaml.Load(r);
        var rows = (YamlSequenceNode)((YamlMappingNode)yaml.Documents[0].RootNode)[new YamlScalarNode("settlements")];
        var towns = rows.Cast<YamlMappingNode>()
            .Where(m => ((YamlScalarNode)m[new YamlScalarNode("type")]).Value is "city" or "town")
            .Select(m => ((YamlScalarNode)m[new YamlScalarNode("id")]).Value!).ToList();
        Assert.Equal(12, towns.Count);
        Assert.Equal(towns, ContentLoader.Load(root).Cities.Select(x => x.Id));   // in the settlements file's order
    }

    /// <summary>Each city's census start (the Phase 3 plan, PR 1, from #569) and its units at people_per_unit 1,000: people
    /// to the nearest unit with at least 1 for a city with people (Q-START-2: Rosh Pinna's 350 is 1), employed to the nearest
    /// unit of people times the working share (Q-START-1: the city's CBS share, or the national 43.9% where none exists).</summary>
    public static IEnumerable<object[]> TheCensus() => new List<object[]>
    {
        new object[] { "jerusalem", 83984, "sourced", 0.342, "sourced", 84, 29 },
        new object[] { "tel_aviv", 248459, "sourced", 0.426, "sourced", 248, 106 },
        new object[] { "haifa", 98618, "sourced", 0.429, "sourced", 99, 42 },
        new object[] { "tiberias", 5555, "sourced", 0.334, "sourced", 6, 2 },
        new object[] { "safed", 2317, "sourced", 0.345, "sourced", 2, 1 },
        new object[] { "petah_tikva", 21879, "sourced", 0.439, "design", 22, 10 },
        new object[] { "rishon_lezion", 10433, "sourced", 0.439, "design", 10, 5 },
        new object[] { "zikhron_yaakov", 1935, "sourced", 0.439, "design", 2, 1 },
        new object[] { "rosh_pinna", 350, "wikipedia-only", 0.439, "design", 1, 0 },
        new object[] { "rehovot", 12522, "sourced", 0.439, "design", 13, 5 },
        new object[] { "hadera", 11811, "sourced", 0.439, "design", 12, 5 },
        new object[] { "netanya", 11589, "sourced", 0.439, "design", 12, 5 },
    };

    // R-CITY-007
    [Theory]
    [MemberData(nameof(TheCensus))]
    public void TheCensusValuesConvertAsStated(string id, int people, string peopleStatus, double share, string shareStatus, int civilians, int employed)
    {
        var x = ContentLoader.Load(RealContentTests.ContentRoot()).Cities.Single(k => k.Id == id);
        Assert.Equal((people, peopleStatus), ((int)x.People!.Value, x.People.Status));
        Assert.Equal(((decimal)share, shareStatus), (x.EmployedShare!.Value, x.EmployedShare.Status));
        Assert.Equal((civilians, employed), (x.Civilians, x.Employed));
        // Q-START-1's gaps, labelled design values: housing for every resident and a job slot for every worker.
        Assert.Equal((civilians, employed), (x.HousingCapacity, x.JobSlots));
        Assert.Equal(("design", "design"), (x.Housing!.Status, x.Jobs!.Status));
        if (peopleStatus != "design") Assert.Equal(id == "rosh_pinna" ? "1948-05" : "1948-11-08", x.People.Date);
    }

    // R-CITY-007
    [Fact]
    public void TheTwelveCitiesStartWith511Units() =>   // 510 to the nearest unit, and Rosh Pinna's 1 (Q-START-2)
        Assert.Equal(511, ContentLoader.Load(RealContentTests.ContentRoot()).Cities.Sum(x => x.Civilians));

    /// <summary>The conversion's edges on the fixture (people_per_unit 10): half a unit rounds up, and a city with any people
    /// has at least 1 unit; one with none has 0.</summary>
    // R-CITY-007
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(14, 1)]
    [InlineData(15, 2)]
    [InlineData(100, 10)]
    public void PeopleRoundToTheNearestUnitWithAtLeastOne(int people, int units)
    {
        var c = Load(new Dictionary<string, string> { ["cities.yaml"] = People(FormattableString.Invariant($"people: {{ value: {people}, status: design }}")).Replace("value: 0.6", "value: 0").Replace("job_slots: { value: 8", "job_slots: { value: 0") });
        Assert.Equal(units, Assert.Single(c.Cities).Civilians);
    }

    /// <summary>A historical value names its status, its sources with their checked dates, and the date it describes; a design
    /// value names its status (the Phase 3 plan, review focus 3). Each refusal names the file and the field.</summary>
    // R-CITY-006
    [Theory]
    [InlineData("people: { value: 100 }", "cities[town_a].people.status", "required")]
    [InlineData("people: { value: 100, status: guessed }", "cities[town_a].people.status", "unknown status 'guessed' (sourced, derived, wikipedia-only or design)")]
    [InlineData("people: { value: 100, status: sourced, date: 1948-11-08 }", "cities[town_a].people.sources", "required: a sourced value needs a source")]
    [InlineData("people: { value: 100, status: derived, date: 1948-11-08 }", "cities[town_a].people.sources", "required: a derived value needs a source")]
    [InlineData("people: { value: 100, status: wikipedia-only, date: 1948-05 }", "cities[town_a].people.sources", "required: a wikipedia-only value needs a source")]
    [InlineData("people: { value: 100, status: sourced, date: 1948-11-08, sources: [{ url: \"https://example.org/census\" }] }", "cities[town_a].people.sources[0]", "needs a url and checked date")]
    [InlineData("people: { value: 100, status: sourced, date: 1948-11-08, sources: [{ checked: 2026-10-15 }] }", "cities[town_a].people.sources[0]", "needs a url and checked date")]
    [InlineData("people: { value: 100, status: sourced, sources: [{ url: \"https://example.org/census\", checked: 2026-10-15 }] }", "cities[town_a].people.date", "required: the day or month the value describes")]
    [InlineData("people: { value: 100, status: sourced, date: 8 November 1948, sources: [{ url: \"https://example.org/census\", checked: 2026-10-15 }] }", "cities[town_a].people.date", "must be YYYY-MM-DD or YYYY-MM")]
    [InlineData("people: { value: 100, status: sourced, date: 1948-02-30, sources: [{ url: \"https://example.org/census\", checked: 2026-10-15 }] }", "cities[town_a].people.date", "must be YYYY-MM-DD or YYYY-MM")]
    public void EveryFieldHasAStatus(string people, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["cities.yaml"] = People(people) }));
        Assert.Equal("cities.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    /// <summary>Every field of a row is required, so a missing one is never read as 0.</summary>
    // R-CITY-006
    [Theory]
    [InlineData("people")]
    [InlineData("employed_share")]
    [InlineData("housing_capacity")]
    [InlineData("job_slots")]
    public void AMissingFieldIsALoadError(string key)
    {
        var row = string.Join("\n", City.Split('\n').Where(l => !l.TrimStart().StartsWith(key + ":", StringComparison.Ordinal)));
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["cities.yaml"] = Cities(row) }));
        Assert.Equal(("cities.yaml", $"cities[town_a].{key}"), (ex.File, ex.Field));
        Assert.Contains("required", ex.Message);
    }

    // R-CITY-006
    [Theory]
    [InlineData(Census)]
    [InlineData("people: { value: 100, status: wikipedia-only, date: 1948-05, sources: [{ url: \"https://example.org/wiki\", checked: 2026-10-15 }] }")]
    [InlineData("people: { value: 100, status: design, sources: [{ url: \"https://example.org/method\", checked: 2026-10-15 }] }")]   // a design value may name what its method rests on
    public void AValueWithItsStatusLoads(string people) =>
        Assert.Equal(10, Assert.Single(Load(new Dictionary<string, string> { ["cities.yaml"] = People(people) }).Cities).Civilians);

    const string City = "  - id: town_a\n    people: { value: 100, status: design }\n    employed_share: { value: 0.6, status: design }\n"
                      + "    housing_capacity: { value: 12, status: design }\n    job_slots: { value: 8, status: design }\n    facilities: []\n";
    const string Design = "people: { value: 100, status: design }";
    const string Census = "people: { value: 100, status: sourced, date: 1948-11-08, sources: [{ url: \"https://example.org/census\", checked: 2026-10-15 }] }";

    static string Cities(string row) => "cities:\n" + row;
    static string Rules(string mvp) => Fixture.Files["rules.yaml"].Replace("map_action: 1 }", Fixture.GddCosts) + mvp;
    static string Types(string row) => "types:\n  - { id: civilian, kind: population }\n" + row;
    static string People(string value) => Cities(City.Replace(Design, value));

    public static IEnumerable<object[]> BadRows() => new List<object[]>
    {
        new object[] { "cities.yaml", Cities(City + City), "cities[town_a].id", "duplicate id" },
        new object[] { "cities.yaml", Cities(City.Replace("town_a", "nowhere")), "cities[nowhere].id", "is not in map/settlements_1948.yaml" },
        new object[] { "cities.yaml", People("people: { value: -1, status: design }"), "cities[town_a].people.value", "must be a whole number >= 0" },
        new object[] { "cities.yaml", People("people: { value: 100.5, status: design }"), "cities[town_a].people.value", "must be a whole number >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("housing_capacity: { value: 12", "housing_capacity: { value: -1")), "cities[town_a].housing_capacity.value", "must be a whole number >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("job_slots: { value: 8", "job_slots: { value: -1")), "cities[town_a].job_slots.value", "must be a whole number >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("employed_share: { value: 0.6", "employed_share: { value: 1.2")), "cities[town_a].employed_share.value", "must be a number from 0 to 1" },
        new object[] { "cities.yaml", Cities(City.Replace("employed_share: { value: 0.6", "employed_share: { value: 0.9")), "cities[town_a].job_slots.value", "fewer than the 9 employed units" },
        new object[] { "cities.yaml", Cities(City.Replace("facilities: []", "facilities: [winery]")), "cities[town_a].facilities", "unknown facility 'winery'" },
        new object[] { "cities.yaml", "cities: []\n", "cities", "needs at least one city to take the arrivals" },
        new object[] { "unit_types.yaml", "types:\n  - { id: civilian, kind: population }\n  - { id: civilian, kind: population }\n", "types[civilian].id", "duplicate id" },
        new object[] { "unit_types.yaml", "types:\n  - { id: civilian, kind: crowd }\n", "types[civilian].kind", "must be population or unit" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("people_per_unit: 10", "people_per_unit: 0")), "mvp.people_per_unit", "must be >= 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("income_per_job_per_year: 1.2", "income_per_job_per_year: -1")), "mvp.income_per_job_per_year", "must be a finite number >= 0" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("craftsman_income_multiple: 2", "craftsman_income_multiple: 1")), "mvp.craftsman_income_multiple", "must be a finite number > 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("  craftsman_income_multiple: 2\n", "")), "mvp.craftsman_income_multiple", "must be a finite number > 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("  city_arrivals_from: 1948-05-15\n", "")), "mvp.city_arrivals_from", "required when eras.yaml systems.loop is gdd" },
        new object[] { "rules.yaml", Rules(""), "mvp", "required when eras.yaml systems.loop is gdd" },
        new object[] { "rules.yaml", Fixture.Files["rules.yaml"] + Fixture.GddRules, "attention_cost.assign_workers", "missing cost" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules).Replace(", train: 1", ""), "attention_cost.train", "missing cost" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules).Replace(", cancel_training: 1", ""), "attention_cost.cancel_training", "missing cost" },
        new object[] { "unit_types.yaml", Types("  - { id: halutz, kind: unit, facility: winery, training_turns: 3, training_cost: 0.25 }\n"), "types[halutz].facility", "unknown facility 'winery'" },
        new object[] { "unit_types.yaml", Types("  - { id: halutz, kind: unit, facility: hakhshara, training_turns: 0, training_cost: 0.25 }\n"), "types[halutz].training_turns", "must be >= 1" },
        new object[] { "unit_types.yaml", Types("  - { id: halutz, kind: unit, facility: hakhshara, training_turns: 3, training_cost: -1 }\n"), "types[halutz].training_cost", "must be a finite number >= 0" },
        new object[] { "unit_types.yaml", Types("  - { id: halutz, kind: unit, training_turns: 3 }\n"), "types[halutz].training_turns", "only a type with a facility trains" },
        new object[] { "unit_types.yaml", Types("  - { id: halutz, kind: unit, training_cost: 0.5 }\n"), "types[halutz].training_cost", "only a type with a facility trains" },
        new object[] { "unit_types.yaml", Types("  - { id: craftsman, kind: population, facility: hakhshara, training_turns: 3, training_cost: 0.5 }\n"), "types[craftsman].facility", "a craftsman trains at industry" },
        new object[] { "unit_types.yaml", Types("  - { id: settler, kind: population, facility: hakhshara, training_turns: 3, training_cost: 0.5 }\n"), "types[settler].kind", "only recruit and craftsman are trained counts" },
    };

    [Theory]
    [MemberData(nameof(BadRows))]
    public void ABadValueFailsNamingTheFileAndField(string file, string text, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { [file] = text }));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Theory]
    [InlineData("cities.yaml")]
    [InlineData("unit_types.yaml")]
    public void TheGddLoopNeedsTheCityFiles(string file)
    {
        var o = Fixture.Gdd();
        o.Remove(file);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(o)));
        Assert.Equal(file, ex.File);
        Assert.Contains("missing file", ex.Message);
    }

    /// <summary>The cities count arrivals from one day in units of one size (R-CITY-003), so an era may change neither.</summary>
    [Fact]
    public void PeoplePerUnitIsTheSameInEveryEra()
    {
        var o = Fixture.Gdd(new Dictionary<string, string> { ["eras.yaml"] = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1950-01-01
    systems: { loop: gdd }
  - id: later
    name: Later
    from: 1950-01-01
    to: 1957-01-01
    systems: { loop: gdd }
" });
        var dir = Fixture.Write(o);
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "mvp: { income_per_job_per_year: 2.4 }\n");
        Assert.Equal(2.4, ContentLoader.Load(dir).EraRules["later"].Mvp!.IncomePerJobPerYear);   // other mvp values may change by era
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "mvp: { people_per_unit: 20 }\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Equal("rules/later.yaml", ex.File);
        Assert.Equal("mvp.people_per_unit", ex.Field);
        Assert.Contains("must be the same in every era", ex.Message);
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "mvp: { city_arrivals_from: 1950-01-01 }\n");
        ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Equal(("rules/later.yaml", "mvp.city_arrivals_from"), (ex.File, ex.Field));
        Assert.Contains("must be the same in every era", ex.Message);
    }

    [Fact]
    public void ASettlementWithoutANameIsALoadError()
    {
        var bad = Fixture.Files["map/settlements_1948.yaml"].Replace("name: Town A, ", "");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlements_1948.yaml"] = bad })));
        Assert.Equal("map/settlements_1948.yaml", ex.File);
        Assert.Equal("settlements[0].name", ex.Field);
    }
}
