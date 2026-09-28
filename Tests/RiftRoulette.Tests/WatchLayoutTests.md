# WatchLayoutTests

Unit tests for `RiftRoulette/Round/WatchLayout`.

- Green offset and yaw are returned unchanged.
- Yellow offsets are rotated half a turn: (500, 500, 300) becomes
  (-500, -500, 300).
- The yellow welcome board lands farther toward the map edge than the watch
  spot (x below -7560).
- Yellow yaw is green yaw + 180, normalized (-90 -> 90, 360 -> 180, 180 -> 0).
- `Angle` only turns the yaw.
- Each watch spot's view angle (`RiftRouletteLocations.WatchGreen` /
  `WatchYellow`) points at its welcome board, within 0.5°.
- `LookAt` pitches negative (up) for a target above.
- `WelcomeFront` faces the sign head on from each watch anchor: yaw 0 on
  green, 180 on yellow, pitch up, floor height of the anchor, lined up with
  the estimated text center and `WelcomeViewDistance` in front of it.
