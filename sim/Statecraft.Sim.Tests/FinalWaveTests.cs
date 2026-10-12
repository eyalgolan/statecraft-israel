using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>M3 final review: the sensitive-event gate (I4), Cause determinism (M2), id-free refusals (M3, M4) and the one
/// call-up cost (M5).</summary>
public class FinalWaveTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    // ---- I4: sensitive events ----

    /// <summary>The events with a sensitive tag (spec 5.5): pinned, so a new one cannot miss the owner's review table.</summary>
    static readonly string[] SensitiveEvents =
        { "altalena", "bernadotte_assassination", "operation_hiram", "operation_horev", "operation_yoav", "reparations_claim", "ten_days" };

    [Fact]
    public void TheSensitiveEventsArePinned()
    {
        var actual = ReleaseGate.SensitiveEvents(Real).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.True(SensitiveEvents.SequenceEqual(actual),
            $"sensitive events changed: {string.Join(", ", actual)}. Add any new one to m3-release.md's sensitivity table and to this list.");
    }

    [Fact]
    public void TheOwnerTableListsEverySensitiveEvent()
    {
        var doc = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "..", "docs", "m3-release.md"));
        foreach (var id in ReleaseGate.SensitiveEvents(Real))
            Assert.True(doc.Contains($"| `{id}` |"), $"{id} is sensitive: add it to m3-release.md's sensitivity table");
    }

    /// <summary>The release gate: a sensitive event not yet sensitivity-reviewed blocks a shareable build.</summary>
    [Fact]
    public void AShareableBuildNeedsEverySensitiveEventReviewed()
    {
        Assert.Empty(ReleaseGate.SensitiveUnreviewed(Real));   // the owner signed off all seven on 2026-10-10 (docs/oss/DECISIONS.md D11)
        var reviewed = Fixture.Write(events: new Dictionary<string, string>
        {
            ["a.yaml"] = "id: a\ntitle: A\ntext: x\nfrom: 1948-06-01\ntags: [terror]\noptions:\n  - id: o\n    text: O\nstatus: sensitivity-reviewed\n",
            ["b.yaml"] = "id: b\ntitle: B\ntext: x\nfrom: 1948-06-01\ntags: [holocaust]\noptions:\n  - id: o\n    text: O\nstatus: fact-checked\n",
        });
        Assert.Equal(new[] { "b" }, ReleaseGate.SensitiveUnreviewed(ContentLoader.Load(reviewed)));
    }

    // ---- M2: Cause text is derived output, deterministic ----

    [Fact]
    public void AReloadedSaveReproducesTheLastTurnsCauses()
    {
        var g = GameSession.New(Real, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; t < 40; t++) g.Step(HistoricalAI.Commands(Real, g.State));
        var loaded = GameSession.Load(Real, g.Save.Write(), out _);
        Assert.Equal(g.Last!.Why.Select(w => (w.System, w.Target, w.Delta, w.Message)), loaded.Last!.Why.Select(w => (w.System, w.Target, w.Delta, w.Message)));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    public void TheGoldenRunsCausesAreTheSameInEveryCulture(string culture)
    {
        List<string> Run()
        {
            var all = new List<string>();
            var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
            for (int t = 0; t < 1000 && s.Date < GoldenRun.Until1952 && s.GameOver == null; t++)
            {
                var r = Simulation.Step(Real, s, HistoricalAI.Commands(Real, s));
                all.AddRange(r.Why.Select(w => w.Message + " | " + w.Delta.ToString("R", CultureInfo.InvariantCulture)));
                s = r.State;
            }
            return all;
        }
        var prev = CultureInfo.CurrentCulture;
        List<string> invariant, other;
        try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; invariant = Run(); CultureInfo.CurrentCulture = new CultureInfo(culture); other = Run(); }
        finally { CultureInfo.CurrentCulture = prev; }
        Assert.Equal(invariant, other);
    }

    // ---- M3, M4: refusals and Causes name no hex coordinate and quote no id ----

    [Fact]
    public void MapActionReasonsAndCausesNameNoCoordinates()
    {
        var c = Real;
        var s = GoldenRun.Play(c, new DateTime(1950, 6, 15));
        var sea = c.Hexes.First(h => !h.Land);
        Assert.Equal("This hex is not land", MapActions.Check(c, s, MapActions.Housing, sea.Q, sea.R));
        var far = c.Hexes.First(h => h.Land && c.SettlementHexes.All(x => MapActions.Distance(h.Q, h.R, x.Q, x.R) > c.Rules.MapActionMaxDistance));
        Assert.Equal($"This hex is more than {c.Rules.MapActionMaxDistance} hex{(c.Rules.MapActionMaxDistance == 1 ? "" : "es")} from a settlement",
            MapActions.Check(c, s, MapActions.Housing, far.Q, far.R));
        var ok = c.Hexes.First(h => MapActions.Check(c, s, MapActions.TransitCamp, h.Q, h.R) == null);
        var why = new List<Cause>();
        Assert.Null(MapActions.Apply(c, s, MapActions.TransitCamp, ok.Q, ok.R, why));
        Assert.Equal("This hex already has a transit camp", MapActions.Check(c, s, MapActions.TransitCamp, ok.Q, ok.R));
        var built = Assert.Single(why, w => w.System == "map");
        Assert.StartsWith("Built a transit camp: cost IL 1.5m", built.Message);
        Assert.DoesNotMatch(@"\(-?\d+, -?\d+\)", built.Message);
    }

    [Fact]
    public void RefusalsQuoteNoId()
    {
        var c = Real;
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Simulation.Step(c, s, new Command[]
        {
            new Appoint("no_such_office", "ben_gurion"), new Appoint("finance_minister", "no_such_person"),
            new SetPolicy("no_such_policy", 0.5), new ChooseEventOption("no_such_event", "x"), new MapAction("airport", 0, 0),
        });
        Assert.Equal(5, r.Rejected.Count);
        foreach (var x in r.Rejected)
            Assert.DoesNotMatch(@"'[^']*'", x.Reason);
        Assert.Contains(r.Rejected, x => x.Reason == "That office does not exist in this game");
        Assert.Contains(r.Rejected, x => x.Reason == "That person is not in this game");
    }

    // ---- M5: one call-up cost ----

    [Fact]
    public void TheCallUpCostComesFromOneRule()
    {
        var s = Simulation.NewGame(Real, 1, Mode.Historical);
        Assert.Equal(Real.Rules.MobilizationCostPerYear * 0.35, EconomySystem.MobilizationCost(Real, Real.Rules.Start, 0.35), 12);
        s.Mobilization = 0.35;
        Assert.Equal(EconomySystem.MobilizationCost(Real, s.Date, 0.35, 0.25), EconomySystem.Flows(Real, s, 0.25).Mobilization, 12);
    }
}
