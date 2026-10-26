#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Game.VehicleReview
{
    /// <summary>A runnable, neutral art-review environment. It has no map coordinates, date, forces or simulation state.</summary>
    public sealed class VehicleReviewStage : MonoBehaviour
    {
        public const string ReviewNotice = "Vehicle review · Non-historical display. No historical deployment is represented.";
        public const float OverviewM = 40000f;
        public sealed class PlacedVehicle
        {
            public string Id = "", Label = "";
            public GameObject Model = null!;
            public Vector3 At;
            public float LongSideM;
        }
        public readonly List<PlacedVehicle> PlacedModels = new List<PlacedVehicle>();
        public IReadOnlyList<PlacedVehicle> Placed => PlacedModels;
        public bool ModelsVisible { get; set; } = true;
        public Camera Camera { get; private set; } = null!;
        public CameraRig Rig { get; private set; } = null!;
        readonly List<TextMesh> labels = new List<TextMesh>();

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
                    var model = Fit(entry.model, root.transform, at, entry.longSideM, entry.id);
                    stage.PlacedModels.Add(new PlacedVehicle { Id = entry.id, Label = entry.label, Model = model, At = at, LongSideM = entry.longSideM });
                    var box = BoundsOf(model);
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
            var p = PlacedModels[index]; Rig.LookAt = p.At + Vector3.up * (BoundsOf(p.Model).size.y * .5f);
            Rig.Distance = 12000f; Rig.Yaw = 10f; Rig.TopInset = 0f; Rig.Apply();
        }
        public void Choose(Camera camera)
        {
            if (camera != Camera) return;
            foreach (var p in PlacedModels) p.Model.SetActive(ModelsVisible);
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
        public void Close()
        {
            gameObject.SetActive(false); Delete(gameObject);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Max(14, Screen.height / 55), alignment = TextAnchor.MiddleLeft, wordWrap = true };
            GUI.Box(new Rect(12, 12, Screen.width - 24, style.fontSize * 4 + 24), ReviewNotice +
                "\n1 Egyptian Spitfire candidate · 2 Syrian Renault R35 candidate · 3 Egyptian Dakota C-47 candidate" +
                "\n0 Show all · 1–3 Inspect a model · Wheel Zoom · Q/E Rotate · Right-drag Pan · Esc Close review", style);
        }
        static void Delete(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
    }
}
#endif
