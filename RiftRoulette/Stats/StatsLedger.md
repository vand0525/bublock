# StatsLedger

Pure per-match kill / death / assist counts by Steam ID. No game
calls; unit tested in `Tests/RiftRoulette.Tests`.

## Types

- `PlayerStats(Kills, Deaths, Assists)`: `Empty`, `Score` (K + A - D, used
  by auto-balance to rank players), `Line` (`"K / D / A"`).
- `Participant(SteamId, Team)`: a player at the moment of the death.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `RecordDeath(victim, attacker?, assisters)` | Victim always gets a death. The attacker gets a kill only if present, non-zero, not the victim, and on another team (suicides, world / trooper kills, and team kills give no kill). Assisters get an assist only if non-zero, on the attacker's team, and not the attacker or victim; each at most once | team credited with the kill, or `null` |
| `Get(steamId)` | Stats, or `Empty` | `PlayerStats` |
| `Total(steamIds)` | Sum of those players' stats | `PlayerStats` |
| `Reset()` | Clears everything | — |
| `Count` | Players tracked | `int` |
