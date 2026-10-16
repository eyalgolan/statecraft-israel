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
