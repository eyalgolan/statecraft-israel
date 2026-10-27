using System.Globalization;
using Statecraft.Sim;

// R-PROJ-001, R-PROJ-002, R-PROJ-003: detached catalogue contract only.
// Written before the adapter. Red/green execution is reserved for the APPLY lane.
public class Gdd03ProjectCatalogueTests
{
    static Content Input(string identity = SaveGame.Gdd03Ruleset)
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        // Synthetic identity selection only; no scenario, region or facility binding.
        content.Eras = new() { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = identity } } };
        return content;
    }

    [Theory]
    [InlineData("he-IL")]
    [InlineData("en-US")]
    public void SettledDescriptorsRetainExactValuesAndProvenance(string locale)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            var c = Gdd03ProjectCatalogue.Read(Input());
            Assert.Equal(3m, c.BuilderSlots.Amount);
            Assert.Equal(1m, c.CityStarts.Amount);
            Assert.Equal(new[] { 1m, 1.5m, 3m }, new[] { c.OrdinaryOrNoviceRate.Amount, c.ApprenticeRate.Amount, c.ExpertRate.Amount });
            // content/gdd03-balance.yaml:524-667; GDD balance.md:78-84.
            var expected = new[] {
                ("maabara", 5m, 1m, 0.10m), ("permanent_housing", 15m, 3m, 0.25m),
                ("industry", 10m, 3m, 0.25m), ("reception_camp", 10m, 3m, 0.25m),
                ("training_centre", 10m, 3m, 0.50m), ("arms_workshop", 15m, 3m, 0.50m),
                ("military_camp", 12m, 3m, 0.50m)
            };
            Assert.Equal(expected.Select(x => x.Item1), c.Projects.Select(x => x.Key));
            foreach (var (key, cost, work, upkeep) in expected)
            {
                var p = Assert.Single(c.Projects.Where(x => x.Key == key));
                Assert.Equal(cost, p.Cost.Amount); Assert.Equal(work, p.Work.Amount); Assert.Equal(upkeep, p.Upkeep.Amount);
                Assert.Equal("Default", p.Upkeep.Basis);
                Assert.Equal(key == "military_camp" ? "Default" : "Agreed", p.Cost.Basis);
                Assert.Equal(p.Cost.Basis, p.Work.Basis);
                Assert.Equal("values." + key + ".parts.upkeep", p.Upkeep.SourcePath);
                Assert.False(string.IsNullOrWhiteSpace(p.Upkeep.Note));
            }
            Assert.Equal(1m, c.Projects[0].Capacity!.Amount); Assert.Equal(-10m, c.Projects[0].ConditionPp!.Amount);
            Assert.Equal(3m, c.Projects[1].Capacity!.Amount); Assert.Equal(10m, c.Projects[1].ConditionPp!.Amount);
            Assert.Equal(1m, c.Projects[2].Capacity!.Amount);
            Assert.Equal(3m, c.Projects[3].Capacity!.Amount);
            Assert.Equal("reception.capacity", c.Projects[3].Capacity!.SourcePath.Substring("values.".Length));
            Assert.Equal(Gdd03ProjectCatalogue.CompletionEffect.MatchingTeacherCourseAndHousing, c.Projects[4].Effect);
            Assert.Null(c.Projects[4].Capacity);
            Assert.Equal("Default", c.Projects[3].StandaloneBasis); Assert.Equal("Default", c.Projects[4].StandaloneBasis);
            Assert.Equal(1m, c.Projects[5].Capacity!.Amount); Assert.Equal(5m, c.Projects[6].Capacity!.Amount);
            Assert.Equal(new[] {
                Gdd03ProjectCatalogue.CompletionEffect.OrdinaryHousing, Gdd03ProjectCatalogue.CompletionEffect.OrdinaryHousing,
                Gdd03ProjectCatalogue.CompletionEffect.IndustryJob, Gdd03ProjectCatalogue.CompletionEffect.ReceptionBeds,
                Gdd03ProjectCatalogue.CompletionEffect.MatchingTeacherCourseAndHousing,
                Gdd03ProjectCatalogue.CompletionEffect.WorkshopWorker, Gdd03ProjectCatalogue.CompletionEffect.MilitaryTrainees
            }, c.Projects.Select(x => x.Effect));
            Assert.DoesNotContain(c.Projects, x => x.Key == "winery" || x.Key == "housing_repair");
        }
        finally { CultureInfo.CurrentCulture = previous; CultureInfo.CurrentUICulture = previousUi; }
    }

    [Theory]
    [InlineData(SaveGame.LegacyRuleset)] [InlineData(SaveGame.GddRuleset)]
    [InlineData("GDD-0.3")] [InlineData("gdd-0.30")] [InlineData("future")]
    public void IdentityIsExactEvenWithCataloguePresent(string identity)
    {
        var input = Input(identity);
        input.Gdd03Balance = null; // Identity refusal precedes any data access.
        Assert.Throws<InvalidOperationException>(() => Gdd03ProjectCatalogue.Read(input));
    }

    [Fact]
    public void PresenceAloneCannotActivateAdapter()
    {
        var input = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(SaveGame.GddRuleset, input.Ruleset);
        Assert.Throws<InvalidOperationException>(() => Gdd03ProjectCatalogue.Read(input));
    }

    [Fact]
    public void MissingOrWrongCatalogueFailsClosed()
    {
        var input = Input(); input.Gdd03Balance = null;
        Assert.Throws<ContentException>(() => Gdd03ProjectCatalogue.Read(input));
        input = Input(); input.Gdd03Balance!.Ruleset = SaveGame.GddRuleset;
        Assert.Throws<ContentException>(() => Gdd03ProjectCatalogue.Read(input));
        input = Input(); input.Gdd03Balance!.Values.Remove("industry");
        Assert.Throws<ContentException>(() => Gdd03ProjectCatalogue.Read(input));
    }

    [Theory]
    [InlineData("deferred")] [InlineData("expression")] [InlineData("missing")]
    [InlineData("negative")] [InlineData("zero")] [InlineData("arity")]
    [InlineData("fractional_slots")] [InlineData("overflow_slots")]
    [InlineData("fractional_work")] [InlineData("condition_range")]
    [InlineData("tier_arity")] [InlineData("tier_order")] [InlineData("tier_zero")]
    [InlineData("provenance")] [InlineData("unknown_part")]
    [InlineData("null_part")] [InlineData("null_values")] [InlineData("upkeep_basis")]
    [InlineData("capacity_fraction")] [InlineData("standalone_expression")]
    public void MalformedOrUnsettledPartsAreRejected(string defect)
    {
        // All mutations below are test-only synthetic invalid data, never balance choices.
        var input = Input(); var values = input.Gdd03Balance!.Values;
        var cost = values["industry"].Parts["cost"];
        switch (defect)
        {
            case "deferred": cost.Basis = "Deferred"; cost.Values.Clear(); cost.Expression = "unresolved"; break;
            case "expression": cost.Values.Clear(); cost.Expression = "arbitrary formula"; break;
            case "missing": values["industry"].Parts.Remove("work"); break;
            case "negative": cost.Values[0] = -1m; break;
            case "zero": values["industry"].Parts["upkeep"].Values[0] = 0m; break;
            case "arity": cost.Values.Add(1m); break;
            case "fractional_slots": values["builder.slots_per_project"].Values[0] = 1.5m; break;
            case "overflow_slots": values["builder.slots_per_project"].Values[0] = decimal.MaxValue; break;
            case "fractional_work": values["industry"].Parts["work"].Values[0] = 1.5m; break;
            case "condition_range": values["maabara"].Parts["condition_pp"].Values[0] = -101m; break;
            case "tier_arity": values["builder.work_rate"].Values.RemoveAt(0); break;
            case "tier_order": values["builder.work_rate"].Values.Reverse(); break;
            case "tier_zero": values["builder.work_rate"].Values[0] = 0m; break;
            case "provenance": cost.Note = ""; break;
            case "unknown_part": values["industry"].Parts["typo"] = cost; break;
            case "null_part": values["industry"].Parts["cost"] = null!; break;
            case "null_values": cost.Values = null!; break;
            case "upkeep_basis": values["industry"].Parts["upkeep"].Basis = "Agreed"; break;
            case "capacity_fraction": values["reception.capacity"].Values[0] = 1.5m; break;
            case "standalone_expression": values["reception_camp"].Parts["standalone_application"].Expression = "evaluate this"; break;
        }
        Assert.Throws<ContentException>(() => Gdd03ProjectCatalogue.Read(input));
    }

    [Fact]
    public void SnapshotCopiesCatalogueWithoutFallbackOrMutableAliases()
    {
        var input = Input(); var cost = input.Gdd03Balance!.Values["industry"].Parts["cost"];
        cost.Values[0] = 7.25m; cost.Note = "test-only synthetic price";
        var result = Gdd03ProjectCatalogue.Read(input);
        cost.Values[0] = 9m;
        var industry = Assert.Single(result.Projects.Where(x => x.Key == "industry"));
        Assert.Equal(7.25m, industry.Cost.Amount); Assert.Equal("test-only synthetic price", industry.Cost.Note);
        Assert.Throws<NotSupportedException>(() => ((IList<Gdd03ProjectCatalogue.Project>)result.Projects).Clear());
    }

    // R-PROJ-001: every provenance field is copied from its own synthetic-labelled source, without aliases.
    [Fact] public void ProjectAndNumberProvenanceIsCopiedFromTheCorrectRows()
    {
        var input = Input(); var values = input.Gdd03Balance!.Values;
        foreach (var pair in values)
        {
            pair.Value.Note = "synthetic root " + pair.Key;
            foreach (var part in pair.Value.Parts) part.Value.Note = "synthetic part " + pair.Key + "." + part.Key;
        }
        var result = Gdd03ProjectCatalogue.Read(input);
        void Check(Gdd03ProjectCatalogue.Number number, string key, string? part = null)
        {
            Assert.Equal("values." + key + (part == null ? "" : ".parts." + part), number.SourcePath);
            Assert.Equal(part == null ? "synthetic root " + key : "synthetic part " + key + "." + part, number.Note);
            Assert.Equal(part == null ? values[key].Basis : values[key].Parts[part].Basis, number.Basis);
        }
        void Verify()
        {
            Check(result.BuilderSlots, "builder.slots_per_project"); Check(result.CityStarts, "construction.starts_per_turn");
            foreach (var rate in new[] { result.OrdinaryOrNoviceRate, result.ApprenticeRate, result.ExpertRate }) Check(rate, "builder.work_rate");
            foreach (var project in result.Projects)
            {
                var key = project.Key;
                Assert.Equal(key == "military_camp" ? "Default" : "Agreed", project.Basis);
                Assert.Equal("synthetic root " + key, project.Note);
                Check(project.Cost, key, "cost"); Check(project.Work, key, "work"); Check(project.Upkeep, key, "upkeep");
                if (project.ConditionPp != null) Check(project.ConditionPp, key, "condition_pp");
                if (project.Capacity != null)
                {
                    if (key == "reception_camp") Check(project.Capacity, "reception.capacity");
                    else if (key == "military_camp") Check(project.Capacity, "military.camp_capacity");
                    else Check(project.Capacity, key, key == "industry" ? "job_slots" : key == "arms_workshop" ? "worker_slots" : "housing");
                }
                if (key == "reception_camp" || key == "training_centre")
                {
                    Assert.Equal("Default", project.StandaloneBasis);
                    Assert.Equal("synthetic part " + key + ".standalone_application", project.StandaloneNote);
                }
                else { Assert.Null(project.StandaloneBasis); Assert.Null(project.StandaloneNote); }
            }
        }
        Verify();
        foreach (var row in values.Values) { row.Note = "mutated synthetic note"; foreach (var part in row.Parts.Values) part.Note = "mutated synthetic note"; }
        Verify();
    }
}
