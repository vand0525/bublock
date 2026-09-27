# KothRuleTests

Unit tests for `RiftRoulette/Duel/KothRule`.

- A capture by one fighter's team makes that fighter the winner and the
  other the loser, for both teams.
- A tie (no winner team) has no winner or loser.
- A winner team with no fighter on it, or with no opponent, has no winner
  or loser (both fighters stay).
- `Crown` adds to the streak when the king wins again and restarts at 1 for
  a new king.
