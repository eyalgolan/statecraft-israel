#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Before/after stills of #2083 and #2084 in the real game scene, English and Hebrew: the game booted as
/// TalModelsShots boots it (15 May 1948, turn 0), the unit figures answered with the editor's prefabs as the player's stream
/// answers them. "Before" withholds Tal Heuberger's two weapons and the C06 point-fire pose source (main's map); "after"
/// answers them. Each frame is close zoom (1.25 x the closest the player can reach), on the token's centre raised by half a
/// figure's height, yaw 0. The attack frame
/// plays the attack as the game does (UnitTokenMotion.PlayAttack; the layer adds the motion only in play mode, so the still
/// adds it the same way) and advances it to half its length; an Animator does not run in edit mode, so every figure's
/// Animator is advanced by hand by the same time. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class TalWeaponsShots
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedUnits;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-weapon-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TalModelsShots.ShotDir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedUnits = UnitFigures.Source;
    }

    [TearDown]
    public void DiscardScene()
    {
        UnitFigures.Source = savedUnits; UnitFigurePointFire.Forget();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static readonly string[] New = { CollectionCatalog.K98k, CollectionCatalog.Sten, CollectionCatalog.C06PointFire };

    IEnumerator Shot(bool hebrew, bool after, string what)
    {
        UnitFigures.Source = TalModelsShots.Answer(after ? Array.Empty<string>() : New);
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        Camera.main.aspect = (float)CivMovementShots.Width / CivMovementShots.Height;
        var map = Object.FindFirstObjectByType<MapScene>();
        // The subject: Harel's token for the Sten (its third figure is the Palmach scout), else the first token with a sim
        // unit whose figures include C06 (the steel helmet), by id.
        var subject = what == "sten"
            ? root.Units!.Tokens.First(t => t.Token.Unit.Id == "harel")
            : root.Units!.Tokens.Where(t => t.Token.SimId != null && t.Token.Headgear.Contains(UnitData.SteelHelmet))
                .OrderBy(t => t.Token.Unit.Id, StringComparer.Ordinal).First();
        var token = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/" + subject.Token.Unit.Id);
        Assert.IsNotNull(token, subject.Token.Unit.Id);
        var at = new Vector3((float)subject.Spot.X, 0f, (float)subject.Spot.Z);
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        // Close zoom (inside the 40 km near band) on the figures' chests rather than their feet, so the whole trio and its
        // hands are in the frame: half a figure's height above the token, a quarter farther than the closest zoom.
        float distance = CameraController.MinDistance * 1.25f;
        var aim = at + Vector3.up * (UnitBadge.FigureHeightM * UnitShowcase.FigureScale * 0.5f);
        TalModelsShots.Look(aim, distance);
        for (int i = 0; i < 5; i++) { Camera.main.Render(); yield return null; }   // the near figures come in at close zoom
        string detail = "";
        if (what == "attack")
        {
            var motion = token.GetComponent<UnitTokenMotion>() ?? token.gameObject.AddComponent<UnitTokenMotion>();
            bool played = motion.PlayAttack();
            var fire = token.GetComponentInChildren<UnitFigurePointFire>();
            float half = 0f;
            foreach (var a in token.GetComponentsInChildren<Animator>())
                if (UnitFigureMotion.CanPlay(a, UnitFigureMotion.Attack)) half = Mathf.Max(half, UnitTokenMotion.FireLength(a) / 2f);
            if (fire != null && fire.Ready) half = Mathf.Max(half, fire.FireLength / 2f);
            motion.Tick(half);
            foreach (var a in token.GetComponentsInChildren<Animator>()) if (a.isActiveAndEnabled) a.Update(half);
            detail = $" attack played={played} at {half:F3} s, aim holding={(fire != null && fire.Holding)}";
            for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
        }
        var held = token.GetComponentsInChildren<Transform>(true).Count(t => t.name == UnitWeapons.HeldName && t.GetComponentInChildren<Renderer>() is Renderer r && !r.forceRenderingOff);
        var figures = string.Join("/", token.GetComponentsInChildren<UnitFigureResidency>(true).Select(o => o.ModelId));
        CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"weapons-{what}-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
        Debug.Log($"SHOT weapons {what} {(after ? "after" : "before")} {(hebrew ? "he" : "en")}: token {subject.Token.Unit.Id} ({figures}) at {aim} from {distance:F0} m, shown weapons {held}{detail}");
    }

    /// <summary>The load at each zoom: on the opening map (15 May 1948, turn 0), every token's held weapons and the triangles they
    /// draw, from the unit zoom (40 km) and the closest zoom on each Israeli token in turn; logged as WEAPONS-LOAD lines.</summary>
    [UnityTest]
    public IEnumerator MeasureLoad()
    {
        UnitFigures.Source = TalModelsShots.Answer(Array.Empty<string>());
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        var map = Object.FindFirstObjectByType<MapScene>();
        var tokens = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens");
        (int instances, int shown, long triangles, long figureTriangles) Count()
        {
            int instances = 0, shown = 0; long triangles = 0, figures = 0;
            foreach (var t in tokens.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != UnitWeapons.HeldName) continue;
                instances++;
                var r = t.GetComponentInChildren<MeshRenderer>(true);
                if (r == null || r.forceRenderingOff || !r.gameObject.activeInHierarchy) continue;
                shown++; triangles += t.GetComponentInChildren<MeshFilter>(true).sharedMesh.triangles.Length / 3;
            }
            foreach (var skin in tokens.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (!skin.forceRenderingOff && skin.gameObject.activeInHierarchy) figures += skin.sharedMesh.triangles.Length / 3;
            return (instances, shown, triangles, figures);
        }
        var israeli = root.Units!.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel && t.Token.Figures > 0).ToList();
        var first = israeli[0];
        var at = new Vector3((float)first.Spot.X, 0f, (float)first.Spot.Z);
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        TalModelsShots.Look(at, 40000f);
        for (int i = 0; i < 3; i++) { Camera.main.Render(); yield return null; }
        var far = Count();
        Debug.Log($"WEAPONS-LOAD far (40 km): tokens {tokens.childCount}, Israeli army tokens {israeli.Count}, held weapons {far.instances}, drawn {far.shown}, weapon triangles drawn {far.triangles}, skinned figure triangles drawn {far.figureTriangles}");
        TalModelsShots.Look(at, CameraController.MinDistance);
        for (int i = 0; i < 3; i++) { Camera.main.Render(); yield return null; }
        var near = Count();
        Debug.Log($"WEAPONS-LOAD near ({CameraController.MinDistance:F0} m, every token's near models in): held weapons {near.instances}, drawn {near.shown}, weapon triangles drawn {near.triangles}, skinned figure triangles drawn {near.figureTriangles}");
        foreach (var t in israeli)
            Debug.Log($"WEAPONS-LOAD token {t.Token.Unit.Id}: figures {string.Join("/", map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/" + t.Token.Unit.Id).GetComponentsInChildren<UnitFigureWeapon>(true).Select(w => w.Weapon))}");
    }

    [UnityTest] public IEnumerator RifleBeforeEnglish() { yield return Shot(false, false, "rifle"); }
    [UnityTest] public IEnumerator RifleAfterEnglish() { yield return Shot(false, true, "rifle"); }
    [UnityTest] public IEnumerator RifleBeforeHebrew() { yield return Shot(true, false, "rifle"); }
    [UnityTest] public IEnumerator RifleAfterHebrew() { yield return Shot(true, true, "rifle"); }
    [UnityTest] public IEnumerator StenBeforeEnglish() { yield return Shot(false, false, "sten"); }
    [UnityTest] public IEnumerator StenAfterEnglish() { yield return Shot(false, true, "sten"); }
    [UnityTest] public IEnumerator StenBeforeHebrew() { yield return Shot(true, false, "sten"); }
    [UnityTest] public IEnumerator StenAfterHebrew() { yield return Shot(true, true, "sten"); }
    [UnityTest] public IEnumerator AttackBeforeEnglish() { yield return Shot(false, false, "attack"); }
    [UnityTest] public IEnumerator AttackAfterEnglish() { yield return Shot(false, true, "attack"); }
    [UnityTest] public IEnumerator AttackBeforeHebrew() { yield return Shot(true, false, "attack"); }
    [UnityTest] public IEnumerator AttackAfterHebrew() { yield return Shot(true, true, "attack"); }
}
