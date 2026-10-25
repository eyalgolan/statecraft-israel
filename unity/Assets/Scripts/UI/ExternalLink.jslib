// C120: preserve the browser's native activation, while allowing a press dragged off its button to cancel.
mergeInto(LibraryManager.library, {
  StatecraftCancelExternalLink: function (ticket) {
    if (typeof window !== "undefined" && window.statecraftOpenOnRelease && window.statecraftOpenOnRelease.id === ticket) window.statecraftOpenOnRelease.cancel();
  },
  StatecraftOpenOnRelease: function (urlPtr, left, top, right, bottom, keyboard) {
    if (typeof window === "undefined") return 0;
    var url = UTF8ToString(urlPtr);
    if (window.statecraftOpenOnRelease) window.statecraftOpenOnRelease.cancel();
    var canvas = document.querySelector("#unity-canvas");
    if (!canvas) return 0;
    var id = window.statecraftExternalLinkSerial = (window.statecraftExternalLinkSerial || 0) + 1;
    var timer;
    var disarm = function () {
      clearTimeout(timer);
      canvas.removeEventListener("pointerup", release);
      canvas.removeEventListener("pointerdown", disarm);
      canvas.removeEventListener("pointercancel", disarm);
      document.removeEventListener("keyup", release);
      document.removeEventListener("keydown", escape);
      document.removeEventListener("visibilitychange", disarm);
      window.removeEventListener("blur", disarm);
      window.removeEventListener("resize", disarm);
      if (window.statecraftOpenOnRelease && window.statecraftOpenOnRelease.id === id) window.statecraftOpenOnRelease = null;
    };
    var escape = function (e) { if (e.key === "Escape") disarm(); };
    var release = function (e) {
      var valid = false;
      if (keyboard) {
        if (e.type !== "keyup" || (e.key !== "Enter" && e.key !== " " && e.key !== "Spacebar")) return;
        valid = true;
      } else {
        if (e.type !== "pointerup" || e.isPrimary === false || e.button !== 0) return;
        var r = canvas.getBoundingClientRect();
        var x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
        valid = r.width > 0 && r.height > 0 && x >= left && x < right && y >= top && y < bottom;
      }
      disarm();
      if (valid) window.open(url, "_blank", "noopener");
    };
    window.statecraftOpenOnRelease = { cancel: disarm, id: id };
    canvas.addEventListener("pointerup", release);
    canvas.addEventListener("pointerdown", disarm);
    canvas.addEventListener("pointercancel", disarm);
    document.addEventListener("keyup", release);
    document.addEventListener("keydown", escape);
    document.addEventListener("visibilitychange", disarm);
    window.addEventListener("blur", disarm);
    window.addEventListener("resize", disarm);
    timer = setTimeout(disarm, 1500);
    return id;
  },
  // The bug form's platform line (#1169, ReportLinks.Environment): the browser's user agent and screen size, nothing else.
  StatecraftBrowserEnv: function () {
    var env = navigator.userAgent + ", screen " + screen.width + "x" + screen.height;
    var size = lengthBytesUTF8(env) + 1;
    var buffer = _malloc(size);
    stringToUTF8(env, buffer, size);
    return buffer;
  },
});
