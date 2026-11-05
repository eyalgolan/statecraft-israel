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
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;
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
    /// 1 July 1949 (Magic Carpet's main wave) to 24 September 1950 (its last flights into Lydda). The harbour craft always.
    /// #2482: the 15 May 1948 turn wants the two Dakotas, lead and wing, where #2274 wanted one.</summary>
    [Test]
    public void TheAltalenaAndTheC54AreWantedOnlyOnTheirSourcedDates()
    {
        var card = C.Events.Single(e => e.Id == SeaAirCollection.AltalenaEvent);
        Assert.AreEqual(new DateTime(1948, 6, 15), card.From); Assert.AreEqual(new DateTime(1948, 6, 23), card.To);
        string[] Keys(DateTime d) => SeaAirCollection.Wants(d, C).Select(w => w.Key).ToArray();
        CollectionAssert.AreEqual(new[] { "haifa-steamer", "jaffa-fishing", "acre-fishing", "el-arish-spitfire", "tel-aviv-dakota-lead", "tel-aviv-dakota-wing" }, Keys(new DateTime(1948, 5, 15)));
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

    // ---- Tal Heuberger's Egyptian Dakotas (owner, 2 November 2026: "Over Tel Aviv, 15 May-11 Jun (Recommended)"; on #2274, "Off the coast is fine (Recommended)";
    // on #2482, 4 November 2026: "1 + make the visual 2 slightly smaller planes and not 0ne big one") ----

    /// <summary>The first bombing period, 15 May to 11 June 1948 (he.wiki rev 43856594, agent-read): on the game's monthly turns
    /// the 15 May turn only, at Tel Aviv's place. #2482 (owner, 4 November 2026: "make the visual 2 slightly smaller planes and
    /// not 0ne big one"; the sourced sortie was "גיחה של שני מטוסי הפצצה", two bombers): two Dakotas, each with its own stable
    /// key made from DakotaKey, the lead first, each slightly smaller than the one 12.2 km Dakota of #2274 (DakotaScale of the
    /// unit tokens' aircraft scale). Changed from #2274's one-key test: it asserted one want and the full 420x size.</summary>
    [Test]
    public void TheTwoDakotasAreWantedOnlyInTheFirstBombingPeriod()
    {
        bool Wanted(DateTime d)
        {
            var keys = SeaAirCollection.Wants(d, C).Where(w => w.Model == SeaAirCollection.Dakota && w.Place == "tel_aviv").Select(w => w.Key).ToArray();
            if (keys.Length == 0) return false;
            CollectionAssert.AreEqual(SeaAirCollection.DakotaKeys, keys, $"both Dakotas, the lead first, on {d:d MMM yyyy}");
            return true;
        }
        Assert.AreEqual(2, SeaAirCollection.DakotaKeys.Length, "two planes, not one");
        Assert.AreEqual(2, SeaAirCollection.DakotaKeys.Distinct().Count(), "each its own key");
        foreach (var k in SeaAirCollection.DakotaKeys) StringAssert.StartsWith(SeaAirCollection.DakotaKey + "-", k, "a key made from DakotaKey");
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
        float old = 29.11f * Statecraft.Game.Map.Units.UnitShowcase.AircraftScale;   // #2274's one Dakota, 12.2 km across
        Assert.That(SeaAirCollection.DakotaScale, Is.InRange(0.6f, 0.95f), "slightly smaller, not a different kind of piece");
        Assert.AreEqual(old * SeaAirCollection.DakotaScale, SeaAirCollection.SizeOf(SeaAirCollection.Dakota), 5f, "the unit tokens' aircraft scale times DakotaScale");
        Assert.Less(SeaAirCollection.SizeOf(SeaAirCollection.Dakota), old - 1000f, "smaller than the one big Dakota");
    }

    /// <summary>Both Dakotas on the 15 May 1948 turn: the layer, the two aircraft (lead first), their loops, and their loop's
    /// centre in the air (one centre: they fly one track).</summary>
    (SeaAirCollection layer, Transform[] planes, DakotaLoop[] loops, Vector3 centre) DrawnDakotas(MapScene map)
    {
        var layer = CollectionGeometryFixture.Craft(map, new DateTime(1948, 5, 15), Array.Empty<Placement>(), C)!;
        Assert.AreEqual(1, asked.Count(a => a.id == SeaAirCollection.Dakota && a.detail == CollectionDetail.Far), "the far mesh, once for both");
        AnswerAll();
        var planes = SeaAirCollection.DakotaKeys.Select(k => layer.transform.Find(k)).ToArray();
        for (int i = 0; i < planes.Length; i++) Assert.IsNotNull(planes[i], $"{SeaAirCollection.DakotaKeys[i]} is drawn on 15 May 1948");
        var centres = layer.Placed.Where(p => p.Model == SeaAirCollection.Dakota).Select(p => p.At).ToList();
        Assert.AreEqual(2, centres.Count, "two Dakotas placed");
        Assert.Less((centres[0] - centres[1]).magnitude, 1e-3f, "on one loop");
        return (layer, planes!, planes.Select(p => p!.GetComponent<DakotaLoop>()).ToArray(), centres[0]);
    }

    /// <summary>World x, z.</summary>
    static Vector2 Xz(Vector3 v) => new Vector2(v.x, v.z);

    /// <summary>The box the labels keep clear of for <paramref name="plane"/> now (SeaAirCollection.KeepClear, which moves it to
    /// where its loop last placed it), or null when it has none. Found by object, through the layer's keepOf.</summary>
    static Bounds? DakotaKeepOf(SeaAirCollection layer, GameObject plane)
    {
        var all = layer.KeepClear;   // first: it moves the Dakotas' boxes
        var keepOf = (System.Collections.IDictionary)typeof(SeaAirCollection).GetField("keepOf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(layer)!;
        if (!keepOf.Contains(plane)) return null;
        var boxes = (Bounds[])keepOf[plane]!;
        Assert.IsTrue(all.Any(b => ReferenceEquals(b, boxes)), $"{plane.name}'s box is in the list the labels read");
        return boxes[0];
    }

    /// <summary>A 1948 locality's point on the ground (world), from its row (content/map/localities_1948.yaml), as its label marks it.</summary>
    static Vector3 LocalityAt(MapScene map, string id)
    {
        var place = map.Localities.FirstOrDefault(p => p.Settlement.Id == id);
        Assert.IsNotNull(place, $"premise: {id} is a 1948 locality on the map");
        return map.Frame.ToWorld(place!.Settlement.Lon, place.Settlement.Lat, 0);
    }

    /// <summary>How far (m) the ground point <paramref name="p"/> lies outside the box's footprint seen from above (0 inside it).</summary>
    static float OutsideFootprint(Bounds box, Vector3 p) =>
        new Vector2(Mathf.Max(0f, box.min.x - p.x, p.x - box.max.x), Mathf.Max(0f, box.min.z - p.z, p.z - box.max.z)).magnitude;

    /// <summary>#2490, owner, 4 November 2026: "Shrink the keep-clear area (Recommended)". On the 15 May 1948 turn the names had
    /// kept clear of each Dakota's whole loop, which hid 14 names round Tel Aviv (EN 18 to 4 shown, HE 20 to 5); now a name
    /// keeps clear only of where an aircraft and its flag are. Ramat Gan (34.8235, 32.07) lies under the lead at the loop's
    /// start (over Tel Aviv's hex), so its box covers it there; at the loop's half (60 s, the lead on the northern pass, the
    /// wing in the eastern turn) neither box comes within 3 km of Ramat Gan or Herzliya (34.8422, 32.1556), both inside the
    /// whole loop's box the names kept clear of before. A box always holds its own aircraft, and moves with it.</summary>
    [Test]
    public void TheNamesKeepClearOnlyOfWhereEachDakotaIsNow()
    {
        var map = OpenMap();
        var (layer, planes, loops, at) = DrawnDakotas(map);
        var ramatGan = LocalityAt(map, "ramat_gan"); var herzliya = LocalityAt(map, "herzliya");
        // Premise: #2274's whole-loop box (the loop and the aircraft's reach) covered both, so both names were hidden.
        var swept = SeaAirCollection.DakotaSweptHalf;
        foreach (var (name, p) in new[] { ("Ramat Gan", ramatGan), ("Herzliya", herzliya) })
            Assert.IsTrue(Mathf.Abs(p.x - at.x) < swept.x && Mathf.Abs(p.z - at.z) < swept.y, $"premise: {name} is inside the whole loop's box");

        for (int p = 0; p < planes.Length; p++) loops[p].Place(0f);
        var lead0 = DakotaKeepOf(layer, planes[0].gameObject)!.Value;
        Assert.AreEqual(0f, OutsideFootprint(lead0, ramatGan), 1e-3f, "at the loop's start the lead is over Ramat Gan: its name keeps clear");
        foreach (var plane in planes)
        {
            var pb = WorldBounds(plane.gameObject); var keep = DakotaKeepOf(layer, plane.gameObject)!.Value;
            Assert.IsTrue(keep.Contains(pb.min) && keep.Contains(pb.max), $"{plane.name}'s box holds it at the loop's start");
        }

        float half = 0.5f * SeaAirCollection.DakotaLoopSeconds;
        for (int p = 0; p < planes.Length; p++) loops[p].Place(half);
        foreach (var plane in planes)
        {
            var keep = DakotaKeepOf(layer, plane.gameObject)!.Value; var pb = WorldBounds(plane.gameObject);
            Assert.IsTrue(keep.Contains(pb.min) && keep.Contains(pb.max), $"{plane.name}'s box moved with it to {half:0} s");
            TestContext.WriteLine($"{plane.name} at {half:0} s: Ramat Gan {OutsideFootprint(keep, ramatGan):0} m out, Herzliya {OutsideFootprint(keep, herzliya):0} m out");
            Assert.Greater(OutsideFootprint(keep, ramatGan), 3000f, $"{plane.name} is far from Ramat Gan at {half:0} s: its name shows");
            Assert.Greater(OutsideFootprint(keep, herzliya), 3000f, $"{plane.name} is far from Herzliya at {half:0} s: its name shows");
        }
        Assert.Less(DakotaKeepOf(layer, planes[0].gameObject)!.Value.size.x, 0.6f * 2f * swept.x, "much smaller than the whole loop's box");
    }

    /// <summary>#2490: the Dakotas' keep-clear boxes move a DakotaKeepStepS step at a time, so the labels (which re-place
    /// themselves only on a new list) re-place at most once a step, never every frame: within one step KeepClear is the
    /// same list, a step on it is a new one, and the box for a step covers the aircraft through the whole step.</summary>
    [Test]
    public void TheDakotasKeepClearMovesAStepAtATime()
    {
        var map = OpenMap();
        var (layer, planes, loops, _) = DrawnDakotas(map);
        float step = SeaAirCollection.DakotaKeepStepS;
        Assert.That(step, Is.InRange(0.25f, 2f), "a short step: the box stays near the aircraft, the labels re-place a few times a second at most");
        for (int p = 0; p < planes.Length; p++) loops[p].Place(10f * step);
        var first = layer.KeepClear;
        Assert.AreSame(first, layer.KeepClear, "no new list while nothing moves");
        for (int p = 0; p < planes.Length; p++) loops[p].Place(10.5f * step);
        Assert.AreSame(first, layer.KeepClear, "the same list within one step");
        var box = DakotaKeepOf(layer, planes[0].gameObject)!.Value;
        for (int i = 0; i <= 8; i++)
        {
            loops[0].Place((10f + i / 8f * 0.999f) * step);
            var pb = WorldBounds(planes[0].gameObject);
            Assert.IsTrue(box.Contains(pb.min) && box.Contains(pb.max), $"the step's box holds the lead through the step ({i}/8)");
        }
        for (int p = 0; p < planes.Length; p++) loops[p].Place(11.5f * step);
        Assert.AreNotSame(first, layer.KeepClear, "a new list a step on, so the labels re-place");
    }


    /// <summary>The world point is on one of Tel Aviv's hexes (content/map/settlement_hexes.yaml), by the map's own click
    /// picker (MapPointer.Pick: lon/lat and tools/hexgrid.py's rounding), a different route from the layer's own grid test.</summary>
    static bool OnTelAviv(MapScene map, Vector3 world)
    {
        var (q, r) = MapPointer.Pick(map, world);
        return C.SettlementHexes.Any(h => h.Id == SeaAirCollection.DakotaTown && h.Q == q && h.R == r);
    }

    /// <summary>Two Dakotas in the window, none outside it (#2482): on the turns of 14 May, 12 June and 15 June 1948 no Dakota,
    /// no loop, and no marks object (flag, shadow, bombs) is drawn; on the 15 May turn exactly two looping Dakotas, each at
    /// the smaller size.</summary>
    [Test]
    public void TwoDakotasFlyInTheWindowAndNoneOutsideIt()
    {
        var map = OpenMap();
        foreach (var (date, flying) in new[]
        {
            (new DateTime(1948, 5, 14), false), (new DateTime(1948, 5, 15), true), (new DateTime(1948, 6, 11), true),
            (new DateTime(1948, 6, 12), false), (new DateTime(1948, 6, 15), false),
        })
        {
            var layer = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
            Assert.AreEqual(flying ? 2 : 0, layer.Placed.Count(p => p.Model == SeaAirCollection.Dakota), $"the Dakotas on {date:d MMM yyyy}");
            Assert.AreEqual(flying ? 2 : 0, Object.FindObjectsByType<DakotaLoop>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, $"the loops on {date:d MMM yyyy}");
            foreach (var k in SeaAirCollection.DakotaKeys)
            {
                Assert.AreEqual(flying, layer.transform.Find(k) != null, $"{k} on {date:d MMM yyyy}");
                Assert.AreEqual(flying, layer.transform.Find(k + SeaAirCollection.DakotaMarksSuffix) != null, $"{k}'s flag, shadow and bombs on {date:d MMM yyyy}");
            }
        }
    }

    /// <summary>#2482: "on the bombing turns it flies over Tel Aviv (not just off the coast)" (option 1, which the owner chose on
    /// 4 November 2026). Changed from #2274's TheDakotaLoopsJustOffTelAvivInsideTheBoardClearOfTokensAndLabels, which held the
    /// whole loop over the sea and its ground box clear of every token, kit and name (the 2 November "Off the coast is fine"
    /// placement): over the city the loop cannot clear Kiryati's token on Tel Aviv, so those ground clearances go and only the
    /// clearance of the aircraft at altitude stays. The loop's start (the screenshots' frame) is over Tel Aviv's hex, its path
    /// crosses the hex and runs out over the sea (in from the sea), the whole loop is over the board, the aircraft fly at the
    /// unit tokens' aircraft altitude, nose east and wingspan north-south at the start, and the labels keep clear of each
    /// aircraft at every point of the loop. Changed for #2490 (owner, 4 November 2026: "Shrink the keep-clear area
    /// (Recommended)"): the box is where the aircraft is now, read after each placing, not one box over its whole loop.</summary>
    [Test]
    public void TheDakotasLoopOverTelAvivAtAltitudeInsideTheBoard()
    {
        var map = OpenMap();
        var (layer, planes, loops, at) = DrawnDakotas(map);
        Assert.Greater(C.SettlementHexes.Count(h => h.Id == SeaAirCollection.DakotaTown), 0, "premise: Tel Aviv has its hex");
        var centre = Xz(at);
        var ground = PlacementLayer.CampGround(map);
        Assert.IsTrue(OnTelAviv(map, new Vector3(centre.x + SeaAirCollection.DakotaLoopOffset(0f).x, 0f, centre.y + SeaAirCollection.DakotaLoopOffset(0f).y)),
            "the loop's start, held in screenshots, is over Tel Aviv's hex");
        int over = 0, sea = 0;
        float period = SeaAirCollection.DakotaLoopSeconds;
        for (int i = 0; i < 240; i++)
        {
            var q = centre + SeaAirCollection.DakotaLoopOffset(i * period / 240f);
            if (OnTelAviv(map, new Vector3(q.x, 0f, q.y))) over++;
            if (ground.Water(q.x, q.y)) sea++;
        }
        TestContext.WriteLine($"Dakota loop: {over} of 240 steps over Tel Aviv's hex, {sea} over the sea");
        Assert.Greater(over, 0, "its path crosses Tel Aviv's hex");
        Assert.Greater(sea, 0, "and runs out over the sea");
        var half = SeaAirCollection.DakotaSweptHalf;
        Assert.IsTrue(ground.Inside(centre.x - half.x, centre.y - half.y) && ground.Inside(centre.x + half.x, centre.y + half.y), "its whole loop over the board");
        float span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota);
        float floor = Statecraft.Game.Map.Units.UnitShowcase.LoopFloor(map, new Vector3(at.x, 0f, at.z), SeaAirCollection.DakotaLoopHalfLengthM + span);
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitShowcase.AltitudeM, at.y - floor, 1f, "the unit tokens' aircraft altitude, as the Spitfire");
        loops[0].Place(0f);
        var b = WorldBounds(planes[0].gameObject);
        Assert.AreEqual(span, b.size.z, 1f, "wingspan north-south at its size: the nose points east");
        Assert.Greater(Vector3.Dot(planes[0].TransformDirection(Vector3.down), Vector3.right), 0.9f, "the nose (the mesh's -y) points east at the loop's start");
        Assert.Greater(b.min.y, floor, "in flight, never on the ground");
        foreach (var loop in loops) Assert.IsTrue(loop.Still, "edit mode holds the loop's start");
        Assert.AreEqual(2, planes.Count(p => DakotaKeepOf(layer, p.gameObject) != null), "one keep-clear box an aircraft");
        for (int i = 0; i < 60; i++)
        {
            float t = i * period / 60f;
            for (int p = 0; p < planes.Length; p++)
            {
                loops[p].Place(t);
                var pb = WorldBounds(planes[p].gameObject);
                var keep = DakotaKeepOf(layer, planes[p].gameObject)!.Value;   // the box follows it (#2490: "Shrink the keep-clear area")
                Assert.IsTrue(keep.Contains(pb.min) && keep.Contains(pb.max), $"{planes[p].name} inside the box the labels keep clear of at {t:0.0} s");
                Assert.IsTrue(Mathf.Abs(pb.center.x - centre.x) <= half.x && Mathf.Abs(pb.center.z - centre.y) <= half.y, $"{planes[p].name} inside its swept box at {t:0.0} s");
                Assert.Greater(pb.min.y, floor, $"{planes[p].name} in flight at {t:0.0} s");
            }
        }
    }

    /// <summary>#2482's pair (the sourced "גיחה של שני מטוסי הפצצה", two bombers a sortie): both fly the one track, the wing
    /// <see cref="SeaAirCollection.DakotaTrailSeconds"/> behind the lead (close behind, not across the loop), at one altitude,
    /// and they never meet: seen from above, every point of a Dakota lies within DakotaReachShare of its span of its drawn
    /// centre (read off the mesh here), and their centres stay more than twice that apart at every point of the loop.</summary>
    [Test]
    public void TheTwoSmallerDakotasFlyOneTrackAsAPairAndNeverMeet()
    {
        var map = OpenMap();
        var (_, planes, loops, centre) = DrawnDakotas(map);
        float span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota), period = SeaAirCollection.DakotaLoopSeconds;
        Assert.AreEqual(0f, loops[0].Lag, 1e-4f, "the lead flies the loop's own clock");
        Assert.AreEqual(SeaAirCollection.DakotaTrailSeconds, loops[1].Lag, 1e-4f, "the wing trails it");
        Assert.That(SeaAirCollection.DakotaTrailSeconds, Is.InRange(0.05f * period, 0.3f * period), "close behind: a pair, not opposite sides of the loop");
        for (int p = 0; p < planes.Length; p++)
        {
            loops[p].Place(loops[p].Lag);   // each at its own loop's start: level, nose east
            Assert.AreEqual(span, WorldBounds(planes[p].gameObject).size.z, 1f, $"{planes[p].name} at the smaller size");
        }
        loops[0].Place(0f);
        var mesh = planes[0].GetComponentInChildren<MeshFilter>();
        var drawn = WorldBounds(planes[0].gameObject).center;
        float reach = mesh.sharedMesh.vertices.Select(mesh.transform.TransformPoint).Max(v => Xz(v - drawn).magnitude);
        TestContext.WriteLine($"Dakota reach from above {reach:0} m, {reach / span:0.000} of its span");
        Assert.LessOrEqual(reach, SeaAirCollection.DakotaReachShare * span, "every point within DakotaReachShare of the span of its centre");
        float closest = float.MaxValue;
        for (int i = 0; i < 480; i++)
        {
            float t = i * period / 480f;
            foreach (var loop in loops) loop.Place(t);
            var a = WorldBounds(planes[0].gameObject).center; var b = WorldBounds(planes[1].gameObject).center;
            var lag = SeaAirCollection.DakotaLoopOffset(t - SeaAirCollection.DakotaTrailSeconds);
            Assert.Less((Xz(b) - (Xz(centre) + lag)).magnitude, 1f, $"the wing on the lead's track at {t:0.0} s");
            Assert.AreEqual(a.y, b.y, 1f, "at one altitude");
            closest = Mathf.Min(closest, (Xz(a) - Xz(b)).magnitude);
        }
        TestContext.WriteLine($"closest approach {closest:0} m, against {2f * SeaAirCollection.DakotaReachShare * span:0} m");
        Assert.Greater(closest, 2f * SeaAirCollection.DakotaReachShare * span, "the two never meet");
    }

    /// <summary>The loop is closed (a period on, the same place and heading, also long into a session), each aircraft flies it
    /// nose first with its nose level, and the per-frame step (the aircraft, its propellers, shadow, flag and bombs) allocates
    /// nothing. Changed from #2274's test: the period may now reach 120 s (the larger loop of #2482 at about the Spitfire's
    /// speed) and both aircraft are checked, each on its own lagged clock.</summary>
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
        var (_, planes, loops, centre) = DrawnDakotas(map);
        for (int p = 0; p < planes.Length; p++)
            for (int i = 0; i < 24; i++)
            {
                float t = i * period / 24f, u = t - loops[p].Lag;
                loops[p].Place(t);
                var nose = planes[p].TransformDirection(Vector3.down);
                Assert.AreEqual(0f, nose.y, 1e-3f, "the nose level (it banks about it)");
                Assert.Greater(Vector2.Dot(new Vector2(nose.x, nose.z).normalized, SeaAirCollection.DakotaLoopHeading(u)), 0.999f, $"{planes[p].name} nose along the path at {t:0.0} s");
                var b = WorldBounds(planes[p].gameObject); var at = SeaAirCollection.DakotaLoopOffset(u);
                Assert.AreEqual(centre.x + at.x, b.center.x, 1f); Assert.AreEqual(centre.z + at.y, b.center.z, 1f); Assert.AreEqual(centre.y, b.center.y, 1f);
            }
        foreach (var loop in loops) for (int i = 0; i < 10; i++) loop.Place(i);   // warm up
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) foreach (var loop in loops) loop.Place(i * 0.016f);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "no allocation per frame");
    }

    /// <summary>#2482's "a bank into their turns" (owner, 4 November 2026, before the ruling: "it also doesnt look like its flying"):
    /// the Dakotas' loop is the Spitfire's shape at DakotaLoopScale, so its curvature at each point is the Spitfire's over that
    /// scale, and the Spitfire's BankFor of the curvature at the Spitfire's scale gives the same bank at the same point: level on
    /// the passes (the bomb run over Tel Aviv among them), the unit tokens' aircraft bank at the ends, and some all through the
    /// turns. The curvature is the path's own (a finite difference of DakotaLoopOffset agrees). Each drawn aircraft's inner
    /// wing is lower by its own bank, on its own lagged clock, and its nose stays level.</summary>
    [Test]
    public void TheDakotasBankIntoTheirTurnsByBankFor()
    {
        float period = SeaAirCollection.DakotaLoopSeconds, max = Statecraft.Game.Map.Units.UnitShowcase.BankDegrees;
        Assert.AreEqual(0f, SeaAirCollection.DakotaBankDegrees(0f), 1e-3f, "level on the bomb run over Tel Aviv");
        Assert.AreEqual(0f, SeaAirCollection.DakotaBankDegrees(0.5f * period), 1e-3f, "level on the pass back");
        Assert.AreEqual(max, SeaAirCollection.DakotaBankDegrees(0.25f * period), 1e-3f, "the turn at the east end");
        Assert.AreEqual(max, SeaAirCollection.DakotaBankDegrees(0.75f * period), 1e-3f, "the turn at the west end");
        float least = SeaAirCollection.DakotaLoopCurvature(0f);
        for (int i = 0; i < 200; i++)
        {
            float t = i * period / 200f, h = 0.5f;
            Vector2 p0 = SeaAirCollection.DakotaLoopOffset(t - h), p1 = SeaAirCollection.DakotaLoopOffset(t), p2 = SeaAirCollection.DakotaLoopOffset(t + h);
            Vector2 d1 = (p2 - p0) / (2f * h), d2 = (p2 - 2f * p1 + p0) / (h * h);
            float numeric = Mathf.Abs(d1.x * d2.y - d1.y * d2.x) / Mathf.Pow(d1.magnitude, 3f);
            float k = SeaAirCollection.DakotaLoopCurvature(t);
            Assert.AreEqual(numeric, k, 0.01f * numeric, $"the path's curvature at {t:0.0} s");
            Assert.AreEqual(SeaAirCollection.BankFor(SeaAirCollection.DakotaLoopScale * k), SeaAirCollection.DakotaBankDegrees(t), 1e-3f, $"BankFor at the Spitfire's scale at {t:0.0} s");
            if (k > 1.05f * least) Assert.Greater(SeaAirCollection.DakotaBankDegrees(t), 0f, $"into the turn at {t:0.0} s");
        }
        var map = OpenMap();
        var (_, planes, loops, centre) = DrawnDakotas(map);
        var c = Xz(centre);
        for (int p = 0; p < planes.Length; p++)
        {
            var mesh = planes[p].GetComponentInChildren<MeshFilter>(); var v = mesh.sharedMesh.vertices;
            float tipX = v.Max(q => Mathf.Abs(q.x));
            Vector3 Tip(float side) { var tip = v.Where(q => q.x * side > 0.96f * tipX).ToList(); return tip.Aggregate(Vector3.zero, (a, q) => a + q) / tip.Count; }
            Vector3 left = Tip(-1f), right = Tip(1f);
            for (int i = 0; i <= 200; i++)
            {
                float t = i * 2f * period / 200f;
                loops[p].Place(t);
                Vector3 a = mesh.transform.TransformPoint(left), b = mesh.transform.TransformPoint(right);
                bool aInner = Vector2.Distance(Xz(a), c) < Vector2.Distance(Xz(b), c);
                Vector3 inner = aInner ? a : b, outer = aInner ? b : a;
                float roll = Mathf.Asin((outer.y - inner.y) / (outer - inner).magnitude) * Mathf.Rad2Deg;
                Assert.AreEqual(SeaAirCollection.DakotaBankDegrees(t - loops[p].Lag), roll, 1.5f, $"{planes[p].name}'s inner wing down by its bank at {t:0.0} s");
                Assert.AreEqual(0f, planes[p].TransformDirection(Vector3.down).y, 1e-3f, $"the nose level at {t:0.0} s");
            }
        }
    }

    /// <summary>#2482's "turning propellers on both engines" (the C-47's two radial engines, en.wiki "Douglas C-47 Skytrain"). The
    /// model is one fused mesh, so, as the Spitfire's (#2260), a translucent disc turns at each engine instead: two children
    /// of the aircraft's mesh, one either side of the fuselage, each at the front of its own nacelle (the front-most vertices
    /// on its side, read here from the mesh), turning about the flight axis by the clock, held still in edit mode and shots.
    /// Their boxes stay inside the airframe's at every angle, so the aircraft's measured box never moves with them.</summary>
    [Test]
    public void EachDakotaEngineHasATurningPropeller()
    {
        var map = OpenMap();
        var (_, planes, loops, _) = DrawnDakotas(map);
        float span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota);
        for (int p = 0; p < planes.Length; p++)
        {
            var loop = loops[p]; var plane = planes[p];
            var airframe = plane.GetComponentInChildren<MeshFilter>();
            var props = airframe.transform.Cast<Transform>().Where(x => x.name == SeaAirCollection.DakotaPropellerName).ToArray();
            Assert.AreEqual(2, props.Length, $"{plane.name}: a disc at each engine");
            loop.Place(loop.Lag);   // level, nose east
            var nose = plane.TransformDirection(Vector3.down); var across = plane.TransformDirection(Vector3.right);
            var middle = WorldBounds(plane.gameObject).center;
            var verts = airframe.sharedMesh.vertices.Select(airframe.transform.TransformPoint).ToList();
            float length = verts.Max(v => Vector3.Dot(v, nose)) - verts.Min(v => Vector3.Dot(v, nose));
            var sides = props.Select(x => Vector3.Dot(x.position - middle, across)).ToArray();
            Assert.Less(sides[0] * sides[1], 0f, "one either side of the fuselage");
            foreach (var prop in props)
            {
                float side = Vector3.Dot(prop.position - middle, across);
                Assert.That(Mathf.Abs(side) / span, Is.InRange(0.08f, 0.2f), "on the wing, at an engine, not at the nose or a tip");
                var nacelle = verts.Where(v => Mathf.Abs(Vector3.Dot(v - middle, across) - side) < 0.03f * span).ToList();
                float front = nacelle.Max(v => Vector3.Dot(v, nose));
                float behind = front - Vector3.Dot(prop.position, nose);
                Assert.That(behind / length, Is.InRange(-0.01f, 0.05f), $"at its nacelle's front ({behind:0} m behind it)");
                Assert.Greater(Mathf.Abs(Vector3.Dot(prop.TransformDirection(Vector3.up), nose)), 0.999f, "it turns about the flight axis");
                Assert.AreEqual(ShadowCastingMode.Off, prop.GetComponent<MeshRenderer>().shadowCastingMode, "a blur casts no shadow");
                loop.Place(loop.Lag); var q0 = prop.localRotation;
                loop.Place(loop.Lag + 0.02f); var q1 = prop.localRotation;
                Assert.AreEqual(0.02f * SpitfireLoop.PropDegreesPerSecond, Quaternion.Angle(q0, q1), 0.5f, "it turns between two frames");
            }
            for (int i = 0; i < 24; i++)
            {
                float t = i * 0.011f + (i % 4) * 25f;
                loop.Place(t);
                var all = WorldBounds(plane.gameObject); var own = airframe.GetComponent<Renderer>().bounds;
                Assert.Less((all.min - own.min).magnitude + (all.max - own.max).magnitude, 0.5f, $"the discs' boxes stay inside the airframe's at {t:0.000} s");
            }
            Assert.IsTrue(loop.Still, "edit mode holds the loop's start");
            var clock = SpitfireLoop.Clock;
            var update = typeof(DakotaLoop).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            try
            {
                loop.Place(0f); var held = props[0].localRotation;
                SpitfireLoop.Clock = () => 12.34f;
                update.Invoke(loop, null);
                Assert.AreEqual(0f, Quaternion.Angle(held, props[0].localRotation), 1e-3f, "held (edit mode, -shot): the propellers do not turn");
            }
            finally { SpitfireLoop.Clock = clock; }
        }
    }

    /// <summary>#2482's "a shadow on the ground", as the Spitfire's: a soft dark disc in each Dakota's marks object, where the
    /// scene's sun casts it through the aircraft, turned with its heading, a little over the ground (or the sea) under it.</summary>
    [Test]
    public void EachDakotaCastsAShadowOnTheGround()
    {
        var map = OpenMap();
        var sun = new GameObject("TEST ONLY sun").AddComponent<Light>();
        var sunWas = RenderSettings.sun;
        try
        {
            sun.type = LightType.Directional; MapLighting.Apply(sun);
            var (layer, planes, loops, centre) = DrawnDakotas(map);
            float y0 = map.Terrain.transform.position.y, span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota);
            float Height(Vector3 q) => Mathf.Max(map.Terrain.SampleHeight(q) + y0, PlacementLayer.CampGround(map).SeaLevelY);
            for (int p = 0; p < planes.Length; p++)
            {
                var shadow = layer.transform.Find(planes[p].name + SeaAirCollection.DakotaMarksSuffix)?.Find(SeaAirCollection.DakotaShadowName);
                Assert.IsNotNull(shadow, $"{planes[p].name}: a ground shadow beside the aircraft");
                var r = shadow!.GetComponent<MeshRenderer>();
                Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode); Assert.IsFalse(r.receiveShadows);
                for (int i = 0; i < 24; i++)
                {
                    float t = i * SeaAirCollection.DakotaLoopSeconds / 24f, u = t - loops[p].Lag;
                    loops[p].Place(t);
                    var at = SeaAirCollection.DakotaLoopOffset(u); var drawn = centre + new Vector3(at.x, 0f, at.y);
                    var s = shadow.position; float g = Height(s);
                    Assert.GreaterOrEqual(s.y - g, 0f, $"over the ground under it at {t:0.0} s");
                    // The disc lies flat, lifted ShadowLiftM over the highest ground under its middle and its four ends
                    // (SpitfireLoop.LayShadow). The Dakota's is km across, so on the rising ground east of Tel Aviv an end can
                    // stand hundreds of metres over the middle: the rule, read off the shadow's own transform, not a fixed band.
                    var fore = shadow.forward; var side = new Vector3(fore.z, 0f, -fore.x);
                    float hMid = Height(s), hAcross1 = Height(s + side * shadow.localScale.x), hAcross2 = Height(s - side * shadow.localScale.x),
                        hAlong1 = Height(s + fore * shadow.localScale.z), hAlong2 = Height(s - fore * shadow.localScale.z);
                    float top = Mathf.Max(hMid, Mathf.Max(Mathf.Max(hAcross1, hAcross2), Mathf.Max(hAlong1, hAlong2)));
                    Assert.AreEqual(top + SpitfireLoop.ShadowLiftM, s.y, 0.5f, $"lifted ShadowLiftM over the highest ground under it at {t:0.0} s: "
                        + $"middle {hMid:0.0}, across {hAcross1:0.0} / {hAcross2:0.0}, along {hAlong1:0.0} / {hAlong2:0.0} m");
                    var cast = drawn + SpitfireLoop.SunRay * ((drawn.y - g) / -SpitfireLoop.SunRay.y);
                    float miss = Xz(s - cast).magnitude;
                    Assert.Less(miss, 0.03f * (drawn.y - g), $"on the sun's ray through the aircraft at {t:0.0} s: ground {g:0} m under the shadow, "
                        + $"{Height(drawn):0} m under the aircraft, {drawn.y:0} m aircraft, {miss:0} m off the ray");
                    // The ray's first crossing (review of #2490): from the aircraft to the shadow the ray is over the ground at
                    // every 100 m, but for the last metre, so no ridge it clips before the shadow is passed over.
                    float reach = Xz(s - drawn).magnitude / Xz(SpitfireLoop.SunRay).magnitude;
                    for (float along = 0f; along < reach - 1f; along += 100f)
                    {
                        var q = drawn + SpitfireLoop.SunRay * along;
                        Assert.Greater(q.y, Height(q), $"the ray over the ground before its shadow at {t:0.0} s: under it at s = {along:0} m of {reach:0} m");
                    }
                    var heading = SeaAirCollection.DakotaLoopHeading(u);
                    Assert.Greater(Vector3.Dot(shadow.forward, new Vector3(heading.x, 0f, heading.y)), 0.999f, "turned with its heading");
                    Assert.That(shadow.lossyScale.x / span, Is.InRange(0.4f, 0.6f), "half as wide, about (narrower in a bank)");
                }
            }
        }
        finally { RenderSettings.sun = sunWas; Object.DestroyImmediate(sun.gameObject); }
    }

    /// <summary>#2482, option 1: "drops bombs with flashes and smoke on the city. Looks only; no change to the game's numbers."
    /// Each Dakota releases a bomb every DakotaBombIntervalS of its loop while it is over Tel Aviv's hex, and only where the
    /// bomb also lands on that hex: every release point and every impact point is on Tel Aviv's hex (by the map's own click
    /// picker). A bomb falls from the aircraft's belly to the ground under the aircraft's track, faster as it falls; at the
    /// impact a flash shows, then puffs of smoke rise there. Over two loops, every bomb, flash and puff drawn is over Tel Aviv's
    /// hex; on the pass back (half a loop on) the lead draws none. Outside the window there is no Dakota, so nothing drops
    /// (TwoDakotasFlyInTheWindowAndNoneOutsideIt).</summary>
    [Test]
    public void TheDakotasBombTelAvivAndOnlyTelAviv()
    {
        var map = OpenMap();
        var (_, planes, loops, _) = DrawnDakotas(map);
        float period = SeaAirCollection.DakotaLoopSeconds, fall = SeaAirCollection.DakotaBombFallS, every = SeaAirCollection.DakotaBombIntervalS;
        var ground = PlacementLayer.CampGround(map);
        for (int p = 0; p < planes.Length; p++)
        {
            var loop = loops[p]; var run = loop.Bombs;
            Assert.IsNotNull(run, $"{planes[p].name} carries its bombs");
            Assert.Greater(run!.Drops.Count, 1, $"{planes[p].name} drops bombs on its pass over Tel Aviv");
            foreach (var d in run.Drops)
            {
                Assert.That(d.At, Is.InRange(0f, period), "a time of its loop");
                Assert.AreEqual(0f, Mathf.Repeat(d.At / every + 0.5f, 1f) - 0.5f, 1e-3f, "on the release clock");
                Assert.IsTrue(OnTelAviv(map, d.From), $"released over Tel Aviv at {d.At:0.0} s ({d.From})");
                Assert.IsTrue(OnTelAviv(map, d.To), $"lands on Tel Aviv at {d.At:0.0} s ({d.To})");
                Assert.Greater(d.From.y - d.To.y, 2000f, "dropped from the aircraft's belly at altitude");
                Assert.AreEqual(ground.Y(d.To.x, d.To.z), d.To.y, 1f, "lands on the ground");
            }
            var first = run.Drops[0];
            loop.Place(loop.Lag + first.At + 0.5f * fall);
            var expect = Vector3.Lerp(first.From, first.To, 0.5f); expect.y = first.From.y - 0.25f * (first.From.y - first.To.y);
            Assert.IsTrue(run.Bombs.Any(r => r.enabled && (r.transform.position - expect).magnitude < 1f), "half way through its fall a bomb is a quarter of the way down, falling faster");
            loop.Place(loop.Lag + first.At + fall + 0.05f);
            Assert.IsTrue(run.Flashes.Any(r => r.enabled && Xz(r.transform.position - first.To).magnitude < 1f), "a flash at the impact");
            loop.Place(loop.Lag + first.At + fall + 1f);
            Assert.IsTrue(run.Smoke.Any(r => r.enabled && Xz(r.transform.position - first.To).magnitude < SeaAirCollection.DakotaBombMarginM), "smoke rising at the impact");
            Assert.IsTrue(run.Blasts.Any(r => r.enabled && Xz(r.transform.position - first.To).magnitude < 1f), "the blast at the impact");
            loop.Place(loop.Lag + 0.5f * period);
            Assert.IsFalse(run.Bombs.Concat(run.Flashes).Concat(run.Blasts).Concat(run.Smoke).Any(r => r.enabled), "no bomb, flash, blast or dust on the pass back, away from Tel Aviv");
            Assert.IsTrue(run.Columns.Any(r => r.enabled), "the smoke columns still rising over Tel Aviv on the pass back (owner, 4 November 2026)");
            int bombs = 0, flashes = 0, blasts = 0, smoke = 0, columns = 0;
            for (int i = 0; i < 2400; i++)
            {
                float t = i * 2f * period / 2400f;
                loop.Place(t);
                foreach (var r in run.Bombs.Concat(run.Flashes).Concat(run.Blasts).Concat(run.Smoke).Concat(run.Columns))
                {
                    if (!r.enabled) continue;
                    Assert.IsTrue(OnTelAviv(map, r.transform.position), $"{r.name} over Tel Aviv at {t:0.00} s ({r.transform.position})");
                    if (run.Bombs.Contains(r)) bombs++; else if (run.Flashes.Contains(r)) flashes++; else if (run.Blasts.Contains(r)) blasts++;
                    else if (run.Smoke.Contains(r)) smoke++; else columns++;
                }
            }
            TestContext.WriteLine($"{planes[p].name}: {run.Drops.Count} drops a loop; {bombs} bomb, {flashes} flash, {blasts} blast, {smoke} dust "
                + $"and {columns} column-puff frames over two loops");
            Assert.Greater(bombs, 0); Assert.Greater(flashes, 0); Assert.Greater(blasts, 0); Assert.Greater(smoke, 0); Assert.Greater(columns, 0);
        }
    }

    /// <summary>The owner, 4 November 2026, on the 15 May captures: "the explosions look bad in the screenshot and there is no
    /// smoke columns coming up", then "it needs to look top quality" (Unity Labs' CC0 flipbooks approved). The blast plays
    /// the explosion flipbook once over DakotaBlastS: it opens at DakotaBlastOpenShare of DakotaBlastM, swells to the full
    /// size by a quarter of its life and on a little after, its frames run in order from the first to the last, and it fades
    /// over its last DakotaBlastFadeShare to nothing; then it is gone.</summary>
    [Test]
    public void EachBlastPlaysTheExplosionFlipbookSwellingThenFading()
    {
        Assert.IsNotNull(Resources.Load<Texture2D>(SeaAirCollection.DakotaBlastTexture), "the explosion flipbook ships in Resources");
        Assert.IsNotNull(Resources.Load<Texture2D>(SeaAirCollection.DakotaSmokeTexture), "the smoke flipbook ships in Resources");
        Assert.AreEqual(SeaAirCollection.DakotaBlastOpenShare, DakotaBombRun.BlastSwell(0f), 1e-5f, "opens small");
        Assert.AreEqual(1f, DakotaBombRun.BlastSwell(0.25f), 1e-5f, "full size by a quarter of its life");
        for (float b = 0.01f; b <= 1f; b += 0.01f)
            Assert.Greater(DakotaBombRun.BlastSwell(b), DakotaBombRun.BlastSwell(b - 0.01f) - 1e-6f, $"never shrinks ({b:0.00})");
        Assert.AreEqual(1f, DakotaBombRun.BlastAlpha(1f - SeaAirCollection.DakotaBlastFadeShare - 0.01f), 1e-5f, "whole until its fade");
        Assert.AreEqual(0f, DakotaBombRun.BlastAlpha(1f), 1e-5f, "gone at its end");

        var map = OpenMap();
        var (_, _, loops, _) = DrawnDakotas(map);
        var loop = loops[0]; var run = loop.Bombs!; var first = run.Drops[0];
        float impact = loop.Lag + first.At + SeaAirCollection.DakotaBombFallS, life = SeaAirCollection.DakotaBlastS;
        int Here() { for (int s = 0; s < run.Blasts.Count; s++) if (run.Blasts[s].enabled && Xz(run.Blasts[s].transform.position - first.To).magnitude < 1f) return s; return -1; }
        int lastFrame = -1; float lastSize = 0f;
        foreach (float b in new[] { 0.02f, 0.15f, 0.3f, 0.6f, 0.9f, 0.99f })
        {
            loop.Place(impact + b * life);
            int s = Here();
            Assert.GreaterOrEqual(s, 0, $"the blast shows at the impact at {b:0.00} of its life");
            var r = run.Blasts[s];
            int frame = run.BlastFrame(s);
            Assert.That(frame, Is.InRange(0, SeaAirCollection.DakotaBlastGrid * SeaAirCollection.DakotaBlastGrid - 1), "a frame of the flipbook");
            Assert.Greater(frame, lastFrame, $"its frames run forward ({b:0.00})");
            Assert.AreEqual(SeaAirCollection.DakotaBlastM * DakotaBombRun.BlastSwell(b), r.transform.localScale.x, 1f, $"its size at {b:0.00}");
            Assert.GreaterOrEqual(r.transform.localScale.x, lastSize - 1e-3f, $"it never shrinks ({b:0.00})");
            Assert.AreEqual(DakotaBombRun.BlastAlpha(b), DakotaBombRun.Colour(r).a, 1e-3f, $"its alpha at {b:0.00}");
            Assert.Less(r.transform.position.y - 0.5f * r.transform.localScale.y, first.To.y + 1f, "its foot at or under the ground");
            lastFrame = frame; lastSize = r.transform.localScale.x;
        }
        Assert.AreEqual(SeaAirCollection.DakotaBlastGrid * SeaAirCollection.DakotaBlastGrid - 1, lastFrame, "it ends on the flipbook's last frame");
        loop.Place(impact + 0.02f * life); float early = DakotaBombRun.Colour(run.Blasts[Here()]).a;
        loop.Place(impact + 0.99f * life); float late = DakotaBombRun.Colour(run.Blasts[Here()]).a;
        Assert.Greater(early, 0.99f, "bright at the start"); Assert.Less(late, 0.05f, "all but gone at the end");
        loop.Place(impact + life + 0.01f);
        Assert.AreEqual(-1, Here(), "gone after its life");
    }

    /// <summary>The owner, 4 November 2026: "there is no smoke columns coming up". Each Dakota lights at most DakotaColumns
    /// smoke columns over Tel Aviv, at impacts of its own (the pair at most twice that, never two on one spot), each made of
    /// DakotaColumnPuffs puffs. A column shows nothing before its first hit; after it, it grows from the ground (its top
    /// higher a few seconds later), and from one DakotaColumnLife on it stands whole: every puff showing, its top near
    /// DakotaColumnM up, wider aloft than at its foot, leaning downwind (east) less than DakotaBombMarginM, so it stays over
    /// Tel Aviv's hex. It stays through the pass back and the next loops (as long as the Dakotas fly). The same time draws
    /// the same column.</summary>
    [Test]
    public void TheBombsLightSmokeColumnsThatGrowThenStandOverTelAviv()
    {
        var map = OpenMap();
        var (_, planes, loops, _) = DrawnDakotas(map);
        float period = SeaAirCollection.DakotaLoopSeconds, life = SeaAirCollection.DakotaColumnLifeS;
        int P = SeaAirCollection.DakotaColumnPuffs;
        var all = new List<Vector3>();
        for (int p = 0; p < planes.Length; p++)
        {
            var loop = loops[p]; var run = loop.Bombs!;
            int sites = run.ColumnSites.Count;
            Assert.That(sites, Is.InRange(1, SeaAirCollection.DakotaColumns), $"{planes[p].name} lights one to {SeaAirCollection.DakotaColumns} columns");
            Assert.AreEqual(sites * P, run.Columns.Count, "a pool of puffs for each column, no more");
            foreach (var site in run.ColumnSites)
            {
                Assert.IsTrue(run.Drops.Any(d => (d.To - site).magnitude < 1e-3f), "each column stands where a bomb lands");
                Assert.IsTrue(all.All(o => Xz(o - site).magnitude > 1f), "never two columns on one spot");
                all.Add(site);
            }
            for (int c = 0; c < sites; c++)
            {
                var site = run.ColumnSites[c]; float from = (float)run.ColumnFrom(c);
                IEnumerable<Renderer> Puffs() => run.Columns.Skip(c * P).Take(P);
                float Top() => Puffs().Where(r => r.enabled).Select(r => r.transform.position.y - site.y).DefaultIfEmpty(0f).Max();
                loop.Place(loop.Lag + from - 0.5f);
                Assert.IsFalse(Puffs().Any(r => r.enabled), $"{planes[p].name} column {c}: nothing before its first hit");
                loop.Place(loop.Lag + from + 2f); float young = Top();
                Assert.IsTrue(Puffs().Any(r => r.enabled), "it rises from its first hit");
                Assert.Less(young, 0.5f * SeaAirCollection.DakotaColumnM, "low at first");
                loop.Place(loop.Lag + from + 8f); float older = Top();
                Assert.Greater(older, young + 1000f, "it grows upward");
                foreach (float later in new[] { 1.5f * life, 0.5f * period, period + 3f, 3f * period + 7.5f })
                {
                    loop.Place(loop.Lag + from + later);
                    var on = Puffs().Where(r => r.enabled).ToList();
                    Assert.AreEqual(P, on.Count, $"whole {later:0.0} s after its first hit");
                    var top = on.OrderBy(r => r.transform.position.y).Last(); var foot = on.OrderBy(r => r.transform.position.y).First();
                    Assert.Greater(top.transform.position.y - site.y, 0.7f * SeaAirCollection.DakotaColumnM, "standing tall");
                    Assert.Greater(top.transform.localScale.x, foot.transform.localScale.x, "wider aloft");
                    Assert.Greater(Vector3.Dot(top.transform.position - site, new Vector3(0.94f, 0f, 0.34f)), 100f, "its top leans downwind (east)");
                    foreach (var r in on)
                    {
                        Assert.Less(Xz(r.transform.position - site).magnitude, SeaAirCollection.DakotaBombMarginM, "it leans less than the margin");
                        Assert.IsTrue(OnTelAviv(map, r.transform.position), $"over Tel Aviv ({r.transform.position})");
                        Assert.That(run.ColumnFrame(run.Columns.ToList().IndexOf(r)), Is.InRange(0, SeaAirCollection.DakotaSmokeGrid * SeaAirCollection.DakotaSmokeGrid - 1), "a smoke flipbook frame");
                    }
                }
                // The same time draws the same column.
                float t = loop.Lag + from + 41.3f;
                loop.Place(t); var a = Puffs().Select(r => (r.transform.position, r.transform.localScale, DakotaBombRun.Colour(r))).ToList();
                loop.Place(t + 17f); loop.Place(t); var b = Puffs().Select(r => (r.transform.position, r.transform.localScale, DakotaBombRun.Colour(r))).ToList();
                CollectionAssert.AreEqual(a, b, "a pure function of the loop's clock");
            }
        }
        Assert.LessOrEqual(all.Count, SeaAirCollection.DakotaColumns * planes.Length, "capped");
        TestContext.WriteLine($"{all.Count} smoke columns over Tel Aviv");
    }

    /// <summary>Sampling the bombs, blasts, dust and columns, and turning them to a camera, allocates nothing (DakotaBombRun),
    /// as the gun's shot (UnitGunFireTests).</summary>
    [Test]
    public void SamplingTheBombRunAllocatesNothing()
    {
        var map = OpenMap();
        var (_, _, loops, _) = DrawnDakotas(map);
        var run = loops[0].Bombs!; var view = Quaternion.Euler(50f, 0f, 0f);
        float t = run.Drops[0].At;
        TestDelegate frames = () => { for (int i = 0; i < 200; i++) { t += 0.05f; run.Sample(t); run.Face(view); } };
        frames();   // Mono JIT-compiles the delegate on its first call (UnitGunFireTests)
        t = run.Drops[0].At;
        Assert.That(frames, UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        // The measured frames ran from the first release through ten seconds of the loop; half a second after the first
        // impact, inside them, a blast, dust and a column were showing.
        run.Sample(run.Drops[0].At + SeaAirCollection.DakotaBombFallS + 0.5f);
        Assert.IsTrue(run.Blasts.Any(r => r.enabled) && run.Smoke.Any(r => r.enabled) && run.Columns.Any(r => r.enabled), "the measured frames drew blasts, dust and columns");
    }

    /// <summary>#2482's margin at its edge (review of #2490): OnDakotaTarget refuses a point 1 m short of
    /// DakotaBombMarginM inside Tel Aviv's hex and accepts one 1 m past it, so dropping its edge check turns this red.</summary>
    [Test]
    public void ABombPointOneMetreShortOfTheMarginIsOffTheTargetAndOneMetrePastItIsOn()
    {
        var map = OpenMap();
        var ground = PlacementLayer.CampGround(map);
        float size = ground.HexSizeM, inradius = 0.5f * Mathf.Sqrt(3f) * size;
        var h = C.SettlementHexes.First(x => x.Id == SeaAirCollection.DakotaTown);
        var centre = HexGrid.Centre(h.Q, h.R, size);
        var targets = new List<Vector2> { Xz(HexGrid.ToWorld(centre, ground.TerrainOrigin, ground.Length)) };
        // Due east of the centre the nearest side is the flat east side, so a point d metres east is inradius - d inside it.
        Vector2 At(float inside) => Xz(HexGrid.ToWorld(centre + new Vector2(inradius - inside, 0f), ground.TerrainOrigin, ground.Length));
        Assert.IsFalse(SeaAirCollection.OnDakotaTarget(ground, targets, At(SeaAirCollection.DakotaBombMarginM - 1f)), "1 m short of the margin");
        Assert.IsTrue(SeaAirCollection.OnDakotaTarget(ground, targets, At(SeaAirCollection.DakotaBombMarginM + 1f)), "1 m past the margin");
    }

    /// <summary>#2482's margin, checked on its own (review of #2490): every bomb's release point and impact point lies at
    /// least SeaAirCollection.DakotaBombMarginM inside the edge of one of Tel Aviv's hexes, so the smoke's drift stays on the
    /// hex. The distance is measured here from the hex's own six corners (pointy-top, circumradius the map's hex size, as
    /// tools/hexgrid.py lays them), as the point's distance to the nearest of the six sides, with the point inside all six;
    /// it does not call the layer's own OnDakotaTarget.</summary>
    [Test]
    public void EveryDakotaBombIsReleasedAndLandsAtLeastTheMarginInsideTelAvivsHex()
    {
        // Pinned, so lowering the constant cannot weaken the code and this test together.
        Assert.AreEqual(600f, SeaAirCollection.DakotaBombMarginM, "the bombs fall at least 600 m inside Tel Aviv's hex");
        var map = OpenMap();
        var (_, planes, loops, _) = DrawnDakotas(map);
        var ground = PlacementLayer.CampGround(map);
        float size = ground.HexSizeM;
        var hexes = new List<Vector2[]>();
        foreach (var h in C.SettlementHexes.Where(x => x.Id == SeaAirCollection.DakotaTown))
        {
            var centre = HexGrid.Centre(h.Q, h.R, size);
            var corners = new Vector2[6];
            for (int k = 0; k < 6; k++)
            {
                float a = (60f * k - 30f) * Mathf.Deg2Rad;
                corners[k] = Xz(HexGrid.ToWorld(centre + size * new Vector2(Mathf.Cos(a), Mathf.Sin(a)), ground.TerrainOrigin, ground.Length));
            }
            hexes.Add(corners);
        }
        Assert.Greater(hexes.Count, 0, "premise: Tel Aviv has its hex");
        // Inside a convex polygon: on the same side of every side; then the distance to the nearest side. Negative outside.
        float Inside(Vector2[] corners, Vector2 p)
        {
            float sign = 0f, nearest = float.MaxValue;
            for (int k = 0; k < 6; k++)
            {
                Vector2 a = corners[k], b = corners[(k + 1) % 6], ab = b - a;
                float cross = ab.x * (p.y - a.y) - ab.y * (p.x - a.x);
                if (sign == 0f) sign = Mathf.Sign(cross);
                else if (Mathf.Sign(cross) != sign) return -1f;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                nearest = Mathf.Min(nearest, (p - (a + t * ab)).magnitude);
            }
            return nearest;
        }
        float closest = float.MaxValue; int points = 0;
        for (int p = 0; p < planes.Length; p++)
        {
            var run = loops[p].Bombs;
            Assert.IsNotNull(run, $"{planes[p].name} carries its bombs");
            Assert.Greater(run!.Drops.Count, 0, $"{planes[p].name} drops bombs");
            foreach (var d in run.Drops)
                foreach (var (what, at) in new[] { ("release", d.From), ("impact", d.To) })
                {
                    float inside = hexes.Max(corners => Inside(corners, Xz(at)));
                    Assert.GreaterOrEqual(inside, SeaAirCollection.DakotaBombMarginM,
                        $"{planes[p].name}'s {what} at {d.At:0.0} s ({at}) is {inside:0} m inside Tel Aviv's hex edge");
                    closest = Mathf.Min(closest, inside); points++;
                }
        }
        TestContext.WriteLine($"{points} release and impact points; the nearest is {closest:0} m inside the hex's edge (margin {SeaAirCollection.DakotaBombMarginM:0} m)");
    }

    /// <summary>Egypt's flag as the owner ruled for the Spitfire on #2260 ("Smaller flag, no pole"): on each Dakota the cloth alone,
    /// in Egypt's sourced colours, half the tokens' flag size, floating over the tail and clear above the aircraft at every
    /// point of its loop (banked or not), turned to the camera, and gone with the aircraft. Changed from #2274's test: both
    /// aircraft are checked, each on its own clock, and both keys are gone after the window.</summary>
    [Test]
    public void TheDakotasFlyEgyptsFlagOverTheirTailsWithNoPole()
    {
        var map = OpenMap();
        var (layer, planes, loops, _) = DrawnDakotas(map);
        var colours = Statecraft.Game.Map.Units.UnitFlags.Colours(Statecraft.Game.Map.Units.UnitFlags.Egypt);
        for (int p = 0; p < planes.Length; p++)
        {
            var loop = loops[p]; var plane = planes[p];
            var flag = loop.Flag;
            Assert.IsNotNull(flag, "a flag over the aircraft");
            Assert.IsNotNull(flag!.GetComponent<Statecraft.Game.Map.Units.UnitFlagFacing>(), "turned to the camera, as the tokens' flags");
            var mesh = flag.GetComponent<MeshFilter>().sharedMesh;
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
                var h = SeaAirCollection.DakotaLoopHeading(t - loop.Lag);
                Assert.Greater(Vector3.Dot(flag.position - b.center, -new Vector3(h.x, 0f, h.y)), 0.3f * b.extents.x, $"over the tail, behind the centre, at {t:0.0} s");
            }
            var fbNow = flag.GetComponent<MeshRenderer>().bounds; var keepNow = DakotaKeepOf(layer, plane.gameObject)!.Value;
            Assert.IsTrue(keepNow.Contains(fbNow.min) && keepNow.Contains(fbNow.max), "the labels keep clear of the flag too, where it is now");
        }
        CollectionGeometryFixture.Craft(map, new DateTime(1948, 6, 15), Array.Empty<Placement>(), C);
        AnswerAll();
        foreach (var k in SeaAirCollection.DakotaKeys)
        {
            Assert.IsNull(layer.transform.Find(k), $"no {k} after its window");
            Assert.IsNull(layer.transform.Find(k + SeaAirCollection.DakotaMarksSuffix), "its flag, shadow and bombs go with it");
        }
        Assert.AreEqual(0, Object.FindObjectsByType<DakotaLoop>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
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
        bool readable = Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable;
        Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = true;   // the disc's vertices are read below
        try { PropellerTurnsAtItsNose(); }
        finally { Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = readable; }
    }

    void PropellerTurnsAtItsNose()
    {
        var map = OpenMap();
        var (_, plane, loop, centre) = DrawnSpitfire(map);
        var airframe = plane.GetComponentInChildren<MeshFilter>();
        var prop = airframe.transform.Find(SeaAirCollection.SpitfirePropellerName);
        Assert.IsNotNull(prop, "a propeller disc, a child of the aircraft's mesh");
        Assert.AreEqual(ShadowCastingMode.Off, prop.GetComponent<MeshRenderer>().shadowCastingMode, "a blur casts no shadow");
        // The coordinator-approved plan, step 1 (5 November 2026): a light haze, not the dark disc it was.
        var colours = prop.GetComponent<MeshFilter>().sharedMesh.colors32;
        Assert.IsTrue(colours.All(c => c.r > 180 && c.g > 180 && c.b > 180), "a light blur");
        Assert.That(colours.Max(c => c.a) / 255f, Is.InRange(0.3f, 0.6f), "see-through");
        Assert.AreEqual(0, colours.Min(c => c.a), "clear at its rim");
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
            Assert.Less((all.min - own.min).magnitude + (all.max - own.max).magnitude, 0.5f, $"the disc's box stays inside the airframe's at {t:0.000} s");
            // The geometry too, not only the renderer's box (review of #2260): every disc vertex inside the airframe's box.
            var grown = own; grown.Expand(1f);
            foreach (var v in prop.GetComponent<MeshFilter>().sharedMesh.vertices)
                Assert.IsTrue(grown.Contains(prop.TransformPoint(v)), $"the disc's vertex {v} inside the airframe's box at {t:0.000} s");
        }
        // Long into a session the angle stays exact (review of #2260: the product with the clock lost whole degrees):
        // 36000.25 s is 150,001 turns and a twenty-fourth, 15 degrees.
        loop.Place(36000.25f);
        Assert.Less(Quaternion.Angle(prop.localRotation, Quaternion.AngleAxis(-15f, Vector3.up)), 0.1f, "15 degrees round after ten hours");

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

    /// <summary>The coordinator-approved Spitfire plan, step 2 (5 November 2026): the streamed model is fully matte, so the
    /// layer gives it a glossy copy of its material (the same shader and map, smoothness UnitShowcase.AircraftSmoothness), and
    /// leaves the streamed material itself as it was.</summary>
    [Test]
    public void TheSpitfireWearsAGlossyCopyOfItsMatteMaterial()
    {
        var map = OpenMap();
        var (_, plane, _, _) = DrawnSpitfire(map);
        var airframe = plane.GetComponentInChildren<MeshFilter>().GetComponent<MeshRenderer>();
        var gloss = airframe.sharedMaterial;
        Assert.IsTrue(gloss.name.EndsWith(" (glossy)"), "a copy, not the streamed material");
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitShowcase.AircraftSmoothness, gloss.GetFloat("_Smoothness"), 1e-6f);
        var name = gloss.name.Substring(0, gloss.name.Length - " (glossy)".Length);
        var sources = Resources.FindObjectsOfTypeAll<Material>().Where(m => m.name == name).ToList();
        Assert.IsNotEmpty(sources, "the streamed material it copies");
        foreach (var source in sources)
        {
            Assert.AreEqual(0f, source.GetFloat("_Smoothness"), 1e-6f, "the streamed material stays matte");
            Assert.AreSame(source.shader, gloss.shader);
            Assert.AreSame(source.GetTexture("_BaseMap"), gloss.GetTexture("_BaseMap"), "the same map");
        }
    }

    /// <summary>The owner's report: "it doesnt look fliying". The aircraft flies well clear of the ground at every point of its
    /// loop (UnitShowcase.AltitudeM over the highest ground under the loop), and a soft dark disc, as long and as wide as the
    /// aircraft seen from above and fading to nothing at its rim, lies on the ground where the map's sun (MapLighting) casts
    /// it: on the sun's ray through the aircraft, east-south-east of it, turned with its heading, a little over the ground
    /// under it and inside the board. The ray starts at SpitfireLoop.ShadowHeightShare of the aircraft's height over the
    /// surface under it, so the shadow lies near enough to read as the aircraft's (e21's captures, 5 November 2026: about
    /// 7.5 km away at the full height).</summary>
    [Test]
    public void TheSpitfireFliesHighOverTheGroundAndItsSoftShadowLiesDownSunOnTheBoard()
    {
        var map = OpenMap();
        bool readable = Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable;
        Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = true;
        var sun = new GameObject("TEST ONLY sun").AddComponent<Light>();
        var sunWas = RenderSettings.sun;
        try
        {
            sun.type = LightType.Directional; MapLighting.Apply(sun);
            Assert.Greater(Vector3.Dot(sun.transform.forward, SpitfireLoop.SunRay), 0.9999f, "the map's sun");
            var (layer, plane, loop, centre) = DrawnSpitfire(map);
            var shadow = Marks(layer, SeaAirCollection.SpitfireShadowName);
            Assert.IsNotNull(shadow, "a ground shadow beside the aircraft");
            Assert.IsTrue(plane.GetComponentsInChildren<Renderer>(true).All(x => x.shadowCastingMode == ShadowCastingMode.Off),
                "its only shadow: the aircraft casts none (one would lie about 7.7 km off at its drawn height, e21's AFTER captures)");
            var renderer = shadow!.GetComponent<MeshRenderer>();
            Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode); Assert.IsFalse(renderer.receiveShadows);
            var alpha = shadow.GetComponent<MeshFilter>().sharedMesh.colors32.Select(c => c.a / 255f).ToList();
            Assert.That(alpha.Max(), Is.InRange(0.2f, 0.8f), "dark but see-through at its middle");
            Assert.AreEqual(0f, alpha.Min(), 1e-3f, "nothing at its rim: a soft edge");
            var ground = PlacementLayer.CampGround(map);
            float span = SeaAirCollection.SizeOf(SeaAirCollection.Spitfire), y0 = map.Terrain.transform.position.y;
            float Height(Vector3 p) => map.Terrain.SampleHeight(p) + y0;
            // Where the shadow's ray starts: ShadowHeightShare of the way up from the surface (land or sea) under the aircraft.
            Vector3 Low(Vector3 drawn) { float u = Mathf.Max(Height(drawn), ground.SeaLevelY); drawn.y = u + SpitfireLoop.ShadowHeightShare * (drawn.y - u); return drawn; }
            Assert.That(SpitfireLoop.ShadowHeightShare, Is.InRange(0.1f, 0.5f), "lower than the drawn height, still well over the ground");
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
                var low = Low(drawn);
                var cast = low + SpitfireLoop.SunRay * ((low.y - g) / -SpitfireLoop.SunRay.y);
                Assert.Less(new Vector2(s.x - cast.x, s.z - cast.z).magnitude, 0.03f * (low.y - g), $"on the sun's ray through the aircraft, from its lowered height, at {t:0.0} s");
                Assert.Greater(Vector2.Dot(new Vector2(s.x - drawn.x, s.z - drawn.z), sunFlat), 0.9f * (low.y - g), "down-sun of it");
                Assert.Less(new Vector2(s.x - drawn.x, s.z - drawn.z).magnitude, 0.6f * (drawn.y - g) / Mathf.Tan(MapLighting.SunElevation * Mathf.Deg2Rad), "nearer than the full height's ray would lay it");
                var heading = SeaAirCollection.LoopHeading(t);
                Assert.Greater(Vector3.Dot(shadow.forward, new Vector3(heading.x, 0f, heading.y)), 0.999f, "turned with its heading");
                Assert.That(shadow.lossyScale.z / span, Is.InRange(0.3f, 0.6f), "half as long as the aircraft, about");
                Assert.That(shadow.lossyScale.x / span, Is.InRange(0.4f, 0.6f), "half as wide, about (narrower in a bank)");
                float reach = Mathf.Max(shadow.lossyScale.x, shadow.lossyScale.z);
                Assert.IsTrue(ground.Inside(s.x - reach, s.z - reach) && ground.Inside(s.x + reach, s.z + reach), $"on the board at {t:0.0} s ({s})");
                Assert.GreaterOrEqual(s.y, ground.SeaLevelY + SpitfireLoop.ShadowLiftM - 0.01f, $"never under the sea's surface at {t:0.0} s");
            }
            // The scene's own sun casts it (the opt-in hex map's dusk and night presets turn the sun, HexTimeOfDay): a
            // higher sun from the south-west puts it north-east of the aircraft, nearer.
            sun.transform.rotation = Quaternion.Euler(48f, 215f - 180f, 0f);
            loop.Place(0f);
            var high = Low(centre + new Vector3(SeaAirCollection.LoopOffset(0f).x, 0f, SeaAirCollection.LoopOffset(0f).y));
            var landed = shadow.position; float under0 = Height(landed);
            var cast0 = high + sun.transform.forward * ((high.y - under0) / -sun.transform.forward.y);
            Assert.Less(new Vector2(landed.x - cast0.x, landed.z - cast0.z).magnitude, 0.03f * (high.y - under0), "on the scene sun's ray, not a fixed one");
        }
        finally { Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = readable; RenderSettings.sun = sunWas; Object.DestroyImmediate(sun.gameObject); }
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
            float t = i * period / 200f, h = 0.2f;   // a step large enough for float positions of km, small for the curve
            Vector2 p0 = SeaAirCollection.LoopOffset(t - h), p1 = SeaAirCollection.LoopOffset(t), p2 = SeaAirCollection.LoopOffset(t + h);
            Vector2 d1 = (p2 - p0) / (2f * h), d2 = (p2 - 2f * p1 + p0) / (h * h);
            float numeric = Mathf.Abs(d1.x * d2.y - d1.y * d2.x) / Mathf.Pow(d1.magnitude, 3f);
            Assert.AreEqual(numeric, SeaAirCollection.LoopCurvature(t), 0.01f * numeric, $"the path's curvature at {t:0.0} s");
            float bank = SeaAirCollection.LoopBankDegrees(t);
            Assert.That(bank, Is.InRange(0f, max + 1e-3f), $"a gentle bank at {t:0.0} s");
            if (SeaAirCollection.LoopCurvature(t) > 1.05f * least) Assert.Greater(bank, 0f, $"into the turn at {t:0.0} s");
        }

        var map = OpenMap();
        bool readable = Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable;
        Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = true;   // explicit, though only the model's own mesh is read
        try { RollsIntoTheTurns(map, period); }
        finally { Statecraft.Game.Map.Units.UnitPlinth.KeepMeshesReadable = readable; }
    }

    void RollsIntoTheTurns(MapScene map, float period)
    {
        var (_, plane, loop, centre) = DrawnSpitfire(map);
        var mesh = plane.GetComponentInChildren<MeshFilter>(); var v = mesh.sharedMesh.vertices;
        float tipX = v.Max(p => Mathf.Abs(p.x));
        Vector3 Tip(float side) { var tip = v.Where(p => p.x * side > 0.96f * tipX).ToList(); return tip.Aggregate(Vector3.zero, (a, p) => a + p) / tip.Count; }
        Vector3 left = Tip(-1f), right = Tip(1f);
        var c = new Vector2(centre.x, centre.z);
        // Densely over two periods, across the wrap at the period's end: the inner wing is never the higher one.
        for (int i = 0; i <= 400; i++)
        {
            float t = i * 2f * period / 400f;
            loop.Place(t);
            Vector3 a = mesh.transform.TransformPoint(left), b = mesh.transform.TransformPoint(right);
            bool aInner = Vector2.Distance(new Vector2(a.x, a.z), c) < Vector2.Distance(new Vector2(b.x, b.z), c);
            Vector3 inner = aInner ? a : b, outer = aInner ? b : a;
            float roll = Mathf.Asin((outer.y - inner.y) / (outer - inner).magnitude) * Mathf.Rad2Deg;
            Assert.AreEqual(SeaAirCollection.LoopBankDegrees(t), roll, 1.5f, $"the inner wing down by the bank at {t:0.0} s");
            Assert.AreEqual(0f, plane.TransformDirection(Vector3.down).y, 1e-3f, $"the nose level at {t:0.0} s");
        }
    }

    /// <summary>The owner's report: "it doesnt show its an egyptian unit". The map marks a unit's side by its side's sourced
    /// flag (UnitPlinth.Build, UnitFlags, owner ruling 31 October 2026), turned to the camera (UnitFlagFacing, owner ruling
    /// 15 October 2026). The Spitfire flies the same flag, cloth and colours, but as the owner ruled on #2260 (2 November
    /// 2026, "Smaller flag, no pole": "a small Egyptian flag pennant floating just above the tail (no pole through the
    /// plane)"): the cloth alone, smaller than the tokens' flag, floating over the aircraft's tail. In every pose of the loop
    /// and facing every way it stays upright, behind the aircraft's middle, clear above every part of the airframe under it
    /// (so it never meets the airframe, banked or not), just above the tail (not over the raised wingtip's height: from
    /// there the map's camera drew it over the cockpit, e21's captures, 5 November 2026), and inside the box the labels keep
    /// clear of.</summary>
    [Test]
    public void TheSpitfireFliesEgyptsFlagAsTheEgyptianTokensDo()
    {
        Assert.IsTrue(Statecraft.Game.Map.Units.UnitData.Sides.Contains(Statecraft.Game.Map.Units.UnitFlags.Egypt), "Egypt is a side of the units' content");
        Assert.IsTrue(Statecraft.Game.Map.Units.UnitFlags.Has(Statecraft.Game.Map.Units.UnitFlags.Egypt), "with the sourced flag its tokens fly");
        var map = OpenMap();
        var (layer, plane, loop, centre) = DrawnSpitfire(map);
        var flag = Marks(layer, SeaAirCollection.SpitfireFlagName);
        Assert.IsNotNull(flag, "a flag beside the aircraft");
        var mesh = flag!.GetComponent<MeshFilter>().sharedMesh;
        Assert.AreEqual(Statecraft.Game.Map.Units.UnitFlags.MeshName(Statecraft.Game.Map.Units.UnitFlags.Egypt), mesh.name, "Egypt's flag, built as the tokens' (UnitFlags.Build)");
        var mats = flag.GetComponent<MeshRenderer>().sharedMaterials;
        var cloth = Statecraft.Game.Map.Units.UnitFlags.Colours(Statecraft.Game.Map.Units.UnitFlags.Egypt);
        Assert.AreEqual(cloth.Count, mesh.subMeshCount, "the cloth's colours only: no pole (owner, \"Smaller flag, no pole\")");
        Assert.AreEqual(cloth.Count, mats.Length, "one cloth material a submesh");
        for (int i = 0; i < cloth.Count; i++)
            Assert.Less(Vector4.Distance(cloth[i], mats[i].color), 1e-4f, $"Egypt's cloth colour {i}, as UnitFlags.ClothMaterials gives the tokens");
        Assert.IsNotNull(flag.GetComponent<Statecraft.Game.Map.Units.UnitFlagFacing>(), "the cloth turns to the camera, as the tokens' flags do");
        float tokens = Statecraft.Game.Map.Units.UnitBadge.FigureHeightM * Statecraft.Game.Map.Units.UnitShowcase.FigureScale;
        Assert.Less(flag.lossyScale.y, 0.75f * tokens, "clearly smaller than the tokens' flag");
        Assert.AreEqual(ShadowCastingMode.Off, flag.GetComponent<MeshRenderer>().shadowCastingMode, "only the aircraft's own shadow on the ground");
        var airframeMesh = plane.GetComponentInChildren<MeshFilter>();
        var local = airframeMesh.sharedMesh.vertices;   // the streamed model is readable (the propeller test reads it too)
        float span = SeaAirCollection.SizeOf(SeaAirCollection.Spitfire);
        var keep = layer.KeepClear.Single(k => k[0].Contains(centre))[0];
        for (int i = 0; i < 32; i++)
        {
            float t = i * SeaAirCollection.SpitfireLoopSeconds / 32f;
            loop.Place(t);
            var at = SeaAirCollection.LoopOffset(t); var heading = SeaAirCollection.LoopHeading(t);
            var drawn = centre + new Vector3(at.x, 0f, at.y);
            var forward = new Vector3(heading.x, 0f, heading.y);
            var verts = local.Select(airframeMesh.transform.TransformPoint).ToArray();
            // The tail: the airframe behind 0.3 of the span back from its middle (the wing's root is well forward of it).
            float tailTop = verts.Where(v => Vector3.Dot(v - drawn, forward) < -0.3f * span).Max(v => v.y);
            Assert.Less(Vector3.Dot(flag.position - drawn, forward), -0.3f * span, $"over the tail, behind the aircraft's middle, at {t:0.0} s");
            // Clear above every part of the airframe the cloth could be over, whichever way it turns: every vertex within the
            // cloth's whole reach about its middle (it turns about its vertical) and a margin of a tenth of the span, which is
            // wider than the airframe's triangles there, lies under the cloth's lower edge.
            var clothMesh = flag.GetComponent<MeshFilter>().sharedMesh.bounds;
            float reach = new Vector2(clothMesh.extents.x, clothMesh.extents.z).magnitude * flag.lossyScale.x + 0.1f * span;
            float lower = flag.position.y - clothMesh.extents.y * flag.lossyScale.y;
            var near = verts.Where(v => new Vector2(v.x - flag.position.x, v.z - flag.position.z).magnitude < reach).ToArray();
            if (near.Length > 0) Assert.Greater(lower, near.Max(v => v.y), $"clear above every part of the airframe the cloth could be over at {t:0.0} s");
            foreach (var look in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, new Vector3(1f, 0f, 1f).normalized })
            {
                Statecraft.Game.Map.Units.UnitFlagFacing.Face(flag, flag.position + look * 50000f);
                Assert.Greater(Vector3.Dot(flag.up, Vector3.up), 0.999f, $"upright at {t:0.0} s");
                var fb = flag.GetComponent<Renderer>().bounds;
                Assert.Greater(fb.min.y, tailTop, $"above the tail at {t:0.0} s");
                Assert.Less(fb.min.y - tailTop, 0.2f * span, $"just above the tail, not up at the raised wingtip's height, at {t:0.0} s");
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
        // Update in play (not held), through the clock seam, and the flags turning to a camera (review of #2260).
        typeof(SpitfireLoop).GetField("still", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(loop, false);
        var update = typeof(SpitfireLoop).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var clock = SpitfireLoop.Clock; float now = 3f;
        try
        {
            SpitfireLoop.Clock = () => now;
            var tick = (Action)Delegate.CreateDelegate(typeof(Action), loop, update);   // a plain call, as the player loop's
            tick();
            Assert.That(() => { for (int k = 0; k < 100; k++) { now += 0.016f; tick(); } }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }
        finally { SpitfireLoop.Clock = clock; }
        var eye = new Vector3(30000f, 40000f, 150000f);
        Statecraft.Game.Map.Units.UnitFlagFacing.FaceAll(eye);
        Assert.That(() => { for (int k = 0; k < 100; k++) { eye.x += 10f; Statecraft.Game.Map.Units.UnitFlagFacing.FaceAll(eye); } }, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
    }

    /// <summary>Review of #2260: drawing, dropping and drawing the Spitfire again (its dates end and come back, the layer
    /// redraws) makes no new propeller, shadow or flag meshes or materials (its glossy paint included) past the first: the
    /// layer makes them once. When
    /// its dates end under residency, the aircraft and its shadow-and-flag object both go, and the labels keep clear of
    /// exactly the craft left.</summary>
    [Test]
    public void DrawingAndDroppingTheSpitfireAgainLeaksNoMeshesOrMaterials()
    {
        var map = OpenMap();
        int Meshes() => Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "SpitfirePropellerBlur" || m.name == "SpitfireShadow"
            || m.name == Statecraft.Game.Map.Units.UnitFlags.MeshName(Statecraft.Game.Map.Units.UnitFlags.Egypt));
        int Materials() => Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name.StartsWith("UnitFlag (egypt", StringComparison.Ordinal)
            || m.name == Statecraft.HexTiles.HexMaterials.AttackBorder || m.name.EndsWith(" (glossy)", StringComparison.Ordinal));
        SeaAirCollection Show(DateTime date) { var l = CollectionGeometryFixture.Craft(map, date, Array.Empty<Placement>(), C)!; AnswerAll(); return l; }
        var layer = Show(new DateTime(1948, 6, 15));
        Assert.IsNotNull(layer.transform.Find("el-arish-spitfire"));
        int meshes = Meshes(), materials = Materials();
        Assert.GreaterOrEqual(meshes, 3, "the blur, the shadow and the flag");
        for (int i = 0; i < 3; i++)
        {
            layer = Show(new DateTime(1949, 8, 15));
            Assert.IsNull(layer.transform.Find("el-arish-spitfire"), "no Spitfire on 15 August 1949");
            Assert.IsNull(layer.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix), "and no shadow or flag");
            Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count, "the labels keep clear of the craft left, and nothing else");
            layer = Show(new DateTime(1948, 6, 15));
            Assert.IsNotNull(layer.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix));
            Assert.AreEqual(meshes, Meshes(), $"no new meshes after {i + 1} redraws");
            Assert.AreEqual(materials, Materials(), $"no new materials after {i + 1} redraws");
        }
    }

    /// <summary>Review of #2260: under residency, when the Spitfire's dates end its model's lease is released with both its
    /// objects gone (the aircraft and its shadow-and-flag object), and their keep-clear entries with them.</summary>
    [Test]
    public void WhenItsWindowEndsTheSpitfireAndItsMarksAndTheirKeepClearGoTogether()
    {
        var map = OpenMap();
        var go = new GameObject(SeaAirCollection.Name); go.transform.SetParent(map.Terrain.transform, false);
        var layer = go.AddComponent<SeaAirCollection>();
        typeof(SeaAirCollection).GetField("map", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(layer, map);
        var released = new List<(string id, bool plane, bool marks)>();
        layer.ConfigureResidency(new CollectionResidencyPolicy(1000000, 1000001, 1000000, 1000001, int.MaxValue, "TEST ONLY"),
            (id, detail, done) => { SeaAirCollection.Request(id, detail, done); return new ReleaseProbe(() => released.Add((id,
                layer.transform.Find("el-arish-spitfire") != null, layer.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix) != null))); });
        var keepOf = (System.Collections.IDictionary)typeof(SeaAirCollection).GetField("keepOf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(layer)!;
        SeaAirCollection.ShowAt(map, new DateTime(1948, 12, 15), Array.Empty<Placement>(), C);
        AnswerAll();
        Assert.IsNotNull(layer.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix), "the shadow and flag drawn");
        Assert.AreEqual(layer.Placed.Count, keepOf.Count, "one keep-clear entry a drawn craft (none for the shadow and flag)");
        SeaAirCollection.ShowAt(map, new DateTime(1949, 1, 15), Array.Empty<Placement>(), C);
        var spitfire = released.Single(r => r.id == SeaAirCollection.Spitfire);
        Assert.IsFalse(spitfire.plane || spitfire.marks, "the aircraft and its shadow and flag are gone before the lease is released");
        Assert.IsNull(layer.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix));
        Assert.AreEqual(layer.Placed.Count, keepOf.Count, "and their keep-clear entries");
        Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count);
        foreach (System.Collections.DictionaryEntry e in keepOf) Assert.IsTrue((GameObject)e.Key != null, "no entry for a destroyed object");
    }

    /// <summary>The layer on <paramref name="date"/> as the window test above builds it (every craft resident), drawn.</summary>
    SeaAirCollection DrawnLayer(MapScene map, DateTime date)
    {
        var go = new GameObject(SeaAirCollection.Name); go.transform.SetParent(map.Terrain.transform, false);
        var layer = go.AddComponent<SeaAirCollection>();
        typeof(SeaAirCollection).GetField("map", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(layer, map);
        layer.ConfigureResidency(new CollectionResidencyPolicy(1000000, 1000001, 1000000, 1000001, int.MaxValue, "TEST ONLY"),
            (id, detail, done) => { SeaAirCollection.Request(id, detail, done); return new ReleaseProbe(() => { }); });
        SeaAirCollection.ShowAt(map, date, Array.Empty<Placement>(), C);
        AnswerAll();
        return layer;
    }

    /// <summary>The Dakotas' keep-clear boxes survive a retirement, as every other craft's do (#2274 merged with main's per-object
    /// boxes): FlyDakota keeps each by object, so ClearForRetirement's rebuild from those boxes keeps them. The retirement here
    /// is degenerate (an empty key set: no craft whose window ends inside the Dakotas', 15 May to 11 June 1948, was found), and
    /// it runs exactly that rebuild. Changed for #2482: both aircraft's boxes, by their two keys, where #2274 had one.</summary>
    [Test]
    public void ARetirementKeepsTheDakotasKeepClearBox()
    {
        var map = OpenMap();
        var layer = DrawnLayer(map, new DateTime(1948, 5, 15));
        var keepOf = (System.Collections.IDictionary)typeof(SeaAirCollection).GetField("keepOf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(layer)!;
        Assert.AreEqual(2, layer.Placed.Count(p => p.Model == SeaAirCollection.Dakota), "premise: both Dakotas are drawn");
        Assert.AreEqual(layer.Placed.Count, keepOf.Count, "one keep-clear entry a drawn craft, each Dakota's included");
        var dakotas = SeaAirCollection.DakotaKeys.Select(k => layer.transform.Find(k)!.gameObject).ToList();
        var retire = typeof(SeaAirCollection).GetMethod("ClearForRetirement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(IReadOnlyCollection<(string id, CollectionDetail detail)>) }, null)!;
        retire.Invoke(layer, new object[] { Array.Empty<(string id, CollectionDetail detail)>() });
        foreach (var dakota in dakotas) Assert.IsTrue(layer.KeepClear.Any(b => ReferenceEquals(b, keepOf[dakota])), $"{dakota.name}'s box is still kept clear");
        Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count);
    }

    /// <summary>#2351's allocation-free residency fill with the Dakotas in it: each Dakota's anchor is the ground under its
    /// loop's centre (never the Tel Aviv coast point its row names); filling allocates nothing once warm; and the fill runs
    /// only when the view's policy lets a camera scan, so the Dakotas are resident only close up. Changed for #2482: one anchor
    /// a Dakota (two), and its radius is its swept half-box's diagonal (DakotaSweptHalf), not its wingspan, because the loop
    /// over Tel Aviv is now far wider than the aircraft (the pair must fly it apart), so an aircraft can be that far from the
    /// centre.</summary>
    [Test]
    public void TheDakotasResidencyAnchorIsItsLoopCentreOnlyWhenScannedAndAllocatesNothing()
    {
        var map = OpenMap();
        var layer = DrawnLayer(map, new DateTime(1948, 5, 15));
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var fill = (Action<List<(string key, Vector3 at, float radius)>>)Delegate.CreateDelegate(
            typeof(Action<List<(string key, Vector3 at, float radius)>>), layer, typeof(SeaAirCollection).GetMethod("FillResidencyAnchors", flags)!);
        int fills = 0;
        Action<List<(string key, Vector3 at, float radius)>> counted = anchors => { fills++; fill(anchors); };
        var list = new List<(string key, Vector3 at, float radius)>();
        fill(list);
        var centre = (Vector2?)typeof(SeaAirCollection).GetMethod("DakotaCentre", flags)!.Invoke(layer, null);
        Assert.IsTrue(centre.HasValue, "premise: the Dakota's loop has a centre on this map");
        var c = centre!.Value;
        var dakota = list.Where(a => SeaAirCollection.DakotaKeys.Contains(a.key)).ToList();
        CollectionAssert.AreEqual(SeaAirCollection.DakotaKeys, dakota.Select(a => a.key), "one anchor a Dakota");
        foreach (var a in dakota)
        {
            Assert.Less((a.at - new Vector3(c.x, 0f, c.y)).magnitude, 1e-3f, "on the ground under its loop's centre");
            Assert.AreEqual(SeaAirCollection.DakotaSweptHalf.magnitude, a.radius, 1e-2f, "the reach of its loop");
        }
        var coast = SeaCraft.Coasts.Single(x => x.place == SeaAirCollection.DakotaTown);
        var coastAt = map.Frame.ToWorld(coast.lon, coast.lat, 0);
        Assert.IsFalse(dakota.Any(a => (a.at - coastAt).magnitude < 1f), "not at the Tel Aviv coast point");

        TestDelegate hundredFills = () => { for (int i = 0; i < 100; i++) { list.Clear(); fill(list); } };
        hundredFills();   // once unmeasured: the JIT, as #2351's test does
        Assert.That(hundredFills, Is.Not.AllocatingGCMemory());

        var cameraObject = new GameObject("TEST Dakota residency camera", typeof(Camera));
        try
        {
            var camera = cameraObject.GetComponent<Camera>();
            var view = new CollectionResidencyView(new CollectionResidencyPolicy(100f, 110f, 100f, 110f, 2, "TEST ONLY"));
            camera.transform.SetPositionAndRotation(new Vector3(c.x, 1000f, c.y), Quaternion.Euler(90f, 0f, 0f));
            for (int frame = 0; frame < 3; frame++) view.Observe(camera, counted, frame);
            Assert.AreEqual(0, fills, "a camera above the view height fills no anchors");
            Assert.IsFalse(SeaAirCollection.DakotaKeys.Any(view.Keys.Contains));
            camera.transform.SetPositionAndRotation(new Vector3(c.x, 10f, c.y), Quaternion.Euler(90f, 0f, 0f));
            for (int frame = 3; frame < 8; frame++) view.Observe(camera, counted, frame);
            Assert.AreEqual(5, fills, "a close camera fills the anchors once a frame");
            Assert.IsTrue(SeaAirCollection.DakotaKeys.All(view.Keys.Contains), "both Dakotas are resident close up");
            int next = 8;
            TestDelegate hundredFrames = () => { for (int i = 0; i < 100; i++) { view.Observe(camera, counted, next); view.Prune(next); next++; } };
            hundredFrames();
            Assert.That(hundredFrames, Is.Not.AllocatingGCMemory());
        }
        finally { Object.DestroyImmediate(cameraObject); }
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
                // The drawn craft and the Spitfire's flag (its shadow sits beside it too, but a shadow may fall on a model).
                var flagNow = craft.transform.Find("el-arish-spitfire" + SeaAirCollection.SpitfireMarksSuffix)?.Find(SeaAirCollection.SpitfireFlagName);
                Assert.AreEqual(date.Year == 1948, flagNow != null, $"the Spitfire's flag on {date:d MMM yyyy}");
                foreach (var t in craft.Placed.Select(p => craft.transform.Find(p.Key)).Concat(flagNow != null ? new[] { flagNow } : Array.Empty<Transform>()))
                {
                    var b = WorldBounds(t.gameObject); checkedCraft++;
                    foreach (var (owner, o) in boxes)
                        Assert.IsFalse(b.min.x < o.max.x && o.min.x < b.max.x && b.min.z < o.max.z && o.min.z < b.max.z,
                            $"{t.name} meets {owner}'s model at {o.center} on {date:d MMM yyyy}");
                }
            }
            Assert.Greater(others, 0, "the other layers drew");
            Assert.AreEqual(10, checkedCraft, "every craft checked, five on the first turn (the Spitfire too) and its flag, four on the second");
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
