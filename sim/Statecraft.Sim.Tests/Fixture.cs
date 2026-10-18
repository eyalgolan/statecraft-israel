using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("FixtureCleanupFramework", "Statecraft.Sim.Tests")]

/// <summary>Writes a small, valid content set to a temp dir. Tests override single files to test errors.</summary>
public static class Fixture
{
    public static readonly Dictionary<string, string> Files = new Dictionary<string, string>
    {
        ["offices.yaml"] = @"
offices:
  - id: prime_minister
    title: Prime Minister
  - id: defense_minister
    title: Minister of Defense
  - id: finance_minister
    title: Minister of Finance
",
        ["eras.yaml"] = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1957-01-01
",
        ["parties.yaml"] = @"
parties:
  - id: alpha
    name: Alpha
  - id: beta
    name: Beta
",
        ["people.yaml"] = @"
people:
  - id: pm_one
    name: Pm One
    living: false
    competence: 8
    loyalty: 9
    offices:
      - office: prime_minister
        from: 1948-05-14
      - office: defense_minister
        from: 1948-05-14
        to: 1949-01-01
  - id: def_two
    name: Def Two
    living: false
    competence: 4
    loyalty: 5
    offices:
      - office: defense_minister
        from: 1949-01-01
      - office: finance_minister
        from: 1948-05-14
",
        ["legislatures.yaml"] = @"
legislatures:
  - id: council
    body: Council
    from: 1948-05-14
    to: 1949-02-14
    size: 5
    budget_approval: false
    seats: { alpha: 3, beta: 2 }
  - id: house
    body: House
    from: 1949-02-14
    size: 10
    budget_approval: true
    seats: { alpha: 4, beta: 6 }
",
        ["governments.yaml"] = @"
governments:
  - id: gov1
    name: Government One
    from: 1948-05-14
    to: 1949-03-10
    coalition: [alpha]
  - id: gov2
    name: Government Two
    from: 1949-03-10
    coalition: [alpha, beta]
",
        ["model.yaml"] = @"
indicators:
  - id: morale
    base: 0.5
    name: Morale
  - id: food_supply
    base: 0.4
    name: Food supply
  - id: housing
    base: 0.3
    name: Housing
policies:
  - id: rationing
    default: 0.0
    office: finance_minister
    name: Rationing
  - id: maabarot
    default: 0.0
    office: finance_minister
    name: Transit camps
effects:
  - id: rationing_food
    source: rationing
    target: food_supply
    k: 0.4
    inertia: 2
    office: finance_minister
    name: Rations feed people
  - id: rationing_morale
    source: rationing
    target: morale
    k: -0.2
    inertia: 1
    name: Rationing wears on morale
situations:
  - id: hunger
    indicator: food_supply
    on_below: 0.3
    off_above: 0.45
    name: Hunger
money:
  - { id: treasury, name: Treasury, prefix: 'IL ' }
  - { id: debt, name: Debt, prefix: 'IL ' }
  - { id: fx_reserves, name: Foreign currency, prefix: '$' }
voter_groups:
  - id: everyone
    weights: { morale: 1.0 }
actors:
  - id: usa
    name: United States
    standing: 0.5
",
        ["rules.yaml"] = @"
start: 1948-05-15
start_rung: 5
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 1, map_action: 1 }
appoint_stability_cost_other_party: 0.1
appoint_stability_cost_same_party: 0.03
map_actions_from: 1948-06-01
map_action_max_distance: 1
map_actions_per_turn: 2
map_actions:
  transit_camp: { cost: 2, housing: 0.05, morale: -0.02, name: a transit camp }
  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project }
rung_attention_step: 0.25
ceasefire_weeks: 4
rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
treasury_start: 100
fx_start: 50
tax_per_capita_per_year: 0.0004
spending_per_capita_per_year: 0.0005
exports_per_year: 20
imports_per_capita_per_year: 0.0001
mobilization_cost_per_year: 40
natural_increase_per_year: 0.017
initial_population: { veterans: 1000 }
budget_start: { finance_minister: 1.0 }
",
        ["population.yaml"] = @"
