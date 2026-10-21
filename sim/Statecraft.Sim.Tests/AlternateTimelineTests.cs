using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The alternate-timeline mechanism (docs/superpowers/plans/2026-10-17-alternate-timelines.md).</summary>
public class AlternateTimelineTests
{
    internal const string Parent = @"
id: parent
title: Parent
text: 'A real decision.'
from: 1948-06-01
to: 1948-06-11
options:
  - id: record
    text: What happened
    historical: true
  - id: road
    text: The road not taken
    alternative:
      basis: proposed
      by: the opposition benches
      date: 1948-06-05
      sources: [{ url: ""https://example.org/minutes"", checked: 2026-10-17 }]
";

    internal static Content Load(params (string file, string yaml)[] events) =>
        ContentLoader.Load(Fixture.Write(events: events.ToDictionary(x => x.file, x => x.yaml)));

    static OptionAlternative Road(Content c) => c.Events.Single().Options.Single(o => o.Id == "road").Alternative!;

    // R-ALT-003
    [Fact]
    public void TheAlternativeRecordIsReadFromYaml()
    {
        var a = Road(Load(("parent.yaml", Parent)));
        Assert.Equal("proposed", a.Basis);
        Assert.Equal("the opposition benches", a.By);
        Assert.Equal(new DateTime(1948, 6, 5), a.Date);
        Assert.Equal("https://example.org/minutes", Assert.Single(a.Sources).Url);
        Assert.Equal(new DateTime(2026, 10, 17), a.Sources[0].Checked);
        Assert.True(a.IsConsidered);
        Assert.Null(Load(("parent.yaml", Parent)).Events.Single().Options.Single(o => o.Id == "record").Alternative);
    }

    // R-ALT-003
    [Theory]
    [InlineData("basis: proposed", "basis: maybe", "alternative.basis", "must be proposed, debated or design")]
    [InlineData("by: the opposition benches", "by: ''", "alternative.by", "a proposed or debated alternative names who put it forward")]
    [InlineData("by: the opposition benches", "by: '  '", "alternative.by", "a proposed or debated alternative names who put it forward")]
    [InlineData("by: the opposition benches", "by: '{prime_minister}'", "alternative.by", "words only, no placeholder")]
    [InlineData("      date: 1948-06-05\n", "", "alternative.date", "needs the date it was put forward")]
    [InlineData("sources: [{ url: \"https://example.org/minutes\", checked: 2026-10-17 }]", "sources: []", "alternative.sources", "needs at least one source")]
    [InlineData("https://example.org/minutes", "http://example.org/minutes", "alternative.sources", "needs an https url and a checked date")]
    [InlineData(", checked: 2026-10-17 }]", " }]", "alternative.sources", "needs an https url and a checked date")]
    public void ABadAlternativeIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent.Replace(from, to))));
        Assert.Equal("events/parent.yaml", ex.File);
        Assert.Equal("events[parent].options[road]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-ALT-003
    [Fact]
    public void ADebatedAlternativeLoadsLikeAProposedOne()
    {
        var a = Road(Load(("parent.yaml", Parent.Replace("basis: proposed", "basis: debated"))));
        Assert.Equal("debated", a.Basis);
        Assert.True(a.IsConsidered);
    }

    // R-ALT-003
    [Fact]
    public void ADesignAlternativeNamesNobody()
    {
        var design = Parent.Replace("basis: proposed", "basis: design");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", design)));
        Assert.Equal("events[parent].options[road].alternative.by", ex.Field);
        Assert.Contains("a design alternative names nobody and no date", ex.Message);
        var dated = design.Replace("      by: the opposition benches\n", "");
        var ex2 = Assert.Throws<ContentException>(() => Load(("parent.yaml", dated)));
        Assert.Equal("events[parent].options[road].alternative.date", ex2.Field);
        Assert.Contains("a design alternative names nobody and no date", ex2.Message);
        var bare = design.Replace("      by: the opposition benches\n      date: 1948-06-05\n", "");
        Assert.False(Road(Load(("parent.yaml", bare))).IsConsidered);
        var plain = Parent.Substring(0, Parent.IndexOf("      basis:", StringComparison.Ordinal)) + "      basis: design\n";
        Assert.Empty(Road(Load(("parent.yaml", plain))).Sources);   // sources are optional for a design alternative
        var badSource = plain + "      sources: [{ url: \"http://example.org\", checked: 2026-10-17 }]\n";
        var ex3 = Assert.Throws<ContentException>(() => Load(("parent.yaml", badSource)));   // but any it gives must be valid
        Assert.Equal("events[parent].options[road].alternative.sources", ex3.Field);
    }

    // R-ALT-003
    [Fact]
    public void TheHistoricalOptionHasNoAlternativeRecord()
    {
        var bad = Parent.Replace("    historical: true\n", "    historical: true\n    alternative: { basis: design }\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", bad)));
        Assert.Equal("events[parent].options[record].alternative", ex.Field);
        Assert.Contains("the historical option is the record, not an alternative", ex.Message);
    }

    [Fact]
    // R-ALT-004
    public void AnAlternativeWithoutARecordIsALoadError()
    {
        var bare = Parent.Substring(0, Parent.IndexOf("    alternative:", StringComparison.Ordinal));
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", bare)));
        Assert.Equal("events[parent].options[road].alternative", ex.Field);
        Assert.Contains("an option that did not happen says who proposed it, or basis: design", ex.Message);
    }

    // ---- Task 5 (#950): consequence cards ------------------------------------------------------------------------

    internal const string Consequence = @"
