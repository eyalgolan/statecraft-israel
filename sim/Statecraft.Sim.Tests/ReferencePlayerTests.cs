using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;
using Statecraft.Sim.Reference;

/// <summary>The reference player (D-A7, #456): the GDD goldens' test policy. It ships in Statecraft.Sim so Unity can replay the
/// goldens (#394), but no game mode or UI may reach it (controller decision on PR 3, 2026-10-14).</summary>
public class ReferencePlayerTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The game's start without its first card: since the cutover idf_founding is raised on 15 May, and answering
    /// it at full war takes the Attention a halutz group needs (the golden run trains on a later turn). These tests check
    /// the stated policy, not that turn.</summary>
    static GameState Start()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        s.PendingEvents.Clear();
        return s;
    }

    [Fact]
    public void ItSendsHistoricalAIsOrdersThenFillsTheJobsThenTrainsOneHalutzGroup()
    {
        var s = Start();
        s.Cities["tel_aviv"] = s.Cities["tel_aviv"].WithCounts(248, 96, 0);   // the start fills every job (Q-START-1): free 10
        var orders = ReferencePlayer.Commands(Mvp, s);
        var history = HistoricalAI.Commands(Mvp, s);
        Assert.Equal(history.Select(x => x.Kind), orders.Take(history.Count).Select(x => x.Kind));
        Assert.Equal(history.Count + 2, orders.Count);
        var assign = Assert.IsType<AssignWorkers>(orders[history.Count]);
        Assert.Equal(("tel_aviv", "job", 106), (assign.City, assign.Role, assign.Count));   // min(106 jobs, 248 civilians)
        var train = Assert.IsType<Train>(orders.Last());
        Assert.Equal(("tel_aviv", "halutz", 1), (train.City, train.Track, train.Count));   // 142 civilians left without work
        Assert.Empty(Simulation.Step(Mvp, s, orders).Rejected);
    }

    /// <summary>The real start: the 15 May card (idf_founding) is answered first, at full war's price. The cities start with
    /// every job filled (Q-START-1: job slots equal the start employed), so no AssignWorkers takes Attention, and the halutz
    /// group fits the rest of that turn's 4 Attention (Q-CUT-5): the player trains on the first turn, 15 May 1948 (before
    /// GDD Phase 3 PR 1 it filled Tel Aviv's jobs first and trained from 15 August 1948). Every order it sends is taken.</summary>
    [Fact]
    public void OnTheRealStartItAnswersTheCardFirstAndTrainsWhenAttentionAllows()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Contains("idf_founding", s.PendingEvents);
        var first = ReferencePlayer.Commands(Mvp, s);
        Assert.IsType<ChooseEventOption>(first[0]);
        Assert.DoesNotContain(first, x => x is AssignWorkers);
        Assert.Contains(first, x => x is Train);
        DateTime? trained = null;
        while (trained == null && s.GameOver == null && s.Turn < 1000)
        {
            var orders = ReferencePlayer.Commands(Mvp, s);
            var r = Simulation.Step(Mvp, s, orders);
            Assert.Empty(r.Rejected);
            if (orders.Any(x => x is Train)) trained = s.Date;
            s = r.State;
        }
        Assert.Equal(new DateTime(1948, 5, 15), trained);
    }

    // R-TRAIN-001: the stated policy trains a halutz group only when there is none, none in training, a civilian without
    // work, and the Attention to pay. Training borrows, so Money does not hold it back (owner decision, 2026-10-15).
    [Fact]
    public void ItTrainsNoHalutzWhenOneExistsOrTrainsOrNobodyIsFreeButDoesWithNoMoney()
    {
        var s = Start();
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
        busy.Cities["tel_aviv"] = busy.Cities["tel_aviv"].WithCounts(106, 106, 0);   // 106 jobs: all filled, nobody free
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, busy), x => x is Train);
        var poor = s.Clone();
        poor.Treasury = 0;   // training borrows (owner decision, 2026-10-15): an empty Treasury does not hold it back
        Assert.Contains(ReferencePlayer.Commands(Mvp, poor), x => x is Train);
    }

    // R-SITE-001: the stated policy sends each idle halutz group, in id order, to the first available yishuv site in id order.
    // Pioneer work borrows (question 13, owner decision 2026-10-15), so an empty Treasury does not hold it back.
    [Fact]
    public void ItSendsAnIdleHalutzToTheFirstAvailableSite()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        s.Units.Add(7, new Unit(7, "halutz", -3, 19));
        s.Units.Add(8, new Unit(8, "halutz", -3, 19));
        s.NextId = 9;
        s.Treasury = 10;
        // Two groups, four yishuv sites (Q-SITE-1, #785): each group takes the next free site in ordinal id order.
        var builds = ReferencePlayer.Commands(Mvp, s).OfType<PioneerWork>().ToList();
        Assert.Equal(new[] { (7, "build_yishuv", "beit_kama"), (8, "build_yishuv", "gilat") }, builds.Select(b => (b.Unit, b.Action, b.Site)));
        Assert.Empty(Simulation.Step(Mvp, s, ReferencePlayer.Commands(Mvp, s)).Rejected);

        var oneLeft = s.Clone();
        oneLeft.Sites["beit_kama"] = new SiteWork("beit_kama", 9, 3, null);
        oneLeft.Sites["gilat"] = SiteWork.Built("gilat", new DateTime(1949, 6, 15));
        oneLeft.Sites["mefalsim"] = SiteWork.Built("mefalsim", new DateTime(1949, 6, 15));
        // One yishuv site left: the first group takes it, and the second builds the first camp site (R-CAMP-001).
        var lastTwo = ReferencePlayer.Commands(Mvp, oneLeft).OfType<PioneerWork>().Select(b => (b.Unit, b.Action, b.Site));
        Assert.Equal(new[] { (7, "build_yishuv", "reim"), (8, "build_camp", "camp_80") }, lastTwo);
        var none = oneLeft.Clone();   // every yishuv and camp site taken: the idle group waits
        none.Sites["reim"] = new SiteWork("reim", 7, 3, null);
        none.Sites["camp_80"] = SiteWork.Built("camp_80", new DateTime(1949, 6, 15));
        none.Sites["tzrifin"] = SiteWork.Built("tzrifin", new DateTime(1949, 6, 15));
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, none), x => x is PioneerWork);
        var poor = s.Clone();
        poor.Treasury = 0;
        Assert.Contains(ReferencePlayer.Commands(Mvp, poor), x => x is PioneerWork);
    }

    /// <summary>The GDD fixture with two yishuv sites, site_a on (2, 0) and site_b on (3, 0), and pioneer_work priced at
    /// <paramref name="cost"/> Attention.</summary>
    static Content TwoSites(int cost) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["sites.yaml"] = Fixture.GddSites + Fixture.GddSites.Replace("sites:\n", "").Replace("site_a", "site_b").Replace("Site A", "Site B").Replace("q: 2", "q: 3"),
        ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("map_action: 1 }", Fixture.GddCosts.Replace("pioneer_work: 0", "pioneer_work: " + cost)) + Fixture.GddRules,
    })));

    static GameState TwoHalutzim(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        for (int i = 0; i < 2; i++) { var id = s.NextId++; s.Units.Add(id, new Unit(id, "halutz", 1, 0)); }
        return s;
    }

    // R-SITE-001: a busy unit is not sent again, even when another site is free; the idle one takes it.
    [Fact]
    public void ItSendsNoBusyUnit()
    {
        var c = TwoSites(0);
        var s = TwoHalutzim(c);
        var (busy, idle) = (s.Units.Keys[0], s.Units.Keys[1]);
        s.Sites["site_a"] = new SiteWork("site_a", busy, 2, null);
        var build = Assert.Single(ReferencePlayer.Commands(c, s).OfType<PioneerWork>());
        Assert.Equal((idle, "site_b"), (build.Unit, build.Site));
    }

    // R-SITE-001: the policy sends builds only while the turn's Attention lasts (the slice prices them at 0; this fixture at 1).
    [Fact]
    public void ItSendsNoMoreBuildsThanAttentionPays()
    {
        var c = TwoSites(1);
        var s = TwoHalutzim(c);
        s.Cities["town_a"] = s.Cities["town_a"].WithCounts(8, 8, 0);   // the jobs are filled: no AssignWorkers takes Attention
        var history = HistoricalAI.Commands(c, s).Sum(x => AttentionSystem.Cost(c, s, x.Kind));
        var price = AttentionSystem.Cost(c, s, Projects.PioneerKind);
        Assert.True(price > 0);
        s.Attention = history + price * 1.5;   // one build, not two
        var build = Assert.Single(ReferencePlayer.Commands(c, s).OfType<PioneerWork>());
        Assert.Equal((s.Units.Keys[0], "site_a"), (build.Unit, build.Site));
        s.Attention = history + price * 0.5;
        Assert.DoesNotContain(ReferencePlayer.Commands(c, s), x => x is PioneerWork);
    }

    [Fact]
    public void ItSendsNoOrderWhenTheJobsAreFilledOrAttentionIsShort()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var full = s.Clone();
        full.Cities["tel_aviv"] = full.Cities["tel_aviv"].WithCounts(120, 106, 0);   // all 106 jobs filled
        Assert.DoesNotContain(ReferencePlayer.Commands(Mvp, full), x => x is AssignWorkers);
        var tired = s.Clone();
        tired.Attention = 0;
        Assert.Empty(ReferencePlayer.Commands(Mvp, tired));
    }

    [Fact]
    public void ItFillsNoMoreJobsThanThereAreCivilians()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        s.Cities["tel_aviv"] = s.Cities["tel_aviv"].WithCounts(50, 40, 0);   // 106 jobs, 50 civilians
        var assign = Assert.IsType<AssignWorkers>(ReferencePlayer.Commands(Mvp, s).Last());
        Assert.Equal(50, assign.Count);
    }

    [Fact]
    public void ItIssuesNoGddOrderInTheLegacyLoop()
    {
        var c = ContentLoader.Load(Fixture.Write());   // the legacy loop (a fixture since the cutover)
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(HistoricalAI.Commands(c, s).Select(x => x.Kind), ReferencePlayer.Commands(c, s).Select(x => x.Kind));
    }

    /// <summary>Only the golden runner (GoldenRun) and tests may name it: no game mode, CLI command or Unity screen.</summary>
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
        Assert.Equal(new[] { "sim/Statecraft.Sim/GoldenRun.cs" }, users);
        Assert.Equal("Statecraft.Sim.Reference", typeof(ReferencePlayer).Namespace);
    }
}
