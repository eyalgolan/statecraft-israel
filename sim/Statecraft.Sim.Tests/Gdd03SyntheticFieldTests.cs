using Statecraft.Sim;
using System.Globalization;

public sealed class Gdd03SyntheticFieldTests
{
    // R-FIELD-002: fixture arithmetic only; these tests do not exercise a turn path.
    static Gdd03BalanceCatalogue Catalogue() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!;
    static Gdd03SyntheticFieldInput Field(CivilianTier? tier = CivilianTier.Trained,
        bool prepared = true, bool workable = true, bool friendly = true, bool eligible = true) =>
        new Gdd03SyntheticFieldInput("synthetic-field", prepared, workable, friendly, eligible, tier);

    [Theory]
    [InlineData(CivilianTier.Novice, 2)]
    [InlineData(CivilianTier.Trained, 3)]
    [InlineData(CivilianTier.Veteran, 5)]
    public void R_FIELD_002_StaffedOutputUsesFrozenTier(CivilianTier tier, int expected)
    {
        var rates = Gdd03FieldRates.FromCatalogue(Catalogue());
        var input = Field(tier);
        var output = Gdd03SyntheticFieldEvaluator.Evaluate(rates, input);
        Assert.Equal(1, rates.SlotsPerField);
        Assert.Equal(expected, output.Food);
        Assert.Equal(0, output.Money);
        Assert.True(output.HasSyntheticWork);
        Assert.Equal(tier, input.FrozenTier);
        Assert.Equal(output.Food, Gdd03SyntheticFieldEvaluator.Evaluate(rates, input).Food);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(false, false, false, false)]
    public void R_FIELD_002_ExplicitInvalidFlagsProduceNothing(bool prepared, bool workable, bool friendly, bool eligible)
    {
        var output = Gdd03SyntheticFieldEvaluator.Evaluate(Gdd03FieldRates.FromCatalogue(Catalogue()),
            Field(prepared: prepared, workable: workable, friendly: friendly, eligible: eligible));
        Assert.Equal(0, output.Food);
        Assert.Equal(0, output.Money);
        Assert.False(output.HasSyntheticWork);
    }

    [Fact]
    public void R_FIELD_002_UnstaffedPreparedFieldProducesNothing()
    {
        var output = Gdd03SyntheticFieldEvaluator.Evaluate(Gdd03FieldRates.FromCatalogue(Catalogue()), Field(null));
        Assert.Equal(0, output.Food);
        Assert.Equal(0, output.Money);
        Assert.False(output.HasSyntheticWork);
    }

    [Theory]
    [InlineData(CivilianTier.None)]
    [InlineData(CivilianTier.Apprentice)]
    [InlineData(CivilianTier.Expert)]
    [InlineData((CivilianTier)999)] // Test-only synthetic invalid enum.
    public void R_FIELD_002_MalformedTierRefusesEvenWhenFlagsDisableWork(CivilianTier tier)
    {
        Assert.Throws<ArgumentException>(() => Field(tier, prepared: false));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData(" ")]
    public void R_FIELD_002_RequiresExplicitSyntheticFieldId(string? id)
    {
        Assert.Throws<ArgumentException>(() => new Gdd03SyntheticFieldInput(id!, true, true, true, true, null));
    }

