# WatchGuardRuleTests

Unit tests for `RiftRoulette/Round/WatchGuardRule`.

- `Line` is the watch spot height minus `Margin`.
- Standing, jumping or dropping a little stays above the line; 1200 and lane
  height (250) are below it.
- The line is more than 500 units above the rift starts, so fighters on the
  ground are never near it (they are released anyway).
