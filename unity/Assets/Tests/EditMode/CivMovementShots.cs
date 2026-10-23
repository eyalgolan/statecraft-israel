#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the stills for #1398's Civ-style movement (U1 to U5), in English and Hebrew, to
/// `.superpowers/civ-1398-shots/` at the repository root (local, not committed). It uses only calls main had before the
/// change, so the same file run on main gives the "before" stills and on the branch the "after" ones, from the same
/// game state, camera and zoom: Golani's card on the first turn, its attack on the Syrian unit beside it, a move by the
/// next unit, and the Why screen after the turn. Each still is the map camera with the UI drawn over it. Explicit, so it
/// does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class CivMovementShots
{
    internal const int Width = 1280, Height = 720;
    string saveDir = "";
    internal static string ShotDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "civ-1398-shots"));
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-civ-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ShotDir);
    }

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>Opens the game scene in the language, with the UI drawn into a transparent texture (Shot puts it over the
    /// map camera's frame), the opening card hidden and the first-turn guide closed.</summary>
    internal static IEnumerator Boot(bool hebrew, string saveDir, Action<RenderTexture, PanelSettings, VisualElement, GameRoot> booted)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        if (hebrew) root.Language = UiLanguage.Hebrew;
        root.Begin(MapFocusTests.Inputs, 0);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        if (root.Guide != null && root.Guide.IsOpen) UiTestKit.Press(ui.Q<Button>("guide-skip"));
        yield return Frames(3);
        booted(rt, ps, ui, root);
    }

    /// <summary>Points the camera straight at the sim hex from <paramref name="distance"/> metres.</summary>
    internal static void Aim(int q, int r, float distance)
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var hex = MapFocusTests.Inputs.Content.HexAt(q, r)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = distance;
        rig.Apply();
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

    /// <summary>The map camera's frame with the UI texture over it (the UI's pixels are premultiplied by their alpha).</summary>
    internal static void Shot(RenderTexture uiTexture, string name)
    {
        var cam = Camera.main;
        var frame = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = frame; cam.Render(); cam.targetTexture = before;
        var map = Read(frame); var over = Read(uiTexture);
        frame.Release(); Object.DestroyImmediate(frame);
        var outPx = new Color[map.Length];
        for (int i = 0; i < map.Length; i++)
        {
            var u = over[i]; var m = map[i];
            outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f);
        }
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("SHOT " + name);
    }

    IEnumerator Shots(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        var bridge = root.Bridge!; var units = root.Units!; var info = root.Info!;
        var map = Object.FindFirstObjectByType<MapScene>();
        var c = bridge.Content;
        string RowOf(int id) => units.Tokens.First(t => UnitCard.SimUnit(c, bridge.State, t.Token) == id).Token.Unit.Id;
        (int q, int r) HexOf(int id) => (bridge.State.Units[id].Q, bridge.State.Units[id].R);

        // 1. Golani's card on the first turn: the End turn button, the card's lines and its orders.
        var golani = HexOf(0);
        Aim(golani.q, golani.r, 40000f);
        GameRoot.OnHexClicked(map, units, info, golani.q, golani.r);
        yield return Frames(5);
        Shot(rt!, $"1-card-{lang}");

        // 2. Golani attacks the Syrian unit beside it (as UnitOrdersTests.AnAttackShowsItsResultAtOnce).
        var syrian = bridge.State.Units.Values.First(x => x.Side == "syria" && MapActions.Distance(x.Q, x.R, golani.q, golani.r) == 1);
        UiTestKit.Press(ui.Q<Button>("info-attack"));
        yield return Frames(2);
        GameRoot.OnHexClicked(map, units, info, syrian.Q, syrian.R);
        yield return Frames(2);
        Aim(golani.q, golani.r, 40000f);   // the same camera as the card's still (the branch's cycle moves it on)
        yield return Frames(3);
        Shot(rt!, $"2-attack-{lang}");

        // 3. The next Israeli unit with a one-point step moves there.
        var mover = bridge.State.Units.Values.Where(x => x.Id != 0 && x.Side == "israel")
            .OrderBy(x => x.Id)
            .First(x => units.Tokens.Any(t => UnitCard.SimUnit(c, bridge.State, t.Token) == x.Id)
                && Movement.Reachable(c, bridge.State, x.Id).Any(kv => kv.Value == 1)).Id;
        var from = HexOf(mover);
        var step = Movement.Reachable(c, bridge.State, mover).Where(kv => kv.Value == 1).Select(kv => kv.Key).OrderBy(h => h.Q).ThenBy(h => h.R).First();
        Aim(from.q, from.r, 40000f);
        // A click selects the unit; on the branch the attack's cycle has already selected it (a second click shows its hex).
        if (units.Selected != RowOf(mover)) GameRoot.OnHexClicked(map, units, info, from.q, from.r);
        yield return Frames(2);
        UiTestKit.Press(ui.Q<Button>("info-move"));
        yield return Frames(2);
        GameRoot.OnHexClicked(map, units, info, step.Q, step.R);
        yield return Frames(2);
        Aim(from.q, from.r, 40000f);
        yield return Frames(3);
        Debug.Log($"SHOT 3 unit {mover} ({RowOf(mover)}) {from.q},{from.r} -> {step.Q},{step.R}");
        Shot(rt!, $"3-move-{lang}");

        // 4. The turn ends (the bridge's own End turn, which main and the branch both have) and the Why screen opens.
        bridge.EndTurn();
        yield return Frames(3);
        var close = ui.Q<Button>("turn-report-close");
        if (close != null && close.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(close);
        UiTestKit.Press(ui.Q<Button>("nav-why"));
        yield return Frames(5);
        Shot(rt!, $"4-why-{lang}");
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
