#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>The second prototype batch behind the same opt-in switch: BuildingPreview's ordered fallbacks, PlacementLayer's
/// opt-in hook beside its unchanged default, PreviewProps' resting rules and table, and PrototypeImport's folder discovery.
/// Generated stand-in prefabs and temp folders only (none of the Tripo models is needed), a fresh empty scene
/// per test, as BuildingPreviewTests.</summary>
public class BuildingPreviewDressingTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat",
        MarkerMatPath = "Assets/Materials/SettlementMarker.mat", SelectionMatPath = "Assets/Materials/HexSelection.mat", SeaMatPath = "Assets/Materials/Sea.mat", BorderLandMatPath = "Assets/Materials/BorderLand.mat";
    const string Maabara = "maabara-camp-v1", Shikun = "shikun-housing-v1", Hall = "kibbutz-dining-hall-v1", Compound = "kibbutz-compound-v2";

    static readonly Content C = ContentLoader.Load(Paths.Content);

    readonly List<GameObject> templates = new List<GameObject>();

    [SetUp] public void FreshScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [TearDown]
    public void DiscardScene()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        if (map != null) BuildingPreview.Apply(map, false, _ => null);   // the next fixture's PlacementLayer must not see it on
        foreach (var t in templates) if (t != null) Object.DestroyImmediate(t);
        templates.Clear();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static Material Load(string path) { var m = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.IsNotNull(m, path); return m; }

    static MapScene BuiltMap()
    {
        new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(TerrainMatPath), Load(MarkerMatPath), Load(SelectionMatPath), Load(SeaMatPath), Load(BorderLandMatPath));
        map.Build();
        return map;
    }

    /// <summary>BuildingPreviewTests' stand-in: an empty root at the bottom centre over a box 1 wide, 0.5 tall and 0.6 deep,
    /// with the primitive's collider and no shadow, so a placed copy shows the code's own work; optionally an Animator.</summary>
    GameObject StandIn(bool animator = false)
    {
        var root = new GameObject(animator ? "stand-in-animated" : "stand-in");
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(root.transform, false);
        box.transform.localPosition = new Vector3(0f, 0.25f, 0f); box.transform.localScale = new Vector3(1f, 0.5f, 0.6f);
        box.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        if (animator) root.AddComponent<Animator>().enabled = false;
        templates.Add(root);
        return root;
    }

    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        Assert.IsNotEmpty(rs, go.name + " has renderers");
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static float Ground(MapScene map, float x, float z) => map.Terrain.SampleHeight(new Vector3(x, 0f, z)) + map.Terrain.transform.position.y;

    /// <summary>PreviewProps.RestY's ground: the highest terrain under the centre and the four corners of the footprint.</summary>
    static float HighestGround(MapScene map, Bounds b) =>
        new[] { (b.center.x, b.center.z), (b.min.x, b.min.z), (b.min.x, b.max.z), (b.max.x, b.min.z), (b.max.x, b.max.z) }.Max(c => Ground(map, c.Item1, c.Item2));

    static void AssertDressed(GameObject go, string what)
    {
        Assert.IsEmpty(go.GetComponentsInChildren<Collider>(true), what + " has a collider");
        foreach (var r in go.GetComponentsInChildren<Renderer>()) Assert.AreEqual(ShadowCastingMode.On, r.shadowCastingMode, what + " casts shadows");
    }

    static Settlement S(string id, string type) => new Settlement { Id = id, Type = type };

    [Test]
    public void TheTablesMapTheBatchWithFallbacksInOrder()
    {
        Assert.AreEqual("jerusalem-stone-city-v1", BuildingPreview.PrototypeFor(S("jerusalem", "city")));
        Assert.AreEqual("haifa-port-city-v1", BuildingPreview.PrototypeFor(S("haifa", "city")));
        Assert.AreEqual("tel-aviv-white-city-v1", BuildingPreview.PrototypeFor(S("tel_aviv", "city")));
        foreach (var id in new[] { "petah_tikva", "rishon_lezion", "zikhron_yaakov", "rosh_pinna", "rehovot", "hadera" })
            Assert.AreEqual("moshava-town-v1", BuildingPreview.PrototypeFor(S(id, "town")), id);
        foreach (var id in new[] { "netanya", "tiberias", "safed" }) Assert.IsNull(BuildingPreview.PrototypeFor(S(id, "town")), id + " keeps its box");
        Assert.AreEqual("nahalal-moshav-v1", BuildingPreview.PrototypeFor(S("nahalal", "moshav")));
        Assert.AreEqual("stone-village-v1", BuildingPreview.PrototypeFor(S("gaza", "arab_town")));
        CollectionAssert.AreEqual(new[] { Compound, Hall }, BuildingPreview.CandidatesFor(S("hanita", "kibbutz")));
        CollectionAssert.AreEqual(new[] { Maabara }, BuildingPreview.CandidatesFor(MapActions.TransitCamp));
        CollectionAssert.AreEqual(new[] { Shikun }, BuildingPreview.CandidatesFor(MapActions.Housing));
        CollectionAssert.IsEmpty(BuildingPreview.CandidatesFor("no_such_action"));

        // Asked in order, none after the first that loads.
        var asked = new List<string>();
        Func<string, GameObject?> Only(params string[] present) => name => { asked.Add(name); return present.Contains(name) ? StandIn() : null; };
        Assert.AreEqual(Hall, BuildingPreview.Resolve(new[] { Compound, Hall }, Only(Hall)).name, "the hall while the compound is absent");
        CollectionAssert.AreEqual(new[] { Compound, Hall }, asked);
        asked.Clear();
        Assert.AreEqual(Compound, BuildingPreview.Resolve(new[] { Compound, Hall }, Only(Compound, Hall)).name);
        CollectionAssert.AreEqual(new[] { Compound }, asked, "the hall is not asked for once the compound loads");
        var none = BuildingPreview.Resolve(new[] { Compound, Hall }, Only());
        Assert.IsNull(none.name, "none loads: the box stays"); Assert.IsNull(none.prefab);

        Assert.AreEqual(210f, BuildingPreview.YawOf(Hall));
        Assert.AreEqual(BuildingPreview.DefaultYaw, BuildingPreview.YawOf("jerusalem-stone-city-v1-7k"), "a variant without a row");
        var every = BuildingPreview.Prototypes.ToList();
        CollectionAssert.AllItemsAreUnique(every);
        foreach (var name in new[] { Maabara, Shikun, Compound, "moshava-town-v1", "nahalal-moshav-v1" }.Concat(PreviewProps.All.SelectMany(p => p.Names)))
            CollectionAssert.Contains(every, name, "the loader may be asked for " + name);

        // Rows (the capture's report): one per mapping, its fallbacks inside it, so a loaded name hides its fallbacks' absence.
        var rows = BuildingPreview.Rows().Select(r => string.Join(",", r)).ToList();
        CollectionAssert.AllItemsAreUnique(rows, "the six moshavot share one row");
        CollectionAssert.Contains(rows, Compound + "," + Hall);
        CollectionAssert.DoesNotContain(rows, Hall, "the hall is a fallback, not a row of its own");
        CollectionAssert.Contains(rows, "vehicle-altalena-lst-1948-v2wl,altalena-lst-v1,altalena-lst-v1-7k", "the high-res export, then the first batch's");
        CollectionAssert.AreEqual(every, BuildingPreview.Rows().SelectMany(r => r).Distinct().ToList(), "Prototypes flattens Rows");
    }

    [Test]
    public void AKibbutzFallsBackToTheHallAndTurnsLikeIt()
    {
        var map = BuiltMap();
        Assert.Greater(BuildingPreview.Apply(map, true, name => name == Hall ? StandIn() : null), 0);
        var proto = map.transform.Find(BuildingPreview.RootName).Find(BuildingPreview.InstancePrefix + "degania_alef");
        Assert.IsNotNull(proto, "the hall stands in while the compound is absent");
        Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, BuildingPreview.YawOf(Hall), 0f), proto.rotation), 0.01f, "the yaw of the prefab that loaded");
        Assert.IsNull(map.transform.Find(BuildingPreview.RootName).Find(BuildingPreview.InstancePrefix + "nahalal"), "a moshav with no prefab keeps its box");
    }

    [Test]
    public void PlacementsKeepTheirBoxesUnlessThePreviewIsOn()
    {
        var map = BuiltMap();
        var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
        var camp = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var housing = C.SettlementHexes.Single(s => s.Id == "jerusalem");
        var placements = new[]
        {
            new Placement(MapActions.TransitCamp, camp.Q, camp.R, new DateTime(1950, 8, 26)),
            new Placement(MapActions.Housing, housing.Q, housing.R, new DateTime(1950, 8, 26)),
        };
        List<(string, Vector3, Vector3, Quaternion)> Boxes()
        {
            var list = new List<(string, Vector3, Vector3, Quaternion)>();
            foreach (Transform t in layer.transform)
            {
                Assert.IsNotNull(t.GetComponent<MeshRenderer>(), t.name + " is a box");
                Assert.AreEqual(0, t.childCount, t.name + " carries nothing");
                list.Add((t.name, t.position, t.localScale, t.rotation));
            }
            return list;
        }
        layer.Render(placements, C, map);
        var drawn = Boxes();
        CollectionAssert.AreEqual(new[] { $"transit_camp_{camp.Q}_{camp.R}", $"housing_{housing.Q}_{housing.R}" }, drawn.Select(d => d.Item1));
        Assert.IsFalse(BuildingPreview.Active(map));

        // Off: nothing is loaded and the boxes are exactly as before.
        BuildingPreview.Apply(map, false, name => { Assert.Fail("loaded " + name + " while off"); return null; });
        layer.Render(placements, C, map);
        CollectionAssert.AreEqual(drawn, Boxes(), "the default path is unchanged");

        // On: the prototypes stand where the boxes stood, fitted to the box's edge.
        var asked = new List<string>();
        BuildingPreview.Apply(map, true, name => { asked.Add(name); return name == Maabara || name == Shikun ? StandIn() : null; });
        Assert.IsTrue(BuildingPreview.Active(map));
        layer.Render(placements, C, map);
        CollectionAssert.IsSubsetOf(new[] { Maabara, Shikun }, asked);
        Assert.AreEqual(placements.Length, layer.transform.childCount, "one child per placement, as before");
        for (int i = 0; i < placements.Length; i++)
        {
            var p = placements[i];
            var holder = layer.transform.GetChild(i);
            var (name, boxAt, boxScale, _) = drawn[i];
            float edge = boxScale.y;   // PlacementLayer's size for the action: the box is that tall
            Assert.AreEqual(name, holder.name, "the box's name");
            Assert.IsNull(holder.GetComponent<MeshRenderer>(), name + " draws no box");
            Assert.AreEqual(Vector3.one, holder.localScale, name + " holder unscaled");
            Assert.AreEqual(1, holder.childCount, name);
            var proto = holder.GetChild(0).gameObject;
            Assert.AreEqual(BuildingPreview.InstancePrefix + p.Action, proto.name);
            var b = Measure(proto);
            Assert.AreEqual(Ground(map, boxAt.x, boxAt.z), b.min.y, 1f, name + " stands on the ground");
            Assert.AreEqual(boxAt.x, b.center.x, 1f, name + " x"); Assert.AreEqual(boxAt.z, b.center.z, 1f, name + " z");
            float fit = BuildingPreview.BuildingScale * edge;
            Assert.AreEqual(fit, Mathf.Max(Mathf.Max(b.size.x, b.size.z) / BuildingPreview.Spread, b.size.y), 1f, name + " fitted to BuildingScale x the box's edge");
            Assert.LessOrEqual(b.size.y, fit + 0.5f, name + " no taller than BuildingScale x the box");
            var yaw = BuildingPreview.YawOf(p.Action == MapActions.TransitCamp ? Maabara : Shikun) + BuildingPreview.PlacementTurn(p.Q, p.R);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, yaw, 0f), proto.transform.rotation), 0.01f, name + " yaw: from the table and the hex");
            AssertDressed(proto, name);
            Assert.AreEqual((p.Q, p.R), MapPointer.Pick(map, holder.position), name + " on its hex");
        }

        // A missing prefab keeps the box, and turning the preview off brings every box back.
        BuildingPreview.Apply(map, true, name => name == Maabara ? StandIn() : null);
        layer.Render(placements, C, map);
        Assert.AreEqual(1, layer.transform.GetChild(0).childCount, "the camp is dressed");
        Assert.IsNotNull(layer.transform.GetChild(1).GetComponent<MeshRenderer>(), "housing without its prefab keeps its box");
        BuildingPreview.Apply(map, false, _ => null);
        Assert.IsFalse(BuildingPreview.Active(map));
        layer.Render(placements, C, map);
        CollectionAssert.AreEqual(drawn, Boxes(), "off again: the boxes as before");
    }

    [Test]
    public void ThePreviewIsOnOnlyForTheMapItWasAppliedTo()
    {
        var map = BuiltMap();
        BuildingPreview.Apply(map, true, _ => null);
        Assert.IsTrue(BuildingPreview.Active(map));
        var other = new GameObject("OtherMap").AddComponent<MapScene>();
        Assert.IsFalse(BuildingPreview.Active(other));
        Object.DestroyImmediate(map.gameObject);
        Assert.IsFalse(BuildingPreview.Active(map), "a destroyed map is no map, though it is the one applied to");
        BuildingPreview.Apply(map, false, _ => null);
        var load = typeof(BuildingPreview).GetField("activeLoad", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(load, "BuildingPreview keeps the loader in activeLoad");
        Assert.IsNull(load!.GetValue(null), "off releases the loader even when the map is gone");
    }

    /// <summary>Neighbouring placements turn differently: on a stretch of the axial grid with negative coordinates too, each
    /// hex's turn is one of the three steps and none of its six neighbours shares it (east and west ones included).</summary>
    [Test]
    public void EveryNeighbourOfAPlacementTurnsDifferently()
    {
        var steps = new[] { -BuildingPreview.PlacementTurnStepDeg, 0f, BuildingPreview.PlacementTurnStepDeg };
        var neighbours = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
        for (int q = -6; q <= 6; q++)
            for (int r = -6; r <= 6; r++)
            {
                float turn = BuildingPreview.PlacementTurn(q, r);
                CollectionAssert.Contains(steps, turn, $"({q}, {r})");
                foreach (var (dq, dr) in neighbours)
                    Assert.AreNotEqual(turn, BuildingPreview.PlacementTurn(q + dq, r + dr), $"({q}, {r}) and its neighbour ({q + dq}, {r + dr})");
            }
    }

    [Test]
    public void PropsRestOnTheGroundTheSeaOrInTheAirWithNoCollider()
    {
        var map = BuiltMap();
        var props = new[]
        {
            new PreviewProps.Prop(35.020, 31.810, 75f, PreviewProps.VehicleM, PreviewProps.Kind.Land, PreviewProps.Motion.None, "truck"),
            new PreviewProps.Prop(34.666, 32.096, 20f, PreviewProps.ShipM, PreviewProps.Kind.Sea, PreviewProps.Motion.Bob, "absent", "ship"),
            new PreviewProps.Prop(34.885, 32.005, 300f, PreviewProps.AircraftM, PreviewProps.Kind.Air, PreviewProps.Motion.Bob, "plane"),
            new PreviewProps.Prop(34.970, 31.835, 200f, PreviewProps.FigureM, PreviewProps.Kind.Land, PreviewProps.Motion.Idle, "figure"),
            new PreviewProps.Prop(34.980, 31.840, 200f, PreviewProps.FortM, PreviewProps.Kind.Land, PreviewProps.Motion.None, "absent"),
        };
        var animated = StandIn(animator: true); var plain = StandIn();
        GameObject? Loader(string name) => name == "absent" ? null : name == "figure" || name == "truck" ? animated : plain;
        Assert.AreEqual(4, PreviewProps.Apply(map, true, Loader, props), "the entry with nothing to load places nothing");
        var root = map.transform.Find(PreviewProps.RootName);
        Assert.IsNotNull(root, "one root, under the map");
        Assert.AreEqual(4, root.childCount);
        float sea = PreviewProps.SeaLevelY();
        for (int i = 0; i < 4; i++)
        {
            var p = props[i];
            var go = root.GetChild(i).gameObject;
            StringAssert.StartsWith(PreviewProps.InstancePrefix + i.ToString("00") + "-", go.name);
            var b = Measure(go);
            var at = map.Frame.ToWorld(p.Lon, p.Lat, 0);
            Assert.AreEqual(at.x, b.center.x, 1f, go.name + " x"); Assert.AreEqual(at.z, b.center.z, 1f, go.name + " z");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, p.Yaw, 0f), go.transform.rotation), 0.01f, go.name + " yaw");
            var turn = go.transform.rotation; go.transform.rotation = Quaternion.identity;
            var own = Measure(go).size; go.transform.rotation = turn;   // the model's own box: the turned one grows with the turn
            Assert.AreEqual(p.SizeM, Mathf.Max(own.x, Mathf.Max(own.y, own.z)), 1f, go.name + " own largest dimension, whatever the turn");
            AssertDressed(go, go.name);
            float ground = HighestGround(map, b);
            if (p.Kind == PreviewProps.Kind.Land)
            {
                Assert.AreEqual(ground, b.min.y, 1f, go.name + " stands on the highest ground under it");
                Assert.GreaterOrEqual(b.min.y, Ground(map, b.center.x, b.center.z) - 0.01f, go.name + " not buried at its centre");
            }
            if (p.Kind == PreviewProps.Kind.Sea)
            {
                Assert.AreEqual(sea, Ground(map, at.x, at.z), 2f, "open sea: the published sea level is the drawn water's");
                Assert.AreEqual(sea - PreviewProps.DraftShare * b.size.y, b.min.y, 1f, go.name + " floats at the sea surface");
            }
            if (p.Kind == PreviewProps.Kind.Air) Assert.AreEqual(ground + PreviewProps.AirAltitudeM, b.min.y, 1f, go.name + " flies above the ground");
            var bob = go.GetComponent<PreviewBob>();
            Assert.AreEqual(p.Motion == PreviewProps.Motion.Bob, bob != null, go.name + " bobs only when asked");
            if (bob != null)
            {
                bool air = p.Kind == PreviewProps.Kind.Air;
                Assert.AreEqual(air ? PreviewProps.AirBobM : PreviewProps.SeaBobM, bob.AmplitudeM, go.name);
                Assert.AreEqual(air ? PreviewProps.AirBobPeriodS : PreviewProps.SeaBobPeriodS, bob.PeriodS, go.name);
            }
            foreach (var a in go.GetComponentsInChildren<Animator>(true))
            {
                Assert.AreEqual(p.Motion == PreviewProps.Motion.Idle, a.enabled, go.name + " animates only when idle");
                Assert.IsFalse(a.applyRootMotion, go.name + " no root motion");
            }
        }

        // Idempotent: a second Apply replaces the first with the same poses; off removes the root and loads nothing.
        var first = root.Cast<Transform>().Select(t => (t.name, t.position, t.localScale)).ToList();
        Assert.AreEqual(4, PreviewProps.Apply(map, true, Loader, props));
        Assert.AreEqual(1, map.transform.Cast<Transform>().Count(t => t.name == PreviewProps.RootName), "a second Apply replaces the first");
        var second = map.transform.Find(PreviewProps.RootName).Cast<Transform>().Select(t => (t.name, t.position, t.localScale)).ToList();
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].name, second[i].name);
            Assert.Less(Vector3.Distance(first[i].position, second[i].position), 0.01f, first[i].name + " position");
            Assert.Less(Vector3.Distance(first[i].localScale, second[i].localScale), 0.001f, first[i].name + " scale");
        }
        Assert.AreEqual(0, PreviewProps.Apply(map, false, name => { Assert.Fail("loaded " + name + " while off"); return null; }, props));
        Assert.IsNull(map.transform.Find(PreviewProps.RootName));
        Assert.AreEqual(0, PreviewProps.Apply(map, false, name => { Assert.Fail("loaded " + name + " while off"); return null; }, props), "off with nothing placed");
    }

    [Test]
    public void BuildingPreviewPlacesAndRemovesThePropsAndARebuildClearsThem()
    {
        var map = BuiltMap();
        var plain = StandIn();
        BuildingPreview.Apply(map, true, _ => plain);
        var root = map.transform.Find(PreviewProps.RootName);
        Assert.IsNotNull(root, "the props come with the switch");
        Assert.AreEqual(PreviewProps.All.Count, root.childCount);
        Assert.IsTrue(map.transform.Find(BuildingPreview.RootName) != null);
        BuildingPreview.Apply(map, false, _ => null);
        Assert.IsNull(map.transform.Find(PreviewProps.RootName), "Apply(false) removes them");
        BuildingPreview.Apply(map, true, _ => plain);
        map.Build();
        Assert.IsNull(map.transform.Find(PreviewProps.RootName), "MapScene.Clear destroys them with the map");
        Assert.AreEqual(0, BuildingPreview.Apply(map, true, _ => null));
        Assert.IsNull(map.transform.Find(PreviewProps.RootName), "nothing loads (no prototype present): no root");
    }

    /// <summary>The table is art dressing, but it must not stand on a town: on the real map with its kits, each prop's reach
    /// (half its largest dimension, times the square root of 2 for any shape at any turn) plus the radius of every kit town
    /// (the game settlements', the 1948 localities' and the Bedouin camps' footprints, as upstream's unit tokens keep off
    /// them; a settlement's prototype stays inside its town's disc, BuildingPreview.FootprintM) is less than their
    /// distance, and no two props overlap. A ship floats over open water (at 0 m, not merely below it) out to its reach.
    /// Every problem is listed at once.</summary>
    [Test]
    public void ThePropTableStandsClearOfEveryTownAndShipsFloat()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        Assert.Greater(map.KitFootprints.Count, map.Settlements.Count, "the kits are built, the 1948 localities' too");
        var problems = new List<string>();
        float Distance(double lon0, double lat0, double lon1, double lat1)
        {
            var a = map.Frame.ToWorld(lon0, lat0, 0); var b = map.Frame.ToWorld(lon1, lat1, 0);
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
        var all = PreviewProps.All;
        foreach (var p in all)
        {
            var at = map.Frame.ToWorld(p.Lon, p.Lat, 0);
            float reach = p.SizeM / 2f * Mathf.Sqrt(2f);
            foreach (var (centre, radius, id) in map.KitFootprints)
            {
                float d = new Vector2(at.x - centre.x, at.z - centre.y).magnitude;
                if (d <= radius + reach) problems.Add($"{p.Names[0]} stands on {id}'s town ({d:0} m apart, needs over {radius + reach:0})");
            }
            foreach (var q in all.Where(q => q != p))
                if (Distance(p.Lon, p.Lat, q.Lon, q.Lat) <= (p.SizeM + q.SizeM) / 2f * Mathf.Sqrt(2f)) problems.Add($"{p.Names[0]} overlaps {q.Names[0]}");
            if (p.Kind != PreviewProps.Kind.Sea) continue;
            float sea = PreviewProps.SeaLevelY(), tolerance = MapBuilder.SeaToleranceM * MapBuilder.VerticalExaggeration;
            for (int k = 0; k < 8; k++)
            {
                var off = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * reach;
                if (Mathf.Abs(Ground(map, at.x + off.x, at.z + off.z) - sea) > tolerance) problems.Add($"{p.Names[0]}: no open water {k * 45} degrees round");
            }
        }
        // The camps are placed at runtime (Localities.FillHexes), so a failure lists them all for whoever moves a prop.
        if (problems.Count > 0)
            foreach (var (centre, radius, id) in map.KitFootprints.Where(f => f.id.StartsWith("camp:", StringComparison.Ordinal)))
                TestContext.WriteLine(FormattableString.Invariant($"KITFOOT {id} {centre.x:0} {centre.y:0} {radius:0}"));
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>A hull floats only on open sea, the ground within the tolerance of 0 m: on the Haifa Bay with its draft under
    /// the water, but on the Kinneret, about 210 m below the sea, it rests on the lake instead of hanging over it at 0 m.</summary>
    [Test]
    public void ASeaPropFloatsOnlyOnOpenSea()
    {
        var map = BuiltMap();
        float sea = PreviewProps.SeaLevelY();
        Bounds Hull(double lon, double lat) => new Bounds(map.Frame.ToWorld(lon, lat, 0), new Vector3(200f, 100f, 200f));
        var bay = Hull(35.030, 32.845);
        Assert.AreEqual(sea - PreviewProps.DraftShare * bay.size.y, PreviewProps.RestY(map.Terrain, PreviewProps.Kind.Sea, bay), 0.01f, "afloat on the bay");
        var lake = Hull(35.590, 32.820);
        Assert.Less(HighestGround(map, lake), sea - 300f, "the Kinneret lies far below the sea surface");
        Assert.AreEqual(HighestGround(map, lake), PreviewProps.RestY(map.Terrain, PreviewProps.Kind.Sea, lake), 0.01f, "on the lake, not at 0 m");
    }

    /// <summary>The import's discovery (it reads, never copies, and nothing is written under Assets): every &lt;name&gt;/fbx/
    /// folder in ordinal order, a JPEG before a PNG, a folder without fbx/ ignored, a name that is not a slug refused.</summary>
    [Test]
    public void TheImportFindsEveryNameWithAnFbxFolder()
    {
        var source = Path.Combine(Path.GetTempPath(), "statecraft-prototypes-" + Guid.NewGuid().ToString("N"));
        string Empty(string dir, string file) { Directory.CreateDirectory(dir); var f = Path.Combine(dir, file); File.WriteAllBytes(f, new byte[0]); return f; }
        try
        {
            var ship = Empty(Path.Combine(source, "immigrant-steamer-v1", "fbx"), "tripo_convert_s.fbx");
            var shipJpeg = Empty(Path.ChangeExtension(ship, ".fbm"), "base.jpg");
            Empty(Path.ChangeExtension(ship, ".fbm"), "other.png");                                    // a JPEG wins
            var figure = Empty(Path.Combine(source, "figure-soldier-1948-v1", "fbx"), "rigged.FBX");
            var figurePng = Empty(Path.ChangeExtension(figure, ".fbm"), "base.png");                   // no JPEG: the one PNG
            var variant = Empty(Path.Combine(source, "jerusalem-stone-city-v1-7k", "fbx"), "j.fbx");
            var variantJpeg = Empty(Path.ChangeExtension(variant, ".fbm"), "j.jpeg");
            var two = Empty(Path.Combine(source, "haifa-port-city-v1", "fbx"), "h.fbx");
            Empty(Path.ChangeExtension(two, ".fbm"), "a.jpg"); Empty(Path.ChangeExtension(two, ".fbm"), "b.jpg"); // two JPEGs: refused
            Empty(Path.Combine(source, "Odd Name", "fbx"), "o.fbx");                                   // not a slug: refused
            Empty(Path.Combine(source, "concepts"), "sheet.jpg");                                      // no fbx/: not a prototype
            Empty(Path.Combine(source, "tegart-fort-v1", "original"), "t.glb");                         // no fbx/ either

            var sources = PrototypeImportType().GetMethod("Sources", BindingFlags.Public | BindingFlags.Static)!;
            var (found, missing) = ((List<(string name, string fbx, string image)>, List<string>))sources.Invoke(null, new object[] { source })!;
            CollectionAssert.AreEqual(new[]
            {
                ("figure-soldier-1948-v1", figure, figurePng), ("immigrant-steamer-v1", ship, shipJpeg), ("jerusalem-stone-city-v1-7k", variant, variantJpeg),
            }, found);
            Assert.AreEqual(2, missing.Count);
            StringAssert.Contains("PROTOTYPE Odd Name MISSING: the folder name is not a slug", missing[0]);
            StringAssert.Contains("PROTOTYPE haifa-port-city-v1 MISSING: 2 base-colour JPEGs", missing[1]);
            Assert.AreEqual("figure-", (string)PrototypeImportType().GetField("FigurePrefix", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!,
                "figures import with their rig and clips");
        }
        finally { Directory.Delete(source, true); }
    }

    static Type PrototypeImportType()
    {
        var t = Type.GetType("PrototypeImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "PrototypeImport exists");
        return t!;
    }
}
