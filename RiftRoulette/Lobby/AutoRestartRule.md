# AutoRestartRule

Pure decision for the automatic map reload (`AutoRestartService`). No
Deadworks calls; linked into the test project.

## Constants

| Name | Value | Meaning |
|---|---|---|
| `StuckJoin` / `Uptime` | `stuck-join` / `uptime` | Reasons, as logged |
| `MinUptimeSeconds` | 600 | No reload within 10 min of a map start (stops a loop if a reload does not clear stuck joins) |
| `MaxUptimeSeconds` | 10,800 | Reload after 3 h up |
| `StuckJoinSeconds` | 180 | A join still connecting after 3 min counts as stuck |

## Operations

| Op | Returns |
|---|---|
| `Reason(enabled, participants, stuckJoins, joinsInProgress, uptimeSeconds)` | `null` (stay up) when off, anyone plays (`participants > 0`), or under `MinUptimeSeconds`. Otherwise `StuckJoin` when any join got stuck since the map started (even with that client still connecting), else `Uptime` at 3 h when no join is in progress, else `null` |
| `IsStuck(pendingSeconds)` | `pendingSeconds >= StuckJoinSeconds` |

## Why

Since 2026-09-27 the server has gone into a state where no new player can
finish joining (they connect, never get a pawn, and leave), while players
already in keep playing. Only a full server restart fixed it so far,
after 1.5 to 3.5 hours up. The cause is unknown; a map reload is tried
first because clients reconnect by themselves.
