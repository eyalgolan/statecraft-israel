using Statecraft.Sim;
using System.Security.Cryptography;

// R-EFX-001/002/005/007, R-MONEY-003: identity comes from eras, never a post-load switch.
public class Gdd03EventRulesTests
{
    const string Manifest = "gdd03-events.yaml";
    const string Empty = "ruleset: gdd-0.3\nexclusions: []\n";
    static readonly string[] Excluded = { "austerity_1949", "clothing_rationing_1950", "forex_crisis_1951", "iraqi_exodus", "israel_bonds", "maabarot_policy", "reparations_claim", "reparations_vote" };
    static string Card(string id = "probe", string option = "") =>
        $"id: {id}\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\noptions:\n  - id: ok\n    text: Accept\n    historical: true\n" + option;
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
    [Fact] public void Gdd03RejectsPassiveBudgetEffect() => Effect("budget", true);
    [Fact] public void Gdd03RejectsAutomaticFoodRefillEffect() => Effect("food_refill", true);
    [Fact] public void Gdd03RejectsFoodSupplyWrite() => Effect("food_supply");
    [Fact] public void Gdd03RejectsHousingCompatibilityWrite() => Effect("housing");
    [Fact] public void Gdd03RejectsFxEffectWithoutApprovedLedger() => Effect("fx_reserves");
    [Fact] public void Gdd03RejectsRetiredRationingPolicy() => Reject("    set_policy: { rationing: 0.5 }\n", "set_policy.rationing");
    [Fact] public void Gdd03RejectsRetiredMaabarotPolicy() => Reject("    set_policy: { maabarot: 0.5 }\n", "set_policy.maabarot");
    [Fact] public void Gdd03RejectsCityLossDefeatEffect() => Effect("city_loss_defeat", true);
    [Fact] public void Gdd03RejectsUnaccountedPopulationGrant() => Reject("    grant_units: [{ type: halutz, n: 1, at: town_a }]\n", "grant_units");
    [Fact] public void Gdd03RejectsUnaccountedFacilityEffect() => Reject("    city_effects: [{ city: town_a, field: housing_capacity, delta: 1 }]\n", "city_effects");
    [Fact] public void Gdd03RejectsUnaccountedProjectUnlock() => Reject("    unlock_projects: [housing]\n", "unlock_projects",
        f => f["projects.yaml"] = Fixture.GddProjects.Replace("id: housing,", "id: housing, locked: true,"));

    [Theory]
    [InlineData("attention_per_turn")]
    [InlineData("budget_finance_minister")]
    [InlineData("passive_budget")]
    [InlineData("automatic_food_refill")]
    [InlineData("defeat")]
    [InlineData("any_city_loss")]
    public void CompatibilityIndicatorsCannotEnableCommandTargets(string target) => Effect(target, true);

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

