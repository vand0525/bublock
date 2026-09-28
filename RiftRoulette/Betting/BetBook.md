# BetBook

Pure bookkeeping of betting souls for round betting. No game calls, so it
is unit tested (`Tests/RiftRoulette.Tests/BetBookTests`). One instance
lives in `BettingService`. Code names still say `Chips`; every player-facing
line says souls (a separate balance from in-game souls).

## State

- Betting souls per Steam ID. Anyone not seen yet has `StartingChips` (100).
- Open bets per Steam ID: `Bet(Team, Stake)`.

## Operations

| Operation | Result |
|---|---|
| `Chips(steamId)` | Souls not staked (`StartingChips` for someone new) |
| `Total(steamId)` | Souls plus the open stake (what the leaderboard shows) |
| `TryGetBet(steamId, out bet)` | The open bet, if any |
| `Bets`, `OpenBets` | All open bets and their count |
| `AwardKill(steamId)` | Adds `ChipsPerKill` (100); returns the new souls |
| `TrySpend(steamId, amount)` | Takes `amount` from the unstaked souls (hero reservations, `RandomModeService.Reserve`). Returns false, and takes nothing, when `amount` is not positive or the souls are short; a staked bet does not count |
| `Place(steamId, team, allowedTeams)` | `NotAllowed` if the team is not in `allowedTeams`. With a bet already open: only the team changes (`Changed`). Otherwise `NoChips` at 0 souls, else stakes every soul on the team (`Placed`) |
| `Settle(winner)` | Per open bet, in Steam ID order: `Won` (souls + stake x `PayoutMultiplier` 2), `Lost` (stake gone), or `Refunded` when `winner` is null. Clears the bets and returns a `BetSettlement` per bet with the new balance |
| `RefundAll()` | `Settle(null)` |
| `Reset()` | Clears souls and bets (everyone back to the starting souls) |

## Invariants

- Betting is all in: after `Placed`, `Chips` is 0 until the bet is
  settled or a kill adds more.
- `Total` never changes when a bet is placed or its team changed.
- Souls never go negative.
- Spent souls are gone: `Total` drops by the amount, and nothing refunds them.
