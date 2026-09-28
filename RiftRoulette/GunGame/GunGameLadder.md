# GunGameLadder

Kill ladder for one Gun Game match (Stage 13k). Pure; unit tested in
`Tests/RiftRoulette.Tests`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Record(attacker, victim)` | Adds one kill for `attacker`; the first to reach `Target` becomes `Winner` | `LadderStep(Counted, Kills, Won)` |
| `TrySetTarget(target)` | Sets the target when it is within `MinTarget`..`MaxTarget` (1..50) | false when out of range |
| `KillsOf(steamId)` | Ladder kills, 0 when none | int |
| `PlaceOf(steamId)` | 1 + the number of players with more kills (ties share a place) | int |
| `Standings()` | Most kills first, then by Steam ID | list of `(SteamId, Kills)` |
| `Forget(steamId)` | Drops the player's kills | — |
| `Reset()` | Drops every kill and the winner; keeps `Target` | — |

## Invariants

- `Record` never counts a self kill (`attacker == victim`) or any kill once
  there is a `Winner`; it then returns `Counted = false` and the attacker's
  current kills.
- Deciding which deaths are kills (human attacker, enemy victim, not a hero
  lock kill) is the caller's job: `Stats/StatsService.RecordDeath` credits
  the kill, then hands it to `GunGameService.OnKill`.
- `Target` is a setting and survives `Reset`; `DefaultTarget` is 10.
