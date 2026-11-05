### Fixed

- On the web, a device that still runs under 30 frames a second after the automatic quality steps (render scale, then
  the tilt-shift) now gets one more: the towns' buildings draw their lighter far copies, without shadows, at every zoom.
  The game still opens at about 30 km. Since v0.3.5 that opening drew the full building models with their shadows from
  the first frame, and weaker graphics ran at 2-3 frames a second. The step undoes itself as frames recover, and a fixed
  quality choice in Settings clears it.
