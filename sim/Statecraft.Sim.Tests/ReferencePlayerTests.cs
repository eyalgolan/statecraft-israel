using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;
using Statecraft.Sim.Reference;

/// <summary>The reference player (D-A7, #456): the GDD goldens' test policy. It ships in Statecraft.Sim so Unity can replay the
/// goldens (#394), but no game mode or UI may reach it (controller decision on PR 3, 2026-10-14).</summary>
public class ReferencePlayerTests
{
    static readonly Content Mvp = MvpSlice.Load(RealContentTests.ContentRoot());

    [Fact]
    public void ItSendsHistoricalAIsOrdersThenFillsTheJobs()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var orders = ReferencePlayer.Commands(Mvp, s);
        var history = HistoricalAI.Commands(Mvp, s);
        Assert.Equal(history.Select(x => x.Kind), orders.Take(history.Count).Select(x => x.Kind));
        var assign = Assert.IsType<AssignWorkers>(orders.Last());
        Assert.Equal(("tel_aviv", "job", 90), (assign.City, assign.Role, assign.Count));   // min(90 jobs, 100 civilians)
        Assert.Empty(Simulation.Step(Mvp, s, orders).Rejected);
    }

    [Fact]
    public void ItSendsNoOrderWhenTheJobsAreFilledOrAttentionIsShort()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var full = s.Clone();
        full.Cities["tel_aviv"] = full.Cities["tel_aviv"].WithCounts(100, 90, 0);
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, full), x => x is AssignWorkers);
        var tired = s.Clone();
        tired.Attention = 0;
        Assert.Empty(ReferencePlayer.Commands(Mvp, tired));
    }

    [Fact]
    public void ItFillsNoMoreJobsThanThereAreCivilians()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        s.Cities["tel_aviv"] = s.Cities["tel_aviv"].WithCounts(50, 40, 0);   // 90 jobs, 50 civilians
        var assign = Assert.IsType<AssignWorkers>(ReferencePlayer.Commands(Mvp, s).Last());
        Assert.Equal(50, assign.Count);
    }

    [Fact]
    public void ItIssuesNoGddOrderInTheLegacyLoop()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(HistoricalAI.Commands(c, s).Select(x => x.Kind), ReferencePlayer.Commands(c, s).Select(x => x.Kind));
    }

    /// <summary>Only the golden runner (MvpGolden) and tests may name it: no game mode, CLI command or Unity screen.</summary>
    [Fact]
    public void NoGameModeOrUiReachesIt()
    {
        Assert.Equal(new[] { "Historical", "Open" }, Enum.GetNames(typeof(Mode)));   // no mode plays it
        var repo = Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
        string Rel(string f) => Path.GetRelativePath(repo, f).Replace('\\', '/');
        var users = new[] { "sim/Statecraft.Sim", "sim/Statecraft.Cli", "unity/Assets", "tools" }
            .Select(d => Path.Combine(repo, d)).Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\bReferencePlayer\b|Statecraft\.Sim\.Reference\b"))
            .Select(Rel)
            .Where(f => !f.StartsWith("sim/Statecraft.Sim/Reference/", StringComparison.Ordinal) && !f.StartsWith("unity/Assets/Tests/", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal);
        Assert.Equal(new[] { "sim/Statecraft.Sim/MvpGolden.cs" }, users);
        Assert.Equal("Statecraft.Sim.Reference", typeof(ReferencePlayer).Namespace);
    }
}
