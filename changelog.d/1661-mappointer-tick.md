### Changed

- `MapPointer` gains an internal `Tick` test seam, as `CameraController` has had since #890, so an EditMode test can
  drive the real `Update` path instead of the handlers beneath it (#1661 item 6). No behaviour change.

### Fixed

- Nothing yet: `MapPointerTickTests` records what the real frame does. A touch gesture that begins and ends inside one
  frame yields neither a tap nor a pan (#1661 item 4, confirmed), and a one-finger drag past the slop pans or does not
  depending on which `Update` Unity happens to run first (#1661 item 5, a failing test kept `[Ignore]`d until the fix
  is chosen).
