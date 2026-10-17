using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>`requires: [{ event, option }, ...]`: an event is eligible only after every listed event was resolved with its option
/// (fix round 1, I2: a follow-up card must not contradict the player's earlier choice).</summary>
public class EventRequiresTests
{
    const string First = @"
id: first
title: First
text: 'A choice.'
from: 1948-06-01
options:
  - id: go
    text: Go
    historical: true
  - id: stay
    text: Stay
";

    const string Follow = @"
id: follow
title: Follow-up
text: 'After going.'
from: 1948-06-01
requires: [{ event: first, option: go }]
options:
  - id: ok
    text: Fine
";

    static Content Load(string follow) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["first.yaml"] = First, ["follow.yaml"] = follow }));

    static GameState State(Content c)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1), RngState = 3 };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    [Fact]
    public void TheRequirementIsReadFromYaml()
    {
        var r = Assert.Single(Load(Follow).Events.Find(x => x.Id == "follow")!.Requires);
        Assert.Equal("first", r.Event);
        Assert.Equal("go", r.Option);
        Assert.False(r.Diverged);
        Assert.Empty(Load(Follow).Events.Find(x => x.Id == "first")!.Requires);
    }

    [Fact]
    public void ItWaitsUntilTheRequiredOptionIsChosen()
    {
        var c = Load(Follow);
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Equal(new[] { "first" }, s.PendingEvents);           // the follow-up is not eligible yet
        Assert.Null(EventSystem.Choose(c, s, "first", "go", why));
        Assert.Equal("go", s.ChosenOptions["first"]);
        EventSystem.Trigger(c, s, why);
        Assert.Equal(new[] { "follow" }, s.PendingEvents);
    }

    [Fact]
    public void AnotherOptionKeepsTheFollowUpAway()
    {
        var c = Load(Follow);
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "first", "stay", why));
        EventSystem.Trigger(c, s, why);
        Assert.Empty(s.PendingEvents);
        Assert.DoesNotContain("follow", s.FiredEvents);
    }

    [Fact]
    public void ADelegatedChoiceCountsToo()
    {
        var c = Load(Follow);
        var s = State(c);
        s.Mode = Mode.Historical;
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        EventSystem.Delegate(c, s, why);
        Assert.True(s.ChosenOptions.ContainsKey("first"));
    }

    [Theory]
    [InlineData("requires: [{ event: nope, option: go }]", "events[follow].requires[0].event", "unknown event 'nope'")]
    [InlineData("requires: [{ event: first, option: nope }]", "events[follow].requires[0].option", "event 'first' has no option 'nope'")]
    [InlineData("requires: [{ event: follow, option: ok }]", "events[follow].requires[0].event", "an event cannot require itself")]
    [InlineData("requires: [{ event: '', option: go }]", "events[follow].requires[0].event", "unknown event ''")]
    public void ABrokenRequirementIsALoadErrorNamingTheFile(string line, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Follow.Replace("requires: [{ event: first, option: go }]", line)));
        Assert.Equal("events/follow.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void ChosenOptionsAreClonedAndHashed()
    {
        var a = new GameState { Date = new DateTime(1948, 6, 1) };
        var b = a.Clone();
        b.ChosenOptions["first"] = "go";
        Assert.Empty(a.ChosenOptions);
        Assert.NotEqual(a.Hash(), b.Hash());
        var c = a.Clone();
        c.ChosenOptions["first"] = "stay";
        Assert.NotEqual(b.Hash(), c.Hash());
    }

    static string Requiring(string id, string other) => $@"
id: {id}
title: {id}
text: 'Waits.'
from: 1948-06-01
requires: [{{ event: {other}, option: ok }}]
options:
  - id: ok
    text: Fine
";

    /// <summary>Re-review N4: a cycle of requirements could never fire; the loader names every file in it.</summary>
    [Fact]
    public void ACycleOfRequirementsIsALoadErrorNamingItsFiles()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["first.yaml"] = First, ["a.yaml"] = Requiring("a", "b"), ["b.yaml"] = Requiring("b", "c"), ["c.yaml"] = Requiring("c", "a"),
        })));
        Assert.Equal("events/a.yaml", ex.File);
        Assert.Equal("events[a].requires", ex.Field);
        Assert.Contains("requirements form a cycle: events/a.yaml -> events/b.yaml -> events/c.yaml -> events/a.yaml", ex.Message);
    }

    [Fact]
    public void AChainWithoutACycleLoads()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["first.yaml"] = First, ["follow.yaml"] = Follow, ["a.yaml"] = Requiring("a", "follow"), ["b.yaml"] = Requiring("b", "a"),
        }));
        Assert.Equal(4, c.Events.Count);
    }

    const string Second = @"
