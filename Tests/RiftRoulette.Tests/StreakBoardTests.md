# StreakBoardTests

Unit tests for `RiftRoulette/Duel/StreakBoard`.

- Streaks 1 to 5, then a later 3: best stays 5 and `Record` returns false.
- A new king (`KothRule.Crown` to another player) records 1 for them and
  leaves the previous king's best unchanged.
- An unknown player has best 0; `Forget` and `Clear` drop rows.
