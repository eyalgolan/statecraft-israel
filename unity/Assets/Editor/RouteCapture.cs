using System.IO;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Close looks at the 1948 railways and roads (docs/routes.md), for the art checkpoint's side-by-sides against the
/// promo video's 0:21 close-up. Editor renders at the web tier, tilt-shift off, the train still. Run headless (with
/// graphics, never -nographics):
/// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod RouteCapture.Close [-captureDir DIR]
/// Writes routes-&lt;view&gt;.png (default captures/visual/routes, git-ignored).</summary>
public static class RouteCapture
{
    /// <summary>(name, lon, lat, distance m, yaw): the still train east of Jaffa, Lydda junction, Latrun to Bab al-Wad with
    /// the Burma Road, the Atlit coast line, the Sorek gorge, the Jericho road, the Jezreel Valley line's Jordan end and Jerusalem's Old City (whole, and close).</summary>
    static readonly (string name, double lon, double lat, float distance, float yaw)[] Views =
    {
        ("train", 34.83, 32.01, 16000f, 10f),
        ("lydda", 34.875, 31.945, 14000f, 10f),
        ("latrun", 34.99, 31.82, 22000f, 0f),
        ("atlit", 34.95, 32.70, 14000f, 0f),       // the Hadera–Haifa line along the Atlit coast (review B14)
        ("sorek", 35.07, 31.755, 10000f, 0f),      // the Jaffa–Jerusalem line in the Sorek gorge (review B15)
        ("jericho", 35.36, 31.80, 14000f, 0f),     // the Jerusalem–Jericho road's descent (review A-I1)
        ("jezreel", 35.55, 32.60, 16000f, 0f),     // the Jezreel Valley line's Jordan end, Beisan to Gesher (#252)
        ("jerusalem", 35.234, 31.777, 32000f, 0f), // the Old City's walls and the roads that end at them (#319)
        ("oldcity", 35.2355, 31.7765, 15000f, 0f), // the Old City close: its walls, gates, the Haram and Al-Aqsa (#319)
    };

    public static void Close()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        var dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(Paths.RepoRoot, "captures", "visual", "routes");
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = MapCapture.RequireMapScene(); map.Build();
        var rig = Camera.main.GetComponent<CameraRig>();
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(0, true);
            if (System.Linq.Enumerable.Contains(VisualToggles.Names, "tiltshift")) VisualToggles.Set("tiltshift", false);
            foreach (var v in Views)
            {
                rig.LookAt = map.Frame.ToWorld(v.lon, v.lat, 0); rig.Distance = v.distance; rig.Yaw = v.yaw; rig.TopInset = 0; rig.Apply();
                Save(Camera.main, 1920, 1080, Path.Combine(dir, $"routes-{v.name}.png"));
            }
        }
        finally { VisualToggles.SetAll(true); QualitySettings.SetQualityLevel(saved, true); }
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
