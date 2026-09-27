# CleanSlate — Feature

## Purpose

Strip default lane/NPC/midboss/urn/shop noise so custom modes (e.g. Rift
Roulette) can run on a cleaner map.

## Current state

- `CleanSlatePlugin` (hooks + command), `CleanSlateService` (operations) and
  `CleanupResult` (counts) under `Bublock/CleanSlate/`, derived from the
  archive snapshot.
- Compiles in `Bublock/Shared/` only; logs to `bublock/logs/CleanSlate/`
  (`master` + `cleanup`).
- Ships as `CleanSlate.dll` (replaces the archive DLL of the same name).

## Public operations

- `CleanSlateService.ApplyConvars(mode)` — spawn/lane/midboss/urn convars.
- `CleanSlateService.RemoveMapEntities(mode)` — removes the fixed removal
  list (lane bosses, spawners, shop kiosks), disables the shop buy zones,
  returns a `CleanupResult`.

## Composition

- Lifecycle: `OnStartupServer`, and `OnLoad(isReload: true)` after a hot
  reload, run both in Clean mode (removals 2 s later). No banner.
- Admin: `/cleanup_run` runs both immediately in Debug mode.

## State

None beyond the pending 2-second startup timer.