id: after_road
kind: consequence
title: After the road
text: 'What the record says was feared.'
from: 1948-06-15
to: 1948-08-15
requires: [{ event: parent, option: road }]
options:
  - id: hold
    text: Hold the course
    alternative:
      basis: debated
      by: the cabinet
      date: 1948-06-20
      sources: [{ url: ""https://example.org/cabinet"", checked: 2026-10-17 }]
";

    static string WithRejoin => Consequence.Replace("    text: Hold the course\n", "    text: Hold the course\n    rejoin: parent\n");

    internal const string RecordFollowUp = @"
id: on_record
title: On the record
text: 'The next real decision.'
from: 1948-07-01
to: 1948-09-15
requires: [{ event: parent, option: record }]
options:
  - id: ok
    text: Ok
";

    [Fact]
    // R-ALT-009
    public void ARejoinReopensOnlyCardsStillInsideTheirWindow()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin), ("on_record.yaml", RecordFollowUp));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road"; s.FiredEvents.Add("parent");
        s.Date = new DateTime(1948, 7, 15);
        EventSystem.Trigger(c, s, new List<Cause>(), new DateTime(1948, 6, 15));
        Assert.DoesNotContain("on_record", s.FiredEvents);                 // off the record: the follow-up waits
        Assert.Null(EventSystem.Choose(c, s, "after_road", "hold", new List<Cause>()));
        Assert.True(Timeline.Rejoined(c, s, "parent"));
        EventSystem.Trigger(c, s, new List<Cause>(), new DateTime(1948, 6, 15));
        Assert.Contains("on_record", s.FiredEvents);                       // its window is still open

        var late = NewState(c);
        late.ChosenOptions["parent"] = "road"; late.ChosenOptions["after_road"] = "hold";
        late.FiredEvents.Add("parent"); late.FiredEvents.Add("after_road");
        late.Date = new DateTime(1948, 10, 15);
        EventSystem.Trigger(c, late, new List<Cause>(), new DateTime(1948, 9, 15));
        Assert.DoesNotContain("on_record", late.FiredEvents);             // its window closed on 15 September: no resurrection
    }

    [Fact]
    // R-ALT-009
    public void TheTimelineKeepsTheRoadAfterARejoin()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road";
        Assert.False(Timeline.Rejoined(c, s, "parent"));                     // no rejoin until the card's option is chosen
        Assert.False(Timeline.Met(c, s, new EventRequirement { Event = "parent", Option = "record" }));
        s.ChosenOptions["after_road"] = "hold";
        Assert.False(Timeline.Rejoined(c, s, "after_road"));                 // only the event named by `rejoin`
        Assert.Equal("road", s.ChosenOptions["parent"]);                   // the record of the road is not rewritten
        Assert.True(Timeline.Diverged(c, s, "parent"));
        Assert.True(Timeline.Met(c, s, new EventRequirement { Event = "parent", Option = "record" }));
        Assert.True(Timeline.Met(c, s, new EventRequirement { Event = "parent", Option = "road" }));   // and the road itself still holds
        Assert.False(Timeline.Met(c, s, new EventRequirement { Event = "parent", Option = "elsewhere" }));   // only the record's own option
    }

    // R-ALT-009
    [Theory]
    [InlineData("rejoin: parent", "rejoin: nowhere", "rejoin", "unknown event 'nowhere'")]
    [InlineData("rejoin: parent", "rejoin: after_road", "rejoin", "a card cannot rejoin itself")]
    public void ABadRejoinIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin.Replace(from, to))));
        Assert.Equal("events[after_road].options[hold]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    // R-ALT-009
    public void OnlyAConsequenceCardRejoinsAndOnlyARoadItFollows()
    {
        var other = "id: other\ntitle: Other\ntext: x\nfrom: 1948-06-01\noptions:\n  - id: a\n    text: A\n    historical: true\n  - id: b\n    text: B\n    alternative: { basis: design }\n";
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("other.yaml", other), ("after_road.yaml", WithRejoin.Replace("rejoin: parent", "rejoin: other"))));
        Assert.Contains("rejoins only an event whose road this card follows", ex.Message);
        var onRecord = RecordFollowUp.Replace("    text: Ok\n", "    text: Ok\n    rejoin: parent\n");
        var ex2 = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("on_record.yaml", onRecord)));
        Assert.Contains("only a consequence card rejoins", ex2.Message);
        // A consequence card has no historical option, so a card that follows one has no record to rejoin there.
        var deeper = Consequence.Replace("id: after_road", "id: deeper").Replace("{ event: parent, option: road }", "{ event: after_road, option: hold }")
            .Replace("    text: Hold the course\n", "    text: Hold the course\n    rejoin: after_road\n");
        var ex3 = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("deeper.yaml", deeper)));
        Assert.Equal("events[deeper].options[hold].rejoin", ex3.Field);
        Assert.Contains("which has a historical option", ex3.Message);
    }

    [Fact]
    // R-ALT-009, R-EVT-005: the card's effect list names the way back.
    public void TheEffectListNamesTheRejoin()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin));
        var o = c.Events.Single(x => x.Id == "after_road").Options.Single();
        var line = Assert.Single(EventSystem.OptionEffects(c, NewState(c), o));
        Assert.Equal(("rejoin", 0.0), (line.Target, line.Delta));
        Assert.Equal("Returns to the recorded course: Parent", line.Message);
    }

    const string RecordToo = "options:\n  - id: record_too\n    text: As recorded\n    historical: true\n";

    static ContentException LoadError(string consequence) =>
        Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", consequence)));

    // R-ALT-005
    [Fact]
    public void AConsequenceCardLoadsWithItsKind()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence));
        Assert.Equal(EventKinds.Consequence, c.Events.Single(e => e.Id == "after_road").Kind);
        Assert.Equal(EventKinds.Historical, c.Events.Single(e => e.Id == "parent").Kind);   // the default
    }

    // R-ALT-005
    [Theory]
    [InlineData("requires: [{ event: parent, option: road }]\n", "", "requires", "a consequence card follows a road not taken")]
    [InlineData("option: road", "option: record", "requires", "a consequence card follows a road not taken")]
    [InlineData("options:\n", RecordToo, "options", "a consequence card has no historical option")]
    [InlineData("kind: consequence", "kind: sideways", "kind", "must be historical, consequence or reconvergence")]
    public void AConsequenceCardHasItsShape(string from, string to, string field, string message)
    {
        var ex = LoadError(Consequence.Replace(from, to));
        Assert.Equal("events/after_road.yaml", ex.File);
        Assert.Equal("events[after_road]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-ALT-005
    [Fact]
    public void AConsequenceCardIsSoft()
    {
        // A hard anchor needs a historical option (EventRules), so the fixture gives it one; the soft rule is checked first.
        var ex = LoadError(Consequence.Replace("kind: consequence\n", "kind: consequence\nanchor: hard\n").Replace("options:\n", RecordToo));
        Assert.Equal("events[after_road].anchor", ex.Field);
        Assert.Contains("a consequence card is soft", ex.Message);
    }

    // R-ALT-005
    [Fact]
    public void DivergedCountsAsARoadNotTaken()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence.Replace("option: road", "diverged: true")));
        Assert.Single(c.Events.Single(e => e.Id == "after_road").Requires);
    }

    // R-ALT-005
    [Fact]
    public void EveryOptionOfAConsequenceCardHasARecord()
    {
        var ex = LoadError(Consequence.Substring(0, Consequence.IndexOf("    alternative:", StringComparison.Ordinal)));
        Assert.Equal("events[after_road].options[hold].alternative", ex.Field);
        Assert.Contains("every option of a consequence card says who proposed it", ex.Message);
    }

    static string DesignParent => Parent.Replace("      basis: proposed\n      by: the opposition benches\n      date: 1948-06-05\n      sources: [{ url: \"https://example.org/minutes\", checked: 2026-10-17 }]\n", "      basis: design\n");

    // R-ALT-006
    [Theory]
    [InlineData("option: road")]
    [InlineData("diverged: true")]
    public void NoConsequenceFollowsADesignAlternative(string entry)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", DesignParent), ("after_road.yaml", Consequence.Replace("option: road", entry))));
        Assert.Equal("events[after_road].requires[0]", ex.Field);
        Assert.Contains("follows 'parent/road', which nobody is recorded proposing (basis: design)", ex.Message);
    }

    static string Chained(string id, string parent, string option) => Consequence
        .Replace("id: after_road", "id: " + id)
        .Replace("requires: [{ event: parent, option: road }]", $"requires: [{{ event: {parent}, option: {option} }}]");

    // R-ALT-005, R-ALT-006
    [Fact]
    public void ARoadRunsAtMostTwoCardsDeep()
    {
        var two = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("second.yaml", Chained("second", "after_road", "hold")));
        Assert.Equal(3, two.Events.Count);
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", Consequence),
            ("second.yaml", Chained("second", "after_road", "hold")), ("third.yaml", Chained("third", "second", "hold"))));
        Assert.Equal("events/third.yaml", ex.File);
        Assert.Equal("events[third].requires", ex.Field);
        Assert.Contains("3 cards deep; the most is 2", ex.Message);
    }

    // R-ALT-006: a second card follows the first card's option only if that option, too, was proposed or debated.
    [Fact]
    public void AChainedCardFollowsOnlyAConsideredOption()
    {
        var designFirst = Consequence.Replace("      basis: debated\n      by: the cabinet\n      date: 1948-06-20\n      sources: [{ url: \"https://example.org/cabinet\", checked: 2026-10-17 }]\n", "      basis: design\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", designFirst), ("second.yaml", Chained("second", "after_road", "hold"))));
        Assert.Equal("events[second].requires[0]", ex.Field);
        Assert.Contains("follows 'after_road/hold'", ex.Message);
    }

    // R-ALT-007
    [Fact]
    public void AConsequenceOfASensitiveEventCarriesItsTags()
    {
        var sensitive = Parent.Replace("from: 1948-06-01\n", "from: 1948-06-01\ntags: [war-casualties]\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", sensitive), ("after_road.yaml", Consequence)));
        Assert.Equal("events[after_road].tags", ex.Field);
        Assert.Contains("must carry 'war-casualties', as 'parent' does", ex.Message);
        // Case does not matter, as for the sensitive rule itself.
        var upper = Consequence.Replace("kind: consequence\n", "kind: consequence\ntags: [War-Casualties]\n");
        Load(("parent.yaml", sensitive), ("after_road.yaml", upper));
        // With the tag inherited, the existing no-gain rule refuses a reward.
        var gain = Consequence.Replace("kind: consequence\n", "kind: consequence\ntags: [war-casualties]\n")
            .Replace("    text: Hold the course\n", "    text: Hold the course\n    effects: [{ target: morale, delta: 0.01 }]\n");
        var ex2 = Assert.Throws<ContentException>(() => Load(("parent.yaml", sensitive), ("after_road.yaml", gain)));
        Assert.Contains("sensitive events cannot reward morale", ex2.Message);
    }

    // R-ALT-007: a second card inherits through the first card, which already carries the tag.
    [Fact]
    public void AChainedCardInheritsTheTagThroughTheFirstCard()
    {
        var sensitive = Parent.Replace("from: 1948-06-01\n", "from: 1948-06-01\ntags: [war-casualties]\n");
        var first = Consequence.Replace("kind: consequence\n", "kind: consequence\ntags: [war-casualties]\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", sensitive), ("after_road.yaml", first), ("second.yaml", Chained("second", "after_road", "hold"))));
        Assert.Equal("events[second].tags", ex.Field);
    }

    static GameState NewState(Content c)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 15), RngState = 3, Mode = Mode.Historical };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    static string Soft(string id) =>
        $"id: {id}\ntitle: {id}\ntext: 'Ordinary.'\nfrom: 1948-06-01\nto: 1948-08-15\noptions:\n  - id: ok\n    text: Ok\n";

    static Content WithThreeSoft() => Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("a1.yaml", Soft("a1")), ("a2.yaml", Soft("a2")), ("a3.yaml", Soft("a3")));

    // R-ALT-008
    [Fact]
    public void ConsequenceCardsTakeTheFirstSoftSlots()
    {
        var c = WithThreeSoft();
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road";
        s.FiredEvents.Add("parent");
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Equal(new[] { "after_road", "a1", "a2" }, s.PendingEvents);   // a3 waits; the road's card is not crowded out
    }

    // R-ALT-008
    [Fact]
    public void WithNoConsequenceCardTheOrderIsUnchanged()
    {
        var c = WithThreeSoft();
        var s = NewState(c);
        s.ChosenOptions["parent"] = "record";
        s.FiredEvents.Add("parent");
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Equal(new[] { "a1", "a2", "a3" }, s.PendingEvents);
    }

    // R-ALT-005 (review): only a consequence card may follow a consequence card, so a historical event never waits on a
    // card the record never raises, and the depth cap cannot be reset by a historical event in the middle of a chain.
    [Fact]
    public void OnlyAConsequenceCardFollowsAConsequenceCard()
    {
        var historical = "id: later\ntitle: Later\ntext: x\nfrom: 1948-09-01\nrequires: [{ event: after_road, option: hold }]\noptions:\n  - id: ok\n    text: Ok\n";
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("later.yaml", historical)));
        Assert.Equal("events/later.yaml", ex.File);
        Assert.Equal("events[later].requires[0]", ex.Field);
        Assert.Contains("only a consequence card may follow a consequence card", ex.Message);
    }

    // R-ALT-006: with diverged: true, every option that did not happen must be proposed or debated, not only the first.
    [Fact]
    public void DivergedNeedsEveryAlternativeConsidered()
    {
        var two = Parent + "  - id: other_road\n    text: Another road\n    alternative: { basis: design }\n";
        Load(("parent.yaml", two), ("after_road.yaml", Consequence));   // following the considered road alone is fine
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", two), ("after_road.yaml", Consequence.Replace("option: road", "diverged: true"))));
        Assert.Contains("follows 'parent/other_road', which nobody is recorded proposing (basis: design)", ex.Message);
    }

    // R-ALT-007 (review): the tags come from every event the card requires, not only the road it follows, so a card shown
    // after a sensitive event can never reward.
    [Fact]
    public void AConsequenceCarriesTheTagsOfEveryEventItRequires()
    {
        var sens = "id: sens\ntitle: Sens\ntext: x\nfrom: 1948-06-01\ntags: [Massacre]\noptions:\n  - id: rec\n    text: Rec\n    historical: true\n";
        var both = Consequence.Replace("requires: [{ event: parent, option: road }]", "requires: [{ event: parent, option: road }, { event: sens, option: rec }]");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("sens.yaml", sens), ("after_road.yaml", both)));
        Assert.Equal("events[after_road].tags", ex.Field);
        Assert.Contains("must carry 'Massacre', as 'sens' does", ex.Message);
        Load(("parent.yaml", Parent), ("sens.yaml", sens), ("after_road.yaml", both.Replace("kind: consequence\n", "kind: consequence\ntags: [massacre]\n")));   // case aside
    }

    // R-ALT-005 (review): an objective's completion card is the record of a completed objective, never a road not taken.
    [Fact]
    public void AConsequenceCardIsNoObjectivesCard()
    {
        var ex = LoadError(Consequence.Replace("kind: consequence\n", "kind: consequence\nobjective: anything\n"));
        Assert.Equal("events[after_road].objective", ex.Field);
        Assert.Contains("a consequence card is not an objective's card", ex.Message);
    }

    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    // R-ALT-012
    [Fact]
    public void TheHistoricalRunRaisesNoConsequenceCard()
    {
        var end = GoldenRun.PlayToEnd(Real);
        var raised = Real.Events.Where(e => e.Kind == EventKinds.Consequence && end.FiredEvents.Contains(e.Id)).Select(e => e.Id).ToList();
        Assert.True(raised.Count == 0, "the historical run raised: " + string.Join(", ", raised));
        Assert.NotEmpty(end.ChosenOptions);
        Assert.All(end.ChosenOptions, kv => Assert.False(Timeline.Diverged(Real, end, kv.Key), $"the historical run diverged at {kv.Key}"));
    }

    // ---- Task 7 (#952): reconvergence events and variant text ----------------------------------------------------

    internal const string Outside = @"
