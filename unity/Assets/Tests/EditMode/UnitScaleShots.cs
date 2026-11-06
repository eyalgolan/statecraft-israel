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
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the before and after stills for #2430 (unit figure scale and base size at the gameplay zoom) to
/// `.superpowers/shots-2430/&lt;SHOTS_TAG&gt;/` at the repository root (local, not committed; SHOTS_TAG from the environment,
/// "untagged" when unset). It uses only calls main has, so the same file gives both sides from the same state and camera. Each
/// still is the game's camera at 1920x1080 with the game's UI drawn over it (CivMovementShots.Shot's composite): the opening
/// view, then one army token of each side at 12, 31, 45 and 60 km (the rig's distance, yaw 10 as PromoPose.Gameplay03), then a
/// town's token and the selected and focused token; DatedTokens adds the Lebanese Army and a token with vehicles. Every figure, weapon and town model is answered from the
/// AssetDatabase, so the stills show what a player gets once the models have streamed. Explicit, so it does not run with the
/// checks. Run (Unity, the unity/ project):
/// <code>SHOTS_TAG=after &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter UnitScaleShots</code></summary>
[Explicit("writes stills for a pull request, not a check")]
public class UnitScaleShots
{
    const int Width = 1920, Height = 1080;
    static readonly string[] Sides = { "israel", "egypt", "syria", "jordan", "iraq", "lebanon" };
    static readonly float[] Km = { 12f, 31f, 45f, 60f };
    string shotDir = "", saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    VisualElement ui = null!;
    Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? savedTown;
    Action<string, CollectionDetail, Action<GameObject?>>? savedFigures;
    Action<string, Action<GameObject?>>? savedWeapons;

