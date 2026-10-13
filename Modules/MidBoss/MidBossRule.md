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
| `TryParseSchedule(text, out seconds)` | `off` gives 999999; else whole seconds 0–999999 |
