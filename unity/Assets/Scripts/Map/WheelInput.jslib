// The browser's wheel events on the canvas, for CameraController (issue #43, T45): Unity's player takes the smallest
// delta it has seen since the page loaded as one notch, so one small touchpad delta made later frames zoom far too much.
// Each event's deltaY and deltaMode are queued here and drained by C# once a frame, which does the scaling. deltaY is
// read before deltaMode, as Unity's own handler does: Firefox reports lines only to a page that asks for deltaMode
// first, and otherwise turns them into pixels.
mergeInto(LibraryManager.library, {
  $StatecraftWheel: { queue: [], canvas: null },

  StatecraftWheelDrain__deps: ["$StatecraftWheel"],
  StatecraftWheelDrain: function (deltaY, mode, capacity) {
    var w = StatecraftWheel;
    var canvas = Module["canvas"];
    if (canvas && w.canvas !== canvas) {
      w.canvas = canvas;
      canvas.addEventListener("wheel", function (e) {
        var dy = e.deltaY;
        if (dy !== 0 && w.queue.length < 1024) w.queue.push(dy, e.deltaMode | 0);
      }, { passive: true });
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
