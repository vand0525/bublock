# EmptySign

The empty-server sign: while nobody plays, two world-text boards in front
of the spectator camera's start spot read `RIFT ROULETTE` and `Join with
connect 66.94.102.245:27015` (layout in `EmptySignLayout`). Static service.

## State

- `Shown`: whether the sign is up. Static: survives a map change (the
  boards do not, so map start redraws with `force`); a hot reload starts
  it false and the reload's redraw forces it.

## Operations

| Operation | Effect |
|---|---|
| `Refresh(mode, force = false)` | Empty means `Participants.Humans()` is empty (seated admins and ban statues don't count). Without `force`, does nothing while the state is unchanged. Empty: creates `board.empty.title` (`EmptySignLayout.Title()`) and `board.empty.join` (`EmptySignLayout.Join()`); `WorldTextService.Create` replaces boards with the same id. Not empty: removes both, unless `force` (the redraw already cleared every board). Logs Information `Empty sign shown Humans=` / `Empty sign removed Humans=` on a change, Debug `Empty sign redrawn` on a forced redraw without a change |

## Called from

- `BoardsPlugin.OnLoad`: every `CheckSeconds` (2 s), Clean.
- `BoardService.Redraw` (`force: true`), after it cleared every board:
  map start, hot reload, `/board_redraw`, `/board_note`, watch spot side
  change, match end, mode change.

## Logs

`Boards` feature log (`boards-YYYYMMDD.log`).

## Invariants

- The sign is up only while there are no participants; it goes within 2 s
  of someone joining and comes back within 2 s of the last one leaving.
- Only these two board ids; never touches other boards.
