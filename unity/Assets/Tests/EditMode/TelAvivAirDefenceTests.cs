#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Tel Aviv's air defence on the second bombing period's turns (SeaAirCollection.AirDefence.cs, TelAvivAirDefence).
/// Owner: "we will need an animation of the 101 attacking the dakotas"; "(a) Add the Dakotas to the 15 July 1948 turn, the
/// second bombing period, with 101 Squadron's fighters attacking them over Tel Aviv. Both sides are historically present that
/// turn."; then "do 1+2": (1) the S-199s attack with hits and no 101 shoot-down, an illustration; (2) one Dakota goes down to
/// anti-aircraft fire, the sourced 17 July 1948 event (JTA 19 July 1948, agent-read, a person's check pending).</summary>
public class TelAvivAirDefenceTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();
    static readonly Content C = ContentLoader.Load(Statecraft.Game.Paths.Content);
    static readonly DateTime July15 = new DateTime(1948, 7, 15), May15 = new DateTime(1948, 5, 15);

    [SetUp]
    public void Seam()
    {
        saved = SeaAirCollection.Request; asked.Clear();
        SeaAirCollection.Request = (id, d, done) => asked.Add((id, d, done));
    }

    [TearDown]
    public void Reset()
    {
        SeaAirCollection.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build(); return map;
    }

    void AnswerAll()
    {
        var pending = asked.ToList(); asked.Clear();
        foreach (var (id, _, done) in pending) done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far)));
    }

    static Bounds WorldBounds(GameObject go) =>
        go.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((a, x) => { a.Encapsulate(x); return a; });

    SeaAirCollection Drawn(MapScene map, DateTime date)
    {
        var layer = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
        return layer;
    }

    static DakotaLoop[] Loops(SeaAirCollection layer) => SeaAirCollection.DakotaKeys.Select(k => layer.transform.Find(k)!.GetComponent<DakotaLoop>()).ToArray();

    /// <summary>Places the Dakotas and then the air defence at the loop clock's <paramref name="t"/>, in the order they run.</summary>
    static void PlaceAll(DakotaLoop[] loops, TelAvivAirDefence defence, float t)
    {
        foreach (var loop in loops) loop.Place(t);
        defence.Place(t);
    }

    /// <summary>The fighters and the anti-aircraft fire only in the second bombing period, 8 to 17 July 1948: on the game's
    /// monthly turns the 15 July 1948 turn only, never the 15 May turn (the first period shows the Dakotas alone).</summary>
    [Test]
    public void TheAirDefenceIsWantedOnlyInTheSecondBombingPeriod()
    {
        Assert.AreEqual(new DateTime(1948, 7, 8), SeaAirCollection.DakotaSecondFrom);
        Assert.AreEqual(new DateTime(1948, 7, 17), SeaAirCollection.DakotaSecondTo);
        Assert.IsFalse(SeaAirCollection.AirDefenceWanted(new DateTime(1948, 7, 7)));
        Assert.IsTrue(SeaAirCollection.AirDefenceWanted(new DateTime(1948, 7, 8)));
        Assert.IsTrue(SeaAirCollection.AirDefenceWanted(new DateTime(1948, 7, 17)));
        Assert.IsFalse(SeaAirCollection.AirDefenceWanted(new DateTime(1948, 7, 18)));
        Assert.IsFalse(SeaAirCollection.AirDefenceWanted(May15), "no fighters on 15 May");
        Assert.IsFalse(SeaAirCollection.AirDefenceWanted(new DateTime(1948, 6, 3)), "not even on 3 June: the first period shows the Dakotas alone");
        var turns = new List<DateTime>();
        for (var d = C.Rules.Start; d < C.End; d = d.AddMonths(1)) turns.Add(d);
        CollectionAssert.AreEqual(new[] { July15 }, turns.Where(SeaAirCollection.AirDefenceWanted), "the 15 July 1948 turn only");
        foreach (var d in turns.Where(SeaAirCollection.AirDefenceWanted)) Assert.IsTrue(SeaAirCollection.DakotaWanted(d), "the fighters only with the Dakotas");
        Assert.IsFalse(SeaAirCollection.Wants(July15, C).Any(w => w.Key.Contains("s199")), "the fighters are code-built, not a streamed Collection craft");
    }

    /// <summary>On the 15 July 1948 turn: the two Dakotas and, beside them, the air defence with two Avia S-199s (the map's
    /// code-built UnitModels.AviaS199, not a Collection model: no request for it), each with its propeller, its shadow and
    /// Israel's flag; on 15 May the Dakotas fly alone: no air defence object, no fighters.</summary>
    [Test]
    public void TheJulyTurnShowsTheDakotasAndTwoS199sAndMayShowsNoFighters()
    {
        var map = OpenMap();
        var july = Drawn(map, July15);
        Assert.AreEqual(2, july.Placed.Count(p => p.Model == SeaAirCollection.Dakota), "both Dakotas on 15 July");
        var defence = july.AirDefence;
        Assert.IsNotNull(defence, "the air defence on 15 July");
        Assert.AreEqual(july.transform.Find(SeaAirCollection.AirDefenceKey), defence!.transform, "beside the Dakotas, under its key");
        Assert.AreEqual(2, defence.FighterPlanes.Length, "two fighters");
        foreach (var plane in defence.FighterPlanes)
        {
            StringAssert.Contains("S199", plane.GetComponent<MeshFilter>().sharedMesh.name, $"{plane.name} is the S-199 model");
            Assert.IsNotNull(plane.Find("Propeller"), $"{plane.name}'s propeller");
            var marks = defence.transform.Find(plane.name + SeaAirCollection.DakotaMarksSuffix);
            Assert.IsNotNull(marks?.Find(SeaAirCollection.DakotaFlagName), $"{plane.name}'s flag");
            Assert.IsNotNull(marks?.Find(SeaAirCollection.DakotaShadowName), $"{plane.name}'s shadow");
        }
        Assert.IsTrue(defence.Still, "edit mode holds the loop's start");
        Assert.AreEqual(1, july.Placed.Count(p => p.Model == SeaAirCollection.Spitfire), "the only Spitfire is el-Arish's: no Spitfire stands in for a fighter");

        var may = Drawn(map, May15);
        Assert.AreEqual(2, may.Placed.Count(p => p.Model == SeaAirCollection.Dakota), "both Dakotas on 15 May");
        Assert.IsNull(may.AirDefence, "no air defence on 15 May");
        Assert.IsNull(may.transform.Find(SeaAirCollection.AirDefenceKey), "no air defence object on 15 May");
        Assert.AreEqual(0, Object.FindObjectsByType<TelAvivAirDefence>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "no fighters anywhere on 15 May");
    }

    /// <summary>#2649 review: 15 May and 15 July want the same two Dakotas, so a direct change between them must still add or
    /// remove the air defence on the same layer (ShowAt reuses it), in both directions.</summary>
    [Test]
    public void ADirectChangeBetweenMayAndJulyAddsAndRemovesTheAirDefence()
    {
        var map = OpenMap();
        var may = Drawn(map, May15);
        Assert.IsNull(may.AirDefence, "no air defence on 15 May");
        var july = Drawn(map, July15);
        Assert.AreSame(may, july, "the same layer, reused");
        Assert.IsNotNull(july.AirDefence, "15 May -> 15 July adds the air defence");
        Assert.AreEqual(2, july.AirDefence!.FighterPlanes.Length, "with its two fighters");
        var back = Drawn(map, May15);
        Assert.IsNull(back.AirDefence, "15 July -> 15 May removes it");
        Assert.IsNull(back.transform.Find(SeaAirCollection.AirDefenceKey), "and its object");
    }

    /// <summary>The fighters stay in their sector: at every point of the loop each fighter, at every heading, bank and pitch,
    /// is inside the box the Dakotas fly through (SeaAirCollection.DakotaSweptHalf about their loop centre, which DakotaSpot
    /// keeps on the board), above the Dakotas, never in a Dakota's box, and inside the box the names keep clear of for it.</summary>
    [Test]
    public void TheFightersStayInTheirSectorClearOfTheDakotasAndTheLabels()
    {
        var map = OpenMap();
        var layer = Drawn(map, July15);
        var defence = layer.AirDefence!; var loops = Loops(layer);
        var centre = layer.Placed.First(p => p.Model == SeaAirCollection.Dakota).At;
        var half = SeaAirCollection.DakotaSweptHalf;
        var ground = PlacementLayer.CampGround(map);
        Assert.IsTrue(ground.Inside(centre.x - half.x, centre.z - half.y) && ground.Inside(centre.x + half.x, centre.z + half.y), "premise: the sector is on the board");
        var keepOf = (System.Collections.IDictionary)typeof(SeaAirCollection).GetField("keepOf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(layer)!;
        float period = SeaAirCollection.DakotaLoopSeconds;
        for (int i = 0; i < 480; i++)
        {
            float t = i * period / 480f;
            PlaceAll(loops, defence, t);
            var all = layer.KeepClear;   // moves the boxes
            var lead = WorldBounds(loops[0].gameObject); var wing = WorldBounds(loops[1].gameObject);
            foreach (var plane in defence.FighterPlanes)
            {
                var b = WorldBounds(plane.gameObject);
                Assert.IsTrue(b.min.x >= centre.x - half.x && b.max.x <= centre.x + half.x && b.min.z >= centre.z - half.y && b.max.z <= centre.z + half.y,
                    $"{plane.name} inside its sector at {t:0.00} s ({b.center - centre})");
                Assert.Greater(b.min.y, lead.min.y, $"{plane.name} above the Dakotas at {t:0.00} s");
                Assert.IsFalse(b.Intersects(lead), $"{plane.name} clear of the lead at {t:0.00} s");
                if (defence.WingShown && SeaAirCollection.WingState(t - SeaAirCollection.DakotaTrailSeconds, out _) == 0)
                    Assert.IsFalse(b.Intersects(wing), $"{plane.name} clear of the wing at {t:0.00} s");
                var keep = ((Bounds[])keepOf[plane.gameObject]!)[0];
                Assert.IsTrue(all.Any(x => ReferenceEquals(x, keepOf[plane.gameObject])), $"{plane.name}'s box is in the list the labels read");
                Assert.IsTrue(keep.Contains(b.min) && keep.Contains(b.max), $"{plane.name} inside its keep-clear box at {t:0.00} s");
            }
        }
    }

    /// <summary>The choreography, deterministic on the Dakotas' loop clock: each fighter dives in (from FighterTopM over the
    /// lead to FighterPassUpM at the middle of its attack), passes it and pulls away; it fires only in its firing share (the
    /// tracers and the gun's flash), and its hits show on the lead in its hit share; never outside its attack. The same
    /// time gives the same frame, a loop later too. The lead is never brought down by the fighters: drawn at every time.</summary>
    [Test]
    public void EachFighterDivesFiresHitsAndPullsAwayAndTheLeadFliesOn()
    {
        var map = OpenMap();
        var layer = Drawn(map, July15);
        var defence = layer.AirDefence!; var loops = Loops(layer);
        var leadRenderers = loops[0].GetComponentsInChildren<Renderer>(true);
        var tracers = defence.TracerRenderers; var hits = defence.HitRenderers;
        int perTracer = SeaAirCollection.FighterTracers, perHit = SeaAirCollection.FighterHits;
        for (int f = 0; f < 2; f++)
        {
            float start = SeaAirCollection.FighterAttackStartS[f], a = SeaAirCollection.FighterAttackS;
            float Up(float p) => SeaAirCollection.FighterOffset(f, start + p * a).y;
            Assert.AreEqual(SeaAirCollection.FighterTopM, Up(0f), 1f, "it starts high over the lead");
            Assert.AreEqual(SeaAirCollection.FighterPassUpM, Up(0.5f), 1f, "dives to its pass");
            Assert.AreEqual(SeaAirCollection.FighterTopM, Up(0.999f), 5f, "and pulls up away");
            float Gap(float p)
            {
                var o = SeaAirCollection.FighterOffset(f, start + p * a); var d = SeaAirCollection.DakotaLoopOffset(start + p * a);
                return new Vector2(o.x - d.x, o.z - d.y).magnitude;
            }
            Assert.Greater(Gap(0f), 5000f, "it starts well behind the lead");
            Assert.Less(Gap(0.5f), 4000f, "closes on it");
            Assert.Greater(Gap(0.999f), 5000f, "and passes it, pulling away ahead");
            var lead = SeaAirCollection.DakotaLoopHeading(start + a);
            var o1 = SeaAirCollection.FighterOffset(f, start + a); var d1 = SeaAirCollection.DakotaLoopOffset(start + a);
            Assert.Greater(Vector2.Dot(new Vector2(o1.x - d1.x, o1.z - d1.y), lead), 0f, "ahead of the lead at its attack's end");

            foreach (var (p, firing, hit) in new[] { (0.1f, false, false), (0.3f, true, false), (0.4f, true, true), (0.6f, false, true), (0.9f, false, false) })
            {
                float t = start + p * a;
                PlaceAll(loops, defence, t);
                Assert.AreEqual(firing, Enumerable.Range(f * perTracer, perTracer).Any(k => tracers[k].enabled), $"fighter {f} firing at {p:0.0} of its attack");
                Assert.AreEqual(hit, Enumerable.Range(f * perHit, perHit).Any(k => hits[k].enabled), $"fighter {f}'s hits on the lead at {p:0.0} of its attack");
            }
        }
        // Outside every attack: no tracer.
        PlaceAll(loops, defence, 30f);
        Assert.IsFalse(tracers.Any(r => r.enabled), "no fire between the attacks");

        // Deterministic: the same time, the same frame, also a loop later; the lead drawn throughout.
        foreach (var t in new[] { 0f, 1.3f, 33.3f, 61.7f, 97.2f })
        {
            PlaceAll(loops, defence, t);
            var first = defence.FighterPlanes.Select(p => (p.position, p.rotation)).ToArray();
            PlaceAll(loops, defence, t + SeaAirCollection.DakotaLoopSeconds);
            var again = defence.FighterPlanes.Select(p => (p.position, p.rotation)).ToArray();
            for (int i = 0; i < first.Length; i++)
            {
                Assert.Less((first[i].position - again[i].position).magnitude, 2f, $"fighter {i} at the same place a loop after {t} s");
                Assert.Greater(Quaternion.Dot(first[i].rotation, again[i].rotation), 0.9999f, $"fighter {i} the same way a loop after {t} s");
            }
        }
        for (int i = 0; i < 240; i++)
        {
            PlaceAll(loops, defence, i * 0.5f);
            Assert.IsTrue(leadRenderers.All(r => r.enabled), $"the lead is never shot down (at {i * 0.5f:0.0} s)");
        }
    }

    /// <summary>Option 2, the sourced 17 July shoot-down by anti-aircraft fire: flak bursts near the wing after its bombs, the
    /// last on it at WingHitS; it then trails smoke and falls, away from its loop and lower, and is gone with a blast on the
    /// ground; a fresh wing joins the loop at WingRejoinS. One kill a loop: the same again a loop later.</summary>
    [Test]
    public void AntiAircraftFireBringsTheWingDownOnceALoopAndAFreshWingJoins()
    {
        var map = OpenMap();
        var layer = Drawn(map, July15);
        var defence = layer.AirDefence!; var loops = Loops(layer);
        var centre = layer.Placed.First(p => p.Model == SeaAirCollection.Dakota).At;
        var ground = PlacementLayer.CampGround(map);
        float lag = SeaAirCollection.DakotaTrailSeconds, period = SeaAirCollection.DakotaLoopSeconds;
        foreach (var loopsOn in new[] { 0f, period })
        {
            float T(float u) => u + lag + loopsOn;   // the clock for the wing's own time u
            PlaceAll(loops, defence, T(SeaAirCollection.FlakAtS[0] - 1f));
            Assert.IsFalse(defence.FlakRenderers.Any(r => r.enabled), "no flak before the first burst");
            PlaceAll(loops, defence, T(SeaAirCollection.FlakAtS[0] + 0.5f));
            Assert.IsTrue(defence.FlakRenderers.Any(r => r.enabled), "flak bursting near the wing");
            Assert.IsTrue(defence.WingShown);
            Assert.IsFalse(defence.TrailRenderers.Any(r => r.enabled), "no smoke trail before the hit");
            // Falling: off its loop, lower, trailing smoke, still over the ground.
            float last = centre.y;
            for (int i = 1; i <= 10; i++)
            {
                float s = i * SeaAirCollection.WingFallS / 10.5f;
                PlaceAll(loops, defence, T(SeaAirCollection.WingHitS + s));
                Assert.IsTrue(defence.WingShown, $"drawn while it falls ({s:0.0} s)");
                var at = defence.WingAt;
                Assert.Less(at.y, last + 1f, $"lower and lower ({s:0.0} s)");
                last = at.y;
                Assert.Greater(at.y, ground.Y(at.x, at.z), $"over the ground until it comes down ({s:0.0} s)");
                Assert.IsTrue(defence.TrailRenderers.Any(r => r.enabled), $"trailing smoke ({s:0.0} s)");
                var onLoop = centre + new Vector3(SeaAirCollection.DakotaLoopOffset(SeaAirCollection.WingHitS + s).x, 0f, SeaAirCollection.DakotaLoopOffset(SeaAirCollection.WingHitS + s).y);
                if (i >= 4) Assert.Greater((at - onLoop).magnitude, 1000f, $"out of the loop ({s:0.0} s)");
            }
            Assert.Less(last, centre.y - 0.5f * (centre.y - ground.Y(centre.x, centre.z)), "most of the way down");
            PlaceAll(loops, defence, T(SeaAirCollection.WingHitS + SeaAirCollection.WingFallS + 0.3f));
            Assert.IsFalse(defence.WingShown, "gone once down");
            Assert.IsTrue(defence.CrashRenderer.enabled, "a blast where it comes down");
            Assert.IsTrue(loops[1].GetComponentsInChildren<Renderer>(true).All(r => !r.enabled), "none of it drawn");
            PlaceAll(loops, defence, T(SeaAirCollection.WingRejoinS - 1f));
            Assert.IsFalse(defence.WingShown, "still gone just before the fresh wing joins");
            PlaceAll(loops, defence, T(SeaAirCollection.WingRejoinS + 1f));
            Assert.IsTrue(defence.WingShown, "a fresh wing on the loop");
            Assert.IsTrue(loops[1].GetComponentsInChildren<Renderer>(true).All(r => r.enabled), "all of it drawn again");
            var back = WorldBounds(loops[1].gameObject).center; var path = SeaAirCollection.DakotaLoopOffset(SeaAirCollection.WingRejoinS + 1f);
            Assert.Less((new Vector2(back.x - centre.x, back.z - centre.z) - path).magnitude, 1f, "on its loop");
            Assert.IsTrue(ground.Water(back.x, back.z), "joining out over the sea");
        }
    }

    /// <summary>A frame of the whole raid allocates nothing: the Dakotas' loops and the air defence (fighters, gun, hits, flak,
    /// the falling wing and its trail), over every part of the loop, after a warm-up.</summary>
    [Test]
    public void PlacingTheAirDefenceAllocatesNothingPerFrame()
    {
        var map = OpenMap();
        var layer = Drawn(map, July15);
        var defence = layer.AirDefence!; var loops = Loops(layer);
        for (int i = 0; i < 400; i++) PlaceAll(loops, defence, i * 0.3f);   // warm up, through every state
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 2000; i++) PlaceAll(loops, defence, i * 0.061f);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "no allocation per frame");
    }
}
