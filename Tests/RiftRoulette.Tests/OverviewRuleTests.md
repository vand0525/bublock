# OverviewRuleTests

Unit tests for `RiftRoulette/Lobby/OverviewRule`.

- The top-down view shows for 10 s from its start and not at 10 s; a null
  end means not showing.
- `CanStart` allows the first top-down view of a live round, refuses a
  second one in the same round, and allows the next round's first.
- Never starts when no round is live, or before the first round
  (round number 0).
