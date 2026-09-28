# MovementLocation

Named teleport target.

| Field | Meaning |
|---|---|
| `Name` | Lookup name (case-insensitive in the registry; letters, digits, `_`, `.`, `-`) |
| `Position` | World position the pawn is teleported to |
| `Angle` | Camera angle (pitch, yaw, roll) sent to the client after the teleport |

Game code can pass a `MovementLocation` straight to `MovementService` without
registering it; registering only makes it reachable by name (`/mv_tp`,
`/mv_list`).

## Operations

- `Offset(local, name = null)`: pure. A new location moved by
  `local = (forward, right, up)` in this location's own frame: forward is
  the direction of `Angle.Y` (yaw 0 = +x, 90 = +y), right is 90 degrees
  clockwise from it (`(sin yaw, -cos yaw)`), up is +z. Pitch and roll are
  ignored. The angle is kept; the name is replaced when `name` is given.
  Moving the anchor moves every offset spot with it, and an anchor turned
  half a turn mirrors its offsets.
- `LocalOf(world)`: pure. The inverse of `Offset`: the
  `(forward, right, up)` of a world position in this location's frame,
  so `LocalOf(Offset(local).Position) == local`.
