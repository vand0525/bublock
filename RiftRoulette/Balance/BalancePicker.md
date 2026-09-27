# BalancePicker

Pure choice of who moves when auto-balance triggers (Stage 13c). Unit tested.

## Operation

`Pick(players, winningTeam)` over `BalanceCandidate(SteamId, Team, Score)`
(`Score` = K + A - D this match):

- No move (`null`) if the winning team is empty or fewer than `MinPlayers`
  (3) players are on the two teams (a 1v1 swap changes nothing).
- Best = winning team's highest score.
- Winning team bigger than the losing team: `BalanceMove(best, null)`, only
  the best player moves.
- Otherwise: `BalanceMove(best, weakest)`, a swap with the losing team's
  lowest score.
- Ties break by lowest Steam ID (deterministic).
