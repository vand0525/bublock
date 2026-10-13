# PitRoundRuleTests

Unit tests for `CrystalCrackers/Pit/PitRoundRule` (pure).

- `Other`: Amber and Sapphire swap; spectator and 0 give 0.
- `TryParseTeam`: `amber` / `sapphire` in any case with spaces; `2`,
  `spectator` and empty fail.
- `PitTeam`: Amber first, then alternates; an admin's choice wins.
- `BlocksSouls`: every phase but `Idle`.
- `Winner`: on a finished window the crystal team, else the killer's
  team, else 0 (also 0 for a non-team killer); time limit and admin end
  give 0.
- `OnRespawn`: fighters dying in `Live` / `BossDown` are sent up unless
  reborn (kept in); a fighter's spawn without a death (hero swap) is left
  alone; non-fighters in a fight and everyone in `Countdown` go up; in
  `Idle` only once a round was played.
- `StartsInPit`: only the pit team, never a non-team.
- `Score`: both teams, 0 when missing.
- `IsFarFromPit`: height ignored; the pit floor and the lane starts
  (2000) are near; 8000 out and (2500, 2500) are far.
- `TryParseSeconds`: whole seconds in range (trimmed); out of range,
  words and decimals fail.
