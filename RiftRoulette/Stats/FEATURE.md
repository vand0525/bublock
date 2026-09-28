# Stats — Feature

## Purpose

Counts every player's kills, deaths and assists from match start
and, in Random mode, shows them on two boards in the draft area: Sapphire's
on the Sapphire side and Amber's on the Amber side, each with the team's
rounds won, the team total, and one row per player.

In 1v1 mode the same two boards show the best-streak leaderboard
(`STREAKS`, then `1  Name   5` rows, highest first) instead of K/D/A. K/D/A
still counts for `/stats`.

## Files

| File | Role |
|---|---|
| `StatsLedger.cs` | Per-player counts and the kill / assist rules (pure, tested) |
| `StatsBoardText.cs` | Board text: team K/D/A and the 1v1 streak leaderboard (pure, tested) |
| `StatsService.cs` | Death recording, board refresh, descriptions |
| `StatsPlugin.cs` | `player_death` hook, `/stats`, `/stats_board`, `/stats_reset` |

## Public operations

See `StatsService.md`. Commands catalogued in `reference/user-commands.md`
(`/stats`) and `reference/admin-commands.md` (`/stats_board`, `/stats_reset`).

## State

`StatsService.Ledger` and round counts (one per DLL load). Reset at
`/match_start` and by `/stats_reset`.

## Dependencies

- `Modules/WorldText` (boards), `Draft/BoardLayout` (positions).
- `GameLoop/MatchService` (running check, round counts), `MatchConfig`
  (boards only when the draft is off: Random and 1v1 mode).
- `RandomMode/RandomModeService.ConsumeEnforcementKill` and
  `Duel/DuelService.ConsumeEnforcementKill` (hero swap deaths
  are not counted), `Balance/BalanceService.RecordKill` (kill counts for
  auto-balance), `Duel/DuelService.StreakRows` (1v1 leaderboard; Duel
  refreshes the boards after each win and at match end).

## Lifecycle vs commands

- The `player_death` hook and the match loop (reset, round counts) run in
  Clean mode; Lobby connect / disconnect and `RandomModeService.PrepareRound`
  refresh the boards.
- Admin commands run the same ops in Debug mode. `/stats` is a player
  command.

## Logs

`stats-YYYYMMDD.log`.
