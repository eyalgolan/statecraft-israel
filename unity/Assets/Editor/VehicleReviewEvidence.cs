#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Game.VehicleReview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Matched evidence from the playable runtime review environment, not standalone asset-preview lighting.</summary>
    public static class VehicleReviewEvidence
    {
        [Serializable] sealed class Report
        {
            public string environment = VehicleReviewStage.ReviewNotice;
            public string provenance = "Dedicated playable vehicle review scene rendered by its runtime construction code in the Unity editor; not a historical map or player screenshot";
            public int width = 3840, height = 2160;
            public List<Frame> frames = new List<Frame>();
        }
        [Serializable] sealed class Frame
        {
            public string file = "", label = "";
            public Vector3 lookAt, cameraPosition, cameraEuler;
            public float fov, distance;
            public bool modelsVisible;
            public string[] activeModels = Array.Empty<string>();
        }

        [MenuItem("Statecraft/Vehicle review/Capture matched scene evidence")]
        public static void Capture()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-captureDir");
            string output = at >= 0 && at + 1 < args.Length ? Path.GetFullPath(args[at + 1])
                : Path.Combine(Paths.RepoRoot, "captures", "vehicle-review");
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene(VehicleReviewCatalog.ScenePath, OpenSceneMode.Single);
            var entry = Object.FindFirstObjectByType<VehicleReviewScene>() ?? throw new InvalidOperationException("The playable review scene entry is missing.");
            var stage = entry.Open(); var report = new Report();
            int previous = QualitySettings.GetQualityLevel(); int pc = Array.IndexOf(QualitySettings.names, "PC");
            if (pc < 0) throw new InvalidOperationException("PC quality tier missing.");
            try
            {
                QualitySettings.SetQualityLevel(pc, true);
                for (int shot = -2; shot < 3; shot++)
                {
                    string name;
                    if (shot < 0) { stage.Overview(); if (shot == -1) stage.Rig.Distance = 60000f; stage.Rig.Apply(); name = shot == -2 ? "overview40" : "overview60"; }
                    else { stage.Focus(shot); name = VehicleReviewCatalog.Ids[shot]; }
                    foreach (bool visible in new[] { false, true })
                    {
                        stage.ModelsVisible = visible; stage.Choose(stage.Camera);
                        MapShaderGlobals.PinWaterTime(100f);
                        string file = name + (visible ? "-after.png" : "-before.png");
                        Save(stage.Camera, Path.Combine(output, file));
                        report.frames.Add(new Frame { file = file, label = visible ? "Three review candidates placed" : "Same review slots before placement",
                            lookAt = stage.Rig.LookAt, cameraPosition = stage.Camera.transform.position, cameraEuler = stage.Camera.transform.eulerAngles,
                            fov = stage.Camera.fieldOfView, distance = stage.Rig.Distance, modelsVisible = visible,
                            activeModels = stage.Placed.Where(p => p.Model.activeSelf).Select(p => p.Id).ToArray() });
                    }
                }
                File.WriteAllText(Path.Combine(output, "evidence.json"), JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("VEHICLE REVIEW EVIDENCE: " + report.frames.Count + " matched runtime-scene frames -> " + output);
            }
            finally { Object.DestroyImmediate(stage.gameObject); MapShaderGlobals.PinWaterTime(null); QualitySettings.SetQualityLevel(previous, true); }
        }
        static void Save(Camera camera, string file)
        {
            var old = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(3840, 2160, 24); var image = new Texture2D(3840, 2160, TextureFormat.RGB24, false);
            try { camera.targetTexture = target; camera.Render(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 3840, 2160), 0, 0); image.Apply(); File.WriteAllBytes(file, image.EncodeToPNG()); }
            finally { camera.targetTexture = old; RenderTexture.active = active; Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
