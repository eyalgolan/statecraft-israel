#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>#2905's End turn probe (?turnlog, TurnProbe): off, it records and writes nothing and allocates nothing; on, every
/// handler of SimBridge.EndTurn's events gets a time on one line of a fixed format, and the game runs as it does off.</summary>
public class TurnProbeTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge NewBridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);

    // The whole line: TURNLOG v=1 turn=N date=yyyy-MM-dd total_ms=X step_ms=X handlers=N, then key=X fields.
    static readonly Regex LineFormat = new Regex(
        @"^TURNLOG v=1 turn=\d+ date=\d{4}-\d{2}-\d{2} total_ms=\d+\.\d{2} step_ms=\d+\.\d{2} handlers=\d+( @?[^\s=]+=\d+\.\d{2})*$");

    readonly List<string> logged = new List<string>();
    void Capture(string message, string stack, LogType type) { if (message.StartsWith(TurnProbe.Prefix, StringComparison.Ordinal)) logged.Add(message); }
    bool wasOn;

    // TurnProbe is static: each test starts and ends with no open turn, so one failed test (an assert between BeginTurn
    // and EndTurn) cannot leave Active set and fail the next ones. Closing it through EndTurn with a no-op log logs nothing.
    static void CloseOpenTurn() => TurnProbe.EndTurn(_ => { });

    [SetUp] public void Listen() { wasOn = TurnProbe.On; CloseOpenTurn(); logged.Clear(); Application.logMessageReceived += Capture; }
    [TearDown] public void Stop() { Application.logMessageReceived -= Capture; CloseOpenTurn(); TurnProbe.On = wasOn; }

    static Dictionary<string, string> Fields(string line) =>
        line.Split(' ').Skip(1).Select(f => f.Split('=')).ToDictionary(kv => kv[0], kv => kv[1]);

    void Named() { }

    [Test]
    public void Off_it_records_and_writes_nothing()
    {
        TurnProbe.On = false;
        var bridge = NewBridge();
        int ran = 0;
        bridge.Changed += () => ran++;
        bridge.TurnEnded += _ => ran++;
        var before = TurnProbe.Last;
        bridge.EndTurn();
        Assert.AreEqual(2, ran, "the handlers ran");
        Assert.IsEmpty(logged, "no TURNLOG line");
        Assert.AreSame(before, TurnProbe.Last, "no line made");
        Assert.IsFalse(TurnProbe.Active);
        Assert.AreEqual(0, TurnProbe.Start(), "no clock read");
        Assert.AreEqual(0, TurnProbe.Lap("x", 123), "no lap");
    }

    [Test]
    public void Off_the_hooks_allocate_nothing()
    {
        TurnProbe.On = false;
        Action handler = Named;
        TurnProbe.Handler("Changed", handler, 0); TurnProbe.Lap("warm", 0);   // any first-call cost outside the count
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            var t = TurnProbe.Start();
            t = TurnProbe.Lap("UnitLayer.Rebuild", t);
            TurnProbe.Handler("Changed", handler, t);
            TurnProbe.Step(t);
        }
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "off: no allocation in the hooks");

        // The control: the same hooks in a probed turn allocate (the handler keys), so the count above can see allocation.
        TurnProbe.On = true;
        TurnProbe.BeginTurn(1, new DateTime(1948, 6, 15));
        before = GC.GetAllocatedBytesForCurrentThread();
        TurnProbe.Handler("Changed", handler, TurnProbe.Start());
        Assert.Greater(GC.GetAllocatedBytesForCurrentThread() - before, 0, "on: the counter sees the handler key");
        TurnProbe.EndTurn(_ => { });
    }

    [Test]
    public void On_every_handler_gets_a_time_in_the_order_it_ran()
    {
        TurnProbe.On = true;
        var bridge = NewBridge();
        bridge.Changed += Named;
        bridge.Changed += () => TurnProbe.Lap("Inner", TurnProbe.Start());
        bridge.Changed += () => { };
        bridge.CombatResolved += _ => { };
        bridge.TurnEnded += _ => { };
        var turn = bridge.TurnStart.Turn; var date = bridge.TurnStart.Date;
        bridge.EndTurn();
        Assert.AreEqual(1, logged.Count, "one line a turn");
        var line = logged[0];
        Assert.AreEqual(line, TurnProbe.Last);
        StringAssert.IsMatch(LineFormat.ToString(), line);
        var f = Fields(line);
        Assert.AreEqual(turn.ToString(CultureInfo.InvariantCulture), f["turn"]);
        Assert.AreEqual(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), f["date"]);
        Assert.AreEqual("5", f["handlers"]);
        var keys = line.Split(' ').Skip(7).Select(x => x.Split('=')[0]).ToList();
        CollectionAssert.AreEqual(new[]
        {
            "Changed/TurnProbeTests.Named",
            "Changed/TurnProbeTests.On_every_handler_gets_a_time_in_the_order_it_ran.lambda",
            "Changed/TurnProbeTests.On_every_handler_gets_a_time_in_the_order_it_ran.lambda#2",
            "CombatResolved/TurnProbeTests.On_every_handler_gets_a_time_in_the_order_it_ran.lambda",
            "TurnEnded/TurnProbeTests.On_every_handler_gets_a_time_in_the_order_it_ran.lambda",
            "@Inner",
        }, keys);
        Assert.IsFalse(TurnProbe.Active, "the turn is closed");

        // Only End turn is probed: an order's Changed outside it writes nothing.
        logged.Clear();
        bridge.Skip(bridge.TurnStart.Units.Keys.First());
        Assert.IsEmpty(logged);
    }

    [Test]
    public void On_the_game_runs_as_it_does_off()
    {
        var hashes = new List<string>();
        foreach (var on in new[] { false, true })
        {
            TurnProbe.On = on;
            var bridge = NewBridge();
            for (int t = 0; t < 3; t++) bridge.EndTurn();
            hashes.Add(bridge.TurnStart.Hash());
        }
        Assert.AreEqual(hashes[0], hashes[1], "the probe never changes the sim");
        Assert.AreEqual(3, logged.Count, "one line for each probed turn");
    }

    [Test]
    public void The_line_format_is_fixed_and_culture_invariant()
    {
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");   // a decimal comma: the line must not use it
            var line = TurnProbe.Line(3, new DateTime(1948, 8, 15), 1234.567, 7.5,
                new[] { ("Changed/GameRoot.RenderPlacementsIfChanged", 12.345), ("TurnEnded/A b=c", 0.004) },
                new[] { ("UnitLayer.Rebuild", 45.0) });
            Assert.AreEqual("TURNLOG v=1 turn=3 date=1948-08-15 total_ms=1234.57 step_ms=7.50 handlers=2 "
                + "Changed/GameRoot.RenderPlacementsIfChanged=12.35 TurnEnded/A_b_c=0.00 @UnitLayer.Rebuild=45.00", line);
            StringAssert.IsMatch(LineFormat.ToString(), line);
        }
        finally { Thread.CurrentThread.CurrentCulture = culture; }
    }

    [Test]
    public void Handler_names_drop_the_compilers_numbering()
    {
        Action group = Named;
        Action lambda = () => { };
        void Local() { }
        Action local = Local;
        Assert.AreEqual("TurnProbeTests.Named", TurnProbe.HandlerName(group));
        Assert.AreEqual("TurnProbeTests.Handler_names_drop_the_compilers_numbering.lambda", TurnProbe.HandlerName(lambda));
        Assert.AreEqual("TurnProbeTests.Handler_names_drop_the_compilers_numbering.Local", TurnProbe.HandlerName(local));
    }

    /// <summary>The game as a player boots it: the line times every subscriber of the three events, and the map layers'
    /// sections #2905 measured (the unit tokens, the special buildings, the other scenery layers).</summary>
    [Test]
    public void In_the_booted_game_every_subscriber_and_the_map_layers_are_timed()
    {
        var saveDir = Path.Combine(Path.GetTempPath(), "statecraft-turnprobe-" + Guid.NewGuid().ToString("N"));
        try
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
            Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
            var root = Object.FindFirstObjectByType<GameRoot>();
            root.Store = new SaveStore(saveDir);
            root.Begin(inputs, 0);
            var bridge = root.Bridge!;
            int Subscribers(string evt) =>
                (typeof(SimBridge).GetField(evt, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bridge) as Delegate)?.GetInvocationList().Length ?? 0;
            int expected = Subscribers(nameof(SimBridge.Changed)) + Subscribers(nameof(SimBridge.CombatResolved)) + Subscribers(nameof(SimBridge.TurnEnded));
            Assert.Greater(expected, 10, "the game's presenters listen");

            TurnProbe.On = true;
            bridge.EndTurn();
            Assert.AreEqual(1, logged.Count);
            var line = logged[0];
            StringAssert.IsMatch(LineFormat.ToString(), line);
            var f = Fields(line);
            Assert.AreEqual(expected.ToString(CultureInfo.InvariantCulture), f["handlers"], line);
            Assert.AreEqual(expected, f.Keys.Count(k => k.Contains("/")), line);
            foreach (var key in new[]
            {
                "Changed/GameRoot.RenderPlacementsIfChanged", "TurnEnded/GameStart.Autosave.Save",
                "@GameRoot.regions", "@UnitLayer.Show", "@GameRoot.unitsDim",
                "@RouteLayer", "@ArmisticeLines", "@TownLandmarks", "@WildlifeLayer", "@HulaPapyrusLayer", "@SiteGroundLayer",
                "@CivilianPeople", "@RuralLife", "@SpecialBuildingModels", "@HexPropModels", "@SeaAirCollection", "@Sites", "@Placements",
            })
                Assert.IsTrue(f.ContainsKey(key), key + " in " + line);
            Assert.Greater(double.Parse(f["step_ms"], CultureInfo.InvariantCulture), 0, "the sim step is timed");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
        }
    }
}
