using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>Invariant checks with tolerance bands (#294), read from sim/Statecraft.Sim.Tests/golden/invariants.yaml and
/// evaluated on the golden runs. A hash golden says "something changed"; an invariant says "it is still plausible". The
/// invariants only add checks: a tuned number still moves the hash goldens, which are rebaselined as before
/// (docs/determinism.md). Each one carries a reason or source, like content does.</summary>
public sealed class Invariant
{
    public string Name { get; set; } = "";
    /// <summary>The golden run: "1950" or "1952" (GoldenRun.Until1950 / Until1952).</summary>
    public string Run { get; set; } = "";
    /// <summary>An indicator id, or treasury, debt, fx, population, stability.</summary>
    public string Value { get; set; } = "";
    /// <summary>"end" (the run's final state), a date yyyy-MM-dd (the first state on or after it), or "always" (every state).</summary>
    public string When { get; set; } = "";
    public double? Min { get; set; }
    public double? Max { get; set; }
    /// <summary>Why this band: a source, or the test or design note it comes from.</summary>
    public string Reason { get; set; } = "";
}

public static class Invariants
{
    public static readonly string[] Runs = { "1950", "1952" };
    static readonly string[] Named = { "treasury", "debt", "fx", "population", "stability" };

    public static string FilePath() =>
        Path.Combine(Directory.GetParent(RealContentTests.ContentRoot())!.FullName, "sim", "Statecraft.Sim.Tests", "golden", "invariants.yaml");

    sealed class FileModel { public List<Invariant> Invariants { get; set; } = new List<Invariant>(); }

    public static List<Invariant> Parse(string yaml)
    {
        var d = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        var list = d.Deserialize<FileModel>(yaml)?.Invariants ?? new List<Invariant>();
        foreach (var e in Problems(list)) throw new InvalidDataException(e);
        return list;
    }

    /// <summary>Format errors, one per line: a missing name, reason or band, an unknown run or when, a duplicate name.</summary>
    public static IEnumerable<string> Problems(IReadOnlyList<Invariant> all)
    {
        foreach (var i in all)
        {
            var who = $"invariant '{i.Name}'";
            if (string.IsNullOrWhiteSpace(i.Name)) yield return "an invariant has no name";
            if (string.IsNullOrWhiteSpace(i.Reason)) yield return $"{who} has no reason or source";
            if (i.Min == null && i.Max == null) yield return $"{who} has neither min nor max";
            if (i.Min != null && i.Max != null && i.Min > i.Max) yield return $"{who} has min above max";
            if (!Runs.Contains(i.Run)) yield return $"{who} names run '{i.Run}' (known: {string.Join(", ", Runs)})";
            if (string.IsNullOrWhiteSpace(i.Value)) yield return $"{who} has no value";
            if (i.When != "end" && i.When != "always" && !DateTime.TryParseExact(i.When, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                yield return $"{who} has when '{i.When}' (use end, always or yyyy-MM-dd)";
        }
        foreach (var g in all.GroupBy(i => i.Name).Where(g => g.Key != "" && g.Count() > 1)) yield return $"invariant '{g.Key}' is listed twice";
    }

    /// <summary>The value on a state; null when the id is not a state value or an indicator of this state.</summary>
    public static double? Read(GameState s, string what)
    {
        switch (what)
        {
            case "treasury": return s.Treasury;
            case "debt": return s.Debt;
            case "fx": return s.FxReserves;
            case "population": return s.TotalPopulation;
            case "stability": return s.CoalitionStability;
        }
        return s.Indicators.TryGetValue(what, out var v) ? v : (double?)null;
    }

    /// <summary>Failures of the invariants for one run, given its states in order. Each names the check, the value and the band.</summary>
    public static List<string> Evaluate(IReadOnlyList<Invariant> invariants, IReadOnlyList<GameState> states)
    {
        var failures = new List<string>();
        foreach (var i in invariants)
        {
            IEnumerable<GameState> at;
            if (i.When == "end") at = new[] { states[states.Count - 1] };
            else if (i.When == "always") at = states;
            else
            {
                var date = DateTime.ParseExact(i.When, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var s = states.FirstOrDefault(x => x.Date >= date);
                if (s == null) { failures.Add($"{Describe(i)}: the run ends before {i.When}"); continue; }
                at = new[] { s };
            }
            foreach (var s in at)
            {
                var v = Read(s, i.Value);
                if (v == null) { failures.Add($"{Describe(i)}: '{i.Value}' is neither a state value nor an indicator"); break; }
                // NaN compares false with every bound, so it would sit inside any band; a non-finite value always fails.
                if (double.IsNaN(v.Value) || double.IsInfinity(v.Value) || (i.Min != null && v < i.Min) || (i.Max != null && v > i.Max))
                {
                    failures.Add($"{Describe(i)}: {i.Value} = {v.Value.ToString("0.###", CultureInfo.InvariantCulture)} on {s.Date:yyyy-MM-dd} is outside {Band(i)}");
                    if (i.When == "always") break;   // the first state that leaves the band is enough
                }
            }
        }
        return failures;
    }

    static string Describe(Invariant i) => $"invariant '{i.Name}' (run {i.Run}, {i.When}; {i.Reason})";
    static string Band(Invariant i) =>
        $"[{(i.Min == null ? "-inf" : i.Min.Value.ToString("0.###", CultureInfo.InvariantCulture))}, {(i.Max == null ? "+inf" : i.Max.Value.ToString("0.###", CultureInfo.InvariantCulture))}]";

    /// <summary>The golden run's states, stepped exactly as <see cref="GoldenRun.Play"/> does.</summary>
    public static List<GameState> History(Content c, DateTime until)
    {
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var h = new List<GameState> { s };
        for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
        {
            s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
            h.Add(s);
        }
        return h;
    }

    public static DateTime Until(string run) => run == "1950" ? GoldenRun.Until1950 : GoldenRun.Until1952;
}
