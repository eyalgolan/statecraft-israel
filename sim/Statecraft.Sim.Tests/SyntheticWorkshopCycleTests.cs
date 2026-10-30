using System;
using System.Globalization;
using Statecraft.Sim;

// #1701 / R-LOOP-005: isolated synthetic workshop contract only; no production work or rule activation.
public class SyntheticWorkshopCycleTests
{
    static Gdd03BalanceCatalogue Catalogue() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!;
    static SyntheticWorkshopRules Rules() => SyntheticWorkshopRules.FromCatalogue(Catalogue());
    // IDs and invalid inputs below are test-only synthetic values, never authored facilities or people.
    static SyntheticWorkshopCycle Empty() => SyntheticWorkshopCycle.Empty(Rules(), "fixture-workshop");

    [Theory]
    [InlineData(SyntheticWorkshopTier.Ordinary, 5)]
    [InlineData(SyntheticWorkshopTier.Novice, 5)]
    [InlineData(SyntheticWorkshopTier.Apprentice, 4)]
    [InlineData(SyntheticWorkshopTier.Expert, 3)]
    public void CataloguePricesAndThreeStaffedTurns(SyntheticWorkshopTier tier, int price)
    {
        var c = Empty().WithWorker("fixture-person", tier);
        Assert.Equal((decimal)price, c.NextPrice);
        c = c.ReserveExplicitly(price);
        Assert.Equal(0, c.Progress);
        Assert.Equal((decimal)price, c.PaidCost);
        for (int i = 1; i < 3; i++)
        {
            c = c.StaffedTurn();
            Assert.Equal(i, c.Progress);
            Assert.Equal(0, c.CompletedArms);
        }
        c = c.StaffedTurn();
        Assert.Null(c.PaidCost);
        Assert.Null(c.PaidTier);
        Assert.Equal(0, c.Progress);
        Assert.Equal(1, c.CompletedArms);
        Assert.Same(c, c.StaffedTurn()); // no automatic restart or unpaid work
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void PauseAndReplacementPreservePaidCycle(int ticks)
    {
        var original = Empty().WithWorker("fixture-a", SyntheticWorkshopTier.Ordinary).ReserveExplicitly(5m);
        var c = original;
        for (int i = 0; i < ticks; i++) c = c.StaffedTurn();
        c = c.WithoutWorker();
        Assert.Same(c, c.StaffedTurn());
        Assert.Same(c, c.StaffedTurn());
        Assert.Equal(ticks, c.Progress);
        Assert.Equal(5m, c.PaidCost);
        Assert.Throws<InvalidOperationException>(() => c.ReserveExplicitly(5m));
        c = c.WithWorker("fixture-b", SyntheticWorkshopTier.Expert);
        Assert.Equal(ticks, c.Progress);
        Assert.Equal(5m, c.PaidCost);
        Assert.Equal(SyntheticWorkshopTier.Ordinary, c.PaidTier);
        Assert.Equal(3m, c.NextPrice);
        for (int remaining = 3 - ticks; remaining > 0; remaining--) c = c.StaffedTurn();
        Assert.Null(c.PaidCost);
        Assert.Equal(1, c.CompletedArms);
        c = c.ReserveExplicitly(3m);
        Assert.Equal(3m, c.PaidCost);
        Assert.Equal(SyntheticWorkshopTier.Expert, c.PaidTier);
        Assert.Equal(0, original.Progress); // immutable source
        Assert.Equal("fixture-a", original.WorkerId);
    }

    [Fact]
    public void ExpertToOrdinaryReplacementDoesNotRepriceCurrentCycle()
    {
        var c = Empty().WithWorker("fixture-expert", SyntheticWorkshopTier.Expert).ReserveExplicitly(3m).StaffedTurn();
        c = c.WithWorker("fixture-ordinary", SyntheticWorkshopTier.Ordinary);
        Assert.Equal(3m, c.PaidCost);
        Assert.Equal(1, c.Progress);
        c = c.StaffedTurn().StaffedTurn();
        Assert.Equal(1, c.CompletedArms);
        c = c.ReserveExplicitly(5m);
        Assert.Equal(5m, c.PaidCost);
        Assert.Equal(0, c.Progress);
    }

    [Fact]
    public void ExplicitReservationRequiresWorkerAndExactAcknowledgement()
    {
        var empty = Empty();
        Assert.Throws<InvalidOperationException>(() => empty.ReserveExplicitly(5m));
        Assert.Same(empty, empty.StaffedTurn());
        var c = empty.WithWorker("fixture-a", SyntheticWorkshopTier.Expert);
        // No Money or affordability policy here: reject malformed external reservation facts only.
        Assert.Throws<ArgumentException>(() => c.ReserveExplicitly(2m));
        Assert.Throws<ArgumentException>(() => c.ReserveExplicitly(4m));
        Assert.Null(c.PaidCost);
        var paid = c.ReserveExplicitly(3m);
        Assert.Throws<InvalidOperationException>(() => paid.ReserveExplicitly(3m));
        Assert.Equal(0, paid.Progress);
        Assert.Throws<ArgumentException>(() => c.WithWorker(" ", SyntheticWorkshopTier.Novice));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.WithWorker("fixture-a", (SyntheticWorkshopTier)99));
    }

