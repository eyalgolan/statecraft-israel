#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Imports the Tripo prototypes from a staging folder: every &lt;name&gt;/fbx/ folder in it holding one .fbx
/// and, beside it, &lt;same name&gt;.fbm/ with one base-colour JPEG (or, when it has no JPEG, one PNG). The folder names are
/// the prefab names BuildingPreview and PreviewProps ask for; a name they do not list is imported all the same (for the
/// turntable, or a table row added later). Copies each to <see cref="Folder"/>/Models/&lt;name&gt;/&lt;name&gt;.fbx and
/// &lt;name&gt;_basecolor.jpg (.png); imports a static mesh alone (no cameras, lights, animation, rig, colliders or
/// materials; not readable; global scale 1) and a figure (<see cref="FigurePrefix"/>, or any skinned model; an export
/// animated without a skin needs the prefix) with its skeleton and clips as a Generic rig (Animate); the texture at most
/// <see cref="MaxTexture"/> px with mipmaps, sRGB; makes a matte URP Lit material like MapMaterials; and saves
/// Resources/SettlementPrototypes/&lt;name&gt;.prefab for BuildingPreview.LoadFromResources. Logs
/// "PROTOTYPE &lt;name&gt; tris=&lt;n&gt; bounds=&lt;x,y,z&gt; unitScale=&lt;s&gt; clips=&lt;n&gt;" each. Every folder is checked
/// before anything is copied: an incomplete one, or none at all, fails the run (exit 1 in batch mode). Idempotent and
/// additive: a re-run replaces the assets of the names it finds and keeps the others (an earlier batch's).
/// The models are Tripo Studio output (paid Max plan), CC0 in ASSETS.yaml; the staging folders with the masters stay
/// outside the repository.
/// Run headless (a relative source is taken from the repo root):
/// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod PrototypeImport.Run -prototypeSource <folder of name/fbx/ exports></summary>
public static class PrototypeImport
{
    public const string Folder = "Assets/Art/Settlements/Prototype";
    const string Models = Folder + "/Models", Prefabs = Folder + "/Resources/" + BuildingPreview.ResourcesFolder;
    public const int MaxTexture = 1024;

    /// <summary>Town clusters assembled from the 13 October building modules (Tal, 14 October 2026: the new high-res models
    /// in the game now; the final cluster assembly stays Blender work). Each is a few imported module prefabs side by side,
    /// saved as one prefab that BuildingPreview maps like any other. A part's x and z are in module widths (every module
    /// prefab is 1 across), its yaw in degrees before the map's own turn (the modules' fronts face +z, as the first batch's
    /// did, so DefaultYaw shows them), its scale relative to that width. Negative z ends up at the back, seen from the
    /// starting camera. A cluster whose modules are not all imported is skipped.</summary>
    public static readonly (string name, (string module, float x, float z, float yaw, float scale)[] parts)[] Clusters =
    {
        ("cluster-tel-aviv-v2", new[] { ("b01-white-city-v2", 0f, -0.5f, 0f, 1f), ("b02a-tel-aviv-corner-block-v2", -1.05f, 0.4f, 10f, 0.85f),
            ("b02b-tel-aviv-narrow-block-v2", 1f, 0.45f, -8f, 0.75f) }),
        ("cluster-jerusalem-v2", new[] { ("b03a-jerusalem-arches-v2", 0f, -0.5f, 0f, 1f), ("b03b-jerusalem-external-stair-v2", -1.05f, 0.4f, 12f, 0.85f),
            ("b03c-jerusalem-plain-v2", 1f, 0.45f, -10f, 0.85f) }),
        ("cluster-haifa-v2", new[] { ("b04a-haifa-stone-balconies-v2", -0.55f, -0.55f, 0f, 1f), ("b04b-haifa-loading-doors-v2", 0.55f, -0.55f, -6f, 0.9f),
            ("b32-port-cargo-shed-v2", -0.6f, 0.55f, 4f, 1f), ("b22-industrial-factory-v2", 0.6f, 0.55f, -4f, 0.9f) }),
        ("cluster-netanya-v2", new[] { ("b13a-netanya-two-storey-v2", -0.55f, -0.3f, 0f, 1f), ("b13a-netanya-two-storey-v2", 0.6f, 0.35f, 90f, 0.85f),
            ("b28-water-tower-v2", 0.75f, -0.6f, 0f, 0.45f) }),
        ("cluster-moshava-v2", new[] { ("b05a-moshava-house-v2", 0f, -0.45f, 0f, 1f), ("b24-agricultural-barn-v2", -1f, 0.4f, 10f, 0.8f),
            ("b28-water-tower-v2", 0.95f, 0.35f, 0f, 0.5f) }),
        ("cluster-moshava-winery-v2", new[] { ("b05a-moshava-house-v2", -0.55f, -0.45f, 0f, 0.9f), ("b23-winery-v2", 0.6f, -0.2f, -8f, 1f),
            ("b28-water-tower-v2", -0.7f, 0.6f, 0f, 0.45f) }),
        ("cluster-kibbutz-v2", new[] { ("b07-kibbutz-dining-hall-v2", 0f, -0.45f, 0f, 1f), ("b09a-kibbutz-communal-building-v2", -1f, 0.45f, 8f, 0.8f),
            ("b28-water-tower-v2", 0.95f, 0.4f, 0f, 0.5f) }),
        ("cluster-moshav-v2", new[] { ("b16-farmhouse-v2", -0.55f, -0.4f, 0f, 1f), ("b16-farmhouse-v2", 0.6f, 0.45f, 25f, 0.85f),
            ("b24-agricultural-barn-v2", 0.75f, -0.6f, -10f, 0.7f) }),
        ("cluster-stone-village-v2", new[] { ("b17-stone-courtyard-house-v2", 0f, -0.45f, 0f, 1f), ("b17-stone-courtyard-house-v2", -1f, 0.4f, 70f, 0.8f),
            ("b21-artisan-workshop-v2", 0.95f, 0.4f, -12f, 0.7f) }),
    };

