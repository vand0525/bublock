# MidBossRule

Pure constants and math for the mid-boss module. No game calls, so it is
unit tested (`Tests/Modules.Tests/MidBossRuleTests`).

## Constants

| Name | Value | Meaning |
|---|---|---|
| `BossName` | `npc_super_neutral` | Mid-boss designer name (C++ `CNPC_MidBoss`) |
| `BossClass` | `CNPC_MidBoss` | Also accepted as a classname match |
| `CrystalName` | `citadel_item_pickup_rejuv` | Rejuvenator crystal the boss drops |
| `InitialSpawnConVar` | `citadel_midboss_initial_spawn_time_override` | Scheduler's first spawn (game seconds; -1 = vdata) |
| `SpawnIntervalConVar` | `citadel_midboss_spawn_interval_override` | Respawn interval (shown in status only) |
| `NpcSpawnConVar` | `citadel_npc_spawn_enabled` | CleanSlate sets 0; may block the scheduler |
| `ParkedSpawnSeconds` | 999999 | Scheduler parked (CleanSlate's value) |
| `NeutralTeam` | 4 | Team of the map's mid-boss spawn point |
| `DefaultScale` / `MinScale` / `MaxScale` | 1 / 0.1 / 10 | Damage scale range |
| `MaxHealthLimit` | 1,000,000 | Largest health a command accepts |
| `ResistanceModifier` | `npc_super_neutral/midboss_modifier_damage_resistance` | The damage threshold that zeroes hits before `OnTakeDamage` |
| `StartingHealth` / `HealthPerMinute` | 13,000 / 195 | `npc_units.vdata`: a boss spawned at game minute m gets 13,000 + 195 × m (m rounded; proven in game) |
| `ReadyPercent` | 51 | Ready health, just above the 50% scream |
| `BaselineFighters` | 12 | Fighters that hit at 1× (6v6) |
| `MaxSouls` | 1,000,000 | Largest souls value `/midboss_ready` accepts |
| `MinuteBySouls` | 19 points, 0 to 45,000 souls | Median game minute at which a real ranked lobby's average net worth per player reaches the souls (`scripts/midboss-health.py`, 1,000 matches, 2026-10-13) |
| `CampSpot` | (0, 0, -768) | `info_neutral_trooper_camp` `neutral_camp_midboss` on `dl_midtown` |

## Functions

| Function | Result |
|---|---|
| `IsBoss(designer, class)` | Designer or class is `npc_super_neutral`, or class is `CNPC_MidBoss` |
| `IsCrystal(designer)` | Designer is `citadel_item_pickup_rejuv` |
| `ClampScale(scale)` | Clamped to 0.1–10; NaN / infinity give 1 |
| `ScaleDamage(damage, scale)` | `damage * ClampScale(scale)` |
| `HealthAtPercent(max, percent)` | `max * percent / 100` rounded, percent clamped 0–100, never below 1 |
| `IsValidHealth(health)` | 1 to `MaxHealthLimit` |
| `IsValidSouls(souls)` | 0 to `MaxSouls` |
| `MinuteForSouls(souls)` | Linear between `MinuteBySouls` points; 0 at or below 0 souls; past 45,000 the last segment's slope continues (about 0.7 min per 1,000 souls) |
| `HealthAtMinute(minute)` | 13,000 + 195 × minute rounded (half away from zero), negative minutes as 0, capped at `MaxHealthLimit` |
| `HealthForSouls(souls)` | `HealthAtMinute(MinuteForSouls(souls))`: the max health the game gives a boss spawned when a real lobby averages that many souls |
| `ScaleForFighters(fighters)` | `ClampScale(12 / fighters)`, fighters below 1 as 1: 12 → 1×, 6 → 2×, 2 → 6×, 1 → 10× (clamped) |
| `TryParseSchedule(text, out seconds)` | `off` gives 999999; else whole seconds 0–999999 |

## Data notes

- Real first mid-boss kills (same run) come at a median minute 23.9 with
  a lobby average of 23,665 souls, on the table's curve.
- The table stops where fewer than a quarter of the matches reach the
  souls (45,000); higher values are extrapolated.
- Rerun `scripts/midboss-health.py` after economy patches and copy the
  printed table in by hand (tests check it rises).
