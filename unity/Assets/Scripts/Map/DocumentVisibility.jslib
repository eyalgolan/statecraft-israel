// Whether the page is hidden (a background tab or a minimised window; in Chromium on Windows and macOS also a window
// covered by others, which Firefox and Safari leave visible): QualityFallbackDriver does not measure frames then, since
// the browser throttles them (issue #216).
mergeInto(LibraryManager.library, {
  StatecraftDocumentHidden: function () { return typeof document !== "undefined" && document.hidden ? 1 : 0; },
});