    /// <summary>Names of rigged exports (Tripo Auto Rig, batch 2). Decided by name as well as by a skinned mesh in the import,
    /// since a re-run first imports under the previous run's settings, which could hide what the file holds. Takes alone do
    /// not count: a static export may carry an empty default take, and the first batch's static models would then become
    /// Generic rigs with Animators. So an export animated without a skin (a vehicle with moving parts) imports static, its
    /// clips dropped, unless its name has this prefix.</summary>
    public const string FigurePrefix = "figure-";

    /// <summary>A prefab name: lower case, digits and hyphens (BuildingPreview's tables, Resources paths, PNG names).</summary>
    static readonly Regex Slug = new Regex(@"^[a-z0-9][a-z0-9-]*\z", RegexOptions.CultureInvariant);

    /// <summary>Optional turn (Euler degrees) on a model before it is measured; a name without a row is imported as is.
    /// Identity to start: the FBX files (Tripo's Blender preset, read 10 October 2026) are Y-up in metres, each Z-up mesh
    /// stood up by its node's -90 about x, which the importer keeps. Add a row after viewing BuildingPreviewCapture's renders
    /// if a model lies on its side or faces away.</summary>
    static readonly Dictionary<string, Vector3> Correction = new Dictionary<string, Vector3>
    {
        ["kibbutz-dining-hall-v1"] = Vector3.zero,
        ["tel-aviv-white-city-v1"] = Vector3.zero,
        ["stone-village-v1"] = Vector3.zero,
    };

