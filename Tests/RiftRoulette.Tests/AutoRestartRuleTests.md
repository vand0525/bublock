# AutoRestartRuleTests

Unit tests for `RiftRoulette/Lobby/AutoRestartRule`.

- A stuck join on an empty server reloads, even with the stuck client
  still connecting.
- 3 hours up on an empty server reloads; that waits for a join in progress.
- Never while anyone plays (1 or 12), never when switched off, never
  within 10 minutes of a map start (exactly 10 minutes is allowed).
- A healthy server under 3 hours stays up.
- A join counts as stuck at 180 s, not 179 s.
