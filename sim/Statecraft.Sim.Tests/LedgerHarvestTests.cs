using Statecraft.Sim;

/// <summary>#1694, #1725 PR 7: <c>Pools.Harvest</c> on a game with an initialized Historical ledger reads the month
/// snapshot (#2970's <c>Gdd03SyntheticMonthContext.CreateLive</c>). Owner, 8 November 2026, on #1725: "One snapshot per
/// turn (Recommended)", the harvest reads the one month snapshot taken at the turn's start; "Held unless enemy on it
/// (Recommended)", a field is held unless an enemy military unit stands on its hex when the harvest runs; "Nothing until
/// #1713 (Recommended)", a plowed sites.yaml field yields nothing on a ledger game; "Yes, recheck (Recommended)", a field
/// yields only if its snapshot farmer still exists and still holds Work on it; "Per field, words later (Recommended)", one
/// field food Why line per field; "Not in this change (Recommended)", no work XP.</summary>
public class LedgerHarvestTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static string Ledger(GameState s) => string.Join("\n", s.PopulationLedger.Canonical());

    // The fields with an enemy military unit (a side other than none and the player's) on their hex, read from the units.
    static HashSet<string> EnemyHeld(Content c, GameState s) => new HashSet<string>(Gdd03FieldFixture.Fields(c)
        .Where(f => s.Units.Values.Any(u => u.Side != "" && u.Side != Movement.PlayerSide && u.Q == f.Q && u.R == f.R))
        .Select(f => f.Id), StringComparer.Ordinal);

    // The farmers on the ledger's records: a Farmer at Work on an authored field (field/q,r), by field id; by default only
    // on the fields no enemy unit stands on, the ones that yield.
    static SortedDictionary<string, PopulationUnit> Farmers(Content c, GameState s, bool heldOnly = true)
    {
        var enemy = heldOnly ? EnemyHeld(c, s) : new HashSet<string>();
        var fields = new HashSet<string>(Gdd03FieldFixture.Fields(c).Select(f => f.Id).Where(f => !enemy.Contains(f)), StringComparer.Ordinal);
        var byField = new SortedDictionary<string, PopulationUnit>(StringComparer.Ordinal);
        foreach (var u in s.PopulationLedger.Units)
            if (u.Profession == CivilianProfession.Farmer && u.Assignment.Kind == PopulationAssignmentKind.Work
                && u.Assignment.Target != null && fields.Contains(u.Assignment.Target))
                byField.Add(u.Assignment.Target, u);
        return byField;
    }

    // farmer.output at the record's tier (content/gdd03-balance.yaml) times field_food_per_turn (rules.yaml), read here.
    static int Expected(Content c, MvpRules r, PopulationUnit farmer) =>
        Gdd03FieldRates.FromCatalogue(c.Gdd03Balance!).FoodFor(farmer.Tier) * r.FieldFoodPerTurn!.Value;

    static Gdd03Field Field(Content c, string id) => Gdd03FieldFixture.Fields(c).Single(f => f.Id == id);

    static void EnemyOn(GameState s, Gdd03Field f)
    {
        var id = s.Units.Count == 0 ? 0 : s.Units.Keys.Max() + 1;
        s.Units.Add(id, new Unit(id, "infantry", f.Q, f.R, side: "arab", strength: 1));
    }

    // R-FIELD-002, R-VET-002 (#2112): every held field with a snapshot farmer adds farmer.output at that tier times
    // field_food_per_turn, one field food line per field in ordinal field-id order, adding up to the stock's change. The
    // ledger is unchanged and reconciles.
    [Fact]
    public void TheDarkStartsFarmersYieldTheirTierOnHeldFields()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var farmers = Farmers(c, s);
        Assert.NotEmpty(farmers);
        Assert.Contains(farmers.Values, f => f.Tier == CivilianTier.Novice);
        Assert.Contains(farmers.Values, f => f.Tier == CivilianTier.Trained);
        Assert.Contains(farmers.Values, f => f.Tier == CivilianTier.Veteran);
        // Every authored field has its farmer; a field an enemy unit already stands on at the start keeps it, unpaid.
        Assert.Equal(Gdd03FieldFixture.Fields(c).Count, Farmers(c, s, heldOnly: false).Count);
        var enemyHeld = EnemyHeld(c, s);
        var expected = farmers.ToDictionary(x => x.Key, x => Expected(c, r, x.Value), StringComparer.Ordinal);
        var food = s.Food; var ledger = Ledger(s);
        var month = Gdd03SyntheticMonthContext.CreateLive(c, s);
        var why = new List<Cause>();

        Pools.Harvest(c, s, r, month, why);

        Assert.Equal(food + expected.Values.Sum(), s.Food);
        Assert.Equal(expected.Keys.OrderBy(x => x, StringComparer.Ordinal), why.Select(x => x.Args.Single()));
        Assert.All(why, x => Assert.Equal(("economy", "food", CauseKeys.FieldFood), (x.System, x.Target, x.Key)));
        Assert.Equal(expected.Keys.Select(k => (double)expected[k]), why.Select(x => x.Delta));
        Assert.DoesNotContain(why, x => enemyHeld.Contains(x.Args.Single()));
        Assert.Equal((double)(s.Food - food), why.Sum(x => x.Delta));
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, Ledger(s));
    }

    // The same on HistoricalPopulation.LedgerFixture, with its date set.
    [Fact]
    public void TheLedgerFixtureHarvests()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        s.Date = c.Rules.Start;
        var r = c.RulesAt(s.Date).Mvp!;
        var farmers = Farmers(c, s);
        Assert.NotEmpty(farmers);
        var food = s.Food;
        var why = new List<Cause>();
        Pools.Harvest(c, s, r, Gdd03SyntheticMonthContext.CreateLive(c, s), why);
        Assert.Equal(food + farmers.Values.Sum(f => Expected(c, r, f)), s.Food);
        Assert.Equal(farmers.Count, why.Count);
    }

    // "an unworked field produces nothing" (owner, 3 November 2026, #1694): a farmer taken off the field before the
    // snapshot leaves it unworked; it adds 0 and writes no line, and the other fields still yield.
    [Fact]
    public void AnUnworkedFieldYieldsNothing()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var farmers = Farmers(c, s);
        var (field, farmer) = farmers.First();
        s.PopulationLedger.ChangeAssignment(farmer.Id, PopulationAssignment.Unassigned);
        var food = s.Food;
        var why = new List<Cause>();
        Pools.Harvest(c, s, r, Gdd03SyntheticMonthContext.CreateLive(c, s), why);
        Assert.Equal(food + farmers.Where(x => x.Key != field).Sum(x => Expected(c, r, x.Value)), s.Food);
        Assert.DoesNotContain(why, x => x.Args.Single() == field);
        Assert.Equal(farmers.Count - 1, why.Count);
    }

    // "Held unless enemy on it (Recommended)", D6 ("its people stay"): an enemy military unit on a field's hex when the
    // harvest runs stops that field's Food, and its farmer's record is unchanged. With the unit gone, the next snapshot
    // yields again with no new assignment.
    [Fact]
    public void AnEnemyHeldFieldYieldsNothingAndKeepsItsFarmer()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var farmers = Farmers(c, s);
        var (field, farmer) = farmers.Last();
        var month = Gdd03SyntheticMonthContext.CreateLive(c, s);
        EnemyOn(s, Field(c, field));   // after the snapshot: the test is at harvest time
        var food = s.Food;
        var why = new List<Cause>();

        Pools.Harvest(c, s, r, month, why);

        Assert.Equal(food + farmers.Where(x => x.Key != field).Sum(x => Expected(c, r, x.Value)), s.Food);
        Assert.DoesNotContain(why, x => x.Args.Single() == field);
        Assert.Equal(farmer, s.PopulationLedger.Get(farmer.Id));

        // A halutz (no side) or the player's own unit holds nothing.
        var enemy = s.Units.Keys.Max();
        s.Units.Remove(enemy);
        s.Units.Add(enemy, new Unit(enemy, "halutz", Field(c, field).Q, Field(c, field).R));
        var again = new List<Cause>(); var before = s.Food;
        Pools.Harvest(c, s, r, Gdd03SyntheticMonthContext.CreateLive(c, s), again);
        Assert.Equal(before + farmers.Values.Sum(x => Expected(c, r, x)), s.Food);
        Assert.Contains(again, x => x.Args.Single() == field && x.Delta == Expected(c, r, farmer));
    }

    // The finding on #2972: at the dark start the Iraqi army (unit 11 today) already stands on field/11,12, a trained
    // farmer's field. Under "Held unless enemy on it (Recommended)" that field yields nothing on turn 1 and its farmer
    // keeps the record, so the turn's harvest is the other staffed fields only: 58 Food today, not the plan's 61.
    [Fact]
    public void TheDarkStartsEnemyHeldFieldYieldsNothingOnTurnOne()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        const string fieldId = "field/11,12";
        var field = Field(c, fieldId);
        // The unit is read from the state, not pinned by id.
        var enemy = s.Units.Values.Where(u => u.Side != "" && u.Side != Movement.PlayerSide && u.Q == field.Q && u.R == field.R).ToList();
        Assert.NotEmpty(enemy);
        Assert.Contains(fieldId, EnemyHeld(c, s));
        var all = Farmers(c, s, heldOnly: false);
        Assert.True(all.TryGetValue(fieldId, out var farmer));
        Assert.Equal(PopulationAssignmentKind.Work, farmer!.Assignment.Kind);
        Assert.Equal(fieldId, farmer.Assignment.Target);
        var others = Farmers(c, s);
        Assert.DoesNotContain(fieldId, others.Keys);
        Assert.Equal(all.Count - 1, others.Count);
        var expected = others.Values.Sum(f => Expected(c, r, f));   // 58 today
        var food = s.Food;
        var why = new List<Cause>();

        Pools.Harvest(c, s, r, Gdd03SyntheticMonthContext.CreateLive(c, s), why);

        Assert.DoesNotContain(why, x => x.Key == CauseKeys.FieldFood && x.Args.Single() == fieldId);
        Assert.Equal(others.Count, why.Count);
        Assert.Equal(food + expected, s.Food);
        Assert.Equal((double)expected, why.Sum(x => x.Delta));
        Assert.Equal(farmer, s.PopulationLedger.Get(farmer.Id));
    }

    // "Yes, recheck (Recommended)": the snapshot's farmer must still exist and still hold Work on that field when the
    // harvest runs. A farmer taken off after the snapshot, or lost, yields nothing for that field.
    [Theory]
    [InlineData("released")] [InlineData("lost")]
    public void AFarmerWhoLeftAfterTheSnapshotYieldsNothing(string how)
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var farmers = Farmers(c, s);
        var (field, farmer) = farmers.First();
        var month = Gdd03SyntheticMonthContext.CreateLive(c, s);
        Assert.Contains(farmer.Id, month.WorkEligibleIds);
        if (how == "released") s.PopulationLedger.ChangeAssignment(farmer.Id, PopulationAssignment.Unassigned);
        else s.PopulationLedger.RegisterCombatLoss(farmer.Id, "test/loss");
        var food = s.Food;
        var why = new List<Cause>();

        Pools.Harvest(c, s, r, month, why);

        Assert.Equal(food + farmers.Where(x => x.Key != field).Sum(x => Expected(c, r, x.Value)), s.Food);
        Assert.DoesNotContain(why, x => x.Args.Single() == field);
    }

    // "Nothing until #1713 (Recommended)": a plowed sites.yaml field site adds nothing on a ledger game and writes no
    // plowed-land line; the aggregate game of the same content still harvests it (R-FIELD-002, unchanged).
    [Fact]
    public void APlowedSiteFieldYieldsNothingOnALedgerGame()
    {
        var c = Gdd03();
        var site = c.Sites.Where(x => x.Kinds.Contains(Projects.FieldKind)).OrderBy(x => x.Id, StringComparer.Ordinal).First();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        Assert.True(r.FieldFoodPerTurn > 0);
        s.Sites[site.Id] = SiteWork.Built(site.Id, s.Date);
        var food = s.Food;
        var why = new List<Cause>();
        Pools.Harvest(c, s, r, Gdd03SyntheticMonthContext.CreateLive(c, s), why);
        Assert.Equal(food + Farmers(c, s).Values.Sum(x => Expected(c, r, x)), s.Food);
        Assert.DoesNotContain(why, x => x.Key == CauseKeys.FoodHarvest);

        var pub = Simulation.NewGame(c, 1, Mode.Historical);
        pub.Sites[site.Id] = SiteWork.Built(site.Id, pub.Date);
        var pubFood = pub.Food; var pubWhy = new List<Cause>();
        Pools.Harvest(c, pub, r, pubWhy);
        Assert.Equal(pubFood + r.FieldFoodPerTurn!.Value, pub.Food);
        Assert.Equal(CauseKeys.FoodHarvest, Assert.Single(pubWhy).Key);
    }

    // "Not in this change (Recommended)": the harvest gives no work XP; no record changes at all.
    [Fact]
    public void TheHarvestGivesNoWorkXp()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var xp = s.PopulationLedger.Units.Select(u => (u.Id, u.WorkXp, u.Tier)).ToList();
        Pools.Harvest(c, s, r, Gdd03SyntheticMonthContext.CreateLive(c, s), new List<Cause>());
        Assert.Equal(xp, s.PopulationLedger.Units.Select(u => (u.Id, u.WorkXp, u.Tier)));
    }

    // R-POP-002: an Open or synthetic ledger is still refused, with or without a month, and the state is unchanged. The
    // no-month path stays refused on the dark start too (PopulationLedgerBoundaryTests keeps its pools.harvest row).
    [Theory]
    [InlineData("open", false)] [InlineData("open", true)] [InlineData("synthetic", false)] [InlineData("synthetic", true)]
    [InlineData("dark", false)]
    public void AnOpenOrSyntheticLedgerOrNoMonthIsStillRefused(string kind, bool withMonth)
    {
        var real = Gdd03();
        var c = kind == "dark" ? real : PopulationLedgerTests.Content();
        var s = kind == "dark" ? Dark(c)
            : PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
                new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: kind == "open" ? Mode.Open : null);
        var month = withMonth ? Gdd03SyntheticMonthContext.CreateLive(real, Dark(real)) : null;
        s.Food = 7;
        var hash = s.Hash(); var before = s.Describe();
        var why = new List<Cause>();
        var r = real.RulesAt(real.Rules.Start).Mvp!;
        if (month == null) Assert.Throws<NotSupportedException>(() => Pools.Harvest(c, s, r, why));
        Assert.Throws<NotSupportedException>(() => Pools.Harvest(c, s, r, month, why));
        Assert.Equal(hash, s.Hash()); Assert.Equal(before, s.Describe()); Assert.Empty(why);
    }

    // Determinism: the harvest on two clones of one state gives equal hashes and equal cause sequences.
    [Fact]
    public void TwoClonesHarvestEqually()
    {
        var c = Gdd03();
        var a = Dark(c);
        var b = a.Clone();
        var r = c.RulesAt(a.Date).Mvp!;
        var aWhy = new List<Cause>(); var bWhy = new List<Cause>();
        Pools.Harvest(c, a, r, Gdd03SyntheticMonthContext.CreateLive(c, a), aWhy);
        Pools.Harvest(c, b, r, Gdd03SyntheticMonthContext.CreateLive(c, b), bWhy);
        Assert.Equal(a.Hash(), b.Hash());
        Assert.NotEmpty(aWhy);
        Assert.Equal(aWhy.Select(x => (x.Key, x.Delta, x.Message, string.Join(",", x.Args))),
            bWhy.Select(x => (x.Key, x.Delta, x.Message, string.Join(",", x.Args))));
    }
}
