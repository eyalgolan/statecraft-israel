#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Routes;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the before and after stills for issue #2917, part B (paths between the villages), to
/// `.superpowers/shots-2917-paths/&lt;SHOTS_TAG&gt;/` at the repository root (local, not committed; SHOTS_TAG from the
/// environment, "untagged" when unset), as FurrowShots does for part A. Each pose is shot twice in one run, in the game's
/// opening state, with the "paths" toggle on (`&lt;pose&gt;-after.png`) and off (`&lt;pose&gt;-before.png`), so a pair differs
/// only by the change. Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class PathShots
{
    static readonly (string name, double lon, double lat, float km, float yaw)[] Poses =
    {
        ("latrun31", 34.98, 31.88, 31f, 0f), ("latrun12", 34.98, 31.86, 12f, 0f), ("latrun6", 34.99, 31.85, 6f, 0f),
        ("rantis12", 34.98, 32.00, 12f, 0f), ("rantis6", 34.99, 32.01, 6f, 0f),
        ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance / 1000f, PromoPose.Gameplay03.Yaw),
        ("diorama02", PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, PromoPose.Diorama02.Distance / 1000f, PromoPose.Diorama02.Yaw),
    };

    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    bool async;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-path-shots-" + Guid.NewGuid().ToString("N"));
        async = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
    }

    [TearDown]
    public void DiscardScene()
    {
        ShaderUtil.allowAsyncCompilation = async;
        VisualToggles.SetAll(true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>The map camera's frame at the game's 1920 x 1080, without the UI.</summary>
    static void Shot(string name, string dir)
    {
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
        Debug.Log($"SHOT {name}");
    }

    [UnityTest]
    public IEnumerator WriteStills()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2917-paths", string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(dir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; });
        yield return Frames(5);
        // CameraRig.Apply freezes the projection at the camera's own aspect, and a batch run's default is 4:3 (GroundDetailShots).
        Camera.main.aspect = 16f / 9f;
        var rig = Camera.main.GetComponent<CameraRig>();
        var map = Object.FindFirstObjectByType<MapScene>();
        rig.LookAt = CameraRig.OpeningLookAt(map.Frame, Camera.main.aspect, map.Settlements.First(s => s.Id == "tel_aviv"), map.Settlements.First(s => s.Id == "jaffa"));
        rig.Apply();
        yield return Frames(3);
        foreach (var on in new[] { true, false })
        {
            VisualToggles.Set(RouteLayer.PathsToggle, on);
            Shot("opening31-" + (on ? "after" : "before"), dir);
        }
        foreach (var (name, lon, lat, km, yaw) in Poses)
        {
            var at = map.Frame.ToWorld(lon, lat, 0);
            at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
            rig.LookAt = at; rig.Distance = km * 1000f; rig.Yaw = yaw; rig.Apply();
            yield return Frames(3);
            foreach (var on in new[] { true, false })
            {
                VisualToggles.Set(RouteLayer.PathsToggle, on);
                yield return Frames(1);
                Shot(name + "-" + (on ? "after" : "before"), dir);
            }
        }
    }
}
