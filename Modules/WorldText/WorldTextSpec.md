# WorldTextSpec

Immutable description of one text board.

| Field | Meaning | Default |
|---|---|---|
| `Text` | Message; `\n` is a line break | — |
| `Position` | World position | — |
| `Angle` | Board angles (pitch, yaw, roll); applied with `Teleport` after create | — |
| `Color` | `WorldTextColor` | — |
| `WorldUnitsPerPx` | Scale (archive boards: 3 for the title, 0.8 for team boards) | — |
| `FontSize` | Pixels | 64 (archive value) |
| `ReorientMode` | 0 = fixed orientation | 0 (archive value) |

Defaults match the archive `CreateBoard`, so a spec built from the archive
arguments renders identically.
