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
            Assert.AreEqual(UnitPlinth.BootsTop(ground, f.position.x, f.position.z) + UnitPlinth.LipShare * plinth.localScale.x, f.position.y, 0.5f,
                $"{what}/{f.name}: stands on the base's top over the ground at its feet");
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
            foreach (var p in army) AssertPlinth(map, tokens.Find(p.Token.Unit.Id), "egypt", p.Token.Unit.Id);
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
    /// figure torsos 0.15-0.24, 0.5-0.9 of the base (band 0.45-1.1); flag white 0.83-0.91 (at least 0.8).</summary>
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
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
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
                Assert.That(shirt / bas, Is.InRange(0.45f, 1.1f), $"{t.name}: the figures against the base as in T29");
                Assert.GreaterOrEqual(white, 0.8f, $"{t.name}: the flag reads white");
            }
        }
        finally { cam.targetTexture = null; RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(map.gameObject); }
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
                Assert.LessOrEqual(Tris(t, UnitPlinth.PlinthName), 2 * UnitPlinth.Rings * UnitPlinth.Segments, $"{t.name}: the base is low-poly");
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
