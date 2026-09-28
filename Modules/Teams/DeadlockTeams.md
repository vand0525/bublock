# DeadlockTeams

Deadlock's team numbers and names, and new-player placement. Pure; unit
tested in `Tests/Modules.Tests`.

## Constants

`Spectator` 1, `Amber` 2, `Sapphire` 3 (the game's numbers; Rift Roulette's
`RiftRouletteTeams` uses the same).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `IsPlayable(team)` | Amber or Sapphire | bool |
| `Other(team)` | Sapphire → Amber, anything else → Sapphire | int |
| `Name(team)` | `Amber`, `Sapphire`, `Spectator`, else `Team N` | string |
| `SmallerTeam(teams, rng)` | Counts Amber and Sapphire in `teams` (other numbers ignored); the smaller one, a tie by `rng` | 2 or 3 |

## Invariants

- Game-agnostic: no game type's names or rules. Module commands take team
  numbers, never names (`.rules` §6).
