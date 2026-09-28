# SessionPlugin

Small Rift Roulette plugin class that records the session lifecycle in the
master log. Lives in `RiftRoulette.dll`.

## Behavior

| Hook | Master log line (`[RiftRoulette]`) |
|------|-----------------------------------|
| `OnLoad` | `Session started Reload=<bool> LogFolder=<path>` |
| `OnStartupServer` | `Server startup Map=<Server.MapName>` |
| `OnUnload` | `Session ending (plugin unload)` |

Every line carries the DLL's `session=` id, so one session's story can be
followed across all Rift Roulette feature logs.

`OnLoad` also calls `RiftRouletteLocations.RegisterAll()`, registering the
draft and rift-start locations by name in `MovementService.Locations`.

## Commands

| Command | Who | Does |
|---|---|---|
| `/session_info` | admin | Replies with `Session=`, `Round=` (`-` when none), `Map=` (`Server.MapName`), `LogFolder=` (`BublockLog.Directory`), `FileLogging=on/OFF` |

Gated with `AdminCommand.Authorize` (accepted / rejected calls logged in the
`Session` feature log). Read-only.

## Side effects

- Writes to `bublock/logs/RiftRoulette/master-YYYYMMDD.log` (hooks) and
  `session-YYYYMMDD.log` (command gate).
- Registers the Rift Roulette locations (names reachable from `/mv_tp`,
  `/mv_list`).
- No gameplay changes.

## Invariants

- Round ids are set by `Rift/RiftService`
  (`r<n>` while a rift runs); `/session_info` shows the current one.
