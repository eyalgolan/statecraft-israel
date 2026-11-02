#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #669: Tal Heuberger's immigrant steamer off Haifa, fishing boats off Jaffa and Acre, the Altalena off Tel Aviv on
/// its card's turn and the C-54 at Lod during Operation Magic Carpet, streamed after the first frame (SeaAirCollection).</summary>
public class SeaAirCollectionTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();
    static readonly Content C = ContentLoader.Load(Statecraft.Game.Paths.Content);

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

    static GameObject Far(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    void AnswerAll()
    {
        var pending = asked.ToList(); asked.Clear();
        foreach (var (id, _, done) in pending) done(Far(id));
    }

    static Bounds WorldBounds(GameObject go) =>
        go.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((a, x) => { a.Encapsulate(x); return a; });

    static readonly string[] Models = { SeaAirCollection.Altalena, SeaAirCollection.FishingBoat, SeaAirCollection.Steamer, SeaAirCollection.C54, SeaAirCollection.Spitfire, SeaAirCollection.Dakota };

    /// <summary>The owner (#663): every model is used. All four of this lane's are in the streamed vehicle bundle and each is
    /// wanted on some turn of the game, on the game's own monthly turn dates.</summary>
    [Test]
    public void EveryModelOfTheLaneIsWantedOnSomeTurnFromTheStreamedVehicleBundle()
    {
        foreach (var id in Models) Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id), id + " is in the streamed vehicle bundle");
        var turns = new List<DateTime>();
        for (var d = C.Rules.Start; d < C.End; d = d.AddMonths(1)) turns.Add(d);
        Assert.AreEqual(new DateTime(1948, 5, 15), turns[0]);
        foreach (var id in Models)
            Assert.IsTrue(turns.Any(d => SeaAirCollection.Wants(d, C).Any(w => w.Model == id)), id + " is wanted on some monthly turn");
        Assert.AreEqual(1, turns.Count(d => SeaAirCollection.Wants(d, C).Any(w => w.Model == SeaAirCollection.Altalena)), "the Altalena on one turn");
    }

    /// <summary>Dated items only on their sourced dates. The Altalena on its card's window, 15-23 June 1948 (the turn that holds
    /// 20-22 June, the days the card's sources put the ship off Kfar Vitkin and Tel Aviv), off Tel Aviv. The C-54 at Lod from
    /// 1 July 1949 (Magic Carpet's main wave) to 24 September 1950 (its last flights into Lydda). The harbour craft always.</summary>
    [Test]
    public void TheAltalenaAndTheC54AreWantedOnlyOnTheirSourcedDates()
    {
        var card = C.Events.Single(e => e.Id == SeaAirCollection.AltalenaEvent);
        Assert.AreEqual(new DateTime(1948, 6, 15), card.From); Assert.AreEqual(new DateTime(1948, 6, 23), card.To);
        string[] Keys(DateTime d) => SeaAirCollection.Wants(d, C).Select(w => w.Key).ToArray();
        CollectionAssert.AreEqual(new[] { "haifa-steamer", "jaffa-fishing", "acre-fishing", "el-arish-spitfire", "tel-aviv-dakota" }, Keys(new DateTime(1948, 5, 15)));
        CollectionAssert.AreEqual(new[] { "haifa-steamer", "jaffa-fishing", "acre-fishing", "tel-aviv-altalena", "el-arish-spitfire" }, Keys(new DateTime(1948, 6, 15)));
        Assert.AreEqual("tel_aviv", SeaAirCollection.Wants(new DateTime(1948, 6, 15), C).Single(w => w.Model == SeaAirCollection.Altalena).Place);
        CollectionAssert.DoesNotContain(Keys(new DateTime(1948, 6, 14)), "tel-aviv-altalena");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1948, 6, 23)), "tel-aviv-altalena");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1948, 7, 15)), "tel-aviv-altalena");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1949, 6, 30)), "lod-c54");
        CollectionAssert.Contains(Keys(new DateTime(1949, 7, 1)), "lod-c54");
        CollectionAssert.Contains(Keys(new DateTime(1950, 9, 24)), "lod-c54");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1950, 9, 25)), "lod-c54");
    }

    /// <summary>#1932 (c): the Egyptian Spitfire from 15 May 1948 to the al-Arish Operational Diary's last Spitfire sortie,
    /// 2 January 1949 (rows R2, R4 and A3 of #1894's research note, an AI draft not yet checked by a person): the turns of
    /// 15 May to 15 December 1948, at el-Arish's place, which is off the board.</summary>
    [Test]
    public void TheSpitfireIsWantedOnlyOnItsSourcedDates()
    {
        bool Wanted(DateTime d) => SeaAirCollection.Wants(d, C).Any(w => w.Key == "el-arish-spitfire" && w.Model == SeaAirCollection.Spitfire && w.Place == "el_arish");
        Assert.IsFalse(Wanted(new DateTime(1948, 5, 14)));
        Assert.IsTrue(Wanted(new DateTime(1948, 5, 15)));
        Assert.IsTrue(Wanted(new DateTime(1948, 12, 15)));
        Assert.IsTrue(Wanted(new DateTime(1949, 1, 2)));
        Assert.IsFalse(Wanted(new DateTime(1949, 1, 3)));
        Assert.IsFalse(Wanted(new DateTime(1949, 1, 15)));
    }

    /// <summary>#1932 (c), "In flight / board edge only": el-Arish lies west of the board (so there is no hex or airfield to
    /// stand on), and the Spitfire is drawn near the board's western edge at el-Arish's latitude, 30 km in from the edge (owner,
    /// #2014: "Move it inward", so the player's camera at 40 and 150 km frames the whole aircraft), the whole aircraft over
    /// the board, nose east (wingspan north-south at its map size), 4,200 m above the highest ground under it, the unit
    /// tokens' aircraft altitude (UnitShowcase.AltitudeM, #2014): never on the ground. Only the far mesh is asked, once, and
    /// nothing draws before the stream answers.</summary>
    [Test]
    public void TheSpitfireFliesOverTheWesternBoardEdgeTowardElArishAndNeverStandsOnTheBoard()
    {
        var map = OpenMap();
        var elArish = map.Frame.ToWorld(SeaAirCollection.ElArishLon, SeaAirCollection.ElArishLat, 0);
        Assert.Less(elArish.x, 0f, "el-Arish is off the board, west of its edge");
        var layer = CollectionGeometryFixture.Craft(map, new DateTime(1948, 6, 15), Array.Empty<Placement>(), C)!;
        CollectionAssert.IsEmpty(layer.Placed, "nothing before the stream answers");
        Assert.AreEqual(1, asked.Count(a => a.id == SeaAirCollection.Spitfire && a.detail == CollectionDetail.Far), "the far mesh, once");
        AnswerAll();
        var (key, model, at) = layer.Placed.Single(p => p.Model == SeaAirCollection.Spitfire);
        float span = SeaAirCollection.SizeOf(SeaAirCollection.Spitfire);
        Assert.AreEqual(11.23f * Statecraft.Game.Map.Units.UnitShowcase.AircraftScale, span, 0.5f, "its 11.23 m wingspan at the unit tokens' aircraft scale");
        Assert.AreEqual(span, SeaAirCollection.RealLengthM(SeaAirCollection.Spitfire) * 420f, 0.5f, "the owner's 420x (#2014)");
        var b = WorldBounds(layer.transform.Find(key)!.gameObject);
        Assert.GreaterOrEqual(b.min.x, 0f, "the whole aircraft is over the board");
        Assert.AreEqual(30000f, at.x, 1f, "30 km in from the board's western edge (#2014)");
        Assert.AreEqual(elArish.z, at.z, 1f, "at el-Arish's latitude");
        Assert.AreEqual(span, b.size.z, 1f, "wingspan north-south at its size: the nose points east");
        Assert.Greater(b.size.z, b.size.x, "the fuselage lies east-west");
        // The model's nose is its mesh's -y: the propeller disc and the wings sit at that end, the tailplane and the tail
        // wheel at the other (vertex slices of the FBX along its fuselage, 31 October 2026).
        var go = layer.transform.Find(key)!;
        Assert.Greater(Vector3.Dot(go.TransformDirection(Vector3.down), Vector3.right), 0.9f, "the nose points east, toward the board");
        // The same from the geometry alone (#2014 review): the wings sit forward of the middle and the tailplane, a quarter
        // of their span, at the tail, so the wingtip vertex lies east of the aircraft's centre.
        var mesh = go.GetComponentInChildren<MeshFilter>();
        var tip = mesh.sharedMesh.vertices.Select(mesh.transform.TransformPoint).OrderBy(v => Mathf.Abs(v.z - b.center.z)).Last();
        Assert.Greater(tip.x, b.center.x + 0.1f * b.size.x, "the wingtips sit east of the centre: the nose points east (geometry)");
        // Its loop (owner, 2 November 2026, "Fly a slow loop") is centred here; edit mode holds the loop's start, due south of
        // the centre, heading east.
        Assert.AreEqual(at.x, b.center.x, 1f, "at the loop's start, east-west on the centre");
        Assert.AreEqual(at.z - SeaAirCollection.SpitfireLoopHalfWidthM, b.center.z, 1f, "at the loop's start, south of the centre");
        float floor = Statecraft.Game.Map.Units.UnitShowcase.LoopFloor(map, new Vector3(at.x, 0f, at.z), SeaAirCollection.SpitfireLoopHalfLengthM + span);
        Assert.AreEqual(floor + 4200f, at.y, 1f, "4,200 m above the highest ground under its loop, the unit tokens' aircraft altitude (#2014)");
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitShowcase.AltitudeM, at.y - floor, 1f, "UnitShowcase.AltitudeM, no number duplicated");
        Assert.Greater(b.min.y, floor, "in flight, never on the ground");
        Assert.AreEqual(1, layer.KeepClear.Count(k => k[0].Contains(b.min) && k[0].Contains(b.max)), "the labels keep clear of it");
    }

    /// <summary>#2014 merge with v0.3.2's residency: Rebuild registers the Spitfire in its model's originals before Fly's
    /// early continue, so when its window ends (the 15 January 1949 turn) the object is destroyed before its model's lease
    /// is released. With the continue first, the lease would be released while the Spitfire is still on the map.</summary>
    [Test]
    public void TheSpitfireIsGoneBeforeItsLeaseIsReleasedWhenItsWindowEnds()
    {
        var map = OpenMap();
        var go = new GameObject(SeaAirCollection.Name); go.transform.SetParent(map.Terrain.transform, false);
        var layer = go.AddComponent<SeaAirCollection>();
        // Private map field: the same construction reference ShowAt sets, before the first demand (as CollectionGeometryFixture).
        typeof(SeaAirCollection).GetField("map", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(layer, map);
        var released = new List<(string id, bool spitfireDrawn)>();
        layer.ConfigureResidency(new CollectionResidencyPolicy(1000000, 1000001, 1000000, 1000001, int.MaxValue, "TEST ONLY"),
            (id, detail, done) => { SeaAirCollection.Request(id, detail, done); return new ReleaseProbe(() => released.Add((id, layer.transform.Find("el-arish-spitfire") != null))); });

        SeaAirCollection.ShowAt(map, new DateTime(1948, 12, 15), Array.Empty<Placement>(), C);
        AnswerAll();
        Assert.IsNotNull(layer.transform.Find("el-arish-spitfire"), "the Spitfire is drawn on 15 December 1948");
        Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count, "labels keep clear of every drawn craft");

        SeaAirCollection.ShowAt(map, new DateTime(1949, 1, 15), Array.Empty<Placement>(), C);
        Assert.AreEqual(1, released.Count(r => r.id == SeaAirCollection.Spitfire), "the Spitfire's lease is released once its window ends");
        Assert.IsFalse(released.Single(r => r.id == SeaAirCollection.Spitfire).spitfireDrawn, "the Spitfire object is gone before its lease is released");
        Assert.IsNull(layer.transform.Find("el-arish-spitfire"));
        Assert.IsFalse(layer.Placed.Any(p => p.Model == SeaAirCollection.Spitfire));
        Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count, "labels keep clear of every drawn craft, and of nothing else");
    }

    sealed class ReleaseProbe : IDisposable
    {
        Action? done; public ReleaseProbe(Action done) { this.done = done; }
        public void Dispose() { var call = done; done = null; call?.Invoke(); }
    }

    /// <summary>The Spitfire's farthest point from its centre, seen from above, at any heading: half the diagonal of a square
    /// as wide as its wingspan (its fuselage is shorter than its wingspan).</summary>
    static float SpitfireReach => 0.5f * Mathf.Sqrt(2f) * SeaAirCollection.SizeOf(SeaAirCollection.Spitfire);

    /// <summary>The owner, 2 November 2026 (v0.3.3: "the egyptian plane in v0.3.3 is not moving"), ruling "Fly a slow loop":
    /// a slow repeating pass in from el-Arish over the western edge and back. The loop's period is tens of seconds and its
    /// size a few km (DESIGN VALUES). Half a period on, the aircraft is on the other side of the loop. At every point it stays
    /// inside the loop's box about EdgeSpot, and the whole aircraft stays over the board, clear of every unit token
    /// (the dated rings and the town clearings) and far from Lod, the board's one drawn airfield.</summary>
    [Test]
    public void TheSpitfireLoopsInsideItsPathAndTheBoardClearOfTheTokens()
    {
        var map = OpenMap();
        var centre = SeaAirCollection.EdgeSpot(map.Frame);
        var ground = PlacementLayer.CampGround(map);
        float period = SeaAirCollection.SpitfireLoopSeconds, a = SeaAirCollection.SpitfireLoopHalfLengthM, b = SeaAirCollection.SpitfireLoopHalfWidthM;
        Assert.That(period, Is.InRange(20f, 90f), "a period of tens of seconds");
        Assert.That(a, Is.InRange(2000f, 10000f), "a few km east-west");
        Assert.That(b, Is.InRange(1000f, a), "narrower north-south: a long loop, in from the west and back");
        var lod = map.Frame.ToWorld(SeaAirCollection.LodLon, SeaAirCollection.LodLat, 0);
        for (int i = 0; i < 120; i++)
        {
            float t = i * period / 120f;
            var at = SeaAirCollection.LoopOffset(t); var opposite = SeaAirCollection.LoopOffset(t + 0.5f * period);
            Assert.Greater((at - opposite).magnitude, 2f * b - 1f, $"at {t:0.0} s and half a period on, the other side of the loop");
            Assert.LessOrEqual(Mathf.Abs(at.x), a + 0.5f, $"within the loop east-west at {t:0.0} s");
            Assert.LessOrEqual(Mathf.Abs(at.y), b + 0.5f, $"within the loop north-south at {t:0.0} s");
            var p = centre + at;
            Assert.IsTrue(ground.Inside(p.x - SpitfireReach, p.y - SpitfireReach) && ground.Inside(p.x + SpitfireReach, p.y + SpitfireReach),
                $"the whole aircraft over the board at {t:0.0} s ({p})");
            Assert.IsFalse(ground.OnToken(p, SpitfireReach, SeaAirCollection.SpitfireFrom), $"over no unit token's ring at {t:0.0} s ({p})");
            foreach (var (c, r) in map.UnitClearings)
                Assert.Greater((p - c).magnitude, r + SpitfireReach, $"over no town's unit clearing at {t:0.0} s ({p})");
            Assert.Greater((p - new Vector2(lod.x, lod.z)).magnitude, 20000f, "far from Lod's airfield");
        }
        // The pass starts flying in from el-Arish: due south of the centre, heading east.
        Assert.AreEqual(0f, SeaAirCollection.LoopOffset(0f).x, 1f);
        Assert.AreEqual(-b, SeaAirCollection.LoopOffset(0f).y, 1f);
        Assert.Greater(SeaAirCollection.LoopHeading(0f).x, 0.999f, "heading east at the start");
    }

    /// <summary>The loop is closed: a period on, the aircraft is back at the same place with the same heading, at any time
    /// (also long into a session, where the clock is large).</summary>
    [Test]
    public void TheSpitfireLoopIsClosed()
    {
        float period = SeaAirCollection.SpitfireLoopSeconds;
        foreach (var t in new[] { 0f, 3.7f, 12.5f, 0.5f * period, period - 0.01f, 1234.5f, 7200.25f })
        {
            Assert.Less((SeaAirCollection.LoopOffset(t) - SeaAirCollection.LoopOffset(t + period)).magnitude, 1f, $"the same place a period after {t} s");
            Assert.Less((SeaAirCollection.LoopHeading(t) - SeaAirCollection.LoopHeading(t + period)).magnitude, 1e-3f, $"the same heading a period after {t} s");
        }
    }

    /// <summary>The drawn aircraft flies the loop nose first: at every sampled time its centre is on the path about its
    /// recorded loop centre, level at that one altitude, its nose (the mesh's -y, as the in-flight test reads it) along the
    /// path's tangent, inside the box the labels keep clear of, and a period later at the same place. The heading is the
    /// path's tangent (a short chord's direction).</summary>
    [Test]
    public void TheSpitfireFacesAlongItsPath()
    {
        float period = SeaAirCollection.SpitfireLoopSeconds;
        for (int i = 0; i < 120; i++)
        {
            float t = i * period / 120f;
            var heading = SeaAirCollection.LoopHeading(t);
            Assert.AreEqual(1f, heading.magnitude, 1e-4f, "a unit heading");
            var chord = SeaAirCollection.LoopOffset(t + 0.01f) - SeaAirCollection.LoopOffset(t - 0.01f);
            Assert.Greater(Vector2.Dot(heading, chord.normalized), 0.999f, $"the heading is the path's tangent at {t:0.0} s");
        }

        var map = OpenMap();
        var layer = CollectionGeometryFixture.Craft(map, new DateTime(1948, 6, 15), Array.Empty<Placement>(), C)!; AnswerAll();
        var plane = layer.transform.Find("el-arish-spitfire")!;
        var loop = plane.GetComponent<SpitfireLoop>();
        Assert.IsNotNull(loop, "the drawn Spitfire carries its loop");
        Assert.IsTrue(loop.Still, "edit mode holds the loop's start, so editor renders repeat");
        var centre = layer.Placed.Single(p => p.Model == SeaAirCollection.Spitfire).At;
        var keep = layer.KeepClear.Single(k => k[0].Contains(centre))[0];
        for (int i = 0; i < 48; i++)
        {
            float t = i * period / 48f;
            loop.Place(t);
            var b = WorldBounds(plane.gameObject); var at = SeaAirCollection.LoopOffset(t);
            Assert.AreEqual(centre.x + at.x, b.center.x, 1f, $"on the path east-west at {t:0.0} s");
            Assert.AreEqual(centre.z + at.y, b.center.z, 1f, $"on the path north-south at {t:0.0} s");
            Assert.AreEqual(centre.y, b.center.y, 1f, $"level, at the loop's one altitude, at {t:0.0} s");
            var nose = plane.TransformDirection(Vector3.down);
            Assert.AreEqual(0f, nose.y, 1e-3f, "level flight");
            Assert.Greater(Vector2.Dot(new Vector2(nose.x, nose.z).normalized, SeaAirCollection.LoopHeading(t)), 0.999f, $"nose along the path at {t:0.0} s");
            Assert.IsTrue(keep.Contains(b.min) && keep.Contains(b.max), $"inside the box the labels keep clear of at {t:0.0} s");
            var here = b.center;
            loop.Place(t + period);
            Assert.Less((WorldBounds(plane.gameObject).center - here).magnitude, 1f, $"back at the same place a period after {t:0.0} s");
        }
    }

    /// <summary>Review k2 C1: in play the aircraft is drawn where the clock puts it from its first frame, not at the loop's
    /// start until its first Update (a component added mid-frame gets no Update that frame, so the frame it is built in would
    /// draw it up to 6 km from where it flies). Screenshots and edit mode still hold the loop's start. An EditMode test has
    /// no frame clock, so it drives SpitfireLoop.Clock.</summary>
    [Test]
    public void TheSpitfireIsDrawnWhereTheClockPutsItFromItsFirstFrame()
    {
        var clock = SpitfireLoop.Clock;
        var flying = new GameObject("flying"); var held = new GameObject("held");
        try
        {
            const float now = 12.5f;
            SpitfireLoop.Clock = () => now;
            var centre = new Vector3(30000f, 4200f, 1000f);
            flying.AddComponent<SpitfireLoop>().Configure(centre, Quaternion.identity, Vector3.zero, still: false);
            held.AddComponent<SpitfireLoop>().Configure(centre, Quaternion.identity, Vector3.zero, still: true);
            var there = SeaAirCollection.LoopOffset(now); var start = SeaAirCollection.LoopOffset(0f);
            Assert.Greater((there - start).magnitude, 1000f, "the test's time is far round the loop from its start");
            Assert.Less((flying.transform.position - (centre + new Vector3(there.x, 0f, there.y))).magnitude, 1f,
                "in play, where the clock puts it, before any Update");
            Assert.Less((held.transform.position - (centre + new Vector3(start.x, 0f, start.y))).magnitude, 1f,
                "held (edit mode, screenshots): at the loop's start whatever the clock");
        }
        finally
        {
            SpitfireLoop.Clock = clock;
            Object.DestroyImmediate(flying); Object.DestroyImmediate(held);
        }
    }

    /// <summary>The loop respects the Spitfire's dates (#1932: 15 May 1948 to 2 January 1949): outside them no aircraft and
    /// no loop is drawn; inside them exactly one looping aircraft.</summary>
    [Test]
    public void OutsideItsDatesThereIsNoSpitfireAndInsideThemOneLoopingOne()
    {
        var map = OpenMap();
        foreach (var (date, flying) in new[]
        {
            (new DateTime(1948, 5, 14), false), (new DateTime(1948, 5, 15), true), (new DateTime(1949, 1, 2), true),
            (new DateTime(1949, 1, 3), false), (new DateTime(1949, 8, 15), false),
        })
        {
            var layer = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
            Assert.AreEqual(flying ? 1 : 0, layer.Placed.Count(p => p.Model == SeaAirCollection.Spitfire), $"the Spitfire on {date:d MMM yyyy}");
            Assert.AreEqual(flying ? 1 : 0, Object.FindObjectsByType<SpitfireLoop>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                $"the loops on {date:d MMM yyyy}");
            Assert.AreEqual(flying, layer.transform.Find("el-arish-spitfire") != null, $"the aircraft on {date:d MMM yyyy}");
        }
    }

    // ---- Tal Heuberger's Egyptian Dakota (owner, 2 November 2026: "Over Tel Aviv, 15 May-11 Jun (Recommended)") ----

    /// <summary>The first bombing period, 15 May to 11 June 1948 (he.wiki rev 43856594, agent-read): on the game's monthly turns
    /// the 15 May turn only, at Tel Aviv's place.</summary>
    [Test]
    public void TheDakotaIsWantedOnlyInTheFirstBombingPeriod()
    {
        bool Wanted(DateTime d) => SeaAirCollection.Wants(d, C).Any(w => w.Key == SeaAirCollection.DakotaKey && w.Model == SeaAirCollection.Dakota && w.Place == "tel_aviv");
        Assert.IsFalse(Wanted(new DateTime(1948, 5, 14)));
        Assert.IsTrue(Wanted(new DateTime(1948, 5, 15)));
        Assert.IsTrue(Wanted(new DateTime(1948, 6, 3)), "the 3 June raid, when two Egyptian C-47s were shot down over Tel Aviv");
        Assert.IsTrue(Wanted(new DateTime(1948, 6, 11)));
        Assert.IsFalse(Wanted(new DateTime(1948, 6, 12)), "the first truce");
        Assert.IsFalse(Wanted(new DateTime(1948, 6, 15)));
        var turns = new List<DateTime>();
        for (var d = C.Rules.Start; d < C.End; d = d.AddMonths(1)) turns.Add(d);
        CollectionAssert.AreEqual(new[] { new DateTime(1948, 5, 15) }, turns.Where(Wanted), "the 15 May 1948 turn only");
        Assert.AreEqual(29.11f, SeaAirCollection.RealLengthM(SeaAirCollection.Dakota), 0.01f, "95 ft 6 in, en.wiki Douglas C-47 Skytrain");
        Assert.AreEqual(29.11f * Statecraft.Game.Map.Units.UnitShowcase.AircraftScale, SeaAirCollection.SizeOf(SeaAirCollection.Dakota), 5f,
            "at the unit tokens' aircraft scale, as the Spitfire (#2014)");
    }

    (SeaAirCollection layer, Transform plane, DakotaLoop loop, Vector3 centre) DrawnDakota(MapScene map)
    {
        var layer = CollectionGeometryFixture.Craft(map, new DateTime(1948, 5, 15), Array.Empty<Placement>(), C)!;
        Assert.AreEqual(1, asked.Count(a => a.id == SeaAirCollection.Dakota && a.detail == CollectionDetail.Far), "the far mesh, once");
        AnswerAll();
        var plane = layer.transform.Find(SeaAirCollection.DakotaKey)!;
        Assert.IsNotNull(plane, "the Dakota is drawn on 15 May 1948");
        return (layer, plane, plane.GetComponent<DakotaLoop>(), layer.Placed.Single(p => p.Model == SeaAirCollection.Dakota).At);
    }

    /// <summary>"A slow loop over Tel Aviv": the loop's centre is the nearest to Tel Aviv whose whole swept box is over the
    /// board and clear of every unit token present in the window (rings and town clearings: Kiryati's token stands on Tel Aviv)
    /// and of every map label's point and town footprint, so the aircraft hides no token and no name; it is just off the city.
    /// The aircraft is drawn there at its map size, nose east, at the unit tokens' aircraft altitude, and the labels keep clear of
    /// its whole loop.</summary>
    [Test]
    public void TheDakotaLoopsJustOffTelAvivInsideTheBoardClearOfTokensAndLabels()
    {
        var map = OpenMap();
        var (layer, plane, loop, at) = DrawnDakota(map);
        var tel = map.Settlements.Single(s => s.Id == "tel_aviv");
        var t3 = map.Frame.ToWorld(tel.Lon, tel.Lat, 0); var telAviv = new Vector2(t3.x, t3.z);
        var centre = new Vector2(at.x, at.z);
        var half = SeaAirCollection.DakotaSweptHalf;
        float fromCity = (centre - telAviv).magnitude;
        TestContext.WriteLine($"Dakota loop centre {fromCity:0} m from Tel Aviv's point; nearest pass {Mathf.Max(Mathf.Abs(centre.x - telAviv.x) - SeaAirCollection.SpitfireLoopHalfLengthM, 0f):0} m east-west");
        Assert.Less(fromCity, 30000f, "by Tel Aviv");
        var ground = PlacementLayer.CampGround(map);
        Assert.IsTrue(ground.Inside(centre.x - half.x, centre.y - half.y) && ground.Inside(centre.x + half.x, centre.y + half.y), "its whole loop over the board");
        foreach (var kv in map.UnitSpots)
        {
            if (Statecraft.Game.Map.Units.UnitData.Day(kv.Key.From) > SeaAirCollection.DakotaTo || Statecraft.Game.Map.Units.UnitData.Day(kv.Key.To) <= SeaAirCollection.DakotaFrom) continue;
            var c = new Vector2((float)kv.Value.X, (float)kv.Value.Z);
            float dx = Mathf.Max(Mathf.Abs(c.x - centre.x) - half.x, 0f), dz = Mathf.Max(Mathf.Abs(c.y - centre.y) - half.y, 0f);
            float r = kv.Value.InTown ? map.UnitClearings.Where(x => (x.centre - c).sqrMagnitude < 1f).Select(x => x.radius).DefaultIfEmpty(0f).Max()
                : map.UnitTokens.Where(x => (x.centre - c).sqrMagnitude < 1f).Select(x => x.radius).DefaultIfEmpty(0f).Max();
            Assert.Greater(r, 0f, $"{kv.Key.Place}'s token has its ring or clearing");
            Assert.Greater(Mathf.Sqrt(dx * dx + dz * dz), r, $"clear of the token at {kv.Key.Place} ({kv.Key.From}-{kv.Key.To})");
        }
        foreach (var s in map.Settlements)
        {
            var p = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            Assert.IsFalse(Mathf.Abs(p.x - centre.x) <= half.x && Mathf.Abs(p.z - centre.y) <= half.y, $"{s.Id}'s name is not under its loop");
        }
        foreach (var (fc, fr, id) in map.KitFootprints)
        {
            float dx = Mathf.Max(Mathf.Abs(fc.x - centre.x) - half.x, 0f), dz = Mathf.Max(Mathf.Abs(fc.y - centre.y) - half.y, 0f);
            Assert.Greater(dx * dx + dz * dz, fr * fr, $"clear of {id}'s kit");
        }
        var b = WorldBounds(plane.gameObject);
        Assert.AreEqual(SeaAirCollection.SizeOf(SeaAirCollection.Dakota), b.size.z, 1f, "wingspan north-south at its size: the nose points east");
        Assert.Greater(Vector3.Dot(plane.TransformDirection(Vector3.down), Vector3.right), 0.9f, "the nose (the mesh's -y) points east at the loop's start");
        float floor = Statecraft.Game.Map.Units.UnitShowcase.LoopFloor(map, new Vector3(at.x, 0f, at.z), SeaAirCollection.SpitfireLoopHalfLengthM + SeaAirCollection.SizeOf(SeaAirCollection.Dakota));
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitShowcase.AltitudeM, at.y - floor, 1f, "the unit tokens' aircraft altitude, as the Spitfire");
        Assert.Greater(b.min.y, floor, "in flight, never on the ground");
        Assert.IsTrue(loop.Still, "edit mode holds the loop's start");
        var keep = layer.KeepClear.Single(k => k[0].Contains(at))[0];
        for (int i = 0; i < 60; i++)
        {
            loop.Place(i * SeaAirCollection.DakotaLoopSeconds / 60f);
            var pb = WorldBounds(plane.gameObject);
            Assert.IsTrue(keep.Contains(pb.min) && keep.Contains(pb.max), $"inside the box the labels keep clear of at step {i}");
            Assert.IsTrue(Mathf.Abs(pb.center.x - centre.x) <= half.x && Mathf.Abs(pb.center.z - centre.y) <= half.y, $"inside its swept box at step {i}");
        }
    }

    /// <summary>The loop is closed (a period on, the same place and heading, also long into a session), the aircraft flies it
    /// nose first, level, and the per-frame step allocates nothing.</summary>
    [Test]
    public void TheDakotaLoopIsClosedNoseFirstAndAllocatesNothingPerFrame()
    {
        float period = SeaAirCollection.DakotaLoopSeconds;
        Assert.That(period, Is.InRange(SeaAirCollection.SpitfireLoopSeconds, 120f), "a slow loop: no faster round than the Spitfire's");
        foreach (var t in new[] { 0f, 3.7f, 12.5f, 0.5f * period, period - 0.01f, 1234.5f, 7200.25f })
        {
            Assert.Less((SeaAirCollection.DakotaLoopOffset(t) - SeaAirCollection.DakotaLoopOffset(t + period)).magnitude, 1f, $"the same place a period after {t} s");
            Assert.Less((SeaAirCollection.DakotaLoopHeading(t) - SeaAirCollection.DakotaLoopHeading(t + period)).magnitude, 1e-3f, $"the same heading a period after {t} s");
            var chord = SeaAirCollection.DakotaLoopOffset(t + 0.2f) - SeaAirCollection.DakotaLoopOffset(t - 0.2f);
            Assert.Greater(Vector2.Dot(SeaAirCollection.DakotaLoopHeading(t), chord.normalized), 0.999f, $"the heading is the path's tangent at {t} s");
        }
        var map = OpenMap();
        var (_, plane, loop, centre) = DrawnDakota(map);
        for (int i = 0; i < 24; i++)
        {
            float t = i * period / 24f;
            loop.Place(t);
            var nose = plane.TransformDirection(Vector3.down);
            Assert.AreEqual(0f, nose.y, 1e-3f, "level flight");
            Assert.Greater(Vector2.Dot(new Vector2(nose.x, nose.z).normalized, SeaAirCollection.DakotaLoopHeading(t)), 0.999f, $"nose along the path at {t:0.0} s");
            var b = WorldBounds(plane.gameObject); var at = SeaAirCollection.DakotaLoopOffset(t);
            Assert.AreEqual(centre.x + at.x, b.center.x, 1f); Assert.AreEqual(centre.z + at.y, b.center.z, 1f); Assert.AreEqual(centre.y, b.center.y, 1f);
        }
        for (int i = 0; i < 10; i++) loop.Place(i);   // warm up
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) loop.Place(i * 0.016f);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "no allocation per frame");
    }

    /// <summary>Egypt's flag as the owner ruled for the Spitfire on #2260 ("Smaller flag, no pole"): the cloth alone, in Egypt's
    /// sourced colours, half the tokens' flag size, floating over the tail and clear above the aircraft at every point of its
    /// loop, turned to the camera, and gone with the aircraft.</summary>
    [Test]
    public void TheDakotaFliesEgyptsFlagOverItsTailWithNoPole()
    {
        var map = OpenMap();
        var (layer, plane, loop, _) = DrawnDakota(map);
        var flag = loop.Flag;
        Assert.IsNotNull(flag, "a flag over the aircraft");
        Assert.IsNotNull(flag!.GetComponent<Statecraft.Game.Map.Units.UnitFlagFacing>(), "turned to the camera, as the tokens' flags");
        var mesh = flag.GetComponent<MeshFilter>().sharedMesh;
        var colours = Statecraft.Game.Map.Units.UnitFlags.Colours(Statecraft.Game.Map.Units.UnitFlags.Egypt);
        Assert.AreEqual(colours.Count, mesh.subMeshCount, "the cloth's colours only: no pole submesh");
        var mats = flag.GetComponent<MeshRenderer>().sharedMaterials;
        Assert.AreEqual(colours.Count, mats.Length, "one material a cloth colour");
        for (int i = 0; i < mats.Length; i++)
            Assert.Less(Vector4.Distance(colours[i], mats[i].color), 1e-4f, $"Egypt's sourced colour {i}");
        Assert.AreEqual(SeaAirCollection.DakotaFlagScale * Statecraft.Game.Map.Units.UnitBadge.FigureHeightM * Statecraft.Game.Map.Units.UnitShowcase.FigureScale,
            flag.lossyScale.y, 1f, "half the tokens' flag size");
        Assert.Less(mesh.bounds.size.y, 0.6f * Statecraft.Game.Map.Units.UnitFlags.PoleHeight, "no pole: the cloth alone");
        for (int i = 0; i < 24; i++)
        {
            float t = i * SeaAirCollection.DakotaLoopSeconds / 24f;
            loop.Place(t);
            var b = WorldBounds(plane.gameObject); var fb = flag.GetComponent<MeshRenderer>().bounds;
            Assert.Greater(fb.min.y, b.max.y, $"the flag floats clear above the aircraft at {t:0.0} s");
            var back = -new Vector3(SeaAirCollection.DakotaLoopHeading(t).x, 0f, SeaAirCollection.DakotaLoopHeading(t).y);
            Assert.Greater(Vector3.Dot(flag.position - b.center, back), 0.3f * b.extents.x, $"over the tail, behind the centre, at {t:0.0} s");
        }
        Assert.AreEqual(1, layer.KeepClear.Count(k => k[0].Contains(flag.GetComponent<MeshRenderer>().bounds.center)), "the labels keep clear of the flag too");
        CollectionGeometryFixture.Craft(map, new DateTime(1948, 6, 15), Array.Empty<Placement>(), C);
        AnswerAll();
        Assert.IsNull(layer.transform.Find(SeaAirCollection.DakotaKey), "no Dakota after its window");
        Assert.IsNull(layer.transform.Find(SeaAirCollection.DakotaKey + SeaAirCollection.DakotaMarksSuffix), "its flag goes with it");
        Assert.AreEqual(0, Object.FindObjectsByType<DakotaLoop>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
    }

    /// <summary>Nothing is drawn until the stream answers, and only the far mesh, once per model. Then every craft is drawn,
    /// each boat on the open sea (the heightmap's 0 m), its hull passing SeaCraft's own clearance (open sea round it, clear of
    /// the harbour works and of SeaCraft's boats and each other) and at least SeaCraft.OffshoreNearM out from its shore point
    /// (past the surf), its waterline on the surface; the C-54 on land by the airport, its wheels on the ground, off every kit
    /// and with no kit tree drawn under it. Every
    /// craft keeps its prefab's rotation under a quarter turn (upright, as #979), lies with its longer side east-west, and is
    /// drawn at its size. Rebuilding gives the same spots.</summary>
    [Test]
    public void TheCraftStreamInAndFloatUprightClearOfTheSurfTheHarbourWorksAndTheOtherBoats()
    {
        var map = OpenMap();
        var ground = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        var kitBoats = map.KitStats!.Crafts; var works = map.KitStats.HarbourWorks;
        Assert.Greater(kitBoats.Count, 0, "SeaCraft's boats are there to keep clear of");
        Assert.Greater(works.Count, 0, "the harbour works are there to keep clear of");
        bool first = true;
        foreach (var date in new[] { new DateTime(1948, 6, 15), new DateTime(1949, 8, 15) })
        {
            var layer = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!;
            if (first) CollectionAssert.IsEmpty(layer.Placed, "nothing before the stream answers");
            first = false;
            Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Far), "the far mesh only");
            Assert.AreEqual(asked.Count, asked.Select(a => a.id).Distinct().Count(), "each model once");
            AnswerAll();
            CollectionAssert.IsEmpty(layer.Missing, "every craft found a spot on " + date.ToString("d MMM yyyy"));
            Assert.AreEqual(date.Year == 1948 ? 5 : 4, layer.Placed.Count, "three boats, then the Altalena and the Spitfire on 15 June 1948, the C-54 on 15 August 1949");
            var spots = layer.Placed.Select(p => p.At).ToArray();
            CollectionGeometryFixture.Craft(map, date.AddMonths(1), Array.Empty<Placement>(), C); AnswerAll();
            CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C); AnswerAll();
            CollectionAssert.AreEqual(spots, layer.Placed.Select(p => p.At).ToArray(), "the same spots again");
            var boats = layer.Placed.Where(p => p.Model != SeaAirCollection.C54).ToList();
            foreach (var (key, model, at) in layer.Placed)
            {
                if (model == SeaAirCollection.Spitfire) continue;   // in flight: its own test
                var child = layer.transform.Find(key)!; var prefab = Far(model);
                var turned = new[] { 0f, 90f }.Min(y => Quaternion.Angle(layer.transform.rotation * Quaternion.Euler(0f, y, 0f) * prefab.transform.localRotation, child.rotation));
                Assert.Less(turned, 0.01f, key + " keeps its prefab's rotation (upright) under a yaw of 0 or 90");
                var b = WorldBounds(child.gameObject);
                Assert.GreaterOrEqual(b.size.x, b.size.z, key + " lies east-west, side-on to the camera");
                Assert.AreEqual(SeaAirCollection.SizeOf(model), b.size.x, 1f, key + " is drawn at its size");
                Assert.AreEqual(at.y, b.min.y, 1f, key + " stands on the surface under it");
                Assert.AreEqual(ground.Y(at.x, at.z), at.y, 0.5f);
                var p = new Vector2(at.x, at.z);
                if (model == SeaAirCollection.C54)
                {
                    Assert.IsFalse(ground.Water(p.x, p.y), "the C-54 stands on land");
                    var lod = map.Frame.ToWorld(SeaAirCollection.LodLon, SeaAirCollection.LodLat, 0);
                    Assert.Less((p - new Vector2(lod.x, lod.z)).magnitude, 3f * SeaAirCollection.SizeOf(model), "by Lod Airport");
                    foreach (var f in map.KitFootprints)
                        Assert.GreaterOrEqual((p - f.centre).magnitude, f.radius + 0.5f * SeaAirCollection.SizeOf(model) - 1f, $"the C-54 on {f.id}'s kit");
                    var veg = map.GetComponentInChildren<VegetationLayer>(true);
                    Assert.IsNotNull(veg);
                    foreach (var (kind, m) in veg.Drawable())
                    {
                        var t = (Vector3)m.GetColumn(3);
                        Assert.GreaterOrEqual((new Vector2(t.x, t.z) - p).magnitude, 0.5f * SeaAirCollection.SizeOf(model) + VegetationLayer.CrownRadius(kind, m) - 1f,
                            "no kit tree drawn under the C-54");
                    }
                    continue;
                }
                Assert.LessOrEqual(Mathf.Abs(ground.ElevM(p.x, p.y)), MapBuilder.SeaToleranceM + 0.5f, key + " lies on the open sea");
                float len = SeaAirCollection.SizeOf(model);
                var others = kitBoats.Concat(boats.Where(o => o.Key != key)
                    .Select(o => new SeaCraft.Craft(SeaCraft.Kind.Steamer, new Vector2(o.At.x, o.At.z), 0f, SeaAirCollection.SizeOf(o.Model), ""))).ToList();
                Assert.IsTrue(SeaCraft.Clear(new SeaCraft.Craft(SeaCraft.Kind.Steamer, p, 0f, len, ""), ground, works.ToList(), others, 0.5f * len),
                    key + " passes SeaCraft's clearance: open sea round it, off the harbour works and every other boat");
                var w = SeaAirCollection.Wants(date, C).Single(x => x.Key == key);
                var coast = SeaCraft.Coasts.Single(c => c.place == w.Place);
                var shoreAt = map.Frame.ToWorld(coast.lon, coast.lat, 0);
                var shore = SeaCraft.FirstWater(new Vector2(shoreAt.x, shoreAt.z), coast.seaward, ground, 20000f)!.Value;
                Assert.GreaterOrEqual(Vector2.Dot(p - shore, coast.seaward), SeaCraft.OffshoreNearM * KitScale.Landmarks / 60f - 1f, key + " lies past the surf");
            }
        }
    }

    /// <summary>The C-54 keeps off the player's placements: a hex placed at the airport moves it to the next free ground.</summary>
    [Test]
    public void TheC54KeepsOffAPlacedHex()
    {
        var map = OpenMap();
        var date = new DateTime(1949, 8, 15);
        var layer = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
        var first = layer.Placed.Single(p => p.Model == SeaAirCollection.C54).At;
        var hex = C.Hexes.OrderBy(h => { var w = map.Frame.ToWorld(h.Lon, h.Lat, 0); return (new Vector2(w.x, w.z) - new Vector2(first.x, first.z)).sqrMagnitude; }).First();
        var placed = new[] { new Placement(MapActions.TransitCamp, hex.Q, hex.R, date) };
        CollectionGeometryFixture.Craft(map, date, placed, C); AnswerAll();
        var moved = layer.Placed.Single(p => p.Model == SeaAirCollection.C54).At;
        var c = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        Assert.GreaterOrEqual((new Vector2(moved.x, moved.z) - new Vector2(c.x, c.z)).magnitude,
            500f * map.Hexes.hex_size_km + 0.5f * SeaAirCollection.SizeOf(SeaAirCollection.C54) - 1f, "off the placed hex");
    }

    /// <summary>The owner (18 October 2026, #1033): "Train scale for all ambient life". Each craft's long side as drawn
    /// (its renderers' world box, seen from above) is inside its band of the steam train's locomotive car (about 204 m:
    /// Tal Heuberger's locomotive and tender at RouteTrain.Scale x ServiceTrain.ModelSize / ServiceTrain.Cars), the band about its
    /// real size at the one world scale (SettlementKits.ModelScale, 24x: an 8.5 m house is one car): the Altalena, a 100 m
    /// LST, about 11.8 cars; the 80 m steamer about 9.4; the 10 m fishing boat about 1.2; the C-54's 35.8 m wingspan about
    /// 4.2; the Spitfire, at the unit tokens' aircraft scale (420x, #2014), its 11.2 m wingspan about 23. No ship dwarfs its town: each is shorter than its port's kit footprint is across and than a whole-town model
    /// (TownLandmarks.SizeM).</summary>
    [Test]
    public void EveryCraftIsAtTheTrainScaleAndNoShipDwarfsItsTown()
    {
        var map = OpenMap();
        float loco = Statecraft.Game.Map.Routes.RouteTrain.ModelRealLengthM * Statecraft.Game.Map.Routes.RouteTrain.Scale
            * Statecraft.Game.Map.Routes.ServiceTrain.ModelSize / Statecraft.Game.Map.Routes.ServiceTrain.Cars;
        Assert.AreEqual(204f, loco, 1f, "the locomotive's car");
        Assert.AreEqual(24f, SettlementKits.ModelScale, 0.01f, "the one world scale (#1030)");
        var bands = new Dictionary<string, (float lo, float hi)>
        {
            [SeaAirCollection.Altalena] = (11f, 12.5f), [SeaAirCollection.Steamer] = (8f, 11f),
            [SeaAirCollection.FishingBoat] = (0.9f, 1.5f), [SeaAirCollection.C54] = (3.9f, 4.5f), [SeaAirCollection.Spitfire] = (22f, 24f),
        };
        var seen = new HashSet<string>();
        var bad = new List<string>();
        foreach (var date in new[] { new DateTime(1948, 6, 15), new DateTime(1949, 8, 15) })
        {
            var layer = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
            Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count, "the labels keep clear of every drawn craft");
            foreach (var (key, model, _) in layer.Placed)
            {
                var b = WorldBounds(layer.transform.Find(key)!.gameObject);
                float side = Mathf.Max(b.size.x, b.size.z), cars = side / loco;
                seen.Add(model);
                TestContext.WriteLine($"{key}: {side:0} m long, {cars:0.00} locomotive cars, {b.size.y:0} m tall");
                var (lo, hi) = bands[model];
                if (cars < lo || cars > hi) bad.Add($"{key}: {side:0} m, {cars:0.00} locomotive cars (band {lo}-{hi})");
                if (model == SeaAirCollection.C54 || model == SeaAirCollection.Spitfire) continue;
                var place = SeaAirCollection.Wants(date, C).Single(w => w.Key == key).Place;
                var town = map.KitFootprints.FirstOrDefault(f => f.id == place);
                Assert.IsNotNull(town.id, place + " has a kit footprint");
                TestContext.WriteLine($"  {place}'s kit footprint {2f * town.radius:0} m across");
                if (side >= 2f * town.radius) bad.Add($"{key}: {side:0} m against {place}'s {2f * town.radius:0} m");
                if (side >= TownLandmarks.SizeM) bad.Add($"{key}: {side:0} m against a {TownLandmarks.SizeM:0} m whole-town model");
            }
        }
        CollectionAssert.AreEquivalent(bands.Keys, seen, "every model measured");
        CollectionAssert.IsEmpty(bad, string.Join("\n", bad));
    }

    /// <summary>The craft keep clear of the other 1948 Collection layers already on main: the whole-town models (#994), the
    /// civilians (#1023), the farm life (#1029), the wildlife (#1022) and the Tegart fort, barracks and factory (#1031). On
    /// both dated turns, every craft's box, seen from above, meets none of their renderers' boxes.</summary>
    [Test]
    public void TheCraftKeepClearOfTheOtherCollectionLayers()
    {
        var map = OpenMap();
        Action<string, CollectionDetail, Action<GameObject?>> now = (id, d, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, d)));
        var savedSeams = (TownLandmarks.Request, CivilianPeople.Request, RuralLife.Request, WildlifeLayer.Request, SpecialBuildingModels.Request);
        TownLandmarks.Request = CivilianPeople.Request = RuralLife.Request = WildlifeLayer.Request = SpecialBuildingModels.Request = now;
        try
        {
            int others = 0, checkedCraft = 0;
            foreach (var date in new[] { new DateTime(1948, 6, 15), new DateTime(1949, 8, 15) })
            {
                var none = Array.Empty<Placement>();
                var layers = new MonoBehaviour?[]
                {
                    TownLandmarks.ShowAt(map, date, none, C), CollectionGeometryFixture.People(map, date, none, C), CollectionGeometryFixture.Rural(map),
                    WildlifeLayer.Show(map), CollectionGeometryFixture.Special(map, date),
                };
                var boxes = layers.Where(l => l != null).SelectMany(l => l!.GetComponentsInChildren<Renderer>(true).Select(r => (l!.GetType().Name, r.bounds))).ToList();
                others = Math.Max(others, boxes.Count);
                var craft = CollectionGeometryFixture.Craft(map, date, none, C)!; AnswerAll();
                foreach (Transform t in craft.transform)
                {
                    var b = WorldBounds(t.gameObject); checkedCraft++;
                    foreach (var (owner, o) in boxes)
                        Assert.IsFalse(b.min.x < o.max.x && o.min.x < b.max.x && b.min.z < o.max.z && o.min.z < b.max.z,
                            $"{t.name} meets {owner}'s model at {o.center} on {date:d MMM yyyy}");
                }
            }
            Assert.Greater(others, 0, "the other layers drew");
            Assert.AreEqual(9, checkedCraft, "every craft checked, five on the first turn (the Spitfire too), four on the second");
        }
        finally
        {
            (TownLandmarks.Request, CivilianPeople.Request, RuralLife.Request, WildlifeLayer.Request, SpecialBuildingModels.Request) = savedSeams;
        }
    }

    /// <summary>Fails open: a model that does not stream warns once and is not drawn; the other craft draw and the map runs on.</summary>
    [Test]
    public void AModelThatDoesNotStreamWarnsAndTheOthersDraw()
    {
        var map = OpenMap(); var layer = CollectionGeometryFixture.Craft(map, new DateTime(1949, 1, 15), Array.Empty<Placement>(), C)!;
        var failed = asked.Single(x => x.id == SeaAirCollection.Steamer);
        LogAssert.Expect(LogType.Warning, new Regex("^SEA/AIR: the model vehicle-immigrant-steamer-1948-v2wl did not stream"));
        failed.done(null);
        foreach (var a in asked.Where(x => x.id != SeaAirCollection.Steamer).ToList()) a.done(Far(a.id));
        CollectionAssert.AreEquivalent(new[] { "jaffa-fishing", "acre-fishing" }, layer.Placed.Select(p => p.Key));
        CollectionAssert.AreEqual(new[] { "haifa-steamer" }, layer.Missing);
    }
}
