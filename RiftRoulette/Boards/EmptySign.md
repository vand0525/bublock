# EmptySign

The empty-server sign: while nobody plays, two world-text boards in front
of the spectator camera's start spot (by default `RIFT ROULETTE` and `Join
with connect 66.94.102.245:27015`). Layout in `EmptySignLayout`, look in
`EmptySignSettings`, saved by `EmptySignStore`. Static service.

## State

- `Shown`: whether the sign is up. Static: survives a map change (the
  boards do not, so map start redraws with `force`); a hot reload starts
  it false and the reload's redraw forces it.
- `Preview`: shows the sign even with players on, so an admin can adjust
  it. Not saved: a reload clears it.

## Operations

| Operation | Effect |
|---|---|
| `Refresh(mode, force = false)` | Up when `Participants.Humans()` is empty (seated admins and ban statues don't count) or `Preview` is on. Without `force`, does nothing while that is unchanged. Up: creates `board.empty.title` and `board.empty.join` from `EmptySignStore.Current` (`WorldTextService.Create` replaces boards with the same id). Down: removes both, unless `force` (the redraw already cleared every board). Logs Information `Empty sign shown Humans= Preview=` / `Empty sign removed Humans=` on a change, Debug `Empty sign redrawn` on a forced redraw without a change |
| `Change(change, mode)` | Applies `change` to the current settings; returns `Problem()` without saving when a value is out of range. Else saves (`EmptySignStore.Set`), redraws both boards if the sign is up, logs Information `Empty sign changed Settings= Shown=`, returns null. Exceptions thrown by `change` (a bad line name) propagate before anything is saved |
| `SetPreview(on, mode)` | Sets `Preview`, logs Information `Empty sign preview set`, then `Refresh(mode)` |

## Called from

- `BoardsPlugin.OnLoad`: `Refresh` every `CheckSeconds` (2 s), Clean.
- `BoardService.Redraw` (`force: true`), after it cleared every board:
  map start, hot reload, `/board_redraw`, `/board_note`, watch spot side
  change, match end, mode change.
- `BoardsPlugin` `/sign_*` commands (Debug): `Change`, `SetPreview`.

## Logs

`Boards` feature log (`boards-YYYYMMDD.log`).

## Invariants

- The sign is up only while there are no participants or the preview is
  on; it goes within 2 s of someone joining and comes back within 2 s of
  the last one leaving.
- Only these two board ids; never touches other boards.
- Never draws settings that fail `Problem()`.
