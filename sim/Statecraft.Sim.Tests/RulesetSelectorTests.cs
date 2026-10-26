using Statecraft.Sim;

public class RulesetSelectorTests
{
    static Dictionary<string, string> Files(string loop, string? ruleset, bool catalogue = true)
    {
        var files = loop == EraSystems.Gdd ? Fixture.Gdd() : new Dictionary<string, string>();
        files["eras.yaml"] = Fixture.Files["eras.yaml"] + $"    systems: {{ loop: {loop}" +
            (ruleset == null ? "" : $", ruleset: {ruleset}") + " }\n";
        if (catalogue) files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(
            Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        return files;
    }

    static Content Load(string loop, string? ruleset, bool catalogue = true) =>
        ContentLoader.Load(Fixture.Write(Files(loop, ruleset, catalogue)));

    // R-LOOP-001, R-SAVE-001: the identity is game-wide; an absent selector preserves inference.
    [Theory]
    [InlineData("legacy", null, "legacy")]
    [InlineData("gdd", null, "gdd-0.1")]
    [InlineData("legacy", "legacy", "legacy")]
    [InlineData("gdd", "gdd-0.1", "gdd-0.1")]
    [InlineData("gdd", "gdd-0.3", "gdd-0.3")]
    public void SelectsInferredOrExplicitIdentity(string loop, string? selector, string expected)
    {
        Assert.Equal(expected, Load(loop, selector).Ruleset);
    }

    [Theory]
    [InlineData("legacy", "gdd-0.3")]
    [InlineData("legacy", "gdd-0.1")]
    [InlineData("gdd", "legacy")]
    [InlineData("gdd", "future")]
    [InlineData("gdd", "")]
    [InlineData("gdd", "null")]
    public void RefusesUnknownOrLoopIncompatibleIdentity(string loop, string selector)
    {
        var error = Assert.Throws<ContentException>(() => Load(loop, selector));
        Assert.Equal("eras.yaml", error.File);
        Assert.Equal("eras[founding].systems.ruleset", error.Field);
    }

    [Theory]
    [InlineData("gdd-0.3", "gdd-0.1", false)]
    [InlineData("gdd-0.3", null, false)]
    [InlineData("gdd-0.3", "gdd-0.3", true)]
    [InlineData("gdd-0.1", null, true)]
    public void EveryEraMustAgreeOnEffectiveIdentity(string first, string? second, bool valid)
    {
        var files = Files(EraSystems.Gdd, first);
        files["eras.yaml"] = files["eras.yaml"].Replace("to: 1957-01-01", "to: 1950-01-01") +
            "  - id: later\n    name: Later\n    from: 1950-01-01\n    to: 1957-01-01\n" +
            "    systems: { loop: gdd" + (second == null ? "" : $", ruleset: {second}") + " }\n";
        if (valid) { Assert.Equal(first, ContentLoader.Load(Fixture.Write(files)).Ruleset); return; }
        var error = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal("eras.yaml", error.File);
        Assert.Equal("eras[later].systems.ruleset", error.Field);
        Assert.Contains("same ruleset", error.Message);
    }

    // R-LOOP-005: the first selectable 0.3 path cannot silently lose its catalogue.
    [Theory]
    [InlineData(null)]
    [InlineData("null\n")]
    public void Gdd03RequiresNonNullCatalogue(string? text)
    {
        var files = Files(EraSystems.Gdd, SaveGame.Gdd03Ruleset, false);
        if (text != null) files[Gdd03BalanceCatalogue.FileName] = text;
        var error = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(Gdd03BalanceCatalogue.FileName, error.File);
        Assert.Equal("(file)", error.Field);
        Assert.Contains(text == null ? "required" : "empty file", error.Message);
    }

    [Theory]
    [InlineData("legacy", null)]
    [InlineData("gdd", null)]
    [InlineData("legacy", "legacy")]
    [InlineData("gdd", "gdd-0.1")]
    public void OlderRulesetsNeedNoCatalogue(string loop, string? selector)
    {
        Assert.Null(Load(loop, selector, false).Gdd03Balance);
    }

    [Theory]
    [InlineData("legacy", null, true)]
    [InlineData("gdd", "gdd-0.1", true)]
    [InlineData("gdd", "gdd-0.3", false)]
    public void AttentionPredicateOnlyDescribesIdentity(string loop, string? selector, bool expected)
    {
        var c = Load(loop, selector);
        Assert.Equal(expected, c.UsesAttention);
    }

    [Theory]
    [InlineData("legacy", null, "legacy", 3)]
    [InlineData("gdd", null, "gdd-0.1", 3)]
    [InlineData("gdd", "gdd-0.3", "gdd-0.3", 4)]
    public void SessionAndSaveCarrySelectedIdentity(string loop, string? selector, string expected, int schema)
    {
        var c = Load(loop, selector);
        var session = GameSession.New(c, 1, Mode.Historical);
        Assert.Equal(expected, session.Save.Ruleset);
        Assert.Equal(schema, session.Save.Version);
        var written = session.Save.Write();
        Assert.Equal(written, SaveGame.Parse(written, c.Ruleset).Write());
    }

    [Fact]
    public void ShippingContentStillInfersGdd01()
    {
        Assert.Equal(SaveGame.GddRuleset, ContentLoader.Load(RealContentTests.ContentRoot()).Ruleset);
    }
}
