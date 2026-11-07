#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Statecraft.Game.Map;
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
        Assert.Greater(cam.GetComponent<CameraRig>().TopInset, 0f, "the top bar has set the inset, so the matrix is the frozen custom one");
        Assert.AreEqual(frame, cam.aspect, 0.001f, "the camera's aspect is its frame's");
        var p = cam.projectionMatrix;
        Assert.AreEqual(frame, p[1, 1] / p[0, 0], 0.001f, "the projection is built at the frame's aspect");
    }

    [UnityTest]
    public IEnumerator TheShotRefusesAFrozenMatrixAtTheWrongAspect()
    {
        dir = Path.Combine(Path.GetTempPath(), "statecraft-shot-camera-" + Guid.NewGuid().ToString("N"));
        yield return CivMovementShots.Boot(false, dir, (t, p, u, g) => { rt = t; ps = p; });
        var cam = Camera.main;
        // The old rig pattern: the matrix froze at the batch run's 4:3, then cam.aspect was set without Apply.
        cam.aspect = 4f / 3f;
        cam.GetComponent<CameraRig>().ApplyProjection();
        cam.aspect = (float)CivMovementShots.Width / CivMovementShots.Height;
        Assert.Throws<InvalidOperationException>(() => CivMovementShots.Shot(rt!, "frozen"), "a frozen matrix at another aspect is refused");
    }
}
