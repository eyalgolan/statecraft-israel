using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// TEMPORARY probe for #665 (not committed): measures Tal's soldiers and renders them on the tokens.
public class Probe665
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName));
    static readonly string[] Soldiers = { "figure-c04-sabra-pioneer-v3", "figure-c05-pioneer-veteran-v2", "figure-c06-young-recruit-v2",
        "figure-c07-veteran-fighter-v2", "figure-c12f-palmach-scout-v2", "figure-c08-arab-legion-v2", "figure-c09-irregular-v2" };

    [Test]
    public void Probe()
    {
        var outDir = Environment.GetEnvironmentVariable("PROBE665_OUT") ?? Path.GetTempPath();
        Directory.CreateDirectory(outDir);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        foreach (var id in Soldiers)
            foreach (var d in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, d));
                var go = UnityEngine.Object.Instantiate(prefab);
                var rs = go.GetComponentsInChildren<Renderer>(true);
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                int tris = go.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh.triangles.Length / 3)
                    + go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(f => f.sharedMesh.triangles.Length / 3);
                var mats = string.Join(",", rs.SelectMany(r => r.sharedMaterials).Select(m => m.name + "/" + m.shader.name + "/inst=" + m.enableInstancing + "/col=" + m.GetColor("_BaseColor")));
                Debug.Log($"PROBE model {id} {d}: bounds centre {b.center} size {b.size} min {b.min} max {b.max}; renderers {string.Join(",", rs.Select(r => r.GetType().Name))}; tris {tris}; mats {mats}; animator {go.GetComponentInChildren<Animator>() != null}");
                UnityEngine.Object.DestroyImmediate(go);
            }

        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.ShowcaseClearing = true;
        map.Build();
        var rt = new RenderTexture(1920, 1080, 24); var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        try
        {
            var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
            var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
            s.Date = new DateTime(1948, 5, 29); s.Rung = 5; s.Mobilization = 0.8;
            layer.Show(C, s);
            var showcase = UnitShowcase.Spawn(map, still: true).transform;
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            var harel = tokens.Find("harel");
            var figures = tokens.Cast<Transform>().SelectMany(t => t.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")))
                .Concat(showcase.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman"))).ToList();
            Debug.Log($"PROBE figures {figures.Count}");

            var looks = new List<(PromoPose pose, Transform? at)>
            {
                (PromoPose.Gameplay03, null),
            };
            void Shoot(string tag)
            {
                foreach (var (name, target, dist) in new (string, Transform?, float)[] { ("gameplay03", null, 0f), ("harel30", harel, 30000f), ("plain30", showcase.Find("Rifleman1948-1") ?? showcase, 30000f), ("plain12", showcase, 12000f), ("harel12", harel, 12000f) })
                {
                    PromoPose pose;
                    if (target == null) pose = PromoPose.Gameplay03;
                    else { var c = target.name == "UnitShowcase" ? UnitShowcase.TrioSpot(map) : target.Find(UnitPlinth.PlinthName) != null ? target.Find(UnitPlinth.PlinthName).position : target.position; var (lon, lat) = map.Frame.ToLonLat(c); pose = new PromoPose(name, lon, lat, dist, 0f); }
                    pose.Apply(rig, map.Frame);
                    UnitFlagFacing.FaceAll(cam.transform.position);
                    layer.FaceCamera(cam);
                    cam.targetTexture = rt; cam.aspect = 1920f / 1080f;
                    cam.Render(); RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outDir, $"{tag}-{name}.png"), tex.EncodeToPNG());
                    // where the army tokens are on screen
                    foreach (var t in tokens.Cast<Transform>().Where(t => t.Find(UnitPlinth.PlinthName) != null).Append(showcase))
                    {
                        var p = t.name == "UnitShowcase" ? UnitShowcase.TrioSpot(map) : t.Find(UnitPlinth.PlinthName).position;
                        var v = cam.WorldToViewportPoint(p);
                        var top = cam.WorldToViewportPoint(p + Vector3.up * 1.9f * UnitShowcase.FigureScale);
                        if (v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1)
                            Debug.Log($"PROBE screen {tag}-{name} {t.name} px {v.x * 1920:0} {(1 - v.y) * 1080:0} figpx {(top.y - v.y) * 1080:0.0}");
                    }
                }
            }
            Shoot("proc");
            var swapped = new List<GameObject>();
            foreach (var mode in new[] { "far", "near" })
            {
                foreach (var g in swapped) UnityEngine.Object.DestroyImmediate(g);
                swapped.Clear();
                foreach (var f in figures)
                {
                    var mf = f.GetComponent<MeshFilter>(); var mr = f.GetComponent<MeshRenderer>();
                    string type = f.parent == showcase ? new[] { UnitShowcase.RiflemanSlouchHatType, UnitShowcase.RiflemanHelmetType, UnitShowcase.RiflemanWoolCapType }[int.Parse(f.name.Substring(f.name.Length - 1))] : mf.sharedMesh.name;
                    string id = type == UnitShowcase.RiflemanSlouchHatType ? Soldiers[0] : type == UnitShowcase.RiflemanHelmetType ? Soldiers[2] : Soldiers[3];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, mode == "far" ? CollectionDetail.Far : CollectionDetail.Near));
                    var g = (GameObject)UnityEngine.Object.Instantiate(prefab, f, false);
                    g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity; g.transform.localScale = Vector3.one;
                    mr.enabled = false;
                    swapped.Add(g);
                }
                Shoot("tal" + mode);
                // also a variant with all five Israeli models in a row on the showcase (c04 c05 c06 c07 c12f) for a look
            }
            foreach (var g in swapped) UnityEngine.Object.DestroyImmediate(g);
        }
        finally { cam.targetTexture = null; cam.ResetAspect(); RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Each soldier alone, front and side, at a neutral pose, for the mapping.</summary>
    [Test]
    public void ProbeLineup()
    {
        var outDir = Environment.GetEnvironmentVariable("PROBE665_OUT") ?? Path.GetTempPath();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        var rt = new RenderTexture(1600, 600, 24); var tex = new Texture2D(1600, 600, TextureFormat.RGB24, false);
        var made = new List<GameObject>();
        try
        {
            for (int i = 0; i < Soldiers.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(Soldiers[i], CollectionDetail.Near));
                var g = UnityEngine.Object.Instantiate(prefab);
                g.transform.position = new Vector3((i - 3) * 0.9f, 0, 0);
                made.Add(g);
            }
            foreach (var (name, pos, look) in new[] { ("front", new Vector3(0, 1.0f, 7.5f), new Vector3(0, 0.9f, 0)), ("side", new Vector3(7.5f, 1.0f, 0.5f), new Vector3(0, 0.9f, 0)), ("back", new Vector3(0, 1.0f, -7.5f), new Vector3(0, 0.9f, 0)) })
            {
                cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = 25; cam.nearClipPlane = 0.05f;
                cam.targetTexture = rt; cam.aspect = 1600f / 600f; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1600, 600), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, $"lineup-{name}.png"), tex.EncodeToPNG());
            }
        }
        finally { foreach (var g in made) UnityEngine.Object.DestroyImmediate(g); cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); }
    }

    /// <summary>Why the far meshes did not draw on the tokens: diagnostics, and the far lineup alone at real size.</summary>
    [Test]
    public void ProbeFar()
    {
        var outDir = Environment.GetEnvironmentVariable("PROBE665_OUT") ?? Path.GetTempPath();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        var rt = new RenderTexture(1600, 600, 24); var tex = new Texture2D(1600, 600, TextureFormat.RGB24, false);
        var made = new List<GameObject>();
        try
        {
            for (int i = 0; i < Soldiers.Length; i++)
                foreach (var d in new[] { CollectionDetail.Far, CollectionDetail.Near })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(Soldiers[i], d));
                    var g = UnityEngine.Object.Instantiate(prefab);
                    g.transform.position = new Vector3((i - 3) * 0.9f, 0, d == CollectionDetail.Far ? 0f : 3f);
                    made.Add(g);
                    foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                    {
                        var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                        Debug.Log($"PROBEFAR {Soldiers[i]} {d} {r.name}: enabled {r.enabled} active {r.gameObject.activeInHierarchy} layer {r.gameObject.layer} rlm {r.renderingLayerMask} forceOff {r.forceRenderingOff} shadow {r.shadowCastingMode} " +
                            $"mesh {(mesh == null ? "NULL" : mesh.name + " v" + mesh.vertexCount + " sub" + mesh.subMeshCount + " idx" + mesh.GetIndexCount(0) + " topo" + mesh.GetTopology(0) + " mb " + mesh.bounds + " attrs " + string.Join("/", mesh.GetVertexAttributes().Select(a => a.attribute + ":" + a.format + "x" + a.dimension)))} " +
                            $"rb {r.bounds} lossy {r.transform.lossyScale} det {r.transform.localToWorldMatrix.determinant:0.000} mats {string.Join(",", r.sharedMaterials.Select(m => m == null ? "NULL" : m.name + ":" + m.shader.name + ":" + m.shader.isSupported + ":pass" + m.passCount + ":" + string.Join(" ", m.shaderKeywords)))}");
                    }
                }
            foreach (var (name, pos, look) in new[] { ("front", new Vector3(0, 1.0f, 9f), new Vector3(0, 0.6f, 1.5f)), ("side", new Vector3(9f, 1.0f, 1.5f), new Vector3(0, 0.6f, 1.5f)), ("top", new Vector3(0, 9f, 1.5f), new Vector3(0, 0f, 1.5f)) })
            {
                cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = 30; cam.nearClipPlane = 0.05f;
                cam.targetTexture = rt; cam.aspect = 1600f / 600f; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1600, 600), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, $"far-lineup-{name}.png"), tex.EncodeToPNG());
            }
            foreach (var g in made) foreach (var r in g.GetComponentsInChildren<Renderer>(true)) Debug.Log($"PROBEFAR visible {g.name} {r.name} {r.isVisible}");
        }
        finally { foreach (var g in made) UnityEngine.Object.DestroyImmediate(g); cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); }
    }

    /// <summary>The real UnitFigures path on the map: code-built, then dressed, at gameplay03, harel 30/12 km and the trio 12 km.</summary>
    [Test]
    public void ProbeDressed()
    {
        var outDir = Environment.GetEnvironmentVariable("PROBE665_OUT") ?? Path.GetTempPath();
        Directory.CreateDirectory(outDir);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
        map.ShowcaseClearing = true;
        map.Build();
        var rt = new RenderTexture(1920, 1080, 24); var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        s.Date = new DateTime(1948, 5, 29); s.Rung = 5; s.Mobilization = 0.8;
        try
        {
            foreach (var tag in new[] { "proc", "dressed" })
            {
                UnitFigures.Forget();
                UnitFigures.Source = tag == "proc" ? null : (id, d, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, d)));
                var layer = UnitLayer.Create(map, UnitData.Parse(CommittedYaml), C.Hexes, still: true);
                layer.Show(C, s);
                var showcase = UnitShowcase.Spawn(map, still: true).transform;
                var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
                var harel = tokens.Find("harel");
                foreach (var f in harel.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")))
                {
                    var m = f.Find(UnitFigures.ModelName);
                    var code = f.GetComponent<MeshRenderer>();
                    Debug.Log($"PROBED {tag} {f.name}: code enabled {code.enabled} code bounds {code.bounds.size} lossy {f.lossyScale.y}; model {(m == null ? "none" : string.Join(" | ", m.GetComponentsInChildren<Renderer>().Select(r => r.name + " en " + r.enabled + " b " + r.bounds.size + " lossy " + r.transform.lossyScale + " vis " + r.isVisible)))}");
                }
                foreach (var (name, target, dist) in new (string, Transform?, float)[] { ("gameplay03", null, 0f), ("harel30", harel, 30000f), ("harel12", harel, 12000f), ("plain12", showcase, 12000f), ("plain30", showcase, 30000f) })
                {
                    PromoPose pose;
                    if (target == null) pose = PromoPose.Gameplay03;
                    else { var c = target == showcase ? UnitShowcase.TrioSpot(map) : target.Find(UnitPlinth.PlinthName).position; var (lon, lat) = map.Frame.ToLonLat(c); pose = new PromoPose(name, lon, lat, dist, 0f); }
                    pose.Apply(rig, map.Frame);
                    UnitFlagFacing.FaceAll(cam.transform.position);
                    layer.FaceCamera(cam);
                    cam.targetTexture = rt; cam.aspect = 1920f / 1080f;
                    cam.Render(); RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outDir, $"{tag}-{name}.png"), tex.EncodeToPNG());
                }
                foreach (var f in harel.Cast<Transform>().Where(x => x.name.StartsWith("Rifleman")))
                {
                    var m = f.Find(UnitFigures.ModelName);
                    if (m != null) Debug.Log($"PROBED after {tag} {f.name}: " + string.Join(" | ", m.GetComponentsInChildren<Renderer>().Select(r => r.name + " vis " + r.isVisible + " b " + r.bounds.size)));
                }
            }
        }
        finally { UnitFigures.Source = null; UnitFigures.Forget(); cam.targetTexture = null; cam.ResetAspect(); RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }
}
