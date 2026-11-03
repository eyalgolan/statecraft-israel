#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>The tool runs behind #2375 and #2376, not checks. BakeThenPrepareControllersThenShoot bakes the borrowed fire
/// takes (FireTakeRetarget.Bake), gives the controllers their attack states (CollectionImport.PrepareTokenFigureControllers)
/// and writes one contact sheet per figure for review. TokenCaptures renders the changed tokens in the real map with the
/// dated legacy roster (20 June 1948, when every changed army stands on the map), close up as the player sees them,
/// standing, mid-walk and at three points of the fire take (owner, 3 November 2026: "Yes, send me captures"). Both write
/// into $STATECRAFT_FIRE_STILLS or unity/../captures/fire-takes. Explicit.</summary>
[Explicit("bakes assets and writes stills for review, not a check")]
public class FireTakeRetargetShots
{
    static Type Retarget => Type.GetType("Statecraft.Game.EditorTools.FireTakeRetarget, Assembly-CSharp-Editor")!;
    static Type Import => Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor")!;
    static Type Generated => Type.GetType("Statecraft.Game.EditorTools.Generated1948Characters, Assembly-CSharp-Editor")!;
    const BindingFlags Any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static string Folder => Environment.GetEnvironmentVariable("STATECRAFT_FIRE_STILLS") is string set && set.Length > 0
        ? set : Path.GetFullPath("../captures/fire-takes");

    [Test]
    public void BakeThenPrepareControllersThenShoot()
    {
        Retarget.GetMethod("Bake", Type.EmptyTypes)!.Invoke(null, null);
        Import.GetMethod("PrepareTokenFigureControllers")!.Invoke(null, null);
        Retarget.GetMethod("Stills")!.Invoke(null, new object[] { Folder });
    }

    [Test]
    public void ShootOnly()
    {
        Retarget.GetMethod("Stills")!.Invoke(null, new object[] { Folder });
    }

    /// <summary>The changed tokens: an Israeli token with the slouch hat (C04) and each Arab army's token (Syria, Iraq, Egypt,
    /// Lebanon and the ALA, whose third figure is the ALA infantry beside two C09), each on a date its token stands on the map
    /// (the ALA's window is 15 to 29 May 1948; the Syrian, Iraqi and Lebanese tokens with figures stand on 20 June).</summary>
    static readonly (DateTime date, string label, Func<PlacedToken, bool> pick)[] Subjects =
    {
        (new DateTime(1948, 5, 20), "israel-c04", t => t.Token.Unit.Side == UnitData.Israel && t.Token.Figures > 0 && t.Token.Headgear.Contains(UnitData.SlouchHat)),
        (new DateTime(1948, 6, 20), "syria", t => t.Token.Unit.Side == "syria" && t.Token.Figures > 0),
        (new DateTime(1948, 6, 20), "iraq", t => t.Token.Unit.Side == "iraq" && t.Token.Figures > 0),
        (new DateTime(1948, 5, 20), "egypt", t => t.Token.Unit.Side == "egypt" && t.Token.Figures > 0),
        (new DateTime(1948, 5, 20), "ala", t => t.Token.Unit.Side == "ala" && t.Token.Figures > 0),
        (new DateTime(1948, 6, 20), "lebanon", t => t.Token.Unit.Side == "lebanon" && t.Token.Figures > 0),
    };

