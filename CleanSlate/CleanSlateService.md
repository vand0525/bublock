# CleanSlateService

Static service holding the CleanSlate map-cleanup operations. Used by
`CleanSlatePlugin` at startup and hot reload (Clean) and by `/cleanup_run`
(Debug).

## Operations

| Operation | Effect | Returns |
|-----------|--------|---------|
| `ApplyConvars(mode)` | Spawns off: `citadel_trooper_spawn_enabled 0`, `citadel_npc_spawn_enabled 0`, `citadel_active_lane 0`, `citadel_midboss_initial_spawn_time_override 999999`. Urn (the game's "crate") off: `citadel_crate_spawn_enabled 0`, `citadel_crate_disable_early_spawn 1`, `citadel_crate_spawn_initial_delay 999999`, `citadel_crate_respawn_interval 999999`. All via `ServerConVars.TrySet`; a missing convar is skipped and logs one `Convar missing` Warning (copied to master) | — |
| `RemoveMapEntities(mode)` | Removes every entity whose `DesignerName` is in `RemovedDesignerNames`; sends `AcceptInput("Disable")` to every entity in `DisabledDesignerNames` | `CleanupResult` (counts per designer name) |

`RemovedDesignerNames`: `npc_trooper_boss`, `npc_boss_tier2`,
`npc_barrack_boss`, `citadel_item_powerup_spawner`,
`citadel_herotest_orbspawner`, `citadel_shop_prop_dynamic`
(the 8 shop kiosk models on `dl_midtown`).

`DisabledDesignerNames`: `trigger_item_shop` (9 buy zones, including a base
shop) and `trigger_item_shop_safe_zone` (2, center shops).

## Logging

`cleanup` log (`[CleanSlate.Cleanup]`):
- `Map entities cleaned Summary=Removed=[...] Disabled=[...]` (Information).
- `Removing ...` / `Disabling DesignerName=... EntityName=...` per entity and
  `Spawn convars applied` (Debug; only written in Debug mode).

## Invariants and constraints

- Iterates a copy (`Entities.All.ToList()`) because it removes while
  iterating. `Entities.All` is a property.
- Never add `info_super_trooper_spawn` (crash risk) or `item_crate_spawn`
  (urn spawn points the game picks from) to the removal list. The urn is off
  by convars only; `citadel_trigger_idol_return` stays too.
- Shop buy zones are disabled, not removed: the shop system may still look
  them up. `citadel_trigger_shop_tunnel` is left alone (not a shop).
- CleanSlate never touches `citadel_allow_purchasing_anywhere`; RiftRoulette
  (`GameLoop/ShopAccess`) owns buying.
- Mode changes logging only, never which entities are removed or disabled.
