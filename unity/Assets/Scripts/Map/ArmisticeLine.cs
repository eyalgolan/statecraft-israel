#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Kits;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Marks one feature's line in the true-polyline overlay (<see cref="ArmisticeLines"/>, #237): its id, the date
    /// it is shown from (ArmisticeLines.ShowAt) and whether it is drawn in the low-accuracy style. It follows the zoom: far
    /// out it is drawn wider (ArmisticeLines.WidthScale).</summary>
    public sealed class ArmisticeLine : MonoBehaviour
    {
        public string Id = ""; public DateTime ShownFrom; public bool LowAccuracy;
        /// <summary>The width scale the line is drawn at now (ArmisticeLines.WidthScale).</summary>
        public float Scale { get; private set; }
        /// <summary>Makes the line's mesh at a width scale (owned by the overlay's MapRuntimeAssets); set by ArmisticeLines.Build.</summary>
        internal Func<float, Mesh>? MeshAt;
        readonly Dictionary<float, Mesh> meshes = new Dictionary<float, Mesh>();

        void LateUpdate() { var cam = Camera.main; if (cam != null) ShowFor(TownModelLayer.ViewDistance(cam)); }

        /// <summary>Draws the line at the width for a camera <paramref name="distanceM"/> away.</summary>
        public void ShowFor(float distanceM)
        {
            float scale = ArmisticeLines.WidthScale(distanceM);
            if (scale != Scale) DrawAt(scale);
        }

        /// <summary>Draws the line at a width scale; each scale's mesh is made once, on first use. At most three steps past the
        /// gameplay zoom, so the first zoom-out past each costs one rebuild of the lines (a one-off hitch).</summary>
        internal void DrawAt(float scale)
        {
            if (MeshAt == null) return;
            if (!meshes.TryGetValue(scale, out var mesh)) meshes[scale] = mesh = MeshAt(scale);
            GetComponent<MeshFilter>().sharedMesh = mesh; Scale = scale;
        }
    }
}
