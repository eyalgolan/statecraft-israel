#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.VehicleReview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Explicit evidence route when a player build is blocked: starts the actual playable scene in Editor Play Mode.</summary>
public class VehicleReviewPlayModeEvidenceTests
{
    [Serializable] sealed class Evidence
    {
        public string provenance = "Actual Unity Editor Play Mode review-scene camera renders. Runtime scene Start placed the models. World labels included; screen-space HUD excluded. Not a native-player or historical map screenshot.";
        public List<Frame> frames = new List<Frame>();
    }
    [Serializable] sealed class Frame
    {
        public string file = "", quality = "";
        public bool modelsVisible;
        public Vector3 lookAt, cameraPosition, cameraEuler;
        public float distance, fov;
        public int width = 3840, height = 2160;
    }

    [UnityTest, Explicit("Produces actual Play Mode visual evidence; run this method explicitly after scoped import")]
    public IEnumerator CaptureRunningReviewScene()
    {
        EditorSceneManager.OpenScene("Assets/VehicleReview.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        VehicleReviewStage? stage = null;
        for (int i = 0; i < 60 && stage == null; i++) { yield return null; stage = Object.FindFirstObjectByType<VehicleReviewStage>(); }
        Assert.IsNotNull(stage, "VehicleReviewScene.Start must place the review environment during real Play Mode");
        Assert.AreEqual(3, stage!.Placed.Count);
        var dir = Path.Combine(Paths.RepoRoot, "captures", "vehicle-review-playmode"); Directory.CreateDirectory(dir);
        var evidence = new Evidence();
        int previous = QualitySettings.GetQualityLevel();
        int pc = Array.IndexOf(QualitySettings.names, "PC"); Assert.GreaterOrEqual(pc, 0);
        try
        {
            QualitySettings.SetQualityLevel(pc, true);
            for (int shot = -2; shot < stage.Placed.Count; shot++)
            {
                string name;
                if (shot < 0) { stage.Overview(); if (shot == -1) stage.Rig.Distance = 60000f; stage.Rig.Apply(); name = shot == -2 ? "overview40" : "strategic60"; }
                else { stage.Focus(shot); name = stage.Placed[shot].Id; }
                foreach (bool visible in new[] { false, true })
                {
                    stage.ModelsVisible = visible; stage.Choose(stage.Camera);
                    string file = name + (visible ? "-after.png" : "-before.png");
                    Capture(stage.Camera, Path.Combine(dir, file));
                    evidence.frames.Add(new Frame { file = file, quality = QualitySettings.names[pc], modelsVisible = visible,
                        distance = stage.Rig.Distance, fov = stage.Camera.fieldOfView, lookAt = stage.Rig.LookAt,
                        cameraPosition = stage.Camera.transform.position, cameraEuler = stage.Camera.transform.eulerAngles });
                }
            }
            File.WriteAllText(Path.Combine(dir, "playmode-evidence.json"), JsonUtility.ToJson(evidence, true) + "\n");
            Assert.AreEqual(10, evidence.frames.Count);
            Debug.Log("VEHICLE REVIEW PLAY MODE EVIDENCE: 10 matched scene frames -> " + dir);
        }
        finally { QualitySettings.SetQualityLevel(previous, true); }
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator RestoreScene()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void Capture(Camera camera, string file)
    {
        var old = camera.targetTexture; var active = RenderTexture.active;
        var target = new RenderTexture(3840, 2160, 24); var image = new Texture2D(3840, 2160, TextureFormat.RGB24, false);
        try { camera.targetTexture = target; camera.Render(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 3840, 2160), 0, 0); image.Apply(); File.WriteAllBytes(file, image.EncodeToPNG()); }
        finally { camera.targetTexture = old; RenderTexture.active = active; Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
    }
}
