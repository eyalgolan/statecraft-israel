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
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #665: the 1948 Collection (Tal Heuberger, #507) on the army tokens, streamed after the first frame. Every one of
/// the original unit models retains its identity (owner, 17 October 2026, #663: every model is used): the Israeli figures,
/// the Arab Legion and the local irregular, and the 65 mm gun and the jeep where a source puts them. New Syrian, ALA and
/// Iraqi figures join their existing formations; the Egyptian figure awaits an approved map representation. Until a model
/// arrives, and when its bundle is missing or damaged, an Israeli token keeps its code-built figures (fail open); an Arab
/// force's token is not drawn at all (#740: no IDF figures, no placeholder).</summary>
public class UnitFiguresTests
{
    /// <summary>The legacy loop, whose roster follows the rows' dated windows and the call-up: these tests sweep the war's
    /// tokens by date (the GDD loop draws the sim's units, UnitsFromStateTests).</summary>
    static readonly Content C = SimBridgeGddTests.Legacy;
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static readonly DateTime May20 = new DateTime(1948, 5, 20), July12 = new DateTime(1948, 7, 12), LastArmistice = new DateTime(1949, 7, 20);

    /// <summary>The models on the existing war roster, including each force's own generated infantry. C09 is kept for local
    /// irregulars, which have no deployment row; importing art does not invent one.</summary>
    static readonly string[] WarModels =
    {
        "figure-c04-sabra-pioneer-v3", "figure-c05-pioneer-veteran-v2", "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2",
        "figure-c12f-palmach-scout-v2", "figure-c08-arab-legion-v2",
        "figure-syrian-infantry-1948-v1", "figure-ala-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
        "figure-vehicle-napoleonchik-65mm-1948-v2", "figure-vehicle-willys-jeep-1948-v2",
        "figure-soldier-1948-v1",
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
    static GameObject NearPrefab(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));

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
    /// arrived and the full call-up, the tokens draw exactly their mapped models, each in the Collection's streamed bundles with its far
    /// prefab on main.</summary>
    [Test]
    public void EveryMappedWarModelStandsOnATokenSomeDayOfTheWar()
    {
        var units = Units;
        var used = new HashSet<string>();
        for (var d = C.Rules.Start; d < LastArmistice; d = d.AddDays(1))
            foreach (var t in UnitRoster.Build(units, C.Rules, d, 5, 0.8, id => true).Where(t => t.Unit.Service == UnitData.Army))
                foreach (var id in ModelsOf(t)) { Assert.IsNotNull(id, $"{d:yyyy-MM-dd} {t}: every figure has a model"); used.Add(id); }
        CollectionAssert.AreEquivalent(WarModels, used);
        foreach (var id in WarModels)
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
        // The uniform default's third figure is the pioneer veteran, Harel's the Palmach scout; Yiftach's and Negev's the 1948
        // soldier, in the Palmach's stocking cap, where Negev's sourced third cap is that cap (so its token is C07, C06 and him).
        CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.PioneerVeteran }, Trio("givati"));
        CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.PalmachScout }, Trio("harel"));
        CollectionAssert.AreEqual(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit, UnitFigures.Soldier1948 }, Trio("yiftach"));
        CollectionAssert.AreEqual(new[] { UnitFigures.VeteranFighter, UnitFigures.YoungRecruit, UnitFigures.Soldier1948 }, Trio("negev"));
        CollectionAssert.AreEqual(new[] { "harel", "negev", "yiftach" }, UnitFigures.ThirdFigure.Keys.OrderBy(k => k, StringComparer.Ordinal),
            "the three Palmach brigades (10th, 11th and 12th Brigade (Palmach), their rows' numbers)");
        foreach (var id in UnitFigures.ThirdFigure.Keys) StringAssert.EndsWith("(Palmach)", units[id].Number, id);
        // A third figure whose sourced cap is not the soldier's keeps its headgear's figure.
        var helmeted = new UnitEntry { Id = "negev", Side = UnitData.Israel, Headgear = new List<string> { UnitData.WoolCap, UnitData.SteelHelmet, UnitData.SteelHelmet } };
        Assert.AreEqual(UnitFigures.YoungRecruit, UnitFigures.FigureFor(helmeted, 2, UnitData.SteelHelmet), "a source's steel helmet is never swapped for his cap");
        CollectionAssert.AreEqual(Enumerable.Repeat(UnitFigures.ArabLegion, 3), Trio("arab_legion"));
        CollectionAssert.AreEqual(Enumerable.Repeat(UnitFigures.AlaInfantry, 3), Trio("arab_liberation_army"));
        Assert.IsNull(UnitFigures.SideFigure("lebanon"), "Lebanese art remains review-only and its row stays hidden");
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
        foreach (var id in WarModels.Where(i => i != UnitFigures.Soldier1948 && !i.StartsWith("figure-vehicle-", StringComparison.Ordinal)))
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
            CollectionAssert.AreEquivalent(WarModels, asked.Select(a => a.id), "each token model once, including the forces' own figures prefetched ahead");
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
                // #336's soldier stands with his arms a little out from his sides (0.550 of his height across, measured):
                // still far from a T-pose, which is about as wide as it is tall.
                float arms = f.Find(UnitFigures.ModelName).GetChild(0).name.StartsWith(UnitFigures.Soldier1948, StringComparison.Ordinal) ? 0.56f : 0.55f;
                Assert.Less(Mathf.Max(b.size.x, b.size.z), arms * b.size.y, $"{f.name}: standing upright, arms down");
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
            Assert.IsTrue(map.Terrain.transform.Find(UnitLayer.RootName).TryGetComponent<UnitFigureDetail>(out _),
                "the close-zoom switch stands on the unit layer's root, and goes with it");
            // A roster change rebuilds the tokens: the new pieces are dressed at once, with no new request.
            int n = asked.Count;
            Assert.IsTrue(layer.Show(C, StateAt(July12, 0.3)), "the call-up changes the roster");
            Assert.Greater(Pieces(map, "Rifleman").Count, 5);
            Assert.IsTrue(Pieces(map, "Rifleman", "Vehicle").All(UnitFigures.Dressed), "the rebuilt pieces are dressed in the frame of the rebuild");
            Assert.AreEqual(n, asked.Count, "a model that has arrived is not asked for again");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A figure standing on its own (a showcase figure, by the code-built rifleman), dressed with model
    /// <paramref name="id"/> once its far model is answered.</summary>
    static Transform DressedFigure(Transform root, string name, string id, Vector3 at, List<(string id, CollectionDetail detail, Action<GameObject> done)> asked)
    {
        var piece = UnitShowcase.Piece(root, name, UnitModels.Rifleman(UnitModels.Headgear.SlouchHat, UnitModels.RiflePose.Ready, false, name),
            Load("Terrain"), at, Quaternion.Euler(0f, 30f, 0f), UnitShowcase.FigureScale).transform;
        UnitFigures.Dress(piece, id);
        asked.Single(a => a.id == id && a.detail == CollectionDetail.Far).done(Prefab(id));
        Assert.IsTrue(UnitFigures.Dressed(piece), $"{name}: the far model is on");
        return piece;
    }

    static Renderer[] FarOf(Transform piece) => piece.Find(UnitFigures.ModelName).GetChild(0).GetComponentsInChildren<Renderer>(true);
    static Transform NearOf(Transform piece) => piece.Find(UnitFigures.ModelName).Find(UnitFigureDetail.NearName);

    /// <summary>Issue #665, close zoom (#663: "full detail and the idle clip only at close zoom"): a camera that comes in
    /// below TownModelLayer.CloseEnterM asks for the near model (and not before); once it arrives the figure shows it, skinned
    /// and playing its idle clip, as tall as the far model and on its feet, under the same holder (so the camera's yaw, #644,
    /// turns it too); the far mesh is off. The band's hysteresis holds it to CloseExitM; beyond, the far mesh is back and the
    /// clip stops. Coming close again asks for nothing new.</summary>
    [Test]
    public void AFigureShowsItsSkinnedIdleModelOnlyCloseUp()
    {
        var root = new GameObject("Units");
        var asked = new List<(string id, CollectionDetail detail, Action<GameObject> done)>();
        UnitFigures.Source = (id, detail, done) => asked.Add((id, detail, done));
        var camera = TopDownCamera(root.transform);
        try
        {
            var piece = DressedFigure(root.transform, "Rifleman0", UnitFigures.SabraPioneer, Vector3.zero, asked);
            Assert.IsTrue(root.TryGetComponent<UnitFigureDetail>(out var detail), "the switch stands on the figures' root");
            int Nears() => asked.Count(a => a.detail == CollectionDetail.Near);

            View(detail, camera, TownModelLayer.CloseEnterM + 1f);
            Assert.AreEqual(0, Nears(), "just beyond the switch the near model is not asked for");
            Assert.IsNull(NearOf(piece));
            Assert.IsTrue(FarOf(piece).All(r => !r.forceRenderingOff), "far off: the far mesh draws");

            View(detail, camera, TownModelLayer.CloseEnterM - 1f);
            Assert.AreEqual(1, Nears(), "close: the near model is asked for, once");
            Assert.IsTrue(FarOf(piece).All(r => !r.forceRenderingOff), "the far mesh draws while the near one streams");
            asked.Single(a => a.detail == CollectionDetail.Near).done(NearPrefab(UnitFigures.SabraPioneer));

            var holder = piece.Find(UnitFigures.ModelName);
            var near = NearOf(piece);
            Assert.IsNotNull(near, "the near model stands beside the far one");
            Assert.AreEqual(holder, near.parent, "under the same holder, so the figure's yaw turns it (#644)");
            Assert.AreEqual(NearPrefab(UnitFigures.SabraPioneer).transform.localRotation, near.localRotation, "the prefab keeps its own rotation (the FBX axis turn)");
            Assert.IsTrue(near.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0, "the near model is the skinned one");
            Assert.IsTrue(FarOf(piece).All(r => r.forceRenderingOff), "close: the far mesh is off");
            Assert.IsTrue(near.GetComponentsInChildren<Renderer>(true).All(r => !r.forceRenderingOff), "close: the near model draws");
            var animators = near.GetComponentsInChildren<Animator>(true);
            Assert.Greater(animators.Length, 0, "the near model has its animator");
            Assert.IsTrue(animators.All(a => a.enabled && a.runtimeAnimatorController != null), "close: the idle clip plays");
            // Sized by what Unity draws: the renderers' own world bounds (a skinned mesh's through its bones), not by BoxIn.
            var far = WorldBounds(holder.GetChild(0)); var box = WorldBounds(near);
            Assert.AreEqual(far.size.y, box.size.y, 0.05f * far.size.y, "as tall as the far model");
            Assert.AreEqual(far.min.y, box.min.y, 0.05f * far.size.y, "feet where the far model's are");
            Assert.AreEqual(0f, new Vector2(box.center.x - far.center.x, box.center.z - far.center.z).magnitude, 0.1f * far.size.y, "centred over them");

            View(detail, camera, TownModelLayer.CloseExitM - 1f);
            Assert.IsTrue(FarOf(piece).All(r => r.forceRenderingOff), "the band's hysteresis: still close inside CloseExitM");
            View(detail, camera, TownModelLayer.CloseExitM + 1f);
            Assert.IsTrue(FarOf(piece).All(r => !r.forceRenderingOff), "beyond the band: the far mesh again");
            Assert.IsTrue(near.GetComponentsInChildren<Renderer>(true).All(r => r.forceRenderingOff), "and the near model off");
            Assert.IsTrue(animators.All(a => !a.enabled), "the idle clip stops far off");

            View(detail, camera, TownModelLayer.CloseEnterM - 1f);
            Assert.AreEqual(1, Nears(), "close again: nothing asked for anew");
            Assert.AreEqual(1, piece.Find(UnitFigures.ModelName).Cast<Transform>().Count(c => c.name == UnitFigureDetail.NearName), "one near model");
            Assert.IsTrue(animators.All(a => a.enabled), "the idle clip plays again");
        }
        finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(root); }
    }

    /// <summary>Close up, a near model that never comes (null: a missing or damaged bundle) leaves the far mesh drawing (fail
    /// open); and C05, the pioneer veteran, has no idle clip (walk and run only), so his near model is never asked for and he
    /// keeps his still far model (owner, 18 October 2026, #665: "Keep its still model"), while the figures beside him swap.</summary>
    [Test]
    public void AMissingNearModelKeepsTheFarMeshAndC05KeepsHisStillModel()
    {
        var root = new GameObject("Units");
        var asked = new List<(string id, CollectionDetail detail, Action<GameObject> done)>();
        UnitFigures.Source = (id, detail, done) => asked.Add((id, detail, done));
        var camera = TopDownCamera(root.transform);
        try
        {
            var c04 = DressedFigure(root.transform, "C04", UnitFigures.SabraPioneer, Vector3.zero, asked);
            var c05 = DressedFigure(root.transform, "C05", UnitFigures.PioneerVeteran, Vector3.right, asked);
            var c06 = DressedFigure(root.transform, "C06", UnitFigures.YoungRecruit, Vector3.left, asked);
            Assert.IsNotNull(NearPrefab(UnitFigures.PioneerVeteran), "C05 has a near prefab, so leaving it out is a choice (not a vacuous pass)");
            View(root.GetComponent<UnitFigureDetail>(), camera, TownModelLayer.CloseEnterM - 1f);
            var nears = asked.Where(a => a.detail == CollectionDetail.Near).ToList();
            CollectionAssert.AreEquivalent(new[] { UnitFigures.SabraPioneer, UnitFigures.YoungRecruit }, nears.Select(a => a.id),
                "C05 has no idle clip: his near model is never asked for");
            nears.Single(a => a.id == UnitFigures.SabraPioneer).done(null);
            nears.Single(a => a.id == UnitFigures.YoungRecruit).done(NearPrefab(UnitFigures.YoungRecruit));
            Assert.IsNull(NearOf(c04), "C04's near model did not come");
            Assert.IsTrue(FarOf(c04).All(r => !r.forceRenderingOff), "so C04's far mesh draws (fail open)");
            Assert.IsNull(NearOf(c05), "C05 has no near model");
            Assert.IsTrue(FarOf(c05).All(r => !r.forceRenderingOff), "C05 keeps his still far model close up");
            Assert.IsNotNull(NearOf(c06), "C06 swaps beside him");
            Assert.IsTrue(FarOf(c06).All(r => r.forceRenderingOff));
        }
        finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(root); }
    }

    static Bounds WorldBounds(Transform model)
    {
        var rs = model.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static Camera TopDownCamera(Transform parent)
    {
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.SetParent(parent, false);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // looking straight down: the view distance is its height
        return camera;
    }

    static void View(UnitFigureDetail detail, Camera camera, float distanceM)
    {
        camera.transform.position = new Vector3(0f, distanceM, 0f);
        Assert.AreEqual(distanceM, TownModelLayer.ViewDistance(camera), 1f);
        detail.Choose(default, camera);
    }

    /// <summary>The Arab forces are drawn only once their own figure has arrived, three of it on
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
            CollectionAssert.IsSubsetOf(new[] { UnitFigures.ArabLegion, UnitFigures.AlaInfantry,
                UnitFigures.SyrianInfantry, UnitFigures.IraqiInfantry }, asked.Keys, "each deployed force's figure is asked for");
            Assert.IsFalse(layer.Tokens.Any(t => t.Token.Unit.Side != UnitData.Israel), "no Arab force before its figure arrives");
            asked[UnitFigures.AlaInfantry](null);   // the ALA's figure never comes
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