anchors: []
",
        // A 5 x 2 map: one sea hex, the rest land; one settlement at (1, 0).
        ["map/hexes.json"] = @"{""attribution"":[""test""],""bounds"":{""lon0"":34.2,""lon1"":35.9,""lat0"":29.45,""lat1"":33.35},""hex_size_km"":5.0,""layout"":""pointy"",""hexes"":[
{""q"":0,""r"":0,""lon"":34.2,""lat"":33.35,""elev_m"":0.0,""land"":false,""biome"":""sea""},
{""q"":1,""r"":0,""lon"":34.29,""lat"":33.35,""elev_m"":20.0,""land"":true,""biome"":""coastal_plain""},
{""q"":2,""r"":0,""lon"":34.38,""lat"":33.35,""elev_m"":250.0,""land"":true,""biome"":""hills""},
{""q"":3,""r"":0,""lon"":34.47,""lat"":33.35,""elev_m"":300.0,""land"":true,""biome"":""hills""},
{""q"":4,""r"":0,""lon"":34.56,""lat"":33.35,""elev_m"":100.0,""land"":true,""biome"":""desert""},
{""q"":0,""r"":1,""lon"":34.24,""lat"":33.28,""elev_m"":10.0,""land"":true,""biome"":""coastal_plain""},
{""q"":1,""r"":1,""lon"":34.33,""lat"":33.28,""elev_m"":40.0,""land"":true,""biome"":""coastal_plain""},
{""q"":2,""r"":1,""lon"":34.42,""lat"":33.28,""elev_m"":260.0,""land"":true,""biome"":""hills""},
{""q"":3,""r"":1,""lon"":34.51,""lat"":33.28,""elev_m"":120.0,""land"":true,""biome"":""desert""}]}
",
        ["map/settlement_hexes.yaml"] = @"
settlements:
  - { id: town_a, q: 1, r: 0 }
",
        ["map/settlements_1948.yaml"] = @"
settlements:
  - { id: town_a, name: Town A, lon: 34.29, lat: 33.35, type: town, founded: 1900, pre_modern: false }
",
        ["aliyah.yaml"] = @"
regions:
  - { id: europe, name: Europe }
  - { id: asia, name: Asia }
series:
  - from: 1948-05-15
    to: 1948-12-31
    origins: { europe: 230, asia: 0 }
",
    };

    /// <summary>The fixture with the GDD loop on (eras.yaml systems.loop: gdd) and the files and keys that loop needs: the
    /// mvp: rules block, the GDD command kinds' attention_cost, cities.yaml and unit_types.yaml. Every value is a fixture
    /// value, not the game's (the real design values are in content/, R-LOOP-005). <paramref name="extra"/> replaces or adds files on top.</summary>
    public static Dictionary<string, string> Gdd(Dictionary<string, string>? extra = null)
    {
        var o = new Dictionary<string, string>
        {
            ["eras.yaml"] = Files["eras.yaml"] + "    systems: { loop: gdd }\n",
            ["rules.yaml"] = Files["rules.yaml"].Replace("map_action: 1 }", GddCosts) + GddRules,
            ["cities.yaml"] = GddCities,
            ["unit_types.yaml"] = GddUnitTypes,
            ["sites.yaml"] = GddSites,
            ["projects.yaml"] = GddProjects,
        };
        if (extra != null) foreach (var kv in extra) o[kv.Key] = kv.Value;
        return o;
    }

    /// <summary>The fixture's attention_cost line with the GDD command kinds added (replaces "map_action: 1 }").</summary>
    public const string GddCosts = "map_action: 1, assign_workers: 1, train: 1, cancel_training: 1, pioneer_work: 0, stop_work: 0, start_project: 1, cancel_project: 0.5 }";

    public const string GddRules = @"
mvp:
  people_per_unit: 10
  income_per_job_per_year: 1.2
  craftsman_income_multiple: 2
  city_upkeep_per_year: 0
  housing_upkeep_per_year: 0
  industry_upkeep_per_year: 0
  transit_camp_upkeep_per_year: 0
  military_camp_upkeep_per_year: 0
  hakhshara_upkeep_per_year: 0
  city_arrivals_from: 1948-05-15
  build_yishuv_turns: 3
  build_yishuv_cost: 1.5
  build_camp_turns: 2
  build_camp_cost: 1
  plow_fields_turns: 2
  plow_fields_cost: 0.5
  dry_swamp_turns: 3
  dry_swamp_cost: 2
  field_food_per_turn: 7
  objective_points: 100
  objective_bonus_per_month: 5
  objective_bonus_cap: 60
  k_food: 0.1
  cover_months: 3
  veteran_halutz_service: 4
  veteran_progress: 2
  housing_start_condition: 0.5
  overflow_condition_loss: 0.2
  defeat_turns: 3
  debt_ceiling: 10
  delegation_loyalty_floor: 5
  disloyal_effect_share: 0.5
