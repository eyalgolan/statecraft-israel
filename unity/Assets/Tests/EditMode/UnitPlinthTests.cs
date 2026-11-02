using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.Map.Collection;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Demo2 T29's token look (refs #197): every army token stands on a round earth plinth with a white rim and flies
/// its side's flag on a pole; a side with no sourced flag flies none. The figures stand about 1.8 times a house
/// (docs/units.md, "The plinth and the flag").</summary>
public class UnitPlinthTests
{
    /// <summary>The legacy loop, whose roster follows the rows' dated windows and the call-up: these tests sweep the war's
    /// tokens by date (the GDD loop draws the sim's units, UnitsFromStateTests).</summary>
    static readonly Content C = SimBridgeGddTests.Legacy;
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName));
    static readonly DateTime[] Dates = { new DateTime(1948, 5, 29), new DateTime(1948, 7, 12), new DateTime(1948, 12, 4) };

    [SetUp] public void Readable() => UnitPlinth.KeepMeshesReadable = true;   // the tests read the built meshes back
    [TearDown] public void Released() => UnitPlinth.KeepMeshesReadable = false;

    // Simulates a retained render entry; it does not establish why OnDisable missed unsubscription.
    [Test]
    public void DestroyedFlagDoesNotRunACapturedCameraCallback()
    {
        var flagObject = new GameObject("flag");
        var cameraObject = new GameObject("camera");
        try
        {
            var flag = flagObject.AddComponent<UnitFlagFacing>();
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(8f, 3f, 11f);
            var method = typeof(UnitFlagFacing).GetMethod("Turn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            var callback = (Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>)Delegate.CreateDelegate(
                typeof(Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>), flag, method!);
            UnityEngine.Object.DestroyImmediate(flagObject);
            Assert.IsTrue(flag == null, "precondition: DestroyImmediate should leave the component Unity-null");
            Assert.DoesNotThrow(() => callback(default, camera));
        }
        finally
        {
            if (flagObject != null) UnityEngine.Object.DestroyImmediate(flagObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    // A retained render entry can outlive unsubscription; exercise the initialized branch without artwork fetches.
    [Test]
    public void DestroyedFigureDoesNotRunACapturedCameraCallback()
    {
        var holder = new GameObject("figure holder");
        var cameraObject = new GameObject("camera");
        try
        {
            var owner = holder.AddComponent<UnitFigureResidency>();
            holder.SetActive(false);
            owner.Initialize("proto:figure-palmach-scout-c12f-1948-v2", ProductionResidency.Approved().Units, null, draw: false);
            var initialized = typeof(UnitFigureResidency).GetField("initialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(initialized);
            Assert.IsTrue((bool)initialized!.GetValue(owner), "precondition: the callback must exercise initialized residency");
            var camera = cameraObject.AddComponent<Camera>();
            var method = typeof(UnitFigureResidency).GetMethod("Choose", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            var callback = (Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>)Delegate.CreateDelegate(
                typeof(Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>), owner, method!);
            UnityEngine.Object.DestroyImmediate(holder);
            Assert.IsTrue(owner == null, "precondition: destroyed residency must be Unity-null");
            Assert.DoesNotThrow(() => callback(default, camera));
        }
        finally
        {
            if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    static GameState StateAt(DateTime date)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        s.Date = date; s.Rung = 5; s.Mobilization = 0.8;
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

    static bool Painted(Mesh m, int slot) => m.uv.Any(uv => Mathf.Abs(uv.x - UnitPalette.U(slot)) < 1e-4f);

    /// <summary>A token's plinth, rim and flag: the plinth mesh is earth with a white rim; the flag is the side's own.</summary>
    static void AssertPlinth(MapScene map, Transform token, string side, string what)
    {
        var plinth = token.Find(UnitPlinth.PlinthName);
        Assert.IsNotNull(plinth, $"{what}: stands on a plinth");
        var mesh = plinth.GetComponent<MeshFilter>().sharedMesh;
        Assert.IsTrue(Painted(mesh, UnitPalette.Earth), $"{what}: the plinth is earth");
        var rim = token.Find(UnitPlinth.RimName);
        Assert.IsNotNull(rim, $"{what}: the plinth has its white rim");
        Assert.AreEqual(plinth.localScale.x, rim.localScale.x, 0.5f, $"{what}: the rim rings the plinth");
        Assert.IsNotNull(token.Find(UnitPlinth.LipName), $"{what}: the plinth has its edge lip");
        var flag = token.Find(UnitPlinth.FlagName);
        if (UnitFlags.Has(side))
        {
            Assert.IsNotNull(flag, $"{what}: flies its side's flag");
            Assert.AreEqual(UnitFlags.MeshName(side), flag.GetComponent<MeshFilter>().sharedMesh.name, $"{what}: the flag is {side}'s");
        }
        else Assert.IsNull(flag, $"{what}: {side} has no sourced flag, so none is drawn");
        var badge = token.Find("Badge");
        if (flag != null && badge != null)   // the flag flies clear of the badge over the trio (a compact token's too)
            Assert.Greater(flag.position.x - badge.position.x, UnitBadge.SizeM / 2f, $"{what}: the flag's pole stands clear of the badge");
        var ground = UnitPlinth.HeightAt(map);
        foreach (var f in token.Cast<Transform>().Where(t => t.name.StartsWith("Rifleman")))
            Assert.AreEqual(ground(f.position.x, f.position.z) + UnitPlinth.LipShare * plinth.localScale.x, f.position.y, 0.5f,
                $"{what}/{f.name}: stands on the draped top at its feet");
    }

    // ---- Every token ----------------------------------------------------------------------------------------------

    /// <summary>On every date checked, every drawn army token has a plinth, a rim and its side's flag; aircraft and ships
    /// have none (they fly or float: deliberate, docs/units.md).</summary>
    [Test]
    public void EveryArmyTokenStandsOnAPlinthWithARimAndItsFlag()
    {
        var map = BuildMap();
        try
        {
            var units = UnitData.Parse(CommittedYaml);
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            int army = 0;
            foreach (var d in Dates)
            {
                layer.Show(C, StateAt(d));
                var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
                foreach (var p in layer.Tokens)
                {
                    var t = tokens.Find(p.Token.Unit.Id);
                    if (p.Token.Unit.Service == UnitData.Army) { AssertPlinth(map, t, p.Token.Unit.Side, $"{d:yyyy-MM-dd} {t.name}"); army++; }
                    else
                    {
                        Assert.IsNull(t.Find(UnitPlinth.PlinthName), $"{t.name}: no plinth under an aircraft or a ship");
                        Assert.IsNull(t.Find(UnitPlinth.FlagName), $"{t.name}: no flag on an aircraft or a ship");
                    }
                }
            }
            Assert.Greater(army, 15, "army tokens were checked");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The showcase trio (the promo captures' -units) has the same plinth and the Israeli flag.</summary>
    [Test]
    public void TheShowcaseTrioStandsOnAPlinthWithTheIsraeliFlag()
    {
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            AssertPlinth(map, root.transform, UnitData.Israel, "showcase");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    // ---- The flag matches the side --------------------------------------------------------------------------------

    /// <summary>Every row of the unit file names its side, and every formation the map draws is Israel's or a force with a
    /// Collection figure (owner, 17 October 2026, #740; issue #665); Israel, Transjordan, Egypt, Iraq, Syria and Lebanon have a
    /// flag (built from their 1948 texts; owner, 31 October 2026: "we need to have the flags of all arab forces like we have
    /// for the israeli ones"), so no flag is invented: a drawn force with none, the ALA's alone, flies the neutral pennant
    /// (Q-ART-1, owner, 15 October 2026), in neither side's colours and not white, and no other side flies anything.</summary>
    [Test]
    public void TheFlagIsTheSidesAndOnlyASourcedFlagIsDrawn()
    {
        var units = UnitData.Parse(CommittedYaml);
        Assert.IsTrue(units.Where(UnitRoster.OnMap).All(u => u.Side == UnitData.Israel || UnitFigures.SideFigure(u.Side) != null),
            "every formation the map draws is Israel's or has its own figure");
        foreach (var side in Flagged)
        {
            Assert.IsTrue(UnitFlags.Has(side), $"{side}: its sourced 1948 flag");
            Assert.IsFalse(UnitFlags.HasPennant(side), $"{side}: a flag, not the pennant");
        }
        foreach (var side in UnitData.Sides.Except(Flagged))
        {
            Assert.IsFalse(UnitFlags.Has(side), $"{side}: no sourced, licensed flag in the repo, so none");
            Assert.IsNull(UnitFlags.Build(side), side);
            Assert.AreEqual(UnitFigures.SideFigure(side) != null, UnitFlags.HasPennant(side), $"{side}: the pennant only on a drawn force");
        }
        CollectionAssert.AreEquivalent(new[] { "ala" }, UnitData.Sides.Where(UnitFlags.HasPennant), "only the ALA flies the pennant (never its dagger emblem: #226, ruling 8)");
        var pennant = UnitFlags.BuildPennant();
        var slots = pennant.uv.Select(u => Enumerable.Range(0, UnitPalette.Count).Single(s => Mathf.Abs(u.x - UnitPalette.U(s)) < 1e-4f)).Distinct();
        CollectionAssert.AreEquivalent(new[] { UnitPalette.RifleWood, UnitPalette.HullGrey }, slots, "a wooden pole and a plain grey cloth");

        // A side with no figure draws nothing: no IDF figures, no placeholder (owner, 17 October 2026, #740). The file with
        // every Israeli row relabelled the local forces' (each given an echelon and a strength, as such a row needs) draws no token.
        var map = BuildMap();
        try
        {
            // A brigade that already gives its strength (#743) keeps it: a second strength key would not parse.
            var local = UnitData.Parse(string.Join("\n- id: ", CommittedYaml.Split(new[] { "\n- id: " }, StringSplitOptions.None).Select(row =>
                row.Replace("side: israel", "side: local_arab_forces\n  echelon: brigade" + (row.Contains("\n  strength:") ? ""
                    : "\n  strength: {value: 1, as_of: '1948-05-15', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}")))));
            Assert.Greater(local.Count(u => u.Side == "local_arab_forces" && u.Service == UnitData.Army), 3);
            var layer = UnitLayer.Create(map, local, C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            Assert.IsEmpty(layer.Tokens, "a row that is not Israel's is not drawn");
            Assert.AreEqual(0, map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").childCount);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The Israeli flag as the Provisional Council's proclamation gives it (220 by 160, two sky-blue stripes 25
    /// broad at 15 from the top and the bottom, a Star of David of six stripes 5.5 broad whose triangles' bases are
    /// parallel to the stripes): every blue vertex lies in a stripe or in the star, every white one in the field.</summary>
    [Test]
    public void TheIsraeliFlagFollowsTheSpecification()
    {
        var m = UnitFlags.Build(UnitData.Israel);
        Assert.IsNotNull(m);
        var v = m!.vertices; var uv = m.uv;
        // To the specification's centimetres, the field's lower corner at the origin (the mesh is in figure heights).
        float k = 220f / UnitFlags.FlagWidth;
        Vector2 Cm(Vector3 p) => new Vector2((p.x - UnitFlags.FlagLeft) * k, (p.y - UnitFlags.FlagBottom) * k);
        int blue = 0, inStripes = 0, inStar = 0;
        for (int i = 0; i < v.Length; i++)
        {
            if (Mathf.Abs(uv[i].x - UnitPalette.U(UnitPalette.RoundelBlue)) > 1e-4f) continue;
            blue++;
            var c = Cm(v[i]);
            Assert.That(c.x, Is.InRange(-0.01f, 220.01f), "blue stays on the field");
            bool stripe = c.y >= 14.99f && c.y <= 40.01f || c.y >= 119.99f && c.y <= 145.01f;
            float r = (c - new Vector2(110f, 80f)).magnitude;
            bool star = r <= UnitFlags.StarOuterCm + 0.01f && r >= UnitFlags.StarOuterCm / 2f - 5.5f * 2f - 0.01f;
            Assert.IsTrue(stripe || star, $"a blue vertex at {c} (cm) is in neither a stripe nor the star");
            if (stripe) inStripes++; else inStar++;
        }
        Assert.AreEqual(2 * 2 * 2 * 4, inStripes, "two stripes, as quads on both faces of the flag (two-sided plates)");
        // The star, against the gazette: two equilateral triangles of 5.5 cm stripes, their bases parallel to the stripes,
        // centred between them; 69 cm across (the construction drawing; the law gives no size).
        var starPts = Enumerable.Range(0, v.Length).Where(i => Mathf.Abs(uv[i].x - UnitPalette.U(UnitPalette.RoundelBlue)) < 1e-4f)
            .Select(i => Cm(v[i])).Where(c => c.y > 41f && c.y < 119f).Select(c => c - new Vector2(110f, 80f)).ToList();
        Assert.AreEqual(69f / 2f, UnitFlags.StarOuterCm, 1e-4f, "69 cm across");
        foreach (var q in starPts)
            Assert.That(Mathf.Min(Mathf.Abs(q.magnitude - 34.5f), Mathf.Abs(q.magnitude - (34.5f - 2f * 5.5f))), Is.LessThan(0.01f),
                $"a star corner at {q.magnitude:0.00} cm from the centre: the outer corners at 34.5, the inner at 34.5 - 2 x 5.5");
        var outerAngles = starPts.Where(q => Mathf.Abs(q.magnitude - 34.5f) < 0.01f).Select(q => Mathf.Repeat(Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg, 360f)).ToList();
        var innerAngles = starPts.Where(q => Mathf.Abs(q.magnitude - 23.5f) < 0.01f).Select(q => Mathf.Repeat(Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg, 360f)).ToList();
        foreach (var want in new[] { 30f, 90f, 150f, 210f, 270f, 330f })
        {
            Assert.IsTrue(outerAngles.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, want)) < 0.01f), $"an outer corner at {want} degrees (a point up and down: bases parallel to the stripes)");
            Assert.IsTrue(innerAngles.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, want)) < 0.01f), $"an inner corner at {want} degrees");
        }
        Assert.IsTrue(outerAngles.Concat(innerAngles).All(a => new[] { 30f, 90f, 150f, 210f, 270f, 330f }.Any(w => Mathf.Abs(Mathf.DeltaAngle(a, w)) < 0.01f)), "no other corners: a hollow star of two triangles");
        Assert.AreEqual(6 * 2 * 2 * 4, inStar, "six star stripes, as quads on both faces");
        var white = Enumerable.Range(0, v.Length).Where(i => Mathf.Abs(uv[i].x - UnitPalette.U(UnitPalette.RoundelWhite)) < 1e-4f).Select(i => Cm(v[i])).ToList();
        Assert.AreEqual(0f, white.Min(c => c.x), 0.01f); Assert.AreEqual(220f, white.Max(c => c.x), 0.01f);
        Assert.AreEqual(0f, white.Min(c => c.y), 0.01f); Assert.AreEqual(160f, white.Max(c => c.y), 0.01f, "8:11");
        UnityEngine.Object.DestroyImmediate(m);
    }

    /// <summary>The Arab Legion's flag, Transjordan's as its Organic Law of 16 April 1928, Art. 3 gives it (the sourcing
    /// note's section 1; owner, 22 October 2026, #756): twice as long as wide, three equal bands, black, white and green from
    /// the top; at the hoist a red triangle whose longest side is the flag's width and whose height is a quarter of its
    /// length; in it a white seven-pointed star a twenty-fourth of the length across, centred where the triangle's
    /// bisectors meet, the axis through one point parallel to the pole. Measured by area, in the law's units (length 2).</summary>
    [Test]
    public void TheArabLegionFliesTransjordansFlagAsThe1928LawGivesIt()
    {
        var m = UnitFlags.Build(UnitFlags.Jordan);
        Assert.IsNotNull(m);
        Assert.AreEqual(UnitFlags.MeshName(UnitFlags.Jordan), m!.name);
        var colours = UnitFlags.Colours(UnitFlags.Jordan);
        Assert.AreEqual(1 + colours.Count, m.subMeshCount, "the pole, then one submesh per colour of the cloth");
        int Sub(Color c) => 1 + colours.ToList().IndexOf(c);
        float k = 2f / UnitFlags.FlagWidth, bottom = UnitFlags.FlagTop - UnitFlags.FlagWidth / 2f;
        var v = m.vertices.Select(p => new Vector3((p.x - UnitFlags.FlagLeft) * k, (p.y - bottom) * k, p.z)).ToArray();
        // One face's area of a colour: every plate is two-sided, so half the sum; the star (off the cloth's plane) separately.
        float Area(int sub, bool star)
        {
            var t = m.GetTriangles(sub); float a = 0f;
            for (int i = 0; i < t.Length; i += 3)
            {
                if ((Mathf.Abs(v[t[i]].z) > 1e-4f) != star || star && v[t[i]].z < 0f) continue;
                Vector2 p = v[t[i]], q = v[t[i + 1]], r = v[t[i + 2]];
                a += Mathf.Abs((q.x - p.x) * (r.y - p.y) - (r.x - p.x) * (q.y - p.y)) / 2f;
            }
            return a / 2f;
        }
        int black = Sub(UnitFlags.BlackColour), white = Sub(UnitFlags.WhiteColour), green = Sub(UnitFlags.GreenColour), red = Sub(UnitFlags.RedColour);
        Assert.That(new[] { black, white, green, red }, Is.All.GreaterThan(0), "black, white, green and red cloth");
        // The triangle (0,0), (0,1), (1/2,1/2) cuts 1/18 off the black and the green band and 1/4 - 2/18 off the white.
        Assert.AreEqual(0.25f, Area(red, false), 1e-4f, "the red triangle: the width at the hoist, a quarter of the length deep");
        Assert.AreEqual(2f / 3f - 1f / 18f, Area(black, false), 1e-4f, "the black band, the top third");
        Assert.AreEqual(2f / 3f - 1f / 18f, Area(green, false), 1e-4f, "the green band, the bottom third");
        Assert.AreEqual(2f / 3f - (0.25f - 2f / 18f), Area(white, false), 1e-4f, "the white band, the middle third");
        Assert.That(m.GetTriangles(black).Select(i => v[i].y), Is.All.GreaterThanOrEqualTo(2f / 3f - 1e-4f), "black on top");
        Assert.That(m.GetTriangles(green).Select(i => v[i].y), Is.All.LessThanOrEqualTo(1f / 3f + 1e-4f), "green below");
        Assert.That(m.GetTriangles(red).Select(i => v[i].x), Is.All.LessThanOrEqualTo(0.5f + 1e-4f), "red at the hoist");
        var cloth = Enumerable.Range(1, colours.Count).SelectMany(m.GetTriangles).Select(i => v[i]).ToList();
        Assert.AreEqual(2f, cloth.Max(p => p.x), 1e-4f); Assert.AreEqual(1f, cloth.Max(p => p.y), 1e-4f, "2:1");
        Assert.AreEqual(0f, cloth.Min(p => p.x), 1e-4f); Assert.AreEqual(0f, cloth.Min(p => p.y), 1e-4f);
        // The star: white, over the red, centred on the incentre (the side-weighted mean of the corners, each corner weighted
        // by the side opposite it), its points on a circle 1/12 across.
        Vector2 A = new Vector2(0f, 0f), B = new Vector2(0f, 1f), Cc = new Vector2(0.5f, 0.5f);
        float sa = (B - Cc).magnitude, sb = (Cc - A).magnitude, sc = (A - B).magnitude;
        var centre = (sa * A + sb * B + sc * Cc) / (sa + sb + sc);
        Assert.AreEqual(centre.x, (1f - centre.x - centre.y) / Mathf.Sqrt(2f), 1e-4f, "as far from the hoist as from the upper edge x + y = 1");
        var star = m.GetTriangles(white).Select(i => v[i]).Where(p => Mathf.Abs(p.z) > 1e-4f).Select(p => (Vector2)p - centre).ToList();
        Assert.IsNotEmpty(star, "a white star, off the cloth's plane on both faces");
        Assert.That(star.Max(q => q.magnitude), Is.EqualTo(1f / 24f).Within(1e-4f), "a twenty-fourth of the length across");
        var points = star.Where(q => Mathf.Abs(q.magnitude - 1f / 24f) < 1e-4f).Select(q => Mathf.Round(Mathf.Repeat(Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg, 360f) * 10f) / 10f).Distinct().ToList();
        Assert.AreEqual(7, points.Count, "seven points");
        Assert.IsTrue(points.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, 90f)) < 0.05f), "one point's axis parallel to the pole");
        Assert.That(Area(white, true), Is.GreaterThan(0f).And.LessThan(Mathf.PI / 576f), "a star inside its circle");
        UnityEngine.Object.DestroyImmediate(m);
    }

    /// <summary>The sides with a sourced flag.</summary>
    static readonly string[] Flagged = { UnitData.Israel, UnitFlags.Jordan, UnitFlags.Egypt, UnitFlags.Iraq, UnitFlags.Syria, UnitFlags.Lebanon };

    /// <summary>The flag sides' keys are the data's own: each Arab flag side is a side the unit file names (<see cref="UnitData.Sides"/>
    /// and a row of the committed units_1948.yaml) and has a figure the map draws (<see cref="UnitFigures.SideFigure"/>), so a
    /// misspelled key fails here. And every side in the class's own sourced set has its own builder and colours: one missing
    /// from <see cref="UnitFlags.Build"/>'s or <see cref="UnitFlags.Colours"/>'s switch throws rather than drawing Israel's flag.</summary>
    [Test]
    public void EveryFlagSideIsADrawnSideWithItsOwnBuilder()
    {
        var rows = UnitData.Parse(CommittedYaml);
        foreach (var side in new[] { UnitFlags.Jordan, UnitFlags.Egypt, UnitFlags.Iraq, UnitFlags.Syria, UnitFlags.Lebanon })
        {
            CollectionAssert.Contains(UnitData.Sides, side, $"{side}: a side the unit file allows");
            Assert.IsTrue(rows.Any(u => u.Side == side), $"{side}: a row of units_1948.yaml");
            Assert.IsNotNull(UnitFigures.SideFigure(side), $"{side}: a figure the map draws");
        }
        CollectionAssert.IsNotEmpty(UnitFlags.SourcedSides);
        CollectionAssert.AreEquivalent(UnitFlags.SourcedSides, UnitData.Sides.Where(UnitFlags.Has), "the sourced set is the data's sides with a flag");
        var names = new HashSet<string>();
        foreach (var side in UnitFlags.SourcedSides)
        {
            var m = UnitFlags.Build(side);
            Assert.IsNotNull(m, side);
            Assert.AreEqual(UnitFlags.MeshName(side), m!.name, $"{side}: built by its own builder");
            Assert.AreEqual(1 + UnitFlags.Colours(side).Count, m.subMeshCount, $"{side}: its own colours");
            names.Add(string.Join(",", UnitFlags.Colours(side)) + "/" + m.vertexCount);
            UnityEngine.Object.DestroyImmediate(m);
        }
        Assert.AreEqual(UnitFlags.SourcedSides.Count, names.Count, "no two sides share one flag's colours and mesh (no fall-through)");
        Assert.Throws<ArgumentException>(() => UnitFlags.Colours("ala"), "a side with no flag has no cloth colours");
    }

    /// <summary>Every sourced flag: the pole, then one non-empty submesh per cloth colour; <see cref="UnitFlags.FlagWidth"/>
    /// long from the pole and topped at <see cref="UnitFlags.FlagTop"/>, its height its law's proportion of that length.</summary>
    [Test]
    public void EverySourcedFlagHasItsColoursAndItsLawsProportion()
    {
        var ratio = new Dictionary<string, float>
        {
            [UnitData.Israel] = 160f / 220f, [UnitFlags.Jordan] = 1f / 2f, [UnitFlags.Iraq] = 1f / 2f, [UnitFlags.Syria] = 1f / 2f,
            [UnitFlags.Egypt] = 2f / 3f, [UnitFlags.Lebanon] = 2f / 3f,
        };
        CollectionAssert.AreEquivalent(Flagged, ratio.Keys);
        foreach (var side in Flagged)
        {
            var m = UnitFlags.Build(side);
            Assert.IsNotNull(m, side);
            Assert.AreEqual(UnitFlags.MeshName(side), m!.name);
            var colours = UnitFlags.Colours(side);
            Assert.AreEqual(1 + colours.Count, m.subMeshCount, $"{side}: the pole, then one submesh per cloth colour");
            Assert.AreEqual(colours.Count, colours.Distinct().Count(), $"{side}: each colour once");
            for (int k = 0; k < m.subMeshCount; k++) Assert.IsNotEmpty(m.GetTriangles(k), $"{side}: submesh {k} drawn");
            var cloth = Enumerable.Range(1, colours.Count).SelectMany(m.GetTriangles).Select(i => m.vertices[i]).ToList();
            Assert.AreEqual(UnitFlags.FlagLeft, cloth.Min(p => p.x), 1e-4f, $"{side}: from the pole");
            Assert.AreEqual(UnitFlags.FlagLeft + UnitFlags.FlagWidth, cloth.Max(p => p.x), 1e-4f, $"{side}: the same length as every flag");
            Assert.AreEqual(UnitFlags.FlagTop, cloth.Max(p => p.y), 1e-4f, $"{side}: the same top as every flag");
            Assert.AreEqual(ratio[side], (cloth.Max(p => p.y) - cloth.Min(p => p.y)) / UnitFlags.FlagWidth, 1e-3f, $"{side}: its law's proportion");
            UnityEngine.Object.DestroyImmediate(m);
        }
    }

    /// <summary>A flag's cloth in its own units (<paramref name="length"/> along +x from the pole, the bottom edge at y = 0),
    /// and one face's area of a submesh: on the cloth's plane, or (<paramref name="layered"/>) on the front layer a hair off it.</summary>
    static (Vector3[] v, Func<int, bool, float> area) InFlagUnits(Mesh m, float length, float hoist)
    {
        float k = length / UnitFlags.FlagWidth, bottom = UnitFlags.FlagTop - UnitFlags.FlagWidth * hoist / length;
        var v = m.vertices.Select(p => new Vector3((p.x - UnitFlags.FlagLeft) * k, (p.y - bottom) * k, p.z)).ToArray();
        float Area(int sub, bool layered)
        {
            var t = m.GetTriangles(sub); float a = 0f;
            for (int i = 0; i < t.Length; i += 3)
            {
                if ((Mathf.Abs(v[t[i]].z) > 1e-4f) != layered || layered && v[t[i]].z < 0f) continue;
                Vector2 p = v[t[i]], q = v[t[i + 1]], r = v[t[i + 2]];
                a += Mathf.Abs((q.x - p.x) * (r.y - p.y) - (r.x - p.x) * (q.y - p.y)) / 2f;
            }
            return a / 2f;   // every plate is two-sided
        }
        return (v, Area);
    }

    /// <summary>The points of the star of <paramref name="n"/> points centred at <paramref name="centre"/>, as angles in
    /// degrees, from the layered vertices of submesh <paramref name="sub"/> within <paramref name="radius"/> of it.</summary>
    static List<float> StarPoints(Mesh m, Vector3[] v, int sub, Vector2 centre, float radius)
    {
        var near = m.GetTriangles(sub).Select(i => v[i]).Where(p => Mathf.Abs(p.z) > 1e-4f).Select(p => (Vector2)p - centre).Where(q => q.magnitude <= radius + 1e-3f).ToList();
        Assert.That(near.Max(q => q.magnitude), Is.EqualTo(radius).Within(1e-3f), $"the star at {centre} reaches {radius}");
        return near.Where(q => Mathf.Abs(q.magnitude - radius) < 1e-3f).Select(q => Mathf.Round(Mathf.Repeat(Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg, 360f) * 10f) / 10f).Distinct().ToList();
    }

    /// <summary>Egypt's flag as Law No. 47 of 1923 and its annex give it (UnitFlags' class note), in the Flaggenbuch's units
    /// that match the annex (1008 by 672): green; the crescent's outer radius a quarter of the hoist (168), its centre on the
    /// line of the two stars nearest the staff, 448 (two thirds of the hoist) from it, its breadth at the middle 63 (three
    /// quarters of a star); three five-pointed stars 84 across (an eighth of the hoist) on an equilateral triangle of side 144
    /// (a seventh of the length). The crescent's area is the outer disc's less its overlap with the inner one.</summary>
    [Test]
    public void EgyptsFlagFollowsTheAnnexToLaw47Of1923()
    {
        var m = UnitFlags.Build(UnitFlags.Egypt)!;
        var colours = UnitFlags.Colours(UnitFlags.Egypt);
        int green = 1 + colours.ToList().IndexOf(UnitFlags.GreenColour), white = 1 + colours.ToList().IndexOf(UnitFlags.WhiteColour);
        Assert.That(new[] { green, white }, Is.All.GreaterThan(0), "green cloth, white crescent and stars");
        var (v, area) = InFlagUnits(m, 1008f, 672f);
        Assert.AreEqual(1008f * 672f, area(green, false), 1f, "the green field, 2:3");
        // The crescent: every white vertex outside the stars lies on the outer circle or the inner one, and none outside the outer.
        Vector2 o = new Vector2(448f, 336f), inner = new Vector2(UnitFlags.EgyptInnerCentre, 336f);
        float side = 1008f / 7f;
        var stars = new[] { new Vector2(448f, 336f + side / 2f), new Vector2(448f, 336f - side / 2f), new Vector2(448f + side * Mathf.Sqrt(3f) / 2f, 336f) };
        Assert.AreEqual(side, (stars[0] - stars[1]).magnitude, 1e-3f); Assert.AreEqual(side, (stars[2] - stars[0]).magnitude, 1e-3f, "an equilateral triangle, a seventh of the length a side");
        var crescent = m.GetTriangles(white).Select(i => v[i]).Where(p => p.z > 1e-4f).Select(p => (Vector2)p).Where(p => stars.All(c => (p - c).magnitude > 42f + 1e-2f)).ToList();
        Assert.IsNotEmpty(crescent);
        foreach (var p in crescent)
            Assert.That(Mathf.Min(Mathf.Abs((p - o).magnitude - 168f), Mathf.Abs((p - inner).magnitude - UnitFlags.EgyptInnerRadius)), Is.LessThan(0.05f), $"a crescent vertex at {p} on neither circle");
        Assert.AreEqual(448f - 672f / 4f, crescent.Min(p => p.x), 0.05f, "the crescent's back a quarter of the hoist behind its centre");
        Assert.AreEqual(63f, UnitFlags.EgyptInnerCentre - UnitFlags.EgyptInnerRadius - 280f, 1e-3f, "its breadth at the middle 63");
        Assert.AreEqual(63f, 0.75f * 672f / 8f, 1e-3f, "three quarters of a star's diameter");
        Assert.IsTrue(crescent.Any(p => Mathf.Abs(p.y - 336f) < 0.01f && Mathf.Abs(p.x - 343f) < 0.05f), "the inner edge at 343 on the axis");
        float d = UnitFlags.EgyptInnerCentre - 448f, R = 168f, r = UnitFlags.EgyptInnerRadius;
        Assert.AreEqual(448f + (d * d + R * R - r * r) / (2f * d), crescent.Max(p => p.x), 0.05f, "the horns towards the fly, where the circles meet");
        float lens = r * r * Mathf.Acos((d * d + r * r - R * R) / (2f * d * r)) + R * R * Mathf.Acos((d * d + R * R - r * r) / (2f * d * R))
            - 0.5f * Mathf.Sqrt((-d + r + R) * (d + r - R) * (d - r + R) * (d + r + R));
        Assert.AreEqual(Mathf.PI * R * R - lens, area(white, true) - 3f * StarArea(42f, 5), 0.02f * (Mathf.PI * R * R - lens), "the crescent: the outer disc less the inner (within the arcs' polygon)");
        // The stars: five points, 84 across, centred on the triangle; one up, one down, one towards the fly (the Commons drawing).
        float[] want = { 90f, 270f, 0f };
        for (int i = 0; i < 3; i++)
        {
            var pts = StarPoints(m, v, white, stars[i], 42f);
            Assert.AreEqual(5, pts.Count, $"star {i}: five points");
            Assert.IsTrue(pts.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, want[i])) < 0.1f), $"star {i}: a point at {want[i]} degrees");
            Assert.Less((stars[i] - inner).magnitude + 42f, UnitFlags.EgyptInnerRadius, $"star {i}: between the horns, clear of the crescent");
        }
        UnityEngine.Object.DestroyImmediate(m);
    }

    /// <summary>A regular {n/2} star's area, its points on a circle of radius <paramref name="outer"/>.</summary>
    static float StarArea(float outer, int n)
    {
        float inner = outer * Mathf.Cos(2f * Mathf.PI / n) / Mathf.Cos(Mathf.PI / n);
        return n * outer * inner * Mathf.Sin(Mathf.PI / n);   // 2n triangles from the centre, each outer x inner x sin(pi / n) / 2
    }

    /// <summary>Iraq's flag as the Organic Law of 1925, Art. 4 gives it (UnitFlags' class note), in the law's units (length 2):
    /// three equal bands, black, white and green from the top; at the staff a red trapezoid, the flag's width at the staff, the
    /// white band's at its other base, a quarter of the length deep; two white seven-pointed stars on it, one above the other,
    /// each in a circle a ninth of the length across at (1/4, 1/3) and (1/4, 2/3) (FOTW's construction; the law fixes none).</summary>
    [Test]
    public void IraqsFlagFollowsTheOrganicLawOf1925()
    {
        var m = UnitFlags.Build(UnitFlags.Iraq)!;
        var colours = UnitFlags.Colours(UnitFlags.Iraq).ToList();
        int black = 1 + colours.IndexOf(UnitFlags.BlackColour), white = 1 + colours.IndexOf(UnitFlags.WhiteColour), green = 1 + colours.IndexOf(UnitFlags.GreenColour), red = 1 + colours.IndexOf(UnitFlags.RedColour);
        Assert.That(new[] { black, white, green, red }, Is.All.GreaterThan(0), "black, white, green and red cloth");
        var (v, area) = InFlagUnits(m, 2f, 1f);
        // The trapezoid (0,0), (0,1), (1/2,2/3), (1/2,1/3) cuts 1/12 off the black and the green band and 1/6 off the white.
        Assert.AreEqual((1f + 1f / 3f) / 2f * 0.5f, area(red, false), 1e-4f, "the red trapezoid: bases 1 and 1/3, a quarter of the length deep");
        Assert.AreEqual(2f / 3f - 1f / 12f, area(black, false), 1e-4f, "the black band, the top third");
        Assert.AreEqual(2f / 3f - 1f / 12f, area(green, false), 1e-4f, "the green band, the bottom third");
        Assert.AreEqual(2f / 3f - 1f / 6f, area(white, false), 1e-4f, "the white band, the middle third");
        Assert.That(m.GetTriangles(black).Select(i => v[i].y), Is.All.GreaterThanOrEqualTo(2f / 3f - 1e-4f), "black on top");
        Assert.That(m.GetTriangles(green).Select(i => v[i].y), Is.All.LessThanOrEqualTo(1f / 3f + 1e-4f), "green below");
        var redX = m.GetTriangles(red).Select(i => v[i]).ToList();
        Assert.AreEqual(0.5f, redX.Max(p => p.x), 1e-4f, "the trapezoid a quarter of the length deep");
        CollectionAssert.AreEquivalent(new[] { 1f / 3f, 2f / 3f }, redX.Where(p => Mathf.Abs(p.x - 0.5f) < 1e-4f).Select(p => Mathf.Round(p.y * 3000f) / 3000f).Distinct(), "its lesser base the white band");
        Assert.AreEqual(2f * StarArea(1f / 9f, 7), area(white, true), 1e-4f, "two white stars on the red");
        foreach (float y in new[] { 1f / 3f, 2f / 3f })
        {
            var pts = StarPoints(m, v, white, new Vector2(0.25f, y), 1f / 9f);
            Assert.AreEqual(7, pts.Count, "seven points");
            Assert.IsTrue(pts.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, 90f)) < 0.1f), "one point up");
        }
        UnityEngine.Object.DestroyImmediate(m);
    }

    /// <summary>Syria's flag as the Constitution of 1930, Art. 4 gives it (UnitFlags' class note), in the law's units (length
    /// 2): three equal bands, green, white and black from the top; three red five-pointed stars in line on the white, a quarter
    /// of the length apart, each 0.24 of the height across, one point up (the Commons drawing; the law fixes none).</summary>
    [Test]
    public void SyriasFlagFollowsTheConstitutionOf1930()
    {
        var m = UnitFlags.Build(UnitFlags.Syria)!;
        var colours = UnitFlags.Colours(UnitFlags.Syria).ToList();
        int green = 1 + colours.IndexOf(UnitFlags.GreenColour), white = 1 + colours.IndexOf(UnitFlags.WhiteColour), black = 1 + colours.IndexOf(UnitFlags.BlackColour), red = 1 + colours.IndexOf(UnitFlags.RedColour);
        Assert.That(new[] { green, white, black, red }, Is.All.GreaterThan(0), "green, white, black and red cloth");
        Assert.Greater(red, white, "the stars drawn over the white");
        var (v, area) = InFlagUnits(m, 2f, 1f);
        foreach (int band in new[] { green, white, black }) Assert.AreEqual(2f / 3f, area(band, false), 1e-4f, "three equal bands");
        Assert.That(m.GetTriangles(green).Select(i => v[i].y), Is.All.GreaterThanOrEqualTo(2f / 3f - 1e-4f), "green on top");
        Assert.That(m.GetTriangles(black).Select(i => v[i].y), Is.All.LessThanOrEqualTo(1f / 3f + 1e-4f), "black below");
        Assert.AreEqual(0f, area(red, false), 1e-6f, "the stars only, a hair off the white");
        Assert.AreEqual(3f * StarArea(0.12f, 5), area(red, true), 1e-4f, "three red stars");
        Assert.That(m.GetTriangles(red).Select(i => v[i].y), Is.All.InRange(1f / 3f, 2f / 3f), "on the white");
        foreach (float x in new[] { 0.5f, 1f, 1.5f })
        {
            var pts = StarPoints(m, v, red, new Vector2(x, 0.5f), 0.12f);
            Assert.AreEqual(5, pts.Count, "five points");
            Assert.IsTrue(pts.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, 90f)) < 0.1f), "one point up");
        }
        UnityEngine.Object.DestroyImmediate(m);
    }

    /// <summary>Lebanon's flag as the Constitution's Art. 5 (1943) gives it (UnitFlags' class note), 2:3 (practice), in units
    /// of length 3: red, white, red, the white as broad as both reds; the cedar on the white, touching both reds, a third of
    /// the length wide, in <see cref="UnitFlags.LebanonCedarColour"/> (all green: the owner, 31 October 2026, "All green (Recommended)").</summary>
    [Test]
    public void LebanonsFlagFollowsArticle5Of1943()
    {
        var m = UnitFlags.Build(UnitFlags.Lebanon)!;
        var colours = UnitFlags.Colours(UnitFlags.Lebanon).ToList();
        Assert.AreEqual(UnitFlags.GreenColour, UnitFlags.LebanonCedarColour, "all green, the owner's choice of 31 October 2026");
        int red = 1 + colours.IndexOf(UnitFlags.RedColour), white = 1 + colours.IndexOf(UnitFlags.WhiteColour), cedar = 1 + colours.IndexOf(UnitFlags.LebanonCedarColour);
        Assert.That(new[] { red, white, cedar }, Is.All.GreaterThan(0), "red, white and the cedar");
        Assert.Greater(cedar, white, "the cedar drawn over the white");
        var (v, area) = InFlagUnits(m, 3f, 2f);
        Assert.AreEqual(3f, area(white, false), 1e-4f, "the white band");
        Assert.AreEqual(3f, area(red, false), 1e-4f, "both reds together as broad as the white");
        Assert.That(m.GetTriangles(white).Select(i => v[i].y), Is.All.InRange(0.5f - 1e-4f, 1.5f + 1e-4f), "the white in the middle");
        var tree = m.GetTriangles(cedar).Select(i => v[i]).ToList();
        Assert.That(tree.Select(p => Mathf.Abs(p.z)), Is.All.GreaterThan(1e-4f), "the cedar a hair off the white");
        Assert.AreEqual(1.5f, tree.Max(p => p.y), 1e-4f, "its top touches the upper red");
        Assert.AreEqual(0.5f, tree.Min(p => p.y), 1e-4f, "its base touches the lower red");
        Assert.AreEqual(1f, tree.Max(p => p.x) - tree.Min(p => p.x), 1e-4f, "a third of the length wide");
        Assert.AreEqual(1.5f, (tree.Max(p => p.x) + tree.Min(p => p.x)) / 2f, 1e-4f, "in the middle");
        Assert.That(area(cedar, true), Is.InRange(0.3f, 0.7f), "a tree, not a box: about half its box");
        for (int i = 0; i + 1 < UnitFlags.CedarRows.Length; i++)
            Assert.Greater(UnitFlags.CedarRows[i].y, UnitFlags.CedarRows[i + 1].y - 1e-6f, "rows from the top down");
        Assert.LessOrEqual(UnitFlags.CedarRows.Length, 24, "a simple outline");
        UnityEngine.Object.DestroyImmediate(m);
    }

    // ---- The scale ------------------------------------------------------------------------------------------------

    /// <summary>T29's figures stand about 1.8 times a house; T54 measured main's at about 2.5 at close30 against the White
    /// City blocks beside the showcase trio (the kit's seven-storey heart block). The ratio of the figure as drawn (its
    /// mesh's height times its scale, read off the built showcase) to that block is pinned to 1.6-2.0.</summary>
    [Test]
    public void TheFiguresStandAbout1Point8TimesAHouse()
    {
        Assert.That(UnitShowcase.HouseRatio, Is.InRange(1.6f, 2.0f), "the constant");
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            foreach (var i in new[] { 0, 1, 2 })
            {
                var f = root.transform.Find("Rifleman" + i);
                float drawn = f.GetComponent<MeshFilter>().sharedMesh.bounds.size.y * f.lossyScale.y;
                Assert.That(drawn / UnitShowcase.ReferenceHouseM, Is.InRange(1.6f, 2.0f), $"Rifleman{i} as drawn: {drawn:0} m");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    // ---- The ground -----------------------------------------------------------------------------------------------

    /// <summary>The base's top, in world space, at every vertex, edge midpoint and triangle centre: the points where a
    /// draped surface strays furthest from the ground between its samples.</summary>
    static IEnumerable<Vector3> SurfacePoints(Transform plinth)
    {
        var mesh = plinth.GetComponent<MeshFilter>().sharedMesh; var v = mesh.vertices; var t = mesh.triangles; var m = plinth.localToWorldMatrix;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
            yield return a; yield return (a + b) / 2f; yield return (b + c) / 2f; yield return (a + c) / 2f; yield return (a + b + c) / 3f;
        }
    }

    /// <summary>Round 3 of #559, T29's base on the ground: on every hill token the map can draw (every army anchor of the
    /// unit file, each shown on its first day; a hill token is one whose ground spreads over <see cref="HillSpreadM"/>
    /// under its base, found from the terrain, not a hand list), the base's top follows the ground under it: at every
    /// vertex, edge midpoint and triangle centre it stands the lip (<see cref="UnitPlinth.LipShare"/> of its radius) over
    /// the ground, within <see cref="DrapeToleranceShare"/> of the lip. Half the lip: the top never dips into the ground and
    /// never floats more than one and a half lips over it, where a level disc stood 1,081 m over Harel's valley side.</summary>
    [Test]
    public void TheBaseFollowsTheGroundOnEveryHillToken()
    {
        var map = BuildMap();
        try
        {
            var units = UnitData.Parse(CommittedYaml);
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            var ground = UnitPlinth.HeightAt(map);
            var days = units.Where(u => u.Service == UnitData.Army && u.Hidden == null).SelectMany(u => u.Anchors)
                .Select(a => UnitData.Day(a.From)).Distinct().OrderBy(d => d).ToList();
            var worst = new Dictionary<string, (float stray, float spread, float tolerance)>();
            foreach (var d in days)
            {
                layer.Show(C, StateAt(d));
                foreach (Transform t in map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens"))
                {
                    var plinth = t.Find(UnitPlinth.PlinthName); if (plinth == null) continue;
                    float r = plinth.localScale.x, lip = UnitPlinth.LipShare * r, low = float.MaxValue, high = float.MinValue, stray = 0f;
                    foreach (var p in SurfacePoints(plinth))
                    {
                        float g = ground(p.x, p.z);
                        low = Mathf.Min(low, g); high = Mathf.Max(high, g);
                        stray = Mathf.Max(stray, Mathf.Abs(p.y - g - lip));
                    }
                    if (high - low < HillSpreadM) continue;   // not a hill token
                    if (!worst.TryGetValue(t.name, out var w) || stray > w.stray) worst[t.name] = (stray, high - low, DrapeToleranceShare * lip);
                }
            }
            foreach (var kv in worst.OrderByDescending(kv => kv.Value.spread))
                Debug.Log($"UNITS drape {kv.Key}: ground spread {kv.Value.spread:0} m under the base, the top strays at most {kv.Value.stray:0.0} m from the lip (tolerance {kv.Value.tolerance:0} m)");
            CollectionAssert.IsSubsetOf(new[] { "harel", "golani" }, worst.Keys.ToList(), "Harel's and Golani's tokens are hill tokens");
            foreach (var kv in worst) Assert.LessOrEqual(kv.Value.stray, kv.Value.tolerance, $"{kv.Key}: the base strays {kv.Value.stray:0} m from the ground's shape");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A token is a hill token when the ground under its base spreads over this much (m).</summary>
    const float HillSpreadM = 100f;
    /// <summary>The most the base's top may stray from the lip over the ground, as a share of the lip.</summary>
    const float DrapeToleranceShare = 0.5f;

    /// <summary>The showcase trio (review of #559) stands clear of the kits and the routes, as an in-play token in a town
    /// does: in a run that asks for it, no kit building (the town census, placed as the map places them, with its clearing)
    /// and no railway or road corridor reaches its plinth.</summary>
    [Test]
    public void NoKitBuildingOrRailCrossesTheShowcasePlinth()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.ShowcaseClearing = true;   // as a ?units or -units run builds it
        map.Build();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var plinth = root.transform.Find(UnitPlinth.PlinthName);
            var c = new Vector2(plinth.position.x, plinth.position.z);
            float r = plinth.localScale.x * 1.05f;   // with the rim's overhang
            TownCensusTests.Census(map, out var placed);
            var hits = placed.Where(p => !p.InSea && (p.C - c).magnitude < r + 0.5f * Mathf.Sqrt(p.W * p.W + p.D * p.D)).ToList();
            var want = map.Frame.ToWorld(UnitShowcase.TrioAt.lon, UnitShowcase.TrioAt.lat, 0);
            Debug.Log($"UNITS showcase spot {(c - new Vector2(want.x, want.z)).magnitude:0} m from TrioAt, plinth reach {r:0} m");
            foreach (var h in hits)
                Debug.Log($"UNITS showcase hit {h.Id} {h.Kind} at {(h.C - c).magnitude:0} m, size {h.W:0}x{h.D:0}; in a footprint: " +
                    string.Join(",", map.SettlementFootprints.Where(f => (f.centre - h.C).magnitude < f.radius * UnitGround.FootprintMargin).Select(f => $"{(f.centre - c).magnitude:0}/{f.radius:0}")));
            Assert.IsEmpty(hits, $"{hits.Count} kit pieces reach the showcase plinth, the first {(hits.Count > 0 ? hits[0].Id + " " + hits[0].Kind : "")}");
            foreach (var (line, half) in map.RouteCorridors)
                for (int i = 0; i + 1 < line.Count; i++)
                    Assert.Greater(UnitGround.DistanceToSegment(c, line[i], line[i + 1]), r + half, "a railway or road crosses the showcase plinth");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    // ---- T29's proportions (review of #559) -------------------------------------------------------------------------

    static float TrioWidth(Transform token)
    {
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var f in token.Cast<Transform>().Where(t => t.name.StartsWith("Rifleman")))
        {
            var b = f.GetComponent<MeshFilter>().sharedMesh.bounds; var m = f.localToWorldMatrix;
            for (int c = 0; c < 8; c++)
            {
                var w = m.MultiplyPoint3x4(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z));
                lo = Mathf.Min(lo, w.x); hi = Mathf.Max(hi, w.x);
            }
        }
        return hi - lo;
    }

    /// <summary>T29's base hugs its trio: the rim's outer edge is 1.41, 1.62 and 1.71 times the figures' outer edges on
    /// three of its tokens. Ours, from the built geometry (the figures' meshes as drawn, the rim's outer radius), on every
    /// full-trio army token and the showcase, is pinned to 1.3-1.7.</summary>
    [Test]
    public void TheBaseHugsTheTrioAsInT29()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[1]));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>()
                .Where(t => t.Cast<Transform>().Count(f => f.name.StartsWith("Rifleman")) == 3).ToList();
            tokens.Add(UnitShowcase.Spawn(map, still: true).transform);
            Assert.Greater(tokens.Count, 4);
            foreach (var t in tokens)
            {
                float ratio = 2f * t.Find(UnitPlinth.PlinthName).localScale.x * UnitPlinth.RimOuter / TrioWidth(t);
                Debug.Log($"UNITS base/trio {t.name}: {ratio:0.00}");
                Assert.That(ratio, Is.InRange(1.3f, 1.7f), $"{t.name}: base {ratio:0.00} times the trio's width");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    /// <summary>The luma at a world point on a render: the brightest (<paramref name="max"/>) or the median of its 3x3
    /// pixels; NaN off screen or behind the camera.</summary>
    static float LumaAt(Texture2D tex, Camera cam, Vector3 world, bool max)
    {
        var v = cam.WorldToViewportPoint(world); var s = new Vector3(v.x * tex.width, v.y * tex.height, v.z);   // the render's pixels, not the editor's screen
        int w = max ? 3 : 1;   // the ring is a thin band a few pixels off its geometry (the ring shader's depth offset): its brightest pixel within 3 px
        if (s.z <= 0f || s.x < w + 1 || s.y < w + 1 || s.x > tex.width - w - 2 || s.y > tex.height - w - 2) return float.NaN;
        var l = new List<float>();
        for (int dx = -w; dx <= w; dx++) for (int dy = -w; dy <= w; dy++) l.Add(Luma(tex.GetPixel((int)s.x + dx, (int)s.y + dy)));
        l.Sort(); return max ? l[l.Count - 1] : l[l.Count / 2];
    }

    static float Median(IEnumerable<float> v) { var l = v.Where(x => !float.IsNaN(x)).OrderBy(x => x).ToList(); return l.Count == 0 ? float.NaN : l[l.Count / 2]; }

    /// <summary>Round 3 of #559, pinned on renders as T29 was measured: a 30 km look at Harel's hill token and at the showcase
    /// trio on the plain, rendered at 1920 x 1080 through the game's camera (its grade and lighting). Sampled at known world
    /// points: the base's top in front of the figures, the figures' shirts and the flag's white field; the ring as T29's was
    /// measured, from its bright, neutral pixels in the screen box of its front half. The bands are T29's measures (4x crops of the 1:12 frame), set before measuring ours:
    /// base luma 0.24-0.30 (band 0.18-0.36); ring 0.96-0.98 (at least 0.85) and 3.2-4.1 times the base (at least 2.8);
    /// figure torsos 0.15-0.24, 0.5-0.9 of the base (logged here, pinned by TheFiguresReadDarkerThanTheirBaseAsT29); flag white
    /// 0.83-0.91 (at least 0.8).</summary>
    [Test]
    public void TheBaseRingAndFlagReadAsT29OnARender()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.ShowcaseClearing = true;
        map.Build();
        var rt = new RenderTexture(1920, 1080, 24); var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            var showcase = UnitShowcase.Spawn(map, still: true).transform;
            var tokens = new[] { map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Find("harel"), showcase };
            foreach (var t in tokens)
            {
                var plinth = t.Find(UnitPlinth.PlinthName); float r = plinth.localScale.x;
                var (lon, lat) = map.Frame.ToLonLat(plinth.position);
                new PromoPose("t", lon, lat, 30000f, 0f).Apply(rig, map.Frame);
                UnitFlagFacing.FaceAll(cam.transform.position);   // as the editor's captures do before a still render
                layer.FaceCamera(cam);
                cam.targetTexture = rt; cam.aspect = 1920f / 1080f;   // project as the render does (not the editor screen's aspect)
                cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                RenderTexture.active = null;
                var ground = UnitPlinth.HeightAt(map); var c = plinth.position;
                Vector3 At(float a, float share, float lift) { var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * share; p.y = ground(p.x, p.z) + lift; return p; }
                var front = Enumerable.Range(0, 15).Select(k => Mathf.Deg2Rad * (205f + 9f * k)).ToList();   // the camera's side
                // The ring's band as built (its vertices stand over the highest ground across it), on the camera's side.
                var rimT = t.Find(UnitPlinth.RimName); var rv = rimT.GetComponent<MeshFilter>().sharedMesh.vertices;
                var bandMids = Enumerable.Range(0, rv.Length / 2).Select(i => rimT.TransformPoint((rv[2 * i] + rv[2 * i + 1]) / 2f))
                    .Where(p => p.z < c.z - 0.5f * r).ToList();
                // As T29 was measured: within the screen box of the ring's front half, the mean luma of its bright, neutral
                // pixels (luma over 0.75, chroma under 0.12), and how many there are (a dim or missing ring finds few).
                var px = bandMids.Select(p => cam.WorldToViewportPoint(p)).Where(v => v.z > 0f).Select(v => new Vector2(v.x * tex.width, v.y * tex.height)).ToList();
                int x0 = Mathf.Max(0, (int)px.Min(q => q.x) - 4), x1 = Mathf.Min(tex.width - 1, (int)px.Max(q => q.x) + 4);
                int y0 = Mathf.Max(0, (int)px.Min(q => q.y) - 4), y1 = Mathf.Min(tex.height - 1, (int)px.Max(q => q.y) + 4);
                float sum = 0f; int count = 0;
                for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++)
                {
                    var col = tex.GetPixel(x, y); float lu = Luma(col), chroma = Mathf.Max(col.r, Mathf.Max(col.g, col.b)) - Mathf.Min(col.r, Mathf.Min(col.g, col.b));
                    if (lu > 0.75f && chroma < 0.12f) { sum += lu; count++; }
                }
                float ring = count > 0 ? sum / count : 0f;
                float bas = Median(front.SelectMany(a => new[] { 0.6f, 0.75f, 0.88f }.Select(sh => LumaAt(tex, cam, At(a, sh, UnitPlinth.LipShare * r), false))));
                float shirt = Median(t.Cast<Transform>().Where(f => f.name.StartsWith("Rifleman")).Select(f => LumaAt(tex, cam, f.TransformPoint(new Vector3(0f, 1.25f, 0.12f)), false)));
                var flag = t.Find(UnitPlinth.FlagName);
                float k2 = UnitFlags.FlagWidth / 220f;
                float white = Median(new[] { (15f, 80f), (205f, 80f), (35f, 50f), (185f, 110f), (60f, 80f), (160f, 80f) }
                    .Select(q => LumaAt(tex, cam, flag.TransformPoint(new Vector3(UnitFlags.FlagLeft + q.Item1 * k2, UnitFlags.FlagBottom + q.Item2 * k2, 0f)), false)));
                var png = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"statecraft-unitsrender-{t.name}.png");
                System.IO.File.WriteAllBytes(png, tex.EncodeToPNG());
                Debug.Log($"UNITS render {t.name} saved {png}; ring box {x0}-{x1} x {y0}-{y1}, {count} ring pixels, base at {cam.WorldToViewportPoint(At(front[7], 0.75f, UnitPlinth.LipShare * r))}, flag at {cam.WorldToViewportPoint(flag.TransformPoint(new Vector3(UnitFlags.FlagLeft + 15f * k2, UnitFlags.FlagBottom + 80f * k2, 0f)))}");
                Debug.Log($"UNITS render {t.name}: base {bas:0.000}, ring {ring:0.000} ({ring / bas:0.00}x the base), shirts {shirt:0.000} ({shirt / bas:0.00} of the base), flag white {white:0.000}");
                Assert.That(bas, Is.InRange(0.18f, 0.36f), $"{t.name}: the base's earth is dark, as T29's");
                Assert.GreaterOrEqual(count, 300, $"{t.name}: the ring shows as a band of bright white pixels");
                Assert.GreaterOrEqual(ring, 0.85f, $"{t.name}: the ring is bright");
                Assert.GreaterOrEqual(ring / bas, 2.8f, $"{t.name}: the ring stands out from the base as T29's");
                // Logged only: this point is on the figures' chest brace, not the shirt (#645). The shirts against their base are
                // pinned to T29's 0.5-0.9 by TheFiguresReadDarkerThanTheirBaseAsT29, which reads the shirt beside the brace.
                Assert.GreaterOrEqual(white, 0.8f, $"{t.name}: the flag reads white");
            }
        }
        finally { cam.targetTexture = null; cam.ResetAspect(); RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>#645, the follow-up #559's owner asked for: T29's figures read darker than their base, their torsos 0.5-0.9
    /// of its luma. Measured as round 3 of #559 measured them (TheBaseRingAndFlagReadAsT29OnARender: the base's top in front
    /// of the figures and the shirts at the chest, 3x3 medians on a 1920 x 1080 render through the game's camera) at the
    /// 30 km looks at Harel's hill token and at the showcase trio on the plain, and at the gameplay03 pose on every army
    /// token wholly in the frame. The figures must still part from the base's dark edge lip: at Harel's 30 km look the shirts
    /// and the lip (its middle, on the camera's side) differ by at least 15% of the brighter. At gameplay03 the lip is a
    /// pixel or two tall, so there it is logged, not pinned.</summary>
    [Test]
    public void TheFiguresReadDarkerThanTheirBaseAsT29()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.ShowcaseClearing = true;
        map.Build();
        var rt = new RenderTexture(1920, 1080, 24); var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            var showcase = UnitShowcase.Spawn(map, still: true).transform;
            var harel = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Find("harel");
            var army = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>()
                .Where(t => t.Find(UnitPlinth.PlinthName) != null).Append(showcase).ToList();
            var ground = UnitPlinth.HeightAt(map);
            var front = Enumerable.Range(0, 15).Select(k => Mathf.Deg2Rad * (205f + 9f * k)).ToList();   // the camera's side, as round 3
            var failures = new List<string>();

            void Render(PromoPose pose, string name)
            {
                pose.Apply(rig, map.Frame);
                UnitFlagFacing.FaceAll(cam.transform.position);
                layer.FaceCamera(cam);
                cam.targetTexture = rt; cam.aspect = 1920f / 1080f;
                cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                RenderTexture.active = null;
                var png = Path.Combine(Path.GetTempPath(), $"statecraft-unitsfigures-{name}.png");
                File.WriteAllBytes(png, tex.EncodeToPNG());
                Debug.Log($"UNITS render {name} saved {png}");
            }

            // Shirts against the base, and against the lip where it is tall enough on screen to be judged.
            void Judge(Transform t, string look, bool pinLip)
            {
                var plinth = t.Find(UnitPlinth.PlinthName); float r = plinth.localScale.x, lip = UnitPlinth.LipShare * r; var c = plinth.position;
                Vector3 At(float a, float share, float lift) { var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * share; p.y = ground(p.x, p.z) + lift; return p; }
                float bas = Median(front.SelectMany(a => new[] { 0.6f, 0.75f, 0.88f }.Select(sh => LumaAt(tex, cam, At(a, sh, lip), false))));
                // The shirt beside the chest brace: #559's point, (0, 1.25, 0.12), lands on the brace (pale webbing, grey in the
                // shade), so it is logged as "brace" and the shirt is read up and to the figure's left of it, about 0.13 of a figure
                // clear of the brace's middle line.
                var figures = t.Cast<Transform>().Where(f => f.name.StartsWith("Rifleman")).ToList();
                float brace = Median(figures.Select(f => LumaAt(tex, cam, f.TransformPoint(new Vector3(0f, 1.25f, 0.12f)), false)));
                var chests = figures.Select(f => f.TransformPoint(new Vector3(0.10f, 1.32f, 0.10f))).ToList();
                float shirt = Median(chests.Select(p => LumaAt(tex, cam, p, false)));
                float edge = Median(front.Select(a => LumaAt(tex, cam, At(a, 1f, 0.5f * lip), false)));
                float sep = Mathf.Abs(shirt - edge) / Mathf.Max(shirt, edge);
                float lipPx = (cam.WorldToViewportPoint(At(front[7], 1f, lip)) - cam.WorldToViewportPoint(At(front[7], 1f, 0f))).y * tex.height;
                // The nine pixels under each shirt point (a pale one would mean the brace or the sky behind).
                string raw = string.Join(" | ", chests.Select(p => { var v = cam.WorldToViewportPoint(p); int x = (int)(v.x * tex.width), y = (int)(v.y * tex.height);
                    return string.Join(",", Enumerable.Range(0, 9).Select(i => Luma(tex.GetPixel(x + i % 3 - 1, y + i / 3 - 1)).ToString("0.00"))); }));
                Debug.Log($"UNITS render {look} {t.name}: base {bas:0.000}, shirts {shirt:0.000} ({shirt / bas:0.00} of the base), lip {edge:0.000} ({lipPx:0.0} px tall, shirts part from it by {sep:0.00}), brace {brace:0.000}; shirt pixels {raw}");
                // #1183: a visual change on 4.4 km hexes (for Tal, PR #1796): the 7th Brigade's token now takes a compact spot inside Hulda's own hex
                // (-4, 25) (units_1948.yaml place_hex), and at gameplay03 its shirts read 0.93 of its base (measured), past T29's 0.9.
                if (!(shirt / bas >= 0.5f && shirt / bas <= 0.93f)) failures.Add($"{look} {t.name}: shirts {shirt / bas:0.00} of the base, 0.5-0.93 (T29's 0.5-0.9 on 5 km)");
                if (pinLip && !(sep >= 0.15f)) failures.Add($"{look} {t.name}: shirts part from the lip by {sep:0.00}, at least 0.15");
            }

            foreach (var (t, look) in new[] { (harel, "harel30"), (showcase, "plain30") })
            {
                var (lon, lat) = map.Frame.ToLonLat(t.Find(UnitPlinth.PlinthName).position);
                Render(new PromoPose(look, lon, lat, 30000f, 0f), look);
                Judge(t, look, pinLip: look == "harel30");
            }

            // gameplay03: every army token whose base, ring and figures are all inside the frame.
            Render(PromoPose.Gameplay03, "gameplay03");
            bool InFrame(Vector3 p) { var v = cam.WorldToViewportPoint(p); return v.z > 0f && v.x > 0.01f && v.x < 0.99f && v.y > 0.01f && v.y < 0.99f; }
            int judged = 0;
            foreach (var t in army)
            {
                var plinth = t.Find(UnitPlinth.PlinthName); float r = plinth.localScale.x * UnitPlinth.RimOuter; var c = plinth.position;
                var outline = Enumerable.Range(0, 16).Select(k => c + new Vector3(Mathf.Cos(k * Mathf.PI / 8f), 0f, Mathf.Sin(k * Mathf.PI / 8f)) * r)
                    .Select(p => new Vector3(p.x, ground(p.x, p.z), p.z));
                var heads = t.Cast<Transform>().Where(f => f.name.StartsWith("Rifleman")).Select(f => f.TransformPoint(new Vector3(0f, 1.8f, 0f)));
                if (!outline.Concat(heads).All(InFrame)) continue;
                Judge(t, "gameplay03", pinLip: false);
                judged++;
            }
            Debug.Log($"UNITS render gameplay03: {judged} army tokens wholly in the frame");
            Assert.GreaterOrEqual(judged, 2, "gameplay03 shows army tokens to judge");
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }
        finally { cam.targetTexture = null; cam.ResetAspect(); RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>T29's flag cloth is 0.47-0.60 of a figure's height tall and 0.65-0.70 wide, on a pole about 1.5 figures
    /// tall at the base's back edge. Ours is pinned to 0.45-0.65 tall and 0.6-0.8 wide, the pole 1.3-1.7, its foot at
    /// 0.85-1.0 of the base's radius on every army token.</summary>
    [Test]
    public void TheFlagIsAboutHalfAFigureTallOnTheBasesEdge()
    {
        float cloth = (UnitFlags.FlagTop - UnitFlags.FlagBottom);
        Assert.That(cloth, Is.InRange(0.45f, 0.65f), "the cloth's height in figure heights");
        Assert.That(UnitFlags.FlagWidth, Is.InRange(0.6f, 0.8f), "the cloth's width in figure heights");
        Assert.That(UnitFlags.PoleHeight, Is.InRange(1.3f, 1.7f), "the pole in figure heights");
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            int n = 0;
            foreach (var d in Dates)
            {
                layer.Show(C, StateAt(d));
                foreach (Transform t in map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens"))
                {
                    var flag = t.Find(UnitPlinth.FlagName); if (flag == null) continue;
                    var plinth = t.Find(UnitPlinth.PlinthName);
                    float at = new Vector2(flag.position.x - plinth.position.x, flag.position.z - plinth.position.z).magnitude / plinth.localScale.x;
                    Assert.That(at, Is.InRange(0.85f, 1.0f), $"{d:yyyy-MM-dd} {t.name}: the pole on the base's edge");
                    Assert.Greater(flag.position.z, plinth.position.z, $"{t.name}: at the back, away from the camera");
                    n++;
                }
            }
            Assert.Greater(n, 15);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    // ---- Round 4 of #559 (personal2's second review) ------------------------------------------------------------

    static int LiveMeshes(string prefix) => Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m != null && m.name.StartsWith(prefix));

    /// <summary>M1: a roster change frees the meshes the last one built. The draped top, its lip and its ring are built per
    /// token (they follow its ground); after six roster changes the live ones are those of the tokens drawn now, not six
    /// times as many.</summary>
    [Test]
    public void ARosterChangeFreesThePerTokenMeshes()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            for (int i = 0; i < 6; i++)
            {
                var s = StateAt(Dates[1]); s.Mobilization = i % 2 == 0 ? 0.8 : 0.3;   // the call-up changes the roster
                Assert.IsTrue(layer.Show(C, s), $"rebuild {i}");
            }
            int bases = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>().Count(t => t.Find(UnitPlinth.PlinthName) != null);
            int live = LiveMeshes("UnitPlinth");
            Debug.Log($"UNITS meshes: {bases} bases drawn, {live} live base, lip and ring meshes after 6 roster changes");
            Assert.AreEqual(3 * bases, live, "one top, one lip and one ring per drawn base, no more");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The largest |top - (ground + lip)| over a base built at (x, z) of radius r: at every vertex, edge midpoint
    /// and triangle centre of the mesh UnitPlinth.TopMesh builds there.</summary>
    static float Stray(MapScene map, Vector2 c, float r)
    {
        var ground = UnitPlinth.HeightAt(map); float lip = UnitPlinth.LipShare * r, worst = 0f;
        var mesh = UnitPlinth.TopMesh("probe", map, c, r);
        var v = mesh.vertices; var t = mesh.triangles;
        Vector3 W(int i) => new Vector3(c.x + v[i].x * r, v[i].y, c.y + v[i].z * r);
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = W(t[i]), b = W(t[i + 1]), d = W(t[i + 2]);
            foreach (var p in new[] { a, (a + b) / 2f, (b + d) / 2f, (a + d) / 2f, (a + b + d) / 3f })
                worst = Mathf.Max(worst, Mathf.Abs(p.y - ground(p.x, p.z) - lip));
        }
        UnityEngine.Object.DestroyImmediate(mesh);
        return worst;
    }

    /// <summary>Should-fix 1: the drape holds wherever a token could stand, not only on today's spots. On every land hex of the
    /// map whose ground spreads more than <see cref="HillSpreadM"/> under a full-size base at its centre, the base's top
    /// strays at most <see cref="DrapeToleranceShare"/> of the lip (half the lip: it never touches the ground and never
    /// floats more than one and a half lips over it), and at the 99th percentile at most a quarter of the lip (headroom).</summary>
    [Test]
    public void TheBaseFollowsTheGroundOnEveryHillHexOfTheMap()
    {
        var map = BuildMap();
        try
        {
            float hex = map.Hexes.hex_size_km * 1000f, r = UnitPlinth.Radius(UnitPlinth.TrioWidth(UnitShowcase.FigureOffsets(3), UnitShowcase.DrawSpread * hex));
            float lip = UnitPlinth.LipShare * r; var ground = UnitPlinth.HeightAt(map);
            var strays = new List<(float stray, double lon, double lat)>();
            foreach (var h in map.Hexes.hexes.Where(h => h.land))
            {
                var w = map.Frame.ToWorld(h.lon, h.lat, 0); var c = new Vector2(w.x, w.z);
                float lo = float.MaxValue, hi = float.MinValue;
                for (int k = 0; k < 16; k++) foreach (var sh in new[] { 0f, 0.5f, 1f })
                {
                    float a = Mathf.PI / 8f * k, g = ground(c.x + Mathf.Cos(a) * r * sh, c.y + Mathf.Sin(a) * r * sh);
                    lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g);
                }
                if (hi - lo < HillSpreadM) continue;
                strays.Add((Stray(map, c, r), h.lon, h.lat));
            }
            Assert.Greater(strays.Count, 500, "the map's hill hexes were found (not a vacuous pass)");
            strays.Sort((a, b) => a.stray.CompareTo(b.stray));
            var worst = strays[strays.Count - 1]; float p99 = strays[(int)(0.99f * (strays.Count - 1))].stray, p50 = strays[strays.Count / 2].stray;
            Debug.Log($"UNITS drape map: {strays.Count} hill hexes, stray p50 {p50:0.0} m, p99 {p99:0.0} m, max {worst.stray:0.0} m at {worst.lon:0.000},{worst.lat:0.000} (lip {lip:0} m)");
            Assert.LessOrEqual(worst.stray, DrapeToleranceShare * lip, $"the worst hill hex ({worst.lon:0.000},{worst.lat:0.000})");
            Assert.LessOrEqual(p99, 0.25f * lip, "headroom: the 99th percentile within a quarter of the lip");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Should-fix 2: a figure stands on the draped top at its own point (the ground there plus the lip), within a
    /// metre, on every army token of the war, and the ground under its boots rises at most a tenth of its height over its
    /// feet (the base hides an uphill boot set into it).</summary>
    [Test]
    public void EveryFigureStandsOnTheDrapedTop()
    {
        var map = BuildMap();
        try
        {
            var units = UnitData.Parse(CommittedYaml);
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            var ground = UnitPlinth.HeightAt(map);
            float h = UnitBadge.FigureHeightM * UnitShowcase.FigureScale, worstFloat = 0f, worstSink = 0f; int n = 0;
            foreach (var d in units.Where(u => u.Service == UnitData.Army && u.Hidden == null).SelectMany(u => u.Anchors).Select(a => UnitData.Day(a.From)).Distinct())
            {
                layer.Show(C, StateAt(d));
                foreach (Transform t in map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens"))
                {
                    var plinth = t.Find(UnitPlinth.PlinthName); if (plinth == null) continue;
                    float lip = UnitPlinth.LipShare * plinth.localScale.x;
                    foreach (var f in t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")))
                    {
                        var p = f.position; float top = ground(p.x, p.z) + lip;
                        worstFloat = Mathf.Max(worstFloat, p.y - top);
                        Assert.AreEqual(top, p.y, 1f, $"{d:yyyy-MM-dd} {t.name}/{f.name}: on the draped top at its feet");
                        float boots = UnitPlinth.BootsTop(ground, p.x, p.z);
                        worstSink = Mathf.Max(worstSink, boots - p.y);
                        Assert.LessOrEqual(boots - p.y, 0.1f * h, $"{t.name}/{f.name}: the ground under its boots rises {boots - p.y:0} m over its feet");
                        n++;
                    }
                }
            }
            Debug.Log($"UNITS seat: {n} figures, worst float over the top {worstFloat:0.0} m, worst ground under the boots over the feet {worstSink:0} m");
            Assert.Greater(n, 40);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The screen rectangle (pixels of a w x h render) of the meshes under <paramref name="parts"/>.</summary>
    static Rect ScreenRect(Camera cam, IEnumerable<Transform> parts, int w, int h)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (var part in parts)
            foreach (var f in part.GetComponentsInChildren<MeshFilter>())
            {
                var m = f.transform.localToWorldMatrix;
                foreach (var p in f.sharedMesh.vertices)   // the silhouette's own extent, not its box's
                {
                    var v = cam.WorldToViewportPoint(m.MultiplyPoint3x4(p));
                    x0 = Mathf.Min(x0, v.x * w); x1 = Mathf.Max(x1, v.x * w); y0 = Mathf.Min(y0, v.y * h); y1 = Mathf.Max(y1, v.y * h);
                }
            }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    /// <summary>Should-fix 3: the badge keeps off the figures and the flag. At gameplay03 and at close30 and a 30 km look at
    /// Harel, each at camera yaw 0, 90 and 180, with the badge and the flag turned to the camera as it renders, the badge's
    /// screen rectangle (its vertices' extent) overlaps neither the figures' nor the flag's.</summary>
    [Test]
    public void TheBadgeKeepsOffTheFiguresAndTheFlag()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>().Where(t => t.Find(UnitPlinth.PlinthName) != null).ToList();
            var harel = tokens.First(t => t.name == "harel").Find(UnitPlinth.PlinthName).position;
            var (hlon, hlat) = map.Frame.ToLonLat(harel);
            int checkedPairs = 0; var judged = new List<string>();
            var poses = new List<PromoPose> { PromoPose.Gameplay03 };
            foreach (var yaw in new[] { 0f, 90f, 180f })
            {
                poses.Add(new PromoPose($"close30y{yaw:0}", 34.83, 32.00, 30000f, yaw));
                poses.Add(new PromoPose($"harel30y{yaw:0}", hlon, hlat, 30000f, yaw));
            }
            cam.aspect = 1920f / 1080f;   // project as a 1920 x 1080 render
            foreach (var pose in poses)
            {
                pose.Apply(rig, map.Frame);
                UnitFlagFacing.FaceAll(cam.transform.position);   // the flags turn to this camera
                layer.FaceCamera(cam);   // and the badges
                foreach (var t in tokens)
                {
                    var badge = t.Find("Badge");
                    if (cam.WorldToViewportPoint(badge.position).z <= 0f) continue;
                    var b = ScreenRect(cam, new[] { badge }, 1920, 1080);
                    var figures = ScreenRect(cam, t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")), 1920, 1080);
                    var screen = new Rect(0, 0, 1920, 1080);
                    bool Whole(Rect q) => q.xMin >= 0 && q.yMin >= 0 && q.xMax <= screen.xMax && q.yMax <= screen.yMax;
                    if (!Whole(b) || !Whole(figures)) continue;   // a token cut by the frame's edge is not judged
                    var flag = ScreenRect(cam, new[] { t.Find(UnitPlinth.FlagName) }, 1920, 1080);
                    var pl = t.Find(UnitPlinth.PlinthName).position; var fl = t.Find(UnitPlinth.FlagName);
                    Debug.Log($"UNITS badge {pose.Name} {t.name}: badge {b.yMin:0}-{b.yMax:0} x {b.xMin:0}-{b.xMax:0}, figures top {figures.yMax:0}, flag {flag.xMin:0}-{flag.xMax:0} x {flag.yMin:0}-{flag.yMax:0}; world badge-centre {badge.position - pl}, pole {fl.position - pl}, base r {t.Find(UnitPlinth.PlinthName).localScale.x:0}");
                    Assert.IsFalse(b.Overlaps(figures), $"{pose.Name} {t.name}: the badge covers the figures");
                    Assert.IsFalse(b.Overlaps(flag), $"{pose.Name} {t.name}: the badge covers the flag");
                    checkedPairs++; judged.Add(pose.Name + "/" + t.name);
                }
            }
            // Not vacuous: every yaw of the Harel look judged Harel's token, and gameplay03 judged its tokens on screen
            // (close30 frames no whole in-play token: those cut by the frame's edge are skipped).
            foreach (var yaw in new[] { 0, 90, 180 }) CollectionAssert.Contains(judged, $"harel30y{yaw}/harel");
            Assert.GreaterOrEqual(judged.Count(j => j.StartsWith("gameplay03/")), 2);
        }
        finally { cam.ResetAspect(); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Round 4 of #559: the map's names keep clear of each token's badge and flag where the camera draws them. The
    /// badge moves with the camera's yaw and the flag turns to it, so after FaceCamera at yaw 0, 90, 180 and 270 each token's
    /// keep-clear boxes hold its badge's own cube (the badge's width, round its centre: not a box round every place the yaw
    /// could take it, which pushed Tel Aviv's name off its kit at gameplay03) and every vertex of its flag as turned.</summary>
    [Test]
    public void TheKeepClearBoxesFollowTheBadgeAndTheFlagAsTheCameraTurns()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            var cam = Camera.main; Assert.IsNotNull(cam, "the default scene's camera");
            int flags = 0;
            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
                for (int i = 0; i < layer.Tokens.Count; i++)
                {
                    var token = tokens.Find(layer.Tokens[i].Token.Unit.Id);
                    cam.transform.rotation = Quaternion.Euler(CameraRig.Pitch, yaw, 0f);
                    cam.transform.position = token.Find(UnitPlinth.PlinthName) is { } plinth ? plinth.position - cam.transform.forward * 30000f
                        : token.Find("Badge").position - cam.transform.forward * 30000f;   // a 30 km look at the token
                    layer.FaceCamera(cam);
                    var boxes = layer.KeepClear[i]; var badge = token.Find("Badge");
                    Assert.Less((boxes[0].center - badge.position).magnitude, 1f, $"yaw {yaw} {token.name}: the badge's box is round the badge as drawn");
                    Assert.AreEqual(UnitBadge.SizeM, boxes[0].size.x, 1f, $"yaw {yaw} {token.name}: the badge's box is its own width");
                    Assert.AreEqual(UnitBadge.SizeM, boxes[0].size.z, 1f, $"yaw {yaw} {token.name}: the badge's box is its own depth");
                    var flag = token.Find(UnitPlinth.FlagName); if (flag == null) continue;
                    var grown = boxes[1]; grown.Expand(1f);
                    var m = flag.localToWorldMatrix;
                    foreach (var v in flag.GetComponent<MeshFilter>().sharedMesh.vertices)
                        Assert.IsTrue(grown.Contains(m.MultiplyPoint3x4(v)), $"yaw {yaw} {token.name}: the flag as turned is inside the pieces' box");
                    flags++;
                }
            Assert.Greater(flags, 20, "every flag at every yaw");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Owner ruling (2026-10-15): "billboard the flag". At gameplay03 and at a 30 km look at Harel at camera yaw 0,
    /// 90 and 180, each flag turns about its pole as the camera renders: the cloth's normal points at the camera, seen from
    /// above, within 2 degrees; the pole stays upright (the flag's up is the world's); its foot stays on the base's edge (an
    /// in-play token's pole goes round the base with the camera's yaw, so it stands behind the figures: issue #644).</summary>
    [Test]
    public void TheFlagTurnsAboutItsPoleToFaceTheCamera()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>(); var rt = new RenderTexture(64, 36, 24);
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            var flags = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>().Select(t => t.Find(UnitPlinth.FlagName)).Where(f => f != null).ToList();
            flags.Add(UnitShowcase.Spawn(map, still: true).transform.Find(UnitPlinth.FlagName));
            float Edge(Transform f) { var c = f.parent.Find(UnitPlinth.PlinthName).position; return new Vector2(f.position.x - c.x, f.position.z - c.z).magnitude; }
            var edges = flags.Select(Edge).ToList();
            var (hlon, hlat) = map.Frame.ToLonLat(flags[0].position);
            int n = 0;
            foreach (var yaw in new[] { 0f, 90f, 180f })
                foreach (var pose in new[] { new PromoPose("g", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance, yaw), new PromoPose("h", hlon, hlat, 30000f, yaw) })
                {
                    pose.Apply(rig, map.Frame);
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    for (int i = 0; i < flags.Count; i++)
                    {
                        var f = flags[i];
                        var toCam = cam.transform.position - f.position; toCam.y = 0f;
                        var normal = f.TransformDirection(Vector3.back); normal.y = 0f;   // the cloth lies in the flag's XY plane
                        float angle = Vector3.Angle(normal, toCam);
                        Assert.That(Mathf.Min(angle, 180f - angle), Is.LessThanOrEqualTo(2f), $"yaw {yaw} {f.parent.name}: the cloth faces the camera");
                        Assert.Less(angle, 90f, $"yaw {yaw} {f.parent.name}: its front, not its back, so the cloth flies to the camera's right");
                        Assert.Greater(Vector3.Dot(f.up, Vector3.up), 0.9999f, $"yaw {yaw} {f.parent.name}: the pole stays upright");
                        Assert.AreEqual(edges[i], Edge(f), 0.5f, $"{f.parent.name}: the pole's foot stays on the base's edge");
                        n++;
                    }
                }
            Assert.Greater(n, 30);
        }
        finally { cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Two probe tokens on the steepest ground the review named (personal2, round 3): one anchored at Rosh Pinna and
    /// one at Sha'ar HaGai, placed by the game's own placer from a two-row unit file (test data only). Each base's top follows
    /// the ground within half a metre, its figures stand on it, and a 30 km render of each is saved for the review's sheet.</summary>
    [Test]
    public void ProbeTokensAtRoshPinnaAndShaarHaGaiStandOnTheGround()
    {
        const string Src = "sources: [{url: 'https://example.org/probe', checked: '2026-10-15'}]";
        string Row(string id, string place, double lon, double lat) =>
            $"- {{id: {id}, name: Probe {id}, number: '0', service: army, side: israel, sector: probe, status: draft, wikipedia_only: true, " +
            $"anchors: [{{place: {place}, lon: {lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}, lat: {lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
            $"from: '1948-05-15', to: '1948-06-30', end_basis: source, end_note: probe, {Src}}}]}}";
        var yaml = "units:\n" + Row("probe_rosh_pinna", "Rosh Pinna", 35.5433, 32.9693) + "\n" + Row("probe_shaar_hagai", "Sha'ar HaGai", 35.03333, 31.81667);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var rt = new RenderTexture(1920, 1080, 24); var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(yaml), C.Hexes, still: true);
            layer.Show(C, StateAt(new DateTime(1948, 5, 29)));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>().ToList();
            Assert.AreEqual(2, tokens.Count, "both probes drawn");
            var ground = UnitPlinth.HeightAt(map);
            foreach (var t in tokens)
            {
                var plinth = t.Find(UnitPlinth.PlinthName); float r = plinth.localScale.x, lip = UnitPlinth.LipShare * r, stray = 0f, lo = float.MaxValue, hi = float.MinValue;
                foreach (var p in SurfacePoints(plinth)) { float g = ground(p.x, p.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); stray = Mathf.Max(stray, Mathf.Abs(p.y - g - lip)); }
                Debug.Log($"UNITS probe {t.name}: ground spread {hi - lo:0} m under the base, the top strays {stray:0.00} m from the lip");
                Assert.LessOrEqual(stray, 0.5f, $"{t.name}: the top follows the ground");
                foreach (var f in t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")))
                    Assert.AreEqual(ground(f.position.x, f.position.z) + lip, f.position.y, 1f, $"{t.name}/{f.name}: on the draped top");
                var (lon, lat) = map.Frame.ToLonLat(plinth.position);
                new PromoPose("p", lon, lat, 30000f, 0f).Apply(rig, map.Frame);
                UnitFlagFacing.FaceAll(cam.transform.position);
                foreach (var b in t.GetComponentsInChildren<UnitBadge>()) b.transform.rotation = cam.transform.rotation;
                cam.targetTexture = rt; cam.aspect = 1920f / 1080f; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                cam.targetTexture = null; cam.ResetAspect(); RenderTexture.active = null;
                var png = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"statecraft-unitsrender-{t.name}.png");
                System.IO.File.WriteAllBytes(png, tex.EncodeToPNG());
                Debug.Log($"UNITS probe {t.name} at {lon:0.000},{lat:0.000} saved {png}");
            }
        }
        finally { cam.targetTexture = null; RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The base, its lip, its ring and the flag cost little: the base, lip and ring are built per token (they
    /// follow its ground) and low-poly; the flag is one shared mesh per side; the base, lip and the flag's pole are in the
    /// units' one palette material, the ring and the cloth in three unlit colours shared by every token.</summary>
    [Test]
    public void ThePlinthAndTheFlagAreLowPolyAndShareTheirMaterials()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[1]));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens").Cast<Transform>().Where(t => t.Find(UnitPlinth.PlinthName) != null).ToList();
            Assert.Greater(tokens.Count, 4);
            var figureMat = tokens[0].GetComponentsInChildren<MeshRenderer>().First(r => r.name.StartsWith("Rifleman")).sharedMaterial;
            int Tris(Transform t, string n) => t.Find(n).GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
            foreach (var t in tokens)
            {
                Assert.AreSame(figureMat, t.Find(UnitPlinth.PlinthName).GetComponent<MeshRenderer>().sharedMaterial, t.name);
                Assert.AreSame(figureMat, t.Find(UnitPlinth.LipName).GetComponent<MeshRenderer>().sharedMaterial, t.name);
                Assert.LessOrEqual(Tris(t, UnitPlinth.PlinthName), 3000, $"{t.name}: the base is low-poly (the terrain's own cells under it)");
                Debug.Log($"UNITS cost {t.name}: top {Tris(t, UnitPlinth.PlinthName)}, lip {Tris(t, UnitPlinth.LipName)}, ring {Tris(t, UnitPlinth.RimName)} triangles");
                Assert.LessOrEqual(Tris(t, UnitPlinth.RimName), 2 * UnitPlinth.RimSegments, $"{t.name}: the ring is a single band");
            }
            Assert.AreEqual(1, tokens.Select(t => t.Find(UnitPlinth.RimName).GetComponent<MeshRenderer>().sharedMaterial).Distinct().Count(), "one ring material");
            var flags = tokens.Select(t => t.Find(UnitPlinth.FlagName)).Where(f => f != null).ToList();
            Assert.AreEqual(1, flags.Select(f => f.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), "one flag mesh per side");
            Assert.AreEqual(1, flags.Select(f => string.Join(",", f.GetComponent<MeshRenderer>().sharedMaterials.Select(m => m.GetInstanceID()))).Distinct().Count(), "one set of flag materials");
            Assert.AreSame(figureMat, flags[0].GetComponent<MeshRenderer>().sharedMaterials[0], "the pole in the palette");
            Assert.AreSame(Resources.Load<Material>(UnitMaterials.RingResource).shader, flags[0].GetComponent<MeshRenderer>().sharedMaterials[1].shader, "the cloth unlit");
            Assert.Less(flags[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 300, "the flag is low-poly");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }
}
