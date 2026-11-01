#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The promo's unit badge (promo/final/03-gameplay-1936.png): a small dark rounded plate with a light glyph,
    /// floating over a token so a player sees it is a unit and can click it. Two flat unlit meshes in the local XY plane
    /// (the plate, and the glyph a little towards the camera), turned to face each camera as it renders (UnitLayer). The
    /// glyph says the service: two figures for a brigade, an aircraft for the squadron, a ship for the navy.</summary>
    public sealed class UnitBadge : MonoBehaviour
    {
        /// <summary>The plate's width at map scale (m): about a third of a figure's height, as in the promo.</summary>
        public const float SizeM = 1400f;
        /// <summary>The largest share of the screen's height a plate is drawn at (owner, 31 October 2026, "Shrink up close
        /// (Recommended)"): a badge as big as it is in a 30 km view keeps <see cref="SizeM"/>; a nearer camera draws it smaller,
        /// so it never grows past this. Just over the largest a plate is drawn from 30 km out (0.142, 154 of 1080 pixels, at the
        /// view's lower edge; UnitBadgeScreenCapTests), so every view from there out is unchanged.</summary>
        public const float MaxScreenShare = 0.15f;
        /// <summary>Clearance (m) between the token's top and the plate's lower edge.</summary>
        public const float LiftM = 1300f;
        /// <summary>A rifleman's height (m, real) and the corvette's mast top (m, real): the token's top before scaling.</summary>
        public const float FigureHeightM = 1.78f, MastHeightM = 21f;
        /// <summary>The glyph's offset towards the camera, in plate widths.</summary>
        public const float GlyphLift = 0.02f;
        /// <summary>The promo badge's dark navy and its ivory glyph (the selection ring's colour).</summary>
        public static readonly Color PlateColour = new Color(0.10f, 0.13f, 0.20f), GlyphColour = MapScene.SelectionColour;

        /// <summary>The plate's width (m) for a perspective camera whose projection scales y by <paramref name="m11"/>
        /// (Camera.projectionMatrix[1, 1]) and that sees the plate <paramref name="depth"/> metres ahead: a plate w wide spans
        /// w m11 / (2 depth) of the screen's height, so <see cref="SizeM"/>, or less to keep that within
        /// <see cref="MaxScreenShare"/>. SizeM for an orthographic camera (m11 is then not a perspective scale) or a plate
        /// behind the camera.</summary>
        public static float SizeFor(float depth, float m11, bool orthographic) =>
            orthographic || depth <= 0f || m11 <= 0f ? SizeM : Mathf.Min(SizeM, 2f * MaxScreenShare * depth / m11);

        /// <summary>A rounded square, 1 x 1 centred on the origin, corner radius 0.22.</summary>
        public static Mesh PlateMesh()
        {
            var outline = new List<Vector2>();
            const float half = 0.5f, rad = 0.22f; const int steps = 5;
            var corners = new[] { new Vector2(half - rad, half - rad), new Vector2(-half + rad, half - rad), new Vector2(-half + rad, -half + rad), new Vector2(half - rad, -half + rad) };
            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= steps; s++)
                {
                    float a = Mathf.PI / 2f * (c + (float)s / steps);
                    outline.Add(corners[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad);
                }
            var m = new Flat();
            m.Fan(outline);
            return m.Build("UnitBadgePlate");
        }

        /// <summary>The service's glyph inside the plate (about 0.6 across).</summary>
        public static Mesh GlyphMesh(string service)
        {
            var m = new Flat();
            switch (service)
            {
                case UnitData.Army:   // two standing figures: a head and a tapered body each
                    foreach (var x in new[] { -0.13f, 0.13f })
                    {
                        m.Fan(Circle(new Vector2(x, 0.17f), 0.07f, 12));
                        m.Fan(new List<Vector2> { new Vector2(x - 0.10f, -0.26f), new Vector2(x + 0.10f, -0.26f), new Vector2(x + 0.08f, 0.07f), new Vector2(x - 0.08f, 0.07f) });
                    }
                    break;
                case UnitData.Air:    // an aircraft seen from above: fuselage, wings, tailplane
                    m.Fan(Rect(0f, 0f, 0.07f, 0.56f));
                    m.Fan(Rect(0f, 0.06f, 0.56f, 0.10f));
                    m.Fan(Rect(0f, -0.22f, 0.22f, 0.07f));
                    break;
                default:              // a ship in profile: hull, bridge, funnel
                    m.Fan(new List<Vector2> { new Vector2(-0.30f, -0.04f), new Vector2(0.30f, -0.04f), new Vector2(0.22f, -0.16f), new Vector2(-0.24f, -0.16f) });
                    m.Fan(Rect(0.04f, 0.04f, 0.16f, 0.16f));
                    m.Fan(Rect(-0.10f, 0.08f, 0.08f, 0.24f));
                    break;
            }
            return m.Build("UnitBadgeGlyph-" + service);
        }

        static List<Vector2> Rect(float cx, float cy, float w, float h) => new List<Vector2>
            { new Vector2(cx - w / 2, cy - h / 2), new Vector2(cx + w / 2, cy - h / 2), new Vector2(cx + w / 2, cy + h / 2), new Vector2(cx - w / 2, cy + h / 2) };

        static List<Vector2> Circle(Vector2 c, float r, int n)
        {
            var pts = new List<Vector2>(n);
            for (int i = 0; i < n; i++) { float a = 2f * Mathf.PI * i / n; pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return pts;
        }

        /// <summary>Convex polygons as triangle fans in the XY plane (the badge shader draws both faces).</summary>
        sealed class Flat
        {
            readonly List<Vector3> verts = new List<Vector3>(); readonly List<int> tris = new List<int>();
            public void Fan(List<Vector2> outline)
            {
                int b = verts.Count;
                foreach (var p in outline) verts.Add(new Vector3(p.x, p.y, 0f));
                for (int i = 1; i + 1 < outline.Count; i++) tris.AddRange(new[] { b, b + i, b + i + 1 });
            }
            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
