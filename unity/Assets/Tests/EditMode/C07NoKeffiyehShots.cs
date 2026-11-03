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

/// <summary>Before/after stills of C07's keffiyeh-free copy (owner, 3 November 2026: "no israeli soldir should wear a khafia on
/// their neck") in the real game scene: the game booted as TalModelsShots boots it (15 May 1948, turn 0), the unit figures and
/// weapons answered with the editor's prefabs as the player's stream answers them, on Negev's token at Nir Am, whose first
/// figure wears the knitted stocking cap (C07, C06 and the third figure). "Before" answers the veteran fighter's request with
/// Tal Heuberger's delivered C07, scarf and all (what main draws there); "after" with the copy. Each frame is close zoom
/// (1.25 x the closest the player can reach) on the token's centre raised by half a figure's height, from the front and from
/// yaw 45, 90, 180 and 270 degrees, as TalSoldierRigShots frames its stills; the walk frame begins a move and advances it to
/// its middle, the attack frame plays the attack and advances it to half its length; an Animator does not run in edit mode, so
/// every figure's Animator is advanced by hand by the same time. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class C07NoKeffiyehShots
{
    const string Delivered = "figure-c07-veteran-fighter-v2";
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedUnits;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-c07-nokeffiyeh-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TalModelsShots.ShotDir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedUnits = UnitFigures.Source;
    }

    [TearDown]
    public void DiscardScene()
    {
        UnitFigures.Source = savedUnits; UnitWeapons.Source = null; UnitFigurePointFire.Forget();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameObject? Prefab(string id, CollectionDetail detail) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));

    /// <summary>How far into the walk take the moving frame is taken: a mid-stride of C07's 57-frame (2.33 s) walk.</summary>
    const float WalkAt = 0.6f;

    IEnumerator Shot(bool after, string what)
    {
        UnitFigures.Source = (id, detail, done) => done(Prefab(!after && id == UnitFigures.VeteranFighter ? Delivered : id, detail));
        UnitWeapons.Source = (id, done) => done(Prefab(id, CollectionDetail.Far));
        yield return CivMovementShots.Boot(false, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
        Camera.main.aspect = (float)CivMovementShots.Width / CivMovementShots.Height;
        var map = Object.FindFirstObjectByType<MapScene>();
        var subject = root.Units!.Tokens.First(t => t.Token.Unit.Id == "negev");
        var token = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/" + subject.Token.Unit.Id);
        Assert.IsNotNull(token, subject.Token.Unit.Id);
        var at = new Vector3((float)subject.Spot.X, 0f, (float)subject.Spot.Z);
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        float distance = CameraController.MinDistance * 1.25f;
        var aim = at + Vector3.up * (UnitBadge.FigureHeightM * UnitShowcase.FigureScale * 0.5f);
        TalModelsShots.Look(aim, distance);
        for (int i = 0; i < 5; i++) { Camera.main.Render(); yield return null; }   // the near figures come in at close zoom
        string detail = "";
        var motion = token.GetComponent<UnitTokenMotion>() ?? token.gameObject.AddComponent<UnitTokenMotion>();
        if (what == "walk")
        {
            motion.Begin(new Vector3(UnitTokenMotion.MinMoveM * 4f, 0f, 0f));
            motion.Tick(UnitTokenMotion.Duration / 2f);
            foreach (var a in token.GetComponentsInChildren<Animator>()) if (a.isActiveAndEnabled) a.Update(WalkAt);
            detail = $" walk at {WalkAt:F2} s, moving={motion.Moving}";
            for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
        }
        else if (what == "attack")
        {
            bool played = motion.PlayAttack();
            var fire = token.GetComponentInChildren<UnitFigurePointFire>();
            float half = 0f;
            foreach (var a in token.GetComponentsInChildren<Animator>())
                if (UnitFigureMotion.CanPlay(a, UnitFigureMotion.Attack)) half = Mathf.Max(half, UnitTokenMotion.FireLength(a) / 2f);
            if (fire != null && fire.Ready) half = Mathf.Max(half, fire.FireLength / 2f);
            motion.Tick(half);
            foreach (var a in token.GetComponentsInChildren<Animator>()) if (a.isActiveAndEnabled) a.Update(half);
            detail = $" attack played={played} at {half:F3} s";
            for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
        }
        var rig = Camera.main.GetComponent<CameraRig>();
        foreach (var yaw in new[] { 0f, 45f, 90f, 180f, 270f })
        {
            rig.Yaw = yaw; rig.Apply();
            for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
            CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"c07-nokeffiyeh-{what}-yaw{yaw:0}-{(after ? "after" : "before")}"));
        }
        var figures = string.Join("/", token.GetComponentsInChildren<UnitFigureResidency>(true).Select(o => o.ModelId));
        var drawn = string.Join("/", token.GetComponentsInChildren<Transform>(true).Where(t => t.parent != null && t.parent.name == UnitFigures.ModelName).Select(t => t.name));
        Debug.Log($"SHOT c07-nokeffiyeh {what} {(after ? "after" : "before")}: token {subject.Token.Unit.Id} ({figures}; drawn {drawn}) at {aim} from {distance:F0} m{detail}");
    }

    [UnityTest] public IEnumerator IdleBefore() { yield return Shot(false, "idle"); }
    [UnityTest] public IEnumerator IdleAfter() { yield return Shot(true, "idle"); }
    [UnityTest] public IEnumerator WalkBefore() { yield return Shot(false, "walk"); }
    [UnityTest] public IEnumerator WalkAfter() { yield return Shot(true, "walk"); }
    [UnityTest] public IEnumerator AttackBefore() { yield return Shot(false, "attack"); }
    [UnityTest] public IEnumerator AttackAfter() { yield return Shot(true, "attack"); }
}
