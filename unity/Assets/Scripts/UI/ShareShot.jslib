// Sharing a post with a screenshot (#1170, ShareShot.cs). The game hands over the PNG when the share panel opens, and it
// is kept here as a File, so it already exists when the player's Share press lands: navigator.share needs the real
// gesture. Unity handles a press on the next frame, outside the browser's event, so the press arms a one-shot listener
// for its release (pointerup on the canvas, or keyup), as ExternalLink.jslib does (#1168), and the release shares. Where
// the browser cannot share files, the same release downloads the picture instead, and the panel shows the share links.
// An unused arm disarms itself after 1.5 s, and a new press on the canvas (pointerdown) disarms it at once: a tap shorter
// than a frame is released before Unity arms the listener, and a stray click would otherwise share or download (#1182
// review, as on #1178). Nothing leaves the page unless the player shares or downloads it.
// Copy (#1238) arms the same kind of release listener; on the release it writes the kept picture to the clipboard, and
// where the browser refuses the picture (no ClipboardItem or no image/png, a denied permission, an iframe without
// clipboard-write) it copies the post text instead, then through a hidden textarea (execCommand), which some iframes
// still allow. StatecraftShareCopied tells the panel which one landed: 1 the picture, 2 the text, 3 neither, 0 not yet.
mergeInto(LibraryManager.library, {
  // Shared by Copy and copy-then-open. Kept PNG means clipboard.write starts in this very release event.
  $StatecraftCopySharePicture: function (text, serial, site) {
    var done = function (result) { if (serial === window.statecraftShareCopySerial) window.statecraftShareCopied = result; };
    var file = window.statecraftShareFile;
    // Site links cannot carry a PNG. If copying it is blocked, request the kept PNG download instead (C149).
    // Called synchronously from the release, before window.open. A promise rejection also retains the picture.
    var download = function () {
      if (serial !== window.statecraftShareCopySerial) return;
      if (!file) { done(3); return; }
      var link = document.createElement("a"), url;
      try {
        url = URL.createObjectURL(file); link.href = url; link.download = file.name;
        document.body.appendChild(link); link.click(); done(4); // requested, not proof that the browser saved it
      } catch (error) { done(3); }
      finally { link.remove(); if (url) setTimeout(function () { URL.revokeObjectURL(url); }, 10000); }
    };
    // The textarea takes the focus for the copy; the game's canvas gets it back, or its keys stop working.
    var legacyCopy = function () {
      if (serial !== window.statecraftShareCopySerial) return false;
      var focused = document.activeElement, area = document.createElement("textarea"), ok = false;
      area.value = text;
      area.setAttribute("readonly", "");
      area.style.position = "fixed"; area.style.opacity = "0";
      document.body.appendChild(area);
      area.select();
      try { ok = document.execCommand("copy"); } catch (e) {}
      area.remove();
      if (focused && focused.focus) focused.focus();
      return ok;
    };
    var copyText = function () {
      if (serial !== window.statecraftShareCopySerial) return;
      var write;
      try { write = navigator.clipboard && navigator.clipboard.writeText ? navigator.clipboard.writeText(text) : Promise.reject(); }
      catch (e) { write = Promise.reject(e); }   // some browsers throw at once instead of rejecting
      write.then(function () { done(2); }, function () { done(legacyCopy() ? 2 : 3); });
    };
    var picture = false;
    try {
      picture = file && navigator.clipboard && navigator.clipboard.write && typeof ClipboardItem !== "undefined"
          && !(ClipboardItem.supports && !ClipboardItem.supports("image/png"));
    } catch (error) {} // a denied capability getter also takes the site download path
    if (site) {
      var policy = document.permissionsPolicy || document.featurePolicy;
      var allowed = true;
      try { if (policy && policy.allowsFeature) allowed = policy.allowsFeature("clipboard-write"); }
      catch (error) { allowed = false; }
      if (!picture || !allowed) { download(); return; }
      try { navigator.clipboard.write([new ClipboardItem({ "image/png": file })]).then(function () { done(1); }, download); }
      catch (error) { download(); }
      return;
    }
    if (!picture) { copyText(); return; }
    try { navigator.clipboard.write([new ClipboardItem({ "image/png": file })]).then(function () { done(1); }, copyText); }
    catch (e) { copyText(); }
  },

  StatecraftShareKeep: function (pngPtr, length, namePtr) {
    // A late rejected write from an old panel must not download after a new capture replaced its picture.
    window.statecraftShareCopySerial = (window.statecraftShareCopySerial || 0) + 1;
    window.statecraftShareCopied = 0;
    var bytes = HEAPU8.slice(pngPtr, pngPtr + length);
    window.statecraftShareFile = new File([bytes], UTF8ToString(namePtr), { type: "image/png" });
  },
  StatecraftShareCanShareFiles: function () {
    var file = window.statecraftShareFile;
    try { return file && navigator.canShare && navigator.canShare({ files: [file] }) ? 1 : 0; } catch (e) { return 0; }
  },
  StatecraftShareFailed: function () { return window.statecraftShareFailed ? 1 : 0; },
  StatecraftShareOnRelease: function (textPtr) {
    var text = UTF8ToString(textPtr);   // both links are already in the post; no separate URL (#1653)
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
      navigator.share({ files: [file], text: text }).catch(function (e) {
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
  StatecraftShareCopied: function () { return window.statecraftShareCopied || 0; },
  StatecraftShareCopyOnRelease__deps: ["$StatecraftCopySharePicture"],
  StatecraftShareCopyOnRelease: function (textPtr) {
    var text = UTF8ToString(textPtr);
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
    var serial = window.statecraftShareCopySerial = (window.statecraftShareCopySerial || 0) + 1;
    var go = function () { disarm(); StatecraftCopySharePicture(text, serial); };
    // Reset on the press, as statecraftShareFailed is: the panel starts asking right after it.
    window.statecraftShareCopied = 0;
    canvas.addEventListener("pointerup", go);
    canvas.addEventListener("pointerdown", disarm);
    document.addEventListener("keyup", go);
    window.statecraftShareDisarm = disarm;
    timer = setTimeout(disarm, 1500);
  },
});
