# StreamFraming

The stream camera's fixed spot for each rift side (`StreamCam` parks
there). Pure data, no game calls; unit tested
(`Tests/RiftRoulette.Tests/StreamFramingTests`).

## Operations

| Operation | Result |
|---|---|
| `Spot(side)` | World position and view angle (pitch, yaw, roll) for that side. |

| Side | Position | Angle |
|---|---|---|
| Green | (7121, -119.906, 2142.375) | (12.281, -1.9375, 0) |
| Yellow | (-7063.344, 47.594, 1664.281) | (-4.75, 174.844, 0) |
| Center | (-479.688, 70.344, 1639.969) | (-13.25, -8.625, 0) |

## Invariants

- Absolute world values, captured by the admin in fly cam with
  `getpos_exact` (it prints `setpos_exact` / `setang_exact`). Not relative
  to the watch spot anchor: moving an anchor does not move the camera.
- The angle is handed unchanged to `SpectateService.Park`, which sends it
  as the client camera angle after the teleport.
