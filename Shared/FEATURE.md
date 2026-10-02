# Shared — Feature

## Purpose

Cross-cutting helpers for every Bublock plugin DLL. Not a plugin; has no
hooks or commands.

## Public operations

| Type | Operation | Doc |
|------|-----------|-----|
| `AdminAuth` | `IsAuthorized(ulong steamId)` | `Auth/AdminAuth.md` |
| `AdminCommand` | `Authorize(caller, log, command)` (null caller trusted; rejection logged + `CommandException`), `Reply(caller, message)` | `Auth/AdminCommand.md` |
| `PlayerChat` | `Send(player, message)` (chat line to one player) | `Chat/PlayerChat.md` |
| `Cheats` | `Run(Action action)` | `Cheats/Cheats.md` |
| `ServerConVars` | `TrySet(name, value, log)` (sets a convar; one Warning per missing name) | `ConVars/ServerConVars.md` |
| `ExecutionMode` | `Clean` / `Debug` enum | `Execution/ExecutionMode.md` |
| `BublockLog` | `Master`, `For(feature)`, `SessionId`, `RoundId`, `Directory` | `Logging/BublockLog.md` |
| `Logger` | `Trace`..`Critical` (optional `PlayerRef`, `Exception`), `WithMode` | `Logging/Logger.md` |
| `PlayerRef` | affected player (slot, Steam ID, name); `controller.ToPlayerRef()` | `Logging/PlayerRef.md` |

Supporting logging internals: `LogHub`, `LogFormatter`, `RollingFileWriter`,
`LogRetention`, `LogPaths`, `LogLevel` (each with a sibling `.md`).

Namespace: `Bublock.Shared`.

## Logging contract

- Server folder: `game/bin/win64/bublock/logs/<Dll>/`
  (`/server/game/bin/win64/bublock/logs/` on this server).
- Files: `master-YYYYMMDD.log` plus one `<feature>-YYYYMMDD.log` per
  feature; size roll at 10 MB (`.1`, `.2`, ...); 7-day retention on load.
- Line: UTC timestamp, `[Level]`, `[<Dll>.<Feature>]` prefix, `session=`,
  `round=`, optional `player="Name" steam=<id> slot=<n>`, then the message.
- Feature `Warning`+ lines are copied to master with `(see <feature>.log)`.
- Clean mode: Information+. Debug mode (`WithMode(ExecutionMode.Debug)`):
  Trace+.
- Console output only when file logging fails (one line, then disabled).

## State

- `AdminAuth` holds the static authorized Steam ID set.
- `BublockLog` holds one lazily created `LogHub` per DLL (open files,
  session id, round id); closed when the DLL's load context unloads.
- Each consuming DLL has its own copy of these statics (Deadworks isolates
  each plugin DLL in its own load context).

## Consumers

- `RiftRoulette.dll`, `DevTools.dll`, `CleanSlate.dll` import `Shared.projitems`.
- `Tests/Shared.Tests` imports it for local tests.
- Every admin command (DevTools, CleanSlate, Rift Roulette plugin classes)
  gates with `AdminCommand.Authorize`.
- `PlayerChat` is used by Rift Roulette plugin classes and services for
  chat lines to one player (e.g. `BettingPlugin` bet replies, `LobbyPlugin`
  `/status`).

## Relation to lifecycle vs commands

- Admin command wrappers call `AdminCommand.Authorize` before running and
  run their ops with `ExecutionMode.Debug`; lifecycle code uses the default
  Clean loggers.
- `Cheats.Run` is available to any operation that needs `sv_cheats`; no
  caller uses it.
