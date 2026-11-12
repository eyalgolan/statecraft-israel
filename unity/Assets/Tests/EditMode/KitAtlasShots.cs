#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
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

/// <summary>Writes the before and after stills for #2428 (the town kits' shared atlas) to `.superpowers/shots-2428/&lt;SHOTS_TAG&gt;/`
/// at the repository root (local, not committed), as MajorCityShots writes its own: the map camera's frame with the UI over it,
/// English. <see cref="Close"/> is the game as it draws: the kit-drawn places at zooms where the kit draws. <see cref="KitsOnly"/>
/// switches the town models off (TownModelLayer.Draw) to judge a kit that the models hide up close, and says so in each file's
/// name (`kit-...`). Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class KitAtlasShots
{
    string shotDir = "", saveDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp]
    public void Prepare()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        shotDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2428", string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(shotDir);
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-kit-atlas-shots-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void Discard()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static MapScene Scene => Object.FindFirstObjectByType<MapScene>();

    static Vector3 Ground(Vector3 at)
    {
        at.y = Scene.Terrain.SampleHeight(at) + Scene.Terrain.transform.position.y;
        return at;
    }

    static Vector3 CentreOf(string id, Vector2 offsetM = default)
    {
        var map = Scene;
        var f = map.KitFootprints.FirstOrDefault(x => x.id == id);
        var at = f.id != null ? new Vector3(f.centre.x, 0f, f.centre.y) : map.Frame.ToWorld(map.Settlements.First(x => x.Id == id).Lon, map.Settlements.First(x => x.Id == id).Lat, 0);
        return Ground(at + new Vector3(offsetM.x, 0f, offsetM.y));
    }

    static void Aim(Vector3 at, float viewM, float yaw = 0f)
    {
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Yaw = yaw; rig.Distance = viewM; rig.Apply();
        float down = -Camera.main.transform.forward.y;
        if (down > 1e-4f) { rig.Distance = Mathf.Max(1000f, viewM - at.y / down); rig.Apply(); }
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
        Debug.Log(FormattableString.Invariant($"SHOT {name}: view {TownModelLayer.ViewDistance(cam):F0} m, aspect {cam.aspect:F3}, m11/m00 {cam.projectionMatrix[1, 1] / cam.projectionMatrix[0, 0]:F3}"));
    }

    IEnumerator Take(string name, Vector3 at, float viewM, float yaw = 0f)
    {
        Aim(at, viewM, yaw);
        for (int i = 0; i < 6; i++) { Camera.main.Render(); yield return null; }
        Shot(name);
    }

    /// <summary>A batch run's default aspect is 4:3, which would draw every still 1.33 times too wide: so the camera takes the
    /// still's frame through ShotCamera.Configure (#2882), and each SHOT line states its aspect.</summary>
    IEnumerator Boot()
    {
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; });
        ShotCamera.Configure(Camera.main, CivMovementShots.Width, CivMovementShots.Height);
    }

    [UnityTest]
    public IEnumerator Close()
    {
        yield return Boot();
        Debug.Log("KITATLAS " + Scene.KitStats);
        // The pose the promo is measured at, the start view, then close: the four cities' blocks.
        var pose = PromoPose.Gameplay03;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = Scene.Frame.ToWorld(pose.Lon, pose.Lat, 0); rig.Yaw = pose.Yaw; rig.Distance = pose.Distance; rig.Apply();
        for (int i = 0; i < 6; i++) { Camera.main.Render(); yield return null; }
        Shot("gameplay03");
        foreach (var (city, yaw) in new[] { ("tel_aviv", 0f), ("jerusalem", 0f), ("haifa", 0f), ("jaffa", 0f) })
            foreach (var km in new[] { 31f, 16f, 8f })
                yield return Take(FormattableString.Invariant($"{city}-{km:0}km"), CentreOf(city), km * 1000f, yaw);
        yield return Take("tel_aviv-low-6km", CentreOf("tel_aviv", new Vector2(2500f, 1500f)), 6000f, 30f);
        // Tal Heuberger's clock tower and minaret in Jaffa beside Tel Aviv's first blocks.
        yield return Take("tel_aviv-tal-8km", CentreOf("tel_aviv", new Vector2(-300f, -1300f)), 8000f, 30f);
        yield return Take("jerusalem-west-8km", CentreOf("jerusalem", new Vector2(-2600f, -300f)), 8000f, 20f);
        yield return Take("jerusalem-old-8km", CentreOf("jerusalem", new Vector2(2300f, -2200f)), 8000f, 20f);
        // Tel Aviv's edge towards its neighbours, where the kit's blocks meet Tal Heuberger's models.
        yield return Take("tel_aviv-edge-12km", CentreOf("tel_aviv", new Vector2(4200f, -1800f)), 12000f);
    }

    [UnityTest]
    public IEnumerator KitsOnly()
    {
        yield return Boot();
        var towns = Scene.GetComponentInChildren<TownModelLayer>(true);
        towns.Draw = false;
        foreach (var (id, km) in new[] { ("nazareth", 14f), ("gaza", 14f), ("acre", 14f), ("petah_tikva", 14f), ("nahalal", 14f), ("degania_alef", 14f), ("rishon_lezion", 14f), ("qalqilya", 14f), ("beersheba", 14f) })
        {
            if (!Scene.KitFootprints.Any(f => f.id == id)) { Debug.LogWarning("SHOT kit-" + id + ": no footprint"); continue; }
            yield return Take(FormattableString.Invariant($"kit-{id}-{km:0}km"), CentreOf(id), km * 1000f);
        }
    }
}
