using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>The writer preview (#295): a rule change shows its effect without touching content/.</summary>
public class PreviewTests
{
    static string Root => RealContentTests.ContentRoot();
    static readonly string[] None = new string[0];
    static readonly string[] Show = { "morale", "treasury" };

    [Fact]
    public void ANoOpPreviewMatchesTheGoldenAndHasAnEmptyDiff()
    {
        var text = Preview.Run(Root, GoldenRun.Until1950, GoldenRun.Seed, None, Show, 8);
        Assert.Contains("(the descriptions are identical)", text);
        Assert.Contains($"hash {GoldenRun.Hash1950.Substring(0, 8)} -> changed", text);
        Assert.Contains($"changed 1950-02-27 hash {GoldenRun.Hash1950.Substring(0, 8)}", text);
    }

    [Fact]
    public void ARealOverrideShowsTheChangedLinesAndTheTable()
    {
        var text = Preview.Run(Root, GoldenRun.Until1950, GoldenRun.Seed, new[] { "tax_per_capita_per_year=0.00007" }, Show, 8);
        Assert.DoesNotContain("(the descriptions are identical)", text);
        Assert.Contains("- debt=", text);
        Assert.Contains("morale", text);
        Assert.Contains("tax_per_capita_per_year=0.00007", text);
    }

    [Fact]
    public void ThePreviewWritesNothingIntoContent()
    {
        string Snapshot() => string.Join("|", Directory.GetFiles(Root, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => f + ":" + File.GetLastWriteTimeUtc(f).Ticks + ":" + new FileInfo(f).Length));
        var before = Snapshot();
        Preview.Run(Root, new DateTime(1949, 1, 1), GoldenRun.Seed, new[] { "tax_per_capita_per_year=0.00007" }, Show, 8);
        Assert.Equal(before, Snapshot());
    }

    [Theory]
    [InlineData("tax_per_capita_per_yeer=0.1", "no top-level key")]
    [InlineData("budget_start=1", "is a block")]
    [InlineData("tax_per_capita_per_year", "wants key=value")]
    [InlineData("Tax=1", "not a rules.yaml key")]
    [InlineData("tax_per_capita_per_year=", "needs a value")]
    public void BadSetsFailWithAReason(string set, string message)
    {
        var ex = Assert.Throws<ArgumentException>(() => Preview.Run(Root, new DateTime(1949, 1, 1), GoldenRun.Seed, new[] { set }, Show, 8));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void AValueOfTheWrongTypeFailsInTheStrictLoader() =>
        Assert.Throws<ContentException>(() => Preview.Run(Root, new DateTime(1949, 1, 1), GoldenRun.Seed, new[] { "attention_per_turn=lots" }, Show, 8));

    [Fact]
    public void AnUnknownShownNameFails() =>
        Assert.Throws<ArgumentException>(() => Preview.Run(Root, new DateTime(1949, 1, 1), GoldenRun.Seed, None, new[] { "wealth" }, 8));

    [Fact]
    public void SetRuleReplacesOnlyThatLine()
    {
        var src = "a: 1   # note\nb: 2\nblock:\n  x: 1\n";
        Assert.Equal("a: 1   # note\nb: 5\nblock:\n  x: 1\n", Preview.SetRule(src, "b", "5"));
    }

    [Fact]
    public void SetRuleWorksOnACrlfFile() =>
        Assert.Equal("a: 1\r\nb: 5\r\n", Preview.SetRule("a: 1\r\nb: 2\r\n", "b", "5"));
}