    [Theory]
    [InlineData("he-IL")] [InlineData("en-US")]
    public void R_FIELD_002_SnapshotsTypedRatesWithoutCultureDependentText(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var catalogue = Catalogue();
            // Test-only synthetic rates, deliberately different from the catalogue's 2/3/5.
            catalogue.Values["farmer.output"].Values = new List<decimal> { 7m, 11m, 13m };
            var rates = Gdd03FieldRates.FromCatalogue(catalogue);
            catalogue.Values["farmer.output"].Values[0] = 99m; // Test-only mutation after snapshot.
            Assert.Equal(7, Gdd03SyntheticFieldEvaluator.Evaluate(rates, Field(CivilianTier.Novice)).Food);
            Assert.Equal(11, Gdd03SyntheticFieldEvaluator.Evaluate(rates, Field(CivilianTier.Trained)).Food);
            Assert.Equal(13, Gdd03SyntheticFieldEvaluator.Evaluate(rates, Field(CivilianTier.Veteran)).Food);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData("farmer.output")] [InlineData("field.slots")]
    public void R_FIELD_002_MissingOrDeferredRatesNeverFallBack(string key)
    {
        var catalogue = Catalogue();
        catalogue.Values.Remove(key);
        AssertKey(catalogue, key);
        catalogue.Values[key] = new Gdd03DesignValue { Basis = "Deferred", Expression = "fixture-only-unresolved", Note = "synthetic" };
        AssertKey(catalogue, key);
    }

    [Theory]
    [InlineData("farmer.output")] [InlineData("field.slots")]
    public void R_FIELD_002_RejectsMalformedRowShapes(string key)
    {
        Action<Gdd03DesignValue>[] mutations = {
            row => row.Basis = "Unknown",
            row => row.Note = "",
            row => row.Values = null!,
            row => row.Values.Clear(),
            row => row.Values.Add(1m), // Test-only malformed arity.
            row => row.Expression = "fixture-only-symbol",
            row => row.Parts = null!,
            row => row.Parts["fixture-only-part"] = new Gdd03DesignValue()
        };
        foreach (var mutate in mutations)
        {
            var catalogue = Catalogue();
            mutate(catalogue.Values[key]);
            AssertKey(catalogue, key);
        }
        var nullRow = Catalogue();
        nullRow.Values[key] = null!;
        AssertKey(nullRow, key);
    }

    [Theory]
    [InlineData("-1")] [InlineData("1.5")] [InlineData("2147483648")]
    public void R_FIELD_002_RejectsNegativeFractionalAndOverflowFood(string number)
    {
        // Test-only malformed numbers; parse explicitly with InvariantCulture under either locale.
        var catalogue = Catalogue();
        catalogue.Values["farmer.output"].Values[0] = decimal.Parse(number, CultureInfo.InvariantCulture);
        AssertKey(catalogue, "farmer.output");
    }

    [Theory]
    [InlineData("0")] [InlineData("2")] [InlineData("1.5")]
    public void R_FIELD_002_RejectsUnsupportedSlotCounts(string number)
    {
        var catalogue = Catalogue();
        catalogue.Values["field.slots"].Values[0] = decimal.Parse(number, CultureInfo.InvariantCulture);
        AssertKey(catalogue, "field.slots");
    }

    [Fact]
    public void R_FIELD_002_RejectsWrongIdentityAndNullArguments()
    {
        var catalogue = Catalogue();
        var rates = Gdd03FieldRates.FromCatalogue(catalogue);
        foreach (var identity in new[] { SaveGame.GddRuleset, "legacy", "gdd-0.3-extra" })
        {
            catalogue.Ruleset = identity;
            Assert.Throws<ContentException>(() => Gdd03FieldRates.FromCatalogue(catalogue));
        }
        Assert.Throws<ArgumentNullException>(() => Gdd03FieldRates.FromCatalogue(null!));
        catalogue.Ruleset = SaveGame.Gdd03Ruleset;
        catalogue.Values = null!;
        Assert.Throws<ContentException>(() => Gdd03FieldRates.FromCatalogue(catalogue));
        Assert.Throws<ArgumentNullException>(() => Gdd03SyntheticFieldEvaluator.Evaluate(null!, Field()));
        Assert.Throws<ArgumentNullException>(() => Gdd03SyntheticFieldEvaluator.Evaluate(rates, null!));
    }

    static void AssertKey(Gdd03BalanceCatalogue catalogue, string key)
    {
        var error = Assert.Throws<ContentException>(() => Gdd03FieldRates.FromCatalogue(catalogue));
        Assert.Equal(Gdd03BalanceCatalogue.FileName, error.File);
        Assert.Contains(key, error.Message);
    }
}
