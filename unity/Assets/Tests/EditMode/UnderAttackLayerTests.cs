#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.HexTiles;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Statecraft.Game.Map.Units;

/// <summary>#1878: the "under attack" look (HexUnderAttack: smoke, fire glow, pulsing border) on the default diorama map.
/// The hex the sim's engagement names shows it while the turn report is open and it clears when the report closes. It reads
/// the turn's Why lines and nothing else: no rule, no state, no save changes.</summary>
public class UnderAttackLayerTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static Cause Engagement(int q, int r) => new Cause("map", FormattableString.Invariant($"{q},{r}"), 0, $"The attack on hex {q},{r}", key: WhyKeys.Engagement);
    static StepResult Turn(params Cause[] why) => new StepResult(GameSession.New(C, GoldenRun.Seed, Mode.Historical).State, why, new Rejection[0]);

    MapScene? map;

    [TearDown] public void DestroyMap() { if (map != null) Object.DestroyImmediate(map.gameObject); map = null; }

    MapScene Map()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
        return map;
    }

    /// <summary>A land hex of the real map, so the look has ground under it.</summary>
    static (int q, int r) LandHex(MapScene m, int skip = 0)
    {
        var h = m.Hexes.hexes.Where(x => x.land).Skip(skip).First();
        return (h.q, h.r);
    }

    [Test]
    public void OnlyEngagementsNameAnAttackedHex()
    {
        var turn = Turn(Engagement(3, 4), new Cause("map", "9,9", 0, "Unit 7 moved", key: WhyKeys.UnitMoved), Engagement(3, 4), Engagement(5, 1));
        CollectionAssert.AreEqual(new[] { (3, 4), (5, 1) }, UnderAttackLayer.AttackedHexes(turn), "each attacked hex once, in the turn's order");
        Assert.IsEmpty(UnderAttackLayer.AttackedHexes(null));
        Assert.IsEmpty(UnderAttackLayer.AttackedHexes(Turn()));
    }

    [Test]
    public void ShowPutsTheLookOnTheAttackedHexOfTheMapAndClearRemovesIt()
    {
        var m = Map();
        var (q, r) = LandHex(m);
        var layer = new UnderAttackLayer(m);
        layer.Show(Turn(Engagement(q, r)));
        var root = m.Terrain.transform.Find(UnderAttackLayer.RootName);
        Assert.IsNotNull(root);
        var look = root!.Find(HexUnderAttack.Name(q, r));
        Assert.IsNotNull(look, "the attacked hex shows the look");
        CollectionAssert.AreEqual(new[] { (q, r) }, layer.Shown);
        // On the diorama the hex centre is Frame.ToWorld of the hex's lon/lat, on the ground there.
        var hex = m.Hexes.hexes.First(h => h.q == q && h.r == r);
        var want = m.Frame.ToWorld(hex.lon, hex.lat, 0);
        Assert.That(Vector2.Distance(new Vector2(look!.position.x, look.position.z), new Vector2(want.x, want.z)), Is.LessThan(5f), "the look is on the hex's centre");
        Assert.That(look.position.y, Is.EqualTo(m.Terrain.SampleHeight(new Vector3(look.position.x, 0f, look.position.z)) + m.Terrain.transform.position.y).Within(0.5f), "on the ground");
        Assert.That(look.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThanOrEqualTo(3), "smoke, glow and border");
        layer.Clear();
        Assert.IsNull(root.Find(HexUnderAttack.Name(q, r)));
        Assert.IsEmpty(layer.Shown);
    }

    [Test]
    public void ANewTurnReplacesTheHexesAndAHexOffTheMapShowsNothing()
    {
        var m = Map();
        var a = LandHex(m); var b = LandHex(m, 40);
        var layer = new UnderAttackLayer(m);
        layer.Show(Turn(Engagement(a.q, a.r)));
        layer.Show(Turn(Engagement(b.q, b.r), Engagement(-500, -500)));
        CollectionAssert.AreEqual(new[] { b }, layer.Shown, "only the new hex, and none for a hex the map does not have");
        Assert.IsNull(m.Terrain.transform.Find(UnderAttackLayer.RootName)!.Find(HexUnderAttack.Name(a.q, a.r)));
        layer.Show(null);
        Assert.IsEmpty(layer.Shown);
    }

    /// <summary>GameRoot's teardown can run once the terrain (and the layer's root under it) is destroyed: neither call throws.</summary>
    [Test]
    public void ClearAndShowAreSafeAfterTheTerrainIsGone()
    {
        var m = Map();
        var (q, r) = LandHex(m);
        var layer = new UnderAttackLayer(m);
        layer.Show(Turn(Engagement(q, r)));
        Object.DestroyImmediate(m.Terrain.gameObject);
        Assert.DoesNotThrow(() => layer.Clear());
        Assert.DoesNotThrow(() => layer.Show(Turn(Engagement(q, r))));
        Assert.IsEmpty(layer.Shown);
    }

    [Test]
    public void TheReportSaysWhenItOpensAndCloses()
    {
        using var ui = new UiTestKit();
        var text = En;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        using var nav = new ScreenNav(ui.Root);
        using var hud = new HudPresenter(ui.Root, bridge, text);
        using var report = new TurnReportPresenter(ui.Root, bridge, text, nav);
        var seen = new List<StepResult?>();
        report.Showing += seen.Add;
        var turn = bridge.EndTurn();
        Assert.IsTrue(report.IsOpen);
        Assert.AreEqual(1, seen.Count);
        Assert.AreSame(bridge.Session.Last, seen[0], "it names the turn it reports");
        UiTestKit.Press(ui.Q<Button>("turn-report-close"));
        Assert.AreEqual(2, seen.Count);
        Assert.IsNull(seen[1], "closing says null");
        report.Close();
        Assert.AreEqual(2, seen.Count, "closing it again says nothing");
    }

    /// <summary>The real game scene: an enemy attack that engages on End turn lights the attacked hex while the report is up,
    /// and closing the report puts it out. The setup is TurnReportTests' (the Arab Liberation Army attacks Golani's hex).</summary>
    [UnityTest]
    public IEnumerator InTheGameTheAttackedHexShowsTheLookUntilTheReportCloses()
    {
        VisualElement ui = null!; GameRoot root = null!; RenderTexture? rt = null; PanelSettings? ps = null;
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-under-attack-" + Guid.NewGuid().ToString("N"));
        try
        {
            yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
            var bridge = root.Bridge!;
            var gameMap = Object.FindFirstObjectByType<MapScene>();
            var layerRoot = gameMap.Terrain.transform.Find(UnderAttackLayer.RootName);
            Assert.IsNotNull(layerRoot, "the game builds the layer");
            var forces = C.StartingForces.ToList();
            int golani = forces.FindIndex(f => f.Row == "golani"), ala = forces.FindIndex(f => f.Row == "arab_liberation_army");
            var s = bridge.Session.State; var g = s.Units[golani]; var a = s.Units[ala];
            s.Units[golani] = new Unit(g.Id, g.Type, a.Q, a.R - 1, g.Service, g.Side, 1);
            s.Units[ala] = new Unit(a.Id, a.Type, a.Q, a.R, a.Service, a.Side, a.Strength, Combat.Attack, $"{a.Q},{a.R - 1}");
            foreach (var id in bridge.NeedsOrders.ToList()) bridge.Skip(id);
            UiTestKit.Press(ui.Q<Button>("end-turn"));
            yield return null;
            Assert.IsTrue(ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass), "precondition: the report is up");
            Assert.IsNotNull(layerRoot!.Find(HexUnderAttack.Name(a.Q, a.R - 1)), "the attacked hex shows the look");
            Assert.AreEqual(1, layerRoot.childCount);
            UiTestKit.Press(ui.Q<Button>("turn-report-close"));
            yield return null;
            Assert.AreEqual(0, layerRoot.childCount, "closing the report clears it");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ps != null) Object.DestroyImmediate(ps);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
