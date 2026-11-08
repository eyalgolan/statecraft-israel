#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>#2202: a side's flag in the UI, the enemy playback's footer picture (owner, 8 November 2026, "Flag + name
    /// (Recommended)"). It draws the very cloth the side's tokens fly: a sourced 1948 flag is <see cref="UnitFlags.BuildCloth"/>'s
    /// triangles in <see cref="UnitFlags.Colours"/>, seen face on; a side the map gives the neutral pennant
    /// (<see cref="UnitFlags.HasPennant"/>, the Arab Liberation Army) gets <see cref="UnitFlags.PennantCloth"/> in
    /// <see cref="UnitFlags.PennantColour"/>. So there is one definition of each design, and nothing here is drawn by hand.
    /// The triangles go straight to the UI mesh (MeshGenerationContext.Allocate, as UiFrame's rules): Painter2D would fill
    /// each triangle with an antialiased edge and show hairline seams across the cloth. The flag is never mirrored in Hebrew
    /// (a flag has one right way round); the row it sits in is. It takes no clicks.</summary>
    [UxmlElement]
    public partial class SideFlag : VisualElement
    {
        /// <summary>A flag in figure units (x along the cloth from the pole, y up): its triangles, three points each, and one
        /// colour per triangle, in drawing order (a later colour lies over an earlier one).</summary>
        public sealed class Shape
        {
            public readonly IReadOnlyList<Vector2> Points;
            public readonly IReadOnlyList<Color> Colours;
            public readonly Rect Bounds;
            public Shape(IReadOnlyList<Vector2> points, IReadOnlyList<Color> colours)
            {
                if (points.Count != colours.Count * 3) throw new ArgumentException("three points a triangle", nameof(points));
                Points = points; Colours = colours;
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var p in points) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
                Bounds = points.Count == 0 ? Rect.zero : Rect.MinMaxRect(x0, y0, x1, y1);
            }
        }

        static readonly Dictionary<string, Shape?> Shapes = new Dictionary<string, Shape?>(StringComparer.Ordinal);

        /// <summary>The flag <paramref name="side"/>'s tokens fly, or null for a side the map draws with no flag or pennant.
        /// Built once a side from the token's own mesh and kept.</summary>
        public static Shape? For(string? side)
        {
            if (string.IsNullOrEmpty(side)) return null;
            if (Shapes.TryGetValue(side!, out var known)) return known;
            Shape? made = null;
            if (UnitFlags.Has(side))
            {
                var cloth = UnitFlags.BuildCloth(side);
                if (cloth != null)
                {
                    var colours = UnitFlags.Colours(side!);
                    var verts = cloth.vertices;
                    var points = new List<Vector2>(); var tints = new List<Color>();
                    for (int k = 0; k < cloth.subMeshCount && k < colours.Count; k++)
                    {
                        var tris = cloth.GetTriangles(k);
                        for (int i = 0; i + 2 < tris.Length; i += 3)
                        {
                            for (int j = 0; j < 3; j++) { var v = verts[tris[i + j]]; points.Add(new Vector2(v.x, v.y)); }
                            tints.Add(colours[k]);
                        }
                    }
                    if (Application.isPlaying) UnityEngine.Object.Destroy(cloth); else UnityEngine.Object.DestroyImmediate(cloth);
                    made = new Shape(points, tints);
                }
            }
            else if (UnitFlags.HasPennant(side))
            {
                var outline = UnitFlags.PennantCloth();
                var points = new List<Vector2>(); var tints = new List<Color>();
                for (int i = 1; i + 1 < outline.Count; i++)   // a fan from the first point, as MiniatureMesh.Plate
                {
                    points.Add(outline[0]); points.Add(outline[i]); points.Add(outline[i + 1]);
                    tints.Add(UnitFlags.PennantColour);
                }
                made = new Shape(points, tints);
            }
            Shapes[side!] = made;
            return made;
        }

        string? side;
        Shape? shape;

        public SideFlag()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("side-flag");
            generateVisualContent += Draw;
        }

        /// <summary>The side whose flag is drawn; setting the same side again does not redraw.</summary>
        public string? Side
        {
            get => side;
            set
            {
                if (string.Equals(side, value, StringComparison.Ordinal)) return;
                side = value; shape = For(value);
                Changes++;
                style.display = shape == null ? DisplayStyle.None : DisplayStyle.Flex;
                MarkDirtyRepaint();
            }
        }

        /// <summary>The flag being drawn, or null.</summary>
        public Shape? Drawn => shape;
        /// <summary>How many times the picture changed (a test seam: the flag changes once a side).</summary>
        public int Changes { get; private set; }

        void Draw(MeshGenerationContext mgc)
        {
            var s = shape; var r = contentRect;
            if (s == null || s.Points.Count == 0 || r.width <= 0f || r.height <= 0f || s.Bounds.width <= 0f || s.Bounds.height <= 0f) return;
            float scale = Mathf.Min(r.width / s.Bounds.width, r.height / s.Bounds.height);
            float ox = r.x + (r.width - s.Bounds.width * scale) * 0.5f, oy = r.y + (r.height - s.Bounds.height * scale) * 0.5f;
            Vector2 At(Vector2 p) => new Vector2(ox + (p.x - s.Bounds.xMin) * scale, oy + (s.Bounds.yMax - p.y) * scale);   // y down
            int tris = s.Colours.Count;
            const int perBatch = 3 * 20000;   // under UI Toolkit's 65535 vertices a mesh
            for (int start = 0; start < tris; start += perBatch / 3)
            {
                int n = Mathf.Min(perBatch / 3, tris - start);
                var m = mgc.Allocate(n * 3, n * 3);
                for (int t = 0; t < n; t++)
                {
                    int i = (start + t) * 3;
                    Vector2 a = At(s.Points[i]), b = At(s.Points[i + 1]), c = At(s.Points[i + 2]);
                    // UI Toolkit wants clockwise triangles on screen (y down): a positive cross product.
                    if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) < 0f) (b, c) = (c, b);
                    Color32 tint = s.Colours[start + t];
                    foreach (var p in new[] { a, b, c }) m.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = tint });
                    for (int j = 0; j < 3; j++) m.SetNextIndex((ushort)(t * 3 + j));
                }
            }
        }
    }
}
