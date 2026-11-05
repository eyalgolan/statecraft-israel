using Statecraft.Sim;
using System.Security.Cryptography;

// R-EFX-001/002/005/007, R-MONEY-003: identity comes from eras, never a post-load switch.
public class Gdd03EventRulesTests
{
    const string Manifest = "gdd03-events.yaml";
    const string Empty = "ruleset: gdd-0.3\nexclusions: []\n";
    static readonly string[] Excluded = { };   // #2483 turned on the last four, after #2457 brought back israel_bonds
    // #2224 (owner, 2 November 2026, "Bring back 3, design bonds (Recommended)"): D1/Q2 keep the foreign-currency ledger,
    // so these three cards, excluded by #1772 for want of a ledger mapping, are active again. israel_bonds came back with
    // its equal Debt (#2457, R-EFX-009; owner, 2 November 2026, #2224: "Add equal Debt").
    static readonly string[] Restored = { "clothing_rationing_1950", "reparations_claim", "reparations_vote" };
    // #2483 (owner, 4 November 2026): "allow cards to effect food supply and housing readout, get these cards into the game
    // turned on". These four, excluded by #1772 for their Food supply, housing and policy writes, are active again.
    static readonly string[] TurnedOn = { "austerity_1949", "forex_crisis_1951", "iraqi_exodus", "maabarot_policy" };
    static string Card(string id = "probe", string option = "") =>
        $"id: {id}\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n  - id: ok\n    text: Accept\n    historical: true\n" + option
        + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n";
    static Dictionary<string, string> Files(bool modern = true)
    {
        var f = Fixture.Gdd();
        if (modern) f["eras.yaml"] = f["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
        f[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        f["model.yaml"] = Fixture.Files["model.yaml"].Replace("indicators:", "indicators:\n  - { id: intl_standing, name: Support, base: 0.5 }");
        f[Manifest] = Empty;
        return f;
    }
    static Content Load(Dictionary<string, string> f, string card) =>
        ContentLoader.Load(Fixture.Write(f, new() { ["probe.yaml"] = card }));
    static ContentException Reject(string option, string field, Action<Dictionary<string, string>>? edit = null)
    {
        var f = Files(); edit?.Invoke(f);
        var ex = Assert.Throws<ContentException>(() => Load(f, Card(option: option)));
        Assert.Equal("events/probe.yaml", ex.File);
        Assert.Equal("events[probe].options[ok]." + field, ex.Field);
        Assert.Contains("gdd-0.3", ex.Message);
        return ex;
    }
    static void Effect(string target, bool indicator = false) => Reject(
        $"    effects: [{{ target: {target}, delta: -0.1 }}]\n", "effects." + target,
        indicator ? f => f["model.yaml"] = Fixture.Files["model.yaml"].Replace("indicators:", $"indicators:\n  - {{ id: {target}, name: Fixture, base: 0.5 }}") : null);

    [Fact] public void Gdd03RejectsAttentionEffectEvenIfIndicatorExists() => Effect("attention", true);
    // R-EFX-001, #2483 (owner, 4 November 2026): an event writes the Food supply and housing readouts under gdd-0.3, through
    // the same clamped shift as gdd-0.1, so the cause names the applied amount.
    [Fact] public void Gdd03AcceptsFoodSupplyWrite() => ReadoutWrite("food_supply");
    [Fact] public void Gdd03AcceptsHousingWrite() => ReadoutWrite("housing");
    static void ReadoutWrite(string target)
    {
        var c = Load(Files(), Card(option: $"    effects: [{{ target: {target}, delta: -0.1 }}, {{ target: {target}, delta: 0.3 }}]\n").Replace("from:", "anchor: hard\nfrom:"));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        EffectsSystem.Shift(c, s, target, .05 - s.Indicators[target]);
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "probe", "ok", why));
        // -0.1 from 0.05 clamps at 0 (applied -0.05), then +0.3 lands on 0.3.
        Assert.Equal(.3, s.Indicators[target], 12);
        Assert.Equal(new[] { -.05, .3 }, why.Where(w => w.Target == target).Select(w => Math.Round(w.Delta, 12)));
    }
    // R-EFX-001, #2483 (maintainer reading: "turned on" includes the cards' policy writes): rationing and maabarot load and
    // apply under gdd-0.3.
    [Theory] [InlineData("rationing")] [InlineData("maabarot")]
    public void Gdd03AcceptsTheCardsPolicyWrites(string policy)
    {
        var c = Load(Files(), Card(option: $"    set_policy: {{ {policy}: 0.5 }}\n").Replace("from:", "anchor: hard\nfrom:"));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Null(EventSystem.Choose(c, s, "probe", "ok", new List<Cause>()));
        Assert.Equal(.5, s.Policies[policy]);
    }
    [Fact] public void Gdd03RejectsCityLossDefeatEffect() => Effect("city_loss_defeat", true);
    [Fact] public void Gdd03RejectsUnaccountedPopulationGrant() => Reject("    grant_units: [{ type: halutz, n: 1, at: town_a }]\n", "grant_units");
    [Fact] public void Gdd03RejectsUnaccountedFacilityEffect() => Reject("    city_effects: [{ city: town_a, field: housing_capacity, delta: 1 }]\n", "city_effects");
    [Fact] public void Gdd03RejectsUnaccountedProjectUnlock() => Reject("    unlock_projects: [housing]\n", "unlock_projects",
        f => f["projects.yaml"] = Fixture.GddProjects.Replace("id: housing,", "id: housing, locked: true,"));

    [Theory]
    [InlineData("attention_per_turn")]
    [InlineData("defeat")]
    [InlineData("any_city_loss")]
    public void CompatibilityIndicatorsCannotEnableCommandTargets(string target) => Effect(target, true);

