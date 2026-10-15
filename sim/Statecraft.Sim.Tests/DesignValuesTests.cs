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
        ("rules.yaml", "mvp.cohesion_housing_weight"),
        ("rules.yaml", "mvp.cohesion_jobs_weight"),
        ("rules.yaml", "mvp.build_yishuv_turns"),
        ("rules.yaml", "mvp.build_yishuv_cost"),
        ("rules.yaml", "mvp.objective_points"),
        ("rules.yaml", "mvp.objective_bonus_per_month"),
        ("rules.yaml", "mvp.objective_bonus_cap"),
        ("cities.yaml", "cities[tel_aviv].housing_capacity"),
        ("cities.yaml", "cities[tel_aviv].job_slots"),
        ("cities.yaml", "cities[tel_aviv].civilians"),
        ("cities.yaml", "cities[tel_aviv].employed"),
        ("unit_types.yaml", "types[recruit].training_turns"),
        ("unit_types.yaml", "types[recruit].training_cost"),
        ("unit_types.yaml", "types[halutz].training_turns"),
        ("unit_types.yaml", "types[halutz].training_cost"),
    };


    /// <summary>Every number in the GDD files and blocks, as (file, key path, the line it is on). Scope: all of cities.yaml and
    /// unit_types.yaml, rules.yaml mvp:, and the GDD kinds under rules.yaml attention_cost.</summary>
    public static List<(string File, string Key, string Line)> Numbers(string root)
    {
        var found = new List<(string, string, string)>();
        foreach (var file in new[] { "rules.yaml", "cities.yaml", "unit_types.yaml" })
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
                        foreach (var kv in m.Children)
                        {
                            var k = ((YamlScalarNode)kv.Key).Value!;
                            var child = key == "" ? k : key + "." + k;
                            var scope = inScope || file != "rules.yaml" || k == "mvp" || (key == "attention_cost" && AttentionSystem.IsGddKind(k));
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
                    case YamlScalarNode sc when inScope && sc.Style == YamlDotNet.Core.ScalarStyle.Plain
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
        Assert.Equal((1000, 0.06, 0.5, 0.5), (m.PeoplePerUnit, m.IncomePerJobPerYear, m.CohesionHousingWeight, m.CohesionJobsWeight));
        Assert.Equal((0.5, 1.0, 0.5), (c.Rules.AttentionCost["assign_workers"], c.Rules.AttentionCost["train"], c.Rules.AttentionCost["cancel_training"]));
        Assert.Equal((0.0, 0.0), (c.Rules.AttentionCost["pioneer_work"], c.Rules.AttentionCost["stop_work"]));   // D-A6: unit orders are free
        Assert.Equal((6, 2.0), (m.BuildYishuvTurns, m.BuildYishuvCost));
        Assert.Equal((100, 5, 60), (m.ObjectivePoints, m.ObjectiveBonusPerMonth, m.ObjectiveBonusCap));   // D-B6, D-B2
        var city = Assert.Single(c.Cities);
        Assert.Equal(("tel_aviv", 120, 90, 100, 80), (city.Id, city.HousingCapacity, city.JobSlots, city.Civilians, city.Employed));
        Assert.Equal(new[] { ("recruit", 3, 0.5), ("halutz", 4, 0.5) },
                     c.UnitTypes.Where(t => t.Facility != "").Select(t => (t.Id, t.TrainingTurns, t.TrainingCost)));
    }

    // R-LOOP-005
    [Fact]
    public void AnUntaggedOrUnlistedNumberIsFound()
    {
        var dir = Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["cities.yaml"] = "cities:\n  - id: town_a\n    housing_capacity: 12   " + Tag + " (D-A4)\n    job_slots: 8\n",
        }));
        var numbers = Numbers(dir).Where(n => n.File == "cities.yaml").ToList();
        Assert.Equal(new[] { "cities[town_a].housing_capacity", "cities[town_a].job_slots" }, numbers.Select(n => n.Key));
        Assert.Contains(Tag, numbers[0].Line);
        Assert.DoesNotContain(Tag, numbers[1].Line);
        // rules.yaml: the mvp block and the GDD kinds are in scope, the legacy numbers are not.
        var rules = Numbers(dir).Where(n => n.File == "rules.yaml").Select(n => n.Key).ToList();
        Assert.Contains("mvp.people_per_unit", rules);
        Assert.Contains("attention_cost.train", rules);
        Assert.DoesNotContain("attention_cost.set_policy", rules);
        Assert.DoesNotContain("treasury_start", rules);
    }
}
