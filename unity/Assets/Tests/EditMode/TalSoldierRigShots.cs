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

/// <summary>Before/after stills of #2384 in the real game scene: the game booted as TalModelsShots boots it (15 May 1948,
/// turn 0), the unit figures and weapons answered with the editor's prefabs as the player's stream answers them, on Negev's
/// token at Nir Am (C07, C06 and the third figure; Yiftach stands only from 11 July 1948, so it is not on the opening map).
/// "Before" answers the third figure's request with C07 (what #2382 draws there); "after" with Tal Heuberger's soldier, rigged.
/// Each frame is close zoom (1.25 x the closest the player can reach) on the token's centre raised by half a figure's height,
/// yaw 0, as TalWeaponsShots frames its stills. The walk frame begins a move and advances it to its middle, the attack frame
/// plays the attack as the game does and advances it to half its length; an Animator does not run in edit mode, so every
/// figure's Animator is advanced by hand by the same time. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class TalSoldierRigShots
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedUnits;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-soldier-rig-shots-" + Guid.NewGuid().ToString("N"));
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
        UnitFigures.Source = (id, detail, done) => done(Prefab(!after && id == UnitFigures.Soldier1948 ? UnitFigures.VeteranFighter : id, detail));
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
            detail = $" attack played={played} at {half:F3} s, aim holding={(fire != null && fire.Holding)}";
            for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
        }
        if (what == "idle")
        {
            // Every model instance of the token's figures: what is drawn, where its hand is and where its weapon is.
            foreach (var figure in token.GetComponentsInChildren<UnitFigureResidency>(true))
            {
                var holder = figure.transform.Find(UnitFigures.ModelName);
                if (holder == null) { Debug.Log($"DIAG {figure.ModelId}: no holder"); continue; }
                for (int i = 0; i < holder.childCount; i++)
                {
                    var model = holder.GetChild(i);
                    var skin = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    var hand = UnitWeapons.FindDeep(model, UnitWeapons.HandBone);
                    var weapon = hand != null ? hand.Find(UnitWeapons.HeldName) : null;
                    var wr = weapon != null ? weapon.GetComponentInChildren<Renderer>(true) : null;
                    var animator = model.GetComponent<Animator>();
                    Debug.Log($"DIAG {figure.ModelId} child {i} '{model.name}' active={model.gameObject.activeInHierarchy} scale={model.lossyScale.y:F4} "
                        + $"skinOff={(skin != null ? skin.forceRenderingOff.ToString() : "none")} skinBounds={(skin != null ? skin.bounds.ToString() : "")} "
                        + $"animator={(animator != null ? animator.enabled + "/" + (animator.runtimeAnimatorController != null) : "none")} "
                        + $"hand={(hand != null ? hand.position.ToString("F3") : "none")} "
                        + $"weapon={(weapon != null ? weapon.position.ToString("F3") + " scale " + weapon.lossyScale.ToString("F3") : "none")} "
                        + $"weaponOff={(wr != null ? wr.forceRenderingOff.ToString() : "none")} weaponBounds={(wr != null ? wr.bounds.ToString() : "")} weaponActive={(wr != null ? wr.gameObject.activeInHierarchy.ToString() : "")}");
                }
            }
            var rig = Camera.main.GetComponent<CameraRig>();
            foreach (var yaw in new[] { 90f, 180f, 270f })
            {
                rig.Yaw = yaw; rig.Apply();
                for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
                CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"soldier-rig-idle-yaw{yaw:0}-{(after ? "after" : "before")}"));
            }
            rig.Yaw = 0f; rig.Apply();
            for (int i = 0; i < 2; i++) { Camera.main.Render(); yield return null; }
        }
        var held = token.GetComponentsInChildren<Transform>(true).Count(t => t.name == UnitWeapons.HeldName && t.GetComponentInChildren<Renderer>() is Renderer r && !r.forceRenderingOff);
        var figures = string.Join("/", token.GetComponentsInChildren<UnitFigureResidency>(true).Select(o => o.ModelId));
        CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"soldier-rig-{what}-{(after ? "after" : "before")}"));
        Debug.Log($"SHOT soldier-rig {what} {(after ? "after" : "before")}: token {subject.Token.Unit.Id} ({figures}) at {aim} from {distance:F0} m, shown weapons {held}{detail}");
    }

    [UnityTest] public IEnumerator IdleBefore() { yield return Shot(false, "idle"); }
    [UnityTest] public IEnumerator IdleAfter() { yield return Shot(true, "idle"); }
    [UnityTest] public IEnumerator WalkBefore() { yield return Shot(false, "walk"); }
    [UnityTest] public IEnumerator WalkAfter() { yield return Shot(true, "walk"); }
    [UnityTest] public IEnumerator AttackBefore() { yield return Shot(false, "attack"); }
    [UnityTest] public IEnumerator AttackAfter() { yield return Shot(true, "attack"); }
}
