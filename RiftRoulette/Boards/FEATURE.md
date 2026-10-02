# Boards

The world-text boards around the watch spot: the RIFT ROULETTE welcome
sign, the `/about` hint and the admin note under it, plus the placement
used by the stats and betting boards.

## Files

| File | Role |
|---|---|
| `BoardLayout.cs` | Where each board sits (welcome, hint, note, side, back), relative to the watch spot, mirrored per lane through `Round/WatchLayout` |
| `WelcomeNoteStore.cs` | The note under the welcome board, kept in `bublock/welcomenote.txt` |
| `BoardService.cs` | `Redraw`: clear every board, draw welcome / hint / note, then the stats and betting boards |
| `BoardsPlugin.cs` | Redraw on load and map start; admin `/board_redraw`, `/board_note` |

## Public operations

- `BoardService.Redraw(mode)`
- `WelcomeNoteStore.Text`, `WelcomeNoteStore.Set(text, mode)`
- `BoardLayout.Welcome / Hint / Note / Side / Back(text)`

## State

- `WelcomeNoteStore`: the note (seeded from the file once per load).
- Board ids: `board.welcome`, `board.hint`, `board.note`; the stats
  (`stats.sapphire`, `stats.amber`) and betting boards belong to their
  features.

## Lifecycle vs commands

- Lifecycle (Clean): `BoardsPlugin` redraws on hot reload and map start;
  `Round/WatchSpot.RefreshBoards` redraws when the watch spot changes side;
  `MatchService` redraws after match end and mode change.
- Admin (Debug): `/board_redraw`, `/board_note`.

## Logs

`Boards` feature log (`boards-YYYYMMDD.log`).
