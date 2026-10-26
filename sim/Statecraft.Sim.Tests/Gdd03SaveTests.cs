using Statecraft.Sim;

/// <summary>#1687, R-SAVE-001/002: a separate envelope, never a live-loop cutover.</summary>
public class Gdd03SaveTests
{
    static string Text(int version, string ruleset) =>
        $"statecraft-save {version}\ncontent {new string('a', 64)}\nseed 1\nmode Historical\nruleset {ruleset}\nend {new string('b', 64)}\n";

    [Fact]
    public void Gdd03WritesSchemaFourAndRoundTrips()
    {
        var save = new SaveGame(new string('a', 64), 1, Mode.Historical, "gdd-0.3", new string('b', 64));
        Assert.Equal(4, save.Version);
        Assert.StartsWith("statecraft-save 4\n", save.Write());
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
    [InlineData(5, "gdd-0.3")]
    [InlineData(4, "legacy")]
    [InlineData(4, "gdd-0.1")]
    public void UnknownRulesOrNewerSchemasAreRefused(int schema, string ruleset) =>
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, ruleset))).Code);

    [Theory]
    [InlineData("legacy")]
    [InlineData("gdd-0.1")]
    public void ExistingRulesStillWriteSchemaThree(string ruleset)
    {
        var save = new SaveGame(new string('a', 64), 1, Mode.Historical, ruleset, new string('b', 64));
        Assert.Equal(3, save.Version);
        Assert.StartsWith("statecraft-save 3\n", save.Write());
    }

    // R-SAVE-001/002: future gdd-0.3 callers must not replay the live rules' saves.
    [Theory]
    [InlineData(2, "legacy")]
    [InlineData(3, "legacy")]
    [InlineData(2, "gdd-0.1")]
    [InlineData(3, "gdd-0.1")]
    public void EarlierRulesAreRefusedByGdd03(int schema, string ruleset)
    {
        var error = Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, ruleset), SaveGame.Gdd03Ruleset));
        Assert.Equal(SaveException.OlderRules, error.Code);
        Assert.DoesNotContain("new game", error.Message);
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("gdd-0.1")]
    public void ExistingGamesRefuseGdd03AsNewer(string ruleset) =>
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(Text(4, "gdd-0.3"), ruleset)).Code);

    [Fact]
    public void Gdd03AcceptsItsOwnEnvelope() =>
        Assert.Equal("gdd-0.3", SaveGame.Parse(Text(4, "gdd-0.3"), SaveGame.Gdd03Ruleset).Ruleset);

    [Theory]
    [InlineData(4, "gdd-future")]
    [InlineData(5, "gdd-0.3")]
    public void Gdd03RefusesUnknownRulesAndFutureSchemas(int schema, string ruleset) =>
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(Text(schema, ruleset), SaveGame.Gdd03Ruleset)).Code);
}
