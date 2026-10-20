#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Construction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Editor.Construction
{
    /// <summary>One self-owned preview scene: no Map.unity, sim, network, singleton or saved scene changes.</summary>
    public sealed class ConstructionStage : IDisposable
    {
        public const string ModelPath = "Assets/Art/Settlements/Prototype/Models/b05a-moshava-house-v2/b05a-moshava-house-v2.fbx";
        readonly PreviewRenderUtility preview = new PreviewRenderUtility();
        readonly List<Object> owned = new List<Object>();
        readonly GameObject root;
        public ConstructionReveal Reveal { get; }
        public Transform Visual { get; }
        bool disposed;
        public float Zoom = 1f;
        public const float TileRadius = 5f;

        public ConstructionStage()
        {
            try
            {
            preview.camera.fieldOfView = 36f; preview.camera.nearClipPlane = 0.05f; preview.camera.farClipPlane = 100f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = new Color(0.17f, 0.22f, 0.19f);
            preview.ambientColor = MapLighting.AmbientEquator;
            preview.lights[0].transform.rotation = Quaternion.Euler(38f, -40f, 0f);
            preview.lights[0].color = MapLighting.SunColour; preview.lights[0].intensity = 1.45f;
            preview.lights[0].shadows = LightShadows.Soft;
            preview.lights[1].transform.rotation = Quaternion.Euler(65f, 145f, 0f);
            preview.lights[1].color = MapLighting.AmbientSky; preview.lights[1].intensity = 0.6f;
            root = new GameObject("Construction one-hex preview"); preview.AddSingleGO(root);
            Hex("Earth edge", 5f, -0.55f, -0.02f, new Color(0.42f, 0.33f, 0.22f));
            Hex("Dry grass", 4.98f, -0.02f, 0f, new Color(0.52f, 0.58f, 0.34f));
            Disc("Building plot", 1.6f, new Vector3(0f, 0.009f, 0f), new Color(0.62f, 0.56f, 0.39f));
            // One quiet approach path, all on this tile. No added scenery obscures the reveal.
            Cube("Dirt approach", new Vector3(0f, 0.006f, -2.65f), new Vector3(0.6f, 0.012f, 2.7f), new Color(0.65f, 0.59f, 0.42f));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ?? throw new InvalidOperationException("Existing moshava house FBX is missing.");
            var host = new GameObject("Building pick root"); host.transform.SetParent(root.transform, false);
            Visual = new GameObject("Mesh-only visual pivot").transform; Visual.SetParent(host.transform, false);
            var model = Object.Instantiate(prefab, Visual);
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) throw new InvalidOperationException("Representative building has no renderer.");
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float scale = 2.9f / Mathf.Max(b.size.x, b.size.z);
            model.transform.localScale *= scale;
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z) * scale;
            var matPath = Path.ChangeExtension(ModelPath, ".mat");
            var buildingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            foreach (var r in rs) if (buildingMat != null) r.sharedMaterial = buildingMat;
            host.AddComponent<BoxCollider>().size = new Vector3(2.9f, 2f, 2.9f);
            Reveal = host.AddComponent<ConstructionReveal>(); Reveal.Bind(Visual, 1.5f);
            }
            catch
            {
                preview.Cleanup();
                foreach (var o in owned) if (o != null) Object.DestroyImmediate(o);
                owned.Clear(); throw;
            }
        }

        Material Material(Color colour)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = colour, hideFlags = HideFlags.HideAndDontSave };
            m.SetFloat("_Smoothness", 0f); owned.Add(m); return m;
        }
        void MeshObject(string name, Mesh mesh, Color colour)
        {
            owned.Add(mesh); var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = Material(colour);
        }
        void Hex(string name, float radius, float bottom, float top, Color colour)
        {
            var v = new List<Vector3>(); var tri = new List<int>();
            v.Add(new Vector3(0f, top, 0f));
            for (int i = 0; i < 6; i++) { float a = Mathf.Deg2Rad * (90f - i * 60f); v.Add(new Vector3(Mathf.Cos(a) * radius, top, Mathf.Sin(a) * radius)); }
            for (int i = 0; i < 6; i++) { tri.Add(0); tri.Add(i + 1); tri.Add((i + 1) % 6 + 1); }
            for (int i = 0; i < 6; i++)
            {
                int n = v.Count; Vector3 a = v[i + 1], b = v[(i + 1) % 6 + 1];
                v.Add(a); v.Add(new Vector3(a.x, bottom, a.z)); v.Add(new Vector3(b.x, bottom, b.z)); v.Add(b);
                tri.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); MeshObject(name, m, colour);
        }
        void Disc(string name, float radius, Vector3 at, Color colour)
        {
            const int sides = 48; var vertices = new Vector3[sides + 1]; var triangles = new int[sides * 3]; vertices[0] = at;
            for (int i = 0; i < sides; i++) { float a = -i * Mathf.PI * 2f / sides; vertices[i + 1] = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius; triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % sides + 1; }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); MeshObject(name, mesh, colour);
        }
        void Cube(string name, Vector3 at, Vector3 scale, Color colour)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false); go.transform.localPosition = at; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = Material(colour);
        }
        void PositionCamera()
        {
            var cam = preview.camera; cam.transform.rotation = Quaternion.Euler(48f, 22f, 0f);
            cam.transform.position = new Vector3(0f, 0.3f, 0f) - cam.transform.forward * (17.8f * Zoom);
            }
        public void Draw(Rect rect)
        {
            PositionCamera(); preview.BeginPreview(rect, GUIStyle.none); preview.Render(true);
            GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.ScaleToFit);
        }
        public Texture2D Frame(int width, int height)
        {
            PositionCamera(); preview.BeginStaticPreview(new Rect(0, 0, width, height)); preview.Render(true);
            return preview.EndStaticPreview();
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (Reveal != null) Reveal.Cancel(); preview.Cleanup(); foreach (var o in owned) if (o != null) Object.DestroyImmediate(o); owned.Clear();
        }
    }

    public sealed class ConstructionPreview : EditorWindow
    {
        ConstructionStage? stage; bool playing; float time; double last;
        [MenuItem("Statecraft/Construction/One Hex Preview")]
        public static void Open() => GetWindow<ConstructionPreview>("Construction reveal");
        void OnEnable() { stage = new ConstructionStage(); last = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        void OnDisable() { EditorApplication.update -= Tick; stage?.Dispose(); stage = null; }
        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing && stage != null)
            {
                time += (float)Math.Min(now - last, 0.1) * stage.Reveal.Speed;
                stage.Reveal.Sample(time); if (time >= stage.Reveal.Duration) playing = false; Repaint();
            }
            last = now;
        }
        void OnGUI()
        {
            if (stage == null) return;
            var fx = stage.Reveal;
            EditorGUILayout.LabelField("ONE HEX / CONSTRUCTION", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Existing moshava house · isolated preview · no game startup", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play / Replay")) { fx.Play(); time = 0f; playing = true; }
                if (GUILayout.Button("Stop / Final")) { fx.Cancel(); time = fx.Duration; playing = false; }
                if (GUILayout.Button("Close zoom")) stage.Zoom = 0.78f;
                if (GUILayout.Button("Tile zoom")) stage.Zoom = 1f;
                if (GUILayout.Button("Gameplay zoom")) stage.Zoom = 1.8f;
            }
            EditorGUI.BeginChangeCheck();
            fx.Style = (RevealStyle)EditorGUILayout.EnumPopup("Direction", fx.Style);
            fx.Duration = EditorGUILayout.Slider("Duration (seconds)", fx.Duration, 0.6f, 4f);
            fx.Intensity = EditorGUILayout.Slider("Dust intensity", fx.Intensity, 0f, 1.5f);
            fx.Speed = EditorGUILayout.Slider("Playback speed", fx.Speed, 0.1f, 3f);
            fx.Seed = EditorGUILayout.IntField("Visual seed", fx.Seed);
            time = EditorGUILayout.Slider("Scrub", time, 0f, fx.Duration);
            if (EditorGUI.EndChangeCheck()) fx.Sample(time);
            var rect = GUILayoutUtility.GetRect(100, 10000, 100, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint && rect.width > 0 && rect.height > 0)
            {
                stage.Draw(rect);
            }
        }

        // Unity -batchmode ... -executeMethod Statecraft.Editor.Construction.ConstructionPreview.Capture
        static string PendingCapture => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/ConstructionCapture.pending.txt"));
        static double readySince;

        // The worktree-local marker survives MCP-triggered assembly reloads before the renderer is ready.
        // Capture itself is synchronous: Unity cannot reload halfway through its frame loop.
        [InitializeOnLoadMethod]
        static void ResumePendingCapture()
        {
            if (!File.Exists(PendingCapture)) return;
            readySince = 0;
            EditorApplication.update -= WaitForCapture;
            EditorApplication.update += WaitForCapture;
        }
        static void WaitForCapture()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { readySince = 0; return; }
            double now = EditorApplication.timeSinceStartup;
            if (readySince == 0) { readySince = now; return; }
            if (now - readySince < 1.5) return;
            EditorApplication.update -= WaitForCapture;
            CaptureNow(File.ReadAllText(PendingCapture));
        }
        public static void Capture()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../asset-staging/ui-local/construction"));
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-constructionCapture") output = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(PendingCapture)!);
            File.WriteAllText(PendingCapture, output);
            ResumePendingCapture();
        }
        static void CaptureNow(string output)
        {
            try
            {
                Directory.CreateDirectory(output);
                using var stage = new ConstructionStage();
                void Save(string file) { var image = stage.Frame(960, 720); File.WriteAllBytes(Path.Combine(output, file), image.EncodeToPNG()); Object.DestroyImmediate(image); }
                stage.Reveal.Cancel(); Save("original-still.png");
                stage.Reveal.Play();
                int peak = 0;
                for (int i = 0; i <= 90; i++) { stage.Reveal.Sample(i / 30f); Save($"frame-{i:0000}.png"); int live = 0; foreach (var ps in stage.Reveal.GetComponentsInChildren<ParticleSystem>()) live += ps.particleCount; peak = Math.Max(peak, live); }
                Save("final-still.png");
                stage.Zoom = 1.8f; stage.Reveal.Play();
                for (int i = 0; i <= 60; i++) { stage.Reveal.Sample(i / 30f); Save($"gameplay-{i:0000}.png"); }
                foreach (RevealStyle style in Enum.GetValues(typeof(RevealStyle)))
                {
                    stage.Zoom = 1f; stage.Reveal.Style = style; stage.Reveal.Play(); stage.Reveal.Sample(0.65f); Save($"preset-{style}.png");
                }
                File.WriteAllText(Path.Combine(output, "capture-info.txt"), $"Actual Unity 6000.3.25f1 URP editor rendering. 960x720, 30fps.\nModel: {ConstructionStage.ModelPath}\nDuration=1.8s Intensity=1 Seed=1948\nTwo native ParticleSystems; limits 36 dust + 8 flecks; peak total live particles observed {peak}.\nNo simulation loaded. No WebGL build run.\n");
                stage.Dispose();
                File.Delete(PendingCapture);
                Debug.Log("CONSTRUCTION CAPTURE OK " + output); if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { File.Delete(PendingCapture); Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }
    }
}
