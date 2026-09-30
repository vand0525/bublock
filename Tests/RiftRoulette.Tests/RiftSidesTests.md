# RiftSidesTests

Pure unit tests for `RiftRoulette.Rift.RiftSides`.

- `TryParse`: accepts green / yellow / center (and middle / mid), ignoring
  case and spaces; rejects empty, null, and unknown names.
- `Other`: Green ↔ Yellow.
- `NextInRotation`: with mid on, Green → Yellow → Center → Green; with mid
  off, Green ↔ Yellow (Center → Green).
- `Name`: GREEN / YELLOW / CENTER.
- `TryMatch`: finds a side within `MatchDistance` of its position; rejects
  far points.
- `Nearest`: closest of the three sides.
- `Position`: archive green / yellow coords and center at (0, 0, 448).
