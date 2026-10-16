#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Issue #321 (T58): the map's region files (the 1948 sub-districts, the 1949 armistice lines, the 1948-49 control
/// areas) are read at boot from their baked runtime forms (tools/regions_bake.py) instead of YamlDotNet over the YAML
/// (about 160 ms at cpu 4: 135 in GameRoot, 24 in the map). Each runtime file must give exactly the YAML's object, field by field; one that does not match
/// its YAML (its check or its version), is cut short anywhere or is malformed is refused with one warning and the YAML is
/// read. The map reads the sub-districts once and GameRoot's region layers take that object.</summary>
public class RegionsRuntimeTests
{
    string saveDir = "";

    [SetUp]
    public void SetUp() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-regions-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameInputs Inputs() => GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

    static string D(DateTime? d) => d?.ToString("yyyy-MM-dd") ?? "null";

    /// <summary>Everything a Subdistricts1948 holds that the map, the camps and the region layers read.</summary>
    static string Describe(Subdistricts1948 s) => string.Join("\n",
        new[] { s.Title, s.Line, s.Label, D(s.ShownFrom), D(s.ShownUntil), s.Layer.Name }
        .Concat(s.Fronts.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => f.Key + "=" + D(f.Value)))
        .Concat(s.Regions.OrderBy(r => r.Id).Select(r => $"{r.Id}|{r.Key}|{r.Name}|{r.District}|{r.Style}|{s.Layer.StyleOf(r.Id)}"))
        .Concat(s.Layer.Hexes.OrderBy(h => h.r).ThenBy(h => h.q).Select(h => $"{h.q},{h.r}:{s.Layer.RegionOf(h.q, h.r)}")));

    static string Describe(Armistice1949 a)
    {
        string Item(Armistice1949.Item i) =>
            $"{i.Id}|{i.Key}|{i.Name}|{i.Front.Key}|{D(i.ShownFrom)}|{i.Style}|{i.Agreement}|{i.LabelNote}|{D(i.Signed)}|{i.LowAccuracy}";
        return string.Join("\n",
            new[] { a.Title, a.Line, a.Label }
            .Concat(a.Fronts.Select(f => $"{f.Key}|{f.Name}|{f.Agreement}|{f.Note}|{D(f.Signed)}|{D(f.ShownFrom)}|{f.LowAccuracy}"))
            .Concat(a.Zones.Select(Item)).Concat(a.Lines.Select(Item))
            .Concat(a.ZoneHexes.OrderBy(k => k.Key.r).ThenBy(k => k.Key.q).Select(k => $"z {k.Key.q},{k.Key.r}:{k.Value.Id}"))
            .Concat(a.LineHexes.OrderBy(k => k.Key.r).ThenBy(k => k.Key.q).Select(k => $"l {k.Key.q},{k.Key.r}:{string.Join(",", k.Value.Select(x => x.Id))}")));
    }

    static string Describe(Control1948 c) => string.Join("\n",
        new[] { c.Title, c.Line, c.Meaning, D(c.ShownFrom) }
        .Concat(c.Periods.Select(p => $"{p.Id}|{D(p.From)}|{D(p.Until)}|{p.Legend}|{p.LineDate}|{string.Join(",", p.Fronts)}"))
        .Concat(c.Codes.OrderBy(k => k.Key.r).ThenBy(k => k.Key.q).Select(k => $"{k.Key.q},{k.Key.r}:{k.Value}")));

    /// <summary>The three files: their runtime and YAML texts, the runtime read, the YAML parse and the boot read.</summary>
    static IEnumerable<(string name, string tsv, string yaml, Func<string, string, string> runtime, Func<string, string> yamlRead,
        Func<string, string, Action<string>, string> boot)> Files(MapInputs m)
    {
        yield return ("subdistricts", m.RegionsTsv, m.RegionsYaml, (t, y) => Describe(Subdistricts1948.ParseRuntime(t, y)),
            y => Describe(Subdistricts1948.Parse(y)), (t, y, w) => Describe(Subdistricts1948.ReadRuntimeOrYaml(t, y, w)));
        yield return ("armistice", m.ArmisticeTsv, m.ArmisticeYaml, (t, y) => Describe(Armistice1949.ParseRuntime(t, y)),
            y => Describe(Armistice1949.Parse(y)), (t, y, w) => Describe(Armistice1949.ReadRuntimeOrYaml(t, y, w)));
        yield return ("control", m.ControlTsv, m.ControlYaml, (t, y) => Describe(Control1948.ParseRuntime(t, y)),
            y => Describe(Control1948.Parse(y)), (t, y, w) => Describe(Control1948.ReadRuntimeOrYaml(t, y, w)));
    }

    [Test]
    public void EachRuntimeFileGivesTheYamlsObjectFieldByField()
    {
        foreach (var (name, tsv, yaml, runtime, yamlRead, _) in Files(Inputs().Map))
        {
            Assert.IsNotEmpty(tsv, $"{name}: the runtime file (tools/regions_bake.py)");
            Assert.AreEqual(yamlRead(yaml), runtime(tsv, yaml), name);
        }
    }

    /// <summary>A runtime file for another YAML, of another version, cut inside its last number (the file ending there, or
    /// the rows after it kept), two bytes short, with a malformed row, or missing: one warning, and the YAML's object.</summary>
    [Test]
    public void AStaleCutOrBrokenRuntimeFileFallsBackToTheYamlWithOneWarning()
    {
        foreach (var (name, tsv, yaml, _, yamlRead, boot) in Files(Inputs().Map))
        {
            var expected = yamlRead(yaml);
            var lines = tsv.Split('\n');
            int last = Array.FindLastIndex(lines, l => l.Length > 0 && !l.StartsWith("meta\t") && !l.StartsWith("#"));
            string lastRow = lines[last];
            int digit = lastRow.Length - 1;
            while (digit > 0 && !char.IsDigit(lastRow[digit])) digit--;
            Assert.Greater(digit, 0, $"{name}: the last data row ends in a number");
            string Cut(int keepRows) => string.Join("\n", lines.Take(last)) + "\n"
                + (keepRows > 0 ? lastRow.Remove(digit, 1) + "\n" + string.Join("\n", lines.Skip(last + 1)) : lastRow.Substring(0, digit));
            var cases = new (string tsv, string yaml, string why)[]
            {
                (tsv, yaml + "\n# an edit after the bake\n", "made from another YAML"),
                (tsv.Replace($"meta\tversion\t{RegionsRuntime.Version}\n", $"meta\tversion\t{RegionsRuntime.Version + 1}\n"), yaml, "version"),
                (Cut(0), yaml, "cut inside the last number, the file ending there"),
                (Cut(1), yaml, "the last number a digit short, the rows after it kept"),
                (tsv.Substring(0, tsv.Length - 2), yaml, "two bytes off the end"),
                (tsv.Replace("\nscalar\t", "\nscalar\tx\t"), yaml, "a malformed row"),
                ("", yaml, "missing"),
            };
            foreach (var (t, y, why) in cases)
            {
                Assert.IsFalse(t == tsv && y == yaml, $"{name}: {why} changes the runtime file or its YAML");
                var warnings = new List<string>();
                var got = boot(t, y, warnings.Add);
                Assert.AreEqual(1, warnings.Count, $"{name}: {why}: {string.Join(" | ", warnings)}");
                StringAssert.Contains("reading the YAML", warnings[0], $"{name}: {why}");
                Assert.AreEqual(y == yaml ? expected : yamlRead(y), got, $"{name}: {why}");
            }
            var none = new List<string>();
            Assert.AreEqual(expected, boot(tsv, yaml, none.Add), $"{name}: the shipped file");
            CollectionAssert.IsEmpty(none, $"{name}: the shipped file is read without a warning");
        }
    }

    /// <summary>Review of #601: a leading zero. The game's YAML path (YamlDotNet 18.1.0, the region classes' own deserializer)
    /// reads an integer with a leading zero as octal: checked with that DLL and Subdistricts1948.Parse's builder chain,
    /// "id: 010" gives 8, "[012, 0, 17]" gives q = 10, "[-012, 0, 17]" gives -10, and "id: 08" throws. The runtime reader
    /// would read 12, so it refuses such a number and the YAML is read (one warning). Each case edits the YAML and its
    /// runtime file alike (yaml_check and body_check made again), so only the number's reading differs.</summary>
    [Test]
    public void ALeadingZeroIsReadAsTheYamlPathReadsIt()
    {
        var m = Inputs().Map;
        foreach (var (yamlOld, yamlNew, tsvOld, tsvNew, why) in new[]
        {
            ("  - [12, 0, 17]\n", "  - [012, 0, 17]\n", "\t870\t12 0 17;", "\t870\t012 0 17;", "a hex's q, 012 (octal 10 on the YAML path)"),
            ("{ id: 17, key: outside,", "{ id: 021, key: outside,", "id=s:17\tkey=s:outside", "id=s:021\tkey=s:outside", "a region id, 021 (octal 17)"),
        })
        {
            StringAssert.Contains(yamlOld, m.RegionsYaml, why); StringAssert.Contains(tsvOld, m.RegionsTsv, why);
            var yaml = m.RegionsYaml.Replace(yamlOld, yamlNew);
            var lines = Rebody(m.RegionsTsv.Replace(tsvOld, tsvNew)).Split('\n');
            int check = Array.FindIndex(lines, l => l.StartsWith("meta\tyaml_check\t"));
            lines[check] = "meta\tyaml_check\t" + Statecraft.Game.Map.Routes.RouteRibbonFile.Check(yaml).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var warnings = new List<string>();
            var got = Describe(Subdistricts1948.ReadRuntimeOrYaml(string.Join("\n", lines), yaml, warnings.Add));
            Assert.AreEqual(Describe(Subdistricts1948.Parse(yaml)), got, why);
            Assert.AreEqual(1, warnings.Count, $"{why}: {string.Join(" | ", warnings)}");
        }
    }

    /// <summary>Review of #601: a runtime file of another version says so, before any row it cannot read (a version 1 file's
    /// typed values are not version 2's).</summary>
    [Test]
    public void AnOldVersionIsNamedFirst()
    {
        var m = Inputs().Map;
        var old = m.RegionsTsv.Replace($"meta\tversion\t{RegionsRuntime.Version}\n", "meta\tversion\t1\n").Replace("id=s:1\t", "id=i:1\t");
        var warnings = new List<string>();
        Subdistricts1948.ReadRuntimeOrYaml(old, m.RegionsYaml, warnings.Add);
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains($"version 1, this build reads {RegionsRuntime.Version}", warnings[0]);
    }

    /// <summary>A field the class does not read is refused, as YamlDotNet refuses an unknown key: the YAML is read.</summary>
    [Test]
    public void AnUnknownFieldIsRefused()
    {
        var m = Inputs().Map;
        var lines = m.RegionsTsv.Split('\n').ToList();
        int at = lines.FindIndex(l => l.StartsWith("item\tregions\t"));
        lines[at] += "\tcolour=s:red";
        var e = Assert.Throws<InvalidDataException>(() => Subdistricts1948.ParseRuntime(Rebody(string.Join("\n", lines)), m.RegionsYaml));
        StringAssert.Contains("unknown field 'colour'", e!.Message);
    }

    /// <summary>A runtime file with its body_check made again for edited rows (a test aid: the map then shows which file it
    /// read).</summary>
    static string Rebody(string tsv)
    {
        var lines = tsv.Split('\n').ToList();
        var rows = lines.Where(l => l.Length > 0 && !l.StartsWith("#") && !l.StartsWith("meta\t")).ToList();
        int body = lines.FindIndex(l => l.StartsWith("meta\tbody_check\t"));
        lines[body] = "meta\tbody_check\t" + RegionsRuntime.BodyCheck(rows).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Join("\n", lines);
    }

    /// <summary>MapScene's and GameRoot's own paths read the runtime files: each file gets one edited name in its runtime
    /// form alone (body_check made again, yaml_check still the YAML's), and the booted game shows the edited names. A map or
    /// a GameRoot that went back to the YAML would show the YAML's names. The region layers take the map's sub-districts.</summary>
    [Test]
    public void TheMapAndGameRootReadTheRuntimeFiles()
    {
        var inputs = Inputs(); var m = inputs.Map;
        m.RegionsTsv = Rebody(m.RegionsTsv.Replace("name=s:Acre Sub-District", "name=s:Acre (runtime)"));
        m.ArmisticeTsv = Rebody(m.ArmisticeTsv.Replace("name=s:Egypt\t", "name=s:Egypt (runtime)\t"));
        m.ControlTsv = Rebody(m.ControlTsv.Replace("id=s:truce1\t", "id=s:truce1x\t"));
        foreach (var (tsv, before) in new[] { (m.RegionsTsv, "Acre (runtime)"), (m.ArmisticeTsv, "Egypt (runtime)"), (m.ControlTsv, "truce1x") })
            StringAssert.Contains(before, tsv);
        var fallbacks = new List<string>();
        void Watch(string message, string stack, LogType type) { if (message.Contains("reading the YAML")) fallbacks.Add(message); }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        Application.logMessageReceived += Watch;
        GameRoot root;
        try
        {
            map.Build(m);
            root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(inputs, 0);
        }
        finally { Application.logMessageReceived -= Watch; }
        CollectionAssert.IsEmpty(fallbacks);
        Assert.IsNotNull(map.Subdistricts, "the map read the sub-districts for the camps");
        Assert.AreEqual("Acre (runtime)", map.Subdistricts!.Regions.Single(r => r.Key == "acre").Name, "the map read the runtime file");
        var regions = root.Regions!;
        Assert.AreSame(map.Subdistricts, regions.Subdistricts, "GameRoot took the map's sub-districts");
        Assert.AreEqual("Egypt (runtime)", regions.Armistice!.FrontOf("egypt")!.Name, "GameRoot read the armistice runtime file");
        CollectionAssert.Contains(regions.Control!.Periods.Select(p => p.Id).ToList(), "truce1x", "GameRoot read the control runtime file");
    }

    /// <summary>The manifest ships the runtime files to the player, beside their YAML (each is checked against it).</summary>
    [Test]
    public void TheManifestShipsTheRuntimeFiles()
    {
        var files = ContentManifest.Collect(Paths.Content);
        foreach (var f in new[] { Subdistricts1948.RuntimeFileName, Armistice1949.RuntimeFileName, Control1948.RuntimeFileName,
            Subdistricts1948.FileName, Armistice1949.FileName, Control1948.FileName })
            CollectionAssert.Contains(files, "map/" + f);
    }
}
