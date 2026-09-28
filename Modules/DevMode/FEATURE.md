# DevMode module

## Purpose

Dev and prod for game types (redock fork). Dev is the sandbox the owner
tests in: no live session, and environment-changing admin commands (bots,
map reload, server commands, teleports, rerolls) are allowed. Prod is the
live session players get; those commands are refused so live players are
never disturbed. Together with `Modules/Session` (pause, resume,
auto-start switch) this lets the experimental server double as a live
CI/CD and UX debugging environment.

## Files

| File | Role |
|---|---|
| `DevRules.cs` | `RunMode`, dev-only refusal text, names (pure, tested) |
| `PositionMemory.cs` | Save / restore where players stood in dev |
| `DebugSnapshot.cs` | State dump to the debug log |
| `DevMode.projitems` | Compiles all three into a consumer |

## How a game type uses it

1. Hold a `RunMode` (start in dev on an experimental server) and set the
   session's `AutoStart` from it (off in dev).
2. `/play`: save dev positions, prod, auto-start on, start the session (or
   `Resume` a paused one).
3. `/stop`: dev, auto-start off, stop the session, restore positions on the
   next tick.
4. `/pause`: `TimedSession.Pause` and `DebugSnapshot.Take`.
5. Dev-only commands call `DevRules.Refusal` first.

Gun Game is the reference (`GunGame/DevPlugin.cs`, `GunGameService`).

## Dependencies

`Modules/Movement`, `Shared`, `DeadworksManaged.Api`.
