using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #665: Tal's soldiers (The 1948 Collection, #507) on the army tokens, streamed after the first frame. Until a
/// model arrives, and when its bundle is missing or damaged, the code-built figure stays (fail open).</summary>
public class UnitFiguresTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static readonly DateTime Day = new DateTime(1948, 5, 29);

    [SetUp] public void Fresh() { UnitFigures.Source = null; UnitFigures.Forget(); }
    [TearDown] public void Restore() { UnitFigures.Source = null; UnitFigures.Forget(); }

    static GameState StateAt(DateTime date, double mobilization)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        s.Date = date; s.Rung = 5; s.Mobilization = mobilization;
        return s;
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

    static List<Transform> Figures(MapScene map) => map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>()
        .SelectMany(t => t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman"))).ToList();

    static GameObject Prefab(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    /// <summary>Steps a coroutine (nested enumerators included) by hand, for at most <paramref name="frames"/> frames.</summary>
    static void Step(IEnumerator run, int frames)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(run); int seen = 0;
        while (stack.Count > 0 && seen < frames)
        {
            var top = stack.Peek();
            if (!top.MoveNext()) { stack.Pop(); continue; }
            if (top.Current is IEnumerator nested) stack.Push(nested); else seen++;
        }
    }

    /// <summary>The mapping table: every rifleman type the Israeli tokens draw maps to one of Tal's people, in the people
    /// bundle; no other side maps (C08 and C09 wait for #226), so those keep the code-built figures.</summary>
    [Test]
    public void EveryIsraeliRiflemanMapsToOneOfTalsSoldiersAndNoOtherSideDoes()
    {
        var people = CollectionCatalog.Bundles.Single(b => b.Bundle == CollectionCatalog.People).Ids;
        int mapped = 0;
        foreach (var hat in UnitData.Headgears)
        {
            var type = UnitRoster.RiflemanType(hat);
            var id = UnitFigures.ModelFor(UnitData.Israel, type);
            Assert.IsNotNull(id, $"{hat}: an Israeli rifleman has a model");
            CollectionAssert.Contains(people, id, $"{hat}: {id} is one of Tal's people");
            Assert.IsNotNull(Prefab(id), $"{id}: its far prefab is on main");
            mapped++;
            foreach (var side in UnitData.Sides.Where(s => s != UnitData.Israel))
                Assert.IsNull(UnitFigures.ModelFor(side, type), $"{side} {hat}: no model before #226");
        }
        Assert.AreEqual(3, mapped);
        CollectionAssert.AreEqual(
            new[] { "figure-c04-sabra-pioneer-v3", "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2" },
            UnitData.Headgears.Select(h => UnitFigures.ModelFor(UnitData.Israel, UnitRoster.RiflemanType(h))).ToArray(),
            "slouch hat, steel helmet, wool cap: Tal's own pairing in #507");
    }

    /// <summary>The tokens are built with the code-built figures and ask for their models; nothing changes in the frame of
    /// the request. When the models arrive every figure shows Tal's soldier in its own frame, with the code-built renderer
    /// off and the figure's transform where it stood. A roster rebuild dresses the new figures at once, without asking again
    /// (no frame of code-built figures).</summary>
    [Test]
    public void TheFiguresSwapToTalsSoldiersWhenTheModelsArrive()
    {
        var map = BuildMap();
        var asked = new List<(string id, CollectionDetail detail, Action<GameObject> done)>();
        UnitFigures.Source = (id, d, done) => asked.Add((id, d, done));
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(C, StateAt(Day, 0.8));
            var figures = Figures(map);
            Assert.Greater(figures.Count, 10, "the riflemen of 29 May 1948 (not a vacuous pass)");
            Assert.AreEqual(figures.Count, asked.Count, "each figure asks for its model");
            Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Far), "the strategic-zoom mesh");
            Assert.IsTrue(figures.All(f => !UnitFigures.Dressed(f) && f.GetComponent<MeshRenderer>().enabled), "the code-built figures until the models arrive");
            var before = figures.ToDictionary(f => f, f => (f.position, f.rotation));
            foreach (var (id, _, done) in asked) done(Prefab(id));
            int dressed = 0;
            foreach (var f in figures)
            {
                Assert.IsTrue(UnitFigures.Dressed(f), $"{f.parent.name}/{f.name} shows Tal's soldier");
                var model = f.Find(UnitFigures.ModelName);
                Assert.AreEqual(Vector3.zero, model.localPosition, $"{f.name}: on the figure's feet");
                Assert.AreEqual(Quaternion.identity, model.localRotation, $"{f.name}: facing as the figure");
                Assert.Greater(model.GetComponentsInChildren<Renderer>().Count(r => r.enabled), 0, $"{f.name}: the model draws");
                Assert.AreEqual(before[f].position, f.position, $"{f.name}: the figure did not move");
                Assert.AreEqual(1, f.Cast<Transform>().Count(c => c.name == UnitFigures.ModelName), $"{f.name}: one model");
                dressed++;
            }
            Assert.Greater(dressed, 10, "dressed figures (not a vacuous pass)");
            // Tal's soldier in the frame of the code-built one: as tall within 15%, standing on its feet.
            foreach (var f in figures.Take(3))
            {
                var code = f.GetComponent<MeshFilter>().sharedMesh.bounds;
                var rs = f.Find(UnitFigures.ModelName).GetComponentsInChildren<Renderer>();
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float height = b.size.y / f.lossyScale.y, want = code.size.y;
                Debug.Log($"UNITS figures {f.parent.name}/{f.name}: model {height:0.00} m tall, code-built {want:0.00} m; feet {(b.min.y - f.position.y) / f.lossyScale.y:0.000} m");
                Assert.AreEqual(want, height, 0.15f * want, $"{f.name}: as tall as the code-built figure");
                Assert.AreEqual(0f, (b.min.y - f.position.y) / f.lossyScale.y, 0.08f, $"{f.name}: feet on the figure's origin");
            }
            // A roster change rebuilds the tokens: the new figures are dressed at once, with no new request.
            int n = asked.Count;
            Assert.IsTrue(layer.Show(C, StateAt(Day, 0.3)), "the call-up changes the roster");
            var rebuilt = Figures(map);
            Assert.Greater(rebuilt.Count, 5);
            Assert.IsTrue(rebuilt.All(UnitFigures.Dressed), "the rebuilt figures are dressed in the frame of the rebuild");
            Assert.AreEqual(n, asked.Count, "a model that has arrived is not asked for again");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A model that arrives after its token was rebuilt or destroyed is dropped quietly, and the showcase trio is
    /// dressed too.</summary>
    [Test]
    public void ALateModelForADestroyedTokenIsDroppedAndTheShowcaseIsDressed()
    {
        var map = BuildMap();
        var asked = new List<(string id, Action<GameObject> done)>();
        UnitFigures.Source = (id, d, done) => asked.Add((id, done));
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(C, StateAt(Day, 0.8));
            var first = asked.ToList(); asked.Clear();
            Assert.IsTrue(layer.Show(C, StateAt(Day, 0.3)), "the roster changes before any model arrives");
            Assert.Greater(asked.Count, 5, "the rebuilt figures ask again");
            foreach (var (id, done) in first) done(Prefab(id));   // for figures that no longer exist
            LogAssert.NoUnexpectedReceived();
            foreach (var (id, done) in asked) done(Prefab(id));
            Assert.IsTrue(Figures(map).All(UnitFigures.Dressed));
            var showcase = UnitShowcase.Spawn(map, still: true).transform;
            var trio = showcase.Cast<Transform>().Where(t => t.name.StartsWith("Rifleman")).ToList();
            Assert.AreEqual(3, trio.Count);
            Assert.IsTrue(trio.All(UnitFigures.Dressed), "the showcase trio wears the models that have arrived");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Fail open: with the people bundle missing, or damaged, the real stream answers null with one warning, and
    /// every figure keeps its code-built mesh, drawn.</summary>
    [Test]
    public void TheCodeBuiltFiguresStayWhenTheBundleIsMissingOrDamaged()
    {
        foreach (var damaged in new[] { false, true })
        {
            var folder = Path.Combine(Path.GetTempPath(), "statecraft-figures-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var map = BuildMap();
            try
            {
                if (damaged) File.WriteAllBytes(Path.Combine(folder, CollectionCatalog.People), System.Text.Encoding.ASCII.GetBytes("not-a-bundle"));
                UnitFigures.Forget();
                var stream = new CollectionStream(new CollectionStream.BundleSource(folder));
                UnitFigures.Source = (id, d, done) => stream.Enqueue(id, d, done);
                var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
                layer.Show(C, StateAt(Day, 0.8));
                LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle collection-people did not load \\(" + (damaged ? "not an AssetBundle" : "no file")));
                Step(stream.Run(), 50);
                var figures = Figures(map);
                Assert.Greater(figures.Count, 10, "the riflemen of 29 May 1948 (not a vacuous pass)");
                foreach (var f in figures)
                {
                    Assert.IsFalse(UnitFigures.Dressed(f), $"{(damaged ? "damaged" : "missing")}: {f.name} keeps the code-built figure");
                    Assert.IsNull(f.Find(UnitFigures.ModelName));
                    Assert.IsTrue(f.GetComponent<MeshRenderer>().enabled, $"{f.name}: drawn");
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); Directory.Delete(folder, true); }
        }
    }
}
