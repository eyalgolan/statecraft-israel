#nullable enable
using System;
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
using Object = UnityEngine.Object;

/// <summary>v0.3.2 (owner, 31 October 2026: "Yes, fallback in v0.3.2"): an enemy token whose side figure FAILED to load is
/// drawn anyway, on its plinth under its side's flag, with the plain built-in figure (the Israeli fail-open figure), and the
/// failure is logged once per side. While the figure is only loading, the token still waits; an arrived figure is drawn as
/// before.</summary>
public class UnitRosterFallbackTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static readonly HashSet<string> SideFigures = new HashSet<string>(UnitFigures.BySide.Values, StringComparer.Ordinal);

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

    static Camera TopDownCamera()
    {
        var camera = new GameObject("TEST ONLY fallback camera").AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(new Vector3(0f, 90000f, 0f), Quaternion.Euler(90f, 0f, 0f));
        return camera;
    }

    static GameObject Prefab(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    static Dictionary<string, Action<GameObject?>> PendingSource()
    {
        var pending = new Dictionary<string, Action<GameObject?>>(StringComparer.Ordinal);
        UnitFigures.Source = (id, detail, done) =>
        {
            Assert.AreEqual(CollectionDetail.Far, detail, "no Near request at the start view");
            pending[id] = done;
        };
        return pending;
    }

    /// <summary>Answers every request (and the requests the answers cause): <paramref name="answer"/> null leaves a request
    /// pending (still loading); otherwise it gives the prefab or null (a failed load).</summary>
    static void Answer(Dictionary<string, Action<GameObject?>> pending, Func<string, (bool answer, GameObject? prefab)> answer)
    {
        for (int round = 0; round < 10; round++)
        {
            var batch = pending.Where(kv => answer(kv.Key).answer).ToArray();
            if (batch.Length == 0) return;
            foreach (var (id, _) in batch) pending.Remove(id);
            foreach (var (id, done) in batch) done(answer(id).prefab);
        }
        Assert.Fail("the source kept asking");
    }

    /// <summary>Every on-map enemy token the roster gives for the fixture's date, by row id.</summary>
    static Dictionary<string, UnitToken> EnemyCandidates(List<UnitEntry> rows, GameState state) =>
        UnitRoster.FromState(rows, C, state, _ => true, (_, start, _, _) => start)
            .Where(t => t.Unit.Side != UnitData.Israel).ToDictionary(t => t.Unit.Id, StringComparer.Ordinal);

    /// <summary>Exactly one "did not load" warning per side in <paramref name="sides"/> and no other warning. LogAssert
    /// matches in order and the sides fail in answer order, so the warnings are captured and expected by one pattern.</summary>
    static void ExpectOneWarningPerSide(List<string> warned, IEnumerable<string> sides)
    {
        var pattern = new Regex(@"^UNITS figures: (\w+)'s figure \S+ did not load; its tokens are drawn with the plain built-in figure\.$");
        var got = warned.Select(w => pattern.Match(w)).Select(m => m.Success ? m.Groups[1].Value : "<other> ").ToList();
        CollectionAssert.AreEquivalent(sides, got, "one warning per failed side, and no other warning");
        foreach (var _ in got) LogAssert.Expect(LogType.Warning, pattern);
    }

    [Test]
    public void FixtureDateHoldsATokenForEverySideInBySide()
    {
        var sides = EnemyCandidates(Rows, JuneState()).Values.Select(t => t.Unit.Side).Distinct().OrderBy(s => s, StringComparer.Ordinal);
        CollectionAssert.AreEqual(UnitFigures.BySide.Keys.OrderBy(s => s, StringComparer.Ordinal), sides,
            "15 June 1948 draws every side with a figure, Egypt and Lebanon included");
    }

    [Test]
    public void FailedSideFigureDrawsEveryEnemyTokenWithTheBuiltInFigureAndWarnsOncePerSide()
    {
        var map = BuildMap();
        var pending = PendingSource();
        var camera = TopDownCamera();
        try
        {
            var state = JuneState();
            var rows = Rows;
            var expected = EnemyCandidates(rows, state);
            var layer = UnitLayer.Create(map, rows, C.Hexes, still: true);
            layer.Show(C, state);
            Assert.IsEmpty(layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel), "no enemy token before any answer");

            var warned = new List<string>();
            void Capture(string message, string _, LogType type) { if (type == LogType.Warning) warned.Add(message); }
            Application.logMessageReceived += Capture;
            Func<string, (bool, GameObject?)> failSides = id => (true, SideFigures.Contains(id) ? null : Prefab(id));
            Answer(pending, failSides);   // every side figure fails
            layer.FaceCamera(camera);   // the next render draws again
            Answer(pending, failSides);   // the drawn tokens' own holders ask again and fail again: no second warning
            Application.logMessageReceived -= Capture;
            ExpectOneWarningPerSide(warned, UnitFigures.BySide.Keys);
            layer.FlushPrefetchRetirement();
            Assert.IsFalse(layer.Show(C, state), "the failed roster is stable");
            LogAssert.NoUnexpectedReceived();

            var drawn = layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel).ToDictionary(t => t.Token.Unit.Id, StringComparer.Ordinal);
            CollectionAssert.AreEquivalent(expected.Keys, drawn.Keys, "every enemy token is drawn despite its failed figure");
            var root = layer.transform.Find("Tokens");
            foreach (var (id, want) in expected)
            {
                var got = drawn[id].Token;
                Assert.AreSame(want.Anchor, got.Anchor, $"{id}: same anchor");
                Assert.AreEqual(want.Figures, got.Figures, $"{id}: same figure count");
                Assert.AreEqual(want.ArtType, got.ArtType, $"{id}: same side figure id");
                var token = root.Find(id);
                Assert.IsNotNull(token, $"{id}: token object");
                Assert.IsNotNull(token.Find(UnitPlinth.PlinthName), $"{id}: plinth");
                var flag = token.Find(UnitPlinth.FlagName);
                Assert.IsNotNull(flag, $"{id}: flag");
                if (!UnitFlags.HasPennant(got.Unit.Side))
                    Assert.IsTrue(flag.GetComponent<MeshRenderer>().sharedMaterials.Any(m => m != null && m.name.StartsWith($"UnitFlag ({got.Unit.Side} ", StringComparison.Ordinal)),
                        $"{id}: the {got.Unit.Side} flag");
                for (int i = 0; i < got.Figures; i++)
                {
                    var figure = token.Find($"Figure{i}");
                    Assert.IsNotNull(figure, $"{id}: figure {i}");
                    if (i == 2 && UnitFigures.ThirdBySide.ContainsKey(got.Unit.Side))
                    {
                        Assert.IsTrue(UnitFigures.Dressed(figure), $"{id}: its loaded third figure is still dressed");
                        continue;
                    }
                    Assert.IsTrue(figure.TryGetComponent<MeshRenderer>(out var mr) && mr.enabled, $"{id}: figure {i} is the built-in figure");
                    Assert.IsNotNull(figure.GetComponent<MeshFilter>().sharedMesh, $"{id}: figure {i} has its mesh");
                }
                Assert.IsNull(token.Find($"Figure{got.Figures}"), $"{id}: no extra figure");
            }
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void LoadingSideFigureStillDrawsNoEnemyToken()
    {
        var map = BuildMap();
        var pending = PendingSource();
        var camera = TopDownCamera();
        try
        {
            var state = JuneState();
            var layer = UnitLayer.Create(map, Rows, C.Hexes, still: true);
            layer.Show(C, state);
            Answer(pending, id => (!SideFigures.Contains(id), SideFigures.Contains(id) ? null : Prefab(id)));   // side figures stay loading
            layer.FaceCamera(camera);
            layer.Show(C, state);
            Assert.IsNotEmpty(layer.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel), "fixture draws Israel");
            Assert.IsEmpty(layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel), "a loading figure draws no placeholder");
            Assert.IsTrue(SideFigures.All(pending.ContainsKey), "every side figure is still asked for");
            foreach (var id in SideFigures) Assert.IsFalse(UnitFigures.Failed(id), $"{id}: loading is not failed");
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void ArrivedSideFigureDrawsTheRealFigure()
    {
        var map = BuildMap();
        var pending = PendingSource();
        var camera = TopDownCamera();
        try
        {
            var state = JuneState();
            var rows = Rows;
            var expected = EnemyCandidates(rows, state);
            var layer = UnitLayer.Create(map, rows, C.Hexes, still: true);
            layer.Show(C, state);
            Answer(pending, id => (true, Prefab(id)));
            layer.FaceCamera(camera);
            var root = layer.transform.Find("Tokens");
            CollectionAssert.AreEquivalent(expected.Keys,
                layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel).Select(t => t.Token.Unit.Id), "every enemy token is drawn");
            foreach (var (id, want) in expected)
                for (int i = 0; i < want.Figures; i++)
                {
                    var figure = root.Find(id).Find($"Figure{i}");
                    Assert.IsFalse(figure.TryGetComponent<MeshRenderer>(out _), $"{id}: figure {i} has no built-in mesh");
                    Assert.IsTrue(UnitFigures.Dressed(figure), $"{id}: figure {i} shows its Collection figure");
                }
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    /// <summary>The bug players hit (v0.3.1 on CrazyGames: no enemy unit at all): a new game at the opening state, 15 May
    /// 1948 with the real content, draws exactly one token per enemy starting force, per side, whether the side figures
    /// arrived (the real figure) or failed to load (the built-in fallback).</summary>
    static readonly IReadOnlyDictionary<string, int> OpeningEnemyTokens = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["egypt"] = 1, ["jordan"] = 1, ["syria"] = 1, ["iraq"] = 1, ["ala"] = 1,
    };

    static void NewGameDrawsEveryEnemyStartingForce(bool figuresLoad)
    {
        var map = BuildMap();
        var pending = PendingSource();
        var camera = TopDownCamera();
        try
        {
            var state = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
            Assert.AreEqual(new DateTime(1948, 5, 15), state.Date, "the opening state");
            var fromSim = C.StartingForces.Where(f => f.Arrives == null && f.Side != UnitData.Israel)
                .GroupBy(f => f.Side).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            CollectionAssert.AreEquivalent(OpeningEnemyTokens, fromSim, "the sim's enemy starting forces (StartingForcesTests)");
            Assert.IsFalse(fromSim.ContainsKey("lebanon"), "Lebanon's map-only token starts on 6 June");

            var layer = UnitLayer.Create(map, Rows, C.Hexes, still: true);
            layer.Show(C, state);
            var warned = new List<string>();
            void Capture(string message, string _, LogType type) { if (type == LogType.Warning) warned.Add(message); }
            Application.logMessageReceived += Capture;
            Func<string, (bool, GameObject?)> answer = id => (true, !figuresLoad && SideFigures.Contains(id) ? null : Prefab(id));
            Answer(pending, answer);
            layer.FaceCamera(camera);
            Answer(pending, answer);
            Application.logMessageReceived -= Capture;
            ExpectOneWarningPerSide(warned, figuresLoad ? Array.Empty<string>() : OpeningEnemyTokens.Keys);
            layer.FlushPrefetchRetirement();

            var enemy = layer.Tokens.Where(t => t.Token.Unit.Side != UnitData.Israel).Select(t => t.Token).ToList();
            var perSide = enemy.GroupBy(t => t.Unit.Side).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            CollectionAssert.AreEquivalent(OpeningEnemyTokens, perSide, $"one token per enemy starting force ({(figuresLoad ? "arrived" : "failed")} figures)");
            var root = layer.transform.Find("Tokens");
            foreach (var t in enemy)
            {
                Assert.AreEqual(3, t.Figures, $"{t.Unit.Id}: full strength");
                for (int i = 0; i < t.Figures; i++)
                {
                    var figure = root.Find(t.Unit.Id).Find($"Figure{i}");
                    Assert.IsNotNull(figure, $"{t.Unit.Id}: figure {i}");
                    bool builtIn = figure.TryGetComponent<MeshRenderer>(out var mr) && mr.enabled;
                    bool third = i == 2 && UnitFigures.ThirdBySide.ContainsKey(t.Unit.Side);
                    if (figuresLoad || third) Assert.IsTrue(UnitFigures.Dressed(figure), $"{t.Unit.Id}: figure {i} is its Collection figure");
                    else Assert.IsTrue(builtIn, $"{t.Unit.Id}: figure {i} is the built-in fallback");
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(map.gameObject);
        }
    }

    [Test]
    public void ANewGameDrawsEveryEnemyStartingForceWithItsArrivedFigure() => NewGameDrawsEveryEnemyStartingForce(true);

    [Test]
    public void ANewGameDrawsEveryEnemyStartingForceWithTheFallbackWhenFiguresFail() => NewGameDrawsEveryEnemyStartingForce(false);

    [Test]
    public void FailedIsSetByANullFarAnswerAndClearedByArrivalOrForget()
    {
        string id = UnitFigures.EgyptianInfantry;
        LogAssert.Expect(LogType.Warning, new Regex("^UNITS figures: egypt's figure .* did not load"));
        LogAssert.Expect(LogType.Warning, new Regex("^UNITS figures: lebanon's figure"));   // in this order
        UnitFigures.AnswerOwned(id, CollectionDetail.Far, null);
        Assert.IsTrue(UnitFigures.Failed(id), "a null Far answer marks the figure failed");
        UnitFigures.AnswerOwned(id, CollectionDetail.Far, null);   // no second warning
        UnitFigures.AnswerOwned(id, CollectionDetail.Near, null);
        Assert.IsTrue(UnitFigures.Failed(id), "a Near answer leaves it");
        UnitFigures.AnswerOwned(id, CollectionDetail.Far, Prefab(id));
        Assert.IsFalse(UnitFigures.Failed(id), "a later arrival clears the failure");
        Assert.IsTrue(UnitFigures.Arrived(id));
        UnitFigures.Forget();
        UnitFigures.AnswerOwned(UnitFigures.LebaneseRegular, CollectionDetail.Far, null);
        UnitFigures.Forget();
        Assert.IsFalse(UnitFigures.Failed(UnitFigures.LebaneseRegular), "Forget clears failures");
        LogAssert.NoUnexpectedReceived();
    }
}
