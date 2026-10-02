# BetBookTests

Unit tests for `RiftRoulette/Betting/BetBook`.

- New players have the starting souls; a kill adds 100 each time, an assist
  50.
- An assist during an open bet adds unstaked souls (the stake is unchanged).
- `TrySpend` takes unstaked souls only; a short balance, a zero amount, or
  souls staked on a bet are refused and nothing changes.
- A bet stakes every soul (souls 0, total unchanged).
- A team that is not allowed is refused and nothing is staked.
- With 0 souls a bet is refused.
- A second bet only changes the team (same stake).
- Settling doubles the winner's stake, takes the loser's, and clears the
  bets; no winner refunds.
- `Refund` adds unstaked souls (an open stake is untouched); a zero or
  negative amount adds nothing.
- `Steal` takes a quarter of the total from unstaked souls first, then off
  the open stake (team kept); from an all-in player only the stake shrinks
  and the thief's own bet is untouched; it rounds down; nothing to take,
  the same player, or a zero divisor take nothing; taking everything closes
  the bet.
- `Reset` gives everyone the starting souls and drops open bets.
