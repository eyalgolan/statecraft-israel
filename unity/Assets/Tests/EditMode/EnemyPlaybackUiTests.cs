#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Fakes = EnemyTurnPlaybackTests;

/// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Tasks 6 and 7): the footer bar while the
/// enemy's moves play, the turn report and the event card waiting for it, and the Settings switch. The owner's answers on the
/// footer (8 November 2026): "Flag + name (Recommended)", so the bar shows the side's flag, the very design its tokens fly,
/// beside its name; and "One side at a time (Recommended)", so the picture changes once a side.</summary>
public class EnemyPlaybackUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(UiPaths.HebrewText(), "strings.he.yaml");
    static IReadOnlyList<UnitEntry> Rows() => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    static readonly string[] Sourced = { "jordan", "egypt", "iraq", "syria", "lebanon" };

    /// <summary>Game.uxml with the card, the report, the playback (fakes for the map and the camera) and its bar, built in
    /// GameRoot's order, and the hold wired as GameRoot wires it.</summary>
    sealed class Kit : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge;
        public readonly EventCardPresenter Card;
        public readonly TurnReportPresenter Report;
        public readonly EnemyTurnPlayback Playback;
        public readonly EnemyPlaybackBar Bar;
        public readonly ScreenNav Nav;
        public readonly Fakes.FakeMap Map = new Fakes.FakeMap();
        public bool On = true;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Kit(UiText text, Func<GameSession, IReadOnlyList<Command>, StepResult>? step = null, GameSession? session = null)
        {
            Bridge = new SimBridge(session ?? GameSession.New(C, GoldenRun.Seed, Mode.Historical), text, step);
            var nav = Nav = new ScreenNav(Ui.Root);
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            owned.Add(nav);
            owned.Add(Card = new EventCardPresenter(Ui.Root, Bridge, text, null));
            owned.Add(Report = new TurnReportPresenter(Ui.Root, Bridge, text, nav));
            owned.Add(Playback = new EnemyTurnPlayback(Bridge, Map, new Fakes.FakeCamera(), () => On, () => false));
            Playback.Holding += held => { Card.HeldForPlayback = held; Report.Held = held; };   // as GameRoot
            owned.Add(Bar = new EnemyPlaybackBar(Ui.Root, text, Playback, Bridge));
        }

        public void RunOut()
        {
            for (int n = 0; (Playback.Active || Playback.Pending) && n < 4000; n++) Playback.Tick(0.05f);
            Assert.IsFalse(Playback.Active || Playback.Pending, "the playback ends");
        }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Ui.Dispose();
        }
    }

    static readonly Cause[] ThreeSides =
    {
        Fakes.Move(901, 0, 0, 1, 0, side: "jordan"), Fakes.Move(902, 30, 0, 31, 0, side: "jordan"),
        Fakes.Move(903, 60, 0, 61, 0, side: "egypt"), Fakes.Move(904, 90, 0, 91, 0, side: "ala"),
    };

    // ---- The flag: one definition, the tokens' ----

    [Test]
    public void TheUiFlagIsTheTokensFlag([ValueSource(nameof(Sourced))] string side)
    {
        Assert.IsTrue(UnitFlags.Has(side), "fixture: a sourced 1948 flag");
        var shape = SideFlag.For(side)!;
        Assert.IsNotNull(shape);
        var cloth = UnitFlags.BuildCloth(side)!;
        try
        {
            var colours = UnitFlags.Colours(side);
            var points = new List<Vector2>(); var tints = new List<Color>();
            for (int k = 0; k < cloth.subMeshCount; k++)
            {
                var tris = cloth.GetTriangles(k);
                foreach (var i in tris) points.Add(new Vector2(cloth.vertices[i].x, cloth.vertices[i].y));
                for (int t = 0; t < tris.Length / 3; t++) tints.Add(colours[k]);
            }
            CollectionAssert.AreEqual(points, shape.Points, "every triangle of the token's cloth, face on");
            CollectionAssert.AreEqual(tints, shape.Colours, "in the token's own colours");
            Assert.AreEqual(colours.Count, cloth.subMeshCount, "one colour a part, as the token's materials");
        }
        finally { Object.DestroyImmediate(cloth); }
        Assert.Greater(shape.Bounds.width, shape.Bounds.height, "a flag, wider than tall");
    }

    [Test]
    public void TheArabLiberationArmyFliesThePennant()
    {
        Assert.IsFalse(UnitFlags.Has("ala"), "no sourced flag: nothing is invented");
        Assert.IsTrue(UnitFlags.HasPennant("ala"));
        var shape = SideFlag.For("ala")!;
        var outline = UnitFlags.PennantCloth();
        CollectionAssert.AreEqual(new[] { outline[0], outline[1], outline[2] }, shape.Points, "the map's pennant, as UnitFlags.BuildPennant");
        CollectionAssert.AreEqual(new[] { UnitFlags.PennantColour }, shape.Colours);
        // The token's mesh is its pole and finial, then the cloth: the last two triangles, its front and its back (a two-sided
        // plate), each on the same three points, in the cloth's plane at z = 0.
        var mesh = UnitFlags.BuildPennant();
        try
        {
            var tris = mesh.GetTriangles(0); var verts = mesh.vertices;
            Assert.GreaterOrEqual(tris.Length, 6, "fixture: a pole and a cloth");
            for (int side = 0; side < 2; side++)
            {
                var corners = Enumerable.Range(0, 3).Select(i => verts[tris[tris.Length - 6 + side * 3 + i]]).ToList();
                foreach (var p in outline.Take(3))
                    Assert.IsTrue(corners.Any(v => Vector3.Distance(v, new Vector3(p.x, p.y, 0f)) < 1e-5f),
                        $"the token's pennant is one triangle too: side {side} has the cloth's point {p}");
            }
        }
        finally { Object.DestroyImmediate(mesh); }
        Assert.IsNull(SideFlag.For("nobody"), "a side the map gives nothing gets nothing");
        Assert.IsNull(SideFlag.For(null));
    }

    // ---- The bar ----

    [Test]
    public void TheBarsFlagAndNameChangeExactlyAtSideBoundaries()
    {
        using var k = new Kit(En, Fakes.Step(ThreeSides));
        Assert.IsFalse(k.Bar.IsOpen, "closed before End turn");
        var seen = new List<(string? side, int changes, string name)>();
        k.Playback.BeatChanged += () => seen.Add((k.Bar.Side, k.Bar.Flag.Changes, k.Ui.Q<Label>("enemy-playback-side").text));
        k.Bridge.EndTurn();
        Assert.IsTrue(k.Bar.IsOpen);
        k.RunOut();
        CollectionAssert.AreEqual(new[] { "jordan", "jordan", "egypt", "ala" }, seen.Select(s => s.side).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 1, 2, 3 }, seen.Select(s => s.changes).ToArray(), "the picture changes once a side");
        CollectionAssert.AreEqual(new[] { "Jordan's turn", "Jordan's turn", "Egypt's turn", "The Arab Liberation Army's turn" },
            seen.Select(s => s.name).ToArray());
        Assert.IsFalse(k.Bar.IsOpen, "closed at the end");
        Assert.AreEqual(DisplayStyle.None, k.Ui.Q<VisualElement>(EnemyPlaybackBar.ElementName).style.display.value);
    }

    [Test]
    public void TheBarCountsTheMoves()
    {
        using var k = new Kit(En, Fakes.Step(ThreeSides));
        var steps = new List<string>();
        k.Playback.BeatChanged += () => steps.Add(k.Ui.Q<Label>("enemy-playback-step").text);
        k.Bridge.EndTurn();
        k.RunOut();
        CollectionAssert.AreEqual(new[] { "Step 1 of 4", "Step 2 of 4", "Step 3 of 4", "Step 4 of 4" }, steps);
    }

    [Test]
    public void TheCaptionNamesTheBeatsUnit([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var rows = Rows();
        using var k = new Kit(text);
        k.Bar.Units = rows;
        var lines = new List<string>();
        k.Playback.BeatChanged += () =>
        {
            if (k.Playback.Current!.Folded) return;
            lines.AddRange(k.Ui.Q<VisualElement>("enemy-playback-actions").Query<Label>(className: "enemy-playback-action").ToList().Select(l => l.text));
        };
        var turn = k.Bridge.EndTurn();
        k.RunOut();
        // The report's own lines for the same turn (TurnReportTests.TheEnemysOrdersAreReportedInBothLanguages).
        var expected = turn.Why.Where(w => w.EnemyOrder == Movement.Kind).Select(w =>
        {
            var row = rows.First(u => u.Id == C.StartingForces[int.Parse(w.Target, System.Globalization.CultureInfo.InvariantCulture)].Row);
            return text.Format("report.enemy_move", hebrew ? row.NameHe : row.Name);
        }).ToList();
        Assert.IsNotEmpty(lines);
        CollectionAssert.IsSubsetOf(lines, expected, "each line is the report's line for that unit");
        CollectionAssert.Contains(lines, hebrew ? "הלגיון הערבי עבר לעמדה חדשה" : "Arab Legion moved to a new position", "the Legion's march on Latrun");
        if (hebrew) Assert.IsTrue(lines.All(UiDirection.RightToLeft), "each line reads right to left");
    }

    [Test]
    public void AnAttackBeatNamesItsAttackers()
    {
        var fight = Fakes.Engagement(40, 0, new[] { 921, 922 }, new[] { 929 });
        using var k = new Kit(En, Fakes.Step(fight));
        k.Bridge.EndTurn();
        k.Playback.Tick(0.05f);
        var lines = k.Ui.Q<VisualElement>("enemy-playback-actions").Query<Label>(className: "enemy-playback-action").ToList().Select(l => l.text).ToArray();
        CollectionAssert.AreEqual(new[] { En.Format("report.enemy_attack", "infantry"), En.Format("report.enemy_attack", "infantry") }, lines,
            "one line an attacker (a type with no name: the type, as UnitNames.Of)");
    }

    [Test]
    public void TheSkipButtonIsFocusableAndSkips()
    {
        using var k = new Kit(En, Fakes.Step(ThreeSides));
        var skip = k.Ui.Q<Button>("enemy-playback-skip");
        Assert.AreEqual(En["playback.skip"], skip.text);
        Assert.AreEqual(En["playback.skip_tip"], skip.tooltip);
        Assert.IsTrue(skip.focusable);
        Assert.AreEqual(0, skip.tabIndex, "in the tab order");
        k.Bridge.EndTurn();
        Assert.IsTrue(k.Playback.Active);
        Assert.IsNull(k.Ui.Root.panel.focusController.focusedElement, "it opens without taking the focus (never modal)");
        skip.Focus();
        UiTestKit.Press(skip);
        Assert.IsFalse(k.Playback.Active);
        Assert.IsFalse(k.Bar.IsOpen);
        Assert.IsEmpty(k.Map.Held, "every token on its place");
        Assert.IsFalse(k.Playback.Skip(), "Esc after it skips nothing: the Escape chain goes on to the next screen");
    }

    [Test]
    public void EveryPlaybackKeyIsInBothLanguages()
    {
        var en = En; var he = He;
        var keys = en.Keys.Where(x => x.StartsWith("playback.", StringComparison.Ordinal)).Append("settings.enemymoves").ToList();
        CollectionAssert.IsSupersetOf(keys, new[] { "playback.title", "playback.step", "playback.skip", "playback.skip_tip", "playback.folded" });
        foreach (var side in Sourced.Append("ala")) CollectionAssert.Contains(keys, "playback.turn_" + side, "a name for each side with a flag");
        CollectionAssert.AreEquivalent(keys.Where(x => x != "settings.enemymoves"),
            he.Keys.Where(x => x.StartsWith("playback.", StringComparison.Ordinal)), "the same keys in Hebrew");
        foreach (var key in keys)
        {
            Assert.IsTrue(he.Has(key), key);
            Assert.AreEqual(Placeholders(en[key]), Placeholders(he[key]), key + ": the same placeholders (UI-15)");
            Assert.IsTrue(UiDirection.HasRightToLeft(he[key]), $"{key} is Hebrew: {he[key]}");
        }
    }

    static string[] Placeholders(string s) => System.Text.RegularExpressions.Regex.Matches(s, @"\{\d+\}").Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();

    // ---- The report and the card wait (Q1 a) ----

    [Test]
    public void TheReportWaitsForThePlaybackAndOpensWhenItEnds()
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();
        Assert.IsTrue(k.Playback.Active, "fixture: the opening turn has enemy moves");
        Assert.IsFalse(k.Report.IsOpen, "the report waits");
        k.Playback.Tick(0.5f);
        Assert.IsFalse(k.Report.IsOpen);
        k.RunOut();
        Assert.IsTrue(k.Report.IsOpen, "it opens when the playback ends");
        Assert.IsFalse(k.Report.Held);
    }

    [Test]
    public void SkipOpensTheReportAtOnce()
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();
        Assert.IsFalse(k.Report.IsOpen);
        Assert.IsTrue(k.Playback.Skip());
        Assert.IsTrue(k.Report.IsOpen);
    }

    /// <summary>End turn pressed again while the moves play (End turn is never blocked): the old playback ends, the new
    /// turn's plays, and the report goes on waiting. It opens once, when the new playback ends, for the new turn: the turn
    /// whose moves were just shown. It never opens in between, over the new playback, with the new turn's lines (#2967
    /// review, M1: the old playback's release opened it at once).</summary>
    [Test]
    public void EndTurnMidPlaybackKeepsTheReportWaitingForTheNewTurnsMoves()
    {
        var extra = Fakes.Move(911, 0, 0, 1, 0, side: "egypt");   // every turn has a move to play, whatever the sim did
        using var k = new Kit(En, (s, o) => { var r = s.EndTurn(o); return new StepResult(r.State, r.Why.Append(extra).ToArray(), r.Rejected); });
        var shown = new List<StepResult?>();
        k.Report.Showing += r => shown.Add(r);
        var held = new List<bool>();
        k.Playback.Holding += h => held.Add(h);
        k.Bridge.EndTurn();
        var first = k.Bridge.Session.Last;
        k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active, "fixture: the first turn's moves play");
        k.Bridge.EndTurn();
        var latest = k.Bridge.Session.Last;
        Assert.AreNotSame(first, latest, "fixture: a second turn ran");
        Assert.IsTrue(k.Playback.Active || k.Playback.Pending, "the new turn's moves play");
        Assert.IsEmpty(shown, "the report never opened over the new playback");
        Assert.IsFalse(k.Report.IsOpen, "it still waits");
        Assert.IsTrue(k.Report.Held);
        Assert.IsFalse(held.Contains(false), "the hold carried over from one playback to the next");
        k.RunOut();
        Assert.IsTrue(k.Report.IsOpen, "it opens when the new playback ends");
        Assert.AreEqual(1, shown.Count, "once");
        Assert.AreSame(latest, shown[0], "for the turn whose moves were just shown");
        Assert.AreEqual(false, held.Last(), "and the hold is let go once, at the end");
        Assert.AreEqual(1, held.Count(h => !h));
    }

    /// <summary>End turn pressed with a screen open (the top bar stays enabled under a side screen): End turn closes the
    /// screen, as it did before the playback, so the moves play on the open map and the report opens when they end,
    /// without waiting for the player to close a screen they never asked to keep (#2967 review 9 November, S1).</summary>
    [Test]
    public void EndTurnWithAScreenOpenClosesItAndTheReportOpensAfterTheMoves()
    {
        using var k = new Kit(En);
        k.Nav.Open("cabinet");
        Assert.AreEqual("cabinet", k.Nav.Current, "fixture: a screen is open at End turn");
        k.Bridge.EndTurn();
        Assert.IsTrue(k.Playback.Active || k.Playback.Pending, "fixture: the opening turn has enemy moves");
        Assert.AreEqual(null, k.Nav.Current, "End turn closes the open screen");
        Assert.IsFalse(k.Report.IsOpen, "the report waits for the moves");
        k.RunOut();
        Assert.IsTrue(k.Report.IsOpen, "the moves ended: the report opens");
        Assert.IsFalse(k.Report.Waiting);
    }

    /// <summary>The moves are not modal: the player may open a screen while they play (Settings, to turn them off; the
    /// Cabinet). When they end, the report does not close that screen under the player's hand; it opens once the player
    /// closes it, as long as nothing else holds it (#2967 review, S3).</summary>
    [Test]
    public void AScreenOpenedDuringThePlaybackStaysAndTheReportWaitsForItsClose([Values("settings", "cabinet")] string screen,
        [Values(false, true)] bool skipped)
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();
        k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active, "fixture: the opening turn has enemy moves");
        k.Nav.Open(screen);
        if (skipped) Assert.IsTrue(k.Playback.Skip());   // the Settings switch turned off, or Skip
        else k.RunOut();
        Assert.IsFalse(k.Playback.Active || k.Playback.Pending);
        Assert.AreEqual(screen, k.Nav.Current, "the screen stays open");
        Assert.IsFalse(k.Report.IsOpen, "the report waits for it");
        Assert.IsTrue(k.Report.Waiting);
        k.Nav.Open(screen == "settings" ? "cabinet" : "settings");
        Assert.IsFalse(k.Report.IsOpen, "another screen opened from it: still waiting");
        k.Nav.Close();
        Assert.IsTrue(k.Report.IsOpen, "the screens closed: the report opens");
        Assert.IsFalse(k.Report.Waiting);
        Assert.AreEqual(null, k.Nav.Current);
    }

    [Test]
    public void ANewGameDropsAReportWaitingForAScreen()
    {
        using var k = new Kit(En);
        k.Bridge.EndTurn();
        k.Nav.Open("settings");
        k.RunOut();
        Assert.IsTrue(k.Report.Waiting, "fixture: the report waits for Settings");
        k.Bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        Assert.IsFalse(k.Report.Waiting, "another game: its report is not this one");
        k.Nav.Close();
        Assert.IsFalse(k.Report.IsOpen);
    }

    [Test]
    public void WithNoEnemyMovesTheReportOpensAtOnce()
    {
        using var k = new Kit(En, (s, o) =>
        {
            var r = s.EndTurn(o);
            return new StepResult(r.State, r.Why.Where(w => w.EnemyOrder == "").ToArray(), r.Rejected);
        });
        k.Bridge.EndTurn();
        Assert.IsFalse(k.Playback.Active || k.Playback.Pending);
        Assert.IsTrue(k.Report.IsOpen);
    }

    [Test]
    public void ACardThatOpenedThisTurnWaitsThenOpensWithItsCue()
    {
        // The first turn whose End turn brings a card where none was waiting.
        int turns = 0;
        var probe = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (bool found = false; !found; turns++)
        {
            Assert.Less(turns, 80, "fixture: a turn that brings a card");
            bool none = probe.State.PendingEvents.Count == 0;
            probe.Step(new Command[0]);
            found = none && probe.State.PendingEvents.Count > 0;
        }
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < turns - 1; i++) g.Step(new Command[0]);
        Assert.IsEmpty(g.State.PendingEvents);
        // The real turn, with enemy moves added so that it plays whatever the sim did that turn.
        using var k = new Kit(En, (s, o) => { var r = s.EndTurn(o); return new StepResult(r.State, r.Why.Concat(ThreeSides).ToArray(), r.Rejected); }, g);
        var opened = new List<string>();
        k.Card.Opened += id => opened.Add(id);
        var card = k.Ui.Q<VisualElement>("event-card");
        Assert.IsNull(k.Card.Shown);
        k.Bridge.EndTurn();
        Assert.IsNotNull(k.Card.Shown, "fixture: End turn brought a card");
        Assert.IsTrue(k.Playback.Active);
        Assert.AreEqual(DisplayStyle.None, card.style.display.value, "the card waits for the playback");
        Assert.IsEmpty(opened, "and so does its cue");
        k.RunOut();
        Assert.AreEqual(DisplayStyle.Flex, card.style.display.value, "then it opens");
        CollectionAssert.AreEqual(new[] { k.Card.Shown!.Id }, opened, "with its cue, once");
    }

    // ---- The Settings switch (Q6 a; the switch itself: SettingsScreenTests) ----

    string dir = "";
    [SetUp] public void SetUp() => dir = Path.Combine(Path.GetTempPath(), "sc-enemymoves-" + Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown() { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

    [Test]
    public void WithItOffEndTurnPlaysNothing()
    {
        new SettingsStore(dir).Write(SettingsPresenter.EnemyMovesKey, "off");
        // The real opening turn: the enemy moves, and the bar moves too, so the report has lines (a Fakes.Step turn changes
        // no number, and an empty report never opens).
        using var k = new Kit(En);
        k.On = SettingsPresenter.EnemyMovesOn(new SettingsStore(dir));
        k.Bridge.EndTurn();
        Assert.IsNotEmpty(UnitBeats.From(k.Bridge.Session.Last!.Why, BeatSource.Enemy), "fixture: the turn has enemy moves");
        k.Playback.Tick(0.05f);
        Assert.IsFalse(k.Playback.Active || k.Playback.Pending);
        Assert.IsEmpty(k.Map.Log, "no token held");
        Assert.IsFalse(k.Bar.IsOpen);
        Assert.IsTrue(k.Report.IsOpen, "the report opens at End turn, as before");
    }

    // ---- The layout ----

    /// <summary>At 360 px wide (the narrowest the game supports) and at 1920, in English and in Hebrew, mid playback: the bar
    /// lies inside the window, below the End turn button; its flag, name and Skip fit inside it; in Hebrew the row is mirrored
    /// (the flag on the right), and the side's name reads right to left.</summary>
    [UnityTest]
    public IEnumerator TheSkipBarFitsAndMirrorsInHebrew([Values(360, 1920)] int w, [Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var rt = new RenderTexture(w, 800, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("EnemyPlaybackUiTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text, Fakes.Step(ThreeSides));
        var hud = new HudPresenter(root, bridge, text);
        var playback = new EnemyTurnPlayback(bridge, new Fakes.FakeMap(), new Fakes.FakeCamera(), () => true, () => false);
        var bar = new EnemyPlaybackBar(root, text, playback, bridge);
        try
        {
            bridge.EndTurn();
            for (int i = 0; i < 30 && !(root.Q("enemy-playback-skip").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(bar.IsOpen);
            var window = root.worldBound;
            var panel = root.Q("enemy-playback-panel").worldBound;
            var flag = root.Q("enemy-playback-flag").worldBound;
            var side = root.Q<Label>("enemy-playback-side");
            var skip = root.Q("enemy-playback-skip").worldBound;
            var endTurn = root.Q("end-turn").worldBound;
            string at = $"{w} px, {(hebrew ? "he" : "en")}";
            bool Inside(Rect inner, Rect outer) => inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f
                && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;
            Assert.IsTrue(Inside(panel, window), $"{at}: the bar {panel} is inside the window {window}");
            foreach (var (name, r) in new[] { ("flag", flag), ("name", side.worldBound), ("skip", skip) })
            {
                Assert.Greater(r.width, 0f, $"{at}: {name} is drawn");
                Assert.IsTrue(Inside(r, panel), $"{at}: the {name} {r} is inside the bar {panel}");
            }
            Assert.IsFalse(panel.Overlaps(endTurn), $"{at}: the bar {panel} leaves End turn {endTurn} clear");
            Assert.IsFalse(flag.Overlaps(skip) || flag.Overlaps(side.worldBound) || skip.Overlaps(side.worldBound), $"{at}: nothing overlaps");
            Assert.GreaterOrEqual(flag.width, 40f, $"{at}: the flag is large");
            if (hebrew)
            {
                Assert.Greater(flag.xMin, side.worldBound.xMax - 0.5f, $"{at}: the flag on the right");
                Assert.Less(skip.xMax, side.worldBound.xMin + 0.5f, $"{at}: Skip on the left");
                Assert.IsTrue(UiDirection.RightToLeft(side.text), $"{at}: {side.text}");
            }
            else
            {
                Assert.Less(flag.xMax, side.worldBound.xMin + 0.5f, $"{at}: the flag on the left");
                Assert.Greater(skip.xMin, side.worldBound.xMax - 0.5f, $"{at}: Skip on the right");
            }
        }
        finally
        {
            bar.Dispose(); playback.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
