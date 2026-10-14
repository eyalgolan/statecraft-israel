using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Scenario goldens (#296): every file in scenarios/ is played from its seed and checked against the manifest
/// (name "scenario-&lt;name&gt;") and a committed snapshot, like the hash goldens (docs/determinism.md).</summary>
public class ScenarioTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly Content Mvp = MvpSlice.Load(RealContentTests.ContentRoot());

    /// <summary>The content a scenario plays on: the real content, or the GDD slice for `content: mvp`.</summary>
    static Content For(Scenario sc) => sc.Content == Scenario.MvpContent ? Mvp : C;

    /// <summary>sim/Statecraft.Sim.Tests/scenarios, where the scenario files live (the runner, Scenario, is in the sim library).</summary>
    public static string Dir() =>
        Path.Combine(Directory.GetParent(RealContentTests.ContentRoot())!.FullName, "sim", "Statecraft.Sim.Tests", "scenarios");

    public static System.Collections.Generic.IEnumerable<object[]> Files() =>
        Directory.GetFiles(Dir(), "*.yaml").OrderBy(f => f, StringComparer.Ordinal).Select(f => new object[] { Path.GetFileNameWithoutExtension(f) });

    [Theory]
    [MemberData(nameof(Files))]
    public void ScenarioMatchesItsGolden(string file)
    {
        var sc = Scenario.Parse(File.ReadAllText(Path.Combine(Dir(), file + ".yaml")));
        Assert.Equal(file, sc.Name);
        var s = sc.Play(For(sc));
        var name = "scenario-" + sc.Name;
        // A new scenario has no manifest line yet: the blessing run (STATECRAFT_UPDATE_GOLDEN=1) adds it with its snapshot.
        var expected = GoldenManifest.UpdateRequested ? GoldenManifest.Read().GetValueOrDefault(name, "") : GoldenManifest.Expected(name);
        GoldenSnapshot.Check(name, s, expected);
    }

    [Fact]
    public void EachScenarioEndsElsewhereThanTheSameRunWithoutItsChoicesAndThanEveryOtherScenario()
    {
        var scenarios = Directory.GetFiles(Dir(), "*.yaml").OrderBy(f => f, StringComparer.Ordinal).Select(f => Scenario.Parse(File.ReadAllText(f))).ToList();
        Assert.True(scenarios.Count >= 2);
        var hashes = scenarios.Select(sc => sc.Play(For(sc)).Hash()).ToList();
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
        foreach (var (sc, hash) in scenarios.Zip(hashes, (sc, h) => (sc, h)))
        {
            var bare = Scenario.Parse($"name: {sc.Name}\nuntil: {sc.Until}\n" + (sc.Seed.HasValue ? $"seed: {sc.Seed}\n" : "") + (sc.Content != "" ? $"content: {sc.Content}\n" : ""));
            Assert.True(bare.Play(For(bare)).Hash() != hash, $"scenario '{sc.Name}' ends where the same run with no scripted command ends: its choices change nothing");
        }
    }

    const string Head = "name: t\nuntil: 1948-08-01\nsteps:\n  - on: 1948-05-15\n    commands:\n";

    [Fact]
    public void ARejectedCommandFailsTheRunWithTheDateAndTheReason()
    {
        var sc = Scenario.Parse(Head + "      - { kind: set_policy, policy: no_such_policy, level: 1.0 }\n");
        var e = Assert.Throws<InvalidOperationException>(() => sc.Play(C));
        Assert.Contains("1948-05-15", e.Message);
        Assert.Contains("set_policy", e.Message);
        Assert.Contains("no_such_policy", e.Message);
    }

    [Fact]
    public void AStepAfterTheRunEndsFailsInsteadOfBeingSkipped()
    {
        var sc = Scenario.Parse("name: t\nuntil: 1948-06-01\nsteps:\n  - on: 1949-01-01\n    commands:\n      - { kind: set_mobilization, level: 0.5 }\n");
        Assert.Contains("never ran", Assert.Throws<InvalidOperationException>(() => sc.Play(C)).Message);
    }

    [Theory]
    [InlineData("      - { kind: security_order, delta: 1.5 }\n", "'delta' must be a whole number")]
    [InlineData("      - { kind: map_action, action: housing, q: 2.5, r: 1 }\n", "'q' must be a whole number")]
    [InlineData("      - { kind: map_action, action: housing, q: 2, r: 1.1 }\n", "'r' must be a whole number")]
    public void AFractionalWholeNumberIsRefusedNotTruncated(string command, string expected) =>
        Assert.Contains(expected, Assert.Throws<InvalidDataException>(() => Scenario.Parse(Head + command)).Message);

    [Theory]
    [InlineData("      - { kind: fly_to_moon }\n", "unknown command kind 'fly_to_moon'")]
    [InlineData("      - { kind: set_policy, policy: rationing }\n", "needs 'level'")]
    [InlineData("      - { kind: set_policy, policy: rationing, level: 1, extra: 2 }\n", "unknown field 'extra'")]
    [InlineData("      - { policy: rationing }\n", "needs 'kind'")]
    [InlineData("      - { kind: set_policy, policy: rationing, level: lots }\n", "must be a number")]
    public void AMalformedCommandIsRefusedWithTheProblemNamed(string command, string expected) =>
        Assert.Contains(expected, Assert.Throws<InvalidDataException>(() => Scenario.Parse(Head + command)).Message);

    [Fact]
    public void TheMvpScenarioPlaysTheGddSlice()
    {
        var sc = Scenario.Parse(File.ReadAllText(Path.Combine(Dir(), "mvp-absorb-first.yaml")));
        Assert.Equal("mvp", sc.Content);
        Assert.True(For(sc).IsGdd);
        Assert.Equal(90, sc.Play(For(sc)).Cities["tel_aviv"].Employed);
    }

    [Fact]
    public void AnUnknownContentIsRefused() =>
        Assert.Contains("'content' must be mvp", Assert.Throws<InvalidDataException>(() => Scenario.Parse("name: t\ncontent: civ\nuntil: 1948-08-01\n")).Message);

    [Fact]
    public void ABadDateIsRefused() =>
        Assert.Contains("yyyy-MM-dd", Assert.Throws<InvalidDataException>(() => Scenario.Parse("name: t\nuntil: soon\n")).Message);
}
