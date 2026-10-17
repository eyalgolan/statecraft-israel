using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The decision events of the war period, 15 May 1948 to the Egyptian armistice (24 February 1949), sourced
/// from research_notes/M3 content 1950-51/facts-1948.md (playtest 1 E: about 40 weekly turns had no event).</summary>
public class WarEvents1948Tests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly DateTime Start = new DateTime(1948, 5, 15);
    static readonly DateTime EgyptArmistice = new DateTime(1949, 2, 24);

    /// <summary>The longest time, in months, from one card of the historical run to the next, up to the Egyptian armistice
    /// card. Controller ruling, fix round 1, I7, restated in months: owner decision of 15 October 2026 (Q-CUT-8, "6 months,
    /// 2 in wartime", #375).</summary>

    static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

    /// <summary>Id; the historical option; the security level it sets (null: unchanged); the responsible office and its
    /// holder; how many options; and DecidedBy, the date of the historical decision (facts-1948.md), which the card must
    /// reach the player by: the player decides during the turn that starts on the card's date.</summary>
    public static readonly (string Id, string Historical, int? Rung, string Office, string Holder, int Options, DateTime DecidedBy)[] Events =
    {
        ("idf_founding", "one_army", null, "defense_minister", "ben_gurion", 2, D(1948, 5, 26)),         // order signed (§3a)
        ("first_truce", "accept", null, "prime_minister", "ben_gurion", 2, D(1948, 6, 10)),              // acceptance sent (§1)
        ("altalena", "enforce", null, "defense_minister", "ben_gurion", 2, D(1948, 6, 20)),              // force authorised (§2)
        ("ten_days", "central_front", 5, "defense_minister", "ben_gurion", 2, D(1948, 7, 18)),           // the second truce ends the fighting (§4, §5)
        ("second_truce", "comply", 4, "prime_minister", "ben_gurion", 2, D(1948, 7, 18)),                // truce in force (§5)
        ("jerusalem_military_government", "military_government", null, "prime_minister", "ben_gurion", 2, D(1948, 8, 2)),
        ("bernadotte_assassination", "outlaw", null, "prime_minister", "ben_gurion", 2, D(1948, 9, 23)),  // ordinance adopted (§6)
        ("latrun_proposal", "accept_vote", null, "prime_minister", "ben_gurion", 2, D(1948, 9, 26)),     // cabinet vote (§13a)
        ("operation_yoav", "stop_at_ceasefire", 5, "defense_minister", "ben_gurion", 2, D(1948, 10, 22)), // Hebron refused, cease-fire (§7)
        ("operation_hiram", "stop_at_ceasefire", 5, "defense_minister", "ben_gurion", 2, D(1948, 10, 31)), // cease-fire (§8)
        ("jerusalem_ceasefire", "keep", null, "prime_minister", "ben_gurion", 2, D(1948, 11, 30)),       // signed (§13b)
        ("operation_horev", "launch", 5, "defense_minister", "ben_gurion", 2, D(1948, 12, 22)),          // launched (§9)
        ("sinai_withdrawal", "withdraw", null, "prime_minister", "ben_gurion", 2, D(1949, 1, 2)),        // withdrawn (§9, FRUS d371)
        ("rhodes_talks", "send_delegation", null, "foreign_minister", "sharett", 2, D(1949, 1, 13)),     // talks open (§9)
        ("elections_1949", "hold", null, "prime_minister", "ben_gurion", 1, D(1949, 1, 25)),             // the vote (§11)
    };

    /// <summary>Cards allowed to reach the player up to Days after DecidedBy, and why (owner decision, 15 October 2026, #567:
    /// on monthly turns no turn date falls between the fact each card states and the decision).</summary>
    static readonly Dictionary<string, (int Days, string Why)> LateByDesign = new Dictionary<string, (int, string)>
    {
        ["first_truce"] = (5, "no monthly turn date between the resolution of 29 May and the acceptance of 10 June"),
        ["rhodes_talks"] = (2, "it needs Horev answered, so it comes on 15 January, after the talks opened on 13 January"),
    };

    /// <summary>Cards that are decisions after the fact (owner decision, 15 October 2026, #567): their text states what
    /// happened, so the card must come on or after DecidedBy, the last fact it states.</summary>
    static readonly HashSet<string> AfterTheFact = new HashSet<string>
        { "bernadotte_assassination", "operation_hiram", "jerusalem_ceasefire", "sinai_withdrawal" };

    /// <summary>Follow-up events and the earlier choice they depend on (review I2).</summary>
    public static readonly (string Id, string Event, string Option)[] Chains =
    {
        ("ten_days", "first_truce", "accept"),
        ("latrun_proposal", "second_truce", "comply"),
        ("jerusalem_ceasefire", "jerusalem_military_government", "military_government"),
        ("sinai_withdrawal", "operation_horev", "launch"),
        ("rhodes_talks", "operation_horev", "launch"),
    };

    public static IEnumerable<object[]> Ids() => Events.Select(e => new object[] { e.Id });

    static GameEvent Get(string id) => C.Events.Single(e => e.Id == id);

    static DateTime LastDay(GameEvent e) => e.To!.Value.AddDays(-1);

    [Theory]
    [MemberData(nameof(Ids))]
    public void EachEventLoadsWithSourcesAndOneHistoricalOption(string id)
    {
        var e = Get(id);
        var row = Events.Single(x => x.Id == id);
        Assert.Contains(e.Status, new[] { "fact-checked", ReleaseGate.ReviewedStatus });   // sensitive ones carry the owner's sign-off (D11)
        Assert.True(e.Sources.Count >= 1, $"{id} has no source");
        Assert.All(e.Sources, s => { Assert.StartsWith("https://", s.Url); Assert.True(s.Checked >= new DateTime(2026, 10, 10), $"{id}: {s.Url} checked before the 10 October review"); });
        Assert.Equal(row.Office, e.Office);
        Assert.True(e.To.HasValue, $"{id} needs a closed window");
        Assert.True(e.From >= Start && e.From < EgyptArmistice, $"{id} opens {e.From:yyyy-MM-dd}, outside the war period");
        Assert.Equal(row.Options, e.Options.Count);
        var historical = Assert.Single(e.Options, o => o.Historical);
        Assert.Equal(row.Historical, historical.Id);
        Assert.True(historical.Recommended);
        Assert.Equal(row.Historical, e.Options[0].Id);   // the smoke run and the event card press the first option
        var chain = Chains.Where(x => x.Id == id).ToList();
        if (chain.Count == 0) Assert.Empty(e.Requires);
        else { var r = Assert.Single(e.Requires); Assert.Equal(chain[0].Event, r.Event); Assert.Equal(chain[0].Option, r.Option); }
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void PlaceholdersResolveOnEveryDayOfTheWindow(string id)
    {
        var e = Get(id);
        var row = Events.Single(x => x.Id == id);
        for (var d = e.From; d < e.To!.Value; d = d.AddDays(1))
        {
            Assert.DoesNotContain("{", Offices.Fill(C, e.Title, d));
            Assert.DoesNotContain("{", Offices.Fill(C, e.Text, d));
            foreach (var o in e.Options) Assert.DoesNotContain("{", Offices.Fill(C, o.Text, d));
        }
        Assert.Equal(row.Holder, Offices.Holder(C, e.Office, e.From)!.Id);
        Assert.Equal(row.Holder, Offices.Holder(C, e.Office, LastDay(e))!.Id);
    }

    /// <summary>Spec section 5.3: office holders are named only through {office} placeholders (review I4).</summary>
    [Theory]
    [MemberData(nameof(Ids))]
    public void NoOfficeHolderIsNamedInTheText(string id)
    {
        var e = Get(id);
        var texts = new[] { e.Title, e.Text }.Concat(e.Options.Select(o => o.Text)).ToList();
        var holders = C.People.Where(p => p.Offices.Any(t => t.From < e.To!.Value && (t.To == null || t.To > e.From))).ToList();
        foreach (var p in holders)
        {
            var surname = p.Name.Split(' ').Last();
            Assert.All(texts, t => Assert.DoesNotContain(surname, t));
        }
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheHistoricalOptionAppliesItsEffectsAndSecurityLevel(string id)
    {
        var e = Get(id);
        var row = Events.Single(x => x.Id == id);
        var option = e.Options.Single(o => o.Historical);
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        s.Date = e.From;
        SecuritySystem.SetRung(C, s, 3, new List<Cause>());   // a level no option sets, so any change is visible
        s.PendingEvents.Add(id);
        var before = C.Indicators.ToDictionary(i => i.Id, i => s.Indicators[i.Id]);
        Assert.Null(EventSystem.Choose(C, s, id, option.Id, new List<Cause>()));
        Assert.Equal(row.Rung ?? 3, s.Rung);
        Assert.Empty(option.SetPolicy);   // no policy fits these events (notes, schema reminders)
        foreach (var i in C.Indicators)
        {
            var expected = option.Effects.Where(f => f.Target == i.Id).Sum(f => f.Delta);
            Assert.Equal(before[i.Id] + expected, s.Indicators[i.Id], 9);
        }
    }

    sealed class Turn
    {
        public DateTime Start, End;
        public List<string> Raised = new List<string>();
        public List<Rejection> Rejected = new List<Rejection>();
        public int RungAfter;
    }

    /// <summary>Plays to March 1949 with HistoricalAI, except that <paramref name="pick"/> may choose another option.</summary>
    static List<Turn> Play(Func<string, string?>? pick = null)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        // The first entry is the new game: a card raised on 15 May 1948 (idf_founding) comes then.
        var turns = new List<Turn> { new Turn { Start = s.Date, End = s.Date, Raised = s.FiredEvents.ToList(), RungAfter = s.Rung } };
        while (s.Date < new DateTime(1949, 3, 1) && s.GameOver == null)
        {
            // A picked answer goes first, so it is never the card left to the minister when Attention runs out (on monthly
            // turns 15 July 1948 brings three cards and full war prices each at 2 of the 4).
            var picked = s.PendingEvents.Where(id => pick?.Invoke(id) != null).Select(id => (Command)new ChooseEventOption(id, pick!(id)!)).ToList();
            var commands = picked.Concat(HistoricalAI.Commands(C, s)
                .Where(c => !(c is ChooseEventOption o && picked.OfType<ChooseEventOption>().Any(p => p.EventId == o.EventId)))).ToList();
            var step = Simulation.Step(C, s, commands);
            turns.Add(new Turn
            {
                Start = s.Date, End = step.State.Date, Rejected = step.Rejected.ToList(), RungAfter = step.State.Rung,
                Raised = step.State.FiredEvents.Except(s.FiredEvents).ToList(),
            });
            s = step.State;
        }
        return turns;
    }

    static readonly List<Turn> Run = Play();

    /// <summary>The date of the card for <paramref name="id"/>: the end of the turn that raised it.</summary>
    static DateTime CardDate(string id) =>
        AtStart.Contains(id) ? Start
        : Run.FirstOrDefault(t => t.Raised.Contains(id))?.End ?? throw new Xunit.Sdk.XunitException($"{id} never fired in the historical run");

    /// <summary>The cards the new game raises on its first day (idf_founding opens on 15 May).</summary>
    static readonly HashSet<string> AtStart = new HashSet<string>(Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).FiredEvents);

    /// <summary>The card reaches the player in a turn that starts by the historical decision (review I1, I7).</summary>
    [Theory]
    [MemberData(nameof(Ids))]
    public void TheCardArrivesBeforeTheHistoricalDecision(string id)
    {
        var row = Events.Single(x => x.Id == id);
        var card = CardDate(id);
        var e = Get(id);
        Assert.True(card >= e.From && card < e.To!.Value.AddDays(31), $"{id}: card {card:yyyy-MM-dd} far from its window");
        if (AfterTheFact.Contains(id))
            Assert.True(card >= row.DecidedBy, $"{id}: card {card:yyyy-MM-dd}, before the facts of {row.DecidedBy:yyyy-MM-dd} it states");
        else if (LateByDesign.TryGetValue(id, out var late))
            Assert.True(card <= row.DecidedBy.AddDays(late.Days), $"{id}: card {card:yyyy-MM-dd}, more than {late.Days} days after {row.DecidedBy:yyyy-MM-dd} ({late.Why})");
        else
            Assert.True(card <= row.DecidedBy, $"{id}: card {card:yyyy-MM-dd}, after the historical decision of {row.DecidedBy:yyyy-MM-dd}");
    }

    /// <summary>Every card is answered by HistoricalAI on the turn it appears, and nothing is refused for attention
    /// (fix round 1: attention at or under the cap on every turn).</summary>
    [Fact]
    public void NoTurnBeforeTheArmisticeRefusesAnOrder()
    {
        foreach (var t in Run)
            Assert.True(t.Rejected.Count == 0, $"turn {t.Start:yyyy-MM-dd}: refused {string.Join("; ", t.Rejected.Select(r => r.Reason))}");
    }

    [Fact]
    public void TheHistoricalRunMovesThroughTheTruces()
    {
        int After(string id) => Run.Single(t => t.Start == CardDate(id) && t.End > t.Start).RungAfter;   // the turn answering the card
        Assert.Equal(4, After("second_truce"));       // the second truce: monthly turns
        Assert.Equal(5, After("operation_yoav"));     // Yoav ends it
        Assert.Equal(5, After("sinai_withdrawal"));   // full war until the armistice with Egypt
    }

    /// <summary>Two cards whose historical options set different security levels may come on the same turn on monthly turns
    /// (15 July 1948: ten_days at 5 and second_truce at 4). Answered in the card's order (window start, then id), the turn
    /// must end at the level of the decision taken last in history (on the same day, the card answered last), so the historical
    /// run keeps the historical level.</summary>
    [Fact]
    public void ATurnAskingForTwoSecurityLevelsEndsAtTheLaterDecisionsLevel()
    {
        for (int i = 0; i + 1 < Run.Count; i++)
        {
            var rows = Run[i].Raised.Select(id => Events.SingleOrDefault(x => x.Id == id)).Where(x => x.Id != null && x.Rung.HasValue).ToList();
            if (rows.Select(x => x.Rung).Distinct().Count() <= 1) continue;
            // Decided last in history; on the same day, the card answered last (the card order: window start, then id).
            var last = rows.OrderBy(x => x.DecidedBy).ThenBy(x => Get(x.Id).From).ThenBy(x => x.Id, StringComparer.Ordinal).Last();
            Assert.True(last.Rung == Run[i + 1].RungAfter, $"card {Run[i].End:yyyy-MM-dd}: the turn ends at level {Run[i + 1].RungAfter}, not {last.Rung} ({last.Id}, decided last)");
        }
    }

    /// <summary>R-TURN-003 in the war period: no more than 2 months pass from one card to the next while the country is at
    /// full war on any turn of the stretch (owner, 15 October 2026: "Full-war level = wartime", SecuritySystem.AtFullWar), the
    /// same rule and predicate as EventRetimingTests.NoLongStretchWithoutACard, on the golden run up to March 1949.</summary>
    // R-TURN-003
    [Fact]
    public void NoLongStretchWithoutACard()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.Date < new DateTime(1949, 3, 1) && s.GameOver == null) { s = Simulation.Step(C, s, GoldenRun.Orders(C, s)).State; run.Add(s); }
        var war = EventRetimingTests.Gaps(run).Where(g => g.AtWar).ToList();
        Assert.NotEmpty(war);
        var bad = war.Where(EventRetimingTests.TooLong).Select(g => $"{g.From:yyyy-MM-dd} to {g.To:yyyy-MM-dd}").ToList();
        Assert.True(bad.Count == 0, "stretches at full war without a card: " + string.Join("; ", bad));
    }

    /// <summary>A follow-up card never fires when the player chose otherwise earlier (review I2).</summary>
    [Fact]
    public void AFollowUpNeedsTheChoiceItDescribes()
    {
        foreach (var (id, required, option) in Chains)
        {
            var other = Get(required).Options.First(o => o.Id != option).Id;
            var run = Play(e => e == required ? other : null);
            Assert.True(run.Any(t => t.Raised.Contains(required)), $"{required} never fired");
            Assert.False(run.Any(t => t.Raised.Contains(id)), $"{id} fired although {required} was answered '{other}'");
        }
    }

    /// <summary>EventRules.SensitiveTags (internal to the sim): the loader refuses a money reward on these.</summary>
    static readonly HashSet<string> Sensitive = new HashSet<string> { "holocaust", "terror", "massacre", "war-casualties" };

    /// <summary>Events with fighting or terror carry the sensitive tags EventRules already enforces (no money reward).</summary>
    [Fact]
    public void CombatAndTerrorEventsAreTaggedSensitive()
    {
        foreach (var id in new[] { "altalena", "ten_days", "operation_yoav", "operation_hiram", "operation_horev" })
            Assert.Contains("war-casualties", Get(id).Tags);
        Assert.Contains("terror", Get("bernadotte_assassination").Tags);
        var tags = C.Events.SelectMany(e => e.Tags).Distinct().ToList();
        Assert.All(tags, t => Assert.True(t == "security" || Sensitive.Contains(t), $"tag '{t}' is neither 'security' nor a sensitive tag the loader enforces"));
    }

    /// <summary>Notes section 6: the killing costs standing whatever the answer (review I5).</summary>
    [Fact]
    public void BothAnswersToTheAssassinationShareTheStandingHit()
    {
        var e = Get("bernadotte_assassination");
        Assert.All(e.Options, o => Assert.Contains(o.Effects, f => f.Target == "intl_standing" && f.Delta == -0.05));
        Assert.All(e.Options, o => Assert.True(o.Effects.Where(f => f.Target == "intl_standing").Sum(f => f.Delta) < 0, $"{o.Id} raises standing"));
    }
}
