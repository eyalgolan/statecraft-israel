using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game;
using Statecraft.Game.EditorTools;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Editor capture tools. Run headless (with graphics, not -nographics):
/// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod MapCapture.TopDown (or MapCapture.Flythrough).
/// Captures go to captures/ at the repo root, which git ignores.</summary>
public static class MapCapture
{
    const string ScenePath = "Assets/Map.unity";

    static string CaptureDir => Path.Combine(Paths.RepoRoot, "captures");

    /// <summary>Creates and saves Assets/Map.unity, the boot scene: the default camera (with CameraRig and
    /// CameraController) and light, a MapScene wired to the material assets, the GameBoot that loads the inputs and builds
    /// the map in play mode and in players, the placement layer, the map pointer, and the UI document with GameRoot wired
    /// to all of them. No EventSystem: UI Toolkit reads the Input System directly.</summary>
    public static void MakeScene()
    {
        MapMaterials.Create();
        UiAssets.Create();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var controller = Camera.main.gameObject.AddComponent<CameraController>();   // adds the CameraRig it requires
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(MapMaterials.TerrainPath), Load(MapMaterials.MarkerPath), Load(MapMaterials.SelectionPath), Load(MapMaterials.SeaPath), Load(MapMaterials.FramePath));
        map.ConfigureKits(Load(MapMaterials.KitPath), Load(MapMaterials.TreePath));
        var boot = new GameObject("Boot").AddComponent<GameBoot>();
        boot.Configure(map);
        var placements = new GameObject("Placements").AddComponent<PlacementLayer>();
        var pointer = new GameObject("MapPointer").AddComponent<MapPointer>();
        var ui = new GameObject("UI");
        var doc = ui.AddComponent<UIDocument>();
        doc.panelSettings = Asset<PanelSettings>(UiPaths.Panel);
        doc.visualTreeAsset = Asset<VisualTreeAsset>(UiPaths.GameUxml);
        ui.AddComponent<GameRoot>().Configure(boot, doc, Asset<TextAsset>(UiPaths.Strings), Asset<TextAsset>(UiPaths.StringsHebrew),
            Asset<PortraitCatalog>(PortraitImport.CatalogPath), map, placements, pointer, controller);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static T Asset<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) throw new FileNotFoundException($"{typeof(T).Name} asset missing", path);
        return a;
    }

    /// <summary>Opens Assets/Map.unity (creating it only when missing) and builds the map in memory. The scene is not
    /// saved, so captures leave it unchanged.</summary>
    static MapScene OpenAndBuild()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) MakeScene();
        else EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var map = RequireMapScene(); map.Build();
        return map;
    }

    /// <summary>The open scene's MapScene; throws naming the scene and the fix when there is none.</summary>
    public static MapScene RequireMapScene()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        if (map == null)
            throw new System.InvalidOperationException($"{ScenePath} has no MapScene component. Delete {ScenePath} so the next " +
                "capture recreates it, or run MapCapture.MakeScene.");
        return map;
    }

    static Material Load(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) throw new FileNotFoundException("material asset missing", path);
        return m;
    }

    /// <summary>Orthographic straight-down render of the whole map, for comparison with promo/ref/geo-reference.png.</summary>
    public static void TopDown()
    {
        var map = OpenAndBuild();
        var cam = new GameObject("TopCam").AddComponent<Camera>();
        var size = map.Terrain.terrainData.size;
        cam.orthographic = true; cam.orthographicSize = size.z / 2; cam.aspect = size.x / size.z;
        cam.transform.position = new Vector3(size.x / 2, 100000, size.z / 2); cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        cam.farClipPlane = 300000;
        Save(cam, Mathf.RoundToInt(2000 * size.x / size.z), 2000, Path.Combine(CaptureDir, "topdown.png"));
    }

    /// <summary>240 frames flying north along the coast at diorama pitch; encode with ffmpeg.</summary>
    public static void Flythrough()
    {
        var map = OpenAndBuild();
        var rig = Camera.main.GetComponent<CameraRig>();
        var dir = Path.Combine(CaptureDir, "fly"); Directory.CreateDirectory(dir);
        for (int i = 0; i < 240; i++)
        {
            double lat = 31.3 + (32.9 - 31.3) * i / 239.0; // from the northern Negev to the Carmel
            rig.LookAt = map.Frame.ToWorld(34.95, lat, 0); rig.Distance = 90000f; rig.Apply();
            Save(Camera.main, 1920, 1080, Path.Combine(dir, $"{i:D4}.png"));
        }
    }

    /// <summary>The surf in motion (promo-sea review I2): 60 frames, consecutive at 60 fps unless -surfDt gives the seconds
    /// between them (the water's clock pinned through MapShaderGlobals.PinWaterTime) of the gameplay03 pose on the web tier (quality level 0), 1920x1080, into
    /// &lt;-captureDir&gt; (default captures/visual/surf) as surf-NNN.png; encode or diff them to judge crawl and shimmer,
    /// which stills cannot show.</summary>
    public static void SurfFrames()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        var dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(CaptureDir, "visual", "surf");
        int dtAt = System.Array.IndexOf(args, "-surfDt");   // seconds between frames (default 1/60: consecutive frames)
        float dt = dtAt >= 0 && dtAt + 1 < args.Length ? float.Parse(args[dtAt + 1], System.Globalization.CultureInfo.InvariantCulture) : 1f / 60f;
        var map = OpenAndBuild();
        var rig = Camera.main.GetComponent<CameraRig>();
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(0, true);
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            for (int i = 0; i < 60; i++)
            {
                MapShaderGlobals.PinWaterTime(100f + i * dt);
                Save(Camera.main, 1920, 1080, Path.Combine(dir, $"surf-{i:D3}.png"));
            }
        }
        finally { MapShaderGlobals.PinWaterTime(null); QualitySettings.SetQualityLevel(saved, true); }
    }

    /// <summary>The open sea up close (bug B10, the checker): the web tier (quality level 0) at 6, 12 and 18 km over the sea
    /// west of Tel Aviv (34.70 E, 32.08 N), the water's clock pinned, at 1920x1080 into &lt;-captureDir&gt; (default
    /// captures/visual/sea-close) as sea-&lt;km&gt;km.png.</summary>
    public static void SeaClose()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        var dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(CaptureDir, "visual", "sea-close");
        var map = OpenAndBuild();
        var rig = Camera.main.GetComponent<CameraRig>();
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(0, true);
            MapShaderGlobals.PinWaterTime(100f);
            var look = map.Frame.ToWorld(34.70, 32.08, 0); look.y = map.Terrain.SampleHeight(look);
            foreach (var km in new[] { 6, 12, 18 })
            {
                rig.LookAt = look; rig.Distance = km * 1000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
                Save(Camera.main, 1920, 1080, Path.Combine(dir, $"sea-{km}km.png"));
            }
        }
        finally { MapShaderGlobals.PinWaterTime(null); QualitySettings.SetQualityLevel(saved, true); }
    }

    /// <summary>Renders each promo pose (PromoPose.All) at 1920x1080 once per quality tier (0 = Mobile, the WebGL tier;
    /// 1 = PC) into &lt;-captureDir&gt; (default captures/visual/editor) as &lt;tier&gt;-&lt;pose&gt;.png. Editor renders show no UI
    /// Toolkit labels; tools/art_checkpoint.sh pairs them with the WebGL screenshots, which do. Units only with -units (from
    /// tools/art_checkpoint.sh --units), as ?shot&amp;units shows them, so plain renders stay comparable across tracks: then
    /// the unit art slice (UnitShowcase) and the units in play as a new game starts (UnitLayer, as the player shows them
    /// on its first turn), both still.</summary>
    public static void Promo()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        var dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(CaptureDir, "visual", "editor");
        var map = OpenAndBuild();
        // The unit art slice and the units in play only when asked (-units, from tools/art_checkpoint.sh --units).
        if (System.Array.IndexOf(args, "-units") >= 0)
        {
            Statecraft.Game.Map.Units.UnitShowcase.Spawn(map, still: true);
            ShowStartUnits(map);
        }
        // The 1948 sub-districts and the 1949 armistice lines, as play draws them (GameRoot.Begin), so the editor renders pair
        // with the WebGL shots: on the game's start by default, or on -regionsDate yyyy-MM-dd (a 1949 date shows the
        // armistice zones in force then).
        var subdistricts = Subdistricts1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Subdistricts1948.FileName)));
        var regions = new MapRegions(subdistricts, Armistice1949.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Armistice1949.FileName))));
        int dateAt = System.Array.IndexOf(args, "-regionsDate");
        var date = dateAt >= 0 && dateAt + 1 < args.Length
            ? System.DateTime.ParseExact(args[dateAt + 1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            : subdistricts.ShownFrom;
        regions.Apply(date);
        // The border renderer's synthetic proof only when asked (-borderproof, from tools/art_checkpoint.sh --borderproof).
        if (System.Array.IndexOf(args, "-" + BorderProof.Flag) >= 0) BorderProof.Show(map);
        var rig = Camera.main.GetComponent<CameraRig>();
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, true);
                foreach (var pose in Poses(args))
                {
                    pose.Apply(rig, map.Frame);
                    Statecraft.Game.Map.Units.UnitFlagFacing.FaceAll(Camera.main.transform.position);   // the flags face this pose's camera (#559)
                    foreach (var layer in Object.FindObjectsByType<Statecraft.Game.Map.Units.UnitLayer>(FindObjectsSortMode.None)) layer.FaceCamera(Camera.main);   // and the badges
                    Save(Camera.main, 1920, 1080, Path.Combine(dir, $"{QualitySettings.names[level]}-{pose.Name}.png"));
                }
            }
        }
        finally { QualitySettings.SetQualityLevel(saved, true); }
    }

    /// <summary>The scripted trailer capture (issue #45): flies the camera through TrailerShot.Default and renders every frame
    /// at 1920x1080 on the desktop tier (PC) into &lt;-captureDir&gt; (default captures/trailer) as frame-NNNNN.png, one numbered
    /// sequence across the shots, with the water's clock pinned to the frame's time so a re-run gives the same camera, state
    /// and water on every frame (the pixels too, but for the unit badges' glyphs at the whole-country zoom; see
    /// docs/RELEASING.md "Capturing trailer footage"). The map as a new game starts: the 1948 sub-districts and the units on
    /// turn 1 of a historical game started with -seed (default the golden seed; turn 1's units do not depend on it today,
    /// but the seed is what makes a later game moment repeatable). PNG frames, not video: a frame is written only once it is
    /// rendered, so a slow software renderer gives the same footage, and ffmpeg encodes them. trailer.txt beside them
    /// records the commit, seed and settings. Editor renders draw no UI Toolkit, so this is the map only.</summary>
    public static void Trailer()
    {
        var args = System.Environment.GetCommandLineArgs();
        string Arg(string name) { int i = System.Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var dir = Arg("-captureDir") ?? Path.Combine(CaptureDir, "trailer");
        var seed = Arg("-seed") is string s ? ulong.Parse(s, System.Globalization.CultureInfo.InvariantCulture) : Statecraft.Sim.GoldenRun.Seed;
        int tier = System.Array.IndexOf(QualitySettings.names, "PC");
        if (tier < 0) throw new System.InvalidOperationException("no PC quality level: the trailer is captured on the desktop tier");
        var map = OpenAndBuild();
        ShowStartUnits(map, seed);
        var subdistricts = Subdistricts1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Subdistricts1948.FileName)));
        new MapRegions(subdistricts, Armistice1949.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Armistice1949.FileName))))
            .Apply(subdistricts.ShownFrom);
        // Frames left from an earlier, longer run would end up in the encode (ffmpeg reads frame-%05d.png until a gap).
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, "frame-*.png")) File.Delete(old);
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.TopInset = 0;
        int saved = QualitySettings.GetQualityLevel(), frame = 0;
        var shots = new System.Text.StringBuilder();
        try
        {
            QualitySettings.SetQualityLevel(tier, true);
            foreach (var shot in TrailerShot.Default)
            {
                shots.AppendLine($"shot {shot.Name}: {shot.From.Name} -> {shot.To.Name}, frames {frame}-{frame + shot.Frames - 1}");
                for (int i = 0; i < shot.Frames; i++, frame++)
                {
                    shot.PoseAt(i).Apply(rig, map.Frame);
                    MapShaderGlobals.PinWaterTime(100f + frame / (float)TrailerShot.Fps);
                    Statecraft.Game.Map.Units.UnitFlagFacing.FaceAll(Camera.main.transform.position);
                    foreach (var layer in Object.FindObjectsByType<Statecraft.Game.Map.Units.UnitLayer>(FindObjectsSortMode.None)) layer.FaceCamera(Camera.main);
                    Save(Camera.main, 1920, 1080, Path.Combine(dir, $"frame-{frame:D5}.png"));
                }
            }
        }
        finally { MapShaderGlobals.PinWaterTime(null); QualitySettings.SetQualityLevel(saved, true); }
        File.WriteAllText(Path.Combine(dir, "trailer.txt"),
            $"commit: {Commit()}\nunity: {Application.unityVersion}\nseed: {seed}\nsize: 1920x1080\nfps: {TrailerShot.Fps}\n" +
            $"tier: PC\nframes: {frame}\n{shots}");
        Debug.Log($"TRAILER {frame} frames in {dir}");
    }

    /// <summary>The checked-out commit for trailer.txt, "-dirty" when the tree has uncommitted changes (they are in the frames
    /// but not in the commit), or "unknown" when git cannot say.</summary>
    static string Commit()
    {
        var sha = Git("rev-parse HEAD");
        if (sha == null || sha.Length == 0) return "unknown";
        var status = Git("status --porcelain");
        return status == null ? sha + "-unknown-status" : status.Length > 0 ? sha + "-dirty" : sha;
    }

    /// <summary>git's trimmed output in the repo root, or null when git is missing or fails.</summary>
    static string Git(string arguments)
    {
        try
        {
            using var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments)
                { WorkingDirectory = Paths.RepoRoot, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
            if (git == null) return null;
            var output = git.StandardOutput.ReadToEnd().Trim(); git.StandardError.ReadToEnd(); git.WaitForExit();
            return git.ExitCode == 0 ? output : null;
        }
        catch (System.Exception e) when (e is System.ComponentModel.Win32Exception || e is System.InvalidOperationException) { return null; }
    }

    /// <summary>The units in play as a new game starts (golden seed unless the trailer passes its -seed, historical mode): what
    /// the player draws on turn 1, or, with -unitsDate, on that day.</summary>
    static void ShowStartUnits(MapScene map, ulong seed = Statecraft.Sim.GoldenRun.Seed)
    {
        var content = Statecraft.Sim.ContentLoader.Load(Paths.Content);
        var units = Statecraft.Game.Map.Units.UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Units.UnitData.DefaultFileName)));
        var layer = Statecraft.Game.Map.Units.UnitLayer.Create(map, units, content.Hexes, still: true);
        var state = Statecraft.Sim.Simulation.NewGame(content, seed, Statecraft.Sim.Mode.Historical);
        // -unitsDate YYYY-MM-DD: the units as on that day (a render of a unit outside the first turn; the sim never sees it).
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-unitsDate");
        if (at >= 0 && at + 1 < args.Length)
        {
            state = state.Clone();
            state.Date = Statecraft.Game.Map.Units.UnitData.Day(args[at + 1]);
        }
        layer.Show(content, state);
    }

    /// <summary>PromoPose.All, plus any "-extraPose name:lon:lat:distanceM:yaw" given (close looks while tuning the art, for
    /// example the terraces at 20 km; not promo matches). All are parsed before any capture, so a bad one fails first.</summary>
    static List<PromoPose> Poses(string[] args)
    {
        var poses = new List<PromoPose>(PromoPose.All);
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-extraPose")
                poses.Add(ParseExtraPose(i + 1 < args.Length ? args[i + 1] : ""));
        return poses;
    }

    /// <summary>One -extraPose value: name (letters, digits, '-' and '_', not a promo pose's name, so it never overwrites
    /// that capture), longitude and latitude inside the map, distance (m) and yaw (degrees). Throws ArgumentException
    /// naming the expected form.</summary>
    public static PromoPose ParseExtraPose(string value)
    {
        const string form = "-extraPose name:lon:lat:distanceM:yaw, e.g. judea20:35.15:31.80:20000:0";
        var f = (value ?? "").Split(':');
        if (f.Length != 5) throw new System.ArgumentException($"'{value}': expected 5 fields ({form})");
        if (!System.Text.RegularExpressions.Regex.IsMatch(f[0], "^[A-Za-z0-9_-]{1,40}$"))
            throw new System.ArgumentException($"'{f[0]}': a pose name is letters, digits, '-' and '_' ({form})");
        if (PromoPose.All.Any(p => p.Name == f[0])) throw new System.ArgumentException($"'{f[0]}' is a promo pose's name: its capture would be overwritten");
        var inv = System.Globalization.CultureInfo.InvariantCulture; const System.Globalization.NumberStyles num = System.Globalization.NumberStyles.Float;
        if (!double.TryParse(f[1], num, inv, out var lon) || !double.TryParse(f[2], num, inv, out var lat)
            || !float.TryParse(f[3], num, inv, out var dist) || !float.TryParse(f[4], num, inv, out var yaw))
            throw new System.ArgumentException($"'{value}': lon, lat, distance and yaw must be numbers ({form})");
        if (lon < 34.2 || lon > 35.9 || lat < 29.45 || lat > 33.35) throw new System.ArgumentException($"'{value}': lon/lat outside the map (34.2-35.9 E, 29.45-33.35 N)");
        if (!(dist > 0f)) throw new System.ArgumentException($"'{value}': distance must be > 0 m");
        return new PromoPose(f[0], lon, lat, dist, yaw);
    }

    /// <summary>Art-track iteration renders (editor, WebGL tier = quality level 0): the promo poses plus a 30 km look at the
    /// promo 03 ground (Jaffa to Ramla), at the symbolic scale given by -kitScale (city blocks), -houseScale and -treeScale (default:
    /// the ruling's KitScale defaults), into -captureDir as kits-&lt;pose&gt;.png. Logs the kit numbers (KITS ...).</summary>
    public static void Kits()
    {
        var args = System.Environment.GetCommandLineArgs();
        string Arg(string name) { int i = System.Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var dir = Arg("-captureDir") ?? Path.Combine(CaptureDir, "visual", "kits");
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        if (Arg("-kitScale") is string ks) Statecraft.Game.Map.Kits.KitScale.CityBlocks = Statecraft.Game.Map.Kits.KitScale.Clamp(float.Parse(ks, ci));
        if (Arg("-houseScale") is string hs) Statecraft.Game.Map.Kits.KitScale.Houses = Statecraft.Game.Map.Kits.KitScale.Clamp(float.Parse(hs, ci));
        if (Arg("-treeScale") is string ts) Statecraft.Game.Map.Kits.KitScale.Trees = Statecraft.Game.Map.Kits.KitScale.Clamp(float.Parse(ts, ci));
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(0, true);
            var map = OpenAndBuild();
            Debug.Log(map.KitStats?.ToString() ?? "KITS none (no kit materials on MapScene)");
            var rig = Camera.main.GetComponent<CameraRig>();
            var poses = new System.Collections.Generic.List<PromoPose>(PromoPose.All)
            {
                new PromoPose("close30", 34.83, 32.00, 30000f, 10f), new PromoPose("hero12", 34.79, 32.06, 12000f, 10f),
                // The 1948 localities (promo-villages): Galilee villages, the Hebron hills, the Beisan valley and Bedouin camps
                // in the northern Negev.
                new PromoPose("galilee30", 35.25, 32.90, 30000f, 10f), new PromoPose("hebron30", 35.10, 31.55, 30000f, 10f),
                new PromoPose("beisan30", 35.50, 32.49, 30000f, 10f), new PromoPose("negev40", 34.80, 31.25, 40000f, 10f),
            };
            foreach (var pose in poses)
            {
                pose.Apply(rig, map.Frame);
                Save(Camera.main, 1920, 1080, Path.Combine(dir, $"kits-{pose.Name}.png"));
            }
        }
        finally
        {
            QualitySettings.SetQualityLevel(saved, true);
            Statecraft.Game.Map.Kits.KitScale.Reset();
        }
    }

    /// <summary>Close-ups of the unit aircraft (the Avia S-199 and the Spitfire) alone, top and side, at a size where the
    /// insignia read, into -captureDir (default captures/visual/aircraft) as &lt;aircraft&gt;-&lt;view&gt;.png: for checking the
    /// markings against the sourced photographs (docs/units.md).</summary>
    public static void Aircraft()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        var dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(CaptureDir, "visual", "aircraft");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.82f, 0.80f, 0.74f); cam.fieldOfView = 30f;
        var paint = new Material(Resources.Load<Material>(Statecraft.Game.Map.Units.UnitMaterials.MiniatureResource));
        paint.SetTexture("_BaseMap", Statecraft.Game.Map.Units.UnitPalette.MakeTexture());
        foreach (var (name, airframe, prop, hub) in new[]
        {
            ("avia-s199", Statecraft.Game.Map.Units.UnitModels.AviaS199("s199"), Statecraft.Game.Map.Units.UnitModels.S199Propeller("p"), Statecraft.Game.Map.Units.UnitModels.S199PropellerHub),
            ("spitfire", Statecraft.Game.Map.Units.UnitModels.Spitfire("spit"), Statecraft.Game.Map.Units.UnitModels.SpitfirePropeller("p"), Statecraft.Game.Map.Units.UnitModels.SpitfirePropellerHub),
        })
        {
            var plane = new GameObject(name);
            var body = new GameObject("Airframe"); body.transform.SetParent(plane.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = airframe; body.AddComponent<MeshRenderer>().sharedMaterial = paint;
            var p = new GameObject("Propeller"); p.transform.SetParent(plane.transform, false); p.transform.localPosition = hub;
            p.AddComponent<MeshFilter>().sharedMesh = prop; p.AddComponent<MeshRenderer>().sharedMaterial = paint;
            foreach (var (view, pos, look) in new[]
            {
                ("top", new Vector3(0f, 22f, -0.5f), new Vector3(0f, 0f, -0.5f)),
                ("bottom", new Vector3(0f, -22f, -0.5f), new Vector3(0f, 0f, -0.5f)),
                ("side-left", new Vector3(-20f, 2.5f, -1f), new Vector3(0f, 0f, -1f)),
                ("three-quarter", new Vector3(-13f, 9f, 13f), Vector3.zero),
            })
            {
                cam.transform.position = pos;
                cam.transform.LookAt(look, Mathf.Abs(pos.y) > 15f ? Vector3.forward : Vector3.up);
                Save(cam, 1600, 1000, Path.Combine(dir, $"{name}-{view}.png"));
            }
            Object.DestroyImmediate(plane);
        }
    }

    static void Save(Camera cam, int w, int h, string path)
    {
        var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
    }
}
