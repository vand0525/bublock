# MidBossPlugin

Thin host for `MidBossService`: watcher hooks plus the `/midboss_*` admin
commands. Every command checks `AdminAuth` (server console trusted), runs
in Debug mode and replies `[MidBoss]` lines to the caller's console.

## Hooks

| Hook | Calls |
|---|---|
| `OnLoad` (first load and hot reload) | `Rescan`, then `WatchCounts` every 1 s (`Timer.Every`) |
| `OnStartupServer` | `Rescan` |
| `OnEntitySpawned` / `OnEntityDeleted` | `ObserveSpawned` / `ObserveDeleted` |
| `OnTakeDamage` | `ScaleHit` (always `Continue`; Restraint's `Stop` still wins) |
| `entity_killed` | `ObserveKilled` |
| `midboss_respawn_pending` | `ObserveRespawnPending(SpawnTime)` |
| `midboss_respawned` (untyped `GameEvent`) | `ObserveRespawned` |

## Commands

| Command | Calls |
|---|---|
| `/midboss_status` | `Describe` |
| `/midboss_spawn [here \| x y z]` | `Spawn` at `MidBossRule.CampSpot`, the caller's hero, or the point; replies again 1 s later with `Report` (skipped if the caller left) |
| `/midboss_schedule <seconds\|off>` | `MidBossRule.TryParseSchedule`, then `Schedule` |
| `/midboss_health <hp>` | `SetHealth` (1 to 1,000,000) |
| `/midboss_maxhealth <hp>` | `SetMaxHealth` (1 to 1,000,000) |
| `/midboss_percent <pct>` | `SetPercent` (above 0, at most 100) |
| `/midboss_damage_scale <k>` | `SetDamageScale` (clamped 0.1–10) |
| `/midboss_modifier_remove <name>` | `RemoveModifier` |
| `/midboss_ready <souls> [fighters]` | `Ready` (souls 0–1,000,000; fighters 1 or more, left out or 0 to count), two reply lines, then `Recheck` 1 s later (reply skipped if the caller left) |
| `/midboss_remove` | `Remove` |

Bad input and "no mid-boss tracked" throw `CommandException`.
