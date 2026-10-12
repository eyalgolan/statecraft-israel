#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A thin horizontal meter in the promo's style (promo/final/04 and 08): a dark track with a coloured fill for a
    /// real 0..1 value (a minister's competence or loyalty out of 10, coalition stability, the reserve call-up, a budget
    /// share), and an optional tick for a threshold on the same scale (the call-up's minimum). The colour comes from a
    /// modifier class in Statecraft.uss. It takes no clicks and holds no text: the number beside it is a Label. In UXML:
    /// <c>&lt;Statecraft.Game.UI.UiBar class="ui-bar--stability" /&gt;</c>.</summary>
    [UxmlElement]
    public partial class UiBar : VisualElement
    {
        public const string ClassName = "ui-bar", FillClass = "ui-bar-fill", MarkClass = "ui-bar-mark";
        readonly VisualElement fill, mark;
        float value;
        float? markAt;

        public UiBar()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
            fill = new VisualElement { name = "fill", pickingMode = PickingMode.Ignore };
            fill.AddToClassList(FillClass);
            Add(fill);
            mark = new VisualElement { name = "mark", pickingMode = PickingMode.Ignore };
            mark.AddToClassList(MarkClass);
            mark.style.display = DisplayStyle.None;
            Add(mark);
            Value = 0;
        }

        static float Clamp(float v) => float.IsNaN(v) ? 0f : Mathf.Clamp01(v);

        /// <summary>The filled share, 0..1 (NaN reads as 0).</summary>
        public float Value
        {
            get => value;
            set { this.value = Clamp(value); fill.style.width = Length.Percent(this.value * 100f); }
        }

        /// <summary>A tick at this share of the track (0..1), or null for none.</summary>
        public float? Mark
        {
            get => markAt;
            set
            {
                markAt = value.HasValue ? Clamp(value.Value) : (float?)null;
                mark.style.display = markAt.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
                if (markAt.HasValue) mark.style.left = Length.Percent(markAt.Value * 100f);
            }
        }

        /// <summary>The fill element (tests read its resolved colour).</summary>
        public VisualElement Fill => fill;
    }
}
