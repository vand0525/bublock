# AutoRestartRuleTests

Unit tests for `RiftRoulette/Lobby/AutoRestartRule`.

- A stuck join on an empty server reloads, even with the stuck client
  still connecting.
- 3 hours up on an empty server reloads; that waits for a join in progress
  and for 10 minutes after a hibernation wake (exactly 10 is allowed). A
  stuck join still reloads right after a wake.
- A gap of 180 s or more between checks is a wake; 60 s and 179 s are not.
- Never while anyone plays (1 or 12), never when switched off, never
  within 10 minutes of a map start (exactly 10 minutes is allowed).
- A healthy server under 3 hours stays up.
- A join counts as stuck at 180 s, not 179 s.
