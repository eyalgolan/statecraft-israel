#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A small vector icon drawn with Painter2D (no image asset), in the promo's style (promo/final/03): a gold coin
    /// for the Treasury, figures for Immigration and Public Support, a blue shield for Security, a globe for International
    /// Standing, a banknote for Foreign Currency, two linked rings for Social cohesion, gold line icons for the screen buttons, a star and the GitHub mark for "Star on GitHub" (#1168), a speech bubble and the share sign for the map screen's Report and Share icons (#1236), and a terrain sign per biome. Each
    /// glyph is drawn in a 24 x 24 design box, scaled to the element's content box and centred in it. In UXML:
    /// <c>&lt;Statecraft.Game.UI.UiIcon glyph="coin" /&gt;</c>. An unknown glyph draws nothing.</summary>
    [UxmlElement]
    public partial class UiIcon : VisualElement
    {
        /// <summary>Every glyph this element can draw (PromoHudTests.EveryGlyphDraws paints each one).</summary>
        public static IReadOnlyCollection<string> Glyphs => Painters.Keys;

        string glyph = "";

        /// <summary>The glyph's name (see <see cref="Glyphs"/>); the UXML attribute "glyph".</summary>
        [UxmlAttribute]
        public string Glyph { get => glyph; set { glyph = value ?? ""; MarkDirtyRepaint(); } }

        public UiIcon()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        // The palette: the promo's gold, beige figures, blue shield and globe, and a sage banknote.
        static readonly Color GoldBase = new Color32(214, 176, 98, 255), GoldDark = new Color32(132, 96, 44, 255),
            GoldLight = new Color32(244, 214, 146, 255), Beige = new Color32(226, 206, 166, 255), BeigeDark = new Color32(176, 154, 116, 255),
            Blue = new Color32(92, 138, 200, 255), BlueLight = new Color32(206, 224, 246, 255), Sage = new Color32(126, 168, 120, 255),
            SageLight = new Color32(214, 236, 204, 255), Navy = new Color32(14, 22, 38, 200);

        /// <summary>Draws one glyph; <c>gold</c> is the line colour of the gold glyphs (plain, or lit while focused).</summary>
        delegate void Painter(Painter2D p, Func<float, float, Vector2> at, float s, Color gold);

        static readonly Dictionary<string, Painter> Painters = new Dictionary<string, Painter>
        {
            ["coin"] = Coin, ["people"] = People, ["crowd"] = Crowd, ["shield"] = Shield, ["globe"] = Globe, ["banknote"] = Banknote, ["rings"] = Rings,
            ["columns"] = Columns, ["scales"] = Scales, ["helmet"] = Helmet, ["question"] = Question, ["book"] = Book, ["restart"] = Restart, ["gear"] = Gear,
            ["hourglass"] = Hourglass, ["star"] = Star, ["mark-github"] = MarkGithub, ["speech"] = Speech, ["share"] = Share,
            ["region"] = Region, ["house"] = House, ["village"] = Village, ["flag"] = Flag,
            ["peaks"] = Peaks, ["skyline"] = Skyline, ["ledger"] = Ledger,
            ["biome-sea"] = Waves, ["biome-lake"] = Waves, ["biome-coastal_plain"] = Plain, ["biome-hills"] = Hills,
            ["biome-mountains"] = Mountains, ["biome-valley"] = Valley, ["biome-lowland"] = Plain, ["biome-desert"] = Dunes,
        };

        /// <summary>The gold glyphs' line colour, plain or lit (PromoHudTests checks lit stays >= 3:1 on the focus fill).</summary>
        public static Color LineColour(bool lit) => lit ? GoldLight : GoldBase;

        /// <summary>True when <paramref name="e"/>, or the Button it sits in, has the keyboard focus. Focus changes the element's
        /// style (Statecraft.uss), which repaints it, so a glyph that reads this is redrawn when focus comes or goes.</summary>
        public static bool IsFocused(VisualElement e)
        {
            var b = e as Button ?? e.GetFirstAncestorOfType<Button>();
            return b != null && b.focusController?.focusedElement == b;
        }

        /// <summary>Lit while its Button has the keyboard focus.</summary>
        public bool Lit => IsFocused(this);

        void Draw(MeshGenerationContext mgc) => Paint(mgc, glyph, contentRect, Lit);

        /// <summary>Draws <paramref name="glyph"/> centred in <paramref name="box"/> (the element's local coordinates). For a
        /// text element (a Button) the sign is painted into the element itself rather than added as a child: a child would
        /// make it a non-leaf node, and Yoga measures only leaf nodes' text, so the button would stop growing to fit its text.</summary>
        public static void Paint(MeshGenerationContext mgc, string glyph, Rect box, bool lit = false)
        {
            if (!Painters.TryGetValue(glyph, out var paint)) return;
            if (!(box.width > 0) || !(box.height > 0)) return;
            var s = Mathf.Min(box.width, box.height) / 24f;
            var origin = new Vector2(box.x + (box.width - 24f * s) / 2f, box.y + (box.height - 24f * s) / 2f);
            var p = mgc.painter2D;
            p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Round;
            paint(p, (x, y) => origin + new Vector2(x, y) * s, s, LineColour(lit));
        }

        // --- helpers (design-box coordinates) ---
        static void Circle(Painter2D p, Vector2 c, float r) { p.BeginPath(); p.Arc(c, r, Angle.Degrees(0), Angle.Degrees(360)); p.ClosePath(); }

        static void Ellipse(Painter2D p, Vector2 c, float rx, float ry)
        {
            const float k = 0.5523f;   // cubic Bezier quarter-circle constant
            p.BeginPath();
            p.MoveTo(c + new Vector2(rx, 0));
            p.BezierCurveTo(c + new Vector2(rx, ry * k), c + new Vector2(rx * k, ry), c + new Vector2(0, ry));
            p.BezierCurveTo(c + new Vector2(-rx * k, ry), c + new Vector2(-rx, ry * k), c + new Vector2(-rx, 0));
            p.BezierCurveTo(c + new Vector2(-rx, -ry * k), c + new Vector2(-rx * k, -ry), c + new Vector2(0, -ry));
            p.BezierCurveTo(c + new Vector2(rx * k, -ry), c + new Vector2(rx, -ry * k), c + new Vector2(rx, 0));
            p.ClosePath();
        }

        static void Rect(Painter2D p, Func<float, float, Vector2> at, float x, float y, float w, float h)
        {
            p.BeginPath(); p.MoveTo(at(x, y)); p.LineTo(at(x + w, y)); p.LineTo(at(x + w, y + h)); p.LineTo(at(x, y + h)); p.ClosePath();
        }

        /// <summary>A head and rounded shoulders, centred on cx, scaled by k from a 12-unit-tall figure standing on y=21.</summary>
        static void Figure(Painter2D p, Func<float, float, Vector2> at, float s, float cx, float k, Color fill, Color edge)
        {
            p.fillColor = fill; p.strokeColor = edge; p.lineWidth = 0.9f * s;
            Circle(p, at(cx, 21 - 13.2f * k), 3.1f * k * s); p.Fill(); p.Stroke();
            p.BeginPath();
            p.MoveTo(at(cx - 5.4f * k, 21)); p.LineTo(at(cx - 5.4f * k, 21 - 4.6f * k));
            p.Arc(at(cx, 21 - 4.6f * k), 5.4f * k * s, Angle.Degrees(180), Angle.Degrees(360));
            p.LineTo(at(cx + 5.4f * k, 21)); p.ClosePath(); p.Fill(); p.Stroke();
        }

        // --- the indicator icons ---
        static void Coin(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = GoldDark; p.strokeColor = new Color32(96, 68, 30, 255); p.lineWidth = 0.9f * s;
            Circle(p, at(14.5f, 14.5f), 7.4f * s); p.Fill(); p.Stroke();
            p.fillColor = gold;
            Circle(p, at(10.5f, 10.5f), 8.4f * s); p.Fill(); p.Stroke();
            p.strokeColor = GoldLight; p.lineWidth = 0.9f * s;
            Circle(p, at(10.5f, 10.5f), 5.8f * s); p.Stroke();
        }

        static void People(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            Figure(p, at, s, 15.2f, 0.92f, BeigeDark, Navy);
            Figure(p, at, s, 8.8f, 1f, Beige, Navy);
        }

        static void Crowd(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            Figure(p, at, s, 5.6f, 0.78f, BeigeDark, Navy);
            Figure(p, at, s, 18.4f, 0.78f, BeigeDark, Navy);
            Figure(p, at, s, 12f, 0.95f, Beige, Navy);
        }

        static void ShieldPath(Painter2D p, Func<float, float, Vector2> at)
        {
            p.BeginPath();
            p.MoveTo(at(12, 2.2f)); p.LineTo(at(20.2f, 5.2f)); p.LineTo(at(20.2f, 11));
            p.BezierCurveTo(at(20.2f, 16.6f), at(16.6f, 20.2f), at(12, 22.2f));
            p.BezierCurveTo(at(7.4f, 20.2f), at(3.8f, 16.6f), at(3.8f, 11));
            p.LineTo(at(3.8f, 5.2f)); p.ClosePath();
        }

        static void Shield(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            ShieldPath(p, at); p.fillColor = Blue; p.Fill();
            p.strokeColor = BlueLight; p.lineWidth = 1.3f * s;
            p.BeginPath(); p.MoveTo(at(12, 3.6f)); p.LineTo(at(12, 20.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(4.6f, 10.6f)); p.LineTo(at(19.4f, 10.6f)); p.Stroke();
            ShieldPath(p, at); p.lineWidth = 1.1f * s; p.Stroke();
        }

        static void Globe(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = new Color(Blue.r, Blue.g, Blue.b, 0.22f);
            Circle(p, at(12, 12), 9.2f * s); p.Fill();
            p.strokeColor = Blue; p.lineWidth = 1.5f * s;
            Circle(p, at(12, 12), 9.2f * s); p.Stroke();
            p.lineWidth = 1.1f * s;
            Ellipse(p, at(12, 12), 4.2f * s, 9.2f * s); p.Stroke();
            p.BeginPath(); p.MoveTo(at(12, 2.8f)); p.LineTo(at(12, 21.2f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(2.8f, 12)); p.LineTo(at(21.2f, 12)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(4.4f, 7.4f)); p.LineTo(at(19.6f, 7.4f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(4.4f, 16.6f)); p.LineTo(at(19.6f, 16.6f)); p.Stroke();
        }

        /// <summary>Social cohesion (#380): two linked gold rings, each drawn over a dark edge so it reads on the bar.</summary>
        static void Rings(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            foreach (var (x, colour) in new[] { (8.6f, GoldBase), (15.4f, GoldLight) })
            {
                p.strokeColor = GoldDark; p.lineWidth = 3.4f * s;
                Circle(p, at(x, 12), 5.8f * s); p.Stroke();
                p.strokeColor = colour; p.lineWidth = 2f * s;
                Circle(p, at(x, 12), 5.8f * s); p.Stroke();
            }
        }

        static void Banknote(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = new Color32(84, 120, 82, 255); p.strokeColor = SageLight; p.lineWidth = 0.9f * s;
            Rect(p, at, 4.5f, 4.2f, 17, 11); p.Fill(); p.Stroke();
            p.fillColor = Sage;
            Rect(p, at, 2.5f, 7.8f, 17, 11); p.Fill(); p.Stroke();
            p.strokeColor = SageLight; p.lineWidth = 1f * s;
            Circle(p, at(11, 13.3f), 3f * s); p.Stroke();
            p.BeginPath(); p.MoveTo(at(4.8f, 10.2f)); p.LineTo(at(6.4f, 10.2f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(15.6f, 16.4f)); p.LineTo(at(17.2f, 16.4f)); p.Stroke();
        }

        // --- the screen buttons (gold line art, as the promo's round buttons) ---
        static void Columns(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = gold;
            p.BeginPath(); p.MoveTo(at(2.5f, 8)); p.LineTo(at(12, 2.6f)); p.LineTo(at(21.5f, 8)); p.ClosePath(); p.Fill();
            Rect(p, at, 3, 8.8f, 18, 1.6f); p.Fill();
            foreach (var x in new[] { 4.6f, 9.1f, 13.3f, 17.8f }) { Rect(p, at, x, 11.2f, 1.6f, 7); p.Fill(); }
            Rect(p, at, 2.6f, 18.8f, 18.8f, 1.5f); p.Fill();
            Rect(p, at, 1.6f, 20.9f, 20.8f, 1.5f); p.Fill();
        }

        static void Scales(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.fillColor = gold; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(12, 4.6f)); p.LineTo(at(12, 19.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(3.6f, 7)); p.LineTo(at(20.4f, 7)); p.Stroke();
            Circle(p, at(12, 4.2f), 1.5f * s); p.Fill();
            Rect(p, at, 7.2f, 19.6f, 9.6f, 1.8f); p.Fill();
            foreach (var cx in new[] { 4.6f, 19.4f })
            {
                p.lineWidth = 0.9f * s;
                p.BeginPath(); p.MoveTo(at(cx, 7)); p.LineTo(at(cx - 3.1f, 13.4f)); p.Stroke();
                p.BeginPath(); p.MoveTo(at(cx, 7)); p.LineTo(at(cx + 3.1f, 13.4f)); p.Stroke();
                p.BeginPath(); p.MoveTo(at(cx + 3.6f, 13.4f));
                p.Arc(at(cx, 13.4f), 3.6f * s, Angle.Degrees(0), Angle.Degrees(180)); p.ClosePath(); p.Fill();
            }
        }

        static void Helmet(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = gold;
            p.BeginPath(); p.MoveTo(at(4.2f, 15.4f)); p.Arc(at(12, 15.4f), 7.8f * s, Angle.Degrees(180), Angle.Degrees(360)); p.ClosePath(); p.Fill();
            p.BeginPath(); p.MoveTo(at(1.8f, 15.6f)); p.LineTo(at(22.2f, 15.6f)); p.LineTo(at(20.6f, 18.4f)); p.LineTo(at(3.4f, 18.4f)); p.ClosePath(); p.Fill();
            p.strokeColor = GoldDark; p.lineWidth = 1f * s;
            p.BeginPath(); p.MoveTo(at(7.2f, 13.6f)); p.Arc(at(12, 13.6f), 4.8f * s, Angle.Degrees(180), Angle.Degrees(270)); p.Stroke();
        }

        static void Question(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 2.3f * s;
            p.BeginPath(); p.MoveTo(at(7.4f, 8.6f));
            p.Arc(at(12, 8.6f), 4.6f * s, Angle.Degrees(180), Angle.Degrees(405));
            p.LineTo(at(12, 14.6f)); p.LineTo(at(12, 15.8f)); p.Stroke();
            p.fillColor = gold; Circle(p, at(12, 20), 1.6f * s); p.Fill();
        }

        /// <summary>An open book (How to play, #1041): two pages from a middle spine, two text lines on each, in gold line, so
        /// its disc is not Why?'s question mark.</summary>
        static void Book(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.6f * s;
            p.BeginPath(); p.MoveTo(at(12, 6.4f)); p.LineTo(at(12, 19.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(12, 6.4f)); p.BezierCurveTo(at(9.4f, 4.8f), at(6, 4.6f), at(3, 5.6f)); p.LineTo(at(3, 18.8f));
            p.BezierCurveTo(at(6, 17.8f), at(9.4f, 18f), at(12, 19.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(12, 6.4f)); p.BezierCurveTo(at(14.6f, 4.8f), at(18, 4.6f), at(21, 5.6f)); p.LineTo(at(21, 18.8f));
            p.BezierCurveTo(at(18, 17.8f), at(14.6f, 18f), at(12, 19.6f)); p.Stroke();
            p.lineWidth = 1f * s;
            foreach (var y in new[] { 10f, 13.6f })
            {
                p.BeginPath(); p.MoveTo(at(5.4f, y)); p.LineTo(at(9.8f, y + 0.6f)); p.Stroke();
                p.BeginPath(); p.MoveTo(at(14.2f, y + 0.6f)); p.LineTo(at(18.6f, y)); p.Stroke();
            }
        }

        static void Restart(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 2f * s;
            p.BeginPath(); p.MoveTo(at(16.2f, 6.2f));
            p.Arc(at(12, 12.6f), 7.6f * s, Angle.Degrees(303), Angle.Degrees(573)); p.Stroke();
            p.fillColor = gold;
            p.BeginPath(); p.MoveTo(at(13.6f, 2.6f)); p.LineTo(at(20.4f, 5.4f)); p.LineTo(at(15.0f, 10.4f)); p.ClosePath(); p.Fill();
        }

        /// <summary>A cog (the Settings screen, #40): eight teeth round a ring and a hole, in gold line. Each tooth is
        /// GearTooth: (degrees from the tooth's middle, radius) pairs, root, tip, tip, root.</summary>
        static readonly (float deg, float r)[] GearTooth = { (-13f, 7f), (-7f, 9.8f), (7f, 9.8f), (13f, 7f) };

        static void Gear(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.8f * s;
            p.BeginPath();
            for (int i = 0; i < 8; i++)
                foreach (var (deg, r) in GearTooth)
                {
                    float a = (i * 45f + deg) * Mathf.Deg2Rad;
                    var v = at(12 + r * Mathf.Cos(a), 12 + r * Mathf.Sin(a));
                    if (i == 0 && deg == GearTooth[0].deg) p.MoveTo(v); else p.LineTo(v);
                }
            p.ClosePath(); p.Stroke();
            Circle(p, at(12, 12), 3.2f * s); p.Stroke();
        }

        static void Hourglass(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.5f * s;
            p.BeginPath(); p.MoveTo(at(6, 3)); p.LineTo(at(18, 3)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(6, 21)); p.LineTo(at(18, 21)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(7.4f, 3)); p.BezierCurveTo(at(7.4f, 9), at(11, 10.4f), at(11, 12)); p.BezierCurveTo(at(11, 13.6f), at(7.4f, 15), at(7.4f, 21)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(16.6f, 3)); p.BezierCurveTo(at(16.6f, 9), at(13, 10.4f), at(13, 12)); p.BezierCurveTo(at(13, 13.6f), at(16.6f, 15), at(16.6f, 21)); p.Stroke();
            p.fillColor = gold;
            p.BeginPath(); p.MoveTo(at(8.6f, 20)); p.LineTo(at(12, 16.4f)); p.LineTo(at(15.4f, 20)); p.ClosePath(); p.Fill();
        }

        // --- the start screen's "Star on GitHub" signs (#1168): Octicons star-fill-24 and mark-github-24 (GitHub's Primer
        // icons, primer/octicons at 6dbe38e4, MIT, THIRD-PARTY-NOTICES.md and LICENSES/Octicons-MIT.txt). Each SVG path is
        // one closed outline in the same 24 x 24 box, written out as absolute cubic segments (its lines and its corner arcs
        // as cubics), so Painter2D fills it without an SVG parser. Filled in the gold glyphs' colour.
        static void Star(Painter2D p, Func<float, float, Vector2> at, float s, Color gold) => FillCubics(p, at, StarPath, gold);

        /// <summary>A speech bubble with two lines of text in it (Report a bug or idea, #1236), in gold line: a rounded box with
        /// a tail at its lower left.</summary>
        static void Speech(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.8f * s;
            const float l = 3.2f, t = 4.2f, r = 20.8f, b = 16.4f, k = 2.6f;   // the box and its corner radius
            p.BeginPath();
            p.MoveTo(at(l + k, t)); p.LineTo(at(r - k, t)); p.Arc(at(r - k, t + k), k * s, Angle.Degrees(270), Angle.Degrees(360));
            p.LineTo(at(r, b - k)); p.Arc(at(r - k, b - k), k * s, Angle.Degrees(0), Angle.Degrees(90));
            p.LineTo(at(10.4f, b)); p.LineTo(at(6.2f, 20.4f)); p.LineTo(at(6.8f, b)); p.LineTo(at(l + k, b));
            p.Arc(at(l + k, b - k), k * s, Angle.Degrees(90), Angle.Degrees(180));
            p.LineTo(at(l, t + k)); p.Arc(at(l + k, t + k), k * s, Angle.Degrees(180), Angle.Degrees(270));
            p.ClosePath(); p.Stroke();
            p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(7.4f, 8.6f)); p.LineTo(at(16.6f, 8.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(7.4f, 12.2f)); p.LineTo(at(13.6f, 12.2f)); p.Stroke();
        }

        /// <summary>The share sign (Share, #1236): three gold nodes, one on the left and two on the right, joined by two lines.</summary>
        static void Share(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.8f * s;
            p.BeginPath(); p.MoveTo(at(17.4f, 5.8f)); p.LineTo(at(6.6f, 12f)); p.LineTo(at(17.4f, 18.2f)); p.Stroke();
            p.fillColor = gold;
            foreach (var (x, y) in new[] { (17.6f, 5.6f), (6.4f, 12f), (17.6f, 18.4f) }) { Circle(p, at(x, y), 3.1f * s); p.Fill(); }
        }

        static void MarkGithub(Painter2D p, Func<float, float, Vector2> at, float s, Color gold) => FillCubics(p, at, MarkGithubPath, gold);

        /// <summary>Fills the outline <paramref name="d"/>: a start point, then (control 1, control 2, end) per segment.</summary>
        static void FillCubics(Painter2D p, Func<float, float, Vector2> at, float[] d, Color fill)
        {
            p.fillColor = fill;
            p.BeginPath(); p.MoveTo(at(d[0], d[1]));
            for (int i = 2; i + 5 < d.Length; i += 6) p.BezierCurveTo(at(d[i], d[i + 1]), at(d[i + 2], d[i + 3]), at(d[i + 4], d[i + 5]));
            p.ClosePath(); p.Fill();
        }

        static readonly float[] StarPath =
        {
            12.672f, 0.668f,
            13.692f, 2.734f, 14.711f, 4.799f, 15.731f, 6.865f,
            18.01f, 7.196f, 20.29f, 7.527f, 22.569f, 7.858f,
            22.852f, 7.899f, 23.087f, 8.097f, 23.175f, 8.369f,
            23.264f, 8.64f, 23.19f, 8.939f, 22.985f, 9.138f,
            21.336f, 10.746f, 19.686f, 12.353f, 18.037f, 13.961f,
            18.426f, 16.232f, 18.816f, 18.502f, 19.205f, 20.773f,
            19.253f, 21.054f, 19.137f, 21.338f, 18.906f, 21.506f,
            18.675f, 21.673f, 18.37f, 21.696f, 18.117f, 21.563f,
            16.078f, 20.491f, 14.039f, 19.419f, 12f, 18.347f,
            9.961f, 19.419f, 7.923f, 20.491f, 5.884f, 21.563f,
            5.631f, 21.696f, 5.325f, 21.674f, 5.094f, 21.506f,
            4.863f, 21.338f, 4.748f, 21.053f, 4.796f, 20.772f,
            5.185f, 18.502f, 5.575f, 16.231f, 5.964f, 13.961f,
            4.315f, 12.353f, 2.665f, 10.746f, 1.016f, 9.138f,
            0.811f, 8.939f, 0.737f, 8.641f, 0.825f, 8.369f,
            0.914f, 8.097f, 1.149f, 7.899f, 1.432f, 7.859f,
            3.711f, 7.528f, 5.991f, 7.196f, 8.27f, 6.865f,
            9.289f, 4.799f, 10.308f, 2.734f, 11.327f, 0.668f,
            11.453f, 0.412f, 11.714f, 0.25f, 12f, 0.25f,
            12.285f, 0.25f, 12.546f, 0.412f, 12.672f, 0.668f
        };

        static readonly float[] MarkGithubPath =
        {
            10.226f, 17.284f,
            7.261f, 16.924f, 5.172f, 14.791f, 5.172f, 12.028f,
            5.172f, 10.905f, 5.576f, 9.692f, 6.25f, 8.884f,
            5.958f, 8.143f, 6.003f, 6.57f, 6.34f, 5.919f,
            7.238f, 5.807f, 8.451f, 6.279f, 9.17f, 6.929f,
            10.023f, 6.66f, 10.922f, 6.525f, 12.023f, 6.525f,
            13.123f, 6.525f, 14.022f, 6.66f, 14.83f, 6.907f,
            15.526f, 6.278f, 16.762f, 5.807f, 17.66f, 5.919f,
            17.975f, 6.525f, 18.02f, 8.098f, 17.727f, 8.861f,
            18.447f, 9.715f, 18.828f, 10.861f, 18.828f, 12.028f,
            18.828f, 14.791f, 16.739f, 16.88f, 13.73f, 17.262f,
            14.493f, 17.756f, 15.01f, 18.834f, 15.01f, 20.069f,
            15.01f, 20.848f, 15.01f, 21.626f, 15.01f, 22.405f,
            15.01f, 23.079f, 15.571f, 23.461f, 16.245f, 23.191f,
            20.311f, 21.641f, 23.5f, 17.576f, 23.5f, 12.545f,
            23.5f, 6.188f, 18.334f, 1f, 11.978f, 1f,
            5.62f, 1f, 0.5f, 6.188f, 0.5f, 12.545f,
            0.5f, 17.531f, 3.667f, 21.665f, 7.935f, 23.214f,
            8.541f, 23.439f, 9.125f, 23.034f, 9.125f, 22.428f,
            9.125f, 21.829f, 9.125f, 21.229f, 9.125f, 20.63f,
            8.783f, 20.773f, 8.417f, 20.849f, 8.047f, 20.854f,
            6.564f, 20.854f, 5.688f, 20.046f, 5.06f, 18.541f,
            4.813f, 17.934f, 4.543f, 17.575f, 4.026f, 17.508f,
            3.756f, 17.485f, 3.667f, 17.373f, 3.667f, 17.238f,
            3.667f, 16.968f, 4.117f, 16.767f, 4.565f, 16.767f,
            5.217f, 16.767f, 5.778f, 17.171f, 6.362f, 18.002f,
            6.812f, 18.653f, 7.283f, 18.945f, 7.845f, 18.945f,
            8.406f, 18.945f, 8.765f, 18.743f, 9.282f, 18.226f,
            9.664f, 17.845f, 9.956f, 17.508f, 10.226f, 17.283f
        };

        // --- the info panel's fact rows (gold line art, as the promo's row signs; HUD round 1) ---
        /// <summary>The region row: a folded map of three panels.</summary>
        static void Region(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(3, 6)); p.LineTo(at(9, 3.6f)); p.LineTo(at(15, 6)); p.LineTo(at(21, 3.6f)); p.LineTo(at(21, 18.4f));
            p.LineTo(at(15, 20.8f)); p.LineTo(at(9, 18.4f)); p.LineTo(at(3, 20.8f)); p.ClosePath(); p.Stroke();
            p.lineWidth = 1f * s;
            p.BeginPath(); p.MoveTo(at(9, 3.6f)); p.LineTo(at(9, 18.4f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(15, 6)); p.LineTo(at(15, 20.8f)); p.Stroke();
        }

        /// <summary>The minimap's whole-map button (promo 03): two peaks in outline, the far one behind, with a snow line.</summary>
        static void Peaks(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(1.8f, 20)); p.LineTo(at(9, 5.2f)); p.LineTo(at(16.2f, 20)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(12.6f, 12.6f)); p.LineTo(at(15.4f, 8.2f)); p.LineTo(at(22.2f, 20)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(1.8f, 20)); p.LineTo(at(22.2f, 20)); p.Stroke();
            p.lineWidth = 1f * s;
            p.BeginPath(); p.MoveTo(at(6.6f, 10.2f)); p.LineTo(at(8.2f, 11.4f)); p.LineTo(at(9.6f, 9.8f)); p.LineTo(at(11.4f, 10.2f)); p.Stroke();
        }

        /// <summary>The minimap's settlement-names button (promo 03): a town's skyline, three towers of different heights.</summary>
        static void Skyline(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(3, 20.6f)); p.LineTo(at(3, 9)); p.LineTo(at(8.4f, 9)); p.LineTo(at(8.4f, 20.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(8.4f, 20.6f)); p.LineTo(at(8.4f, 4.4f)); p.LineTo(at(14.6f, 4.4f)); p.LineTo(at(14.6f, 20.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(14.6f, 20.6f)); p.LineTo(at(14.6f, 12)); p.LineTo(at(21, 12)); p.LineTo(at(21, 20.6f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(1.6f, 20.6f)); p.LineTo(at(22.4f, 20.6f)); p.Stroke();
            p.fillColor = gold;
            foreach (var y in new[] { 7.4f, 11f, 14.6f }) { Rect(p, at, 10.4f, y, 2.2f, 1.8f); p.Fill(); }
            Rect(p, at, 4.6f, 12f, 2.2f, 1.8f); p.Fill();
            Rect(p, at, 16.6f, 15f, 2.2f, 1.8f); p.Fill();
        }

        /// <summary>The Budget screen's button (owner, 2026-10-15, as promo 03's column): a ruled ledger with a curl at the top
        /// and the bottom, its entries in three panels.</summary>
        static void Ledger(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(6, 4.6f)); p.LineTo(at(6, 19.4f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(18, 4.6f)); p.LineTo(at(18, 19.4f)); p.Stroke();
            foreach (var y in new[] { 3.4f, 20.6f })
            {
                p.BeginPath(); p.MoveTo(at(5, y)); p.LineTo(at(19.6f, y)); p.Stroke();
                p.fillColor = gold; Circle(p, at(4.4f, y), 1.3f * s); p.Fill();
            }
            p.lineWidth = 1f * s;
            foreach (var y in new[] { 8.2f, 16 }) { p.BeginPath(); p.MoveTo(at(6, y)); p.LineTo(at(18, y)); p.Stroke(); }
            p.fillColor = gold;
            foreach (var (x, y, w) in new[] { (7.6f, 5.2f, 3.6f), (12.6f, 5.2f, 3.8f), (7.6f, 9.8f, 2.6f), (11.4f, 9.8f, 5f), (7.6f, 12.4f, 4f), (12.8f, 12.4f, 3.6f), (7.6f, 17.6f, 3.4f), (12.4f, 17.6f, 4f) })
            { Rect(p, at, x, y, w, 1.6f); p.Fill(); }
        }

        /// <summary>The settlements row: a house.</summary>
        static void House(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(2.6f, 11.4f)); p.LineTo(at(12, 3.4f)); p.LineTo(at(21.4f, 11.4f)); p.Stroke();
            p.BeginPath(); p.MoveTo(at(5.4f, 9.6f)); p.LineTo(at(5.4f, 20.6f)); p.LineTo(at(18.6f, 20.6f)); p.LineTo(at(18.6f, 9.6f)); p.Stroke();
            p.fillColor = gold; Rect(p, at, 10.4f, 14.4f, 3.2f, 6.2f); p.Fill();
        }

        /// <summary>The 1948 localities row: two small flat-roofed houses, one behind the other.</summary>
        static void Village(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = new Color(gold.r, gold.g, gold.b, 0.55f);
            Rect(p, at, 11.6f, 6.8f, 9.4f, 8.6f); p.Fill();
            p.fillColor = gold;
            Rect(p, at, 3, 10.6f, 11, 9.8f); p.Fill();
            p.fillColor = Navy;
            Rect(p, at, 7.4f, 15.2f, 2.4f, 5.2f); p.Fill();
            Rect(p, at, 15.4f, 9.4f, 2, 2.2f); p.Fill();
        }

        /// <summary>The placement row (an order or a built placement): a flag on a pole.</summary>
        static void Flag(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 1.5f * s;
            p.BeginPath(); p.MoveTo(at(6.4f, 21.4f)); p.LineTo(at(6.4f, 2.8f)); p.Stroke();
            p.fillColor = gold;
            p.BeginPath(); p.MoveTo(at(7.2f, 3.6f)); p.LineTo(at(19.6f, 7.6f)); p.LineTo(at(7.2f, 11.6f)); p.ClosePath(); p.Fill();
        }

        // --- terrain signs for the info panel (drawn dark on the biome's own map colour) ---
        static readonly Color Ink = new Color32(14, 22, 38, 170), InkLight = new Color32(255, 250, 236, 220);   // waves at >= 3:1 on sea and lake

        static void Waves(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = InkLight; p.lineWidth = 1.6f * s;
            foreach (var y in new[] { 8f, 13f, 18f })
            {
                p.BeginPath(); p.MoveTo(at(3, y));
                p.BezierCurveTo(at(5.5f, y - 2.6f), at(7, y - 2.6f), at(9, y)); p.BezierCurveTo(at(11, y + 2.6f), at(13, y + 2.6f), at(15, y));
                p.BezierCurveTo(at(17, y - 2.6f), at(19, y - 2.6f), at(21, y)); p.Stroke();
            }
        }

        static void Plain(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = Ink; p.lineWidth = 1.4f * s;
            p.BeginPath(); p.MoveTo(at(2.5f, 17)); p.LineTo(at(21.5f, 17)); p.Stroke();
            foreach (var x in new[] { 6f, 12f, 18f })
            {
                p.BeginPath(); p.MoveTo(at(x, 17)); p.LineTo(at(x, 10.5f)); p.Stroke();
                p.BeginPath(); p.MoveTo(at(x, 13.4f)); p.LineTo(at(x - 2.2f, 11.2f)); p.Stroke();
                p.BeginPath(); p.MoveTo(at(x, 12.2f)); p.LineTo(at(x + 2.2f, 10f)); p.Stroke();
            }
        }

        static void Hills(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = Ink;
            p.BeginPath(); p.MoveTo(at(1.5f, 19)); p.BezierCurveTo(at(4, 9.5f), at(10, 9.5f), at(12.5f, 15)); p.BezierCurveTo(at(14.5f, 11), at(20, 11), at(22.5f, 19)); p.ClosePath(); p.Fill();
        }

        static void Mountains(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = Ink;
            p.BeginPath(); p.MoveTo(at(1.5f, 20)); p.LineTo(at(9, 5)); p.LineTo(at(13, 12)); p.LineTo(at(16, 8.4f)); p.LineTo(at(22.5f, 20)); p.ClosePath(); p.Fill();
            p.fillColor = InkLight;
            p.BeginPath(); p.MoveTo(at(9, 5)); p.LineTo(at(11.2f, 9.4f)); p.LineTo(at(9.6f, 8.6f)); p.LineTo(at(8.2f, 9.8f)); p.LineTo(at(7.2f, 8.6f)); p.ClosePath(); p.Fill();
        }

        static void Valley(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = Ink;
            p.BeginPath(); p.MoveTo(at(1.5f, 6)); p.BezierCurveTo(at(6, 7), at(9, 15), at(12, 17)); p.BezierCurveTo(at(15, 15), at(18, 7), at(22.5f, 6)); p.LineTo(at(22.5f, 20)); p.LineTo(at(1.5f, 20)); p.ClosePath(); p.Fill();
            p.strokeColor = InkLight; p.lineWidth = 1.3f * s;
            p.BeginPath(); p.MoveTo(at(12, 17.4f)); p.BezierCurveTo(at(10.5f, 18.6f), at(13.5f, 19.2f), at(12, 20)); p.Stroke();
        }

        static void Dunes(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.fillColor = Ink;
            p.BeginPath(); p.MoveTo(at(1.5f, 20)); p.BezierCurveTo(at(5, 12), at(9, 11), at(14, 16)); p.BezierCurveTo(at(17, 13), at(20, 13), at(22.5f, 15)); p.LineTo(at(22.5f, 20)); p.ClosePath(); p.Fill();
            p.fillColor = InkLight;
            Circle(p, at(17.5f, 6.5f), 2.4f * s); p.Fill();
        }
    }
}
