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
        // The settlements' names, as UnitOrdersTests gives them: a hex the tests click may be a city's, whose card names it.
        var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
        rig.Info = new InfoPanelPresenter(ui.Root, rig.Bridge, Text, towns,
            selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
            showReachable: map.ShowReachable, showRoute: map.ShowRoute,
            showArrow: hexes => { rig.Arrows.Add(hexes.ToList()); map.ShowMoveArrow(hexes); },
            clearSelection: () => { layer.Select(null); map.ClearSelection(); });   // as GameRoot: Escape drops the unit too
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
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        // Every row's token, as TheCardOffersMoveOnlyWhereTheSimHasAHex (UnitOrdersTests) picks its enemy force.
        var tokens = UnitRoster.FromState(Units, C, g.State, _ => true, (row, a, q, r) => a);
        PlacedToken? shown = null;
        var arrows = new List<IReadOnlyList<(int q, int r)>>();
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown,
            showArrow: hexes => arrows.Add(hexes.ToList()));
        var golani = tokens.Single(t => t.Unit.Id == "golani");
        var golaniId = UnitCard.SimUnit(C, g.State, golani.Unit)!.Value;
        shown = new PlacedToken(golani, new AnchorSpot(0, 0, 0, 0, false));
        info.ShowUnit();
        var lit = Movement.Reachable(C, g.State, golaniId).Keys.First();
        info.ShowRouteTo(lit.Q, lit.R);
        Assert.IsNotEmpty(arrows.Last(), "precondition: Golani's hover draws the arrow");

        var enemy = tokens.First(t => t.Unit.Side != UnitData.Israel && UnitCard.SimUnit(C, g.State, t.Unit) != null);
        var e = g.State.Units[UnitCard.SimUnit(C, g.State, enemy.Unit)!.Value];
        shown = new PlacedToken(enemy, new AnchorSpot(0, 0, 0, 0, false));
        info.ShowUnit();
        Assert.IsFalse(info.MovePicking, "precondition: an enemy unit's card opens no Move pick");
        Assert.IsEmpty(arrows.Last(), "selecting the enemy unit cleared Golani's arrow");
        var from = arrows.Count;
        foreach (var h in C.Hexes.Where(h => h.Land && System.Math.Abs(h.Q - e.Q) <= 3 && System.Math.Abs(h.R - e.R) <= 3))
            info.ShowRouteTo(h.Q, h.R);
        Assert.IsTrue(arrows.Skip(from).All(a => a.Count == 0), "no hover near the enemy unit draws an arrow");

        var path = new[] { (e.Q + 1, e.R) };
        Assert.IsEmpty(InfoPanelPresenter.ArrowHexes(e, path), "the arrow's own rule: never an enemy unit's, even with a path");
        var own = g.State.Units[golaniId];
        CollectionAssert.AreEqual(new[] { (own.Q, own.R), (own.Q + 1, own.R) }, InfoPanelPresenter.ArrowHexes(own, new[] { (own.Q + 1, own.R) }));
        info.Dispose();
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

    /// <summary>The map hexes whose centres are a step from (q, r): neighbours by distance (a step is sqrt(3) hex sizes),
    /// so the tests need no assumption about the coordinates' axes.</summary>
    static List<(int q, int r)> Neighbours(MapScene map, int q, int r)
    {
        float step = Mathf.Sqrt(3f) * map.Hexes.hex_size_km * 1000f;
        var c = Centre(map, q, r);
        return map.Hexes.hexes.Where(h => (h.q, h.r) != (q, r) && Mathf.Abs((Centre(map, h.q, h.r) - c).magnitude - step) < 0.05f * step)
            .Select(h => (h.q, h.r)).ToList();
    }

    static float Turn(MapScene map, (int q, int r) a, (int q, int r) b, (int q, int r) c) =>
        Vector2.Angle(Centre(map, b.q, b.r) - Centre(map, a.q, a.r), Centre(map, c.q, c.r) - Centre(map, b.q, b.r));

    /// <summary>A path with a 60-degree bend then a 120-degree bend, from the map's own hexes.</summary>
    static List<(int q, int r)> BendyPath(MapScene map)
    {
        foreach (var h in map.Hexes.hexes)
        {
            var b = (h.q, h.r); var around = Neighbours(map, b.q, b.r);
            if (around.Count < 6) continue;
            foreach (var a in around)
                foreach (var c in around)
                {
                    if (Mathf.Abs(Turn(map, a, b, c) - 60f) > 1f) continue;
                    var next = Neighbours(map, c.q, c.r).FirstOrDefault(d => Mathf.Abs(Turn(map, b, c, d) - 120f) < 1f && d != b);
                    if (next != default) return new List<(int q, int r)> { a, b, c, next };
                }
        }
        throw new AssertionException("no hex on the map has a 60 then 120 degree bend");
    }

    /// <summary>Review of #2134 (C1): the shader widens the band with the camera's distance, so at a bend the cross-sections
    /// next to the mitre must lie beyond where the widest band's inner edge reaches, or the band folds back over itself and
    /// blends twice. At the farthest zoom with a margin (the half-width the shader gives a vertex 1.5 times
    /// CameraController.MaxDistance away), each side's edge moves forward from every cross-section to the next.</summary>
    [Test]
    public void TheBandNeverFoldsAtABend()
    {
        var map = BuildMap();
        try
        {
            var path = BendyPath(map);
            map.ShowMoveArrow(path);
            var mesh = ArrowRenderer(map).GetComponent<MeshFilter>().sharedMesh;
            var v = mesh.vertices; var side = new List<Vector4>(); mesh.GetUVs(1, side);
            float half = 0.5f * MapScene.MoveArrowWidthPerDistance * CameraController.MaxDistance * 1.5f;
            for (int i = 0; i + 3 < v.Length; i += 2)
            {
                Vector2 p0 = new Vector2(v[i].x, v[i].z), p1 = new Vector2(v[i + 2].x, v[i + 2].z);
                Vector2 s0 = new Vector2(side[i].x, side[i].y), s1 = new Vector2(side[i + 2].x, side[i + 2].y);
                var along = p1 - p0;
                foreach (var sign in new[] { -1f, 1f })
                    Assert.Greater(Vector2.Dot((p1 + sign * half * s1) - (p0 + sign * half * s0), along), 0f,
                        $"section {i / 2} to {i / 2 + 1}, side {sign}: the band folds back");
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review of #2134 (C4): the mesh holds the centre line and the shader widens it, so the bounds must hold the
    /// widest band (a mitre doubles the half-width at most; a vertex up to 1.5 times CameraController.MaxDistance away), or
    /// a bend just off screen is culled while its band is on screen.</summary>
    [Test]
    public void TheBoundsHoldTheWidestBand()
    {
        var map = BuildMap();
        try
        {
            map.ShowMoveArrow(BendyPath(map));
            var mesh = ArrowRenderer(map).GetComponent<MeshFilter>().sharedMesh;
            var v = mesh.vertices; var side = new List<Vector4>(); mesh.GetUVs(1, side);
            float half = 0.5f * MapScene.MoveArrowWidthPerDistance * CameraController.MaxDistance * 1.5f;
            var b = mesh.bounds;
            for (int i = 0; i < v.Length; i++)
                foreach (var sign in new[] { -1f, 1f })
                {
                    var p = v[i] + sign * half * new Vector3(side[i].x, 0f, side[i].y);
                    Assert.IsTrue(b.Contains(new Vector3(p.x, b.center.y, p.z)), $"vertex {i}, side {sign}: {p} is outside {b}");
                }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review of #2134 (C3): with no pick open, moving over the map never touches the arrow (no rebuild, no upload,
    /// no call), however many hexes the pointer crosses.</summary>
    [Test]
    public void HoveringWithNoPickLeavesTheArrowAlone()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var rig = Open(map, ui);
            Assert.IsFalse(rig.Info.MovePicking, "precondition: nothing selected");
            foreach (var h in C.Hexes.Where(h => h.Land).Take(40)) rig.Info.ShowRouteTo(h.Q, h.R);
            Assert.IsEmpty(rig.Arrows, "no pick: the arrow is never asked to draw or clear");
            rig.Info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review of #2134 (C2): a new state (here another unit's order, the pick staying open) hides the hovered
    /// arrow, read on the old state, and the next hover draws it again from the new one.</summary>
    [Test]
    public void ANewStateHidesTheArrowAndTheNextHoverRedrawsIt()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var rig = Open(map, ui);
            GameRoot.OnHexClicked(map, rig.Layer, rig.Info, rig.Golani.Q, rig.Golani.R);
            var lit = FarLit(rig);
            rig.Info.ShowRouteTo(lit.q, lit.r);
            var drawn = rig.Arrows.Last().ToList();
            Assert.IsNotEmpty(drawn, "precondition: the arrow is drawn");
            var (other, order) = rig.Bridge.State.Units.Values.Where(u => u.Id != rig.Id && u.Side == Movement.PlayerSide)
                .SelectMany(u => new[] { Orders.Hold, Orders.Fortify }.Select(o => (u.Id, o)))
                .First(x => Orders.Check(C, rig.Bridge.State, x.Id, x.o) == null);
            Assert.IsNull(rig.Bridge.Queue(new UnitOrder(other, order)), "precondition: another unit's order is taken");
            Assert.IsTrue(rig.Info.MovePicking, "precondition: Golani's pick is still open");
            Assert.IsEmpty(rig.Arrows.Last(), "the state changed: the arrow read on the old one is hidden");
            Assert.IsFalse(ArrowRenderer(map).enabled);
            rig.Info.ShowRouteTo(lit.q, lit.r);
            var path = Movement.Path(C, rig.Bridge.State, rig.Id, lit.q, lit.r, rig.Bridge.Spent).Select(h => (h.Q, h.R));
            CollectionAssert.AreEqual(new[] { rig.Start }.Concat(path).ToList(), rig.Arrows.Last(), "the hover draws it from the new state");
            rig.Info.Dispose();
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review of #2134 (C2): the arrow's width on screen, measured in a render (not reckoned from the constants):
    /// a straight path across the screen, its shaft's thickness at the middle hex, the same at the nearest and the farthest
    /// zoom the camera allows, and clearly visible at both. The editor renders with its own graphics API, not WebGL;
    /// ShaderChecks.Gles3Errors covers the WebGL 2 compile.</summary>
    [Test]
    public void TheArrowKeepsItsScreenWidthFromNearestToFarthestZoom()
    {
        var map = BuildMap();
        RenderSample.TiltShift(false);
        try
        {
            // Three hexes in a line, the middle one with every neighbour on the map.
            List<(int q, int r)>? line = null;
            foreach (var h in map.Hexes.hexes)
            {
                var around = Neighbours(map, h.q, h.r);
                if (around.Count < 6) continue;
                var a = around[0];
                var c = around.First(n => Mathf.Abs(Turn(map, a, (h.q, h.r), n)) < 1f);
                line = new List<(int q, int r)> { a, (h.q, h.r), c };
                break;
            }
            Assert.IsNotNull(line);
            var mid = Centre(map, line![1].q, line[1].r);
            var dir = (Centre(map, line[2].q, line[2].r) - Centre(map, line[0].q, line[0].r)).normalized;
            float ground = map.Terrain.terrainData.GetInterpolatedHeight(Mathf.Clamp01(mid.x / map.Frame.WidthM), Mathf.Clamp01(mid.y / map.Frame.LengthM));
            var look = map.Terrain.transform.TransformPoint(new Vector3(mid.x, ground + MapScene.SelectionClearanceM, mid.y));
            var cam = Camera.main; var camRig = cam.GetComponent<CameraRig>();
            float Thickness(float distance)
            {
                // Looking square across the path, so it runs left to right on screen.
                camRig.LookAt = look; camRig.Distance = distance; camRig.TopInset = 0;
                camRig.Yaw = Mathf.Atan2(dir.y, -dir.x) * Mathf.Rad2Deg; camRig.Apply();
                map.ShowMoveArrow(System.Array.Empty<(int, int)>());
                var without = RenderSample.RenderSteady(cam, 1920, 1080, 3);
                map.ShowMoveArrow(line);
                var at = new Vector2[1];
                RenderSample.Render(cam, 1920, 1080, new[] { look }, at);
                var with = RenderSample.RenderSteady(cam, 1920, 1080, 3);
                bool Changed(int x, int y) { var a = with.At(x, y); var b = without.At(x, y); return Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)) > 0.08f; }
                int cx = Mathf.RoundToInt(at[0].x), cy = Mathf.RoundToInt(at[0].y);
                var runs = new List<int>();
                for (int x = cx - 8; x <= cx + 8; x++)
                {
                    int y0 = Enumerable.Range(cy - 30, 61).Where(y => Changed(x, y)).DefaultIfEmpty(int.MinValue).OrderBy(y => Mathf.Abs(y - cy)).First();
                    if (y0 == int.MinValue) { runs.Add(0); continue; }
                    int lo = y0, hi = y0;
                    while (Changed(x, lo - 1)) lo--;
                    while (Changed(x, hi + 1)) hi++;
                    runs.Add(hi - lo + 1);
                }
                runs.Sort();
                TestContext.WriteLine($"{distance} m: shaft {runs[runs.Count / 2]} px (columns {string.Join(",", runs)})");
                return runs[runs.Count / 2];
            }
            float near = Thickness(CameraController.MinDistance), far = Thickness(CameraController.MaxDistance);
            Assert.GreaterOrEqual(near, 6f, "the shaft is clearly visible at the nearest zoom");
            Assert.GreaterOrEqual(far, 6f, "the shaft is clearly visible at the farthest zoom");
            Assert.LessOrEqual(Mathf.Abs(near - far), 2f, $"the same on screen at every zoom: {near} px near, {far} px far");
        }
        finally { RenderSample.TiltShift(true); Object.DestroyImmediate(map.gameObject); }
    }
}
