#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The legislature as a half circle of seats (promo/final/04), drawn with Painter2D: one dot per seat of the
    /// body that sits on the date (legislatures.yaml size), coloured by block in the order the presenter gives (the
    /// coalition's parties first, from the left), with a gold majority line that, row by row, runs between the last seat of
    /// a majority (more than half the seats, counted from the left) and the next seat, so no dot sits on it. Seats a record
    /// does not assign to a party are drawn as hollow rings, never given to a party. It takes no clicks.</summary>
    [UxmlElement]
    public partial class Hemicycle : VisualElement
    {
        /// <summary>One party's block of seats, in drawing order.</summary>
        public readonly struct Group : IEquatable<Group>
        {
            public readonly string Party; public readonly int Seats; public readonly Color Colour; public readonly bool Coalition;
            public Group(string party, int seats, Color colour, bool coalition) { Party = party; Seats = seats; Colour = colour; Coalition = coalition; }
            public bool Equals(Group o) => Party == o.Party && Seats == o.Seats && Colour == o.Colour && Coalition == o.Coalition;
        }

        /// <summary>One seat: its centre on the unit half circle (x in -1..1, y in 0..1, y up), its angle and its row.</summary>
        public readonly struct SeatPlace
        {
            public readonly Vector2 Position; public readonly float Angle; public readonly int Row;
            public SeatPlace(Vector2 position, float angle, int row) { Position = position; Angle = angle; Row = row; }
        }

        static readonly Color Gold = new Color32(232, 204, 142, 255), Unrecorded = new Color32(170, 164, 150, 255);
        /// <summary>Inner radius of the half circle, as a share of the outer.</summary>
        public const float Inner = 0.42f;

        IReadOnlyList<Group> groups = Array.Empty<Group>();
        int size;
        List<SeatPlace> places = new List<SeatPlace>();
        List<Color?> colours = new List<Color?>();
        List<Vector2> boundary = new List<Vector2>();
        float[] cuts = Array.Empty<float>();
        float dot;

        public Hemicycle()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public IReadOnlyList<Group> Groups => groups;
        /// <summary>The number of seats in the body.</summary>
        public int Size => size;
        /// <summary>Seats needed for a majority: more than half.</summary>
        public int Majority => size / 2 + 1;
        /// <summary>How many times <see cref="Set"/> changed the picture (an unchanged house is not redrawn).</summary>
        public int Changes { get; private set; }

        /// <summary>Sets the body's size and its party blocks (their seats may sum to less than the size: the rest are drawn
        /// unassigned; never more). The same house again changes nothing and is not redrawn.</summary>
        public void Set(IReadOnlyList<Group> blocks, int bodySize)
        {
            if (bodySize < 0) throw new ArgumentOutOfRangeException(nameof(bodySize));
            if (blocks.Sum(g => g.Seats) > bodySize) throw new ArgumentException("more party seats than the body has", nameof(blocks));
            if (bodySize == size && blocks.SequenceEqual(groups)) return;
            groups = blocks.ToList(); size = bodySize;
            places = Places(size);
            dot = DotRadius(size);
            colours = new List<Color?>(size);
            foreach (var g in groups) for (int i = 0; i < g.Seats; i++) colours.Add(g.Colour);
            while (colours.Count < size) colours.Add(null);
            cuts = Cuts(places, Majority, size);
            boundary = Boundary(cuts, size);
            Changes++;
            MarkDirtyRepaint();
        }

        /// <summary>Each seat's colour from left to right: the blocks in order, then unassigned seats (null).</summary>
        public IReadOnlyList<Color?> SeatColours() => colours;

        /// <summary>The majority line on the unit half circle, from the inner edge out (see <see cref="Boundary"/>).</summary>
        public IReadOnlyList<Vector2> MajorityLine => boundary;

        /// <summary>The majority line's angle in each row (radians, inner row first; NaN for an empty row).</summary>
        public IReadOnlyList<float> MajorityCuts => cuts;

        /// <summary>Rows of seats for a body of <paramref name="seatCount"/>.</summary>
        public static int Rows(int seatCount) => seatCount <= 12 ? 2 : seatCount <= 40 ? 4 : seatCount <= 90 ? 5 : seatCount <= 160 ? 7 : 9;

        static float Radius(int row, int rows) => rows == 1 ? 1f : Inner + (1f - Inner) * row / (rows - 1);

        /// <summary>Seats per row, inner first: in proportion to each row's length, with at most one row holding an odd
        /// count (pairs of odd rows give a seat from the inner to the outer one), so at most one seat sits at the top.</summary>
        public static int[] RowCounts(int seatCount)
        {
            int rows = Rows(seatCount);
            var radii = Enumerable.Range(0, rows).Select(i => Radius(i, rows)).ToArray();
            var total = radii.Sum();
            var counts = radii.Select(r => (int)Math.Floor(seatCount * r / total)).ToArray();
            for (int i = rows - 1, left = seatCount - counts.Sum(); left > 0; i = i == 0 ? rows - 1 : i - 1, left--) counts[i]++;   // the rest to the outer rows
            var odd = Enumerable.Range(0, rows).Where(i => counts[i] % 2 == 1).ToList();
            for (int k = 0; k + 1 < odd.Count; k += 2) { counts[odd[k]]--; counts[odd[k + 1]]++; }
            return counts;
        }

        /// <summary>Every seat, from left to right by angle (and from the inside out within an angle).</summary>
        public static List<SeatPlace> Places(int seatCount)
        {
            var result = new List<SeatPlace>(Math.Max(0, seatCount));
            if (seatCount <= 0) return result;
            int rows = Rows(seatCount);
            var counts = RowCounts(seatCount);
            var placed = new List<(float angle, int row)>(seatCount);
            for (int row = 0; row < rows; row++)
                for (int j = 0; j < counts[row]; j++)
                    placed.Add((counts[row] == 1 ? Mathf.PI / 2 : Mathf.PI * (1f - (float)j / (counts[row] - 1)), row));
            foreach (var (a, row) in placed.OrderByDescending(p => Math.Round(p.angle, 5)).ThenBy(p => p.row))
            {
                var r = Radius(row, rows);
                // sin(pi) is -9e-8 in float: keep the ends on the base line.
                result.Add(new SeatPlace(new Vector2(Mathf.Cos(a) * r, Mathf.Max(0f, Mathf.Sin(a)) * r), a, row));
            }
            return result;
        }

        /// <summary>Seat centres on the unit half circle, from left to right (see <see cref="Places"/>).</summary>
        public static List<Vector2> Layout(int seatCount) => Places(seatCount).Select(p => p.Position).ToList();

        /// <summary>A dot's radius on the unit half circle: under half the gap between rows and between seats in the outer row.</summary>
        public static float DotRadius(int seatCount)
        {
            if (seatCount <= 0) return 0f;
            int rows = Rows(seatCount);
            var rowGap = rows == 1 ? 1f : (1f - Inner) / (rows - 1);
            var outer = RowCounts(seatCount)[rows - 1];
            var seatGap = outer <= 1 ? 1f : Mathf.PI / (outer - 1);
            return 0.34f * Mathf.Min(rowGap, seatGap);   // leaves room for the majority line and its stroke between dots
        }

        /// <summary>Where the majority line crosses each row (radians, inner row first; NaN for an empty row): between the
        /// row's last seat among the first <paramref name="majority"/> (counted from the left) and its next seat; half a
        /// step outside the row when the whole row is on one side.</summary>
        public static float[] Cuts(IReadOnlyList<SeatPlace> seats, int majority, int seatCount)
        {
            int rows = seatCount <= 0 ? 0 : Rows(seatCount);
            var cuts = new float[rows];
            for (int row = 0; row < rows; row++)
            {
                var inRow = seats.Select((s, i) => (s, i)).Where(x => x.s.Row == row).ToList();   // left to right
                if (inRow.Count == 0) { cuts[row] = float.NaN; continue; }
                var step = inRow.Count > 1 ? Mathf.PI / (inRow.Count - 1) : Mathf.PI;
                bool anyMajority = inRow.Any(x => x.i < majority), anyRest = inRow.Any(x => x.i >= majority);
                cuts[row] = !anyMajority ? inRow[0].s.Angle + step / 2                       // the whole row is outside the majority
                    : !anyRest ? inRow[inRow.Count - 1].s.Angle - step / 2                    // the whole row is inside it
                    : (inRow.Last(x => x.i < majority).s.Angle + inRow.First(x => x.i >= majority).s.Angle) / 2;
            }
            return cuts;
        }

        /// <summary>The majority line through the row cuts: in each row a radial step at its cut, and consecutive rows joined
        /// along the circle half way between them. Points on the unit half circle, inner edge first.</summary>
        public static List<Vector2> Boundary(IReadOnlyList<float> cuts, int seatCount)
        {
            var line = new List<Vector2>();
            if (seatCount < 2) return line;
            int rows = cuts.Count;
            var gap = rows <= 1 ? 0.5f : (1f - Inner) / (rows - 1);
            Vector2 At(float angle, float r) => new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            float? previous = null;
            for (int row = 0; row < rows; row++)
            {
                var angle = cuts[row];
                if (float.IsNaN(angle)) continue;
                var r = Radius(row, rows);
                float lo = r - gap / 2, hi = r + gap / 2;
                if (previous.HasValue)   // along the circle between the rows, from the last row's angle to this one's
                    for (int k = 1; k <= 8; k++) line.Add(At(Mathf.Lerp(previous.Value, angle, k / 8f), lo));
                else line.Add(At(angle, lo));
                line.Add(At(angle, hi));
                previous = angle;
            }
            return line;
        }

        /// <summary>The majority line's stroke, in pixels.</summary>
        public const float LineWidth = 2f;

        /// <summary>The margin round the unit half circle: the dots, and the line's ends half a row gap out.</summary>
        static float Margin(int seatCount) => Mathf.Max(DotRadius(seatCount), (1f - Inner) / Mathf.Max(1, Rows(seatCount) - 1) / 2f);

        /// <summary>Pixels per unit radius when the house is fitted into a box of <paramref name="box"/> pixels.</summary>
        public static float ScaleFor(Vector2 box, int seatCount)
        {
            var m = Margin(seatCount);
            return Mathf.Min(box.x / (2f + 2f * m), box.y / (1f + 2f * m));
        }

        void Draw(MeshGenerationContext mgc)
        {
            var box = contentRect;
            if (size == 0 || !(box.width > 8) || !(box.height > 8)) return;
            // The unit half circle plus a margin, fitted into the box and sat on its bottom edge.
            var margin = Margin(size);
            var scale = ScaleFor(box.size, size);
            var centre = new Vector2(box.center.x, box.yMax - margin * scale);
            Vector2 At(Vector2 unit) => centre + new Vector2(unit.x, -unit.y) * scale;
            var p = mgc.painter2D;
            for (int i = 0; i < size; i++)
            {
                var c = colours[i];
                p.BeginPath(); p.Arc(At(places[i].Position), dot * scale, Angle.Degrees(0), Angle.Degrees(360)); p.ClosePath();
                if (c.HasValue) { p.fillColor = c.Value; p.Fill(); }
                else { p.strokeColor = Unrecorded; p.lineWidth = 1.2f; p.Stroke(); }
            }
            if (boundary.Count < 2) return;
            p.strokeColor = Gold; p.lineWidth = LineWidth; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            p.BeginPath(); p.MoveTo(At(boundary[0]));
            for (int i = 1; i < boundary.Count; i++) p.LineTo(At(boundary[i]));
            p.Stroke();
        }
    }
}
