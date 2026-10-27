#nullable enable
using System.Linq;
using Statecraft.HexTiles.Capture;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>The light and grade the painter's time-of-day switch works on: the Sun light and PostFX volume that
    /// HexBoardCapture.Stage makes ("Add the captures' light and grade"), found in the open scene or made once.</summary>
    public static class HexPainterStage
    {
        public const string SunName = "Sun", VolumeName = "PostFX";

        /// <summary>The scene's staged Sun and PostFX profile, or (null, null) when either is missing.</summary>
        public static (Light? sun, VolumeProfile? profile) Find()
        {
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(l => l.name == SunName && l.type == LightType.Directional);
            var volume = Object.FindObjectsByType<Volume>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(v => v.name == VolumeName && v.isGlobal);
            if (sun == null || volume == null) return (null, null);
            // Stage gives the volume its own profile instance, so editing it changes no asset.
            return (sun, volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile);
        }

        /// <summary>The staged Sun and PostFX profile, staging them (HexBoardCapture.Stage) when the scene lacks either.</summary>
        public static (Light sun, VolumeProfile profile) FindOrStage()
        {
            var (sun, profile) = Find();
            if (sun == null || profile == null)
            {
                HexBoardCapture.Stage();
                (sun, profile) = Find();
            }
            return (sun!, profile!);
        }

        /// <summary>Switches <paramref name="switches"/> to <paramref name="time"/> on the staged light and grade and the
        /// board under <paramref name="boardRoot"/> (HexTimeOfDay.Apply), undoing the preset before it.</summary>
        public static void SetTime(HexPainterSwitches switches, HexTime time, GameObject? boardRoot)
        {
            switches.UndoTime();   // restore the day look before the light is looked up or staged
            if (time == HexTime.Day) return;
            var (sun, profile) = FindOrStage();
            switches.SetTime(time, t => HexTimeOfDay.Apply(t, profile, sun, boardRoot));
        }
    }
}
