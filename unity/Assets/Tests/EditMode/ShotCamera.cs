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
}
