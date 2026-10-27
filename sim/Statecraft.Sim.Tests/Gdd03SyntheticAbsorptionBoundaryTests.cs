using System.Globalization;
using Statecraft.Sim;

// R-OBJ-004: inactive boundary contract, not an integrated objective test.
public class Gdd03SyntheticAbsorptionBoundaryTests
{
    static Gdd03BalanceCatalogue Catalogue() =>
        ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!;

    // All counts here are test-only synthetic settled facts, not population/region initialization.
    // 100/101 follows the strict target/scale in gdd03-balance.yaml:46-49,512-515.
    [Theory]
    [InlineData(100, false)]
    [InlineData(101, true)]
    public void RepresentedOwnPopulationMustExceedTheTarget(long own, bool expected)
    {
        Assert.Equal(expected, Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            Catalogue(), own, false, 0, 0, 0, 0));
    }

    // The caller supplies canonical waiting, not a location/status inferred by this predicate.
    [Theory]
    [InlineData(false, true)] // synthetic legal reception bed: no waiting
    [InlineData(true, false)] // synthetic port queue OR refugee evacuation transit: waiting
    public void OnlyTheSuppliedCanonicalWaitingFlagBlocksReception(bool waiting, bool expected)
    {
        Assert.Equal(expected, Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            Catalogue(), 101, waiting, 0, 0, 0, 0));
    }

    // Synthetic demand 100 and free civilians 10; source percentages are 10 and 40.
    [Theory]
    [InlineData(10, 4, true)]
    [InlineData(11, 4, false)]
    [InlineData(10, 3, false)]
    [InlineData(9, 5, true)]
    public void HousingAndEmploymentBoundariesAreInclusive(long overflow, long employed, bool expected)
    {
        Assert.Equal(expected, Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            Catalogue(), 101, false, overflow, 100, employed, 10));
    }

    [Theory]
    [InlineData(0, 0, 4, 10)]
    [InlineData(10, 100, 0, 0)]
    [InlineData(0, 0, 0, 0)]
    public void EmptyDenominatorsPassTheirRespectiveSubconditions(long overflow, long demand, long employed, long free)
    {
        Assert.True(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            Catalogue(), 101, false, overflow, demand, employed, free));
        Assert.False(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            Catalogue(), 0, false, overflow, demand, employed, free));
    }

    // Invalid test-only facts must fail even if the population/waiting gate would already fail.
    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(101, -1, 100, 4, 10)]
    [InlineData(101, 0, -1, 4, 10)]
    [InlineData(101, 0, 100, -1, 10)]
    [InlineData(101, 0, 100, 0, -1)]
    [InlineData(0, 1, 0, 0, 0)]
    [InlineData(101, 0, 100, 1, 0)]
    [InlineData(101, 101, 100, 4, 10)]
    [InlineData(101, 0, 100, 11, 10)]
    public void InvalidSettledCountsAreRejected(long own, long overflow, long demand, long employed, long free)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                Catalogue(), own, true, overflow, demand, employed, free));
    }

    [Theory]
    [InlineData("population.people_per_unit")]
    [InlineData("absorption.population_target")]
    [InlineData("absorption.max_overflow")]
    [InlineData("absorption.min_employment")]
    public void MissingDeferredOrMalformedThresholdsHaveNoFallback(string key)
    {
        foreach (var fault in new[] { "missing", "Deferred", "Default", "empty", "multiple", "fraction", "negative", "expression", "parts", "range" })
        {
            var catalogue = Catalogue();
            var entry = catalogue.Values[key];
            switch (fault)
            {
                case "missing": catalogue.Values.Remove(key); break;
                case "Deferred": entry.Basis = "Deferred"; entry.Values.Clear(); entry.Expression = "unresolved"; break;
                case "Default": entry.Basis = "Default"; break;
                case "empty": entry.Values.Clear(); break;
                case "multiple": entry.Values.Add(1); break;
                case "fraction": entry.Values[0] = 0.5m; break;
                case "negative": entry.Values[0] = -1; break;
                case "expression": entry.Expression = "unresolved"; break;
                case "parts": entry.Parts.Add("synthetic", new Gdd03DesignValue()); break;
                case "range": entry.Values[0] = key.StartsWith("absorption.m", StringComparison.Ordinal) ? 101 : 0; break;
            }
            if (key == "population.people_per_unit")
                Assert.Throws<ContentException>(() => Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                    catalogue, 0, true, 0, 0, 0, 0));
            else
                Assert.Throws<ArgumentException>(() => Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                    catalogue, 0, true, 0, 0, 0, 0));
        }
        Assert.Throws<ArgumentNullException>(() => Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            null!, 101, false, 0, 0, 0, 0));
        var wrongIdentity = Catalogue(); wrongIdentity.Ruleset = "gdd-0.1";
        Assert.Throws<ArgumentException>(() => Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            wrongIdentity, 101, false, 0, 0, 0, 0));
    }

    [Theory]
    [InlineData("he-IL")]
    [InlineData("en-US")]
    public void PopulationScaleSharesMainInt32Limit(string locale)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            var catalogue = Catalogue();
            var content = new Content {
                Eras = new() { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = SaveGame.Gdd03Ruleset } } },
                Gdd03Balance = catalogue
            };
            // Test-only synthetic scales: Int32 maximum is valid; larger whole decimals must be refused.
            catalogue.Values["population.people_per_unit"].Values[0] = int.MaxValue;
            Assert.Equal(int.MaxValue, content.PopulationPeoplePerUnit);
            Assert.True(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                catalogue, long.MaxValue, false, 0, 0, 0, 0));
            foreach (var scale in new[] { int.MaxValue + 1m, decimal.MaxValue })
            {
                catalogue.Values["population.people_per_unit"].Values[0] = scale;
                Assert.Throws<ContentException>(() => { _ = content.PopulationPeoplePerUnit; });
                Assert.Throws<ContentException>(() => Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                    catalogue, long.MaxValue, false, 0, 0, 0, 0));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // Catalogue overrides below are labelled test-only synthetic values, never shipped defaults.
    [Theory]
    [InlineData("population.people_per_unit", 20000, 51, 0, 4, true)]
    [InlineData("absorption.population_target", 2000000, 101, 0, 4, false)]
    [InlineData("absorption.max_overflow", 20, 101, 20, 4, true)]
    [InlineData("absorption.min_employment", 50, 101, 0, 4, false)]
    public void EachThresholdIsReadFromTheSuppliedCatalogue(string key, int syntheticValue,
        long own, long overflow, long employed, bool expected)
    {
        var catalogue = Catalogue();
        catalogue.Values[key].Values[0] = syntheticValue;
        Assert.Equal(expected, Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
            catalogue, own, false, overflow, 100, employed, 10));
    }

    [Theory]
    [InlineData("he-IL")]
    [InlineData("en-US")]
    public void LargeSyntheticCountsAndBothCulturesKeepExactBoundaries(string locale)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            var catalogue = Catalogue();
            // Test-only stress values, chosen near Int64.MaxValue to expose cross-product overflow.
            const long demand = 9000000000000000000;
            const long overflow = 900000000000000000;
            const long employed = 3600000000000000000;
            Assert.True(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                catalogue, long.MaxValue, false, overflow, demand, employed, demand));
            Assert.False(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                catalogue, long.MaxValue, false, overflow + 1, demand, employed, demand));
            Assert.False(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                catalogue, long.MaxValue, false, overflow, demand, employed - 1, demand));
            // Purity: repeated calls retain the caller's catalogue entries and values.
            var entry = catalogue.Values["absorption.max_overflow"];
            var value = entry.Values.Single();
            Assert.True(Gdd03SyntheticAbsorptionBoundary.HoldsForSyntheticSettledFacts(
                catalogue, 101, false, 0, 0, 0, 0));
            Assert.Same(entry, catalogue.Values["absorption.max_overflow"]);
            Assert.Equal(value, entry.Values.Single());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
