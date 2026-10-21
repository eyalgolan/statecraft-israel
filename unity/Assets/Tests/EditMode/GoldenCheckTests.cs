using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Sim;
using System.Linq;

public class GoldenCheckTests
{
    [Test]
    public void TheCheckReportsEveryManifestGoldenAsOkInTheEditor()
    {
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        var (ok, lines) = GoldenCheck.Run(inputs.Content, inputs.Goldens);
        foreach (var line in lines) TestContext.WriteLine(line);   // the full-set evidence in the results XML
        Assert.IsTrue(ok, string.Join("\n", lines));
        var expected = GoldenData.Parse(inputs.Goldens.Manifest).Select(x => "GOLDEN OK " + x.name + " " + x.hash).ToArray();
        CollectionAssert.AreEqual(expected, lines);
        Assert.AreEqual(inputs.Goldens.Scenarios.Count, expected.Count(x => x.StartsWith("GOLDEN OK scenario-")));
    }

    /// <summary>The check plays what the manifest lists, not a list of its own (#394): a golden it cannot play, a missing
    /// scenario file, a wrong hash or an empty manifest each give a FAIL line and fail the run; none is skipped.</summary>
    [Test]
    public void TheCheckFailsLoudlyOnAnUnknownGoldenAWrongHashOrAnEmptyManifest()
    {
        var c = ContentLoader.Load(Paths.Content);
        var h1950 = GoldenRun.Hash1950;
        var manifest = "# c\nhash1950 " + h1950 + "\nmade-up-golden " + h1950 + "\nscenario-nope " + h1950 + "\nhash1952 " + new string('0', 64) + "\n";
        var (ok, lines) = GoldenCheck.Run(c, new GoldenData { Manifest = manifest });
        Assert.IsFalse(ok);
        Assert.AreEqual(4, lines.Count, string.Join("\n", lines));
        Assert.AreEqual("GOLDEN OK hash1950 " + h1950, lines[0]);
        StringAssert.StartsWith("GOLDEN FAIL made-up-golden ", lines[1]);
        StringAssert.StartsWith("GOLDEN FAIL scenario-nope ", lines[2]);
        StringAssert.StartsWith("GOLDEN FAIL hash1952 ", lines[3]);
        var (emptyOk, emptyLines) = GoldenCheck.Run(c, new GoldenData { Manifest = "# nothing pinned\n" });
        Assert.IsFalse(emptyOk);
        StringAssert.StartsWith("GOLDEN FAIL", emptyLines.Single());
    }

    /// <summary>The editor reads the repository's manifest and every scenario file it names (the players read the copies
    /// ContentSync ships), so the set the test plays is the .NET set.</summary>
    [Test]
    public void TheEditorReadsTheWholeManifestAndEveryScenarioFile()
    {
        var g = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap).Goldens;
        var parsed = GoldenData.Parse(g.Manifest);
        var names = parsed.Select(x => x.name).ToList();
        CollectionAssert.IsSubsetOf(new[] { "hash1950", "hash1952", "smoke5", "mvp-end" }, names);
        CollectionAssert.AreEquivalent(names.Where(n => n.StartsWith("scenario-")).Select(n => n.Substring("scenario-".Length)).ToList(), g.Scenarios.Keys.ToList());
        Assert.AreEqual(GoldenRun.Hash1950, parsed.Single(x => x.name == "hash1950").hash);
        Assert.AreEqual(GoldenRun.Hash1952, parsed.Single(x => x.name == "hash1952").hash);
    }

    [TestCase("http://localhost:8000/index.html?golden", true)]
    [TestCase("http://localhost:8000/index.html?smoke=5&golden", true)]
    [TestCase("https://html.itch.zone/html/123/golden-project/index.html", false)]
    [TestCase("http://localhost:8000/index.html?goldenrod=1", false)]
    [TestCase("http://localhost:8000/index.html", false)]
    [TestCase("http://localhost:8000/index.html?golden.y", false)]          // Task 24 review M2: a whole query parameter
    [TestCase("http://localhost:8000/index.html?golden-x&smoke=5", false)]
    [TestCase("http://localhost:8000/index.html?golden#top", true)]
    [TestCase("http://localhost:8000/index.html?golden=1", true)]
    public void TheGoldenFlagIsAQueryParameterNotAPathWord(string url, bool expected) =>
        Assert.AreEqual(expected, UrlFlags.Has(url, "golden"));

    [Test]
    public void IntegerFlagsReadTheirValue()
    {
        Assert.AreEqual(5, UrlFlags.Int("http://x/index.html?golden&smoke=5", "smoke"));
        Assert.AreEqual(0, UrlFlags.Int("http://x/smoke=5/index.html", "smoke"));
    }

    [Test]
    public void AnOverflowingIntegerFlagReadsAsAbsent() =>
        Assert.AreEqual(0, UrlFlags.Int("http://x/index.html?smoke=99999999999999999999", "smoke"));

    [Test]
    public void ABootErrorQuitsWithCode1OnlyForADesktopGoldenRun()
    {
        Assert.AreEqual(1, GoldenCheck.QuitCodeOnError(requested: true, webgl: false));
        Assert.IsNull(GoldenCheck.QuitCodeOnError(requested: true, webgl: true));
        Assert.IsNull(GoldenCheck.QuitCodeOnError(requested: false, webgl: false));
    }

    /// <summary>Task 24 review M2: golden, profile and camlog share one rule (UrlFlags.Has): the flag is a whole query
    /// parameter, bare or with a value, ending at '&amp;', '#' or the end.</summary>
    [TestCase("profile")]
    [TestCase("camlog")]
    [TestCase("golden")]
    public void EveryBooleanFlagIsAWholeQueryParameter(string flag)
    {
        Assert.IsTrue(UrlFlags.Has($"http://x/index.html?{flag}", flag));
        Assert.IsTrue(UrlFlags.Has($"http://x/index.html?smoke=5&{flag}&y", flag));
        Assert.IsFalse(UrlFlags.Has($"http://x/index.html?{flag}-x", flag));
        Assert.IsFalse(UrlFlags.Has($"http://x/index.html?{flag}s", flag));
        Assert.IsFalse(UrlFlags.Has($"http://x/{flag}/index.html", flag));
    }
}
