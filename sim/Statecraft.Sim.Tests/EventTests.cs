using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class EventTests
{
    const string Shortage = @"
id: shortage
title: Bread lines
text: '{finance_minister} reports shortages.'
office: finance_minister
from: 1948-06-01
conditions:
  - indicator: food_supply
    max: 0.45
options:
  - id: ration
    text: Ration food
    historical: true
    recommended: true
    effects:
      - target: food_supply
        delta: 0.2
  - id: import
    text: Buy abroad
    effects:
      - target: fx_reserves
        delta: -10
  - id: escalate
    text: Blame the border
    set_rung: 3
";

    static Content Load(Dictionary<string, string> events) => ContentLoader.Load(Fixture.Write(events: events));

    static GameState State(Content c, DateTime d)
    {
        var s = new GameState { Date = d, RngState = 3 };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    [Fact]
    public void TriggersWhenDateAndConditionsHoldOnlyOnce()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var why = new List<Cause>();
        var s = State(c, new DateTime(1948, 5, 20));
        EventSystem.Trigger(c, s, why); Assert.Empty(s.PendingEvents);           // too early
        s.Date = new DateTime(1948, 6, 1);
        EventSystem.Trigger(c, s, why); Assert.Equal(new[] { "shortage" }, s.PendingEvents);
        s.PendingEvents.Clear();
        EventSystem.Trigger(c, s, why); Assert.Empty(s.PendingEvents);           // fires once
    }

    [Fact]
    public void ConditionBlocksTrigger()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); s.Indicators["food_supply"] = 0.9;
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Empty(s.PendingEvents);
    }

    [Fact]
    public void ChooseAppliesEffectsAndClearsPending()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "import", why));
        Assert.Equal(40, s.FxReserves);
        Assert.Empty(s.PendingEvents);
        Assert.Equal("That event is not waiting for a decision", EventSystem.Choose(c, s, "shortage", "ration", why));
    }

    /// <summary>R-EFX-001: a `food` effect moves the stock by whole units, rounding half away from zero, and the
    /// stock stays in 0..int.MaxValue even at the range edge.</summary>
    [Theory]
    [InlineData("7", 0, 7)]
    [InlineData("2.5", 100, 103)]
    [InlineData("-2.5", 100, 97)]
    [InlineData("2147483647", 100, 2147483647)]
    public void AnOptionCanMoveTheFoodStock(string delta, int start, int expected)
    {
        var ev = Shortage.Replace("target: fx_reserves\n        delta: -10", "target: food\n        delta: " + delta);
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = ev });
        var s = State(c, new DateTime(1948, 6, 1)); s.Food = start; var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "import", why));
        Assert.Equal(expected, s.Food);
        Assert.Contains(why, w => w.Target == "food" && w.Delta == expected - start);
    }

    /// <summary>R-EFX-001: beyond the int range the loader refuses the delta, naming file and field (an out-of-range
    /// double-to-int cast is not the same on every runtime, docs/determinism.md).</summary>
    [Fact]
    public void AFoodDeltaBeyondTheIntRangeIsALoadError()
    {
        var ev = Shortage.Replace("target: fx_reserves\n        delta: -10", "target: food\n        delta: 3000000000");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = ev }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.food", ex.Field);
        Assert.Contains("food delta out of range", ex.Message);
    }

    [Fact]
    public void AFoodCutStopsAtAnEmptyStock()
    {
        var ev = Shortage.Replace("target: fx_reserves\n        delta: -10", "target: food\n        delta: -10");
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = ev });
        var s = State(c, new DateTime(1948, 6, 1)); s.Food = 3; var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "import", why));
        Assert.Equal(0, s.Food);
        Assert.Contains(why, w => w.Target == "food" && w.Delta == -3);
    }

    /// <summary>R-EFX-001: any target that is not an indicator, `treasury`, `fx_reserves` or `food` is a load error
    /// naming file and field. `arms` pins the boundary: the Arms stock becomes a target with its pool (#781).</summary>
    [Fact]
    public void AnUnknownTargetNamesTheField()
    {
        var ev = Shortage.Replace("target: fx_reserves", "target: arms");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = ev }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.arms", ex.Field);
        Assert.Contains("unknown target", ex.Message);
    }

    [Fact]
    public void EventIndicatorEffectPersistsThroughEffectsRebuilds()
    {
        var ev = Shortage.Replace("target: food_supply\n        delta: 0.2", "target: food_supply\n        delta: 0.1");
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = ev });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "ration", why));
        Assert.Equal(0.5, s.Indicators["food_supply"], 9);   // visible immediately: base 0.4 + 0.1
        for (int i = 0; i < 3; i++)
        {
            var turn = new List<Cause>();
            EffectsSystem.Apply(c, s, turn);
            Assert.Equal(0.5, s.Indicators["food_supply"], 9);
            Assert.DoesNotContain(turn, w => w.System == "effects" && w.Target == "food_supply");   // no fake reversal
        }
        Assert.Equal(0.1, s.IndicatorShifts["food_supply"], 9);
    }

    const string Swing = @"
