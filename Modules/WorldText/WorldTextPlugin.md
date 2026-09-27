# WorldTextPlugin

Thin Deadworks plugin class (`Name` = `World Text`) exposing the admin
`/wt_*` commands. No hooks and no state; all work is done by
`WorldTextService`.

## Commands

All admin-only (`AdminCommand.Authorize`; server console trusted), run their
op in `ExecutionMode.Debug`, and reply through `AdminCommand.Reply` (caller's
console, or server console). Errors are thrown as `CommandException`, which
Deadworks shows to the caller. Accepted and rejected calls are logged in the
`WorldText` feature log.

| Command | Op | Result |
|---|---|---|
| `/wt_list` | `WorldTextService.List` | Count, then `id | Position | preview` per board |
| `/wt_create <id> <text...>` | `WorldTextService.Create` | Board 150 units in front of the caller's eye, facing them (`WorldTextPlacement`), white, scale 0.8. Replaces an existing board with the same id. In-game only (needs a hero pawn) |
| `/wt_update <id> <text...>` | `WorldTextService.Update` | Changes the text; error if the id is unknown |
| `/wt_remove <id>` | `WorldTextService.Remove` | Removes one board; error if the id is unknown |
| `/wt_clear` | `WorldTextService.ClearAll` | Removes every `point_worldtext` on the map; replies with the count |

Text arguments take the rest of the line (`params string[]`, joined with
spaces); `\n` becomes a line break (`WorldTextFormat.FromArgs`). Quoted text
also works.

## Invariants

- Commands only wrap service ops; never add board logic here.
- Handlers take `CCitadelPlayerController?` so the server console can run
  them (`dw_wt_*`).
