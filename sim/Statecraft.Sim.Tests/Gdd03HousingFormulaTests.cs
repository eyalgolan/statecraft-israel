using Statecraft.Sim;
using System.Globalization;

public class Gdd03HousingFormulaTests
{
    // All counts, Money amounts and conditions below are TEST-ONLY SYNTHETIC inputs.
    static Gdd03HousingFormula Formula() => Gdd03HousingFormula.FromCatalogue(
        ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance ?? throw new InvalidOperationException("GDD 0.3 catalogue required"));

    static Gdd03HousingInput Input(int surcharge = 10, int beforeArrival = 10,
        int cohesion = 10, int population = 20, decimal capacity = 5m,
        decimal condition = 0.40m) =>
        new Gdd03HousingInput(surcharge, beforeArrival, cohesion, population,
            capacity, condition);

    // Each fixture changes one coefficient before the existing reader validates/snapshots it.
    static Gdd03HousingFormula FormulaWithSyntheticScalar(string key, decimal value)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance ?? throw new InvalidOperationException("GDD 0.3 catalogue required");
        var row = c.Values[key];
        row.Values[0] = value;
        row.Note = "TEST-ONLY SYNTHETIC coefficient override: " + value.ToString(CultureInfo.InvariantCulture);
        return Gdd03HousingFormula.FromCatalogue(c);
    }

    // Exact decimal arithmetic only; all divisions below terminate, so no tolerance is needed.
    [Fact] public void SyntheticMoneyThresholdComesFromSuppliedCatalogue()
    {
        // R-HOUSE-001: catches moneyThreshold replaced by the shipped literal 0.50m.
        const decimal threshold = 0.70m;
        var f = FormulaWithSyntheticScalar("housing.money_threshold", threshold);
        var expected = 10m * Math.Max(0m, threshold - 0.40m) * 0.1m;   // the shipped money_slope, 0.1 since #1703 (owner, 2 November 2026)
        Assert.Equal(expected, f.Evaluate(Input(condition: 0.40m)).SurchargeMoney);
    }

    [Fact] public void SyntheticMoneySlopeComesFromSuppliedCatalogue()
    {
        // R-HOUSE-001: catches moneySlope replaced by the shipped literal 0.1m.
        const decimal slope = 2.5m;
        var f = FormulaWithSyntheticScalar("housing.money_slope", slope);
        var expected = 10m * Math.Max(0m, 0.50m - 0.40m) * slope;
        Assert.Equal(expected, f.Evaluate(Input(condition: 0.40m)).SurchargeMoney);
    }

    [Fact] public void SyntheticCohesionThresholdComesFromSuppliedCatalogue()
    {
        // R-HOUSE-002: catches cohesionThreshold replaced by the shipped literal 0.25m.
        const decimal threshold = 0.50m;
        var f = FormulaWithSyntheticScalar("housing.cohesion_threshold", threshold);
        var expected = 2m * (5m / 20m) * Math.Max(0m, (threshold - 0.125m) / threshold);
        Assert.Equal(expected, f.Evaluate(Input(cohesion: 5, population: 20,
            capacity: 10m, condition: 0.125m)).CohesionLossPoints);
    }

    [Fact] public void SyntheticMaximumCohesionLossComesFromSuppliedCatalogue()
    {
        // R-HOUSE-002: catches maxCohesionLoss replaced by the shipped literal 2m.
        const decimal maximumLoss = 3m;
        var f = FormulaWithSyntheticScalar("housing.max_cohesion_loss", maximumLoss);
        var expected = maximumLoss * (5m / 20m) * Math.Max(0m, (0.25m - 0.125m) / 0.25m);
        Assert.Equal(expected, f.Evaluate(Input(cohesion: 5, population: 20,
            capacity: 10m, condition: 0.125m)).CohesionLossPoints);
    }

    [Fact] public void SyntheticOverflowRateComesFromSuppliedCatalogue()
    {
        // R-HOUSE-002: catches overflowPp replaced by the shipped literal 5m.
        const decimal overflowPp = 12.5m;
        var f = FormulaWithSyntheticScalar("housing.overflow_deterioration", overflowPp);
        var expectedLoss = (overflowPp / 100m) * Math.Max(0m, 20m - 8m) / 20m;
        var r = f.Evaluate(Input(beforeArrival: 20, capacity: 8m,
            condition: 0.40m));
        Assert.Equal(expectedLoss, r.OverflowConditionLoss);
        Assert.Equal(0.40m - expectedLoss, r.ConditionAfter);
    }

    [Fact] public void SyntheticRepairEffectComesFromSuppliedCatalogue()
    {
        // R-HOUSE-001: catches repairPp replaced by the shipped literal 10m.
        const decimal repairPp = 25m;
        var c = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance ?? throw new InvalidOperationException("GDD 0.3 catalogue required");
        var row = c.Values["housing_repair"].Parts["condition_pp"];
        row.Values[0] = repairPp;
        row.Note = "TEST-ONLY SYNTHETIC repair effect: " + repairPp.ToString(CultureInfo.InvariantCulture);
        var f = Gdd03HousingFormula.FromCatalogue(c);
        var expected = Math.Min(1m, 0.40m + repairPp / 100m);
        Assert.Equal(expected, f.RepairCondition(0.40m));
    }

    [Fact] public void PreDeteriorationPriceAndBothLossesHaveWorkedNumbers()
    {
        // R-HOUSE-001, R-HOUSE-002: price first, deteriorate once, return arithmetic only.
        var r = Formula().Evaluate(Input());
        Assert.Equal(0.1m, r.SurchargeMoney);   // 10 x 0.10 x money_slope 0.1 (#1703: "Scale it to unit size (Recommended)", owner, 2 November 2026; was 1)
        Assert.Equal(0.025m, r.OverflowConditionLoss);
        Assert.Equal(0.375m, r.ConditionAfter);
        Assert.Equal(0m, r.CohesionLossPoints);
        Assert.Equal(2.5m, r.TotalConditionLossPp);
    }

    [Theory]
    [InlineData("0.499", "0.001")]   // 10 x 0.001 x money_slope 0.1 (#1703; was 0.01 at slope 1)
    [InlineData("0.50", "0")]
    [InlineData("0.60", "0")]
    public void SurchargeIsContinuous(string condition, string expected)
    {
        // R-HOUSE-001
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture),
            Formula().Evaluate(Input(condition: decimal.Parse(condition, CultureInfo.InvariantCulture))).SurchargeMoney);
    }

    [Fact] public void ExplicitBoundaryCountsAreNeverSubstitutedForEachOther()
    {
        // R-HOUSE-002, R-HOUSE-003: does not select residents or settle timing.
        var r = Formula().Evaluate(Input(surcharge: 10, beforeArrival: 20,
            cohesion: 5, population: 20, capacity: 15m, condition: 0.125m));
        Assert.Equal(0.375m, r.SurchargeMoney);   // 10 x 0.375 x money_slope 0.1 (#1703; was 3.75 at slope 1)
        Assert.Equal(0.0125m, r.OverflowConditionLoss);
        Assert.Equal(0.1125m, r.ConditionAfter);
        Assert.Equal(0.275m, r.CohesionLossPoints);
    }

    [Fact] public void CohesionUsesPostDeteriorationConditionAndOwnPopulation()
    {
        // R-HOUSE-002: no extra direct overflow penalty or indicator mutation.
        var f = Formula();
        var r = f.Evaluate(Input(population: 10, condition: 0.26m));
        Assert.Equal(0.235m, r.ConditionAfter);
        Assert.Equal(0.12m, r.CohesionLossPoints);
        var a = f.Evaluate(Input(condition: 0.125m, capacity: 10m));
        var b = f.Evaluate(Input(surcharge: 5, beforeArrival: 5, cohesion: 5,
            condition: 0m, capacity: 5m));
        Assert.Equal(1m, a.CohesionLossPoints + b.CohesionLossPoints);
    }

    [Fact] public void ZeroDenominatorsAndFullPaymentHaveNoLoss()
    {
        // R-HOUSE-002
        var r = Formula().Evaluate(Input(surcharge: 0, beforeArrival: 0, cohesion: 0,
            population: 0, capacity: 0m, condition: 0m));
        Assert.Equal(0m, r.OverflowConditionLoss);
        Assert.Equal(0m, r.CohesionLossPoints);
        Assert.Equal(0m, r.SurchargeMoney);
        Assert.Equal(0m, Formula().Evaluate(Input(beforeArrival: 0, cohesion: 0, population: 0,
            condition: 0m)).CohesionLossPoints);
    }

    [Fact] public void ConditionClampsAfterDeteriorationAndEachExplicitEffect()
    {
        // R-HOUSE-001: arithmetic only, no project, payment or initial condition.
        var f = Formula();
        Assert.Equal(0m, f.Evaluate(Input(condition: 0.01m)).ConditionAfter);
        Assert.Equal(1m, f.RepairCondition(0.95m));
        Assert.Equal(0m, f.ApplyConditionEffect(0.04m, -0.10m));
        Assert.Equal(0.90m, f.ApplyConditionEffect(f.RepairCondition(0.95m), -0.10m));
        Assert.Equal(0.14m, f.RepairCondition(0.04m));
    }

    [Theory]
    [InlineData(40, 20)]
    [InlineData(1, 0)]
    public void RegionalCohesionResidentsCannotExceedOwnPopulation(int residents, int population)
    {
        // R-HOUSE-002: a region's ordinary residents are part of total own population.
        Assert.Throws<ArgumentOutOfRangeException>(() => Formula().Evaluate(
            Input(cohesion: residents, population: population, condition: 0m)));
    }

    [Fact] public void InvalidExplicitInputsAreRejectedWithoutState()
    {
        var f = Formula();
        foreach (var input in new[] { Input(surcharge: -1), Input(beforeArrival: -1),
            Input(cohesion: -1), Input(population: -1), Input(capacity: -1m),
            Input(condition: -0.01m), Input(condition: 1.01m) })
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Evaluate(input));
        Assert.Throws<ArgumentNullException>(() => f.Evaluate(null!));
        Assert.Equal(0.1m, f.Evaluate(Input()).SurchargeMoney);   // money_slope 0.1 (#1703; was 1)
    }

    [Theory]
    [InlineData("he-IL")][InlineData("en-US")]
    public void CatalogueReaderAndArithmeticAreCultureIndependent(string culture)
    {
        var old = CultureInfo.CurrentCulture;
        var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal("0.375", Formula().Evaluate(Input()).ConditionAfter.ToString(CultureInfo.InvariantCulture));
        }
        finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = oldUi; }
    }

    [Theory]
    [InlineData("missing")][InlineData("deferred")][InlineData("shape")]
    [InlineData("zero")][InlineData("negative")][InlineData("basis")]
    public void ReaderRefusesMissingDeferredOrMalformedScalar(string failure)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance ?? throw new InvalidOperationException("GDD 0.3 catalogue required");
        var row = c.Values["housing.cohesion_threshold"];
        if (failure == "missing") c.Values.Remove("housing.cohesion_threshold");
        if (failure == "deferred") { row.Basis = "Deferred"; row.Values.Clear(); row.Expression = "unresolved"; }
        if (failure == "shape") row.Values.Add(1m); // TEST-ONLY SYNTHETIC invalid shape
        if (failure == "zero") row.Values[0] = 0m;
        if (failure == "negative") row.Values[0] = -1m;
        if (failure == "basis") row.Basis = "Unknown";
        Assert.Throws<ContentException>(() => Gdd03HousingFormula.FromCatalogue(c));
    }

    [Fact] public void ReaderSnapshotsCatalogueAndPreservesDefaultProvenance()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance ?? throw new InvalidOperationException("GDD 0.3 catalogue required");
        var f = Gdd03HousingFormula.FromCatalogue(c);
        // The shipped row is Agreed since the owner's 2 November 2026 ruling ("Yes, 5 pp under GDD 0.3 (Recommended)");
        // the reader carries whichever basis the row has, so a Default row still reads as Default.
        Assert.Equal("Agreed", f.OverflowBasis);
        var labelledDefault = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!;
        labelledDefault.Values["housing.overflow_deterioration"].Basis = "Default"; // TEST-ONLY SYNTHETIC provenance
        Assert.Equal("Default", Gdd03HousingFormula.FromCatalogue(labelledDefault).OverflowBasis);
        c.Values["housing.overflow_deterioration"].Values[0] = 99m; // TEST-ONLY SYNTHETIC mutation
        Assert.Equal(0.025m, f.Evaluate(Input()).OverflowConditionLoss);
        c.Ruleset = SaveGame.GddRuleset;
        Assert.Throws<ContentException>(() => Gdd03HousingFormula.FromCatalogue(c));
        Assert.Throws<ArgumentNullException>(() => Gdd03HousingFormula.FromCatalogue(null!));
    }

    // R-HOUSE-005 (#1703): the live step EvaluateTurn (double, the state type) is the decimal Evaluate with every resident
    // count equal and no upkeep (27M-17). Pins the two forms together over the shipped catalogue and each synthetic scalar
    // override above, every count, capacity and condition these tests use, and conditions 0.00 to 0.60 in 0.01 steps.
    [Fact] public void LiveDoubleStepAgreesWithTheDecimalFormula()
    {
        var formulas = new[]
        {
            Formula(),
            FormulaWithSyntheticScalar("housing.money_threshold", 0.70m),
            FormulaWithSyntheticScalar("housing.money_slope", 2.5m),
            FormulaWithSyntheticScalar("housing.cohesion_threshold", 0.50m),
            FormulaWithSyntheticScalar("housing.max_cohesion_loss", 3m),
            FormulaWithSyntheticScalar("housing.overflow_deterioration", 12.5m),
        };
        var conditions = new List<decimal> { 0m, 0.01m, 0.125m, 0.235m, 0.26m, 0.40m, 0.499m, 0.50m, 0.60m, 0.95m, 1m };
        for (int i = 0; i <= 60; i++) conditions.Add(i / 100m);
        var checkedCases = 0;
        foreach (var f in formulas)
        foreach (var population in new[] { 0, 10, 20, 511, 600 })
        foreach (var residents in new[] { 0, 1, 5, 10, 20, 248, 511 })
        foreach (var capacity in new[] { 0, 5, 8, 10, 15, 248 })
        foreach (var condition in conditions)
        {
            if (residents > population) continue;
            var d = f.Evaluate(new Gdd03HousingInput(residents, residents, residents, population, capacity, condition));
            var live = f.EvaluateTurn(residents, population, capacity, (double)condition);
            var at = $"R={residents} P={population} cap={capacity} c={condition}";
            Assert.True(Math.Abs((double)d.SurchargeMoney - live.Surcharge) <= 1e-9, "surcharge " + at);
            Assert.True(Math.Abs((double)d.ConditionAfter - live.ConditionAfter) <= 1e-9, "condition " + at);
            Assert.True(Math.Abs((double)d.CohesionLossPoints - live.Cohesion) <= 1e-9, "cohesion " + at);
            checkedCases++;
        }
        Assert.True(checkedCases > 10000, checkedCases.ToString(CultureInfo.InvariantCulture));
    }

    // R-HOUSE-001: a valid numeric child cannot authorize an unresolved parent repair row.
    [Fact]
    public void ReaderRejectsDeferredRepairParentWithAnIntactNumericPart()
    {
        var catalogue = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance
            ?? throw new InvalidOperationException("GDD 0.3 catalogue required");
        Gdd03HousingFormula.FromCatalogue(catalogue);
        catalogue.Values["housing_repair"].Basis = "Deferred"; // TEST-ONLY SYNTHETIC parent gate flip
        Assert.Throws<ContentException>(() => Gdd03HousingFormula.FromCatalogue(catalogue));
    }
}
