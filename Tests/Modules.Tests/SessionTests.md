# SessionTests

Unit tests for the pure parts of `Modules/Session` (`SessionRule`,
`Scoreboard`). `TimedSession` needs the game's timers and is checked in
game (`gg_status`, `session-*.log`).

- `Decide` (min 2): waiting starts at 2, playing / break stop below 2,
  otherwise nothing.
- `Winners`: the top scorer, everyone tied on top, nobody with no scores.
- `Clock`: `2:00`, `0:07`, negatives `0:00`.
- Match length 30..1800 seconds.
- `Scoreboard`: totals, standings order, shared places, unknown last,
  `Forget`, `Clear`.