id: swing
title: Mood swing
text: Public mood shifts.
from: 1948-06-01
options:
  - id: up
    text: Up
    effects:
      - target: morale
        delta: 0.3
  - id: down
    text: Down
    effects:
      - target: morale
        delta: -0.2
  - id: sink
    text: Sink
    effects:
      - target: morale
        delta: -0.5
  - id: rise
    text: Rise
    effects:
      - target: morale
        delta: 0.1
";

    /// <summary>Morale (base 0.5, no effect contributions) set to level via its shift, then each option chosen in turn.</summary>
    static List<(double level, double causeDelta)> Swings(double level, params string[] options)
    {
        var c = Load(new Dictionary<string, string> { ["swing.yaml"] = Swing });
        var s = State(c, new DateTime(1948, 6, 1));
        s.IndicatorShifts["morale"] = level - 0.5; s.Indicators["morale"] = level;
        var result = new List<(double, double)>();
        foreach (var o in options)
        {
            s.PendingEvents.Add("swing"); var why = new List<Cause>();
            Assert.Null(EventSystem.Choose(c, s, "swing", o, why));
            result.Add((s.Indicators["morale"], why.Single(w => w.Target == "morale").Delta));
        }
        return result;
    }

    [Fact]
    public void EventPastTheTopStoresOnlyTheEffectiveChange()
    {
        var r = Swings(0.95, "up", "down");
        Assert.Equal(1.0, r[0].level, 9);
        Assert.Equal(0.8, r[1].level, 9);   // not stuck at 1.0
    }

    [Fact]
    public void EventPastTheBottomStoresOnlyTheEffectiveChange()
    {
        var r = Swings(0.02, "sink", "rise");
        Assert.Equal(0.0, r[0].level, 9);
        Assert.Equal(0.1, r[1].level, 9);
    }

    [Fact]
    public void EventCauseAmountIsTheActualChange()
    {
        var r = Swings(0.95, "up", "down");
        Assert.Equal(0.05, r[0].causeDelta, 9);
        Assert.Equal(-0.2, r[1].causeDelta, 9);
    }

    // Morale: base 0.5, effect rationing_morale k -0.2, inertia 1, no office, so Base + Σ = 0.5 - 0.2 * rationing.
    static void SetInner(Content c, GameState s, double rationing) { s.Policies["rationing"] = rationing; EffectsSystem.Apply(c, s, new List<Cause>()); }

    static double ChooseSwing(Content c, GameState s, string option)
    {
        s.PendingEvents.Add("swing"); var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "swing", option, why));
        return why.Single(w => w.Target == "morale").Delta;
    }

    [Fact]
    public void EventBelowAnOverBoundEffectSumIsVisibleAndSurvivesEffectChanges()
    {
        var c = Load(new Dictionary<string, string> { ["swing.yaml"] = Swing });
        var s = State(c, new DateTime(1948, 6, 1));
        SetInner(c, s, -4.0);                                    // Base + Σ = 1.3
        Assert.Equal(1.0, s.Indicators["morale"], 9);
        Assert.Equal(-0.2, ChooseSwing(c, s, "down"), 9);         // cause = visible change
        Assert.Equal(0.8, s.Indicators["morale"], 9);             // visible immediately
        SetInner(c, s, -4.0);                                    // a rebuild with the same effects keeps it
        Assert.Equal(0.8, s.Indicators["morale"], 9);
        SetInner(c, s, -2.5);                                    // Base + Σ = 1.0
        Assert.Equal(0.8, s.Indicators["morale"], 9);
        SetInner(c, s, -1.0);                                    // Base + Σ = 0.7
        Assert.Equal(0.5, s.Indicators["morale"], 9);
    }

    [Fact]
    public void OvershootingEventLeavesOnlyTheVisibleShift()
    {
        var c = Load(new Dictionary<string, string> { ["swing.yaml"] = Swing });
        var s = State(c, new DateTime(1948, 6, 1));
        SetInner(c, s, -2.0);                                    // inner 0.9
        Assert.Equal(0.1, ChooseSwing(c, s, "up"), 9);            // +0.3 only moves it to 1.0
        Assert.Equal(1.0, s.Indicators["morale"], 9);
        Assert.Equal(0.1, s.IndicatorShifts["morale"], 9);
        Assert.Equal(-0.2, ChooseSwing(c, s, "down"), 9);
        Assert.Equal(0.8, s.Indicators["morale"], 9);
    }

    [Fact]
    public void EventWithNoVisibleChangeReportsZero()
    {
        var c = Load(new Dictionary<string, string> { ["swing.yaml"] = Swing });
        var s = State(c, new DateTime(1948, 6, 1));
        SetInner(c, s, -4.0);                                    // inner 1.0 (Base + Σ = 1.3)
        Assert.Equal(0.0, ChooseSwing(c, s, "rise"), 9);          // already at 1.0
        Assert.Equal(1.0, s.Indicators["morale"], 9);
    }

    [Fact]
    public void UnknownOptionIsRejected()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Equal("That event has no such option", EventSystem.Choose(c, s, "shortage", "nap", why));
    }

    [Fact]
    public void DelegationIsDeterministicAndClearsPending()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        GameState Run()
        {
            var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
            EventSystem.Trigger(c, s, why); EventSystem.Delegate(c, s, why);
            return s;
        }
        var a = Run(); var b = Run();
        Assert.Empty(a.PendingEvents);
        Assert.Equal(a.Hash(), b.Hash());
    }

    [Fact]
    public void RenderFillsOfficeHolder()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        Assert.Equal("Def Two reports shortages.", EventSystem.Render(c, c.Events[0], new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void SensitiveEventCannotRewardTreasury()
    {
        var bad = Sensitive("massacre", "treasury", "50");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.treasury", ex.Field);
    }

    [Fact]
    public void HardAnchorNeedsAHistoricalOption()
    {
        var bad = Shortage.Replace("office: finance_minister", "anchor: hard\noffice: finance_minister")
                          .Replace("    historical: true\n", "");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options", ex.Field);
        Assert.Contains("hard anchor needs a historical option", ex.Message);
    }

    [Fact]
    public void HardAnchorWithAHistoricalOptionLoads()
    {
        var ok = Shortage.Replace("office: finance_minister", "anchor: hard\noffice: finance_minister");
        Assert.Equal("hard", Load(new Dictionary<string, string> { ["shortage.yaml"] = ok }).Events.Single().Anchor);
    }

    [Fact]
    public void PlaceholderMustBeAnOffice()
    {
        var bad = Shortage.Replace("{finance_minister}", "{king}");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].text", ex.Field);
    }

    // def_two takes defense only on 1949-02-01: nobody holds it in [1949-01-01, 1949-02-01).
    static readonly string GapPeople = Fixture.Files["people.yaml"].Replace(
        "office: defense_minister\n        from: 1949-01-01", "office: defense_minister\n        from: 1949-02-01");

    static Content LoadWithGap(string ev) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = GapPeople },
                                         new Dictionary<string, string> { ["shortage.yaml"] = ev }));

    [Fact]
    public void PlaceholderOfficeWithoutHolderForPartOfWindowFailsToLoad()
    {
        var bad = Shortage.Replace("{finance_minister}", "{defense_minister}");
        var ex = Assert.Throws<ContentException>(() => LoadWithGap(bad));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].text", ex.Field);
        Assert.Contains("defense_minister", ex.Message);
        Assert.Contains("1949-01-01", ex.Message);
    }

    [Fact]
    public void PlaceholderOfficeLeftByAHoldersDeathFailsToLoad()
    {
        // def_two's finance term is open-ended; a death inside the latest government (from 1949-03-10) loads, but
        // the office is then vacant from the date of death on, inside the open event window.
        var people = Fixture.Files["people.yaml"].Replace("    name: Def Two\n", "    name: Def Two\n    died: 1949-06-01\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = people },
                                                                                          new Dictionary<string, string> { ["shortage.yaml"] = Shortage })));
        Assert.Equal("events[shortage].text", ex.Field);
        Assert.Contains("office 'finance_minister' has no holder on 1949-06-01", ex.Message);
    }

    [Fact]
    public void PlaceholderUncoveredAtWindowStartNamesThatDate()
    {
        var bad = Shortage.Replace("text: Ration food", "text: '{defense_minister} rations food'")
                          .Replace("from: 1948-06-01", "from: 1949-01-15\nto: 1949-03-01");
        var ex = Assert.Throws<ContentException>(() => LoadWithGap(bad));
        Assert.Equal("events[shortage].options[ration].text", ex.Field);
        Assert.Contains("1949-01-15", ex.Message);
    }

    // Window ends 1949-01-01; nobody holds defense from then until 1949-02-01.
    static readonly string LateEvent = Shortage.Replace("text: Ration food", "text: '{defense_minister} rations food'")
                                               .Replace("from: 1948-06-01", "from: 1948-06-01\nto: 1949-01-01");

    [Fact]
    public void ChoosingAfterWindowEndFillsAtLastInWindowDate()
    {
        var c = LoadWithGap(LateEvent);
        var s = State(c, new DateTime(1948, 12, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        s.Date = new DateTime(1949, 1, 15);   // still pending after To, office unheld today
        Assert.Null(EventSystem.Choose(c, s, "shortage", "ration", why));
        Assert.Contains(why, w => w.Message == "Chosen: Pm One rations food");
    }

    [Fact]
    public void DelegatingAfterWindowEndFillsAtLastInWindowDate()
    {
        var c = LoadWithGap(LateEvent);
        c.People.First(p => p.Id == "def_two").Competence = 10;   // always the recommended option (ration)
        var s = State(c, new DateTime(1948, 12, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        s.Date = new DateTime(1949, 1, 1);    // exactly To
        EventSystem.Delegate(c, s, why);
        Assert.Contains(why, w => w.Message.StartsWith("Delegated") && w.Message.EndsWith("Pm One rations food"));
    }

    [Fact]
    public void PlaceholderFullyCoveredWindowLoads()
    {
        var ok = Shortage.Replace("{finance_minister}", "{defense_minister}")
                         .Replace("from: 1948-06-01", "from: 1948-06-01\nto: 1949-01-01");   // window ends where the gap starts
        Assert.Single(LoadWithGap(ok).Events);
    }

    [Fact]
    public void PlaceholderCoveredAcrossAHandoverLoads()
    {
        // Fixture: pm_one holds defense until 1949-01-01, def_two from then on (open-ended).
        var ok = Shortage.Replace("{finance_minister}", "{defense_minister}");
        Assert.Single(LoadWithStanding(ok).Events);
    }

    [Theory]
    [InlineData("'{Finance} reports shortages.'", "text: Ration food", "events[shortage].text")]
    [InlineData("'{finance_minister} reports { shortages.'", "text: Ration food", "events[shortage].text")]
    [InlineData("'{finance_minister} reports shortages.'", "text: 'Ration } food'", "events[shortage].options[ration].text")]
    public void StrayBracesAreRejected(string text, string optionText, string field)
    {
        var bad = Shortage.Replace("'{finance_minister} reports shortages.'", text).Replace("text: Ration food", optionText);
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void StrayBraceInTitleIsRejected()
    {
        var bad = Shortage.Replace("title: Bread lines", "title: 'Bread {lines'");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].title", ex.Field);
    }

    [Fact]
    public void EffectTargetMustExist()
    {
        var bad = Shortage.Replace("target: food_supply", "target: bread");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].options[ration].effects.bread", ex.Field);
    }

    [Fact]
    public void DuplicateEventIdIsAnError()
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["a.yaml"] = Shortage, ["b.yaml"] = Shortage }));
        Assert.Equal("events/b.yaml", ex.File);
        Assert.Equal("events[shortage].id", ex.Field);
    }

    /// <summary>The shortage event with a sensitive tag, its import option's effect replaced by <paramref name="target"/> and
    /// <paramref name="delta"/>. Its ration option's food gain becomes a cost, since a sensitive option may not raise food.</summary>
    static string Sensitive(string tag, string target, string delta) =>
        Shortage.Replace("title: Bread lines", "title: Bread lines\ntags: [" + tag + "]")
                .Replace("target: food_supply\n        delta: 0.2", "target: food_supply\n        delta: -0.2")
                .Replace("target: fx_reserves\n        delta: -10", "target: " + target + "\n        delta: " + delta);

    [Theory]
    [InlineData("Massacre")]
    [InlineData("War-Casualties")]
    [InlineData("HOLOCAUST")]
    [InlineData("Terror")]
    public void SensitiveTagMatchingIgnoresCase(string tag)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = Sensitive(tag, "treasury", "50") }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.treasury", ex.Field);
    }

    [Fact]
    public void SensitiveEventCannotRewardFxReserves()
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = Sensitive("terror", "fx_reserves", "50") }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.fx_reserves", ex.Field);
    }

    [Theory]
    [InlineData("treasury", "0")]
    [InlineData("treasury", "-20")]
    [InlineData("fx_reserves", "0")]
    [InlineData("fx_reserves", "-20")]
    public void SensitiveEventMayCostOrNotChangeMoney(string target, string delta)
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Sensitive("massacre", target, delta) });
        Assert.Single(c.Events);
    }

    /// <summary>The fixture with an intl_standing indicator, as the real model has.</summary>
    static Content LoadWithStanding(string ev) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["model.yaml"] = Fixture.Files["model.yaml"].Replace("indicators:\n", "indicators:\n  - id: intl_standing\n    base: 0.5\n    name: International standing\n"),
        }, new Dictionary<string, string> { ["shortage.yaml"] = ev }));

    /// <summary>R-EFX-002 (owner decision of 15 October 2026 on Q-EFX-2, "Extend it"): a sensitive option may not raise any
    /// resource. The legacy indicators stand for Food (food_supply), Social cohesion (morale) and International Support
    /// (intl_standing); `food` is Food's own target.</summary>
    [Theory]
    [InlineData("food_supply")]
    [InlineData("morale")]
    [InlineData("intl_standing")]
    [InlineData("food")]
    public void SensitiveEventCannotRaiseFoodCohesionOrSupport(string target)
    {
        var ex = Assert.Throws<ContentException>(() => LoadWithStanding(Sensitive("war-casualties", target, "0.02")));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal($"events[shortage].options[import].effects.{target}", ex.Field);
        Assert.Contains("sensitive events cannot reward", ex.Message);
    }

    [Theory]
    [InlineData("food_supply", "0")]
    [InlineData("morale", "-0.02")]
    [InlineData("food", "0")]
    [InlineData("food", "-5")]
    [InlineData("intl_standing", "-0.05")]
    public void SensitiveEventMayCostOrNotChangeFoodCohesionOrSupport(string target, string delta)
    {
        Assert.Single(LoadWithStanding(Sensitive("terror", target, delta)).Events);
    }

    /// <summary>R-EFX-007 (Q-DEP-6(a)): a displacement option cannot raise any available resource target. Arms and
    /// score are kept in this test as explicit boundaries: neither is an event-effect target on this HEAD, so the
    /// loader refuses them as unknown rather than inventing a target in this PR.</summary>
    [Theory]
    [InlineData("treasury", "sensitive events cannot reward")]
    [InlineData("fx_reserves", "sensitive events cannot reward")]
    [InlineData("food", "sensitive events cannot reward")]
    [InlineData("food_supply", "sensitive events cannot reward")]
    [InlineData("morale", "sensitive events cannot reward")]
    [InlineData("intl_standing", "sensitive events cannot reward")]
    [InlineData("arms", "unknown target")]
    [InlineData("score", "unknown target")]
    public void ADisplacementOptionCannotRaiseAnything(string target, string reason)
    {
        var ex = Assert.Throws<ContentException>(() => LoadWithStanding(Sensitive("displacement", target, "0.02")));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal($"events[shortage].options[import].effects.{target}", ex.Field);
        Assert.Contains(reason, ex.Message);
    }

    /// <summary>A non-sensitive event may still raise them.</summary>
    [Fact]
    public void AnUntaggedEventMayRaiseCohesionAndSupport()
    {
        var ok = Shortage.Replace("target: fx_reserves\n        delta: -10", "target: intl_standing\n        delta: 0.05");
        Assert.Single(LoadWithStanding(ok).Events);
    }

    [Fact]
    public void DuplicateOptionIdIsAnError()
    {
        var bad = Shortage.Replace("id: import", "id: ration");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[ration].id", ex.Field);
    }

    static string DelegatedText(Content c, int competence, ulong seed)
    {
        c.People.First(p => p.Id == "def_two").Competence = competence;
        var s = State(c, new DateTime(1948, 6, 1)); s.RngState = seed; var why = new List<Cause>();
        EventSystem.Trigger(c, s, why); why.Clear();
        EventSystem.Delegate(c, s, why);
        return why.First(w => w.Message.StartsWith("Delegated")).Message;
    }

    [Fact]
    public void MaximalCompetenceAlwaysPicksRecommendedOption()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        for (ulong seed = 1; seed <= 30; seed++)
            Assert.EndsWith("Ration food", DelegatedText(c, 10, seed));
    }

    [Fact]
    public void MinimalCompetenceDelegateCanPickOtherOptions()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var picks = new HashSet<string>();
        for (ulong seed = 1; seed <= 60; seed++) picks.Add(DelegatedText(c, 0, seed));
        Assert.True(picks.Count > 1, "competence 0 should choose at random among options");
    }

    static string Simple(string id, string anchor, string from, string? to = null) =>
        $"id: {id}\ntitle: '{id} for {{defense_minister}}'\ntext: Something happens.\nanchor: {anchor}\nfrom: {from}\n" +
        (to == null ? "" : $"to: {to}\n") +
        "options:\n  - id: ok\n    text: Accept\n    historical: true\n";

    [Fact]
    public void ShortWindowInsideAQuarterTurnStillFires()
    {
        // start_rung 1 gives quarter turns: 1948-05-15 -> 1948-08-15. The window 1948-06-01..06-11 lies wholly inside it.
        var c = ContentLoader.Load(Fixture.Write(
            new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("start_rung: 5", "start_rung: 1") },
            new Dictionary<string, string> { ["brief.yaml"] = Simple("brief", "soft", "1948-06-01", "1948-06-11") }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(s.PendingEvents);
        var r = Simulation.Step(c, s, new List<Command>());
        Assert.Equal(new DateTime(1948, 8, 15), r.State.Date);
        Assert.Equal(new[] { "brief" }, r.State.PendingEvents);
        Assert.Contains(r.Why, w => w.Message == "New: brief for Pm One");
    }

    [Fact]
    public void WindowThatEndedBeforeTheTurnDoesNotFire()
    {
        var c = ContentLoader.Load(Fixture.Write(
            new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("start_rung: 5", "start_rung: 1") },
            new Dictionary<string, string> { ["old.yaml"] = Simple("old", "soft", "1948-05-14", "1948-05-15") }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(Simulation.Step(c, s, new List<Command>()).State.PendingEvents);
    }

    [Fact]
    public void HardAnchorIsNeverLostToTheCap()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["a.yaml"] = Simple("a", "soft", "1948-05-15"),
            ["b.yaml"] = Simple("b", "soft", "1948-05-15"),
            ["c.yaml"] = Simple("c", "soft", "1948-05-15"),
            ["z.yaml"] = Simple("z", "hard", "1948-05-15"),
        }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "a", "b", "z" }, s.PendingEvents);   // hard first for the cap, then the usual order
    }

    [Fact]
    public void AllHardAnchorsFireEvenBeyondTheCap()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["a.yaml"] = Simple("a", "soft", "1948-05-15"),
            ["h1.yaml"] = Simple("h1", "hard", "1948-05-15"),
            ["h2.yaml"] = Simple("h2", "hard", "1948-05-15"),
            ["h3.yaml"] = Simple("h3", "hard", "1948-05-15"),
            ["h4.yaml"] = Simple("h4", "hard", "1948-05-15"),
        }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "h1", "h2", "h3", "h4" }, s.PendingEvents);   // the soft one finds no slot left
    }

    [Fact]
    public void TinyNegativeEffectNeverRendersAsMinusPlusZero()
    {
        var ev = Shortage.Replace("delta: -10", "delta: -0.0001");
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = ev });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "import", why));
        Assert.Contains(why, w => w.Message == "Foreign currency $0.0m (Bread lines)");
        Assert.DoesNotContain(why, w => w.Message.Contains("-+"));
    }

    [Fact]
    public void EventFiredAfterItsWindowNamesTheInWindowHolder()
    {
        // Quarter turns: 1948-11-15 -> 1949-02-15. defense_minister passes from Pm One to Def Two on 1949-01-01.
        var c = ContentLoader.Load(Fixture.Write(
            new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("start_rung: 5", "start_rung: 1") },
            new Dictionary<string, string> { ["late.yaml"] = Simple("late", "soft", "1948-12-20", "1948-12-30") }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        StepResult r = null!;
        for (int i = 0; i < 3; i++) { r = Simulation.Step(c, s, new List<Command>()); s = r.State; }
        Assert.Equal(new DateTime(1949, 2, 15), s.Date);
        Assert.Contains(r.Why, w => w.Message == "New: late for Pm One");
    }

    // R-SWAMP-002, Phase 2 Q-CUT-4's rule: hula_drainage gained only a condition (site: hula), not new text or options, so
    // its review status and the release gate's verdict on it are unchanged.
    [Fact]
    public void TheHulaConditionLeavesItsReleaseStatus()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var e = c.Events.Single(x => x.Id == "hula_drainage");
        Assert.Equal(("hula", "fact-checked"), (e.Site, e.Status));
        Assert.DoesNotContain("hula_drainage", ReleaseGate.SensitiveEvents(c));
        Assert.DoesNotContain("hula_drainage", ReleaseGate.SensitiveUnreviewed(c));
    }
}
