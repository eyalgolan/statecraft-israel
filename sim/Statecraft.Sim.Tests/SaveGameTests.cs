using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Saves: seed + command log + content hash + schema version; loading replays to the same state hash.</summary>
public class SaveGameTests
{
    /// <summary>The fixture with a two-office budget, so a saved AllocateBudget has more than one share.</summary>
    static string Rules(string treasury = "100") =>
        Fixture.Files["rules.yaml"].Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { finance_minister: 0.6, defense_minister: 0.4 }")
            .Replace("treasury_start: 100", "treasury_start: " + treasury);

    static Content Fixture1(string treasury = "100") => ContentLoader.Load(Fixture.Write(
        new Dictionary<string, string> { ["rules.yaml"] = Rules(treasury) },
        new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event }));

    /// <summary>Ten turns using every command kind, including ones the sim rejects.</summary>
    static GameSession Played(Content c)
    {
        var g = GameSession.New(c, 99, Mode.Historical);
        var orders = new List<IReadOnlyList<Command>>
        {
            new Command[] { new SetMobilization(0.9), new SecurityOrder(-2) },
            new Command[] { new ChooseEventOption("drought", "ration"), new SetPolicy("rationing", 1.0 / 3) },
            new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 0.75, ["defense_minister"] = 0.25 }) },
            new Command[] { new Appoint("finance_minister", "pm_one"), new MapAction("housing", 2, 0) },
            new Command[] { new SetPolicy("odd id with spaces%", 0.5), new MapAction("transit_camp", 4, 0) },
            new Command[] { new AssignWorkers("town_a", "job", 1) },   // a GDD order: the legacy fixture refuses it
            new Command[] { new Train("town_a", "halutz", 1), new CancelTraining(1) },   // GDD orders too
            new Command[] { new PioneerWork(1, "build_yishuv", "site_a"), new StopWork(1) },
            new Command[] { new SecurityOrder(1) }, new Command[0],
        };
        foreach (var o in orders) g.Step(o);
        return g;
    }

    [Fact]
    public void ASaveReplaysToTheSameStateHash()
    {
        var c = Fixture1();
        var played = Played(c);
        var text = played.Save.Write();
        var loaded = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(played.State.Hash(), loaded.State.Hash());
        Assert.Equal(10, loaded.Save.Turns.Count);
        Assert.Equal(text, loaded.Save.Write());                         // writing the loaded save gives the same text
        Assert.Equal(played.Previous!.Hash(), loaded.Previous!.Hash());  // "since last turn" survives a load
    }

    /// <summary>#398 review S1: a load also keeps the step before the last, so a change that compares two steps (the top
    /// bar's arrivals) is the one the player saw before the reload. A new session and one step have none.</summary>
    [Fact]
    public void ALoadKeepsTheStepBeforeTheLast()
    {
        var c = Fixture1();
        var played = Played(c);
        var loaded = GameSession.Load(c, played.Save.Write(), out _);
        Assert.NotNull(played.BeforeLast);
        Assert.Equal(played.BeforeLast!.State.Hash(), loaded.BeforeLast!.State.Hash());
        Assert.Equal(played.BeforeLast.Why.Select(w => (w.System, w.Delta)), loaded.BeforeLast.Why.Select(w => (w.System, w.Delta)));
        Assert.Equal(played.Last!.State.Hash(), loaded.Last!.State.Hash());
        var fresh = GameSession.New(c, 99, Mode.Historical);
        Assert.Null(fresh.BeforeLast);
        fresh.Step(new Command[0]);
        Assert.Null(fresh.BeforeLast);
        var first = fresh.Last;
        fresh.Step(new Command[0]);
        Assert.Same(first, fresh.BeforeLast);
    }

    [Fact]
    public void TheSaveRecordsTheHeaderAndExactNumbers()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var lines = text.Split('\n');
        Assert.Equal("statecraft-save 2", lines[0]);
        Assert.Equal("content " + c.Hash, lines[1]);
        Assert.Equal("seed 99", lines[2]);
        Assert.Equal("mode Historical", lines[3]);
        Assert.Equal("ruleset legacy", lines[4]);
        Assert.Contains("policy rationing 3FD5555555555555", lines);       // 1/3, bit for bit
        Assert.Contains("policy odd%20id%20with%20spaces%25 3FE0000000000000", lines);
        Assert.Contains("budget defense_minister=3FD0000000000000,finance_minister=3FE8000000000000", lines);
        Assert.Contains("map housing 2 0", lines);
        Assert.Contains("security -2", lines);
        Assert.StartsWith("end ", lines[lines.Length - 2]);
    }

    [Fact]
    public void TheHistoricalPlaythroughTo1952RoundTrips()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < GoldenRun.Until1952 && g.State.GameOver == null) g.Step(GoldenRun.Orders(c, g.State));
        Assert.Equal(GoldenRun.Hash1952, g.State.Hash());
        var loaded = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(GoldenRun.Hash1952, loaded.State.Hash());
    }

    [Fact]
    public void OtherContentLoadsWithAWarning()
    {
        var text = Played(Fixture1()).Save.Write();
        var other = Fixture1("90");
        GameSession.Load(other, text, out var warnings);
        Assert.Equal(new[] { "This save was made with different game content; the replayed game may differ.", "The replayed game differs from the saved one." }, warnings);
    }

    [Fact]
    public void ADivergingReplayWithTheSameContentIsAnError()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var lines = text.Split('\n');
        lines[2] = "seed 100";                                                // same content, different game
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Contains("The replay does not match the saved game", ex.Message);
    }

    [Theory]
    [InlineData("statecraft-save 3", 1, "line 1: save schema version 3 is newer than this game (2)")]
    [InlineData("statecraft-save x", 1, "line 1: bad schema version")]
    [InlineData("chess-save 1", 1, "line 1: expected 'statecraft-save'")]
    public void BadHeadersAreRejected(string first, int line, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n');
        lines[0] = first;
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal((line, message), (ex.Line, ex.Message));
    }

    [Theory]
    [InlineData("fly me 1 2", "unknown command 'fly'")]
    [InlineData("policy rationing 0.5", "bad number '0.5'")]
    [InlineData("map housing two 0", "bad integer 'two'")]
    [InlineData("event drought", "'event' needs 2 values")]
    public void BadCommandLinesNameTheLine(string command, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n').ToList();
        lines.Insert(6, command);                                             // after the first "turn" (schema 2: the ruleset line is line 5)
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal(7, ex.Line);
        Assert.Equal("line 7: " + message, ex.Message);
    }

    [Fact]
    public void TruncatedTextIsRejected()
    {
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse("statecraft-save 1\ncontent x\n"));
        Assert.Equal("Not a Statecraft save (too short)", ex.Message);
    }

    [Fact]
    public void TheContentHashCoversEveryFileAndIgnoresLineEndings()
    {
        var a = ContentLoader.Load(Fixture.Write());
        var b = ContentLoader.Load(Fixture.Write());
        Assert.Equal(a.Hash, b.Hash);
        Assert.Equal(64, a.Hash.Length);
        var crlf = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["parties.yaml"] = Fixture.Files["parties.yaml"].Replace("\n", "\r\n") }));
        Assert.Equal(a.Hash, crlf.Hash);
        var map = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = Fixture.Files["map/settlement_hexes.yaml"] + "# note\n" }));
        Assert.NotEqual(a.Hash, map.Hash);
    }

    /// <summary>Every command type the sim has must be savable: the played session uses each one, and a new Command type
    /// that the save format cannot write fails here.</summary>
    [Fact]
    public void EveryCommandTypeIsInTheSavedSession()
    {
        var all = typeof(Command).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract).Select(t => t.Name).OrderBy(x => x).ToList();
        var used = Played(Fixture1()).Save.Turns.SelectMany(x => x).Select(x => x.GetType().Name).Distinct().OrderBy(x => x).ToList();
        Assert.Equal(all, used);
    }

    // ---- fix round 1: immutability, null/surrogate ids, hostile input, damaged saves, encoding ----

    static string Lines(GameSession g, Func<string[], string[]> edit) => string.Join("\n", edit(g.Save.Write().Split('\n')));
    static string Hex64(char c) => new string(c, 64);

    [Fact]
    public void ARecordedTurnDoesNotChangeWhenTheCallerMutatesItsOrders()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var shares = c.Rules.BudgetStart.ToDictionary(kv => kv.Key, kv => kv.Value);
        var g = GameSession.New(c, 5, Mode.Historical);
        g.Step(new Command[] { new AllocateBudget(shares) });
        g.Step(new Command[0]);
        var before = g.Save.Write();
        shares.Remove(shares.Keys.First());                         // the caller reuses its own dictionary afterwards
        shares["prime_minister"] = 99;
        Assert.Equal(before, g.Save.Write());
        var loaded = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
    }

    [Fact]
    public void ANullIdIsRefusedAtStepSoTheSaveStaysWritable()
    {
        var g = GameSession.New(Fixture1(), 1, Mode.Historical);
        Assert.Throws<ArgumentNullException>(() => g.Step(new Command[] { new SetPolicy(null!, 1) }));
        Assert.Throws<ArgumentNullException>(() => g.Step(new Command[] { new ChooseEventOption("drought", null!) }));
        Assert.Throws<ArgumentNullException>(() => g.Step(new Command[] { new Appoint(null!, "pm_one") }));
        Assert.Throws<ArgumentNullException>(() => g.Step(new Command[] { new MapAction(null!, 1, 1) }));
        Assert.Empty(g.Save.Turns);                                  // nothing half-recorded
        Assert.StartsWith("statecraft-save 2\n", g.Save.Write());
    }

    [Fact]
    public void ALoneSurrogateIsRefusedAtStep()
    {
        var g = GameSession.New(Fixture1(), 1, Mode.Historical);
        Assert.Throws<ArgumentException>(() => g.Step(new Command[] { new SetPolicy("bad\uD800id", 1) }));
        g.Step(new Command[] { new SetPolicy("fine\uD83D\uDE00id", 1) });          // a valid pair is fine and round-trips
        var loaded = GameSession.Load(Fixture1(), g.Save.Write(), out _);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
    }

    [Fact]
    public void ANullBudgetReplaysWithTheSameRejectionReason()
    {
        var c = Fixture1();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new AllocateBudget(null!) });
        var live = g.Last!.Rejected.Single().Reason;
        var loaded = GameSession.Load(c, g.Save.Write(), out _);
        Assert.Equal(live, loaded.Last!.Rejected.Single().Reason);
        Assert.Equal("Budget shares are required", live);
        var empty = GameSession.New(c, 1, Mode.Historical);
        empty.Step(new Command[] { new AllocateBudget(new Dictionary<string, double>()) });
        Assert.NotEqual(live, empty.Last!.Rejected.Single().Reason);   // an empty map is a different mistake
        Assert.Equal(empty.Last.Rejected.Single().Reason, GameSession.Load(c, empty.Save.Write(), out _).Last!.Rejected.Single().Reason);
    }

    [Theory]
    [InlineData("mode Open", 4, "mode Open is not supported")]
    [InlineData("mode 0", 4, "bad mode")]
    [InlineData("mode Foo", 4, "bad mode")]
    [InlineData("mode ", 4, "bad mode")]
    public void ModesMustBeNamesTheGameCanStart(string mode, int line, string message)
    {
        var g = Played(Fixture1());
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(Fixture1(), Lines(g, l => { l[3] = mode; return l; }), out _));
        Assert.Equal((line, "line 4: " + message), (ex.Line, ex.Message));
    }

    [Fact]
    public void AHugeTurnCountIsRefusedBeforeReplay()
    {
        var text = "statecraft-save 1\ncontent " + Hex64('a') + "\nseed 1\nmode Historical\n" + string.Concat(Enumerable.Repeat("turn\n", 400000)) + "end " + Hex64('b') + "\n";
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse(text));
        Assert.Equal("save has 400000 turns; the most this game plays is 2000", ex.Message);
        var ok = "statecraft-save 1\ncontent " + Hex64('a') + "\nseed 1\nmode Historical\n" + string.Concat(Enumerable.Repeat("turn\n", 2000)) + "end " + Hex64('b') + "\n";
        Assert.Equal(2000, SaveGame.Parse(ok).Turns.Count);
    }

    /// <summary>The replay runs under content whose step throws (a MapAction with no attention cost line): the exception
    /// must surface as a SaveException that names the turn and keeps the original exception for the log (#631).</summary>
    [Fact]
    public void AReplayThatThrowsIsASaveExceptionNamingTheTurn()
    {
        var text = Played(Fixture1()).Save.Write();
        var broken = Fixture1();
        broken.Rules.AttentionCost.Remove("map_action");
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(broken, text, out _));
        Assert.StartsWith("turn 4: ", ex.Message);
        var inner = Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Equal("turn 4: " + inner.Message, ex.Message);
        Assert.Equal(0, ex.Line);
    }

    [Theory]
    [InlineData("content xyz", 2, "bad content hash")]
    [InlineData("end 12", -1, "bad end hash")]
    [InlineData("end ZZZZ", -1, "bad end hash")]
    public void DamagedHashesAreNamedAsSuch(string replacement, int lineIndex, string message)
    {
        var g = Played(Fixture1());
        var lines = g.Save.Write().Split('\n');
        var idx = lineIndex < 0 ? lines.Length - 2 : lineIndex - 1;
        lines[idx] = replacement;
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(Fixture1(), string.Join("\n", lines), out _));
        Assert.Equal("line " + (idx + 1) + ": " + message, ex.Message);
    }

    [Fact]
    public void AnEditedCommandWithTheSameContentSaysTheSaveIsDamagedNotThatTheGameHasABug()
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n');
        var i = Array.FindIndex(lines, l => l.StartsWith("policy rationing "));
        lines[i] = "policy rationing 3FE0000000000000";
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal("The replay does not match the saved game: the save is damaged or was edited, or the game has a bug.", ex.Message);
    }

    [Theory]
    [InlineData("policy bad%ZZ 3FE0000000000000", "bad text 'bad%ZZ'")]
    [InlineData("policy bad%4 3FE0000000000000", "bad text 'bad%4'")]
    [InlineData("policy bad%FF 3FE0000000000000", "bad text 'bad%FF'")]
    public void MalformedEscapesAreSaveExceptionsWithALine(string command, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n').ToList();
        lines.Insert(6, command);
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal("line 7: " + message, ex.Message);
    }

    [Fact]
    public void TheEscapingIsOurOwnAndMatchesRfc3986()
    {
        var g = GameSession.New(Fixture1(), 1, Mode.Historical);
        g.Step(new Command[] { new SetPolicy("A-z_0.9~ \u00E9\u20AC\uD83D\uDE00/", 1) });
        Assert.Contains("policy A-z_0.9~%20%C3%A9%E2%82%AC%F0%9F%98%80%2F 3FF0000000000000", g.Save.Write().Split('\n'));
    }

    [Fact]
    public void ABudgetSummedInAnyInsertionOrderIsJudgedAlike()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var v = new[] { 0.37466668006693443, 0.1119292744233292, 0.016696259782142436, 0.014296586668015622, 0.010296855346174184, 0.19600249013696078, 0.12343932175585436, 0.152673531820589 };
        var keys = c.Rules.BudgetStart.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var forward = new Dictionary<string, double>(); var backward = new Dictionary<string, double>();
        for (int i = 0; i < keys.Count; i++) forward[keys[i]] = v[i];
        for (int i = keys.Count - 1; i >= 0; i--) backward[keys[i]] = v[i];
        var s1 = Simulation.NewGame(c, 1, Mode.Historical); var s2 = Simulation.NewGame(c, 1, Mode.Historical);
        var r1 = Simulation.Step(c, s1, new Command[] { new AllocateBudget(forward) });
        var r2 = Simulation.Step(c, s2, new Command[] { new AllocateBudget(backward) });
        Assert.Equal(r1.Rejected.Select(x => x.Reason), r2.Rejected.Select(x => x.Reason));
        Assert.Equal(r1.State.Hash(), r2.State.Hash());
    }

    [Fact]
    public void ABomOnContentOrOnASaveChangesNothing()
    {
        var plain = ContentLoader.Load(Fixture.Write());
        var dir = Fixture.Write();
        var withBom = ContentLoader.Load(rel =>
        {
            var path = Path.Combine(dir, rel);
            return File.Exists(path) ? "\uFEFF" + File.ReadAllText(path) : null;
        }, new string[0]);
        Assert.Equal(plain.Hash, withBom.Hash);
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var loaded = GameSession.Load(c, "\uFEFF" + text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(Played(c).State.Hash(), loaded.State.Hash());
    }

    /// <summary>content/map/settlement_population_1945.yaml sizes the map's settlement kits (presentation only). The sim
    /// never reads it, so it is outside the content hash: editing it cannot break a save or move a golden. Checked on the
    /// fixture and on the real content (with the file removed from a copy).</summary>
    [Fact]
    public void TheKitPopulationFileIsOutsideTheContentHash()
    {
        var dir = Fixture.Write();
        var before = ContentLoader.Load(dir).Hash;
        File.WriteAllText(Path.Combine(dir, "map", "settlement_population_1945.yaml"), "settlements:\n- id: x\n  population: 1\n");
        Assert.Equal(before, ContentLoader.Load(dir).Hash);

        var real = RealContentTests.ContentRoot();
        Assert.True(File.Exists(Path.Combine(real, "map", "settlement_population_1945.yaml")));
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-nopop-" + Guid.NewGuid().ToString("N"));
        foreach (var f in Directory.GetFiles(real, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(real, f);
            if (rel.Replace('\\', '/') == "map/settlement_population_1945.yaml") continue;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(copy, rel))!);
            File.Copy(f, Path.Combine(copy, rel));
        }
        Assert.Equal(ContentLoader.Load(real).Hash, ContentLoader.Load(copy).Hash);
    }

    /// <summary>content/map/localities_1948.yaml (the 1948 localities and Bedouin camp areas the map's kits and labels draw)
    /// and its baked runtime form localities_1948.tsv are presentation only, like the population file: the sim never reads it, so it is outside the content hash and no save
    /// or golden depends on it. Checked on the fixture (a file added) and on the real content (the file removed from a copy).</summary>
    [Fact]
    public void TheLocalitiesFileIsOutsideTheContentHash()
    {
        var dir = Fixture.Write();
        var before = ContentLoader.Load(dir).Hash;
        File.WriteAllText(Path.Combine(dir, "map", "localities_1948.yaml"), "status: draft\nplaces:\n- id: x\n  population: 1\n");
        File.WriteAllText(Path.Combine(dir, "map", "localities_1948.tsv"), "place\tx\n");
        Assert.Equal(before, ContentLoader.Load(dir).Hash);

        var real = RealContentTests.ContentRoot();
        Assert.True(File.Exists(Path.Combine(real, "map", "localities_1948.yaml")));
        Assert.True(File.Exists(Path.Combine(real, "map", "localities_1948.tsv")));
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-noloc-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var f in Directory.GetFiles(real, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(real, f);
                if (rel.Replace('\\', '/') is "map/localities_1948.yaml" or "map/localities_1948.tsv") continue;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(copy, rel))!);
                File.Copy(f, Path.Combine(copy, rel));
            }
            Assert.Equal(ContentLoader.Load(real).Hash, ContentLoader.Load(copy).Hash);
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }   // review M13: no temp copy left behind
    }

    /// <summary>content/map/route_paths_1948.tsv, the baked runtime form of route_paths_1948.yaml the map reads at boot
    /// (issue #321), is map-only like the YAML: the sim never reads it, so it is outside the content hash and no save or
    /// golden depends on it. Checked as the localities' file: on the fixture (added) and on the real content (removed).</summary>
    [Fact]
    public void TheRoutePathsRuntimeFileIsOutsideTheContentHash()
    {
        var dir = Fixture.Write();
        var before = ContentLoader.Load(dir).Hash;
        File.WriteAllText(Path.Combine(dir, "map", "route_paths_1948.tsv"), "meta\tversion\t2\n");
        Assert.Equal(before, ContentLoader.Load(dir).Hash);

        var real = RealContentTests.ContentRoot();
        Assert.True(File.Exists(Path.Combine(real, "map", "route_paths_1948.tsv")));
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-noroutetsv-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var f in Directory.GetFiles(real, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(real, f);
                if (rel.Replace('\\', '/') == "map/route_paths_1948.tsv") continue;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(copy, rel))!);
                File.Copy(f, Path.Combine(copy, rel));
            }
            Assert.Equal(ContentLoader.Load(real).Hash, ContentLoader.Load(copy).Hash);
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }
    }

    [Fact]
    public void TheContentHashChangesWithEventsAndSettlements()
    {
        var a = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event }));
        var b = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event + "# edited\n" }));
        Assert.NotEqual(a.Hash, b.Hash);
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"] + "# note\n" }));
        Assert.NotEqual(ContentLoader.Load(Fixture.Write()).Hash, c.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("statecraft-save 1\ncontent x\n")]
    [InlineData("statecraft-save 0")]
    public void ShortInputsAreSaveExceptions(string text) => Assert.Throws<SaveException>(() => SaveGame.Parse(text));

    [Fact]
    public void AMissingOrShortEndLineIsReportedAsDamage()
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n').ToList();
        lines.RemoveAt(lines.Count - 2);                                    // drop "end <hash>"
        Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
    }

    [Theory]
    [InlineData("statecraft-save 0", "unknown save schema version 0")]
    [InlineData("statecraft-save 3", "save schema version 3 is newer than this game (2)")]
    [InlineData("statecraft-save -1", "bad schema version")]
    public void SchemaVersionsOutsideOneAreRefused(string first, string message)
    {
        var lines = Played(Fixture1()).Save.Write().Split('\n');
        lines[0] = first;
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse(string.Join("\n", lines)));
        Assert.Equal("line 1: " + message, ex.Message);
    }

    [Fact]
    public void CrLfLoadsAndALoneCrIsNotASave()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        GameSession.Load(c, text.Replace("\n", "\r\n"), out var warnings);
        Assert.Empty(warnings);
        Assert.Throws<SaveException>(() => SaveGame.Parse(text.Replace("\n", "\r")));
    }

    sealed class Foreign : Command { public override string Kind => "set_policy"; }

    [Fact]
    public void AForeignCommandNeverMakesTheSaveUnwritable()
    {
        var g = GameSession.New(Fixture1(), 1, Mode.Historical);
        Assert.Throws<ArgumentException>(() => g.Step(new Command[] { new Foreign() }));
        g.Step(new Command[0]);
        Assert.StartsWith("statecraft-save 2\n", g.Save.Write());
        Assert.Single(g.Save.Turns);
    }

    [Fact]
    public void MutatingWhatTurnsReturnsDoesNotChangeTheSave()
    {
        var c = Fixture1();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 0.6, ["defense_minister"] = 0.4 }) });
        var before = g.Save.Write();
        var turn = g.Save.Turns[0];
        ((AllocateBudget)turn[0]).Shares.Clear();                  // probe 1: the shares of a recorded order
        // probe 2: the lists themselves are read-only
        Assert.Throws<NotSupportedException>(() => ((IList<Command>)turn).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<IReadOnlyList<Command>>)g.Save.Turns).Clear());
        Assert.Equal(before, g.Save.Write());
        Assert.Single(g.Save.Turns);
        Assert.Equal(g.State.Hash(), GameSession.Load(c, g.Save.Write(), out _).State.Hash());
    }

    [Fact]
    public void AnUnknownOfficeIsRefusedAlikeInPlayAndInReplay()
    {
        var c = Fixture1();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new AllocateBudget(new Dictionary<string, double> { ["zzz"] = 0.5, ["aaa"] = 0.5 }) });
        var live = g.Last!.Rejected.Single().Reason;
        Assert.Equal("The budget names a ministry that does not exist in this game", live);   // no id in player text (final review M4)
        Assert.Equal(live, GameSession.Load(c, g.Save.Write(), out _).Last!.Rejected.Single().Reason);
    }

    // R-SAVE-001
    [Fact]
    public void AGddSaveNamesItsRulesetAndRoundTrips()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(HistoricalAI.Commands(c, g.State));
        var text = g.Save.Write();
        Assert.StartsWith("statecraft-save 2\n", text);
        Assert.Contains("\nruleset gdd-0.1\n", text);
        Assert.Equal("gdd-0.1", SaveGame.Parse(text).Ruleset);
        Assert.Equal(g.State.Hash(), GameSession.Load(c, text, out var warnings).State.Hash());
        Assert.Empty(warnings);
    }

    // R-SAVE-001
    [Fact]
    public void AVersionOneSaveReadsAsLegacyAndStillLoadsInTheLegacyGame()
    {
        var c = Fixture1();
        var g = Played(c);
        var v1 = g.Save.Write().Replace("statecraft-save 2\n", "statecraft-save 1\n").Replace("ruleset legacy\n", "");
        Assert.Equal("legacy", SaveGame.Parse(v1).Ruleset);
        Assert.Equal(g.State.Hash(), GameSession.Load(c, v1, out _).State.Hash());
        Assert.StartsWith("statecraft-save 2\n", SaveGame.Parse(v1).Write());   // written back as schema 2
        Assert.Contains("\nruleset legacy\n", SaveGame.Parse(v1).Write());
    }

    /// <summary>N4 (#382): a save from before the cutover (a schema 1 save, or one that names the legacy ruleset) is refused
    /// with a stable code the UI translates, and the sim's English reason does not itself tell the player to start a new
    /// game: the UI's message says that once.</summary>
    // R-SAVE-001
    [Fact]
    public void ALegacySaveIsRefusedOnceWithACode()
    {
        var g = Played(Fixture1());   // the legacy loop: a fixture since the cutover
        var real = ContentLoader.Load(RealContentTests.ContentRoot());
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(real, g.Save.Write(), out _));
        Assert.Equal(SaveException.OlderRules, ex.Code);
        Assert.Equal("This save was made with an earlier version of the game's rules.", ex.Message);
        Assert.DoesNotContain("new game", ex.Message);
        var v1 = g.Save.Write().Replace("statecraft-save 2\n", "statecraft-save 1\n").Replace("ruleset legacy\n", "");
        var ex1 = Assert.Throws<SaveException>(() => GameSession.Load(real, v1, out _));
        Assert.Equal((ex.Code, ex.Message), (ex1.Code, ex1.Message));
    }

    /// <summary>N2 (#382): a save naming a ruleset this build does not know was made by a newer version of the game, and is
    /// refused the way a newer save schema is: with the same code.</summary>
    // R-SAVE-002
    [Fact]
    public void AnUnknownRulesetSaysItIsNewer()
    {
        var real = ContentLoader.Load(RealContentTests.ContentRoot());
        var text = GameSession.New(real, 1, Mode.Historical).Save.Write().Replace("\nruleset gdd-0.1\n", "\nruleset gdd-0.2\n");
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse(text));
        Assert.Equal((SaveException.NewerVersion, 5), (ex.Code, ex.Line));
        Assert.Contains("This save was made with a newer version of the game", ex.Message);
        Assert.DoesNotContain("new game", ex.Message);
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => GameSession.Load(real, text, out _)).Code);
        var schema3 = text.Replace("statecraft-save 2\n", "statecraft-save 3\n");
        Assert.Equal(SaveException.NewerVersion, Assert.Throws<SaveException>(() => SaveGame.Parse(schema3)).Code);
    }

    // R-SAVE-001
    [Fact]
    public void ALegacyGameRefusesAGddSave()
    {
        var text = GameSession.New(ContentLoader.Load(RealContentTests.ContentRoot()), 1, Mode.Historical).Save.Write();
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(Fixture1(), text, out _));   // the legacy loop: a fixture
        Assert.Equal(SaveException.NewerVersion, ex.Code);
        Assert.Equal("This save was made with a newer version of the game's rules.", ex.Message);
    }

    /// <summary>A damaged save carries no refusal code: the UI shows its own reason.</summary>
    [Fact]
    public void AMalformedSaveHasNoRefusalCode() =>
        Assert.Null(Assert.Throws<SaveException>(() => SaveGame.Parse("statecraft-save x\ncontent a\nseed 1\nmode Historical\nend b\n")).Code);

    [Theory]
    [InlineData("ruleset chess", "a newer version of the game (ruleset 'chess')")]
    [InlineData("ruleset", "expected 'ruleset'")]
    [InlineData("turn", "expected 'ruleset'")]
    public void ABadRulesetLineIsRefused(string line, string message)
    {
        var text = "statecraft-save 2\ncontent " + Hex64('a') + "\nseed 1\nmode Historical\n" + line + "\nturn\nend " + Hex64('b') + "\n";
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse(text));
        Assert.Equal(5, ex.Line);
        Assert.Contains(message, ex.Message);
    }
}
