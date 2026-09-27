# StatsPlugin

Thin plugin class (`Name` = "Rift Roulette Stats").

## Hooks

- `player_death`: `StatsService.RecordDeath(args)` (Clean). Runs alongside
  Lobby's `player_death` logging hook.

## Commands

| Command | Who | Calls | Reply |
|---|---|---|---|
| `/stats` | player | `StatsService.Describe` | two chat lines: your K / D / A, then team totals |
| `/stats_board` | admin | `StatsService.RefreshBoards(Debug)`, `DescribeAll` | `[Stats]` lines to the caller's console |
| `/stats_reset` | admin | `StatsService.Reset(Debug)` | `[Stats] Match stats reset` |

Admin commands call `AdminCommand.Authorize` with the `Stats` log first
(server console trusted).
