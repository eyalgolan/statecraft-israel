using Statecraft.Sim;
using System.Globalization;
using YamlDotNet.Serialization;

public partial class DesignValuesTests
{
    const string CatalogueFile = "gdd03-balance.yaml";
    // R-LOOP-005: exact key inventory of #1668 balance.md at 605277df, including project and biome rows.
    static readonly string[] Gdd03Keys = {
        "campaign.play_minutes",
        "money.il_millions",
        "indicator.display_max",
        "indicator.storage_max",
        "indicator.low_on",
        "indicator.low_off",
        "start.cohesion",
        "start.support",
        "event.max_gap_peace",
        "event.max_gap_war",
        "population.people_per_unit",
        "start.population",
        "start.money",
        "start.food",
        "start.spare_arms",
        "start.army",
        "start.farmers",
        "start.prepared_fields",
        "start.trained_pioneers",
        "start.apprentices",
        "start.experts",
        "start.pioneer_teachers",
        "start.military_camps",
        "start.reception_camps",
        "start.training_centres",
        "start.free_civilians",
        "start.industry_slots",
        "start.permanent_housing_projects",
        "start.housing_capacity",
        "start.housing_condition",
        "start.food_production",
        "start.food_consumption",
        "population.food_per_turn",
        "industry.income",
        "farmer.output",
        "profession.farmer_tools",
        "profession.pioneer_tools",
        "profession.course_turns",
        "profession.trainees_per_teacher",
        "profession.work_to_veteran",
        "pioneer.work_rate",
        "craft.course_cost",
        "craft.course_turns",
        "craft.course_income",
        "craft.expert_work_turns",
        "builder.slots_per_project",
        "builder.work_rate",
        "construction.starts_per_turn",
        "field.slots",
        "field.cost",
        "field.work",
        "field.nominal_turns",
        "reception.capacity",
        "reception.turns",
        "reception.basic_credit",
        "housing.condition_max",
        "housing.money_threshold",
        "housing.money_slope",
        "housing.cohesion_threshold",
        "housing.max_cohesion_loss",
        "housing.overflow_deterioration",
        "housing.max_deterioration",
        "transit.cost",
        "reception.queue_cohesion_loss",
        "food.shortage_cohesion_max",
        "food.shortage_combat_slope",
        "import.arms_price",
        "import.food_per_money",
        "import.delivery_turns",
        "immigration.ship_interval",
        "immigration.units_per_ship",
        "immigration.mix",
        "workshop.staffed_turns",
        "workshop.cost",
        "military.camp_capacity",
        "military.basic_turns",
        "military.issue_arms",
        "military.draft_money",
        "military.draft_wait",
        "military.demobilise_money",
        "military.demobilise_wait",
        "military.demobilise_return",
        "military.repeat_issue",
        "military.combat_arms",
        "military.training_money",
        "military.active_money",
        "military.strength",
        "military.combat_xp_gain",
        "military.combat_xp_full",
        "military.refugee_multiplier",
        "military.fort_bonus",
        "military.fortify_turns",
        "military.fortify_bonus",
        "military.stack_limit",
        "military.move_infantry",
        "military.move_armoured",
        "military.move_road",
        "military.move_rail",
        "military.armistice_quiet_turns",
        "occupation.garrison",
        "occupation.turns",
        "annex.tax_town",
        "annex.tax_city",
        "puppet.tax_fraction",
        "annex.support_cost",
        "puppet.support_cost",
        "occupation.tax",
        "prisoner.food",
        "prisoner.return_points",
        "combat.draw_count",
        "combat.enemy_attack_ratio",
        "combat.retreat_hexes",
        "combat.armoured_factor",
        "combat.artillery_range",
        "combat.artillery_factor",
        "combat.loss_cohorts",
        "objective.base_points",
        "objective.early_per_month",
        "objective.early_cap",
        "absorption.population_target",
        "absorption.max_overflow",
        "absorption.min_employment",
        "maabara",
        "permanent_housing",
        "industry",
        "reception_camp",
        "training_centre",
        "arms_workshop",
        "military_camp",
        "housing_repair",
        "designated_yishuv",
        "hula_drainage",
        "road_segment",
        "rail_segment",
        "coastal_plain",
        "lowland",
        "valley",
        "hills",
        "mountains",
        "desert",
        "lake",
        "sea"
    };
    static Dictionary<object, object> Catalogue()
    {
        var path = Path.Combine(RealContentTests.ContentRoot(), CatalogueFile);
        Assert.True(File.Exists(path), "gdd-0.3 balance catalogue must be present");
        return new DeserializerBuilder().WithDuplicateKeyChecking().Build().Deserialize<Dictionary<object, object>>(File.ReadAllText(path));
    }
    static Dictionary<object, object> Row(Dictionary<object, object> map, string key) => (Dictionary<object, object>)map[key];

