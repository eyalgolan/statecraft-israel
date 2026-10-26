#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Statecraft.Game.Review
{
    /// <summary>An isolated, playable art-review stage. Local display coordinates are never campaign positions.</summary>
    public sealed class Generated1948CharacterReview : MonoBehaviour
    {
        [Serializable]
        public sealed class Character
        {
            public string Id = "", Label = "", Status = "";
            public bool Campaign;
            public GameObject Model = null!;
            public Material Material = null!;
            public AnimationClip Idle = null!, Walk = null!;
        }

        [SerializeField] Camera reviewCamera = null!;
        [SerializeField] Character[] characters = Array.Empty<Character>();
        readonly List<GameObject> instances = new List<GameObject>();
        readonly List<PlayableGraph> graphs = new List<PlayableGraph>();
        readonly List<AnimationMixerPlayable> motions = new List<AnimationMixerPlayable>();
        readonly List<Mesh> groundMeshes = new List<Mesh>();
        readonly List<Vector3> groundVertices = new List<Vector3>();
        Material? floorMaterial;
        float yaw = 180f, zoom = 1f;
        bool gameCamera = true;
        bool paused;
        public IReadOnlyList<Character> Characters => characters;
        public IReadOnlyList<GameObject> Instances => instances;
        public Camera ReviewCamera => reviewCamera;
        public string Notice => "Character review. No historical positions or campaign orders.";
        public bool Walking { get; private set; }
        public int Selected { get; private set; } = -1;
        public bool GameCamera => gameCamera;

        void Start() => BuildForReview();

        /// <summary>Build from scene-owned model, material and motion references; no Resources or campaign state.</summary>
        public void BuildForReview()
        {
            if (instances.Count > 0) return;
            if (reviewCamera == null || characters.Length != 7) throw new InvalidOperationException("Review needs its camera and seven characters");
            for (int i = 0; i < characters.Length; i++)
            {
                var entry = characters[i];
                if (entry.Model == null || entry.Material == null || entry.Idle == null || entry.Walk == null)
                    throw new InvalidOperationException("Incomplete review character: " + entry.Id);
                var go = Instantiate(entry.Model, transform);
                go.name = entry.Label;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers) renderer.sharedMaterial = entry.Material;
                var bounds = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);
                go.transform.localScale *= 1.8f / bounds.size.y;
                bounds = renderers[0].bounds;
                for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);
                var display = new Vector3((i < 4 ? i - 1.5f : i - 5f) * 2.5f, .08f, i < 4 ? -1.8f : 1.8f);
                go.transform.position += transform.TransformPoint(display) - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                instances.Add(go);
                groundMeshes.Add(new Mesh());
                var animator = go.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException("Review requires an actual Avatar: " + entry.Id);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var graph = PlayableGraph.Create(entry.Label + " review");
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var motion = AnimationMixerPlayable.Create(graph, 2);
                foreach (var pair in new[] { (entry.Idle, 0), (entry.Walk, 1) })
                {
                    var clip = AnimationClipPlayable.Create(graph, pair.Item1);
                    clip.SetApplyFootIK(false);
                    clip.SetApplyPlayableIK(false);
                    graph.Connect(clip, 0, motion, pair.Item2);
                }
                motion.SetInputWeight(0, 1);
                motion.SetInputWeight(1, 0);
                var output = AnimationPlayableOutput.Create(graph, "Review motion", animator);
                output.SetSourcePlayable(motion);
                graph.Play();
                graph.Evaluate(0);
                graphs.Add(graph);
                motions.Add(motion);
                Ground(i);
            }
            floorMaterial = new Material(characters[0].Material);
            floorMaterial.SetTexture("_BaseMap", null);
            floorMaterial.SetColor("_BaseColor", new Color(.23f, .26f, .28f));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Review stage - local display coordinates";
            floor.transform.SetParent(transform, false);
            floor.transform.localScale = new Vector3(12, .08f, 8);
            floor.transform.localPosition = new Vector3(0, -.04f, 0);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            Remove(floor.GetComponent<Collider>());
            Focus(-1);
        }

        public void SetMotion(bool walk)
        {
            Walking = walk;
            for (int i = 0; i < motions.Count; i++)
            {
                motions[i].SetInputWeight(0, walk ? 0 : 1);
                motions[i].SetInputWeight(1, walk ? 1 : 0);
                motions[i].GetInput(walk ? 1 : 0).SetTime(0);
                graphs[i].Evaluate(0);
                Ground(i);
            }
        }

        public void Focus(int index)
        {
            if (index < -1 || index >= characters.Length) throw new ArgumentOutOfRangeException(nameof(index));
            Selected = index;
            for (int i = 0; i < instances.Count; i++) instances[i].SetActive(index < 0 || i == index);
            zoom = 1f;
            Frame();
        }

        /// <summary>Match the game's pitch/FOV/projection, or inspect cloth and feet with a lower detail view.</summary>
        public void SetGameCamera(bool enabled)
        {
            gameCamera = enabled;
            Frame();
        }

        public void Turn(float degrees) { yaw += degrees; Frame(); }

        /// <summary>Deterministic art-evidence sampling; normal playback continues on GameTime.</summary>
        public void SampleMotion(float seconds)
        {
            for (int i = 0; i < graphs.Count; i++)
            {
                graphs[i].SetTimeUpdateMode(DirectorUpdateMode.Manual);
                motions[i].GetInput(0).SetTime(seconds);
                motions[i].GetInput(1).SetTime(seconds);
                graphs[i].Evaluate(0);
                Ground(i);
                graphs[i].SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            }
        }

        void Frame()
        {
            var target = Selected < 0 ? transform.position + Vector3.up : new Vector3(
                instances[Selected].transform.position.x, transform.position.y + .95f, instances[Selected].transform.position.z);
            reviewCamera.orthographic = !gameCamera;
            reviewCamera.fieldOfView = CameraRig.Fov;
            reviewCamera.nearClipPlane = .03f;
            reviewCamera.farClipPlane = 1000f;
            reviewCamera.orthographicSize = (Selected < 0 ? 4.4f : 1.5f) * zoom;
            reviewCamera.transform.rotation = Quaternion.Euler(gameCamera ? CameraRig.Pitch : 12, yaw, 0);
            float distance = (Selected < 0 ? 19f : 5f) * zoom;
            reviewCamera.transform.position = target - reviewCamera.transform.forward * distance;
        }

        void OnGUI()
        {
            if (instances.Count == 0) return;
            var previous = GUI.matrix;
            float scale = Screen.width / 1000f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float height = Screen.height / scale;
            GUILayout.BeginArea(new Rect(12, 10, 976, 72), GUI.skin.box);
            GUILayout.Label("CHARACTER REVIEW — seven generated figures");
            GUILayout.Label(Notice);
            GUILayout.Label("Idle and walk are animation review only. Campaign movement and deployments stay unchanged.");
            GUILayout.EndArea();
            foreach (var pair in CharactersWithPositions())
            {
                if (Selected >= 0 && pair.index != Selected) continue;
                var screen = reviewCamera.WorldToScreenPoint(pair.position);
                if (screen.z <= 0) continue;
                var rect = new Rect(screen.x / scale - 118, (Screen.height - screen.y) / scale - 46, 236, 48);
                GUI.Box(rect, characters[pair.index].Label + "\n" + characters[pair.index].Status);
            }
            GUILayout.BeginArea(new Rect(12, height - 130, 976, 120), GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All seven")) Focus(-1);
            if (GUILayout.Button("Idle")) SetMotion(false);
            if (GUILayout.Button("Walk in place")) SetMotion(true);
            if (GUILayout.Button("Game angle")) SetGameCamera(true);
            if (GUILayout.Button("Detail")) SetGameCamera(false);
            if (GUILayout.Button(paused ? "Play" : "Pause"))
            {
                paused = !paused;
                foreach (var graph in graphs) { if (paused) graph.Stop(); else graph.Play(); }
            }
            if (GUILayout.Button("Turn left")) Turn(-30);
            if (GUILayout.Button("Turn right")) Turn(30);
            if (GUILayout.Button("Rear view")) Turn(180);
            if (GUILayout.Button("Zoom in")) { zoom = Mathf.Max(.7f, zoom * .8f); Frame(); }
            if (GUILayout.Button("Zoom out")) { zoom = Mathf.Min(2, zoom * 1.25f); Frame(); }
            GUILayout.EndHorizontal();
            GUILayout.Label((Walking ? "Playing walk in place. " : "Playing idle. ") +
                (gameCamera ? "Game camera angle on an isolated review stage." : "Lower detail view on an isolated review stage."));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < characters.Length; i++) if (GUILayout.Button(characters[i].Label)) Focus(i);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.matrix = previous;
        }

        IEnumerable<(int index, Vector3 position)> CharactersWithPositions()
        {
            for (int i = 0; i < instances.Count; i++) yield return (i, new Vector3(
                instances[i].transform.position.x, transform.position.y + 2.1f, instances[i].transform.position.z));
        }
        void LateUpdate()
        {
            for (int i = 0; i < instances.Count; i++) if (instances[i].activeSelf) Ground(i);
        }
        void Ground(int index)
        {
            var skin = instances[index].GetComponentInChildren<SkinnedMeshRenderer>(true);
            // These delivered model instances have non-unit skin scale. Include it when baking; the default false
            // plus TransformPoint disagrees with the actual renderer envelope (recorded ALA scale probe).
            skin.BakeMesh(groundMeshes[index], true);
            groundMeshes[index].GetVertices(groundVertices);
            float bottom = float.PositiveInfinity;
            foreach (var vertex in groundVertices)
                bottom = Mathf.Min(bottom, skin.transform.TransformPoint(vertex).y);
            instances[index].transform.position += Vector3.up * (transform.position.y + .01f - bottom);
        }
        static void Remove(UnityEngine.Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        void OnDestroy()
        {
            foreach (var graph in graphs) if (graph.IsValid()) graph.Destroy();
            foreach (var mesh in groundMeshes) Remove(mesh);
            if (floorMaterial != null) Remove(floorMaterial);
        }
    }
}

#endif
