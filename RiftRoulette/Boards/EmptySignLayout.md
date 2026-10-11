# EmptySignLayout

Where the empty-server sign sits: two lines centered in the view of a
spectator camera at its start spot, facing it. Pure data and math, no game
calls; unit tested (`Tests/RiftRoulette.Tests/EmptySignLayoutTests`).

## Constants

| Name | Value |
|---|---|
| `TitleText` | `RIFT ROULETTE` |
| `JoinAddress` / `JoinText` | `66.94.102.245:27015` / `Join with connect 66.94.102.245:27015` (the public address in `README.md`) |
| `CameraPosition` / `CameraAngle` | (0, 0, 0) / (-0.59375, 179.3125, 0): where a spectator's camera starts on an empty server (under mid, looking level along -x), captured with `getpos_exact` |
| `Distance` | 300 units in front of the camera (600 and 400 read too far in game) |
| `SideShift` | 100 units along the viewer's right (negative: left), nudged by eye in game |
| `FontSize` | 64 px |
| `TitleScale` / `JoinScale` | 1.2 / 0.45 world units per px (about 77 / 29 units tall) |
| `LineGap` | 8 units between the center and each line |
| `CharWidth` | 0.66: estimated text width per character as a share of the font size, from the welcome board (`WatchLayout.WelcomeHalfWidth`) |

## Operations

| Operation | Result |
|---|---|
| `Center` | `WorldTextPlacement.InFrontOf(CameraPosition, yaw, Distance)`, lowered by `tan(pitch) * Distance` (positive pitch looks down; here it is raised about 3 units), then moved `SideShift` along `Right` |
| `Angle` | `WorldTextPlacement.FacingViewer(yaw)`: `(0, yaw - 90, 90)`, the welcome board's convention |
| `Right(yaw)` | The viewer's right, `(sin yaw, -cos yaw, 0)`: the way world text runs |
| `Width(text, scale)` | Estimated text width: characters × `CharWidth` × `FontSize` × scale |
| `Title()` | Solid green (0, 190, 0) `TitleText` spec, bottom edge `LineGap` above the center |
| `Join()` | Solid green (0, 190, 0) `JoinText` spec, top edge `LineGap` below the center |

Green because the area under mid is bright beige: white washed out and
dark navy read worse in game.

Each line's position is its bottom-left corner: center minus half its
width along `Right`, plus its height offset.

## Invariants

- Absolute world values; not tied to the watch spot or rift side.
- Centering is an estimate from the character count; nudge `CharWidth`
  or `Distance` if the sign reads off-center in game.