id: outside
kind: reconvergence
title: The outside event
text: 'It happened regardless.'
from: 1948-07-01
to: 1948-08-01
options:
  - id: record
    text: As it happened
    historical: true
variants:
  - when: [{ event: parent, diverged: true }]
    title: The outside event, on your course
    text: 'It happened regardless; on your course, the earlier decision went the other way.'
    sources: [{ url: ""https://example.org/outside"", checked: 2026-10-17 }]
";

    [Theory]
    // R-ALT-010
    [InlineData("road", "The outside event, on your course", "It happened regardless; on your course")]
    [InlineData("record", "The outside event", "It happened regardless.")]
    public void TheFirstMatchingVariantSuppliesTheText(string picked, string title, string text)
    {
        var c = Load(("parent.yaml", Parent), ("outside.yaml", Outside));
        var s = NewState(c);
        s.ChosenOptions["parent"] = picked;
        var e = c.Events.Single(x => x.Id == "outside");
        Assert.Equal(title, EventSystem.Title(c, s, e));
        Assert.StartsWith(text, EventSystem.Render(c, s, e));
        Assert.Equal("As it happened", e.Options.Single().Text);             // options never vary
    }

    // R-ALT-010
    [Theory]
    [InlineData("kind: reconvergence\n", "", "variants", "only a reconvergence event carries variants")]
    [InlineData("  - when: [{ event: parent, diverged: true }]", "  - when: [{ event: parent, option: record }]", "variants[0].when", "a variant follows a road not taken")]
    [InlineData("  - when: [{ event: parent, diverged: true }]", "  - when: []", "variants[0].when", "a variant follows a road not taken")]
    [InlineData("    sources: [{ url: \"https://example.org/outside\", checked: 2026-10-17 }]\n", "", "variants[0].sources", "needs at least one source")]
    [InlineData("on your course, the earlier", "on your course, {nobody} and the earlier", "variants[0].text", "unknown office placeholder '{nobody}'")]
    [InlineData("    title: The outside event, on your course\n", "    title: '{nobody} on your course'\n", "variants[0].title", "unknown office placeholder '{nobody}'")]
    [InlineData("    text: 'It happened regardless; on your course, the earlier decision went the other way.'\n", "    text: ' '\n", "variants[0].text", "a variant has text")]
    [InlineData("    historical: true\nvariants:", "variants:", "options", "a reconvergence event happened: it has a historical option")]
    [InlineData("{ event: parent, diverged: true }", "{ event: nowhere, diverged: true }", "variants[0].when[0].event", "unknown event 'nowhere'")]
    public void ABadVariantIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("outside.yaml", Outside.Replace(from, to))));
        Assert.Equal("events[outside]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    // R-ALT-010, review focus 5
    public void AVariantNeedsItsRoadClosedBeforeTheCardOpens()
    {
        var early = Outside.Replace("from: 1948-07-01", "from: 1948-06-05");   // parent's window runs to 11 June
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("outside.yaml", early)));
        Assert.Equal("events[outside].variants[0].when[0]", ex.Field);
        Assert.Contains("'parent' can still be open when this card opens", ex.Message);
    }

    [Fact]
    // R-ALT-010: a road with an open window is never settled.
    public void AVariantNeedsItsRoadToHaveAWindowThatCloses()
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent.Replace("to: 1948-06-11\n", "")), ("outside.yaml", Outside)));
        Assert.Equal("events[outside].variants[0].when[0]", ex.Field);
        Assert.Contains("'parent' can still be open when this card opens", ex.Message);
    }

    [Fact]
    // R-ALT-010: variants are tried in file order; the first whose every entry holds wins.
    public void TheFirstOfTwoMatchingVariantsWins()
    {
        var two = Outside + @"  - when: [{ event: parent, option: road }]
    title: The second variant
    text: 'Also on your course.'
    sources: [{ url: ""https://example.org/outside"", checked: 2026-10-17 }]
";
        var c = Load(("parent.yaml", Parent), ("outside.yaml", two));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road";
        var e = c.Events.Single(x => x.Id == "outside");
        Assert.Same(e.Variants[0], Timeline.Variant(c, s, e));
        Assert.Equal("The outside event, on your course", EventSystem.Title(c, s, e));
        var untitled = two.Replace("    title: The outside event, on your course\n", "");
        var c2 = Load(("parent.yaml", Parent), ("outside.yaml", untitled));
        var s2 = NewState(c2); s2.ChosenOptions["parent"] = "road";
        Assert.Equal("The outside event", EventSystem.Title(c2, s2, c2.Events.Single(x => x.Id == "outside")));   // no title: the event's own
    }

    [Fact]
    // R-ALT-010: every `when` entry must hold, not just one.
    public void AVariantNeedsEveryEntryToHold()
    {
        var other = Parent.Replace("id: parent", "id: other").Replace("title: Parent", "title: Other");
        var both = Outside.Replace("[{ event: parent, diverged: true }]", "[{ event: parent, diverged: true }, { event: other, option: road }]");
        var c = Load(("parent.yaml", Parent), ("other.yaml", other), ("outside.yaml", both));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road"; s.ChosenOptions["other"] = "record";
        var e = c.Events.Single(x => x.Id == "outside");
        Assert.Null(Timeline.Variant(c, s, e));
        Assert.Equal("It happened regardless.", EventSystem.Render(c, s, e));
        s.ChosenOptions["other"] = "road";
        Assert.Same(e.Variants[0], Timeline.Variant(c, s, e));
    }

    [Fact]
    // R-ALT-010 with R-ALT-009, review focus 5: a rejoin makes a road's historical option hold, so a variant that names
    // that option waits for every card that can rejoin the road, or its text could flip while the card is open.
    public void AVariantOnTheRecordWaitsForEveryCardThatCanRejoinIt()
    {
        var other = Parent.Replace("id: parent", "id: other").Replace("title: Parent", "title: Other");
        var withRejoin = Consequence.Replace("    text: Hold the course\n", "    text: Hold the course\n    rejoin: parent\n");   // open 15 June to 15 August
        var onRecord = Outside.Replace("[{ event: parent, diverged: true }]", "[{ event: parent, option: record }, { event: other, diverged: true }]");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent), ("other.yaml", other), ("after_road.yaml", withRejoin), ("outside.yaml", onRecord)));
        Assert.Equal("events[outside].variants[0].when[0]", ex.Field);
        Assert.Contains("'after_road' can rejoin 'parent' while this card is open", ex.Message);
        var later = onRecord.Replace("from: 1948-07-01\nto: 1948-08-01", "from: 1948-08-15\nto: 1948-09-01");
        Assert.Single(Load(("parent.yaml", Parent), ("other.yaml", other), ("after_road.yaml", withRejoin), ("outside.yaml", later)).Events.Single(x => x.Id == "outside").Variants);
        // A variant on the road itself never flips on a rejoin (the chosen option is not rewritten, diverged stays true).
        foreach (var onRoad in new[] { Outside, Outside.Replace("{ event: parent, diverged: true }", "{ event: parent, option: road }") })
            Assert.Single(Load(("parent.yaml", Parent), ("after_road.yaml", withRejoin), ("outside.yaml", onRoad)).Events.Single(x => x.Id == "outside").Variants);
    }

    [Fact]
    // R-ALT-012
    public void TheHistoricalRunShowsNoVariant()
    {
        var end = GoldenRun.PlayToEnd(Real);
        foreach (var e in Real.Events.Where(x => x.Variants.Count > 0))
            Assert.Null(Timeline.Variant(Real, end, e));
    }

    /// <summary>Review focus 5 on real content: at the state where the card first appears on its road, the variant applies.</summary>
    [Fact]
    public void EveryVariantShowsWhenItsRoadIsTaken()
    {
        foreach (var e in Real.Events.Where(x => x.Variants.Count > 0))
            for (int i = 0; i < e.Variants.Count; i++)
            {
                var road = AltRun.RoadTo(Real, new GameEvent { Id = e.Id, Requires = e.Variants[i].When });
                var fired = AltRun.Play(Real, road).FirstOrDefault(s => s.FiredEvents.Contains(e.Id));
                Assert.NotNull(fired);
                Assert.Same(e.Variants[i], Timeline.Variant(Real, fired!, e));
            }
    }

    /// <summary>Review focus 2: every consequence card in content/ fires in a run that takes its road (and, for a second
    /// card, the first card's option too), with the reference player's orders everywhere else. Until a content slice adds
    /// one, there is none; AltRunWalksTheRoadBack pins the helper on fixtures.</summary>
    [Fact]
    public void EveryConsequenceCardFiresWhenItsRoadIsTaken()
    {
        foreach (var e in Real.Events.Where(x => x.Kind == EventKinds.Consequence))
        {
            var forced = AltRun.RoadTo(Real, e);
            var run = AltRun.Play(Real, forced);
            Assert.True(run.Any(s => s.FiredEvents.Contains(e.Id)), $"{e.Id} never fired on the road {string.Join(", ", forced.Select(kv => kv.Key + "/" + kv.Value))}");
        }
    }

    [Fact]
    public void AltRunWalksTheRoadBack()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence), ("second.yaml", Chained("second", "after_road", "hold")));
        Assert.Equal(new Dictionary<string, string> { ["parent"] = "road" }, AltRun.RoadTo(c, c.Events.Single(x => x.Id == "after_road")));
        Assert.Equal(new Dictionary<string, string> { ["after_road"] = "hold", ["parent"] = "road" }, AltRun.RoadTo(c, c.Events.Single(x => x.Id == "second")));
        var div = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence.Replace("option: road", "diverged: true")));
        Assert.Equal(new Dictionary<string, string> { ["parent"] = "road" }, AltRun.RoadTo(div, div.Events.Single(x => x.Id == "after_road")));
    }

    /// <summary>AltRun on the real content: forcing an alternative changes the run, and only where it is forced.</summary>
    [Fact]
    public void AltRunForcesOnlyTheGivenChoice()
    {
        var run = AltRun.Play(Real, new Dictionary<string, string> { ["first_truce"] = "refuse" });
        var end = run[run.Count - 1];
        Assert.Equal("refuse", end.ChosenOptions["first_truce"]);
        Assert.True(Timeline.Diverged(Real, end, "first_truce"));
        Assert.Equal(new[] { "first_truce" }, end.ChosenOptions.Keys.Where(k => Timeline.Diverged(Real, end, k)).ToArray());
    }

    // ---- Task 8 (#953): the timeline helper ----------------------------------------------------------------------

    [Fact]
    // R-ALT-011
    public void TheTimelineListsEachResolvedEventInWindowOrder()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", WithRejoin), ("outside.yaml", Outside));
        var s = NewState(c);
        s.ChosenOptions["outside"] = "record"; s.ChosenOptions["parent"] = "road"; s.ChosenOptions["after_road"] = "hold";
        var rows = Timeline.Entries(c, s);
        Assert.Equal(new[] { "parent", "after_road", "outside" }, rows.Select(r => r.Event));
        var p = rows[0];
        Assert.Equal(("road", "record", true, "after_road"), (p.Chosen, p.Historical, p.Diverged, p.RejoinedBy));
        Assert.Equal(("hold", "", false), (rows[1].Chosen, rows[1].Historical, rows[1].Diverged));
    }

    [Fact]
    // R-ALT-011
    public void TheOriginNamesTheRoadTheCardFollows()
    {
        var c = Load(("parent.yaml", Parent), ("after_road.yaml", Consequence));
        var s = NewState(c);
        s.ChosenOptions["parent"] = "road";
        Assert.Equal(("parent", "road"), Timeline.Origin(c, s, c.Events.Single(x => x.Id == "after_road"))!.Value);
        Assert.Null(Timeline.Origin(c, s, c.Events.Single(x => x.Id == "parent")));
    }
}
