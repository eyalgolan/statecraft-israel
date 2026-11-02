using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

/// <summary>#2083 and #2084 (owner, 2 November 2026): Tal Heuberger's weapons in the token soldier figures' hands, and C06's
/// point-fire aim while its token attacks, on the real imported models.</summary>
public class UnitWeaponsTests
{
    readonly List<Object> made = new List<Object>();

    [SetUp]
    public void Fresh()
    {
        UnitFigures.Source = null; UnitFigures.Forget(); UnitFigurePointFire.Forget();
        // The editor's prefabs answer at once, as the captures' sources do.
        UnitFigures.Source = (id, detail, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail)));
        UnitWeapons.Source = (id, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far)));
    }

    [TearDown]
    public void Restore()
    {
        foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
        made.Clear();
        UnitFigures.Source = null; UnitFigures.Forget(); UnitWeapons.Source = null; UnitFigurePointFire.Forget();
    }

    static GameObject Prefab(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far))
        ?? throw new AssertionException(id + ": no prefab");

    /// <summary>A token with one figure dressed as <paramref name="figure"/> (its far model, as the map draws it far off),
    /// holding its weapon and, for C06, its point-fire aim.</summary>
    (UnitTokenMotion motion, Transform piece) Token(string figure, string side = UnitData.Israel)
    {
        var token = new GameObject("token"); made.Add(token);
        var piece = new GameObject("Rifleman0").transform; piece.SetParent(token.transform, false);
        UnitFigures.Swap(piece, figure, Prefab(figure));
        UnitFigureWeapon.Equip(piece, side, figure);
        UnitFigurePointFire.Equip(piece, figure);
        var motion = token.AddComponent<UnitTokenMotion>(); motion.enabled = false;
        return (motion, piece);
    }

    static Transform Model(Transform piece) => piece.Find(UnitFigures.ModelName).GetChild(0);

    // ---- data: sourced rows, per army ----

    [Test]
    public void EveryWeaponRowIsSourcedAgentReadAndDatedBeforeEveryToken()
    {
        var units = UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
        Assert.IsNotEmpty(UnitWeapons.Rows);
        foreach (var row in UnitWeapons.Rows)
        {
            Assert.IsNotEmpty(row.Sources, row.Weapon);
            foreach (var s in row.Sources) StringAssert.StartsWith("https://", s, row.Weapon);
            Assert.AreEqual("agent-read", row.Review, row.Weapon + ": a person's check is pending (CONTENT_POLICY rule 6)");
            Assert.IsTrue(CollectionCatalog.IsGeneratedWeapon(row.Weapon), row.Weapon);
            var from = DateTime.ParseExact(row.From, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            foreach (var u in units.Where(u => u.Side == row.Side))
                foreach (var a in u.Anchors)
                    Assert.LessOrEqual(from, DateTime.ParseExact(a.From, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                        $"{u.Id} stands from {a.From}, before {row.Weapon}'s sourced {row.From}");
        }
    }

    [Test]
    public void OnlyIsraeliFiguresWithTalsRigHoldAWeapon()
    {
        Assert.AreEqual(CollectionCatalog.K98k, UnitWeapons.WeaponFor(UnitData.Israel, UnitFigures.YoungRecruit));
        Assert.AreEqual(CollectionCatalog.K98k, UnitWeapons.WeaponFor(UnitData.Israel, UnitFigures.SabraPioneer));
        Assert.AreEqual(CollectionCatalog.K98k, UnitWeapons.WeaponFor(UnitData.Israel, UnitFigures.VeteranFighter));
        Assert.AreEqual(CollectionCatalog.K98k, UnitWeapons.WeaponFor(UnitData.Israel, UnitFigures.PioneerVeteran));
        Assert.AreEqual(CollectionCatalog.Sten, UnitWeapons.WeaponFor(UnitData.Israel, UnitFigures.PalmachScout), "the Palmach's personal weapon");
        Assert.IsNull(UnitWeapons.WeaponFor(UnitData.Israel, UnitFigures.Soldier1948), "#336's soldier has no rig: [GAP]");
        foreach (var kv in UnitFigures.BySide)
        {
            Assert.IsNull(UnitWeapons.WeaponFor(kv.Key, kv.Value), kv.Key + ": no source gives this army Tal's weapons");
            Assert.IsNull(UnitWeapons.WeaponFor(kv.Key, UnitFigures.YoungRecruit), kv.Key);
        }
        foreach (var figure in UnitWeapons.ByFigure.Keys)
            Assert.IsNotNull(UnitWeapons.FindDeep(Prefab(figure).transform, UnitWeapons.HandBone), figure + " has the hand bone");
    }

    // ---- the weapon in the hand ----

    [Test]
    public void ArmedFiguresHoldTheirWeaponInTheRightHandAtTheirOwnHeight()
    {
        foreach (var figure in UnitWeapons.ByFigure.Keys)
        {
            var bare = new GameObject("bare").transform; made.Add(bare.gameObject);
            UnitFigures.Swap(bare, figure, Prefab(figure));
            var (_, piece) = Token(figure);
            var hand = UnitWeapons.FindDeep(Model(piece), UnitWeapons.HandBone);
            var held = hand.Find(UnitWeapons.HeldName);
            Assert.IsNotNull(held, figure + " holds its weapon");
            Assert.AreSame(Prefab(UnitWeapons.ByFigure[figure]).GetComponentInChildren<MeshFilter>().sharedMesh,
                held.GetComponentInChildren<MeshFilter>().sharedMesh, figure + " holds " + UnitWeapons.ByFigure[figure]);
            Assert.AreEqual(1, Model(piece).GetComponentsInChildren<Transform>(true).Count(t => t.name == UnitWeapons.HeldName), "once");
            var with = UnitFigures.BoxIn(piece.Find(UnitFigures.ModelName), Model(piece)).Value;
            var without = UnitFigures.BoxIn(bare.Find(UnitFigures.ModelName), Model(bare)).Value;
            Assert.That(Vector3.Distance(with.min, without.min) + Vector3.Distance(with.max, without.max), Is.LessThan(1e-5f),
                figure + ": the weapon never changes the figure's box (height, feet)");
            Assert.AreEqual(without.size.y * piece.Find(UnitFigures.ModelName).localScale.y,
                with.size.y * piece.Find(UnitFigures.ModelName).localScale.y, 1e-5f);
        }
    }

    [Test]
    public void AnArabFigureAndTheUnriggedSoldierStayAsTheyWere()
    {
        foreach (var figure in new[] { UnitFigures.SyrianInfantry, UnitFigures.ArabLegion, UnitFigures.Irregular, UnitFigures.Soldier1948 })
        {
            var (_, piece) = Token(figure, figure == UnitFigures.Soldier1948 ? UnitData.Israel : "syria");
            Assert.IsFalse(Model(piece).GetComponentsInChildren<Transform>(true).Any(t => t.name == UnitWeapons.HeldName), figure);
            Assert.IsNull(piece.GetComponent<UnitFigureWeapon>(), figure);
        }
    }

    /// <summary>The socket is Tal's own fixed pair: in his point-fire export the K98k does not move against C06's right hand
    /// over the 97 frames, and that relation, turned into the C06 rig's frame, is the socket. Then, held by C06 in the aim,
    /// our K98k lies on Tal's rifle and our C06 on his C06, vertex for vertex.</summary>
    [Test]
    public void TheK98kSocketIsTalsFixedPairAndTheAimMatchesHisExport()
    {
        string path = CollectionCatalog.SourceModelPath(CollectionCatalog.C06PointFire);
        var export = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path)); made.Add(export);
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
        var source = Prefab(CollectionCatalog.C06PointFire).GetComponent<UnitFigurePoseSource>();
        Assert.AreSame(clip, source.Clip);
        Assert.AreEqual(36f / 24f, source.AimTime, 1e-6f, "frame 37 of 97 at 24 fps");
        var rifle = UnitWeapons.FindDeep(export.transform, "tripo_node_0fa6038f-9e8d-4708-a9e8-755be6fdcc67");
        var eHand = UnitWeapons.FindDeep(export.transform, UnitWeapons.HandBone);
        Matrix4x4? pair = null;
        for (int frame = 1; frame <= 97; frame += 4)
        {
            clip.SampleAnimation(export, (frame - 1) / 24f);
            var s = eHand.worldToLocalMatrix * rifle.localToWorldMatrix;
            if (pair is Matrix4x4 p) for (int k = 0; k < 16; k++) Assert.AreEqual(p[k], s[k], 1e-3f * Mathf.Max(1f, Mathf.Abs(p[k])), $"frame {frame}: the rifle moved in the hand");
            else pair = s;
        }
        // Expected socket: correction(hand)^-1 x Tal's pair x (our K98k's mesh frame into his).
        int hand = Array.IndexOf(source.Bones, UnitWeapons.HandBone);
        var talMesh = rifle.GetComponent<MeshFilter>().sharedMesh;
        var ours = Prefab(CollectionCatalog.K98k);
        var ourMesh = ours.GetComponentInChildren<MeshFilter>().sharedMesh;
        // His export and ours split the same K98k's vertices differently (6,840 and 6,889), so each of ours is matched to his nearest.
        var mine = ourMesh.vertices; var his = talMesh.vertices;
        float k98 = talMesh.bounds.size.x / ourMesh.bounds.size.x;
        Assert.That(Nearest(mine.Where((_, i) => i % 13 == 0).Select(v => v * k98), his), Is.LessThan(1e-4f * k98),
            "the same K98k, same axes, scale " + k98);
        var socket = source.Correction[hand].inverse * pair.Value * Matrix4x4.Scale(Vector3.one * k98);
        string want = FormattableString.Invariant($"pos=({socket.m03:R}f, {socket.m13:R}f, {socket.m23:R}f) rot=({socket.rotation.x:R}f, {socket.rotation.y:R}f, {socket.rotation.z:R}f, {socket.rotation.w:R}f) scale={socket.lossyScale.x:R}f");
        Assert.That(Vector3.Distance(UnitWeapons.SocketPosition, socket.GetColumn(3)), Is.LessThan(1e-5f), want);
        Assert.That(Quaternion.Angle(UnitWeapons.SocketRotation, socket.rotation), Is.LessThan(0.01f), want);
        Assert.AreEqual(socket.lossyScale.x, UnitWeapons.K98kSocketScale, 1e-4f * socket.lossyScale.x, want);

        // The real path: C06 on a token, armed, attacking (held in the aim) against Tal's export at the aim frame.
        var (motion, piece) = Token(UnitFigures.YoungRecruit);
        Assert.IsTrue(motion.PlayAttack());
        clip.SampleAnimation(export, source.AimTime);
        var model = Model(piece);
        var ourRifle = UnitWeapons.FindDeep(model, UnitWeapons.HeldName).GetComponentInChildren<MeshFilter>();
        Matrix4x4 InModel(Transform t) => model.worldToLocalMatrix * t.localToWorldMatrix;
        Matrix4x4 InExport(Transform t) => export.transform.worldToLocalMatrix * t.localToWorldMatrix;
        var hisInExport = his.Select(v => InExport(rifle).MultiplyPoint3x4(v)).ToArray();
        float worst = Nearest(mine.Where((_, i) => i % 7 == 0).Select(v => InModel(ourRifle.transform).MultiplyPoint3x4(v)), hisInExport);
        Assert.That(worst, Is.LessThan(1e-4f), "our K98k lies on Tal's (model units: the figure is 1 tall)");
        var skin = model.GetComponentInChildren<SkinnedMeshRenderer>(); var hisSkin = export.GetComponentInChildren<SkinnedMeshRenderer>();
        var a = new Mesh(); var b = new Mesh();
        try
        {
            skin.BakeMesh(a, true); hisSkin.BakeMesh(b, true);
            var av = a.vertices; var bv = b.vertices;
            var aMax = av.Select(v => InModel(skin.transform).MultiplyPoint3x4(v)).ToArray();
            var bMax = bv.Select(v => InExport(hisSkin.transform).MultiplyPoint3x4(v)).ToArray();
            Bounds Box(Vector3[] vs) { var bx = new Bounds(vs[0], Vector3.zero); foreach (var v in vs) bx.Encapsulate(v); return bx; }
            var ab = Box(aMax); var bb = Box(bMax);
            Assert.That(Vector3.Distance(ab.min, bb.min) + Vector3.Distance(ab.max, bb.max), Is.LessThan(2e-3f), $"C06 in the aim {ab} against Tal's {bb}");
            var right = UnitWeapons.FindDeep(model, UnitWeapons.HandBone).position; var hisRight = eHand.position;
            Assert.That(Vector3.Distance(model.InverseTransformPoint(right), export.transform.InverseTransformPoint(hisRight)), Is.LessThan(1e-4f), "the right hand");
        }
        finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
    }

    /// <summary>The largest distance from a point of <paramref name="points"/> to its nearest point of <paramref name="cloud"/>.</summary>
    static float Nearest(IEnumerable<Vector3> points, Vector3[] cloud)
    {
        float worst = 0f;
        foreach (var p in points)
        {
            float best = float.MaxValue;
            foreach (var q in cloud) best = Mathf.Min(best, (p - q).sqrMagnitude);
            worst = Mathf.Max(worst, Mathf.Sqrt(best));
        }
        return worst;
    }

    // ---- the aim while attacking ----

    static Matrix4x4[] Bones(Transform model) =>
        UnitWeapons.FindDeep(model, "Armature").GetComponentsInChildren<Transform>(true)
            .Where(t => !UnitWeapons.IsHeld(t, model)).Select(t => Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale)).ToArray();

    /// <summary>Every node's local transform within 1e-6 (a quaternion read back after it is set can differ in the last bit).</summary>
    static void Same(Matrix4x4[] want, Matrix4x4[] have, string message)
    {
        Assert.AreEqual(want.Length, have.Length, message);
        for (int i = 0; i < want.Length; i++)
            for (int k = 0; k < 16; k++) Assert.AreEqual(want[i][k], have[i][k], 1e-6f * Mathf.Max(1f, Mathf.Abs(want[i][k])), $"{message} (node {i})");
    }

    [Test]
    public void AnAttackHoldsC06sAimForItsFireTakeThenPutsEveryBoneBack()
    {
        var (motion, piece) = Token(UnitFigures.YoungRecruit);
        var model = Model(piece);
        var before = Bones(model);
        var hand = UnitWeapons.FindDeep(model, UnitWeapons.HandBone);
        var handBefore = model.InverseTransformPoint(hand.position);
        Assert.IsTrue(motion.PlayAttack(), "a far C06 takes the aim: the attack plays");
        var aim = piece.GetComponent<UnitFigurePointFire>();
        Assert.IsTrue(aim.Holding);
        Assert.That(Vector3.Distance(handBefore, model.InverseTransformPoint(hand.position)), Is.GreaterThan(0.05f), "the right hand rose to the aim");
        var held = Bones(model);
        float length = aim.FireLength;
        Assert.AreEqual(37f / 24f, length, 0.05f, "C06's own fire take: 37 frames");
        motion.Tick(length * 0.5f);
        Same(held, Bones(model), "held still");
        motion.Tick(length);
        Assert.IsFalse(motion.Attacking);
        Assert.IsFalse(aim.Holding);
        Same(before, Bones(model), "every bone back as it was");
        foreach (var a in model.GetComponentsInChildren<Animator>(true)) Assert.IsFalse(a.enabled, "a far model's Animator stays off");
    }

    [Test]
    public void TheAimIsTheSameFrameOnEveryToken()
    {
        var (m1, p1) = Token(UnitFigures.YoungRecruit);
        m1.PlayAttack();
        var first = Bones(Model(p1));
        UnitFigurePointFire.Forget();   // computed afresh
        var (m2, p2) = Token(UnitFigures.YoungRecruit);
        m2.PlayAttack();
        Same(first, Bones(Model(p2)), "the same aim");
    }

    [Test]
    public void HoldingAndReleasingTheAimAllocatesNothing()
    {
        var (motion, piece) = Token(UnitFigures.YoungRecruit);
        motion.PlayAttack();   // the first hold computes the pose once
        var aim = piece.GetComponent<UnitFigurePointFire>();
        aim.Release();
        Assert.That(() => aim.Hold(), Is.Not.AllocatingGCMemory());
        Assert.That(() => aim.Release(), Is.Not.AllocatingGCMemory());
    }

    [Test]
    public void OtherFiguresKeepTheirOwnFireTake()
    {
        foreach (var figure in new[] { UnitFigures.VeteranFighter, UnitFigures.SabraPioneer, UnitFigures.PalmachScout })
        {
            var (motion, piece) = Token(figure);
            Assert.IsNull(piece.GetComponent<UnitFigurePointFire>(), figure);
            var before = Bones(Model(piece));
            motion.PlayAttack();
            Same(before, Bones(Model(piece)), figure + ": a far figure without the aim stands as it was");
        }
    }

    [Test]
    public void WithoutItsPoseSourceC06FailsOpen()
    {
        UnitWeapons.Source = (id, done) => done(id == CollectionCatalog.C06PointFire ? null : AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far)));
        var (motion, piece) = Token(UnitFigures.YoungRecruit);
        var before = Bones(Model(piece));
        Assert.IsFalse(motion.PlayAttack(), "far, with no aim and no near fire take: nothing plays, as before");
        Same(before, Bones(Model(piece)), "unchanged");
    }

    [Test]
    public void TheWeaponsAndThePoseStreamInTheirOwnBundlesAfterThePeoples()
    {
        foreach (var id in new[] { CollectionCatalog.K98k, CollectionCatalog.Sten, CollectionCatalog.C06PointFire })
        {
            Assert.AreEqual("collection-" + id, CollectionCatalog.BundleOf(id));
            Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.After["collection-" + id]);
        }
        var import = Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor");
        var pose = ((AssetBundleBuild[])import!.GetMethod("Builds")!.Invoke(null, null)!).Single(b => b.assetBundleName == "collection-" + CollectionCatalog.C06PointFire);
        CollectionAssert.AreEqual(new[] { CollectionCatalog.PrefabPath(CollectionCatalog.C06PointFire, CollectionDetail.Far) }, pose.assetNames);
        var deps = AssetDatabase.GetDependencies(pose.assetNames[0], true);
        Assert.IsFalse(deps.Any(d => d.EndsWith(".jpg") || d.EndsWith(".png")), "no texture: " + string.Join(", ", deps));
        Assert.IsNull(Prefab(CollectionCatalog.C06PointFire).GetComponentInChildren<Renderer>(true), "no mesh in the pose source");
    }

    // ---- review of #2277: hiding mid-attack, the near model, the socket on the other figures ----

    /// <summary>Brings the token's near (close-zoom) models in, as a camera below the switch does (UnitFigureDetail).</summary>
    Transform Near(Transform piece)
    {
        var camera = new GameObject("Camera").AddComponent<Camera>(); made.Add(camera.gameObject);
        camera.transform.SetPositionAndRotation(new Vector3(0f, Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM - 1f, 0f), Quaternion.Euler(90f, 0f, 0f));
        piece.root.GetComponent<UnitFigureDetail>().Choose(default, camera);
        var near = piece.Find(UnitFigures.ModelName).Find(UnitFigureDetail.NearName);
        Assert.IsNotNull(near, "the near model came in");
        return near;
    }

    /// <summary>The units' visual toggle hides the layer's root (UnitLayer) mid-attack. UnitTokenMotion drops the attack then
    /// (in a player; it does not run in edit mode), but its Pose finds no figure under an inactive root, so the aim lets go
    /// by itself: the token shown again stands as it did, its far and near bones back and the near model's Animator running.</summary>
    [Test]
    public void AHiddenTokenMidAttackLetsGoOfTheAim()
    {
        var (motion, piece) = Token(UnitFigures.YoungRecruit);
        var near = Near(piece);
        var far = Model(piece);
        var farBefore = Bones(far); var nearBefore = Bones(near);
        var nearAnimator = near.GetComponentInChildren<Animator>(true);
        Assert.IsTrue(nearAnimator.enabled, "close: the near model's Animator runs");
        Assert.IsTrue(motion.PlayAttack());
        var aim = piece.GetComponent<UnitFigurePointFire>();
        Assert.IsTrue(aim.Holding);
        Assert.IsFalse(nearAnimator.enabled, "held: paused");
        motion.gameObject.SetActive(false);   // the units toggled off mid-attack
        motion.gameObject.SetActive(true);
        Assert.IsFalse(aim.Holding, "and its aim let go");
        Same(farBefore, Bones(far), "the far model's bones back");
        Same(nearBefore, Bones(near), "the near model's bones back");
        Assert.IsTrue(nearAnimator.enabled, "the near model's Animator runs again");
    }

    /// <summary>The near model a player sees close up is the same streamed prefab as the far one (CollectionCatalog.PrefabPath
    /// gives one prefab for both details), so it takes the same aim: held on it, it too lies on Tal's export.</summary>
    [Test]
    public void TheNearModelHoldsTheAimOfTalsExportToo()
    {
        Assert.AreEqual(CollectionCatalog.PrefabPath(UnitFigures.YoungRecruit, CollectionDetail.Far),
            CollectionCatalog.PrefabPath(UnitFigures.YoungRecruit, CollectionDetail.Near), "one prefab for both details");
        string path = CollectionCatalog.SourceModelPath(CollectionCatalog.C06PointFire);
        var export = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path)); made.Add(export);
        var source = Prefab(CollectionCatalog.C06PointFire).GetComponent<UnitFigurePoseSource>();
        var (motion, piece) = Token(UnitFigures.YoungRecruit);
        var near = Near(piece);
        Assert.IsTrue(motion.PlayAttack());
        source.Clip!.SampleAnimation(export, source.AimTime);
        var hand = UnitWeapons.FindDeep(near, UnitWeapons.HandBone); var hisHand = UnitWeapons.FindDeep(export.transform, UnitWeapons.HandBone);
        Assert.That(Vector3.Distance(near.InverseTransformPoint(hand.position), export.transform.InverseTransformPoint(hisHand.position)),
            Is.LessThan(1e-4f), "the near model's right hand");
        foreach (var bone in source.Bones)
        {
            var mine = UnitWeapons.FindDeep(near, bone); var his = UnitWeapons.FindDeep(export.transform, bone);
            Assert.That(Vector3.Distance(near.InverseTransformPoint(mine.position), export.transform.InverseTransformPoint(his.position)),
                Is.LessThan(1e-4f), bone);
        }
        var rifle = UnitWeapons.FindDeep(export.transform, "tripo_node_0fa6038f-9e8d-4708-a9e8-755be6fdcc67");
        var held = UnitWeapons.FindDeep(near, UnitWeapons.HeldName);
        Assert.IsNotNull(held, "the near model holds the K98k too");
        var ours = held.GetComponentInChildren<MeshFilter>(); var tals = rifle.GetComponent<MeshFilter>();
        var hisPoints = tals.sharedMesh.vertices.Select(v => export.transform.InverseTransformPoint(rifle.TransformPoint(v))).ToArray();
        Assert.That(Nearest(ours.sharedMesh.vertices.Where((_, i) => i % 7 == 0).Select(v => near.InverseTransformPoint(ours.transform.TransformPoint(v))), hisPoints),
            Is.LessThan(1e-4f), "the near model's K98k lies on Tal's");
    }

    /// <summary>The other figures take C06's socket because they share its rig (the same 67 bones): in each one's bind pose
    /// the K98k's long axis stays within 45 degrees of where C06 holds it (across the hand would be about 90), so it hangs
    /// along the hand. Measured on 2 November 2026: C04 34.4 degrees; the others are logged.</summary>
    [Test]
    public void TheOtherFiguresHoldTheRifleAlongTheHandAsC06Does()
    {
        Vector3 Barrel(string figure)
        {
            var skin = Prefab(figure).GetComponentInChildren<SkinnedMeshRenderer>(true);
            int hand = Array.FindIndex(skin.bones, b => b.name == UnitWeapons.HandBone);
            Matrix4x4 RootFrom(Transform t) { var m = Matrix4x4.identity; for (var at = t; at.parent != null; at = at.parent) m = Matrix4x4.TRS(at.localPosition, at.localRotation, at.localScale) * m; return m; }
            var rest = RootFrom(skin.transform) * skin.sharedMesh.bindposes[hand].inverse;   // the hand at bind, in the model's frame
            return rest.MultiplyVector(UnitWeapons.SocketRotation * Vector3.right).normalized;   // Tal's GLBs are long along X
        }
        var c06 = Barrel(UnitFigures.YoungRecruit);
        var angles = UnitWeapons.ByFigure.Keys.Where(f => f != UnitFigures.YoungRecruit)
            .Select(f => (figure: f, degrees: Vector3.Angle(c06, Barrel(f)))).ToList();
        Debug.Log("WEAPON-AXES degrees from C06: " + string.Join(", ",
            angles.Select(a => a.figure + " " + a.degrees.ToString("F1", System.Globalization.CultureInfo.InvariantCulture))));
        foreach (var (figure, degrees) in angles) Assert.Less(degrees, 45f, figure);   // compared as floats: no culture involved
    }
}
