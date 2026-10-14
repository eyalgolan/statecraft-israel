using System;
using System.Collections.Generic;
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

/// <summary>Demo2 T29's token look (refs #197): every army token stands on a round earth plinth with a white rim and flies
/// its side's flag on a pole; a side with no sourced flag flies none. The figures stand about 1.8 times a house
/// (docs/units.md, "The plinth and the flag").</summary>
public class UnitPlinthTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName));
    static readonly DateTime[] Dates = { new DateTime(1948, 5, 29), new DateTime(1948, 7, 12), new DateTime(1948, 12, 4) };

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
    static void AssertPlinth(Transform token, string side, string what)
    {
        var plinth = token.Find(UnitPlinth.PlinthName);
        Assert.IsNotNull(plinth, $"{what}: stands on a plinth");
        var mesh = plinth.GetComponent<MeshFilter>().sharedMesh;
        Assert.IsTrue(Painted(mesh, UnitPalette.Earth), $"{what}: the plinth is earth");
        Assert.IsTrue(Painted(mesh, UnitPlinth.RimSlot), $"{what}: the plinth has its white rim");
        var flag = token.Find(UnitPlinth.FlagName);
        if (UnitFlags.Has(side))
        {
            Assert.IsNotNull(flag, $"{what}: flies its side's flag");
            Assert.AreEqual(UnitFlags.MeshName(side), flag.GetComponent<MeshFilter>().sharedMesh.name, $"{what}: the flag is {side}'s");
        }
        else Assert.IsNull(flag, $"{what}: {side} has no sourced flag, so none is drawn");
        foreach (var f in token.Cast<Transform>().Where(t => t.name.StartsWith("Rifleman")))
            Assert.AreEqual(plinth.position.y, f.position.y, 0.5f, $"{what}/{f.name}: stands on the plinth's top");
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
                    if (p.Token.Unit.Service == UnitData.Army) { AssertPlinth(t, p.Token.Unit.Side, $"{d:yyyy-MM-dd} {t.name}"); army++; }
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
            AssertPlinth(root.transform, UnitData.Israel, "showcase");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    // ---- The flag matches the side --------------------------------------------------------------------------------

    /// <summary>Every row of the unit file names its side, and every formation in it is Israel's (the file is the IDF's
    /// 1948 order of battle); only Israel has a flag (built from the 1948 specification), so no flag is invented.</summary>
    [Test]
    public void TheFlagIsTheSidesAndOnlyASourcedFlagIsDrawn()
    {
        var units = UnitData.Parse(CommittedYaml);
        Assert.IsTrue(units.All(u => u.Side == UnitData.Israel), "every 1948 formation in the file is Israel's");
        Assert.IsTrue(UnitFlags.Has(UnitData.Israel));
        foreach (var side in UnitData.Sides.Where(s => s != UnitData.Israel))
        {
            Assert.IsFalse(UnitFlags.Has(side), $"{side}: no sourced, licensed flag in the repo, so none");
            Assert.IsNull(UnitFlags.Build(side), side);
        }

        // An Arab side's token (as #226 will add): the same plinth and rim, and no flag.
        var map = BuildMap();
        try
        {
            var egypt = UnitData.Parse(CommittedYaml.Replace("side: israel", "side: egypt"));
            var layer = UnitLayer.Create(map, egypt, C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[0]));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            var army = layer.Tokens.Where(p => p.Token.Unit.Service == UnitData.Army).ToList();
            Assert.Greater(army.Count, 3);
            foreach (var p in army) AssertPlinth(tokens.Find(p.Token.Unit.Id), "egypt", p.Token.Unit.Id);
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
        Assert.AreEqual(6 * 2 * 2 * 4, inStar, "six star stripes, as quads on both faces");
        var white = Enumerable.Range(0, v.Length).Where(i => Mathf.Abs(uv[i].x - UnitPalette.U(UnitPalette.RoundelWhite)) < 1e-4f).Select(i => Cm(v[i])).ToList();
        Assert.AreEqual(0f, white.Min(c => c.x), 0.01f); Assert.AreEqual(220f, white.Max(c => c.x), 0.01f);
        Assert.AreEqual(0f, white.Min(c => c.y), 0.01f); Assert.AreEqual(160f, white.Max(c => c.y), 0.01f, "8:11");
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

    /// <summary>On the map's hills a plinth's top is no lower than the ground under it, and its skirt reaches the lowest
    /// ground under it, so no boot sinks and no plinth floats.</summary>
    [Test]
    public void APlinthNeverSinksOrFloatsOnTheHills()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            int n = 0; float worstSkirt = 0f;
            foreach (var d in Dates)
            {
                layer.Show(C, StateAt(d));
                var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
                foreach (Transform t in tokens)
                {
                    var plinth = t.Find(UnitPlinth.PlinthName); if (plinth == null) continue;
                    float r = plinth.localScale.x, top = plinth.position.y, lowest = float.MaxValue;
                    for (int k = 0; k < 48; k++)
                        foreach (var s in new[] { 0.3f, 0.6f, 0.95f })
                        {
                            float a = Mathf.PI / 24f * k + 0.05f;
                            var q = UnitShowcase.Ground(map, plinth.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * s);
                            Assert.LessOrEqual(q.y, top + 25f, $"{d:yyyy-MM-dd} {t.name}: the ground rises through the plinth");
                            lowest = Mathf.Min(lowest, q.y);
                        }
                    float bottom = top - UnitPlinth.ThicknessShare * r;
                    var skirt = t.Find(UnitPlinth.SkirtName);
                    if (skirt != null) bottom = Mathf.Min(bottom, skirt.position.y - skirt.localScale.y);
                    Assert.LessOrEqual(bottom, lowest + 1f, $"{d:yyyy-MM-dd} {t.name}: the plinth reaches the ground downhill");
                    worstSkirt = Mathf.Max(worstSkirt, top - lowest);
                    n++;
                }
            }
            Debug.Log($"UNITS plinths: {n} checked, worst rise under a plinth {worstSkirt:0} m");
            Assert.Greater(n, 15);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The plinth and the flag cost little: one shared mesh each (not one per token), low-poly, in the units'
    /// one palette material (no new shader or texture).</summary>
    [Test]
    public void ThePlinthAndTheFlagAreSharedLowPolyMeshesInTheUnitMaterial()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            layer.Show(C, StateAt(Dates[1]));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            var plinths = tokens.Cast<Transform>().Select(t => t.Find(UnitPlinth.PlinthName)).Where(p => p != null).ToList();
            var flags = tokens.Cast<Transform>().Select(t => t.Find(UnitPlinth.FlagName)).Where(p => p != null).ToList();
            Assert.Greater(plinths.Count, 4);
            Assert.AreEqual(1, plinths.Select(p => p.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), "one plinth mesh");
            Assert.AreEqual(1, flags.Select(p => p.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), "one flag mesh per side");
            var figureMat = tokens.GetComponentsInChildren<MeshRenderer>().First(r => r.name.StartsWith("Rifleman")).sharedMaterial;
            foreach (var p in plinths.Concat(flags)) Assert.AreSame(figureMat, p.GetComponent<MeshRenderer>().sharedMaterial, p.name);
            Assert.Less(plinths[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 400, "the plinth is low-poly");
            Assert.Less(flags[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 300, "the flag is low-poly");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }
}
