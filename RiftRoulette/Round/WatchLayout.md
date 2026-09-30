# WatchLayout

Pure geometry for what surrounds a watch spot (boards, camera). The map is
point-symmetric, so the yellow layout is the green one turned half a turn
around the watch spot. Center uses the green offsets and yaw (no mirror).
All board positions and angles are written for green and turned here for
yellow.

## Operations

| Op | Returns |
|---|---|
| `WelcomeOffset` | (500, 500, 300): the welcome board's offset from the watch spot on green (toward the map edge). The watch view angles aim at it |
| `WelcomeHalfWidth` / `WelcomeHalfHeight` | 825 / 70: half the size of the "RIFT ROULETTE" text. Estimated from the font size (64 px, 3 units per px) as ~825 (earlier eye nudges 830→730→430 read off-center) |
| `WelcomeCenter` | Estimated middle of the welcome text on green: the text starts at `WelcomeOffset` (its bottom-left) and runs toward -y, facing -x, so the center is `WelcomeOffset + (0, -825, 70)` |
| `WelcomeFrontOffset` | Green offset of the spot straight in front of that center: `WelcomeViewDistance` (900) toward -x, same y, floor height (z 0) |
| `WelcomeFront(anchor, side)` | Location `<anchor>#welcome` at `anchor + Offset(side, WelcomeFrontOffset)`, angle `LookAt` from `EyeHeight` (64) above it to the turned center: yaw 0 on green/center, 180 on yellow, looking slightly up |
| `Offset(side, greenOffset)` | Green and Center: unchanged. Yellow: (-x, -y, z) |
| `Yaw(side, greenYaw)` | Green and Center: unchanged. Yellow: `greenYaw + 180`, normalized |
| `Angle(side, greenAngle)` | (pitch, `Yaw(side, yaw)`, roll): only yaw turns |
| `LookAt(from, to)` | (pitch, yaw, 0) in Source convention (pitch positive = down, yaw from +x toward +y), yaw normalized |
| `Normalize(yaw)` | yaw wrapped into (-180, 180] |

## Used by

- `Draft/BoardLayout`: every board spec.
- `Lobby/AdminSeat`: the roaming admin stands at `WelcomeFront`.
- `Tests/RiftRoulette.Tests/WatchLayoutTests`: checks the
  `RiftRouletteLocations` watch view angles against `LookAt` of the welcome
  board, so camera and board cannot drift apart.

## Invariants

- No Deadworks calls; safe to link into the test project.
- `WelcomeFront` is behind the watch rows, on the invisible skybox floor,
  which is not in the map mesh. The floor held at the first spot (600 back,
  830 half-width; 2026-09-28); the current one is unverified (`AdminSeat`
  catches a fall).
