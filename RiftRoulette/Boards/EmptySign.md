# EmptySign

The empty-server sign: while nobody plays, two world-text boards in front
of the spectator camera's start spot (by default `RIFT ROULETTE` and `Join
with connect 162.250.125.66:11074`). Layout in `EmptySignLayout`, look in
`EmptySignSettings`, saved by `EmptySignStore`. Static service.

## State

- `Shown`: whether the sign should be up, as of the last refresh. Static:
  it survives a map change but the boards do not, so `Refresh` never
  trusts it to decide; it checks the boards themselves
  (`WorldTextService.IsLive`). `Shown` only picks the log line and lets
  `Change` redraw at once.
- `Preview`: shows the sign even with players on, so an admin can adjust
  it. Not saved: a reload clears it.

## Operations

| Operation | Effect |
|---|---|
| `Refresh(mode, force = false)` | Up when `Participants.Humans()` is empty (seated admins and ban statues don't count) or `Preview` is on. Without `force`, does nothing while the boards that exist already match (both live when up, neither when down). Up: creates `board.empty.title` and `board.empty.join` from `EmptySignStore.Current` (`WorldTextService.Create` replaces boards with the same id). Down: removes whichever is live. Logs Information `Empty sign shown Humans= Preview=` / `Empty sign removed Humans=` on a change, Information `Empty sign repaired ... TitleLive= JoinLive=` when the boards went missing without a change (map change, `/wt_clear`), Debug `Empty sign redrawn` on a forced redraw. Never throws: an exception logs Error `Empty sign refresh failed` and the next 2 s check tries again |
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
  the last one leaving, or of being wiped (map change, restart,
  `/wt_clear`).
- Only these two board ids; never touches other boards.
- Never draws settings that fail `Problem()`.