    // R-EFX-001, #1718 D1/Q1/Q2 (owner, 1 November 2026, "Approve all six (Recommended)"): main's economy stays (passive
    // budget, monthly Food refill, the foreign-currency ledger), so under gdd-0.3 only Attention retires. These targets
    // load under gdd-0.3 exactly as under gdd-0.1: a budget or refill spelling is accepted when model.yaml defines it.
    [Theory]
    [InlineData("budget", true)]
    [InlineData("budget_finance_minister", true)]
    [InlineData("passive_budget", true)]
    [InlineData("passive_budget_defence", true)]
    [InlineData("food_refill", true)]
    [InlineData("automatic_food_refill", true)]
    [InlineData("fx_reserves", false)]
    public void Gdd03KeepsD1BudgetRefillAndForeignCurrencyTargets(string target, bool indicator)
    {
        foreach (var modern in new[] { true, false })
        {
            var f = Files(modern);
            if (indicator) f["model.yaml"] = Fixture.Files["model.yaml"].Replace("indicators:", $"indicators:\n  - {{ id: {target}, name: Fixture, base: 0.5 }}");
            var c = Load(f, Card(option: $"    effects: [{{ target: {target}, delta: -0.1 }}]\n"));
            Assert.Equal(target, c.Events.Single().Options.Single().Effects.Single().Target);
        }
    }
    // R-EFX-001: keeping D1's targets adds no alias. A budget or refill spelling model.yaml does not define is still an
    // unknown target under gdd-0.3, as under gdd-0.1.
    [Theory]
    [InlineData("passive_budget")]
    [InlineData("food_refill")]
    public void Gdd03KeptD1TargetsStillNeedARealTarget(string target)
    {
        foreach (var modern in new[] { true, false })
        {
            var ex = Assert.Throws<ContentException>(() => Load(Files(modern), Card(option: $"    effects: [{{ target: {target}, delta: -0.1 }}]\n")));
            Assert.Equal("events[probe].options[ok].effects." + target, ex.Field);
            Assert.EndsWith(": unknown target", ex.Message);
        }
    }
    // R-EFX-001, #1718 Q2 (owner, 1 November 2026): foreign currency stays the internal ledger, so a gdd-0.3 card moves it once.
    [Fact]
    public void Gdd03ForeignCurrencyEffectMovesTheLedgerOnce()
    {
        var c = Load(Files(), Card(option: "    effects: [{ target: fx_reserves, delta: -3 }]\n").Replace("from:", "anchor: hard\nfrom:"));
        var s = Simulation.NewGame(c, 1, Mode.Historical); s.FxReserves = 10;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "probe", "ok", why));
        Assert.Equal(7, s.FxReserves);
        Assert.Contains(why, w => w.Target == "fx_reserves" && w.Delta == -3);
        Assert.NotNull(EventSystem.Choose(c, s, "probe", "ok", why));
        Assert.Equal(7, s.FxReserves);
    }

    [Theory]
    [InlineData("food_supply")]
    [InlineData("housing")]
    [InlineData("fx_reserves")]
    public void OldRulesStillAcceptCompatibilityWrites(string target)
    {
        var c = Load(Files(false), Card(option: $"    effects: [{{ target: {target}, delta: -0.1 }}]\n"));
        Assert.Equal(target, c.Events.Single().Options.Single().Effects.Single().Target);
    }
    [Theory] [InlineData("rationing")] [InlineData("maabarot")]
    public void OldRulesStillAcceptPolicies(string policy) =>
        Assert.Single(Load(Files(false), Card(option: $"    set_policy: {{ {policy}: 0.5 }}\n")).Events);

    static string Exclude(string rows) => "ruleset: gdd-0.3\nexclusions:\n" + rows;
    [Theory]
    [InlineData(null, "(file)")]
    [InlineData("null\n", "(file)")]
    [InlineData("ruleset: legacy\nexclusions: []\n", "ruleset")]
    [InlineData("ruleset: gdd-0.3\n", "exclusions")]
    [InlineData("ruleset: gdd-0.3\nexclusions: null\n", "exclusions")]
    [InlineData("ruleset: gdd-0.3\nexclusions: [{ id: missing, reason: Fixture }]\n", "exclusions[0].id")]
    [InlineData("ruleset: gdd-0.3\nexclusions: [{ id: probe, reason: '' }]\n", "exclusions[0].reason")]
    [InlineData("ruleset: gdd-0.3\nexclusions: [{ id: probe, reason: Fixture }, { id: probe, reason: Fixture }]\n", "exclusions[1].id")]
    public void ManifestFailsClosed(string? text, string field)
    {
        var f = Files(); if (text == null) f.Remove(Manifest); else f[Manifest] = text;
        var ex = Assert.Throws<ContentException>(() => Load(f, Card()));
        Assert.Equal(Manifest, ex.File); Assert.Equal(field, ex.Field);
    }
    [Fact]
    public void ExcludedSourceStillParsesStrictly()
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        Assert.Equal("events/probe.yaml", Assert.Throws<ContentException>(() => Load(f, Card() + "unknown_field: true\n")).File);
    }
    [Theory]
    [InlineData("requires: [{ event: probe, option: ok }]\n", "requires[0].event")]
    [InlineData("kind: reconvergence\nvariants: [{ when: [{ event: probe, diverged: true }], text: Fixture }]\n", "variants[0].when[0].event")]
    public void CannotStrandActiveReferences(string reference, string field)
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f, new()
        {
            ["probe.yaml"] = Card(), ["active.yaml"] = Card("active") + reference
        })));
        Assert.Equal("events/active.yaml", ex.File); Assert.Equal("events[active]." + field, ex.Field);
        Assert.Contains("excluded", ex.Message);
    }
    [Fact]
    public void CannotStrandObjectiveCard()
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        f["objectives.yaml"] = "objectives: [{ id: goal, card: probe }]\n";
        var ex = Assert.Throws<ContentException>(() => Load(f, Card()));
        Assert.Equal("objectives.yaml", ex.File); Assert.Equal("objectives[goal].card", ex.Field);
    }
    [Fact]
    public void CannotStrandRejoin()
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f, new()
        {
            ["probe.yaml"] = Card(), ["active.yaml"] = Card("active", "    rejoin: probe\n")
        })));
        Assert.Equal("events[active].options[ok].rejoin", ex.Field);
    }
    [Fact]
    public void ExcludedDuplicateIdsAreNotHidden()
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f, new()
        { ["a.yaml"] = Card(), ["b.yaml"] = Card() })));
        Assert.Equal("events[probe].id", ex.Field);
    }

    internal static Content Real(bool modern, string? manifest = null)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            if (rel == Manifest && manifest != null) return manifest;
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            return modern && rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }
    [Fact]
    public void RealCatalogueExcludesNoWholeCardAndKeepsOrder()
    {
        var old = Real(false); var modern = Real(true);
        Assert.Equal(41, old.Events.Count);   // Deport's two cards (#801) are not excluded
        Assert.Equal(old.Events.Count - Excluded.Length, modern.Events.Count);
        Assert.Equal(Excluded, old.Events.Select(e => e.Id).Except(modern.Events.Select(e => e.Id)).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(old.Events.Where(e => !Excluded.Contains(e.Id)).Select(e => e.Id), modern.Events.Select(e => e.Id));
        Assert.NotEqual(old.Hash, modern.Hash);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OlderModesNeverReadManifestOrChangeHashSaveOrState(bool gdd)
    {
        var f = gdd ? Files(false) : new Dictionary<string, string>();
        var dir = Fixture.Write(f, new() { ["probe.yaml"] = Card() });
        var reads = new List<string>();
        Content Read(string? manifest) => ContentLoader.Load(rel =>
        {
            reads.Add(rel);
            if (rel == Manifest) return manifest;
            var path = Path.Combine(dir, rel); return File.Exists(path) ? File.ReadAllText(path) : null;
        }, new[] { "events/probe.yaml" });
        var absent = Read(null); var poison = Read("this is deliberately invalid YAML: [");
        Assert.DoesNotContain(Manifest, reads); Assert.Equal(absent.Hash, poison.Hash);
        var a = GameSession.New(absent, 1948, Mode.Historical); var b = GameSession.New(poison, 1948, Mode.Historical);
        Assert.Equal(a.Save.Write(), b.Save.Write()); Assert.Equal(a.State.Hash(), b.State.Hash());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExcludedCardsCannotTriggerChooseOrDelegate(bool delegateChoice)
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        var c = Load(f, Card(option: "    effects: [{ target: treasury, delta: 100 }]\n"));
        var s = Simulation.NewGame(c, 1, Mode.Historical); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Empty(s.FiredEvents); Assert.Empty(s.PendingEvents);
        var money = s.Treasury;
        Assert.NotNull(EventSystem.Choose(c, s, "probe", "ok", why));
        if (delegateChoice) EventSystem.Delegate(c, s, why);
        Assert.Equal(money, s.Treasury); Assert.Empty(s.ChosenOptions);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExplicitStocksAndBorrowingApplyOnceWithoutAttention(bool delegateChoice)
    {
        var c = Load(Files(), Card(option: "    effects: [{ target: food, delta: 2.5 }, { target: arms, delta: -2.5 }, { target: treasury, delta: -4 }]\n").Replace("from:", "anchor: hard\nfrom:"));
        var s = Simulation.NewGame(c, 1, Mode.Historical); s.Food = 1; s.Arms = 1; s.Treasury = 1; s.Debt = 0; s.Attention = double.NaN;
        var why = new List<Cause>();
        if (delegateChoice) EventSystem.Delegate(c, s, why); else Assert.Null(EventSystem.Choose(c, s, "probe", "ok", why));
        Assert.Equal(4, s.Food); Assert.Equal(0, s.Arms); Assert.Equal(0, s.Treasury); Assert.Equal(3, s.Debt);
        Assert.True(double.IsNaN(s.Attention));
        Assert.Contains(why, w => w.Target == "food" && w.Delta == 3);
        Assert.Contains(why, w => w.Target == "arms" && w.Delta == -1);
        Assert.Contains(why, w => w.Target == "debt" && w.Delta == 3);
        Assert.NotNull(EventSystem.Choose(c, s, "probe", "ok", why));
        EventSystem.Delegate(c, s, why); Assert.Equal(4, s.Food); Assert.Equal(3, s.Debt);
    }
    [Theory]
    [InlineData("holocaust")] [InlineData("terror")] [InlineData("massacre")]
    [InlineData("war-casualties")] [InlineData("displacement")]
    public void SensitivePositiveGainsStillFailAndNegativeCostsRemainAllowed(string tag)
    {
        foreach (var target in new[] { "treasury", "fx_reserves", "food", "arms", "morale", "intl_standing" })
        {
            string Record(int delta) => Card(option: FormattableString.Invariant($"    effects: [{{ target: {target}, delta: {delta} }}]\n")) + $"tags: [{tag}]\n";
            var ex = Assert.Throws<ContentException>(() => Load(Files(), Record(1)));
            Assert.Equal("events[probe].options[ok].effects." + target, ex.Field);
            Assert.Contains("sensitive", ex.Message);
            Assert.Single(Load(Files(), Record(-1)).Events);
        }
    }

    // R-EFX-001/002/007: previews name authored deltas; causes name the bounded applied amount.
    [Theory] [InlineData(false)] [InlineData(true)]
    public void RetainedIndicatorsClampOnceThroughChooseAndDelegate(bool delegateChoice)
    {
        var c = Load(Files(), Card(option: "    effects: [{ target: morale, delta: 0.2 }, { target: intl_standing, delta: -0.2 }]\n").Replace("from:", "anchor: hard\nfrom:"));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        EffectsSystem.Shift(c, s, "morale", .99 - s.Indicators["morale"]);
        EffectsSystem.Shift(c, s, "intl_standing", .01 - s.Indicators["intl_standing"]);
        var preview = EventSystem.OptionEffects(c, s, c.Events.Single().Options.Single());
        Assert.Equal(.2, preview.Single(x => x.Target == "morale").Delta);
        Assert.Equal(-.2, preview.Single(x => x.Target == "intl_standing").Delta);
        var why = new List<Cause>();
        if (delegateChoice) EventSystem.Delegate(c, s, why);
        else Assert.Null(EventSystem.Choose(c, s, "probe", "ok", why));
        Assert.Equal(1, s.Indicators["morale"]); Assert.Equal(0, s.Indicators["intl_standing"]);
        Assert.Equal(.01, why.Single(x => x.Target == "morale").Delta, 12);
        Assert.Equal(-.01, why.Single(x => x.Target == "intl_standing").Delta, 12);
        Assert.Equal("ok", s.ChosenOptions["probe"]); Assert.Empty(s.PendingEvents);
        var count = why.Count;
        Assert.NotNull(EventSystem.Choose(c, s, "probe", "ok", why));
        EventSystem.Delegate(c, s, why);
        Assert.Equal(count, why.Count); Assert.Equal(1, s.Indicators["morale"]); Assert.Equal(0, s.Indicators["intl_standing"]);
    }
    // R-EVT-001/002: active hard anchors reserve slots; source order is not runtime order.
    [Fact]
    public void RetainedHardAnchorsFireInOrderAndExcludedAnchorCannotConsumeASoftSlot()
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: excluded, reason: Fixture }\n");
        string Hard(string id) => Card(id).Replace("from:", "anchor: hard\nfrom:");
        var c = ContentLoader.Load(Fixture.Write(f, new()
        {
            ["a.yaml"] = Card("soft_c"), ["b.yaml"] = Hard("hard_b"),
            ["c.yaml"] = Card("soft_a"), ["d.yaml"] = Hard("excluded"),
            ["e.yaml"] = Hard("hard_a"), ["f.yaml"] = Card("soft_b")
        }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "hard_a", "hard_b", "soft_a" }, s.PendingEvents);
        Assert.Equal(s.PendingEvents, s.FiredEvents);
        var next = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Equal(new[] { "soft_b", "soft_c" }, next.PendingEvents);
        Assert.Equal(new[] { "hard_a", "hard_b", "soft_a", "soft_b", "soft_c" }, next.FiredEvents);
        Assert.DoesNotContain("excluded", next.ChosenOptions.Keys);
        Assert.All(new[] { "hard_a", "hard_b", "soft_a" }, id => Assert.Equal("ok", next.ChosenOptions[id]));
    }

    // Inventory from the approved plan for #1772, pinned to ef35703; includes valid sibling fields.
    static readonly string[] Inventory =
    {
        "austerity_1949/impose/set_policy.rationing=1",
        "austerity_1949/impose/effects.food_supply=0.15",
        "austerity_1949/impose/effects.morale=-0.05",
        "austerity_1949/decline/effects.fx_reserves=-8",
        "austerity_1949/decline/effects.food_supply=-0.05",
        "forex_crisis_1951/request_aid/effects.fx_reserves=-45",
        "forex_crisis_1951/request_aid/effects.fx_reserves=15",
        "forex_crisis_1951/request_aid/effects.intl_standing=-0.01",
        "forex_crisis_1951/cut_imports/effects.fx_reserves=-45",
        "forex_crisis_1951/cut_imports/effects.food_supply=-0.05",
        "forex_crisis_1951/cut_imports/effects.morale=-0.04",
        "iraqi_exodus/airlift/effects.housing=-0.04",
        "iraqi_exodus/airlift/effects.morale=0.03",
        "iraqi_exodus/pace/effects.morale=-0.05",
        "iraqi_exodus/pace/effects.intl_standing=-0.02",
        "israel_bonds/launch/effects.fx_reserves=52",
        "israel_bonds/launch/effects.intl_standing=0.02",
        "israel_bonds/donations_only/effects.fx_reserves=10",
        "maabarot_policy/maabarot/set_policy.maabarot=1",
        "maabarot_policy/maabarot/effects.housing=0.04",
        "maabarot_policy/maabarot/effects.morale=-0.02",
        "maabarot_policy/maabarot/effects.treasury=2",
        "maabarot_policy/keep_camps/effects.housing=-0.03",
        "maabarot_policy/keep_camps/effects.treasury=-4",
    };
    [Fact]
    public void AllTwentyFourAffectedAssignmentsRetainTheirLegacyValues()
    {
        // #2483: the four turned-on cards and israel_bonds (#2457) are read as active under gdd-0.3, with the same values.
        Assert.Equal(Inventory.OrderBy(x => x, StringComparer.Ordinal),
            Rows(Real(true), TurnedOn.Append("israel_bonds")).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(Real(false).Events, e => Assert.All(e.Options, o =>
        { Assert.Empty(o.GrantUnits); Assert.Empty(o.CityEffects); Assert.Empty(o.UnlockProjects); }));
    }
    // #2224: the restored cards' rows, the same legacy values the #1772 inventory pinned for them (ef35703).
    static readonly string[] RestoredInventory =
    {
        "clothing_rationing_1950/extend/effects.morale=-0.05",
        "clothing_rationing_1950/relax/effects.morale=0.03",
        "clothing_rationing_1950/relax/effects.fx_reserves=-8",
        "reparations_claim/claim/effects.intl_standing=0",
        "reparations_claim/claim/effects.treasury=-1",
        "reparations_claim/refuse/effects.intl_standing=0",
        "reparations_claim/refuse/effects.fx_reserves=-4",
        "reparations_vote/negotiate/effects.morale=-0.02",
        "reparations_vote/reject/effects.fx_reserves=-4",
        "reparations_vote/referendum/effects.treasury=-1",
    };
    static IEnumerable<string> Rows(Content c, IEnumerable<string> ids) => c.Events.Where(e => ids.Contains(e.Id)).SelectMany(e =>
        e.Options.SelectMany(o => o.SetPolicy.Select(kv => $"{e.Id}/{o.Id}/set_policy.{kv.Key}=" + kv.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture))
            .Concat(o.Effects.Select(fx => $"{e.Id}/{o.Id}/effects.{fx.Target}=" + fx.Delta.ToString("G", System.Globalization.CultureInfo.InvariantCulture)))));
    // #2224: under gdd-0.3 the three restored cards load with their legacy effects and reach the player on the turns
    // EventRetimingTests.EachEventFiresOnItsTurn pins for them in the gdd-0.1 golden run. The reference player is not
    // defined for gdd-0.3 (ReferencePlayer.Commands), so this run gives no orders; the cards' windows still decide the turn.
    [Fact]
    public void RestoredCardsLoadWithLegacyEffectsAndFireOnTheirTurns()
    {
        var c = Real(true);
        Assert.Equal(RestoredInventory.OrderBy(x => x, StringComparer.Ordinal), Rows(c, Restored).OrderBy(x => x, StringComparer.Ordinal));
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.GameOver == null && run.Count < 1000) { s = Simulation.Step(c, s, Array.Empty<Command>()).State; run.Add(s); }
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        DateTime? CardDate(string id) => run.FirstOrDefault(x => x.FiredEvents.Contains(id))?.Date;
        Assert.Equal(new DateTime(1950, 7, 15), CardDate("clothing_rationing_1950"));
        Assert.Equal(new DateTime(1951, 2, 15), CardDate("reparations_claim"));   // #2551: a turn earlier (was 15 March)
        Assert.Equal(new DateTime(1951, 12, 15), CardDate("reparations_vote"));
        // #2457: back under gdd-0.3 on the turn EventRetimingTests pins for it in the gdd-0.1 golden run. As a hard anchor
        // left unanswered in Historical mode it takes launch, so the Debt it adds is the IL equivalent of the card's $52m.
        Assert.Equal(new DateTime(1951, 5, 15), CardDate("israel_bonds"));
        Assert.Equal("launch", s.ChosenOptions["israel_bonds"]);
    }

    // R-EFX-009 (#2457; owner, 2 November 2026, #2224: "Add equal Debt", "Rate in force on the date (Recommended)"): under
    // gdd-0.3 an option's debt_dollars adds that many US$ millions to Debt in IL millions, at the mvp.il_per_dollar rate in
    // force on the event's date. The fixture's rate is 0.5 from 15 May 1948 and 0.25 from 15 August: the card's window
    // ends on 10 August and the turn is 15 August, so the event's date (9 August) gives 0.5, the turn's date would give 0.25.
    // gdd-0.1 reads the field and leaves Debt alone. Red without the effect: Debt stays put under gdd-0.3 too.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DebtDollarsAddDebtAtTheEventsDateRateUnderGdd03Only(bool modern)
    {
        var card = Card(option: "    effects: [{ target: fx_reserves, delta: 52 }]\n    debt_dollars: 52\n").Replace("from: 1948-05-15", "from: 1948-08-01\nto: 1948-08-10");
        var c = Load(Files(modern), card);
        Assert.Equal(modern, c.Ruleset == SaveGame.Gdd03Ruleset);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Date = new DateTime(1948, 8, 15); s.PendingEvents.Add("probe");
        var (fx, debt, treasury) = (s.FxReserves, s.Debt, s.Treasury);
        var preview = EventSystem.OptionEffects(c, s, c.Events.Single(e => e.Id == "probe").Options[0]);
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "probe", "ok", why));
        Assert.Equal(fx + 52, s.FxReserves);
        Assert.Equal(treasury, s.Treasury);
        Assert.Equal(debt + (modern ? 26 : 0), s.Debt, 9);
        var lines = why.Where(x => x.Target == "debt").ToList();
        var shown = preview.Where(x => x.Target == "debt").ToList();
        if (!modern) { Assert.Empty(lines); Assert.Empty(shown); return; }
        var line = Assert.Single(lines);
        Assert.Equal((CauseKeys.EventAmount, 26.0), (line.Key, line.Delta));
        Assert.StartsWith("Debt", line.Message);
        Assert.EndsWith("(Fixture)", line.Message);
        Assert.Equal((CauseKeys.EventAmount, 26.0), (Assert.Single(shown).Key, shown[0].Delta));
    }
    // R-EFX-009, R-EVT-003: a disloyal minister's half share halves the Debt with the foreign currency.
    [Fact]
    public void DelegatedHalfShareHalvesTheDebt()
    {
        var c = Load(Files(), Card(option: "    effects: [{ target: fx_reserves, delta: 4 }]\n    debt_dollars: 4\n") + "office: prime_minister\n");
        foreach (var p in c.People) p.Loyalty = 0;
        var s = Simulation.NewGame(c, 1, Mode.Historical); s.Debt = 0;
        var fx = s.FxReserves;
        EventSystem.Delegate(c, s, new List<Cause>());
        Assert.Equal((fx + 2, 1.0), (s.FxReserves, s.Debt));
    }
    // R-EFX-009: the loader checks the field in every ruleset, since every ruleset reads the same cards.
    [Theory]
    [InlineData(true, "0")]
    [InlineData(true, "-52")]
    [InlineData(true, ".nan")]
    [InlineData(false, ".inf")]
    public void DebtDollarsMustBeAFinitePositiveAmount(bool modern, string value)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Files(modern), Card(option: $"    debt_dollars: {value}\n")));
        Assert.Equal(("events/probe.yaml", "events[probe].options[ok].debt_dollars"), (ex.File, ex.Field));
    }
    // R-EFX-009 (review on #2487): a value that is not a number is a content error in every ruleset, reported at
    // the card's file and the debt_dollars line (line 9 of the fixture), before any ruleset check runs.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DebtDollarsThatIsNotANumberIsRefused(bool modern)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Files(modern), Card(option: "    debt_dollars: nope\n")));
        Assert.Equal(("events/probe.yaml", "debt_dollars (line 9)"), (ex.File, ex.Field));
        Assert.Contains("nope", ex.Message);
    }
    // R-EFX-009 (#2457): on the real card, launch adds the $52m and IL 18.564m of Debt (52 x 0.357, the rate in force on
    // 10 May 1951); donations_only adds its $10m and no Debt (owner: "No Debt for donations; use 52 (Recommended)").
    [Theory]
    [InlineData("launch", 52, 18.564)]
    [InlineData("donations_only", 10, 0)]
    public void IsraelBondsLaunchAddsEqualDebtAndDonationsAddNone(string option, double dollars, double debt)
    {
        var c = Real(true);
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        s.Date = new DateTime(1951, 5, 15); s.PendingEvents.Add("israel_bonds");
        var (fx, before) = (s.FxReserves, s.Debt);
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "israel_bonds", option, why));
        Assert.Equal(fx + dollars, s.FxReserves, 9);
        Assert.Equal(before + debt, s.Debt, 9);
        Assert.Equal(debt == 0 ? 0 : 1, why.Count(x => x.Target == "debt" && x.Key == CauseKeys.EventAmount));
    }
    // #2483: on the real content under gdd-0.3 the four cards are in the catalogue and reach the player on the turns
    // EventRetimingTests.EachEventFiresOnItsTurn pins for them in the gdd-0.1 golden run (no orders, as above).
    [Fact]
    public void Gdd03FourCardsAreActive()
    {
        var c = Real(true);
        Assert.All(TurnedOn, id => Assert.Contains(c.Events, e => e.Id == id));
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.GameOver == null && run.Count < 1000) { s = Simulation.Step(c, s, Array.Empty<Command>()).State; run.Add(s); }
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        DateTime? CardDate(string id) => run.FirstOrDefault(x => x.FiredEvents.Contains(id))?.Date;
        Assert.Equal(new DateTime(1949, 4, 15), CardDate("austerity_1949"));   // #2551: a turn earlier (was 15 May)
        Assert.Equal(new DateTime(1950, 4, 15), CardDate("maabarot_policy"));
        Assert.Equal(new DateTime(1951, 2, 15), CardDate("iraqi_exodus"));   // #2833: a turn earlier (was 15 March)
        Assert.Equal(new DateTime(1951, 10, 15), CardDate("forex_crisis_1951"));   // #2833: a turn earlier (was 15 November)
    }
    static readonly string[] SourceBytes =
    {
        "absorb_immigrants:95682d1835e8de59e3e69f56a9beb13ddf91447270a672d23eaff2523898fea1", // re-pinned for #1065 (reviewer comment matches the text), then for the stale reviewer comment (comment-only)
        "altalena:83f547759e5a991e78c32240c2ee3551b9b8ad9acdad8c135621b56805895f8d", // re-pinned for #1065 (owner's native read; flag removed in 8fbcb2aff), then for the stale reviewer comment (comment-only)
        "armistice_egypt:253a579e5de78fd3648b582b3f2329ca2947100df5568db74ff2a1819442a782", // re-pinned for #1065 (Beri's native Hebrew read), then for #2478's front_quiet key (no text, date, option or effect changed), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "armistice_jordan:2d5b7eb8721ca6afa5aba7bd680b2f71688139b84a581cd8e26b1e597c792a91", // re-pinned for #1065 (Beri's native Hebrew read), then for #2478's front_quiet key (no text, date, option or effect changed), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "armistice_lebanon:5ce3ba85f056294691be4ac345896a3222a8898042491bbb989a281b8bc6a882", // re-pinned for #1065 (Beri's native Hebrew read), then for #2478's front_quiet key (no text, date, option or effect changed), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "armistice_syria:b552b74029c98058bd6b7f8b4a62f815539cc766023fa99efb4c01adbcb06824", // re-pinned for #1065 (Beri's native Hebrew read), then for #2478's front_quiet key (no text, date, option or effect changed), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "austerity_1949:94454d82815b0507236a206d1f834a0af25e2483990bb842f6085e04327ed928", // re-pinned for #1065 (Beri's native Hebrew read), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "bernadotte_assassination:3b7bce0f1707180fffdd254a6848efbb095cdf38dd70c7b31e83475c34623ec2", // re-pinned for #1065 (Beri's native Hebrew read), then for the stale reviewer comment (comment-only)
        "burma_road_complete:6c49179b3844761c6f580c1436596cbace6932cbb6b620a9ea2872b6e0342ba2", // re-pinned for #1065 (owner's native read; flag removed in 8fbcb2aff), then for the stale reviewer comment (comment-only)
        "clothing_rationing_1950:ce2d24a8a98ad86a1670e62094d18fca9da71b5cd11f437cd5314a09f4a1d562", // re-pinned for #1065 (Beri's native Hebrew read)
        "degania_defence:f915a42f050777ddffd7e7e26090b51c6cf922dade41a98fe7174b1c50a62c6e", // re-pinned for #2020 (owner-approved Hebrew wording, 30 October 2026), then for #1065 (Beri's native Hebrew read)
        "deport_acre:950dfb6a76dd47d1b2c132cac22ee67c6db956dff16592fe8036f3b43010414f", // added for #801 (Deport's follow-up cards), re-pinned for #2197 review round 2 (comments say "adapted") and the owner's sign-off (status sensitivity-reviewed, 2 Nov 2026), then for the stale DRAFT note (comment-only cleanup) and the owner's Hebrew read (flag removed), both #2292
        "deport_nazareth:033bc3da1cd0bc93263a69c9e537ee48556eb962d416812fbba3d577475fe9cd", // added for #801 (Deport's follow-up cards), re-pinned for #2197 ("near the town", source packet N5; review round 2 comments) and the owner's sign-off (status sensitivity-reviewed, 2 Nov 2026), then for the stale DRAFT note (comment-only cleanup) and the owner's Hebrew read (flag removed), both #2292, then for two citation comments that now point inside the repo (comment-only)
        "education_registration_1951:d0d21344c5c198f3477f1736b402ddd3d3444423198f66f8bb97f3d72f2d72df", // re-pinned for #1065 (Beri's native Hebrew read), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "elections_1949:962bd8064e8b98d4919be9b4ff4fab68cb9bd3d15213ea43917caa4432f42116", // re-pinned for #1065 (Beri's native Hebrew read)
        "first_new_yishuv:0c75e086931ddbd0d356b8691afffb914ba1e657475d9295375baa3ee32c66b9", // re-pinned for #1065 (owner's native read; flag removed in 8fbcb2aff), then for the stale reviewer comment (comment-only)
        "first_truce:51240c2fc8b3b944b4698cf15840652c4b3e15efe37ca14fcd398d4acec0fcb8", // re-pinned for #1065 (Beri's native Hebrew read), then for the stale reviewer comment (comment-only)
        "forex_crisis_1951:8a200332ee0788ca8f32b40091eb9b8c6460c7b7f6a02234a4a9c9eba0a5a1fd", // re-pinned for #1065 (Beri's native Hebrew read), then for #2833 (R-TURN-002: `from` a turn earlier and its comment)
        "frumkin_commission:c7bfb21991dc41bfcbaaf4778c47c5226913a8289ba208474c87128768ba622c", // re-pinned for #1065 (Beri's native Hebrew read)
        "hula_drainage:b7ec7159b5cb6ecc009e1ff7e12a7f2426ee1c4b992b8c91c4675c8df3cc1bf5", // re-pinned for #1065 (Beri's native Hebrew read)
        "idf_founding:caad7affcf0309a41c56956c10dd15936ec319ac996ddff6f2cd85efbcebc51e", // re-pinned for #1065 (Beri's native Hebrew read)
        "iraqi_exodus:ac4fe1b69f39fbd3529606de2221d23ef722de69a2b10c40f8411027657652ee", // re-pinned for #1065 (Beri's native Hebrew read), then for #2833 (R-TURN-002: `from` a turn earlier and its comment)
        "israel_bonds:db9b8cf0754fd62166effed2ee3b563aa06bfdba7cf6d43f4965cab44997786a", // re-pinned for #1065 (Beri's native Hebrew read), then for #2457 (R-EFX-009: launch's debt_dollars: 52 and its comment; owner, 2 November 2026, #2224, "Add equal Debt"), then for the donations-only wording and pending Hebrew review marker
        "jerusalem_ceasefire:b850682a7736dcf2870550322f32a2d5c30963a05529dc9162dd68c9234648d6", // re-pinned for #1065 (Beri's native Hebrew read)
        "jerusalem_demilitarisation_guard:a9693784deafaad901268afd8a0c47a2f5b7d5f75eba33d6180f94b62913fb53", // re-pinned for #1065 (owner's native read; flag removed in 05b5777d9), then for the stale reviewer comment (comment-only)
        "jerusalem_military_government:526dbc88eabe4691e3ee1f3ed2c5fdeb640f11ae0a7a632c63066175b9c18176", // re-pinned for #1065 (Beri's native Hebrew read)
        "latrun_proposal:c35f17ffa48d949b432ce9205a245048bd50bf660940206245e8f0d6f5b62916", // re-pinned for #1065 (Beri's native Hebrew read), then for #2087 (R-ENEMY-005 unit order: Harel toward Latrun; owner rulings, 1 and 2 November 2026, order targets; hex re-derived on the 4.4 km grid by tools/unit_anchor_hexes.py and its units_1948.yaml line citations updated; card text, Hebrew and review status unchanged)
        "law_of_return:d5d7f352eaa63e8f57a0f3fcdcbba82791e4ea3cd267afd8adee9d6b01a2f052", // re-pinned for #1065 (Beri's native Hebrew read), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "maabarot_policy:12e53e14fe3b045f7c18b75ac7af68acf079c17968baf6c82df2435675eb0884", // re-pinned for #1065 (Beri's native Hebrew read)
        "ministry_supply_crisis:2de253de7bafb65f0168bdc151dc0b47fec94843ad6f13bf3ac092d7015b55f9", // re-pinned for #1065 (Beri's native Hebrew read)
        "operation_hiram:e7c661e53f93576d8a135008e0d1e623430967a36729b58b67a4af195165bccf", // re-pinned for #1065 (Beri's native Hebrew read), then for #2087 (R-ENEMY-005 unit orders: Carmeli to Manara, the 7th Brigade to al-Malikiyya; owner rulings, 1 and 2 November 2026, order targets; hexes re-derived on the 4.4 km grid by tools/unit_anchor_hexes.py and its units_1948.yaml line citations updated; card text, Hebrew and review status unchanged)
        "operation_horev:373bf144ad21c326eed5895d7bf57b2d73e922c8589bab47da812e2f96f5fb45", // re-pinned for #1065 (Beri's native Hebrew read), then for #2087 (R-ENEMY-005 unit orders; owner ruling, 31 October 2026; hex re-derived on the 4.4 km grid by tools/unit_anchor_hexes.py; review status unchanged), then re-pinned for the Horev garrison line (owner, 2 November 2026, "Add the line (Recommended)", #2087)
        "operation_yoav:c4af1976488006f26f06cddaa6a9e3379d0193f5c5c22c8e789ccff64dabfefc", // re-pinned for #1065 (Beri's native Hebrew read), then for #2087 (R-ENEMY-005 unit orders: Negev to Beersheba, Givati to Huleiqat; owner rulings, 1 and 2 November 2026, order targets; hexes re-derived on the 4.4 km grid by tools/unit_anchor_hexes.py and its units_1948.yaml line citations updated; card text, Hebrew and review status unchanged)
        "rafah_force_pressure:37ca577e1b93f241035d8bf322810f311a084427307c5722f4122238aa9ec6b8", // re-pinned for #1065 (owner's native read; flag removed in 05b5777d9), then for the stale reviewer comment (comment-only)
        "railway_complete:98d06d62c88d889e3e4a98f954fb04727695c37f58c6ccef94449fc16e88bd04", // re-pinned for #1065 (owner's native read; flag removed in 8fbcb2aff), then for the stale reviewer comment (comment-only)
        "reparations_claim:0aa053313d9385bc8746fa1d7318883aab3d1d6706318ad978fc4cd97d98d898", // re-pinned for #1065 (Beri's native Hebrew read), then for #2551 (R-TURN-002: `from` a turn earlier and its comment)
        "reparations_vote:a79521d61ec59aa72a581b6598ecfade18e8d1e52dc25cef083078b0665a13b3",
        "rhodes_talks:c5c9cdf906f75e2672e5eedaeadab7f224df96f666a3097813903b81ec06f4f6", // re-pinned for #1065 (Beri's native Hebrew read)
        "second_truce:22eeb171849bac248d58cec944c03bac6aee56c96a192aabf0aba2b7e02b19f6", // re-pinned for #1065 (Beri's native Hebrew read)
        "sinai_withdrawal:4abcd39e509c3d404523f7a5609bdb8e1e5180c19524367790ba7adf0af9e06b", // re-pinned for #1065 (Beri's native Hebrew read)
        "ten_days:0be9dc54863d46b935428e7a6fd1c9d8fe21194960c976d3ed4283b9e1088962", // re-pinned for #1065 (Beri's native Hebrew read), then for #2087 (R-ENEMY-005 unit orders: Kiryati to Ramle replaces Yiftach to Lydda, sent to Ramla's capture target; owner rulings, 1 and 2 November 2026, order target; hexes re-derived on the 4.4 km grid by tools/unit_anchor_hexes.py and its units_1948.yaml line citations updated; card text, Hebrew and review status unchanged)
    };
    // R-ENEMY-005 (owner, 31 October 2026, #749: "GDD 0.1 only"): the operation cards' unit orders apply under gdd-0.1; under
    // gdd-0.3 the cards stay effect-free for units until a 0.3 accounting contract exists (R-EFX-001/005).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OperationCardOrdersApplyUnderGdd01Only(bool modern)
    {
        var c = Real(modern);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal("golani", c.StartingForces[0].Row);
        var before = s.Units[0];
        s.PendingEvents.Add("operation_horev");
        Assert.Null(EventSystem.Choose(c, s, "operation_horev", "launch", new List<Cause>()));
        var after = s.Units[0];
        Assert.Equal(modern ? (before.Order, before.OrderTarget) : (Movement.GoTo, "-8,28"), (after.Order, after.OrderTarget));
    }
    // R-ENEMY-005, R-CTRL-013 (owner, 2 November 2026, #2087: "Keep a garrison in Nazareth (Recommended)"; owner, 31 October
    // 2026, #749: "GDD 0.1 only"): Golani alone on Nazareth in the `occupied` state, its timer still running (the EnemyAITests
    // cases are all `awaiting_end`), and Horev launched. Under gdd-0.1 the order is held back with Orders.RefusedLastGarrison
    // and Golani stays; under gdd-0.3 the card gives no unit order, so nothing is refused, RefusedLastGarrison never appears,
    // and Golani is untouched.
    // Red without the rule's `occupied` half (SettlementControl.LastGarrison reading only `awaiting_end`): under gdd-0.1 the
    // order is given, or refused with another reason, so no RefusedLastGarrison names unit 0. Red for a check made outside
    // the gdd-0.3 guard (EventSystem.Resolve): under gdd-0.3 a RefusedLastGarrison would appear.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheLastGarrisonRuleHoldsForAnOccupiedPlaceUnderGdd01Only(bool modern)
    {
        var c = Real(modern);
        Assert.Equal(modern, c.Ruleset == SaveGame.Gdd03Ruleset);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal("golani", c.StartingForces[0].Row);
        var nazareth = c.SettlementTargets.Single(t => t.Id == "nazareth");
        var g = s.Units[0];
        s.Units[0] = new Unit(g.Id, g.Type, nazareth.Q, nazareth.R, g.Service, g.Side, g.Strength, "", "", g.HomeCity, g.Full, g.CombatXp);
        Assert.Equal(new[] { 0 }, s.Units.Values.Where(u => (u.Q, u.R) == (nazareth.Q, nazareth.R) && u.Side == Movement.PlayerSide).Select(u => u.Id));
        s.Settlements["nazareth"] = new SettlementControl(Movement.PlayerSide, SettlementControl.Occupied, s.Date, turns: 3);
        s.PendingEvents.Add("operation_horev");
        var rejected = new List<Rejection>();
        Assert.Null(EventSystem.Choose(c, s, "operation_horev", "launch", new List<Cause>(), rejected: rejected));
        Assert.Equal(modern ? Array.Empty<string>() : new[] { Orders.RefusedLastGarrison },
            rejected.Where(j => j.Command is UnitOrder { Unit: 0 }).Select(j => j.Reason));
        if (modern) Assert.DoesNotContain(rejected, j => j.Reason == Orders.RefusedLastGarrison);
        Assert.Equal((nazareth.Q, nazareth.R, "", ""), (s.Units[0].Q, s.Units[0].R, s.Units[0].Order, s.Units[0].OrderTarget));
        Assert.False(s.CardOrders.ContainsKey(0));
        Assert.Equal(SettlementControl.Occupied, s.Settlements["nazareth"].State);
    }
    // Review of #2087, optional 2: under gdd-0.3 the loader still checks the cards' orders (owner, 31 October 2026, #749), so
    // a Horev order to a hex that is not one of golani's anchors is refused there too.
    [Fact]
    public void Gdd03StillRefusesACardOrderToAHexNoAnchorHas()
    {
        var root = RealContentTests.ContentRoot();
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            if (rel == "eras.yaml") return text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            return rel == "events/operation_horev.yaml" ? text!.Replace("{ unit: golani, q: -8, r: 28 }", "{ unit: golani, q: -8, r: 27 }") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p))));
        Assert.Equal(("events/operation_horev.yaml", "events[operation_horev].options[launch].unit_orders[0]"), (e.File, e.Field));
        Assert.Contains("not an anchor", e.Message);
    }
    [Fact]
    public void AllFortyOneSourceRecordsIncludingReviewsAndHebrewAreUnchanged()
    {
        var root = Path.Combine(RealContentTests.ContentRoot(), "events");
        var files = Directory.GetFiles(root, "*.yaml").OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(SourceBytes.Select(x => x.Split(':')[0]), files.Select(Path.GetFileNameWithoutExtension));
        for (int i = 0; i < files.Length; i++)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(files[i]).Replace("\r\n", "\n"));
            Assert.Equal(SourceBytes[i].Split(':')[1], Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }
    }
    [Fact]
    public void DelegatedHalfShareRoundsAfterScalingAndBorrows()
    {
        var c = Load(Files(), Card(option: "    effects: [{ target: food, delta: 5 }, { target: treasury, delta: -4 }]\n") + "office: prime_minister\n");
        foreach (var p in c.People) p.Loyalty = 0;
        var s = Simulation.NewGame(c, 1, Mode.Historical); s.Food = 0; s.Treasury = 0; s.Debt = 0;
        var why = new List<Cause>(); EventSystem.Delegate(c, s, why);
        Assert.Equal(3, s.Food); Assert.Equal(2, s.Debt);
        Assert.Equal("ok", s.ChosenOptions["probe"]);
    }
    [Fact]
    public void PreviewAndSaveReplayApplyOneGrantAndRejectDuplicateChoice()
    {
        var c = Load(Files(), Card(option: "    effects: [{ target: food, delta: 3 }]\n"));
        var session = GameSession.New(c, 1948, Mode.Historical);
        var command = new ChooseEventOption("probe", "ok");
        var preview = Simulation.Preview(c, session.State, new Command[] { command });
        var direct = session.State.Clone(); var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, direct, "probe", "ok", why));
        Assert.Equal(direct.Hash(), preview.State.Hash());
        Assert.Equal(EventSystem.OptionEffects(c, session.State, c.Events.Single().Options.Single()).Single().Delta,
            why.Single(w => w.Target == "food").Delta);
        Assert.Empty(session.Step(new Command[] { command }).Rejected);
        var written = session.Save.Write(); var restored = GameSession.Load(c, written, out var warnings);
        Assert.Empty(warnings); Assert.Equal(written, restored.Save.Write()); Assert.Equal(session.State.Hash(), restored.State.Hash());
        var duplicate = Simulation.Preview(c, restored.State, new Command[] { command });
        Assert.Single(duplicate.Rejected); Assert.Equal(restored.State.Hash(), duplicate.State.Hash());
    }

    [Theory]
    [InlineData("food", ".nan", ".delta")]
    [InlineData("arms", ".inf", ".delta")]
    [InlineData("treasury", "-.inf", ".delta")]
    [InlineData("morale", ".nan", ".delta")]
    [InlineData("food", "3000000000", "")]
    [InlineData("arms", "-3000000000", "")]
    public void ApprovedTargetsRetainFiniteAndStockRangeChecks(string target, string delta, string suffix)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Files(), Card(option: $"    effects: [{{ target: {target}, delta: {delta} }}]\n")));
        Assert.Equal("events/probe.yaml", ex.File);
        Assert.Equal("events[probe].options[ok].effects." + target + suffix, ex.Field);
    }
    [Theory]
    [InlineData("ruleset: gdd-0.3\nexclusions: [{ id: probe, reason: Fixture, typo: true }]\n")]
    [InlineData("ruleset: gdd-0.3\nexclusions: [null]\n")]
    [InlineData("ruleset: gdd-0.3\nexclusions: [\n")]
    public void MalformedManifestIsAContentError(string text)
    {
        var f = Files(); f[Manifest] = text;
        Assert.Equal(Manifest, Assert.Throws<ContentException>(() => Load(f, Card())).File);
    }
    [Fact]
    public void ExcludedNullOptionIsStillAnError()
    {
        var f = Files(); f[Manifest] = Exclude("  - { id: probe, reason: Fixture }\n");
        Assert.Equal("events/probe.yaml", Assert.Throws<ContentException>(() => Load(f, Card().Replace("  - id: ok", "  - null\n  - id: ok"))).File);
    }
}
