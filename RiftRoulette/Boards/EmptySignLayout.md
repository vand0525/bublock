# EmptySignLayout

Where the empty-server sign sits: two lines centered in the view of a
spectator camera at its start spot, facing it, placed from an
`EmptySignSettings` (distance, shift, height, sizes, gap, colors, text).
Pure math, no game calls; unit tested
(`Tests/RiftRoulette.Tests/EmptySignLayoutTests`).

## Constants

| Name | Value |
|---|---|
| `JoinAddress` | `66.94.102.245:27015` (the public address in `README.md`; used by the default join text) |
| `CameraPosition` / `CameraAngle` | (0, 0, 0) / (-0.59375, 179.3125, 0): where a spectator's camera starts on an empty server (under mid, looking level along -x), captured with `getpos_exact` |
| `FontSize` | 64 px |
| `CharWidth` | 0.66: estimated text width per character as a share of the font size, from the welcome board (`WatchLayout.WelcomeHalfWidth`) |

## Operations

| Operation | Result |
|---|---|
| `Center(settings)` | `WorldTextPlacement.InFrontOf(CameraPosition, yaw, Distance)`, lowered by `tan(pitch) * Distance` (positive pitch looks down; here it rises a few units), plus `Shift` along `Right` and `Height` up |
| `Angle` | `WorldTextPlacement.FacingViewer(yaw)`: `(0, yaw - 90, 90)`, the welcome board's convention |
| `Right(yaw)` | The viewer's right, `(sin yaw, -cos yaw, 0)`: the way world text runs |
| `Width(text, scale)` | Estimated text width: characters × `CharWidth` × `FontSize` × scale |
| `Title(settings)` | `TitleText` in `TitleColor` at `TitleScale`, bottom edge `Gap` above the center |
| `Join(settings)` | `JoinText` in `JoinColor` at `JoinScale`, top edge `Gap` below the center |

Each line's position is its bottom-left corner: center minus half its
estimated width along `Right`, plus its height offset.

## Invariants

- Absolute world values; not tied to the watch spot or rift side.
- Centering is an estimate from the character count; `Shift` corrects it
  by eye.
