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

/// <summary>Issue #665: the 1948 Collection (MATKOT, #507) on the army tokens, streamed after the first frame. Every one of
/// the nine unit models is placed (owner, 17 October 2026, #663: every model is used): the five Israeli figures, the Arab
/// Legion and the irregular on their forces' tokens, and the 65 mm gun and the jeep where a source puts them. Until a model
/// arrives, and when its bundle is missing or damaged, an Israeli token keeps its code-built figures (fail open); an Arab
/// force's token is not drawn at all (#740: no IDF figures, no placeholder).</summary>
public class UnitFiguresTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static readonly DateTime May20 = new DateTime(1948, 5, 20), July12 = new DateTime(1948, 7, 12), LastArmistice = new DateTime(1949, 7, 20);

    /// <summary>The nine unit models of #665.</summary>
    static readonly string[] Nine =
    {
        "figure-c04-sabra-pioneer-v3", "figure-c05-pioneer-veteran-v2", "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2",
        "figure-c12f-palmach-scout-v2", "figure-c08-arab-legion-v2", "figure-c09-irregular-v2",
        "figure-vehicle-napoleonchik-65mm-1948-v2", "figure-vehicle-willys-jeep-1948-v2",
    };

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

    static Transform Tokens(MapScene map) => map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");

    /// <summary>Every figure and vehicle piece of the drawn tokens (code-built riflemen, an Arab force's figures, vehicles).</summary>
    static List<Transform> Pieces(MapScene map, params string[] prefixes) => Tokens(map).Cast<Transform>()
        .SelectMany(t => t.Cast<Transform>().Where(x => prefixes.Any(x.name.StartsWith))).ToList();

    static GameObject Prefab(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    /// <summary>The model ids a token draws: its figures', then its vehicles'.</summary>
    static IEnumerable<string> ModelsOf(UnitToken t) =>
        Enumerable.Range(0, t.Figures).Select(i => UnitFigures.FigureFor(t.Unit, i, i < t.Headgear.Count ? t.Headgear[i] : null)).Concat(t.Vehicles);

    /// <summary>The box of a piece's Collection model in the piece's own frame (real metres before FigureScale).</summary>
    static Bounds ModelBox(Transform piece)
    {
        var model = piece.Find(UnitFigures.ModelName);
        Bounds? box = null;
        foreach (var f in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var b = f.sharedMesh.bounds; var m = piece.worldToLocalMatrix * f.transform.localToWorldMatrix;
            for (int c = 0; c < 8; c++)
            {
                var p = m.MultiplyPoint3x4(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z));
                if (box is Bounds bb) { bb.Encapsulate(p); box = bb; } else box = new Bounds(p, Vector3.zero);
            }
        }
        return box!.Value;
    }

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

    /// <summary>The owner: "make sure we are using all of [his] models" (#663). Over every day of the war, with every model
    /// arrived and the full call-up, the tokens draw exactly the nine, each in the Collection's streamed bundles with its far
    /// prefab on main.</summary>
    [Test]
    public void EveryOneOfTheNineModelsStandsOnATokenSomeDayOfTheWar()
    {
        var units = Units;
        var used = new HashSet<string>();
        for (var d = C.Rules.Start; d < LastArmistice; d = d.AddDays(1))
            foreach (var t in UnitRoster.Build(units, C.Rules, d, 5, 0.8, id => true).Where(t => t.Unit.Service == UnitData.Army))
                foreach (var id in ModelsOf(t)) { Assert.IsNotNull(id, $"{d:yyyy-MM-dd} {t}: every figure has a model"); used.Add(id); }
        CollectionAssert.AreEquivalent(Nine, used);
        foreach (var id in Nine)
        {
            Assert.IsNotNull(CollectionCatalog.BundleOf(id), $"{id}: streamed");
            Assert.IsNotNull(Prefab(id), $"{id}: its far prefab is on main");
        }
    }

    /// <summary>The mapping table (docs/units.md, "The 1948 Collection on the tokens").</summary>
    [Test]
    public void TheTableGivesEachTokenItsFiguresAndVehicles()
    {
        var units = Units.ToDictionary(u => u.Id);
        string[] Trio(string id) => Enumerable.Range(0, 3).Select(i => UnitFigures.FigureFor(units[id], i, (units[id].Headgear ?? UnitData.DefaultHeadgear.ToList())[i])).ToArray();
        // By headgear, as #507 paired them: bush hat C04, steel helmet C06, stocking cap C07.
        CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.VeteranFighter },
            UnitData.Headgears.Select(h => UnitFigures.ByHeadgear[h]).ToArray());
        // The uniform default's third figure is the pioneer veteran, Harel's the Palmach scout; Negev's headgear is sourced.
        CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.PioneerVeteran }, Trio("givati"));
        CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.PalmachScout }, Trio("harel"));
        CollectionAssert.AreEqual(new[] { UnitFigures.VeteranFighter, UnitFigures.YoungRecruit, UnitFigures.VeteranFighter }, Trio("negev"));
        CollectionAssert.AreEqual(Enumerable.Repeat(UnitFigures.ArabLegion, 3), Trio("arab_legion"));
        CollectionAssert.AreEqual(Enumerable.Repeat(UnitFigures.Irregular, 3), Trio("arab_liberation_army"));
        foreach (var side in UnitData.Sides.Where(s => s != UnitData.Israel && s != "jordan" && s != "ala"))
            Assert.IsNull(UnitFigures.SideFigure(side), $"{side}: no figure, so its rows stay data only");
        // The vehicles from their sourced days.
        CollectionAssert.IsEmpty(UnitFigures.VehiclesAt(units["harel"], new DateTime(1948, 5, 31)));
        CollectionAssert.AreEqual(new[] { UnitFigures.WillysJeep }, UnitFigures.VehiclesAt(units["harel"], new DateTime(1948, 6, 1)), "the Burma Road jeeps");
        CollectionAssert.AreEqual(new[] { UnitFigures.WillysJeep, UnitFigures.Napoleonchik }, UnitFigures.VehiclesAt(units["harel"], July12));
        CollectionAssert.AreEqual(new[] { UnitFigures.WillysJeep }, UnitFigures.VehiclesAt(units["negev"], new DateTime(1948, 6, 2)), "the Negev Beasts' jeeps");
        foreach (var id in new[] { "carmeli", "givati", "yiftach" })
        {
            CollectionAssert.IsEmpty(UnitFigures.VehiclesAt(units[id], new DateTime(1948, 7, 10)), id);
            CollectionAssert.AreEqual(new[] { UnitFigures.Napoleonchik }, UnitFigures.VehiclesAt(units[id], new DateTime(1948, 7, 11)), $"{id}: the 11 July distribution");
        }
        CollectionAssert.IsEmpty(UnitFigures.VehiclesAt(units["golani"], July12), "no source puts either with Golani");
        // A token's vehicles change the roster on their day, so the tokens are rebuilt with them.
        var before = UnitRoster.Build(Units, C.Rules, new DateTime(1948, 5, 31), 5, 0.8);
        var after = UnitRoster.Build(Units, C.Rules, new DateTime(1948, 6, 1), 5, 0.8);
        Assert.IsFalse(before.SequenceEqual(after), "Harel's jeep arrives on 1 June 1948");
    }

    /// <summary>Every figure on a token stands with its arms down in its far prefab, the one the tokens draw (C05 shipped in
    /// his T-pose: his static meshes are now posed at his walk's first frame, tools/slim_collection.py POSE_CLIP).</summary>
    [Test]
    public void EveryFigureOnATokenStandsWithItsArmsDown()
    {
        foreach (var id in Nine.Where(i => i.StartsWith("figure-c")))
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(id));
            try
            {
                var rs = go.GetComponentsInChildren<Renderer>();
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                Debug.Log($"UNITS figures {id}: {b.size.x:0.000} x {b.size.z:0.000}, {b.size.y:0.000} tall");
                Assert.Less(Mathf.Max(b.size.x, b.size.z), 0.55f * b.size.y, $"{id}: arms down, not a T-pose (wider than half his height)");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }

    /// <summary>The tokens are built with the code-built figures and ask for their models; nothing changes in the frame of
    /// the request. When the models arrive every figure shows its model in its own frame, upright, as tall as the code-built
    /// figure, with the code-built renderer off and the figure where it stood; the vehicles arrive on their bases, side-on
    /// and lower than a figure. A roster rebuild dresses the new pieces at once, without asking again.</summary>
    [Test]
    public void TheFiguresAndVehiclesSwapInWhenTheModelsArrive()
    {
        var map = BuildMap();
        var asked = new List<(string id, CollectionDetail detail, Action<GameObject> done)>();
        UnitFigures.Source = (id, d, done) => asked.Add((id, d, done));
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(C, StateAt(July12, 0.8));
            var figures = Pieces(map, "Rifleman");
            var vehicles = Pieces(map, "Vehicle");
            Assert.Greater(figures.Count, 10, "the riflemen of 12 July 1948 (not a vacuous pass)");
            CollectionAssert.AreEquivalent(new[] { "carmeli", "givati", "harel", "harel", "yiftach", "negev", "negev" }, vehicles.Select(v => v.parent.name),
                "the guns of the 11 July distribution and the Harel and Negev jeeps");
            Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Far), "the strategic-zoom mesh");
            CollectionAssert.AreEquivalent(Nine, asked.Select(a => a.id), "each model once: the seven on the tokens, and the two forces' figures asked for ahead");
            Assert.IsTrue(figures.All(f => !UnitFigures.Dressed(f) && f.GetComponent<MeshRenderer>().enabled), "the code-built figures until the models arrive");
            Assert.IsTrue(vehicles.All(v => v.childCount == 0 && v.GetComponentsInChildren<Renderer>().Length == 0), "no vehicle is drawn before it arrives");
            var before = figures.ToDictionary(f => f, f => (f.position, f.rotation));
            foreach (var (id, _, done) in asked.ToList()) done(Prefab(id));
            float man = UnitBadge.FigureHeightM;
            foreach (var f in figures)
            {
                Assert.IsTrue(UnitFigures.Dressed(f), $"{f.parent.name}/{f.name} shows its model");
                Assert.AreEqual(1, f.Cast<Transform>().Count(c => c.name == UnitFigures.ModelName), $"{f.name}: one model");
                Assert.AreEqual(before[f].position, f.position, $"{f.name}: the figure did not move");
                Assert.AreEqual(before[f].rotation, f.rotation, $"{f.name}: nor turned");
                var holder = f.Find(UnitFigures.ModelName);
                var prefab = Prefab(UnitFigures.FigureFor(Units.Single(u => u.Id == f.parent.name), int.Parse(f.name.Substring(8)),
                    layer.Tokens.Single(t => t.Token.Unit.Id == f.parent.name).Token.Headgear[int.Parse(f.name.Substring(8))]));
                Assert.AreEqual(prefab.transform.localRotation, holder.GetChild(0).localRotation, $"{f.name}: the prefab keeps its own rotation (the FBX axis turn)");
                var b = ModelBox(f); float want = f.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
                Assert.AreEqual(want, b.size.y, 0.02f * want, $"{f.name}: as tall as the code-built figure");
                Assert.Less(Mathf.Max(b.size.x, b.size.z), 0.55f * b.size.y, $"{f.name}: standing upright, arms down");
                Assert.AreEqual(0f, b.min.y, 0.01f, $"{f.name}: feet on the figure's origin");
            }
            foreach (var v in vehicles)
            {
                Assert.IsTrue(UnitFigures.Dressed(v), $"{v.parent.name}/{v.name} shows its vehicle");
                var b = ModelBox(v);
                Assert.AreEqual(UnitFigures.VehicleLengthShare * man, Mathf.Max(b.size.x, b.size.z), 0.01f, $"{v.parent.name}/{v.name}: sized against the figures");
                Assert.Less(b.size.y, 0.6f * man, $"{v.parent.name}/{v.name}: upright (lower than a man), not on its end");
                Assert.AreEqual(0f, b.min.y, 0.01f, $"{v.parent.name}/{v.name}: on its wheels");
                var plinth = v.parent.Find(UnitPlinth.PlinthName).localScale.x;
                var centre = v.parent.Find(UnitPlinth.PlinthName).position;
                Assert.Less(new Vector2(v.position.x - centre.x, v.position.z - centre.z).magnitude, 0.75f * plinth, $"{v.parent.name}/{v.name}: on its base");
            }
            // A roster change rebuilds the tokens: the new pieces are dressed at once, with no new request.
            int n = asked.Count;
            Assert.IsTrue(layer.Show(C, StateAt(July12, 0.3)), "the call-up changes the roster");
            Assert.Greater(Pieces(map, "Rifleman").Count, 5);
            Assert.IsTrue(Pieces(map, "Rifleman", "Vehicle").All(UnitFigures.Dressed), "the rebuilt pieces are dressed in the frame of the rebuild");
            Assert.AreEqual(n, asked.Count, "a model that has arrived is not asked for again");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The Arab Legion and the Arab Liberation Army are drawn only once their own figure has arrived, three of it on
    /// a base under the neutral pennant, never an IDF figure, and on ground no Israeli force held on the window's first day
    /// (#226, ruling 9). A figure that never comes (null: a missing or damaged bundle) draws nothing, and warns nothing more.</summary>
    [Test]
    public void TheArabForcesAreDrawnOnlyWithTheirOwnFiguresOnceTheyArrive()
    {
        var map = BuildMap();
        var asked = new Dictionary<string, Action<GameObject>>();
        UnitFigures.Source = (id, d, done) => asked[id] = done;
        try
        {
            var units = Units;
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            layer.Show(C, StateAt(May20, 0.8));
            Assert.IsTrue(asked.ContainsKey(UnitFigures.ArabLegion) && asked.ContainsKey(UnitFigures.Irregular), "the two forces' figures are asked for");
            Assert.IsFalse(layer.Tokens.Any(t => t.Token.Unit.Side != UnitData.Israel), "no Arab force before its figure arrives");
            asked[UnitFigures.Irregular](null);   // the ALA's figure never comes
            Assert.IsFalse(layer.Tokens.Any(t => t.Token.Unit.Side != UnitData.Israel), "a figure that never comes draws nothing");
            asked[UnitFigures.ArabLegion](Prefab(UnitFigures.ArabLegion));
            var legion = layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel).ToList();
            CollectionAssert.AreEqual(new[] { "arab_legion" }, legion.Select(t => t.Token.Unit.Id), "the Legion, drawn when its figure arrives");
            var go = Tokens(map).Find("arab_legion");
            Assert.AreEqual(0, go.Cast<Transform>().Count(c => c.name.StartsWith("Rifleman")), "no IDF figure");
            var figures = go.Cast<Transform>().Where(c => c.name.StartsWith("Figure")).ToList();
            Assert.AreEqual(3, figures.Count);
            foreach (var f in figures)
            {
                Assert.IsTrue(UnitFigures.Dressed(f), f.name);
                StringAssert.StartsWith(UnitFigures.ArabLegion, f.Find(UnitFigures.ModelName).GetChild(0).name, $"{f.name}: the Legion's own figure");
                var b = ModelBox(f);
                Assert.AreEqual(UnitBadge.FigureHeightM, b.size.y, 0.02f, $"{f.name}: a man's height");
                Assert.Less(Mathf.Max(b.size.x, b.size.z), 0.55f * b.size.y, $"{f.name}: upright");
            }
            var flag = go.Find(UnitPlinth.FlagName);
            Assert.IsNotNull(flag, "a marker on the pole");
            Assert.AreEqual(UnitFlags.PennantMeshName, flag.GetComponent<MeshFilter>().sharedMesh.name, "the neutral pennant (Q-ART-1), not a flag");
            // On the force's own ground (#226, ruling 9): the spot's hex is not Israeli-held on the window's first day.
            var control = Control1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Control1948.FileName)));
            foreach (var u in units.Where(u => u.Side != UnitData.Israel && UnitRoster.OnMap(u)))
                foreach (var a in u.Anchors)
                {
                    Assert.IsTrue(layer.Spots.TryGetValue(a, out var spot), $"{u.Id} at {a.Place}: placed");
                    var side = control.At(spot.Q, spot.R, UnitData.Day(a.From)).side;
                    TestContext.WriteLine($"{u.Id} at {a.Place}: hex ({spot.Q},{spot.R}) {side} on {a.From}");
                    Assert.AreNotEqual(Control1948.Side.Israel, side, $"{u.Id} at {a.Place}: not on ground Israel held");
                }
            LogAssert.NoUnexpectedReceived();
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A model that arrives after its token was rebuilt or destroyed is dropped quietly, and the showcase trio is
    /// dressed by headgear too (C04, C06, C07).</summary>
    [Test]
    public void ALateModelForADestroyedTokenIsDroppedAndTheShowcaseIsDressed()
    {
        var map = BuildMap();
        var asked = new List<(string id, Action<GameObject> done)>();
        UnitFigures.Source = (id, d, done) => asked.Add((id, done));
        try
        {
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(C, StateAt(July12, 0.8));
            Assert.IsTrue(layer.Show(C, StateAt(July12, 0.3)), "the roster changes before any model arrives");
            Assert.AreEqual(asked.Select(a => a.id).Distinct().Count(), asked.Count, "each model is asked for once, rebuild or not");
            foreach (var (id, done) in asked.ToList()) done(Prefab(id));   // the first figures no longer exist
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(Pieces(map, "Rifleman", "Vehicle").All(UnitFigures.Dressed), "the rebuilt pieces wear the models that arrived");
            var showcase = UnitShowcase.Spawn(map, still: true).transform;
            var trio = showcase.Cast<Transform>().Where(t => t.name.StartsWith("Rifleman")).ToList();
            Assert.AreEqual(3, trio.Count);
            Assert.IsTrue(trio.All(UnitFigures.Dressed), "the showcase trio wears the models that have arrived");
            CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.VeteranFighter },
                trio.Select(t => UnitFigures.ByHeadgear.Values.Single(id => t.Find(UnitFigures.ModelName).GetChild(0).name.StartsWith(id))));
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Fail open: with the people and vehicles bundles missing, or damaged, the real stream answers null with one
    /// warning per bundle; every Israeli figure keeps its code-built mesh, drawn, no vehicle is drawn, and no Arab force.</summary>
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
                if (damaged)
                    foreach (var b in new[] { CollectionCatalog.People, CollectionCatalog.Vehicles })
                        File.WriteAllBytes(Path.Combine(folder, b), System.Text.Encoding.ASCII.GetBytes("not-a-bundle"));
                UnitFigures.Forget();
                var stream = new CollectionStream(new CollectionStream.BundleSource(folder));
                UnitFigures.Source = (id, d, done) => stream.Enqueue(id, d, done);
                var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
                layer.Show(C, StateAt(May20, 0.8));
                layer.Show(C, StateAt(July12, 0.8));
                foreach (var b in new[] { CollectionCatalog.People, CollectionCatalog.Vehicles })
                    LogAssert.Expect(LogType.Warning, new Regex("^Collection: bundle " + b + " did not load \\(" + (damaged ? "not an AssetBundle" : "no file")));
                Step(stream.Run(), 80);
                var figures = Pieces(map, "Rifleman");
                Assert.Greater(figures.Count, 10, "the riflemen of 12 July 1948 (not a vacuous pass)");
                foreach (var f in figures)
                {
                    Assert.IsFalse(UnitFigures.Dressed(f), $"{(damaged ? "damaged" : "missing")}: {f.name} keeps the code-built figure");
                    Assert.IsTrue(f.GetComponent<MeshRenderer>().enabled, $"{f.name}: drawn");
                }
                Assert.IsTrue(Pieces(map, "Vehicle").All(v => v.GetComponentsInChildren<Renderer>().Length == 0), "no vehicle is drawn");
                layer.Show(C, StateAt(May20, 0.8));
                Assert.IsFalse(layer.Tokens.Any(t => t.Token.Unit.Side != UnitData.Israel), "no Arab force, and no placeholder");
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); Directory.Delete(folder, true); }
        }
    }
}
