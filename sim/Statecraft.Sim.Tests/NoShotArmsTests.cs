using Statecraft.Sim;
using System.Text.RegularExpressions;

/// <summary>R-COMBAT-001, R-COMBAT-004, R-COMBAT-007 (#1706): under gdd-0.3 the player's attacks and artillery fire neither
/// check nor spend Arms (owner ruling, 25 October 2026, on #1668). Only the shot price changes: the fixture's combat table,
/// the instant timing, the Arms stock and every other Arms cost stay, and legacy and gdd-0.1 keep their prices.</summary>
public class NoShotArmsTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: car, kind: unit, side: israel, class: armour, movement_points: 5, combat_percent: 150 }\n"
        + "  - { id: gun, kind: unit, side: israel, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3, combat_percent: 200 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy_gun, kind: unit, side: egypt, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3 }\n";

    static Dictionary<string, string> Files(bool modern)
    {
        var f = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
        });
        if (modern) f["eras.yaml"] = f["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
        f[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        return f;
    }

    /// <summary>The fixture under gdd-0.3 (<paramref name="modern"/>) or gdd-0.1, after <paramref name="edit"/>; files under
    /// rules/ are written in their own folder.</summary>
    static Content Load(bool modern = true, Action<Dictionary<string, string>>? edit = null)
    {
        var f = Files(modern);
        edit?.Invoke(f);
        var nested = f.Where(p => p.Key.StartsWith("rules/", StringComparison.Ordinal)).ToArray();
        foreach (var pair in nested) f.Remove(pair.Key);
        var dir = Fixture.Write(f);
        foreach (var pair in nested)
        {
            var path = Path.Combine(dir, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, pair.Value);
        }
        return ContentLoader.Load(dir);
    }

    static Unit Of(string type, int id, int q, int r, int strength = 6, string side = "israel") =>
        new Unit(id, type, q, r, side: side, strength: strength);

    /// <summary>Unit 1 of <paramref name="type"/> on (2, 1) and an enemy of strength 2 in its reach, with the stock at
    /// <paramref name="arms"/>, and the attack order.</summary>
    static (GameState s, UnitOrder cmd) Battle(Content c, string type, int arms)
    {
        int q = type == "gun" ? 4 : 3;
        var s = CombatTests.With(c, Of(type, 1, 2, 1), Of("enemy", 2, q, 1, 2, "egypt"));
        s.Arms = arms;
        s.Attention = 500;
        return (s, new UnitOrder(1, Combat.Attack, q, 1));
    }

    /// <summary>Makes every shot price missing or invalid after loading, so any read of one would throw or refuse.</summary>
    static void Poison(Content c, bool missing)
    {
        foreach (var r in c.EraRules.Values.Append(c.Rules)) r.Mvp!.ArmsPerAttack = missing ? null : int.MinValue;
        foreach (var t in c.UnitTypes.Where(t => t.Class == Combat.ArtilleryClass)) t.ArmsPerFire = missing ? null : int.MinValue;
    }

    public static IEnumerable<object[]> Shots() =>
        from type in new[] { "soldier", "car", "gun" }
        from arms in new[] { 0, 17 }
        from missing in new[] { false, true }
        select new object[] { type, arms, missing };

    [Theory]
    [MemberData(nameof(Shots))]
    public void CheckPreviewAndInstantExecutionDoNotReadOrPayShotPrices(string type, int arms, bool missing)
    {
        var c = Load();
        Poison(c, missing);
        var (s, cmd) = Battle(c, type, arms);
        var hash = s.Hash();
        Assert.Null(Combat.Check(c, s, 1, cmd.Q, cmd.R));
        Assert.NotNull(Combat.Preview(c, s, 1, cmd.Q!.Value, cmd.R!.Value));

        var now = Simulation.Preview(c, s, new[] { cmd });
        Assert.Empty(now.Rejected);
        Assert.Equal(arms, now.State.Arms);
        Assert.Equal(hash, s.Hash());
        Assert.NotEqual(s.RngState, now.State.RngState);
        Assert.Contains(now.Why, w => w.Key == WhyKeys.Engagement);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Equal(0, Movement.PointsLeft(c, now.State, 1, now.Spent));

        var ended = Simulation.Step(c, s, new[] { cmd });
        Assert.Empty(ended.Rejected);
        Assert.Equal(arms, ended.State.Arms);
        Assert.Single(ended.Why, w => w.Key == WhyKeys.Engagement); // the player's strike is never fought twice
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NewModeLoadsWithoutObsoleteBaseAndEraPrices(bool era)
    {
        var c = Load(edit: f =>
        {
            f["rules.yaml"] = f["rules.yaml"].Replace("  arms_per_attack: 2\n", "");
            f["unit_types.yaml"] = f["unit_types.yaml"].Replace("arms_per_fire: 3, ", "");
            if (era) f["rules/founding.yaml"] = "mvp: { arms_per_attack: null }\n";
        });
        foreach (var type in new[] { "soldier", "car", "gun" })
        {
            var (s, cmd) = Battle(c, type, 0);
            Assert.Empty(Simulation.Preview(c, s, new[] { cmd }).Rejected);
        }
    }

    [Theory]
    [InlineData("attack", false)]
    [InlineData("attack", true)]
    [InlineData("fire", false)]
    [InlineData("fire", true)]
    public void OldModeRetainsMissingAndNegativePriceValidation(string field, bool negative)
    {
        var ex = Assert.Throws<ContentException>(() => Load(false, f =>
        {
            if (field == "attack")
                f["rules.yaml"] = f["rules.yaml"].Replace("  arms_per_attack: 2\n", negative ? "  arms_per_attack: -1\n" : "");
            else
                f["unit_types.yaml"] = f["unit_types.yaml"].Replace("arms_per_fire: 3, ", negative ? "arms_per_fire: -1, " : "");
        }));
        Assert.Contains(field == "attack" ? "arms_per_attack" : "arms_per_fire", ex.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EraShotPricesRemainStrictOnlyInTheOlderIdentity(bool modern)
    {
        Action<Dictionary<string, string>> edit = f => f["rules/founding.yaml"] = "mvp: { arms_per_attack: -9 }\n";
        if (!modern)
        {
            Assert.Contains("arms_per_attack", Assert.Throws<ContentException>(() => Load(false, edit)).Message);
            return;
        }
        var c = Load(true, edit);
        var (s, cmd) = Battle(c, "soldier", 0);
        Assert.Empty(Simulation.Preview(c, s, new[] { cmd }).Rejected);
    }

    [Fact]
    public void NewModeStillRequiresAnArtilleryRange()
    {
        var ex = Assert.Throws<ContentException>(() => Load(edit: f => f["unit_types.yaml"] = f["unit_types.yaml"].Replace("range: 2, ", "")));
        Assert.Contains("range", ex.Message);
    }

    [Fact]
    public void LegacyStillHasNoGddCombatCommands()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(SaveGame.LegacyRuleset, c.Ruleset);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var h = s.Hash();
        var r = Simulation.Preview(c, s, new[] { new UnitOrder(1, Combat.Attack, 2, 1) });
        Assert.Single(r.Rejected);
        Assert.Equal(h, r.State.Hash());
    }

    [Theory]
    [InlineData("soldier", 2)]
    [InlineData("car", 2)]
    [InlineData("gun", 3)]
    public void OldModeStillRefusesAndPaysTheExactPrice(string type, int cost)
    {
        var c = Load(false);
        var (s, cmd) = Battle(c, type, 0);
        var refused = Simulation.Preview(c, s, new[] { cmd });
        Assert.StartsWith("An attack needs", Assert.Single(refused.Rejected).Reason);
        Assert.Equal(s.Hash(), refused.State.Hash());

        s.Arms = 17;
        var paid = Simulation.Preview(c, s, new[] { cmd });
        Assert.Empty(paid.Rejected);
        Assert.Equal(17 - cost, paid.State.Arms);
    }

    [Theory]
    [InlineData("soldier")]
    [InlineData("car")]
    [InlineData("gun")]
    public void OutcomesAndDrawsStayTheSameApartFromStockAccounting(string type)
    {
        var old = Load(false);
        var modern = Load();
        var (a, ac) = Battle(old, type, 17);
        var (b, bc) = Battle(modern, type, 17);
        Assert.Equal(Combat.Preview(old, a, 1, ac.Q!.Value, ac.R!.Value), Combat.Preview(modern, b, 1, bc.Q!.Value, bc.R!.Value));

        var x = Simulation.Preview(old, a, new[] { ac });
        var y = Simulation.Preview(modern, b, new[] { bc });
        Assert.Empty(x.Rejected);
        Assert.Empty(y.Rejected);
        Assert.Equal(x.State.RngState, y.State.RngState);
        Assert.Equal(17, y.State.Arms);

        var normalized = y.State.Clone();
        normalized.Arms = x.State.Arms;
        static string Core(string text) => Regex.Replace(text.Replace("ruleset=gdd-0.3;", ""), @"\natt=[^\n]*", "");
        Assert.Equal(Core(x.State.Describe()), Core(normalized.Describe()));
        Assert.Equal(x.Why.Select(w => w.Message), y.Why.Select(w => w.Message));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnemyNoSupplyNeverReadsThePlayersPrices(bool modern)
    {
        var c = Load(modern);
        Poison(c, true);
        foreach (var type in new[] { "enemy", "enemy_gun" })
        {
            var s = CombatTests.With(c, Of(type, 1, 2, 1, 2, "egypt"), Of("soldier", 2, 3, 1));
            s.Arms = 0;
            Assert.Null(Combat.Check(c, s, 1, 3, 1, enemy: true));
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("civilian")]
    [InlineData("nohex")]
    [InlineData("range")]
    [InlineData("noenemy")]
    [InlineData("truce")]
    [InlineData("spent")]
    [InlineData("gameover")]
    public void GenuineRefusalsStillLeaveStockAndRngUntouched(string invalid)
    {
        var c = Load();
        Poison(c, true);
        var (s, cmd) = Battle(c, "soldier", 0);
        switch (invalid)
        {
            case "missing": cmd = new UnitOrder(999, Combat.Attack, 3, 1); break;
            case "foreign": cmd = new UnitOrder(2, Combat.Attack, 2, 1); break;
            case "civilian": s.Units[1] = new Unit(1, "halutz", 2, 1); break;
            case "nohex": cmd = new UnitOrder(1, Combat.Attack); break;
            case "range": cmd = new UnitOrder(1, Combat.Attack, 5, 1); break;
            case "noenemy": s.Units.Remove(2); break;
            case "truce": s.ChosenOptions["first_truce"] = "accept"; break;
            case "gameover": s.GameOver = GameOverCodes.DefeatEconomy; break;
        }
        var before = s.Hash();
        var cmds = invalid == "spent"
            ? new Command[] { new Move(1, 2, 2), new Move(1, 2, 1), new Move(1, 2, 2), cmd }
            : new Command[] { cmd };
        var r = Simulation.Preview(c, s, cmds);
        Assert.Single(r.Rejected);
        Assert.Equal(0, r.State.Arms);
        Assert.Equal(s.RngState, r.State.RngState);
        if (invalid != "spent") Assert.Equal(before, r.State.Hash());
        else Assert.Contains("movement points", r.Rejected[0].Reason);
    }

    [Fact]
    public void GunsStillCannotHoldAndArmsStockStaysHashRelevant()
    {
        var c = Load();
        var (s, _) = Battle(c, "gun", 0);
        var h = s.Hash();
        s.Arms = 1;
        Assert.NotEqual(h, s.Hash());
        s.Arms = 0;
        Assert.Equal("Artillery does not hold ground", Simulation.Preview(c, s, new[] { new UnitOrder(1, Orders.Hold) }).Rejected.Single().Reason);
        Assert.Contains("not enough Arms", Assert.Single(Simulation.Preview(c, s, new[] { new Train("town_a", "recruit", 1) }).Rejected).Reason);

        s.Arms = 17;
        var trained = Simulation.Preview(c, s, new[] { new Train("town_a", "recruit", 1) });
        Assert.Empty(trained.Rejected);
        Assert.Equal(15, trained.State.Arms);
        var cohort = trained.State.Cohorts.Values.Single();
        var cancelled = Simulation.Preview(c, s, new Command[] { new Train("town_a", "recruit", 1), new CancelTraining(cohort.Id) });
        Assert.Empty(cancelled.Rejected);
        Assert.Equal(17, cancelled.State.Arms);

        s.Arms = 0;
        var bought = Simulation.Preview(c, s, new[] { new BuyArms(1) });
        Assert.Empty(bought.Rejected);
        Assert.Equal(1, bought.State.Arms);
        Assert.True(bought.State.Treasury < s.Treasury);
    }

    [Fact]
    public void TwoUnitsStillCommitTwoIndependentEngagementsWithoutStockReservation()
    {
        var c = Load();
        Poison(c, true);
        var s = CombatTests.With(c, Of("soldier", 1, 2, 0), Of("car", 2, 2, 2), Of("enemy", 3, 3, 0, 1, "egypt"), Of("enemy", 4, 3, 2, 1, "egypt"));
        s.Arms = 0;
        var r = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 0), new UnitOrder(2, Combat.Attack, 3, 2) });
        Assert.Empty(r.Rejected);
        Assert.Equal(0, r.State.Arms);
        Assert.Equal(2, r.Why.Count(w => w.Key == WhyKeys.Engagement));
    }
}

/// <summary>R-COMBAT-001, R-COMBAT-004 on the real content under gdd-0.3: an attack with an empty stock commits, replays
/// from a schema-4 save and is not fought again at End turn.</summary>
public class NoShotArmsSessionTests
{
    [Fact]
    public void AZeroStockInstantAttackReplaysInSchemaFourAndIsNotRepeatedAtEndTurn()
    {
        var root = RealContentTests.ContentRoot();
        var c = ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            return rel == "eras.yaml" ? text.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
        foreach (var r in c.EraRules.Values.Append(c.Rules)) r.Mvp!.ArmsStart = 0;

        var game = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var attack = (from u in game.Now.State.Units.Values.Where(u => u.Side == Movement.PlayerSide)
                      from e in game.Now.State.Units.Values.Where(e => e.Side != "" && e.Side != Movement.PlayerSide)
                      where Combat.Check(c, game.Now.State, u.Id, e.Q, e.R) == null
                      select new UnitOrder(u.Id, Combat.Attack, e.Q, e.R)).First();
        Assert.Null(game.Commit(attack));
        Assert.Equal(0, game.Now.State.Arms);
        Assert.Single(game.Now.Why, w => w.Key == WhyKeys.Engagement);

        var text = game.Save.Write();
        Assert.StartsWith("statecraft-save 4", text);
        var replay = GameSession.Load(c, text, out _);
        Assert.Equal(game.Now.State.Hash(), replay.Now.State.Hash());
        Assert.Single(replay.Committed);

        var ended = game.EndTurn(Array.Empty<Command>());
        var replayEnd = replay.EndTurn(Array.Empty<Command>());
        Assert.Equal(ended.State.Hash(), replayEnd.State.Hash());
        Assert.Equal(0, ended.State.Arms);
        Assert.Equal(game.Now.State.Hash(), GameSession.Load(c, game.Save.Write(), out _).Now.State.Hash());
        Assert.Empty(game.Committed);
        Assert.Equal("", ended.State.Units[attack.Unit].Order);
    }
}
