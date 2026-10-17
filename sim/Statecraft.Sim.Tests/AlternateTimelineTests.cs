using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The alternate-timeline mechanism (docs/superpowers/plans/2026-10-17-alternate-timelines.md).</summary>
public class AlternateTimelineTests
{
    internal const string Parent = @"
id: parent
title: Parent
text: 'A real decision.'
from: 1948-06-01
to: 1948-06-11
options:
  - id: record
    text: What happened
    historical: true
  - id: road
    text: The road not taken
    alternative:
      basis: proposed
      by: the opposition benches
      date: 1948-06-05
      sources: [{ url: ""https://example.org/minutes"", checked: 2026-10-17 }]
";

    internal static Content Load(params (string file, string yaml)[] events) =>
        ContentLoader.Load(Fixture.Write(events: events.ToDictionary(x => x.file, x => x.yaml)));

    static OptionAlternative Road(Content c) => c.Events.Single().Options.Single(o => o.Id == "road").Alternative!;

    // R-ALT-003
    [Fact]
    public void TheAlternativeRecordIsReadFromYaml()
    {
        var a = Road(Load(("parent.yaml", Parent)));
        Assert.Equal("proposed", a.Basis);
        Assert.Equal("the opposition benches", a.By);
        Assert.Equal(new DateTime(1948, 6, 5), a.Date);
        Assert.Equal("https://example.org/minutes", Assert.Single(a.Sources).Url);
        Assert.Equal(new DateTime(2026, 10, 17), a.Sources[0].Checked);
        Assert.True(a.IsConsidered);
        Assert.Null(Load(("parent.yaml", Parent)).Events.Single().Options.Single(o => o.Id == "record").Alternative);
    }

    // R-ALT-003
    [Theory]
    [InlineData("basis: proposed", "basis: maybe", "alternative.basis", "must be proposed, debated or design")]
    [InlineData("by: the opposition benches", "by: ''", "alternative.by", "a proposed or debated alternative names who put it forward")]
    [InlineData("by: the opposition benches", "by: '  '", "alternative.by", "a proposed or debated alternative names who put it forward")]
    [InlineData("by: the opposition benches", "by: '{prime_minister}'", "alternative.by", "words only, no placeholder")]
    [InlineData("      date: 1948-06-05\n", "", "alternative.date", "needs the date it was put forward")]
    [InlineData("sources: [{ url: \"https://example.org/minutes\", checked: 2026-10-17 }]", "sources: []", "alternative.sources", "needs at least one source")]
    [InlineData("https://example.org/minutes", "http://example.org/minutes", "alternative.sources", "needs an https url and a checked date")]
    [InlineData(", checked: 2026-10-17 }]", " }]", "alternative.sources", "needs an https url and a checked date")]
    public void ABadAlternativeIsALoadError(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", Parent.Replace(from, to))));
        Assert.Equal("events/parent.yaml", ex.File);
        Assert.Equal("events[parent].options[road]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-ALT-003
    [Fact]
    public void ADebatedAlternativeLoadsLikeAProposedOne()
    {
        var a = Road(Load(("parent.yaml", Parent.Replace("basis: proposed", "basis: debated"))));
        Assert.Equal("debated", a.Basis);
        Assert.True(a.IsConsidered);
    }

    // R-ALT-003
    [Fact]
    public void ADesignAlternativeNamesNobody()
    {
        var design = Parent.Replace("basis: proposed", "basis: design");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", design)));
        Assert.Equal("events[parent].options[road].alternative.by", ex.Field);
        Assert.Contains("a design alternative names nobody and no date", ex.Message);
        var dated = design.Replace("      by: the opposition benches\n", "");
        var ex2 = Assert.Throws<ContentException>(() => Load(("parent.yaml", dated)));
        Assert.Contains("a design alternative names nobody and no date", ex2.Message);
        var bare = design.Replace("      by: the opposition benches\n      date: 1948-06-05\n", "");
        Assert.False(Road(Load(("parent.yaml", bare))).IsConsidered);
        var plain = Parent.Substring(0, Parent.IndexOf("      basis:", StringComparison.Ordinal)) + "      basis: design\n";
        Assert.Empty(Road(Load(("parent.yaml", plain))).Sources);   // sources are optional for a design alternative
    }

    // R-ALT-003
    [Fact]
    public void TheHistoricalOptionHasNoAlternativeRecord()
    {
        var bad = Parent.Replace("    historical: true\n", "    historical: true\n    alternative: { basis: design }\n");
        var ex = Assert.Throws<ContentException>(() => Load(("parent.yaml", bad)));
        Assert.Equal("events[parent].options[record].alternative", ex.Field);
        Assert.Contains("the historical option is the record, not an alternative", ex.Message);
    }

    // R-ALT-003: optional at load until #949 requires it, so the content on main loads unchanged.
    [Fact]
    public void AnOptionWithoutARecordStillLoads()
    {
        var bare = Parent.Substring(0, Parent.IndexOf("    alternative:", StringComparison.Ordinal));
        Assert.Null(Load(("parent.yaml", bare)).Events.Single().Options.Single(o => o.Id == "road").Alternative);
    }
}