id: second
title: Second
text: 'Another choice.'
from: 1948-06-01
options:
  - id: agree
    text: Agree
    historical: true
  - id: decline
    text: Decline
";

    const string Plain = @"
id: plain
title: Plain
text: 'No record of what happened.'
from: 1948-06-01
options:
  - id: one
    text: One
  - id: two
    text: Two
";

    static Content LoadAll(string follow) => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
    {
        ["first.yaml"] = First, ["second.yaml"] = Second, ["plain.yaml"] = Plain, ["follow.yaml"] = follow,
    }));

    /// <summary>Plays the two choices, then triggers once more and says whether the follow-up fired.</summary>
    static bool FiresAfter(Content c, string first, string second)
    {
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "first", first, why));
        Assert.Null(EventSystem.Choose(c, s, "second", second, why));
        EventSystem.Trigger(c, s, why);
        return s.FiredEvents.Contains("follow");
    }

    // R-ALT-001
    [Theory]
    [InlineData("go", "decline", true)]
    [InlineData("go", "agree", false)]
    [InlineData("stay", "decline", false)]
    public void EveryRequirementInTheListMustHold(string first, string second, bool fires)
    {
        var c = LoadAll(Follow.Replace("requires: [{ event: first, option: go }]",
            "requires: [{ event: first, option: go }, { event: second, option: decline }]"));
        Assert.Equal(fires, FiresAfter(c, first, second));
    }

    // R-ALT-002
    [Theory]
    [InlineData("go", false)]
    [InlineData("stay", true)]
    public void DivergedMeansAnyOptionButTheHistoricalOne(string first, bool fires)
    {
        var c = LoadAll(Follow.Replace("requires: [{ event: first, option: go }]", "requires: [{ event: first, diverged: true }]"));
        Assert.Equal(fires, FiresAfter(c, first, "agree"));
    }

    // R-ALT-002
    [Fact]
    public void AnUnansweredEventHasNotDiverged()
    {
        var c = LoadAll(Follow);
        var s = State(c);
        Assert.False(Timeline.Diverged(c, s, "first"));
        Assert.False(Timeline.Diverged(c, s, "plain"));
        s.ChosenOptions["plain"] = "two";
        Assert.False(Timeline.Diverged(c, s, "plain"));    // no historical option: nothing to diverge from
        s.ChosenOptions["first"] = "stay";
        Assert.True(Timeline.Diverged(c, s, "first"));
    }

    // R-ALT-002
    [Theory]
    [InlineData("requires: [{ event: first }]", "events[follow].requires[0]", "needs an option or diverged: true")]
    [InlineData("requires: [{ event: first, option: go, diverged: true }]", "events[follow].requires[0]", "an option or diverged: true, not both")]
    [InlineData("requires: [{ event: plain, diverged: true }]", "events[follow].requires[0].diverged", "event 'plain' has no historical option")]
    [InlineData("requires: [{ event: first, option: go }, { event: first, diverged: true }]", "events[follow].requires[1].event", "event 'first' is already required")]
    [InlineData("requires: [{ event: first, option: go }, { event: first, option: stay }]", "events[follow].requires[1].event", "event 'first' is already required")]
    public void ABadDivergedRequirementIsALoadError(string line, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => LoadAll(Follow.Replace("requires: [{ event: first, option: go }]", line)));
        Assert.Equal("events/follow.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-ALT-002: a delegated answer diverges as a chosen one does (Q-ALT-11, decided).
    [Fact]
    public void ADelegatedAlternativeDivergesToo()
    {
        var c = LoadAll(Follow);
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        for (ulong seed = 0; seed < 200 && !Timeline.Diverged(c, s, "first"); seed++)
        {
            s = State(c);
            s.RngState = seed;
            EventSystem.Trigger(c, s, why);
            EventSystem.Delegate(c, s, why);      // soft and no office: a random pick half the time
        }
        Assert.Equal("stay", s.ChosenOptions["first"]);
        Assert.True(Timeline.Diverged(c, s, "first"));
    }

    /// <summary>The cycle check walks every entry of the list, not only the first.</summary>
    [Fact]
    public void ACycleThroughASecondRequirementIsFound()
    {
        var a = "id: a\ntitle: a\ntext: x\nfrom: 1948-06-01\nrequires: [{ event: first, option: go }, { event: b, option: ok }]\noptions:\n  - id: ok\n    text: Ok\n";
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["first.yaml"] = First, ["a.yaml"] = a, ["b.yaml"] = Requiring("b", "a"),
        })));
        Assert.Equal("events/a.yaml", ex.File);
        Assert.Equal("events[a].requires", ex.Field);
        Assert.Contains("requirements form a cycle: events/a.yaml -> events/b.yaml -> events/a.yaml", ex.Message);
    }
}
