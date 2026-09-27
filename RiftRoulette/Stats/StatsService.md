# StatsService

Match kill / death / assist tracking and the stats boards shown when the
draft is off (Stage 13c; 1v1 mode added in 13g). Static; owns one `StatsLedger` and the last known round counts.

## Operations

| Op | Behavior |
|---|---|
| `Reset(mode)` | Clears the ledger, takes round counts from `MatchService.State`, logs, refreshes boards. Called by `MatchService.Start` and `/stats_reset` |
| `SetRounds(sapphire, amber, mode)` | Stores round counts, refreshes boards. Called by `MatchService.OnRoundEnded` outside 1v1 mode |
| `RecordDeath(args, mode)` | From `player_death`. Only while a match runs (intermission or round). Victim must be a human controller. If `RandomModeService.ConsumeEnforcementKill` or `DuelService.ConsumeEnforcementKill` is true (hero swap guard), the death is skipped. Otherwise attacker and `Assister1controller`..`Assister5controller` (humans only) go to `StatsLedger.RecordDeath`; a credited team feeds `BalanceService.RecordKill`, and the killer gets betting chips (`BettingService.OnKill`, Random mode). Debug log, then refresh boards |
| `RefreshBoards(mode)` | When the draft is off (Random and 1v1 mode; not Draft). 1v1 mode: both boards get the same `StatsBoardText.StreakBoard(DuelService.StreakRows())` leaderboard. Random mode: for Sapphire and Amber, rows for participants (no bots, no seated admin) by current `TeamNum`, text from `StatsBoardText.TeamBoard`. Either way `WorldTextService.Update` if the board exists, else `Create` at `BoardLayout.Side(team)` (same spots and colors) |
| `Describe(player)` | `/stats`: the caller's `K / D / A`, then both team totals |
| `DescribeAll()` | Admin: match running, tracked count, rounds, then one line per human (team, K/D/A, score) |

Boards: `stats.sapphire`, `stats.amber` (same spots as the Draft pool
boards). `DraftService.RedrawBoards` clears all boards and calls
`RefreshBoards` when the draft is off (Random and 1v1 mode), so refresh
always recreates missing boards.

## State

Ledger (per match), last round counts. Stats stay after `/match_end` until
the next `/match_start`.

## Logs

`Stats` feature log (`stats-YYYYMMDD.log`): resets (Information), each
recorded death (Debug).

## Constraints

- Bots are ignored as victims, attackers and assisters.
- Whether the game fills the assister fields is unconfirmed; without them
  assists stay 0.
