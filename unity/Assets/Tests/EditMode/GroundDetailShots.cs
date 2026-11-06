#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the before and after stills for issue #2878 (ground detail and scatter between the towns) to
/// `.superpowers/shots-2878/&lt;SHOTS_TAG&gt;/` at the repository root (local, not committed; SHOTS_TAG from the environment,
/// "untagged" when unset). Each still is the map camera's frame with the UI drawn over it (CivMovementShots.Shot), in the
/// game's opening state, so a pair differs only by the change. The poses: the opening itself (the rig as the game leaves
/// it), the same look-at at 12 km, the land between the towns at 31 and 12 km, the hills, the desert, the sea, Jerusalem,
/// and the two promo poses. Each gets a `SHOT` line with the tree instances submitted. Explicit, so it does not run with
/// the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class GroundDetailShots
{
    static readonly (string name, double lon, double lat, float km, float yaw)[] Poses =
    {
        ("between31", 34.93, 32.02, 31f, 0f), ("between12", 34.93, 32.02, 12f, 0f),
        ("citrus12", 34.85, 31.95, 12f, 0f),
        ("hills31", 35.05, 31.85, 31f, 0f), ("hills12", 35.05, 31.76, 12f, 0f),
        ("galilee31", 35.30, 32.85, 31f, 0f),
        ("desert31", 34.85, 30.90, 31f, 0f), ("desert12", 34.90, 30.60, 12f, 0f),
        ("sea31", 34.72, 32.40, 31f, 0f),
        ("jerusalem12", 35.2256, 31.7789, 12f, 0f),
        ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance / 1000f, PromoPose.Gameplay03.Yaw),
        ("diorama02", PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, PromoPose.Diorama02.Distance / 1000f, PromoPose.Diorama02.Yaw),
    };

    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp]
    public void Prepare() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-ground-shots-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>The map camera's frame at the game's 1920 x 1080, without the UI.</summary>
    static void Shot(string name, string dir)
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var layer = map.GetComponentInChildren<VegetationLayer>(true);
        var detail = map.GetComponentInChildren<GroundDetail>(true);
        var cam = Camera.main;
        var frame = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = frame; cam.Render(); cam.targetTexture = before;
        var prev = RenderTexture.active; RenderTexture.active = frame;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
        RenderTexture.active = prev; frame.Release(); Object.DestroyImmediate(frame);
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log(FormattableString.Invariant($"SHOT {name} trees planted {layer.Count} drawn {layer.LastDrawn} scrub tiles {detail?.LastTiles}"));
    }

    /// <summary>The frame with the UI over it, at the UI texture's size (CivMovementShots.Shot).</summary>
    static void UiShot(string name, string dir)
    {
        CivMovementShots.Shot(UiTexture!, name);
        var from = Path.Combine(CivMovementShots.ShotDir, name + ".png");
        File.Copy(from, Path.Combine(dir, name + ".png"), true); File.Delete(from);
    }

    static RenderTexture? UiTexture;

    [UnityTest]
    public IEnumerator WriteStills()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2878", string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(dir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; });
        UiTexture = rt;
        yield return Frames(5);
        var rig = Camera.main.GetComponent<CameraRig>();
        var start = rig.LookAt;
        Shot("opening31", dir); UiShot("opening31-ui", dir);
        rig.Distance = 12000f; rig.Apply();
        yield return Frames(3);
        Shot("opening12", dir);
        var map = Object.FindFirstObjectByType<MapScene>();
        foreach (var (name, lon, lat, km, yaw) in Poses)
        {
            var at = map.Frame.ToWorld(lon, lat, 0);
            at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
            rig.LookAt = at; rig.Distance = km * 1000f; rig.Yaw = yaw; rig.Apply();
            yield return Frames(3);
            Shot(name, dir);
        }
        rig.LookAt = start;
    }
}
