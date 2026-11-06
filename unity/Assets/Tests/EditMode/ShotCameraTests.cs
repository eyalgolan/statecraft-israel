#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#2882: the shot rigs boot the game and render Camera.main into a 1280 x 720 texture. CameraRig.ApplyProjection
/// freezes the projection as a custom matrix once the top bar sets TopInset, so a camera left at the batch run's 4:3 aspect
/// drew every still 1.33 times too wide. Must run in batch mode: an editor whose Game view is 16:9 passes by accident.</summary>
public class ShotCameraTests
{
    RenderTexture? rt; PanelSettings? ps; string dir = "";

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (dir != "" && Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [UnityTest]
    public IEnumerator TheShotBootsCameraMatchesItsFrame()
    {
        dir = Path.Combine(Path.GetTempPath(), "statecraft-shot-camera-" + Guid.NewGuid().ToString("N"));
        yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; });
        var cam = Camera.main;
        float frame = (float)CivMovementShots.Width / CivMovementShots.Height;
        Assert.AreEqual(frame, cam.aspect, 0.001f, "the camera's aspect is its frame's");
        var p = cam.projectionMatrix;
        Assert.AreEqual(frame, p[1, 1] / p[0, 0], 0.001f, "the projection is built at the frame's aspect");
    }
}
