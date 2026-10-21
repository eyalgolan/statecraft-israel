// Sharing a post with a screenshot (#1170, ShareShot.cs). The game hands over the PNG when the share panel opens, and it
// is kept here as a File, so it already exists when the player's Share press lands: navigator.share needs the real
// gesture. Unity handles a press on the next frame, outside the browser's event, so the press arms a one-shot listener
// for its release (pointerup on the canvas, or keyup), as ExternalLink.jslib does (#1168), and the release shares. Where
// the browser cannot share files, the same release downloads the picture instead, and the panel shows the share links.
// An unused arm disarms itself after 1.5 s, and a new press on the canvas (pointerdown) disarms it at once: a tap shorter
// than a frame is released before Unity arms the listener, and a stray click would otherwise share or download (#1182
// review, as on #1178). Nothing leaves the page unless the player shares or downloads it.
mergeInto(LibraryManager.library, {
  StatecraftShareKeep: function (pngPtr, length, namePtr) {
    var bytes = HEAPU8.slice(pngPtr, pngPtr + length);
    window.statecraftShareFile = new File([bytes], UTF8ToString(namePtr), { type: "image/png" });
  },
  StatecraftShareCanShareFiles: function () {
    var file = window.statecraftShareFile;
    try { return file && navigator.canShare && navigator.canShare({ files: [file] }) ? 1 : 0; } catch (e) { return 0; }
  },
  StatecraftShareFailed: function () { return window.statecraftShareFailed ? 1 : 0; },
  StatecraftShareOnRelease: function (textPtr, urlPtr) {
    var text = UTF8ToString(textPtr), url = UTF8ToString(urlPtr);
    if (typeof window === "undefined") return;
    if (window.statecraftShareDisarm) window.statecraftShareDisarm();
    var canvas = document.querySelector("#unity-canvas") || document;
    var timer;
    var disarm = function () {
      clearTimeout(timer);
      canvas.removeEventListener("pointerup", go);
      canvas.removeEventListener("pointerdown", disarm);
      document.removeEventListener("keyup", go);
      window.statecraftShareDisarm = null;
    };
    var download = function (file) {
      var link = document.createElement("a");
      link.href = URL.createObjectURL(file);
      link.download = file.name;
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(function () { URL.revokeObjectURL(link.href); }, 10000);
    };
    var go = function () {
      disarm();
      var file = window.statecraftShareFile;
      if (!file) return;
      var canShare = false;
      try { canShare = !!(navigator.canShare && navigator.canShare({ files: [file] })); } catch (e) {}
      if (!canShare) { download(file); return; }
      // A cancelled sheet (AbortError) is not a failure. Any other refusal (an iframe without web-share, a share target
      // that fails) downloads the picture instead, and StatecraftShareFailed tells the panel to show the share links.
      navigator.share({ files: [file], text: text, url: url }).catch(function (e) {
        if (e && e.name === "AbortError") return;
        window.statecraftShareFailed = true;
        download(file);
      });
    };
    // Reset here, not on the release: the panel asks StatecraftShareFailed after this press, and after an earlier
    // refused sheet it would read that old refusal and show the links before this share has run.
    window.statecraftShareFailed = false;
    canvas.addEventListener("pointerup", go);
    canvas.addEventListener("pointerdown", disarm);
    document.addEventListener("keyup", go);
    window.statecraftShareDisarm = disarm;
    timer = setTimeout(disarm, 1500);
  },
});
