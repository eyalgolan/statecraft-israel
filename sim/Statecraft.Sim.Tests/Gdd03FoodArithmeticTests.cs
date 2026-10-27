using System;
using System.Globalization;
using System.Numerics;
using Statecraft.Sim;

/// <summary>Synthetic arithmetic probes only; no scenario, ledger, content or resolver fixture.</summary>
public class Gdd03FoodArithmeticTests
{
    // Pinned GDD D (see NOTES.md), docs/gdd/balance.md:
    // own ration 1 (:52), cohesion max 10 (:108), combat slope 1 (:109),
    // display max 100 (:17), storage max 1 (:18). No runtime fallback is introduced.
    static Gdd03FoodResult Settle(long available, long own, long provisions) =>
        Gdd03FoodArithmetic.Settle(available, own, provisions, 1, 10, 1, 100, 1);

    static void Fraction(long numerator, long denominator, Gdd03FoodFraction actual)
    {
        Assert.Equal(new BigInteger(numerator), actual.Numerator);
        Assert.Equal(new BigInteger(denominator), actual.Denominator);
    }

    // RED-FIRST R-FOOD-001, R-FOOD-002, R-FOOD-003: all counts/stocks below are test-only synthetic values.
    [Fact]
    public void SharedShortageKeepsExactSharesAndOneNationalLoss()
    {
        var result = Settle(2, 3, 2);
        Assert.Equal(new BigInteger(5), result.Need);
        Assert.Equal(2L, result.Consumed);
        Assert.Equal(0L, result.Remaining);
        Fraction(6, 5, result.OwnConsumed);
        Fraction(4, 5, result.ProvisionConsumed);
        Fraction(3, 5, result.UnmetShare);
        Fraction(6, 1, result.CohesionLossPoints);
        Fraction(3, 50, result.CohesionLossStored);
        Fraction(2, 5, result.NextOwnStrengthFactor);
    }

    // RED-FIRST R-FOOD-002, R-FOOD-003: no refill and replacement, not accumulation; synthetic sequence.
    [Fact]
    public void ConsecutiveCallsRetainOnlyStockAndReplaceTheFactor()
    {
        var first = Settle(2, 4, 0);
        var second = Settle(first.Remaining, 4, 0);
        var recovered = Settle(3, 4, 0);
        var fed = Settle(7, 4, 0);
        Assert.Equal(0L, second.Remaining);
        Fraction(1, 2, first.NextOwnStrengthFactor);
        Fraction(0, 1, second.NextOwnStrengthFactor);
        Fraction(1, 4, recovered.UnmetShare);
        Fraction(3, 4, recovered.NextOwnStrengthFactor);
        Fraction(1, 1, fed.NextOwnStrengthFactor);
        Assert.Equal(3L, fed.Remaining);
        // Repeating the same input neither stacks a penalty nor mutates earlier results.
        Fraction(1, 2, Settle(2, 4, 0).NextOwnStrengthFactor);
        Fraction(1, 2, first.NextOwnStrengthFactor);
    }

    // R-FOOD-001, R-FOOD-002, R-FOOD-003: test-only synthetic boundary cases, including each demand alone.
    [Theory]
    [InlineData(9, 0, 0, 0, 9, 0, 1)]
    [InlineData(0, 0, 0, 0, 0, 0, 1)]
    [InlineData(0, 3, 2, 0, 0, 1, 1)]
    [InlineData(5, 3, 2, 5, 0, 0, 1)]
    [InlineData(8, 3, 2, 5, 3, 0, 1)]
    [InlineData(1, 2, 0, 1, 0, 1, 2)]
    [InlineData(1, 0, 2, 1, 0, 1, 2)]
    public void Boundaries(long available, long own, long provisions, long consumed, long remaining,
        long unmetNumerator, long unmetDenominator)
    {
        var result = Settle(available, own, provisions);
        Assert.Equal(consumed, result.Consumed);
        Assert.Equal(remaining, result.Remaining);
        Fraction(unmetNumerator, unmetDenominator, result.UnmetShare);
        Assert.Equal(result.UnmetShare.Numerator * 10 * result.CohesionLossPoints.Denominator,
            result.CohesionLossPoints.Numerator * result.UnmetShare.Denominator);
        Assert.Equal(result.CohesionLossPoints.Numerator * result.CohesionLossStored.Denominator,
            result.CohesionLossStored.Numerator * 100 * result.CohesionLossPoints.Denominator);
        Assert.Equal((result.UnmetShare.Denominator - result.UnmetShare.Numerator)
            * result.NextOwnStrengthFactor.Denominator,
            result.NextOwnStrengthFactor.Numerator * result.UnmetShare.Denominator);
        // 10 and 100 above are catalogue values: D docs/gdd/balance.md:108,17.
        if (own == 0) Fraction(0, 1, result.OwnConsumed);
        if (provisions == 0) Fraction(0, 1, result.ProvisionConsumed);
        Assert.Equal(result.Consumed * result.OwnConsumed.Denominator * result.ProvisionConsumed.Denominator,
            result.OwnConsumed.Numerator * result.ProvisionConsumed.Denominator
            + result.ProvisionConsumed.Numerator * result.OwnConsumed.Denominator);
    }

