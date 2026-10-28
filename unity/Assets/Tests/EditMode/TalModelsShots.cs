#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
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
    Action<string, CollectionDetail, Action<GameObject?>>? savedRural, savedPeople, savedUnits;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-tal-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ShotDir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedRural = RuralLife.Request; savedPeople = CivilianPeople.Request; savedUnits = UnitFigures.Source;
    }

    [TearDown]
    public void DiscardScene()
    {
        RuralLife.Request = savedRural!; CivilianPeople.Request = savedPeople!; UnitFigures.Source = savedUnits;
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
        // The tent's frame holds the tent and the nearest code-built camp (coordinator decision on #1893: the tent stands in the
        // camp area, apart from the camps' plots): the midpoint of the tent and that camp's footprint, from the closest zoom the
        // player can reach or as far as needed to hold both. Every placed tent's distance to its nearest camp is logged.
        Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
        var camps = map.KitFootprints.Where(f => f.id.StartsWith("camp:", StringComparison.Ordinal)).ToList();
        (Vector2 centre, float radius, float edge) Nearest(Vector3 at) => camps.Select(f => (f.centre, f.radius, Vector2.Distance(Flat(at), f.centre) - f.radius)).OrderBy(c => c.Item3).First();
        foreach (var placed in rural.Placed.Where(p => p.Model == RuralLife.BedouinTent && p.Detail == CollectionDetail.Near))
            Debug.Log($"SHOT bedouin tent {placed.Key}: nearest code-built camp footprint edge {Nearest(placed.At).edge:F0} m away");
        var camp = Nearest(tent.At);
        var campAt = new Vector3(camp.centre.x, 0f, camp.centre.y);
        float span = Vector2.Distance(Flat(tent.At), camp.centre) + camp.radius;
        var frames = new[] { ("tent", tent.Key, (tent.At + campAt) * 0.5f, Mathf.Max(CameraController.MinDistance, 2.5f * span)),
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

    /// <summary>Al-Malikiyya's hex (unit_anchor_hexes.yaml: the Lebanese army's anchor, q 12, r 4) after the first turn (15 June
    /// 1948, inside the token's 6 June to 8 July window), from the unit zoom and from the closest zoom. The unit figures are
    /// answered with the editor's prefabs, as the player's stream would answer them. The same code runs on main for the before.</summary>
    IEnumerator Malikiyya(bool hebrew)
    {
        UnitFigures.Source = (id, detail, done) => done(Prefab(id, detail));
        yield return Boot(hebrew);
        root.Bridge!.EndTurn();
        for (int i = 0; i < 3; i++) yield return null;
        var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        var close = ui.Q<Button>("turn-report-close");
        if (close != null && close.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(close);
        // The new turn opens a decision card over the map; the player's Hide button puts it away, and the card shrinks away.
        var card = ui.Q("event-card");
        Assert.IsNotNull(card, "the event card");
        if (card.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(ui.Q<Button>("event-hide"));
        for (int i = 0; i < 120 && card.resolvedStyle.display != DisplayStyle.None; i++) yield return null;
        Assert.AreEqual(DisplayStyle.None, card.resolvedStyle.display, "the decision card is hidden before the frame");
        const int q = 12, r = 4;
        var here = root.Units!.Tokens.Where(t => t.Q == q && t.R == r).Select(t => $"{t.Token.Unit.Id} ({t.Token.ArtType})").ToList();
        foreach (var (what, distance) in new[] { ("unit", 40000f), ("close", CameraController.MinDistance) })
        {
            CivMovementShots.Aim(q, r, distance);
            for (int i = 0; i < 5; i++) yield return null;
            CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"malikiyya-{what}-{(hebrew ? "he" : "en")}"));
            Debug.Log($"SHOT malikiyya {what} on {root.Bridge.State.Date:yyyy-MM-dd} hex {q},{r} from {distance:F0} m, event card display {card.resolvedStyle.display}: tokens [{string.Join(", ", here)}]");
        }
    }

    [UnityTest] public IEnumerator MalikiyyaEnglish() { yield return Malikiyya(false); }
    [UnityTest] public IEnumerator MalikiyyaHebrew() { yield return Malikiyya(true); }

    [UnityTest] public IEnumerator BedouinBeforeEnglish() { yield return Bedouin(false, false); }
    [UnityTest] public IEnumerator BedouinAfterEnglish() { yield return Bedouin(false, true); }
    [UnityTest] public IEnumerator BedouinBeforeHebrew() { yield return Bedouin(true, false); }
    [UnityTest] public IEnumerator BedouinAfterHebrew() { yield return Bedouin(true, true); }
}