    static Content Real(bool modern, string? manifest = null)
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
    public void RealCatalogueExcludesExactlyEightWholeCardsAndKeepsOrder()
    {
        var old = Real(false); var modern = Real(true);
        Assert.Equal(39, old.Events.Count); Assert.Equal(31, modern.Events.Count);
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
        foreach (var target in new[] { "treasury", "food", "arms", "morale", "intl_standing" })
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
    // reparations_vote (#1575) joined after the main merge, excluded for the same reason as reparations_claim.
    static readonly string[] Inventory =
    {
        "austerity_1949/impose/set_policy.rationing=1",
        "austerity_1949/impose/effects.food_supply=0.15",
        "austerity_1949/impose/effects.morale=-0.05",
        "austerity_1949/decline/effects.fx_reserves=-8",
        "austerity_1949/decline/effects.food_supply=-0.05",
        "clothing_rationing_1950/extend/effects.morale=-0.05",
        "clothing_rationing_1950/relax/effects.morale=0.03",
        "clothing_rationing_1950/relax/effects.fx_reserves=-8",
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
        "reparations_claim/claim/effects.intl_standing=0",
        "reparations_claim/claim/effects.treasury=-1",
        "reparations_claim/refuse/effects.intl_standing=0",
        "reparations_claim/refuse/effects.fx_reserves=-4",
        "reparations_vote/negotiate/effects.morale=-0.02",
        "reparations_vote/reject/effects.fx_reserves=-4",
        "reparations_vote/referendum/effects.treasury=-1",
    };
    [Fact]
    public void AllThirtyFourAffectedAssignmentsRetainTheirLegacyValues()
    {
        var rows = Real(false).Events.Where(e => Excluded.Contains(e.Id)).SelectMany(e =>
            e.Options.SelectMany(o => o.SetPolicy.Select(kv => $"{e.Id}/{o.Id}/set_policy.{kv.Key}=" + kv.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture))
                .Concat(o.Effects.Select(fx => $"{e.Id}/{o.Id}/effects.{fx.Target}=" + fx.Delta.ToString("G", System.Globalization.CultureInfo.InvariantCulture)))));
        Assert.Equal(Inventory.OrderBy(x => x, StringComparer.Ordinal), rows.OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(Real(false).Events, e => Assert.All(e.Options, o =>
        { Assert.Empty(o.GrantUnits); Assert.Empty(o.CityEffects); Assert.Empty(o.UnlockProjects); }));
    }
    static readonly string[] SourceBytes =
    {
        "absorb_immigrants:dbab4d5af95361ac47b8f72894780ca1779b283e99cea8e96d19c4c9e96de135", // re-pinned for #1065 (Beri's native Hebrew read)
        "altalena:d2c85de2ba034928441a67e490862b265f693d005e000553622bd80fbcb7ede8", // re-pinned for #1065 (Beri's native Hebrew read)
        "armistice_egypt:e4b745fcd6753398d5a988e26f9e84dc7c0bbb303e52315e07c6b2202fa8dbee", // re-pinned for #1065 (Beri's native Hebrew read)
        "armistice_jordan:3356343149b53b723ee73698308d7b3474fb026d97f9c26d332e953240f83ef2", // re-pinned for #1065 (Beri's native Hebrew read)
        "armistice_lebanon:7aa5b70927057078437be5e36a0b5e35a3ae4d86fd45b02425082281bacd9a55", // re-pinned for #1065 (Beri's native Hebrew read)
        "armistice_syria:1573d6425173e532b18028bd1910dccb93f0d5532dc8ba793a64689a57f4d945", // re-pinned for #1065 (Beri's native Hebrew read)
        "austerity_1949:7b4534f4982116745eef2ab2f959ad52faf6efa8efb7f0f5bda3844bac590cc4", // re-pinned for #1065 (Beri's native Hebrew read)
        "bernadotte_assassination:ea0f988df86957b3e56bf6cd0322f5d33adc927d969509e94ec67aa53e7ed0c5", // re-pinned for #1065 (Beri's native Hebrew read)
        "burma_road_complete:f48054bb7720b93b582cae4e168f13595a4360ee62e91e12815df08de384e6e3",
        "clothing_rationing_1950:ce2d24a8a98ad86a1670e62094d18fca9da71b5cd11f437cd5314a09f4a1d562", // re-pinned for #1065 (Beri's native Hebrew read)
        "degania_defence:f915a42f050777ddffd7e7e26090b51c6cf922dade41a98fe7174b1c50a62c6e", // re-pinned for #2020 (owner-approved Hebrew wording, 30 October 2026), then for #1065 (Beri's native Hebrew read)
        "education_registration_1951:1d5cb0e4062dc61b787a7e1b375b3c85c9a1663bd4c3c73f3c9c6f9ee5ac4da3", // re-pinned for #1065 (Beri's native Hebrew read)
        "elections_1949:962bd8064e8b98d4919be9b4ff4fab68cb9bd3d15213ea43917caa4432f42116", // re-pinned for #1065 (Beri's native Hebrew read)
        "first_new_yishuv:9edf9e0da02c2feefe11eb52aa056ee64af60a41ac3fa2c283ff976f33d0e0c8",
        "first_truce:fb78d2e82393d5cfb19e56938965c7838c73426f5a23d053d612bc74f8010062", // re-pinned for #1065 (Beri's native Hebrew read)
        "forex_crisis_1951:53fee056940af610e16f7793d66d38f6470027a63d0c5968c0cc748eb1dc2af0", // re-pinned for #1065 (Beri's native Hebrew read)
        "frumkin_commission:c7bfb21991dc41bfcbaaf4778c47c5226913a8289ba208474c87128768ba622c", // re-pinned for #1065 (Beri's native Hebrew read)
        "hula_drainage:b7ec7159b5cb6ecc009e1ff7e12a7f2426ee1c4b992b8c91c4675c8df3cc1bf5", // re-pinned for #1065 (Beri's native Hebrew read)
        "idf_founding:caad7affcf0309a41c56956c10dd15936ec319ac996ddff6f2cd85efbcebc51e", // re-pinned for #1065 (Beri's native Hebrew read)
        "iraqi_exodus:461dbb8a0685b7dceb6242d8e819635a27bb10033463e66d1a2e3ac68c9b8521", // re-pinned for #1065 (Beri's native Hebrew read)
        "israel_bonds:9bb4e8f38b8c9dc41b339645457efe1ce8d74580c53556faf4c81df50f67a992", // re-pinned for #1065 (Beri's native Hebrew read)
        "jerusalem_ceasefire:b850682a7736dcf2870550322f32a2d5c30963a05529dc9162dd68c9234648d6", // re-pinned for #1065 (Beri's native Hebrew read)
        "jerusalem_demilitarisation_guard:93cec80c25ad328a0382c3178de10e133ea9a506ec78b3e038b7e25b7f6e2d62", // re-pinned for #2020 (owner-approved Hebrew wording, owner re-review bc139d5eb)
        "jerusalem_military_government:526dbc88eabe4691e3ee1f3ed2c5fdeb640f11ae0a7a632c63066175b9c18176", // re-pinned for #1065 (Beri's native Hebrew read)
        "latrun_proposal:169933e28b8fc3085c5e74176a731d83f1e1955ed0ffac9a706029f738353bf5", // re-pinned for #1065 (Beri's native Hebrew read)
        "law_of_return:d04bb849bbb6fa089afc6e1123f8335aa1b03f49bc025f9d1c6746c48d27b398", // re-pinned for #1065 (Beri's native Hebrew read)
        "maabarot_policy:12e53e14fe3b045f7c18b75ac7af68acf079c17968baf6c82df2435675eb0884", // re-pinned for #1065 (Beri's native Hebrew read)
        "ministry_supply_crisis:2de253de7bafb65f0168bdc151dc0b47fec94843ad6f13bf3ac092d7015b55f9", // re-pinned for #1065 (Beri's native Hebrew read)
        "operation_hiram:c8db23f9a331b0cf517e4762fe8f71dc286548ada77115b9500a93bce6ba740e", // re-pinned for #1065 (Beri's native Hebrew read)
        "operation_horev:6e018d54d46b46676d5ab56bc2fcedbeffab23a00334f75b415aec1713507f51", // re-pinned for #1065 (Beri's native Hebrew read)
        "operation_yoav:3d2e9a9a4416a4277e4c17da1e442895bda562c238e6f459c45b6ab89a9133fe", // re-pinned for #1065 (Beri's native Hebrew read)
        "rafah_force_pressure:658b99340f0cf7dbf042e64d131509b87cec48f4374b16a1337fd0f831dbd28d", // re-pinned for #2020 (owner-approved Hebrew wording, owner re-review bc139d5eb)
        "railway_complete:2108338a93d663ec9cb06bcb474c1f1211cfff480f9ef0acaec9c758a4854a7c", // re-pinned for #1676 (person's fact-check of #1410, status draft -> fact-checked)
        "reparations_claim:5412e71f4f5a90a92bd86622eb1a169feee45f0ba2fe82c5ab40bd8f3d43cafd", // re-pinned for #1065 (Beri's native Hebrew read)
        "reparations_vote:a79521d61ec59aa72a581b6598ecfade18e8d1e52dc25cef083078b0665a13b3",
        "rhodes_talks:c5c9cdf906f75e2672e5eedaeadab7f224df96f666a3097813903b81ec06f4f6", // re-pinned for #1065 (Beri's native Hebrew read)
        "second_truce:22eeb171849bac248d58cec944c03bac6aee56c96a192aabf0aba2b7e02b19f6", // re-pinned for #1065 (Beri's native Hebrew read)
        "sinai_withdrawal:4abcd39e509c3d404523f7a5609bdb8e1e5180c19524367790ba7adf0af9e06b", // re-pinned for #1065 (Beri's native Hebrew read)
        "ten_days:7fece9c0eb4f56ece79a7247e430fb679d3d4d826165e598fd0bd982099a412a", // re-pinned for #1065 (Beri's native Hebrew read)
    };
    [Fact]
    public void AllThirtyNineSourceRecordsIncludingReviewsAndHebrewAreUnchanged()
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
