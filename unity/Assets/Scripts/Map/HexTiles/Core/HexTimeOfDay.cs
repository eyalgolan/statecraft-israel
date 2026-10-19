#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.HexTiles
{
    /// <summary>The hex-tile map's time of day. Day is today's warm look (HexLook); dusk and night are presets laid over it.</summary>
    public enum HexTime { Day, Dusk, Night }

    /// <summary>Time-of-day presets for the opt-in hex-tile map, next to HexLook: presentation only, no simulation state is
    /// read or written. Web ?hex-time=dusk|night (a top-level query parameter, with ?hex-tiles), desktop -hex-time dusk|night;
    /// the Hex Painter has a switch. Apply returns the undo, which restores everything it changed.
    /// STUB for round 2: the signatures are fixed so the painter can be wired; lane D fills in the presets.</summary>
    public static class HexTimeOfDay
    {
        public static readonly string[] Ids = { "day", "dusk", "night" };

        public static string Id(HexTime time) => Ids[(int)time];

        /// <summary>A preset by its id (day, dusk, night).</summary>
        public static bool TryParse(string? id, out HexTime time)
        {
            int i = Array.IndexOf(Ids, id ?? "");
            time = i < 0 ? HexTime.Day : (HexTime)i;
            return i >= 0;
        }

        public static HexTime Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>The preset asked for, day when the flag is absent or names no preset.</summary>
        public static HexTime Requested(string? url, string[] args, bool webgl) => HexTime.Day;

        /// <summary>Lays <paramref name="time"/> over the day look already applied (HexLook.Apply): the grade in
        /// <paramref name="profile"/>, the <paramref name="sun"/>, the ambient light and, for night, the board under
        /// <paramref name="boardRoot"/> (null: none). Day changes nothing. Returns the undo.</summary>
        public static Action Apply(HexTime time, VolumeProfile? profile, Light? sun, GameObject? boardRoot) => () => { };
    }
}
