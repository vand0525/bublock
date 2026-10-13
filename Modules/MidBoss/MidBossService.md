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
| `Ready(souls, fighters?, mode)` | Removes every tracked boss (no death, no crystal), spawns one at `CampSpot`, removes `ResistanceModifier`, writes `m_iMaxHealth` = `HealthForSouls(souls)`, then `Health` = 51% of it, and sets the damage scale to `ScaleForFighters` (fighters given, else `CountFighters`). Master line `Mid-boss ready` | `ReadyResult` |
| `Recheck(ready, mode)` | 1 s after `Ready` (the plugin's timer): if the resistance is back it is removed again; if the max moved, max and health are written again (warning line). Never throws | report line |
| `CountFighters()` | Non-bot players on team 2 or 3 (a roaming admin on a team counts; spectators don't) | count |
| `Remove()` | `Remove()` every boss (no death, no crystal) | one line per boss |
| `Describe()` | Bosses (health, max, effective max, team, position, modifier names), game clock, next spawn, kill and crystal counts, the three convars, damage scale and hit totals, crystals | lines |
| `Report(entity)` | One status line for a boss |

Every op that changes the tracked bosses (except `Ready`, which spawns
one) throws `CommandException` when none is tracked.

`ReadyResult`: the boss, souls, minute, max health, health, fighters and
whether they were counted, the applied scale, `removed` / `not found` for
the resistance, whether the max field was written, and how many old bosses
were replaced.

## Dangerous constraints

- `Spawn` (keyvalues `subclass_name`, `origin`, `angles`, `teamnumber`) was
  proven in game on 2026-10-13 (no crash). A boss created without
  keyvalues is reported to crash; keep them.
- The engine owns `CEntityKeyValues` after `Spawn`; never dispose them.
- A spawned boss may not report `npc_super_neutral` as its designer name;
  it is tracked by handle at creation for that reason.
- With `ResistanceModifier` on, pit hits reach `OnTakeDamage` already at 0,
  so `ScaleHit` changes nothing; `Ready` removes it first.
- Writing `m_iMaxHealth` doesn't clamp `Health`; write the max first, then
  the health. A new boss always gets the vdata max again.
- `Schedule` fights CleanSlate: CleanSlate puts 999999 back on every load
  and `/cleanup_run`.
