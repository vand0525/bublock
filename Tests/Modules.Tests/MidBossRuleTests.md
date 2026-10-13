# MidBossRuleTests

Unit tests for `Modules/MidBoss/MidBossRule` (pure).

- `HealthAtPercent`: the share of max (51% of 13,000 is 6,630), rounded;
  never below 1 (0%, negative, tiny percent, max 0); capped at max; NaN
  means full.
- `ClampScale`: kept within 0.1–10; NaN and infinity give 1.
- `ScaleDamage`: multiplies by the clamped scale.
- `TryParseSchedule`: `off` (any case, spaces) is 999999; whole seconds
  0–999999 pass; negatives, decimals, words, empty and above 999999 fail.
- `IsBoss`: designer or class `npc_super_neutral`, or class
  `CNPC_MidBoss`; a Walker is not a boss.
- `IsCrystal`: only `citadel_item_pickup_rejuv`.
- `IsValidHealth`: 1 to 1,000,000.
- `IsValidSouls`: 0 to 1,000,000.
- `MinuteBySouls`: starts at (0, 0); souls and minutes both rise.
- `MinuteForSouls`: 0 at or below 0 souls, the table value at a point,
  linear between points (21,250 souls is minute 22.05), the last slope
  past 45,000 (47,500 is minute 40.7).
- `HealthAtMinute`: 13,000 at minute 0 or below; the two in-game spawns
  (15.98 min gives 16,120, 23.43 min gives 17,485); capped at 1,000,000.
- `HealthForSouls`: 0 souls 13,000; 20,000 souls 17,095; 45,000 souls
  20,605.
- `ScaleForFighters`: 12 over fighters (12 is 1×, 6 is 2×, 2 is 6×, 24 is
  0.5×), clamped to 10× (1 or 0 fighters) and 0.1× (500).
