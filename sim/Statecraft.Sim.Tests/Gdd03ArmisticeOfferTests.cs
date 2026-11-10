using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>#2478 (#1709 slice B), R-WAR-008, R-EVT-002 and R-ALT-008 under exact gdd-0.3: an armistice card with
/// `front_quiet` may come before its hard-anchor date, and again after a refusal, once its own front may have an early offer
/// (#2318's Armistice.MayOfferEarly: from the 15 December 1948 turn, military.armistice_quiet_turns quiet turns, two full
/// quiet turns after a refusal). On that path it is a soft event that takes the first soft slot, ahead of consequence cards
/// and the other soft events, within the cap of 3. Gdd03MovementFortTests' board and the real catalogue (2 quiet turns).</summary>
public class Gdd03ArmisticeOfferTests
{
    // The real cards' shape (content/events/armistice_*.yaml) with their own front. The dates are the fixture's: Egypt's
    // hard anchor is a year after the turns these tests play, Jordan's comes later still.
    static string Card(string front, string from) => $@"
id: armistice_{front}
title: Armistice with {front}
text: 'Israel decides whether to sign.'
office: prime_minister
anchor: hard
front_quiet: {front}
from: {from}
sources: [{{ url: https://example.org/event, checked: 2026-10-18 }}]
options:
  - id: sign
    text: Sign the armistice
    historical: true
  - id: hold_out
    text: Refuse and keep fighting
    alternative: {{ basis: design }}
";

    const string Parent = @"
id: parent
title: Parent
text: 'A real decision.'
from: 1948-12-01
to: 1948-12-11
sources: [{ url: https://example.org/event, checked: 2026-10-18 }]
options:
  - id: record
    text: What happened
    historical: true
  - id: road
    text: The road not taken
    alternative:
      basis: proposed
      by: the opposition benches
      date: 1948-12-05
      sources: [{ url: ""https://example.org/minutes"", checked: 2026-10-17 }]
";

    const string Consequence = @"
id: after_road
kind: consequence
title: After the road
text: 'What the record says was feared.'
from: 1948-12-11
to: 1949-02-01
requires: [{ event: parent, option: road }]
sources: [{ url: https://example.org/event, checked: 2026-10-18 }]
options:
  - id: hold
    text: Hold the course
    alternative:
      basis: debated
      by: the cabinet
      date: 1948-12-12
      sources: [{ url: ""https://example.org/cabinet"", checked: 2026-10-17 }]
";

    // An ordinary soft event whose `from` is earlier than every armistice's, so ordering by `from` alone would put it first.
    static string Soft(string id) => $"id: {id}\ntitle: {id}\ntext: 'Ordinary.'\nfrom: 1948-12-01\nto: 1949-02-01\nsources: [{{ url: \"https://example.org/event\", checked: 2026-10-18 }}]\noptions:\n  - id: ok\n    text: Ok\n";

    static Content Load(string ruleset = SaveGame.Gdd03Ruleset, string jordanFrom = "1950-01-01", bool others = true)
    {
        var f = Gdd03MovementFortTests.Files(ruleset);
        f["events/armistice_egypt.yaml"] = Card("egypt", "1949-12-01");
        f["events/armistice_jordan.yaml"] = Card("jordan", jordanFrom);
        if (others)
        {
            f["events/parent.yaml"] = Parent;
            f["events/after_road.yaml"] = Consequence;
            foreach (var id in new[] { "a1", "a2", "a3" }) f[$"events/{id}.yaml"] = Soft(id);
        }
        return ContentLoader.Load(Fixture.Write(f));
    }

    static GameState At(Content c, DateTime date, int egypt, int jordan)
    {
        var s = CombatTests.With(c);
        s.Date = date;
        s.QuietTurns["egypt"] = egypt; s.QuietTurns["jordan"] = jordan;
        s.FiredEvents.Add("parent");   // decided earlier; only after_road's requirement reads it
        s.ChosenOptions["parent"] = "record";
        return s;
    }

    static readonly DateTime December15 = new DateTime(1948, 12, 15);

    static List<string> Fire(Content c, GameState s)
    {
        EventSystem.Trigger(c, s, new List<Cause>(), s.Date.AddDays(-1));
        return s.PendingEvents.ToList();
    }

    // R-WAR-008, R-EVT-002 (owner, 27 October 2026, #1709: "The first offer can come on the 15 December 1948 turn, not
    // earlier"): quiet fronts get no early offer on the turn before; on the 15 December turn the card comes, ahead of the
    // ordinary soft events.
    [Fact]
    public void R_EVT_002_NoEarlyOfferBeforeTheFifteenthOfDecemberTurn()
    {
        var c = Load(others: false);
        Assert.Empty(Fire(c, At(c, new DateTime(1948, 12, 14), 5, 5)));
        Assert.Equal(new[] { "armistice_egypt", "armistice_jordan" }, Fire(c, At(c, December15, 5, 5)));
    }

    // R-WAR-008, R-WAR-005: the card reads its own front's count, so another front's quiet turns never open it.
    [Fact]
    public void R_EVT_002_AnotherFrontsQuietTurnsDoNotOpenTheCard()
    {
        var c = Load(others: false);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, At(c, December15, 2, 1)));
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, At(c, December15, 0, 7)));
    }

    // R-EVT-002, R-ALT-008 (Q-EVT-1, Q-OBJ-1): an early armistice takes the first soft slot, then the consequence card, then
    // the ordinary soft events by `from` and id, three in all.
    [Fact]
    public void R_EVT_002_R_ALT_008_AnEarlyArmisticeComesAheadOfConsequenceAndOrdinarySoftEvents()
    {
        var c = Load();
        var s = At(c, December15, 2, 0);
        s.ChosenOptions["parent"] = "road";
        Assert.Equal(new[] { "armistice_egypt", "after_road", "a1" }, Fire(c, s));
    }

    // R-EVT-002, R-ALT-008: two fronts eligible together take the first two slots in a stable order (the cards' `from`, then
    // the id, ordinal) and the cap of 3 still holds; with the same `from` the id decides.
    [Theory]
    [InlineData("1950-01-01", new[] { "armistice_egypt", "armistice_jordan", "after_road" })]
    [InlineData("1949-12-01", new[] { "armistice_egypt", "armistice_jordan", "after_road" })]
    [InlineData("1949-11-01", new[] { "armistice_jordan", "armistice_egypt", "after_road" })]
    public void R_EVT_002_R_ALT_008_TwoEligibleFrontsKeepAStableOrderAndTheCap(string jordanFrom, string[] expected)
    {
        var c = Load(jordanFrom: jordanFrom);
        var s = At(c, December15, 3, 3);
        s.ChosenOptions["parent"] = "road";
        var pending = Fire(c, s);
        Assert.Equal(expected, pending);
        Assert.Equal(EventSystem.MaxSoftEventsPerTurn, pending.Count);
        var again = At(c, December15, 3, 3);
        again.ChosenOptions["parent"] = "road";
        Assert.Equal(pending, Fire(c, again));
    }

    // R-EVT-001 (#2605), R-WAR-008: an early armistice is soft, so when hard anchors take every slot it waits with the other
    // soft events left out. It comes on the next turn its front is still quiet, ahead of them, and leaves the wait. If the
    // front was engaged meanwhile it stays out: the wait keeps a card past its window, not past its other gates.
    [Fact]
    public void R_EVT_001_AnEarlyArmisticeTheCapLeavesOutWaitsForItsQuietFront()
    {
        var c = Load();
        foreach (var id in new[] { "h1", "h2", "h3" })
            c.Events.Add(new GameEvent
            {
                Id = id, Title = id, Anchor = "hard", From = new DateTime(1948, 12, 1),
                Options = new List<EventOption> { new EventOption { Id = "ok", Text = "Ok" } },
            });
        var s = At(c, December15, 2, 0);
        Assert.Equal(new[] { "h1", "h2", "h3" }, Fire(c, s));
        Assert.Equal(new[] { "a1", "a2", "a3", "armistice_egypt" }, s.CappedEvents);

        var engaged = s.Clone();
        engaged.PendingEvents.Clear();
        engaged.Date = December15.AddDays(14);
        engaged.QuietTurns["egypt"] = 0;
        Assert.Equal(new[] { "a1", "a2", "a3" }, Fire(c, engaged));
        Assert.Contains("armistice_egypt", engaged.CappedEvents);

        var quiet = s.Clone();
        quiet.PendingEvents.Clear();
        quiet.Date = December15.AddDays(14);
        quiet.QuietTurns["egypt"] = 3;
        Assert.Equal(new[] { "armistice_egypt", "a1", "a2" }, Fire(c, quiet));
        Assert.Equal(new[] { "a3" }, quiet.CappedEvents);
    }

    // R-EVT-001, R-WAR-008: the sourced hard-anchor date stays the card's ordinary path: on or after it, a front that is not
    // quiet still gets its card, as a hard anchor that takes no soft slot.
    [Fact]
    public void R_EVT_001_TheHardAnchorDateStaysTheOrdinaryPath()
    {
        var c = Load();
        var s = At(c, new DateTime(1949, 12, 15), 0, 0);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));

        var early = At(c, new DateTime(1949, 1, 15), 2, 0);
        Assert.Equal(new[] { "armistice_egypt", "a1", "a2" }, Fire(c, early));   // early, it is soft: a3 waits
    }

    // R-TURN-002 (#2551), R-WAR-008, R-EVT-001: each real armistice card's `from` is now the 15th on or before its signing,
    // so on that turn a quiet front's card comes as a hard anchor: it cannot wait, it comes after the soft events of an
    // earlier `from`, and like every hard anchor it counts against the cap of 3, so the third soft event waits. Before #2551
    // its `from` was a turn later: on this turn the card came early and soft, ahead of the soft events (R-EVT-002). The card's
    // `from` is read from the real content; the rest is the fixture's.
    [Theory]
    [InlineData("egypt", "1949-02-15")]
    [InlineData("jordan", "1949-03-15")]
    [InlineData("lebanon", "1949-03-15")]
    [InlineData("syria", "1949-07-15")]
    public void R_TURN_002_OnTheTurnBeforeTheSigningAQuietFrontsCardComesAsAHardAnchor(string front, string turn)
    {
        var real = ContentLoader.Load(RealContentTests.ContentRoot()).Events.Single(e => e.Id == $"armistice_{front}");
        var f = Gdd03MovementFortTests.Files(SaveGame.Gdd03Ruleset);
        f[$"events/armistice_{front}.yaml"] = Card(front, real.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        foreach (var id in new[] { "s1", "s2", "s3" })
            f[$"events/{id}.yaml"] = $"id: {id}\ntitle: {id}\ntext: 'Ordinary.'\nfrom: 1949-01-01\nto: 1950-01-01\nsources: [{{ url: \"https://example.org/event\", checked: 2026-10-18 }}]\noptions:\n  - id: ok\n    text: Ok\n";
        var c = ContentLoader.Load(Fixture.Write(f));
        var s = CombatTests.With(c);
        s.Date = DateTime.ParseExact(turn, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        s.QuietTurns[front] = 5;
        Assert.Equal(new[] { "s1", "s2", $"armistice_{front}" }, Fire(c, s));
        Assert.Equal(new[] { "s3" }, s.CappedEvents);
    }

    // R-WAR-006, R-WAR-008 (Q49: "Two full quiet turns must follow the refusal"): a card refused on turn T does not come at
    // the end of T or T+1, and comes again at the end of T+2, once T+1 and T+2 were quiet; then signing ends it.
    [Fact]
    public void R_EVT_002_ARefusedOfferReturnsOnlyAfterTwoFullQuietTurns()
    {
        var c = Load(others: false);
        var s = CombatTests.With(c, new Unit(1, "recruit", 0, 1, side: "israel", strength: 6));
        s.Date = December15;
        s.QuietTurns["jordan"] = 5;
        s.FiredEvents.Add("armistice_egypt");
        s.ChosenOptions["armistice_egypt"] = "sign";   // Egypt is out of the way: a signed front gets no card
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));

        s = Turn(c, s, new ChooseEventOption("armistice_jordan", "hold_out"));
        Assert.Empty(s.PendingEvents);
        s = Turn(c, s);
        Assert.Empty(s.PendingEvents);
        s = Turn(c, s);
        Assert.Equal(new[] { "armistice_jordan" }, s.PendingEvents);
        s = Turn(c, s, new ChooseEventOption("armistice_jordan", "sign"));
        s = Turn(c, s);
        s = Turn(c, s);
        Assert.Empty(s.PendingEvents);
    }

    // R-WAR-006, R-WAR-008: a card refused on or after its date is not brought back by its date while its front is not
    // quiet, so it does not come every turn; it comes back once the front has had two quiet turns again.
    [Fact]
    public void R_EVT_002_ACardRefusedOnItsDateReturnsOnlyThroughTheQuietPath()
    {
        var c = Load(others: false);
        var s = At(c, new DateTime(1950, 2, 15), 2, 1);
        s.FiredEvents.Add("armistice_egypt"); s.ChosenOptions["armistice_egypt"] = "sign";
        s.FiredEvents.Add("armistice_jordan"); s.ChosenOptions["armistice_jordan"] = "hold_out";
        Assert.Empty(Fire(c, s));
        s.QuietTurns["jordan"] = 2;
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));
    }

    // R-WAR-008 (#2813): the rule gives a refused card's return no end date, so a quiet front brings it back after its `to`
    // as well. The real cards all end `to: 1950-01-01`; here Jordan's card (from 1949-07-20), refused,
    // must still come on the 15 February 1950 turn once its front has been quiet for two turns.
    [Fact]
    public void R_EVT_002_ARefusedCardReturnsAfterItsTo()
    {
        var f = Gdd03MovementFortTests.Files(SaveGame.Gdd03Ruleset);
        f["events/armistice_egypt.yaml"] = Card("egypt", "1949-12-01");
        f["events/armistice_jordan.yaml"] = Card("jordan", "1949-07-20").Replace("from: 1949-07-20", "from: 1949-07-20\nto: 1950-01-01");
        var c = ContentLoader.Load(Fixture.Write(f));
        var s = At(c, new DateTime(1950, 2, 15), 2, 1);
        s.FiredEvents.Add("armistice_egypt"); s.ChosenOptions["armistice_egypt"] = "sign";
        s.FiredEvents.Add("armistice_jordan"); s.ChosenOptions["armistice_jordan"] = "hold_out";
        Assert.Empty(Fire(c, s));
        s.QuietTurns["jordan"] = 2;
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));
    }

    // R-WAR-008, R-EVT-003 (owner, 4 November 2026, #2508, option A, "keep early signing"): in Historical mode an early card
    // left unanswered is still a hard anchor for delegation, so it signs on the 15 December 1948 turn, long before its date.
    [Fact]
    public void R_EVT_003_HistoricalModeSignsAnUnansweredEarlyCard()
    {
        var c = Load(others: false);
        var s = CombatTests.With(c, new Unit(1, "recruit", 0, 1, side: "israel", strength: 6));
        s.Mode = Mode.Historical;
        s.Date = December15;
        s.QuietTurns["egypt"] = 2;
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));