";

    public static readonly string GddCities = "\ncities:\n" + City("town_a", 12, 8, 10, 6, "[military_camp, hakhshara]");

    /// <summary>One cities.yaml row in the fixture's units (people_per_unit 10): every field a design value (fixture values,
    /// not history), with the people and the employed share chosen so they convert to the given units (R-CITY-007).</summary>
    public static string City(string id, int housing, int jobs, int civilians, int employed, string facilities = "[]") =>
        FormattableString.Invariant($"  - {{ id: {id}, people: {{ value: {civilians * 10}, status: design }}, employed_share: {{ value: {(civilians == 0 ? 0m : (decimal)employed / civilians)}, status: design }}, housing_capacity: {{ value: {housing}, status: design }}, job_slots: {{ value: {jobs}, status: design }}, facilities: {facilities} }}\n");

    public const string GddUnitTypes = @"
types:
  - { id: civilian, kind: population }
  - { id: refugee, kind: population }
  - { id: recruit, kind: population, facility: military_camp, training_turns: 2, training_cost: 0.5 }
  - { id: halutz, kind: unit, facility: hakhshara, training_turns: 3, training_cost: 0.25 }
  - { id: craftsman, kind: population, facility: industry, training_turns: 3, training_cost: 0.75 }
";

    /// <summary>The three city project kinds (R-PROJ-001). Fixture values, not the game's.</summary>
    public const string GddProjects = @"
projects:
  - { id: housing, name: a housing project, cost: 3, turns: 2, capacity: 4, condition: 0.1, job_slots: 0 }
  - { id: transit_camp, name: a transit camp, cost: 1, turns: 1, capacity: 3, condition: -0.1, job_slots: 0 }
  - { id: industry, name: an industry project, cost: 2, turns: 2, capacity: 0, condition: 0, job_slots: 3 }
";

    /// <summary>A city's resources key with vineyards and a fixture source (R-PROJ-005), to add to a <see cref="City"/> row.</summary>
    public const string Vineyards = @"resources: [{ id: vineyards, sources: [{ url: ""https://example.org/vineyards"", checked: 2026-10-18 }], to_verify: a fixture note }]";

    /// <summary>A projects.yaml kind that requires vineyards (R-PROJ-005), to add to <see cref="GddProjects"/>. Fixture values.</summary>
    public const string Winery = "  - { id: winery, name: a winery, cost: 2, turns: 2, capacity: 0, condition: 0, job_slots: 6, requires: vineyards }\n";

    /// <summary>One designated site on a land hex next to town_a, with no settlement on it. Fixture values, not history.</summary>
    public const string GddSites = @"
sites:
  - id: site_a
    name: Site A
    name_he: אתר א
    q: 2
    r: 0
    kinds: [yishuv]
    founded: 1949-06-12
    sources:
      - { url: ""https://example.org/site_a"", checked: 2026-10-14 }
    status: fact-checked
";

    // Every Write goes under one root per test run, which FixtureCleanupFramework deletes when the run ends.
    // One folder per call (~1,150 per `dotnet test` run) used to stay behind (#1017).
    static readonly string Root = Path.Combine(Path.GetTempPath(), "statecraft-fixture-" + Guid.NewGuid().ToString("N"));

    public static void DeleteAll()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }

    public static string Write(Dictionary<string, string>? overrides = null, Dictionary<string, string>? events = null)
    {
        var dir = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "events"));
        Directory.CreateDirectory(Path.Combine(dir, "map"));
        foreach (var kv in Files)
            File.WriteAllText(Path.Combine(dir, kv.Key), overrides != null && overrides.TryGetValue(kv.Key, out var o) ? o : kv.Value);
        if (overrides != null)
            foreach (var kv in overrides)
                if (!Files.ContainsKey(kv.Key)) File.WriteAllText(Path.Combine(dir, kv.Key), kv.Value);   // a file only some content has (cities.yaml)
        if (events != null)
            foreach (var kv in events) File.WriteAllText(Path.Combine(dir, "events", kv.Key), kv.Value);
        return dir;
    }
}

// xunit 2 has no assembly teardown; the test framework is disposed once the run is over, while the
// test host still waits for it. ProcessExit is too late: the host is killed mid-delete (measured: 845 of
// 1,156 folders were still there). TestFramework.Dispose is not virtual, so IDisposable is re-implemented.
public sealed class FixtureCleanupFramework : XunitTestFramework, IDisposable
{
    public FixtureCleanupFramework(IMessageSink sink) : base(sink) { }

    void IDisposable.Dispose()
    {
        try { Fixture.DeleteAll(); }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }   // a file held open (antivirus on Windows) must not fail a green run
        base.Dispose();
    }
}
