using Statecraft.Sim;

// #2465: R-POP-004 and the Historical starting farmer-tier rule (docs/GDD.md, "Historical starting farmer tiers"):
// 1:2:1 novice : trained : veteran, largest remainder, ties to trained.
public class Gdd03FarmerTierSplitTests
{
    // Counts 0-8 (then 10, 11, 1001) written out by hand from the rule. Five farmers are 1/3/1 (docs/GDD.md). Counts of 2 mod 4 follow the
    // owner's ruling of 6 November 2026 on #2465, "Trained: 1/4/1 (Recommended)": 6 farmers are novice 1, trained 4, veteran 1.
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 1, 0)]
    [InlineData(2, 0, 2, 0)]
    [InlineData(3, 1, 1, 1)]
    [InlineData(4, 1, 2, 1)]
    [InlineData(5, 1, 3, 1)]
    [InlineData(6, 1, 4, 1)]
    [InlineData(7, 2, 3, 2)]
    [InlineData(8, 2, 4, 2)]
    [InlineData(10, 2, 6, 2)]
    [InlineData(11, 3, 5, 3)]
    [InlineData(1001, 250, 501, 250)]
    public void SplitsOneTwoOneByLargestRemainderWithTiesToTrained(int farmers, int novice, int trained, int veteran)
        => Assert.Equal((novice, trained, veteran), Gdd03FarmerTierSplit.Split(farmers));

    [Fact]
    public void TwoModFourGivesTheLeftoverToTrained()
    {
        // The owner's ruling of 6 November 2026 on #2465: "Trained: 1/4/1 (Recommended)".
        Assert.Equal((1, 4, 1), Gdd03FarmerTierSplit.Split(6));
        for (var n = 2; n <= 402; n += 4)
        {
            var (nov, tra, vet) = Gdd03FarmerTierSplit.Split(n);
            Assert.Equal((n - 2) / 4, nov); Assert.Equal((n - 2) / 4, vet); Assert.Equal((n + 2) / 2, tra);
        }
    }

    [Fact]
    public void EveryCountKeepsItsTotalAndMultiplesOfFourSplitExactly()
    {
        for (var n = 0; n <= 2000; n++)
        {
            var (nov, tra, vet) = Gdd03FarmerTierSplit.Split(n);
            Assert.Equal(n, nov + tra + vet);
            if (n % 4 == 0) Assert.Equal((n / 4, n / 2, n / 4), (nov, tra, vet));
        }
        Assert.Equal((int.MaxValue / 4 + 1, int.MaxValue / 2, int.MaxValue / 4 + 1), Gdd03FarmerTierSplit.Split(int.MaxValue));
    }

    [Fact]
    public void NegativeCountIsRefused() => Assert.Throws<System.ArgumentOutOfRangeException>(() => Gdd03FarmerTierSplit.Split(-1));
}
