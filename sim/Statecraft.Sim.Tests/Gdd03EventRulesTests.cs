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

    // Inventory from the approved C244 plan, pinned to ef35703; includes valid sibling fields.
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
        "absorb_immigrants:56880a2e2a96ed94f9131f71e3be55d8f2b27a8cab7aceda83e7cb6b3dd886de",
        "altalena:7c70e207bcc8b354e5c137e7c7b15d3db1de10f85303e197495615360de397ca",
        "armistice_egypt:b85f731749e0636502a8d4a10118fbdd9c21f56a6009d6ccc0857fd5591943bc",
        "armistice_jordan:d6284502a8b49e7463deabd81ee760181451b732bf12db46ead2e838219e4cd4",
        "armistice_lebanon:e87a66c9df444b0c039b23d2533956d500ca1c277b3ce4236b534b121c24e87e",
        "armistice_syria:c8424d98187b549a6e7d5d392d9e064bcf6ff0c62d4da3118c940e12ea6300b0",
        "austerity_1949:f5b0d85ce7fc8772c55f6d235dc8441372083a35e6f51282e63f49b2e17397af",
        "bernadotte_assassination:9263942438c459aad95b51c2569d1aa89cb2255f7278655e244121044e5755c4",
        "burma_road_complete:f48054bb7720b93b582cae4e168f13595a4360ee62e91e12815df08de384e6e3",
        "clothing_rationing_1950:2e33baac62ea7d12e36b9a5fd5237a128139671b8729c4841c967afdfa6dba60",
        "degania_defence:47233f988033e4a7499e1e3df52a542fe252d2a32a5f1ac8d358887f676d4e23",
        "education_registration_1951:cce6e7f6ca42058ed265da888258cce7fc40407710bba98e028ad7801a351e11",
        "elections_1949:959ae7714c1a53a3df6766e6d191bc3490f3fa5b5d2972dd1b965a855106c110",
        "first_new_yishuv:9edf9e0da02c2feefe11eb52aa056ee64af60a41ac3fa2c283ff976f33d0e0c8",
        "first_truce:251eaf274e28427c667a1ea46a4d0f3e9f5eefbc4bff8fe1141fc6d8bdf6b378",
        "forex_crisis_1951:212a675012c6cfb915b960de96d7a2e2eac9622c9652afd7a6ffd4ee2e1ed304",
        "frumkin_commission:e88bc10c1ae0739c3191fa03a3958f98c7136bd3a1de9e93aaf129ddb8fff55c",
        "hula_drainage:0b400fda5ce85e202cf2bf7c2557a2ff612f406f33f2f59b24e943636aaf372e",
        "idf_founding:6777150c6f2cdb064cacd181761f8bb80fa25205c9fc25e523a842a825adae58",
        "iraqi_exodus:62c43ba683f4981951829a72c0cae97b1ac8f11663472cccf73b46cefc1c60f5",
        "israel_bonds:b8f95f2ab683a4b88ca36b8a375d168a4bf5921ccb864e5504c45d66821bea08",
        "jerusalem_ceasefire:79d875217efb2d614538db27b405d9469d46fb61b5288651766611e5559a4643",
        "jerusalem_demilitarisation_guard:97cc99fc9054ded329be4550248b7cc8d46826581e14f8d1b4554fbd3af4cece",
        "jerusalem_military_government:322585aeedf0c69b9f81722edebaebbdc0d27ebb4e7b67d275e898607d2cd211",
        "latrun_proposal:f7612a99e1e6f378e6b9a91f36bb4c31e16de465e8bccb2785fea70bfe4e6932",
        "law_of_return:d9453c9d4645bfe7b8bd715bbc4aae6d2f5156b72c717317bd6990184b8e61e4",
        "maabarot_policy:de29ffa17fbc70aa7283b833e546a219b449c8b3e0d712e3d158ed18169d592b",
        "ministry_supply_crisis:000d54fdc5198b105f101bfa7816a152e2c7682f01a97da67e97988e5ef141db",
        "operation_hiram:77657ee082201710acf8d1ab25979ddb5571261452e3a8c9ccc5d33e783177b7",
        "operation_horev:72fb22533fc52e5507502fdce6e84c59cbb0649118b00a1701e5e9b522437678",
        "operation_yoav:418ea425c461986c0ad2bcf9d493941adc9e22082877506c867b6aab4cf75250",
        "rafah_force_pressure:ab01fc670aadbb410928c22eccc23f66e785be775dea26a3644c1317b1edb82b",
        "railway_complete:fb2c0b0edb93217a703a2e9437177943275546c177f5b0751936e3705bca1513",
        "reparations_claim:b9982aae12edd0f6e2d9f8e4e3273d865d2e9106480fae5824728524edfa4139",
        "reparations_vote:a79521d61ec59aa72a581b6598ecfade18e8d1e52dc25cef083078b0665a13b3",
        "rhodes_talks:9ff18db85e108157ab4ff3753fbf42f9933bedb5f021310979d05fae1b731d1c",
        "second_truce:56c2259ad9af32b4d6d43d22044908611e6b43f2836211350beb125e864b1611",
        "sinai_withdrawal:29854edb31218257e99417f86f23a6dde91c07a9e66230fa1a1b4a0248b15ac5",
        "ten_days:805500e569a78c2debd27df872614a153730bdba2360ff94b2edb74ea76c56cb",
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
