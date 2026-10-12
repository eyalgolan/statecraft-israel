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
        var update = GoldenManifest.UpdateRequested;
        if (update) { File.WriteAllText(path, now); GoldenManifest.Update(name, state.Hash()); }
        var hashOk = state.Hash() == expectedHash;
        var saved = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;   // a CRLF checkout is not a change
        if (hashOk && saved == now) return;
        var sb = new StringBuilder();
        if (!hashOk) sb.AppendLine($"Golden '{name}' moved: expected {expectedHash}, got {state.Hash()}.");
        else sb.AppendLine($"Golden '{name}' hash is unchanged but its snapshot is out of date.");
        if (saved == null) sb.AppendLine($"No snapshot at {path}; run STATECRAFT_UPDATE_GOLDEN=1 dotnet test to create it.");
        else sb.Append(Diff(saved, now));
        if (update) sb.AppendLine("Snapshot, manifest and GoldenRun.cs rewritten (STATECRAFT_UPDATE_GOLDEN=1). Run the tests again: GoldenManifestTests names the docs that still show the old hash. Add a row to the rebaseline history and say why in the commit message.");
        else sb.AppendLine("If this is deliberate: STATECRAFT_UPDATE_GOLDEN=1 dotnet test, then follow docs/determinism.md.");
        Assert.Fail(sb.ToString());
    }

    /// <summary>Changed lines between two descriptions, grouped by section (see <see cref="StateDiff"/>).</summary>
    public static string Diff(string before, string after) => StateDiff.Lines(before, after);
}
