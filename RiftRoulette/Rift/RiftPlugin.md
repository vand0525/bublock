# RiftPlugin

Thin plugin class for the rift: admin command wrappers around `RiftService`.
No hooks. Name "Rift Roulette Rift".

## Commands

Every command is admin-only (`AdminCommand.Authorize` with the `Rift` log;
server console trusted; rejections logged as Warning with name and Steam
ID), runs its op in Debug mode, and replies to the caller's console with a
`[Rift]` prefix.

| Command | Calls | Reply / errors |
|---|---|---|
| `/rift_start` | `Round/RoundFlow.RunRound(Timer, Debug)` (the same composed path the lifecycle runs Clean) | `Rift starting on GREEN.`, or the refusal if a rift is running or gamerules cannot be reached |
| `/rift_status` | `RiftService.DescribeRift` | two status lines |
| `/rift_next <green\|yellow>` | `RiftSides.TryParse`, `RiftService.SetNextSide`, then `Round/WatchSpot.MoveAllUp` (everyone alive and the boards move above the new side) | `Next rift: <side>. N player(s) moved to the watch spot.`; error for an unknown side or while a rift is running |
| `/rift_cancel` | `Round/RoundFlow.CancelRound(Timer, Debug)` | summary, or `No rift is running.` |
| `/rift_cleanup` | `RiftService.CleanupRiftTroopers` (every `npc_trooper`) | count removed |

## Invariants

- `Timer` is this plugin's; rift sequences stop if this plugin unloads.
- `/rift_start` is admin-only.
