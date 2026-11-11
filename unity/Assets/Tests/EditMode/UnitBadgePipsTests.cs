#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

/// <summary>#2432, #2960 PR 2: the damaged look on the map. Owner, 8 November 2026: "A. Pips on badge (Recommended)" (one
/// pip per population unit on the badge plate, filled for the strength left, hollow for the strength lost; not colour
/// alone; shared meshes on the existing unlit badge material) and "(c) No bands, pips (Recommended)". Owner, 9 November
/// 2026: "Rows of five (Recommended)", "Every military unit (Recommended)" (a full unit shows its pips too) and "All 3 as
/// recommended (Recommended)": a row inside the plate under the glyph (the glyph smaller, the plate's size unchanged), the
/// same order in both languages, always shown with the plate. #2432: both sides, clears on a reinforce, dims with the
/// badge, built when the tokens are rebuilt (no per-frame work), military sim units only. The pips are read back from the
/// badge's "Pips" mesh: one connected piece per pip, a disc when filled, a ring when hollow.</summary>
public class UnitBadgePipsTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    const string Golani = "golani", Egypt = "egyptian_expeditionary_force";

    MapScene map = null!;
    UnitLayer? layer;
    GameObject standIn = null!;
    bool readable;

    [OneTimeSetUp]
    public void BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("MapFrame"));
        map.Build();
    }

    [OneTimeTearDown]
    public void DropMap()
    {
        if (map != null) Object.DestroyImmediate(map.gameObject);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [SetUp]
    public void Prepare()
    {
        // The meshes are read back below (a player build keeps no CPU copy).
        readable = UnitPlinth.KeepMeshesReadable; UnitPlinth.KeepMeshesReadable = true;
        // An Arab force's token waits for its Collection figure, which edit mode never streams: answer at once with a
        // stand-in, as UnitBadgeScreenCapTests does (the badge does not depend on the figure's mesh).
        standIn = GameObject.CreatePrimitive(PrimitiveType.Cube); standIn.transform.position = Vector3.one * -100000f;
        UnitFigures.Source = (_, __, done) => done(standIn); UnitFigures.Forget();
        layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
    }

    [TearDown]
    public void Discard()
    {
        if (layer != null) Object.DestroyImmediate(layer.gameObject);
        layer = null;
        UnitFigures.Source = null; UnitFigures.Forget();
        Object.DestroyImmediate(standIn);
        UnitPlinth.KeepMeshesReadable = readable;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static GameState NewGame() => Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();

    static int SimId(string row) => C.StartingForces.FindIndex(f => f.Row == row);

    /// <summary>Sets sim unit <paramref name="id"/>'s strength and its Full (0: it has lost none).</summary>
    static void SetStrength(GameState s, int id, int strength, int full)
    {
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, strength, u.Order, u.OrderTarget, u.HomeCity, full, u.CombatXp);
    }

    Transform Badge(string row) => layer!.transform.Find("Tokens/" + row + "/Badge");

    struct Pip { public Vector2 Centre; public float Radius; public bool Hollow; }

    /// <summary>The pips on <paramref name="badge"/>, in the plate's frame (1 x 1, centred), top row first, each row from
    /// the left: one connected piece of the "Pips" mesh per pip; a ring (some vertices well inside its radius) is hollow.
    /// None when the badge has no "Pips" renderer.</summary>
    static List<Pip> PipsOn(Transform badge)
    {
        var holder = badge.Find("Pips");
        if (holder == null) return new List<Pip>();
        var mesh = holder.GetComponent<MeshFilter>().sharedMesh;
        Assert.IsTrue(mesh.isReadable, "fixture: the pips' mesh is kept readable");
        var v = mesh.vertices; var t = mesh.triangles;
        var parent = Enumerable.Range(0, v.Length).ToArray();
        int Root(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        for (int i = 0; i < t.Length; i += 3) { parent[Root(t[i + 1])] = Root(t[i]); parent[Root(t[i + 2])] = Root(t[i]); }
        var offset = (Vector2)holder.localPosition;
        var pips = new List<Pip>();
        foreach (var piece in Enumerable.Range(0, v.Length).GroupBy(Root))
        {
            var pts = piece.Select(i => (Vector2)v[i] + offset).ToList();
            var centre = pts.Aggregate(Vector2.zero, (a, b) => a + b) / pts.Count;
            float far = pts.Max(p => (p - centre).magnitude), near = pts.Min(p => (p - centre).magnitude);
            pips.Add(new Pip { Centre = centre, Radius = far, Hollow = near < 0.8f * far });
        }
        return Rows(pips).SelectMany(r => r).ToList();
    }

    /// <summary>The pips' rows, top first, each from the left: a pip within 0.02 of the plate's height of the row's first
    /// pip stands in that row.</summary>
    static List<List<Pip>> Rows(List<Pip> pips)
    {
        var rows = new List<List<Pip>>();
        foreach (var p in pips.OrderByDescending(p => p.Centre.y))
            if (rows.Count > 0 && Mathf.Abs(rows[rows.Count - 1][0].Centre.y - p.Centre.y) < 0.02f) rows[rows.Count - 1].Add(p);
            else rows.Add(new List<Pip> { p });
        return rows.Select(r => r.OrderBy(p => p.Centre.x).ToList()).ToList();
    }

    static string Draw(List<Pip> pips) => string.Join(" ", Rows(pips).Select(r => string.Concat(r.Select(p => p.Hollow ? "o" : "x"))));

    /// <summary>The look the rulings ask for: <paramref name="strength"/> filled pips then the lost ones hollow, five to a
    /// row from the top, e.g. "xxxxx xxooo" for 7 of 10.</summary>
    static string Want(int strength, int full) => string.Join(" ", Enumerable.Range(0, (full + 4) / 5)
        .Select(r => string.Concat(Enumerable.Range(r * 5, Mathf.Min(5, full - r * 5)).Select(i => i < strength ? "x" : "o"))));

    /// <summary>"Every military unit (Recommended)", both sides: at the opening every military sim unit's badge carries one
    /// filled pip per population unit (its full strength, UnitDamage.FullStrength), the Egyptian force's 10 in two rows of
    /// five; the squadron, the ships and a map-only token (no sim unit, no strength) carry none. Units of the same strength
    /// share one pips mesh.</summary>
    [Test]
    public void EveryMilitaryUnitOnBothSidesShowsItsStrengthInPips()
    {
        var s = NewGame();
        layer!.Show(C, s);
        int israel = 0, arab = 0;
        var meshes = new Dictionary<string, Mesh>();
        foreach (var p in layer.Tokens)
        {
            var badge = Badge(p.Token.Unit.Id);
            var u = p.Token.StateUnit;
            if (u == null || u.Side == "")
            {
                Assert.IsNull(badge.Find("Pips"), $"{p.Token}: no sim strength, no pips");
                continue;
            }
            int full = UnitDamage.FullStrength(C, u);
            Assert.AreEqual(Want(u.Strength, full), Draw(PipsOn(badge)), $"{p.Token}: {u.Strength} of {full}");
            if (u.Side == UnitData.Israel) israel++; else arab++;
            var key = Want(u.Strength, full);
            var mesh = badge.Find("Pips").GetComponent<MeshFilter>().sharedMesh;
            if (meshes.TryGetValue(key, out var seen)) Assert.AreSame(seen, mesh, $"{p.Token}: the same pips share one mesh");
            else meshes[key] = mesh;
        }
        Assert.Greater(israel, 2, "Israeli units carry pips");
        Assert.Greater(arab, 0, "an Arab force carries pips");
        Assert.IsTrue(layer.Tokens.Any(p => p.Token.StateUnit == null), "fixture: the squadron and the ships are drawn");
        var egypt = s.Units[SimId(Egypt)];
        Assert.AreEqual(10, UnitDamage.FullStrength(C, egypt), "fixture: the Egyptian force starts at 10 (units_1948.yaml)");
        Assert.AreEqual("xxxxx xxxxx", Draw(PipsOn(Badge(Egypt))), "\"Rows of five (Recommended)\": 10 is two rows of five");
    }

    /// <summary>"A. Pips on badge (Recommended)": a damaged unit shows filled pips for the strength left, then hollow ones
    /// for the strength lost (3 of 4: x x x o), by shape (a disc, a ring), not colour alone; the pips follow the sim through
    /// another loss and clear when a reinforce refills the unit (Full back to 0). A change in Full alone redraws them too.</summary>
    [Test]
    public void ADamagedUnitShowsHollowPipsForItsLossesAndAReinforceClearsThem()
    {
        var s = NewGame();
        int id = SimId(Golani);
        foreach (var (strength, full, want) in new[] { (3, 4, "xxxo"), (2, 4, "xxoo"), (1, 4, "xooo"), (4, 0, "xxxx"), (3, 0, "xxx"), (3, 4, "xxxo") })
        {
            SetStrength(s, id, strength, full);
            layer!.Show(C, s);
            var pips = PipsOn(Badge(Golani));
            Assert.AreEqual(want, Draw(pips), $"{strength} with Full {full}");
            Assert.AreEqual(1, Rows(pips).Count, "four or fewer pips stand in one row");
            for (int i = 1; i < pips.Count; i++) Assert.Greater(pips[i].Centre.x, pips[i - 1].Centre.x, "filled first, from the left");
        }
    }

    /// <summary>"Rows of five (Recommended)": one pip per unit, five to a row; a unit of 10 at 7 shows a full top row and
    /// x x o o o under it; 5 is one row; above 10 a third row.</summary>
    [Test]
    public void ALargeUnitShowsItsPipsInRowsOfFive()
    {
        var s = NewGame();
        int id = SimId(Golani);
        foreach (var (strength, full) in new[] { (5, 0), (6, 0), (7, 10), (10, 0), (4, 10), (12, 0) })
        {
            SetStrength(s, id, strength, full);
            layer!.Show(C, s);
            int f = full > 0 ? full : strength;
            var pips = PipsOn(Badge(Golani));
            Assert.AreEqual(Want(strength, f), Draw(pips), $"{strength} of {f}");
            var rows = Rows(pips);
            Assert.AreEqual((f + 4) / 5, rows.Count, $"{strength} of {f}: rows of five");
            for (int r = 1; r < rows.Count; r++)
                Assert.Less(rows[r][0].Centre.y, rows[r - 1][0].Centre.y, "each next row stands under the last");
        }
    }

    /// <summary>"All 3 as recommended (Recommended)": the pips stand in a strip inside the plate, under the glyph, which is
    /// drawn smaller than the 0.5-tall army glyph so they fit; the plate keeps its 1 x 1 mesh and its scale (the click box,
    /// the label keep-clear box and the 0.055 screen cap all read the plate). The pips stay big enough to read at the
    /// gameplay zoom: up to 10, each at least 0.09 of the plate across (about 5 of the plate's 59 pixels).</summary>
    [Test]
    public void ThePipsStandInsideThePlateUnderASmallerGlyph()
    {
        var s = NewGame();
        int id = SimId(Golani);
        for (int full = 1; full <= 12; full++)
        {
            SetStrength(s, id, full - 1 > 0 ? full - 1 : full, full);
            layer!.Show(C, s);
            var badge = Badge(Golani);
            var plate = badge.Find("Plate");
            var plateBox = plate.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.AreEqual(1f, plateBox.size.x, 1e-3f, "the plate is 1 wide"); Assert.AreEqual(1f, plateBox.size.y, 1e-3f, "and 1 tall");
            Assert.AreEqual(Vector3.one, plate.localScale, "at the badge's own scale");
            var glyph = badge.Find("Glyph");
            var glyphBox = glyph.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.Less(glyphBox.size.y, 0.45f, $"{full}: the glyph is drawn smaller to make room");
            var pips = PipsOn(badge);
            Assert.AreEqual(full, pips.Count, $"{full}: one pip per unit");
            foreach (var p in pips)
            {
                Assert.IsTrue(InsidePlate(p.Centre, p.Radius + 0.02f), $"{full}: the pip at {p.Centre} (r {p.Radius:0.000}) is inside the plate");
                Assert.Less(p.Centre.y + p.Radius, glyphBox.min.y + glyph.localPosition.y, $"{full}: the pip at {p.Centre} is under the glyph");
                if (full <= 10) Assert.GreaterOrEqual(2f * p.Radius, 0.09f, $"{full}: the pip reads at the gameplay zoom");
            }
        }
    }

    /// <summary>Whether a circle of <paramref name="r"/> at <paramref name="c"/> is inside UnitBadge.PlateMesh's rounded
    /// square (1 x 1, corner radius 0.22).</summary>
    static bool InsidePlate(Vector2 c, float r)
    {
        var q = new Vector2(Mathf.Abs(c.x), Mathf.Abs(c.y));
        const float half = 0.5f, rad = 0.22f;
        if (q.x <= half - rad || q.y <= half - rad) return q.x + r <= half && q.y + r <= half;
        return (q - new Vector2(half - rad, half - rad)).magnitude + r <= rad;
    }

    /// <summary>"All 3 as recommended (Recommended)": the same order in both languages. The pips are a count, not text: with
    /// the process in Hebrew (he-IL) every badge's pips are the ones it has in English, filled first from the left.</summary>
    [Test]
    public void ThePipsReadTheSameWayInHebrew()
    {
        var s = NewGame();
        SetStrength(s, SimId(Golani), 3, 4);
        string Drawn()
        {
            layer!.Show(C, s, slide: false);
            return string.Join("\n", layer.Tokens.Select(p => p.Token.Unit.Id + " " + Draw(PipsOn(Badge(p.Token.Unit.Id)))
                + " " + string.Join(",", PipsOn(Badge(p.Token.Unit.Id)).Select(x => x.Centre.x.ToString("0.000", CultureInfo.InvariantCulture)))));
        }
        var culture = CultureInfo.CurrentCulture; var ui = CultureInfo.CurrentUICulture;
        string english, hebrew;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            english = Drawn();
            if (layer != null) Object.DestroyImmediate(layer.gameObject);
            layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("he-IL");
            hebrew = Drawn();
        }
        finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = ui; }
        StringAssert.Contains(Golani + " xxxo ", english, "fixture: the damaged unit's pips");
        Assert.AreEqual(english, hebrew, "the same pips in the same order in both languages");
    }

    /// <summary>#2432: the end-turn Dim shades the pips with the rest of the badge (each at UnitLayer.DimShade of its own
    /// colour) and lifts it again.</summary>
    [Test]
    public void ThePipsDimWithTheBadge()
    {
        layer!.Show(C, NewGame());
        var pips = Badge(Golani).Find("Pips");
        Assert.IsNotNull(pips, "golani's badge carries pips");
        var r = pips.GetComponent<Renderer>();
        Assert.AreSame(Badge(Golani).Find("Glyph").GetComponent<Renderer>().sharedMaterial, r.sharedMaterial,
            "the pips use the glyph's unlit badge material");
        var block = new MaterialPropertyBlock();
        layer.Dim(new[] { Golani });
        r.GetPropertyBlock(block);
        var own = r.sharedMaterial.color;
        var want = new Vector4(own.r * UnitLayer.DimShade, own.g * UnitLayer.DimShade, own.b * UnitLayer.DimShade, own.a);
        Assert.Less(Vector4.Distance(want, (Vector4)block.GetColor("_BaseColor")), 1e-3f, "the pips dim with the badge");
        layer.Dim(new string[0]);
        r.GetPropertyBlock(block);
        Assert.IsTrue(block.isEmpty, "and come back with it");
    }

    /// <summary>#2432 "at most 2.5 ms", no per-frame work: the pips are children of the badge, built with the tokens, so
    /// turning the badges to a moving camera allocates nothing.</summary>
    [Test]
    public void FacingBadgesWithPipsDoesNotAllocate()
    {
        var eye = new GameObject("PipsCamera");
        try
        {
            layer!.Show(C, NewGame());
            Assert.IsNotNull(Badge(Golani).Find("Pips"), "golani's badge carries pips");
            var cam = eye.AddComponent<Camera>(); cam.farClipPlane = 1e6f;
            cam.transform.position = Badge(Golani).position + new Vector3(0f, 20000f, -30000f);
            for (int k = 0; k < 5; k++) { cam.transform.Rotate(0f, 0.1f, 0f); layer.FaceCamera(cam); }
            Assert.That(() => { for (int k = 0; k < 50; k++) { cam.transform.Rotate(0f, 0.1f, 0f); layer.FaceCamera(cam); } }, Is.Not.AllocatingGCMemory());
        }
        finally { Object.DestroyImmediate(eye); }
    }
}
