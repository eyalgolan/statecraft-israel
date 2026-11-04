using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Dated windows for model records (#57, chapters design 3.2): the active view, the state following it, the command
/// check and the loader's reference checks. Records with no window behave as before (the goldens pin that).</summary>
public class ContentWindowsTests
{
    static readonly DateTime D0 = new DateTime(1948, 5, 15);
    static readonly DateTime Cut = new DateTime(1948, 8, 1);

    // The fixture model plus `shelter`, an indicator no code reads by name, so these tests can give it a window (the loader
    // refuses one on morale, food_supply, housing, security and intl_standing). `security` and `intl_standing` are added too,
    // since the fixture model has neither and the refusal test needs both.
    const string Shelter = "  - id: shelter\n    base: 0.3\n    name: Shelter\n";
    const string HudRead = "  - id: security\n    base: 0.3\n    name: Security\n  - id: intl_standing\n    base: 0.5\n    name: Standing\n";
    static string Base => Fixture.Files["model.yaml"].Replace("policies:\n", Shelter + HudRead + "policies:\n");
    static string Model(Func<string, string>? edit = null) => edit == null ? Base : edit(Base);
    static string Offices(Func<string, string> edit) => edit(Fixture.Files["offices.yaml"]);

    static Content Load(string model, string? offices = null, Dictionary<string, string>? events = null)
    {
        var o = new Dictionary<string, string> { ["model.yaml"] = model };
        if (offices != null) o["offices.yaml"] = offices;
        return ContentLoader.Load(Fixture.Write(o, events));
    }

    static string Window(string text, string anchor, string window) => text.Replace(anchor, anchor + "\n    " + window);

    static void Refused(string model, string expected, string? offices = null, Dictionary<string, string>? events = null)
    {
        var e = Assert.Throws<ContentException>(() => Load(model, offices, events));
        Assert.Contains(expected, e.Message);
    }

    // ---- the active view

    [Fact]
    public void WithNoWindowEveryDateHasTheSameFullView()
    {
        var c = Load(Model());
        Assert.Same(c.At(D0), c.At(new DateTime(1956, 1, 1)));
        Assert.Equal(c.Policies.Count, c.At(D0).Policies.Count);
        Assert.Equal(c.Indicators.Count, c.At(D0).Indicators.Count);
    }

    [Fact]
    public void ARecordIsActiveOnItsFromAndNotOnItsTo()
    {
        var c = Load(Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-08-01\n    to: 1948-11-01")));
        Assert.DoesNotContain(c.At(Cut.AddDays(-1)).Policies, p => p.Id == "maabarot");
        Assert.Contains(c.At(Cut).Policies, p => p.Id == "maabarot");
        Assert.Contains(c.At(new DateTime(1948, 10, 31)).Policies, p => p.Id == "maabarot");
        Assert.DoesNotContain(c.At(new DateTime(1948, 11, 1)).Policies, p => p.Id == "maabarot");
    }

    [Fact]
    public void TheActiveListsAreBuiltOncePerSegmentAtLoadNotPerCall()
    {
        var c = Load(Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-08-01")));
        Assert.Same(c.At(D0), c.At(Cut.AddDays(-30)));          // one segment before the cut
        Assert.Same(c.At(Cut), c.At(new DateTime(1950, 1, 1))); // one segment after it
        Assert.NotSame(c.At(D0), c.At(Cut));
        var first = c.At(D0);
        for (int i = 0; i < 100; i++) Assert.Same(first, c.At(D0.AddDays(i % 60)));
    }

    // ---- the state follows the windows

    // The fixture's turns run weekly to 12 June, then every quarter (the ceasefire ends the war rung): 5/15 ... 6/12, 9/12.
    static Content Windowed() => Load(Model(m => Window(Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-06-20"), "  - id: shelter\n    base: 0.3", "to: 1948-08-01")));

    [Fact]
    public void ANewGameHoldsOnlyTheRecordsActiveOnTheStartDate()
    {
        var c = Windowed();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.DoesNotContain("maabarot", s.Policies.Keys);
        Assert.Contains("shelter", s.Indicators.Keys);
    }

    static GameState Play(Content c, DateTime until)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        while (s.Date < until && s.GameOver == null) s = Simulation.Step(c, s, new Command[0]).State;
        return s;
    }

    [Fact]
    public void ARecordThatBeginsIsAddedAtItsDefaultAndOneThatEndsIsRemoved()
    {
        var c = Windowed();
        var early = Play(c, new DateTime(1948, 6, 10));
        Assert.DoesNotContain("maabarot", early.Policies.Keys);
        Assert.Contains("shelter", early.Indicators.Keys);
        var later = Play(c, new DateTime(1948, 9, 10));
        Assert.Equal(0.0, later.Policies["maabarot"]);             // added as Init would add it
        Assert.DoesNotContain("shelter", later.Indicators.Keys);   // removed with its shift
        Assert.DoesNotContain("shelter", later.IndicatorShifts.Keys);
        Assert.NotEmpty(later.Hash());                              // the hash never reads a missing key
        Assert.NotEmpty(later.Describe());
    }

    [Fact]
    public void ARecordThatBeginsEntersWithAZeroContributionAndTheStateStaysConsistent()
    {
        var m = Model(x => x.Replace("  - id: rationing_food\n    source: rationing", "  - id: rationing_food\n    from: 1948-08-01\n    source: rationing"));
        var c = Load(m);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.DoesNotContain("rationing_food", s.EffectContributions.Keys);
        s = Play(c, new DateTime(1948, 8, 10));
        Assert.Equal(0.0, s.EffectContributions["rationing_food"]);
        Assert.Equal(c.Effects.Count, s.EffectContributions.Count);
    }

    [Fact]
    public void ASituationThatEndsLeavesTheActiveSet()
    {
        var m = Model(x => x.Replace("    on_below: 0.3", "    to: 1948-09-01\n    on_below: 0.3").Replace("base: 0.4\n    name: Food supply", "base: 0.1\n    name: Food supply"));
        var c = Load(m);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s = Simulation.Step(c, s, new Command[0]).State;
        Assert.Contains("hunger", s.ActiveSituations);
        s = Play(c, new DateTime(1948, 9, 10));
        Assert.DoesNotContain("hunger", s.ActiveSituations);
    }

    // ---- commands

    [Fact]
    public void SetPolicyOutsideItsWindowIsRefusedAndChangesNothing()
    {
        var c = Windowed();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var before = s.Hash();
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("maabarot", 1.0) });
        Assert.Contains("not available", r.Rejected.Single().Reason);
        Assert.DoesNotContain("maabarot", r.State.Policies.Keys);
        Assert.Equal(s.Attention, Simulation.NewGame(c, 1, Mode.Historical).Attention);
        Assert.Equal(before, s.Hash());   // Step never mutates its input
    }

    [Fact]
    public void SetPolicyInsideItsWindowWorksAndAnUnknownPolicyKeepsItsOwnReason()
    {
        var c = Windowed();
        var s = Play(c, new DateTime(1948, 8, 10));
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("maabarot", 1.0), new SetPolicy("nope", 1.0) });
        Assert.Equal(1.0, r.State.Policies["maabarot"]);
        Assert.Contains("does not exist", r.Rejected.Single().Reason);
    }

    // ---- the loader refuses a reference to a record that is not active for the whole window of the record using it

    [Theory]
    [InlineData("  - id: maabarot\n    default: 0.0", "policies[maabarot]")]
    [InlineData("  - id: shelter\n    base: 0.3", "indicators[shelter]")]
    [InlineData("  - id: rationing_morale\n    source: rationing", "effects[rationing_morale]")]
    [InlineData("  - id: hunger\n    indicator: food_supply", "situations[hunger]")]
    [InlineData("  - id: everyone", "voter_groups[everyone]")]
    [InlineData("  - id: usa", "actors[usa]")]
    public void AWindowMustRunForward(string anchor, string field)
    {
        foreach (var window in new[] { "from: 1948-09-01\n    to: 1948-08-01", "from: 1948-09-01\n    to: 1948-09-01" })   // backwards, and empty
        {
            var e = Assert.Throws<ContentException>(() => Load(Model(m => Window(m, anchor, window))));
            Assert.Equal("model.yaml", e.File);
            Assert.Equal(field, e.Field);
            Assert.Contains("from must be before to", e.Message);
        }
    }

    [Theory]
    [InlineData("morale", "  - id: morale\n    base: 0.5")]
    [InlineData("food_supply", "  - id: food_supply\n    base: 0.4")]
    [InlineData("housing", "  - id: housing\n    base: 0.3")]
    [InlineData("security", "  - id: security\n    base: 0.3")]
    [InlineData("intl_standing", "  - id: intl_standing\n    base: 0.5")]
    public void AWindowOnAnIndicatorTheCodeReadsByNameIsRefused(string id, string anchor)
    {
        foreach (var w in new[] { "from: 1948-08-01", "to: 1949-01-01" })
            Refused(Model(m => Window(m, anchor, w)), $"'{id}' is read by name in the code");
        Assert.NotNull(Load(Model()));   // no window: fine
    }

    [Fact]
    public void AnEffectSourceThatIsNotActiveThroughTheEffectIsRefused()
    {
        var e = Assert.Throws<ContentException>(() => Load(Model(m => Window(m, "  - id: rationing\n    default: 0.0", "from: 1948-08-01"))));
        Assert.Equal("model.yaml", e.File);                     // the error names the file and the key
        Assert.Equal("effects[rationing_food].source", e.Field);
        Assert.Contains("source 'rationing' is not active", e.Message);
    }

    [Fact]
    public void AnEffectTargetThatIsNotActiveThroughTheEffectIsRefused() =>
        Refused(Model(m => Window(m, "  - id: shelter\n    base: 0.3", "to: 1948-09-01").Replace("situations:\n", "  - id: shelter_effect\n    source: rationing\n    target: shelter\n    k: 0.1\n    inertia: 1\n    name: Shelter effect\nsituations:\n")), "target 'shelter' is not active");

    [Fact]
    public void ASituationOnAnIndicatorThatEndsEarlyIsRefused() =>
        Refused(Model(m => Window(m, "  - id: shelter\n    base: 0.3", "to: 1948-09-01").Replace("situations:\n", "situations:\n  - id: homeless\n    indicator: shelter\n    on_below: 0.2\n    off_above: 0.3\n    name: Homeless\n")), "indicator 'shelter' is not active");

    [Fact]
    public void AVoterGroupWeightOnAnIndicatorThatBeginsLateIsRefused() =>
        Refused(Model(m => Window(m, "  - id: shelter\n    base: 0.3", "from: 1948-08-01").Replace("weights: { morale: 1.0 }", "weights: { morale: 1.0, shelter: 0.5 }")), "is not active for the whole window");

    [Fact]
    public void AWindowedRecordMayUseARecordWithAWiderWindow()
    {
        var m = Model(x => Window(x, "  - id: rationing\n    default: 0.0", "from: 1948-06-01")
            .Replace("  - id: rationing_food\n", "  - id: rationing_food\n    from: 1948-07-01\n").Replace("  - id: rationing_morale\n", "  - id: rationing_morale\n    from: 1948-07-01\n"));
        Assert.NotNull(Load(m));
    }

    [Fact]
    public void ARecordMayUseOneWithExactlyTheSameWindow()
    {
        // Both edges are inclusive for the check: the same from and the same to cover the user's whole window.
        const string w = "from: 1948-07-01\n    to: 1949-01-01";
        var m = Model(x => Window(x, "  - id: rationing\n    default: 0.0", w)
            .Replace("  - id: rationing_food\n", "  - id: rationing_food\n    " + w + "\n").Replace("  - id: rationing_morale\n", "  - id: rationing_morale\n    " + w + "\n"));
        Assert.NotNull(Load(m));
        Refused(m.Replace("  - id: rationing_food\n    from: 1948-07-01", "  - id: rationing_food\n    from: 1948-06-30"), "source 'rationing' is not active");
        Refused(m.Replace("  - id: rationing_food\n    from: 1948-07-01\n    to: 1949-01-01", "  - id: rationing_food\n    from: 1948-07-01\n    to: 1949-01-02"), "source 'rationing' is not active");
    }

    const string Event = "id: probe\ntitle: Probe\ntext: A probe.\nanchor: soft\nfrom: 1948-06-01\nto: 1948-12-01\n{0}options:\n  - id: a\n    text: Do a\n{1}sources: [{{ url: \"https://example.org/event\", checked: 2026-10-18 }}]\n";

    [Fact]
    public void AnEventConditionOnAnIndicatorThatBeginsInsideItsWindowIsRefused() =>
        Refused(Model(m => Window(m, "  - id: shelter\n    base: 0.3", "from: 1948-07-01")), "not active for the whole window of this event",
            events: new Dictionary<string, string> { ["probe.yaml"] = string.Format(Event, "conditions: [{ indicator: shelter, min: 0.1 }]\n", "") });

    [Fact]
    public void AnEventOptionThatSetsAPolicyOrMovesAnIndicatorOutsideItsWindowIsRefused()
    {
        var model = Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-07-01"));
        Refused(model, "not active for the whole window of this event",
            events: new Dictionary<string, string> { ["probe.yaml"] = string.Format(Event, "", "    set_policy: { maabarot: 1.0 }\n") });
        Refused(Model(m => Window(m, "  - id: shelter\n    base: 0.3", "to: 1948-09-01")), "not active for the whole window of this event",
            events: new Dictionary<string, string> { ["probe.yaml"] = string.Format(Event, "", "    effects: [{ target: shelter, delta: 0.1 }]\n") });
    }

    // ---- a late fire (EventSystem.Trigger) can come after the window of a record the event reaches has ended

    // The event's window [20 June, 1 July) falls inside the fixture's quarterly turn 12 June to 12 September, so it fires late,
    // on 12 September, after shelter (and maabarot) have left the state on 1 July.
    static Content LateFire(string conditions, string option) => Load(
        Model(m => Window(Window(m, "  - id: shelter\n    base: 0.3", "to: 1948-07-01"), "  - id: maabarot\n    default: 0.0", "to: 1948-07-01")),
        events: new Dictionary<string, string> { ["probe.yaml"] = "id: probe\ntitle: Probe\ntext: A probe.\nanchor: soft\nfrom: 1948-06-20\nto: 1948-07-01\n" + conditions + "options:\n  - id: a\n    text: Do a\n" + option + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n" });

    [Fact]
    public void ALateFireDoesNotReadAnIndicatorWhoseWindowHasEnded()
    {
        var c = LateFire("conditions: [{ indicator: shelter, min: 0.0 }]\n", "");
        var s = Play(c, new DateTime(1948, 9, 1));
        Assert.Equal(new DateTime(1948, 9, 12), s.Date);
        Assert.DoesNotContain("probe", s.FiredEvents);   // its condition reads a record that no longer exists, so it is not met
    }

    [Fact]
    public void ALateFiredEventResolvedAfterTheWindowLeavesEndedRecordsOut()
    {
        var c = LateFire("", "    effects: [{ target: shelter, delta: 0.1 }, { target: treasury, delta: 5 }]\n    set_policy: { maabarot: 1.0 }\n");
        var s = Play(c, new DateTime(1948, 9, 1));
        Assert.Contains("probe", s.PendingEvents);
        var r = Simulation.Step(c, s, new Command[] { new ChooseEventOption("probe", "a") });
        Assert.Empty(r.Rejected);
        Assert.DoesNotContain("shelter", r.State.Indicators.Keys);
        Assert.DoesNotContain("shelter", r.State.IndicatorShifts.Keys);
        Assert.DoesNotContain("maabarot", r.State.Policies.Keys);   // not added back by the option
        Assert.Contains("probe", r.State.ChosenOptions.Keys);
        Assert.Contains(r.Why, w => w.Target == "treasury" && w.Delta == 5);   // treasury is not a dated record: the option still pays it
    }

    [Fact]
    public void ARecordWithAWindowNeedsItsOfficeForThatWholeWindow()
    {
        var offices = Offices(o => o + "  - id: trade_minister\n    title: Trade\n    from: 1948-09-01\n");
        string OnTrade(string m) => m.Replace("  - id: maabarot\n    default: 0.0\n    office: finance_minister", "  - id: maabarot\n    default: 0.0\n    office: trade_minister");
        Refused(Window(OnTrade(Model()), "  - id: maabarot\n    default: 0.0", "from: 1948-07-01"), "office 'trade_minister' does not exist for the whole window", offices);
        Assert.NotNull(Load(OnTrade(Model()), offices));   // with no window the existing rule stands: the office may be created later
    }

    [Fact]
    public void AnEffectWithAWindowNeedsItsOfficeForThatWholeWindow()
    {
        var offices = Offices(o => o + "  - id: trade_minister\n    title: Trade\n    from: 1948-09-01\n");
        var m = Model(x => x.Replace("  - id: rationing_food\n    source: rationing", "  - id: rationing_food\n    from: 1948-07-01\n    source: rationing")
            .Replace("    inertia: 2\n    office: finance_minister", "    inertia: 2\n    office: trade_minister"));
        var e = Assert.Throws<ContentException>(() => Load(m, offices));
        Assert.Equal("effects[rationing_food].office", e.Field);
        Assert.Contains("office 'trade_minister' does not exist for the whole window", e.Message);
    }
}
