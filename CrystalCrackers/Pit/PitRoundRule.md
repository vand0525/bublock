# PitRoundRule

Pure decisions for the pit round (no game types beyond `Vector3`), tested
in `PitRoundRuleTests`.

## Types

- `PitPhase`: `Idle` (no round), `Countdown` (everyone up top, `Round N`
  banner), `Live` (boss up, fighting), `BossDown` (boss dead, crystal
  window running).
- `PitEndReason`: `WindowOver` (crystal window finished), `TimeLimit`
  (boss alive at the limit), `Admin` (`/pit_end`).
- `RespawnAction`: `None` (leave alone), `SendUp` (out for the round:
  restrain and send up top), `KeepIn` (reborn by the crystal: stays in).

## Constants

| Name | Value |
|---|---|
| `AmberTeam` / `SapphireTeam` | 2 / 3 |
| `CountdownSeconds` | 3 |
| Crystal window | default 20 s, 5–120 |
| Time limit | default 300 s, 30–1800 |
| `RebirthReturnDistance` | 3000 (horizontal, from the pit center at the origin) |

## Operations

| Op | Result |
|---|---|
| `IsTeam(team)` | Team 2 or 3 |
| `Other(team)` | The other playing team; 0 for anything else |
| `TeamName(team)` | `Amber`, `Sapphire`, else `Nobody` |
| `TryParseTeam(text, out team)` | `amber` / `sapphire`, any case, trimmed |
| `PitTeam(chosen, lastPitTeam)` | The admin's choice; else the other team from last round; else Amber |
| `BlocksSouls(phase)` | True in every phase but `Idle` |
| `IsFighting(phase)` | `Live` or `BossDown` |
| `Winner(reason, crystalTeam, killerTeam)` | Only on `WindowOver`: the crystal team, else the boss killer's team, else 0. Time limit and admin end have no winner |
| `OnRespawn(phase, isFighter, died, reborn, roundsPlayed)` | `Countdown`: `SendUp`. `Live` / `BossDown`: a non-fighter `SendUp`; a fighter who did not die this round (the spawn of a hero swap) `None`, the swap callback places them; a fighter who died `KeepIn` when reborn, else `SendUp`. `Idle`: `SendUp` once a round was played, else `None` |
| `StartsInPit(team, pitTeam)` | The team holds the pit this round (else it starts in the lane) |
| `Score(wins)` | `Sapphire N - Amber M` |
| `EndLine(reason)` | Banner line for a round with no winner |
| `IsFarFromPit(position)` | Horizontal distance from the origin over 3000 (the lane starts at 2000 are not far) |
| `TryParseSeconds(text, min, max, out seconds)` | Whole seconds within `[min, max]` |

## Invariants

- A dead fighter is out for the round unless the mid-boss crystal's
  rebirth credit brought them back; the vanilla rejuvenator rebirth is
  never undone.
- No winner without a finished crystal window.
