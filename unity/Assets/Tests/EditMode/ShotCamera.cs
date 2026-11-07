#nullable enable
using Statecraft.Game.Map;
using UnityEngine;

/// <summary>#2882: the one place a shot rig sets its camera to the frame it renders. CameraRig.ApplyProjection freezes the
/// projection as a custom matrix once TopInset is above 0 (the top bar sets it), and a custom matrix no longer follows the
/// target texture, so a camera left at the batch run's 4:3 aspect draws a 16:9 still 1.33 times too wide.</summary>
static class ShotCamera
{
    /// <summary>Sets the camera's aspect to <paramref name="width"/> by <paramref name="height"/> and rebuilds the rig's
    /// projection at it.</summary>
    internal static void Configure(Camera cam, int width, int height)
    {
        cam.aspect = (float)width / height;
        var rig = cam.GetComponent<CameraRig>();
        if (rig != null) rig.ApplyProjection();
    }

    /// <summary>Throws when the camera would draw a stretched still: its aspect, or the aspect its projection matrix was
    /// built at, differs from the frame's. A rig that set cam.aspect after the top bar froze the matrix passes the first check
    /// and fails the second.</summary>
    internal static void Require(Camera cam, int width, int height)
    {
        float frame = (float)width / height;
        var p = cam.projectionMatrix;
        float built = p[1, 1] / p[0, 0];
        if (Mathf.Abs(cam.aspect - frame) > 0.001f || Mathf.Abs(built - frame) > 0.001f)
            throw new System.InvalidOperationException($"the camera's aspect is {cam.aspect:0.000} and its projection's {built:0.000}, its frame's {frame:0.000} ({width} x {height}): call ShotCamera.Configure after the scene opens");
    }
}