    public static void Run()
    {
        try
        {
            var source = Arg("-prototypeSource") ?? throw new ArgumentException("PrototypeImport needs -prototypeSource <dir> (the staging folder holding <name>/fbx/)");
            Import(Path.GetFullPath(Path.Combine(Paths.RepoRoot, source)));
        }
        catch (Exception e)
        {
            Debug.LogError("PROTOTYPE IMPORT FAILED " + e.Message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    public static void Import(string source)
    {
        // Check every folder first, so an incomplete one leaves the local folder as it was.
        var (found, missing) = Sources(source);
        foreach (var line in missing) Debug.LogError(line);
        if (missing.Count > 0) throw new FileNotFoundException($"{missing.Count} of {found.Count + missing.Count} prototypes incomplete under {source}");
        if (found.Count == 0) throw new FileNotFoundException($"no prototype under {source}: expected <name>/fbx/ folders");

        foreach (var (name, fbx, image) in found)
        {
            var dir = EnsureFolder(Models + "/" + name);
            string mesh = $"{dir}/{name}.fbx", tex = $"{dir}/{name}_basecolor{(Ext(image) == ".png" ? ".png" : ".jpg")}";
            File.Copy(image, tex, true);
            AssetDatabase.ImportAsset(tex, ForceUpdate);
            ConfigureTexture(tex);
            File.Copy(fbx, mesh, true);
            AssetDatabase.ImportAsset(mesh, ForceUpdate);
            bool rigged = name.StartsWith(FigurePrefix, StringComparison.Ordinal) || Skinned(mesh);
            float unitScale = ConfigureModel(mesh, rigged);
            var clips = rigged ? Clips(mesh) : new List<AnimationClip>();
            var controller = clips.Count > 0 ? MakeController($"{dir}/{name}.controller", clips) : null;
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(tex) ?? throw new FileNotFoundException("texture did not import", tex);
            var (tris, size) = SavePrefab(name, mesh, MakeMaterial($"{dir}/{name}.mat", baseMap), controller);
            Debug.Log(FormattableString.Invariant($"PROTOTYPE {name} tris={tris} bounds={size.x:0.###},{size.y:0.###},{size.z:0.###} unitScale={unitScale} clips={clips.Count}"));
        }
        AssetDatabase.SaveAssets();
        BuildClusters();
        BuildUnitFigures();
    }

    /// <summary>The 13 October characters as the army tokens' riflemen (Tal, 14 October 2026: our characters in place of the
    /// game's), through UnitShowcase's authored-model hook: a prefab at Resources/Units/Authored/&lt;type&gt; replaces the
    /// code-built rifleman of that type. Matched by headgear: the bush hat to the sabra pioneer, the steel helmet to the
    /// young recruit, the wool cap to the veteran fighter. The prefabs live in this folder's own Resources; without them the
    /// token draws the code-built riflemen.</summary>
    public static readonly (string unitType, string prototype)[] UnitFigures =
    {
        (UnitShowcase.RiflemanSlouchHatType, "figure-c04-sabra-pioneer-v3"),
        (UnitShowcase.RiflemanHelmetType, "figure-c06-young-recruit-v2"),
        (UnitShowcase.RiflemanWoolCapType, "figure-c07-veteran-fighter-v2"),
    };

    /// <summary>Rebuilds the clusters and the unit figures from the module and character prefabs already imported, without
    /// importing anything (Unity -batchmode ... -executeMethod PrototypeImport.Rebuild).</summary>
    public static void Rebuild() { BuildClusters(); BuildUnitFigures(); }

    /// <summary>Saves each of <see cref="UnitFigures"/> whose character prefab is imported as
    /// Resources/Units/Authored/&lt;type&gt;.prefab, in the hook's convention: real metres (the character's normalised prefab
    /// scaled on a child to UnitBadge.FigureHeightM tall, since the token sets the root's own scale), facing +Z, feet at
    /// the origin, its own material, and its Animator playing the idle. Logs "PROTOTYPE UNIT &lt;type&gt; from &lt;name&gt;".</summary>
    public static void BuildUnitFigures()
    {
        var folder = EnsureFolder(Folder + "/Resources/" + UnitShowcase.AuthoredFolder.TrimEnd('/'));
        foreach (var (type, prototype) in UnitFigures)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{prototype}.prefab");
            if (source == null) { Debug.Log($"PROTOTYPE UNIT {type} skipped: {prototype} is not imported"); continue; }
            var root = new GameObject(type);
            try
            {
                var fit = new GameObject("fit").transform;
                fit.SetParent(root.transform, false);
                var figure = Object.Instantiate(source, fit, false);
                figure.name = prototype;
                float height = Measure(root, type).size.y;
                if (height <= 0f) throw new InvalidOperationException($"{prototype}: no height");
                fit.localScale = Vector3.one * (UnitBadge.FigureHeightM / height);
                PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{type}.prefab", out bool saved);
                if (!saved) throw new InvalidOperationException($"{type}: the prefab could not be saved");
                Debug.Log($"PROTOTYPE UNIT {type} from {prototype}");
            }
            finally { Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>Saves each of <see cref="Clusters"/> whose module prefabs are all imported (this run's or an earlier one's)
    /// as Resources/SettlementPrototypes/&lt;name&gt;.prefab: copies of the modules placed as the table says under one pivot,
    /// normalised like a single model (largest horizontal extent 1, bottom at y = 0, footprint centred). Logs
    /// "PROTOTYPE CLUSTER &lt;name&gt; parts=&lt;n&gt; bounds=&lt;x,y,z&gt;", or the modules it is missing.</summary>
    public static void BuildClusters()
    {
        foreach (var (name, parts) in Clusters)
        {
            var modules = parts.Select(p => AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{p.module}.prefab")).ToArray();
            var absent = parts.Where((p, i) => modules[i] == null).Select(p => p.module).Distinct().ToList();
            if (absent.Count > 0) { Debug.Log($"PROTOTYPE CLUSTER {name} skipped: missing {string.Join(", ", absent)}"); continue; }
            var root = new GameObject(name);
            try
            {
                var pivot = new GameObject("pivot").transform;
                pivot.SetParent(root.transform, false);
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = Object.Instantiate(modules[i], pivot, false);
                    part.name = $"{i}-{parts[i].module}";
                    part.transform.localPosition = new Vector3(parts[i].x, 0f, parts[i].z);
                    part.transform.localRotation = Quaternion.Euler(0f, parts[i].yaw, 0f);
                    part.transform.localScale = Vector3.one * parts[i].scale;
                }
                var size = Normalise(root, pivot, name).size;
                PrefabUtility.SaveAsPrefabAsset(root, $"{EnsureFolder(Prefabs)}/{name}.prefab", out bool saved);
                if (!saved) throw new InvalidOperationException($"{name}: the prefab could not be saved");
                Debug.Log(FormattableString.Invariant($"PROTOTYPE CLUSTER {name} parts={parts.Length} bounds={size.x:0.###},{size.y:0.###},{size.z:0.###}"));
            }
            finally { Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
    }

    const ImportAssetOptions ForceUpdate = ImportAssetOptions.ForceUpdate;

    /// <summary>Every &lt;name&gt;/fbx/ folder under <paramref name="source"/>, in ordinal order of name: its one .fbx and the
    /// one JPEG (any case) in its .fbm folder, or when that holds no JPEG its one PNG; and a "PROTOTYPE &lt;name&gt; MISSING"
    /// line for each folder without exactly one of either, or whose name is not a slug. A folder without fbx/ (the
    /// original exports, concepts, references) is not a prototype. Reads only; public for the tests.</summary>
    public static (List<(string name, string fbx, string image)> found, List<string> missing) Sources(string source)
    {
        var found = new List<(string name, string fbx, string image)>();
        var missing = new List<string>();
        var names = (Directory.Exists(source) ? Directory.GetDirectories(source) : Array.Empty<string>())
            .Select(d => Path.GetFileName(d) ?? "").Where(n => n.Length > 0 && Directory.Exists(Path.Combine(source, n, "fbx")))
            .OrderBy(n => n, StringComparer.Ordinal);
        foreach (var name in names)
        {
            var dir = Path.Combine(source, name, "fbx");
            if (!Slug.IsMatch(name)) { missing.Add($"PROTOTYPE {name} MISSING: the folder name is not a slug (a-z, 0-9 and '-'), in {dir}"); continue; }
            var fbx = Files(dir).Where(f => Ext(f) == ".fbx").ToList();
            if (fbx.Count != 1) { missing.Add($"PROTOTYPE {name} MISSING: {fbx.Count} .fbx files in {dir}, expected one"); continue; }
            var fbm = Path.ChangeExtension(fbx[0], ".fbm");
            var jpegs = Files(fbm).Where(f => Ext(f) == ".jpg" || Ext(f) == ".jpeg").ToList();
            var pngs = Files(fbm).Where(f => Ext(f) == ".png").ToList();
            var image = jpegs.Count > 0 ? jpegs : pngs;
            if (image.Count != 1) { missing.Add($"PROTOTYPE {name} MISSING: {jpegs.Count} base-colour JPEGs ({pngs.Count} PNGs) in {fbm}, expected one"); continue; }
            found.Add((name, fbx[0], image[0]));
        }
        return (found, missing);
    }

    static void ConfigureTexture(string path)
    {
        var t = (TextureImporter)AssetImporter.GetAtPath(path);
        t.textureType = TextureImporterType.Default; t.maxTextureSize = MaxTexture; t.mipmapEnabled = true; t.sRGBTexture = true;
        t.SaveAndReimport();
    }

    /// <summary>Whether the model as imported holds a skinned mesh (a rig).</summary>
    static bool Skinned(string path)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return model != null && model.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
    }

    /// <summary>At scale 1; returns the file's own unit scale (the bounds normalise the size either way). A static model is
    /// the mesh alone. A rigged one keeps its skeleton as a Generic rig with an avatar made from itself: Generic is the safe
    /// default, as the clips only ever play on their own skeleton (Humanoid retargeting is not needed, and its avatar
    /// fails on an incomplete rig). Its clips are the file's own takes; those named like a cycle (Loops), or a lone one
    /// (an idle or walk preset), loop.</summary>
    static float ConfigureModel(string path, bool rigged)
    {
        var m = (ModelImporter)AssetImporter.GetAtPath(path);
        m.importCameras = false; m.importLights = false;
        m.addCollider = false; m.materialImportMode = ModelImporterMaterialImportMode.None;
        m.isReadable = false; m.globalScale = 1f;
        if (!rigged)
        {
            m.importAnimation = false; m.animationType = ModelImporterAnimationType.None;
            m.SaveAndReimport();
            return m.fileScale;
        }
        m.animationType = ModelImporterAnimationType.Generic;
        m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        m.importAnimation = true;
        m.clipAnimations = Array.Empty<ModelImporterClipAnimation>();   // back to the file's own takes before reading them
        m.SaveAndReimport();
        m = (ModelImporter)AssetImporter.GetAtPath(path);
        var takes = m.defaultClipAnimations;
        foreach (var take in takes) take.loopTime = takes.Length == 1 || Loops(take.name);
        m.clipAnimations = takes;
        m.SaveAndReimport();
        return m.fileScale;
    }

    static bool Loops(string clip) => new[] { "idle", "walk", "run", "loop" }.Any(w => clip.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);

    static bool IsIdle(AnimationClip clip) => clip.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>The model's imported clips (not the importer's "__preview__" copies), in ordinal order of name.</summary>
    static List<AnimationClip> Clips(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
        .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).OrderBy(c => c.name, StringComparer.Ordinal).ToList();

    /// <summary>A fresh AnimatorController with one state per clip, the first idle-like clip (else the first clip) the default,
    /// so a placed figure idles with no script. Replaces the previous run's controller.</summary>
    static AnimatorController MakeController(string path, List<AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var preferred = clips.FirstOrDefault(IsIdle) ?? clips[0];
        AnimatorState? start = null;
        foreach (var clip in clips)
        {
            // A state name may not hold '.', which Tripo's takes do ("idle.001"): Unity warns and renames it.
            var state = controller.layers[0].stateMachine.AddState(clip.name.Replace('.', '_'));
            state.motion = clip;
            if (clip == preferred) start = state;
        }
        if (start != null) controller.layers[0].stateMachine.defaultState = start;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    /// <summary>Matte URP Lit with the base-colour map, like MapMaterials' marker (Shader.Find is editor-only here; players
    /// get the saved asset). No property block will tint it.</summary>
    static Material MakeMaterial(string path, Texture2D baseMap)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit"); // editor only: fine here
        if (shader == null) throw new InvalidOperationException("shader not found: Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        else mat.shader = shader;
        mat.enableInstancing = true;
        mat.SetTexture("_BaseMap", baseMap); mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0f); mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>An empty root at the origin over a "pivot" that holds the correction turn and the normalising scale and
    /// offset, over the model exactly as imported (its node's own turn stands the mesh up). Normalised from the measured
    /// renderer bounds: largest horizontal extent 1, bottom at y = 0, footprint centred on the root. A rigged model's own
    /// root ("model") keeps the importer's Animator, given <paramref name="controller"/> and no root motion (Generic clips
    /// bind by paths from it); with no controller any Animator is removed, so it holds its bind pose. Returns the triangle
    /// count (skinned meshes too) and the normalised size.</summary>
    static (long tris, Vector3 size) SavePrefab(string name, string meshPath, Material mat, AnimatorController? controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath) ?? throw new FileNotFoundException("model did not import", meshPath);
        var root = new GameObject(name);
        try
        {
            var pivot = new GameObject("pivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localRotation = Quaternion.Euler(Correction.TryGetValue(name, out var turn) ? turn : Vector3.zero);
            var instance = Object.Instantiate(model, pivot, false);
            instance.name = "model";
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(mat, Math.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            }
            if (controller != null)
            {
                if (!instance.TryGetComponent(out Animator animator)) animator = instance.AddComponent<Animator>();
                if (animator.avatar == null) animator.avatar = AssetDatabase.LoadAllAssetsAtPath(meshPath).OfType<Avatar>().FirstOrDefault();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                PoseAndFitSkins(instance, controller);
            }
            else foreach (var a in instance.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            var meshes = instance.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
                .Concat(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => s.sharedMesh));
            long tris = 0;
            foreach (var mesh in meshes)
                for (int i = 0; mesh != null && i < mesh.subMeshCount; i++)
                    if (mesh.GetTopology(i) == MeshTopology.Triangles) tris += mesh.GetIndexCount(i) / 3;
            var b = Normalise(root, pivot, name);
            PrefabUtility.SaveAsPrefabAsset(root, $"{EnsureFolder(Prefabs)}/{name}.prefab", out bool saved);
            if (!saved) throw new InvalidOperationException($"{name}: the prefab could not be saved");
            return (tris, b.size);
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>Share of each axis added to a skinned mesh's fitted bounds, for the motion of its idle and walk clips.</summary>
    const float SkinPad = 0.1f;

    /// <summary>Poses a rigged model at the first frame of its controller's default (idle) clip, the pose an edit-mode
    /// prop shows, and fits each skinned mesh's localBounds to its vertices as posed, padded by <see cref="SkinPad"/>.
    /// The importer's own bounds are not the mesh's: batch 2's farmer and woman measured up to six times too deep, so
    /// the normalising scale, the map's fit and culling all read a wrong size. The fitted bounds are saved in the prefab.</summary>
    static void PoseAndFitSkins(GameObject instance, AnimatorController controller)
    {
        if (controller.layers[0].stateMachine.defaultState?.motion is AnimationClip idle) idle.SampleAnimation(instance, 0f);
        var baked = new Mesh();
        try
        {
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.BakeMesh(baked, false);   // without scale: in the renderer's own local space
                if (baked.vertexCount == 0) continue;
                var space = skin.rootBone != null ? skin.rootBone : skin.transform;   // localBounds is held in the root bone's frame
                var toSpace = space.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                var vertices = baked.vertices;
                var fit = new Bounds(toSpace.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                foreach (var v in vertices) fit.Encapsulate(toSpace.MultiplyPoint3x4(v));
                fit.Expand(fit.size * SkinPad);
                skin.localBounds = fit;
                skin.updateWhenOffscreen = false;
            }
        }
        finally { Object.DestroyImmediate(baked); }
    }

    /// <summary>Scales <paramref name="pivot"/> (at scale 1) so the root's largest horizontal extent is 1, then moves it so the
    /// bottom is at y = 0 and the footprint centred on the root; returns the normalised bounds.</summary>
    static Bounds Normalise(GameObject root, Transform pivot, string name)
    {
        var b = Measure(root, name);
        float across = Mathf.Max(b.size.x, b.size.z);
        if (across <= 0f) throw new InvalidOperationException($"{name}: the model has no horizontal extent");
        pivot.localScale = Vector3.one / across;
        b = Measure(root, name);   // re-measured after the scale, then moved onto the origin
        pivot.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
        return Measure(root, name);
    }

    static Bounds Measure(GameObject go, string name)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) throw new InvalidOperationException($"{name}: the model has no renderer");
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>Creates the asset folder and its parents through the AssetDatabase, which writes their .meta files.</summary>
    static string EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var at = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = at + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(at, parts[i]);
            at = next;
        }
        return path;
    }

    // Extensions compared without case (the kibbutz texture is .JPEG) and without the file system's pattern rules.
    static string Ext(string path) => Path.GetExtension(path).ToLowerInvariant();
    static string[] Files(string dir) => Directory.Exists(dir) ? Directory.GetFiles(dir) : Array.Empty<string>();

    static string? Arg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
