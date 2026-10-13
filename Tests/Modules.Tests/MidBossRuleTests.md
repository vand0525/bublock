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
