// The browser's safe-area insets in CSS pixels (the notch, the home indicator), for SafeAreaFit: a probe element reads
// the env(safe-area-inset-*) values, which iOS Safari and Chrome on Android expose; everywhere else they are 0, so the
// UI is padded only where hardware eats the screen. Writes top, right, bottom, left. The HEAPF32 indexing assumes
// wasm32 pointers (Unity 6000.3's WebGL target), as WheelInput.jslib.
mergeInto(LibraryManager.library, {
  StatecraftSafeAreaInsets: function (out) {
    // The Statecraft template owns and clips both regions inside the hardware margins. Its inner grid can scroll;
    // scrolling the canvas is not another notch inset and must not move the game's UI padding.
    var layout = typeof document !== "undefined" && typeof document.getElementById === "function" ? document.getElementById("player-layout") : null;
    if (layout && typeof Module !== "undefined" && Module.canvas && layout.contains(Module.canvas)) {
      for (var i = 0; i < 4; i++) HEAPF32[(out >> 2) + i] = 0;
      return;
    }
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
      // #1119: the page can already inset/resize the canvas to make room for the companion. Only pad the part of
      // the hardware inset that still intersects it; a full-window canvas keeps the original margins unchanged.
      if (typeof Module !== "undefined" && Module.canvas && typeof Module.canvas.getBoundingClientRect === "function" && typeof window !== "undefined") {
        var r = Module.canvas.getBoundingClientRect();
        v = [Math.max(0, v[0] - r.top), Math.max(0, v[1] - (window.innerWidth - r.right)),
             Math.max(0, v[2] - (window.innerHeight - r.bottom)), Math.max(0, v[3] - r.left)];
      }
    }
    for (var i = 0; i < 4; i++) HEAPF32[(out >> 2) + i] = v[i];
  },
});