    [Theory]
    [InlineData("he-IL")] [InlineData("en-US")]
    public void NumberTextUsesInvariantCulture(string locale)
    {
        var old = CultureInfo.CurrentCulture;
        var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            var c = Empty().WithWorker("fixture-a", SyntheticWorkshopTier.Expert).ReserveExplicitly(3m).StaffedTurn();
            Assert.Equal("1/3; paid=3; arms=0", c.ProgressText());
        }
        finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = oldUi; }
    }

    [Theory]
    [InlineData("workshop.staffed_turns")] [InlineData("workshop.cost")]
    public void MissingAndDeferredRulesHaveNoFallback(string key)
    {
        var c = Catalogue();
        c.Values.Remove(key);
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
        c = Catalogue();
        c.Values[key].Basis = "Deferred";
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
    }

    [Fact]
    public void MalformedRulesAreRejectedAndRulesSnapshotCatalogue()
    {
        var c = Catalogue();
        var rules = SyntheticWorkshopRules.FromCatalogue(c);
        c.Values["workshop.cost"].Values[0] = 99m; // test-only mutation
        Assert.Equal(5m, rules.Price(SyntheticWorkshopTier.Ordinary));
        c = Catalogue(); c.Values["workshop.staffed_turns"].Values[0] = 1.5m;
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
        c = Catalogue(); c.Values["workshop.cost"].Values.RemoveAt(0);
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
        c = Catalogue(); c.Values["workshop.cost"].Values[0] = 0m;
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
        c = Catalogue(); c.Values["workshop.cost"].Expression = "unknown";
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
        c = Catalogue(); c.Ruleset = SaveGame.GddRuleset;
        Assert.Throws<ArgumentException>(() => SyntheticWorkshopRules.FromCatalogue(c));
    }

    // R-LOOP-005: completing a fixture cycle keeps its staffed identity for the next explicit reservation.
    [Fact]
    public void CompletionKeepsTheWorkerForTheNextPaidCycle()
    {
        var cycle = Empty().WithWorker("fixture-a", SyntheticWorkshopTier.Apprentice).ReserveExplicitly(4m);
        cycle = cycle.StaffedTurn().StaffedTurn().StaffedTurn();
        Assert.Equal(1, cycle.CompletedArms);
        Assert.Null(cycle.PaidCost);
        Assert.Equal("fixture-workshop", cycle.FixtureFacilityId);
        Assert.Equal("fixture-a", cycle.WorkerId);
        Assert.Equal(SyntheticWorkshopTier.Apprentice, cycle.WorkerTier);
        cycle = cycle.ReserveExplicitly(4m);
        var next = cycle.StaffedTurn();
        Assert.NotSame(cycle, next);
        Assert.Equal(1, next.Progress);
        Assert.Equal(4m, next.PaidCost);
        Assert.Equal("fixture-workshop", next.FixtureFacilityId);
        Assert.Equal("fixture-a", next.WorkerId);
        Assert.Equal(2, next.StaffedTurn().StaffedTurn().CompletedArms);
    }
}
