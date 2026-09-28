# BetBookTests

Unit tests for `RiftRoulette/Betting/BetBook`.

- New players have the starting souls; a kill adds 100 each time.
- `TrySpend` takes unstaked souls only; a short balance, a zero amount, or
  souls staked on a bet are refused and nothing changes.
- A bet stakes every soul (souls 0, total unchanged).
- A team that is not allowed is refused and nothing is staked.
- With 0 souls a bet is refused.
- A second bet only changes the team (same stake).
- Settling doubles the winner's stake, takes the loser's, and clears the
  bets; no winner refunds.
- `Reset` gives everyone the starting souls and drops open bets.
