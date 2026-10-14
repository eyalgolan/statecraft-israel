// The browser's wheel events on the canvas, for CameraController (issue #43, T45): Unity's player takes the smallest
// delta it has seen since the page loaded as one notch, so one small touchpad delta made later frames zoom far too much.
// Each event's deltaY and deltaMode are queued here and drained by C# once a frame, which does the scaling.
//
// The listener is on window in the capture phase, so it runs before Unity's own handler on the canvas, and it reads
// deltaMode before deltaY. Firefox (88 and later, dom.event.wheel-deltaMode-lines.disabled) reports a mouse notch in
// lines only to a script that asks for deltaMode first; once asked, the event stays in lines for every later reader,
// Unity's handler included. Unity counts the smallest delta it has seen as one notch whatever the mode, so its own
// readings (UI Toolkit scrolling) still give one notch per notch. Other browsers send the same event either way.
// It only reads (passive, no preventDefault) and ignores events whose target is not Unity's canvas.
//
// The listener is never removed: Unity's Quit() removes only the handlers Emscripten registered. This game creates
// the player once, and the queue is capped, so nothing grows. The HEAPF32/HEAP32 indexing assumes wasm32 pointers
// (Unity 6000.3's WebGL target).
mergeInto(LibraryManager.library, {
  $StatecraftWheel: { queue: [], listening: false },

  StatecraftWheelDrain__deps: ["$StatecraftWheel"],
  StatecraftWheelDrain: function (deltaY, mode, capacity) {
    var w = StatecraftWheel;
    if (!w.listening && typeof window !== "undefined") {
      w.listening = true;
      window.addEventListener("wheel", function (e) {
        if (e.target !== Module["canvas"]) return;
        var m = e.deltaMode | 0;   // first: see above
        var dy = e.deltaY;
        if (dy !== 0 && w.queue.length < 1024) w.queue.push(dy, m);
      }, { capture: true, passive: true });
    }
    var n = Math.min(capacity, w.queue.length / 2);
    for (var i = 0; i < n; i++) {
      HEAPF32[(deltaY >> 2) + i] = w.queue[2 * i];
      HEAP32[(mode >> 2) + i] = w.queue[2 * i + 1];
    }
    w.queue.splice(0, 2 * n);
    return n;
  },
});
