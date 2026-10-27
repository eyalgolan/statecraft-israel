using Statecraft.Sim;

// R-POP-001, R-POP-002, R-COMBAT-001..008: groundwork does not release aggregate guards.
public class Gdd03CombatContractTests
{
    [Theory, InlineData("he-IL"), InlineData("en-US")]
    public void InitializedLedgerHasItsOwnAuthorityAndAggregateCombatRefusesAtomically(string culture)
    {
        Gdd03CombatCatalogueTests.InCulture(culture, () => {
            var c = Gdd03CombatCatalogueTests.Synthetic();
            var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] {
                new PopulationInventoryRow("synthetic-combat-contract", 0, PopulationLedgerTests.Person()) });
            s.Population["fixture"] = 2000; s.Arms = 5; s.Food = 7; s.Treasury = 23; s.Debt = 2; s.RngState = 99;
            Assert.True(s.PopulationLedger.IsInitialized);
            Assert.Equal(10000d, s.TotalPopulation); // ledger authority, not the poisoned aggregate 2000
            s.PopulationLedger.Reconcile();
            var before = s.Describe(); var hash = s.Hash(); var rng = s.RngState;
            var cause = new Cause("fixture", "keep", 1, "keep"); var why = new List<Cause> { cause };
            foreach (var operation in new Action[] {
                () => Combat.Resolve(c, s, why), () => { Combat.Outcomes(c, s, 0); } })
            {
                Assert.Throws<NotSupportedException>(operation);
                Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
                Assert.Equal(rng, s.RngState); Assert.Equal(5, s.Arms); Assert.Equal(7, s.Food);
                Assert.Equal(23, s.Treasury); Assert.Equal(2, s.Debt); Assert.Same(cause, Assert.Single(why));
            }
            // Reading design data has no state argument, mutator or RNG source.
            Assert.Equal(1, Gdd03BalanceCatalogue.ReadCombatCatalogue(c).DrawCount);
            Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
        });
    }

    [Theory, InlineData(SaveGame.GddRuleset), InlineData(SaveGame.Gdd03Ruleset)]
    public void UninitializedSyntheticStateKeepsTheExistingAggregateOutcomePath(string identity)
    {
        var files = Fixture.Gdd();
        files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + identity);
        files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = CombatTests.With(c);
        Assert.False(s.PopulationLedger.IsInitialized);
        var before = s.Describe(); var rng = s.RngState;
        var mvp = c.RulesAt(s.Date).Mvp!; var authored = mvp.CombatTable[0];
        var expected = Enumerable.Range(0, authored.AttackerLoss.Count)
            .Select(i => (AttackersLose: authored.AttackerLoss[i] * mvp.PeoplePerUnit,
                DefendersLose: authored.DefenderLoss[i] * mvp.PeoplePerUnit, Retreat: authored.DefenderRetreats[i] == 1))
            .GroupBy(face => face)
            .Select(group => (Faces: group.Count(), Of: authored.AttackerLoss.Count,
                group.Key.AttackersLose, group.Key.DefendersLose, group.Key.Retreat)).ToArray();
        Assert.Equal(expected, Combat.Outcomes(c, s, 0).ToArray());
        Assert.Equal(before, s.Describe()); Assert.Equal(rng, s.RngState);
    }

    [Fact]
    public void ShippedErasCannotEnterTheNewCatalogueBoundary()
    {
        var shipped = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.NotEmpty(shipped.Eras);
        foreach (var era in shipped.Eras)
        {
            Assert.NotEqual(SaveGame.Gdd03Ruleset, era.Systems.Ruleset);
            var c = new Content { Eras = new List<Era> { era }, Gdd03Balance = shipped.Gdd03Balance };
            Assert.Throws<NotSupportedException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(c));
        }
        foreach (var identity in new[] { SaveGame.LegacyRuleset, SaveGame.GddRuleset, "gdd-0.30" })
            Assert.Throws<NotSupportedException>(() => Gdd03BalanceCatalogue.ReadCombatCatalogue(Gdd03CombatCatalogueTests.Synthetic(identity)));
    }
}
