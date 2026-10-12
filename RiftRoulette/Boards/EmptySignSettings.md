# EmptySignSettings

The adjustable look of the empty-server sign: position, size, gap, colors
and text. Immutable records, no game calls; unit tested
(`Tests/RiftRoulette.Tests/EmptySignSettingsTests`). Saved by
`EmptySignStore`, changed by the `/sign_*` commands.

## Types

- `SignColor(R, G, B)`: 0-255 each. `ToWorldText()` gives an opaque
  `WorldTextColor`; `ToString()` is `R G B`.
- `EmptySignSettings` (JSON names in brackets):

| Field | Default | Meaning |
|---|---|---|
| `Distance` (`distance`) | 300 | Units in front of the camera |
| `Shift` (`shift`) | 100 | Units along the camera's right; negative is left |
| `Height` (`height`) | 0 | Units up from the middle of the view; negative is down |
| `TitleScale` / `JoinScale` (`titleScale` / `joinScale`) | 1.2 / 0.45 | World units per pixel (64 px font: about 77 / 29 units tall) |
| `Gap` (`gap`) | 8 | Units between the middle and each line |
| `TitleColor` / `JoinColor` (`titleColor` / `joinColor`) | 0 190 0 / 0 190 0 | Solid green: the area under mid is bright beige, where white washed out and dark navy read worse |
| `TitleText` / `JoinText` (`titleText` / `joinText`) | `RIFT ROULETTE` / `Join with connect 66.94.102.245:27015` | The two lines (`DefaultTitle` / `DefaultJoin`) |

## Operations

| Operation | Result |
|---|---|
| `Default` | A settings value with every default |
| `Problem()` | Null when valid, else a player-readable reason: distance 50-3000, shift and height within ±3000, sizes 0.05-10, gap 0-500, colors 0-255, text at most 120 characters |
| `Describe()` | One line with every value, for replies and logs |
| `ToJson()` / `FromJson(json)` | Indented JSON. Missing fields keep their defaults; `null` or broken JSON throws `JsonException` |

## Invariants

- `Problem()` is checked before anything is saved or drawn
  (`EmptySign.Change`, `EmptySignStore` load).
