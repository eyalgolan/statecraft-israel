#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the stills for picking the b21 arms workshop's size on PR #2876 (owner, 6 November 2026: "the workshop
/// should be bigger as it's barely visible") to `.superpowers/shots-workshop-scale/` at the repository root (local, not
/// committed). English only: a map change.
/// It boots one gdd-0.3 game (as MajorCityShots.WorkshopProps boots it) with one built arms workshop in tel_aviv (a major
/// city: its b21 stands at the city's edge, EdgeSpot) and one in rehovot (an ordinary town, HexPropModelsTests' workshop
/// town), both on their settlement hex, built on 15 August 1948. For each try of SpecialBuildingModels.WorkshopScale (the
/// seam; 1, 2, 2.5 and 3) it sets the seam, lets GameRoot redraw on the same date and rebuilds the special layer
/// (MajorCityShots.Special), so the plan is made again with the bigger clearance: a spot may move or be lost, and the log
/// says which.
/// <list type="bullet">
/// <item>`&lt;scale&gt;-&lt;town&gt;-&lt;km&gt;km.png` (for example `2.5-tel_aviv-8km.png`, `1-rehovot-30km.png`): the camera (yaw 0)
/// on the workshop's planned spot at 8 and 30 km of the town layer's view distance (MajorCityShots.Aim, with its aspect fix), the map camera's
/// frame with the UI over it (MajorCityShots.Shot).</item>
/// <item>`compare-&lt;town&gt;-&lt;km&gt;km.png`: the four stills at half size, left to right in the order 1, 2, 2.5, 3, each
/// under a label drawn with a small built-in pixel font: its scale and its size in metres (for example "2.5x 720m"). A scale
/// whose spot was lost has a grey panel and "-" for its size.</item>
/// </list>
/// Each still gets a `SHOT` line: the scale, SizeOf(Workshop) by model name and SizeOf of the row (the two must agree), the
/// clearance (ClearanceOf), the spot's offset from the town's kit-footprint centre, its hex and whether that is the project's
/// hex, whether the spot moved from scale 1's, and for the major city the nearest of its kit blocks (MapScene.MajorCityBlocks):
/// its footprint W x D and its box distance, so "not bigger than a kit block" can be read off numbers. The seam is restored
/// in a finally and in TearDown. Run (Unity, the unity/ project):
/// <code>&lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter WorkshopScaleShots</code>
/// Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class WorkshopScaleShots
{
    static readonly float[] Scales = { 1f, 2f, 2.5f, 3f };
    static readonly string[] Towns = { "tel_aviv", "rehovot" };
    static readonly float[] DistancesKm = { 8f, 30f };
    /// <summary>MajorCityShots.WorkshopsBuilt: three months in, where every major city's edge has ground the Israeli side holds.</summary>
    static readonly DateTime WorkshopsBuilt = new DateTime(1948, 8, 15);
    const int Half = 2, LabelH = 44, Cell = 6;
    string shotDir = "", saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    int poke = -2876500;   // ids no unit has, for SimBridge.Skip to raise Changed (each id only once)
    float savedScale;
    Action<string, CollectionDetail, Action<GameObject?>>? savedSpecial;
    Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? savedTown;

    [SetUp]
    public void Prepare()
    {
        shotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-workshop-scale"));
        Directory.CreateDirectory(shotDir);
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-workshop-scale-shots-" + Guid.NewGuid().ToString("N"));
        savedScale = SpecialBuildingModels.WorkshopScale;
        savedSpecial = SpecialBuildingModels.Request; savedTown = TownModelLayer.ModelAcquire;
        TownModelLayer.ModelAcquire = (id, detail, done) => { done(Prefab(id, detail)); return new NoLease(); };
    }

    [TearDown]
    public void DiscardScene()
    {
        SpecialBuildingModels.WorkshopScale = savedScale;
        SpecialBuildingModels.Request = savedSpecial!; TownModelLayer.ModelAcquire = savedTown!;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    sealed class NoLease : IDisposable { public void Dispose() { } }
    static GameObject? Prefab(string id, CollectionDetail detail) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));
    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    static MapScene Scene => Object.FindFirstObjectByType<MapScene>();
    static TownModelLayer TownLayer => Scene.GetComponentInChildren<TownModelLayer>(true);
    static string Inv(FormattableString s) => FormattableString.Invariant(s);

    /// <summary>MajorCityShots.CentreOf: a town's kit-footprint centre on the ground; the settlement's own point without one.</summary>
    static Vector3 CentreOf(string id)
    {
        var map = Scene;
        var f = map.KitFootprints.FirstOrDefault(x => x.id == id);
        Vector3 at;
        if (f.id != null) at = new Vector3(f.centre.x, 0f, f.centre.y);
        else { var s = map.Settlements.First(x => x.Id == id); at = map.Frame.ToWorld(s.Lon, s.Lat, 0); }
        return Ground(at);
    }

    static Vector3 Ground(Vector3 at)
    {
        var map = Scene;
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        return at;
    }

    /// <summary>MajorCityShots.Aim: yaw 0 at <paramref name="viewM"/> of the town layer's view distance, with the aspect fix.</summary>
    static void Aim(Vector3 at, float viewM)
    {
        Camera.main.aspect = (float)CivMovementShots.Width / CivMovementShots.Height;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Yaw = 0f; rig.Distance = viewM; rig.Apply();
        float down = -Camera.main.transform.forward.y;
        if (down > 1e-4f) { rig.Distance = Mathf.Max(1000f, viewM - at.y / down); rig.Apply(); }
    }

    /// <summary>MajorCityShots.Settle.</summary>
    static IEnumerator Settle(string city)
    {
        var towns = TownLayer; var cam = Camera.main;
        int i = 0;
        for (; i < 60; i++)
        {
            towns.PrepareCamera(cam); towns.RestoreRenderers();
            bool waiting = towns.PendingRetirementLeases > 0 || (towns.AdmitsTown(city, cam) && !towns.ReadyForTown(city, cam));
            if (!waiting && i >= 3) break;
            cam.Render();
            yield return null;
        }
        if (i >= 60) Debug.LogWarning($"SHOT {city}: the town layer did not settle in 60 frames");
    }

    static Color[] Read(RenderTexture from)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = from;
        var tex = new Texture2D(from.width, from.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, from.width, from.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }

    static void Save(string path, Color[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>MajorCityShots.Shot (the map camera's frame with the premultiplied UI over it), also returning the pixels.</summary>
    Color[] Shot(string name)
    {
        int w = CivMovementShots.Width, h = CivMovementShots.Height;
        var cam = Camera.main;
        var frame = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = frame; cam.Render(); cam.targetTexture = before;
        var map = Read(frame); var over = Read(rt!);
        frame.Release(); Object.DestroyImmediate(frame);
        var outPx = new Color[map.Length];
        for (int i = 0; i < map.Length; i++)
        {
            var u = over[i]; var m = map[i];
            outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f);
        }
        Save(Path.Combine(shotDir, name + ".png"), outPx, w, h);
        return outPx;
    }

    /// <summary>MajorCityShots.SetDate.</summary>
    IEnumerator SetDate(DateTime date)
    {
        var bridge = root.Bridge!;
        foreach (var s in new HashSet<GameState> { bridge.State, bridge.TurnStart }) s.Date = date;
        bridge.Skip(poke--);
        yield return Frames(3);
    }

    /// <summary>MajorCityShots.Special: the special layer rebuilt (and so re-planned) on the game's date and built workshops.</summary>
    SpecialBuildingModels Special()
    {
        var map = Scene; var state = root.Bridge!.State; var content = root.Bridge.Content;
        SpecialBuildingModels.Request = (id, detail, done) => done(Prefab(id, detail));
        var old = map.Terrain.transform.Find(SpecialBuildingModels.Name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        return CollectionGeometryFixture.Special(map, state.Date, content, state.CityProjects.Values)!;
    }

    /// <summary>The nearest of <paramref name="city"/>'s kit blocks to <paramref name="at"/> (MajorCityShots.NearestBlocks' box
    /// distance), with its footprint; "" when the town has none (an ordinary town).</summary>
    static string NearestBlock(Vector2 at, string city)
    {
        float best = float.MaxValue; SettlementKits.Placed? near = null;
        foreach (var b in Scene.MajorCityBlocks)
        {
            if (b.Id != city) continue;
            float a = b.Yaw * Mathf.Deg2Rad;
            var q = b.C - at; float qu = q.x * Mathf.Cos(a) + q.y * Mathf.Sin(a), qv = -q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a);
            float du = Mathf.Max(0f, Mathf.Abs(qu) - 0.5f * b.W), dv = Mathf.Max(0f, Mathf.Abs(qv) - 0.5f * b.D);
            float d = Mathf.Sqrt(du * du + dv * dv);
            if (d < best) { best = d; near = b; }
        }
        return near is SettlementKits.Placed n
            ? Inv($"; nearest {city} kit block: {n.Kind} {n.W:F0} x {n.D:F0} m (yaw {n.Yaw:F0}), {best:F0} m away")
            : "; no kit blocks (not a major city)";
    }

    [UnityTest]
    public IEnumerator Scales1To3()
    {
        var inputs = Statecraft.Game.Boot.GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        foreach (var era in inputs.Content.Eras) { era.Systems.Ruleset = SaveGame.Gdd03Ruleset; era.Systems.Loop = EraSystems.Gdd; }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(CivMovementShots.Width, CivMovementShots.Height, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Scene.Build(inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(inputs, 0);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));
        yield return Frames(3);
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        Assert.AreEqual(SaveGame.Gdd03Ruleset, content.Ruleset);
        yield return SetDate(WorkshopsBuilt);
        var built = new List<(int id, string town, int q, int r)>();
        foreach (var town in Towns)
        {
            var home = content.SettlementHexes.FirstOrDefault(h => h.Id == town);
            if (home == null) { Debug.LogWarning($"SHOT {town}: no settlement hex; no workshop"); continue; }
            int id = state.NextId++;
            state.CityProjects.Add(id, new CityProject(id, Statecraft.Sim.Workshops.ArmsWorkshop, town, home.Q, home.R, 0, state.Date));
            built.Add((id, town, home.Q, home.R));
            Debug.Log(Inv($"SHOT setup: {town} ({(TownModels.IsMajorCity(town) ? "major city" : "ordinary town")}) arms workshop, project {id} on hex {home.Q},{home.R}, built {state.Date:yyyy-MM-dd}"));
        }
        var land = content.Hexes.Where(h => h.Land).Select(h => (h, at: Scene.Frame.ToWorld(h.Lon, h.Lat, 0))).ToList();
        var firstSpot = new Dictionary<string, Vector2>();
        var panels = new Dictionary<(string town, float km), Color[]?[]>();
        foreach (var t in built) foreach (var km in DistancesKm) panels[(t.town, km)] = new Color[]?[Scales.Length];
        try
        {
            for (int si = 0; si < Scales.Length; si++)
            {
                float scale = Scales[si];
                SpecialBuildingModels.WorkshopScale = scale;
                yield return SetDate(state.Date);   // GameRoot redraws every layer on the new size first
                var layer = Special();
                foreach (var (id, town, q, r) in built)
                {
                    var key = SpecialBuildingModels.WorkshopKey(id);
                    var want = SpecialBuildingModels.ArmsWorkshops(content, state.CityProjects.Values).Single(w => w.Key == key);
                    float byName = SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop), byRow = SpecialBuildingModels.SizeOf(want);
                    float clearance = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
                    var sizes = Inv($"scale {scale:0.##}: SizeOf(Workshop) {byName:F1} m, SizeOf(row) {byRow:F1} m, ClearanceOf(Workshop) {clearance:F1} m, ClearanceOf(row) {SpecialBuildingModels.ClearanceOf(want):F1} m");
                    var placed = layer.Placed.Where(p => p.Key == key).ToList();
                    if (placed.Count == 0)
                    {
                        Debug.LogWarning($"SHOT {town}: {sizes}; LOST: the re-plan gave {key} no spot; no stills");
                        continue;
                    }
                    var at = Ground(placed[0].At); var flat = new Vector2(at.x, at.z);
                    var centre = CentreOf(town);
                    var hex = land.OrderBy(x => (new Vector2(x.at.x, x.at.z) - flat).sqrMagnitude).First().h;
                    string moved;
                    if (firstSpot.TryGetValue(key, out var first)) moved = Inv($"{(flat - first).magnitude:F0} m from scale {Scales[0]:0.##}'s spot");
                    else { firstSpot[key] = flat; moved = Inv($"the reference spot (scale {scale:0.##})"); }
                    var spot = Inv($"spot {at} ({at.x - centre.x:F0}, {at.z - centre.z:F0}) m east, north of {town}'s centre ({(flat - new Vector2(centre.x, centre.z)).magnitude:F0} m), ")
                        + Inv($"hex {hex.Q},{hex.R} ({(hex.Q == q && hex.R == r ? "on" : "off")} the project hex {q},{r}), {moved}");
                    foreach (var km in DistancesKm)
                    {
                        Aim(at, km * 1000f);
                        layer.Choose(default, Camera.main);
                        yield return Settle(town);
                        var name = Inv($"{scale:0.##}-{town}-{km:0}km");
                        panels[(town, km)][si] = Shot(name);
                        Debug.Log(Inv($"SHOT {name}: {sizes}; {spot}; view {TownModelLayer.ViewDistance(Camera.main):F0} m (rig {Camera.main.GetComponent<CameraRig>().Distance:F0} m)")
                            + NearestBlock(flat, town));
                    }
                }
            }
        }
        finally { SpecialBuildingModels.WorkshopScale = savedScale; }
        foreach (var kv in panels) Compare(kv.Key.town, kv.Key.km, kv.Value);
    }

    /// <summary>The four stills of one town and distance at half size, left to right in <see cref="Scales"/>' order, each under
    /// a label "&lt;scale&gt;x &lt;size&gt;m" (the size at that scale; "-" for a lost spot, whose panel is grey).</summary>
    void Compare(string town, float km, Color[]?[] shots)
    {
        int w = CivMovementShots.Width, h = CivMovementShots.Height, pw = w / Half, ph = h / Half;
        int cw = pw * Scales.Length, ch = ph + LabelH;
        var px = new Color[cw * ch];
        for (int i = 0; i < px.Length; i++) px[i] = Color.black;
        for (int s = 0; s < Scales.Length; s++)
        {
            var src = shots[s];
            for (int y = 0; y < ph; y++)
                for (int x = 0; x < pw; x++)
                {
                    Color c = new Color(0.35f, 0.35f, 0.35f);
                    if (src != null)
                    {
                        c = Color.clear;
                        for (int dy = 0; dy < Half; dy++) for (int dx = 0; dx < Half; dx++) c += src[(y * Half + dy) * w + x * Half + dx];
                        c /= Half * Half; c.a = 1f;
                    }
                    px[y * cw + s * pw + x] = c;
                }
            // a 2 px white rule between panels
            if (s > 0) for (int y = 0; y < ch; y++) for (int x = 0; x < 2; x++) px[y * cw + s * pw + x] = Color.white;
            float size = SpecialBuildingModels.RealLengthM(SpecialBuildingModels.Workshop) * SpecialBuildingModels.WorldScale * Scales[s];
            var label = Inv($"{Scales[s]:0.##}x ") + (src == null ? "-" : Inv($"{size:F0}m"));
            Text(px, cw, s * pw + 10, ch - 7, label);
        }
        var name = Inv($"compare-{town}-{km:0}km");
        Save(Path.Combine(shotDir, name + ".png"), px, cw, ch);
        Debug.Log(Inv($"SHOT {name}: scales {string.Join(", ", Scales.Select(x => x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)))} left to right, ")
            + $"{shots.Count(x => x != null)} of {Scales.Length} drawn");
    }

    /// <summary>A 3x5 pixel font for the labels' characters (rows top to bottom, 1 = lit).</summary>
    static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        ['0'] = new[] { "111", "101", "101", "101", "111" }, ['1'] = new[] { "010", "110", "010", "010", "111" },
        ['2'] = new[] { "111", "001", "111", "100", "111" }, ['3'] = new[] { "111", "001", "111", "001", "111" },
        ['4'] = new[] { "101", "101", "111", "001", "001" }, ['5'] = new[] { "111", "100", "111", "001", "111" },
        ['6'] = new[] { "111", "100", "111", "101", "111" }, ['7'] = new[] { "111", "001", "001", "001", "001" },
        ['8'] = new[] { "111", "101", "111", "101", "111" }, ['9'] = new[] { "111", "101", "111", "001", "111" },
        ['.'] = new[] { "000", "000", "000", "000", "010" }, ['x'] = new[] { "000", "101", "010", "101", "000" },
        ['m'] = new[] { "000", "110", "111", "101", "101" }, ['-'] = new[] { "000", "000", "111", "000", "000" },
        [' '] = new[] { "000", "000", "000", "000", "000" },
    };

    /// <summary>Draws <paramref name="text"/> in white, <see cref="Cell"/> px a font pixel, its top row at <paramref name="top"/>
    /// (rows count up from the image's bottom, as Texture2D.SetPixels takes them).</summary>
    static void Text(Color[] px, int width, int left, int top, string text)
    {
        int x0 = left;
        foreach (var ch in text)
        {
            if (Glyphs.TryGetValue(ch, out var g))
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                        if (g[row][col] == '1')
                            for (int dy = 0; dy < Cell; dy++)
                                for (int dx = 0; dx < Cell; dx++)
                                {
                                    int x = x0 + col * Cell + dx, y = top - row * Cell - dy;
                                    if (x >= 0 && x < width && y >= 0 && y * width + x < px.Length) px[y * width + x] = Color.white;
                                }
            x0 += 4 * Cell;
        }
    }
}