    sealed class NoLease : IDisposable { public void Dispose() { } }
    static GameObject? Prefab(string id, CollectionDetail detail) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));
    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    static MapScene Scene => Object.FindFirstObjectByType<MapScene>();

    [SetUp]
    public void Prepare()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        shotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2430", string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(shotDir);
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-unit-scale-shots-" + Guid.NewGuid().ToString("N"));
        savedTown = TownModelLayer.ModelAcquire; savedFigures = UnitFigures.Source; savedWeapons = UnitWeapons.Source;
        TownModelLayer.ModelAcquire = (id, detail, done) => { done(Prefab(id, detail)); return new NoLease(); };
        UnitFigures.Forget();
        UnitFigures.Source = (id, detail, done) => done(Prefab(id, detail));
        UnitWeapons.Source = (id, done) => done(id == CollectionCatalog.C06PointFire ? null : Prefab(id, CollectionDetail.Far));
    }

    [TearDown]
    public void DiscardScene()
    {
        TownModelLayer.ModelAcquire = savedTown!; UnitFigures.Source = savedFigures; UnitWeapons.Source = savedWeapons; UnitFigures.Forget();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    /// <summary>CivMovementShots.Boot at this rig's size.</summary>
    IEnumerator Boot()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        Camera.main.aspect = (float)Width / Height;   // CameraRig.Apply freezes the projection at the camera's own aspect: a batch run's
                                                      // default is 4:3, which drew every still 1.33 times too wide
        rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Scene.Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));
        yield return Frames(3);
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

    /// <summary>Settles the near models and the labels on the camera, then writes the composite.</summary>
    IEnumerator Shot(string name)
    {
        var cam = Camera.main; var units = root.Units!;
        for (int i = 0; i < 8; i++)
        {
            units.FaceCamera(cam); UnitFlagFacing.FaceAll(cam.transform.position);
            foreach (var a in units.GetComponentsInChildren<Animator>(true)) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; if (a.isActiveAndEnabled) a.Update(0f); }
            cam.Render();
            yield return null;
        }
        var frame = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
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
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log(FormattableString.Invariant($"SHOT {name}: rig {cam.GetComponent<CameraRig>().Distance:F0} m, aspect {cam.aspect:F3}, m00/m11 {cam.projectionMatrix[0, 0] / cam.projectionMatrix[1, 1]:F3}"));
    }

    static void Aim(Vector3 at, float distance, float yaw = 10f)
    {
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Distance = distance; rig.Yaw = yaw; rig.Apply();
    }

    /// <summary>A token's ground point, its base and its figures' screen height on the current camera.</summary>
    static string Measure(PlacedToken p)
    {
        var map = Scene; var cam = Camera.main;
        var token = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/" + p.Token.Unit.Id);
        var plinth = token.Find(UnitPlinth.PlinthName);
        var at = UnitShowcase.Ground(map, new Vector3((float)p.Spot.X, 0f, (float)p.Spot.Z));
        float Px(Vector3 a, Vector3 b) => (cam.WorldToScreenPoint(a) - cam.WorldToScreenPoint(b)).magnitude * Height / cam.pixelHeight;
        var box = token.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Figure") || r.name.StartsWith("Rifleman") || r.transform.parent.name == UnitFigures.ModelName).Select(r => r.bounds).ToList();
        float tall = box.Count == 0 ? 0f : box.Max(b => b.size.y);
        float wide = plinth == null ? 0f : 2f * plinth.localScale.x * UnitPlinth.RimOuter;
        return FormattableString.Invariant($"{p.Token.Unit.Id} ({p.Token.Unit.Side}, compact={p.Spot.Compact}, figures={p.Token.Figures}): figure {tall:F0} m = {Px(at, at + Vector3.up * tall):F0} px; ring {wide:F0} m = {Px(at, at + cam.transform.right * wide):F0} px");
    }

    [UnityTest]
    public IEnumerator Zooms()
    {
        yield return Boot();
        var units = root.Units!;
        var map = Scene; var cam = Camera.main;
        Debug.Log("SHOT tokens: " + string.Join(", ", units.Tokens.Select(t => $"{t.Token.Unit.Id}/{t.Token.Unit.Side}/{t.Token.Unit.Service}")));
        yield return Shot("opening");
        foreach (var side in Sides)
        {
            var hit = units.Tokens.Where(t => t.Token.Unit.Side == side && t.Token.Unit.Service == UnitData.Army).OrderBy(t => t.Token.Unit.Id, StringComparer.Ordinal).ToList();
            if (hit.Count == 0) { Debug.LogWarning($"SHOT {side}: no army token at the opening"); continue; }
            var p = hit[0];
            var at = UnitShowcase.Ground(map, new Vector3((float)p.Spot.X, 0f, (float)p.Spot.Z));
            foreach (var km in Km)
            {
                Aim(at, km * 1000f);
                var name = FormattableString.Invariant($"{side}-{km:0}km");
                yield return Shot(name);
                Debug.Log($"SHOT {name}: " + Measure(p));
            }
        }
        // A token with vehicles on its base, if the opening has one.
        var wheels = units.Tokens.Where(t => t.Token.Vehicles.Count > 0).OrderBy(t => t.Token.Unit.Id, StringComparer.Ordinal).ToList();
        Debug.Log("SHOT vehicles: " + string.Join(", ", wheels.Select(t => t.Token.Unit.Id)));
        if (wheels.Count > 0)
        {
            var at = UnitShowcase.Ground(map, new Vector3((float)wheels[0].Spot.X, 0f, (float)wheels[0].Spot.Z));
            foreach (var km in new[] { 12f, 31f, 60f }) { Aim(at, km * 1000f); yield return Shot(FormattableString.Invariant($"vehicles-{km:0}km")); }
        }
        // A token in a town (compact, #588), selected, with the map's keyboard stop focused on it.
        var town = units.Tokens.Where(t => t.Spot.Compact && t.Token.Unit.Service == UnitData.Army).OrderBy(t => t.Token.Unit.Id, StringComparer.Ordinal).FirstOrDefault();
        if (town.Token != null)
        {
            var at = UnitShowcase.Ground(map, new Vector3((float)town.Spot.X, 0f, (float)town.Spot.Z));
            foreach (var km in new[] { 12f, 31f })
            {
                Aim(at, km * 1000f);
                yield return Shot(FormattableString.Invariant($"town-{km:0}km"));
                Debug.Log("SHOT town: " + Measure(town));
                units.Select(town.Token.Unit.Id);
                ui.Q("map-focus").Focus();
                yield return Frames(3);
                yield return Shot(FormattableString.Invariant($"town-selected-{km:0}km"));
                units.Select(null);
            }
        }
    }

    /// <summary>The tokens the opening does not have: the Lebanese Army's and a token with vehicles on its base, on the legacy
    /// dated roster (Generated1948Characters.CampaignStage, the map without the UI), the first date each stands.</summary>
    [UnityTest]
    public IEnumerator DatedTokens()
    {
        var generated = Type.GetType("Statecraft.Game.EditorTools.Generated1948Characters, Assembly-CSharp-Editor")!;
        const System.Reflection.BindingFlags Any = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var (map, layer, state) = ((MapScene map, UnitLayer layer, GameState state))generated.GetMethod("CampaignStage", Any)!.Invoke(null, new object[] { new DateTime(1948, 5, 20) });
        var render = generated.GetMethod("RenderMap", Any)!;
        var cam = Object.FindFirstObjectByType<CameraRig>().GetComponent<Camera>(); var rig = cam.GetComponent<CameraRig>();
        cam.aspect = 16f / 9f;
        var todo = new Dictionary<string, Func<PlacedToken, bool>>
        {
            ["lebanon"] = t => t.Token.Unit.Side == "lebanon" && t.Token.Unit.Service == UnitData.Army,
            ["vehicles"] = t => t.Token.Vehicles.Count > 0,
        };
        foreach (var date in new[] { new DateTime(1948, 5, 20), new DateTime(1948, 6, 5), new DateTime(1948, 7, 12), new DateTime(1948, 10, 15), new DateTime(1948, 12, 4) })
        {
            var s = state.Clone(); s.Date = date; layer.Show(SimBridgeGddTests.Legacy, s);
            foreach (var key in todo.Keys.ToList())
            {
                var hit = layer.Tokens.Where(todo[key]).OrderBy(t => t.Token.Unit.Id, StringComparer.Ordinal).ToList();
                if (hit.Count == 0) continue;
                todo.Remove(key);
                var at = UnitShowcase.Ground(map, new Vector3((float)hit[0].Spot.X, 0f, (float)hit[0].Spot.Z));
                foreach (var km in new[] { 12f, 31f, 60f })
                {
                    rig.LookAt = at; rig.Distance = km * 1000f; rig.Yaw = 10f; rig.TopInset = 0f; rig.Apply();
                    for (int i = 0; i < 8; i++)
                    {
                        layer.FaceCamera(cam); UnitFlagFacing.FaceAll(cam.transform.position);
                        foreach (var a in layer.GetComponentsInChildren<Animator>(true)) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; if (a.isActiveAndEnabled) a.Update(0f); }
                        cam.Render();
                        yield return null;
                    }
                    var name = FormattableString.Invariant($"dated-{key}-{km:0}km");
                    render.Invoke(null, new object[] { cam, Path.Combine(shotDir, name + ".jpg") });
                    Debug.Log(FormattableString.Invariant($"SHOT {name}: {hit[0].Token.Unit.Id} on {date:yyyy-MM-dd}, vehicles {hit[0].Token.Vehicles.Count}"));
                }
            }
        }
        foreach (var key in todo.Keys) Debug.LogWarning($"SHOT {key}: no token on the dates tried");
    }
}
