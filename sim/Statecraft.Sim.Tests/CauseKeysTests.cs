using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Statecraft.Sim;

/// <summary>B1: the economy, population, effect and security lines carry a key and their values (CauseKeys), so the UI can
/// word them in the player's language. The key is result data only: Message, Delta and the state are what they were, and
/// the values are the same whatever the machine's culture.</summary>
public class CauseKeysTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly Content Fix = ContentLoader.Load(Fixture.Write());
    static readonly string[] Keyed = { "population", "effects", "situations", "security" };

    [Fact]
    public void EveryKeyIsListedOnceAndNoneIsAUnitKey()
    {
        Assert.Equal(CauseKeys.All.Distinct().Count(), CauseKeys.All.Count);
        Assert.Empty(CauseKeys.All.Intersect(WhyKeys.All));
        Assert.Equal(CauseKeys.Purpose.All.Distinct().Count(), CauseKeys.Purpose.All.Count);
    }

    /// <summary>The reference run to the end: every line of the population, effects, situations and security systems has a
    /// known key.</summary>
    [Fact]
    public void TheReferenceRunKeysEveryLineOfTheSlice()
    {
        var lines = Run(Real, 1000);
        Assert.NotEmpty(lines);
        foreach (var w in lines.Where(w => Keyed.Contains(w.System)))
            Assert.True(CauseKeys.All.Contains(w.Key), $"[{w.System}] {w.Message}: no key");
        // Each key the run reaches, at least once (the rest are covered below on the fixture).
        foreach (var key in new[] { CauseKeys.MoneyFlows, CauseKeys.FxFlows, CauseKeys.Borrowed, CauseKeys.FxCrisisBegan, CauseKeys.Arrived,
                     CauseKeys.EffectChange, CauseKeys.SituationBegan, CauseKeys.RungChanged, CauseKeys.CallUpRaised, CauseKeys.CallUpLowered })
            Assert.Contains(lines, w => w.Key == key);
    }

    /// <summary>Other files also write "economy" lines, so EconomySystem's own are checked at the producer: its turn step and
    /// the civilian upkeep, on every state of the reference run, as they are, with the Treasury short, and with the currency
    /// crisis starting and easing. Every line they write has a key.</summary>
    [Fact]
    public void EveryLineEconomySystemWritesHasAKey()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var lines = new List<Cause>();
        for (int t = 0; t < 1000 && s.GameOver == null; t++)
        {
            foreach (var change in new Action<GameState>[] { _ => { }, x => x.Treasury = -50, x => x.FxReserves = -1e6,
                         x => { x.ActiveSituations.Add(EconomySystem.FxCrisis); x.FxReserves = 1e6; } })
            {
                var x = s.Clone(); change(x);
                EconomySystem.Apply(Real, x, 0.25, lines);
                EconomySystem.PayCivilianUpkeep(Real, x, Real.RulesAt(x.Date).Mvp!, 0.25, lines);
            }
            s = Simulation.Step(Real, s, GoldenRun.Orders(Real, s)).State;
        }
        foreach (var w in lines) Assert.True(CauseKeys.All.Contains(w.Key), $"[{w.System}] {w.Message}: no key");
        foreach (var key in new[] { CauseKeys.MoneyFlows, CauseKeys.FxFlows, CauseKeys.Borrowed, CauseKeys.FxCrisisBegan, CauseKeys.FxCrisisEased })
            Assert.Contains(lines, w => w.Key == key);
        Assert.All(lines.Where(w => w.Key == CauseKeys.Borrowed), w => Assert.Contains(w.Args[0], CauseKeys.Purpose.All));
    }

    /// <summary>The values parse back to exactly the numbers the English line printed, as invariant text.</summary>
    [Fact]
    public void TheValuesAreTheNumbersTheLineNames()
    {
        foreach (var w in Run(Real, 1000).Where(w => w.Key != ""))
            switch (w.Key)
            {
                case CauseKeys.MoneyFlows:
                    Assert.Equal($"{PlayerText.Change(Real, "treasury", w.Delta)} (tax {Money(w, 0)}, spending {Money(w, 1)}, reserve call-up {Money(w, 2)})", w.Message);
                    break;
                case CauseKeys.Arrived:
                    Assert.Equal(FormattableString.Invariant($"{w.Delta:N0} immigrants arrived ({PlayerText.Region(Real, w.Target)})"), w.Message);
                    break;
                case CauseKeys.EffectChange:
                    var names = w.Args.Select(id => Real.Effects.Single(e => e.Id == id).Name).ToList();
                    Assert.Equal(PlayerText.Change(Real, w.Target, w.Delta) + (names.Count > 0 ? $" ({string.Join(", ", names)})" : ""), w.Message);
                    break;
                case CauseKeys.RungChanged:
                    Assert.Equal($"Security level {w.Args[0]} to {w.Args[1]}", w.Message);
                    break;
                case CauseKeys.CallUpLowered:
                    Assert.Equal($"Reserves sent home: call-up lowered to {PlayerText.Percent(Number(w, 0))}, the minimum for security level {w.Args[1]}", w.Message);
                    break;
            }
    }

    /// <summary>Keys and values are the same under a comma-decimal culture: the UI reads them as invariant text.</summary>
    [Fact]
    public void TheValuesDoNotDependOnTheCulture()
    {
        string Lines() => string.Join("\n", Run(Real, 30).Select(w => w.Key + "|" + string.Join(";", w.Args)));
        var invariant = Lines();
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal(invariant, Lines());
        }
        finally { Thread.CurrentThread.CurrentCulture = culture; }
    }

    [Fact]
    public void TheFixtureLinesTheReferenceRunDoesNotReachAreKeyed()
    {
        var s = new GameState { Rung = 1 }; var why = new List<Cause>();
        Assert.Null(SecuritySystem.SetMobilization(Fix, s, 0.9, why));
        Assert.Equal(CauseKeys.CallUpSet, Assert.Single(why).Key);

        s = new GameState(); why = new List<Cause>();
        SecuritySystem.Init(Fix, s, why);
        for (int i = 0; i < 4; i++) SecuritySystem.Apply(Fix, s, TurnLength.Week, why);
        Assert.Equal(CauseKeys.Ceasefire, Assert.Single(why, w => w.Target == "ceasefire").Key);

        // Situation ended: the fixture's hunger turns off above 0.45.
        s = new GameState { Date = new DateTime(1948, 6, 1) };
        EffectsSystem.Init(Fix, s);
        s.ActiveSituations.Add("hunger"); s.Indicators["food_supply"] = 0.5;
        why = new List<Cause>();
        EffectsSystem.UpdateSituations(Fix, s, why);
        var ended = Assert.Single(why);
        Assert.Equal((CauseKeys.SituationEnded, "hunger"), (ended.Key, ended.Target));
        Assert.Equal(new[] { "food_supply", "0.5" }, ended.Args);
    }

    [Fact]
    public void TheCurrencyCrisisLinesAreKeyed()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"] + "fx_crisis_food_per_year: -0.4\n" }));
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        s.Population["veterans"] = 1000;
        s.FxReserves = -20;
        var why = new List<Cause>();
        EconomySystem.Apply(c, s, 0.25, why);
        Assert.Equal(CauseKeys.FxCrisisBegan, Assert.Single(why, w => w.Target == EconomySystem.FxCrisis).Key);
        Assert.Equal(CauseKeys.FxCrisisFood, Assert.Single(why, w => w.Target == "food_supply").Key);

        s.FxReserves = 1000; why.Clear();
        EconomySystem.Apply(c, s, 0.25, why);
        Assert.Equal(CauseKeys.FxCrisisEased, Assert.Single(why, w => w.Target == EconomySystem.FxCrisis).Key);
    }

    [Fact]
    public void CivilianUpkeepAndItsBorrowingAreKeyed()
    {
        var s = new GameState { Treasury = 0, Debt = 0 }; var why = new List<Cause>();
        EconomySystem.PayOrBorrow(Fix, s, 3, "for civilian upkeep", why, CauseKeys.Purpose.CivilianUpkeep);
        var line = Assert.Single(why);
        Assert.Equal((CauseKeys.Borrowed, "Borrowed IL 3.0m for civilian upkeep"), (line.Key, line.Message));
        Assert.Equal(new[] { CauseKeys.Purpose.CivilianUpkeep }, line.Args);

        why.Clear();
        EconomySystem.PayOrBorrow(Fix, s, 3, "to build a housing project", why, CauseKeys.Purpose.Project, "housing");
        line = Assert.Single(why);
        Assert.Equal(CauseKeys.Borrowed, line.Key);
        Assert.Equal(new[] { CauseKeys.Purpose.Project, "housing" }, line.Args);
    }

    static string Money(Cause w, int i) => PlayerText.Money(Real, "treasury", Number(w, i));
    static double Number(Cause w, int i) => double.Parse(w.Args[i], NumberStyles.Float, CultureInfo.InvariantCulture);

    static List<Cause> Run(Content c, int turns)
    {
        var lines = new List<Cause>();
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; t < turns && s.GameOver == null; t++)
        {
            var r = Simulation.Step(c, s, GoldenRun.Orders(c, s));
            lines.AddRange(r.Why);
            s = r.State;
        }
        return lines;
    }
}
