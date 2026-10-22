// #1063: the hand pointer over a city the player can click (GameRoot.HoverCity); "" gives the page's own cursor back.
mergeInto(LibraryManager.library, {
  StatecraftHandCursor: function (on) { if (typeof Module !== "undefined" && Module.canvas) Module.canvas.style.cursor = on ? "pointer" : ""; },
});
