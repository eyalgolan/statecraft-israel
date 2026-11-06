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

/// <summary>Writes the before and after stills for PR #2876 (the major cities draw their kits at every zoom: owner, 6 November
/// 2026, "Big blocks, every zoom" and "Just the four (Recommended)") to `.superpowers/shots-2876/&lt;SHOTS_TAG&gt;/` at the
/// repository root (local, not committed; SHOTS_TAG from the environment, "untagged" when unset). English only: a map change,
/// and the labels do not change.
/// <list type="bullet">
/// <item>`&lt;city&gt;-&lt;km&gt;km.png` for tel_aviv, jerusalem, haifa and jaffa at 250, 60, 39 and 31 km (31 km is
/// CameraRig.StartDistance), the camera (yaw 0) on the city's kit-footprint centre (MapScene.KitFootprints, also its label
/// point: TownModelResidencyTests). The km are the town layer's own view distance (TownModelLayer.ViewDistance, the one its
/// 40/45 km close switch and 100 km far band read), so the rig's distance is shortened by the look-at's height. Before
/// (main): Tal Heuberger's far-off copies at 250 and 60 km (main's AtZoom admits these four in view at every zoom), the
/// originals at 39 and 31 km. After: the kits at every distance.</item>
/// <item>`&lt;prop&gt;-close.png`, the props placed to clear the 24x building models (SpecialBuildingModels.Plan's TownEdge rows,
/// clear of TownModelLayer.InstancesOf) that may now stand inside the 60x/100x kit blocks, at 8 km: the Altalena sandbags on
/// the Tel Aviv shore (tel_aviv_altalena_sandbags, the game date set to 15 June 1948, SandbagsFrom: the row covers 15 to 23
/// June 1948 only), the Haifa port quay crates (haifa_quay_crates, 1948 turns, Year1948From), and a built arms workshop's b21
/// in each of the four cities (gdd-0.3 only: SpecialBuildingModels.ArmsWorkshops; a fixture project built on the opening
/// date, as TalModelsShots.ArmsWorkshop builds it).</item>
/// </list>
/// How the models load in edit mode, the same on main and on the branch: the town layer gets the editor's complete model list
/// (MapScene.TownModelsForBuild reads Kits.TownModels.SetPath from the AssetDatabase in edit mode), so TownModelLayer does not
/// stream and a town is ready as soon as a camera admits it; TownModelLayer.ModelAcquire is answered from the AssetDatabase too,
/// as TalModelsShots.Houses does, in case it streams. The special layer is rebuilt as TalModelsShots.Prop rebuilds it
/// (CollectionGeometryFixture.Special, its requests answered from the AssetDatabase), after GameRoot has refreshed on the same
/// date, so GameRoot's next refresh keeps it.
/// Each still is the map camera's frame with the UI drawn over it (CivMovementShots.Boot and its Shot's alpha composite), and
/// each gets a `SHOT` line: whether the city's kit renderer "&lt;id&gt;" and its close mesh "&lt;id&gt; close" draw for the map camera
/// (forceRenderingOff after TownModelLayer.PrepareCamera), whether the layer admits and has ready the city's models, and the
/// instances the layer drew in the frame. It uses only calls main has. Run (Unity, the unity/ project), at the PR's head:
/// <code>SHOTS_TAG=after &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter MajorCityShots</code>
/// then with this file and its .meta copied onto main:
/// <code>SHOTS_TAG=before &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter MajorCityShots</code>
/// Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class MajorCityShots
{
    static readonly string[] Cities = { "tel_aviv", "jerusalem", "haifa", "jaffa" };
    /// <summary>The day WorkshopProps builds the four workshops on (#2876): three months into the game.</summary>
    static readonly DateTime WorkshopsBuilt = new DateTime(1948, 8, 15);
    static readonly float[] DistancesKm = { 250f, 60f, 39f, CameraRig.StartDistance / 1000f };
    const float PropDistanceM = 8000f;
    string shotDir = "", saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    int poke = -2876000;   // ids no unit has, for SimBridge.Skip to raise Changed (each id only once)
    Action<string, CollectionDetail, Action<GameObject?>>? savedSpecial;
    Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? savedTown;

    [SetUp]
    public void Prepare()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        shotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2876",
            string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(shotDir);
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-major-city-shots-" + Guid.NewGuid().ToString("N"));
        savedSpecial = SpecialBuildingModels.Request; savedTown = TownModelLayer.ModelAcquire;
        TownModelLayer.ModelAcquire = (id, detail, done) => { done(Prefab(id, detail)); return new NoLease(); };
    }

    [TearDown]
    public void DiscardScene()
    {
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
    static TownModelLayer Towns => Scene.GetComponentInChildren<TownModelLayer>(true);

    /// <summary>A city's kit-footprint centre on the ground (its label point); the settlement's own point without a footprint.</summary>
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

    /// <summary>The camera on <paramref name="at"/> with yaw 0 and the game's top inset, at <paramref name="viewM"/> of the town
    /// layer's view distance (to the y = 0 plane: the rig's distance plus the look-at's height along the view ray).</summary>
    static void Aim(Vector3 at, float viewM)
    {
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Yaw = 0f; rig.Distance = viewM; rig.Apply();
        float down = -Camera.main.transform.forward.y;
        if (down > 1e-4f) { rig.Distance = Mathf.Max(1000f, viewM - at.y / down); rig.Apply(); }
    }

    /// <summary>Waits until the town layer has settled for the map camera: renders (each render runs the layer's own callback)
    /// until the city, if this zoom admits it, has its models ready and no lease is retiring.</summary>
    static IEnumerator Settle(string city)
    {
        var towns = Towns; var cam = Camera.main;
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

    /// <summary>The map camera's state for <paramref name="city"/>: whether its kit and close renderers draw (as the layer leaves
    /// them for this camera), and whether the layer admits it and has its models ready.</summary>
    static string CityState(string city)
    {
        var towns = Towns; var cam = Camera.main;
        var kits = Scene.transform.Find(KitLayer.RootName);
        var kit = kits == null ? null : kits.Find(city)?.GetComponent<Renderer>();
        var close = kits == null ? null : kits.Find(city + KitLayer.CloseSuffix)?.GetComponent<Renderer>();
        towns.PrepareCamera(cam);
        string Draws(Renderer? r) => r == null ? "missing" : (!r.forceRenderingOff).ToString();
        string s = $"kit \"{city}\" draws={Draws(kit)}, \"{city}{KitLayer.CloseSuffix}\" draws={Draws(close)}, " +
                   $"models admitted={towns.AdmitsTown(city, cam)} ready={towns.ReadyForTown(city, cam)}";
        towns.RestoreRenderers();
        return s;
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

    /// <summary>CivMovementShots.Shot into this run's folder: the map camera's frame with the UI texture over it (the UI's
    /// pixels are premultiplied by their alpha).</summary>
    void Shot(string name)
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
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static string Frame(Vector3 at)
    {
        var rig = Camera.main.GetComponent<CameraRig>(); var towns = Towns;
        return FormattableString.Invariant($"look-at {at}, view {TownModelLayer.ViewDistance(Camera.main):F0} m (rig {rig.Distance:F0} m), ")
            + FormattableString.Invariant($"layer drew {towns.LastDrawn} instances, close={towns.LastClose}");
    }

    /// <summary>Sets the game's date (the state the map shows and the turn's start) and lets GameRoot redraw on it.</summary>
    IEnumerator SetDate(DateTime date)
    {
        var bridge = root.Bridge!;
        foreach (var s in new HashSet<GameState> { bridge.State, bridge.TurnStart }) s.Date = date;
        bridge.Skip(poke--);
        yield return Frames(3);
    }

    /// <summary>TalModelsShots.Prop's special layer: rebuilt with every model answered from the AssetDatabase, on the game's
    /// date and its built arms workshops (as GameRoot shows it, so its next refresh keeps this layer).</summary>
    SpecialBuildingModels Special()
    {
        var map = Scene; var state = root.Bridge!.State; var content = root.Bridge.Content;
        SpecialBuildingModels.Request = (id, detail, done) => done(Prefab(id, detail));
        var old = map.Terrain.transform.Find(SpecialBuildingModels.Name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var layer = CollectionGeometryFixture.Special(map, state.Date, content, state.CityProjects.Values)!;
        // Every placed row, and whether it stands inside one of the four cities' kit footprints.
        foreach (var p in layer.Placed)
        {
            var inside = map.KitFootprints.Where(f => Cities.Contains(f.id) && Vector2.Distance(new Vector2(p.At.x, p.At.z), f.centre) < f.radius)
                .Select(f => f.id).ToList();
            Debug.Log(FormattableString.Invariant($"SCAN {state.Date:yyyy-MM-dd} {p.Key} ({p.Model}) at {p.At}: inside {(inside.Count == 0 ? "no major city" : string.Join(", ", inside))}"));
        }
        return layer;
    }

    IEnumerator Prop(SpecialBuildingModels layer, string key, string city, string name)
    {
        var placed = layer.Placed.Where(p => p.Key == key).ToList();
        if (placed.Count == 0) { Debug.LogWarning($"SHOT {name}: {key} is not placed on {root.Bridge!.State.Date:yyyy-MM-dd}; no still"); yield break; }
        var at = Ground(placed[0].At);
        Aim(at, PropDistanceM);
        layer.Choose(default, Camera.main);
        yield return Settle(city);
        var state = CityState(city);
        Shot(name);
        var f = Scene.KitFootprints.FirstOrDefault(x => x.id == city);
        float fromCentre = f.id == null ? float.NaN : Vector2.Distance(new Vector2(at.x, at.z), f.centre);
        Debug.Log(FormattableString.Invariant($"SHOT {name}: prop {key} ({placed[0].Model}) on {root.Bridge!.State.Date:yyyy-MM-dd}, ")
            + FormattableString.Invariant($"{fromCentre:F0} m from {city}'s kit centre (radius {f.radius:F0} m), {PropDistanceM / 1000f:F0} km; {state}; {Frame(at)}")
            + NearestBlocks(new Vector2(at.x, at.z), city));
    }

    /// <summary>#2876: a spot's box distance (m) to the nearest kit block of <paramref name="city"/> and of any other major city
    /// (MapScene.MajorCityBlocks), for the SHOT line.</summary>
    string NearestBlocks(Vector2 at, string city)
    {
        float own = float.MaxValue, other = float.MaxValue; string otherId = "none";
        foreach (var b in Scene.MajorCityBlocks)
        {
            float a = b.Yaw * Mathf.Deg2Rad;
            var q = b.C - at; float qu = q.x * Mathf.Cos(a) + q.y * Mathf.Sin(a), qv = -q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a);
            float du = Mathf.Max(0f, Mathf.Abs(qu) - 0.5f * b.W), dv = Mathf.Max(0f, Mathf.Abs(qv) - 0.5f * b.D);
            float d = Mathf.Sqrt(du * du + dv * dv);
            if (b.Id == city) own = Mathf.Min(own, d);
            else if (d < other) { other = d; otherId = b.Id; }
        }
        return own == float.MaxValue ? "" : FormattableString.Invariant($"; nearest kit block: {city}'s {own:F0} m, {otherId}'s {other:F0} m");
    }

    [UnityTest]
    public IEnumerator CityZooms()
    {
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        foreach (var city in Cities)
        {
            var at = CentreOf(city);
            foreach (var km in DistancesKm)
            {
                Aim(at, km * 1000f);
                yield return Settle(city);
                var state = CityState(city);
                var name = FormattableString.Invariant($"{city}-{km:0}km");
                Shot(name);
                Debug.Log(FormattableString.Invariant($"SHOT {name}: city {city}, {km:0} km; {state}; {Frame(at)}"));
            }
        }
    }

    /// <summary>The sandbags on the Altalena's turn (15 June 1948) and the crates on the opening date (15 May 1948, in 1948).</summary>
    [UnityTest]
    public IEnumerator Props()
    {
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        foreach (var (key, city, name, date) in new[] {
            ("tel_aviv_altalena_sandbags", "tel_aviv", "altalena-sandbags-close", SpecialBuildingModels.SandbagsFrom),
            ("haifa_quay_crates", "haifa", "haifa-quay-crates-close", SpecialBuildingModels.Year1948From) })
        {
            var want = SpecialBuildingModels.EveryPlacement.Single(w => w.Key == key);
            Debug.Log(FormattableString.Invariant($"SHOT {name}: {key} covers {want.From:yyyy-MM-dd} to {(want.To is DateTime to ? to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "open")}; the date set is {date:yyyy-MM-dd}"));
            yield return SetDate(date);
            var layer = Special();
            yield return Prop(layer, key, city, name);
        }
    }

    /// <summary>A gdd-0.3 game (every era's ruleset set on fresh inputs, as TalModelsShots.ArmsWorkshop boots it) with one arms
    /// workshop built in each of the four cities on the opening date (a fixture state), each workshop's b21 from 8 km.</summary>
    [UnityTest]
    public IEnumerator WorkshopProps()
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
        // #2876: built three months in (an arms workshop takes three monthly turns), where every major city's edge has ground
        // the Israeli side holds (round Jerusalem, none before the first truce, 11 June 1948).
        yield return SetDate(WorkshopsBuilt);
        var built = new List<(int id, string city)>();
        foreach (var city in Cities)
        {
            var home = content.SettlementHexes.FirstOrDefault(h => h.Id == city);
            if (home == null) { Debug.LogWarning($"SHOT {city}-workshop-close: {city} has no settlement hex; no workshop"); continue; }
            int id = state.NextId++;
            state.CityProjects.Add(id, new CityProject(id, Statecraft.Sim.Workshops.ArmsWorkshop, city, home.Q, home.R, 0, state.Date));
            built.Add((id, city));
        }
        yield return SetDate(state.Date);   // GameRoot redraws with the workshops first, on the same date
        var layer = Special();
        foreach (var (id, city) in built)
            yield return Prop(layer, SpecialBuildingModels.WorkshopKey(id), city, $"{city}-workshop-close");
    }
}
