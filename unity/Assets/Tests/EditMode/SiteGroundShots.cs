#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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

/// <summary>Writes the before and after stills for #2877 to `.superpowers/shots-2877/&lt;SHOTS_TAG&gt;/` at the repository root
/// (local, not committed; SHOTS_TAG from the environment, "untagged" when unset). Every field site and the Hula swamp are put in
/// the game's own state (State.Sites, SiteWork.Built) and GameRoot draws them on the bridge's Changed, so each still is the game
/// at 1920x1080: the map camera with the UI drawn over it. Poses: the gameplay pose (60 km, yaw 10) on the promo's own ground and
/// on each kind of site, and closer (31 km, 15 km). It uses only calls main has, so the same file gives both sides. Explicit, so
/// it does not run with the checks. Run (Unity, the unity/ project):
/// <code>SHOTS_TAG=after &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter SiteGroundShots</code></summary>
[Explicit("writes stills for a pull request, not a check")]
public class SiteGroundShots
{
    const int Width = 1920, Height = 1080;
    static string ShotDir
    {
        get
        {
            var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2877", string.IsNullOrEmpty(tag) ? "untagged" : tag));
        }
    }
    static readonly string[] Ours = { "mefalsim_field", "beit_kama_field", "reim_field", "kiryat_shmona_field", "beit_shemesh_field", "gilat_field", "beit_kama", "hula" };
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-site-ground-shots-" + Guid.NewGuid().ToString("N"));
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

    /// <summary>CivMovementShots.Boot at this rig's size, with the camera's aspect set first (issue #2882).</summary>
    IEnumerator Boot(bool hebrew)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        ShotCamera.Configure(Camera.main, Width, Height);
        rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        if (hebrew) root.Language = UiLanguage.Hebrew;
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));
        yield return Frames(3);
        var bridge = root.Bridge!;
        foreach (var id in Ours) bridge.State.Sites[id] = SiteWork.Built(id, bridge.State.Date);
        foreach (var id in Ours) if (!ReferenceEquals(bridge.State, bridge.TurnStart)) bridge.TurnStart.Sites[id] = SiteWork.Built(id, bridge.State.Date);
        bridge.Skip(-2877100);
        yield return Frames(5);
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

    IEnumerator Shot(string name, double lon, double lat, float distance, float yaw)
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var at = map.Frame.ToWorld(lon, lat, 0);
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Distance = distance; rig.Yaw = yaw; rig.Apply();
        for (int i = 0; i < 8; i++) { cam.Render(); yield return null; }
        var frame = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = frame; cam.Render(); cam.targetTexture = before;
        var mapPx = Read(frame); var over = Read(rt!);
        frame.Release(); Object.DestroyImmediate(frame);
        var outPx = new Color[mapPx.Length];
        for (int i = 0; i < mapPx.Length; i++)
        {
            var u = over[i]; var m = mapPx[i];
            outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f);
        }
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log(FormattableString.Invariant($"SHOT {name}: rig {rig.Distance:F0} m, aspect {cam.aspect:F3}, m00/m11 {cam.projectionMatrix[0, 0] / cam.projectionMatrix[1, 1]:F3}, frame {Width}x{Height}"));
    }

    IEnumerator OnHex(string name, int q, int r, float distance, float yaw = 10f)
    {
        var hex = MapFocusTests.Inputs.Content.HexAt(q, r)!;
        yield return Shot(name, hex.Lon, hex.Lat, distance, yaw);
    }

    [UnityTest]
    public IEnumerator English()
    {
        yield return Boot(false);
        var p = PromoPose.Gameplay03;
        yield return Shot("gameplay03", p.Lon, p.Lat, p.Distance, p.Yaw);
        yield return OnHex("beitkama-60km-gameplay03", -9, 32, 60000f);
        yield return OnHex("beitkama-31km", -9, 32, 31000f);
        yield return OnHex("beitkama-15km", -9, 32, 15000f);
        yield return OnHex("mefalsim-sea-60km-gameplay03", -11, 31, 60000f);
        yield return OnHex("reim-gilat-desert-31km", -12, 34, 31000f);
        yield return OnHex("beitshemesh-hills-town-31km", -4, 27, 31000f);
        yield return OnHex("beitshemesh-hills-town-15km", -4, 27, 15000f);
        yield return OnHex("hula-60km-gameplay03", 16, 3, 60000f);
        yield return OnHex("hula-31km", 16, 3, 31000f);
        yield return OnHex("hula-15km", 16, 3, 15000f);
        yield return OnHex("kiryatshmona-15km", 16, 2, 15000f);
    }

    [UnityTest]
    public IEnumerator Hebrew()
    {
        yield return Boot(true);
        yield return OnHex("he-beitkama-60km-gameplay03", -9, 32, 60000f);
    }
}