    // R-FOOD-001, R-FOOD-003: test-only machine-boundary probes; no int/long overflow or fraction rounding.
    [Fact]
    public void LargeDemandAndProductsStayExact()
    {
        var result = Settle(long.MaxValue, long.MaxValue, long.MaxValue);
        Assert.Equal(new BigInteger(long.MaxValue) * 2, result.Need);
        Assert.Equal(long.MaxValue, result.Consumed);
        Assert.Equal(0L, result.Remaining);
        Fraction(long.MaxValue, 2, result.OwnConsumed);
        Fraction(long.MaxValue, 2, result.ProvisionConsumed);
        Fraction(1, 2, result.UnmetShare);
        Fraction(5, 1, result.CohesionLossPoints);
        Fraction(1, 20, result.CohesionLossStored);
        Fraction(1, 2, result.NextOwnStrengthFactor);
    }

    // R-FOOD-001, R-FOOD-002, R-FOOD-003: synthetic exhaustive small domain; conservation and symmetry.
    [Fact]
    public void SmallDomainConservesStockAndAllocatesTheSameFedShare()
    {
        for (long stock = 0; stock <= 8; stock++)
        for (long own = 0; own <= 5; own++)
        for (long provisions = 0; provisions <= 5; provisions++)
        {
            var result = Settle(stock, own, provisions);
            Assert.Equal(stock, result.Consumed + result.Remaining);
            Assert.Equal(Math.Min(stock, own + provisions), result.Consumed);
            var swapped = Settle(stock, provisions, own);
            Assert.Equal(result.OwnConsumed.Numerator, swapped.ProvisionConsumed.Numerator);
            Assert.Equal(result.OwnConsumed.Denominator, swapped.ProvisionConsumed.Denominator);
            var need = own + provisions;
            Assert.Equal(new BigInteger(need - result.Consumed) * result.UnmetShare.Denominator,
                result.UnmetShare.Numerator * need);
            Assert.Equal(result.OwnConsumed.Numerator * provisions * result.ProvisionConsumed.Denominator,
                result.ProvisionConsumed.Numerator * own * result.OwnConsumed.Denominator);
        }
    }

    // R-FOOD-001, R-FOOD-002: invalid synthetic inputs are refused; no clipping hides upstream errors.
    [Theory]
    [InlineData(-1, 0, 0, "available")]
    [InlineData(0, -1, 0, "ownUnits")]
    [InlineData(0, 0, -1, "provisionDemand")]
    public void RejectsNegativeInputs(long available, long own, long provisions, string parameter)
    {
        Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(
            () => Settle(available, own, provisions)).ParamName);
    }

    // R-FOOD-001, R-FOOD-003: every non-catalogue coefficient below is test-only synthetic.
    [Theory]
    [InlineData(0, 10, 1, 100, 1, "ownRation")]
    [InlineData(-1, 10, 1, 100, 1, "ownRation")]
    [InlineData(1, -1, 1, 100, 1, "cohesionMaximum")]
    [InlineData(1, 10, -1, 100, 1, "combatSlope")]
    [InlineData(1, 10, 1, 0, 1, "indicatorDisplayMaximum")]
    [InlineData(1, 10, 1, -1, 1, "indicatorDisplayMaximum")]
    [InlineData(1, 10, 1, 100, 0, "indicatorStorageMaximum")]
    [InlineData(1, 10, 1, 100, -1, "indicatorStorageMaximum")]
    public void RejectsInvalidBalanceInputs(long ration, long maximum, long slope, long display, long storage, string parameter)
    {
        Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() =>
            Gdd03FoodArithmetic.Settle(2, 3, 2, ration, maximum, slope, display, storage)).ParamName);
    }

    // R-FOOD-001, R-FOOD-003: synthetic coefficients prove explicit inputs are used, with no hidden balance fallback.
    [Fact]
    public void ExplicitBalanceInputsControlEveryDerivedQuantity()
    {
        var result = Gdd03FoodArithmetic.Settle(3, 2, 0, 3, 8, 3, 80, 2);
        Assert.Equal(new BigInteger(6), result.Need);
        Fraction(1, 2, result.UnmetShare);
        Fraction(4, 1, result.CohesionLossPoints);
        Fraction(1, 10, result.CohesionLossStored);
        Fraction(0, 1, result.NextOwnStrengthFactor); // max(0, 1 - 3/2)
        var zero = Gdd03FoodArithmetic.Settle(0, 1, 0, 1, 0, 0, 100, 1);
        Fraction(0, 1, zero.CohesionLossPoints);
        Fraction(1, 1, zero.NextOwnStrengthFactor);
    }

    // R-FOOD-003: fraction representation boundaries, all test-only synthetic operands.
    [Fact]
    public void FractionsNormalizeAndRejectInvalidOperands()
    {
        Fraction(2, 3, new Gdd03FoodFraction(6, 9));
        Fraction(0, 1, new Gdd03FoodFraction(0, 9));
        Assert.Equal("numerator", Assert.Throws<ArgumentOutOfRangeException>(
            () => new Gdd03FoodFraction(-1, 1)).ParamName);
        Assert.Equal("denominator", Assert.Throws<ArgumentOutOfRangeException>(
            () => new Gdd03FoodFraction(1, 0)).ParamName);
        Assert.Equal("denominator", Assert.Throws<ArgumentOutOfRangeException>(
            () => new Gdd03FoodFraction(1, -1)).ParamName);
    }

    // R-FOOD-003: no production code formats numbers; explicit invariant diagnostic in both locales.
    [Theory]
    [InlineData("he-IL")]
    [InlineData("en-US")]
    public void ArithmeticIsCultureIndependent(string locale)
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            var result = Settle(2, 3, 2);
            Assert.Equal("3/5", result.UnmetShare.Numerator.ToString(CultureInfo.InvariantCulture)
                + "/" + result.UnmetShare.Denominator.ToString(CultureInfo.InvariantCulture));
            Fraction(3, 50, result.CohesionLossStored);
            Fraction(2, 5, result.NextOwnStrengthFactor);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }
}
