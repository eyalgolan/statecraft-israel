using System;
using Statecraft.Sim;

/// <summary>Fixed reference values for the generator and for the golden historical playthroughs. See docs/determinism.md.</summary>
public class DeterminismTests
{
    /// <summary>The first three outputs of Vigna's reference splitmix64.c (https://prng.di.unimi.it/splitmix64.c)
    /// for each seed, taken from the reference C code, not from this implementation.</summary>
    [Theory]
    [InlineData(0UL, 0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL)]
    [InlineData(1234567UL, 6457827717110365317UL, 3203168211198807973UL, 9817491932198370423UL)]
    public void SplitMix64MatchesTheReferenceVectors(ulong seed, ulong first, ulong second, ulong third)
    {
        var rng = new Rng(seed);
        Assert.Equal(first, rng.NextULong());
        Assert.Equal(second, rng.NextULong());
        Assert.Equal(third, rng.NextULong());
    }

    /// <summary>Any change to this value is a deliberate rules or content change: update it here, in
    /// GoldenRun.Hash1950 and in docs/determinism.md in the same commit.</summary>
    public const string GoldenHash = "71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07";

    /// <summary>Real content, seed 1948, Historical mode, HistoricalAI until the date reaches 1950-01-01.</summary>
    public static GameState GoldenPlaythrough(Content c) => GoldenRun.Play(c, GoldenRun.Until1950);

    [Fact]
    public void GoldenPlaythroughHashIsFixed()
    {
        var s = GoldenPlaythrough(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.Null(s.GameOver);
        Assert.True(s.Date >= GoldenRun.Until1950);
        Assert.Equal(GoldenHash, s.Hash());
    }

    /// <summary>Same rule for the second golden run, which covers the M3 content range (to 1952-01-01).</summary>
    public const string GoldenHash1952 = "62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8";

    [Fact]
    public void GoldenPlaythroughTo1952HashIsFixed()
    {
        var s = GoldenRun.Play(ContentLoader.Load(RealContentTests.ContentRoot()), GoldenRun.Until1952);
        Assert.Null(s.GameOver);
        Assert.Equal(new DateTime(1952, 2, 27), s.Date);
        Assert.Equal(GoldenHash1952, s.Hash());
    }

    /// <summary>The library constants Unity players compare against are the values these tests pin.</summary>
    [Fact]
    public void LibraryGoldenConstantsMatchThisTest()
    {
        Assert.Equal(GoldenHash, GoldenRun.Hash1950);
        Assert.Equal(GoldenHash1952, GoldenRun.Hash1952);
    }
}
