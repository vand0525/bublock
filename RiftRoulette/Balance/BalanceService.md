# BalanceService

Auto-balance for Random mode. Static; holds one
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
| `Describe()` | Enabled, current verdict, tracker counters, the rules (including `Needs 3+ fighting (the bench does not count); counters reset while fewer fight`) | lines |

### TryBalance

Called by `RandomModeService.PrepareRound` at the start of each
intermission, before heroes are drawn. `teams` is Random mode's Steam ID to
team dictionary; editing it makes the following `Start` call `ChangeTeam`.

1. Fewer than `BalancePicker.MinPlayers` (3) fighters in `teams` (the
   bench player is never in it): resets the tracker, Debug log `Balance
   off, fewer than 3 fighting Fighters=`, nothing. Even when forced. A 1v1's
   rounds and kills so never carry into a bigger match (at most the round
   that just ended counts, which cannot trigger a stomp or a streak).
2. Disabled and not forced: nothing.
3. Verdict = `Tracker.Check()`; when forced (`/balance_now`) and there is
   no verdict, `Forced` for `Tracker.Leader()`. No verdict: Debug log, nothing.
4. `BalancePicker.Pick` with each player's `StatsService` score. No move:
   Information log, nothing.
5. Applies the move to `teams`, logs (feature + master), sends every human
   the chat line `Auto-balance: Theo <-> Sam` (or `Theo moves to Amber`),
   and resets the tracker.

## Logs

`Balance` feature log (`balance-YYYYMMDD.log`); swaps also go to master.
