#nullable enable
using System;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Construction;
using Statecraft.HexTiles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statecraft.Editor.Construction
{
    /// <summary>One existing game hex, built by the game renderer in a private preview scene. No game/sim startup.</summary>
    public sealed class ConstructionStage : IDisposable
    {
        public const string BoardId = "central_israel", PrototypeId = "moshava_village";
        public const int BoardQ = 1, BoardR = 13;
        public const string BuildingModelId = "sandbox:kit_moshava_2";
        readonly PreviewRenderUtility preview = new PreviewRenderUtility();
        readonly GameObject root;
        public ConstructionReveal Reveal { get; }
        public Transform Visual { get; }
        public int PlacementCount { get; }
        public int MissingModels { get; }
        public Vector3 TargetPosition { get; }
        public Vector3 TileCentre { get; }
        public float Zoom = 0.56f;
        public const float TileRadius = HexScale.HexM;
        bool disposed;

        public ConstructionStage()
        {
            root = new GameObject("Construction real-hex preview"); preview.AddSingleGO(root);
            try
            {
                preview.camera.fieldOfView = 35f; preview.camera.nearClipPlane = 25f; preview.camera.farClipPlane = 100000f;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.13f, 0.20f, 0.18f);
                preview.ambientColor = MapLighting.AmbientEquator * 1.15f;
                preview.lights[0].transform.rotation = Quaternion.Euler(MapLighting.SunElevation, MapLighting.SunAzimuth - 180f, 0f);
                preview.lights[0].color = Color.Lerp(MapLighting.SunColour, new Color(1f, 0.88f, 0.70f), 0.35f);
                preview.lights[0].intensity = MapLighting.SunIntensity * HexLook.SunBoost;
                preview.lights[0].shadows = LightShadows.Soft;
                preview.lights[1].transform.rotation = Quaternion.Euler(65f, 145f, 0f);
                preview.lights[1].color = MapLighting.AmbientSky; preview.lights[1].intensity = 0.6f;

                // Preserve the actual board coordinates, so the prototype's seeded arrangement is unchanged.
                var original = HexBoard.Load(BoardId);
                var h = original.Hexes[(BoardQ, BoardR)];
                if (h.prototype != PrototypeId) throw new InvalidOperationException("The construction preview hex no longer has its expected prototype.");
                // Keep the real immediate ground neighbours; missing context would turn this into an island.
                var context = original.Hexes.Values.Where(x => HexGeometry.Distance(h.q, h.r, x.q, x.r) <= 1)
                    .Select(x => x == h ? h : new BoardHex { q = x.q, r = x.r, terrain = x.terrain, ground = x.ground }).ToArray();
                var board = new HexBoard(new HexBoardFile { id = "construction-one-hex", hexes = context });
                var surface = new HexBoardSurface(board);
                var centre = HexGeometry.Centre(h.q, h.r, HexScale.HexM);
                var (boardRoot, _) = HexBoardBuilder.Build(board, Ground, seed: 1948,
                    options: new HexBoardBuilder.Options { Parent = root.transform, Origin = -centre, OwnSea = false, HolesOutsideLand = true });
                var assets = boardRoot.GetComponent<HexBoardAssets>();
                TileCentre = new Vector3(0f, surface.Height(centre), 0f);
                var ctx = new HexTileContext(h.q, h.r, surface, seed: 1948);
                HexPrototypes.Create(h.prototype).Build(ctx);
                PlacementCount = ctx.Placements.Count;
                MissingModels = ctx.Placements.Count(p => p.Model != "" && HexModels.Prefab(HexModels.Resolve(p.Model, p.Position)) == null);
                // Use an existing roofed inner house: edge houses leave too little room for the dust footprint.
                var chosen = ctx.Placements.Where(p => p.Model == BuildingModelId)
                    .OrderBy(p => (p.Position - centre).sqrMagnitude).ThenBy(p => p.Position.x).First();

                // Only split this preview's combined render objects; their meshes remain owned by HexBoardAssets.
                Object.DestroyImmediate(boardRoot.transform.Find("HexBoardPieces").gameObject);
                HexBoardRenderer.Emit(boardRoot.transform, ctx.Placements.Where(p => !p.Equals(chosen)), assets);
                var host = new GameObject("Building pick root"); host.transform.SetParent(boardRoot.transform, false);
                host.transform.localPosition = chosen.Position;
                Visual = new GameObject("Mesh-only visual pivot").transform; Visual.SetParent(host.transform, false);
                var rebased = new HexPlacement(chosen.Model, chosen.StandIn, chosen.Fit, chosen.Tint,
                    Matrix4x4.Translate(-chosen.Position) * chosen.Matrix, chosen.Q, chosen.R);
                HexBoardRenderer.Emit(Visual, new[] { rebased }, assets);
                var bounds = HexModels.Bounds(Visual.gameObject);
                float radius = new Vector2(bounds.size.x, bounds.size.z).magnitude * 0.5f;
                if (!HexGeometry.InHex(chosen.Position, centre, HexScale.HexM, HexScale.HexM * HexScale.InsetShare + radius * 2.25f))
                    throw new InvalidOperationException($"The chosen building has insufficient inset ground: offset={chosen.Position - centre}, radius={radius}, bounds={bounds.size}.");
                var pick = host.AddComponent<BoxCollider>(); pick.center = host.transform.InverseTransformPoint(bounds.center); pick.size = bounds.size;
                Reveal = host.AddComponent<ConstructionReveal>(); Reveal.Bind(Visual, radius);
                Reveal.GroundOffset = Mathf.Max(0f, surface.Height(chosen.Position) - chosen.Position.y);
                TargetPosition = host.transform.position;
            }
            catch { preview.Cleanup(); throw; }
        }

        static (Texture2D?, Texture2D?) Ground(string name)
        {
            const string folder = "Assets/Art/Settlements/Prototype/HexSandboxGround/";
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(folder + name + "_diff_2k.jpg"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(folder + name + "_nor_gl_2k.jpg"));
        }
        void PositionCamera()
        {
            var cam = preview.camera; cam.transform.rotation = Quaternion.Euler(50f, 22f, 0f);
            var at = Zoom < 0.4f ? TargetPosition + Vector3.up * Reveal.FootprintRadius * 0.35f : TileCentre;
            cam.transform.position = at - cam.transform.forward * (26000f * Zoom);
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
            if (Reveal != null) Reveal.Cancel(); preview.Cleanup();
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
            EditorGUILayout.LabelField("Actual moshava_village hex · game terrain and placements · no simulation", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play / Replay")) { fx.Play(); time = 0f; playing = true; }
                if (GUILayout.Button("Stop / Final")) { fx.Cancel(); time = fx.Duration; playing = false; }
                if (GUILayout.Button("Close zoom")) stage.Zoom = 0.23f;
                if (GUILayout.Button("Tile zoom")) stage.Zoom = 0.56f;
                if (GUILayout.Button("Gameplay zoom")) stage.Zoom = 1f;
            }
            EditorGUI.BeginChangeCheck();
            fx.Look = (ConstructionLook)EditorGUILayout.EnumPopup("Comparison", fx.Look);
            fx.Style = (RevealStyle)EditorGUILayout.EnumPopup("Direction", fx.Style);
            fx.Duration = EditorGUILayout.Slider("Duration (seconds)", fx.Duration, 0.6f, 4f);
            fx.Intensity = EditorGUILayout.Slider("Dust intensity", fx.Intensity, 0f, 1.5f);
            fx.Speed = EditorGUILayout.Slider("Playback speed", fx.Speed, 0.1f, 3f);
            fx.Seed = EditorGUILayout.IntField("Visual seed", fx.Seed);
            time = EditorGUILayout.Slider("Scrub", time, 0f, fx.Duration);
            if (EditorGUI.EndChangeCheck()) fx.Sample(time);
            if (stage.MissingModels > 0) EditorGUILayout.HelpBox($"{stage.MissingModels} local game models are unavailable; existing game stand-ins are shown.", MessageType.Info);
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
                int peak = 0;
                foreach (var view in new[] { "normal", "close" })
                {
                    stage.Zoom = view == "normal" ? 1f : 0.23f;
                    foreach (ConstructionLook look in Enum.GetValues(typeof(ConstructionLook)))
                    {
                        stage.Reveal.Look = look; stage.Reveal.Play();
                        for (int i = 0; i <= 66; i++)
                        {
                            stage.Reveal.Sample(i / 30f); Save($"{view}-{look.ToString().ToLowerInvariant()}-{i:0000}.png");
                            int live = 0; foreach (var ps in stage.Reveal.GetComponentsInChildren<ParticleSystem>()) live += ps.particleCount;
                            peak = Math.Max(peak, live);
                        }
                    }
                }
                stage.Zoom = 0.56f; stage.Reveal.Cancel(); Save("real-tile-final.png");
                File.WriteAllText(Path.Combine(output, "capture-info.txt"),
                    $"Actual Unity 6000.3.25f1 URP editor rendering. 960x720, 30fps.\n" +
                    $"Existing board={ConstructionStage.BoardId}, prototype={ConstructionStage.PrototypeId}, q={ConstructionStage.BoardQ}, r={ConstructionStage.BoardR}, model={ConstructionStage.BuildingModelId}, seed=1948, placements={stage.PlacementCount}, missing models={stage.MissingModels}.\n" +
                    $"Original v1 vs Stones on same tile/camera. Normal distance=26000m; close=5980m.\n" +
                    $"Two systems, 36 soft puffs + 8 faceted mesh stones maximum; peak={peak}. Duration=1.8 seconds.\n" +
                    "No simulation loaded. No WebGL build run. Preview uses game sun constants and camera pitch/FOV, without the global post-processing profile.\n");
                stage.Dispose();
                File.Delete(PendingCapture);
                Debug.Log("CONSTRUCTION CAPTURE OK " + output); if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { File.Delete(PendingCapture); Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }
    }
}
