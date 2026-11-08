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

/// <summary>Writes the before and after stills for #803 (each settlement's control state as a word tag under its name: owner,
/// 24 October 2026, "1521: 1. a 2.a") to `.superpowers/shots-803/&lt;SHOTS_TAG&gt;/` at the repository root (local, not
/// committed; SHOTS_TAG from the environment, "untagged" when unset), in English and Hebrew (GameRoot with its Hebrew
/// language, so UiDirection runs), at 1920x1080 and 390x844. Each still is the map camera's frame with the UI drawn over it,
/// as CivMovementShots draws it. The same opening game in each: four records set on the first turn's state (Acre awaiting
/// the capture choice, Nazareth occupied with 4 turns left, Lydda awaiting the end of its occupation, Ramle depopulated
/// and counted once in DisplacedByPlayer), then Changed raised, as a turn would.
/// <list type="bullet">
/// <item>`centre-&lt;lang&gt;-&lt;w&gt;x&lt;h&gt;.png`: the camera on Lydda's hex from 45 km (Lydda, Ramle, Jaffa).</item>
/// <item>`north-&lt;lang&gt;-&lt;w&gt;x&lt;h&gt;.png`: the camera on Nazareth's hex from 60 km (Nazareth, Acre).</item>
/// <item>`panel-&lt;lang&gt;-&lt;w&gt;x&lt;h&gt;.png`: the centre view with Lydda's hex clicked (its side panel).</item>
/// <item>`support-tip-&lt;lang&gt;-&lt;w&gt;x&lt;h&gt;.png`: the centre view with International support hovered.</item>
/// </list>
/// It uses only calls main has, so the same file run on main gives the "before" stills. Run (Unity, the unity/ project):
/// <code>SHOTS_TAG=after &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter SettlementControlShots</code>
/// then with this file and its .meta copied onto main, SHOTS_TAG=before. Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class SettlementControlShots
{
    string shotDir = "", saveDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp]
    public void Prepare()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        shotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-803",
            string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(shotDir);
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-803-shots-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        rt = null; ps = null;
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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

    /// <summary>The map camera's frame with the UI texture over it (premultiplied by its alpha), as CivMovementShots.Shot.</summary>
    void Shot(int w, int h, string name)
    {
        var cam = Camera.main;
        ShotCamera.Require(cam, w, h);
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
        Debug.Log("SHOT " + name);
    }

    static void Aim(Content c, string settlement, float distance)
    {
        var at = c.SettlementHexes.First(x => x.Id == settlement);
        var map = Object.FindFirstObjectByType<MapScene>();
        var hex = c.HexAt(at.Q, at.R)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = distance; rig.Apply();
    }

    static void Enter(VisualElement e)
    {
        using var ev = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = e.worldBound.center });
        ev.target = e; e.SendEvent(ev);
    }

    IEnumerator Shots(bool hebrew, int w, int h)
    {
        string at = $"{(hebrew ? "he" : "en")}-{w}x{h}";
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        ShotCamera.Configure(Camera.main, w, h);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Language = hebrew ? UiLanguage.Hebrew : UiLanguage.English;
        root.Begin(MapFocusTests.Inputs, 0);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));
        yield return Frames(3);

        var bridge = root.Bridge!; var c = bridge.Content;
        var s = bridge.State.Settlements;
        s["acre"] = new SettlementControl("israel", SettlementControl.AwaitingChoice);
        s["nazareth"] = new SettlementControl("israel", SettlementControl.Occupied, turns: 4);
        s["lod"] = new SettlementControl("israel", SettlementControl.AwaitingEnd);
        s["ramla"] = new SettlementControl(depopulated: true);
        bridge.State.DisplacedByPlayer = 1;
        bridge.Replace(bridge.Session);   // Changed, as after a turn
        yield return Frames(3);

        Aim(c, "lod", 45000f); yield return Frames(6);
        Shot(w, h, "centre-" + at);
        Aim(c, "nazareth", 60000f); yield return Frames(6);
        Shot(w, h, "north-" + at);

        Aim(c, "lod", 45000f); yield return Frames(4);
        var meter = ui.Q("meter-standing");
        Enter(meter);
        yield return Frames(4);
        Shot(w, h, "support-tip-" + at);
        using (var ev = PointerLeaveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = Vector2.zero })) { ev.target = meter; meter.SendEvent(ev); }
        yield return Frames(2);

        var lod = c.SettlementHexes.First(x => x.Id == "lod");
        GameRoot.OnHexClicked(Object.FindFirstObjectByType<MapScene>(), root.Units!, root.Info!, lod.Q, lod.R);
        yield return Frames(6);
        Shot(w, h, "panel-" + at);
    }

    [UnityTest] public IEnumerator EnglishWide() => Shots(false, 1920, 1080);
    [UnityTest] public IEnumerator HebrewWide() => Shots(true, 1920, 1080);
    [UnityTest] public IEnumerator EnglishPhone() => Shots(false, 390, 844);
    [UnityTest] public IEnumerator HebrewPhone() => Shots(true, 390, 844);
}
