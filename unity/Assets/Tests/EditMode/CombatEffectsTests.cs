#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.HexTiles;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>The combat effects the owner chose on 4 November 2026 ("Hit on target hex", "Dust when a unit dies"), drawn with
/// #2490's flipbook technique (Flipbook, FlipbookBursts): each burst starts where and when it should, plays on the clock
/// alone, ends and frees its slot, allocates nothing per frame, and reads nothing but the engagement's structured lines.</summary>
public class CombatEffectsTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static GameSession NewSession() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    readonly List<Object> made = new List<Object>();
    double now;

    [SetUp] public void HoldTheClock() { now = 100.0; CombatBursts.Clock = () => now; }

    [TearDown]
    public void Restore()
    {
        CombatBursts.Clock = () => Time.timeAsDouble;
        foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
        made.Clear();
    }

    CombatBursts Bursts(out GameObject parent)
    {
        parent = new GameObject("test parent"); made.Add(parent);
        var c = CombatBursts.Create(parent.transform);
        Assert.IsNotNull(c, "the two flipbooks ship in Resources/Effects (#2490)");
        return c!;
    }

    static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
    static int Shown(FlipbookBursts pool) => pool.Renderers.Count(r => r.enabled);

    [Test]
    public void TheFlipbookFramesRunAlongTheRowsFromTheTopLeft()
    {
        var meshes = new List<Mesh>();
        var frames = Flipbook.Frames(Flipbook.ExplosionGrid, m => { meshes.Add(m); return m; }, "test-frame");
        try
        {
            Assert.AreEqual(25, frames.Length); CollectionAssert.AreEqual(frames, meshes, "each frame is handed to its owner");
            Assert.AreEqual("test-frame-5x5-0", frames[0].name);
            Rect UVs(Mesh m) { var uv = m.uv; return Rect.MinMaxRect(uv.Min(p => p.x), uv.Min(p => p.y), uv.Max(p => p.x), uv.Max(p => p.y)); }
            Assert.AreEqual(Rect.MinMaxRect(0f, 0.8f, 0.2f, 1f).ToString("F4"), UVs(frames[0]).ToString("F4"), "frame 0 top left");
            Assert.AreEqual(Rect.MinMaxRect(0.2f, 0.8f, 0.4f, 1f).ToString("F4"), UVs(frames[1]).ToString("F4"), "then along the row");
            Assert.AreEqual(Rect.MinMaxRect(0.8f, 0f, 1f, 0.2f).ToString("F4"), UVs(frames[24]).ToString("F4"), "the last bottom right");
            Assert.AreEqual(0, Flipbook.FrameAt(-0.5f, 25)); Assert.AreEqual(24, Flipbook.FrameAt(1.5f, 25)); Assert.AreEqual(12, Flipbook.FrameAt(0.5f, 25));
            Assert.AreEqual(63, Flipbook.Looping(-0.5, 64), "a looping clock wraps below zero");
            Assert.AreEqual(1, Flipbook.Looping(65.2, 64));
        }
        finally { foreach (var m in meshes) Object.DestroyImmediate(m); }
    }

    /// <summary>"Hit on target hex", the guns: the shell bursts on the spot ShellFlightS after the gun's flash, plays the
    /// explosion flipbook through, then ends, frees its slot and stops the component.</summary>
    [Test]
    public void AGunsHitBurstsOnTheSpotAfterTheShellsFlightThenEnds()
    {
        var c = Bursts(out _);
        var spot = new Vector3(1000f, 50f, -2000f);
        Assert.IsTrue(c.Hit(spot, guns: true, smallArms: false));
        Assert.AreEqual(1, c.Blasts!.Busy); Assert.AreEqual(0, c.Dust!.Busy, "no small arms, no dust");
        Assert.IsTrue(c.enabled, "it runs while a burst plays");
        c.Sample(now + CombatBursts.ShellFlightS - 0.01);
        Assert.AreEqual(0, Shown(c.Blasts), "the shell is still in flight");
        c.Sample(now + CombatBursts.ShellFlightS + 0.1);
        var r = c.Blasts.Renderers.Single(x => x.enabled);
        Assert.Less((XZ(r.transform.position) - XZ(spot)).magnitude, 1f, "on the spot");
        Assert.Greater(r.transform.position.y, spot.y, "its foot on the ground, its centre above");
        int first = c.Blasts.FrameOf(Array.IndexOf(c.Blasts.Renderers, r));
        c.Sample(now + CombatBursts.ShellFlightS + 0.9 * CombatBursts.BlastStyle.LifeS);
        Assert.Greater(c.Blasts.FrameOf(Array.IndexOf(c.Blasts.Renderers, r)), first, "it plays the flipbook through");
        c.Sample(now + CombatBursts.ShellFlightS + CombatBursts.BlastStyle.LifeS + 0.01);
        Assert.AreEqual(0, Shown(c.Blasts)); Assert.AreEqual(0, c.Blasts.Busy, "its slot is free again");
        Assert.IsFalse(c.enabled, "nothing runs per frame once it ends");
    }

    /// <summary>"Hit on target hex", small arms: a volley of dust puffs round the spot, VolleyGapS apart from VolleyS on.</summary>
    [Test]
    public void SmallArmsRaiseAVolleyOfDustRoundTheSpot()
    {
        var c = Bursts(out _);
        var spot = new Vector3(-300f, 10f, 700f);
        Assert.IsTrue(c.Hit(spot, guns: false, smallArms: true));
        Assert.AreEqual(0, c.Blasts!.Busy, "no guns, no shell");
        Assert.AreEqual(CombatBursts.VolleyPuffs, c.Dust!.Busy);
        c.Sample(now + CombatBursts.VolleyS - 0.01);
        Assert.AreEqual(0, Shown(c.Dust), "nothing before the volley");
        c.Sample(now + CombatBursts.VolleyS + 0.01);
        Assert.AreEqual(1, Shown(c.Dust), "the first puff");
        c.Sample(now + CombatBursts.VolleyS + (CombatBursts.VolleyPuffs - 1) * CombatBursts.VolleyGapS + 0.01);
        Assert.AreEqual(CombatBursts.VolleyPuffs, Shown(c.Dust), "then the others");
        for (int i = 0; i < c.Dust.Capacity; i++)
            if (!double.IsNaN(c.Dust.StartOf(i)))
                Assert.LessOrEqual((XZ(c.Dust.Foot(i)) - XZ(spot)).magnitude, CombatBursts.SpreadShare * CombatBursts.HitDustM + 1f, "round the spot");
        c.Sample(now + CombatBursts.VolleyS + (CombatBursts.VolleyPuffs - 1) * CombatBursts.VolleyGapS + CombatBursts.DustStyle.LifeS + 0.01);
        Assert.AreEqual(0, c.Dust.Busy); Assert.AreEqual(0, Shown(c.Dust)); Assert.IsFalse(c.enabled);
    }

    /// <summary>The hit's first sight on its target, from the fire take (e21's capture of #2614): the volley's first puff when
    /// any small arms fired, else the shell's burst. A destroyed unit's dust and the defenders' shake start then.</summary>
    [Test]
    public void TheImpactTimeIsTheFirstHitSeen()
    {
        Assert.AreEqual(CombatBursts.ShellFlightS, CombatBursts.ImpactS(new CombatHit(0, 0, true, false, new int[0])));
        Assert.AreEqual(CombatBursts.VolleyS, CombatBursts.ImpactS(new CombatHit(0, 0, false, true, new int[0])));
        Assert.AreEqual(CombatBursts.VolleyS, CombatBursts.ImpactS(new CombatHit(0, 0, true, true, new int[0])), "the volley's puff shows first");
        Assert.AreEqual(0f, CombatBursts.ImpactS(new CombatHit(0, 0, false, false, new int[0])));
    }

    /// <summary>A destroyed unit's dust given a delay (the hit's impact time) shows nothing before it and rises then.</summary>
    [Test]
    public void ADestroyedUnitsDustWaitsForItsDelay()
    {
        var c = Bursts(out _);
        Assert.IsTrue(c.Destroyed(new Vector3(4000f, 120f, 900f), CombatBursts.ShellFlightS));
        c.Sample(now + CombatBursts.ShellFlightS - 0.01);
        Assert.AreEqual(0, Shown(c.Dust!), "nothing before the hit lands");
        c.Sample(now + CombatBursts.ShellFlightS + 0.01);
        Assert.Greater(Shown(c.Dust!), 0, "the cloud rises with the hit");
    }

    /// <summary>"Dust when a unit dies": the cloud rises at once where the token stood, then ends.</summary>
    [Test]
    public void ADestroyedUnitsDustRisesAtOnceAndEnds()
    {
        var c = Bursts(out _);
        var spot = new Vector3(4000f, 120f, 900f);
        Assert.IsTrue(c.Destroyed(spot));
        Assert.AreEqual(CombatBursts.DeathPuffs, c.Dust!.Busy);
        c.Sample(now + 0.01);
        var first = c.Dust.Renderers.Single(x => x.enabled);
        Assert.Less((XZ(first.transform.position) - XZ(spot)).magnitude, CombatBursts.SpreadShare * CombatBursts.DeathDustM, "where the token stood");
        c.Sample(now + (CombatBursts.DeathPuffs - 1) * CombatBursts.DeathGapS + CombatBursts.DustStyle.LifeS + 0.01);
        Assert.AreEqual(0, c.Dust.Busy); Assert.IsFalse(c.enabled);
    }

    [Test]
    public void TheSameTimeDrawsTheSameFrame()
    {
        var c = Bursts(out _);
        c.Hit(Vector3.zero, guns: true, smallArms: true);
        Assert.IsTrue(c.March(new Vector3(300f, 0f, -200f)));
        List<object> Drawn()
        {
            var s = new List<object>();
            foreach (var pool in new[] { c.Blasts!, c.Dust!, c.MoveDust! })
                for (int i = 0; i < pool.Capacity; i++)
                {
                    var r = pool.Renderers[i];
                    s.Add(r.enabled); s.Add(r.transform.position); s.Add(r.transform.localScale); s.Add(pool.FrameOf(i)); s.Add(r.sharedMaterial.GetColor("_BaseColor"));
                }
            return s;
        }
        c.Sample(now + 0.4);
        Assert.AreEqual(1, c.MoveDust!.Busy, "fixture: movement dust is active in the snapshot");
        Assert.AreEqual(1, Shown(c.MoveDust), "fixture: the movement puff is drawn, not merely reserved");
        var a = Drawn();
        c.Sample(now + 0.2); c.Sample(now + 0.4);
        CollectionAssert.AreEqual(a, Drawn());
    }

    [Test]
    public void AFullPoolGivesUpItsOldestBurst()
    {
        var c = Bursts(out _);
        for (int i = 0; i <= CombatBursts.BlastSlots; i++) { now = 100.0 + i * 0.1; c.Hit(new Vector3(i * 100f, 0f, 0f), guns: true, smallArms: false); }
        Assert.AreEqual(CombatBursts.BlastSlots, c.Blasts!.Busy, "small and fixed");
        var feet = Enumerable.Range(0, c.Blasts.Capacity).Select(i => c.Blasts.Foot(i).x).ToList();
        CollectionAssert.DoesNotContain(feet, 0f, "the first burst gave up its slot");
        CollectionAssert.Contains(feet, CombatBursts.BlastSlots * 100f, "to the newest");
    }

    [Test]
    public void MarchingDustUsesAFixedPoolAndReusesItsOldestSlot()
    {
        var c = Bursts(out _);
        for (int i = 0; i <= CombatBursts.MoveDustSlots; i++)
        {
            now = 100.0 + i * 0.01;
            Assert.IsTrue(c.March(new Vector3(i * 100f, 0f, 0f)));
        }
        Assert.AreEqual(CombatBursts.MoveDustSlots, c.MoveDust!.Busy);
        var feet = Enumerable.Range(0, c.MoveDust.Capacity).Select(i => c.MoveDust.Foot(i).x).ToList();
        CollectionAssert.DoesNotContain(feet, 0f, "the oldest puff gave up its slot");
        CollectionAssert.Contains(feet, CombatBursts.MoveDustSlots * 100f);
        c.Sample(now + CombatBursts.MoveDustStyle.LifeS + 0.01);
        Assert.AreEqual(0, c.MoveDust.Busy);
        Assert.IsTrue(c.March(Vector3.zero), "a freed slot is reusable");
    }

    [Test]
    public void ATokenMoveEmitsThreeOrderedPuffsAndThresholdFramesAllocateNothing()
    {
        var c = Bursts(out _);
        for (int i = 0; i < CombatBursts.MoveDustSlots; i++) c.March(Vector3.zero);
        c.Sample(now + CombatBursts.MoveDustStyle.LifeS + 0.01);   // warm every slot and return to the idle state
        Assert.IsFalse(c.enabled, "fixture: the first measured emission wakes the idle effects");
        var token = new GameObject("moving token"); made.Add(token);
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.Effects = c; motion.GroundHome = new Vector3(100f, 0f, 200f); motion.enabled = false;
        motion.Begin(Vector3.right * 4000f);
        long betweenBefore = GC.GetAllocatedBytesForCurrentThread();
        motion.Tick(UnitTokenMotion.Duration / 8f);
        long betweenAllocated = GC.GetAllocatedBytesForCurrentThread() - betweenBefore;
        Assert.AreEqual(0, betweenAllocated, "a moving frame between emission thresholds allocates nothing");
        Assert.AreEqual(0, c.MoveDust!.Busy);
        for (int i = 0; i < UnitTokenMotion.MoveDustPuffs; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            motion.Tick(i == 0 ? UnitTokenMotion.Duration / 8f
                : UnitTokenMotion.Duration / (UnitTokenMotion.MoveDustPuffs + 1f));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, allocated, "emission frame " + i + " allocates nothing");
            Assert.AreEqual(i + 1, c.MoveDust!.Busy);
        }
        motion.Tick(UnitTokenMotion.Duration);
        Assert.AreEqual(UnitTokenMotion.MoveDustPuffs, c.MoveDust!.Busy, "arrival emits no extra puff");
        Assert.AreEqual(Vector3.zero, token.transform.position);
        var xs = Enumerable.Range(0, c.MoveDust.Capacity).Where(i => !double.IsNaN(c.MoveDust.StartOf(i)))
            .Select(i => c.MoveDust.Foot(i).x).OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(new[] { 1100f, 2100f, 3100f }, xs);
    }

    /// <summary>Per-frame garbage stutters on WebGL: sampling the effects, the pools full, allocates nothing.</summary>
    [Test]
    public void SamplingAllocatesNothing()
    {
        var c = Bursts(out _);
        for (int i = 0; i < 4; i++) { c.Hit(new Vector3(i * 500f, 0f, 0f), true, true); c.Destroyed(new Vector3(0f, 0f, i * 500f)); }
        for (int i = 0; i < 10; i++) c.Sample(now + i * 0.016);   // warm up
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) c.Sample(now + 0.2 + (i % 100) * 0.016);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "no allocation per frame");
    }

    [Test]
    public void HiddenItPlaysNothingAndDropsWhatPlayed()
    {
        var c = Bursts(out var parent);
        c.Destroyed(Vector3.zero); c.March(Vector3.zero);
        parent.SetActive(false);
        Assert.IsFalse(c.Hit(Vector3.zero, true, true)); Assert.IsFalse(c.Destroyed(Vector3.zero)); Assert.IsFalse(c.March(Vector3.zero));
        parent.SetActive(true);
        Assert.AreEqual(0, c.Dust!.Busy, "the units' toggle dropped the dust that played");
        Assert.AreEqual(0, c.MoveDust!.Busy, "hidden marching dust does not resume");
    }

    [Test]
    public void DestroyedItFreesEveryMeshAndMaterialItMade()
    {
        var c = Bursts(out var parent);
        var owned = c.Blasts!.Owned.Concat(c.Dust!.Owned).Concat(c.MoveDust!.Owned).ToList();
        Assert.Greater(owned.Count, 0);
        Assert.IsTrue(owned.All(o => o is Mesh || o is Material), "the frames and the slots' materials; the flipbooks are assets and stay");
        Object.DestroyImmediate(parent);
        Assert.IsTrue(owned.All(o => o == null), "every one destroyed with the effects");
        Assert.IsNotNull(Flipbook.Load(Flipbook.ExplosionResource), "the asset itself is untouched");
    }

    static Cause Line(string key, string attackers, string hex = "9,9", string defenderLoss = "0") => new Cause("map", hex, 0, "Report text is never read",
        key: key, units: new[] { new UnitRef(new Unit(71, "infantry", 0, 0)), new UnitRef(new Unit(72, "guns", 0, 0)),
            new UnitRef(new Unit(73, "infantry", 0, 0)) }, args: new[] { hex, "150", "0", defenderLoss, attackers });

    [Test]
    public void AnEngagementsHitIsItsHexItsGunsAndItsDefenders()
    {
        Func<string, bool> guns = type => type == "guns";
        var both = UnitAttackPresenter.HitOf(Line(WhyKeys.Engagement, "2", "-3,19"), guns)!.Value;
        Assert.AreEqual((-3, 19), (both.Q, both.R));
        Assert.IsTrue(both.Guns); Assert.IsTrue(both.SmallArms);
        CollectionAssert.AreEqual(new[] { 73 }, both.Defenders);
        var rifles = UnitAttackPresenter.HitOf(Line(WhyKeys.Engagement, "1"), guns)!.Value;
        Assert.IsFalse(rifles.Guns); Assert.IsTrue(rifles.SmallArms);
        CollectionAssert.AreEqual(new[] { 72, 73 }, rifles.Defenders);
        Assert.AreEqual(7, UnitAttackPresenter.HitOf(Line(WhyKeys.Engagement, "1", defenderLoss: "7"), guns)!.Value.DefenderLoss);
        Assert.AreEqual(0, UnitAttackPresenter.HitOf(Line(WhyKeys.Engagement, "1", defenderLoss: "lost"), guns)!.Value.DefenderLoss,
            "a malformed loss suppresses only the reaction, not the existing impact");
        Assert.IsNull(UnitAttackPresenter.HitOf(Line(WhyKeys.AttackOrdered, "2"), guns), "only an engagement hits");
        Assert.IsNull(UnitAttackPresenter.HitOf(Line(WhyKeys.Engagement, "0"), guns), "a malformed count");
        Assert.IsNull(UnitAttackPresenter.HitOf(Line(WhyKeys.Engagement, "2", "nine"), guns), "a malformed hex");
    }

    /// <summary>The real sim: an attack order plays one hit, on the hex it attacked, after the map is redrawn; End Turn's
    /// replay of it plays none.</summary>
    [Test]
    public void AnAttackOrderPlaysOneHitOnTheHexItAttacked()
    {
        var session = NewSession();
        var bridge = new SimBridge(session, Text);
        var hits = new List<CombatHit>();
        bool redrawn = false;
        bridge.Changed += () => redrawn = true;
        using var presenter = new UnitAttackPresenter(bridge, _ => true, h => { Assert.IsTrue(redrawn, "after Changed"); hits.Add(h); return true; }, (_, _) => true);
        UnitOrder? order = null; Unit? target = null;
        foreach (var u in session.Now.State.Units.Values.Where(u => u.Side == Movement.PlayerSide))
        {
            target = session.Now.State.Units.Values.FirstOrDefault(x => x.Side != Movement.PlayerSide && x.Side != "" && MapActions.Distance(x.Q, x.R, u.Q, u.R) == 1);
            if (target != null) { order = new UnitOrder(u.Id, Combat.Attack, target.Q, target.R); break; }
        }
        Assert.IsNotNull(order, "fixture: a player unit stands next to an enemy at the start");
        Assert.IsNull(bridge.Queue(order!));
        Assert.AreEqual(1, hits.Count, "one engagement, one hit");
        Assert.AreEqual((target!.Q, target.R), (hits[0].Q, hits[0].R));
        CollectionAssert.Contains(hits[0].Defenders, target.Id);
        bridge.EndTurn();
        Assert.AreEqual(1, hits.Count(h => (h.Q, h.R) == (target.Q, target.R) && h.Defenders.Contains(target.Id)), "End Turn's replay plays it no second time");
    }

    [Test]
    public void ADestroyedLineRaisesItsUnitsDust()
    {
        var destroyed = new List<int>();
        var lines = new[] { Line(WhyKeys.Engagement, "2"), new Cause("map", "73", 0, "Unit 73 was destroyed", key: WhyKeys.Destroyed,
            units: new[] { new UnitRef(new Unit(73, "infantry", 9, 9)) }) };
        var bridge = new SimBridge(NewSession(), Text,
            (session, orders) => new StepResult(session.State, session.Now.Why.Concat(lines).ToArray(), Array.Empty<Rejection>()));
        using var presenter = new UnitAttackPresenter(bridge, _ => true, _ => true, (id, _) => { destroyed.Add(id); return true; });
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { 73 }, destroyed);
        bridge.Replace(NewSession());
        CollectionAssert.AreEqual(new[] { 73 }, destroyed, "a load raises nothing");
    }

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
            File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    /// <summary>On the map: a hit lands on the defender's token, else on the hex's centre; a unit the rebuild dropped raises
    /// its dust where its token stood, once; a unit still drawn raises none.</summary>
    [Test]
    public void OnTheMapTheHitAndTheDustFindTheTokens()
    {
        var map = BuildMap();
        try
        {
            var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.AnimateMoves = true;
            layer.Show(C, s);
            Assert.IsNotNull(layer.Combat, "the layer makes its combat effects");
            var drawn = s.Units.Values.Where(u => u.Side == Movement.PlayerSide && layer.Tokens.Any(t => t.Token.StateUnit?.Id == u.Id)).Take(2).ToArray();
            Assert.AreEqual(2, drawn.Length, "fixture: two player units are drawn");
            Vector2 SpotOf(Unit u) { var spot = layer.Tokens.Single(t => t.Token.StateUnit?.Id == u.Id).Spot; return new Vector2((float)spot.X, (float)spot.Z); }

            var defender = drawn[0];
            Assert.IsFalse(layer.PlayDefenderHit(new CombatHit(defender.Q, defender.R, false, true,
                drawn.Select(u => u.Id).ToArray())), "the five-argument caller means no recorded defender loss");
            Assert.IsTrue(layer.PlayDefenderHit(new CombatHit(defender.Q, defender.R, false, true, 1,
                drawn.Select(u => u.Id).ToArray())), "all drawn defenders react to the aggregate loss");
            // They react when the volley's first puff lands (CombatBursts.ImpactS), not with the shot (e21's capture of #2614).
            foreach (var unit in drawn)
            {
                var motion = layer.GetComponentsInChildren<UnitTokenMotion>().Single(m => m.SimId == unit.Id);
                Assert.IsFalse(motion.Reacting, "no shake before the hit lands"); Assert.IsTrue(motion.HitPending);
                if (motion.HitFlash != null) Assert.IsFalse(motion.HitFlash.enabled, "no flash before the hit lands");
                motion.Tick(CombatBursts.VolleyS - 0.01f);
                Assert.IsFalse(motion.Reacting, "still waiting just before it");
                motion.Tick(0.02f);
                Assert.IsTrue(motion.Reacting, "the shake starts as it lands");
            }
            Assert.IsTrue(layer.PlayHit(new CombatHit(defender.Q, defender.R, true, false, new[] { defender.Id })));
            int slot = Enumerable.Range(0, layer.Combat!.Blasts!.Capacity).Single(i => !double.IsNaN(layer.Combat.Blasts.StartOf(i)));
            Assert.Less((XZ(layer.Combat.Blasts.Foot(slot)) - SpotOf(defender)).magnitude, 1f, "on the defender's token");
            Assert.AreEqual(now + CombatBursts.ShellFlightS, layer.Combat.Blasts.StartOf(slot), 1e-9, "timed to the attack");

            var hex = C.HexAt(defender.Q, defender.R)!;
            var centre = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
            layer.Combat.Blasts.Clear();
            Assert.IsTrue(layer.PlayHit(new CombatHit(hex.Q, hex.R, true, false, Array.Empty<int>())));
            slot = Enumerable.Range(0, layer.Combat.Blasts.Capacity).Single(i => !double.IsNaN(layer.Combat.Blasts.StartOf(i)));
            Assert.Less((XZ(layer.Combat.Blasts.Foot(slot)) - XZ(centre)).magnitude, 1f, "no defender drawn: the hex's centre");

            var lost = drawn[1]; var was = SpotOf(lost);
            s.Units.Remove(lost.Id);
            Assert.IsTrue(layer.Show(C, s), "fixture: the roster changed");
            Assert.IsFalse(layer.PlayDestroyed(defender.Id), "a unit still drawn raises no dust");
            Assert.IsTrue(layer.PlayDestroyed(lost.Id, CombatBursts.ShellFlightS));
            var dust = layer.Combat.Dust!;
            Assert.AreEqual(CombatBursts.DeathPuffs, dust.Busy);
            var starts = Enumerable.Range(0, dust.Capacity).Select(dust.StartOf).Where(t => !double.IsNaN(t)).ToList();
            Assert.AreEqual(now + CombatBursts.ShellFlightS, starts.Min(), 1e-9, "the dust rises with the shell's burst, not the shot");
            for (int i = 0; i < dust.Capacity; i++)
                if (!double.IsNaN(dust.StartOf(i)))
                    Assert.LessOrEqual((XZ(dust.Foot(i)) - was).magnitude, CombatBursts.SpreadShare * CombatBursts.DeathDustM + 1f, "where its token stood");
            Assert.IsFalse(layer.PlayDestroyed(lost.Id), "once");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
}
