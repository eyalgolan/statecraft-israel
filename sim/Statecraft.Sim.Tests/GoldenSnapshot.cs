using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Statecraft.Sim;

/// <summary>Makes a golden-hash failure readable (#256). Each golden run's final state is committed as text
/// (sim/Statecraft.Sim.Tests/golden/&lt;name&gt;.txt, written by <see cref="GameState.Describe"/>). When the hash differs,
/// the test fails with the lines that changed against that snapshot, grouped by section, instead of two hashes.
/// To accept a deliberate change, run <c>STATECRAFT_UPDATE_GOLDEN=1 dotnet test</c>: it rewrites the snapshot, so the
/// snapshot's diff in the PR shows reviewers what moved. The hash constants are still updated by hand (docs/determinism.md).</summary>
public static class GoldenSnapshot
{
    static string Path_(string name) =>
        Path.Combine(Directory.GetParent(RealContentTests.ContentRoot())!.FullName, "sim", "Statecraft.Sim.Tests", "golden", name + ".txt");

    /// <summary>Asserts the state's hash equals <paramref name="expectedHash"/>; on a mismatch fails with the diff.
    /// Also asserts the committed snapshot is the state's current description, so it cannot go stale.</summary>
    public static void Check(string name, GameState state, string expectedHash)
    {
        var now = state.Describe();
        var path = Path_(name);
        var update = Environment.GetEnvironmentVariable("STATECRAFT_UPDATE_GOLDEN") == "1";
        if (update) File.WriteAllText(path, now);
        var hashOk = state.Hash() == expectedHash;
        var saved = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;   // a CRLF checkout is not a change
        if (hashOk && saved == now) return;
        var sb = new StringBuilder();
        if (!hashOk) sb.AppendLine($"Golden '{name}' moved: expected {expectedHash}, got {state.Hash()}.");
        else sb.AppendLine($"Golden '{name}' hash is unchanged but its snapshot is out of date.");
        if (saved == null) sb.AppendLine($"No snapshot at {path}; run STATECRAFT_UPDATE_GOLDEN=1 dotnet test to create it.");
        else sb.Append(Diff(saved, now));
        if (update) sb.AppendLine("Snapshot rewritten (STATECRAFT_UPDATE_GOLDEN=1). Update the hash constants in GoldenRun.cs, DeterminismTests.cs and docs/determinism.md, and say why in the commit message.");
        else sb.AppendLine("If this is deliberate: STATECRAFT_UPDATE_GOLDEN=1 dotnet test, then update the three hash constants (docs/determinism.md).");
        Assert.Fail(sb.ToString());
    }

    /// <summary>Changed lines between two descriptions, grouped by section (the label before '{' or '[' or '=').</summary>
    public static string Diff(string before, string after)
    {
        List<(string section, string line)> Parse(string text)
        {
            var res = new List<(string, string)>();
            string section = "header";
            foreach (var raw in text.Split('\n'))
            {
                if (raw.Length == 0) continue;
                if (!raw.StartsWith("  "))
                {
                    var end = raw.IndexOfAny(new[] { '{', '[', '=' });
                    section = end > 0 && raw.IndexOf(';') is var semi && (semi < 0 || end < semi) ? raw.Substring(0, end) : "header";
                }
                res.Add((section, raw));
            }
            return res;
        }
        var a = Parse(before); var b = Parse(after);
        var aSet = new HashSet<(string, string)>(a); var bSet = new HashSet<(string, string)>(b);
        var sb = new StringBuilder();
        foreach (var g in a.Concat(b).Select(x => x.section).Distinct())
        {
            var removed = a.Where(x => x.section == g && !bSet.Contains(x)).Select(x => "  - " + x.line.TrimStart()).ToList();
            var added = b.Where(x => x.section == g && !aSet.Contains(x)).Select(x => "  + " + x.line.TrimStart()).ToList();
            if (removed.Count + added.Count == 0) continue;
            sb.AppendLine($"[{g}]");
            foreach (var l in removed.Take(40)) sb.AppendLine(l);
            foreach (var l in added.Take(40)) sb.AppendLine(l);
        }
        return sb.Length == 0 ? "(the descriptions are identical)\n" : sb.ToString();
    }
}
