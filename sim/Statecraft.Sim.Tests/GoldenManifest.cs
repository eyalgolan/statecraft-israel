using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>The pinned golden hashes, read from sim/Statecraft.Sim.Tests/golden/manifest.txt (#293). Tests compare against
/// it, so a deliberate change is one file in the PR diff. STATECRAFT_UPDATE_GOLDEN=1 rewrites it and the constants in
/// GoldenRun.cs, which Unity players compare against.</summary>
public static class GoldenManifest
{
    static string Root() => Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
    public static string Path_() => Path.Combine(Root(), "sim", "Statecraft.Sim.Tests", "golden", "manifest.txt");

    public static bool UpdateRequested => Environment.GetEnvironmentVariable("STATECRAFT_UPDATE_GOLDEN") == "1";

    // Test classes run in parallel, so an update run's writes (DeterminismTests, SliceSmokeTests) and reads take turns.
    static readonly object Gate = new object();

    public static Dictionary<string, string> Read()
    {
        lock (Gate) return File.ReadAllLines(Path_()).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#"))
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToDictionary(p => p.Length == 2 ? p[0] : throw new InvalidDataException("manifest line must be 'name hash'"), p => p[1]);
    }

    public static string Expected(string name) =>
        Read().TryGetValue(name, out var h) ? h : throw new KeyNotFoundException($"'{name}' is not in {Path_()}");

    /// <summary>Writes the new hash into the manifest and, for the two playthroughs, into the GoldenRun.cs constant.</summary>
    public static void Update(string name, string hash)
    {
        lock (Gate) UpdateLocked(name, hash);
    }

    static void UpdateLocked(string name, string hash)
    {
        var lines = File.ReadAllLines(Path_()).ToList();
        var i = lines.FindIndex(l => l.StartsWith(name + " "));
        if (i >= 0) lines[i] = name + " " + hash; else lines.Add(name + " " + hash);
        File.WriteAllText(Path_(), string.Join("\n", lines) + "\n");
        var constant = name switch { "hash1950" => "Hash1950", "hash1952" => "Hash1952", _ => null };
        if (constant == null) return;
        var cs = System.IO.Path.Combine(Root(), "sim", "Statecraft.Sim", "GoldenRun.cs");
        var text = File.ReadAllText(cs);
        File.WriteAllText(cs, RewriteConstant(text, constant, hash));
    }

    /// <summary>GoldenRun.cs source with the 64-hex value of <paramref name="constant"/> replaced.</summary>
    public static string RewriteConstant(string source, string constant, string hash) =>
        Regex.Replace(source, "(const string " + constant + " = \")[0-9a-f]{64}(\")", "${1}" + hash + "${2}");
}
