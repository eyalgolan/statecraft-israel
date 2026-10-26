using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Render the saved playable review scene; these are isolated stage frames, not campaign/player captures.</summary>
    public static class Generated1948CharacterReviewEvidence
    {
        [Serializable] sealed class Frame
        {
            public string file, character, cameraMode, motion;
            public bool rear;
            public float sampleSeconds, pitch, fov;
            public bool orthographic;
            public Vector3 cameraPosition, cameraEuler;
        }
        [Serializable] sealed class Record
        {
            public string kind = "Actual saved Unity character-review scene camera renders; isolated stage, no campaign positions, no player/WebGL claim";
            public string scene = Generated1948CharacterReviewMenu.ScenePath;
            public Frame[] frames;
        }
        /// <summary>One fixed ALA pose: distinguish mesh placement from detached shadow bias without changing assets.</summary>
        public static void ProbeGround() => CaptureGround(false);
        public static void ProofGround() => CaptureGround(true);
        static void CaptureGround(bool proofOnly)
        {
            string folder = Path.GetFullPath(proofOnly ? "../captures/character-review-ground-proof" : "../captures/character-review-ground-probe");
            Directory.CreateDirectory(folder);
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(Generated1948CharacterReviewMenu.ScenePath);
                var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948CharacterReview>()).Single();
                review.BuildForReview();
                review.Focus(2);
                review.SetGameCamera(false);
                review.SetMotion(true);
                review.SampleMotion(.6f);
                var sun = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>()).Single();
                Render(review.ReviewCamera, Path.Combine(folder, proofOnly ? "ala-grounded-proof.png" : "ala-current-shadow.png"));
                if (!proofOnly)
                {
                    sun.shadows = LightShadows.None;
                    Render(review.ReviewCamera, Path.Combine(folder, "ala-no-shadow.png"));
                    sun.shadows = LightShadows.Hard;
                    sun.GetUniversalAdditionalLightData().usePipelineSettings = false;
                    sun.shadowBias = sun.shadowNormalBias = 0f;
                    review.ReviewCamera.farClipPlane = 60f;
                    Render(review.ReviewCamera, Path.Combine(folder, "ala-zero-bias-shadow.png"));
                }
                var skin = review.Instances[2].GetComponentInChildren<SkinnedMeshRenderer>(true);
                var mesh = new Mesh();
                try
                {
                    skin.BakeMesh(mesh, false);
                    var vertices = mesh.vertices;
                    float plain = vertices.Min(v => skin.transform.TransformPoint(v).y);
                    float indexed = mesh.triangles.Distinct().Min(i => skin.transform.TransformPoint(vertices[i]).y);
                    skin.BakeMesh(mesh, true);
                    string report = (proofOnly ? "One fixed ALA walk pose with scale-correct stage grounding; original saved light/camera settings unchanged.\n" :
                        "One fixed ALA walk pose, same camera and model. First: saved light settings; second: no shadows; third: zero bias, camera far60.\n") +
                        "Bake(false) transformed minY=" + plain + "; indexed minY=" + indexed + "; renderer bounds minY=" + skin.bounds.min.y +
                        "; skin lossyScale=" + skin.transform.lossyScale.ToString("F6") + "; Bake(true) local minY=" + mesh.vertices.Min(v => v.y) +
                        "; Bake(true) transformed minY=" + mesh.vertices.Min(v => skin.transform.TransformPoint(v).y);
                    File.WriteAllText(Path.Combine(folder, "ground-probe.txt"), report);
                    Debug.Log(report);
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            finally
            {
                if (previous.Any(s => s.isLoaded) && previous.Count(s => s.isActive) == 1)
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        [MenuItem("Statecraft/Character review/Capture game-angle and detail evidence")]
        public static void Capture()
        {
            string folder = Path.GetFullPath("../captures/character-review-scene");
            Directory.CreateDirectory(folder);
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var frames = new List<Frame>();
            try
            {
                var scene = EditorSceneManager.OpenScene(Generated1948CharacterReviewMenu.ScenePath);
                var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948CharacterReview>()).Single();
                review.BuildForReview();
                foreach (bool game in new[] { true, false })
                {
                    review.SetGameCamera(game);
                    foreach (int index in Enumerable.Range(-1, 8))
                    {
                        review.Focus(index);
                        foreach (bool walking in new[] { false, true })
                        {
                            review.SetMotion(walking);
                            review.SampleMotion(walking ? .6f : 2.5f);
                            foreach (bool rear in new[] { false, true })
                            {
                                if (rear) review.Turn(180);
                                string character = index < 0 ? "all-seven" : review.Characters[index].Id;
                                string name = character + "-" + (game ? "game-angle" : "detail") + "-" +
                                    (walking ? "walk" : "idle") + "-" + (rear ? "rear" : "front") + ".png";
                                Render(review.ReviewCamera, Path.Combine(folder, name));
                                frames.Add(new Frame {
                                    file = name, character = character, cameraMode = game ? "game-angle" : "detail", motion = walking ? "walk" : "idle", rear = rear,
                                    sampleSeconds = walking ? .6f : 2.5f, pitch = review.ReviewCamera.transform.eulerAngles.x, fov = review.ReviewCamera.fieldOfView,
                                    orthographic = review.ReviewCamera.orthographic, cameraPosition = review.ReviewCamera.transform.position,
                                    cameraEuler = review.ReviewCamera.transform.eulerAngles
                                });
                                if (rear) review.Turn(180);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (previous.Any(s => s.isLoaded) && previous.Count(s => s.isActive) == 1)
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            File.WriteAllText(Path.Combine(folder, "scene-camera-evidence.json"), JsonUtility.ToJson(new Record { frames = frames.ToArray() }, true));
            Debug.Log("CHARACTER REVIEW SCENE CAPTURES " + frames.Count + " " + folder);
        }
        static void Render(Camera camera, string path)
        {
            var target = new RenderTexture(960, 720, 24);
            var texture = new Texture2D(960, 720, TextureFormat.RGB24, false);
            var prior = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = prior; Object.DestroyImmediate(texture); Object.DestroyImmediate(target); }
        }
    }
}
