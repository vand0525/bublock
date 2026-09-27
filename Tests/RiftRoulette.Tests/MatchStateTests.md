# MatchStateTests

Unit tests for `RiftRoulette/GameLoop/MatchState` (with `Rift/RiftRoundResult`).

- A new state is idle with `Sapphire 0 - 0 Amber`.
- `Start` enters the intermission; `BeginRound` counts rounds.
- `finished` gives the point to the winner team (Sapphire 3, Amber 2) and
  returns it.
- `finished` with an unknown or null team scores nothing.
- `tied`, `cancelled`, and `spawn timed out` score nothing, even with a
  team set; only `tied` counts as a tie.
- `Reset` clears phase, round, score, and ties.
- `DescribeResult` banner text for each outcome.
