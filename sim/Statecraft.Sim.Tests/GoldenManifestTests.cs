using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>golden/manifest.txt is the one place a golden hash is pinned (#293). These tests keep every other place that
/// shows a hash in step with it, and name the place that is out of date.</summary>
public class GoldenManifestTests
{
    static string Repo => Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
    static string Text(params string[] path) => File.ReadAllText(Path.Combine(new[] { Repo }.Concat(path).ToArray()));

    [Fact]
    public void TheManifestHoldsExactlyTheKnownGoldens()
    {
        var m = GoldenManifest.Read();
        var scenarios = System.IO.Directory.GetFiles(ScenarioTests.Dir(), "*.yaml").Select(f => "scenario-" + System.IO.Path.GetFileNameWithoutExtension(f));
        var known = new[] { "hash1950", "hash1952", "smoke5", "mvp-end" }.Concat(scenarios).OrderBy(k => k, StringComparer.Ordinal).ToArray();   // one line per scenario file (#296)
        Assert.Equal(known, m.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.All(m.Values, h => Assert.Matches("^[0-9a-f]{64}$", h));
    }

    [Fact]
    public void EveryScenarioSnapshotHasItsScenarioFile()
    {
        var names = System.IO.Directory.GetFiles(ScenarioTests.Dir(), "*.yaml").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToHashSet();
        var snapshots = System.IO.Directory.GetFiles(System.IO.Path.Combine(Repo, "sim", "Statecraft.Sim.Tests", "golden"), "scenario-*.txt");
        Assert.NotEmpty(snapshots);
        foreach (var f in snapshots)
        {
            var n = System.IO.Path.GetFileNameWithoutExtension(f).Substring("scenario-".Length);
            Assert.True(names.Contains(n), $"golden/{System.IO.Path.GetFileName(f)} has no scenarios/{n}.yaml: delete the snapshot with its scenario");
        }
    }

    [Fact]
    public void TheLibraryConstantsUnityComparesAgainstEqualTheManifest()
    {
        Assert.Equal(GoldenRun.Hash1950, GoldenManifest.Expected("hash1950"));
        Assert.Equal(GoldenRun.Hash1952, GoldenManifest.Expected("hash1952"));
    }

    [Theory]
    [InlineData("hash1950")]
    [InlineData("hash1952")]
    [InlineData("smoke5")]
    public void TheDocsAndTheWebCheckShowTheManifestHash(string name)
    {
        var hash = GoldenManifest.Expected(name);
        Assert.True(Text("docs", "determinism.md").Contains(hash), $"docs/determinism.md does not show the current {name} hash {hash}");
        Assert.True(Text("docs", "RELEASING.md").Contains(hash), $"docs/RELEASING.md does not show the current {name} hash {hash}");
        if (name != "smoke5")
            Assert.True(Text("tools", "web_check.py").Contains(hash), $"tools/web_check.py does not show the current {name} hash {hash}");
    }

    [Fact]
    public void TheArtCheckpointReadsTheManifestInsteadOfPinningAHash()
    {
        var script = Text("tools", "art_checkpoint.sh");
        Assert.Contains("sim/Statecraft.Sim.Tests/golden/manifest.txt", script);
        Assert.DoesNotMatch("[0-9a-f]{64}", script);
    }

    [Fact]
    public void RewritingAConstantChangesOnlyThatConstant()
    {
        var src = Text("sim", "Statecraft.Sim", "GoldenRun.cs");
        var new1950 = new string('a', 64);
        var after = GoldenManifest.RewriteConstant(src, "Hash1950", new1950);
        Assert.Contains($"Hash1950 = \"{new1950}\"", after);
        Assert.Contains($"Hash1952 = \"{GoldenRun.Hash1952}\"", after);
        Assert.NotEqual(src, after);
    }

    [Fact]
    public void EveryWebCheckExpectInReleasingShowsTheManifestHash()
    {
        // RELEASING.md runs web_check on two builds (the release zip and the Pages demo): both take the goldens from the manifest loop and pin smoke5
        var doc = Text("docs", "RELEASING.md");
        var webChecks = System.Text.RegularExpressions.Regex.Matches(doc, @"web_check\.py unity/Builds/\S+ --query ""golden&smoke=5""[^\n]*");
        Assert.True(webChecks.Count >= 2, $"expected the golden web_check of both web builds in docs/RELEASING.md, found {webChecks.Count}");
        foreach (System.Text.RegularExpressions.Match m in webChecks)
            Assert.Contains("\"${golden_expect[@]}\"", m.Value);
        var smokes = System.Text.RegularExpressions.Regex.Matches(doc, @"--expect ""SMOKE OK 5 turns 1948-10-15 ([0-9a-f]{64})""");
        Assert.True(smokes.Count >= 2, $"expected the smoke5 --expect line of both web builds, found {smokes.Count}");
        foreach (System.Text.RegularExpressions.Match m in smokes)
            Assert.True(GoldenManifest.Expected("smoke5") == m.Groups[1].Value, $"docs/RELEASING.md shows a stale smoke5 hash: {m.Value}");
    }
}