    [Fact] public void Gdd03CatalogueHasEveryPinnedDesignKeyExactlyOnce()
    {
        var c=Catalogue(); Assert.Equal("gdd-0.3", c["ruleset"]);
        Assert.Equal(Gdd03Keys.OrderBy(x=>x,StringComparer.Ordinal), Row(c,"values").Keys.Cast<string>().OrderBy(x=>x,StringComparer.Ordinal));
    }
    [Fact] public void Gdd03DistinguishesAgreedDefaultsAndDeferredExpressions()
    {
        var v=Row(Catalogue(),"values");
        Assert.Equal("Agreed",Row(v,"population.people_per_unit")["basis"]);
        Assert.Equal("Default",Row(v,"campaign.play_minutes")["basis"]);
        Assert.Equal("Deferred",Row(v,"start.army")["basis"]); Assert.Equal("A",Row(v,"start.army")["expression"]);
        Assert.False(Row(v,"start.army").ContainsKey("values"));
        Assert.Equal("Deferred",Row(v,"start.free_civilians")["basis"]); Assert.Equal("34 - A",Row(v,"start.free_civilians")["expression"]);
        var parts=Row(Row(v,"maabara"),"parts");
        Assert.Equal("Agreed",Row(parts,"cost")["basis"]); Assert.Equal("Default",Row(parts,"upkeep")["basis"]);
        Assert.Equal("Default",Row(v,"start.housing_capacity")["basis"]);
        Assert.Equal("Agreed",Row(v,"military.combat_arms")["basis"]);
        Assert.Contains("owner ruling, 25 Oct 2026, on #1668",(string)Row(v,"military.combat_arms")["note"]);
    }
    [Theory]
    [InlineData("0")][InlineData("1")][InlineData("34")][InlineData("-1")][InlineData("1.5")]
    public void Gdd03LoaderRefusesEveryNumericArmyGuessWhileDeferred(string number)
    {
        var yaml=$"ruleset: gdd-0.3\nvalues:\n  start.army: {{ basis: Deferred, values: [{number}], note: test }}\n";
        var dir=Fixture.Write(new Dictionary<string,string>{{CatalogueFile,yaml}});
        var ex=Assert.Throws<ContentException>(()=>ContentLoader.Load(dir));
        Assert.Equal(CatalogueFile,ex.File); Assert.Contains("start.army",ex.Message); Assert.Contains("Deferred",ex.Message);
    }
    [Fact] public void Gdd03LoaderRefusesNumericArmyExpressionAndUnapprovedPromotion()
    {
        foreach(var row in new[]{"basis: Deferred, expression: '12'", "basis: Default, values: [12]", "basis: Agreed, values: [12]"})
        {
            var dir=Fixture.Write(new Dictionary<string,string>{{CatalogueFile,$"ruleset: gdd-0.3\nvalues:\n  start.army: {{ {row}, note: test }}\n"}});
            var ex=Assert.Throws<ContentException>(()=>ContentLoader.Load(dir)); Assert.Contains("start.army",ex.Message);
        }
    }
    [Theory][InlineData("Guessed")][InlineData("")]
    public void Gdd03LoaderRefusesUnlabelledValues(string basis)
    {
        var dir=Fixture.Write(new Dictionary<string,string>{{CatalogueFile,$"ruleset: gdd-0.3\nvalues:\n  start.army: {{ basis: '{basis}', expression: A, note: test }}\n"}});
        Assert.Throws<ContentException>(()=>ContentLoader.Load(dir));
    }
    [Fact] public void Gdd03CatalogueOnlyAcceptsExactDecimalNumbersAndLabelledParts()
    {
        void Check(Dictionary<object,object> row)
        {
            Assert.Contains((string)row["basis"],new[]{"Agreed","Default","Deferred"}); Assert.False(string.IsNullOrWhiteSpace((string)row["note"]));
            if(row.TryGetValue("values",out var nums)) foreach(var x in (List<object>)nums)
                Assert.True(decimal.TryParse(x.ToString(),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out _));
            if(row.TryGetValue("parts",out var parts)) foreach(var x in ((Dictionary<object,object>)parts).Values) Check((Dictionary<object,object>)x);
        }
        foreach(var r in Row(Catalogue(),"values").Values) Check((Dictionary<object,object>)r);
    }
    [Fact] public void Gdd03CatalogueLoadsWithoutChangingTheExistingLoopAndUsesDecimals()
    {
        var c=ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.NotNull(c.Gdd03Balance); Assert.Equal(SaveGame.GddRuleset,c.Ruleset);
        var v=c.Gdd03Balance.Values;
        Assert.Equal(10000m,Assert.Single(v["population.people_per_unit"].Values));
        Assert.Equal(new[]{0.75m,1m,1.5m},v["pioneer.work_rate"].Values);
        Assert.Equal(0.10m,Assert.Single(v["maabara"].Parts["upkeep"].Values));
        Assert.Equal("2 * current regional housing upkeep",v["housing_repair"].Parts["cost"].Expression);
        Assert.Equal(65m,Assert.Single(v["start.population"].Values));
        Assert.Equal(new[]{5m,10m,5m},v["start.farmers"].Values);
        Assert.Equal(new[]{0.5m,1m,2m},v["combat.draw_count"].Parts["ratio_thresholds"].Values);
        Assert.Null(ContentLoader.Load(Fixture.Write()).Gdd03Balance); // old authored content still loads
    }
    [Theory]
    [InlineData("bad: { basis: Unknown, values: [1], note: test }")]
    [InlineData("bad: { basis: Agreed, values: [1] }")]
    [InlineData("bad: { basis: Agreed, values: } ")]
    [InlineData("bad: { basis: Agreed, parts: } ")]
    [InlineData("bad: null")]
    [InlineData("bad: { basis: Agreed, parts: { cost: null }, note: test }")]
    [InlineData("bad: { basis: Deferred, values: [1], note: test }")]
    [InlineData("bad: { basis: Agreed, values: [1], expression: A, note: test }")]
    [InlineData("bad: { basis: Agreed, values: [1], typo: wrong, note: test }")]
    [InlineData("bad: { basis: Agreed, values: [1], note: test }\n  bad: { basis: Default, values: [2], note: test }")]
    public void Gdd03LoaderRejectsMalformedEntriesAndParts(string row)
    {
        var yaml="ruleset: gdd-0.3\nvalues:\n  start.army: { basis: Deferred, expression: A, note: test }\n  "+row+"\n";
        var dir=Fixture.Write(new Dictionary<string,string>{{CatalogueFile,yaml}});
        var ex=Assert.Throws<ContentException>(()=>ContentLoader.Load(dir)); Assert.Equal(CatalogueFile,ex.File);
    }

}
