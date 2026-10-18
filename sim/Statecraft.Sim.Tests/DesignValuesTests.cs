using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;

/// <summary>Every balance number of the GDD loop lives in content/ as a labelled design value (Q-CUT-1, owner, 15 October
/// 2026: "Labelled design values"): tagged on its own line, and listed here, so a new number cannot slip in untagged and
/// the owner reviews one list. R-LOOP-005.</summary>
public class DesignValuesTests
{
    public const string Tag = "# DESIGN VALUE: owner-set balance, not a historical figure";

    /// <summary>Every design value: file, then the path of its key (a list item by its id). Adding one here and in content is
    /// a balance change for the owner to set or accept in review.</summary>
    public static readonly (string File, string Key)[] Listed =
    {
        ("rules.yaml", "attention_cost.assign_workers"),
        ("rules.yaml", "attention_cost.train"),
        ("rules.yaml", "attention_cost.cancel_training"),
        ("rules.yaml", "attention_cost.pioneer_work"),
        ("rules.yaml", "attention_cost.stop_work"),
        ("rules.yaml", "mvp.people_per_unit"),
        ("rules.yaml", "mvp.income_per_job_per_year"),
        ("rules.yaml", "mvp.craftsman_income_multiple"),
        ("rules.yaml", "mvp.city_upkeep_per_year"),
        ("rules.yaml", "mvp.housing_upkeep_per_year"),
        ("rules.yaml", "mvp.industry_upkeep_per_year"),
        ("rules.yaml", "mvp.transit_camp_upkeep_per_year"),
        ("rules.yaml", "mvp.military_camp_upkeep_per_year"),
        ("rules.yaml", "mvp.hakhshara_upkeep_per_year"),
        ("rules.yaml", "tax_per_capita_per_year"),
        ("rules.yaml", "spending_per_capita_per_year"),
        ("rules.yaml", "mvp.build_yishuv_turns"),
        ("rules.yaml", "mvp.build_yishuv_cost"),
        ("rules.yaml", "mvp.build_camp_turns"),
        ("rules.yaml", "mvp.build_camp_cost"),
        ("rules.yaml", "mvp.plow_fields_turns"),
        ("rules.yaml", "mvp.plow_fields_cost"),
        ("rules.yaml", "mvp.dry_swamp_turns"),
        ("rules.yaml", "mvp.dry_swamp_cost"),
        ("rules.yaml", "mvp.field_food_per_turn"),
        ("rules.yaml", "mvp.objective_points"),
        ("rules.yaml", "mvp.objective_bonus_per_month"),
        ("rules.yaml", "mvp.objective_bonus_cap"),
        ("rules.yaml", "mvp.k_food"),
        ("rules.yaml", "mvp.cover_months"),
        ("rules.yaml", "mvp.veteran_halutz_service"),
        ("rules.yaml", "mvp.veteran_progress"),
        ("rules.yaml", "mvp.housing_start_condition"),
        ("rules.yaml", "mvp.overflow_condition_loss"),
        ("rules.yaml", "mvp.defeat_turns"),
        ("rules.yaml", "mvp.debt_ceiling"),
        ("rules.yaml", "mvp.delegation_loyalty_floor"),   // R-EVT-003, #107
        ("rules.yaml", "mvp.disloyal_effect_share"),
        // Q-START-1 (owner, 15 October 2026, #375): no city has a sourced housing stock or job count, so every city's
        // housing is its start people and its job slots its start employed; seven towns with no CBS working share take
        // the national one. The census people and the five CBS shares are sourced, not design values (R-CITY-006).
        ("cities.yaml", "cities[jerusalem].housing_capacity.value"),
        ("cities.yaml", "cities[jerusalem].job_slots.value"),
        ("cities.yaml", "cities[tel_aviv].housing_capacity.value"),
        ("cities.yaml", "cities[tel_aviv].job_slots.value"),
        ("cities.yaml", "cities[haifa].housing_capacity.value"),
        ("cities.yaml", "cities[haifa].job_slots.value"),
        ("cities.yaml", "cities[tiberias].housing_capacity.value"),
        ("cities.yaml", "cities[tiberias].job_slots.value"),
        ("cities.yaml", "cities[safed].housing_capacity.value"),
        ("cities.yaml", "cities[safed].job_slots.value"),
        ("cities.yaml", "cities[petah_tikva].employed_share.value"),
        ("cities.yaml", "cities[petah_tikva].housing_capacity.value"),
        ("cities.yaml", "cities[petah_tikva].job_slots.value"),
        ("cities.yaml", "cities[rishon_lezion].employed_share.value"),
        ("cities.yaml", "cities[rishon_lezion].housing_capacity.value"),
        ("cities.yaml", "cities[rishon_lezion].job_slots.value"),
        ("cities.yaml", "cities[zikhron_yaakov].employed_share.value"),
        ("cities.yaml", "cities[zikhron_yaakov].housing_capacity.value"),
        ("cities.yaml", "cities[zikhron_yaakov].job_slots.value"),
        ("cities.yaml", "cities[rosh_pinna].employed_share.value"),
        ("cities.yaml", "cities[rosh_pinna].housing_capacity.value"),
        ("cities.yaml", "cities[rosh_pinna].job_slots.value"),
        ("cities.yaml", "cities[rehovot].employed_share.value"),
        ("cities.yaml", "cities[rehovot].housing_capacity.value"),
        ("cities.yaml", "cities[rehovot].job_slots.value"),
        ("cities.yaml", "cities[hadera].employed_share.value"),
        ("cities.yaml", "cities[hadera].housing_capacity.value"),
        ("cities.yaml", "cities[hadera].job_slots.value"),
        ("cities.yaml", "cities[netanya].employed_share.value"),
        ("cities.yaml", "cities[netanya].housing_capacity.value"),
        ("cities.yaml", "cities[netanya].job_slots.value"),
        ("unit_types.yaml", "types[recruit].training_turns"),
        ("unit_types.yaml", "types[recruit].training_cost"),
        ("unit_types.yaml", "types[halutz].training_turns"),
        ("unit_types.yaml", "types[halutz].training_cost"),
        ("unit_types.yaml", "types[craftsman].training_turns"),
        ("unit_types.yaml", "types[craftsman].training_cost"),
        // Social cohesion's two new inputs and the national parameters' thresholds (Phase 2 PR 4; Q-NAT-2, Q-NAT-3; Reading 1).
        ("model.yaml", "effects[work_lifts_cohesion].k"),
        ("model.yaml", "effects[work_lifts_cohesion].inertia"),
        ("model.yaml", "effects[food_lifts_cohesion].k"),
        ("model.yaml", "effects[food_lifts_cohesion].inertia"),
        ("model.yaml", "situations[low_cohesion].on_below"),
        ("model.yaml", "situations[low_cohesion].off_above"),
        ("model.yaml", "situations[low_intl_support].on_below"),
        ("model.yaml", "situations[low_intl_support].off_above"),
        // City projects (Phase 3 PR 4; Q-PROJ-1, Q-PROJ-5): agent-proposed under "Agents propose design values" (#375).
        ("rules.yaml", "attention_cost.start_project"),
        ("rules.yaml", "attention_cost.cancel_project"),
        ("projects.yaml", "projects[housing].cost"),
        ("projects.yaml", "projects[housing].turns"),
        ("projects.yaml", "projects[housing].capacity"),
        ("projects.yaml", "projects[housing].condition"),
        ("projects.yaml", "projects[housing].job_slots"),
        ("projects.yaml", "projects[transit_camp].cost"),
        ("projects.yaml", "projects[transit_camp].turns"),
        ("projects.yaml", "projects[transit_camp].capacity"),
        ("projects.yaml", "projects[transit_camp].condition"),
        ("projects.yaml", "projects[transit_camp].job_slots"),
        ("projects.yaml", "projects[industry].cost"),
        ("projects.yaml", "projects[industry].turns"),
        ("projects.yaml", "projects[industry].capacity"),
        ("projects.yaml", "projects[industry].condition"),
        ("projects.yaml", "projects[industry].job_slots"),
    };


