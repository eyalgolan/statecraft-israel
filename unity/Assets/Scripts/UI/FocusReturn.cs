#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Gives the keyboard focus to a sensible control when the one that had it goes away (accessibility audit,
    /// #41 M1): the start screen closing, the last open event answered, Escape from a screen. Without it nothing has the
    /// focus and the next Tab starts again at Cabinet.</summary>
    public static class FocusReturn
    {
        /// <summary>Focuses <paramref name="target"/> now, and once more when styles have resolved if by then nothing has the
        /// focus (a control that was hidden or disabled a moment ago cannot take it yet). A control that takes the focus
        /// meanwhile (the decision card when its cover closes) keeps it.</summary>
        public static void To(VisualElement? target)
        {
            if (target?.panel == null) return;
            target.Focus();
            target.schedule.Execute(() => { if (target.panel != null && target.focusController?.focusedElement == null) target.Focus(); });
        }

        /// <summary>Whether an element can actually take the keyboard focus now: a control that is disabled or not drawn
        /// cannot, and asking it to leaves the focus nowhere.</summary>
        static bool CanTakeFocus(VisualElement? e) =>
            e?.panel != null && e.focusable && e.enabledInHierarchy && e.resolvedStyle.display != DisplayStyle.None;

        /// <summary>Focuses <paramref name="target"/>; failing that the first control that can take the focus in each of
        /// <paramref name="scopes"/> in turn; and last of all <paramref name="last"/> (accessibility audit, #41, the
        /// keyboard-only turn's "Still open" list). The K2 fix hands the focus to the rebuilt button of the same name, but
        /// a press can leave its whole part with nothing focusable: Build on a site or a corridor segment disables every
        /// Build in the part once one group is ordered, and Stop and a project's Cancel leave a state line and a disabled
        /// button. Without a last stop outside the part the focus is left nowhere and the next Tab starts again at the
        /// first screen, which is the symptom K2 was meant to end. Nearest first, so the next Tab stays where the player
        /// was working.</summary>
        public static void ToOrWithin(VisualElement? target, VisualElement? last, params VisualElement?[] scopes)
        {
            if (CanTakeFocus(target)) { To(target); return; }
            foreach (var scope in scopes)
            {
                if (scope?.panel == null) continue;
                foreach (var e in scope.Query<VisualElement>().Build())
                    if (e != scope && CanTakeFocus(e)) { To(e); return; }
            }
            if (CanTakeFocus(last)) To(last);
        }
    }

    /// <summary>Whether the last press inside an element came from the keyboard (Enter or Space: NavigationSubmitEvent) or
    /// the pointer. A pointer press moves no focus anywhere (PointerBlur's rule: a mouse click leaves no focus fill behind),
    /// so a presenter asks this before it hands the focus on.</summary>
    public sealed class PressSource : System.IDisposable
    {
        readonly VisualElement scope;
        readonly EventCallback<PointerDownEvent> onPointer;
        readonly EventCallback<NavigationSubmitEvent> onSubmit;
        bool pointer;

        public PressSource(VisualElement scope)
        {
            this.scope = scope;
            onPointer = _ => pointer = true;
            onSubmit = _ => pointer = false;
            scope.RegisterCallback(onPointer, TrickleDown.TrickleDown);
            scope.RegisterCallback(onSubmit, TrickleDown.TrickleDown);
        }

        /// <summary>True unless the last press in the element was a pointer press.</summary>
        public bool ByKeyboard => !pointer;

        public void Dispose()
        {
            scope.UnregisterCallback(onPointer, TrickleDown.TrickleDown);
            scope.UnregisterCallback(onSubmit, TrickleDown.TrickleDown);
        }
    }
}
