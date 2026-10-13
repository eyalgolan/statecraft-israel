#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

/// <summary>The 3D building models (BuildingPreview): an Apply that hides only the mapped settlements' boxes and kit towns
/// behind a loaded prefab and puts everything back, the size rule and knob, and PrototypeImport's source check. Uses a
/// generated stand-in prefab and temp folders, so the tests do not depend on the Tripo models. Fresh empty scene per
/// test, as MapSceneTests (R4).</summary>
public class BuildingPreviewTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat",
        MarkerMatPath = "Assets/Materials/SettlementMarker.mat", SelectionMatPath = "Assets/Materials/HexSelection.mat", SeaMatPath = "Assets/Materials/Sea.mat", BorderLandMatPath = "Assets/Materials/BorderLand.mat";

    GameObject? template;

    [SetUp] public void FreshScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [TearDown]
    public void DiscardScene()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        if (map != null) BuildingPreview.Apply(map, false, _ => null);   // the next fixture's PlacementLayer must not see it on
        if (template != null) Object.DestroyImmediate(template);
        template = null;
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

    /// <summary>Shaped like PrototypeImport's prefabs: an empty root at the bottom centre over a box 1 wide, 0.5 tall and
    /// 0.6 deep. It keeps the primitive's collider and casts no shadow, so a placed copy shows Apply's own work (no
    /// collider, shadows on); its property block is for Apply to clear, should Instantiate copy one.</summary>
    GameObject StandIn()
    {
        if (template != null) return template;
        template = new GameObject("stand-in");
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(template.transform, false);
        box.transform.localPosition = new Vector3(0f, 0.25f, 0f); box.transform.localScale = new Vector3(1f, 0.5f, 0.6f);
        var r = box.GetComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        var tint = new MaterialPropertyBlock(); tint.SetColor("_BaseColor", Color.red); r.SetPropertyBlock(tint);
        return template;
    }

    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        Assert.IsNotEmpty(rs, go.name + " has renderers");
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static int CountNamed(string name)
    {
        int n = 0; foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name == name) n++;
        return n;
    }

    static Transform Markers(MapScene map) => map.transform.Find("Settlements");

    static bool Shown(MapScene map, string id) => Markers(map).Find(id).GetComponent<MeshRenderer>().enabled;

    [Test]
    public void TelAvivKibbutzimAndArabTownsMapToThePrototypes()
    {
        static Settlement S(string id, string type) => new Settlement { Id = id, Type = type };
        Assert.AreEqual("tel-aviv-white-city-v1", BuildingPreview.PrototypeFor(S("tel_aviv", "city")), "by id, before its type");
        CollectionAssert.AreEqual(new[] { "kibbutz-compound-v2", "kibbutz-dining-hall-v1" }, BuildingPreview.CandidatesFor(S("degania_alef", "kibbutz")),
            "the compound, else the first batch's hall");
        Assert.AreEqual("stone-village-v1", BuildingPreview.PrototypeFor(S("lod", "arab_town")));
        foreach (var (id, type) in new[] { ("netanya", "town"), ("tiberias", "town"), ("safed", "town"), ("somewhere", "village"), ("somewhere", "city") })
            Assert.IsNull(BuildingPreview.PrototypeFor(S(id, type)), id + " keeps its box");
        CollectionAssert.IsSubsetOf(new[] { "kibbutz-dining-hall-v1", "tel-aviv-white-city-v1", "stone-village-v1" }, BuildingPreview.Prototypes.ToList());
        CollectionAssert.AllItemsAreUnique(BuildingPreview.Prototypes.ToList());
        Assert.IsNull(BuildingPreview.LoadFromResources("no-such-prototype"), "a missing prefab loads as null");
    }

    [Test]
    public void PrototypesStandInOnlyForTheMappedMarkers()
    {
        var map = BuiltMap();
        var markers = Markers(map);
        var before = map.Settlements.ToDictionary(s => s.Id, s =>
        {
            var t = markers.Find(s.Id); return (t.position, t.lossyScale, t.GetComponent<Renderer>().sharedMaterial);
        });
        var asked = new List<string>();
        Assert.IsNotNull(StandIn().GetComponentInChildren<Collider>(), "the prefab brings a collider for Apply to remove");
        int placed = BuildingPreview.Apply(map, true, name => { asked.Add(name); return StandIn(); });

        var root = map.transform.Find(BuildingPreview.RootName);
        Assert.IsNotNull(root, "one root for the prototypes, under the map");
        Assert.AreEqual(1, CountNamed(BuildingPreview.RootName));
        Assert.AreEqual(1, CountNamed("Settlements"), "the markers' root is still the only one (MapSceneTests.AssertOneOfEach)");
        var mapped = map.Settlements.Where(s => BuildingPreview.PrototypeFor(s) != null).ToList();
        Assert.AreEqual(mapped.Count, placed); Assert.AreEqual(mapped.Count, root.childCount);
        CollectionAssert.IsSubsetOf(asked.Distinct().ToList(), BuildingPreview.Prototypes.ToList(), "only prototype names are loaded");
        foreach (var id in new[] { "tel_aviv", "degania_alef", "lod", "ramla" })
            Assert.IsNotNull(root.Find(BuildingPreview.InstancePrefix + id), id);

        foreach (var s in map.Settlements)
        {
            var box = markers.Find(s.Id); Assert.IsNotNull(box, s.Id);
            var (position, scale, material) = before[s.Id];
            Assert.AreEqual(position, box.position, s.Id + " position"); Assert.AreEqual(scale, box.lossyScale, s.Id + " scale");
            Assert.AreSame(material, box.GetComponent<Renderer>().sharedMaterial, s.Id + " material");
            Assert.IsNull(box.GetComponent<Collider>(), s.Id + " has a collider");
            bool isMapped = BuildingPreview.PrototypeFor(s) != null;
            Assert.AreEqual(!isMapped, box.GetComponent<MeshRenderer>().enabled, s.Id + ": only a mapped marker hides its box");
            var proto = root.Find(BuildingPreview.InstancePrefix + s.Id);
            Assert.AreEqual(isMapped, proto != null, s.Id + " prototype");
            if (proto == null) continue;

            float edge = BuildingPreview.FootprintM(map, s), height = BuildingPreview.HeightM(s);
            var b = Measure(proto.gameObject);
            float ground = map.Terrain.SampleHeight(box.position) + map.Terrain.transform.position.y;
            Assert.AreEqual(ground, b.min.y, 1f, s.Id + " stands on the ground");
            Assert.AreEqual(box.position.x, b.center.x, 1f, s.Id + " x"); Assert.AreEqual(box.position.z, b.center.z, 1f, s.Id + " z");
            Assert.AreEqual(BuildingPreview.BuildingScale * BuildingPreview.Spread * SettlementPlacer.MarkerHeightM(s.Type), edge, 0.01f, s.Id + ": no kits, so the scaled box sets the footprint");
            Assert.LessOrEqual(b.size.x, edge + 0.5f, s.Id + " footprint x within FootprintM");
            Assert.LessOrEqual(b.size.z, edge + 0.5f, s.Id + " footprint z within FootprintM");
            Assert.LessOrEqual(b.size.y, height + 0.5f, s.Id + " no taller than HeightM");
            Assert.AreEqual(1f, Mathf.Max(Mathf.Max(b.size.x, b.size.z) / edge, b.size.y / height), 0.001f, s.Id + " as large as the footprint or height allows");
            Assert.IsEmpty(proto.GetComponentsInChildren<Collider>(true), s.Id + " prototype has a collider");
            foreach (var r in proto.GetComponentsInChildren<Renderer>())
            {
                Assert.AreEqual(ShadowCastingMode.On, r.shadowCastingMode, s.Id + " casts shadows");
                Assert.IsFalse(r.HasPropertyBlock(), s.Id + " no per-type tint over the texture");
            }
        }
    }

    [Test]
    public void AMissingPrefabKeepsThatMarkersBox()
    {
        var map = BuiltMap();
        Assert.AreEqual(1, BuildingPreview.Apply(map, true, name => name == "tel-aviv-white-city-v1" ? StandIn() : null));
        var root = map.transform.Find(BuildingPreview.RootName);
        Assert.IsFalse(Shown(map, "tel_aviv"));
        foreach (var id in new[] { "degania_alef", "lod", "jerusalem" })
        {
            Assert.IsTrue(Shown(map, id), id + " keeps its box");
            Assert.IsNull(root.Find(BuildingPreview.InstancePrefix + id), id);
        }
        // Nothing loads at all (no prototype present): no root, and every box shows again.
        Assert.AreEqual(0, BuildingPreview.Apply(map, true, _ => null));
        Assert.IsNull(map.transform.Find(BuildingPreview.RootName));
        foreach (var s in map.Settlements) Assert.IsTrue(Shown(map, s.Id), s.Id);
    }

    [Test]
    public void ApplyingAgainReplacesAndTurningOffRestoresEveryBox()
    {
        var map = BuiltMap();
        List<(string, Vector3, Quaternion, Vector3)> Poses() => map.transform.Find(BuildingPreview.RootName).Cast<Transform>()
            .Select(t => (t.name, t.position, t.rotation, t.localScale)).ToList();
        int placed = BuildingPreview.Apply(map, true, _ => StandIn());
        var first = Poses();
        Assert.AreEqual(placed, BuildingPreview.Apply(map, true, _ => StandIn()));
        Assert.AreEqual(1, CountNamed(BuildingPreview.RootName), "a second Apply replaces the first");
        var second = Poses();
        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].Item1, second[i].Item1);
            Assert.Less(Vector3.Distance(first[i].Item2, second[i].Item2), 0.01f, first[i].Item1 + " position");
            Assert.Less(Quaternion.Angle(first[i].Item3, second[i].Item3), 0.01f, first[i].Item1 + " yaw: constant, never random");
            Assert.Less(Vector3.Distance(first[i].Item4, second[i].Item4), 0.001f, first[i].Item1 + " scale");
        }
        Assert.AreEqual(0, BuildingPreview.Apply(map, false, _ => StandIn()));
        Assert.AreEqual(0, CountNamed(BuildingPreview.RootName));
        foreach (var s in map.Settlements) Assert.IsTrue(Shown(map, s.Id), s.Id + " shows its box again");
        Assert.AreEqual(1, CountNamed("Settlements"));
        // The default path: off with nothing placed loads nothing and touches nothing.
        Assert.AreEqual(0, BuildingPreview.Apply(map, false, name => { Assert.Fail("loaded " + name + " while off"); return null; }));
    }

    /// <summary>With upstream's kits built (the real scene), a prototype stands in for its settlement's kit town: that kit's
    /// renderer is off while the prototype stands, the prototype stays inside the town's disc at any turn, and Apply(false)
    /// turns on again exactly what it turned off (the boxes the kits hide stay hidden). Off with nothing placed touches no
    /// renderer, the kits' included.</summary>
    [Test]
    public void APrototypeReplacesItsKitTownAndOffRestoresExactly()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var kits = map.transform.Find(Statecraft.Game.Map.Kits.KitLayer.RootName);
        Assert.IsNotNull(kits, "the scene builds the kits");
        var renderers = map.GetComponentsInChildren<MeshRenderer>(true);
        var before = renderers.Select(r => r.enabled).ToArray();
        void AssertAsBefore(string when) { for (int i = 0; i < renderers.Length; i++) Assert.AreEqual(before[i], renderers[i].enabled, renderers[i].name + ": " + when); }
        Assert.AreEqual(0, BuildingPreview.Apply(map, false, name => { Assert.Fail("loaded " + name + " while off"); return null; }));
        AssertAsBefore("off touches nothing");
        Assert.Greater(BuildingPreview.Apply(map, true, _ => StandIn()), 0);
        var root = map.transform.Find(BuildingPreview.RootName);
        foreach (var s in map.Settlements)
        {
            var proto = root.Find(BuildingPreview.InstancePrefix + s.Id);
            Assert.AreEqual(proto == null, kits.Find(s.Id).GetComponent<MeshRenderer>().enabled, s.Id + ": only a replaced town's kit is off");
            Assert.IsFalse(Shown(map, s.Id), s.Id + ": the kits keep every box hidden");
            if (proto == null) continue;
            var (centre, radius, _) = map.KitFootprints.First(f => f.id == s.Id);
            var b = Measure(proto.gameObject);
            foreach (var (x, z) in new[] { (b.min.x, b.min.z), (b.min.x, b.max.z), (b.max.x, b.min.z), (b.max.x, b.max.z) })
                Assert.LessOrEqual(new Vector2(x - centre.x, z - centre.y).magnitude, radius + 1f, s.Id + " stays inside its town's disc");
        }
        Assert.AreEqual(0, BuildingPreview.Apply(map, false, _ => null));
        AssertAsBefore("off restores every renderer exactly");
    }

    [TestCase(null, 1f)]
    [TestCase("https://localhost/?smoke=5", 1f)]
    [TestCase("https://localhost/?smoke=5&previewscale=150", 1.5f)]
    [TestCase("https://localhost/?previewscale=75&smoke=5", 0.75f)]
    [TestCase("https://localhost/?smoke=5&previewscale=5", 0.25f)]
    [TestCase("https://localhost/?smoke=5&previewscale=9000", 4f)]
    [TestCase("https://localhost/?smoke=5&previewscale=0", 1f)]
    [TestCase("https://localhost/?smoke=5&previewscale=big", 1f)]
    public void TheSizeKnobIsAClampedPercentage(string? url, float expected) =>
        Assert.AreEqual(expected, BuildingPreview.RequestedScale(url), 1e-6f);

    /// <summary>The knob scales every prototype alike, and turning the preview off sets it back to one.</summary>
    [Test]
    public void TheSizeKnobScalesEveryPrototypeAndOffResetsIt()
    {
        var map = BuiltMap();
        BuildingPreview.Apply(map, true, _ => StandIn(), 1f);
        var plain = map.transform.Find(BuildingPreview.RootName).Cast<Transform>().ToDictionary(t => t.name, t => Measure(t.gameObject).size);
        BuildingPreview.Apply(map, true, _ => StandIn(), 1.5f);
        Assert.AreEqual(1.5f, BuildingPreview.Scale);
        foreach (Transform t in map.transform.Find(BuildingPreview.RootName))
            Assert.AreEqual(1.5f * plain[t.name].y, Measure(t.gameObject).size.y, 1f, t.name);
        BuildingPreview.Apply(map, false, _ => null);
        Assert.AreEqual(1f, BuildingPreview.Scale);
    }

    [Test]
    public void RebuildingTheMapClearsThePrototypes()
    {
        var map = BuiltMap();
        Assert.Greater(BuildingPreview.Apply(map, true, _ => StandIn()), 0);
        map.Build();
        Assert.IsNull(map.transform.Find(BuildingPreview.RootName), "MapScene.Clear destroys every child, the prototypes too");
        Assert.AreEqual(1, CountNamed("Settlements"));
        foreach (var s in map.Settlements) Assert.IsTrue(Shown(map, s.Id), s.Id);
    }

    [Test]
    public void AnImportWithMissingSourcesChangesNothing()
    {
        var empty = TempDir("statecraft-prototypes-");
        try
        {
            var before = FolderState(ImportFolder());
            var e = Assert.Throws<TargetInvocationException>(() => ImportMethod("Import").Invoke(null, new object[] { empty }));
            Assert.IsInstanceOf<FileNotFoundException>(e.InnerException);
            StringAssert.Contains("no prototype under", e.InnerException!.Message);
            CollectionAssert.AreEqual(before, FolderState(ImportFolder()), "nothing was copied");

            var whole = Path.Combine(empty, "tegart-fort-v1", "fbx");
            Directory.CreateDirectory(Path.Combine(whole, "a.fbm"));
            File.WriteAllBytes(Path.Combine(whole, "a.fbx"), new byte[0]); File.WriteAllBytes(Path.Combine(whole, "a.fbm", "base.jpg"), new byte[0]);
            Directory.CreateDirectory(Path.Combine(empty, "altalena-lst-v1", "fbx"));                  // no model
            LogAssert.Expect(LogType.Error, new Regex("PROTOTYPE altalena-lst-v1 MISSING"));
            e = Assert.Throws<TargetInvocationException>(() => ImportMethod("Import").Invoke(null, new object[] { empty }));
            StringAssert.Contains("1 of 2 prototypes incomplete", e.InnerException!.Message);
            CollectionAssert.AreEqual(before, FolderState(ImportFolder()), "the complete one was not copied either");
        }
        finally { Directory.Delete(empty, true); }
    }

    /// <summary>The source check alone (it reads, never copies): exactly one .fbx per prototype, and one JPEG of any case
    /// in its .fbm folder (the kibbutz texture is .JPEG).</summary>
    [Test]
    public void TheImportNeedsOneModelAndOneJpegPerPrototype()
    {
        var source = TempDir("statecraft-prototypes-");
        string Empty(string dir, string file) { Directory.CreateDirectory(dir); var f = Path.Combine(dir, file); File.WriteAllBytes(f, new byte[0]); return f; }
        try
        {
            var hall = Empty(Path.Combine(source, "kibbutz-dining-hall-v1", "fbx"), "tripo_convert_a.fbx");
            var jpeg = Empty(Path.ChangeExtension(hall, ".fbm"), "base.JPEG");
            Empty(Path.ChangeExtension(hall, ".fbm"), "normal.png");                                   // not a JPEG: ignored
            Empty(Path.Combine(source, "tel-aviv-white-city-v1", "fbx"), "a.fbx");
            Empty(Path.Combine(source, "tel-aviv-white-city-v1", "fbx"), "b.FBX");                     // two models: refused
            Empty(Path.Combine(source, "stone-village-v1", "fbx"), "c.fbx");                           // no .fbm: no texture
            var (found, missing) = ((List<(string name, string fbx, string image)>, List<string>))ImportMethod("Sources").Invoke(null, new object[] { source })!;
            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(("kibbutz-dining-hall-v1", hall, jpeg), found[0]);
            Assert.AreEqual(2, missing.Count);   // in ordinal order of name
            StringAssert.Contains("PROTOTYPE stone-village-v1 MISSING: 0 base-colour JPEGs", missing[0]);
            StringAssert.Contains("PROTOTYPE tel-aviv-white-city-v1 MISSING: 2 .fbx files", missing[1]);
        }
        finally { Directory.Delete(source, true); }
    }

    static System.Type PrototypeImportType()
    {
        var t = System.Type.GetType("PrototypeImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "PrototypeImport exists");
        return t!;
    }

    static MethodInfo ImportMethod(string name) => PrototypeImportType().GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    static string ImportFolder() => (string)PrototypeImportType().GetField("Folder", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    /// <summary>Whether the folder exists, then every file under it with its write time.</summary>
    static List<string> FolderState(string folder)
    {
        var state = new List<string> { Directory.Exists(folder) ? "present" : "absent" };
        if (Directory.Exists(folder))
            state.AddRange(Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(f => f, System.StringComparer.Ordinal)
                .Select(f => f + " " + File.GetLastWriteTimeUtc(f).Ticks));
        return state;
    }

    static string TempDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), prefix + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
