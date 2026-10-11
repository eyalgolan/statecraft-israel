#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Keeps the focus look for the keyboard only. UI Toolkit focuses a Button on pointer-down and has no
    /// :focus-visible, so after a mouse click the button would keep the focus fill (Statecraft.uss Button:focus) until
    /// something else took focus. One ClickEvent callback at the UI root blurs the clicked Button: every Button, the ones
    /// made at runtime (event options, Cabinet seats, Budget rows) included. Enter and Space raise NavigationSubmitEvent,
    /// not ClickEvent, so keyboard focus stays.</summary>
    public sealed class PointerBlur : System.IDisposable
    {
        readonly VisualElement root;
        static readonly EventCallback<ClickEvent> OnClick = e => (e.target as VisualElement)?.GetFirstOfType<Button>()?.Blur();

        public PointerBlur(VisualElement root) { this.root = root; root.RegisterCallback(OnClick); }

        public void Dispose() => root.UnregisterCallback(OnClick);
    }
}
