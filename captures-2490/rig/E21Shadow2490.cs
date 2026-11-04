#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Throwaway (never committed to a PR): PR #2490's Dakota shadow at its worst relief. Samples both planes' loops at
/// SeaAirCollectionTests.EachDakotaCastsAShadowOnTheGround's 24 times (t = i * DakotaLoopSeconds / 24), logs s.y - Height(s)
/// per sample (Height = max(terrain SampleHeight + y0, sea level), as the test defines it), the max with its plane and time,
/// and stills at that time framed on that plane's shadow and the slope under it.</summary>
[Explicit("throwaway stills")]
public class E21Shadow2490
{
    static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "e21-labels-2490"));
    string saveDir = "";
    RenderTexture? rt, blank; PanelSettings? ps; GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedCraft;

    [SetUp] public void S()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "e21-2490s-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Dir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedCraft = SeaAirCollection.Request;
    }
    [TearDown] public void T()
    {
        SeaAirCollection.Request = savedCraft!;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (blank != null) { blank.Release(); Object.DestroyImmediate(blank); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    [UnityTest]
    public IEnumerator WorstRelief()
    {
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        var map = Object.FindFirstObjectByType<MapScene>();
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        Assert.AreEqual(new DateTime(1948, 5, 15), state.Date.Date, "date");
        SeaAirCollection.Request = TalModelsShots.Answer(Array.Empty<string>());
        var old = map.Terrain.transform.Find(SeaAirCollection.Name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var layer = CollectionGeometryFixture.Craft(map, state.Date, state.Placements, content)!;
        var planes = SeaAirCollection.DakotaKeys.Select(k => layer.transform.Find(k)).ToArray();
        Assert.IsTrue(planes.All(p => p != null), "both Dakotas drawn");
        var loops = planes.Select(p => p.GetComponent<DakotaLoop>()).ToArray();
        var shadows = planes.Select(p => layer.transform.Find(p.name + SeaAirCollection.DakotaMarksSuffix)?.Find(SeaAirCollection.DakotaShadowName)).ToArray();
        Assert.IsTrue(shadows.All(s => s != null), "a shadow per Dakota");
        float y0 = map.Terrain.transform.position.y, sea = PlacementLayer.CampGround(map).SeaLevelY;
        float Height(Vector3 q) => Mathf.Max(map.Terrain.SampleHeight(q) + y0, sea);
        var log = new StringBuilder();
        string F(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        log.AppendLine($"# PR #2490 Dakota shadow over the ground, after tree; y0={F(y0)} sea={F(sea)} loop={F(SeaAirCollection.DakotaLoopSeconds)} s; ShadowLiftM={F(SpitfireLoop.ShadowLiftM)}");
        log.AppendLine("# plane\ti\tt_s\ts.y\tg=Height(s)\ts.y-g\tterrainUnder\tplaneY\tshadowSizeX\tshadowSizeZ");
        float best = float.MinValue; int bp = -1; float bt = 0f;
        for (int p = 0; p < planes.Length; p++)
            for (int i = 0; i < 24; i++)
            {
                float t = i * SeaAirCollection.DakotaLoopSeconds / 24f;
                loops[p].Place(t);
                var s = shadows[p]!.position; float g = Height(s), d = s.y - g;
                log.AppendLine($"{planes[p].name}\t{i}\t{F(t)}\t{F(s.y)}\t{F(g)}\t{F(d)}\t{F(map.Terrain.SampleHeight(s) + y0)}\t{F(planes[p].position.y)}\t{F(shadows[p]!.lossyScale.x)}\t{F(shadows[p]!.lossyScale.z)}");
                if (d > best) { best = d; bp = p; bt = t; }
            }
        var line = $"max(s.y - g) = {F(best)} m: {planes[bp].name} at t = {F(bt)} s";
        log.AppendLine("# " + line);
        Debug.Log("E21SHADOW " + line);
        foreach (var l in loops) l.Place(bt);
        var at = shadows[bp]!.position;
        log.AppendLine($"# shadow at {at} (world), shadow lossyScale {shadows[bp]!.lossyScale}, plane at {planes[bp].position}");
        File.WriteAllText(Path.Combine(Dir, "shadow-worst-relief.tsv"), log.ToString());
        // A blank UI texture for stills without the HUD and the names over the disc.
        blank = new RenderTexture(CivMovementShots.Width, CivMovementShots.Height, 24, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active; RenderTexture.active = blank; GL.Clear(true, true, Color.clear); RenderTexture.active = prev;
        var rig = Camera.main.GetComponent<CameraRig>();
        float size = Mathf.Max(shadows[bp]!.lossyScale.x, shadows[bp]!.lossyScale.z);
        foreach (var (what, distance, yaw, ui) in new[] { ("close", Mathf.Max(6f * size, 4000f), 0f, false), ("close-east", Mathf.Max(6f * size, 4000f), 90f, false), ("wide", 20000f, 0f, true) })
        {
            rig.LookAt = at; rig.Distance = distance; rig.Yaw = yaw; rig.Apply();
            for (int i = 0; i < 5; i++) yield return null;
            foreach (var l in loops) l.Place(bt);
            UnitFlagFacing.FaceAll(Camera.main.transform.position);
            CivMovementShots.Shot(ui ? rt! : blank, Path.Combine("..", "e21-labels-2490", $"shadow-{what}"));
            Debug.Log($"E21SHADOW still {what} d={distance:F0} yaw={yaw:F0} camera={Camera.main.transform.position} shadow viewport={Camera.main.WorldToViewportPoint(at)}");
        }
    }
}
