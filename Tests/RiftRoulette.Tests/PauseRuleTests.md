# PauseRuleTests

Unit tests for `RiftRoulette/Lobby/PauseRule`.

- `IsPauseCommand` matches the four pause commands (any case, trimmed) and
  rejects look-alikes such as `citadel_pause_count`, other commands, empty
  and null.
- `ConVars(false)` sets every pause convar to 0; `ConVars(true)` turns
  pausing back on and keeps pregame pausing off. Neither touches the pause
  count convars (0 there means unlimited).
- `ShouldUnpause` fires only while paused with pausing off, at most once
  per 2 s.
- `ShouldTell` fires at most once per 5 s per player.
