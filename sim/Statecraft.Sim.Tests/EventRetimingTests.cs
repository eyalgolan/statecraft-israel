using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The event windows re-timed for monthly turns at the cutover (docs/superpowers/specs/2026-10-14-event-timing-monthly.md,
/// #567, owner decisions of 15 October 2026), checked on the golden run (GoldenRun.Orders) from 15 May 1948 to the game's
/// end on 15 January 1952.</summary>
public class EventRetimingTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Every state of the golden run, the new game first.</summary>
    static readonly List<GameState> Run = Play();

    static List<GameState> Play()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.GameOver == null && run.Count < 1000) { s = Simulation.Step(C, s, GoldenRun.Orders(C, s)).State; run.Add(s); }
        return run;
    }

    /// <summary>The date the card for <paramref name="id"/> reaches the player: the date of the first state it has fired in
    /// (a card raised at the new game comes on 15 May 1948), or null.</summary>
    static DateTime? CardDate(string id) => Run.FirstOrDefault(s => s.FiredEvents.Contains(id))?.Date;

    /// <summary>#567 section 4 item 8: all 32 events fire in the historical monthly run (#567's 31 and the first new yishuv
    /// card, R-OBJ-003). Before the re-timing, sinai_withdrawal and rhodes_talks never fired (Horev's card came after both
    /// windows had closed).</summary>
    [Fact]
    public void EveryEventFiresInTheHistoricalMonthlyRun()
    {
        Assert.Equal(GameOverCodes.MvpEnded, Run[Run.Count - 1].GameOver);
        var missing = C.Events.Select(e => e.Id).Where(id => CardDate(id) == null).ToList();
        Assert.True(missing.Count == 0, "never fired: " + string.Join(", ", missing));
        Assert.Equal(32, C.Events.Count);
    }

    /// <summary>The card date of every event, from #567's tables (the "Proposed" column in the war period, "Monthly" after
    /// it). The re-timed windows (#567 section 4, edits 1 to 6, edit 2 by the owner's Q-CUT-7 answer) move idf_founding,
    /// altalena, jerusalem_military_government, latrun_proposal, operation_horev and elections_1949, and make
    /// sinai_withdrawal and rhodes_talks fire.</summary>
    // R-TURN-002
    [Theory]
    [InlineData("idf_founding", "1948-05-15")]                     // edit 1: the game's first day (was 15 June)
    [InlineData("first_truce", "1948-06-15")]                      // late by design, +5 days (owner, Q-CUT-3)
    [InlineData("altalena", "1948-06-15")]                         // edit 2 (owner, Q-CUT-7; was 15 July)
    [InlineData("ten_days", "1948-07-15")]                         // rewritten in PR 1a (#577)
    [InlineData("second_truce", "1948-07-15")]
    [InlineData("jerusalem_military_government", "1948-07-15")]    // edit 3 (was 15 August)
    [InlineData("latrun_proposal", "1948-09-15")]                  // edit 4 (was 15 October)
    [InlineData("bernadotte_assassination", "1948-10-15")]         // rewritten in PR 1a (#577)
    [InlineData("operation_yoav", "1948-10-15")]
    [InlineData("operation_hiram", "1948-11-15")]                  // rewritten in PR 1a (#577)
    [InlineData("jerusalem_ceasefire", "1948-12-15")]              // rewritten in PR 1a (#577)
    [InlineData("operation_horev", "1948-12-15")]                  // edit 5 (was 15 January 1949)
    [InlineData("sinai_withdrawal", "1949-01-15")]                 // fires through edit 5; rewritten in PR 1a (#577)
    [InlineData("rhodes_talks", "1949-01-15")]                     // fires through edit 5; late by design, +2 days
    [InlineData("elections_1949", "1949-01-15")]                   // edit 6 (was 15 February)
    [InlineData("armistice_egypt", "1949-03-15")]
    [InlineData("armistice_lebanon", "1949-04-15")]
    [InlineData("armistice_jordan", "1949-04-15")]
    [InlineData("austerity_1949", "1949-05-15")]
    [InlineData("first_new_yishuv", "1949-06-15")]                 // R-OBJ-003: the turn the reference player builds Mefalsim
    [InlineData("armistice_syria", "1949-08-15")]
    [InlineData("frumkin_commission", "1950-01-15")]
    [InlineData("maabarot_policy", "1950-04-15")]
    [InlineData("law_of_return", "1950-07-15")]
    [InlineData("clothing_rationing_1950", "1950-07-15")]
    [InlineData("ministry_supply_crisis", "1950-10-15")]
    [InlineData("education_registration_1951", "1950-12-15")]
    [InlineData("reparations_claim", "1951-03-15")]
    [InlineData("hula_drainage", "1951-03-15")]
    [InlineData("iraqi_exodus", "1951-03-15")]
    [InlineData("israel_bonds", "1951-05-15")]
    [InlineData("forex_crisis_1951", "1951-11-15")]
    public void EachEventFiresOnItsTurn(string id, string date) =>
        Assert.Equal(DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture), CardDate(id));

    /// <summary>The gap rule in months (owner, 15 October 2026: "Restate the rule in months", Q-CUT-8: "6 months, 2 in
    /// wartime"; docs/m3-rulings.md). In the historical run no more than 6 months pass from one card to the next, or from the
    /// last card to the game's end, and no more than 2 when the country is at full war on any turn of the stretch
    /// (SecuritySystem.AtFullWar, rung 5).</summary>
    // R-TURN-003
    [Fact]
    public void NoLongStretchWithoutACard()
    {
        var gaps = Gaps(Run);
        Assert.NotEmpty(gaps);
        var bad = gaps.Where(TooLong).Select(Describe).ToList();
        Assert.True(bad.Count == 0, "stretches without a card: " + string.Join("; ", bad));
        Assert.Contains(gaps, g => g.AtWar);   // the war period is measured, not skipped
    }

    public const int MaxMonths = 6, MaxMonthsAtWar = 2;

    /// <summary>R-TURN-003's limit for one stretch: 6 months, or 2 at full war.</summary>
    public static bool TooLong((DateTime From, DateTime To, int Months, bool AtWar) g) => g.Months > (g.AtWar ? MaxMonthsAtWar : MaxMonths);

    /// <summary>The owner's numbers (Q-CUT-8: "6 months, 2 in wartime"), pinned on both sides of each limit, and the real run's
    /// longest stretches, which sit exactly on them: a limit raised or lowered fails here.</summary>
    // R-TURN-003
    [Fact]
    public void TheLimitsAreSixMonthsAndTwoAtWar()
    {
        var d = new DateTime(1950, 1, 15);
        (DateTime, DateTime, int, bool) G(int months, bool war) => (d, d.AddMonths(months), months, war);
        Assert.False(TooLong(G(6, false)));
        Assert.True(TooLong(G(7, false)));
        Assert.False(TooLong(G(2, true)));
        Assert.True(TooLong(G(3, true)));
        var gaps = Gaps(Run);
        Assert.Equal(6, gaps.Where(g => !g.AtWar).Max(g => g.Months));   // 15 May to 15 November 1951
        Assert.Equal(2, gaps.Where(g => g.AtWar).Max(g => g.Months));    // 15 July to 15 September 1948, 15 January to 15 March 1949
    }

    public static List<(DateTime From, DateTime To, int Months, bool AtWar)> Gaps(List<GameState> run)
    {
        var cards = run.Where((s, i) => i == 0 ? s.FiredEvents.Count > 0 : s.FiredEvents.Count > run[i - 1].FiredEvents.Count).Select(s => s.Date).ToList();
        var end = run[run.Count - 1].Date;
        if (cards.Count == 0 || cards[cards.Count - 1] != end) cards.Add(end);
        var gaps = new List<(DateTime, DateTime, int, bool)>();
        for (int i = 1; i < cards.Count; i++)
        {
            var (a, b) = (cards[i - 1], cards[i]);
            var months = (b.Year - a.Year) * 12 + b.Month - a.Month;
            gaps.Add((a, b, months, run.Any(s => s.Date >= a && s.Date < b && SecuritySystem.AtFullWar(s))));
        }
        return gaps;
    }

    static string Describe((DateTime From, DateTime To, int Months, bool AtWar) g) =>
        FormattableString.Invariant($"{g.From:yyyy-MM-dd} to {g.To:yyyy-MM-dd}, {g.Months} months{(g.AtWar ? " at full war" : "")}");

    // R-TURN-003
    [Fact]
    public void TheGapRuleCatchesALongStretch()
    {
        DateTime D(int y, int m) => new DateTime(y, m, 15);
        GameState S(DateTime d, int rung, int fired) => new GameState { Date = d, Rung = rung, FiredEvents = new SortedSet<string>(Enumerable.Range(0, fired).Select(i => "e" + i)) };
        var peace = new List<GameState> { S(D(1950, 1), 2, 1) };
        for (int m = 2; m <= 8; m++) peace.Add(S(D(1950, m), 2, 1));
        peace.Add(S(D(1950, 9), 2, 2));   // a card after 8 months
        Assert.Equal(8, Assert.Single(Gaps(peace)).Months);
        var war = new List<GameState> { S(D(1948, 6), 5, 1), S(D(1948, 7), 5, 1), S(D(1948, 8), 5, 1), S(D(1948, 9), 5, 2) };
        var g = Assert.Single(Gaps(war));
        Assert.True(g.AtWar && g.Months == 3 && g.Months > MaxMonthsAtWar);
    }
}
