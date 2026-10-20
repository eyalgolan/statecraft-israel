#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>The stand-in shapes drawn when a model is not on the machine: faceted low-poly unit shapes (footprint
    /// centred on the origin within a unit square, base at y = 0, 1 tall), each part with a palette colour. Self-contained
    /// so the hex tiles need nothing from other map layers.</summary>
    public static class HexStandIns
    {
        public static readonly IReadOnlyDictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            ["bark"] = new Color(0.36f, 0.27f, 0.20f), ["olive"] = new Color(0.47f, 0.52f, 0.36f), ["cypress"] = new Color(0.16f, 0.30f, 0.18f),
            ["palm"] = new Color(0.33f, 0.48f, 0.22f), ["rock"] = new Color(0.62f, 0.57f, 0.50f), ["plaster"] = new Color(0.95f, 0.93f, 0.87f),
            ["green"] = new Color(0.55f, 0.62f, 0.30f), ["hull"] = new Color(0.20f, 0.28f, 0.30f),
        };

        public enum Shape { Box, Olive, Cypress, Palm, Rock, Field }

        public static (Mesh mesh, string colour)[] Build(Shape shape) => shape switch
        {
            Shape.Box => new[] { (Frustum(4, (0f, 0.7071f), (1f, 0.7071f), 45f), "plaster") },
            Shape.Olive => new[] { (Frustum(5, (0f, 0.06f), (0.35f, 0.05f)), "bark"), (Frustum(6, (0.3f, 0.3f), (0.6f, 0.48f), (1f, 0.18f)), "olive") },
            Shape.Cypress => new[] { (Frustum(5, (0f, 0.04f), (0.12f, 0.03f)), "bark"), (Frustum(6, (0.1f, 0.1f), (0.35f, 0.17f), (1f, 0f)), "cypress") },
            Shape.Palm => new[] { (Frustum(5, (0f, 0.03f), (0.85f, 0.024f)), "bark"), (Frustum(8, (0.8f, 0.04f), (0.88f, 0.42f), (1f, 0.05f)), "palm") },
            Shape.Rock => new[] { (Frustum(5, (0f, 0.5f), (1f, 0.28f)), "rock") },
            _ => new[] { (Frustum(4, (0f, 0.7071f), (1f, 0.7071f), 45f), "green") },
        };

        /// <summary>A stack of rings (height, radius) joined by flat-shaded sides, closed top and bottom.</summary>
        static Mesh Frustum(int sides, params (float y, float r)[] rings) => Frustum(sides, 0f, rings);

        static Mesh Frustum(int sides, (float y, float r) a, (float y, float r) b, float turnDeg) => Frustum(sides, turnDeg, a, b);

        static Mesh Frustum(int sides, float turnDeg, params (float y, float r)[] rings)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 P(int ring, int i)
            {
                float a = Mathf.Deg2Rad * (turnDeg + 360f * i / sides);
                return new Vector3(Mathf.Cos(a) * rings[ring].r, rings[ring].y, Mathf.Sin(a) * rings[ring].r);
            }
            void Tri(Vector3 p, Vector3 q, Vector3 r) { int n = v.Count; v.Add(p); v.Add(q); v.Add(r); t.Add(n); t.Add(n + 1); t.Add(n + 2); }
            for (int k = 0; k + 1 < rings.Length; k++)
                for (int i = 0; i < sides; i++)
                {
                    Vector3 a0 = P(k, i), a1 = P(k, i + 1), b0 = P(k + 1, i), b1 = P(k + 1, i + 1);
                    Tri(a0, b1, a1); Tri(a0, b0, b1);   // clockwise from outside (Unity's front face)
                }
            var bottom = new Vector3(0f, rings[0].y, 0f); var top = new Vector3(0f, rings[rings.Length - 1].y, 0f);
            for (int i = 0; i < sides; i++)
            {
                Tri(bottom, P(0, i), P(0, i + 1));
                if (rings[rings.Length - 1].r > 0f) Tri(top, P(rings.Length - 1, i + 1), P(rings.Length - 1, i));
            }
            var m = new Mesh { name = "hex-standin" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Samples the idle's first frame for authoring/Legacy clips; player non-Legacy rigs keep their authored Animator.</summary>
        public static void SampleIdle(Animator a)
        {
            var clips = a.runtimeAnimatorController != null ? a.runtimeAnimatorController.animationClips : System.Array.Empty<AnimationClip>();
            AnimationClip? clip = null;
            foreach (var c in clips) if (c != null && c.name.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0) { clip = c; break; }
            if (clip == null && clips.Length > 0) clip = clips[0];
            if (clip != null) SampleClip(clip, a.gameObject, 0f);
        }

        // Collection's non-Legacy player rigs run their authored Animator controller. Manual sampling is reserved for
        // Editor previews and Legacy clips; sampling these streamed rigs in a player logs an error on every pose.
        internal static void SampleClip(AnimationClip clip, GameObject rig, float time)
        {
            if (Application.isEditor || clip.legacy) clip.SampleAnimation(rig, time);
        }
    }
}
