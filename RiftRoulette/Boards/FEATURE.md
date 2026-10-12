# Boards

The world-text boards around the watch spot: the RIFT ROULETTE welcome
sign, the `/about` hint and the admin note under it, plus the placement
used by the stats and betting boards. Also the empty-server sign
(`RIFT ROULETTE` / `Join with connect 66.94.102.245:27015`) in front of the
spectator camera's start spot under mid, up only while nobody plays.

## Files

| File | Role |
|---|---|
| `BoardLayout.cs` | Where each board sits (welcome, hint, note, side, back), relative to the watch spot, mirrored per lane through `Round/WatchLayout` |
| `WelcomeNoteStore.cs` | The note under the welcome board, kept in `bublock/welcomenote.txt` |
| `BoardService.cs` | `Redraw`: clear every board, draw welcome / hint / note, then the stats and betting boards and the empty-server sign |
| `EmptySignLayout.cs` | Where the empty-server sign sits: centered in front of the spectator camera's start spot, facing it (tested) |
| `EmptySignSettings.cs` | The sign's adjustable look: distance, shift, height, per-line offsets, sizes, gap, colors, text; range checks and JSON (tested) |
| `EmptySignStore.cs` | Saves those settings in `bublock/emptysign.json` |
| `EmptySign.cs` | Shows the empty-server sign while there are no participants (or the admin preview is on), removes it otherwise; applies setting changes |
| `BoardsPlugin.cs` | Redraw on load and map start; empty-sign check every 2 s; admin `/board_redraw`, `/board_note`, `/sign_*` |

## Public operations

- `BoardService.Redraw(mode)`
- `EmptySign.Refresh(mode, force)`, `EmptySign.Change(change, mode)`,
  `EmptySign.SetPreview(on, mode)`
- `EmptySignStore.Current`, `EmptySignStore.Set(settings, mode)`
- `WelcomeNoteStore.Text`, `WelcomeNoteStore.Set(text, mode)`
- `BoardLayout.Welcome / Hint / Note / Side / Back(text)`
- `EmptySignLayout.Title / Join(settings)`

## State

- `WelcomeNoteStore`: the note (seeded from the file once per load).
- `EmptySignStore`: the sign settings (seeded from
  `bublock/emptysign.json` once per load).
- `EmptySign.Shown`: whether the empty-server sign is up;
  `EmptySign.Preview`: admin preview (not saved).
- Board ids: `board.welcome`, `board.hint`, `board.note`,
  `board.empty.title`, `board.empty.join`; the stats
  (`stats.sapphire`, `stats.amber`) and betting boards belong to their
  features.

## Lifecycle vs commands

- Lifecycle (Clean): `BoardsPlugin` checks the empty-server sign every 2 s
  (`EmptySign.Refresh`); `BoardsPlugin` redraws on hot reload and map start;
  `Round/WatchSpot.RefreshBoards` redraws when the watch spot changes side;
  `MatchService` redraws after match end and mode change.
- Admin (Debug): `/board_redraw`, `/board_note`; the empty-server sign's
  `/sign_status`, `/sign_distance`, `/sign_shift`, `/sign_height`,
  `/sign_offset`, `/sign_size`, `/sign_gap`, `/sign_color`, `/sign_text`,
  `/sign_preview`, `/sign_reset` (every change saved to
  `bublock/emptysign.json` and redrawn at once if the sign is up).

## Logs

`Boards` feature log (`boards-YYYYMMDD.log`).
