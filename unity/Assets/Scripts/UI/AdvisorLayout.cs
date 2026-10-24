#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    public enum HudTier { OneRow, TwoRow, ThreeRow }

    /// <summary>Where the advisor popup goes (spec 7.1), in panel px. One and two rows: the right column's upper part,
    /// 400 px wide, its top under the "Decision waiting" row whether or not it shows (so it never jumps), its foot 12 px
    /// above the minimap. An upright phone (three rows): a sheet from the left column's edge to the window's right edge,
    /// under "Decision waiting" and the toast, its foot 12 px above the higher of the info panel and the minimap's label.
    /// Hebrew: the same place (the HUD does not mirror its panels, Statecraft.uss:943-946).</summary>
    public static class AdvisorLayout
    {
        public const float Width = 400f, EdgePx = 16f, GapPx = 12f, DecisionRowPx = 46f, BelowPx = 8f;

        public static HudTier Tier(VisualElement hudRoot) =>
            hudRoot.ClassListContains(HudPresenter.ThreeRowClass) ? HudTier.ThreeRow
            : hudRoot.ClassListContains(HudPresenter.TwoRowClass) ? HudTier.TwoRow : HudTier.OneRow;

        public static Rect Slot(HudTier tier, Rect window, Rect rightColumn, Rect leftColumn, Rect minimap,
            Rect? decisionWaiting, Rect? toast, Rect? infoPanel, Rect? mapLabel)
        {
            float right = window.xMax - EdgePx;
            if (tier != HudTier.ThreeRow)
                return Rect.MinMaxRect(right - Width, rightColumn.yMin + DecisionRowPx + BelowPx, right, minimap.yMin - GapPx);
            float top = rightColumn.yMin;
            if (decisionWaiting is Rect d) top = Mathf.Max(top, d.yMax + BelowPx);
            if (toast is Rect t) top = Mathf.Max(top, t.yMax + BelowPx);
            float floor = minimap.yMin;
            if (mapLabel is Rect m) floor = Mathf.Min(floor, m.yMin);
            if (infoPanel is Rect i) floor = Mathf.Min(floor, i.yMin);
            return Rect.MinMaxRect(leftColumn.xMax + GapPx, top, right, floor - GapPx);
        }

        /// <summary>The popup's rectangle in its slot: at the top, or at the foot when that keeps it off
        /// <paramref name="avoid"/> (Show me's target, spec 7.3); never taller than the slot.</summary>
        public static Rect Place(Rect slot, float height, Rect? avoid)
        {
            float h = Mathf.Max(0f, Mathf.Min(height, slot.height));
            var top = new Rect(slot.xMin, slot.yMin, slot.width, h);
            if (!(avoid is Rect a) || !top.Overlaps(a)) return top;
            var foot = new Rect(slot.xMin, slot.yMax - h, slot.width, h);
            return foot.Overlaps(a) ? top : foot;
        }
    }
}
