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
        var rim = token.Find(UnitPlinth.RimName);
        Assert.IsNotNull(rim, $"{what}: the plinth has its white rim");
        Assert.AreEqual(plinth.localScale.x, rim.localScale.x, 0.5f, $"{what}: the rim rings the plinth");
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
        foreach (var f in token.Cast<Transform>().Where(t => t.name.StartsWith("Rifleman")))
            Assert.GreaterOrEqual(f.position.y, plinth.position.y - 0.5f, $"{what}/{f.name}: stands on the plinth's top (or on ground rising through it)");
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

    /// <summary>On the map's hills a plinth sits at the median ground under it (its bottom there, its top a tenth of its
    /// radius higher) and its skirt reaches at most <see cref="UnitPlinth.SkirtCapM"/> below its bottom, down to the lowest
    /// ground where that is nearer (review of #559: no mesa).</summary>
    [Test]
    public void APlinthSitsAtTheMedianGroundWithAShortSkirt()
    {
        var map = BuildMap();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            int n = 0, skirts = 0, closed = 0;
            foreach (var d in Dates)
            {
                layer.Show(C, StateAt(d));
                var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
                foreach (Transform t in tokens)
                {
                    var plinth = t.Find(UnitPlinth.PlinthName); if (plinth == null) continue;
                    float r = plinth.localScale.x;
                    var (low, median, _) = UnitPlinth.GroundUnder((x, z) => UnitShowcase.Ground(map, new Vector3(x, 0, z)).y, plinth.position.x, plinth.position.z, r);
                    float bottom = plinth.position.y - UnitPlinth.ThicknessShare * r;
                    Assert.AreEqual(median, bottom, 1f, $"{d:yyyy-MM-dd} {t.name}: the plinth's bottom is at the median ground");
                    var skirt = t.Find(UnitPlinth.SkirtName);
                    float reach = skirt != null ? skirt.position.y - skirt.localScale.y : bottom;
                    Assert.GreaterOrEqual(reach, bottom - UnitPlinth.SkirtCapM - 1f, $"{t.name}: the skirt is at most {UnitPlinth.SkirtCapM} m");
                    if (low >= bottom - UnitPlinth.SkirtCapM) { Assert.LessOrEqual(reach, low + 1f, $"{t.name}: the skirt closes the seam"); closed++; }
                    if (skirt != null) skirts++;
                    n++;
                }
            }
            Debug.Log($"UNITS plinths: {n} checked, {skirts} with a skirt, {closed} with the seam closed down to the lowest ground");
            Assert.Greater(n, 15);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>No mesa on the hills (review of #559): on every hill token the map can draw (every army anchor of the unit
    /// file, each shown on its first day, the hills found from the terrain under its plinth, not from a hand list), the
    /// plinth's top stands at most <see cref="MaxRiseOverGround"/> of a figure's height over the lowest ground under it.
    /// Before the fix it stood on the highest ground: 1,081 m over the lowest at Harel, 916 m at Golani.</summary>
    [Test]
    public void APlinthStandsLowOnEveryHillToken()
    {
        var map = BuildMap();
        try
        {
            var units = UnitData.Parse(CommittedYaml);
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            var days = units.Where(u => u.Service == UnitData.Army && u.Hidden == null).SelectMany(u => u.Anchors)
                .Select(a => UnitData.Day(a.From)).Distinct().OrderBy(d => d).ToList();
            var worst = new Dictionary<string, (float rise, float spread)>();
            foreach (var d in days)
            {
                layer.Show(C, StateAt(d));
                var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
                foreach (Transform t in tokens)
                {
                    var plinth = t.Find(UnitPlinth.PlinthName); if (plinth == null) continue;
                    float r = plinth.localScale.x, low = float.MaxValue, high = float.MinValue;
                    foreach (var s in new[] { 0f, 0.25f, 0.5f, 0.75f, 0.95f })
                        for (int k = 0; k < 36; k++)
                        {
                            float a = Mathf.PI / 18f * k;
                            float y = UnitShowcase.Ground(map, plinth.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * s).y;
                            low = Mathf.Min(low, y); high = Mathf.Max(high, y);
                        }
                    if (high - low < HillSpreadM) continue;   // not a hill token
                    float rise = plinth.position.y - low;
                    if (!worst.TryGetValue(t.name, out var w) || rise > w.rise) worst[t.name] = (rise, high - low);
                }
            }
            foreach (var kv in worst.OrderByDescending(kv => kv.Value.rise))
                Debug.Log($"UNITS hill {kv.Key}: plinth top {kv.Value.rise:0} m over the lowest ground under it (ground spread {kv.Value.spread:0} m)");
            CollectionAssert.Contains(worst.Keys, "harel", "Harel's Jerusalem-hills token is a hill token");
            float limit = MaxRiseOverGround * UnitBadge.FigureHeightM * UnitShowcase.FigureScale;
            foreach (var kv in worst) Assert.LessOrEqual(kv.Value.rise, limit, $"{kv.Key}: the plinth stands {kv.Value.rise:0} m over the ground (limit {limit:0} m)");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>A token is a hill token when the ground under its plinth spreads over this much (m).</summary>
    const float HillSpreadM = 100f;
    /// <summary>The most a plinth's top may stand over the lowest ground under it, as a share of a figure's height (748 m at
    /// 1400x). The median seat bounds it by the ground's fall from its median plus the base's thickness; the steepest token,
    /// Harel's on the Jerusalem hills (959 m of ground under its base), comes to about 707 m. Before the fix: 1,081 m.</summary>
    const float MaxRiseOverGround = 0.3f;

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

    /// <summary>T29's ring is bright white and wraps the base's edge, about 8% of its radius wide, and the base has a
    /// little thickness. Ours: the rim is drawn unlit (the selection ring's shader) in near-white, well above the lit earth,
    /// 9% of the radius wide; the base is a tenth of its radius thick.</summary>
    [Test]
    public void TheRimIsBrightWhiteAndWideAndTheBaseHasThickness()
    {
        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        Assert.GreaterOrEqual(Luma(UnitPlinth.RimColour), 0.95f, "near-white");
        Assert.GreaterOrEqual(Luma(UnitPlinth.RimColour), 1.5f * Luma(UnitPalette.Shade(UnitPalette.Earth, 0.8f)), "well above the earth it rings");
        Assert.That(UnitPlinth.RimOuter - UnitPlinth.RimInner, Is.InRange(0.07f, 0.12f), "a wide band");
        Assert.Greater(UnitPlinth.RimOuter, 1f, "it wraps the base's edge");
        Assert.That(UnitPlinth.ThicknessShare, Is.InRange(0.08f, 0.14f), "a miniature base's thickness");
        var map = BuildMap();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            var rim = root.transform.Find(UnitPlinth.RimName).GetComponent<MeshRenderer>().sharedMaterial;
            Assert.AreSame(Resources.Load<Material>(UnitMaterials.RingResource).shader, rim.shader, "unlit, as the selection ring");
            Assert.GreaterOrEqual(Luma(rim.color), 0.95f);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
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
            var rims = tokens.Cast<Transform>().Select(t => t.Find(UnitPlinth.RimName)).Where(p => p != null).ToList();
            Assert.AreEqual(plinths.Count, rims.Count);
            Assert.AreEqual(1, rims.Select(p => p.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), "one rim mesh");
            Assert.AreEqual(1, rims.Select(p => p.GetComponent<MeshRenderer>().sharedMaterial).Distinct().Count(), "one rim material");
            Assert.Less(rims[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 200, "the rim is low-poly");
            Assert.Less(plinths[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 400, "the plinth is low-poly");
            Assert.Less(flags[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 300, "the flag is low-poly");
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }
}
