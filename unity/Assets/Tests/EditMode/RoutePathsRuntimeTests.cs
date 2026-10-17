#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Routes;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #321: the map reads route_paths_1948.yaml's baked runtime form (route_paths_1948.tsv, tools/route_paths_bake.py)
/// at boot instead of parsing the YAML (about 125 ms of the map build at cpu 4), as #291 did for the localities. The
/// runtime file must give exactly the routes the YAML gives; one that does not match the YAML it ships with (its check or
/// its version) is refused with a warning and the YAML is read instead.</summary>
public class RoutePathsRuntimeTests
{
    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static MapInputs Inputs() => GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);

    static (double, double, double, double) Bounds()
    {
        var b = HeightmapLoader.Load(Paths.StreamingMap).meta;
        return (b.lon0, b.lon1, b.lat0, b.lat1);
    }

    [Test]
    public void TheRuntimeFileYieldsTheSameRoutesAsTheYaml()
    {
        var inputs = Inputs();
        Assert.IsNotEmpty(inputs.RoutesTsv, "content/map/" + RoutePaths.RuntimeFileName + " (tools/route_paths_bake.py)");
        var yaml = RoutePaths.Parse(inputs.RoutesYaml, Bounds());
        var tsv = RoutePaths.ParseRuntime(inputs.RoutesTsv, inputs.RoutesYaml, Bounds());
        Assert.AreEqual(yaml.Count, tsv.Count);
        for (int i = 0; i < yaml.Count; i++)
        {
            var (y, t) = (yaml[i], tsv[i]);
            Assert.AreEqual((y.Id, y.Kind, y.Status, y.LengthKm, y.From, y.To, y.OnSymbol, y.MaxGapM, y.Points.Count),
                (t.Id, t.Kind, t.Status, t.LengthKm, t.From, t.To, t.OnSymbol, t.MaxGapM, t.Points.Count), y.Id);
            for (int k = 0; k < y.Points.Count; k++)
            {
                Assert.AreEqual(2, t.Points[k].Count, $"{y.Id} point {k}");
                Assert.AreEqual(System.BitConverter.DoubleToInt64Bits(y.Points[k][0]), System.BitConverter.DoubleToInt64Bits(t.Points[k][0]), $"{y.Id} point {k} lon");
                Assert.AreEqual(System.BitConverter.DoubleToInt64Bits(y.Points[k][1]), System.BitConverter.DoubleToInt64Bits(t.Points[k][1]), $"{y.Id} point {k} lat");
            }
        }
    }

    /// <summary>The boot read: the runtime file when it is for this YAML, without a warning.</summary>
    [Test]
    public void TheMapReadsTheRuntimeFile()
    {
        var inputs = Inputs();
        var warnings = new List<string>();
        var routes = RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, Bounds(), warnings.Add);
        CollectionAssert.IsEmpty(warnings);
        Assert.AreEqual(RoutePaths.Parse(inputs.RoutesYaml, Bounds()).Count, routes.Count);
    }

    /// <summary>Review of #547, S2: the map scene itself reads the runtime file, not only ReadRuntimeOrYaml. The first route's
    /// first point is moved 0.01 degrees east in the runtime file alone (its body_check re-made, its yaml_check still the
    /// YAML's), so the routes the map builds show which file it read: a MapScene that went back to the YAML would put the
    /// corridor's first point where the YAML has it. No fallback warning is logged.</summary>
    [Test]
    public void TheMapSceneBuildsItsRoutesFromTheRuntimeFile()
    {
        var inputs = Inputs();
        var lines = inputs.RoutesTsv.Split('\n').ToList();
        int at = lines.FindIndex(l => l.StartsWith("route\t"));
        var c = lines[at].Split('\t'); var points = c[10].Split(';'); var first = points[0].Split(' ');
        double lon = double.Parse(first[0], CultureInfo.InvariantCulture) + 0.01, lat = double.Parse(first[1], CultureInfo.InvariantCulture);
        points[0] = lon.ToString("R", CultureInfo.InvariantCulture) + " " + first[1];
        c[10] = string.Join(";", points); lines[at] = string.Join("\t", c);
        int end = lines.FindIndex(l => l.StartsWith("meta\tbody_check\t"));
        lines[end] = "meta\tbody_check\t" + RoutePaths.BodyCheck(lines.Where(l => l.StartsWith("route\t") || l.StartsWith("service\t"))).ToString(CultureInfo.InvariantCulture);   // #668: the service rows too
        inputs.RoutesTsv = string.Join("\n", lines);

        var fallbacks = new List<string>();
        void Watch(string message, string stack, LogType type) { if (message.Contains("reading the YAML")) fallbacks.Add(message); }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        Application.logMessageReceived += Watch;
        try { map.Build(inputs); }
        finally { Application.logMessageReceived -= Watch; }
        CollectionAssert.IsEmpty(fallbacks);
        var moved = map.Frame.ToWorld(lon, lat, 0);
        var drawn = map.RouteCorridors[0].line[0];
        Assert.AreEqual(moved.x, drawn.x, 0.01f, "the corridor starts where the runtime file puts the route, not the YAML");
        Assert.AreEqual(moved.z, drawn.y, 0.01f);
    }

    /// <summary>A runtime file for another YAML, of another version, malformed or missing: a warning, and the YAML's routes.</summary>
    [Test]
    public void AStaleOrBrokenRuntimeFileFallsBackToTheYaml()
    {
        var inputs = Inputs();
        int expected = RoutePaths.Parse(inputs.RoutesYaml, Bounds()).Count;
        string otherYaml = inputs.RoutesYaml + "\n# an edit after the bake\n";
        string otherVersion = inputs.RoutesTsv.Replace($"meta\tversion\t{RoutePaths.RuntimeVersion}\n", $"meta\tversion\t{RoutePaths.RuntimeVersion + 1}\n");
        Assert.AreNotEqual(inputs.RoutesTsv, otherVersion, "the file carries its version");
        string badRow = inputs.RoutesTsv.Replace("\trail\t", "\tcanal\t");
        string cutRow = inputs.RoutesTsv.Substring(0, inputs.RoutesTsv.LastIndexOf(';'));
        string lastRowGone = inputs.RoutesTsv.Substring(0, inputs.RoutesTsv.TrimEnd('\n').LastIndexOf('\n') + 1);
        // Review of #547, S1: a file cut inside a number. The last route row's last number loses its last digit, with the
        // file ending there (a cut download) or with the rows after it intact; and the file loses its last two bytes.
        int lastRoute = inputs.RoutesTsv.LastIndexOf("\nroute\t") + 1;
        int lastRouteEnd = inputs.RoutesTsv.IndexOf('\n', lastRoute);
        string cutMidNumber = inputs.RoutesTsv.Substring(0, lastRouteEnd - 1);
        string shortNumber = inputs.RoutesTsv.Remove(lastRouteEnd - 1, 1);
        string twoBytesOff = inputs.RoutesTsv.Substring(0, inputs.RoutesTsv.Length - 2);
        foreach (var (tsv, yaml, why) in new[]
        {
            (inputs.RoutesTsv, otherYaml, "made from another YAML"), (otherVersion, inputs.RoutesYaml, "version"),
            (badRow, inputs.RoutesYaml, "unknown kind"), (cutRow, inputs.RoutesYaml, "a point cut short"), (lastRowGone, inputs.RoutesYaml, "a route row cut off"),
            (cutMidNumber, inputs.RoutesYaml, "cut inside the last number"), (shortNumber, inputs.RoutesYaml, "one number a digit short"),
            (twoBytesOff, inputs.RoutesYaml, "two bytes off the end"), ("", inputs.RoutesYaml, "missing"),
        })
        {
            var warnings = new List<string>();
            var routes = RoutePaths.ReadRuntimeOrYaml(tsv, yaml, Bounds(), warnings.Add);
            Assert.AreEqual(1, warnings.Count, why);
            StringAssert.Contains("reading the YAML", warnings[0], why);
            Assert.AreEqual(expected, routes.Count, why);
        }
    }

    /// <summary>Review of #547, N1: one cause, one warning. With both files missing, only the YAML's own warning; with a
    /// runtime file that matches the YAML but fails the reader's checks (here every point is outside the bounds), one
    /// warning and no routes, without parsing the YAML only to fail the same way.</summary>
    [Test]
    public void OneCauseGivesOneWarning()
    {
        var inputs = Inputs();
        var warnings = new List<string>();
        CollectionAssert.IsEmpty(RoutePaths.ReadRuntimeOrYaml("", "", Bounds(), warnings.Add));
        Assert.AreEqual(1, warnings.Count, string.Join(" | ", warnings));
        warnings.Clear();
        CollectionAssert.IsEmpty(RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, (0.0, 1.0, 0.0, 1.0), warnings.Add));
        Assert.AreEqual(1, warnings.Count, string.Join(" | ", warnings));
        StringAssert.Contains("outside the map", warnings[0]);
    }

    /// <summary>A malformed row is an error naming the file and its line, as the localities' runtime file.</summary>
    [Test]
    public void TheRuntimeFileRejectsBadRowsByLine()
    {
        var inputs = Inputs();
        var lines = inputs.RoutesTsv.Split('\n');
        int at = System.Array.FindIndex(lines, l => l.StartsWith("route\t"));
        lines[at] = lines[at].Replace("\t", "\t\t");
        var e = Assert.Throws<InvalidDataException>(() => RoutePaths.ParseRuntime(string.Join("\n", lines), inputs.RoutesYaml, Bounds()));
        StringAssert.Contains($"{RoutePaths.RuntimeFileName}:{at + 1}:", e!.Message);
    }

    /// <summary>The manifest ships the runtime file to the player, beside the YAML (the ribbon bake and this check read it).</summary>
    [Test]
    public void TheManifestShipsTheRuntimeFile()
    {
        var files = ContentManifest.Collect(Paths.Content);
        CollectionAssert.Contains(files, "map/" + RoutePaths.RuntimeFileName);
        CollectionAssert.Contains(files, "map/" + RoutePaths.DefaultFileName);
    }
}
