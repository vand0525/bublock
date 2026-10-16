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
| `WakeGapSeconds` | 180 | A gap this long between 60 s checks means the server was hibernating and just woke |
| `WakeHoldSeconds` | 600 | No uptime reload within 10 min of a wake |

## Operations

| Op | Returns |
|---|---|
| `Reason(enabled, participants, stuckJoins, joinsInProgress, uptimeSeconds, sinceWakeSeconds)` | `null` (stay up) when off, anyone plays (`participants > 0`), or under `MinUptimeSeconds`. Otherwise `StuckJoin` when any join got stuck since the map started (even with that client still connecting, and even right after a wake), else `Uptime` at 3 h when no join is in progress and the last wake is at least `WakeHoldSeconds` ago, else `null` |
| `IsStuck(pendingSeconds)` | `pendingSeconds >= StuckJoinSeconds` |
| `IsWake(secondsSinceLastCheck)` | `secondsSinceLastCheck >= WakeGapSeconds` |

## Wake hold

An empty server hibernates: no ticks, so no timers and no checks. Only a
connecting client wakes it, and the first check after the wake runs
before `OnClientConnect` has recorded the join (`Pending=0`). On
2026-10-14 that check saw 10 h up and no one playing and reloaded the map
in the same second, dropping the joining admin
(`NETWORK_DISCONNECT_CREATE_SERVER_FAILED`). Holding the uptime reload
after a wake lets that join finish.

## Why

Since 2026-09-27 the server has gone into a state where no new player can
finish joining (they connect, never get a pawn, and leave), while players
already in keep playing. Only a full server restart fixed it so far,
after 1.5 to 3.5 hours up. The cause is unknown; a map reload is tried
first because clients reconnect by themselves.
