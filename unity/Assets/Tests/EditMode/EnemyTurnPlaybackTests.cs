#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 5; owner, 2 November 2026: "Q1-Q7
/// recommended, Q8 yes", and 8 November 2026: "All moves, as approved (Recommended)"): after End turn the driver plays the
/// enemy's beats one at a time with the camera going to each and back, and never changes the game. The bridge is built over a
/// real session, as UnitAttackPresenterTests does; the map and the camera are fakes that record what they are asked.</summary>
public class EnemyTurnPlaybackTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static GameSession NewSession() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);
    static string Hex(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);

    /// <summary>An enemy move line as Movement.Move writes it (as UnitBeatsTests.Move).</summary>
    internal static Cause Move(int id, int fromQ, int fromR, int toQ, int toR, string side = "egypt") =>
        new Cause("map", id.ToString(CultureInfo.InvariantCulture), 0, "Report text is never read", Movement.Kind,
            key: WhyKeys.UnitMoved, units: new[] { new UnitRef(new Unit(id, "infantry", fromQ, fromR, side: side)) },
            args: new[] { Hex(toQ, toR), "1" });

    /// <summary>An enemy engagement as Combat.Engage writes it (as UnitBeatsTests.Engagement), with a loss on the defenders.</summary>
    internal static Cause Engagement(int q, int r, int[] attackers, int[] defenders, string side = "egypt")
    {
        var units = attackers.Select(a => new UnitRef(new Unit(a, "infantry", q + 1, r, side: side)))
            .Concat(defenders.Select(d => new UnitRef(new Unit(d, "infantry", q, r, side: Movement.PlayerSide)))).ToList();
        return new Cause("map", Hex(q, r), 0, "Report text is never read", Combat.Attack, enemyAttackers: attackers,
            key: WhyKeys.Engagement, units: units,
            args: new[] { Hex(q, r), "150", "100", "1", attackers.Length.ToString(CultureInfo.InvariantCulture) });
    }

    /// <summary>A step that returns these lines after the open turn's, with the state unchanged (UnitAttackPresenterTests).</summary>
    internal static Func<GameSession, IReadOnlyList<Command>, StepResult> Step(params Cause[] lines) =>
        (session, orders) => new StepResult(session.State, session.Now.Why.Concat(lines).ToArray(), Array.Empty<Rejection>());

    internal sealed class FakeMap : IPlaybackMap
    {
        public readonly List<string> Log = new List<string>();
        public readonly List<int> Released = new List<int>();
        public readonly HashSet<int> Held = new HashSet<int>();
        public readonly HashSet<int> Gone = new HashSet<int>();
        public bool Shown = true;
        public Action<IReadOnlyList<Cause>>? OnFight;
        public void Hold(IEnumerable<int> ids) { var list = ids.ToList(); foreach (var id in list) Held.Add(id); Log.Add("hold " + string.Join(",", list)); }
        public bool Release(int id) { Released.Add(id); Log.Add("release " + id); return Held.Remove(id); }
        public void ReleaseAll(bool snap) { Log.Add("releaseall " + snap); Held.Clear(); }
        public void Fight(IReadOnlyList<Cause> lines) { Log.Add("fight " + lines.Count); OnFight?.Invoke(lines); }
        public bool Draws(int id) => !Gone.Contains(id);
        public bool UnitsShown => Shown;
        public Vector3? World(int q, int r) => new Vector3(q * 1000f, 0f, r * 1000f);
        public float HexMetres => 1000f;
    }

    internal sealed class FakeCamera : IPlaybackCamera
    {
        public Vector3 LookAt { get; set; } = new Vector3(-5000f, 0f, -5000f);
        public float Distance { get; set; } = 30000f;
        public Func<Vector3, bool> Sees = _ => false;   // every beat is off screen: each one pans
        public int Settles;
        public bool InView(Vector3 point) => Sees(point);
        public void Settle() => Settles++;
    }

    sealed class Kit : IDisposable
    {
        public readonly SimBridge Bridge;
        public readonly FakeMap Map = new FakeMap();
        public readonly FakeCamera Camera = new FakeCamera();
        public readonly EnemyTurnPlayback Playback;
        public bool On = true, Clip;
        public Kit(Func<GameSession, IReadOnlyList<Command>, StepResult>? step = null, GameSession? session = null)
        {
            Bridge = new SimBridge(session ?? NewSession(), Text, step);
            Playback = new EnemyTurnPlayback(Bridge, Map, Camera, () => On, () => Clip);
        }
        /// <summary>Ticks until the playback ends; the number of ticks.</summary>
        public int RunOut(float dt = 0.05f, int max = 4000)
        {
            int n = 0;
            while ((Playback.Active || Playback.Pending) && n < max) { Playback.Tick(dt); n++; }
            Assert.Less(n, max, "the playback ends");
            return n;
        }
        public void Dispose() => Playback.Dispose();
    }

    static readonly Cause[] ThreeMoves = { Move(901, 0, 0, 1, 0), Move(902, 30, 0, 31, 0), Move(903, 60, 0, 61, 0) };

    [Test]
    public void TheBridgeRaisesSteppedThenChangedThenCombatResolvedThenTurnEnded()
    {
        var bridge = new SimBridge(NewSession(), Text, Step(ThreeMoves));
        var order = new List<string>();
        bridge.Stepped += r => order.Add("stepped " + r.Why.Count(w => w.Key == WhyKeys.UnitMoved && w.Units[0].Id > 900));
        bridge.Changed += () => order.Add("changed");
        bridge.CombatResolved += _ => order.Add("combat");
        bridge.TurnEnded += _ => order.Add("turn");
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { "stepped 3", "changed", "combat", "turn" }, order);
    }

    [Test]
    public void SteppedIsNotRaisedOnARefusedTurnOrANewGame()
    {
        var bridge = new SimBridge(NewSession(), Text, (s, orders) => throw new ArgumentException("refused for the test"));
        int stepped = 0;
        bridge.Stepped += _ => stepped++;
        LogAssert.Expect(LogType.Warning, "End Turn refused: refused for the test");
        bridge.EndTurn();
        bridge.Replace(NewSession());
        Assert.AreEqual(0, stepped);
    }

    [Test]
    public void ThePlaybackVisitsEveryEnemyMoveInTheSimsOrder()
    {
        using var k = new Kit();
        k.Bridge.EndTurn();
        var why = k.Bridge.Session.Last!.Why;
        var moves = why.Where(w => w.Key == WhyKeys.UnitMoved && w.EnemyOrder == Movement.Kind).ToList();
        Assert.IsNotEmpty(moves, "fixture: the first 1948 turn has enemy moves");
        // Owner, 8 November 2026, "One side at a time (Recommended)": side by side, in the order the sides first act, each
        // side in the sim's order.
        var expected = moves.Select(w => w.Units[0].Side).Distinct()
            .SelectMany(side => moves.Where(w => w.Units[0].Side == side).Select(w => w.Units[0].Id)).ToArray();
        Assert.IsTrue(k.Playback.Active || k.Playback.Pending, "End turn started it");
        k.RunOut();
        var ids = new HashSet<int>(expected);
        CollectionAssert.AreEqual(expected, k.Map.Released.Where(ids.Contains).ToArray());
    }

    [Test]
    public void HeldTokensAreHeldBeforeTheMapRebuilds()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Bridge.Changed += () => k.Map.Log.Add("changed");
        k.Bridge.EndTurn();
        Assert.AreEqual("hold 901,902,903", k.Map.Log[0]);
        Assert.Less(k.Map.Log.IndexOf("hold 901,902,903"), k.Map.Log.IndexOf("changed"));
    }

    [Test]
    public void TheReportAndTheCardAreHeldFromTheStepUntilTheEnd()
    {
        using var k = new Kit(Step(ThreeMoves));
        var log = new List<string>();
        k.Playback.Holding += h => log.Add(h ? "held" : "released");
        k.Bridge.TurnEnded += _ => log.Add("turn");
        k.Bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { "held", "turn" }, log, "held at the step, before the report hears of the turn");
        k.RunOut();
        CollectionAssert.AreEqual(new[] { "held", "turn", "released" }, log, "released once, at the end");
    }

    [Test]
    public void SkipReleasesEverythingAtOnceAndReturnsTheCamera()
    {
        using var k = new Kit(Step(ThreeMoves));
        var saved = k.Camera.LookAt;
        k.Bridge.EndTurn();
        for (int i = 0; i < 10; i++) k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active);
        Assert.AreNotEqual(saved, k.Camera.LookAt, "fixture: the camera has gone to the first beat");
        int finished = 0; k.Playback.Finished += () => finished++;
        Assert.IsTrue(k.Playback.Skip());
        Assert.AreEqual("releaseall True", k.Map.Log.First(l => l.StartsWith("releaseall", StringComparison.Ordinal)));
        Assert.IsEmpty(k.Map.Held);
        Assert.AreEqual(saved, k.Camera.LookAt, "back on the player's view in the same call");
        Assert.IsFalse(k.Playback.Active);
        Assert.AreEqual(1, finished);
        Assert.IsFalse(k.Playback.Skip(), "nothing to skip now");
    }

    [Test]
    public void TheCameraReturnsToThePlayersViewAfterTheLastBeat()
    {
        using var k = new Kit(Step(ThreeMoves));
        var saved = k.Camera.LookAt;
        k.Bridge.EndTurn();
        k.RunOut();
        Assert.Less(Vector3.Distance(saved, k.Camera.LookAt), 1f);
        CollectionAssert.AreEqual(new[] { 901, 902, 903 }, k.Map.Released.ToArray());
        Assert.IsEmpty(k.Map.Held);
    }

    [Test]
    public void APlayerPanMidPlaybackIsNotUndone()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Bridge.EndTurn();
        for (int i = 0; i < 6; i++) k.Playback.Tick(0.05f);
        var mine = new Vector3(12345f, 0f, 6789f);
        k.Camera.LookAt = mine;   // the player pans
        int settles = k.Camera.Settles;
        k.RunOut();
        Assert.AreEqual(mine, k.Camera.LookAt, "no further writes, no return glide");
        Assert.AreEqual(settles, k.Camera.Settles);
        Assert.IsTrue(k.Map.Released.Contains(903), "the moves still play");
    }

    [Test]
    public void AZoomMidPlaybackIsTheirsToo()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Bridge.EndTurn();
        for (int i = 0; i < 6; i++) k.Playback.Tick(0.05f);
        var at = k.Camera.LookAt;
        k.Camera.Distance = 12000f;   // the player zooms
        k.RunOut();
        Assert.AreEqual(at, k.Camera.LookAt);
        Assert.IsTrue(k.Playback.TookOver);
    }

    [Test]
    public void ThePlaybackChangesNoState()
    {
        // The real turns, with one more enemy move each, so that every turn has a playback whatever the sim did.
        var extra = Move(911, 0, 0, 1, 0);
        using var k = new Kit((s, o) => { var r = s.EndTurn(o); return new StepResult(r.State, r.Why.Append(extra).ToArray(), r.Rejected); });
        k.Bridge.EndTurn();
        Assert.IsTrue(k.Playback.Active || k.Playback.Pending, "fixture: a playback runs");
        string hash = k.Bridge.State.Hash(), save = k.Bridge.Session.Save.Write();
        var last = k.Bridge.Session.Last;
        var queued = k.Bridge.Queued.ToList();
        void Same(string when)
        {
            Assert.AreEqual(hash, k.Bridge.State.Hash(), when);
            Assert.AreEqual(save, k.Bridge.Session.Save.Write(), when);
            Assert.AreSame(last, k.Bridge.Session.Last, when);
            CollectionAssert.AreEqual(queued, k.Bridge.Queued, when + ": no order queued for the next End turn");
        }
        for (int i = 0; i < 20; i++) k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active, "fixture: still playing after 20 ticks");
        Same("during");
        k.RunOut();
        Same("after");
        k.Bridge.EndTurn();
        hash = k.Bridge.State.Hash(); save = k.Bridge.Session.Save.Write(); last = k.Bridge.Session.Last; queued = k.Bridge.Queued.ToList();
        k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active || k.Playback.Pending, "fixture: the second turn's playback runs");
        Assert.IsTrue(k.Playback.Skip(), "Skip ended a playback");
        Same("after Skip");
    }

    [Test]
    public void ADestroyedMoversBeatIsSkippedAndThePlaybackGoesOn()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Map.Gone.Add(902);
        var beats = new List<int>();
        k.Playback.BeatChanged += () => beats.Add(k.Playback.Current!.Moves[0].Unit);
        k.Bridge.EndTurn();
        k.RunOut();
        CollectionAssert.AreEqual(new[] { 901, 903 }, beats);
        CollectionAssert.AreEqual(new[] { 901, 903 }, k.Map.Released.ToArray());
    }

    [Test]
    public void EndTurnMidPlaybackFinishesTheOldOneFirst()
    {
        var second = new[] { Move(911, 0, 0, 1, 0) };
        int turn = 0;
        using var k = new Kit((s, o) => Step(turn++ == 0 ? ThreeMoves : second)(s, o));
        k.Bridge.EndTurn();
        k.Playback.Tick(0.05f);
        k.Map.Log.Clear();
        k.Bridge.EndTurn();
        Assert.AreEqual("releaseall True", k.Map.Log[0], "the old one ends first");
        Assert.AreEqual("hold 911", k.Map.Log[1], "then only the new turn's units are held");
        CollectionAssert.AreEquivalent(new[] { 911 }, k.Map.Held);
        Assert.AreEqual(1, k.Playback.Count);
    }

    [Test]
    public void NewGameMidPlaybackDropsIt()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Bridge.EndTurn();
        k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active);
        k.Bridge.Replace(NewSession());
        Assert.IsFalse(k.Playback.Active);
        Assert.IsFalse(k.Playback.Pending);
        Assert.IsEmpty(k.Map.Held);
    }

    [Test]
    public void DisposingMidPlaybackReleasesEveryTokenAndReturnsTheCamera()
    {
        var k = new Kit(Step(ThreeMoves));
        var saved = k.Camera.LookAt;
        k.Bridge.EndTurn();
        for (int i = 0; i < 10; i++) k.Playback.Tick(0.05f);
        k.Playback.Dispose();
        Assert.IsEmpty(k.Map.Held);
        Assert.AreEqual(saved, k.Camera.LookAt);
        k.Map.Log.Clear();
        k.Bridge.EndTurn();
        Assert.IsEmpty(k.Map.Log, "a disposed playback no longer listens");
        k.Playback.Dispose();
    }

    /// <summary>Teardown mid-glide (a language switch, OnDisable): GameRoot disposes the map labels before the playback (they
    /// are built after it), and the game camera's Settle refreshes them. So the playback puts the player's view back without
    /// Settle; the camera controller applies the look-at point on its next frame (#2967 review 9 November, S2).</summary>
    [Test]
    public void DisposingMidGlideReturnsTheCameraWithoutSettlingIt()
    {
        var k = new Kit(Step(ThreeMoves));
        var saved = k.Camera.LookAt;
        k.Bridge.EndTurn();
        for (int i = 0; i < 10; i++) k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active, "fixture: the playback runs");
        Assert.AreNotEqual(saved, k.Camera.LookAt, "fixture: the camera has left the player's view");
        int settles = k.Camera.Settles;
        Assert.Greater(settles, 0, "fixture: the glide settled the camera");
        k.Playback.Dispose();
        Assert.AreEqual(settles, k.Camera.Settles, "no Settle at teardown: its callback reaches presenters already disposed");
        Assert.AreEqual(saved, k.Camera.LookAt, "the player's view is back");
        Assert.IsEmpty(k.Map.Held, "every token stands on its place");
        Assert.IsFalse(k.Playback.Active || k.Playback.Pending);
    }

    [Test]
    public void ItWaitsForTheOutcomeClips()
    {
        using var k = new Kit(Step(ThreeMoves)) { Clip = true };
        k.Bridge.EndTurn();
        k.Playback.Start();
        k.Playback.Tick(0.05f);
        Assert.IsFalse(k.Playback.Active, "a clip shows: it waits");
        Assert.IsTrue(k.Playback.Pending);
        CollectionAssert.AreEquivalent(new[] { 901, 902, 903 }, k.Map.Held, "the tokens wait where they stood");
        k.Clip = false;
        k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active, "it begins once the clip has gone");
    }

    [Test]
    public void AClaimedEngagementPlaysOnItsBeatNotAtOnce()
    {
        var fight = Engagement(40, 0, new[] { 921, 922 }, new[] { 929 });
        using var k = new Kit(Step(Move(901, 0, 0, 1, 0), fight));
        var played = new List<int>();
        using var attacks = new UnitAttackPresenter(k.Bridge, id => { played.Add(id); return true; }) { Claimed = k.Playback.Claims };
        k.Map.OnFight = attacks.Play;
        k.Bridge.EndTurn();
        Assert.IsEmpty(played, "End turn leaves it for its beat");
        Assert.IsTrue(k.Playback.Claims(fight));
        k.RunOut();
        CollectionAssert.AreEqual(new[] { 921, 922 }, played, "once per attacker, on its beat");
        Assert.IsFalse(k.Playback.Claims(fight));
    }

    [Test]
    public void WithThePlaybackOffAttacksPlayAsBefore()
    {
        var fight = Engagement(40, 0, new[] { 921, 922 }, new[] { 929 });
        using var k = new Kit(Step(Move(901, 0, 0, 1, 0), fight)) { On = false };
        var played = new List<int>();
        using var attacks = new UnitAttackPresenter(k.Bridge, id => { played.Add(id); return true; }) { Claimed = k.Playback.Claims };
        k.Bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { 921, 922 }, played, "at End turn, as #2177 plays them");
        Assert.IsFalse(k.Playback.Active || k.Playback.Pending);
        Assert.IsEmpty(k.Map.Log, "nothing held, nothing released");
    }

    [Test]
    public void AnInstantAttackOrderIsNeverClaimed()
    {
        var session = NewSession();
        using var k = new Kit(Step(ThreeMoves), session);
        var played = new List<int>();
        using var attacks = new UnitAttackPresenter(k.Bridge, id => { played.Add(id); return true; }) { Claimed = k.Playback.Claims };
        k.Bridge.EndTurn();
        k.Playback.Tick(0.05f);
        Assert.IsTrue(k.Playback.Active, "fixture: an order given while the enemy's moves play");
        UnitOrder? order = null; int attacker = -1;
        foreach (var u in k.Bridge.State.Units.Values.Where(u => u.Side == Movement.PlayerSide))
        {
            var enemy = k.Bridge.State.Units.Values.FirstOrDefault(x => x.Side != Movement.PlayerSide && x.Side != ""
                && MapActions.Distance(x.Q, x.R, u.Q, u.R) == 1);
            if (enemy == null) continue;
            order = new UnitOrder(u.Id, Combat.Attack, enemy.Q, enemy.R); attacker = u.Id; break;
        }
        Assert.IsNotNull(order, "fixture: a player unit next to an enemy");
        Assert.IsNull(k.Bridge.Queue(order!));
        CollectionAssert.Contains(played, attacker, "the player's own attack plays at once");
    }

    [Test]
    public void NoPlaybackWhileTheUnitsAreHidden()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Map.Shown = false;
        int holding = 0; k.Playback.Holding += _ => holding++;
        k.Bridge.EndTurn();
        k.Playback.Tick(0.05f);
        Assert.IsFalse(k.Playback.Active || k.Playback.Pending);
        Assert.IsEmpty(k.Map.Log);
        Assert.AreEqual(0, holding, "the report is never held");
    }

    [Test]
    public void ABeatInViewDoesNotPan()
    {
        using var k = new Kit(Step(ThreeMoves));
        k.Camera.Sees = _ => true;
        var saved = k.Camera.LookAt;
        k.Bridge.EndTurn();
        k.RunOut();
        Assert.AreEqual(saved, k.Camera.LookAt, "the camera never moved");
        Assert.AreEqual(0, k.Camera.Settles);
        CollectionAssert.AreEqual(new[] { 901, 902, 903 }, k.Map.Released.ToArray());
    }

    /// <summary>Q3 (owner, 2 November 2026): "a 12 s cap on the whole playback". The whole playback is the beats and the
    /// camera's glide back to the player's view after them: an overlong turn (eight attacks far apart, each off screen, the
    /// player's view far from the last one, so the return glide is the longest pan) ends within the cap.</summary>
    [Test]
    public void AnOverlongTurnEndsWithinTheCapReturnGlideIncluded()
    {
        var fights = Enumerable.Range(0, UnitBeats.Timing.MaxBeats)
            .Select(i => Engagement(i * 30, 0, new[] { 921 + i }, new[] { 941 + i })).ToArray();
        using var k = new Kit(Step(fights));
        k.Bridge.EndTurn();
        Assert.IsTrue(k.Playback.Active, "fixture: the moves play");
        Assert.AreEqual(UnitBeats.Timing.MaxBeats, k.Playback.Count, "fixture: no fold");
        const float dt = 0.001f;
        int ticks = k.RunOut(dt, 40000);
        // Each phase (a pan, an act, the return) ends on the tick that passes its length, so it may run a tick or two over
        // (with the float sum of the ticks): at most 2 x MaxBeats + 1 phases, well under 0.1 s at this tick. The glide back
        // is up to Timing.PanMax (0.8 s) long, so a cap that leaves it out fails by far more.
        Assert.LessOrEqual(ticks * dt, UnitBeats.Timing.Cap + 0.1f,
            "the beats and the glide back together stay within the cap");
        Assert.Less(Vector3.Distance(new Vector3(-5000f, 0f, -5000f), k.Camera.LookAt), 1f, "fixture: the camera glided back");
    }
}
