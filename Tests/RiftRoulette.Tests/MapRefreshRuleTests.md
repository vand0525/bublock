# MapRefreshRuleTests

Unit tests for `RiftRoulette/Lobby/MapRefreshRule`.

- Due at the budget (160), not at 159; still due above it.
- Never due when the budget is 0 (off).
- Rounds left from map start: 40 at 2v2, 27 at 3v3, 20 at 4v4, 16 at 5v5,
  14 at 6v6 (rounded up).
- Rounds left after some play: 1 at 156 (2v2), 0 once due.
- Rounds left is unknown (`null`) when off or with nobody fighting.
