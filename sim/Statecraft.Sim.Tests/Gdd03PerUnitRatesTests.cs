using Statecraft.Sim;

// #2112 (owner, 1 November 2026: "Main's base x 0.3's tiers (Recommended)"): D2 keeps the census cities at 1,000 people a
// unit, so the gdd-0.3 tier rates are multiples of main's per-unit rate, as R-CRAFT-002 already does for craftsmen.
public sealed class Gdd03PerUnitRatesTests
{
    // R-CITY-002, R-CRAFT-002: one synthetic production step at the Historical census start gives main's pay for one
    // employed unit; the Industry tiers (ordinary or novice / apprentice / expert) are 1x / 2x / 3x of it.
    [Fact] public void IndustryTiersAreMultiplesOfTheCensusStartJobIncome()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        var r = c.RulesAt(s.Date).Mvp!;
        var yearFraction = SimTime.YearFraction(SecuritySystem.TurnLengthFor(c, s.Date, s.Rung));
        Assert.All(s.Cities.Values, x => Assert.Equal(0, x.Craftsmen));
        var employed = s.Cities.Values.Sum(x => x.Employed);
        Assert.True(employed > 0);
        var before = s.Treasury;
        Cities.Produce(c, s, r, yearFraction, new List<Cause>());
        var perUnit = (s.Treasury - before) / employed;
        Assert.Equal(r.IncomePerJobPerYear * yearFraction, perUnit, 12);

        var rates = Gdd03IndustryRates.Read(c.Gdd03Balance);
        Assert.Equal(1m, rates.For(CivilianTier.None)); Assert.Equal(1m, rates.For(CivilianTier.Novice));
        Assert.Equal(2m, rates.For(CivilianTier.Apprentice)); Assert.Equal(3m, rates.For(CivilianTier.Expert));
        Assert.Equal(perUnit, rates.MoneyPerTurn(CivilianTier.None, r, yearFraction), 12);
        Assert.Equal(perUnit, rates.MoneyPerTurn(CivilianTier.Novice, r, yearFraction), 12);
        Assert.Equal(2 * perUnit, rates.MoneyPerTurn(CivilianTier.Apprentice, r, yearFraction), 12);
        Assert.Equal(3 * perUnit, rates.MoneyPerTurn(CivilianTier.Expert, r, yearFraction), 12);
        // An apprentice earns what main's craftsman earns (R-CRAFT-002, craftsman_income_multiple 2).
        Assert.Equal(r.CraftsmanIncomeMultiple * perUnit, rates.MoneyPerTurn(CivilianTier.Apprentice, r, yearFraction), 12);
    }

    // R-FIELD-002, R-VET-002: the farmer tiers (novice / trained / veteran) are 2x / 3x / 5x field_food_per_turn, so they
    // follow main's base when it changes (test-only base of 2 below).
    [Theory]
    [InlineData(1, 2, 3, 5)]
    [InlineData(2, 4, 6, 10)]
    public void FarmerTiersAreMultiplesOfFieldFoodPerTurn(int baseFood, int novice, int trained, int veteran)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var r = c.RulesAt(c.Rules.Start).Mvp!;
        Assert.Equal(1, r.FieldFoodPerTurn);
        r.FieldFoodPerTurn = baseFood; // test-only base
        var rates = Gdd03FieldRates.FromCatalogue(c.Gdd03Balance!);
        Assert.Equal(novice, rates.FoodPerTurn(CivilianTier.Novice, r));
        Assert.Equal(trained, rates.FoodPerTurn(CivilianTier.Trained, r));
        Assert.Equal(veteran, rates.FoodPerTurn(CivilianTier.Veteran, r));
    }

    // #2112 review: the conversions fail closed, with no default base and no silent overflow.
    [Fact] public void ConversionsFailClosed()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var r = c.RulesAt(c.Rules.Start).Mvp!;
        var industry = Gdd03IndustryRates.Read(c.Gdd03Balance);
        Assert.Throws<ArgumentNullException>(() => industry.MoneyPerTurn(CivilianTier.Expert, null!, 1.0 / 12));
        Assert.Throws<ArgumentException>(() => industry.MoneyPerTurn((CivilianTier)99, r, 1.0 / 12));
        var fields = Gdd03FieldRates.FromCatalogue(c.Gdd03Balance!);
        Assert.Throws<ArgumentNullException>(() => fields.FoodPerTurn(CivilianTier.Trained, null!));
        Assert.Throws<ArgumentException>(() => fields.FoodPerTurn(CivilianTier.Apprentice, r));
        r.FieldFoodPerTurn = int.MaxValue; // test-only base
        Assert.Throws<OverflowException>(() => fields.FoodPerTurn(CivilianTier.Veteran, r));
        r.FieldFoodPerTurn = null; // test-only: the GDD loader requires it
        var ex = Assert.Throws<ContentException>(() => fields.FoodPerTurn(CivilianTier.Trained, r));
        Assert.Equal("mvp.field_food_per_turn", ex.Field);
    }
}
