using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD slice's city data (Phase 1 PR 3, Task 3.1): cities.yaml, unit_types.yaml and the mvp: rules block. The
/// loader reads and requires them only when systems.loop is gdd; their values are placeholders in MvpSlice.Overlay or test
/// fixtures, never under content/ (controller ruling 2).</summary>
public class CitiesContentTests
{
    static Content Load(Dictionary<string, string>? extra = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));

    [Fact]
    public void TheGddFixtureLoadsItsCityTypesAndRules()
    {
        var c = Load();
        var city = Assert.Single(c.Cities);
        Assert.Equal(("town_a", 12, 8, 10, 6), (city.Id, city.HousingCapacity, city.JobSlots, city.Civilians, city.Employed));
        Assert.Equal(new[] { "civilian", "refugee" }, c.UnitTypes.Select(t => t.Id));
        var mvp = c.RulesAt(c.Eras[0].From).Mvp!;
        Assert.Equal((10, 1.2, 0.5, 0.5, "town_a"), (mvp.PeoplePerUnit, mvp.IncomePerJobPerYear, mvp.CohesionHousingWeight, mvp.CohesionJobsWeight, mvp.ArrivalsCity));
        Assert.Equal("Town A", c.SettlementNames["town_a"]);
    }

    [Fact]
    public void TheLegacyLoopReadsNoCityData()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Empty(c.Cities);
        Assert.Empty(c.UnitTypes);
        Assert.Null(c.RulesAt(c.Eras[0].From).Mvp);
        Assert.Equal("Tel Aviv", c.SettlementNames["tel_aviv"]);   // names are read in both loops: refusals and causes name places
    }

    [Fact]
    public void TheSliceHasTelAvivFromTheOverlay()
    {
        var c = MvpSlice.Load(RealContentTests.ContentRoot());
        Assert.Equal("tel_aviv", Assert.Single(c.Cities).Id);
        Assert.Equal("tel_aviv", c.RulesAt(c.Eras[0].From).Mvp!.ArrivalsCity);
        Assert.True(c.Rules.AttentionCost.ContainsKey("assign_workers"));
    }

    [Fact]
    public void NoBalanceValueIsUnderContent()
    {
        var root = RealContentTests.ContentRoot();
        Assert.False(File.Exists(Path.Combine(root, "cities.yaml")));
        Assert.False(File.Exists(Path.Combine(root, "unit_types.yaml")));
        var overrides = Directory.Exists(Path.Combine(root, "rules")) ? Directory.GetFiles(Path.Combine(root, "rules"), "*.yaml") : new string[0];
        foreach (var f in new[] { "rules.yaml" }.Concat(overrides.Select(x => "rules/" + Path.GetFileName(x))))
        {
            var text = File.ReadAllText(Path.Combine(root, f));
            Assert.DoesNotContain("mvp:", text);
            Assert.DoesNotContain("assign_workers", text);
        }
    }

    const string City = "  - { id: town_a, housing_capacity: 12, job_slots: 8, civilians: 10, employed: 6, facilities: [] }\n";

    static string Cities(string row) => "cities:\n" + row;
    static string Rules(string mvp) => Fixture.Files["rules.yaml"].Replace("map_action: 1 }", "map_action: 1, assign_workers: 1 }") + mvp;

    public static IEnumerable<object[]> BadRows() => new List<object[]>
    {
        new object[] { "cities.yaml", Cities(City + City), "cities[town_a].id", "duplicate id" },
        new object[] { "cities.yaml", Cities(City.Replace("town_a", "nowhere")), "cities[nowhere].id", "is not in map/settlements_1948.yaml" },
        new object[] { "cities.yaml", Cities(City.Replace("civilians: 10", "civilians: -1")), "cities[town_a].civilians", "must be >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("housing_capacity: 12", "housing_capacity: -1")), "cities[town_a].housing_capacity", "must be >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("job_slots: 8", "job_slots: -1")), "cities[town_a].job_slots", "must be >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("employed: 6", "employed: -1")), "cities[town_a].employed", "must be >= 0" },
        new object[] { "cities.yaml", Cities(City.Replace("employed: 6", "employed: 11").Replace("job_slots: 8", "job_slots: 20")), "cities[town_a].employed", "cannot be more than civilians" },
        new object[] { "cities.yaml", Cities(City.Replace("employed: 6", "employed: 9")), "cities[town_a].employed", "cannot be more than job_slots" },
        new object[] { "cities.yaml", Cities(City.Replace("facilities: []", "facilities: [winery]")), "cities[town_a].facilities", "unknown facility 'winery'" },
        new object[] { "cities.yaml", "cities: []\n", "mvp.arrivals_city", "is not a city in cities.yaml" },
        new object[] { "unit_types.yaml", "types:\n  - { id: civilian, kind: population }\n  - { id: civilian, kind: population }\n", "types[civilian].id", "duplicate id" },
        new object[] { "unit_types.yaml", "types:\n  - { id: civilian, kind: crowd }\n", "types[civilian].kind", "must be population or unit" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("people_per_unit: 10", "people_per_unit: 0")), "mvp.people_per_unit", "must be >= 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("income_per_job_per_year: 1.2", "income_per_job_per_year: -1")), "mvp.income_per_job_per_year", "must be a finite number >= 0" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("cohesion_housing_weight: 0.5", "cohesion_housing_weight: 1.5")), "mvp.cohesion_housing_weight", "must be a number from 0 to 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("cohesion_jobs_weight: 0.5", "cohesion_jobs_weight: 0.4")), "mvp.cohesion_jobs_weight", "the two cohesion weights must sum to 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("arrivals_city: town_a", "arrivals_city: nowhere")), "mvp.arrivals_city", "is not a city in cities.yaml" },
        new object[] { "rules.yaml", Rules(""), "mvp", "required when eras.yaml systems.loop is gdd" },
        new object[] { "rules.yaml", Fixture.Files["rules.yaml"] + Fixture.GddRules, "attention_cost.assign_workers", "missing cost" },
    };

    [Theory]
    [MemberData(nameof(BadRows))]
    public void ABadValueFailsNamingTheFileAndField(string file, string text, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { [file] = text }));
        Assert.Equal(field == "mvp.arrivals_city" ? "rules.yaml" : file, ex.File);
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

    /// <summary>Arrivals are counted from the game's start in units of one size (R-CITY-003), so an era may not change it.</summary>
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
