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
    Action<string, CollectionDetail, Action<GameObject?>>? savedRural, savedPeople;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-tal-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ShotDir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedRural = RuralLife.Request; savedPeople = CivilianPeople.Request;
    }

    [TearDown]
    public void DiscardScene()
    {
        RuralLife.Request = savedRural!; CivilianPeople.Request = savedPeople!;
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
        CollectionGeometryFixture.Rural(map);
        CollectionGeometryFixture.People(map, root.Bridge!.State.Date, Array.Empty<Placement>(), root.Bridge.Content);
        // The camps come from the layers' pure plans, so the frames are the same before and after: the first camp the tent
        // stands at, and the first camp of the camels, where the Bedouin civilian stands.
        var plan = RuralLife.Wants(map);
        var tentCamp = plan.First(w => w.Model == RuralLife.BedouinTent);
        var figureCamp = CivilianPeople.Served(plan, RuralLife.Camel).First();
        foreach (var (what, camp) in new[] { ("tent", tentCamp), ("figure", figureCamp) })
        {
            var at = new Vector3(camp.Anchor.x, 0f, camp.Anchor.y);
            at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
            Look(at, 2500f);
            for (int i = 0; i < 5; i++) yield return null;
            CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"bedouin-{what}-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
            Debug.Log($"SHOT bedouin {what} camp {camp.Key} at {at}");
        }
    }

    [UnityTest] public IEnumerator BedouinBeforeEnglish() { yield return Bedouin(false, false); }
    [UnityTest] public IEnumerator BedouinAfterEnglish() { yield return Bedouin(false, true); }
    [UnityTest] public IEnumerator BedouinBeforeHebrew() { yield return Bedouin(true, false); }
    [UnityTest] public IEnumerator BedouinAfterHebrew() { yield return Bedouin(true, true); }
}
