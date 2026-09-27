# BalanceService

Auto-balance for Random mode (Stage 13c). Static; holds one
`BalanceTracker` and the `Enabled` flag (on by default, reset to on at every
DLL load).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `SetEnabled(enabled, mode)` | Turns auto-balance on or off; logs | — |
| `Reset(mode)` | Zeroes the tracker (match start) | — |
| `RecordRound(pointTo, mode)` | Tracker round result (from `MatchService.OnRoundEnded`); Debug log | — |
| `RecordKill(team)` | Tracker kill (from `StatsService.RecordDeath`) | — |
| `TryBalance(teams, mode, force)` | See below | announcement text or `null` |
| `Describe()` | Enabled, current verdict, tracker counters, the rules | lines |

### TryBalance

Called by `RandomModeService.PrepareRound` at the start of each
intermission, before heroes are drawn. `teams` is Random mode's Steam ID to
team dictionary; editing it makes the following `Start` call `ChangeTeam`.

1. Disabled and not forced: nothing.
2. Verdict = `Tracker.Check()`; when forced (`/balance_now`) and there is
   no verdict, `Forced` for `Tracker.Leader()`. No verdict: Debug log, nothing.
3. `BalancePicker.Pick` with each player's `StatsService` score. No move:
   Information log, nothing.
4. Applies the move to `teams`, logs (feature + master), sends every human
   the chat line `Auto-balance: Theo <-> Sam` (or `Theo moves to Amber`),
   and resets the tracker.

## Logs

`Balance` feature log (`balance-YYYYMMDD.log`); swaps also go to master.