    [UnityTest]
    public IEnumerator TokenCaptures()
    {
        string folder = Path.Combine(Folder, "tokens");
        Directory.CreateDirectory(folder);
        var previous = EditorSceneManager.GetSceneManagerSetup();
        var pending = new List<(string id, Action<GameObject?> done)>();
        UnitFigures.Forget();
        UnitFigures.Source = (id, detail, done) => pending.Add((id, done));
        // The weapons, so C04 holds its K98k as in the game; not C06's point-fire pose source, which needs the game's frames.
        UnitWeapons.Source = (id, done) => done(id == CollectionCatalog.C06PointFire ? null : AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far)));
        void Answer()
        {
            for (int round = 0; round < 4 && pending.Count > 0; round++)
            {
                var asked = pending.ToArray(); pending.Clear();
                foreach (var r in asked) r.done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(r.id, CollectionDetail.Far)));
            }
        }
        var records = new List<string>
        {
            "Unity editor renders of the real map and the dated legacy roster (rung 5, mobilization 0.8), the game's camera at close zoom (1.25 x the closest the player reaches, yaw 0).",
            "1-standing: the figures as an attack shows them before #2375/#2376 (no fire take, so they stand). 2-moving: mid-walk, a figure's height into a slide (unchanged by this change). 3-firing-NN: the fire take at NN % of its length, as UnitTokenMotion plays it.",
            "Not a player run; no simulated state or placement modified.",
        };
        var render = Generated.GetMethod("RenderMap", Any)!;
        var missing = new List<string>();
        try
        {
            foreach (var group in Subjects.GroupBy(s => s.date))
            {
                var stage = ((MapScene map, UnitLayer layer, GameState state))Generated.GetMethod("CampaignStage", Any)!.Invoke(null, new object[] { group.Key });
                var (map, layer, state) = stage;
                Answer();
                var camera = Object.FindFirstObjectByType<CameraRig>().GetComponent<Camera>();
                var rig = camera.GetComponent<CameraRig>();
                camera.aspect = 16f / 9f;
                // Every requested model answered and the layer settled on them over editor frames: a model's arrival makes
                // FaceCamera draw the tokens again (UnitFigures.AvailabilityVersion), which would replace a token held mid-shot.
                for (int i = 0; i < 6; i++) { camera.Render(); Answer(); layer.FaceCamera(camera); Answer(); yield return null; }
                foreach (var (date, label, pick) in group)
                {
                    var found = layer.Tokens.Where(pick).OrderBy(t => t.Token.Unit.Id, StringComparer.Ordinal).ToArray();
                    if (found.Length == 0) { missing.Add(label); records.Add($"{label}: no token with figures on {date:yyyy-MM-dd}"); continue; }
                    var placed = found[0];
                    var at = new Vector3((float)placed.Spot.X, 0f, (float)placed.Spot.Z);
                    at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
                    // Close zoom as TalWeaponsShots frames it: on the figures' chests, a quarter farther than the closest zoom, yaw 0.
                    var aim = at + Vector3.up * (UnitBadge.FigureHeightM * UnitShowcase.FigureScale * 0.5f);
                    rig.LookAt = aim; rig.Distance = CameraController.MinDistance * 1.25f; rig.Yaw = 0f; rig.TopInset = 0; rig.Apply();
                    // The near figures come in as the camera renders close (UnitFigureDetail.Choose), over frames as in the game.
                    for (int i = 0; i < 6; i++) { camera.Render(); Answer(); layer.FaceCamera(camera); Answer(); yield return null; }
                    var token = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/" + placed.Token.Unit.Id);
                    Assert.IsNotNull(token, label + ": " + placed.Token.Unit.Id);
                    var animators = token.GetComponentsInChildren<Animator>(true);
                    foreach (var a in animators) { a.cullingMode = AnimatorCullingMode.AlwaysAnimate; if (a.isActiveAndEnabled) a.Update(0f); }
                    var live = animators.Where(a => a.isActiveAndEnabled).ToArray();
                    var motion = token.GetComponent<UnitTokenMotion>() ?? token.gameObject.AddComponent<UnitTokenMotion>();
                    motion.enabled = false;
                    var hands = token.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Right_Hand" || t.name == "mixamorig:RightHand").ToArray();
                    var standing = hands.Select(h => h.position).ToArray();
                    string figures = string.Join("/", token.GetComponentsInChildren<UnitFigureResidency>(true).Select(o => o.ModelId));
                    string State(Animator a) => a.GetCurrentAnimatorStateInfo(0).fullPathHash == UnitFigureMotion.Attack ? "attack"
                        : a.GetCurrentAnimatorStateInfo(0).fullPathHash == UnitFigureMotion.Walk ? "walk" : a.GetCurrentAnimatorStateInfo(0).fullPathHash == UnitFigureMotion.Idle ? "idle" : "other";
                    // A skinned mesh already drawn is skinned again only by an editor frame, so each shot waits two frames
                    // after its pose (as TalWeaponsShots does), then renders.
                    IEnumerator Shot(string name)
                    {
                        int version = UnitFigures.AvailabilityVersion;
                        layer.FaceCamera(camera);
                        UnitFlagFacing.FaceAll(camera.transform.position);
                        for (int i = 0; i < 2; i++) { camera.Render(); yield return null; }
                        if (version != UnitFigures.AvailabilityVersion || token == null) throw new InvalidOperationException(label + ": the layer drew its tokens again during " + name);
                        render.Invoke(null, new object[] { camera, Path.Combine(folder, name + ".jpg") });
                        float moved = hands.Length > 0 ? hands.Select((h, i) => (h.position - standing[i]).magnitude).Max() : 0f;
                        records.Add($"{label} {name}: Animators {string.Join("/", live.Select(State))}; a right hand moved {moved:F0} m from standing");
                    }
                    records.Add($"{label}: token {placed.Token.Unit.Id} ({figures}) on {date:yyyy-MM-dd}; live Animators {live.Length} of {animators.Length}; aim {aim}; distance {rig.Distance:F0} m");
                    // 1. Standing: what the player sees during an attack before this change (no fire take, so the figures stand).
                    yield return Shot(label + "-1-standing");
                    // 2. Mid-walk: a figure's height into a slide (unchanged by this change; the walks are #2176's).
                    motion.Begin(Vector3.right * (UnitBadge.FigureHeightM * UnitShowcase.FigureScale));
                    motion.Tick(0.3f);
                    foreach (var a in live) a.Update(0.3f);
                    yield return Shot(label + "-2-moving");
                    motion.Tick(1f);
                    foreach (var a in live) a.Update(1f);
                    // 3. Firing: the attack as UnitTokenMotion plays it, at a quarter, half and three quarters of the take.
                    bool played = motion.PlayAttack();
                    float length = 0f;
                    foreach (var a in live) if (UnitFigureMotion.CanPlay(a, UnitFigureMotion.Attack)) length = Mathf.Max(length, UnitTokenMotion.FireLength(a));
                    records.Add($"{label}: attack played={played}, take {length:F3} s");
                    float step = length / 4f;
                    foreach (var quarter in new[] { "25", "50", "75" })
                    {
                        motion.Tick(step);
                        foreach (var a in live) a.Update(step);
                        yield return Shot(label + "-3-firing-" + quarter);
                    }
                    motion.Tick(length);
                    File.WriteAllLines(Path.Combine(folder, "README.txt"), records);
                }
            }
            File.WriteAllLines(Path.Combine(folder, "README.txt"), records);
            Debug.Log("FireTakeRetarget token captures -> " + folder + "\n" + string.Join("\n", records));
            CollectionAssert.IsEmpty(missing, "every changed army's token was found");
        }
        finally
        {
            UnitFigures.Source = null; UnitWeapons.Source = null;
            UnitFigures.Forget();
            if (previous.Any(s => s.isLoaded) && previous.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
