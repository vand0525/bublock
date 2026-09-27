# WatchSpotRuleTests

Unit tests for `RiftRoulette/Round/WatchSpotRule`.

- A running rift keeps the watch spot above the side being fought on, even
  though `NextSide` already flipped at spawn.
- Idle (intermission, no match) uses the next side.
- Running with no current side falls back to the next side.
