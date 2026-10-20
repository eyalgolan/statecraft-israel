// The browser's safe-area insets in CSS pixels (the notch, the home indicator), for SafeAreaFit: a probe element reads
// the env(safe-area-inset-*) values, which iOS Safari and Chrome on Android expose; everywhere else they are 0, so the
// UI is padded only where hardware eats the screen. Writes top, right, bottom, left. The HEAPF32 indexing assumes
// wasm32 pointers (Unity 6000.3's WebGL target), as WheelInput.jslib.
mergeInto(LibraryManager.library, {
  StatecraftSafeAreaInsets: function (out) {
    var v = [0, 0, 0, 0];
    if (typeof document !== "undefined" && document.body) {
      var probe = document.createElement("div");
      probe.style.cssText = "position:fixed;visibility:hidden;pointer-events:none;" +
        "padding-top:env(safe-area-inset-top,0px);padding-right:env(safe-area-inset-right,0px);" +
        "padding-bottom:env(safe-area-inset-bottom,0px);padding-left:env(safe-area-inset-left,0px);";
      document.body.appendChild(probe);
      var cs = getComputedStyle(probe);
      v = [parseFloat(cs.paddingTop) || 0, parseFloat(cs.paddingRight) || 0,
           parseFloat(cs.paddingBottom) || 0, parseFloat(cs.paddingLeft) || 0];
      document.body.removeChild(probe);
    }
    for (var i = 0; i < 4; i++) HEAPF32[(out >> 2) + i] = v[i];
  },
});
