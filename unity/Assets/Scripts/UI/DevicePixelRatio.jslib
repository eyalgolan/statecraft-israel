// The browser's devicePixelRatio: the canvas is drawn in device pixels, so HudPresenter's text floor divides the panel's
// scale by it to count CSS pixels, the size the player sees (#624).
mergeInto(LibraryManager.library, {
  StatecraftDevicePixelRatio: function () { return typeof window !== "undefined" && window.devicePixelRatio > 0 ? window.devicePixelRatio : 1; },
});