    /// <summary>Every number in the GDD files and blocks, as (file, key path, the line it is on). Scope: all of cities.yaml,
    /// unit_types.yaml and projects.yaml, rules.yaml mvp:, and the GDD kinds under rules.yaml attention_cost. A value whose own mapping says
    /// `status: sourced`, `derived` or `wikipedia-only` is a historical figure, not balance: the loader holds it to its
    /// sources and date instead (cities.yaml, R-CITY-006), so it is left out. model.yaml predates the rule (its header calls
    /// every number there a design value), so only its tagged lines are in scope: the numbers Phase 2 PR 4 added.</summary>
    public static List<(string File, string Key, string Line)> Numbers(string root)
    {
        var found = new List<(string, string, string)>();
        foreach (var file in new[] { "rules.yaml", "cities.yaml", "unit_types.yaml", "model.yaml", "projects.yaml" })
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path)) continue;
            var lines = File.ReadAllLines(path);
            var yaml = new YamlStream();
            using (var r = new StringReader(File.ReadAllText(path))) yaml.Load(r);
            var top = (YamlMappingNode)yaml.Documents[0].RootNode;
            void Walk(YamlNode node, string key, bool inScope)
            {
                switch (node)
                {
                    case YamlMappingNode m:
                        if (m.Children.TryGetValue(new YamlScalarNode("status"), out var st) && st is YamlScalarNode ss
                            && (ss.Value == "sourced" || ss.Value == "derived" || ss.Value == "wikipedia-only")) break;
                        foreach (var kv in m.Children)
                        {
                            var k = ((YamlScalarNode)kv.Key).Value!;
                            var child = key == "" ? k : key + "." + k;
                            var scope = inScope || (file != "rules.yaml" && file != "model.yaml") || (file == "rules.yaml" && k == "mvp")
                                || (file == "rules.yaml" && (k == "tax_per_capita_per_year" || k == "spending_per_capita_per_year"))
                                || (key == "attention_cost" && AttentionSystem.IsGddKind(k));
                            Walk(kv.Value, child, scope);
                        }
                        break;
                    case YamlSequenceNode seq:
                        int i = 0;
                        foreach (var item in seq.Children)
                        {
                            var id = item is YamlMappingNode im && im.Children.TryGetValue(new YamlScalarNode("id"), out var v) ? ((YamlScalarNode)v).Value : i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            Walk(item, $"{key}[{id}]", inScope);
                            i++;
                        }
                        break;
                    case YamlScalarNode sc when (inScope || (file == "model.yaml" && lines[(int)sc.Start.Line - 1].Contains(Tag))) && sc.Style == YamlDotNet.Core.ScalarStyle.Plain
                                                  && double.TryParse(sc.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _):
                        found.Add((file, key, lines[(int)sc.Start.Line - 1]));
                        break;
                }
            }
            Walk(top, "", false);
        }
        return found;
    }

    // R-LOOP-005
    [Fact]
    public void EveryBalanceNumberIsListedAndTagged()
    {
        var numbers = Numbers(RealContentTests.ContentRoot());
        var keys = numbers.Select(n => (n.File, n.Key)).ToList();
        foreach (var listed in Listed)
            Assert.True(keys.Contains(listed), $"{listed.File}: {listed.Key} is listed as a design value but is not in content");
        foreach (var n in numbers)
        {
            Assert.True(Listed.Contains((n.File, n.Key)), $"{n.File}: {n.Key} is a GDD balance number that DesignValuesTests.Listed does not list");
            Assert.True(n.Line.Contains(Tag), $"{n.File}: {n.Key} lacks the design-value tag on its line ('{Tag}')");
            Assert.Matches(@"\(D-[A-I]\d+[^)]*\)\s*$", n.Line);   // each names the decision row it belongs to
        }
    }

    // R-LOOP-005
    [Fact]
    public void TheListedValuesAreTheOnesTheGameReads()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var m = c.Rules.Mvp!;
        Assert.Equal((1000, 0.06), (m.PeoplePerUnit, m.IncomePerJobPerYear));
        Assert.Equal(2.0, m.CraftsmanIncomeMultiple);   // Q-CRAFT-2
        Assert.Equal((0.0000545, 0.0001036), (c.Rules.TaxPerCapitaPerYear, c.Rules.SpendingPerCapitaPerYear));
        Assert.Equal((0.0, 0.0, 0.0, 0.0, 0.0, 0.0), (m.CityUpkeepPerYear!.Value, m.HousingUpkeepPerYear!.Value,
            m.IndustryUpkeepPerYear!.Value, m.TransitCampUpkeepPerYear!.Value, m.MilitaryCampUpkeepPerYear!.Value, m.HakhsharaUpkeepPerYear!.Value));
        Assert.Equal((0.5, 1.0, 0.5), (c.Rules.AttentionCost["assign_workers"], c.Rules.AttentionCost["train"], c.Rules.AttentionCost["cancel_training"]));
        Assert.Equal((0.0, 0.0), (c.Rules.AttentionCost["pioneer_work"], c.Rules.AttentionCost["stop_work"]));   // D-A6: unit orders are free
        Assert.Equal((6, 2.0), (m.BuildYishuvTurns, m.BuildYishuvCost));
        Assert.Equal((100, 5, 60), (m.ObjectivePoints, m.ObjectiveBonusPerMonth, m.ObjectiveBonusCap));   // D-B6, D-B2
        Assert.Equal((0.35, 0.15), (m.HousingStartCondition, m.OverflowConditionLoss));   // Q-START-3, Q-HOUSE-1 (D-D1)
        Assert.Equal((5, 0.5), (m.DelegationLoyaltyFloor, m.DisloyalEffectShare));   // R-EVT-003, #107 (D-H1)
        Assert.Equal(12, c.Cities.Count);
        foreach (var x in c.Cities)   // the listed housing and job values are the ones Cities.Init starts from
            Assert.Equal(((int)x.Housing!.Value, (int)x.Jobs!.Value), (x.HousingCapacity, x.JobSlots));
        Assert.Equal(new[] { ("recruit", 3, 0.5), ("halutz", 4, 0.5), ("craftsman", 3, 0.5) },   // the craftsman: Q-CRAFT-3
                     c.UnitTypes.Where(t => t.Facility != "").Select(t => (t.Id, t.TrainingTurns, t.TrainingCost)));
    }

    // R-LOOP-005
    [Fact]
    public void AnUntaggedOrUnlistedNumberIsFound()
    {
        var dir = Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["cities.yaml"] = "cities:\n  - id: town_a\n    people: { value: 100, status: sourced, date: 1948-11-08, sources: [{ url: \"https://example.org/census\", checked: 2026-10-15 }] }\n"
                + "    housing_capacity: { value: 12, status: design }   " + Tag + " (D-A4)\n    job_slots: { value: 8, status: design }\n",
        }));
        var numbers = Numbers(dir).Where(n => n.File == "cities.yaml").ToList();
        // the sourced census value is not a balance number; the two design values are, tagged or not
        Assert.Equal(new[] { "cities[town_a].housing_capacity.value", "cities[town_a].job_slots.value" }, numbers.Select(n => n.Key));
        Assert.Contains(Tag, numbers[0].Line);
        Assert.DoesNotContain(Tag, numbers[1].Line);
        // rules.yaml: the mvp block and the GDD kinds are in scope, the legacy numbers are not.
        var rules = Numbers(dir).Where(n => n.File == "rules.yaml").Select(n => n.Key).ToList();
        Assert.Contains("mvp.people_per_unit", rules);
        Assert.Contains("attention_cost.train", rules);
        Assert.DoesNotContain("attention_cost.set_policy", rules);
        Assert.DoesNotContain("treasury_start", rules);
        Assert.Contains("tax_per_capita_per_year", rules);
        Assert.Contains("spending_per_capita_per_year", rules);
    }
}
