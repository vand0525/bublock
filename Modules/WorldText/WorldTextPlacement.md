# WorldTextPlacement

Pure math for placing a board in front of a viewer. No game calls; unit
tested in `Tests/Modules.Tests`.

## Operations

- `InFrontOf(eyePosition, viewerYaw, distance = 150)`: point `distance`
  units ahead of the eye along the viewer's yaw, level (pitch ignored, so
  looking down does not bury the board in the floor).
- `FacingViewer(viewerYaw)`: board angles `(0, viewerYaw - 90, 90)`, yaw
  normalized to (-180, 180].
- `NormalizeYaw(yaw)`: wraps any yaw into (-180, 180].

## Where the angle rule comes from

It matches the draft boards, which players read from the draft position:

| Board | Offset from draft | Viewer yaw toward it | Board yaw |
|---|---|---|---|
| Sapphire | +Y | 90 | 360 (= 0) |
| Amber | -Y | -90 | 180 |
| Title | +X, +Y (high) | about 0 | -90 |

Board yaw = viewer yaw - 90, with pitch 0 and roll 90 (exact for the two
team boards; the title sits off-axis and matches approximately).

## Constraints

- Uses Source angle order (pitch, yaw, roll) in degrees.
- In-game orientation is confirmed.
