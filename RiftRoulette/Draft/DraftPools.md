# DraftPools

The two Rift Roulette hero pools. Pure data; unit tested in
`Tests/RiftRoulette.Tests`. Extracted from Legacy `SapphireDraft` /
`AmberDraft` in Stage 9 (values and order unchanged).

## Values

| Pool | Heroes (board order) |
|---|---|
| `Sapphire` | Shiv, Yamato, Mirage, Wraith, Krill, Viper |
| `Amber` | Fencer, Drifter, Doorman, Werewolf, PunkGoat, Magician |

## Operations

- `TeamOf(hero)`: `RiftRouletteTeams.Sapphire` (3) for a Sapphire hero,
  `RiftRouletteTeams.Amber` (2) for an Amber hero, 0 otherwise. Same order of
  checks as the archive `/select` ternary.

## Consumers

- `DraftService` (pick team, board text, pool listings).
- `Rift/RiftService.MoveTeamsToRift` (who goes to which rift start).

## Invariants

- Read-only lists; order is the board order.
