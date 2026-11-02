#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>#2201 (a player's report, relayed by the owner on 1 Nov 2026): lit hexes were a thin ivory outline with no fill,
/// hard to see over towns and trees, and nothing marked the hex under the pointer. Now a lit hex also has a translucent fill
/// and, while a Move or Attack pick is open, the hex under the pointer has a gold band with navy edges. Both are drawn over the
/// models (Statecraft/HexOverlay, ZTest Always).</summary>
public class HexHighlightTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f
        && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;   // vertex colours come back as 8-bit

    static bool Shows(MapScene map, string name) => map.Terrain.transform.Find(name)?.GetComponent<MeshRenderer>().enabled == true;

    /// <summary>Each lit hex gets a fill in the selection's ivory, drawn with the overlay material: a strip from the hex's edge,
    /// translucent so the ground and the models read through it, to a ring FillDepth further in, clear, so the middle of the
    /// hex stays clear at the closest zoom. The outline band stays as before. Nothing to
    /// light (none, or only hexes off the map) hides the fill with the band.</summary>
    [Test]
    public void LitHexesAreFilledWithATranslucentIvory()
    {
        var map = BuildMap();
        try
        {
            var hexes = C.Hexes.Where(h => h.Land).Take(3).Select(h => (h.Q, h.R)).ToList();
            map.ShowReachable(hexes);
            var fill = map.Terrain.transform.Find(MapScene.ReachableFillName);
            Assert.IsNotNull(fill, "the lit hexes have a fill");
            Assert.IsTrue(fill.GetComponent<MeshRenderer>().enabled);
            Assert.AreEqual(Resources.Load<Material>(MapScene.OverlayMaterialResource), fill.GetComponent<MeshRenderer>().sharedMaterial);
            var mesh = fill.GetComponent<MeshFilter>().sharedMesh;
            int n = MapBuilder.RingVertices;
            Assert.AreEqual(3 * 2 * n, mesh.vertexCount, "one strip of two rings per hex");
            var clear = MapScene.FillColour.linear; clear.a = 0f;
            var colours = mesh.colors;
            for (int i = 0; i < colours.Length; i++)
                Assert.IsTrue(Near(colours[i], i % (2 * n) < n ? MapScene.FillColour.linear : clear), $"vertex {i}: the edge tinted, the inner ring clear");
            var uvs = new System.Collections.Generic.List<Vector2>(); mesh.GetUVs(0, uvs);
            Assert.IsTrue(uvs.Count == mesh.vertexCount && uvs.All(uv => uv.x == 1f), "the fill fades as the camera comes close (uv.x 1)");
            Assert.AreEqual(MapScene.SelectionColour, new Color(MapScene.FillColour.r, MapScene.FillColour.g, MapScene.FillColour.b), "the selection's ivory");
            Assert.That(MapScene.FillColour.a, Is.InRange(0.15f, 0.5f), "translucent: the ground reads through");
            Assert.AreEqual(3 * 2 * MapBuilder.RingVertices, map.Terrain.transform.Find(MapScene.ReachableName).GetComponent<MeshFilter>().sharedMesh.vertexCount,
                "the outline band is unchanged");
            map.ShowReachable(new[] { (9999, 9999) });
            Assert.IsFalse(Shows(map, MapScene.ReachableFillName), "off the map: nothing to fill");
            map.ShowReachable(hexes);
            map.ShowReachable(new (int, int)[0]);
            Assert.IsFalse(Shows(map, MapScene.ReachableFillName));
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>ShowHover marks one hex with a band in three strips (navy edge, gold core, navy edge) centred on it, follows the
    /// hex it is given, hides with none or a hex off the map, and asking again for the same hex allocates nothing (GameRoot
    /// asks every frame).</summary>
    [Test]
    public void TheHoverMarkerFollowsItsHexAndHides()
    {
        var map = BuildMap();
        try
        {
            var land = C.Hexes.Where(h => h.Land).ToList();
            var a = land[land.Count / 2]; var b = land[land.Count / 2 + 40];   // inland: a ring at the map's edge is clamped to it
            map.ShowHover((a.Q, a.R));
            var hover = map.Terrain.transform.Find(MapScene.HoverName);
            Assert.IsNotNull(hover, "the hovered hex is marked");
            var renderer = hover.GetComponent<MeshRenderer>();
            Assert.IsTrue(renderer.enabled);
            Assert.AreEqual(Resources.Load<Material>(MapScene.HoverMaterialResource), renderer.sharedMaterial);
            var mesh = hover.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(3 * 2 * MapBuilder.RingVertices, mesh.vertexCount, "three strips of two rings");
            int n = MapBuilder.RingVertices;
            var colours = mesh.colors;
            for (int i = 0; i < colours.Length; i++)   // strip k is vertices 2kn..2(k+1)n-1: navy, gold, navy from the edge in
                Assert.IsTrue(Near(colours[i], i / (2 * n) == 1 ? MapScene.HoverColour.linear : MapScene.HoverInk.linear),
                    $"vertex {i}: navy outer strip, gold core, navy inner strip");
            var uvs = new System.Collections.Generic.List<Vector2>(); mesh.GetUVs(0, uvs);
            Assert.IsTrue(uvs.All(uv => uv.x == 0f), "the hover band keeps its strength at every zoom (uv.x 0)");
            Vector2 At(HexJson h) { var w = map.Frame.ToWorld(h.lon, h.lat, 0); return new Vector2(w.x, w.z); }
            HexJson Json(MapHex h) => map.Hexes.hexes.Single(j => j.q == h.Q && j.r == h.R);
            float size = map.Hexes.hex_size_km * 1000f;
            Vector2 Centre() => new Vector2(renderer.bounds.center.x, renderer.bounds.center.z);
            Assert.Less(Vector2.Distance(Centre(), At(Json(a))), 0.1f * size, "centred on the hex");
            map.ShowHover((b.Q, b.R));
            Assert.Less(Vector2.Distance(Centre(), At(Json(b))), 0.1f * size, "follows the hex it is given");
            map.ShowHover((b.Q, b.R));
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) map.ShowHover((b.Q, b.R));
            Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before, "the same hex again allocates nothing");
            map.ShowHover(null);
            Assert.IsFalse(renderer.enabled, "none: hidden");
            map.ShowHover((a.Q, a.R));
            map.ShowHover((9999, 9999));
            Assert.IsFalse(renderer.enabled, "off the map: hidden");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The overlay shader compiles for WebGL 2, and its material draws in the transparent queue just under the move
    /// arrow's (Transparent+50, #2134), so an arrow is never covered by a fill or the hover band. Its pass ignores depth (ZTest
    /// Always): the fix for #2201 rests on that line, with ZTest LEqual the towns and trees stand over the fill and the hover
    /// band again. The pass state is fixed in the source, not a material property, so the test reads the source.</summary>
    [Test]
    public void TheOverlayRunsOnWebGL2UnderTheMoveArrow()
    {
        var mat = Resources.Load<Material>(MapScene.OverlayMaterialResource);
        Assert.IsNotNull(mat);
        Assert.AreEqual("Statecraft/HexOverlay", mat.shader.name);
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(mat.shader));
        Assert.AreEqual((int)UnityEngine.Rendering.RenderQueue.Transparent + 40, mat.renderQueue);
        var source = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(mat.shader));
        StringAssert.IsMatch(@"(?m)^\s*ZTest\s+Always\s*$", source, "drawn over the models, not hidden behind them");
        StringAssert.IsMatch(@"(?m)^\s*ZWrite\s+Off\s*$", source, "writes no depth");
        StringAssert.IsMatch(@"(?m)^\s*Blend\s+SrcAlpha\s+OneMinusSrcAlpha\s*$", source, "alpha-blended: the fill is translucent");
    }

    /// <summary>The hover band draws over the lit hexes' fill and under the move arrow (#2184: queue Transparent+50, the default
    /// sorting layer, order in layer 0), so the arrow's tip on the hovered hex stays on top. URP sorts transparent renderers by
    /// sorting layer and order in layer before the queue, so the band keeps the arrow's layer and order and sits under it by
    /// queue alone: an order of 1 would draw it after the arrow whatever the queue. Same shader as the fill (ZTest Always).</summary>
    [Test]
    public void TheHoverBandDrawsOverTheFillAndUnderTheMoveArrow()
    {
        var map = BuildMap();
        try
        {
            var lit = C.Hexes.Where(h => h.Land).Select(h => (h.Q, h.R)).First();
            map.ShowReachable(new[] { lit });
            map.ShowHover(lit);
            var fill = map.Terrain.transform.Find(MapScene.ReachableFillName).GetComponent<MeshRenderer>();
            var hover = map.Terrain.transform.Find(MapScene.HoverName).GetComponent<MeshRenderer>();
            Assert.AreEqual(0, hover.sortingLayerID, "the arrow's sorting layer, the default");
            Assert.AreEqual(0, hover.sortingOrder, "the arrow's order in layer: a higher one draws the band over the arrow");
            Assert.AreEqual(0, fill.sortingLayerID);
            Assert.AreEqual(0, fill.sortingOrder);
            const int arrow = (int)UnityEngine.Rendering.RenderQueue.Transparent + 50;
            Assert.That(hover.sharedMaterial.renderQueue, Is.GreaterThan(fill.sharedMaterial.renderQueue).And.LessThan(arrow),
                "queued after the fill and before the arrow");
            Assert.AreEqual(fill.sharedMaterial.shader, hover.sharedMaterial.shader, "drawn over the models as the fill is");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>GameRoot marks the hex under the pointer only where a click would give the pick's order (PickTakes), during the
    /// Move pick and the Attack pick: a lit hex, and not water or an unlit land hex the pick does not take (here the unit's own
    /// hex), where a click ends the pick. Nothing before a unit is selected, off the map, or once the pick ends.</summary>
    [Test]
    public void TheHoverMarksOnlyHexesAClickWouldTake()
    {
        using var rig = new SelectThenClickMoveTests.Rig();
        var s = rig.Bridge.State;
        var id = UnitOrdersTests.EnemyInGolanisReach(s);
        rig.Layer.Show(rig.Bridge.Content, s);
        var water = C.Hexes.First(h => !h.Land);
        var sea = (water.Q, water.R);
        var home = rig.At(id);
        GameRoot.MarkHover(rig.Map, rig.Info, rig.Reach(id).First());
        Assert.IsFalse(Shows(rig.Map, MapScene.HoverName), "no pick: no marker");
        void MarksOnlyWhatAClickTakes(string pick)
        {
            var lit = rig.Lit.Last();
            Assert.IsNotEmpty(lit, $"precondition: the {pick} pick lights hexes");
            Assert.IsFalse(lit.Contains(home) || rig.Info.PickTakes(home.q, home.r), $"precondition: the {pick} pick does not take the unit's own hex");
            Assert.IsFalse(rig.Info.PickTakes(sea.Q, sea.R), $"precondition: the {pick} pick does not take water");
            GameRoot.MarkHover(rig.Map, rig.Info, lit.First());
            Assert.IsTrue(Shows(rig.Map, MapScene.HoverName), $"the {pick} pick marks a lit hex");
            GameRoot.MarkHover(rig.Map, rig.Info, sea);
            Assert.IsFalse(Shows(rig.Map, MapScene.HoverName), $"the {pick} pick: no marker on water, a click there ends the pick");
            GameRoot.MarkHover(rig.Map, rig.Info, home);
            Assert.IsFalse(Shows(rig.Map, MapScene.HoverName), $"the {pick} pick: no marker on an unlit land hex it does not take");
            GameRoot.MarkHover(rig.Map, rig.Info, lit.First());
            GameRoot.MarkHover(rig.Map, rig.Info, null);
            Assert.IsFalse(Shows(rig.Map, MapScene.HoverName), "off the map: no marker");
        }
        rig.Select(rig.Token("golani"));
        Assert.IsTrue(rig.Info.MovePicking, "precondition: the Move pick is open");
        MarksOnlyWhatAClickTakes("Move");
        UiTestKit.Press(rig.Ui.Q<Button>("info-attack"));
        Assert.IsFalse(rig.Info.MovePicking, "precondition: the Attack pick is open");
        Assert.IsTrue(rig.Info.Picking);
        MarksOnlyWhatAClickTakes("Attack");
        var target = rig.Lit.Last().First();
        rig.Click(sea.Q, sea.R);   // a hex with no target ends the pick
        Assert.IsFalse(rig.Info.Picking, "precondition: the pick ended");
        GameRoot.MarkHover(rig.Map, rig.Info, target);
        Assert.IsFalse(Shows(rig.Map, MapScene.HoverName), "the pick ended: no marker");
    }
}
