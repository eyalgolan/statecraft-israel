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

    // R-ALT-003: optional at load until #949 requires it, so the content on main loads unchanged.
    [Fact]
    public void AnOptionWithoutARecordStillLoads()
    {
        var bare = Parent.Substring(0, Parent.IndexOf("    alternative:", StringComparison.Ordinal));
        Assert.Null(Load(("parent.yaml", bare)).Events.Single().Options.Single(o => o.Id == "road").Alternative);
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

    // R-ALT-006
    [Fact]
    public void NoConsequenceFollowsAnAlternativeWithNoRecord()
    {
        // Until #949 makes the record required, an alternative without one is not "recorded proposing" either.
        var bare = Parent.Substring(0, Parent.IndexOf("    alternative:", StringComparison.Ordinal));
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", bare), ("after_road.yaml", Consequence)));
        Assert.Contains("which nobody is recorded proposing", ex.Message);
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
}
