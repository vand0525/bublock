# DuelPlugin

Thin plugin class for 1v1 mode (`Name` = "Rift Roulette Duel").

## Hooks

- `player_respawned` and `player_spawn`: in 1v1 mode, for participants only,
  on the next tick finds the player again by Steam ID. During a match it
  calls `DuelService.ApplyPending` (re-apply after a death or a hero-swap
  kill); otherwise `DuelService.GrantSetup` (100,000 souls, level 36).

## Commands

Player (Clean, reply in chat, then `AutoStartService.Check`):

| Command | Calls | Reply |
|---|---|---|
| `/queue` | `DuelService.JoinQueue` | `<name> joined the 1v1 queue at position N of M.`, the current position if already queued, or the refusal (not 1v1 mode, not playing) |
| `/unqueue` | `DuelService.LeaveQueue` | `<name> left the 1v1 queue.`, or the refusal (not queued, fighting now, fights next round) |

Admin: `AdminCommand.Authorize` with the `Duel` log (server console
trusted), Debug mode, `[Duel]` replies.

| Command | Calls | Reply / errors |
|---|---|---|
| `/duel_copy <slot>` | `DuelService.Copy` | `Copied <hero> from <name>. Match started...`, or the refusal (not 1v1, match running, not playing, dead, fewer than 2 queued); error if the slot is empty |
| `/duel_clear` | `DuelService.ClearSnapshot` | `1v1 build cleared...`; refused during a match |
| `/duel_status` | `DuelService.Describe` | config / lock / queue line, build, items, one line per participant |
| `/duel_queue` | `DuelService.DescribeQueue` | pairing, then the queue in order with fighting / king tags |
| `/duel_queue_add <slot>` | `DuelService.JoinQueue` (Debug), then `AutoStartService.Check` | same replies as `/queue` |
| `/duel_queue_remove <slot>` | `DuelService.LeaveQueue(force: true)` (Debug), then `AutoStartService.Check` | removed, or refused mid-fight (`/rift_cancel` first). Removing a fighter between rounds makes the next pair fight after one more intermission |
