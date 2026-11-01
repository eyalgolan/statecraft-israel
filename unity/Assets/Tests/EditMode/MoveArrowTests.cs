#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

/// <summary>#2134 (owner, 1 Nov 2026: "an arrow showing which tiles the unit is going to move to. this needs to be for
/// israeli units only"): in an Israeli unit's Move pick, the hovered hex gets an animated arrow from the unit's hex through
/// every hex of the sim's path (Movement.Path for a lit hex, Movement.Route for a go-to); never for an enemy unit; cleared
/// when the move is made or the pick ends; the per-frame hover allocates nothing and the animation is the shader's.</summary>
public class MoveArrowTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
        return map;
    }

    sealed class Rig
    {
        public SimBridge Bridge = null!; public UnitLayer Layer = null!; public InfoPanelPresenter Info = null!;
        public readonly List<IReadOnlyList<(int q, int r)>> Arrows = new List<IReadOnlyList<(int q, int r)>>();
        public PlacedToken Golani; public int Id;
        public (int q, int r) Start => (Bridge.State.Units[Id].Q, Bridge.State.Units[Id].R);
    }

    static Rig Open(MapScene map, UiTestKit ui)
    {
        var rig = new Rig { Bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text) };
        rig.Layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
        rig.Layer.Show(rig.Bridge.Content, rig.Bridge.State);
        var layer = rig.Layer;
        rig.Info = new InfoPanelPresenter(ui.Root, rig.Bridge, Text, new Dictionary<string, string>(),
            selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
            showReachable: map.ShowReachable, showRoute: map.ShowRoute,
            showArrow: hexes => { rig.Arrows.Add(hexes.ToList()); map.ShowMoveArrow(hexes); });
        rig.Golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
        rig.Id = UnitCard.SimUnit(C, rig.Bridge.State, rig.Golani.Token.Unit)!.Value;
        return rig;
    }

    /// <summary>A lit hex two or more hexes away (Movement.Path enters at least two hexes), the first in Reachable's order.</summary>
    static (int q, int r) FarLit(Rig rig) => Movement.Reachable(C, rig.Bridge.State, rig.Id, rig.Bridge.Spent).Keys
        .Where(h => Movement.Path(C, rig.Bridge.State, rig.Id, h.Q, h.R, rig.Bridge.Spent).Count >= 2).Select(h => (h.Q, h.R)).First();

    static MeshRenderer ArrowRenderer(MapScene map) => map.Terrain.transform.Find(MapScene.MoveArrowName).GetComponent<MeshRenderer>();

    static Vector2 Centre(MapScene map, int q, int r)
    {
        var hex = map.Hexes.hexes.Single(h => h.q == q && h.r == r);
        var w = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        return new Vector2(w.x, w.z);
    }

    /// <summary>(a) Hovering a lit hex draws the arrow from the unit's hex through Movement.Path's hexes, in order; the mesh's
    /// centre line passes through each of their centres. An unlit hex a go-to reaches gets Movement.Route's hexes.</summary>
    [Test]
    public void TheArrowFollowsTheSimsPathHexByHex()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var rig = Open(map, ui);
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, rig.Golani.Q, rig.Golani.R);
            Assert.IsTrue(rig.Info.MovePicking, "precondition: selecting Golani opens its Move pick");
            var lit = FarLit(rig);
            var path = Movement.Path(C, rig.Bridge.State, rig.Id, lit.q, lit.r, rig.Bridge.Spent).Select(h => (h.Q, h.R)).ToList();
            rig.Info.ShowRouteTo(lit.q, lit.r);
            var expected = new[] { rig.Start }.Concat(path).ToList();
            CollectionAssert.AreEqual(expected, rig.Arrows.Last(), "the unit's hex, then the sim's path to the hovered hex");
            CollectionAssert.AreEqual(expected, map.ShownArrow);
            var renderer = ArrowRenderer(map);
            Assert.IsTrue(renderer.enabled);
            var go = renderer.transform;
            var line = renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => { var w = go.TransformPoint(v); return new Vector2(w.x, w.z); }).ToList();
            foreach (var (q, r) in path)
            {
                var c = Centre(map, q, r);
                Assert.Less(line.Min(p => (p - c).magnitude), 1f, $"the arrow passes through hex {q},{r}'s centre");
            }

            var reach = Movement.Reachable(C, rig.Bridge.State, rig.Id, rig.Bridge.Spent).Keys.ToHashSet();
            var far = C.Hexes.Where(h => h.Land && !reach.Contains((h.Q, h.R)) && (h.Q, h.R) != rig.Start).Select(h => (h.Q, h.R))
                .First(h => Movement.Route(C, rig.Bridge.State, rig.Id, h.Q, h.R, rig.Bridge.Spent).LastOrDefault().Turn == 2);
            rig.Info.ShowRouteTo(far.Item1, far.Item2);
            var now = rig.Bridge.Session.Now;
            var route = Movement.Route(C, now.State, rig.Id, far.Item1, far.Item2, now.Spent).Select(h => (h.Q, h.R)).ToList();
            CollectionAssert.AreEqual(new[] { rig.Start }.Concat(route).ToList(), rig.Arrows.Last(), "a go-to's arrow follows the sim's route");
            Assert.IsTrue(renderer.enabled);
            rig.Info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>(b) An enemy unit never gets an arrow: its card opens no pick, so no hovered hex draws one; and the arrow's
    /// own rule refuses an enemy unit even with a path in hand.</summary>
    [Test]
    public void AnEnemyUnitGetsNoArrow()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var rig = Open(map, ui);
            var enemy = rig.Layer.Tokens.First(p => p.Token.Unit.Side != UnitData.Israel && UnitCard.SimUnit(C, rig.Bridge.State, p.Token.Unit) != null);
            var enemyId = UnitCard.SimUnit(C, rig.Bridge.State, enemy.Token.Unit)!.Value;
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, enemy.Q, enemy.R);
            Assert.AreEqual(enemy.Token.Unit.Id, rig.Layer.Selected, "precondition: the enemy unit is selected");
            var e = rig.Bridge.State.Units[enemyId];
            foreach (var h in C.Hexes.Where(h => h.Land && System.Math.Abs(h.Q - e.Q) <= 2 && System.Math.Abs(h.R - e.R) <= 2))
                rig.Info.ShowRouteTo(h.Q, h.R);
            Assert.IsTrue(rig.Arrows.All(a => a.Count == 0), "no hover over the enemy's neighbourhood draws an arrow");
            Assert.IsEmpty(map.ShownArrow);

            var golaniPath = new[] { (rig.Start.q + 1, rig.Start.r) };
            Assert.IsEmpty(InfoPanelPresenter.ArrowHexes(e, golaniPath), "the arrow's own rule: never an enemy unit's");
            CollectionAssert.AreEqual(new[] { rig.Start, golaniPath[0] }, InfoPanelPresenter.ArrowHexes(rig.Bridge.State.Units[rig.Id], golaniPath));
            rig.Info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>(c) The arrow clears when the move is made, when the pick is cancelled (Escape), when the mouse leaves the map,
    /// and when another hex is selected.</summary>
    [Test]
    public void TheArrowClearsOnMoveAndOnCancel()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var rig = Open(map, ui);
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, rig.Golani.Q, rig.Golani.R);
            var lit = FarLit(rig);
            rig.Info.ShowRouteTo(lit.q, lit.r);
            Assert.IsNotEmpty(rig.Arrows.Last(), "precondition: the hover draws the arrow");

            rig.Info.HideRoute();
            Assert.IsEmpty(rig.Arrows.Last(), "the mouse left the map");
            Assert.IsFalse(ArrowRenderer(map).enabled);

            rig.Info.ShowRouteTo(lit.q, lit.r);
            Assert.IsNotEmpty(rig.Arrows.Last(), "back on the hex");
            Assert.IsTrue(rig.Info.Escape());
            Assert.IsEmpty(rig.Arrows.Last(), "Escape cancels the pick");
            Assert.IsFalse(ArrowRenderer(map).enabled);

            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, rig.Golani.Q, rig.Golani.R);
            rig.Info.ShowRouteTo(lit.q, lit.r);
            Assert.IsNotEmpty(rig.Arrows.Last(), "precondition: a new pick, the arrow again");
            var water = C.Hexes.First(h => !h.Land);
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, water.Q, water.R);
            Assert.IsEmpty(rig.Arrows.Last(), "selecting another hex ends the pick");

            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, rig.Golani.Q, rig.Golani.R);
            rig.Info.ShowRouteTo(lit.q, lit.r);
            Assert.IsNotEmpty(rig.Arrows.Last(), "precondition: the arrow before the move");
            map.Select(lit.q, lit.r);
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, lit.q, lit.r);
            Assert.IsNotEmpty(rig.Bridge.Session.Committed.OfType<Move>(), "precondition: the move is made");
            Assert.IsEmpty(rig.Arrows.Last(), "the move is made");
            Assert.IsEmpty(map.ShownArrow);
            Assert.IsFalse(ArrowRenderer(map).enabled);
            rig.Info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>(d) GameRoot.LateUpdate asks for the hovered hex's route and arrow every frame: once drawn, asking again for
    /// the same hex allocates nothing. The arrow has no script of its own: its chevrons move in the shader.</summary>
    [Test]
    public void TheHoverEveryFrameAllocatesNothing()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var rig = Open(map, ui);
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, rig.Golani.Q, rig.Golani.R);
            var lit = FarLit(rig);
            rig.Info.ShowRouteTo(lit.q, lit.r);
            Assert.IsNotEmpty(rig.Arrows.Last(), "precondition: the arrow is drawn");
            var calls = rig.Arrows.Count;
            Assert.That(() => rig.Info.ShowRouteTo(lit.q, lit.r), Is.Not.AllocatingGCMemory());
            Assert.AreEqual(calls, rig.Arrows.Count, "the same hex draws nothing again");
            Assert.IsEmpty(ArrowRenderer(map).GetComponents<MonoBehaviour>(), "no per-frame script on the arrow");
            rig.Info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The arrow's material comes from Resources with its shader (a player keeps it), the shader compiles for WebGL 2,
    /// and its width follows the camera distance so it stays about the same size on screen at every zoom the camera allows.</summary>
    [Test]
    public void TheArrowShaderShipsAndKeepsItsScreenWidthAtEveryZoom()
    {
        var mat = Resources.Load<Material>(MapScene.MoveArrowMaterialResource);
        Assert.IsNotNull(mat, $"Resources/{MapScene.MoveArrowMaterialResource}.mat");
        Assert.AreEqual("Statecraft/MoveArrow", mat.shader.name);
        Assert.IsTrue(mat.shader.isSupported);
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(mat.shader));
        Assert.AreEqual(MapScene.MoveArrowWidthPerDistance, mat.GetFloat("_WidthPerDistance"), 1e-6f, "the material and the code agree");
        // The shader widens the arrow in proportion to the camera's distance, so its on-screen width is the same from
        // CameraController.MinDistance to MaxDistance: at 1080 px, its width over the visible height at that distance
        // (CameraRig.Fov), the distance cancelling out.
        float px = MapScene.MoveArrowWidthPerDistance / (2f * Mathf.Tan(CameraRig.Fov * Mathf.Deg2Rad / 2f)) * 1080f;
        Assert.That(px, NUnit.Framework.Is.InRange(20f, 40f), "the arrowhead's width on screen");
    }
}
