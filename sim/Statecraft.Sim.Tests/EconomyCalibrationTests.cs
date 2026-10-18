using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>
/// Plays the historical choices to 1952 and compares the economy with sourced anchors (rules.yaml cites each).
///
/// ANCHOR INDEPENDENCE (as in CalibrationTests): rules.yaml derives or tunes a parameter from each of the first four
/// anchors, so they are CONSISTENCY checks that the derivation holds in play:
/// - internal debt rose IL 98.4m over 1950-51 (Michaely 1975, Table 5-2: 65.4 + 33.0): spending_per_capita is derived from it,
///   with the cities' job income included (owner, 16 October 2026, #821, option (a)): the gap over taxes is
///   (98.4m - 6m reserve call-up + 211 employed units x 0.06 x 2 years = 25.32m) / ~2.40m person-years = 0.00004905, so
///   spending = 0.0000545 + 0.00004905 = 0.0001036 (rounded); the run gives 98.7m. Before GDD Phase 3 PR 1 it was 0.000093
///   and the run gave 88.7m, the 9.6m of Tel Aviv's job income below the anchor;
/// - the FY 1950/51 ordinary budget was IL 60m (en.wiki "1950 in Israel"): tax_per_capita is derived from it and remains
///   alongside job income (owner, 18 October 2026, #884, the accepted per-head-tax draft);
/// - exports covered under a third of imports (spec section 3): exports and imports were chosen to satisfy it;
/// - foreign currency ran out in autumn 1951 (FRUS 1951 v.V doc 513): capital_inflow_per_year is tuned to it.
/// M3 has NO fully independent economy anchor. The one loose INDEPENDENT check: external debt rose IL 65.7m over 1950-51
/// (Michaely Table 5-2: 22.6 + 43.1), which nothing in rules.yaml was derived from (see its test for the tolerance).
/// M4 adds independent anchors (GNP +25% 1950 to 1951, the FY1951/52 instalments).
/// </summary>
public class EconomyCalibrationTests
{
    const double DebtTolerance = 0.15;
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly List<GameState> History = Play();

