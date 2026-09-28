# DebugSnapshot

A point-in-time dump for debugging a live session: map, player and bot
counts, then one line per player (slot, name, bot, team, alive and health,
position), then the game type's own lines. Written to `debug-YYYYMMDD.log`
(mirrored by `scripts/pull-logs.sh`), with a master-log line, and returned
for the caller.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Take(reason, gameLines, mode = Debug)` | Builds and logs the snapshot | lines |
