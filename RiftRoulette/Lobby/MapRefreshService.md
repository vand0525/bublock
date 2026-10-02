# MapRefreshService

Join budget map refresh. Counts fighter-rounds (one per fighter per Random
mode round) since the map started. When the count reaches the budget
(`MapRefreshRule`, default 160) at a scored round end, it warns everyone in
chat, ends the match, and reloads the map 10 s later
(`AutoRestartService.Restart("join-budget")`). Auto-start then begins a
fresh match when 2+ humans are back. This keeps the join package under the
512 KB limit (see `MapRefreshRule.md`).

## State

- `FighterRounds`: fighter-rounds since the map start (reset in `OnMapStart`).
- `Budget`: default `MapRefreshRule.DefaultBudget`; `/restart_budget`
  changes it until the next upload; 0 = off.
- `LastFighters`: fighters in the last counted round (for the status line).
- `Pending`: a refresh was started and the map has not restarted yet.

## Operations

| Op | Behavior |
|---|---|
| `AddRound(fighters, mode)` | From `MatchService.StartRound` (Random mode, `RandomModeService.FighterCount`). Adds `fighters` (ignored when 0), Information `Round counted Fighters= FighterRounds= Budget=` |
| `OnMapStart()` | From `AutoRestartService.OnMapStart`: Information `Map started, join budget reset`, zeroes the counter, clears `Pending` |
| `TryBegin(timer, mode)` | From `MatchService.OnRoundEnded` (scored path, after the result banner). False when `Pending` or not `MapRefreshRule.Due`. Otherwise: sets `Pending`, sends `WarningLine` to every player (`Players.GetAll()`, chat only), Warning `Join budget reached, ending the match and reloading in 10s` (master too), `MatchService.End`, and after `WarningSeconds` (10) calls `AutoRestartService.Restart(Reason)`. Returns true (the caller skips `ScheduleNextRound`) |
| `SetBudget(budget, mode)` | Sets `Budget` (negative becomes 0), Information and a master line. Returns reply text |
| `Describe()` | `Join budget: N/B fighter-rounds since map start (~R rounds left at XvX)`, or off; notes a pending reload |

Chat line: `Server refresh in 10s: the map reloads to keep joins working.
You'll reconnect automatically, so don't leave. A new match starts right
after.`

## Side effects

- Ends the running match (score, bets and the round's heroes reset, the
  `Match over` banner shows), then reloads the map; every client
  reconnects by itself (`IsMapChangeReconnect`).
- While `Pending`, `AutoStartService.Check` never starts a match and the
  waiting chat line is not sent.
- If the map has not restarted `FallbackSeconds` (60) after the reload
  call, Warning `Refresh reload did not happen`, clears `Pending` and runs
  `AutoStartService.Check` (the next scored round end tries again).

## Invariants

- Random and Mirror mode rounds are counted (`MatchService.StartRound`
  adds each round's fighter count).
- Never ends a round early: only at a scored round end.
- Logs go to `restart-YYYYMMDD.log`.

## Known limits

- A hot reload (DLL upload) resets the counter but not the string tables,
  so after an upload mid-map the refresh comes late.
- The first player back after the reload may see the usual waiting line
  until a second player reconnects.
