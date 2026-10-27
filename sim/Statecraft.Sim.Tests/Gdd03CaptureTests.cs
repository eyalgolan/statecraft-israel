using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Sim;

public class Gdd03CaptureTests
{
    static readonly string Catalogue = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));

    static Content Load(string identity = SaveGame.Gdd03Ruleset, bool shared = false, bool fled = false) => CaptureTests.Load(files =>
    {
        files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + identity);
        files[Gdd03BalanceCatalogue.FileName] = Catalogue;
        if (shared)
            foreach (var key in new[] { "settlement_targets.yaml", "map/settlement_hexes.yaml" })
                files[key] = files[key].Replace("q: 4, r: 0", "q: 1, r: 0");
    }, fled ? new[] { "town_a" } : Array.Empty<string>(), CaptureTests.AllTargets);

    static GameState Held(Content c) => CombatTests.With(c, CaptureTests.Soldier(1, 1, 0));
    static GameState Acquired(Content c)
    {
        var result = Simulation.Preview(c, Held(c), new Command[] { new CaptureLocality("town_a") });
        Assert.Empty(result.Rejected);
        return result.State;
    }

    // R-CTRL-023: red before metadata fix: the new producer emits an empty key and no fled argument.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcquisitionEmitsTheExistingCaptureKeyAndResidentSnapshot(bool fled)
    {
        var c = Load(fled: fled);
        var result = Simulation.Preview(c, Held(c), new Command[] { new CaptureLocality("town_a") });
        Assert.Empty(result.Rejected);
        var captured = Assert.Single(result.Why);
        Assert.Equal(CauseKeys.PlaceCaptured, captured.Key);
        Assert.Equal(new[] { fled ? "1" : "0" }, captured.Args);
        Assert.Equal("town_a", captured.Target);
        Assert.Equal("TOWN A is captured: Israeli units hold it and no enemy unit is there. "
            + (fled ? "Its residents fled. " : "") + "It awaits a decision", captured.Message);
        Assert.Equal(fled, result.State.Settlements["town_a"].Fled);
        result.State.Settlements.Clear();
        Assert.Equal(new[] { fled ? "1" : "0" }, captured.Args);
    }

    // R-CTRL-023: red before pricing fix: recognized but unpriced capture indexes a missing cost.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldModesPriceCaptureAtZeroAndRefuseItBeforeSpending(bool gdd)
    {
        var c = gdd ? Load(SaveGame.GddRuleset) : ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Attention = 0;
        var cmd = new CaptureLocality("town_a");
        Assert.True(AttentionSystem.IsCommandKind(cmd.Kind));
        Assert.False(AttentionSystem.IsPricedKind(cmd.Kind));
        Assert.DoesNotContain(cmd.Kind, AttentionSystem.CommandKinds);
        Assert.False(c.RulesAt(s.Date).AttentionCost.ContainsKey(cmd.Kind));
        var before = s.Describe();
        Assert.Equal(0.0, AttentionSystem.Price(c, s, cmd));
        Assert.Equal(0.0, AttentionSystem.Left(c, s, new Command[] { cmd }));
        Assert.Equal(before, s.Describe());
        var preview = Simulation.Preview(c, s, new Command[] { cmd });
        Assert.Equal("That order is not available in this game", Assert.Single(preview.Rejected).Reason);
        Assert.Equal(before, preview.State.Describe());
        var game = GameSession.New(c, 1, Mode.Historical);
        game.State.Attention = 0;
        var save = game.Save.Write();
        Assert.Equal("That order is not available in this game", game.Commit(cmd));
        Assert.Equal(0.0, game.State.Attention);
        Assert.Empty(game.Committed);
        Assert.Equal(save, game.Save.Write());
    }

    // R-CTRL-023: red before validation fix: recognition wrongly accepts a new old-mode cost key.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldModesRejectAnAttentionCostForCapture(bool gdd)
    {
        var files = gdd ? Fixture.Gdd() : new Dictionary<string, string>();
        var path = Fixture.Write(files);
        var rules = Path.Combine(path, "rules.yaml");
        var text = File.ReadAllText(rules);
        Assert.Contains("attention_cost: {", text);
        File.WriteAllText(rules, text.Replace("attention_cost: {", "attention_cost: { capture_locality: 1,"));
        var error = Assert.Throws<ContentException>(() => ContentLoader.Load(path));
        Assert.Equal("attention_cost.capture_locality", error.Field);
    }

    // R-CTRL-023: control: zero-for-unpriced must not enable Attention APIs in gdd-0.3.
    [Fact]
    public void AcquisitionKeepsTheNoAttentionContract()
    {
        var c = Load(); var s = Held(c); var cmd = new CaptureLocality("town_a");
        s.Attention = double.NaN;
        Assert.Throws<InvalidOperationException>(() => AttentionSystem.Price(c, s, cmd));
        Assert.Throws<InvalidOperationException>(() => AttentionSystem.Left(c, s, new Command[] { cmd }));
        var result = Simulation.Preview(c, s, new Command[] { cmd });
        Assert.Empty(result.Rejected);
        Assert.True(double.IsNaN(result.State.Attention));
    }

    // R-CTRL-023, R-SAVE-003: red without the distinct token or completed-batch command ordering.
    [Fact]
    public void CompletedAcquisitionThenOccupationLoadsAndReplaysByteIdentically()
    {
        var c = Load();
        c.StartingForces.Add(new StartingForce { Row = "fixture", Type = "soldier", Side = "israel", Strength = 2, Q = 1, R = 0 });
        var game = GameSession.New(c, 1, Mode.Historical);
        Assert.Null(game.Commit(new CaptureLocality("town_a")));
        var result = game.EndTurn(new Command[] { new ResolveCapture("town_a", SettlementControl.Occupy) });
        Assert.Empty(result.Rejected);
        Assert.Empty(game.Committed);
        Assert.Equal(SettlementControl.Occupied, game.State.Settlements["town_a"].State);
        var text = game.Save.Write();
        Assert.Empty(game.Save.Open);
        var turn = Assert.Single(game.Save.Turns);
        Assert.IsType<CaptureLocality>(turn[0]);
        Assert.IsType<ResolveCapture>(turn[1]);
        Assert.Equal(2, turn.Count);
        Assert.Contains("capture_locality town_a\ncapture town_a occupy\n", text);
        var loaded = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(game.State.Describe(), loaded.State.Describe());
        Assert.Equal(game.State.Hash(), loaded.State.Hash());
        Assert.Equal(game.Now.State.Describe(), loaded.Now.State.Describe());
        Assert.Equal(text, loaded.Save.Write());
        var replay = GameSession.New(c, 1, Mode.Historical);
        foreach (var batch in SaveGame.Parse(text, c.Ruleset).Turns) Assert.Empty(replay.Step(batch).Rejected);
        Assert.Equal(game.State.Describe(), replay.State.Describe());
        Assert.Equal(text, replay.Save.Write());
    }

    // R-SAVE-003: compatibility control, expected green on main; no new command appears in these saves.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldModeSavesWithoutAcquisitionLoadAndReplayUnchanged(bool gdd)
    {
        var c = gdd ? CaptureTests.Load(CaptureTests.AllTargets) : ContentLoader.Load(Fixture.Write());
        if (gdd)
            c.StartingForces.Add(new StartingForce { Row = "fixture", Type = "soldier", Side = "israel", Strength = 2, Q = 0, R = 1 });
        var game = GameSession.New(c, 1, Mode.Historical);
        var commands = gdd
            ? new Command[] { new Move(0, 1, 0), new ResolveCapture("town_a", SettlementControl.Occupy) }
            : new Command[] { new SetPolicy("rationing", 0.5) };
        Assert.Empty(game.Step(commands).Rejected);
        Assert.Empty(game.Step(Array.Empty<Command>()).Rejected);
        var text = game.Save.Write();
        Assert.DoesNotContain("capture_locality", text);
        var loaded = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(game.State.Describe(), loaded.State.Describe());
        Assert.Equal(game.State.Hash(), loaded.State.Hash());
        Assert.Equal(text, loaded.Save.Write());
        var replay = GameSession.New(c, 1, Mode.Historical);
        foreach (var batch in SaveGame.Parse(text, c.Ruleset).Turns) Assert.Empty(replay.Step(batch).Rejected);
        Assert.Equal(game.State.Describe(), replay.State.Describe());
        Assert.Equal(text, replay.Save.Write());
    }

    // R-CTRL-004, R-CTRL-023: red on main: moving/continuing a route acquires automatically.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovementAndResolverNeverBypassConfirmation(bool route)
    {
        var c = Load();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 2,
            order: route ? Movement.GoTo : "", orderTarget: route ? "1,0" : ""));
        var commands = route ? Array.Empty<Command>() : new Command[] { new Move(1, 1, 0) };
        var preview = Simulation.Preview(c, s, commands);
        Assert.Empty(preview.Rejected);
        Assert.Equal((1, 0), (preview.State.Units[1].Q, preview.State.Units[1].R));
        Assert.Empty(preview.State.Settlements);
        Assert.Empty(Simulation.Step(c, s, commands).State.Settlements);
    }

    // R-CTRL-005, R-CTRL-008, R-CTRL-023: red on main: acquisition command/preview are absent.
    [Fact]
    public void PreviewIsPureAndConfirmationChangesOnlyTheSelectedSharedHexLocality()
    {
        var c = Load(shared: true); var s = Held(c); var before = s.Describe();
        var p = SettlementControl.PreviewCapture(c, s, "town_a");
        Assert.Null(p.Refused);
        Assert.Null(p.Before);
        Assert.Equal(SettlementControl.AwaitingChoice, p.After!.State);
        Assert.Equal(new[] { SettlementControl.Raid, SettlementControl.Occupy, SettlementControl.Deport }, p.Options.Select(x => x.Outcome));
        Assert.Equal(CaptureRefusal.NotInPlay, p.Options.Last().Refused!.Id);
        Assert.Null(p.Options.Last().Preview);
        Assert.Equal(before, s.Describe());
        var after = Acquired(c);
        Assert.Equal("town_a", Assert.Single(after.Settlements).Key);
        Assert.Equal(p.After!.State, after.Settlements["town_a"].State);
        Assert.Equal(s.RngState, after.RngState);
        after.Settlements.Clear();
        Assert.Equal(before, after.Describe());
    }

    // R-CTRL-001, R-CTRL-023: red on main: no selected acquisition validation exists.
    [Theory]
    [InlineData("unknown")]
    [InlineData("town_e")]
    [InlineData("town_d")]
    [InlineData("town_c")]
    public void IneligibleTargetsAreRefusedWithoutMutation(string id)
    {
        var c = Load(); var s = Held(c); var before = s.Describe();
        var p = SettlementControl.PreviewCapture(c, s, id);
        Assert.NotNull(p.Refused); Assert.Null(p.After); Assert.Empty(p.Options);
        var result = Simulation.Preview(c, s, new Command[] { new CaptureLocality(id) });
        Assert.Equal(p.Refused!.Text, Assert.Single(result.Rejected).Reason);
        Assert.Equal(before, result.State.Describe());
    }

    // R-CTRL-004, R-CTRL-023: red on main: no command revalidation of a displayed acquisition preview.
    [Fact]
    public void StalePreviewDuplicateAndUnreviewedTargetCannotCapture()
    {
        var c = Load(); var s = Held(c);
        Assert.Null(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
        s.Units[2] = CaptureTests.Enemy(2, 1, 0);
        Assert.NotNull(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
        Assert.Single(Simulation.Preview(c, s, new Command[] { new CaptureLocality("town_a") }).Rejected);
        s.Units.Remove(2);
        var acquired = Acquired(c); var before = acquired.Describe();
        var duplicate = Simulation.Preview(c, acquired, new Command[] { new CaptureLocality("town_a") });
        Assert.Single(duplicate.Rejected); Assert.Equal(before, duplicate.State.Describe());
        c.SettlementTargets.Single(x => x.Id == "town_a").Status = "draft";
        Assert.NotNull(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
    }

    // R-CTRL-004, R-CTRL-020, R-CTRL-023: red on main: explicit ground-holder guard is absent.
    [Fact]
    public void WorkersArtilleryAndDepopulatedPlacesCannotBeAcquired()
    {
        var c = Load();
        var s = CombatTests.With(c, new Unit(1, "halutz", 1, 0));
        Assert.NotNull(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
        c.UnitTypes.Add(new UnitType { Id = "gun", Side = "israel", Class = Combat.ArtilleryClass });
        s.Units[1] = new Unit(1, "gun", 1, 0, side: "israel");
        Assert.NotNull(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
        s = Held(c);
        s.Settlements["town_a"] = new SettlementControl(depopulated: true);
        Assert.NotNull(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
    }

    // R-CTRL-007, R-CTRL-008, R-CTRL-012: red on main: catalogue turns are ignored.
    [Fact]
    public void CatalogueTimerAndExistingUpkeepFeedThePreviewAndResolution()
    {
        var c = Load(); var s = Acquired(c);
        var expected = decimal.ToInt32(c.Gdd03Balance!.Values["occupation.turns"].Values.Single());
        Assert.Equal(expected, SettlementControl.PreviewCapture(c, Held(c), "town_a").OccupationTurns);
        Assert.Equal(c.RulesAt(s.Date).Mvp!.OccupationUpkeepPerTurn!.Value,
            SettlementControl.PreviewCapture(c, Held(c), "town_a").UpkeepPerTurn);
        var resolved = s.Clone(); var why = new List<Cause>();
        Assert.Null(SettlementControl.Resolve(c, resolved, "town_a", SettlementControl.Occupy, why));
        Assert.Equal(expected, resolved.Settlements["town_a"].Turns);
        var net = resolved.Treasury - resolved.Debt;
        SettlementControl.Tick(c, resolved, c.RulesAt(s.Date).Mvp!, why);
        Assert.Equal(expected - 1, resolved.Settlements["town_a"].Turns);
        Assert.Equal(c.RulesAt(s.Date).Mvp!.OccupationUpkeepPerTurn!.Value,
            net - (resolved.Treasury - resolved.Debt), 8);
        for (var i = 1; i < expected; i++) SettlementControl.Tick(c, resolved, c.RulesAt(s.Date).Mvp!, why);
        Assert.Equal(SettlementControl.AwaitingEnd, resolved.Settlements["town_a"].State);
        c.Gdd03Balance!.Values["occupation.turns"].Values[0] = expected + 2;
        Assert.Null(SettlementControl.Resolve(c, s, "town_a", SettlementControl.Occupy, why));
        Assert.Equal(expected + 2, s.Settlements["town_a"].Turns);
    }

    // R-CTRL-007, R-CTRL-018, R-CTRL-023: red on main: cannot commit an acquisition.
    [Fact]
    public void ConfirmationIsInstantButExistingOutcomeRemainsDeferredAndGated()
    {
        var c = Load(); var s = Held(c);
        Assert.True(Simulation.IsInstant(new CaptureLocality("town_a")));
        Assert.False(Simulation.IsInstant(new ResolveCapture("town_a", SettlementControl.Occupy)));
        var result = Simulation.Step(c, s, new Command[] { new CaptureLocality("town_a"), new ResolveCapture("town_a", SettlementControl.Occupy) });
        Assert.Empty(result.Rejected);
        Assert.Equal(result.State.Date, result.State.Settlements["town_a"].Since);
        var acquired = Acquired(c); var before = acquired.Describe();
        Assert.NotNull(SettlementControl.Resolve(c, acquired, "town_a", SettlementControl.Deport, new List<Cause>()));
        Assert.Equal(before, acquired.Describe());
        Assert.NotNull(SettlementControl.Resolve(c, acquired, "town_a", "puppet", new List<Cause>()));
        Assert.Equal(before, acquired.Describe());
        acquired.Units.Clear(); acquired.Units[2] = CaptureTests.Enemy(2, 1, 0);
        SettlementControl.Capture(c, acquired, acquired.Date.AddDays(7), new List<Cause>());
        Assert.Empty(acquired.Settlements);
    }

    // R-CTRL-023, R-SAVE-003: red on main: distinct acquisition save/scenario token is absent.
    [Fact]
    public void AcquisitionRoundTripsInOpenTurnWithoutReinterpretingCaptureOutcome()
    {
        var c = Load();
        c.StartingForces.Add(new StartingForce { Row = "fixture", Type = "soldier", Side = "israel", Strength = 2, Q = 1, R = 0 });
        var game = GameSession.New(c, 1, Mode.Historical);
        Assert.Null(game.Commit(new CaptureLocality("town_a")));
        var reloaded = GameSession.Load(c, game.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(game.Now.State.Describe(), reloaded.Now.State.Describe());
        Assert.Equal(game.Save.Write(), reloaded.Save.Write());
        var saved = new SaveGame(c.Hash, 1, Mode.Historical, c.Ruleset, game.State.Hash());
        saved.SetOpen(new Command[] { new CaptureLocality("town_a") });
        var text = saved.Write();
        Assert.Contains("open\ncapture_locality town_a\n", text);
        var parsed = SaveGame.Parse(text, c.Ruleset);
        Assert.Equal("town_a", Assert.IsType<CaptureLocality>(Assert.Single(parsed.Open)).Settlement);
        Assert.Equal(text, parsed.Write());
        saved.Record(new Command[] { new ResolveCapture("town_a", SettlementControl.Occupy) }, game.State);
        Assert.Contains("capture town_a occupy\n", saved.Write());
        var scenario = Scenario.Parse("name: capture-fixture\nuntil: 1948-06-15\nsteps:\n  - on: 1948-05-15\n    commands:\n      - { kind: capture_locality, settlement: town_a }\n");
        Assert.IsType<CaptureLocality>(Scenario.Build(Assert.Single(Assert.Single(scenario.Steps).Commands)));
        Assert.Equal(game.Save.Write(), SaveGame.Parse(game.Save.Write(), c.Ruleset).Write());
    }

    // R-CTRL-012: red on main: malformed occupation.turns is never consumed or checked.
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public void InvalidCatalogueTimerNeverFallsBack(string number)
    {
        var c = Load();
        c.Gdd03Balance!.Values["occupation.turns"].Values[0] = decimal.Parse(number, CultureInfo.InvariantCulture);
        var error = Assert.Throws<ContentException>(() => SettlementControl.PreviewCapture(c, Held(c), "town_a"));
        Assert.Equal("values.occupation.turns", error.Field);
    }

    // R-CTRL-012: red on main: no requirement for a non-Deferred occupation timer.
    [Fact]
    public void DeferredOrMissingTimerIsRefusedAtLoad()
    {
        foreach (var missing in new[] { false, true })
        {
            var error = Assert.Throws<ContentException>(() => CaptureTests.Load(files =>
            {
                files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
                var start = Catalogue.IndexOf("  \"occupation.turns\":", StringComparison.Ordinal);
                var end = Catalogue.IndexOf("  \"annex.tax_town\":", start, StringComparison.Ordinal);
                var row = Catalogue.Substring(start, end - start);
                files[Gdd03BalanceCatalogue.FileName] = Catalogue.Replace(row, missing ? "" :
                    "  \"occupation.turns\": { basis: Deferred, expression: pending, note: fixture }\n");
            }, CaptureTests.AllTargets));
            Assert.Equal("values.occupation.turns", error.Field);
        }
    }

    // R-CTRL-004, R-CTRL-012, R-CTRL-023: regression control, already green on main.
    [Fact]
    public void Gdd01StillAutoCapturesAndIgnoresTheNewTimer()
    {
        var c = Load(SaveGame.GddRuleset); var s = Held(c);
        var acquired = Simulation.Preview(c, s, Array.Empty<Command>()).State;
        Assert.Equal(SettlementControl.AwaitingChoice, acquired.Settlements["town_a"].State);
        Assert.NotNull(SettlementControl.PreviewCapture(c, s, "town_a").Refused);
        c.Gdd03Balance!.Values["occupation.turns"].Values[0] = 9;
        Assert.Null(SettlementControl.Resolve(c, acquired, "town_a", SettlementControl.Occupy, new List<Cause>()));
        Assert.Equal(c.RulesAt(s.Date).Mvp!.OccupationTurns!.Value, acquired.Settlements["town_a"].Turns);
    }

    // R-CTRL-023: same-run old-mode control; catalogue presence changes no state or save bytes.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OldModesMatchInTheSameRunWithOrWithoutTheCatalogue(bool gdd)
    {
        var files = new Dictionary<string, string>();
        var bare = gdd ? CaptureTests.Load(CaptureTests.AllTargets) : ContentLoader.Load(Fixture.Write(files));
        files[Gdd03BalanceCatalogue.FileName] = Catalogue;
        var extra = gdd ? Load(SaveGame.GddRuleset) : ContentLoader.Load(Fixture.Write(files));
        if (gdd)
            foreach (var c in new[] { bare, extra })
                c.StartingForces.Add(new StartingForce { Row = "fixture", Type = "soldier", Side = "israel", Strength = 2, Q = 0, R = 1 });
        var a = GameSession.New(bare, 1, Mode.Historical);
        var b = GameSession.New(extra, 1, Mode.Historical);
        var before = a.Now.State.Describe();
        Assert.NotNull(a.Commit(new CaptureLocality("town_a")));
        Assert.Equal(before, a.Now.State.Describe());
        Assert.Empty(a.Committed);
        for (var i = 0; i < 2; i++)
        {
            var commands = gdd && i == 0
                ? new Command[] { new Move(0, 1, 0), new ResolveCapture("town_a", SettlementControl.Occupy) }
                : Array.Empty<Command>();
            a.Step(commands); b.Step(commands);
            Assert.Equal(a.State.Describe(), b.State.Describe()); Assert.Equal(a.State.Hash(), b.State.Hash());
        }
        // Content hashes intentionally include authored catalogue bytes; normalize only the envelope's content line.
        Assert.Equal(a.Save.Write().Replace(bare.Hash, "CONTENT"), b.Save.Write().Replace(extra.Hash, "CONTENT"));
    }

    // R-CTRL-004, R-CTRL-012: manifest authority stays in the existing tests; no new pinned hash.
    [Fact]
    public void ShippingGoldenMatchesTheUnchangedManifest()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = GoldenRun.Play(c, GoldenRun.Until1952);
        Assert.Equal(GoldenManifest.Expected("hash1952"), s.Hash());
    }
}
