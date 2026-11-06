#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

/// <summary>Writes the before and after stills for #2812 ("Ground change only", PR #2872) to `.superpowers/shots-2812/` at the
/// repository root (local, not committed), as `&lt;state&gt;-&lt;lang&gt;.png` in English (en) and Hebrew (he):
/// <list type="bullet">
/// <item>`both-built`, the main shot: Beit Kama (beit_kama, [yishuv]) and Beit Kama fields (beit_kama_field, [field]) both
/// built, both at hex (-9, 32). Before: two kits or boxes and two pills on the hex. After: one of each, the yishuv's.</item>
/// <item>`yishuv-only`, for context: Beit Kama built alone, the same camera.</item>
/// <item>`hula-drained`: the Hula swamp (hula, [swamp]) built (drained), at its hex (16, 3). Before: a kit or box and a
/// pill. After: neither.</item>
/// </list>
/// The sites are put in the game's own state (State.Sites, SiteWork.Built, as SiteLayerTests and SiteLabelTests build it)
/// and drawn by GameRoot itself: its RenderSitesIfChanged (SiteLayer.Render, then the labels' SetSites) runs on the
/// bridge's Changed, raised here with SimBridge.Skip on an id no unit has. The camera looks at the hex from 15 km with yaw 0
/// and the game's own top inset (so the game's tilt-shift). Each still is the map camera's frame with the UI drawn over it
/// (CivMovementShots.Boot, Aim's ground point, and its Shot's alpha composite). It uses only calls main has, so the same
/// file gives the "before" stills on main and the "after" ones on the branch. Run (Unity, batch mode, the unity/ project):
/// <code>-runTests -testPlatform EditMode -testFilter SiteFieldShots</code>
/// at the PR's head for AFTER, and with this file (and its .meta) copied onto main for BEFORE. Explicit, so it does not run
/// with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class SiteFieldShots
{
    const float Distance = 15000f;
    static string ShotDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2812"));
    static readonly string[] Ours = { "beit_kama", "beit_kama_field", "hula" };
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;
    int poke = -2812000;   // ids no unit has, for SimBridge.Skip to raise Changed (each id only once)

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-site-shots-" + Guid.NewGuid().ToString("N"));
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

    /// <summary>Only <paramref name="built"/> of our three sites are built (others' records are left alone), in the state the
    /// map shows and the turn's start (the same object at a turn's start), then GameRoot redraws the sites.</summary>
    void Build(params string[] built)
    {
        var bridge = root.Bridge!;
        foreach (var s in new HashSet<GameState> { bridge.State, bridge.TurnStart })
        {
            foreach (var id in Ours) s.Sites.Remove(id);
            foreach (var id in built) s.Sites[id] = SiteWork.Built(id, s.Date);
        }
        bridge.Skip(poke--);
    }

    /// <summary>CivMovementShots.Aim's ground point, with yaw 0; the top inset is left as the game set it.</summary>
    static void Aim(int q, int r)
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var hex = MapFocusTests.Inputs.Content.HexAt(q, r)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = Distance; rig.Yaw = 0f;
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

    /// <summary>CivMovementShots.Shot into this still's folder: the map camera's frame with the UI texture over it (the UI's
    /// pixels are premultiplied by their alpha).</summary>
    static void Shot(RenderTexture uiTexture, string name)
    {
        int w = CivMovementShots.Width, h = CivMovementShots.Height;
        var cam = Camera.main;
        var frame = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
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
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("SHOT " + name);
    }

    IEnumerator Take(string name, int q, int r, params string[] built)
    {
        Build(built);
        yield return Frames(3);
        Aim(q, r);
        yield return Frames(8);   // the kits stream in and the labels place and declutter
        Shot(rt!, name);
    }

    IEnumerator Shots(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        yield return Take($"both-built-{lang}", -9, 32, "beit_kama", "beit_kama_field");
        yield return Take($"yishuv-only-{lang}", -9, 32, "beit_kama");
        yield return Take($"hula-drained-{lang}", 16, 3, "hula");
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
