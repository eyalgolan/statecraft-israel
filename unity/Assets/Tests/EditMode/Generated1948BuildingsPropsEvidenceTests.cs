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
        string dir = Path.Combine(Paths.RepoRoot, "captures", "buildings-props-playmode"); Directory.CreateDirectory(dir);
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
                if (sample.name.StartsWith("Repaired"))
                {
                    rig.Yaw = 180f; rig.Apply(); yield return null; Capture(camera, Path.Combine(dir, file + "-rear.png"));
                }
            }
            // Matched campaign evidence: restore only the pre-branch house pools in memory, rebuild the same map,
            // then restore the approved pools. No source asset or simulation state is changed by this comparison.
            var map = Object.FindFirstObjectByType<MapScene>();
            var layer = map.GetComponentInChildren<TownModelLayer>();
            var courtyard = layer.Instances().First(i => i.model == TownModel.CourtyardRepair2026).m.GetColumn(3);
            var anchors = new[] {
                map.Frame.ToWorld(map.Settlements.Single(s => s.Id == "netanya").Lon, map.Settlements.Single(s => s.Id == "netanya").Lat, 0),
                map.Frame.ToWorld(map.Settlements.Single(s => s.Id == "tiberias").Lon, map.Settlements.Single(s => s.Id == "tiberias").Lat, 0),
                new Vector3(courtyard.x, courtyard.y, courtyard.z) };
            string[] towns = { "netanya", "tiberias", "courtyard-plot" };
            var pools = new[] { TownModels.Netanya, TownModels.Tiberias, TownModels.ArabCourtyard };
            var originals = pools.Select(pool => pool.ToArray()).ToArray();
            try
            {
                for (int i = 0; i < pools.Length; i++) for (int j = 0; j < pools[i].Length; j++)
                    pools[i][j] = i == 0 ? TownModel.B13aTwoStorey : TownModel.B17CourtyardHouse;
                map.Build();
                for (int i = 0; i < anchors.Length; i++)
                {
                    rig.LookAt = anchors[i]; rig.LookAt.y = map.Terrain.SampleHeight(anchors[i]) + map.Terrain.transform.position.y;
                    rig.Distance = CameraController.MinDistance; rig.Yaw = 0; rig.Apply(); yield return null;
                    Capture(camera, Path.Combine(dir, towns[i] + "-before.png"));
                }
            }
            finally { for (int i = 0; i < pools.Length; i++) System.Array.Copy(originals[i], pools[i], pools[i].Length); }
            map.Build();
            for (int i = 0; i < anchors.Length; i++)
            {
                rig.LookAt = anchors[i]; rig.LookAt.y = map.Terrain.SampleHeight(anchors[i]) + map.Terrain.transform.position.y;
                rig.Distance = CameraController.MinDistance; rig.Yaw = 0; rig.Apply(); yield return null;
                Capture(camera, Path.Combine(dir, towns[i] + "-after.png"));
            }
            File.WriteAllText(Path.Combine(dir, "README.txt"), "Actual Unity Editor Play Mode, runtime Start placement, real MapScene CameraRig at its fixed elevated pitch. PC quality,3840x2160. Camera.Render captures exclude screen-space GUI labels/HUD. Matched campaign pairs temporarily restore only original B13/B17 pool contents in memory and rebuild identical map inputs, then restore approved pools; same camera/quality. Not a native player or new historical deployment.\n");
            Debug.Log("BUILDINGS/PROPS PLAY MODE EVIDENCE: all11 + focused roof views -> " + dir);
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
