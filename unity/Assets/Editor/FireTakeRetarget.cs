#nullable enable
using System;
using System.IO;
using System.Linq;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Bakes the fire takes the token figures borrow (CollectionImport.BorrowedFireTakes; #2375) as a clip named
    /// fire beside each borrowing figure's model, from Tal Heuberger's own fire takes, with no change to any FBX: the
    /// source's take is played on the figure's model and the figure's own bones are recorded at every frame of the take,
    /// each bone the take moves (a bone it holds still keeps its pose on two keys; one it never touches is left to the
    /// controller's default pose). The figure's rig must bind the source take by bone path (C04 and C07 share one
    /// Armature), so the clip is resampled per frame from the source take (tangents rebuilt, near-static bones dropped).
    /// The baked clip binds the figure's own bone paths, as its walk does. Idempotent: a second bake writes the same
    /// curves into the same asset.
    /// Headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.FireTakeRetarget.Bake</summary>
    public static class FireTakeRetarget
    {
        /// <summary>A bone that turned less than this (degrees) or moved less than this (metres) over the whole take is left
        /// to the controller's default pose, as a take that never keys it.</summary>
        const float StillDegrees = 0.01f, StillMetres = 1e-5f;

        [MenuItem("Statecraft/Collection/Bake borrowed fire takes")]
        public static void Bake()
        {
            foreach (var pair in CollectionImport.BorrowedFireTakes) Bake(pair.Key, pair.Value);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Bakes <paramref name="source"/>'s fire take for <paramref name="figure"/> into CollectionImport.FireTakePath.</summary>
        public static void Bake(string figure, string source)
        {
            string figureModel = CollectionImport.Model(figure, CollectionDetail.Near), sourceModel = CollectionImport.Model(source, CollectionDetail.Near);
            var take = FireTake(sourceModel);
            if (take.isHumanMotion || !BindsByPath(take, figureModel))
                throw new InvalidOperationException(figure + ": " + source + "'s fire take does not bind this figure's rig by bone path");
            var clip = Sample(figureModel, take);
            clip.name = "fire";
            string path = CollectionImport.FireTakePath(figure);
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null) AssetDatabase.CreateAsset(clip, path);
            else { EditorUtility.CopySerialized(clip, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(clip); }
            AssetDatabase.SaveAssets();
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            Debug.Log(FormattableString.Invariant($"FireTakeRetarget: {figure} fires with {source}'s {take.name} (same rig, by bone path): {saved.length:F3} s at {saved.frameRate} fps, {AnimationUtility.GetCurveBindings(saved).Length} curves, {new FileInfo(path).Length} bytes -> {path}"));
        }

        /// <summary>The one fire take among a model's imported takes.</summary>
        internal static AnimationClip FireTake(string model) =>
            AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .SingleOrDefault(c => UnitTokenMotion.CollectionFireTake(c.name))
            ?? throw new InvalidOperationException(model + ": no fire take");

        /// <summary>Whether every bone path <paramref name="clip"/> animates exists on <paramref name="model"/>'s rig, so the
        /// clip binds directly by bone path.</summary>
        internal static bool BindsByPath(AnimationClip clip, string model)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(model) ?? throw new FileNotFoundException("no model", model);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            return bindings.Length > 0 && bindings.All(b => root.transform.Find(b.path) != null);
        }

        /// <summary><paramref name="take"/> played on <paramref name="figureModel"/> frame by frame by bone path, the
        /// figure's own bones recorded as a Generic clip on their paths from its Animator.</summary>
        static AnimationClip Sample(string figureModel, AnimationClip take)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(figureModel) ?? throw new FileNotFoundException("no model", figureModel);
                var go = Object.Instantiate(model);
                SceneManager.MoveGameObjectToScene(go, scene);
                var animator = go.GetComponent<Animator>() ?? throw new InvalidOperationException(figureModel + ": no Animator on the model's root");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                var bones = go.GetComponentsInChildren<Transform>(true).Where(t => t != go.transform).ToArray();
                var restPosition = bones.Select(b => b.localPosition).ToArray();
                var restRotation = bones.Select(b => b.localRotation).ToArray();
                int frames = Mathf.RoundToInt(take.length * take.frameRate) + 1;
                var positions = new Vector3[bones.Length, frames];
                var rotations = new Quaternion[bones.Length, frames];
                var graph = PlayableGraph.Create("FireTakeRetarget sample");
                try
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, take);
                    playable.SetApplyFootIK(false);
                    playable.SetApplyPlayableIK(false);
                    var output = AnimationPlayableOutput.Create(graph, "figure", animator);
                    output.SetSourcePlayable(playable);
                    graph.Play();
                    for (int f = 0; f < frames; f++)
                    {
                        playable.SetTime(f / take.frameRate);
                        graph.Evaluate(0f);
                        for (int b = 0; b < bones.Length; b++)
                        {
                            positions[b, f] = bones[b].localPosition;
                            var q = bones[b].localRotation;
                            // One sign along the take: a flip between frames would interpolate through zero.
                            if (f > 0 && Quaternion.Dot(q, rotations[b, f - 1]) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                            rotations[b, f] = q;
                        }
                    }
                }
                finally { graph.Destroy(); }
                var clip = new AnimationClip { frameRate = take.frameRate };
                int moved = 0;
                for (int b = 0; b < bones.Length; b++)
                {
                    bool turned = false, shifted = false, turning = false, shifting = false;
                    for (int f = 0; f < frames; f++)
                    {
                        turned |= Quaternion.Angle(rotations[b, f], restRotation[b]) > StillDegrees;
                        shifted |= (positions[b, f] - restPosition[b]).magnitude > StillMetres;
                        turning |= Quaternion.Angle(rotations[b, f], rotations[b, 0]) > StillDegrees;
                        shifting |= (positions[b, f] - positions[b, 0]).magnitude > StillMetres;
                    }
                    if (!turned && !shifted) continue;
                    moved++;
                    string path = AnimationUtility.CalculateTransformPath(bones[b], go.transform);
                    if (turned)
                        for (int c = 0; c < 4; c++)
                        {
                            int component = c;
                            clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[c], Curve(turning ? frames : 2, take.frameRate, f => rotations[b, turning ? f : 0][component], frames));
                        }
                    if (shifted)
                        for (int c = 0; c < 3; c++)
                        {
                            int component = c;
                            clip.SetCurve(path, typeof(Transform), "localPosition." + "xyz"[c], Curve(shifting ? frames : 2, take.frameRate, f => positions[b, shifting ? f : 0][component], frames));
                        }
                }
                if (moved == 0) throw new InvalidOperationException(figureModel + ": the take moved no bone");
                clip.EnsureQuaternionContinuity();
                return clip;
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        /// <summary>A key on each of <paramref name="keys"/> frames with smooth, non-overshooting tangents; two keys (the
        /// first and last frame of <paramref name="frames"/>) hold a value that never changes.</summary>
        static AnimationCurve Curve(int keys, float frameRate, Func<int, float> value, int frames)
        {
            var curve = new AnimationCurve();
            for (int k = 0; k < keys; k++) curve.AddKey(new Keyframe((keys == 2 && k == 1 ? frames - 1 : k) / frameRate, value(k)));
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            return curve;
        }

        /// <summary>Contact sheets of the fire takes for review, one PNG per figure in <paramref name="folder"/>: each
        /// borrowing figure's game prefab playing its baked take, and each source figure playing its own, at five points of
        /// the take (rows) seen whole from +Z, -Z and +X and the hands close up from +Z and -Z (columns). Previews of the
        /// assets, not gameplay.</summary>
        public static void Stills(string folder)
        {
            Directory.CreateDirectory(folder);
            var figures = CollectionImport.BorrowedFireTakes.Select(p => (id: p.Key, clip: CollectionImport.FireTakePath(p.Key), borrowed: true))
                .Concat(CollectionImport.BorrowedFireTakes.Values.Distinct().Select(id => (id, clip: CollectionImport.Model(id, CollectionDetail.Near), borrowed: false)));
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var camera = new GameObject("fire take camera").AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.18f, .21f, .24f);
                camera.orthographic = true;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 100f;
                foreach (var angle in new[] { new Vector3(35, -30, 0), new Vector3(25, 160, 0) })
                {
                    var light = new GameObject("fire take light").AddComponent<Light>();
                    SceneManager.MoveGameObjectToScene(light.gameObject, scene);
                    light.type = LightType.Directional;
                    light.intensity = .9f;
                    light.transform.rotation = Quaternion.Euler(angle);
                }
                foreach (var (id, clipPath, borrowed) in figures)
                {
                    var clip = borrowed ? AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) : FireTake(clipPath);
                    if (clip == null) throw new FileNotFoundException("no fire take", clipPath);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near))
                        ?? throw new FileNotFoundException("no prefab", id);
                    const int tile = 320;
                    var fractions = new[] { 0f, .25f, .5f, .75f, 1f };
                    var sheet = new Texture2D(tile * 5, tile * fractions.Length, TextureFormat.RGB24, false);
                    try
                    {
                        for (int row = 0; row < fractions.Length; row++)
                        {
                            // A fresh instance per row: a skinned mesh already drawn is skinned again only by an editor frame,
                            // not by a camera render inside a tool run, so a re-posed instance would draw its first pose.
                            var go = Object.Instantiate(prefab);
                            SceneManager.MoveGameObjectToScene(go, scene);
                            try
                            {
                                var animator = go.GetComponentInChildren<Animator>(true) ?? throw new InvalidOperationException(id + ": no Animator");
                                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                                animator.applyRootMotion = false;
                                animator.Rebind();
                                var bones = go.GetComponentsInChildren<Transform>(true);
                                var right = bones.FirstOrDefault(t => t.name == "Right_Hand");
                                var left = bones.FirstOrDefault(t => t.name == "Left_Hand");
                                var graph = Pose(animator, clip, fractions[row]);
                                try
                                {
                                    var box = Box(go);
                                    var hands = right != null && left != null ? (right.position + left.position) / 2f : box.center;
                                    var views = new (Vector3 from, Vector3 at, float half)[]
                                    {
                                        (Vector3.forward, box.center, box.size.y * .55f), (Vector3.back, box.center, box.size.y * .55f),
                                        (Vector3.right, box.center, box.size.y * .55f),
                                        (Vector3.forward, hands, box.size.y * .14f), (Vector3.back, hands, box.size.y * .14f),
                                    };
                                    for (int col = 0; col < views.Length; col++)
                                    {
                                        var (from, at, half) = views[col];
                                        camera.orthographicSize = half;
                                        camera.transform.position = at + from * Mathf.Max(4f, box.size.magnitude * 3f);
                                        camera.transform.LookAt(at, Vector3.up);
                                        Blit(camera, sheet, col * tile, (fractions.Length - 1 - row) * tile, tile);
                                    }
                                }
                                finally { graph.Destroy(); }
                            }
                            finally { Object.DestroyImmediate(go); }
                        }
                        string file = Path.Combine(folder, $"{id}-fire-{(borrowed ? "borrowed" : "own")}-ASSET-PREVIEW.png");
                        File.WriteAllBytes(file, sheet.EncodeToPNG());
                        Debug.Log($"FireTakeRetarget still: {id} ({(borrowed ? "borrowed" : "own")} fire, {clip.length:F3} s) -> {file}");
                    }
                    finally { Object.DestroyImmediate(sheet); }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static PlayableGraph Pose(Animator animator, AnimationClip clip, float fraction)
        {
            var graph = PlayableGraph.Create("FireTakeRetarget still");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            var output = AnimationPlayableOutput.Create(graph, "figure", animator);
            output.SetSourcePlayable(playable);
            graph.Play();
            playable.SetTime(clip.length * fraction);
            graph.Evaluate(0f);
            return graph;
        }

        /// <summary>The posed skin's world box (the imported rest box can be larger than a pose).</summary>
        static Bounds Box(GameObject go)
        {
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>(true) ?? throw new InvalidOperationException(go.name + ": no skin");
            var baked = new Mesh();
            try
            {
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                var bounds = new Bounds(skin.transform.TransformPoint(vertices[0]), Vector3.zero);
                foreach (var v in vertices) bounds.Encapsulate(skin.transform.TransformPoint(v));
                return bounds;
            }
            finally { Object.DestroyImmediate(baked); }
        }

        static void Blit(Camera camera, Texture2D sheet, int x, int y, int size)
        {
            var rt = new RenderTexture(size, size, 24);
            var prior = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var tile = new Texture2D(size, size, TextureFormat.RGB24, false);
                try
                {
                    tile.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    sheet.SetPixels(x, y, size, size, tile.GetPixels());
                }
                finally { Object.DestroyImmediate(tile); }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = prior;
                Object.DestroyImmediate(rt);
            }
        }
    }
}
