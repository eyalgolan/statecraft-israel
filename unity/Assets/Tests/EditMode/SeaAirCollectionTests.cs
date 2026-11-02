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
using UnityEngine.Rendering;
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

    static readonly string[] Models = { SeaAirCollection.Altalena, SeaAirCollection.FishingBoat, SeaAirCollection.Steamer, SeaAirCollection.C54, SeaAirCollection.Spitfire };

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
        CollectionAssert.AreEqual(new[] { "haifa-steamer", "jaffa-fishing", "acre-fishing", "el-arish-spitfire" }, Keys(new DateTime(1948, 5, 15)));
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

    /// <summary>The Spitfire on the 15 June 1948 turn: the layer, the aircraft, its loop, and its loop's centre in the air.</summary>
    (SeaAirCollection layer, Transform plane, SpitfireLoop loop, Vector3 centre) DrawnSpitfire(MapScene map)
    {
        var layer = CollectionGeometryFixture.Craft(map, new DateTime(1948, 6, 15), Array.Empty<Placement>(), C)!; AnswerAll();
        var plane = layer.transform.Find("el-arish-spitfire")!;
        Assert.IsNotNull(plane, "the Spitfire is drawn on 15 June 1948");
        return (layer, plane, plane.GetComponent<SpitfireLoop>(), layer.Placed.Single(p => p.Model == SeaAirCollection.Spitfire).At);
    }

    static Transform? Marks(SeaAirCollection layer, string part) =>
        layer.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix)?.Find(part);

    /// <summary>The owner's report, 2 November 2026: "the gyptian plane's propelor isn't moving". The model is one fused mesh,
    /// so a translucent disc at its nose turns instead: a child of the aircraft's mesh, its hub at the spinner (the mesh's
    /// front-most vertices), about the size of the model's own blades, turning about the flight axis by the clock, a sixth of
    /// a turn or less between frames at 30 fps (three blurred blades never seem to turn backwards). Its box stays inside the
    /// airframe's at every angle, so the aircraft's measured box (the labels' keep-clear, the tests above) never moves with
    /// it. Edit mode and screenshots (-shot) hold it at the loop's start, as they hold the aircraft; in play it turns.</summary>
    [Test]
    public void TheSpitfirePropellerTurnsAtItsNoseAboutTheFlightAxisAndHoldsStillInShots()
    {
        var map = OpenMap();
        var (_, plane, loop, centre) = DrawnSpitfire(map);
        var airframe = plane.GetComponentInChildren<MeshFilter>();
        var prop = airframe.transform.Find(SeaAirCollection.SpitfirePropellerName);
        Assert.IsNotNull(prop, "a propeller disc, a child of the aircraft's mesh");
        Assert.AreEqual(ShadowCastingMode.Off, prop.GetComponent<MeshRenderer>().shadowCastingMode, "a blur casts no shadow");
        float span = SeaAirCollection.SizeOf(SeaAirCollection.Spitfire);
        loop.Place(0f);
        var nose = plane.TransformDirection(Vector3.down);   // the mesh's -y, as the in-flight test reads it
        var verts = airframe.sharedMesh.vertices.Select(airframe.transform.TransformPoint).ToList();
        float front = verts.Max(v => Vector3.Dot(v, nose)), back = verts.Min(v => Vector3.Dot(v, nose));
        var spinner = verts.Where(v => Vector3.Dot(v, nose) > front - 0.01f * (front - back)).ToList();
        var tip = spinner.Aggregate(Vector3.zero, (a, v) => a + v) / spinner.Count;
        Assert.Less((prop.position - tip).magnitude, 0.06f * span, $"the hub at the spinner ({prop.position} against {tip})");
        Assert.Less(front - Vector3.Dot(prop.position, nose), 0.08f * (front - back), "at the nose");
        Assert.AreEqual(prop.lossyScale.x, prop.lossyScale.z, 0.01f * span, "a round disc");
        Assert.That(prop.lossyScale.x / span, Is.InRange(0.09f, 0.15f), "about the model's own blades (a real Spitfire's: 3.27 m across an 11.23 m span)");
        Assert.Greater(Mathf.Abs(Vector3.Dot(prop.TransformDirection(Vector3.up), nose)), 0.999f, "it turns about the flight axis");

        loop.Place(0f); var q0 = prop.localRotation;
        loop.Place(0.02f); var q1 = prop.localRotation;
        Assert.AreEqual(0.02f * SpitfireLoop.PropDegreesPerSecond, Quaternion.Angle(q0, q1), 0.5f, "it turns between two frames");
        Assert.Less(SpitfireLoop.PropDegreesPerSecond / 30f, 60f, "under half a blade's spacing a frame at 30 fps");
        for (int i = 0; i < 24; i++)
        {
            float t = i * 0.011f + (i % 4) * 10f;   // every spin angle, on the passes and in the turns
            loop.Place(t);
            var all = WorldBounds(plane.gameObject); var own = airframe.GetComponent<Renderer>().bounds;
            Assert.Less((all.min - own.min).magnitude + (all.max - own.max).magnitude, 0.5f, $"the disc stays inside the airframe's box at {t:0.000} s");
        }

        Assert.IsTrue(loop.Still, "edit mode holds the loop's start");
        var clock = SpitfireLoop.Clock;
        var update = typeof(SpitfireLoop).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var flying = new GameObject("flying");
        try
        {
            loop.Place(0f); var held = prop.localRotation;
            SpitfireLoop.Clock = () => 12.34f;
            update.Invoke(loop, null);
            Assert.AreEqual(0f, Quaternion.Angle(held, prop.localRotation), 1e-3f, "held (edit mode, -shot): the propeller does not turn");
            var disc = new GameObject("disc").transform; disc.SetParent(flying.transform, false);
            var live = flying.AddComponent<SpitfireLoop>();
            live.Attach(disc, null, null, null, Vector2.zero);
            live.Configure(centre, Quaternion.identity, Vector3.zero, still: false);
            var then = disc.localRotation;
            SpitfireLoop.Clock = () => 12.36f;
            update.Invoke(live, null);
            Assert.AreEqual(0.02f * SpitfireLoop.PropDegreesPerSecond, Quaternion.Angle(then, disc.localRotation), 0.5f, "in play it turns with the clock");
        }
        finally { SpitfireLoop.Clock = clock; Object.DestroyImmediate(flying); }
    }

    /// <summary>The owner's report: "it doesnt look fliying". The aircraft flies well clear of the ground at every point of its
    /// loop (UnitShowcase.AltitudeM over the highest ground under the loop), and a soft dark disc, as long and as wide as the
    /// aircraft seen from above and fading to nothing at its rim, lies on the ground where the map's sun (MapLighting) casts
    /// it: on the sun's ray through the aircraft, east-south-east of it, turned with its heading, a little over the ground
    /// under it and inside the board.</summary>
    [Test]
    public void TheSpitfireFliesHighOverTheGroundAndItsSoftShadowLiesDownSunOnTheBoard()
    {
        var map = OpenMap();
        bool readable = Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable;
        Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = true;
        var sun = new GameObject("TEST ONLY sun").AddComponent<Light>();
        try
        {
            sun.type = LightType.Directional; MapLighting.Apply(sun);
            Assert.Greater(Vector3.Dot(sun.transform.forward, SpitfireLoop.SunRay), 0.9999f, "the map's sun");
            var (layer, plane, loop, centre) = DrawnSpitfire(map);
            var shadow = Marks(layer, SeaAirCollection.SpitfireShadowName);
            Assert.IsNotNull(shadow, "a ground shadow beside the aircraft");
            var renderer = shadow!.GetComponent<MeshRenderer>();
            Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode); Assert.IsFalse(renderer.receiveShadows);
            var alpha = shadow.GetComponent<MeshFilter>().sharedMesh.colors32.Select(c => c.a / 255f).ToList();
            Assert.That(alpha.Max(), Is.InRange(0.2f, 0.8f), "dark but see-through at its middle");
            Assert.AreEqual(0f, alpha.Min(), 1e-3f, "nothing at its rim: a soft edge");
            var ground = PlacementLayer.CampGround(map);
            float span = SeaAirCollection.SizeOf(SeaAirCollection.Spitfire), y0 = map.Terrain.transform.position.y;
            float Height(Vector3 p) => map.Terrain.SampleHeight(p) + y0;
            var sunFlat = new Vector2(SpitfireLoop.SunRay.x, SpitfireLoop.SunRay.z).normalized;
            for (int i = 0; i < 40; i++)
            {
                float t = i * SeaAirCollection.SpitfireLoopSeconds / 40f;
                loop.Place(t);
                var b = plane.GetComponentInChildren<Renderer>().bounds;
                float under = Mathf.Max(Height(b.center), Height(b.min), Height(b.max), Height(new Vector3(b.min.x, 0f, b.max.z)), Height(new Vector3(b.max.x, 0f, b.min.z)));
                Assert.Greater(b.min.y - under, 2000f, $"well clear of the ground at {t:0.0} s");
                var at = SeaAirCollection.LoopOffset(t); var drawn = centre + new Vector3(at.x, 0f, at.y);
                var s = shadow.position; float g = Height(s);
                Assert.That(s.y - g, Is.InRange(0f, SpitfireLoop.ShadowLiftM + 400f), $"on the ground under it at {t:0.0} s");
                var cast = drawn + SpitfireLoop.SunRay * ((drawn.y - g) / -SpitfireLoop.SunRay.y);
                Assert.Less(new Vector2(s.x - cast.x, s.z - cast.z).magnitude, 0.03f * (drawn.y - g), $"on the sun's ray through the aircraft at {t:0.0} s");
                Assert.Greater(Vector2.Dot(new Vector2(s.x - drawn.x, s.z - drawn.z), sunFlat), 0.9f * (drawn.y - g), "down-sun of it");
                var heading = SeaAirCollection.LoopHeading(t);
                Assert.Greater(Vector3.Dot(shadow.forward, new Vector3(heading.x, 0f, heading.y)), 0.999f, "turned with its heading");
                Assert.That(shadow.lossyScale.z / span, Is.InRange(0.3f, 0.6f), "half as long as the aircraft, about");
                Assert.That(shadow.lossyScale.x / span, Is.InRange(0.4f, 0.6f), "half as wide, about (narrower in a bank)");
                float reach = Mathf.Max(shadow.lossyScale.x, shadow.lossyScale.z);
                Assert.IsTrue(ground.Inside(s.x - reach, s.z - reach) && ground.Inside(s.x + reach, s.z + reach), $"on the board at {t:0.0} s ({s})");
            }
        }
        finally { Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = readable; Object.DestroyImmediate(sun.gameObject); }
    }

    /// <summary>The owner's report: "it doesnt look fliying". The aircraft rolls into its turns by the path's curvature: no bank
    /// on a straight, none on the loop's passes (its least curvature, in from el-Arish and back), the unit tokens' aircraft
    /// bank (UnitShowcase.BankDegrees) at the turns at its ends (its most), and some all through the turns. The curvature is
    /// the path's own (a finite difference of LoopOffset agrees). The drawn aircraft's inner wing is lower by that roll, and
    /// its nose stays level.</summary>
    [Test]
    public void TheSpitfireBanksIntoItsTurnsByTheirCurvatureAndFliesLevelOnItsPasses()
    {
        float period = SeaAirCollection.SpitfireLoopSeconds, max = Statecraft.Game.Map.Units.UnitShowcase.BankDegrees;
        Assert.AreEqual(0f, SeaAirCollection.BankFor(0f), "no bank on a straight");
        Assert.AreEqual(0f, SeaAirCollection.LoopBankDegrees(0f), 1e-3f, "level on the pass in from el-Arish");
        Assert.AreEqual(0f, SeaAirCollection.LoopBankDegrees(0.5f * period), 1e-3f, "level on the pass back");
        Assert.AreEqual(max, SeaAirCollection.LoopBankDegrees(0.25f * period), 1e-3f, "the turn at the east end");
        Assert.AreEqual(max, SeaAirCollection.LoopBankDegrees(0.75f * period), 1e-3f, "the turn at the west end");
        float least = SeaAirCollection.LoopCurvature(0f);
        for (int i = 0; i < 200; i++)
        {
            float t = i * period / 200f, h = 0.01f;
            Vector2 p0 = SeaAirCollection.LoopOffset(t - h), p1 = SeaAirCollection.LoopOffset(t), p2 = SeaAirCollection.LoopOffset(t + h);
            Vector2 d1 = (p2 - p0) / (2f * h), d2 = (p2 - 2f * p1 + p0) / (h * h);
            float numeric = Mathf.Abs(d1.x * d2.y - d1.y * d2.x) / Mathf.Pow(d1.magnitude, 3f);
            Assert.AreEqual(numeric, SeaAirCollection.LoopCurvature(t), 0.01f * numeric, $"the path's curvature at {t:0.0} s");
            float bank = SeaAirCollection.LoopBankDegrees(t);
            Assert.That(bank, Is.InRange(0f, max + 1e-3f), $"a gentle bank at {t:0.0} s");
            if (SeaAirCollection.LoopCurvature(t) > 1.05f * least) Assert.Greater(bank, 0f, $"into the turn at {t:0.0} s");
        }

        var map = OpenMap();
        var (_, plane, loop, centre) = DrawnSpitfire(map);
        var mesh = plane.GetComponentInChildren<MeshFilter>(); var v = mesh.sharedMesh.vertices;
        float tipX = v.Max(p => Mathf.Abs(p.x));
        Vector3 Tip(float side) { var tip = v.Where(p => p.x * side > 0.96f * tipX).ToList(); return tip.Aggregate(Vector3.zero, (a, p) => a + p) / tip.Count; }
        Vector3 left = Tip(-1f), right = Tip(1f);
        var c = new Vector2(centre.x, centre.z);
        foreach (var t in new[] { 0f, 0.1f * period, 0.25f * period, 0.4f * period, 0.5f * period, 0.75f * period, 0.9f * period })
        {
            loop.Place(t);
            Vector3 a = mesh.transform.TransformPoint(left), b = mesh.transform.TransformPoint(right);
            bool aInner = Vector2.Distance(new Vector2(a.x, a.z), c) < Vector2.Distance(new Vector2(b.x, b.z), c);
            Vector3 inner = aInner ? a : b, outer = aInner ? b : a;
            float roll = Mathf.Asin((outer.y - inner.y) / (outer - inner).magnitude) * Mathf.Rad2Deg;
            Assert.AreEqual(SeaAirCollection.LoopBankDegrees(t), roll, 1.5f, $"the inner wing down by the bank at {t:0.0} s");
            Assert.AreEqual(0f, plane.TransformDirection(Vector3.down).y, 1e-3f, $"the nose level at {t:0.0} s");
        }
    }

    /// <summary>The owner's report: "it doesnt show its an egyptian unit". The map marks a unit's side by its side's flag on a
    /// pole (UnitPlinth.Build: the sourced 1948 flag, UnitFlags, owner ruling 31 October 2026), turned to the camera
    /// (UnitFlagFacing, owner ruling 15 October 2026). The Spitfire flies the same: Egypt's flag mesh and cloth colours, the
    /// pole in the units' palette, at the tokens' flag size, its pole's foot on the aircraft wherever it is in its loop,
    /// upright in a bank, and inside the box the labels keep clear of at every heading.</summary>
    [Test]
    public void TheSpitfireFliesEgyptsFlagAsTheEgyptianTokensDo()
    {
        Assert.IsTrue(Statecraft.Game.Map.Units.UnitData.Sides.Contains(Statecraft.Game.Map.Units.UnitFlags.Egypt), "Egypt is a side of the units' content");
        Assert.IsTrue(Statecraft.Game.Map.Units.UnitFlags.Has(Statecraft.Game.Map.Units.UnitFlags.Egypt), "with the sourced flag its tokens fly");
        var map = OpenMap();
        var (layer, _, loop, centre) = DrawnSpitfire(map);
        var flag = Marks(layer, SeaAirCollection.SpitfireFlagName);
        Assert.IsNotNull(flag, "a flag beside the aircraft");
        var mesh = flag!.GetComponent<MeshFilter>().sharedMesh;
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitFlags.MeshName(Statecraft.Game.Map.Units.UnitFlags.Egypt), mesh.name, "Egypt's flag, built as the tokens' (UnitFlags.Build)");
        var mats = flag.GetComponent<MeshRenderer>().sharedMaterials;
        var cloth = Statecraft.Game.Map.Units.UnitFlags.Colours(Statecraft.Game.Map.Units.UnitFlags.Egypt);
        Assert.AreEqual(1 + cloth.Count, mats.Length, "the pole and each of the cloth's colours");
        Assert.AreEqual(mesh.subMeshCount, mats.Length, "one material a submesh");
        for (int i = 0; i < cloth.Count; i++) Assert.AreEqual(cloth[i], mats[i + 1].color, $"Egypt's cloth colour {i}, as UnitFlags.ClothMaterials gives the tokens");
        var miniature = Resources.Load<Material>(Statecraft.Game.Map.Units.UnitMaterials.MiniatureResource);
        Assert.AreEqual(miniature.shader, mats[0].shader, "the pole in the units' miniature paint");
        Assert.IsNotNull(mats[0].GetTexture("_BaseMap"), "with the units' palette");
        Assert.IsNotNull(flag.GetComponent<Statecraft.Game.Map.Units.UnitFlagFacing>(), "the cloth turns to the camera, as the tokens' flags do");
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitBadge.FigureHeightM * Statecraft.Game.Map.Units.UnitShowcase.FigureScale, flag.lossyScale.y, 1f, "the tokens' flag size");
        Assert.AreEqual(ShadowCastingMode.Off, flag.GetComponent<MeshRenderer>().shadowCastingMode, "only the aircraft's own shadow on the ground");
        var keep = layer.KeepClear.Single(k => k[0].Contains(centre))[0];
        for (int i = 0; i < 16; i++)
        {
            float t = i * SeaAirCollection.SpitfireLoopSeconds / 16f;
            loop.Place(t);
            var at = SeaAirCollection.LoopOffset(t); var drawn = centre + new Vector3(at.x, 0f, at.y);
            Assert.Less((flag.position - drawn).magnitude, 1f, $"the pole stands on the aircraft at {t:0.0} s");
            foreach (var look in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Statecraft.Game.Map.Units.UnitFlagFacing.Face(flag, flag.position + look * 50000f);
                Assert.Greater(Vector3.Dot(flag.up, Vector3.up), 0.999f, $"upright at {t:0.0} s");
                var fb = flag.GetComponent<Renderer>().bounds;
                Assert.IsTrue(keep.Contains(fb.min) && keep.Contains(fb.max), $"inside the labels' keep-clear box at {t:0.0} s");
            }
        }
    }

    /// <summary>The flight's per-frame work allocates nothing (the aircraft, its bank, its propeller, its shadow and its flag).</summary>
    [Test]
    public void TheSpitfireLoopAllocatesNothingPerFrame()
    {
        var map = OpenMap();
        var (_, _, loop, _) = DrawnSpitfire(map);
        loop.Place(1f);
        int i = 0;
        Assert.That(() => { for (int k = 0; k < 100; k++) loop.Place(++i * 0.137f); }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
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
                // The drawn craft (the Spitfire's shadow and flag sit beside it in the layer, and a shadow may fall on a model).
                foreach (var t in craft.Placed.Select(p => craft.transform.Find(p.Key)))
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
