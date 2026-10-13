# MidBossService

Finds, spawns, tunes and watches the mid-boss (`npc_super_neutral`) and its
crystal (`citadel_item_pickup_rejuv`). Discovery stage: every op is reached
from an admin command or a watcher hook; nothing runs it on a schedule
except `WatchCounts`. Logs to `midboss-*.log` (`BublockLog.For("MidBoss")`);
spawns, kills, crystal drops / removals and count changes also go to master.

## State

| Field | Meaning |
|---|---|
| `Bosses` / `Crystals` | Tracked `EntityHandle`s (never indexes); invalid handles are dropped on read |
| `DamageScale` | Multiplier for hits on a boss (default 1) |
| `HitCount`, `DamageBefore`, `DamageAfter` | Hits on a boss and their damage before / after scaling since the last scale change |
| `_counts` | Last seen kill / Amber / Sapphire crystal counts |

## Operations

| Op | Behavior |
|---|---|
| `Rescan(mode)` | Rebuilds tracking from `Entities.ByDesignerName` (load, reload, map start) |
| `ObserveSpawned(entity)` | Tracks a boss (designer or class match) or crystal; master line with health, team, position |
| `ObserveDeleted(entity)` | Untracks; a crystal logs `PickedUp` (`m_bPickedUp`, `unknown` if the field is missing) |
| `ObserveKilled(EntityKilledEvent)` | On a boss: master `Mid-boss killed` with attacker (player name when a hero), team, inflictor, damage bits |
| `ObserveRespawnPending(spawnTime)` / `ObserveRespawned()` | Feature log lines with the game clock |
| `WatchCounts()` | Master line when `GameRules.MidbossKillCount` / `AmberRejuvCount` / `SapphireRejuvCount` change |
| `IsBoss(entity)` | Tracked handle or `MidBossRule.IsBoss` |
| `ScaleHit(TakeDamageEvent)` | For a boss: `Info.Damage *= DamageScale`, counts the hit, logs the first hit after each scale change. Always `Continue` |
| `SetDamageScale(scale, mode)` | Clamps, resets the hit counters | applied scale |
| `Spawn(position, mode)` | `CreateByDesignerName(npc_super_neutral)`, then `Spawn(CEntityKeyValues)` with `subclass_name`, `origin`, `angles` 0, `teamnumber` 4; tracks it | entity |
| `Schedule(seconds, mode)` | Sets `citadel_midboss_initial_spawn_time_override` (999999 = parked); never touches `citadel_npc_spawn_enabled` | reply |
| `SetHealth(hp)` / `SetMaxHealth(hp)` / `SetPercent(pct)` | Writes `Health`, `m_iMaxHealth`, or `Health` = pct of `GetMaxHealth()` on every tracked boss | one line per boss |
| `RemoveModifier(name)` | `RemoveModifier(name)` on every boss | one line per boss |
| `Kill(attacker)` | `TakeDamage` for health + 1 with `ForceDeath` (no resistances, no `OnTakeDamage`); attacker is the caller's hero or the boss | one line per boss |
| `Remove()` | `Remove()` every boss (no death, no crystal) | one line per boss |
| `Describe()` | Bosses (health, max, effective max, team, position, modifier names), game clock, next spawn, kill and crystal counts, the three convars, damage scale and hit totals, crystals | lines |
| `Report(entity)` | One status line for a boss |

Every op that changes a boss throws `CommandException` when none is tracked.

## Dangerous constraints

- `Spawn` is untested in game. Community notes report a native crash for a
  boss created without keyvalues, and the 2026-09-29 Rem creeps upload was
  rolled back after a crash. Keep the rollback stamp ready.
- The engine owns `CEntityKeyValues` after `Spawn`; never dispose them.
- A spawned boss may not report `npc_super_neutral` as its designer name;
  it is tracked by handle at creation for that reason.
- Whether `Info.Damage` written in `OnTakeDamage` changes the hit is
  unverified; `DamageBefore` / `DamageAfter` plus the boss's health show it.
- `Schedule` fights CleanSlate: CleanSlate puts 999999 back on every load
  and `/cleanup_run`.
