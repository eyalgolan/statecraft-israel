using System;
using Statecraft.Sim;

/// <summary>The mvp-slice golden: pinned in golden/manifest.txt with its snapshot, like the scenario goldens.</summary>
public class MvpGoldenTests
{
    static readonly Content Mvp = MvpSlice.Load(RealContentTests.ContentRoot());

    [Fact]
    public void TheSliceMatchesItsGolden()
    {
        var s = MvpGolden.Play(Mvp);
        Assert.Equal(new DateTime(1949, 3, 15), s.Date);
        Assert.Equal(10, s.Turn);
        // R-SITE-001, R-SITE-003: the reference player's halutz group is ready on 15 September 1948, starts Mefalsim that turn
        // (borrowing the cost) and builds it at the end of the sixth turn, on 15 March 1949: the last turn the golden plays.
        Assert.Equal(new DateTime(1949, 3, 15), s.Sites["mefalsim"].BuiltOn);
        var expected = GoldenManifest.UpdateRequested ? GoldenManifest.Read().GetValueOrDefault("mvp-slice", "") : GoldenManifest.Expected("mvp-slice");
        GoldenSnapshot.Check("mvp-slice", s, expected);
    }

    [Fact]
    public void TheSliceIsNotTheLegacyRun()
    {
        var legacy = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(legacy, GoldenRun.Seed, Mode.Historical);
        while (s.Date < MvpGolden.Until) s = Simulation.Step(legacy, s, HistoricalAI.Commands(legacy, s)).State;
        Assert.NotEqual(s.Hash(), MvpGolden.Play(Mvp).Hash());
    }
}
