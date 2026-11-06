using Statecraft.Sim;

/// <summary>#1687, R-SAVE-001/002: a separate envelope, never a live-loop cutover. #2605: every ruleset now writes schema 5;
/// gdd-0.3 wrote 4 before, legacy 1 to 3 and gdd-0.1 2 to 3.</summary>
public class Gdd03SaveTests
{
    static string Text(int version, string ruleset) =>
        $"statecraft-save {version}\ncontent {new string('a', 64)}\nseed 1\nmode Historical\nruleset {ruleset}\nend {new string('b', 64)}\n";

    [Fact]
    public void Gdd03WritesSchemaFiveAndRoundTrips()
    {
        var save = new SaveGame(new string('a', 64), 1, Mode.Historical, "gdd-0.3", new string('b', 64));
        Assert.Equal(5, save.Version);
        Assert.StartsWith("statecraft-save 5\n", save.Write());
        Assert.Equal("gdd-0.3", SaveGame.Parse(save.Write()).Ruleset);
        Assert.Equal(save.Write(), SaveGame.Parse(save.Write()).Write());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Gdd03UnderAnEarlierSchemaIsDamage(int schema)
    {
        // No build writes it, so neither the live game nor gdd-0.3 may call it older or newer rules.
        Assert.Null(Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, "gdd-0.3"))).Code);
        Assert.Null(Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, "gdd-0.3"), SaveGame.Gdd03Ruleset)).Code);
        Assert.Null(Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, "gdd-0.3"), SaveGame.GddRuleset)).Code);
    }

    [Theory]
    [InlineData(4, "gdd-future")]
    [InlineData(6, "gdd-0.3")]
    [InlineData(6, "legacy")]
    [InlineData(6, "gdd-0.1")]
    public void UnknownRulesOrNewerSchemasAreRefused(int schema, string ruleset) =>
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, ruleset))).Code);

    // #2605: schema 4 was gdd-0.3's alone; no build wrote it for the older rules, so such a save was edited.
    [Theory]
    [InlineData("legacy")]
    [InlineData("gdd-0.1")]
    public void OlderRulesUnderSchemaFourAreDamage(string ruleset) =>
        Assert.Null(Assert.Throws<SaveException>(() => SaveGame.Parse(Text(4, ruleset))).Code);

    [Theory]
    [InlineData("legacy")]
    [InlineData("gdd-0.1")]
    [InlineData("gdd-0.3")]
    public void EveryRulesetWritesSchemaFive(string ruleset)
    {
        var save = new SaveGame(new string('a', 64), 1, Mode.Historical, ruleset, new string('b', 64));
        Assert.Equal(5, save.Version);
        Assert.StartsWith("statecraft-save 5\n", save.Write());
    }

    // R-SAVE-005 (#2605): the earlier schema of each ruleset still reads, keeps its version, and is written back as schema 5.
    [Theory]
    [InlineData(3, "legacy")]
    [InlineData(3, "gdd-0.1")]
    [InlineData(4, "gdd-0.3")]
    public void AnEarlierSchemaReadsAndIsWrittenBackAsFive(int schema, string ruleset)
    {
        var save = SaveGame.Parse(Text(schema, ruleset));
        Assert.Equal(schema, save.Version);
        Assert.Equal(ruleset, save.Ruleset);
        Assert.StartsWith("statecraft-save 5\n", save.Write());
    }

    // R-SAVE-001/002: future gdd-0.3 callers must not replay the live rules' saves.
    [Theory]
    [InlineData(2, "legacy")]
    [InlineData(3, "legacy")]
    [InlineData(2, "gdd-0.1")]
    [InlineData(3, "gdd-0.1")]
    [InlineData(5, "legacy")]
    [InlineData(5, "gdd-0.1")]
    public void EarlierRulesAreRefusedByGdd03(int schema, string ruleset)
    {
        var error = Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, ruleset), SaveGame.Gdd03Ruleset));
        Assert.Equal(SaveException.OlderRules, error.Code);
        Assert.DoesNotContain("new game", error.Message);
    }

    [Theory]
    [InlineData(4, "legacy")]
    [InlineData(4, "gdd-0.1")]
    [InlineData(5, "legacy")]
    [InlineData(5, "gdd-0.1")]
    public void ExistingGamesRefuseGdd03AsNewer(int schema, string ruleset) =>
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, "gdd-0.3"), ruleset)).Code);

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void Gdd03AcceptsItsOwnEnvelope(int schema) =>
        Assert.Equal("gdd-0.3", SaveGame.Parse(Text(schema, "gdd-0.3"), SaveGame.Gdd03Ruleset).Ruleset);

    [Theory]
    [InlineData(4, "gdd-future")]
    [InlineData(6, "gdd-0.3")]
    public void Gdd03RefusesUnknownRulesAndFutureSchemas(int schema, string ruleset) =>
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, ruleset), SaveGame.Gdd03Ruleset)).Code);

    // R-SAVE-005 (#2605): a schema 4 gdd-0.3 save through GameSession.Load, not only SaveGame.Parse. With its saved hash it
    // loads with no warning and is written back as schema 5. With a wrong end hash it was played under the earlier cap, so
    // it loads with the replay_differs warning alone, not an exception.
    [Fact]
    public void ASchemaFourGdd03SaveLoadsThroughTheSession()
    {
        var files = Fixture.Gdd(); NoAttentionTests.Select(files, true);
        var c = ContentLoader.Load(Fixture.Write(files));
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        var played = GameSession.New(c, 1, Mode.Historical);
        played.Step(new Command[] { new BuyArms(1) });
        played.Step(new Command[0]);
        var five = played.Save.Write();
        var four = five.Replace("statecraft-save 5\n", "statecraft-save 4\n");
        Assert.Equal(4, SaveGame.Parse(four).Version);
        var loaded = GameSession.Load(c, four, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(played.State.Hash(), loaded.State.Hash());
        Assert.Equal(five, loaded.Save.Write());

        var wrongEnd = four.Replace("end " + played.State.Hash(), "end " + new string('0', 64));
        Assert.NotEqual(four, wrongEnd);
        var differs = GameSession.Load(c, wrongEnd, out warnings);
        Assert.Equal(new[] { GameSession.ReplayDiffers }, warnings);
        Assert.Equal(played.State.Hash(), differs.State.Hash());
    }
}
