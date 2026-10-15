#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A small vector icon drawn with Painter2D (no image asset), in the promo's style (promo/final/03): a gold coin
    /// for the Treasury, figures for Immigration and Public Support, a blue shield for Security, a globe for International
    /// Standing, a banknote for Foreign Currency, gold line icons for the screen buttons, and a terrain sign per biome. Each
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
            ["coin"] = Coin, ["people"] = People, ["crowd"] = Crowd, ["shield"] = Shield, ["globe"] = Globe, ["banknote"] = Banknote,
            ["columns"] = Columns, ["scales"] = Scales, ["helmet"] = Helmet, ["question"] = Question, ["restart"] = Restart,
            ["chevron"] = Chevron, ["chevron-back"] = ChevronBack, ["hourglass"] = Hourglass,
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

        /// <summary>A round navy disc with a gold rim and <paramref name="glyph"/> inside it, centred on <paramref name="centre"/>
        /// (the promo's round option and button sign).</summary>
        public static void PaintDisc(MeshGenerationContext mgc, string glyph, Vector2 centre, float radius)
        {
            var p = mgc.painter2D;
            p.fillColor = new Color32(14, 22, 38, 255); p.strokeColor = new Color32(196, 160, 92, 150); p.lineWidth = 1f;
            Circle(p, centre, radius); p.Fill(); p.Stroke();
            var inner = radius * 0.62f;
            Paint(mgc, glyph, new Rect(centre.x - inner, centre.y - inner, 2 * inner, 2 * inner));
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

        static void Restart(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 2f * s;
            p.BeginPath(); p.MoveTo(at(16.2f, 6.2f));
            p.Arc(at(12, 12.6f), 7.6f * s, Angle.Degrees(303), Angle.Degrees(573)); p.Stroke();
            p.fillColor = gold;
            p.BeginPath(); p.MoveTo(at(13.6f, 2.6f)); p.LineTo(at(20.4f, 5.4f)); p.LineTo(at(15.0f, 10.4f)); p.ClosePath(); p.Fill();
        }

        static void Chevron(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 2.2f * s;
            p.BeginPath(); p.MoveTo(at(9.6f, 6.4f)); p.LineTo(at(15.2f, 12)); p.LineTo(at(9.6f, 17.6f)); p.Stroke();
        }

        /// <summary>The chevron mirrored, pointing left: the option sign in the Hebrew UI.</summary>
        static void ChevronBack(Painter2D p, Func<float, float, Vector2> at, float s, Color gold)
        {
            p.strokeColor = gold; p.lineWidth = 2.2f * s;
            p.BeginPath(); p.MoveTo(at(14.4f, 6.4f)); p.LineTo(at(8.8f, 12)); p.LineTo(at(14.4f, 17.6f)); p.Stroke();
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
