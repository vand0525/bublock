# BoardService

Draws every board at the watch spot. Static; no state of its own.

## Operations

| Op | Behavior |
|---|---|
| `Redraw(mode)` | `WorldTextService.ClearAll()`, then `board.welcome` ("RIFT ROULETTE", `BoardLayout.Welcome`), `board.hint` right under it (`BoardLayout.Hint(AboutHint)`), `board.note` under the hint (`BoardLayout.Note(WelcomeNoteStore.Text)`, skipped when the note is empty), then the stats boards (`StatsService.RefreshBoards`) and the betting board (`BettingService.RefreshBoard`, Random mode only). Debug log `Boards redrawn Mode=` |

`AboutHint` (public const): `Type /about to learn how to play and bet`.

## Called from

- `BoardsPlugin`: load (hot reload), map start, `/board_redraw`, `/board_note`.
- `Round/WatchSpot.RefreshBoards` when the watch spot changes side.
- `GameLoop/MatchService.End` and `SetHeroMode`, after players return to
  the lobby hero.

## Logs

`Boards` feature log (`boards-YYYYMMDD.log`).

## Dangerous constraints

- Clears **every** `point_worldtext` on the map, not only these boards.
- Game-thread only.
