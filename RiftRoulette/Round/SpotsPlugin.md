# SpotsPlugin

Thin plugin class (Name `Rift Roulette Spots`) for the per-slot spot admin
commands. Ops live on `SpotCheck`. No hooks.

## Commands

| Command | Who | Does |
|---|---|---|
| `/spots_list [green\|yellow]` | admin | `SpotCheck.Describe` for the side, or both sides; one console line per slot with its watch, Sapphire and Amber positions |
| `/spots_walk <watch\|sapphire\|amber> [green\|yellow]` | admin, in game only (not the server console) | `SpotCheck.Walk` for the caller; side defaults to the current watch-spot side. Unknown group or side, or a server-console caller: `CommandException` |

Both call `AdminCommand.Authorize` first (logged in `Spots`) and run in
Debug mode.
