# WorldTextPlacementTests

Unit tests for `Modules/WorldText/WorldTextPlacement`.

- `InFrontOf` moves along the viewer's yaw (0, 90, 180, -90), keeps the eye
  height, and honors a custom distance.
- `FacingViewer` gives the draft board angles: viewer yaw 90 ->
  Sapphire board `(0, 0, 90)`; viewer yaw -90 -> Amber board `(0, 180, 90)`.
- `NormalizeYaw` wraps into (-180, 180] (e.g. 270 -> -90, -180 -> 180,
  725 -> 5).
