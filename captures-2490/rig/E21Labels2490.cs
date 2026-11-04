#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

/// <summary>Throwaway (never committed to a PR): PR #2490 stills. The 15 May 1948 map booted as CivMovementShots boots it
/// (GameRoot.Begin; Hebrew sets root.Language so GameRoot attaches UiDirection), the craft layer built with the prefabs and
/// GameRoot's label keep-clear pointed at it, so the names keep clear of the Dakota loop box as in play. Logs the shown names
/// per view.</summary>
[Explicit("throwaway stills")]
public class E21Labels2490
{
    static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "e21-labels-2490"));
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps; GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedCraft;

    [SetUp] public void S()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "e21-2490-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Dir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedCraft = SeaAirCollection.Request;
    }
    [TearDown] public void T()
    {
        SeaAirCollection.Request = savedCraft!;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static bool Shown(Label l) => l.parent != null && l.parent.style.visibility.value == Visibility.Visible;

    IEnumerator Shot(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        var map = Object.FindFirstObjectByType<MapScene>();
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        Assert.AreEqual(new DateTime(1948, 5, 15), state.Date.Date, "date");
        var rig = Camera.main.GetComponent<CameraRig>(); var cc = rig.GetComponent<CameraController>();
        var bootAt = rig.LookAt; float bootDistance = rig.Distance, bootYaw = rig.Yaw;
        SeaAirCollection.Request = TalModelsShots.Answer(Array.Empty<string>());
        var old = map.Terrain.transform.Find(SeaAirCollection.Name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var layer = CollectionGeometryFixture.Craft(map, state.Date, state.Placements, content)!;
        typeof(GameRoot).GetField("seaAir", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(root, layer);
        var labels = root.Labels!;
        var dakotas = layer.Placed.Where(p => p.Model == SeaAirCollection.Dakota).ToList();
        Assert.Greater(dakotas.Count, 0, "a Dakota is drawn on 15 May 1948");
        Debug.Log($"E21LABELS {lang} meta date={state.Date:yyyy-MM-dd} turn={state.Turn} dakotas={dakotas.Count} keepClear={layer.KeepClear.Count} "
            + $"boxes={string.Join(" | ", layer.KeepClear.SelectMany(b => b).Select(b => $"{b.center} {b.size}"))} boot lookAt={bootAt} d={bootDistance:F0} yaw={bootYaw:F1}");
        var tlv = content.SettlementHexes.First(s => s.Id == "tel_aviv");
        var hex = content.HexAt(tlv.Q, tlv.R)!;
        var city = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        city.y = map.Terrain.SampleHeight(city) + map.Terrain.transform.position.y;
        var loop = new Vector3(dakotas[0].At.x, 0f, dakotas[0].At.z);
        // "turnmap" = the camera as the 15 May turn opens (GameRoot.Begin's view); 60 km = MapLabelTests' gameplay view.
        foreach (var (what, at, distance) in new[] { ("turnmap", bootAt, bootDistance), ("telaviv-60km", city, 60000f), ("loop-60km", loop, 60000f) })
        {
            if (what == "turnmap") { rig.LookAt = bootAt; rig.Distance = bootDistance; rig.Yaw = bootYaw; rig.Apply(); }
            else { TalModelsShots.Look(at, distance); CameraController.Step(rig, default, 0f, cc.Bounds, cc.Ground, cc.GroundTop, true); }
            for (int i = 0; i < 5; i++) yield return null;
            UnitFlagFacing.FaceAll(Camera.main.transform.position);
            labels.Refresh(force: true); yield return null;
            labels.Refresh(force: true); yield return null;
            var shown = new List<string>();
            for (int i = 0; i < labels.Labels.Count; i++) if (Shown(labels.Labels[i])) shown.Add($"S{i}:{labels.Labels[i].text}");
            for (int i = 0; i < labels.LocalityLabels.Count; i++) if (Shown(labels.LocalityLabels[i])) shown.Add($"L{i}:{labels.LocalityLabels[i].text}");
            for (int i = 0; i < labels.LocationLabels.Count; i++) if (Shown(labels.LocationLabels[i])) shown.Add($"M{i}:{labels.LocationLabels[i].text}");
            Debug.Log($"E21LABELS {lang} {what} d={rig.Distance:F0} lookAt={rig.LookAt} shown=[{string.Join(" ; ", shown)}]");
            CivMovementShots.Shot(rt!, Path.Combine("..", "e21-labels-2490", $"labels-{what}-{lang}"));
        }
    }

    [UnityTest] public IEnumerator English() { yield return Shot(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shot(true); }
}