    static List<GameState> Play()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var h = new List<GameState> { s };
        while (s.Date <= new DateTime(1952, 1, 1) && s.GameOver == null)
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            h.Add(s);
        }
        return h;
    }

    /// <summary>A state value on a date, linearly interpolated between the two turns around it.</summary>
    static double On(DateTime date, Func<GameState, double> value)
    {
        var after = History.First(s => s.Date >= date);
        var before = History.Last(s => s.Date <= date);
        if (after.Date == before.Date) return value(after);
        var t = (date - before.Date).TotalDays / (after.Date - before.Date).TotalDays;
        return value(before) + t * (value(after) - value(before));
    }

    static string F(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    [Fact]
    public void ThePlaythroughSurvivesTo1952()
    {
        var last = History[History.Count - 1];
        // The game's own end (15 January 1952, R-LOOP-004) is the only way this run may stop.
        Assert.True(last.GameOver == null || last.GameOver == GameOverCodes.MvpEnded, "game over: " + last.GameOver);
        Assert.True(last.CoalitionStability > PoliticsSystem.CollapseBelow);
    }

    [Fact]
    public void BorrowingIn1950And1951MatchesTheRiseInInternalDebt()
    {
        const double michaely = 65.4 + 33.0;
        var rise = On(new DateTime(1952, 1, 1), s => s.Debt) - On(new DateTime(1950, 1, 1), s => s.Debt);
        Assert.True(Math.Abs(rise - michaely) / michaely <= DebtTolerance,
            $"[consistency: spending_per_capita_per_year is derived from this anchor] debt rose {F(rise)}m in 1950-51; Michaely: {F(michaely)}m");
    }

    [Fact]
    public void TaxesAtMidFiscalYear1950_51FundTheIl60mBudget()
    {
        var perYear = C.Rules.TaxPerCapitaPerYear * On(new DateTime(1950, 10, 1), s => s.TotalPopulation);
        Assert.True(Math.Abs(perYear - 60) / 60 <= DebtTolerance,
            $"[consistency: tax_per_capita_per_year is derived from this anchor] tax revenue {F(perYear)}m a year; budget IL 60m");
    }

    [Fact]
    public void ExportsCoverUnderAThirdOfImportsIn1950And1951()
    {
        foreach (var s in History.Where(s => s.Date.Year == 1950 || s.Date.Year == 1951))
        {
            var ratio = C.Rules.ExportsPerYear / (C.Rules.ImportsPerCapitaPerYear * s.TotalPopulation);
            Assert.True(ratio < 1.0 / 3, $"[consistency: exports and imports were chosen to satisfy this] {s.Date:yyyy-MM-dd}: exports cover {ratio:P0} of imports");
        }
    }

    /// <summary>[consistency: capital_inflow_per_year is tuned to this anchor] FRUS 1951 v.V doc 513 (19 Oct 1951): the
    /// crisis was acute in the last quarter of 1951, when about $45m of short-term credit fell due Oct-Dec and Israel
    /// asked for $15m of immediate aid. So: (1) reserves are not exhausted before the turn that covers 19 October;
    /// (2) at the end of that turn they cannot cover the $45m falling due; (3) they are exhausted no later than the turn
    /// that covers the end of 1951.</summary>
    [Fact]
    public void ForeignCurrencyRunsOutInTheLastQuarterOf1951()
    {
        const string kind = "[consistency: capital_inflow_per_year is tuned to this anchor]";
        var frus = new DateTime(1951, 10, 19);
        var i = History.FindIndex(s => s.ActiveSituations.Contains(EconomySystem.FxCrisis));
        Assert.True(i > 0, kind + " reserves never ran out");
        Assert.All(History.Take(i), s => Assert.True(s.FxReserves >= 0, $"{kind} {s.Date:yyyy-MM-dd}: fx {F(s.FxReserves)}"));
        Assert.True(History[i].Date > frus && History[i - 1].Date < new DateTime(1952, 1, 1),
            $"{kind} reserves ran out in the turn ending {History[i].Date:yyyy-MM-dd}; historically in the last quarter of 1951");
        var october = History.First(s => s.Date >= frus).FxReserves;
        Assert.True(october < 45, $"{kind} reserves at the turn covering 19 Oct 1951 are {F(october)} $m: enough to meet the $45m falling due");
    }

    /// <summary>[independent, loose] External debt rose IL 22.6m + 43.1m = 65.7m over 1950-51 (Michaely Table 5-2), which
    /// at the formal IL 0.36 per dollar is $182.5m. The model does not split loans from donations: its foreign currency
    /// from abroad (capital_inflow_per_year plus the event grants and bond sales in the run) is loans + donations, and
    /// donations (the UJA, transfers) are not debt. So Michaely's loans must be covered by the model's inflow
    /// (ratio >= 1), and the inflow must stay within three times the loans (ratio <= 3): the research notes give no
    /// donation total, so the band only guards the order of magnitude. Tightening it needs the donation series (M4).</summary>
    [Fact]
    public void ForeignInflowIsOfTheOrderOfTheRiseInExternalDebt()
    {
        const double externalDebtUsd = (22.6 + 43.1) / 0.36;
        double inflow = 0;
        for (int i = 1; i < History.Count; i++)
        {
            var (a, b) = (History[i - 1], History[i]);
            if (a.Date < new DateTime(1950, 1, 1) || a.Date >= new DateTime(1952, 1, 1)) continue;
            var yf = SimTime.YearFraction(SecuritySystem.TurnLengthFor(C, a.Date, a.Rung));
            var trade = C.Rules.ExportsPerYear * yf - C.Rules.ImportsPerCapitaPerYear * a.TotalPopulation * yf;
            inflow += (b.FxReserves - a.FxReserves) - trade;     // everything that reached the reserves except trade
        }
        var ratio = inflow / externalDebtUsd;
        Assert.True(ratio >= 1 && ratio <= 3,
            $"[independent, loose] foreign inflow {F(inflow)} $m in 1950-51 is {ratio:0.00}x the rise in external debt ({F(externalDebtUsd)} $m); expected 1-3x");
    }

    [Fact]
    public void ReservesGoHomeAfterTheArmistices()
    {
        var s = History.First(x => x.Date >= new DateTime(1950, 1, 1));
        Assert.Equal(2, s.Rung);
        Assert.Equal(C.Rules.RungMinMobilization[1], s.Mobilization);
    }
}
