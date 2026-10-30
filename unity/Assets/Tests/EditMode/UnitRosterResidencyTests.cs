#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Object = UnityEngine.Object;

/// <summary>Regression R1: a roster token owns the strategic Far originals for its full lifetime. Camera residency still
/// controls Near only, so leaving the approved unit band retires close detail without leaving a drawn bare plinth.</summary>
public class UnitRosterResidencyTests
{
    const float StartViewM = 90000f;
    const float DisplacedViewM = 11800f;
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));

    sealed class Lease : IDisposable
    {
        Action? release;
        internal Lease(Action release) { this.release = release; }
        public void Dispose() { var done = release; release = null; done?.Invoke(); }
    }

    [SetUp]
    public void Fresh()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        UnitFigures.Source = null;
        UnitFigures.Forget();
    }

    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UnitFigures.Source = null;
        UnitFigures.Forget();
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static MapScene BuildMap()
    {
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        KitPopulation.Use(KitPopulation.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", KitPopulation.FileName))));
        map.Build();
        return map;
    }

    static GameState JuneState()
    {
        var state = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        state.Date = new DateTime(1948, 6, 15);
        return state;
    }

    static Camera TopDownCamera(float height, Vector3 focus)
    {
        var camera = new GameObject("TEST ONLY residency camera").AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(new Vector3(focus.x, height, focus.z), Quaternion.Euler(90f, 0f, 0f));
        return camera;
    }

    static GameObject Prefab(string id, CollectionDetail detail) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));

    static Dictionary<string, Action<GameObject?>> PendingSource()
    {
        var pending = new Dictionary<string, Action<GameObject?>>(StringComparer.Ordinal);
        UnitFigures.Source = (id, detail, done) =>
        {
            Assert.AreEqual(CollectionDetail.Far, detail, "no Near request before a close-detail switch");
            pending[id] = done;
        };
        return pending;
    }

    static void AnswerAll(Dictionary<string, Action<GameObject?>> pending)
    {
        foreach (var (id, done) in pending.ToArray()) done(Prefab(id, CollectionDetail.Far));
    }

    [Test]
    public void StartViewRetainsEveryOnMapNonIsraeliTokenAfterItsFakeSourceAnswers()
    {
        var map = BuildMap();
        var pending = PendingSource();
        var camera = TopDownCamera(StartViewM, Vector3.zero);
        try
        {
            var rows = Rows;
            var state = JuneState();
            var expectedSides = rows.Where(UnitRoster.OnMap).Where(u => u.Side != UnitData.Israel)
                .Select(u => u.Side).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var candidates = UnitRoster.FromState(rows, C, state, _ => true, (_, start, _, _) => start)
                .Where(t => t.Unit.Side != UnitData.Israel).ToArray();
            CollectionAssert.AreEqual(expectedSides, candidates.Select(t => t.Unit.Side).Distinct()
                .OrderBy(s => s, StringComparer.Ordinal).ToArray(), "fixture date contains every on-map non-Israeli side");
            var expectedTokens = candidates.Select(t => t.Unit.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();

            var layer = UnitLayer.Create(map, rows, C.Hexes, still: true);
            layer.Show(C, state);
            foreach (var holder in layer.GetComponentsInChildren<UnitFigureResidency>(true)) holder.Choose(default, camera);
            Assert.AreEqual(StartViewM, TownModelLayer.ViewDistance(camera), 1f, "the verified 90 km start view");

            AnswerAll(pending);

            var actual = layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel).Select(t => t.Token.Unit.Id)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expectedTokens, actual, "source answers restore every side's tokens even outside camera residency");
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void DisplacedFocusNeverLeavesAFigureHolderOnABarePlinth()
    {
        var map = BuildMap();
        var pending = PendingSource();
        Camera? camera = null;
        try
        {
            var layer = UnitLayer.Create(map, Rows, C.Hexes, still: true);
            layer.Show(C, JuneState());
            AnswerAll(pending);
            Assert.IsNotEmpty(layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel), "fixture draws the foreign tokens");

            var subject = layer.Tokens.First(t => t.Token.Unit.Side != UnitData.Israel);
            var policy = ProductionResidency.Approved().Units;
            var focus = new Vector3((float)subject.Spot.X + policy.ExitM + policy.EnterM, 0f, (float)subject.Spot.Z);
            camera = TopDownCamera(DisplacedViewM, focus);
            Assert.AreEqual(DisplacedViewM, TownModelLayer.ViewDistance(camera), 1f, "the verified 11.8 km view");
            Assert.Greater(Vector2.Distance(new Vector2(focus.x, focus.z), new Vector2((float)subject.Spot.X, (float)subject.Spot.Z)),
                policy.ExitM, "focus is beyond the approved leave radius from the token's displaced placement spot");
            foreach (var holder in layer.GetComponentsInChildren<UnitFigureResidency>(true)) holder.Choose(default, camera);

            var tokenRoot = layer.transform.Find("Tokens");
            var holders = tokenRoot.GetComponentsInChildren<UnitFigureResidency>(true);
            Assert.IsNotEmpty(holders, "drawn army tokens have figure or vehicle holders");
            foreach (var holder in holders)
                Assert.IsNotNull(holder.transform.Find(UnitFigures.ModelName), $"{holder.transform.parent.name}/{holder.name}: no bare plinth");
        }
        finally
        {
            if (camera != null) Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void NearLeaseStillRetiresWhenCameraLeavesWhileRosterFarRemains()
    {
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var live = new List<CollectionDetail>();
        IDisposable Acquire(string _, CollectionDetail detail, Action<GameObject?> answer)
        {
            live.Add(detail);
            answer(prefab);
            return new Lease(() => live.Remove(detail));
        }
        var policy = ProductionResidency.Approved().Units;
        var camera = TopDownCamera(DisplacedViewM, new Vector3(policy.ExitM + policy.EnterM, 0f, 0f));
        try
        {
            var owner = piece.AddComponent<UnitFigureResidency>();
            owner.Initialize(UnitFigures.YoungRecruit, policy, Acquire, retainFar: true);
            owner.RequestNear();
            CollectionAssert.AreEquivalent(new[] { CollectionDetail.Far, CollectionDetail.Near }, live,
                "fixture starts with both roster Far and admitted Near leases");

            owner.Choose(default, camera);

            CollectionAssert.AreEqual(new[] { CollectionDetail.Far }, live, "leaving retires Near but keeps roster Far");
            Assert.IsNotNull(piece.transform.Find(UnitFigures.ModelName), "the retained Far model still dresses the holder");
            Object.DestroyImmediate(piece);
            Assert.IsEmpty(live, "the roster Far lease retires when its holder is destroyed");
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            if (piece != null) Object.DestroyImmediate(piece);
            Object.DestroyImmediate(prefab);
        }
    }
}
