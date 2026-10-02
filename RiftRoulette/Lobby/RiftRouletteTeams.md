# RiftRouletteTeams

Rift Roulette team numbers and names. Pure (no game calls), unit tested in
`Tests/RiftRoulette.Tests`.

## Values

| Const | Team number | Used by |
|---|---|---|
| `Amber` | 2 | Amber team (`ChangeTeam(2)`); roaming admins play on 2 |
| `Sapphire` | 3 | Sapphire team (`ChangeTeam(3)`) |

## Operations

- `TryParse(name, out team)`: `sapphire` → 3, `amber` → 2, ignoring case;
  anything else returns false with `team = 0`.
- `IsPlayable(team)`: true for Sapphire or Amber.
- `Other(team)`: Amber for Sapphire, Sapphire for anything else.
- `Name(team)`: `Sapphire`, `Amber`, or `Team<n>` for any other number.

## Invariants

- Game-specific: lives under `RiftRoulette/`, never in `Modules/` (module
  commands take team numbers).
