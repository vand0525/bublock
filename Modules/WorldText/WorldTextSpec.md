# WorldTextSpec

Immutable description of one text board.

| Field | Meaning | Default |
|---|---|---|
| `Text` | Message; `\n` is a line break | — |
| `Position` | World position | — |
| `Angle` | Board angles (pitch, yaw, roll); applied with `Teleport` after create | — |
| `Color` | `WorldTextColor` | — |
| `WorldUnitsPerPx` | Scale (3 for a title board, 0.8 for team boards) | — |
| `FontSize` | Pixels | 64 |
| `ReorientMode` | 0 = fixed orientation | 0 |
