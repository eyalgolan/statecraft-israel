#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The promo's panel trim (promo/final/03 and 05), drawn with Painter2D over a panel: a soft shadow outside its
    /// edge, a thin gold rule inset inside the border, and a gold corner bracket with a small diamond at each corner. It is
    /// an absolute overlay that fills its parent (Statecraft.uss ".ui-frame"), takes no clicks and lays nothing out, so a
    /// panel's layout is the same with or without it. Inside a panel that clips (overflow: hidden) the shadow is clipped
    /// away and only the trim shows. In UXML: <c>&lt;Statecraft.Game.UI.UiFrame /&gt;</c> as a panel's last child, so the trim is drawn over the content (the
    /// top bar's shadow-only frame is its first child: it draws nothing inside).</summary>
    [UxmlElement]
    public partial class UiFrame : VisualElement
    {
        public const string ClassName = "ui-frame";
        static readonly Color Gold = new Color32(196, 160, 92, 255);

        /// <summary>False drops the corner brackets (a slim bar keeps only the rule and the shadow); UXML "corners".</summary>
        [UxmlAttribute] public bool Corners { get => corners; set { corners = value; MarkDirtyRepaint(); } }
        /// <summary>False drops the outer shadow; UXML "shadow".</summary>
        [UxmlAttribute] public bool Shadow { get => shadow; set { shadow = value; MarkDirtyRepaint(); } }
        /// <summary>False drops the inset gold rule; UXML "rule".</summary>
        [UxmlAttribute] public bool Rule { get => rule; set { rule = value; MarkDirtyRepaint(); } }
        bool corners = true, shadow = true, rule = true;

        public UiFrame()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = new Rect(0, 0, layout.width, layout.height);
            if (!(r.width > 8) || !(r.height > 8)) return;
            var p = mgc.painter2D;
            p.lineJoin = LineJoin.Miter;
            if (shadow)
            {
                // Rings outside the parent's 1 px border, fading out over 12 px and weighted downwards (light from above).
                for (int i = 1; i <= 12; i++)
                {
                    var t = 1f - i / 13f;
                    p.strokeColor = new Color(0f, 0f, 0f, 0.20f * t * t);
                    p.lineWidth = 1.05f;
                    var g = 1f + i;
                    StrokeRect(p, new Rect(r.x - g, r.y - g + i * 0.25f, r.width + 2 * g, r.height + 2 * g + i * 0.25f));
                }
            }
            // The inner rule.
            const float inset = 4f;
            if (rule)
            {
                p.strokeColor = new Color(Gold.r, Gold.g, Gold.b, 0.30f);
                p.lineWidth = 1f;
                StrokeRect(p, new Rect(r.x + inset, r.y + inset, r.width - 2 * inset, r.height - 2 * inset));
            }
            if (!corners) return;
            // Corner brackets over the rule, with a diamond on each corner.
            float leg = Mathf.Min(16f, Mathf.Min(r.width, r.height) / 4f), d = 2.6f;
            p.strokeColor = Gold; p.lineWidth = 1.6f; p.fillColor = Gold;
            foreach (var (cx, cy, sx, sy) in new[] { (r.xMin + inset, r.yMin + inset, 1f, 1f), (r.xMax - inset, r.yMin + inset, -1f, 1f),
                                                     (r.xMin + inset, r.yMax - inset, 1f, -1f), (r.xMax - inset, r.yMax - inset, -1f, -1f) })
            {
                p.BeginPath(); p.MoveTo(new Vector2(cx + sx * leg, cy)); p.LineTo(new Vector2(cx, cy)); p.LineTo(new Vector2(cx, cy + sy * leg)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(cx + sx * 4f, cy + sy * 2.4f)); p.LineTo(new Vector2(cx + sx * 9f, cy + sy * 2.4f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(cx + sx * 2.4f, cy + sy * 4f)); p.LineTo(new Vector2(cx + sx * 2.4f, cy + sy * 9f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(cx, cy - d)); p.LineTo(new Vector2(cx + d, cy)); p.LineTo(new Vector2(cx, cy + d)); p.LineTo(new Vector2(cx - d, cy)); p.ClosePath(); p.Fill();
            }
        }

        static void StrokeRect(Painter2D p, Rect r)
        {
            p.BeginPath(); p.MoveTo(new Vector2(r.xMin, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMax));
            p.LineTo(new Vector2(r.xMin, r.yMax)); p.ClosePath(); p.Stroke();
        }
    }
}
