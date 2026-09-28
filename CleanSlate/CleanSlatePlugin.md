# CleanSlatePlugin

Thin plugin host for map cleanup: disables trooper/NPC/midboss/urn spawning,
removes lane bosses, powerup spawners and shop kiosks, and disables the shop
buy zones shortly after startup. The work lives in `CleanSlateService`.
Shows no banner (players don't need one).

## Hooks

- `OnLoad(isReload)`: master `Loaded Reload=`. On a hot reload it also runs
  the startup work (`RunStartup`), because a reload does not call
  `OnStartupServer` and drops the pending timers of the previous load.
- `OnStartupServer` (Clean): `RunStartup`.
- `RunStartup`: `ApplyConvars()` immediately, then `RemoveMapEntities()`
  at 2, 10 and 30 s (`PassSeconds`). Master
  `Map cleanup complete Reload=... Pass=<seconds> Removed=[...] Disabled=[...]`
  for the first pass and for any later pass that removed something. The
  later passes exist because after a map reload (`changelevel`, e.g.
  `/restart_now`) the 2 s pass ran before the lane guardians
  (`npc_trooper_boss`) spawned and left 6 standing; on a cold start
  loading delays the timers, so 2 s was enough. Every pass is idempotent.
  No banner.

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
