### Fixed

- A one-finger pan lifted just after it crossed the slop is no longer lost (#1661 item 5). `MapPointer` now declares
  `[DefaultExecutionOrder(-100)]`, so its frame runs before `CameraController`'s and sets the press's `moved` before the
  camera asks whether the press may still be a tap. With no order declared, Unity was free to tick the camera first,
  which read that answer from the frame before, kept the drag in `HoldDrag` and then dropped it as a click's.

### Changed

- `MapPointer` gains an internal `Tick` test seam, as `CameraController` has had since #890, so an EditMode test can
  drive the real `Update` path instead of the handlers beneath it (#1661 item 6). No behaviour change.
