#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Keeps the HUD and the panels inside the phone's safe area: on a notched phone the top bar otherwise sits
    /// under the notch and the bottom buttons under the home indicator (mobile audit). Pads the root by the safe area's
    /// insets, in panel px, and lets the layout do the rest: every panel is positioned from the root's edges, the taller
    /// top bar flows into the camera's inset through TopBarInset as before, and the map labels re-place from their
    /// layer's moved origin (MapLabelsPresenter.Refresh measures container.worldBound). A desktop or a notch-free phone
    /// has no insets, so nothing changes there.</summary>
    public sealed class SafeAreaFit : System.IDisposable
    {
        readonly VisualElement root;
        readonly System.Func<(float l, float r, float t, float b)>? readInsets;   // tests feed the insets; production reads the platform
        IVisualElementScheduledItem? poll;
        float appliedL = -1f, appliedR, appliedT, appliedB;   // an impossible start: the first Update always writes

        public SafeAreaFit(VisualElement root) : this(root, null) { }

        internal SafeAreaFit(VisualElement root, System.Func<(float l, float r, float t, float b)>? readInsets)
        {
            this.root = root;
            this.readInsets = readInsets;
            root.RegisterCallback<GeometryChangedEvent>(OnGeometry);   // a rotation usually moves the notch with the bounds
            // ...but not always: two landscapes of the same dimensions swap the notch side with the root bounds
            // unchanged and no event fires, so poll every frame; Apply writes only on a change.
            poll = root.schedule.Execute(Update).Every(0);
            Update();
        }

        void OnGeometry(GeometryChangedEvent e) => Update();

        public void Update()
        {
            if (readInsets != null) { var v = readInsets(); Apply(v.l, v.r, v.t, v.b); return; }
            var panel = root.panel;
            if (panel == null) return;
            float scale = panel.scaledPixelsPerPoint;
#if UNITY_WEBGL && !UNITY_EDITOR
            // The browser's env(safe-area-inset-*) values come in CSS px (SafeArea.jslib); the canvas is drawn in device
            // pixels, so a CSS px is devicePixelRatio device px (HudPresenter's text floor divides by the same ratio).
            var css = new float[4];
            StatecraftSafeAreaInsets(css);
            float dpr = StatecraftDevicePixelRatio();
            Apply(Panel(css[3], dpr, scale), Panel(css[1], dpr, scale), Panel(css[0], dpr, scale), Panel(css[2], dpr, scale));
#else
            // Screen.safeArea's margins in device px; the editor and desktops get the whole screen, so all four are 0.
            var safe = Screen.safeArea;
            Apply(PanelPx(safe.xMin, scale), PanelPx(Screen.width - safe.xMax, scale),
                PanelPx(Screen.height - safe.yMax, scale), PanelPx(safe.yMin, scale));
#endif
        }

        /// <summary>Pads the root by the insets, in panel px. Idempotent (the same insets write once); Dispose resets.
        /// The seam SafeAreaTests pins, so the padding writes and the reset cannot silently go.</summary>
        internal void Apply(float l, float r, float t, float b)
        {
            if (l == appliedL && r == appliedR && t == appliedT && b == appliedB) return;
            appliedL = l; appliedR = r; appliedT = t; appliedB = b;
            root.style.paddingLeft = l; root.style.paddingRight = r;
            root.style.paddingTop = t; root.style.paddingBottom = b;
        }

        /// <summary>cssPx in panel px: dpr device px per CSS px (the canvas's scale), scale device px per panel px.</summary>
        public static int Panel(float cssPx, float dpr, float scale) => scale > 0f ? Mathf.RoundToInt(cssPx * dpr / scale) : 0;
        /// <summary>devicePx in panel px at scale device px per panel px.</summary>
        public static int PanelPx(float devicePx, float scale) => scale > 0f ? Mathf.RoundToInt(devicePx / scale) : 0;

        public void Dispose()
        {
            poll?.Pause(); poll = null;
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            root.style.paddingLeft = StyleKeyword.Null; root.style.paddingRight = StyleKeyword.Null;
            root.style.paddingTop = StyleKeyword.Null; root.style.paddingBottom = StyleKeyword.Null;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftSafeAreaInsets(float[] insets);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern float StatecraftDevicePixelRatio();
#endif
    }
}
