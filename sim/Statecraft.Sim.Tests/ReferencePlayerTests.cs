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
    public void ItSendsHistoricalAIsOrdersThenFillsTheJobsThenTrainsOneHalutzGroup()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var orders = ReferencePlayer.Commands(Mvp, s);
        var history = HistoricalAI.Commands(Mvp, s);
        Assert.Equal(history.Select(x => x.Kind), orders.Take(history.Count).Select(x => x.Kind));
        Assert.Equal(history.Count + 2, orders.Count);
        var assign = Assert.IsType<AssignWorkers>(orders[history.Count]);
        Assert.Equal(("tel_aviv", "job", 90), (assign.City, assign.Role, assign.Count));   // min(90 jobs, 100 civilians)
        var train = Assert.IsType<Train>(orders.Last());
        Assert.Equal(("tel_aviv", "halutz", 1), (train.City, train.Track, train.Count));   // 10 civilians left without work
        Assert.Empty(Simulation.Step(Mvp, s, orders).Rejected);
    }

    // R-TRAIN-001: the stated policy trains a halutz group only when there is none, none in training, a civilian without
    // work, and the Treasury and Attention to pay.
    [Fact]
    public void ItTrainsNoHalutzWhenOneExistsOrTrainsOrNobodyIsFreeOrMoneyIsShort()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var training = Simulation.Step(Mvp, s, ReferencePlayer.Commands(Mvp, s)).State;
        Assert.Single(training.Cohorts);
        training.Treasury = 100;   // only the cohort in training holds it back: Money, Attention and free civilians are there
        training.Attention = 10;
        Assert.True(training.Cities["tel_aviv"].Civilians > training.Cities["tel_aviv"].Employed);
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, training), x => x is Train);
        training.Cohorts.Clear();
        Assert.Contains(ReferencePlayer.Commands(Mvp, training), x => x is Train);
        var trained = s.Clone();
        trained.Units.Add(1, new Unit(1, "halutz", -3, 19));
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, trained), x => x is Train);
        var busy = s.Clone();
        busy.Cities["tel_aviv"] = busy.Cities["tel_aviv"].WithCounts(90, 80, 0);   // 90 jobs: all of them filled
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, busy), x => x is Train);
        var poor = s.Clone();
        poor.Treasury = 0.4;
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, poor), x => x is Train);
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
