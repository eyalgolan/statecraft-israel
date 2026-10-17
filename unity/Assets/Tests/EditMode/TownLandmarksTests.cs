#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #663, owner ruling of 17 October 2026: "The nine whole-town models ship (this reverses the 15 Oct 'layout
/// references only' ruling). They are slimmed, streamed after the first frame, and pass the load gate. The ma'abara and the
/// shikun appear only from their sourced 1950s dates." MATKOT's nine whole-town models stand as overview
/// landmarks beside their towns (#507's mapping), streamed through CollectionStream, clear of every kit, and they fail open.</summary>
public class TownLandmarksTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();

    [SetUp]
    public void Seam()
    {
        saved = TownLandmarks.Request;
        asked.Clear();
        TownLandmarks.Request = (id, detail, done) => asked.Add((id, detail, done));
    }

    [TearDown]
    public void Reset()
    {
        TownLandmarks.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static readonly string[] Towns =
    {
        "tel-aviv-white-city-v1", "jerusalem-stone-city-v1", "haifa-port-city-v1", "moshava-town-v1", "nahalal-moshav-v1",
        "kibbutz-compound-v2", "stone-village-v1", "maabara-camp-v1", "shikun-housing-v1",
    };

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    static GameObject Far(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    void AnswerAll()
    {
        var pending = asked.ToList();
        asked.Clear();
        foreach (var (id, _, done) in pending) done(Far(id));
    }

    [Test]
    public void EveryTownModelIsUsedAndEachIsInTheStreamedTownsBundle()
    {
        var used = TownLandmarks.ById.Values.Concat(TownLandmarks.ByType.Values).Concat(TownLandmarks.ByAction.Values.Select(v => v.Model)).Distinct();
        CollectionAssert.AreEquivalent(Towns, used, "all nine, as #507 mapped them");
        foreach (var id in Towns) Assert.AreEqual(CollectionCatalog.Towns, CollectionCatalog.BundleOf(id), id);
        Assert.AreEqual(TownLandmarks.TelAviv, TownLandmarks.ById["tel_aviv"]);
        Assert.AreEqual(TownLandmarks.Kibbutz, TownLandmarks.ByType["kibbutz"]);
    }

    [Test]
    public void TheMaabaraAndTheShikunOnlyFromTheirDates()
    {
        Assert.AreEqual(new DateTime(1950, 5, 23), TownLandmarks.MaabaraFrom, "Davar of 23 May 1950 (content/events/maabarot_policy.yaml)");
        Assert.IsNull(TownLandmarks.ModelFor(MapActions.TransitCamp, new DateTime(1950, 5, 22)));
        Assert.AreEqual(TownLandmarks.Maabara, TownLandmarks.ModelFor(MapActions.TransitCamp, new DateTime(1950, 5, 23)));
        // Owner on #917 (17 October 2026): "Use 11 Dec 1949", single-source; the exact first project is a [GAP].
        Assert.AreEqual(new DateTime(1949, 12, 11), TownLandmarks.ShikunFrom);
        Assert.IsNull(TownLandmarks.ModelFor(MapActions.Housing, new DateTime(1949, 12, 10)));
        Assert.AreEqual(TownLandmarks.Shikun, TownLandmarks.ModelFor(MapActions.Housing, new DateTime(1949, 12, 11)));
        Assert.IsNull(TownLandmarks.ModelFor(MapActions.Housing, new DateTime(1949, 11, 15)), "not on the 15 November 1949 turn");
        Assert.AreEqual(TownLandmarks.Shikun, TownLandmarks.ModelFor(MapActions.Housing, new DateTime(1949, 12, 15)), "from the 15 December 1949 turn");
        Assert.IsNull(TownLandmarks.ModelFor("no_such_action", new DateTime(1951, 1, 1)));
    }

    /// <summary>The overview switch itself: a camera at the 120 km start view draws the landmarks, one at the 60 km gameplay
    /// view does not (forceRenderingOff on every landmark renderer, as the camera is about to render).</summary>
    [Test]
    public void TheLandmarksDrawAtTheStartViewAndNotAtTheGameplayView()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), c)!;
        AnswerAll();
        var renderers = t.GetComponentsInChildren<Renderer>(true);
        Assert.Greater(renderers.Length, 0);
        Camera At(float distanceM)
        {
            var cam = new GameObject($"cam {distanceM}").AddComponent<Camera>();
            cam.transform.rotation = Quaternion.Euler(CameraRig.Pitch, 0f, 0f);
            cam.transform.position = new Vector3(50000f, 0f, 50000f) - cam.transform.forward * distanceM;
            Assert.AreEqual(distanceM, TownLandmarks.ViewDistance(cam), 1f);
            return cam;
        }
        var start = At(120000f); var play = At(60000f);
        t.Choose(default, start);
        Assert.IsTrue(renderers.All(r => !r.forceRenderingOff), "drawn at the 120 km start view");
        t.Choose(default, play);
        Assert.IsTrue(renderers.All(r => r.forceRenderingOff), "hidden at the 60 km gameplay view");
        Object.DestroyImmediate(start.gameObject); Object.DestroyImmediate(play.gameObject);
    }

    [Test]
    public void TheOverviewSwitchHasHysteresis()
    {
        // Owner on #917 (17 October 2026): "Larger, from 120 km" (enter at 110 km).
        var rig = new GameObject("rig").AddComponent<CameraRig>();
        try { Assert.IsTrue(TownLandmarks.OverviewAt(rig.Distance, null), "the starting view (CameraRig.Distance, 120 km)"); }
        finally { Object.DestroyImmediate(rig.gameObject); }
        Assert.IsTrue(TownLandmarks.OverviewAt(110000f, null), "on from 110 km");
        Assert.IsFalse(TownLandmarks.OverviewAt(109000f, null));
        Assert.IsFalse(TownLandmarks.OverviewAt(60000f, null), "gameplay03: the per-building kits draw");
        Assert.IsFalse(TownLandmarks.OverviewAt(60000f, true), "gameplay03, coming in from the overview");
        Assert.IsTrue(TownLandmarks.OverviewAt(250000f, null), "the farthest zoom");
        Assert.IsTrue(TownLandmarks.OverviewAt(100000f, true), "stays on in the band");
        Assert.IsFalse(TownLandmarks.OverviewAt(100000f, false), "stays off in the band");
        Assert.IsFalse(TownLandmarks.OverviewAt(97000f, true));
        Assert.Greater(TownLandmarks.OverviewEnterM / TownLandmarks.OverviewExitM, 1f / (1f - CameraController.ZoomPerStep),
            "the band is wider than one wheel notch");
        Assert.AreEqual(6000f, TownLandmarks.SizeM, "about 6 km across");
        Assert.AreEqual(CameraController.MaxDistance, 250000f, "the overview is within the camera's reach");
    }

    /// <summary>Owner, 18 October 2026: the models "seem to be on their side on the map". Every Collection Far prefab's root
    /// carries the FBX axis conversion (-90 degrees about X; the .fbx.meta files have bakeAxisConversion 0), so a landmark
    /// must keep the prefab's own rotation under its yaw: its rotation is the yaw times the prefab's, and its height on the
    /// map is the model's own height (at the landmark's scale), not its length.</summary>
    [Test]
    public void EveryLandmarkStandsUprightAsItsModelDoes()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), c)!;
        AnswerAll();
        Assert.Greater(t.Placed.Count, 0);
        var heights = new Dictionary<string, float>();
        foreach (var (key, model, _) in t.Placed)
        {
            var prefab = Far(model);
            var child = t.transform.Find(key);
            Assert.IsNotNull(child, key);
            var want = t.transform.rotation * Quaternion.Euler(0f, TownLandmarks.YawOf(model), 0f) * prefab.transform.localRotation;
            Assert.Less(Quaternion.Angle(want, child!.rotation), 0.01f, $"{key} ({model}) keeps its prefab's rotation under its yaw");
            if (!heights.TryGetValue(model, out var h))
            {
                // The model as its prefab stands (unparented, its own rotation): a yaw about the vertical keeps this height.
                var probe = Object.Instantiate(prefab);
                try { heights[model] = h = WorldBounds(probe).size.y / probe.transform.localScale.y; }
                finally { Object.DestroyImmediate(probe); }
            }
            float scale = child.localScale.y;
            Assert.AreEqual(h * scale, WorldBounds(child.gameObject).size.y, 0.01f * h * scale, $"{key} ({model}) is as tall as its model");
        }
    }

    static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    [Test]
    public void ASpotIsTheFirstFreeOneAndNoneWhenAllAreTaken()
    {
        var town = new Vector2(10000f, 10000f);
        var discs = new List<(Vector2 centre, float radius)> { (town, 1500f), (town + new Vector2(4000f, 4000f), 2500f) };
        var spot = TownLandmarks.Spot(town, 1500f, 2000f, (p, r) => TownLandmarks.Clear(p, r, discs));
        Assert.IsTrue(spot.HasValue);
        Assert.IsTrue(TownLandmarks.Clear(spot!.Value, 2000f, discs), "clear of every disc");
        Assert.Less((spot.Value - town).magnitude, 1500f + 2000f * (TownLandmarks.Rings + 1), "beside the town");
        Assert.IsNull(TownLandmarks.Spot(town, 1500f, 2000f, (p, r) => false));
    }

    /// <summary>Nothing is drawn until the stream answers (it never serves a request in the frame that made it), and only
    /// the strategic zoom's mesh is asked for, once per model.</summary>
    [Test]
    public void TheLandmarksStreamInAndStandClearOfEveryKitAndEachOther()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), c)!;
        Assert.IsNotNull(t);
        CollectionAssert.IsEmpty(t.Placed, "nothing before the stream answers");
        Assert.AreEqual(0, t.transform.childCount);
        Assert.Greater(asked.Count, 0);
        Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Far), "the far mesh only");
        Assert.AreEqual(asked.Count, asked.Select(a => a.id).Distinct().Count(), "each model once");
        CollectionAssert.DoesNotContain(asked.Select(a => a.id), TownLandmarks.Maabara, "no ma'abara in 1949");
        CollectionAssert.DoesNotContain(asked.Select(a => a.id), TownLandmarks.Shikun);
        AnswerAll();

        CollectionAssert.IsEmpty(t.Missing, "every wanted landmark found a spot");
        Assert.AreEqual(t.Wanted.Count, t.Placed.Count);
        Assert.AreEqual(1, t.Placed.Count(p => p.Key == "tel_aviv" && p.Model == TownLandmarks.TelAviv));
        Assert.AreEqual(map.Settlements.Count(s => s.Type == "kibbutz"), t.Placed.Count(p => p.Model == TownLandmarks.Kibbutz));
        // Owner on #917 (17 October 2026): "Keep at all seven" arab_town rows.
        Assert.AreEqual(7, map.Settlements.Count(s => s.Type == "arab_town"));
        Assert.AreEqual(7, t.Placed.Count(p => p.Model == TownLandmarks.StoneVillage), "the stone village at all seven");
        float r = 0.5f * TownLandmarks.SizeM;
        var at = t.Placed.Select(p => new Vector2(p.At.x, p.At.z)).ToList();
        foreach (var (p, (key, _, _)) in at.Zip(t.Placed, (p, q) => (p, q)))
        {
            foreach (var f in map.KitFootprints)
                Assert.GreaterOrEqual((p - f.centre).magnitude, f.radius + r - 1f, $"{key} stands on {f.id}'s kit");
            foreach (var q in at) if (q != p) Assert.GreaterOrEqual((p - q).magnitude, 2f * r - 1f, $"{key} on another landmark");
            var w = t.Wanted.Single(x => x.Key == key);
            Assert.Less((p - w.At).magnitude, w.Radius + r * (TownLandmarks.Rings + 1), $"{key} is beside its town");
        }
        foreach (Transform child in t.transform)
        {
            var b = child.GetComponentsInChildren<Renderer>().Select(x => x.bounds).Aggregate((a, x) => { a.Encapsulate(x); return a; });
            Assert.AreEqual(TownLandmarks.SizeM, Mathf.Max(b.size.x, b.size.z), 1f, child.name + " is drawn at the landmark size");
            var spot = t.Placed.Single(p => p.Key == child.name).At;
            Assert.Less(new Vector2(b.center.x - spot.x, b.center.z - spot.z).magnitude, 1f, child.name + " is centred on its spot");
            Assert.AreEqual(spot.y, b.min.y, 1f, child.name + " stands on the ground");
        }
    }

    [Test]
    public void TheMaabaraLandmarkComesWithATransitCampFromItsDate()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = c.SettlementHexes.Single(s => s.Id == "revivim");
        var camp = new[] { new Placement(MapActions.TransitCamp, hex.Q, hex.R, new DateTime(1950, 2, 1)) };
        var t = TownLandmarks.ShowAt(map, new DateTime(1950, 5, 22), camp, c)!;
        AnswerAll();
        CollectionAssert.DoesNotContain(t.Placed.Select(p => p.Model), TownLandmarks.Maabara, "not before 23 May 1950");
        TownLandmarks.ShowAt(map, new DateTime(1950, 5, 23), camp, c);
        Assert.AreEqual(1, asked.Count(a => a.id == TownLandmarks.Maabara), "asked once it is wanted");
        AnswerAll();
        Assert.AreEqual(1, t.Placed.Count(p => p.Model == TownLandmarks.Maabara && p.Key == $"{MapActions.TransitCamp}_{hex.Q}_{hex.R}"));
    }

    /// <summary>Owner on #917 (17 October 2026): the shikun "Use 11 Dec 1949". A housing project draws its shikun landmark
    /// from the 15 December 1949 turn, the first on or after that date, and not on the 15 November 1949 turn.</summary>
    [Test]
    public void TheShikunLandmarkComesWithAHousingProjectFromTheDecember1949Turn()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = c.SettlementHexes.Single(s => s.Id == "revivim");
        var housing = new[] { new Placement(MapActions.Housing, hex.Q, hex.R, new DateTime(1949, 11, 15)) };
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 11, 15), housing, c)!;
        AnswerAll();
        CollectionAssert.DoesNotContain(t.Placed.Select(p => p.Model), TownLandmarks.Shikun, "not on the 15 November 1949 turn");
        CollectionAssert.DoesNotContain(asked.Select(a => a.id), TownLandmarks.Shikun);
        TownLandmarks.ShowAt(map, new DateTime(1949, 12, 15), housing, c);
        Assert.AreEqual(1, asked.Count(a => a.id == TownLandmarks.Shikun), "asked once it is wanted");
        AnswerAll();
        Assert.AreEqual(1, t.Placed.Count(p => p.Model == TownLandmarks.Shikun && p.Key == $"{MapActions.Housing}_{hex.Q}_{hex.R}"));
    }

    [Test]
    public void AModelThatDoesNotStreamWarnsAndTheMapRunsOn()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), c)!;
        var tel = asked.Single(a => a.id == TownLandmarks.TelAviv);
        LogAssert.Expect(LogType.Warning, new Regex("^LANDMARKS: the town model tel-aviv-white-city-v1 did not stream"));
        tel.done(null);
        Assert.AreEqual(0, t.transform.childCount);
        foreach (var (id, _, done) in asked.Where(a => a.id != TownLandmarks.TelAviv).ToList()) done(Far(id));
        Assert.Greater(t.Placed.Count, 0, "the others are drawn");
        CollectionAssert.DoesNotContain(t.Placed.Select(p => p.Key), "tel_aviv");
        CollectionAssert.Contains(t.Missing, "tel_aviv");
    }
}
