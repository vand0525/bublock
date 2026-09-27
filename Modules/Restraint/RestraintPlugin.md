# RestraintPlugin

Thin host for `RestraintService`: the per-frame hook and admin commands.
Name `Restraint`; log feature `Restraint` (`restraint-YYYYMMDD.log`).

## Hook

- `OnGameFrame(simulating, ...)`: calls `RestraintService.Sustain()` on
  simulating frames only.

## Commands (all admin, `AdminCommand.Authorize`, Debug mode)

| Command | Effect |
|---|---|
| `/restrain <slot>` | `RestraintService.Restrain`. |
| `/restrain_release <slot>` | `RestraintService.Release`. |
| `/restrain_list` | `RestraintService.Describe` lines. |
| `/status_add <slot> <modifier> [seconds=10]` | Adds any game modifier by name for testing (`RestraintService.AddModifier`); logs Info with the result. Refuses a dead pawn. |
| `/status_remove <slot> <modifier>` | `pawn.RemoveModifier(modifier)`. |

Unknown slots throw `CommandException("No player in slot N.")`.

## Side effects

- Restraint lasts until released. In Rift Roulette the game also restrains
  and releases players on its own (watch spot / move into the rift), so a
  manual `/restrain_release` up top is re-applied the next time the player
  is sent up.
