# CleanSlatePlugin

Thin plugin host for map cleanup: disables trooper/NPC/midboss/urn spawning,
removes lane bosses, powerup spawners and shop kiosks, and disables the shop
buy zones shortly after startup. The work lives in `CleanSlateService`.

## Status

Derived from `archive/CleanSlate/CleanSlatePlugin.cs`. Stage 4 replaced its
`Console.WriteLine` output with shared file logging; Stage 12 moved the
convars and removals into `CleanSlateService` and added `/cleanup_run`. The
playtest fixes (2026-09-27) added the hot-reload run and the shops / urn
cleanup. The "Map cleaned" banner was removed again (players don't need it).

## Hooks

- `OnLoad(isReload)`: master `Loaded Reload=`. On a hot reload it also runs
  the startup work (`RunStartup`), because a reload does not call
  `OnStartupServer` and drops the pending 2 s timer of the previous load.
- `OnStartupServer` (Clean): `RunStartup`.
- `RunStartup`: `ApplyConvars()` immediately, then after
  `Timer.Once(2.Seconds())` `RemoveMapEntities()` and master
  `Map cleanup complete Reload=... Removed=[...] Disabled=[...]`. No banner.

## Commands

| Command | Who | Effect |
|---------|-----|--------|
| `/cleanup_run` | admin (`AdminCommand.Authorize`, null caller trusted) | `ApplyConvars(Debug)` and `RemoveMapEntities(Debug)` immediately (no 2 s delay); master `Map cleanup re-run Removed=[...] Disabled=[...]`; replies `[CleanSlate] Convars applied, N entities removed, M disabled` |

## Logging

Folder: `bublock/logs/CleanSlate/`.

| Event | File | Prefix |
|-------|------|--------|
| Plugin loaded, map cleanup complete / re-run | `master` | `[CleanSlate]` |
| `/cleanup_run` accepted / rejected, cleanup summary, each entity (Debug) | `cleanup` | `[CleanSlate.Cleanup]` |
