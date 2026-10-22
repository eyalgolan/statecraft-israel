#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Statecraft.Game.VehicleReview
{
    /// <summary>A runnable, neutral art-review environment. It has no map coordinates, date, forces or simulation state.</summary>
    public sealed class VehicleReviewStage : MonoBehaviour
    {
        public const string ReviewNotice = "Vehicle review · Non-historical display. No historical deployment is represented.";
        public const string Flag = "vehicle-review";
        public const float OverviewM = 40000f;
        public sealed class PlacedVehicle
        {
            public string Id = "", Label = "";
            public GameObject Near = null!, Far = null!;
            public Vector3 At;
            public float LongSideM;
        }
        public readonly List<PlacedVehicle> PlacedModels = new List<PlacedVehicle>();
        public IReadOnlyList<PlacedVehicle> Placed => PlacedModels;
        public bool ModelsVisible { get; set; } = true;
        public Camera Camera { get; private set; } = null!;
        public CameraRig Rig { get; private set; } = null!;
        readonly List<TextMesh> labels = new List<TextMesh>();
        Action? restoreScene;
        bool? wasClose;

        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, Flag) : Array.IndexOf(args, "-" + Flag) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OpenRequested()
        {
            if (!Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer)) return;
            var catalog = Resources.Load<VehicleReviewCatalog>(VehicleReviewCatalog.Resource);
            if (catalog == null) { Debug.LogError("Vehicle review models are missing; the normal game continues."); return; }
            catalog.Validate();
            var originals = SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.activeSelf).ToArray();
            foreach (var original in originals) original.SetActive(false);
            try
            {
                var stage = Create(catalog);
                stage.restoreScene = () => { foreach (var original in originals) if (original != null) original.SetActive(true); };
            }
            catch
            {
                foreach (var original in originals) if (original != null) original.SetActive(true);
                throw;
            }
        }

        public static VehicleReviewStage Create(VehicleReviewCatalog catalog)
        {
            catalog.Validate();
            var root = new GameObject("VehicleReview — non-historical layout");
            var stage = root.AddComponent<VehicleReviewStage>();
            var assets = root.AddComponent<MapRuntimeAssets>();
            try
            {
                var sun = new GameObject("Vehicle review sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.SetParent(root.transform);
                var cameraGo = new GameObject("Vehicle review game camera"); cameraGo.transform.SetParent(root.transform); cameraGo.tag = "MainCamera";
                stage.Camera = cameraGo.AddComponent<Camera>();
                stage.Rig = cameraGo.AddComponent<CameraRig>(); stage.Rig.Init();
                stage.Camera.backgroundColor = new Color(.17f, .19f, .20f);
                foreach (var action in stage.Rig.TeardownActions) assets.OnTeardown(action);
                stage.Rig.TeardownActions.Clear();
                stage.Rig.Volume.transform.SetParent(root.transform); assets.Own(stage.Rig.Volume.profile);
                var control = cameraGo.AddComponent<CameraController>();
                control.Bounds = new Rect(-40000f, -30000f, 80000f, 60000f); control.Ground = _ => 0f; control.GroundTop = 0f;
                var ground = assets.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Vehicle review neutral ground", color = new Color(.39f, .40f, .37f) });
                ground.SetFloat("_Smoothness", 0f);
                Box(root.transform, "Neutral review ground", new Vector3(0, -70, 0), new Vector3(60000, 100, 40000), ground);
                var pad = assets.Own(new Material(ground) { name = "Vehicle review pads", color = new Color(.50f, .49f, .43f) });
                var positions = new[] { new Vector3(-8000, 0, 0), Vector3.zero, new Vector3(8000, 0, 0) };
                var shortLabels = new[] { "1 · Spitfire", "2 · R35", "3 · Dakota" };
                for (int i = 0; i < catalog.entries.Length; i++)
                {
                    var entry = catalog.entries[i]; var at = positions[i];
                    Box(root.transform, "Review slot " + (i + 1), at + new Vector3(0, -10, 0), new Vector3(entry.longSideM * 1.3f, 20f, entry.longSideM * 1.3f), pad);
                    var near = Fit(entry.near, root.transform, at, entry.longSideM, entry.id + " near");
                    var far = Fit(entry.far, root.transform, at, entry.longSideM, entry.id + " far");
                    stage.PlacedModels.Add(new PlacedVehicle { Id = entry.id, Label = entry.label, Near = near, Far = far, At = at, LongSideM = entry.longSideM });
                    var box = BoundsOf(near);
                    stage.Label(shortLabels[i], new Vector3(at.x, 150f, box.max.z + box.size.y * 1.3f + 700f), 40f);
                }
                stage.Label("Vehicle review · Non-historical display", new Vector3(0, 0, 8500), 80f);
                stage.Label("Models shown for visual review only · No historical location", new Vector3(0, 0, 7000), 40f);
                stage.Overview(); stage.Choose(stage.Camera);
                return stage;
            }
            catch { Delete(root); throw; }
        }

        static void Box(Transform parent, string name, Vector3 at, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
            go.transform.position = at; go.transform.localScale = size; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Delete(go.GetComponent<Collider>());
        }

        static GameObject Fit(GameObject prefab, Transform parent, Vector3 at, float longSideM, string name)
        {
            var holder = new GameObject(name); holder.transform.SetParent(parent, false);
            var model = Instantiate(prefab, holder.transform, false);
            var box = BoundsOf(model); float size = Mathf.Max(box.size.x, box.size.z);
            if (size <= 1e-6f) throw new InvalidOperationException(name + " has no horizontal extent");
            model.transform.localScale *= longSideM / size;
            box = BoundsOf(model); model.transform.position -= new Vector3(box.center.x, box.min.y, box.center.z);
            holder.transform.position = at;
            return holder;
        }

        public static Bounds BoundsOf(GameObject root)
        {
            bool any = false; var box = new Bounds();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { box = new Bounds(p, Vector3.zero); any = true; } else box.Encapsulate(p);
                }
            }
            if (!any) throw new InvalidOperationException(root.name + " has no static mesh");
            return box;
        }

        void Label(string text, Vector3 at, float size)
        {
            var go = new GameObject(text); go.transform.SetParent(transform); go.transform.position = at;
            var label = go.AddComponent<TextMesh>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material; label.text = text; label.fontSize = 64; label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = new Color(.98f, .94f, .82f);
            labels.Add(label);
        }

        public void Overview() { Rig.LookAt = new Vector3(0, 800f, 2500f); Rig.Distance = OverviewM; Rig.Yaw = 0f; Rig.TopInset = 0f; Rig.Apply(); }
        public void Focus(int index)
        {
            var p = PlacedModels[index]; Rig.LookAt = p.At + Vector3.up * (BoundsOf(p.Near).size.y * .5f);
            Rig.Distance = 12000f; Rig.Yaw = 10f; Rig.TopInset = 0f; Rig.Apply();
        }
        public void Choose(Camera camera)
        {
            if (camera != Camera) return;
            bool close = TownModelLayer.CloseAt(Rig.Distance, wasClose); wasClose = close;
            foreach (var p in PlacedModels) { p.Near.SetActive(ModelsVisible && close); p.Far.SetActive(ModelsVisible && !close); }
            foreach (var label in labels) if (label != null) label.transform.rotation = camera.transform.rotation;
        }
        void Render(ScriptableRenderContext _, Camera camera) => Choose(camera);
        void OnEnable() => RenderPipelineManager.beginCameraRendering += Render;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Render;
        void Update()
        {
            var keys = Keyboard.current; if (keys == null) return;
            if (keys.digit0Key.wasPressedThisFrame) Overview();
            if (keys.digit1Key.wasPressedThisFrame) Focus(0);
            if (keys.digit2Key.wasPressedThisFrame) Focus(1);
            if (keys.digit3Key.wasPressedThisFrame) Focus(2);
            if (keys.escapeKey.wasPressedThisFrame) Close();
        }
        void Start()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-vehicle-proof");
            if (at >= 0 && at + 1 < args.Length && Application.platform != RuntimePlatform.WebGLPlayer)
                StartCoroutine(NativeProof(Path.GetFullPath(args[at + 1])));
        }

        [Serializable] sealed class PlayerEvidence
        {
            public string provenance = "Actual native review-player screen captures, including on-screen non-historical notice";
            public List<PlayerFrame> frames = new List<PlayerFrame>();
        }
        [Serializable] sealed class PlayerFrame
        {
            public string file = ""; public int width, height; public bool modelsVisible;
            public float distance, fov; public Vector3 lookAt, cameraPosition, cameraEuler;
        }
        IEnumerator NativeProof(string output)
        {
            Directory.CreateDirectory(output); var report = new PlayerEvidence();
            for (int shot = -2; shot < PlacedModels.Count; shot++)
            {
                string name;
                if (shot < 0) { Overview(); if (shot == -1) Rig.Distance = 60000f; Rig.Apply(); name = shot == -2 ? "overview40" : "strategic60"; }
                else { Focus(shot); name = PlacedModels[shot].Id; }
                foreach (bool visible in new[] { false, true })
                {
                    ModelsVisible = visible;
                    for (int settle = 0; settle < VisualShots.SettleFrames; settle++) yield return null;
                    yield return new WaitForEndOfFrame();
                    var image = ScreenCapture.CaptureScreenshotAsTexture();
                    string file = name + (visible ? "-after.png" : "-before.png");
                    try { File.WriteAllBytes(Path.Combine(output, file), image.EncodeToPNG()); }
                    finally { Destroy(image); }
                    report.frames.Add(new PlayerFrame { file = file, width = Screen.width, height = Screen.height, modelsVisible = visible,
                        distance = Rig.Distance, fov = Camera.fieldOfView, lookAt = Rig.LookAt, cameraPosition = Camera.transform.position, cameraEuler = Camera.transform.eulerAngles });
                }
            }
            File.WriteAllText(Path.Combine(output, "player-evidence.json"), JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("VEHICLE REVIEW PLAYER PROOF: " + report.frames.Count + " frames -> " + output);
            Application.Quit(0);
        }

        public void Close()
        {
            var restore = restoreScene; restoreScene = null;
            // Disable this camera action map before the original camera re-enables its own shared actions.
            gameObject.SetActive(false); restore?.Invoke(); Delete(gameObject);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Max(14, Screen.height / 55), alignment = TextAnchor.MiddleLeft, wordWrap = true };
            GUI.Box(new Rect(12, 12, Screen.width - 24, style.fontSize * 4 + 24), ReviewNotice +
                "\n1 Egyptian Spitfire candidate · 2 Syrian Renault R35 candidate · 3 Egyptian Dakota C-47 candidate" +
                "\n0 Show all · 1–3 Inspect a model · Wheel Zoom · Q/E Rotate · Right-drag Pan · Esc Close review", style);
        }
        void OnDestroy() { restoreScene?.Invoke(); restoreScene = null; }
        static void Delete(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
    }
}
