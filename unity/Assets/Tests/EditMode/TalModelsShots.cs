#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Before/after stills of Tal Heuberger's signed-off models in the real game scene (owner, 27 October 2026, #756: "Sign
/// off all 19"), in English and Hebrew: the game booted as CivMovementShots boots it, the map camera with the UI over it.
/// "Before" withholds the new models (main's map: every layer adds them last, so nothing else moves); "after" answers them.
/// The editor streams nothing, so the scenery layers are rebuilt with the geometry fixture's prefab answers. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class TalModelsShots
{
    internal static string ShotDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "tal-models-shots"));
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedRural, savedPeople, savedSpecial;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-tal-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ShotDir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedRural = RuralLife.Request; savedPeople = CivilianPeople.Request; savedSpecial = SpecialBuildingModels.Request;
    }

    [TearDown]
    public void DiscardScene()
    {
        RuralLife.Request = savedRural!; CivilianPeople.Request = savedPeople!; SpecialBuildingModels.Request = savedSpecial!;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameObject? Prefab(string id, CollectionDetail detail) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));

    internal static Action<string, CollectionDetail, Action<GameObject?>> Answer(ICollection<string> withheld) =>
        (id, detail, done) => done(withheld.Contains(id) ? null : Prefab(id, detail));

    static void Drop(MapScene map, string name)
    {
        var old = map.Terrain.transform.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
    }

    internal static void Look(Vector3 at, float distance)
    {
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Distance = distance; rig.Yaw = 0f;
        rig.Apply();
    }

    IEnumerator Boot(bool hebrew)
    {
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
    }

    IEnumerator Bedouin(bool hebrew, bool after)
    {
        yield return Boot(hebrew);
        var map = Object.FindFirstObjectByType<MapScene>();
        var withheld = after ? new string[0] : new[] { CollectionCatalog.BedouinTent, CollectionCatalog.BedouinCivilian };
        RuralLife.Request = CivilianPeople.Request = Answer(withheld);
        Drop(map, RuralLife.Name); Drop(map, CivilianPeople.Name);
        var rural = CollectionGeometryFixture.Rural(map)!;
        CollectionGeometryFixture.People(map, root.Bridge!.State.Date, Array.Empty<Placement>(), root.Bridge.Content);
        // The first camel scene the layer placed: the same camp before and after (the tent and the figure come last).
        var camel = rural.Placed.First(p => p.Model == RuralLife.Camel && p.Detail == CollectionDetail.Near);
        Look(camel.At, 5000f);
        for (int i = 0; i < 5; i++) yield return null;
        CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"bedouin-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
        Debug.Log($"SHOT bedouin camp {camel.Key} at {camel.At}");
    }

    /// <summary>Degania Alef after the first turn (15 June 1948: the tank stands from 20 May), the camera on the kibbutz's own point,
    /// so the frame is the same with and without the tank.</summary>
    IEnumerator Degania(bool hebrew, bool after)
    {
        yield return Boot(hebrew);
        root.Bridge!.EndTurn();
        for (int i = 0; i < 3; i++) yield return null;
        var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        var close = ui.Q<Button>("turn-report-close");
        if (close != null && close.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(close);
        for (int i = 0; i < 3; i++) yield return null;
        var map = Object.FindFirstObjectByType<MapScene>();
        SpecialBuildingModels Layer(ICollection<string> withheld)
        {
            SpecialBuildingModels.Request = Answer(withheld);
            Drop(map, SpecialBuildingModels.Name);
            return CollectionGeometryFixture.Special(map, root.Bridge.State.Date)!;
        }
        // The frame is where the tank stands on this date with the model answered, the same before and after.
        var layer = Layer(Array.Empty<string>());
        var tank = layer.Placed.First(p => p.Model == CollectionCatalog.SyrianR35);
        if (!after) layer = Layer(new[] { CollectionCatalog.SyrianR35 });
        var point = tank.At;
        point.y = map.Terrain.SampleHeight(point) + map.Terrain.transform.position.y;
        // The closest the player's camera comes (CameraController.MinDistance).
        Look(point, CameraController.MinDistance);
        for (int i = 0; i < 5; i++) yield return null;
        CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"degania-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
        Debug.Log($"SHOT degania on {root.Bridge.State.Date:yyyy-MM-dd}: {string.Join(", ", layer.Placed.Select(p => p.Key))}");
    }

    [UnityTest] public IEnumerator DeganiaBeforeEnglish() { yield return Degania(false, false); }
    [UnityTest] public IEnumerator DeganiaAfterEnglish() { yield return Degania(false, true); }
    [UnityTest] public IEnumerator DeganiaBeforeHebrew() { yield return Degania(true, false); }
    [UnityTest] public IEnumerator DeganiaAfterHebrew() { yield return Degania(true, true); }

    [UnityTest] public IEnumerator BedouinBeforeEnglish() { yield return Bedouin(false, false); }
    [UnityTest] public IEnumerator BedouinAfterEnglish() { yield return Bedouin(false, true); }
    [UnityTest] public IEnumerator BedouinBeforeHebrew() { yield return Bedouin(true, false); }
    [UnityTest] public IEnumerator BedouinAfterHebrew() { yield return Bedouin(true, true); }
}
