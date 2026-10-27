#nullable enable
using System;
using System.Collections;
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

/// <summary>Issue #663, owner ruling of 17 October 2026: "The nine whole-town models ship (this reverses the 15 Oct 'layout
/// references only' ruling). They stream after the first frame; C89/C93 bounds their original-file residency by camera focus. The ma'abara and the
/// shikun appear only from their sourced 1950s dates." Tal Heuberger's nine whole-town models stand as overview
/// landmarks beside their towns (#507's mapping), streamed through CollectionStream, clear of every kit, and they fail open.</summary>
public class TownLandmarksTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly Dictionary<string, Camera> geometryCameras = new Dictionary<string, Camera>(StringComparer.Ordinal);
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();

    [SetUp]
    public void Seam()
    {
        saved = TownLandmarks.Request;
        asked.Clear(); geometryCameras.Clear();
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
        "tel-aviv-white-city-v1", "jerusalem-stone-city-v1-3denhance", "haifa-port-city-v1", "moshava-town-v1", "nahalal-moshav-v1",
        "kibbutz-compound-v2", "stone-village-v1", "maabara-camp-v1", "shikun-housing-v1",
    };

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        ResidencyCall(map.GetComponentInChildren<TownModelLayer>(true), typeof(TownModelLayer), "ConfigureResidency",
            1000f, 2000f, 1000f, 2, "test fixture only; not measured");
        return map;
    }

    static GameObject Far(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    // Geometry tests explicitly admit the complete fixture catalog; these cameras only check geometry and seats.
    // Production camera isolation/admission is checked independently by ResidencyFixture below.
    void AdmitGeometryTowns(MapScene map, TownLandmarks towns)
    {
        var layer = map.GetComponentInChildren<TownModelLayer>(true);
        var places = (IReadOnlyList<(string id, UnityEngine.Bounds bounds)>)typeof(TownModelLayer).GetField("places", ResidencyFlags)!.GetValue(layer)!;
        Assert.IsNotEmpty(places);
        // TEST ONLY: these geometry/seat checks intentionally cover every original town, not shipping admission.
        // Derive the allowance from this fixture's extent; focused admission is checked by ResidencyFixture below.
        float coverage = Mathf.Max(1f, places.Max(p => p.bounds.max.x) - places.Min(p => p.bounds.min.x)
            + places.Max(p => p.bounds.max.z) - places.Min(p => p.bounds.min.z));
        typeof(TownModelLayer).GetField("EdgeAllowance", ResidencyFlags)!.SetValue(layer,
            new Func<IReadOnlyList<string>, string, bool>((_, __) => true));
        ResidencyCall(layer, typeof(TownModelLayer), "ConfigureResidency", coverage, coverage, coverage, places.Count,
            "TEST ONLY all-town geometry coverage; NOT measured or accepted residency limits");
        foreach (var want in towns.Wanted)
        {
            if (!geometryCameras.TryGetValue(want.Key, out var camera))
                geometryCameras[want.Key] = camera = new GameObject("geometry owner " + want.Key).AddComponent<Camera>();
            var at = places.First(p => p.id == want.Key).bounds.center;
            at.y = 0; // Aim at the same ground plane used by focused admission, not the building bounds' height.
            camera.transform.position = at + new Vector3(0, 40000, -40000); camera.transform.LookAt(at);
            towns.Choose(default, camera);
            Assert.IsTrue((bool)ResidencyCall(layer, typeof(TownModelLayer), "WantsWholeTown", want.Key, camera)!, want.Key);
        }
    }
    void AnswerAll()
    {
        var map = Object.FindFirstObjectByType<MapScene>(); var towns = Object.FindFirstObjectByType<TownLandmarks>();
        AdmitGeometryTowns(map, towns);
        var pending = asked.ToList();
        asked.Clear();
        foreach (var (id, _, done) in pending) done(Far(id));
    }

    [Test]
    public void EveryTownModelIsUsedAndEachIsInTheStreamedTownsBundle()
    {
        var used = TownLandmarks.ById.Values.Concat(TownLandmarks.ByType.Values).Concat(TownLandmarks.ByAction.Values.Select(v => v.Model)).Distinct();
        CollectionAssert.AreEquivalent(Towns, used, "all nine, as #507 mapped them, with #336's second Jerusalem for #507's (owner, 21 October 2026, #708)");
        foreach (var id in Towns) Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id), id);
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

    /// <summary>The owner, 21 October 2026, on #708 (issuecomment-6323524970): #336's second Jerusalem (#886) "is Jerusalem's
    /// only far-zoom model for the whole game. The first model stays in the repository, unused on the map. No date switch".
    /// It streams from the towns bundle and stands on Jerusalem's core on every date; #507's first Jerusalem is never asked for.</summary>
    [Test]
    public void JerusalemIsTheSecondStoneCityOnEveryDate()
    {
        Assert.AreEqual("collection-" + TownLandmarks.Jerusalem3D, CollectionCatalog.BundleOf(TownLandmarks.Jerusalem3D));
        Assert.AreEqual(TownLandmarks.Jerusalem3D, TownLandmarks.ById["jerusalem"]);
        CollectionAssert.DoesNotContain(TownLandmarks.ById.Values.Concat(TownLandmarks.ByType.Values).ToList(), TownLandmarks.Jerusalem,
            "#507's first Jerusalem stays in the repository, unused on the map");
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var map = OpenMap();
        foreach (var date in new[] { content.Rules.Start, new DateTime(1949, 4, 15), new DateTime(1951, 12, 15) })
        {
            var t = TownLandmarks.ShowAt(map, date, Array.Empty<Placement>(), content)!;
            CollectionAssert.DoesNotContain(asked.Select(a => a.id).ToList(), TownLandmarks.Jerusalem, "the first Jerusalem is never asked for");
            AnswerAll();
            Assert.AreEqual(TownLandmarks.Jerusalem3D, t.Placed.Single(p => p.Key == "jerusalem").Model, $"{date:d MMM yyyy}");
            Assert.IsTrue(t.Seats.ContainsKey("jerusalem"), "on its core");
        }
    }

    /// <summary>The towns with a whole-town model, in the file's order.</summary>
    static List<Settlement> Mapped(MapScene map) => map.Settlements.Where(s => TownLandmarks.ModelFor(s) != null).ToList();

    TownLandmarks ShowAll(MapScene map)
    {
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), c)!;
        AnswerAll();
        return t;
    }

    /// <summary>Renders the map's own camera at <paramref name="distanceM"/> over <paramref name="at"/> (the hooks run as
    /// in the player: RenderPipelineManager.beginCameraRendering).</summary>
    static void RenderAt(MapScene map, Vector2 at, float distanceM)
    {
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = new Vector3(at.x, 0f, at.y); rig.Distance = distanceM; rig.Yaw = 0f; rig.Apply();
        RenderSample.Render(cam, 320, 180);
        // Inspect this camera's flags before EndCamera restores them, rather than asserting a stale global flag.
        var towns = map.GetComponentInChildren<TownLandmarks>(true);
        towns.Choose(default, cam);
    }

    /// <summary>Owner ruling, 18 October 2026 (on #917, #663, #659 and #700): the towns are Tal Heuberger's models, "Whole model far,
    /// buildings near". Far off, each mapped town's whole-town model stands ON its town, inside its kit footprint
    /// (MapScene.KitFootprints), as wide as its core (TownLandmarks.CoreOf: where most of its building models stand), not
    /// beside the town at a fixed 6 km.</summary>
    [Test]
    public void EveryTownLandmarkStandsOnItsTownSizedToItsCore()
    {
        var map = OpenMap();
        var t = ShowAll(map);
        CollectionAssert.IsEmpty(t.Missing);
        var footprint = new Dictionary<string, (Vector2 centre, float radius)>();
        foreach (var (centre, radius, id) in map.KitFootprints) if (!footprint.ContainsKey(id)) footprint[id] = (centre, radius);
        var mapped = Mapped(map);
        Assert.Greater(mapped.Count, 20);
        foreach (var s in mapped)
        {
            var child = t.transform.Find(s.Id);
            Assert.IsNotNull(child, s.Id + " has its landmark");
            var (centre, radius) = footprint[s.Id];
            var b = WorldBounds(child!.gameObject);
            Assert.Less(new Vector2(b.center.x - centre.x, b.center.z - centre.y).magnitude, radius, $"{s.Id} stands on its town");
            Assert.LessOrEqual(Mathf.Max(b.size.x, b.size.z), 1.12f * 2f * radius, $"{s.Id} is no wider than its footprint ({radius:0} m radius)");
            Assert.IsTrue(t.Seats.TryGetValue(s.Id, out var seat), s.Id);
            Assert.AreEqual(seat.Edge, Mathf.Max(b.size.x, b.size.z), 0.12f * seat.Edge, $"{s.Id} is as wide as its core ({0.5f * seat.Edge:0} m radius)");
        }
    }

    /// <summary>The coordinator for the owner, 18 October 2026: the whole-town model is sized to its town's core, "so Tel Aviv
    /// doesn't cover Jaffa and Haifa doesn't reach into the bay". No town's whole-town model covers another game settlement:
    /// no other settlement's kit footprint centre lies inside its ground rectangle (its world bounds on x/z).</summary>
    [Test]
    public void NoTownLandmarkCoversAnotherSettlement()
    {
        var map = OpenMap();
        var t = ShowAll(map);
        var ids = map.Settlements.Select(s => s.Id).ToHashSet();
        var centres = new Dictionary<string, Vector2>();
        foreach (var (c, _, id) in map.KitFootprints) if (ids.Contains(id) && !centres.ContainsKey(id)) centres[id] = c;
        var bad = new List<string>();
        foreach (var s in Mapped(map))
        {
            var b = WorldBounds(t.transform.Find(s.Id)!.gameObject);
            foreach (var (id, c) in centres)
                if (id != s.Id && c.x > b.min.x && c.x < b.max.x && c.y > b.min.z && c.y < b.max.z) bad.Add($"{s.Id}'s model covers {id}");
        }
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>No whole-town model reaches far into the sea or a lake: at most TownLandmarks.MaxWaterShare
    /// (a fifth; a coastal town keeps its shore) of the ground under its disc (its largest edge across) is water.</summary>
    [Test]
    public void NoTownLandmarkReachesIntoTheSea()
    {
        var map = OpenMap();
        var t = ShowAll(map);
        var ground = PlacementLayer.CampGround(map);
        var bad = new List<string>(); var lines = new List<string>();
        foreach (var s in Mapped(map))
        {
            var b = WorldBounds(t.transform.Find(s.Id)!.gameObject);
            var c = new Vector2(b.center.x, b.center.z);
            float r = 0.5f * Mathf.Max(b.size.x, b.size.z);
            int n = 0, wet = 0;
            for (int i = 0; i <= 10; i++)
                for (int k = 0; k <= 10; k++)
                {
                    var q = c + new Vector2(-r + 2f * r * i / 10f, -r + 2f * r * k / 10f);
                    if ((q - c).sqrMagnitude > r * r) continue;
                    n++; if (ground.Water(q.x, q.y)) wet++;
                }
            float share = n == 0 ? 0f : (float)wet / n;
            lines.Add($"{s.Id} {share:0.00}");
            if (share > 0.2f + 1e-3f) bad.Add($"{s.Id}: {share:P0} of its model's ground is water");
        }
        TestContext.WriteLine(string.Join(", ", lines));
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>The swap with #700's building models: the landmarks draw where TownModelLayer draws the kits' far view and
    /// hide where it draws the building models (TownModelLayer.CloseAt: in below CloseEnterM, out above CloseExitM), with
    /// the same hysteresis, at the start view (90 km), the gameplay view (60 km), both sides of the band and the closest
    /// zoom.</summary>
    [Test]
    public void TheLandmarksSwapWithTheBuildingModelsAtTheThreshold()
    {
        var map = OpenMap();
        var t = ShowAll(map);
        var layer = map.transform.Find(KitLayer.RootName).GetComponentInChildren<TownModelLayer>();
        Assert.IsTrue(layer.HasModels);
        var renderers = t.GetComponentsInChildren<Renderer>(true);
        Assert.Greater(renderers.Length, 0);
        var tel = map.KitFootprints.First(f => f.id == "tel_aviv").centre;
        foreach (var (d, close, why) in new[]
        {
            (90000f, false, "the start view"), (60000f, false, "the gameplay view"),
            (TownModelLayer.CloseEnterM + 1000f, false, "inside the band, coming in"), (TownModelLayer.CloseEnterM - 1000f, true, "past the threshold"),
            (TownModelLayer.CloseExitM - 2000f, true, "inside the band, going out"), (TownModelLayer.CloseExitM + 1000f, false, "out of the band"),
            (CameraController.MinDistance, true, "the closest zoom"),
        })
        {
            RenderAt(map, tel, d);
            Assert.AreEqual(close, layer.LastClose, $"{d / 1000f} km, {why}: the building models");
            foreach (var renderer in renderers)
            {
                var town = renderer.transform;
                while (town.parent != t.transform) town = town.parent;
                bool admitted = (bool)ResidencyCall(layer, typeof(TownModelLayer), "WantsWholeTown", town.name, Camera.main)!;
                Assert.AreEqual(!admitted, renderer.forceRenderingOff, $"{why}: only this camera's admitted whole town draws");
            }
        }
    }

    /// <summary>C89/C93 scopes the older whole-far/buildings-near ruling by this camera's admitted towns. An original
    /// ready for another camera does not hide this camera's procedural fallback.</summary>
    [Test]
    public void OnlyTheCurrentCamerasReadyTownsSuppressProceduralHouses()
    {
        var map = OpenMap();
        var t = ShowAll(map);
        var root = map.transform.Find(KitLayer.RootName);
        var mapped = Mapped(map);
        var plain = map.Settlements.First(s => TownLandmarks.ModelFor(s) == null);
        var plainKit = root.Find(plain.Id).GetComponent<MeshRenderer>();
        foreach (var (d, close) in new[] { (120000f, false), (60000f, false), (TownModelLayer.CloseEnterM - 1000f, true), (CameraController.MinDistance, true) })
        {
            RenderAt(map, map.KitFootprints.First(f => f.id == "tel_aviv").centre, d);
            foreach (var s in mapped)
            {
                var layer = root.GetComponentInChildren<TownModelLayer>();
                bool near = (bool)ResidencyCall(layer, typeof(TownModelLayer), "ReadyForTown", s.Id, Camera.main)!;
                bool whole = (bool)ResidencyCall(layer, typeof(TownModelLayer), "WantsWholeTown", s.Id, Camera.main)! && t.Seats.ContainsKey(s.Id);
                bool ready = near || whole;
                Assert.AreEqual(ready, root.Find(s.Id).GetComponent<MeshRenderer>().forceRenderingOff, s.Id);
                var closeR = root.Find(s.Id + KitLayer.CloseSuffix)?.GetComponent<MeshRenderer>();
                Assert.IsNotNull(closeR, s.Id + " has a close mesh to swap");
                Assert.AreEqual(!ready, closeR!.forceRenderingOff, s.Id);
            }
            var buildingLayer = root.GetComponentInChildren<TownModelLayer>();
            bool plainReady = (bool)ResidencyCall(buildingLayer, typeof(TownModelLayer), "ReadyForTown", plain.Id, Camera.main)!;
            Assert.AreEqual(plainReady, plainKit.forceRenderingOff, "an unrelated plain town keeps fallback");
        }
    }

    /// <summary>The fail-open check: without #700's building models (no model list) nothing would draw a mapped town up
    /// close, so its whole-town model stays at every zoom, and its kit houses still never draw.</summary>
    [UnityTest]
    public IEnumerator WithoutTheBuildingModelsTheWholeTownModelStaysUpClose()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.ConfigureTownModels(null!);
        LogAssert.Expect(LogType.Error, new Regex("town models: the map scene has no model list"));
        map.Build();
        ResidencyCall(map.GetComponentInChildren<TownModelLayer>(true), typeof(TownModelLayer), "ConfigureResidency",
            1000f, 2000f, 1000f, 2, "test fixture only; not measured");
        yield return null;
        var t = ShowAll(map);
        var root = map.transform.Find(KitLayer.RootName);
        Assert.IsFalse(root.GetComponentInChildren<TownModelLayer>().HasModels);
        RenderAt(map, map.KitFootprints.First(f => f.id == "tel_aviv").centre, CameraController.MinDistance);
        Assert.IsFalse(t.transform.Find("tel_aviv").GetComponentsInChildren<Renderer>(true).All(r => r.forceRenderingOff),
            "the admitted whole town remains visible when individual buildings are unavailable");
        Assert.IsTrue(root.Find("tel_aviv").GetComponent<MeshRenderer>().forceRenderingOff, "Tel Aviv's kit houses still never draw");
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
            var tilt = t.Seats.TryGetValue(key, out var seat) ? Quaternion.FromToRotation(Vector3.up, seat.Normal) : Quaternion.identity;
            var want = t.transform.rotation * tilt * Quaternion.Euler(0f, TownLandmarks.YawOf(model), 0f) * prefab.transform.localRotation;
            Assert.Less(Quaternion.Angle(want, child!.rotation), 0.01f, $"{key} ({model}) keeps its prefab's rotation under its yaw and its seat's tilt");
            if (!heights.TryGetValue(model, out var h))
            {
                // The model as its prefab stands (unparented, its own rotation): a yaw about the vertical keeps this height.
                var probe = Object.Instantiate(prefab);
                try { heights[model] = h = WorldBounds(probe).size.y / probe.transform.localScale.y; }
                finally { Object.DestroyImmediate(probe); }
            }
            // Untilted (the seat's tilt taken off about the base), the landmark is as tall as its model at its scale.
            var keep = child.rotation; child.rotation = Quaternion.Inverse(tilt) * child.rotation;
            float scale = child.localScale.y;
            try { Assert.AreEqual(h * scale, WorldBounds(child.gameObject).size.y, 0.01f * h * scale, $"{key} ({model}) is as tall as its model"); }
            finally { child.rotation = keep; }
        }
    }

    static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>The whole-town model on a slope (the kibbutz model cut into its hill at the closest zoom, on #917): each one
    /// is tilted to the least-squares plane of the ground under its footprint (at most TownLandmarks.MaxTiltDeg) and seated at
    /// its lowest point, so it never floats: no land under its footprint lies below its base plane. Degania Alef stands on a
    /// slope, so its model leans with the ground.</summary>
    [Test]
    public void AWholeTownModelFollowsItsGroundAndNeverFloats()
    {
        var map = OpenMap();
        var t = ShowAll(map);
        var ground = PlacementLayer.CampGround(map);
        var lines = new List<string>();
        foreach (var s in Mapped(map))
        {
            Assert.IsTrue(t.Seats.TryGetValue(s.Id, out var seat), s.Id);
            float tilt = Vector3.Angle(Vector3.up, seat.Normal);
            Assert.LessOrEqual(tilt, TownLandmarks.MaxTiltDeg + 0.01f, s.Id);
            float r = 0.5f * seat.Edge, deepest = 0f;
            for (int i = 0; i <= 10; i++)
                for (int k = 0; k <= 10; k++)
                {
                    var q = seat.Centre + new Vector2(-r + 2f * r * i / 10f, -r + 2f * r * k / 10f);
                    if ((q - seat.Centre).sqrMagnitude > 0.999f * r * r || !ground.Inside(q.x, q.y) || ground.Water(q.x, q.y)) continue;
                    var d = q - seat.Centre;
                    float plane = seat.BaseY - (seat.Normal.x * d.x + seat.Normal.z * d.y) / seat.Normal.y;
                    float above = ground.Y(q.x, q.y) - plane;
                    Assert.GreaterOrEqual(above, -0.5f, $"{s.Id} floats {-above:0.0} m above its ground at {q}");
                    deepest = Mathf.Max(deepest, above);
                }
            lines.Add($"{s.Id} tilt {tilt:0.0} deg, ground up to {deepest:0} m above its base");
        }
        TestContext.WriteLine(string.Join("; ", lines));
        Assert.Greater(Vector3.Angle(Vector3.up, t.Seats["degania_alef"].Normal), 0.5f, "Degania Alef leans with its slope");
    }

    /// <summary>#588 (owner, 18 October 2026, option (a), "Clearing doesn't move the core"): a unit clearing's ground in a
    /// town counts as if it still held plots, so the token's clearing does not pull the whole-town model's core away from
    /// it (Carmeli's in Haifa pushed Haifa's model into the bay). A town's plots on a 100 m grid with a hole cut where a
    /// clearing stands keep the core of the whole grid; without the clearing the core moves off it.</summary>
    [Test]
    public void AUnitClearingDoesNotMoveATownsCore()
    {
        var centre = new Vector2(10000f, 10000f);
        var clearing = (centre: centre + new Vector2(250f, 0f), radius: 200f);
        var all = new List<Vector2>();
        for (int i = -6; i <= 6; i++) for (int k = -6; k <= 6; k++) all.Add(centre + new Vector2(100f * i, 100f * k));
        var holed = all.Where(p => (p - clearing.centre).magnitude >= clearing.radius).ToList();
        Assert.Less(holed.Count, all.Count, "the clearing takes plots");
        var none = new (Vector2, float)[0];
        bool Dry(float x, float z) => false;
        var whole = TownLandmarks.CoreOf(centre, 1500f, all, none, new Vector2[0], Dry);
        var kept = TownLandmarks.CoreOf(centre, 1500f, holed, new[] { clearing }, new Vector2[0], Dry);
        var moved = TownLandmarks.CoreOf(centre, 1500f, holed, none, new Vector2[0], Dry);
        Assert.Greater((moved.centre - whole.centre).magnitude, 10f, "without the clearing's ground the core moves away from it");
        Assert.Less((kept.centre - whole.centre).magnitude, 5f, "with it the core stays where the full town's is");
        // The grid ties at the core's edge, so which edge plots make the share shifts its radius by a few metres.
        Assert.AreEqual(whole.radius, kept.radius, 0.03f * whole.radius, "and keeps its size");
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
    public void TheLandmarksStreamInOncePerModel()
    {
        var map = OpenMap();
        var c = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var t = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), c)!;
        Assert.IsNotNull(t);
        CollectionAssert.IsEmpty(t.Placed, "nothing before the stream answers");
        Assert.AreEqual(0, t.transform.childCount);
        AdmitGeometryTowns(map, t);
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
        AdmitGeometryTowns(map, t);
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
        AdmitGeometryTowns(map, t);
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
        AdmitGeometryTowns(map, t);
        var tel = asked.Single(a => a.id == TownLandmarks.TelAviv);
        LogAssert.Expect(LogType.Warning, new Regex("^LANDMARKS: the town model tel-aviv-white-city-v1 did not stream"));
        tel.done(null);
        Assert.AreEqual(0, t.transform.childCount);
        foreach (var (id, _, done) in asked.Where(a => a.id != TownLandmarks.TelAviv).ToList()) done(Far(id));
        Assert.Greater(t.Placed.Count, 0, "the others are drawn");
        CollectionAssert.DoesNotContain(t.Placed.Select(p => p.Key), "tel_aviv");
        CollectionAssert.Contains(t.Missing, "tel_aviv");
    }

    const System.Reflection.BindingFlags ResidencyFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    static object? ResidencyCall(object? owner, Type type, string method, params object?[] args)
    {
        var found = type.GetMethod(method, ResidencyFlags);
        Assert.IsNotNull(found, "focused residency must implement " + method);
        return found!.Invoke(owner, args);
    }
    sealed class ResidencySource : CollectionStream.ISource
    {
        public GameObject? Prefab;
        public int Opens, Unloads;
        public Action? BeforeUnload;
        public IEnumerator Open(string bundle, Action<string?> done) { Opens++; yield return null; done(null); }
        public IEnumerator Load(string bundle, string path, Action<GameObject?> done) { yield return null; done(Prefab); }
        public void Unload(string bundle) { BeforeUnload?.Invoke(); Unloads++; }
    }
    static void ResidencyPump(IEnumerator run, int frames = 20)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(run); int seen = 0;
        while (stack.Count != 0 && seen < frames)
        {
            var top = stack.Peek();
            if (!top.MoveNext()) stack.Pop();
            else if (top.Current is IEnumerator child) stack.Push(child);
            else seen++;
        }
    }
    sealed class ResidencyFixture : IDisposable
    {
        public readonly MapScene map;
        public readonly TownLandmarks towns;
        public readonly TownModelLayer buildings;
        public readonly Camera camera;
        public readonly TownLandmarks.Want focus;
        public readonly ResidencySource source;
        public readonly CollectionStream stream;
        public readonly List<string> acquired = new List<string>();
        public readonly List<Action<GameObject?>> callbacks = new List<Action<GameObject?>>();
        readonly List<GameObject> observedInstances = new List<GameObject>();
        readonly List<Renderer> observedRenderers = new List<Renderer>();
        readonly IEnumerator run;
        readonly System.Reflection.FieldInfo acquireField;
        readonly object? oldAcquire;
        readonly GameObject prefab;
        readonly Action<string, CollectionDetail, Action<GameObject?>> oldRequest;
        /// <param name="approved">The shipping town policy (ProductionResidency.Approved(): in the footprint only, view band
        /// 120/160 km) instead of the fixture's 1000/2000/1000/2 spatial values, and a focus admitted under it.</param>
        public ResidencyFixture(Action<string, CollectionDetail, Action<GameObject?>> defaultRequest, bool approved = false)
        {
            acquireField = typeof(TownLandmarks).GetField("Acquire", ResidencyFlags)!;
            Assert.IsNotNull(acquireField, "whole-town requests must use acquired ownership");
            oldAcquire = acquireField.GetValue(null); oldRequest = TownLandmarks.Request;
            map = OpenMap(); buildings = map.GetComponentInChildren<TownModelLayer>(true);
            var policy = ProductionResidency.Approved().Towns;
            if (approved) buildings.ConfigureProduction(policy);
            prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); prefab.name = "fake original town";
            source = new ResidencySource { Prefab = prefab }; stream = new CollectionStream(source); run = stream.Run();
            TownLandmarks.Request = defaultRequest;
            acquireField.SetValue(null, new Func<string, CollectionDetail, Action<GameObject?>, IDisposable>((id, detail, answer) =>
            {
                acquired.Add(id); callbacks.Add(answer);
                var retain = typeof(CollectionStream).GetMethod("Retain", ResidencyFlags);
                Assert.IsNotNull(retain);
                return (IDisposable)retain!.Invoke(stream, new object[] { id, detail, answer })!;
            }));
            var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
            towns = TownLandmarks.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
            // Map.Build -> KitLayer.Build -> TownModelLayer.Init, including real explicit town/renderer identity wiring.
            var places = (IReadOnlyList<(string id, UnityEngine.Bounds bounds)>)typeof(TownModelLayer).GetField("places", ResidencyFlags)!.GetValue(buildings)!;
            var chosen = towns.Wanted.Where(w => w.OnTown && buildings.CountFor(w.Key) > 0).OrderBy(w => w.Key, StringComparer.Ordinal).First(w =>
            {
                var ids = approved
                    ? TownModelLayer.FocusedTowns(places, new Vector3(w.At.x, 0, w.At.y), null, null, policy.Enter, policy.Exit, policy.Edge, policy.MaxEdges)
                    : (string[])ResidencyCall(null, typeof(TownModelLayer), "FocusedTowns", places,
                        new Vector3(w.At.x, 0, w.At.y), null, null, 1000f, 2000f, 1000f, 2)!;
                return ids.Length > 0 && ids[0] == w.Key;
            });
            focus = chosen;
            var go = new GameObject("residency camera"); camera = go.AddComponent<Camera>();
        }
        public void Choose(bool close = false, bool outside = false)
        {
            var at = outside ? new Vector2(-10000000, -10000000) : focus.At;
            float height = close ? 10000 : 40000; // view-ray distance 14 km or 56 km, inside the original visibility gate
            camera.transform.position = new Vector3(at.x, height, at.y - height);
            camera.transform.LookAt(new Vector3(at.x, 0, at.y));
            towns.Choose(default, camera);
        }
        public void Arrive()
        {
            ResidencyPump(run);
            var instance = towns.transform.Find(focus.Key);
            Assert.IsNotNull(instance, "capture a real instance before testing teardown");
            observedInstances.Add(instance.gameObject); observedRenderers.AddRange(instance.GetComponentsInChildren<Renderer>(true));
            Assert.IsNotEmpty(observedRenderers);
        }
        public void AssertConsumerCleared()
        {
            Assert.IsNotEmpty(observedInstances); Assert.IsNotEmpty(observedRenderers);
            foreach (var instance in observedInstances) Assert.IsTrue(instance == null, "detached instances must actually be destroyed");
            foreach (var renderer in observedRenderers) Assert.IsTrue(renderer == null, "a detached renderer is still a consumer");
            Assert.IsNull(towns.transform.Find(focus.Key), "instances retire before Unload(true)");
            Assert.IsFalse(towns.Seats.ContainsKey(focus.Key));
            var cached = (System.Collections.IDictionary)typeof(TownLandmarks).GetField("prefabs", ResidencyFlags)!.GetValue(towns)!;
            Assert.IsFalse(cached.Contains(focus.Model), "consumer prefab reference retires before source unload");
        }
        public void Dispose()
        {
            Object.DestroyImmediate(towns.gameObject); Object.DestroyImmediate(camera.gameObject);
            acquireField.SetValue(null, oldAcquire); TownLandmarks.Request = oldRequest;
            Object.DestroyImmediate(prefab);
        }
    }

    /// <summary>The still-applicable part of the owner's 18 October 2026 ruling ("no procedural houses in a mapped town at any
    /// zoom", the deleted AMappedTownDrawsNoProceduralHousesAtAnyZoom) under focused residency (#1273), across both
    /// hysteresis bands, inward and outward: the town view band (in at 120 km, out at 160 km) and the close band (buildings in
    /// below 40 km, out above 45 km). Once the town the camera is on has its original, its kit houses never draw; outside the
    /// view band (125 km on the way in, 165 km on the way out) the procedural fallback draws again. It runs under the shipping
    /// policy (ProductionResidency.Approved()), and each step also checks which original stands in: the building models
    /// below the close band, the whole-town model above it, neither outside the view band.</summary>
    [Test]
    public void TheFocusedMappedTownDrawsNoProceduralHousesAtAnyZoom()
    {
        using (var f = new ResidencyFixture(saved, approved: true))
        {
            Assert.AreEqual(ProductionResidency.Approved().Towns.Evidence, f.buildings.SpatialPolicyEvidence, "the approved policy, not the fixture's values");
            var identity = (IDictionary<Renderer, string>)typeof(TownModelLayer).GetField("swapIds", ResidencyFlags)!.GetValue(f.buildings)!;
            var kit = identity.Where(p => p.Value == f.focus.Key).Select(p => p.Key).ToList();
            Assert.IsNotEmpty(kit, f.focus.Key + " has kit houses to hide");
            var root = f.map.transform.Find(KitLayer.RootName);
            var closeR = root.Find(f.focus.Key + KitLayer.CloseSuffix)?.GetComponent<MeshRenderer>();
            Assert.IsNotNull(closeR, f.focus.Key + " has a close mesh to swap");
            int arrived = 0;
            // original: an original stands in (kit houses hidden); buildings: it is the building models (else the whole town).
            var steps = new (float d, bool original, bool buildings, string why)[]
            {
                (125000f, false, false, "in: outside the 120 km view band, fallback"), (119000f, true, false, "in: inside the view band"),
                (60000f, true, false, "in: the gameplay view"), (44000f, true, false, "in: above the 40 km close threshold, whole town"),
                (39000f, true, true, "in: below 40 km, building models"), (CameraController.MinDistance, true, true, "in: the closest zoom"),
                (44000f, true, true, "out: below the 45 km close exit, still buildings"), (46000f, true, false, "out: above 45 km, whole town again"),
                (150000f, true, false, "out: inside the 160 km view exit"), (165000f, false, false, "out: beyond 160 km, fallback restored"),
            };
            foreach (var (d, original, buildings, why) in steps)
            {
                float h = d / Mathf.Sqrt(2f);   // the view ray at 45 degrees: distance d to the ground
                f.camera.transform.position = new Vector3(f.focus.At.x, h, f.focus.At.y - h);
                f.camera.transform.LookAt(new Vector3(f.focus.At.x, 0, f.focus.At.y));
                f.towns.Choose(default, f.camera);   // also prepares the building layer for this camera
                if (f.acquired.Count > arrived) { f.Arrive(); arrived = f.acquired.Count; f.towns.Choose(default, f.camera); }
                string at = FormattableString.Invariant($"{f.focus.Key} at {d / 1000f} km ({why})");
                Assert.AreEqual(buildings, f.buildings.ReadyForTown(f.focus.Key, f.camera), "building models: " + at);
                Assert.AreEqual(original && !buildings, f.buildings.WantsWholeTown(f.focus.Key, f.camera), "whole-town original: " + at);
                foreach (var r in kit) Assert.AreEqual(original, r.forceRenderingOff, "kit houses: " + at);
                Assert.AreEqual(!original, closeR!.forceRenderingOff, "close mesh: " + at);
            }
            Assert.Greater(arrived, 0, "the whole-town original was admitted and arrived");
        }
    }

    [Test]
    public void FocusedOverviewAcquiresOnlyItsWholeTownAndLoadingKeepsFallback()
    {
        using (var f = new ResidencyFixture(saved))
        {
            CollectionAssert.IsEmpty(f.acquired, "no camera has admitted a town yet");
            f.Choose(); CollectionAssert.AreEqual(new[] { f.focus.Model }, f.acquired);
            Assert.IsFalse(f.buildings.Suppressed(f.focus.Key), "pending original cannot hide fallback");
            f.Arrive(); Assert.IsTrue(f.towns.Seats.ContainsKey(f.focus.Key));
            Assert.AreEqual(1, f.towns.Placed.Count(p => p.Key == f.focus.Key));
            Assert.AreEqual(1, f.source.Opens);
            f.Choose();
            var identity = (IDictionary<Renderer, string>)typeof(TownModelLayer).GetField("swapIds", ResidencyFlags)!.GetValue(f.buildings)!;
            foreach (var pair in identity) if (pair.Value == f.focus.Key) Assert.IsTrue(pair.Key.forceRenderingOff);
        }
    }
    [Test]
    public void CloseBuildingsAndWholeTownNeverOwnDuplicateRepresentations()
    {
        using (var f = new ResidencyFixture(saved))
        {
            f.Choose(close: true);
            CollectionAssert.IsEmpty(f.acquired, "individual Near buildings are the only admitted representation");
            Assert.IsTrue((bool)ResidencyCall(f.buildings, typeof(TownModelLayer), "ReadyForTown", f.focus.Key, f.camera)!);
            f.Choose(); f.Arrive(); Assert.IsTrue(f.towns.Seats.ContainsKey(f.focus.Key));
            f.Choose(close: true); Assert.IsFalse(f.towns.Seats.ContainsKey(f.focus.Key));
            Assert.AreEqual(1, f.source.Unloads, "whole lease retires when near representation takes over");
        }
    }
    [Test]
    public void LeavingFocusClearsInstancesAndConsumerReferencesBeforeLeaseUnload()
    {
        using (var f = new ResidencyFixture(saved))
        {
            f.Choose(); f.Arrive();
            f.source.BeforeUnload = f.AssertConsumerCleared;
            f.Choose(outside: true);
            Assert.AreEqual(1, f.source.Unloads); f.AssertConsumerCleared();
            var identity = (IDictionary<Renderer, string>)typeof(TownModelLayer).GetField("swapIds", ResidencyFlags)!.GetValue(f.buildings)!;
            foreach (var pair in identity) Assert.IsFalse(pair.Key.forceRenderingOff, "outside focus is code-built fallback");
        }
    }
    [Test]
    public void FailedOrLateWholeTownRepliesCannotCreateAHoleOrReviveAnEvictedTown()
    {
        using (var f = new ResidencyFixture(saved))
        {
            f.Choose(); var late = f.callbacks.Single(); f.Choose(outside: true);
            late(null); // stale reply must not create a warning, asset reference or instance
            Assert.IsFalse(f.towns.Seats.ContainsKey(f.focus.Key));
            f.Choose(); LogAssert.Expect(LogType.Warning, new Regex("LANDMARKS: the town model .* did not stream"));
            f.callbacks.Last()(null);
            Assert.IsFalse(f.buildings.Suppressed(f.focus.Key)); Assert.IsFalse(f.towns.Seats.ContainsKey(f.focus.Key));
        }
    }
    [Test]
    public void LayerDisableRetiresWholeTownOwnershipAfterInstancesAndReferences()
    {
        using (var f = new ResidencyFixture(saved))
        {
            f.Choose(); f.Arrive(); f.source.BeforeUnload = f.AssertConsumerCleared;
            f.towns.enabled = false;
            Assert.AreEqual(1, f.source.Unloads); f.AssertConsumerCleared();
        }
    }
    [Test]
    public void ProductionKitWiringNeverTreatsAMixedRendererAsOneNamedTown()
    {
        using (var f = new ResidencyFixture(saved))
        {
            var identity = (IDictionary<Renderer, string>)typeof(TownModelLayer).GetField("swapIds", ResidencyFlags)!.GetValue(f.buildings)!;
            var settlementIds = f.map.Settlements.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
            Assert.IsNotEmpty(identity);
            foreach (var pair in identity) Assert.IsTrue(settlementIds.Contains(pair.Value), "only built.Towns gets explicit identity");
            var swaps = (IEnumerable<(Renderer kit, Renderer? close)>)typeof(TownModelLayer).GetField("swaps", ResidencyFlags)!.GetValue(f.buildings)!;
            var mixed = swaps.Where(pair => !identity.ContainsKey(pair.kit)).ToList();
            Assert.IsNotEmpty(mixed, "Map.Build must include grouped localities/camps for this fixture");
            f.Choose(close: true);
            foreach (var pair in mixed) Assert.IsFalse(pair.kit.forceRenderingOff, "an atomic mixed tile keeps its fallback");
        }
    }


    [Test]
    public void AQuietCameraExpiresAndRetiresWholeTownOwnershipWithoutAnotherRender()
    {
        using (var f = new ResidencyFixture(saved))
        {
            f.Choose(); f.Arrive(); f.source.BeforeUnload = f.AssertConsumerCleared;
            ResidencyCall(f.buildings, typeof(TownModelLayer), "AdvanceCameraFrame", Time.frameCount + 3);
            ResidencyCall(f.towns, typeof(TownLandmarks), "RefreshWanted");
            Assert.AreEqual(1, f.source.Unloads); f.AssertConsumerCleared();
        }
    }

    [Test]
    public void NoAdmissionLayerCannotFallBackToUnboundedWholeTownRequests()
    {
        var root = new GameObject("no town policy");
        try
        {
            var towns = root.AddComponent<TownLandmarks>();
            var whole = new TownLandmarks.Want("town", TownLandmarks.TelAviv, Vector2.zero, 100, true);
            var placement = new TownLandmarks.Want("placement", TownLandmarks.Kibbutz, Vector2.zero, 100, false);
            Assert.IsFalse((bool)ResidencyCall(towns, typeof(TownLandmarks), "Allowed", whole)!);
            Assert.IsTrue((bool)ResidencyCall(towns, typeof(TownLandmarks), "Allowed", placement)!);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
