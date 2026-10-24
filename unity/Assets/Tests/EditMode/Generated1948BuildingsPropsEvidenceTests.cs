#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class Generated1948BuildingsPropsEvidenceTests
{
    static readonly string[] Regenerated = { "Tiled house", "Porch house", "Courtyard house" };

    [UnityTest, Explicit("Actual Play Mode game-camera evidence; run after category preparation")]
    public IEnumerator CaptureRunningReview()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
        yield return new EnterPlayMode();
        Generated1948BuildingsPropsReview? review = null;
        for (int frame = 0; frame < 900; frame++)
        {
            review = Object.FindFirstObjectByType<Generated1948BuildingsPropsReview>();
            if (review != null && review.PlacedCount == 11) break;
            yield return null;
        }
        Assert.IsNotNull(review); Assert.AreEqual(11, review!.PlacedCount);
        var camera = Camera.main; var rig = camera.GetComponent<CameraRig>();
        string dir = Path.Combine(Paths.RepoRoot, "captures", "buildings-props-regenerated-review-v3"); Directory.CreateDirectory(dir);
        int oldQuality = QualitySettings.GetQualityLevel();
        try
        {
            int pc = System.Array.IndexOf(QualitySettings.names, "PC"); Assert.GreaterOrEqual(pc, 0); QualitySettings.SetQualityLevel(pc, true);
            rig.Apply(); yield return null; Capture(camera, Path.Combine(dir, "all11-overview.png"));
            var display = review.transform.Find("Buildings and props — review samples");
            foreach (Transform sample in display)
            {
                if (sample.name == "Review display surface") continue;
                var bounds = Generated1948BuildingsPropsReview.BoundsOf(sample.gameObject);
                rig.LookAt = bounds.center; rig.Distance = CameraController.MinDistance; rig.Yaw = 0f; rig.Apply();
                yield return null;
                string file = sample.name.ToLowerInvariant().Replace(' ', '-'); Capture(camera, Path.Combine(dir, file + ".png"));
                if (Regenerated.Contains(sample.name))
                {
                    rig.Yaw = 180f; rig.Apply(); yield return null; Capture(camera, Path.Combine(dir, file + "-rear.png"));
                }
            }
            File.WriteAllText(Path.Combine(dir, "README.txt"), "Actual Unity Editor Play Mode, runtime Start placement, real MapScene CameraRig at its fixed elevated pitch. PC quality,3840x2160. Camera.Render captures exclude screen-space GUI labels/HUD. Editor-only source inspection; no campaign variation is activated. 01, 03 and 05 are the 24 October 2026 regenerations (#1379), shown front and rear. Not a native player or new historical deployment.\n");
            Debug.Log("BUILDINGS/PROPS EDITOR EVIDENCE: all11 + regenerated front/rear views -> " + dir);
        }
        finally { QualitySettings.SetQualityLevel(oldQuality, true); }
        yield return new ExitPlayMode();
    }

    [UnityTearDown] public IEnumerator Restore()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void Capture(Camera camera, string file)
    {
        var old = camera.targetTexture; var active = RenderTexture.active;
        var target = new RenderTexture(3840,2160,24); var image = new Texture2D(3840,2160,TextureFormat.RGB24,false);
        try { camera.targetTexture=target; camera.Render(); camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,3840,2160),0,0); image.Apply(); File.WriteAllBytes(file,image.EncodeToPNG()); }
        finally { camera.targetTexture=old; RenderTexture.active=active; Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
    }
}
