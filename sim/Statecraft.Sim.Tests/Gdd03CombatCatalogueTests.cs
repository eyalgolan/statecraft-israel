using System.Globalization;
using Statecraft.Sim;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// R-COMBAT-002, R-COMBAT-007, R-COMBAT-008: #1706 slices 0/1, synthetic catalogue only.
public class Gdd03CombatCatalogueTests
{
    internal static Content Synthetic(string identity = SaveGame.Gdd03Ruleset)
    {
        // Copy design data, never shipped scenario state. The content file is not changed by this draft.
        var data = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithDuplicateKeyChecking().Build().Deserialize<Gdd03BalanceCatalogue>(
                File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName)));
        var c = PopulationLedgerTests.Content(identity);
        c.Gdd03Balance = data;
        return c;
    }

    static Gdd03DesignValue At(Content c, string path)
    {
        var pieces = path.Split('/');
        var value = c.Gdd03Balance!.Values[pieces[0]];
        return pieces.Length == 1 ? value : value.Parts[pieces[1]];
    }

    [Theory, InlineData("he-IL"), InlineData("en-US")]
    public void PositiveFractionalEndpointRefuses(string culture)
    {
        InCulture(culture, () => {
            var c = Synthetic(); At(c, "combat.draw_count/below_0_5").Values[1] = 59.5m;
            Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
        });
    }

    [Theory, InlineData("he-IL"), InlineData("en-US")]
    public void NonnegativeReversedPairRefuses(string culture)
    {
        InCulture(culture, () => {
            var c = Synthetic(); var values = At(c, "combat.draw_count/below_0_5").Values;
            // Keep adjacent endpoints contiguous around the reversed 60..59 pair.
            values[3] = 59; values[4] = 60;
            Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
        });
    }

    public static IEnumerable<object[]> RequiredCases()
    {
        foreach (var culture in new[] { "he-IL", "en-US" })
        foreach (var path in new[] { "combat.draw_count", "combat.draw_count/ratio_thresholds",
            "combat.draw_count/draw_band", "combat.draw_count/below_0_5", "combat.draw_count/from_0_5_below_1",
            "combat.draw_count/from_1_below_2", "combat.draw_count/at_least_2",
            "combat.armoured_factor", "combat.artillery_factor", "combat.artillery_range" })
        foreach (var defect in new[] { "missing", "null", "Deferred", "basis", "relabel", "note", "expression", "values", "parts", "empty", "extra", "fraction" })
            yield return new object[] { culture, path, defect };
    }

    [Theory, MemberData(nameof(RequiredCases))]
    public void EveryRequiredEntryRefusesMissingDeferredOrMalformed(string culture, string path, string defect)
    {
        InCulture(culture, () => {
            var c = Synthetic(); var pieces = path.Split('/'); var v = At(c, path);
            var map = pieces.Length == 1 ? c.Gdd03Balance!.Values : c.Gdd03Balance!.Values[pieces[0]].Parts;
            switch (defect)
            {
                case "missing": map.Remove(pieces[^1]); break;
                case "null": map[pieces[^1]] = null!; break;
                case "Deferred": v.Basis = "Deferred"; v.Values.Clear(); v.Parts.Clear(); v.Expression = "pending"; break;
                case "basis": v.Basis = "default"; break;
                case "relabel": v.Basis = v.Basis == "Agreed" ? "Default" : "Agreed"; break;
                case "note": v.Note = " "; break;
                case "expression": v.Expression = "pending"; break;
                case "values": v.Values = null!; break;
                case "parts": v.Parts = null!; break;
                case "empty": v.Values.Clear(); break;
                case "extra": v.Values.Add(100); break;
                case "fraction": v.Values[0] = -0.25m; break;
            }
            Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
        });
    }

    [Theory, InlineData("he-IL"), InlineData("en-US")]
    public void ExactThresholdsAndAllHundredDrawsMatchPinnedGdd(string culture)
    {
        InCulture(culture, () => {
            var table = Gdd03BalanceCatalogue.ReadCombatCatalogue(Synthetic());
            // Independent endpoint oracle: GDD balance.md:182-185 at 605277df.
            int[][] ends = { new[] { 59, 89, 99, 99 }, new[] { 29, 69, 94, 99 },
                new[] { 9, 39, 84, 99 }, new[] { -1, 9, 69, 99 } };
            var probes = new[] { (0m, 0), (0.4999999999999999999999999999m, 0), (0.5m, 1),
                (0.9999999999999999999999999999m, 1), (1m, 2),
                (1.9999999999999999999999999999m, 2), (2m, 3), (decimal.MaxValue, 3) };
            foreach (var (ratio, row) in probes)
            {
                Assert.Equal(row, table.Row(ratio));
                for (var draw = 0; draw < 100; draw++)
                {
                    var outcome = draw <= ends[row][0] ? Gdd03CombatOutcome.AttackerLoss
                        : draw <= ends[row][1] ? Gdd03CombatOutcome.BothHold
                        : draw <= ends[row][2] ? Gdd03CombatOutcome.DefenderRetreats : Gdd03CombatOutcome.DefenderLoss;
                    Assert.Equal(outcome, table.Outcome(ratio, draw));
                    Assert.Single(table.Bands(row), b => draw >= b.First && draw <= b.Last);
                }
            }
            Assert.Equal(1, table.DrawCount);
            Assert.Equal(1.50m, table.ArmouredFactor); Assert.Equal(1m, table.ArtilleryFactor);
            Assert.Equal(2, table.ArtilleryRange);
            Assert.Equal("Agreed", table.Sources["combat.draw_count"].Basis);
            foreach (var source in table.Sources.Where(p => p.Key != "combat.draw_count"))
                Assert.Equal("Default", source.Value.Basis);
            foreach (var source in table.Sources.Values) Assert.Contains("605277df", source.Note);
            Assert.Throws<ArgumentOutOfRangeException>(() => table.Outcome(1m, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.Outcome(1m, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.Row(-1m));
            // Validated snapshot must not follow later edits to mutable input.
            var c = Synthetic(); var snapshot = Gdd03BalanceCatalogue.ReadCombatCatalogue(c);
            At(c, "combat.draw_count/below_0_5").Values[1] = 0;
            Assert.Equal(Gdd03CombatOutcome.AttackerLoss, snapshot.Outcome(0m, 59));
        });
    }

    [Theory]
    [InlineData("below_0_5", 2, 59)] // overlap
    [InlineData("below_0_5", 2, 61)] // gap
    [InlineData("from_0_5_below_1", 7, 98)] // incomplete
    [InlineData("from_1_below_2", 1, -1)] // reversed
    [InlineData("at_least_2", 5, 100)] // outside band
    [InlineData("ratio_thresholds", 0, 1)] // duplicate
    [InlineData("draw_band", 1, 98)]
    public void InvalidBandsAndThresholdsRefuse(string part, int index, int value)
    {
        var c = Synthetic(); At(c, "combat.draw_count/" + part).Values[index] = value;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
    }

    [Fact]
    public void AdditionalDrawsAreRejectedAtTheCatalogueBoundary()
    {
        var c = Synthetic(); At(c, "combat.draw_count").Values[0] = 2;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
    }

    [Theory, InlineData("combat.armoured_factor"), InlineData("combat.artillery_factor"), InlineData("combat.artillery_range")]
    public void ZeroRoleQuantitiesRefuse(string key)
    {
        var c = Synthetic(); At(c, key).Values[0] = 0;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
    }

    [Fact]
    public void MissingCatalogueAndWrongCatalogueIdentityNeverFallBack()
    {
        var c = Synthetic(); c.Gdd03Balance = null;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
        c = Synthetic(); c.Gdd03Balance!.Ruleset = SaveGame.GddRuleset;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
        c = Synthetic(); c.Gdd03Balance!.Values = null!;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
    }

    [Theory, InlineData("combat.draw_count"), InlineData("combat.artillery_range")]
    public void FractionalCountsRefuse(string key)
    {
        var c = Synthetic(); At(c, key).Values[0] = 1.5m;
        Assert.Throws<ContentException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
    }

    internal static void InCulture(string name, Action action)
    {
        var prior = CultureInfo.CurrentCulture; var ui = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name); CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture; action(); }
        finally { CultureInfo.CurrentCulture = prior; CultureInfo.CurrentUICulture = ui; }
    }
}
