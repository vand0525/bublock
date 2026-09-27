# WatchLayout

Pure geometry for what surrounds a watch spot (boards, camera). The map is
point-symmetric, so the yellow layout is the green one turned half a turn
around the watch spot. All board positions and angles are written for green
and turned here for yellow.

## Operations

| Op | Returns |
|---|---|
| `WelcomeOffset` | (500, 500, 300): the welcome board's offset from the watch spot on green (toward the map edge). The watch view angles aim at it |
| `Offset(side, greenOffset)` | Green: unchanged. Yellow: (-x, -y, z) |
| `Yaw(side, greenYaw)` | Green: unchanged. Yellow: `greenYaw + 180`, normalized |
| `Angle(side, greenAngle)` | (pitch, `Yaw(side, yaw)`, roll): only yaw turns |
| `LookAt(from, to)` | (pitch, yaw, 0) in Source convention (pitch positive = down, yaw from +x toward +y), yaw normalized |
| `Normalize(yaw)` | yaw wrapped into (-180, 180] |

## Used by

- `Draft/BoardLayout`: every board spec.
- `Tests/RiftRoulette.Tests/WatchLayoutTests`: checks the
  `RiftRouletteLocations` watch view angles against `LookAt` of the welcome
  board, so camera and board cannot drift apart.

## Invariants

- No Deadworks calls; safe to link into the test project.