        var rng = s.RngState;
        s = Turn(c, s);
        Assert.Equal("sign", s.ChosenOptions["armistice_egypt"]);
        Assert.Equal(rng, s.RngState);   // the hard-anchor path: ordinary delegation would draw
    }

    static GameState Turn(Content c, GameState s, params Command[] orders)
    {
        var r = Simulation.Step(c, s, orders);
        Assert.Empty(r.Rejected);
        return r.State;
    }

    // R-WAR-008 is gdd-0.3 only: under gdd-0.1 the same quiet counts open nothing before the hard-anchor date.
    [Fact]
    public void Gdd01HasNoEarlyOffer()
    {
        var c = Load(SaveGame.GddRuleset);
        var s = At(c, December15, 5, 5);
        s.ChosenOptions["parent"] = "road";
        Assert.Equal(new[] { "after_road", "a1", "a2" }, Fire(c, s));
    }

    // ---- R-WAR-008's early text (#1709; owner, 9 November 2026: "Separate early text (Recommended)", then "Ordinary text
    // (Recommended)": no new saved state). A fixture text, never a real card's: the four real texts wait for the owner's
    // approval of the words and their sign-off ("You approve the words (Recommended)", "You, before they land (Recommended)").

    internal const string Early = "The talks came sooner. {prime_minister} must decide.";
    const string EarlyHe = "השיחות הגיעו מוקדם יותר. על {prime_minister} להחליט.";

    static string WithEarly(string card, string text = Early) => card.Replace("office: prime_minister", $"early_text: '{text}'\noffice: prime_minister");

    internal static Content LoadEarly(string ruleset = SaveGame.Gdd03Ruleset)
    {
        var f = Gdd03MovementFortTests.Files(ruleset);
        f["events/armistice_egypt.yaml"] = WithEarly(Card("egypt", "1949-12-01"));
        f["events/armistice_jordan.yaml"] = WithEarly(Card("jordan", "1950-01-01"));
        return ContentLoader.Load(Fixture.Write(f));
    }

    static string Filled(Content c, GameState s, string text) => Cabinet.Fill(c, s, text, s.Date);

    // R-WAR-008: a card shown before its `from`, which only the quiet path can do, shows its early text, its placeholder filled.
    // Red without it: the loader refused early_text, and Render gave the ordinary text.
    [Fact]
    public void R_WAR_008_AnEarlyCardShowsItsEarlyText()
    {
        var c = LoadEarly();
        var s = At(c, December15, 2, 0);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));
        var e = c.Events.Single(x => x.Id == "armistice_egypt");
        Assert.Equal(Filled(c, s, Early), EventSystem.Render(c, s, e));
        Assert.DoesNotContain("{", EventSystem.Render(c, s, e));
    }

    // R-WAR-008: the hard-anchor path. A front that never turns quiet gets its card on its `from`, with its ordinary text.
    [Fact]
    public void R_WAR_008_TheCardOnItsDateShowsItsOrdinaryText()
    {
        var c = LoadEarly();
        var s = At(c, new DateTime(1949, 12, 15), 0, 0);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));
        var e = c.Events.Single(x => x.Id == "armistice_egypt");
        Assert.Equal(Filled(c, s, "Israel decides whether to sign."), EventSystem.Render(c, s, e));
    }

    // R-WAR-008, "Ordinary text (Recommended)": a card refused on its date and brought back by the quiet path after its
    // `from` shows its ordinary text: the talks are no longer early. Nothing saved records the path.
    [Fact]
    public void R_WAR_008_ACardRefusedOnItsDateReturnsWithItsOrdinaryText()
    {
        var c = LoadEarly();
        var s = At(c, new DateTime(1950, 2, 15), 2, 2);
        s.FiredEvents.Add("armistice_egypt"); s.ChosenOptions["armistice_egypt"] = "sign";
        s.FiredEvents.Add("armistice_jordan"); s.ChosenOptions["armistice_jordan"] = "hold_out";
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));
        var e = c.Events.Single(x => x.Id == "armistice_jordan");
        Assert.Equal(Filled(c, s, "Israel decides whether to sign."), EventSystem.Render(c, s, e));
    }

    // R-WAR-008, R-WAR-006: a card refused early comes back after two full quiet turns, still before its `from`, with its
    // early text again.
    [Fact]
    public void R_WAR_008_AnEarlyRefusalReturnsEarlyWithTheEarlyText()
    {
        var c = LoadEarly();
        var s = CombatTests.With(c, new Unit(1, "recruit", 0, 1, side: "israel", strength: 6));
        s.Date = December15;
        s.QuietTurns["jordan"] = 5;
        s.FiredEvents.Add("armistice_egypt");
        s.ChosenOptions["armistice_egypt"] = "sign";
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));
        var e = c.Events.Single(x => x.Id == "armistice_jordan");
        Assert.Equal(Filled(c, s, Early), EventSystem.Render(c, s, e));

        s = Turn(c, s, new ChooseEventOption("armistice_jordan", "hold_out"));
        s = Turn(c, s);
        s = Turn(c, s);
        Assert.Equal(new[] { "armistice_jordan" }, s.PendingEvents);
        Assert.True(s.Date < e.From);
        Assert.Equal(Filled(c, s, Early), EventSystem.Render(c, s, e));
    }

    // R-WAR-008 is gdd-0.3 only: under gdd-0.1 the card never comes before its `from`, so its early text is never shown.
    [Fact]
    public void Gdd01NeverShowsTheEarlyText()
    {
        var c = LoadEarly(SaveGame.GddRuleset);
        Assert.Empty(Fire(c, At(c, December15, 5, 5)));
        var s = At(c, new DateTime(1949, 12, 15), 5, 5);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));
        Assert.Equal(Filled(c, s, "Israel decides whether to sign."), EventSystem.Render(c, s, c.Events.Single(x => x.Id == "armistice_egypt")));
    }

    // The loader (R-WAR-008): early text only on a `front_quiet` card, the only kind the quiet path can show before its
    // `from` (so EventSystem.ShowsEarly, which reads the date alone, never fires on a card that is never early); its Hebrew
    // exactly when the card has Hebrew, in Hebrew letters; its placeholders held from the first early offer
    // (Armistice.EarliestOffer, 15 December 1948), not only from the card's `from`.
    [Theory]
    [InlineData("no_front_quiet", "early_text", "front_quiet")]
    [InlineData("he_without_en", "early_text", "required: early_text_he is set but early_text is not")]
    [InlineData("he_missing", "early_text_he", "required: the card's text is in Hebrew too")]
    [InlineData("he_not_hebrew", "early_text_he", "must be Hebrew")]
    [InlineData("he_on_english_card", "early_text_he", "only a card with Hebrew text has early_text_he")]
    [InlineData("office_gap", "early_text", "has no holder on 1948-12-15")]
    public void TheLoaderRefusesAMisplacedOrIncompleteEarlyText(string fault, string field, string message)
    {
        var f = Gdd03MovementFortTests.Files();
        var card = Card("jordan", "1950-01-01");
        const string hebrew = "title_he: שביתת נשק עם ירדן\ntext_he: 'ישראל מחליטה אם לחתום.'\n";
        card = fault switch
        {
            "no_front_quiet" => WithEarly(card).Replace("front_quiet: jordan\n", ""),
            "he_without_en" => card.Replace("office: prime_minister", $"early_text_he: '{EarlyHe}'\noffice: prime_minister"),
            "he_missing" => WithHebrew(WithEarly(card)),
            "he_not_hebrew" => WithHebrew(WithEarly(card)).Replace("office: prime_minister", "early_text_he: 'Not Hebrew {prime_minister}.'\noffice: prime_minister"),
            "he_on_english_card" => WithEarly(card).Replace("office: prime_minister", $"early_text_he: '{EarlyHe}'\noffice: prime_minister"),
            "office_gap" => WithEarly(card, "The talks came sooner. {finance_minister} must decide."),
            _ => throw new ArgumentException(fault),
        };
        if (fault == "office_gap")   // a holder from 1949-01-01 only: inside the early window, before the card's `from`
            f["people.yaml"] = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1949-01-01");
        f["events/armistice_jordan.yaml"] = card;
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f)));
        Assert.Equal($"events[armistice_jordan].{field}", ex.Field);
        Assert.Contains(message, ex.Message);

        static string WithHebrew(string yaml) => yaml.Replace("office: prime_minister", hebrew + "office: prime_minister")
            .Replace("    text: Sign the armistice\n", "    text: Sign the armistice\n    text_he: לחתום על שביתת הנשק\n")
            .Replace("    text: Refuse and keep fighting\n", "    text: Refuse and keep fighting\n    text_he: לסרב ולהמשיך להילחם\n");
    }

    // The same card with every early-text rule met loads: the guard on each refusal above is the fault, not the shape.
    [Fact]
    public void TheLoaderTakesAFrontQuietCardWithItsEarlyTextInBothLanguages()
    {
        var f = Gdd03MovementFortTests.Files();
        f["events/armistice_jordan.yaml"] = WithEarly(Card("jordan", "1950-01-01")).Replace("office: prime_minister",
            $"title_he: שביתת נשק עם ירדן\ntext_he: 'ישראל מחליטה אם לחתום.'\nearly_text_he: '{EarlyHe}'\noffice: prime_minister")
            .Replace("    text: Sign the armistice\n", "    text: Sign the armistice\n    text_he: לחתום על שביתת הנשק\n")
            .Replace("    text: Refuse and keep fighting\n", "    text: Refuse and keep fighting\n    text_he: לסרב ולהמשיך להילחם\n");
        var c = ContentLoader.Load(Fixture.Write(f));
        // The Hebrew overlay (#1618) carries it: the snapshot writes it, and a cleared card gets it back from the set.
        var events = Translations.Write(HebrewOverlay.Snapshot(c)).Events;
        Assert.Contains("early_text: \"" + EarlyHe + "\"", events);
        HebrewOverlay.Clear(c);
        Assert.DoesNotContain("early_text", Translations.Write(HebrewOverlay.Snapshot(c)).Events);
        HebrewOverlay.Apply(c, Translations.Parse(Translations.Hebrew, null, events, null));
        Assert.Equal(events, Translations.Write(HebrewOverlay.Snapshot(c)).Events);
    }

    // The loader: `front_quiet` names one of Prisoners.Fronts, and only on that front's own card (armistice_<front>), whose
    // refusal resets that front's count (R-WAR-006).
    [Theory]
    [InlineData("iraq", "unknown front 'iraq'")]
    [InlineData("egypt", "armistice_egypt")]
    public void TheLoaderRefusesAFrontQuietThatIsNotTheCardsOwnFront(string front, string message)
    {
        var f = Gdd03MovementFortTests.Files();
        f["events/armistice_jordan.yaml"] = Card("jordan", "1950-01-01").Replace("front_quiet: jordan", "front_quiet: " + front);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f)));
        Assert.Equal("events[armistice_jordan].front_quiet", ex.Field);
        Assert.Contains(message, ex.Message);
    }
}
