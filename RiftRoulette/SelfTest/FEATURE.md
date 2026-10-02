# SelfTest

## Purpose

Tells within minutes what a Deadlock / Deadworks update broke. One
read-only command checks every live game dependency (convars, raw schema
fields, entity names, map rift points, hero and item data, floors under the
spots, player controllers, hook calls); a second exercises teleport,
restraint, banner and loadout reading on one player. Part of the patch-day
runbook (`reference/patch-day.md`), next to the offline
`scripts/patch-check.py`.

## Files

| File | Role |
|---|---|
| `SelfTestResult.cs` | `CheckStatus`, `CheckResult`, `SelfTestReport` (pure) |
| `GameDependencies.cs` | The convars, entities and events checked (pure data) |
| `EventCounters.cs` | Hook call counts since load |
| `SelfTestService.cs` | `Run` and `Live` |
| `SelfTestPlugin.cs` | `/selftest_run`, `/selftest_live` |

## Public operations

- `SelfTestService.Run(mode)`: all read-only checks.
- `SelfTestService.Live(caller, player, timer, mode)`: live checks on one
  player, between rounds.
- `EventCounters.Hit(name)`: called by the Lobby, Random and GameLoop
  hooks.

## State

`EventCounters` only (reset on load).

## Lifecycle vs commands

Nothing runs on its own; the lifecycle only feeds `EventCounters`. Both
commands are admin-only and run in Debug mode.

## Keeping it current

A new game dependency (convar, entity name, schema field, modifier, event)
goes into `GameDependencies` (or a check in `SelfTestService`) and into
`reference/patch-day.md` section 5 in the same change.
