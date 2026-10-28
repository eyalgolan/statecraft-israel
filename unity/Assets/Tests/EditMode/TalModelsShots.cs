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
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        void Layers(ICollection<string> withheld)
        {
            RuralLife.Request = CivilianPeople.Request = Answer(withheld);
            Drop(map, RuralLife.Name); Drop(map, CivilianPeople.Name);
            // GameRoot's order and inputs: the figures first, then the rural scenes, which keep clear of them.
            CollectionGeometryFixture.People(map, state.Date, state.Placements, content, state.Settlements, state.CityProjects.Values);
            CollectionGeometryFixture.Rural(map);
        }
        // The frames come from the map with the new models, so they are the same before and after: where the tent and the
        // figure stand. Both layers add them last, so nothing else moves when they are withheld.
        Layers(Array.Empty<string>());
        var rural = map.Terrain.transform.Find(RuralLife.Name).GetComponent<RuralLife>();
        var people = map.Terrain.transform.Find(CivilianPeople.Name).GetComponent<CivilianPeople>();
        var tent = rural.Placed.First(p => p.Model == RuralLife.BedouinTent);
        var figure = people.Placed.First(p => p.Model == CivilianPeople.BedouinCivilian);
        // The tent's frame holds the tent and the camel at the same Bedouin-area anchor: the midpoint of the two, from the
        // closest zoom the player can reach or as far as needed to hold both.
        var camel = rural.Placed.First(p => p.Model == RuralLife.Camel && p.Key == tent.Key);
        Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
        float apart = Vector2.Distance(Flat(tent.At), Flat(camel.At));
        var camps = map.KitFootprints.Where(f => f.id.StartsWith("camp:", StringComparison.Ordinal)).ToList();
        float nearestCamp = camps.Count == 0 ? float.NaN : camps.Min(f => Vector2.Distance(Flat(tent.At), f.centre) - f.radius);
        Debug.Log($"SHOT bedouin tent {tent.Key}: camel {apart:F0} m away; nearest code-built camp footprint edge {nearestCamp:F0} m away");
        var frames = new[] { ("tent", tent.Key, (tent.At + camel.At) * 0.5f, Mathf.Max(CameraController.MinDistance, 2.5f * apart)),
                             ("figure", figure.Key, figure.At, CameraController.MinDistance) };
        if (!after)
        {
            Layers(new[] { CollectionCatalog.BedouinTent, CollectionCatalog.BedouinCivilian });
            rural = map.Terrain.transform.Find(RuralLife.Name).GetComponent<RuralLife>();
            people = map.Terrain.transform.Find(CivilianPeople.Name).GetComponent<CivilianPeople>();
        }
        Debug.Log($"SHOT bedouin {(after ? "after" : "before")}: rural tents {rural.Placed.Count(p => p.Model == RuralLife.BedouinTent)}, " +
                  $"figures {people.Placed.Count(p => p.Model == CivilianPeople.BedouinCivilian)}, tent size {RuralLife.SizeOf(RuralLife.BedouinTent)} m");
        foreach (var (what, key, flat, distance) in frames)
        {
            var at = new Vector3(flat.x, 0f, flat.z);
            at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
            Look(at, distance);
            for (int i = 0; i < 5; i++) yield return null;
            CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"bedouin-{what}-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
            Debug.Log($"SHOT bedouin {what} at {key} {at} from {distance:F0} m");
        }
    }

    [UnityTest] public IEnumerator BedouinBeforeEnglish() { yield return Bedouin(false, false); }
    [UnityTest] public IEnumerator BedouinAfterEnglish() { yield return Bedouin(false, true); }
    [UnityTest] public IEnumerator BedouinBeforeHebrew() { yield return Bedouin(true, false); }
    [UnityTest] public IEnumerator BedouinAfterHebrew() { yield return Bedouin(true, true); }
}
