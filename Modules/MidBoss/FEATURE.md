# MidBoss — Feature

## Purpose

Reusable, game-mode-agnostic control of Deadlock's mid-boss
(`npc_super_neutral`, C++ `CNPC_MidBoss`) and the Rejuvenator crystal it
drops (`citadel_item_pickup_rejuv`). This stage is discovery: small
isolated admin commands to spawn, tune and watch the boss on the private
dev server, so each mechanic is proven in game before a mode builds on it.
No round logic, no automatic spawns.

## What is known (see `RiftRoulette/reference/resources.md`)

- Spawned by the map's `info_neutral_trooper_camp` (subclass
  `neutral_camp_midboss`) at about (0, 0, -768); CleanSlate parks that
  scheduler (`citadel_midboss_initial_spawn_time_override 999999`) and sets
  `citadel_npc_spawn_enabled 0`.
- A boss spawned at game minute m has 13,000 + 195 × m health (fixed at
  spawn), 15 health/s regen, and a damage-resistance modifier (threshold
  35 + 5 per game minute) that zeroes hits before `OnTakeDamage`; roars at
  50% (not from written health).
- Only players inside `trigger_midboss_shield` can damage it.
- In-game probe record: `RiftRoulette/reference/probes/2026-10-13-midboss.md`.

## Killable boss (`/midboss_ready`)

One command puts a fresh boss in the state a mode wants: spawned at the
camp, resistance removed, max health from real-match data for the
average souls per player (`MidBossRule.HealthForSouls`), health at 51%,
and damage scaled by fighter count (12 = 1×). The boss is never killed
by code; players kill it.

## Public operations

| Op | Doc |
|---|---|
| `MidBossService.Rescan / ObserveSpawned / ObserveDeleted / ObserveKilled / WatchCounts` | `MidBossService.md` |
| `MidBossService.ScaleHit / SetDamageScale` | `MidBossService.md` |
| `MidBossService.Spawn / Schedule` | `MidBossService.md` |
| `MidBossService.SetHealth / SetMaxHealth / SetPercent / RemoveModifier / Remove` | `MidBossService.md` |
| `MidBossService.Ready / Recheck / CountFighters` | `MidBossService.md` |
| `MidBossService.BossKilled / CrystalTaken` events, `ClearRejuv / HasRebirth / RemoveBosses / RemoveCrystals` | `MidBossService.md` |
| `MidBossService.Describe / Report` | `MidBossService.md` |
| `MidBossRule` constants and math | `MidBossRule.md` |

## State

`MidBossService` holds tracked boss and crystal handles, the damage scale
and its hit totals, and the last seen kill / crystal counts.

## Units

| File | Role |
|---|---|
| `MidBossRule.cs` | Names, convars, camp spot, scale and health math, souls-to-minute table (unit tested) |
| `MidBossService.cs` | Tracking, spawn routes, health, damage scale, ready, remove, status |
| `MidBossPlugin.cs` | Watcher hooks and `/midboss_*` admin commands |
| `MidBoss.projitems` | Rule + service (no commands) |
| `MidBossCommands.projitems` | Plugin class |

## Lifecycle vs commands

- Watchers (spawn, delete, kill, respawn events, count changes, damage
  scale) run from hooks in Clean mode and only log, except `ScaleHit`,
  which changes damage when an admin set a scale other than 1.
- Every change to a boss comes from an admin command in Debug mode. See
  `RiftRoulette/reference/admin-commands.md` (MidBoss).

## Consumers

- `CrystalCrackers.dll` imports both projitems. Its pit round
  (`CrystalCrackers/Pit`) calls `Ready`, `RemoveBosses`, `RemoveCrystals`,
  `ClearRejuv` and `HasRebirth`, and listens to `BossKilled` /
  `CrystalTaken`.
