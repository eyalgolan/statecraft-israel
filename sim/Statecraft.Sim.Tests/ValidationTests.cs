using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Load-time validation of malformed content: empty keys, enum-like fields, number ranges and ids.</summary>
public class ValidationTests
{
    internal const string Event = @"
id: drought
title: Drought
text: '{finance_minister} reports a drought.'
anchor: soft
tags: [economy]
office: finance_minister
from: 1948-06-01
to: 1948-09-01
conditions:
  - indicator: food_supply
    max: 0.9
options:
  - id: ration
    text: Ration water
    historical: true
    effects:
      - target: food_supply
        delta: 0.1
  - id: wait
    text: Wait for rain
    alternative: { basis: design }
status: draft
";

    /// <summary>Loads the fixture with one text replacement in <paramref name="file"/> (an events/ file when it
    /// starts with "events/") and returns the ContentException it must raise.</summary>
    internal static ContentException Bad(string file, string from, string to)
    {
        var overrides = new Dictionary<string, string>();
        var events = new Dictionary<string, string> { ["drought.yaml"] = Event };
        string original = file.StartsWith("events/") ? Event : Fixture.Files[file];
        Assert.Contains(from, original);
        var bad = original.Replace(from, to);
        if (file.StartsWith("events/")) events["drought.yaml"] = bad; else overrides[file] = bad;
        return Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(overrides, events)));
    }

    [Fact]
    public void FixtureWithTheTestEventLoads() =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event }));

    // I1: a key written with no value deserialises to null.
    [Theory]
    [InlineData("events/drought.yaml", "tags: [economy]", "tags:", "events[drought].tags", "must be a list")]
    [InlineData("events/drought.yaml", "options:\n  - id: ration\n    text: Ration water\n    historical: true\n    effects:\n      - target: food_supply\n        delta: 0.1\n  - id: wait\n    text: Wait for rain\n    alternative: { basis: design }\n", "options:\n", "events[drought].options", "must be a list")]
    [InlineData("events/drought.yaml", "    effects:\n      - target: food_supply\n        delta: 0.1\n", "    effects:\n", "events[drought].options[ration].effects", "must be a list")]
    [InlineData("people.yaml", "    offices:\n      - office: defense_minister\n        from: 1949-01-01\n      - office: finance_minister\n        from: 1948-05-14\n", "    offices:\n", "people[def_two].offices", "must be a list")]
    [InlineData("legislatures.yaml", "seats: { alpha: 4, beta: 6 }", "seats:", "legislatures[house].seats", "must be a map")]
    [InlineData("rules.yaml", "budget_start: { finance_minister: 1.0 }", "budget_start:", "budget_start", "must be a map")]
    [InlineData("parties.yaml", "parties:\n  - id: alpha\n    name: Alpha\n  - id: beta\n    name: Beta\n", "parties:\n", "parties", "must be a list")]
    public void EmptyListOrMapIsAContentError(string file, string from, string to, string field, string message)
    {
        var ex = Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.EndsWith(message, ex.Message);
    }

    // R3: with two null lists, the ordinally-first property is reported on every runtime.
    [Fact]
    public void FirstNullFieldReportedIsOrdinallyFirst()
    {
        var two = Event.Replace("tags: [economy]", "tags:").Replace(
            "options:\n  - id: ration\n    text: Ration water\n    historical: true\n    effects:\n      - target: food_supply\n        delta: 0.1\n  - id: wait\n    text: Wait for rain\n    alternative: { basis: design }\n", "options:\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = two })));
        Assert.Equal("events[drought].options", ex.Field);
    }

    // I2: enum-like fields.
    [Theory]
    [InlineData("events/drought.yaml", "anchor: soft", "anchor: hrad", "events[drought].anchor")]
    [InlineData("events/drought.yaml", "anchor: soft", "anchor: Hard", "events[drought].anchor")]
    [InlineData("events/drought.yaml", "status: draft", "status: done", "events[drought].status")]
    [InlineData("people.yaml", "    loyalty: 9\n", "    loyalty: 9\n    status: checked\n", "people[pm_one].status")]
    [InlineData("legislatures.yaml", "    size: 10\n", "    size: 10\n    status: final\n", "legislatures[house].status")]
    [InlineData("governments.yaml", "    coalition: [alpha]\n", "    coalition: [alpha]\n    status: Fact-Checked\n", "governments[gov1].status")]
    [InlineData("aliyah.yaml", "    origins: { europe: 230, asia: 0 }\n", "    origins: { europe: 230, asia: 0 }\n    status: ok\n", "series[0].status")]
    [InlineData("population.yaml", "anchors: []", "anchors:\n  - date: 1948-11-08\n    jews: 700\n    status: ''\n", "anchors[0].status")]
    public void EnumLikeFieldOutsideItsValuesIsAContentError(string file, string from, string to, string field)
    {
        var ex = Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Theory]
    [InlineData("hard")]
    [InlineData("soft")]
    public void BothAnchorsLoad(string anchor) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event.Replace("anchor: soft", "anchor: " + anchor) }));

    [Theory]
    [InlineData("draft")]
    [InlineData("fact-checked")]
    [InlineData("historian-reviewed")]
    [InlineData("sensitivity-reviewed")]
    public void EveryStatusLoads(string status) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event.Replace("status: draft", "status: " + status) }));

    // I3: numbers and dates out of range.
    [Theory]
    [InlineData("model.yaml", "base: 0.5", "base: .nan", "indicators[morale].base")]
    [InlineData("model.yaml", "base: 0.5", "base: 1.5", "indicators[morale].base")]
    [InlineData("model.yaml", "base: 0.5", "base: -0.1", "indicators[morale].base")]
    [InlineData("model.yaml", "default: 0.0", "default: 1.2", "policies[rationing].default")]
    [InlineData("model.yaml", "default: 0.0", "default: .nan", "policies[rationing].default")]
    [InlineData("model.yaml", "k: 0.4", "k: .inf", "effects[rationing_food].k")]
    [InlineData("model.yaml", "k: 0.4", "k: .nan", "effects[rationing_food].k")]
    [InlineData("model.yaml", "on_below: 0.3", "on_below: .nan", "situations[hunger].on_below")]
    [InlineData("model.yaml", "off_above: 0.45", "off_above: 1.45", "situations[hunger].off_above")]
    [InlineData("model.yaml", "weights: { morale: 1.0 }", "weights: { morale: .nan }", "voter_groups[everyone].weights.morale")]
    [InlineData("model.yaml", "standing: 0.5", "standing: 2", "actors[usa].standing")]
    [InlineData("model.yaml", "standing: 0.5", "standing: .nan", "actors[usa].standing")]
    [InlineData("rules.yaml", "[0.02, 0.05, 0.15, 0.4, 0.8]", "[0.02, 0.05, .nan, 0.4, 0.8]", "rung_min_mobilization[2]")]
    [InlineData("rules.yaml", "[0.02, 0.05, 0.15, 0.4, 0.8]", "[0.02, 0.05, 0.15, 0.4, 7]", "rung_min_mobilization[4]")]
    [InlineData("rules.yaml", "[0.02, 0.05, 0.15, 0.4, 0.8]", "[-0.02, 0.05, 0.15, 0.4, 0.8]", "rung_min_mobilization[0]")]
    [InlineData("rules.yaml", "[0.02, 0.05, 0.15, 0.4, 0.8]", "[0.02, 0.05, 0.15, 0.1, 0.8]", "rung_min_mobilization[3]")]
    [InlineData("rules.yaml", "treasury_start: 100", "treasury_start: .nan", "treasury_start")]
    [InlineData("rules.yaml", "fx_start: 50", "fx_start: .inf", "fx_start")]
    [InlineData("rules.yaml", "tax_per_capita_per_year: 0.0004", "tax_per_capita_per_year: .nan", "tax_per_capita_per_year")]
    [InlineData("rules.yaml", "spending_per_capita_per_year: 0.0005", "spending_per_capita_per_year: -.inf", "spending_per_capita_per_year")]
    [InlineData("rules.yaml", "exports_per_year: 20", "exports_per_year: .nan", "exports_per_year")]
    [InlineData("rules.yaml", "imports_per_capita_per_year: 0.0001", "imports_per_capita_per_year: .inf", "imports_per_capita_per_year")]
    [InlineData("rules.yaml", "mobilization_cost_per_year: 40", "mobilization_cost_per_year: .nan", "mobilization_cost_per_year")]
    [InlineData("rules.yaml", "natural_increase_per_year: 0.017", "natural_increase_per_year: .nan", "natural_increase_per_year")]
    [InlineData("rules.yaml", "initial_population: { veterans: 1000 }", "initial_population: { veterans: -5 }", "initial_population.veterans")]
    [InlineData("rules.yaml", "initial_population: { veterans: 1000 }", "initial_population: { veterans: .inf }", "initial_population.veterans")]
    [InlineData("rules.yaml", "budget_start: { finance_minister: 1.0 }", "budget_start: { finance_minister: .nan }", "budget_start.finance_minister")]
    [InlineData("rules.yaml", "budget_start: { finance_minister: 1.0 }", "budget_start: { finance_minister: 1.5, defense_minister: -0.5 }", "budget_start.finance_minister")]
    [InlineData("rules.yaml", "budget_start: { finance_minister: 1.0 }", "budget_start: { finance_minister: 0.9 }", "budget_start")]
    [InlineData("rules.yaml", "ceasefire_weeks: 4", "ceasefire_weeks: 0", "ceasefire_weeks")]
    [InlineData("rules.yaml", "ceasefire_weeks: 4", "ceasefire_weeks: -5", "ceasefire_weeks")]
    [InlineData("rules.yaml", "start_rung: 5", "start_rung: 0", "start_rung")]
    [InlineData("rules.yaml", "start_rung: 5", "start_rung: 6", "start_rung")]
    [InlineData("aliyah.yaml", "europe: 230", "europe: -230", "series[0].origins.europe")]
    [InlineData("aliyah.yaml", "europe: 230", "europe: .nan", "series[0].origins.europe")]
    [InlineData("aliyah.yaml", "  - from: 1948-05-15\n    to:", "  - to:", "series[0].from")]
    [InlineData("aliyah.yaml", "to: 1948-12-31", "to: 1948-05-01", "series[0].to")]
    [InlineData("population.yaml", "anchors: []", "anchors:\n  - date: 1948-11-08\n    jews: -1\n", "anchors[0].jews")]
    [InlineData("population.yaml", "anchors: []", "anchors:\n  - jews: 700\n", "anchors[0].date")]
    [InlineData("people.yaml", "competence: 8", "competence: 11", "people[pm_one].competence")]
    [InlineData("people.yaml", "competence: 8", "competence: -1", "people[pm_one].competence")]
    [InlineData("people.yaml", "loyalty: 9", "loyalty: 50", "people[pm_one].loyalty")]
    [InlineData("people.yaml", "      - office: prime_minister\n        from: 1948-05-14\n", "      - office: prime_minister\n", "people[pm_one].offices.prime_minister.from")]
    [InlineData("legislatures.yaml", "seats: { alpha: 4, beta: 6 }", "seats: { alpha: -2, beta: 12 }", "legislatures[house].seats.alpha")]
    [InlineData("legislatures.yaml", "    size: 5\n", "    size: 0\n", "legislatures[council].size")]
    [InlineData("legislatures.yaml", "    from: 1949-02-14\n", "", "legislatures[house].from")]
    [InlineData("legislatures.yaml", "to: 1949-02-14", "to: 1948-05-14", "legislatures[council].to")]
    [InlineData("governments.yaml", "    from: 1949-03-10\n", "", "governments[gov2].from")]
    [InlineData("governments.yaml", "to: 1949-03-10", "to: 1948-01-01", "governments[gov1].to")]
    [InlineData("events/drought.yaml", "delta: 0.1", "delta: .nan", "events[drought].options[ration].effects.food_supply.delta")]
    [InlineData("events/drought.yaml", "delta: 0.1", "delta: .inf", "events[drought].options[ration].effects.food_supply.delta")]
    [InlineData("events/drought.yaml", "max: 0.9", "max: .nan", "events[drought].conditions.food_supply.max")]
    [InlineData("events/drought.yaml", "from: 1948-06-01\n", "", "events[drought].from")]
    [InlineData("events/drought.yaml", "to: 1948-09-01", "to: 1948-06-01", "events[drought].to")]
    [InlineData("events/drought.yaml", "to: 1948-09-01", "to: 1948-01-01", "events[drought].to")]
    public void NumberOrDateOutOfRangeIsAContentError(string file, string from, string to, string field)
    {
        var ex = Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    // Final fix 3 M6: event condition bounds are finite, within [0,1], and min <= max.
    [Theory]
    [InlineData("max: 0.9", "max: .inf", "events[drought].conditions.food_supply.max", "must be a finite number")]
    [InlineData("max: 0.9", "max: 1.5", "events[drought].conditions.food_supply.max", "must be a number from 0 to 1")]
    [InlineData("max: 0.9", "max: -0.1", "events[drought].conditions.food_supply.max", "must be a number from 0 to 1")]
    [InlineData("max: 0.9", "min: -0.5\n    max: 0.9", "events[drought].conditions.food_supply.min", "must be a number from 0 to 1")]
    [InlineData("max: 0.9", "min: .nan\n    max: 0.9", "events[drought].conditions.food_supply.min", "must be a finite number")]
    [InlineData("max: 0.9", "min: 0.95\n    max: 0.9", "events[drought].conditions.food_supply.min", "must not exceed max")]
    public void EventConditionBoundsAreCheckedAtLoad(string from, string to, string field, string message)
    {
        var ex = Bad("events/drought.yaml", from, to);
        Assert.Equal("events/drought.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.EndsWith(message, ex.Message);
    }

    [Fact]
    public void EqualConditionBoundsAreAllowed() =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event.Replace("max: 0.9", "min: 0.9\n    max: 0.9") }));

    [Fact]
    public void EqualMobilizationFloorsAreAllowed() =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("[0.02, 0.05, 0.15, 0.4, 0.8]", "[0.05, 0.05, 0.15, 0.4, 0.8]") }));

    // M8: every content id has the office-id shape.
    [Theory]
    [InlineData("events/drought.yaml", "id: drought", "id: 'dro,ught'", "events[dro,ught].id")]
    [InlineData("events/drought.yaml", "id: wait", "id: 'wa,it'", "events[drought].options[wa,it].id")]
    [InlineData("parties.yaml", "id: beta", "id: 'be,ta'", "parties[be,ta].id")]
    [InlineData("people.yaml", "id: def_two", "id: 'def,two'", "people[def,two].id")]
    [InlineData("legislatures.yaml", "id: house", "id: 'ho,use'", "legislatures[ho,use].id")]
    [InlineData("governments.yaml", "id: gov2", "id: 'gov,2'", "governments[gov,2].id")]
    [InlineData("model.yaml", "id: food_supply", "id: 'food,supply'", "indicators[food,supply].id")]
    [InlineData("model.yaml", "id: rationing\n", "id: 'Rationing'\n", "policies[Rationing].id")]
    [InlineData("model.yaml", "id: rationing_food", "id: 'rationing,food'", "effects[rationing,food].id")]
    [InlineData("model.yaml", "id: hunger", "id: 'hun,ger'", "situations[hun,ger].id")]
    [InlineData("model.yaml", "id: everyone", "id: 'every one'", "voter_groups[every one].id")]
    [InlineData("model.yaml", "id: usa", "id: 'u,sa'", "actors[u,sa].id")]
    public void IdOutsideTheIdPatternIsAContentError(string file, string from, string to, string field)
    {
        var ex = Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    // I6: living is required, so a forgotten value cannot ship a living person in a release build.
    [Theory]
    [InlineData("    living: false\n", "")]
    [InlineData("    living: false\n", "    living:\n")]
    public void LivingIsRequired(string from, string to)
    {
        var ex = Bad("people.yaml", from, to);
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].living", ex.Field);
    }

    // M1: event options can set policies.
    internal static readonly string RationingEvent = Event.Replace("    historical: true\n", "    historical: true\n    set_policy: { rationing: 1.0 }\n");

    [Fact]
    public void ChoosingAnOptionSetsItsPolicies()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = RationingEvent }));
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Equal(0.0, s.Policies["rationing"]);
        Assert.Null(EventSystem.Choose(c, s, "drought", "ration", why));
        Assert.Equal(1.0, s.Policies["rationing"]);
        Assert.Contains(why, w => w.System == "events" && w.Target == "rationing" && w.Delta == 1.0);
    }

    [Theory]
    [InlineData("set_policy: { rationing: 1.0 }", "set_policy: { bread: 1.0 }", "events[drought].options[ration].set_policy.bread")]
    [InlineData("set_policy: { rationing: 1.0 }", "set_policy: { rationing: 1.5 }", "events[drought].options[ration].set_policy.rationing")]
    [InlineData("set_policy: { rationing: 1.0 }", "set_policy: { rationing: .nan }", "events[drought].options[ration].set_policy.rationing")]
    public void SetPolicyNeedsAKnownPolicyAndALevelFrom0To1(string from, string to, string field)
    {
        var bad = RationingEvent.Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = bad })));
        Assert.Equal("events/drought.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void DuplicateKeyIsAContentError()
    {
        // YamlDotNet keeps the last of two equal keys by default, which would silently drop the first value.
        var ex = Bad("legislatures.yaml", "    size: 10\n", "    size: 10\n    seats:\n");
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.StartsWith("line ", ex.Field);
    }
}
