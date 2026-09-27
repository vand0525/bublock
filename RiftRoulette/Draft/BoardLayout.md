# BoardLayout

Where the draft-area boards sit (Stage 13c, extracted from `DraftService`).
Positions are offsets from the watch spot the boards are drawn at
(`Round/WatchSpot.Location(WatchSpot.BoardSide)`, Stage 13i), so the boards
move with the watch spot between rounds.

Offsets and angles below are the green ones (the archive's, unchanged). On
yellow every board goes through `Round/WatchLayout`: offsets rotate half a
turn around the watch spot (x and y negated) and yaw gets +180, so the
layout looks the same from the watch spot on both lanes and the welcome
board always sits toward the map edge.

## Operations

| Op | Returns |
|---|---|
| `Origin` | Current board anchor (watch spot position) |
| `Welcome(text)` | `WorldTextSpec`: white, scale 3, angle (0, -90, 90), origin + `WatchLayout.WelcomeOffset` (500, 500, 300) |
| `Note(text)` | Like `Welcome` but `NoteDrop` (120) units lower and scale `NoteFontScale` (1.2): a line under the RIFT ROULETTE sign (first guess, tune in game) |
| `Side(team, text)` | Sapphire: (0,150,255), angle (0, 360, 90), origin + (-90, 500, 0). Any other team: Amber, (255,70,0), angle (0, 180, 90), origin + (90, -500, 0). Scale `SideFontScale` (0.8) |

## Used by

- `DraftService.RedrawBoards`: welcome board, the note under it, and the
  Draft mode pool boards.
- `Stats/StatsService.RefreshBoards`: the Random mode stats boards, in the
  same spots as the pool boards.

## Invariants

- The side boards turn with the layout, so on yellow the Sapphire board
  hangs over the Amber half (same place relative to the viewer as on green).
- The watch view angles in `RiftRouletteLocations` aim at the welcome board;
  change `WatchLayout.WelcomeOffset` and those angles together (a test
  checks them).
